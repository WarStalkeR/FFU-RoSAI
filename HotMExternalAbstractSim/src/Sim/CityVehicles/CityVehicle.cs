 using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.Universal.Deserialization;
using Arcen.HotM.Visualization;
using MathExpressionParser;

namespace Arcen.HotM.External
{
    internal class CityVehicle : ConcurrentPoolable<CityVehicle>, IProtectedListable, ISimCityVehicle
    {
        //serialized
        //---------------------------------------------------------
        public Int64 CityVehicleID;
        public CityVehicleType Type;
		private Vector3 VisWorldLocation;
        private readonly List<Vector3> SimPath = List<Vector3>.Create_WillNeverBeGCed( 8, "CityVehicle-SimPath", 4 );

        //nonserialized
        //---------------------------------------------------------
        private static int LastGlobalCityVehicleIndex = 1;
        public readonly int GlobalCityVehicleIndex;

        private MapCell VisCurrentMapCell = null;
        private CityVehicleMovementBase MovementLogic = null;

        public bool HasPointToFirePodsAtWhenGettingNewPath { get; set; } = false;
        public Vector3 PointToFirePodsAtWhenGettingNewPath { get; set; } = Vector3.zero;
        private bool isWaitingOnNewPath = false;

        private IAutoPooledFloatingObject floatingObject; //this may be used in place of the icon if we're not in detective mode, depending on the CityVehicle
        private IA5RendererGroup rGroupToUseOnFadeOut;
        private Quaternion objectRotation = Quaternion.identity;
		private Color chosenObjectColor = ColorMath.Transparent;
        private bool hasChosenObjectColor = false;
        private bool wasLoadedFromSaveGame = false;
        private Int64 LastFramePrepRendered = -1;

        private bool IsFullyFadedIn = false;
        private float AlphaAmountFromFadeIn = 0;

        private readonly List<SimBuilding> workingBuildings = List<SimBuilding>.Create_WillNeverBeGCed( 20, "CityVehicle-workingBuildings" );
        private readonly List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList =
            List<(LanePrefab lane, int index, Vector3 tPos)>.Create_WillNeverBeGCed( 12, "CityVehicle-workingLaneList", 12 );

        public void ClearData()
        {
            this.CityVehicleID = -1;
            this.Type = null;
            this.SimPath.Clear();

            this.VisWorldLocation = Vector3.zero;
            this.VisCurrentMapCell = null;
            this.MovementLogic = null;

            this.HasPointToFirePodsAtWhenGettingNewPath = false;
            this.PointToFirePodsAtWhenGettingNewPath = Vector3.zero;
            this.isWaitingOnNewPath = false;

            if ( this.floatingObject != null )
            {
                //this.floatingObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingObject = null;
            }
            this.rGroupToUseOnFadeOut = null;

            this.objectRotation = Quaternion.identity;
            this.chosenObjectColor = ColorMath.Transparent;
            this.hasChosenObjectColor = false;
            this.wasLoadedFromSaveGame = false;
            this.LastFramePrepRendered = -1;

            this.IsFullyFadedIn = false;
            this.AlphaAmountFromFadeIn = 0;

            this.workingBuildings.Clear();
            this.workingLaneList.Clear();
        }

        private const float FADE_IN_SPEED = 0.6f;

        #region Pooling
        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "CityVehicle" );
        private CityVehicle()
        {
            if ( RefTracker != null )
                RefTracker.IncrementObjectCount();
            GlobalCityVehicleIndex = Interlocked.Increment( ref LastGlobalCityVehicleIndex );
        }

        private static readonly ConcurrentPool<CityVehicle> Pool = new ConcurrentPool<CityVehicle>( "CityVehicle",
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new CityVehicle(); } );

        public static void InitializePoolIfNeeded( ref long poolCount, ref long poolItemCount )
        {
            poolCount++;
            poolItemCount += Pool.DoInitializationIfNeeded( 900 ); //900 should be enough to handle most of the map
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool(); //when I am removed from a list, put me back in the pool
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.ClearData();
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }
        #endregion PoolingLogic

        #region EqualsRelated
        public bool EqualsRelated( IAutoRelatedObject Other )
        {
            if ( Other is CityVehicle otherVehicle )
                return this.GlobalCityVehicleIndex == otherVehicle.GlobalCityVehicleIndex;
            return false;
        }
        #endregion

        #region CreateNew
        public static CityVehicle CreateNew( CityVehicleType Type, RandomGenerator Rand )
        {
            CityVehicle CityVehicle = Pool.GetFromPoolOrCreate();
            CityVehicle.Type = Type;
            CityVehicle.CityVehicleID = Interlocked.Increment( ref World_CityVehicles.LastCityVehicleID );
            World_CityVehicles.TryAddCityVehicle( CityVehicle );
            CityVehicle.MovementLogic = Type.GetMovementType();
            return CityVehicle;
        }
        #endregion

        public bool GetHasCityVehicleExpired()
        {
            if ( this.Type == null )
                return true;
            if ( this.CityVehicleID < 0 )
                return true;
            if ( this.GetInPoolStatus() )
                return true;
            return false;
        }

        #region Serialize / Deserialize
        public bool GetCanBeSerialized()
        {
            if ( this.ToGetNewPath != null )
                return false; //we have no good way of serializing this, so we can't serialize the entire thing
            return true;
        }

        public void Serialize( ArcenFileSerializer Serializer )
        {
            //already on a CityVehicle sub object

            Serializer.AddInt64( "ID", this.CityVehicleID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Type", this.Type.ID );
            Serializer.AddVector3( "WorldLocation", this.VisWorldLocation );
            Serializer.AddListIfHasAnyEntries( ArcenSerializedDataType.UnityVector3, "Path", this.SimPath );
        }

        public static void Deserialize( DeserializedObjectLayer Data )
        {
            CityVehicleType CityVehicleType = Data.GetTableRow( "Type", CityVehicleTypeTable.Instance, false );
            if ( CityVehicleType == null )
            {
                ArcenDebugging.LogSingleLine( "Warning: skipped CityVehicle of type '" + Data.GetString( "Type", false ) +
                    "', as it was not found in the current data.  Perhaps the game just changed, or mods changed?", Verbosity.DoNotShow );
                return;
            }

            CityVehicle CityVehicle = Pool.GetFromPoolOrCreate();
            CityVehicle.CityVehicleID = Data.GetInt64( "ID", true );

            CityVehicle.Type = CityVehicleType;
            CityVehicle.SetSimAndVisWorldLocation( Data.GetUnityVector3( "WorldLocation", true ) );

            if ( Data.TryGetList( "Path", out List<Vector3> path ) )
            {
                foreach ( Vector3 point in path )
                    CityVehicle.SimPath.Add( point );
            }

            if ( CityVehicleType.MoveAlongRoadsAtRandom != null )
                CityVehicle.MovementLogic = CityVehicleType.MoveAlongRoadsAtRandom;
            else if ( CityVehicleType.MoveFromBuildingToBuilding != null )
                CityVehicle.MovementLogic = CityVehicleType.MoveFromBuildingToBuilding;

            CityVehicle.wasLoadedFromSaveGame = true;

            World_CityVehicles.TryAddCityVehicle( CityVehicle );
        }
        #endregion

        #region SetSimAndVisWorldLocation
        public void SetSimAndVisWorldLocation( Vector3 Vec )
        {
            this.VisWorldLocation = Vec;
            this.VisCurrentMapCell = CityMap.TryGetWorldCellAtCoordinates( this.VisWorldLocation );
        }
        #endregion

        public void SetRotation()
        {
	        RotateObjectToFace(1000);
        }
        public List<(LanePrefab lane, int index, Vector3 tPos)> GetWorkingLaneList()
        {
            return workingLaneList;
        }

        public void DoPerQuarterSecondLogic_BackgroundThread( MersenneTwister Rand )
        {
            if ( this.isWaitingOnNewPath )
                this.CalculateNewPathOrDisband();
        }

        private void CalculateNewPathOrDisband()
        {
            try
            {
                if ( ToGetNewPath != null )
                {
                    int max = 10;
                    while ( SimPath.Count == 0 && max-- > 0 )
                    {
                        ToGetNewPath.Invoke( this, Engine_Universal.PermanentQualityRandom );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine( "CalculateNewPathOrDisband City vehicle error: " + e, Verbosity.ShowAsError );
                this.Disband( true );
                return;
            }

            if ( SimPath.Count == 0 )
            {
                this.Disband( true );
                return;
            }
            //ArcenDebugging.LogSingleLine( "New path! "  + this.Type.ID + "  " + this.GlobalCityVehicleIndex, Verbosity.DoNotShow );
            this.isWaitingOnNewPath = false; //let the main thread start moving again
        }

        /// <summary> Aka once per frame, on the main thread.  Therefore much more sensitive for performance reasons.</summary>
        public void DoPerFrameLogic()
        {
            if ( this.Type == null )
                return;

            #region AutoDisbandsIfNotInCityLifeRange Checking
            if ( this.Type.AutoDisbandsIfNotInCityLifeRange && ( this.VisCurrentMapCell == null || !(this.VisCurrentMapCell?.ShouldHaveExtended1xCityVehiclesRightNow ?? false) ) )
            {
                //this one is far from the camera, for this type of CityVehicle we don't care about that
                this.Disband( false ); //no need to drop fading copy of self, since we are so far away
                return;
            }
            #endregion

            if ( this.MovementLogic != null )
                this.DoPerFrameMovementLogic();
        }

        private void DoPerFrameMovementLogic()
        {
            if ( this.isWaitingOnNewPath )
                return;
			if (SimPath.Count == 0 )
			{
				return;
			}

			//if you were following a path and reach then end, then clear the path and set ShouldDisbandWhenVisLocationCatchesUpToSimLocation to true
			Vector3 dist = (SimPath[0] - VisWorldLocation);
			float moveDist = this.MovementLogic.MovementSpeed * ArcenTime.SmoothUnpausedDeltaTime * SimCommon.CurrentVisualSpeed  * SimCommon.BackgroundAnimationMoveSpeed;
			if (dist.sqrMagnitude < moveDist*30 + (MovementLogic.TurnRadius < 0.1 ? 0.1f : 0.005f))
			{
				SimPath.RemoveAt(0);

				if (SimPath.Count < 1)
				{
					if (!this.MovementLogic.ShouldDespawnWhenPathCompletes && ToGetNewPath == null)
					{
                        if ( !this.wasLoadedFromSaveGame ) //only complain if this wasn't loaded from a save
						    ArcenDebugging.LogSingleLine($"CityVehicle {this.Type.ID} does not despawn on path complete, but has no ability to get a new path.", Verbosity.ShowAsError);
						this.Disband( true );
						return;
					}

                    if ( this.MovementLogic.ShouldDespawnWhenPathCompletes )
                    {
                        this.Disband( true );
                        return;
                    }

                    this.isWaitingOnNewPath = true;
					return;
				}
			}

            //avoid falling behind by having to take a longer path due to interpolation
            //moveDist = MathA.Max(moveDist, Mathf.Lerp(0, dist.magnitude, 0.95f * moveDist));
            if (this.MovementLogic.TurnRadius >= 0 && SimPath.Count > 0)
			{
				Vector3 targetLook = SimPath[0] - this.VisWorldLocation; //the sim is ahead of the vis location
				targetLook.y *= 0f;

				if (targetLook.sqrMagnitude <= 0.01f || Quaternion.Angle(objectRotation, Quaternion.LookRotation(targetLook, Vector3.up)) < SimCommon.CurrentVisualSpeed * this.MovementLogic.RotationRate)
				{
					Vector3 dir = dist.normalized;
					VisWorldLocation += dir * moveDist;
				}
			}
			else
			{
				Vector3 Forward = this.objectRotation * Vector3.forward;
				VisWorldLocation += Forward * moveDist;
			}
			//note: this might be too far down, but it seems okay
			this.VisCurrentMapCell = CityMap.TryGetWorldCellAtCoordinates(this.VisWorldLocation);
		}

        private Action<ISimCityVehicle, MersenneTwister> ToGetNewPath;

		public void SetFuncCallForNewPath(Action<ISimCityVehicle, MersenneTwister> NewPathCall)
		{
			ToGetNewPath = NewPathCall;
		}

		public void DoPerSecondLogic_BackgroundThread( MersenneTwister Rand )
		{
            #region Stationary Checking
            if ( this.MovementLogic == null )
            {
                if ( SimPath.Count > 0 )
                    SimPath.Clear();
                this.Disband( true );
                return;
            }
            #endregion
        }

        public void DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, Int64 FramesPrepped, out bool ShouldSkipDrawing )
        {
            IsMouseOver = false;
            if ( this.LastFramePrepRendered >= FramesPrepped )
            {
                ShouldSkipDrawing = true;
                return;
            }
            this.LastFramePrepRendered = FramesPrepped;

            if ( this.GetHasCityVehicleExpired() )
            {
                ShouldSkipDrawing = true;
                return;
            }

            CityVehicleType vehicleType = this.Type;
            if ( vehicleType == null )
            {
                ShouldSkipDrawing = true;
                return;
            }

            if ( InputCaching.SkipDrawing_SmallFliers && vehicleType.IsSmallFlier )
            {
                ShouldSkipDrawing = true;
                return;
            }
            if ( InputCaching.SkipDrawing_StreetVehicles && vehicleType.IsStreetVehicle )
            {
                ShouldSkipDrawing = true;
                return;
            }

            if ( !this.IsFullyFadedIn )
            {
                this.AlphaAmountFromFadeIn += ArcenTime.UnpausedDeltaTime * FADE_IN_SPEED;
                if ( this.AlphaAmountFromFadeIn >= 1f )
                    this.IsFullyFadedIn = true;
            }

            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;
            MapCell cell = this.VisCurrentMapCell;

            if ( !isMapView )
            {
                if ( cell != null && !cell.IsConsideredInCameraView )
                {
                    ShouldSkipDrawing = true;
                    FrameBufferManagerData.CellFrustumCullCount.Construction++;
                    return;
                }
            }

            ShouldSkipDrawing = false;

            if ( vehicleType.VisObjectToDraw != null )
            {
                if ( !isMapView || vehicleType.ShouldShowVisObjectInMapMode ) //don't draw any of these in map mode, unless they say they should)
                {
                    IAutoPooledFloatingObject fObject = this.floatingObject;

                    if ( fObject == null || !fObject.GetIsValidToUse( this ) )
                    {
                        fObject = vehicleType.VisObjectToDraw.FloatingObjectPool.GetFromPool( this );
                        this.floatingObject = fObject;
                        IA5RendererGroup parentGroup = fObject.Renderer.ParentGroup;
                        if ( parentGroup != null )
                            this.rGroupToUseOnFadeOut = parentGroup;
                        if ( this.rGroupToUseOnFadeOut == null )
                            ArcenDebugging.LogSingleLine( "Null rGroupToUseOnFadeOut! " + this.Type.ID, Verbosity.ShowAsError );
                        this.RotateObjectToFace( 1000 ); //rotate correctly immediately

                        if ( !this.hasChosenObjectColor ) //we do this and remember it because we want the color to still be the same when we turn back to look at it!
                        {
                            this.hasChosenObjectColor = true;
                            this.chosenObjectColor = vehicleType.VisObjectToDraw.RandomColorType == null ? Color.white :
                                vehicleType.VisObjectToDraw.RandomColorType.ColorList.GetRandom( Engine_Universal.PermanentQualityRandom ).Color;
                        }
                    }
                    else
                    {
                        CityVehicleMovementBase moveLogic = this.MovementLogic;
                        if ( moveLogic != null )
                        {
                            float rotationRate = moveLogic.RotationRate;
                            if ( rotationRate <= 0 )
                                rotationRate = 1000;
                            this.RotateObjectToFace( ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * rotationRate * SimCommon.BackgroundAnimationMoveSpeed ); //rotate over time
                        }
                    }

                    fObject.CollisionLayer = InputCaching.IsInInspectMode_Any ? CollisionLayers.VehicleMixedIn : CollisionLayers.IgnoreRaycast;

                    fObject.ObjectScale = vehicleType.VisObjectScale;
                    fObject.WorldLocation = this.VisWorldLocation.PlusY( vehicleType.VisObjectExtraOffset );
                    fObject.MarkAsStillInUseThisFrame();
                    fObject.Rotation = this.objectRotation;

                    if ( fObject.IsMouseover )
                    {
                        IsMouseOver = true;
                        this.RenderTooltip();
                    }
                }
            }
        }

        public bool GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingObj, out Color color )
        {
            //now actually do the draw, as the above just handles colliders and any particle effects
            floatingObj = this.floatingObject;
            color = this.chosenObjectColor;
            if ( !this.IsFullyFadedIn )
                color.a = this.AlphaAmountFromFadeIn;
            return this.floatingObject != null && this.floatingObject.GetIsValidToUse( this );
        }

        private void RenderTooltip()
        {
            LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

            if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( "CityVehicle", (int)this.CityVehicleID, 0 ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
            {
                lowerLeft.TitleUpperLeft.AddRaw( this.GetDisplayName() );

                string desc = this.Type.GetDescription();
                if ( desc.Length > 0 )
                    lowerLeft.Main.AddRaw( desc, ColorTheme.NarrativeColor ).Line();

                if ( GameSettings.Current.GetBool( "Debug_ShowPathfinding" ) )
                {
                    if ( this.MovementLogic == null )
                    {
                        lowerLeft.Main.AddNeverTranslated( "{No movement logic}", true ).Line();
                    }
                    else
                    {
                        lowerLeft.Main.AddNeverTranslated( $"{SimPath.Count} waypoints", true ).Line();
                    }
                }

                if ( Type != null )
                {
                    Type.Implementation.AddExtraTooltipInformation( Type, this, lowerLeft.Main );
                }
            }
        }

        #region DropFadingCopyOfSelf
        private void DropFadingCopyOfSelf( float FadeOutSpeed)
        {
            CityVehicleType vehicleType = this.Type;
            if ( vehicleType == null )
                return;

            IA5RendererGroup rGroup = this.rGroupToUseOnFadeOut;
            if ( rGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Fail rGroup null. " + vehicleType.ID, Verbosity.DoNotShow );
                return;
            }

            Vector3 pos = this.VisWorldLocation.PlusY( vehicleType.VisObjectExtraOffset );
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( pos );
            if ( cell == null )
            {
                //ArcenDebugging.LogSingleLine( "Fail cell null.", Verbosity.DoNotShow );
                return; //this is valid to happen; we might be out of where any cells are.
            }

            MapFadingItem fadingItem = MapFadingItem.GetFromPoolOrCreate();
            fadingItem.Position = pos;
            fadingItem.Rotation = this.objectRotation;
            fadingItem.Scale = new Vector3( vehicleType.VisObjectScale, vehicleType.VisObjectScale, vehicleType.VisObjectScale );

            fadingItem.RendererGroup = rGroup;
            fadingItem.ColorStyle = RenderColorStyle.SelfColor;

            fadingItem.FadeOutSpeed = FadeOutSpeed;
            fadingItem.BaseColor = this.chosenObjectColor;
            if ( !this.IsFullyFadedIn )
                fadingItem.BaseColor.a = this.AlphaAmountFromFadeIn;

            float speed = this.MovementLogic.MovementSpeed;
            if ( speed > 0.01f )
            {
                fadingItem.HasRemainingMotion = true;
                fadingItem.RemainingMotionDirectionAndSpeed = speed * (this.objectRotation * Vector3.forward );
            }

            MapEffectCoordinator.AddMapFadingItem( cell, fadingItem );

            //ArcenDebugging.LogSingleLine( "Presumed success. " + vehicleType.ID, Verbosity.DoNotShow );
        }
        #endregion

        #region DropBurningCopyOfSelf
        private void DropBurningCopyOfSelf()
        {
            CityVehicleType vehicleType = this.Type;
            if ( vehicleType == null )
                return;

            IA5RendererGroup rGroup = this.rGroupToUseOnFadeOut;
            if ( rGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Fail rGroup null. " + vehicleType.ID, Verbosity.DoNotShow );
                return;
            }

            Vector3 pos = this.VisWorldLocation.PlusY( vehicleType.VisObjectExtraOffset );
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( pos );
            if ( cell == null )
            {
                //ArcenDebugging.LogSingleLine( "Fail cell null.", Verbosity.DoNotShow );
                return; //this is valid to happen; we might be out of where any cells are.
            }

            MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();
            materializingItem.Position = pos;
            materializingItem.Rotation = this.objectRotation;
            materializingItem.Scale = new Vector3( vehicleType.VisObjectScale, vehicleType.VisObjectScale, vehicleType.VisObjectScale );

            materializingItem.RendererGroup = rGroup;
            materializingItem.Materializing = MaterializeType.BurnDownLarge;

            MapEffectCoordinator.AddMaterializingItem( materializingItem );

            //ArcenDebugging.LogSingleLine( "Presumed success. " + vehicleType.ID, Verbosity.DoNotShow );
        }
        #endregion

        #region RotateObjectToFace
        private void RotateObjectToFace( float maxDelta )
        {
            if ( maxDelta <= 0 || SimPath.Count < 1)
                return;
            Vector3 targetLook = ( SimPath.Count > 1 ? SimPath[1] : SimPath[0] ) - this.VisWorldLocation; //the sim is ahead of the vis location
            targetLook.y = 0f;

			if ( targetLook.sqrMagnitude <= 0.1f )
                return; //if the range is really short, don't rotate us; it will give us invalid rotations anyway, and cause error cascades
            
			Quaternion lookRot = Quaternion.LookRotation( targetLook, Vector3.up );

            if ( lookRot.x == float.NaN )
                return; //don't try to rotate to this!

            if ( maxDelta > 900 ) //if we want to get there so bad, just get there
                this.objectRotation = lookRot;
            else
            {
	            /*float angle = Quaternion.Angle(objectRotation, lookRot);
				if (angle < 5f)
	            {
		            maxDelta *= 0.2f;
	            }*/

                this.objectRotation = Quaternion.RotateTowards( this.objectRotation, lookRot, maxDelta );
                if ( this.objectRotation.x == float.NaN ) //if we got a NaN result, then just make us the lookRot
                    this.objectRotation = lookRot;
            }
        }
        #endregion

        #region Disband
        public void Disband( bool FadeOut )
        {
            if ( FadeOut )
                this.DropFadingCopyOfSelf( 3f );

            World_CityVehicles.RemoveCityVehicle( this );
        }
        #endregion

        public void DisbandAndBurn()
        {
            this.DropBurningCopyOfSelf();

            World_CityVehicles.RemoveCityVehicle( this );
        }

        #region ISimCityVehicle
        public Int64 GetCityVehicleID()
        {
            return this.CityVehicleID;
        }

        public CityVehicleType GetCityVehicleType()
        {
            return this.Type;
        }

        public Vector3 GetVisWorldLocation()
        {
            return this.VisWorldLocation;
        }

        public MapCell GetVisCurrentMapCell()
        {
            return this.VisCurrentMapCell;
        }
		
		public bool GetIsValidToUse() => true;

        /// <summary> Call this from any thread other than the sim thread, and we'll have some exceptions eventually </summary>
        public List<Vector3> GetPathListToAlter_FromSimThreadOnly()
        {
            return this.SimPath;
        }

        public string GetDisplayName()
        {
            return this.Type?.GetDisplayName() ?? "[null]";
        }

        public void WriteDataItemUIXClickedDetails_SubTooltipLinkHover( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
        {
        }

        public MouseHandlingResult WriteWorldExamineDetails_SubTooltipLinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
        {
            return MouseHandlingResult.None;
        }

        public bool DataItemUIX_TryHandlePrimaryClick( out bool ShouldCloseWindow )
        {
            this.DataItemUIX_HandleAltClick( out ShouldCloseWindow );
            return true;
        }

        public void DataItemUIX_HandleAltClick( out bool ShouldCloseWindow )
        {
            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( this.VisWorldLocation, false );
            ShouldCloseWindow = true;
        }
        #endregion
    }
}

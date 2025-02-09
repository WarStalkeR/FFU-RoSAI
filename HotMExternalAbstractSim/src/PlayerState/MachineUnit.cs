using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.External
{
    internal class MachineUnit : BaseMachineActor, ITimeBasedPoolable<MachineUnit>, IProtectedListable, ISimMachineUnit
    {
        //
        //Serialized data
        //-----------------------------------------------------
        public int UnitID = 0;
        public MachineUnitType UnitType { get; private set; }
        public int TurnsSinceMoved { get; private set; }
        public bool HasMovedThisTurn { get; private set; }
        public string UnitName = string.Empty;
        public Vector3 GroundLocation { get; private set; }
        public WrapperedSimUnitLocation ContainerLocation { get; private set; }
        private Vector3 drawLocation = Vector3.zero;

        //
        //NonSerialized data
        //-----------------------------------------------------
        public string DebugText { get; set; } = string.Empty;
        public DamageTextPopups.FloatingDamageTextPopup MostRecentDamageText { get; set; } = null;
        public MachineUnitStance Stance { get; set; }
        private IAutoPooledFloatingLODObject floatingLODObject;
        private IAutoPooledFloatingParticleLoop floatingParticleLoop;
        private Int64 LastFramePrepRendered = -1;
        private Quaternion objectRotation = Quaternion.identity;
        private bool hasEverSetRotation = false;

        private bool hasDoneStompCheckSinceCreationOrLoad = false;
        private float moveProgress = 0;
        private Vector3 originalSourceLocation = Vector3.zero;
        private Vector3 desiredNewGroundLocation = Vector3.zero;
        private ISimUnitLocation desiredNewContainerLocation = null;
        public MaterializeType Materializing { get; set; } = MaterializeType.None;
        public float MaterializingProgress { get; set; } = 0;
        private float RotateToY = 0;
        private bool IsRotatingYNow = false;

        private Action afterMove_MeleeCallback = null;

        private LocationActionType afterMove_Action;
        private NPCEvent afterMove_Event;
        private ProjectOutcomeStreetSenseItem afterMove_ProjectItem;
        private ISimBuilding afterMove_BuildingOrNull;
        private string afterMove_OtherOptionalID = string.Empty;

        private static readonly List<MapItem> staticWorkingBuildings = List<MapItem>.Create_WillNeverBeGCed( 20, "MachineUnit-workingBuildings" );

        public FogOfWarCutter FogOfWarCutting { get; set; } //this is not really reset or anything later
        public NPCRevealer NPCRevealing { get; set; } //this is not really reset or anything later       

        public readonly DoubleBufferedList<ISimBuilding> BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob = DoubleBufferedList<ISimBuilding>.Create_WillNeverBeGCed( 10, "MachineUnit-BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob", 10 );

        protected override string DebugStringDescriptor => "machine unit";
        protected override string DebugStringID => this.UnitType?.ID ?? "[null UnitType]";

        protected override void ClearData_MachineTypeSpecific()
        {
            //Serialized
            //---------------
            this.UnitID = 0;
            this.UnitType = null;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = false;
            this.UnitName = string.Empty;
            this.GroundLocation = Vector3.zero;
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );
            this.drawLocation = Vector3.zero;

            //Nonserialized
            //---------------
            this.DebugText = string.Empty;
            this.MostRecentDamageText = null;
            this.Stance = null;

            if ( this.floatingLODObject != null )
            {
                //this.floatingLODObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingLODObject = null;
            }

            if ( this.floatingParticleLoop != null )
            {
                //this.floatingParticleLoop.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingParticleLoop = null;
            }

            this.LastFramePrepRendered = -1;
            this.objectRotation = Quaternion.identity;
            this.hasEverSetRotation = false;

            this.hasDoneStompCheckSinceCreationOrLoad = false;
            this.moveProgress = 0;
            this.originalSourceLocation = Vector3.zero;
            this.desiredNewGroundLocation = Vector3.zero;
            this.desiredNewContainerLocation = null;
            this.Materializing = MaterializeType.None;
            this.MaterializingProgress = 0;
            this.RotateToY = 0;
            this.IsRotatingYNow = false;

            this.afterMove_MeleeCallback = null;

            this.afterMove_Action = null;
            this.afterMove_Event = null;
            this.afterMove_ProjectItem = null;
            this.afterMove_BuildingOrNull = null;
            this.afterMove_OtherOptionalID = string.Empty;

            //this.FogOfWarCutting; does not need a reset
            //this.NPCRevealing; does not need a reset
            BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearAllVersions();
        }

        #region CreateNew
        public static MachineUnit CreateNew( MachineUnitType UnitType, ISimUnitLocation ContainerLocation, int UnitID, MersenneTwister Rand, bool Materialize, bool IsForDeserialize )
        {
            MachineUnit unit = MachineUnit.GetFromPoolOrCreate();
            unit.UnitType = UnitType;

            if ( UnitID <= 0 )
                UnitID = Interlocked.Increment( ref SimCommon.LastActorID );
            unit.UnitID = UnitID;
            unit.ContainerLocation = new WrapperedSimUnitLocation( ContainerLocation );

            if ( !IsForDeserialize )
            {
                unit.CurrentTurnSeed = Rand.Next() + 1;
                unit.StatCalculationRandSeed = Rand.Next() + 1;
                unit.InitializeActorData_Randomized( UnitType.ID, UnitType.IsConsideredMech ? ActorDataTypeSet.PlayerMechs : ActorDataTypeSet.PlayerAndroids, UnitType.ActorData );
            }
            unit.DoPerSecondRecalculations( false ); //make sure any effective maximums are high enough

            if ( Materialize )
            {
                unit.Materializing = MaterializeType.Appear;
                unit.MaterializingProgress = 0;
            }
            return unit;
        }
        #endregion

        #region Serialize Unit 
        protected override void Serialize_MachineTypeTypeSpecific( ArcenFileSerializer Serializer )
        {
            Serializer.AddInt32( "ID", UnitID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Type", this.UnitType.ID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Stance", this.Stance.ID );
            Serializer.AddInt32IfGreaterThanZero( "TurnsSinceMoved", TurnsSinceMoved );
            Serializer.AddBoolIfTrue( "HasMovedThisTurn", this.HasMovedThisTurn );
            Serializer.AddUniqueString_UnicodeIfNotBlank( "UnitName", this.UnitName );
            ISimUnitLocation containerLocation = this.ContainerLocation.Get();
            if ( containerLocation == null )
                Serializer.AddVector3( "GroundLocation", this.GroundLocation );
            else
            {
                if ( containerLocation is MapOutdoorSpot outdoorSpot )
                    Serializer.AddInt32( "ContainerOutdoorSpotID", outdoorSpot.MapOutdoorSpotID );
                else if ( containerLocation is ISimBuilding building )
                    Serializer.AddInt32( "ContainerBuildingID", building.GetBuildingID() );
                else if ( containerLocation is ISimMachineVehicle machineVehicle )
                    Serializer.AddInt32( "ContainerVehicleID", machineVehicle.ActorID );
                else
                    ArcenDebugging.LogSingleLine( "MachineUnit: Unknown ISimUnitLocation container type on save! " + containerLocation.GetType(), Verbosity.ShowAsError );
            }
            Serializer.AddVector3( "drawLocation", this.drawLocation );
            if ( this.UnitType.IsConsideredMech )
                Serializer.AddInt32IfGreaterThanZero( "RotationY", Mathf.RoundToInt( this.objectRotation.eulerAngles.y ) );
        }

        public int ActorID { get { return this.UnitID; } }

        private DeserializedObjectLayer dataForLateDeserialize;
        public static MachineUnit Deserialize( DeserializedObjectLayer Data )
        {
            MachineUnitType unitType = Data.GetTableRow( "Type", MachineUnitTypeTable.Instance, true );
            if ( unitType == null )
                return null;

            int unitID = Data.GetInt32( "ID", true );
            MachineUnit unit = CreateNew( unitType, null, unitID, Engine_Universal.PermanentQualityRandom, false, true );
            unit.dataForLateDeserialize = Data;
            unit.UnitName = Data.GetString( "UnitName", false );
            unit.AssistInDeserialize_BaseMapActor_Primary( unit.UnitType.ID, unitType.IsConsideredMech ? ActorDataTypeSet.PlayerMechs : ActorDataTypeSet.PlayerAndroids, Data, unitType.ActorData, null );
            unit.AssistInDeserialize_BaseMachineActor_Primary( Data );

            unit.Stance = Data.GetTableRow( "Stance", MachineUnitStanceTable.Instance, false );
            if ( unit.Stance == null )
            {
                if ( unitType.IsConsideredAndroid )
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;
                else
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
            }

            if ( unitType.IsConsideredAndroid && !unit.Stance.CanBeUsedByAndroids )
                unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;
            else if ( unitType.IsConsideredMech && !unit.Stance.CanBeUsedByMechs )
                unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;

            if ( Data.TryGetUnityVector3( "GroundLocation", out Vector3 groundLocation ) )
                unit.SetActualGroundLocation( groundLocation ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn

            if ( Data.TryGetUnityVector3( "drawLocation", out Vector3 drawLocation ) )
                unit.drawLocation = drawLocation;

            if ( Data.TryGetInt32( "ContainerOutdoorSpotID", out int ContainerOutdoorSpotID ) )
            {
                MapOutdoorSpot containerSpot = World_OutdoorSpots.OutdoorSpotsByID[ContainerOutdoorSpotID];
                if ( containerSpot != null )
                    unit.SetActualContainerLocation( containerSpot );
                else
                {
                    unit.SetActualGroundLocation( unit.drawLocation ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn

                    if ( SimCommon.Turn > 1 )
                        ArcenDebugging.LogSingleLine( "MachineUnit: Asked for MapOutdoorSpot with ID " + ContainerOutdoorSpotID + ", but not found (" +
                            World_OutdoorSpots.OutdoorSpotsByID.Count + " spots).", Verbosity.ShowAsError );
                }
            }

            if ( Data.TryGetInt32( "ContainerBuildingID", out int ContainerBuildingID ) )
            {
                SimBuilding building = World_Buildings.BuildingsByID[ContainerBuildingID];
                if ( building != null )
                    unit.SetActualContainerLocation( building ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn
                else
                    ArcenDebugging.LogSingleLine( "MachineUnit: Asked for ISimBuilding with ID " + ContainerBuildingID + ", but not found (" +
                        World_Buildings.BuildingsByID.Count + " buildings).", Verbosity.ShowAsError );
            }

            //ContainerVehicleID must be handled later

            unit.TurnsSinceMoved = Data.GetInt32( "TurnsSinceMoved", false );
            unit.HasMovedThisTurn = Data.GetBool( "HasMovedThisTurn", false );

            if ( Data.TryGetInt32( "RotationY", out int rotationY ) )
            {
                unit.objectRotation = Quaternion.Euler( 0, rotationY, 0 );
                unit.hasEverSetRotation = true;
            }

            unit.DoPerSecondRecalculations( true ); //this will correct any effective maximums that are too low

            World_Forces.MachineUnitsByID[unit.UnitID] = unit;
            SimCommon.AllActorsByID[unit.UnitID] = unit;

            CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );

            return unit;
        }

        public void FinishDeserializeLate()
        {
            if ( dataForLateDeserialize.TryGetInt32( "ContainerVehicleID", out int ContainerVehicleID ) )
            {
                MachineVehicle vehicle = World_Forces.MachineVehiclesByID[ContainerVehicleID];
                if ( vehicle != null )
                    this.SetActualContainerLocation( vehicle );
                else
                    ArcenDebugging.LogSingleLine( "MachineUnit: Asked for MachineVehicle with ID " + ContainerVehicleID + ", but not found (" +
                        World_Forces.MachineVehiclesByID.Count + " machine vehicles).", Verbosity.ShowAsError );
            }

            this.AssistInDeserialize_BaseMapActor_Late( dataForLateDeserialize );
            this.AssistInDeserialize_BaseMachineActor_Late( dataForLateDeserialize );
            this.dataForLateDeserialize = null;
        }
        #endregion

        public int GetInt32ValueForSerialization()
        {
            return this.UnitID;
        }

        public override Vector3 GetDrawLocation()
        {
            return this.drawLocation;
        }

        public Quaternion GetDrawRotation()
        {
            return this.objectRotation;
        }

        public bool GetIsDeployed()
        {
            ISimUnitLocation loc = this.ContainerLocation.Get();
            return loc == null || loc.GetIsVisibleOnMapWorldLocationForContainedUnit();
        }

        public bool GetIsValidForCollisions()
        {
            return this.GetIsDeployed();
        }

        public bool GetIsInFogOfWar()
        {
            return false; //always false for machine units
        }

        public void DoOnPostTakeDamage( ISimMapActor DamageSource, int PhysicalDamageAmount, int MoraleDamageAmount, int SquadMembersLost, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            this.DoDeathCheck( Rand, ShouldDoDamageTextPopupsAndLogging );
        }

        public override void DoDeathCheck( MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            if ( this.IsFullDead )
                return;

            if ( this.GetActorDataCurrent( ActorRefs.ActorHP, false ) > 0 )
                return;

            this.HandleAnyMachineLogicOnDeath();

            this.SpawnBurningItemAtCurrentLocation();
            this.UnitType.OnDeath.DuringGame_PlayAtLocation( this.GetDrawLocation() );

            if ( this.UnitType.IsConsideredMech )
            {
                CityStatisticTable.AlterScore( "CombatLossesMechs", 1 );
            }
            else
            {
                CityStatisticTable.AlterScore( "CombatLossesAndroids", 1 );
            }

            ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.MachineUnitDied, NoteStyle.BothGame, this.UnitType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                this.GetDisplayName(), string.Empty, string.Empty,
                SimCommon.Turn + 10 );

            this.IsFullDead = true;

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            this.ReturnToPool(); //goodbye forever!
        }

        #region SpawnBurningItemAtCurrentLocation
        public void SpawnBurningItemAtCurrentLocation()
        {
            IAutoPooledFloatingLODObject fObject = this.floatingLODObject;
            if ( fObject != null )
            {
                bool drawAggressive = (this.Stance?.ShouldShowAggressivePose ?? false) || (this.CurrentActionOverTime?.Type?.ShouldShowAggressivePose ?? false) ||
                    (this.AlmostCompleteActionOverTime?.Type?.ShouldShowAggressivePose ?? false) || 
                    this.TurnsSinceDidAttack < 3; //if have attacked in the last three turns, show aggressive stance
                VisLODDrawingObject toDraw = drawAggressive ? this.UnitType?.VisObjectAggressive : this.UnitType?.VisObjectCasual;
                if ( toDraw == null )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = fObject.WorldLocation;
                materializingItem.Rotation = fObject.Rotation;
                materializingItem.Scale = fObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.LODRenderGroups[toDraw.LODRenderGroups.Count - 1];
                materializingItem.Materializing = this.GetBurnDownType();

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingLODObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingLODObject = null;
            }
        }
        #endregion

        public MaterializeType GetBurnDownType()
        {
            return this.UnitType.IsConsideredMech ? MaterializeType.BurnDownLarge : MaterializeType.BurnDownSmall;
        }

        public bool GetIsOverCap()
        {
            return this.UnitType?.GetIsOverCap() ?? false;
        }

        public void KillFromCrash( string CrashingVehicleName )
        {
            if ( this.IsFullDead )
                return;

            this.HandleAnyMachineLogicOnDeath();

            this.IsFullDead = true;

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            CityStatisticTable.AlterScore( this.UnitType.IsConsideredMech ? "VehicleCrashLossesMechs" : "VehicleCrashLossesAndroids", 1 );
            ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.MachineUnitDiedInCrash, NoteStyle.BothGame, this.UnitType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                this.GetDisplayName(), CrashingVehicleName, string.Empty,
                SimCommon.Turn + 10 );

            this.ReturnToPool(); //goodbye forever!
        }

        #region TryScrapRightNowWithoutWarning_Danger
        // Must be kept the same as PopupReasonCannotScrapIfCannot
        public bool TryScrapRightNowWithoutWarning_Danger( ScrapReason Reason )
        {
            if ( !this.PopupReasonCannotScrapIfCannot( Reason ) )
                return false;

            this.HandleAnyMachineLogicOnDeath();

            bool skipVFX = false;
            switch ( Reason )
            {
                case ScrapReason.ReplacementByPlayer:
                    skipVFX = true;
                    CityStatisticTable.AlterScore( this.UnitType.IsConsideredMech ? "ScrappedMechs" : "ScrappedAndroids", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.MachineUnitReplaced, NoteStyle.StoredGame, this.UnitType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.Cheat:
                case ScrapReason.ArbitraryPlayer:
                    skipVFX = Reason == ScrapReason.ArbitraryPlayer;
                    CityStatisticTable.AlterScore( this.UnitType.IsConsideredMech ? "ScrappedMechs" : "ScrappedAndroids", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.MachineUnitScrapped, NoteStyle.StoredGame, this.UnitType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.CaughtInExplosion:
                    CityStatisticTable.AlterScore( this.UnitType.IsConsideredMech ? "ExplosionLossesMechs" : "ExplosionLossesAndroids", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.MachineUnitDiedInExplosion, NoteStyle.BothGame, this.UnitType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "Oops!  There was nothing set up to log the scrapping of a unit for reason '" + Reason + "'!", Verbosity.ShowAsError );
                    break;
            }

            this.SpawnBurningItemAtCurrentLocation();
            if ( !skipVFX )
                this.UnitType.OnDeath.DuringGame_PlayAtLocation( this.GetDrawLocation() );

            this.IsFullDead = true;

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            this.ReturnToPool(); //goodbye forever!

            return true;
        }
        #endregion

        #region PopupReasonCannotScrapIfCannot
        // Must be kept the same as TryScrapRightNowWithoutWarning_Danger
        public bool PopupReasonCannotScrapIfCannot( ScrapReason Reason )
        {
            if ( this.IsFullDead )
                return false;

            if ( this.GetIsBlockedFromBeingScrappedRightNow( true ) )
                return false;

            return true;
        }
        #endregion

        #region Location Setting And Related
        public void SetActualContainerLocation( ISimUnitLocation CLoc )
        {
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            if ( CLoc != null )
            {
                this.GroundLocation = CLoc.GetEffectiveWorldLocationForContainedUnit();
                if ( this.UnitType?.IsConsideredMech??false )
                {
                    if ( CLoc is ISimMachineVehicle )
                    { } //this is fine, we're headed up to a vehicle
                    else //anything else clamps to the ground
                        this.GroundLocation = this.GroundLocation.ReplaceY( 0 );
                }

                this.drawLocation = this.GroundLocation;
            }
            this.ContainerLocation = new WrapperedSimUnitLocation( CLoc );
            CLoc?.SetOccupyingUnitToThisOne( this );
            this.isMoving = false;
            this.desiredNewContainerLocation = null;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;
            SimCommon.NeedsVisibilityGranterRecalculation = true;

            MapTile newTile = CLoc?.GetLocationMapCell()?.ParentTile;
            if ( newTile != null )
            {
                newTile.HasEverBeenExplored = true;
                newTile.IsTileContainingMachineActors.SetBothAtOnce( true );
            }
        }

        public void SetActualGroundLocation( Vector3 GLoc )
        {
            if ( this.UnitType.IsConsideredMech )
                GLoc.y = 0;

            this.GroundLocation = GLoc;
            this.drawLocation = GLoc;

            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );
            this.isMoving = false;
            this.desiredNewContainerLocation = null;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;
            SimCommon.NeedsVisibilityGranterRecalculation = true;

            MapTile newTile = CityMap.TryGetWorldCellAtCoordinates( GLoc )?.ParentTile;
            if ( newTile != null )
            {
                newTile.HasEverBeenExplored = true;
                newTile.IsTileContainingMachineActors.SetBothAtOnce( true );
            }
        }

        public void SetDesiredContainerLocation( ISimUnitLocation CLoc )
        {
            if ( CLoc == null )
                return;
            this.desiredNewContainerLocation = CLoc;
            this.originalSourceLocation = this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
            this.isMoving = true;
            this.moveProgress = 0;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;

            if ( this.UnitType.IsConsideredMech )
                this.RotateObjectToFace( this.GetDrawLocation(), this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit(), 1000 );
        }

        public void SetDesiredGroundLocation( Vector3 GLoc )
        {
            if ( this.UnitType.IsConsideredMech )
                GLoc.y = 0;
            else
            {
                //adapted from the logic with intersecting a road
                if ( GLoc.y < 0.4f )
                    GLoc.y = 0.4f;
            }

            this.desiredNewContainerLocation = null;
            this.desiredNewGroundLocation = GLoc;
            this.originalSourceLocation = this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
            this.isMoving = true;
            this.moveProgress = 0;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;

            if ( this.UnitType.IsConsideredMech )
                this.RotateObjectToFace( this.GetDrawLocation(), this.desiredNewGroundLocation, 1000 );
        }

        public void SetActionToTakeAfterMovementEnds( LocationActionType Action, ISimBuilding BuildingOrNull, NPCEvent EventTypeOrNull, ProjectOutcomeStreetSenseItem ProjectItemOrNull, string OtherOptionalID )
        {
            this.afterMove_Action = Action;
            this.afterMove_Event = EventTypeOrNull;
            this.afterMove_ProjectItem = ProjectItemOrNull;
            this.afterMove_BuildingOrNull = BuildingOrNull;
            this.afterMove_OtherOptionalID = OtherOptionalID;

            this.afterMove_MeleeCallback = null;
        }

        public void SetMeleeCallbackForAfterMovementEnds( Action MeleeCallback )
        {
            this.afterMove_Action = null;
            this.afterMove_Event = null;
            this.afterMove_ProjectItem = null;
            this.afterMove_BuildingOrNull = null;
            this.afterMove_OtherOptionalID = string.Empty;

            this.afterMove_MeleeCallback = MeleeCallback;
        }
        #endregion

        #region GetIsBlockedFromActing
        public bool GetIsBlockedFromActingForAbility( AbilityType Ability, bool CareAboutAPAndMentalEnergy )
        {
            return GetIsBlockedFromActingInner( CareAboutAPAndMentalEnergy && Ability.ActionPointCost > 0, CareAboutAPAndMentalEnergy && Ability.MentalEnergyCost > 0, Ability );
        }

        public bool GetIsBlockedFromActingInGeneral()
        {
            return GetIsBlockedFromActingInner( true, true, null );
        }

        public bool GetIsBlockedFromActingForTurnCompletePurposes()
        {
            return GetIsBlockedFromActingInner( true, false, null );
        }

        public bool GetIsBlockedFromActingForMovementPurposes()
        {
            return GetIsBlockedFromActingInner( false, false, null );
        }

        private bool GetIsBlockedFromActingInner( bool CareAboutActionPoints, bool CareAboutEnergy, AbilityType Ability )
        {
            if ( CareAboutActionPoints )
            {
                if ( this.CurrentActionPoints <= 0 )
                    return true;
            }

            if ( this.IsFullDead || !SimCommon.GetHaveAllNPCsActed() )
                return true;

            if ( this.CurrentActionOverTime != null && !(Ability?.CanBeUsedEvenWhenDoingActionOverTime ?? false) )
                return true;

            if ( CareAboutEnergy )
            {
                if ( ResourceRefs.MentalEnergy.Current <= 0 )
                    return true;
            }

            if ( this.UnitType.IsConsideredMech )
            {
                if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt && !(Ability?.CanBeUsedEvenWhenOverUnitCap??false) )
                    return true;
            }
            else
            {
                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt && !(Ability?.CanBeUsedEvenWhenOverUnitCap ?? false) )
                    return true;
            }
            return false;
        }
        #endregion

        #region GetIsBlockedFromAutomaticSelection
        public bool GetIsBlockedFromAutomaticSelection( bool CareAboutAP )
        {
            if ( (this.Stance?.ShouldNotBeAutoSelectedForOrders ?? false) )
                return true;

            //this section is duplicating most of GetIsBlockedFromActing, except leaving out some bits on purpose
            if ( (CareAboutAP && this.CurrentActionPoints <= 0) || this.CurrentActionOverTime != null || this.IsFullDead )
                return true;

            return false;
        }
        #endregion

        #region GetIsCurrentlyInvisible
        public bool GetIsCurrentlyInvisible( InvisibilityPurpose ForPurpose )
        {
            switch ( ForPurpose )
            {
                case InvisibilityPurpose.ForPlayerTargeting:
                case InvisibilityPurpose.ForNPCTargeting:
                    return (this.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false) || !this.GetIsDeployed() ||
                        (this.AlmostCompleteActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false);
                case InvisibilityPurpose.ForCameraFocus:
                default:
                    return false;
            }
        }
        #endregion

        #region Ability Getting And Setting
        public override bool GetActorHasAbilityEquipped( AbilityType Ability )
        {
            return this.UnitType?.DuringGameData?.GetActorHasAbilityEquipped( Ability )??false;
        }

        public override AbilityType GetActorAbilityInSlotIndex( int SlotIndex )
        {
            AbilityType ability = this.UnitType?.DuringGameData?.GetActorAbilityInSlotIndex( SlotIndex );

            if ( ability != null )
            {
                if ( ability.UseAlternativeWhenInVehicle != null && !this.GetIsDeployed() )
                    return ability.UseAlternativeWhenInVehicle;
            }
            return ability;
        }

        public override bool GetActorAbilityCanBePerformedNow( AbilityType AbilityType, ArcenDoubleCharacterBuffer BufferOrNull )
        {
            if ( AbilityType == null )
                return false;

            if ( AbilityType.DuringGame_IsHiddenByFlags() )
                return false;

            if ( !AbilityType.CanBeUsedWhenInVehicle && !this.GetIsDeployed() )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "CannotUseThisAbilityWhileStoredInVehicle" );
                return false;
            }

            if ( !AbilityType.CalculateIfActorCanUseThisAbilityRightNow( this, this.GetTypeDuringGameData(), BufferOrNull, true, false ) )
                return false;

            if ( this.UnitType.IsConsideredMech )
            {
                if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt && !AbilityType.CanBeUsedEvenWhenOverUnitCap )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.AddFormat1( "TooManyMechsOnline", SimCommon.TotalCapacityUsed_Mechs - MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt, ColorTheme.Gray );
                    return false;
                }
            }
            else
            {
                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt && !AbilityType.CanBeUsedEvenWhenOverUnitCap )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.AddFormat1( "TooManyAndroidsOnline", SimCommon.TotalCapacityUsed_Androids - MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt, ColorTheme.Gray );
                    return false;
                }
            }

            if ( this.CurrentActionPoints <= 0 && AbilityType.ActionPointCost > 0 )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "UnitIsOutOfActionPoints" );
                return false;
            }
            if ( AbilityType.ActionPointCost > this.CurrentActionPoints )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddFormat2( "UnitDoesNotHaveEnoughActionPoints", AbilityType.ActionPointCost, this.CurrentActionPoints );
                return false;
            }
            if ( AbilityType.MentalEnergyCost > 0 && AbilityType.MentalEnergyCost > ResourceRefs.MentalEnergy.Current )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddFormat2( "YouAreOutOfMentalEnergy", AbilityType.MentalEnergyCost, ResourceRefs.MentalEnergy.Current );
                return false;
            }
            if ( !SimCommon.GetHaveAllNPCsActed() )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.StartStyleLineHeightA().AddLang( "AllNPCUnitsNeedToFinishActingFirst" ).Line().StartSize90().AddFormat1( "PressXToSelectTheNextNPC",
                        InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(), ColorTheme.RedOrange2 ).EndSize().EndLineHeight();
                return false;
            }
            if ( this.CurrentActionOverTime != null && !AbilityType.CanBeUsedEvenWhenDoingActionOverTime )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "UnitBusyWithActionOverTime" );
                return false;
            }

            ISimBuilding currentBuildingOrNull = this.ContainerLocation.Get() as ISimBuilding;
            if ( AbilityType.IfUnitMustBeLocatedAtBuilding )
            {
                if ( currentBuildingOrNull == null )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.AddLang( "CanOnlyUseThisAbilityAtABuilding" );
                    return false;
                }
            }

            Vector3 location = this.GetDrawLocation();

            ActorAbilityResult result = AbilityType.Implementation.TryHandleAbility( this, currentBuildingOrNull, location, AbilityType, BufferOrNull,
                 ActorAbilityLogic.CalculateIfAbilityBlocked );

            if ( result == ActorAbilityResult.PlayErrorSound )
                return false;
            return true;
        }

        private static ArcenDoubleCharacterBuffer abilityOutputBuffer = new ArcenDoubleCharacterBuffer( "MachineVehicle-abilityOutputBuffer" );
        private static MersenneTwister postAbilityRand = new MersenneTwister( 0 );
        public void TryPerformActorAbilityInSlot( int SlotIndex, TriggerStyle Style )
        {
            this.TryPerformActorAbilityInSlot( SlotIndex, false, Style );
        }

        public override bool TryPerformActorAbilityInSlot( int SlotIndex, bool DoSilently, TriggerStyle Style )
        {
            AbilityType ability = GetActorAbilityInSlotIndex( SlotIndex );
            if ( !GetActorAbilityCanBePerformedNow( ability, DoSilently ? null : abilityOutputBuffer ) )
            {
                if ( !DoSilently )
                {
                    if ( !abilityOutputBuffer.GetIsEmpty() ) //if a reason was specified, show it to the player
                    {
                        VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( abilityOutputBuffer, TooltipID.Create( "Ability", ability.ID ), TooltipWidth.Mid, 3 );
                    }
                }
                return false;
            }

            ISimBuilding currentBuildingOrNull = this.ContainerLocation.Get() as ISimBuilding;
            Vector3 location = this.GetDrawLocation();

            //if we got to this point, we can actually do the ability
            ActorAbilityResult result = ability.Implementation.TryHandleAbility( this, currentBuildingOrNull, location, ability, null,
                 Style == TriggerStyle.DirectByPlayer ? ActorAbilityLogic.ExecuteAbilityFromPlayerDirect : ActorAbilityLogic.ExecuteAbilityAutomated );

            switch ( result )
            {
                case ActorAbilityResult.PlayErrorSound:
                    if ( !DoSilently && Style == TriggerStyle.DirectByPlayer )
                    {
                        //error sound, please
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    }
                    return false;
                case ActorAbilityResult.SuccessDidFullAbilityNow:
                    //do all the stuff, sound and statistics change and personality changes!
                    postAbilityRand.ReinitializeWithSeed( this.CurrentTurnSeed + SlotIndex );
                    ability.DuringGameplay_ApplyAbilityStatistics( postAbilityRand );
                    if ( !DoSilently )
                        ability.OnUse.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                    if ( ability.ActionPointCost > 0 )
                        this.AlterCurrentActionPoints( -ability.ActionPointCost );
                    if ( ability.MentalEnergyCost > 0 )
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -ability.MentalEnergyCost, "Expense_UnitAbilities", ResourceAddRule.IgnoreUntilTurnChange );

                    this.HandleEnemyActionsAfterUnitMoveOrAct( true );
                    return true;
                case ActorAbilityResult.OpenedInterface:
                    if ( !DoSilently )
                        ability.OnUse.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                    return true;
                case ActorAbilityResult.StartedActionOverTime:
                    if ( !DoSilently )
                    {
                        //play the on-use sound but don't do anything like apply the statistics and personality changes
                        ability.OnUse.DuringGame_PlayAtLocation( this.GetDrawLocation() );
                    }
                    this.HandleEnemyActionsAfterUnitMoveOrAct( true );
                    return true;
                case ActorAbilityResult.NothingHappened:
                    //huh, okay.  Be silent I guess
                    return false;
            }
            return false;
        }
        #endregion

        #region TryAltViewActorAbilityInSlot
        public void TryAltViewActorAbilityInSlot( int SlotIndex )
        {
            AbilityType ability = GetActorAbilityInSlotIndex( SlotIndex );
            if ( ability == null )
                return; //if nothing to alt view, then don't

            ISimBuilding currentBuildingOrNull = this.ContainerLocation.Get() as ISimBuilding;
            Vector3 location = this.GetDrawLocation();

            ability.Implementation.TryHandleAbility( this, currentBuildingOrNull, location, ability, null,
                 ActorAbilityLogic.TriggerAbilityAltView );
        }
        #endregion

        #region GetUnitCanBeRemovedFromVehicleNow
        public bool GetUnitCanBeRemovedFromVehicleNow( ArcenDoubleCharacterBuffer BufferOrNull )
        {
            ISimMachineVehicle vehicleOrNull = this.ContainerLocation.Get() as ISimMachineVehicle;
            if ( vehicleOrNull == null )
                return true; //if this unit is not in a vehicle, then sure, go ahead and let us out anytime?

            if ( this.UnitType.IsConsideredMech )
            {
                if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.AddFormat1( "TooManyMechsOnline", SimCommon.TotalCapacityUsed_Mechs - MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt );
                    return false;
                }
            }
            else
            {
                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.AddFormat1( "TooManyAndroidsOnline", SimCommon.TotalCapacityUsed_Androids - MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt );
                    return false;
                }
            }

            if ( this.CurrentActionPoints <= 0 )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "UnitIsOutOfActionPoints" );
                return false;
            }
            if ( this.CurrentActionOverTime != null )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "UnitBusyWithActionOverTime" );
                return false;
            }

            return true;
        }
        #endregion

        #region GetIsCurrentStanceInvalid
        public bool GetIsCurrentStanceInvalid()
        {
            return false;
        }
        #endregion

        #region HandleUnitPerTurnFairlyEarly
        public void HandleUnitPerTurnFairlyEarly( MersenneTwister RandForThisTurn )
        {
            this.HandlePerTurnMachineActorLogic();
            this.PerTurnHandleBaseFairlyEarlyActorLogic( RandForThisTurn );
            bool doActionOverTime = this.CurrentActionPoints > 0;
            int apRemainingAfterLastTurn = this.CurrentActionPoints;

            this.CurrentTurnSeed = Engine_Universal.PermanentQualityRandom.Next() + 1;
            this.TurnsSinceMoved++;
            this.HasMovedThisTurn = false;

            if ( this.Stance != null )
                this.SetCurrentActionPoints( this.MaxActionPoints );
            else
                this.SetCurrentActionPoints( 0 );

            this.PerTurn_HandleUnitContainedInVehicle();

            if ( this.CurrentActionOverTime != null && doActionOverTime )
            {
                apRemainingAfterLastTurn = 0;
                if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) > 0 )
                    this.CurrentActionOverTime.DoActionPerTurnLogic( RandForThisTurn ); //unit is still alive, so do the thing
                else
                    this.CurrentActionOverTime.DestroyAndCancelDueToFailureOfSomeKind(); //the failure is that the unit is dead
            }            

            this.Stance?.Implementation.HandleLogicForUnitInStance( this, this.Stance, RandForThisTurn, apRemainingAfterLastTurn, null, MachineUnitStanceLogic.PerTurnLogic );

            #region Discover Cell Things
            MapCell cell = this.CalculateMapCell();
            if ( cell != null )
            {
                MapDistrict district = cell?.ParentTile?.District;
                if ( district != null )
                {
                    if ( !district.HasBeenDiscovered )
                        district.HasBeenDiscovered = true;
                }
            }
            #endregion
        }
        #endregion

        #region HandleUnitPerTurnVeryLate
        public void HandleUnitPerTurnVeryLate( MersenneTwister RandForThisTurn )
        {
            this.PerTurnHandleBaseActorVeryLateLogic( RandForThisTurn );
        }
        #endregion

        #region PerTurn_HandleUnitContainedInVehicle
        private void PerTurn_HandleUnitContainedInVehicle()
        {
            ISimMachineVehicle vehicle = this.ContainerLocation.Get() as ISimMachineVehicle;
            if ( vehicle == null )
            {
                return; //we are not actually in a vehicle, so nevermind!
            }
        }
        #endregion

        public void DoPerSecondRecalculations( bool IsFromDeserialization )
        {
            this.RecalculateActorStats( IsFromDeserialization );
            this.RecalculatePerksPerSecond( this.UnitType.DuringGameData.Equipment, this.UnitType.DefaultPerks, this.Stance?.PerksGranted, this.Stance?.PerksBlocked );
        }

        protected override void RecalculateActorStats( bool IsFromDeserialization )
        {
            MachineUnitType type = this.UnitType;
            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            foreach ( KeyValuePair<ActorDataType, MapActorData> kv in this.actorData )
            {
                if ( kv.Value == null )
                    continue;
                kv.Value.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null, this.UnitType.DuringGameData.Equipment,
                    boostingStructures, null, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses, null, null,
                    type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(), type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(),
                    this.BadgeDict, null, null, IsFromDeserialization, null, -1f, -1f, 1, 1, -1f, 0, 0, false, this.UnitType.ID );
            }
        }

        public void AppendExtraDetailsForDataType( ArcenDoubleCharacterBuffer Buffer, MapActorData DataData )
        {
            if ( DataData == null || Buffer == null )
                return;
            MachineUnitType type = this.UnitType;
            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            DataData.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null, this.UnitType.DuringGameData.Equipment, 
                boostingStructures, null, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses, null, null,
                type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(), type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(),
                this.BadgeDict, null, null, false, Buffer, -1f, -1f, 1, 1, -1f, 0, 0, false, this.UnitType.ID );
        }

        public void DoPerFrameLogic()
        {
            this.PerFrameValidateActorData();

            #region Fix Missing Names
            if ( this.UnitName.Length <= 0 )
            {
                this.UnitName = this.UnitType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineUnit(
                    this.UnitType.NameStyle, this, Engine_Universal.PermanentQualityRandom );
            }
            #endregion

            bool weAreSelected = this == Engine_HotM.SelectedActor;        

            if ( this.isMoving )
                this.DoPerFrameMovementLogic();
            else
            {
                if ( !this.GetIsDeployed() ) //if a unit is not moving and is not deployed, then make sure its location is the current location of the vehicle it is in
                    this.drawLocation = this.ContainerLocation.Get().GetEffectiveWorldLocationForContainedUnit();
            }

            if ( this.CurrentActionOverTime != null )
            {
                if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) > 0 )
                    this.CurrentActionOverTime.DoActionPerFrameLogic( Engine_Universal.PermanentQualityRandom ); //unit is still alive, so check the thing
                else
                    this.CurrentActionOverTime.DestroyAndCancelDueToFailureOfSomeKind(); //the failure is that the unit is dead
            }

            if ( !SimCommon.IsCurrentlyRunningSimTurn )
            {
                if ( this.AlmostCompleteActionOverTime != null && this.AlmostCompleteActionOverTime.Type.ActionIsCompletedAtTurnChangeWithoutAskingUserToLookAtIt )
                    this.AlmostCompleteActionOverTime.DoActionFinalSuccessLogic( false, Engine_Universal.PermanentQualityRandom );
            }

            //if ( weAreSelected )
            //{
            //    if ( this.EmitReconstructedMessage && !SimCommon.IsCurrentlyRunningSimTurn )
            //    {
            //        this.EmitReconstructedMessage = false;

            //        Vector3 startLocation = this.GetPositionForCollisions();
            //        Vector3 endLocation = startLocation.PlusY( this.GetHalfHeightForCollisions() + 0.2f );
            //        DamageTextPopups.TextBuffer.AddLang( "Reconstructed", IconRefs.Reconstruction.DefaultColorHexWithHDRHex );
            //        DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer(
            //            startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "MajorActionPopup_LingerTime" ) ) );
            //        newDamageText.DamageIncluded = 0;
            //        newDamageText.SquadDeathsIncluded = 0;
            //    }
            //}

            #region Registration Check
            NPCCohort registration = this.CurrentRegistration;
            if ( registration != null )
                registration.DuringGame_DiscoverIfNeed();
            #endregion

            if ( this.IsTakingCover && 
                !( this.UnitType?.IsConsideredMech??false) ) //mechs "taking cover" are just having their shields up
            {
                ISimBuilding building = this.ContainerLocation.Get() as ISimBuilding;
                if ( building == null || !building.GetIsLocationStillValid() )
                {
                    this.IsTakingCover = false;
                    this.AddOrRemoveBadge( CommonRefs.TakingCover, false );
                }
            }

            MachineUnitStance stance = Stance;

            MachineUnitType unitType = this.UnitType;

            if ( unitType != null && unitType.DestroyIntersectingBuildingsStrength > 0 && ( !this.hasDoneStompCheckSinceCreationOrLoad ||
                (unitType.ShouldDestroyIntersectingBuildingsDuringMovement && isMoving ) ) &&
                (unitType.ShouldDestroyIntersectingBuildingsDuringMovement || !isMoving) )
            {
                if ( !this.isMoving ) //if we got here by moving, then keep doing it
                    this.hasDoneStompCheckSinceCreationOrLoad = true;

                #region If We Are Here To Destroy Buildings
                if ( !isMoving || !this.IsCloaked )
                {
                    staticWorkingBuildings.Clear();
                    CityMap.FillListOfBuildingsIntersectingCollidable( this, this.drawLocation,
                        this.objectRotation.eulerAngles.y, staticWorkingBuildings, false );

                    if ( staticWorkingBuildings.Count > 0 )
                    {
                        int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);
                        foreach ( MapItem bld in staticWorkingBuildings )
                        {
                            ISimBuilding building = bld?.SimBuilding;
                            if ( building == null )
                                continue;
                            if ( building?.GetStatus()?.ShouldBuildingBeNonColliding ?? false )
                                continue; //don't collide with invisible buildings
                            if ( bld.Type.ExtraPlaceableData.ResistanceToDestructionFromCollisions > unitType.DestroyIntersectingBuildingsStrength )
                                continue; //if this blocks being stepped on, then don't stop on it

                            int deaths = building.KillEveryoneHere();
                            if ( deaths > 0 )
                                CityStatisticRefs.MurdersByBuildingCollapse.AlterScore_CityAndMeta( deaths );

                            building.SetStatus( CommonRefs.DemolishedBuildingStatus );

                            bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                            if ( dropMoreParticles )
                            {
                                particleCount++;
                                ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( bld.OBBCache.BottomCenter,
                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                            }
                        }
                        staticWorkingBuildings.Clear();
                    }
                }
                #endregion
            }

            if ( unitType?.BlockUnitAndDeleteExisting?.DuringGameplay_IsTripped ?? false )
                this.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
        }

        #region DoPerFrameMovementLogic
        private void DoPerFrameMovementLogic()
        {
            MachineUnitType unitType = this.UnitType;
            if ( unitType != null && unitType.IsConsideredMech )
                this.moveProgress += (ArcenTime.SmoothUnpausedDeltaTime * 4 * InputCaching.MoveSpeed_PlayerMech) * Mathf.Max( 0.05f, unitType.MechStyleMovementSpeed );
            else
                this.moveProgress += (ArcenTime.SmoothUnpausedDeltaTime * 5 * InputCaching.MoveSpeed_PlayerAndroid);

            SimCommon.IsSomeUnitMoving.Construction = true;

            if ( this.moveProgress >= 1 )
            {
                this.isMoving = false;

                //if ( Engine_HotM.GameMode == MainGameMode.Streets && this.CurrentActionPoints == 0 && Engine_HotM.SelectedActor == this  )
                //    CameraCurrent.HaveFocusedOnNewTargetYet_Streets = false; //make the camera refocus on us here

                Vector3 targetFinalLoc = this.desiredNewContainerLocation != null ? this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit() : this.desiredNewGroundLocation;
                this.drawLocation = targetFinalLoc;

                ISimUnitLocation reachedContainerLocation = this.desiredNewContainerLocation;
                if ( reachedContainerLocation != null )
                {
                    if ( this.ContainerLocation.Get() is ISimMachineVehicle vehicle ) //if getting out of a vehicle
                    {
                        if ( this.UnitType.ScreenShake_OnExitingVehicle_Duration > 0 )
                            ScreenShake.StartShake( this.UnitType.ScreenShake_OnExitingVehicle_Duration,
                                this.UnitType.ScreenShake_OnExitingVehicle_Intensity,
                                this.UnitType.ScreenShake_OnExitingVehicle_DecreaseFactor );

                        this.UnitType.OnGetOutOfVehicleEnd.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );
                    }

                    this.SetActualContainerLocation( reachedContainerLocation );
                }
                else
                {
                    if ( this.ContainerLocation.Get() is ISimMachineVehicle vehicle ) //if getting out of a vehicle
                    {
                        if ( this.UnitType.ScreenShake_OnExitingVehicle_Duration > 0 )
                            ScreenShake.StartShake( this.UnitType.ScreenShake_OnExitingVehicle_Duration,
                                this.UnitType.ScreenShake_OnExitingVehicle_Intensity,
                                this.UnitType.ScreenShake_OnExitingVehicle_DecreaseFactor );

                        this.UnitType.OnGetOutOfVehicleEnd.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );
                    }
                    else //if regular move
                    {
                        if ( this.UnitType.ScreenShake_OnRegularMove_Duration > 0 )
                            ScreenShake.StartShake( this.UnitType.ScreenShake_OnRegularMove_Duration,
                                this.UnitType.ScreenShake_OnRegularMove_Intensity,
                                this.UnitType.ScreenShake_OnRegularMove_DecreaseFactor );

                        if ( this.afterMove_Action == null && this.UnitType.OnMovementFinishedWithNoAction != null )
                            this.UnitType.OnMovementFinishedWithNoAction.DuringGame_PlayAtLocation( this.GetDrawLocation(), this.objectRotation.eulerAngles );
                    }

                    #region If We Are Here To Destroy Buildings
                    if ( this.UnitType.DestroyIntersectingBuildingsStrength > 0 )
                    {
                        this.hasDoneStompCheckSinceCreationOrLoad = true;
                        staticWorkingBuildings.Clear();
                        CityMap.FillListOfBuildingsIntersectingCollidable( this, this.drawLocation,
                            this.objectRotation.eulerAngles.y, staticWorkingBuildings, false );

                        if ( staticWorkingBuildings.Count > 0 )
                        {
                            int particleCount = (FrameBufferManagerData.ParticleCount.Display + Engine_HotM.NonSim_TotalParticleEffectsPlayingNow);
                            foreach ( MapItem bld in staticWorkingBuildings )
                            {
                                if ( bld.SimBuilding == null )
                                    continue;
                                if ( bld.SimBuilding?.GetStatus()?.ShouldBuildingBeNonColliding ?? false )
                                    continue; //don't collide with invisible buildings
                                if ( bld.Type.ExtraPlaceableData.ResistanceToDestructionFromCollisions > unitType.DestroyIntersectingBuildingsStrength )
                                    continue; //if this blocks being stepped on, then don't stop on it

                                bld.SimBuilding.SetStatus( CommonRefs.DemolishedBuildingStatus );

                                bool dropMoreParticles = particleCount > 80 ? Engine_Universal.PermanentQualityRandom.Next( 0, 14400 ) > particleCount * particleCount : //120 squared
                                Engine_Universal.PermanentQualityRandom.Next( 0, 120 ) > particleCount;
                                if ( dropMoreParticles )
                                {
                                    particleCount++;
                                    ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( bld.OBBCache.BottomCenter,
                                        new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                                }
                            }
                            staticWorkingBuildings.Clear();
                        }
                    }
                    #endregion

                    this.SetActualGroundLocation( this.desiredNewGroundLocation );
                }

                if ( this.afterMove_Action != null )
                {
                    this.afterMove_Action.OnArrive.DuringGame_PlayAtLocation( this.drawLocation, this.objectRotation.eulerAngles );
                    if ( this.afterMove_Action.Implementation.TryHandleLocationAction( this, this.afterMove_BuildingOrNull, this.drawLocation,
                        this.afterMove_Action, this.afterMove_Event, this.afterMove_ProjectItem, this.afterMove_OtherOptionalID, ActionLogic.Execute, out _, 0 ) == ActionResult.Success )
                    {
                        postAbilityRand.ReinitializeWithSeed( this.CurrentTurnSeed + this.afterMove_Action.RowIndexNonSim );
                        this.afterMove_Action.DuringGameplay_ApplyActionStatistics( postAbilityRand );
                    }
                    this.ApplyVisibilityFromAction( this.afterMove_Action.VisibilityStyle );
                    if ( reachedContainerLocation != null && reachedContainerLocation is ISimMachineVehicle containerVehicle )
                    {
                        //do this so that the selection changes to the vehicle after a unit loads into a vehicle
                        Engine_HotM.SetSelectedActor( containerVehicle, false, true, true );
                    }
                }
                else
                {
                    if ( this.afterMove_MeleeCallback == null )
                        this.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                }
                if ( this.afterMove_MeleeCallback != null )
                {
                    this.afterMove_MeleeCallback();
                    this.ApplyVisibilityFromAction( ActionVisibility.IsMoveAndAttack );
                }
                this.afterMove_Action = null;
                this.afterMove_Event = null;
                this.afterMove_ProjectItem = null;
                this.afterMove_BuildingOrNull = null;
                this.afterMove_OtherOptionalID = string.Empty;

                this.afterMove_MeleeCallback = null;

                //make sure that things are newly-random after moving
                this.CurrentTurnSeed = Engine_Universal.PermanentQualityRandom.Next() + 1;

                this.HandleEnemyActionsAfterUnitMoveOrAct( true );
                return;
            }

            Vector3 targetLoc = this.desiredNewContainerLocation != null ? this.desiredNewContainerLocation.GetEffectiveWorldLocationForContainedUnit() : this.desiredNewGroundLocation;
            this.drawLocation = Vector3.Lerp( this.originalSourceLocation, targetLoc, this.moveProgress );

            if ( unitType != null && unitType.IsConsideredMech )
            {
                float lowY = -(this.GetHalfHeightForCollisions() * 1.5f) * unitType.MechStyleMovementDip;
                if ( this.moveProgress < 0.5f )
                {
                    float yShift = Mathf.Lerp( this.originalSourceLocation.y, lowY, this.moveProgress * 2f );
                    this.drawLocation.y = yShift;
                }
                else if ( this.moveProgress < 1f )
                {
                    float yShift = Mathf.Lerp( lowY, targetLoc.y, (this.moveProgress - 0.5f) * 2f );
                    this.drawLocation.y = yShift;
                }
            }
            else
            {
                float highY = (this.GetHalfHeightForCollisions() * 4.5f);
                if ( this.moveProgress < 0.5f )
                {
                    float yShift = Mathf.Lerp( 0, highY, this.moveProgress * 2f );
                    this.drawLocation.y += yShift;
                }
                else if ( this.moveProgress < 1f )
                {
                    float yShift = Mathf.Lerp( highY, 0, (this.moveProgress - 0.5f) * 2f );
                    this.drawLocation.y += yShift;
                }
            }

            this.RotateObjectToFace( this.originalSourceLocation, targetLoc, ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 80 ); //rotate over time
        }
        #endregion

        #region SetCustomUnitName
        /// <summary>
        /// We don't care about duplicates, as this is the doing of the player.  If they want duplicates, that is up to them.
        /// </summary>
        public void SetCustomUnitName( string Name )
        {
            this.UnitName = Name;
        }
        #endregion

        protected override void HandleAnyBitsFromBeingRenderedInAnyFashion()
        {
            if ( !SimCommon.IsCurrentlyRunningSimTurn )
            {
                if ( this.AlmostCompleteActionOverTime != null )
                    this.AlmostCompleteActionOverTime.DoActionFinalSuccessLogic( false, Engine_Universal.PermanentQualityRandom );
            }
        }

        #region MarkParticleLoopActiveFromMainFrame
        public void MarkParticleLoopActiveFromMainFrame( ActorParticleLoopReason Reason )
        {
            if ( Reason == null )
                return;

            if ( !this.UnitType.ParticleLoops.TryGetValue( Reason.ID, out SubParticleLoop loop ) )
            {
                ArcenDebugging.LogSingleLine( "Unit type '" + this.UnitType.ID + "' is not st up to have ActorParticleLoopReason '" + Reason.ID + "'!", Verbosity.ShowAsError );
                return;
            }

            if ( this.floatingParticleLoop == null || !this.floatingParticleLoop.GetIsValidToUse( this ) ||
                this.floatingParticleLoop.ParticleLoop != loop.Loop )
            {
                this.floatingParticleLoop = loop.Loop.ParticleLoopPool.GetFromPool( this );
                this.floatingParticleLoop.CollisionLayer = CollisionLayers.IconMixedIn;
            }

            this.floatingParticleLoop.LocalScale = loop.Scale;
            this.floatingParticleLoop.Rotation = this.GetDrawRotation();
            this.floatingParticleLoop.WorldLocation = this.GetDrawLocation() + loop.PositionOffset;
            this.floatingParticleLoop.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region DoPerFrameDrawBecauseExistsInOrOutOfCameraView
        public void DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, Int64 FramesPrepped, out bool TooltipShouldBeAtCursor, out bool ShouldSkipDrawing, out bool ShouldDrawAsSilhouette )
        {
            TooltipShouldBeAtCursor = false;
            ShouldSkipDrawing = false;
            ShouldDrawAsSilhouette = false; //not used for machine units
            IsMouseOver = false;

            if ( this.LastFramePrepRendered >= FramesPrepped )
            {
                ShouldSkipDrawing = true;
                return;
            }
            this.LastFramePrepRendered = FramesPrepped;

            MachineUnitType unitType = this.UnitType;
            if ( unitType == null )
            {
                ShouldSkipDrawing = true;
                return;
            }
            if ( this.IsFullDead )
            {
                ShouldSkipDrawing = true;
                return;
            }

            //if ( Engine_HotM.SelectedActor != this ) //if we are selected, then always draw
            //{
            //    this.CalculateMapCell
            //    if ( this.currentCell != null && !this.currentCell.IsConsideredInCameraView )
            //    {
            //        FrameBufferManagerData.CellFrustumCullCount.Construction++;
            //        return; //we're not drawing, because we are not actually in view
            //    }
            //}

            this.HandleAnyBitsFromBeingRenderedInAnyFashion();

            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;

            bool isInvisibleButDrawingCollisionBoxForActionOverTime = false;
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
            {
                ShouldSkipDrawing = true;
                this.Materializing = MaterializeType.Reveal;
                this.MaterializingProgress = 0;

                //if we're not on the city map, and we ARE doing an action over time, think about rendering some text even though we are invisible
                if ( !isMapView && this.CurrentActionOverTime != null && this.GetIsDeployed() )
                    isInvisibleButDrawingCollisionBoxForActionOverTime = true;
                else
                    return;
            }

            if ( isInvisibleButDrawingCollisionBoxForActionOverTime )
            { }
            else
            {
                if ( this.Materializing != MaterializeType.None && ArcenUI.CurrentlyShownWindowsWith_ShouldBlurBackgroundGame.Count == 0 &&
                    !VisCurrent.IsShowingActualEvent && FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped )
                {
                    this.MaterializingProgress += ArcenTime.SmoothUnpausedDeltaTime * this.Materializing.GetSpeed();
                    if ( this.MaterializingProgress >= 1.1f ) //go an extra 10%
                        this.Materializing = MaterializeType.None;
                }
            }

            {
                #region Floating LOD Object
                bool drawAggressive = (this.Stance?.ShouldShowAggressivePose ?? false) || (this.CurrentActionOverTime?.Type?.ShouldShowAggressivePose ?? false) ||
                    (this.AlmostCompleteActionOverTime?.Type?.ShouldShowAggressivePose ?? false) ||
                    this.TurnsSinceDidAttack < 3; //if have attacked in the last three turns, show aggressive stance
                VisLODDrawingObject toDraw = drawAggressive ? unitType.VisObjectAggressive : unitType.VisObjectCasual;

                IAutoPooledFloatingLODObject fObject = this.floatingLODObject;
                if ( fObject == null || !fObject.GetIsValidToUse( this ) ||
                    fObject.Object.ID != toDraw.ID ) //if we are switching between casual and aggressive stance
                {
                    if ( toDraw.FloatingObjectPool == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null FloatingObjectPool on VisSimpleDrawingObject '" + toDraw.ID +
                            "'! Be sure and set create_auto_pooled_floating_object=\"true\"!", Verbosity.ShowAsError );
                        return;
                    }
                    fObject = toDraw.FloatingObjectPool.GetFromPool( this );
                    this.floatingLODObject = fObject;
                    fObject.CollisionLayer = CollisionLayers.VehicleMixedIn;
                    fObject.ColliderScale = unitType.ColliderScale;
                    if ( !this.hasEverSetRotation )
                    {
                        this.hasEverSetRotation = true;
                        float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                        this.objectRotation = Quaternion.Euler( 0, y, 0 );
                    }
                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, 1000 ); //rotate correctly immediately

                }
                else
                {
                    if ( !this.isMoving ) //only do this passive rotation when we are not moving
                    {
                        if ( !this.hasEverSetRotation )
                        {
                            this.hasEverSetRotation = true;
                            float y = Engine_Universal.PermanentQualityRandom.Next( 0, 360 );
                            this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        }

                        #region Rotate To Mouse
                        if ( this.UnitType.IsConsideredAndroid && this.IsRotatingYNow ) //Androids rotate, but not mechs
                        {
                            float y = this.objectRotation.eulerAngles.y;
                            while ( y > 360f )
                                y -= 360f;
                            while ( y < 0f )
                                y += 360f;

                            y += 1000f;
                            float targetY = this.RotateToY + 1000f;
                            float diff = targetY - y;
                            if ( diff > 180f )
                                targetY -= 360f;
                            else if ( diff < -180f )
                                targetY += 360f;

                            bool rotateUp = false;
                            if ( y < targetY )
                                rotateUp = true;
                            if ( rotateUp )
                            {
                                y += (ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * MathRefs.AndroidCursorFollowSpeed.FloatMin);

                                if ( y >= targetY )
                                {
                                    y = targetY;
                                    this.IsRotatingYNow = false;
                                }
                            }
                            else
                            {
                                y -= (ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * MathRefs.AndroidCursorFollowSpeed.FloatMin);

                                if ( y <= targetY )
                                {
                                    y = targetY;
                                    this.IsRotatingYNow = false;
                                }
                            }
                            y -= 1000f;
                            this.objectRotation = Quaternion.Euler( 0, y, 0 );
                        }
                        #endregion
                    }

                    //this.RotateObjectToFace( EffectivePosition, Engine_HotM.MouseWorldLocation, ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 20f ); //rotate over time
                }

                if ( unitType.ColliderScale > 1f )
                {
                    if ( VisCurrent.IsInPhotoMode )
                        fObject.ColliderScale = 1f;
                    else
                    {
                        float roughDistanceFromCamera = (CameraCurrent.CameraBodyPosition - this.drawLocation).GetLargestAbsXZY();
                        if ( roughDistanceFromCamera < 4 )
                            fObject.ColliderScale = 1f;
                        else if ( roughDistanceFromCamera > 12 )
                            fObject.ColliderScale = unitType.ColliderScale;
                        else
                            fObject.ColliderScale = 1f + ( ( unitType.ColliderScale - 1f ) * ( (roughDistanceFromCamera- 4f) / 8f) );
                    }
                }

                bool isMapViewScaling = isMapView && (UnitType.IsConsideredAndroid || UnitType.RadiusForCollisions < 2f);

                fObject.ObjectScale = isMapViewScaling ? unitType.VisObjectScale * MathRefs.UnitExtraScaleOnCityMap.FloatMin : unitType.VisObjectScale;
                fObject.WorldLocation = this.drawLocation.PlusY( unitType.VisObjectExtraOffset + (isMapView ? unitType.VisObjectExtraOffsetOnCityMap : 0f) );
                fObject.MarkAsStillInUseThisFrame();
                fObject.Rotation = this.objectRotation;

                if ( fObject.IsMouseover )
                {
                    this.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    IsMouseOver = true;
                }
                #endregion
            }

            if ( InputCaching.ShowUnitHealthAndAboveHeadIcons )
            {
                //if we're not on the city map, and we ARE doing an action over time, think about rendering some text
                if ( !isMapView && this.CurrentActionOverTime != null && this.GetIsDeployed() )
                    this.HandleActionOverTimeText();

                if ( (this.floatingLODObject?.IsMouseover??false) || Engine_HotM.SelectedActor == this || 
                    (SimCommon.CurrentCityLens?.ShowHealthBars?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowHealthBars ?? false) || InputCaching.IsInInspectMode_ShowMoreStuff ||
                    (Engine_HotM.SelectedMachineActionMode?.ShowHealthBarsForYourUnits ?? false) )
                {
                    MapActorData health = this.actorData.GetValueOrInitializeIfMissing( ActorRefs.ActorHP );
                    float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
                    float scale = scaleMultiplier * 0.4f;
                    IconRefs.RenderHealthBar( health.Current, health.Maximum, this.GetActualPositionForMovementOrPlacement(), this.CurrentActionOverTime == null ? -0.4f : -0.8f, scaleMultiplier, scale,
                        true, null, false, this );
                }

                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                        {
                            Vector3 effectivePoint = this.drawLocation.PlusY( this.UnitType.HeightForCollisions + 0.2f );
                            if ( !this.TryDrawSelectedToActIcon( effectivePoint ) )
                                this.DrawNotSelectedIcon( effectivePoint );
                        }
                        break;
                }
            }
        }
        #endregion

        #region HandleActionOverTimeText
        private void HandleActionOverTimeText()
        {
            ActionOverTime aot = this.CurrentActionOverTime;

            if ( aot == null )
                return;
            ActionOverTimeType aotType = aot.Type;
            if ( aotType == null ) 
                return;

            //but only render the text if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - this.GetDrawLocation()).sqrMagnitude;
            if ( squareDistanceFromCamera <= 6400 ) //80 squared
            {
                if ( this.FloatingText != null && this.FloatingText.GetIsValidToUse( this ) )
                { } //already exists, so just update it
                else
                {
                    //if ( CurrentCycleTime.ElapsedMilliseconds < 4 ) //if it's been more than 4ms, then don't do more
                    {
                        //does not already exist, so establish it
                        this.FloatingText = CommonRefs.FloatingTextBasic.GetFromPool( this );
                        this.FloatingText.ObjectScale = 1f; //any other scale is likely blurry!
                    }
                }

                this.FloatingText.WorldLocation = this.GetDrawLocation();

                if ( this.FloatingText != null )
                {
                    float fontSize = 4f;
                    if ( squareDistanceFromCamera > 100 ) //10 squared
                    {
                        float squareDistanceAbove = squareDistanceFromCamera - 100;
                        if ( squareDistanceAbove >= 1600 ) //40 squared
                            fontSize *= 3f;
                        else
                            fontSize *= (1 + ((squareDistanceAbove / 1600f) * 2f));
                    }

                    this.FloatingText.FontSize = fontSize;
                    this.FloatingText.MarkAsStillInUseThisFrame();
                    try
                    {
                        ActionOverTimeResult aotResult = aotType.Implementation.TryHandleActionOverTime( aot, this.FloatingTextBuffer,
                        ActionOverTimeLogic.PredictFloatingText, null,
                        null, SideClamp.Any, TooltipExtraText.None, TooltipExtraRules.None );
                        if ( aotResult == ActionOverTimeResult.IntermediateSuccess )
                            this.FloatingText.Text = this.FloatingTextBuffer.GetStringAndResetForNextUpdate();
                        else
                        {
                            if ( aotResult == ActionOverTimeResult.Fail )
                                aot.DestroyAndCancelDueToFailureOfSomeKind();
                            this.FloatingText.Text = string.Empty;
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "MU-HandleActionOverTimeText error with '" + aotType.ID + "': " + e, Verbosity.ShowAsError );
                    }
                }
            }
        }
        #endregion

        public bool GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject fObject, out Color color )
        {
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || this.IsFullDead )
            {
                fObject = null;
                color = ColorMath.White;
                return false;
            }

            //now actually do the draw, as the above just handles colliders and any particle effects
            fObject = this.floatingLODObject;
            color = Color.white;
            //if ( !this.IsFullyFadedIn )
            //    color.a = this.AlphaAmountFromFadeIn;
            return fObject != null && fObject.GetIsValidToUse( this );
        }

        private const float ICON_HALF_HEIGHT = 0.9f;
        private const float ICON_MAP_POSITION_Y = 4f;

        #region RotateObjectToFace
        private void RotateObjectToFace( Vector3 effectivePosition, Vector3 otherPosition, float maxDelta )
        {
            if ( otherPosition.x == float.NegativeInfinity || otherPosition.y == float.NegativeInfinity )
                return;

            Vector3 targetLook = otherPosition - effectivePosition; //the sim is ahead of the vis location
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

                Quaternion oldRot = objectRotation;
                this.objectRotation = Quaternion.RotateTowards( this.objectRotation, lookRot, maxDelta );
                if ( this.objectRotation.x == float.NaN ) //undo if NaN result
                    this.objectRotation = oldRot;
            }
        }
        #endregion

        #region RotateAndroidToFacePoint
        public void RotateAndroidToFacePoint( Vector3 TargetPoint )
        {
            TargetPoint.y = drawLocation.y;
            Vector3 angle = drawLocation - TargetPoint;
            if ( angle.sqrMagnitude < 0.1f )
                return; //too close.  would give wrong results

            float targetY = MathA.AngleXZInDegrees( drawLocation, TargetPoint ) + 90f; //the extra 90 degrees is to get into the correct rotation frame

            while ( targetY > 360f )
                targetY -= 360f;

            while ( targetY < 0f )
                targetY += 360f;

            this.RotateToY = targetY;
            this.IsRotatingYNow = true;
        }
        #endregion

        protected override float GetIconScale()
        {
            return this.UnitType?.IconScale ?? 1f;
        }

        public override MobileActorTypeDuringGameData GetTypeDuringGameData()
        {
            return this.UnitType.DuringGameData;
        }

        #region Pooling
        private static int LastGlobalUnitIndex = 1;
        public readonly int GlobalUnitIndex;

        private static ReferenceTracker RefTracker;
        private MachineUnit()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "MachineUnits" );
            RefTracker.IncrementObjectCount();
            GlobalUnitIndex = Interlocked.Increment( ref LastGlobalUnitIndex );
        }

        private static readonly TimeBasedPool<MachineUnit> Pool = TimeBasedPool<MachineUnit>.Create_WillNeverBeGCed( "MachineUnit", 3, 20,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MachineUnit(); } );

        public static MachineUnit GetFromPoolOrCreate()
        {
            return Pool.GetFromPoolOrCreate();
        }
        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleNeeded()
        {
        }

        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
            this.ClearData(); //this is the main one
        }

        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOutgoingReferences()
        {
            this.ContainerLocation.Get()?.ClearOccupyingUnitIfThisOne( this );
            this.ContainerLocation = new WrapperedSimUnitLocation( null );

            SimCommon.AllActorsByID.TryRemove( this.UnitID, 5 );
            World_Forces.MachineUnitsByID.TryRemove( this.UnitID, 5 );
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool(); //when I am remove from a list, put me back in the pool
        }
        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        #region EqualsRelated
        public bool EqualsRelated( IAutoRelatedObject Other )
        {
            if ( Other is MachineUnit otherUnit )
                return this.GlobalUnitIndex == otherUnit.GlobalUnitIndex;
            return false;
        }
        #endregion

        public Vector3 GetPositionForCameraFocus()
        {
            return this.drawLocation.PlusY( Engine_HotM.SelectedActor == this ?
                (this.UnitType?.ExtraOffsetForCameraFocusWhenSelected ?? 0) : 0 );
        }

        public override Vector3 GetEmissionLocation()
        {
            return this.drawLocation;
        }

        public Vector3 GetActualPositionForMovementOrPlacement()
        {
            return this.ContainerLocation.Get()?.GetEffectiveWorldLocationForContainedUnit() ?? this.GroundLocation;
        }

        public float GetMovementRange()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;
            return range;
        }

        public float GetMovementRangeSquared()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;
            return range * range;
        }

        public float GetNPCRevealRange()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;
            if ( range < 10 )
                range = 10;
            if ( range > SimCommon.MaxNPCAttackRange )
                range = SimCommon.MaxNPCAttackRange;

            return range;
        }

        public float GetNPCRevealRangeSquared()
        {
            float range = this.ActorData[ActorRefs.ActorMoveRange]?.Current ?? 0;
            if ( range < 10 )
                range = 10;
            if ( range > SimCommon.MaxNPCAttackRange )
                return SimCommon.MaxNPCAttackRangeSquared;

            return range * range;
        }

        public ArcenDynamicTableRow GetTypeAsRow()
        {
            return this.UnitType;
        }

        public override IA5Sprite GetShapeIcon()
        {
            return this.UnitType?.ShapeIcon;
        }

        public string GetShapeIconColorString()
        {
            return string.Empty;
        }

        public override IA5Sprite GetTooltipIcon()
        {
            return this.UnitType?.TooltipIcon;
        }

        public override VisColorUsage GetTooltipIconColorStyle()
        {
            return this.UnitType?.TooltipIconColorStyle;
        }

        public string GetDisplayName()
        {
            return this.UnitName;
        }

        public string GetTypeName()
        {
            return this.UnitType?.GetDisplayName() ?? "[null unit type]";
        }

        public Vector3 GetPositionForCollisions()
        {
            return this.drawLocation.PlusY( this.UnitType?.HalfHeightForCollisions??0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
        }

        public Vector3 GetBottomCenterPosition()
        {
            return this.drawLocation;
        }

        public Vector3 GetCollisionCenter()
        {
            return this.drawLocation.PlusY( this.UnitType?.HalfHeightForCollisions??0 );
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            if ( this.UnitType.IsConsideredMech)
                return TheoreticalPoint.ReplaceY( this.UnitType?.HalfHeightForCollisions??0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 ); //the base offset is always 0 for mechs
            else
                return TheoreticalPoint.PlusY( this.UnitType?.HalfHeightForCollisions ?? 0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
        }

        public bool GetEquals( ICollidable other )
        {
            if ( other is MachineUnit unit )
                return unit.UnitID == this.UnitID;
            return false;
        }

        public string GetCollidableTypeID()
        {
            return this.UnitType?.ID ?? "[null machine unit]";
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return this.UnitType?.DestroyIntersectingBuildingsStrength??0;
        }

        public float GetRadiusForCollisions()
        {
            return this.UnitType.RadiusForCollisions;
        }

        public float GetSquaredRadiusForCollisions()
        {
            return this.UnitType.RadiusSquaredForCollisions;
        }

        public float GetHalfHeightForCollisions()
        {
            return this.UnitType.HalfHeightForCollisions;
        }

        public bool GetShouldHideIntersectingDecorations()
        {
            return this.UnitType.ShouldHideIntersectingDecorations;
        }

        public List<SubCollidable> GetSubCollidablesOrNull()
        {
            return this.UnitType.SubCollidables;
        }

        public float GetRotationYForCollisions()
        {
            return this.objectRotation.eulerAngles.y;
        }

        public float GetExtraRadiusBufferWhenTestingForNew()
        {
            return 0; //for now
        }

        public void SetRotationY( float YRot )
        {
            this.objectRotation = Quaternion.Euler( 0, YRot, 0 );
            this.hasEverSetRotation = true;
        }

        public bool ShouldFreezeCameraAtTheMoment => this.isMoving;

        public void DoOnSelectByPlayer()
        {
        }

        public void DoOnFocus_Streets()
        {
        }

        public void DoOnFocus_CityMap()
        {
        }

        private MersenneTwister workingRandomMainThreadOnlyShouldBeReset = new MersenneTwister( 0 );

        public int SortPriority
        {
            get
            {
                if ( !this.GetIsDeployed() )
                    return 300; //non-deployed units come later
                return 200; //deployed units still come after vehicles
            }
        }
        public int SortID => this.UnitID;

        public override int GetHashCode()
        {
            int hash = this.UnitID.GetHashCode();
            hash = HashCodeHelper.CombineHashCodes( hash, 250.GetHashCode() );
            return hash;
        }

        public override bool Equals( object obj )
        {
            if ( obj is MachineUnit unit )
                return unit.UnitID == this.UnitID;
            else
                return false;
        }
        public ListView<ISimBuilding> GetBoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob()
        {
            return ListView<ISimBuilding>.Create( this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList() );
        }

        public bool GetAmIAVeryLowPriorityTargetRightNow()
        {
            return false; //these are always regular priority!
        }

        #region RenderTooltip
        //BOOKMARK
        public override void RenderTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp, TooltipShadowStyle ShadowStyle, bool ShouldBeAtMouseCursor,
            ActorTooltipExtraData ExtraData, TooltipExtraRules ExtraRules )
        {
            MachineUnitType unitType = this.UnitType;
            if ( unitType == null )
                return;

            UnitStyleNovelTooltipBuffer usn = UnitStyleNovelTooltipBuffer.Instance;

            if ( usn.TryStartUnitStyleTooltip( ShouldBeAtMouseCursor || DrawNextTo != null,
                TooltipID.Create( this ), DrawNextTo, Clamp, ShadowStyle, TooltipExtraText.None, ExtraRules ) )
            {
                bool hasAnythingHyperDetailed = false;
                bool showNormalDetailed = InputCaching.ShouldShowDetailedTooltips;
                bool showHyperDetailed = InputCaching.ShouldShowHyperDetailedTooltips;
                bool showAnyDetailed = showNormalDetailed || showHyperDetailed;
                if ( showHyperDetailed )
                    showNormalDetailed = false;

                usn.Title.AddSpriteStyled_NoIndent( unitType.ShapeIcon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                usn.Title.AddRaw( this.UnitName );
                usn.PortraitIcon = this.GetTooltipIcon();
                usn.PortraitFrameColorHex = this.GetTooltipIconColorStyle()?.RelatedBorderColorHex ?? string.Empty;

                usn.UpperStats.StartUppercase().AddRaw( unitType.GetDisplayName(), ColorTheme.DataBlue ).EndUppercase().Line();
                usn.UpperStats.AddRaw( this.Stance?.GetDisplayName() ?? LangCommon.None.Text, ColorTheme.Gray ).Line();

                int effectiveClearanceLevel = this.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
                SecurityClearance effectiveClearance = this.CurrentBaseClearance;
                if ( effectiveClearanceLevel > 0 && (this.CurrentBaseClearance == null || this.CurrentBaseClearance.Level < effectiveClearanceLevel) )
                    effectiveClearance = SecurityClearanceTable.ByLevel[effectiveClearanceLevel];

                usn.LowerStats.AddLangAndAfterLineItemHeader( "SecurityClearance_Brief", ColorTheme.DataLabelWhite ).AddRaw(
                    effectiveClearance == null || effectiveClearance.Level <= 0 ? "-" : effectiveClearance.Level.ToString(), ColorTheme.DataBlue ).Line();

                if ( !unitType.GetDescription().IsEmpty() )
                    usn.Main.AddRaw( unitType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                if ( this.UnitType.IsConsideredMech )
                {
                    if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
                        usn.Main.AddFormat1( "TooManyMechsOnline", SimCommon.TotalCapacityUsed_Mechs - MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt, ColorTheme.RedOrange2 ).Line();
                }
                else
                {
                    if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                        usn.Main.AddFormat1( "TooManyAndroidsOnline", SimCommon.TotalCapacityUsed_Androids - MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt, ColorTheme.RedOrange2 ).Line();
                }

                if ( this.CurrentActionPoints <= 0 )
                    usn.Main.AddLang( "UnitIsOutOfActionPoints", ColorTheme.Gray ).Line();

                if ( this.UnitType.IsConsideredMech && showAnyDetailed && this.CurrentRegistration != null )
                    usn.Main.AddBoldLangAndAfterLineItemHeader( "Registration", ColorTheme.DataLabelWhite )
                        .AddRaw( this.CurrentRegistration == null ? LangCommon.None.Text : this.CurrentRegistration.GetDisplayName() ).Line();

                this.RenderDataLinesIntoNovelList( null, null, showAnyDetailed );

                int numberStatuses;
                if ( showHyperDetailed )
                {
                    numberStatuses = this.RenderStatusLinesIfAny( usn.Col2Body, true ) + this.AppendClearanceAndActionOverTimeAsIfWereStatuses( usn.Col2Body, false, true );
                    if ( numberStatuses > 0 )
                        usn.Col2Title.AddLang( "StatusEffects" );
                }
                else
                {
                    usn.FrameCBody.StartStyleLineHeightA();
                    numberStatuses = this.RenderStatusLinesIfAny( usn.FrameCBody, false ) + this.AppendClearanceAndActionOverTimeAsIfWereStatuses( usn.FrameCBody, showNormalDetailed, false );
                    //if ( numberStatuses > 0 )
                    //    usn.FrameCTitle.AddLang( "StatusEffects" );
                }

                if ( numberStatuses > 0 )
                    hasAnythingHyperDetailed = true;

                if ( this.GetHasAggroedAnyNPCCohort() )
                {
                    hasAnythingHyperDetailed = true;

                    ArcenDoubleCharacterBuffer titleToUse = usn.FrameBTitle;
                    ArcenDoubleCharacterBuffer bodyToUse = usn.FrameBBody;

                    if ( showHyperDetailed )
                    {
                        if ( numberStatuses > 0 )
                        {
                            titleToUse = usn.Col3Title;
                            bodyToUse = usn.Col3Body;
                        }
                        else
                        {
                            titleToUse = usn.Col2Title;
                            bodyToUse = usn.Col2Body;
                        }
                    }

                    bool isFirst = true;
                    int countSoFar = 0;
                    int extraCount = 0;
                    foreach ( NPCCohort cohort in NPCCohortTable.Instance.Rows )
                    {
                        int aggroAmount = this.GetAmountHasAggroedNPCCohort( cohort );
                        if ( aggroAmount > 0 )
                        {
                            if ( countSoFar > 9 && !showHyperDetailed )
                            {
                                extraCount++;
                                continue;
                            }

                            if ( isFirst )
                            {
                                isFirst = false;
                                titleToUse.AddLang( "AngeredCohorts" ).Line();
                            }
                            else
                                bodyToUse.ListSeparator();

                            if ( showHyperDetailed )
                                bodyToUse.AddRawAndAfterLineItemHeader( cohort.GetDisplayName(), ColorTheme.CannotAfford ).AddRaw( aggroAmount.ToStringWholeBasic() );
                            else
                                bodyToUse.AddRaw( cohort.GetDisplayName(), ColorTheme.CannotAfford );
                        }
                    }

                    if ( extraCount > 0 )
                    {
                        bodyToUse.ListSeparator();
                        bodyToUse.AddFormat1( "PlusXMore", extraCount, ColorTheme.CannotAfford );
                    }
                }

                ArcenDoubleCharacterBuffer lowerEffective = showHyperDetailed ? usn.FrameBBody : usn.LowerNarrative;

                string debugText = this.DebugText;                

                if ( this.IncomingDamage.Display.IncomingPhysicalDamageTargeting > 0 )
                {
                    hasAnythingHyperDetailed = true;
                    this.AppendIncomingDamageLines( lowerEffective, showHyperDetailed );
                }

                if ( !string.IsNullOrEmpty( debugText ) )
                    lowerEffective.AddNeverTranslatedAndAfterLineItemHeader( "DebugText", ColorTheme.AngRed_Dimmed, true ).AddRaw( debugText ).Line();

                switch ( ExtraData )
                {
                    case ActorTooltipExtraData.SelectFocus:
                        lowerEffective.StartStyleLineHeightA().StartSize90();
                        lowerEffective.AddFormat2( "ClickToSelect", Lang.GetLeftClickText(), this.GetDisplayName(), ColorTheme.SoftGold ).Line();
                        lowerEffective.AddFormat2( "ClickToFocusOn", Lang.GetRightClickText(), this.GetDisplayName(), ColorTheme.SoftGold ).Line();
                        break;
                    case ActorTooltipExtraData.FocusDestroy:
                        lowerEffective.StartSize80().StartColor( ColorTheme.SoftGold );
                        lowerEffective.AddFormat2( "ClickToFocusOn", Lang.GetRightClickText(), this.GetDisplayName() ).Line();
                        lowerEffective.AddFormat2( "ClickToDestroyUnit", Lang.GetLeftClickText(), this.GetDisplayName() );
                        break;
                }

                if ( hasAnythingHyperDetailed )
                    usn.CanExpand = HyperCanExpandType.Hyper;
                else
                {
                    if ( usn.CanExpand == HyperCanExpandType.No )
                        usn.CanExpand = HyperCanExpandType.NormalOnly;
                }
            }
        }
        #endregion

        protected override void GetIconInfo( out IA5Sprite Sprite, out ArcenPoint FrameColRow,
            out bool DrawFrameStyle, out Color Color, out float IconScale, IconLogic Logic )
        {
            //if ( InputCaching.ShowUnitPortraitsOnMapAndSidebar )
            //{
            //    DrawFrameStyle = true;
            //    Sprite = this.UnitType.TooltipIcon;
            //    FrameColRow = IconRefs.PlayerUnit_Portrait.FrameMaskRowCol;
            //    Color = IconRefs.PlayerUnit_Portrait.DefaultColorHDR;
            //    IconScale = IconRefs.PlayerUnit_Portrait.DefaultScale;
            //}
            //else
            {
                DrawFrameStyle = false;
                Sprite = this.UnitType.ShapeIcon;
                if ( this.CurrentStandby == StandbyType.None )
                {
                    FrameColRow = IconRefs.PlayerUnitReady_Icon.FrameMaskRowCol;
                    Color = IconRefs.PlayerUnitReady_Icon.DefaultColorHDR;
                    IconScale = IconRefs.PlayerUnitReady_Icon.DefaultScale;

                    VisIconUsage iconUsage = this.CurrentStandby != StandbyType.None ? IconRefs.MachineStandbyIcon : (
                        this.CurrentActionPoints <= 0 || ResourceRefs.MentalEnergy.Current <= 0 || this.CurrentActionOverTime != null ?
                        IconRefs.MachineUnreadyIcon : IconRefs.MachineReadyIcon);
                    Color = iconUsage.DefaultColorHDR;
                }
                else
                {
                    FrameColRow = IconRefs.PlayerUnitStandby_Icon.FrameMaskRowCol;
                    Color = IconRefs.PlayerUnitStandby_Icon.DefaultColorHDR;
                    IconScale = IconRefs.PlayerUnitStandby_Icon.DefaultScale;
                }
            }
        }

        #region GetCombinedFromChildrenActorDataCurrent
        public int GetCombinedFromChildrenActorDataCurrent( ActorDataType Type )
        {
            return 0; //these never have children!
        }

        public int GetCombinedFromChildrenActorDataCurrent( string TypeName )
        {
            return 0; //these never have children!
        }
        #endregion

        #region CalculateMapCell
        public override MapCell CalculateMapCell()
        {
            MapCell cell;
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
                cell = loc.GetLocationMapCell();
            else
                cell = CityMap.TryGetWorldCellAtCoordinates( this.GroundLocation );
            return cell;
        }
        #endregion

        #region CalculateMapDistrict
        public override MapDistrict CalculateMapDistrict()
        {
            MapCell cell = this.CalculateMapCell();
            return cell?.ParentTile?.District;
        }
        #endregion

        #region CalculatePOI
        public override MapPOI CalculatePOI()
        {
            ISimUnitLocation loc = this.ContainerLocation.Get();
            if ( loc != null )
                return loc.CalculateLocationPOI();
            else
            {
                MapCell cell = this.CalculateMapCell();
                return cell?.ParentTile?.GetPOIOfPoint( this.drawLocation );
            }
        }
        #endregion

        public bool GetMustStayOnGround()
        {
            return this.UnitType.IsConsideredMech;
        }

        public int GetPercentRobotic()
        {
            return 100;
        }

        public int GetPercentBiological()
        {
            return 0; //todo later: make cyborgs partly biological
        }

        public void PlayBulletHitEffectAtPositionForCollisions()
        {
            this.UnitType?.OnBulletHit?.DuringGame_PlayAtLocation( this.GetPositionForCollisions() );
        }

        public bool GetIsHuman()
        {
            return false;
        }

        #region ShotEmissionPoint
        public Vector3 GetTransformedShotEmissionPoint( ShotEmissionPoint Point )
        {
            return Point.CalculatePointFrom( this.GetPositionForCollisions(), this.GetRotationYForCollisions() );
        }
        public ProtectedList<ShotEmissionGroup> GetShotEmissionGroups()
        {
            return this.UnitType?.ShotEmissionGroups;
        }
        #endregion

        public bool GetShouldBeTargetForFriendlyAOEAbilities()
        {
            return true;
        }

        public bool GetWillFireOnMachineUnitsBaseline()
        {
            return false;
        }

        public override bool GetIsPlayerControlled()
        {
            return true;
        }

        public override bool GetIsPartOfPlayerForcesInAnyWay()
        {
            return true;
        }

        public override bool GetIsAnAllyFromThePlayerPerspective()
        {
            return true;
        }

        public override bool GetIsRelatedToPlayerShellCompany()
        {
            return this.UnitType?.IsTiedToShellCompany ?? false;
        }

        public override bool GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer()
        {
            return this.UnitType?.IsTiedToShellCompany ?? false;
        }

        public bool IsInvalid => this.UnitType == null || this.IsInPoolAtAll || this.IsFullDead;

        public bool CanMakeMeleeAttacks()
        {
            return this.UnitType.CanEverMakeMeleeAttacks;
        }

        public bool CanMakeRangedAttacks()
        {
            if ( !this.UnitType.CanEverMakeRangedAttacks )
                return false;
            if ( this.UnitType.CanMakeRangedAttacksByDefault )
                return true;

            foreach ( EquipmentEntry equipment in this.UnitType.DuringGameData.Equipment)
            {
                if ( equipment.CurrentType != null && equipment.CurrentType.IsRangedWeapon )
                    return true;
            }
            return false; //if no equipment enabled it, then the answer is no
        }

        public VisParticleAndSoundUsage GetMeleeAttackSoundAndParticles()
        {
            VisParticleAndSoundUsage meleeSound = null;
            int meleeSoundPriority = -1;
            foreach ( EquipmentEntry equipment in this.UnitType.DuringGameData.Equipment )
            {
                if ( equipment.CurrentType != null && equipment.CurrentType.IsMeleeWeapon )
                {
                    if ( equipment.CurrentType.OnAttack != null && equipment.CurrentType.OnAttackPriority > meleeSoundPriority )
                    {
                        meleeSound = equipment.CurrentType.OnAttack;
                        meleeSoundPriority = equipment.CurrentType.OnAttackPriority;
                    }
                }
            }

            if ( meleeSound != null )
                return meleeSound;

            return this.UnitType.OnStandardMeleeAttack;
        }

        public VisParticleAndSoundUsage GetRangedAttackSoundAndParticles()
        {
            VisParticleAndSoundUsage rangedSound = null;
            int rangedSoundPriority = -1;
            foreach ( EquipmentEntry equipment in this.UnitType.DuringGameData.Equipment )
            {
                if ( equipment.CurrentType != null && equipment.CurrentType.IsRangedWeapon )
                {
                    if ( equipment.CurrentType.OnAttack != null && equipment.CurrentType.OnAttackPriority > rangedSoundPriority )
                    {
                        rangedSound = equipment.CurrentType.OnAttack;
                        rangedSoundPriority = equipment.CurrentType.OnAttackPriority;
                    }
                }
            }

            if ( rangedSound != null )
                return rangedSound;

            return this.UnitType.OnStandardRangedAttack;
        }

        protected override bool GetIsAtABuilding()
        {
            return this.ContainerLocation.Get() is ISimBuilding;
        }

        public bool GetIsInActorCollection( ActorCollection Collection )
        {
            if ( Collection == null )
                return false;
            return this.UnitType?.Collections?.ContainsKey( Collection.ID ) ?? false;
        }

        #region GetIsUnremarkableRightNow_NotCountingActualClearanceChecks
        /// <summary>
        /// The portion that is the actual clearance check should be handled via GetEffectiveClearance
        /// </summary>
        private bool GetIsUnremarkableRightNow_NotCountingActualClearanceChecks( ClearanceCheckType CheckType )
        {
            if ( this.IsUnremarkableAnywhereUpToClearanceInt >= 0 )
                return true;

            switch ( CheckType )
            {
                case ClearanceCheckType.StayingThere:
                    {
                        if ( this.IsUnremarkableBuildingOnlyUpToClearanceInt >= 0 )
                            return this.ContainerLocation.Get() is ISimBuilding;
                    }
                    break;
                case ClearanceCheckType.MovingToBuilding:
                    {
                        if ( this.IsUnremarkableBuildingOnlyUpToClearanceInt >= 0 )
                            return true;
                    }
                    break;
                    //case ClearanceCheckType.MovingToNonBuilding: //nothing special to do here
                    //    break;
            }

            return false;
        }
        #endregion

        #region GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation
        public bool GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation( NPCCohort FromCohort, bool SkipInconspicuousCheck )
        {
            if ( !this.GetIsDeployed() || this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || this.IsFullDead || this.IsCloaked )
                return true;

            if ( SkipInconspicuousCheck )
                return false;
            if ( this.GetAmountHasAggroedNPCCohort( FromCohort ) > 0 )
                return false; //if we have aggroed that group, then no hiding from them

            if ( this.GetIsUnremarkableRightNow_NotCountingActualClearanceChecks( ClearanceCheckType.StayingThere ) )
            {
                int effectiveClearance = this.GetEffectiveClearance( ClearanceCheckType.StayingThere );
                if ( this.ContainerLocation.Get() is ISimBuilding Building )
                {
                    int clearanceInt = Building.CalculateLocationSecurityClearanceInt();
                    if ( clearanceInt > effectiveClearance )
                        return false; //this building has a higher security clearance than us, so shoot at us
                    return true; //we are in some other building, so don't shoot
                }
                else
                {
                    Vector3 drawLocation = this.GetDrawLocation();
                    MapTile tile = CityMap.TryGetWorldCellAtCoordinates( drawLocation )?.ParentTile;
                    if ( tile == null )
                        return true; //we are off the map somehow

                    foreach ( MapPOI poi in tile.RestrictedPOIs )
                    {
                        if ( poi == null || poi.HasBeenDestroyed )
                            continue;
                        ArcenFloatRectangle rect = poi.GetOuterRect();
                        if ( !rect.ContainsPointXZ( drawLocation ) )
                            continue; //destination not in here, so nevermind

                        SecurityClearance clearance = poi.Type.RequiredClearance;
                        if ( clearance != null && clearance.Level > effectiveClearance )
                            return false; //this poi has a higher security clearance than us, so shoot at us
                    }
                }

                return true;
            }
            else //shoot us, we're a scary machine!
                return false;
        }
        #endregion

        #region GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation
        public bool GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation( NPCCohort FromCohort, ISimBuilding BuildingOrNull, Vector3 Location, bool WillHaveDoneAttack, bool SkipInconspicuousCheck )
        {
            ClearanceCheckType clearanceCheck = BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding;

            return MachineActorHelper.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Unit( FromCohort, BuildingOrNull, Location, WillHaveDoneAttack, SkipInconspicuousCheck,
                this.IsCloaked, this.GetAmountHasAggroedNPCCohort( FromCohort ),
                this.GetIsUnremarkableRightNow_NotCountingActualClearanceChecks( clearanceCheck ),
                this.GetEffectiveClearance( clearanceCheck ) );
        }
        #endregion

        public bool GetIsValidToCatchInAreaOfEffectExplosion_Current( ISimMapActor Target )
        {
            return GetIsValidToAutomaticallyShootAt_SameLocation( Target, false, false );
        }

        public bool GetIsValidToAutomaticallyShootAt_Current( ISimMapActor Target )
        {
            return GetIsValidToAutomaticallyShootAt_SameLocation( Target, false, false );
        }

        public bool GetIsValidToAutomaticallyShootAt_SameLocation( ISimMapActor Target, bool WillBeShiftedToTakingCover, bool WillBeShiftedToCloaked )
        {
            if ( Target is ISimNPCUnit npcUnit )
            {
                if ( Target.IsFullDead )
                    return false;
                if ( Target.GetIsAnAllyFromThePlayerPerspective() )
                    return false;
                //for now this is enough
                if ( npcUnit.GetWillFireOnMachineUnitsBaseline() )
                {
                    if ( this.UnitType?.IsTiedToShellCompany??false )
                    {
                        //we are part of a shell company, and they are not after us, so we can't fire without giving ourselves away
                        if ( !(npcUnit.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false) )
                            return false;
                    }
                    else
                    {
                        //we are not part of a shell company, so cannot intervene without giving ourselves away
                        if ( npcUnit.Stance?.WillAttackShellCompanyMachinesEvenIfNotAggroed ?? false )
                            return false;
                    }

                    return true;
                }
            }
            else if ( Target is ISimMachineActor )
                return false;
            return false;
        }
    }
}

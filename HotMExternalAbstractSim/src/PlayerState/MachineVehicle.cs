using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.External
{
    internal class MachineVehicle : BaseMachineActor, ITimeBasedPoolable<MachineVehicle>, IProtectedListable, ISimMachineVehicle
	{
        //
        //Serialized data
        //-----------------------------------------------------
        public int VehicleID { get; private set; } = 0;
        public MachineVehicleType VehicleType { get; private set; }
        public int TurnsSinceMoved { get; private set; }
        public bool HasMovedThisTurn { get; private set; }
        public string VehicleName = string.Empty;
        private Vector3 worldLocation = Vector3.zero;
        private Quaternion objectRotation = Quaternion.identity;
        private Color chosenObjectColor = ColorMath.Transparent;
        public MachineVehicleStance Stance { get; set; }

        //
        //NonSerialized data
        //-----------------------------------------------------
        public string DebugText { get; set; } = string.Empty;
        public DamageTextPopups.FloatingDamageTextPopup MostRecentDamageText { get; set; } = null;
        private MapCell currentCell = null;
        private Int64 LastFramePrepRendered = -1;

        private Vector3 desiredLocation = Vector3.zero;
        private Vector3 originalSourceLocation = Vector3.zero;
        private float moveProgress = 0;
        private readonly List<ISimMachineUnit> StoredUnits = List<ISimMachineUnit>.Create_WillNeverBeGCed( 20, "MachineVehicle-StoredUnits" );

        public MaterializeType Materializing { get; set; } = MaterializeType.None;
        public float MaterializingProgress { get; set; } = 0;

        private IAutoPooledFloatingObject floatingObject;
        private readonly ParticleSpawner smokeSpawner = new ParticleSpawner();
        private IAutoPooledFloatingParticleLoop floatingParticleLoop;

        public FogOfWarCutter FogOfWarCutting { get; set; } //this is not really reset or anything later
        public NPCRevealer NPCRevealing { get; set; } //this is not really reset or anything later

        public readonly DoubleBufferedList<ISimBuilding> BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob = DoubleBufferedList<ISimBuilding>.Create_WillNeverBeGCed( 10, "MachineVehicle-BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob", 10 );

        protected override string DebugStringDescriptor => "machine vehicle";
        protected override string DebugStringID => this.VehicleType?.ID ?? "[null VehicleType]";

        public int ActorID { get { return this.VehicleID; } }

        private SidebarItemFromOther<ISimMachineVehicle> sidebarHeader_VehicleContainer = null; //don't bother cleaning this up on restart, it's fine
        public SidebarItemFromOther<ISimMachineVehicle> SidebarHeader_VehicleContainer
        {
            get => this.sidebarHeader_VehicleContainer;
            set => this.sidebarHeader_VehicleContainer = value;
        }

        protected override void ClearData_MachineTypeSpecific()
        {
            this.VehicleType = null;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = false;
            this.VehicleName = string.Empty;
            this.worldLocation = Vector3.zero;
            this.objectRotation = Quaternion.identity;
            this.chosenObjectColor = ColorMath.Transparent;
            this.Stance = null;

            this.DebugText = string.Empty;
            this.MostRecentDamageText = null;
            this.currentCell = null;
            this.LastFramePrepRendered = -1;

            this.desiredLocation = Vector3.zero;
            this.originalSourceLocation = Vector3.zero;
            this.moveProgress = 0;
            this.StoredUnits.Clear();

            this.Materializing = MaterializeType.None;
            this.MaterializingProgress = 0;

            if ( this.floatingObject != null )
            {
                //this.floatingObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingObject = null;
            }

            if ( this.floatingParticleLoop != null )
            {
                //this.floatingParticleLoop.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingParticleLoop = null;
            }

            //this.FogOfWarCutting; does not need a reset
            //this.NPCRevealing; does not need a reset
            BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearAllVersions();
        }

        #region CreateNew
        public static MachineVehicle CreateNew( MachineVehicleType VehicleType, Vector3 WorldPosition, int VehicleID, MersenneTwister Rand, bool Materialize, bool IsForDeserialize )
        {
            MachineVehicle vehicle = MachineVehicle.GetFromPoolOrCreate();
            vehicle.VehicleType = VehicleType;

            if ( VehicleID <= 0 )
                VehicleID = Interlocked.Increment( ref SimCommon.LastActorID );
            vehicle.VehicleID = VehicleID;

            vehicle.chosenObjectColor = VehicleType.VisObjectToDraw.RandomColorType == null ? Color.white :
                    VehicleType.VisObjectToDraw.RandomColorType.ColorList.GetRandom( Rand ).Color;
            vehicle.Stance = MachineVehicleStanceTable.BasicActiveStanceForVehicles;

            vehicle.SetWorldLocation( WorldPosition.ReplaceY( VehicleType.InitialHeight ) );

            if ( !IsForDeserialize )
            {
                vehicle.CurrentTurnSeed = Rand.Next() + 1;
                vehicle.StatCalculationRandSeed = Rand.Next() + 1;
                vehicle.InitializeActorData_Randomized( VehicleType.ID,  ActorDataTypeSet.PlayerVehicles, VehicleType.ActorData );
            }

            vehicle.DoPerSecondRecalculations( false ); //make sure any effective maximums are high enough
            if ( Materialize )
            {
                vehicle.Materializing = MaterializeType.Appear;
                vehicle.MaterializingProgress = 0;
            }
            return vehicle;
        }
        #endregion

        #region Serialize Vehicle 
        protected override void Serialize_MachineTypeTypeSpecific( ArcenFileSerializer Serializer )
        {
            Serializer.AddInt32( "ID", VehicleID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Type", this.VehicleType.ID );
			Serializer.AddVector3( "WorldLocation", worldLocation );
            Serializer.AddInt32IfGreaterThanZero( "TurnsSinceMoved", TurnsSinceMoved );
            Serializer.AddBoolIfTrue( "HasMovedThisTurn", this.HasMovedThisTurn );
            Serializer.AddUniqueString_UnicodeIfNotBlank( "VehicleName", this.VehicleName );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "Stance", this.Stance?.ID );

            Serializer.AddUniqueString_CondensedIfNotBlank( "Color", this.chosenObjectColor.GetHexCodeNoCaching() );
            Serializer.AddFloat( "RotationY", this.objectRotation.eulerAngles.y );
        }

        private DeserializedObjectLayer dataForLateDeserialize;
        public static MachineVehicle Deserialize( DeserializedObjectLayer Data )
        {
            MachineVehicleType vehicleType = Data.GetTableRow( "Type", MachineVehicleTypeTable.Instance, true );
            if ( vehicleType == null )
                return null;

            int vehicleID = Data.GetInt32( "ID", true );

            MachineVehicle vehicle = CreateNew( vehicleType, Vector3.zero, vehicleID, Engine_Universal.PermanentQualityRandom, false, true );
            vehicle.dataForLateDeserialize = Data;
            vehicle.SetWorldLocation( Data.GetUnityVector3( "WorldLocation", true ) ); //must be done BEFORE TurnsSinceMoved and HasMovedThisTurn
            vehicle.TurnsSinceMoved = Data.GetInt32( "TurnsSinceMoved", false );
            vehicle.HasMovedThisTurn = Data.GetBool( "HasMovedThisTurn", false );
            vehicle.VehicleName = Data.GetString( "VehicleName", false );
            vehicle.AssistInDeserialize_BaseMapActor_Primary( vehicle.VehicleType.ID, ActorDataTypeSet.PlayerVehicles, Data, vehicleType.ActorData, null );
            vehicle.AssistInDeserialize_BaseMachineActor_Primary( Data );

            if ( Data.TryGetTableRow( "Stance", MachineVehicleStanceTable.Instance, out MachineVehicleStance stance ) )
                vehicle.Stance = stance;
            else
                vehicle.Stance = MachineVehicleStanceTable.BasicActiveStanceForVehicles;

            if ( Data.ContainsAttribute( "Color" ) )
                vehicle.chosenObjectColor = ColorMath.HexToColor( Data.GetString( "Color", false ) );
            if ( Data.ContainsAttribute( "RotationY" ) )
                vehicle.objectRotation = Quaternion.Euler( 0, Data.GetFloat( "RotationY", false ), 0 );

            vehicle.DoPerSecondRecalculations( true ); //this will correct any effective maximums that are too low

            World_Forces.MachineVehiclesByID[vehicle.VehicleID] = vehicle;
            SimCommon.AllActorsByID[vehicle.VehicleID] = vehicle;

            CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                RefPair<ICollidable, Int64>.Create( vehicle, SimCommon.VisibilityGranterCycle ) );

            return vehicle;
        }

        public void FinishDeserializeLate()
        {            
            this.AssistInDeserialize_BaseMapActor_Late( dataForLateDeserialize );
            this.AssistInDeserialize_BaseMachineActor_Late( dataForLateDeserialize );
            this.dataForLateDeserialize = null;
        }
        #endregion

        public MapCell GetCurrentMapCell()
        {
            return this.currentCell;
        }

        public int GetInt32ValueForSerialization()
        {
            return this.VehicleID;
        }

        public override Vector3 GetDrawLocation()
        {
            return this.worldLocation.PlusY( this.VehicleType?.VisObjectExtraOffset??0 );
        }

        public Quaternion GetDrawRotation()
        {
            return this.objectRotation;
        }

        public override Vector3 GetEmissionLocation()
        {
            return this.worldLocation.PlusY( (this.VehicleType?.VisObjectExtraOffset??0) + (this.VehicleType?.HalfHeightForCollisions??0) );
        }

        public bool GetIsValidForCollisions()
        {
            return true;
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
            this.VehicleType.OnDeath.DuringGame_PlayAtLocation( this.GetDrawLocation() );

            CityStatisticTable.AlterScore( "CombatLossesVehicles", 1 );

            ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.MachineVehicleDied, NoteStyle.BothGame, this.VehicleType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                this.GetDisplayName(), string.Empty, string.Empty,
                SimCommon.Turn + 10 );

            for ( int i = StoredUnits.Count - 1; i >= 0; i-- )
                this.StoredUnits[i]?.KillFromCrash( this.GetDisplayName() );

            this.IsFullDead = true;

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            this.ReturnToPool(); //goodbye forever!
        }

        #region SpawnBurningItemAtCurrentLocation
        public void SpawnBurningItemAtCurrentLocation()
        {
            IAutoPooledFloatingObject fObject = this.floatingObject;
            if ( fObject != null )
            {
                VisSimpleDrawingObject toDraw = this.VehicleType?.VisObjectToDraw;
                if ( toDraw == null )
                    return;

                Vector3 position = fObject.WorldLocation;
                if ( position.x == 0 && position.z == 0 )
                    return;
                if ( float.IsNaN( position.x ) )
                    return;

                MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                materializingItem.Position = position;
                materializingItem.Rotation = fObject.Rotation;
                materializingItem.Scale = fObject.EffectiveScale;

                materializingItem.RendererGroup = toDraw.RendererGroup;
                materializingItem.Materializing = this.GetBurnDownType();

                MapEffectCoordinator.AddMaterializingItem( materializingItem );

                //this.floatingObject.ReturnToPool(); //do not try to do this, it just causes exceptions from background threads
                this.floatingObject = null;
            }
        }
        #endregion

        public MaterializeType GetBurnDownType()
        {
            return MaterializeType.BurnDownLarge;
        }

        public bool GetIsOverCap()
        {
            return this.VehicleType?.GetIsOverCap()??false;
        }

        #region TryScrapRightNowWithoutWarning_Danger
        // Must be kept the same as PopupReasonCannotScrapIfCannot
        public bool TryScrapRightNowWithoutWarning_Danger( ScrapReason Reason )
        {
            if ( !this.PopupReasonCannotScrapIfCannot( Reason ) )
                return false;

            this.HandleAnyMachineLogicOnDeath();

            bool skipVFX = false;
            switch (Reason)
            {
                case ScrapReason.Cheat:
                    CityStatisticTable.AlterScore( "ScrappedVehicles", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.MachineVehicleScrapped, NoteStyle.StoredGame, this.VehicleType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.CaughtInExplosion:
                    CityStatisticTable.AlterScore( "ExplosionLossesVehicles", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteRefs.MachineVehicleDiedInExplosion, NoteStyle.BothGame, this.VehicleType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.ReplacementByPlayer:
                    skipVFX = true;
                    break;
            }

            switch ( Reason )
            {
                case ScrapReason.ReplacementByPlayer:
                    skipVFX = true;
                    CityStatisticTable.AlterScore( "ScrappedVehicles", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.MachineVehicleReplaced, NoteStyle.StoredGame, this.VehicleType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.Cheat:
                    break; //handled above
                case ScrapReason.ArbitraryPlayer:
                    skipVFX = true;
                    CityStatisticTable.AlterScore( "ScrappedVehicles", 1 );
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteRefs.MachineVehicleScrapped, NoteStyle.StoredGame, this.VehicleType.ID, string.Empty, string.Empty, string.Empty, 0, 0, 0,
                        this.GetDisplayName(), string.Empty, string.Empty,
                        SimCommon.Turn + 10 );
                    break;
                case ScrapReason.CaughtInExplosion:
                    break; //handled above
                default:
                    ArcenDebugging.LogSingleLine( "Oops!  There was nothing set up to log the scrapping of a vehicle for reason '" + Reason + "'!", Verbosity.ShowAsError );
                    break;
            }

            this.SpawnBurningItemAtCurrentLocation();
            this.IsFullDead = true;
            if ( !skipVFX )
                this.VehicleType.OnDeath.DuringGame_PlayAtLocation( this.GetDrawLocation() );

            //disappear right away
            this.IsFullDead = true;

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;

            this.ReturnToPool();
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

            bool scrapNoMatterWhat = false;
            switch ( Reason )
            {
                case ScrapReason.Cheat:
                    scrapNoMatterWhat = true;
                    break;
                case ScrapReason.CaughtInExplosion:
                    scrapNoMatterWhat = true;
                    break;
            }

            for ( int i = StoredUnits.Count - 1; i >= 0; i-- )
            {
                if ( !StoredUnits[i].IsFullDead )
                {
                    if ( scrapNoMatterWhat )
                        StoredUnits[i].KillFromCrash( this.GetDisplayName() );
                    else
                    {
                        VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotScrapWithUnitsAboard_Header" ),
                            TooltipID.Create( "ScrapMachineActor", this.NonSimUniqueID, 0 ), TooltipWidth.Narrow, 2, 2f );

                        //ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        //    LocalizedString.AddLang_New( "CannotScrapWithUnitsAboard_Header" ),
                        //    LocalizedString.AddLang_New( "CannotScrapWithUnitsAboard_Body" ), LangCommon.Popup_Common_Ok.LocalizedString );
                        return false;
                    }
                }
            }

            return true;
        }
        #endregion

        #region Location Setting
        public Vector3 WorldLocation => this.worldLocation;
		public Quaternion WorldRotation => this.objectRotation;

        public void SetWorldLocation( Vector3 Vec )
        {
            this.worldLocation = Vec;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;
            if ( !this.isMoving ) //only set this when not moving, to not have intermediate cells set
            {
                this.currentCell = CityMap.TryGetWorldCellAtCoordinates( this.worldLocation );
                SimCommon.NeedsVisibilityGranterRecalculation = true;
                SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
            }
        }

        public void SetDesiredLocation( Vector3 desiredLoc )
        {
            if ( desiredLoc.x == float.NegativeInfinity || desiredLoc.x == float.PositiveInfinity )
                return; //invalid!

            float distance = ( desiredLoc - this.worldLocation ).magnitude;
            if ( distance > 10000 )
                return; //also something must be wrong

            this.desiredLocation = desiredLoc;
            this.originalSourceLocation = this.worldLocation;
            this.isMoving = true;
            this.moveProgress = 0;
            this.TurnsSinceMoved = 0;
            this.HasMovedThisTurn = true;
            CityStatisticTable.AlterScore( "VehicleMovements", 1 );

            this.RotateObjectToFace( this.desiredLocation, 1000 );
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

        public bool GetIsBlockedFromActingForMovementPurposes()
        {
            return GetIsBlockedFromActingInner( false, false, null );
        }

        public bool GetIsBlockedFromActingForTurnCompletePurposes()
        {
            return GetIsBlockedFromActingInner( true, false, null );
        }

        private bool GetIsBlockedFromActingInner( bool CareAboutActionPoints, bool CareAboutEnergy, AbilityType Ability )
        {
            if ( CareAboutActionPoints )
            {
                if ( this.CurrentActionPoints <= 0 )
                    return true;
            }

            if ( this.IsFullDead || !SimCommon.GetHaveAllNPCsActed()  )
                return true;

            if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt && !(Ability?.CanBeUsedEvenWhenOverUnitCap ?? false) )
                return true;

            if ( this.CurrentActionOverTime != null && !(Ability?.CanBeUsedEvenWhenDoingActionOverTime ?? false) )
                return true;

            if ( CareAboutEnergy )
            {
                if ( ResourceRefs.MentalEnergy.Current <= 0 )
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

            //this section is duplicating most of GetIsBlockedFromActing, but intentionally leaving out some bits
            if ( (CareAboutAP && this.CurrentActionPoints <= 0 ) || this.CurrentActionOverTime != null || this.IsFullDead )
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
                    return this.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false;
                case InvisibilityPurpose.ForCameraFocus:
                default:
                    return false;
            }
        }
        #endregion

        #region Ability Getting And Setting
        public override bool GetActorHasAbilityEquipped( AbilityType Ability )
        {
            return this.VehicleType?.DuringGameData?.GetActorHasAbilityEquipped( Ability ) ?? false;
        }

        public override AbilityType GetActorAbilityInSlotIndex( int SlotIndex )
        {
            return this.VehicleType?.DuringGameData?.GetActorAbilityInSlotIndex( SlotIndex );
        }

        public override bool GetActorAbilityCanBePerformedNow( AbilityType AbilityType, ArcenDoubleCharacterBuffer BufferOrNull )
        {
            if ( AbilityType == null )
                return false;

            if ( AbilityType.DuringGame_IsHiddenByFlags() )
                return false;

            if ( !AbilityType.CalculateIfActorCanUseThisAbilityRightNow( this, this.GetTypeDuringGameData(), BufferOrNull, true, false ) )
                return false;

            if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt && !AbilityType.CanBeUsedEvenWhenOverUnitCap )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddFormat1( "TooManyVehicles", SimCommon.TotalCapacityUsed_Vehicles - MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt );
                return false;
            }
            if ( this.CurrentActionOverTime != null && !AbilityType.CanBeUsedEvenWhenDoingActionOverTime )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.AddLang( "UnitBusyWithActionOverTime" );
                return false;
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

            ActorAbilityResult result = AbilityType.Implementation.TryHandleAbility( this, null, this.worldLocation, AbilityType, BufferOrNull,
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

            //if we got to this point, we can actually do the ability
            ActorAbilityResult result = ability.Implementation.TryHandleAbility( this, null, this.worldLocation, ability, null,
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
                        ability.OnUse.DuringGame_PlayAtLocation( this.worldLocation );
                    if ( ability.ActionPointCost > 0 )
                        this.AlterCurrentActionPoints( -ability.ActionPointCost );
                    if ( ability.MentalEnergyCost > 0 )
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -ability.MentalEnergyCost, "Expense_UnitAbilities", ResourceAddRule.IgnoreUntilTurnChange );
                    this.HandleEnemyActionsAfterUnitMoveOrAct( true );
                    return true;
                case ActorAbilityResult.OpenedInterface:
                    if ( !DoSilently )
                    {
                        //play the on-use sound but don't do anything like apply the statistics and personality changes
                        ability.OnUse.DuringGame_PlayAtLocation( this.worldLocation );
                    }
                    return true;
                case ActorAbilityResult.StartedActionOverTime:
                    if ( !DoSilently )
                    {
                        //play the on-use sound but don't do anything like apply the statistics and personality changes
                        ability.OnUse.DuringGame_PlayAtLocation( this.worldLocation );
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

            ability.Implementation.TryHandleAbility( this, null, this.worldLocation, ability, null,
                 ActorAbilityLogic.TriggerAbilityAltView );
        }
        #endregion

        #region GetIsCurrentStanceInvalid
        public bool GetIsCurrentStanceInvalid()
        {
            return false;
        }
        #endregion

        #region HandleVehiclePerTurnFairlyEarly
        public void HandleVehiclePerTurnFairlyEarly( MersenneTwister RandForThisTurn )
        {
            this.HandlePerTurnMachineActorLogic();
            this.PerTurnHandleBaseFairlyEarlyActorLogic( RandForThisTurn );

            if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) <= 0 )
            {
                this.DoDeathCheck( RandForThisTurn, false );
                return;
            }

            bool doActionOverTime = this.CurrentActionPoints > 0;
            int apRemainingAfterLastTurn = this.CurrentActionPoints;

            this.SetCurrentActionPoints( this.MaxActionPoints );
            this.CurrentTurnSeed = Engine_Universal.PermanentQualityRandom.Next() + 1;
            this.TurnsSinceMoved++;
            this.HasMovedThisTurn = false;

            if ( this.CurrentActionOverTime != null && doActionOverTime )
            {
                apRemainingAfterLastTurn = 0;
                if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) > 0 )
                {
                    this.IsScheduledToDoActionOverTime = true;
                    //this.CurrentActionOverTime.DoActionPerTurnLogic( RandForThisTurn ); //unit is still alive, so do the thing
                }
                else
                    this.CurrentActionOverTime.DestroyAndCancelDueToFailureOfSomeKind(); //the failure is that the unit is dead
            }

            this.Stance?.Implementation.HandleLogicForVehicleInStance( this, this.Stance, RandForThisTurn, apRemainingAfterLastTurn, null, VehicleStanceLogic.PerTurnLogic );

            #region Discover Cell Things
            MapCell cell = this.currentCell;
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

        #region HandleVehiclePerTurnVeryLate
        public void HandleVehiclePerTurnVeryLate( MersenneTwister RandForThisTurn )
        {
            this.PerTurnHandleBaseActorVeryLateLogic( RandForThisTurn );
        }
        #endregion

        public void DoPerSecondRecalculations( bool IsFromDeserialization )
        {
            this.RecalculateActorStats( IsFromDeserialization );
            this.RecalculatePerksPerSecond( this.VehicleType.DuringGameData.Equipment, this.VehicleType.DefaultPerks, this.Stance?.PerksGranted, this.Stance?.PerksBlocked );
        }

        protected override void RecalculateActorStats( bool IsFromDeserialization )
        {
            MachineVehicleType type = this.VehicleType;
            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            foreach ( KeyValuePair<ActorDataType, MapActorData> kv in this.actorData )
            {
                if ( kv.Value == null )
                    continue;
                kv.Value.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null, this.VehicleType.DuringGameData.Equipment, boostingStructures,
                    this.StoredUnits, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses, null, null,
                    type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(), type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(),
                    this.BadgeDict, null, null, IsFromDeserialization, null, -1f, -1f, 1, 1, -1f, 0, 0, false, this.VehicleType.ID );
            }
        }

        public void AppendExtraDetailsForDataType( ArcenDoubleCharacterBuffer Buffer, MapActorData DataData )
        {
            if ( DataData == null || Buffer == null ) 
                return;
            MachineVehicleType type = this.VehicleType;
            List<ISimBuilding> boostingStructures = this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList();
            DataData.RecalculateEffectiveValues_ForMapActorData( type.DataFlatUpgrades, type.DataMultiplicativeUpgrades, null, null, this.VehicleType.DuringGameData.Equipment, boostingStructures, 
                this.StoredUnits, this.DataChangesToMaximumsFromStatuses, this.DataMultipliersToMaximumsFromStatuses, null, null,
                type.DuringGameData.DirectStatAdditions, type.DuringGameData.DealStatAdditions.GetDisplayDict(), type.DuringGameData.CollectionsWithUpgrades.GetDisplayList(),
                this.BadgeDict, null, null, false, Buffer, -1f, -1f, 1, 1, -1f, 0, 0, false, this.VehicleType.ID );
        }

        public void DoPerFrameLogic()
        {
            this.PerFrameValidateActorData();

            bool weAreSelected = this == Engine_HotM.SelectedActor;

            #region Fix Missing Names
            if ( this.VehicleName.Length <= 0 )
            {
                this.VehicleName = this.VehicleType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineVehicle(
                    this.VehicleType.NameStyle, this, Engine_Universal.PermanentQualityRandom );
            }
            #endregion

            #region Registration Check
            NPCCohort registration = this.CurrentRegistration;
            if ( registration != null )
                registration.DuringGame_DiscoverIfNeed();
            #endregion

            if ( this.IsScheduledToDoActionOverTime )
            {
                if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count == 0 && SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count == 0 && 
                    !this.GetWouldBeDeadFromIncomingPhysicalDamageActual() && !SimCommon.IsCurrentlyRunningSimTurn )
                {
                    if ( this.GetActorDataCurrent( ActorRefs.ActorHP, true ) <= 0 )
                    {
                        this.DoDeathCheck( Engine_Universal.PermanentQualityRandom, false );
                        return;
                    }

                    this.CurrentActionOverTime.DoActionPerTurnLogic( Engine_Universal.PermanentQualityRandom );
                    this.IsScheduledToDoActionOverTime = false;
                }
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

            this.GetIsCurrentStanceInvalid();

            if ( !this.isMoving )
                return;

            if ( smokeSpawner.smoke2DTrailObject == null )
            {
                smokeSpawner.smoke2DTrailObject = VisSimpleDrawingObjectTable.Instance.GetRowByID( "SmokePuff" );
                smokeSpawner.smoke3DTrailObject = VisSimpleDrawingObjectTable.Instance.GetRowByID( "3DLightCloud2" );
            }

            SimCommon.IsSomeUnitMoving.Construction = true;

            this.moveProgress += ( ArcenTime.SmoothUnpausedDeltaTime * this.VehicleType.MovementSpeedMultiplier * InputCaching.MoveSpeed_PlayerVehicle);
            if ( this.moveProgress >= 1 )
            {
                this.isMoving = false;
                this.SetWorldLocation( this.desiredLocation );
                if ( this.currentCell != null && this.currentCell.ParentTile != null )
                {
                    this.currentCell.ParentTile.HasEverBeenExplored = true;
                    this.currentCell.ParentTile.IsTileContainingMachineActors.SetBothAtOnce( true );
                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                    SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                }

                //make sure that things are newly-random after moving
                this.CurrentTurnSeed = Engine_Universal.PermanentQualityRandom.Next() + 1;

                //if ( Engine_HotM.GameMode == MainGameMode.Streets && this.CurrentActionPoints == 0 )
                //    CameraCurrent.HaveFocusedOnNewTargetYet_Streets = false; //make the camera refocus on us here

                this.HandleEnemyActionsAfterUnitMoveOrAct( true );
                return;
            }

            Vector3 intermediateSpot = Vector3.Lerp( this.originalSourceLocation, this.desiredLocation, this.moveProgress );
            this.SetWorldLocation( intermediateSpot );

            //if ( this.moveProgress < 0.8f )
            //    this.RotateObjectToFace( this.desiredLocation, ArcenTime.SmoothUnpausedDeltaTime * 4f );

            //we want to drop a smoke trail object, but we want that to be managed by the actual particle system, not ourselves
            //only drop 80 per second, and have them last for TTL seconds
            //here's a version that drops two 3D objects instead
            smokeSpawner.Spawn3DSmokeStyleParticleAtCurrentLocation( intermediateSpot.PlusY( this.VehicleType.SmokeAddedY ), 
                this.VehicleType.SmokeColor1, this.VehicleType.SmokeColor2, 40,
                0.0125f, this.VehicleType.SmokeTTL, Vector3.one * this.VehicleType.SmokeScale1, Vector3.one * this.VehicleType.SmokeScale2,
                this.VehicleType.SmokeScaleGrowth, this.VehicleType.SmokePositionJitter );
        }

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

            if ( !this.VehicleType.ParticleLoops.TryGetValue( Reason.ID, out SubParticleLoop loop ) )
            {
                ArcenDebugging.LogSingleLine( "Vehicle type '" + this.VehicleType.ID + "' is not st up to have ActorParticleLoopReason '" + Reason.ID + "'!", Verbosity.ShowAsError );
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
        public void DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, Int64 FramesPrepped )
        {
            IsMouseOver = false;
            if ( this.LastFramePrepRendered >= FramesPrepped )
                return;
            this.LastFramePrepRendered = FramesPrepped;

            MachineVehicleType vehicleType = this.VehicleType;
            if ( vehicleType == null )
                return;

            if ( this.Materializing != MaterializeType.None && ArcenUI.CurrentlyShownWindowsWith_ShouldBlurBackgroundGame.Count == 0 &&
                !VisCurrent.IsShowingActualEvent && FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped )
            {
                this.MaterializingProgress += ArcenTime.SmoothUnpausedDeltaTime * this.Materializing.GetSpeed();
                if ( this.MaterializingProgress >= 1.1f ) //go an extra 10%
                    this.Materializing = MaterializeType.None;
            }

            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
            {
                this.Materializing = MaterializeType.Reveal;
                this.MaterializingProgress = 0;
                return;
            }

            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;

            //if ( Engine_HotM.SelectedActor != this ) //if we are selected, then always draw
            //{
            //    if ( this.currentCell != null && !this.currentCell.IsConsideredInCameraView )
            //    {
            //        FrameBufferManagerData.CellFrustumCullCount.Construction++;
            //        return; //we're not drawing, because we are not actually in view
            //    }
            //}

            this.HandleAnyBitsFromBeingRenderedInAnyFashion();

            Vector3 drawLoc;
            {
                #region Floating Object
                IAutoPooledFloatingObject fObject = this.floatingObject;
                if ( fObject == null || !fObject.GetIsValidToUse( this ) )
                {
                    if ( vehicleType.VisObjectToDraw.FloatingObjectPool == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null FloatingObjectPool on VisSimpleDrawingObject '" + vehicleType.VisObjectToDraw.ID +
                            "'! Be sure and set create_auto_pooled_floating_object=\"true\"!", Verbosity.ShowAsError );
                        return;
                    }
                    fObject = vehicleType.VisObjectToDraw.FloatingObjectPool.GetFromPool( this );
                    this.floatingObject = fObject;
                    fObject.CollisionLayer = CollisionLayers.VehicleMixedIn;
                    fObject.ColliderScale = vehicleType.ColliderScale;
                    //this.RotateObjectToFace( 1000 ); //rotate correctly immediately

                }
                else
                {
                    //this.RotateObjectToFace( ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * 80 ); //rotate over time
                }

                drawLoc = worldLocation.PlusY( vehicleType.VisObjectExtraOffset + vehicleType.ExtraOffsetForIconAndObject +
                    (isMapView ? MathRefs.VehicleExtraYOffsetOnCityMap.FloatMin : 0f) );

                if ( vehicleType.ColliderScale > 1f )
                {
                    if ( VisCurrent.IsInPhotoMode )
                        fObject.ColliderScale = 1f;
                    else
                    {
                        float roughDistanceFromCamera = (CameraCurrent.CameraBodyPosition - drawLoc).GetLargestAbsXZY();
                        if ( roughDistanceFromCamera < 4 )
                            fObject.ColliderScale = 1f;
                        else if ( roughDistanceFromCamera > 12 )
                            fObject.ColliderScale = vehicleType.ColliderScale;
                        else
                            fObject.ColliderScale = 1f + ((vehicleType.ColliderScale - 1f) * ((roughDistanceFromCamera - 4f) / 8f));
                    }
                }

                fObject.ObjectScale = isMapView ? vehicleType.VisObjectScale * MathRefs.VehicleExtraScaleOnCityMap.FloatMin : vehicleType.VisObjectScale;
                fObject.WorldLocation = drawLoc;
                fObject.MarkAsStillInUseThisFrame();
                fObject.Rotation = this.objectRotation;

                if ( fObject.IsMouseover )
                {
                    IsMouseOver = true;
                    this.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                }
                #endregion
            }

            if ( InputCaching.ShowUnitHealthAndAboveHeadIcons )
            {
                //if we're not on the city map, and we ARE doing an action over time, think about rendering some text
                if ( !isMapView && this.CurrentActionOverTime != null )
                    this.HandleActionOverTimeText();

                if ( (this.floatingObject?.IsMouseover??false) || Engine_HotM.SelectedActor == this || 
                    (SimCommon.CurrentCityLens?.ShowHealthBars?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowHealthBars ?? false) || InputCaching.IsInInspectMode_ShowMoreStuff ||
                    (Engine_HotM.SelectedMachineActionMode?.ShowHealthBarsForYourUnits ?? false) )
                {
                    MapActorData health = this.actorData.GetValueOrInitializeIfMissing( ActorRefs.ActorHP );
                    float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
                    float scale = scaleMultiplier * 0.4f;
                    IconRefs.RenderHealthBar( health.Current, health.Maximum, this.GetActualPositionForMovementOrPlacement(), this.CurrentActionOverTime == null ? -0.4f : - 0.8f, scaleMultiplier, scale,
                        true, null, false, this );
                }

                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                        {
                            Vector3 effectivePoint = drawLoc.PlusY( -this.VehicleType.ExtraOffsetForIconAndObject + this.VehicleType.HeightForCollisions + 0.1f );
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
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - this.worldLocation).sqrMagnitude;
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
                        this.FloatingText.WorldLocation = this.worldLocation;// item.OBBCache.OBB.Center;
                    }
                }

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
                        ArcenDebugging.LogSingleLine( "MV-HandleActionOverTimeText error with '" + aotType.ID + "': " + e, Verbosity.ShowAsError );
                    }
                }
            }
        }
        #endregion

        protected override float GetIconScale()
        {
            return 0.7f; //for some reason this is always constant for vehicles.  Can change later if needed.
        }

        public override MobileActorTypeDuringGameData GetTypeDuringGameData()
        {
            return this.VehicleType.DuringGameData;
        }

        #region SetCustomVehicleName
        /// <summary>
        /// We don't care about duplicates, as this is the doing of the player.  If they want duplicates, that is up to them.
        /// </summary>
        public void SetCustomVehicleName( string Name )
        {
            this.VehicleName = Name;
        }
        #endregion

        public bool GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingObj, out Color color )
        {
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
            {
                floatingObj = null;
                color = ColorMath.White;
                return false;
            }

            //now actually do the draw, as the above just handles colliders and any particle effects
            floatingObj = this.floatingObject;
            color = this.chosenObjectColor;
            return floatingObj != null && floatingObj.GetIsValidToUse( this );
        }

        private const float ICON_HALF_HEIGHT = 0.9f;

        #region RotateObjectToFace
        private void RotateObjectToFace( Vector3 otherPosition, float maxDelta )
        {
            Vector3 targetLook = otherPosition - this.worldLocation; //the sim is ahead of the vis location
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

        #region TryCreateNewMachineUnitInThisVehicleOrAsCloseAsPossibleToIt
        public ISimMachineUnit TryCreateNewMachineUnitInThisVehicleOrAsCloseAsPossibleToIt( MachineUnitType UnitTypeToGrant, string NewUnitOverridingName, CellRange CRange, 
            MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( UnitTypeToGrant == null )
                return null;

            if ( this.CanThisContainUnit( UnitTypeToGrant, null ) )
            {
                ISimMachineUnit unit = World.Forces.CreateNewMachineUnitInVehicle( UnitTypeToGrant, this, Rand, NewUnitOverridingName, StartAsReadyToAct );
                if ( unit != null )
                    return unit;
            }

            Vector3 vehicleSpot = this.worldLocation.ReplaceY( 0 );
            MapCell cell = this.currentCell;

            return World.Forces.TryCreateNewMachineUnitAsCloseAsPossibleToLocation( vehicleSpot, cell, UnitTypeToGrant, NewUnitOverridingName, CRange, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
        }
        #endregion

        #region Pooling
        private static int LastGlobalVehicleIndex = 1;
        public readonly int GlobalVehicleIndex;
        private static ReferenceTracker RefTracker;
        private MachineVehicle()
        {
            if (RefTracker == null)
                RefTracker = new ReferenceTracker("MachineVehicles");
            RefTracker.IncrementObjectCount();
            GlobalVehicleIndex = Interlocked.Increment( ref LastGlobalVehicleIndex );
        }

        private static readonly TimeBasedPool<MachineVehicle> Pool = TimeBasedPool<MachineVehicle>.Create_WillNeverBeGCed( "MachineVehicle", 3, 20,
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new MachineVehicle(); } );

        public static MachineVehicle GetFromPoolOrCreate()
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
            if ( Engine_HotM.SelectedActor == this )
                Engine_HotM.SetSelectedActor( null, false, false, false );

            for ( int i = StoredUnits.Count - 1; i >= 0; i-- )
                (this.StoredUnits[i] as MachineUnit)?.ReturnToPool();

            World_Forces.MachineVehiclesByID.TryRemove( this.VehicleID, 5 );
            SimCommon.AllActorsByID.TryRemove( this.VehicleID, 5 );
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
            if ( Other is MachineVehicle otherVehicle )
                return this.GlobalVehicleIndex == otherVehicle.GlobalVehicleIndex;
            return false;
        }
        #endregion

        #region CanThisContainUnit
        public bool CanThisContainUnit( MachineUnitType unit, ArcenCharacterBufferBase BufferOrNull )
        {
            if ( unit == null ) 
                return false;

            if ( unit.IsTiedToShellCompany )
            {
                if ( !(this.VehicleType?.IsTiedToShellCompany??false) )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.StartSize90().AddLang( "VehicleCannotCarryThisTypeOfUnit_Short", ColorTheme.RedOrange2 ).EndSize();
                    return false; //in other words, we NEVER can carry this
                }
            }
            else
            {
                if ( this.VehicleType?.IsTiedToShellCompany ?? false )
                {
                    if ( BufferOrNull != null )
                        BufferOrNull.StartSize90().AddLang( "VehicleCannotCarryThisTypeOfUnit_Short", ColorTheme.RedOrange2 ).EndSize();
                    return false; //in other words, we NEVER can carry this
                }
            }

            MachineUnitStorageSlotType slotType = unit.StorageSlotType;
            int baseSlots = this.VehicleType.UnitStorageSlotCounts[slotType];
            if ( baseSlots <= 0 )
            {
                if ( BufferOrNull != null )
                    BufferOrNull.StartSize90().AddLang( "VehicleCannotCarryThisTypeOfUnit_Short", ColorTheme.RedOrange2 ).EndSize();
                return false; //in other words, we NEVER can carry this
            }

            foreach ( MachineUnit otherUnit in this.StoredUnits )
            {
                if ( otherUnit.UnitType.StorageSlotType == slotType )
                {
                    baseSlots--;
                    if ( baseSlots <= 0 )
                    {
                        if ( BufferOrNull != null )
                            BufferOrNull.StartSize90().AddLang( "VehicleNoRoomForUnitsOfThisType_Short", ColorTheme.RedOrange2 ).EndSize();
                        return false;
                    }
                }
            }

            return true;
        }
        #endregion

        #region GetRemainingUnitSlotsOfType
        public int GetRemainingUnitSlotsOfType( MachineUnitStorageSlotType SlotType )
        {
            int baseSlots = this.VehicleType.UnitStorageSlotCounts[SlotType];
            if ( baseSlots <= 0 )
                return 0; //in other words, we NEVER can carry this

            foreach ( MachineUnit otherUnit in this.StoredUnits )
            {
                if ( otherUnit.UnitType.StorageSlotType == SlotType )
                {
                    baseSlots--;
                    if ( baseSlots <= 0 )
                        return 0;
                }
            }

            return baseSlots;
        }
        #endregion

        public ListView<ISimBuilding> GetBoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob()
        {
            return ListView<ISimBuilding>.Create( this.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.GetDisplayList() );
        }

        public Vector3 GetPositionForCameraFocus()
        {
            return this.worldLocation.PlusY( this.VehicleType.AddedYForCameraFocus );
        }

        public Vector3 GetActualPositionForMovementOrPlacement()
        {
            return this.worldLocation;
        }

        public float GetNPCRevealRange()
        {
            float range = this.ActorData[ActorRefs.AttackRange]?.Current ?? 0;
            if ( range < 10 )
                range = 10;
            range += range; //for now, machine vehicles just give a flat double of their scan range

            if ( range > SimCommon.MaxNPCAttackRange )
                range = SimCommon.MaxNPCAttackRange;

            return range;
        }

        public float GetNPCRevealRangeSquared()
        {
            float range = this.ActorData[ActorRefs.AttackRange]?.Current ?? 0;
            if ( range < 10 )
                range = 10;
            range += range; //for now, machine vehicles just give a flat double of their scan range

            if ( range > SimCommon.MaxNPCAttackRange )
                return SimCommon.MaxNPCAttackRangeSquared;

            return range * range;
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

        public Vector3 GetEffectiveWorldLocationForContainedUnit()
        {
            return this.worldLocation.PlusY( this.VehicleType.AddedYForCameraFocus );
        }

        public bool GetIsVisibleOnMapWorldLocationForContainedUnit()
        {
            return false;
        }

        public string GetLocationNameForNPCEvents()
        {
            return this.VehicleName;
        }

        public MapCell GetLocationMapCell()
        {
            return this.currentCell;
        }

        public MapTile GetLocationMapTile()
        {
            return this.currentCell?.ParentTile;
        }

        public MapDistrict GetLocationDistrict()
        {
            return this.currentCell?.ParentTile?.District;
        }

        public ListView<ISimMachineUnit> GetStoredUnits()
        {
	        return ListView<ISimMachineUnit>.Create( this.StoredUnits );
        }

		public ArcenDynamicTableRow GetTypeAsRow()
        {
            return this.VehicleType;
        }

        public override IA5Sprite GetShapeIcon()
        {
            return this.VehicleType?.ShapeIcon;
        }

        public string GetShapeIconColorString()
        {
            return string.Empty;
        }

        public override IA5Sprite GetTooltipIcon()
        {
            return this.VehicleType?.TooltipIcon;
        }

        public override VisColorUsage GetTooltipIconColorStyle()
        {
            return this.VehicleType?.TooltipIconColorStyle;
        }

        public string GetDisplayName()
        {
            return this.VehicleName;
        }

        public string GetTypeName()
        {
            return this.VehicleType?.GetDisplayName() ?? "[null vehicle type]";
        }

        public Vector3 GetPositionForCollisions()
        {
            return this.worldLocation.PlusY( this.VehicleType?.YOffsetForCollisionBase ?? 0 );
        }

        public Vector3 GetBottomCenterPosition()
        {
            return this.worldLocation;
        }

        public Vector3 GetCollisionCenter()
        {
            return this.GetPositionForCollisions();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            return TheoreticalPoint.PlusY( this.VehicleType?.YOffsetForCollisionBase ?? 0 );
        }

        public bool GetEquals( ICollidable other )
        {
            if ( other is MachineVehicle vehicle )
                return vehicle.VehicleID == this.VehicleID;
            return false;
        }

        public string GetCollidableTypeID()
        {
            return this.VehicleType?.ID ?? "[null machine vehicle]";
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return 0;
        }

        public float GetRadiusForCollisions()
        {
            return this.VehicleType.RadiusForCollisions;
        }

        public float GetSquaredRadiusForCollisions()
        {
            return this.VehicleType.RadiusSquaredForCollisions;
        }

        public float GetHalfHeightForCollisions()
        {
            return this.VehicleType.HalfHeightForCollisions;
        }

        public bool GetShouldHideIntersectingDecorations()
        {
            return false; //never do this for vehicles
        }

        public List<SubCollidable> GetSubCollidablesOrNull()
        {
            return null;
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
            //this.hasEverSetRotation = true;
        }

        public bool ShouldFreezeCameraAtTheMoment => this.isMoving;

        public void DoOnSelectByPlayer()
        {
            //nothing for now
        }

        public void DoOnFocus_Streets()
        {
        }

        public void DoOnFocus_CityMap()
        {
        }

        public bool GetIsLocationInFogOfWar()
        {
            return false; //always false for machine vehicles
        }

        public MapPOI CalculateLocationPOI()
        {
            return this.currentCell?.ParentTile?.GetPOIOfPoint( this.worldLocation );
        }

        public int CalculateLocationSecurityClearanceInt()
        {
            return MathA.Max( this.currentCell?.ParentTile?.GetPOIOfPoint( this.worldLocation )?.Type?.RequiredClearance?.Level??0,
                 this.currentCell?.ParentTile?.POIOrNull?.Type?.RequiredClearance?.Level??0 );
        }

        public NPCCohort CalculateLocationLocalAuthority()
        {
            NPCCohort group = this.currentCell?.ParentTile?.GetPOIOfPoint( this.worldLocation )?.ControlledBy;
            if ( group != null )
                return group;
            return this.currentCell?.ParentTile?.District?.ControlledBy;
        }

        public bool GetIsGroundStyleLocation()
        {
            return false;
        }

        public int SortPriority => 100; //machine vehicles first
        public int SortID => this.VehicleID;

        public override int GetHashCode()
        {
            int hash = this.VehicleID.GetHashCode();
            hash = HashCodeHelper.CombineHashCodes( hash, 100.GetHashCode() );
            return hash;
        }

        public override bool Equals( object obj )
        {
            if ( obj is MachineVehicle vehicle )
                return vehicle.VehicleID == this.VehicleID;
            else
                return false;
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
            MachineVehicleType unitType = this.VehicleType;
            if ( unitType == null )
                return;

            UnitStyleNovelTooltipBuffer usn = UnitStyleNovelTooltipBuffer.Instance;

            if ( usn.TryStartUnitStyleTooltip( ShouldBeAtMouseCursor || DrawNextTo != null,
                TooltipID.Create( this ), DrawNextTo, Clamp, ShadowStyle, TooltipExtraText.None, ExtraRules ) )
            {
                int debugStage = 0;
                try
                {
                    debugStage = 12000;
                    bool hasAnythingHyperDetailed = false;
                    bool showNormalDetailed = InputCaching.ShouldShowDetailedTooltips;
                    bool showHyperDetailed = InputCaching.ShouldShowHyperDetailedTooltips;
                    bool showAnyDetailed = showNormalDetailed || showHyperDetailed;
                    if ( showHyperDetailed )
                        showNormalDetailed = false;

                    usn.Title.AddSpriteStyled_NoIndent( unitType.ShapeIcon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                    usn.Title.AddRaw( this.VehicleName );
                    usn.PortraitIcon = this.GetTooltipIcon();
                    usn.PortraitFrameColorHex = this.GetTooltipIconColorStyle()?.RelatedBorderColorHex ?? string.Empty;

                    debugStage = 22000;

                    usn.UpperStats.StartUppercase().AddRaw( unitType.GetDisplayName(), ColorTheme.DataBlue ).EndUppercase().Line();
                    usn.UpperStats.AddRaw( this.Stance?.GetDisplayName() ?? LangCommon.None.Text, ColorTheme.Gray ).Line();

                    debugStage = 32000;

                    int effectiveClearanceLevel = this.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
                    SecurityClearance effectiveClearance = this.CurrentBaseClearance;
                    if ( effectiveClearanceLevel > 0 && (this.CurrentBaseClearance == null || this.CurrentBaseClearance.Level < effectiveClearanceLevel) )
                        effectiveClearance = SecurityClearanceTable.ByLevel[effectiveClearanceLevel];

                    usn.LowerStats.AddLangAndAfterLineItemHeader( "SecurityClearance_Brief", ColorTheme.DataLabelWhite ).AddRaw(
                        effectiveClearance == null || effectiveClearance.Level <= 0 ? "-" : effectiveClearance.Level.ToString(), ColorTheme.DataBlue ).Line();

                    debugStage = 42000;

                    if ( !unitType.GetDescription().IsEmpty() )
                        usn.Main.AddRaw( unitType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                    debugStage = 52000;

                    if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt )
                        usn.Main.AddFormat1( "TooManyVehicles", SimCommon.TotalCapacityUsed_Vehicles - MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt, ColorTheme.RedOrange2 ).Line();

                    debugStage = 62000;

                    if ( this.CurrentActionPoints <= 0 )
                        usn.Main.AddLang( "UnitIsOutOfActionPoints", ColorTheme.Gray ).Line();

                    debugStage = 72000;

                    if ( this.CurrentRegistration != null && showAnyDetailed )
                    {
                        debugStage = 72100;
                        usn.Main.AddBoldLangAndAfterLineItemHeader( "Registration", ColorTheme.DataLabelWhite )
                            .AddRaw( this.CurrentRegistration == null ? LangCommon.None.Text : this.CurrentRegistration.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    }

                    debugStage = 82000;

                    debugStage = 92000;

                    debugStage = 102000;

                    this.RenderDataLinesIntoNovelList( null, null, showAnyDetailed );
                    if ( this.VehicleType.UnitStorageSlotCounts.Count > 0 )
                    {
                        foreach ( MachineUnitStorageSlotType slotType in MachineUnitStorageSlotTypeTable.Instance.Rows )
                        {
                            int slotCount = this.VehicleType.UnitStorageSlotCounts[slotType];
                            if ( slotCount > 0 )
                            {
                                usn.StartFreshStat();

                                int usedSlots = slotCount - this.GetRemainingUnitSlotsOfType( slotType );

                                if ( showAnyDetailed )
                                    usn.StatWorkingLeft.AddLangAndAfterLineItemHeader( "PassengerSeats", ColorTheme.BasicLightTextBlue );
                                usn.StatWorkingLeft.AddRaw( slotType.GetDisplayName(), ColorTheme.BasicLightTextBlue );

                                usn.StatWorkingRight.AddFormat2( "OutOF", usedSlots, slotCount, ColorTheme.DataBlue );

                                usn.FinishStatDataLine( slotType.Icon, ColorTheme.BasicLightTextBlue );
                            }
                        }
                    }

                    debugStage = 112000;

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
                            int aggroAmount = this.GetAmountHasAggroedNPCCohort( cohort, null, null );
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

                    debugStage = 122000;

                    if ( this.IncomingDamage.Display.IncomingPhysicalDamageTargeting > 0 )
                    {
                        hasAnythingHyperDetailed = true;
                        this.AppendIncomingDamageLines( lowerEffective, showHyperDetailed );
                    }

                    debugStage = 132000;

                    if ( !string.IsNullOrEmpty( debugText ) )
                        lowerEffective.AddNeverTranslatedAndAfterLineItemHeader( "DebugText", ColorTheme.AngRed_Dimmed, true ).AddRaw( debugText ).Line();

                    debugStage = 142000;

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
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "WriteVehicleTooltipOrDetails", debugStage, e, Verbosity.ShowAsError );
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
            //    Sprite = this.VehicleType.TooltipIcon;
            //    FrameColRow = IconRefs.PlayerUnit_Portrait.FrameMaskRowCol;
            //    Color = IconRefs.PlayerUnit_Portrait.DefaultColorHDR;
            //    IconScale = IconRefs.PlayerUnit_Portrait.DefaultScale;
            //}
            //else
            {
                DrawFrameStyle = false;
                Sprite = this.VehicleType.ShapeIcon;
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
            int current = 0;
            foreach ( MachineUnit unit in this.StoredUnits )
            {
                if ( unit.ActorData.TryGetValue( Type, out MapActorData data ) )
                    current += data.Current;
            }
            return current;
        }

        public int GetCombinedFromChildrenActorDataCurrent( string TypeName )
        {
            ActorDataType dataType = ActorDataTypeTable.Instance.GetRowByID( TypeName );
            if ( dataType == null )
            {
                ArcenDebugging.LogSingleLine( "GetCombinedFromChildrenActorDataCurrent: Asked for data '" + TypeName + "', but it did not exist!", Verbosity.ShowAsError );
                return 0;
            }
            return GetCombinedFromChildrenActorDataCurrent( dataType );
        }
        #endregion

        /// <summary>
        /// In this context, this means "remove me from here if I was here"
        /// </summary>
        public void ClearOccupyingUnitIfThisOne( ISimUnit IUnit )
        {
            MachineUnit unit = IUnit as MachineUnit;
            if ( unit == null )
                return;
            this.StoredUnits.Remove( unit );
        }

        /// <summary>
        /// In this context, this means "add me to the list of units"
        /// </summary>
        public void SetOccupyingUnitToThisOne( ISimUnit IUnit )
        {
            MachineUnit unit = IUnit as MachineUnit;
            if ( unit == null )
                return;
            this.StoredUnits.AddIfNotAlreadyIn( unit );
        }

        public bool GetIsNPCBlockedFromComingHere( NPCUnitStance Stance )
        {
            return true; //always yes
        }

        public bool GetAreMoreUnitsBlockedFromComingHere()
        {
            throw new NotSupportedException( "Cannot call GetAreMoreUnitsBlockedFromComingHere on a MachineVehicle!" );
        }

        public void MarkAsBlockedUntilNextTurn( ISimUnit Blocker )
        {
            throw new NotSupportedException( "Cannot call MarkAsBlockedUntilNextTurn on a MachineVehicle!" );
        }

        #region CalculateMapCell
        public override MapCell CalculateMapCell()
        {
            return this.currentCell;
        }
        #endregion

        #region CalculateMapDistrict
        public override MapDistrict CalculateMapDistrict()
        {
            return this.currentCell?.ParentTile?.District;
        }
        #endregion

        #region CalculatePOI
        public override MapPOI CalculatePOI()
        {
            MapCell cell = this.currentCell;
            if ( cell == null )
                return null;
            else
                return cell?.ParentTile?.GetPOIOfPoint( this.worldLocation );
        }
        #endregion

        public int GetPercentRobotic()
        {
            return 100;
        }

        public int GetPercentBiological()
        {
            return 0;
        }

        public void PlayBulletHitEffectAtPositionForCollisions()
        {
            this.VehicleType?.OnBulletHit?.DuringGame_PlayAtLocation( this.GetPositionForCollisions() );
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
            return this.VehicleType?.ShotEmissionGroups;
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
            return this.VehicleType?.IsTiedToShellCompany ?? false;
        }

        public override bool GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer()
        {
            return this.VehicleType?.IsTiedToShellCompany ?? false;
        }

        public bool IsInvalid => this.VehicleType == null || this.IsInPoolAtAll || this.IsFullDead;

        public bool CanMakeMeleeAttacks()
        {
            return false;
        }

        public bool CanMakeRangedAttacks()
        {
            return true;
        }

        public int GetUniqueLocationID()
        {
            return this.VehicleID;
        }

        public bool GetIsLocationStillValid()
        {
            return !this.IsInPoolAtAll;
        }

        public VisParticleAndSoundUsage GetMeleeAttackSoundAndParticles()
        {
            return null;
        }

        public VisParticleAndSoundUsage GetRangedAttackSoundAndParticles()
        {
            VisParticleAndSoundUsage rangedSound = null;
            int rangedSoundPriority = -1;
            foreach ( EquipmentEntry equipment in this.VehicleType.DuringGameData.Equipment )
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

            return this.VehicleType.OnStandardRangedAttack;
        }

        protected override bool GetIsAtABuilding()
        {
            return false; //that has no meaning for vehicles
        }

        public bool GetIsInActorCollection( ActorCollection Collection )
        {
            if ( Collection == null )
                return false;
            return this.VehicleType?.Collections?.ContainsKey( Collection.ID ) ?? false;
        }

        #region GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation
        public bool GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation( NPCCohort FromCohort, bool SkipInconspicuousCheck )
        {
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || this.IsFullDead || this.IsCloaked )
                return true;

            if ( SkipInconspicuousCheck )
                return false;

            if ( this.GetAmountHasAggroedNPCCohort( FromCohort, null, null ) > 0 )
                return false; //if we have aggroed that group, then no hiding from them

            int unremarkableUpTo = this.IsUnremarkableAnywhereUpToClearanceInt; //we will ignore the building variant for vehicles, since they cannot be at buildings
            if ( unremarkableUpTo >= 0 ) //if this is less than zero, doesn't matter what our clearance is, we're being hunted
            {
                MapTile tile = this.currentCell?.ParentTile;
                if ( tile == null )
                    return true; //we are off the map somehow

                int effectiveClearance = this.IsUnremarkableAnywhereUpToClearanceInt;

                Vector3 pt = this.worldLocation;
                foreach ( MapPOI poi in tile.RestrictedPOIs )
                {
                    if ( poi == null || poi.HasBeenDestroyed )
                        continue;
                    ArcenFloatRectangle rect = poi.GetOuterRect();
                    if ( !rect.ContainsPointXZ( pt ) )
                        continue; //destination not in here, so nevermind

                    SecurityClearance clearance = poi.Type.RequiredClearance;
                    if ( clearance != null && clearance.Level > effectiveClearance )
                        return false; //this poi has a higher security clearance than us, so shoot at us
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
            return MachineActorHelper.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Vehicle( FromCohort, BuildingOrNull, Location, WillHaveDoneAttack, SkipInconspicuousCheck,
                this.IsCloaked, this.GetAmountHasAggroedNPCCohort( FromCohort, null, null ), this.IsUnremarkableAnywhereUpToClearanceInt );
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
                if ( npcUnit.GetWillFireOnMachineUnitsBaseline() )
                {
                    if ( !(npcUnit.Stance?.IsHyperAggressiveAgainstAllButItsOwnCohort ?? false) )
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

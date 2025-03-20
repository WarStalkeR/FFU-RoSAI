using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using TMPro;

namespace Arcen.HotM.External
{
    internal abstract class BaseMachineActor : BaseMapActor
    {
        //
        //Serialized data
        //-----------------------------------------------------
        private int currentActionPoints = 0;
        public int StartingHealthAtTheEndOfTheTurn { get; protected set; } = 0;
        public bool IsScheduledToDoActionOverTime = false;
        public SecurityClearance CurrentBaseClearance { get; set; } = null;
        public NPCCohort CurrentRegistration { get; set; } = null;
        public AbilityType IsInAbilityTypeTargetingMode { get; private set; }
        public ResourceConsumable IsInConsumableTargetingMode { get; private set; }
        public StandbyType CurrentStandby { get; set; } = StandbyType.None;

        public ActionOverTime CurrentActionOverTime { get; set; } = null;
        public ActionOverTime AlmostCompleteActionOverTime { get; set; } = null;

        private IAutoPooledFloatingColliderIcon ColliderIcon { get; set; } = null;

        public readonly ConcurrentDictionary<NPCManager, int> ManagerBlockedUntilTurns = ConcurrentDictionary<NPCManager, int>.Create_WillNeverBeGCed( "BaseMachineActor-ManagerBlockedUntilTurns" );

        //
        //NonSerialized data
        //-----------------------------------------------------
        public bool IsFullDead { get; protected set; } = false;
        protected bool isMoving = false;
        public IAutoPooledFloatingText FloatingText { get; set; } = null;
        protected readonly ArcenDoubleCharacterBuffer FloatingTextBuffer = new ArcenDoubleCharacterBuffer( "BaseMachineActor-FloatingTextBuffer" );

        protected sealed override void ClearData_TypeSpecific()
        {
            //Serialized
            //---------------
            this.currentActionPoints = 0;
            this.StartingHealthAtTheEndOfTheTurn = 0;
            this.IsScheduledToDoActionOverTime = false;
            this.CurrentBaseClearance = null;
            this.CurrentRegistration = null;
            this.IsInAbilityTypeTargetingMode = null;
            this.IsInConsumableTargetingMode = null;
            this.CurrentStandby = StandbyType.None;

            if ( this.CurrentActionOverTime != null )
            {
                this.CurrentActionOverTime.ReturnToPool();
                this.CurrentActionOverTime = null;
            }
            if ( this.AlmostCompleteActionOverTime != null )
            {
                this.AlmostCompleteActionOverTime.ReturnToPool();
                this.AlmostCompleteActionOverTime = null;
            }

            this.ColliderIcon = null;

            this.ManagerBlockedUntilTurns.Clear();

            //Nonserialized
            //---------------
            this.IsFullDead = false;

            this.isMoving = false;

            if ( this.FloatingText != null )
            {
                this.FloatingText.ReturnToPool();
                this.FloatingText = null;
            }
            FloatingTextBuffer.Clear();

            this.ClearData_MachineTypeSpecific();
        }

        protected abstract void ClearData_MachineTypeSpecific();

        #region Serialization

        protected sealed override void Serialize_TypeSpecific( ArcenFileSerializer Serializer )
        {
            Serializer.AddInt32IfGreaterThanZero( "CurrentActionPoints", this.currentActionPoints );
            Serializer.AddInt32IfGreaterThanZero( "StartingHealthAtTheEndOfTheTurn", StartingHealthAtTheEndOfTheTurn );
            Serializer.AddBoolIfTrue( "IsScheduledToDoActionOverTime", IsScheduledToDoActionOverTime );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "CurrentBaseClearance", this.CurrentBaseClearance?.ID );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "CurrentRegistration", this.CurrentRegistration?.ID );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "IsInAbilityTypeTargetingMode", this.IsInAbilityTypeTargetingMode?.ID );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "IsInConsumableTargetingMode", this.IsInConsumableTargetingMode?.ID );
            Serializer.AddInt32IfGreaterThanZero( "CurrentStandby", (int)this.CurrentStandby );

            if ( this.CurrentActionOverTime != null )
            {
                Serializer.StartObject( "ActionOverTime" );
                this.CurrentActionOverTime.SerializeData( Serializer );
                Serializer.EndObject( "ActionOverTime" );
            }
            if ( this.AlmostCompleteActionOverTime != null )
            {
                Serializer.StartObject( "AlmostCompleteActionOverTime" );
                this.AlmostCompleteActionOverTime.SerializeData( Serializer );
                Serializer.EndObject( "AlmostCompleteActionOverTime" );
            }

            if ( this.ManagerBlockedUntilTurns.Count > 0 )
            {
                foreach ( KeyValuePair<NPCManager, int> kv in this.ManagerBlockedUntilTurns )
                {
                    if ( kv.Value <= SimCommon.Turn )
                        this.ManagerBlockedUntilTurns.TryRemove( kv.Key, 5 );
                }
            }

            Serializer.AddDictionaryIfHasAnyEntries( ArcenSerializedDataType.RepeatedStringCondensed, ArcenSerializedDataType.Int32, "ManagerBlockedUntilTurns", this.ManagerBlockedUntilTurns );

            this.Serialize_MachineTypeTypeSpecific( Serializer );
        }
        protected abstract void Serialize_MachineTypeTypeSpecific( ArcenFileSerializer Serializer );

        protected void AssistInDeserialize_BaseMachineActor_Primary( DeserializedObjectLayer Data )
        {
            this.currentActionPoints = Data.GetInt32( "CurrentActionPoints", false );
            this.StartingHealthAtTheEndOfTheTurn = Data.GetInt32( "StartingHealthAtTheEndOfTheTurn", false );
            this.IsScheduledToDoActionOverTime = Data.GetBool( "IsScheduledToDoActionOverTime", false );

            if ( Data.TryGetTableRow( "CurrentBaseClearance", SecurityClearanceTable.Instance, out SecurityClearance clearance ) )
                this.CurrentBaseClearance = clearance;
            else
                this.CurrentBaseClearance = null;

            if ( Data.TryGetTableRow( "CurrentRegistration", NPCCohortTable.Instance, out NPCCohort currentRegistration ) )
                this.CurrentRegistration = currentRegistration;
            else
                this.CurrentRegistration = null;

            if ( Data.TryGetTableRow( "IsInAbilityTypeTargetingMode", AbilityTypeTable.Instance, out AbilityType isInAbilityTypeTargetingMode ) )
                this.IsInAbilityTypeTargetingMode = isInAbilityTypeTargetingMode;
            else
                this.IsInAbilityTypeTargetingMode = null;

            if ( Data.TryGetTableRow( "IsInConsumableTargetingMode", ResourceConsumableTable.Instance, out ResourceConsumable isInConsumableTargetingMode ) )
                this.IsInConsumableTargetingMode = isInConsumableTargetingMode;
            else
                this.IsInConsumableTargetingMode = null;

            this.CurrentStandby = (StandbyType)Data.GetInt32( "CurrentStandby", false );

            if ( Data.TryGetDictionary( "ManagerBlockedUntilTurns", out Dictionary<string, int> managerBlockedUntilTurnsDict ) )
            {
                foreach ( KeyValuePair<string, int> kv in managerBlockedUntilTurnsDict )
                {
                    if ( kv.Value <= SimCommon.Turn )
                        continue;
                    NPCManager manager = NPCManagerTable.Instance.GetRowByIDOrNullIfNotFound( kv.Key );
                    if ( manager == null )
                        continue; //must have been deprecated
                    this.ManagerBlockedUntilTurns[manager] = kv.Value;
                }
            }

            //ActionOverTime must be handled in AssistInDeserialize_BaseMachineActor_Late because it references buildings
            //AlmostCompleteActionOverTime must be handled in AssistInDeserialize_BaseMachineActor_Late because it references buildings
        }

        protected void AssistInDeserialize_BaseMachineActor_Late( DeserializedObjectLayer Data )
        {
            if ( Data.ChildLayersByName.TryGetValue( "ActionOverTime", out List<DeserializedObjectLayer> actionOverTimeList ) )
            {
                foreach ( DeserializedObjectLayer actionOverTimeData in actionOverTimeList )
                    ActionOverTime.DeserializeData( (ISimMachineActor)this, actionOverTimeData, false ); //will automatically assign itself here

            }
            if ( Data.ChildLayersByName.TryGetValue( "AlmostCompleteActionOverTime", out List<DeserializedObjectLayer> almostCompleteActionOverTimeList ) )
            {
                foreach ( DeserializedObjectLayer almostCompleteActionOverTimeData in almostCompleteActionOverTimeList )
                    ActionOverTime.DeserializeData( (ISimMachineActor)this, almostCompleteActionOverTimeData, true ); //will automatically assign itself here
            }
        }
        #endregion Serialization

        public void SetTargetingMode( AbilityType AbilityType, ResourceConsumable Consumable )
        {
            this.IsInAbilityTypeTargetingMode = AbilityType;
            this.IsInConsumableTargetingMode = Consumable;

            //ArcenDebugging.LogWithStack( "Set targeting mode.", Verbosity.DoNotShow );
        }

        #region HandleAnyMachineLogicOnDeath
        protected void HandleAnyMachineLogicOnDeath()
        {
            //if this unit is dying, go ahead and do this quickly
            ActionOverTime almostComplete = this.AlmostCompleteActionOverTime;
            if ( almostComplete != null )
            {
                almostComplete?.DoActionFinalSuccessLogic( true, Engine_Universal.PermanentQualityRandom );
                this.AlmostCompleteActionOverTime = null;
            }

            //if we can't complete our action over time because we're dying, some npc units may care about that
            ActionOverTime currentAction = this.CurrentActionOverTime;
            if ( currentAction != null )
            {
                if ( currentAction.RelatedInvestigationTypeOrNull != null )
                    currentAction.RelatedInvestigationTypeOrNull?.DuringGame_DoAnythingNeededFromFailure( false );

                ActivityScheduler.DoThreatNeutralizedCheckRelatedToUnitWithActionOvertimeDeath( this as ISimMachineActor, currentAction );

                this.CurrentActionOverTime?.ReturnToPool();
                this.CurrentActionOverTime = null;
            }
        }
        #endregion

        #region GetEffectiveClearance
        public int GetEffectiveClearance( ClearanceCheckType CheckType )
        {
            int bestClearance = this.IsUnremarkableAnywhereUpToClearanceInt;
            if ( bestClearance < 0 )
                bestClearance = 0; //for these purposes, our floor-value is 0

            SecurityClearance currentClearance = this.CurrentBaseClearance;
            if ( currentClearance != null && currentClearance.Level > bestClearance )
                bestClearance = currentClearance.Level;

            switch (CheckType)
            {
                case ClearanceCheckType.StayingThere:
                    {
                        int bestClearanceAtBuilding = this.IsUnremarkableBuildingOnlyUpToClearanceInt;
                        if ( bestClearanceAtBuilding > bestClearance && this.GetIsAtABuilding() )
                            bestClearance = bestClearanceAtBuilding;
                    }
                    break;
                case ClearanceCheckType.MovingToBuilding:
                    {
                        int bestClearanceAtBuilding = this.IsUnremarkableBuildingOnlyUpToClearanceInt;
                        if ( bestClearanceAtBuilding > bestClearance ) //we already know we're moving to a building
                            bestClearance = bestClearanceAtBuilding;
                    }
                    break;
                //case ClearanceCheckType.MovingToNonBuilding: //nothing special to do here
                //    break;
            }

            return bestClearance;
        }
        #endregion

        protected abstract bool GetIsAtABuilding();

        public ConcurrentDictionary<NPCManager, int> GetManagersBlockedUntilTurns()
        {
            return this.ManagerBlockedUntilTurns;
        }

        #region HandleEnemyActionsAfterUnitMove
        protected void HandleEnemyActionsAfterUnitMoveOrAct( bool IsFromMainThread )
        {
            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
        }
        #endregion

        #region GetMostRelevantGroup
        protected NPCCohort GetMostRelevantGroup()
        {
            NPCCohort group = null;
            ISimMachineUnit machineUnit = this as ISimMachineUnit;
            if ( machineUnit != null )
            {
                if ( machineUnit.ContainerLocation.Get() is ISimBuilding building )
                {
                    group = building.GetMapItem()?.GetParentPOIOrNull()?.ControlledBy;
                    if ( group != null )
                        return group;

                    group = building.GetMapItem()?.ParentTile?.District?.ControlledBy;
                    if ( group != null )
                        return group;
                }
            }

            MapCell cell = this.CalculateMapCell();
            if ( cell != null )
            {
                group = cell?.ParentTile?.District?.ControlledBy;
                if ( group != null )
                    return group;
            }
            return group;
        }
        #endregion

        #region TryCreateNewUnitAsCloseAsPossibleToThisOne
        public ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToThisOne( MachineUnitType UnitTypeToGrant, string NewUnitOverridingName, CellRange CRange,
            MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( UnitTypeToGrant == null )
                return null;

            Vector3 unitSpot;
            MapCell cell;
            ISimMachineUnit machineUnit = this as ISimMachineUnit;
            if ( machineUnit != null )
            {
                ISimUnitLocation loc = machineUnit.ContainerLocation.Get();
                if ( loc != null )
                {
                    cell = loc.GetLocationMapCell();
                    unitSpot = loc.GetEffectiveWorldLocationForContainedUnit().ReplaceY( 0 );
                }
                else
                {
                    cell = CityMap.TryGetWorldCellAtCoordinates( machineUnit.GroundLocation );
                    unitSpot = machineUnit.GroundLocation.ReplaceY( 0 );
                }
            }
            else
            {
                ISimMachineVehicle machineVehicle = this as ISimMachineVehicle;
                if ( machineVehicle == null )
                    return null;

                unitSpot = machineVehicle.WorldLocation.ReplaceY( 0 );
                cell = machineVehicle.GetCurrentMapCell();
            }

            return World.Forces.TryCreateNewMachineUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, NewUnitOverridingName, CRange,
                Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
        }
        #endregion

        #region TryCreateNewMachineUnitAsCloseAsPossibleToBuilding
        public ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( UnitTypeToGrant == null )
                return null;
            MapItem buildingItem = BuildingToBeCloseTo?.GetMapItem();
            MapCell cell = buildingItem?.ParentCell;
            if ( buildingItem == null || cell == null )
                return TryCreateNewMachineUnitAsCloseAsPossibleToThisOne( UnitTypeToGrant, NewUnitOverridingName, CRange, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );

            Vector3 unitSpot = buildingItem.GroundCenterPoint;
            return World.Forces.TryCreateNewMachineUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, NewUnitOverridingName, CRange,
                Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
        }
        #endregion

        #region TryCreateNewVehicleAsCloseAsPossibleToThisOne
        public ISimMachineVehicle TryCreateNewVehicleAsCloseAsPossibleToThisOne( MachineVehicleType VehicleTypeToGrant, string NewVehicleOverridingName,
            MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( VehicleTypeToGrant == null )
                return null;

            MapCell cell = null;
            ISimMachineUnit machineUnit = this as ISimMachineUnit;
            if ( machineUnit != null )
            {
                if ( machineUnit.ContainerLocation.Get() != null )
                    cell = machineUnit.ContainerLocation.Get()?.GetLocationMapCell();

                if ( cell == null )
                    cell = CityMap.TryGetWorldCellAtCoordinates( machineUnit.GroundLocation );
            }
            else
            {
                ISimMachineVehicle machineVehicle = this as ISimMachineVehicle;
                if ( machineVehicle == null )
                    return null;

                cell = machineVehicle.GetCurrentMapCell();
            }

            MapItem randomBuilding = cell.BuildingList.GetDisplayList().GetRandom( Engine_Universal.PermanentQualityRandom );
            Vector3 aboveBuilding = randomBuilding.TopCenterPoint.PlusY( 1.2f );
            return World.Forces.TryCreateNewMachineVehicleAsCloseAsPossibleToLocation( aboveBuilding, cell, VehicleTypeToGrant,
                NewVehicleOverridingName, CellRange.CellAndAdjacent2x, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToThis
        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToThis( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance,
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, 
            CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;

            ISimMachineUnit machineUnit = this as ISimMachineUnit;
            Vector3 unitSpot;
            MapCell cell;
            if ( machineUnit != null )
            {
                ISimUnitLocation loc = machineUnit.ContainerLocation.Get();
                if ( loc != null )
                {
                    cell = loc.GetLocationMapCell();
                    unitSpot = loc.GetEffectiveWorldLocationForContainedUnit().ReplaceY( 0 );
                }
                else
                {
                    cell = CityMap.TryGetWorldCellAtCoordinates( machineUnit.GroundLocation );
                    unitSpot = machineUnit.GroundLocation.ReplaceY( 0 );
                }
            }
            else 
            {
                ISimMachineVehicle machineVehicle = this as ISimMachineVehicle;
                if ( machineVehicle == null )
                    return null;

                unitSpot = machineVehicle.WorldLocation.ReplaceY( 0 );
                cell = machineVehicle.GetCurrentMapCell();
            }

            return World.Forces.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant,
                FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );
        }
        #endregion

        #region TryCreateNewNPCUnitWithinThisRadius
        public ISimNPCUnit TryCreateNewNPCUnitWithinThisRadius( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance,
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, float Radius, int MaxDesiredClearance, 
            CellRange FailoverCRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;

            ISimMachineUnit machineUnit = this as ISimMachineUnit;
            Vector3 unitSpot;
            MapCell cell;
            if ( machineUnit != null )
            {
                ISimUnitLocation loc = machineUnit.ContainerLocation.Get();
                if ( loc != null )
                {
                    cell = loc.GetLocationMapCell();
                    unitSpot = loc.GetEffectiveWorldLocationForContainedUnit().ReplaceY( 0 );
                }
                else
                {
                    cell = CityMap.TryGetWorldCellAtCoordinates( machineUnit.GroundLocation );
                    unitSpot = machineUnit.GroundLocation.ReplaceY( 0 );
                }
            }
            else
            {
                ISimMachineVehicle machineVehicle = this as ISimMachineVehicle;
                if ( machineVehicle == null )
                    return null;

                unitSpot = machineVehicle.WorldLocation.ReplaceY( 0 );
                cell = machineVehicle.GetCurrentMapCell();
            }

            return World.Forces.TryCreateNewNPCUnitWithinThisRadius( unitSpot, cell, UnitTypeToGrant,
                FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, Radius, MaxDesiredClearance, FailoverCRange, Rand, Rule, CreationReason );
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToBuilding
        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, NPCUnitType UnitTypeToGrant,
            NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint,
            float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant == null )
                return null;
            MapItem buildingItem = BuildingToBeCloseTo?.GetMapItem();
            MapCell cell = buildingItem?.ParentCell;
            if ( buildingItem == null || cell == null )
                return TryCreateNewNPCUnitAsCloseAsPossibleToThis( UnitTypeToGrant, FromCohort,
                    Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );

            Vector3 unitSpot = buildingItem.GroundCenterPoint;
            return World.Forces.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, FromCohort,
                Stance, SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, CRange, Rand, Rule, CreationReason );
        }
        #endregion

        protected void HandlePerTurnMachineActorLogic()
        {
            this.StartingHealthAtTheEndOfTheTurn = this.GetActorDataCurrent( ActorRefs.ActorHP, true );
        }

        public abstract bool GetActorHasAbilityEquipped( AbilityType Ability );
        public abstract AbilityType GetActorAbilityInSlotIndex( int SlotIndex );
        public abstract bool GetActorAbilityCanBePerformedNow( AbilityType AbilityType, ArcenDoubleCharacterBuffer BufferOrNull );
        public abstract bool TryPerformActorAbilityInSlot( int SlotIndex, bool DoSilently, TriggerStyle Style );

        #region CalculateEffectiveScaleMultiplierFromCameraPositionAndMode
        public float CalculateEffectiveScaleMultiplierFromCameraPositionAndMode()
        {
            bool isMapView = Engine_HotM.GameMode == MainGameMode.CityMap;
            float newScale = isMapView ? 4f : 1f;
            if ( isMapView )
            {
                //if we are very high, then scale the icon up
                if ( Engine_HotM.CameraHeight > 100 )
                {
                    float amountAbove = Engine_HotM.CameraHeight - 100;
                    if ( amountAbove >= 400 )
                        newScale += 8f;
                    else
                        newScale += (amountAbove / 400f) * 8f;
                }
            }
            else
            {
                //if we are a bit high, then scale the icon up
                if ( Engine_HotM.CameraHeight > 10 )
                {
                    float amountAbove = Engine_HotM.CameraHeight - 10;
                    if ( amountAbove >= 40 )
                        newScale += 3f;
                    else
                        newScale += (amountAbove / 40f) * 3f;
                }
            }
            newScale *= GetIconScale();
            return newScale;
        }
        #endregion

        public bool GetIsBlockedFromBeingScrappedRightNow()
        {
            return this.GetIsBlockedFromBeingScrappedRightNow( false );
        }

        protected bool GetIsBlockedFromBeingScrappedRightNow(bool DoPopupsIfBlocked )
        {
            if ( this.CurrentActionOverTime?.Type?.BlocksBeingScrapped ?? false )
            {
                VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotScrapUnitThatIsDoingAnActionOvertime" ), 
                    TooltipID.Create( "ScrapMachineActor", this.NonSimUniqueID, 0 ), TooltipWidth.Narrow, 2, 2f );
                return true;
            }
            if ( this.AlmostCompleteActionOverTime?.Type?.BlocksBeingScrapped ?? false )
            {
                VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotScrapUnitThatIsDoingAnActionOvertime" ),
                    TooltipID.Create( "ScrapMachineActor", this.NonSimUniqueID, 0 ), TooltipWidth.Narrow, 2, 2f );
                return true;
            }
            return false;

        }
        #region AppendClearanceAndActionOverTimeAsIfWereStatuses
        protected int AppendClearanceAndActionOverTimeAsIfWereStatuses( ArcenDoubleCharacterBuffer Buffer, bool showNormalDetailed, bool ShowHyperDetailed )
        {
            int additions = 0;
            int effectiveClearanceLevel = this.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
            if ( effectiveClearanceLevel > 0 && (this.CurrentBaseClearance == null || this.CurrentBaseClearance.Level < effectiveClearanceLevel) )
            {
                SecurityClearance effectiveClearance = SecurityClearanceTable.ByLevel[effectiveClearanceLevel];
                if ( effectiveClearance.Level > 1 )
                {
                    if ( ShowHyperDetailed )
                    {
                        additions++;
                        Buffer.AddBoldLangAndAfterLineItemHeader( "SecurityClearance", ColorTheme.DataLabelWhite );
                        Buffer.AddRaw( effectiveClearance.Level.ToStringWholeBasic(), ColorTheme.DataBlue );
                        Buffer.HyphenSeparator().AddRaw( effectiveClearance.GetDisplayName(), ColorTheme.DataBlue );
                            //.AddRaw( effectiveClearance.GetDescription(), ColorTheme.NarrativeColor );
                        Buffer.Line();
                    }
                }
            }
            else
            {
                if ( this.CurrentBaseClearance != null && this.CurrentBaseClearance.Level > 1 )
                {
                    if ( ShowHyperDetailed )
                    {
                        Buffer.AddBoldLangAndAfterLineItemHeader( "SecurityClearance", ColorTheme.DataLabelWhite );
                        Buffer.AddRaw( this.CurrentBaseClearance.Level.ToStringWholeBasic(), ColorTheme.DataBlue );
                        Buffer.HyphenSeparator().AddRaw( this.CurrentBaseClearance.GetDisplayName(), ColorTheme.DataBlue );
                            //.AddRaw( this.CurrentBaseClearance.GetDescription(), ColorTheme.NarrativeColor );
                        Buffer.Line();
                    }
                }
            }

            if ( this.CurrentActionOverTime != null )
            {
                this.CurrentActionOverTime.Type.Implementation.TryHandleActionOverTime( this.CurrentActionOverTime, Buffer, ActionOverTimeLogic.PredictTurnsRemainingText, null,
                    null, SideClamp.Any, TooltipExtraText.None, TooltipExtraRules.None );
                Buffer.Space1x().AddRaw( this.CurrentActionOverTime.Type.GetDisplayName(), this.CurrentActionOverTime.Type.IconColor.ColorHexWithHDR );
                Buffer.Line();
            }

            return additions;
        }
        #endregion

        protected abstract float GetIconScale();
        public abstract IA5Sprite GetTooltipIcon();
        public abstract IA5Sprite GetShapeIcon();
        public abstract VisColorUsage GetTooltipIconColorStyle();
        public abstract MobileActorTypeDuringGameData GetTypeDuringGameData();

        public override float GetFeatAmount( ActorFeat Feat )
        {
            return this.GetTypeDuringGameData()?.EffectiveFeats?.GetDisplayDict()?[Feat] ?? -1f;
        }

        public FeatSetForType GetFeatSetOrNull()
        {
            return this.GetTypeDuringGameData()?.FeatSet;
        }

        #region ActionPoints
        public int MaxActionPoints
        {
            get
            {
                return this.ActorData[ActorRefs.ActorMaxActionPoints]?.Current ?? 2;
            }
        }

        public int CurrentActionPoints
        {
            get
            {
                return this.currentActionPoints;
            }
        }

        public void AlterCurrentActionPoints( int ByAmount )
        {
            Interlocked.Add( ref this.currentActionPoints, ByAmount );
            int max = this.MaxActionPoints;
            int overMax = this.currentActionPoints - max;
            if ( overMax > 0 )
                Interlocked.Add( ref this.currentActionPoints, -overMax );
            else if ( this.currentActionPoints < 0 )
                Interlocked.Add( ref this.currentActionPoints, -this.currentActionPoints );
        }

        protected void SetCurrentActionPoints( int NewValue )
        {
            Interlocked.Exchange( ref this.currentActionPoints, NewValue );
        }

        internal void SetStartingActionPoints( bool IsReadyToAct )
        {
            if ( IsReadyToAct )
                Interlocked.Exchange( ref this.currentActionPoints, this.MaxActionPoints );
            else
                Interlocked.Exchange( ref this.currentActionPoints, 0 );
        }
        #endregion

        #region TryDrawSelectedToActIcon
        protected bool TryDrawSelectedToActIcon( Vector3 EffectivePosition )
        {
            if ( Engine_HotM.SelectedActor != this )
                return false; //also never draw this if we are not the selected unit!
            if ( Engine_HotM.SelectedMachineActionMode != null )
                return false; //if in another mode
            if ( VisCurrent.GetShouldBeBlurred() )
                return false; //hide when in a blurred scene
            if ( VisCurrent.IsInPhotoMode )
                return false; //hide when in photo mode

            if ( InputCaching.IsInInspectMode_FocusOnLens )
                return false;

            //VisIconUsage iconUsage = this.CurrentStandby != StandbyType.None ? IconRefs.MachineStandbySelectedIcon : (
            //    this.CurrentActionPoints <= 0 || ResourceRefs.MentalEnergy.Current <= 0 || this.CurrentActionOverTime != null ?
            //    IconRefs.MachineSelectedUnreadyIcon : IconRefs.MachineSelectedReadyIcon );

            //IA5Sprite icon = iconUsage?.Icon;
            //if ( icon == null )
            //    return false;

            float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
            //float scale = scaleMultiplier * iconUsage.DefaultScale * InputCaching.Scale_UnitStanceIcons;
            Vector3 effectivePoint = EffectivePosition;//.PlusY( iconUsage.DefaultAddedY );

            //if ( this.CurrentActionOverTime == null ) //don't draw this part when doing an action over time, but do draw the incoming damage
            //    icon.WriteToDrawBufferForOneFrame( true, effectivePoint, scale, iconUsage.DefaultColorHDR, false, false, true );

            this.DrawIncomingDamage( effectivePoint, scaleMultiplier );

            return true;
        }
        #endregion

        #region DrawNotSelectedIcon
        protected void DrawNotSelectedIcon( Vector3 EffectivePosition )
        {
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene
            if ( VisCurrent.IsInPhotoMode )
                return; //hide when in photo mode
            if ( !(this is IAutoRelatedObject relatable) )
                return;

            if ( InputCaching.IsInInspectMode_FocusOnLens )
                return;

            VisIconUsage iconUsage = this.CurrentStandby != StandbyType.None ? IconRefs.MachineStandbyIcon : (
                this.CurrentActionPoints <= 0 || ResourceRefs.MentalEnergy.Current <= 0 || this.CurrentActionOverTime != null ?
                IconRefs.MachineUnreadyIcon : IconRefs.MachineReadyIcon );

            IA5Sprite icon = iconUsage?.Icon;
            if ( icon == null )
                return;

            float scaleMultiplier = this.CalculateEffectiveScaleMultiplierFromCameraPositionAndMode();
            float scale = scaleMultiplier * iconUsage.DefaultScale * InputCaching.Scale_UnitStanceIcons;
            Vector3 effectivePoint = EffectivePosition.PlusY( iconUsage.DefaultAddedY );

            if ( this.CurrentActionOverTime == null ) //don't draw this part when doing an action over time, but do draw the incoming damage
            {
                IA5Sprite sprite = this.GetShapeIcon();
                if ( this.ColliderIcon != null && this.ColliderIcon.GetIsValidToUse( relatable ) )
                { } //already exists, so just update it
                else
                {
                    //does not already exist, so establish it
                    this.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( sprite, relatable );
                    this.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                    this.ColliderIcon.IsForOverlayCamera = true;
                    this.ColliderIcon.UseBackingColor = false;
                }

                this.ColliderIcon.WorldLocation = effectivePoint;
                this.ColliderIcon.Sprite = sprite;
                this.ColliderIcon.DrawFrameStyle = false;

                if ( this.CurrentStandby != StandbyType.None )
                    this.ColliderIcon.Color = IconRefs.PlayerUnitStandby_Icon.DefaultColorHDR;
                else
                    this.ColliderIcon.Color = iconUsage.DefaultColorHDR;
                this.ColliderIcon.ObjectScale = scale;

                if ( this.ColliderIcon.IsMouseover )
                    this.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );

                this.ColliderIcon.MarkAsStillInUseThisFrame();

                //this.GetShapeIcon().WriteToDrawBufferForOneFrame( true, effectivePoint, scale, iconUsage.DefaultColorHDR, false, false, true );
                //icon.WriteToDrawBufferForOneFrame( true, effectivePoint, scale, iconUsage.DefaultColorHDR, false, false, true );
            }

            this.DrawIncomingDamage( effectivePoint, scaleMultiplier );
        }
        #endregion

        private void DrawIncomingDamage( Vector3 EffectivePosition, float scaleMultiplier )
        {
            if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count > 0 || SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count > 0 )
                return;

            int incomingDamage = this.IncomingDamage.Display.IncomingPhysicalDamageTargeting;
            if ( incomingDamage > 0 )
            {
                int currentHealth = this.GetActorDataCurrent( ActorRefs.ActorHP, true );
                if ( currentHealth > 0 )
                {
                    int progressPercentage = MathA.IntPercentageClamped( incomingDamage, currentHealth, 0, 100 );
                    IA5Sprite progressIcon = IconRefs.DamageSprites[progressPercentage];

                    VisIconUsage style = IconRefs.IncomingDamageStyle;

                    progressIcon.WriteToDrawBufferForOneFrame( InputCaching.IsInInspectMode_Any, EffectivePosition + (CameraCurrent.CameraUp * (style.DefaultAddedY * scaleMultiplier)),
                        style.DefaultScale * 1.4f, style.DefaultColorHDR, false, false, true );
                }
            }
        }

        public bool GetIsMovingRightNow()
        {
            return this.isMoving;
        }

        public override bool GetIsVeryLowPriorityForLog()
        {
            return false;
        }
    }
}

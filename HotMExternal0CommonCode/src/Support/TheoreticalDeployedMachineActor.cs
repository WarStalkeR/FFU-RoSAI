using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.External
{
    public struct TheoreticalDeployedMachineActor : ISimMachineActor
    {
        public MobileActorTypeDuringGameData ActorDGD;
        public Vector3 Location;
        public MachineUnitStance UnitStance;
        public MachineVehicleStance VehicleStance;

        private Dictionary<ActorPerk, bool> StancePerksGranted()
        {
            if ( this.UnitStance != null )
                return this.UnitStance.PerksGranted;
            else
                return this.VehicleStance.PerksGranted;
        }

        private Dictionary<ActorPerk, bool> StancePerksBlocked()
        {
            if ( this.UnitStance != null )
                return this.UnitStance.PerksBlocked;
            else
                return this.VehicleStance.PerksBlocked;
        }

        public int CurrentActionPoints => throw new NotImplementedException();

        public int MaxActionPoints => throw new NotImplementedException();

        public StandbyType CurrentStandby { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ActionOverTime CurrentActionOverTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ActionOverTime AlmostCompleteActionOverTime { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int StartingHealthAtTheEndOfTheTurn => throw new NotImplementedException();

        public SecurityClearance CurrentBaseClearance { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public NPCCohort CurrentRegistration { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AbilityType IsInAbilityTypeTargetingMode => throw new NotImplementedException();

        public ResourceConsumable IsInConsumableTargetingMode => throw new NotImplementedException();

        public FogOfWarCutter FogOfWarCutting { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public MaterializeType Materializing { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public float MaterializingProgress { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IAutoPooledFloatingText FloatingText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public SidebarItemFromOther<ISimMapActor> SidebarItem_MainMapActor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int ActorID => -234322;

        public int CurrentTurnSeed => throw new NotImplementedException();

        public int TurnsHasExisted => throw new NotImplementedException();

        public ThreadsafeTableDictionaryView<ActorDataType, MapActorData> ActorData => throw new NotImplementedException();

        public bool IsInvalid => false;

        public int OutcastLevel 
        {
            get
            {
                return this.ActorDGD.CalculateOutcastLevelForUnitThatDoesNotYetExist( this.StancePerksGranted(), this.StancePerksBlocked() );
            }
            set => throw new NotImplementedException(); 
        }

        public bool IsTakingCover => false;

        public bool IsCloaked => false;

        public int IsUnremarkableAnywhereUpToClearanceInt => this.ActorDGD.CalculateIsUnremarkableAnywhereUpToClearanceInt( this.StancePerksGranted(), this.StancePerksBlocked() );
        public int IsUnremarkableBuildingOnlyUpToClearanceInt => this.ActorDGD.CalculateIsUnremarkableAnywhereUpToClearanceIntBuildingOnly( this.StancePerksGranted(), this.StancePerksBlocked() );

        public bool IsFullDead => false;

        public DoubleBufferedClass<NPCIncomingDamage> IncomingDamage => throw new NotImplementedException();

        public int IncomingDamageActual => throw new NotImplementedException();

        public NPCRevealer NPCRevealing { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DamageTextPopups.FloatingDamageTextPopup MostRecentDamageText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int SortPriority => throw new NotImplementedException();

        public int SortID => throw new NotImplementedException();

        public int NonSimUniqueID => throw new NotImplementedException();

        public bool ShouldFreezeCameraAtTheMoment => throw new NotImplementedException();

        public string DebugText { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsTrackedByCriminalSyndicates => false;
        public bool IsTrackedByGangs => false;
        public bool IsTrackedByReligions => false;
        public bool IsTrackedByCults => false;
        public bool IsTrackedByRebels => false;
        public bool DoesNotLoseDeterrenceOrProtectionFromLostHealth => false;

        public int IncomingPhysicalDamageActual => throw new NotImplementedException();

        public int IncomingMoraleDamageActual => throw new NotImplementedException();

        public bool DrawAsInvisibleEvenThoughNotCloaked => false;

        public int StatusEffectCount => 0;
        public int StatusEffectCountToShowByHealthBar => 0;

        public bool HasMovedThisTurn => false;
        public int TurnsSinceMoved => 0;

        public void AddOrRemoveBadge( ActorBadge Badge, bool Add )
        {
            throw new NotImplementedException();
        }

        public void AddOrRemoveBadge( string BadgeName, bool Add )
        {
            throw new NotImplementedException();
        }

        public void AddStatus( ActorStatus Status, int Intensity, int TurnsToLast )
        {
            throw new NotImplementedException();
        }

        public void AlterActorDataCurrent( ActorDataType Type, int AmountToAdd, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public void AlterActorDataCurrent( string TypeName, int AmountToAdd, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public void AlterAggroOfNPCCohort( NPCCohort Cohort, int AmountToAdd )
        {
            throw new NotImplementedException();
        }

        public void AlterCurrentActionPoints( int ByAmount )
        {
            throw new NotImplementedException();
        }

        public void AlterIncomingDamageActual( int Added )
        {
            throw new NotImplementedException();
        }

        public void AppendExtraDetailsForDataType( ArcenDoubleCharacterBuffer Buffer, MapActorData DataData )
        {
            throw new NotImplementedException();
        }

        public void AppendIncomingDamageLines( ArcenCharacterBufferBase Buffer, bool doFull )
        {
            throw new NotImplementedException();
        }

        public void ApplyVisibilityFromAction( ActionVisibility VisibilityStyle )
        {
            throw new NotImplementedException();
        }

        public void CalculateEffectiveClearances( MapCell cellOfUnit, out int MaxGroundClearance, out int MaxBuildingClearance )
        {
            throw new NotImplementedException();
        }

        public MapCell CalculateMapCell()
        {
            throw new NotImplementedException();
        }

        public MapDistrict CalculateMapDistrict()
        {
            throw new NotImplementedException();
        }

        public MapPOI CalculatePOI()
        {
            throw new NotImplementedException();
        }

        public int CalculateTurnsHasStatusFor( ActorStatus Status )
        {
            throw new NotImplementedException();
        }

        public bool CanMakeMeleeAttacks()
        {
            throw new NotImplementedException();
        }

        public bool CanMakeRangedAttacks()
        {
            throw new NotImplementedException();
        }

        public void ClearAggroedNPCCohorts()
        {
            throw new NotImplementedException();
        }

        public void ClearStatus( ActorStatus Status )
        {
            throw new NotImplementedException();
        }

        public void DoDeathCheck( MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            throw new NotImplementedException();
        }

        public void DoOnFocus_CityMap()
        {
            throw new NotImplementedException();
        }

        public void DoOnFocus_Streets()
        {
            throw new NotImplementedException();
        }

        public void DoOnPostTakeDamage( ISimMapActor DamageSource, int PhysicalDamageAmount, int MoraleDamageAmount, int SquadMembersLost, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
        {
            throw new NotImplementedException();
        }

        public void DoOnSelectByPlayer()
        {
            throw new NotImplementedException();
        }

        public ISimBuilding FindClosestBuildingWithinRange( float WithinRange )
        {
            throw new NotImplementedException();
        }

        public ISimBuilding FindClosestBuildingWithinRange( float WithinRange, Predicate<ISimBuilding> ExtraConditions )
        {
            throw new NotImplementedException();
        }

        public void FireWeaponsAtTargetPoint( Vector3 targetPoint, MersenneTwister Rand, Action OnFinalHit )
        {
            throw new NotImplementedException();
        }

        public bool GetActorAbilityCanBePerformedNow( AbilityType AbilityType, ArcenDoubleCharacterBuffer BufferOrNull )
        {
            throw new NotImplementedException();
        }

        public AbilityType GetActorAbilityInSlotIndex( int SlotIndex )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataCurrent( ActorDataType Type, bool OkayIfNull )
        {
            return this.ActorDGD.NonSimActorDataMinMax.GetDisplayDict()[Type].LeftItem;
        }

        public int GetActorDataCurrent( string TypeName, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public MapActorData GetActorDataData( ActorDataType Type, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public MapActorData GetActorDataData( string TypeName, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public MapActorData GetActorDataDataAndInitializeIfNeedBe( ActorDataType Type, int Current, int Maximum )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataLostFromMax( ActorDataType Type, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataLostFromMax( string TypeName, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataMaximum( ActorDataType Type, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataMaximum( string TypeName, bool OkayIfNull )
        {
            throw new NotImplementedException();
        }

        public bool GetActorHasAbilityEquipped( AbilityType Ability )
        {
            throw new NotImplementedException();
        }

        public Vector3 GetActualPositionForMovementOrPlacement()
        {
            return Location;
        }

        public bool GetAmIAVeryLowPriorityTargetRightNow()
        {
            throw new NotImplementedException();
        }

        public int GetAmountHasAggroedNPCCohort( NPCCohort Group )
        {
            return 0;
        }

        public float GetAreaOfAttack()
        {
            throw new NotImplementedException();
        }

        public float GetAreaOfAttackIntensityMultiplier()
        {
            throw new NotImplementedException();
        }

        public float GetAreaOfAttackSquared()
        {
            throw new NotImplementedException();
        }

        public float GetAttackRange()
        {
            throw new NotImplementedException();
        }

        public float GetAttackRangeSquared()
        {
            throw new NotImplementedException();
        }

        public ListView<ISimBuilding> GetBoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob()
        {
            throw new NotImplementedException();
        }

        public MaterializeType GetBurnDownType()
        {
            throw new NotImplementedException();
        }

        public string GetCollidableTypeID()
        {
            throw new NotImplementedException();
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return this.ActorDGD?.GetCollidableSmashesBuildingStrength()??0;
        }

        public Vector3 GetCollisionCenter()
        {
            return Location;
        }

        public int GetCombinedFromChildrenActorDataCurrent( ActorDataType Type )
        {
            throw new NotImplementedException();
        }

        public int GetCombinedFromChildrenActorDataCurrent( string TypeName )
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetDrawLocation()
        {
            return Location;
        }

        public int GetEffectiveClearance( ClearanceCheckType CheckType )
        {
            if ( CheckType == ClearanceCheckType.MovingToBuilding )
                return this.ActorDGD.CalculateIsUnremarkableAnywhereUpToClearanceIntBuildingOnly( this.StancePerksGranted(), this.StancePerksBlocked() );
            else
                return this.ActorDGD.CalculateIsUnremarkableAnywhereUpToClearanceInt( this.StancePerksGranted(), this.StancePerksBlocked() );
        }

        public Vector3 GetEmissionLocation()
        {
            throw new NotImplementedException();
        }

        public bool GetEquals( ICollidable other )
        {
            throw new NotImplementedException();
        }

        public float GetExtraRadiusBufferWhenTestingForNew()
        {
            throw new NotImplementedException();
        }

        public float GetFeatAmount( ActorFeat Feat )
        {
            throw new NotImplementedException();
        }

        public float GetFeatAmount( string FeatName )
        {
            throw new NotImplementedException();
        }

        public FeatSetForType GetFeatSetOrNull()
        {
            return this.ActorDGD.FeatSet;
        }

        public float GetHalfHeightForCollisions()
        {
            throw new NotImplementedException();
        }

        public bool GetHasAggroedAnyNPCCohort()
        {
            throw new NotImplementedException();
        }

        public bool GetHasAreaOfAttack()
        {
            throw new NotImplementedException();
        }

        public bool GetHasBadge( ActorBadge Badge )
        {
            throw new NotImplementedException();
        }

        public bool GetHasBadge( string BadgeName )
        {
            throw new NotImplementedException();
        }

        public bool GetHasPerk( ActorPerk Perk )
        {
            throw new NotImplementedException();
        }

        public bool GetHasPerk( string PerkName )
        {
            throw new NotImplementedException();
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
                case ClearanceCheckType.MovingToBuilding:
                    {
                        if ( this.IsUnremarkableBuildingOnlyUpToClearanceInt >= 0 )
                            return true;
                    }
                    break;
            }

            return false;
        }
        #endregion

        public bool GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation( NPCCohort FromCohort, ISimBuilding BuildingOrNull, Vector3 Location, bool WillHaveDoneAttack, bool SkipInconspicuousCheck )
        {
            if ( this.ActorDGD.ParentVehicleTypeOrNull != null )
                return MachineActorHelper.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Vehicle( FromCohort, BuildingOrNull, Location, WillHaveDoneAttack, SkipInconspicuousCheck,
                    false, 0, this.IsUnremarkableAnywhereUpToClearanceInt );
            else
            {
                ClearanceCheckType clearanceCheck = BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding;

                return MachineActorHelper.GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Unit( FromCohort, BuildingOrNull, Location, WillHaveDoneAttack, SkipInconspicuousCheck,
                    this.IsCloaked, this.GetAmountHasAggroedNPCCohort( FromCohort ),
                    this.GetIsUnremarkableRightNow_NotCountingActualClearanceChecks( clearanceCheck ),
                    this.GetEffectiveClearance( clearanceCheck ) );
            }
        }

        public bool GetIsBlockedFromActingForAbility( AbilityType Ability, bool CareAboutAPAndMentalEnergy )
        {
            return true;
        }

        public bool GetIsBlockedFromActingForTurnCompletePurposes()
        {
            throw new NotImplementedException();
        }

        public bool GetIsBlockedFromActingInGeneral()
        {
            throw new NotImplementedException();
        }

        public bool GetIsBlockedFromActingForMovementPurposes()
        {
            throw new NotImplementedException();
        }

        public bool GetIsBlockedFromAutomaticSelection( bool CareAboutAP )
        {
            throw new NotImplementedException();
        }

        public bool GetIsCurrentlyAbleToAvoidAutoTargetingShotAt_CurrentLocation( NPCCohort FromCohort, bool SkipInconspicuousCheck )
        {
            throw new NotImplementedException();
        }

        public bool GetIsCurrentlyInvisible( InvisibilityPurpose ForPurpose )
        {
            return false;
        }

        public bool GetIsHuman()
        {
            throw new NotImplementedException();
        }

        public bool GetIsInActorCollection( ActorCollection Collection )
        {
            throw new NotImplementedException();
        }

        public bool GetIsInFogOfWar()
        {
            throw new NotImplementedException();
        }

        public bool GetIsMovingRightNow()
        {
            throw new NotImplementedException();
        }

        public bool GetIsOverCap()
        {
            throw new NotImplementedException();
        }

        public bool GetIsPartOfPlayerForcesInAnyWay()
        {
            return true;
        }

        public bool GetIsAnAllyFromThePlayerPerspective()
        {
            return true;
        }

        public bool GetIsRelatedToPlayerShellCompany()
        {
            return this.ActorDGD?.IsTiedToShellCompany ?? false;
        }

        public bool GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer()
        {
            return this.ActorDGD?.IsTiedToShellCompany ?? false;
        }

        public bool GetIsPlayerControlled()
        {
            return true;
        }

        public bool GetIsStatusBlockedByBadges( ActorStatus Status )
        {
            throw new NotImplementedException();
        }

        public bool GetIsValidForCollisions()
        {
            throw new NotImplementedException();
        }

        public bool GetIsValidToAutomaticallyShootAt_Current( ISimMapActor Target )
        {
            throw new NotImplementedException();
        }

        public bool GetIsValidToAutomaticallyShootAt_SameLocation( ISimMapActor Target, bool WillBeShiftedToTakingCover, bool WillBeShiftedToCloaked )
        {
            throw new NotImplementedException();
        }

        public bool GetIsValidToCatchInAreaOfEffectExplosion_Current( ISimMapActor Target )
        {
            throw new NotImplementedException();
        }

        public bool GetIsVeryLowPriorityForLog()
        {
            throw new NotImplementedException();
        }

        public ConcurrentDictionary<NPCManager, int> GetManagersBlockedUntilTurns()
        {
            throw new NotImplementedException();
        }

        public VisParticleAndSoundUsage GetMeleeAttackSoundAndParticles()
        {
            throw new NotImplementedException();
        }

        public float GetMovementRange()
        {
            throw new NotImplementedException();
        }

        public float GetMovementRangeSquared()
        {
            throw new NotImplementedException();
        }

        public float GetNPCRevealRange()
        {
            throw new NotImplementedException();
        }

        public float GetNPCRevealRangeSquared()
        {
            throw new NotImplementedException();
        }

        public int GetPercentBiological()
        {
            throw new NotImplementedException();
        }

        public int GetPercentRobotic()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCameraFocus()
        {
            return Location;
        }

        public Vector3 GetPositionForCollisions()
        {
            return Location.PlusY( this.ActorDGD?.GetYOffsetForCollisionBase() ?? 0 );
        }

        public Vector3 GetBottomCenterPosition()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            return TheoreticalPoint.PlusY( this.ActorDGD?.GetYOffsetForCollisionBase() ?? 0 );
        }

        public float GetRadiusForCollisions()
        {
            throw new NotImplementedException();
        }

        public VisParticleAndSoundUsage GetRangedAttackSoundAndParticles()
        {
            throw new NotImplementedException();
        }

        public float GetRotationYForCollisions()
        {
            throw new NotImplementedException();
        }

        public IA5Sprite GetShapeIcon()
        {
            throw new NotImplementedException();
        }

        public string GetShapeIconColorString()
        {
            throw new NotImplementedException();
        }

        public ProtectedList<ShotEmissionGroup> GetShotEmissionGroups()
        {
            throw new NotImplementedException();
        }

        public bool GetShouldBeTargetForFriendlyAOEAbilities()
        {
            throw new NotImplementedException();
        }

        public bool GetShouldHideIntersectingDecorations()
        {
            throw new NotImplementedException();
        }

        public float GetSquaredRadiusForCollisions()
        {
            throw new NotImplementedException();
        }

        public int GetStackCountOfStatus( ActorStatus Status )
        {
            return 0;
        }

        public int GetStatusIntensity( ActorStatus Status )
        {
            return 0;
        }

        public List<SubCollidable> GetSubCollidablesOrNull()
        {
            return null;
        }

        public IA5Sprite GetTooltipIcon()
        {
            throw new NotImplementedException();
        }

        public VisColorUsage GetTooltipIconColorStyle()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetTransformedShotEmissionPoint( ShotEmissionPoint Point )
        {
            throw new NotImplementedException();
        }

        public ArcenDynamicTableRow GetTypeAsRow()
        {
            throw new NotImplementedException();
        }

        public MobileActorTypeDuringGameData GetTypeDuringGameData()
        {
            return this.ActorDGD;
        }

        public string GetTypeName()
        {
            throw new NotImplementedException();
        }

        public bool GetWillFireOnMachineUnitsBaseline()
        {
            throw new NotImplementedException();
        }

        public bool GetWouldBeDeadFromIncomingDamageActual()
        {
            throw new NotImplementedException();
        }

        public void MarkParticleLoopActiveFromMainFrame( ActorParticleLoopReason Reason )
        {
            throw new NotImplementedException();
        }

        public void PlayBulletHitEffectAtPositionForCollisions()
        {
            throw new NotImplementedException();
        }

        public void RecalculateStatusIntensities( bool ClearIfNonePresent )
        {
            throw new NotImplementedException();
        }

        public void RenderColliderIcon( IconLogic Logic )
        {
            throw new NotImplementedException();
        }

        public void RerollCurrentTurnSeed()
        {
            throw new NotImplementedException();
        }

        public void SetTargetingMode( AbilityType AbilityType, ResourceConsumable Consumable )
        {
            throw new NotImplementedException();
        }

        public void TestFireWeapons( MersenneTwister Rand )
        {
            throw new NotImplementedException();
        }

        public void TriggerTargetingPassAfterThisRecalculatesPerSecondPerks()
        {
            throw new NotImplementedException();
        }

        public void TryAltViewActorAbilityInSlot( int SlotIndex )
        {
            throw new NotImplementedException();
        }

        public ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, MachineUnitType UnitTypeToGrant, string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanBeInIrradiated )
        {
            throw new NotImplementedException();
        }

        public ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToThisOne( MachineUnitType UnitTypeToGrant, string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanBeInIrradiated )
        {
            throw new NotImplementedException();
        }

        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToThis( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public ISimNPCUnit TryCreateNewNPCUnitWithinThisRadius( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, float Radius, int MaxDesiredClearance, CellRange FailoverCRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public ISimMachineVehicle TryCreateNewVehicleAsCloseAsPossibleToThisOne( MachineVehicleType VehicleTypeToGrant, string NewVehicleOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanBeInIrradiated )
        {
            throw new NotImplementedException();
        }

        public void TryPerformActorAbilityInSlot( int SlotIndex, TriggerStyle Style )
        {
            throw new NotImplementedException();
        }

        public bool TryScrapRightNowWithoutWarning_Danger( ScrapReason Reason )
        {
            return true;
        }

        public bool PopupReasonCannotScrapIfCannot( ScrapReason Reason )
        {
            return true;
        }

        public void RenderTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp, TooltipShadowStyle ShadowStyle, bool ShouldBeAtMouseCursor,
            ActorTooltipExtraData ExtraData, TooltipExtraRules ExtraRules )
        {
            throw new NotImplementedException();
        }

        public bool GetIsTrackedByCohort( NPCCohort OtherCohort )
        {
            return false;
        }

        public bool GetIsBlockedFromBeingScrappedRightNow()
        {
            return false;
        }

        public void AlterIncomingPhysicalDamageActual( int Added )
        {
        }

        public bool GetWouldBeDeadFromIncomingPhysicalDamageActual()
        {
            return false;
        }

        public void AlterIncomingMoraleDamageActual( int Added )
        {
        }

        public bool GetCanTakeMoraleDamage()
        {
            return false;
        }

        public bool GetWouldDisbandFromIncomingMoraleDamageActual()
        {
            return false;
        }

        public ThreadsafeTableDictionary32<ActorStatus> GetStatusIntensities()
        {
            throw new NotImplementedException();
        }
    }
}
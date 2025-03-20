using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.External
{
    public struct TheoreticalDeployedNPCUnit : ISimNPCUnit
    {
        public NPCUnitType UnitType;
        public NPCCohort Cohort;
        public NPCUnitStance Stance;
        public Vector3 Location;

        public NPCCohort FromCohort => Cohort;

        public int CurrentSquadSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int TurnsInCurrentStance => 0;

        public Vector3 CreationPosition => Location;

        public string CreationReason => throw new NotImplementedException();

        public NPCUnitObjective CurrentObjective { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public NPCUnitObjective NextObjective { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public WrapperedSimBuilding ObjectiveBuilding { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISimMapActor ObjectiveActor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public DoubleBufferedClass<NPCAttackPlan> AttackPlan => throw new NotImplementedException();

        public NPCManager ParentManager => null;

        public WrapperedSimBuilding ManagerStartLocation => throw new NotImplementedException();

        public int ManagerOriginalMachineActorFocusID => throw new NotImplementedException();

        public NPCManagedUnit IsManagedUnit => null;
        public CityConflictUnit IsCityConflictUnit => null;

        public void DoAlternativeToDeathPositiveItems( ISimMapActor DamageSource, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging ) { }

        public int TurnsSinceMoved => 0;

        public int ObjectiveProgressPoints { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ObjectiveFailureWaitTurns { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool ObjectiveIsCompleteAndWaiting { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsNPCInFogOfWar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public NPCActionDesire WantsToPerformAction { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool OverridingActionWillWarpOut { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string LangKeyForDisbandPopup { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool NextMoveIsSilent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DisbandAtTheStartOfNextTurn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DisbandAsSoonAsNotSelected { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool DisbandWhenObjectiveComplete { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool HasAttackedYetThisTurn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string SpecialtyActionStringToBeDoneNext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public MapPOI HomePOI { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public MapDistrict HomeDistrict { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public VisSimpleDrawingObject CustomVisSimpleObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public VisLODDrawingObject CustomVisLODObject { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<string> TurnDumpLines => throw new NotImplementedException();

        public List<string> TargetingDumpLines => throw new NotImplementedException();

        public Vector3 PriorLocation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Quaternion PriorRotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISimMapActor TargetActor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Vector3 TargetLocation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public WrapperedSimUnitLocation ContainerLocation => throw new NotImplementedException();

        public Vector3 GroundLocation => throw new NotImplementedException();

        public int TurnsSinceDidAttack => throw new NotImplementedException();

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
                return this.UnitType.DuringGameData.CalculateOutcastLevelForUnitThatDoesNotYetExist( this.Stance.PerksGranted, this.Stance.PerksBlocked );
            }
            set => throw new NotImplementedException();
        }

        public bool IsTakingCover => false;

        public bool IsCloaked => false;

        public int IsUnremarkableAnywhereUpToClearanceInt => this.UnitType.DuringGameData.CalculateIsUnremarkableAnywhereUpToClearanceInt( this.Stance.PerksGranted, this.Stance.PerksBlocked );
        public int IsUnremarkableBuildingOnlyUpToClearanceInt => this.UnitType.DuringGameData.CalculateIsUnremarkableAnywhereUpToClearanceIntBuildingOnly( this.Stance.PerksGranted, this.Stance.PerksBlocked );

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

        NPCUnitType ISimNPCUnit.UnitType => this.UnitType;

        NPCUnitStance ISimNPCUnit.Stance { get => this.Stance; set { } }

        public bool IsTrackedByCriminalSyndicates => false;
        public bool IsTrackedByGangs => false;
        public bool IsTrackedByReligions => false;
        public bool IsTrackedByCults => false;
        public bool IsTrackedByRebels => false;
        public bool DoesNotLoseDeterrenceOrProtectionFromLostHealth => false;

        public CityConflict ParentCityConflict => throw new NotImplementedException();

        public int IncomingPhysicalDamageActual => throw new NotImplementedException();

        public int IncomingMoraleDamageActual => throw new NotImplementedException();

        public bool DrawAsInvisibleEvenThoughNotCloaked => false;

        public int StatusEffectCount => 0;

        public int StatusEffectCountToShowByHealthBar => 0;

        public int LastTurnDidAmbush { get => 0; set { } }

        public int LargestSquadSize => 0;

        public bool HasBeenPhysicallyDamagedByPlayer { get => false; set {  } }
        public bool HasBeenPhysicallyOrMoraleDamagedByPlayer { get => false; set { } }

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

        public void AlterAccumulatorAmount( NPCUnitAccumulator Accumulator, int ByAmount )
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

        public bool AttackChosenTarget_MainThreadOnly( MersenneTwister RandThatWillBeReset )
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

        public int CalculateSquadSizeLossFromDamage( int DamageAmount )
        {
            throw new NotImplementedException();
        }

        public int CalculateTheoreticalStatStrengthIfWeImagineThisAmountOfLostHealth( ActorDataType DataType, int DamageAmount )
        {
            throw new NotImplementedException();
        }

        public int CalculateTurnsHasStatusFor( ActorStatus Status )
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

        public void ConvertEnemyRobotToPlayerForces()
        {
            throw new NotImplementedException();
        }

        public void DisbandNPCUnit( NPCDisbandReason Reason )
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

        public void DoOnPostHitWithHostileAction( ISimMapActor HostileActionSource, int IntensityOfNegativeAction, MersenneTwister Rand, bool ShouldDoDamageTextPopupsAndLogging )
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

        public void DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, long FramesPrepped, out bool TooltipShouldBeAtCursor, out bool ShouldSkipDrawing, out bool ShouldDrawAsSilhouette )
        {
            throw new NotImplementedException();
        }

        public bool EqualsRelated( IAutoRelatedObject Other )
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

        public int GetAccumulatorAmount( NPCUnitAccumulator Accumulator )
        {
            throw new NotImplementedException();
        }

        public int GetActorDataCurrent( ActorDataType Type, bool OkayIfNull )
        {
            return this.UnitType.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[Type].LeftItem;
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

        public Vector3 GetActualPositionForMovementOrPlacement()
        {
            throw new NotImplementedException();
        }

        public bool GetAmIAVeryLowPriorityTargetRightNow()
        {
            throw new NotImplementedException();
        }

        public int GetAmountHasAggroedNPCCohort( NPCCohort Group, NPCUnitStance Stance1OrNull, NPCUnitStance Stance2OrNull )
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
            return this.UnitType?.DestroyIntersectingBuildingsStrength??0;
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

        public bool GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject, out IAutoPooledFloatingObject floatingSimpleObject, out Color color )
        {
            throw new NotImplementedException();
        }

        public string GetDisplayName()
        {
            throw new NotImplementedException();
        }

        public bool GetDoesRevealingForPlayer()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetDrawLocation()
        {
            return Location;
        }

        public Quaternion GetDrawRotation()
        {
            throw new NotImplementedException();
        }

        public int GetEffectiveClearance( ClearanceCheckType CheckType )
        {
            if ( CheckType == ClearanceCheckType.MovingToBuilding )
                return this.UnitType.DuringGameData.CalculateIsUnremarkableAnywhereUpToClearanceIntBuildingOnly( this.Stance.PerksGranted, this.Stance.PerksBlocked );
            else
                return this.UnitType.DuringGameData.CalculateIsUnremarkableAnywhereUpToClearanceInt( this.Stance.PerksGranted, this.Stance.PerksBlocked );
        }

        public Vector3 GetEmissionLocation()
        {
            return Location;
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
            return this.UnitType.DuringGameData.FeatSet;
        }

        public float GetHalfHeightForCollisions()
        {
            throw new NotImplementedException();
        }

        public bool GetHasAggroedAnyNPCCohort()
        {
            throw new NotImplementedException();
        }

        public bool GetHasAnyActionItWantsTodo()
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

        public int GetInt32ValueForSerialization()
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

        public bool GetIsNPCActionStillValid()
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

        public bool GetIsPlayerControlled()
        {
            return true;
        }

        public bool GetIsRelatedToPlayerShellCompany()
        {
            return this.UnitType?.IsTiedToShellCompany ?? false;
        }

        public bool GetIsRelatedToPlayerShellCompanyOrCompletelyUnrelatedToPlayer()
        {
            return this.UnitType?.IsTiedToShellCompany ?? false;
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

        public bool GetIsValidToAutomaticallyShootAt_TheoreticalOtherLocation( ISimMapMobileActor Target, MapPOI TheoreticalLocationPOI, ISimBuilding TheoreticalBuildingOrNull, Vector3 TheoreticalLocation, MapPOI TheoreticalAggroedPOI, NPCCohort TheoreticalAggroedCohort, bool WillHaveDoneAttack )
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

        public float GetMovementRange()
        {
            throw new NotImplementedException();
        }

        public float GetMovementRangeSquared()
        {
            throw new NotImplementedException();
        }

        public bool GetMustStayOnGround()
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
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCollisions()
        {
            return Location.PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
        }

        public Vector3 GetBottomCenterPosition()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            return TheoreticalPoint.PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
        }

        public float GetRadiusForCollisions()
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

        public bool GetShouldActionsBeWatchedByPlayer()
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
            return this.UnitType?.DuringGameData;
        }

        public string GetTypeName()
        {
            throw new NotImplementedException();
        }

        public bool GetWillFireOnMachineUnitsBaseline()
        {
            return this.FromCohort?.GetWillFireOnMachineUnits() ?? false;
        }

        public bool GetIsConsideredHostileToPlayer()
        {
            return false; //if we're placing it, then no
        }

        public bool GetWouldBeDeadFromIncomingDamageActual()
        {
            return false; //if we're placing it, then no
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

        public void RotateObjectToFace( Vector3 effectivePosition, Vector3 otherPosition, float maxDelta )
        {
            throw new NotImplementedException();
        }

        public void SetActualContainerLocation( ISimUnitLocation Loc )
        {
            throw new NotImplementedException();
        }

        public void SetActualGroundLocation( Vector3 GroundLocation )
        {
            throw new NotImplementedException();
        }

        public void SetDesiredContainerLocation( ISimUnitLocation Loc )
        {
            throw new NotImplementedException();
        }

        public void SetDesiredGroundLocation( Vector3 GroundLocation )
        {
            throw new NotImplementedException();
        }

        public bool StartNPCAction( MersenneTwister RandThatWillBeReset )
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

        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToBuilding( ISimBuilding BuildingToBeCloseTo, NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToThisOne( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public ISimNPCUnit TryCreateNewNPCUnitWithinThisRadius( NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, float Radius, int MaxDesiredClearance, CellRange FailoverCRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            throw new NotImplementedException();
        }

        public void WipeAllObjectiveData()
        {
            throw new NotImplementedException();
        }

        public void WriteAnyActionItWantsTodo( ArcenCharacterBufferBase Buffer )
        {
            throw new NotImplementedException();
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
            return this.GetActorDataCurrent( ActorRefs.UnitMorale, true ) > 0;
        }

        public bool GetWouldDisbandFromIncomingMoraleDamageActual()
        {
            return false;
        }

        public ThreadsafeTableDictionary32<ActorStatus> GetStatusIntensities()
        {
            throw new NotImplementedException();
        }

        public void ChangeCohort( NPCCohort NewCohort )
        {
        }
    }
}
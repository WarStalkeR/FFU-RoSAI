using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class BasicActionsHelper
    {
        public static MersenneTwister workingRand = new MersenneTwister( 0 );
        private static DrawBag<MachineUnitType> unitTypeDrawBag = DrawBag<MachineUnitType>.Create_WillNeverBeGCed( 10, "BasicActionsHelper-unitTypeDrawBag" );

        #region InitializeWorkingRandToBuildingOnly
        public static MersenneTwister InitializeWorkingRandToBuildingOnly( ISimBuilding Building )
        {
            workingRand.ReinitializeWithSeed( Building.GetBuildingTurnRandomNumber() + Building.GetBuildingID() );
            return workingRand;
        }
        #endregion

        #region InitializeWorkingRandToBuildingAndUnit
        public static MersenneTwister InitializeWorkingRandToBuildingAndUnit( ISimBuilding Building, ISimMachineActor Actor )
        {
            workingRand.ReinitializeWithSeed( Building.GetBuildingTurnRandomNumber() + Building.GetBuildingID() + Actor.CurrentTurnSeed );
            return workingRand;
        }
        #endregion

        #region InitializeWorkingRandToAbilityAndActor
        public static MersenneTwister InitializeWorkingRandToAbilityAndActor( ISimMachineActor Actor, AbilityType Ability )
        {
            workingRand.ReinitializeWithSeed( Actor.CurrentTurnSeed + Ability.RowIndexNonSim );
            return workingRand;
        }
        #endregion

        #region InitializeWorkingRandToBuildingAndActor
        public static MersenneTwister InitializeWorkingRandToBuildingAndActor( ISimMachineActor Actor, ISimBuilding Building )
        {
            workingRand.ReinitializeWithSeed( Actor.CurrentTurnSeed + Building.GetBuildingTurnRandomNumber() );
            return workingRand;
        }
        #endregion

        #region CalcEffectiveActorWouldBecomeOutcast
        public static ActorBadge CalcEffectiveActorWouldBecomeOutcast( ISimMachineActor Actor, ISimBuilding Building )
        {
            if ( Building == null || Actor == null )
                return null;
            MapPOI restrictedPOI = Building.CalculateLocationPOI();
            if ( restrictedPOI == null )
                return null;
            return CalcEffectiveActorWouldBecomeOutcast( Actor, restrictedPOI, Building );
        }

        public static ActorBadge CalcEffectiveActorWouldBecomeOutcast( ISimMachineActor Actor, MapPOI POI, ISimBuilding BuildingOrNull )
        {
            if ( Actor == null ) 
                return null;
            if ( Actor.IsCloaked )
                return null; //if they are cloaked, nothing happens to them!

            if ( POI != null && POI.BuildingOrNull != null ) //if this is a poi that exists, and is not a single-building type
            {
                int requiredClearanceInt = POI.BuildingOrNull.CalculateLocationSecurityClearanceInt();
                if ( requiredClearanceInt <= 0 )
                    return null;

                int unitClearanceInt = Actor.GetEffectiveClearance( BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding );
                int clearanceDifference = requiredClearanceInt - unitClearanceInt;
                if ( clearanceDifference < 0 )
                {
                    //if we have more clearance than needed, no problem
                    return null;
                }
                else if ( clearanceDifference > 0 )
                {
                    SecurityClearance requiredClearance = SecurityClearanceTable.ByLevel[requiredClearanceInt];
                    if ( requiredClearance.AppliedOutcastBadgeIfActorComesHere != null )
                    {
                        if ( !Actor.GetHasBadge( requiredClearance.AppliedOutcastBadgeIfActorComesHere ) )
                            return requiredClearance.AppliedOutcastBadgeIfActorComesHere; //if we have less security clearance than desired, and it would give a badge we don't have, then say so
                    }
                }
            }
            return null;
        }
        #endregion

        #region CalcEffectiveDangerForActor
        public static int CalcEffectiveDangerForActor( ISimMachineActor Actor, ISimBuilding Building )
        {
            int requiredClearance = Building.CalculateLocationSecurityClearanceInt();
            return CalcEffectiveDangerForActor( Actor, requiredClearance, Building );
        }
        #endregion

        #region CalcEffectiveDangerForActor
        public static int CalcEffectiveDangerForActor( ISimMachineActor Actor, int RequiredClearanceInt, ISimBuilding BuildingOrNull )
        {
            int dangerLevel = 0;
            if ( RequiredClearanceInt > 0 )
            {
                int unitClearanceInt = Actor.GetEffectiveClearance( BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding );
                int clearanceDifference = RequiredClearanceInt - unitClearanceInt;
                if ( clearanceDifference < 0 )
                {
                    //if we have more clearance than needed, blank out the danger level
                    return 0;
                }
                else if ( clearanceDifference > 0 )
                {
                    //if we have not-enough clearance, then increase the danger level a lot
                    switch ( clearanceDifference )
                    {
                        case 1:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus1.IntMin;
                            break;
                        case 2:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus2.IntMin;
                            break;
                        case 3:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus3.IntMin;
                            break;
                        case 4:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus4.IntMin;
                            break;
                        case 5:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus5.IntMin;
                            break;
                        default:
                            dangerLevel += MathRefs.ExtraDangerFromSecurityClearancePlus6OrMore.IntMin;
                            break;
                    }
                }
            }
            return dangerLevel;
        }
        #endregion

        #region ApplyOutcastBadgeToActor
        public static void ApplyOutcastBadgeToActor( ISimMachineActor Actor, ActorBadge Badge )
        {
            if ( Actor.IsCloaked )
                return; //if they are cloaked, nothing happens to them!

            if ( Actor.OutcastLevel >= 1 )
                return; //if already outcast a certain amount, don't try again

            Actor.AddOrRemoveBadge( Badge, true );
            //Actor.IsOutcast = true; should not be needed
        }
        #endregion

        //#region Calc_RecruitAndroid
        //public static void Calc_RecruitAndroid( ISimMachineActor Actor, ISimBuilding Building, out MachineUnitType UnitType )
        //{            
        //    unitTypeDrawBag.Clear();
        //    unitTypeDrawBag.AddItem( CommonRefs.CombatUnitUnitType, 60 );
        //    unitTypeDrawBag.AddItem( CommonRefs.TechnicianUnitType, 40 );
        //    unitTypeDrawBag.AddItem( CommonRefs.NickelbotUnitType, 60 );

        //    InitializeWorkingRandToBuildingAndUnit( Building, Actor );

        //    UnitType = unitTypeDrawBag.PickRandom( workingRand );
        //}
        //#endregion

        #region Calc_RandBuildingOnlyIntInclusive
        public static int Calc_RandBuildingOnlyIntInclusive( ISimBuilding Building, int minValue, int maxValue )
        {
            InitializeWorkingRandToBuildingOnly( Building );
            return workingRand.NextInclus( minValue, maxValue );
        }
        #endregion

        #region Calc_RandBuildingAndUnitIntInclusive
        public static int Calc_RandBuildingAndUnitIntInclusive( ISimBuilding Building, ISimMachineActor Actor, int minValue, int maxValue )
        {
            InitializeWorkingRandToBuildingAndUnit( Building, Actor );
            return workingRand.NextInclus( minValue, maxValue );
        }
        #endregion

        #region Calc_StealVehicle
        public static void Calc_StealVehicle( ISimBuilding Building, out MachineVehicleType VehicleType )
        {
            VehicleType = CommonRefs.DeliveryCraftVehicleType;
        }
        #endregion

        #region RenderTurnsInfo
        public static void RenderTurnsInfo( ActionOverTime Action, ArcenCharacterBufferBase Buffer, ActionOverTimeLogic Logic, int TurnsToTake )
        {
            switch ( Logic )
            {
                case ActionOverTimeLogic.PredictFloatingText:
                case ActionOverTimeLogic.PredictSidebarButtonText:
                case ActionOverTimeLogic.PredictTurnsRemainingText:
                    if ( Logic == ActionOverTimeLogic.PredictSidebarButtonText )
                        Buffer.AddRaw( Action.Type.GetDisplayName() );
                    else
                        Buffer.AddSpriteStyled_NoIndent( Action.Type.Icon, AdjustedSpriteStyle.InlineSmaller095,
                            Action.Type.IconColor.ColorHexWithHDR );

                    {
                        if ( Logic == ActionOverTimeLogic.PredictFloatingText )
                            Buffer.Line();
                        else
                            Buffer.Space1x();

                        if ( !InputCaching.ShouldShowDetailedTooltips && Logic == ActionOverTimeLogic.PredictFloatingText )
                            Buffer.StartSize70();

                        Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RustDarker )
                            .AddRaw( TurnsToTake.ToStringWholeBasic(), ColorTheme.RustLighter );
                    }
                    break;
            }
        }
        #endregion

        #region FullHandleForceIntoConversation
        public static ActorAbilityResult FullHandleForceIntoConversation( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            switch ( Logic )
            {
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        bool isFull = Logic == ActorAbilityLogic.AppendToAbilityTooltip_Full;
                        if ( isFull )
                        {
                            bool hasDoneHeader = false;

                            MapCell cell = Actor.CalculateMapCell();
                            if ( cell != null )
                            {
                                float maxDist = Actor.GetAttackRangeSquared();
                                Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                                foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    if ( otherActor.IsFullDead || !(otherActor is ISimNPCUnit npc) )
                                        continue;
                                    int countOfDiscussions = npc?.IsManagedUnit?.GetDialogCountCanBeForcedInto() ?? 0;
                                    if ( countOfDiscussions == 0 )
                                        continue;

                                    float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                    if ( dist > maxDist )
                                        continue; //too far away, nevermind

                                    if ( !hasDoneHeader )
                                    {
                                        hasDoneHeader = true;
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "TargetsAvailable", ColorTheme.DataLabelWhite );
                                        novel.Main.AddRaw( otherActor.GetDisplayName(), ColorTheme.DataBlue );
                                    }
                                    else
                                        novel.Main.ListSeparator().AddRaw( otherActor.GetDisplayName(), ColorTheme.DataBlue );
                                }
                            }

                            if ( hasDoneHeader )
                            {
                                novel.Main.Line();
                                return ActorAbilityResult.OpenedInterface;
                            }
                            else
                            {
                                novel.Main.AddBoldLangAndAfterLineItemHeader( "TargetsAvailable", ColorTheme.DataLabelWhite );
                                novel.Main.AddRaw( LangCommon.None.Text, ColorTheme.DataBlue ).Line();
                                return ActorAbilityResult.PlayErrorSound;
                            }
                        }
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        bool hadAnyCouldTalkTo = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead || !(otherActor is ISimNPCUnit npc) )
                                    continue;
                                int countOfDiscussions = npc?.IsManagedUnit?.GetDialogCountCanBeForcedInto() ?? 0;
                                if ( countOfDiscussions == 0 )
                                    continue;

                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                hadAnyCouldTalkTo = true;
                                break;
                            }
                        }

                        if ( !hadAnyCouldTalkTo )
                        {
                            if ( Actor.IsInAbilityTypeTargetingMode == Ability )
                                Actor.SetTargetingMode( null, null );

                            if ( BufferOrNull != null && Logic == ActorAbilityLogic.CalculateIfAbilityBlocked )
                                BufferOrNull.AddLang( "NoUnitsWithinRangeAbleToBeForcedIntoConversation" );
                            return ActorAbilityResult.PlayErrorSound;
                        }

                        switch ( Logic )
                        {
                            case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                            case ActorAbilityLogic.ExecuteAbilityAutomated:
                            case ActorAbilityLogic.TriggerAbilityAltView:
                                {
                                    if ( Actor.IsInAbilityTypeTargetingMode == Ability )
                                        Actor.SetTargetingMode( null, null );
                                    else
                                        Actor.SetTargetingMode( Ability, null );
                                    break;
                                }
                        }
                        return ActorAbilityResult.OpenedInterface;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region FullHandleRepairNearby
        public static ActorAbilityResult FullHandleRepairNearby( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            int engineeringSkill = Actor.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true );
            int healthRepair = Mathf.CeilToInt( engineeringSkill * Ability.GetSingleFloatByID( "EngineeringSkillToHPRepaired", 1 ) );
            int structureRepair = healthRepair * MathRefs.HealingMultiplierForStructures.IntMin;

            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( !Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                    {
                        bool didAnyRepairs = false;

                        int totalStructureHealingDone = 0;
                        int totalUnitHealingDone = 0;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead && !(otherActor is MachineStructure) )
                                    continue;

                                if ( otherActor is ISimMachineActor machineActor)
                                {
                                    if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing??false )
                                        continue;
                                }

                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                HandleSpecificOtherActorForRepairs_Execute( otherActor, ref didAnyRepairs, ref totalStructureHealingDone, ref totalUnitHealingDone, healthRepair, structureRepair );

                                if ( otherActor is ISimMachineVehicle otherVehicle )
                                {
                                    foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                    {
                                        if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                            HandleSpecificOtherActorForRepairs_Execute( ridingUnit, ref didAnyRepairs, ref totalStructureHealingDone, ref totalUnitHealingDone, healthRepair, structureRepair );
                                    }
                                }
                            }
                        }

                        if ( didAnyRepairs )
                        {
                            if ( Actor is ISimMachineUnit machineUnit )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "UnitRepairNearby" ),
                                    NoteStyle.StoredGame, machineUnit.UnitType.ID, string.Empty, string.Empty, string.Empty, 
                                    totalStructureHealingDone, totalUnitHealingDone, 0, machineUnit.GetDisplayName(), string.Empty, string.Empty,
                                    SimCommon.Turn + 15 );
                            else if ( Actor is ISimMachineVehicle machineVehicle )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "VehicleRepairNearby" ),
                                    NoteStyle.StoredGame, machineVehicle.VehicleType.ID, string.Empty, string.Empty, string.Empty, 
                                    totalStructureHealingDone, totalUnitHealingDone, 0, machineVehicle.GetDisplayName(), string.Empty, string.Empty,
                                    SimCommon.Turn + 15 );
                        }

                        return didAnyRepairs ? ActorAbilityResult.SuccessDidFullAbilityNow : ActorAbilityResult.PlayErrorSound;
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddLangAndAfterLineItemHeader( "RepairStrengthFromEngineeringSkill", ColorTheme.CyanDim )
                            .AddRaw( healthRepair.ToStringWholeBasic() ).Line();

                        bool isFull = Logic == ActorAbilityLogic.AppendToAbilityTooltip_Full;
                        if ( isFull )
                        {
                            bool hasDoneHeader = false;

                            MapCell cell = Actor.CalculateMapCell();
                            if ( cell != null )
                            {
                                float attackRange = Actor.GetAttackRange();
                                Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();

                                DrawHelper.RenderCircle( actorSpot.ReplaceY( 0 ), attackRange,
                                    ColorRefs.MachineUnitHackLine.ColorHDR, 1f );

                                float maxDist = attackRange * attackRange;
                                foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    if ( otherActor.IsFullDead && !(otherActor is MachineStructure) )
                                        continue;
                                    float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                    if ( dist > maxDist )
                                        continue; //too far away, nevermind


                                    HandleSpecificOtherActorForRepairs_Prediction( otherActor, ref hasDoneHeader, novel, healthRepair, structureRepair, false );

                                    if ( otherActor is ISimMachineVehicle otherVehicle )
                                    {
                                        foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                        {
                                            if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                                HandleSpecificOtherActorForRepairs_Prediction( ridingUnit, ref hasDoneHeader, novel, healthRepair, structureRepair, true );
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        bool hadAnyCouldRepair = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead && !( otherActor is MachineStructure) )
                                    continue;
                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind
                                if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                    continue; //if not a friend, don't heal them
                                if ( otherActor.GetPercentRobotic() < 50 )
                                    continue; //if less than 50% robot, then also not repairable

                                int lostHealth = otherActor.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
                                if ( lostHealth > 0 )
                                {
                                    hadAnyCouldRepair = true;
                                    break;
                                }

                                if ( otherActor is ISimMachineVehicle otherVehicle )
                                {
                                    foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                    {
                                        if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                        {
                                            lostHealth = ridingUnit.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
                                            if ( lostHealth > 0 )
                                            {
                                                hadAnyCouldRepair = true;
                                                break;
                                            }
                                        }
                                    }
                                    if ( hadAnyCouldRepair )
                                        break;
                                }
                            }

                            if ( !hadAnyCouldRepair && Actor is ISimMachineVehicle mainVehicle )
                            {
                                foreach ( ISimMachineUnit ridingUnit in mainVehicle.GetStoredUnits() )
                                {
                                    if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                    {
                                        int lostHealth = ridingUnit.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
                                        if ( lostHealth > 0 )
                                        {
                                            hadAnyCouldRepair = true;
                                            break;
                                        }
                                    }
                                }
                                if ( hadAnyCouldRepair )
                                    break;
                            }
                        }

                        if ( !hadAnyCouldRepair )
                        {
                            if ( BufferOrNull != null )
                                BufferOrNull.AddLang( "NoDamagedUnitsToRepair" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region HandleSpecificOtherActorForRepairs_Prediction
        private static void HandleSpecificOtherActorForRepairs_Prediction( ISimMapActor otherActor, ref bool hasDoneHeader, NovelTooltipBuffer novel, int healthRepair, int structureRepair, bool IsRidingActor )
        {
            if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                return; //if not a friend, don't heal them
            if ( otherActor.GetPercentRobotic() < 50 )
                return; //if less than 50% robot, then also not repairable

            int lostHealth = (int)otherActor.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( lostHealth > 0 )
            {
                if ( !hasDoneHeader )
                {
                    hasDoneHeader = true;
                    novel.Main.Line().AddBoldLang( "RepairResults", ColorTheme.DataLabelWhite ).Line();
                }

                int effectiveHealthRepair = healthRepair;
                if ( otherActor is MachineStructure )
                    effectiveHealthRepair = structureRepair;

                int current = otherActor.GetActorDataCurrent( ActorRefs.ActorHP, true );
                int max = otherActor.GetActorDataMaximum( ActorRefs.ActorHP, true );
                int newHealth = MathA.Min( current + effectiveHealthRepair, max );
                novel.Main.AddRawAndAfterLineItemHeader( otherActor.GetDisplayName(), current <= 0 ? ColorTheme.RedOrange2 : ColorTheme.HealingGreen )
                    .AddFormat3( "NumberUpWithMax", current.ToStringWholeBasic(), newHealth.ToStringWholeBasic(), max.ToStringWholeBasic() ).Line();

                if ( !IsRidingActor )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.HealingAmount, otherActor.GetDrawLocation() );
            }
        }
        #endregion

        #region HandleSpecificOtherActorForRepairs_Execute
        private static void HandleSpecificOtherActorForRepairs_Execute( ISimMapActor otherActor, ref bool didAnyRepairs, ref int totalStructureHealingDone, 
            ref int totalUnitHealingDone, int healthRepair, int structureRepair )
        {
            if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                return; //if not a friend, don't heal them
            if ( otherActor.GetPercentRobotic() < 50 )
                return; //if less than 50% robot, then also not repairable

            int lostHealth = otherActor.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( lostHealth > 0 )
            {
                int effectiveHealthRepair = healthRepair;
                if ( otherActor is MachineStructure )
                    effectiveHealthRepair = structureRepair;

                effectiveHealthRepair = Mathf.Min( effectiveHealthRepair, lostHealth );

                didAnyRepairs = true;
                otherActor.AlterActorDataCurrent( ActorRefs.ActorHP, effectiveHealthRepair, true );

                if ( otherActor is MachineStructure otherStructure )
                {
                    JobHelper.HandleStructureAfterRepair( otherStructure );
                    totalStructureHealingDone += effectiveHealthRepair;
                }
                else
                    totalUnitHealingDone += effectiveHealthRepair;
            }
        }
        #endregion

        #region FullHandleNetworkShieldNearby
        public static ActorAbilityResult FullHandleNetworkShieldNearby( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            int netControlSkill = Actor.GetActorDataCurrent( ActorRefs.UnitNetControl, true );
            int damageNegated = Mathf.CeilToInt( netControlSkill * Ability.GetSingleFloatByID( "NetControlToDamageNegated", 1 ) );

            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( !Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                    {
                        bool didAnyShields = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                    continue;

                                if ( otherActor is ISimMachineActor machineActor )
                                {
                                    if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                        continue;
                                }
                                else
                                {
                                    if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                        continue;//if not a friend, don't heal them
                                }

                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                otherActor.AddStatus( StatusRefs.NetworkShield, damageNegated, 1 );
                                didAnyShields = true;

                                if ( otherActor is ISimMachineVehicle otherVehicle )
                                {
                                    foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                    {
                                        if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                        {
                                            ridingUnit.AddStatus( StatusRefs.NetworkShield, damageNegated, 1 );
                                            didAnyShields = true;
                                        }
                                    }
                                }
                            }
                        }

                        if ( didAnyShields )
                        {
                            //todo later possibly
                            //ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "UnitRepairNearby" ),
                            //    NoteStyle.StoredGame, machineUnit.UnitType.ID, string.Empty, string.Empty, string.Empty,
                            //    totalStructureHealingDone, totalUnitHealingDone, 0, machineUnit.GetDisplayName(), string.Empty, string.Empty,
                            //    SimCommon.Turn + 15 );
                        }

                        return didAnyShields ? ActorAbilityResult.SuccessDidFullAbilityNow : ActorAbilityResult.PlayErrorSound;
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddLangAndAfterLineItemHeader( "DamageNegationFromNetControlSkill", ColorTheme.CyanDim )
                            .AddRaw( damageNegated.ToStringWholeBasic() ).Line();

                        bool isFull = Logic == ActorAbilityLogic.AppendToAbilityTooltip_Full;
                        if ( isFull )
                        {
                            bool hasDoneHeader = false;

                            MapCell cell = Actor.CalculateMapCell();
                            if ( cell != null )
                            {
                                float attackRange = Actor.GetAttackRange();
                                Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();

                                DrawHelper.RenderCircle( actorSpot.ReplaceY( 0 ), attackRange,
                                    ColorRefs.MachineUnitHackLine.ColorHDR, 1f );

                                float maxDist = attackRange * attackRange;
                                foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                        continue;

                                    if ( otherActor is ISimMachineActor machineActor )
                                    {
                                        if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                            continue;
                                    }
                                    else
                                    {
                                        if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                            continue;//if not a friend, don't heal them
                                    }

                                    float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                    if ( dist > maxDist )
                                        continue; //too far away, nevermind
                                   
                                    {
                                        if ( !hasDoneHeader )
                                        {
                                            hasDoneHeader = true;
                                            novel.Main.Line().AddBoldLang( "WillApplyTo", ColorTheme.DataLabelWhite ).Line();
                                        }

                                        novel.Main.AddRaw( otherActor.GetDisplayName(), ColorTheme.DataBlue ).Line();
                                    }

                                    if ( otherActor is ISimMachineVehicle otherVehicle )
                                    {
                                        foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                        {
                                            if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                            {
                                                {
                                                    if ( !hasDoneHeader )
                                                    {
                                                        hasDoneHeader = true;
                                                        novel.Main.Line().AddBoldLang( "WillApplyTo", ColorTheme.DataLabelWhite ).Line();
                                                    }

                                                    novel.Main.AddRaw( ridingUnit.GetDisplayName(), ColorTheme.DataBlue ).Line();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        bool hadAnyCouldShield = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                    continue;
                                if ( otherActor is ISimMachineActor machineActor )
                                {
                                    if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                        continue;
                                }
                                else
                                {
                                    if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                        continue;//if not a friend, don't heal them
                                }
                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                hadAnyCouldShield = true;
                                break;
                            }
                        }

                        if ( !hadAnyCouldShield )
                        {
                            if ( BufferOrNull != null )
                                BufferOrNull.AddLang( "NoAlliesInRange" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region FullHandleNetworkTargetingNearby
        public static ActorAbilityResult FullHandleNetworkTargetingNearby( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            int netControlSkill = Actor.GetActorDataCurrent( ActorRefs.UnitNetControl, true );
            int attackPowerAdded = Mathf.CeilToInt( netControlSkill * Ability.GetSingleFloatByID( "NetControlToAttackPowerAdded", 1 ) );

            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( !Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                    {
                        bool didAnyBuffs = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                    continue;

                                if ( otherActor is ISimMachineActor machineActor )
                                {
                                    if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                        continue;
                                }
                                else
                                {
                                    if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                        continue;//if not a friend, don't heal them
                                }

                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                otherActor.AddStatus( StatusRefs.NetworkAssistedTargeting, attackPowerAdded, 1 );
                                didAnyBuffs = true;

                                if ( otherActor is ISimMachineVehicle otherVehicle )
                                {
                                    foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                    {
                                        if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                        {
                                            ridingUnit.AddStatus( StatusRefs.NetworkAssistedTargeting, attackPowerAdded, 1 );
                                            didAnyBuffs = true;
                                        }
                                    }
                                }
                            }
                        }

                        if ( didAnyBuffs )
                        {
                            //ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "UnitRepairNearby" ),
                            //    NoteStyle.StoredGame, machineUnit.UnitType.ID, string.Empty, string.Empty, string.Empty,
                            //    totalStructureHealingDone, totalUnitHealingDone, 0, machineUnit.GetDisplayName(), string.Empty, string.Empty,
                            //    SimCommon.Turn + 15 );
                        }

                        return didAnyBuffs ? ActorAbilityResult.SuccessDidFullAbilityNow : ActorAbilityResult.PlayErrorSound;
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddLangAndAfterLineItemHeader( "AttackPowerAddedFromNetControlSkill", ColorTheme.CyanDim )
                            .AddRaw( attackPowerAdded.ToStringWholeBasic() ).Line();

                        bool isFull = Logic == ActorAbilityLogic.AppendToAbilityTooltip_Full;
                        if ( isFull )
                        {
                            bool hasDoneHeader = false;

                            MapCell cell = Actor.CalculateMapCell();
                            if ( cell != null )
                            {
                                float attackRange = Actor.GetAttackRange();
                                Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();

                                DrawHelper.RenderCircle( actorSpot.ReplaceY( 0 ), attackRange,
                                    ColorRefs.MachineUnitHackLine.ColorHDR, 1f );

                                float maxDist = attackRange * attackRange;
                                foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                        continue;

                                    if ( otherActor is ISimMachineActor machineActor )
                                    {
                                        if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                            continue;
                                    }
                                    else
                                    {
                                        if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                            continue;//if not a friend, don't heal them
                                    }

                                    float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                    if ( dist > maxDist )
                                        continue; //too far away, nevermind

                                    {
                                        if ( !hasDoneHeader )
                                        {
                                            hasDoneHeader = true;
                                            novel.Main.Line().AddBoldLang( "WillApplyTo", ColorTheme.DataLabelWhite ).Line();
                                        }

                                        novel.Main.AddRaw( otherActor.GetDisplayName(), ColorTheme.DataBlue ).Line();
                                    }

                                    if ( otherActor is ISimMachineVehicle otherVehicle )
                                    {
                                        foreach ( ISimMachineUnit ridingUnit in otherVehicle.GetStoredUnits() )
                                        {
                                            if ( ridingUnit != null && !ridingUnit.IsFullDead )
                                            {
                                                {
                                                    if ( !hasDoneHeader )
                                                    {
                                                        hasDoneHeader = true;
                                                        novel.Main.Line().AddBoldLang( "WillApplyTo", ColorTheme.DataLabelWhite ).Line();
                                                    }

                                                    novel.Main.AddRaw( ridingUnit.GetDisplayName(), ColorTheme.DataBlue ).Line();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        bool hadAnyCouldBoost = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = Actor.GetAttackRangeSquared();
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();
                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                if ( otherActor.IsFullDead || otherActor is MachineStructure )
                                    continue;
                                if ( otherActor is ISimMachineActor machineActor )
                                {
                                    if ( machineActor?.CurrentActionOverTime?.Type?.IsUnitInvisibleAndAbsentSeemingWhileActing ?? false )
                                        continue;
                                }
                                else
                                {
                                    if ( !otherActor.GetShouldBeTargetForFriendlyAOEAbilities() )
                                        continue;//if not a friend, don't heal them
                                }
                                float dist = (actorSpot - otherActor.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                hadAnyCouldBoost = true;
                                break;
                            }
                        }

                        if ( !hadAnyCouldBoost )
                        {
                            if ( BufferOrNull != null )
                                BufferOrNull.AddLang( "NoAlliesInRange" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region FullHandleFlamethrower
        public static ActorAbilityResult FullHandleFlamethrower( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            int moraleHitIntensity = Actor.GetActorDataCurrent( ActorRefs.ActorPower, true ) + Actor.GetActorDataCurrent( ActorRefs.UnitIntimidation, true );
            float flamethrowerRange = 11;

            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( !Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                    {
                        int unitTargetCount = 0;
                        int buildingTargetCount = 0;
                        int residentsUnhomed = 0;

                        MapCell outerCell = Actor.CalculateMapCell();
                        if ( outerCell != null )
                        {
                            float maxDist = flamethrowerRange * flamethrowerRange;
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();

                            foreach ( ISimMapActor otherActor in outerCell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                ISimNPCUnit npc = otherActor as ISimNPCUnit;
                                if ( npc == null )
                                    continue; //if not an NPC!

                                if ( npc.IsFullDead || npc.GetIsPartOfPlayerForcesInAnyWay() || npc.UnitType.DeathsCountAsMurders )
                                    continue; //don't target dead people, or friends
                                float dist = (actorSpot - npc.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                if ( npc.GetActorDataMaximum( ActorRefs.UnitMorale, true ) <= 0 || //if does not have morale, not a valid target
                                    !npc.UnitType.Collections.ContainsKey( CommonRefs.Flammable.ID ) )
                                    continue; //i not

                                unitTargetCount++; //this is a target!

                                npc.AlterActorDataCurrent( ActorRefs.UnitMorale, -moraleHitIntensity, true );
                                npc.AddStatus( StatusRefs.OnFire, moraleHitIntensity, 3 );
                                npc.DoOnPostHitWithHostileAction( Actor, moraleHitIntensity, Engine_Universal.PermanentQualityRandom, true );
                            }

                            TargetingHelper.DoForAllBuildingsWithinRangeTight( Actor, flamethrowerRange, CommonRefs.FlamethrowerTarget,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;

                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    buildingTargetCount++;

                                    MapCell pCell = item.ParentCell;
                                    Vector3 position = item.CenterPoint;

                                    int residentCount = Building.GetTotalResidentCount();

                                    //destroy the building as well, because this structure is the building
                                    item.SimBuilding?.SetStatus( CommonRefs.DemolishedBuildingStatus );
                                    item.DropBurningEffect();
                                    item.SimBuilding?.FullyDeleteBuilding(); //AND fully delete the building

                                    if ( residentCount > 0 )
                                    {
                                        residentsUnhomed += residentCount;
                                        CityStatisticTable.AlterScore( "CitizensDisplacedWithFire", residentCount );
                                    }
                                    CityStatisticTable.AlterScore( "SmallStructuresBurnedWithFlamethrower", 1 );

                                    //add an outdoor spot that units can move to like they used to move to the structure
                                    if ( pCell != null )
                                    {
                                        MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( pCell );
                                        outdoorSpot.IsOnRoad = false;
                                        outdoorSpot.Position = position.ReplaceY( MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD );
                                        pCell.AllOutdoorSpots.Add( outdoorSpot );
                                    }

                                    return false; //keep going
                                } );
                        }

                        if ( unitTargetCount > 0 || buildingTargetCount > 0 )
                        {
                            Actor.ApplyVisibilityFromAction( ActionVisibility.IsAttack );

                            ArcenNotes.SendSimpleNoteToGameOnly( 200, NoteInstructionTable.Instance.GetRowByID( "UnitFlamethrowerNearby" ),
                                NoteStyle.BothGame, string.Empty, string.Empty, string.Empty, string.Empty,
                                unitTargetCount, buildingTargetCount, residentsUnhomed, string.Empty, string.Empty, string.Empty,
                                SimCommon.Turn + 30 );

                            ParticleSoundRefs.FlamethrowerBurst.DuringGame_PlayAtLocation( Actor.GetActualPositionForMovementOrPlacement().ReplaceY( 0.5f ) );
                        }

                        return unitTargetCount > 0 || buildingTargetCount > 0 ? ActorAbilityResult.SuccessDidFullAbilityNow : ActorAbilityResult.PlayErrorSound;
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddLangAndAfterLineItemHeader( "MoraleDamageWillInflict", ColorTheme.CyanDim )
                            .AddRaw( moraleHitIntensity.ToStringWholeBasic() ).Line();

                        bool isFull = Logic == ActorAbilityLogic.AppendToAbilityTooltip_Full;
                        if ( isFull )
                        {
                            int unitTargetCount = 0;
                            int buildingTargetCount = 0;

                            MapCell cell = Actor.CalculateMapCell();
                            if ( cell != null )
                            {
                                float maxDist = flamethrowerRange * flamethrowerRange;
                                Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement().ReplaceY( 0.1f );

                                Int64 framesPrepped = RenderManager.FramesPrepped;
                                DrawHelper.RenderRangeCircle( actorSpot, flamethrowerRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    ISimNPCUnit npc = otherActor as ISimNPCUnit;
                                    if ( npc == null )
                                        continue; //if not an NPC!

                                    if ( npc.IsFullDead || npc.GetIsPartOfPlayerForcesInAnyWay() || npc.UnitType.DeathsCountAsMurders )
                                        continue; //don't target dead people, or friends
                                    float dist = (actorSpot - npc.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                    if ( dist > maxDist )
                                        continue; //too far away, nevermind

                                    if ( npc.GetActorDataMaximum( ActorRefs.UnitMorale, true ) <= 0 || //if does not have morale, not a valid target
                                        !npc.UnitType.Collections.ContainsKey( CommonRefs.Flammable.ID ) )
                                        continue; //i not

                                    unitTargetCount++; //this is a target!

                                    CursorHelper.RenderSpecificScalingIconAtSpot( true, IconRefs.FirePrediction, otherActor.GetDrawLocation(), false );
                                }

                                TargetingHelper.DoForAllBuildingsWithinRangeTight( Actor, flamethrowerRange, CommonRefs.FlamethrowerTarget,
                                    delegate ( ISimBuilding Building )
                                    {
                                        MapItem item = Building.GetMapItem();
                                        if ( item == null )
                                            return false;

                                        if ( item.LastFramePrepRendered_StructureHighlight >= framesPrepped )
                                            return false;
                                        item.LastFramePrepRendered_StructureHighlight = framesPrepped;

                                        if ( Building.MachineStructureInBuilding != null )
                                            return false; //if no machine structure possible here right now, or already has one

                                        buildingTargetCount++;
                                        //if we reached this point, this is a valid option!
                                        RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidFlamethrowerTarget.ColorHDR,
                                            new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                        return false; //keep going
                                    } );
                            }

                            novel.Main.AddBoldLangAndAfterLineItemHeader( "UnitsWillHit", ColorTheme.DataLabelWhite )
                                .AddRaw( unitTargetCount.ToString(), ColorTheme.DataBlue ).Line();
                            novel.Main.AddBoldLangAndAfterLineItemHeader( "BuildingsWillBurnDown", ColorTheme.DataLabelWhite )
                                .AddRaw( buildingTargetCount.ToString(), ColorTheme.DataBlue ).Line();

                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "MoraleDamageWillInflict_Flamethrower_Details", ColorTheme.PurpleDim ).Line();
                        }
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        bool hadAnyTargets = false;

                        MapCell cell = Actor.CalculateMapCell();
                        if ( cell != null )
                        {
                            float maxDist = flamethrowerRange * flamethrowerRange;
                            Vector3 actorSpot = Actor.GetActualPositionForMovementOrPlacement();

                            foreach ( ISimMapActor otherActor in cell.ParentTile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                            {
                                ISimNPCUnit npc = otherActor as ISimNPCUnit;
                                if ( npc == null )
                                    continue; //if not an NPC!

                                if ( npc.IsFullDead || npc.GetIsPartOfPlayerForcesInAnyWay() || npc.UnitType.DeathsCountAsMurders )
                                    continue; //don't target dead people, or friends
                                float dist = (actorSpot - npc.GetActualPositionForMovementOrPlacement()).GetSquareGroundMagnitude();
                                if ( dist > maxDist )
                                    continue; //too far away, nevermind

                                if ( npc.GetActorDataMaximum( ActorRefs.UnitMorale, true ) <= 0 || //if does not have morale, not a valid target
                                    !npc.UnitType.Collections.ContainsKey( CommonRefs.Flammable.ID ) )
                                    continue; //i not

                                hadAnyTargets = true;
                                break;
                            }

                            if ( !hadAnyTargets )
                                TargetingHelper.DoForAllBuildingsWithinRangeTight( Actor, flamethrowerRange, CommonRefs.FlamethrowerTarget,
                                    delegate ( ISimBuilding Building )
                                    {
                                        MapItem item = Building.GetMapItem();
                                        if ( item == null )
                                            return false;

                                        if ( Building.MachineStructureInBuilding != null )
                                            return false; //if no machine structure possible here right now, or already has one

                                        hadAnyTargets = true;
                                        return true; //true causes it to stop
                                    } );
                        }

                        if ( !hadAnyTargets )
                        {
                            if ( BufferOrNull != null )
                                BufferOrNull.AddLang( "NoTargetsInRange" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region FullHandleBattleRecharge
        public static ActorAbilityResult FullHandleBattleRecharge( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( !Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                    {
                        if ( ResourceRefs.MentalEnergy.Current >= 2 && Actor.CurrentActionPoints < Actor.GetActorDataCurrent( ActorRefs.ActorMaxActionPoints, true ) )
                        {
                            Actor.AlterCurrentActionPoints( 1 );
                            return ActorAbilityResult.SuccessDidFullAbilityNow;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        
                    }
                    break;
                default:
                    {
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;

                        if ( Actor.CurrentActionPoints >= Actor.GetActorDataCurrent( ActorRefs.ActorMaxActionPoints, true ) )
                        {
                            if ( BufferOrNull != null && Logic == ActorAbilityLogic.AppendToUsageBlockedTooltip )
                                BufferOrNull.AddLang( "UnitAlreadyHasMaxAP" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        if ( ResourceRefs.MentalEnergy.Current < 2 )
                        {
                            if ( BufferOrNull != null && Logic == ActorAbilityLogic.AppendToUsageBlockedTooltip )
                                BufferOrNull.AddLang( "MustHaveAtLeastTwoEnergyToUseThis" );
                            return ActorAbilityResult.PlayErrorSound;
                        }
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region FullHandleStandby
        public static ActorAbilityResult FullHandleStandby( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    break;
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                    if ( Actor.CurrentStandby != StandbyType.None )
                    {
                        Actor.CurrentStandby = StandbyType.None;
                        ParticleSoundRefs.StandbyOff.DuringGame_PlaySoundOnlyAtCamera();
                    }
                    else
                    {
                        Actor.CurrentStandby = StandbyType.Temporary;
                        ParticleSoundRefs.StandbyTemporary.DuringGame_PlaySoundOnlyAtCamera();
                    }
                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                case ActorAbilityLogic.TriggerAbilityAltView:
                    if ( Actor.CurrentStandby != StandbyType.Indefinite )
                    {
                        Actor.CurrentStandby = StandbyType.Indefinite;
                        ParticleSoundRefs.StandbyIndefinite.DuringGame_PlaySoundOnlyAtCamera();
                    }
                    else
                    {
                        Actor.CurrentStandby = StandbyType.None;
                        ParticleSoundRefs.StandbyOff.DuringGame_PlaySoundOnlyAtCamera();
                    }
                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddLangAndAfterLineItemHeader( "UnitReadiness", ColorTheme.CyanDim );
                        switch ( Actor.CurrentStandby )
                        {
                            case StandbyType.None:
                                novel.Main.AddLang( "UnitReadiness_Normal", ColorTheme.CategorySelectedBlue );
                                break;
                            case StandbyType.Temporary:
                                novel.Main.AddLang( "UnitReadiness_StandbyTemporary", ColorTheme.HeaderGoldOrangeDark );
                                break;
                            case StandbyType.Indefinite:
                                novel.Main.AddLang( "UnitReadiness_StandbyIndefinite", ColorTheme.HeaderGoldOrangeDark );
                                break;
                        }
                        novel.Main.Line();

                        novel.Main.AddFormat2( "Standby_Expalanation1",
                            InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                            InputCaching.GetGetHumanReadableKeyComboForNextMachineActor(), ColorTheme.NarrativeColor ).Line();

                        if ( InputCaching.ShouldShowDetailedTooltips )
                        {
                            novel.Main.AddFormat2( "Standby_Expalanation2", InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                                InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn(), ColorTheme.PurpleDim ).Line();
                        }
                        novel.FrameBody.AddFormat2( "Standby_Expalanation3",
                            Lang.GetLeftClickText(), Lang.GetRightClickText(), ColorTheme.SoftGold ).Line();
                    }
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        novel.Main.AddFormat2( "Standby_Expalanation1",
                            InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                            InputCaching.GetGetHumanReadableKeyComboForNextMachineActor(), ColorTheme.NarrativeColor ).Line();

                        if ( InputCaching.ShouldShowDetailedTooltips )
                        {
                            novel.Main.AddFormat2( "Standby_Expalanation2", InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                               InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn(), ColorTheme.PurpleDim ).Line();
                        }
                        novel.FrameBody.AddFormat2( "Standby_Expalanation3",
                            Lang.GetLeftClickText(), Lang.GetRightClickText(), ColorTheme.SoftGold ).Line();
                    }
                    break;
                default:
                    {
                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion

        #region Calc_IsBlockedFromRecruitingMoreAndroids
        public static bool Calc_IsBlockedFromRecruitingMoreAndroids( MachineUnitType UnitTypeToRecruit )
        {
            if ( UnitTypeToRecruit == null )
                return true;
            if ( SimCommon.TotalCapacityUsed_Androids + UnitTypeToRecruit.UnitCapacityCost > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                return true;
            if ( FlagRefs.AndroidSecurityPatch.DuringGameplay_HasEverCompleted )
                return true;
            return false;
        }
        #endregion

        #region IdleGather
        public static void IdleGather( int ScavengingSkill, int NumberToDo )
        {
            if ( ScavengingSkill <= 0 || NumberToDo <= 0 )
                return;

            float baseElementalSlurry = MathRefs.BaseElementalSlurryPerTurnPerAP.FloatMin;
            float addedElementalSlurry = MathRefs.AddedElementalSlurryPerScavengingSkillPerTurnPerAP.FloatMin;

            int totalElementalSlurry = Mathf.CeilToInt( (baseElementalSlurry + ( addedElementalSlurry * ScavengingSkill)) * NumberToDo );

            if ( totalElementalSlurry > 0 )
                ResourceRefs.ElementalSlurry.AlterCurrent_Named( totalElementalSlurry, "Increase_IdleUnitGathering", ResourceAddRule.IgnoreUntilTurnChange );
        }
        #endregion

        #region HandleConsumablesAbility
        public static ActorAbilityResult HandleConsumablesAbility( ISimMachineActor Actor, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            switch ( Logic )
            {
                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                case ActorAbilityLogic.ExecuteAbilityAutomated:
                    if ( /*!Actor.GetIsBlockedFromActingForAbility( Ability, false )*/ Actor.GetTypeDuringGameData().ConsumablesAvailable.Count > 0 )
                    {
                        if ( Actor.IsInConsumableTargetingMode != null )
                        {
                            Actor.SetTargetingMode( null, null );
                            return ActorAbilityResult.OpenedInterface;
                        }

                        Window_AbilityOptionList.HandleOpenCloseToggle( Actor, Ability );
                        return ActorAbilityResult.OpenedInterface;
                    }
                    break;
                case ActorAbilityLogic.TriggerAbilityAltView:
                    {
                        if ( Actor.IsInConsumableTargetingMode != null )
                        {
                            Actor.SetTargetingMode( null, null );
                            return ActorAbilityResult.OpenedInterface;
                        }

                        //if ( !Window_PlayerHardware.Instance.IsOpen )
                        //    Window_PlayerHardware.Instance.Open();
                        //Window_PlayerHardware.customParent.currentlyRequestedDisplayType = Window_PlayerHardware.HardwareDisplayType.Consumables;
                        return ActorAbilityResult.OpenedInterface;
                    }
                case ActorAbilityLogic.HandleOptionsListPopulation:
                    {
                        foreach ( ResourceConsumable consumable in Actor.GetTypeDuringGameData().ConsumablesAvailable.GetDisplayList() )
                        {
                            Window_AbilityOptionList.RenderAbilityOption( consumable.Icon, delegate( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                int countCanAfford = consumable.CalculateMaxCouldCreate( false );

                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool canUse = countCanAfford > 0;
                                            if ( canUse )
                                                canUse = consumable.CalculateCanUseDirectConsumable( Actor, true );

                                            string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, !canUse );
                                            if ( countCanAfford > 999 )
                                                countCanAfford = 999;

                                            ExtraData.Buffer.AddRaw( countCanAfford.ToStringThousandsWhole() ).Position30()
                                                    .AddRaw( consumable.GetDisplayName(), colorHex );
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            consumable.RenderConsumableTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, Actor, TooltipInstruction.ForDeploying, TooltipExtraText.None, TooltipExtraRules.None );
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            //if ( ExtraData.MouseInput.RightButtonClicked )
                                            //{
                                            //    if ( !Window_PlayerHardware.Instance.IsOpen )
                                            //        Window_PlayerHardware.Instance.Open();
                                            //    Window_PlayerHardware.customParent.currentlyRequestedDisplayType = Window_PlayerHardware.HardwareDisplayType.Consumables;
                                            //    return;
                                            //}

                                            if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) || countCanAfford <= 0 )
                                            {
                                                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                                return;
                                            }

                                            if ( Ability.MentalEnergyCost > 0 && Ability.MentalEnergyCost > ResourceRefs.MentalEnergy.Current )
                                            {
                                                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                                return;
                                            }


                                            ConsumableUseResult result = consumable.TryToDirectlyUseByActor( Actor, Ability );
                                            if ( result == ConsumableUseResult.Fail )
                                            {
                                                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                                return;
                                            }

                                            if ( result == ConsumableUseResult.EnteredTargetingMode )
                                            {
                                                //if we did successfully started targeting, then close
                                                Window_AbilityOptionList.Instance.Close( WindowCloseReason.UserDirectRequest );
                                            }
                                        }
                                        break;
                                }
                            } );
                        }
                    }
                    return ActorAbilityResult.OpenedInterface;
                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                    {
                    }
                    break;
                case ActorAbilityLogic.AppendToUsageBlockedTooltip:
                    if ( Actor.GetTypeDuringGameData().ConsumablesAvailable.Count == 0 )
                        BufferOrNull.AddLang( "NoConsumablesValidForThisUnit", ColorTheme.RedOrange2 ).Line();
                    break;
                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                    break;
                default:
                    {
                        if ( Actor.GetTypeDuringGameData().ConsumablesAvailable.Count == 0 )
                            return ActorAbilityResult.PlayErrorSound;
                        if ( Actor.GetIsBlockedFromActingForAbility( Ability, true ) )
                            return ActorAbilityResult.PlayErrorSound;
                        return ActorAbilityResult.OpenedInterface;
                    }
            }
            return ActorAbilityResult.PlayErrorSound;
        }
        #endregion HandleConsumablesAbility
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class MachineMovePlannerImplementation : MachineMovePlanner
    {
        #region Basic Methods
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public override void ClearAllObjectsBecauseOfUnload()
        {
        }

        public MachineMovePlannerImplementation()
        {
            MachineMovePlanner.Instance = this;
        }
        public override void InitializePoolsIfNeeded( ref long poolCount, ref long poolItemCount )
        {
        }
        #endregion

        public override bool GetShouldCameraLateralMovementBeAllowed()
        {
            if ( !FlagRefs.UITour7_AbilityBar.DuringGameplay_IsTripped ) //directly before controls unlocking
                return false;

            if ( Engine_HotM.SelectedActor is ISimNPCUnit )
                return true; //always allow movement when an NPC unit
            if ( Engine_HotM.SelectedActor is MachineStructure )
                return true; //always allow movement when a machine structure

            ISimMachineActor actor = Engine_HotM.SelectedActor as ISimMachineActor;
            if ( actor == null || !actor.GetIsBlockedFromActingInGeneral() ) //allow lateral movement when the actor has not acted yet, or when no actor selected
            {
                if ( actor is ISimMachineVehicle vehicle )
                {
                    if ( vehicle.IsFullDead ) 
                        return false; //only lock camera if they are dead
                }
                if ( actor is ISimMachineUnit unit )
                {
                    if ( unit.IsFullDead )
                        return false; //only lock camera if they are dead
                }
                return true;
            }
            return true;
        }

        public override void HandleActionTypeMouseInteractionsAndAnyExtraRendering()
        {
            ISimMapMobileActor actor = Engine_HotM.SelectedActor as ISimMapMobileActor;
            if ( actor == null )
                return;
            if ( VisCurrent.IsShowingActualEvent || !FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped || VisCurrent.IsShowingChapterChange )
                return; //we don't do any of this when in an event, or if not yet emerged into the city
            if ( (Window_MajorEventWindow.Instance?.GetShouldDrawThisFrame()??false) || 
                (Window_SimpleChoiceWindow.Instance?.GetShouldDrawThisFrame()??false) ||
                (Window_RewardWindow.Instance?.GetShouldDrawThisFrame() ?? false) )
                return;
            if ( Engine_HotM.SelectedMachineActionMode != null )
                return; //don't do any of this if we're in a different action mode


            //if ( actor == MouseHelper.CalculateActorUnderCursor() && Engine_HotM.GameMode == MainGameMode.CityMap )
            //{
            //    ArcenDebugging.LogSingleLine( "yes under cursor! peek: " + ArcenInput.LeftMouseNonUI.PeekIsBrieflyClicked(), Verbosity.ShowAsError );
            //    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )
            //    {
            //        VisCommands.ToggleCityMapMode();
            //        return;
            //    }
            //}

            if ( actor is ISimMachineUnit unit )
            {
                if ( unit == null )
                    return;
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        break;
                    default:
                        return; //handle in both streets and city map
                }
                if ( unit.IsFullDead )
                    return;
                if ( unit.CurrentActionOverTime != null || unit.AlmostCompleteActionOverTime != null )
                    return;

                {
                    NPCMission missionUnderCursor = CursorHelper.FindNPCMissionUnderCursor();
                    if ( missionUnderCursor != null )
                    {
                        //this is a big deal!  If a unit is selected and we are hovering over a mission, then we want to give different mouse interactions!
                        return;
                    }
                }


                if ( unit.CurrentActionPoints == 0 )
                {
                    HandbookRefs.UnitsCanReload.DuringGame_UnlockIfNeeded( true );
                    FlagRefs.AndroidOverdrive.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                }

                if ( unit.IsInConsumableTargetingMode != null )
                {
                    float attackRange = unit.GetAttackRange();
                    float moveRange = unit.GetMovementRange();
                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 center = unit.GetActualPositionForMovementOrPlacement();
                    Vector3 groundCenter = center.ReplaceY( groundLevel );

                    bool didHandling = unit.IsInConsumableTargetingMode.Implementation.HandleConsumableHardTargeting( unit, unit.IsInConsumableTargetingMode, center, attackRange, 
                        moveRange );
                    //must be AFTER the above
                    if ( unit.UnitType.IsConsideredAndroid )
                        AndroidMoveAndAttackLogic.HandleMouseInteractionsAndAnyExtraRendering_Android( unit, didHandling );
                    else if ( unit.UnitType.IsConsideredMech )
                        MechMoveAndAttackLogic.HandleMechBasicLogic( unit );

                    if ( !didHandling )
                    {
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                    return;
                }

                if ( unit.UnitType.IsConsideredAndroid )
                {
                    if ( unit.IsInAbilityTypeTargetingMode != null && !unit.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !unit.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                    {
                        //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                        //{
                        //    unit.SetTargetingMode( null, null );
                        //    return;
                        //}

                        float attackRange = unit.GetAttackRange();
                        float moveRange = unit.GetMovementRange();
                        float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                        Vector3 center = unit.GetActualPositionForMovementOrPlacement();
                        Vector3 groundCenter = center.ReplaceY( groundLevel );

                        AndroidMoveAndAttackLogic.DrawAndroidRangeCircles( groundCenter,  attackRange, moveRange );

                        unit.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityHardTargeting( unit, unit.IsInAbilityTypeTargetingMode,
                             center, attackRange, moveRange );
                        return;
                    }

                    {
                        if ( unit.IsInAbilityTypeTargetingMode != null && unit.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                        {
                            if ( !unit.GetIsBlockedFromActingInGeneral() )
                            {
                                float attackRange = unit.GetAttackRange();
                                float moveRange = unit.GetMovementRange();
                                Vector3 center = unit.GetActualPositionForMovementOrPlacement();
                                unit.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityMixedTargeting( unit, unit.IsInAbilityTypeTargetingMode,
                                     center, attackRange, moveRange );
                            }
                        }

                        AndroidMoveAndAttackLogic.HandleMouseInteractionsAndAnyExtraRendering_Android( unit, false );

                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                }
                else if ( unit.UnitType.IsConsideredMech )
                {
                    if ( MechMoveAndAttackLogic.TryHandleGetInVehicleLogic( unit ) )
                        return;

                    if ( unit.IsInAbilityTypeTargetingMode != null && !unit.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !unit.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                    {
                        if ( unit.IsInAbilityTypeTargetingMode.IfVehicleOrMechEnablesMovement )
                            MechMoveAndAttackLogic.HandleMoveMode_Mech( unit );
                        else
                        {
                            //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                            //{
                            //    unit.SetTargetingMode( null, null );
                            //    return;
                            //}

                            float attackRange = unit.GetAttackRange();
                            float moveRange = unit.GetMovementRange();
                            float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                            Vector3 center = unit.GetActualPositionForMovementOrPlacement();
                            Vector3 groundCenter = center.ReplaceY( groundLevel );

                            //AndroidMoveAndAttackLogic.DrawAndroidRangeCircles( groundCenter, scanRange, attackRange, moveRange );
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                            unit.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityHardTargeting( unit, unit.IsInAbilityTypeTargetingMode,
                                 center, attackRange, moveRange );
                        }

                        return;
                    }

                    {
                        if ( unit.IsInAbilityTypeTargetingMode != null && unit.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                        {
                            if ( !unit.GetIsBlockedFromActingInGeneral() )
                            {
                                float attackRange = unit.GetAttackRange();
                                float moveRange = unit.GetMovementRange();
                                Vector3 center = unit.GetActualPositionForMovementOrPlacement();
                                unit.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityMixedTargeting( unit, unit.IsInAbilityTypeTargetingMode,
                                     center, attackRange, moveRange );
                            }
                        }

                        MechMoveAndAttackLogic.HandleMechBasicLogic( unit );
                    }
                }
            }
            else if ( actor is ISimMachineVehicle vehicle )
            {
                if ( vehicle.CurrentActionOverTime != null || vehicle.AlmostCompleteActionOverTime != null )
                    return;
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        break;
                    default:
                        return; //render in both streets and city map
                }
                {
                    NPCMission missionUnderCursor = CursorHelper.FindNPCMissionUnderCursor();
                    if ( missionUnderCursor != null )
                    {
                        //this is a big deal!  If a unit is selected and we are hovering over a mission, then we want to give different mouse interactions!
                        return;
                    }
                }

                if ( vehicle.IsInConsumableTargetingMode != null )
                {
                    float attackRange = vehicle.GetAttackRange();
                    float moveRange = vehicle.GetMovementRange();
                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 center = vehicle.GetActualPositionForMovementOrPlacement();
                    Vector3 groundCenter = center.ReplaceY( groundLevel );

                    vehicle.IsInConsumableTargetingMode.Implementation.HandleConsumableHardTargeting( vehicle, vehicle.IsInConsumableTargetingMode, 
                        center, attackRange, moveRange );
                    //must be AFTER the above
                    VehicleMoveAndAttackLogic.HandleVehicleBasicLogic( vehicle );
                    return;
                }

                if ( vehicle.IsInAbilityTypeTargetingMode != null && 
                    !vehicle.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !vehicle.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                {
                    if ( !vehicle.GetIsBlockedFromActingInGeneral() )
                    {
                        if ( vehicle.IsInAbilityTypeTargetingMode.IfVehicleOrMechEnablesMovement )
                            VehicleMoveAndAttackLogic.HandleMoveMode_Vehicle( vehicle );
                        else
                        {
                            //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                            //{
                            //    vehicle.SetTargetingMode( null, null );
                            //    return;
                            //}

                            float attackRange = vehicle.GetAttackRange();
                            float moveRange = vehicle.GetMovementRange();
                            float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                            Vector3 center = vehicle.GetActualPositionForMovementOrPlacement();
                            Vector3 groundCenter = center.ReplaceY( groundLevel );

                            //AndroidMoveAndAttackLogic.DrawAndroidRangeCircles( groundCenter, scanRange, attackRange, moveRange );
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                            vehicle.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityHardTargeting( vehicle, vehicle.IsInAbilityTypeTargetingMode,
                                 center, attackRange, moveRange );
                        }
                    }
                    return;
                }

                {
                    if ( vehicle.IsInAbilityTypeTargetingMode != null && vehicle.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                    {
                        if ( !vehicle.GetIsBlockedFromActingInGeneral() )
                        {
                            float attackRange = vehicle.GetAttackRange();
                            float moveRange = vehicle.GetMovementRange();
                            Vector3 center = vehicle.GetActualPositionForMovementOrPlacement();
                            vehicle.IsInAbilityTypeTargetingMode.Implementation.HandleAbilityMixedTargeting( vehicle, vehicle.IsInAbilityTypeTargetingMode,
                                 center, attackRange, moveRange );
                        }
                    }

                    VehicleMoveAndAttackLogic.HandleVehicleBasicLogic( vehicle );
                }
            }
            else if ( actor is ISimNPCUnit npcUnit )
            {
                if ( !npcUnit.GetIsPlayerControlled() )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( false );
                    return;
                }
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        break;
                    default:
                        return; //render in both streets and city map
                }
                {
                    NPCMission missionUnderCursor = CursorHelper.FindNPCMissionUnderCursor();
                    if ( missionUnderCursor != null )
                    {
                        //this is a big deal!  If a unit is selected and we are hovering over a mission, then we want to give different mouse interactions!
                        return;
                    }
                }

                #region Mouseover Other From NPC
                {
                    //when selecting an npc, if you mouse over something else
                    ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                    if ( actorMousingOver != null && actorMousingOver != npcUnit && !(actorMousingOver is ISimMachineVehicle ) )
                    {
                        //select it if it's one of our main ones
                        if ( actorMousingOver is ISimMachineActor || actorMousingOver is MachineStructure )
                        {
                            MouseHelper.RenderWouldSelectTooltipAndCursor( actorMousingOver );
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( false );
                            return;
                        }
                        else if ( actorMousingOver is ISimNPCUnit npc && npc.GetIsPartOfPlayerForcesInAnyWay() ) //also select it if it's one of our npcs
                        {
                            MouseHelper.RenderWouldSelectTooltipAndCursor( actorMousingOver );
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( false );
                            return;
                        }

                        //even we just missed the tooltip situation above, that's all
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                }
                #endregion

                if ( npcUnit.UnitType == null )
                { }
                else if ( npcUnit.UnitType.CostsToCreateIfBulkAndroid.Count > 0 )
                    BulkAndroidMoveLogic.HandleMouseInteractionsAndAnyExtraRendering_BulkAndroid( npcUnit );
                else
                {
                    if ( npcUnit.UnitType.IsMechStyleMovement )
                        CapturedNPCUnitMoveLogic.HandleMouseInteractionsAndAnyExtraRendering_MechStyle( npcUnit );
                    else
                        CapturedNPCUnitMoveLogic.HandleMouseInteractionsAndAnyExtraRendering_NormalStyle( npcUnit );
                }

                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                    MouseHelper.BasicLeftClickHandler( false );
            }
            else //whatever this might be
            {
                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                    MouseHelper.BasicLeftClickHandler( false );
            }
        }

        #region HandleNonBuildingConsumableTargetingModeForAnyMachineActor
        public static bool HandleNonBuildingConsumableTargetingModeForAnyMachineActor( ISimMachineActor ActorUsing )
        {
            if ( ActorUsing == null ) 
                return false;
            ResourceConsumable consumableTargeting = ActorUsing.IsInConsumableTargetingMode;
            if ( consumableTargeting == null )
                return false;

            bool isAndroid = (ActorUsing is ISimMachineUnit unit && unit.UnitType.IsConsideredAndroid);

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 100;
            try
            {
                Vector3 aimPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = ActorUsing.GetEmissionLocation();

                debugStage = 2000;
                float attackRange = ActorUsing.ActorData[ActorRefs.AttackRange].Current;
                if ( attackRange <= 0 )
                    return false;

                debugStage = 5000;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );
                //DrawHelper.RenderRangeCircle( groundCenter, range, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                if ( hasValidDestinationPoint )
                {
                    if ( aimPoint.x == float.NegativeInfinity || aimPoint.x == float.PositiveInfinity )
                        hasValidDestinationPoint = false;
                }

                if ( hasValidDestinationPoint )
                {
                    hasValidDestinationPoint = (center - aimPoint).GetSquareGroundMagnitude() <= attackRange * attackRange;
                }

                ISimMapActor actorUnderCursor = null;
                bool canDoAction = false;
                //if ( hasValidDestinationPoint )
                {
                    actorUnderCursor = MouseHelper.ActorUnderCursor;
                    if ( actorUnderCursor != null )
                    {
                        aimPoint = actorUnderCursor.GetEmissionLocation();
                        if ( consumableTargeting.CalculateCanUseDirectConsumableOnTargetUnit( ActorUsing, actorUnderCursor, null, false ) )
                            canDoAction = true;
                    }
                }

                {
                    //against the current location of the actor
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( ActorUsing, center.PlusY( ActorUsing.GetHalfHeightForCollisions() ), null,
                            false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.CurrentLocation_NoOp, false, ThreatLineLogic.Normal );
                }

                if ( isAndroid && actorUnderCursor == null )
                    return false; //do whatever else is supposed to be done

                #region Tooltip For Use Item
                {
                    if ( !canDoAction ) //it's probably because it's on the other side of a lockdown or something
                    {
                        if ( actorUnderCursor != null )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( consumableTargeting ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                            {
                                novel.Icon = consumableTargeting.Icon;
                                novel.ShouldTooltipBeRed = true;

                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "ConsumableTargeting_InvalidTarget" ).AddRaw( actorUnderCursor.GetDisplayName() );
                                consumableTargeting.CalculateCanUseDirectConsumableOnTargetUnit( ActorUsing, actorUnderCursor, novel.MainHeader, false );
                            }
                        }
                    }
                    else
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( consumableTargeting ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.Icon = consumableTargeting.Icon;

                            novel.TitleUpperLeft.AddRawAndAfterLineItemHeader( consumableTargeting.GetDisplayName() ).AddRaw( actorUnderCursor.GetDisplayName() );

                            if ( !hasValidDestinationPoint )
                            {
                                novel.ShouldTooltipBeRed = true;
                                novel.MainHeader.AddLang( "ValidTargetOutOfRange" );
                            }
                            else if ( ActorUsing.CurrentActionPoints == 0 )
                            {
                                novel.ShouldTooltipBeRed = true;
                                novel.MainHeader.AddLang( "ValidTargetOutOfActionPoints" );
                            }
                            else if ( ResourceRefs.MentalEnergy.Current == 0 )
                            {
                                novel.ShouldTooltipBeRed = true;
                                novel.MainHeader.AddLang( "ValidTargetOutOfMentalEnergy" );
                            }
                            else
                            {
                                novel.MainHeader.AddLang( "ValidTarget" );
                            }

                            if ( hasValidDestinationPoint )
                            {
                                int lengthMainHeader = novel.MainHeader.GetLength();
                                int lengthMain = novel.Main.GetLength();
                                CombatTextHelper.AppendLastPredictedDamageLong( ActorUsing, novel.MainHeader, novel.Main, InputCaching.ShouldShowDetailedTooltips, false, false );
                                CombatTextHelper.AppendRestrictedAreaLong( ActorUsing, novel.GetMainHeaderContinuation(), novel.Main, InputCaching.ShouldShowDetailedTooltips, null, center, true );

                                if ( lengthMainHeader != novel.MainHeader.GetLength() || lengthMain != novel.Main.GetLength() )
                                    novel.CanExpand = CanExpandType.Brief;
                                else
                                    novel.CanExpand = CanExpandType.No;
                            }
                        }
                    }
                }
                #endregion

                //DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                {
                    //draw the sole version
                    DrawHelper.RenderCatmullLine( ActorUsing.GetCollisionCenter(), aimPoint,
                        hasValidDestinationPoint && ( canDoAction || actorUnderCursor == null ) ?
                        ( consumableTargeting.DirectUseConsumable.UseAssistStyleTargetingLine ? ColorRefs.MachineUnitHelpLine.ColorHDR : ColorRefs.MachineUnitAttackLine.ColorHDR )
                        : Color.red, canDoAction ? 1.5f : 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                }

                if ( !hasValidDestinationPoint )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, aimPoint );
                else if ( !canDoAction )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, aimPoint );
                else
                {
                    if ( consumableTargeting.DirectUseConsumable.UseAssistStyleTargetingLine )
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, consumableTargeting.Icon, IconRefs.MouseMoveMode_ProvideHelp, aimPoint );
                    else
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, consumableTargeting.Icon, IconRefs.MouseMoveMode_RangedConsumableAttack, aimPoint );
                }

                #region Handle Clicks
                if ( hasValidDestinationPoint && canDoAction && ActorUsing.CurrentActionPoints > 0 && !ActorUsing.GetIsMovingRightNow() &&
                    SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && !ActorUsing.GetIsBlockedFromActingInGeneral() )
                {
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                    {
                        if ( consumableTargeting.TryToDirectlyUseByActorAgainstTargetUnit( ActorUsing, actorUnderCursor, null ) == ConsumableUseResult.Success )
                        {
                            if ( !InputCaching.ShouldKeepDoingAction )
                                ActorUsing.SetTargetingMode( null, null );
                        }
                    }
                }
                #endregion

                return true;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "HandleNonBuildingConsumableTargetingModeForAnyMachineActor Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
                return false;
            }
        }
        #endregion
    }
}

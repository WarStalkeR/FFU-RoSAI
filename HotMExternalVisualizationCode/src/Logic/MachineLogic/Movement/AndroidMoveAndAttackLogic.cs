using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class AndroidMoveAndAttackLogic
    {
        private static ArcenDoubleCharacterBuffer secondaryBufferForKeyAttackNotes = new ArcenDoubleCharacterBuffer( "AndroidMoveAndAttackLogic-secondaryBufferForKeyAttackNotes" );
        private static readonly ArcenDoubleCharacterBuffer passingMessageBuffer = new ArcenDoubleCharacterBuffer( "AndroidMoveAndAttackLogic-passingMessageBuffer" );
        private static MersenneTwister workingMainThreadRandom = new MersenneTwister( 0 );
        private static MersenneTwister postAbilityRand = new MersenneTwister( 0 );

        #region HandleMouseInteractionsAndAnyExtraRendering_Android
        public static void HandleMouseInteractionsAndAnyExtraRendering_Android( ISimMachineUnit Unit, bool AlreadyDidHandling )
        {
            if ( TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() )
                return; //no more interaction if already it has been written

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Unit.GetActualPositionForMovementOrPlacement();
                Camera cam = Engine_HotM.GameModeData.MainCamera;

                MachineUnitStance stance = Unit.Stance;
                if ( stance == null )
                    return;

                debugStage = 500;
                if ( stance.ShouldBlockAndroidMovement )
                    return;

                debugStage = 2200;

                debugStage = 3200;
                {

                    debugStage = 7300;

                    #region Render Range Circles
                    bool isBlockedFromActing = Unit.GetIsBlockedFromActingForMovementPurposes();

                    if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                        destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity && !isBlockedFromActing )
                        Unit.RotateAndroidToFacePoint( destinationPoint );

                    debugStage = 9100;
                    float attackRange = Unit.GetAttackRange();
                    float moveRange = Unit.GetMovementRange();

                    debugStage = 12100;
                    if ( attackRange <= 0 )
                    {
                        //ArcenDebugging.LogSingleLine( "MachineMovePlannerImplementation no AttackRange!", Verbosity.ShowAsError );
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }
                    debugStage = 13100;
                    if ( moveRange <= 0 )
                    {
                        //ArcenDebugging.LogSingleLine( "MachineMovePlannerImplementation no UnitMoveRange!", Verbosity.ShowAsError );
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }                    

                    debugStage = 19100;
                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 groundCenter = center.ReplaceY( groundLevel );

                    if ( !isBlockedFromActing && !TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() && Unit.IsInAbilityTypeTargetingMode == null && !AlreadyDidHandling )
                    {
                        //if our cursor is over a structure...
                        //and if the structure is under construction, digging basement, installing a job, or damaged...
                        //then handle that separately.
                        MachineStructure structureUnderCursor = MouseHelper.StructureUnderCursor;
                        if ( structureUnderCursor != null && (structureUnderCursor.IsUnderConstruction || structureUnderCursor.IsJobStillInstalling) )
                        {
                            LocationActionType importantAction = null;
                            NPCEvent importantEvent = null;
                            ProjectOutcomeStreetSenseItem importantProjectItem = null;
                            string importantActionOptionalID = string.Empty;
                            MoveHelper.UnpackActionFromBuildingForActor( Unit, structureUnderCursor.Building, false, ref importantAction, ref importantEvent, ref importantProjectItem, ref importantActionOptionalID );

                            if ( importantAction == null )
                                HandleAttemptToAssistTargetStructure( Unit, structureUnderCursor, center, groundCenter,
                                    moveRange, attackRange, groundLevel );
                        }
                    }

                    if ( isBlockedFromActing || TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() || AlreadyDidHandling ||
                        //aka, hard targeting mode
                        ( Unit.IsInAbilityTypeTargetingMode != null && !Unit.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !Unit.IsInAbilityTypeTargetingMode.IsMixedTargetingMode )
                        )
                    {
                        DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );

                        {
                            ISimMapActor mapActorUnderCursor = CursorHelper.FindMapActorUnderCursor();
                            //if ( mapActorUnderCursor == Unit )
                            //{
                            //    DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );
                            //    return; //if we mouse over ourselves, then do rings only
                            //}

                            if ( mapActorUnderCursor != null && mapActorUnderCursor is ISimNPCUnit npcUnit && !npcUnit.GetIsAnAllyFromThePlayerPerspective() )
                            {
                                //this is a big deal!  If a unit is selected and we are hovering over another unit, then we might shoot it or something
                                TryHandleAttemptToInteractWithHostileOrNeutralTargetUnit( Unit, mapActorUnderCursor, center, groundCenter, destinationPoint,
                                    moveRange, attackRange, stance, groundLevel );
                                return;
                            }
                        }

                        return;
                    }

                    debugStage = 21100;
                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    if ( hasValidDestinationPoint )
                    {
                        if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                            hasValidDestinationPoint = false;
                    }

                    debugStage = 22100;

                    #region If Unit Is In A Vehicle And Not Allowed To Disembark
                    if ( !Unit.GetIsDeployed() && !Unit.GetUnitCanBeRemovedFromVehicleNow( null ) )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_Invalid );

                        if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                        {
                            passingMessageBuffer.Clear();
                            if ( !Unit.GetUnitCanBeRemovedFromVehicleNow( passingMessageBuffer ) )
                            {
                                if ( !passingMessageBuffer.GetIsEmpty() )
                                    ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddRaw_New( passingMessageBuffer.GetStringAndResetForNextUpdate() ),
                                        NoteStyle.ShowInPassing, 3f );
                            }
                        }
                        return;
                    }
                    #endregion

                    if ( Unit.GetIsCurrentStanceInvalid() )
                    {
                        if ( Unit.UnitType.IsConsideredMech )
                            Unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                        else if ( Unit.UnitType.IsConsideredAndroid )
                            Unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;
                    }

                    bool canMoveIntoRestrictedAreas = true;
                    if ( Unit.Stance.IsForbiddenFromEnteringRestrictedAreasUnlessCloaked )
                    {
                        if ( !Unit.IsCloaked )
                            canMoveIntoRestrictedAreas = false;
                    }

                    debugStage = 22110;

                    debugStage = 25100;

                    {
                        ISimMachineVehicle vehicleUnderCursor = CursorHelper.FindMachineVehicleUnderCursorIfNotDowned();

                        if ( vehicleUnderCursor != null )
                        {
                            //this is a big deal!  If a unit is selected and we are hovering over a vehicle, then we might get this unit into this vehicle
                            HandleAttemptToGetInVehicle( Unit, vehicleUnderCursor, center, groundCenter, destinationPoint, moveRange, attackRange,
                                hasValidDestinationPoint, false );
                            return;
                        }
                    }

                    {
                        ISimMapActor mapActorUnderCursor = CursorHelper.FindMapActorUnderCursor();
                        //if ( mapActorUnderCursor == Unit )
                        //{
                        //    DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );
                        //    return; //if we mouse over ourselves, then do rings only
                        //}

                        if ( mapActorUnderCursor != null && mapActorUnderCursor is ISimNPCUnit npcUnit && !npcUnit.GetIsPartOfPlayerForcesInAnyWay() )
                        {
                            //this is a big deal!  If a unit is selected and we are hovering over another unit, then we might shoot it or something
                            if ( !TryHandleAttemptToInteractWithHostileOrNeutralTargetUnit( Unit, mapActorUnderCursor, center, groundCenter, destinationPoint,
                                moveRange, attackRange, stance, groundLevel ) )
                            {
                                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );
                            }
                            return;
                        }
                    }

                    #region Mouseover Other From Android
                    {
                        //when in a mech, if you mouse over something else
                        ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                        if ( actorMousingOver != null && actorMousingOver != Unit )
                        {
                            //select it if it's one of our main ones
                            if ( actorMousingOver is ISimMachineActor || actorMousingOver is MachineStructure )
                            {
                                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );
                                MouseHelper.RenderWouldSelectTooltipAndCursor( actorMousingOver );
                                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )//select
                                    Engine_HotM.SetSelectedActor( actorMousingOver, false, true, true );
                                return;
                            }
                            else if ( actorMousingOver is ISimNPCUnit npc && npc.GetIsPartOfPlayerForcesInAnyWay() ) //also select it if it's one of our npcs
                            {
                                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );
                                MouseHelper.RenderWouldSelectTooltipAndCursor( actorMousingOver );
                                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )//select
                                    Engine_HotM.SetSelectedActor( actorMousingOver, false, true, true );
                                return;
                            }

                            //even we just missed the tooltip situation above, that's all
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )//select
                                Engine_HotM.SetSelectedActor( actorMousingOver, false, true, true );
                        }
                    }
                    #endregion

                    debugStage = 26050;

                    bool isBlocked = false;
                    MapItem isToBuilding = null;
                    SecurityClearance requiredClearance = null;
                    bool isIntersectingRoad = false;
                    bool isBlockedBySecurityClearance = false;
                    string debugText = string.Empty;
                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 26100;
                        if ( !MoveHelper.MachineAndroid_ActualDestinationIsValid( Unit, ref destinationPoint, ref canMoveIntoRestrictedAreas, out isBlocked,
                            out isToBuilding, out requiredClearance, out isIntersectingRoad, out isBlockedBySecurityClearance, out debugText ) )
                        {
                            hasValidDestinationPoint = false;
                            //debugText += "adv-block ";
                        }
                    }
                    //debugText = "isToBuilding: " + (isToBuilding == null ? "[null]" : isToBuilding.SimBuilding.GetDisplayName()) +
                    //    " hasValidDestinationPoint: " + hasValidDestinationPoint + " isBlocked: " + isBlocked + " canMoveIntoRestrictedAreas: " + canMoveIntoRestrictedAreas;

                    if ( !hasValidDestinationPoint )
                    {
                        //when an android, and you have a bad target, if you mouse over something else
                        ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                        if ( actorMousingOver != null && actorMousingOver != Unit )
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
                        }

                        if ( MouseHelper.StructureUnderCursor != null )
                        {
                            MouseHelper.RenderWouldSelectTooltipAndCursor( MouseHelper.StructureUnderCursor );
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( false );
                            return;
                        }
                    }

                    Lockdown blockedByLockdown = null;
                    if ( hasValidDestinationPoint )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                        }
                    }

                    debugStage = 32100;

                    float sqrDistToDest;

                    if ( isToBuilding != null )
                    {
                        //it is possible for different points on a building to be in and out of the ranges above, so
                        //we need to calculate our distance to the building in a bit of a different way
                        sqrDistToDest = (isToBuilding.CenterPoint.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;
                    }
                    else //if no building, just test to the point
                        sqrDistToDest = (destinationPoint.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;

                    MoveHelper.CalculateSprint( sqrDistToDest, moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar );

                    debugStage = 38100;

                    debugStage = 62100;
                    bool isStayingAtCurrentBuilding = false;
                    LocationActionType actionToTake = stance.ActionOnOutdoorArrive;
                    NPCEvent actionToTakeEventOrNull = null;
                    ProjectOutcomeStreetSenseItem actionToTakeStreetItemOrNull = null;
                    string actionToTakeOtherOptionalID = string.Empty;

                    if ( !isBeyondEvenSprinting && hasValidDestinationPoint )
                    {
                        debugStage = 72100;
                        if ( isToBuilding != null )
                        {
                            debugStage = 72200;
                            //if not a valid building, or someone is already in there
                            ISimUnit existingUnit = isToBuilding.SimBuilding?.CurrentOccupyingUnit;
                            if ( isToBuilding.SimBuilding == null || existingUnit != null )
                            {
                                if ( existingUnit != null && existingUnit.Equals( Unit ) ) //we are going to the same place we already are
                                {
                                    isStayingAtCurrentBuilding = true;
                                    destinationPoint = isToBuilding.SimBuilding.GetEffectiveWorldLocationForContainedUnit();
                                    MoveHelper.UnpackActionFromBuildingForActor( Unit, isToBuilding.SimBuilding, false, ref actionToTake, 
                                        ref actionToTakeEventOrNull, ref actionToTakeStreetItemOrNull, ref actionToTakeOtherOptionalID );
                                }
                                else if ( existingUnit != null )
                                {
                                    hasValidDestinationPoint = false;
                                    isBlocked = true;
                                    isToBuilding = null;
                                    //debugText += "blocking-unit ";

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                        novel.ShouldTooltipBeRed = true;
                                        novel.CanExpand = CanExpandType.Brief;

                                        novel.TitleUpperLeft.AddLang( "Move_AnotherUnitIsInYourWay" );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            if ( existingUnit.GetIsPlayerControlled() )
                                                novel.Main.AddLang( "Move_AnotherUnitIsInYourWay_Yours_StrategyTip" ).Line();
                                            else
                                                novel.Main.AddLang( "Move_AnotherUnitIsInYourWay_Other_StrategyTip" ).Line();
                                        }

                                        if ( debugText.Length > 0 )
                                            novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                                    }
                                }
                                else
                                {
                                    hasValidDestinationPoint = false;
                                    //debugText += "null-build ";
                                    isBlocked = true;
                                    isToBuilding = null;

                                    //this is some kind of invalid building
                                }
                            }
                            else //if IS a valid building, and no unit here, then the building itself is the new target
                            {
                                debugStage = 82100;
                                destinationPoint = isToBuilding.SimBuilding.GetEffectiveWorldLocationForContainedUnit();
                                MoveHelper.UnpackActionFromBuildingForActor( Unit, isToBuilding.SimBuilding, true, ref actionToTake, 
                                    ref actionToTakeEventOrNull, ref actionToTakeStreetItemOrNull, ref actionToTakeOtherOptionalID );
                            }
                        }
                        else //valid spot, but not to a building
                        {
                        }
                    }

                    bool isActionBlocked = false;
                    bool isBlockedByNeedingToSwitchToWorkOnSomethingElse = false;

                    if ( !FlagRefs.Ch0_FirstAlertHasBeenRead.DuringGameplay_IsTripped )
                    {
                        isBlockedByNeedingToSwitchToWorkOnSomethingElse = true;
                    }
                    else if ( SimCommon.AreAnyCityTasksActiveThatBlockNormalAndroidActions )
                    {
                        isBlockedByNeedingToSwitchToWorkOnSomethingElse = true;
                    }

                    if ( isBlockedByNeedingToSwitchToWorkOnSomethingElse )
                    {
                        actionToTake = null;
                        isToBuilding = null;
                    }

                    //ArcenDebugging.SendNoteToGameOnly( LocalizedString.AddNeverTranslated_New( "isToBuilding: " + (isToBuilding == null ? "null" :
                    //    isToBuilding.SimBuilding.GetDisplayName()) + "\n" +
                    //    "bNoFilter: " + (MouseHelper.BuildingNoFilterUnderCursor == null ? "null" :
                    //    MouseHelper.BuildingNoFilterUnderCursor.GetDisplayName()) + "\n" +
                    //    "bUnderCursor: " + (MouseHelper.BuildingUnderCursor == null ? "null" :
                    //    MouseHelper.BuildingUnderCursor.GetDisplayName()) + "\n" +
                    //    "markable: " + (Engine_HotM.MarkableUnderMouse == null ? "null" :
                    //    Engine_HotM.MarkableUnderMouse.GetType().ToString()) + "\n" ), NoteStyle.ShowInPassing, 0.1f );

                    if ( actionToTake != null && actionToTake.SkipBlockedBySecurityClearance )
                    {
                        isBlockedBySecurityClearance = false;
                    }

                    ActionResult actionResult = ActionResult.Success;
                    if ( actionToTake != null)
                    {
                        actionResult = actionToTake.Implementation.TryHandleLocationAction( Unit, isToBuilding?.SimBuilding, destinationPoint, actionToTake, 
                            actionToTakeEventOrNull, actionToTakeStreetItemOrNull,
                            actionToTakeOtherOptionalID, ActionLogic.CalculateIfBlocked, out _, extraEnergyCostFromMovingFar );
                        isActionBlocked = actionResult == ActionResult.Blocked;
                        if ( !actionToTake.CanBeDoneByAndroids )
                            isActionBlocked = true;

                        if ( isActionBlocked )
                        {
                            isBlocked = true;
                        }
                    }

                    Vector3 drawnDestination = destinationPoint; //draw to the main spot, not to bumped-up

                    debugStage = 92100;
                    if ( hasValidDestinationPoint && isToBuilding == null )
                    {
                        destinationPoint = destinationPoint.ReplaceY( isIntersectingRoad ? 1.3f : 0.4f );
                        if ( drawnDestination.y > destinationPoint.y )
                            drawnDestination.y = destinationPoint.y;
                    }

                    if ( isBeyondEvenSprinting )
                    {
                        hasValidDestinationPoint = false;
                    }

                    bool skipThreatLinesFromDestination = isBeyondEvenSprinting || extraEnergyCostFromMovingFar > 0 || (actionToTake?.SkipThreatLinesAtDestination ?? false);
                    if ( !isActionBlocked )
                    {
                        if ( actionToTake != null && actionToTake.IsDoneFromADistance )
                        {
                            //we are staying here and doing the destination from afar
                            MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, Unit.GetDrawLocation(), Unit.ContainerLocation.Get() as ISimBuilding,
                                false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                                out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies, 
                                null, AttackAmounts.Zero(), EnemyTargetingReason.CurrentLocation_NoOp, skipThreatLinesFromDestination, ThreatLineLogic.Normal );
                        }
                        else //we are moving to the destination
                        {
                            if ( hasValidDestinationPoint )
                                MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, drawnDestination, isToBuilding?.SimBuilding,
                                    !isStayingAtCurrentBuilding, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                                    out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                    out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies, 
                                    null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, skipThreatLinesFromDestination, ThreatLineLogic.Normal );
                            else
                                CombatTextHelper.ClearPredictionStats();
                        }
                    }
                    else
                        CombatTextHelper.ClearPredictionStats();

                    #region The Tooltip Writing
                    //bool tellMeAboutSprintDangerIfNoActon = false;
                    {
                        debugStage = 93100;
                        if ( isBeyondEvenSprinting && blockedByLockdown == null && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                        {
                            debugStage = 93200;

                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.CanExpand = CanExpandType.Brief;

                                novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddLang( "Move_OutOfRange_StrategyTip" );

                                if ( debugText.Length > 0 )
                                    novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                            }
                        }

                        if ( blockedByLockdown != null )
                        {
                            debugStage = 93200;
                            MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                        }
                        else if ( !hasValidDestinationPoint && isBlockedBySecurityClearance )
                        {
                            if ( FlagRefs.HasLearnedThereIsNoSafetyWithHumans.DuringGameplay_IsTripped )
                                HandbookRefs.BlockedByInsufficientClearance.DuringGame_UnlockIfNeeded( true );

                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.CanExpand = CanExpandType.Brief;

                                novel.TitleUpperLeft.AddLang( "Move_InsufficientSecurityClearance" );

                                CombatTextHelper.AppendRestrictedAreaShort( Unit, novel.Main,
                                    InputCaching.ShouldShowDetailedTooltips, isToBuilding?.SimBuilding, destinationPoint, false );

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddLang( "Move_InsufficientSecurityClearance_StrategyTip" );

                                if ( debugText.Length > 0 )
                                    novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                            }
                        }
                        else if ( !isBeyondEvenSprinting && hasValidDestinationPoint )
                        {
                            debugStage = 96100;
                            if ( isToBuilding != null )
                            {
                                debugStage = 96200;
                                //if not a valid building, or someone is already in there
                                ISimUnit existingUnit = isToBuilding.SimBuilding?.CurrentOccupyingUnit;
                                if ( isToBuilding.SimBuilding == null || existingUnit != null )
                                {
                                    if ( (existingUnit?.ActorID ?? 0) == Unit.ActorID )
                                    { } //unit staying where it is
                                    else if ( existingUnit != null )
                                    {
                                        //unit blocked by another unit
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                        {
                                            novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                            novel.ShouldTooltipBeRed = true;
                                            novel.CanExpand = CanExpandType.Brief;

                                            novel.TitleUpperLeft.AddLang( "Move_BlockedByAnotherUnit" );
                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                novel.Main.AddLang( "Move_BlockedByAnotherUnit_StrategyTip" );

                                            if ( debugText.Length > 0 )
                                                novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                                        }
                                    }
                                    else
                                    {
                                        isBlocked = true;
                                        //this is some kind of invalid building other than that
                                    }
                                }
                                else //if IS a valid building, then the building itself is the new target
                                {
                                    debugStage = 97200;
                                }
                            }
                            else //valid spot, but not to a building
                            {
                                
                            }
                        }

                        bool isSprinting = !isBeyondEvenSprinting && extraEnergyCostFromMovingFar > 0 && !isBlocked && hasValidDestinationPoint;

                        //if ( !canAffordExtraEnergy )
                        //    hasValidDestinationPoint = false;

                        bool includeRestrictedAreaNoticeInTooltip = true;
                        debugStage = 98100;
                        if ( actionToTake != null && !isBeyondEvenSprinting && hasValidDestinationPoint && blockedByLockdown == null )
                        {
                            if ( !actionToTake.SkipNormalActionTooltip )
                            {
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    novel.Icon = actionToTake.Icon;
                                    novel.ShouldTooltipBeRed = isActionBlocked;
                                    novel.TitleUpperLeft.AddRaw( actionToTake.GetDisplayName() );

                                    bool hasDescription = !actionToTake.GetDescription().IsEmpty();
                                    if ( hasDescription )
                                        novel.CanExpand = CanExpandType.Brief;

                                    if ( hasDescription && InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        novel.FrameTitle.AddLang( "Move_ActionDetails" );

                                        if ( !actionToTake.GetDescription().IsEmpty() )
                                            novel.FrameBody.AddRaw( actionToTake.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                        if ( !actionToTake.StrategyTipOptional.Text.IsEmpty() )
                                            novel.FrameBody.AddRaw( actionToTake.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                    }

                                    actionToTake.Implementation.TryHandleLocationAction( Unit, isToBuilding?.SimBuilding, destinationPoint, actionToTake, 
                                        actionToTakeEventOrNull, actionToTakeStreetItemOrNull,
                                        actionToTakeOtherOptionalID, ActionLogic.AppendToTooltip, out includeRestrictedAreaNoticeInTooltip, extraEnergyCostFromMovingFar );

                                    if ( !actionToTake.CanBeDoneByAndroids )
                                        novel.Main.AddLang( "ActionCannotBeDoneByThisKindOfUnit", ColorTheme.RedOrange2 );
                                    else
                                    {
                                        if ( includeRestrictedAreaNoticeInTooltip )
                                            CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.Main, 
                                                novel.Main, InputCaching.ShouldShowDetailedTooltips, isToBuilding?.SimBuilding, destinationPoint, true );
                                    }

                                    if ( isSprinting )
                                    {
                                        string energyHex = canAffordExtraEnergy ? string.Empty : ColorTheme.RedOrange2;

                                        if ( !canAffordExtraEnergy )
                                            novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.RemoveLastCharacterIfNewline();
                                        novel.TitleUpperLeft.Space2x()
                                            .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                                            .AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex );

                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex )
                                                .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex );
                                            novel.Main.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Line();

                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_Sprinting_Details" );
                                        }
                                    }

                                    if ( debugText.Length > 0 )
                                        novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                                }
                            }
                            else
                            {
                                //normal tooltip is being skipped, probably because of this being a basic move

                                bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( Unit );
                                bool hasRestrictedArea = CombatTextHelper.GetIsMovingToRestrictedArea( Unit, isToBuilding?.SimBuilding, destinationPoint, true );

                                if ( hasCombatLines || hasRestrictedArea )
                                {
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        if ( isSprinting )
                                        {
                                            string energyHex = canAffordExtraEnergy ? string.Empty : ColorTheme.RedOrange2;

                                            if ( !canAffordExtraEnergy )
                                                novel.ShouldTooltipBeRed = true;
                                            novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                            novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                            novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Space2x();
                                        }

                                        if ( hasCombatLines && !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.TitleUpperLeft, TTTextBefore.None, TTTextAfter.None );
                                            if ( hasRestrictedArea )
                                                CombatTextHelper.AppendRestrictedAreaShort( Unit, novel.MainHeader, false, isToBuilding?.SimBuilding, destinationPoint, true );
                                        }
                                        else
                                        {
                                            if ( hasCombatLines )
                                            {
                                                CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.TitleUpperLeft, TTTextBefore.None, TTTextAfter.None );

                                                CombatTextHelper.AppendLastPredictedDamageLongSecondary( Unit,
                                                    InputCaching.ShouldShowDetailedTooltips, false, false, true );

                                                if ( hasRestrictedArea )
                                                    CombatTextHelper.AppendRestrictedAreaShort( Unit, novel.Main,
                                                        InputCaching.ShouldShowDetailedTooltips, isToBuilding?.SimBuilding, destinationPoint, true );
                                            }
                                            else
                                            {
                                                if ( hasRestrictedArea )
                                                {
                                                    CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.TitleUpperLeft, novel.Main,
                                                        InputCaching.ShouldShowDetailedTooltips, isToBuilding?.SimBuilding, destinationPoint, true );
                                                }
                                            }
                                        }

                                        if ( isSprinting && InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            string energyHex = canAffordExtraEnergy ? string.Empty : ColorTheme.RedOrange2;

                                            novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                            novel.Main.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Line();

                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_Sprinting_Details" );
                                        }

                                        novel.CanExpand = CanExpandType.Brief;

                                        if ( debugText.Length > 0 )
                                            novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                                    }
                                }
                                else
                                {
                                    if ( isSprinting )
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                        {
                                            string energyHex = canAffordExtraEnergy ? string.Empty : ColorTheme.RedOrange2;
                                            string apHex = Unit.CurrentActionPoints > 0 ? string.Empty : ColorTheme.RedOrange2;

                                            novel.ShouldTooltipBeRed = !canAffordExtraEnergy || Unit.CurrentActionPoints == 0;
                                            novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                            novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                            novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Space2x();

                                            novel.TitleUpperLeft.AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex );
                                            novel.TitleUpperLeft.AddRaw( "1", apHex );

                                            //if we're out of action range, then explain that
                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_Sprinting_Details" );

                                            novel.CanExpand = CanExpandType.Brief;

                                            if ( debugText.Length > 0 )
                                                novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                        drawnDestination.y = groundLevel;

                    Color moveColor;
                    bool drawThinLine = false;//ColorRefs.MachineUnitMoveNoTarget.ColorHDR

                    debugStage = 99100;
                    if ( isBeyondEvenSprinting )
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, drawnDestination );
                        moveColor = Color.red;
                        drawThinLine = true;
                    }
                    else if ( isActionBlocked )
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, drawnDestination );
                        moveColor = Color.red;
                    }
                    else if ( Unit.CurrentActionPoints == 0 )
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, ActorRefs.ActorMaxActionPoints.Icon, IconRefs.Mouse_OutOfRange, drawnDestination );
                        moveColor = Color.red;
                        drawThinLine = true;

                        if ( (InputCaching.ShouldShowDetailedTooltips || SimCommon.Turn == 1) && ResourceRefs.MentalEnergy.Current > 1 )
                        {
                            //unit is out of AP
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips || SimCommon.Turn == 1 ? TooltipNovelWidth.Smaller : TooltipNovelWidth.SizeToText ) )
                            {
                                novel.Icon = ActorRefs.ActorMaxActionPoints.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.CanExpand = CanExpandType.Brief;

                                novel.TitleUpperLeft.AddLang( "Move_OutOfActionPoints" );
                                if ( InputCaching.ShouldShowDetailedTooltips || SimCommon.Turn == 1 )
                                    novel.Main.AddLang( "Move_OutOfActionPoints_StrategyTip" );

                                if ( debugText.Length > 0 )
                                    novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                            }
                        }
                    }
                    else if ( hasValidDestinationPoint )
                    {
                        if ( ((actionToTake?.IsDoneFromADistance ?? false)) )
                        {
                            moveColor = ColorRefs.MachineActorActionOverDistanceLine.ColorHDR;
                        }
                        else
                        {
                            if ( extraEnergyCostFromMovingFar > 0 )
                            {
                                //sprinting
                                if ( actionToTake == null || actionToTake.SkipActingAsIfActionIsHereForLineColor )
                                    moveColor = ColorRefs.MachineUnitSprintGeneralLine.ColorHDR;
                                else
                                    moveColor = ColorRefs.MachineUnitSprintToActionLine.ColorHDR;
                            }
                            else
                            {
                                if ( actionToTake == null || actionToTake.SkipActingAsIfActionIsHereForLineColor )
                                {
                                    moveColor = isToBuilding != null ? ColorRefs.MachineUnitMoveBuildingLine.ColorHDR : ColorRefs.MachineUnitMoveGroundLine.ColorHDR;
                                    drawThinLine = true;
                                }
                                else
                                    moveColor = ColorRefs.MachineUnitMoveActionLine.ColorHDR;
                            }
                        }

                        if ( isBlockedByNeedingToSwitchToWorkOnSomethingElse || actionToTake == null )
                        {
                            CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_Valid_ButNoTarget, drawnDestination, 0.2f ); //scale it very small
                        }
                        else
                        {
                            if ( isToBuilding?.SimBuilding == null )
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_MoveToPoint, drawnDestination, moveColor );
                            else
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, drawnDestination, moveColor );
                        }
                    }
                    else
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, drawnDestination );
                        moveColor = Color.red;
                        drawThinLine = true;
                    }

                    debugStage = 102100;

                    debugStage = 105100;
                    DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );

                    if ( !isStayingAtCurrentBuilding )
                    {
                        if ( stance.IsNewAttackRangeProjected )
                            DrawHelper.RenderRangeCircle( drawnDestination.ReplaceY( groundLevel - 0.08f ), attackRange, ColorRefs.UnitNewLocationAttackRangeBorder.ColorHDR, 1.5f );
                    }

                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center, drawnDestination,
                        moveColor, drawThinLine ? 1f : 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                    #endregion

                    if ( isBlockedByNeedingToSwitchToWorkOnSomethingElse )
                    {
                        if ( !FlagRefs.Ch0_FirstAlertHasBeenRead.DuringGameplay_IsTripped )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                            {
                                novel.TitleUpperLeft.AddLang( "ThereIsAnAlertInTheUpperRight" );
                                novel.Main.AddLang( "ThereIsAnAlertInTheUpperRight_Details" );
                            }
                        }
                        else if ( SimCommon.AreAnyCityTasksActiveThatBlockNormalAndroidActions )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                            {
                                novel.TitleUpperLeft.AddLang( "ThereAreInstructionsInTheUpperRight" );
                                novel.Main.AddLang( "ThereAreInstructionsInTheUpperRight_Details" );
                            }
                        }

                        {
                            if( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                            {
                                novel.TitleUpperLeft.AddLang( "Move_ThereAreOtherThingsYouNeedToWorkOnFirst" );
                            }
                        }
                    }

                    debugStage = 112100;
                    #region Handle Clicks
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                    {
                        debugStage = 118200;
                        if ( hasValidDestinationPoint && !isActionBlocked && actionToTake != null && canAffordExtraEnergy &&
                            Unit.CurrentActionPoints >= 1 && ResourceRefs.MentalEnergy.Current >= 1 && !Unit.GetIsMovingRightNow() && !isBlockedByNeedingToSwitchToWorkOnSomethingElse &&
                            SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                        {
                            switch ( actionResult )
                            {
                                case ActionResult.Success:
                                    debugStage = 118500;
                                    ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                                        delegate
                                        {
                                            SimCommon.HasGivenAnyAndroidMovementOrders = true;
                                            InnerDoActionForAndroid( isStayingAtCurrentBuilding, Unit, center, actionToTake,
                                                isToBuilding, isToBuilding?.SimBuilding, destinationPoint, actionToTakeEventOrNull, 
                                                actionToTakeStreetItemOrNull, actionToTakeOtherOptionalID, extraEnergyCostFromMovingFar );
                                        } );
                                    break;
                            }
                        }
                    }
                    else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( false );
                    #endregion
                }

                debugStage = 201200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "AndroidMoveAndAttackLogic HandleMouseInteractionsAndAnyExtraRendering_Android Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public static void DrawAndroidRangeCircles( Vector3 groundCenter, float attackRange, float moveRange )
        {
            DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR, 2f );
            if ( moveRange != attackRange )
                DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), moveRange, ColorRefs.UnitMoveRangeBorder.ColorHDR, 2f );
        }

        #region InnerDoActionForAndroid
        private static void InnerDoActionForAndroid( bool isStayingAtCurrentBuilding, ISimMachineUnit Unit, Vector3 center,
            LocationActionType actionToTake, MapItem isToBuilding, ISimBuilding building, Vector3 destinationPoint, 
            NPCEvent actionToTakeEventOrNull, ProjectOutcomeStreetSenseItem actionToTakeStreetItemOrNull, string actionToTakeOptionalID, int ExtraEnergyCostFromMovingFar )
        {
            if ( !isStayingAtCurrentBuilding )
            {
                {
                    Unit.AlterCurrentActionPoints( -1 );
                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                    if ( ExtraEnergyCostFromMovingFar > 0 )
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -ExtraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );
                }

                bool actuallyMove = true;
                if ( actionToTake != null )
                {
                    if ( actionToTake.IsDoneFromADistance ) //doing this from a distance
                    {
                        actuallyMove = false;
                        if ( actionToTake.Implementation.TryHandleLocationAction( Unit, building, destinationPoint,
                            actionToTake, actionToTakeEventOrNull, actionToTakeStreetItemOrNull, actionToTakeOptionalID,
                            ActionLogic.Execute, out _, 0 ) == ActionResult.Success )
                        {
                            Unit.ApplyVisibilityFromAction( actionToTake.VisibilityStyle );
                            actionToTake.OnArrive.DuringGame_PlayAtLocation( building.GetMapItem().TopCenterPoint, Unit.GetDrawRotation().eulerAngles );
                            postAbilityRand.ReinitializeWithSeed( Unit.CurrentTurnSeed + actionToTake.RowIndexNonSim );
                            actionToTake.DuringGameplay_ApplyActionStatistics( postAbilityRand );

                            //make sure that things are newly-random after moving
                            Unit.RerollCurrentTurnSeed();
                        }
                    }
                    else //doing this after moving to the location
                        Unit.SetActionToTakeAfterMovementEnds( actionToTake, isToBuilding?.SimBuilding, actionToTakeEventOrNull, actionToTakeStreetItemOrNull, actionToTakeOptionalID );
                }

                if ( actuallyMove )
                {
                    Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );

                    if ( isToBuilding?.SimBuilding != null )
                        Unit.SetDesiredContainerLocation( isToBuilding.SimBuilding );
                    else
                        Unit.SetDesiredGroundLocation( destinationPoint );
                    CityStatisticTable.AlterScore( "AndroidMovements", 1 );
                }
            }
            else //staying at current building
            {
                if ( actionToTake.Implementation.TryHandleLocationAction( Unit, building, destinationPoint, 
                    actionToTake, actionToTakeEventOrNull, actionToTakeStreetItemOrNull, actionToTakeOptionalID,
                    ActionLogic.Execute, out _, 0 ) == ActionResult.Success )
                {
                    if ( !actionToTake.SkipCostsIfAtSameLocation )
                    {
                        Unit.AlterCurrentActionPoints( -1 );
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    Unit.ApplyVisibilityFromAction( actionToTake.VisibilityStyle );
                    actionToTake.OnArrive.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                    postAbilityRand.ReinitializeWithSeed( Unit.CurrentTurnSeed + actionToTake.RowIndexNonSim );
                    actionToTake.DuringGameplay_ApplyActionStatistics( postAbilityRand );
                }
            }
        }
        #endregion

        #region HandleAttemptToGetInVehicle
        private static void HandleAttemptToGetInVehicle( ISimMachineUnit Unit, ISimMachineVehicle vehicleUnderCursor, Vector3 center, Vector3 groundCenter,
            Vector3 destinationPoint, float moveRange, float attackRange, bool hasValidDestinationPoint, bool showTooltipAsRed )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 100;
            try
            {
                MachineUnitStorageSlotType storageSlotType = Unit.UnitType.StorageSlotType;
                Vector3 vehicleLocation = vehicleUnderCursor.WorldLocation.PlusY( vehicleUnderCursor.GetHalfHeightForCollisions() );
                destinationPoint = vehicleLocation;
                float sqrDistToVehicle = (vehicleLocation.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;
                bool vehicleHasRoomForUnit = true;

                MoveHelper.CalculateSprint( sqrDistToVehicle, moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar );
                if ( isBeyondEvenSprinting || !canAffordExtraEnergy )
                    hasValidDestinationPoint = false;

                int vehicleTotalSpotsForThisUnitType = vehicleUnderCursor.GetRemainingUnitSlotsOfType( storageSlotType );
                if ( vehicleTotalSpotsForThisUnitType <= 0 )
                    vehicleHasRoomForUnit = false;

                Lockdown blockedByLockdown = null;
                if ( hasValidDestinationPoint )
                {
                    if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                    {
                        hasValidDestinationPoint = false;
                        showTooltipAsRed = true;
                    }
                }

                if ( !vehicleUnderCursor.CanThisContainUnit( Unit.UnitType, null ) )
                {
                    hasValidDestinationPoint = false;
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                    {
                        novel.Icon = IconRefs.Mouse_Invalid.Icon;
                        novel.ShouldTooltipBeRed = true;
                        vehicleUnderCursor.CanThisContainUnit( Unit.UnitType, novel.TitleUpperLeft );
                    }
                }
                else if ( blockedByLockdown != null )
                {
                    debugStage = 93200;
                    showTooltipAsRed = true;
                    MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                }
                else if ( vehicleUnderCursor == Unit.ContainerLocation.Get() )
                {
                    hasValidDestinationPoint = false;
                    vehicleHasRoomForUnit = false;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                    {
                        novel.Icon = IconRefs.Mouse_Invalid.Icon;
                        novel.ShouldTooltipBeRed = true;
                        novel.TitleUpperLeft.AddLang( "Move_UnitAlreadyInVehicle" );
                    }
                }
                else if ( !vehicleHasRoomForUnit )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                    {
                        novel.CanExpand = CanExpandType.Brief;
                        novel.Icon = IconRefs.Mouse_Invalid.Icon;
                        novel.ShouldTooltipBeRed = true;
                        novel.TitleUpperLeft.AddLang( "Move_VehicleHasNoSpaceForUnit" );

                        if ( InputCaching.ShouldShowDetailedTooltips )
                        {
                            if ( vehicleTotalSpotsForThisUnitType == 0 )
                                novel.Main.AddFormat1( "Move_VehicleHasNoSpaceForUnit_NoneOfType_StrategyTip",
                                    storageSlotType.GetDisplayName() ).Line();
                            else
                                novel.Main.AddFormat2( "Move_VehicleHasNoSpaceForUnit_StrategyTip",
                                    storageSlotType.GetDisplayName(), vehicleTotalSpotsForThisUnitType ).Line();
                        }
                    }
                }
                else if ( isBeyondEvenSprinting )
                {
                    showTooltipAsRed = true;
                    if ( !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );

                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "Move_OutOfRange_StrategyTip" ).Line();
                        }
                    }
                }
                else
                {
                    MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, vehicleUnderCursor.GetDrawLocation(), null,
                        true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                        out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                        out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                        null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, true, ThreatLineLogic.ForceConsiderMovingOutOfRange );

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                    {
                        novel.CanExpand = CanExpandType.Brief;
                        novel.Icon = IconRefs.MouseMoveMode_BoardVehicle.Icon;
                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_BoardVehicle" ).AddRaw( vehicleUnderCursor.GetDisplayName() );

                        if ( InputCaching.ShouldShowDetailedTooltips )
                            novel.Main.AddLang( "Move_BoardVehicle_StrategyTip" ).Line();

                        CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.Main, TTTextBefore.SpacingAlways, TTTextAfter.None );
                    }
                }

                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );

                //draw the sole version
                DrawHelper.RenderCatmullLine( center, vehicleLocation,
                    hasValidDestinationPoint ? ColorRefs.MachineUnitMoveActionLine.ColorHDR : Color.red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );

                if ( isBeyondEvenSprinting )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, vehicleLocation );
                else if ( !vehicleHasRoomForUnit )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, vehicleLocation );
                else
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_BoardVehicle, vehicleLocation );

                #region Handle Clicks
                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                {
                    if ( hasValidDestinationPoint && vehicleHasRoomForUnit && !isBeyondEvenSprinting && !Unit.GetIsMovingRightNow()
                        && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 &&
                        ResourceRefs.MentalEnergy.Current >= 1 + extraEnergyCostFromMovingFar && Unit.CurrentActionPoints >= 1 )
                    {
                        ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.ForceConsiderMovingOutOfRange,
                            delegate
                            {
                                SimCommon.HasGivenAnyAndroidMovementOrders = true;
                                LocationActionType boardVehicleAction = CommonRefs.BoardVehicleAction;

                                Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                Unit.AlterCurrentActionPoints( -1 );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                if ( extraEnergyCostFromMovingFar > 0 )
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );
                                Unit.ApplyVisibilityFromAction( boardVehicleAction.VisibilityStyle );
                                Unit.SetActionToTakeAfterMovementEnds( boardVehicleAction, null, null, null, string.Empty );
                                Unit.SetDesiredContainerLocation( vehicleUnderCursor );
                            } );
                    }
                }
                else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                    MouseHelper.BasicLeftClickHandler( false );
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "AndroidMoveAndAttackLogic HandleAttemptToGetInVehicle Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion HandleAttemptToGetInVehicle

        #region TryHandleAttemptToInteractWithHostileOrNeutralTargetUnit
        private static bool TryHandleAttemptToInteractWithHostileOrNeutralTargetUnit( ISimMachineUnit Unit, ISimMapActor actorUnderCursor, Vector3 center, Vector3 groundCenter,
            Vector3 destinationPoint, float moveRange, float attackRange,
            MachineUnitStance stance, float groundLevel )
        {
            if ( actorUnderCursor == Unit )
                return false; //if self, don't do much

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 100;
            try
            {
                Vector3 targetUnitLocation = actorUnderCursor.GetPositionForCollisions();
                destinationPoint = targetUnitLocation;
                float sqrDistToTargetUnit = (targetUnitLocation.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;
                bool isTargetUnitOutOfAttackRange = false;
                bool hasValidDestinationPoint = true;

                if ( sqrDistToTargetUnit > attackRange * attackRange )
                    isTargetUnitOutOfAttackRange = true;

                MoveHelper.CalculateSprint( sqrDistToTargetUnit, moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar );

                if ( isBeyondEvenSprinting || !canAffordExtraEnergy ) //definitely disallowed
                    hasValidDestinationPoint = false;

                ISimNPCUnit npcUnit = actorUnderCursor as ISimNPCUnit;

                bool wouldBeMeleeAttack = false;
                bool wouldMoveInClose = false;
                bool wouldBeAttack = !actorUnderCursor.GetIsAnAllyFromThePlayerPerspective();
                bool wouldBeDialog = false;
                bool canDoAction = true;
                bool canAfford = true;
                int apCost = wouldBeAttack ? Unit.UnitType.APCostPerAttack : 1;

                bool canTargetNoncombatants = Unit.IsInAbilityTypeTargetingMode?.AllowsTargetingNoncombatants ?? false;

                if ( wouldBeAttack && canTargetNoncombatants && !(npcUnit?.IsManagedUnit?.IsBlockedFromAnyKilling ?? false) )
                { } //if you are specifically trying to attack noncombatants, and they are not blocked from being killed, then dialog does not happen
                else
                {
                    if ( npcUnit != null )
                    {
                        if ( npcUnit.IsManagedUnit?.DialogToShow != null && !npcUnit.IsManagedUnit.DialogToShow.DuringGame_HasHandled )
                        {
                            wouldBeAttack = false;
                            wouldBeDialog = true;
                            wouldMoveInClose = true;
                        }
                    }
                }

                if ( Unit.IsInAbilityTypeTargetingMode != null )
                {
                    if ( wouldBeAttack && Unit.IsInAbilityTypeTargetingMode.PreventsNormalHostileUnitInteractions )
                        return false;
                    if ( !wouldBeAttack && Unit.IsInAbilityTypeTargetingMode.PreventsNormalFriendlyUnitInteractions )
                        return false;
                }

                if ( wouldBeAttack )
                {
                    wouldBeMeleeAttack = isTargetUnitOutOfAttackRange || !Unit.CanMakeRangedAttacks();

                    canAfford = Unit.CurrentActionPoints >= apCost; //Do we have that much?

                    if ( !canAfford )
                        canDoAction = false;
                    if ( npcUnit != null && npcUnit.UnitType.DeathsCountAsMurders && !canTargetNoncombatants )
                        canDoAction = false;

                    if ( !FlagRefs.Ch0_FirstAlertHasBeenRead.DuringGameplay_IsTripped )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                        {
                            novel.TitleUpperLeft.AddLang( "ThereIsAnAlertInTheUpperRight" );
                            novel.Main.AddLang( "ThereIsAnAlertInTheUpperRight_Details" );
                        }
                        return false;
                    }
                    else if ( SimCommon.AreAnyCityTasksActiveThatBlockNormalAndroidActions )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                        {
                            novel.TitleUpperLeft.AddLang( "ThereAreInstructionsInTheUpperRight" );
                            novel.Main.AddLang( "ThereAreInstructionsInTheUpperRight_Details" );
                        }
                        return false;
                    }

                    if ( npcUnit?.IsManagedUnit?.IsBlockedFromAnyKilling??false )
                    {
                        canDoAction = false;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "BlockedFromAttacking_Header" );
                            novel.Main.AddLang( npcUnit.IsManagedUnit.LangKeyForKillingBlock );
                        }

                        return false;
                    }

                    if ( canDoAction )
                    {
                        if ( !Unit.GetIsValidToAutomaticallyShootAt_Current( actorUnderCursor ) )
                        {
                            canDoAction = false;
                            if ( Unit?.UnitType?.IsTiedToShellCompany??false )
                            {
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                                {
                                    novel.ShouldTooltipBeRed = true;
                                    novel.TitleUpperLeft.AddLang( "BlockedFromAttacking_Header" );
                                    novel.Main.AddLang( "WouldRevealYourShellCompany_Tied" );
                                }
                            }
                            else
                            {
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.SuperSmall ) )
                                {
                                    novel.ShouldTooltipBeRed = true;
                                    novel.TitleUpperLeft.AddLang( "BlockedFromAttacking_Header" );
                                    novel.Main.AddLang( "WouldRevealYourShellCompany_Distant" );
                                }
                            }

                            //DrawHelper.RenderCatmullLine( center.PlusY( Unit.GetHalfHeightForCollisions() ), targetUnitLocation,
                            //    hasValidDestinationPoint ?
                            //     (wouldBeAttack ? ColorRefs.MachineUnitAttackLine.ColorHDR :
                            //     ColorRefs.MachineUnitHelpLine.ColorHDR)
                            //    : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );

                            //return false;
                        }
                    }
                }
                else
                {
                    canAfford = Unit.CurrentActionPoints >= apCost; //Do we have that much?
                    canDoAction = true;//say we can do it no matter what
                }

                ISimUnit unitUnderCursor = actorUnderCursor as ISimUnit;
                if ( wouldBeAttack && unitUnderCursor == null )
                    return false;

                Lockdown blockedByLockdown = null;

                ISimUnitLocation targetLocation = null;
                bool wouldActuallyMove = false;

                if ( wouldBeMeleeAttack || wouldMoveInClose )
                {
                    //find the nearest spot to this location
                    targetLocation = PathingHelper.FindBestMachineUnitLocationNearCoordinatesFromSearchSpot( Unit, destinationPoint, destinationPoint, 3f, 
                        1 ); //if it's clearance 1, we don't care
                    if ( targetLocation == null )
                    {
                        canDoAction = false;
                        hasValidDestinationPoint = false;
                    }
                    else
                    {
                        destinationPoint = targetLocation.GetEffectiveWorldLocationForContainedUnit();
                        wouldActuallyMove = true;
                    }
                }

                AttackAmounts predictedAttack = AttackAmounts.Zero();
                int predictedTargetSquadMembersLost = 0;
                if ( wouldBeAttack )
                {
                    secondaryBufferForKeyAttackNotes.EnsureResetForNextUpdate();
                    predictedAttack = AttackHelper.HandlePlayerAttackPrediction( Unit, unitUnderCursor,
                        isTargetUnitOutOfAttackRange, CalculationType.PredictionDuringPlayerTurn, 0, wouldActuallyMove, destinationPoint,
                        secondaryBufferForKeyAttackNotes );

                    if ( actorUnderCursor is ISimNPCUnit targetNPC )
                    {
                        predictedTargetSquadMembersLost = targetNPC.CalculateSquadSizeLossFromDamage( predictedAttack.Physical );
                    }
                }
                else
                    secondaryBufferForKeyAttackNotes.EnsureResetForNextUpdate();

                if ( wouldBeMeleeAttack || wouldMoveInClose )
                {
                    if ( canDoAction )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                            canDoAction = false;
                            wouldActuallyMove = false;
                        }
                    }

                    //against where the android would go
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), targetLocation as ISimBuilding,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            wouldBeAttack ? unitUnderCursor : null, predictedAttack, EnemyTargetingReason.ProposedDestination, isTargetUnitOutOfAttackRange, ThreatLineLogic.Normal );
                }
                else
                {
                    //against the current location of the android
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, center.PlusY( Unit.GetHalfHeightForCollisions() ), Unit.ContainerLocation.Get() as ISimBuilding,
                            false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            wouldBeAttack ? unitUnderCursor : null, predictedAttack, wouldBeAttack ? EnemyTargetingReason.CurrentLocation_Attacking : 
                            EnemyTargetingReason.CurrentLocation_NoOp, false, ThreatLineLogic.Normal );
                }

                bool isSprinting = !isBeyondEvenSprinting && extraEnergyCostFromMovingFar > 0 && canDoAction && hasValidDestinationPoint && blockedByLockdown == null;

                if ( wouldBeAttack && isSprinting )
                {
                    if ( !wouldBeMeleeAttack ) //this should not happen, but block us if it does
                        canDoAction = false;
                }

                if ( canDoAction )
                {
                    MobileActorTypeDuringGameData dgd = Unit.GetTypeDuringGameData();
                    if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                    {
                        foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                        {
                            if ( kv.Value > kv.Key.Current )
                            {
                                canDoAction = false;
                                break;
                            }
                        }
                    }
                }

                #region Tooltip For Attack Or Assist
                if ( blockedByLockdown != null )
                {
                    MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                }
                else if ( isBeyondEvenSprinting )
                {
                    if ( !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                    {
                        bool isAbbreviated = wouldBeAttack && !InputCaching.ShouldShowDetailedTooltips;
                        if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.ShouldTooltipBeRed = true;
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.Mouse_OutOfRange.Icon;

                            if ( wouldBeAttack )
                            {
                                InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, 
                                    Unit, novel.TitleUpperLeft, isAbbreviated ? novel.MainHeader : novel.Main, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                    isTargetUnitOutOfAttackRange, wouldActuallyMove, destinationPoint, secondaryBufferForKeyAttackNotes, apCost, 1 + extraEnergyCostFromMovingFar,
                                    false, true, targetLocation, destinationPoint, true );
                            }
                            else
                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_OutOfRange" ).AddRaw( actorUnderCursor.GetDisplayName() );

                            if ( InputCaching.ShouldShowDetailedTooltips )
                            {
                                novel.FrameTitle.AddLangAndAfterLineItemHeader( "Move_OutOfRange" );
                                novel.FrameBody.AddLang( "Attack_OutOfRange_StrategyTip" ).Line();
                            }
                        }
                    }
                }
                else
                {
                    if ( !canDoAction )
                    {
                        if ( wouldBeAttack )
                        {
                            if ( npcUnit != null && npcUnit.UnitType.DeathsCountAsMurders && !canTargetNoncombatants )
                            {
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( actorUnderCursor ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    novel.CanExpand = CanExpandType.Brief;
                                    novel.Icon = IconRefs.MurderCount.Icon;
                                    novel.ShouldTooltipBeRed = true;
                                    novel.TitleUpperLeft.AddLang( "Attack_Noncombatant" );

                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                       novel.Main.AddLang( "Attack_Noncombatant_Details" ).Line();
                                }
                            }
                            else if ( wouldBeMeleeAttack )
                            {
                                bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                                if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.ShouldTooltipBeRed = true;
                                    novel.CanExpand = CanExpandType.Brief;
                                    novel.Icon = IconRefs.MouseMoveMode_MeleeAttack.Icon;
                                    //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Melee" ).AddRaw( actorUnderCursor.GetDisplayName() );

                                    ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                                    InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                        isTargetUnitOutOfAttackRange, wouldActuallyMove, destinationPoint, secondaryBufferForKeyAttackNotes, apCost, 1 + extraEnergyCostFromMovingFar,
                                        true, true, targetLocation, destinationPoint, false );

                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "Attack_Melee" );
                                        novel.FrameBody.AddLang( "Attack_Melee_StrategyTip" ).Line();
                                    }
                                }
                            }
                            else
                            {
                                bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                                if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.ShouldTooltipBeRed = true;
                                    novel.CanExpand = CanExpandType.Brief;
                                    novel.Icon = IconRefs.MouseMoveMode_RangedAndroidAttack.Icon;
                                    //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Ranged" ).AddRaw( actorUnderCursor.GetDisplayName() );

                                    ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                                    InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                        isTargetUnitOutOfAttackRange, wouldActuallyMove, destinationPoint, secondaryBufferForKeyAttackNotes, apCost, 1 + extraEnergyCostFromMovingFar,
                                        true, true, targetLocation, destinationPoint, false );

                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "Attack_Ranged" );
                                        novel.FrameBody.AddLang( "Attack_Ranged_StrategyTip" ).Line();
                                    }
                                }
                            }
                        }
                        else if ( wouldBeDialog )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                            {
                                novel.ShouldTooltipBeRed = true;
                                novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CannotSpeakWithTarget" ).AddRaw( actorUnderCursor.GetDisplayName() );
                                novel.CanExpand = CanExpandType.Brief;

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddLang( "CannotSpeakWithTarget_StrategyTip" ).Line();
                            }
                        }
                        else
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                            {
                                novel.ShouldTooltipBeRed = true;
                                novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "CannotAssist_Target" ).AddRaw( actorUnderCursor.GetDisplayName() );
                                novel.CanExpand = CanExpandType.Brief;

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddLang( "CannotAssist_Target_StrategyTip" ).Line();
                            }
                        }
                    }
                    else
                    {
                        if ( wouldBeAttack )
                        {
                            if ( wouldBeMeleeAttack )
                            {
                                bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                                if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.CanExpand = CanExpandType.Brief;
                                    novel.Icon = IconRefs.MouseMoveMode_MeleeAttack.Icon;
                                    //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Melee" ).AddRaw( unitUnderCursor.GetDisplayName() );

                                    ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                                    InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                        isTargetUnitOutOfAttackRange, wouldActuallyMove, destinationPoint, secondaryBufferForKeyAttackNotes, apCost, 1 + extraEnergyCostFromMovingFar,
                                        true, true, targetLocation, destinationPoint, false );

                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "Attack_Melee" );
                                        novel.FrameBody.AddLang( "Attack_Melee_StrategyTip" ).Line();
                                    }

                                    if ( isSprinting )
                                    {
                                        string energyHex = canAffordExtraEnergy ? string.Empty : ColorTheme.RedOrange2;

                                        if ( !canAffordExtraEnergy )
                                            novel.ShouldTooltipBeRed = true;

                                        //novel.TitleUpperLeft.RemoveLastCharacterIfNewline();
                                        //novel.TitleUpperLeft.Space2x()
                                        //    .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                                        //    .AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex );

                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            effectiveMain.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex )
                                                .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex );
                                            effectiveMain.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Line();

                                            effectiveMain.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_Sprinting_Details" );
                                        }
                                    }
                                }
                            }
                            else
                            {
                                bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                                if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.CanExpand = CanExpandType.Brief;
                                    novel.Icon = IconRefs.MouseMoveMode_RangedAndroidAttack.Icon;
                                    //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Ranged" ).AddRaw( unitUnderCursor.GetDisplayName() );

                                    ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                                    InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                        isTargetUnitOutOfAttackRange, wouldActuallyMove, destinationPoint, secondaryBufferForKeyAttackNotes, apCost, 1 + extraEnergyCostFromMovingFar,
                                        true, true, targetLocation, destinationPoint, false );

                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "Attack_Ranged" );
                                        novel.FrameBody.AddLang( "Attack_Ranged_StrategyTip" ).Line();
                                    }

                                    //no chance of sprinting to ranged attack
                                }
                            }
                        }
                        else if ( wouldBeDialog )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                novel.Icon = IconRefs.DialogActionIcon.Icon;
                                novel.TitleUpperLeft.AddLang( "DiscussionTopic" );
                                novel.MainHeader.AddRaw( npcUnit?.IsManagedUnit?.DialogToShow?.GetDisplayName() ?? string.Empty );

                                if ( isSprinting || !canAfford )
                                {
                                    string energyHex = canAffordExtraEnergy && canAfford ? string.Empty : ColorTheme.RedOrange2;

                                    if ( !canAffordExtraEnergy || !canAfford )
                                        novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.RemoveLastCharacterIfNewline();
                                    novel.TitleUpperLeft.Space2x()
                                        .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                                        .AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex );

                                    if ( InputCaching.ShouldShowDetailedTooltips || !canAfford )
                                    {
                                        novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex )
                                            .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex );
                                        novel.Main.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Line();

                                        if ( isSprinting && InputCaching.ShouldShowDetailedTooltips )
                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_Sprinting_Details" );
                                    }
                                }
                            }
                        }
                        else
                        {      
                            //this is some sort of nothing case at this point
                            canDoAction = false;

                            //IconRefs.MouseMoveMode_ProvideHelp.Icon
                        }

                    }
                }
                #endregion

                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );

                if ( wouldActuallyMove && hasValidDestinationPoint )
                {
                    if ( stance.IsNewAttackRangeProjected )
                        DrawHelper.RenderRangeCircle( destinationPoint.ReplaceY( groundLevel - 0.08f ), attackRange, ColorRefs.UnitNewLocationAttackRangeBorder.ColorHDR, 1.5f );
                }

                if ( targetLocation != null )
                {
                    //draw the line to the close-helper spot
                    DrawHelper.RenderCatmullLine( center, destinationPoint,
                        hasValidDestinationPoint ? ( isSprinting ? ColorRefs.MachineUnitSprintToActionLine.ColorHDR : ColorRefs.MachineUnitMoveActionLine.ColorHDR ) : Color.red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );

                    //then draw the ghost of the unit at that spot
                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Quaternion.identity,// Unit.GetDrawRotation(), 
                        targetLocation.GetEffectiveWorldLocationForContainedUnit(), destinationPoint );
                    MoveHelper.RenderUnitTypeColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid, false );

                    //then draw the line from that spot to the final spot
                    DrawHelper.RenderCatmullLine( destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), targetUnitLocation,
                        hasValidDestinationPoint ?
                         (wouldBeAttack ? ColorRefs.MachineUnitAttackLine.ColorHDR :
                         ColorRefs.MachineUnitHelpLine.ColorHDR)
                        : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                }
                else
                {
                    //note, sprinting should not apply in this version

                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center.PlusY( Unit.GetHalfHeightForCollisions() ), targetUnitLocation,
                        hasValidDestinationPoint ?
                         (wouldBeAttack ? ColorRefs.MachineUnitAttackLine.ColorHDR :
                         ColorRefs.MachineUnitHelpLine.ColorHDR)
                        : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );

                    if ( hasValidDestinationPoint && wouldBeAttack )
                    {
                        float areaAttackRange = Unit.GetAreaOfAttack();
                        if ( areaAttackRange > 0 )
                        {
                            DrawHelper.RenderCircle( targetUnitLocation.ReplaceY( 0 ), areaAttackRange,
                                ColorRefs.MachineUnitAttackLine.ColorHDR, 1f );
                        }
                    }
                }

                if ( isBeyondEvenSprinting )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, targetUnitLocation );
                else if ( !canDoAction )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, targetUnitLocation );
                else
                {
                    if ( wouldBeAttack )
                    {
                        if ( wouldBeMeleeAttack )
                            CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_MeleeAttack, targetUnitLocation );
                        else
                            CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_RangedAndroidAttack, targetUnitLocation );
                    }
                    else if ( wouldBeDialog )
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.DialogActionIcon, targetUnitLocation );
                    else
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_ProvideHelp, targetUnitLocation );
                }

                #region Handle Clicks
                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                {
                    if ( hasValidDestinationPoint && canDoAction && !isBeyondEvenSprinting && canAfford &&
                        Unit.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current >= 1 + extraEnergyCostFromMovingFar && !Unit.GetIsMovingRightNow() &&
                        SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                    {
                        Vector3 effectiveMovePoint = destinationPoint;
                        if ( wouldBeAttack )
                        {
                            if ( !wouldBeMeleeAttack )
                                effectiveMovePoint = Unit.GetDrawLocation();
                            else
                                HandbookRefs.MeleeLeaps.DuringGame_UnlockIfNeeded( false );
                        }
                        else if ( !wouldMoveInClose )
                            effectiveMovePoint = Unit.GetDrawLocation();

                        ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, effectiveMovePoint, ThreatLineLogic.Normal,
                        delegate
                        {
                            SimCommon.HasGivenAnyAndroidMovementOrders = true;
                            if ( wouldBeAttack )
                            {
                                if ( wouldBeMeleeAttack && targetLocation != null )
                                {
                                    //melee attack
                                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsMoveAndAttack );
                                    Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                    Unit.AlterCurrentActionPoints( -apCost );
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                    if ( extraEnergyCostFromMovingFar > 0 )
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );

                                    MobileActorTypeDuringGameData dgd = Unit.GetTypeDuringGameData();
                                    if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                                    {
                                        foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                                            kv.Key.AlterCurrent_Named( -kv.Value, "Expense_UsedForUnitAttacks", ResourceAddRule.IgnoreUntilTurnChange );
                                    }

                                    Unit.SetMeleeCallbackForAfterMovementEnds( delegate //this will be called-back on the main thread
                                    {
                                        workingMainThreadRandom.ReinitializeWithSeed( Unit.CurrentTurnSeed );
                                        if ( unitUnderCursor != null )
                                        {
                                            Unit.ApplyVisibilityFromAction( ActionVisibility.IsMoveAndAttack );
                                            Unit.GetMeleeAttackSoundAndParticles()?.DuringGame_PlayAtLocation( unitUnderCursor.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                                            AttackHelper.HandlePlayerMeleeAttackLogic( Unit, unitUnderCursor, workingMainThreadRandom,
                                                isTargetUnitOutOfAttackRange );

                                            AbilityType abilityMode = Unit.IsInAbilityTypeTargetingMode;
                                            if ( abilityMode != null && (abilityMode.AttacksAreFearBased || abilityMode.AttacksAreArgumentBased) )
                                            {
                                                ParticleSoundRefs.MoraleDamage.DuringGame_PlaySoundOnlyAtLocation( unitUnderCursor.GetDrawLocation() );
                                            }
                                        }
                                    } );
                                    Unit.SetDesiredContainerLocation( targetLocation );
                                }
                                else if ( !wouldBeMeleeAttack )
                                {
                                    //ranged attack
                                    Unit.AlterCurrentActionPoints( -apCost );
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                    if ( extraEnergyCostFromMovingFar > 0 ) //should never happen, but eh
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );

                                    MobileActorTypeDuringGameData dgd = Unit.GetTypeDuringGameData();
                                    if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                                    {
                                        foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                                            kv.Key.AlterCurrent_Named( -kv.Value, "Expense_UsedForUnitAttacks", ResourceAddRule.IgnoreUntilTurnChange );
                                    }

                                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsAttack );

                                    Unit.FireWeaponsAtTargetPoint( unitUnderCursor.GetEmissionLocation(),
                                        Engine_Universal.PermanentQualityRandom,//this is used only for some visual bits, and we are on the main thread, so all good
                                        delegate //this will be called-back on the main thread
                                        {
                                            workingMainThreadRandom.ReinitializeWithSeed( Unit.CurrentTurnSeed );
                                            if ( unitUnderCursor != null )
                                            {
                                                unitUnderCursor.PlayBulletHitEffectAtPositionForCollisions();
                                                AttackHelper.HandlePlayerRangedAttackLogic( Unit, unitUnderCursor, workingMainThreadRandom,
                                                    isTargetUnitOutOfAttackRange );
                                            }
                                        } );
                                }
                            }
                            else if ( wouldBeDialog ) //dialog!
                            {
                                if ( wouldMoveInClose && targetLocation != null && npcUnit != null )
                                {
                                    NPCDialog dialog = npcUnit.IsManagedUnit?.DialogToShow;
                                    if ( dialog != null && !dialog.DuringGame_HasHandled )
                                    {
                                        //move in
                                        Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                        Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                        Unit.AlterCurrentActionPoints( -apCost );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        if ( extraEnergyCostFromMovingFar > 0 )
                                            ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );
                                        Unit.SetMeleeCallbackForAfterMovementEnds( delegate //this will be called-back on the main thread
                                        {
                                            SimCommon.RewardProvider = NPCDialogChoiceHandler.Start( Unit, npcUnit.IsManagedUnit.DialogToShow, npcUnit, true );
                                            SimCommon.OpenWindowRequest = OpenWindowRequest.Reward;
                                            ParticleSoundRefs.Dialog.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                                        } );
                                        Unit.SetDesiredContainerLocation( targetLocation );
                                    }
                                }
                            }
                            else //not an attack or dialog
                            {
                                if ( wouldMoveInClose && targetLocation != null )
                                {
                                    //yo there is nothing to do here.  This is where repairs used to live
                                }
                                else if ( !wouldMoveInClose )
                                {
                                    //do from here
                                    Unit.AlterCurrentActionPoints( -apCost );
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                    if ( extraEnergyCostFromMovingFar > 0 )
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );
                                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsInoffensive );
                                }
                            }
                        } );
                    }
                    else
                    {
                        if ( !canDoAction )
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            AttackHelper.DoCannotAffordPopupAtMouseIfNeeded( Unit.GetTypeDuringGameData(), Engine_HotM.MouseWorldLocation );
                        }
                    }
                }
                else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                    MouseHelper.BasicLeftClickHandler( false );
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "AndroidMoveAndAttackLogic HandleAttemptToInteractWithHostileOrNeutralTargetUnit Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }

            return true;
        }
        #endregion HandleAttemptToInteractWithHostileOrNeutralTargetUnit

        #region HandleAttemptToAssistTargetStructure
        internal static void HandleAttemptToAssistTargetStructure( ISimMachineUnit Unit, MachineStructure structureUnderCursor, Vector3 center, Vector3 groundCenter,
            float moveRange, float attackRange, 
            float groundLevel )
        {
            if ( structureUnderCursor == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 100;
            try
            {
                Vector3 targetStructureLocation = structureUnderCursor.GetPositionForCollisions();
                Vector3 destinationPoint = targetStructureLocation;
                float sqrDistToTargetStructure = (targetStructureLocation.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;

                MoveHelper.CalculateSprint( sqrDistToTargetStructure, moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar );

                bool isTargetStructureOutOfMoveRange = false;
                bool hasValidDestinationPoint = true;
                if ( isBeyondEvenSprinting || !canAffordExtraEnergy )
                {
                    isTargetStructureOutOfMoveRange = true;
                    hasValidDestinationPoint = false;
                }

                int apCost = 1;
                int energyCost = 1;

                bool wouldMoveInClose = false;
                bool canDoAction = true;
                bool wantsToDoEngineering = false;
                bool canAfford = Unit.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current >= energyCost;

                {
                    canDoAction = false;                    
                    if ( structureUnderCursor.IsUnderConstruction || structureUnderCursor.IsJobStillInstalling )
                    {
                        wantsToDoEngineering = true;
                        wouldMoveInClose = true;
                        canDoAction = true;// canAfford; always say we can do it
                    }
                }

                if ( !wantsToDoEngineering )
                    return;

                Lockdown blockedByLockdown = null;

                ISimUnitLocation targetLocation = null;
                bool wouldActuallyMove = false;

                if ( wouldMoveInClose )
                {
                    //find the nearest spot to this location
                    targetLocation = PathingHelper.FindBestMachineUnitLocationNearCoordinatesFromSearchSpot( Unit, destinationPoint, destinationPoint, 3f, 
                        1 ); //if it's clearance 1, we don't care

                    if ( targetLocation == null ) //if nothing, then expand the search area to 8
                        targetLocation = PathingHelper.FindBestMachineUnitLocationNearCoordinatesFromSearchSpot( Unit, destinationPoint, destinationPoint, 8f,
                            1 ); //if it's clearance 1, we don't care

                    if ( targetLocation == null )
                    {
                        canDoAction = false;
                        hasValidDestinationPoint = false;
                    }
                    else
                    {
                        destinationPoint = targetLocation.GetEffectiveWorldLocationForContainedUnit();
                        wouldActuallyMove = true;
                    }

                    if ( canDoAction )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                            canDoAction = false;
                            wouldActuallyMove = false;
                        }
                    }

                    //against where the android would go
                    if ( !isTargetStructureOutOfMoveRange )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), targetLocation as ISimBuilding,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, isTargetStructureOutOfMoveRange, ThreatLineLogic.Normal );
                }
                else
                {
                    //against the current location of the android
                    if ( !isTargetStructureOutOfMoveRange )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, center.PlusY( Unit.GetHalfHeightForCollisions() ), Unit.ContainerLocation.Get() as ISimBuilding,
                            false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.CurrentLocation_NoOp, false, ThreatLineLogic.Normal );
                }


                int engineeringSkill = Unit.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true );

                #region Tooltip For Attack Or Assist
                if ( blockedByLockdown != null )
                {
                    MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                }
                else if ( isTargetStructureOutOfMoveRange )
                {
                    if ( !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );

                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "Move_OutOfRange_StrategyTip" ).Line();

                        }
                    }
                }
                else
                {
                    if ( !canDoAction ) //it's probably because it's on the other side of a lockdown or something
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.Mouse_Invalid.Icon;
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "CannotAssist_Target" );

                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "CannotAssist_Target_StrategyTip" ).Line();
                        }
                    }
                    else if ( engineeringSkill < 100 )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.Mouse_Invalid.Icon;
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "CannotAssist_Target" );

                            novel.Main.AddFormat2( "CannotAssist_Target_NeedAtLeastXEngineering", 100, engineeringSkill ).Line();
                        }
                    }
                    else
                    {
                        if ( wantsToDoEngineering )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                novel.CanExpand = CanExpandType.Brief;
                                novel.Icon = IconRefs.RepairTarget.Icon;
                                novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "StructuralEngineering_Target" ).AddRaw( structureUnderCursor.GetDisplayName() );

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddLang( "StructuralEngineering_Target_StrategyTip", ColorTheme.NarrativeColor ).Line();

                                novel.Main.StartStyleLineHeightA();

                                string costHeaderColor = canAfford ? ColorTheme.HeaderLighterBlue : ColorTheme.RedOrange2;
                                string costColor = canAfford ? string.Empty : ColorTheme.RedOrange3;

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddRawAndAfterLineItemHeader( ActorRefs.ActorEngineeringSkill.GetDisplayName(), ColorTheme.HeaderLighterBlue )
                                        .AddSpriteStyled_NoIndent( ActorRefs.ActorEngineeringSkill.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                        .AddRaw( engineeringSkill.ToStringThousandsWhole() ).Line();

                                int turnsRemaining = MathA.Max( structureUnderCursor.ConstructionTurnsRemaining, structureUnderCursor.JobInstallationTurnsRemaining );
                                if ( turnsRemaining < 1 )
                                    turnsRemaining = 1;

                                novel.Main.AddLangAndAfterLineItemHeader( "ConstructionTurns", ColorTheme.HeaderLighterBlue )
                                    .AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                    .AddFormat2( "NumberUp", turnsRemaining, turnsRemaining - 1 ).Line();

                                if ( !canAfford )
                                {
                                    string energyHex = canAffordExtraEnergy && canAfford ? string.Empty : ColorTheme.RedOrange2;

                                    if ( !canAffordExtraEnergy || !canAfford )
                                        novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.RemoveLastCharacterIfNewline();
                                    novel.TitleUpperLeft.Space2x()
                                        .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                                        .AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex );

                                    if ( InputCaching.ShouldShowDetailedTooltips || !canAfford )
                                    {
                                        novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex )
                                            .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex );
                                        novel.Main.AddRaw( (extraEnergyCostFromMovingFar + 1).ToStringThousandsWhole(), energyHex ).Line();
                                    }
                                }

                                novel.Main.EndLineHeight();

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                {
                                    CombatTextHelper.AppendLastPredictedDamageLong( Unit, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, false, false );
                                    CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, targetLocation, destinationPoint, true );
                                }
                                else
                                {
                                    CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                    CombatTextHelper.AppendRestrictedAreaShort( Unit, novel.Main, InputCaching.ShouldShowDetailedTooltips, targetLocation, destinationPoint, true );
                                }
                            }
                        }
                        else
                        {
                            //whatever this was is deprecated
                            //MouseMoveMode_ProvideHelp.Icon
                        }
                    }
                }
                #endregion

                if ( engineeringSkill < 100 )
                    canDoAction = false;

                DrawAndroidRangeCircles( groundCenter, attackRange, moveRange );

                if ( wouldActuallyMove )
                {
                    MachineUnitStance stance = Unit.Stance;
                    if ( stance.IsNewAttackRangeProjected )
                        DrawHelper.RenderRangeCircle( destinationPoint.ReplaceY( groundLevel - 0.08f ), attackRange, ColorRefs.UnitNewLocationAttackRangeBorder.ColorHDR, 1.5f );
                }

                if ( targetLocation != null )
                {
                    //draw the line to the close-helper spot
                    DrawHelper.RenderCatmullLine( center, destinationPoint,
                        hasValidDestinationPoint ? ColorRefs.MachineUnitMoveActionLine.ColorHDR : Color.red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );

                    //then draw the ghost of the unit at that spot
                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Quaternion.identity,// Unit.GetDrawRotation(), 
                        targetLocation.GetEffectiveWorldLocationForContainedUnit(), destinationPoint );
                    MoveHelper.RenderUnitTypeColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid, false );

                    //then draw the line from that spot to the final spot
                    DrawHelper.RenderCatmullLine( destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), targetStructureLocation,
                        hasValidDestinationPoint ?
                         ColorRefs.MachineUnitHelpLine.ColorHDR
                        : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                }
                else
                {
                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center.PlusY( Unit.GetHalfHeightForCollisions() ), targetStructureLocation,
                        hasValidDestinationPoint ?
                         ColorRefs.MachineUnitHelpLine.ColorHDR
                        : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                }

                if ( isTargetStructureOutOfMoveRange )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, targetStructureLocation );
                else if ( !canDoAction )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, targetStructureLocation );
                else
                {
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_ProvideHelp, targetStructureLocation );
                }

                #region Handle Clicks
                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                {
                    if ( hasValidDestinationPoint && canDoAction && canAfford && !isTargetStructureOutOfMoveRange && Unit.CurrentActionPoints >= apCost && !Unit.GetIsMovingRightNow() &&
                        SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 &&
                        ResourceRefs.MentalEnergy.Current >= energyCost + extraEnergyCostFromMovingFar )
                    {
                        ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                        delegate
                        {
                            SimCommon.HasGivenAnyAndroidMovementOrders = true;

                            if ( wouldMoveInClose && targetLocation != null )
                            {
                                //move in
                                Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                Unit.AlterCurrentActionPoints( -apCost );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -energyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                if ( extraEnergyCostFromMovingFar > 0 )
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );

                                Unit.SetMeleeCallbackForAfterMovementEnds( delegate //this will be called-back on the main thread
                                {
                                    if ( wantsToDoEngineering )
                                    {
                                        CityStatisticTable.AlterScore( "Uses_StructuralEngineering", 1 );

                                        //do the equivalent of one turn
                                        structureUnderCursor.DoConstructionWork( Engine_Universal.PermanentQualityRandom );

                                        ParticleSoundRefs.Repairs.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                                    }
                                } );
                                Unit.SetDesiredContainerLocation( targetLocation );
                                //clear out the structural engineering selection
                                Unit.SetTargetingMode( null, null );
                            }
                            else if ( !wouldMoveInClose )
                            {
                                //do from here
                                Unit.AlterCurrentActionPoints( -apCost );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -energyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                if ( extraEnergyCostFromMovingFar > 0 )
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );
                                Unit.ApplyVisibilityFromAction( ActionVisibility.IsInoffensive );
                                //not sure what this is, but clear it out
                                Unit.SetTargetingMode( null, null );
                            }
                        } );
                    }
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "AndroidMoveAndAttackLogic HandleAttemptToAssistTargetStructure Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion HandleAttemptToAssistTargetStructure
    }
}

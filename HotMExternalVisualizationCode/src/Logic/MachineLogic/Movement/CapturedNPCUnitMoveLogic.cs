using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public static class CapturedNPCUnitMoveLogic
    {
        private static ArcenDoubleCharacterBuffer secondaryBufferForKeyAttackNotes = new ArcenDoubleCharacterBuffer( "CapturedNPCUnitMoveLogic-secondaryBufferForKeyAttackNotes" );
        internal static readonly List<MapItem> workingBuildings = List<MapItem>.Create_WillNeverBeGCed( 20, "CapturedNPCUnitMoveLogic-workingBuildings" );
        private static MersenneTwister workingMainThreadRandom = new MersenneTwister( 0 );
        private static MersenneTwister postAbilityRand = new MersenneTwister( 0 );

        #region HandleMouseInteractionsAndAnyExtraRendering_NormalStyle
        public static void HandleMouseInteractionsAndAnyExtraRendering_NormalStyle( ISimNPCUnit Unit )
        {
            int excess = SimCommon.TotalCapturedUnitSquadCapacityUsed - MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt;
            if ( excess > 0 )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                if ( Unit.IsFullDead || Unit.IsInvalid )
                    return;

                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Unit.GetActualPositionForMovementOrPlacement();
                Camera cam = Engine_HotM.GameModeData.MainCamera;

                NPCUnitStance stance = Unit.Stance;
                if ( stance == null )
                    return;

                {
                    ISimMachineVehicle vehicleUnderCursor = CursorHelper.FindMachineVehicleUnderCursorIfNotDowned();

                    if ( vehicleUnderCursor != null )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.ShouldTooltipBeRed = true;
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.MouseMoveMode_BoardVehicle.Icon;
                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_BoardVehicle" ).AddRaw( vehicleUnderCursor.GetDisplayName() );

                            novel.Main.AddLang( "Move_BoardVehicle_Captured", ColorTheme.NarrativeColor ).Line();
                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "Move_BoardVehicle_Captured_StrategyTip", ColorTheme.PurpleDim ).Line();

                            CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.Main, TTTextBefore.SpacingAlways, TTTextAfter.None );
                        }

                        //draw the sole version
                        DrawHelper.RenderCatmullLine( center, vehicleUnderCursor.GetDrawLocation(), Color.red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                        return;
                    }
                }

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7300;

                    #region Render Range Circles
                    debugStage = 9100;
                    float moveRange = Unit.GetMovementRange();

                    debugStage = 13100;
                    if ( moveRange <= 0 )
                    {
                        //ArcenDebugging.LogSingleLine( "CapturedNPCUnitMoveLogic no UnitMoveRange!", Verbosity.ShowAsError );
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }

                    debugStage = 19100;
                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 groundCenter = center.ReplaceY( groundLevel );

                    debugStage = 21100;
                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    if ( hasValidDestinationPoint )
                    {
                        if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                            hasValidDestinationPoint = false;
                    }


                    if ( MouseHelper.ActorUnderCursor != null && !(MouseHelper.ActorUnderCursor is MachineStructure) )
                        hasValidDestinationPoint = false;

                    debugStage = 22100;

                    debugStage = 22110;

                    debugStage = 25100;

                    debugStage = 26050;

                    bool isBlocked = false;
                    MapItem isToBuilding = null;
                    SecurityClearance requiredClearance = null;
                    bool isIntersectingRoad = false;
                    string debugText = string.Empty;
                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 26100;
                        if ( !MoveHelper.MachineBulkAndroid_ActualDestinationIsValid( Unit.UnitType, ref destinationPoint, out isBlocked,
                            out isToBuilding, out requiredClearance, out isIntersectingRoad, out debugText ) )
                        {
                            hasValidDestinationPoint = false;
                            //debugText += "adv-block ";
                        }
                    }


                    StreetSenseDataAtBuilding streetSenseDataOrNull = isToBuilding?.SimBuilding?.GetCurrentStreetSenseActionThatShouldShowOnMap();
                    LocationActionType actionToTake = streetSenseDataOrNull?.ActionType;
                    if ( actionToTake != null ) //bulk androids cannot do actions of this sort at all
                    {
                        hasValidDestinationPoint = false;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.ShouldTooltipBeRed = true;
                            novel.TitleUpperLeft.AddLang( "ActionCannotBeDoneByThisKindOfUnit" );

                            actionToTake.Implementation.TryHandleLocationAction( null, isToBuilding?.SimBuilding, destinationPoint, actionToTake, 
                                streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                streetSenseDataOrNull?.OtherOptionalID, ActionLogic.AppendToTooltip, out bool IncludeRestrictedAreaNoticeInTooltip, 0 );
                        }
                    }

                    if ( !hasValidDestinationPoint )
                    {
                        //when a captured unit, and you have a bad target, if you mouse over something else
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

                    //debugText = "isToBuilding: " + (isToBuilding == null ? "[null]" : isToBuilding.SimBuilding.GetDisplayName()) +
                    //    " hasValidDestinationPoint: " + hasValidDestinationPoint + " isBlocked: " + isBlocked + " canMoveIntoRestrictedAreas: " + canMoveIntoRestrictedAreas;

                    Lockdown blockedByLockdown = null;
                    if ( hasValidDestinationPoint )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                        }
                    }

                    debugStage = 32100;

                    int baseEnergyCost = 2;
                    int extraEnergyCostFromMovingFar = 0;
                    float sqrDistToDest;

                    if ( isToBuilding != null )
                    {
                        //it is possible for different points on a building to be in and out of the ranges above, so
                        //we need to calculate our distance to the building in a bit of a different way
                        sqrDistToDest = (isToBuilding.CenterPoint.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;
                    }
                    else //if no building, just test to the point
                        sqrDistToDest = (destinationPoint.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;

                    bool isBeyondEvenSprinting = false;
                    if ( sqrDistToDest > moveRange * moveRange )
                    {
                        bool foundSpot = false;
                        for ( int extraMoveRatio = 1; extraMoveRatio < 10; extraMoveRatio++ ) 
                        {
                            float totalMove = moveRange + (extraMoveRatio * moveRange );
                            if ( sqrDistToDest <= totalMove * totalMove )
                            {
                                extraEnergyCostFromMovingFar = ( extraMoveRatio + extraMoveRatio ); //double!
                                foundSpot = true;
                                break;
                            }
                        }

                        if ( !foundSpot )
                            isBeyondEvenSprinting = true;
                    }

                    bool canAfford = extraEnergyCostFromMovingFar + baseEnergyCost <= ResourceRefs.MentalEnergy.Current;

                    debugStage = 38100;

                    debugStage = 62100;

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
                                if ( existingUnit != null )
                                {
                                    hasValidDestinationPoint = false;
                                    isBlocked = true;
                                    isToBuilding = null;
                                    //debugText += "blocking-unit ";
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
                            }
                        }
                        else //valid spot, but not to a building
                        {
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

                    bool skipThreatLinesFromDestination = isBeyondEvenSprinting || extraEnergyCostFromMovingFar > 0;
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, drawnDestination, isToBuilding?.SimBuilding,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, skipThreatLinesFromDestination, ThreatLineLogic.Normal );
                    else
                        CombatTextHelper.ClearPredictionStats();

                    #region The Tooltip Writing
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
                    }
                    #endregion

                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                        drawnDestination.y = groundLevel;

                    Color moveColor;

                    debugStage = 99100;
                    if ( isBeyondEvenSprinting )
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, drawnDestination );
                        moveColor = Color.red;
                    }
                    else if ( hasValidDestinationPoint )
                    {
                        moveColor = ColorRefs.BulkAndroidMoveColor.ColorHDR;

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
                    }

                    debugStage = 102100;

                    debugStage = 105100;
                    //DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), moveRange, ColorRefs.UnitMoveRangeBorder.ColorHDR, 2f );

                    {
                        float attackRange = Unit.GetAttackRange();
                        DrawHelper.RenderRangeCircle( drawnDestination.ReplaceY( groundLevel - 0.08f ), attackRange, ColorRefs.BulkAndroidNewLocationAttackRangeBorder.ColorHDR, 1.5f );
                    }

                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center, drawnDestination,
                        moveColor, 1f, CatmullSlotType.Move, CatmullSlope.Movement );
                    #endregion

                    //if ( actionToTake != null )
                    //    debugText += " " + actionToTake.ID + " " + actionToTake.IsDoneFromADistance;

                    bool isSprinting = !isBeyondEvenSprinting && extraEnergyCostFromMovingFar > 0 && !isBlocked && hasValidDestinationPoint;
                    bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( Unit );
                    bool hasRestrictedArea = CombatTextHelper.GetIsMovingToRestrictedArea( Unit, isToBuilding?.SimBuilding, destinationPoint, true );

                    if ( !hasValidDestinationPoint )
                    { }
                    else if ( hasCombatLines || hasRestrictedArea )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            if ( isSprinting )
                            {
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                if ( !canAfford )
                                    novel.ShouldTooltipBeRed = true;
                                novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex ).Space2x();
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
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                novel.Main.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex ).Line();

                                novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_SprintingBulkAndroid_Details" );
                            }

                            novel.CanExpand = CanExpandType.Brief;

                            if ( debugText.Length > 0 )
                                novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                        }
                    }
                    else
                    {
                        //if ( isSprinting ) always, because always costs more than 1
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                novel.ShouldTooltipBeRed = !canAfford;
                                novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex );

                                //if we're out of action range, then explain that
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_SprintingBulkAndroid_Details" );

                                novel.CanExpand = CanExpandType.Brief;

                                if ( debugText.Length > 0 )
                                    novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                            }
                        }
                    }

                    debugStage = 112100;
                    #region Handle Clicks
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                    {
                        debugStage = 118200;
                        if ( hasValidDestinationPoint && !Unit.GetIsMovingRightNow() && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && canAfford )
                        {
                            ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                                delegate
                                {
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -baseEnergyCost, "Expense_OtherCapturedUnitReposition", ResourceAddRule.IgnoreUntilTurnChange );
                                    if ( extraEnergyCostFromMovingFar > 0 )
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_OtherCapturedUnitReposition", ResourceAddRule.IgnoreUntilTurnChange );

                                    Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );

                                    if ( isToBuilding?.SimBuilding != null )
                                        Unit.SetDesiredContainerLocation( isToBuilding.SimBuilding );
                                    else
                                        Unit.SetDesiredGroundLocation( destinationPoint );
                                    CityStatisticTable.AlterScore( "OtherCapturedUnitRepositions", 1 );
                                } );
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
                ArcenDebugging.LogDebugStageWithStack( "CapturedNPCUnitMoveLogic HandleMouseInteractionsAndAnyExtraRendering_NormalStyle Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region HandleMouseInteractionsAndAnyExtraRendering_MechStyle
        public static void HandleMouseInteractionsAndAnyExtraRendering_MechStyle( ISimNPCUnit Unit )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                if ( Unit.IsFullDead || Unit.IsInvalid )
                    return;

                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Unit.GetActualPositionForMovementOrPlacement();
                Camera cam = Engine_HotM.GameModeData.MainCamera;

                NPCUnitStance stance = Unit.Stance;
                if ( stance == null )
                    return;

                {
                    ISimMachineVehicle vehicleUnderCursor = CursorHelper.FindMachineVehicleUnderCursorIfNotDowned();

                    if ( vehicleUnderCursor != null )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            novel.ShouldTooltipBeRed = true;
                            novel.CanExpand = CanExpandType.Brief;
                            novel.Icon = IconRefs.MouseMoveMode_BoardVehicle.Icon;
                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_BoardVehicle" ).AddRaw( vehicleUnderCursor.GetDisplayName() );

                            novel.Main.AddLang( "Move_BoardVehicle_Captured", ColorTheme.NarrativeColor ).Line();
                            if ( InputCaching.ShouldShowDetailedTooltips )
                                novel.Main.AddLang( "Move_BoardVehicle_Captured_StrategyTip", ColorTheme.PurpleDim ).Line();

                            CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.Main, TTTextBefore.SpacingAlways, TTTextAfter.None );
                        }

                        //draw the sole version
                        DrawHelper.RenderCatmullLine( center, vehicleUnderCursor.GetDrawLocation(), Color.red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                        return;
                    }
                }

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7300;
                    destinationPoint.y = 0; //always drop mechs to this

                    #region Render Range Circles
                    debugStage = 9100;
                    float moveRange = Unit.GetMovementRange();

                    debugStage = 13100;
                    if ( moveRange <= 0 )
                    {
                        //ArcenDebugging.LogSingleLine( "CapturedNPCUnitMoveLogic no UnitMoveRange!", Verbosity.ShowAsError );
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }

                    debugStage = 19100;
                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 groundCenter = center.ReplaceY( groundLevel );

                    ISimBuilding building = MouseHelper.BuildingUnderCursor;

                    debugStage = 21100;
                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    if ( hasValidDestinationPoint )
                    {
                        if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                            hasValidDestinationPoint = false;
                    }


                    if ( MouseHelper.ActorUnderCursor != null && !(MouseHelper.ActorUnderCursor is MachineStructure) )
                        hasValidDestinationPoint = false;

                    debugStage = 22100;

                    debugStage = 22110;

                    debugStage = 25100;

                    debugStage = 26050;

                    if ( !hasValidDestinationPoint )
                    {
                        //when a bulk android, and you have a bad target, if you mouse over something else
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

                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Unit.GetDrawRotation(), center, destinationPoint );

                    bool isBlocked = false;
                    string debugText = string.Empty;
                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 26100;
                        float theoreticalY = theoreticalRotation.eulerAngles.y;
                        if ( !CityMap.CalculateIsValidLocationForCollidable( Unit, destinationPoint, theoreticalY, true,
                            CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, false, CollisionRule.Relaxed ) )
                        {
                            isBlocked = true;
                            hasValidDestinationPoint = false;
                        }


                        if ( !hasValidDestinationPoint )
                        {
                            workingBuildings.Clear();
                            CityMap.FillListOfBuildingsIntersectingCollidable( Unit, destinationPoint, theoreticalY, workingBuildings, false );
                            if ( workingBuildings.Count > 0 )
                            {
                                Int64 framesPrepped = RenderManager.FramesPrepped;
                                int stompStrength = Unit.UnitType.DestroyIntersectingBuildingsStrength;
                                foreach ( MapItem item in workingBuildings )
                                {
                                    RenderManager_Streets.DrawMapItemHighlightedGhost( item,
                                        item.Type.ExtraPlaceableData.ResistanceToDestructionFromCollisions <= stompStrength ?
                                        ColorRefs.BuildingToBeDestroyedByMech.ColorHDR :
                                        (stompStrength > 0 ? ColorRefs.BuildingBlockingGiantMech.ColorHDR : ColorRefs.BuildingBlockingSmallMech.ColorHDR),
                                        false, framesPrepped );
                                }
                            }
                        }

                        if ( hasValidDestinationPoint )
                        {
                            bool canMoveIntoRestrictedAreas = true;
                            //if ( Unit.Stance.IsForbiddenFromEnteringRestrictedAreasUnlessCloaked )
                            //{
                            //    if ( !Unit.IsCloaked )
                            //        canMoveIntoRestrictedAreas = false;
                            //}

                            if ( !MoveHelper.MachineNPC_ActualDestinationIsValid( Unit, destinationPoint, Unit.GetEffectiveClearance( ClearanceCheckType.MovingToNonBuilding ), canMoveIntoRestrictedAreas, out isBlocked,
                                out SecurityClearance requiredClearance, out debugText ) )
                            {
                                isBlocked = true;
                                hasValidDestinationPoint = false;
                                //debugText += "adv-block ";
                            }
                        }
                    }

                    //debugText = "isToBuilding: " + (isToBuilding == null ? "[null]" : isToBuilding.SimBuilding.GetDisplayName()) +
                    //    " hasValidDestinationPoint: " + hasValidDestinationPoint + " isBlocked: " + isBlocked + " canMoveIntoRestrictedAreas: " + canMoveIntoRestrictedAreas;

                    Lockdown blockedByLockdown = null;
                    if ( hasValidDestinationPoint )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                        }
                    }

                    debugStage = 32100;

                    int baseEnergyCost = 2;
                    int extraEnergyCostFromMovingFar = 0;
                    float sqrDistToDest;

                    sqrDistToDest = (destinationPoint.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;

                    bool isBeyondEvenSprinting = false;
                    if ( sqrDistToDest > moveRange * moveRange )
                    {
                        bool foundSpot = false;
                        for ( int extraMoveRatio = 1; extraMoveRatio < 10; extraMoveRatio++ )
                        {
                            float totalMove = moveRange + (extraMoveRatio * moveRange);
                            if ( sqrDistToDest <= totalMove * totalMove )
                            {
                                extraEnergyCostFromMovingFar = (extraMoveRatio + extraMoveRatio); //double!
                                foundSpot = true;
                                break;
                            }
                        }

                        if ( !foundSpot )
                            isBeyondEvenSprinting = true;
                    }

                    bool canAfford = extraEnergyCostFromMovingFar + baseEnergyCost <= ResourceRefs.MentalEnergy.Current;

                    debugStage = 38100;

                    debugStage = 62100;

                    Vector3 drawnDestination = destinationPoint; //draw to the main spot, not to bumped-up

                    debugStage = 92100;
                    if ( hasValidDestinationPoint )
                    {
                        destinationPoint.y = 0f;
                        if ( drawnDestination.y > destinationPoint.y )
                            drawnDestination.y = destinationPoint.y;
                    }

                    if ( isBeyondEvenSprinting )
                    {
                        hasValidDestinationPoint = false;
                    }

                    bool skipThreatLinesFromDestination = isBeyondEvenSprinting || extraEnergyCostFromMovingFar > 0;
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, drawnDestination, null,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, skipThreatLinesFromDestination, ThreatLineLogic.Normal );
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
                        else if ( !isBeyondEvenSprinting && hasValidDestinationPoint )
                        {
                            debugStage = 96100;
                        }
                    }
                    #endregion

                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                        drawnDestination.y = groundLevel;

                    Color moveColor;

                    debugStage = 99100;
                    if ( isBeyondEvenSprinting )
                    {
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, drawnDestination );
                        moveColor = Color.red;
                    }
                    else if ( hasValidDestinationPoint )
                    {
                        moveColor = ColorRefs.BulkAndroidMoveColor.ColorHDR;
                        MoveHelper.RenderNPCUnitColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid );
                        CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, drawnDestination, moveColor );
                    }
                    else
                    {
                        MoveHelper.RenderNPCUnitColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostBlocked );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, drawnDestination );
                        moveColor = Color.red;
                    }

                    debugStage = 102100;

                    debugStage = 105100;
                    //DrawHelper.RenderRangeCircle( groundCenter.PlusY( -0.08f ), moveRange, ColorRefs.UnitMoveRangeBorder.ColorHDR, 2f );

                    {
                        float attackRange = Unit.GetAttackRange();
                        DrawHelper.RenderRangeCircle( drawnDestination.ReplaceY( groundLevel - 0.08f ), attackRange, ColorRefs.BulkAndroidNewLocationAttackRangeBorder.ColorHDR, 1.5f );
                    }

                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center, drawnDestination,
                        moveColor, 1f, CatmullSlotType.Move, CatmullSlope.Movement );
                    #endregion

                    //if ( actionToTake != null )
                    //    debugText += " " + actionToTake.ID + " " + actionToTake.IsDoneFromADistance;

                    bool isSprinting = !isBeyondEvenSprinting && extraEnergyCostFromMovingFar > 0 && !isBlocked && hasValidDestinationPoint;
                    bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( Unit );
                    bool hasRestrictedArea = CombatTextHelper.GetIsMovingToRestrictedArea( Unit, null, destinationPoint, true );

                    if ( !hasValidDestinationPoint )
                    { }
                    else if ( hasCombatLines || hasRestrictedArea )
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                        {
                            if ( isSprinting )
                            {
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                if ( !canAfford )
                                    novel.ShouldTooltipBeRed = true;
                                novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex ).Space2x();
                            }

                            if ( hasCombatLines && !InputCaching.ShouldShowDetailedTooltips )
                            {
                                CombatTextHelper.AppendLastPredictedDamageBrief( Unit, novel.TitleUpperLeft, TTTextBefore.None, TTTextAfter.None );
                                if ( hasRestrictedArea )
                                    CombatTextHelper.AppendRestrictedAreaShort( Unit, novel.MainHeader, false, null, destinationPoint, true );
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
                                            InputCaching.ShouldShowDetailedTooltips, null, destinationPoint, true );
                                }
                                else
                                {
                                    if ( hasRestrictedArea )
                                    {
                                        CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.TitleUpperLeft, novel.Main,
                                            InputCaching.ShouldShowDetailedTooltips, null, destinationPoint, true );
                                    }
                                }
                            }

                            if ( isSprinting && InputCaching.ShouldShowDetailedTooltips )
                            {
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                novel.Main.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                novel.Main.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex ).Line();

                                novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_SprintingCapturedMech_Details" );
                            }

                            novel.CanExpand = CanExpandType.Brief;

                            if ( debugText.Length > 0 )
                                novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                        }
                    }
                    else
                    {
                        //if ( isSprinting ) always, because there is always an extra cost
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                string energyHex = canAfford ? string.Empty : ColorTheme.RedOrange2;

                                novel.ShouldTooltipBeRed = !canAfford;
                                novel.Icon = ResourceRefs.MentalEnergy.Icon;
                                novel.IconColorHex = ResourceRefs.MentalEnergy.IconColorHex;

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_TotalMentalEnergyCost", ResourceRefs.MentalEnergy.IconColorHex );
                                novel.TitleUpperLeft.AddRaw( (extraEnergyCostFromMovingFar + baseEnergyCost).ToStringThousandsWhole(), energyHex );

                                //if we're out of action range, then explain that
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Move_Sprinting", ColorTheme.DataLabelWhite ).AddLang( "Move_SprintingCapturedMech_Details" );

                                novel.CanExpand = CanExpandType.Brief;

                                if ( debugText.Length > 0 )
                                    novel.Main.AddNeverTranslated( "debugText: " + debugText, true ).Line();
                            }
                        }
                    }

                    debugStage = 112100;
                    #region Handle Clicks
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                    {
                        debugStage = 118200;
                        if ( hasValidDestinationPoint && !Unit.GetIsMovingRightNow() && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && canAfford )
                        {
                            ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                                delegate
                                {
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -baseEnergyCost, "Expense_CapturedMechReposition", ResourceAddRule.IgnoreUntilTurnChange );
                                    if ( extraEnergyCostFromMovingFar > 0 )
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_CapturedMechReposition", ResourceAddRule.IgnoreUntilTurnChange );

                                    Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                    Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );

                                    Unit.SetDesiredGroundLocation( destinationPoint );
                                    CityStatisticTable.AlterScore( "CapturedMechRepositions", 1 );
                                } );
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
                ArcenDebugging.LogDebugStageWithStack( "CapturedNPCUnitMoveLogic HandleMouseInteractionsAndAnyExtraRendering_MechStyle Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}

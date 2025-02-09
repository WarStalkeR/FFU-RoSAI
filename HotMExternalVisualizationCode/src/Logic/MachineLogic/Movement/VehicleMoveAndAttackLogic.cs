using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class VehicleMoveAndAttackLogic
    {
        private static ArcenDoubleCharacterBuffer secondaryBufferForKeyAttackNotes = new ArcenDoubleCharacterBuffer( "VehicleMoveAndAttackLogic-secondaryBufferForKeyAttackNotes" );
        private static MersenneTwister workingMainThreadRandom = new MersenneTwister( 0 );
        private static MersenneTwister postAbilityRand = new MersenneTwister( 0 );

        #region HandleMoveMode_Vehicle
        public static void HandleMoveMode_Vehicle( ISimMachineVehicle Vehicle )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Vehicle.GetActualPositionForMovementOrPlacement();
                Camera cam = Engine_HotM.GameModeData.MainCamera;

                debugStage = 500;

                debugStage = 2200;

                #region Mouseover Other From Vehicle
                {
                    //when in a vehicle, if you mouse over something else
                    ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                    if ( actorMousingOver != null && actorMousingOver != Vehicle )
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

                debugStage = 3200;
                {
                    debugStage = 7200;
                    Ray ray = cam.ScreenPointToRay( Input.mousePosition );
                    float flightPlaneY = Vehicle.VehicleType.InitialHeight;
                    if ( MathA.DoesIntersectPlane( Vector3.up, Vector3.up * flightPlaneY, ray.origin, ray.direction, out float t ) )
                    {
                        destinationPoint = (ray.origin + ray.direction * t);
                    }
                    else
                    {
                        //if the ray-plane intersection fails, this will convert a ground point into an aerial point
                        //which works, but isn't the best
                        destinationPoint.y = flightPlaneY; //keep us at the same altitude
                    }

                    //allow the stuff above for the y offset, but now put the actual destination at the altitude this vehicle must always be at:
                    destinationPoint.y = flightPlaneY;

                    debugStage = 7300;

                    #region Render Range Circles
                    float originalRange = Vehicle.ActorData[ActorRefs.ActorMoveRange].Current;
                    float range = originalRange;
                    if ( range <= 0 )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }

                    debugStage = 8300;

                    float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                    Vector3 groundCenter = center.ReplaceY( groundLevel );
                    ISimBuilding building = MouseHelper.BuildingUnderCursor;

                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    if ( hasValidDestinationPoint )
                    {
                        if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                            hasValidDestinationPoint = false;
                    }

                    debugStage = 9300;

                    //keep within maximum range
                    if ( (destinationPoint - center).sqrMagnitude > range * range )
                    {
                        destinationPoint = ((destinationPoint - center).normalized * range) + center;
                    }

                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Vehicle.WorldRotation, center, destinationPoint );

                    bool isBlocked = false;

                    Lockdown blockedByLockdown = null;
                    if ( hasValidDestinationPoint )
                    {
                        if ( !Vehicle.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;

                            MoveHelper.RenderLockdownWarning( TooltipID.Create( Vehicle ), blockedByLockdown );
                        }
                    }

                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 12300;
                        if ( !CityMap.CalculateIsValidLocationForCollidable( Vehicle, destinationPoint, theoreticalRotation.eulerAngles.y, true,
                            CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, false, CollisionRule.Strict ) )
                        {
                            isBlocked = true;
                            hasValidDestinationPoint = false;
                        }

                        if ( hasValidDestinationPoint )
                        {
                            bool canMoveIntoRestrictedAreas = true;
                            if ( Vehicle.Stance.IsForbiddenFromEnteringRestrictedAreasUnlessCloaked )
                            {
                                if ( !Vehicle.IsCloaked )
                                    canMoveIntoRestrictedAreas = false;
                            }

                            if ( !MoveHelper.MachineOther_ActualDestinationIsValid( Vehicle, destinationPoint, Vehicle.GetEffectiveClearance( ClearanceCheckType.MovingToNonBuilding ), canMoveIntoRestrictedAreas, out isBlocked,
                                out SecurityClearance requiredClearance, out string debugText ) )
                            {
                                isBlocked = true;
                                hasValidDestinationPoint = false;
                                //debugText += "adv-block ";
                            }
                        }
                    }

                    //against where the vehicle would go
                    if ( hasValidDestinationPoint )
                    {
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Vehicle, destinationPoint.PlusY( Vehicle.GetHalfHeightForCollisions() ), null,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );

                        bool hasCombatDamage = CombatTextHelper.NextTurn_DamageFromEnemies.Physical > 0 || CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies > 0;
                        bool hasRestrictedArea = CombatTextHelper.GetIsMovingToRestrictedArea( Vehicle, null, destinationPoint, true );

                        if ( hasCombatDamage || hasRestrictedArea )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                if ( hasCombatDamage && !InputCaching.ShouldShowDetailedTooltips )
                                {
                                    CombatTextHelper.AppendLastPredictedDamageBrief( Vehicle, novel.TitleUpperLeft, TTTextBefore.None, TTTextAfter.None );
                                    if ( hasRestrictedArea )
                                        CombatTextHelper.AppendRestrictedAreaShort( Vehicle, novel.MainHeader, false, null, true );
                                }
                                else
                                {
                                    if ( hasCombatDamage )
                                    {
                                        CombatTextHelper.AppendLastPredictedDamageBrief( Vehicle, novel.TitleUpperLeft, TTTextBefore.None, TTTextAfter.None );

                                        CombatTextHelper.AppendLastPredictedDamageLongSecondary( Vehicle,
                                            InputCaching.ShouldShowDetailedTooltips, false, false, true );

                                        if ( hasRestrictedArea )
                                            CombatTextHelper.AppendRestrictedAreaShort( Vehicle, novel.Main,
                                                InputCaching.ShouldShowDetailedTooltips, null, destinationPoint, true );
                                    }
                                    else
                                    {
                                        if ( hasRestrictedArea )
                                        {
                                            CombatTextHelper.AppendRestrictedAreaLong( Vehicle, novel.TitleUpperLeft, novel.Main,
                                                InputCaching.ShouldShowDetailedTooltips, null, destinationPoint, true );
                                        }
                                    }
                                }

                                novel.CanExpand = CanExpandType.Brief;
                            }
                        }
                    }

                    debugStage = 14300;
                    if ( hasValidDestinationPoint )
                        MoveHelper.RenderVehicleTypeColoredForMoveTarget( Vehicle.VehicleType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid );
                    else
                    {
                        if ( isBlocked )
                            MoveHelper.RenderVehicleTypeColoredForMoveTarget( Vehicle.VehicleType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostBlocked );
                        else
                            CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                    }

                    debugStage = 18300;
                    //draw the high version
                    DrawHelper.RenderRangeCircle( center, range, ColorRefs.VehicleRangeBorder.ColorHDR );
                    DrawHelper.RenderLine( center, destinationPoint,
                        hasValidDestinationPoint ? ColorRefs.VehicleRangeBorder.ColorHDR : Color.red );

                    debugStage = 20300;
                    //draw the ground-level version
                    DrawHelper.RenderRangeCircle( groundCenter, range, ColorRefs.VehicleRangeBorderLow.ColorHDR );
                    DrawHelper.RenderLine( groundCenter, destinationPoint.ReplaceY( groundLevel ),
                        hasValidDestinationPoint ? ColorRefs.VehicleRangeBorderLow.ColorHDR : Color.red );
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForVehicleAndMechMovement )
                    {
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action (move)
                        {
                            if ( hasValidDestinationPoint && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                            {
                                ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Vehicle, destinationPoint, ThreatLineLogic.Normal,
                                    delegate
                                    {
                                        debugStage = 44300;
                                        Vehicle.VehicleType.SFXToPlayOnMoveStart.TryToPlayRandom( center, 1f );
                                        Vehicle.SetDesiredLocation( destinationPoint );
                                        Vehicle.AlterCurrentActionPoints( -1 );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        Vehicle.ApplyVisibilityFromAction( ActionVisibility.IsMovement );

                                        //for vehicles, take them out of movement mode after 1 move unless the player has shift held down
                                        if ( !InputCaching.ShouldKeepDoingAction )
                                            Vehicle.SetTargetingMode( null, null );
                                    } );
                            }
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear ability-action mode
                            Vehicle.SetTargetingMode( null, null );
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action (move)
                        {
                            if ( hasValidDestinationPoint && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                            {
                                ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Vehicle, destinationPoint, ThreatLineLogic.Normal,
                                    delegate
                                    {
                                        debugStage = 44300;
                                        Vehicle.VehicleType.SFXToPlayOnMoveStart.TryToPlayRandom( center, 1f );
                                        Vehicle.SetDesiredLocation( destinationPoint );
                                        Vehicle.AlterCurrentActionPoints( -1 );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        Vehicle.ApplyVisibilityFromAction( ActionVisibility.IsMovement );

                                        //for vehicles, take them out of movement mode after 1 move unless the player has shift held down
                                        if ( !InputCaching.ShouldKeepDoingAction )
                                            Vehicle.SetTargetingMode( null, null );
                                    } );
                            }
                        }
                    }
                    #endregion
                }

                debugStage = 101200;
            }

            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "VehicleMoveAndAttackLogic HandleMoveMode_Vehicle Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region HandleVehicleBasicLogic
        public static void HandleVehicleBasicLogic( ISimMachineVehicle vehicle )
        {
            #region Mouseover Other From Vehicle
            {
                //when in a vehicle, if you mouse over something else
                ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                if ( actorMousingOver != null && actorMousingOver != vehicle )
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

            //if ( !vehicle.GetIsBlockedFromActingInGeneral() )
            {
                if ( HandleAttackPartialLogic_Vehicle( vehicle ) )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                        MouseHelper.BasicLeftClickHandler( false );
                    return;
                }
            }

            if ( TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() )
                return;

            if ( !vehicle.GetIsBlockedFromActingInGeneral() )
            {
                MachineStructure structureUnderCursor = MouseHelper.StructureUnderCursor;

                //if our cursor is over a structure...
                //and if the structure is under construction, digging basement, installing a job, or damaged...
                //then handle that separately.
                if ( structureUnderCursor != null && (structureUnderCursor.IsUnderConstruction || structureUnderCursor.IsJobStillInstalling ) )
                {
                    //this is a big deal!  If a unit is selected and we are hovering over another unit, then we might shoot it or something
                    HandleAttemptToAssistTargetStructure( vehicle, structureUnderCursor );
                }
            }

            if ( TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() )
                return;

            MouseHelper.HandleMouseInteractionsWhenNoMode();

            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                MouseHelper.BasicLeftClickHandler( false );
        }
        #endregion

        #region HandleAttackPartialLogic_Vehicle
        public static bool HandleAttackPartialLogic_Vehicle( ISimMachineVehicle Vehicle )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 1000;
                Vector3 aimPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Vehicle.GetEmissionLocation();

                debugStage = 2000;
                float attackRange = Vehicle.ActorData[ActorRefs.AttackRange].Current;
                if ( attackRange <= 0 )
                {
                    //CursorHelper.RenderSpecificMouseCursor( IconRefs.Mouse_OutOfRange );
                    return false;
                }

                debugStage = 5000;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );
                DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                if ( hasValidDestinationPoint )
                {
                    if ( aimPoint.x == float.NegativeInfinity || aimPoint.x == float.PositiveInfinity )
                        hasValidDestinationPoint = false;
                }

                if ( !hasValidDestinationPoint )
                    return false;

                if ( TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() )
                    return false;

                debugStage = 9000;
                ISimMapActor mapActorUnderCursor = null;
                float attackRangeSquared = attackRange * attackRange;
                if ( (center - aimPoint).GetSquareGroundMagnitude() <= attackRangeSquared ) 
                    mapActorUnderCursor = CursorHelper.FindMapActorUnderCursor();

                debugStage = 12100;
                ISimUnit unitUnderCursor = mapActorUnderCursor as ISimUnit;
                if ( mapActorUnderCursor == null )
                {
                    debugStage = 15100;
                    //no actual attack target

                    ISimBuilding toBuilding = CursorHelper.FindBuildingUnderCursorNoFilters();
                    if ( toBuilding != null )
                    {
                        #region Building Interaction
                        StreetSenseDataAtBuilding streetSenseDataOrNull = toBuilding.GetCurrentStreetSenseActionThatShouldShowOnMap();
                        LocationActionType actionToTake = streetSenseDataOrNull?.ActionType;
                        if ( actionToTake != null ) //if we have a result, we don't care if it's out of scan range!
                        {
                            if ( actionToTake != null  )
                            {
                                string actionToTakeOptionalID = streetSenseDataOrNull?.OtherOptionalID;
                                Vector3 destinationPoint = toBuilding.GetMapItem().CenterPoint;

                                ActionResult actionResult = actionToTake.Implementation.TryHandleLocationAction( Vehicle, toBuilding, destinationPoint, actionToTake, 
                                    streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                    actionToTakeOptionalID, ActionLogic.CalculateIfBlocked, out _, 0 );
                                bool isActionBlocked = actionResult == ActionResult.Blocked;
                                if ( !actionToTake.CanBeDoneByVehicles )
                                    isActionBlocked = true;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
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

                                    actionToTake.Implementation.TryHandleLocationAction( Vehicle, toBuilding, destinationPoint, actionToTake, 
                                        streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                        actionToTakeOptionalID, ActionLogic.AppendToTooltip, out bool IncludeRestrictedAreaNoticeInTooltip, 0 );

                                    if ( !actionToTake.CanBeDoneByVehicles )
                                        novel.Main.AddLang( "ActionCannotBeDoneByAVehicle", ColorTheme.RedOrange2 );
                                    else
                                    {
                                        if ( IncludeRestrictedAreaNoticeInTooltip )
                                            CombatTextHelper.AppendRestrictedAreaLong( Vehicle, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, toBuilding, destinationPoint, true );
                                    }
                                }

                                if ( isActionBlocked )
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f ); //scale it very small
                                else
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_Valid, destinationPoint );

                                //draw the sole version
                                DrawHelper.RenderCatmullLine( center, destinationPoint,
                                    !isActionBlocked ? ColorRefs.MachineActorActionOverDistanceLine.ColorHDR : Color.red, 
                                    !isActionBlocked ? 2f : 1f, CatmullSlotType.Move, CatmullSlope.VehicleTargeting );

                                #region Handle Clicks
                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                                {
                                    if ( hasValidDestinationPoint && actionToTake != null && Vehicle.CurrentActionPoints >= 1 && ResourceRefs.MentalEnergy.Current >= 1 
                                        && !Vehicle.GetIsMovingRightNow() && !isActionBlocked )
                                    {
                                        debugStage = 44300;
                                        if ( actionToTake.IsDoneFromADistance ) //doing this from a distance
                                        {
                                            if ( actionToTake.Implementation.TryHandleLocationAction( Vehicle, toBuilding, destinationPoint,
                                                actionToTake, streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull, actionToTakeOptionalID,
                                                ActionLogic.Execute, out _, 0 ) == ActionResult.Success )
                                            {
                                                actionToTake.OnArrive.DuringGame_PlayAtLocation( toBuilding.GetMapItem().TopCenterPoint, Vehicle.WorldRotation.eulerAngles );
                                                postAbilityRand.ReinitializeWithSeed( Vehicle.CurrentTurnSeed + actionToTake.RowIndexNonSim );
                                                actionToTake.DuringGameplay_ApplyActionStatistics( postAbilityRand );

                                                //make sure that things are newly-random after moving
                                                Vehicle.RerollCurrentTurnSeed();
                                            }
                                        }
                                        Vehicle.AlterCurrentActionPoints( -1 );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                }
                                else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                    MouseHelper.BasicLeftClickHandler( false );
                                #endregion

                                return true;
                            }
                        }
                        #endregion Building Interaction
                    }

                    return false;
                }
                else if ( mapActorUnderCursor.GetIsAnAllyFromThePlayerPerspective() )
                {
                    //something friendly!
                    if ( mapActorUnderCursor is ISimMachineActor machineActor )
                    {
                        //if ( machineActor.IsDowned && !machineActor.IsReconstructing )
                        {
                            //start reconstruction on a downed unit
                            //targetLocation = PathingHelper.FindBestUnitLocationNearCoordinatesFromSearchSpot( Unit, destinationPoint, destinationPoint, false, false );
                            //if ( targetLocation == null )
                            //    canDoAction = false;
                            //else
                            //{
                            //    destinationPoint = targetLocation.GetEffectiveWorldLocationForContainedUnit();
                            //    wouldActuallyMove = true;
                            //}

                            ////against where the android would go
                            //if ( !isTargetUnitOutOfMoveRange )
                            //    MoveHelper.DrawThreatLinesAgainstMachineActor( Unit, destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), targetLocation as ISimBuilding,
                            //        true, out CombatTextHelper.LastPredictedEnemySquadsTargeting,
                            //        out CombatTextHelper.LastPredictedEnemiesTargeting, out CombatTextHelper.LastPredictedDamageFromEnemies,
                            //        unitUnderCursor, predictedAttackDamage, EnemyTargetingReason.ProposedDestination );
                        }
                    }
                }
                else if ( unitUnderCursor == null )
                    return false;
                else if ( (unitUnderCursor as ISimNPCUnit)?.IsManagedUnit?.IsBlockedFromAnyKilling ?? false )
                {
                    debugStage = 19100;
                    //this is an npc blocked from being killed for now
                    aimPoint = unitUnderCursor.GetDrawLocation().PlusY( unitUnderCursor.GetHalfHeightForCollisions() );
                    DrawHelper.RenderCatmullLine( center, aimPoint, ColorRefs.TargetingLineInvalid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.VehicleTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, aimPoint );

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                    {
                        novel.ShouldTooltipBeRed = true;
                        novel.TitleUpperLeft.AddLang( "BlockedFromAttacking_Header" );
                        novel.Main.AddLang( (unitUnderCursor as ISimNPCUnit).IsManagedUnit.LangKeyForKillingBlock );
                    }
                }
                else if ( !Vehicle.GetIsValidToAutomaticallyShootAt_Current( unitUnderCursor ) )
                {
                    debugStage = 18100;
                    //this is a friend of some sort, or otherwise invalid
                    aimPoint = unitUnderCursor.GetDrawLocation().PlusY( unitUnderCursor.GetHalfHeightForCollisions() );
                    if ( (center - aimPoint).GetSquareGroundMagnitude() > attackRangeSquared )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return false; //out of range
                    }
                    DrawHelper.RenderCatmullLine( center, aimPoint, ColorRefs.TargetingLineInvalid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.VehicleTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, aimPoint );
                }
                else
                {
                    debugStage = 22100;

                    secondaryBufferForKeyAttackNotes.EnsureResetForNextUpdate();
                    AttackAmounts predictedAttackDamage = AttackHelper.HandlePlayerAttackPrediction( Vehicle, unitUnderCursor,
                        false, CalculationType.PredictionDuringPlayerTurn, 0, false, Vector3.zero, secondaryBufferForKeyAttackNotes );

                    int apCost = Vehicle.VehicleType.APCostPerAttack;
                    bool canAfford = Vehicle.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current > 0; //Do we have that much?

                    if ( canAfford )
                    {
                        MobileActorTypeDuringGameData dgd = Vehicle.GetTypeDuringGameData();
                        if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                        {
                            foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                            {
                                if ( kv.Value > kv.Key.Current )
                                {
                                    canAfford = false;
                                    break;
                                }
                            }
                        }
                    }

                    debugStage = 22300;

                    //possible attack target
                    aimPoint = unitUnderCursor.GetDrawLocation().PlusY( unitUnderCursor.GetHalfHeightForCollisions() );
                    if ( (center - aimPoint).GetSquareGroundMagnitude() > attackRangeSquared )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );

                        bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                        if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Vehicle ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                            novel.ShouldTooltipBeRed = true;
                            //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_OutOfRange" ).AddRaw( unitUnderCursor.GetDisplayName() );

                            ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                            InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Vehicle, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttackDamage,
                                false, false, Vector3.zero, secondaryBufferForKeyAttackNotes, apCost, 1, false, false, null, Vector3.zero, true );
                        }

                        return false; //out of range
                    }
                    DrawHelper.RenderCatmullLine( center, aimPoint, ColorRefs.TargetingLineValid.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.VehicleTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_RangedVehicleAttack, aimPoint );

                    {
                        float areaAttackRange = Vehicle.GetAreaOfAttack();
                        if ( areaAttackRange > 0 )
                        {
                            DrawHelper.RenderCircle( aimPoint.ReplaceY( 0 ), areaAttackRange,
                                ColorRefs.MachineUnitAttackLine.ColorHDR, 1f );
                        }
                    }

                    debugStage = 26100;

                    debugStage = 29100;
                    int predictedTargetSquadMembersLost = 0;
                    if ( unitUnderCursor is ISimNPCUnit targetNPC )
                    {
                        debugStage = 32100;
                        predictedTargetSquadMembersLost = targetNPC.CalculateSquadSizeLossFromDamage( predictedAttackDamage.Physical );
                    }

                    debugStage = 33100;
                    MoveHelper.DrawThreatLinesAgainstMapMobileActor( Vehicle, center.PlusY( Vehicle.GetHalfHeightForCollisions() ), null,
                        false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                        out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                        out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                        unitUnderCursor, predictedAttackDamage, EnemyTargetingReason.CurrentLocation_Attacking, false, ThreatLineLogic.Normal );

                    debugStage = 36100;

                    {
                        bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                        if ( novel.TryStartSmallerOrCombatTooltip( !InputCaching.ShouldShowDetailedTooltips, TooltipID.Create( Vehicle ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.ShouldTooltipBeRed = !canAfford;
                            novel.Icon = IconRefs.MouseMoveMode_RangedVehicleAttack.Icon;
                            //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Ranged" ).AddRaw( unitUnderCursor.GetDisplayName() );

                            ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                            InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Vehicle, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttackDamage,
                                false, false, Vector3.zero, secondaryBufferForKeyAttackNotes, apCost, 1, true, true, null, aimPoint, false );

                            if ( InputCaching.ShouldShowDetailedTooltips )
                            {
                                novel.FrameTitle.AddLangAndAfterLineItemHeader( "Attack_Ranged" );
                                novel.FrameBody.AddLang( "Attack_Ranged_StrategyTip", ColorTheme.PurpleDim ).Line();
                            }
                        }
                    }

                    debugStage = 42100;

                    #region Handle Clicks

                    debugStage = 44100;
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //action
                    {
                        if ( hasValidDestinationPoint && canAfford && Vehicle.CurrentActionPoints >= apCost && !Vehicle.GetIsMovingRightNow() &&
                            SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                        {
                            //ranged attack
                            Vehicle.AlterCurrentActionPoints( -apCost );
                            ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );

                            MobileActorTypeDuringGameData dgd = Vehicle.GetTypeDuringGameData();
                            if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                            {
                                foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                                    kv.Key.AlterCurrent_Named( -kv.Value, "Expense_UsedForUnitAttacks", ResourceAddRule.IgnoreUntilTurnChange );
                            }

                            Vehicle.FireWeaponsAtTargetPoint( unitUnderCursor.GetEmissionLocation(),
                                Engine_Universal.PermanentQualityRandom,//this is used only for some visual bits, and we are on the main thread, so all good
                                delegate //this will be called-back on the main thread
                                {
                                    workingMainThreadRandom.ReinitializeWithSeed( Vehicle.CurrentTurnSeed );
                                    if ( unitUnderCursor != null )
                                    {
                                        unitUnderCursor.PlayBulletHitEffectAtPositionForCollisions();
                                        AttackHelper.HandlePlayerRangedAttackLogic( Vehicle, unitUnderCursor, workingMainThreadRandom, false );
                                        Vehicle.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                                    }
                                } );
                        }
                        else
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            AttackHelper.DoCannotAffordPopupAtMouseIfNeeded( Vehicle.GetTypeDuringGameData(), Engine_HotM.MouseWorldLocation );
                        }
                    }
                    else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( false );
                }
                #endregion

            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "VehicleMoveAndAttackLogic HandleAttackPartialLogic_Vehicle Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return true;
        }
        #endregion

        #region HandleAttemptToAssistTargetStructure
        internal static void HandleAttemptToAssistTargetStructure( ISimMachineVehicle Vehicle, MachineStructure structureUnderCursor )
        {
            if ( structureUnderCursor == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 100;
            try
            {
                Vector3 aimPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Vehicle.GetEmissionLocation();

                debugStage = 2000;
                float attackRange = Vehicle.ActorData[ActorRefs.AttackRange].Current;
                if ( attackRange <= 0 )
                {
                    //CursorHelper.RenderSpecificMouseCursor( IconRefs.Mouse_OutOfRange );
                    return;
                }

                debugStage = 5000;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );
                //DrawHelper.RenderRangeCircle( groundCenter, range, ColorRefs.UnitAttackRangeBorder.ColorHDR );

                DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.UnitAttackRangeBorder.ColorHDR );

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

                int apCost = 1;
                int energyCost = 1;

                bool canDoAction = hasValidDestinationPoint;
                bool wantsToDoEngineering = false;
                bool canAfford = Vehicle.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current >= energyCost;

                if ( structureUnderCursor.IsUnderConstruction || structureUnderCursor.IsJobStillInstalling )
                {
                    wantsToDoEngineering = true;
                    if ( hasValidDestinationPoint )
                        canDoAction = canAfford;
                }

                if ( !wantsToDoEngineering )
                    return;
                
                {
                    //against the current location of the vehicle
                    if ( hasValidDestinationPoint )
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Vehicle, center.PlusY( Vehicle.GetHalfHeightForCollisions() ), null,
                            false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.CurrentLocation_NoOp, false, ThreatLineLogic.Normal );
                }

                int engineeringSkill = Vehicle.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true );

                #region Tooltip For Attack Or Assist
                if ( !hasValidDestinationPoint )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
                         InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                    {
                        novel.CanExpand = CanExpandType.Brief;
                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                        novel.ShouldTooltipBeRed = true;
                        novel.TitleUpperLeft.AddLang( "VehicleAssist_OutOfRange" );

                        if ( InputCaching.ShouldShowDetailedTooltips )
                            novel.Main.AddLang( "VehicleAssist_OutOfRange_StrategyTip" ).Line();

                    }
                }
                else
                {
                    if ( !canDoAction ) //it's probably because it's on the other side of a lockdown or something
                    {
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
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
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
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
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Vehicle ), null, SideClamp.Any,
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

                                novel.Main.EndLineHeight();

                                if ( InputCaching.ShouldShowDetailedTooltips )
                                {
                                    CombatTextHelper.AppendLastPredictedDamageLong( Vehicle, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, false, false );
                                    CombatTextHelper.AppendRestrictedAreaLong( Vehicle, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, null, center, true );
                                }
                                else
                                {
                                    CombatTextHelper.AppendLastPredictedDamageBrief( Vehicle, novel.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                    CombatTextHelper.AppendRestrictedAreaShort( Vehicle, novel.Main, InputCaching.ShouldShowDetailedTooltips, null, center, true );
                                }
                            }
                        }
                        else
                        {
                            //old legacy stuff
                            //IconRefs.MouseMoveMode_ProvideHelp.Icon
                        }

                    }
                }
                #endregion

                if ( engineeringSkill < 100 )
                    canDoAction = false;

                {
                    //draw the sole version
                    DrawHelper.RenderCatmullLine( center.PlusY( Vehicle.GetHalfHeightForCollisions() ), aimPoint,
                        hasValidDestinationPoint ?
                         ColorRefs.MachineUnitHelpLine.ColorHDR
                        : Color.red, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                }

                if ( !hasValidDestinationPoint )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, aimPoint );
                else if ( !canDoAction )
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, aimPoint );
                else
                {
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_ProvideHelp, aimPoint );
                }

                #region Handle Clicks
                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                {
                    if ( hasValidDestinationPoint && canDoAction && Vehicle.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current >= energyCost && !Vehicle.GetIsMovingRightNow() &&
                        SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                    {
                        //do from here
                        Vehicle.AlterCurrentActionPoints( -apCost );
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -energyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                        Vehicle.ApplyVisibilityFromAction( ActionVisibility.IsInoffensive );
                        //not sure what this is, but clear it out
                        Vehicle.SetTargetingMode( null, null );

                        if ( wantsToDoEngineering )
                        {
                            CityStatisticTable.AlterScore( "Uses_StructuralEngineering", 1 );

                            //do the equivalent of one turn
                            structureUnderCursor.DoConstructionWork( Engine_Universal.PermanentQualityRandom );

                            ParticleSoundRefs.Repairs.DuringGame_PlayAtLocation( Vehicle.GetDrawLocation(), Vehicle.WorldRotation.eulerAngles );
                        }
                    }
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "VehicleMoveAndAttackLogic HandleAttemptToAssistTargetStructure Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion HandleAttemptToAssistTargetStructure
    }
}

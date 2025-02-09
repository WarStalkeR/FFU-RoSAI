using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public static class MechMoveAndAttackLogic
    {
        private static readonly ArcenDoubleCharacterBuffer passingMessageBuffer = new ArcenDoubleCharacterBuffer( "MechMoveAndAttackLogic-passingMessageBuffer" );
        private static ArcenDoubleCharacterBuffer secondaryBufferForKeyAttackNotes = new ArcenDoubleCharacterBuffer( "MechMoveAndAttackLogic-secondaryBufferForKeyAttackNotes" );
        internal static readonly List<MapItem> workingBuildings = List<MapItem>.Create_WillNeverBeGCed( 20, "MechMoveAndAttackLogic-workingBuildings" );
        private static MersenneTwister workingMainThreadRandom = new MersenneTwister( 0 );
        private static MersenneTwister postAbilityRand = new MersenneTwister( 0 );

        #region HandleMoveMode_Mech
        public static void HandleMoveMode_Mech( ISimMachineUnit Unit )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Unit.GetActualPositionForMovementOrPlacement();
                Camera cam = Engine_HotM.GameModeData.MainCamera;

                debugStage = 500;

                float originalRange = Unit.GetMovementRange();
                float moveRange = originalRange;

                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );

                #region Mouseover Other From Vehicle
                {
                    //when in a vehicle, if you mouse over something else
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

                        //even we just missed the tooltip situation above, that's all
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                }
                #endregion

                debugStage = 2200;

                #region If Unit Is In A Vehicle And Not Allowed To Disembark
                if ( !Unit.GetIsDeployed() && !Unit.GetUnitCanBeRemovedFromVehicleNow( null ) )
                {
                    CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_Invalid );

                    bool isClicked = false;
                    if ( InputCaching.UseLeftClickForVehicleAndMechMovement )
                        isClicked = ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action
                    else
                        isClicked = ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume();

                    if ( isClicked )
                    {
                        passingMessageBuffer.Clear();
                        if ( !Unit.GetUnitCanBeRemovedFromVehicleNow( passingMessageBuffer ) )
                        {
                            if ( !passingMessageBuffer.GetIsEmpty() )
                                ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddRaw_New( passingMessageBuffer.GetStringAndResetForNextUpdate() ),
                                    NoteStyle.ShowInPassing, 5f );
                        }
                    }
                    return;
                }
                #endregion

                debugStage = 3200;

                if ( TryHandleGetInVehicleLogic( Unit ) )
                    return;

                {
                    debugStage = 7200;
                    destinationPoint.y = 0; //always drop mechs to this

                    debugStage = 7300;

                    #region Render Range Circles
                    if ( moveRange <= 0 )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return;
                    }

                    debugStage = 8300;

                    ISimBuilding building = MouseHelper.BuildingUnderCursor;

                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    if ( hasValidDestinationPoint )
                    {
                        if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                            hasValidDestinationPoint = false;
                    }

                    debugStage = 9300;

                    //keep within maximum range
                    if ( (destinationPoint - center).sqrMagnitude > moveRange * moveRange )
                    {
                        destinationPoint = ((destinationPoint - center).normalized * moveRange) + center;
                    }

                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Unit.GetDrawRotation(), center, destinationPoint );

                    bool isBlocked = false;

                    Lockdown blockedByLockdown = null;
                    if ( hasValidDestinationPoint )
                    {
                        if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                        {
                            hasValidDestinationPoint = false;
                            MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                        }
                    }

                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 12300;
                        float theoreticalY = theoreticalRotation.eulerAngles.y;
                        if ( !CityMap.CalculateIsValidLocationForCollidable( Unit, destinationPoint, theoreticalY, true,
                             CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, false, CollisionRule.Relaxed ) ) //don't collide with buildings if we would destroy them!
                        {
                            isBlocked = true;
                            hasValidDestinationPoint = false;
                        }

                        if ( Unit.UnitType.DestroyIntersectingBuildingsStrength > 0 || !hasValidDestinationPoint )
                        {
                            workingBuildings.Clear();
                            CityMap.FillListOfBuildingsIntersectingCollidable( Unit, destinationPoint, theoreticalY, workingBuildings, false );
                            if ( workingBuildings.Count > 0 )
                            {
                                int stompStrength = Unit.UnitType.DestroyIntersectingBuildingsStrength;
                                Int64 framesPrepped = RenderManager.FramesPrepped;
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
                            if ( Unit.Stance.IsForbiddenFromEnteringRestrictedAreasUnlessCloaked )
                            {
                                if ( !Unit.IsCloaked )
                                    canMoveIntoRestrictedAreas = false;
                            }

                            if ( !MoveHelper.MachineOther_ActualDestinationIsValid( Unit, destinationPoint, Unit.GetEffectiveClearance( ClearanceCheckType.MovingToNonBuilding ), canMoveIntoRestrictedAreas, out isBlocked,
                                out SecurityClearance requiredClearance, out string debugText ) )
                            {
                                isBlocked = true;
                                hasValidDestinationPoint = false;
                                //debugText += "adv-block ";
                            }
                        }
                    }

                    //against where the unit would go
                    if ( hasValidDestinationPoint )
                    {
                        //against where the mech would go
                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, destinationPoint.PlusY( Unit.GetHalfHeightForCollisions() ), null,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );


                        if ( CombatTextHelper.NextTurn_DamageFromEnemies.Physical > 0 || CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies > 0 ||
                            CombatTextHelper.GetIsMovingToRestrictedArea( Unit, null, destinationPoint, true ) )
                        {
                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                            {
                                novel.Icon = IconRefs.MouseMoveMode_Valid.Icon;

                                if ( CombatTextHelper.NextTurn_DamageFromEnemies.Physical > 0 || CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies > 0 )
                                    CombatTextHelper.AppendLastPredictedDamageLong( Unit, novel.MainHeader, novel.Main, InputCaching.ShouldShowDetailedTooltips, false, false );

                                CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.GetMainHeaderContinuation(), novel.Main, InputCaching.ShouldShowDetailedTooltips, null, destinationPoint, true );
                            }
                        }
                    }

                    debugStage = 14300;
                    if ( hasValidDestinationPoint )
                        MoveHelper.RenderUnitTypeColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid, false );
                    else
                    {
                        if ( isBlocked )
                            MoveHelper.RenderUnitTypeColoredForMoveTarget( Unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostBlocked, false );
                        else
                            CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                    }

                    debugStage = 18300;
                    //draw the high version
                    DrawHelper.RenderRangeCircle( center, moveRange, ColorRefs.VehicleRangeBorder.ColorHDR );
                    DrawHelper.RenderLine( center, destinationPoint,
                        hasValidDestinationPoint ? ColorRefs.VehicleRangeBorder.ColorHDR : Color.red );

                    debugStage = 20300;
                    //draw the ground-level version
                    DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.VehicleRangeBorderLow.ColorHDR );
                    DrawHelper.RenderLine( groundCenter, destinationPoint.ReplaceY( groundLevel ),
                        hasValidDestinationPoint ? ColorRefs.VehicleRangeBorderLow.ColorHDR : Color.red );
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForVehicleAndMechMovement )
                    {
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                        {
                            if ( hasValidDestinationPoint && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                            {
                                ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                                    delegate
                                    {
                                        Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                        Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                        Unit.AlterCurrentActionPoints( -1 );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        debugStage = 44300;
                                        //Unit.SetActionToTakeAfterMovementEnds( actionToTake, isToBuilding?.SimBuilding, isOutOfScanRange );
                                        Unit.SetDesiredGroundLocation( destinationPoint );
                                        CityStatisticTable.AlterScore( "MechMovements", 1 );

                                        //for mechs, take them out of movement mode after 1 move unless the player has shift held down
                                        if ( !InputCaching.ShouldKeepDoingAction )
                                            Unit.SetTargetingMode( null, null );
                                    } );
                            }
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear ability-action mode
                            Unit.SetTargetingMode( null, null );
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                        {
                            if ( hasValidDestinationPoint && SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                            {
                                ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.Normal,
                                    delegate
                                    {
                                        Unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                        Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                        Unit.AlterCurrentActionPoints( -1 );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        debugStage = 44300;
                                        //Unit.SetActionToTakeAfterMovementEnds( actionToTake, isToBuilding?.SimBuilding, isOutOfScanRange );
                                        Unit.SetDesiredGroundLocation( destinationPoint );
                                        CityStatisticTable.AlterScore( "MechMovements", 1 );

                                        //for mechs, take them out of movement mode after 1 move unless the player has shift held down
                                        if ( !InputCaching.ShouldKeepDoingAction )
                                            Unit.SetTargetingMode( null, null );
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
                ArcenDebugging.LogDebugStageWithStack( "MechMoveAndAttackLogic HandleMoveMode_Mech Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public static bool TryHandleGetInVehicleLogic( ISimMachineUnit Unit )
        {
            ISimMachineVehicle vehicleUnderCursor = null;
            #region Find vehicleUnderCursor
            if ( Engine_HotM.MarkableUnderMouse is IAutoPooledFloatingColliderIcon icon )
            {
                if ( icon != null )
                    vehicleUnderCursor = icon.GetCurrentRelated() as ISimMachineVehicle;
            }
            else if ( Engine_HotM.MarkableUnderMouse is IAutoPooledFloatingObject fObj )
            {
                if ( fObj != null )
                    vehicleUnderCursor = fObj.GetCurrentRelated() as ISimMachineVehicle;
            }
            else if ( Engine_HotM.MarkableUnderMouse is IAutoPooledFloatingLODObject fLODObj )
            {
                if ( fLODObj != null )
                    vehicleUnderCursor = fLODObj.GetCurrentRelated() as ISimMachineVehicle;
            }
            #endregion

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            #region If Mousing Over A Vehicle
            if ( vehicleUnderCursor != null )
            {
                //this is a big deal!  If a unit is selected and we are hovering over a vehicle, then we might get this unit into this vehicle
                bool hasValidDestinationPoint = true;

                #region Handle Unit Attempt To Get In Vehicle
                Vector3 center = Unit.GetActualPositionForMovementOrPlacement();
                float originalRange = Unit.GetMovementRange();
                float moveRange = originalRange;

                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );

                MachineUnitStorageSlotType storageSlotType = Unit.UnitType.StorageSlotType;
                Vector3 vehicleLocation = vehicleUnderCursor.WorldLocation;
                Vector3 destinationPoint = vehicleLocation;
                float sqrDistToVehicle = (vehicleLocation.ReplaceY( 0 ) - center.ReplaceY( 0 )).sqrMagnitude;
                bool vehicleHasRoomForUnit = true;

                MoveHelper.CalculateSprint( sqrDistToVehicle, moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar );
                if ( isBeyondEvenSprinting || !canAffordExtraEnergy )
                    hasValidDestinationPoint = false;

                Lockdown blockedByLockdown = null;
                if ( hasValidDestinationPoint )
                {
                    if ( !Unit.IsCloaked && MoveHelper.MachineAny_DestinationBlockedByLockdown( center, destinationPoint, out blockedByLockdown ) )
                    {
                        hasValidDestinationPoint = false;
                        MoveHelper.RenderLockdownWarning( TooltipID.Create( Unit ), blockedByLockdown );
                    }
                }

                int vehicleTotalSpotsForThisUnitType = vehicleUnderCursor.VehicleType.UnitStorageSlotCounts[storageSlotType];

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

                //draw the high version
                DrawHelper.RenderRangeCircle( center, moveRange, ColorRefs.VehicleRangeBorder.ColorHDR );

                //draw the ground-level version
                DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.VehicleRangeBorderLow.ColorHDR );

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
                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                {
                    if ( hasValidDestinationPoint && vehicleHasRoomForUnit && !isBeyondEvenSprinting && 
                        ResourceRefs.MentalEnergy.Current >= 1 + extraEnergyCostFromMovingFar && Unit.CurrentActionPoints >= 1 )
                    {
                        ThreatLineData.HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( Unit, destinationPoint, ThreatLineLogic.ForceConsiderMovingOutOfRange,
                            delegate
                            {
                                LocationActionType boardVehicleAction = CommonRefs.BoardVehicleAction;

                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                Unit.ApplyVisibilityFromAction( boardVehicleAction.VisibilityStyle );
                                Unit.SetActionToTakeAfterMovementEnds( boardVehicleAction, null, null, null, string.Empty );
                                Unit.SetDesiredContainerLocation( vehicleUnderCursor );

                                Unit.AlterCurrentActionPoints( -1 );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                if ( extraEnergyCostFromMovingFar > 0 )
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -extraEnergyCostFromMovingFar, "Expense_UnitSprinting", ResourceAddRule.IgnoreUntilTurnChange );

                                //for mechs, take them out of movement mode when they get in a vehicle
                                Unit.SetTargetingMode( null, null );
                            } );
                    }
                }
                else
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                        MouseHelper.BasicLeftClickHandler( false );
                }
                #endregion

                #endregion

                return true;
            }
            #endregion If Mousing Over A Vehicle

            return false;
        }

        #region HandleMechBasicLogic
        public static void HandleMechBasicLogic( ISimMachineUnit unit )
        {
            if ( TryHandleGetInVehicleLogic( unit ) )
                return;

            #region Mouseover Other From Mech
            {
                //when in a mech, if you mouse over something else
                ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                if ( actorMousingOver != null && actorMousingOver != unit )
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

            //if ( !unit.GetIsBlockedFromActingInGeneral() )
            {
                if ( HandleAttackPartialLogic_Mech( unit ) )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                        MouseHelper.BasicLeftClickHandler( false );
                    return;
                }
            }

            if ( TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() )
                return;

            MouseHelper.HandleMouseInteractionsWhenNoMode();

            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select, if leftover mouse click
                MouseHelper.BasicLeftClickHandler( false );
        }
        #endregion

        #region HandleAttackPartialLogic_Mech
        public static bool HandleAttackPartialLogic_Mech( ISimMachineUnit Unit)
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 1000;
                Vector3 aimPoint = Engine_HotM.MouseWorldHitLocation;
                Vector3 center = Unit.GetEmissionLocation();

                debugStage = 2000;
                float attackRange = Unit.ActorData[ActorRefs.AttackRange].Current;
                if ( attackRange <= 0 )
                    return false;

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
                            if ( actionToTake != null )
                            {
                                string actionToTakeOptionalID = streetSenseDataOrNull?.OtherOptionalID;
                                Vector3 destinationPoint = toBuilding.GetMapItem().CenterPoint;

                                ActionResult actionResult = actionToTake.Implementation.TryHandleLocationAction( Unit, toBuilding, destinationPoint, actionToTake,
                                    streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                    actionToTakeOptionalID, ActionLogic.CalculateIfBlocked, out _, 0 );
                                bool isActionBlocked = actionResult == ActionResult.Blocked;
                                if ( !actionToTake.CanBeDoneByMechs )
                                    isActionBlocked = true;

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

                                    actionToTake.Implementation.TryHandleLocationAction( Unit, toBuilding, destinationPoint, actionToTake, 
                                        streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                        actionToTakeOptionalID, ActionLogic.AppendToTooltip, out bool IncludeRestrictedAreaNoticeInTooltip, 0 );

                                    if ( !actionToTake.CanBeDoneByMechs )
                                        novel.Main.AddLang( "ActionCannotBeDoneByAMech", ColorTheme.RedOrange2 );
                                    else
                                    {
                                        if ( IncludeRestrictedAreaNoticeInTooltip )
                                            CombatTextHelper.AppendRestrictedAreaLong( Unit, novel.Main, novel.Main, InputCaching.ShouldShowDetailedTooltips, toBuilding, destinationPoint, true );
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
                                    if ( hasValidDestinationPoint && actionToTake != null && Unit.CurrentActionPoints >= 1 && ResourceRefs.MentalEnergy.Current >= 1 && !Unit.GetIsMovingRightNow() )
                                    {
                                        debugStage = 44300;
                                        if ( actionToTake.IsDoneFromADistance ) //doing this from a distance
                                        {
                                            if ( actionToTake.Implementation.TryHandleLocationAction( Unit, toBuilding, destinationPoint, 
                                                actionToTake, streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull, actionToTakeOptionalID,
                                                ActionLogic.Execute, out _, 0 ) == ActionResult.Success )
                                            {
                                                actionToTake.OnArrive.DuringGame_PlayAtLocation( toBuilding.GetMapItem().TopCenterPoint, Unit.GetDrawRotation().eulerAngles );
                                                postAbilityRand.ReinitializeWithSeed( Unit.CurrentTurnSeed + actionToTake.RowIndexNonSim );
                                                actionToTake.DuringGameplay_ApplyActionStatistics( postAbilityRand );

                                                //make sure that things are newly-random after moving
                                                Unit.RerollCurrentTurnSeed();
                                            }
                                        }
                                        Unit.AlterCurrentActionPoints( -1 );
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
                else if ( mapActorUnderCursor.GetIsPartOfPlayerForcesInAnyWay() )
                {
                    //something friendly!
                    if ( mapActorUnderCursor is ISimMachineActor machineActor )
                    {
                        //if ( machineActor.IsDowned && !machineActor.IsReconstructing )
                        {
                            //start reconstruction on a downed unit
                            //ISimUnitLocation targetLocation = PathingHelper.FindBestUnitLocationNearCoordinatesFromSearchSpot( Unit, machineActor.GetDrawLocation(), machineActor.GetDrawLocation(), false, false );
                            //bool canAfford = machineActor.MicrobuildersCostToStartReconstruction <= ResourceRefs.Microbuilders.GetCurrent(); //if we can afford it
                            //bool canDoAction = canAfford;
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
                    return false; //nothing to do, apparently
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

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                    {
                        novel.ShouldTooltipBeRed = true;
                        novel.TitleUpperLeft.AddLang( "BlockedFromAttacking_Header" );
                        novel.Main.AddLang( (unitUnderCursor as ISimNPCUnit).IsManagedUnit.LangKeyForKillingBlock );
                    }
                }
                else if ( !Unit.GetIsValidToAutomaticallyShootAt_Current( unitUnderCursor ) )
                {
                    debugStage = 18100;
                    //this is a friend of some sort, or otherwise invalid
                    aimPoint = unitUnderCursor.GetDrawLocation().PlusY( unitUnderCursor.GetHalfHeightForCollisions() );
                    if ( (center - aimPoint).GetSquareGroundMagnitude() > attackRangeSquared )
                    {
                        CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                        return false; //out of range
                    }
                    DrawHelper.RenderCatmullLine( center, aimPoint, ColorRefs.TargetingLineInvalid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.MechTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, aimPoint );
                }
                else
                {
                    debugStage = 22100;

                    secondaryBufferForKeyAttackNotes.EnsureResetForNextUpdate();
                    AttackAmounts predictedAttack = AttackHelper.HandlePlayerAttackPrediction( Unit, unitUnderCursor,
                        false, CalculationType.PredictionDuringPlayerTurn, 0, false, Vector3.zero, secondaryBufferForKeyAttackNotes );

                    int apCost = Unit.UnitType.APCostPerAttack;
                    bool canAfford = Unit.CurrentActionPoints >= apCost && ResourceRefs.MentalEnergy.Current > 0; //Do we have that much?

                    if ( canAfford )
                    {
                        MobileActorTypeDuringGameData dgd = Unit.GetTypeDuringGameData();
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
                        if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                            novel.ShouldTooltipBeRed = true;
                            //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Move_OutOfRange" ).AddRaw( unitUnderCursor.GetDisplayName() );

                            ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                            InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                false, false, Vector3.zero, secondaryBufferForKeyAttackNotes, apCost, 1,
                                false, false, null, Vector3.zero, true );
                        }

                        return false; //out of range
                    }
                    DrawHelper.RenderCatmullLine( center, aimPoint, ColorRefs.TargetingLineValid.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.MechTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.MouseMoveMode_RangedVehicleAttack, aimPoint );

                    {
                        float areaAttackRange = Unit.GetAreaOfAttack();
                        if ( areaAttackRange > 0 )
                        {
                            DrawHelper.RenderCircle( aimPoint.ReplaceY( 0 ), areaAttackRange,
                                ColorRefs.MachineActorActionOverDistanceLine.ColorHDR, 1f );
                        }
                    }

                    debugStage = 26100;

                    debugStage = 29100;
                    int predictedTargetSquadMembersLost = 0;
                    if ( unitUnderCursor is ISimNPCUnit targetNPC )
                    {
                        debugStage = 32100;
                        predictedTargetSquadMembersLost = targetNPC.CalculateSquadSizeLossFromDamage( predictedAttack.Physical );
                    }

                    debugStage = 33100;
                    MoveHelper.DrawThreatLinesAgainstMapMobileActor( Unit, center.PlusY( Unit.GetHalfHeightForCollisions() ), null,
                        false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                        out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                        out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                        unitUnderCursor, predictedAttack, EnemyTargetingReason.CurrentLocation_Attacking, false, ThreatLineLogic.Normal );

                    debugStage = 36100;

                    debugStage = 39100;

                    {
                        bool isAbbreviated = !InputCaching.ShouldShowDetailedTooltips;
                        if ( novel.TryStartSmallerOrCombatTooltip( isAbbreviated, TooltipID.Create( Unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.CanExpand = CanExpandType.Brief;
                            novel.ShouldTooltipBeRed = !canAfford;
                            novel.Icon = IconRefs.MouseMoveMode_RangedVehicleAttack.Icon;
                            //novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "Attack_Ranged" ).AddRaw( unitUnderCursor.GetDisplayName() );

                            ArcenDoubleCharacterBuffer effectiveMain = isAbbreviated ? novel.MainHeader : novel.Main;

                            InteractionTextCache.AppendPredictedDamageFromPlayerAttack( isAbbreviated, Unit, novel.TitleUpperLeft, effectiveMain, unitUnderCursor as ISimNPCUnit, predictedAttack,
                                false, false, Vector3.zero, secondaryBufferForKeyAttackNotes, apCost, 1,
                                true, true, null, aimPoint, false );

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
                        if ( hasValidDestinationPoint && canAfford && Unit.CurrentActionPoints >= apCost && !Unit.GetIsMovingRightNow() &&
                            SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 )
                        {
                            //ranged attack
                            Unit.AlterCurrentActionPoints( -apCost );
                            ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );


                            MobileActorTypeDuringGameData dgd = Unit.GetTypeDuringGameData();
                            if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
                            {
                                foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                                    kv.Key.AlterCurrent_Named( -kv.Value, "Expense_UsedForUnitAttacks", ResourceAddRule.IgnoreUntilTurnChange );
                            }

                            Unit.FireWeaponsAtTargetPoint( unitUnderCursor.GetEmissionLocation(),
                                Engine_Universal.PermanentQualityRandom,//this is used only for some visual bits, and we are on the main thread, so all good
                                delegate //this will be called-back on the main thread
                                {
                                    workingMainThreadRandom.ReinitializeWithSeed( Unit.CurrentTurnSeed );
                                    if ( unitUnderCursor != null )
                                    {
                                        unitUnderCursor.PlayBulletHitEffectAtPositionForCollisions();
                                        AttackHelper.HandlePlayerRangedAttackLogic( Unit, unitUnderCursor, workingMainThreadRandom, false );
                                        Unit.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                                    }
                                } );
                        }
                        else
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            AttackHelper.DoCannotAffordPopupAtMouseIfNeeded( Unit.GetTypeDuringGameData(), Engine_HotM.MouseWorldLocation );
                        }
                    }
                    else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( false );
                    #endregion
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "MechMoveAndAttackLogic HandleAttackPartialLogic_Mech Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return true;
        }
        #endregion
    }
}

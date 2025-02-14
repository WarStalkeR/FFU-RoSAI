using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class CommandModeHandler : IMachineActionModeImplementation
    {
        public static MachineCommandModeCategory currentCategory = null;

        public static MachineCommandModeAction LastHoveredAction = null;
        public static NPCUnitType LastHoveredDeployableNPC = null;
        public static MachineUnitType LastHoveredDeployableMachineUnitType = null;
        public static MachineVehicleType LastHoveredDeployableMachineVehicleType = null;
        public static float HoverExpireTime = 0;

        private static Vector3 lastWorldHitLocation = Vector3.zero;

        public void FlagAllRelatedResources( MachineActionMode Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "CommandMode" )
                return;

            if ( currentCategory != null )
            {
                currentCategory?.NonSim_TargetingDeployableMachineUnitType?.FlagAllRelatedResources();
                currentCategory?.NonSim_TargetingDeployableMachineVehicleType?.FlagAllRelatedResources();
                currentCategory?.NonSim_TargetingDeployableNPCType?.FlagAllRelatedResources();
            }

            //LastHoveredAction nothing to do?

            LastHoveredDeployableNPC?.FlagAllRelatedResources();
            LastHoveredDeployableMachineUnitType?.FlagAllRelatedResources();
            LastHoveredDeployableMachineVehicleType?.FlagAllRelatedResources();
        }

        public bool GetShouldCloseOnUnitSelection( MachineActionMode Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "CommandMode" )
                return true;

            if ( Engine_HotM.CurrentCommandModeActionTargeting != null )
                return Engine_HotM.CurrentCommandModeActionTargeting.GetShouldCloseOnUnitSelection();

            return true;
        }

        #region HandleCancelButton
        public bool HandleCancelButton()
        {
            //if ( currentCategory != null )
            //{
            //    if ( currentCategory.NonSim_TargetingDeployableMachineUnitType != null ||
            //        currentCategory.NonSim_TargetingDeployableMachineVehicleType != null ||
            //        currentCategory.NonSim_TargetingDeployableNPCType != null )
            //    {
            //        //if deploying, go all the way out right away
            //        Engine_HotM.CurrentCommandModeActionTargeting = null;
            //        return true;
            //    }
            //}

            //if ( currentCategory != null )
            //{
            //    if ( currentCategory.NonSim_TargetingDeployableMachineUnitType != null )
            //    {
            //        currentCategory.NonSim_TargetingDeployableMachineUnitType = null;
            //        return true;
            //    }
            //    else if ( currentCategory.NonSim_TargetingDeployableMachineVehicleType != null )
            //    {
            //        currentCategory.NonSim_TargetingDeployableMachineVehicleType = null;
            //        return true;
            //    }
            //    else if ( currentCategory.NonSim_TargetingDeployableNPCType != null )
            //    {
            //        currentCategory.NonSim_TargetingDeployableNPCType = null;
            //        return true;
            //    }
            //    else 
            //    {
            //        currentCategory = null;
            //        return true;
            //    }
            //}

            Engine_HotM.SelectedMachineActionMode = null;
            return true;
        }
        #endregion

        public bool HandleActionModeMouseInteractionsAndAnyExtraRendering( MachineActionMode Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "CommandMode" )
                return false;

            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null )
            {
                Engine_HotM.SelectedMachineActionMode = null;
                return false;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    Engine_HotM.SelectedMachineActionMode = null;
                    return false;
            }

            bool isMouseBlocked = Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || 
                Engine_Universal.IsMouseOutsideGameWindow || InputCaching.IsMousePositionIgnored;


            Vector3 destinationPoint = Engine_HotM.MouseWorldHitLocation;
            if ( destinationPoint.x != float.NegativeInfinity )
                lastWorldHitLocation = destinationPoint;

            if ( Engine_HotM.CurrentCommandModeActionTargeting != null )
            {
                //if ( isMouseBlocked )
                //    return false;

                bool result = Engine_HotM.CurrentCommandModeActionTargeting.HandleWorldMousePerFrameWhileActive();

                if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( true );
                    return false;
                }

                return result;
            }

            #region Hovered Only
            if ( ArcenTime.AnyTimeSinceStartF < HoverExpireTime )
            {
                if ( !Engine_Universal.IsMouseOverGUI ) //if mouse no longer over the gui, then expire it
                    HoverExpireTime = 0;
            }

            if ( ArcenTime.AnyTimeSinceStartF < HoverExpireTime )
            {
                MachineCommandModeAction action = LastHoveredAction;
                if ( action != null && action.DuringGame_IsVisible() )
                    return this.HandleAction( action, true );
                else
                {
                    NPCUnitType npc = LastHoveredDeployableNPC;
                    if ( npc != null && npc.DuringGame_IsUnlockedForPlayer() )
                        return this.HandleDeployNPC( npc, true, isMouseBlocked );

                    MachineUnitType unit = LastHoveredDeployableMachineUnitType;
                    if ( unit != null && unit.DuringGame_IsUnlocked() )
                        return this.HandleDeployMachineUnitType( unit, true, isMouseBlocked );

                    MachineVehicleType vehicle = LastHoveredDeployableMachineVehicleType;
                    if ( vehicle != null && vehicle.DuringGame_IsUnlocked() )
                        return this.HandleDeployMachineVehicleType( vehicle, true, isMouseBlocked );
                }
            }
            #endregion

            if ( !isMouseBlocked )
            {
                if ( InputCaching.UseLeftClickForUnitDeployment )
                {
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear ability-action mode, only in left click being the default, though
                    {
                        if ( currentCategory != null )
                        {
                            currentCategory.NonSim_TargetingDeployableMachineUnitType = null;
                            currentCategory.NonSim_TargetingDeployableMachineVehicleType = null;
                            currentCategory.NonSim_TargetingDeployableNPCType = null;
                            return true;
                        }
                    }
                }
            }

            if ( currentCategory != null )
            {
                MachineCommandModeAction action = Engine_HotM.CurrentCommandModeActionTargeting;
                if ( action != null && action.DuringGame_IsVisible() )
                {
                    if ( isMouseBlocked )
                        return false;

                    bool result = this.HandleAction( action, false );
                    if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                    {
                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( true );
                        return false;
                    }
                    return result;
                }
                else
                {
                    NPCUnitType npc = currentCategory.NonSim_TargetingDeployableNPCType;
                    if ( npc != null && npc.DuringGame_IsUnlockedForPlayer() )
                    {
                        bool result = this.HandleDeployNPC( npc, false, isMouseBlocked );

                        if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                        {
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( true );
                            return false;
                        }
                        return true;// result;
                    }

                    MachineUnitType unit = currentCategory.NonSim_TargetingDeployableMachineUnitType;
                    if ( unit != null && unit.DuringGame_IsUnlocked() )
                    {
                        bool result = this.HandleDeployMachineUnitType( unit, false, isMouseBlocked );

                        if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                        {
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( true );
                            return false;
                        }
                        return true;//return result;
                    }

                    MachineVehicleType vehicle = currentCategory.NonSim_TargetingDeployableMachineVehicleType;
                    if ( vehicle != null && vehicle.DuringGame_IsUnlocked() )
                    {
                        bool result = this.HandleDeployMachineVehicleType( vehicle, false, isMouseBlocked );

                        if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                        {
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                MouseHelper.BasicLeftClickHandler( true );
                            return false;
                        }
                        return true;//return result;
                    }
                }
            }
            else
            {
                if ( isMouseBlocked )
                    return false;
                if ( MouseHelper.ActorUnderCursor is ISimMachineActor || MouseHelper.ActorUnderCursor is ISimNPCUnit )
                {
                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        MouseHelper.BasicLeftClickHandler( true );
                    return false;
                }
            }
            
            return false;
        }

        #region FindBestAndroidDeployer
        public static void FindBestAndroidDeployer( Vector3 destinationPoint, ref ISimMapActor bestDeployer, ref Vector3 bestDeployerSpot, bool AlsoRenderDeployerLines )
        {
            float largestRange = MathRefs.AndroidMaxDeploymentDistanceFromLauncher.FloatMin;
            float largestRangeSquared = largestRange * largestRange;

            float bestDeployerDistance = 9999999999;
            foreach ( ISimMachineActor deployer in SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.GetDisplayList() )
            {
                Vector3 deployerLocation = deployer.GetCollisionCenter();
                float deployerAttackRange = deployer.GetAttackRange();

                float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                if ( sqrDistToDeployer <= largestRangeSquared )
                {
                    if ( bestDeployer == null )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                    else if ( sqrDistToDeployer < bestDeployerDistance )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                }

                if ( AlsoRenderDeployerLines )
                    DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }

            foreach ( MachineStructure deployer in JobRefs.AndroidLauncher.DuringGame_FullList.GetDisplayList() )
            {
                if ( deployer == null || deployer.IsInvalid || !deployer.IsFunctionalJob || !deployer.IsFunctionalStructure )
                    continue;

                Vector3 deployerLocation = deployer.GetCollisionCenter();

                float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                if ( sqrDistToDeployer <= largestRangeSquared )
                {
                    if ( bestDeployer == null )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                    else if ( sqrDistToDeployer < bestDeployerDistance )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                }

                if ( AlsoRenderDeployerLines )
                    DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }

            {
                MachineStructure deployer = SimCommon.TheNetwork?.Tower;
                if ( deployer != null && !deployer.IsInvalid )
                {
                    Vector3 deployerLocation = deployer.GetCollisionCenter();

                    float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                    if ( sqrDistToDeployer <= largestRangeSquared )
                    {
                        if ( bestDeployer == null )
                        {
                            bestDeployer = deployer;
                            bestDeployerDistance = sqrDistToDeployer;
                            bestDeployerSpot = deployerLocation;
                        }
                        else if ( sqrDistToDeployer < bestDeployerDistance )
                        {
                            bestDeployer = deployer;
                            bestDeployerDistance = sqrDistToDeployer;
                            bestDeployerSpot = deployerLocation;
                        }
                    }

                    if ( AlsoRenderDeployerLines )
                        DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                            ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
                }
            }
        }
        #endregion

        #region FindBestMechDeployer
        public static void FindBestMechDeployer( Vector3 destinationPoint, ref ISimMapActor bestDeployer, ref Vector3 bestDeployerSpot, bool AlsoRenderDeployerLines )
        {
            float largestRange = MathRefs.MechMaxDeploymentDistanceFromHangar.FloatMin;
            float largestRangeSquared = largestRange * largestRange;

            List<ISimMachineActor> list = SimCommon.MachineActorsDeployedAndActingAsMechLauncher.GetDisplayList();

            float bestDeployerDistance = 9999999999;
            foreach ( ISimMachineActor deployer in list )
            {
                Vector3 deployerLocation = deployer.GetCollisionCenter();
                float deployerAttackRange = deployer.GetAttackRange();

                float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                if ( sqrDistToDeployer <= largestRangeSquared )
                {
                    if ( bestDeployer == null )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                    else if ( sqrDistToDeployer < bestDeployerDistance )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                }

                if ( AlsoRenderDeployerLines )
                    DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }

            foreach ( MachineStructure deployer in JobRefs.AerospaceHangar.DuringGame_FullList.GetDisplayList() )
            {
                if ( deployer == null || deployer.IsInvalid || !deployer.IsFunctionalJob || !deployer.IsFunctionalStructure )
                    continue;

                Vector3 deployerLocation = deployer.GetCollisionCenter();

                float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                if ( sqrDistToDeployer <= largestRangeSquared )
                {
                    if ( bestDeployer == null )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                    else if ( sqrDistToDeployer < bestDeployerDistance )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }

                    if ( AlsoRenderDeployerLines )
                        DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                            ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
                }
            }
        }
        #endregion

        #region FindBestVehicleDeployer
        public static void FindBestVehicleDeployer( Vector3 destinationPoint, ref ISimMapActor bestDeployer, ref Vector3 bestDeployerSpot, bool AlsoRenderDeployerLines )
        {
            float largestRange = MathRefs.VehicleMaxDeploymentDistanceFromHangar.FloatMin;
            float largestRangeSquared = largestRange * largestRange;

            float bestDeployerDistance = 9999999999;
            foreach ( MachineStructure deployer in JobRefs.AerospaceHangar.DuringGame_FullList.GetDisplayList() )
            {
                if ( deployer == null || deployer.IsInvalid || !deployer.IsFunctionalJob || !deployer.IsFunctionalStructure )
                    continue;

                Vector3 deployerLocation = deployer.GetCollisionCenter();

                float sqrDistToDeployer = (deployerLocation - destinationPoint).GetSquareGroundMagnitude();
                if ( sqrDistToDeployer <= largestRangeSquared )
                {
                    if ( bestDeployer == null )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                    else if ( sqrDistToDeployer < bestDeployerDistance )
                    {
                        bestDeployer = deployer;
                        bestDeployerDistance = sqrDistToDeployer;
                        bestDeployerSpot = deployerLocation;
                    }
                }

                if ( AlsoRenderDeployerLines )
                    DrawHelper.RenderCircle( deployerLocation.ReplaceY( ThreatLineData.BaselineHeight ), largestRange,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }
        }
        #endregion

        #region HandleDeployNPC
        private bool HandleDeployNPC( NPCUnitType npcType, bool IsHoverOnly, bool isMouseBlocked )
        {
            if ( IsHoverOnly || npcType == null )
                return false; //nothing to for this


            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = lastWorldHitLocation;

                if ( destinationPoint.x == float.NegativeInfinity )
                    return false;

                bool isBlocked = false;
                MapItem isToBuilding = null;
                SecurityClearance requiredClearance = null;
                bool isIntersectingRoad = false;
                string debugText = string.Empty;
                bool hasValidDestinationPoint = true;

                if ( !npcType.IsMechStyleMovement && !npcType.IsVehicle )
                {
                    if ( !MoveHelper.MachineBulkAndroid_ActualDestinationIsValid( npcType, ref destinationPoint, out isBlocked,
                        out isToBuilding, out requiredClearance, out isIntersectingRoad, out debugText ) )
                    {
                        hasValidDestinationPoint = false;
                        //debugText += "adv-block ";
                    }


                    if ( isToBuilding != null )
                    {
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
                            destinationPoint = isToBuilding.SimBuilding.GetEffectiveWorldLocationForContainedUnit();
                        }
                    }

                    if ( hasValidDestinationPoint && isToBuilding == null )
                        destinationPoint = destinationPoint.ReplaceY( isIntersectingRoad ? 1.3f : 0.4f );
                }
                else
                {
                    //for vehicles or mechs.  May never actually do that, though.
                }

                Vector3 bestDeployerSpot = SimCommon.TheNetwork?.Tower?.GetBottomCenterPosition() ?? Vector3.zero;
                ISimMapActor bestDeployer = null;
                FindBestAndroidDeployer( destinationPoint, ref bestDeployer, ref bestDeployerSpot, true );
                if ( isMouseBlocked )
                    return true;

                bool isBlockedFromBuilding = false;

                if ( bestDeployer == null )
                {
                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( npcType ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                    {
                        atMouse.Icon = IconRefs.Mouse_OutOfRange.Icon;
                        atMouse.ShouldTooltipBeRed = true;
                        atMouse.TitleUpperLeft.AddLang( "LocationIsTooFarFromAnyAndroidLauncher_Short" );
                    }

                    isBlockedFromBuilding = true;
                }

                {
                    Lockdown blockedByLockdown = null;
                    if ( MoveHelper.MachineAny_DestinationBlockedByLockdown( destinationPoint, bestDeployerSpot, out blockedByLockdown ) )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                            ColorMath.Red, 1f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint );

                        MoveHelper.RenderLockdownWarning( TooltipID.Create( npcType ), blockedByLockdown );

                        return true;
                    }
                }

                bool canAfford = true;
                if ( npcType.CalculateMaxCouldCreateAsBulkAndroid() <= 0 )
                {
                    if ( !isBlockedFromBuilding )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                            ColorMath.Red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_CannotAfford, destinationPoint, 0.5f );
                    }
                    canAfford = false;
                    isBlockedFromBuilding = true;
                }

                {
                    if ( !npcType.IsMechStyleMovement && !npcType.IsVehicle )
                    {
                        float rotationY = currentDeployedAndroidRotationY;

                        if ( hasValidDestinationPoint )
                        {
                            ProposedNPCPlayerAndroidLocation proposedLoc = new ProposedNPCPlayerAndroidLocation( npcType, destinationPoint, rotationY );

                            if ( !CityMap.CalculateIsValidLocationForCollidable( proposedLoc, destinationPoint, rotationY, false, 
                                CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, 
                                npcType.IsMechStyleMovement ? CollisionRule.Relaxed : CollisionRule.Strict ) )
                                hasValidDestinationPoint = false;
                        }

                        Vector3 drawnDestinationCenter = destinationPoint.PlusY( npcType.GetHalfHeightForCollisions() );

                        CombatTextHelper.ClearPredictionStats();

                        if ( hasValidDestinationPoint && canAfford )
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                isBlockedFromBuilding ? ColorRefs.DeploymentLineBlocked.ColorHDR : ColorRefs.DeploymentLineValid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            SharedRenderManagerData.RenderDeploymentHexes( drawnDestinationCenter, !isBlockedFromBuilding );

                            MoveHelper.RenderNPCUnitColoredForMoveTarget( npcType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                isBlockedFromBuilding ? ColorRefs.VehicleMoveGhostBlocked : ColorRefs.VehicleMoveGhostValid );

                            TheoreticalDeployedNPCUnit theoreticalNPC;
                            theoreticalNPC.UnitType = npcType;
                            theoreticalNPC.Cohort = CohortRefs.YourTroops;
                            theoreticalNPC.Stance = CommonRefs.Player_DeterAndDefend;
                            theoreticalNPC.Location = destinationPoint;

                            MoveHelper.DrawThreatLinesAgainstMapMobileActor( theoreticalNPC, destinationPoint, isToBuilding?.SimBuilding,
                                true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting,
                                out CombatTextHelper.NextTurn_DamageFromEnemies,
                                out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                                null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );

                            bool isToRestrictedArea = CombatTextHelper.GetIsDGDBeingDeployedToRestrictedArea( npcType.DuringGameData, isToBuilding?.SimBuilding, destinationPoint, true );
                            bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( theoreticalNPC );

                            if ( SimCommon.TotalBulkUnitSquadCapacityUsed + npcType.BulkUnitCapacityRequired > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt )
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( npcType ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.Icon = npcType.ShapeIcon;
                                    atMouse.CanExpand = CanExpandType.Brief;
                                    atMouse.TitleUpperLeft.AddLang( "WillTakeYouAboveCap_H1", ColorTheme.RedOrange2 );
                                    atMouse.MainHeader.AddLang( "WillTakeYouAboveCap_H2" );
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        atMouse.Main.AddLang( "WillTakeYouAboveCap_BulkAndroids_Details" ).Line();

                                    if ( isToRestrictedArea )
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( npcType.DuringGameData, atMouse.Main,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                    if ( hasCombatLines )
                                    {
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalNPC, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalNPC, true, false, false, true );
                                    }
                                }
                            }
                            else
                            {
                                if ( isToRestrictedArea )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( npcType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( npcType.DuringGameData, atMouse.TitleUpperLeft,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                        if ( hasCombatLines )
                                        {
                                            atMouse.CanExpand = CanExpandType.Brief;
                                            CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalNPC, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalNPC, true, false, false, true );
                                        }
                                    }
                                }
                                else if ( hasCombatLines )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( npcType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        atMouse.CanExpand = CanExpandType.Brief;
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalNPC, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalNPC, true, false, false, true );
                                    }
                                }
                            }
                        }
                        else
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                ColorRefs.VehicleMoveGhostBlocked.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            MoveHelper.RenderNPCUnitColoredForMoveTarget( npcType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                ColorRefs.VehicleMoveGhostBlocked );
                        }

                        {
                            LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                            if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( npcType ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Smaller : TooltipNovelWidth.SizeToText ) )
                            {
                                lowerLeft.Icon = npcType.ShapeIcon;
                                lowerLeft.TitleUpperLeft.AddRaw( npcType.GetDisplayName() );
                                lowerLeft.CanExpand = CanExpandType.Brief;

                                ArcenDoubleCharacterBuffer mainText = InputCaching.ShouldShowDetailedTooltips ? lowerLeft.Main : lowerLeft.MainHeader;
                                npcType.AppendDeploymentText( mainText );

                                if ( !debugText.IsEmpty() )
                                    mainText.Line().AddNeverTranslated( "debugText: ", true ).AddRaw( debugText );
                            }
                        }

                        #region Handle Clicks
                        if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && hasValidDestinationPoint && !isBlockedFromBuilding && canAfford )
                        {
                            bool isClicked = false;
                            if ( InputCaching.UseLeftClickForUnitDeployment )
                                isClicked = ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action
                            else
                                isClicked = ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action

                            if ( isClicked )
                            {
                                //no need to do any attacks of opportunity, as the unit is getting out of a craft!

                                ISimNPCUnit newBulk = World.Forces.CreateNewNPCUnitAtExactSpotOrBuildingForPlayer( npcType, CohortRefs.YourTroops, CommonRefs.Player_DeterAndDefend, 1f, rotationY,
                                    isToBuilding, destinationPoint, Engine_Universal.PermanentQualityRandom, "PlayerDeployment" );
                                if ( newBulk != null )
                                {
                                    if ( npcType.DeployBulkAndroidNow_TheInventoryPartOnly() )
                                    {
                                        npcType.OnAppearAsNewUnit.DuringGame_PlayAtLocation( destinationPoint, new Vector3( 0, rotationY, 0 ) );
                                        CityStatisticTable.AlterScore( "BulkAndroidsCreatedViaCommandMode", 1 );

                                        if ( !InputCaching.ShouldKeepDoingAction && InputCaching.CloseCommandModeAfterSingleDeployment )
                                        {
                                            Engine_HotM.SelectedMachineActionMode = null;
                                        }
                                    }
                                    else
                                        newBulk.DisbandNPCUnit( NPCDisbandReason.Cheat );
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        //for vehicles or mechs.  May never actually do that, though.
                    }
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "CommandModeHandler.HandleDeployNPC Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }
        #endregion

        #region HandleDeployMachineUnitType
        private bool HandleDeployMachineUnitType( MachineUnitType unitType, bool IsHoverOnly, bool isMouseBlocked )
        {
            if ( IsHoverOnly || unitType == null )
                return false; //nothing to for this

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = lastWorldHitLocation;

                if ( destinationPoint.x == float.NegativeInfinity )
                    return false;

                bool isBlocked = false;
                MapItem isToBuilding = null;
                SecurityClearance requiredClearance = null;
                bool isIntersectingRoad = false;
                string debugText = string.Empty;
                bool hasValidDestinationPoint = true;

                if ( !unitType.IsConsideredMech )
                {
                    if ( !MoveHelper.MachineAndroid_ActualDestinationIsValid( unitType, ref destinationPoint, out isBlocked,
                        out isToBuilding, out requiredClearance, out isIntersectingRoad, out debugText ) )
                    {
                        hasValidDestinationPoint = false;
                        //debugText += "adv-block ";
                    }

                    if ( isToBuilding != null )
                    {
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
                            destinationPoint = isToBuilding.SimBuilding.GetEffectiveWorldLocationForContainedUnit();
                        }
                    }

                    if ( hasValidDestinationPoint && isToBuilding == null )
                        destinationPoint = destinationPoint.ReplaceY( isIntersectingRoad ? 1.3f : 0.4f );
                }
                else
                {
                    //for mechs

                    if ( !MoveHelper.MachineMech_ActualDestinationIsValid( unitType, ref destinationPoint, out isBlocked,
                        out requiredClearance, out debugText ) )
                    {
                        hasValidDestinationPoint = false;
                        //debugText += "adv-block ";
                    }

                    destinationPoint.y = 0;
                }

                Vector3 bestDeployerSpot = SimCommon.TheNetwork?.Tower?.GetBottomCenterPosition()??Vector3.zero;
                ISimMapActor bestDeployer = null;
                if ( unitType.IsConsideredMech )
                    FindBestMechDeployer( destinationPoint, ref bestDeployer, ref bestDeployerSpot, true );
                else
                    FindBestAndroidDeployer( destinationPoint, ref bestDeployer, ref bestDeployerSpot, true );
                if ( isMouseBlocked )
                    return true;

                bool isBlockedFromBuilding = false;

                if ( bestDeployer == null )
                {
                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint );
                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                    {
                        atMouse.Icon = IconRefs.Mouse_OutOfRange.Icon;
                        atMouse.ShouldTooltipBeRed = true;
                        atMouse.TitleUpperLeft.AddLang( unitType.IsConsideredMech ? "LocationIsTooFarFromAnyMechLauncher_Short" :
                            "LocationIsTooFarFromAnyAndroidLauncher_Short" );
                    }
                    isBlockedFromBuilding = true;
                }

                {
                    Lockdown blockedByLockdown = null;
                    if ( MoveHelper.MachineAny_DestinationBlockedByLockdown( destinationPoint, bestDeployerSpot, out blockedByLockdown ) )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                            ColorMath.Red, 1f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint );

                        MoveHelper.RenderLockdownWarning( TooltipID.Create( unitType ), blockedByLockdown );

                        return true;
                    }
                }

                bool canAfford = true;
                if ( !unitType.CalculateCanAfford() )
                {
                    if ( !isBlockedFromBuilding )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                            ColorMath.Red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_CannotAfford, destinationPoint, 0.5f );
                    }
                    canAfford = false;
                    isBlockedFromBuilding = true;
                }

                {
                    if ( !unitType.IsConsideredMech )
                    {
                        float rotationY = currentDeployedAndroidRotationY;

                        if ( hasValidDestinationPoint )
                        {
                            ProposedAndroidLocation proposedLoc = new ProposedAndroidLocation( unitType, destinationPoint, rotationY );

                            if ( !CityMap.CalculateIsValidLocationForCollidable( proposedLoc, destinationPoint, rotationY, false,
                                CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, CollisionRule.Strict ) )
                                hasValidDestinationPoint = false;
                        }

                        Vector3 drawnDestinationCenter = destinationPoint.PlusY( unitType.GetHalfHeightForCollisions() );

                        CombatTextHelper.ClearPredictionStats();

                        if ( hasValidDestinationPoint && canAfford )
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                isBlockedFromBuilding ? ColorRefs.DeploymentLineBlocked.ColorHDR : ColorRefs.DeploymentLineValid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            SharedRenderManagerData.RenderDeploymentHexes( drawnDestinationCenter, !isBlockedFromBuilding );

                            MoveHelper.RenderUnitTypeColoredForMoveTarget( unitType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                isBlockedFromBuilding ? ColorRefs.VehicleMoveGhostBlocked : ColorRefs.VehicleMoveGhostValid, false );

                            TheoreticalDeployedMachineActor theoreticalActor;
                            theoreticalActor.ActorDGD = unitType.DuringGameData;
                            theoreticalActor.UnitStance = MachineUnitStanceTable.BasicActiveStanceForAndroids;
                            theoreticalActor.VehicleStance = null;
                            theoreticalActor.Location = destinationPoint;

                            MoveHelper.DrawThreatLinesAgainstMapMobileActor( theoreticalActor, destinationPoint, isToBuilding?.SimBuilding,
                                true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting,
                                out CombatTextHelper.NextTurn_DamageFromEnemies,
                                out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                                null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );

                            bool isToRestrictedArea = CombatTextHelper.GetIsDGDBeingDeployedToRestrictedArea( unitType.DuringGameData, isToBuilding?.SimBuilding, destinationPoint, true );
                            bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( theoreticalActor );

                            if ( SimCommon.TotalCapacityUsed_Androids + unitType.UnitCapacityCost > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.Icon = unitType.ShapeIcon;
                                    atMouse.CanExpand = CanExpandType.Brief;
                                    atMouse.TitleUpperLeft.AddLang( "WillTakeYouAboveCap_H1", ColorTheme.RedOrange2 );
                                    atMouse.MainHeader.AddLang( "WillTakeYouAboveCap_H2" );
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        atMouse.Main.AddLang( "WillTakeYouAboveCap_Androids_Details" ).Line();

                                    if ( isToRestrictedArea )
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( unitType.DuringGameData, atMouse.Main,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                    if ( hasCombatLines )
                                    {
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                    }
                                }
                            }
                            else
                            {
                                if ( isToRestrictedArea )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( unitType.DuringGameData, atMouse.TitleUpperLeft,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                        if ( hasCombatLines )
                                        {
                                            atMouse.CanExpand = CanExpandType.Brief;
                                            CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                        }
                                    }
                                }
                                else if ( hasCombatLines )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        atMouse.CanExpand = CanExpandType.Brief;
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                    }
                                }
                            }
                        }
                        else
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                ColorRefs.VehicleMoveGhostBlocked.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            MoveHelper.RenderUnitTypeColoredForMoveTarget( unitType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                ColorRefs.VehicleMoveGhostBlocked, false );
                        }

                        {
                            LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                            if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Smaller : TooltipNovelWidth.SizeToText ) )
                            {
                                lowerLeft.Icon = unitType.ShapeIcon;
                                lowerLeft.TitleUpperLeft.AddRaw( unitType.GetDisplayName() );
                                lowerLeft.CanExpand = CanExpandType.Brief;

                                ArcenDoubleCharacterBuffer mainText = InputCaching.ShouldShowDetailedTooltips ? lowerLeft.Main : lowerLeft.MainHeader;
                                unitType.AppendDeploymentText( mainText );

                                if ( !debugText.IsEmpty() )
                                    mainText.Line().AddNeverTranslated( "debugText: ", true ).AddRaw( debugText );
                            }
                        }

                        #region Handle Clicks
                        if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && hasValidDestinationPoint && !isBlockedFromBuilding && canAfford )
                        {
                            bool isClicked = false;
                            if ( InputCaching.UseLeftClickForUnitDeployment )
                                isClicked = ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action
                            else
                                isClicked = ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action

                            if ( isClicked )
                            {
                                //no need to do any attacks of opportunity, as the unit is getting out of a craft!

                                ISimMachineUnit newUnit = World.Forces.CreateNewMachineUnitAtExactSpotOrBuildingForPlayer( unitType, rotationY,
                                    isToBuilding, destinationPoint, Engine_Universal.PermanentQualityRandom, true );
                                if ( newUnit != null )
                                {
                                    if ( unitType.TryPayCostsNow() )
                                    {
                                        ParticleSoundRefs.PlaceAndroidIntoWorld.DuringGame_PlayAtLocation( destinationPoint, new Vector3( 0, rotationY, 0 ) );
                                        //unitType.OnAppearAsNewUnit.DuringGame_PlayAtLocation( destinationPoint, new Vector3( 0, rotationY, 0 ) );
                                        CityStatisticTable.AlterScore( "AndroidsCreatedViaCommandMode", 1 );

                                        if ( !InputCaching.ShouldKeepDoingAction && InputCaching.CloseCommandModeAfterSingleDeployment )
                                        {
                                            Engine_HotM.SelectedMachineActionMode = null;
                                        }
                                    }
                                    else
                                        newUnit.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        //for mechs

                        float rotationY = currentDeployedAndroidRotationY;

                        if ( hasValidDestinationPoint )
                        {
                            ProposedMachineMechLocation proposedLoc = new ProposedMachineMechLocation( unitType, destinationPoint, rotationY );

                            if ( !CityMap.CalculateIsValidLocationForCollidable( proposedLoc, destinationPoint, rotationY, true,
                                CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, CollisionRule.Relaxed ) )
                                hasValidDestinationPoint = false;
                        }

                        Vector3 drawnDestinationCenter = destinationPoint.PlusY( unitType.GetHalfHeightForCollisions() );

                        CombatTextHelper.ClearPredictionStats();

                        if ( hasValidDestinationPoint && canAfford )
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                isBlockedFromBuilding ? ColorRefs.DeploymentLineBlocked.ColorHDR : ColorRefs.DeploymentLineValid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            SharedRenderManagerData.RenderDeploymentHexes( drawnDestinationCenter, !isBlockedFromBuilding );

                            MoveHelper.RenderUnitTypeColoredForMoveTarget( unitType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                isBlockedFromBuilding ? ColorRefs.VehicleMoveGhostBlocked : ColorRefs.VehicleMoveGhostValid, false );

                            TheoreticalDeployedMachineActor theoreticalActor;
                            theoreticalActor.ActorDGD = unitType.DuringGameData;
                            theoreticalActor.UnitStance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                            theoreticalActor.VehicleStance = null;
                            theoreticalActor.Location = destinationPoint;

                            MoveHelper.DrawThreatLinesAgainstMapMobileActor( theoreticalActor, destinationPoint, isToBuilding?.SimBuilding,
                                true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting,
                                out CombatTextHelper.NextTurn_DamageFromEnemies,
                                out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                                null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );

                            bool isToRestrictedArea = CombatTextHelper.GetIsDGDBeingDeployedToRestrictedArea( unitType.DuringGameData, isToBuilding?.SimBuilding, destinationPoint, true );
                            bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( theoreticalActor );

                            if ( SimCommon.TotalCapacityUsed_Mechs + unitType.UnitCapacityCost > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.Icon = unitType.ShapeIcon;
                                    atMouse.CanExpand = CanExpandType.Brief;
                                    atMouse.TitleUpperLeft.AddLang( "WillTakeYouAboveCap_H1", ColorTheme.RedOrange2 );
                                    atMouse.MainHeader.AddLang( "WillTakeYouAboveCap_H2" );
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        atMouse.Main.AddLang( "WillTakeYouAboveCap_Mechs_Details" ).Line();

                                    if ( isToRestrictedArea )
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( unitType.DuringGameData, atMouse.Main,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                    if ( hasCombatLines )
                                    {
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                    }
                                }
                            }
                            else
                            {
                                if ( isToRestrictedArea )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        CombatTextHelper.AppendRestrictedDGDAreaShort( unitType.DuringGameData, atMouse.TitleUpperLeft,
                                            false, isToBuilding?.SimBuilding, destinationPoint, true );
                                        if ( hasCombatLines )
                                        {
                                            atMouse.CanExpand = CanExpandType.Brief;
                                            CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                                CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                        }
                                    }
                                }
                                else if ( hasCombatLines )
                                {
                                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                        InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                    {
                                        atMouse.CanExpand = CanExpandType.Brief;
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                    }
                                }
                            }
                        }
                        else
                        {
                            DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                                ColorRefs.VehicleMoveGhostBlocked.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                            MoveHelper.RenderUnitTypeColoredForMoveTarget( unitType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                                ColorRefs.VehicleMoveGhostBlocked, false );
                        }

                        {
                            LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                            if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( unitType ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Smaller : TooltipNovelWidth.SizeToText ) )
                            {
                                lowerLeft.Icon = unitType.ShapeIcon;
                                lowerLeft.TitleUpperLeft.AddRaw( unitType.GetDisplayName() );
                                lowerLeft.CanExpand = CanExpandType.Brief;

                                ArcenDoubleCharacterBuffer mainText = InputCaching.ShouldShowDetailedTooltips ? lowerLeft.Main : lowerLeft.MainHeader;
                                unitType.AppendDeploymentText( mainText );

                                if ( !debugText.IsEmpty() )
                                    mainText.Line().AddNeverTranslated( "debugText: ", true ).AddRaw( debugText );
                            }
                        }

                        #region Handle Clicks
                        if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && hasValidDestinationPoint && !isBlockedFromBuilding && canAfford )
                        {
                            bool isClicked = false;
                            if ( InputCaching.UseLeftClickForUnitDeployment )
                                isClicked = ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action
                            else
                                isClicked = ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action

                            if ( isClicked )
                            {
                                //no need to do any attacks of opportunity, as the unit is getting out of a craft!

                                ISimMachineUnit newUnit = World.Forces.CreateNewMachineUnitAtExactSpotOrBuildingForPlayer( unitType, rotationY,
                                    null, destinationPoint, Engine_Universal.PermanentQualityRandom, true );
                                if ( newUnit != null )
                                {
                                    if ( unitType.TryPayCostsNow() )
                                    {
                                        ParticleSoundRefs.PlaceVehicleIntoWorld.DuringGame_PlayAtLocation( destinationPoint, new Vector3( 0, rotationY, 0 ) );
                                        CityStatisticTable.AlterScore( "MechsCreatedViaCommandMode", 1 );

                                        if ( !InputCaching.ShouldKeepDoingAction && InputCaching.CloseCommandModeAfterSingleDeployment )
                                        {
                                            Engine_HotM.SelectedMachineActionMode = null;
                                        }
                                    }
                                    else
                                        newUnit.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
                                }
                            }
                        }
                        #endregion
                    }
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "CommandModeHandler.HandleDeployMachineUnitType Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }
        #endregion

        #region HandleDeployMachineVehicleType
        private bool HandleDeployMachineVehicleType( MachineVehicleType vehicleType, bool IsHoverOnly, bool isMouseBlocked )
        {
            if ( IsHoverOnly || vehicleType == null )
                return false; //nothing to for this

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = lastWorldHitLocation;

                if ( destinationPoint.x == float.NegativeInfinity )
                    return false;

                Camera cam = Engine_HotM.GameModeData.MainCamera;
                Ray ray = cam.ScreenPointToRay( Input.mousePosition );
                float flightPlaneY = vehicleType.InitialHeight;
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


                bool isBlocked = false;
                SecurityClearance requiredClearance = null;
                string debugText = string.Empty;
                bool hasValidDestinationPoint = true;
                if ( !MoveHelper.MachineVehicle_ActualDestinationIsValid( vehicleType, ref destinationPoint, out isBlocked,
                    out requiredClearance, out debugText ) )
                {
                    hasValidDestinationPoint = false;
                    //debugText += "adv-block ";
                }

                destinationPoint.y = flightPlaneY;

                Vector3 bestDeployerSpot = SimCommon.TheNetwork?.Tower?.GetBottomCenterPosition() ?? Vector3.zero;
                ISimMapActor bestDeployer = null;
                FindBestVehicleDeployer( destinationPoint, ref bestDeployer, ref bestDeployerSpot, true );
                if ( isMouseBlocked )
                    return true;

                bool isBlockedFromBuilding = false;

                if ( bestDeployer == null )
                {
                    NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint );
                    if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( vehicleType ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                    {
                        atMouse.Icon = IconRefs.Mouse_OutOfRange.Icon;
                        atMouse.ShouldTooltipBeRed = true;
                        atMouse.TitleUpperLeft.AddLang( "LocationIsTooFarFromAnyAerospaceHangar_Short" );
                    }
                    isBlockedFromBuilding = true;
                }

                {
                    Lockdown blockedByLockdown = null;
                    if ( MoveHelper.MachineAny_DestinationBlockedByLockdown( destinationPoint, bestDeployerSpot, out blockedByLockdown ) )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                            ColorMath.Red, 1f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint );

                        MoveHelper.RenderLockdownWarning( TooltipID.Create( vehicleType ), blockedByLockdown );

                        return true;
                    }
                }

                bool canAfford = true;
                if ( !vehicleType.CalculateCanAfford() )
                {
                    if ( !isBlockedFromBuilding )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, destinationPoint,
                        ColorMath.Red, 2f, CatmullSlotType.Move, CatmullSlope.Movement );
                        CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_CannotAfford, destinationPoint, 0.5f );
                    }
                    canAfford = false;
                    isBlockedFromBuilding = true;
                }

                //if ( MouseHelper.ActorUnderCursor != null )
                //{
                //    //cursor over some other actor
                //    return true;
                //}
                //else
                {
                    float rotationY = currentDeployedAndroidRotationY;

                    if ( hasValidDestinationPoint )
                    {
                        ProposedVehicleLocation proposedLoc = new ProposedVehicleLocation( vehicleType, destinationPoint ); //don't care about rotation for vehicles

                        if ( !CityMap.CalculateIsValidLocationForCollidable( proposedLoc, destinationPoint, rotationY, true,
                            CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, CollisionRule.Strict ) )
                            hasValidDestinationPoint = false;
                    }

                    Vector3 drawnDestinationCenter = destinationPoint.PlusY( vehicleType.GetHalfHeightForCollisions() );

                    CombatTextHelper.ClearPredictionStats();

                    if ( hasValidDestinationPoint && canAfford )
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                            isBlockedFromBuilding ? ColorRefs.DeploymentLineBlocked.ColorHDR : ColorRefs.DeploymentLineValid.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                        SharedRenderManagerData.RenderDeploymentHexes( drawnDestinationCenter, !isBlockedFromBuilding );

                        MoveHelper.RenderVehicleTypeColoredForMoveTarget( vehicleType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                            isBlockedFromBuilding ? ColorRefs.VehicleMoveGhostBlocked : ColorRefs.VehicleMoveGhostValid );

                        TheoreticalDeployedMachineActor theoreticalActor;
                        theoreticalActor.ActorDGD = vehicleType.DuringGameData;
                        theoreticalActor.UnitStance = null;
                        theoreticalActor.VehicleStance = MachineVehicleStanceTable.BasicActiveStanceForVehicles;
                        theoreticalActor.Location = destinationPoint;

                        MoveHelper.DrawThreatLinesAgainstMapMobileActor( theoreticalActor, destinationPoint, null,
                            true, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting,
                            out CombatTextHelper.NextTurn_DamageFromEnemies,
                            out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                            out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                            null, AttackAmounts.Zero(), EnemyTargetingReason.ProposedDestination, false, ThreatLineLogic.Normal );

                        bool isToRestrictedArea = CombatTextHelper.GetIsDGDBeingDeployedToRestrictedArea( vehicleType.DuringGameData, null, destinationPoint, true );
                        bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( theoreticalActor );

                        if ( SimCommon.TotalCapacityUsed_Vehicles + vehicleType.VehicleCapacityCost > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt )
                        {
                            NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                            if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( vehicleType ), null, SideClamp.Any,
                                InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                            {
                                atMouse.Icon = vehicleType.ShapeIcon;
                                atMouse.CanExpand = CanExpandType.Brief;
                                atMouse.TitleUpperLeft.AddLang( "WillTakeYouAboveCap_H1", ColorTheme.RedOrange2 );
                                atMouse.MainHeader.AddLang( "WillTakeYouAboveCap_H2" );
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    atMouse.Main.AddLang( "WillTakeYouAboveCap_Vehicles_Details" ).Line();

                                if ( isToRestrictedArea )
                                    CombatTextHelper.AppendRestrictedDGDAreaShort( vehicleType.DuringGameData, atMouse.Main,
                                        false, null, destinationPoint, true );
                                if ( hasCombatLines )
                                {
                                    CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                }
                            }
                        }
                        else
                        {
                            if ( isToRestrictedArea )
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( vehicleType ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips && hasCombatLines ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    CombatTextHelper.AppendRestrictedDGDAreaShort( vehicleType.DuringGameData, atMouse.TitleUpperLeft,
                                        false, null, destinationPoint, true );
                                    if ( hasCombatLines )
                                    {
                                        atMouse.CanExpand = CanExpandType.Brief;
                                        CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                    }
                                }
                            }
                            else if ( hasCombatLines )
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( vehicleType ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.CanExpand = CanExpandType.Brief;
                                    CombatTextHelper.AppendLastPredictedDamageBrief( theoreticalActor, atMouse.Main, TTTextBefore.None, TTTextAfter.Linebreak );
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        CombatTextHelper.AppendLastPredictedDamageLongSecondary( theoreticalActor, true, false, false, true );
                                }
                            }
                        }
                    }
                    else
                    {
                        DrawHelper.RenderCatmullLine( bestDeployerSpot, drawnDestinationCenter,
                            ColorRefs.VehicleMoveGhostBlocked.ColorHDR, 1f, CatmullSlotType.Move, CatmullSlope.Movement );

                        MoveHelper.RenderVehicleTypeColoredForMoveTarget( vehicleType, destinationPoint, Quaternion.Euler( 0, rotationY, 0 ),
                            ColorRefs.VehicleMoveGhostBlocked );
                    }

                    {
                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( vehicleType ), null, SideClamp.Any,
                            InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Smaller : TooltipNovelWidth.SizeToText ) )
                        {
                            lowerLeft.Icon = vehicleType.ShapeIcon;
                            lowerLeft.TitleUpperLeft.AddRaw( vehicleType.GetDisplayName() );
                            lowerLeft.CanExpand = CanExpandType.Brief;

                            ArcenDoubleCharacterBuffer mainText = InputCaching.ShouldShowDetailedTooltips ? lowerLeft.Main : lowerLeft.MainHeader;
                            vehicleType.AppendDeploymentText( mainText );

                            if ( !debugText.IsEmpty() )
                                mainText.Line().AddNeverTranslated( "debugText: ", true ).AddRaw( debugText );
                        }
                    }

                    #region Handle Clicks
                    if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0 && hasValidDestinationPoint && !isBlockedFromBuilding && canAfford )
                    {
                        bool isClicked = false;
                        if ( InputCaching.UseLeftClickForUnitDeployment )
                            isClicked = ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action
                        else
                            isClicked = ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //ability-action

                        if ( isClicked )
                        {
                            //no need to do any attacks of opportunity, as the unit is getting out of a craft!

                            ISimMachineVehicle newVehicle = World.Forces.CreateNewMachineVehicleAtExactSpotForPlayer( vehicleType, rotationY,
                                destinationPoint, Engine_Universal.PermanentQualityRandom, true );

                            if ( newVehicle != null )
                            {
                                if ( vehicleType.TryPayCostsNow() )
                                {
                                    ParticleSoundRefs.PlaceVehicleIntoWorld.DuringGame_PlayAtLocation( destinationPoint, new Vector3( 0, rotationY, 0 ) );
                                    CityStatisticTable.AlterScore( "VehiclesCreatedViaCommandMode", 1 );

                                    if ( !InputCaching.ShouldKeepDoingAction && InputCaching.CloseCommandModeAfterSingleDeployment )
                                    {
                                        Engine_HotM.SelectedMachineActionMode = null;
                                    }
                                }
                                else
                                    newVehicle.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
                            }
                        }
                    }
                    #endregion
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "CommandModeHandler.HandleDeployMachineVehicleType Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }
        #endregion

        #region HandleAction
        private bool HandleAction( MachineCommandModeAction action, bool IsHoverOnly )
        {
            return false;
        }
        #endregion

        public float currentDeployedAndroidRotationY = 0f;

        public void HandlePassedInput( int Int1, InputActionTypeData InputActionType )
        {
            if ( currentCategory != null )
            {
                MachineCommandModeAction action = Engine_HotM.CurrentCommandModeActionTargeting;
                if ( action != null && action.DuringGame_IsVisible() )
                {
                    //nothing do do right now I guess
                }
                else
                {
                    NPCUnitType npc = currentCategory.NonSim_TargetingDeployableNPCType;
                    if ( npc != null && npc.DuringGame_IsUnlockedForPlayer() )
                    {
                        string InputActionID = InputActionType.ID;
                        switch ( InputActionID )
                        {
                            case "RotateBulkUnitLeft":
                                {
                                    ArcenInput.BlockUntilNextFrame();

                                    currentDeployedAndroidRotationY -= 90;
                                    while ( currentDeployedAndroidRotationY < 0 )
                                        currentDeployedAndroidRotationY += 360;
                                }
                                break;
                            case "RotateBulkUnitRight":
                                {
                                    ArcenInput.BlockUntilNextFrame();

                                    currentDeployedAndroidRotationY += 90;
                                    while ( currentDeployedAndroidRotationY >= 360 )
                                        currentDeployedAndroidRotationY -= 360;
                                }
                                break;
                        }
                    }
                }
            }
        }

        public void TriggerSlotNumber( int SlotNumber )
        {
            int categoryIndex = 1;
            foreach ( MachineCommandModeCategory category in SimCommon.ActiveCommandModeCategories.GetDisplayList() )
            {
                if ( categoryIndex == SlotNumber )
                {
                    if ( !category.GetShouldBeEnabled() )
                        return;

                    if ( currentCategory != null )
                    {
                        //this was super confusing when it remembered!
                        Engine_HotM.CurrentCommandModeActionTargeting = null;
                        category.NonSim_TargetingDeployableNPCType = null;
                    }

                    if ( currentCategory == category )
                        currentCategory = null;
                    else
                        currentCategory = category;
                    category.NonSim_TargetingDeployableNPCType = null;

                    return;
                }
                categoryIndex++;
            }
        }
    }
}

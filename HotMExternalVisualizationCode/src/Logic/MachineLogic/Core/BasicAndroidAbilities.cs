using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class BasicAndroidAbilities : IAbilityImplementation
    {
        public ActorAbilityResult TryHandleAbility( ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            if ( Ability == null || Actor == null )
                return ActorAbilityResult.PlayErrorSound;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                if ( unit == null )
                {
                    debugStage = 200;
                    if ( Actor is ISimMachineVehicle vehicle )
                        ArcenDebugging.LogSingleLine( "BasicAndroidAbilities: Called HandleAbility for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError );
                    return ActorAbilityResult.PlayErrorSound;
                }

                debugStage = 500;
                if ( Ability.MustBeTargeted )
                {
                    switch ( Ability.ID )
                    {                        
                        case "AndroidForceConversation":
                            debugStage = 1200;
                            return BasicActionsHelper.FullHandleForceIntoConversation( Actor, Ability, BufferOrNull, Logic );
                    }

                    switch ( Logic )
                    {
                        default:
                            return ActorAbilityResult.OpenedInterface;
                        case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                        case ActorAbilityLogic.ExecuteAbilityAutomated:
                        case ActorAbilityLogic.TriggerAbilityAltView:
                            {
                                debugStage = 700;
                                if ( Actor.IsInAbilityTypeTargetingMode == Ability )
                                    Actor.SetTargetingMode( null, null );
                                else
                                    Actor.SetTargetingMode( Ability, null );
                                return ActorAbilityResult.OpenedInterface;
                            }
                    }
                }

                switch ( Ability.ID )
                {
                    case "AndroidStandby":
                        return BasicActionsHelper.FullHandleStandby( Actor, Ability, BufferOrNull, Logic );
                    case "AndroidBattleRecharge":
                        debugStage = 1200;
                        return BasicActionsHelper.FullHandleBattleRecharge( Actor, Ability, BufferOrNull, Logic );
                    case "AndroidNearbyRepair":
                        debugStage = 1200;
                        return BasicActionsHelper.FullHandleRepairNearby( Actor, Ability, BufferOrNull, Logic );
                    case "AndroidNetworkShield":
                        debugStage = 1200;
                        return BasicActionsHelper.FullHandleNetworkShieldNearby( Actor, Ability, BufferOrNull, Logic );
                    case "AndroidNetworkTargeting":
                        debugStage = 1200;
                        return BasicActionsHelper.FullHandleNetworkTargetingNearby( Actor, Ability, BufferOrNull, Logic );
                    case "Flamethrower":
                        return BasicActionsHelper.FullHandleFlamethrower( Actor, Ability, BufferOrNull, Logic );
                    case "Demolish":
                    case "Wallripper":
                    case "QuietlyLoot":
                    case "HackUnit":
                    case "ProbeExoticComms":
                        return ActorAbilityResult.PlayErrorSound; //nothing to do in this part
                    case "AndroidUseItem":
                        debugStage = 1200;
                        return BasicActionsHelper.HandleConsumablesAbility( Actor, Ability, BufferOrNull, Logic );
                    case "AndroidTakeCover":
                        #region AndroidTakeCover
                        {
                            debugStage = 1200;
                            switch ( Logic )
                            {
                                case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                    {
                                        if ( unit.GetHasBadge( Ability.RelatedBadge ) )
                                        {
                                            if ( BufferOrNull != null )
                                                BufferOrNull.AddLang( "UnitAlreadyTakingCover" );
                                            return ActorAbilityResult.PlayErrorSound;
                                        }
                                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                                    }
                                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                                    {

                                    }
                                    break;
                                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                                case ActorAbilityLogic.ExecuteAbilityAutomated:
                                    if ( !unit.GetIsBlockedFromActingForAbility( Ability, true ) && !unit.GetHasBadge( Ability.RelatedBadge ) && BuildingOrNull != null &&
                                        BuildingOrNull.GetIsLocationStillValid() )
                                    {
                                        unit.AddOrRemoveBadge( Ability.RelatedBadge, true );

                                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                                    }
                                    break;
                            }
                            return ActorAbilityResult.PlayErrorSound;
                        }
                    #endregion
                    case "AndroidMercurialForm":
                        #region AndroidMercurialForm
                        {
                            debugStage = 1200;
                            switch ( Logic )
                            {
                                case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                    {
                                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                                    }
                                case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                                case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                                    {

                                    }
                                    break;
                                case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                                case ActorAbilityLogic.ExecuteAbilityAutomated:
                                    if ( !unit.GetIsBlockedFromActingForAbility( Ability, true ) )
                                    {
                                        unit.AddOrRemoveBadge( Ability.RelatedBadge, !unit.GetHasBadge( Ability.RelatedBadge ) );
                                        return ActorAbilityResult.SuccessDidFullAbilityNow;
                                    }
                                    break;
                            }
                            return ActorAbilityResult.PlayErrorSound;
                        }
                    #endregion
                    default:
                        ArcenDebugging.LogSingleLine( "BasicAndroidAbilities: Called HandleAbility for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
                        return ActorAbilityResult.PlayErrorSound;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BasicAndroidAbilities.TryHandleAbility", debugStage, Ability?.ID??"[null-ability]", e, Verbosity.ShowAsError );
                return ActorAbilityResult.PlayErrorSound;
            }
        }

        public void HandleAbilityHardTargeting( ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange )
        {
        }

        public void HandleAbilityMixedTargeting( ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange )
        {
            if ( Ability == null || Actor == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                if ( unit == null )
                {
                    debugStage = 200;
                    if ( Actor is ISimMachineVehicle vehicle )
                        ArcenDebugging.LogSingleLine( "BasicAndroidAbilities: Called HandleAbilityHardTargeting for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError );
                    return;
                }


                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );

                switch ( Ability.ID )
                {
                    case "Demolish":
                        #region Demolish
                        {
                            debugStage = 1200;

                            //lot of range on this one
                            if ( attackRange < moveRange )
                                attackRange = moveRange;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            TargetingHelper.DoForAllBuildingsWithinRangeTight( unit, attackRange, CommonRefs.DemolishTarget,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one
                                    if ( Building.CurrentOccupyingUnit != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;
                                else
                                    destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                            }


                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                                unit.RotateAndroidToFacePoint( destinationPoint );
                            else
                                return; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

                            if ( !isInRange )
                            {
                                return;
                            }

                            if ( buildingUnderCursor == null || !(buildingUnderCursor?.GetVariant()?.Tags?.ContainsKey( CommonRefs.DemolishTarget.ID ) ?? false) ||
                                buildingUnderCursor.MachineStructureInBuilding != null || buildingUnderCursor.CurrentOccupyingUnit != null )
                            {
                                return;
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( buildingUnderCursor ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.Icon = Ability.Icon;
                                    novel.TitleUpperLeft.AddRightClickFormat( "Move_ClickToDemolish" );
                                }

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.1f, 1.1f, 1.1f ), HighlightPass.AlwaysHappen, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    //do the thing
                                    unit.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                                    unit.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                                    unit.AlterCurrentActionPoints( -Ability.ActionPointCost );
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );

                                    ProjectileInstructionPrePacket prePacket = ProjectileInstructionPrePacket.CreateVerySimple( center, destinationPoint.ReplaceY( groundLevel ),
                                        unit, delegate
                                        {
                                            int residentCount = buildingUnderCursor.GetTotalResidentCount();

                                            if ( residentCount > 0 )
                                            {
                                                CityStatisticTable.AlterScore( "Murders", residentCount );

                                                CityStatisticTable.AlterScore( "CitizensHitByExplodingDrones", residentCount );
                                            }
                                            CityStatisticTable.AlterScore( "BuildingsDemolishedByDrones", 1 );

                                            BuildingTypeVariant variant = buildingUnderCursor.GetVariant();

                                            buildingUnderCursor.KillEveryoneHere();

                                            buildingUnderCursor?.SetStatus( CommonRefs.DemolishedBuildingStatus );
                                            buildingUnderCursor.GetMapItem().DropBurningEffect();

                                            if ( FlagRefs.Ch1_MIN_ManArmorPierce.DuringGame_ActualOutcome != null || SimMetagame.CurrentChapterNumber > 1 )
                                            {
                                                if ( variant?.DemolishSpecialManager != null )
                                                {
                                                    variant.DemolishSpecialManager.HandleManualInvocationAtPoint( destinationPoint, Engine_Universal.PermanentQualityRandom, true );
                                                }
                                                else
                                                {
                                                    //scarier stuff
                                                    ManagerRefs.Man_DemolishBuildingHarder.HandleManualInvocationAtPoint( destinationPoint, Engine_Universal.PermanentQualityRandom, true );
                                                }
                                            }
                                            else
                                            {
                                                //less-scary stuff
                                                ManagerRefs.Man_DemolishBuildingEasy.HandleManualInvocationAtPoint( destinationPoint, Engine_Universal.PermanentQualityRandom, true );
                                            }
                                        } );

                                    Ability.OnTargetedUse.DuringGame_PlayAtLocationWithTarget( center, prePacket, false );

                                    if ( !InputCaching.ShouldKeepDoingAction )
                                        unit.SetTargetingMode( null, null );
                                }
                            }
                        }
                        break;
                    #endregion
                    case "Wallripper":
                        #region Wallripper
                        {
                            debugStage = 1200;

                            //lot of range on this one
                            if ( attackRange < moveRange )
                                attackRange = moveRange;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            TargetingHelper.DoForAllBuildingsWithinRangeTight( unit, attackRange, null, //not limited by tag in this case
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;
                                    if ( Building.HasSpecialResourceAlreadyBeenExtracted )
                                        return false;
                                    BuildingTypeVariant variant = Building.GetVariant();
                                    if ( variant == null || variant.SpecialScavengeResource == null )
                                        return false;
                                    if ( !variant.SpecialScavengeResource.DuringGame_IsUnlocked() )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one
                                    if ( Building.CurrentOccupyingUnit != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidForMachineStructure.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                    RenderManager_Streets.RenderSpecialResourceAtBuilding( item,
                                        delegate
                                        {
                                            //hovering this building
                                            JobHelper.HandleHoverSpecialtyResource( unit, Ability, attackRange, center, Building );
                                        }, framesPrepped );
                                    return false; //keep going
                                } );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;
                            }

                            JobHelper.HandleHoverSpecialtyResource( unit, Ability, attackRange, destinationPoint, buildingUnderCursor );
                        }
                        break;
                    #endregion
                    case "QuietlyLoot":
                        #region QuietlyLoot
                        {
                            debugStage = 1200;

                            //lot of range on this one
                            if ( attackRange < moveRange )
                                attackRange = moveRange;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            TargetingHelper.DoForAllBuildingsWithinRangeTight( unit, attackRange, null, //not limited by tag in this case
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;
                                    if ( Building.HasSpecialResourceAlreadyBeenExtracted )
                                        return false;
                                    BuildingTypeVariant variant = Building.GetVariant();
                                    if ( variant == null || variant.SpecialScavengeResource == null )
                                        return false;
                                    if ( !variant.SpecialScavengeResource.DuringGame_IsUnlocked() )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one
                                    if ( Building.CurrentOccupyingUnit != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidForMachineStructure.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                    RenderManager_Streets.RenderSpecialResourceAtBuilding( item,
                                        delegate
                                        {
                                            //hovering this building
                                            JobHelper.HandleHoverSpecialtyResource( unit, Ability, attackRange, center, Building );
                                        }, framesPrepped );
                                    return false; //keep going
                                } );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;
                            }

                            JobHelper.HandleHoverSpecialtyResource( unit, Ability, attackRange, destinationPoint, buildingUnderCursor );
                        }
                        break;
                    #endregion
                    case "HackUnit":
                        #region HackUnit
                        {
                            debugStage = 1200;

                            DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                                unit.RotateAndroidToFacePoint( destinationPoint );
                            else
                                return; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= moveRange * moveRange;

                            ISimNPCUnit hoveredNPC = MouseHelper.ActorUnderCursor as ISimNPCUnit;
                            bool isValidNPC = hoveredNPC != null && hoveredNPC.GetActorDataMaximum( ActorRefs.NPCHackingResistance, true ) > 0 && 
                                !hoveredNPC.GetIsAnAllyFromThePlayerPerspective() && hoveredNPC.UnitType.HackingScenario != null;
                            bool isHostileNPC = hoveredNPC != null && !hoveredNPC.GetIsAnAllyFromThePlayerPerspective();

                            if ( !isInRange )
                            {
                                if ( hoveredNPC != null && (isValidNPC ||!MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) ) )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                        novel.Main.AddLangAndAfterLineItemHeader( isValidNPC ? "ConsumableTargeting_ValidTarget" : "ConsumableTargeting_InvalidTarget",
                                            isValidNPC ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 ).AddRaw( hoveredNPC.GetDisplayName() );
                                    }
                                }
                                return;
                            }

                            if ( hoveredNPC != null && !isValidNPC && isHostileNPC )
                            {
                                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "ConsumableTargeting_InvalidTarget" );
                                    novel.Main.AddLang( "HackUnit_Invalid" );
                                }

                                return;
                            }

                            if ( hoveredNPC != null && isValidNPC )
                            {
                                int hackingSkill = unit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                                int resistance = hoveredNPC.GetActorDataCurrent( ActorRefs.NPCHackingResistance, true );

                                {
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = Ability.Icon;
                                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "HackUnit_Action_Header" ).AddRaw( hoveredNPC.GetDisplayName() );
                                        novel.Main.StartStyleLineHeightA().AddBoldLangAndAfterLineItemHeader( "HackUnit_Action_ScenarioWillStart", ColorTheme.DataLabelWhite )
                                            .AddRaw( hoveredNPC.UnitType.HackingScenario.GetDisplayName() ).Line()
                                            .AddBoldLangAndAfterLineItemHeader( "HackUnit_Action_Resistance", ColorTheme.DataLabelWhite ).AddRaw(
                                                resistance.ToStringThousandsWhole(), ColorTheme.DataBlue );
                                    }

                                    {
                                        //then draw the line from that spot to the final spot
                                        DrawHelper.RenderCatmullLine( destinationPoint.PlusY( unit.GetHalfHeightForCollisions() ), center,
                                           ColorRefs.MachineUnitHackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                                    }
                                    CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, Ability.Icon, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitHackLine.ColorHDR );

                                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                    {
                                        //do the thing

                                        Window_HackOptionList.HandleOpenCloseToggle( unit, hoveredNPC, Ability );                                        

                                        //if ( !InputCaching.ShouldKeepDoingAction )
                                        //    unit.SetTargetingMode( null, null );
                                    }
                                }
                            }
                        }
                        break;
                    #endregion
                    case "ProbeExoticComms":
                        #region ProbeExoticComms
                        {
                            debugStage = 1200;

                            DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }
                            if ( (Actor.GetTypeAsRow()?.ID??string.Empty) != "CombatUnitRed" )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }    

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                                unit.RotateAndroidToFacePoint( destinationPoint );
                            else
                                return; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= moveRange * moveRange;

                            ISimNPCUnit hoveredNPC = MouseHelper.ActorUnderCursor as ISimNPCUnit;
                            bool isValidNPC = hoveredNPC != null && /*
                                !hoveredNPC.GetIsAnAllyFromThePlayerPerspective() && */hoveredNPC.UnitType.ProbeCommsScenario != null;
                            //bool isHostileNPC = hoveredNPC != null && !hoveredNPC.GetIsAnAllyFromThePlayerPerspective();

                            if ( !isInRange )
                            {
                                if ( hoveredNPC != null && (isValidNPC || !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint )) )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                        novel.Main.AddLangAndAfterLineItemHeader( isValidNPC ? "ConsumableTargeting_ValidTarget" : "ConsumableTargeting_InvalidTarget",
                                            isValidNPC ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 ).AddRaw( hoveredNPC.GetDisplayName() );
                                    }
                                }
                                return;
                            }

                            if ( hoveredNPC != null && !isValidNPC )//&& isHostileNPC )
                            {
                                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "ConsumableTargeting_InvalidTarget" );
                                    novel.Main.AddLang( "ProbeCommsOfUnit_Invalid" );
                                }

                                return;
                            }

                            if ( hoveredNPC != null && isValidNPC )
                            {
                                {
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = Ability.Icon;
                                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "ProbeCommsOfUnit_Action_Header" ).AddRaw( hoveredNPC.GetDisplayName() );
                                        novel.Main.StartStyleLineHeightA().AddBoldLangAndAfterLineItemHeader( "HackUnit_Action_ScenarioWillStart", ColorTheme.DataLabelWhite )
                                            .AddRaw( hoveredNPC.UnitType.ProbeCommsScenario.GetDisplayName() ).Line();
                                    }

                                    {
                                        //then draw the line from that spot to the final spot
                                        DrawHelper.RenderCatmullLine( destinationPoint.PlusY( unit.GetHalfHeightForCollisions() ), center,
                                           ColorRefs.MachineUnitHackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                                    }
                                    CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, Ability.Icon, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitHackLine.ColorHDR );

                                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                    {
                                        //do the thing
                                        unit.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                                        unit.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                                        unit.AlterCurrentActionPoints( -Ability.ActionPointCost );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        Ability.OnTargetedUse?.DuringGame_PlayAtLocation( destinationPoint, TargetingHelper.GetRotationAngleBetweenPoints( center, destinationPoint ) );

                                        Hacking.HackingScene.HackerUnit = unit;
                                        Hacking.HackingScene.TargetUnit = hoveredNPC;
                                        Hacking.HackingScene.TargetDifficultyForAlly = 0;
                                        Hacking.HackingScene.Scenario = hoveredNPC.UnitType.ProbeCommsScenario.GetEffectiveScenarioType();
                                        Hacking.HackingScene.HasPopulatedScene = false;
                                        Engine_HotM.CurrentLowerMode = CommonRefs.HackingScene;

                                        //if ( !InputCaching.ShouldKeepDoingAction )
                                        //    unit.SetTargetingMode( null, null );
                                    }
                                }
                            }
                        }
                        break;
                    #endregion
                    case "AndroidForceConversation":
                        #region AndroidForceConversation
                        {
                            debugStage = 1200;

                            DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                                unit.RotateAndroidToFacePoint( destinationPoint );
                            else
                                return; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= moveRange * moveRange;

                            ISimNPCUnit hoveredNPC = MouseHelper.ActorUnderCursor as ISimNPCUnit;
                            bool isValidNPC = hoveredNPC != null && (hoveredNPC?.IsManagedUnit?.GetDialogCountCanBeForcedInto()??0) > 0;
                            bool isHostileNPC = hoveredNPC != null && !hoveredNPC.GetIsPartOfPlayerForcesInAnyWay();

                            if ( !isInRange )
                            {
                                if ( hoveredNPC != null && (isValidNPC || !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint )) )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                        novel.Main.AddLangAndAfterLineItemHeader( isValidNPC ? "ConsumableTargeting_ValidTarget" : "ConsumableTargeting_InvalidTarget",
                                            isValidNPC ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 ).AddRaw( hoveredNPC.GetDisplayName() );
                                    }
                                }
                                return;
                            }

                            if ( hoveredNPC != null && !isValidNPC && isHostileNPC )
                            {
                                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "ConsumableTargeting_InvalidTarget" );
                                    novel.Main.AddLang( "ForceConversation_Invalid" );
                                }

                                return;
                            }

                            if ( hoveredNPC != null && isValidNPC )
                            {
                                Vector3 originalDest = destinationPoint;

                                //find the nearest spot to this location
                                ISimUnitLocation targetLocation = PathingHelper.FindBestMachineUnitLocationNearCoordinatesFromSearchSpot( unit, destinationPoint, destinationPoint, 3f,
                                    1 ); //if it's clearance 1, we don't care
                                if ( targetLocation == null )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "Move_NoRoomToMoveForward" );
                                        novel.Main.AddLang( "Move_NoRoomToMoveForward_StrategyTip" );
                                    }

                                    return;
                                }
                                else
                                {
                                    destinationPoint = targetLocation.GetEffectiveWorldLocationForContainedUnit();
                                }

                                if ( !hoveredNPC.IsManagedUnit.DialogCanBeDoneByShellCompanyUnits && unit.UnitType.IsTiedToShellCompany )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "WrongKindOfUnitToTalk" );
                                        novel.Main.AddLang( "WrongKindOfUnitToTalk_Shell_StrategyTip" );
                                    }

                                    return;
                                }

                                if ( !hoveredNPC.IsManagedUnit.DialogCanBeDoneByPMCImpostor && unit.UnitType.IsPMCImpostor )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "WrongKindOfUnitToTalk" );
                                        novel.Main.AddLang( "WrongKindOfUnitToTalk_PMC_StrategyTip" );
                                    }

                                    return;
                                }

                                if ( !hoveredNPC.IsManagedUnit.DialogCanBeDoneByRegularUnits && !unit.UnitType.IsPMCImpostor && !unit.UnitType.IsTiedToShellCompany )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "WrongKindOfUnitToTalk" );
                                        if ( hoveredNPC.IsManagedUnit.DialogCanBeDoneByPMCImpostor )
                                            novel.Main.AddLang( "WrongKindOfUnitToTalk_MustBePMC_StrategyTip" );
                                        else if ( hoveredNPC.IsManagedUnit.DialogCanBeDoneByShellCompanyUnits )
                                            novel.Main.AddLang( "WrongKindOfUnitToTalk_MustBeShell_StrategyTip" );
                                        else
                                            novel.Main.AddLang( "WrongKindOfUnitToTalk_Other_StrategyTip" );                                        
                                    }

                                    return;
                                }

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredNPC ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                {
                                    novel.Icon = Ability.Icon;
                                    novel.TitleUpperLeft.AddLang( "ForceConversation_Header" );
                                    novel.Main.AddRaw( hoveredNPC.GetDisplayName() );
                                }

                                {
                                    //draw the line to the close-helper spot
                                    DrawHelper.RenderCatmullLine( center, destinationPoint,
                                        ColorRefs.MachineUnitMoveActionLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );

                                    //then draw the ghost of the unit at that spot
                                    Quaternion theoreticalRotation = MoveHelper.CalculateRotationForMove( Quaternion.identity,// Unit.GetDrawRotation(), 
                                        targetLocation.GetEffectiveWorldLocationForContainedUnit(), destinationPoint );
                                    MoveHelper.RenderUnitTypeColoredForMoveTarget( unit.UnitType, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid, false );

                                    //then draw the line from that spot to the final spot
                                    DrawHelper.RenderCatmullLine( destinationPoint.PlusY( unit.GetHalfHeightForCollisions() ), originalDest,
                                       ColorRefs.MachineUnitForceConversationLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                                }
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, Ability.Icon, IconRefs.MouseMoveMode_Valid, originalDest, ColorRefs.MachineUnitForceConversationLine.ColorHDR );

                                NPCDialog dialog = hoveredNPC.IsManagedUnit?.GetNextForcedDialog();
                                if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                                { } //do not do the action
                                else if ( dialog != null && ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    //move in
                                    unit.ApplyVisibilityFromAction( ActionVisibility.IsMovement );
                                    unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( center ); //consider rotation here
                                    unit.AlterCurrentActionPoints( -1 );
                                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                    SimCommon.HasGivenAnyAndroidMovementOrders = true;

                                    unit.SetMeleeCallbackForAfterMovementEnds( delegate //this will be called-back on the main thread
                                    {
                                        //do the thing
                                        SimCommon.RewardProvider = NPCDialogChoiceHandler.Start( unit, dialog, hoveredNPC, true );
                                        SimCommon.OpenWindowRequest = OpenWindowRequest.Reward;
                                        ParticleSoundRefs.Dialog.DuringGame_PlayAtLocation( unit.GetDrawLocation(), unit.GetDrawRotation().eulerAngles );
                                    } );
                                    unit.SetDesiredContainerLocation( targetLocation );                                    
                                }
                            }
                        }
                        break;
                    #endregion
                    case "InfiltrationSysOp":
                        #region InfiltrationSysOp
                        {
                            debugStage = 1200;

                            DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            if ( unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost )
                            {
                                unit.SetTargetingMode( null, null );
                                return;
                            }

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                                unit.RotateAndroidToFacePoint( destinationPoint );
                            else
                                return; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= moveRange * moveRange;

                            ISimMachineActor hoveredAlly = MouseHelper.ActorUnderCursor as ISimMachineActor;

                            if ( hoveredAlly == null )
                                hoveredAlly = buildingUnderCursor?.CurrentOccupyingUnit as ISimMachineActor;

                            if ( hoveredAlly == null )
                                return;

                            HackingScenarioType scenario = hoveredAlly?.CurrentActionOverTime?.RelatedInvestigationTypeOrNull?.HackBuildingScenario;
                            int scenarioDifficulty = hoveredAlly?.CurrentActionOverTime?.RelatedInvestigationTypeOrNull?.HackingBuildingScenarioDifficulty ?? 100;
                            if ( scenario != null && scenarioDifficulty < 100 )
                                scenarioDifficulty = 100;

                            bool isValidAllyToInfiltrationSysOp = scenario != null;

                            if ( !isInRange )
                            {
                                if ( hoveredAlly != null && isValidAllyToInfiltrationSysOp )
                                {
                                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfRange, destinationPoint, 0.2f );

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredAlly ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                        novel.ShouldTooltipBeRed = true;

                                        novel.TitleUpperLeft.AddLang( "ProvideInfiltrationSysOpForAlly_Action_Header" );
                                        novel.Main.AddLangAndAfterLineItemHeader( "Move_OutOfRange",
                                            isValidAllyToInfiltrationSysOp ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 ).AddRaw( hoveredAlly.GetDisplayName() );
                                    }
                                    return;
                                }
                            }

                            if ( hoveredAlly != null && !isValidAllyToInfiltrationSysOp && hoveredAlly != unit )
                            {
                                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredAlly ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.Icon = IconRefs.Mouse_Invalid.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "ProvideInfiltrationSysOpForAlly_Action_Header" );
                                    novel.Main.AddLang( "ConsumableTargeting_InvalidTarget" );
                                }

                                return;
                            }

                            if ( hoveredAlly != null && isValidAllyToInfiltrationSysOp )
                            {
                                {
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( hoveredAlly ), null, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.Icon = Ability.Icon;
                                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "ProvideInfiltrationSysOpForAlly_Action_Header" ).AddRaw( hoveredAlly.GetDisplayName() );
                                        novel.Main.StartStyleLineHeightA().AddBoldLangAndAfterLineItemHeader( "HackUnit_Action_ScenarioWillStart", ColorTheme.DataLabelWhite )
                                            .AddRaw( scenario.GetDisplayName() ).Line();
                                    }

                                    {
                                        //then draw the line from that spot to the final spot
                                        DrawHelper.RenderCatmullLine( destinationPoint.PlusY( unit.GetHalfHeightForCollisions() ), center,
                                            ColorRefs.MachineUnitHackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.Movement );
                                    }
                                    CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, Ability.Icon, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitHackLine.ColorHDR );

                                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                    {
                                        //do the thing
                                        unit.AlterCurrentActionPoints( -Ability.ActionPointCost );
                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        Ability.OnTargetedUse?.DuringGame_PlayAtLocation( destinationPoint, TargetingHelper.GetRotationAngleBetweenPoints( center, destinationPoint ) );

                                        Hacking.HackingScene.HackerUnit = unit;
                                        Hacking.HackingScene.TargetUnit = hoveredAlly;
                                        Hacking.HackingScene.TargetDifficultyForAlly = scenarioDifficulty;
                                        Hacking.HackingScene.Scenario = scenario;
                                        Hacking.HackingScene.HasPopulatedScene = false;
                                        Engine_HotM.CurrentLowerMode = CommonRefs.HackingScene;

                                        //if ( !InputCaching.ShouldKeepDoingAction )
                                        //    unit.SetTargetingMode( null, null );
                                    }
                                }
                            }
                        }
                        break;
                    #endregion
                    default:
                        ArcenDebugging.LogSingleLine( "BasicAndroidAbilities: Called HandleAbilityMixedTargeting for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BasicAndroidAbilities.HandleAbilityMixedTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError );
            }
        }
    }
}

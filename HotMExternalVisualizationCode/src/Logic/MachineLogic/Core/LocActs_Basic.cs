using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class LocActs_Basic : IMachineActorLocationActionImplementation
    {
        public ActionResult TryHandleLocationAction( ISimMachineActor ActorOrNull, ISimBuilding BuildingOrNull, Vector3 ActionLocation, 
            LocationActionType Action, NPCEvent EventOrNull, ProjectOutcomeStreetSenseItem ProjectStreetOrNull,
            string OtherOptionalID, ActionLogic Logic, out bool IncludeRestrictedAreaNoticeInTooltip, int ExtraMentalEnergyCostFromSprinting )
        {
            if ( Action == null )
            {
                IncludeRestrictedAreaNoticeInTooltip = false;
                return ActionResult.Blocked;
            }

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Action.ID )
            {
                case "MoveToPosition":
                case "MechMove":
                    #region MoveToPosition and MechMove
                    {
                        if ( ActorOrNull == null )
                        {
                            IncludeRestrictedAreaNoticeInTooltip = false;
                            return ActionResult.Blocked;
                        }
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        if ( Logic == ActionLogic.CalculateIfBlocked )
                        {
                            IncludeRestrictedAreaNoticeInTooltip = true;
                            return ActionResult.Success;
                        }
                        if ( Logic == ActionLogic.PredictIcons )
                            return ActionResult.Success; // we don't do icons for this one, which is unusual but this is not at a building typically (if you sprint it might be)

                        int debugStage = 0;
                        try
                        {
                            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( ActionLocation );
                            MapTile tile = cell?.ParentTile;
                            if ( tile == null )
                                return ActionResult.Success; //invalid location I guess?
                            debugStage = 100;
                            MapPOI restrictedPOI = BuildingOrNull != null ? BuildingOrNull.CalculateLocationPOI() : cell.CalculateAreCoordinatesInRestrictedPOI( ActionLocation );
                            ActorBadge wouldBecomeOutcast = BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, restrictedPOI, BuildingOrNull );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return ActionResult.Success;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        //this does not render a tooltip
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "MoveToPosition/MechMove", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "NoOp":
                    {
                        //do nothing
                        IncludeRestrictedAreaNoticeInTooltip = true;
                    }
                    break;
                case "MoveToBuilding":
                    #region MoveToBuilding
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = true;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ActorBadge wouldBecomeOutcast = BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, BuildingOrNull );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    { }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        //this does not render a tooltip
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "MoveToBuilding", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "StealVehicle":
                    #region StealVehicle
                    IncludeRestrictedAreaNoticeInTooltip = true;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ActorBadge wouldBecomeOutcast = BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, BuildingOrNull );
                            BasicActionsHelper.Calc_StealVehicle( BuildingOrNull, out MachineVehicleType claimVehicleType );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    if ( SimCommon.TotalCapacityUsed_Vehicles >= MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt || 
                                        FlagRefs.VehicularSecurityPatch.DuringGameplay_HasEverCompleted )
                                        return ActionResult.Blocked;
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        if ( claimVehicleType != null )
                                        {
                                            RenderManager_Streets.RenderFrameStyleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(),
                                                claimVehicleType.TooltipIcon, IconRefs.Act0VehicleCapture,
                                                RenderManager.FramesPrepped );
                                        }
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 200;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            if ( wouldBecomeOutcast != null )
                                            {
                                                //outcast
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ColorTheme.RedOrange3 );
                                            }

                                            if ( !canAffordEnergy )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                            if ( !canAffordAP )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                            //Vehicle
                                            if ( claimVehicleType != null )
                                            {
                                                novel.Main.AddSpriteStyled_NoIndent( claimVehicleType.ShapeIcon,
                                                    AdjustedSpriteStyle.InlineLarger1_2 )
                                                    .AddRaw( claimVehicleType.GetDisplayName() ).Line();
                                            }

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        //Vehicle
                                        if ( claimVehicleType != null )
                                        {
                                            novel.Main.AddSpriteStyled_NoIndent( claimVehicleType.ShapeIcon,
                                                AdjustedSpriteStyle.InlineLarger1_2 )
                                                .AddRaw( claimVehicleType.GetDisplayName() ).Line();
                                        }

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        //then outcast
                                        if ( wouldBecomeOutcast != null )
                                        {
                                            novel.Main.AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange3 )
                                                .AddLangAndAfterLineItemHeader( "Unit_WillBecomeOutcast", ColorTheme.RedOrange3 ).Line();
                                            novel.Main.AddLang( "Unit_WillBecomeOutcast_StrategyTip", ColorTheme.PurpleDim ).Line();
                                        }

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;
                                    {
                                        if ( claimVehicleType != null )
                                        {
                                            MapItem item = BuildingOrNull.GetMapItem();
                                            Vector3 aboveBuilding = item.TopCenterPoint.ReplaceY( 0 ); //make the vehicle appear at its proper height, because it will jump upwards

                                            ISimMachineVehicle newVehicle = World.Forces.TryCreateNewMachineVehicleAsCloseAsPossibleToLocation( aboveBuilding,
                                                item.ParentCell, claimVehicleType, string.Empty, CellRange.CellAndAdjacent2x, 
                                                BasicActionsHelper.InitializeWorkingRandToBuildingOnly( BuildingOrNull ), true, CollisionRule.Strict, true );

                                            if ( newVehicle == null )
                                                return ActionResult.Blocked;
                                            newVehicle.CurrentRegistration = CommonRefs.DeliveryServiceTag.CohortsList.GetRandom( Engine_Universal.PermanentQualityRandom );

                                            ArcenNotes.SendSimpleNoteToGameOnly( 200, NoteInstructionTable.Instance.GetRowByID( "StoleMachineVehicle" ), NoteStyle.StoredGame,
                                                claimVehicleType.ID, string.Empty, string.Empty, string.Empty, newVehicle.ActorID, 0, 0, newVehicle.GetDisplayName(), string.Empty, string.Empty, 0 );

                                            if ( !FlagRefs.HasStolenADeliveryCraft.DuringGameplay_IsTripped )
                                            {
                                                FlagRefs.HasStolenADeliveryCraft.TripIfNeeded();
                                                FlagRefs.VehicularSecurityPatch.DuringGameplay_StartNowIfNeeded();

                                                FlagRefs.FirstFlight.ForGame_HasBeenUnlocked = true;
                                                ArcenMusicPlayer.Instance.CurrentOneShotOverridingMusicTrackPlaying = FlagRefs.FirstFlight;
                                            }
                                        }
                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "StealVehicle", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "RecruitAndroid":
                    #region RecruitAndroid
                    IncludeRestrictedAreaNoticeInTooltip = true;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ActorBadge wouldBecomeOutcast = BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, BuildingOrNull );
                            MachineUnitType claimUnitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( OtherOptionalID );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    if ( BasicActionsHelper.Calc_IsBlockedFromRecruitingMoreAndroids( claimUnitType ) )
                                        return ActionResult.Blocked;
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        if ( claimUnitType != null )
                                        {
                                            RenderManager_Streets.RenderFrameStyleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(), claimUnitType.TooltipIcon,
                                                BasicActionsHelper.Calc_IsBlockedFromRecruitingMoreAndroids( claimUnitType ) ? IconRefs.Act0UnitCaptureBlocked : IconRefs.Act0UnitCapture,
                                                RenderManager.FramesPrepped );
                                        }
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        novel.Icon = claimUnitType.ShapeIcon;
                                        //novel.TitleUpperLeft.Clear();
                                        novel.TitleUpperLeft.AfterLineItemHeader().AddRaw( claimUnitType.GetDisplayName() );

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        debugStage = 2100;
                                        if ( BasicActionsHelper.Calc_IsBlockedFromRecruitingMoreAndroids( claimUnitType ) )
                                        {
                                            if ( novel.TooltipWidth < 250 )
                                                novel.TooltipWidth = 250;

                                            novel.Main.AddFormat4( "RecruitAndroid_Blocked",
                                                World.Forces.GetMachineUnitsByID().Count, 
                                                SimCommon.TotalCapacityUsed_Androids, MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt,
                                                claimUnitType.UnitCapacityCost,
                                                ColorTheme.RedOrange2 ).Line();
                                            return ActionResult.Blocked;
                                        }

                                        debugStage = 3100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            if ( wouldBecomeOutcast != null )
                                            {
                                                //outcast
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ColorTheme.RedOrange3 );
                                            }

                                            if ( !canAffordEnergy )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                            if ( !canAffordAP )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        debugStage = 5100;
                                        if ( claimUnitType != null )
                                        {
                                            debugStage = 5200;

                                            novel.Main.AddBoldRawAndAfterLineItemHeader( claimUnitType.GetDisplayName(), ColorTheme.DataLabelWhite )
                                                .AddRaw( claimUnitType.GetDescription() ).Line();
                                        }

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 6100;

                                        //then outcast
                                        if ( wouldBecomeOutcast != null )
                                        {
                                            novel.Main.AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange3 )
                                                .AddLangAndAfterLineItemHeader( "Unit_WillBecomeOutcast", ColorTheme.RedOrange3 ).Line();
                                            novel.Main.AddLang( "Unit_WillBecomeOutcast_StrategyTip", ColorTheme.PurpleDim ).Line();
                                        }

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;
                                    {
                                        if ( BasicActionsHelper.Calc_IsBlockedFromRecruitingMoreAndroids( claimUnitType ) )
                                            return ActionResult.Blocked;

                                        if ( claimUnitType != null )
                                        {
                                            ISimMachineUnit newUnit = ActorOrNull.TryCreateNewMachineUnitAsCloseAsPossibleToBuilding( BuildingOrNull, claimUnitType, 
                                                string.Empty, CellRange.CellAndAdjacent2x,
                                                BasicActionsHelper.InitializeWorkingRandToBuildingOnly( BuildingOrNull ), true, CollisionRule.Strict, true ); //have them immediately able to act
                                            if ( newUnit == null )
                                                return ActionResult.Blocked;
                                            newUnit.CurrentRegistration = CommonRefs.CorporationPropertyOwnerTag.CohortsList.GetRandom( Engine_Universal.PermanentQualityRandom );

                                            ArcenNotes.SendSimpleNoteToGameOnly( 200, NoteInstructionTable.Instance.GetRowByID( "ClaimMachineUnit" ), NoteStyle.StoredGame,
                                                claimUnitType.ID, string.Empty, string.Empty, string.Empty, newUnit.ActorID, 0, 0, newUnit.GetDisplayName(), string.Empty, string.Empty, 0 );
                                        }
                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );

                                        CityStatisticTable.AlterScore( "AndroidsErased", 1 );

                                        //these intermediate increments are just to make sure it does not calculate wrong due to time delay
                                        SimCommon.TotalOnline_Androids++;
                                        SimCommon.TotalCapacityUsed_Androids += claimUnitType.UnitCapacityCost;
                                        //and then this is to force a recalculation
                                        SimCommon.MarkLocationActionRecalculationsAsNeeded();
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "RecruitAndroid", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "ColdBlood":
                    #region ColdBlood
                    IncludeRestrictedAreaNoticeInTooltip = true;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ActorBadge wouldBecomeOutcast = BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, BuildingOrNull );
                            int murderMin = 1;
                            int murderMax = 5;
                            if ( FlagRefs.GaveUpColdBlood.DuringGameplay_IsTripped )
                                return ActionResult.Blocked;

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderSimpleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(),
                                                RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string killColor = ColorTheme.RedLess;
                                        string expGainColor = ColorTheme.HeaderGoldMoreRich;

                                        debugStage = 3100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        //kill count
                                        novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( IconRefs.MurderCount.Icon, AdjustedSpriteStyle.InlineLarger1_2, killColor )
                                            .AddFormat2( "PositiveChangeRange", murderMin, murderMax, killColor );

                                        //Energy gain
                                        novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, expGainColor )
                                            .AddFormat2( "PositiveChangeRange", murderMin, murderMax, expGainColor );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            if ( wouldBecomeOutcast != null )
                                            {
                                                //outcast
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ColorTheme.RedOrange3 );
                                            }

                                            if ( !canAffordEnergy )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                            if ( !canAffordAP )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }


                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        debugStage = 4100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5100;
                                        {
                                            debugStage = 5200;
                                            //kill count
                                            novel.Main.AddSpriteStyled_NoIndent( IconRefs.MurderCount.Icon, AdjustedSpriteStyle.InlineLarger1_2, killColor )
                                                .AddLangAndAfterLineItemHeader( "Kills", killColor )
                                                .AddFormat2( "PositiveChangeRange", murderMin, murderMax, killColor );
                                            novel.Main.Line();
                                        }

                                        {
                                            //energy gain
                                            novel.Main.AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, expGainColor )
                                                .AddFormat2( "PositiveChangeRange", murderMin, murderMax, expGainColor );
                                            novel.Main.Line();
                                        }

                                        debugStage = 6100;

                                        //then outcast
                                        if ( wouldBecomeOutcast != null )
                                        {
                                            novel.Main.AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange3 )
                                                .AddLangAndAfterLineItemHeader( "Unit_WillBecomeOutcast", ColorTheme.RedOrange3 ).Line();
                                            novel.Main.AddLang( "Unit_WillBecomeOutcast_StrategyTip", ColorTheme.PurpleDim ).Line();
                                        }

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;
                                    {
                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );

                                        int murderCount = BasicActionsHelper.Calc_RandBuildingAndUnitIntInclusive( BuildingOrNull, ActorOrNull, murderMin, murderMax );

                                        ArcenNotes.SendSimpleNoteToGameOnly( 200, NoteInstructionTable.Instance.GetRowByID( "ColdBlood" ), NoteStyle.StoredGame,
                                            ActorOrNull.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, ActorOrNull.SortID, 0, 0, ActorOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );

                                        ResourceRefs.MentalEnergy.AlterCurrent_Named( murderCount, "Increase_ColdBlood", ResourceAddRule.IgnoreUntilTurnChange );
                                        CityStatisticTable.AlterScore( "Murders", murderCount );

                                        Action.DuringGame_BlockedUntilTurn = SimCommon.Turn + Engine_Universal.PermanentQualityRandom.NextInclus( 1, 3 );


                                        ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                        if ( buffer != null )
                                        {
                                            string killColor = ColorTheme.RedLess;
                                            string expGainColor = ColorTheme.HeaderGoldMoreRich;

                                            //kill count
                                            buffer.AddSpriteStyled_NoIndent( IconRefs.MurderCount.Icon, AdjustedSpriteStyle.InlineLarger1_2, killColor )
                                                .AddRaw( murderCount.ToString(), killColor );

                                            //Energy gain
                                            buffer.Space3x().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, expGainColor )
                                                .AddRaw( murderCount.ToString(), expGainColor );

                                            Vector3 startLocation = ActorOrNull.GetPositionForCollisions().PlusY( + 1f );
                                            Vector3 endLocation = startLocation.PlusY( ActorOrNull.GetHalfHeightForCollisions() + 0.2f );

                                            DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                            newDamageText.PhysicalDamageIncluded = 0;
                                            newDamageText.MoraleDamageIncluded = 0;
                                            newDamageText.SquadDeathsIncluded = 0;
                                        }
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "ColdBlood", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "MurderAndroidForRegistration":
                    #region MurderAndroidForRegistration
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            int turnCount = 2;
                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderSimpleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(),
                                                RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string turnsColor = ColorTheme.RustLighter;

                                        debugStage = 3100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        //turn count
                                        novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                            .AddRaw( turnCount.ToString(), turnsColor );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;


                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.None, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5100;
                                        {
                                            debugStage = 5200;
                                            //turn count
                                            novel.Main.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                                .AddLangAndAfterLineItemHeader( "Turns", turnsColor )
                                                .AddRaw( turnCount.ToString(), turnsColor );
                                            novel.Main.Line();
                                        }

                                        debugStage = 6100;

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;
                                    {
                                        debugStage = 7100;

                                        ActionOverTime action = ActionOverTime.TryToCreate( "MurderAndroidForRegistration", ActorOrNull, 0, BuildingOrNull, 
                                            ActionOverTimeStart.OkayIfActorBlockedFromActing ); //because we may have used our last AP just to get here
                                        if ( action == null )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }
                                        debugStage = 7200;
                                        action.RelatedAbilityOrNull = null;
                                        debugStage = 7300;
                                        action.SetIntData( "Target1", turnCount );
                                        ActorOrNull.SetTargetingMode( null, null );

                                        debugStage = 7500;

                                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "MurderAndroidForRegistrationStarted" ), NoteStyle.StoredGame,
                                            ActorOrNull.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, ActorOrNull.SortID, 0, 0, ActorOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "MurderAndroidForRegistration", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "Wiretap":
                    #region Wiretap
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            int turnCount = 7;
                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderSimpleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(),
                                                RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string turnsColor = ColorTheme.RustLighter;

                                        debugStage = 3100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        //turn count
                                        novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                            .AddRaw( turnCount.ToString(), turnsColor );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.None, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5100;
                                        {
                                            debugStage = 5200;
                                            //turn count
                                            novel.Main.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                                .AddLangAndAfterLineItemHeader( "Turns", turnsColor )
                                                .AddRaw( turnCount.ToString(), turnsColor );
                                            novel.Main.Line();
                                        }

                                        debugStage = 6100;

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;

                                    {
                                        debugStage = 7100;

                                        ActionOverTime action = ActionOverTime.TryToCreate( "Wiretap", ActorOrNull, 0, BuildingOrNull,
                                            ActionOverTimeStart.OkayIfActorBlockedFromActing ); //because we may have used our last AP just to get here
                                        if ( action == null )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }
                                        debugStage = 7200;
                                        action.RelatedAbilityOrNull = null;
                                        debugStage = 7300;
                                        action.SetIntData( "Target1", turnCount );
                                        ActorOrNull.SetTargetingMode( null, null );

                                        debugStage = 7500;

                                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "WiretapStarted" ), NoteStyle.StoredGame,
                                            ActorOrNull.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, ActorOrNull.SortID, 0, 0, ActorOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );

                                        Action.DuringGame_BlockedUntilTurn = SimCommon.Turn + Engine_Universal.PermanentQualityRandom.NextInclus( 4, 9 );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "MurderAndroidForRegistration", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "HideAndSelfRepair":
                    #region HideAndSelfRepair
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            int turnCount = 2;
                            int lostAmount = 0;
                            int current = 0;
                            int max = 0;
                            if ( ActorOrNull != null )
                            {
                                MapActorData healthData = ActorOrNull.GetActorDataData( ActorRefs.ActorHP, false );
                                lostAmount = healthData.LostFromMax;
                                current = healthData.Current;
                                max = healthData.Maximum;
                            }
                            int engineeringSkill = ActorOrNull == null ? 0 : ActorOrNull.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true );
                            int healthRepair = engineeringSkill * turnCount;
                            healthRepair = Math.Min( lostAmount, healthRepair );

                            int finalHealth = healthRepair + current;
                            int percentageHealthAfter = MathA.IntPercentageClamped( finalHealth, max, 0, 100 );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    if ( lostAmount <= 0 )
                                        return ActionResult.Blocked; //no reason to do this
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderSimpleStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(),
                                                RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string turnsColor = ColorTheme.RustLighter;

                                        debugStage = 3100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        //turn count
                                        novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                            .AddRaw( turnCount.ToString(), turnsColor );

                                        //health repaired
                                        novel.TitleUpperLeft.Space3x().AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.SkillPaleGreen )
                                            .AddRaw( healthRepair.ToStringThousandsWhole(), ColorTheme.SkillPaleGreen )
                                            .Space1x().AddFormat1( LangCommon.Parenthetical, percentageHealthAfter.ToStringIntPercent(), ColorTheme.SkillPaleGreen );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.Linebreak );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5100;
                                        {
                                            debugStage = 5200;
                                            //turn count
                                            novel.Main.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                                .AddLangAndAfterLineItemHeader( "Turns", turnsColor )
                                                .AddRaw( turnCount.ToString(), turnsColor );
                                            novel.Main.Line();
                                        }

                                        debugStage = 6100;

                                        novel.Main.AddRawAndAfterLineItemHeader( ActorRefs.ActorEngineeringSkill.GetDisplayName(), ColorTheme.HeaderLighterBlue )
                                            .AddSpriteStyled_NoIndent( ActorRefs.ActorEngineeringSkill.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                            .AddRaw( engineeringSkill.ToStringThousandsWhole() ).Line();
                                        novel.Main.AddLangAndAfterLineItemHeader( "HealthThatWillBeRestored", ColorTheme.HeaderLighterBlue )
                                            .AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.SkillPaleGreen )
                                            .AddRaw( healthRepair.ToStringThousandsWhole(), ColorTheme.SkillPaleGreen )
                                            .Space1x().AddFormat1( LangCommon.Parenthetical, percentageHealthAfter.ToStringIntPercent(), ColorTheme.SkillPaleGreen ).Line();

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;
                                    if ( lostAmount <= 0 )
                                        return ActionResult.Blocked; //no reason to do this

                                    {
                                        debugStage = 7100;

                                        ActionOverTime action = ActionOverTime.TryToCreate( "HideAndSelfRepair", ActorOrNull, 0, BuildingOrNull,
                                            ActionOverTimeStart.OkayIfActorBlockedFromActing ); //because we may have used our last AP just to get here
                                        if ( action == null )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }
                                        debugStage = 7200;
                                        action.RelatedAbilityOrNull = null;
                                        debugStage = 7300;
                                        action.SetIntData( "Target1", turnCount );
                                        action.SetIntData( "Quantity", engineeringSkill ); //the amount per turn
                                        ActorOrNull.SetTargetingMode( null, null );

                                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "GeneralActionOverTimeStarted" ), NoteStyle.StoredGame,
                                            ActorOrNull.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, ActorOrNull.SortID,
                                            ActorOrNull.GetActorDataCurrent( ActorRefs.ActorHP, true ), 0, ActorOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );

                                        debugStage = 7500;
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "HideAndSelfRepair", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "MinorEvent":
                    #region MinorEvent
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        NPCEvent minorEvent = EventOrNull;
                        if ( minorEvent == null|| minorEvent.MinorData == null ) //could not find the minor event, or no minor event specified!
                            return ActionResult.Blocked;
                        if ( minorEvent.GateByCity != null && !minorEvent.GateByCity.CalculateMeetsPrerequisites( false ) )
                            return ActionResult.Blocked; //this happens when something just got blocked, but the fact that it was blocked is calculated on a bg thread after the aggregate calculations happened.

                        if ( FlagRefs.Ch0_DejaVu.DuringGameplay_IsWaitingNow )
                            return ActionResult.Blocked; //hide them entirely during this

                        if ( !minorEvent.MinorData.GetIsPotentialBuildingStillValid( BuildingOrNull ) )
                            return ActionResult.Blocked; //something is blocking it

                        bool isBlockedByClearance = false;
                        int clearanceRequired = 0;
                        int actorHasClearance = 0;
                        if ( minorEvent.EventIsBlockedForActorsWithoutOverridingClearance > 0 && ActorOrNull != null )
                        {
                            clearanceRequired = minorEvent.EventIsBlockedForActorsWithoutOverridingClearance;
                            ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            isBlockedByClearance = clearanceRequired > actorHasClearance;
                        }
                        else if ( minorEvent.EventIsBlockedForActorsWithoutClearance && ActorOrNull != null )
                        {
                            clearanceRequired = BuildingOrNull.CalculateLocationSecurityClearanceInt();
                            ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            isBlockedByClearance = clearanceRequired > actorHasClearance;
                        }

                        bool isBlockedByOtherEventStats = false;
                        if ( ActorOrNull != null )
                            isBlockedByOtherEventStats = !minorEvent.Event_CalculateMeetsPrerequisites( ActorOrNull, GateByLogicCheckType.ActorSpecific, EventCheckReason.StandardSeeding, false );

                        bool isBlockedByActor = false;
                        if ( ActorOrNull != null && !minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, null ) )
                            isBlockedByActor = true;

                        bool isBlockedAtAll = isBlockedByClearance || isBlockedByActor || isBlockedByOtherEventStats;

                        int debugStage = 0;
                        try
                        {
                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return isBlockedAtAll ? ActionResult.Blocked : ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderMinorEventStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(), minorEvent, isBlockedAtAll,
                                                RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 200;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( isBlockedByClearance && !HandbookRefs.ClearancesAreContextual.Meta_HasBeenUnlocked )
                                            HandbookRefs.ClearancesAreContextual.DuringGame_UnlockIfNeeded( true );

                                        NPCCohort cohort = minorEvent.MinorData?.CalculateCohort( BuildingOrNull );
                                        if ( cohort != null )
                                            cohort.DuringGame_DiscoverIfNeed();
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedByActor )
                                            novel.ShouldTooltipBeRed = true;


                                        novel.Icon = minorEvent.Icon;
                                        //novel.IconColorHex = minorEvent?.MinorData?.ColorHex ?? string.Empty;
                                        novel.TitleUpperLeft.Clear();

                                        novel.TitleUpperLeft.AddRaw( minorEvent.GetDisplayName() );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {                                            
                                            if ( isBlockedByClearance )
                                                novel.MainHeader.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", ColorTheme.RedOrange2 )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();

                                            novel.MainHeader.StartStyleLineHeightA();

                                            minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, novel.MainHeader );

                                            if ( cohort != null )
                                            {
                                                //related cohort
                                                novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "RelatedTo", ColorTheme.DataLabelWhite )
                                                    .AddRaw( cohort.GetDisplayName() ).Line();
                                            }

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.None, TTTextAfter.Linebreak );

                                            //Building name if not restricted (if restricted, was already shown
                                            if ( !CombatTextHelper.GetIsMovingToRestrictedArea( ActorOrNull, BuildingOrNull, ActionLocation, false ) )
                                            {
                                                //Building name
                                                novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                                    .AddRaw( BuildingOrNull.GetDisplayName() ).Line();
                                            }

                                            if ( SimMetagame.CurrentChapterNumber <= 0 )
                                                novel.MainHeader.AddRightClickFormat( "ClickToInteractWithEvent_Brief", ColorTheme.SoftGold ).Line();

                                            novel.MainHeader.EndLineHeight();

                                            if ( minorEvent.MinorData?.ShouldAlwaysShowExpandedActionDetails??false)
                                            {
                                                if ( novel.TooltipWidth < 300 )
                                                    novel.TooltipWidth = 300;

                                                novel.FrameTitle.Clear();
                                                novel.FrameBody.Clear();
                                                novel.FrameTitle.AddLang( "Move_ActionDetails" );

                                                if ( !minorEvent.GetDescription().IsEmpty() )
                                                    novel.FrameBody.AddRaw( minorEvent.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                                if ( !minorEvent.StrategyTip.Text.IsEmpty() )
                                                    novel.FrameBody.AddRaw( minorEvent.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                            }

                                            return ActionResult.Success;
                                        }

                                        //minor event
                                        {
                                            if ( isBlockedByClearance )
                                                novel.Main.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", ColorTheme.RedOrange2 )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();
                                            
                                            minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, novel.Main );

                                            novel.FrameTitle.Clear();
                                            novel.FrameBody.Clear();
                                            novel.FrameTitle.AddLang( "Move_ActionDetails" );

                                            if ( !minorEvent.GetDescription().IsEmpty() )
                                                novel.FrameBody.AddRaw( minorEvent.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                            if ( !minorEvent.StrategyTip.Text.IsEmpty() )
                                                novel.FrameBody.AddRaw( minorEvent.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                        }

                                        novel.Main.StartStyleLineHeightA();

                                        if ( cohort != null )
                                        {
                                            //related cohort
                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "RelatedTo", ColorTheme.DataLabelWhite )
                                                .AddRaw( cohort.GetDisplayName() ).Line();
                                        }

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );

                                        if ( SimMetagame.CurrentChapterNumber <= 0 )
                                            novel.Main.AddRightClickFormat( "ClickToInteractWithEvent_Brief", ColorTheme.SoftGold ).Line();

                                        novel.Main.EndLineHeight();
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;

                                    {
                                        NPCCohort cohort = minorEvent.MinorData?.CalculateCohort( BuildingOrNull );
                                        if ( cohort != null )
                                            cohort.DuringGame_DiscoverIfNeed();
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedAtAll )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }

                                        if ( isBlockedByActor )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }

                                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        {
                                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                                            VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( BuildingOrNull.GetPositionForCameraFocus(), true );
                                        }

                                        //START_MINOR_EVENT
                                        minorEvent.ClearForMinorEvent();
                                        MinorEventHandler.TryStart( ActorOrNull, minorEvent, BuildingOrNull, cohort, true );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "MinorEvent", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "ProjectStreetSenseAction":
                    #region ProjectStreetSenseAction
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        ProjectOutcomeStreetSenseItem streetItem = ProjectStreetOrNull;
                        if ( streetItem == null ) //could not find the StreetSense item, or none specified
                            return ActionResult.Blocked;
                        if ( !streetItem.DuringGame_CanStillDo() )
                            return ActionResult.Blocked; //this happens when something just expired

                        bool isBlockedByClearance = false;
                        int clearanceRequired = 0;
                        int actorHasClearance = 0;
                        if ( streetItem.BlockedForActorsWithoutClearance >= 0 && ActorOrNull != null )
                        {
                            clearanceRequired = streetItem.BlockedForActorsWithoutClearance;
                            ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            isBlockedByClearance = clearanceRequired > actorHasClearance;
                        }

                        bool isBlockedByActor = false;
                        if ( ActorOrNull != null && !streetItem.DuringGameplay_StreetActionCanBeDoneByActor( ActorOrNull, null, false ) )
                            isBlockedByActor = true;

                        bool isBlockedAtAll = isBlockedByClearance || isBlockedByActor;

                        int debugStage = 0;
                        try
                        {
                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return isBlockedAtAll ? ActionResult.Blocked : ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        RenderManager_Streets.RenderProjectStreetItemStreetSenseIconAtBuilding( Action, BuildingOrNull.GetMapItem(), streetItem, isBlockedAtAll,
                                            RenderManager.FramesPrepped );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 200;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || (ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting);
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints ?? 1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( isBlockedByClearance && !HandbookRefs.ClearancesAreContextual.Meta_HasBeenUnlocked )
                                            HandbookRefs.ClearancesAreContextual.DuringGame_UnlockIfNeeded( true );

                                        //NPCCohort cohort = streetItem.MinorData?.CalculateCohort( BuildingOrNull );
                                        //if ( cohort != null )
                                        //    cohort.DuringGame_DiscoverIfNeed();
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        novel.Icon = streetItem.Icon;
                                        //novel.IconColorHex = minorEvent?.MinorData?.ColorHex ?? string.Empty;
                                        novel.TitleUpperLeft.Clear();

                                        novel.TitleUpperLeft.AddRaw( streetItem.DisplayName.Text);

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            if ( isBlockedByClearance )
                                                novel.MainHeader.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", ColorTheme.RedOrange2 )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();

                                            novel.MainHeader.StartStyleLineHeightA();

                                            novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "RelatedToProject", ColorTheme.DataLabelWhite )
                                                .AddRaw( streetItem.ParentProject?.GetDisplayName() ?? "???", ColorTheme.DataBlue ).Line();
                                            streetItem.DuringGameplay_StreetActionCanBeDoneByActor( ActorOrNull, novel.MainHeader, false );
                                            streetItem.DuringGameplay_WriteCostsAndGains( novel.MainHeader, true, false );

                                            //if ( cohort != null )
                                            //{
                                            //    //related cohort
                                            //    novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "RelatedTo", ColorTheme.DataLabelWhite )
                                            //        .AddRaw( cohort.GetDisplayName() ).Line();
                                            //}

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.MainHeader, TTTextBefore.None, TTTextAfter.Linebreak );

                                            ////Building name if not restricted (if restricted, was already shown
                                            //if ( !CombatTextHelper.GetIsMovingToRestrictedArea( ActorOrNull, BuildingOrNull, ActionLocation, true ) )
                                            //{
                                            //    //Building name
                                            //    novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            //        .AddRaw( BuildingOrNull.GetDisplayName() ).Line();
                                            //}

                                            novel.MainHeader.EndLineHeight();

                                            if ( streetItem.ShouldAlwaysShowExpandedActionDetails )
                                            {
                                                if ( novel.TooltipWidth < 300 )
                                                    novel.TooltipWidth = 300;

                                                novel.FrameTitle.Clear();
                                                novel.FrameBody.Clear();
                                                //novel.FrameTitle.AddLang( "Move_ActionDetails" );

                                                if ( streetItem.MaxTimesCanDo > 0 )
                                                {
                                                    novel.FrameTitle.AddLangAndAfterLineItemHeader( "RemainingTimesCanBeDone" )
                                                        .AddRaw( MathA.Max( 0, streetItem.MaxTimesCanDo - streetItem.DuringGameplay_TimesHasBeenDone ).ToString(), ColorTheme.DataBlue );
                                                }
                                                else
                                                    novel.FrameTitle.AddLang( "CanBeDoneRepeatedly" );

                                                if ( !streetItem.Description.Text.IsEmpty() )
                                                    novel.FrameBody.AddRaw( streetItem.Description.Text, ColorTheme.NarrativeColor ).Line();
                                                if ( !streetItem.StrategyTip.Text.IsEmpty() )
                                                    novel.FrameBody.AddRaw( streetItem.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                            }

                                            return ActionResult.Success;
                                        }

                                        novel.Main.StartStyleLineHeightA();

                                        //street action
                                        {
                                            if ( isBlockedByClearance )
                                                novel.Main.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", ColorTheme.RedOrange2 )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();

                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "RelatedToProject", ColorTheme.DataLabelWhite )
                                                .AddRaw( streetItem.ParentProject?.GetDisplayName() ?? "???", ColorTheme.DataBlue ).Line();
                                            streetItem.DuringGameplay_StreetActionCanBeDoneByActor( ActorOrNull, novel.Main, true );
                                            streetItem.DuringGameplay_WriteCostsAndGains( novel.Main, true, true );

                                            novel.FrameTitle.Clear();
                                            novel.FrameBody.Clear();
                                            //novel.FrameTitle.AddLang( "Move_ActionDetails" );

                                            if ( streetItem.MaxTimesCanDo > 0 )
                                            {
                                                novel.FrameTitle.AddLangAndAfterLineItemHeader( "RemainingTimesCanBeDone" )
                                                    .AddRaw( MathA.Max( 0, streetItem.MaxTimesCanDo - streetItem.DuringGameplay_TimesHasBeenDone ).ToString(), ColorTheme.DataBlue )
                                                    .Line();
                                                novel.FrameBody.StartSize80().AddLang( "RemainingTimesCanBeDone_Explanation", ColorTheme.NarrativeColor ).EndSize().Line();
                                            }
                                            else
                                                novel.FrameTitle.AddLang( "CanBeDoneRepeatedly" );

                                            
                                            if ( !streetItem.Description.Text.IsEmpty() )
                                                novel.FrameBody.AddRaw( streetItem.Description.Text, ColorTheme.NarrativeColor ).Line();
                                            if ( !streetItem.StrategyTip.Text.IsEmpty() )
                                                novel.FrameBody.AddRaw( streetItem.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                        }

                                        //if ( cohort != null )
                                        //{
                                        //    //related cohort
                                        //    novel.Main.AddBoldLangAndAfterLineItemHeader( "RelatedTo", ColorTheme.DataLabelWhite )
                                        //        .AddRaw( cohort.GetDisplayName() ).Line();
                                        //}

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );

                                        novel.Main.EndLineHeight();
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    if ( ActorOrNull == null )
                                        return ActionResult.Blocked;

                                    {
                                        //NPCCohort cohort = streetItem.MinorData?.CalculateCohort( BuildingOrNull );
                                        //if ( cohort != null )
                                        //    cohort.DuringGame_DiscoverIfNeed();
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedAtAll )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        if ( streetItem.TryHaveUnitDoThis_MainThreadOnly( ActorOrNull, BuildingOrNull ) )
                                        {
                                            if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                            {
                                                Engine_HotM.SetGameMode( MainGameMode.Streets );
                                                VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( BuildingOrNull.GetPositionForCameraFocus(), true );
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "ProjectStreetSenseAction", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "AndroidInvestigate":
                    #region AndroidInvestigate
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            Investigation investigation = SimCommon.GetEffectiveCurrentInvestigation();
                            if ( investigation == null || investigation.Type == null )
                                return ActionResult.Blocked;

                            InvestigationType investType = investigation.Type;
                            ISimMachineUnit actorAsUnit = ActorOrNull as ISimMachineUnit;
                            if ( investigation == null || BuildingOrNull == null || !investigation.PossibleBuildings.ContainsKey( BuildingOrNull ) || investType == null )
                                return ActionResult.Blocked;

                            if ( Logic != ActionLogic.Execute )
                            {
                                if ( BuildingOrNull.GetAreMoreUnitsBlockedFromComingHere() )
                                {
                                    if ( actorAsUnit != null && BuildingOrNull.CurrentOccupyingUnit == actorAsUnit )
                                    { } //this is okay, we are at this building
                                    else
                                        return ActionResult.Blocked;
                                }
                            }

                            bool isInvasiveNotInfiltration = investigation.Type.Style.IsEveryPossibilityAValidChoice;
                            bool isInfiltration = investigation.Type.Style.IsInfiltration;
                            if ( isInfiltration )
                                isInvasiveNotInfiltration = false;
                            ActorBadge wouldBecomeOutcast = isInfiltration ? null : 
                                (isInvasiveNotInfiltration ? CommonRefs.MarkedDefective : BasicActionsHelper.CalcEffectiveActorWouldBecomeOutcast( ActorOrNull, BuildingOrNull ) );

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    if ( investType.DuringGame_IsBlockedFromInvestigatingAny() || !investigation.GetCanActorDoThisInvestigation( ActorOrNull ) )
                                        return ActionResult.Blocked;
                                    return ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        //RenderManager_Streets.DrawMapItemHighlightedBorder( BuildingOrNull.GetMapItem(),ColorRefs.BuildingPartOfInvestigation.ColorHDR, Engine_HotM.GameMode == MainGameMode.CityMap );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;

                                        novel.CanExpand = CanExpandType.Brief;

                                        debugStage = 3100;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( investType.DuringGame_IsBlockedFromInvestigatingAny() )
                                        {
                                            novel.TitleUpperLeft.Clear();
                                            novel.TitleUpperLeft.AddLang( "InvestigationBlocked" );


                                            if ( !canAffordEnergy )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                            if ( !canAffordAP )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                            if ( investType.DuringGame_IsBlockedFromInvestigatingGroupA() )
                                                novel.Main.AddLang( investType.BlockedByProjectGroupALangKey ).Line();
                                            else
                                                novel.Main.AddLang( investType.BlockedByProjectGroupBLangKey ).Line();

                                            return ActionResult.Success;
                                        }
                                        if ( !investigation.GetCanActorDoThisInvestigation( ActorOrNull ) )
                                        {
                                            if ( novel.TooltipWidth < 250 )
                                                novel.TooltipWidth = 250;

                                            novel.TitleUpperLeft.Clear(); //just show the name of the investigation
                                            novel.TitleUpperLeft.AddRaw( ActorOrNull.GetDisplayName() );
                                            investigation.AppendInvestigatorRequirements( novel.Main, ActorOrNull, true );

                                            if ( !canAffordEnergy )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                            if ( !canAffordAP )
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                            if ( InputCaching.ShouldShowDetailedTooltips )
                                            {
                                                int countSoFar = 0;
                                                foreach ( ISimMachineActor otherActor in SimCommon.AllMachineActors.GetDisplayList() )
                                                {
                                                    if ( otherActor is ISimMachineUnit otherUnit && otherUnit.UnitType.IsConsideredAndroid )
                                                    {
                                                        if ( investigation.GetCanActorDoThisInvestigation( otherUnit ) )
                                                        {
                                                            if ( countSoFar == 0 )
                                                                novel.Main.AddLangAndAfterLineItemHeader( "UnitsThatCanInvestigate", ColorTheme.HeaderGoldMoreRich ).Line();
                                                            novel.Main.AddRaw( otherUnit.GetDisplayName(), ColorTheme.HealingGreen ).HyphenSeparator()
                                                                .AddRaw( otherUnit.UnitType.GetDisplayName() ).Line();
                                                            countSoFar++;

                                                            if ( countSoFar >= 5 )
                                                                break;
                                                        }
                                                    }
                                                }

                                                if ( countSoFar == 0 )
                                                {
                                                    novel.Main.AddLangAndAfterLineItemHeader( "NoUnitsAvailableToInvestigate", ColorTheme.HeaderGoldMoreRich ).Line();
                                                    novel.Main.AddLang( "NoUnitsAvailableToInvestigate_Details" ).Line();
                                                }
                                            }

                                            return ActionResult.Success;
                                        }

                                        //investigation
                                        novel.TitleUpperLeft.Clear(); //just show the name of the investigation
                                        novel.TitleUpperLeft.AddRaw( investType.GetDisplayName() );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            if ( wouldBecomeOutcast != null )
                                            {
                                                //outcast
                                                novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                    ColorTheme.RedOrange3 );
                                            }

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );

                                            //Building name
                                            novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                                .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                            if ( isInvasiveNotInfiltration )
                                                novel.Main.AddLang( "InvasiveAction", ColorTheme.RedOrange2 ).Line();
                                            else if ( isInfiltration )
                                                novel.Main.AddLang( "InfiltrationAction", ColorTheme.RedOrange2 ).Line();
                                            else
                                                novel.Main.AddLang( "StealthyAction", ColorTheme.HeaderLighterBlue ).Line();

                                            if ( novel.TooltipWidth < 200 )
                                                novel.TooltipWidth = 200;


                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5100;
                                        {
                                            debugStage = 5200;

                                            if ( investType.GetDescription().Length > 0 )
                                                novel.Main.AddRaw( investType.GetDescription() ).Line();
                                            if ( investType.Style.StrategyTip.Text.Length > 0 )
                                                novel.Main.AddRaw( investType.Style.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                            if ( !investType.Style.MethodDetails.Text.IsEmpty() )
                                            {
                                                novel.Main.AddBoldLangAndAfterLineItemHeader( "InvestigationMethodology", ColorTheme.DataLabelWhite )
                                                    .AddRaw( investType.Style.MethodDetails.Text ).Line();
                                            }
                                        }

                                        debugStage = 6100;

                                        //then outcast
                                        if ( wouldBecomeOutcast != null && !isInvasiveNotInfiltration )
                                        {
                                            novel.Main.AddSpriteStyled_NoIndent( wouldBecomeOutcast.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange3 )
                                                .AddLangAndAfterLineItemHeader( "Unit_WillBecomeOutcast", ColorTheme.RedOrange3 ).Line();
                                            novel.Main.AddLang( "Unit_WillBecomeOutcast_StrategyTip", ColorTheme.PurpleDim ).Line();
                                        }

                                        if ( isInvasiveNotInfiltration )
                                            novel.Main.AddLang( "InvasiveAction", ColorTheme.RedOrange2 ).Line();
                                        else if ( isInfiltration )
                                            novel.Main.AddLang( "InfiltrationAction", ColorTheme.RedOrange2 ).Line();
                                        else
                                            novel.Main.AddLang( "StealthyAction", ColorTheme.HeaderLighterBlue ).Line();

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( investType.DuringGame_IsBlockedFromInvestigatingAny() || !investigation.GetCanActorDoThisInvestigation( ActorOrNull ) )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }

                                        if ( wouldBecomeOutcast != null )
                                            BasicActionsHelper.ApplyOutcastBadgeToActor( ActorOrNull, wouldBecomeOutcast );

                                        Engine_Universal.BeginProfilerSample( "Investigation-InvestigateSpecificBuilding" );

                                        //do the stuff!
                                        if ( investigation.InvestigateSpecificBuilding( actorAsUnit, BuildingOrNull ) )
                                        {
                                            //we found it!
                                        }
                                        else
                                        {
                                            //we did not find it, but we made progress
                                        }

                                        Engine_Universal.EndProfilerSample( "Investigation-InvestigateSpecificBuilding" );

                                        return ActionResult.Success;
                                    }
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "AndroidInvestigate", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "AndroidContemplate":
                    #region AndroidContemplate
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ContemplationType contemplation = BuildingOrNull?.GetCurrentContemplationThatShouldShowOnMap();

                            ISimMachineUnit actorAsUnit = ActorOrNull as ISimMachineUnit;
                            if ( contemplation == null || BuildingOrNull == null )
                            {
                                //switch ( Logic )
                                //{
                                //    case ActionLogic.AppendToTooltip:
                                //        BufferOrNull.Line().AddNeverTranslated( "contemplation:" + (contemplation == null ? "[null]" : contemplation.ID) + " Building: " +
                                //            (BuildingOrNull == null ? "[null]" : BuildingOrNull.GetDisplayName() ) );
                                //        break;
                                //}
                                return ActionResult.Blocked;
                            }
                            if ( !contemplation.DuringGame_GetShouldShowOnMap() || contemplation.IsAdventureForAFutureBuild )
                                return ActionResult.Blocked; //we can only do it once per timeline

                            if ( Logic != ActionLogic.Execute )
                            {
                                if ( BuildingOrNull.GetAreMoreUnitsBlockedFromComingHere() )
                                {
                                    if ( actorAsUnit != null && BuildingOrNull.CurrentOccupyingUnit == actorAsUnit )
                                    { } //this is okay, we are at this building
                                    else
                                    {
                                        //switch ( Logic )
                                        //{
                                        //    case ActionLogic.AppendToTooltip:
                                        //        BufferOrNull.Line().AddNeverTranslated( "MoreUnitsBlockedFromComingHere" );
                                        //        break;
                                        //}
                                        return ActionResult.Blocked;
                                    }
                                }
                            }

                            NPCEvent minorEvent = contemplation.EventToTriggerAtLocation;
                            if ( minorEvent == null || minorEvent.MinorData == null ) //could not find the minor event, or no minor event specified!
                            {
                                //switch ( Logic )
                                //{
                                //    case ActionLogic.AppendToTooltip:
                                //        BufferOrNull.Line().AddNeverTranslated( "minorEvent:" + (minorEvent == null ? "[null]" : minorEvent.ID ) + " minorData: " +
                                //            (minorEvent?.MinorData == null ? "[null]" : "[ok]" ) );
                                //        break;
                                //}
                                return ActionResult.Blocked;
                            }
                            if ( minorEvent.GateByCity != null && !minorEvent.GateByCity.CalculateMeetsPrerequisites( false ) )
                            {
                                //switch ( Logic )
                                //{
                                //    case ActionLogic.AppendToTooltip:
                                //        BufferOrNull.Line().AddNeverTranslated( "gate-by-city blocked" );
                                //        break;
                                //}
                                return ActionResult.Blocked; //this happens when something just got blocked, but the fact that it was blocked is calculated on a bg thread after the aggregate calculations happened.
                            }

                            bool isBlockedByClearance = false;
                            int clearanceRequired = 0;
                            int actorHasClearance = 0;
                            if ( minorEvent.EventIsBlockedForActorsWithoutOverridingClearance > 0 && ActorOrNull != null )
                            {
                                clearanceRequired = minorEvent.EventIsBlockedForActorsWithoutOverridingClearance;
                                ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                                isBlockedByClearance = clearanceRequired > actorHasClearance;
                            }
                            else if ( minorEvent.EventIsBlockedForActorsWithoutClearance )
                            {
                                clearanceRequired = BuildingOrNull.CalculateLocationSecurityClearanceInt();
                                ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                                isBlockedByClearance = clearanceRequired > actorHasClearance;
                            }

                            bool isBlockedByActor = false;
                            if ( ActorOrNull != null && !minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, null ) )
                                isBlockedByActor = true;

                            bool isBlockedAtAll = isBlockedByClearance || isBlockedByActor;

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return isBlockedAtAll ? ActionResult.Blocked : ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        //RenderManager_Streets.DrawMapItemHighlightedBorder( BuildingOrNull.GetMapItem(),ColorRefs.BuildingPartOfInvestigation.ColorHDR, Engine_HotM.GameMode == MainGameMode.CityMap );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string projectColor = isBlockedAtAll ? ColorTheme.RedOrange2 : string.Empty;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || ( ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting );
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints??1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( isBlockedByClearance && !HandbookRefs.ClearancesAreContextual.Meta_HasBeenUnlocked )
                                            HandbookRefs.ClearancesAreContextual.DuringGame_UnlockIfNeeded( true );

                                        debugStage = 3100;

                                        novel.TitleUpperLeft.Clear(); //make this the first text
                                        novel.TitleUpperLeft.AddRaw( contemplation.GetDisplayName(), projectColor );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            //contemplation

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );

                                            if ( isBlockedByClearance )
                                                novel.MainHeader.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", projectColor )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();
                                            
                                            minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, novel.MainHeader );

                                            //Building name
                                            novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                                .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                            //BufferOrNull.Line().AddNeverTranslated( "Clearance:" + BuildingOrNull.CalculateLocationSecurityClearanceInt() + " vs " +
                                            //    Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding ) );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        if ( contemplation.GetDescription().Length > 0 )
                                            novel.Main.AddRaw( contemplation.GetDescription() ).Line();
                                        if ( contemplation.StrategyTip.Text.Length > 0 )
                                            novel.Main.AddRaw( contemplation.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                        debugStage = 5100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5200;

                                        if ( isBlockedByClearance )
                                            novel.Main.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", projectColor )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();
                                        
                                        minorEvent.MinorData.DuringGameplay_EventCanBeStartedByActor( ActorOrNull, novel.Main );

                                        debugStage = 6100;

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( isBlockedAtAll )
                                            return ActionResult.Blocked;
                                        NPCCohort cohort = minorEvent.MinorData?.CalculateCohort( BuildingOrNull );
                                        if ( cohort != null )
                                            cohort.DuringGame_DiscoverIfNeed();
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedAtAll )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        {
                                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                                            VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( BuildingOrNull.GetPositionForCameraFocus(), true );
                                        }

                                        //START_MINOR_EVENT
                                        minorEvent.ClearForMinorEvent();
                                        MinorEventHandler.TryStart( ActorOrNull, minorEvent, BuildingOrNull, cohort, true );

                                        SimCommon.NeedsContemplationTargetRecalculation = true;
                                        SimCommon.NeedsStreetSenseTargetRecalculation = true;
                                        SimCommon.CurrentContemplationSeed = Engine_Universal.PermanentQualityRandom.Next();
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "AndroidContemplate", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "AndroidExploreSite":
                    #region AndroidExploreSite
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            ExplorationSiteType explorationSite = BuildingOrNull?.GetCurrentExplorationSiteThatShouldShowOnMap();

                            ISimMachineUnit actorAsUnit = ActorOrNull as ISimMachineUnit;
                            if ( explorationSite == null || BuildingOrNull == null )
                            {
                                //switch ( Logic )
                                //{
                                //    case ActionLogic.AppendToTooltip:
                                //        BufferOrNull.Line().AddNeverTranslated( "contemplation:" + (contemplation == null ? "[null]" : contemplation.ID) + " Building: " +
                                //            (BuildingOrNull == null ? "[null]" : BuildingOrNull.GetDisplayName() ) );
                                //        break;
                                //}
                                return ActionResult.Blocked;
                            }
                            if ( explorationSite.DuringGame_HasBeenCompleted )
                                return ActionResult.Blocked; //we can only do it once per timeline

                            if ( Logic != ActionLogic.Execute )
                            {
                                if ( BuildingOrNull.GetAreMoreUnitsBlockedFromComingHere() )
                                {
                                    if ( actorAsUnit != null && BuildingOrNull.CurrentOccupyingUnit == actorAsUnit )
                                    { } //this is okay, we are at this building
                                    else
                                    {
                                        //switch ( Logic )
                                        //{
                                        //    case ActionLogic.AppendToTooltip:
                                        //        BufferOrNull.Line().AddNeverTranslated( "MoreUnitsBlockedFromComingHere" );
                                        //        break;
                                        //}
                                        return ActionResult.Blocked;
                                    }
                                }
                            }

                            bool isBlockedByClearance = false;
                            int clearanceRequired = 0;
                            int actorHasClearance = 0;
                            //does not seem interesting
                            //if ( minorEvent.EventIsBlockedForActorsWithoutOverridingClearance > 0 && ActorOrNull != null )
                            //{
                            //    clearanceRequired = minorEvent.EventIsBlockedForActorsWithoutOverridingClearance;
                            //ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            //    isBlockedByClearance = clearanceRequired > actorHasClearance;
                            //}
                            //else if ( minorEvent.EventIsBlockedForActorsWithoutClearance )
                            //{
                            //    clearanceRequired = BuildingOrNull.CalculateLocationSecurityClearanceInt();
                            //ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            //    isBlockedByClearance = clearanceRequired > actorHasClearance;
                            //}

                            bool isBlockedByActor = false;
                            if ( ActorOrNull != null && !explorationSite.DuringGameplay_ExplorationCanBeStartedByActor( ActorOrNull, null ) )
                                isBlockedByActor = true;

                            bool isBlockedAtAll = isBlockedByClearance || isBlockedByActor;

                            bool isBlockedByBeingPartOfShellCompany = false;
                            if ( actorAsUnit?.GetIsRelatedToPlayerShellCompany() ?? false )
                            {
                                isBlockedAtAll = true;
                                isBlockedByBeingPartOfShellCompany = true;
                            }

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return isBlockedAtAll ? ActionResult.Blocked : ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        //RenderManager_Streets.DrawMapItemHighlightedBorder( BuildingOrNull.GetMapItem(),ColorRefs.BuildingPartOfInvestigation.ColorHDR, Engine_HotM.GameMode == MainGameMode.CityMap );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string projectColor = isBlockedAtAll ? ColorTheme.RedOrange2 : string.Empty;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || (ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting);
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints ?? 1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        if ( isBlockedByClearance && !HandbookRefs.ClearancesAreContextual.Meta_HasBeenUnlocked )
                                            HandbookRefs.ClearancesAreContextual.DuringGame_UnlockIfNeeded( true );

                                        debugStage = 3100;

                                        novel.TitleUpperLeft.Clear(); //make this the first text
                                        novel.TitleUpperLeft.AddRaw( explorationSite.GetDisplayName(), projectColor );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            //explore site

                                            novel.MainHeader.StartStyleLineHeightA();

                                            explorationSite.AppendRewardPortion( novel.MainHeader );

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );

                                            if ( isBlockedByClearance )
                                                novel.MainHeader.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", projectColor )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();
                                            if ( isBlockedByBeingPartOfShellCompany )
                                                novel.MainHeader.AddLang( "CannotUseUnitsTiedToYourShellCompany", projectColor ).Line();

                                            explorationSite.DuringGameplay_ExplorationCanBeStartedByActor( ActorOrNull, novel.MainHeader );

                                            //Building name
                                            novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                                .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                            //BufferOrNull.Line().AddNeverTranslated( "Clearance:" + BuildingOrNull.CalculateLocationSecurityClearanceInt() + " vs " +
                                            //    Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding ) );

                                            novel.MainHeader.EndLineHeight();

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        debugStage = 5100;

                                        novel.Main.StartStyleLineHeightA();

                                        explorationSite.AppendRewardPortion( novel.Main );

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5200;

                                        if ( isBlockedByClearance )
                                            novel.Main.AddLangAndAfterLineItemHeader( "InsufficientSecurityClearanceToAttempt", projectColor )
                                                    .AddFormat2( "OutOF", actorHasClearance, clearanceRequired ).Line();
                                        if ( isBlockedByBeingPartOfShellCompany )
                                            novel.Main.AddLang( "CannotUseUnitsTiedToYourShellCompany", projectColor ).Line();

                                        explorationSite.DuringGameplay_ExplorationCanBeStartedByActor( ActorOrNull, novel.Main );

                                        novel.Main.EndLineHeight();

                                        debugStage = 6100;

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( isBlockedAtAll )
                                            return ActionResult.Blocked;
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedAtAll )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        {
                                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                                            VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( BuildingOrNull.GetPositionForCameraFocus(), true );
                                        }

                                        ActionOverTime action = ActionOverTime.TryToCreate( "AndroidExploreSite", ActorOrNull, 0, BuildingOrNull,
                                            ActionOverTimeStart.OkayIfActorBlockedFromActing ); //because we may have used our last AP just to get here
                                        if ( action == null )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            return ActionResult.Blocked;
                                        }
                                        debugStage = 7200;
                                        action.RelatedAbilityOrNull = null;
                                        debugStage = 7300;
                                        action.SetIntData( "Target1", explorationSite.CalculateEstimatedRemainingTurnsForActor( ActorOrNull ) );
                                        action.SetStringData( "ID1", explorationSite.ID );
                                        ActorOrNull.SetTargetingMode( null, null );
                                        explorationSite.LockToCurrentBuildingForTurns( 4 );

                                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "ExploreSiteStarted" ), NoteStyle.StoredGame,
                                            explorationSite.ID, string.Empty, string.Empty, string.Empty, ActorOrNull.SortID,
                                            0, 0, ActorOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "AndroidContemplate", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "AndroidEnterCityConflict":
                    #region AndroidEnterCityConflict
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    if ( BuildingOrNull != null )
                    {
                        int debugStage = 0;
                        try
                        {
                            CityConflict cityConflict = BuildingOrNull?.CurrentCityConflict?.Display;
                            ISimMachineUnit actorAsUnit = ActorOrNull as ISimMachineUnit;
                            if ( cityConflict == null || BuildingOrNull == null )
                            {
                                //switch ( Logic )
                                //{
                                //    case ActionLogic.AppendToTooltip:
                                //        BufferOrNull.Line().AddNeverTranslated( "cityConflict:" + (cityConflict == null ? "[null]" : cityConflict.ID) + " Building: " +
                                //            (BuildingOrNull == null ? "[null]" : BuildingOrNull.GetDisplayName() ) );
                                //        break;
                                //}
                                return ActionResult.Blocked;
                            }
                            if ( cityConflict.DuringGameplay_State != CityConflictState.Active )
                                return ActionResult.Blocked; //we can only do it when it is active

                            if ( Logic != ActionLogic.Execute )
                            {
                                if ( BuildingOrNull.GetAreMoreUnitsBlockedFromComingHere() )
                                {
                                    if ( actorAsUnit != null && BuildingOrNull.CurrentOccupyingUnit == actorAsUnit )
                                    { } //this is okay, we are at this building
                                    else
                                    {
                                        //switch ( Logic )
                                        //{
                                        //    case ActionLogic.AppendToTooltip:
                                        //        BufferOrNull.Line().AddNeverTranslated( "MoreUnitsBlockedFromComingHere" );
                                        //        break;
                                        //}
                                        return ActionResult.Blocked;
                                    }
                                }
                            }

                            //bool isBlockedByClearance = false;
                            //int clearanceRequired = 0;
                            //if ( minorEvent.EventIsBlockedForActorsWithoutOverridingClearance > 0 && ActorOrNull != null )
                            //{
                            //    clearanceRequired = minorEvent.EventIsBlockedForActorsWithoutOverridingClearance;
                            //ActorOrNull.GetTypeDuringGameData().CalculateClearanceLevelsForUnitThatDoesNotYetExist( out _, out _, out actorHasClearance );
                            //    isBlockedByClearance = clearanceRequired > actorHasClearance;
                            //}
                            //else if ( minorEvent.EventIsBlockedForActorsWithoutClearance )
                            //{
                            //    clearanceRequired = BuildingOrNull.CalculateLocationSecurityClearanceInt();
                            //    isBlockedByClearance = clearanceRequired > ActorOrNull.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
                            //}

                            //bool isBlockedByActorCollection = false;
                            //if ( minorEvent.EventIsBlockedUnlessActorIsInCollection != null && !ActorOrNull.GetIsInActorCollection( minorEvent.EventIsBlockedUnlessActorIsInCollection ) )
                            //    isBlockedByActorCollection = true;

                            bool isBlockedAtAll = false;// isBlockedByClearance || isBlockedByActorCollection;

                            bool isBlockedByBeingPartOfShellCompany = false;
                            if ( actorAsUnit?.GetIsRelatedToPlayerShellCompany() ?? false )
                            {
                                isBlockedAtAll = true;
                                isBlockedByBeingPartOfShellCompany = true;
                            }

                            switch ( Logic )
                            {
                                case ActionLogic.CalculateIfBlocked:
                                    return isBlockedAtAll ? ActionResult.Blocked : ActionResult.Success;
                                case ActionLogic.PredictIcons:
                                    {
                                        //RenderManager_Streets.DrawMapItemHighlightedBorder( BuildingOrNull.GetMapItem(),ColorRefs.BuildingPartOfInvestigation.ColorHDR, Engine_HotM.GameMode == MainGameMode.CityMap );
                                    }
                                    break;
                                case ActionLogic.AppendToTooltip:
                                    {
                                        debugStage = 2100;
                                        string projectColor = isBlockedAtAll ? ColorTheme.RedOrange2 : string.Empty;

                                        novel.CanExpand = CanExpandType.Brief;

                                        bool canAffordEnergy = ActorOrNull == null || (ResourceRefs.MentalEnergy.Current >= 1 + ExtraMentalEnergyCostFromSprinting);
                                        bool canAffordAP = (ActorOrNull?.CurrentActionPoints ?? 1) > 0;

                                        if ( !canAffordEnergy || !canAffordAP )
                                            novel.ShouldTooltipBeRed = true;

                                        debugStage = 3100;

                                        novel.TitleUpperLeft.Clear(); //make this the first text
                                        novel.TitleUpperLeft.AddRaw( cityConflict.GetDisplayName(), projectColor );

                                        if ( !canAffordEnergy )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( (1 + ExtraMentalEnergyCostFromSprinting).ToString(), ColorTheme.RedOrange3 );
                                        if ( !canAffordAP )
                                            novel.TitleUpperLeft.Space3xIfNotEmpty().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                                                ResourceRefs.MentalEnergy.IconColorHex ).AddRaw( "1", ColorTheme.RedOrange3 );

                                        if ( !InputCaching.ShouldShowDetailedTooltips )
                                        {
                                            debugStage = 3200;

                                            //conflict

                                            CombatTextHelper.AppendLastPredictedDamageBrief( ActorOrNull, novel.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );

                                            //if ( isBlockedByClearance )
                                            //    novel.MainHeader.AddLang( "InsufficientSecurityClearanceToAttempt", projectColor ).Line();
                                            //if ( isBlockedByActorCollection )
                                            //    novel.MainHeader.AddFormat1( "YourUnitMustBe", minorEvent.EventIsBlockedUnlessActorIsInCollection.GetDisplayName(), projectColor ).Line();
                                            if ( isBlockedByBeingPartOfShellCompany )
                                                novel.MainHeader.AddLang( "CannotUseUnitsTiedToYourShellCompany", projectColor ).Line();

                                            //Building name
                                            novel.MainHeader.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                                .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                            //BufferOrNull.Line().AddNeverTranslated( "Clearance:" + BuildingOrNull.CalculateLocationSecurityClearanceInt() + " vs " +
                                            //    Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding ) );

                                            return ActionResult.Success;
                                        }

                                        debugStage = 4100;

                                        if ( cityConflict.GetDescription().Length > 0 )
                                            novel.Main.AddRaw( cityConflict.GetDescription() ).Line();
                                        if ( cityConflict.StrategyTip.Text.Length > 0 )
                                            novel.Main.AddRaw( cityConflict.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                        debugStage = 5100;

                                        //Building name
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Location", ColorTheme.DataLabelWhite )
                                            .AddRaw( BuildingOrNull.GetDisplayName() ).Line();

                                        debugStage = 5200;

                                        //if ( isBlockedByClearance )
                                        //    novel.Main.AddLang( "InsufficientSecurityClearanceToAttempt", projectColor ).Line();
                                        //if ( isBlockedByActorCollection )
                                        //    novel.Main.AddFormat1( "YourUnitMustBe", minorEvent.EventIsBlockedUnlessActorIsInCollection.GetDisplayName(), projectColor ).Line();
                                        if ( isBlockedByBeingPartOfShellCompany )
                                            novel.Main.AddLang( "CannotUseUnitsTiedToYourShellCompany", projectColor ).Line();

                                        debugStage = 6100;

                                        CombatTextHelper.AppendLastPredictedDamageLong( ActorOrNull, novel.Main, novel.Main, true, false, false );
                                    }
                                    break;
                                case ActionLogic.Execute:
                                    {
                                        if ( isBlockedAtAll )
                                            return ActionResult.Blocked;
                                        MapPOI relevantPOI = BuildingOrNull.CalculateLocationPOI();
                                        if ( relevantPOI != null )
                                        {
                                            relevantPOI.HasBeenDiscovered = true;
                                            NPCCohort poiCohort = relevantPOI.ControlledBy;
                                            if ( poiCohort != null )
                                                poiCohort.DuringGame_DiscoverIfNeed();
                                        }

                                        if ( isBlockedAtAll )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        {
                                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                                            VisManagerMidBase.Instance.MainCamera_JumpCameraToPosition( BuildingOrNull.GetPositionForCameraFocus(), true );
                                        }

                                        //START_MINOR_EVENT
                                        SimCommon.CurrentSimpleChoice = CityConflictExaminationHandler.Start( ActorOrNull, cityConflict, BuildingOrNull );

                                        //SimCommon.NeedsCityConflictTargetRecalculation = true;
                                        //SimCommon.NeedsStreetSenseTargetRecalculation = true;
                                        //SimCommon.CurrentCityConflictSeed = Engine_Universal.PermanentQualityRandom.Next();
                                    }
                                    break;
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "AndroidEnterCityConflict", debugStage, e, Verbosity.ShowAsError );
                        }
                    }
                    #endregion
                    break;
                case "BoardVehicle": //there is no special logic for this
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = false;
                    return ActionResult.Success;
                default:
                    if ( ActorOrNull == null )
                    {
                        IncludeRestrictedAreaNoticeInTooltip = false;
                        return ActionResult.Blocked;
                    }
                    IncludeRestrictedAreaNoticeInTooltip = true;
                    ArcenDebugging.LogSingleLine( "LocActs_Basic: Called HandleAction for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return ActionResult.Blocked;
            }
            return ActionResult.Success;
        }

        public string TryHandleLocationActionForList( ISimBuilding BuildingOrNull, LocationActionType Action, NPCEvent EventOrNull, ProjectOutcomeStreetSenseItem ProjectStreetOrNull,
            string OtherOptionalID, out IA5Sprite Icon )
        {
            if ( Action == null )
            {
                Icon = null;
                return string.Empty;
            }

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Action.ID )
            {                
                case "MoveToPosition":
                case "MechMove":
                case "NoOp":
                case "MoveToBuilding":
                case "BoardVehicle":
                case "AndroidInvestigate":
                case "AndroidContemplate":
                case "AndroidExploreSite":
                case "AndroidEnterCityConflict":
                    break; //nothing to do on these
                case "StealVehicle":
                    #region StealVehicle
                    if ( BuildingOrNull != null )
                    {
                        BasicActionsHelper.Calc_StealVehicle( BuildingOrNull, out MachineVehicleType claimVehicleType );
                        Icon = claimVehicleType?.ShapeIcon;
                        return LocalizedString.AddRawAndAfterLineItemHeader_New( Action.GetDisplayName() ).AddRaw( claimVehicleType?.GetDisplayName() ).FinalText;
                    }
                    break;
                    #endregion
                case "RecruitAndroid":
                    #region RecruitAndroid
                    if ( BuildingOrNull != null )
                    {
                        MachineUnitType claimUnitType = MachineUnitTypeTable.Instance.GetRowByIDOrNullIfNotFound( OtherOptionalID );
                        Icon = claimUnitType?.ShapeIcon;
                        return LocalizedString.AddRawAndAfterLineItemHeader_New( Action.GetDisplayName() ).AddRaw( claimUnitType?.GetDisplayName() ).FinalText;
                    }
                    break;
                    #endregion
                case "ColdBlood":
                case "MurderAndroidForRegistration":
                case "Wiretap":
                case "HideAndSelfRepair":
                    Icon = Action.Icon;
                    return Action.GetDisplayName();
                case "MinorEvent":
                    #region MinorEvent
                    if ( BuildingOrNull != null )
                    {
                        NPCEvent minorEvent = EventOrNull;
                        if ( minorEvent == null || minorEvent.MinorData == null ) //could not find the minor event, or no minor event specified!
                            break;
                        if ( minorEvent.GateByCity != null && !minorEvent.GateByCity.CalculateMeetsPrerequisites( false ) )
                            break; //this happens when something just got blocked, but the fact that it was blocked is calculated on a bg thread after the aggregate calculations happened.

                        Icon = minorEvent.Icon;
                        return minorEvent.GetDisplayName();
                    }
                    #endregion
                    break;
                case "ProjectStreetSenseAction":
                    #region ProjectStreetSenseAction
                    if ( BuildingOrNull != null )
                    {
                        ProjectOutcomeStreetSenseItem streetItem = ProjectStreetOrNull;
                        if ( streetItem == null ) //could not find the StreetSense item, or none specified
                            break;
                        if ( !streetItem.DuringGame_CanStillDo() )
                            break; //this happens when something just expired

                        Icon = streetItem.Icon;
                        return streetItem.DisplayName.Text;
                    }
                    #endregion
                    break;
                //case "AndroidEnterCityConflict":
                //    #region AndroidEnterCityConflict
                //    if ( BuildingOrNull != null )
                //    {
                //        CityConflict cityConflict = BuildingOrNull?.CurrentCityConflict?.Display;
                //        if ( cityConflict != null )
                //        {
                //            Icon = cityConflict.Icon;
                //            return LocalizedString.AddRawAndAfterLineItemHeader_New( Action.GetDisplayName() ).AddRaw( cityConflict.GetDisplayName() ).FinalText;
                //        }
                //    }
                //    break;
                //#endregion
                default:
                    ArcenDebugging.LogSingleLine( "LocActs_Basic: Called TryHandleLocationActionForList for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    Icon = null;
                    return string.Empty;
            }

            Icon = null;
            return string.Empty;
        }
    }
}

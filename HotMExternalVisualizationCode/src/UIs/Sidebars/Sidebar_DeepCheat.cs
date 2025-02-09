using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_DeepCheat : Sidebar_Base, IUISidebarTypeImplementation
    {
        #region FillListOfSidebarItems
        public void WriteAnySidebarItems( ref float currentY )
        {
            bool hasEmergedYet = FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped && FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped;
            bool isInTheEndOfTime = Engine_HotM.GameMode == MainGameMode.TheEndOfTime;

            if ( hasEmergedYet )
            {
                if ( SimCommon.IsCheatTimeline && !isInTheEndOfTime )
                {
                    AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "DeepCheat" ).Items, ref currentY );
                    AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "DebugDeepCheat" ).Items, ref currentY );
                    //currentY -= 10f;
                }
                
                //currentY -= 10f;

                //AddItem( item_ExitHeader );
                //AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "Cosmetic" ).Items );
                ////currentY -= 10f;
            }
        }
        #endregion
    }

    public class Sidebar_DeepCheatItems : ISidebarBasicItemImplementation
    {
        public void Sidebar_GetOrSetUIData( UISidebarBasicItem Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.GetTextToShowFromVolatile:
                    ExtraData.Buffer.AddRaw( Item.GetDisplayName() );
                    break;
                case UIAction.HandleMouseover:
                    if ( ButtonController != null )
                        break; //don't do mouseovers on the section headers 

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Item ), Element, SideClamp.LeftOrRight, 
                        TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.ClampToSidebar ) )
                    {
                        novel.TitleUpperLeft.AddRaw( Item.GetDisplayNameForSidebar() );
                        string hotkeyToDisplay = Item.GetHotkeyToDisplayAsToggleForSidebar();
                        if ( hotkeyToDisplay.Length > 0 )
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( hotkeyToDisplay );
                        novel.Main.StartColor( ColorTheme.NarrativeColor );
                        WriteSidebarTooltip( Item, novel.Main );
                        novel.Icon = Item.Icon;
                    }
                    break;
                case UIAction.OnClick:
                    HandleClick( Item, ExtraData.MouseInput );
                    break;
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        ExtraData.Image.SetSpriteIfNeeded_Simple( Item.GetIcon()?.GetSpriteForUI() );

                        bool isHovered = Element.LastHadMouseWithin;

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddRaw( Item.GetDisplayName() );
                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();

                        if ( ExtraData.SubTextsWrapper.Length > 1 )
                        {
                            //details line
                            buffer = ExtraData.SubTextsWrapper[1].Text.StartWritingToBuffer();
                            this.WriteSidebarSecondLine( Item, buffer );
                            ExtraData.SubTextsWrapper[1].Text.FinishWritingToBuffer();
                        }
                    }
                    break;
            }
        }

        public SidebarItemType GetItemType( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                case "DeepCheatHeader":
                    return SidebarItemType.TextHeader;
                case "BuildingOverlays":
                    {
                        BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();
                        if ( effectiveOverlayType != null && !effectiveOverlayType.IsDefault )
                            return SidebarItemType.ImgDoubleLine;
                    }
                    break;
                case "BuildingDirectory":
                case "PopulationStats":
                case "VisualSpeed":
                case "UIVisibility":
                case "WorldIcons":
                    return SidebarItemType.ImgDoubleLine;
            }
            return SidebarItemType.ImgSingleLine;
        }

        public MouseHandlingResult HandleClick( UISidebarBasicItem Item, MouseHandlingInput input )
        {
            switch ( Item.ID )
            {
                case "BuildingOverlays":
                    Window_FilterOverlayWindow.Instance.Open();
                    break;
                case "PhotoMode":
                    {
                        InputCaching.TogglePhotoModeOneFrame = true;
                    }
                    break;
                case "VisualSpeed":
                    {
                    }
                    break;
                case "UIVisibility":
                    VisCurrent.IsUIHiddenExceptForSidebar = !VisCurrent.IsUIHiddenExceptForSidebar;
                    break;
                case "WorldIcons":
                    VisCurrent.AreAllGameIconsHidden = !VisCurrent.AreAllGameIconsHidden;
                    break;
                case "CityMap":
                    VisCommands.ToggleCityMapMode();
                    break;
                case "Cheats":
                    if ( Window_CheatWindow.Instance.IsOpen )
                        Window_CheatWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_CheatWindow.Instance.Open();
                    break;
                case "Performance":
                    VisCommands.ShowPerformance();
                    break;
                case "BuildingDirectory":
                    if ( Window_BuildingDirectory.Instance.IsOpen )
                        Window_BuildingDirectory.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_BuildingDirectory.Instance.Open();
                    break;
                case "PopulationStats":
                    if ( Window_PopulationStatistics.Instance.IsOpen )
                        Window_PopulationStatistics.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_PopulationStatistics.Instance.Open();
                    break;
                case "CityFlags":
                    if ( Window_CityFlagsInfo.Instance.IsOpen )
                        Window_CityFlagsInfo.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_CityFlagsInfo.Instance.Open();
                    break;
                case "MetaFlags":
                    if ( Window_MetaFlagsInfo.Instance.IsOpen )
                        Window_MetaFlagsInfo.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_MetaFlagsInfo.Instance.Open();
                    break;
                case "UpgradesAndUnlocks":
                    if ( Window_UpgradesAndUnlocksEditing.Instance.IsOpen )
                        Window_UpgradesAndUnlocksEditing.Instance.Close( WindowCloseReason.UserDirectRequest );
                    else
                        Window_UpgradesAndUnlocksEditing.Instance.Open();
                    break;
                case "DeepCheatHeader":
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "Error!  Did not have a HandleClick entry for Sidebar_DeepCheat ID '" + Item.ID + "'!", Verbosity.ShowAsError );
                    break;
            }
            return MouseHandlingResult.None;
        }

        public void WriteSidebarTooltip( UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer )
        {
            switch ( Item.ID )
            {
                case "BuildingDirectory":
                    {
                        int visibleCount = 0;
                        int totalCount = 0;

                        foreach ( KeyValuePair<int, ISimBuilding> kv in World.Buildings.GetAllBuildings() )
                        {
                            if ( kv.Value.GetParentCell()?.ParentTile?.HasEverBeenExplored??false )
                                visibleCount++;
                            totalCount++;
                        }

                        buffer.AddLang( "Sidebar_BuildingsCount_Details", ColorTheme.CyanDim ).Space1x();
                        if ( visibleCount == totalCount )
                            buffer.AddRaw( totalCount.ToStringThousandsWhole() ).Line();
                        else
                            buffer.AddFormat2( "Sidebar_VisibleOutOf", totalCount.ToStringThousandsWhole(), visibleCount.ToStringThousandsWhole() ).Line();
                    }
                    break;
                case "PopulationStats":
                    {
                        DictionaryView<EconomicClassType, int> dictData = World.People.GetCurrentResidents();
                        int totalPopulation = 0;
                        foreach ( KeyValuePair<EconomicClassType, int> kv in dictData )
                            totalPopulation += kv.Value;

                        buffer.AddLang( "Sidebar_HumanPopulationCount_Detailed", ColorTheme.CyanDim ).Space1x();
                        buffer.AddRaw( totalPopulation.ToStringThousandsWhole() ).Line();
                    }
                    break;
                case "BuildingOverlays":
                    {
                        BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();
                        if ( effectiveOverlayType != null && !effectiveOverlayType.IsDefault )
                        {
                            buffer.AddLang( "Sidebar_CurrentOverlay_Detailed", ColorTheme.CyanDim ).Space1x();
                            buffer.AddRaw( Engine_HotM.GetEffectiveOverlayType()?.GetDisplayName() ?? "" ).Line();
                        }
                    }
                    break;
            }

            buffer.AddRaw( Item.GetDescription() );

            switch ( Item.ID )
            {
                case "PhotoMode":
                    {
                        buffer.Line();

                        buffer.Line();
                        buffer.AddRawAndAfterLineItemHeader( InputActionTypeDataTable.Instance.GetRowByID( "PhotoModeManualFocus" ).GetDisplayName(), ColorTheme.CyanDim )
                            .AddColorizedKeyCombo( string.Empty, ColorTheme.Grayer, "PhotoModeManualFocus" );
                    }
                    break;
                case "VisualSpeed":
                    {
                        buffer.Line();

                        float timeScale = Time.timeScale;
                        buffer.Line();
                        buffer.AddLangAndAfterLineItemHeader( "VisualsPausedPrefix", ColorTheme.CyanDim )
                            .AddLang( timeScale <= 0 ? "VisualsPaused" : "VisualsRunning" );

                        float speed = timeScale <= 0 ? SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused] : timeScale;

                        buffer.Line();
                        buffer.AddLangAndAfterLineItemHeader( "VisualsSpeed", ColorTheme.CyanDim )
                            .AddRaw( Mathf.RoundToInt( speed * 100 ).ToStringIntPercent() );

                        buffer.Line();

                        buffer.Line();
                        buffer.AddRawAndAfterLineItemHeader( InputActionTypeDataTable.Instance.GetRowByID( "ToggleVisualPause" ).GetDisplayName(), ColorTheme.CyanDim )
                            .AddColorizedKeyCombo( string.Empty, ColorTheme.Grayer, "ToggleVisualPause" );

                        buffer.Line();
                        buffer.AddRawAndAfterLineItemHeader( InputActionTypeDataTable.Instance.GetRowByID( "DecreaseVisualSpeed" ).GetDisplayName(), ColorTheme.CyanDim )
                            .AddColorizedKeyCombo( string.Empty, ColorTheme.Grayer, "DecreaseVisualSpeed" );

                        buffer.Line();
                        buffer.AddRawAndAfterLineItemHeader( InputActionTypeDataTable.Instance.GetRowByID( "IncreaseVisualSpeed" ).GetDisplayName(), ColorTheme.CyanDim )
                            .AddColorizedKeyCombo( string.Empty, ColorTheme.Grayer, "IncreaseVisualSpeed" );
                    }
                    break;
            }
        }

        public void WriteSidebarSecondLine( UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer )
        {
            switch ( Item.ID )
            {
                case "BuildingDirectory":
                    {
                        int totalBuildingsCount = World.Buildings.GetAllBuildings().Count;
                        buffer.AddLang( "Sidebar_BuildingsCount_Abbrev" ).AfterLineItemHeader();
                        buffer.AddRaw( totalBuildingsCount.ToStringLargeNumberAbbreviated() );
                    }
                    break;
                case "PopulationStats":
                    {
                        DictionaryView<EconomicClassType, int> dictData = World.People.GetCurrentResidents();
                        int totalPopulation = 0;
                        foreach ( KeyValuePair<EconomicClassType, int> kv in dictData )
                            totalPopulation += kv.Value;

                        buffer.AddLang( "Sidebar_HumanPopulationCount_Abbrev" ).AfterLineItemHeader();
                        buffer.AddRaw( totalPopulation.ToStringLargeNumberAbbreviated() );
                    }
                    break;
                case "BuildingOverlays":
                    {
                        BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();
                        if ( effectiveOverlayType != null && !effectiveOverlayType.IsDefault )
                        {
                            buffer.AddLang( "Sidebar_CurrentOverlay_Abbrev" ).Space1x();
                            buffer.AddRaw( Engine_HotM.GetEffectiveOverlayType()?.GetDisplayName() ?? "" );
                        }
                    }
                    break;
                case "VisualSpeed":
                    {
                        float timeScale = Time.timeScale;
                        if ( timeScale <= 0 )
                            buffer.AddLang( "VisualsPaused", ColorTheme.RedOrange2 );
                        else
                        {
                            float speed = timeScale <= 0 ? SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused] : timeScale;
                            buffer.AddLangAndAfterLineItemHeader( "VisualsSpeed", ColorTheme.CyanDim )
                                .AddRaw( Mathf.RoundToInt( speed * 100 ).ToStringIntPercent(),
                                speed < 0.5f ? ColorTheme.RedOrange2 : (speed < 1f ? 
                                ColorTheme.Settings_CurrentDiffersFromDefaultYellow : ColorTheme.SkillPaleGreen ) );
                        }
                    }
                    break;
                case "UIVisibility":
                    {
                        if ( VisCurrent.IsUIHiddenExceptForSidebar )
                            buffer.AddLang( "UIHidden", ColorTheme.RedOrange2 );
                        else
                            buffer.AddLang( "UIVisible", ColorTheme.Gray );
                    }
                    break;
                case "WorldIcons":
                    {
                        if ( VisCurrent.AreAllGameIconsHidden )
                            buffer.AddLang( "IconsHidden", ColorTheme.RedOrange2 );
                        else
                            buffer.AddLang( "IconsVisible", ColorTheme.Gray );
                    }
                    break;
            }
        }
    }
}

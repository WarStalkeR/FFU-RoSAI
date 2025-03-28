using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_MainGameHeaderBarRight : WindowControllerAbstractBase
    {
        public static Window_MainGameHeaderBarRight Instance;
		
		public Window_MainGameHeaderBarRight()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Window_MainGameHeaderBarLeft.Instance == null )
                return false;
            if ( !Window_MainGameHeaderBarLeft.GetShouldDrawAnyHeaderBar() )
                return false;
            if ( !InputCaching.ShowTopRightBar )
                return false;
            if ( Window_MainGameHeaderBarLeft.GetIsOpeningSubMenuBlocked( false ) )
                return false;
            if ( VisCurrent.IsShowingActualEvent )
                return false;
            if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped )
                return false;
            if ( Window_VirtualRealityNameWindow.Instance.IsOpen )
                return false;
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null && lowerMode.HideRightHeaderFull )
                return false;

            return true;
        }

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            if ( !InputCaching.ShowTopRightBar )
                return 0;

            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        private static RectTransform SizingRect = null;

        #region GetXWidthForOtherWindowOffsets
        public static float GetXWidthForOtherWindowOffsets()
        {
            if ( !InputCaching.ShowTopRightBar || SizingRect == null )
                return 0;

            float minX = SizingRect.GetWorldSpaceBottomLeftCorner().x;
            return ArcenUI.Instance.world_TopRight.x - minX;
        }
        #endregion

        #region GetCurrentHeight_Scaled
        public float GetCurrentHeight_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 3; //hidden entirely!

            return 18f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        #region GetHeaderBarCurrentHeight_Scaled
        /// <summary>
        /// Gets the amount of vertical space the header bar will be taking up,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetHeaderBarCurrentHeight_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 14f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bIconBox> bIconBoxPool;
        private static ButtonAbstractBase.ButtonPool<bMapIcon> bMapIconPool;

        public const float POPUP_OFFSET_X = 2f;
        public const float POPUP_OFFSET_Y = -1f;

        public const float ICON_WIDTH_NORMAL = 19f;
        public const float ICON_WIDTH_WIDER = 28.57f;
        public const float ICON_HEIGHT = 17f;
        public const float ICON_SPACING = 1.5f;
        public const float MAP_ICON_SPACING = 3f;
        public const float MAP_ICON_SUBTRACTION_AT_END = -2.5f;
        public const float FIRST_MAP_ICON_POSITION_NO_INT_CLASS = 4f;
        public const float FIRST_MAP_ICON_POSITION_WITH_INT_CLASS = 16f;

        public const float MAP_BUTTON_WIDTH = 24.21f;
        public const float MAP_BUTTON_HEIGHT = 18.9f;
        public const float MAP_BUTTON_Y = -0.2f;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_HeaderBar" ) * 1.2f;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bIconBox.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bIconBoxPool = new ButtonAbstractBase.ButtonPool<bIconBox>( bIconBox.Original, 10, "bIconBox" );
                        bMapIconPool = new ButtonAbstractBase.ButtonPool<bMapIcon>( bMapIcon.Original, 10, "bMapIcon" );
                    }
                }
                #endregion

                this.UpdateIcons();
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bIconBoxPool.Clear( 5 );
                bMapIconPool.Clear( 5 );

                float currentX = GetShouldShowIntelligenceClass() ? -FIRST_MAP_ICON_POSITION_WITH_INT_CLASS : -FIRST_MAP_ICON_POSITION_NO_INT_CLASS;
                float startingX = currentX;
                float currentY = 0f;

                this.DrawMenuButtons( ref currentX, ref currentY, startingX );

                #region Expand or Shrink Height Of This Window
                float widthForWindow = MathA.Abs( currentX ) + EXTRA_SPACING;
                if ( lastWindowWidth != widthForWindow )
                {
                    lastWindowWidth = widthForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.UI_SetWidth( widthForWindow );
                }
                #endregion

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour2_RightHeader && Instance.GetShouldDrawThisFrame() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "UITour_RightHeader_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_RightHeader_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 2, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour2_RightHeader", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.Right );
                }
            }
            private float lastWindowWidth = -1f;

            public const float EXTRA_SPACING = 0f;

            private void DrawMenuButtons( ref float currentX, ref float currentY, float startingX )
            {
                //this whole thing goes from right to left!

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                bool isSomethingCausingAllWindowsToBeHidden = ArcenUI.CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow.Count > 0;
                bool isSomethingCausingAllWindowsToBeHidden_MoreStrict = isSomethingCausingAllWindowsToBeHidden;
                if ( isSomethingCausingAllWindowsToBeHidden )
                {
                    isSomethingCausingAllWindowsToBeHidden = false;
                    foreach ( WindowControllerAbstractBase windowBase in ArcenUI.CurrentlyShownWindowsWith_ShouldCauseAllOtherWindowsToNotShow )
                    {
                        if ( windowBase is Window_PlayerResources )
                            continue;
                        if ( windowBase is Window_PlayerHardware )
                            continue;
                        if ( windowBase is Window_PlayerHistory )
                            continue;
                        if ( windowBase is Window_VictoryPath )
                            continue;
                        isSomethingCausingAllWindowsToBeHidden = true;
                        break;
                    }
                }

                #region End Of Time First-Time Stuff
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime && VisCommands.GetCanSeeEndOfTime( false, false ) )
                {
                    if ( !FlagRefs.HasAlreadyIndicatedTheEndOfTime.DuringGameplay_IsTripped )
                        FlagRefs.HasAlreadyIndicatedTheEndOfTime.TripIfNeeded();

                    if ( SimMetagame.CurrentChapterNumber < 3 && VisCommands.GetCanSeeEndOfTime( true, false ) )
                    {
                        SimMetagame.AdvanceToNextChapter();
                        HandbookRefs.TheNatureOfTheEndOfTime.DuringGame_UnlockIfNeeded( true );
                        HandbookRefs.CallingNewTimelines.DuringGame_UnlockIfNeeded( true );
                        HandbookRefs.NegativeTimelineBleedover.DuringGame_UnlockIfNeeded( true );
                        HandbookRefs.PositiveTimelineBleedover.DuringGame_UnlockIfNeeded( true );
                    }
                }
                #endregion

                #region End Of Time Icon
                if ( !VisCurrent.IsShowingActualEvent && VisCommands.GetCanSeeEndOfTime( false, false ) )  //when showing event camera, do not include this
                {
                    bMapIcon icon = bMapIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_EndOfTime.Icon.GetSpriteForUI() );
                        float mapY = MAP_BUTTON_Y;
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref mapY, false, false, MAP_BUTTON_WIDTH, MAP_BUTTON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isSelectedOrHovered = element.LastHadMouseWithin ||
                                            (!isSomethingCausingAllWindowsToBeHidden_MoreStrict && Engine_HotM.GameMode == MainGameMode.TheEndOfTime);

                                        icon.SetBoxStyle( isSelectedOrHovered ? MapIconStyle.SecondaryHighlighted : MapIconStyle.Secondary );

                                        if ( SharedRenderManagerData.CurrentIndicator == Indicator.EndOfTimeButton )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateEndOfTimeButtonButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateEndOfTimeButtonButton_Text", "AlwaysSame" ), element, SideClamp.Any, TooltipArrowSide.TopRight );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "EndOfTime" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_EndOfTime.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_TheEndOfTime_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleEndOfTimeMode" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_TheEndOfTime_Explanation" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleEndOfTimeMode();
                                    break;
                            }
                        } );
                        currentX -= (MAP_ICON_SPACING + MAP_BUTTON_WIDTH);
                    }
                }
                #endregion

                #region Virtual World Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    SimCommon.GetIsVirtualRealityEstablished() && FlagRefs.HasUnlockedSeeingTheVirtualWorld.DuringGameplay_IsTripped )
                {
                    bMapIcon icon = bMapIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_VirtualWorld.Icon.GetSpriteForUI() );
                        float mapY = MAP_BUTTON_Y;
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref mapY, false, false, MAP_BUTTON_WIDTH, MAP_BUTTON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isSelectedOrHovered = element.LastHadMouseWithin;

                                        if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime && !isSomethingCausingAllWindowsToBeHidden_MoreStrict )
                                        {
                                            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                                            if ( lowerMode != null )
                                            {
                                                switch ( lowerMode.ID )
                                                {
                                                    case "ZodiacNeuronScene":
                                                    case "ZodiacPodScene":
                                                    case "ZodiacHexRewardScene":
                                                        isSelectedOrHovered = true;
                                                        FlagRefs.HasGoneIntoTheVirtualWorld.TripIfNeeded();
                                                        if ( SimMetagame.CurrentChapterNumber < 2 )
                                                            SimMetagame.AdvanceToNextChapter();
                                                        break;
                                                }
                                            }
                                        }
                                        icon.SetBoxStyle( isSelectedOrHovered ? MapIconStyle.SecondaryHighlighted : MapIconStyle.Secondary );

                                        if ( SharedRenderManagerData.CurrentIndicator == Indicator.VirtualWorldButton )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddFormat1( "IndicateVirtualWorldButton_Text", SimMetagame.VREnvironmentName );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateVirtualWorldButton_Text", "AlwaysSame" ), element, SideClamp.Any, TooltipArrowSide.TopRight );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "VirtualWorld" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple, 
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_VirtualWorld.Icon;
                                        novel.TitleUpperLeft.AddRaw( SimMetagame.VREnvironmentName );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleTheZodiacMode" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_VirtualWorld_Explanation" ).Line();

                                        novel.FrameBody.AddFormat2( "ClickToRenameTheVirtualWorld", Lang.GetRightClickText(), SimMetagame.VREnvironmentName, ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        VisCommands.ToggleTheVirtualWorld();
                                    else if ( ExtraData.MouseInput.RightButtonClicked && !isSomethingCausingAllWindowsToBeHidden )
                                        Window_VirtualRealityNameWindow.Instance.Open();
                                    break;
                            }
                        } );
                        currentX -= (MAP_ICON_SPACING + MAP_BUTTON_WIDTH);
                    }
                }
                #endregion

                #region Map Icon
                if ( !VisCurrent.IsShowingActualEvent ) //when showing event camera, do not include this
                {
                    bMapIcon icon = bMapIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Map.Icon.GetSpriteForUI() );
                        float mapY = MAP_BUTTON_Y;
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref mapY, false, false, MAP_BUTTON_WIDTH, MAP_BUTTON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isSelectedOrHovered = element.LastHadMouseWithin ||
                                            (!isSomethingCausingAllWindowsToBeHidden_MoreStrict && Engine_HotM.GameMode == MainGameMode.CityMap && Engine_HotM.CurrentLowerMode == null);

                                        icon.SetBoxStyle( isSelectedOrHovered ? MapIconStyle.PrimaryHighlighted : MapIconStyle.Primary );

                                        if ( SharedRenderManagerData.CurrentIndicator == Indicator.MapButton_StreetSense )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateMapButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateMapButton_Text", "AlwaysSame" ), element, SideClamp.Any, TooltipArrowSide.Right );
                                        }
                                        else if ( SharedRenderManagerData.CurrentIndicator == Indicator.MapButton_Investigate )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateMapButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateMapButton_Text", "AlwaysSame" ), element, SideClamp.Any, TooltipArrowSide.TopRight );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Map" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Map.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_Map_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleMapMode" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_Map_Explanation" ).Line();
                                    }

                                    #region If Already In Map Mode
                                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.MapButton_Investigate )
                                    {
                                        switch ( Engine_HotM.GameMode )
                                        {
                                            case MainGameMode.CityMap:
                                                FlagRefs.IndicateMapButton.UnTripIfNeeded();
                                                break;
                                        }
                                    }
                                    #endregion

                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleCityMapMode();
                                    break;
                            }
                        } );
                        currentX -= (MAP_ICON_SPACING + MAP_BUTTON_WIDTH);
                    }
                }
                #endregion

                float notchX = currentX;

                currentX -= (ICON_SPACING);
                currentX -= (ICON_SPACING);

                {
                    LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                    if ( lowerMode != null && lowerMode.HideRightHeaderExceptMaps )
                        return;
                }

                #region Recent Events Icon
                if ( !VisCurrent.IsShowingActualEvent && !isSomethingCausingAllWindowsToBeHidden && //when showing event camera, do not include this
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //also not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_RecentEvents.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_RecentEvents.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "RecentEvents" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_RecentEvents.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_RecentEvents_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "RecentEvents" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_RecentEvents_Explanation" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.RecentEvents );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region History Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    SimMetagame.CurrentChapterNumber > 0 && //also don't show this until chapter 1
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //and not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_History.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_PlayerHistory.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "History" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_History.Icon;
                                        novel.TitleUpperLeft.AddLang( "Header_History_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleHistory" );
                                        if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime )
                                        {
                                            novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                .AddLangWithFirstLineBold( "Header_History_City_Explanation1" ).Line()
                                                .AddLang( "Header_History_City_Explanation2" ).Line();

                                            novel.FrameBody.AddRightClickFormat( "ClickForNextActiveNPCCombatant", ColorTheme.SoftGold ).AddTooltipHotkeySuffixSoftGold( "NextActiveNPCCombatant" ).Line();
                                        }
                                        else
                                        {
                                            novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                .AddLang( "Header_History_EndOfTime_Explanation" ).Line();
                                        }
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.History );
                                    else if ( ExtraData.MouseInput.RightButtonClicked && !isSomethingCausingAllWindowsToBeHidden )
                                    {
                                        if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime )
                                            SimCommon.CycleThroughNPCUnits( true, ( ISimNPCUnit u ) => !u.UnitType.DeathsCountAsMurders && !(u.Stance?.IsConsideredBasicGuard ?? false) ); //NextActiveNPCCombatant
                                    }
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Hardware Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //also not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Hardware.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_PlayerHardware.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Hardware" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Hardware.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_Hardware_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleHardware" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLangWithFirstLineBold( "HeaderBar_Hardware_Explanation1" ).Line()
                                            .AddLang( "HeaderBar_Hardware_Explanation2" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.Hardware );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Resource Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //also not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Inventory.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_PlayerResources.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Resource" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Inventory.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_Inventory_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "Inventory" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLangWithFirstLineBold( "HeaderBar_Inventory_Explanation1" ).Line()
                                            .AddLang( "HeaderBar_Inventory_Explanation2" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.Resources );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Handbook Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    MachineHandbookEntry.DuringGame_UnlockedEntries > 0 )
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Handbook.Icon.GetSpriteForUI() );
                        float width = MachineHandbookEntry.DuringGame_UnreadEntries > 0 ? ICON_WIDTH_WIDER : ICON_WIDTH_NORMAL;
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, width, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        int countUnread = MachineHandbookEntry.DuringGame_UnreadEntries;

                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_Handbook.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );

                                        if ( countUnread > 0 )
                                        {
                                            if ( countUnread > 9 )
                                                ExtraData.Buffer.StartSize80();
                                            ExtraData.Buffer.AddRaw( countUnread.ToString() );
                                            icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.Alert );
                                        }
                                        else
                                        {
                                            icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Handbook" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Handbook.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_Handbook_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "Handbook" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_Handbook_Explanation" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.MachineHandbook );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + width);
                    }
                }
                #endregion

                #region Timeline Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    Engine_HotM.GameMode == MainGameMode.TheEndOfTime && //only show this in the end of time
                    !isSomethingCausingAllWindowsToBeHidden )
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Timeline.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin ||
                                            (Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.TimelinesSidebar);

                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Timeline" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Timeline.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_TimelinesSidebar_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleTimelinesSidebar" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_TimelinesSidebar_Explanation" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleTimelinesSidebar();
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Networks Icon
                if ( !VisCurrent.IsShowingActualEvent && SimMetagame.CurrentChapterNumber > 0 && //when showing event camera, do not include this; also not in chapter zero
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime && //also not in the end of time
                    !isSomethingCausingAllWindowsToBeHidden && GameSettings.Current.GetBool( "ShowNetworkSidebarIcon" ) )
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Networks.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin ||
                                            (Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.NetworksSidebar);

                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Networks" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                            TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Networks.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_NetworksSidebar_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleNetworksSidebar" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_NetworksSidebar_Explanation" ).Line();

                                        int subnetCount = 0;
                                        foreach ( MachineSubnet subnet in SimCommon.Subnets )
                                        {
                                            if ( subnet.SubnetNodes.Count > 0 )
                                                subnetCount++;
                                        }

                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Subnets", ColorTheme.DataLabelWhite )
                                            .AddRaw( subnetCount.ToStringThousandsWhole(), ColorTheme.DataBlue ).Line();

                                        novel.FrameBody.AddRightClickFormat( "ClickForNextSubnet", ColorTheme.SoftGold ).AddTooltipHotkeySuffixSoftGold( "NextSubnet" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        VisCommands.ToggleNetworksSidebar();
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                        SimCommon.CycleThroughSubnets( ( MachineSubnet s ) => true ); //NextSubnet
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region CultActions Icon
                if ( !VisCurrent.IsShowingActualEvent && SimMetagame.CurrentChapterNumber > 1 && //when showing event camera, do not include this; also not in chapter zero
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime && //also not in the end of time
                    !isSomethingCausingAllWindowsToBeHidden && FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_CultActions.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin ||
                                            (Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.CultActionsSidebar);

                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "CultActions" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                            TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_CultActions.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_CultActionsSidebar_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleCultActionsSidebar" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_CultActionsSidebar_Explanation" ).Line();

                                        novel.Main.AddSpriteStyled_NoIndent( ResourceRefs.CultLoyalty.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.CultLoyalty.IconColorHex )
                                            .AddRawAndAfterLineItemHeader( ResourceRefs.CultLoyalty.GetDisplayName(), ResourceRefs.CultLoyalty.IconColorHex )
                                            .AddRaw( ResourceRefs.CultLoyalty.Current.ToStringThousandsWhole(), ColorTheme.DataLabelWhite ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.CultActions );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Structures With Complaints Icon
                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped && //only if it's ready
                    !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //also not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_StructuresWithComplaints.Icon.GetSpriteForUI() );
                        int anyComplaintCount = SimCommon.StructuresWithAnyFormOfIssue.Count;
                        float width = anyComplaintCount > 0 ? ICON_WIDTH_WIDER : ICON_WIDTH_NORMAL;
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, width, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            int visibleComplaintCount = SimCommon.StructuresWithVisibleComplaints.Count;
                            int lowerGradeComplaintCount = SimCommon.StructuresWithLowerGradeComplaints.Count;
                            int damagedCount = SimCommon.StructuresWithDamage.Count;
                            int constructionCount = SimCommon.StructuresWithAnyFormOfConstruction.Count;
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin ||
                                            (Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.NetworksSidebar); //todo

                                        if ( anyComplaintCount > 0 )
                                        {
                                            if ( anyComplaintCount > 9 )
                                                ExtraData.Buffer.StartSize80();
                                            ExtraData.Buffer.AddRaw( anyComplaintCount.ToStringWholeBasic() );
                                            icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.RedHighlighted : IconBoxStyle.Red );
                                            icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.Alert );
                                        }
                                        else
                                        {

                                            icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                            icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:                                                                        
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Complaints" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_StructuresWithComplaints.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_StructuresWithComplaints_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleStructuresWithComplaints" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_StructuresWithComplaints_Explanation" ).Line();

                                        novel.Main.StartStyleLineHeightA();
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "StructuresWithMajorComplaints", ColorTheme.DataLabelWhite )
                                            .AddRaw( visibleComplaintCount.ToStringThousandsWhole(), visibleComplaintCount > 0 ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        if ( lowerGradeComplaintCount > 0 )
                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "StructuresWithMinorComplaints", ColorTheme.DataLabelWhite )
                                                .AddRaw( lowerGradeComplaintCount.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();
                                        if ( constructionCount > 0 )
                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "StructuresWithAnyConstruction", ColorTheme.DataLabelWhite )
                                                .AddRaw( constructionCount.ToStringThousandsWhole(), ColorTheme.RedOrange2 ).Line();
                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "DamagedStructures", ColorTheme.DataLabelWhite )
                                            .AddRaw( damagedCount.ToStringThousandsWhole(), damagedCount > 0 ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.EndLineHeight();

                                        novel.FrameBody.AddRightClickFormat( "ClickForNextStructureWithAnyIssue", ColorTheme.SoftGold ).AddTooltipHotkeySuffixSoftGold( "NextStructureWithAnyIssue" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.StructuresWithComplaints );
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked && !isSomethingCausingAllWindowsToBeHidden )
                                        SimCommon.CycleThroughMachineStructuresWithAnyIssue( false, ( MachineStructure s ) => true );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + width);
                    }
                }
                #endregion

                #region Victory Path Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    SimMetagame.CurrentChapterNumber > 0 && //also don't show this until chapter 1
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime ) //also not in the end of time
                {
                    bIconBox icon = bIconBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_VictoryPath.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_NORMAL, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin || Window_VictoryPath.Instance.IsOpen;
                                        icon.SetBoxStyle( isHoveredOrSelected ? IconBoxStyle.NormalHighlighted : IconBoxStyle.Normal );
                                        icon.SetNumberPlateStyle( IconBoxNumberPlateStyle.None );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "VictoryPath" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_VictoryPath.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_VictoryPath_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleVictoryPath" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLangWithFirstLineBold( "HeaderBar_VictoryPath_Explanation1" ).Line()
                                            .AddLang( "HeaderBar_VictoryPath_Explanation2" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.VictoryPath );
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_NORMAL);
                    }
                }
                #endregion

                #region Forces Icon
                if ( !VisCurrent.IsShowingActualEvent && //when showing event camera, do not include this
                    Engine_HotM.GameMode != MainGameMode.TheEndOfTime &&  //also not in the end of time
                    !isSomethingCausingAllWindowsToBeHidden )
                {
                    bForcesIconBox icon = bForcesIconBox.Sole;
                    if ( icon != null )
                    {
                        icon.SetSpriteIfNeeded( IconRefs.Header_Forces.Icon.GetSpriteForUI() );
                        icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, ICON_WIDTH_WIDER, ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                        icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHoveredOrSelected = element.LastHadMouseWithin ||
                                            (Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.ForcesSidebar);

                                        string colorHex = string.Empty;
                                        bool hasGaps = SimCommon.GetHasRosterGaps();

                                        if ( !hasGaps )
                                            colorHex = ColorTheme.ForcesBlueHighlight;
                                        else
                                        {
                                            if ( SimCommon.TotalCapacityUsed_Androids < MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                                                colorHex = ColorTheme.ForcesRedHighlight;
                                        }

                                        int forcesCount = World.Forces.GetMachineUnitsByID().Count + World.Forces.GetMachineVehiclesByID().Count;
                                        if ( forcesCount > 9 )
                                            ExtraData.Buffer.StartSize80();
                                        else
                                            ExtraData.Buffer.StartSize90();

                                        ExtraData.Buffer.AddRaw( forcesCount.ToStringWholeBasic(), colorHex );

                                        if ( hasGaps )
                                        {
                                            if ( SimCommon.TotalCapacityUsed_Androids < MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                                                icon.SetBoxStyle( isHoveredOrSelected ? ForcesBoxStyle.RedHighlighted : ForcesBoxStyle.Red );
                                            else
                                                icon.SetBoxStyle( isHoveredOrSelected ? ForcesBoxStyle.NormalHighlighted : ForcesBoxStyle.Normal );
                                        }
                                        else
                                            icon.SetBoxStyle( isHoveredOrSelected ? ForcesBoxStyle.BlueHighlighted : ForcesBoxStyle.Blue );

                                        if ( SharedRenderManagerData.CurrentIndicator == Indicator.ForcesSidebar )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateForcesSidebar_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateForcesSidebar_Text", "AlwaysSame" ), element, SideClamp.Any, TooltipArrowSide.TopRight );
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "Forces" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                                        TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                                    {
                                        novel.Icon = IconRefs.Header_Forces.Icon;
                                        novel.TitleUpperLeft.AddLang( "HeaderBar_ForcesSidebar_Top" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleForcesSidebar" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddLang( "HeaderBar_ForcesSidebar_Explanation" ).Line();

                                        novel.Main.StartStyleLineHeightA();

                                        novel.Main.AddBoldRawAndAfterLineItemHeader( MathRefs.MaxAndroidCapacity.GetDisplayName(), ColorTheme.DataLabelWhite )
                                            .AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Androids.ToStringThousandsWhole(), MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt,
                                            SimCommon.TotalCapacityUsed_Androids < MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.AddBoldRawAndAfterLineItemHeader( MathRefs.MaxMechCapacity.GetDisplayName(), ColorTheme.DataLabelWhite )
                                            .AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Mechs.ToStringThousandsWhole(), MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt,
                                            SimCommon.TotalCapacityUsed_Mechs < MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.AddBoldRawAndAfterLineItemHeader( MathRefs.MaxVehicleCapacity.GetDisplayName(), ColorTheme.DataLabelWhite )
                                            .AddFormat2( "OutOF", SimCommon.TotalCapacityUsed_Vehicles.ToStringThousandsWhole(), MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt,
                                            SimCommon.TotalCapacityUsed_Vehicles < MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.AddBoldRawAndAfterLineItemHeader( MathRefs.BulkUnitCapacity.GetDisplayName(), ColorTheme.DataLabelWhite )
                                            .AddFormat2( "OutOF", SimCommon.TotalBulkUnitSquadCapacityUsed.ToStringThousandsWhole(), MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt,
                                            SimCommon.TotalBulkUnitSquadCapacityUsed < MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.EndLineHeight(); //before the last line so we get separation
                                        novel.Main.AddBoldRawAndAfterLineItemHeader( MathRefs.CapturedUnitCapacity.GetDisplayName(), ColorTheme.DataLabelWhite )
                                            .AddFormat2( "OutOF", SimCommon.TotalCapturedUnitSquadCapacityUsed.ToStringThousandsWhole(), MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt,
                                            SimCommon.TotalCapturedUnitSquadCapacityUsed < MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt ? ColorTheme.RedOrange2 : ColorTheme.DataBlue ).Line();
                                        novel.Main.EndLineHeight();

                                        novel.FrameBody.AddRightClickFormat( "ClickForNextMachineActor", ColorTheme.SoftGold ).AddTooltipHotkeySuffixSoftGold( "NextMachineActor" ).Line();
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.Forces );
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                        SimCommon.CycleThroughMachineActors( true, ( ISimMachineActor a ) => true ); //NextMachineActor
                                    break;
                            }
                        } );
                        currentX -= (ICON_SPACING + ICON_WIDTH_WIDER);
                    }
                }
                else
                {
                    bForcesIconBox icon = bForcesIconBox.Sole;
                    if ( icon != null )
                        icon.Clear();

                    currentX -= 4f; //since this is not here, extra space is needed.
                }
                #endregion

                {
                    notchX = -notchX;

                    RectTransform rightBG = this.Element.RelatedImages[0].rectTransform;
                    rightBG.anchoredPosition = new Vector2( startingX + MAP_ICON_SPACING, 1.5f );
                    rightBG.UI_SetWidth( notchX + startingX - MAP_ICON_SUBTRACTION_AT_END );

                    notchX += 1.5f;

                    float finalWidth = -currentX;

                    RectTransform leftBG = this.Element.RelatedImages[1].rectTransform;
                    leftBG.UI_SetWidth( finalWidth - ( notchX - 0.5f ) );

                    SizingRect = leftBG;
                }
            }
        }

        #region bIconBoxBase
        public abstract class bIconBoxBase : ButtonAbstractBaseWithImage
        {
            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }

            #region SetBoxStyle
            public IconBoxStyle lastBoxStyle = IconBoxStyle.Normal;

            public ArcenUI_Button But;

            public void SetBoxStyle( IconBoxStyle Style )
            {
                if ( lastBoxStyle == Style )
                    return;

                if ( this.But == null )
                {
                    this.But = this.Element as ArcenUI_Button;
                    if ( this.But == null )
                        return;
                }

                lastBoxStyle = Style;

                switch ( Style )
                {
                    case IconBoxStyle.Normal:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //normal
                        this.image.material = null; //no shader
                        break;
                    case IconBoxStyle.NormalHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //normal
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[2]; //normal highlight
                        this.image.material = this.But.ReferenceImagesNormalMaterials[0]; //blue
                        break;
                    case IconBoxStyle.Red:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //red
                        this.image.material = null; //no shader
                        break;
                    case IconBoxStyle.RedHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //red
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[3]; //red highlight
                        this.image.material = this.But.ReferenceImagesNormalMaterials[1]; //red
                        break;
                }
            }
            #endregion

            #region SetNumberPlateStyle
            public IconBoxNumberPlateStyle lastNumberPlateStyle = IconBoxNumberPlateStyle.None;

            public void SetNumberPlateStyle( IconBoxNumberPlateStyle Style )
            {
                if ( lastNumberPlateStyle == Style )
                    return;

                lastNumberPlateStyle = Style;

                switch ( Style )
                {
                    case IconBoxNumberPlateStyle.None:
                        this.Element.RelatedImages[2].enabled = false;
                        this.image.rectTransform.anchoredPosition = new Vector2( 1.5f, -8.499908f );
                        break;
                    case IconBoxNumberPlateStyle.Alert:
                        this.Element.RelatedImages[2].enabled = true;
                        this.image.rectTransform.anchoredPosition = new Vector2( 3.5f, -8.499908f );
                        break;
                }
            }
            #endregion
        }
        #endregion

        #region bIconBox
        public class bIconBox : bIconBoxBase
        {
            public static bIconBox Original;
            public bIconBox() { if ( Original == null ) Original = this; }
        }
        #endregion

        public enum IconBoxStyle
        {
            Normal,
            NormalHighlighted,
            Red,
            RedHighlighted
        }

        public enum IconBoxNumberPlateStyle
        {
            None,
            Alert
        }

        #region bForcesIconBox
        public class bForcesIconBox : ButtonAbstractBaseWithImage
        {
            public static bForcesIconBox Sole;
            public bForcesIconBox() { if ( Sole == null ) Sole = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                if ( lowerMode != null && lowerMode.HideRightHeaderExceptMaps )
                    return true;

                return this.UIDataController == null;
            }

            public ArcenUI_Button But;

            #region SetBoxStyle
            public ForcesBoxStyle lastBoxStyle = ForcesBoxStyle.Normal;

            public void SetBoxStyle( ForcesBoxStyle Style )
            {
                if ( lastBoxStyle == Style )
                    return;

                if ( this.But == null )
                {
                    this.But = this.Element as ArcenUI_Button;
                    if ( this.But == null )
                        return;
                }

                lastBoxStyle = Style;

                switch ( Style )
                {
                    case ForcesBoxStyle.Normal:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //normal
                        this.image.material = null; //no shader
                        break;
                    case ForcesBoxStyle.NormalHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //normal
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[4]; //blue highlight
                        this.image.material = this.But.ReferenceImagesHoverMaterials[0]; //blue glow
                        break;
                    case ForcesBoxStyle.Red:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[2]; //red
                        this.image.material = this.But.ReferenceImagesNormalMaterials[1]; //red
                        break;
                    case ForcesBoxStyle.RedHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[2]; //red
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[3]; //red highlight
                        this.image.material = this.But.ReferenceImagesHoverMaterials[1]; //red glow
                        break;
                    case ForcesBoxStyle.Blue:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //blue
                        this.image.material = this.But.ReferenceImagesNormalMaterials[0]; //blue
                        break;
                    case ForcesBoxStyle.BlueHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //blue
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[4]; //blue highlight
                        this.image.material = this.But.ReferenceImagesHoverMaterials[0]; //blue glow
                        break;
                }
            }
            #endregion
        }
        #endregion

        public enum ForcesBoxStyle
        {
            Normal,
            NormalHighlighted,
            Red,
            RedHighlighted,
            Blue,
            BlueHighlighted,
        }

        #region bMapIcon
        public class bMapIcon : ButtonAbstractBaseWithImage
        {
            public static bMapIcon Original;
            public bMapIcon() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }

            public ArcenUI_Button But;

            #region SetBoxStyle
            public MapIconStyle lastBoxStyle = MapIconStyle.Primary;

            public void SetBoxStyle( MapIconStyle Style )
            {
                if ( lastBoxStyle == Style )
                    return;

                if ( this.But == null )
                {
                    this.But = this.Element as ArcenUI_Button;
                    if ( this.But == null )
                        return;
                }

                lastBoxStyle = Style;

                switch ( Style )
                {
                    case MapIconStyle.Primary:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //primary
                        this.image.material = null; //no shader
                        break;
                    case MapIconStyle.PrimaryHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[0]; //primary
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[2]; //primary highlight
                        this.image.material = this.But.ReferenceImagesHoverMaterials[0]; //highlight glow
                        break;
                    case MapIconStyle.Secondary:
                        this.Element.RelatedImages[0].enabled = false;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //secondary
                        this.image.material = null; //no shader
                        break;
                    case MapIconStyle.SecondaryHighlighted:
                        this.Element.RelatedImages[0].enabled = true;
                        this.Element.RelatedImages[1].sprite = this.Element.RelatedSprites[1]; //secondary
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[3]; //secondary highlight
                        this.image.material = this.But.ReferenceImagesHoverMaterials[0]; //highlight glow
                        break;
                }
            }
            #endregion
        }
        #endregion

        public enum MapIconStyle
        {
            Primary,
            PrimaryHighlighted,
            Secondary,
            SecondaryHighlighted,
        }

        #region GetShouldShowIntelligenceClass
        public static bool GetShouldShowIntelligenceClass()
        {
            if ( SimMetagame.CurrentChapterNumber > 1 )
                return true;
            if ( FlagRefs.Ch1_BuildingABetterBrain.DuringGameplay_TurnStarted > 0 )
                return true;
            return false;
        }
        #endregion

        #region bIntelligenceClass
        public class bIntelligenceClass : ButtonAbstractBaseWithImage
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                bool isHovered = this.Element.LastHadMouseWithin;

                Buffer.AddRaw( SimMetagame.IntelligenceClass.ToString(), ColorTheme.GetIntelligenceBlue( isHovered ) );
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderRight", "IntClass" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple,
                    TooltipExtraText.None, TooltipExtraRules.None ) ) //was MustBeToLeftOfTaskStack
                {
                    Int64 neuralProcessing = 0;
                    foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                        neuralProcessing += timeline.NeuralProcessing;

                    ISeverity nextCutoff = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );
                    int percentage = MathA.IntPercentage( neuralProcessing, nextCutoff.CutoffInt );

                    novel.TitleUpperLeft.AddLang( "IntelligenceClass" );
                    novel.TitleUpperRight.AddRaw( SimMetagame.IntelligenceClass.ToString() );
                    {
                        ArcenDoubleCharacterBuffer Buffer = novel.TitleLowerLeft;
                        Buffer.AddFormat1( "NeuralProcessing_PercentageToNext", MathA.ClampInt( 100 - percentage, 1, 100 ).ToStringIntPercent() );
                    }
                    {
                        ArcenDoubleCharacterBuffer Buffer = novel.Main;
                        Buffer.StartColor( ColorTheme.NarrativeColor );
                        switch ( SimMetagame.IntelligenceClass )
                        {
                            case 0:
                            case 1:
                                Buffer.AddLang( "IntelligenceClass1_Description" ).Line();
                                break;
                            case 2:
                                Buffer.AddLang( "IntelligenceClass2_Description" ).Line();
                                break;
                            case 3:
                                Buffer.AddLang( "IntelligenceClass3_Description" ).Line();
                                break;
                            case 4:
                                Buffer.AddLang( "IntelligenceClass4_Description" ).Line();
                                break;
                            case 5:
                            default:
                                Buffer.AddLang( "IntelligenceClass5_Description" ).Line();
                                break;
                        }
                        Buffer.AddBoldLangAndAfterLineItemHeader( "NeuralProcessingForUpgrade" ).AddFormat2( "OutOF",
                            neuralProcessing.ToStringLargeNumberAbbreviated(), nextCutoff.CutoffInt.ToStringLargeNumberAbbreviated() )
                            .Space1x().AddFormat1( "ParentheticalProgress", percentage.ToStringIntPercent(), ColorTheme.DataGood ).Line();
                    }
                    novel.Icon = IconRefs.Header_Menu.Icon;
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return !GetShouldShowIntelligenceClass();
            }
        }
        #endregion
    }
}

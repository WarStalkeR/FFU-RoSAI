using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_BuildSidebar : WindowControllerAbstractBase
    {
        public static Window_BuildSidebar Instance;
        public override bool PutMeOnTheEscapeCloseStack => false; //otherwise things get very confused!
		public Window_BuildSidebar()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.SelectedMachineActionMode == null ||
                Engine_HotM.SelectedMachineActionMode?.ID != "BuildMode" )
                return false;

            if ( VisCurrent.IsShowingActualEvent )
                return false; //don't show when in a city event

            if ( Window_MainGameHeaderBarLeft.Instance == null )
                return false;
            if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                return false; //if the header bar is not showing for whatever reason, then also don't show ourselves
            if ( SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false )
                return false;
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }
            if ( SimCommon.CurrentSimpleChoice != null )
                return false;
            if ( Window_RewardWindow.Instance?.IsOpen ?? false )
                return false;
            if ( Window_NetworkNameWindow.Instance?.IsOpen ?? false )
                return false;
            if ( !Window_RadialMenu.Instance?.GetShouldDrawThisFrame() ?? false )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( Window_Debate.Instance?.IsOpen ?? false )
                return false;
            if ( SimCommon.ShouldBeShowingPostGoalScreen )
                return false;

            return true;
        }
        #endregion

        public override void ChildOnShowAfterNotShowing()
        {
            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );
            FilterText = string.Empty;
            FilteredToInternalRobotics = null;

            base.ChildOnShowAfterNotShowing();
        }

        public static string FilterText = string.Empty;
        public static UpgradeInt FilteredToInternalRobotics = null;

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "BuildMenu" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }

        public class tSubHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                MachineBuildModeCategory category = BuildModeHandler.currentCategory;
                if ( category != null )
                {
                    if ( !category.IsSuggested && !category.IsAll && !category.IsIconSkipped )
                        Buffer.AddSpriteStyled_NoIndent( category.Icon, AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();

                    Buffer.AddRaw( category.GetDisplayName() ).Space1x().AddFormat1( "Parenthetical",
                        category.GetEntryCount() );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }

        #region GetXWidthForOtherWindowOffsets
        public static float GetXWidthForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            float height = SizingRect.GetWorldSpaceSize().x;
            //height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        #region GetMinXForTooltips
        public static float GetMinXForTooltips()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            return SizingRect.GetWorldSpaceTopRightCorner().x;
        }
        #endregion

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_BuildSidebar.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bBuildItemTwoLines> bBuildItemTwoLinesPool;
            private static ButtonAbstractBase.ButtonPool<bBuildItemOneLine> bBuildItemOneLinePool;
            private static ButtonAbstractBase.ButtonPool<bCategoryNormal> bCategoryNormalPool;
            private static ButtonAbstractBase.ButtonPool<bCategorySuggested> bCategorySuggestedPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Engine_HotM.SelectedMachineActionMode = null;
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bBuildItemTwoLines.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bBuildItemTwoLinesPool = new ButtonAbstractBase.ButtonPool<bBuildItemTwoLines>( bBuildItemTwoLines.Original, 10, "bBuildItemTwoLines" );
                            bBuildItemOneLinePool = new ButtonAbstractBase.ButtonPool<bBuildItemOneLine>( bBuildItemOneLine.Original, 2, "bBuildItemOneLine" );
                            bCategoryNormalPool = new ButtonAbstractBase.ButtonPool<bCategoryNormal>( bCategoryNormal.Original, 10, "bCategoryNormal" );
                            bCategorySuggestedPool = new ButtonAbstractBase.ButtonPool<bCategorySuggested>( bCategorySuggested.Original, 3, "bCategorySuggested" );
                        }
                    }
                    #endregion

                    bBuildItemTwoLinesPool.Clear( 60 );
                    bBuildItemOneLinePool.Clear( 60 );
                    bCategoryNormalPool.Clear( 50 );
                    bCategorySuggestedPool.Clear( 50 );

                    RectTransform rTran = (RectTransform)bBuildItemTwoLines.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = topBuffer;

                    if ( BuildModeHandler.currentCategory == null || !BuildModeHandler.currentCategory.GetIsReadyToDrawNow() )
                    {
                        switch ( Engine_HotM.GameMode )
                        {
                            case MainGameMode.Streets:
                            case MainGameMode.CityMap:
                                {
                                    if ( CommonRefs.Main_Recommended.GetIsReadyToDrawNow() )
                                        BuildModeHandler.currentCategory = CommonRefs.Main_Recommended;
                                    else if ( CommonRefs.Main_Hand.GetIsReadyToDrawNow() )
                                        BuildModeHandler.currentCategory = CommonRefs.Main_Hand;
                                    else if ( CommonRefs.Main_All.GetIsReadyToDrawNow() )
                                        BuildModeHandler.currentCategory = CommonRefs.Main_All;
                                    else
                                        BuildModeHandler.currentCategory = null;
                                }
                                break;
                            default:
                                BuildModeHandler.currentCategory = null;
                                break;
                        }
                    }

                    this.OnUpdate_Content();

                    if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                        Engine_HotM.SelectedMachineActionMode = null;

                    #region Positioning Logic
                    //Now size the parent, called Content, to get scrollbars to appear if needed.
                    Vector2 sizeDelta = rTran.sizeDelta;
                    sizeDelta.y = MathA.Abs( runningY );
                    rTran.sizeDelta = sizeDelta;
                    #endregion

                    SizingRect = this.Element.RelevantRect;
                }
            }

            public const float CATEGORY_ICON_X = 180.3f;
            public const float CATEGORY_ICON_Y_START = -19.2f;
            public const float CATEGORY_ICON_WIDTH = 42.77f;
            public const float CATEGORY_ICON_HEIGHT = 34f;
            public const float CATEGORY_ICON_SUGGESTED_HEIGHT = 27.1f;
            public const float CATEGORY_ICON_Y_SPACING = 1f;

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 400; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load
            private const float MAIN_CONTENT_WIDTH = 155f;

            public const float CONTENT_ROW_HEIGHT_TWO_LINES = 28f;
            public const float CONTENT_ROW_HEIGHT_ONE_LINE = 24f;
            public const float CONTENT_ROW_SPACING = 4f;

            protected float leftBuffer = 5;
            protected float topBuffer = -5.5f;

            #region CalculateBoundsTwoLines
            protected void CalculateBoundsTwoLines( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW_HEIGHT_TWO_LINES );

                innerY -= CONTENT_ROW_HEIGHT_TWO_LINES + CONTENT_ROW_SPACING;
            }
            #endregion

            #region CalculateBoundsSingleLine
            protected void CalculateBoundsSingleLine( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW_HEIGHT_ONE_LINE );

                innerY -= CONTENT_ROW_HEIGHT_ONE_LINE + CONTENT_ROW_SPACING;
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                bool isInTheZodiac = (Engine_HotM.CurrentLowerMode?.ID??string.Empty) == "ZodiacNeuronScene";

                #region Draw Category Icons
                {
                    float currentY = CATEGORY_ICON_Y_START;
                    foreach ( MachineBuildModeCategory category in SimCommon.ActiveBuildModeCategories.GetDisplayList() )
                    {
                        if ( isInTheZodiac )
                        {
                            if ( !category.IsAvailableInTheZodiac )
                                continue;
                        }
                        else
                        {
                            if ( !category.IsAvailableInMainGame )
                                continue;
                        }

                        if ( category.IsSuggested || category.IsAll || category.IsIconSkipped )
                            DrawCategoryIconSuggested( category, ref currentY );
                        else
                            DrawCategoryIconNormal( category, ref currentY );
                    }
                }
                #endregion

                MachineBuildModeCategory currentCategory = BuildModeHandler.currentCategory;
                if ( currentCategory != null )
                {
                    List<MachineJob> jobList = isInTheZodiac ? currentCategory.DuringGame_VisibleZodiacJobs.GetDisplayList() : 
                        currentCategory.DuringGame_VisibleMainJobs.GetDisplayList();

                    if ( !FilterText.IsEmpty() || FilteredToInternalRobotics != null )
                    {
                        foreach ( SortedMachineJob job in JobCollectionRefs.All.JobList )
                        {
                            if ( !job.Type.DuringGame_IsUnlocked() )
                                continue;

                            HandleSpecificJob( job.Type );
                        }
                    }
                    else
                    {
                        foreach ( MachineJob job in jobList )
                        {
                            HandleSpecificJob( job );
                        }
                    }

                    List<MachineStructureType> structureList = isInTheZodiac ? currentCategory.DuringGame_VisibleZodiacStructures.GetDisplayList() : 
                        currentCategory.DuringGame_VisibleMainStructuresAll.GetDisplayList();

                    MachineStructureType deleteFunction = null;
                    MachineStructureType pauseFunction = null;

                    foreach ( MachineStructureType structureType in structureList )
                    {
                        if ( structureType.IsTheDeleteFunction )
                        {
                            deleteFunction = structureType;
                            continue;
                        } 
                        else if ( structureType.IsThePauseFunction )
                        {
                            pauseFunction = structureType;
                            continue;
                        }
                        else
                        {
                            if ( !FilterText.IsEmpty() )
                            {
                                if ( !structureType.GetMatchesSearchString( FilterText ) )
                                    continue;
                            }
                            if ( FilteredToInternalRobotics != null )
                                continue;

                            this.CalculateBoundsTwoLines( out Rect bounds, ref runningY );
                            if ( bounds.yMax < minYToShow )
                                continue; //it's scrolled up far enough we can skip it, yay!
                            if ( bounds.yMax > maxYToShow )
                                continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                            bBuildItemTwoLines row = bBuildItemTwoLinesPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                break; //this was just time-slicing, so ignore that failure for now
                            bBuildItemTwoLinesPool.ApplySingleItemInRow( row, bounds.x, bounds.y );

                            row.Assign( structureType, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            string topColor = string.Empty;
                                            int targetImageIndex = 0;
                                            if ( !structureType.GetCanAfford( null ) )
                                            {
                                                topColor = ColorTheme.GetCannotAfford( isHovered );
                                                targetImageIndex = 4;
                                            }
                                            else if ( BuildModeHandler.TargetingStructure == structureType )
                                            {
                                                topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                targetImageIndex = 3;
                                            }
                                            else
                                                topColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                            ExtraData.Buffer.StartColor( topColor ).AddRaw( structureType.GetDisplayName() );

                                            row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[targetImageIndex] );
                                        }
                                        break;
                                    case UIAction.GetOtherTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            switch ( ExtraData.Int )
                                            {
                                                case 0:
                                                    {
                                                        string iconColor = string.Empty;
                                                        if ( !structureType.GetCanAfford( null ) )
                                                            iconColor = ColorTheme.GetCannotAfford( isHovered );
                                                        else if ( BuildModeHandler.TargetingStructure == structureType )
                                                            iconColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                        else
                                                            iconColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                                        ExtraData.Buffer.AddSpriteStyled_NoIndent( structureType.Icon, AdjustedSpriteStyle.InlineLarger1_2, iconColor );
                                                    }
                                                    break;
                                                case 1:
                                                    {
                                                        string topColor = string.Empty;
                                                        if ( !structureType.GetCanAfford( null ) )
                                                        {
                                                            topColor = ColorTheme.GetCannotAfford( isHovered );
                                                            ExtraData.Buffer.StartSize125().StartColor( topColor );
                                                            structureType.WriteResourceCostsBrief( ExtraData.Buffer, ColorTheme.GetCanAfford( isHovered ), ColorTheme.GetCannotAfford( isHovered ) );
                                                        }
                                                        else if ( BuildModeHandler.TargetingStructure == structureType )
                                                        {
                                                            topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                            ExtraData.Buffer.StartSize125().StartColor( topColor );
                                                            structureType.WriteResourceCostsBrief( ExtraData.Buffer, ColorTheme.GetCanAfford( isHovered ), ColorTheme.GetCannotAfford( isHovered ) );
                                                        }
                                                        else
                                                        {
                                                            topColor = ColorTheme.GetGray( isHovered );
                                                            ExtraData.Buffer.StartSize125().StartColor( topColor );
                                                            structureType.WriteResourceCostsBrief( ExtraData.Buffer, ColorTheme.GetCanAfford( isHovered ), ColorTheme.GetCannotAfford( isHovered ) );
                                                        }

                                                        if ( structureType.EstablishesNetworkOnBuild )
                                                            ExtraData.Buffer.Space1x().StartSize80().AddFormat1( "BuildAdditionalSuggestedCount",
                                                                1, ColorTheme.GetSuggestedCount( isHovered ) ).EndSize();
                                                    }
                                                    break;
                                                default:
                                                    ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                    break;
                                            }
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( structureType != null )
                                            {
                                                structureType.RenderStructureTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipInstruction.ForConstruction,
                                                    TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfBuildMenu );

                                                BuildModeHandler.LastHoveredJob = null;
                                                Engine_HotM.HoveredJob = null;
                                                BuildModeHandler.LastHoveredStructureType = structureType;
                                                BuildModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                            }
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            if ( structureType != null )
                                            {
                                                if ( InputCaching.IsInInspectMode_Any )
                                                {
                                                }
                                                else
                                                {
                                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                                    {
                                                        if ( !structureType.GetCanAfford( null ) )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }

                                                        if ( BuildModeHandler.TargetingStructure == structureType )
                                                            BuildModeHandler.ClearAllTargeting();
                                                        else
                                                        {
                                                            BuildModeHandler.ClearAllTargeting();
                                                            BuildModeHandler.TargetingStructure = structureType;
                                                        }
                                                    }
                                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                                    {
                                                        if ( structureType.DuringGame_NumberFunctional.Display > 0 || structureType.DuringGame_NumberUnderConstruction.Display > 0 )
                                                            SimCommon.CycleThroughMachineStructures( true, ( MachineStructure s ) => s.Type == structureType );
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                }
                            } );
                        }
                    }

                    foreach ( Investigation investigation in currentCategory.DuringGame_VisibleInvestigations )
                    {
                        if ( !FilterText.IsEmpty() )
                        {
                            if ( !investigation.GetMatchesSearchString( FilterText ) )
                                continue;
                        }
                        if ( FilteredToInternalRobotics != null )
                            continue;

                        this.CalculateBoundsTwoLines( out Rect bounds, ref runningY );
                        if ( bounds.yMax < minYToShow )
                            continue; //it's scrolled up far enough we can skip it, yay!
                        if ( bounds.yMax > maxYToShow )
                            continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        bBuildItemTwoLines row = bBuildItemTwoLinesPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            break; //this was just time-slicing, so ignore that failure for now
                        bBuildItemTwoLinesPool.ApplySingleItemInRow( row, bounds.x, bounds.y );

                        row.Assign( investigation, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHovered = false;
                                        if ( element is ArcenUI_Button but )
                                        {
                                            if ( but.LastHadMouseWithin )
                                                isHovered = true;
                                        }
                                        string topColor = string.Empty;
                                        int targetImageIndex = 0;
                                        if ( BuildModeHandler.TargetingTerritoryControlInvestigation == investigation )
                                        {
                                            topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                            targetImageIndex = 3;
                                        }
                                        else
                                            topColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                        ExtraData.Buffer.StartColor( topColor ).AddRaw( investigation.Type?.Style.GetDisplayName() );


                                        row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[targetImageIndex] );
                                    }
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    {
                                        bool isHovered = false;
                                        if ( element is ArcenUI_Button but )
                                        {
                                            if ( but.LastHadMouseWithin )
                                                isHovered = true;
                                        }
                                        switch ( ExtraData.Int )
                                        {
                                            case 0:
                                                {
                                                    string iconColor = string.Empty;
                                                    if ( BuildModeHandler.TargetingTerritoryControlInvestigation == investigation )
                                                        iconColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                    else
                                                        iconColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( CommonRefs.TerritoryControlFlag.Icon, AdjustedSpriteStyle.InlineLarger1_2, iconColor );
                                                }
                                                break;
                                            case 1:
                                                {
                                                    string topColor = string.Empty;
                                                    if ( BuildModeHandler.TargetingTerritoryControlInvestigation == investigation )
                                                    {
                                                        topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                        ExtraData.Buffer.StartSize125().StartColor( topColor );
                                                    }
                                                    else
                                                    {
                                                        topColor = ColorTheme.GetGray( isHovered );
                                                        ExtraData.Buffer.StartSize125().StartColor( topColor );
                                                    }

                                                    ExtraData.Buffer.AddFormat2( "OutOF", 0, 1 ).Space1x();

                                                    ExtraData.Buffer.Space1x().StartSize80().AddFormat1( "BuildAdditionalSuggestedCount",
                                                        1, ColorTheme.GetSuggestedCount( isHovered ) ).EndSize();
                                                }
                                                break;
                                            default:
                                                ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                break;
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        investigation.RenderActiveInvestigationTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, null, TooltipExtraRules.MustBeToRightOfBuildMenu );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        if ( investigation != null )
                                        {
                                            if ( ExtraData.MouseInput.LeftButtonClicked )
                                            {

                                                if ( BuildModeHandler.TargetingTerritoryControlInvestigation == investigation )
                                                    BuildModeHandler.ClearAllTargeting();
                                                else
                                                {
                                                    BuildModeHandler.ClearAllTargeting();
                                                    BuildModeHandler.TargetingTerritoryControlInvestigation = investigation;
                                                }
                                            }
                                            else if ( ExtraData.MouseInput.RightButtonClicked )
                                            {
                                            }
                                        }
                                    }
                                    break;
                            }
                        } );
                    }

                    if ( deleteFunction != null )
                    {
                        for ( int i = 0; i < 1; i++ )
                        {
                            this.CalculateBoundsSingleLine( out Rect bounds, ref runningY );
                            if ( bounds.yMax < minYToShow )
                                continue; //it's scrolled up far enough we can skip it, yay!
                            if ( bounds.yMax > maxYToShow )
                                continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                            bBuildItemOneLine row = bBuildItemOneLinePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                break; //this was just time-slicing, so ignore that failure for now
                            bBuildItemOneLinePool.ApplySingleItemInRow( row, bounds.x, bounds.y );

                            row.Assign( deleteFunction, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            string topColor = ColorTheme.GetBasicLightTextPurple( isHovered );
                                            int targetImageIndex = 2;
                                            if ( BuildModeHandler.TargetingStructure == deleteFunction )
                                            {
                                                topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                targetImageIndex = 3;
                                            }

                                            ExtraData.Buffer.StartColor( topColor ).AddRaw( deleteFunction.GetDisplayName() );

                                            row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[targetImageIndex] );
                                        }
                                        break;
                                    case UIAction.GetOtherTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            switch ( ExtraData.Int )
                                            {
                                                case 0:
                                                    {
                                                        string iconColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                                        ExtraData.Buffer.AddSpriteStyled_NoIndent( deleteFunction.Icon, AdjustedSpriteStyle.InlineLarger1_2, iconColor );
                                                    }
                                                    break;
                                                default:
                                                    ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                    break;
                                            }
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( deleteFunction != null )
                                            {
                                                deleteFunction.RenderStructureTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipInstruction.ForConstruction,
                                                    TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfBuildMenu );

                                                BuildModeHandler.LastHoveredJob = null;
                                                Engine_HotM.HoveredJob = null;
                                                BuildModeHandler.LastHoveredStructureType = deleteFunction;
                                                BuildModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                            }
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            if ( deleteFunction != null )
                                            {
                                                if ( ExtraData.MouseInput.LeftButtonClicked )
                                                {
                                                    if ( !deleteFunction.GetCanAfford( null ) )
                                                    {
                                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                        break;
                                                    }

                                                    if ( BuildModeHandler.TargetingStructure == deleteFunction )
                                                        BuildModeHandler.ClearAllTargeting();
                                                    else
                                                    {
                                                        BuildModeHandler.ClearAllTargeting();
                                                        BuildModeHandler.TargetingStructure = deleteFunction;
                                                    }
                                                }
                                                else if ( ExtraData.MouseInput.RightButtonClicked )
                                                {
                                                    if ( World.Buildings.GetBuildingsWithBrokenMachineStructures().Count > 0 )
                                                    {
                                                        //mass-delete broken buildings
                                                        foreach ( ISimBuilding building in World.Buildings.GetBuildingsWithBrokenMachineStructures() )
                                                        {
                                                            MachineStructure structure = building.MachineStructureInBuilding;
                                                            if ( structure != null )
                                                                structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }
                                                    else if ( World.Buildings.GetBuildingsWithMachineStructuresOutOfNetwork().Count > 0 )
                                                    {
                                                        //mass-delete broken buildings
                                                        foreach ( ISimBuilding building in World.Buildings.GetBuildingsWithMachineStructuresOutOfNetwork() )
                                                        {
                                                            MachineStructure structure = building.MachineStructureInBuilding;
                                                            if ( structure != null )
                                                                structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        break;
                                }
                            } );
                        }
                    }


                    if ( pauseFunction != null )
                    {
                        for ( int i = 0; i < 1; i++ )
                        {
                            this.CalculateBoundsSingleLine( out Rect bounds, ref runningY );
                            if ( bounds.yMax < minYToShow )
                                continue; //it's scrolled up far enough we can skip it, yay!
                            if ( bounds.yMax > maxYToShow )
                                continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                            bBuildItemOneLine row = bBuildItemOneLinePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                break; //this was just time-slicing, so ignore that failure for now
                            bBuildItemOneLinePool.ApplySingleItemInRow( row, bounds.x, bounds.y );

                            row.Assign( pauseFunction, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            string topColor = ColorTheme.GetBasicLightTextPurple( isHovered );
                                            int targetImageIndex = 2;
                                            if ( BuildModeHandler.TargetingStructure == pauseFunction )
                                            {
                                                topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                                targetImageIndex = 3;
                                            }

                                            ExtraData.Buffer.StartColor( topColor ).AddRaw( pauseFunction.GetDisplayName() );

                                            row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[targetImageIndex] );
                                        }
                                        break;
                                    case UIAction.GetOtherTextToShowFromVolatile:
                                        {
                                            bool isHovered = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isHovered = true;
                                            }
                                            switch ( ExtraData.Int )
                                            {
                                                case 0:
                                                    {
                                                        string iconColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                                        ExtraData.Buffer.AddSpriteStyled_NoIndent( pauseFunction.Icon, AdjustedSpriteStyle.InlineLarger1_2, iconColor );
                                                    }
                                                    break;
                                                default:
                                                    ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                    break;
                                            }
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( pauseFunction != null )
                                            {
                                                pauseFunction.RenderStructureTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipInstruction.ForConstruction,
                                                    TooltipExtraText.None, TooltipExtraRules.MustBeToRightOfBuildMenu );

                                                BuildModeHandler.LastHoveredJob = null;
                                                Engine_HotM.HoveredJob = null;
                                                BuildModeHandler.LastHoveredStructureType = pauseFunction;
                                                BuildModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                            }
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            if ( pauseFunction != null )
                                            {
                                                if ( ExtraData.MouseInput.LeftButtonClicked )
                                                {
                                                    if ( !pauseFunction.GetCanAfford( null ) )
                                                    {
                                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                        break;
                                                    }

                                                    if ( BuildModeHandler.TargetingStructure == pauseFunction )
                                                        BuildModeHandler.ClearAllTargeting();
                                                    else
                                                    {
                                                        BuildModeHandler.ClearAllTargeting();
                                                        BuildModeHandler.TargetingStructure = pauseFunction;
                                                    }
                                                }
                                                else if ( ExtraData.MouseInput.RightButtonClicked )
                                                {
                                                    //mass-un-pause paused buildings
                                                    foreach ( ISimBuilding building in World.Buildings.GetBuildingsWithPausedMachineStructures() )
                                                    {
                                                        MachineStructure structure = building.MachineStructureInBuilding;
                                                        if ( structure != null )
                                                            structure.IsJobPaused = false;
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        break;
                                }
                            } );
                        }
                    }
                }
            }
            #endregion

            #region HandleSpecificJob
            private void HandleSpecificJob( MachineJob job )
            {
                if ( !FilterText.IsEmpty() )
                {
                    if ( !job.GetMatchesSearchString( FilterText ) )
                        return;
                }
                if ( FilteredToInternalRobotics != null )
                {
                    if ( job.InternalRoboticsTypeNeeded != FilteredToInternalRobotics )
                        return;
                }

                this.CalculateBoundsTwoLines( out Rect bounds, ref runningY );
                if ( bounds.yMax < minYToShow )
                    return; //it's scrolled up far enough we can skip it, yay!
                if ( bounds.yMax > maxYToShow )
                    return; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                bBuildItemTwoLines row = bBuildItemTwoLinesPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return; //this was just time-slicing, so ignore that failure for now
                bBuildItemTwoLinesPool.ApplySingleItemInRow( row, bounds.x, bounds.y );

                row.Assign( job, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = false;
                                if ( element is ArcenUI_Button but )
                                {
                                    if ( but.LastHadMouseWithin )
                                        isHovered = true;
                                }
                                int targetImageIndex = 0;
                                string topColor = string.Empty;
                                if ( job.GetIsAtCap( true, null ) )
                                {
                                    topColor = ColorTheme.GetAtCapBrighter( isHovered );
                                    targetImageIndex = 1;
                                }
                                else if ( !job.GetCanAffordAnother( true, null ) )
                                {
                                    topColor = ColorTheme.GetCannotAfford( isHovered );
                                    targetImageIndex = 4;
                                }
                                else if ( BuildModeHandler.TargetingJob == job )
                                {
                                    topColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                    targetImageIndex = 3;
                                }
                                else
                                    topColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[targetImageIndex] );

                                ExtraData.Buffer.StartColor( topColor ).AddRaw( job.GetDisplayName() );
                            }
                            break;
                        case UIAction.GetOtherTextToShowFromVolatile:
                            {
                                bool isHovered = false;
                                if ( element is ArcenUI_Button but )
                                {
                                    if ( but.LastHadMouseWithin )
                                        isHovered = true;
                                }
                                switch ( ExtraData.Int )
                                {
                                    case 0:
                                        {
                                            string iconColor = string.Empty;
                                            if ( job.GetIsAtCap( true, null ) )
                                                iconColor = ColorTheme.GetAtCap( isHovered );
                                            else if ( !job.GetCanAffordAnother( true, null ) )
                                                iconColor = ColorTheme.GetCannotAfford( isHovered );
                                            else if ( BuildModeHandler.TargetingJob == job )
                                            {
                                                iconColor = ColorTheme.GetBasicLightTextBlue( isHovered );
                                            }
                                            else
                                                iconColor = ColorTheme.GetBasicLightTextPurple( isHovered );

                                            ExtraData.Buffer.AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2, iconColor );

                                        }
                                        break;
                                    case 1:
                                        {
                                            int cost = job.InternalRobotsCountNeeded;
                                            int currentRemaining = job.InternalRoboticsTypeNeeded.DuringGame_CalculateRemainingForInternalRoboticsForJobs();
                                            int currentTotal = job.GetCurrentCountOfStructuresTotal();

                                            string bottomColor = string.Empty;
                                            if ( job.GetIsAtCap( true, null ) )
                                            {
                                                bottomColor = ColorTheme.GetAtCap( isHovered );
                                                ExtraData.Buffer.StartSize125().StartColor( bottomColor );
                                                ExtraData.Buffer.AddFormat2( "OutOF", cost, currentRemaining );

                                                if ( !job.Meta_HasEverBeenBuilt )
                                                    ExtraData.Buffer.Space1x().AddLang( "NewItem_Brief", ColorTheme.GetNewItem( isHovered ) );
                                                else
                                                    ExtraData.Buffer.Space1x().AddFormat1( "Parenthetical", currentTotal, ColorTheme.GetCategoryWhite( isHovered ) );
                                            }
                                            else if ( BuildModeHandler.TargetingJob == job )
                                            {
                                                bottomColor = ColorTheme.GetBasicSecondLineTextBlue( isHovered );
                                                ExtraData.Buffer.StartSize125().StartColor( bottomColor );
                                                ExtraData.Buffer.AddFormat2( "OutOF", cost, currentRemaining ).Space1x();
                                                job.WriteResourceCostsBrief( true, ExtraData.Buffer, ColorTheme.GetCanAfford( isHovered ), ColorTheme.GetCannotAfford( isHovered ) );
                                                if ( !job.Meta_HasEverBeenBuilt )
                                                    ExtraData.Buffer.Space1x().AddLang( "NewItem_Brief", ColorTheme.GetNewItem( isHovered ) );
                                                else
                                                    ExtraData.Buffer.Space1x().AddFormat1( "Parenthetical", currentTotal, ColorTheme.GetCategoryWhite( isHovered ) );
                                            }
                                            else
                                            {
                                                bottomColor = ColorTheme.GetBasicSecondLineTextPurple( isHovered );
                                                ExtraData.Buffer.StartSize125().StartColor( bottomColor );
                                                ExtraData.Buffer.AddFormat2( "OutOF", cost, currentRemaining ).Space1x();
                                                job.WriteResourceCostsBrief( true, ExtraData.Buffer, ColorTheme.GetCanAfford( isHovered ), ColorTheme.GetCannotAfford( isHovered ) );
                                                if ( !job.Meta_HasEverBeenBuilt )
                                                    ExtraData.Buffer.Space1x().AddLang( "NewItem_Brief", ColorTheme.GetNewItem( isHovered ) );
                                                else
                                                    ExtraData.Buffer.Space1x().AddFormat1( "Parenthetical", currentTotal, ColorTheme.GetCategoryWhite( isHovered ) );
                                            }

                                            if ( job.DuringGame_NumberSuggestedToBuild.Display > 0 )
                                                ExtraData.Buffer.Space1x().AddFormat1( "BuildAdditionalSuggestedCount",
                                                    job.DuringGame_NumberSuggestedToBuild.Display, ColorTheme.GetSuggestedCount( isHovered ) );
                                        }
                                        break;
                                    default:
                                        ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                        break;
                                }
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                if ( job != null )
                                {
                                    job.RenderJobTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipInstruction.ForConstruction, null,
                                        FilteredToInternalRobotics == null ? TooltipExtraText.ControlClickToFilterToInternalRobotics : TooltipExtraText.None, 
                                        TooltipExtraRules.MustBeToRightOfBuildMenu );

                                    BuildModeHandler.LastHoveredJob = job;
                                    Engine_HotM.HoveredJob = job;
                                    BuildModeHandler.LastHoveredStructureType = null;
                                    BuildModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                if ( job != null )
                                {
                                    if ( InputCaching.IsInInspectMode_Any )
                                    {
                                        FilteredToInternalRobotics = job.InternalRoboticsTypeNeeded;
                                    }
                                    else
                                    {
                                        if ( ExtraData.MouseInput.LeftButtonClicked )
                                        {
                                            if ( !job.GetCanAffordAnother( true, null ) )
                                            {
                                                ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                break;
                                            }

                                            if ( BuildModeHandler.TargetingJob == job )
                                                BuildModeHandler.ClearAllTargeting();
                                            else
                                            {
                                                BuildModeHandler.ClearAllTargeting();
                                                BuildModeHandler.TargetingJob = job;
                                            }
                                        }
                                        else if ( ExtraData.MouseInput.RightButtonClicked )
                                        {
                                            if ( job.DuringGame_FullList.Count > 0 )
                                                SimCommon.CycleThroughMachineStructures( true, ( MachineStructure s ) => s.CurrentJob == job );
                                        }
                                    }
                                }
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region DrawCategoryIconSuggested
            private bool DrawCategoryIconSuggested( MachineBuildModeCategory category, ref float currentY )
            {
                bool isSelectedCategory = false;
                bCategorySuggested icon = bCategorySuggestedPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float iconX = CATEGORY_ICON_X;
                    icon.ApplyItemInPositionNoTextSizing( ref iconX, ref currentY, false, false, CATEGORY_ICON_WIDTH, CATEGORY_ICON_SUGGESTED_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = category.GetHasAnyEntries();

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( category != null )
                                {
                                    bool isSelected = BuildModeHandler.currentCategory == category && isPerformable;

                                    string colorHex = isSelected ? "FFFFFF" : (isPerformable ? "EEEEEE" : "666666");

                                    icon.SetRelatedImage1EnabledIfNeeded( isSelected );
                                    ExtraData.Buffer.AddRaw( category.ShortName.Text, colorHex );
                                    if ( !category.IsAll )
                                    {
                                        int countToDraw = category.GetEntryCount();
                                        if ( countToDraw > 0 )
                                            ExtraData.Buffer.Line().AddRaw( countToDraw.ToString(), ColorTheme.DataBlue );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( category != null )
                                        category.RenderCategoryTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipExtraRules.MustBeToRightOfBuildMenu );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( category != null )
                                {
                                    if ( !category.GetHasAnyEntries() )
                                        break;

                                    if ( BuildModeHandler.currentCategory == category )
                                        break; //nothing to do here
                                    else
                                    {
                                        BuildModeHandler.currentCategory = category;
                                        BuildModeHandler.ClearAllTargeting();
                                    }
                                }
                                break;
                        }
                    } );

                    currentY -= CATEGORY_ICON_SUGGESTED_HEIGHT + CATEGORY_ICON_Y_SPACING;
                }
                return isSelectedCategory;
            }
            #endregion

            #region DrawCategoryIconNormal
            private bool DrawCategoryIconNormal( MachineBuildModeCategory category, ref float currentY )
            {
                bool isSelectedCategory = false;
                bCategoryNormal icon = bCategoryNormalPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float iconX = CATEGORY_ICON_X;
                    icon.ApplyItemInPositionNoTextSizing( ref iconX, ref currentY, false, false, CATEGORY_ICON_WIDTH, CATEGORY_ICON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = category.GetHasAnyEntries();

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( category != null )
                                {
                                    bool isSelected = BuildModeHandler.currentCategory == category && isPerformable;

                                    string colorHex = isSelected ? "FFFFFF" : (isPerformable ? "EEEEEE" : "666666");

                                    icon.SetRelatedImage0SpriteIfNeeded( category.Icon.GetSpriteForUI() );
                                    icon.SetRelatedImage0ColorFromHexIfNeeded( colorHex );

                                    icon.SetRelatedImage1EnabledIfNeeded( isSelected );
                                    ExtraData.Buffer.AddRaw( category.ShortName.Text, colorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( category != null )
                                        category.RenderCategoryTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipExtraRules.MustBeToRightOfBuildMenu );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( category != null )
                                {
                                    if ( !category.GetHasAnyEntries() )
                                        break;

                                    if ( BuildModeHandler.currentCategory == category )
                                        break; //nothing to do here
                                    else
                                    {
                                        BuildModeHandler.currentCategory = category;
                                        BuildModeHandler.ClearAllTargeting();
                                    }
                                }
                                break;
                        }
                    } );

                    currentY -= CATEGORY_ICON_HEIGHT + CATEGORY_ICON_Y_SPACING;
                }
                return isSelectedCategory;
            }
            #endregion
        }

        #region dInternalRobotics
        public class dInternalRobotics : DropdownAbstractBase
        {
            public static dInternalRobotics Instance;
            public dInternalRobotics()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                if ( Item.GetItem() is UpgradeInt internalRobotics )
                    FilteredToInternalRobotics = internalRobotics;
                else
                    FilteredToInternalRobotics = null; //must be the "any" type
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                List<UpgradeInt> validOptions = UpgradeIntTable.Instance.SortedWhichAreJobInternalRobotics_Available;
                validOptions.Clear();
                foreach ( UpgradeInt upgrade in UpgradeIntTable.Instance.SortedWhichAreJobInternalRobotics )
                {
                    if ( upgrade != null && upgrade.DuringGameplay_CurrentJobTypesUnlocked > 0)
                        validOptions.Add( upgrade );
                }

                UpgradeInt typeDataToSelect = FilteredToInternalRobotics;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        FilteredToInternalRobotics = null;
                    }
                }
                #endregion

                //#region Select Default If Blank
                //if ( typeDataToSelect == null && validOptions.Count > 0 )
                //    typeDataToSelect = validOptions[0];
                //#endregion

                //if ( FilteredToInternalRobotics == null && typeDataToSelect != null )
                //    FilteredToInternalRobotics = typeDataToSelect;

                bool foundMismatch = false;

                object selectedNow = elementAsType.CurrentlySelectedOption?.GetItem();

                if ( selectedNow is UpgradeInt selectedUpgrade )
                {
                    if ( selectedUpgrade != typeDataToSelect )
                        foundMismatch = true;
                }
                else
                {
                    if ( typeDataToSelect != null )
                        foundMismatch = true;
                }

                List<IArcenDropdownOption> itemList = elementAsType.GetItems_DoNotAlterDirectly();
                if ( foundMismatch )
                { } //no more checking to do
                else if ( ( validOptions.Count + 1 ) != itemList.Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < validOptions.Count && i + 1 < itemList.Count; i++ )
                    {
                        UpgradeInt row = validOptions[i];

                        IArcenDropdownOption option = itemList[i+1];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        UpgradeInt optionItemAsType = (UpgradeInt)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    ArbitraryArcenOption anyOption;
                    anyOption.DisplayNameLangKey = "AnyInternalRobotics";
                    anyOption.DescriptionLangKey = "AnyInternalRobotics_Description";
                    anyOption.NumericIndex = -1;
                    anyOption.NumericIndexAsString = "-1";

                    elementAsType.AddItem( anyOption, typeDataToSelect == null );

                    for ( int i = 0; i < validOptions.Count; i++ )
                    {
                        UpgradeInt row = validOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
                if ( FilteredToInternalRobotics != null )
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "dInternalRobotics", "Main" ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.SizeToText ) )
                    {
                        novel.TitleUpperLeft.AddLang( "InternalRobotics" );
                        novel.MainHeader.StartSize90().AddRightClickFormat( "RightClickToClearFilter", ColorTheme.SoftGold ).EndSize();
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                //MachineHandbookCollection ItemAsType = (MachineHandbookCollection)Item.GetItem();
            }

            public override bool HandleOverallClick( PointerEventData eventData )
            {
                if ( eventData.button == PointerEventData.InputButton.Left )
                    return false;

                FilteredToInternalRobotics = null;
                return true;
            }
        }
        #endregion

        #region iFilterText
        public class iFilterText : InputAbstractBase
        {
            public int maxTextLength = 50;
            public static iFilterText Instance;
            public iFilterText() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar;
            }

            public override void OnValueChanged( string newString )
            {
                FilterText = newString;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void OnEndEdit()
            {
                FilterText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( inputField == null )
                {
                    inputField = (this.Element as ArcenUI_Input);
                    inputField?.SetPlaceholderTextLangKey( "ResearchWindow_SearchText" );
                }
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
        #endregion

        //not actually needed at this time, but needed for compilation
        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        #region bBuildItemTwoLines
        public class bBuildItemTwoLines : ButtonAbstractBaseWithImage
        {
            public static bBuildItemTwoLines Original;
            public bBuildItemTwoLines() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private MachineJob lastJob = null;
            private MachineStructureType lastStructureType = null;
            private Investigation lastInvestigation = null;

            public void Assign( MachineJob Job, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastJob != Job )
                {
                    this.lastJob = Job;
                    this.lastStructureType = null;
                    this.lastInvestigation = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( MachineStructureType StructureType, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastStructureType != StructureType )
                {
                    this.lastJob = null;
                    this.lastStructureType = StructureType;
                    this.lastInvestigation = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( Investigation Invest, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastInvestigation != Invest )
                {
                    this.lastJob = null;
                    this.lastStructureType = null;
                    this.lastInvestigation = Invest;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
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
        }
        #endregion

        #region bBuildItemOneLine
        public class bBuildItemOneLine : ButtonAbstractBaseWithImage
        {
            public static bBuildItemOneLine Original;
            public bBuildItemOneLine() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private MachineJob lastJob = null;
            private MachineStructureType lastStructureType = null;
            private Investigation lastInvestigation = null;

            public void Assign( MachineJob Job, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastJob != Job )
                {
                    this.lastJob = Job;
                    this.lastStructureType = null;
                    this.lastInvestigation = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( MachineStructureType StructureType, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastStructureType != StructureType )
                {
                    this.lastJob = null;
                    this.lastStructureType = StructureType;
                    this.lastInvestigation = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( Investigation Invest, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastInvestigation != Invest )
                {
                    this.lastJob = null;
                    this.lastStructureType = null;
                    this.lastInvestigation = Invest;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
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
        }
        #endregion

        #region bCategorySuggested
        public class bCategorySuggested : ButtonAbstractBaseWithImage
        {
            public static bCategorySuggested Original;
            public bCategorySuggested() { if ( Original == null ) Original = this; }

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
        }
        #endregion

        #region bCategoryNormal
        public class bCategoryNormal : ButtonAbstractBaseWithImage
        {
            public static bCategoryNormal Original;
            public bCategoryNormal() { if ( Original == null ) Original = this; }

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
        }
        #endregion

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Engine_HotM.SelectedMachineActionMode = null;
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

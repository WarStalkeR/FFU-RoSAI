using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_VictoryPath : ToggleableWindowController, IInputActionHandler
    {
        #region Main Controller
        public static Window_VictoryPath Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_VictoryPath()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.ShouldPretendDoesNotBlockUIForMouseWhenOpen = true;
        }

        public override void OnOpen()
        {
            hasDoneFirstMetaAchievementsSort = false;
            sortedMetaAchievements.Clear();

            hasDoneFirstTimelineAchievementsSort = false;
            sortedTimelineAchievements.Clear();

            MetaAchievements_FilterText = string.Empty;
            TimelineAchievements_FilterText = string.Empty;

            timelineGoalCollection = null;
            achievementCollection = null;

            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );

            foreach ( ActorDataType row in ActorDataTypeTable.Instance.Rows )
            {
                row.DuringGameplay_SkillCheckSuccesses.ResetCombinedName();
                row.DuringGameplay_SkillCheckFailures.ResetCombinedName();
            }

            base.OnOpen();
        }

        private static TimelineGoalCollection timelineGoalCollection = null;
        private static AchievementCollection achievementCollection = null;

        private static string TimelineAchievements_FilterText = string.Empty;
        private static string TimelineAchievements_LastFilterText = string.Empty;
        private static int TimelineAchievements_lastTurn = 0;

        private static string MetaAchievements_FilterText = string.Empty;
        private static string MetaAchievements_LastFilterText = string.Empty;
        private static int MetaAchievements_lastTurn = 0;

        private static bool hasDoneFirstMetaAchievementsSort = false;
        private static readonly List<Achievement> sortedMetaAchievements = List<Achievement>.Create_WillNeverBeGCed( 40, "Window_VictoryPath-sortedMetaAchievements" );

        private static bool hasDoneFirstTimelineAchievementsSort = false;
        private static readonly List<Achievement> sortedTimelineAchievements = List<Achievement>.Create_WillNeverBeGCed( 40, "Window_VictoryPath-sortedTimelineAchievements" );

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public static VictoryTabType currentlyRequestedDisplayType = VictoryTabType.VictoryOverview;

            public customParent()
            {
                Window_VictoryPath.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;
            private static ButtonAbstractBase.ButtonPool<bDataFullItem> bDataFullItemPool;
            private static ButtonAbstractBase.ButtonPool<bDataAdjustedItem> bDataAdjustedItemPool;
            private static ButtonAbstractBase.ButtonPool<bDataHeader> bDataHeaderPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        //keeps it from having visual glitches, but uses more GPU.  That's fine, this is the only window open at that time.
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "bCategory" );
                            bDataFullItemPool = new ButtonAbstractBase.ButtonPool<bDataFullItem>( bDataFullItem.Original, 10, "bDataFullItem" );
                            bDataAdjustedItemPool = new ButtonAbstractBase.ButtonPool<bDataAdjustedItem>( bDataAdjustedItem.Original, 10, "bDataAdjustedItem" );
                            bDataHeaderPool = new ButtonAbstractBase.ButtonPool<bDataHeader>( bDataHeader.Original, 10, "bDataHeader" );
                        }
                    }
                    #endregion

                    OnUpdate_Categories();
                    OnUpdate_Content();
                }
            }
            private void OnUpdate_Categories()
            {
                bCategoryPool.Clear( 60 );

                float currentY = -5; //the position of the first entry

                int chapterNumber = SimMetagame.CurrentChapterNumber;

                // Categories are mostly static.
                for ( VictoryTabType displayType = VictoryTabType.VictoryOverview; displayType < VictoryTabType.Length; displayType++ )
                {
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( chapterNumber < 3 && displayType == VictoryTabType.MetaGoalHistory )
                        break; //don't show the meta ones until chapter 3

                    if ( item == null )
                        break;
                    item.Assign( displayType );
                }

                bCategoryPool.ApplyItemsInRows( 6.1f, ref currentY, 27f, 218.5f, 25.6f, false );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;
            private float yTopOffset = 0f;

            private const float TOP_OFFSET_BUFFER = -1f;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load

            private void OnUpdate_Content()
            {
                bDataFullItemPool.Clear( 60 );
                bDataAdjustedItemPool.Clear( 60 );
                bDataHeaderPool.Clear( 60 );

                RectTransform rTran = (RectTransform)bDataFullItem.Original.Element.RelevantRect.parent;

                maxYToShow = -rTran.anchoredPosition.y;
                minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                maxYToShow += EXTRA_BUFFER;
                yTopOffset = -((rTran.anchoredPosition.y - topBuffer) + TOP_OFFSET_BUFFER);

                runningY = topBuffer;

                if ( !hasDoneFirstMetaAchievementsSort )
                    GetAndSortMetaAchievements(); //note, the way the filtering is done is different here
                if ( !hasDoneFirstTimelineAchievementsSort )
                    GetAndSortTimelineAchievements(); //note, the way the filtering is done is different here

                switch ( currentlyRequestedDisplayType )
                {
                    case VictoryTabType.Dooms:
                        OnUpdate_Content_Dooms();
                        break;
                    case VictoryTabType.TimelineGoals:
                        OnUpdate_Content_TimelineGoals();
                        break;
                    case VictoryTabType.MetaGoalHistory:
                        OnUpdate_Content_MetaGoalHistory();
                        break;
                    case VictoryTabType.MetaAchievements:
                        OnUpdate_Content_MetaAchievements();
                        break;
                    case VictoryTabType.TimelineAchievements:
                        OnUpdate_Content_TimelineAchievements();
                        break;
                    case VictoryTabType.VictoryOverview:
                        OnUpdate_Content_Overview();
                        break;
                    case VictoryTabType.Length:
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( $"Error! Victory Overview tried to render with a category of {currentlyRequestedDisplayType.ToString()}, this should be impossible. Acting as though it's requesting Primary Resources instead.", Verbosity.ShowAsError );
                        currentlyRequestedDisplayType = VictoryTabType.VictoryOverview;
                        return;
                }

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( runningY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region CalculateBoundsSingle
            protected const float leftBuffer = 5.1f;
            protected const float topBuffer = -2.55f;

            private const float FULL_WIDTH = 832f; //was 809.9
            private const float QUARTER_WIDTH = 204f; //208
            private const float QUARTER_WIDTH_SPACING = 4f;

            private const float HALF_WIDTH = 404.45f;
            private const float HALF_WIDTH_SPACING = 1f;

            private const float QUINT_WIDTH = 164.4f; //166.4
            private const float QUINT_WIDTH_SPACING = 1f;

            private const float QUARTER_WIDTH_COL0 = leftBuffer;
            private const float QUARTER_WIDTH_COL1 = QUARTER_WIDTH_COL0 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;
            private const float QUARTER_WIDTH_COL2 = QUARTER_WIDTH_COL1 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;
            private const float QUARTER_WIDTH_COL3 = QUARTER_WIDTH_COL2 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;

            private const float QUINT_WIDTH_COL0 = leftBuffer;
            private const float QUINT_WIDTH_COL1 = QUINT_WIDTH_COL0 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL2 = QUINT_WIDTH_COL1 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL3 = QUINT_WIDTH_COL2 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL4 = QUINT_WIDTH_COL3 + QUINT_WIDTH + QUINT_WIDTH_SPACING;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY, bool IsTall )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, FULL_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );

                innerY -= IsTall ? CONTENT_ROW_HEIGHT_TALL + CONTENT_ROW_GAP_TALL : CONTENT_ROW_HEIGHT_SHORT + CONTENT_ROW_GAP_SHORT;
            }

            protected void CalculateBoundsAchievement( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, FULL_WIDTH, CONTENT_ROW_HEIGHT_ACHIEVEMENT );

                innerY -= CONTENT_ROW_HEIGHT_ACHIEVEMENT + CONTENT_ROW_GAP_TALL + CONTENT_ROW_GAP_SHORT;
            }

            protected void CalculateBoundsLargeTwoLineHeader( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, FULL_WIDTH, 58.02f );

                innerY -= 58.02f + CONTENT_ROW_GAP_TALL;
            }
            #endregion

            #region CalculateBoundsDual
            protected void CalculateBoundsDual( out Rect leftBounds, out Rect rightBounds, ref float innerY, bool IsTall )
            {
                leftBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, HALF_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );
                rightBounds = ArcenFloatRectangle.CreateUnityRect( leftBounds.xMax + HALF_WIDTH_SPACING, innerY, HALF_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );

                innerY -= IsTall ? CONTENT_ROW_HEIGHT_TALL + CONTENT_ROW_GAP_TALL : CONTENT_ROW_HEIGHT_SHORT + CONTENT_ROW_GAP_SHORT;
            }
            #endregion

            #region CalculateBoundsQuadColumn
            protected void CalculateBoundsQuadColumn( out Rect soleBounds, int CurrentColumn, float innerY, bool IsTall )
            {
                float xPos;
                switch ( CurrentColumn )
                {
                    case 0:
                        xPos = QUARTER_WIDTH_COL0;
                        break;
                    case 1:
                        xPos = QUARTER_WIDTH_COL1;
                        break;
                    case 2:
                        xPos = QUARTER_WIDTH_COL2;
                        break;
                    case 3:
                        xPos = QUARTER_WIDTH_COL3;
                        break;
                    default:
                        throw new Exception( "CalculateBoundsQuadColumn: Asked for invalid column " + CurrentColumn + "!" );
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( xPos, innerY, QUARTER_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );
            }
            #endregion

            #region CalculateBoundsQuintColumn
            protected void CalculateBoundsQuintColumn( out Rect soleBounds, int CurrentColumn, float innerY, bool IsTall )
            {
                float xPos;
                switch ( CurrentColumn )
                {
                    case 0:
                        xPos = QUINT_WIDTH_COL0;
                        break;
                    case 1:
                        xPos = QUINT_WIDTH_COL1;
                        break;
                    case 2:
                        xPos = QUINT_WIDTH_COL2;
                        break;
                    case 3:
                        xPos = QUINT_WIDTH_COL3;
                        break;
                    case 4:
                        xPos = QUINT_WIDTH_COL4;
                        break;
                    default:
                        throw new Exception( "CalculateBoundsQuintColumn: Asked for invalid column " + CurrentColumn + "!" );
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( xPos, innerY, QUINT_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );
            }
            #endregion

            #region OnUpdate_Content_Overview
            private void OnUpdate_Content_Overview()
            {
                CityTimelineDoomType currentDoom = SimCommon.GetEffectiveTimelineDoomType();

                #region Timeline Overview Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "VictoryProgress" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "GoalsCompleted" )
                                        .AddRaw( SimCommon.CompletedGoals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );

                                    ExtraData.Buffer.Space8x();

                                    if ( !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
                                    {
                                    }
                                    else
                                    {
                                        if ( currentDoom != null )
                                        {
                                            int lastTurn = 0;
                                            foreach ( DoomEvent doomEvent in currentDoom.DoomMainEvents )
                                            {
                                                lastTurn = MathA.Max( lastTurn, doomEvent.DuringGameplay_WillHappenOnTurn );
                                            }

                                            if ( lastTurn >= SimCommon.Turn )
                                            {
                                                ExtraData.Buffer.Space8x();
                                                ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TurnsUntilFinalDoom" )
                                                    .AddRaw( (lastTurn - SimCommon.Turn).ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                if ( FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
                {
                    int maxIndex = MathA.Max( TimelineGoalTable.SortedGoals.Count, currentDoom.DoomMainEvents.Count );

                    for ( int i = 0; i < maxIndex; i++ )
                    {
                        this.CalculateBoundsDual( out Rect leftBounds, out Rect rightBounds, ref runningY, false );
                        if ( leftBounds.yMax < minYToShow )
                            continue; //it's scrolled up far enough we can skip it, yay!
                        if ( leftBounds.yMax > maxYToShow )
                            continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        #region Timeline Goal
                        if ( i < TimelineGoalTable.SortedGoals.Count )
                        {
                            bDataAdjustedItem row = bDataAdjustedItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                continue;
                            bDataAdjustedItemPool.ApplySingleItemInRow( row, leftBounds, true );

                            TimelineGoal goal = TimelineGoalTable.SortedGoals[i];

                            row.Assign( "TimelineGoal", goal.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            int completed = 0;
                                            int possible = 0;
                                            foreach ( TimelineGoalPath path in goal.Paths )
                                            {
                                                possible++;
                                                if ( path.DuringGameplay_HasAchievedInThisTimeline )
                                                    completed++;
                                            }

                                            ExtraData.Buffer.StartSize80();

                                            bool isFailed = false;
                                            if ( completed == 0 && !goal.GetIsCurrentlyViable() )
                                                isFailed = true;

                                            ExtraData.Buffer.Position10();

                                            //show how many complete out of how many possible
                                            if ( completed == 0 )
                                                ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.Gray );
                                            else
                                                ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.TechnologyCountGreen );

                                            ExtraData.Buffer.Position60();

                                            bool isHovered = element.LastHadMouseWithin;
                                            row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                            string mainColor = isFailed ? ColorTheme.GetRedOrange2( isHovered ) : ColorTheme.GetBasicLightTextBlue( isHovered );
                                            ExtraData.Buffer.StartColor( mainColor );

                                            ExtraData.Buffer.AddSpriteStyled_NoIndent( goal.Icon, AdjustedSpriteStyle.InlineLarger1_2, mainColor ).Space1x();
                                            ExtraData.Buffer.AddRaw( goal.GetDisplayName() );
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            goal.WriteTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, true );
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        //goal.OpenPopupShowingThisGoal();
                                        break;
                                }
                            } );
                        }
                        #endregion Timeline Goal

                        #region Doom
                        if ( i < currentDoom.DoomMainEvents.Count )
                        {
                            bDataAdjustedItem row = bDataAdjustedItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                continue;
                            bDataAdjustedItemPool.ApplySingleItemInRow( row, rightBounds, true );

                            DoomEvent doomEvent = currentDoom.DoomMainEvents[i];

                            row.Assign( "DoomEvent", doomEvent.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool isHovered = element.LastHadMouseWithin;
                                            row.SetRelatedImage0EnabledIfNeeded( isHovered );

                                            ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                            ExtraData.Buffer.Position10();
                                            if ( !doomEvent.DuringGameplay_HasHappened )
                                            {
                                                ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataProblem )
                                                    .AddRaw( (doomEvent.DuringGameplay_WillHappenOnTurn - SimCommon.Turn).ToString(), ColorTheme.DataProblem );
                                            }
                                            else
                                            {
                                                ExtraData.Buffer.AddNeverTranslated( "-", true, ColorTheme.DataProblem );
                                                //ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.KillOrIncapacitate.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataProblem );
                                            }

                                            ExtraData.Buffer.Position60();

                                            ExtraData.Buffer.AddRaw( doomEvent.GetDisplayName() );
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            doomEvent.WriteTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false );
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        //goal.OpenPopupShowingThisGoal();
                                        break;
                                }
                            } );
                        }
                        #endregion
                    }
                }
                else
                {
                    //must be a lower chapter!

                    for ( int i = 0; i <= 4; i++ )
                    {
                        this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                        if ( bounds.yMax < minYToShow )
                            continue; //it's scrolled up far enough we can skip it, yay!
                        if ( bounds.yMax > maxYToShow )
                            continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            continue;
                        bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                        MetaChapter chapter = MetaChapterTable.ChaptersByNumber[i];

                        row.Assign( "Chapter", i, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHovered = element.LastHadMouseWithin;
                                        row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                        ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                        ExtraData.Buffer.AddRaw( chapter.GetDisplayName() );

                                        ExtraData.Buffer.EndSize().Position250().StartSize70();

                                        if ( SimMetagame.CurrentChapterNumber > chapter.ChapterNumber )
                                            ExtraData.Buffer.AddLang( "ChecklistItemComplete", ColorTheme.DataBlue );
                                        else
                                            ExtraData.Buffer.AddLang( "ChecklistItemIncomplete", ColorTheme.RedLess );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( chapter ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                        {
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.TitleUpperLeft.AddRaw( chapter.GetDisplayName() );
                                            novel.Main.AddRaw( chapter.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                            if ( !chapter.StrategyTip.Text.IsEmpty() )
                                                novel.Main.AddRaw( chapter.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                                        }
                                    }
                                    break;
                                case UIAction.OnClick:
                                    //goal.OpenPopupShowingThisGoal();
                                    break;
                            }
                        } );
                    }
                }
            }
            #endregion

            #region OnUpdate_Content_Dooms
            private void OnUpdate_Content_Dooms()
            {
                CityTimelineDoomType currentDoom = SimCommon.GetEffectiveTimelineDoomType();

                #region Timeline Doom Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ActivityOverview_Dooms" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    if ( !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
                                    {
                                        ExtraData.Buffer.AddLang( "DoomsStartLater" );
                                    }
                                    else
                                    {
                                        if ( currentDoom != null )
                                        {
                                            int doomsDone = 0;
                                            int lastTurn = 0;
                                            foreach ( DoomEvent doomEvent in currentDoom.DoomMainEvents )
                                            {
                                                if ( doomEvent.DuringGameplay_HasHappened )
                                                    doomsDone++;

                                                lastTurn = MathA.Max( lastTurn, doomEvent.DuringGameplay_WillHappenOnTurn );
                                            }

                                            ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DoomsExperienced" )
                                                .AddRaw( doomsDone.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );

                                            if ( lastTurn >= SimCommon.Turn )
                                            {
                                                ExtraData.Buffer.Space8x();
                                                ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TurnsUntilFinalDoom" )
                                                    .AddRaw( (lastTurn - SimCommon.Turn).ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                if ( currentDoom == null || !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
                    return;

                foreach ( DoomEvent doomEvent in currentDoom.DoomMainEvents )
                {
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "DoomEvent", doomEvent.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddRaw( doomEvent.GetDisplayName() );

                                    ExtraData.Buffer.EndSize().Position350().StartSize70();

                                    if ( doomEvent.DuringGameplay_WillHappenOnTurn <= 0 )
                                    {
                                        ExtraData.Buffer.AddLang( "Doom_Skipped", ColorTheme.RedLess );
                                    }
                                    else
                                    {
                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "Doom_WillHappenOnTurn_VeryBrief" ).AddRaw( doomEvent.DuringGameplay_WillHappenOnTurn.ToString() );

                                        if ( !doomEvent.DuringGameplay_HasHappened )
                                        {
                                            ExtraData.Buffer.Position500();
                                            ExtraData.Buffer.AddLangAndAfterLineItemHeader( "Doom_TurnsRemaining_VeryBrief" ).AddRaw( (doomEvent.DuringGameplay_WillHappenOnTurn - SimCommon.Turn).ToString() );
                                        }
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    doomEvent.WriteTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false );
                                }
                                break;
                            case UIAction.OnClick:
                                //goal.OpenPopupShowingThisGoal();
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_TimelineGoals
            private void OnUpdate_Content_TimelineGoals()
            {
                #region Timeline Goal Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ActivityOverview_TimelineGoals" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "GoalsCompleted" )
                                        .AddRaw( SimCommon.CompletedGoals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );

                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "GoalsAvailable" )
                                        .AddRaw( SimCommon.AvailableGoals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                foreach ( TimelineGoal goal in TimelineGoalTable.SortedGoals )
                {
                    if ( goal.IsHidden )
                        continue;
                    //filtering
                    if ( timelineGoalCollection != null )
                    {
                        if ( !goal.Collections.ContainsKey( timelineGoalCollection.ID ) )
                            continue;
                    }

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "TimelineGoal", goal.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    int completed = 0;
                                    int possible = 0;
                                    foreach ( TimelineGoalPath path in goal.Paths )
                                    {
                                        possible++;
                                        if ( path.DuringGameplay_HasAchievedInThisTimeline )
                                            completed++;
                                    }

                                    bool isFailed = false;
                                    if ( completed == 0 && !goal.GetIsCurrentlyViable() )
                                        isFailed = true;

                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    string mainColor = isFailed ? ColorTheme.GetRedOrange2( isHovered ) : ColorTheme.GetBasicLightTextBlue( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( mainColor );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( goal.Icon, AdjustedSpriteStyle.InlineLarger1_2, mainColor ).Space1x();
                                    ExtraData.Buffer.AddRaw( goal.GetDisplayName() );

                                    //show how many complete out of how many possible
                                    
                                    ExtraData.Buffer.Position400();
                                    if ( completed == 0 )
                                        ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.Gray );
                                    else
                                        ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.TechnologyCountGreen );

                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    goal.WriteTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, true );
                                }
                                break;
                            case UIAction.OnClick:
                                //goal.OpenPopupShowingThisGoal();
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_MetaGoalHistory
            private void OnUpdate_Content_MetaGoalHistory()
            {
                #region Meta Goal Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );



                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ActivityOverview_MetaGoalHistory" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "GoalsCompleted" )
                                        .AddRaw( SimMetagame.GoalsCompletedInAnyTimeline.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );

                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "GoalsAvailable" )
                                        .AddRaw( SimCommon.AvailableGoals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                foreach ( TimelineGoal goal in TimelineGoalTable.SortedGoals )
                {
                    //filtering
                    if ( timelineGoalCollection != null )
                    {
                        if ( !goal.Collections.ContainsKey( timelineGoalCollection.ID ) )
                            continue;
                    }

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "TimelineGoal", goal.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( goal.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.GetBasicLightTextBlue( isHovered ) ).Space1x();
                                    ExtraData.Buffer.AddRaw( goal.GetDisplayName() );

                                    //todo show how many complete out of how many possible
                                    int completed = 0;
                                    int possible = 0;
                                    foreach ( TimelineGoalPath path in goal.Paths )
                                    {
                                        possible++;
                                        if ( path.Meta_TimesCompleted > 0 )
                                            completed++;
                                    }

                                    ExtraData.Buffer.Position400();
                                    if ( completed < possible )
                                        ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.DataBlue );
                                    else
                                        ExtraData.Buffer.AddFormat2( "OutOF", completed, possible, ColorTheme.TechnologyCountGreen );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    goal.WriteTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false );
                                }
                                break;
                            case UIAction.OnClick:
                                //goal.OpenPopupShowingThisGoal();
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_MetaAchievements
            private void GetAndSortMetaAchievements()
            {
                hasDoneFirstMetaAchievementsSort = true;

                sortedMetaAchievements.Clear();
                foreach ( Achievement row in AchievementTable.Instance.Rows )
                {
                    if ( !row.IsHidden )
                    {
                        //filtering
                        if ( achievementCollection != null )
                        {
                            if ( !row.Collections.ContainsKey( achievementCollection.ID ) )
                                continue;
                        }
                        sortedMetaAchievements.Add( row );
                    }
                }

                sortedMetaAchievements.Sort( delegate ( Achievement Left, Achievement Right )
                {
                    int val = Right.Meta_HasBeenTripped.CompareTo( Left.Meta_HasBeenTripped ); //desc
                    if ( val != 0 )
                        return val;
                    val = Right.Meta_DateTripped.CompareTo( Left.Meta_DateTripped ); //desc
                    if ( val != 0 )
                        return val;
                    return Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                } );
            }

            private void OnUpdate_Content_MetaAchievements()
            {
                GetAndSortMetaAchievements();
                if ( sortedMetaAchievements.Count <= 0 )
                    return;

                bool hasFilterChanged = !(MetaAchievements_FilterText == MetaAchievements_LastFilterText && MetaAchievements_lastTurn == SimCommon.Turn);
                string filterText = MetaAchievements_FilterText;
                MetaAchievements_LastFilterText = MetaAchievements_FilterText;
                MetaAchievements_lastTurn = SimCommon.Turn;

                #region Meta Achievements Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "CrossTimelineAchievements" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    {
                                        int completedEver = 0;
                                        foreach ( Achievement achievement in sortedMetaAchievements )
                                        {
                                            if ( achievement.Meta_HasBeenTripped )
                                                completedEver++;
                                        }

                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "AchievementsTotalCount" )
                                            .AddRaw( sortedMetaAchievements.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                        ExtraData.Buffer.Space8x();
                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "AchievementsEverCompleted" )
                                            .AddRaw( completedEver.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                foreach ( Achievement achievement in sortedMetaAchievements )
                {
                    if ( hasFilterChanged )
                    {
                        if ( filterText.IsEmpty() )
                            achievement.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            achievement.DuringGame_HasBeenFilteredOutInInventory = !achievement.GetMatchesSearchString( filterText );
                    }
                    if ( achievement.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsAchievement( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "MetaAchievement", achievement.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( achievement.Meta_HasBeenTripped ?
                                        achievement.Icon_Normal : achievement.Icon_Locked, AdjustedSpriteStyle.InlineLarger1_8 );
                                    ExtraData.Buffer.Space3x();

                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.StartBold().AddRaw( achievement.GetDisplayName() ).EndBold();
                                    if ( achievement.Meta_HasBeenTripped )
                                    {
                                        ExtraData.Buffer.Position350();
                                        ExtraData.Buffer.AddRaw( achievement.Meta_DateTripped.ToFullDateTimeString() );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    achievement.RenderAchievementTooltip( true, element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                                }
                                break;
                            case UIAction.OnClick:

                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_TimelineAchievements
            private void GetAndSortTimelineAchievements()
            {
                hasDoneFirstTimelineAchievementsSort = true;

                sortedTimelineAchievements.Clear();
                foreach ( Achievement row in AchievementTable.Instance.Rows )
                {
                    if ( !row.IsHidden )
                    {
                        //filtering
                        if ( achievementCollection != null )
                        {
                            if ( !row.Collections.ContainsKey( achievementCollection.ID ) )
                                continue;
                        }
                        sortedTimelineAchievements.Add( row );
                    }
                }

                sortedTimelineAchievements.Sort( delegate ( Achievement Left, Achievement Right )
                {
                    int val = Right.OneTimeline_HasBeenTripped.CompareTo( Left.OneTimeline_HasBeenTripped ); //desc
                    if ( val != 0 )
                        return val;
                    val = Right.OneTimeline_DateTripped.CompareTo( Left.OneTimeline_DateTripped ); //desc
                    if ( val != 0 )
                        return val;
                    val = Right.OneTimeline_TurnTripped.CompareTo( Left.OneTimeline_TurnTripped ); //desc
                    if ( val != 0 )
                        return val;
                    return Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                } );
            }

            private void OnUpdate_Content_TimelineAchievements()
            {
                GetAndSortTimelineAchievements();
                if ( sortedTimelineAchievements.Count <= 0 )
                    return;

                bool hasFilterChanged = !(TimelineAchievements_FilterText == TimelineAchievements_LastFilterText && TimelineAchievements_lastTurn == SimCommon.Turn);
                string filterText = TimelineAchievements_FilterText;
                TimelineAchievements_LastFilterText = TimelineAchievements_FilterText;
                TimelineAchievements_lastTurn = SimCommon.Turn;

                #region This-Timeline Achievements Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ThisTimelineAchievements" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    {
                                        int completedEver = 0;
                                        int completedThisTimeline = 0;
                                        foreach ( Achievement achievement in sortedTimelineAchievements )
                                        {
                                            if ( achievement.Meta_HasBeenTripped )
                                                completedEver++;
                                            if ( achievement.OneTimeline_HasBeenTripped )
                                                completedThisTimeline++;
                                        }

                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "AchievementsTotalCount" )
                                            .AddRaw( sortedTimelineAchievements.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                        ExtraData.Buffer.Space8x();
                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "AchievementsEverCompleted" )
                                            .AddRaw( completedEver.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                        ExtraData.Buffer.Space8x();
                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "AchievementsCompletedThisTimeline" )
                                            .AddRaw( completedThisTimeline.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                foreach ( Achievement achievement in sortedTimelineAchievements )
                {
                    if ( hasFilterChanged )
                    {
                        if ( filterText.IsEmpty() )
                            achievement.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            achievement.DuringGame_HasBeenFilteredOutInInventory = !achievement.GetMatchesSearchString( filterText );
                    }
                    if ( achievement.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsAchievement( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "TimelineAchievement", achievement.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( achievement.OneTimeline_HasBeenTripped ? 
                                        achievement.Icon_Normal : achievement.Icon_Locked, AdjustedSpriteStyle.InlineLarger1_8 );
                                    ExtraData.Buffer.Space3x();

                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.StartBold().AddRaw( achievement.GetDisplayName() ).EndBold();
                                    if ( achievement.OneTimeline_HasBeenTripped )
                                    {
                                        ExtraData.Buffer.Position350();
                                        ExtraData.Buffer.AddRaw( achievement.OneTimeline_DateTripped.ToFullDateTimeString() );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    achievement.RenderAchievementTooltip( false, element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                                }
                                break;
                            case UIAction.OnClick:

                                break;
                        }
                    } );
                }
            }
            #endregion
        }
        #endregion

        #region Supporting Elements
        public enum VictoryTabType
        {
            VictoryOverview,
            TimelineGoals,
            Dooms,
            TimelineAchievements,

            MetaGoalHistory,
            MetaAchievements,
            Length
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "VictoryPath" );
            }
        }

        public class bExit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Close );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        #region dRightOptions
        public class dRightOptions : DropdownAbstractBase
        {
            public static dRightOptions Instance;
            public dRightOptions()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                if ( Item.GetItem() is TimelineGoalCollection timelineGoalColl )
                {
                    timelineGoalCollection = timelineGoalColl;
                }
                else if ( Item.GetItem() is AchievementCollection achievementColl )
                {
                    achievementCollection = achievementColl;
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case VictoryTabType.MetaGoalHistory:
                        #region MetaGoalHistory
                        {
                            TimelineGoalCollection[] validOptions = TimelineGoalCollectionTable.Instance.Rows;

                            TimelineGoalCollection typeDataToSelect = timelineGoalCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            //if ( typeDataToSelect != null )
                            //{
                            //    if ( !validOptions.Contains( typeDataToSelect ) )
                            //    {
                            //        typeDataToSelect = null;
                            //        timelineGoalCollection = null;
                            //    }
                            //}
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Length > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as TimelineGoalCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Length != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    TimelineGoalCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    TimelineGoalCollection optionItemAsType = option.GetItem() as TimelineGoalCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    TimelineGoalCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case VictoryTabType.TimelineGoals:
                        #region TimelineGoals
                        {
                            TimelineGoalCollection[] validOptions = TimelineGoalCollectionTable.Instance.Rows;

                            TimelineGoalCollection typeDataToSelect = timelineGoalCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            //if ( typeDataToSelect != null )
                            //{
                            //    if ( !validOptions.Contains( typeDataToSelect ) )
                            //    {
                            //        typeDataToSelect = null;
                            //        timelineGoalCollection = null;
                            //    }
                            //}
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Length > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as TimelineGoalCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Length != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    TimelineGoalCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    TimelineGoalCollection optionItemAsType = option.GetItem() as TimelineGoalCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    TimelineGoalCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case VictoryTabType.MetaAchievements:
                    case VictoryTabType.TimelineAchievements:
                        #region Achievements
                        {
                            List<AchievementCollection> validOptions = AchievementCollectionTable.SortedCollections;

                            AchievementCollection typeDataToSelect = achievementCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            //if ( typeDataToSelect != null )
                            //{
                            //    if ( !validOptions.Contains( typeDataToSelect ) )
                            //    {
                            //        typeDataToSelect = null;
                            //        achievementCollection = null;
                            //    }
                            //}
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as AchievementCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    AchievementCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    AchievementCollection optionItemAsType = option.GetItem() as AchievementCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    AchievementCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                }
            }

            public override void HandleMouseover()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                if ( elementAsType != null && elementAsType.CurrentlySelectedOption is ArcenDynamicTableRow Row )
                {
                    string description = Row.GetDescription();
                    if ( description.IsEmpty() )
                        return;

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Row ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        Row.WriteOptionDisplayTextForDropdown( novel.TitleUpperLeft );
                        novel.Main.AddRaw( description );
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                if ( Item.GetItem() is ArcenDynamicTableRow Row )
                {
                    string description = Row.GetDescription();
                    if ( description.IsEmpty() )
                        return;

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Row ), ItemElement, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        Row.WriteOptionDisplayTextForDropdown( novel.TitleUpperLeft );
                        novel.Main.AddRaw( description );
                    }
                }
            }

            public override bool GetShouldBeHidden()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case VictoryTabType.TimelineGoals:
                    case VictoryTabType.MetaGoalHistory:
                    case VictoryTabType.MetaAchievements:
                    case VictoryTabType.TimelineAchievements:
                        return false;
                }
                return true;
            }
        }
        #endregion

        #region dLeftOptions
        public class dLeftOptions : DropdownAbstractBase
        {
            public static dLeftOptions Instance;
            public dLeftOptions()
            {
                Instance = this;
            }
            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                //switch ( customParent.currentlyRequestedDisplayTypeCity )
                //{
                //}
            }

            public override bool GetShouldBeHidden()
            {
                //switch ( customParent.currentlyRequestedDisplayTypeCity )
                //{
                //    case CityActivityDisplayType.UnitTypes:
                //        return false;
                //}
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
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case VictoryTabType.TimelineAchievements:
                        TimelineAchievements_FilterText = newString;
                        break;
                    case VictoryTabType.MetaAchievements:
                        MetaAchievements_FilterText = newString;
                        break;
                }
            }

            public override bool GetShouldBeHidden()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case VictoryTabType.TimelineAchievements:
                    case VictoryTabType.MetaAchievements:
                        return false;
                }
                return true;
            }

            public override void OnEndEdit()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {

                    case VictoryTabType.MetaAchievements:
                        MetaAchievements_FilterText = this.GetText();
                        break;
                    case VictoryTabType.TimelineAchievements:
                        TimelineAchievements_FilterText = this.GetText();
                        break;
                }
            }

            private ArcenUI_Input inputField = null;
            private VictoryTabType lastDisplayType = VictoryTabType.Length;

            public override void OnMainThreadUpdate()
            {
                if ( lastDisplayType != customParent.currentlyRequestedDisplayType )
                {
                    switch ( customParent.currentlyRequestedDisplayType )
                    {

                        case VictoryTabType.MetaAchievements:
                            this.SetText( MetaAchievements_FilterText );
                            break;
                        case VictoryTabType.TimelineAchievements:
                            this.SetText( TimelineAchievements_FilterText );
                            break;
                    }
                    lastDisplayType = customParent.currentlyRequestedDisplayType;
                }

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

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    break;
                default:
                    VisCommands.HandleMajorWindowKeyPress( InputActionType );
                    break;
            }
        }
        #endregion

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            public VictoryTabType displayType = VictoryTabType.Length;

            private string NameFromLang => Lang.Get( $"ActivityOverview_{displayType.ToString()}" );
            private string DescriptionFromLang => Lang.Get( $"ActivityOverview_{displayType.ToString()}Description" );

            public void Assign( VictoryTabType displayType )
            {
                this.displayType = displayType;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( displayType != VictoryTabType.Length )
                    customParent.currentlyRequestedDisplayType = displayType;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bCategory", (int)displayType, 0 ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddRaw( NameFromLang );
                    novel.Main.AddRaw( DescriptionFromLang );
                }
            }

            private int countToShow = 0;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                countToShow = 0;
                bool showGrayedOut = false;
                bool isSelected = false;
                if ( this.displayType != VictoryTabType.Length )
                {
                    this.ExtraSpaceAfterInAutoSizing = 0f;
                    isSelected = customParent.currentlyRequestedDisplayType == displayType;
                    switch ( this.displayType )
                    {
                        case VictoryTabType.TimelineGoals:
                            countToShow = -1;
                            showGrayedOut = false;
                            break;
                        case VictoryTabType.Dooms:
                            countToShow = -1;
                            showGrayedOut = !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped || FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped;
                            break;
                        case VictoryTabType.TimelineAchievements:
                            countToShow = sortedTimelineAchievements.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case VictoryTabType.MetaGoalHistory:
                            countToShow = SimMetagame.GoalsCompletedInAnyTimeline;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case VictoryTabType.MetaAchievements:
                            countToShow = sortedMetaAchievements.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case VictoryTabType.VictoryOverview:
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case VictoryTabType.Length:
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( "Forgot to set up bCategory-GetTextToShowFromVolatile for " + this.displayType + "!", Verbosity.ShowAsError );
                            break;
                    }

                    this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                    Buffer.StartColor( isSelected ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) :
                        (showGrayedOut ? ColorTheme.GetDisabledPurple( this.Element.LastHadMouseWithin ) : ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin )) );

                    Buffer.AddRaw( NameFromLang ).EndColor();
                }
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                if ( countToShow >= 0 )
                {
                    this.SetRelatedImage1EnabledIfNeeded( true );
                    Buffer.AddRaw( countToShow.ToString() );
                }
                else
                    this.SetRelatedImage1EnabledIfNeeded( false );
            }

            public override bool GetShouldBeHidden() => displayType == VictoryTabType.Length;

            public override void Clear()
            {
                displayType = VictoryTabType.Length;
            }
        }
        #endregion

        public const float CONTENT_ROW_HEIGHT_SHORT = 22f;
        public const float CONTENT_ROW_HEIGHT_TALL = 32f;
        public const float CONTENT_ROW_HEIGHT_ACHIEVEMENT = 28f;
        public const float CONTENT_ROW_GAP_SHORT = 2.55f;
        public const float CONTENT_ROW_GAP_TALL = 2.55f;

        #region bDataFullItem
        public class bDataFullItem : ButtonAbstractBase
        {
            public static bDataFullItem Original;
            public bDataFullItem() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;
            private int lastID3 = 0;
            private IGameNote lastNote = null;

            public override void OnUpdateSub() { }

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
                    this.lastID3 = 0;
                    this.lastNote = null;
                    ( this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( string ID1, int ID3, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID3 != ID3 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = string.Empty;
                    this.lastID3 = ID3;
                    this.lastNote = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( IGameNote Note, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastNote != Note )
                {
                    this.lastID1 = string.Empty;
                    this.lastID2 = string.Empty;
                    this.lastID3 = 0;
                    this.lastNote = null;
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

        #region bDataHeader
        public class bDataHeader : ButtonAbstractBase
        {
            public static bDataHeader Original;
            public bDataHeader() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;
            private IGameNote lastNote = null;

            public override void OnUpdateSub() { }

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
                    this.lastNote = null;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public void Assign( IGameNote Note, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastNote != Note )
                {
                    this.lastID1 = string.Empty;
                    this.lastID2 = string.Empty;
                    this.lastNote = null;
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

        #region bResourceItem
        public class bResourceItem : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region bUnitType
        public class bUnitType : ButtonAbstractBase
        {
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region bDataAdjustedItem
        public class bDataAdjustedItem : ButtonAbstractBase
        {
            public static bDataAdjustedItem Original;
            public bDataAdjustedItem() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;

            public override void OnUpdateSub() { }

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
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

        #region bLargeTwoLineHeader
        public class bLargeTwoLineHeader : ButtonAbstractBase
        {
            public static bLargeTwoLineHeader Instance;
            public bLargeTwoLineHeader() { if ( Instance == null ) Instance = this; }

            public GetOrSetUIData UIDataController;

            public override void OnUpdateSub() { }

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
    }
}

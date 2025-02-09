using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_GoalCompleteSideWindow : WindowControllerAbstractBase
    {
        public static Window_GoalCompleteSideWindow Instance;
        public override bool PutMeOnTheEscapeCloseStack => false;
		public Window_GoalCompleteSideWindow()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "TimelineGoalCompleted" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !SimCommon.ShouldBeShowingPostGoalScreen )
                return false;

            if ( VisCommands.GetIsAnyBigWindowOpen() )
                return false;
            if ( Window_SystemMenu.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_LoadGameMenu.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SaveGameMenu.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_SettingsMenu.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Window_ControlBindingsMenu.Instance.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                return false;

            return true;
        }

        private static int lastSetRandomItemsSeed = -10;
        private static MersenneTwister workingRand = new MersenneTwister( 0 );
        private static readonly List<CityStatistic> workingStatisticsA = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsA" );
        private static readonly List<CityStatistic> workingStatisticsB = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsB" );
        private static readonly List<CityStatistic> workingStatisticsC = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsC" );
        private static readonly List<CityStatistic> workingStatisticsD = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsD" );
        private static readonly List<CityStatistic> workingStatisticsE = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsE" );
        private static readonly List<CityStatistic> workingStatisticsF = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsF" );
        private static readonly List<CityStatistic> workingStatisticsG = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatisticsG" );
        private static readonly List<CityStatistic> workingRemainder = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-workingRemainder" );
        private static readonly List<CityStatistic> randomStatistics = List<CityStatistic>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomStatistics" );
        private static readonly List<Achievement> randomAchievements = List<Achievement>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-randomAchievements" );
        private static readonly List<Achievement> workingAchievements = List<Achievement>.Create_WillNeverBeGCed( 40, "Window_GoalCompleteSideWindow-workingAchievements" );

        public override void Close( WindowCloseReason CloseReason )
        {
        }

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_GoalCompleteSideWindow.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bDataItem> bDataItemPool;
            private static ButtonAbstractBase.ButtonPool<bDataHeader> bDataHeaderPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.ExtraOffsetX = -(Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled());

                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bDataItem.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bDataItemPool = new ButtonAbstractBase.ButtonPool<bDataItem>( bDataItem.Original, 10, "bDataItem" );
                            bDataHeaderPool = new ButtonAbstractBase.ButtonPool<bDataHeader>( bDataHeader.Original, 10, "bDataHeader" );
                        }
                    }
                    #endregion

                    float offsetFromSide = 0;
                    if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                        offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                    else //the sidebar is on, and is on the left, move right
                        offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                    this.WindowController.ExtraOffsetX = offsetFromSide;

                    bDataItemPool.Clear( 60 );
                    bDataHeaderPool.Clear( 60 );

                    RectTransform rTran = (RectTransform)bDataItem.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = topBuffer;

                    this.OnUpdate_Content();

                    #region Positioning Logic
                    //Now size the parent, called Content, to get scrollbars to appear if needed.
                    Vector2 sizeDelta = rTran.sizeDelta;
                    sizeDelta.y = MathA.Abs( runningY );
                    rTran.sizeDelta = sizeDelta;
                    #endregion

                    SizingRect = this.Element.RelevantRect;
                }
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 400; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load
            private const float MAIN_CONTENT_WIDTH = 366.92f;
            private const float CONTENT_ROW_GAP = 0.55f;
            private const float CONTENT_ROW = 15.9f;
            private const float HEADER_ROW = 17.4f;

            private const float SECTION_GAP = 8.55f;

            public const float CONTENT_ROW_HEIGHT_ACHIEVEMENT = 28f;

            #region CalculateBoundsSingle
            protected float leftBuffer = 5.1f;
            protected float topBuffer = -6;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW );

                innerY -= CONTENT_ROW + CONTENT_ROW_GAP;
            }

            protected void CalculateBoundsAchievement( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW_HEIGHT_ACHIEVEMENT );

                innerY -= CONTENT_ROW_HEIGHT_ACHIEVEMENT + CONTENT_ROW_GAP;
            }

            protected void CalculateBoundsHeader( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, HEADER_ROW );

                innerY -= HEADER_ROW + CONTENT_ROW_GAP;
            }
            #endregion

            private void GetAndSortRandomItems()
            {
                lastSetRandomItemsSeed = SimCommon.PostGoalRNGSeed;
                workingRand.ReinitializeWithSeed( SimCommon.PostGoalRNGSeed );

                GetAndSortRandomStatistics();
                GetAndSortRandomAchievements();
            }

            #region GetAndSortRandomStatistics
            private void GetAndSortRandomStatistics()
            {
                randomStatistics.Clear();
                workingStatisticsA.Clear();
                workingStatisticsB.Clear();
                workingStatisticsC.Clear();
                workingStatisticsD.Clear();
                workingStatisticsE.Clear();
                workingStatisticsF.Clear();
                workingStatisticsG.Clear();
                workingRemainder.Clear();

                foreach ( CityStatistic stat in CityStatisticTable.Instance.Rows )
                {
                    if ( stat.GetScore() <= 0 )
                        continue;
                    if ( stat.IsGroupAInterestingIfAboveZero )
                        workingStatisticsA.Add( stat );
                    else if ( stat.IsGroupBInterestingIfAboveZero )
                        workingStatisticsB.Add( stat );
                    else if ( stat.IsGroupCInterestingIfAboveZero )
                        workingStatisticsC.Add( stat );
                    else if ( stat.IsGroupDInterestingIfAboveZero )
                        workingStatisticsD.Add( stat );
                    else if ( stat.IsGroupEInterestingIfAboveZero )
                        workingStatisticsE.Add( stat );
                    else if ( stat.IsGroupFInterestingIfAboveZero )
                        workingStatisticsF.Add( stat );
                    else if ( stat.IsGroupGInterestingIfAboveZero )
                        workingStatisticsG.Add( stat );
                }

                int desiredFromA = 4;
                int desiredFromB = 4;
                int desiredFromC = 4;
                int desiredFromD = 4;

                while ( desiredFromA > 0 )
                {
                    if ( workingStatisticsA.Count == 0 )
                        break;

                    randomStatistics.Add( workingStatisticsA.GetRandomAndRemove( workingRand ) );
                    desiredFromA--;
                }

                while ( desiredFromB > 0 )
                {
                    if ( workingStatisticsB.Count == 0 )
                        break;

                    randomStatistics.Add( workingStatisticsB.GetRandomAndRemove( workingRand ) );
                    desiredFromB--;
                }

                while ( desiredFromC > 0 )
                {
                    if ( workingStatisticsC.Count == 0 )
                        break;

                    randomStatistics.Add( workingStatisticsC.GetRandomAndRemove( workingRand ) );
                    desiredFromC--;
                }

                while ( desiredFromD > 0 )
                {
                    if ( workingStatisticsD.Count == 0 )
                        break;

                    randomStatistics.Add( workingStatisticsD.GetRandomAndRemove( workingRand ) );
                    desiredFromD--;
                }

                int totalRemaining = desiredFromA + desiredFromB + desiredFromC + desiredFromD;
                if ( totalRemaining > 0 )
                {
                    workingRemainder.Clear();
                    if ( workingStatisticsA.Count > 0 )
                        workingRemainder.AddRange( workingStatisticsA );
                    if ( workingStatisticsB.Count > 0 )
                        workingRemainder.AddRange( workingStatisticsB );
                    if ( workingStatisticsC.Count > 0 )
                        workingRemainder.AddRange( workingStatisticsC );
                    if ( workingStatisticsD.Count > 0 )
                        workingRemainder.AddRange( workingStatisticsD );

                    while ( totalRemaining > 0 )
                    {
                        if ( workingRemainder.Count == 0 )
                            break;

                        randomStatistics.Add( workingRemainder.GetRandomAndRemove( workingRand ) );
                        totalRemaining--;
                    }
                }

                if ( totalRemaining > 0 && workingStatisticsE.Count> 0 )
                {
                    while ( totalRemaining > 0 )
                    {
                        if ( workingStatisticsE.Count == 0 )
                            break;

                        randomStatistics.Add( workingStatisticsE.GetRandomAndRemove( workingRand ) );
                        totalRemaining--;
                    }
                }

                if ( totalRemaining > 0 && workingStatisticsF.Count > 0 )
                {
                    while ( totalRemaining > 0 )
                    {
                        if ( workingStatisticsF.Count == 0 )
                            break;

                        randomStatistics.Add( workingStatisticsF.GetRandomAndRemove( workingRand ) );
                        totalRemaining--;
                    }
                }

                if ( totalRemaining > 0 && workingStatisticsG.Count > 0 )
                {
                    while ( totalRemaining > 0 )
                    {
                        if ( workingStatisticsG.Count == 0 )
                            break;

                        randomStatistics.Add( workingStatisticsG.GetRandomAndRemove( workingRand ) );
                        totalRemaining--;
                    }
                }

                workingStatisticsA.Clear();
                workingStatisticsB.Clear();
                workingStatisticsC.Clear();
                workingStatisticsD.Clear();
                workingStatisticsE.Clear();
                workingStatisticsF.Clear();
                workingStatisticsG.Clear();
                workingRemainder.Clear();
            }
            #endregion

            #region GetAndSortRandomAchievements
            private void GetAndSortRandomAchievements()
            {
                randomAchievements.Clear();
                workingAchievements.Clear();

                AddWorkingAchievements( "Tier1Goals", false );
                if ( workingAchievements.Count < 4 )
                {
                    AddWorkingAchievements( "SideExploration", false );
                    AddWorkingAchievements( "SideChallenge", false );
                    AddWorkingAchievements( "DangerousChoices", false );
                    AddWorkingAchievements( "EpicUnlocks", false );
                    AddWorkingAchievements( "SarcasticAccomplishments", false );
                }
                if ( workingAchievements.Count < 4 )
                {
                    AddWorkingAchievements( "HardMode", false );
                }
                if ( workingAchievements.Count < 4 )
                {
                    AddWorkingAchievements( "ExtremeMode", false );
                }
                if ( workingAchievements.Count < 4 )
                {
                    foreach ( Achievement achieve in AchievementTable.Instance.Rows )
                    {
                        if ( achieve.Meta_HasBeenTripped )
                            continue;
                        workingAchievements.AddIfNotAlreadyIn( achieve );
                    }
                }

                int totalRemaining = 4;
                while ( totalRemaining > 0 )
                {
                    if ( workingAchievements.Count == 0 )
                        break;

                    randomAchievements.Add( workingAchievements.GetRandomAndRemove( workingRand ) );
                    totalRemaining--;
                }
                workingAchievements.Clear();
            }

            private static void AddWorkingAchievements( string CollectionID, bool AllowTripped )
            {
                AchievementCollection coll = AchievementCollectionTable.Instance.GetRowByIDOrNullIfNotFound( CollectionID );
                if ( coll == null || coll.List.Count == 0 )
                    return;

                foreach ( Achievement achieve in coll.List )
                {
                    if ( !AllowTripped && achieve.Meta_HasBeenTripped )
                        continue;
                    workingAchievements.AddIfNotAlreadyIn( achieve );
                }
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                if ( lastSetRandomItemsSeed != SimCommon.PostGoalRNGSeed )
                    GetAndSortRandomItems();

                if ( randomStatistics.Count > 0 )
                {
                    this.AddHeader( ref runningY, "SelectedStats" );
                    foreach ( CityStatistic stat in randomStatistics )
                        this.AddStatistic( ref runningY, stat );

                    for ( int i = randomStatistics.Count; i < 16;i++ )
                    {
                        this.CalculateBoundsSingle( out Rect bounds, ref runningY ); //add extra spacing for stats that would have been there
                    }

                    runningY -= SECTION_GAP;
                }

                if ( randomAchievements.Count > 0 )
                {
                    this.AddHeader( ref runningY, "OtherAchievements" );
                    foreach ( Achievement achieve in randomAchievements )
                        this.AddAchievement( ref runningY, achieve );
                }
            }
            #endregion

            #region AddHeader
            private bool AddHeader( ref float runningY, string HeaderID )
            {
                this.CalculateBoundsHeader( out Rect bounds, ref runningY );

                bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataHeaderPool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "Header", HeaderID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                switch ( HeaderID )
                                {
                                    case "SelectedStats":
                                        ExtraData.Buffer.AddLang( "TimelineGoalCompletedSection_SelectedStats" );
                                        break;
                                    case "OtherAchievements":
                                        ExtraData.Buffer.AddLang( "TimelineGoalCompletedSection_OtherAchievements" );
                                        break;
                                }
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Header", HeaderID ), element,
                                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                                {
                                    switch ( HeaderID )
                                    {
                                        case "SelectedStats":
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.TitleUpperLeft.AddLang( "TimelineGoalCompletedSection_SelectedStats" );
                                            novel.Main.AddLeftClickFormat( "TimelineGoalCompletedSection_SelectedStats_Tooltip" );
                                            break;
                                        case "OtherAchievements":
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.TitleUpperLeft.AddLang( "TimelineGoalCompletedSection_OtherAchievements" );
                                            novel.Main.AddLang( "TimelineGoalCompletedSection_OtherAchievements_Tooltip" );
                                            break;
                                    }
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                switch ( HeaderID )
                                {
                                    case "SelectedStats":
                                        VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.ActionStatistics );
                                        break;
                                    case "OtherAchievements":
                                        VisCommands.ToggleVictoryPath_TargetTab( Window_VictoryPath.VictoryTabType.TimelineAchievements );
                                        break;
                                }
                            }
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region AddStatistic
            private bool AddStatistic( ref float runningY, CityStatistic stat )
            {
                if ( stat == null ) 
                    return false;
                this.CalculateBoundsSingle( out Rect bounds, ref runningY );

                bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataHeaderPool.ApplySingleItemInRow( row, bounds, true );

                row.Assign( "CityStatistic", stat.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                ExtraData.Buffer.AddRaw( stat.GetScore().ToStringThousandsWhole() ).Position100();
                                stat.AppendDisplayName( ExtraData.Buffer );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                string desc = stat.GetDescription();

                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( stat.GetTypeCode(), stat.GetID() ), element,
                                    SideClamp.LeftOrRight, desc.IsEmpty() ? TooltipNovelWidth.SizeToText : TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    stat.AppendDisplayName( novel.TitleUpperLeft );
                                    novel.TitleUpperLeft.AfterLineItemHeader().AddRaw( stat.GetScore().ToStringThousandsWhole(), ColorTheme.DataBlue );
                                    if ( !desc.IsEmpty() )
                                        novel.Main.AddRaw( desc );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region AddAchievement
            private bool AddAchievement( ref float runningY, Achievement achievement )
            {
                if ( achievement == null )
                    return false;
                this.CalculateBoundsAchievement( out Rect bounds, ref runningY );

                bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataHeaderPool.ApplySingleItemInRow( row, bounds, true );

                row.Assign( "Achievement", achievement.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
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
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                achievement.RenderAchievementTooltip( true, element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard );
                            }
                            break;
                        case UIAction.OnClick:
                            break;
                    }
                } );
                return true;
            }
            #endregion
        }

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

        #region bDataItem
        public class bDataItem : ButtonAbstractBaseWithImage
        {
            public static bDataItem Original;
            public bDataItem() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private IGameNote lastNote = null;

            public void Assign( IGameNote Note, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastNote != Note )
                {
                    this.lastNote = Note;
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

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                SimCommon.ShouldBeShowingPostGoalScreen = false;
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bStartNewTimeline
        public class bStartNewTimeline : ButtonAbstractBase
        {
            public static bStartNewTimeline Original;
            public bStartNewTimeline() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "TimelineGoalCompletedButton_StartNewTimeline",
                    FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetGrayer( this.Element.LastHadMouseWithin ) );
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bStartNewTimeline", "Button" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    if ( novel.TooltipWidth < 400 )
                        novel.TooltipWidth = 400;
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "TimelineGoalCompletedButton_StartNewTimeline" );
                    novel.Main.AddLang( "TimelineGoalCompletedButton_StartNewTimeline_Tooltip", ColorTheme.NarrativeColor ).Line();
                    novel.Main.AddLang( "TimelineGoalCompletedButton_HigherDifficulty_Tooltip", ColorTheme.PurpleDim ).Line();
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }
                if ( Engine_HotM.GameMode != MainGameMode.TheEndOfTime )
                    VisCommands.ToggleEndOfTimeMode();
                SimCommon.ShouldBeShowingPostGoalScreen = false;
                return MouseHandlingResult.None;
            }
        }
        #endregion


        #region bContinueThisTimeline
        public class bContinueThisTimeline : ButtonAbstractBase
        {
            public static bContinueThisTimeline Original;
            public bContinueThisTimeline() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped )
                {
                    Buffer.AddLang( "TimelineGoalCompletedButton_JustOneMoreGoal", ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
                }
                else
                {
                    Buffer.AddLang( "TimelineGoalCompletedButton_KeepExploring", ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
                }
            }

            public override void HandleMouseover()
            {
                if ( FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped )
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bStartNewTimeline", "Button" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        if ( novel.TooltipWidth < 400 )
                            novel.TooltipWidth = 400;
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        novel.TitleUpperLeft.AddLang( "TimelineGoalCompletedButton_JustOneMoreGoal" );
                        novel.Main.AddLang( "TimelineGoalCompletedButton_JustOneMoreGoal_Tooltip", ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLang( "TimelineGoalCompletedButton_JustOneMoreGoal_TooltipP2", ColorTheme.PurpleDim ).Line();
                    }
                }
                else
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bStartNewTimeline", "Button" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        if ( novel.TooltipWidth < 400 )
                            novel.TooltipWidth = 400;
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        novel.TitleUpperLeft.AddLang( "TimelineGoalCompletedButton_KeepExploring" );
                        novel.Main.AddLang( "TimelineGoalCompletedButton_KeepExploring_Tooltip", ColorTheme.NarrativeColor ).Line();
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                SimCommon.ShouldBeShowingPostGoalScreen = false;
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

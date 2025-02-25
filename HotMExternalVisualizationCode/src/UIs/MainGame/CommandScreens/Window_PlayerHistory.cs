using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_PlayerHistory : ToggleableWindowController, IInputActionHandler
    {
        #region Main Controller
        public static Window_PlayerHistory Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_PlayerHistory()
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
            hasDoneFirstCityStatisticsSort = false;
            sortedCitywideStatistics.Clear();
            sortedCityActionStatistics.Clear();

            hasDoneFirstMetaStatisticsSort = false;
            sortedMetaStatistics.Clear();

            hasDoneFirstInventionSort = false;
            sortedInventions.Clear();

            hasDoneFirstUpgradeSort = false;
            sortedUpgrades.Clear();

            hasDoneFirstEventLogSort = false;
            sortedEvents.Clear();

            hasDoneFirstMetaEventLogSort = false;
            sortedMetaEvents.Clear();

            MajorLocationCategory = null;
            hasDoneFirstMajorLocationsSort = false;
            sortedMajorLocations.Clear();
            majorLocationCategories.Clear();

            CohortCategory = null;
            hasDoneFirstCohortsSort = false;
            sortedCohorts.Clear();
            cohortCategories.Clear();

            hasDoneFirstCityConflictSort = false;
            sortedCityConflicts_Active.Clear();
            sortedCityConflicts_Complete.Clear();

            EventLog_FilterText = string.Empty;
            eventLogCollection = null;
            Unlock_FilterText = string.Empty;
            unlockCollection = null;
            Upgrade_FilterText = string.Empty;
            upgradeCollection = null;
            ProjectHistory_FilterText = string.Empty;
            projectCollection = null;
            ActionStatistics_FilterText = string.Empty;
            CityWideStatistics_FilterText = string.Empty;
            MetaEventLog_FilterText = string.Empty;
            MetaStatistics_FilterText = string.Empty;
            MajorLocations_FilterText = string.Empty;
            Cohorts_FilterText = string.Empty;
            CityConflicts_FilterText = string.Empty;

            contemplationCollection = null;
            streetSenseCollection = null;
            explorationSiteCollection = null;

            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );

            foreach ( ActorDataType row in ActorDataTypeTable.Instance.Rows )
            {
                row.DuringGameplay_SkillCheckSuccesses.ResetCombinedName();
                row.DuringGameplay_SkillCheckFailures.ResetCombinedName();
            }

            base.OnOpen();
        }

        private static bool hasDoneFirstEventLogSort = false;
        private static readonly List<NoteLog.NoteDisplayData> sortedEvents = List<NoteLog.NoteDisplayData>.Create_WillNeverBeGCed( 40, "Window_PlayerTechnologies-sortedEvents" );
        private static readonly List<NoteLog.NoteDisplayData> sortedEventsWorking = List<NoteLog.NoteDisplayData>.Create_WillNeverBeGCed( 40, "Window_PlayerTechnologies-sortedEvents" );
        private static int PermanentEvents = 0;
        private static int EphemeralEvents = 0;

        private static bool hasDoneFirstMetaEventLogSort = false;
        private static readonly List<IGameNote> sortedMetaEvents = List<IGameNote>.Create_WillNeverBeGCed( 40, "Window_PlayerTechnologies-sortedMetaEvents" );

        private static bool hasDoneFirstInventionSort = false;
        private static readonly List<Unlock> sortedInventions = List<Unlock>.Create_WillNeverBeGCed( 40, "Window_PlayerTechnologies-sortedInventions" );

        private static bool hasDoneFirstUpgradeSort = false;
        private static readonly List<IUpgrade> sortedUpgrades = List<IUpgrade>.Create_WillNeverBeGCed( 40, "Window_PlayerTechnologies-sortedUpgrades" );

        private static string EventLog_FilterText = string.Empty;
        private static string EventLog_LastFilterText = string.Empty;
        private static int EventLog_lastTurn = 0;
        private static NoteInstructionCollection eventLogCollection = null;
        private static NoteInstructionCollection lastEventLogCollection = null;

        private static string Unlock_FilterText = string.Empty;
        private static string Unlock_LastFilterText = string.Empty;
        private static int Unlock_lastTurn = 0;
        private static UnlockCollection unlockCollection = null;

        private static string Upgrade_FilterText = string.Empty;
        private static string Upgrade_LastFilterText = string.Empty;
        private static int Upgrade_lastTurn = 0;
        private static int Upgrade_completed = 0;
        private static UpgradeCollection upgradeCollection = null;

        private static string ProjectHistory_FilterText = string.Empty;
        private static string ProjectHistory_LastFilterText = string.Empty;
        private static int ProjectHistory_lastTurn = 0;
        private static MachineProjectCollection projectCollection = null;

        private static ContemplationCollection contemplationCollection = null;
        private static StreetSenseCollection streetSenseCollection = null;
        private static ExplorationSiteCollection explorationSiteCollection = null;

        private static string ActionStatistics_FilterText = string.Empty;
        private static string ActionStatistics_LastFilterText = string.Empty;
        private static int ActionStatistics_lastTurn = 0;

        private static string CityWideStatistics_FilterText = string.Empty;
        private static string CityWideStatistics_LastFilterText = string.Empty;
        private static int CityWideStatistics_lastTurn = 0;

        private static string MetaEventLog_FilterText = string.Empty;
        private static string MetaEventLog_LastFilterText = string.Empty;
        private static int MetaEventLog_lastTurn = 0;

        private static string MetaStatistics_FilterText = string.Empty;
        private static string MetaStatistics_LastFilterText = string.Empty;
        private static int MetaStatistics_lastTurn = 0;

        private static string MajorLocations_FilterText = string.Empty;
        private static string MajorLocations_LastFilterText = string.Empty;
        private static int MajorLocations_lastTurn = 0;

        private static bool hasDoneFirstCityStatisticsSort = false;
        private static readonly List<IStatisticSource> sortedCitywideStatistics = List<IStatisticSource>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedCitywideStatistics" );
        private static readonly List<IStatisticSource> sortedCityActionStatistics = List<IStatisticSource>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedCityActionStatistics" );

        private static bool hasDoneFirstMetaStatisticsSort = false;
        private static readonly List<IStatisticSource> sortedMetaStatistics = List<IStatisticSource>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedMetaStatistics" );

        private static MajorLocationAggregationType lastMajorLocationCategory = null;
        private static MajorLocationAggregationType MajorLocationCategory = null;
        private static bool hasDoneFirstMajorLocationsSort = false;
        private static readonly List<IAggregatedMajorLocationDimension> sortedMajorLocations = List<IAggregatedMajorLocationDimension>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedMajorLocations" );
        private static readonly List<MajorLocationAggregationType> majorLocationCategories = List<MajorLocationAggregationType>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-majorLocationCategories" );

        private static string Cohorts_FilterText = string.Empty;
        private static string Cohorts_LastFilterText = string.Empty;
        private static int Cohorts_lastTurn = 0;

        private static NPCCohortCategory lastCohortCategory = null;
        private static NPCCohortCategory CohortCategory = null;
        private static bool hasDoneFirstCohortsSort = false;
        private static readonly List<NPCCohort> sortedCohorts = List<NPCCohort>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedCohorts" );
        private static readonly List<NPCCohortCategory> cohortCategories = List<NPCCohortCategory>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-cohortCategories" );

        private static string CityConflicts_FilterText = string.Empty;
        private static string CityConflicts_LastFilterText = string.Empty;
        private static int CityConflicts_lastTurn = 0;

        private static bool hasDoneFirstCityConflictSort = false;
        private static readonly List<CityConflict> sortedCityConflicts_Active = List<CityConflict>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedCityConflicts" );
        private static readonly List<CityConflict> sortedCityConflicts_Complete = List<CityConflict>.Create_WillNeverBeGCed( 40, "Window_PlayerHistory-sortedCityConflicts" );

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public static HistoryType currentlyRequestedDisplayType = HistoryType.EventLog;

            public customParent()
            {
                Window_PlayerHistory.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;
            private static ButtonAbstractBase.ButtonPool<bDataFullItem> bDataFullItemPool;
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
                for ( HistoryType displayType = HistoryType.EventLog; displayType < HistoryType.Length; displayType++ )
                {
                    if ( chapterNumber < 3 && displayType == HistoryType.MetaEventLog )
                        break; //don't show the meta ones until chapter 3

                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
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
                bDataHeaderPool.Clear( 60 );

                RectTransform rTran = (RectTransform)bDataFullItem.Original.Element.RelevantRect.parent;

                maxYToShow = -rTran.anchoredPosition.y;
                minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                maxYToShow += EXTRA_BUFFER;
                yTopOffset = -((rTran.anchoredPosition.y - topBuffer) + TOP_OFFSET_BUFFER);

                runningY = topBuffer;
                {
                    if ( !hasDoneFirstMetaStatisticsSort )
                        GetAndSortMetaStatistics(); //note, the way the filtering is done is different here

                    if ( !hasDoneFirstMetaEventLogSort || MetaEventLog_LastFilterText != MetaEventLog_FilterText || MetaEventLog_lastTurn != SimCommon.Turn )
                    {
                        GetAndSortMetaEventLog();
                        MetaEventLog_LastFilterText = MetaEventLog_FilterText;
                        MetaEventLog_lastTurn = SimCommon.Turn;
                    }

                    if ( !hasDoneFirstInventionSort || Unlock_FilterText != Unlock_LastFilterText || ( Unlock_lastTurn != SimCommon.Turn && !SimCommon.IsCurrentlyRunningSimTurn ) )
                    {
                        GetAndSortUnlocks();
                        Unlock_LastFilterText = Unlock_FilterText;
                        Unlock_lastTurn = SimCommon.Turn;
                    }
                    if ( !hasDoneFirstUpgradeSort || Upgrade_LastFilterText != Upgrade_FilterText || Upgrade_lastTurn != SimCommon.Turn || Upgrade_completed != UnlockCoordinator.CompletedUpgrades )
                    {
                        GetAndSortUpgrades();
                        Upgrade_LastFilterText = Upgrade_FilterText;
                        Upgrade_lastTurn = SimCommon.Turn;
                        Upgrade_completed = UnlockCoordinator.CompletedUpgrades;
                    }

                    if ( !hasDoneFirstMajorLocationsSort || MajorLocations_LastFilterText != MajorLocations_FilterText || 
                        MajorLocations_lastTurn != SimCommon.Turn || lastMajorLocationCategory != MajorLocationCategory )
                    {
                        GetAndSortMajorLocations();
                        MajorLocations_LastFilterText = MajorLocations_FilterText;
                        MajorLocations_lastTurn = SimCommon.Turn;
                        lastMajorLocationCategory = MajorLocationCategory;
                    }

                    if ( !hasDoneFirstCohortsSort || Cohorts_LastFilterText != Cohorts_FilterText ||
                        ( Cohorts_lastTurn != SimCommon.Turn && !SimCommon.IsCurrentlyRunningSimTurn ) || lastCohortCategory != CohortCategory )
                    {
                        GetAndSortCohorts();
                        Cohorts_LastFilterText = Cohorts_FilterText;
                        Cohorts_lastTurn = SimCommon.Turn;
                        lastCohortCategory = CohortCategory;
                    }

                    if ( !hasDoneFirstCityConflictSort || CityConflicts_LastFilterText != CityConflicts_FilterText ||
                        ( CityConflicts_lastTurn != SimCommon.Turn && !SimCommon.IsCurrentlyRunningSimTurn ) )
                    {
                        GetAndSortCityConflicts();
                        CityConflicts_LastFilterText = CityConflicts_FilterText;
                        CityConflicts_lastTurn = SimCommon.Turn;
                    }

                    if ( eventLogCollection == null )
                        eventLogCollection = NoteInstructionCollectionTable.SortedCollections[0];

                    if ( !hasDoneFirstEventLogSort || EventLog_LastFilterText != EventLog_FilterText || EventLog_lastTurn != SimCommon.Turn ||
                        lastEventLogCollection != eventLogCollection )
                    {
                        GetAndSortEventLog();
                        EventLog_LastFilterText = EventLog_FilterText;
                        EventLog_lastTurn = SimCommon.Turn;
                        lastEventLogCollection = eventLogCollection;
                    }

                    if ( !hasDoneFirstCityStatisticsSort )
                        GetAndSortCityStatistics(); //note, the way the filtering is done is different here

                    switch ( currentlyRequestedDisplayType )
                    {
                        case HistoryType.MetaEventLog:
                            OnUpdate_Content_MetaEventLog();
                            break;
                        case HistoryType.MetaStatistics:
                            OnUpdate_Content_MetaStatistics();
                            break;
                        case HistoryType.EventLog:
                            OnUpdate_Content_EventLog();
                            break;
                        case HistoryType.UnlockHistory:
                            OnUpdate_Content_Unlocks();
                            break;
                        case HistoryType.Upgrades:
                            OnUpdate_Content_Upgrades();
                            break;
                        case HistoryType.NamedThings:
                            OnUpdate_Content_NamedThings();
                            break;
                        case HistoryType.KeyContacts:
                            OnUpdate_Content_KeyContacts();
                            break;
                        case HistoryType.Contemplations:
                            OnUpdate_Content_Contemplations();
                            break;
                        case HistoryType.StreetSense:
                            OnUpdate_Content_StreetSense();
                            break;
                        case HistoryType.ExplorationSites:
                            OnUpdate_Content_ExplorationSites();
                            break;
                        case HistoryType.MajorLocations:
                            OnUpdate_Content_MajorLocations();
                            break;
                        case HistoryType.Cohorts:
                            OnUpdate_Content_Cohorts();
                            break;
                        case HistoryType.CityConflicts:
                            OnUpdate_Content_CityConflicts();
                            break;
                        case HistoryType.CityStatistics:
                            OnUpdate_Content_CityStatistics( sortedCitywideStatistics, true, true );
                            break;
                        case HistoryType.ActionStatistics:
                            OnUpdate_Content_CityStatistics( sortedCityActionStatistics, false, false );
                            break;
                        case HistoryType.ProjectHistory:
                            OnUpdate_Content_ProjectHistory();
                            break;
                        case HistoryType.Length:
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( $"Error! Player History tried to render with a category of {currentlyRequestedDisplayType.ToString()}, this should be impossible. Acting as though it's requesting Primary Resources instead.", Verbosity.ShowAsError );
                            currentlyRequestedDisplayType = HistoryType.EventLog;
                            return;
                    }
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

            #region OnUpdate_Content_EventLog
            private void GetAndSortEventLog()
            {
                hasDoneFirstEventLogSort = true;
                sortedEventsWorking.Clear();
                IGameNote lastTurnHeader = null;

                PermanentEvents = 0;
                EphemeralEvents = 0;

                for ( int i = 0; i < NoteLog.LongTermGameLog.Count; i++ )
                {
                    IGameNote note = NoteLog.LongTermGameLog[i];
                    if ( !note.HandleNote( GameNoteAction.GetIsStillValid, null, false, null, null, string.Empty, 0, false ) )
                        continue;

                    NoteInstruction inst = note.Instruction;

                    bool isHeader = (inst?.HeaderType ?? 0) == 2;

                    if ( note.PruneAfter > 0 )
                    {
                        EphemeralEvents++;
                        if ( eventLogCollection.ExcludeEphemeral )
                            continue; //ephemeral
                    }
                    else
                    {
                        if ( !isHeader )
                        {
                            PermanentEvents++;

                            if ( eventLogCollection.ExcludePermanent )
                                continue; //ephemeral
                        }
                    }

                    if ( !isHeader && !EventLog_FilterText.IsEmpty() )
                    {
                        if ( !note.HandleNote( GameNoteAction.GetMatchesSearchString, null, false, null, null, EventLog_FilterText, 0, false ) )
                            continue;
                    }

                    if ( inst != null && inst.HeaderType <= 0 )
                    {
                        if ( eventLogCollection != null && !eventLogCollection.TypesDict.ContainsKey( inst.ID ) )
                            continue;
                    }

                    if ( isHeader )
                        lastTurnHeader = note;

                    sortedEventsWorking.Add( NoteLog.NoteDisplayData.Create( note, isHeader, lastTurnHeader ) );
                }

                sortedEvents.Clear();

                bool lastWasHeader = false;
                for ( int i = sortedEventsWorking.Count - 1; i >= 0; i-- )
                {
                    NoteLog.NoteDisplayData data = sortedEventsWorking[i];
                    if ( data.IsHeader )
                    {
                        if ( lastWasHeader )
                        {
                            //if ( i - 1 >= 0 )
                            {
                                //if ( sortedEventsWorking[i - 1].IsHeader )
                                    continue; //if the next one is a header, and we are a header, and the last one was a header, skip me
                            }
                        }
                        else
                            lastWasHeader = true;

                        if ( eventLogCollection.ExcludeHeaderLines || !EventLog_FilterText.IsEmpty() )
                            continue;
                    }
                    else
                        lastWasHeader = false;

                    sortedEvents.Add( data );
                }

            }

            private void OnUpdate_Content_EventLog()
            {
                #region Event Log Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_EventLog" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "PermanentLogCount" )
                                        .AddRaw( PermanentEvents.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "EphemeralEventCount" )
                                        .AddRaw( EphemeralEvents.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( NoteLog.NoteDisplayData dd in sortedEvents )
                {
                    IGameNote note = dd.Note;
                    IGameNote lastTurnHeader = dd.LastHeader;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( dd.IsHeader )
                    {
                        bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            continue;
                        bDataHeaderPool.ApplySingleItemInRow( row, bounds, false );

                        row.Assign( note, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHovered = element.LastHadMouseWithin;
                                        //ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                        if ( lastTurnHeader != null && lastTurnHeader != note )
                                            lastTurnHeader.HandleNote( GameNoteAction.WriteUltraBriefAddendumToAnotherAction, ExtraData.Buffer, true, null, null, string.Empty, 0, false );

                                        note.HandleNote( GameNoteAction.WriteText, ExtraData.Buffer, true, null, null, string.Empty, 0, false );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        note.HandleNote( GameNoteAction.WriteTooltip, null, false, element, lastTurnHeader, string.Empty, (int)SideClamp.AboveOrBelow, false );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        note.HandleNote( GameNoteAction.LeftClick, null, false, null, null, string.Empty, 0, false );
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                        note.HandleNote( GameNoteAction.RightClick, null, false, null, null, string.Empty, 0, false );
                                    break;
                            }
                        } );
                    }
                    else
                    {

                        bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            continue;
                        bDataFullItemPool.ApplySingleItemInRow( row, bounds, false );

                        row.Assign( note, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isHovered = element.LastHadMouseWithin;
                                        row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                        ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                        if ( lastTurnHeader != null && lastTurnHeader != note )
                                            lastTurnHeader.HandleNote( GameNoteAction.WriteUltraBriefAddendumToAnotherAction, ExtraData.Buffer, true, null, null, string.Empty, 0, false );
                                        else
                                            ExtraData.Buffer.StartSize60().StartColor( ColorTheme.DataBlue ).AddLang( "Turn" ).Space1x().AddRaw( "1" )
                                                .EndColor().EndSize().Position60();

                                        note.HandleNote( GameNoteAction.WriteText, ExtraData.Buffer, true, null, null, string.Empty, 0, false );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        note.HandleNote( GameNoteAction.WriteTooltip, null, false, element, lastTurnHeader, string.Empty, (int)SideClamp.AboveOrBelow, false );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                        note.HandleNote( GameNoteAction.LeftClick, null, false, null, null, string.Empty, 0, false );
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                        note.HandleNote( GameNoteAction.RightClick, null, false, null, null, string.Empty, 0, false );
                                    break;
                            }
                        } );
                    }
                }
            }
            #endregion

            #region OnUpdate_Content_MetaEventLog
            private void GetAndSortMetaEventLog()
            {
                hasDoneFirstMetaEventLogSort = true;
                sortedMetaEvents.Clear();
                for ( int i = SimMetagame.LongTermMetaLog.Count - 1; i >= 0; i-- )
                {
                    IGameNote note = SimMetagame.LongTermMetaLog[i];
                    if ( !MetaEventLog_FilterText.IsEmpty() )
                    {
                        if ( !note.HandleNote( GameNoteAction.GetMatchesSearchString, null, false, null, null, MetaEventLog_FilterText, 0, false ) )
                            continue;
                    }
                    sortedMetaEvents.Add( note );
                }
            }

            private void OnUpdate_Content_MetaEventLog()
            {
                #region Meta Event Log Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_MetaEventLog" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CrossTimelineLogCount" )
                                        .AddRaw( SimMetagame.LongTermMetaLog.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( IGameNote note in sortedMetaEvents )
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

                    row.Assign( note, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    note.HandleNote( GameNoteAction.WriteText, ExtraData.Buffer, true, null, null, string.Empty, 0, false );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    note.HandleNote( GameNoteAction.WriteTooltip, null, false, element, null, string.Empty, (int)SideClamp.AboveOrBelow, false );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( ExtraData.MouseInput.LeftButtonClicked )
                                    note.HandleNote( GameNoteAction.LeftClick, null, false, null, null, string.Empty, 0, false );
                                else if ( ExtraData.MouseInput.RightButtonClicked )
                                    note.HandleNote( GameNoteAction.RightClick, null, false, null, null, string.Empty, 0, false );
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_Unlocks
            private void GetAndSortUnlocks()
            {
                hasDoneFirstInventionSort = true;
                sortedInventions.Clear();
                foreach ( Unlock row in UnlockTable.Instance.Rows )
                {
                    if ( row.DuringGameplay_IsInvented )
                    {
                        if ( !Unlock_FilterText.IsEmpty() )
                        {
                            if ( !row.GetMatchesSearchString( Unlock_FilterText ) )
                                continue;
                        }
                        sortedInventions.Add( row );
                    }
                }

                sortedInventions.Sort( delegate ( Unlock Left, Unlock Right )
                {
                    int val = Right.DuringGame_InventedOnTurn.CompareTo( Left.DuringGame_InventedOnTurn ); //desc
                    if ( val != 0 ) return val;
                    val = Right.DuringGame_InventedAtSecond.CompareTo( Left.DuringGame_InventedAtSecond ); //desc
                    if ( val != 0 ) return val;
                    return Left.GetDisplayName().CompareTo( Right.GetDisplayName() );
                } );
            }

            private void OnUpdate_Content_Unlocks()
            {
                #region Unlock History Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_UnlockHistory" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "Invented" )
                                        .AddRaw( sortedInventions.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "PotentialInventions" )
                                        .AddRaw( UnlockCoordinator.PotentialInventions.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                {
                    bool render = true;
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    runningY -= CONTENT_ROW_GAP_SHORT;

                    bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        render = false;
                    if ( render )
                    {
                        float diff = bounds.yMin - yTopOffset;
                        if ( diff > 0 )
                            bounds.y -= diff;

                        bDataHeaderPool.ApplySingleItemInRow( row, bounds, false );
                        row.Element.RelevantRect.SetAsLastSibling(); //make sure it is always on top

                        row.Assign( "Totals", "UnitTypeAnalysis", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartSize80();
                                        ExtraData.Buffer.AddLang( "Unlock_Header" );
                                        ExtraData.Buffer.Position400();
                                        ExtraData.Buffer.AddLang( "UnlockType_Header" );
                                        ExtraData.Buffer.Position600();
                                        ExtraData.Buffer.AddLang( "UnlockedTurn_Header" );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }

                foreach ( Unlock unlock in sortedInventions )
                {
                    if ( unlockCollection != null && !unlockCollection.TypesDict.ContainsKey( unlock ) )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "Unlock", unlock.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    unlock.RenderUnlockName( ExtraData.Buffer, string.Empty );

                                    ExtraData.Buffer.Position400();
                                    unlock.RenderUnlockPrefix( ExtraData.Buffer, string.Empty );

                                    ExtraData.Buffer.Position600().StartSize60();
                                    ExtraData.Buffer.StartColor( ColorTheme.GetDataBlue( isHovered ) ).AddLangAndAfterLineItemHeader( "InventedOnTurn" );
                                    ExtraData.Buffer.AddRaw( unlock.DuringGame_InventedOnTurn.ToStringWholeBasic() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    unlock.RenderUnlockTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipInstruction.ForExistingObject, TooltipExtraText.None );
                                }
                                break;
                            case UIAction.OnClick:
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_Upgrades
            private void GetAndSortUpgrades()
            {
                hasDoneFirstUpgradeSort = true;
                sortedUpgrades.Clear();
                foreach ( UpgradeFloat upgrade in UpgradeFloatTable.Instance.Rows )
                {
                    if ( upgrade.GetShouldBeDisplayedInUpgradeLists() )
                    {
                        if ( !Upgrade_FilterText.IsEmpty() )
                        {
                            if ( !upgrade.GetMatchesSearchString( Upgrade_FilterText ) )
                                continue;
                        }
                        sortedUpgrades.Add( upgrade );
                    }
                }
                foreach ( UpgradeInt upgrade in UpgradeIntTable.Instance.Rows )
                {
                    if ( upgrade.GetShouldBeDisplayedInUpgradeLists() )
                    {
                        if ( !Upgrade_FilterText.IsEmpty() )
                        {
                            if ( !upgrade.GetMatchesSearchString( Upgrade_FilterText ) )
                                continue;
                        }
                        sortedUpgrades.Add( upgrade );
                    }
                }

                sortedUpgrades.Sort( delegate ( IUpgrade Left, IUpgrade Right )
                {
                    int val = Left.GetPriorityGroup().CompareTo( Right.GetPriorityGroup() ); //asc
                    if ( val != 0 ) return val;
                    return Left.GetDisplayName().CompareTo( Right.GetDisplayName() );
                } );
            }

            private void OnUpdate_Content_Upgrades()
            {
                #region Upgrade Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_Upgrades" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "UpgradesSoFar" )
                                        .AddRaw( UnlockCoordinator.CompletedUpgrades.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "PotentialUpgrades" )
                                        .AddRaw( UnlockCoordinator.PotentialUpgrades.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "UpgradeTypes" )
                                        .AddRaw( (UpgradeFloatTable.Instance.Rows.Length +
                                        UpgradeIntTable.Instance.Rows.Length).ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( IUpgrade upgrade in sortedUpgrades )
                {
                    if ( upgradeCollection != null && !upgradeCollection.TypesDict.ContainsKey( upgrade ) )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( upgrade.GetTypeCode(), upgrade.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    upgrade.RenderInventoryLine( ExtraData.Buffer );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    upgrade.RenderUpgradeTooltip_General( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                                }
                                break;
                            case UIAction.OnClick:
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_NamedThings
            private void OnUpdate_Content_NamedThings()
            {
                #region NamedThings Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_NamedThings" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLang( "ActivityOverview_NamedThings_Instructions" );
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

                foreach ( NamedThing namedThing in NamedThingTable.Instance.Rows )
                {
                    if ( !namedThing.GetIsVisible() )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "NamedThing", namedThing.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddBoldRawAndAfterLineItemHeader( namedThing.GetDisplayName() ).AddRaw( namedThing.DuringGame_ChosenName );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NamedThing", namedThing.ID ), element,
                                    SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddBoldRawAndAfterLineItemHeader( namedThing.GetDisplayName() ).AddRaw( namedThing.DuringGame_ChosenName );

                                    if ( !namedThing.GetDescription().IsEmpty() )
                                        novel.Main.AddRaw( namedThing.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                    if ( !namedThing.StrategyTip.Text.IsEmpty() )
                                        novel.Main.AddRaw( namedThing.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                    novel.Main.AddLang( "ActivityOverview_NamedThings_Instructions", ColorTheme.SoftGold ).Line();
                                }

                                break;
                            case UIAction.OnClick:
                                if (namedThing.RandomNamePool != null )
                                {
                                    Window_ModalTextboxWindowWithRandomizer.Instance.Open( namedThing.GetDisplayName(), Lang.Get( "NewName" ), false, namedThing.DuringGame_ChosenName, 60,
                                        namedThing );
                                }
                                else
                                {
                                    Window_ModalTextboxWindow.Instance.Open( namedThing.GetDisplayName(), Lang.Get( "NewName" ), false, namedThing.DuringGame_ChosenName, 60,
                                        delegate ( string NewName )
                                        {
                                            namedThing.SetChosenName( NewName, true );
                                            return true;
                                        } );
                                }

                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_KeyContacts
            private void OnUpdate_Content_KeyContacts()
            {
                #region Key Contact Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_KeyContacts" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "KeyContactsMet" )
                                        .AddRaw( SimCommon.ActiveKeyContacts.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( KeyContact keyContact in SimCommon.ActiveKeyContacts.GetDisplayList() )
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

                    row.Assign( "KeyContact", keyContact.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    keyContact.AppendDisplayNameWithPrefixAndColors( ExtraData.Buffer, ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin ), string.Empty );

                                    if ( keyContact.DuringGame_IsDead )
                                    {
                                        ExtraData.Buffer.StartSize60();
                                        ExtraData.Buffer.Space3x();
                                        ExtraData.Buffer.AddFormat1( "DiedOnTurn_Brief", keyContact.DuringGame_KilledOnTurn, ColorTheme.RedOrange2 );
                                        ExtraData.Buffer.StartSize70();
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    keyContact.RenderKeyContactTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard );
                                }
                                break;
                            case UIAction.OnClick:
                                keyContact.OpenPopupShowingThisKeyContact();
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_Contemplations
            private void OnUpdate_Content_Contemplations()
            {
                #region Contemplation Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_Contemplations" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CurrentContemplationsAvailable" )
                                        .AddRaw( SimCommon.ContemplationsAvailable.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( KeyValuePair<ISimBuilding, ContemplationType> kv in SimCommon.ContemplationsAvailable.GetDisplayList() )
                {
                    ContemplationType contemplation = kv.Value;
                    ISimBuilding building = kv.Key;

                    //filtering
                    if ( contemplationCollection != null )
                    {
                        if ( !contemplation.Collections.ContainsKey( contemplationCollection.ID ) )
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

                    row.Assign( "ContemplationType", contemplation.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( contemplation.Icon, AdjustedSpriteStyle.InlineLarger1_2, contemplation.ColorHex ).Space1x();
                                    ExtraData.Buffer.AddRaw( contemplation.GetDisplayName() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    contemplation.RenderContemplationTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, false );
                                }
                                break;
                            case UIAction.OnClick:
                                //take the mouse cursor to this contemplation in the map
                                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetMapItem().CenterPoint, false );
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                                SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ContemplationsLens );
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_StreetSense
            private void OnUpdate_Content_StreetSense()
            {
                #region StreetSense Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_StreetSense" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CurrentStreetSenseActivitiesDetected" )
                                        .AddRaw( SimCommon.CurrentStreetSenseBuildings.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CurrentStreetSenseActivityTypesDetected" )
                                        .AddRaw( SimCommon.CurrentStreetSenseTitleBuildings.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                List<KeyValuePair<string, ISimBuilding>> sortedList = SimCommon.CurrentStreetSenseTitleBuildings.GetDisplayDict().GetLastSortedResult();

                foreach ( KeyValuePair<string, ISimBuilding> kv in sortedList )
                {
                    ISimBuilding building = kv.Value;
                    string titleText = kv.Key;
                    IA5Sprite icon = building?.GetCurrentStreetSenseActionThatShouldShowOnFilteredList( streetSenseCollection )?.Icon;

                    //filtering was already done
                    if ( icon == null )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( "StreetSense", titleText, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();

                                    int count = SimCommon.CurrentStreetSenseTitleCounts.GetDisplayDict()[titleText];
                                    if ( count > 1 )
                                        ExtraData.Buffer.StartSize70().AddFormat1( "Multiplier", count, ColorTheme.DataBlue ).EndSize();

                                    ExtraData.Buffer.Position50();
                                    ExtraData.Buffer.AddRaw( titleText );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    StreetSenseDataAtBuilding streetSense = building?.GetCurrentStreetSenseActionThatShouldShowOnFilteredList( null ); //don't bother filtering
                                    LocationActionType actionToTake = streetSense?.ActionType;
                                    if ( actionToTake != null )
                                    {
                                        NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                        if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( actionToTake ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                        {
                                            InputCaching.ShouldShowDetailedTooltips = true;

                                            atMouse.ShadowStyle = TooltipShadowStyle.Standard;
                                            atMouse.Icon = actionToTake.Icon;
                                            atMouse.TitleUpperLeft.AddRaw( actionToTake.GetDisplayName() );

                                            bool hasDescription = !actionToTake.GetDescription().IsEmpty();
                                            if ( hasDescription )
                                                atMouse.CanExpand = CanExpandType.Brief;

                                            if ( hasDescription )
                                            {
                                                atMouse.FrameTitle.AddLang( "Move_ActionDetails" );

                                                if ( !actionToTake.GetDescription().IsEmpty() )
                                                    atMouse.FrameBody.AddRaw( actionToTake.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                                if ( !actionToTake.StrategyTipOptional.Text.IsEmpty() )
                                                    atMouse.FrameBody.AddRaw( actionToTake.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                            }

                                            bool wasDetailed = InputCaching.ShouldShowDetailedTooltips;

                                            actionToTake.Implementation.TryHandleLocationAction( null, building, Vector3.zero, actionToTake, streetSense.EventOrNull,
                                                streetSense.ProjectItemOrNull, streetSense.OtherOptionalID, ActionLogic.AppendToTooltip, out _, 0 );

                                            InputCaching.ShouldShowDetailedTooltips = wasDetailed;
                                            atMouse.ShouldTooltipBeRed = false;
                                        }
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                //take the mouse cursor to this building in the map
                                if ( building != null )
                                {
                                    VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetMapItem().CenterPoint, false );
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                    SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.StreetSenseLens );
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_ExplorationSites
            private void OnUpdate_Content_ExplorationSites()
            {
                #region When Not Yet Unlocked
                if ( !CommonRefs.ExplorationSitesLens.GetIsLensVisible () )
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_ExplorationSites" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ExplorationSiteSpot_NotYetReady", ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    return;
                }
                #endregion

                #region ExplorationSite Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_ExplorationSites" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CurrentExplorationSitesAvailable" )
                                        .AddRaw( SimCommon.ExplorationSitesAvailable.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( KeyValuePair<ISimBuilding, ExplorationSiteType> kv in SimCommon.ExplorationSitesAvailable.GetDisplayList() )
                {
                    ExplorationSiteType explorationSite = kv.Value;
                    ISimBuilding building = kv.Key;

                    //filtering
                    if ( explorationSiteCollection != null )
                    {
                        if ( !explorationSite.Collections.ContainsKey( explorationSiteCollection.ID ) )
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

                    row.Assign( "ExplorationSiteType", explorationSite.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( explorationSite.Icon, AdjustedSpriteStyle.InlineLarger1_2, explorationSite.ColorHex ).Space1x();
                                    ExtraData.Buffer.AddRaw( explorationSite.GetDisplayName() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    explorationSite.RenderExplorationSiteTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, false );
                                }
                                break;
                            case UIAction.OnClick:
                                //take the mouse cursor to this exploration site in the map
                                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetMapItem().CenterPoint, false );
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                                SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ExplorationSitesLens );
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region OnUpdate_Content_CityStatistics
            private void GetAndSortCityStatistics()
            {
                hasDoneFirstCityStatisticsSort = true;

                bool shouldShowDebugStatistics = GameSettings.Current.GetBool( "Debug_ShowDebugStatistics" );

                sortedCitywideStatistics.Clear();
                sortedCityActionStatistics.Clear();
                foreach ( CityStatistic row in CityStatisticTable.Instance.Rows )
                {
                    if ( row.GetScore() > 0 )
                    {
                        if ( ( row.ShouldBeInvisibleToPlayers || row.ShouldBeSkippedOnLists ) && !shouldShowDebugStatistics )
                            continue;
                        if ( row.ShouldBeInCitywideCategory )
                            sortedCitywideStatistics.Add( row );
                        else
                            sortedCityActionStatistics.Add( row );
                    }
                }
                foreach ( ActorDataType row in ActorDataTypeTable.Instance.Rows )
                {
                    if ( row.DuringGameplay_SkillCheckSuccesses.GetScore() > 0 )
                        sortedCityActionStatistics.Add( row.DuringGameplay_SkillCheckSuccesses );
                    if ( row.DuringGameplay_SkillCheckFailures.GetScore() > 0 )
                        sortedCityActionStatistics.Add( row.DuringGameplay_SkillCheckFailures );
                }
                foreach ( AbilityType row in AbilityTypeTable.Instance.Rows )
                {
                    if ( row.GetScore() > 0 && !row.GetDisplayName().IsEmpty() )
                        sortedCityActionStatistics.Add( row );
                }
                foreach ( LocationActionType row in LocationActionTypeTable.Instance.Rows )
                {
                    if ( row.SkipNormalActionTooltip )
                        continue;
                    if ( row.GetScore() > 0 && !row.GetDisplayName().IsEmpty() )
                        sortedCityActionStatistics.Add( row );
                }
                foreach ( ResourceConsumable row in ResourceConsumableTable.Instance.Rows )
                {
                    if ( row.GetScore() > 0 )
                        sortedCityActionStatistics.Add( row );
                }
            }
            
            private void OnUpdate_Content_CityStatistics( List<IStatisticSource> listToUse, bool SortAlphabetically, bool IsCityWide )
            {
                GetAndSortCityStatistics();
                if ( listToUse.Count <= 0 )
                    return;

                if ( SortAlphabetically )
                {
                    listToUse.Sort( delegate ( IStatisticSource Left, IStatisticSource Right )
                    {
                        int val = Left.GetEffectiveSortName().CompareTo( Right.GetEffectiveSortName() ); //asc
                        if ( val != 0 ) return val;
                        return Right.GetScore().CompareTo( Left.GetScore() ); //desc
                    } );
                }
                else
                {
                    listToUse.Sort( delegate ( IStatisticSource Left, IStatisticSource Right )
                    {
                        int val = Right.GetScore().CompareTo( Left.GetScore() ); //desc
                        if ( val != 0 ) return val;
                        return Left.GetEffectiveSortName().CompareTo( Right.GetEffectiveSortName() ); //asc
                    } );
                }

                bool hasFilterChanged;
                string filterText = string.Empty;
                if ( IsCityWide )
                {
                    hasFilterChanged = CityWideStatistics_FilterText == CityWideStatistics_LastFilterText && CityWideStatistics_lastTurn == SimCommon.Turn;
                    CityWideStatistics_LastFilterText = CityWideStatistics_FilterText;
                    CityWideStatistics_lastTurn = SimCommon.Turn;
                    filterText = CityWideStatistics_FilterText;
                }
                else
                {
                    hasFilterChanged = ActionStatistics_FilterText == ActionStatistics_LastFilterText && ActionStatistics_lastTurn == SimCommon.Turn;
                    ActionStatistics_LastFilterText = ActionStatistics_FilterText;
                    ActionStatistics_lastTurn = SimCommon.Turn;
                    filterText = ActionStatistics_FilterText;
                }

                #region City Statistics Totals
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
                                    ExtraData.Buffer.AddLang( IsCityWide ? "ActivityOverview_CityStatistics" : "ActivityOverview_ActionStatistics" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "ActivityOverview_CityStatistics" )
                                        .AddRaw( sortedCitywideStatistics.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "ActivityOverview_ActionStatistics" )
                                        .AddRaw( sortedCityActionStatistics.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( IStatisticSource stat in listToUse )
                {
                    if ( hasFilterChanged )
                    {
                        if ( filterText.IsEmpty() )
                            stat.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            stat.DuringGame_HasBeenFilteredOutInInventory = !stat.GetMatchesSearchString( filterText );
                    }
                    if ( stat.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( stat.GetTypeCode(), stat.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
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
                                        SideClamp.AboveOrBelow, desc.IsEmpty() ? TooltipNovelWidth.SizeToText : TooltipNovelWidth.Smaller ) )
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
                }
            }
            #endregion

            #region OnUpdate_Content_MetaStatistics
            private void GetAndSortMetaStatistics()
            {
                hasDoneFirstMetaStatisticsSort = true;

                bool shouldShowDebugStatistics = GameSettings.Current.GetBool( "Debug_ShowDebugStatistics" );

                sortedMetaStatistics.Clear();
                foreach ( MetaStatistic row in MetaStatisticTable.Instance.Rows )
                {
                    if ( row.GetScore() > 0 )
                    {
                        if ( row.ShouldBeInvisibleToPlayers && !shouldShowDebugStatistics )
                            continue;
                        sortedMetaStatistics.Add( row );
                    }
                }
            }

            private void OnUpdate_Content_MetaStatistics()
            {
                GetAndSortMetaStatistics();
                if ( sortedMetaStatistics.Count <= 0 )
                    return;

                sortedMetaStatistics.Sort( delegate ( IStatisticSource Left, IStatisticSource Right )
                {
                    int val = Right.GetScore().CompareTo( Left.GetScore() ); //desc
                    if ( val != 0 ) return val;
                    return Left.GetEffectiveSortName().CompareTo( Right.GetEffectiveSortName() );
                } );

                bool hasFilterChanged = !( MetaStatistics_FilterText == MetaStatistics_LastFilterText && MetaStatistics_lastTurn == SimCommon.Turn );
                string filterText = MetaStatistics_FilterText;
                MetaStatistics_LastFilterText = MetaStatistics_FilterText;
                MetaStatistics_lastTurn = SimCommon.Turn;

                #region Meta Statistics Totals
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
                                    ExtraData.Buffer.AddLang( "CrossTimelineStatistics" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "StatisticsEntriesCount" )
                                        .AddRaw( sortedMetaStatistics.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( IStatisticSource stat in sortedMetaStatistics )
                {
                    if ( hasFilterChanged )
                    {
                        if ( filterText.IsEmpty() )
                            stat.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            stat.DuringGame_HasBeenFilteredOutInInventory = !stat.GetMatchesSearchString( filterText );
                    }
                    if ( stat.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    row.Assign( stat.GetTypeCode(), stat.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
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
                                        SideClamp.AboveOrBelow, desc.IsEmpty() ? TooltipNovelWidth.SizeToText : TooltipNovelWidth.Smaller ) )
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
                }
            }
            #endregion

            #region OnUpdate_Content_ProjectHistory
            private void OnUpdate_Content_ProjectHistory()
            {
                #region Project History Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_ProjectHistory" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CompletedProjects" )
                                        .AddRaw( SimCommon.CompletedProjects.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "FailedProjects" )
                                        .AddRaw( SimCommon.FailedProjects.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                bool hasFilterChanged = !( ProjectHistory_FilterText == ProjectHistory_LastFilterText && ProjectHistory_lastTurn == SimCommon.Turn );
                ProjectHistory_LastFilterText = ProjectHistory_FilterText;
                ProjectHistory_lastTurn = SimCommon.Turn;

                foreach ( MachineProject project in SimCommon.CompletedProjects.GetDisplayList() )
                {
                    if ( projectCollection != null && !projectCollection.TypesDict.ContainsKey( project ) )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( ProjectHistory_FilterText.IsEmpty() )
                            project.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            project.DuringGame_HasBeenFilteredOutInInventory = !project.GetMatchesSearchString( ProjectHistory_FilterText );
                    }
                    if ( project.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    HandleSpecificCompletedProject( row, project, false );
                }

                foreach ( MachineProject project in SimCommon.FailedProjects.GetDisplayList() )
                {
                    if ( projectCollection != null && !projectCollection.TypesDict.ContainsKey( project ) )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( ProjectHistory_FilterText.IsEmpty() )
                            project.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            project.DuringGame_HasBeenFilteredOutInInventory = !project.GetMatchesSearchString( ProjectHistory_FilterText );
                    }
                    if ( project.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                    HandleSpecificCompletedProject( row, project, true );
                }
            }

            private void HandleSpecificCompletedProject( bDataFullItem row, MachineProject project, bool Failed )
            {
                row.Assign( "MachineProject", project.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                ExtraData.Buffer.AddSpriteStyled_NoIndent( project.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x();
                                ExtraData.Buffer.StartSize80().AddRaw( project.GetDisplayName() ).EndSize();

                                //ExtraData.Buffer.Position250();
                                //ExtraData.Buffer.StartSize80();
                                //unlock.RenderUnlockNameAndPrefixOnly( ExtraData.Buffer, ColorTheme.HeaderGoldDim, ColorTheme.HeaderGold );
                                //ExtraData.Buffer.EndSize();

                                ExtraData.Buffer.Position500();
                                ExtraData.Buffer.StartSize60().StartColor( Failed ? ColorTheme.RedOrange2 : ColorTheme.GetDataBlue( isHovered ) )
                                    .AddLangAndAfterLineItemHeader( Failed ? "FailedProject_Suffix" : "CompletedProject_Suffix" ).
                                    AddFormat1( "TurnNumber", project.DuringGameplay_TurnEnded.ToString() );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                project.RenderProjectTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, false, false, false );
                            }
                            break;
                        case UIAction.OnClick:
                            if ( project.IsMinorProject )
                            {
                                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                break;
                            }
                            Instance.Close( WindowCloseReason.UserDirectRequest );
                            SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                            Window_RewardWindow.Instance.Open();
                            break;
                    }
                } );
            }
            #endregion

            private int foundDistricts = 0;
            private int totalDistricts = 0;
            private int foundPOIs = 0;
            private int totalPOIs = 0;

            #region OnUpdate_Content_MajorLocations
            private void GetAndSortMajorLocations()
            {
                hasDoneFirstMajorLocationsSort = true;
                foundDistricts = 0;
                totalDistricts = 0;
                foundPOIs = 0;
                totalPOIs = 0;

                foreach ( MajorLocationAggregationType aggregationType in MajorLocationAggregationTypeTable.Instance.Rows )
                    aggregationType.NonSim_ForUI_EntriesInThisType = 0;

                MajorLocationAggregationType defaultType = MajorLocationAggregationTypeTable.Instance.DefaultRow;
                if ( defaultType == null )
                    defaultType = MajorLocationAggregationTypeTable.Instance.Rows[0];

                if ( MajorLocationCategory == null )
                    MajorLocationCategory = defaultType;

                sortedMajorLocations.Clear();
                foreach ( MapDistrict district in CityMap.AllDistricts )
                {
                    totalDistricts++;
                    if ( !district.HasBeenDiscovered )
                        continue;
                    foundDistricts++;

                    bool found = false;
                    List<MajorLocationAggregationType> aggregationTypes = district.GetParentAggregationTypeList();
                    foreach ( MajorLocationAggregationType aggregationType in aggregationTypes )
                    {
                        aggregationType.NonSim_ForUI_EntriesInThisType++;
                        if ( aggregationType == MajorLocationCategory )
                            found = true;
                    }

                    if ( !found )
                        continue;

                    if ( !MajorLocations_FilterText.IsEmpty() )
                    {
                        if ( !district.GetMatchesSearchString( MajorLocations_FilterText ) )
                            continue;
                    }
                    sortedMajorLocations.Add( district );

                }
                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                {
                    MapPOI poi = kv.Value;
                    totalPOIs++;
                    if ( !poi.HasBeenDiscovered )
                        continue;
                    foundPOIs++;

                    bool found = false;
                    List<MajorLocationAggregationType> aggregationTypes = poi.GetParentAggregationTypeList();
                    foreach ( MajorLocationAggregationType aggregationType in aggregationTypes )
                    {
                        aggregationType.NonSim_ForUI_EntriesInThisType++;
                        if ( aggregationType == MajorLocationCategory )
                            found = true;
                    }

                    if ( !found )
                        continue;

                    if ( !MajorLocations_FilterText.IsEmpty() )
                    {
                        if ( !poi.GetMatchesSearchString( MajorLocations_FilterText ) )
                            continue;
                    }
                    sortedMajorLocations.Add( poi );
                }

                sortedMajorLocations.Sort( delegate ( IAggregatedMajorLocationDimension Left, IAggregatedMajorLocationDimension Right )
                {
                    int val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                    if ( val != 0 ) return val;
                    return Left.GetID().CompareTo( Right.GetID() );
                } );


                majorLocationCategories.Clear();
                foreach ( MajorLocationAggregationType aggregationType in MajorLocationAggregationTypeTable.Instance.Rows )
                {
                    if ( aggregationType.NonSim_ForUI_EntriesInThisType > 0 || aggregationType == defaultType )
                        majorLocationCategories.Add( aggregationType );
                }
            }

            private void OnUpdate_Content_MajorLocations()
            {
                if ( sortedMajorLocations.Count <= 0 )
                    return;

                #region Major Location Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_MajorLocations" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DistrictsCount" )
                                        .AddFormat2( "OutOF", foundDistricts, totalDistricts, ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "POIsCount" )
                                        .AddFormat2( "OutOF", foundPOIs, totalPOIs, ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( IAggregatedMajorLocationDimension location in sortedMajorLocations )
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

                    row.Assign( location.GetTypeCode(), location.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    //bool isHovered = element.LastHadMouseWithin;
                                    //row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    //ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    if ( location is MapDistrict District )
                                    {
                                        bool isSelected = CameraCurrent.FocusDistrictOrNull == District;
                                        row.SetRelatedImage0EnabledIfNeeded( isSelected );
                                        ExtraData.Buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( row.Element.LastHadMouseWithin ) :
                                            ColorTheme.GetBasicLightTextBlue( row.Element.LastHadMouseWithin ) );

                                        ExtraData.Buffer.StartSize80();

                                        ExtraData.Buffer.AddRaw( District.GetDisplayName() );
                                        ExtraData.Buffer.Position200();
                                        if ( District.ControlledBy != null )
                                            ExtraData.Buffer.AddRaw( District.ControlledBy.GetDisplayName() );

                                        ExtraData.Buffer.SetPositionOffset( 450 );
                                        ExtraData.Buffer.AddRaw( District.Type.GetDisplayName() );
                                    }
                                    else if ( location is MapPOI POI )
                                    {
                                        MapDistrict mapDistrict = POI.Tile.District;
                                        bool copyNameFromDistrict = POI.Type.CopyNameFromDistrict;

                                        bool isSelected = false;// CameraCurrent.FocusDistrictOrNull == mapDistrict;
                                        row.SetRelatedImage0EnabledIfNeeded( isSelected );
                                        ExtraData.Buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( row.Element.LastHadMouseWithin ) :
                                            ColorTheme.GetBasicLightTextBlue( row.Element.LastHadMouseWithin ) );

                                        ExtraData.Buffer.StartSize70();
                                        ExtraData.Buffer.AddRaw( POI.GetDisplayName() );

                                        ExtraData.Buffer.Position200();
                                        if ( POI.ControlledBy != null )
                                            ExtraData.Buffer.AddRaw( POI.ControlledBy.GetDisplayName() );

                                        ExtraData.Buffer.SetPositionOffset( 450 );
                                        ExtraData.Buffer.AddRaw( POI.Type.GetDisplayName() );

                                        if ( !InputCaching.Debug_ShowPOIGuardStats )
                                        {
                                            if ( !copyNameFromDistrict )
                                            {
                                                ExtraData.Buffer.SetPositionOffset( 650 );
                                                ExtraData.Buffer.AddRaw( mapDistrict.DistrictName );
                                            }
                                        }
                                        else
                                        {
                                            ExtraData.Buffer.SetPositionOffset( 650 );
                                            if ( POI.HasEverCountedGuardTags && POI.NormalMaxGuardCount > 0 )
                                            {
                                                if ( POI.CurrentlyShortThisManyGuards > 0 )
                                                {
                                                    int onHand = POI.NormalMinGuardCount - POI.CurrentlyShortThisManyGuards;
                                                    ExtraData.Buffer.AddRaw( POI.NormalMinGuardCount <= 0 ? "INFINITY???" : Mathf.RoundToInt(
                                                        ((float)onHand / (float)POI.NormalMinGuardCount) * 100 ).ToStringIntPercent(), ColorTheme.RedOrange2 );
                                                }
                                                else
                                                    ExtraData.Buffer.AddRaw( "-  ", isSelected ? ColorTheme.GetInvertibleListTextBlue_SecondarySelected( row.Element.LastHadMouseWithin ) :
                                                        ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin ) );

                                                ExtraData.Buffer.Space2x();

                                                if ( POI.CouldHaveThisManyMoreGuardsAtMost > 0 )
                                                {
                                                    int onHand = POI.NormalMaxGuardCount - POI.CouldHaveThisManyMoreGuardsAtMost;
                                                    ExtraData.Buffer.AddRaw( POI.NormalMaxGuardCount <= 0 ? "INFINITY???" : Mathf.RoundToInt(
                                                        ((float)onHand / (float)POI.NormalMaxGuardCount) * 100 ).ToStringIntPercent(), ColorTheme.CategorySelectedBlue );
                                                }
                                                else
                                                    ExtraData.Buffer.AddRaw( "-  ", isSelected ? ColorTheme.GetInvertibleListTextBlue_SecondarySelected( row.Element.LastHadMouseWithin ) :
                                                        ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin ) );

                                                ExtraData.Buffer.Space2x();

                                                if ( POI.IsBlockedFromBeingTargetedForReinforcementsForXMoreTurns > 0 )
                                                {
                                                    ExtraData.Buffer.AddRaw( POI.IsBlockedFromBeingTargetedForReinforcementsForXMoreTurns.ToString(), ColorTheme.RedLess );
                                                }
                                                else
                                                    ExtraData.Buffer.AddRaw( "-  ", isSelected ? ColorTheme.GetInvertibleListTextBlue_SecondarySelected( row.Element.LastHadMouseWithin ) :
                                                        ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin ) );
                                            }
                                        }
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    location.WriteDataItemUIXTooltip( element, SideClamp.LeftOrRight );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.LeftButtonDoubleClicked )
                                {
                                    if ( location.DataItemUIX_TryHandlePrimaryClick( out bool shouldCloseWindow ) )
                                    {
                                        if ( shouldCloseWindow )
                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                    }
                                    else
                                        VisCommands.ShowInformationAboutUIXExaminedDataItem( location );
                                }
                                else
                                {
                                    //ArcenDebugging.LogSingleLine( "call handle alt on" + this.DataItem.GetType(), Verbosity.DoNotShow );
                                    location.DataItemUIX_HandleAltClick( out bool shouldCloseWindow );
                                    if ( shouldCloseWindow )
                                        Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            private int foundCohorts = 0;
            private int totalCohorts = 0;

            #region OnUpdate_Content_Cohorts
            private void GetAndSortCohorts()
            {
                hasDoneFirstCohortsSort = true;
                foundCohorts = 0;
                totalCohorts = 0;

                NPCCohortCategory allCategory = NPCCohortCategoryTable.Instance.GetRowByID( "All" );

                foreach ( NPCCohortCategory cohortCategory in NPCCohortCategoryTable.Instance.Rows )
                    cohortCategory.NonSim_ForUI_CohortsInThisCategory = 0;

                if ( CohortCategory == null )
                    CohortCategory = allCategory;

                sortedCohorts.Clear();
                foreach ( NPCCohort cohort in NPCCohortTable.Instance.Rows )
                {
                    totalCohorts++;
                    if ( !cohort.DuringGame_HasBeenDiscovered )
                        continue;
                    foundCohorts++;

                    bool found = false;
                    {
                        cohort.PartOfGroup.ParentCategory.NonSim_ForUI_CohortsInThisCategory++;
                        if ( cohort.PartOfGroup.ParentCategory == CohortCategory )
                            found = true;
                    }
                    {
                        allCategory.NonSim_ForUI_CohortsInThisCategory++;
                        if ( allCategory == CohortCategory )
                            found = true;
                    }

                    if ( !found )
                        continue;

                    if ( !Cohorts_FilterText.IsEmpty() )
                    {
                        if ( !cohort.GetMatchesSearchString( Cohorts_FilterText ) )
                            continue;
                    }
                    sortedCohorts.Add( cohort );

                }

                sortedCohorts.Sort( delegate ( NPCCohort Left, NPCCohort Right )
                {
                    int val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                    if ( val != 0 ) return val;
                    return Left.ID.CompareTo( Right.ID );
                } );


                cohortCategories.Clear();
                foreach ( NPCCohortCategory cohortCategory in NPCCohortCategoryTable.Instance.Rows )
                {
                    if ( cohortCategory.NonSim_ForUI_CohortsInThisCategory > 0 || cohortCategory == allCategory )
                        cohortCategories.Add( cohortCategory );
                }
            }

            private void OnUpdate_Content_Cohorts()
            {
                if ( sortedCohorts.Count <= 0 )
                    return;

                #region Cohort Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_Cohorts" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CohortsCount" )
                                        .AddFormat2( "OutOF", foundCohorts, totalCohorts, ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( NPCCohort cohort in sortedCohorts )
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

                    row.Assign( cohort.ID, cohort.PartOfGroup.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    //bool isHovered = element.LastHadMouseWithin;
                                    //row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    //ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                    bool isSelected = false;// Instance.SelectedFile == this.Save;
                                    row.SetRelatedImage0EnabledIfNeeded( isSelected );
                                    ExtraData.Buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( row.Element.LastHadMouseWithin ) :
                                        ColorTheme.GetBasicLightTextBlue( row.Element.LastHadMouseWithin ) );

                                    ExtraData.Buffer.StartSize70();
                                    cohort.AppendDisplayNameWithPrefixAndColors( ExtraData.Buffer, isSelected ? ColorTheme.GetInvertibleListTextBlue_SecondarySelected( row.Element.LastHadMouseWithin ) :
                                        ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin ), string.Empty );
                                    if ( cohort.DuringGame_HasBeenDisbanded )
                                    {
                                        ExtraData.Buffer.StartSize60();
                                        ExtraData.Buffer.Space3x();
                                        ExtraData.Buffer.AddLang( "CohortHasBeenDisbanded_Brief", ColorTheme.RedOrange2 );
                                        ExtraData.Buffer.StartSize70();
                                    }
                                    ExtraData.Buffer.Position300();
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    cohort.WriteDataItemUIXTooltip( element, SideClamp.LeftOrRight );
                                }
                                break;
                            case UIAction.OnClick:
                                if ( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.LeftButtonDoubleClicked )
                                {
                                    if ( cohort.DataItemUIX_TryHandlePrimaryClick( out bool shouldCloseWindow ) )
                                    {
                                        if ( shouldCloseWindow )
                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                    }
                                    else
                                        VisCommands.ShowInformationAboutUIXExaminedDataItem( cohort );
                                }
                                else
                                {
                                    //ArcenDebugging.LogSingleLine( "call handle alt on" + this.DataItem.GetType(), Verbosity.DoNotShow );
                                    cohort.DataItemUIX_HandleAltClick( out bool shouldCloseWindow );
                                    if ( shouldCloseWindow )
                                        Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            private int activeCityConflicts = 0;
            private int completeCityConflicts = 0;

            #region OnUpdate_Content_CityConflicts
            private void GetAndSortCityConflicts()
            {
                hasDoneFirstCityConflictSort = true;
                activeCityConflicts = 0;
                completeCityConflicts = 0;

                sortedCityConflicts_Active.Clear();
                sortedCityConflicts_Complete.Clear();
                foreach ( CityConflict conflict in CityConflictTable.Instance.Rows )
                {
                    CityConflictState status = conflict.DuringGameplay_State;
                    if ( status == CityConflictState.NeverStarted )
                        continue; //don't show, since they never started

                    if ( status == CityConflictState.Active )
                        activeCityConflicts++;
                    else
                        completeCityConflicts++;

                    if ( !CityConflicts_FilterText.IsEmpty() )
                    {
                        if ( !conflict.GetMatchesSearchString( CityConflicts_FilterText ) )
                            continue;
                    }
                    if ( status == CityConflictState.Active)
                        sortedCityConflicts_Active.Add( conflict );
                    else
                        sortedCityConflicts_Complete.Add( conflict );

                }

                sortedCityConflicts_Active.Sort( delegate ( CityConflict Left, CityConflict Right )
                {
                    int val = Right.DuringGame_TurnStarted.CompareTo( Left.DuringGame_TurnStarted ); //desc
                    if ( val != 0 ) return val;

                    val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                    if ( val != 0 ) return val;
                    return Left.ID.CompareTo( Right.ID );
                } );

                sortedCityConflicts_Complete.Sort( delegate ( CityConflict Left, CityConflict Right )
                {
                    int val = Right.DuringGame_TurnEnded.CompareTo( Left.DuringGame_TurnEnded ); //desc
                    if ( val != 0 ) return val;

                    val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                    if ( val != 0 ) return val;
                    return Left.ID.CompareTo( Right.ID );
                } );
            }

            private void OnUpdate_Content_CityConflicts()
            {
                if ( sortedCityConflicts_Active.Count <= 0 && sortedCityConflicts_Complete.Count <= 0 )
                    return;

                #region Cohort Totals
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
                                    ExtraData.Buffer.AddLang( "ActivityOverview_CityConflicts" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CityConflicts_Active" )
                                        .AddRaw( activeCityConflicts.ToString(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) )
                                        .Space8x();

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CityConflicts_Ended" )
                                        .AddRaw( completeCityConflicts.ToString(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                foreach ( CityConflict conflict in sortedCityConflicts_Active )
                    RenderSpecificConflict( conflict );

                foreach ( CityConflict conflict in sortedCityConflicts_Complete )
                    RenderSpecificConflict( conflict );
            }

            private void RenderSpecificConflict( CityConflict conflict )
            {
                this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                if ( bounds.yMax < minYToShow )
                    return; //it's scrolled up far enough we can skip it, yay!
                if ( bounds.yMax > maxYToShow )
                    return; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return;
                bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                row.Assign( "CityConflict", conflict.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                //row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                //ExtraData.Buffer.StartSize80().StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartColor( isHovered ? ColorTheme.GetInvertibleListTextBlue_Selected( row.Element.LastHadMouseWithin ) :
                                    ColorTheme.GetBasicLightTextBlue( row.Element.LastHadMouseWithin ) );

                                string colorHex = isHovered ? ColorTheme.GetInvertibleListTextBlue_SecondarySelected( row.Element.LastHadMouseWithin ) :
                                    ColorTheme.GetInvertibleListTextBlue_Normal( row.Element.LastHadMouseWithin );

                                ExtraData.Buffer.StartSize70().StartColor( colorHex );
                                ExtraData.Buffer.AddRaw( conflict.GetDisplayName() );
                                ExtraData.Buffer.Position300();

                                switch ( conflict.DuringGameplay_State )
                                {
                                    case CityConflictState.Active:
                                        {
                                            {
                                                int score = conflict.DuringGame_AggressorScore;
                                                int target = conflict.PointsForVictory;
                                                int remain = target - score;
                                                int minTurns = Mathf.CeilToInt( (float)remain / (float)(conflict.AggressorPointsPerTurn.Max <= 0 ? 0.1f : conflict.AggressorPointsPerTurn.Max) );
                                                int maxTurns = Mathf.CeilToInt( (float)remain / (float)(conflict.AggressorPointsPerTurn.Min <= 0 ? 0.1f : conflict.AggressorPointsPerTurn.Min) );

                                                ExtraData.Buffer.AddFormat3( "CityConflict_ShorterProgressStyle", 
                                                    MathA.IntPercentage( score, target ).ToStringIntPercent(), minTurns, maxTurns );
                                            }

                                            ExtraData.Buffer.Space8x();

                                            {
                                                int score = conflict.DuringGame_DefenderScore;
                                                int target = conflict.PointsForVictory;
                                                int remain = target - score;
                                                int minTurns = Mathf.CeilToInt( (float)remain / (float)(conflict.DefenderPointsPerTurn.Max <= 0 ? 0.1f : conflict.DefenderPointsPerTurn.Max) );
                                                int maxTurns = Mathf.CeilToInt( (float)remain / (float)(conflict.DefenderPointsPerTurn.Min <= 0 ? 0.1f : conflict.DefenderPointsPerTurn.Min) );

                                                ExtraData.Buffer.AddFormat3( "CityConflict_ShorterProgressStyle",
                                                    MathA.IntPercentage( score, target ).ToStringIntPercent(), minTurns, maxTurns );
                                            }

                                            ExtraData.Buffer.Space8x();
                                            if ( conflict.DuringGame_WillBeOpenFightingUntilTurn >= SimCommon.Turn )
                                                ExtraData.Buffer.AddLang( "CityConflict_ConflictStatus_OpenWar_Brief", ColorTheme.DataProblem );
                                            else
                                                ExtraData.Buffer.AddLang( "CityConflict_ConflictStatus_Quiet_Brief", ColorTheme.DataGood );
                                        }
                                        break;
                                    case CityConflictState.WonByAggressor:
                                    case CityConflictState.WonByDefender:
                                    case CityConflictState.Abandoned:
                                        ExtraData.Buffer.AddBoldLangAndAfterLineItemHeader( "CityConflicts_EndedOnTurn" )
                                            .AddRaw( conflict.DuringGame_TurnEnded.ToString() );
                                        break;
                                }
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                conflict.RenderCityConflictTooltip( element, SideClamp.AboveOrBelow, false );
                            }
                            break;
                        case UIAction.OnClick:
                            if ( conflict.DuringGameplay_State == CityConflictState.Active )
                            {
                                foreach ( ISimBuilding building in SimCommon.CityConflictsAvailable.GetDisplayList() )
                                {
                                    if ( building.CurrentCityConflict.Display == conflict )
                                    {
                                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.CityConflictLens );
                                        Instance.Close( WindowCloseReason.UserDirectRequest );
                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( building.GetMapItem().CenterPoint, false );
                                        break;
                                    }
                                }
                            }
                            break;
                    }
                } );
            }
            #endregion
        }
        #endregion

        #region Supporting Elements
        public enum HistoryType
        {
            EventLog,
            Upgrades,
            ProjectHistory,
            UnlockHistory,
            NamedThings,
            KeyContacts,
            MajorLocations,
            Cohorts,
            StreetSense,
            Contemplations,
            ExplorationSites,
            CityConflicts,
            ActionStatistics,
            CityStatistics,
            MetaEventLog,
            MetaStatistics,
            Length
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "History" );
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

                if ( Item.GetItem() is UnlockCollection unlockColl )
                {
                    unlockCollection = unlockColl;
                }
                else if ( Item.GetItem() is UpgradeCollection upgradeColl )
                {
                    upgradeCollection = upgradeColl;
                }
                else if ( Item.GetItem() is MachineProjectCollection projectColl )
                {
                    projectCollection = projectColl;
                }
                else if ( Item.GetItem() is NoteInstructionCollection eventLogColl )
                {
                    eventLogCollection = eventLogColl;
                }
                else if ( Item.GetItem() is MajorLocationAggregationType majorLocCat )
                {
                    MajorLocationCategory = majorLocCat;
                }
                else if ( Item.GetItem() is NPCCohortCategory cohortCat )
                {
                    CohortCategory = cohortCat;
                }
                else if ( Item.GetItem() is ContemplationCollection contemplationColl )
                {
                    contemplationCollection = contemplationColl;
                }
                else if ( Item.GetItem() is StreetSenseCollection streetSenseColl )
                {
                    streetSenseCollection = streetSenseColl;
                }
                else if ( Item.GetItem() is ExplorationSiteCollection explorationSiteColl )
                {
                    explorationSiteCollection = explorationSiteColl;
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;


                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HistoryType.UnlockHistory:
                        #region UnlockHistory
                        {
                            List<UnlockCollection> validOptions = UnlockCollection.AvailableCollections.GetDisplayList();

                            UnlockCollection typeDataToSelect = unlockCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    unlockCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as UnlockCollection != typeDataToSelect) )
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
                                    UnlockCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    UnlockCollection optionItemAsType = option.GetItem() as UnlockCollection;
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
                                    UnlockCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.Upgrades:
                        #region Upgrades
                        {
                            List<UpgradeCollection> validOptions = UpgradeCollection.AvailableCollections.GetDisplayList();

                            UpgradeCollection typeDataToSelect = upgradeCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    upgradeCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as UpgradeCollection != typeDataToSelect) )
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
                                    UpgradeCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    UpgradeCollection optionItemAsType = option.GetItem() as UpgradeCollection;
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
                                    UpgradeCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.ProjectHistory:
                        #region ProjectHistory
                        {
                            List<MachineProjectCollection> validOptions = MachineProjectCollection.AvailableCollections.GetDisplayList();

                            MachineProjectCollection typeDataToSelect = projectCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    projectCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as MachineProjectCollection != typeDataToSelect) )
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
                                    MachineProjectCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    MachineProjectCollection optionItemAsType = option.GetItem() as MachineProjectCollection;
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
                                    MachineProjectCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.EventLog:
                        #region EventLog
                        {
                            List<NoteInstructionCollection> validOptions = NoteLog.FullCollections.GetDisplayList();
                            foreach ( NoteInstructionCollection item in validOptions )
                                item.Meta_EffectiveVisible = item.Meta_TotalVisible.Display;

                            NoteInstructionCollection typeDataToSelect = eventLogCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    eventLogCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as NoteInstructionCollection != typeDataToSelect) )
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
                                    NoteInstructionCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    NoteInstructionCollection optionItemAsType = option.GetItem() as NoteInstructionCollection;
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
                                    NoteInstructionCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.MajorLocations:
                        #region MajorLocations
                        {
                            if ( majorLocationCategories.Count == 0 )
                                return;

                            MajorLocationAggregationType typeDataToSelect = MajorLocationCategory;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !majorLocationCategories.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    MajorLocationCategory = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && majorLocationCategories.Count > 0 )
                            {
                                typeDataToSelect = majorLocationCategories[0];
                                MajorLocationCategory = typeDataToSelect;
                            }
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as MajorLocationAggregationType != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( majorLocationCategories.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < majorLocationCategories.Count; i++ )
                                {
                                    MajorLocationAggregationType row = majorLocationCategories[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    MajorLocationAggregationType optionItemAsType = option.GetItem() as MajorLocationAggregationType;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < majorLocationCategories.Count; i++ )
                                {
                                    MajorLocationAggregationType row = majorLocationCategories[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.Cohorts:
                        #region Cohorts
                        {
                            if ( cohortCategories.Count == 0 )
                                return;

                            NPCCohortCategory typeDataToSelect = CohortCategory;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !cohortCategories.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    CohortCategory = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && cohortCategories.Count > 0 )
                            {
                                typeDataToSelect = cohortCategories[0];
                                CohortCategory = typeDataToSelect;
                            }
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as NPCCohortCategory != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( cohortCategories.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < cohortCategories.Count; i++ )
                                {
                                    NPCCohortCategory row = cohortCategories[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    NPCCohortCategory optionItemAsType = option.GetItem() as NPCCohortCategory;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < cohortCategories.Count; i++ )
                                {
                                    NPCCohortCategory row = cohortCategories[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.Contemplations:
                        #region Contemplations
                        {
                            List<ContemplationCollection> validOptions = ContemplationCollection.AvailableCollections.GetDisplayList();

                            ContemplationCollection typeDataToSelect = contemplationCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    contemplationCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as ContemplationCollection != typeDataToSelect) )
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
                                    ContemplationCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    ContemplationCollection optionItemAsType = option.GetItem() as ContemplationCollection;
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
                                    ContemplationCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.StreetSense:
                        #region StreetSense
                        {
                            List<StreetSenseCollection> validOptions = StreetSenseCollection.AvailableCollections.GetDisplayList();

                            StreetSenseCollection typeDataToSelect = streetSenseCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    streetSenseCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as StreetSenseCollection != typeDataToSelect) )
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
                                    StreetSenseCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    StreetSenseCollection optionItemAsType = option.GetItem() as StreetSenseCollection;
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
                                    StreetSenseCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HistoryType.ExplorationSites:
                        #region ExplorationSites
                        {
                            List<ExplorationSiteCollection> validOptions = ExplorationSiteCollection.AvailableCollections.GetDisplayList();

                            ExplorationSiteCollection typeDataToSelect = explorationSiteCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    explorationSiteCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as ExplorationSiteCollection != typeDataToSelect) )
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
                                    ExplorationSiteCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    ExplorationSiteCollection optionItemAsType = option.GetItem() as ExplorationSiteCollection;
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
                                    ExplorationSiteCollection row = validOptions[i];
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
                    case HistoryType.UnlockHistory:
                    case HistoryType.Upgrades:
                    case HistoryType.ProjectHistory:
                    case HistoryType.EventLog:
                    case HistoryType.MajorLocations:
                    case HistoryType.Cohorts:
                    case HistoryType.Contemplations:
                    case HistoryType.StreetSense:
                    case HistoryType.ExplorationSites:
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
                    case HistoryType.MetaEventLog:
                        MetaEventLog_FilterText = newString;
                        break;
                    case HistoryType.MetaStatistics:
                        MetaStatistics_FilterText = newString;
                        break;
                    case HistoryType.ProjectHistory:
                        ProjectHistory_FilterText = newString;
                        break;
                    case HistoryType.ActionStatistics:
                        ActionStatistics_FilterText = newString;
                        break;
                    case HistoryType.CityStatistics:
                        CityWideStatistics_FilterText = newString;
                        break;
                    case HistoryType.UnlockHistory:
                        Unlock_FilterText = newString;
                        break;
                    case HistoryType.Upgrades:
                        Upgrade_FilterText = newString;
                        break;
                    case HistoryType.EventLog:
                        EventLog_FilterText = newString;
                        break;
                    case HistoryType.MajorLocations:
                        MajorLocations_FilterText = newString;
                        break;
                    case HistoryType.Cohorts:
                        Cohorts_FilterText = newString;
                        break;
                    case HistoryType.CityConflicts:
                        CityConflicts_FilterText = newString;
                        break;
                }
            }

            public override bool GetShouldBeHidden()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HistoryType.MetaEventLog:
                    case HistoryType.MetaStatistics:
                    case HistoryType.ProjectHistory:
                    case HistoryType.ActionStatistics:
                    case HistoryType.CityStatistics:
                    case HistoryType.UnlockHistory:
                    case HistoryType.Upgrades:
                    case HistoryType.EventLog:
                    case HistoryType.MajorLocations:
                    case HistoryType.Cohorts:
                    case HistoryType.CityConflicts:
                        return false;
                }
                return true;
            }

            public override void OnEndEdit()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HistoryType.MetaEventLog:
                        MetaEventLog_FilterText = this.GetText();
                        break;
                    case HistoryType.MetaStatistics:
                        MetaStatistics_FilterText = this.GetText();
                        break;
                    case HistoryType.ProjectHistory:
                        ProjectHistory_FilterText = this.GetText();
                        break;
                    case HistoryType.ActionStatistics:
                        ActionStatistics_FilterText = this.GetText();
                        break;
                    case HistoryType.CityStatistics:
                        CityWideStatistics_FilterText = this.GetText();
                        break;
                    case HistoryType.UnlockHistory:
                        Unlock_FilterText = this.GetText();
                        break;
                    case HistoryType.Upgrades:
                        Upgrade_FilterText = this.GetText();
                        break;
                    case HistoryType.EventLog:
                        EventLog_FilterText = this.GetText();
                        break;
                    case HistoryType.MajorLocations:
                        MajorLocations_FilterText = this.GetText();
                        break;
                    case HistoryType.Cohorts:
                        Cohorts_FilterText = this.GetText();
                        break;
                    case HistoryType.CityConflicts:
                        CityConflicts_FilterText = this.GetText();
                        break;
                }
            }

            private ArcenUI_Input inputField = null;
            private HistoryType lastDisplayType = HistoryType.Length;

            public override void OnMainThreadUpdate()
            {
                if ( lastDisplayType != customParent.currentlyRequestedDisplayType )
                {
                    switch ( customParent.currentlyRequestedDisplayType )
                    {
                        case HistoryType.MetaEventLog:
                            this.SetText( MetaEventLog_FilterText );
                            break;
                        case HistoryType.MetaStatistics:
                            this.SetText( MetaStatistics_FilterText );
                            break;
                        case HistoryType.ProjectHistory:
                            this.SetText( ProjectHistory_FilterText );
                            break;
                        case HistoryType.ActionStatistics:
                            this.SetText( ActionStatistics_FilterText );
                            break;
                        case HistoryType.CityStatistics:
                            this.SetText( CityWideStatistics_FilterText );
                            break;
                        case HistoryType.UnlockHistory:
                            this.SetText( Unlock_FilterText );
                            break;
                        case HistoryType.Upgrades:
                            this.SetText( Upgrade_FilterText );
                            break;
                        case HistoryType.EventLog:
                            this.SetText( EventLog_FilterText );
                            break;
                        case HistoryType.MajorLocations:
                            this.SetText( MajorLocations_FilterText );
                            break;
                        case HistoryType.Cohorts:
                            this.SetText( Cohorts_FilterText );
                            break;
                        case HistoryType.CityConflicts:
                            this.SetText( CityConflicts_FilterText );
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

            public HistoryType displayType = HistoryType.Length;

            private string NameFromLang => Lang.Get( $"ActivityOverview_{displayType.ToString()}" );
            private string DescriptionFromLang => Lang.Get( $"ActivityOverview_{displayType.ToString()}Description" );

            public void Assign( HistoryType displayType )
            {
                this.displayType = displayType;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( displayType != HistoryType.Length )
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
                this.ExtraSpaceAfterInAutoSizing = 0f;
                if ( this.displayType != HistoryType.Length )
                {
                    isSelected = customParent.currentlyRequestedDisplayType == displayType;
                    switch ( this.displayType )
                    {
                        case HistoryType.NamedThings:
                            countToShow = -1;
                            showGrayedOut = false;
                            break;
                        case HistoryType.KeyContacts:
                            countToShow = SimCommon.ActiveKeyContacts.Count;
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                            break;
                        case HistoryType.Contemplations:
                            countToShow = SimCommon.ContemplationsAvailable.Count;
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                            break;
                        case HistoryType.StreetSense:
                            countToShow = SimCommon.CurrentStreetSenseTitleBuildings.Count;
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                            break;
                        case HistoryType.ExplorationSites:
                            countToShow = SimCommon.ExplorationSitesAvailable.Count;
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                            break;
                        case HistoryType.CityStatistics:
                            countToShow = sortedCitywideStatistics.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case HistoryType.ActionStatistics:
                            countToShow = sortedCityActionStatistics.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.ProjectHistory:
                            countToShow = SimCommon.CompletedProjects.Count + SimCommon.FailedProjects.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.UnlockHistory:
                            countToShow = sortedInventions.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case HistoryType.Upgrades:
                            countToShow = sortedUpgrades.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.EventLog:
                            countToShow = sortedEvents.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.MajorLocations:
                            countToShow = sortedMajorLocations.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.Cohorts:
                            countToShow = sortedCohorts.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case HistoryType.CityConflicts:
                            countToShow = sortedCityConflicts_Active.Count + sortedCityConflicts_Complete.Count;
                            showGrayedOut = countToShow == 0 || FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped;
                            countToShow = sortedCityConflicts_Active.Count;
                            if ( countToShow == 0 )
                                countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                            break;
                        case HistoryType.MetaStatistics:
                            countToShow = sortedMetaStatistics.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.MetaEventLog:
                            countToShow = sortedMetaEvents.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            break;
                        case HistoryType.Length:
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( "Forgot to set up bCategory-GetTextToShowFromVolatile for city " + this.displayType + "!", Verbosity.ShowAsError );
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

            public override bool GetShouldBeHidden() => displayType == HistoryType.Length;

            public override void Clear()
            {
                displayType = HistoryType.Length;
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
            public override bool GetShouldBeHidden()
            {
                return true;
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

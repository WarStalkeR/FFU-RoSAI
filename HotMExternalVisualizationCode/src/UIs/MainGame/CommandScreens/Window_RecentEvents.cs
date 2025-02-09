using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_RecentEvents : ToggleableWindowController, IInputActionHandler
    {
        public static Window_RecentEvents Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_RecentEvents()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        public static string FilterText = string.Empty;

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "RecentEvents" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public override void OnOpen()
        {
            hasSetFilterTextSinceStart = false;
            hasDoneFirstEventLogSort = false;
            sortedEvents.Clear();

            EventLog_FilterText = string.Empty;
            eventLogCollection = null;

            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );

            base.OnOpen();
        }

        private static bool hasDoneFirstEventLogSort = false;
        private static readonly List<NoteLog.NoteDisplayData> sortedEvents = List<NoteLog.NoteDisplayData>.Create_WillNeverBeGCed( 40, "Window_RecentEvents-sortedEvents" );
        private static readonly List<NoteLog.NoteDisplayData> sortedEventsWorking = List<NoteLog.NoteDisplayData>.Create_WillNeverBeGCed( 40, "Window_RecentEvents-sortedEvents" );

        private static bool hasSetFilterTextSinceStart = false;
        private static string EventLog_FilterText = string.Empty;
        private static string EventLog_LastFilterText = string.Empty;
        private static int EventLog_lastTurn = 0;
        private static NoteInstructionCollection eventLogCollection = null;
        private static NoteInstructionCollection lastEventLogCollection = null;

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

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_RecentEvents.CustomParentInstance = this;
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
                if ( VisCurrent.IsUIHiddenExceptForSidebar || Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
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

                    if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                        Instance.Close( WindowCloseReason.ShowingRefused );

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
            private const float MAIN_CONTENT_WIDTH = 280f;
            private const float CONTENT_ROW_GAP = 2.55f;
            private const float CONTENT_ROW = 11.9f;
            private const float HEADER_ROW = 17.4f;

            #region CalculateBoundsSingle
            protected float leftBuffer = 5.1f;
            protected float topBuffer = -6;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW );

                innerY -= CONTENT_ROW + CONTENT_ROW_GAP;
            }

            protected void CalculateBoundsHeader( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, HEADER_ROW );

                innerY -= HEADER_ROW + CONTENT_ROW_GAP;
            }
            #endregion

            private void GetAndSortEventLog()
            {
                hasDoneFirstEventLogSort = true;
                sortedEventsWorking.Clear();
                List<KeyValuePair<IGameNote, IGameNote>> list = NoteLog.RecentLog.GetDisplayList();
                for ( int i = 0; i < list.Count; i++ )
                {
                    KeyValuePair<IGameNote, IGameNote> kv = list[i];
                    IGameNote note = kv.Key;
                    if ( note == null ) 
                        continue;
                    if ( !note.HandleNote( GameNoteAction.GetIsStillValid, null, false, null, null, string.Empty, 0, false ) )
                        continue;

                    NoteInstruction inst = note.Instruction;

                    bool isHeader = (inst?.HeaderType ?? 0) == 2;

                    if ( note.PruneAfter > 0 )
                    {
                        if ( eventLogCollection.ExcludeEphemeral )
                            continue; //ephemeral
                    }
                    else
                    {
                        if ( !isHeader )
                        {
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

                    sortedEventsWorking.Add( NoteLog.NoteDisplayData.Create( note, isHeader, kv.Value ) );
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

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                if ( eventLogCollection == null )
                    eventLogCollection = NoteInstructionCollectionTable.Instance.DefaultRow;

                if ( !hasDoneFirstEventLogSort || EventLog_LastFilterText != EventLog_FilterText || EventLog_lastTurn != SimCommon.Turn ||
                        lastEventLogCollection != eventLogCollection )
                {
                    GetAndSortEventLog();
                    EventLog_LastFilterText = EventLog_FilterText;
                    EventLog_lastTurn = SimCommon.Turn;
                    lastEventLogCollection = eventLogCollection;
                }

                List<IGameNote> list = Engine_HotM.GameMode == MainGameMode.TheEndOfTime ? SimMetagame.LongTermMetaLog : NoteLog.LongTermGameLog;

                //now actually loop over the entries and add those that are in the viewing window
                foreach ( NoteLog.NoteDisplayData dd in sortedEvents )
                {
                    IGameNote note = dd.Note;
                    IGameNote lastTurnHeader = dd.LastHeader;

                    Rect bounds;

                    if ( dd.IsHeader )
                        this.CalculateBoundsHeader( out bounds, ref runningY );
                    else
                        this.CalculateBoundsSingle( out bounds, ref runningY );

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
                    else
                    {
                        bDataItem row = bDataItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            break; //this was just time-slicing, so ignore that failure for now
                        bDataItemPool.ApplySingleItemInRow( row, bounds, false );

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
            }
            #endregion
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
                EventLog_FilterText = newString;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void OnEndEdit()
            {
                EventLog_FilterText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( !hasSetFilterTextSinceStart )
                {
                    hasSetFilterTextSinceStart = true;
                    this.SetText( EventLog_FilterText );
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

        #region dFilter
        public class dFilter : DropdownAbstractBase
        {
            public static dFilter Instance;
            public dFilter()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                if ( Item.GetItem() is NoteInstructionCollection eventLogColl )
                {
                    eventLogCollection = eventLogColl;
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                #region EventLog
                {
                    List<NoteInstructionCollection> validOptions = NoteLog.RecentCollections.GetDisplayList();
                    foreach ( NoteInstructionCollection item in validOptions )
                        item.Meta_EffectiveVisible = item.Meta_RecentVisible.Display;

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
                    if ( typeDataToSelect == null && validOptions.Count > 1 )
                        typeDataToSelect = NoteInstructionCollectionTable.Instance.DefaultRow;
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

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Row ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        Row.WriteOptionDisplayTextForDropdown( novel.TitleUpperLeft );
                        novel.Main.AddRaw( description );
                    }
                }
            }

            public override bool GetShouldBeHidden()
            {
                return false;
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
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

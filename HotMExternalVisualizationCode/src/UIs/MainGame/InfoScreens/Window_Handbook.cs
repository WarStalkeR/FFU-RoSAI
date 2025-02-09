using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_Handbook : ToggleableWindowController, IInputActionHandler
    {
        public static Window_Handbook Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_Handbook()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        public static string FilterText = string.Empty;
        public static MachineHandbookCollection CurrentCollection = null;
        public static MachineHandbookEntry EntryToShow = null;

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "MachineHandbook" ).Space1x().AddFormat1( "Parenthetical", MachineHandbookEntry.DuringGame_UnlockedEntries );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public override void OnClose( WindowCloseReason CloseReason )
        {
            base.OnClose( CloseReason );
        }

        public override void OnOpen()
        {
            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );
            FilterText = string.Empty;
            //EntryToShow = null;

            base.OnOpen();
        }

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_Handbook.CustomParentInstance = this;
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

                    bDataItemPool.Clear( 60 );
                    bDataHeaderPool.Clear( 10 );

                    RectTransform rTran = (RectTransform)bDataItem.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = -3f;

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
            private const float MAIN_CONTENT_WIDTH = 350f;

            #region CalculateBoundsNormalRow
            protected void CalculateBoundsNormalRow( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( 5.1f, innerY, MAIN_CONTENT_WIDTH, 24f );

                innerY -= 24f + 1.5f;
            }
            #endregion

            #region CalculateBoundsHeaderRow
            protected void CalculateBoundsHeaderRow( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( 5.1f, innerY, MAIN_CONTENT_WIDTH, 20.6f );

                innerY -= ( 20.6f + 4f);
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                //now actually loop over the entries and add those that are in the viewing window
                foreach ( MachineHandbookSection section in MachineHandbookSectionTable.Instance.Rows )
                {
                    bool hasDoneHeader = false;
                    foreach ( MachineHandbookEntry entry in section.EntriesList )
                    {
                        if ( !entry.Meta_HasBeenUnlocked )
                            continue;
                        if ( CurrentCollection != null && entry.Collections[CurrentCollection.ID] == null )
                            continue;
                        if ( !FilterText.IsEmpty() )
                        {
                            if ( !entry.GetMatchesSearchString( FilterText ) )
                                continue;
                        }

                        if ( !hasDoneHeader )
                        {
                            hasDoneHeader = true;

                            this.CalculateBoundsHeaderRow( out Rect headerBounds, ref runningY );
                            bool render = true;
                            if ( headerBounds.yMax < minYToShow )
                                render = false; //it's scrolled up far enough we can skip it, yay!
                            if ( headerBounds.yMax > maxYToShow )
                                render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                            if ( render )
                            {
                                bDataHeader header = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                                if ( header != null ) //this was just time-slicing, so ignore that failure for now
                                {
                                    bDataHeaderPool.ApplySingleItemInRow( header, headerBounds, false );

                                    header.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                    {
                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    bool isSelected = EntryToShow == entry;
                                                    bool isHovered = element.LastHadMouseWithin;

                                                    header.SetRelatedImage1EnabledIfNeeded( section.Meta_EntriesUnread > 0 );
                                                    header.SetRelatedImage2SpriteIfNeeded( header.Element.RelatedSprites[section.Meta_IsOpen ? 1 : 0] );

                                                    string colorHex = ColorTheme.GetCategoryWhite( isHovered );
                                                    ExtraData.Buffer.StartColor( colorHex ).AddRaw( section.GetDisplayName() )
                                                        .Space5x().StartSize70().AddFormat1( "Parenthetical", section.Meta_EntriesVisible, colorHex ).EndSize();
                                                }
                                                break;
                                            case UIAction.GetOtherTextToShowFromVolatile:
                                                {
                                                    switch ( ExtraData.Int )
                                                    {
                                                        case 0:
                                                            if ( section.Meta_EntriesUnread > 0 )
                                                                ExtraData.Buffer.AddFormat1( "UnreadCount", section.Meta_EntriesUnread );
                                                            break;
                                                        default:
                                                            ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for section: " + ExtraData.Int, Verbosity.ShowAsError );
                                                            break;
                                                    }
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                section.Meta_IsOpen = !section.Meta_IsOpen;
                                                break;
                                        }
                                    } );
                                }
                            }
                        }

                        if ( !section.Meta_IsOpen )
                        {
                            if ( ( CurrentCollection == null || CurrentCollection.ID == "All" ) && FilterText.IsEmpty() )
                                continue;
                        }

                        this.CalculateBoundsNormalRow( out Rect bounds, ref runningY );
                        if ( bounds.yMax < minYToShow )
                            continue; //it's scrolled up far enough we can skip it, yay!
                        if ( bounds.yMax > maxYToShow )
                            continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        bDataItem row = bDataItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( row == null )
                            break; //this was just time-slicing, so ignore that failure for now
                        bDataItemPool.ApplySingleItemInRow( row, bounds, false );

                        row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        bool isSelected = EntryToShow == entry;
                                        bool isHovered = element.LastHadMouseWithin;

                                        row.SetRelatedImage0EnabledIfNeeded( isSelected );

                                        string colorHex = isSelected ? ColorTheme.GetInvertibleListTextBlueDarker_Selected( isHovered ) : ColorTheme.GetInvertibleListTextBlue_Normal( isHovered );
                                        if ( isSelected )
                                            ExtraData.Buffer.StartBold();

                                        ExtraData.Buffer.AddRaw( entry.GetDisplayName(), colorHex );
                                    }
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    {
                                        switch ( ExtraData.Int )
                                        {
                                            case 0:
                                                if ( !entry.Meta_HasBeenRead )
                                                    ExtraData.Buffer.AddLang( "NewItem_Brief" );
                                                break;
                                            default:
                                                ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                break;
                                        }
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        
                                    }
                                    break;
                                case UIAction.OnClick:
                                    entry.Meta_HasBeenRead = true;
                                    if ( EntryToShow == entry )
                                        EntryToShow = null;
                                    else
                                        EntryToShow = entry;
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
                    ArcenInput.BlockForAJustPartOfOneSecond();
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

        #region bDataHeader
        public class bDataHeader : ButtonAbstractBaseWithImage
        {
            public static bDataHeader Original;
            public bDataHeader() { if ( Original == null ) Original = this; }

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
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
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

                MachineHandbookCollection ItemAsType = (MachineHandbookCollection)Item.GetItem();
                CurrentCollection = ItemAsType;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                List<MachineHandbookCollection> validOptions = MachineHandbookCollection.AvailableCollections.GetDisplayList();

                MachineHandbookCollection typeDataToSelect = CurrentCollection;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        CurrentCollection = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && validOptions.Count > 0 )
                    typeDataToSelect = validOptions[0];
                #endregion

                if ( CurrentCollection == null && typeDataToSelect != null )
                    CurrentCollection = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (MachineHandbookCollection)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
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
                        MachineHandbookCollection row = validOptions[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        MachineHandbookCollection optionItemAsType = (MachineHandbookCollection)option.GetItem();
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
                        MachineHandbookCollection row = validOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                //MachineHandbookCollection ItemAsType = (MachineHandbookCollection)Item.GetItem();
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

        #region Left Side

        public class tLeftLargeHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( EntryToShow != null )
                    Buffer.AddRaw( EntryToShow.GetDisplayName() );
            }
        }

        public class tLeftLargeSubHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( EntryToShow?.Section != null )
                    Buffer.AddRaw( EntryToShow.Section.GetDisplayName() );
            }
        }

        public class tLeftSmallHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( EntryToShow != null )
                    Buffer.AddRaw( EntryToShow.GetDisplayName(), ColorTheme.NarrativeHeader );
            }
        }

        public class tLeftMainText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( EntryToShow != null )
                {
                    Buffer.StartColor( ColorTheme.NarrativeColor ).AddRaw( EntryToShow.GetDescription() ).Line();
                    Buffer.AddRaw( EntryToShow.StrategyTip.Text ).Line();

                    if ( FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_State == CityTaskState.Active )
                        FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_CompleteIfActive();
                }
            }
        }
        #endregion
    }
}

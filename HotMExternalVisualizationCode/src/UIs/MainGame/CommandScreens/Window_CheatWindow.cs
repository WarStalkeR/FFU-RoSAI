using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Window_CheatWindow : ToggleableWindowController, IInputActionHandler
    {
        public static Window_CheatWindow Instance;
        public Window_CheatWindow()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
        }

        #region OnOpen
        public override void OnOpen()
        {
            
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public sealed override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "CheatWindow_Header" );
            }
        }
        #endregion

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( !Engine_Universal.PrimaryIsLeft )
                {
                    //if the sidebar is on our side, move over to be out of its way
                    this.WindowController.ExtraOffsetX = -Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                }
                else
                    this.WindowController.ExtraOffsetX = 0; //sidebar is not bothering us, so no worries!

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    //if ( bIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                    }
                }
                #endregion
            }
        }

        #region dOverlayStyle
        public class dOverlayStyle : DropdownAbstractBase
        {
            public static dOverlayStyle Instance;
            public dOverlayStyle()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                CheatType ItemAsType = (CheatType)Item.GetItem();
                Engine_HotM.CurrentCheat = ItemAsType;
                Engine_HotM.CurrentCheatSelection = null;
                Engine_HotM.CurrentCheatText = string.Empty;
                Engine_HotM.CurrentCheatOverridingClickMode = null;
                
            }

            private static readonly List<CheatType> validListOfGroups = List<CheatType>.Create_WillNeverBeGCed( 200, "dOverlayStyle-validListOfGroups", 200 );

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                CheatTypeTable.Instance.FillListMatchingCurrentConstraints( validListOfGroups, out CheatType DefaultRow );

                CheatType typeDataToSelect = Engine_HotM.CurrentCheat;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validListOfGroups.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Engine_HotM.CurrentCheat = null;
                        Engine_HotM.CurrentCheatSelection = null;
                        Engine_HotM.CurrentCheatText = string.Empty;
                        Engine_HotM.CurrentCheatOverridingClickMode = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && validListOfGroups.Count > 0 )
                    typeDataToSelect = validListOfGroups[0];
                #endregion

                if ( Engine_HotM.CurrentCheat == null && typeDataToSelect != null )
                {
                    Engine_HotM.CurrentCheat = typeDataToSelect;
                    Engine_HotM.CurrentCheatOverridingClickMode = null;
                }

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (CheatType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Engine_HotM.CurrentCheatSelection = null;
                    Engine_HotM.CurrentCheatText = string.Empty;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( validListOfGroups.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        CheatType row = validListOfGroups[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        CheatType optionItemAsType = (CheatType)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        CheatType row = validListOfGroups[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                CheatType typeDataToSelect = Engine_HotM.CurrentCheat;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "CheatWindow_CheatType" );

                    if ( typeDataToSelect != null )
                    {
                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "InfoWindow_Currently" ).AddRaw( typeDataToSelect.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        if ( !typeDataToSelect.GetDescription().IsEmpty() )
                            novel.FrameBody.AddRaw( typeDataToSelect.GetDescription(), ColorTheme.NarrativeColor ).Line();
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                CheatType ItemAsType = (CheatType)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "CheatWindow_CheatType" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region dOverlaySelection
        public class dOverlaySelection : DropdownAbstractBase
        {
            public static dOverlaySelection Instance;
            public dOverlaySelection()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                ICheatSecondaryDataSource ItemAsType = (ICheatSecondaryDataSource)Item.GetItem();
                Engine_HotM.CurrentCheatSelection = ItemAsType;
            }

            public override bool GetShouldBeHidden()
            {
                CheatType currentCheat = Engine_HotM.CurrentCheat;
                if ( currentCheat != null && currentCheat.Implementation != null && currentCheat.Implementation.GetUsesSecondaryDataSources( currentCheat ) )
                    return false;
                return true;
            }

            private static readonly List<ICheatSecondaryDataSource> validListOfGroups = List<ICheatSecondaryDataSource>.Create_WillNeverBeGCed( 200, "dOverlaySelection-validListOfGroups", 200 );

            public override void OnUpdate()
            {
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                    debugStage = 200;
                    CheatType currentCheat = Engine_HotM.CurrentCheat;
                    ICheatSecondaryDataSource DefaultRow = null;
                    if ( currentCheat != null && currentCheat.Implementation != null )
                        currentCheat.Implementation.FillListOfSecondaryDataSource( currentCheat, validListOfGroups, out DefaultRow );
                    else
                        validListOfGroups.Clear();

                    debugStage = 500;
                    ICheatSecondaryDataSource typeDataToSelect = Engine_HotM.CurrentCheatSelection;

                    debugStage = 1100;
                    #region If The Selected Type Is Not Valid Right Now, Then Skip It
                    if ( typeDataToSelect != null )
                    {
                        debugStage = 1200;
                        if ( !validListOfGroups.Contains( typeDataToSelect ) )
                        {
                            debugStage = 1300;
                            typeDataToSelect = null;
                            Engine_HotM.CurrentCheatSelection = null;
                        }
                    }
                    #endregion

                    debugStage = 2100;
                    #region Select Default If Blank
                    if ( typeDataToSelect == null && DefaultRow != null )
                        typeDataToSelect = DefaultRow;
                    debugStage = 2300;
                    if ( typeDataToSelect == null && validListOfGroups.Count > 0 )
                        typeDataToSelect = validListOfGroups[0];
                    #endregion

                    debugStage = 3100;
                    if ( Engine_HotM.CurrentCheatSelection == null && typeDataToSelect != null )
                        Engine_HotM.CurrentCheatSelection = typeDataToSelect;

                    debugStage = 4100;
                    bool foundMismatch = false;
                    if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || ( elementAsType.CurrentlySelectedOption.GetItem() as ICheatSecondaryDataSource ) != typeDataToSelect) )
                    {
                        foundMismatch = true;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                    }
                    else
                    {
                        debugStage = 5100;
                        if ( validListOfGroups.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                            foundMismatch = true;
                        else
                        {
                            debugStage = 6100;
                            for ( int i = 0; i < validListOfGroups.Count; i++ )
                            {
                                debugStage = 6200;
                                ICheatSecondaryDataSource row = validListOfGroups[i];

                                debugStage = 6300;
                                IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                if ( option == null )
                                {
                                    foundMismatch = true;
                                    break;
                                }
                                debugStage = 6400;
                                ICheatSecondaryDataSource optionItemAsType = (ICheatSecondaryDataSource)option.GetItem();
                                debugStage = 6500;
                                if ( row == optionItemAsType )
                                    continue;
                                foundMismatch = true;
                                break;
                            }
                        }
                    }

                    debugStage = 8100;
                    if ( foundMismatch )
                    {
                        debugStage = 8200;
                        elementAsType.ClearItems();

                        debugStage = 8300;
                        for ( int i = 0; i < validListOfGroups.Count; i++ )
                        {
                            debugStage = 8400;
                            ICheatSecondaryDataSource row = validListOfGroups[i];
                            debugStage = 8500;
                            elementAsType.AddItem( row, row == typeDataToSelect );
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "CheatDropdownUpdate Error", debugStage, e, Verbosity.ShowAsError );
                }
            }
            public override void HandleMouseover()
            {
                ICheatSecondaryDataSource typeDataToSelect = Engine_HotM.CurrentCheatSelection;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "CheatItemType", typeDataToSelect.GetDisplayName() ), this.Element,
                   SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "CheatWindow_CheatTypeSelection" );

                    if ( typeDataToSelect != null )
                    {
                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "InfoWindow_Currently" ).AddRaw( typeDataToSelect.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        if ( !typeDataToSelect.GetDescription().IsEmpty() )
                            novel.FrameBody.AddRaw( typeDataToSelect.GetDescription(), ColorTheme.NarrativeColor ).Line();
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                ICheatSecondaryDataSource ItemAsType = (ICheatSecondaryDataSource)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "CheatItemType", ItemAsType.GetDisplayName() ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "CheatWindow_CheatTypeSelection" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region iText
        public class iText : InputAbstractBase
        {
            public int maxTextLength = 50;
            public static iText Instance;
            public iText() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar;
            }

            public override void OnValueChanged( string newString )
            {
                Engine_HotM.CurrentCheatText = newString;
            }

            public override bool GetShouldBeHidden()
            {
                CheatType currentCheat = Engine_HotM.CurrentCheat;
                if ( currentCheat != null && currentCheat.Implementation != null && currentCheat.Implementation.GetUsesTextbox( currentCheat ) )
                    return false;
                return true;
            }

            public override void OnEndEdit()
            {
                Engine_HotM.CurrentCheatText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( inputField == null )
                {
                    inputField = (this.Element as ArcenUI_Input);
                    inputField?.SetPlaceholderTextLangKey( "CheatWindow_CheatText" );
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

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Cancel );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        public class bSubmit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Submit );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                DoOnSubmit();
                return MouseHandlingResult.None;
            }

            public static void DoOnSubmit()
            {
                CheatType currentCheat = Engine_HotM.CurrentCheat;
                //without this, the value is not set unless you explicitly click a choice
                Engine_HotM.CurrentCheatSelection = (dOverlaySelection.Instance.Element as ArcenUI_Dropdown).GetSelectedObject() as ICheatSecondaryDataSource;
                if ( currentCheat != null && currentCheat.Implementation != null && currentCheat.Implementation.ExecuteCheat( currentCheat, Engine_HotM.CurrentCheatSelection, Engine_HotM.CurrentCheatText ) )
                {
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                }
            }
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    bSubmit.DoOnSubmit();
                    break;
                //case "Cancel":
                //    this.Close();
                //    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                //    ArcenInput.BlockForAJustPartOfOneSecond();
                //    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

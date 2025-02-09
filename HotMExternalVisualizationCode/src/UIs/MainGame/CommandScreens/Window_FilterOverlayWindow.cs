using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Window_FilterOverlayWindow : ToggleableWindowController, IInputActionHandler
    {
        public static Window_FilterOverlayWindow Instance;
        public Window_FilterOverlayWindow()
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
                Buffer.AddLang( "OverlayWindow_Header" );
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
                BuildingOverlayType ItemAsType = (BuildingOverlayType)Item.GetItem();
                Engine_HotM.OverlayType_PlayerChosen = ItemAsType;
                Engine_HotM.OverlayType_PlayerChosen_Selection = null;
                Engine_HotM.OverlayType_PlayerChosen_Text = string.Empty;
            }

            private static readonly List<BuildingOverlayType> validListOfGroups = List<BuildingOverlayType>.Create_WillNeverBeGCed( 200, "dOverlayStyle-validListOfGroups", 200 );

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                BuildingOverlayTypeTable.Instance.FillListMatchingCurrentConstraints( validListOfGroups, out BuildingOverlayType DefaultRow );

                BuildingOverlayType typeDataToSelect = Engine_HotM.OverlayType_PlayerChosen;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validListOfGroups.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Engine_HotM.OverlayType_PlayerChosen = null;
                        Engine_HotM.OverlayType_PlayerChosen_Selection = null;
                        Engine_HotM.OverlayType_PlayerChosen_Text = string.Empty;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && validListOfGroups.Count > 0 )
                    typeDataToSelect = validListOfGroups[0];
                #endregion

                if ( Engine_HotM.OverlayType_PlayerChosen == null && typeDataToSelect != null )
                    Engine_HotM.OverlayType_PlayerChosen = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (BuildingOverlayType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Engine_HotM.OverlayType_PlayerChosen_Selection = null;
                    Engine_HotM.OverlayType_PlayerChosen_Text = string.Empty;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( validListOfGroups.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        BuildingOverlayType row = validListOfGroups[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        BuildingOverlayType optionItemAsType = (BuildingOverlayType)option.GetItem();
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
                        BuildingOverlayType row = validListOfGroups[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                BuildingOverlayType typeDataToSelect = Engine_HotM.OverlayType_PlayerChosen;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "OverlayWindow_BuildingOverlayStyle" );

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
                BuildingOverlayType ItemAsType = (BuildingOverlayType)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "OverlayWindow_BuildingOverlayStyle" );

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
                IBuildingOverlaySecondaryDataSource ItemAsType = (IBuildingOverlaySecondaryDataSource)Item.GetItem();
                Engine_HotM.OverlayType_PlayerChosen_Selection = ItemAsType;
            }

            public override bool GetShouldBeHidden()
            {
                BuildingOverlayType currentOverlay = Engine_HotM.OverlayType_PlayerChosen;
                if ( currentOverlay != null && currentOverlay.Implementation != null && currentOverlay.Implementation.GetUsesSecondaryDataSources( currentOverlay ) )
                    return false;
                return true;
            }

            private static readonly List<IBuildingOverlaySecondaryDataSource> validListOfGroups = List<IBuildingOverlaySecondaryDataSource>.Create_WillNeverBeGCed( 200, "dOverlaySelection-validListOfGroups", 200 );

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                BuildingOverlayType currentOverlay = Engine_HotM.OverlayType_PlayerChosen;
                IBuildingOverlaySecondaryDataSource DefaultRow = null;
                if ( currentOverlay != null && currentOverlay.Implementation != null )
                    currentOverlay.Implementation.FillListOfSecondaryDataSource( currentOverlay, validListOfGroups, out DefaultRow );
                else
                    validListOfGroups.Clear();

                IBuildingOverlaySecondaryDataSource typeDataToSelect = Engine_HotM.OverlayType_PlayerChosen_Selection;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validListOfGroups.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Engine_HotM.OverlayType_PlayerChosen_Selection = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && validListOfGroups.Count > 0 )
                    typeDataToSelect = validListOfGroups[0];
                #endregion

                if ( Engine_HotM.OverlayType_PlayerChosen_Selection == null && typeDataToSelect != null )
                    Engine_HotM.OverlayType_PlayerChosen_Selection = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (IBuildingOverlaySecondaryDataSource)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( validListOfGroups.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        IBuildingOverlaySecondaryDataSource row = validListOfGroups[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        IBuildingOverlaySecondaryDataSource optionItemAsType = (IBuildingOverlaySecondaryDataSource)option.GetItem();
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
                        IBuildingOverlaySecondaryDataSource row = validListOfGroups[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                IBuildingOverlaySecondaryDataSource typeDataToSelect = Engine_HotM.OverlayType_PlayerChosen_Selection;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "SecondaryBuilding", typeDataToSelect.GetDisplayName() ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "OverlayWindow_BuildingOverlayStyleSelection" );

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
                IBuildingOverlaySecondaryDataSource ItemAsType = (IBuildingOverlaySecondaryDataSource)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "SecondaryBuilding", ItemAsType.GetDisplayName() ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "OverlayWindow_BuildingOverlayStyleSelection" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region iSearch
        public class iSearch : InputAbstractBase
        {
            public int maxTextLength = 50;
            public static iSearch Instance;
            public iSearch() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar;
            }

            public override void OnValueChanged( string newString )
            {
                Engine_HotM.OverlayType_PlayerChosen_Text = newString;
            }

            public override bool GetShouldBeHidden()
            {
                BuildingOverlayType currentOverlay = Engine_HotM.OverlayType_PlayerChosen;
                if ( currentOverlay != null && currentOverlay.Implementation != null && currentOverlay.Implementation.GetUsesTextbox( currentOverlay ) )
                    return false;
                return true;
            }

            public override void OnEndEdit()
            {
                Engine_HotM.OverlayType_PlayerChosen_Text = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( inputField == null )
                {
                    inputField = (this.Element as ArcenUI_Input);
                    inputField?.SetPlaceholderTextLangKey( "OverlayWindow_FilterText" );
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
                Buffer.AddLang( LangCommon.Popup_Common_Close );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    this.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

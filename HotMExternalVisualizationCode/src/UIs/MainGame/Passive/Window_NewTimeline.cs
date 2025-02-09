using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_NewTimeline : ToggleableWindowController, IInputActionHandler
    {
        public static EndOfTimeItem CreateOnItem;
        public static int CreateOnItemAtIndex = -1;

        public static Window_NewTimeline Instance;
		
		public Window_NewTimeline()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if ( iCityName.Instance != null )
                iCityName.Instance.SetText( GenerateNewCityName( Engine_Universal.PermanentQualityRandom ) );

            dCityStyle.Selected = CityStyleTable.Instance.ValidOptions[0];
            dYourOrigin.Selected = MachineOriginTable.Instance.ValidOptions[0];
        }

        #region tTitleText
        public class tTitleText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "NewTimeline_WindowHeader" );
            }
        }
        #endregion

        #region tBodyText
        public class tBodyText : TextAbstractBase
        {
            public static ArcenUIWrapperedTMProText WrapperedText;
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( WrapperedText == null )
                    WrapperedText = Tex.Text;
                WrapperedText.CalculateHeightFromContentsButDoNotUpdate = true;

                Buffer.AddLang( "NewTimeline_Body" );
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                switch ( TooltipLinkData[0] )
                {
                    case "EnemyAndCanvas":
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NewTimeline", TooltipLinkData[0] ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "BackgroundLore_Header" );
                            novel.Main.AddLang( "NewTimeline_Body_EnemyAndCanvasDetails", ColorTheme.NarrativeColor );
                        }
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "Window_NewTimeline: Unknown link info '" + TooltipLinkData[0] + "'", Verbosity.ShowAsError );
                        break;
                }

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
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_CentralChoicePopup" );
                
                float extraOffsetY = -15;
                if ( Engine_Universal.IsAnyTextboxFocused && Engine_Universal.IsSteamDeckVersion )
                    extraOffsetY = -300;

                float offsetFromSide = 0;
                //if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                //    offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                //else //the sidebar is on, and is on the left, move right
                //    offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetX = offsetFromSide;
                this.WindowController.ExtraOffsetY = extraOffsetY;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bStart.Original != null )
                    {
                        hasGlobalInitialized = true;
                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                #region Expand or Shrink Height Of This Window
                float heightForWindow = HEIGHT_FROM_OTHER_BITS + (tBodyText.WrapperedText?.LastHeightSet ?? 0);
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion
            }

            private const float HEIGHT_FROM_OTHER_BITS = 26f + 112.9f + -3f //text top, plus header plus buffer
                + 25.7f + 25.7f + 25.7f + + 10f;//plus buttons plus more border

            private float lastWindowHeight = -1f;
        }

        public class bRandomize : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddSpriteStyled_NoIndent( IconRefs.RandomizedResult.Icon, AdjustedSpriteStyle.InlineLarger1_2,
                    this.Element.LastHadMouseWithin ? ColorTheme.HeaderLighterBlue : string.Empty );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ParticleSoundRefs.RerollSound.DuringGame_PlaySoundOnlyAtCamera();
                if ( iCityName.Instance != null )
                    iCityName.Instance.SetText( GenerateNewCityName( Engine_Universal.PermanentQualityRandom ) );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
            }
        }

        public class bCancel : ButtonAbstractBase
        {
            public static bCancel Original;
            public bCancel() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Cancel, ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
            }
        }

        #region tCityNameHeader
        public class tCityNameHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "NewTimeline_CityNameHeader" );
            }
        }
        #endregion

        public class iCityName : InputAbstractBase
        {
            public int maxTextLength = 20;
            public static iCityName Instance;
            public iCityName() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar; //anything they want to type is fine
            }
            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        if ( !EscapeWindowStackController.HandleCancel( WindowCloseReason.UserDirectRequest ) )
                            Window_ModalTextboxWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        bStart.TryToStart();
                        break;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            private bool hasSetNewName = false;
            public override void OnUpdate()
            {
                if (!hasSetNewName )
                {
                    hasSetNewName = true;
                    this.SetText( GenerateNewCityName( Engine_Universal.PermanentQualityRandom ) );
                }
            }
        }

        #region tCityStyle
        public class tCityStyle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "NewTimeline_CityStyleHeader" );
            }
        }
        #endregion

        #region dCityStyle
        public class dCityStyle : DropdownAbstractBase
        {
            public static CityStyle Selected = null;

            public static dCityStyle Instance;
            public dCityStyle()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                Selected = (CityStyle)Item.GetItem();

            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                CityStyle typeDataToSelect = Selected;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !CityStyleTable.Instance.ValidOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Selected = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && CityStyleTable.Instance.ValidOptions.Count > 0 )
                    typeDataToSelect = CityStyleTable.Instance.ValidOptions[0];
                #endregion

                if ( Selected == null && typeDataToSelect != null )
                    Selected = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (CityStyle)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Selected = null;
                }
                else if ( CityStyleTable.Instance.ValidOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < CityStyleTable.Instance.ValidOptions.Count; i++ )
                    {
                        CityStyle row = CityStyleTable.Instance.ValidOptions[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        CityStyle optionItemAsType = (CityStyle)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CityStyleTable.Instance.ValidOptions.Count; i++ )
                    {
                        CityStyle row = CityStyleTable.Instance.ValidOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
                CityStyle typeDataToSelect = Selected;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_CityStyleTooltipHeader" );
                    novel.Main.AddLang( "NewTimeline_CityStyle", ColorTheme.NarrativeColor );

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
                CityStyle ItemAsType = (CityStyle)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_CityStyleTooltipHeader" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region tStartingConflicts
        public class tStartingConflicts : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "NewTimeline_DoomTypeHeader" );
            }
        }
        #endregion

        #region dStartingConflicts
        public class dStartingConflicts : DropdownAbstractBase
        {
            public static CityTimelineDoomType Selected = null;

            public static dStartingConflicts Instance;
            public dStartingConflicts()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                Selected = (CityTimelineDoomType)Item.GetItem();

            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                CityTimelineDoomType typeDataToSelect = Selected;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !CityTimelineDoomTypeTable.Instance.ValidOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Selected = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && CityTimelineDoomTypeTable.Instance.ValidOptions.Count > 0 )
                    typeDataToSelect = CityTimelineDoomTypeTable.Instance.ValidOptions[0];
                #endregion

                if ( Selected == null && typeDataToSelect != null )
                    Selected = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (CityTimelineDoomType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Selected = null;
                }
                else if ( CityTimelineDoomTypeTable.Instance.ValidOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < CityTimelineDoomTypeTable.Instance.ValidOptions.Count; i++ )
                    {
                        CityTimelineDoomType row = CityTimelineDoomTypeTable.Instance.ValidOptions[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        CityTimelineDoomType optionItemAsType = (CityTimelineDoomType)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CityTimelineDoomTypeTable.Instance.ValidOptions.Count; i++ )
                    {
                        CityTimelineDoomType row = CityTimelineDoomTypeTable.Instance.ValidOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
                CityTimelineDoomType typeDataToSelect = Selected;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_DoomTypeHeader" );
                    novel.Main.AddLang( "NewTimeline_DoomType", ColorTheme.NarrativeColor );

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
                CityTimelineDoomType ItemAsType = (CityTimelineDoomType)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_DoomTypeTooltipHeader" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region tYourOrigin
        public class tYourOrigin : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "NewTimeline_YourOriginHeader" );
            }
        }
        #endregion

        #region dYourOrigin
        public class dYourOrigin : DropdownAbstractBase
        {
            public static MachineOrigin Selected = null;

            public static dYourOrigin Instance;
            public dYourOrigin()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                Selected = (MachineOrigin)Item.GetItem();

            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                MachineOrigin typeDataToSelect = Selected;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !MachineOriginTable.Instance.ValidOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Selected = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && MachineOriginTable.Instance.ValidOptions.Count > 0 )
                    typeDataToSelect = MachineOriginTable.Instance.ValidOptions[0];
                #endregion

                if ( Selected == null && typeDataToSelect != null )
                    Selected = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (MachineOrigin)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Selected = null;
                }
                else if ( MachineOriginTable.Instance.ValidOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < MachineOriginTable.Instance.ValidOptions.Count; i++ )
                    {
                        MachineOrigin row = MachineOriginTable.Instance.ValidOptions[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        MachineOrigin optionItemAsType = (MachineOrigin)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < MachineOriginTable.Instance.ValidOptions.Count; i++ )
                    {
                        MachineOrigin row = MachineOriginTable.Instance.ValidOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
                MachineOrigin typeDataToSelect = Selected;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_YourOriginTooltipHeader" );
                    novel.Main.AddLang( "NewTimeline_YourOrigin", ColorTheme.NarrativeColor );

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
                MachineOrigin ItemAsType = (MachineOrigin)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "NewTimeline_YourOriginTooltipHeader" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        public class bStart : ButtonAbstractBase
        {
            public static bStart Original;
            public bStart() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "NewCity_Dive", ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToStart();
                return MouseHandlingResult.None;
            }

            public static void TryToStart()
            {
                if ( CreateOnItem == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "UnableToCreateTimeline" ), LocalizedString.AddLang_New( "UnableToCreateTimeline_NoRockOrZiggurat" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                if ( !CreateOnItem.GetIsValidSlotIndexForNewCity( CreateOnItemAtIndex ) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "UnableToCreateTimeline" ), LocalizedString.AddLang_New( "UnableToCreateTimeline_ChosenSlotAlreadyFilled" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                string cityName = iCityName.Instance.GetText();
                if ( cityName.IsEmpty() )
                {
                    cityName = GenerateNewCityName( Engine_Universal.PermanentQualityRandom );
                    iCityName.Instance.SetText( cityName );

                }

                if ( GetIsNameAlreadyInUse( cityName ) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, 
                        LocalizedString.AddLang_New( "CityName_AlreadyInUse" ), LocalizedString.AddFormat1_New( "CityName_AlreadyInUseBody", cityName ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                if ( dCityStyle.Selected == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "UnableToCreateTimeline" ), LocalizedString.AddLang_New( "UnableToCreateTimeline_CityStyleNotChosen" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                if ( dStartingConflicts.Selected == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "UnableToCreateTimeline" ), LocalizedString.AddLang_New( "UnableToCreateTimeline_DoomTypeNotChosen" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                if ( dYourOrigin.Selected == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "UnableToCreateTimeline" ), LocalizedString.AddLang_New( "UnableToCreateTimeline_YourOriginNotChosen" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                CityTimeline newTimeline = CityTimeline.CreateNew( cityName, Guid.NewGuid().ToString(), 2 );
                newTimeline.ChildOfEndOfTimeObjectWithID = CreateOnItem.ItemID;
                newTimeline.CitySlotIndexUsedFromParent = CreateOnItemAtIndex;
                newTimeline.SeedStyle = dCityStyle.Selected;
                newTimeline.DoomType = dStartingConflicts.Selected;
                newTimeline.Origin = dYourOrigin.Selected;
                if ( !CreateOnItem.TrySetSubObjectSlotAsBeingInUse( newTimeline.CitySlotIndexUsedFromParent, newTimeline ) )
                    return;

                ResourceRefs.Aetagest.AlterCurrent( -100, "Expense_CalledNewTimeline" );
                FlagRefs.HasAlreadyIndicatedTheEndOfTimeCreatorMode.TripIfNeeded();

                if ( !WorldSaveLoad.SaveTimelineToFileOnMainThreadIfNotAlreadySaving() )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }

                if ( Engine_Universal.CheckIfErrorShouldBeIgnored() )
                    Engine_Universal.ResetErrorIgnoring();

                Engine_Universal.ClearAllTraceOfExistingGame( false ); //clear all of the city, but not the metagame
                Engine_HotM.SetGameMode( MainGameMode.Streets );
                Engine_HotM.GameStatus = MainGameStatus.Ingame;
                CityMap.WasMapLoadedFromSavegame = false;
                SimCommon.CurrentTimeline = newTimeline;

                //now generate the new city
                CommonPlanner.Instance.OnMainGameStarted();
                AbstractSimPlanner.Instance.OnMainGameStarted();
                VisPlanner.Instance.OnMainGameStarted();
                VisCentralData.IsWaitingToStartSimOnMapgen = true;
                Instance.Close( WindowCloseReason.UserDirectRequest );
            }

            public override void HandleMouseover()
            {
            }
        }

        #region GetIsNameAlreadyInUse
        public static bool GetIsNameAlreadyInUse( string PotentialName )
        {
            foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
            {
                if ( kv.Value.Name.EqualsCaseInvariant( PotentialName ) )
                    return true;
            }
            return false;
        }
        #endregion

        #region GenerateNewCityName
        public static string GenerateNewCityName( MersenneTwister Rand )
        {
            NamePoolType namePool = NamePoolTypeTable.Instance.GetRowByID( "City" );

            foreach ( Name name in namePool.NamesList.GetRandomStartEnumerable( Rand ) )
            {
                string potentialName = name.GetDisplayName();

                bool hadMatch = false;
                foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
                {
                    if ( kv.Value.Name.EqualsCaseInvariant( potentialName ) )
                    {
                        hadMatch = true;
                        break;
                    }
                }
                if ( !hadMatch )
                    return potentialName;
            }

            foreach ( Name name in namePool.NamesList.GetRandomStartEnumerable( Rand ) )
            {
                string potentialName = name.GetDisplayName();
                potentialName += " " + Rand.NextInclus( 2, 9 );

                bool hadMatch = false;
                foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
                {
                    if ( kv.Value.Name.EqualsCaseInvariant( potentialName ) )
                    {
                        hadMatch = true;
                        break;
                    }
                }
                if ( !hadMatch )
                    return potentialName;
            }
            return "Malkovich";
        }
        #endregion

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    bStart.TryToStart();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

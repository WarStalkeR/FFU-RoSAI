using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_NewProfile : ToggleableWindowController, IInputActionHandler
    {
        public static Window_NewProfile Instance;
		
		public Window_NewProfile()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if ( iCityName.Instance != null )
                iCityName.Instance.SetText( string.Empty );
        }

        #region tTitleText
        public class tTitleText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "ProfileNaming_NameWindowHeader" );
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

                Buffer.AddLang( "ProfileNaming_Body" );
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                switch ( TooltipLinkData[0] )
                {
                    case "Dessicated":
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NewProfile", TooltipLinkData[0] ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "BackgroundLore_Header" );
                            novel.Main.AddLang( "ProfileNaming_Body_DessicatedDetails", ColorTheme.NarrativeColor );
                        }
                        break;
                    case "Stars":
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NewProfile", TooltipLinkData[0] ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "BackgroundLore_Header" );
                            novel.Main.AddLang( "ProfileNaming_Body_StarsDetails", ColorTheme.NarrativeColor );
                        }
                        break;
                    case "Gridlocked":
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NewProfile", TooltipLinkData[0] ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "BackgroundLore_Header" );
                            novel.Main.AddLang( "ProfileNaming_Body_GridlockedDetails", ColorTheme.NarrativeColor );
                        }
                        break;
                    case "EnemyAndCanvas":
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NewProfile", TooltipLinkData[0] ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "BackgroundLore_Header" );
                            novel.Main.AddLang( "NewTimeline_Body_EnemyAndCanvasDetails", ColorTheme.NarrativeColor );
                        }
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "Window_NewProfile: Unknown link info '" + TooltipLinkData[0] + "'", Verbosity.ShowAsError );
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

                float extraOffsetY = 15;
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

            private const float HEIGHT_FROM_OTHER_BITS = 26f + 22.9f + -3f //text top, plus header plus buffer
                + 25.7f + 30f;//plus buttons plus more border

            private float lastWindowHeight = -1f;
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
                Buffer.AddLangAndAfterLineItemHeader( "ProfileNaming_NameHeader" );
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
                //use a whitelist of approved characters only
                if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                    return addedChar;
                if ( addedChar == '_' || addedChar == ' ' || addedChar == '-' )
                    return addedChar;
                //block everything except alphanumerics and _ and -
                //for right now
                return '\0';
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
                    this.SetText( string.Empty );
                }
            }
        }

        #region tStartType
        public class tStartType : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "Profile_StartType" );
            }
        }
        #endregion

        #region dStartType
        public class dStartType : DropdownAbstractBase
        {
            public static ProfileStartType Selected = null;

            public static dStartType Instance;
            public dStartType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                Selected = (ProfileStartType)Item.GetItem();

            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                ProfileStartType typeDataToSelect = Selected;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !ProfileStartTypeTable.Instance.ValidOptions.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        Selected = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && ProfileStartTypeTable.Instance.ValidOptions.Count > 0 )
                    typeDataToSelect = ProfileStartTypeTable.Instance.ValidOptions[0];
                #endregion

                if ( Selected == null && typeDataToSelect != null )
                    Selected = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (ProfileStartType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    Selected = null;
                }
                else if ( ProfileStartTypeTable.Instance.ValidOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < ProfileStartTypeTable.Instance.ValidOptions.Count; i++ )
                    {
                        ProfileStartType row = ProfileStartTypeTable.Instance.ValidOptions[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        ProfileStartType optionItemAsType = (ProfileStartType)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < ProfileStartTypeTable.Instance.ValidOptions.Count; i++ )
                    {
                        ProfileStartType row = ProfileStartTypeTable.Instance.ValidOptions[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }

            public override void HandleMouseover()
            {
                ProfileStartType typeDataToSelect = Selected;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( typeDataToSelect ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "Profile_StartType" );
                    novel.Main.AddLang( "Profile_StartType_Details" );

                    if ( typeDataToSelect != null )
                    {
                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "InfoWindow_Currently" ).AddRaw( typeDataToSelect.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        if ( !typeDataToSelect.GetDescription().IsEmpty() )
                            novel.FrameBody.AddRaw( typeDataToSelect.GetDescription(), ColorTheme.NarrativeColor ).Line();

                        if ( !typeDataToSelect.GetCanBeSelected() && !typeDataToSelect.CanBeSelectedIfInDemo && Engine_HotM.IsDemoVersion )
                            novel.FrameBody.AddRaw( typeDataToSelect.StrategyTip_IfBlockedByDemo.Text, ColorTheme.PurpleDim ).Line();
                        else
                            novel.FrameBody.AddRaw( typeDataToSelect.StrategyTip_Normal.Text, ColorTheme.PurpleDim ).Line();
                    }
                }
            }

            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                ProfileStartType ItemAsType = (ProfileStartType)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( ItemAsType ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "Profile_StartType" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();

                    if ( !ItemAsType.GetCanBeSelected() && !ItemAsType.CanBeSelectedIfInDemo && Engine_HotM.IsDemoVersion )
                        novel.Main.AddRaw( ItemAsType.StrategyTip_IfBlockedByDemo.Text, ColorTheme.PurpleDim ).Line();
                    else
                        novel.Main.AddRaw( ItemAsType.StrategyTip_Normal.Text, ColorTheme.PurpleDim ).Line();
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
                Buffer.AddLang( "NewProfile", ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToStart();
                return MouseHandlingResult.None;
            }

            public static void TryToStart()
            {
                string profileName = iCityName.Instance.GetText();
                if ( profileName.IsEmpty() )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "ProfileName_Required" ), LocalizedString.AddLang_New( "ProfileName_RequiredBody" ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }
                if ( !ArcenIO.IsValidFilename( profileName, false ) )
                {
                    profileName = ArcenIO.ConvertToValidFilename( profileName, false );
                    iCityName.Instance.SetText( profileName );
                }
                if ( GetIsNameAlreadyInUse( profileName ) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "ProfileName_AlreadyInUse" ), LocalizedString.AddFormat1_New( "ProfileName_AlreadyInUseBody", profileName ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                if ( !dStartType.Selected.GetCanBeSelected() )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }

                DoFinalStart( profileName );
            }

            private static void DoFinalStart( string profileName )
            {
                GameStartData startData = new GameStartData();
                startData.IsNewProfile = true;
                startData.ProfileName = profileName;
                startData.ProfileGUID = Guid.NewGuid().ToString();
                startData.CityName = Lang.Get( "FirstCityName" );
                startData.CityGUID = Guid.NewGuid().ToString();
                startData.StartType = dStartType.Selected != null ? dStartType.Selected : ProfileStartTypeTable.Instance.DefaultRow;
                ParticleSoundRefs.StartGame.DuringGame_PlaySoundOnlyAtCamera();

                Engine_Universal.ClearAllTraceOfExistingGame( true );
                VisPlanner.Instance.DoStartOfGameFromMainMenu_OnMainThread( startData );
                Instance.Close( WindowCloseReason.UserDirectRequest );
            }

            public override void HandleMouseover()
            {
            }
        }

        #region GetIsNameAlreadyInUse
        public static bool GetIsNameAlreadyInUse( string PotentialName )
        {
            string[] folderList = ArcenIO.GetDirectories( Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.WorldSaveFolder );
            for ( int i = 0; i < folderList.Length; i++ )
            {
                string existing = ArcenIO.GetFileNameWithoutExtension( folderList[i] );
                if ( existing.EqualsCaseInvariant( PotentialName ) )
                    return true;
            }
            return false;
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

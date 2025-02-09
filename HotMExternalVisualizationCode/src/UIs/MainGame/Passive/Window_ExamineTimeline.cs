using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ExamineTimeline : ToggleableWindowController, IInputActionHandler
    {
        public static CityTimeline TimelineToExamine;

        public static Window_ExamineTimeline Instance;
		
		public Window_ExamineTimeline()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if ( TimelineToExamine == null )
                Close( WindowCloseReason.ShowingRefused );

            if ( iCityName.Instance != null )
                iCityName.Instance.SetText( TimelineToExamine.Name );
            if ( iNotesToSelf.Instance != null )
                iNotesToSelf.Instance.SetText( TimelineToExamine.PlayerNotesToSelf );
        }

        #region tTitleText
        public class tTitleText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "ExamineTimeline_WindowHeader" );
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

                TimelineToExamine?.RenderExaminationLines( Buffer );
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                TimelineToExamine?.HandleExaminationHyperlinkHover( TooltipLinkData );
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
                    extraOffsetY = -175;

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
                    if ( bSave.Original != null )
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

            private const float HEIGHT_FROM_OTHER_BITS = 26f + 34f + 112.9f + -3f //text top, plus header plus buffer
                + 2f;//plus buttons plus more border

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
                Buffer.StartSize80().AddLangAndAfterLineItemHeader( "NewTimeline_CityNameHeader" );
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
                        if ( !EscapeWindowStackController.HandleCancel( WindowCloseReason.UserCasualRequest ) )
                            Window_ModalTextboxWindow.Instance.Close( WindowCloseReason.UserCasualRequest );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        bSave.TryToStart();
                        break;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
            }
        }

        #region tNotesToSelfHeader
        public class tNotesToSelfHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartSize80().AddLangAndAfterLineItemHeader( "ExamineTimeline_NotesToSelfHeader" );
            }
        }
        #endregion

        public class iNotesToSelf : InputAbstractBase
        {
            public int maxTextLength = 500;
            public static iNotesToSelf Instance;
            public iNotesToSelf() { Instance = this; }
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
                        if ( !EscapeWindowStackController.HandleCancel( WindowCloseReason.UserCasualRequest ) )
                            Window_ModalTextboxWindow.Instance.Close( WindowCloseReason.UserCasualRequest );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    //case "Return": //enter key
                    //    bSave.TryToStart();
                    //    break;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
            }
        }

        public class bDive : ButtonAbstractBase
        {
            public static bDive Original;
            public bDive() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                bool canDive = true;
                if ( TimelineToExamine == null || !TimelineToExamine.HasBeenSeeded || TimelineToExamine == SimCommon.CurrentTimeline )
                    canDive = false;

                Buffer.AddLang( "ExistingCity_Dive", canDive ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) : ColorTheme.RedOrange2 );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToDive();
                return MouseHandlingResult.None;
            }

            public static void TryToDive()
            {
                if ( !WorldSaveLoad.SaveTimelineToFileOnMainThreadIfNotAlreadySaving() )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }

                string fileName = SimMetagame.ProfileName + "/" + (TimelineToExamine?.GUID??"null");

                WorldSaveLoad.TryStartLoadTimelineFile( fileName );
                Instance.Close( WindowCloseReason.UserDirectRequest );
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
                                
                if ( TimelineToExamine == null )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Timeline", TimelineToExamine?.TimelineID ?? -44, 0 ), this.Element,
                        SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                    {
                        novel.TitleUpperLeft.AddLang( "CannotDive_MissingTimeline", ColorTheme.RedOrange2 );
                        novel.ShouldTooltipBeRed = true;
                    }
                }
                else if ( !TimelineToExamine.HasBeenSeeded )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Timeline", TimelineToExamine?.TimelineID ?? -44, 0 ), this.Element,
                        SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                    {
                        novel.TitleUpperLeft.AddLang( "CannotDive_NotSeeded", ColorTheme.RedOrange2 );
                        novel.ShouldTooltipBeRed = true;
                    }
                }
                else if ( TimelineToExamine == SimCommon.CurrentTimeline )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Timeline", TimelineToExamine?.TimelineID ?? -44, 0 ), this.Element,
                        SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                    {
                        novel.TitleUpperLeft.AddLang( "CannotDive_AlreadyThere", ColorTheme.RedOrange2 );
                        novel.ShouldTooltipBeRed = true;
                    }
                }
                else if ( TimelineToExamine.IsTimelineAFailure )
                {
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Timeline", TimelineToExamine?.TimelineID ?? -44, 0 ), this.Element,
                        SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                    {
                        novel.TitleUpperLeft.AddLang( "DiveWarning_FailedTimeline", ColorTheme.RedOrange2 );
                        novel.ShouldTooltipBeRed = true;
                    }
                }
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public static bSave Original;
            public bSave() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( LangCommon.Popup_Common_Save.Text, ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToStart();
                return MouseHandlingResult.None;
            }

            public static void TryToStart()
            {
                if ( TimelineToExamine == null )
                    return;

                string newCityName = iCityName.Instance.GetText();
                string newNotesToSelf = iNotesToSelf.Instance.GetText();

                if ( GetIsNameAlreadyInUse( newCityName ) )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                        LocalizedString.AddLang_New( "CityName_AlreadyInUse" ), LocalizedString.AddFormat1_New( "CityName_AlreadyInUseBody", newCityName ),
                        LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                TimelineToExamine.Name = newCityName;
                TimelineToExamine.PlayerNotesToSelf = newNotesToSelf;
                Instance.Close( WindowCloseReason.UserDirectRequest );
            }

            #region GetIsNameAlreadyInUse
            public static bool GetIsNameAlreadyInUse( string PotentialName )
            {
                foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
                {
                    if ( kv.Value.TimelineID == TimelineToExamine.TimelineID )
                        continue; //don't check ourselves
                    if ( kv.Value.Name.EqualsCaseInvariant( PotentialName ) )
                        return true;
                }
                return false;
            }
            #endregion

            public override void HandleMouseover()
            {
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    bSave.TryToStart();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ModalTextboxWindowWithRandomizer : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_ModalTextboxWindowWithRandomizer Instance;
        public Window_ModalTextboxWindowWithRandomizer()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
            this.ShouldBlurBackgroundGame = true;
        }

        private bool IsOpen;
        private NamedThing namedThing;
        private string headerText;
        private string labelText;
        private bool mustBeAlphaNumeric;
        public void Open( string HeaderText, string LabelText, bool MustBeAlphaNumeric, string StartingText, int maxTextLength, NamedThing Named )
        {
            this.IsOpen = true;
            iModalTextbox.Instance.SetText( StartingText );
            iModalTextbox.Instance.maxTextLength = maxTextLength;
            ((ArcenUI_Input)iModalTextbox.Instance.Element).Focus();
            this.namedThing = Named;
            this.headerText = HeaderText;
            this.labelText = LabelText;
            this.mustBeAlphaNumeric = MustBeAlphaNumeric;
			EscapeWindowStackController.RegisterShouldCloseOnEscape(this, true);
        }

        public override void Close( WindowCloseReason Reason )
        {
            this.IsOpen = false;
            EscapeWindowStackController.CloseWindowAndChildren(this, Reason );
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( !this.IsOpen )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            return true;
        }

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ModalTextboxWindowWithRandomizer.Instance != null )
                {
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                    }
                }

                float extraOffsetY = 0;
                if ( Engine_Universal.IsAnyTextboxFocused && Engine_Universal.IsSteamDeckVersion )
                    extraOffsetY = -200;
                this.WindowController.ExtraOffsetY = extraOffsetY;
            }
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
                if ( iModalTextbox.Instance != null )
                {
                    for ( int i = 0; i < 20; i++ )
                    {
                        string newName = Instance.namedThing.RandomNamePool.NamesList.GetRandom( Engine_Universal.PermanentQualityRandom )?.GetDisplayName();
                        if ( !newName.IsEmpty() && Instance.namedThing.GetIsValidName( newName ) )
                        {
                            iModalTextbox.Instance.SetText( newName );
                            break;
                        }
                    }
                }

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToSave();
                return MouseHandlingResult.None;
            }

            #region TryToSave
            public static void TryToSave()
            {
                string newText = iModalTextbox.Instance.GetText();
                if ( newText == null || newText.Length <= 0 )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddLang_New( "Popup_TextRequired" ),
                        LocalizedString.AddLang_New( "Popup_TextRequired_Explanation" ), LangCommon.Popup_Common_GoBack.LocalizedString );
                    return;
                }

                if ( !Instance.namedThing.GetIsValidName( newText ) )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }

                Instance.namedThing.SetChosenName( newText, true );

                Instance.Close( WindowCloseReason.UserDirectRequest );
            }
            #endregion

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Save, ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }
        }

        public class bExit : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }
        }

        public class bCancel : ButtonAbstractBase
        {
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

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Instance.headerText );
            }
            public override void OnUpdate() { }
        }

        public class tLabelText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Instance.labelText );
            }
            public override void OnUpdate() { }
        }

        public class iModalTextbox : InputAbstractBase
        {
            public int maxTextLength = 10;
            public static iModalTextbox Instance;
            public iModalTextbox() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                if ( Window_ModalTextboxWindowWithRandomizer.Instance.mustBeAlphaNumeric )
                {
                    //use a whitelist of approved characters only
                    if ( Char.IsLetterOrDigit( addedChar ) ) //must be alphanumeric
                        return addedChar;
                    if ( addedChar == '_' || addedChar == ' ' || addedChar == '-' )
                        return addedChar;
                    //block everything except alphanumerics and _ and -
                    //for right now
                    return '\0';
                }
                else
                    return addedChar;
            }
            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        if ( !EscapeWindowStackController.HandleCancel( WindowCloseReason.UserDirectRequest ) )
                            Window_ModalTextboxWindowWithRandomizer.Instance.Close( WindowCloseReason.UserDirectRequest );
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        bSave.TryToSave();
                        break;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    bSave.TryToSave();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }

        public delegate bool SaveDelegate( string NewText );
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Window_VirtualRealityNameWindow : ToggleableWindowController, IInputActionHandler
    {
        public static Window_VirtualRealityNameWindow Instance;
		
		public Window_VirtualRealityNameWindow()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
        }

        private static string GenerateNewVirtualRealityName()
        {
            NamePoolType namePool = NamePoolTypeTable.Instance.GetRowByID( "VirtualRealitySimulation" );
            return namePool.NamesList.GetRandom( Engine_Universal.PermanentQualityRandom ).GetDisplayName();
        }

        public override void OnOpen()
        {
            base.OnOpen();

            if ( iNetworkName.Instance != null )
            {
                if ( !SimMetagame.VREnvironmentName.IsEmpty() )
                    iNetworkName.Instance.SetText( SimMetagame.VREnvironmentName );
                else
                    iNetworkName.Instance.SetText( GenerateNewVirtualRealityName() );
            }
        }

        #region tTitleText
        public class tTitleText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !SimMetagame.VREnvironmentName.IsEmpty() )
                    Buffer.AddFormat1( "VirtualRealityRenaming_Header", SimMetagame.VREnvironmentName );
                else
                    Buffer.AddLang( "VirtualRealityNaming_NameWindowHeader" );
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

                if ( !SimMetagame.VREnvironmentName.IsEmpty() )
                    Buffer.AddLang( "VirtualRealityRenaming_Body" );
                else
                {
                    if ( UnlockRefs.ExpandedVRSimulation.DuringGameplay_IsInvented )
                        Buffer.AddLang( "VirtualRealityNaming_Expanded_Body" );
                    else
                        Buffer.AddLang( "VirtualRealityNaming_Early_Body" );
                }
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
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
                
                float extraOffsetY = 0;
                if ( Engine_Universal.IsAnyTextboxFocused && Engine_Universal.IsSteamDeckVersion )
                    extraOffsetY = -200;

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
                    if ( bCreate.Original != null )
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

            private const float HEIGHT_FROM_OTHER_BITS = 26f + 4f + -3f //text top, plus buffer
                + 25.7f + 30f;//plus buttons plus more border

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
                if ( iNetworkName.Instance != null )
                    iNetworkName.Instance.SetText( GenerateNewVirtualRealityName() );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
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

        #region tNetworkNameHeader
        public class tNetworkNameHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLangAndAfterLineItemHeader( "VirtualRealityNaming_NameHeader" );
            }
        }
        #endregion

        public class iNetworkName : InputAbstractBase
        {
            public int maxTextLength = 20;
            public static iNetworkName Instance;
            public iNetworkName() { Instance = this; }
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
                        bCreate.TryToStart();
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
                    if ( !SimMetagame.VREnvironmentName.IsEmpty() )
                        this.SetText( SimMetagame.VREnvironmentName );
                    else
                        this.SetText( GenerateNewVirtualRealityName() );
                }
            }
        }

        public class bCreate : ButtonAbstractBase
        {
            public static bCreate Original;
            public bCreate() { if ( Original == null ) Original = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Network_Create",  ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                TryToStart();
                return MouseHandlingResult.None;
            }

            public static void TryToStart()
            {
                string VRName = iNetworkName.Instance.GetText();
                if ( VRName.IsEmpty() )
                {
                    VRName = GenerateNewVirtualRealityName();
                    iNetworkName.Instance.SetText( VRName );

                }

                DoCreation( VRName );
            }

            private static void DoCreation( string VRName )
            {
                SimMetagame.VREnvironmentName = VRName;

                Instance.Close( WindowCloseReason.UserDirectRequest );
            }

            public override void HandleMouseover()
            {
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    bCreate.TryToStart();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

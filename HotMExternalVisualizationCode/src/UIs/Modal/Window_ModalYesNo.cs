﻿using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ModalYesNo : ToggleableWindowController, IInputActionHandler
    {
        public static Window_ModalYesNo Instance;
        public Window_ModalYesNo()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
        }
        public override void Close( WindowCloseReason Reason )
        {
	        base.Close( Reason );
	        Engine_Universal.CurrentPopups.SafeTryRemoveAt(0);
        }

		public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( InputActionTypeDataTable.GetActionByName_FairlySlow( "Return" ).CalculateIsSingleStartingPressed() )
                {
                    try
                    {
                        ModalPopupData popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                        Engine_Universal.CurrentPopups.SafeTryRemoveAt( 0 );
                        Instance.Close( WindowCloseReason.UserDirectRequest );
                        try
                        {
                            popupData?.OnYes?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
                }
            }
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            //if ( !base.GetShouldDrawThisFrame_Subclass() )
            //    return false;
            if ( Engine_Universal.CurrentPopups.Count <= 0 )
                return false;
            ModalPopupData data = Engine_Universal.CurrentPopups.FirstOrDefault;
            if ( data == null )
                return false;
            if ( data.Style != ModalPopupStyle.YesNo_Normal )
                return false;
            return true;
        }

        public override void ChildOnShowAfterNotShowing()
        {
            if ( tBodyText.bodyTextTransform )
            {
                UnityEngine.Vector3 pos = tBodyText.bodyTextTransform.localPosition;
                pos.y = 0;
                tBodyText.bodyTextTransform.localPosition = pos;
            }
        }

        public class bYes : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.YesButtonText??string.Empty, ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                try
                {
                    ModalPopupData popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                    Engine_Universal.CurrentPopups.SafeTryRemoveAt( 0 );
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    try
                    {
                        popupData?.OnYes?.Invoke();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                }
                return MouseHandlingResult.None;
            }
        }

        public class bNo : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.NoOrCloseButtonText??string.Empty, ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                try
                {
                    ModalPopupData popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                    Engine_Universal.CurrentPopups.SafeTryRemoveAt( 0 );
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    try
                    {
                        popupData?.OnNoOrClose?.Invoke();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "Error in clicking No button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                }
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : TextAbstractBase
        {
            public static UnityEngine.RectTransform bodyTextTransform = null;
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.BodyText??string.Empty, ColorTheme.NarrativeColor );
                Buffer.AddRaw( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }
            public override void OnUpdate()
            {
                if ( bodyTextTransform == null )
                    bodyTextTransform = this.Element.RelevantRect;

                ArcenUI_Text textElement = this.Element as ArcenUI_Text;
                if ( textElement )
                    textElement.FontScale = InputCaching.Scale_PopupWindowText;
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.HeaderText??string.Empty );
            }
            public override void OnUpdate() { }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {                
                case "Return":
                    try
                    {
                        ModalPopupData popupData = Engine_Universal.CurrentPopups.FirstOrDefault;
                        Engine_Universal.CurrentPopups.SafeTryRemoveAt( 0 );
                        Instance.Close( WindowCloseReason.UserDirectRequest );
                        try
                        {
                            popupData?.OnYes?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Error in clicking Yes button: " + e, Verbosity.ShowAsError );
                        }
                    }
                    catch
                    {
                        Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
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

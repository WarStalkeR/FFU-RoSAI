using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ModalOKTall_UltraWide : Window_ModalBaseOK, IInputActionHandler
    {
        public static Window_ModalOKTall_UltraWide Instance;
        public Window_ModalOKTall_UltraWide()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = true;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
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
                            popupData?.OnNoOrClose?.Invoke();
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Error in clicking OK button: " + e, Verbosity.ShowAsError );
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
            if ( data.Style != ModalPopupStyle.OkTallUltraWide )
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

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.NoOrCloseButtonText??string.Empty );
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
                        ArcenDebugging.LogSingleLine( "Error in clicking OK button: " + e, Verbosity.ShowAsError );
                    }
                }
                catch
                {
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                }
                return MouseHandlingResult.None;
            }
        }

        public class tBodyText : tBodyTextBase
        {
            public override float WidthForBodyText => 1000f; //20px spare room
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.CurrentPopups.FirstOrDefault?.HeaderText??string.Empty, ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
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
                        Engine_Universal.CurrentPopups.FirstOrDefault?.OnNoOrClose?.Invoke();
                        Engine_Universal.CurrentPopups.SafeTryRemoveAt( 0 );
                    }
                    catch { }
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

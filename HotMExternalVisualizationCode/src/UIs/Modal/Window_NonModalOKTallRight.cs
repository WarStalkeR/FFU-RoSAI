﻿using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_NonModalOKTallRight : Window_ModalBaseOK
    {
        public static Window_NonModalOKTallRight Instance;
        public Window_NonModalOKTallRight()
        {
            Instance = this;
            this.ShouldShowEvenWhenGUIHidden = true;
            this.PreventsNormalInputHandlers = false;
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
		}

        protected override void ClearPopupData()
		{
			ModalPopupData popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull(FromSide.Right, true);
            if ( popupData != null)
			    popupData.OnNoOrClose?.Invoke();
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
                        ModalPopupData popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull( FromSide.Right, true );
                        Instance.Close( WindowCloseReason.UserDirectRequest );
                        try
                        {
                            popupData.OnNoOrClose?.Invoke();
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
            ModalPopupData data = Engine_Universal.GetFirstPopupDataFromSideOrNull( FromSide.Right, false );
            if ( data == null )
                return false;
            //if ( data.Style != ModalPopupStyle.OkTallRight ) //we don't check this, it's based on the side we are on
            //    return false;
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
                Buffer.AddRaw( Engine_Universal.GetFirstPopupDataFromSideOrNull( FromSide.Right, false ).NoOrCloseButtonText, 
                    ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                DoClose();
                return MouseHandlingResult.None;
            }

            public static void DoClose()
            {
                try
                {
                    ModalPopupData popupData = Engine_Universal.GetFirstPopupDataFromSideOrNull( FromSide.Right, true );
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

        public class tBodyText : tBodyTextBase
        {
            public override bool UsesModal => false;
            public override bool UsesNonModalLeft => false;
            public override float WidthForBodyText => 340f; //20px spare room
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( Engine_Universal.GetFirstPopupDataFromSideOrNull( FromSide.Right, false ).HeaderText );
            }
            public override void OnUpdate() { }
        }
    }
}

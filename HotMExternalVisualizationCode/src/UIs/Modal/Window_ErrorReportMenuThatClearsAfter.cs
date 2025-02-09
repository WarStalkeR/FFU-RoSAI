using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ErrorReportMenuThatClearsAfter : WindowControllerAbstractBase
    {
        public static Window_ErrorReportMenuThatClearsAfter Instance;
        public Window_ErrorReportMenuThatClearsAfter()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.ShouldShowEvenWhenGUIHidden = true;
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;
            if ( Engine_Universal.LastErrorText.Length <= 0 )
                return false;
            if ( Engine_Universal.LastErrorText.Contains( "ThreadAbortException" ) ) //don't visibly show thread abort exceptions
            {
                Engine_Universal.LastErrorText = string.Empty;
                return false;
            }
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            //if ( Window_JoinMultiplayerGameByIPMenu.Instance.GetShouldDrawThisFrame() ) 
            //    return false;
            //if ( Window_JoinMultiplayerGameByListMenu.Instance.GetShouldDrawThisFrame() ) 
            //    return false;
            //if ( Window_ClientMultiplayerConnectionStatus.Instance.GetShouldDrawThisFrame() ) 
            //    return false;
            //if the correct other window is not open, then don't show this message
            return false;
        }

        public override void Close( WindowCloseReason Reason )
        {
            Engine_Universal.LastErrorText = string.Empty;
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Error_Header" );
            }
        }
        #endregion

        public class customParent : CustomUIAbstractBase
        {
            public override void OnUpdate()
            {
            }
        }

        public class bIgnore : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Ok );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //this clears the error count, since these were not problematic in the main
                ArcenDebugging.ErrorSinceStart = 0;

                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                try
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ErrorClearsAfter", "bIgnore" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.TitleUpperLeft.AddLang( "Error_Header" );
                        novel.Main.AddLang( "ErrorClearsAfter" );
                    }
                }
                catch { }
            }
        }

        public class tBodyText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                string textToShow = Engine_Universal.LastErrorText;
                if ( textToShow.Length > 5000 )
                    textToShow.Substring( 5000 );
                Buffer.AddRaw( textToShow );
                Buffer.AddRaw( "\n\n\n\n\n \n" ); //add extra spacing to prevent clipping
            }
            public override void OnUpdate() { }
        }
    }
}

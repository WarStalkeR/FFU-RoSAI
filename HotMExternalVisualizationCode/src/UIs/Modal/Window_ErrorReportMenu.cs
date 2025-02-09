using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ErrorReportMenu : WindowControllerAbstractBase
    {
        public static Window_ErrorReportMenu Instance;
        public Window_ErrorReportMenu()
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
            if ( this.IsPermanentlyClosed )
            {
                this.Close( WindowCloseReason.ShowingRefused );
                return false;
            }
            return true;
        }

        private bool IsPermanentlyClosed;

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

        public class bOpenLog : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Error_OpenLog" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //open the regular log
                System.Diagnostics.Process.Start( Engine_Universal.CurrentPlayerDataDirectory + Engine_Universal.GameLoop.LogFolder +
                    ArcenDebugging.GetDebugLogDestination( DebugLogDestination.GameNameLog ) );

                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                try
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Exception", "bOpenLog" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.TitleUpperLeft.AddLang( "Error_OpenLog" );
                        novel.Main.AddLang( "Error_OpenLog_Explanation" );
                    }
                }
                catch { }
            }
            public override void OnUpdateSub() { }
        }

        public class bIgnore : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Error_Ignore" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                try
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Exception", "bIgnore" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.TitleUpperLeft.AddLang( "Error_Ignore" );
                        novel.Main.AddLang( "Error_Ignore_Explanation" );
                    }
                }
                catch { }
            }
            public override void OnUpdateSub() { }
        }

        public class bIgnoreAndStopReporting : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Error_IgnoreAndStop" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.IsPermanentlyClosed = true;
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
            public override void HandleMouseover()
            {
                try
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Exception", "bIgnoreAndStopReporting" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.TitleUpperLeft.AddLang( "Error_IgnoreAndStop" );
                        novel.Main.AddLang( "Error_IgnoreAndStop_Explanation" );
                    }
                }
                catch { }
            }
            public override void OnUpdateSub() { }
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

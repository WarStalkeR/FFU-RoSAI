using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_WelcomeToEA : ToggleableWindowController, IInputActionHandler
    {
        public static Window_WelcomeToEA Instance;
        public Window_WelcomeToEA()
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
                if ( InputActionTypeDataTable.GetActionByName_FairlySlow( "Return" ).CalculateIsSingleStartingPressed() ||
                    InputActionTypeDataTable.GetActionByName_FairlySlow( "JumpToNextActorOrEndTurn" ).CalculateIsSingleStartingPressed() )
                    Instance.Close( WindowCloseReason.UserDirectRequest );
            }
        }

        public class bContinue : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "MainMenu_Continue", ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public class tBodyTextFixed : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "EarlyAccessMessage", ColorTheme.NarrativeColor );
            }
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "DemoEnd_Header" );
            }
            public override void OnUpdate() { }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "JumpToNextActorOrEndTurn":
                case "Return":
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
            }
        }
    }
}

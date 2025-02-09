using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_BasicLowerLeftNovelTooltip : WindowControllerAbstractBase
    {
        private static Window_BasicLowerLeftNovelTooltip _instance;
        public Window_BasicLowerLeftNovelTooltip()
        {
            _instance = this;
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;            
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return handler.GetShouldDrawThisFrame();
        }

        private static readonly BasicNovelTooltipHandler handler = new BasicNovelTooltipHandler();

        public static void ClearMyself()
        {
            handler.bun.ClearMyself();
        }

        public static void ClearAndInvalidateTimeLastSet()
        {
            handler.bun.ClearAndInvalidateTimeLastSet();
        }

        public static Int64 TotalSetCalls { get { return handler.bun.TotalSetCalls; } }

        public override void OnPerFrameForWindow( bool IsActiveWindow )
        {
            handler.bun.OnPerFrameForWindow();
        }

        public static void SetDataSourceInner_Scalable( IArcenUIElementForSizing BeAboveOrBelow, IUITooltipDataSource Data, 
            TooltipExtraText ExtraText, SideClamp SideClamp, TooltipExtraRules ExtraRules, TooltipID ToolID )
        {
            handler.bun.SetDataSourceInner_Scalable( BeAboveOrBelow, Data, ExtraText, SideClamp, ExtraRules, ToolID, InputCaching.Scale_GeneralTooltips );
        }

        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                handler.CPI = this;
                handler.bufferSource = LowerLeftBuffer.Instance;
            }

            public override void OnUpdate()
            {
            }

            public override void OnSetElement( ArcenUI_Element Element )
            {
                handler.bun.UpdateWindowReferences( (ArcenUI_CustomUI)Element, "LowerLeftBasicNovel", TooltipBundle.PositionStyle.LowerLeftCorner, true, 32730 );
            }
        }

        public class tUpperTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tUpperTitle_DoPerFrame( this, Text );
            }
        }

        public class tUpperTitleRight : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tUpperTitleRight_DoPerFrame( Text );
            }
        }

        public class tLowerTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tLowerTitle_DoPerFrame( this, Text );
            }
        }

        public class tLowerTitleRight : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tLowerTitleRight_DoPerFrame( Text );
            }
        }

        public class tFrameTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameTitle_DoPerFrame( this, Text );
            }
        }

        public class tFrameText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameText_DoPerFrame( this, Text );
            }
        }

        public class tMainText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tMainText_DoPerFrame( Text );
            }
        }

        public class tTooltipExpandNote : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tTooltipExpandNote_DoPerFrame( Text );
            }

            public override bool GetShouldBeHidden()
            {
                return handler.tTooltipExpandNote_GetShouldBeHidden();
            }
        }
    }
}

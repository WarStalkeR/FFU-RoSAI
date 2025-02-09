using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_SmallerAtMouseNovelTooltip : WindowControllerAbstractBase
    {
        private static Window_SmallerAtMouseNovelTooltip _instance;
        public Window_SmallerAtMouseNovelTooltip()
        {
            _instance = this;
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
            
		}

        #region Basics
        public override void Close( WindowCloseReason Reason )
        {

        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return handler.GetShouldDrawThisFrame();
        }

        private static readonly SmallerNovelTooltipHandler handler = new SmallerNovelTooltipHandler();

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
        #endregion

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
                handler.bufferSource = NovelTooltipBuffer.Instance;
            }

            public override void OnUpdate()
            {
            }

            public override void OnSetElement( ArcenUI_Element Element )
            {
                handler.bun.UpdateWindowReferences( (ArcenUI_CustomUI)Element, "AtMouseSmallerNovel", TooltipBundle.PositionStyle.AtMouseStandard, true, 32740 );
            }
        }

        public class tSoleTitleLeft : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tSoleTitleLeft_DoPerFrame( Text );
            }
        }

        public class tSoleTitleRight : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tSoleTitleRight_DoPerFrame( Text );
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
                handler.tFrameText_DoPerFrame( Text );
            }
        }

        public class tMainTextHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tMainTextHeader_DoPerFrame( Text );
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

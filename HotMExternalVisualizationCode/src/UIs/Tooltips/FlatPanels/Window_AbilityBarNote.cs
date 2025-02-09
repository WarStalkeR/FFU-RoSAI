using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{
    public class Window_AbilityBarNote : WindowControllerAbstractBase
    {
        private static Window_AbilityBarNote _instance;
        public Window_AbilityBarNote()
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
            if ( bPanel.Instance == null )
                return false;
            if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                return false;
            return bun.GetShouldDrawThisFrame_Normal();
        }

        private static readonly TooltipBundle bun = new TooltipBundle();

        public static void ClearMyself()
        {
            bun.ClearMyself();
        }

        public static void ClearAndInvalidateTimeLastSet()
        {
            bun.ClearAndInvalidateTimeLastSet();
        }

        public override void OnPerFrameForWindow( bool IsActiveWindow )
        {
            bun.OnPerFrameForWindow();
        }

        public class bPanel : ImageButtonAbstractBase
        {
            public static bPanel Instance;
            public bPanel() { Instance = this; }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup _SubImages, SubTextGroup _SubTexts )
            {
            }

            public override void HandleHyperlinkHover( IArcenUIElementForSizing HoveredElement, string[] LinkData )
            {
                VisAbilityBarNote.HandleHyperlinkHover( HoveredElement, LinkData );
            }

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] LinkData )
            {
                return VisAbilityBarNote.HandleHyperlinkClick( Input, LinkData );
            }

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = Element;
                bun.UpdateWindowReferences( (ArcenUI_ImageButton)Element, "AbilityBarNote", TooltipBundle.PositionStyle.AbilityBarNote, 12f, 12f, false, 32740 );
            }

            public void SetText( ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID )
            {
                if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                    return;

                bun.SetTextInner_BasicLoose( Buffer.GetStringAndResetForNextUpdate(), TooltipWidth.AbilityBarNote, ToolID );
            }
        }
    }
}

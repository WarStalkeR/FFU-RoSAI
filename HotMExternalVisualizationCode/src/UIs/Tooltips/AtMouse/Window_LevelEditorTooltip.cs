using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorTooltip : WindowControllerAbstractBase
    {
        private static Window_LevelEditorTooltip _instance;
        public Window_LevelEditorTooltip()
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

        public static Int64 TotalSetCalls { get { return bun.TotalSetCalls; } }

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

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = Element;
                bun.UpdateWindowReferences( (ArcenUI_ImageButton)Element, "LevelEditor", TooltipBundle.PositionStyle.AtMouseStandard, 15f, 15f, true, 32740 );
            }

            #region SetText Wrappers
            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, LocalizedString Text, TooltipWidth Width, TooltipID ToolID,
                SideClamp Clamp = SideClamp.Any, MinHeight MinimumHeight = MinHeight.Any )
            {
                bun.SetTextInner_Scalable( MustBeAboveOrBelow, Text.FinalText, Clamp, MinimumHeight, TooltipExtraRules.None, Width, ToolID );
            }

            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, ArcenDoubleCharacterBuffer Buffer, TooltipWidth Width, TooltipID ToolID,
                SideClamp Clamp = SideClamp.Any, MinHeight MinimumHeight = MinHeight.Any )
            {
                bun.SetTextInner_Scalable( MustBeAboveOrBelow, Buffer.GetStringAndResetForNextUpdate(), Clamp, MinimumHeight, TooltipExtraRules.None, Width, ToolID );
            }

            public void SetText( IArcenUIElementForSizing MustBeAboveOrBelow, ArcenDoubleCharacterBuffer Buffer, TooltipWidth Width, TooltipID ToolID,
                SideClamp Clamp, MinHeight MinimumHeight, TooltipExtraRules ExtraRules )
            {
                bun.SetTextInner_Scalable( MustBeAboveOrBelow, Buffer.GetStringAndResetForNextUpdate(), Clamp, MinimumHeight, ExtraRules, Width, ToolID );
            }
            #endregion
        }
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ArrowIndicatorDark : WindowControllerAbstractBase
    {
        private static Window_ArrowIndicatorDark _instance;
        public Window_ArrowIndicatorDark()
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
            return bun.GetShouldDrawThisFrame_Arrow();
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
                if ( this.Element != null )
                    TooltipShadowStyle.Arrow.ApplyTo( this.Element.RelatedShadows[0] );
            }

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                if ( SharedRenderManagerData.CurrentIndicator == Indicator.PointAtCountdown )
                {
                    SimCommon.PointAtThisCountdown = null;
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour3_TaskStack )
                {
                    FlagRefs.UITour3_TaskStack.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour2_RightHeader )
                {
                    FlagRefs.UITour2_RightHeader.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour1_LeftHeader )
                {
                    FlagRefs.UITour1_LeftHeader.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour6_UnitSidebar )
                {
                    FlagRefs.UITour6_UnitSidebar.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour4_RadialMenu )
                {
                    FlagRefs.UITour4_RadialMenu.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour5_Toast )
                {
                    FlagRefs.UITour5_Toast.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour7_AbilityBar )
                {
                    FlagRefs.UITour7_AbilityBar.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour1_Target )
                {
                    FlagRefs.DebateTour1_Target.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour2_Failures )
                {
                    FlagRefs.DebateTour2_Failures.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour3_ActiveSlot )
                {
                    FlagRefs.DebateTour3_ActiveSlot.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour4_Discard )
                {
                    FlagRefs.DebateTour4_Discard.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour5_Queue )
                {
                    FlagRefs.DebateTour5_Queue.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour6_Bonus )
                {
                    FlagRefs.DebateTour6_Bonus.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour7_Opponent )
                {
                    FlagRefs.DebateTour7_Opponent.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour8_FinalAdvice )
                {
                    FlagRefs.DebateTour8_FinalAdvice.TripIfNeeded();
                    ClearAndInvalidateTimeLastSet();
                }

                return base.HandleClick( input );
            }            

            public override void SetElement( ArcenUI_Element Element )
            {
                this.Element = Element;
                bun.UpdateWindowReferences( (ArcenUI_ImageButton)Element, "ArrowIndicatorDark", TooltipBundle.PositionStyle.Arrow, 28f, 28f, false, 32740 );
            }

            internal void SetText_OnlyFromImplementation( IArcenUIElementForSizing MustBeAnchoredTo, ArcenDoubleCharacterBuffer Buffer, TooltipWidth Width, 
                TooltipID ToolID, TooltipArrowSide ArrowSide )
            {
                bun.SetTextInner_Arrow( MustBeAnchoredTo, Buffer.GetStringAndResetForNextUpdate(), ArrowSide, Width, ToolID );
            }
        }
    }
}

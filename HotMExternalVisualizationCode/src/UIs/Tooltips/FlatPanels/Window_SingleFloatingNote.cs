using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{
    public class Window_SingleFloatingNote : WindowControllerAbstractBase
    {
        private static Window_SingleFloatingNote _instance;
        public Window_SingleFloatingNote()
        {
            _instance = this;
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
            
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

        #region GetOtherShouldShowCriteria
        public static bool GetOtherShouldShowCriteria()
        {
            if ( Engine_Universal.GameLoop == null )
                return false;
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return false;//only render in the main game

            if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                return false;
            if ( VisCurrent.IsInPhotoMode )
                return false;
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                return false;
            if ( Engine_HotM.IsBigBannerBeingShown && !VisCurrent.IsShowingActualEvent )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            //if ( !FlagRefs.HasBeenAskedAboutUITour.DuringGameplay_IsTripped )
            //    return false;
            //LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            //if ( lowerMode != null && lowerMode.HideLeftHeader )
            //    return false;
            if ( Window_SystemMenu.Instance.IsOpen )
                return false;
            //if ( Window_Debate.Instance.IsOpen )
            //    return false;

            return true;
        }
        #endregion

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( bPanel.Instance == null )
                return false;
            if ( !GetOtherShouldShowCriteria() )
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

        private static string currentText = string.Empty;
        private static float lastsUntil = 0f;
        private static TooltipWidth lastWidth;
        private static TooltipID lastTooltipID;
        private static int linesAdded = 0;

        public override void OnPerFrameForWindow( bool IsActiveWindow )
        {
            bun.OnPerFrameForWindow();

            if ( lastsUntil > 0 && lastsUntil > ArcenTime.AnyTimeSinceStartF && !currentText.IsEmpty() )
                bun.SetTextInner_BasicLoose( currentText, lastWidth, lastTooltipID );
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
                bun.UpdateWindowReferences( (ArcenUI_ImageButton)Element, "SingleFloatingNote", TooltipBundle.PositionStyle.FloatingUpwards, 12f, 12f, false, 32740 );
            }

            #region AddToExistingFloatingTextAtCurrentMousePosition
            public void AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString Text, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd, float OverridingTime )
            {
                if ( !GetOtherShouldShowCriteria() )
                    return;

                AddToExistingFloatingTextAtCurrentMousePositionInner( Text.FinalText, ToolID, Width, MaxLinesToAdd, OverridingTime );
            }

            public void AddToExistingFloatingTextAtCurrentMousePosition( ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd, float OverridingTime )
            {
                if ( !GetOtherShouldShowCriteria() )
                    return;

                AddToExistingFloatingTextAtCurrentMousePositionInner( Buffer.GetStringAndResetForNextUpdate(), ToolID, Width, MaxLinesToAdd, OverridingTime );
            }

            private void AddToExistingFloatingTextAtCurrentMousePositionInner( string FinalText, TooltipID ToolID, TooltipWidth Width, int MaxLinesToAdd, float OverridingTime )
            {
                bool setFreshPosition = false;
                if ( lastsUntil > ArcenTime.AnyTimeSinceStartF && !currentText.IsEmpty() && linesAdded < MaxLinesToAdd && lastTooltipID.GetIsIdentical( ToolID ) )
                {
                    currentText = currentText + "\n" + FinalText;
                    linesAdded++;
                }
                else
                {
                    currentText = FinalText;
                    linesAdded = 0;
                    setFreshPosition = true;
                }

                lastsUntil = ArcenTime.AnyTimeSinceStartF + OverridingTime;
                lastWidth = Width;
                lastTooltipID = ToolID;

                bun.SetTextInner_BasicLoose( currentText, Width, ToolID );
                if ( setFreshPosition )
                    bun.FloatingPosition = ArcenUI.Instance.guiCamera.ScreenToWorldPoint( new Vector3( ArcenInput.MouseScreenX,
                        ArcenInput.MouseScreenY + MathRefs.ScreenStartingOffsetY.FloatMin, ArcenUI.POSITION_Z ) );
                else
                    bun.FloatingPosition.y += MathRefs.ScreenAdditionalLineOffsetY.FloatMin;
            }
            #endregion

            //#region SetFloatingTextAtCurrentMousePosition
            //public void SetFloatingTextAtCurrentMousePosition( ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, TooltipWidth Width )
            //{
            //    if ( !GetOtherShouldShowCriteria() )
            //        return;

            //    currentText = Buffer.GetStringAndResetForNextUpdate();
            //    linesAdded = 0;
            //    lastsUntil = ArcenTime.AnyTimeSinceStartF + OverridingTime;
            //    lastWidth = Width;
            //    lastTooltipID = ToolID;

            //    bun.SetTextInner_BasicLoose( currentText, Width, ToolID );
            //    bun.FloatingPosition = ArcenInput.NormalMouseTooltipPosition;
            //}
            //#endregion
        }
    }
}

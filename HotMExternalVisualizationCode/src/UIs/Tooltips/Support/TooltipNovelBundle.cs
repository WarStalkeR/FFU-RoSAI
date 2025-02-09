using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using TMPro;

namespace Arcen.HotM.ExternalVis
{
    public class TooltipNovelBundle
    {
        public IUITooltipDataSource CurrentData = null;
        public IUITooltipDataSource LastDataActuallyDrawn = null;
        public TooltipExtraText CurrentExtraText = TooltipExtraText.None;
        public TooltipExtraText LastExtraText = TooltipExtraText.None;
        public TooltipID CurrentToolID;
        public float TimeLastSetFreshText;
        public float TimeLastSetAnyText;
        public bool hasSetCanvasOffset = false;
        public bool wantsToRenderNow = false;

        public SideClamp Clamp = SideClamp.Any;
        public float Scale = 1f;
        public Vector2 RenderedTextSize;
        public TooltipExtraRules lastExtraRules;
        public IArcenUIElementForSizing MustBeAboveOrBelow = null;
        public float lastObjectScale = -1f;

        private IArcenUIElementForSizing MustBeAnchoredTo = null;
        private TooltipBundle.PositionStyle positionStyle = TooltipBundle.PositionStyle.Arrow;

        public RectTransform SizingRect = null;

        #region Arrow Section Only
        public TooltipArrowSide lastArrowSide = TooltipArrowSide.Right - 1;
        public TooltipArrowSide nextArrowSide = TooltipArrowSide.Right - 1;

        private Vector2[] arrowAnchorPositions;
        private UnityEngine.UI.Image[] arrowImages;
        #endregion

        public Int64 TotalSetCalls = 0;

        #region GetShouldDrawThisFrame_Arrow
        public bool GetShouldDrawThisFrame_Arrow()
        {
            if ( wantsToRenderNow && ArcenTime.AnyTimeSinceStartF - TimeLastSetAnyText < 0.4f )
                return true;
            return false;
        }
        #endregion

        #region GetShouldDrawThisFrame_Normal
        public bool GetShouldDrawThisFrame_Normal()
        {
            if ( CurrentData == null )
                return false;

            if ( wantsToRenderNow && ArcenTime.AnyTimeSinceStartF - TimeLastSetAnyText < ArcenUI.TimespanAfterWhichTooltipsDisappear )
            {
                if ( InputCaching.ShouldShowHyperDetailedTooltips )
                {
                    if ( TooltipRefs.LowerLeftUnitStyleNovel.GetIsDirectlyDrawnThisFrameOrLastFrame() || TooltipRefs.AtMouseUnitStyleNovel.GetIsDirectlyDrawnThisFrameOrLastFrame() )
                    {
                        if ( this.TooltipType != TooltipRefs.LowerLeftUnitStyleNovel && this.TooltipType != TooltipRefs.AtMouseUnitStyleNovel )
                            return false;
                    }
                }

                return true;
            }
            return false;
        }
        #endregion

        #region ClearMyself
        public void ClearMyself()
        {
            CurrentData = null;
            CurrentExtraText = TooltipExtraText.None;
            wantsToRenderNow = false;
        }
        #endregion

        #region ClearAndInvalidateTimeLastSet
        public void ClearAndInvalidateTimeLastSet()
        {
            CurrentData = null;
            CurrentExtraText = TooltipExtraText.None;
            wantsToRenderNow = false;
            TimeLastSetFreshText = 0f;
            TimeLastSetAnyText = 0f;
        }
        #endregion

        public ArcenUI_Window Window;
        public ArcenUI_CustomUI MyElement;
        public UITooltipType TooltipType;

        public bool DisablesRaycastersAndSetHigh = false;
        public int HowHighToSet = 32767;

        #region UpdateWindowReferences
        public void UpdateWindowReferences( ArcenUI_CustomUI MyElement, string TooltipTypeID, TooltipBundle.PositionStyle PosStyle, 
            bool DisablesRaycastersAndSetHigh, int HowHighToSet )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                this.Window = MyElement.Window;
                this.MyElement = MyElement;
                this.MyElement.Window.MaxDeltaTimeBeforeUpdates = 0;

                this.positionStyle = PosStyle;

                this.DisablesRaycastersAndSetHigh = DisablesRaycastersAndSetHigh;
                this.HowHighToSet = HowHighToSet;

                this.TooltipType = UITooltipTypeTable.Instance.GetRowByID( TooltipTypeID );
                if ( this.TooltipType == null )
                    throw new Exception( "Null tooltip type passed in!" );

                if ( PosStyle == TooltipBundle.PositionStyle.Arrow )
                {
                    debugStage = 2100;
                    if ( arrowAnchorPositions == null && MyElement?.RelatedImages != null )
                    {
                        debugStage = 2200;
                        arrowImages = MyElement.RelatedImages;
                        arrowAnchorPositions = new Vector2[arrowImages.Length];
                        for ( int i = 0; i < arrowAnchorPositions.Length; i++ )
                        {
                            arrowAnchorPositions[i] = arrowImages[i].rectTransform.anchoredPosition;
                            //ArcenDebugging.LogSingleLine( "saved " + i + ": " + anchorPositions[i] + " " + arrowImages[i].rectTransform.localPosition, Verbosity.DoNotShow );
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "TooltipNovelBundle.UpdateWindowReferences", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region OnPerFrameForWindow
        public void OnPerFrameForWindow()
        {
            if ( Window == null )
                return; //not ready yet

            if ( CurrentData == null )
                wantsToRenderNow = false;
            else if ( TimeLastSetFreshText > 0 )
            {
                if ( !wantsToRenderNow )
                {
                    wantsToRenderNow = true;
                }

                if ( Window.GetIsConsideredActive() )
                {
                    if ( wantsToRenderNow )
                    {
                        UpdateDataSourceIfNeededAndVisible();
                        UpdatePosition();
                    }
                }
            }
            else
            {
                if ( Window.GetIsConsideredActive() )
                    UpdatePosition();
            }
        }
        #endregion

        #region UpdateDataSourceIfNeededAndVisible
        public void UpdateDataSourceIfNeededAndVisible()
        {
            IUITooltipDataSource nextData = CurrentData;
            if ( nextData == null )
            {
                ClearMyself();
                return;
            }

            try
            {
                float scale = Scale;
                if ( lastObjectScale != scale )
                {
                    lastObjectScale = scale;
                    MyElement.RelevantRect.transform.localScale = new Vector3( scale, scale, scale );
                }

                if ( LastDataActuallyDrawn != nextData || LastExtraText != CurrentExtraText )
                {
                    LastDataActuallyDrawn = nextData;
                    LastExtraText = CurrentExtraText;

                    if ( arrowImages != null )
                    {
                        if ( lastArrowSide != nextArrowSide )
                        {
                            if ( lastArrowSide >= 0 )
                                arrowImages[(int)lastArrowSide].enabled = false;
                            lastArrowSide = nextArrowSide;
                            UnityEngine.UI.Image arrowImage = arrowImages[(int)nextArrowSide];
                            arrowImage.enabled = true;
                            //ArcenDebugging.LogSingleLine( "restored " + (int)nextArrowSide + ": " + anchorPositions[(int)nextArrowSide] + " " + arrowImage.rectTransform.anchoredPosition + " vs " +
                            //    arrowImage.rectTransform.localPosition, Verbosity.DoNotShow );
                            arrowImage.rectTransform.anchoredPosition = arrowAnchorPositions[(int)nextArrowSide];
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogSingleLine( "Exception in UpdateTextIfNeededAndVisible for TooltipNovelBundle:" + e.ToString(), Verbosity.ShowAsError );
            }
        }
        #endregion

        #region UpdatePosition
        public void UpdatePosition()
        {
            if ( this.MyElement == null || MyElement.RelevantRect == null )
                return;
            if ( !hasSetCanvasOffset )
            {
                if ( this.MyElement != null && this.MyElement.Window != null )
                {
                    hasSetCanvasOffset = true;
                    if ( this.DisablesRaycastersAndSetHigh )
                    {
                        this.MyElement.Window.FadeInSpeed = 28f;
                        this.MyElement.Window.FadeOutSpeed = 18f;
                        this.MyElement.Window.DisableAllRaycasters(); //this prevents us from intercepting ourselves when hovering something
                        this.MyElement.Window.SetOverridingCanvasSortingOrder( this.HowHighToSet ); //as high as it will go, so this is always on top!
                    }
                }
            }

            this.MyElement.Window.IsAutomaticPositioningDisabled = true;

            Vector3 worldSpacePoint = ArcenInput.NormalMouseTooltipPosition;

            if ( SizingRect == null )
            {
                SizingRect = MyElement.RelevantRect;
                //SizingRect = MyElement.RelevantRect;
            }

            Vector2 sizeDelta = MyElement.RelevantRect.GetWorldSpaceSize();
            float width = sizeDelta.x;
            float height = sizeDelta.y;

            float maxXPixel = ArcenUI.Instance.world_BottomRight.x - width - 0.1f;
            float maxYPixel = ArcenUI.Instance.world_BottomRight.y + height;

            worldSpacePoint.x = MathA.Min( worldSpacePoint.x, maxXPixel );
            worldSpacePoint.y = MathA.Max( worldSpacePoint.y, maxYPixel );

            switch ( this.positionStyle )
            {
                case TooltipBundle.PositionStyle.Arrow:
                    #region Arrow
                    if ( MustBeAnchoredTo != null )
                    {
                        RectTransform rTran = MustBeAnchoredTo.GetRelevantRect();
                        if ( rTran )
                        {
                            bool hadError = false;
                            Vector2 minMaxY = Vector2.zero;
                            Vector2 minMaxX = Vector2.zero;
                            try
                            {
                                minMaxY = rTran.GetWorldSpaceMinYAndMaxY( 0 );
                                minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                            }
                            catch { hadError = true; }

                            if ( !hadError )
                            {
                                float arrowSize = 1.4f * ArcenUI.ratioFromScreenSize;
                                float padding = 0.4f;
                                float halfPadding = 0.2f;
                                switch ( lastArrowSide )
                                {
                                    case TooltipArrowSide.Right:
                                        worldSpacePoint.y = minMaxY.x + padding; //aligned with top
                                        worldSpacePoint.x = minMaxX.x - width - arrowSize; //moved to left
                                        break;
                                    case TooltipArrowSide.BottomRight:
                                        worldSpacePoint.y = minMaxY.x + height + arrowSize; //moved above
                                        worldSpacePoint.x = minMaxX.y - width - padding; //snapped to right

                                        maxXPixel -= padding;
                                        break;
                                    case TooltipArrowSide.BottomLeft:
                                        worldSpacePoint.y = minMaxY.x + height + arrowSize; //moved above
                                        worldSpacePoint.x = minMaxX.x; //snapped to left

                                        maxXPixel -= padding;
                                        break;
                                    case TooltipArrowSide.Left:
                                        worldSpacePoint.y = minMaxY.x + padding; //aligned with top
                                        worldSpacePoint.x = minMaxX.y + arrowSize; //moved to right
                                        break;
                                    case TooltipArrowSide.TopLeft:
                                        worldSpacePoint.y = minMaxY.y - arrowSize; //moved below
                                        worldSpacePoint.x = minMaxX.x; //snapped to left

                                        maxXPixel -= padding;
                                        break;
                                    case TooltipArrowSide.TopRight:
                                        worldSpacePoint.y = minMaxY.y - arrowSize; //moved below
                                        worldSpacePoint.x = minMaxX.y - width - halfPadding; //snapped to right

                                        maxXPixel -= padding;
                                        break;
                                }
                            }

                            float absoluteMaxY = ArcenUI.Instance.world_TopLeft.y;

                            //don't let it go off the top of the screen
                            if ( worldSpacePoint.y > ArcenUI.Instance.world_TopLeft.y )
                                worldSpacePoint.y = ArcenUI.Instance.world_TopLeft.y;

                            //don't let it go off the left of the screen
                            if ( worldSpacePoint.x < ArcenUI.Instance.world_TopLeft.x )
                                worldSpacePoint.x = ArcenUI.Instance.world_TopLeft.x;

                            //don't let it go off the right
                            if ( worldSpacePoint.x > maxXPixel )
                                worldSpacePoint.x = maxXPixel;

                            //don't let it go off the bottom
                            if ( worldSpacePoint.y < maxYPixel )
                                worldSpacePoint.y = maxYPixel;
                        }
                    }
                    #endregion
                    break;
                case TooltipBundle.PositionStyle.TopLeft:
                    #region TopLeft
                    {
                        float minXPixel = ArcenUI.Instance.world_BottomLeft.x;
                        float minYPixel = ArcenUI.Instance.world_TopRight.y;

                        worldSpacePoint.x = minXPixel + 0.14f;
                        worldSpacePoint.y = minYPixel - 0.12f; //positive is up on this part of the screen

                        if ( Engine_Universal.PrimaryIsLeft )
                        {
                            float minX = Window_Sidebar.GetMinXWhenOnLeft();

                            if ( worldSpacePoint.x < minX )
                                worldSpacePoint.x = minX;
                        }

                        worldSpacePoint.y -= Window_MainGameHeaderBarLeft.GetYHeightForOtherWindowOffsets(); //always bump down
                        worldSpacePoint.y -= Window_TechHeader.GetYHeightForOtherWindowOffsets(); //bump down if present

                        //good chance this will never  ever be an issue now:
                        //if ( Window_ActorSidebarStatsEquipment.GetTopYForOtherWindowOffsets() >= worldSpacePoint.y - sizeDelta.y )
                        //{
                        //    worldSpacePoint.x += Window_ActorSidebarStatsEquipment.GetXWidthForOtherWindowOffsets();
                        //}
                    }
                    #endregion
                    break;
                case TooltipBundle.PositionStyle.AbilityBarNote:
                    #region AbilityBarNote
                    {
                        worldSpacePoint.x = Window_AbilityFooterBar.GetMaxXForTooltips() - width - 0.05f; //  maxXPixel - 0.05f;
                        worldSpacePoint.y = maxYPixel + 0.05f;

                        //worldSpacePoint.x -= (Window_Sidebar.GetXWidthWhenOnRight() * 1.05f); //buffer as well
                        //if ( Window_RadialMenu.Instance.GetShouldDrawThisFrame() )
                        //    worldSpacePoint.x -= Window_RadialMenu.GetXWidth(); //move to the side of the radial menu
                        if ( Window_AbilityFooterBar.Instance.GetShouldDrawThisFrame() )
                            worldSpacePoint.y += Window_AbilityFooterBar.GetYHeightForOtherWindowOffsets(); //move above the ability footer bar
                    }
                    #endregion
                    break;
                case TooltipBundle.PositionStyle.NotePrimaryCorner:
                    #region NotePrimaryCorner
                    {
                        worldSpacePoint.x = maxXPixel;
                        worldSpacePoint.y = maxYPixel + 0.05f;

                        worldSpacePoint.x -= (Window_Sidebar.GetXWidthWhenOnRight() * 1.05f); //buffer as well
                        if ( Window_RadialMenu.Instance.GetShouldDrawThisFrame() )
                            worldSpacePoint.x -= ( Window_RadialMenu.GetXWidth() + 0.9f ); //move to the side of the radial menu
                        if ( Window_AbilityFooterBar.Instance.GetShouldDrawThisFrame() )
                            worldSpacePoint.y += Window_AbilityFooterBar.GetYHeight(); //move above the ability footer bar as well
                        if ( Window_CommandFooter.Instance.GetShouldDrawThisFrame() )
                            worldSpacePoint.y += Window_CommandFooter.GetYHeight(); //move above the command footer bar as well
                    }
                    #endregion
                    break;
                case TooltipBundle.PositionStyle.AtMouseStandard:
                    #region AtMouseStandard
                    {
                        if ( MustBeAboveOrBelow != null )
                        {
                            RectTransform rTran = MustBeAboveOrBelow.GetRelevantRect();
                            if ( rTran )
                            {
                                bool hadError = false;
                                Vector2 minMaxY = Vector2.zero;
                                Vector2 minMaxX = Vector2.zero;
                                try
                                {
                                    minMaxY = rTran.GetWorldSpaceMinYAndMaxY( 0 );
                                    minMaxX = rTran.GetWorldSpaceMinXAndMaxX( 0 );
                                }
                                catch { hadError = true; }
                                if ( !hadError )
                                {
                                    bool alignedToSideOnly = false;
                                    bool mustBeAbove = false;
                                    //bool mustBeBelow = false;
                                    switch ( this.Clamp )
                                    {
                                        case SideClamp.AboveOrBelow:
                                            break;
                                        case SideClamp.AboveOnly:
                                            mustBeAbove = true;
                                            break;
                                        case SideClamp.BelowOnly:
                                            //mustBeBelow = true;
                                            break;
                                        case SideClamp.LeftOrRight:
                                        case SideClamp.Any:
                                            {
                                                //if this is fairly narrow, then try to just move this to the side

                                                float rightOfUsIfPlacedToTheRightOfElement = minMaxX.y + sizeDelta.x;
                                                if ( rightOfUsIfPlacedToTheRightOfElement < ArcenUI.Instance.world_TopRight.x )
                                                {
                                                    //if there IS room to the right, then place us there:
                                                    worldSpacePoint.x = minMaxX.y;
                                                    worldSpacePoint.y = minMaxY.x; //and align our top with the top of this
                                                    alignedToSideOnly = true;
                                                }
                                                else
                                                {
                                                    float leftOfUsIfPlacedToLeftOfElement = minMaxX.x - sizeDelta.x;

                                                    if ( leftOfUsIfPlacedToLeftOfElement >= ArcenUI.Instance.world_TopLeft.x )
                                                    {
                                                        //if there IS room to the left, then place us there:
                                                        worldSpacePoint.x = leftOfUsIfPlacedToLeftOfElement;
                                                        worldSpacePoint.y = minMaxY.x; //and align our top with the top of this
                                                        alignedToSideOnly = true;
                                                    }
                                                }
                                            }
                                            break;
                                    }

                                    if ( !alignedToSideOnly )
                                    {
                                        float bottomOfUsIfPlacedBelowElement = minMaxY.y //"max y" is a smaller number, further down the screen
                                                - sizeDelta.y; //we must subtract the size, because we are going down not up

                                        if ( bottomOfUsIfPlacedBelowElement > ArcenUI.Instance.world_BottomRight.y 
                                            && !mustBeAbove ) //if must be above, ignore this
                                        {
                                            //if there IS room below, then place us there:
                                            worldSpacePoint.y = minMaxY.y; //the top of our tooltip goes at the bottom of our element we are aligning to
                                        }
                                        else
                                        {
                                            float topOfUsIfPlacedAboveElement = minMaxY.x //"min y" is a larger number, higher up the screen
                                                + sizeDelta.y; //we must add the size, because we are going up instead of down

                                            if ( topOfUsIfPlacedAboveElement < ArcenUI.Instance.world_TopRight.y )
                                            {
                                                //if there IS room above, then place us there:
                                                worldSpacePoint.y = topOfUsIfPlacedAboveElement;
                                            }
                                        }
                                    }

                                    worldSpacePoint.x = MathA.Min( worldSpacePoint.x, maxXPixel );
                                    worldSpacePoint.y = MathA.Max( worldSpacePoint.y, maxYPixel );
                                }
                                //example:
                                //minMaxY: (-6.8, -7.7) 
                                //sizeDelta.y 2.3862 
                                //world_BottomRight.y -8.007345
                                //world_TopRight.y 8.007345
                                //ArcenDebugging.ArcenDebugLogSingleLine( "minMaxY: " + minMaxY + " sizeDelta.y " + sizeDelta.y + " world_BottomRight.y " + ArcenUI.Instance.world_BottomRight.y +
                                //    " world_TopRight.y " + ArcenUI.Instance.world_TopRight.y, Verbosity.DoNotShow );
                            }
                        }

                        UIHelper.HandleTooltipExtraRules( lastExtraRules, sizeDelta, ref worldSpacePoint );
                    }
                    #endregion
                    break;
                case TooltipBundle.PositionStyle.LowerLeftCorner:
                    #region LowerLeftCorner
                    {
                        float minXPixel = ArcenUI.Instance.world_BottomLeft.x;

                        worldSpacePoint.x = minXPixel + 0.05f;
                        worldSpacePoint.y = maxYPixel + 0.05f;

                        if ( Window_ActorSidebarStatsLowerLeft.TryGetHeightForOtherWindowOffsets( out float Height ) )
                            worldSpacePoint.y += Height + 0.6f;

                        //keep it from going off the top
                        float minYPixel = ArcenUI.Instance.world_TopLeft.y;
                        worldSpacePoint.y = MathA.Min( worldSpacePoint.y, minYPixel );

                        worldSpacePoint.x += Window_BuildSidebar.GetXWidthForOtherWindowOffsets();

                    }
                    #endregion
                    break;
            }

            this.MyElement.Window.SetPositionIfNeeded( worldSpacePoint );

        }
        #endregion

        #region SetDataSourceInner_Scalable
        public void SetDataSourceInner_Scalable( IArcenUIElementForSizing BeAboveOrBelow, IUITooltipDataSource Data, TooltipExtraText ExtraText, 
            SideClamp SideClamp, TooltipExtraRules ExtraRules, TooltipID ToolID, float NewScale )
        {
            if ( Data == null )
            {
                ArcenDebugging.LogSingleLine( "IUITooltipDataSource Data was null!", Verbosity.ShowAsError );
                return;
            }
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return;
            if ( !InputCaching.ShowTooltips )
                return;

            this.TooltipType.SetLastFrameRendered();

            MustBeAboveOrBelow = BeAboveOrBelow;
            Clamp = SideClamp;
            TimeLastSetAnyText = ArcenTime.AnyTimeSinceStartF;
            TotalSetCalls++;
            TimeLastSetAnyText = ArcenTime.AnyTimeSinceStartF;
            Scale = NewScale;

            if ( CurrentToolID.GetIsIdentical( ToolID ) )
            {
                if ( Data == CurrentData )
                    return;
            }
            else
            {
                CurrentToolID = ToolID;
                wantsToRenderNow = false;
                LastDataActuallyDrawn = null;
                LastExtraText = TooltipExtraText.None;
                TimeLastSetFreshText = ArcenTime.AnyTimeSinceStartF;
            }
            lastExtraRules = ExtraRules;

            if ( TimeLastSetFreshText < 1 )
                TimeLastSetFreshText = 1;

            this.TooltipType.ClearOthersIfNeeded();

            CurrentData = Data;
            CurrentExtraText = ExtraText;
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;
using System.Linq.Dynamic;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public class Window_VRFooter : WindowControllerAbstractBase
    {
        public static Window_VRFooter Instance;
		
		public Window_VRFooter()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                return false;
            if ( Engine_HotM.CurrentLowerMode != CommonRefs.ZodiacPodScene )
                return false;
            if ( VisCurrent.IsInPhotoMode )
                return false; //do nothing, must exit photo mode first

            if ( VisCurrent.IsShowingChapterChange || SimMetagame.CurrentChapterNumber < 2 )
                return false; //don't show when in the chapter change notice

            if ( !(SimMetagame.CurrentChapter?.Meta_HasShownBanner??false) )
                return false; //don't show when not having yet shown the chapter change notice

            return true;
        }

        #region GetMaxXForTooltips
        public static float GetMaxXForTooltips()
        {
            if ( SizingRect == null )
                return 0;
            return SizingRect.GetWorldSpaceBottomLeftCorner().x;
        }
        #endregion

        #region GetMaxYForTooltips
        public static float GetMaxYForTooltips()
        {
            if ( SizingRect == null )
                return 0;
            return SizingRect.GetWorldSpaceTopRightCorner().y;
        }
        #endregion

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        private static RectTransform SizingRect = null;

        #region GetXWidth
        public static float GetXWidth()
        {
            if ( SizingRect == null )
                return 0;
            float width = SizingRect.GetWorldSpaceSize().x;
            width *= 1.05f; //add buffer 
            return width;
        }
        #endregion

        #region GetYHeight
        public static float GetYHeight()
        {
            if ( SizingRect == null )
                return 0;
            float width = SizingRect.GetWorldSpaceSize().y;
            width *= 1.05f; //add buffer 
            return width;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bCategoryIcon> bCategoryIconPool;
        private static ButtonAbstractBase.ButtonPool<bVRBasicIcon> bVRBasicIconPool;

        public const float CATEGORY_ICON_X_START = 9f;
        public const float CATEGORY_ICON_Y = -3.5f;
        public const float CATEGORY_ICON_SIZE = 46f;
        public const float CATEGORY_ICON_TEXT_SIZE = 42f;
        public const float CATEGORY_ICON_X_SPACING = 2f;

        public const float VR_ICON_Y_LAST_LINE = 11f;
        public const float VR_ICON_Y_LINE_INCREASE_PER = 36f + 2f;
        public const float VR_ICON_SIZE = 36f;
        public const float VR_ICON_X_SPACING = 2f;

        public const float EXTRA_WINDOW_OFFSET_WIDTH = -2f + CATEGORY_ICON_X_START;

        public const float DEFAULT_ACTION_COUNT = 6f;
        public const float WIDTH_ASIDE_FROM_CATEGORIES = 12f; //nothing much!
        public const float CENTER_OFFSET_AMOUNT_LINE_1 = -6f; //based on WIDTH_ASIDE_FROM_CATEGORIES at least partly?

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.1f * GameSettings.Current.GetFloat( "Scale_AbilityFooterBar" );

                float offsetFromRight = -Window_RadialMenu.Instance.GetRadialMenuCurrentWidth_Scaled();
                if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                    offsetFromRight -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetX = offsetFromRight;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bCategoryIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bCategoryIconPool = new ButtonAbstractBase.ButtonPool<bCategoryIcon>( bCategoryIcon.Original, 10, "bCategoryIcon" );
                        bVRBasicIconPool = new ButtonAbstractBase.ButtonPool<bVRBasicIcon>( bVRBasicIcon.Original, 30, "bVRBasicIcon" );
                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                this.UpdateIcons();
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bCategoryIconPool.Clear( 5 );
                bVRBasicIconPool.Clear( 5 );

                int categorySlots = SimCommon.ActiveVRModeCategories.Count;

                float sizeFromCategories = (CATEGORY_ICON_SIZE * categorySlots) + (CATEGORY_ICON_X_SPACING * (categorySlots - 1));
                float line1OffsetX = 0f;

                #region Draw Category Icons
                {
                    float currentX = CATEGORY_ICON_X_START;
                    int categoryIndex = 1;
                    foreach ( MachineVRModeCategory category in SimCommon.ActiveVRModeCategories.GetDisplayList() )
                    {
                        if ( DrawCategoryIcon( categoryIndex, category, ref currentX ) )
                        {
                            //if returned true, this was the selected category
                            //so offset the next line based off of where this button is
                            line1OffsetX = (sizeFromCategories * 0.5f) - (currentX - (CATEGORY_ICON_SIZE * 0.5f));
                        }
                        categoryIndex++;
                    }

                    currentX += CATEGORY_ICON_X_START; //same buffer on the other end
                }
                #endregion

                float widthForWindow = WIDTH_ASIDE_FROM_CATEGORIES + sizeFromCategories + EXTRA_WINDOW_OFFSET_WIDTH;

                if ( VRModeHandler.currentCategory != null )
                {
                    int maxItemsPerLineIfPossible = 12;
                    int remainingItemsToDraw = VRModeHandler.currentCategory.DuringGame_VisibleVRActions.Count;

                    int linesToDraw = 1;
                    int overallWidth = remainingItemsToDraw;
                    if ( overallWidth > maxItemsPerLineIfPossible )
                        overallWidth = maxItemsPerLineIfPossible;

                    {
                        int itemsToCount = remainingItemsToDraw - maxItemsPerLineIfPossible;
                        while ( itemsToCount > 0 )
                        {
                            linesToDraw++;
                            itemsToCount -= maxItemsPerLineIfPossible;
                        }
                    }

                    float centerXLine1 = widthForWindow / 2f;
                    float currentY = VR_ICON_Y_LAST_LINE + ((linesToDraw - 1 ) * VR_ICON_Y_LINE_INCREASE_PER);
                    float startingY = currentY;

                    int currentIndexActions = 0;

                    float minX = 999999;
                    float maxX = -999999;

                    while ( remainingItemsToDraw > 0 )
                    {
                        int itemsInThisLine = remainingItemsToDraw;
                        if ( itemsInThisLine > maxItemsPerLineIfPossible )
                            itemsInThisLine = maxItemsPerLineIfPossible;

                        float totalNeedWidth = itemsInThisLine * (VR_ICON_SIZE + VR_ICON_X_SPACING);
                        float currentX = centerXLine1 - line1OffsetX - (totalNeedWidth / 2f);
                        currentX += CENTER_OFFSET_AMOUNT_LINE_1;
                        //don't go off the left side, normally
                        if ( currentX < CATEGORY_ICON_X_START )
                            currentX = CATEGORY_ICON_X_START;
                        //but if we would go off the right side, then go off the left if we would have to
                        if ( currentX + totalNeedWidth > widthForWindow )
                            currentX = widthForWindow - totalNeedWidth;

                        if ( currentX < minX )
                            minX = currentX;
                        if ( currentX > maxX )
                            maxX = currentX;

                        for ( int i = currentIndexActions; i < VRModeHandler.currentCategory.DuringGame_VisibleVRActions.Count; i++ )
                        {
                            MachineVRModeAction action = VRModeHandler.currentCategory.DuringGame_VisibleVRActions.GetDisplayList()[i];
                            DrawVRIcon_Action( action, ref currentX, currentY );

                            if ( currentX < minX )
                                minX = currentX;
                            if ( currentX > maxX )
                                maxX = currentX;

                            currentIndexActions++;
                            itemsInThisLine--;
                            remainingItemsToDraw--;
                            if ( itemsInThisLine <= 0 )
                                break;
                        }

                        currentY -= VR_ICON_Y_LINE_INCREASE_PER;
                    }

                    this.SetRelatedImage0EnabledIfNeeded( true ); //turn off the upper bg
                    Vector3 bgPos = new Vector3( minX - 4f, startingY + 4f, 0f );
                    float bgWidth = ( maxX - minX ) + 8f -2f; //there is one extra spacing at the end
                    float bgHeight = ( (startingY + VR_ICON_SIZE ) - VR_ICON_Y_LAST_LINE ) + 8f;

                    bgPos.y -= ( bgHeight - VR_ICON_SIZE );

                    RectTransform bgRect = this.Element.RelatedTransforms[0];
                    if ( priorBGPos != bgPos )
                    {
                        priorBGPos = bgPos;
                        bgRect.anchoredPosition = bgPos;
                        //ArcenDebugging.LogSingleLine( "bgPos: " + bgPos + " startingY: " + startingY, Verbosity.DoNotShow );
                    }
                    if ( priorBGWidth != bgWidth )
                    {
                        priorBGWidth = bgWidth;
                        bgRect.UI_SetWidth( bgWidth );
                        //ArcenDebugging.LogSingleLine( "bgHeight: " + bgHeight, Verbosity.DoNotShow );
                    }
                    if ( priorBGHeight != bgHeight )
                    {
                        priorBGHeight = bgHeight;
                        bgRect.UI_SetHeight( bgHeight );
                        //ArcenDebugging.LogSingleLine( "bgWidth: " + bgWidth, Verbosity.DoNotShow );
                    }

                }
                else //no category at the moment!
                {
                    this.SetRelatedImage0EnabledIfNeeded( false ); //turn off the upper bg
                }

                #region Expand or Shrink Width Of This Window
                if ( lastWindowWidth != widthForWindow )
                {
                    lastWindowWidth = widthForWindow;
                    SizingRect = this.Element.RelevantRect;
                    SizingRect.anchorMin = new Vector2( 0.5f, 0.5f );
                    SizingRect.anchorMax = new Vector2( 0.5f, 0.5f );
                    SizingRect.pivot = new Vector2( 0.5f, 0.5f );
                    SizingRect.UI_SetWidth( widthForWindow );
                }
                #endregion
            }
            private float lastWindowWidth = -1f;

            private Vector3 priorBGPos = Vector3.zero;
            private float priorBGWidth = 0f;
            private float priorBGHeight = 0f;

            #region DrawCategoryIcon
            private bool DrawCategoryIcon( int CategoryIndex, MachineVRModeCategory category, ref float currentX )
            {
                bCategoryIcon icon = bCategoryIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float iconY = CATEGORY_ICON_Y;
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, CATEGORY_ICON_SIZE, CATEGORY_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( category == null )
                            return;

                        bool isPerformable = category.GetShouldBeEnabled();
                        bool isSelectedCategory = VRModeHandler.currentCategory == category && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddRaw( category.ShortName.Text, isPerformable ? (isSelectedCategory ? "FFE5CA" : string.Empty) : "b8e594" );

                                    if ( isSelectedCategory )
                                    {
                                        icon.SetRelatedImage0EnabledIfNeeded( false );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage1SpriteIfNeeded( category.Icon.GetSpriteForUI() );

                                        icon.SetRelatedImage2EnabledIfNeeded( true );
                                        icon.SetRelatedImage3SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0EnabledIfNeeded( true );
                                        icon.SetRelatedImage0SpriteIfNeeded( category.Icon.GetSpriteForUI() );
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "b8e594" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2EnabledIfNeeded( false );
                                        icon.SetRelatedImage3SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.GetOtherTextToShowFromVolatile:
                                {
                                    switch ( ExtraData.Int )
                                    {
                                        case 0:
                                            ExtraData.Buffer.AddRaw( CategoryIndex.ToString(), isPerformable ? (isSelectedCategory ? "CB685C" : "b8e594") : "434266" );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( category != null && VRModeHandler.currentCategory != category )
                                        category.RenderCategoryTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.None ); //MustBeToLeftOfCommandBar
                                }
                                break;
                            case UIAction.OnClick:
                                if ( category != null )
                                {
                                    if ( !category.GetShouldBeEnabled() )
                                        break;

                                    if ( VRModeHandler.currentCategory == category )
                                        VRModeHandler.currentCategory = null;
                                    else
                                    {
                                        VRModeHandler.currentCategory = category;
                                        //this was super confusing when it remembered!
                                        //Engine_HotM.CurrentVRModeActionTargeting = null;
                                        //category.NonSim_TargetingDeployableNPCType = null;
                                        //category.NonSim_TargetingDeployableMachineUnitType = null;
                                        //category.NonSim_TargetingDeployableMachineVehicleType = null;
                                        TooltipRefs.AtMouseTag.ClearAllImmediately();
                                    }
                                }
                                break;
                        }
                    } );

                    currentX += CATEGORY_ICON_SIZE + CATEGORY_ICON_X_SPACING;
                }
                return VRModeHandler.currentCategory == category && category.GetShouldBeEnabled();
            }
            #endregion DrawCategoryIcon

            #region DrawVRIcon_Action
            private void DrawVRIcon_Action( MachineVRModeAction action, ref float currentX, float iconY )
            {
                bVRBasicIcon icon = bVRBasicIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, VR_ICON_SIZE, VR_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = action.GetCanAfford();

                        switch ( Action )
                        {
                            case UIAction.ButtonOnUpdate:
                                {
                                    if ( isPerformable && element.LastHadMouseWithin )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );

                                        icon.SetRelatedImage0EnabledIfNeeded( false );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage1SpriteIfNeeded( action.Icon.GetSpriteForUI() );

                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "b8e594" );

                                        icon.SetRelatedImage0EnabledIfNeeded( true );
                                        icon.SetRelatedImage0SpriteIfNeeded( action.Icon.GetSpriteForUI() );
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? action.UIColorHex : "b8e594" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( action != null )
                                    {
                                        action.RenderVRActionTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.None );

                                        VRModeHandler.LastHoveredAction = action;
                                        VRModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                if ( action != null )
                                {
                                    if ( !action.GetCanAfford() )
                                        break;

                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        action.DoMenuClick();
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        action.DoAlternativeMenuClick();
                                        break;
                                    }
                                    else
                                        break;

                                }
                                break;
                        }
                    } );

                    currentX += VR_ICON_SIZE + VR_ICON_X_SPACING;
                }
            }
            #endregion DrawVRIcon_Structure
        }

        #region bCategoryIcon
        public class bCategoryIcon : ButtonAbstractBaseWithImage
        {
            public static bCategoryIcon Original;
            public bCategoryIcon() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion

        #region bVRBasicIcon
        public class bVRBasicIcon : ButtonAbstractBaseWithImage
        {
            public static bVRBasicIcon Original;
            public bVRBasicIcon() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion
    }
}

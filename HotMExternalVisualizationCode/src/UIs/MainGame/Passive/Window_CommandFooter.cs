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
    public class Window_CommandFooter : WindowControllerAbstractBase
    {
        public static Window_CommandFooter Instance;
		
		public Window_CommandFooter()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.SelectedMachineActionMode == null ||
                Engine_HotM.SelectedMachineActionMode?.ID != "CommandMode" )
                return false;
            
            if ( VisCurrent.IsShowingActualEvent )
                return false; //don't show when in a city event

            if ( Window_MainGameHeaderBarLeft.Instance == null )
                return false;
            if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                return false; //if the header bar is not showing for whatever reason, then also don't show ourselves
            if ( SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false )
                return false;
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }
            if ( Engine_HotM.CurrentLowerMode != null )
                return false;
            if ( SimCommon.CurrentSimpleChoice != null )
                return false;
            if ( Window_RewardWindow.Instance?.IsOpen ?? false )
                return false;
            if ( Window_NetworkNameWindow.Instance?.IsOpen ?? false )
                return false;
            if ( !Window_RadialMenu.Instance?.GetShouldDrawThisFrame() ?? false )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( Window_Debate.Instance?.IsOpen ?? false )
                return false;
            if ( SimCommon.ShouldBeShowingPostGoalScreen )
                return false;

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
        private static ButtonAbstractBase.ButtonPool<bCommandBasicIcon> bCommandBasicIconPool;
        private static ButtonAbstractBase.ButtonPool<bCommandUnitIcon> bCommandUnitIconPool;

        public const float CATEGORY_ICON_X_START = 9f;
        public const float CATEGORY_ICON_Y = -3.5f;
        public const float CATEGORY_ICON_SIZE = 46f;
        public const float CATEGORY_ICON_TEXT_SIZE = 42f;
        public const float CATEGORY_ICON_X_SPACING = 2f;

        public const float COMMAND_ICON_Y_LAST_LINE = 11f;
        public const float COMMAND_ICON_Y_LINE_INCREASE_PER = 36f + 2f;
        public const float COMMAND_ICON_SIZE = 36f;
        public const float COMMAND_ICON_X_SPACING = 2f;

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
                        bCommandBasicIconPool = new ButtonAbstractBase.ButtonPool<bCommandBasicIcon>( bCommandBasicIcon.Original, 30, "bCommandBasicIcon" );
                        bCommandUnitIconPool = new ButtonAbstractBase.ButtonPool<bCommandUnitIcon>( bCommandUnitIcon.Original, 30, "bCommandUnitIcon" );
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
                bCommandBasicIconPool.Clear( 5 );
                bCommandUnitIconPool.Clear( 5 );

                int categorySlots = SimCommon.ActiveCommandModeCategories.Count;

                float sizeFromCategories = (CATEGORY_ICON_SIZE * categorySlots) + (CATEGORY_ICON_X_SPACING * (categorySlots - 1));
                float line1OffsetX = 0f;

                #region Draw Category Icons
                {
                    float currentX = CATEGORY_ICON_X_START;
                    int categoryIndex = 1;
                    foreach ( MachineCommandModeCategory category in SimCommon.ActiveCommandModeCategories.GetDisplayList() )
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

                if ( CommandModeHandler.currentCategory != null )
                {
                    int maxItemsPerLineIfPossible = 12;
                    int remainingItemsToDraw = CommandModeHandler.currentCategory.DuringGame_VisibleCommandActions.Count +
                        CommandModeHandler.currentCategory.DuringGame_VisibleDeployableNPCTypes.Count +
                        CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineUnitTypes.Count +
                        CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineVehicleTypes.Count;

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
                    float currentY = COMMAND_ICON_Y_LAST_LINE + ((linesToDraw - 1 ) * COMMAND_ICON_Y_LINE_INCREASE_PER);
                    float startingY = currentY;

                    int currentIndexActions = 0;
                    int currentIndexDeployableNPCTypes = 0;
                    int currentIndexDeployableMachineUnitTypes = 0;
                    int currentIndexDeployableMachineVehicleTypes = 0;

                    float minX = 999999;
                    float maxX = -999999;

                    while ( remainingItemsToDraw > 0 )
                    {
                        int itemsInThisLine = remainingItemsToDraw;
                        if ( itemsInThisLine > maxItemsPerLineIfPossible )
                            itemsInThisLine = maxItemsPerLineIfPossible;

                        float totalNeedWidth = itemsInThisLine * (COMMAND_ICON_SIZE + COMMAND_ICON_X_SPACING);
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

                        for ( int i = currentIndexActions; i < CommandModeHandler.currentCategory.DuringGame_VisibleCommandActions.Count; i++ )
                        {
                            MachineCommandModeAction action = CommandModeHandler.currentCategory.DuringGame_VisibleCommandActions.GetDisplayList()[i];
                            DrawCommandIcon_Action( action, ref currentX, currentY );

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

                        for ( int i = currentIndexDeployableNPCTypes; i < CommandModeHandler.currentCategory.DuringGame_VisibleDeployableNPCTypes.Count; i++ )
                        {
                            NPCUnitType npc = CommandModeHandler.currentCategory.DuringGame_VisibleDeployableNPCTypes.GetDisplayList()[i];
                            DrawCommandIcon_DeployableNPC( npc, ref currentX, currentY );

                            if ( currentX < minX )
                                minX = currentX;
                            if ( currentX > maxX )
                                maxX = currentX;

                            currentIndexDeployableNPCTypes++;
                            itemsInThisLine--;
                            remainingItemsToDraw--;
                            if ( itemsInThisLine <= 0 )
                                break;
                        }

                        for ( int i = currentIndexDeployableMachineUnitTypes; i < CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineUnitTypes.Count; i++ )
                        {
                            MachineUnitType unit = CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineUnitTypes.GetDisplayList()[i];
                            DrawCommandIcon_DeployableUnit( unit, ref currentX, currentY );

                            if ( currentX < minX )
                                minX = currentX;
                            if ( currentX > maxX )
                                maxX = currentX;

                            currentIndexDeployableMachineUnitTypes++;
                            itemsInThisLine--;
                            remainingItemsToDraw--;
                            if ( itemsInThisLine <= 0 )
                                break;
                        }

                        for ( int i = currentIndexDeployableMachineVehicleTypes; i < CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineVehicleTypes.Count; i++ )
                        {
                            MachineVehicleType vehicle = CommandModeHandler.currentCategory.DuringGame_VisibleDeployableMachineVehicleTypes.GetDisplayList()[i];
                            DrawCommandIcon_DeployableVehicle( vehicle, ref currentX, currentY );

                            if ( currentX < minX )
                                minX = currentX;
                            if ( currentX > maxX )
                                maxX = currentX;

                            currentIndexDeployableMachineVehicleTypes++;
                            itemsInThisLine--;
                            remainingItemsToDraw--;
                            if ( itemsInThisLine <= 0 )
                                break;
                        }

                        currentY -= COMMAND_ICON_Y_LINE_INCREASE_PER;
                    }

                    this.SetRelatedImage0EnabledIfNeeded( true ); //turn off the upper bg
                    Vector3 bgPos = new Vector3( minX - 4f, startingY + 4f, 0f );
                    float bgWidth = ( maxX - minX ) + 8f -2f; //there is one extra spacing at the end
                    float bgHeight = ( (startingY + COMMAND_ICON_SIZE ) - COMMAND_ICON_Y_LAST_LINE ) + 8f;

                    bgPos.y -= ( bgHeight - COMMAND_ICON_SIZE );

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
                    SizingRect.anchorMin = new Vector2( 1f, 0.5f );
                    SizingRect.anchorMax = new Vector2( 1f, 0.5f );
                    SizingRect.pivot = new Vector2( 1f, 0.5f );
                    SizingRect.UI_SetWidth( widthForWindow );
                }
                #endregion
            }
            private float lastWindowWidth = -1f;

            private Vector3 priorBGPos = Vector3.zero;
            private float priorBGWidth = 0f;
            private float priorBGHeight = 0f;

            #region DrawCategoryIcon
            private bool DrawCategoryIcon( int CategoryIndex, MachineCommandModeCategory category, ref float currentX )
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
                        bool isSelectedCategory = CommandModeHandler.currentCategory == category && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddRaw( category.ShortName.Text, isPerformable ? (isSelectedCategory ? "FFE5CA" : string.Empty) : "9694E5" );

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
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "9694E5" );

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
                                            ExtraData.Buffer.AddRaw( CategoryIndex.ToString(), isPerformable ? (isSelectedCategory ? "CB685C" : "9694E5") : "434266" );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( category != null && CommandModeHandler.currentCategory != category )
                                        category.RenderCategoryTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.None ); //MustBeToLeftOfCommandBar
                                }
                                break;
                            case UIAction.OnClick:
                                if ( category != null )
                                {
                                    if ( !category.GetShouldBeEnabled() )
                                        break;

                                    if ( CommandModeHandler.currentCategory == category )
                                        CommandModeHandler.currentCategory = null;
                                    else
                                    {
                                        CommandModeHandler.currentCategory = category;
                                        //this was super confusing when it remembered!
                                        Engine_HotM.CurrentCommandModeActionTargeting = null;
                                        category.NonSim_TargetingDeployableNPCType = null;
                                        category.NonSim_TargetingDeployableMachineUnitType = null;
                                        category.NonSim_TargetingDeployableMachineVehicleType = null;
                                        TooltipRefs.AtMouseTag.ClearAllImmediately();
                                    }
                                }
                                break;
                        }
                    } );

                    currentX += CATEGORY_ICON_SIZE + CATEGORY_ICON_X_SPACING;
                }
                return CommandModeHandler.currentCategory == category && category.GetShouldBeEnabled();
            }
            #endregion DrawCategoryIcon

            #region DrawCommandIcon_Action
            private void DrawCommandIcon_Action( MachineCommandModeAction action, ref float currentX, float iconY )
            {
                bCommandBasicIcon icon = bCommandBasicIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, COMMAND_ICON_SIZE, COMMAND_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = action.GetCanAfford();
                        bool isSelected = Engine_HotM.CurrentCommandModeActionTargeting == action && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.ButtonOnUpdate:
                                {
                                    if ( isSelected )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );

                                        icon.SetRelatedImage0EnabledIfNeeded( false );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage1SpriteIfNeeded( action.Icon.GetSpriteForUI() );

                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "9694E5" );

                                        icon.SetRelatedImage0EnabledIfNeeded( true );
                                        icon.SetRelatedImage0SpriteIfNeeded( action.Icon.GetSpriteForUI() );
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? action.UIColorHex : "9694E5" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( action != null )
                                    {
                                        action.RenderCommandActionTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.None );

                                        CommandModeHandler.LastHoveredDeployableNPC = null;
                                        CommandModeHandler.LastHoveredDeployableMachineUnitType = null;
                                        CommandModeHandler.LastHoveredDeployableMachineVehicleType = null;
                                        CommandModeHandler.LastHoveredAction = action;
                                        CommandModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
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
                                        action.CommandModeCategory.NonSim_TargetingDeployableNPCType = null;
                                        action.CommandModeCategory.NonSim_TargetingDeployableMachineUnitType = null;
                                        action.CommandModeCategory.NonSim_TargetingDeployableMachineVehicleType = null;
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

                    currentX += COMMAND_ICON_SIZE + COMMAND_ICON_X_SPACING;
                }
            }
            #endregion DrawCommandIcon_Structure

            #region DrawCommandIcon_DeployableNPC
            private void DrawCommandIcon_DeployableNPC( NPCUnitType npc, ref float currentX, float iconY )
            {
                bCommandUnitIcon icon = bCommandUnitIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, COMMAND_ICON_SIZE, COMMAND_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = npc.CalculateMaxCouldCreateAsBulkAndroid() > 0;
                        bool isSelected = CommandModeHandler.currentCategory?.NonSim_TargetingDeployableNPCType == npc && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.ButtonOnUpdate:
                                {
                                    icon.SetRelatedImage0SpriteIfNeeded( npc.GetDefaultTooltipIcon().GetSpriteForUI() );

                                    if ( isSelected )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "9694E5" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( npc != null )
                                    {
                                        npc.RenderNPCUnitTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForDeploying, TooltipExtraText.None, TooltipExtraRules.None );

                                        CommandModeHandler.LastHoveredDeployableNPC = npc;
                                        CommandModeHandler.LastHoveredDeployableMachineUnitType = null;
                                        CommandModeHandler.LastHoveredDeployableMachineVehicleType = null;
                                        CommandModeHandler.LastHoveredAction = null;
                                        CommandModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                if ( npc != null )
                                {
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        if ( npc.CalculateMaxCouldCreateAsBulkAndroid() <= 0 )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        npc.CommandModeCategoryOptional.NonSim_TargetingDeployableMachineUnitType = null;
                                        npc.CommandModeCategoryOptional.NonSim_TargetingDeployableMachineVehicleType = null;

                                        Engine_HotM.CurrentCommandModeActionTargeting = null;
                                        if ( npc.CommandModeCategoryOptional.NonSim_TargetingDeployableNPCType == npc )
                                            npc.CommandModeCategoryOptional.NonSim_TargetingDeployableNPCType = null;
                                        else
                                            npc.CommandModeCategoryOptional.NonSim_TargetingDeployableNPCType = npc;
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        SimCommon.CycleThroughNPCUnits( true, ( ISimNPCUnit u ) => u.UnitType == npc );
                                    }
                                }
                                break;
                        }
                    } );

                    currentX += COMMAND_ICON_SIZE + COMMAND_ICON_X_SPACING;
                }
            }
            #endregion DrawCommandIcon_Job

            #region DrawCommandIcon_DeployableUnit
            private void DrawCommandIcon_DeployableUnit( MachineUnitType unit, ref float currentX, float iconY )
            {
                bCommandUnitIcon icon = bCommandUnitIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, COMMAND_ICON_SIZE, COMMAND_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = unit.CalculateCanAfford();
                        bool isSelected = CommandModeHandler.currentCategory?.NonSim_TargetingDeployableMachineUnitType == unit;

                        switch ( Action )
                        {
                            case UIAction.ButtonOnUpdate:
                                {
                                    icon.SetRelatedImage0SpriteIfNeeded( unit.TooltipIcon.GetSpriteForUI() );

                                    if ( isSelected )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "9694E5" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] );
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( unit != null )
                                    {
                                        unit.RenderUnitTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForDeploying, TooltipExtraText.None, TooltipExtraRules.None );

                                        CommandModeHandler.LastHoveredDeployableNPC = null;
                                        CommandModeHandler.LastHoveredDeployableMachineUnitType = unit;
                                        CommandModeHandler.LastHoveredDeployableMachineVehicleType = null;
                                        CommandModeHandler.LastHoveredAction = null;
                                        CommandModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                if ( unit != null )
                                {
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        if ( !unit.CalculateCanAfford() )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        unit.CommandModeCategory.NonSim_TargetingDeployableNPCType = null;
                                        unit.CommandModeCategory.NonSim_TargetingDeployableMachineVehicleType = null;

                                        Engine_HotM.CurrentCommandModeActionTargeting = null;
                                        if ( unit.CommandModeCategory.NonSim_TargetingDeployableMachineUnitType == unit )
                                            unit.CommandModeCategory.NonSim_TargetingDeployableMachineUnitType = null;
                                        else
                                            unit.CommandModeCategory.NonSim_TargetingDeployableMachineUnitType = unit;
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        SimCommon.CycleThroughMachineActors( true, ( ISimMachineActor a ) => a is ISimMachineUnit u && u.UnitType == unit );
                                    }
                                }
                                break;
                        }
                    } );

                    currentX += COMMAND_ICON_SIZE + COMMAND_ICON_X_SPACING;
                }
            }
            #endregion DrawCommandIcon_Job

            #region DrawCommandIcon_DeployableVehicle
            private void DrawCommandIcon_DeployableVehicle( MachineVehicleType vehicle, ref float currentX, float iconY )
            {
                bCommandUnitIcon icon = bCommandUnitIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, COMMAND_ICON_SIZE, COMMAND_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool isPerformable = vehicle.CalculateCanAfford();
                        bool isSelected = CommandModeHandler.currentCategory?.NonSim_TargetingDeployableMachineVehicleType == vehicle && isPerformable;

                        switch ( Action )
                        {
                            case UIAction.ButtonOnUpdate:
                                {
                                    icon.SetRelatedImage0SpriteIfNeeded( vehicle.TooltipIcon.GetSpriteForUI() );

                                    if ( isSelected )
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( "ffffff" );

                                        icon.SetRelatedImage1EnabledIfNeeded( true );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[1] );
                                    }
                                    else
                                    {
                                        icon.SetRelatedImage0ColorFromHexIfNeeded( isPerformable ? "ffffff" : "9694E5" );

                                        icon.SetRelatedImage1EnabledIfNeeded( false );
                                        icon.SetRelatedImage2SpriteIfNeeded( element.RelatedSprites[isPerformable ? 0 : 2] ); ;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( vehicle != null )
                                    {
                                        vehicle.RenderVehicleTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForDeploying, TooltipExtraText.None, TooltipExtraRules.None );

                                        CommandModeHandler.LastHoveredDeployableNPC = null;
                                        CommandModeHandler.LastHoveredDeployableMachineUnitType = null;
                                        CommandModeHandler.LastHoveredDeployableMachineVehicleType = vehicle;
                                        CommandModeHandler.LastHoveredAction = null;
                                        CommandModeHandler.HoverExpireTime = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                if ( vehicle != null )
                                {
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        if ( !vehicle.CalculateCanAfford() )
                                        {
                                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                            break;
                                        }

                                        vehicle.CommandModeCategory.NonSim_TargetingDeployableNPCType = null;
                                        vehicle.CommandModeCategory.NonSim_TargetingDeployableMachineUnitType = null;

                                        Engine_HotM.CurrentCommandModeActionTargeting = null;
                                        if ( vehicle.CommandModeCategory.NonSim_TargetingDeployableMachineVehicleType == vehicle )
                                            vehicle.CommandModeCategory.NonSim_TargetingDeployableMachineVehicleType = null;
                                        else
                                            vehicle.CommandModeCategory.NonSim_TargetingDeployableMachineVehicleType = vehicle;
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        SimCommon.CycleThroughMachineActors( true, ( ISimMachineActor a ) => a is ISimMachineVehicle v && v.VehicleType == vehicle );
                                    }
                                }
                                break;
                        }
                    } );

                    currentX += COMMAND_ICON_SIZE + COMMAND_ICON_X_SPACING;
                }
            }
            #endregion DrawCommandIcon_Job
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

        #region bCommandBasicIcon
        public class bCommandBasicIcon : ButtonAbstractBaseWithImage
        {
            public static bCommandBasicIcon Original;
            public bCommandBasicIcon() { if ( Original == null ) Original = this; }

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

        #region bCommandUnitIcon
        public class bCommandUnitIcon : ButtonAbstractBaseWithImage
        {
            public static bCommandUnitIcon Original;
            public bCommandUnitIcon() { if ( Original == null ) Original = this; }

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

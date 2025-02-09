using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_MainGameHeaderBarLeft : WindowControllerAbstractBase
    {
        public static Window_MainGameHeaderBarLeft Instance;
		
		public Window_MainGameHeaderBarLeft()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        public static bool GetShouldDrawAnyHeaderBar()
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
            if ( !FlagRefs.HasBeenAskedAboutUITour.DuringGameplay_IsTripped )
                return false;
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null && lowerMode.HideLeftHeader )
                return false;
            if ( Window_SystemMenu.Instance.IsOpen )
                return false;
            if ( Window_Debate.Instance.IsOpen )
                return false;

            return true;
        }

        public static bool GetIsOpeningSubMenuBlocked( bool IsAggressiveCheck )
        {
            if ( Window_VirtualRealityNameWindow.Instance.IsOpen )
                return true;

            if ( Window_RewardWindow.Instance.IsOpen && SimCommon.RewardProvider == NPCDialogChoiceHandler.Instance )
            {
                return true;
            }
            else if ( SimCommon.CurrentSimpleChoice != null )
            {
                return true;
            }
            else if ( IsAggressiveCheck && Window_PlayerHistory.Instance.IsOpen )
            {
                return true;
            }
            else if ( IsAggressiveCheck && Window_PlayerResources.Instance.IsOpen )
            {
                return true;
            }
            else if ( IsAggressiveCheck && Window_PlayerHardware.Instance.IsOpen )
            {
                return true;
            }
            else if ( IsAggressiveCheck && Window_VictoryPath.Instance.IsOpen )
            {
                return true;
            }
            else if ( Window_Debate.Instance.IsOpen )
                return true;
            return false;
        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !GetShouldDrawAnyHeaderBar() )
                return false;
            if ( !FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped )
                return false;

            return true;
        }

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        #region GetBottomLeftCorner
        public static Vector3 GetBottomLeftCorner()
        {
            if ( SizingRectForFullOffset == null )
                return Vector3.zero;
            return SizingRectForFullOffset.GetWorldSpaceBottomLeftCorner();
        }
        #endregion

        private static RectTransform SizingRectForFullOffset = null;

        private static ButtonAbstractBase.ButtonPool<bResourceBox> bResourceBoxPool;
        private static ButtonAbstractBase.ButtonPool<bHeaderTextPopup> bHeaderTextPopupPool;

        public const float POPUP_OFFSET_X = 16f;
        public const float POPUP_OFFSET_Y = 2f;

        public const float RESOURCE_BOX_WIDTH = 55.66f;
        public const float RESOURCE_BOX_HEIGHT = 16f;
        public const float RESOURCE_BOX_SPACING = 1.2f;
        public const float FIRST_RESOURCE_BOX_SPACING = 39.8f;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_HeaderBar" ) * 1.2f;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bResourceBox.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bResourceBoxPool = new ButtonAbstractBase.ButtonPool<bResourceBox>( bResourceBox.Original, 10, "bResourceBox" );
                        bHeaderTextPopupPool = new ButtonAbstractBase.ButtonPool<bHeaderTextPopup>( bHeaderTextPopup.Original, 10, "HeaderTextPopups" );
                    }
                }
                #endregion

                this.UpdateIcons();

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour1_LeftHeader && Instance.GetShouldDrawThisFrame() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "UITour_LeftHeader_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_LeftHeader_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 1, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour1_LeftHeader", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.Left );
                }
            }

            private readonly List<NetworkActorData> networkDatasToDraw = List<NetworkActorData>.Create_WillNeverBeGCed( 20, "Window_MainGameHeaderBarLeft-networkDatasToDraw" );
            private readonly List<ResourceType> resourcesToDraw = List<ResourceType>.Create_WillNeverBeGCed( 80, "Window_MainGameHeaderBarLeft-networkDatasToDraw" );

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bResourceBoxPool.Clear( 5 );
                bHeaderTextPopupPool.Clear( 5 );

                float currentX = FIRST_RESOURCE_BOX_SPACING;
                float currentY = 0f;

                float startingX = currentX;
                float maxX = currentX;
                int rows = 1;

                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                {
                    #region Draw MetaResource Amounts
                    {
                        foreach ( MetaResourceType resourceType in MetaResourceTypeTable.Instance.Rows )
                        {
                            if ( resourceType.IsHiddenFromTopBarWhenNoneHad )
                            {
                                Int64 val = resourceType.Current;
                                if ( val <= 0 )
                                {
                                    if ( !resourceType.IsPinnedToTopBar )
                                        continue;
                                }
                            }

                            DrawInventoryIcon_MetaResourceType( resourceType, ref currentX, ref currentY );
                            if ( currentX > maxX )
                                maxX = currentX;
                        }
                    }
                    #endregion
                }
                else
                {
                    int numberPerRow = 1;

                    {
                        float ratio = this.Element.RelevantRect.GetWorldSpaceWidth() / MathA.Max( 0.001f, this.Element.RelevantRect.sizeDelta.x );

                        float availableSpace = ArcenUI.worldFullSize.x - Window_MainGameHeaderBarRight.GetXWidthForOtherWindowOffsets() - (FIRST_RESOURCE_BOX_SPACING * ratio);
                        float sizePer = (RESOURCE_BOX_WIDTH + RESOURCE_BOX_SPACING) * ratio;

                        numberPerRow = Mathf.FloorToInt( availableSpace / sizePer );
                    }

                    int numberDesired = 0;

                    MachineNetwork network = SimCommon.TheNetwork;
                    networkDatasToDraw.Clear();
                    resourcesToDraw.Clear();

                    #region Count And Filter Stats
                    if ( network != null )
                    {
                        foreach ( ActorDataType dataType in ActorDataTypeTable.Instance.Rows )
                        {
                            if ( !dataType.DuringGameplay_GetShouldBeVisible() )
                                continue;
                            NetworkActorData data = network.GetNetworkDataData( dataType, true );
                            if ( data == null )
                                continue;
                            numberDesired++;
                            networkDatasToDraw.Add( data );
                        }
                    }
                    {
                        bool isInBuildMode = (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "BuildMode";

                        foreach ( ResourceType resourceType in ResourceTypeTable.Instance.Rows )
                        {
                            if ( !resourceType.IsRelatedToCurrentActivities.Display ) //if this is true, then ignore all other constraints
                            {
                                //not related to a current activity, so now check if it should show:

                                if ( !resourceType.DuringGame_IsUnlocked() )
                                    continue;

                                if ( resourceType.IsShownInHeaderOnlyDuringBuildModeOrPinned )
                                {
                                    if ( !isInBuildMode && !resourceType.IsPinnedToTopBar )
                                        continue;
                                }
                                else
                                {

                                    if ( !resourceType.IsPinnedToTopBar )
                                        continue;
                                }

                                if ( resourceType.IsHiddenWhenNoneHad )
                                {
                                    Int64 val = resourceType.Current;
                                    if ( val <= 0 )
                                        continue;
                                }
                            }
                            numberDesired++;
                            resourcesToDraw.Add( resourceType );
                        }
                    }
                    #endregion


                    //divide this into nice rows that are more event
                    rows = numberDesired <= numberPerRow ? 1 : Mathf.CeilToInt( (float)numberDesired / (float)numberPerRow );
                    int actualPerRow = rows == 1 ? numberDesired : Mathf.CeilToInt( (float)numberDesired / (float)rows );

                    int remainingInRow = actualPerRow;

                    #region Draw Network Stats
                    if ( network != null )
                    {
                        foreach ( NetworkActorData data in networkDatasToDraw )
                        {
                            DrawNetworkStat( data.Type, data, network, ref currentX, ref currentY );
                            if ( currentX > maxX )
                                maxX = currentX;
                            remainingInRow--;
                            if ( remainingInRow <= 0 )
                            {
                                remainingInRow = actualPerRow;
                                currentY -= (RESOURCE_BOX_HEIGHT + RESOURCE_BOX_SPACING);
                                currentX = startingX;
                            }
                        }
                    }
                    #endregion

                    #region Draw Resource Amounts
                    {
                        foreach ( ResourceType resourceType in resourcesToDraw )
                        {
                            DrawInventoryIcon_ResourceType( resourceType, ref currentX, ref currentY );
                            if ( currentX > maxX )
                                maxX = currentX;
                            remainingInRow--;
                            if ( remainingInRow <= 0)
                            {
                                remainingInRow = actualPerRow;
                                currentY -= (RESOURCE_BOX_HEIGHT + RESOURCE_BOX_SPACING);
                                currentX = startingX;
                            }
                        }
                    }
                    #endregion
                }

                #region Expand or Shrink Width Of This Window
                float widthForWindow = MathA.Abs( maxX ) + EXTRA_SPACING_AFTER_ELEMENTS;
                if ( lastWindowWidth != widthForWindow )
                {
                    lastWindowWidth = widthForWindow;
                    //this.Element.RelevantRect.anchorMin = new Vector2( 1f, 0.5f );
                    //this.Element.RelevantRect.anchorMax = new Vector2( 1f, 0.5f );
                    //this.Element.RelevantRect.pivot = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.UI_SetWidth( widthForWindow );

                    //float ratio = this.Element.RelevantRect.GetWorldSpaceWidth() / MathA.Max( 0.001f, this.Element.RelevantRect.sizeDelta.x );

                    //ArcenDebugging.LogSingleLine( "widthForWindow: " + widthForWindow + " UISpaceWidth " + this.Element.RelevantRect.GetWorldSpaceWidth() + " rightWidA: " +
                    //    Window_MainGameHeaderBarRight.GetXWidthForOtherWindowOffsets() + " worldFullWid: " +
                    //    ArcenUI.worldFullSize.x + " predictedWidthA: " +
                    //    (widthForWindow * ArcenUI.unitsPerUIPixelX) + " sizeDelta: " + this.Element.RelevantRect.sizeDelta.x + " ratio: " + ratio +
                    //    " predictedWidthB: " +
                    //    (widthForWindow * ratio), Verbosity.DoNotShow );

                    //{
                    //    float ratio = this.Element.RelevantRect.GetWorldSpaceWidth() / MathA.Max( 0.001f, this.Element.RelevantRect.sizeDelta.x );

                    //    float availableSpace = ArcenUI.worldFullSize.x - Window_MainGameHeaderBarRight.GetXWidthForOtherWindowOffsets() - (FIRST_RESOURCE_BOX_SPACING * ratio);
                    //    float sizePer = (RESOURCE_BOX_WIDTH + RESOURCE_BOX_SPACING) * ratio;

                    //    ArcenDebugging.LogSingleLine( "availableSpace: " + availableSpace + " sizePer " + sizePer + " canFit: " +
                    //        Mathf.FloorToInt( availableSpace / sizePer ), Verbosity.DoNotShow );
                    //}
                }
                #endregion

                #region Expand or Shrink Height Of This Window
                float heightForWindow = ( rows * RESOURCE_BOX_HEIGHT ) + ( (rows - 1) * RESOURCE_BOX_SPACING );
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion
            }
            private float lastWindowWidth = -1f;
            private float lastWindowHeight = -1f;

            public const float EXTRA_SPACING_AFTER_ELEMENTS = 3f;

            #region DrawInventoryIcon_MetaResourceType
            private void DrawInventoryIcon_MetaResourceType( MetaResourceType resource, ref float currentX, ref float currentY )
            {
                bResourceBox icon = bResourceBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float popupX = currentX + POPUP_OFFSET_X;
                    icon.SetSpriteIfNeeded( resource.Icon.GetSpriteForUI() );
                    {
                        string resourceColorHex = resource.Implementation.CalculateColorHex( resource );
                        icon.SetImageColorFromHexIfNeeded( resourceColorHex );
                    }
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, true, false, RESOURCE_BOX_WIDTH, RESOURCE_BOX_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                icon.SetRelatedImage0EnabledIfNeeded( false );// resource.IsPinnedToTopBar );

                                Int64 materialAmount = resource.Current;
                                ExtraData.Buffer.AddRaw( materialAmount.ToStringLargeNumberAbbreviated(), materialAmount <= 0 ? ColorTheme.RedOrange2 : string.Empty );
                                break;
                            case UIAction.HandleMouseover:
                                resource.WriteMetaResourceTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None );
                                break;
                            case UIAction.OnClick:
                                //if ( GetIsOpeningSubMenuBlocked() )
                                {
                                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                    break;
                                }

                                //VisCommands.ToggleInventoryWindow_MetaTargetTab( Window_PlayerInventory.MetaInventoryDisplayType.MetaResources );
                                //break;
                        }
                    } );

                    float popupY = currentY - RESOURCE_BOX_HEIGHT + POPUP_OFFSET_Y;
                    //UIHelper.RenderTrendPopups( bHeaderTextPopupPool, changeSet, resource.GetDisplayName(), ref popupX, ref popupY, false, FourDirection.South,
                    //        TrendPopupSize.Small );

                    currentX += RESOURCE_BOX_SPACING;
                }
            }
            #endregion

            #region DrawInventoryIcon_ResourceType
            private void DrawInventoryIcon_ResourceType( ResourceType resource, ref float currentX, ref float currentY )
            {
                TrendChangeSet changeSet = resource.Trends;

                bResourceBox icon = bResourceBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float popupX = currentX + POPUP_OFFSET_X;
                    icon.SetSpriteIfNeeded( resource.Icon.GetSpriteForUI() );
                    {
                        string resourceColorHex = resource.Implementation.CalculateColorHex( resource );
                        icon.SetImageColorFromHexIfNeeded( resourceColorHex );
                    }
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, true, false, RESOURCE_BOX_WIDTH, RESOURCE_BOX_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                icon.SetRelatedImage0EnabledIfNeeded( resource.IsPinnedToTopBar );
                                Int64 materialAmount = resource.Current;
                                ExtraData.Buffer.AddRaw( materialAmount.ToStringLargeNumberAbbreviated(), materialAmount <= 0 ? ColorTheme.RedOrange2 : string.Empty );
                                break;
                            case UIAction.HandleMouseover:
                                resource.WriteResourceTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForConstruction, TooltipExtraText.None );
                                break;
                            case UIAction.OnClick:
                                if ( GetIsOpeningSubMenuBlocked( false ) )
                                {
                                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                    break;
                                }

                                VisCommands.ToggleResourceWindow_TargetTab( Window_PlayerResources.ResourcesDisplayType.ResourceStorage );
                                break;
                        }
                    } );

                    float popupY = currentY - RESOURCE_BOX_HEIGHT + POPUP_OFFSET_Y;
                    UIHelper.RenderTrendPopups( bHeaderTextPopupPool, changeSet, resource.GetDisplayName(), ref popupX, ref popupY, false, FourDirection.South,
                            TrendPopupSize.Small );

                    currentX += RESOURCE_BOX_SPACING;
                }
            }
            #endregion

            #region DrawNetworkStat
            private void DrawNetworkStat( ActorDataType dataType, NetworkActorData networkData, MachineNetwork network, ref float currentX, ref float currentY )
            {
                //TrendChangeSet changeSet = networkData.Trends;

                bResourceBox icon = bResourceBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    //float popupX = currentX + POPUP_OFFSET_X;
                    icon.SetSpriteIfNeeded( dataType.Icon.GetSpriteForUI() );
                    {
                        string resourceColorHex = networkData.CalculateSidebarIconColorHex( true );
                        icon.SetImageColorFromHexIfNeeded( resourceColorHex );
                    }
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, true, false, RESOURCE_BOX_WIDTH, RESOURCE_BOX_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    icon.SetRelatedImage0EnabledIfNeeded( false );// resource.IsPinnedToTopBar );

                                    int currentVal = networkData.Current;
                                    {
                                        string colorHex = networkData.CalculateSidebarIconColorHex( false );
                                        float multiplier = networkData.EffectivenessMultiplier;

                                        if ( multiplier < 1f )
                                            colorHex = ColorTheme.RedOrange3;

                                        {
                                            int percentage = networkData.PercentProvided;
                                            ExtraData.Buffer.AddRaw( percentage.ToStringIntPercent(), colorHex );
                                        }
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    networkData.WriteActorDataTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.None );
                                }
                                break;
                            case UIAction.OnClick:
                                //if ( GetIsOpeningSubMenuBlocked() )
                                //{
                                //    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                //    break;
                                //}
                                //if ( Window_PlayerInventory.Instance.IsOpen )
                                //    Window_PlayerInventory.Instance.Close( WindowCloseReason.UserDirectRequest );
                                //else
                                //    Window_PlayerInventory.Instance.Open();
                                //Window_PlayerInventory.customParent.currentlyRequestedDisplayTypeCity = Window_PlayerInventory.CityInventoryDisplayType.PrimaryResources;
                                break;
                        }
                    } );

                    //float popupY = currentY - ICON_SIZE + POPUP_OFFSET_Y;
                    //UIHelper.RenderTrendPopups( bHeaderTextPopupPool, changeSet, resource.GetDisplayName(), ref popupX, ref popupY, false, FourDirection.South,
                    //        TrendPopupSize.Small );

                    currentX += RESOURCE_BOX_SPACING;
                }
            }
            #endregion
        }

        #region bResourceBox
        public class bResourceBox : ButtonAbstractBaseWithImage
        {
            public static bResourceBox Original;
            public bResourceBox() { if ( Original == null ) Original = this; }

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

        #region bHeaderTextPopup
        public class bHeaderTextPopup : ButtonAbstractBaseWithImage, IAssignableGetOrSetUIData
        {
            public static bHeaderTextPopup Original;
            public bHeaderTextPopup() { if ( Original == null ) Original = this; }

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

        #region bSettings
        public class bSettings : ButtonAbstractBaseWithImage
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                this.SetSpriteIfNeeded( IconRefs.Header_Menu.Icon.GetSpriteForUI() );
                this.SetBoxStyle( this.Element.LastHadMouseWithin || Window_SystemMenu.Instance.IsOpen );
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderLeft", "Menu" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                {
                    novel.TitleUpperLeft.AddLang( "HeaderBar_SystemMenu_Top" );
                    novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "Cancel" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "HeaderBar_SystemMenu_Explanation" );
                    novel.Icon = IconRefs.Header_Menu.Icon;
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( GetIsOpeningSubMenuBlocked( false ) )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                VisCommands.ToggleSystemMenu();
                return MouseHandlingResult.None;
            }

            public ArcenUI_Button But;

            #region SetBoxStyle
            private bool lastWasHighlighted = false;

            public void SetBoxStyle( bool IsHighlighted )
            {
                if ( lastWasHighlighted == IsHighlighted )
                    return;

                if ( this.But == null )
                {
                    this.But = this.Element as ArcenUI_Button;
                    if ( this.But == null )
                        return;
                }

                lastWasHighlighted = IsHighlighted;

                if ( IsHighlighted )
                {
                    this.Element.RelatedImages[0].enabled = true; //glow
                    this.image.material = this.But.RelatedMaterials[0]; //highlight glow
                }
                else
                {
                    this.Element.RelatedImages[0].enabled = false; //no glow
                    this.image.material = null; //no shader
                }
            }
            #endregion
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ActorTypeSidebarStats : WindowControllerAbstractBase, IBadgeIconFactory
    {
        public static Window_ActorTypeSidebarStats Instance;
		
		public Window_ActorTypeSidebarStats()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !( Window_ActorCustomizeLoadout.Instance?.IsOpen ?? false ) )
                return false;
            if ( Window_ActorCustomizeLoadout.ActorBeingExaminedOrNull != null )
                return false;

            return true;
        }

        #region GetCurrentWidth_Scaled
        /// <summary>
        /// Gets the amount of horizontal space the this will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetCurrentWidth_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 66f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        #region GetTopYForOtherWindowOffsets
        public static float GetTopYForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            return SizingRect.GetWorldSpaceMinYAndMaxY( 0 ).x;
        }
        #endregion

        #region GetBottomYForOtherWindowOffsets
        public static float GetBottomYForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            return SizingRect.GetWorldSpaceMinYAndMaxY( 0 ).y;
        }
        #endregion

        #region GetXWidthForOtherWindowOffsets
        public static float GetXWidthForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            float height = SizingRect.GetWorldSpaceSize().x;
            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        public static RectTransform SizingRect;

        #region bActorName
        public class bActorName : ButtonAbstractBase
        {
            public static bActorName Instance;
            public bActorName() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                    Buffer.Space1x().AddRaw( actorDGD.GetDisplayName() );
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                {
                    if ( actorDGD.ParentUnitTypeOrNull != null )
                    {
                        if ( actorDGD.ParentUnitTypeOrNull.IsConsideredMech )
                            Buffer.AddLang( "StandardMech" );
                        else
                            Buffer.AddLang( "StandardAndroid" );
                    }
                    else if ( actorDGD.ParentVehicleTypeOrNull != null )
                        Buffer.AddLang( "StandardVehicle" );
                    else if ( actorDGD.ParentNPCUnitTypeOrNull != null )
                    {
                        if ( actorDGD.ParentNPCUnitTypeOrNull.CostsToCreateIfBulkAndroid.Count > 0 )
                            Buffer.AddLang( "BulkAndroid" );
                        else if ( actorDGD.ParentNPCUnitTypeOrNull.CapturedUnitCapacityRequired > 0 )
                            Buffer.AddLang( "CapturedUnit" );
                        else
                            Buffer.AddLang( "AlliedUnit" );
                    }
                }
            }

            public override void HandleMouseover()
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                {
                    if ( actorDGD.ParentUnitTypeOrNull != null )
                        actorDGD.ParentUnitTypeOrNull.RenderUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentVehicleTypeOrNull != null )
                        actorDGD.ParentVehicleTypeOrNull.RenderVehicleTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentNPCUnitTypeOrNull != null )
                        actorDGD.ParentNPCUnitTypeOrNull.RenderNPCUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bActorIcon
        public class bActorIcon : ButtonAbstractBaseWithImage
        {
            public static bActorIcon Instance;
            public bActorIcon() { if ( Instance == null ) Instance = this; }

            private VisColorUsage lastTooltipIconColorStyle = null;
            private int PriorNumberOfSelectDataReloads = 0;

            public override void OnUpdateSubSub()
            {
                VisColorUsage desiredTooltipIconColorStyle = null;
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                {
                    Instance.SetSpriteIfNeeded( actorDGD.GetTooltipIcon()?.GetSpriteForUI() );
                    desiredTooltipIconColorStyle = actorDGD.GetTooltipIconColorStyle();
                }

                if ( desiredTooltipIconColorStyle == null )
                    desiredTooltipIconColorStyle = ColorRefs.DefaultTooltipIconColorStyle;

                if ( lastTooltipIconColorStyle != desiredTooltipIconColorStyle || PriorNumberOfSelectDataReloads != Engine_HotM.NumberOfSelectDataReloads )
                {
                    lastTooltipIconColorStyle = desiredTooltipIconColorStyle;
                    this.Element.RelatedImages[0].color = desiredTooltipIconColorStyle.ColorWithoutHDR;
                    this.Element.RelatedImages[2].color = desiredTooltipIconColorStyle.RelatedLightColor;
                    PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                }
            }

            public override void HandleMouseover()
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                {
                    if ( actorDGD.ParentUnitTypeOrNull != null )
                        actorDGD.ParentUnitTypeOrNull.RenderUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentVehicleTypeOrNull != null )
                        actorDGD.ParentVehicleTypeOrNull.RenderVehicleTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentNPCUnitTypeOrNull != null )
                        actorDGD.ParentNPCUnitTypeOrNull.RenderNPCUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bCustomize
        public class bCustomize : ButtonAbstractBaseWithImage
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD.IsNPC )
                {
                    NPCUnitType npcUnitType = actorDGD.ParentNPCUnitTypeOrNull;
                    Buffer.AddLangAndAfterLineItemHeader( npcUnitType != null && (npcUnitType.IsMechStyleMovement || npcUnitType.IsVehicle) ? "Crew_Prefix" : "Squad_Prefix" )
                        .AddRaw( npcUnitType.BasicSquadSize.ToStringWholeBasic(), ColorTheme.HeaderGoldOrangeDark );
                    return;
                }
            }

            public override void HandleMouseover()
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD != null )
                {
                    if ( actorDGD.ParentUnitTypeOrNull != null )
                        actorDGD.ParentUnitTypeOrNull.RenderUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentVehicleTypeOrNull != null )
                        actorDGD.ParentVehicleTypeOrNull.RenderVehicleTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                    else if ( actorDGD.ParentNPCUnitTypeOrNull != null )
                        actorDGD.ParentNPCUnitTypeOrNull.RenderNPCUnitTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                }
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;
                if ( actorDGD.IsNPC )
                    return false;
                return true;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bStatsIcon> bStatsIconPool;
        private static ButtonAbstractBase.ButtonPool<bBadgeIcon> bBadgeIconPool;

        public const float STATS_ICON_X = 2f;
        public const float STATS_ICON_Y_SPACING = 1f;
        public const float STATS_ICON_HEIGHT = 18f;
        public const float STATS_ICON_WIDTH = 67.96f;
        public const float STATS_ICON_SPACING = 2f;
        public const float STATS_NUMBER_WIDTH = 42.8f;

        public const float POST_NAME_ICON_Y_START = -85.2f;

        public const float STATS_STARTING_Y = -3.1f;

        public const float ACTOR_ICON_SIZE = 75f;
        public const float ACTOR_NAME_HEIGHT = 18.8f;

        public static Sprite UnitFrameEmptySprite = null;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public RectTransform panelForStats = null;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.2f * GameSettings.Current.GetFloat( "Scale_StatsSidebar" );

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bStatsIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bStatsIconPool = new ButtonAbstractBase.ButtonPool<bStatsIcon>( bStatsIcon.Original, 10, "bStatsIcon" );
                        bBadgeIconPool = new ButtonAbstractBase.ButtonPool<bBadgeIcon>( bBadgeIcon.Original, 10, "bBadgeIcon" );

                        panelForStats = this.Element.RelatedTransforms[0];
                    }
                }
                #endregion

                this.UpdateIcons();
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bStatsIconPool.Clear( 5 );
                bBadgeIconPool.Clear( 5 );

                MobileActorTypeDuringGameData actorDGD = Window_ActorCustomizeLoadout.DuringGameDataBeingExamined;

                float cumulativeStatStatSize = 0f;

                float finalYForSubPanel = POST_NAME_ICON_Y_START;
                float currentY = POST_NAME_ICON_Y_START;
                float startingY = 1.7f; //margin at the bottom
                float statsY = STATS_STARTING_Y;
                float statsMargin = 3.1f + 3.1f;

                #region Draw Actor Stats
                {
                    foreach ( ActorDataType dataType in ActorDataTypeTable.Instance.Rows )
                    {
                        Pair<int, int> minMax = actorDGD.NonSimActorDataMinMax.GetDisplayDict()[dataType];

                        if ( minMax.RightItem <= 0 ) //if max is <= 0
                        {
                            if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                            {
                                Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out int netChange );
                                if ( netChange > 0 )
                                    DrawActorEntityData( dataType, actorDGD, ref statsY, ref cumulativeStatStatSize );
                                else
                                {
                                    Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out int pendingChange );
                                    if ( pendingChange > 0 )
                                        DrawActorEntityData( dataType, actorDGD, ref statsY, ref cumulativeStatStatSize );
                                }
                            }
                            continue;
                        }

                        if ( dataType.AlsoVisibleWhenInDanger ) //always consider in danger in this case
                        { } //draw!
                        else
                        {
                            int currentVal = minMax.RightItem;
                            if ( (dataType.OnlyVisibleWhenBelow <= currentVal || dataType.OnlyVisibleWhenAbove >= currentVal) )
                            {
                                if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                                {
                                    Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out int netChange );
                                    if ( netChange > 0 )
                                        DrawActorEntityData( dataType, actorDGD, ref statsY, ref cumulativeStatStatSize );
                                    else
                                    {
                                        Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out int pendingChange );
                                        if ( pendingChange > 0 )
                                            DrawActorEntityData( dataType, actorDGD, ref statsY, ref cumulativeStatStatSize );
                                    }
                                }
                                continue;
                            }
                        }
                        DrawActorEntityData( dataType, actorDGD, ref statsY, ref cumulativeStatStatSize );
                    }
                }
                #endregion

                {
                    UIHelper.ApplyPanelInPosition( this.panelForStats, 2.06f, finalYForSubPanel, 73.2f, cumulativeStatStatSize + statsMargin );
                    currentY -= (cumulativeStatStatSize + statsMargin);
                }

                float finalHeight = -(currentY - startingY);

                #region Expand or Shrink Height Of This Window
                float heightForWindow = finalHeight;
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 1f, 0.5f );
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion

                SizingRect = this.Element.RelevantRect;

                UIHelper.DrawAllActorTypeBadgesAndPerks( actorDGD, Instance, 61.5f, 14.3f, 18f, 0f, 15f, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.None );
            }

            private float lastWindowHeight = -1f;

            #region DrawActorEntityData
            private void DrawActorEntityData( ActorDataType dataType, MobileActorTypeDuringGameData actorDGD, ref float currentY, ref float cumulativeStatStatSize )
            {
                bStatsIcon icon = bStatsIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    if ( cumulativeStatStatSize > 0 )
                        cumulativeStatStatSize += STATS_ICON_SPACING;

                    icon.SetSpriteIfNeeded( dataType.Icon.GetSpriteForUI() );
                    if ( icon.image != null )
                        icon.image.color = dataType.SidebarIconColor;
                    float iconX = STATS_ICON_X;
                    float heightToUse = STATS_ICON_HEIGHT;
                    icon.ApplyItemInPositionSetTextSize( ref iconX, ref currentY, true, false, STATS_ICON_WIDTH, STATS_NUMBER_WIDTH, STATS_ICON_HEIGHT );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        int pendingChange = 0;
                        int netChange = 0;
                        if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                        {
                            Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out netChange );
                            Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out pendingChange );
                        }

                        Pair<int, int> minMax = actorDGD.NonSimActorDataMinMax.GetDisplayDict()[dataType];

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                int currentVal = minMax.LeftItem; //min
                                ExtraData.Buffer.Space1x(); //adds a bit of room from the icon
                                if ( netChange != 0 )
                                    ExtraData.Buffer.StartSize80().AddFormat2( "NumberUp", dataType.GetNumberAsFormattedStringForSidebar( currentVal ),
                                        dataType.GetNumberAsFormattedStringForSidebar( MathA.Max( currentVal + netChange, dataType.MaxCannotBeReducedBelow ) ),
                                        netChange > 0 ? ColorTheme.TrendPositive : ColorTheme.TrendNegative ).EndSize();
                                else if ( pendingChange != 0 )
                                    ExtraData.Buffer.StartSize80().AddFormat2( "NumberUp", dataType.GetNumberAsFormattedStringForSidebar( currentVal ),
                                        dataType.GetNumberAsFormattedStringForSidebar( MathA.Max( currentVal + pendingChange, dataType.MaxCannotBeReducedBelow ) ),
                                        ColorTheme.Gray ).EndSize();
                                else
                                {
                                    ExtraData.Buffer.AddRaw( dataType.GetNumberAsFormattedStringForSidebar( currentVal ), dataType.SidebarIconColorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartBasicTooltip( TooltipID.Create( dataType ), element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = dataType.Icon;
                                    novel.TitleUpperLeft.AddRaw( dataType.GetDisplayName() );
                                    novel.TitleLowerLeft.AddLang( "UnitStat_Subheader" );

                                    if ( minMax.LeftItem == minMax.RightItem )
                                        novel.TitleUpperRight.AddRaw( minMax.LeftItem.ToString() );
                                    else
                                        novel.TitleUpperRight.AddFormat2( "MinMaxRange", minMax.LeftItem.ToString(), minMax.RightItem.ToString() );

                                    if ( !dataType.GetDescription().IsEmpty() )
                                        novel.Main.AddRaw( dataType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                    if ( dataType.StrategyTipOptional.Text.Length > 0 )
                                        novel.Main.AddRaw( dataType.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                }
                                break;
                            case UIAction.OnClick:
                                break;
                        }
                    } );

                    currentY -= (heightToUse + STATS_ICON_SPACING);
                    cumulativeStatStatSize += heightToUse;
                }
            }
            #endregion DrawActorEntityData
        }

        #region bStatsIcon
        public class bStatsIcon : ButtonAbstractBaseWithImage
        {
            public static bStatsIcon Original;
            public bStatsIcon() { if ( Original == null ) Original = this; }

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

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion

        #region bHeaderTextPopup
        public class bHeaderTextPopup : ButtonAbstractBaseWithImage
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region bBadgeIcon
        public class bBadgeIcon : bBadgeIconBase
        {
            public static bBadgeIcon Original;
            public bBadgeIcon() { if ( Original == null ) Original = this; }
        }

        public bBadgeIconBase GetNewBadgeIconBase()
        {
            return bBadgeIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
        }
        #endregion

        #region tStanceLabel
        public class tStanceLabel : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region bStance
        public class bStance : ButtonAbstractBaseWithImage
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region bActionOverTime
        public class bActionOverTime : ButtonAbstractBaseWithImage
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

        #region imgBasicWindowBG
        public class imgBasicWindowBG : ImageAbstractBase
        {
            public override void HandleClick( MouseHandlingInput input )
            {
                if ( input.RightButtonClicked )
                    Window_ActorCustomizeLoadout.Instance.Close( WindowCloseReason.UserCasualRequest );
            }
        }
        #endregion
    }
}

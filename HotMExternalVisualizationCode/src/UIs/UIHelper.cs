using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public static class UIHelper
    {
        public const float SMALL_POPUP_NUMBER_HEIGHT = 11.2f;
        public const float SMALL_POPUP_NUMBER_WIDTH = 31.12f;
        public const float SMALL_POPUP_TO_POPUP_SPACING = 1.4f;

        public const float NORMAL_POPUP_NUMBER_HEIGHT = 14f;
        public const float NORMAL_POPUP_NUMBER_WIDTH = 46f;
        public const float NORMAL_POPUP_TO_POPUP_SPACING = 2f;

        #region RenderTrendPopups
        public static void RenderTrendPopups<bHeaderTextPopup>( ButtonAbstractBase.ButtonPool<bHeaderTextPopup> bHeaderTextPopupPool,
            ITrendChangeSetRecent changeSet, string NameOfThingBeingChanged, ref float popupX, ref float popupY, bool IsSecondary, FourDirection MoveDirection,
            TrendPopupSize PopupSize ) 
            where bHeaderTextPopup : ButtonAbstractBaseWithImage
        {
            float popupHeight = 0;
            float popupWidth = 0;
            float popupSpacing = 0;
            if ( PopupSize == TrendPopupSize.Small )
            {
                popupHeight = SMALL_POPUP_NUMBER_HEIGHT;
                popupWidth = SMALL_POPUP_NUMBER_WIDTH;
                popupSpacing = SMALL_POPUP_TO_POPUP_SPACING;
            }
            else
            {
                popupHeight = NORMAL_POPUP_NUMBER_HEIGHT;
                popupWidth = NORMAL_POPUP_NUMBER_WIDTH;
                popupSpacing = NORMAL_POPUP_TO_POPUP_SPACING;
            }

            #region Popup Negative
            if ( changeSet.HadRecentNegativeChanges ) //this means that we've recently gained or lost some
            {
                bHeaderTextPopup popup = bHeaderTextPopupPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( popup != null )
                {
                    IAssignableGetOrSetUIData assignable = (popup as IAssignableGetOrSetUIData);
                    if ( assignable == null )
                    {
                        ArcenDebugging.LogWithStack( "Invalid bHeaderTextPopup!  Does not inherit from IAssignableGetOrSetUIData.", Verbosity.ShowAsError );
                        return;
                    }
                    popup.ApplyItemInPosition( ref popupX, ref popupY, false, false, popupWidth, popupHeight );
                    assignable.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool hadChange = changeSet.HadRecentNegativeChanges;
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( hadChange )
                                {
                                    if ( IsSecondary )
                                        ExtraData.Buffer.StartSize60().AddLang( "SecondaryResource_XP_Prefix" ).Space1x().EndSize().StartSize70();
                                    ExtraData.Buffer.AddRaw( changeSet.RecentNegativeChangeString, changeSet.GetNegativeColor() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                if ( hadChange )
                                {
                                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( NameOfThingBeingChanged, "Negative" ), element, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( NameOfThingBeingChanged );
                                        novel.Main.AddRaw( changeSet.RecentNegativeChangeString, changeSet.GetNegativeColor() );
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                changeSet.ClearRecentNegativeData();
                                break;
                            case UIAction.GetShouldBeHidden:
                                ExtraData.Bool = !hadChange;
                                break;
                        }
                    } );
                    switch (MoveDirection)
                    {
                        case FourDirection.South:
                            popupY -= popupHeight + popupSpacing;
                            break;
                        case FourDirection.North:
                            popupY += popupHeight + popupSpacing;
                            break;
                        case FourDirection.East:
                            popupX += popupWidth + popupSpacing;
                            break;
                        case FourDirection.West:
                            popupX -= popupWidth + popupSpacing;
                            break;
                    }
                }
            }
            #endregion

            #region Popup Positive
            if ( changeSet.HadRecentPositiveChanges ) //this means that we've recently gained or lost some
            {
                bHeaderTextPopup popup = bHeaderTextPopupPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( popup != null )
                {
                    IAssignableGetOrSetUIData assignable = (popup as IAssignableGetOrSetUIData);
                    if ( assignable == null )
                    {
                        ArcenDebugging.LogWithStack( "Invalid bHeaderTextPopup!  Does not inherit from IAssignableGetOrSetUIData.", Verbosity.ShowAsError );
                        return;
                    }

                    popup.ApplyItemInPosition( ref popupX, ref popupY, false, false, popupWidth, popupHeight );
                    assignable.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool hadChange = changeSet.HadRecentPositiveChanges;
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( hadChange )
                                {
                                    if ( IsSecondary )
                                        ExtraData.Buffer.StartSize60().AddLang( "SecondaryResource_XP_Prefix" ).Space1x().EndSize().StartSize70();
                                    ExtraData.Buffer.AddFormat1( "PositiveChange", changeSet.RecentPositiveChangeString, changeSet.GetPositiveColor() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                if ( hadChange )
                                {
                                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( NameOfThingBeingChanged, "Positive" ), element, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( NameOfThingBeingChanged );
                                        novel.Main.AddFormat1( "PositiveChange", changeSet.RecentPositiveChangeString, changeSet.GetPositiveColor() );
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                changeSet.ClearRecentPositiveData();
                                break;
                            case UIAction.GetShouldBeHidden:
                                ExtraData.Bool = !hadChange;
                                break;
                        }
                    } );
                    switch ( MoveDirection )
                    {
                        case FourDirection.South:
                            popupY -= popupHeight + popupSpacing;
                            break;
                        case FourDirection.North:
                            popupY += popupHeight + popupSpacing;
                            break;
                        case FourDirection.East:
                            popupX += popupWidth + popupSpacing;
                            break;
                        case FourDirection.West:
                            popupX -= popupWidth + popupSpacing;
                            break;
                    }
                }
            }
            #endregion
        }
        #endregion RenderTrendPopups

        #region HandleTooltipExtraRules
        public static void HandleTooltipExtraRules( TooltipExtraRules ExtraRules, Vector2 sizeDelta, ref Vector3 worldSpacePoint )
        {
            switch ( ExtraRules )
            {
                case TooltipExtraRules.ClampToSidebar:
                    {
                        bool showOnLeft = Engine_Universal.PrimaryIsLeft;
                        if ( showOnLeft )
                        {
                            float minX = Window_Sidebar.GetMinXWhenOnLeft();

                            if ( worldSpacePoint.x < minX )
                                worldSpacePoint.x = minX;
                        }
                        else
                        {
                            worldSpacePoint.x = Window_Sidebar.GetMaxXWhenOnRight() - sizeDelta.x - 0.1f;
                        }
                    }
                    break;
                case TooltipExtraRules.MustBeToLeftOfAbilityFooter:
                    {
                        //disabled for now!
                        //if ( Window_AbilityFooterBar.Instance.GetShouldDrawThisFrame() )
                        //{
                        //    float footerBarLeft = Window_AbilityFooterBar.GetMaxXForTooltips();
                        //    float maxX = footerBarLeft - sizeDelta.x - 0.1f;
                        //    if ( worldSpacePoint.x > maxX )
                        //        worldSpacePoint.x = maxX;

                        //    //BUT!  We still need to make sure that it does not go off the screen
                        //    if ( worldSpacePoint.x < ArcenUI.Instance.world_TopLeft.x )
                        //    {
                        //        worldSpacePoint.x = ArcenUI.Instance.world_TopLeft.x;

                        //        if ( worldSpacePoint.x + sizeDelta.x > footerBarLeft + 0.1f )
                        //        {
                        //            //if this happens, then we also need to bump this up to avoid overlapping the ability footer bar
                        //            float maxY = Window_AbilityFooterBar.GetMaxYForTooltips() + sizeDelta.y - 0.1f;
                        //            if ( worldSpacePoint.y < maxY )
                        //                worldSpacePoint.y = maxY;
                        //        }
                        //    }
                        //}

                        //instead do above
                        if ( Window_AbilityFooterBar.TryGetTopYForOtherWindowOffsets( out float MinY ) )
                        {
                            float bottom = worldSpacePoint.y - sizeDelta.y - 0.1f;
                            if ( bottom < MinY )
                                worldSpacePoint.y = MinY + sizeDelta.y + 0.1f;
                        }
                    }
                    break;
                case TooltipExtraRules.MustBeToLeftOfCommandBar:
                    {
                        if ( Window_CommandFooter.Instance.GetShouldDrawThisFrame() )
                        {
                            float footerBarLeft = Window_CommandFooter.GetMaxXForTooltips();
                            float maxX = footerBarLeft - sizeDelta.x - 0.1f;
                            if ( worldSpacePoint.x > maxX )
                                worldSpacePoint.x = maxX;

                            //BUT!  We still need to make sure that it does not go off the screen
                            if ( worldSpacePoint.x < ArcenUI.Instance.world_TopLeft.x )
                            {
                                worldSpacePoint.x = ArcenUI.Instance.world_TopLeft.x;

                                if ( worldSpacePoint.x + sizeDelta.x > footerBarLeft + 0.1f )
                                {
                                    //if this happens, then we also need to bump this up to avoid overlapping the ability footer bar
                                    float maxY = Window_CommandFooter.GetMaxYForTooltips() + sizeDelta.y - 0.1f;
                                    if ( worldSpacePoint.y < maxY )
                                        worldSpacePoint.y = maxY;
                                }
                            }
                        }
                    }
                    break;                    
                case TooltipExtraRules.MustBeToLeftOfSidebar:
                    {
                        if ( Window_Sidebar.Instance.GetShouldDrawThisFrame() )
                        {
                            float sidebarLeft = Window_Sidebar.GetMaxXForTooltips();
                            float maxX = sidebarLeft - sizeDelta.x - 0.1f;
                            if ( worldSpacePoint.x > maxX )
                                worldSpacePoint.x = maxX;
                        }
                    }
                    break;                    
                case TooltipExtraRules.MustBeToRightOfCustomizeLoadout:
                    {
                        if ( Window_ActorCustomizeLoadout.Instance.GetShouldDrawThisFrame() )
                        {
                            float minX = Window_ActorCustomizeLoadout.GetMinXForTooltips() + 0.1f;
                            if ( worldSpacePoint.x < minX )
                            {
                                worldSpacePoint.x = minX;

                                float rightmost = worldSpacePoint.x + sizeDelta.x;
                                if ( rightmost >= ArcenUI.Instance.world_TopRight.x )
                                {
                                    //if there IS room to the left, then place us there:
                                    worldSpacePoint.x = ArcenUI.Instance.world_TopRight.x - sizeDelta.x;
                                }
                            }
                        }
                    }
                    break;
                case TooltipExtraRules.MustBeToRightOfBuildMenu:
                    {
                        if ( Window_BuildSidebar.Instance.GetShouldDrawThisFrame() )
                        {
                            float minX = Window_BuildSidebar.GetMinXForTooltips() + 0.1f;
                            if ( worldSpacePoint.x < minX )
                            {
                                worldSpacePoint.x = minX;

                                float rightmost = worldSpacePoint.x + sizeDelta.x;
                                if ( rightmost >= ArcenUI.Instance.world_TopRight.x )
                                {
                                    //if there IS room to the left, then place us there:
                                    worldSpacePoint.x = ArcenUI.Instance.world_TopRight.x - sizeDelta.x;
                                }
                            }
                        }
                    }
                    break;
                case TooltipExtraRules.MustBeToLeftOfTaskStack:
                    {
                        if ( Window_TaskStack.LastTaskEntryCount > 0 && Window_TaskStack.Instance.GetShouldDrawThisFrame() )
                        {
                            float taskStackLeft = Window_TaskStack.GetMinXForTooltips();
                            float maxX = taskStackLeft - sizeDelta.x - 0.1f;
                            if ( worldSpacePoint.x > maxX )
                                worldSpacePoint.x = maxX;
                        }
                        else
                        {
                            if ( Window_Sidebar.Instance.GetShouldDrawThisFrame() )
                            {
                                float sidebarLeft = Window_Sidebar.GetMaxXForTooltips();
                                float maxX = sidebarLeft - sizeDelta.x - 0.1f;
                                if ( worldSpacePoint.x > maxX )
                                    worldSpacePoint.x = maxX;
                            }
                        }
                    }
                    break;
                case TooltipExtraRules.MustBeAboveBottomLeftUnitPanels:
                    {
                        if ( Window_ActorSidebarStatsLowerLeft.TryGetTopYForOtherWindowOffsets( out float MinY ) )
                        {
                            float bottom = worldSpacePoint.y - sizeDelta.y - 0.6f;
                            if ( bottom < MinY )
                                worldSpacePoint.y = MinY + sizeDelta.y + 0.6f;
                        }
                    }
                    break;
            }
        }
        #endregion

        #region ApplyPanelInPosition
        public static void ApplyPanelInPosition( RectTransform relevantRect, ref float currentX, ref float currentY, bool MoveInX, bool MoveInY, float SOLE_WIDTH, float HEIGHT )
        {
            relevantRect.anchoredPosition = new Vector2( currentX, currentY );
            relevantRect.localScale = Vector3.one;
            relevantRect.localRotation = Quaternion.identity;
            relevantRect.sizeDelta = new Vector2( SOLE_WIDTH, HEIGHT );
            if ( MoveInY )
            {
                currentY -= HEIGHT;
            }

            if ( MoveInX )
            {
                currentX += SOLE_WIDTH;
            }
        }

        public static void ApplyPanelInPosition( RectTransform relevantRect, float currentX, float currentY, float SOLE_WIDTH, float HEIGHT )
        {
            relevantRect.anchoredPosition = new Vector2( currentX, currentY );
            relevantRect.localScale = Vector3.one;
            relevantRect.localRotation = Quaternion.identity;
            relevantRect.sizeDelta = new Vector2( SOLE_WIDTH, HEIGHT );
        }
        #endregion

        #region DrawAllActorBadgesAndPerks
        public static void DrawAllActorBadgesAndPerks( ISimMapActor actor, IBadgeIconFactory Factory, 
            float badgeX, float badgeY, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            MobileActorTypeDuringGameData actorDGD = null;
            if ( actor is ISimMapMobileActor mobile )
            {
                actorDGD = mobile.GetTypeDuringGameData();
            }

            if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
            {
                DrawAllActorTypeBadgesAndPerks( actorDGD, Factory, badgeX, badgeY, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
                return;
            }

            foreach ( ActorBadge badge in ActorBadgeTable.Instance.Rows )
            {
                if ( !actor.GetHasBadge( badge ) )
                    continue;
                DrawActorBadge( badge, actor, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            foreach ( ActorPerk perk in ActorPerkTable.Instance.Rows )
            {
                if ( !actor.GetHasPerk( perk ) )
                    continue;
                DrawActorPerk( perk, actor, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            foreach ( ActorFeat feat in ActorFeatTable.Instance.Rows )
            {
                if ( actor.GetFeatAmount( feat ) <= 0 )
                    continue;
                DrawActorFeat( feat, actor, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            foreach ( ActorStatus status in ActorStatusTable.Instance.Rows )
            {
                if ( actor.GetStatusIntensity( status ) <= 0 )
                    continue;
                DrawActorStatus( status, actor, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }
        }
        #endregion

        #region DrawAllActorTypeBadgesAndPerks
        public static void DrawAllActorTypeBadgesAndPerks( MobileActorTypeDuringGameData actorDGD, IBadgeIconFactory Factory,
            float badgeX, float badgeY, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool isFromCustomizeLoadout = Window_ActorCustomizeLoadout.Instance.IsOpen;

            foreach ( ActorBadge badge in ActorBadgeTable.Instance.Rows )
            {
                if ( isFromCustomizeLoadout )
                {
                    if ( !Window_ActorCustomizeLoadout.EffectiveBadges.ContainsKey( badge ) )
                        continue;
                }
                else
                {
                    if ( !actorDGD.EffectiveBadges.GetDisplayDict().ContainsKey( badge ) )
                        continue;
                }
                DrawActorBadge( badge, null, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            foreach ( ActorPerk perk in ActorPerkTable.Instance.Rows )
            {
                if ( isFromCustomizeLoadout )
                {
                    if ( !Window_ActorCustomizeLoadout.EffectivePerks.ContainsKey( perk ) )
                        continue;
                }
                else
                {
                    if ( !actorDGD.EffectivePerks.GetDisplayDict().ContainsKey( perk ) )
                        continue;
                }
                DrawActorPerk( perk, null, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            foreach ( ActorFeat feat in ActorFeatTable.Instance.Rows )
            {
                if ( isFromCustomizeLoadout )
                {
                    if ( Window_ActorCustomizeLoadout.EffectiveFeats[feat] <= 0 )
                        continue;
                }
                else
                {
                    if ( actorDGD.EffectiveFeats.GetDisplayDict()[feat] <= 0 )
                        continue;
                }
                DrawActorFeat( feat, null, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ShadowStyle, ExtraRules );
            }

            if ( isFromCustomizeLoadout )
            {
                if ( Window_ActorCustomizeLoadout.EffectiveCostsPerAttack.Count > 0 )
                {
                    //todo CostPerAttack
                }
            }
            else
            {
                if ( actorDGD.EffectiveCostsPerAttack.Count > 0 )
                {
                    //todo CostPerAttack
                }
            }

            //foreach ( ActorStatus status in ActorStatusTable.Instance.Rows )
            //{
            //    if ( actor.GetStatusIntensity( status ) <= 0 )
            //        continue;
            //    DrawActorStatus( status, null, actorDGD, ref badgeX, ref badgeY, Factory, badgeSize, XShiftPer, YShiftPer, Clamp, ExtraRules );
            //}
        }
        #endregion

        #region DrawActorBadge
        private static void DrawActorBadge( ActorBadge badge, ISimMapActor actorOrNull, MobileActorTypeDuringGameData actorDGD, ref float badgeX, ref float badgeY, 
            IBadgeIconFactory Factory, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool detailsWindowIsOpen = Window_ActorCustomizeLoadout.Instance?.IsOpen ?? false;

            bBadgeIconBase icon = Factory.GetNewBadgeIconBase();
            if ( icon != null )
            {
                icon.SetRelatedImage0SpriteIfNeeded( badge.Icon.GetSpriteForUI() );
                if ( icon.relatedImage0 != null )
                    icon.relatedImage0.color = badge.IconColor;
                icon.ApplyItemInPositionNoTextSizing( ref badgeX, ref badgeY, false, false, badgeSize, badgeSize, IgnoreSizeOption.SetToZero );
                icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            break;
                        case UIAction.HandleMouseover:
                            badge.WriteBadgeTooltipForActor( element, Clamp, ShadowStyle, actorOrNull, actorDGD, ExtraRules );
                            break;
                        case UIAction.OnClick:
                            //todo for sidebar click:
                            //if ( Window_PlayerInventory.Instance.IsOpen )
                            //    Window_PlayerInventory.Instance.Close();
                            //else
                            //    Window_PlayerInventory.Instance.Open();
                            break;
                    }
                } );

                badgeX += XShiftPer;
                badgeY -= YShiftPer;
            }
        }
        #endregion DrawActorBadge

        #region DrawActorPerk
        private static void DrawActorPerk( ActorPerk perk, ISimMapActor actorOrNull, MobileActorTypeDuringGameData actorDGD, ref float badgeX, ref float badgeY, 
            IBadgeIconFactory Factory, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool detailsWindowIsOpen = Window_ActorCustomizeLoadout.Instance?.IsOpen ?? false;

            bBadgeIconBase icon = Factory.GetNewBadgeIconBase();
            if ( icon != null )
            {
                icon.SetRelatedImage0SpriteIfNeeded( perk.Icon.GetSpriteForUI() );
                if ( icon.relatedImage0 != null )
                    icon.relatedImage0.color = perk.IconColor;
                icon.ApplyItemInPositionNoTextSizing( ref badgeX, ref badgeY, false, false, badgeSize, badgeSize, IgnoreSizeOption.SetToZero );
                icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            break;
                        case UIAction.HandleMouseover:
                            perk.WritePerkTooltipForActor( element, Clamp, ShadowStyle, actorOrNull, actorDGD, ExtraRules );
                            break;
                        case UIAction.OnClick:
                            //todo for sidebar click:
                            //if ( Window_PlayerInventory.Instance.IsOpen )
                            //    Window_PlayerInventory.Instance.Close();
                            //else
                            //    Window_PlayerInventory.Instance.Open();
                            break;
                    }
                } );

                badgeX += XShiftPer;
                badgeY -= YShiftPer;
            }
        }
        #endregion DrawActorPerk

        #region DrawActorFeat
        private static void DrawActorFeat( ActorFeat feat, ISimMapActor actorOrNull, MobileActorTypeDuringGameData actorDGD, ref float badgeX, ref float badgeY, 
            IBadgeIconFactory Factory, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool detailsWindowIsOpen = Window_ActorCustomizeLoadout.Instance?.IsOpen ?? false;

            bBadgeIconBase icon = Factory.GetNewBadgeIconBase();
            if ( icon != null )
            {
                icon.SetRelatedImage0SpriteIfNeeded( feat.Icon.GetSpriteForUI() );
                if ( icon.relatedImage0 != null )
                    icon.relatedImage0.color = feat.IconColor;
                icon.ApplyItemInPositionNoTextSizing( ref badgeX, ref badgeY, false, false, badgeSize, badgeSize, IgnoreSizeOption.SetToZero );
                icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            break;
                        case UIAction.HandleMouseover:
                            float featAmount = 0f;
                            if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                                featAmount = Window_ActorCustomizeLoadout.EffectiveFeats[feat];
                            else
                                featAmount = actorOrNull != null ? actorOrNull.GetFeatAmount( feat ) : actorDGD.EffectiveFeats.GetDisplayDict()[feat];

                            feat.WriteFeatTooltipForActor( element, Clamp, ShadowStyle, featAmount, ExtraRules );
                            break;
                        case UIAction.OnClick:
                            //todo for sidebar click:
                            //if ( Window_PlayerInventory.Instance.IsOpen )
                            //    Window_PlayerInventory.Instance.Close();
                            //else
                            //    Window_PlayerInventory.Instance.Open();
                            break;
                    }
                } );

                badgeX += XShiftPer;
                badgeY -= YShiftPer;
            }
        }
        #endregion DrawActorPerk

        #region DrawActorStatus
        private static void DrawActorStatus( ActorStatus status, ISimMapActor actor, ref float badgeX, ref float badgeY, 
            IBadgeIconFactory Factory, float badgeSize, float XShiftPer, float YShiftPer, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool detailsWindowIsOpen = Window_ActorCustomizeLoadout.Instance?.IsOpen ?? false;

            bBadgeIconBase icon = Factory.GetNewBadgeIconBase();
            if ( icon != null )
            {
                icon.SetRelatedImage0SpriteIfNeeded( status.Icon.GetSpriteForUI() );
                if ( icon.relatedImage0 != null )
                    icon.relatedImage0.color = status.IconColor;
                icon.ApplyItemInPositionNoTextSizing( ref badgeX, ref badgeY, false, false, badgeSize, badgeSize, IgnoreSizeOption.SetToZero );
                icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            break;
                        case UIAction.HandleMouseover:
                            status.WriteStatusTooltipForActor( element, Clamp, ShadowStyle, actor, ExtraRules );
                            break;
                        case UIAction.OnClick:
                            //todo for sidebar click:
                            //if ( Window_PlayerInventory.Instance.IsOpen )
                            //    Window_PlayerInventory.Instance.Close();
                            //else
                            //    Window_PlayerInventory.Instance.Open();
                            break;
                    }
                } );

                badgeX += XShiftPer;
                badgeY -= YShiftPer;
            }
        }
        #endregion DrawActorStatus

        #region DrawActorStats
        public static void DrawActorStats( ISimMapActor actor, TerritoryControlType territoryControlOrNull, IStatsIconBaseFactory Factory, ref float cumulativeStatStatSize,
            ref float statsX, ref float statsY, float statWidth, float statHeight, float xSpacing, float ySpacing, int MaxRows, ref int currentCol, ref int currentRow, 
            SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bool isInDanger = actor.OutcastLevel > 0;
            currentCol = 0;
            currentRow = 0;
            float initialY = statsY;

            foreach ( ActorDataType dataType in ActorDataTypeTable.Instance.Rows )
            {
                if ( territoryControlOrNull != null && !dataType.IsShownOnTerritoryControl )
                    continue;
                if ( !dataType.DuringGameplay_GetShouldBeVisible() )
                    continue;

                MapActorData data = actor.ActorData[dataType];

                if ( data == null )
                {
                    if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                    {
                        Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out int netChange );
                        if ( netChange > 0 )
                            DrawActorEntityData( dataType, null, actor, ref statsX, ref statsY, ref cumulativeStatStatSize, ref currentCol, ref currentRow, Factory,
                                statWidth, statHeight, xSpacing, ySpacing, MaxRows, initialY, Clamp, ShadowStyle, ExtraRules );
                        else
                        {
                            Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out int pendingChange );
                            if ( pendingChange > 0 )
                                DrawActorEntityData( dataType, null, actor, ref statsX, ref statsY, ref cumulativeStatStatSize, ref currentCol, ref currentRow, Factory,
                                    statWidth, statHeight, xSpacing, ySpacing, MaxRows, initialY, Clamp, ShadowStyle, ExtraRules );
                        }
                    }
                    continue;
                }

                if ( dataType.AlsoVisibleWhenInDanger && isInDanger )
                { } //draw!
                else
                {
                    int currentVal = data.Current;
                    if ( (dataType.OnlyVisibleWhenBelow <= currentVal || dataType.OnlyVisibleWhenAbove >= currentVal) )///&& !data.Trends.GetHasAnyDataFromLastFewTurns() )
                    {
                        if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                        {
                            Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out int netChange );
                            if ( netChange > 0 )
                                DrawActorEntityData( dataType, null, actor, ref statsX, ref statsY, ref cumulativeStatStatSize, ref currentCol, ref currentRow, Factory,
                                    statWidth, statHeight, xSpacing, ySpacing, MaxRows, initialY, Clamp, ShadowStyle, ExtraRules );
                            else
                            {
                                Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out int pendingChange );
                                if ( pendingChange > 0 )
                                    DrawActorEntityData( dataType, null, actor, ref statsX, ref statsY, ref cumulativeStatStatSize, ref currentCol, ref currentRow, Factory,
                                        statWidth, statHeight, xSpacing, ySpacing, MaxRows, initialY, Clamp, ShadowStyle, ExtraRules );
                            }
                        }
                        continue;
                    }
                    //if ( dataType.OnlyVisibleWhenMissingSomeOrExpanded && !isShowingExtended && data.LostFromMax <= 0 && !detailsWindowIsOpen )
                    //    continue;
                }
                DrawActorEntityData( dataType, data, actor, ref statsX, ref statsY, ref cumulativeStatStatSize, ref currentCol, ref currentRow, Factory,
                    statWidth, statHeight, xSpacing, ySpacing, MaxRows, initialY, Clamp, ShadowStyle, ExtraRules );
            }
        }
        #endregion

        #region DrawActorEntityData
        private static void DrawActorEntityData( ActorDataType dataType, MapActorData dataOrNull, ISimMapActor actor,
            ref float currentX, ref float currentY, ref float cumulativeStatStatHeight, ref int currentCol, ref int currentRow, IStatsIconBaseFactory Factory,
            float statWidth, float statHeight, float xSpacing, float ySpacing, int MaxRows, float initialY, SideClamp Clamp, TooltipShadowStyle ShadowStyle, TooltipExtraRules ExtraRules )
        {
            bStatsIconBase icon = Factory.GetNewStatsIconBase();
            if ( icon != null )
            {
                if ( cumulativeStatStatHeight > 0 )
                    cumulativeStatStatHeight += ySpacing;

                //float popupY = currentY + POPUP_OFFSET_Y;
                icon.SetSpriteIfNeeded( dataType.Icon.GetSpriteForUI() );
                if ( icon.image != null )
                    icon.image.color = dataOrNull?.CalculateIconColor( true ) ?? ColorMath.White;
                icon.ApplyItemInPositionNoTextSizing( ref currentX, ref currentY, false, false, statWidth, statHeight, IgnoreSizeOption.IgnoreSize );
                icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int pendingChange = 0;
                    int netChange = 0;
                    if ( Window_ActorCustomizeLoadout.Instance.IsOpen )
                    {
                        Window_ActorCustomizeLoadout.NetProposedStatChanges.TryGetValue( dataType, out netChange );
                        Window_ActorCustomizeLoadout.NetPendingStatChanges.TryGetValue( dataType, out pendingChange );
                    }

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            int currentVal = dataOrNull?.Current ?? 0;
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
                                string colorHex = dataOrNull?.CalculateSidebarIconColorHex( false ) ?? string.Empty;
                                if ( dataType.ShowTwoLineSidebarEntryWithPercentOutOfMax && dataOrNull != null )
                                {
                                    int max = dataOrNull.Maximum;
                                    if ( max < 1 )
                                        max = 1;
                                    int percentage = Mathf.FloorToInt( ((float)currentVal / (float)max) * 100f );
                                    if ( percentage == 0 && currentVal > 0 )
                                        percentage = 1;

                                    ExtraData.Buffer.StartSize80().AddRaw( percentage.ToStringIntPercent(), colorHex ).Space3x();

                                    ExtraData.Buffer.AddRaw( dataType.GetNumberAsFormattedStringForSidebar( max ), dataType.SidebarIconColorHex );
                                }
                                else
                                    ExtraData.Buffer.AddRaw( dataType.GetNumberAsFormattedStringForSidebar( currentVal ), colorHex );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            if ( dataOrNull != null )
                            {
                                dataOrNull.WriteActorDataTooltip( element, Clamp, ShadowStyle, ExtraRules, actor );
                            }
                            else
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartBasicTooltip( TooltipID.Create( dataType ), element, Clamp, TooltipNovelWidth.Simple, 
                                    TooltipExtraText.None, ExtraRules ) )
                                {
                                    novel.Icon = dataType.Icon;
                                    novel.TitleUpperLeft.AddRaw( dataType.GetDisplayName() );
                                    novel.TitleLowerLeft.AddLang( actor is MachineStructure ? "StructureStat_Subheader" : "UnitStat_Subheader" );

                                    if ( actor is ISimMapMobileActor mobileActor )
                                    {
                                        MobileActorTypeDuringGameData actorDGD = mobileActor.GetTypeDuringGameData();
                                        if ( actorDGD != null )
                                        {
                                            Pair<int, int> minMax = actorDGD.NonSimActorDataMinMax.GetDisplayDict()[dataType];

                                            if ( minMax.LeftItem == minMax.RightItem )
                                                novel.TitleUpperRight.AddRaw( minMax.LeftItem.ToString() );
                                            else
                                                novel.TitleUpperRight.AddFormat2( "MinMaxRange", minMax.LeftItem.ToString(), minMax.RightItem.ToString() );
                                        }
                                    }

                                    if ( !dataType.GetDescription().IsEmpty() )
                                        novel.Main.AddRaw( dataType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                    if ( dataType.StrategyTipOptional.Text.Length > 0 )
                                        novel.Main.AddRaw( dataType.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            //todo for sidebar click:
                            //if ( Window_PlayerInventory.Instance.IsOpen )
                            //    Window_PlayerInventory.Instance.Close();
                            //else
                            //    Window_PlayerInventory.Instance.Open();
                            break;
                    }
                } );

                //float popupX = STATS_ICON_X + POPUP_OFFSET_X;
                //if ( changeSetOrNull != null )
                //    UIHelper.RenderTrendPopups( bHeaderTextPopupPool, changeSetOrNull, dataType.GetDisplayName(), ref popupX, ref popupY, false, FourDirection.East,
                //        TrendPopupSize.Normal );

                currentY -= (statHeight + ySpacing);
                cumulativeStatStatHeight += statHeight;

                currentRow++;
                if ( MaxRows > 0 && currentRow >= MaxRows )
                {
                    currentRow = 0;
                    currentY = initialY;
                    currentX += statWidth + xSpacing;
                    currentCol++;
                }
            }
        }
        #endregion DrawActorEntityData

        #region SetSmallOptionBGAndGetColor
        public static string SetSmallOptionBGAndGetColor( ButtonAbstractBaseWithImage Img, bool IsSelected, bool IsBlocked )
        {
            Img.SetSpriteIfNeeded( Img.Element.RelatedSprites[IsBlocked ? 2 : ( IsSelected ? 1 : 0)] );

            return IsBlocked ? ColorTheme.SmallPopupTextBlocked : (IsSelected ? ColorTheme.SmallPopupTextSelected : ColorTheme.SmallPopupTextNormal);
        }
        #endregion
    }

    public interface IAssignableGetOrSetUIData
    {
        void Assign( GetOrSetUIData UIDataController );
    }

    public enum TrendPopupSize
    {
        Small,
        Normal
    }
}

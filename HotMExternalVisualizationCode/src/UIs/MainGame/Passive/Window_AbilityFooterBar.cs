using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Window_AbilityFooterBar : WindowControllerAbstractBase
    {
        public static Window_AbilityFooterBar Instance;
		
		public Window_AbilityFooterBar()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !(Engine_HotM.SelectedActor is ISimMachineActor machineActor ) )
                return false; //if nothing selected, or selected not-a-machine-actor, don't show

            if ( machineActor.IsFullDead )
                return false; //if the machine actor is dead or downed, don't show the ability bar

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
            if ( Engine_HotM.SelectedMachineActionMode != null )
                return false;
            if ( !FlagRefs.UITour6_UnitSidebar.DuringGameplay_IsTripped ) //the one directly prior to this one
                return false;
            if ( Window_ActorCustomizeLoadout.Instance.IsOpen ) //the one directly prior to this one
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
            return SizingRect.GetWorldSpaceTopRightCorner().x;
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

        private static DrawAs lastDraw = DrawAs.A;

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            switch ( lastDraw )
            {
                case DrawAs.B:
                    height += height;
                    break;
                case DrawAs.C:
                    height += (height + height );
                    break;
            }

            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        #region TryGetTopYForOtherWindowOffsets
        public static bool TryGetTopYForOtherWindowOffsets( out float MinY )
        {
            MinY = 0;
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return false; //hidden entirely!

            MinY = SizingRect.GetWorldSpaceMinYAndMaxY( 0 ).x;
            return true;
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

        #region GetAbilityBarCurrentHeight_Scaled
        /// <summary>
        /// Gets the amount of vertical space the ability bar will be taking up,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetAbilityBarCurrentHeight_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            float height = 35f;
            switch (CurrentDraw)
            {
                case DrawAs.B:
                    height = 71f;
                    break;
                case DrawAs.C:
                    height = 106f;
                    break;
            }


            return height * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        #region GetWorldScaleTrio
        public static Vector3 GetWorldScaleTrio()
        {
            //this converts all of these into world space, without any issues due to scaling, etc
            return (SizingRect.localToWorldMatrix * new Vector3( A_Size.x, B_Size.x, C_Size.x ));
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bActionIcon> bActionIconPool;

        public const float ACTION_ICON_SIZE = 33.5f;

        public static DrawAs CurrentDraw = DrawAs.A;

        public static Vector2 A_Size = new Vector2( 332.1f, 43.79f );
        public static Vector2 B_Size = new Vector2( 191.7f, 79.4f );
        public static Vector2 C_Size = new Vector2( 121.9f, 114.2f );

        public static readonly Vector2[] A_Positions = new Vector2[]
        {
            new Vector2( 4.936f, 39.46f ), new Vector2( 40.15f, 39.46f ), new Vector2( 75.16f, 39.46f ),
            new Vector2( 110.19f, 39.46f ), new Vector2( 145.3f, 39.46f ), new Vector2( 180.38f, 39.46f ),
            new Vector2( 215.47f, 39.46f ), new Vector2( 250.59f, 39.46f ), new Vector2( 285.58f, 39.46f ),
        };

        public static readonly Vector2[] B_Positions = new Vector2[]
        {
            new Vector2( 4.936f, 74.95f ), new Vector2( 40.15f, 74.95f ), new Vector2( 75.16f, 74.95f ), new Vector2( 110.19f, 74.95f ), //new Vector2( 145.3f, 74.95f ),
            new Vector2( 4.936f, 39.46f ), new Vector2( 40.15f, 39.46f ), new Vector2( 75.16f, 39.46f ), new Vector2( 110.19f, 39.46f ), new Vector2( 145.3f, 39.46f ),
        };

        public static readonly Vector2[] C_Positions = new Vector2[]
        {
            new Vector2( 4.936f, 109.95f ), new Vector2( 40.15f, 109.95f ), new Vector2( 75.16f, 109.95f ),
            new Vector2( 4.936f, 74.95f ), new Vector2( 40.15f, 74.95f ), new Vector2( 75.16f, 74.95f ),
            new Vector2( 4.936f, 39.46f ), new Vector2( 40.15f, 39.46f ), new Vector2( 75.16f, 39.46f ),
        };

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

                //float offsetFromRight = -Window_RadialMenu.Instance.GetRadialMenuCurrentWidth_Scaled();
                //if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                //    offsetFromRight -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetXRaw = Window_ActorSidebarStatsLowerLeft.GetXWidthForOtherWindowOffsets();

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bActionIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bActionIconPool = new ButtonAbstractBase.ButtonPool<bActionIcon>( bActionIcon.Original, 10, "bActionIcon" );
                    }
                }
                #endregion

                this.UpdateIcons();

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour7_AbilityBar && !this.GetShouldBeHidden() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "UITour_AbilityBar_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_AbilityBar_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomTextLast", 7, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour7_AbilityBar", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.BottomLeft );
                }
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bActionIconPool.Clear( 5 );

                ISimMachineActor actor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( actor == null )
                    return;

                if ( displayInfos == null )
                    displayInfos = new AbilitySlotDisplayInfo[AbilitySlotInfo.MAX_SLOT_OVERALL + 1];

                #region Calculate desiredDraw
                if ( !SizingRect )
                    SizingRect = this.Element.RelevantRect;

                float startingX = SizingRect.GetWorldSpaceBottomLeftCorner().x;
                float radialX = Window_RadialMenu.GetLeftPositionForOtherWindows();

                Vector3 trio = GetWorldScaleTrio();

                float rightA = startingX + trio.x;
                float rightB = startingX + trio.y;
                float rightC = startingX + trio.z;

                DrawAs desiredDraw = DrawAs.A;
                if ( rightA < radialX )
                    desiredDraw = DrawAs.A;
                else if ( rightB < radialX )
                    desiredDraw = DrawAs.B;
                else
                    desiredDraw = DrawAs.C;
                #endregion

                bool isVehicle = Engine_HotM.SelectedActor is ISimMachineVehicle;
                int maxSlot = isVehicle ? AbilitySlotInfo.MAX_SLOT_VEHICLES : AbilitySlotInfo.MAX_SLOT_UNITS;
                int actionSlots = maxSlot + 1 - AbilitySlotInfo.MIN_SLOT;

                Vector2[] positions = A_Positions;
                switch ( desiredDraw )
                {
                    case DrawAs.B:
                        positions = B_Positions;
                        break;
                    case DrawAs.C:
                        positions = C_Positions;
                        break;
                }

                lastDraw = desiredDraw;

                for ( int i = AbilitySlotInfo.MIN_SLOT; i <= maxSlot; i++ )
                    DrawActionIcon( i, actor, positions );

                #region Expand or Shrink Size Of This Window
                if ( desiredDraw != CurrentDraw )
                {
                    CurrentDraw = desiredDraw;
                    SizingRect.anchorMin = new Vector2( 0f, 0f );
                    SizingRect.anchorMax = new Vector2( 0f, 0f );
                    SizingRect.pivot = new Vector2( 0f, 0f );
                    switch (CurrentDraw)
                    {
                        case DrawAs.A:
                            SizingRect.UI_SetWidth( A_Size.x );
                            SizingRect.UI_SetHeight( A_Size.y );
                            this.Element.RelatedTransforms[0].gameObject.SetActive( true );
                            this.Element.RelatedTransforms[1].gameObject.SetActive( false );
                            this.Element.RelatedTransforms[2].gameObject.SetActive( false );
                            break;
                        case DrawAs.B:
                            SizingRect.UI_SetWidth( B_Size.x );
                            SizingRect.UI_SetHeight( B_Size.y );
                            this.Element.RelatedTransforms[0].gameObject.SetActive( false );
                            this.Element.RelatedTransforms[1].gameObject.SetActive( true );
                            this.Element.RelatedTransforms[2].gameObject.SetActive( false );
                            break;
                        case DrawAs.C:
                            SizingRect.UI_SetWidth( C_Size.x );
                            SizingRect.UI_SetHeight( C_Size.y );
                            this.Element.RelatedTransforms[0].gameObject.SetActive( false );
                            this.Element.RelatedTransforms[1].gameObject.SetActive( false );
                            this.Element.RelatedTransforms[2].gameObject.SetActive( true );
                            break;
                    }
                }
                #endregion
            }
            private AbilitySlotDisplayInfo[] displayInfos;

            private static ArcenDoubleCharacterBuffer extraMainBuffer = new ArcenDoubleCharacterBuffer( "Action-extraMainBuffer" );
            private static ArcenDoubleCharacterBuffer extraFrameBuffer = new ArcenDoubleCharacterBuffer( "Action-extraFrameBuffer" );

            #region DrawActionIcon
            private void DrawActionIcon( int SlotIndex, ISimMachineActor actor, Vector2[] positions )
            {
                //TrendChangeSet changeSet = data.Trends;

                bActionIcon icon = bActionIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    //float popupY = currentY + POPUP_OFFSET_Y;
                    if ( displayInfos[SlotIndex] == null )
                        displayInfos[SlotIndex] = new AbilitySlotDisplayInfo( icon );


                    {
                        AbilityType ability = actor?.GetActorAbilityInSlotIndex( SlotIndex );
                        bool isPerformable = ability == null || actor.GetActorAbilityCanBePerformedNow( ability, null );
                        displayInfos[SlotIndex].UpdateFromAbility( ability, isPerformable, ability != null &&
                            ( actor.IsInAbilityTypeTargetingMode == ability || 
                            ( actor.CurrentStandby != StandbyType.None && ability.IsStandbyControls ) ||
                            Window_AbilityOptionList.GetIsOpenAndFocusedOn( ability ) ) && isPerformable,
                            ability?.DuringGame_IsHiddenByFlags()??false );

                        ////done this way so that just being out of AP or ME doesn't cause this to deselect.
                        ////but this is required so that if you are in force-speech mode and there are no speakers nearby, you don't get trapped in that mode, for one example.
                        //if ( ability != null && actor.IsInAbilityTypeTargetingMode == ability )
                        //{
                        //    if ( !ability.CalculateIfActorCanUseThisAbilityRightNow( actor.GetTypeDuringGameData(), null, false, false ) )
                        //        actor.SetTargetingMode( null, null );
                        //    else
                        //    {
                        //        ActorAbilityResult result = ability.Implementation.TryHandleAbility( actor, null, actor.GetDrawLocation(), ability, null,
                        //            ActorAbilityLogic.CalculateIfAbilityBlocked );

                        //        if ( result == ActorAbilityResult.PlayErrorSound )
                        //            actor.SetTargetingMode( null, null );
                        //    }
                        //}
                    }
                    Vector2 iconPos = positions[SlotIndex - 1];
                    float iconX = iconPos.x;
                    float iconY = iconPos.y;
                    icon.ApplyItemInPositionNoTextSizing( ref iconX, ref iconY, false, false, ACTION_ICON_SIZE, ACTION_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        AbilityType ability = actor?.GetActorAbilityInSlotIndex( SlotIndex );
                        bool isPerformable = ability == null || actor.GetActorAbilityCanBePerformedNow( ability, null );

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( Window_VehicleUnitPanel.Instance.IsOpen )
                                    ExtraData.Buffer.AddRaw( "-", ColorRefs.AbilityBarDisabledNumberColor.ColorHex );
                                else if ( ability == null )
                                    ExtraData.Buffer.AddRaw( SlotIndex.ToString(), ColorRefs.AbilityBarEmptyNumberColor.ColorHex );
                                else  if ( ability.DuringGame_IsHiddenByFlags() )
                                    ExtraData.Buffer.AddRaw( SlotIndex.ToString(), ColorRefs.AbilityBarLockedNumberColor.ColorHex );
                                else
                                    ExtraData.Buffer.AddRaw( SlotIndex.ToString(), isPerformable ? ability.AbilityNumberColorHex : ColorRefs.AbilityBarDisabledNumberColor.ColorHex );
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( ability == null )
                                        return;
                                    if ( Window_AbilityOptionList.GetIsOpenAndFocusedOn( ability ) )
                                        return;

                                    //if ( SizingRect )
                                    //{
                                    //    float startingX = SizingRect.GetWorldSpaceBottomLeftCorner().x;
                                    //    float radialX = Window_RadialMenu.GetLeftPositionForOtherWindows();

                                    //    Vector3 trio = GetWorldScaleTrio();

                                    //    float rightA = startingX + trio.x;
                                    //    float rightB = startingX + trio.y;
                                    //    float rightC = startingX + trio.z;

                                    //    ExtraData.Buffer.AddRaw( "startingX: " + startingX + " radialX: " + radialX + 
                                    //        "\n myWidthWorld: " + myWidthWorld + " rightA: " + rightA +
                                    //        "\n rightB: " + rightB + " rightC: " + rightC );

                                    //    break;
                                    //}

                                    if ( ability?.DuringGame_IsHiddenByFlags()??false )
                                    {
                                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( actor ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple, TooltipExtraText.None,
                                            TooltipExtraRules.MustBeToLeftOfAbilityFooter ) )
                                        {
                                            novel.Icon = IconRefs.AbilityLocked.Icon;
                                            novel.TitleUpperLeft.AddLang( "Locked" );
                                            novel.TitleLowerLeft.AddLang( "Ability_Prefix" );
                                            novel.Main.AddLang( "AbilityLocked_Details" );
                                        }

                                        break;
                                    }

                                    extraMainBuffer.EnsureResetForNextUpdate();
                                    extraFrameBuffer.EnsureResetForNextUpdate();
                                    if ( !isPerformable )
                                    {
                                        extraFrameBuffer.AddLangAndAfterLineItemHeader( "AbilityUnavailable", ColorTheme.RedOrange );
                                        extraFrameBuffer.StartColor( ColorTheme.RedOrange_Lighter );
                                        if ( ability != null && actor != null )
                                            actor.GetActorAbilityCanBePerformedNow( ability, extraFrameBuffer );
                                        ability?.Implementation?.TryHandleAbility( actor, (actor as ISimMachineUnit)?.ContainerLocation.Get() as ISimBuilding, 
                                            actor.GetDrawLocation(), ability, extraFrameBuffer, ActorAbilityLogic.AppendToUsageBlockedTooltip );
                                        extraFrameBuffer.EndColor().Line();
                                    }
                                    else
                                    {
                                        if ( !(ability?.SkipDrawThreatLinesWhenPlayerHovers ?? false) && ((actor as ISimMachineUnit)?.GetIsDeployed() ?? true) )
                                        {
                                            EnemyTargetingReason reason = EnemyTargetingReason.CurrentLocation_NoOp;
                                            if ( (ability?.IsConsideredToAddCloakingForThreatLines ?? false) )
                                                reason = EnemyTargetingReason.CurrentLocation_PlusCloaking;
                                            else if ( (ability?.IsConsideredToAddTakeCoverForThreatLines ?? false) )
                                                reason = EnemyTargetingReason.CurrentLocation_PlusTakeCover;

                                            Vector3 center = actor.GetDrawLocation();
                                            MoveHelper.DrawThreatLinesAgainstMapMobileActor( actor, center.PlusY( actor.GetHalfHeightForCollisions() ), (actor as ISimMachineUnit)?.ContainerLocation.Get() as ISimBuilding,
                                                false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                                                out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting,
                                                out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                                                null, AttackAmounts.Zero(), reason, false, ThreatLineLogic.Normal );

                                            CombatTextHelper.AppendLastPredictedDamageLong( actor, extraMainBuffer, extraMainBuffer, true, false, false );
                                        }
                                    }

                                    ability.RenderAbilityTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraRules.MustBeToLeftOfAbilityFooter,
                                        AbilityTooltipInstruction.ForAbilityBar, SlotIndex, actor, actor.GetTypeDuringGameData(), TooltipExtraText.None, extraMainBuffer, extraFrameBuffer );                                    
                                }
                                break;
                            case UIAction.OnClick:
                                if ( actor != null && ability != null )
                                {
                                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped ) //not valid until this is complete
                                        break;
                                    if ( ability?.DuringGame_IsHiddenByFlags()??true )
                                        break;

                                    if ( Window_AbilityOptionList.CloseIfAlreadyTargetingThis( actor, actor.GetActorAbilityInSlotIndex( SlotIndex ) ) )
                                        break; //toggling it closed

                                    Window_AbilityOptionList.CloseIfOpen(); //if it was open and we're clicking something else, close it

                                    if ( !isPerformable )
                                        break;

                                    if ( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.LeftButtonDoubleClicked )
                                        actor.TryPerformActorAbilityInSlot( SlotIndex, TriggerStyle.DirectByPlayer );
                                    else
                                        actor.TryAltViewActorAbilityInSlot( SlotIndex );
                                }
                                break;
                        }
                    } );

                    //float popupX = STATS_ICON_X - UIHelper.POPUP_NUMBER_WIDTH + POPUP_OFFSET_X;
                    //UIHelper.RenderTrendPopups( bHeaderTextPopupPool, changeSet, dataType, popupX, popupY, FourDirection.West );

                    //currentX += ACTION_ICON_SIZE + ACTION_ICON_X_SPACING;
                }
            }
            #endregion DrawActionIcon
        }

        #region bActionIcon
        public class bActionIcon : ButtonAbstractBaseWithImage
        {
            public static bActionIcon Original;
            public bActionIcon() { if ( Original == null ) Original = this; }

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

        #region AbilitySlotDisplayInfo
        private class AbilitySlotDisplayInfo
        {
            public readonly bActionIcon ActionIcon;
            public readonly Image ClipImage;

            public readonly Image IconImage;
            private Material IconImageMaterial = null;

            public readonly Image BGImage;
            private Material BGImageMaterial = null;

            public readonly Sprite[] BGSpriteList;
            public readonly int BGMaxSpriteIndex;

            public readonly Image FrameImage;
            public readonly Image HighlightImage;
            public readonly Image LockedImage;
            public readonly Image DisabledImage;

            public AbilityType PriorDrawAbility = null;
            private bool hasSetFromNullPriorAbility = false;
            private int PriorNumberOfSelectDataReloads = 0;
            private bool PriorWasPerformable = false;
            private bool PriorWasHighlighted = false;
            private bool PriorWasLocked = false;
            private int LastReloadCount = 0;

            public AbilitySlotDisplayInfo( bActionIcon ActionIcon )
            {
                this.ActionIcon = ActionIcon;
                this.ClipImage = this.ActionIcon.Element.GetComponent<Image>();

                IconImage = ActionIcon.Element.RelatedImages[0];
                BGImage = ActionIcon.Element.RelatedImages[1];
                FrameImage = ActionIcon.Element.RelatedImages[2];
                HighlightImage = ActionIcon.Element.RelatedImages[3];
                LockedImage = ActionIcon.Element.RelatedImages[4];
                DisabledImage = ActionIcon.Element.RelatedImages[5];

                BGImageMaterial = new Material( BGImage.material ); //required so that each icon gets its own color
                BGImage.material = BGImageMaterial;

                BGSpriteList = ActionIcon.Element.RelatedSprites;
                BGMaxSpriteIndex = BGSpriteList.Length - 1;

                IconImageMaterial = new Material( IconImage.material ); //required so that each icon gets its own color
                IconImage.material = IconImageMaterial;
                IconImage.color = Color.black; //this makes it so that the glow is the only contributor
            }

            public void UpdateFromAbility( AbilityType Ability, bool IsPerformable, bool IsHighlighted, bool IsLocked )
            {
                if ( Ability != null )
                {
                    if ( Ability == PriorDrawAbility && PriorNumberOfSelectDataReloads == Engine_HotM.NumberOfSelectDataReloads && 
                        PriorWasPerformable == IsPerformable && PriorWasHighlighted == IsHighlighted && PriorWasLocked == IsLocked &&
                        LastReloadCount == Engine_HotM.NumberOfSelectDataReloads )
                    { } //skip, it's already fine
                    else
                    {
                        PriorDrawAbility = Ability;
                        hasSetFromNullPriorAbility = false;
                        PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                        PriorWasPerformable = IsPerformable;
                        PriorWasHighlighted = IsHighlighted;
                        PriorWasLocked = IsLocked;
                        LastReloadCount = Engine_HotM.NumberOfSelectDataReloads;
                        this.ClipImage.color = Color.white;

                        //note: this should not be required, this is overkill.  But otherwise they don't update once set a single time.
                        Material oldMat = BGImageMaterial;
                        BGImageMaterial = new Material( oldMat );
                        Material.Destroy( oldMat );

                        BGImageMaterial.SetFloat( "_HsvShift", Ability.AbilityBackgroundHueShift );
                        BGImageMaterial.SetFloat( "_HsvSaturation", ( IsLocked ? MathRefs.LockedAbilityBackgroundSaturationShift.FloatMin : Ability.AbilityBackgroundSaturation ) );

                        if ( IsPerformable && !IsLocked )
                        {
                            oldMat = IconImageMaterial;
                            IconImageMaterial = new Material( oldMat );
                            Material.Destroy( oldMat );
                            IconImage.material = IconImageMaterial;
                            IconImage.color = Color.black; //this makes it so that the glow is the only contributor

                            IconImageMaterial.SetColor( "_GlowColor", Ability.AbilityGlowColor );
                            IconImageMaterial.SetFloat( "_Glow", Ability.AbilityGlowIntensity );
                            BGImageMaterial.SetFloat( "_HsvBright", Ability.AbilityBackgroundBrightness );
                            DisabledImage.enabled = false;
                        }
                        else
                        {
                            IconImage.material = null;
                            IconImage.color = ColorRefs.AbilityBarDisabledIconColor.ColorWithoutHDR;
                            BGImageMaterial.SetFloat( "_HsvBright", Ability.AbilityBackgroundBrightness * ( IsLocked ? MathRefs.LockedAbilityBackgroundBrightness.FloatMin : 1 ) );
                            DisabledImage.enabled = true;
                            DisabledImage.color = ColorRefs.AbilityBarDisabledBackgroundColor.ColorWithoutHDR;
                        }
                        BGImage.sprite = BGSpriteList[Mathf.Clamp( 0, IsLocked ? MathRefs.LockedAbilityBackgroundIndex.IntMin : Ability.AbilityBackgroundIndex, BGMaxSpriteIndex )];
                        BGImage.material = BGImageMaterial;

                        if ( IsLocked )
                        {
                            IconImage.enabled = false;
                            LockedImage.enabled = true;
                            FrameImage.color = ColorRefs.AbilityBarLockedBorderColor.ColorHDR;
                        }
                        else
                        {
                            IconImage.enabled = true;
                            LockedImage.enabled = false;
                            IconImage.sprite = Ability.Icon.GetSpriteForUI();
                            FrameImage.color = IsPerformable ? Ability.AbilityBorderColor : ColorRefs.AbilityBarDisabledBorderColor.ColorHDR;
                        }
                        BGImage.enabled = true;
                        HighlightImage.enabled = IsHighlighted;
                    }
                }
                else
                {
                    if ( hasSetFromNullPriorAbility && PriorNumberOfSelectDataReloads == Engine_HotM.NumberOfSelectDataReloads )
                    { } //skip, it's already fine
                    else
                    {
                        PriorDrawAbility = null;
                        hasSetFromNullPriorAbility = true;
                        PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                        //icon.SetSpriteIfNeeded( ability.Icon.GetSpriteForUI() );
                        this.ClipImage.color = ColorMath.Transparent;

                        IconImage.enabled = false;
                        BGImage.enabled = false;
                        HighlightImage.enabled = false;
                        LockedImage.enabled = false;
                        DisabledImage.enabled = false;
                        FrameImage.color = ColorRefs.AbilityBarEmptyBorderColor.ColorHDR;
                    }
                }
            }
        }
        #endregion AbilitySlotDisplayInfo

        public enum DrawAs
        {
            A = 0,
            B,
            C
        }
    }
}

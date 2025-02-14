using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ActorSidebarStatsLowerLeft : WindowControllerAbstractBase, IBadgeIconFactory, IStatsIconBaseFactory
    {
        public static Window_ActorSidebarStatsLowerLeft Instance;
		
		public Window_ActorSidebarStatsLowerLeft()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.SelectedActor == null )
                return false; //if nothing selected, don't show
            if ( VisCurrent.IsShowingActualEvent )
                return false; //don't show when in a city event

            if ( Window_MainGameHeaderBarLeft.Instance == null )
                return false;
            if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                return false; //if the header bar is not showing for whatever reason, then also don't show ourselves
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
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( !FlagRefs.UITour5_Toast.DuringGameplay_IsTripped )
                return false;
            if ( (Window_ActorCustomizeLoadout.Instance?.GetShouldDrawThisFrame() ?? false) )
                return false; //stop drawing when the customize loadout is going to draw

            if ( Window_CommandFooter.Instance.GetShouldDrawThisFrame()) 
                return false;
            if ( Window_Debate.Instance?.IsOpen ?? false )
                return false;

            return true;
        }

        #region GetCurrentHeight_Scaled
        /// <summary>
        /// Gets the amount of vertical space the this will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetCurrentHeight_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 100f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
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

        #region TryGetHeightForOtherWindowOffsets
        public static bool TryGetHeightForOtherWindowOffsets( out float Height )
        {
            Height = 0;
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return false; //hidden entirely!

            Vector2 size = SizingRect.GetWorldSpaceMinYAndMaxY( 0 );

            Height = size.x - size.y;
            return true;
        }
        #endregion

        #region GetXWidthForOtherWindowOffsets
        public static float GetXWidthForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            float width = SizingRect.GetWorldSpaceSize().x;
            return width;
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
                ISimMapActor actor = Engine_HotM.SelectedActor;
                if ( actor != null )
                {
                    if ( actor is MachineStructure structure )
                    {
                        TerritoryControlType controlType = structure.TerritoryControlType;
                        if ( controlType != null )
                        {
                            Buffer.AddSpriteStyled_NoIndent( controlType.Resource.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                            Buffer.AddRaw( controlType.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );
                        }
                        else
                        {
                            MachineJob job = structure.CurrentJob;
                            if ( job != null )
                            {
                                //Buffer.AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                Buffer.AddRaw( job.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );
                            }
                            else
                            {
                                Buffer.AddRaw( structure.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );
                            }
                        }
                    }
                    else
                        Buffer.AddRaw( actor.GetDisplayName() );
                }
            }

            public override void OnUpdateSub()
            {
                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour6_UnitSidebar && !this.GetShouldBeHidden() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddFormat1WithFirstLineBold( "UITour_UnitSidebar_Text1",
                        InputActionTypeDataTable.Instance.GetRowByID( "Cancel" ).GetHumanReadableKeyCombo() )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_UnitSidebar_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 6, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour6_UnitSidebar", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.BottomLeft );
                }
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                ISimMapActor actor = Engine_HotM.SelectedActor;
                if ( actor != null )
                {
                    if ( actor is MachineStructure structure )
                        Buffer.AddRaw( structure.Type.GetDisplayName() ); //otherwise it would show the job name
                    else
                    {
                        if ( actor is ISimMachineUnit mUnit )
                        {
                            if ( mUnit.UnitType?.PrimaryArchetypeCollection != null )
                                Buffer.AddRaw( mUnit.UnitType?.PrimaryArchetypeCollection?.GetDisplayName()??string.Empty );
                            else
                            {
                                if ( mUnit.UnitType.IsConsideredMech )
                                    Buffer.AddLang( "StandardMech" );
                                else
                                    Buffer.AddLang( "StandardAndroid" );
                            }
                        }
                        else if ( actor is ISimMachineVehicle mVehicle )
                            Buffer.AddLang( "StandardVehicle" );
                        else if ( actor is ISimNPCUnit npc )
                        {
                            if ( npc.UnitType.CostsToCreateIfBulkAndroid.Count > 0 )
                                Buffer.AddLang( "BulkAndroid" );
                            else if ( npc.GetIsPlayerControlled() && npc.UnitType.CapturedUnitCapacityRequired > 0 )
                                Buffer.AddLang( "CapturedUnit" );
                            else if ( npc.GetIsPartOfPlayerForcesInAnyWay() )
                                Buffer.AddLang( "AlliedUnit" );
                            else
                                Buffer.AddLang( "ThirdPartyUnit" );
                        }
                    }
                }
            }

            public override void HandleMouseover()
            {
                ISimMapActor mapActor = Engine_HotM.SelectedActor;
                if ( mapActor != null )
                    mapActor.RenderTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
            }

            public override bool GetShouldBeHidden()
            {
                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor == null )
                {
                    ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                    if ( npcUnit == null )
                        return true;
                }
                return false;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ISimMapMobileActor mobileActor = Engine_HotM.SelectedActor as ISimMapMobileActor;
                if ( mobileActor == null || (mobileActor is ISimNPCUnit npcUnit && !npcUnit.GetIsPlayerControlled()) )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Window_ActorCustomizeLoadout.Instance.ToggleOpen();
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

            private IA5Sprite lastSecondarySprite = null;
            private BuildingMarkerColor lastSecondarySpriteColor = null;

            public override void OnUpdateSubSub() 
            {
                VisColorUsage desiredTooltipIconColorStyle = null;
                ISimMachineUnit machineUnit = Engine_HotM.SelectedActor as ISimMachineUnit;
                IA5Sprite secondarySprite = null;
                BuildingMarkerColor secondarySpriteColor = null;

                if ( machineUnit != null )
                {
                    MachineUnitType unitType = machineUnit?.UnitType;
                    if ( unitType == null )
                        return;
                    Instance.SetSpriteIfNeeded( unitType.TooltipIcon.GetSpriteForUI() );
                    desiredTooltipIconColorStyle = unitType.TooltipIconColorStyle;
                }
                else
                {
                    ISimMachineVehicle vehicle = Engine_HotM.SelectedActor as ISimMachineVehicle;
                    if ( vehicle != null )
                    {
                        MachineVehicleType vehicleType = vehicle?.VehicleType;
                        if ( vehicleType == null )
                            return;
                        Instance.SetSpriteIfNeeded( vehicleType.TooltipIcon.GetSpriteForUI() );
                        desiredTooltipIconColorStyle = vehicleType.TooltipIconColorStyle;
                    }
                    else
                    {
                        ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                        if ( npcUnit != null )
                        {
                            Instance.SetSpriteIfNeeded( npcUnit.GetTooltipIcon()?.GetSpriteForUI() );
                            desiredTooltipIconColorStyle = npcUnit.GetTooltipIconColorStyle();
                        }
                        else
                        {
                            MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                            if ( structure != null )
                            {
                                Instance.SetSpriteIfNeeded( structure.GetPortraitIcon()?.GetSpriteForUI() );
                                //desiredTooltipIconColorStyle = structure.GetTooltipIconColorStyle();
                                secondarySprite = structure.CurrentJob?.Icon;
                                secondarySpriteColor = structure.CurrentJob?.MarkerColor;

                                if ( structure.TerritoryControlType != null )
                                {
                                    secondarySprite = structure.TerritoryControlType?.Resource?.Icon;
                                    secondarySpriteColor = CommonRefs.TerritoryControlMarkerColor;
                                }
                            }
                        }
                    }
                }

                if ( desiredTooltipIconColorStyle == null )
                    desiredTooltipIconColorStyle = ColorRefs.DefaultTooltipIconColorStyle;

                if ( lastTooltipIconColorStyle != desiredTooltipIconColorStyle || PriorNumberOfSelectDataReloads != Engine_HotM.NumberOfSelectDataReloads )
                {
                    lastTooltipIconColorStyle = desiredTooltipIconColorStyle;
                    this.Element.RelatedImages[0].color = desiredTooltipIconColorStyle.ColorWithoutHDR;
                    this.Element.RelatedImages[2].color = desiredTooltipIconColorStyle.RelatedLightColor;
                }

                if ( secondarySprite != lastSecondarySprite )
                {
                    lastSecondarySprite = secondarySprite;
                    this.Element.RelatedImages[1].sprite = secondarySprite?.GetSpriteForUI();
                    this.Element.RelatedImages[1].enabled = secondarySprite != null;
                }

                if ( ( secondarySpriteColor != lastSecondarySpriteColor || PriorNumberOfSelectDataReloads != Engine_HotM.NumberOfSelectDataReloads ) && secondarySpriteColor != null )
                {
                    lastSecondarySpriteColor = secondarySpriteColor;
                    this.Element.RelatedImages[1].color = secondarySpriteColor.ColorForSidebar;
                }

                PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
            }

            public override void HandleMouseover()
            {
                ISimMapActor mapActor = Engine_HotM.SelectedActor;
                if ( mapActor != null )
                    mapActor.RenderTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
            }

            public override bool GetShouldBeHidden()
            {
                //ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                //if ( machineActor == null )
                //{
                //    ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                //    if ( npcUnit == null )
                //    {
                //        MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                //        if ( structure == null )
                //            return true;
                //    }
                //}
                return false;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ISimMapMobileActor mobileActor = Engine_HotM.SelectedActor as ISimMapMobileActor;
                if ( mobileActor == null || (mobileActor is ISimNPCUnit npcUnit && !npcUnit.GetIsPlayerControlled()) )
                    return MouseHandlingResult.PlayClickDeniedSound;

                Window_ActorCustomizeLoadout.Instance.ToggleOpen();
                return MouseHandlingResult.None;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bStatsIcon> bStatsIconPool;
        private static ButtonAbstractBase.ButtonPool<bBadgeIcon> bBadgeIconPool;
        private static ButtonAbstractBase.ButtonPool<bHeaderTextPopup> bHeaderTextPopupPool;

        public const float POPUP_OFFSET_X = 72.7f;
        public const float POPUP_OFFSET_Y = -1f;

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

                float offset = 0;
                if ( Window_BuildSidebar.Instance.GetShouldDrawThisFrame() )
                    offset = 160;

                this.WindowController.ExtraOffsetX = offset;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bStatsIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bStatsIconPool = new ButtonAbstractBase.ButtonPool<bStatsIcon>( bStatsIcon.Original, 10, "bStatsIcon" );
                        bBadgeIconPool = new ButtonAbstractBase.ButtonPool<bBadgeIcon>( bBadgeIcon.Original, 10, "bBadgeIcon" );
                        bHeaderTextPopupPool = new ButtonAbstractBase.ButtonPool<bHeaderTextPopup>( bHeaderTextPopup.Original, 10, "HeaderTextPopups" );

                        panelForStats = this.Element.RelatedTransforms[0]; 
                    }
                }
                #endregion

                this.UpdateIcons();
            }

            private AboveSidebarLitColor lastTopColorGlow = AboveSidebarLitColor.Blue;

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bStatsIconPool.Clear( 5 );
                bBadgeIconPool.Clear( 5 );
                bHeaderTextPopupPool.Clear( 5 );

                ISimMapActor actor = Engine_HotM.SelectedActor;
                if ( actor == null )
                    return;
                ISimMachineActor machineActorOrNull = Engine_HotM.SelectedActor as ISimMachineActor;
                ISimMachineUnit unitOrNull = actor as ISimMachineUnit;
                ISimMachineVehicle vehicleOrNull = actor as ISimMachineVehicle;
                ISimNPCUnit npcUnitOrNull = Engine_HotM.SelectedActor as ISimNPCUnit;
                MachineStructure structureOrNull = Engine_HotM.SelectedActor as MachineStructure;
                TerritoryControlType territoryControlOrNull = structureOrNull?.TerritoryControlType;

                MachineUnitType unitTypeOrNull = unitOrNull?.UnitType;
                MachineVehicleType vehicleTypeOrNull = vehicleOrNull?.VehicleType;


                AboveSidebarLitColor desiredTopColorGlow = AboveSidebarLitColor.Blue;

                if ( structureOrNull != null )
                    desiredTopColorGlow = AboveSidebarLitColor.Gold;
                else if ( npcUnitOrNull != null )
                {
                    if ( npcUnitOrNull.GetIsPlayerControlled() )
                        desiredTopColorGlow = AboveSidebarLitColor.Purple;
                    else if ( npcUnitOrNull.GetIsPartOfPlayerForcesInAnyWay() || (npcUnitOrNull.Stance?.IsConsideredNoncombatant??false) )
                        desiredTopColorGlow = AboveSidebarLitColor.Gold;
                    else
                        desiredTopColorGlow = AboveSidebarLitColor.Red;
                }

                int maxRows = structureOrNull != null ? 3 : 4;
                int currentCol = 0;
                int currentRow = 0;

                float currentX = 13.1f;
                float currentY = -5.799f;

                float unusedHeight = 0f;
                UIHelper.DrawActorStats( actor, territoryControlOrNull, Instance, ref unusedHeight, ref currentX, ref currentY, 67.96f, 18f, 2f, 2f, 
                    maxRows, ref currentCol, ref currentRow, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );

                if ( territoryControlOrNull != null || (structureOrNull?.GetCanBePaused() ?? false) )
                    currentRow++;

                if ( currentRow == 0 )
                {
                    currentCol--; //nevermind, would be an empty column!
                    currentRow = maxRows;
                }

                int actualRows = currentCol > 0 ? maxRows : currentRow;
                float neededHeight = (18f * actualRows) + ((actualRows + 2) * 2f) + 5f;

                float neededWidth = 87.1f + (currentCol <= 0 ? 0 : (( currentCol * ( 67.96f + 2f ) ) ) );

                UIHelper.ApplyPanelInPosition( this.panelForStats, 77.6f, 2.480011f, neededWidth, neededHeight );

                {
                    float stanceX = 1.35f;
                    float stanceY = -77.47987f;
                    bStance.Instance.ApplyItemInPositionNoTextSizing( ref stanceX, ref stanceY, false, false, 83.39f, 14.4f, IgnoreSizeOption.IgnoreSize );
                }

                if ( territoryControlOrNull != null || (structureOrNull?.GetCanBePaused()??false) )
                {
                    bFlagActivator.Instance.ApplyItemInPosition( ref currentX, ref currentY, false, true, 67.96f, 18f );
                }
                //else if ( structureOrNull?.CurrentJob?.Implementation?.HandleJobActivationLogic( structureOrNull, structureOrNull?.CurrentJob, null, 
                //    JobActivationLogic.ShouldHaveActivationButton, out _, out _ ) ??false )
                //{
                //    float aotX = 2.06f;
                //    bFlagActivator.Instance.ApplyItemInPosition( ref aotX, ref currentY, false, true, 73.2f, 30f );
                //}

                float finalWidth = 77.6f + neededWidth;

                #region Expand or Shrink Width Of This Window
                if ( lastWindowWidth != finalWidth )
                {
                    lastWindowWidth = finalWidth;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.pivot = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.UI_SetWidth( finalWidth );
                }
                #endregion

                SizingRect = this.Element.RelevantRect;

                float badgeY = 15.2f;
                float badgeX = structureOrNull != null ? -2.8f : 70.5f;

                UIHelper.DrawAllActorBadgesAndPerks( actor, Instance, badgeX, badgeY, 18f, 15f, 0f, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );

                if ( desiredTopColorGlow != lastTopColorGlow )
                {
                    lastTopColorGlow = desiredTopColorGlow;
                    this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[(int)desiredTopColorGlow];
                }
            }

            private float lastWindowWidth = -1f;         
        }

        #region bStatsIcon
        public class bStatsIcon : bStatsIconBase
        {
            public static bStatsIcon Original;
            public bStatsIcon() { if ( Original == null ) Original = this; }
        }

        public bStatsIconBase GetNewStatsIconBase()
        {
            return bStatsIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
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

        #region bStance
        public class bStance : ButtonAbstractBaseWithImage
        {
            public static bStance Instance;
            public bStance() { if ( Instance == null ) Instance = this; }

            private StanceLitButtonColor lastColorGlow = StanceLitButtonColor.Green;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                StanceLitButtonColor desiredColorGlow = StanceLitButtonColor.Green;

                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor == null || machineActor.CurrentActionOverTime == null )
                {
                    ISimMachineUnit unit = Engine_HotM.SelectedActor as ISimMachineUnit;
                    if ( unit != null )
                    {
                        //Buffer.AlignLeft().Space3x();
                        Buffer.AddSpriteStyled_NoIndent( unit.Stance.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                        Buffer.AddRaw( unit.Stance.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );

                        if ( unit.UnitType.IsConsideredAndroid && MachineUnitStanceTable.AndroidStanceCount <= 1 )
                            desiredColorGlow = StanceLitButtonColor.Gray;
                        if ( unit.UnitType.IsConsideredMech && MachineUnitStanceTable.MechStanceCount <= 1 )
                            desiredColorGlow = StanceLitButtonColor.Gray;
                    }
                    else
                    {
                        ISimMachineVehicle vehicle = Engine_HotM.SelectedActor as ISimMachineVehicle;
                        if ( vehicle != null )
                        {
                            //Buffer.AlignLeft().Space3x();
                            Buffer.AddSpriteStyled_NoIndent( vehicle.Stance.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                            Buffer.AddRaw( vehicle.Stance.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );

                            if ( MachineVehicleStanceTable.VehicleStanceCount <= 1 )
                                desiredColorGlow = StanceLitButtonColor.Gray;
                        }
                        else
                        {
                            ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                            if ( npcUnit != null )
                            {
                                Buffer.AddSpriteStyled_NoIndent( npcUnit.Stance.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                Buffer.AddRaw( npcUnit.Stance.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );

                                if ( !npcUnit.GetIsPlayerControlled() )
                                    desiredColorGlow = StanceLitButtonColor.Gray;
                            }
                            else
                            {
                                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                                if ( structure != null )
                                {
                                    TerritoryControlType controlType = structure.TerritoryControlType;
                                    if ( controlType != null )
                                    {
                                        Buffer.AddSpriteStyled_NoIndent( controlType.Resource.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                        Buffer.AddRaw( controlType.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );

                                        desiredColorGlow = StanceLitButtonColor.Gray;
                                    }
                                    else
                                    {
                                        MachineJob job = structure.CurrentJob;
                                        if ( job != null )
                                        {
                                            //Buffer.AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                            Buffer.AddRaw( job.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );
                                        }
                                        else
                                        {
                                            Buffer.AddRaw( structure.GetDisplayName() ).AddNeverTranslated( " <size=1%>.</size>", true );
                                        }
                                        desiredColorGlow = StanceLitButtonColor.Gray;
                                    }
                                }
                            }
                        }
                    }

                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        desiredColorGlow = StanceLitButtonColor.Gray;
                }
                else //machine actor with action over time
                {
                    machineActor.CurrentActionOverTime.Type.Implementation.TryHandleActionOverTime(machineActor.CurrentActionOverTime, Buffer,
                        ActionOverTimeLogic.PredictSidebarButtonText, null,
                        null, SideClamp.Any, TooltipExtraText.None, TooltipExtraRules.None );

                    if ( !machineActor.CurrentActionOverTime.Type.CanBeCanceled )
                        desiredColorGlow = StanceLitButtonColor.Red;
                }

                if ( desiredColorGlow != lastColorGlow )
                {
                    lastColorGlow = desiredColorGlow;
                    this.Element.RelatedImages[0].enabled = desiredColorGlow != StanceLitButtonColor.Gray;
                    this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[(int)desiredColorGlow];
                }
            }

            public override void HandleMouseover()
            {
                if ( Window_ActorStanceChange.Instance.IsOpen || Window_StructureChoiceList.Instance.IsOpen )
                    return;

                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor == null || machineActor.CurrentActionOverTime == null )
                {
                    ISimMachineUnit machineUnit = Engine_HotM.SelectedActor as ISimMachineUnit;
                    if ( machineUnit != null )
                    {
                        machineUnit.Stance.RenderMachineUnitStanceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraText.MachineUnitStanceChange, 
                            TooltipExtraRules.MustBeAboveBottomLeftUnitPanels, machineUnit );
                    }
                    else
                    {
                        ISimMachineVehicle vehicle = Engine_HotM.SelectedActor as ISimMachineVehicle;
                        if ( vehicle != null )
                        {
                            vehicle.Stance.RenderMachineVehicleStanceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraText.MachineUnitStanceChange,
                                TooltipExtraRules.MustBeAboveBottomLeftUnitPanels, vehicle );
                        }
                        else
                        {
                            ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                            if ( npcUnit != null )
                            {
                                npcUnit.Stance.RenderNPCStanceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraText.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels, npcUnit );
                            }
                            else
                            {
                                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                                if ( structure != null )
                                {
                                    #region MachineStructure
                                    TerritoryControlType controlType = structure.TerritoryControlType;
                                    if ( controlType != null )
                                    {
                                        controlType.Resource.WriteResourceTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, 
                                            TooltipExtraText.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
                                    }
                                    else
                                    {
                                        //no tooltip, I guess
                                    }
                                    #endregion MachineStructure
                                }
                            }
                        }
                    }
                }
                else
                {
                    machineActor.CurrentActionOverTime.Type.Implementation.TryHandleActionOverTime( machineActor.CurrentActionOverTime, null, ActionOverTimeLogic.WriteTooltip, null,
                        this.Element, SideClamp.AboveOrBelow, TooltipExtraText.ActionOverTimeCancelInstructions, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return MouseHandlingResult.PlayClickDeniedSound;

                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor == null || machineActor.CurrentActionOverTime == null )
                {
                    TooltipRefs.AtMouseTag.ClearAllImmediately();
                    MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                    if ( structure != null )
                    {
                        TerritoryControlType controlType = structure.TerritoryControlType;
                        if ( controlType != null )
                            BuildModeHandler.TryScrapSelectedStructure( structure );
                        else
                            return MouseHandlingResult.PlayClickDeniedSound;
                        return MouseHandlingResult.None;
                    }

                    if ( !VisCommands.ToggleStanceWindow() )
                        return MouseHandlingResult.PlayClickDeniedSound;
                }
                else
                {
                    if ( !machineActor.CurrentActionOverTime.Type.CanBeCanceled || (machineActor.CurrentActionOverTime.RelatedInvestigationTypeOrNull?.IsBlockedFromBeingCanceled ?? false) )
                    {
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                        return MouseHandlingResult.None;
                    }

                    machineActor.CurrentActionOverTime?.TryToCancelAtRequestOfUser();
                    ParticleSoundRefs.CancelActionOverTimeSound.DuringGame_PlayAtLocation( machineActor.GetDrawLocation() );
                }
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }
        #endregion

        #region tLowerStats
        public class tLowerStats : TextAbstractBase
        {
            public static tLowerStats Instance;
            public tLowerStats() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                if ( npcUnit != null )
                {
                    NPCUnitType npcUnitType = npcUnit.UnitType;
                    Buffer.AddLangAndAfterLineItemHeader( npcUnitType != null && (npcUnitType.IsMechStyleMovement || npcUnitType.IsVehicle) ? "Crew_Prefix" : "Squad_Prefix" )
                        .AddRaw( npcUnit.CurrentSquadSize.ToStringWholeBasic()/*, ColorTheme.HeaderGoldOrangeDark*/ ).Line();
                    Buffer.AddRaw( npcUnit.FromCohort?.GetDisplayName()??string.Empty );
                }
                else
                {
                    if ( Engine_HotM.SelectedActor is ISimMachineUnit unit )
                    {
                        MachineUnitType unitType = unit.UnitType;
                        Buffer.AddRaw( unitType?.GetDisplayName()??string.Empty ).Line();
                        int effectiveClearanceLevel = unit.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
                        Buffer.AddLangAndAfterLineItemHeader( "SecurityClearance_Brief" )
                            .AddRaw( effectiveClearanceLevel <= 0 ? "-" : effectiveClearanceLevel.ToString( "00" ) ).Line();
                    }
                    else if ( Engine_HotM.SelectedActor is ISimMachineVehicle vehicle )
                    {
                        MachineVehicleType vehicleType = vehicle.VehicleType;
                        Buffer.AddRaw( vehicleType?.GetDisplayName() ?? string.Empty ).Line();

                        int unitSlotCount = 0;
                        foreach ( KeyValuePair<MachineUnitStorageSlotType, int> kv in vehicle.VehicleType.UnitStorageSlotCounts )
                            unitSlotCount += kv.Value;

                        int passengers = vehicle.GetStoredUnits().Count;
                        if ( passengers > unitSlotCount )
                            unitSlotCount = passengers;

                        if ( unitSlotCount <= 0 )
                            Buffer.AddLangAndAfterLineItemHeader( "Passengers" ).AddRaw( "-" ).Line();
                        else
                            Buffer.AddLangAndAfterLineItemHeader( "Passengers" ).AddFormat2( "OutOF", passengers, unitSlotCount, ColorTheme.DataBlue ).Line();

                        //int effectiveClearanceLevel = vehicle.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
                        //Buffer.AddLangAndAfterLineItemHeader( "SecurityClearance_Brief" )
                        //    .AddRaw( effectiveClearanceLevel <= 0 ? "-" : effectiveClearanceLevel.ToString( "00" ) ).Line();
                    }
                    else if ( Engine_HotM.SelectedActor is MachineStructure structure )
                    {
                        TerritoryControlType controlType = structure.TerritoryControlType;

                        if ( controlType != null )
                        {
                            Buffer.AddRaw( structure.Type.GetDisplayName() ).Line();


                            MapActorData deterrence = structure.GetActorDataData( ActorRefs.FlagRequiredDeterrence, true );
                            bool canActivate = false;
                            if ( deterrence != null && deterrence.Current * 5 >= deterrence.Maximum ) //aka 20%
                                canActivate = true;

                            Buffer.AddLang( structure.IsTerritoryControlActive ? "TerritoryControl_Active" : "TerritoryControl_Dormant", canActivate ? string.Empty : ColorTheme.RedOrange2 );
                        }
                        else
                        {
                            if ( structure.GetHasAnyComplaint() )
                            {
                                //Buffer.StartColor( ColorTheme.RedOrange2 );
                                structure.WriteComplaints( Buffer, false, false, true, 2 );
                            }
                            else
                            {
                                Buffer.AddRaw( structure.Type.GetDisplayName() ).Line();
                                if ( structure.CurrentSubnet.Display != null )
                                    Buffer.AddFormat1( "SubnetName", structure.CurrentSubnet.Display.SubnetIndex ).Line();
                                else
                                    Buffer.AddRaw( SimCommon.TheNetwork.NetworkName ).Line();
                            }
                        }
                    }
                }
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                if ( Engine_HotM.SelectedActor is MachineStructure structure )
                {
                    TerritoryControlType controlType = structure.TerritoryControlType;
                    if ( controlType != null )
                    {

                    }
                    else
                    {
                        structure?.CurrentSubnet?.Display?.RenderSubnetTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
                    }
                }
                else
                {
                    //ISimMapActor mapActor = Engine_HotM.SelectedActor;
                    //if ( mapActor != null )
                    //    mapActor.RenderTooltip( this.Element, SideClamp.AboveOrBelow, false, false, ActorTooltipExtraData.None, TooltipExtraRules.MustBeAboveBottomLeftUnitPanels );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return MouseHandlingResult.PlayClickDeniedSound;
                ISimMapMobileActor mobileActor = Engine_HotM.SelectedActor as ISimMapMobileActor;
                if ( mobileActor == null || (mobileActor is ISimNPCUnit npcUnit && !npcUnit.GetIsPlayerControlled()) )
                    return MouseHandlingResult.None;

                Window_ActorCustomizeLoadout.Instance.ToggleOpen();

                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }
        #endregion

        #region bFlagActivator
        public class bFlagActivator : ButtonAbstractBaseWithImage
        {
            public static bFlagActivator Instance;
            public bFlagActivator() { if ( Instance == null ) Instance = this; }

            private StanceLitButtonColor lastColorGlow = StanceLitButtonColor.Green;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                StanceLitButtonColor desiredColorGlow = StanceLitButtonColor.Red;

                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                if ( structure != null )
                {
                    if ( structure.TerritoryControlType != null )
                    {
                        MapActorData deterrence = structure.GetActorDataData( ActorRefs.FlagRequiredDeterrence, true );
                        bool canActivate = false;
                        if ( deterrence != null && deterrence.Current * 5 >= deterrence.Maximum ) //aka 20%
                            canActivate = true;

                        Buffer.AddLang( structure.IsTerritoryControlActive ? "TerritoryControl_Active" : "TerritoryControl_Dormant", canActivate ? string.Empty : ColorTheme.RedOrange2 );

                        if ( structure.IsTerritoryControlActive )
                            desiredColorGlow = StanceLitButtonColor.Green;
                    }
                    else
                    {
                        desiredColorGlow = structure.IsJobPaused ? StanceLitButtonColor.Red : StanceLitButtonColor.Green;
                        //structure?.CurrentJob?.Implementation?.HandleJobActivationLogic( structure, structure?.CurrentJob, Buffer, JobActivationLogic.WriteActivationButtonText, out _, out _ );

                        Buffer.AddLang( !structure.IsJobPaused ? "TerritoryControl_Active" : "TerritoryControl_Dormant", string.Empty );
                    }
                }


                if ( desiredColorGlow != lastColorGlow )
                {
                    lastColorGlow = desiredColorGlow;
                    this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[(int)desiredColorGlow];
                }
            }

            public override bool GetShouldBeHidden()
            {
                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                if ( structure != null )
                {
                    if ( structure.TerritoryControlType != null )
                        return false; //never hidden in this case

                    if ( !structure.GetCanBePaused() )
                        return true;

                    return false; //never disabled for other jobs
                }
                else
                    return true;
            }

            public override void HandleMouseover()
            {
                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                if ( structure != null )
                {
                    //if ( Window_ActorStanceChange.Instance.IsOpen || Window_StructureJobChange.Instance.IsOpen || Window_StructureChoiceList.Instance.IsOpen )
                    //    return;

                    if ( structure.TerritoryControlType != null )
                    {
                        MapActorData deterrence = structure.GetActorDataData( ActorRefs.FlagRequiredDeterrence, true );
                        bool canActivate = false;
                        if ( deterrence != null && deterrence.Current * 5 >= deterrence.Maximum ) //aka 20%
                            canActivate = true;

                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( structure ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                            TooltipExtraText.None, TooltipExtraRules.None ) ) //MustBeAboveBottomLeftUnitPanels taken off since it will be right next to it
                        {
                            novel.TitleUpperLeft.AddLang( structure.IsTerritoryControlActive ? "TerritoryControl_ClickToDeactivate_Header" : "TerritoryControl_ClickToActivate_Header" );
                            novel.Main.AddLang( structure.IsTerritoryControlActive ? "TerritoryControl_Active_Details" : "TerritoryControl_Dormant_Details" ).Line();
                            if ( !canActivate )
                                novel.Main.AddLang( "TerritoryControl_Blocked_Details", ColorTheme.RedOrange2 ).Line();
                        }
                    }
                    else
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( structure ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple,
                            TooltipExtraText.None, TooltipExtraRules.None ) ) //MustBeAboveBottomLeftUnitPanels taken off since it will be right next to it
                        {
                            novel.TitleUpperLeft.AddLang( !structure.IsJobPaused ? "TerritoryControl_ClickToDeactivate_Header" : "TerritoryControl_ClickToActivate_Header" );
                            novel.Main.AddLang( !structure.IsJobPaused ? "RegularJob_Active_Details" : "RegularJob_Dormant_Details" ).Line();
                        }

                        //note, not a real thing for now
                        //structure?.CurrentJob?.Implementation?.HandleJobActivationLogic( structure, structure?.CurrentJob, tooltipBuffer,
                        //    JobActivationLogic.WriteActivationTooltip, out TooltipMinHeight, out Width );
                    }
                    return;
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                    return MouseHandlingResult.PlayClickDeniedSound;
                TooltipRefs.AtMouseTag.ClearAllImmediately();
                MachineStructure structure = Engine_HotM.SelectedActor as MachineStructure;
                if ( structure != null )
                {
                    if ( structure.TerritoryControlType != null )
                    {
                        MapActorData deterrence = structure.GetActorDataData( ActorRefs.FlagRequiredDeterrence, true );
                        bool canActivate = false;
                        if ( deterrence != null && deterrence.Current * 5 >= deterrence.Maximum ) //aka 20%
                            canActivate = true;

                        if ( !canActivate )
                        {
                            structure.IsTerritoryControlActive = false;
                            return MouseHandlingResult.PlayClickDeniedSound;
                        }

                        structure.IsTerritoryControlActive = !structure.IsTerritoryControlActive;
                        if ( structure.IsTerritoryControlActive )
                        {
                            if ( structure.TerritoryControlType != null )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "TerritoryControlFlagActivated" ),
                                    NoteStyle.StoredGame, structure.TerritoryControlType.ID, 0, 0, 0, SimCommon.Turn + 40 );

                            ActivityScheduler.DoFullSpawnCheckForAllTerritoryControlRelatedManagers( Engine_Universal.PermanentQualityRandom, false, false );
                        }
                        else
                        {
                            if ( structure.TerritoryControlType != null )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "TerritoryControlFlagDisabled" ),
                                    NoteStyle.StoredGame, structure.TerritoryControlType.ID, 0, 0, 0, SimCommon.Turn + 40 );
                        }
                    }
                    else
                    {
                        if ( structure.GetCanBePaused() )
                            structure.IsJobPaused = !structure.IsJobPaused;
                        else
                            structure.IsJobPaused = false;

                        //if ( input.LeftButtonClicked )
                        //    structure.CurrentJob?.Implementation?.HandleJobActivationLogic( structure, structure?.CurrentJob, null, JobActivationLogic.HandleActivationTrigger, out _, out _ );
                        //else if ( input.RightButtonClicked )
                        //    structure.CurrentJob?.Implementation?.HandleJobActivationLogic( structure, structure?.CurrentJob, null, JobActivationLogic.HandleActivationTriggerAltFunction, out _, out _ );
                    }
                    return MouseHandlingResult.None;
                }

                return MouseHandlingResult.None;
            }
        }
        #endregion
    }

    public enum StanceLitButtonColor
    {
        Green = 0,
        Red = 1,
        Gray = 2,
    }

    public enum AboveSidebarLitColor
    {
        Blue = 0,
        Purple = 1,
        Red = 2,
        Gold = 3,
    }
}

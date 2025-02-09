using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics.Eventing.Reader;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ActorSidebarStatsEquipment : WindowControllerAbstractBase, IBadgeIconFactory, IStatsIconBaseFactory
    {
        public static Window_ActorSidebarStatsEquipment Instance;
		
		public Window_ActorSidebarStatsEquipment()
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

            if ( !(Window_ActorCustomizeLoadout.Instance?.GetShouldDrawThisFrame() ?? false ) )
                return false; //only draw when the customize loadout is going to draw
            
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

        #region GetBottomXY
        public static Vector3 GetBottomXY()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return Vector3.zero; //hidden entirely!

            return SizingRect.GetWorldSpaceBottomLeftCorner();
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
                            if ( mUnit.UnitType.IsConsideredMech )
                                Buffer.AddLang( "StandardMech" );
                            else
                                Buffer.AddLang( "StandardAndroid" );
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
                    mapActor.RenderTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
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
                    mapActor.RenderTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
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

        #region bCustomize
        public class bCustomize : ButtonAbstractBaseWithImage
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                if ( npcUnit != null )
                {
                    NPCUnitType npcUnitType = npcUnit.UnitType;
                    Buffer.AddLangAndAfterLineItemHeader( npcUnitType != null && ( npcUnitType.IsMechStyleMovement || npcUnitType.IsVehicle ) ? "Crew_Prefix" : "Squad_Prefix" )
                        .AddRaw( npcUnit.CurrentSquadSize.ToStringWholeBasic(), ColorTheme.HeaderGoldOrangeDark );
                    return;
                }
            }

            public override void OnUpdateSubSub()
            {
            }

            public override void HandleMouseover()
            {
                ISimMapActor mapActor = Engine_HotM.SelectedActor;
                if ( mapActor != null )
                    mapActor.RenderTooltip( this.Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
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
                ISimNPCUnit npcUnit = Engine_HotM.SelectedActor as ISimNPCUnit;
                if ( npcUnit != null && npcUnit.CurrentSquadSize > 1 )
                    return false;

                return true;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bStatsIcon> bStatsIconPool;
        private static ButtonAbstractBase.ButtonPool<bBadgeIcon> bBadgeIconPool;
        private static ButtonAbstractBase.ButtonPool<bHeaderTextPopup> bHeaderTextPopupPool;

        public const float POPUP_OFFSET_X = 72.7f;
        public const float POPUP_OFFSET_Y = -1f;

        public const float POST_NAME_ICON_Y_START_MACHINE = -85.2f;
        public const float POST_NAME_ICON_Y_START_NON_MACHINE_OR_NPC = -85.2f + 18.8f;

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
                    offset = 154;

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

                float cumulativeStatStatSize = 0f;
                float currentY = structureOrNull != null ? POST_NAME_ICON_Y_START_NON_MACHINE_OR_NPC : POST_NAME_ICON_Y_START_MACHINE;
                float startingY = 1.7f; //margin at the bottom

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

                float finalYForSubPanel = currentY;
                float statsMargin = 3.1f + 3.1f;
                int currentCol = 0;
                int currentRow = 0;

                float statsX = 2f;
                float statsY = -3.1f;

                UIHelper.DrawActorStats( actor, territoryControlOrNull, Instance, ref cumulativeStatStatSize, ref statsX, ref statsY, 67.96f, 18f, 2f, 2f, 0,
                    ref currentCol, ref currentRow, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.None );

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

                UIHelper.DrawAllActorBadgesAndPerks( actor, Instance, 61.5f, 14.3f, 18f, 0f, 15f, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.None );

                if ( desiredTopColorGlow != lastTopColorGlow )
                {
                    lastTopColorGlow = desiredTopColorGlow;
                    this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[(int)desiredTopColorGlow];
                }
            }

            private float lastWindowHeight = -1f;
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
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }

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

        #region bActionOverTime
        public class bActionOverTime : ButtonAbstractBaseWithImage
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override bool GetShouldBeHidden()
            {
                return true;
            }

            public override void HandleMouseover()
            {
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

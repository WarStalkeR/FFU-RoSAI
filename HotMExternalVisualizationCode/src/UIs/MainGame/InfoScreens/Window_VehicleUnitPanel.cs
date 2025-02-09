using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_VehicleUnitPanel : ToggleableWindowController
    {
        public static Window_VehicleUnitPanel Instance;
		
		public Window_VehicleUnitPanel()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void OnOpen()
        {
            if ( Window_AbilityOptionList.Instance.IsOpen )
                Window_AbilityOptionList.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
        }

        public static ISimMachineVehicle VehicleToShow = null;

        public class tHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat1( "UnitsRidingIn", VehicleToShow?.GetDisplayName() ??"[null]" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( VehicleToShow != null && Engine_HotM.SelectedActor != VehicleToShow )
                {
                    Engine_HotM.SetSelectedActor( VehicleToShow, false, true, true );
                    Engine_HotM.SelectedMachineActionMode = null;
                }
                return MouseHandlingResult.None;
            }
        }

        private static ButtonAbstractBase.ButtonPool<bUnitIcon> bUnitIconPool;

        public const float UNIT_ICON_SIZE = 43.5f;
        public const float UNIT_ICON_Y = 47.7f;
        public const float UNIT_ICON_X_START = 3.5f;
        public const float UNIT_ICON_X_SPACING = 2f;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1f * GameSettings.Current.GetFloat( "Scale_AbilityFooterBar" );

                this.WindowController.ExtraOffsetXRaw = Window_ActorSidebarStatsLowerLeft.GetXWidthForOtherWindowOffsets();
                this.WindowController.ExtraOffsetY = -Window_AbilityFooterBar.Instance.GetAbilityBarCurrentHeight_Scaled();

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bUnitIcon.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bUnitIconPool = new ButtonAbstractBase.ButtonPool<bUnitIcon>( bUnitIcon.Original, 10, "bUnitIcon" );
                    }
                }
                #endregion

                this.UpdateIcons();
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bUnitIconPool.Clear( 5 );

                ISimMachineVehicle vehicle = VehicleToShow;
                if ( vehicle == null )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                MachineVehicleType vehicleType = vehicle?.VehicleType;
                if ( vehicleType == null )
                    return;

                if ( displayInfos == null )
                    displayInfos = new UnitSlotDisplayInfo[10]; //max slot is 9

                int unitSlotCount = 0;
                foreach ( KeyValuePair<MachineUnitStorageSlotType, int> kv in vehicle.VehicleType.UnitStorageSlotCounts )
                    unitSlotCount += kv.Value;

                ListView<ISimMachineUnit> storedUnits = vehicle.GetStoredUnits();
                int unitCount = storedUnits.Count;
                if ( unitCount > unitSlotCount ) //this is kind of an error case, but can be caused by older saves
                    unitSlotCount = unitCount;

                if ( storedUnits.Count <= 0 ) //when there is nothing in the vehicle, don't let it stay open
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                float currentX = UNIT_ICON_X_START;

                #region Draw Vehicle Units
                {
                    for ( int i = 0; i < unitSlotCount; i++ )
                    {
                        DrawUnitIcon( i, vehicle, i < storedUnits.Count ? storedUnits[i] : null, ref currentX );
                    }
                }
                #endregion

                #region Expand or Shrink Width Of This Window
                float widthForWindow = 1f + currentX;
                if ( lastWindowWidth != widthForWindow )
                {
                    lastWindowWidth = widthForWindow;
                    //this.Element.RelevantRect.anchorMin = new Vector2( 0f, 0 );
                    //this.Element.RelevantRect.anchorMax = new Vector2( 0f, 0 );
                    //this.Element.RelevantRect.pivot = new Vector2( 0f, 0 );
                    this.Element.RelevantRect.UI_SetWidth( widthForWindow );
                }
                #endregion
            }
            private float lastWindowWidth = -1f;

            private UnitSlotDisplayInfo[] displayInfos;

            #region DrawUnitIcon
            private void DrawUnitIcon( int SlotIndex, ISimMachineVehicle vehicle, ISimMachineUnit unitOrNull, ref float currentX )
            {
                bUnitIcon icon = bUnitIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    if ( displayInfos[SlotIndex] == null )
                        displayInfos[SlotIndex] = new UnitSlotDisplayInfo( icon );

                    {
                        displayInfos[SlotIndex].UpdateFromUnit( vehicle, unitOrNull, Engine_HotM.SelectedActor == unitOrNull );

                    }
                    float iconY = UNIT_ICON_Y;
                    icon.ApplyItemInPositionNoTextSizing( ref currentX, ref iconY, false, false, UNIT_ICON_SIZE, UNIT_ICON_SIZE, IgnoreSizeOption.IgnoreSize );
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                if ( unitOrNull == null )
                                    ExtraData.Buffer.AddRaw( ( SlotIndex + 1 ).ToString(), ColorRefs.VehicleBarEmptyNumberColor.ColorHex );
                                else
                                    ExtraData.Buffer.AddRaw( ( SlotIndex + 1).ToString(), ColorRefs.VehicleBarFilledNumberColor.ColorHex );
                                break;
                            case UIAction.GetOtherTextToShowFromVolatile:
                                if ( unitOrNull != null )
                                {
                                    ExtraData.Buffer.AddRaw( unitOrNull.GetDisplayName() );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                if ( unitOrNull == null )
                                {
                                    if ( vehicle != null )
                                    {
                                        foreach ( KeyValuePair<MachineUnitStorageSlotType, int> kv in vehicle.VehicleType.UnitStorageSlotCounts )
                                        {
                                            int baseSlots = kv.Value;
                                            if ( baseSlots <= 0 )
                                                continue;
                                            int remainingSlots = vehicle.GetRemainingUnitSlotsOfType( kv.Key );

                                            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( vehicle ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                            {
                                                novel.Icon = kv.Key.Icon;

                                                novel.TitleUpperLeft.AddRawAndAfterLineItemHeader( kv.Key.GetDisplayName() )
                                                    .AddFormat2( "OutOF", remainingSlots, baseSlots );
                                            }

                                            break;
                                        }
                                    }
                                }
                                else
                                    unitOrNull.RenderTooltip( null, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                                break;
                            case UIAction.OnClick:
                                if ( unitOrNull != null )
                                {
                                    //note: allow the player to select the units in the vehicles, regardless of if they can't get out.  Otherwise they can't see many things.
                                    Engine_HotM.SetSelectedActor( unitOrNull, false, true, true );
                                    Engine_HotM.SelectedMachineActionMode = null;
                                }
                                break;
                        }
                    } );

                    currentX += ( UNIT_ICON_SIZE + UNIT_ICON_X_SPACING );
                }
            }
            #endregion DrawUnitIcon
        }

        #region TriggerSlotIndex
        public void TriggerSlotIndex( int Index, TriggerStyle Style )
        {
            ISimMachineVehicle vehicle = VehicleToShow;
            if ( vehicle == null )
                return;
            ListView<ISimMachineUnit> storedUnits = vehicle.GetStoredUnits();
            for ( int i = 0; i < storedUnits.Count; i++ )
            {
                if ( i + 1 == Index )
                {
                    Engine_HotM.SetSelectedActor( storedUnits[i], false, true, true );
                    Engine_HotM.SelectedMachineActionMode = null;
                    return;
                }
            }

            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
        }
        #endregion

        #region bUnitIcon
        public class bUnitIcon : ButtonAbstractBaseWithImage
        {
            public static bUnitIcon Original;
            public bUnitIcon() { if ( Original == null ) Original = this; }

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

        #region UnitSlotDisplayInfo
        private class UnitSlotDisplayInfo
        {
            public readonly bUnitIcon UnitIcon;
            public readonly Image ClipImage;

            public readonly Image PortraitImage;

            public readonly Image FrameImage;
            public readonly Image HighlightImage;
            public readonly Image DisabledImage;

            public ISimMachineVehicle PriorContainer = null;
            public ISimMachineUnit PriorRidingUnit = null;
            private bool hasSetFromNullPriorAbility = false;
            private int PriorNumberOfSelectDataReloads = 0;
            private bool PriorWasHighlighted = false;
            private int LastReloadCount = 0;

            public UnitSlotDisplayInfo( bUnitIcon ActionIcon )
            {
                this.UnitIcon = ActionIcon;
                this.ClipImage = this.UnitIcon.Element.GetComponent<Image>();

                PortraitImage = ActionIcon.Element.RelatedImages[0];
                FrameImage = ActionIcon.Element.RelatedImages[1];
                HighlightImage = ActionIcon.Element.RelatedImages[2];
                DisabledImage = ActionIcon.Element.RelatedImages[3];
            }

            public void UpdateFromUnit( ISimMachineVehicle Container, ISimMachineUnit RidingUnit, bool IsHighlighted )
            {
                if ( RidingUnit != null )
                {
                    if ( Container == PriorContainer && RidingUnit == PriorRidingUnit && PriorNumberOfSelectDataReloads == Engine_HotM.NumberOfSelectDataReloads &&
                        PriorWasHighlighted == IsHighlighted &&
                        LastReloadCount == Engine_HotM.NumberOfSelectDataReloads )
                    { } //skip, it's already fine
                    else
                    {
                        PriorContainer = Container;
                        PriorRidingUnit = RidingUnit;
                        hasSetFromNullPriorAbility = false;
                        PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                        PriorWasHighlighted = IsHighlighted;
                        LastReloadCount = Engine_HotM.NumberOfSelectDataReloads;
                        this.ClipImage.color = Color.white;
                        DisabledImage.enabled = false;
                        FrameImage.color = ColorRefs.VehicleBarFilledBorderColor.ColorHDR;
                        PortraitImage.sprite = RidingUnit.UnitType.TooltipIcon.GetSpriteForUI();
                        PortraitImage.enabled = true;
                        HighlightImage.enabled = IsHighlighted;
                    }
                }
                else
                {
                    if ( hasSetFromNullPriorAbility && PriorNumberOfSelectDataReloads == Engine_HotM.NumberOfSelectDataReloads )
                    { } //skip, it's already fine
                    else
                    {
                        PriorContainer = null;
                        PriorRidingUnit = null;
                        hasSetFromNullPriorAbility = true;
                        PriorNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                        //icon.SetSpriteIfNeeded( ability.Icon.GetSpriteForUI() );
                        this.ClipImage.color = ColorMath.Transparent;

                        PortraitImage.enabled = false;
                        HighlightImage.enabled = false;

                        DisabledImage.enabled = true;

                        FrameImage.color = ColorRefs.VehicleBarEmptyBorderColor.ColorHDR;
                    }
                }
            }
        }
        #endregion AbilitySlotDisplayInfo

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

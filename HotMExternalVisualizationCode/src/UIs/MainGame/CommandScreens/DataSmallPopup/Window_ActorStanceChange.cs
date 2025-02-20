using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ActorStanceChange : ToggleableWindowController, IInputActionHandler
    {
        public static Window_ActorStanceChange Instance;
        public Window_ActorStanceChange()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public ISimMapMobileActor ActorToEdit = null;

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }
            if ( Engine_Universal.GameLoop.IsLevelEditor )
            {
                this.IsOpen = false;
                return false;
            }

            if ( this.ActorToEdit == null )
            {
                this.IsOpen = false;
                return false;
            }
            if ( this.ActorToEdit.IsFullDead )
            {
                this.IsOpen = false;
                return false;
            }

            if ( this.ActorToEdit != Engine_HotM.SelectedActor )
            {
                this.IsOpen = false;
                this.ActorToEdit = null;
                return false;
            }
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;

            return this.IsOpen;
        }
        #endregion

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_ActorStanceChange.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bIconRow> bIconRowPool;

            private int heightToShow = 0;
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.1f * GameSettings.Current.GetFloat( "Scale_StatsSidebar" );

                this.WindowController.ExtraOffsetY = -(Window_ActorSidebarStatsLowerLeft.Instance.GetCurrentHeight_Scaled());

                if ( Window_ActorStanceChange.Instance != null )
                {
                    #region Global Init
                    if ( bIconRow.Original != null && !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0; //make as responsive as possible
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
                        hasGlobalInitialized = true;
                        bIconRowPool = new ButtonAbstractBase.ButtonPool<bIconRow>( bIconRow.Original, 10, "bIconRow" );
                    }
                    #endregion

                    bIconRowPool.Clear( 60 );

                    this.OnUpdate_Content();
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 25 + MathA.Min( lastYHeightOfInterior, 400 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0f );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                float runningY = -3.069992f; //starting

                if ( Instance.ActorToEdit is ISimMachineUnit unit )
                    this.OnUpdate_Content_Unit( runningY, unit );
                else if ( Instance.ActorToEdit is ISimMachineVehicle vehicle )
                    this.OnUpdate_Content_Vehicle( runningY, vehicle );
                else if ( Instance.ActorToEdit is ISimNPCUnit npcUnit && npcUnit.GetIsPlayerControlled() )
                    this.OnUpdate_Content_NPCUnit( runningY, npcUnit );
            }
            #endregion

            #region OnUpdate_Content_Unit
            private void OnUpdate_Content_Unit( float runningY, ISimMachineUnit unit )
            {
                if ( unit == null || unit.IsFullDead )
                    return;

                float x = 2.769974f;

                foreach ( MachineUnitStance stance in MachineUnitStanceTable.Instance.Rows )
                {
                    if ( stance.IsHidden )
                        continue;
                    if ( !unit.UnitType.AvailableStances.ContainsKey( stance ) )
                        continue; //this is not an available stance to this unit, so nevermind!

                    bIconRow row = bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        break; //this was just time-slicing, so ignore that failure for now

                    bIconRowPool.ApplySingleItemInRow( row, x, runningY );
                    runningY -= 20.73f;
                    row.SetRelatedImage0SpriteIfNeeded( stance.Icon.GetSpriteForUI() );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( row, unit.Stance == stance, false );
                                    ExtraData.Buffer.AddRaw( stance.GetDisplayName(), colorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    stance.RenderMachineUnitStanceTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraText.MachineUnitStanceInfo, TooltipExtraRules.None, unit );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    unit.Stance = stance;
                                    stance.OnPlayerChooses.DuringGame_PlayAtLocation( unit.GetDrawLocation() );
                                    SimCommon.NeedsVisibilityGranterRecalculation = true; //otherwise the sidebar does not update properly
                                    LocationCalculationTypeTable.Instance.DoOnGameClear(); //otherwise calculated data will be stale
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                }

                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }
            #endregion

            #region OnUpdate_Content_Vehicle
            private void OnUpdate_Content_Vehicle( float runningY, ISimMachineVehicle vehicle )
            {
                if ( vehicle == null || vehicle.IsFullDead )
                    return;
                float x = 2.769974f;

                foreach ( MachineVehicleStance stance in MachineVehicleStanceTable.Instance.Rows )
                {
                    if ( stance.IsHidden )
                        continue;
                    if ( !vehicle.VehicleType.AvailableStances.ContainsKey( stance ) )
                        continue; //this is not an available stance to this vehicle, so nevermind!
                    if ( !stance.DuringGame_IsUnlocked() )
                        continue; //locked stances are considered hidden

                    bIconRow row = bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        break; //this was just time-slicing, so ignore that failure for now

                    bIconRowPool.ApplySingleItemInRow( row, x, runningY );
                    runningY -= 20.73f;
                    row.SetRelatedImage0SpriteIfNeeded( stance.Icon.GetSpriteForUI() );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( row, vehicle.Stance == stance, false );
                                    ExtraData.Buffer.AddRaw( stance.GetDisplayName(), colorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    stance.RenderMachineVehicleStanceTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraText.MachineUnitStanceInfo, TooltipExtraRules.None, vehicle );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( !stance.DuringGame_IsUnlocked() )
                                        return;

                                    vehicle.Stance = stance;
                                    stance.OnPlayerChooses.DuringGame_PlayAtLocation( vehicle.WorldLocation );
                                    SimCommon.NeedsVisibilityGranterRecalculation = true; //otherwise the sidebar does not update properly
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                }

                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }
            #endregion

            #region OnUpdate_Content_NPCUnit
            private void OnUpdate_Content_NPCUnit( float runningY, ISimNPCUnit npcUnit )
            {
                if ( npcUnit == null || npcUnit.IsFullDead )
                    return;
                float x = 2.769974f;

                foreach ( NPCUnitStance stance in NPCUnitStanceTable.PlayerStances )
                {
                    if ( stance.IsHidden )
                        continue;
                    if ( stance.IsInWorkOption && !InputCaching.Debug_IncludeInWorkProgress )
                        continue;
                    //if ( !stance.DuringGame_IsUnlocked() )
                    //    continue; //locked stances are considered hidden

                    bIconRow row = bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        break; //this was just time-slicing, so ignore that failure for now

                    bIconRowPool.ApplySingleItemInRow( row, x, runningY );
                    runningY -= 20.73f;
                    row.SetRelatedImage0SpriteIfNeeded( stance.Icon.GetSpriteForUI() );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( row, npcUnit.Stance == stance, false );
                                    ExtraData.Buffer.AddRaw( stance.GetDisplayName(), colorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    stance.RenderNPCStanceTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraText.BulkAndroidStanceInfo, TooltipExtraRules.None, null ); //npcUnit not sent for some reason
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    //if ( !stance.DuringGame_IsUnlocked() )
                                    //    return;

                                    npcUnit.Stance = stance;
                                    ParticleSoundRefs.ModeClick.DuringGame_PlayAtLocation( npcUnit.GetDrawLocation() );
                                    SimCommon.NeedsVisibilityGranterRecalculation = true; //otherwise the sidebar does not update properly
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                }

                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }
            #endregion
        }

        private static int lastYHeightOfInterior = 0;

		/// <summary>
		/// Top header
		/// </summary>
		public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "SelectUnitStance_Header" );
            }
            public override void OnUpdate() { }
        }

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

		public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            //think about it, we'll see
        }

        #region bIconRow
        public class bIconRow : ButtonAbstractBaseWithImage
        {
            public static bIconRow Original;
            public bIconRow() { if ( Original == null ) Original = this; }

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

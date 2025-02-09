using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using DiffLib;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ScrapUnitList : ToggleableWindowController, IInputActionHandler
    {
        public static ScrapUnitMode Mode = ScrapUnitMode.None;

        #region HandleOpenCloseToggle
        public static void HandleOpenCloseToggle( ScrapUnitMode NewMode )
        {
            if ( Instance.IsOpen && Mode == NewMode )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                //ArcenDebugging.LogSingleLine( "close from match", Verbosity.DoNotShow );
            }
            else
            {
                Window_ActorStanceChange.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
                Window_StructureChoiceList.Instance.Close( WindowCloseReason.OtherWindowCausingClose );

                Mode = NewMode;
                Instance.Open();
                //ArcenDebugging.LogSingleLine( "open", Verbosity.DoNotShow );
            }
        }
        #endregion

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }
            if ( Engine_HotM.CurrentLowerMode != null )
                return false;

            if ( Mode == ScrapUnitMode.None )
            {
                this.IsOpen = false;
                return false;
            }

            return base.GetShouldDrawThisFrame_Subclass();
        }

        public static Window_ScrapUnitList Instance;
        public Window_ScrapUnitList()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public override void OnClose( WindowCloseReason CloseReason )
        {
            Mode = ScrapUnitMode.None;
            base.OnClose( CloseReason );
        }

        private static float runningY = 0;

        #region RenderChoice
        public static bool RenderChoice( IA5Sprite Sprite, GetOrSetUIData UIData )
        {
            bIconRow row = customParent.bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            if ( row == null )
                return false; //this was just time-slicing, so ignore that failure for now

            float x = 2.769974f;
            customParent.bIconRowPool.ApplySingleItemInRow( row, x, runningY );
            runningY -= 20.73f;
            row.SetRelatedImage0SpriteIfNeeded( Sprite.GetSpriteForUI() );

            row.Assign( UIData );
            return true;
        }
        #endregion RenderChoice

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_ScrapUnitList.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            internal static ButtonAbstractBase.ButtonPool<bIconRow> bIconRowPool;

            private int heightToShow = 0;
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1.1f * GameSettings.Current.GetFloat( "Scale_StatsSidebar" );

                this.WindowController.ExtraOffsetX = -(Window_TaskStack.Instance.GetCurrentWidth_Scaled() + Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled());

                float extraY = Window_MainGameHeaderBarRight.GetYHeightForOtherWindowOffsets();
                this.WindowController.ExtraOffsetYRaw = extraY;


                if ( Window_ScrapUnitList.Instance != null )
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
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 1f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 1f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 1f );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( Mode == ScrapUnitMode.None )
                    Instance.Close( WindowCloseReason.ShowingRefused );
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                runningY = -3.069992f; //starting

                switch ( Mode )
                {
                    case ScrapUnitMode.Androids:
                        #region Androids
                        {
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                if ( !(actor is ISimMachineUnit unit) )
                                    continue; //skip non-units
                                if ( !(unit.UnitType?.IsConsideredAndroid??false) )
                                    continue; //skip anything that's not an android
                                if ( actor.CurrentActionOverTime != null )
                                    continue;

                                RenderChoice( unit.GetShapeIcon(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( unit == null )
                                        return;
                                    MachineUnitType unitType = unit?.UnitType;
                                    if ( unitType == null )
                                        return;

                                    try
                                    {

                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    int currentHealth = unit.GetActorDataCurrent( ActorRefs.ActorHP, true );
                                                    int maxHealth = unit.GetActorDataMaximum( ActorRefs.ActorHP, true );
                                                    int healthPercentage = MathA.IntPercentageClamped( currentHealth, maxHealth, 1, 100 );
                                                    ExtraData.Buffer.StartSize80().AddRaw( healthPercentage.ToStringIntPercent(), ColorTheme.SmallPopupTextSelected ).EndSize();

                                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );
                                                    ExtraData.Buffer.Position40().AddRaw( unit.GetDisplayName(), colorHex );
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", unitType.UnitCapacityCost.ToString(), ColorTheme.RedOrange2 );
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    unit.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.FocusDestroy, TooltipExtraRules.None );
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                {
                                                    if ( ExtraData.MouseInput.RightButtonClicked )
                                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                                    else
                                                    {
                                                        if ( !actor.PopupReasonCannotScrapIfCannot( ScrapReason.ReplacementByPlayer ) )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }
                                                        if ( actor.GetIsBlockedFromBeingScrappedRightNow() )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }

                                                        if ( unitType.GetIsOverCap() )
                                                        {
                                                            //we are still at cap, so we're going to kill the unit we clicked if the below succeeds

                                                            unit.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ReplacementByPlayer );
                                                        }
                                                        else
                                                        {
                                                            //oh hey, we're no longer at cap, so just create the unit fresh, neat
                                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        ArcenDebugging.LogSingleLine( "Androids: " + Action + " " + e, Verbosity.ShowAsError );
                                    }
                                } );
                            }
                        }
                        #endregion Androids
                        break;
                    case ScrapUnitMode.Mechs:
                        #region Mechs
                        {
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                if ( !(actor is ISimMachineUnit unit) )
                                    continue; //skip non-units
                                if ( !(unit.UnitType?.IsConsideredMech??false) )
                                    continue; //skip anything that's not a mech
                                if ( actor.CurrentActionOverTime != null )
                                    continue;

                                RenderChoice( unit.GetShapeIcon(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( unit == null )
                                        return;
                                    MachineUnitType unitType = unit?.UnitType;
                                    if ( unitType == null )
                                        return;

                                    try
                                    {

                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    int currentHealth = unit.GetActorDataCurrent( ActorRefs.ActorHP, true );
                                                    int maxHealth = unit.GetActorDataMaximum( ActorRefs.ActorHP, true );
                                                    int healthPercentage = MathA.IntPercentageClamped( currentHealth, maxHealth, 1, 100 );
                                                    ExtraData.Buffer.StartSize80().AddRaw( healthPercentage.ToStringIntPercent(), ColorTheme.SmallPopupTextSelected ).EndSize();

                                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );
                                                    ExtraData.Buffer.Position40().AddRaw( unit.GetDisplayName(), colorHex );
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", unitType.UnitCapacityCost.ToString(), ColorTheme.RedOrange2 );
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    unit.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.FocusDestroy, TooltipExtraRules.None );
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                {
                                                    if ( ExtraData.MouseInput.RightButtonClicked )
                                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( unit.GetDrawLocation(), false );
                                                    else
                                                    {
                                                        if ( !actor.PopupReasonCannotScrapIfCannot( ScrapReason.ReplacementByPlayer ) )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }
                                                        if ( actor.GetIsBlockedFromBeingScrappedRightNow() )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }

                                                        if ( unitType.GetIsOverCap() )
                                                        {
                                                            //we are still at cap, so we're going to kill the unit we clicked if the below succeeds

                                                            unit.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ReplacementByPlayer );
                                                        }
                                                        else
                                                        {
                                                            //oh hey, we're no longer at cap, so just create the unit fresh, neat
                                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        ArcenDebugging.LogSingleLine( "Mechs: " + Action + " " + e, Verbosity.ShowAsError );
                                    }
                                } );
                            }
                        }
                        #endregion Mechs
                        break;
                    case ScrapUnitMode.Vehicles:
                        #region Vehicles
                        {
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                if ( !(actor is ISimMachineVehicle vehicle) )
                                    continue; //skip non-units
                                if ( actor.CurrentActionOverTime != null )
                                    continue;

                                RenderChoice( vehicle.GetShapeIcon(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( vehicle == null )
                                        return;
                                    MachineVehicleType vehicleType = vehicle?.VehicleType;
                                    if ( vehicleType == null )
                                        return;

                                    try
                                    {
                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    int currentHealth = vehicle.GetActorDataCurrent( ActorRefs.ActorHP, true );
                                                    int maxHealth = vehicle.GetActorDataMaximum( ActorRefs.ActorHP, true );
                                                    int healthPercentage = MathA.IntPercentageClamped( currentHealth, maxHealth, 1, 100 );
                                                    ExtraData.Buffer.StartSize80().AddRaw( healthPercentage.ToStringIntPercent(), ColorTheme.SmallPopupTextSelected ).EndSize();

                                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );
                                                    ExtraData.Buffer.Position40().AddRaw( vehicle.GetDisplayName(), colorHex );
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", vehicleType.VehicleCapacityCost.ToString(), ColorTheme.RedOrange2 );
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    vehicle.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.FocusDestroy, TooltipExtraRules.None );
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                {
                                                    if ( ExtraData.MouseInput.RightButtonClicked )
                                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( vehicle.GetDrawLocation(), false );
                                                    else
                                                    {
                                                        if ( !vehicle.PopupReasonCannotScrapIfCannot( ScrapReason.ReplacementByPlayer ) )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }
                                                        if ( vehicle.GetIsBlockedFromBeingScrappedRightNow() )
                                                        {
                                                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                                            break;
                                                        }

                                                        if ( vehicleType.GetIsOverCap() )
                                                        {
                                                            //we are still at cap, so we're going to kill the unit we clicked if the below succeeds

                                                            vehicle.TryScrapRightNowWithoutWarning_Danger( ScrapReason.ReplacementByPlayer );
                                                        }
                                                        else
                                                        {
                                                            //oh hey, we're no longer at cap, so just create the unit fresh, neat
                                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        ArcenDebugging.LogSingleLine( "Vehicles: " + Action + " " + e, Verbosity.ShowAsError );
                                    }
                                } );
                            }
                        }
                        #endregion Vehicles
                        break;
                    case ScrapUnitMode.BulkAndroids:
                        #region BulkAndroids
                        {
                            foreach ( ISimNPCUnit npc in SimCommon.AllPlayerRelatedNPCUnits.GetDisplayList() )
                            {
                                if ( npc.IsFullDead || npc == null )
                                    continue;
                                NPCUnitType npcType = npc?.UnitType;
                                if ( npcType == null )
                                    return;
                                if ( npcType == null || npcType.CostsToCreateIfBulkAndroid.Count == 0 )
                                    continue;

                                if ( !npc.GetIsPlayerControlled() )
                                    continue;

                                RenderChoice( npc.GetShapeIcon(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( npc == null || npcType == null )
                                        return;
                                    try
                                    {
                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    int currentHealth = npc.GetActorDataCurrent( ActorRefs.ActorHP, true );
                                                    int maxHealth = npc.GetActorDataMaximum( ActorRefs.ActorHP, true );
                                                    int healthPercentage = MathA.IntPercentageClamped( currentHealth, maxHealth, 1, 100 );
                                                    ExtraData.Buffer.StartSize80().AddRaw( healthPercentage.ToStringIntPercent(), ColorTheme.SmallPopupTextSelected).EndSize();

                                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );
                                                    ExtraData.Buffer.Position40().AddRaw( npc.GetDisplayName(), colorHex ).Space2x();
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", npcType?.BulkUnitCapacityRequired.ToString(), ColorTheme.RedOrange2 );
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    npc.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.FocusDestroy, TooltipExtraRules.None );
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                {
                                                    if ( ExtraData.MouseInput.RightButtonClicked )
                                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( npc.GetDrawLocation(), false );
                                                    else
                                                    {
                                                        if ( npcType.GetIsOverCapIfBelongsToPlayer() )
                                                        {
                                                            //we are still at cap, so we're going to kill the unit we clicked if the below succeeds

                                                            npc.DisbandNPCUnit( NPCDisbandReason.PlayerRequestForOwnUnit );
                                                        }
                                                        else
                                                        {
                                                            //oh hey, we're no longer at cap, so just create the unit fresh, neat
                                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        ArcenDebugging.LogSingleLine( "BulkAndroids: " + Action + " " + e, Verbosity.ShowAsError );
                                    }
                                } );
                            }
                        }
                        #endregion BulkAndroids
                        break;
                    case ScrapUnitMode.CapturedUnits:
                        #region CapturedUnits
                        {
                            foreach ( ISimNPCUnit npc in SimCommon.AllPlayerRelatedNPCUnits.GetDisplayList() )
                            {
                                if ( npc.IsFullDead || npc == null )
                                    continue;
                                NPCUnitType npcType = npc?.UnitType;
                                if ( npcType == null )
                                    return;
                                if ( npcType == null || npcType.CostsToCreateIfBulkAndroid.Count > 0 )
                                    continue;

                                if ( !npc.GetIsPlayerControlled() )
                                    continue;

                                RenderChoice( npc.GetShapeIcon(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    if ( npc == null )
                                        return;
                                    if ( npcType == null )
                                        return;
                                    try
                                    {

                                        switch ( Action )
                                        {
                                            case UIAction.GetTextToShowFromVolatile:
                                                {
                                                    int currentHealth = npc.GetActorDataCurrent( ActorRefs.ActorHP, true );
                                                    int maxHealth = npc.GetActorDataMaximum( ActorRefs.ActorHP, true );
                                                    int healthPercentage = MathA.IntPercentageClamped( currentHealth, maxHealth, 1, 100 );
                                                    ExtraData.Buffer.StartSize80().AddRaw( healthPercentage.ToStringIntPercent(), ColorTheme.SmallPopupTextSelected).EndSize();

                                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );
                                                    ExtraData.Buffer.Position40().AddRaw( npc.GetDisplayName(), colorHex ).Space2x();
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", npcType.CapturedUnitCapacityRequired.ToString(), ColorTheme.RedOrange2 );
                                                }
                                                break;
                                            case UIAction.HandleMouseover:
                                                {
                                                    npc.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.FocusDestroy, TooltipExtraRules.None );
                                                }
                                                break;
                                            case UIAction.OnClick:
                                                {
                                                    if ( ExtraData.MouseInput.RightButtonClicked )
                                                        VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( npc.GetDrawLocation(), false );
                                                    else
                                                    {
                                                        if ( npcType.GetIsOverCapIfBelongsToPlayer() )
                                                        {
                                                            //we are still at cap, so we're going to kill the unit we clicked if the below succeeds

                                                            npc.DisbandNPCUnit( NPCDisbandReason.PlayerRequestForOwnUnit );
                                                        }
                                                        else
                                                        {
                                                            //oh hey, we're no longer at cap, so just create the unit fresh, neat
                                                            Instance.Close( WindowCloseReason.UserDirectRequest );
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    catch ( Exception e )
                                    {
                                        ArcenDebugging.LogSingleLine( "CapturedUnits: " + Action + " " + e, Verbosity.ShowAsError );
                                    }
                                } );
                            }
                        }
                        #endregion CapturedUnits
                        break;
                }

                if ( bMainContentParent.ParentRT )
                    bMainContentParent.ParentRT.UI_SetHeight( MathA.Abs( runningY ) );
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
                switch ( Mode )
                {
                    case ScrapUnitMode.Androids:
                        {
                            int current = SimCommon.TotalCapacityUsed_Androids;
                            int max = MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt;
                            Buffer.AddRawAndAfterLineItemHeader( MathRefs.MaxAndroidCapacity.GetDisplayName() )
                                .AddFormat2( "OutOF", current, max, current > max ? ColorTheme.RedOrange2 : string.Empty );

                            if ( current <= max )
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                        }
                        break;
                    case ScrapUnitMode.Mechs:
                        {
                            int current = SimCommon.TotalCapacityUsed_Mechs;
                            int max = MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt;
                            Buffer.AddRawAndAfterLineItemHeader( MathRefs.MaxMechCapacity.GetDisplayName() )
                                .AddFormat2( "OutOF", current, max, current > max ? ColorTheme.RedOrange2 : string.Empty );

                            if ( current <= max )
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                        }
                        break;
                    case ScrapUnitMode.Vehicles:
                        {
                            int current = SimCommon.TotalCapacityUsed_Vehicles;
                            int max = MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt;
                            Buffer.AddRawAndAfterLineItemHeader( MathRefs.MaxVehicleCapacity.GetDisplayName() )
                                .AddFormat2( "OutOF", current, max, current > max ? ColorTheme.RedOrange2 : string.Empty );

                            if ( current <= max )
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                        }
                        break;
                    case ScrapUnitMode.BulkAndroids:
                        {
                            int current = SimCommon.TotalBulkUnitSquadCapacityUsed;
                            int max = MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt;
                            Buffer.AddRawAndAfterLineItemHeader( MathRefs.BulkUnitCapacity.GetDisplayName() )
                                .AddFormat2( "OutOF", current, max, current > max ? ColorTheme.RedOrange2 : string.Empty );

                            if ( current <= max )
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                        }
                        break;
                    case ScrapUnitMode.CapturedUnits:
                        {
                            int current = SimCommon.TotalCapturedUnitSquadCapacityUsed;
                            int max = MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt;
                            Buffer.AddRawAndAfterLineItemHeader( MathRefs.CapturedUnitCapacity.GetDisplayName() )
                                .AddFormat2( "OutOF", current, max, current > max ? ColorTheme.RedOrange2 : string.Empty );

                            if ( current <= max )
                                Instance.Close( WindowCloseReason.UserDirectRequest );
                        }
                        break;
                }
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

    public enum ScrapUnitMode
    {
        None,
        Androids,
        Mechs,
        Vehicles,
        BulkAndroids,
        CapturedUnits
    }
}

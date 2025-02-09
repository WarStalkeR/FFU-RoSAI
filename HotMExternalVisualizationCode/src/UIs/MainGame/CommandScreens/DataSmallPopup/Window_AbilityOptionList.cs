using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_AbilityOptionList : ToggleableWindowController, IInputActionHandler
    {
        public static ISimMachineActor ActorDoingAbility = null;
        public static AbilityType AbilityWeAreDoing = null;
        
        #region HandleOpenCloseToggle
        public static void HandleOpenCloseToggle( ISimMachineActor Actor, AbilityType Ability )
        {
            if ( Instance.IsOpen && ActorDoingAbility == Actor && AbilityWeAreDoing == Ability )
                Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                ActorDoingAbility = Actor;
                AbilityWeAreDoing = Ability;
                Instance.Open();
            }
        }
        #endregion

        #region CloseIfAlreadyTargetingThis
        public static bool CloseIfAlreadyTargetingThis( ISimMachineActor Actor, AbilityType Ability )
        {
            if ( Instance.IsOpen && ActorDoingAbility == Actor && AbilityWeAreDoing == Ability )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return true;
            }
            return false;
        }
        #endregion

        #region GetIsOpenAndFocusedOn
        public static bool GetIsOpenAndFocusedOn( AbilityType Ability )
        {
            return Instance.IsOpen && AbilityWeAreDoing == Ability;
        }
        #endregion

        #region CloseIfOpen
        public static void CloseIfOpen()
        {
            Instance.Close( WindowCloseReason.UserDirectRequest );
        }
        #endregion

        public override void OnOpen()
        {
            if ( Window_VehicleUnitPanel.Instance.IsOpen )
                Window_VehicleUnitPanel.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
        }

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

            if ( Engine_HotM.SelectedMachineActionMode != null )
                return false;

            return base.GetShouldDrawThisFrame_Subclass();
        }

        public static Window_AbilityOptionList Instance;
        public Window_AbilityOptionList()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public override void OnClose( WindowCloseReason CloseReason )
        {
            ActorDoingAbility = null;
            AbilityWeAreDoing = null;
            base.OnClose( CloseReason );
        }

        private static float runningY = 0;

        #region RenderAbilityOption
        public static bool RenderAbilityOption( IA5Sprite Sprite, GetOrSetUIData UIData )
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
        #endregion RenderAbilityOption

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_AbilityOptionList.CustomParentInstance = this;
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
                this.WindowController.myScale = 1f * GameSettings.Current.GetFloat( "Scale_AbilityFooterBar" );

                this.WindowController.ExtraOffsetXRaw = Window_ActorSidebarStatsLowerLeft.GetXWidthForOtherWindowOffsets();
                this.WindowController.ExtraOffsetY = -Window_AbilityFooterBar.Instance.GetAbilityBarCurrentHeight_Scaled();

                if ( Window_AbilityOptionList.Instance != null )
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
                    //appear from the bottom up
                    this.Element.RelevantRect.anchorMin = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.pivot = new Vector2( 0f, 0f );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( ActorDoingAbility != Engine_HotM.SelectedActor || //ActorDoingAbility.GetIsBlockedFromActingInGeneral() ||
                    ActorDoingAbility.IsFullDead || ActorDoingAbility.IsInvalid )
                    Instance.Close( WindowCloseReason.ShowingRefused );
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                runningY = -3.069992f; //starting

                ISimMachineActor actor = ActorDoingAbility;
                if ( actor == null || actor.IsFullDead || actor.IsInvalid )
                    return;
                AbilityType ability = AbilityWeAreDoing;
                if ( ability == null )
                    return;

                if ( actor is ISimMachineUnit unit )
                {
                    ISimBuilding currentBuildingOrNull = unit.ContainerLocation.Get() as ISimBuilding;
                    Vector3 location = unit.GetDrawLocation();
                    ability.Implementation.TryHandleAbility( actor, currentBuildingOrNull, location, ability, null, ActorAbilityLogic.HandleOptionsListPopulation );
                }
                else if ( actor is ISimMachineVehicle vehicle )
                {
                    Vector3 location = vehicle.WorldLocation;
                    ability.Implementation.TryHandleAbility( actor, null, location, ability, null, ActorAbilityLogic.HandleOptionsListPopulation );
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
                if ( AbilityWeAreDoing != null)
                    Buffer.AddRaw( AbilityWeAreDoing.GetDisplayName() );
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

using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LensFilters : ToggleableWindowController, IInputActionHandler
    {        
        #region HandleOpenCloseToggle
        public static void HandleOpenCloseToggle()
        {
            if ( Instance.IsOpen )
                Instance.Close( WindowCloseReason.UserDirectRequest );
            else
                Instance.Open();
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
            return base.GetShouldDrawThisFrame_Subclass();
        }

        public static Window_LensFilters Instance;
        public Window_LensFilters()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public override void OnClose( WindowCloseReason CloseReason )
        {
            base.OnClose( CloseReason );
        }

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_LensFilters.CustomParentInstance = this;
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

                float offsetFromRight = -Window_RadialMenu.Instance.GetRadialMenuCurrentWidth_Scaled();
                if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                    offsetFromRight -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetX = offsetFromRight;
                //this.WindowController.ExtraOffsetY = -Window_AbilityFooterBar.Instance.GetAbilityBarCurrentHeight_Scaled();

                if ( Window_LensFilters.Instance != null )
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
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                float runningY = -3.069992f; //starting
                float x = 2.769974f;

                CityLensType lens = SimCommon.CurrentCityLens;
                if ( lens == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                foreach ( CityLensFilter filter in lens.FilterList )
                {
                    bIconRow row = bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        break; //this was just time-slicing, so ignore that failure for now

                    bIconRowPool.ApplySingleItemInRow( row, x, runningY );
                    runningY -= 20.73f;

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    string colorHex = UIHelper.SetSmallOptionBGAndGetColor( row, filter.DuringGame_IsActive, false );
                                    ExtraData.Buffer.Space1x().AddRaw( filter.DisplayName.Text, colorHex );
                                    row.SetRelatedImage0SpriteIfNeeded( ( filter.DuringGame_IsActive ? IconRefs.LensFilterActive : IconRefs.LensFilterDisabled ).Icon.GetSpriteForUI() );
                                    row.SetRelatedImage0ColorFromHexIfNeeded( colorHex );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {                                    
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Filter", filter?.ID ?? "Null" ), element, 
                                        SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( filter.DisplayName.Text );

                                        if ( !filter.Description.Text.IsEmpty() )
                                            novel.Main.AddRaw( filter.Description.Text, ColorTheme.NarrativeColor ).Line();
                                        if ( !filter.StrategyTip.Text.IsEmpty() )
                                            novel.Main.AddRaw( filter.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                            .AddLang( filter.IsEnabledByDefault ? "On" : "Off", ColorTheme.DataBlue ).Line();
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    filter.DuringGame_IsActive = !filter.DuringGame_IsActive;
                                }
                                break;
                        }
                    } );
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
                CityLensType lens = SimCommon.CurrentCityLens;
                if ( lens == null )
                    return;
                Buffer.AddRaw( lens.GetDisplayName() );
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

        #region imgBasicWindowBG
        public class imgBasicWindowBG : ImageAbstractBase
        {
            public override void HandleClick( MouseHandlingInput input )
            {
                //if ( input.RightButtonClicked )
                Instance.Close( WindowCloseReason.UserCasualRequest );
            }
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;


namespace Arcen.HotM.ExternalVis
{
    public class Window_Sidebar : ToggleableWindowController, IInputActionHandler
    {
        public UISidebarType CurrentTab;

        public static Window_Sidebar Instance;
        public Window_Sidebar()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        #region GetSidebarCurrentWidth_Scaled
        /// <summary>
        /// Gets the amount of horizontal space the sidebar will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetSidebarCurrentWidth_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 157f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        private static RectTransform SizingRect;

        #region GetMaxXForTooltips
        public static float GetMaxXForTooltips()
        {
            if ( SizingRect == null )
                return 0;
            return SizingRect.GetWorldSpaceBottomLeftCorner().x;
        }
        #endregion

        #region GetMinXWhenOnLeft
        public static float GetMinXWhenOnLeft()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceTopRightCorner().x;
        }
        #endregion

        #region GetMaxXWhenOnRight
        public static float GetMaxXWhenOnRight()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceBottomLeftCorner().x;
        }
        #endregion

        #region GetXWidthWhenOnRight
        public static float GetXWidthWhenOnRight()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() )
                return 0;
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().x;
        }
        #endregion

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !base.GetShouldDrawThisFrame_Subclass() )
                return false;

            if ( !IsOpen )
                return false;

            if ( VisCurrent.GetShouldBeBlurred() )
                return false;

            return true;
        }

        private static int lastNumberOfCompleteClears = 0;

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( Engine_HotM.NumberOfCompleteClears != lastNumberOfCompleteClears )
            {
                Set.RefreshAllElements = true;
                lastNumberOfCompleteClears = Engine_HotM.NumberOfCompleteClears;
                return;
            }

            //bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public static bMainContentParent Instance;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    Instance = this;
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        /// <summary>
		/// Top header
		/// </summary>
		public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Instance.CurrentTab != null )
                    Buffer.AddRaw( Instance.CurrentTab.GetDisplayName() );
            }
            public override void OnUpdate() { }
        }

        #region GetSidebarColorFromHex
        private static readonly Dictionary<string, Color> sidebarHexColors = Dictionary<string, Color>.Create_WillNeverBeGCed( 200, "Window_Sidebar-sidebarHexColors" );
        public static Color GetSidebarColorFromHex( string hex )
        {
            if ( hex == null || hex.Length == 0 )
                return ColorMath.White;

            if ( !sidebarHexColors.TryGetValue( hex, out Color c ) )
            {
                c = ColorMath.HexToColor( hex );
                sidebarHexColors[hex] = c;
            }
            return c;
        }
        #endregion

        public static ImageButtonAbstractBase.ImageButtonPool<bSidebarItemDouble> bSidebarItemDoublePool;
        public static ImageButtonAbstractBase.ImageButtonPool<bSidebarItemSingle> bSidebarItemSinglePool;
        public static ImageButtonAbstractBase.ImageButtonPool<bSidebarUnit> bSidebarUnitButtonPool;
        public static ButtonAbstractBase.ButtonPool<bSidebarTextHeader> bSidebarTextHeaderPool;

        private const float SIDEBAR_ITEM_WIDTH = 180f;
        private const float SIDEBAR_ITEM_HEIGHT = 24f;
        
        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                AdjustHeightToScreenMax( 0, 1f, string.Empty, 0.8f, "Scale_Sidebar", 1.2f, "Scale_HeaderBar" );

                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bSidebarItemDouble.Original != null && bSidebarItemSingle.Original != null &&
                            bSidebarUnit.Original != null && bSidebarTextHeader.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bSidebarItemDoublePool = new ImageButtonAbstractBase.ImageButtonPool<bSidebarItemDouble>( bSidebarItemDouble.Original, 10 );
                            bSidebarItemSinglePool = new ImageButtonAbstractBase.ImageButtonPool<bSidebarItemSingle>( bSidebarItemSingle.Original, 10 );
                            bSidebarUnitButtonPool = new ImageButtonAbstractBase.ImageButtonPool<bSidebarUnit>( bSidebarUnit.Original, 10 );
                            bSidebarTextHeaderPool = new ButtonAbstractBase.ButtonPool<bSidebarTextHeader>( bSidebarTextHeader.Original, 10, "bSidebarTextHeader" );
                        }
                    }
                    #endregion
                }

                if ( !hasGlobalInitialized )
                    return;

                if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                {
                    if ( Instance.IsOpen ) //this could wind up being still visible onto the main menu
                        Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                if ( Instance.CurrentTab == null )
                {
                    UISidebarType[] tabs = UISidebarTypeTable.Instance.Rows;
                    if ( tabs.Length > 0 )
                        Instance.CurrentTab = tabs[0];
                }

                bSidebarItemDoublePool.Clear( 80 );
                bSidebarItemSinglePool.Clear( 80 );
                bSidebarUnitButtonPool.Clear( 80 );
                bSidebarTextHeaderPool.Clear( 80 );

                float currentY = -7f;

                if ( Instance.CurrentTab != null && ArcenUI.CurrentlyShownWindowsWith_ShouldBlurBackgroundGame.Count <= 0 )
                    Instance.CurrentTab.Implementation.WriteAnySidebarItems( ref currentY );

                #region Positioning Logic
                SizingRect = this.Element.RelevantRect;
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bSidebarItemDouble.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region ApplySingleItemInRow
            public static void ApplySingleItemInRow( ImageButtonAbstractBase bItem, float currentX, ref float currentY, float ROW_ADVANCE, float WIDTH, float HEIGHT )
            {
                if ( bItem.ExtraSpaceBeforeInAutoSizing != 0f )
                {
                    currentY -= bItem.ExtraSpaceBeforeInAutoSizing;
                    bItem.ExtraSpaceBeforeInAutoSizing = 0f;
                }

                RectTransform relevantRect = bItem.Element.RelevantRect;
                relevantRect.anchoredPosition = new Vector2( currentX, currentY );
                relevantRect.localScale = Vector3.one;
                relevantRect.localRotation = Quaternion.identity;
                if ( bItem.AlternativeHeightToUseInAutoSizing > 0f )
                {
                    //relevantRect.sizeDelta = new Vector2( WIDTH, bItem.AlternativeHeightToUseInAutoSizing );
                    currentY -= ROW_ADVANCE - (HEIGHT - bItem.AlternativeHeightToUseInAutoSizing);
                    bItem.AlternativeHeightToUseInAutoSizing = 0f;
                }
                else
                {
                    //relevantRect.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentY -= ROW_ADVANCE;
                }

                if ( bItem.ExtraSpaceAfterInAutoSizing != 0f )
                {
                    currentY -= bItem.ExtraSpaceAfterInAutoSizing;
                    bItem.ExtraSpaceAfterInAutoSizing = 0f;
                }
            }

            public static void ApplySingleItemInRow( ButtonAbstractBase bItem, float currentX, ref float currentY, float ROW_ADVANCE, float WIDTH, float HEIGHT )
            {
                if ( bItem.ExtraSpaceBeforeInAutoSizing != 0f )
                {
                    currentY -= bItem.ExtraSpaceBeforeInAutoSizing;
                    bItem.ExtraSpaceBeforeInAutoSizing = 0f;
                }

                RectTransform relevantRect = bItem.Element.RelevantRect;
                relevantRect.anchoredPosition = new Vector2( currentX, currentY );
                relevantRect.localScale = Vector3.one;
                relevantRect.localRotation = Quaternion.identity;
                if ( bItem.AlternativeHeightToUseInAutoSizing > 0f )
                {
                    //relevantRect.sizeDelta = new Vector2( WIDTH, bItem.AlternativeHeightToUseInAutoSizing );
                    currentY -= ROW_ADVANCE - (HEIGHT - bItem.AlternativeHeightToUseInAutoSizing);
                    bItem.AlternativeHeightToUseInAutoSizing = 0f;
                }
                else
                {
                    //relevantRect.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentY -= ROW_ADVANCE;
                }

                if ( bItem.ExtraSpaceAfterInAutoSizing != 0f )
                {
                    currentY -= bItem.ExtraSpaceAfterInAutoSizing;
                    bItem.ExtraSpaceAfterInAutoSizing = 0f;
                }
            }
            #endregion
        }

        #region bSidebarItemDouble
        public class bSidebarItemDouble : ImageButtonAbstractBase
        {
            public static bSidebarItemDouble Original;
            public bSidebarItemDouble() { if ( Original == null ) Original = this; }

            private ISidebarItem priorItem = null;
            private ISidebarItem ItemItself = null;
            private ISidebarCustomHandler ItemCustomHandler;

            private bool hasEverAssigned = false;

            public void Assign( ISidebarItem Item, ISidebarCustomHandler ItemCustomHandler )
            {
                this.ItemItself = Item;
                this.ItemCustomHandler = ItemCustomHandler;

                if ( !this.hasEverAssigned && this.button != null )
                {
                    this.button.OptionalGetterAndSetter = delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( this.ItemItself == null )
                            return;
                        if ( this.ItemCustomHandler != null )
                            this.ItemCustomHandler.Sidebar_GetOrSetUIData( this.ItemItself, this, null, Element, Action, ref ExtraData );
                        else
                            this.ItemItself.Sidebar_GetOrSetUIData( this, null, Element, Action, ref ExtraData );
                    };
                    this.hasEverAssigned = true;
                }

                if ( this.priorItem != this.ItemItself )
                {
                    this.priorItem = this.ItemItself;
                    (this.Element as ArcenUI_ImageButton)?.TriggerImageAndTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_ImageButton)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override bool GetShouldBeHidden() => this.ItemItself == null;

            public override void Clear()
            {
                this.ItemItself = null;
                this.ItemCustomHandler = null;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {}
        }
        #endregion

        #region bSidebarItemSingle
        public class bSidebarItemSingle : ImageButtonAbstractBase
        {
            public static bSidebarItemSingle Original;
            public bSidebarItemSingle() { if ( Original == null ) Original = this; }

            private ISidebarItem priorItem = null;
            private ISidebarItem ItemItself = null;
            private ISidebarCustomHandler ItemCustomHandler;

            private bool hasEverAssigned = false;

            public void Assign( ISidebarItem Item, ISidebarCustomHandler ItemCustomHandler )
            {
                this.ItemItself = Item;
                this.ItemCustomHandler = ItemCustomHandler;

                if ( !this.hasEverAssigned && this.button != null )
                {
                    this.button.OptionalGetterAndSetter = delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( this.ItemItself == null )
                            return;
                        if ( this.ItemCustomHandler != null )
                            this.ItemCustomHandler.Sidebar_GetOrSetUIData( this.ItemItself, this, null, Element, Action, ref ExtraData );
                        else
                            this.ItemItself.Sidebar_GetOrSetUIData( this, null, Element, Action, ref ExtraData );
                    };
                    this.hasEverAssigned = true;
                }

                if ( this.priorItem != this.ItemItself )
                {
                    this.priorItem = this.ItemItself;
                    (this.Element as ArcenUI_ImageButton)?.TriggerImageAndTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_ImageButton)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override bool GetShouldBeHidden() => this.ItemItself == null;

            public override void Clear()
            {
                this.ItemItself = null;
                this.ItemCustomHandler = null;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {}
        }
        #endregion

        #region bSidebarUnit
        public class bSidebarUnit : ImageButtonAbstractBase
        {
            public static bSidebarUnit Original;
            public bSidebarUnit() { if ( Original == null ) Original = this; }

            private ISidebarItem priorItem = null;
            private ISidebarItem ItemItself = null;
            private ISidebarCustomHandler ItemCustomHandler;

            private bool hasEverAssigned = false;

            public void Assign( ISidebarItem Item, ISidebarCustomHandler ItemCustomHandler )
            {
                this.ItemItself = Item;
                this.ItemCustomHandler = ItemCustomHandler;

                if ( !this.hasEverAssigned && this.button != null )
                {
                    this.button.OptionalGetterAndSetter = delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( this.ItemItself == null )
                            return;
                        if ( this.ItemCustomHandler != null )
                            this.ItemCustomHandler.Sidebar_GetOrSetUIData( this.ItemItself, this, null, Element, Action, ref ExtraData );
                        else
                            this.ItemItself.Sidebar_GetOrSetUIData( this, null, Element, Action, ref ExtraData );
                    };
                    this.hasEverAssigned = true;
                }

                if ( this.priorItem != this.ItemItself )
                {
                    this.priorItem = this.ItemItself;
                    (this.Element as ArcenUI_ImageButton)?.TriggerImageAndTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_ImageButton)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override bool GetShouldBeHidden() => this.ItemItself == null;

            public override void Clear()
            {
                this.ItemItself = null;
                this.ItemCustomHandler = null;
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            { }
        }
        #endregion

        #region bSidebarTextHeader
        public class bSidebarTextHeader : ButtonAbstractBase
        {
            public static bSidebarTextHeader Original;
            public bSidebarTextHeader() { if ( Original == null ) Original = this; }

            private ISidebarItem priorItem = null;
            private ISidebarItem ItemItself = null;
            private ISidebarCustomHandler ItemCustomHandler;

            private bool hasEverAssigned = false;

            public void Assign( ISidebarItem Item, ISidebarCustomHandler ItemCustomHandler )
            {
                this.ItemItself = Item;
                this.ItemCustomHandler = ItemCustomHandler;

                if ( !this.hasEverAssigned && this.button != null )
                {
                    this.button.OptionalGetterAndSetter = delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( this.ItemItself == null )
                            return;
                        if ( this.ItemCustomHandler != null )
                            this.ItemCustomHandler.Sidebar_GetOrSetUIData( this.ItemItself, null, this, Element, Action, ref ExtraData );
                        else
                            this.ItemItself.Sidebar_GetOrSetUIData(  null, this, Element, Action, ref ExtraData );
                    };
                    this.hasEverAssigned = true;
                }

                if ( this.priorItem != this.ItemItself )
                {
                    this.priorItem = this.ItemItself;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override bool GetShouldBeHidden() => this.ItemItself == null;

            public override void Clear()
            {
                this.ItemItself = null;
                this.ItemCustomHandler = null;
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
                Instance.CurrentTab = null;
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                //case "Cancel":
                //    this.Close();
                //    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                //    ArcenInput.BlockForAJustPartOfOneSecond();
                //    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

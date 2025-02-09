using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_MetaFlagsInfo : ToggleableWindowController, IInputActionHandler
    {
        public static Window_MetaFlagsInfo Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_MetaFlagsInfo()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public static string FilterText = string.Empty;
        private static readonly List<MetaFlag> sortedFilteredFlags = List<MetaFlag>.Create_WillNeverBeGCed( 800, "Window_MetaFlagsInfo-sortedFilteredFlags" );

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "MetaFlags_Header" ).Space1x().AddFormat1( "Parenthetical", sortedFilteredFlags.Count );
            }
        }

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_MetaFlagsInfo.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bDataItem> bDataItemPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;

                        if ( bDataItem.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bDataItemPool = new ButtonAbstractBase.ButtonPool<bDataItem>( bDataItem.Original, 10, "bDataItem" );
                        }
                    }
                    #endregion

                    bDataItemPool.Clear( 60 );

                    RectTransform rTran = (RectTransform)bDataItem.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = topBuffer;

                    this.OnUpdate_Content();

                    #region Positioning Logic
                    //Now size the parent, called Content, to get scrollbars to appear if needed.
                    Vector2 sizeDelta = rTran.sizeDelta;
                    sizeDelta.y = MathA.Abs( runningY );
                    rTran.sizeDelta = sizeDelta;
                    #endregion
                }
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load
            private const float MAIN_CONTENT_WIDTH = 280f;

            #region CalculateBoundsSingle
            protected float leftBuffer = 5.1f;
            protected float topBuffer = -2.55f;

            public const float CONTENT_ROW_HEIGHT_SHORT = 15.4f;
            public const float CONTENT_ROW_HEIGHT_TALL = 15.4f;
            public const float CONTENT_ROW_GAP_SHORT = 2.55f;
            public const float CONTENT_ROW_GAP_TALL = 2.55f;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY, bool IsTall )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );

                innerY -= IsTall ? CONTENT_ROW_HEIGHT_TALL + CONTENT_ROW_GAP_TALL : CONTENT_ROW_HEIGHT_SHORT + CONTENT_ROW_GAP_SHORT;
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                sortedFilteredFlags.Clear();

                #region First Get The Filtered List
                bool isFiltering = FilterText.Length > 0;
                foreach ( MetaFlag flag in MetaFlagTable.Instance.Rows )
                {
                    if ( flag.IsHidden )
                        continue; //disabled, evidently
                    if ( isFiltering )
                    {
                        if ( !flag.ID.Contains( FilterText, StringComparison.InvariantCultureIgnoreCase ) &&
                            !flag.GetDisplayName().Contains( FilterText, StringComparison.InvariantCultureIgnoreCase ) )
                            continue; //this was filtered out!
                    }

                    //make sure we have something to show
                    if ( flag.GetDisplayName().Length <= 0 )
                        flag.SetOriginalDisplayName( flag.ID );

                    sortedFilteredFlags.Add( flag );
                }
                #endregion

                #region Then Sort That List
                sortedFilteredFlags.Sort( delegate ( MetaFlag left, MetaFlag right )
                {
					int val = string.Compare(left.GetDisplayName(), right.GetDisplayName(), StringComparison.CurrentCulture);
					return val != 0 ? val : left.RowIndexNonSim.CompareTo(right.RowIndexNonSim);
				} );
                #endregion

                //now actually loop over the entries and add those that are in the viewing window
                foreach ( MetaFlag flagGlobal in sortedFilteredFlags )
                {
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    MetaFlag flag = flagGlobal;

                    bDataItem row = bDataItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        break; //this was just time-slicing, so ignore that failure for now
                    bDataItemPool.ApplySingleItemInRow( row, bounds, false );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isHovered = element.LastHadMouseWithin;
                                    row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                    ExtraData.Buffer.StartColor( isHovered ? ColorTheme.GetInvertibleListTextBlue_Selected( isHovered ) : ColorTheme.GetBasicLightTextPurple( isHovered ) );

                                    ExtraData.Buffer.StartSize90().AddRawAndAfterLineItemHeader( flag.GetDisplayName() );
                                    if ( flag.DuringGameplay_IsTripped )
                                        ExtraData.Buffer.AddLang( "FlagTripped", ColorTheme.SkillPaleGreen );
                                    else
                                        ExtraData.Buffer.AddLang( "FlagNotTripped", ColorTheme.Grayer );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( flag ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( flag.GetDisplayName() );
                                        if ( flag.DuringGameplay_IsTripped )
                                            novel.Main.AddLang( "FlagTripped", ColorTheme.SkillPaleGreen );
                                        else
                                            novel.Main.AddLang( "FlagNotTripped", ColorTheme.Grayer );
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                if ( SimCommon.IsCheatTimeline ) //only in cheat mode, we can adjust flags directly
                                    flag.SetTripTo( !flag.DuringGameplay_IsTripped );
                                break;
                        }
                    } );
                }
            }
            #endregion
        }

        public class bClose : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( LangCommon.Popup_Common_Close );
            }
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        #region iFilterText
        public class iFilterText : InputAbstractBase
        {
            public int maxTextLength = 50;
            public static iFilterText Instance;
            public iFilterText() { Instance = this; }
            public override char ValidateInput( char addedChar )
            {
                if ( this.GetText().Length >= this.maxTextLength )
                    return '\0';
                return addedChar;
            }

            public override void OnValueChanged( string newString )
            {
                FilterText = newString;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void OnEndEdit()
            {
                FilterText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( inputField == null )
                {
                    inputField = (this.Element as ArcenUI_Input);
                    inputField?.SetPlaceholderTextLangKey( "ResearchWindow_SearchText" );
                }
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                        ArcenInput.BlockForAJustPartOfOneSecond();
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
        #endregion

        //not actually needed at this time, but needed for compilation
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
            switch ( InputActionType.ID )
            {
                case "Return":
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }

        #region bDataItem
        public class bDataItem : ButtonAbstractBaseWithImage
        {
            public static bDataItem Original;
            public bDataItem() { if ( Original == null ) Original = this; }

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

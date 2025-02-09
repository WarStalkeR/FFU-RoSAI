using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_StructuresWithProblems : ToggleableWindowController, IInputActionHandler
    {
        public static Window_StructuresWithProblems Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_StructuresWithProblems()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        public static string FilterText = string.Empty;

        /// <summary>Top header</summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "HeaderBar_StructuresWithComplaints_Top" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public override void OnOpen()
        {
            hasSetFilterTextSinceStart = false;

            Structures_FilterText = string.Empty;

            if ( iFilterText.Instance != null )
                iFilterText.Instance.SetText( string.Empty );

            base.OnOpen();
        }

        private static bool hasSetFilterTextSinceStart = false;
        private static string Structures_FilterText = string.Empty;
        private static string Structures_LastFilterText = string.Empty;
        private static int Structures_lastTurn = 0;

        #region GetXWidthForOtherWindowOffsets
        public static float GetXWidthForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() || SizingRect == null )
                return 0; //hidden entirely!

            float height = SizingRect.GetWorldSpaceSize().x;
            //height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        public static RectTransform SizingRect;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {   
            public customParent()
            {
                Window_StructuresWithProblems.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bTextHeader> bTextHeaderPool;
            private static ImageButtonAbstractBase.ImageButtonPool<bProblemStructure> bProblemStructurePool;
            private static ImageButtonAbstractBase.ImageButtonPool<bHealthyStructure> bHealthyStructurePool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar || Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bTextHeader.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bTextHeaderPool = new ButtonAbstractBase.ButtonPool<bTextHeader>( bTextHeader.Original, 10, "bTextHeader" );
                            bProblemStructurePool = new ImageButtonAbstractBase.ImageButtonPool<bProblemStructure>( bProblemStructure.Original, 10 );
                            bHealthyStructurePool = new ImageButtonAbstractBase.ImageButtonPool<bHealthyStructure>( bHealthyStructure.Original, 10 );
                        }
                    }
                    #endregion

                    float offsetFromSide = 0;
                    if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                        offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                    else //the sidebar is on, and is on the left, move right
                        offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                    this.WindowController.ExtraOffsetX = offsetFromSide;

                    bTextHeaderPool.Clear( 60 );
                    bProblemStructurePool.Clear( 60 );
                    bHealthyStructurePool.Clear( 60 );

                    RectTransform rTran = (RectTransform)bTextHeader.Original.Element.RelevantRect.parent;

                    maxYToShow = -rTran.anchoredPosition.y;
                    minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                    maxYToShow += EXTRA_BUFFER;

                    runningY = topBuffer;

                    this.OnUpdate_Content();

                    if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                        Instance.Close( WindowCloseReason.ShowingRefused );

                    #region Positioning Logic
                    //Now size the parent, called Content, to get scrollbars to appear if needed.
                    Vector2 sizeDelta = rTran.sizeDelta;
                    sizeDelta.y = MathA.Abs( runningY );
                    rTran.sizeDelta = sizeDelta;
                    #endregion

                    SizingRect = this.Element.RelevantRect;
                }
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 400; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load
            private const float MAIN_CONTENT_WIDTH = 280f;
            private const float CONTENT_ROW_GAP = 2f;
            private const float CONTENT_ROW = 24f;
            private const float HEADER_ROW = 41f;

            #region CalculateBoundsSingle
            protected float leftBuffer = 5.1f;
            protected float topBuffer = -6;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, CONTENT_ROW );

                innerY -= CONTENT_ROW + CONTENT_ROW_GAP;
            }

            protected void CalculateBoundsHeader( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, MAIN_CONTENT_WIDTH, HEADER_ROW );

                innerY -= HEADER_ROW;// + CONTENT_ROW_GAP;
            }
            #endregion

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                bool hasFilterChanged = !(Structures_FilterText == Structures_LastFilterText && Structures_lastTurn == SimCommon.Turn);
                Structures_LastFilterText = Structures_FilterText;
                Structures_lastTurn = SimCommon.Turn;

                #region Complaints Header
                {
                    this.CalculateBoundsHeader( out Rect bounds, ref runningY );

                    bool shouldRender = true;
                    if ( bounds.yMax < minYToShow )
                        shouldRender = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        shouldRender = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bTextHeader row = shouldRender ? bTextHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds() : null;
                    if ( row == null )
                        shouldRender = false;
                    if ( shouldRender )
                    {
                        bTextHeaderPool.ApplySingleItemInRow( row, bounds, false );

                        row.Assign( "Header", "ComplaintsHeader", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartSize80().AddLangAndAfterLineItemHeader( "StructuresWithProblems" )
                                            .AddRaw( SimCommon.StructuresWithAnyFormOfIssue.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }
                #endregion

                //structures with issues are first
                foreach ( MachineStructure structure in SimCommon.StructuresWithAnyFormOfIssue.GetDisplayList() )
                {
                    if ( hasFilterChanged )
                    {
                        if ( Structures_FilterText.IsEmpty() )
                            structure.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            structure.DuringGame_HasBeenFilteredOutInInventory = !structure.GetMatchesSearchString( Structures_FilterText );
                    }
                    if ( structure.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleProblemStructure( structure, bounds ) )
                        continue; //time-slicing
                }

                #region No Complaints Header
                {
                    this.CalculateBoundsHeader( out Rect bounds, ref runningY );

                    bool shouldRender = true;
                    if ( bounds.yMax < minYToShow )
                        shouldRender = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        shouldRender = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bTextHeader row = shouldRender ? bTextHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds() : null;
                    if ( row == null )
                        shouldRender = false;
                    if ( shouldRender )
                    {
                        bTextHeaderPool.ApplySingleItemInRow( row, bounds, false );

                        row.Assign( "Header", "ComplaintsHeader", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartSize80().AddLangAndAfterLineItemHeader( "StructuresWorkingFine" )
                                            .AddRaw( SimCommon.StructuresWithNoIssues.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }
                #endregion

                //then those with no issues
                foreach ( MachineStructure structure in SimCommon.StructuresWithNoIssues.GetDisplayList() )
                {
                    if ( hasFilterChanged )
                    {
                        if ( Structures_FilterText.IsEmpty() )
                            structure.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            structure.DuringGame_HasBeenFilteredOutInInventory = !structure.GetMatchesSearchString( Structures_FilterText );
                    }
                    if ( structure.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleHealthyStructure( structure, bounds ) )
                        continue; //time-slicing
                }
            }
            #endregion

            #region HandleProblemStructure
            private bool HandleProblemStructure( MachineStructure structure, Rect bounds )
            {
                bProblemStructure row = bProblemStructurePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bProblemStructurePool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "StructureReport", structure.PermanentIndexNonSimAsString, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.UpdateImageContentFromVolatile:
                            {
                                ExtraData.Image.SetSpriteIfNeeded_Simple( structure?.GetShapeIcon()?.GetSpriteForUI() );

                                bool isHovered = element.LastHadMouseWithin;

                                ArcenDoubleCharacterBuffer buffer;

                                //main line
                                buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                                buffer.AddRaw( structure.GetDisplayName(), ColorTheme.GetBasicLightTextBlue( isHovered ) );
                                ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();

                                //details line
                                buffer = ExtraData.SubTextsWrapper[1].Text.StartWritingToBuffer();
                                structure.WriteComplaints( buffer, false, false, true, 0 );
                                ExtraData.SubTextsWrapper[1].Text.FinishWritingToBuffer();

                                int healthPercentage = 100;
                                MapActorData health = structure.GetActorDataData( ActorRefs.ActorHP, true );
                                if ( health != null && health.LostFromMax > 0 )
                                {
                                    healthPercentage = Mathf.FloorToInt( ((float)health.Current / (float)health.Maximum) * 100f );
                                    if ( healthPercentage == 0 && health.Current > 0 )
                                        healthPercentage = 1;
                                }

                                row.SetRelatedImage0FillPercentageIfNeeded( healthPercentage );
                                ISeverity severityColor = ScaleRefs.UnitListHealth.GetSeverityFromScale( healthPercentage );
                                if ( severityColor?.VisColor != null )
                                    row.SetRelatedImage0ColorFromHexIfNeeded( severityColor.VisColor.ColorHex );


                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                structure.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                            }
                            break;
                        case UIAction.OnClick:
                            if ( ExtraData.MouseInput.LeftButtonClicked )
                            {
                                Engine_HotM.SelectedMachineActionMode = null;
                                if ( Engine_HotM.SelectedActor == structure ) //focus on the actor
                                    VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( structure.GetDrawLocation(), false );
                                else
                                {
                                    Engine_HotM.SetSelectedActor( structure, false, false, false );
                                }
                            }
                            else if ( ExtraData.MouseInput.RightButtonClicked )
                                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( structure.GetDrawLocation(), false );
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region HandleHealthyStructure
            private bool HandleHealthyStructure( MachineStructure structure, Rect bounds )
            {
                bHealthyStructure row = bHealthyStructurePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bHealthyStructurePool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "StructureReport", structure.PermanentIndexNonSimAsString, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.UpdateImageContentFromVolatile:
                            {
                                ExtraData.Image.SetSpriteIfNeeded_Simple( structure?.GetShapeIcon()?.GetSpriteForUI() );

                                bool isHovered = element.LastHadMouseWithin;

                                ArcenDoubleCharacterBuffer buffer;
                                
                                //sole line
                                buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                                buffer.AddRaw( structure.GetDisplayName(), ColorTheme.GetBasicLightTextBlue( isHovered ) );
                                ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                structure.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                            }
                            break;
                        case UIAction.OnClick:
                            if ( ExtraData.MouseInput.LeftButtonClicked )
                            {
                                Engine_HotM.SelectedMachineActionMode = null;
                                if ( Engine_HotM.SelectedActor == structure ) //focus on the actor
                                    VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( structure.GetDrawLocation(), false );
                                else
                                {
                                    Engine_HotM.SetSelectedActor( structure, false, false, false );
                                }
                            }
                            else if ( ExtraData.MouseInput.RightButtonClicked )
                                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( structure.GetDrawLocation(), false );
                            break;
                    }
                } );
                return true;
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
                Structures_FilterText = newString;
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }

            public override void OnEndEdit()
            {
                Structures_FilterText = this.GetText();
            }

            private ArcenUI_Input inputField = null;

            public override void OnMainThreadUpdate()
            {
                if ( !hasSetFilterTextSinceStart )
                {
                    hasSetFilterTextSinceStart = true;
                    this.SetText( Structures_FilterText );
                }

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
                    break;
                case "ScrapSelected":
                    VisCommands.ScrapSelected();
                    break;
                default:
                    VisCommands.HandleMajorWindowKeyPress( InputActionType );
                    break;
            }
        }

        #region bHealthyStructure
        public class bHealthyStructure : ImageButtonAbstractBase
        {
            public static bHealthyStructure Original;
            public bHealthyStructure() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
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

        #region bProblemStructure
        public class bProblemStructure : ImageButtonAbstractBase
        {
            public static bProblemStructure Original;
            public bProblemStructure() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
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

        #region bTextHeader
        public class bTextHeader : ButtonAbstractBase
        {
            public static bTextHeader Original;
            public bTextHeader() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            private string lastID1 = string.Empty;
            private string lastID2 = string.Empty;

            public override void OnUpdateSub() { }

            public void Assign( string ID1, string ID2, GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;

                if ( this.lastID1 != ID1 || this.lastID2 != ID2 )
                {
                    this.lastID1 = ID1;
                    this.lastID2 = ID2;
                    (this.Element as ArcenUI_Button)?.TriggerTextUpdateImmediately( false );
                    //ArcenDebugging.LogSingleLine( "Item is fresh: " + ((this.Element as ArcenUI_Button)?.ElementName ?? "null"), Verbosity.DoNotShow );
                }
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

        #region dFilter
        public class dFilter : DropdownAbstractBase
        {
            public static dFilter Instance;
            public dFilter()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                
            }

            public override void HandleMouseover()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                if ( elementAsType != null && elementAsType.CurrentlySelectedOption is ArcenDynamicTableRow Row )
                {
                    string description = Row.GetDescription();
                    if ( description.IsEmpty() )
                        return;

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Row ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        Row.WriteOptionDisplayTextForDropdown( novel.TitleUpperLeft );
                        novel.Main.AddRaw( description );
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                if ( Item.GetItem() is ArcenDynamicTableRow Row )
                {
                    string description = Row.GetDescription();
                    if ( description.IsEmpty() )
                        return;

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Row ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        Row.WriteOptionDisplayTextForDropdown( novel.TitleUpperLeft );
                        novel.Main.AddRaw( description );
                    }
                }
            }

            public override bool GetShouldBeHidden()
            {
                return true;
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

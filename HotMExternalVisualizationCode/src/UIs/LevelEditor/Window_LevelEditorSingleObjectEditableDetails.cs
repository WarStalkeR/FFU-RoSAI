using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorSingleObjectEditableDetails : WindowControllerAbstractBase
    {
        public static Window_LevelEditorSingleObjectEditableDetails Instance;
        public Window_LevelEditorSingleObjectEditableDetails()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
        }

        #region GetShouldDrawThisFrame_Subclass
        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_Universal.GameLoop == null )
                return false;
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false;//only render in the level editor

            FillSelectedObjectsListIfNotRecentlyDone();

            if ( !SelectedA5PlaceableInUse )
                return false; //only include this if we have exactly one object selected;

            if ( workingSelectedListDoNotReference.Count == 1 )
            {
                if ( SelectedA5PlaceableInUse.AdvancedIsMetaRegion ) //advanced meta regions always show this window when they are selected singly
                    return true;
            }

            if ( SelectedA5PlaceableInUse.AdvancedIsPathingRegion ) //anything with pathing regions shows this window
                return true;

            return false; //right now nothing else does
        }
        #endregion

        private const float FULL_WIDTH_IGNORESCROLLBAR = 240f;
        private const float FULL_WIDTH_AVOIDSCROLLBAR = 220f;
        private const float LABEL_OBJ_WIDTH = 60f;
        private const float LABEL_FIELD_WIDTH = 20f;
        private const float FLOAT_WIDTH = 45f;
        private const float PAD_BETWEEN_FLOATS = 2f;

        private const float QUARTER_WIDTH_AVOIDCROLLBAR = (FULL_WIDTH_AVOIDSCROLLBAR - 20 ) / 4f; //the 20 is for buffer

        private const float ROW_HEIGHT = 24;
        private const float ROW_BUFFER = 1.5f;

        #region FillSelectedObjectsListIfNotRecentlyDone
        private static float lastTimeSelectedObjectListFilled = 0;
        private static void FillSelectedObjectsListIfNotRecentlyDone()
        {
            if ( ArcenTime.AnyTimeSinceStartF - lastTimeSelectedObjectListFilled < 0.1f )
                return;
            lastTimeSelectedObjectListFilled = ArcenTime.AnyTimeSinceStartF;

            ( Engine_Universal.GameLoop as LevelEditorCoreGameLoop)?.LevelEditorHook?.FillListOfSelectedObjects( workingSelectedListDoNotReference );

            if ( workingSelectedListDoNotReference.Count > 0 )
                SelectedA5PlaceableInUse = workingSelectedListDoNotReference[0];
            else
                SelectedA5PlaceableInUse = null;
        }
        #endregion

        private static readonly List<A5Placeable> workingSelectedListDoNotReference = List<A5Placeable>.Create_WillNeverBeGCed( 100, "Window_LevelEditorSingleObjectEditableDetails-workingSelectedListDoNotReference", 100 );
        public static A5Placeable SelectedA5PlaceableInUse = null;

        private ArcenCachedExternalTypeDirect type_bButtonGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bButtonGeneric ) );
        private ArcenCachedExternalTypeDirect type_tLabelGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric ) );
        private ArcenCachedExternalTypeDirect type_iFloatText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iFloatText ) );
        private ArcenCachedExternalTypeDirect type_iReadonlyText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iReadonlyText ) );
        private ArcenCachedExternalTypeDirect type_dPathingType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dPathingType ) );

        private static int lastYHeightOfInterior = 0;
        private static int lastNumberOfCompleteClears = 0;


        public override void Close( WindowCloseReason Reason )
        {

        }
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

            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;

            int objID = SelectedA5PlaceableInUse.ObjectRootID;

            float runningY = 3;
            Rect bounds;
            int outerIndex;

            if ( SelectedA5PlaceableInUse != null )
            {
                #region Advanced Meta Zones
                outerIndex = -30;

                if ( SelectedA5PlaceableInUse.AdvancedIsMetaRegion )
                {
                    //advanced meta zone size (name is already in the header)
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, string.Empty, outerIndex, 0, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    Bounds worldSpaceBounds = SelectedA5PlaceableInUse.GetComponent<Renderer>().bounds;
                                    Vector3 sz = worldSpaceBounds.size;

                                    ExtraData.Buffer.AddNeverTranslated( "Width: ", true );
                                    ExtraData.Buffer.AddNeverTranslated( sz.x.ToString( "0.0##" ), true );

                                    ExtraData.Buffer.Position80().AddNeverTranslated( "Depth: ", true );
                                    ExtraData.Buffer.AddNeverTranslated( sz.z.ToString( "0.0##" ), true );

                                    ExtraData.Buffer.Position160().AddNeverTranslated( "Height: ", true );
                                    ExtraData.Buffer.AddNeverTranslated( sz.y.ToString( "0.0##" ), true );
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
                #endregion

                if ( SelectedA5PlaceableInUse.GetHasNSEWIndicators() )
                {
                    #region NSEW Indicators
                    outerIndex = 100;

                    {
                        //id path to the placeable, which is helpful for many reasons
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, QUARTER_WIDTH_AVOIDCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "N", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        if ( SelectedA5PlaceableInUse.DirEnabledN )
                                            ExtraData.Buffer.StartColor( ColorTheme.HeaderGold );
                                        else
                                            ExtraData.Buffer.StartColor( ColorTheme.RustLighter );
                                        ExtraData.Buffer.AddNeverTranslated( "N", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "Should items be seeded along this side of this region?", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        SelectedA5PlaceableInUse.DirEnabledN = !SelectedA5PlaceableInUse.DirEnabledN;
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + 5, runningY, QUARTER_WIDTH_AVOIDCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "S", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        if ( SelectedA5PlaceableInUse.DirEnabledS )
                                            ExtraData.Buffer.StartColor( ColorTheme.HeaderGold );
                                        else
                                            ExtraData.Buffer.StartColor( ColorTheme.RustLighter );
                                        ExtraData.Buffer.AddNeverTranslated( "S", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "Should items be seeded along this side of this region?", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        SelectedA5PlaceableInUse.DirEnabledS = !SelectedA5PlaceableInUse.DirEnabledS;
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + 5, runningY, QUARTER_WIDTH_AVOIDCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "E", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        if ( SelectedA5PlaceableInUse.DirEnabledE )
                                            ExtraData.Buffer.StartColor( ColorTheme.HeaderGold );
                                        else
                                            ExtraData.Buffer.StartColor( ColorTheme.RustLighter );
                                        ExtraData.Buffer.AddNeverTranslated( "E", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "Should items be seeded along this side of this region?", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        SelectedA5PlaceableInUse.DirEnabledE = !SelectedA5PlaceableInUse.DirEnabledE;
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );


                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + 5, runningY, QUARTER_WIDTH_AVOIDCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "W", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        if ( SelectedA5PlaceableInUse.DirEnabledW )
                                            ExtraData.Buffer.StartColor( ColorTheme.HeaderGold );
                                        else
                                            ExtraData.Buffer.StartColor( ColorTheme.RustLighter );
                                        ExtraData.Buffer.AddNeverTranslated( "W", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "Should items be seeded along this side of this region?", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), SideClamp.AboveOrBelow );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        SelectedA5PlaceableInUse.DirEnabledW = !SelectedA5PlaceableInUse.DirEnabledW;
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_BUFFER;
                    }
                    #endregion
                }

                if ( SelectedA5PlaceableInUse.ObjRoot.IsPathingRegion )
                {
                    #region dPathingType
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                    UIFlow.AddDropdown( "DropdownBlue", Set, type_dPathingType, string.Empty,
                        SelectedA5PlaceableInUse.GetLevelEditorObjectID().SpecificObjectID, SelectedA5PlaceableInUse.GetLevelEditorObjectID().ObjectRootID, -1, bounds, null );
                    runningY += ROW_HEIGHT + ROW_BUFFER;
                    #endregion dPathingType
                }
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );

            lastYHeightOfInterior = Mathf.CeilToInt( runningY );
        }

        public class tLabelGeneric : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {                
            }
        }

        public class bButtonGeneric : ButtonAbstractBase
        {

        }

        #region dPathingType
        public class dPathingType : DropdownAbstractBase
        {
            public static dPathingType Instance;
            public dPathingType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
                PathingRegionType newType = (PathingRegionType)Item.GetItem();
                PathingRegionType priorType = SelectedA5PlaceableInUse?.PathingType;
                if ( newType == priorType )
                    return;

                ThrowawayList<KeyValuePair<A5Placeable, PathingRegionType>> itemsToApplyTo = new ThrowawayList<KeyValuePair<A5Placeable, PathingRegionType>>();
                itemsToApplyTo.Add( new KeyValuePair<A5Placeable, PathingRegionType>( SelectedA5PlaceableInUse, SelectedA5PlaceableInUse.PathingType ) );

                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                {
                    switch ( Stage )
                    {
                        case GeneralUndoActionStage.Execute:
                        case GeneralUndoActionStage.Redo:
                            foreach ( KeyValuePair<A5Placeable, PathingRegionType> kv in itemsToApplyTo )
                                kv.Key.PathingType = newType;
                            break;
                        case GeneralUndoActionStage.Undo:
                            foreach ( KeyValuePair<A5Placeable, PathingRegionType> kv in itemsToApplyTo )
                                kv.Key.PathingType = kv.Value;
                            break;
                    }
                } );
            }

            public override void OnUpdate()
            {
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;
                    debugStage = 200;

                    PathingRegionType saveCodeToSelect = null;
                    if ( SelectedA5PlaceableInUse?.PathingType != null )
                        saveCodeToSelect = SelectedA5PlaceableInUse?.PathingType;

                    debugStage = 5000;
                    bool foundMismatch = false;
                    if ( saveCodeToSelect != null && (elementAsType.CurrentlySelectedOption == null || ((PathingRegionType)elementAsType.CurrentlySelectedOption.GetItem()) != saveCodeToSelect) )
                    {
                        foundMismatch = true;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                    }
                    else
                    {
                        debugStage = 6000;
                        PathingRegionType[] types = PathingRegionTypeTable.Instance.Rows;
                        debugStage = 6010;
                        for ( int i = 0; i < types.Length; i++ )
                        {
                            PathingRegionType type = types[i];
                            debugStage = 6400;
                            IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                            debugStage = 6500;
                            if ( option == null )
                            {
                                foundMismatch = true;
                                break;
                            }
                            debugStage = 6600;
                            PathingRegionType optionItemAsType = (PathingRegionType)option.GetItem();
                            if ( type == optionItemAsType )
                                continue;
                            foundMismatch = true;
                            break;
                        }
                    }

                    debugStage = 7000;
                    if ( foundMismatch )
                    {
                        debugStage = 7100;
                        elementAsType.ClearItems();

                        debugStage = 7110;
                        PathingRegionType[] types = PathingRegionTypeTable.Instance.Rows;
                        debugStage = 7120;
                        for ( int i = 0; i < types.Length; i++ )
                        {
                            PathingRegionType type = types[i];
                            elementAsType.AddItem( type, type == saveCodeToSelect );
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "dPathingType", debugStage, e, Verbosity.ShowAsError );
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "Choose which pathing type will be used in the selected pathing regions.";
                PathingRegionType typeDataToSelect = SelectedA5PlaceableInUse?.PathingType;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\nCurrently: <color=#7ab9ff>" + typeDataToSelect.ID + "</color>";
                }
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, LocalizedString.AddNeverTranslated_New( mouseoverText ), TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                PathingRegionType ItemAsType = (PathingRegionType)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LevelEditorItem", "AnyIsFine" ), null, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.TitleUpperLeft.AddRaw( ItemAsType.ID );
                    novel.Main.AddNeverTranslated( "Choose which pathing type will be used in the selected pathing regions.", true );
                }
            }
        }
        #endregion

        /// <summary>
        /// Top header, which shows the name of the object being edited
        /// </summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( SelectedA5PlaceableInUse.name );
            }
            public override void OnUpdate() { }
        }

        public class customParent : CustomUIAbstractBase
        {
            public static customParent CustomParentInstance;
            public customParent()
            {
                CustomParentInstance = this;
            }

            public int HeightToShow
            {
                get { return this.heightToShow; }
            }

            private int heightToShow = 0;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LevelEditorSingleObjectEditableDetails.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                    }
                    #endregion
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 24 + MathA.Min( lastYHeightOfInterior, 400 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    //note: appear from the bottom up
                    this.Element.RelevantRect.anchorMin = new Vector2( 0, 0 );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0, 0 );
                    this.Element.RelevantRect.pivot = new Vector2( 0, 0 );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion
            }
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

        public class iFloatText : InputAbstractBase
        {
            public override char ValidateInput( char addedChar )
            {
                if ( char.IsNumber( addedChar ) )
                    return addedChar;
                switch ( addedChar )
                {
                    case '.':
                    case '-':
                        return addedChar;
                }

                return '\0'; //accept only numeric inputs
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                        return InputActionTextboxResult.UnfocusMe;
                    case "Return": //enter key
                    case "Tab": //tab key
                        return InputActionTextboxResult.TryGoToNextInputField;
                    case "ShiftTab": //shift+tab key
                        return InputActionTextboxResult.TryGoToPriorInputField;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }

        public class iReadonlyText : InputAbstractBase
        {
            public override char ValidateInput( char addedChar )
            {
                return '\0'; //accept nothing
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                    case "Tab": //tab key
                        return InputActionTextboxResult.TryGoToNextInputField;
                    case "ShiftTab": //shift+tab key
                        return InputActionTextboxResult.TryGoToPriorInputField;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }
        }
    }
}

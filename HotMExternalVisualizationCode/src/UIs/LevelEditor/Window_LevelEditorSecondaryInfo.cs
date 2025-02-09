using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorSecondaryInfo : WindowControllerAbstractBase
    {
        public static Window_LevelEditorSecondaryInfo Instance;
        public Window_LevelEditorSecondaryInfo()
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

            if ( !GameSettings.Current.GetBool( "LevelEditorDebugPopupWindowForEditingSingleObject" ) )
                return false; //if we don't have the debug setting on for this, then don't even try

            return true;
        }
        #endregion

        private const float FULL_WIDTH_IGNORESCROLLBAR = 240f;
        private const float FULL_WIDTH_AVOIDSCROLLBAR = 220f;
        private const float LABEL_OBJ_WIDTH = 60f;
        private const float LABEL_FIELD_WIDTH = 20f;
        private const float FLOAT_WIDTH = 45f;
        private const float PAD_BETWEEN_FLOATS = 2f;

        private const float ROW_HEIGHT = 24;
        private const float ROW_BUFFER = 1.5f;

        #region FillSelectedObjectsListIfNotRecentlyDone
        private static float lastTimeSelectedObjectListFilled = 0;
        private static void FillSelectedObjectsListIfNotRecentlyDone()
        {
            if ( ArcenTime.AnyTimeSinceStartF - lastTimeSelectedObjectListFilled < 0.1f )
                return;
            lastTimeSelectedObjectListFilled = ArcenTime.AnyTimeSinceStartF;

            if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop)?.LevelEditorHook?.GetCountOfSelectedObjects( true ) != 1 ) //we use the expensive check because it won't pull the correct count otherwise
            {
                workingSelectedListDoNotReference.Clear();
                SelectedA5PlaceableInUse = null;
                return;
            }

            ( Engine_Universal.GameLoop as LevelEditorCoreGameLoop)?.LevelEditorHook?.FillListOfSelectedObjects( workingSelectedListDoNotReference );

            if ( workingSelectedListDoNotReference.Count > 0 )
                SelectedA5PlaceableInUse = workingSelectedListDoNotReference[0];
            else
                SelectedA5PlaceableInUse = null;
        }
        #endregion

        private static readonly List<A5Placeable> workingSelectedListDoNotReference = List<A5Placeable>.Create_WillNeverBeGCed( 100, "Window_LevelEditorSecondaryInfo-workingSelectedListDoNotReference", 100 );
        public static A5Placeable SelectedA5PlaceableInUse = null;

        private ArcenCachedExternalTypeDirect type_bButtonGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bButtonGeneric ) );
        private ArcenCachedExternalTypeDirect type_tLabelGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric ) );
        private ArcenCachedExternalTypeDirect type_iFloatText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iFloatText ) );
        private ArcenCachedExternalTypeDirect type_iReadonlyText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iReadonlyText ) );

        private static int lastYHeightOfInterior = 0;
        private static int lastNumberOfCompleteClears = 0;

        private static readonly Dictionary<string, ArcenDynamicTableBase> workingChangedRows = Dictionary<string, ArcenDynamicTableBase>.Create_WillNeverBeGCed( 100, "Window_LevelEditorSecondaryInfo-workingChangedRows" );


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

            workingChangedRows.Clear();

            float runningY = 3;
            Rect bounds;
            int outerIndex;

            if ( SelectedA5PlaceableInUse != null )
            {
                #region Path
                outerIndex = -35;

                {
                    //id path to the placeable, which is helpful for many reasons
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_IGNORESCROLLBAR, ROW_HEIGHT );
                    UIFlow.AddInputTextbox( "BasicTextboxBlue", Set, type_iReadonlyText, "Path", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        //SelectedA5PlaceableInUse.ObjRoot.IDPath, readonly
                        ArcenUI_Input input = element as ArcenUI_Input;
                        switch ( Action )
                        {
                            case UIAction.WriteToUIFromData:
                                input.SetTextIfDifferentFromLastTextAndNotFocused( SelectedA5PlaceableInUse.ObjRoot.IDPath );
                                break;
                            case UIAction.OnFocus:
                                //copy the text to the clipboard
                                GUIUtility.systemCopyBuffer = input.GetText();
                                input.ReferenceInputField.SelectAll();
                                input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                break;
                        }
                    } );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
                #endregion

                #region Decoration Zones
                outerIndex = 0;

                for ( int i = 0; i < SelectedA5PlaceableInUse.ObjRoot.DecorationZones.Count; i++ )
                {
                    DecorationZonePrefab zone = SelectedA5PlaceableInUse.ObjRoot.DecorationZones[i];

                    //check for changes!
                    if ( zone.OriginalZoneOffset != zone.ZoneOffset || zone.OriginalZoneWidthHeight != zone.ZoneWidthHeight )
                        workingChangedRows[zone.OriginalXmlData.CachedDoc.FullSourceFileName] = DecorationZonePrefabTable.Instance;

                    {
                        //row 1
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, LABEL_OBJ_WIDTH, ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, string.Empty, outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //object name
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartSize75().AddNeverTranslated( "Deco Zone", true );
                                        if ( SelectedA5PlaceableInUse.ObjRoot.DecorationZones.Count > 1 )
                                            ExtraData.Buffer.Space1x().AddRaw( (i + 1).ToString() );
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        float savedXMax = bounds.xMax;

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, LABEL_FIELD_WIDTH, ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "A", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //field name
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                        ExtraData.Buffer.StartSize75().AddNeverTranslated( "Pos", true );
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                        UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "AX", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //ZoneOffset.x
                            ArcenUI_Input input = element as ArcenUI_Input;
                            switch ( Action )
                            {
                                case UIAction.WriteToUIFromData:
                                    input.SetTextIfDifferentFromLastTextAndNotFocused( zone.ZoneOffset.x.ToString() );
                                    break;
                                case UIAction.SendToDataAfterUIEdits:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                    {
                                        float atStart = zone.ZoneOffset.x;
                                        if ( atStart != result )
                                            levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceX( result );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceX( atStart );
                                                            break;
                                                    }
                                                } );
                                    }
                                    break;
                                case UIAction.MouseDrag:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                    {
                                        float change = ExtraData.DragDiff.x / 100;
                                        float newData = orig += change;
                                        input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                        zone.ZoneOffset = zone.ZoneOffset.ReplaceX( newData );
                                    }
                                    break;
                                case UIAction.MouseDragComplete:
                                    {
                                        if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                        {
                                            if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                            {
                                                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceX( newFinal );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceX( atStart );
                                                            break;
                                                    }
                                                } );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnFocus:
                                    input.ReferenceInputField.SelectAll();
                                    input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                    break;
                            }
                        } );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + PAD_BETWEEN_FLOATS, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                        UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "AY", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //ZoneOffset.y
                            ArcenUI_Input input = element as ArcenUI_Input;
                            switch ( Action )
                            {
                                case UIAction.WriteToUIFromData:
                                    input.SetTextIfDifferentFromLastTextAndNotFocused( zone.ZoneOffset.y.ToString() );
                                    break;
                                case UIAction.SendToDataAfterUIEdits:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                    {
                                        float atStart = zone.ZoneOffset.y;
                                        if ( atStart != result )
                                            levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceY( result );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceY( atStart );
                                                            break;
                                                    }
                                                } );
                                    }
                                    break;
                                case UIAction.MouseDrag:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                    {
                                        float change = ExtraData.DragDiff.x / 100;
                                        float newData = orig += change;
                                        input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                        zone.ZoneOffset = zone.ZoneOffset.ReplaceY( newData );
                                    }
                                    break;
                                case UIAction.MouseDragComplete:
                                    {
                                        if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                        {
                                            if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                            {
                                                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceY( newFinal );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceY( atStart );
                                                            break;
                                                    }
                                                } );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnFocus:
                                    input.ReferenceInputField.SelectAll();
                                    input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                    break;
                            }
                        } );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + PAD_BETWEEN_FLOATS, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                        UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "AZ", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //ZoneOffset.z
                            ArcenUI_Input input = element as ArcenUI_Input;
                            switch ( Action )
                            {
                                case UIAction.WriteToUIFromData:
                                    input.SetTextIfDifferentFromLastTextAndNotFocused( zone.ZoneOffset.z.ToString() );
                                    break;
                                case UIAction.SendToDataAfterUIEdits:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                    {
                                        float atStart = zone.ZoneOffset.z;
                                        if ( atStart != result )
                                            levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceZ( result );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceZ( atStart );
                                                            break;
                                                    }
                                                } );
                                    }
                                    break;
                                case UIAction.MouseDrag:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                    {
                                        float change = ExtraData.DragDiff.x / 100;
                                        float newData = orig += change;
                                        input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                        zone.ZoneOffset = zone.ZoneOffset.ReplaceZ( newData );
                                    }
                                    break;
                                case UIAction.MouseDragComplete:
                                    {
                                        if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                        {
                                            if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                            {
                                                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceZ( newFinal );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneOffset = zone.ZoneOffset.ReplaceZ( atStart );
                                                            break;
                                                    }
                                                } );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnFocus:
                                    input.ReferenceInputField.SelectAll();
                                    input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                    break;
                            }
                        } );

                        runningY += ROW_HEIGHT + ROW_BUFFER;

                        //row 2
                        bounds.xMax = savedXMax;

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, LABEL_FIELD_WIDTH, ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "B", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //field name
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.StartSize75().AddNeverTranslated( "Siz", true );
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                        UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "BX", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //ZoneWidthHeight.x
                            ArcenUI_Input input = element as ArcenUI_Input;
                            switch ( Action )
                            {
                                case UIAction.WriteToUIFromData:
                                    input.SetTextIfDifferentFromLastTextAndNotFocused( zone.ZoneWidthHeight.x.ToString() );
                                    break;
                                case UIAction.SendToDataAfterUIEdits:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                    {
                                        float atStart = zone.ZoneWidthHeight.x;
                                        if ( atStart != result )
                                            levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceX( result );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceX( atStart );
                                                            break;
                                                    }
                                                } );
                                    }
                                    break;
                                case UIAction.MouseDrag:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                    {
                                        float change = ExtraData.DragDiff.x / 100;
                                        float newData = orig += change;
                                        input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                        zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceX( newData );
                                    }
                                    break;
                                case UIAction.MouseDragComplete:
                                    {
                                        if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                        {
                                            if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                            {
                                                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceX( newFinal );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceX( atStart );
                                                            break;
                                                    }
                                                } );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnFocus:
                                    input.ReferenceInputField.SelectAll();
                                    input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                    break;
                            }
                        } );

                        bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + PAD_BETWEEN_FLOATS, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                        UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "BY", outerIndex, i, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            //ZoneWidthHeight.y
                            ArcenUI_Input input = element as ArcenUI_Input;
                            switch ( Action )
                            {
                                case UIAction.WriteToUIFromData:
                                    input.SetTextIfDifferentFromLastTextAndNotFocused( zone.ZoneWidthHeight.y.ToString() );
                                    break;
                                case UIAction.SendToDataAfterUIEdits:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                    {
                                        float atStart = zone.ZoneWidthHeight.y;
                                        if ( atStart != result )
                                            levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceY( result );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceY( atStart );
                                                            break;
                                                    }
                                                } );
                                    }
                                    break;
                                case UIAction.MouseDrag:
                                    if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                    {
                                        float change = ExtraData.DragDiff.x / 100;
                                        float newData = orig += change;
                                        input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                        zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceY( newData );
                                    }
                                    break;
                                case UIAction.MouseDragComplete:
                                    {
                                        if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                        {
                                            if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                            {
                                                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                {
                                                    switch ( Stage )
                                                    {
                                                        case GeneralUndoActionStage.Execute:
                                                        case GeneralUndoActionStage.Redo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceY( newFinal );
                                                            break;
                                                        case GeneralUndoActionStage.Undo:
                                                            zone.ZoneWidthHeight = zone.ZoneWidthHeight.ReplaceY( atStart );
                                                            break;
                                                    }
                                                } );
                                            }
                                        }
                                    }
                                    break;
                                case UIAction.OnFocus:
                                    input.ReferenceInputField.SelectAll();
                                    input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                    break;
                            }
                        } );

                        //row 2 is done, so increment y
                        runningY += ROW_HEIGHT + ROW_BUFFER;
                    }
                }
                #endregion

                #region Road Lanes
                outerIndex = 1; //road lane points

                int globalPointIndex = 0;
                for ( int roadIndexOuter = 0; roadIndexOuter < SelectedA5PlaceableInUse.ObjRoot.Roads.Count; roadIndexOuter++ )
                {
                    RoadPrefab roadOuter = SelectedA5PlaceableInUse.ObjRoot.Roads[roadIndexOuter];

                    int roadIndexToDraw = 0;
                    if ( SelectedA5PlaceableInUse.ObjRoot.Roads.Count > 1 )
                        roadIndexToDraw = roadIndexOuter + 1;
                    else
                        roadIndexToDraw = -1;

                    for ( int laneIndexTemp = 0; laneIndexTemp < roadOuter.Lanes.Count; laneIndexTemp++ )
                    {
                        LanePrefab laneOuter = roadOuter.Lanes[laneIndexTemp];

                        int laneHash = objID.GetHashCode();
                        laneHash = HashCodeHelper.CombineHashCodes( laneHash, roadOuter.RowUniqueHashIndex.GetHashCode() );
                        laneHash = HashCodeHelper.CombineHashCodes( laneHash, laneOuter.ID.GetHashCode() );

                        for ( int ptIndexTemp = 0; ptIndexTemp < laneOuter.LanePoints.Count; ptIndexTemp++ )
                        {
                            globalPointIndex++; //this exists so that we have unique IDs for each

                            int ptIndex = ptIndexTemp; //we have to make a copy of this so that delegates can reference it
                            int roadIndex = roadIndexOuter;
                            int laneIndex = laneIndexTemp;

                            //check for changes!
                            if ( laneOuter.LanePoints.Count != laneOuter.OriginalPointsOnLoad.Count )
                                workingChangedRows[roadOuter.OriginalXmlData.CachedDoc.FullSourceFileName] = RoadPrefabTable.Instance;
                            else if ( laneOuter.LanePoints[ptIndex] != laneOuter.OriginalPointsOnLoad[ptIndex] )
                                workingChangedRows[roadOuter.OriginalXmlData.CachedDoc.FullSourceFileName] = RoadPrefabTable.Instance;

                            {
                                //sole row
                                bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, LABEL_OBJ_WIDTH, ROW_HEIGHT );
                                UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "LaneA", outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                    if ( road == null ) return;
                                    LanePrefab lane = road.Lanes[laneIndex];
                                    if ( lane == null ) return;

                                    //object name
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            {
                                                ExtraData.Buffer.StartSize75().AddNeverTranslated( "", true );
                                                if ( roadIndexToDraw > 0 )
                                                    ExtraData.Buffer.AddNeverTranslated( "R", true ).AddRaw( roadIndexToDraw.ToString() ).Space1x();
                                                ExtraData.Buffer.AddNeverTranslated( lane.ID, true );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );

                                                ExtraData.Buffer.AddNeverTranslated( "Road Prefab: ", true, "a6a3a0" );
                                                ExtraData.Buffer.AddRaw( road.ID ).Line();
                                                if ( roadIndexToDraw >= 0 )
                                                    ExtraData.Buffer.AddNeverTranslated( "Road Index: ", true, "a6a3a0" ).AddRaw( roadIndexToDraw.ToString() ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "-------------------------------------------------", true, "595959" ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Lane ID: ", true, "a6a3a0" ).AddRaw( lane.ID ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Point Index For Lane: ", true, "a6a3a0" ).AddRaw( ptIndex.ToString() ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Right-click here to duplicate point, middle-click to delete point.", true );
                                                Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                            }
                                            break;
                                        case UIAction.OnClick:
                                            {
                                                if ( ExtraData.MouseInput.RightButtonClicked )
                                                {
                                                    Vector3 atStart = lane.LanePoints[ptIndex];
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                    {
                                                        switch ( Stage )
                                                        {
                                                            case GeneralUndoActionStage.Execute:
                                                            case GeneralUndoActionStage.Redo:
                                                                lane.LanePoints.Insert( ptIndex, atStart );
                                                                Set.RefreshAllElements = true; //refresh all the actual controls in the directives set
                                                                break;
                                                            case GeneralUndoActionStage.Undo:
                                                                lane.LanePoints.RemoveAt( ptIndex );
                                                                Set.RefreshAllElements = true;
                                                                Set.RefreshAllElements = true; //refresh all the actual controls in the directives set
                                                                break;
                                                        }
                                                    } );
                                                }
                                                else if ( ExtraData.MouseInput.MiddleButtonClicked )
                                                {
                                                    Vector3 atStart = lane.LanePoints[ptIndex];
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                    {
                                                        switch ( Stage )
                                                        {
                                                            case GeneralUndoActionStage.Execute:
                                                            case GeneralUndoActionStage.Redo:
                                                                lane.LanePoints.RemoveAt( ptIndex );
                                                                Set.RefreshAllElements = true; //refresh all the actual controls in the directives set
                                                                break;
                                                            case GeneralUndoActionStage.Undo:
                                                                lane.LanePoints.Insert( ptIndex, atStart );
                                                                Set.RefreshAllElements = true; //refresh all the actual controls in the directives set
                                                                break;
                                                        }
                                                    } );
                                                }
                                            }
                                            break;
                                    }
                                }, 12f, TextWrapStyle.Unset_DoDefault );

                                bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, LABEL_FIELD_WIDTH, ROW_HEIGHT );
                                UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "LaneB", outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                    if ( road == null ) return;
                                    LanePrefab lane = road.Lanes[laneIndex];
                                    if ( lane == null ) return;

                                    //field name
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            {
                                                ExtraData.Buffer.StartSize75().AddNeverTranslated( "", true );
                                                ExtraData.Buffer.AddNeverTranslated( "P", true ).AddRaw( ptIndex.ToString() );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );

                                                ExtraData.Buffer.AddNeverTranslated( "Road Prefab: ", true, "a6a3a0" );
                                                ExtraData.Buffer.AddRaw( road.ID ).Line();
                                                if ( roadIndexToDraw >= 0 )
                                                    ExtraData.Buffer.AddNeverTranslated( "Road Index: ", true, "a6a3a0" ).AddRaw( roadIndexToDraw.ToString() ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "-------------------------------------------------", true, "595959" ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Lane ID: ", true, "a6a3a0" ).AddRaw( lane.ID ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Point Index For Lane: ", true, "a6a3a0" ).AddRaw( ptIndex.ToString() ).Line();
                                                ExtraData.Buffer.AddNeverTranslated( "Right-click here to duplicate point, middle-click to delete point.", true );
                                                Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                            }
                                            break;
                                        case UIAction.OnClick:
                                            {
                                                if ( ExtraData.MouseInput.RightButtonClicked )
                                                {
                                                    Vector3 atStart = lane.LanePoints[ptIndex];
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                    {
                                                        switch ( Stage )
                                                        {
                                                            case GeneralUndoActionStage.Execute:
                                                            case GeneralUndoActionStage.Redo:
                                                                lane.LanePoints.Insert( ptIndex, atStart );
                                                                break;
                                                            case GeneralUndoActionStage.Undo:
                                                                lane.LanePoints.RemoveAt( ptIndex );
                                                                break;
                                                        }
                                                    } );
                                                }
                                                else if ( ExtraData.MouseInput.MiddleButtonClicked )
                                                {
                                                    Vector3 atStart = lane.LanePoints[ptIndex];
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                    {
                                                        switch ( Stage )
                                                        {
                                                            case GeneralUndoActionStage.Execute:
                                                            case GeneralUndoActionStage.Redo:
                                                                lane.LanePoints.RemoveAt( ptIndex );
                                                                break;
                                                            case GeneralUndoActionStage.Undo:
                                                                lane.LanePoints.Insert( ptIndex, atStart );
                                                                break;
                                                        }
                                                    } );
                                                }
                                            }
                                            break;
                                    }
                                }, 12f, TextWrapStyle.Unset_DoDefault );

                                bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                                UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "LaneX", outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                    if ( road == null ) return;
                                    LanePrefab lane = road.Lanes[laneIndex];
                                    if ( lane == null ) return;

                                    //lane.Points.x
                                    ArcenUI_Input input = element as ArcenUI_Input;
                                    switch ( Action )
                                    {
                                        case UIAction.WriteToUIFromData:
                                            input.SetTextIfDifferentFromLastTextAndNotFocused( lane.LanePoints[ptIndex].x.ToString() );
                                            break;
                                        case UIAction.SendToDataAfterUIEdits:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                            {
                                                float atStart = lane.LanePoints[ptIndex].x;
                                                if ( atStart != result )
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                        {
                                                            switch ( Stage )
                                                            {
                                                                case GeneralUndoActionStage.Execute:
                                                                case GeneralUndoActionStage.Redo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceX( result );
                                                                    break;
                                                                case GeneralUndoActionStage.Undo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceX( atStart );
                                                                    break;
                                                            }
                                                        } );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDrag:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                            {
                                                float change = ExtraData.DragDiff.x / 100;
                                                float newData = orig += change;
                                                input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                                lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceX( newData );
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDragComplete:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                                if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                                {
                                                    if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                                    {
                                                        levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                        {
                                                            switch ( Stage )
                                                            {
                                                                case GeneralUndoActionStage.Execute:
                                                                case GeneralUndoActionStage.Redo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceX( newFinal );
                                                                    break;
                                                                case GeneralUndoActionStage.Undo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceX( atStart );
                                                                    break;
                                                            }
                                                        } );
                                                    }
                                                }
                                            }
                                            break;
                                        case UIAction.OnFocus:
                                            input.ReferenceInputField.SelectAll();
                                            input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                            break;
                                    }
                                } );

                                bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + PAD_BETWEEN_FLOATS, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                                UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "LaneY", outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                    if ( road == null ) return;
                                    LanePrefab lane = road.Lanes[laneIndex];
                                    if ( lane == null ) return;

                                    //lane.Points.y
                                    ArcenUI_Input input = element as ArcenUI_Input;
                                    switch ( Action )
                                    {
                                        case UIAction.WriteToUIFromData:
                                            input.SetTextIfDifferentFromLastTextAndNotFocused( lane.LanePoints[ptIndex].y.ToString() );
                                            break;
                                        case UIAction.SendToDataAfterUIEdits:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                            {
                                                float atStart = lane.LanePoints[ptIndex].y;
                                                if ( atStart != result ) 
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                        {
                                                            switch ( Stage )
                                                            {
                                                                case GeneralUndoActionStage.Execute:
                                                                case GeneralUndoActionStage.Redo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceY( result );
                                                                    break;
                                                                case GeneralUndoActionStage.Undo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceY( atStart );
                                                                    break;
                                                            }
                                                        } );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDrag:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                            {
                                                float change = ExtraData.DragDiff.x / 100;
                                                float newData = orig += change;
                                                input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                                lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceY( newData );
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDragComplete:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                                if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                                {
                                                    if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                                    {
                                                        levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                        {
                                                            switch ( Stage )
                                                            {
                                                                case GeneralUndoActionStage.Execute:
                                                                case GeneralUndoActionStage.Redo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceY( newFinal );
                                                                    break;
                                                                case GeneralUndoActionStage.Undo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceY( atStart );
                                                                    break;
                                                            }
                                                        } );
                                                    }
                                                }
                                            }
                                            break;
                                        case UIAction.OnFocus:
                                            input.ReferenceInputField.SelectAll();
                                            input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                            break;
                                    }
                                } );

                                bounds = ArcenFloatRectangle.CreateUnityRect( bounds.xMax + PAD_BETWEEN_FLOATS, runningY, FLOAT_WIDTH, ROW_HEIGHT );
                                UIFlow.AddInputDraggableTextbox( "DraggableTextbox", Set, type_iFloatText, "LaneZ", outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                    if ( road == null ) return;
                                    LanePrefab lane = road.Lanes[laneIndex];
                                    if ( lane == null ) return;

                                    //lane.Points.z
                                    ArcenUI_Input input = element as ArcenUI_Input;
                                    switch ( Action )
                                    {
                                        case UIAction.WriteToUIFromData:
                                            input.SetTextIfDifferentFromLastTextAndNotFocused( lane.LanePoints[ptIndex].z.ToString() );
                                            break;
                                        case UIAction.SendToDataAfterUIEdits:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float result ) )
                                            {
                                                float atStart = lane.LanePoints[ptIndex].z;
                                                if ( atStart != result)
                                                    levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                    {
                                                        switch ( Stage )
                                                        {
                                                            case GeneralUndoActionStage.Execute:
                                                            case GeneralUndoActionStage.Redo:
                                                                lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceZ( result );
                                                                break;
                                                            case GeneralUndoActionStage.Undo:
                                                                lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceZ( atStart );
                                                                break;
                                                        }
                                                    } );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDrag:
                                            if ( FloatExtensions.TryParsePrecise( input.GetText(), out float orig ) )
                                            {
                                                float change = ExtraData.DragDiff.x / 100;
                                                float newData = orig += change;
                                                input.SetTextIfDifferentFromLastTextAndNotFocused( newData.ToString() );
                                                lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceZ( newData );
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                            }
                                            break;
                                        case UIAction.MouseDragComplete:
                                            {
                                                lane.LevelEditorMouseOverIndex = ValueUntilTime<int>.Create( ptIndex, -1, 0.1f );
                                                if ( FloatExtensions.TryParsePrecise( input.GetText(), out float newFinal ) )
                                                {
                                                    if ( FloatExtensions.TryParsePrecise( input.TextOnMouseDragStart, out float atStart ) )
                                                    {
                                                        levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                                                        {
                                                            switch ( Stage )
                                                            {
                                                                case GeneralUndoActionStage.Execute:
                                                                case GeneralUndoActionStage.Redo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceZ( newFinal );
                                                                    break;
                                                                case GeneralUndoActionStage.Undo:
                                                                    lane.LanePoints[ptIndex] = lane.LanePoints[ptIndex].ReplaceZ( atStart );
                                                                    break;
                                                            }
                                                        } );
                                                    }
                                                }                                                
                                            }
                                            break;
                                        case UIAction.OnFocus:
                                            input.ReferenceInputField.SelectAll();
                                            input.ReferenceInputField.MarkGeometryAsDirty(); //this makes us actually able to SEE that it is selected-all
                                            break;
                                    }
                                } );

                                runningY += ROW_HEIGHT + ROW_BUFFER;
                            }
                        }

                        if ( laneOuter.LanePoints.Count == 0 )
                        {
                            globalPointIndex++; //this exists so that we have unique IDs for each, and that should include the non-point ones here

                            int roadIndex = roadIndexOuter;
                            int laneIndex = laneIndexTemp;

                            //sole row
                            bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                            UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, string.Empty, outerIndex, globalPointIndex, laneHash, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                RoadPrefab road = SelectedA5PlaceableInUse?.ObjRoot?.Roads[roadIndex];
                                if ( road == null ) return;
                                LanePrefab lane = road.Lanes[laneIndex];
                                if ( lane == null ) return;

                                //object name
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            ExtraData.Buffer.StartSize90();
                                            if ( roadIndexToDraw > 0 )
                                                ExtraData.Buffer.AddNeverTranslated( "R", true ).AddRaw( roadIndexToDraw.ToString() ).Space1x();
                                            ExtraData.Buffer.AddRaw( lane.ID );
                                            ExtraData.Buffer.AddNeverTranslated( " - No Points Yet!", true );
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            ExtraData.Buffer.AddNeverTranslated( "Road Prefab: ", true, "a6a3a0" );
                                            ExtraData.Buffer.AddRaw( road.ID ).Line();
                                            if ( roadIndexToDraw >= 0 )
                                                ExtraData.Buffer.AddNeverTranslated( "Road Index: ", true, "a6a3a0" ).AddRaw( roadIndexToDraw.ToString() ).Line();
                                            ExtraData.Buffer.AddNeverTranslated( "-------------------------------------------------", true, "595959" ).Line();
                                            ExtraData.Buffer.AddNeverTranslated( "Lane ID: ", true, "a6a3a0" ).AddRaw( lane.ID ).Line();
                                            ExtraData.Buffer.AddNeverTranslated( "Point Index For Lane: ", true, "a6a3a0" ).AddNeverTranslated( "No points yet!", true ).Line();
                                            ExtraData.Buffer.AddNeverTranslated( "Click here to add the first point to this lane.", true );
                                            Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            lane.LanePoints.Add( new Vector3( 0, 0, 0 ) );
                                        }
                                        break;
                                }
                            }, 12f, TextWrapStyle.Unset_DoDefault );   

                            runningY += ROW_HEIGHT + ROW_BUFFER;
                        }
                    }
                }
                #endregion

                if ( workingChangedRows.Count > 0 )
                {
                    #region Changes Detected!  Save them?
                    outerIndex = 9000;

                    {
                        //id path to the placeable, which is helpful for many reasons
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "Changes", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow ).AddRaw( workingChangedRows.Count.ToString() )
                                        .AddNeverTranslated( " Unsaved changes.", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "If you don't save these changes, they will be lost when you close the level editor.  If you wish to save them, click this button.", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    {
                                        foreach ( KeyValuePair<string, ArcenDynamicTableBase> kv in workingChangedRows )
                                        {
                                            kv.Value.TrySaveAllRowsMatchingPath( kv.Key );
                                        }
                                        Window_LevelEditorBottomRightPopup.AddPopupMessage( "Xml Saved!", 3f );
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_BUFFER;
                    }
                    #endregion
                }
                else
                {
                    #region No Changes Detected
                    outerIndex = 9000;

                    {
                        //id path to the placeable, which is helpful for many reasons
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "Changes", outerIndex, -1, objID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartColor( ColorTheme.RustDarker ).AddNeverTranslated( "No changes detected.", true );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "If there were changes, you would probably want to save them.", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_BUFFER;
                    }
                    #endregion
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

        /// <summary>
        /// Top header, which shows the name of the object being edited
        /// </summary>
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                //if ( SelectedObjectList.Count > 0 )
                //{
                //    GameObject obj = SelectedObjectList[0];
                //    Buffer.Add( obj.name );
                //}
                if ( SelectedA5PlaceableInUse )
                {
                    Buffer.AddRaw( SelectedA5PlaceableInUse.ObjRoot.ExtraPlaceableData.GetDisplayName() );
                }
            }
            public override void OnUpdate() { }
        }

        public class customParent : CustomUIAbstractBase
        {
            private int heightToShow = 0;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LevelEditorSecondaryInfo.Instance != null )
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
                    //note: we would uncomment these in order to have this appear from the bottom up
                    //this.Element.RelevantRect.anchorMin = new Vector2( 0, 0 );
                    //this.Element.RelevantRect.anchorMax = new Vector2( 0, 0 );
                    //this.Element.RelevantRect.pivot = new Vector2( 0, 0 );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( !Window_LevelEditorLevelDetails.Instance.GetShouldDrawThisFrame() )
                    this.WindowController.ExtraOffsetY = 0; //not drawing the level editor details, so snap to the top
                else
                {
                    //yes drawing the level editor details, so go down by the amount of that window's extra height, plus a tiny buffer
                    this.WindowController.ExtraOffsetY = (Window_LevelEditorLevelDetails.customParent.CustomParentInstance.HeightToShow * 0.86f);
                }
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

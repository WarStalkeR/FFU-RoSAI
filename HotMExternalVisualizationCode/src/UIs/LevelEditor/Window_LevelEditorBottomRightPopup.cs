using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorBottomRightPopup : WindowControllerAbstractBase
    {
        public static Window_LevelEditorBottomRightPopup Instance;
        public Window_LevelEditorBottomRightPopup()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		#region GetShouldDrawThisFrame_Subclass
		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_Universal.GameLoop == null )
                return false;
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false;//only render in the level editor

            #region Settings Check
            if ( ( DateTime.Now - lastTimeCheckedSettings).TotalSeconds > 1 )
            {
                DebugMousePosition = GameSettings.Current?.GetBool( "LevelEditorDebugMousePosition" )??false;
                lastTimeCheckedSettings = DateTime.Now;
            }
            #endregion

            if ( DebugMousePosition )
                return true;
            if ( Messages.Count > 0 )
                return true;

            if ( !Engine_Universal.IsLoadingXmlNow )
            {
                if ( !A5ObjectAggregation.GetHasLoadedAllLevelItems() )
                    return true;
                if ( CommonPlanner.Instance == null )
                    return true;
            }

            if ( Engine_HotM.PlaceableUnderMouse as A5Placeable )
                return true;

            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
            if ( levelEditorHook != null )
            {
                if ( levelEditorHook.GetCountOfOutOfBoundsObjects() > 0 )
                    return true;
                if ( levelEditorHook.GetCountOfNegativeScaleObjects() > 0 )
                    return true;
                if ( levelEditorHook.GetCountOfTooLargeOrSmallObjects() > 0 )
                    return true;
            }

            LevelType levelType = levelEditorHook?.CurrentLevelType;
            if ( levelType != null ) 
            {
                string lastSaveOrLoadPath = Window_LevelEditorHeaderBar.LastSaveOrLoadPath;
                if ( lastSaveOrLoadPath != null && lastSaveOrLoadPath.Length > 0 )
                {
                    if ( !lastSaveOrLoadPath.Contains( levelType.Subfolder + "/", StringComparison.InvariantCultureIgnoreCase ) )
                        return true; //path mismatch
                }
            }

            return false;
        }
        #endregion

        #region AddPopupMessage
        private static int LastMessageID = 0;

        public static void AddPopupMessage( string MessageText, float SecondsToLastFor )
        {
            if ( Engine_Universal.GameLoop == null )
                return;
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
            {
                ArcenDebugging.LogWithStack( "Called Window_LevelEditorBottomRightPopup.AddPopupMessage from outside of the level editor!", Verbosity.ShowAsError );
                return;
            }

            MessageData message;
            message.MessageID = Interlocked.Increment( ref LastMessageID );
            message.Expires = ArcenTime.AnyTimeSinceStartF + SecondsToLastFor;
            message.Text = MessageText;

            Messages[message.MessageID] = message;
        }
        #endregion

        private static readonly ConcurrentDictionary<int,MessageData> Messages = ConcurrentDictionary<int, MessageData>.Create_WillNeverBeGCed( "Window_LevelEditorBottomRightPopup-Messages" );

        private static DateTime lastTimeCheckedSettings = DateTime.MinValue;
        private static bool DebugMousePosition = false;

        private const float FULL_WIDTH = 240f;

        private const float ROW_HEIGHT = 24;
        private const float ROW_BUFFER = 1.5f;

        private ArcenCachedExternalTypeDirect type_tLabelGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric ) );

        private static int lastYHeightOfInterior = 0;
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

            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
            LevelType levelType = levelEditorHook.CurrentLevelType;

            float runningY = 3;
            Rect bounds;
            int outerIndex;

            #region Messages
            outerIndex = 300; //Messages

            foreach ( KeyValuePair<int, MessageData> kv in Messages )
            {
                bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, string.Empty, outerIndex, 0, kv.Key, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                ExtraData.Buffer.AddNeverTranslated( kv.Value.Text, true );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                ExtraData.Buffer.AddNeverTranslated( kv.Value.Text, true );
                                Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                            }
                            break;
                    }
                }, 12f, TextWrapStyle.Unset_DoDefault );

                runningY += ROW_HEIGHT + ROW_BUFFER;
            }
            #endregion

            #region DebugMousePosition
            if ( DebugMousePosition )
            {
                outerIndex = 400; //DebugMousePosition

                bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "MouseScreen", outerIndex, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                ExtraData.Buffer.AddNeverTranslated( "Screen (X,Y): ", true ).AddNeverTranslated( ArcenInput.MouseScreenX.ToStringWholeBasic(), true ).AddNeverTranslated( ", ", true ).AddNeverTranslated( ArcenInput.MouseScreenY.ToStringWholeBasic(), true );
                            }
                            break;
                    }
                }, 12f, TextWrapStyle.Unset_DoDefault );

                runningY += ROW_HEIGHT + ROW_BUFFER;

                bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "MouseWorld", outerIndex, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                Vector3 worldLoc = Engine_HotM.MouseWorldLocation;
                                if ( worldLoc.y < -10000 )
                                    ExtraData.Buffer.AddNeverTranslated( "World Plane Not Intersected", true );
                                else
                                    ExtraData.Buffer.AddNeverTranslated( "World (X,Y,Z): ", true ).AddNeverTranslated( worldLoc.x.ToStringWholeBasic(), true )
                                        .AddNeverTranslated( ", ", true ).AddNeverTranslated( worldLoc.y.ToStringWholeBasic(), true ).AddNeverTranslated( ", ", true ).AddNeverTranslated( worldLoc.z.ToStringWholeBasic(), true );
                            }
                            break;
                    }
                }, 12f, TextWrapStyle.Unset_DoDefault );

                runningY += ROW_HEIGHT + ROW_BUFFER;
            }
            #endregion

            #region Late Loading Items
            if ( !Engine_Universal.IsLoadingXmlNow )
            {
                outerIndex = 500; //Late Loading Items

                if ( !A5ObjectAggregation.GetHasLoadedAllLevelItems() )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "!GetHasLoadedAllLevelItems", outerIndex, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Still Loading: ", true ).AddNeverTranslated( A5ObjectAggregation.A5LevelItems_StillLoading.ToString(), true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Still Loading: ", true ).AddNeverTranslated( ( A5ObjectAggregation.A5LevelItems_StillLoading +
                                        A5ObjectAggregation.A5LevelItemCategories_StillLoading ).ToString(), true ).Line();
                                    ExtraData.Buffer.AddNeverTranslated( "Cats Still Loading: ", true ).AddNeverTranslated( A5ObjectAggregation.A5LevelItemCategories_StillLoading.ToString(), true ).Line();
                                    ExtraData.Buffer.AddNeverTranslated( "Time Last Loaded: ", true ).AddNeverTranslated( A5ObjectAggregation.A5LevelItems_TimeLastLoaded.ToString(), true ).Line();
                                    ExtraData.Buffer.AddNeverTranslated( "Time Now: ", true ).AddNeverTranslated( ArcenTime.AnyTimeSinceStartF.ToString(), true ).Line();
                                    ExtraData.Buffer.AddNeverTranslated( "A5ObjectAggregation Loaded: ", true ).AddNeverTranslated( A5ObjectAggregation.A5LevelItems_Loaded.ToString(), true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
                if ( CommonPlanner.Instance == null )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "!CommonPlanner.Instance", outerIndex, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "CommonPlanner.Instance still null", true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "CommonPlanner.Instance still null", true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
            }
            #endregion

            #region PlaceableUnderMouse
            {
                outerIndex = 500; //PlaceableUnderMouse

                A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
                if ( place )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "PlaceableUnderMouse", outerIndex, place.LevelEditorConnectionIndex + place.GetSpecificLevelEditorObjectID(), 
                        place.ObjectRootID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        if ( !place ) //this can happen when things are stale after a reload
                            return;

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    if ( place.LevelEditorConnectionIndex >= 0 )
                                        ExtraData.Buffer.AddNeverTranslated( "Border Object: ", true, "ff5d8b" );
                                    ExtraData.Buffer.AddNeverTranslated( place.name, true );
                                    if ( place.AdvancedIsMetaRegion )
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( " Size: ", true, "ff5d8b" );
                                        Bounds objBounds = BoundsUtility.BoundsFrom_NotThreadsafe_MayNotWorkAsExpected( place.gameObject );
                                        ExtraData.Buffer.AddNeverTranslated( "{", true ).AddNeverTranslated( objBounds.size.x.ToString( "0.##" ), true ).AddNeverTranslated( " x ", true ).AddNeverTranslated( objBounds.size.z.ToString( "0.##" ), true ).AddNeverTranslated( "}", true );
                                    }
                                    if ( place.LevelEditorIsTooLargeOrSmall )
                                        ExtraData.Buffer.AddNeverTranslated( " BAD SIZE", true, "fd6dff" );
                                    if ( place.LevelEditorIsNegativeScale )
                                        ExtraData.Buffer.AddNeverTranslated( " NEG SCALE", true, "ff4545" );
                                    if ( place.LevelEditorIsOutOfBounds )
                                        ExtraData.Buffer.AddNeverTranslated( " OUT OF BOUNDS", true, "ff1c1c" );
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;

                    if ( GameSettings.Current.GetBool( "LevelEditorDebugBoundsOfHoveredBoxes" ) )
                    {
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT + ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "BoundsUnderMouse", outerIndex, place.LevelEditorConnectionIndex + place.GetSpecificLevelEditorObjectID(), 
                            place.ObjectRootID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            if ( !place ) //this can happen when things are stale after a reload
                                return;

                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        Bounds objBounds = BoundsUtility.BoundsFrom_NotThreadsafe_MayNotWorkAsExpected( place.gameObject );
                                        ExtraData.Buffer.AddNeverTranslated( objBounds.ToString(), true );
                                        ExtraData.Buffer.Line();
                                        ExtraData.Buffer.AddNeverTranslated( "Pivot: ", true ).AddNeverTranslated( place.ObjRoot.OriginalPivot.ToString(), true );
                                        ExtraData.Buffer.AddNeverTranslated( "   Front: ", true );
                                        switch ( place.FrontSide )
                                        {
                                            case FrontFace.NoFront:
                                                ExtraData.Buffer.AddNeverTranslated( "-", true );
                                                break;
                                            case FrontFace.North:
                                                ExtraData.Buffer.AddNeverTranslated( "N", true );
                                                break;
                                            case FrontFace.South:
                                                ExtraData.Buffer.AddNeverTranslated( "S", true );
                                                break;
                                            case FrontFace.East:
                                                ExtraData.Buffer.AddNeverTranslated( "E", true );
                                                break;
                                            case FrontFace.West:
                                                ExtraData.Buffer.AddNeverTranslated( "W", true );
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_HEIGHT + ROW_BUFFER;
                    }

                    if ( GameSettings.Current.GetBool( "LevelEditorShowFloorsOfSingleBuildingUnderCursor" ) )
                    {
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT + ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "FloorsUnderMouse", outerIndex, place.LevelEditorConnectionIndex + place.GetSpecificLevelEditorObjectID(),
                            place.ObjectRootID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( !place || place.ObjRoot?.Building == null ) //this can happen when things are stale after a reload
                                    return;

                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            ExtraData.Buffer.AddNeverTranslated( "Floors: ", true ).AddNeverTranslated( place.ObjRoot.Building.BuildingFloors.Count.ToString(), true );
                                            ExtraData.Buffer.AddNeverTranslated( "   Storage: ", true ).AddNeverTranslated( place.ObjRoot.Building.NormalTotalStorageVolumeFullDimensions.ToString(), true );
                                        }
                                        break;
                                }
                            }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_HEIGHT + ROW_BUFFER;
                    }

                    if ( place.DebugMessageForTooltip.Length > 0 )
                    {
                        int rowsToDraw = place.DebugMessageRowsToDraw;
                        if ( rowsToDraw < 1 )
                            rowsToDraw = 1;

                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT * rowsToDraw );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "DebugMessageUnderMouse", outerIndex, place.LevelEditorConnectionIndex + place.GetSpecificLevelEditorObjectID(),
                            place.ObjectRootID, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                if ( !place ) //this can happen when things are stale after a reload
                                    return;

                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            ExtraData.Buffer.AddNeverTranslated( place.DebugMessageForTooltip, true );
                                        }
                                        break;
                                }
                            }, 10f, TextWrapStyle.Unset_DoDefault );

                        runningY += ( ROW_HEIGHT * rowsToDraw ) + ROW_BUFFER;
                    }
                }
            }
            #endregion

            #region CountOfOutOfBoundsObjects
            {
                outerIndex = 700; //CountOfOutOfBoundsObjects

                int countOfOutOfBounds = levelEditorHook.GetCountOfOutOfBoundsObjects();

                if ( countOfOutOfBounds > 0 )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "CountOfOutOfBoundsObjects", outerIndex, countOfOutOfBounds, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Out Of Bounds Objects: ", true, "ff3838" );
                                    ExtraData.Buffer.AddNeverTranslated( countOfOutOfBounds.ToString(), true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "All objects should be within the bounds of the level, or else there will be problems when this level is used to populate the game.  The objects that are out of bounds are visually pulsing red.\n\n", true );
                                    ExtraData.Buffer.AddNeverTranslated( "Click here to select all out of bounds objects.", true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                            case UIAction.OnClick:
                                levelEditorHook.SelectAllOutOfBoundsObjects();
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
            }
            #endregion

            #region CountOfNegativeScaleObjects
            {
                outerIndex = 800; //CountOfNegativeScaleObjects

                int countOfOutOfBounds = levelEditorHook.GetCountOfNegativeScaleObjects();

                if ( countOfOutOfBounds > 0 )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "CountOfNegativeScaleObjects", outerIndex, countOfOutOfBounds, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Negative Scale Objects: ", true, "ff3838" );
                                    ExtraData.Buffer.AddNeverTranslated( countOfOutOfBounds.ToString(), true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "All objects should have a positive scale.  Sometimes by editing them, their scale is made negative, like turning a sock inside out.  It sort of looks right, but many things will act wrong on it.  The objects that are negative scale are visually pulsing red.\n\n", true );
                                    ExtraData.Buffer.AddNeverTranslated( "Click here to select all negative scale objects.", true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                            case UIAction.OnClick:
                                levelEditorHook.SelectAllNegativeScaleObjects();
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
            }
            #endregion

            #region CountOfTooLargeOrSmallObjects
            {
                outerIndex = 900; //CountOfTooLargeOrSmallObjects

                int countOfOutOfBounds = levelEditorHook.GetCountOfTooLargeOrSmallObjects();

                if ( countOfOutOfBounds > 0 )
                {
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                    UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, "CountOfTooLargeOrSmallObjects", outerIndex, countOfOutOfBounds, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Too Large/Small Objects: ", true, "ff3838" );
                                    ExtraData.Buffer.AddNeverTranslated( countOfOutOfBounds.ToString(), true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Some regions have a limit on how small they can be and still be useful, or how large they can be and still seed vegetation.  The regions that are out of bounds are visually pulsing pink.\n\n", true );
                                    ExtraData.Buffer.AddNeverTranslated( "Click here to select all too large and small objects.", true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                            case UIAction.OnClick:
                                levelEditorHook.SelectAllTooLargeOrSmallObjects();
                                break;
                        }
                    }, 12f, TextWrapStyle.Unset_DoDefault );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
                }
            }
            #endregion

            #region PathMismatch
            outerIndex = 1100; //PathMismatch

            if ( levelType != null )
            {
                string lastSaveOrLoadPath = Window_LevelEditorHeaderBar.LastSaveOrLoadPath;
                if ( lastSaveOrLoadPath != null && lastSaveOrLoadPath.Length > 0 )
                {
                    if ( !lastSaveOrLoadPath.Contains( levelType.Subfolder + "/", StringComparison.InvariantCultureIgnoreCase ) )
                    {
                        bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH, ROW_HEIGHT );
                        UIFlow.AddText( "HoverableText", Set, type_tLabelGeneric, lastSaveOrLoadPath, outerIndex, levelType.RowIndexNonSim, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "Path mismatch (hover for details).", true, "ff1f33" );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        ExtraData.Buffer.AddNeverTranslated( "The path of the current level type is supposed to include '", true ).AddNeverTranslated( levelType.Subfolder + "/", true, "fff17b" ).AddNeverTranslated( "', but it does not.\n", true );
                                        ExtraData.Buffer.AddNeverTranslated( "The last path was '", true ).AddNeverTranslated( lastSaveOrLoadPath, true, "fff17b" ).AddNeverTranslated( "'.\n\n", true );
                                        ExtraData.Buffer.AddNeverTranslated( "This probably means that a level was copy-pasted from one folder to another, and the level type has not yet been set to match the proper level type for that new folder.", true );
                                        Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    levelEditorHook.SelectAllTooLargeOrSmallObjects();
                                    break;
                            }
                        }, 12f, TextWrapStyle.Unset_DoDefault );

                        runningY += ROW_HEIGHT + ROW_BUFFER;
                    }
                }
            }
            #endregion

            bMainContentParent.ParentRT.UI_SetHeight( runningY );

            lastYHeightOfInterior = Mathf.CeilToInt( runningY );
        }

        public class tLabelGeneric : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {                
            }
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
                if ( Window_LevelEditorBottomRightPopup.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        hasGlobalInitialized = true;
                    }
                    #endregion
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 2 + MathA.Min( lastYHeightOfInterior, 400 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    //note: the below makes this appear from the bottom up
                    this.Element.RelevantRect.anchorMin = new Vector2( 0, 0 );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0, 0 );
                    this.Element.RelevantRect.pivot = new Vector2( 0, 0 );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                #region Remove Expired Messages
                foreach ( KeyValuePair<int, MessageData> kv in Messages )
                {
                    if ( kv.Value.Expires <= ArcenTime.AnyTimeSinceStartF )
                        Messages.TryRemove( kv.Key, 5 );
                }
                #endregion

                if ( !Window_LevelEditorSingleObjectEditableDetails.Instance.GetShouldDrawThisFrame() )
                    this.WindowController.ExtraOffsetY = 0; //not drawing the single object editable details, so snap to the bottom
                else
                {
                    //yes drawing the single object editable details, so go up by the amount of that window's extra height, plus a tiny buffer
                    this.WindowController.ExtraOffsetY = -(Window_LevelEditorSingleObjectEditableDetails.customParent.CustomParentInstance.HeightToShow * 0.86f);
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

        private struct MessageData
        {
            public int MessageID;
            public float Expires;
            public string Text;
        }
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorLevelDetails : WindowControllerAbstractBase
    {
        public static Window_LevelEditorLevelDetails Instance;
        public Window_LevelEditorLevelDetails()
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
            if ( !Engine_Universal.GameLoop?.IsLevelEditor??false )
                return false;//only render in the level editor

            //shows when this is set to show
            return (Engine_Universal.GameLoop as LevelEditorCoreGameLoop)?.LevelEditorHook?.IsLevelEditorDetailsWindowShowing??false;
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

        private ArcenCachedExternalTypeDirect type_bButtonGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bButtonGeneric ) );
        private ArcenCachedExternalTypeDirect type_tLabelGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tLabelGeneric ) );
        private ArcenCachedExternalTypeDirect type_iFloatText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iFloatText ) );
        private ArcenCachedExternalTypeDirect type_iReadonlyText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iReadonlyText ) );
        private ArcenCachedExternalTypeDirect type_dLevelType = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dLevelType ) );
        
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
            }

            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
            LevelType levelType = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;

            float runningY = 3;
            Rect bounds;
            //int outerIndex;

            #region dLevelType
            bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
            UIFlow.AddDropdown( "DropdownBlue", Set, type_dLevelType, string.Empty, -1, -1, -1, bounds, null );
            runningY += ROW_HEIGHT + ROW_BUFFER;
            #endregion dLevelType

            #region Buttons For Skeleton Type Levels
            if ( levelType != null && levelType.IsSkeletonStyle )
            {
                int stillWorkingOuter = MetaRegionPopulator.ZonesStillWorking;

                //sole row
                bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "TestPopulateSkeleton", stillWorkingOuter, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    //object name
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                int stillWorking = MetaRegionPopulator.ZonesStillWorking;
                                if ( stillWorking > 0 )
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Generating", true );
                                    ExtraData.Buffer.AddNeverTranslated( " (", true );
                                    ExtraData.Buffer.AddNeverTranslated( stillWorking.ToString(), true );
                                    ExtraData.Buffer.AddNeverTranslated( ")...", true );
                                }
                                else
                                    ExtraData.Buffer.AddNeverTranslated( "Test Populate Metas", true );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                ExtraData.Buffer.AddNeverTranslated( "This allows you to test population of the metas with various buildings or decorations as would be seen in the game.  You will not be able to interact with the populated items at all, and cannot save them as part of the level; they are for example only.", true );
                                Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                MetaRegionPopulator.PopulateAllRegionsInLevelEditor();
                            }
                            break;
                    }
                }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                runningY += ROW_HEIGHT + ROW_BUFFER;

                if ( levelEditorHook.GetCountOfLevelEditorTestObjects() > 0 )
                {
                    //sole row
                    bounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, FULL_WIDTH_AVOIDSCROLLBAR, ROW_HEIGHT );
                    UIFlow.AddButton( "ButtonBlue", Set, type_bButtonGeneric, "ClearSkeletonTest", -1, -1, -1, bounds, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        //object name
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Clear Test Objects", true );
                                    ExtraData.Buffer.AddNeverTranslated( "  (", true );
                                    ExtraData.Buffer.AddNeverTranslated( levelEditorHook.GetCountOfLevelEditorTestObjects().ToString(), true );
                                    ExtraData.Buffer.AddNeverTranslated( ")", true );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    ExtraData.Buffer.AddNeverTranslated( "Get rid of any test objects that were added.", true );
                                    Window_LevelEditorTooltip.bPanel.Instance.SetText( element, ExtraData.Buffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    levelEditorHook.ClearAllLevelEditorTestObjects();
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                    runningY += ROW_HEIGHT + ROW_BUFFER;
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
                Buffer.AddNeverTranslated( "Level Details", true );
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
                if ( Window_LevelEditorLevelDetails.Instance != null )
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

        #region dLevelType
        public class dLevelType : DropdownAbstractBase
        {
            public static dLevelType Instance;
            public dLevelType()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
                LevelType newType = (LevelType)Item.GetItem();
                LevelType priorType = levelEditorHook.CurrentLevelType;
                if ( newType == priorType )
                    return;

                levelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                {
                    switch ( Stage )
                    {
                        case GeneralUndoActionStage.Execute:
                        case GeneralUndoActionStage.Redo:
                            levelEditorHook.CurrentLevelType = newType;
                            break;
                        case GeneralUndoActionStage.Undo:
                            levelEditorHook.CurrentLevelType = priorType;
                            break;
                    }
                } );
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                LevelType typeDataToSelect = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;
                if ( typeDataToSelect == null )
                    typeDataToSelect = LevelTypeTable.Instance.DefaultRow;
                if ( typeDataToSelect == null )
                    typeDataToSelect = LevelTypeTable.Instance.Rows[0];

                if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType == null && typeDataToSelect != null )
                    (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (LevelType)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else
                {
                    for ( int i = 0; i < LevelTypeTable.Instance.Rows.Length; i++ )
                    {
                        LevelType row = LevelTypeTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        LevelType optionItemAsType = (LevelType)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < LevelTypeTable.Instance.Rows.Length; i++ )
                    {
                        LevelType row = LevelTypeTable.Instance.Rows[i];
                        if ( row.IsHidden )
                            continue;
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "Choose which level type this level will conform to.  That dictates size, in-game usage, connection points, and more.";
                LevelType typeDataToSelect = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\nCurrently: <color=#7ab9ff>" + typeDataToSelect.GetDisplayName() + "</color>\n" +
                        typeDataToSelect.GetDescription();
                }
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, LocalizedString.AddNeverTranslated_New( mouseoverText ), TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                LevelType ItemAsType = (LevelType)Item.GetItem();
                Window_LevelEditorTooltip.bPanel.Instance.SetText( ItemElement,
                    LocalizedString.AddNeverTranslated_New( "Choose which level type this level will conform to.  That dictates size, in-game usage, connection points, and more.\n\n<color=#ffc87a>" +
                    ItemAsType.GetDisplayName() + "</color>\n" +
                        ItemAsType.GetDescription() ), TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ControlBindingsMenu : ToggleableWindowController, IInputActionHandler
    {
        private readonly float rowHeight = 24;
        private readonly float rowBuffer = 3f;
        private InputActionCategory CategoryBeingViewed;
        public static Window_ControlBindingsMenu Instance;
        public Window_ControlBindingsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
		}

        public override void OnOpen()
        {
            InputActionTypeDataTable.Instance.CopyCurrentValuesToTemp();
        }

        private ArcenCachedExternalTypeDirect type_iGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iGeneric ) );
        private ArcenCachedExternalTypeDirect type_bModifier1 = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bModifier1 ) );
        private ArcenCachedExternalTypeDirect type_tPlus = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tPlus ) );
        private ArcenCachedExternalTypeDirect type_bKeyCode = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bKeyCode ) );
        private ArcenCachedExternalTypeDirect type_tSettingName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingName ) );

        private enum TooltipGroup
        {
            Label,
            Modifier,
            MainKey
        }


        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "ControlBindings_Header" );
            }
        }
        #endregion

        #region tSubHeaderText
        public class tSubHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "ControlBindings_SubHeader_Function" )
                    .AddNeverTranslated( "<pos=305>", false ).AddLang( "ControlBindings_SubHeader_PrimaryMapping" )
                    .AddNeverTranslated( "<pos=555>", false ).AddLang( "ControlBindings_SubHeader_AlternateMapping" );
            }
        }
        #endregion

        private static int lastNumberOfCompleteClears = 0;

        public override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
            {
                Instance.Close( WindowCloseReason.ShowingRefused );
                return;
            }
            if ( bMainContentParent.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParent.ParentT );

            if ( Engine_HotM.NumberOfCompleteClears != lastNumberOfCompleteClears )
            {
                Set.RefreshAllElements = true;
                lastNumberOfCompleteClears = Engine_HotM.NumberOfCompleteClears;
                return;
            }

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            //arguments to createunityrectagle: X, Y, width, height

            if ( CategoryBeingViewed == null )
                CategoryBeingViewed = InputActionCategoryTable.Instance.GetRowByID( "Common" );


            // Add header buttons (close/save/reset)
            float runningY = 3;

            //Here we check for which Category is active, then display the slider if there are "enough" settings to require a slider to display
            List<InputActionTypeData> listToShow = CategoryBeingViewed.ActionsForThisCategory;

            foreach ( InputActionTypeData inputAction in listToShow )
            {
                if ( inputAction.Keybinds.Count == 0 )
                    continue;

                Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 5, runningY, 300, this.rowHeight );
                UIFlow.AddText( "HoverableTextLight", Set, type_tSettingName, inputAction.ID, inputAction.RowIndexNonSim, -2, -1, nameBounds,
                delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                {
                    #region InputName
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                ExtraData.Buffer.AddNeverTranslated( "<pos=5>", false ).AddRaw( inputAction.GetDisplayName() );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( inputAction.ID, -2, 0 ), Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddRaw( inputAction.GetDisplayName() );
                                    if ( !inputAction.GetDescription().IsEmpty() )
                                    {
                                        novel.Main.StartColor( ColorTheme.NarrativeColor );
                                        novel.Main.AddRaw( inputAction.GetDescription() ).Line();
                                    }
                                }
                            }
                            break;
                    }
                    #endregion InputName
                }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                int keybindIndex = 0;
                foreach ( InputActionKeybind keybind in inputAction.Keybinds )
                {
                    if ( (keybind.KeyCode?.ID??string.Empty) == "MouseWheel" )
                    {
                        nameBounds = ArcenFloatRectangle.CreateUnityRect( 315 + 80 + 25, runningY, 300, this.rowHeight );
                        UIFlow.AddText( "HoverableTextLight", Set, type_tSettingName, inputAction.ID, inputAction.RowIndexNonSim, -3, -1, nameBounds,
                        delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
                        {
                            #region MouseWheel
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.AddRaw( keybind.KeyCode.GetDisplayName() );
                                    }
                                    break;
                                case UIAction.HandleMouseover:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( inputAction.ID, -2, 0 ), Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.TitleUpperLeft.AddRaw( inputAction.GetDisplayName() );
                                            if ( !inputAction.GetDescription().IsEmpty() )
                                            {
                                                novel.Main.StartColor( ColorTheme.NarrativeColor );
                                                novel.Main.AddRaw( inputAction.GetDescription() ).Line();
                                            }
                                        }
                                    }
                                    break;
                            }
                            #endregion MouseWheel
                        }, 12f, TextWrapStyle.NoWrap_Ellipsis );
                    }
                    else
                        AddKeyPair( Set, inputAction, keybind, keybindIndex, ref runningY );
                    keybindIndex++;
                }

                if ( keybindIndex == 1 )
                    runningY += this.rowHeight + rowBuffer;

                UIFlow.AddImage( "SeparationLineControls", Set, type_iGeneric, inputAction.ID, inputAction.RowIndexNonSim, -1, -1,
                    new Rect( 15f, runningY, 0f, 0f ), null );
                runningY += 0.8f + rowBuffer;
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        private void AddKeyPair( ArcenUI_SetOfCreateElementDirectives Set, InputActionTypeData inputAction, InputActionKeybind keybind, int keybindIndex, ref float runningY )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            float startingX = keybindIndex == 0 ? 305f : 555f;

            Rect mod1Bounds = ArcenFloatRectangle.CreateUnityRect( startingX + 10, runningY, 80, this.rowHeight );
            UIFlow.AddButton( "ButtonControl", Set, type_bModifier1, inputAction.ID, inputAction.RowIndexNonSim, keybindIndex, -1, mod1Bounds,
            delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
            {
                #region Modifier1
                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            if ( keybind.Working_amIWaitingOnKeypress_Modifier1 && ArcenTime.AnyTimeSinceStartF > keybind.Working_doNothingUntil )
                            {
                                if ( Input.anyKeyDown )
                                {
                                    for ( int i = 0; i < EnumInput.Lookup.Length; i++ )
                                    {
                                        if ( Input.GetKeyDown( EnumInput.Lookup[i] ) )
                                        {
                                            keybind.Working_amIWaitingOnKeypress_Modifier1 = false;
                                            InputManager.IgnoreAllInput = false;
                                            if ( !InputCode.ByEnumLookup.TryGetValue( i, out keybind.TempModifier1KeyCode ) )
                                                keybind.TempModifier1KeyCode = null;
                                            keybind.Working_doNothingUntil = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                            break;
                                        }
                                    }
                                }
                            }

                            if ( keybind.Working_amIWaitingOnKeypress_Modifier1 )
                                ExtraData.Buffer.AddLang( "UnknownMarker" );
                            else
                            {
                                if ( keybind.TempKeyCode == null && keybind.TempModifier1KeyCode != null )
                                    ExtraData.Buffer.AddLang( "InputAction_ErrorSetOther", "ff6666" );
                                else
                                    ExtraData.Buffer.AddRaw( keybind.TempModifier1KeyCode != null ? keybind.TempModifier1KeyCode.GetDisplayName() : string.Empty,
                                        ColorTheme.GetBasicLightTextBlue( Element.LastHadMouseWithin ) );
                            }
                        }
                        break;
                    case UIAction.HandleMouseover:
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( inputAction.ID, keybindIndex, 0 ), Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                        {
                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                            novel.TitleUpperLeft.AddRaw( inputAction.GetDisplayName() );

                            if ( keybind.TempModifier1KeyCode != null && keybind.TempKeyCode != null )
                                novel.TitleUpperRight.AddRaw( keybind.TempModifier1KeyCode.GetDisplayName() ).AddRaw( "+" ).AddRaw( keybind.TempKeyCode.GetDisplayName() );
                            else if ( keybind.TempKeyCode != null )
                                novel.TitleUpperRight.AddRaw( keybind.TempKeyCode.GetDisplayName() );

                            {
                                ArcenDoubleCharacterBuffer Buffer = novel.Main;
                                Buffer.StartColor( ColorTheme.NarrativeColor );
                                if ( !inputAction.GetDescription().IsEmpty() )
                                    Buffer.AddRaw( inputAction.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLeftClickFormat( "InputAction_Set", ColorTheme.SoftGold ).Line();
                                Buffer.AddRightClickFormat( "InputAction_Unset", ColorTheme.SoftGold ).Line();
                                Buffer.EndLineHeight();
                            }

                            novel.FrameTitle.AddLang( "ModifierKey_Header" );
                            novel.FrameBody.StartColor( ColorTheme.NarrativeColor ).AddLang( "ModifierKey_Explanation" );
                        }
                        break;
                    case UIAction.OnClick:
                        {
                            if ( ArcenTime.AnyTimeSinceStartF < keybind.Working_doNothingUntil )
                                return;

                            if ( ExtraData.MouseInput.RightButtonClicked )
                            {
                                keybind.TempModifier1KeyCode = null;
                            }
                            else
                            {
                                InputManager.IgnoreAllInput = true;
                                keybind.Working_amIWaitingOnKeypress_Modifier1 = true;
                                keybind.Working_doNothingUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                            }
                        }
                        break;
                }
                #endregion Modifier1
            }, -1f, TextWrapStyle.NoWrap_Ellipsis );//okay to use here, as it will be consistent per run

            Rect textPlusBounds = ArcenFloatRectangle.CreateUnityRect( mod1Bounds.xMax, runningY, 25, this.rowHeight );
            UIFlow.AddText( "HoverableText", Set, type_tPlus, inputAction.ID, inputAction.RowIndexNonSim, keybindIndex, -1, textPlusBounds,
            null, 14f, TextWrapStyle.NoWrap_Ellipsis );

            Rect keyCodeBounds = ArcenFloatRectangle.CreateUnityRect( textPlusBounds.xMax, runningY, 80, this.rowHeight );
            UIFlow.AddButton( "ButtonControl", Set, type_bKeyCode, inputAction.ID, inputAction.RowIndexNonSim, keybindIndex, -1, keyCodeBounds,
            delegate ( ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
            {
                #region KeyCode
                switch ( Action )
                {
                    case UIAction.GetTextToShowFromVolatile:
                        {
                            if ( keybind.Working_amIWaitingOnKeypress_Main && ArcenTime.AnyTimeSinceStartF > keybind.Working_doNothingUntil )
                            {
                                if ( Input.anyKeyDown )
                                {
                                    for ( int i = 0; i < EnumInput.Lookup.Length; i++ )
                                    {
                                        if ( Input.GetKeyDown( EnumInput.Lookup[i] ) )
                                        {
                                            keybind.Working_amIWaitingOnKeypress_Main = false;
                                            InputManager.IgnoreAllInput = false;
                                            if ( !InputCode.ByEnumLookup.TryGetValue( i, out keybind.TempKeyCode ) )
                                                keybind.TempKeyCode = null;
                                            keybind.Working_doNothingUntil = ArcenTime.AnyTimeSinceStartF + 0.5f;
                                            break;
                                        }
                                    }
                                }
                            }

                            if ( keybind.Working_amIWaitingOnKeypress_Main )
                                ExtraData.Buffer.AddLang( "UnknownMarker" );
                            else
                                ExtraData.Buffer.AddRaw( keybind.TempKeyCode != null ? keybind.TempKeyCode.GetDisplayName() : string.Empty,
                                        ColorTheme.GetBasicLightTextBlue( Element.LastHadMouseWithin ) );
                        }
                        break;
                    case UIAction.HandleMouseover:
                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( inputAction.ID, keybindIndex, 0 ), Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                        {
                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                            novel.TitleUpperLeft.AddRaw( inputAction.GetDisplayName() );

                            if ( keybind.TempModifier1KeyCode != null && keybind.TempKeyCode != null )
                                novel.TitleUpperRight.AddRaw( keybind.TempModifier1KeyCode.GetDisplayName() ).AddRaw( "+" ).AddRaw( keybind.TempKeyCode.GetDisplayName() );
                            else if ( keybind.TempKeyCode != null )
                                novel.TitleUpperRight.AddRaw( keybind.TempKeyCode.GetDisplayName() );

                            {
                                ArcenDoubleCharacterBuffer Buffer = novel.Main;
                                Buffer.StartColor( ColorTheme.NarrativeColor );
                                if ( !inputAction.GetDescription().IsEmpty() )
                                    Buffer.AddRaw( inputAction.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLeftClickFormat( "InputAction_Set", ColorTheme.SoftGold ).Line();
                                Buffer.AddRightClickFormat( "InputAction_Unset", ColorTheme.SoftGold ).Line();
                                Buffer.EndLineHeight();
                            }

                            novel.FrameTitle.AddLang( "MainKey_Header" );
                            novel.FrameBody.StartColor( ColorTheme.NarrativeColor ).AddLang( "MainKey_Explanation" );
                        }
                        break;
                    case UIAction.OnClick:
                        {
                            if ( ArcenTime.AnyTimeSinceStartF < keybind.Working_doNothingUntil )
                                return;

                            if ( ExtraData.MouseInput.RightButtonClicked )
                            {
                                keybind.TempKeyCode = null;
                            }
                            else
                            {
                                InputManager.IgnoreAllInput = true;
                                keybind.Working_amIWaitingOnKeypress_Main = true;
                                keybind.Working_doNothingUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                            }
                        }
                        break;
                }
                #endregion KeyCode
            }, -1f, TextWrapStyle.NoWrap_Ellipsis );

            if ( keybindIndex >= 1 )
                runningY += this.rowHeight + rowBuffer;
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

        public class tPlus : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                buffer.AddNeverTranslated( "  +", true );
            }
        }

        private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_ControlBindingsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "ControlBindingCategory" );
                        }
                    }
                    #endregion
                }

                this.OnUpdateCategories();
            }

            public void OnUpdateCategories()
            {
                if ( !hasGlobalInitialized )
                    return;

                btnCategoryPool.Clear( 10 );

                foreach ( InputActionCategory category in InputActionCategoryTable.Instance.Rows )
                {
                    if ( category.IsHidden )
                        continue;
                    if ( category.ActionsForThisCategory.Count == 0 )
                        continue;
                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( category );
                }

                #region Positioning Logic 1
                float currentY = -6.7f; //the position of the first entry
                btnCategoryPool.ApplyItemsInRows( 6.1f, ref currentY, 31.7f, 218, 25.6f, false );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            private InputActionCategory Category = null;

            public void Assign( InputActionCategory Category )
            {
                this.Category = Category;
            }

            public override bool GetShouldBeHidden()
            {
                return this.Category == null;
            }

            public override void Clear()
            {
                this.Category = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.Category == null )
                    return;

                bool isSelected = Instance.CategoryBeingViewed == this.Category;
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                buffer.StartColor( isSelected ? ColorTheme.GetCategoryWhite( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                buffer.AddRaw( this.Category.GetDisplayName() );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CategoryBeingViewed = this.Category;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                InputActionCategory category = this.Category;
                if ( category == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( category ), Element, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddRaw( category.GetDisplayName() );
                    if ( !category.GetDescription().IsEmpty() )
                    {
                        novel.Main.StartColor( ColorTheme.NarrativeColor );
                        novel.Main.AddRaw( category.GetDescription() );
                    }
                }
            }
        }
        #endregion

        public class tSettingName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
            }
        }

        public class iGeneric : ImageAbstractBase
        {
        }

        public class bModifier1 : ButtonAbstractBase
        {
        }        

        public class bKeyCode : ButtonAbstractBase
        {
            
        }

        public class bCancel : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddLang( LangCommon.Popup_Common_Cancel );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public class bExit : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );

                return MouseHandlingResult.None;
            }
        }

        public class bSave : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddLang( LangCommon.Popup_Common_Save );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InputActionTypeDataTable.Instance.CopyTempValuesToCurrent();
                ArcenInput.SaveInputMappingsToDisk();
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }

        public class bSetDefaults : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                Buffer.AddLang( LangCommon.Popup_Common_SetDefaults );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                int rowCount = InputActionTypeDataTable.Instance.Rows.Length;
                for ( int i = 0; i < rowCount; i++ )
                {
                    InputActionTypeData inputData = InputActionTypeDataTable.Instance.Rows[i];
                    foreach ( InputActionKeybind bind in inputData.Keybinds )
                    {
                        bind.TempModifier1KeyCode = bind.DefaultModifier1KeyCode;
                        bind.TempModifier2KeyCode = bind.DefaultModifier2KeyCode;
                        bind.TempModifier3KeyCode = bind.DefaultModifier3KeyCode;
                        bind.TempKeyCode = bind.DefaultKeyCode;
                    }
                }
                return MouseHandlingResult.None;
            }
        }

        public override void Close( WindowCloseReason Reason )
        {
            InputManager.IgnoreAllInput = false;
            InputActionTypeDataTable.Instance.CopyCurrentValuesToTemp(); // shouldn't matter, but tidies things up
            base.Close( Reason );
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                //case "Cancel":
                //    this.Close();
                //    //make sure no other input is processed for 0.2 of a second, so that for instance this doesn't open the escape menu.
                //    ArcenInput.BlockForAJustPartOfOneSecond();
                //    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

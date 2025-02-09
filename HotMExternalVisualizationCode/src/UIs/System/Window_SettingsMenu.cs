using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_SettingsMenu : ToggleableWindowController, IInputActionHandler
    {
        private readonly float rowHeight = 24;
        private readonly float rowBuffer = 1f;
        public ArcenSettingCategory CurrentCategory;
        public static Window_SettingsMenu Instance;
        public Window_SettingsMenu()
        {
            Instance = this;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
		}

        public string ShowOnlyCategory = string.Empty;

        public override void OnOpen()
        {
            ArcenSettingTable.Instance.CopyCurrentValuesToTemp();
            this.lastWasShowingAdvanced = CalculateShouldShowAdvancedSettings();
            this.RecalculateCategories();
            if ( this.CurrentCategory == null )
            {
                if ( this.categories.Count > 0 )
                    this.CurrentCategory = this.categories[0];
                else
                    this.CurrentCategory = null;
            }
            this.isTempShowingAdvanced = false;

            if ( bCategory.Original != null ) //scroll left panel back to top when opening
                bCategory.Original.Element.TryScrollToTop();
        }

        public override void OnClose( WindowCloseReason CloseReason )
        {
            this.ShowOnlyCategory = string.Empty;
        }

        public bool isTempShowingAdvanced = false;
        public bool CalculateShouldShowAdvancedSettings()
        {
            return ArcenSettingTable.Instance.GetRowByID( "ShowAdvancedSettings" ).TempValue_Bool || isTempShowingAdvanced;
        }

        #region RecalculateCategories
        public readonly List<ArcenSettingCategory> categories = List<ArcenSettingCategory>.Create_WillNeverBeGCed( 400, "Window_SettingsMenu-categories" );
        public void RecalculateCategories()
        {
            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();
            categories.Clear();
            foreach ( ArcenSettingCategory cat in ArcenSettingCategoryTable.Instance.Rows )
            {
                if ( cat.IsHidden ) //if hidden, skip
                    continue;
                if ( this.ShowOnlyCategory.Length > 0 )
                {
                    if ( cat.ID != this.ShowOnlyCategory )
                        continue;
                }

                if ( !cat.ShowEvenWhenEmpty )
                {
                    //if literally empty, skip
                    if ( cat.VisibleRows_All.Count <= 0 )
                        continue;

                    //otherwise check to see if all the settings within are hidden
                    bool foundAtLeastOneVisible = false;
                    foreach ( ArcenSetting row in cat.VisibleRows_All )
                    {
                        if ( row.GetIsVisibleRightNowBasedOnModsAndExpansions() )
                        {
                            if ( row.IsAdvancedSetting && !shouldShowAdvancedSettings )
                            {
                                if ( row.GetIsTempValueMatchingDefault() )
                                    continue; //only hide advanced rows that match
                            }
                            foundAtLeastOneVisible = true;
                            break;
                        }
                    }
                    if ( !foundAtLeastOneVisible )
                        continue;
                }
                categories.Add( cat );
            }
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Settings_Header" );
            }
        }
        #endregion

        public const int MAX_MOD_DESCRIPTION_LENGTH = 1000;

        private bool lastWasShowingAdvanced = false;


        private readonly ArcenCachedExternalTypeDirect type_bGenericToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bGenericToggle ) );
        private readonly ArcenCachedExternalTypeDirect type_tXmlModName = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tXmlModName ) );
        private readonly ArcenCachedExternalTypeDirect type_bXmlModToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bXmlModToggle ) );
        private readonly ArcenCachedExternalTypeDirect type_tXmlModDisabledDuringGame = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tXmlModDisabledDuringGame ) );
        private readonly ArcenCachedExternalTypeDirect type_bXmlModMore = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bXmlModMore ) );
        private readonly ArcenCachedExternalTypeDirect type_tAdvancedHiddenOverall = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenOverall ) );
        private readonly ArcenCachedExternalTypeDirect type_tSubSectionHeader = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSubSectionHeader ) );
        private readonly ArcenCachedExternalTypeDirect type_tGenericText = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tGenericText ) );
        private ArcenCachedExternalTypeDirect type_iGeneric = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iGeneric ) );
        private readonly ArcenCachedExternalTypeDirect type_bToggle = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( bToggle ) );
        private readonly ArcenCachedExternalTypeDirect type_iIntInput = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( iIntInput ) );
        private readonly ArcenCachedExternalTypeDirect type_sFloatSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( sFloatSlider ) );
        private readonly ArcenCachedExternalTypeDirect type_sIntSlider = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( sIntSlider ) );
        private readonly ArcenCachedExternalTypeDirect type_dDropdown = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( dDropdown ) );
        private readonly ArcenCachedExternalTypeDirect type_tSettingValueDescription = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tSettingValueDescription ) );
        private readonly ArcenCachedExternalTypeDirect type_tAdvancedHiddenSubCat = ArcenExternalTypeManager.GetOrCreateTypeDirect( typeof( tAdvancedHiddenSubCat ) );

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

            bool shouldShowAdvancedSettings = CalculateShouldShowAdvancedSettings();

            if ( this.categories.Count == 0 || lastWasShowingAdvanced != shouldShowAdvancedSettings )
                this.RecalculateCategories();

            lastWasShowingAdvanced = shouldShowAdvancedSettings;

            if ( this.CurrentCategory == null && this.categories.Count > 0 )
            {
                this.CurrentCategory = this.categories[0];
                this.isTempShowingAdvanced = false;
            }
            if ( this.CurrentCategory == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            //arguments to createunityrectagle: X, Y, width, height

            // Add header buttons (close/save/reset)
            float runningY = 3;

            int numberAdvancedHidden = 0;

            //render the main stuff for this category
            this.RenderSettingsFromSubList( this.CurrentCategory.VisibleRows_NotInSubCategory, ref runningY, Set, shouldShowAdvancedSettings, -1, ref numberAdvancedHidden,
                this.CurrentCategory.ShowLanguageDropdownAtIndex );

            #region ShowExpansionsList
            if ( this.CurrentCategory.ShowExpansionsList )
            {
                for ( int i = 0; i < ExpansionTable.Instance.Rows.Length; i++ )
                {
                    Expansion expansion = ExpansionTable.Instance.Rows[i];
                    Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                    UIFlow.AddText( "HoverableTextLight", Set, type_tGenericText, "ExpansionName", expansion.RowIndexNonSim, -1, -1, nameBounds,
                    delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    ExtraData.Buffer.StartSize60().AddLangAndAfterLineItemHeader( "SettingsExpansionFullName" ).EndSize().Space1x().StartColor( expansion.ColorForDisplay ).AddRaw( expansion.GetDisplayName() );
                                    if ( expansion.Abbreviation.Length > 0 )
                                        ExtraData.Buffer.AddRaw( " (" ).AddRaw( expansion.Abbreviation ).AddRaw( ")" );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( !expansion.GetDisplayName().IsEmpty() )
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( expansion ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                        {
                                            novel.ShadowStyle = TooltipShadowStyle.Standard;
                                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "SettingsExpansionFullName" );
                                            novel.TitleUpperLeft.AddRaw( expansion.GetDisplayName(), expansion.ColorForDisplay );
                                            if ( expansion.Abbreviation.Length > 0 )
                                                novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( expansion.Abbreviation ).AddRaw( ")" );

                                            if ( !expansion.GetDescription().IsEmpty() )
                                                novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( expansion.GetDescription() ).Line();
                                        }
                                    }
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                    Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                    if ( expansion.IsInstalledAtAll )
                    {
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            UIFlow.AddButton( "ButtonBlue", Set, type_bGenericToggle, expansion.ID, expansion.RowIndexNonSim, -1, -1, valueSettingControlBounds, //todo later
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            {
                                                ArcenSetting setting = expansion.GetRelatedSetting();
                                                ExtraData.Buffer.AddLang( !setting.TempValue_Bool ? "Enabled" : "Disabled",
                                                    !setting.TempValue_Bool ? string.Empty : "666666" );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( expansion ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "SettingsExpansionFullName" );
                                                    novel.TitleUpperLeft.AddRaw( expansion.GetDisplayName(), expansion.ColorForDisplay );
                                                    if ( expansion.Abbreviation.Length > 0 )
                                                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( expansion.Abbreviation ).AddRaw( ")" );

                                                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "ExpansionDetails_HoverToLeft" ).Line();

                                                    novel.FrameBody.AddLang( "ExpansionTooltipRestartNotes" );
                                                }
                                            }
                                            break;
                                        case UIAction.OnClick:
                                            {
                                                ArcenSetting setting = expansion.GetRelatedSetting();

                                                setting.TempValue_Bool = !setting.TempValue_Bool;
                                                setting.MarkAsCustomPresetIfJustOverrideValue();
                                                ExtraData.MouseResult = MouseHandlingResult.None;
                                            }
                                            break;
                                    }
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        else
                            UIFlow.AddText( "HoverableTextLight", Set, type_tGenericText, "InvalidExpansion", expansion.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            {
                                                ArcenSetting setting = expansion.GetRelatedSetting();
                                                ExtraData.Buffer.AddLang( !setting.TempValue_Bool ? "Enabled" : "Disabled",
                                                    !setting.TempValue_Bool ? string.Empty : "666666" );
                                            }
                                            break;
                                        case UIAction.HandleMouseover:
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( expansion ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "SettingsExpansionFullName" );
                                                    novel.TitleUpperLeft.AddRaw( expansion.GetDisplayName(), expansion.ColorForDisplay );
                                                    if ( expansion.Abbreviation.Length > 0 )
                                                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( expansion.Abbreviation ).AddRaw( ")" );

                                                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "ExpansionDetails_HoverToLeft" ).Line();

                                                    novel.FrameBody.AddLang( "ExpansionTooltipRestartBlocked" );
                                                }
                                            }
                                            break;
                                    }
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                    }
                    else
                        UIFlow.AddText( "HoverableTextLight", Set, type_tGenericText, "NotInstalledExpansion", expansion.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            ExtraData.Buffer.AddLang( "NotInstalled" );
                                            break;
                                        case UIAction.HandleMouseover:
                                            if ( !expansion.GetDisplayName().IsEmpty() )
                                            {
                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( expansion ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                                {
                                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "SettingsExpansionFullName" );
                                                    novel.TitleUpperLeft.AddRaw( expansion.GetDisplayName(), expansion.ColorForDisplay );
                                                    if ( expansion.Abbreviation.Length > 0 )
                                                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( expansion.Abbreviation ).AddRaw( ")" );

                                                    if ( !expansion.GetDescription().IsEmpty() )
                                                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( expansion.GetDescription() ).Line();
                                                }
                                            }
                                            break;
                                    }
                                }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                    runningY += this.rowHeight + rowBuffer;
                }
            }
            #endregion

            #region ShowMainModsList
            if ( this.CurrentCategory.ShowMainModsList )
            {
                for ( int i = 0; i < TotalConversionModTable.Instance.Rows.Length; i++ )
                {
                    TotalConversionMod mod = TotalConversionModTable.Instance.Rows[i];
                    Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                    UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModName, mod.ID, mod.RowIndexNonSim, -1, 33, nameBounds,
                        delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                        }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                    Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                    if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                        UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModToggle, mod.ID, mod.RowIndexNonSim, -1, 33, valueSettingControlBounds, //todo later
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                    else
                        UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModDisabledDuringGame, mod.ID, mod.RowIndexNonSim, -1, 33, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );

                    if ( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                    {
                        Rect moreBounds = ArcenFloatRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 170, this.rowHeight );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModMore, mod.ID, mod.RowIndexNonSim, -1, 33, moreBounds, //todo later
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                    }

                    runningY += this.rowHeight + rowBuffer;
                }

                for ( int i = 0; i < XmlModTable.Instance.Rows.Length; i++ )
                {
                    XmlMod mod = XmlModTable.Instance.Rows[i];
                    if ( mod.IsSaveSafeMod | mod.IsFrameworkMod )
                        continue;
                    Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                    UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModName, mod.ID, mod.RowIndexNonSim, -1, -1, nameBounds,
                        delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                        }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                    Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                    if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                        UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModToggle, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,//todo later
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                    else
                        UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModDisabledDuringGame, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );

                    if ( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                    {
                        Rect moreBounds = ArcenFloatRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 170, this.rowHeight );
                        UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModMore, mod.ID, mod.RowIndexNonSim, -1, -1, moreBounds,//todo later
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                    }

                    runningY += this.rowHeight + rowBuffer;
                }
            }
            #endregion

            int subCatIndex = -1;
            foreach ( ArcenSettingSubcategory subCat in this.CurrentCategory.Subcategories )
            {
                subCatIndex++;
                if ( subCat.VisibleRows.Count <= 0 && !subCat.ShowNetworkExtras && !subCat.ShowSaveSafeModsList && !subCat.ShowFrameworkModsList )
                    continue; //skip any subcategories that would be empty

                //render the main stuff for this subcategory
                this.RenderSettingsFromSubList( subCat.VisibleRows, ref runningY, Set, shouldShowAdvancedSettings, subCatIndex, ref numberAdvancedHidden, -1 );

                #region ShowNetworkExtras
                if ( subCat.ShowNetworkExtras )
                {
                    //Public IP Address

                    //Local IP Addresses
                }
                #endregion

                #region ShowSaveSafeModsList
                if ( subCat.ShowSaveSafeModsList )
                {
                    for ( int i = 0; i < XmlModTable.Instance.Rows.Length; i++ )
                    {
                        XmlMod mod = XmlModTable.Instance.Rows[i];
                        if ( !mod.IsSaveSafeMod )
                            continue;
                        Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                        UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModName, mod.ID, mod.RowIndexNonSim, -1, -1, nameBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                        Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModToggle, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,//todo later
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        else
                            UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModDisabledDuringGame, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );

                        if ( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                        {
                            Rect moreBounds = ArcenFloatRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 170, this.rowHeight );
                            UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModMore, mod.ID, mod.RowIndexNonSim, -1, -1, moreBounds,//todo later
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        }

                        runningY += this.rowHeight + rowBuffer;
                    }
                }
                #endregion

                #region ShowFrameworkModsList
                if ( subCat.ShowFrameworkModsList )
                {
                    for ( int i = 0; i < XmlModTable.Instance.Rows.Length; i++ )
                    {
                        XmlMod mod = XmlModTable.Instance.Rows[i];
                        if ( !mod.IsFrameworkMod )
                            continue;
                        Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                        UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModName, mod.ID, mod.RowIndexNonSim, -1, -1, nameBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                        Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                        if ( Engine_Universal.RunStatus == RunStatus.GameStart )
                            UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModToggle, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,//todo later
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        else
                            UIFlow.AddText( "HoverableTextLight", Set, type_tXmlModDisabledDuringGame, mod.ID, mod.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );

                        if ( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                        {
                            Rect moreBounds = ArcenFloatRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 170, this.rowHeight );//todo later
                            UIFlow.AddButton( "ButtonBlue", Set, type_bXmlModMore, mod.ID, mod.RowIndexNonSim, -1, -1, moreBounds,
                                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        }

                        runningY += this.rowHeight + rowBuffer;
                    }
                }
                #endregion
            }

            if ( numberAdvancedHidden > 0 )
            {
                Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                UIFlow.AddText( "HoverableHeader", Set, type_tAdvancedHiddenOverall, string.Empty, numberAdvancedHidden, numberAdvancedHidden, -1, nameBounds,
                    null, 10f, TextWrapStyle.NoWrap_Ellipsis );
                runningY += this.rowHeight + this.rowBuffer;
            }

            bMainContentParent.ParentRT.UI_SetHeight( runningY );
        }

        public class tAdvancedHiddenOverall : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                int numberHidden = (int)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                buffer.StartColor( ColorTheme.Settings_HiddenFieldNoticeLightRed ).AddFormat1( "HiddenFields", numberHidden );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                int numberHidden = (int)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Settings", "tAdvancedHiddenSubCat" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "HiddenFields_TooltipHeader" );
                    novel.Main.AddFormat1( "HiddenFieldsInstruction", numberHidden );
                }
            }
        }

        public class tAdvancedHiddenSubCat : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                int numberHidden = (int)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                buffer.StartColor( ColorTheme.Settings_HiddenFieldNoticeLightRed ).AddFormat1( "HiddenFieldsInSubCat", numberHidden );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.isTempShowingAdvanced = !Instance.isTempShowingAdvanced;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                int numberHidden = (int)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Settings", "tAdvancedHiddenSubCat" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "HiddenFields_TooltipHeader" );
                    novel.Main.AddFormat1( "HiddenFieldsSubcategoryInstruction", numberHidden );
                }
            }
        }

        #region RenderSettingsFromSubList
        private void RenderSettingsFromSubList( List<ArcenSetting> listToShow, ref float runningY, ArcenUI_SetOfCreateElementDirectives Set,
            bool shouldShowAdvancedSettings, int subCatIndex, ref int numberAdvancedHidden, int ShowLanguageDropdownAtIndex )
        {
            if ( listToShow.Count <= 0 )
                return;

            bool isEnglishVersion = GameSettings.CurrentLanguage?.IsOriginalEnglish??false;

            bool isFirst = true;
            int numberAdvancedHiddenHere = 0;

            for ( int index = 0; index < listToShow.Count; index++ )
            {
                if ( ShowLanguageDropdownAtIndex == index )
                    RenderLanguageDropdown( ref runningY, Set, ref isFirst, subCatIndex );

                ArcenSetting setting = listToShow[index];
                if ( setting.Deprecated ) //don't show deprecated settings
                    continue;
                if ( setting.ShouldOnlyShowInEnglishVersion && !isEnglishVersion )
                    continue;

                if ( !setting.GetIsVisibleRightNowBasedOnModsAndExpansions() )
                    continue; //don't show things hidden by mods or expansions not being installed
                if ( setting.IsAdvancedSetting && !shouldShowAdvancedSettings )
                {
                    if ( setting.GetIsTempValueMatchingDefault() ) //only hide advanced rows that match
                    {
                        numberAdvancedHidden++;
                        numberAdvancedHiddenHere++;
                        continue;
                    }
                }

                bool wasFirst = isFirst;
                if ( isFirst )
                {
                    isFirst = false;
                    //New Section Header
                    if ( subCatIndex >= 0 )
                    {
                        runningY += (this.rowHeight + rowBuffer) * 0.5f;
                        Rect nameHeaderBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                        UIFlow.AddText( "HoverableHeader", Set, type_tSubSectionHeader, string.Empty, subCatIndex, subCatIndex, -1, nameHeaderBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, 12f, TextWrapStyle.NoWrap_Ellipsis );
                        runningY += this.rowHeight + rowBuffer;
                    }
                }

                if ( !wasFirst )
                {
                    UIFlow.AddImage( "SeparationLineControls", Set, type_iGeneric, setting.ID, setting.RowIndexNonSim, -1, -1,
                        new Rect( 15f, runningY, 0f, 0f ), null );
                    runningY += 0.8f + rowBuffer;
                }

                Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                UIFlow.AddText( "HoverableTextLight", Set, type_tGenericText, setting.ID, setting.RowIndexNonSim, -1, -1, nameBounds,
                    delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        ArcenUI_Text text = element as ArcenUI_Text;
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    if ( !setting.GetIsTempValueMatchingDefault() )
                                        ExtraData.Buffer.StartColor( ColorTheme.Settings_CurrentDiffersFromDefaultYellow );
                                    ExtraData.Buffer.AddRaw( setting.GetDisplayName() );
                                    ExtraData.Buffer.EndColor();
                                    setting.AppendSecondLineWithExpansionAndOrModThisIsFrom( ExtraData.Buffer );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    HandleMouseoverForSetting( setting, element );
                                }
                                break;
                        }
                    }, 12f, TextWrapStyle.NoWrap_Ellipsis );//okay to use here, as it will be consistent per run

                Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );
                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        //scale it down
                        valueSettingControlBounds.y += 2;
                        valueSettingControlBounds.height = 20;
                        valueSettingControlBounds.width = 48.8f;
                        UIFlow.AddButton( "ButtonToggle", Set, type_bToggle, setting.ID, setting.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.HandleMouseover:
                                        HandleMouseoverForSetting( setting, element );
                                        break;
                                }
                            }, -1f, TextWrapStyle.NoWrap_Ellipsis );
                        break;
                    case ArcenSettingType.IntTextbox:
                        valueSettingControlBounds.y += 2;
                        valueSettingControlBounds.height -= 2;
                        UIFlow.AddInputTextbox( "BasicTextboxBlue", Set, type_iIntInput, setting.ID, setting.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.HandleMouseover:
                                        HandleMouseoverForSetting( setting, element );
                                        break;
                                }
                            } );
                        break;
                    case ArcenSettingType.FloatSlider:
                        UIFlow.AddHorizontalSlider( "BasicHorizontalSlider", Set, type_sFloatSlider, setting.ID, setting.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.HandleMouseover:
                                        HandleMouseoverForSetting( setting, element );
                                        break;
                                }
                            } );
                        break;
                    case ArcenSettingType.IntSlider:
                        UIFlow.AddHorizontalSlider( "BasicHorizontalSlider", Set, type_sIntSlider, setting.ID, setting.RowIndexNonSim, -1, -1, valueSettingControlBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.HandleMouseover:
                                        HandleMouseoverForSetting( setting, element );
                                        break;
                                }
                            } );
                        break;
                    case ArcenSettingType.IntDropdown:
                        valueSettingControlBounds.y += 2;
                        valueSettingControlBounds.height -= 4;
                        UIFlow.AddDropdown( "DropdownBlue", Set, type_dDropdown, setting.ID, setting.RowIndexNonSim, -1, -1, valueSettingControlBounds, 
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.DropdownSelectionChanged:
                                        {
                                            if ( ExtraData.DropdownItem == null )
                                                return;
                                            ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;

                                            int newValue = elementAsType.IndexOfItem( ExtraData.DropdownItem );
                                            setting.TempValue_Int = newValue;

                                            if ( setting.ID == "QualityPreset" )
                                                SetCurrentPresetValues();
                                            else
                                                setting.MarkAsCustomPresetIfJustOverrideValue();
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        HandleMouseoverForSetting( setting, element );
                                        break;
                                    case UIAction.DropdownOnUpdate:
                                        {
                                            ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                            if ( elementAsType == null )
                                                return;

                                            int tempValue = setting.TempValue_Int;
                                            if ( setting.DropdownFiller != null )
                                            {
                                                tempValue = setting.DropdownFiller.GetTempValue( setting );

                                                List<IArcenDropdownOption> options = setting.DropdownFiller.GetListOfDropdownOptions();
                                                if ( options == null )
                                                    elementAsType.ClearItems();
                                                else if ( elementAsType.GetItemCount() != options.Count )
                                                {
                                                    elementAsType.ClearItems();
                                                    for ( int k = 0; k < options.Count; k++ )
                                                        elementAsType.AddItem( options[k], k == tempValue );
                                                }
                                            }

                                            if ( elementAsType.GetItemCount() > tempValue && tempValue != elementAsType.GetSelectedIndex() )
                                                elementAsType.SetSelectedIndex( tempValue, DropdownSetType.FromExternalData );
                                        }
                                        break;
                                }
                            } );
                        break;
                }

                Rect valueDescriptionBounds = ArcenFloatRectangle.CreateUnityRect( valueSettingControlBounds.xMax, runningY, 320, this.rowHeight );
                UIFlow.AddText( "HoverableHeader", Set, type_tSettingValueDescription, setting.ID, setting.RowIndexNonSim, -1, -1, valueDescriptionBounds,
                            delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                            }, 12f, TextWrapStyle.NoWrap_Ellipsis );

                runningY += this.rowHeight + rowBuffer;
            }

            if ( numberAdvancedHiddenHere > 0 && !isFirst )
            {
                {
                    UIFlow.AddImage( "SeparationLineControls", Set, type_iGeneric, "AdvancedSubCat", numberAdvancedHiddenHere, subCatIndex, -1,
                        new Rect( 15f, runningY, 0f, 0f ), null );
                    runningY += 0.8f + rowBuffer;
                }

                Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                UIFlow.AddText( "HoverableHeader", Set, type_tAdvancedHiddenSubCat, string.Empty, numberAdvancedHiddenHere, subCatIndex, -1, nameBounds,
                    null, 10f, TextWrapStyle.NoWrap_Ellipsis );
                runningY += this.rowHeight + this.rowBuffer;
            }
        }
        #endregion

        #region HandleMouseoverForSetting
        private static void HandleMouseoverForSetting( ArcenSetting setting, ArcenUI_Element Element )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( setting ), Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
            {
                novel.ShadowStyle = TooltipShadowStyle.Standard;
                novel.TitleUpperLeft.AddRaw( setting.GetDisplayName() );
                {
                    ArcenDoubleCharacterBuffer Buffer = novel.Main;
                    if ( !setting.GetDescription().IsEmpty() )
                    {
                        Buffer.StartColor( ColorTheme.NarrativeColor );
                        Buffer.AddRaw( setting.GetDescription() ).Line();
                    }

                    switch ( setting.Type )
                    {
                        case ArcenSettingType.BoolToggle:
                            if ( setting.IgnoresDefaults )
                                Buffer.AddLang( setting.IgnoresDefaultsLangKey, ColorTheme.DataBlue );
                            else
                                Buffer.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                    .AddLang( setting.DefaultBoolValue ? "On" : "Off", ColorTheme.DataBlue ).Line();
                            break;
                        case ArcenSettingType.IntTextbox:
                            if ( setting.IgnoresDefaults )
                                Buffer.AddLang( setting.IgnoresDefaultsLangKey, ColorTheme.DataBlue );
                            else
                                Buffer.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                    .AddRaw( setting.DefaultIntValue.ToString(), ColorTheme.DataBlue ).Line();
                            break;
                        case ArcenSettingType.FloatSlider:
                            if ( setting.IgnoresDefaults )
                                Buffer.AddLang( setting.IgnoresDefaultsLangKey, ColorTheme.DataBlue );
                            else
                                Buffer.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                    .AddRaw( setting.DefaultFloatValue.ToStringThousandsDecimal_Optional4(), ColorTheme.DataBlue ).Line();
                            break;
                        case ArcenSettingType.IntSlider:
                            if ( setting.IgnoresDefaults )
                                Buffer.AddLang( setting.IgnoresDefaultsLangKey, ColorTheme.DataBlue );
                            else
                                Buffer.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                    .AddRaw( setting.DefaultIntValue.ToString(), ColorTheme.DataBlue ).Line();
                            break;
                        case ArcenSettingType.IntDropdown:
                            if ( setting.IgnoresDefaults )
                                Buffer.AddLang( setting.IgnoresDefaultsLangKey, ColorTheme.DataBlue );
                            else
                            {
                                List<IArcenDropdownOption> options = setting.DropdownFiller?.GetListOfDropdownOptions();
                                if ( options == null || setting.DefaultIntValue < 0 || setting.DefaultIntValue >= options.Count )
                                    break;

                                Buffer.AddBoldLangAndAfterLineItemHeader( "DefaultValue", ColorTheme.DataLabelWhite )
                                    .AddRaw( options[setting.DefaultIntValue].GetOptionValueForDropdown(), ColorTheme.DataBlue ).Line();
                            }
                            break;
                    }

                    setting.AppendNewLineWithExpansionAndOrModThisIsFromAndStatementForTooltipRaw( Buffer,
                        Lang.Get( "SettingAddedBy" ) );
                }
            }
        }
        #endregion

        #region RenderLanguageDropdown
        private void RenderLanguageDropdown( ref float runningY, ArcenUI_SetOfCreateElementDirectives Set, ref bool isFirst, int subCatIndex )
        {
            bool wasFirst = isFirst;
            if ( isFirst && subCatIndex >= 0 )
            {
                isFirst = false;
                //New Section Header
                {
                    runningY += (this.rowHeight + rowBuffer) * 0.5f;
                    Rect nameHeaderBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
                    UIFlow.AddText( "HoverableText", Set, type_tSubSectionHeader, string.Empty, subCatIndex, subCatIndex, -1, nameHeaderBounds,
                        delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                        }, 12f, TextWrapStyle.NoWrap_Ellipsis );
                    runningY += this.rowHeight + rowBuffer;
                }
            }

            if ( !wasFirst )
            {
                UIFlow.AddImage( "SeparationLineControls", Set, type_iGeneric, "CurrentLanguageLabel", 456452, -1, -1,
                    new Rect( 15f, runningY, 0f, 0f ), null );
                runningY += 0.8f + rowBuffer;
            }

            Rect nameBounds = ArcenFloatRectangle.CreateUnityRect( 15, runningY, 490, this.rowHeight );
            UIFlow.AddText( "HoverableTextLight", Set, type_tGenericText, "CurrentLanguageLabel", 456452, -1, -1, nameBounds,
                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    ArcenUI_Text text = element as ArcenUI_Text;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                ExtraData.Buffer.AddLang( "LanguageSetting_Name" );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( GameSettings.CurrentLanguage ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "LanguageSetting_Name" );
                                    novel.TitleUpperLeft.AddRaw( GameSettings.CurrentLanguage.GetDisplayName() );

                                    novel.Main.StartColor( ColorTheme.NarrativeColor );
                                    if ( !GameSettings.CurrentLanguage.GetDescription().IsEmpty() )
                                        novel.Main.AddRaw( GameSettings.CurrentLanguage.GetDescription() ).Line();

                                    novel.Main.AddLang( "SetDefaults_Ignores", ColorTheme.DataBlue ).Line();
                                }
                            }
                            break;
                    }
                }, 12f, TextWrapStyle.NoWrap_Ellipsis );//okay to use here, as it will be consistent per run

            Rect valueSettingControlBounds = ArcenFloatRectangle.CreateUnityRect( nameBounds.xMax, runningY, 170, this.rowHeight );

            valueSettingControlBounds.y += 2;
            valueSettingControlBounds.height -= 4;
            UIFlow.AddDropdown( "DropdownBlue", Set, type_dDropdown, "CurrentLanguageDropdwon", 865437, -1, -1, valueSettingControlBounds,
                delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.DropdownSelectionChanged:
                            {
                                LocalizationType localizationType = ExtraData.DropdownItem as LocalizationType;

                                if ( localizationType != null )
                                {
                                    if ( GameSettings.CurrentLanguage != localizationType )
                                    {
                                        GameSettings.CurrentLanguage = localizationType;
                                        GameSettings.CurrentLanguage.LoadLanguageFile();
                                        GameSettings.SaveToDisk(); //make sure this is not forgotten
                                    }
                                }
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( GameSettings.CurrentLanguage ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "LanguageSetting_Name" );
                                    novel.TitleUpperLeft.AddRaw( GameSettings.CurrentLanguage.GetDisplayName() );

                                    novel.Main.StartColor( ColorTheme.NarrativeColor );
                                    if ( !GameSettings.CurrentLanguage.GetDescription().IsEmpty() )
                                        novel.Main.AddRaw( GameSettings.CurrentLanguage.GetDescription() ).Line();

                                    novel.Main.AddLang( "SetDefaults_Ignores", ColorTheme.DataBlue ).Line();
                                }
                            }
                            break;
                        case UIAction.HandleDropdownItemMouseover:
                            {
                                LocalizationType localizationType = ExtraData.DropdownItem as LocalizationType;
                                if ( localizationType == null )
                                    return;

                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( GameSettings.CurrentLanguage ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.TitleUpperLeft.AddRaw( localizationType.GetDisplayName() );

                                    if ( !localizationType.GetDescription().IsEmpty() )
                                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( localizationType.GetDescription() ).Line();
                                }
                            }
                            break;
                        case UIAction.DropdownOnUpdate:
                            {
                                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)element;
                                if ( elementAsType == null )
                                    return;

                                //int tempValue = setting.TempValue_Int;
                                if ( elementAsType.GetItemCount() != LocalizationTypeTable.Instance.Rows.Length )
                                {
                                    elementAsType.ClearItems();
                                    foreach ( LocalizationType row in LocalizationTypeTable.Instance.Rows )
                                        elementAsType.AddItem( row, row == GameSettings.CurrentLanguage );
                                }
                            }
                            break;
                    }
                } );

            runningY += this.rowHeight + rowBuffer;
        }
        #endregion

        public static ArcenSetting GetSettingForController( ElementAbstractBase controller )
        {
            int tableIndex = (int)controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( tableIndex >= ArcenSettingTable.Instance.Rows.Length )
                return null;
            return ArcenSettingTable.Instance.Rows[tableIndex];
        }

        public static IModOfSomeSort GetXmlModForController( ElementAbstractBase controller )
        {
            int tableIndex = (int)controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
            if ( controller.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag2 == 33 ) //total conversion
            {
                if ( tableIndex >= TotalConversionModTable.Instance.Rows.Length )
                    return null;
                return TotalConversionModTable.Instance.Rows[tableIndex];
            }
            else
            {
                if ( tableIndex >= XmlModTable.Instance.Rows.Length )
                    return null;
                return XmlModTable.Instance.Rows[tableIndex];
            }
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

        private static ButtonAbstractBase.ButtonPool<bCategory> btnCategoryPool;

        public class customParent : CustomUIAbstractBase
        {
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_SettingsMenu.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            btnCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "SettingCategory" );
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

                btnCategoryPool.Clear( 5 );

                List<ArcenSettingCategory> categories = Instance.categories;
                foreach ( ArcenSettingCategory cat in categories )
                {
                    int countCurrent = 0;
                    int countMax = 0;
                    if ( cat.ShowExpansionsList )
                    {
                        foreach ( Expansion exp in ExpansionTable.Instance.Rows )
                        {
                            if ( !exp.IsDisabledBasedOnSettings && exp.IsInstalledAndEnabled && exp.IsInstalledAtAll )
                                countCurrent++;
                            countMax++;
                        }
                    }
                    if ( cat.ShowMainModsList )
                    {
                        foreach ( XmlMod mod in XmlModTable.Instance.Rows )
                        {
                            if ( !mod.IsDisabledBasedOnSettings )
                                countCurrent++;
                            countMax++;
                        }

                        foreach ( TotalConversionMod mod in TotalConversionModTable.Instance.Rows )
                        {
                            if ( mod.GetIsEnabled() )
                                countCurrent++;
                            countMax++;
                        }
                    }

                    bCategory item = btnCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( cat, countCurrent, countMax );
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

            private ArcenSettingCategory Category = null;
            private int CountCurrent;
            private int CountOutOf;

            public void Assign( ArcenSettingCategory Cat, int CountCurrent, int CountOutOf )
            {
                this.Category = Cat;
                this.CountCurrent = CountCurrent;
                this.CountOutOf = CountOutOf;
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

                bool isSelected = Instance.CurrentCategory == this.Category;
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                buffer.StartColor( isSelected ? ColorTheme.GetCategoryWhite( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                buffer.AddRaw( this.Category.GetDisplayName() );
                if ( this.CountOutOf > 0 )
                    buffer.Space1x().StartSize90().AddFormat2( LangCommon.ParentheticalOutOF, this.CountCurrent, this.CountOutOf ).EndSize();
                this.Category.AppendSecondLineWithExpansionAndOrModThisIsFrom( buffer );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                Instance.CurrentCategory = this.Category;
                Instance.isTempShowingAdvanced = false;
                //scroll right panel back to top when change category
                if ( bMainContentParent.Instance != null )
                    bMainContentParent.Instance.Element.TryScrollToTop();
                return MouseHandlingResult.None;
            }
        }
        #endregion

        public class tGenericText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            { 
            }
        }

        public class iGeneric : ImageAbstractBase
        {
        }


        public class bGenericToggle : ButtonAbstractBase
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return MouseHandlingResult.None;                               
            }
        }

        public class tXmlModName : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return;

                buffer.AddRaw( "<size=60%>" ).AddLangAndAfterLineItemHeader( mod.GetIsTotalConversion() ? "SettingsTotalConversionAbbreviation" : "SettingsModAbbreviation" )
                    .AddRaw( "</size> <pos=20>" ).StartColor( mod.GetColorForDisplay() ).StartSize90().AddRaw( mod.GetDisplayName() );
                if ( mod.GetAbbreviation().Length > 0 )
                    buffer.AddRaw( " </size>(" ).AddRaw( mod.GetAbbreviation() ).AddRaw( ")" );
                buffer.EndColor();
                buffer.AddRaw( "\n<pos=20><size=70%> " ).AddFormat1( "SettingsModAuthor", mod.GetAuthor() ).AddRaw( " </size>" );
            }

            public override void HandleMouseover()
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return;
                if ( mod.GetDisplayName().Length > 0 )
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Mod", mod.GetDisplayName() ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( mod.GetIsTotalConversion() ? "SettingsTotalConversionFull" : "SettingsModFull" );
                        novel.TitleUpperLeft.AddRaw( mod.GetDisplayName(), mod.GetColorForDisplay() );
                        if ( mod.GetAbbreviation().Length > 0 )
                            novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( mod.GetAbbreviation() ).AddRaw( ")" );

                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddFormat1( "SettingsModAuthor", mod.GetAuthor() ).Line();

                        if( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                        {
                            novel.Main.AddRaw( mod.GetDescription().Substring( 0, MAX_MOD_DESCRIPTION_LENGTH ) )
                                .AddNeverTranslated( "...\n\n", true ).AddLang( "LongDescriptionCutoff" );
                        }
                        else
                            novel.Main.AddRaw( mod.GetDescription() );
                    }
                }
            }
        }

        public class tXmlModDisabledDuringGame : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return;

                if ( mod.GetIsTotalConversion() )
                {
                    TotalConversionMod tcMod = mod as TotalConversionMod;

                    buffer.AddLang( tcMod.GetIsEnabled() ? "Active" : "Inactive",
                        tcMod.GetIsEnabled() ? string.Empty : "888888" );
                }
                else
                {
                    XmlMod xMod = mod as XmlMod;
                    ArcenSetting setting = xMod.GetRelatedSetting();

                    if ( xMod.IsOffByDefault )
                        buffer.AddLang( setting.TempValue_Bool ? "Enabled" : "Disabled", setting.TempValue_Bool ? string.Empty : "666666" );
                    else
                        buffer.AddLang( !setting.TempValue_Bool ? "Enabled" : "Disabled", !setting.TempValue_Bool ? string.Empty : "666666" );
                }
            }

            public override void HandleMouseover()
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) 
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Mod", mod.GetDisplayName() ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( mod.GetIsTotalConversion() ? "SettingsTotalConversionFull" : "SettingsModFull" );
                    novel.TitleUpperLeft.AddRaw( mod.GetDisplayName(), mod.GetColorForDisplay() );
                    if ( mod.GetAbbreviation().Length > 0 )
                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( mod.GetAbbreviation() ).AddRaw( ")" );

                    novel.Main.AddLang( "ModTooltipRestartBlocked" ).Line();

                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddFormat1( "SettingsModAuthor", mod.GetAuthor() ).Line();

                    if ( mod.GetDescription().Length > MAX_MOD_DESCRIPTION_LENGTH )
                    {
                        novel.Main.AddRaw( mod.GetDescription().Substring( 0, MAX_MOD_DESCRIPTION_LENGTH ) )
                            .AddNeverTranslated( "...\n\n", true ).AddLang( "LongDescriptionCutoff" );
                    }
                    else
                        novel.Main.AddRaw( mod.GetDescription() );
                }
            }
        }

        public class bXmlModToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return;

                if ( mod.GetIsTotalConversion() )
                {
                    TotalConversionMod tcMod = mod as TotalConversionMod;

                    buffer.AddLang( tcMod.GetIsEnabled() ? "TotalConversionDisable" : "TotalConversionEnable",
                        tcMod.GetIsEnabled() ? "e6b490" : "e6e490" );
                }
                else
                {
                    XmlMod xMod = mod as XmlMod;
                    ArcenSetting setting = xMod.GetRelatedSetting();

                    if ( xMod.IsOffByDefault )
                        buffer.AddLang( setting.TempValue_Bool ? "Enabled" : "Disabled", setting.TempValue_Bool ? string.Empty : "666666" );
                    else
                        buffer.AddLang( !setting.TempValue_Bool ? "Enabled" : "Disabled", !setting.TempValue_Bool ? string.Empty : "666666" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return MouseHandlingResult.None;

                if ( mod.GetIsTotalConversion() )
                {
                    TotalConversionMod tcMod = mod as TotalConversionMod;

                    if ( tcMod.RequiredExpansions.Count > 0 && !tcMod.GetIsEnabled() ) //only check for dlcs when enabling, not when disabling
                    {
                        foreach ( Expansion exp in tcMod.RequiredExpansions )
                        {
                            if ( exp != null )
                            {
                                if ( !exp.IsInstalledAtAll )
                                {
                                    string expansionList = string.Empty;
                                    foreach ( Expansion exp2 in tcMod.RequiredExpansions )
                                    {
                                        if ( expansionList.Length > 0 )
                                            expansionList += ", ";
                                        expansionList += exp2.GetDisplayName();

                                        if ( !exp2.IsInstalledAtAll )
                                            expansionList += " " + Lang.Get( "ExpansionOrMod_NotInstalled" );
                                        else
                                        {
                                            ArcenSetting set2 = exp2.GetRelatedSetting();
                                            if ( set2.TempValue_Bool )
                                                expansionList += " " + Lang.Get( "ExpansionOrMod_NotEnabled" );
                                        }
                                    }

                                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddLang_New( "Popup_CannotEnableMod" ),
                                        LocalizedString.AddFormat1_New( "Popup_ExpansionsMissing", expansionList ), LangCommon.Popup_Common_Ok.LocalizedString );
                                    return MouseHandlingResult.PlayClickDeniedSound;
                                }

                                ArcenSetting set = exp.GetRelatedSetting();
                                if ( set.TempValue_Bool ) //bool on means disabled
                                {
                                    string expansionList = string.Empty;
                                    foreach ( Expansion exp2 in tcMod.RequiredExpansions )
                                    {
                                        if ( expansionList.Length > 0 )
                                            expansionList += ", ";
                                        expansionList += exp2.GetDisplayName();

                                        if ( !exp2.IsInstalledAtAll )
                                            expansionList += " (Not Installed)";
                                        else
                                        {
                                            ArcenSetting set2 = exp2.GetRelatedSetting();
                                            if ( set2.TempValue_Bool )
                                                expansionList += " (Not Enabled)";
                                        }
                                    }

                                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddLang_New( "Popup_CannotEnableMod" ),
                                        LocalizedString.AddFormat1_New( "Popup_ExpansionsMissing", expansionList ), LangCommon.Popup_Common_Ok.LocalizedString );
                                    return MouseHandlingResult.PlayClickDeniedSound;
                                }
                            }
                        }
                    }

                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, delegate
                    {
                        string totalConversionFile = Engine_Universal.CurrentRootApplicationDirectory + "PlayerData/" + "TotalConversionModOption.txt";

                        if ( ArcenIO.FileExists( totalConversionFile ) )
                            ArcenIO.DeleteFile( totalConversionFile );

                        if ( tcMod.GetIsEnabled() )
                        {
                            ArcenIO.AppendAllText( totalConversionFile, "//" + tcMod.ID );
                        }
                        else
                        {
                            ArcenIO.AppendAllText( totalConversionFile, tcMod.ID );
                        }

                        Application.Quit();

                    }, null, tcMod.GetIsEnabled() ? LocalizedString.AddLang_New( "TotalConversion_End" ) : LocalizedString.AddLang_New( "TotalConversion_Start" ),
                        tcMod.GetIsEnabled() ? LocalizedString.AddLang_New( "TotalConversion_End_Details" ) : LocalizedString.AddLang_New( "TotalConversion_Start_Details" ),
                        LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                    //ArcenSetting setting = mod.GetRelatedSetting();

                    //setting.TempValue_Bool = !setting.TempValue_Bool;
                }
                else
                {
                    XmlMod xMod = mod as XmlMod;

                    ArcenSetting setting = xMod.GetRelatedSetting();

                    if ( xMod.IsOffByDefault ? !setting.TempValue_Bool : setting.TempValue_Bool ) //if enabling
                    {
                        if ( xMod.RequiredExpansions.Count > 0 )
                        {
                            foreach ( Expansion exp in xMod.RequiredExpansions )
                            {
                                if ( exp != null )
                                {
                                    if ( !exp.IsInstalledAtAll )
                                    {
                                        string expansionList = string.Empty;
                                        foreach ( Expansion exp2 in xMod.RequiredExpansions )
                                        {
                                            if ( expansionList.Length > 0 )
                                                expansionList += ", ";
                                            expansionList += exp2.GetDisplayName();

                                            if ( !exp2.IsInstalledAtAll )
                                                expansionList += " (Not Installed)";
                                            else
                                            {
                                                ArcenSetting set2 = exp2.GetRelatedSetting();
                                                if ( set2.TempValue_Bool )
                                                    expansionList += " (Not Enabled)";
                                            }
                                        }
                                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddLang_New( "Popup_CannotEnableMod" ),
                                            LocalizedString.AddFormat1_New ( "Popup_ExpansionsMissing", expansionList ), LangCommon.Popup_Common_Ok.LocalizedString );
                                        return MouseHandlingResult.PlayClickDeniedSound;
                                    }

                                    ArcenSetting set = exp.GetRelatedSetting();
                                    if ( set.TempValue_Bool ) //bool on means disabled
                                    {
                                        string expansionList = string.Empty;
                                        foreach ( Expansion exp2 in xMod.RequiredExpansions )
                                        {
                                            if ( expansionList.Length > 0 )
                                                expansionList += ", ";
                                            expansionList += exp2.GetDisplayName();

                                            if ( !exp2.IsInstalledAtAll )
                                                expansionList += " (Not Installed)";
                                            else
                                            {
                                                ArcenSetting set2 = exp2.GetRelatedSetting();
                                                if ( set2.TempValue_Bool )
                                                    expansionList += " (Not Enabled)";
                                            }
                                        }

                                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddLang_New( "Popup_CannotEnableMod" ),
                                            LocalizedString.AddFormat1_New( "Popup_ExpansionsMissing", expansionList ), LangCommon.Popup_Common_Ok.LocalizedString );
                                        return MouseHandlingResult.PlayClickDeniedSound;
                                    }
                                }
                            }
                        }
                    }

                    setting.TempValue_Bool = !setting.TempValue_Bool;
                }
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Mod", mod.GetDisplayName() ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( mod.GetIsTotalConversion() ? "SettingsTotalConversionFull" : "SettingsModFull" );
                    novel.TitleUpperLeft.AddRaw( mod.GetDisplayName(), mod.GetColorForDisplay() );
                    if ( mod.GetAbbreviation().Length > 0 )
                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( mod.GetAbbreviation() ).AddRaw( ")" );

                    novel.Main.AddLang( "ModTooltipRestartBlocked" ).Line();

                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddFormat1( "SettingsModAuthor", mod.GetAuthor() ).Line();

                    novel.Main.AddLang( "ModDetails_HoverToLeft" ).Line();

                    if ( mod.GetIsTotalConversion() )
                    {
                        novel.Main.AddLang( "TotalConversionTooltipRestartNotesP1" );
                        novel.Main.Line().AddLang( "TotalConversionTooltipRestartNotesP2" );
                        novel.Main.Line().AddLang( "TotalConversionTooltipRestartNotesP3" );
                    }
                    else
                        novel.Main.AddLang( "ModTooltipRestartNotes" );
                }
            }
        }

        public class bXmlModMore : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                buffer.AddLang( "ReadMore" );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null ) return MouseHandlingResult.None;

                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.TallWide, null,
                    LocalizedString.AddRaw_New( mod.GetDisplayName() ), LocalizedString.AddRaw_New( mod.GetDescription() ), LangCommon.Popup_Common_Ok.LocalizedString );

                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                IModOfSomeSort mod = GetXmlModForController( this );
                if ( mod == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Mod", mod.GetDisplayName() ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( mod.GetIsTotalConversion() ? "SettingsTotalConversionFull" : "SettingsModFull" );
                    novel.TitleUpperLeft.AddRaw( mod.GetDisplayName(), mod.GetColorForDisplay() );
                    if ( mod.GetAbbreviation().Length > 0 )
                        novel.TitleUpperLeft.AddRaw( " (" ).AddRaw( mod.GetAbbreviation() ).AddRaw( ")" );

                    novel.Main.AddLang( "ModIsVerbose" ).Line();

                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddFormat1( "SettingsModAuthor", mod.GetAuthor() ).Line();

                    novel.Main.AddLang( "ReadMoreInstructions" );
                }
            }
        }


        public class tSubSectionHeader : TextAbstractBase
        {
            public ArcenSettingSubcategory GetSubcategory()
            {
                int subCategoryIndex = (int)this.Element.CreatedByCodeDirective.Identifier.CodeDirectiveTag1;
                ArcenSettingCategory category = Window_SettingsMenu.Instance.CurrentCategory;
                if ( subCategoryIndex >= 0 && subCategoryIndex < category.Subcategories.Count )
                    return category.Subcategories[subCategoryIndex];
                return null;
            }
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSettingSubcategory subCat = this.GetSubcategory();
                if ( subCat == null )
                    buffer.StartSize125().AddNeverTranslated( "Null subcategory!", true ); //doesn't need translation, error only
                else
                    buffer.StartSize125().AddRaw( subCat.GetDisplayName() );
            }

            public override void HandleMouseover()
            {
                
            }
        }

        public class tSettingValueDescription : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                switch ( setting.Type )
                {
                    case ArcenSettingType.BoolToggle:
                        break;
                    case ArcenSettingType.FloatSlider:
                        {
                            buffer.Space2x();
                            float tempValue = setting.TempValue_Float;
                            string minValueString;
                            string maxValueString;
                            switch ( setting.RoundingType )
                            {
                                case ArcenSettingRoundingType.Tenths:
                                    buffer.AddRaw( tempValue.ToStringSmallFixedDecimal( 1 ) );
                                    minValueString = setting.MinFloatValue.ToStringSmallFixedDecimal( 1 );
                                    maxValueString = setting.MaxFloatValue.ToStringSmallFixedDecimal( 1 );
                                    break;
                                case ArcenSettingRoundingType.Twentieths:
                                    buffer.AddRaw( tempValue.ToStringSmallFixedDecimal( 2 ) );
                                    minValueString = setting.MinFloatValue.ToStringSmallFixedDecimal( 2 );
                                    maxValueString = setting.MaxFloatValue.ToStringSmallFixedDecimal( 2 );
                                    break;
                                case ArcenSettingRoundingType.None:
                                default:
                                    buffer.AddRaw( tempValue.ToStringThousandsDecimal_Optional4() );
                                    minValueString = setting.MinFloatValue.ToStringThousandsDecimal_Optional4();
                                    maxValueString = setting.MaxFloatValue.ToStringThousandsDecimal_Optional4();
                                    break;
                            }
                            buffer.Space2x().StartSize80().StartColor( "7994b8" ).AddFormat2( "SliderRange", minValueString, maxValueString );
                        }
                        break;
                    case ArcenSettingType.IntSlider:
                        {
                            buffer.Space2x();
                            int tempValue = setting.TempValue_Int;
                            buffer.AddRaw( tempValue.ToStringThousandsWhole() );
                            buffer.Space2x().StartSize80().StartColor( "7994b8" ).AddFormat2( "SliderRange", setting.MinIntValue.ToString(), setting.MaxIntValue.ToStringThousandsWhole() );
                        }
                        break;
                    case ArcenSettingType.IntTextbox:
                        int valueAsInt;
                        if ( !Int32.TryParse( setting.TempValue_String, out valueAsInt ) )
                            buffer.Space1x().AddLang( "IntTextbox_MustBeInteger" );
                        else if ( valueAsInt < setting.MinIntValue )
                            buffer.Space1x().AddFormat1( "IntTextbox_MustBeAtLeast", setting.MinIntValue.ToString() );
                        else if ( setting.MaxIntValue > 0 && setting.MaxIntValue > setting.MinIntValue && valueAsInt > setting.MaxIntValue )
                            buffer.Space1x().AddFormat1( "IntTextbox_MustBeAtMost", setting.MaxIntValue.ToString() );
                        break;
                }
            }
        }

        public class bToggle : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[setting.TempValue_Bool ? 0 : 1] );

                buffer.AddLang( "On", setting.TempValue_Bool ? string.Empty : "63647F" );

            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer, int OtherTextIndex )
            {
                if ( OtherTextIndex == 0 )
                {
                    ArcenSetting setting = GetSettingForController( this );
                    if ( setting == null ) return;

                    buffer.AddLang( "Off", !setting.TempValue_Bool ? string.Empty : "63647F" );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return MouseHandlingResult.None;

                setting.TempValue_Bool = !setting.TempValue_Bool;
                return MouseHandlingResult.None;
            }
        }

        public class iIntInput : InputAbstractBase
        {
            public override void OnEndEdit()
            {
                ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                string newValue = elementAsType.GetText();

                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;
                int newValueAsInt;
                if ( !Int32.TryParse( newValue, out newValueAsInt ) )
                    return;
                if ( newValueAsInt < setting.MinIntValue )
                    return;
                if ( setting.MaxIntValue > setting.MinIntValue && newValueAsInt > setting.MaxIntValue )
                    return;

                setting.TempValue_String = newValue;
                setting.MarkAsCustomPresetIfJustOverrideValue();
            }

            public override InputActionTextboxResult OnInputActionOfSpecificSort( InputActionTypeData Action )
            {
                switch ( Action.ID )
                {
                    case "Cancel": //escape key
                    case "Return": //enter key
                        return InputActionTextboxResult.UnfocusMe;
                }
                return InputActionTextboxResult.DoNothingFurther;
            }

            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                //only update to the current value if we're not editing this field right now
                if ( !this.GetIsCurrentlyBeingEdited() )
                {
                    ArcenUI_Input elementAsType = (ArcenUI_Input)this.Element;
                    elementAsType.SetText( setting.TempValue_String );
                }
            }
        }

        public class sFloatSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float currentValue = setting.TempValue_Float;
                float range = setting.MaxFloatValue - setting.MinFloatValue;
                if ( range == 0 ) range = 1;
                float currentPortion = (currentValue - setting.MinFloatValue) / range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                float range = setting.MaxFloatValue - setting.MinFloatValue;
                float adjustedNewValue = setting.MinFloatValue + (range * NewValue);

                setting.TempValue_Float = adjustedNewValue;
                setting.MarkAsCustomPresetIfJustOverrideValue();
            }
        }

        public class sIntSlider : SliderAbstractBase
        {
            public override void OnUpdate()
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int currentValue = setting.TempValue_Int;
                int range = setting.MaxIntValue - setting.MinIntValue;
                if ( range == 0 ) range = 1;
                float currentPortion = (float)(currentValue - setting.MinIntValue) / (float)range;

                ArcenUI_Slider elementAsType = (ArcenUI_Slider)this.Element;
                elementAsType.ReferenceSlider.value = currentPortion;
            }

            public override void OnChange( float NewValue )
            {
                ArcenSetting setting = GetSettingForController( this );
                if ( setting == null ) return;

                int range = setting.MaxIntValue - setting.MinIntValue;
                int adjustedNewValue = setting.MinIntValue + Mathf.RoundToInt( range * NewValue );

                setting.TempValue_Int = adjustedNewValue;
                setting.MarkAsCustomPresetIfJustOverrideValue();
            }
        }

        public class dDropdown : DropdownAbstractBase
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
                bool willRequireReload = false;
                //Enabled Expansions
                for ( int i = 0; i < ExpansionTable.Instance.Rows.Length; i++ )
                {
                    Expansion expansion = ExpansionTable.Instance.Rows[i];
                    ArcenSetting setting = expansion.GetRelatedSetting();
                    if ( setting.TempValue_Bool != GameSettings.Current.GetBool( setting ) )
                    {
                        willRequireReload = true;
                        break;
                    }
                }

                //Mods
                for ( int i = 0; i < XmlModTable.Instance.Rows.Length; i++ )
                {
                    XmlMod mod = XmlModTable.Instance.Rows[i];
                    ArcenSetting setting = mod.GetRelatedSetting();
                    if ( setting.TempValue_Bool != GameSettings.Current.GetBool( setting ) )
                    {
                        willRequireReload = true;
                        break;
                    }
                }

                if ( willRequireReload )
                {
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, delegate
                    {
                        if ( Engine_Universal.IsReloadingAllXml )
                            return; //we clicked it a second time somehow

                        //save the settings
                        ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                        GameSettings.SaveToDisk();
                        GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;

                        //force the windows here to close, rather ungracefully
                        Window_SettingsMenu.Instance.IsOpen = false;
                        ArcenUI.Instance.OnMainThreadUpdate();
                        ArcenUI.Instance.OnUpdateUIFromMainThread_Normal();

                        //do the hot-reload
                        Engine_Universal.ReloadXmlDataAsMuchAsIsAllowed();

                    }, null, LocalizedString.AddLang_New( "ReloadDataFiles_Header" ),
                        LocalizedString.AddLang_New( "ReloadDataFiles_Details" ),
                        LocalizedString.AddLang_New( "ReloadDataFiles_Continue" ), LangCommon.Popup_Common_NoWait.LocalizedString );
                }
                else
                {
                    ArcenSettingTable.Instance.CopyTempValuesToCurrent();
                    GameSettings.SaveToDisk();
                    GameSettings.Current.DoGraphicsSettingsNeedARefresh = true;
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                }
                return MouseHandlingResult.None;
            }
        }

        public static void SetCurrentPresetValues()
        {
            QualityPreset currentPreset;
            {
                int index = ArcenSettingTable.Instance.GetRowByID( "QualityPreset" ).TempValue_Int;
                if ( index < 0 )
                    index = 0;
                if ( index >= QualityPresetTable.Instance.Rows.Length )
                    index = 0;
                currentPreset = QualityPresetTable.Instance.Rows[index];
            }

            if ( currentPreset != null )
                currentPreset.ApplyPresetNow();
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
                if ( Instance.ShowOnlyCategory.Length > 0 ) //if only showing a single category, then just do it for that
                    SetDefaults( Instance.ShowOnlyCategory );
                else
                {
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, delegate
                    {
                        SetDefaults( Instance.CurrentCategory.ID );
                    },
                    delegate
                    {
                        SetDefaults( string.Empty );
                    },
                    LocalizedString.AddLang_New( "Settings_ResetDefaults_Header" ),
                    LocalizedString.AddLang_New( "Settings_ResetDefaults_Body" ),
                    LocalizedString.AddLang_New( "Settings_ResetDefaults_CurrentTab" ), LocalizedString.AddLang_New( "Settings_ResetDefaults_AllTabs" ) );
                }

                return MouseHandlingResult.None;
            }

            private static void SetDefaults( string ForCategory )
            {
                QualityPreset currentPreset;
                {
                    int index = ArcenSettingTable.Instance.GetRowByID( "QualityPreset" ).TempValue_Int;
                    if ( index < 0 )
                        index = 0;
                    if ( index >= QualityPresetTable.Instance.Rows.Length )
                        index = 0;
                    currentPreset = QualityPresetTable.Instance.Rows[index];
                }

                int rowCount = ArcenSettingTable.Instance.VisibleRows_All.Count;
                for ( int i = 0; i < rowCount; i++ )
                {
                    ArcenSetting setting = ArcenSettingTable.Instance.VisibleRows_All[i];
                    if ( setting.RowFromExpansion != null && setting.RowFromExpansion.IsDisabledBasedOnSettings )
                        continue; //if from disabled expansion
                    if ( setting.RowFromXmlMod != null && setting.RowFromXmlMod.IsDisabledBasedOnSettings )
                        continue; //if from disabled mod
                    if ( ForCategory.Length > 0 )
                    {
                        if ( setting.ParentCategory.ID != ForCategory )
                            continue;
                    }

                    if ( currentPreset != null )
                    {
                        switch ( setting.Type )
                        {
                            case ArcenSettingType.BoolToggle:
                                if ( currentPreset.Bools.TryGetValue( setting.ID, out bool presetBool ) )
                                {
                                    setting.TempValue_Bool = presetBool;
                                    continue;
                                }
                                break;
                            case ArcenSettingType.FloatSlider:
                                if ( currentPreset.Floats.TryGetValue( setting.ID, out float presetFloat ) )
                                {
                                    setting.TempValue_Float = presetFloat;
                                    continue;
                                }
                                break;
                            case ArcenSettingType.IntSlider:
                                if ( currentPreset.Ints.TryGetValue( setting.ID, out int presetInt1 ) )
                                {
                                    setting.TempValue_Int = presetInt1;
                                    continue;
                                }
                                break;
                            case ArcenSettingType.IntTextbox:
                                if ( currentPreset.Ints.TryGetValue( setting.ID, out int presetInt2 ) )
                                {
                                    setting.TempValue_Int = presetInt2;
                                    setting.TempValue_String = presetInt2.ToString();
                                    continue;
                                }
                                break;
                            case ArcenSettingType.IntDropdown:
                                {
                                    if ( currentPreset.Ints.TryGetValue( setting.ID, out int presetInt3 ) )
                                    {
                                        setting.TempValue_Int = presetInt3;
                                        continue;
                                    }
                                    if ( currentPreset.Strings.TryGetValue( setting.ID, out string presetString1 ) )
                                    {
                                        if ( setting.DropdownFiller != null )
                                        {
                                            List<IArcenDropdownOption> list = setting.DropdownFiller.GetListOfDropdownOptions();
                                            int index = 0;
                                            bool useFoundIndex = false;
                                            foreach ( IArcenDropdownOption item in list )
                                            {
                                                if ( item.GetStringForDropdownMatch() == presetString1 )
                                                {
                                                    useFoundIndex = true;
                                                    break;
                                                }
                                                index++;
                                            }
                                            if ( useFoundIndex )
                                                setting.TempValue_Int = index;
                                            continue;
                                        }
                                    }
                                }
                                break;
                            default:
                                if ( currentPreset.Strings.TryGetValue( setting.ID, out string presetString2 ) )
                                {
                                    setting.TempValue_String = presetString2;
                                    continue;
                                }
                                break;
                        }
                    }

                    if ( setting.IgnoresDefaults )
                        continue;

                    switch ( setting.Type )
                    {
                        case ArcenSettingType.BoolToggle:
                            setting.TempValue_Bool = setting.DefaultBoolValue;
                            break;
                        case ArcenSettingType.FloatSlider:
                            setting.TempValue_Float = setting.DefaultFloatValue;
                            break;
                        case ArcenSettingType.IntDropdown:
                        case ArcenSettingType.IntSlider:
                            setting.TempValue_Int = setting.DefaultIntValue;
                            break;
                        case ArcenSettingType.IntTextbox:
                            setting.TempValue_Int = setting.DefaultIntValue;
                            setting.TempValue_String = setting.DefaultIntValue.ToString();
                            break;
                        default:
                            setting.TempValue_String = setting.DefaultStringValue;
                            break;
                    }
                }
            }
        }

        public override void Close( WindowCloseReason Reason )
        {
            ArcenSettingTable.Instance.CopyCurrentValuesToTemp(); // shouldn't matter, but tidies things up
            base.Close( Reason );
        }

        //from IInputActionHandler
        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    this.Close( WindowCloseReason.UserDirectRequest );
                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                    ArcenInput.BlockForAJustPartOfOneSecond();
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

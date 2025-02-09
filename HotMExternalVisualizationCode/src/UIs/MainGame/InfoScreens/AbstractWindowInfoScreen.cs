using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public abstract class AbstractWindowInfoScreen<CatType,DatType> : ToggleableWindowController, IInputActionHandler 
        where 
        CatType : class, UIXExaminedCategory
        where DatType : class, UIXExaminedDataItem
    {
        public static AbstractWindowInfoScreen<CatType, DatType> InstanceG;

        public CatType CurrentRowType;

        protected static UIXDisplayItem<DatType> ChosenDisplayStyle = null;
        protected static UIXSortFilterItem<DatType> ChosenSortAndFilter = null;

        protected static readonly List<UIXDisplayItem<DatType>> CurrentValidDisplayItems = List<UIXDisplayItem<DatType>>.Create_WillNeverBeGCed( 200, "AbstractWindowInfoScreen<CatType,DatType>-CurrentValidDisplayItems" );
        protected static readonly List<UIXSortFilterItem<DatType>> CurrentValidSortFilterItems = List<UIXSortFilterItem<DatType>>.Create_WillNeverBeGCed( 200, "AbstractWindowInfoScreen<CatType,DatType>-CurrentValidSortFilterItems" );

        #region OnOpen
        public sealed override void OnOpen()
        {
            this.priorType = null;
            if ( this.CurrentRowType == null )
            {
                ITableLikeDataSource<CatType> table = this.GetTable();
                this.CurrentRowType = table.GetDefaultRowOrFirstRow();
            }

            if ( bCategoryBase.Original != null ) //scroll left panel back to top when opening
                bCategoryBase.Original.Element.TryScrollToTop();

            if ( !AbstractWindowManager.AbstractInfoWindows.Contains( this ) )
                AbstractWindowManager.AbstractInfoWindows.Add( this );

            //when this one opens, close any others of this that might be open
            foreach ( ToggleableWindowController window in AbstractWindowManager.AbstractInfoWindows )
            {
                if ( window == this )
                    continue;
                if ( window.IsOpen )
                    window.Close( WindowCloseReason.OtherWindowCausingClose );
            }
        }
        #endregion

        public sealed override void OnClose( WindowCloseReason CloseReason ) { }

        protected abstract ITableLikeDataSource<CatType> GetTable();
        protected abstract UIXDisplayTable<DatType> GetUIXDisplayTable();
        protected abstract UIXSortFilterTable<DatType> GetUIXSortFilterTable();
        protected abstract void GetHeaderText( ArcenDoubleCharacterBuffer Buffer );

        private readonly List<DatType> dataItemsInType = List<DatType>.Create_WillNeverBeGCed( 400, "AbstractWindowInfoScreen<CatType,DatType>-dataItemsInType" );

        #region RecalculateDataItemsInCategory
        private CatType priorType = null;
        private UIXSortFilterItem<DatType> lastSortAndFilter = null;
        private float lastTimeRecalculated = 0;
        private void RecalculateDataItemsInCategory()
        {
            if ( priorType == this.CurrentRowType &&
                lastSortAndFilter == ChosenSortAndFilter &&
                ( ArcenTime.AnyTimeSinceStartF - lastTimeRecalculated ) < 4f ) //even if nothing has changed, recalculate it every 4 seconds
                return;
            this.priorType = CurrentRowType;
            this.lastSortAndFilter = ChosenSortAndFilter;

            dataItemsInType.Clear();
            this.RecalculateDataItemsInCategory_Inner();

            if ( ChosenSortAndFilter != null )
                ChosenSortAndFilter.GetImplementation().SortList_UIXSortAndFilter( dataItemsInType, ChosenSortAndFilter );

            lastTimeRecalculated = ArcenTime.AnyTimeSinceStartF;
        }
        #endregion

        #region PassesFilter
        protected bool PassesFilter( DatType Item )
        {
            if ( ChosenSortAndFilter != null )
            {
                if ( !ChosenSortAndFilter.GetImplementation().ShouldBeShown_UIXSortAndFilter( Item, ChosenSortAndFilter ) )
                    return false;
            }
            return true;
        }
        #endregion

        #region AddToDataItemListInCategory
        protected bool AddToDataItemListInCategory( DatType Item )
        {
            if ( Item == null )
                return false;

            if ( ChosenSortAndFilter != null )
            {
                if ( !ChosenSortAndFilter.GetImplementation().ShouldBeShown_UIXSortAndFilter( Item, ChosenSortAndFilter ) )
                    return false;
            }

            dataItemsInType.Add( Item );
            return true;
        }
        #endregion

        protected abstract void RecalculateDataItemsInCategory_Inner();
        protected abstract bool CalculateIsAllowedToShowCategory( CatType Category );

        #region tHeaderTextBase
        public abstract class tHeaderTextBase : TextAbstractBase
        {
            public sealed override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                InstanceG.GetHeaderText( Buffer );
            }
        }
        #endregion

        #region PopulateFreeFormControls
        private static int lastNumberOfCompleteClears = 0;

        public sealed override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
        {
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
            {
                this.Close( WindowCloseReason.ShowingRefused );
                return;
            }
            if ( bMainContentParentBase.ParentT == null )
                return;
            this.Window.SetOverridingTransformToWhichToAddChildren( bMainContentParentBase.ParentT );

            if ( Engine_HotM.NumberOfCompleteClears != lastNumberOfCompleteClears )
            {
                Set.RefreshAllElements = true;
                lastNumberOfCompleteClears = Engine_HotM.NumberOfCompleteClears;
                return;
            }
        }
        #endregion

        #region bMainContentParentBase
        public class bMainContentParentBase : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public static bMainContentParentBase Instance;
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
        #endregion

        private static ButtonAbstractBase.ButtonPool<bCategoryBase> bCategoryPool;
        private static ButtonAbstractBase.ButtonPool<bDataItemBase> bDataItemBasePool;

        private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
        private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load

        public abstract class customParentBase : CustomUIAbstractBase
        {
            protected float rowHeight = 25.7f;
            //protected float rowBuffer = 1.5f;

            #region CalculateBoundsSingle
            protected void CalculateBoundsSingle( out Rect soleBounds, ref float runningY, float SoleWidth )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( 5f, runningY, SoleWidth, rowHeight );

                runningY -= rowHeight;// + rowBuffer;
            }
            #endregion

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( InstanceG != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        if ( bCategoryBase.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategoryBase>( bCategoryBase.Original, 10, typeof(CatType) + "Category" );
                            bDataItemBasePool = new ButtonAbstractBase.ButtonPool<bDataItemBase>( bDataItemBase.Original, 10, typeof( CatType ) + "Worker" );
                        }
                    }
                    #endregion
                }

                if ( !hasGlobalInitialized )
                    return;

                bDataItemBasePool.Clear( 5 );

                InstanceG.RecalculateDataItemsInCategory();

                List<DatType> dataItems = InstanceG.dataItemsInType;

                RectTransform rTran = (RectTransform)bDataItemBase.Original.Element.RelevantRect.parent;
                //float minYToShow = bMainContentParentBase.Instance.Element.GetRelevantRect().anchoredPosition.y;
                //float maxYToShow = minYToShow + MAX_VIEWPORT_SIZE + EXTRA_BUFFER;
                //minYToShow -= EXTRA_BUFFER;

                float maxYToShow = -rTran.anchoredPosition.y;
                float minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                maxYToShow += EXTRA_BUFFER;

                float runningY = -2.55f; //position of the first entry
                Rect leftBounds;

                float maxyShown = 0;

                int dataItemsHadInCurrentCategory = 0;
                foreach ( DatType dataItem in dataItems )
                {
                    this.CalculateBoundsSingle( out leftBounds, ref runningY, 809.9f );
                    dataItemsHadInCurrentCategory++;

                    if ( leftBounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( leftBounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    maxyShown = leftBounds.yMax;

                    bDataItemBase item = bDataItemBasePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( dataItem );
                    bDataItemBasePool.ApplySingleItemInRow( item, leftBounds, false );
                }

                this.OnUpdateCategories( dataItemsHadInCurrentCategory );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( runningY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            public void OnUpdateCategories( int DataItemsHadInCurrentCategory )
            {
                float currentY = -5; //the position of the first entry

                if ( !hasGlobalInitialized )
                    return;

                bCategoryPool.Clear( 5 );

                System.Collections.Generic.IEnumerable<CatType> rows = InstanceG.GetTable().GetRows();
                foreach ( CatType category in rows )
                {
                    if ( !InstanceG.CalculateIsAllowedToShowCategory( category ) )
                        continue; //skip any that don't match the style

                    if ( DataItemsHadInCurrentCategory <= 0 ) //if we had no items, then switch to the first one with items
                    {
                        InstanceG.CurrentRowType = category;
                        DataItemsHadInCurrentCategory = 1;
                    }

                    bCategoryBase item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break; //time slicing, too many added right now
                    item.Assign( category );
                }

                //ArcenDebugging.LogSingleLine( "Folders: " + folders.Count + " addedCount: " + addedCount, Verbosity.ShowAsError );

                #region Positioning Logic 1
                bCategoryPool.ApplyItemsInRows( 6.1f, ref currentY, 27f, 218.5f, 25.6f, false );
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategoryBase.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }
        }

        #region bCategoryBase
        public abstract class bCategoryBase : ButtonAbstractBase
        {
            public static bCategoryBase Original;
            public bCategoryBase() { if ( Original == null ) Original = this; }

            private CatType Category = null;

            public void Assign( CatType Category )
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

                bool isSelected = InstanceG.CurrentRowType == this.Category;
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                buffer.StartColor( isSelected ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) :
                    ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );

                buffer.AddRaw( this.Category.GetDisplayName() );
                int number = this.Category.GetNumberInCategoryUIX();
                if ( number >= 0 )
                {
                    this.SetRelatedImage1EnabledIfNeeded( true );
                    this.SetOtherReferenceText0TextIfNeeded( number.ToString() );
                }
                else
                {
                    this.SetRelatedImage1EnabledIfNeeded( false );
                    this.SetOtherReferenceText0TextIfNeeded( string.Empty );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( this.Category == null )
                    return MouseHandlingResult.PlayClickDeniedSound;
                InstanceG.CurrentRowType = this.Category;
                //scroll right panel back to top when change category
                if ( bMainContentParentBase.Instance != null )
                    bMainContentParentBase.Instance.Element.TryScrollToTop();
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                if ( this.Category == null )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "CatType", Category.GetDisplayName() ), this.Element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddRaw( this.Category.GetDisplayName() );
                    this.Category.WriteDescriptionForTooltip( novel.Main );
                }
            }
        }
        #endregion

        #region bDataItemBase
        public class bDataItemBase : ButtonAbstractBase
        {
            public static bDataItemBase Original;
            public bDataItemBase() { if ( Original == null ) Original = this; }

            protected DatType DataItem = null;

            public void Assign( DatType Fac )
            {
                this.DataItem = Fac;
            }

            public override bool GetShouldBeHidden()
            {
                return this.DataItem == null;
            }

            public override void Clear()
            {
                this.DataItem = null;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
            {
                if ( this.DataItem == null )
                    return;

                if ( ChosenDisplayStyle != null )
                    ChosenDisplayStyle.GetImplementation().WriteSecondaryText_UIXDisplayStyle( buffer, this.DataItem, ChosenDisplayStyle, this );
                else
                {
                    bool isSelected = false;// Instance.SelectedFile == this.Save;
                    this.SetRelatedImage0EnabledIfNeeded( isSelected );
                    buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( this.Element.LastHadMouseWithin ) :
                        ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) );

                    buffer.AddRaw( this.DataItem.GetDisplayName() );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                //ArcenDebugging.LogSingleLine( "clicked:" + ( this.DataItem != null ), Verbosity.DoNotShow );
                if ( this.DataItem == null )
                    return MouseHandlingResult.PlayClickDeniedSound;

                if ( input.LeftButtonClicked || input.LeftButtonDoubleClicked )
                {
                    if ( this.DataItem.DataItemUIX_TryHandlePrimaryClick( out bool shouldCloseWindow ) )
                    {
                        if ( shouldCloseWindow )
                            InstanceG.Close( WindowCloseReason.UserDirectRequest );
                    }
                    else
                        VisCommands.ShowInformationAboutUIXExaminedDataItem( this.DataItem );
                }
                else
                {
                    //ArcenDebugging.LogSingleLine( "call handle alt on" + this.DataItem.GetType(), Verbosity.DoNotShow );
                    this.DataItem.DataItemUIX_HandleAltClick( out bool shouldCloseWindow );
                    if ( shouldCloseWindow )
                        InstanceG.Close( WindowCloseReason.UserDirectRequest );
                }
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                this.DataItem?.WriteDataItemUIXTooltip( this.Element, SideClamp.LeftOrRight );
            }
        }
        #endregion

        #region dRightOptionsBase
        public class dRightOptionsBase : DropdownAbstractBase
        {
            public static dRightOptionsBase Instance;
            public dRightOptionsBase()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                UIXDisplayItem<DatType> ItemAsType = (UIXDisplayItem<DatType>)Item.GetItem();
                ChosenDisplayStyle = ItemAsType;
            }

            public override bool GetShouldBeHidden()
            {
                UIXDisplayTable<DatType> displayTable = InstanceG.GetUIXDisplayTable();
                return displayTable == null;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                UIXDisplayItem<DatType> DefaultRow = null;

                UIXDisplayTable<DatType> displayTable = InstanceG.GetUIXDisplayTable();
                if ( displayTable != null )
                    displayTable.FillValidDisplayItems( CurrentValidDisplayItems, out DefaultRow );

                UIXDisplayItem<DatType> typeDataToSelect = ChosenDisplayStyle;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !CurrentValidDisplayItems.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        ChosenDisplayStyle = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && CurrentValidDisplayItems.Count > 0 )
                    typeDataToSelect = CurrentValidDisplayItems[0];
                #endregion

                if ( ChosenDisplayStyle == null && typeDataToSelect != null )
                    ChosenDisplayStyle = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (UIXDisplayItem<DatType>)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( CurrentValidDisplayItems.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < CurrentValidDisplayItems.Count; i++ )
                    {
                        UIXDisplayItem<DatType> row = CurrentValidDisplayItems[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        UIXDisplayItem<DatType> optionItemAsType = (UIXDisplayItem<DatType>)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CurrentValidDisplayItems.Count; i++ )
                    {
                        UIXDisplayItem<DatType> row = CurrentValidDisplayItems[i];

                        ArcenDropdownOption option = new ArcenDropdownOption( row,
                            delegate ( ArcenDoubleCharacterBuffer buffer )
                            {
                                buffer.AddRaw( row.GetDisplayName() );
                            } );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                UIXDisplayItem<DatType> typeDataToSelect = ChosenDisplayStyle;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "AbstractBase", "dRightOptionsBase" ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "InfoWindow_DisplayStyle" );

                    if ( typeDataToSelect != null )
                    {
                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "InfoWindow_Currently" ).AddRaw( typeDataToSelect.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        if ( !typeDataToSelect.GetDescription().IsEmpty() )
                            novel.FrameBody.AddRaw( typeDataToSelect.GetDescription(), ColorTheme.NarrativeColor ).Line();
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                UIXDisplayItem<DatType> ItemAsType = (UIXDisplayItem<DatType>)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "AbstractBaseItem", Item.GetOptionValueForDropdown() ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "InfoWindow_DisplayStyle" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        #region dLeftOptionsBase
        public class dLeftOptionsBase : DropdownAbstractBase
        {
            public static dLeftOptionsBase Instance;
            public dLeftOptionsBase()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                UIXSortFilterItem<DatType> ItemAsType = (UIXSortFilterItem<DatType>)Item.GetItem();
                ChosenSortAndFilter = ItemAsType;
            }

            public override bool GetShouldBeHidden()
            {
                UIXSortFilterTable<DatType> sortFilterTable = InstanceG.GetUIXSortFilterTable();
                return sortFilterTable == null;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                UIXSortFilterItem<DatType> DefaultRow = null;

                UIXSortFilterTable<DatType> sortFilterTable = InstanceG.GetUIXSortFilterTable();
                if ( sortFilterTable != null )
                    sortFilterTable.FillValidSortFilterItems( CurrentValidSortFilterItems, out DefaultRow );

                UIXSortFilterItem<DatType> typeDataToSelect = ChosenSortAndFilter;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !CurrentValidSortFilterItems.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        ChosenSortAndFilter = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && CurrentValidSortFilterItems.Count > 0 )
                    typeDataToSelect = CurrentValidSortFilterItems[0];
                #endregion

                if ( ChosenSortAndFilter == null && typeDataToSelect != null )
                    ChosenSortAndFilter = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (UIXSortFilterItem<DatType>)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( CurrentValidSortFilterItems.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < CurrentValidSortFilterItems.Count; i++ )
                    {
                        UIXSortFilterItem<DatType> row = CurrentValidSortFilterItems[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        UIXSortFilterItem<DatType> optionItemAsType = (UIXSortFilterItem<DatType>)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < CurrentValidSortFilterItems.Count; i++ )
                    {
                        UIXSortFilterItem<DatType> row = CurrentValidSortFilterItems[i];

                        ArcenDropdownOption option = new ArcenDropdownOption( row,
                            delegate ( ArcenDoubleCharacterBuffer buffer )
                            {
                                buffer.AddRaw( row.GetDisplayName() );
                            } );
                        elementAsType.AddItem( option, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                UIXSortFilterItem<DatType> typeDataToSelect = ChosenSortAndFilter;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "AbstractBaseItem", "dLeftOptionsBase" ), this.Element,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "InfoWindow_SortFilter" );

                    if ( typeDataToSelect != null )
                    {
                        novel.FrameTitle.AddLangAndAfterLineItemHeader( "InfoWindow_Currently" ).AddRaw( typeDataToSelect.GetDisplayName(), ColorTheme.DataBlue ).Line();
                        if ( !typeDataToSelect.GetDescription().IsEmpty() )
                            novel.FrameBody.AddRaw( typeDataToSelect.GetDescription(), ColorTheme.NarrativeColor ).Line();
                    }
                }
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                UIXSortFilterItem<DatType> ItemAsType = (UIXSortFilterItem<DatType>)Item.GetItem();

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "dLeftOptionsBase", ItemAsType.GetID() ), ItemElement,
                    SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.TitleUpperLeft.AddLang( "InfoWindow_SortFilter" );

                    novel.Main.AddRaw( ItemAsType.GetDisplayName(), ColorTheme.DataBlue ).Line();
                    if ( !ItemAsType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( ItemAsType.GetDescription(), ColorTheme.NarrativeColor ).Line();
                }
            }
        }
        #endregion

        public class bExitBase : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InstanceG.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        public class bCloseBase : ButtonAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.StartColor( ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin ) );
                Buffer.AddLang( LangCommon.Popup_Common_Close );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                InstanceG.Close( WindowCloseReason.UserDirectRequest );
                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
                ArcenInput.BlockForAJustPartOfOneSecond();
                return MouseHandlingResult.None;
            }
        }

        public sealed override void Close( WindowCloseReason Reason )
        {
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

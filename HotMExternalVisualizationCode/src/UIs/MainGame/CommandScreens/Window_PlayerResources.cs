using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_PlayerResources : ToggleableWindowController, IInputActionHandler
    {
        #region Main Controller
        public static Window_PlayerResources Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_PlayerResources()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
            this.ShouldCauseAllOtherWindowsToNotShow = true;
            this.ShouldPretendDoesNotBlockUIForMouseWhenOpen = true;
        }

        public override void OnOpen()
        {
            ResourceStorage_FilterText = string.Empty;
            Lifeforms_FilterText = string.Empty;
            StrategicResources_FilterText = string.Empty;
            Ledger_FilterText = string.Empty;
            TPSReports_FilterText = string.Empty;

            bool resetFilterText = true;

            if ( resetFilterText )
            {
                if ( iFilterText.Instance != null )
                    iFilterText.Instance.SetText( string.Empty );
            }

            PrimaryRegularResourceCollection = null;
            LifeformsCollection = null;
            PrimaryStrategicResourceCollection = null;
            InputOutput_target = null;

            base.OnOpen();
        }

        private static string ResourceStorage_FilterText = string.Empty;
        private static string ResourceStorage_LastFilterText = string.Empty;
        private static int ResourceStorage_lastTurn = 0;
        private static ResourceTypeCollection PrimaryRegularResourceCollection = null;

        private static string Lifeforms_FilterText = string.Empty;
        private static string Lifeforms_LastFilterText = string.Empty;
        private static int Lifeforms_lastTurn = 0;
        private static ResourceTypeCollection LifeformsCollection = null;

        private static string StrategicResources_FilterText = string.Empty;
        private static string StrategicResources_LastFilterText = string.Empty;
        private static int StrategicResources_lastTurn = 0;
        private static ResourceTypeCollection PrimaryStrategicResourceCollection = null;

        private static string Ledger_FilterText = string.Empty;
        private static string Ledger_LastFilterText = string.Empty;
        private static int Ledger_lastTurn = 0;

        private static string TPSReports_FilterText = string.Empty;
        private static string TPSReports_LastFilterText = string.Empty;
        private static int TPSReports_lastTurn = 0;

        private static IProducerConsumerTarget InputOutput_target = null;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public static ResourcesDisplayType currentlyRequestedDisplayType = ResourcesDisplayType.ResourceStorage;

            public customParent()
            {
                Window_PlayerResources.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;
            private static ButtonAbstractBase.ButtonPool<bDataFullItem> bDataFullItemPool;
            private static ButtonAbstractBase.ButtonPool<bDataAdjustedItem> bDataAdjustedItemPool;
            private static ButtonAbstractBase.ButtonPool<bResourceItem> bResourceItemPool;
            private static ButtonAbstractBase.ButtonPool<bUnitType> bUnitTypePool;
            private static ButtonAbstractBase.ButtonPool<bDataHeader> bDataHeaderPool;

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
                        //keeps it from having visual glitches, but uses more GPU.  That's fine, this is the only window open at that time.
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bCategory.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "bCategory" );
                            bDataFullItemPool = new ButtonAbstractBase.ButtonPool<bDataFullItem>( bDataFullItem.Original, 10, "bDataFullItem" );
                            bDataAdjustedItemPool = new ButtonAbstractBase.ButtonPool<bDataAdjustedItem>( bDataAdjustedItem.Original, 10, "bDataAdjustedItem" );
                            bResourceItemPool = new ButtonAbstractBase.ButtonPool<bResourceItem>( bResourceItem.Original, 10, "bResourceItem" );
                            bUnitTypePool = new ButtonAbstractBase.ButtonPool<bUnitType>( bUnitType.Original, 10, "bUnitType" );
                            bDataHeaderPool = new ButtonAbstractBase.ButtonPool<bDataHeader>( bDataHeader.Original, 10, "bDataHeader" );
                        }
                    }
                    #endregion

                    OnUpdate_Categories();
                    OnUpdate_Content();
                }
            }
            private void OnUpdate_Categories()
            {
                bCategoryPool.Clear( 60 );

                float currentY = -5; //the position of the first entry

                // Categories are mostly static.
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                for ( ResourcesDisplayType displayType = ResourcesDisplayType.ResourceStorage; displayType < ResourcesDisplayType.Length; displayType++ )
                {
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break;
                    item.Assign( displayType );
                }

                bCategoryPool.ApplyItemsInRows( 6.1f, ref currentY, 27f, 218.5f, 25.6f, false );

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            private float maxYToShow;
            private float minYToShow;
            private float runningY;
            private float yTopOffset = 0f;

            private const float TOP_OFFSET_BUFFER = -1f;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load

            private void OnUpdate_Content()
            {
                bDataFullItemPool.Clear( 60 );
                bDataAdjustedItemPool.Clear( 60 );
                bResourceItemPool.Clear( 60 );
                bUnitTypePool.Clear( 60 );
                bDataHeaderPool.Clear( 60 );

                RectTransform rTran = (RectTransform)bDataFullItem.Original.Element.RelevantRect.parent;

                maxYToShow = -rTran.anchoredPosition.y;
                minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                maxYToShow += EXTRA_BUFFER;
                yTopOffset = -((rTran.anchoredPosition.y - topBuffer) + TOP_OFFSET_BUFFER);

                runningY = topBuffer;
                {
                    switch ( currentlyRequestedDisplayType )
                    {
                        case ResourcesDisplayType.ResourceStorage:
                            ResourceTypeCollection.DrawStyle = ResourceDrawStyle.Regular;
                            OnUpdate_Content_ResourceStorage();
                            break;
                        case ResourcesDisplayType.Lifeforms:
                            ResourceTypeCollection.DrawStyle = ResourceDrawStyle.Lifeforms;
                            OnUpdate_Content_Lifeforms();
                            break;
                        case ResourcesDisplayType.StrategicResources:
                            ResourceTypeCollection.DrawStyle = ResourceDrawStyle.Strategic;
                            OnUpdate_Content_StrategicResources();
                            break;
                        case ResourcesDisplayType.TPSReports:
                            OnUpdate_Content_TPSReports();
                            break;
                        case ResourcesDisplayType.Ledger:
                            OnUpdate_Content_Ledger();
                            break;
                        case ResourcesDisplayType.InputOutput:
                            OnUpdate_Content_InputOutput();
                            break;
                        case ResourcesDisplayType.ActiveDeals:
                            OnUpdate_Content_Deals();
                            break;
                        case ResourcesDisplayType.Length:
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( $"Error! Player Inventory tried to render with a city category of {currentlyRequestedDisplayType.ToString()}, this should be impossible. Acting as though it's requesting Primary Resources instead.", Verbosity.ShowAsError );
                            currentlyRequestedDisplayType = ResourcesDisplayType.ResourceStorage;
                            return;
                    }
                }

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( runningY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            private const float RESOURCE_ITEM_HEIGHT = 48.258f;
            private const float UNIT_ITEM_HEIGHT = 40f;


            public const float CONTENT_ROW_HEIGHT_SHORT = 22f;
            public const float CONTENT_ROW_HEIGHT_TALL = 32f;
            public const float CONTENT_ROW_GAP_SHORT = 0.1f;
            public const float CONTENT_ROW_GAP_TALL = 1f;
            public const float CONTENT_ROW_GAP_RESOURCE_ITEM = 4f;
            public const float CONTENT_ROW_GAP_UNIT_ITEM = 4f;

            #region CalculateBoundsSingle
            protected const float leftBuffer = 5.1f;
            protected const float topBuffer = -2.55f;

            private const float FULL_WIDTH = 832f;
            private const float QUARTER_WIDTH = 204f; //208
            private const float QUARTER_WIDTH_SPACING = 4f;

            private const float HALF_WIDTH = 406f; //was 416
            private const float HALF_WIDTH_SPACING = 11f;

            private const float QUINT_WIDTH = 163.4f; //166.4
            private const float QUINT_WIDTH_SPACING = 4f;

            private const float QUARTER_WIDTH_COL0 = leftBuffer;
            private const float QUARTER_WIDTH_COL1 = QUARTER_WIDTH_COL0 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;
            private const float QUARTER_WIDTH_COL2 = QUARTER_WIDTH_COL1 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;
            private const float QUARTER_WIDTH_COL3 = QUARTER_WIDTH_COL2 + QUARTER_WIDTH + QUARTER_WIDTH_SPACING;

            private const float QUINT_WIDTH_COL0 = leftBuffer;
            private const float QUINT_WIDTH_COL1 = QUINT_WIDTH_COL0 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL2 = QUINT_WIDTH_COL1 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL3 = QUINT_WIDTH_COL2 + QUINT_WIDTH + QUINT_WIDTH_SPACING;
            private const float QUINT_WIDTH_COL4 = QUINT_WIDTH_COL3 + QUINT_WIDTH + QUINT_WIDTH_SPACING;

            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY, bool IsTall )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, FULL_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );

                innerY -= IsTall ? CONTENT_ROW_HEIGHT_TALL + CONTENT_ROW_GAP_TALL : CONTENT_ROW_HEIGHT_SHORT + CONTENT_ROW_GAP_SHORT;
            }

            protected void CalculateBoundsLargeTwoLineHeader( out Rect soleBounds, ref float innerY )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, FULL_WIDTH, 58.02f );

                innerY -= 58.02f + CONTENT_ROW_GAP_TALL;
            }
            #endregion

            protected void CalculateBoundsDual( out Rect leftBounds, out Rect rightBounds, ref float innerY, bool IsTall )
            {
                leftBounds = ArcenFloatRectangle.CreateUnityRect( leftBuffer, innerY, HALF_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );
                rightBounds = ArcenFloatRectangle.CreateUnityRect( leftBounds.xMax + HALF_WIDTH_SPACING, innerY, HALF_WIDTH, IsTall ? CONTENT_ROW_HEIGHT_TALL : CONTENT_ROW_HEIGHT_SHORT );

                innerY -= IsTall ? CONTENT_ROW_HEIGHT_TALL + CONTENT_ROW_GAP_TALL : CONTENT_ROW_HEIGHT_SHORT + CONTENT_ROW_GAP_SHORT;
            }

            #region CalculateBoundsQuadColumn
            protected void CalculateBoundsQuadColumn( out Rect soleBounds, int CurrentColumn, float innerY, float HeightUsed )
            {
                float xPos;
                switch ( CurrentColumn )
                {
                    case 0:
                        xPos = QUARTER_WIDTH_COL0;
                        break;
                    case 1:
                        xPos = QUARTER_WIDTH_COL1;
                        break;
                    case 2:
                        xPos = QUARTER_WIDTH_COL2;
                        break;
                    case 3:
                        xPos = QUARTER_WIDTH_COL3;
                        break;
                    default:
                        throw new Exception( "CalculateBoundsQuadColumn: Asked for invalid column " + CurrentColumn + "!" );
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( xPos, innerY, QUARTER_WIDTH, HeightUsed );
            }
            #endregion

            #region CalculateBoundsQuintColumn
            protected void CalculateBoundsQuintColumn( out Rect soleBounds, int CurrentColumn, float innerY, float HeightUsed )
            {
                float xPos;
                switch ( CurrentColumn )
                {
                    case 0:
                        xPos = QUINT_WIDTH_COL0;
                        break;
                    case 1:
                        xPos = QUINT_WIDTH_COL1;
                        break;
                    case 2:
                        xPos = QUINT_WIDTH_COL2;
                        break;
                    case 3:
                        xPos = QUINT_WIDTH_COL3;
                        break;
                    case 4:
                        xPos = QUINT_WIDTH_COL4;
                        break;
                    default:
                        throw new Exception( "CalculateBoundsQuintColumn: Asked for invalid column " + CurrentColumn + "!" );
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( xPos, innerY, QUINT_WIDTH, HeightUsed );
            }
            #endregion

            #region OnUpdate_Content_ResourceStorage
            private void OnUpdate_Content_ResourceStorage()
            {
                #region Resource Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_ResourceStorage" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    int discoveredResourceCount = 0;
                                    int totalResourceCount = 0;
                                    foreach ( ResourceType resource in ResourceTypeTable.SortedRegularResources )
                                    {
                                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                                            continue;
                                        totalResourceCount++;
                                        if ( !resource.DuringGame_IsUnlocked() )
                                            continue;
                                        discoveredResourceCount++;
                                    }

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DiscoveredResources" )
                                        .AddRaw( discoveredResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TotalResources" )
                                        .AddRaw( totalResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                int currentColumn = 0;

                bool hasFilterChanged = !( ResourceStorage_FilterText == ResourceStorage_LastFilterText && ResourceStorage_lastTurn == SimCommon.Turn );
                ResourceStorage_LastFilterText = ResourceStorage_FilterText;
                ResourceStorage_lastTurn = SimCommon.Turn;

                if ( PrimaryRegularResourceCollection != null )
                {
                    //do the resources in order from the collection
                    foreach ( SortedResourceType sortedResource in PrimaryRegularResourceCollection.RegularResourcesList )
                    {
                        ResourceType resource = sortedResource.Type;
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( ResourceStorage_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_PrimaryResource = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_PrimaryResource = !resource.GetMatchesSearchString( ResourceStorage_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_PrimaryResource )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }
                else
                {
                    //do the resources in alphabetical order
                    foreach ( ResourceType resource in ResourceTypeTable.SortedRegularResources )
                    {
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( ResourceStorage_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_PrimaryResource = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_PrimaryResource = !resource.GetMatchesSearchString( ResourceStorage_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_PrimaryResource )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }

                //if ( currentColumn < 3 )
                {
                    this.CalculateBoundsQuintColumn( out Rect bounds, currentColumn, runningY, RESOURCE_ITEM_HEIGHT );
                    runningY -= (bounds.height + CONTENT_ROW_GAP_RESOURCE_ITEM);
                }
            }
            #endregion

            #region HandleSpecificResource
            private bool HandleSpecificResource( ResourceType resource, ref int CurrentColumn )
            {
                this.CalculateBoundsQuintColumn( out Rect bounds, CurrentColumn, runningY, RESOURCE_ITEM_HEIGHT );
                CurrentColumn++;
                if ( CurrentColumn > 4 )
                {
                    CurrentColumn = 0;
                    runningY -= (bounds.height + CONTENT_ROW_GAP_RESOURCE_ITEM);
                }

                if ( bounds.yMax < minYToShow )
                    return true; //it's scrolled up far enough we can skip it, yay!
                if ( bounds.yMax > maxYToShow )
                    return true; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                bResourceItem row = bResourceItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bResourceItemPool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "ResourceType", resource.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                string iconColorHex = resource.Implementation.CalculateColorHex( resource );

                                row.SetRelatedImage1SpriteIfNeeded( resource.Icon.GetSpriteForUI() );
                                row.SetRelatedImage1ColorFromHexIfNeeded( iconColorHex );
                                row.SetRelatedImage2EnabledIfNeeded( resource.IsPinnedToTopBar );

                                ExtraData.Buffer.AddRaw( resource.GetDisplayName(), ColorTheme.GetBasicLightTextBlue( element.LastHadMouseWithin ) );
                            }
                            break;
                        case UIAction.GetOtherTextToShowFromVolatile:
                            switch ( ExtraData.Int )
                            {
                                case 0:
                                    {
                                        Int64 val = resource.Current;

                                        if ( resource.ExcessOverage > 0 )
                                        {
                                            ExtraData.Buffer.StartSize60()
                                                .AddSpriteStyled_NoIndent( IconRefs.ResourceExcess.Icon, AdjustedSpriteStyle.InlineLarger1_2, IconRefs.ResourceExcess.DefaultColorTextHex )
                                                .AddLangAndAfterLineItemHeader( "Excess", IconRefs.ResourceExcess.DefaultColorTextHex )
                                                .AddFormat1( "Parenthetical", resource.ExcessOverage.ToStringLargeNumberAbbreviatedIfAboveOneMillion(),
                                                    IconRefs.ResourceExcess.DefaultColorTextHex ).EndSize();
                                            ExtraData.Buffer.Space3x();
                                        }

                                        bool isHovered = element.LastHadMouseWithin;

                                        ExtraData.Buffer.AddRaw( val.ToStringLargeNumberAbbreviatedIfAboveOneMillion(), val > 0 ? string.Empty : ColorTheme.GetCannotAfford( isHovered ) );

                                        if ( resource.HardCap >= 0 && resource.BaseStorageCapacity >= 0 )
                                        {
                                            if ( resource.Current >= resource.HardCap )
                                                ExtraData.Buffer.AddFormat1( "OutOF_SecondPartLargeSpacing", resource.HardCap.ToStringLargeNumberAbbreviated(), ColorTheme.GetCannotAfford( isHovered ) );
                                            else
                                                ExtraData.Buffer.AddFormat1( "OutOF_SecondPartLargeSpacing", resource.HardCap.ToStringLargeNumberAbbreviated(), ColorTheme.GetGray( isHovered ) );
                                        }
                                    }
                                    break;
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                TooltipExtraText extraText = TooltipExtraText.None;
                                if ( resource.IsPinnedToTopBar )
                                    extraText = TooltipExtraText.PinnedResource_Is;
                                else
                                    extraText = TooltipExtraText.PinnedResource_IsNot;

                                resource.WriteResourceTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForConstruction, extraText );
                            }
                            break;
                        case UIAction.OnClick:
                            resource.IsPinnedToTopBar = !resource.IsPinnedToTopBar;
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region OnUpdate_Content_StrategicResources
            private void OnUpdate_Content_StrategicResources()
            {
                #region Strategic Resource Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_StrategicResources" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    int discoveredResourceCount = 0;
                                    int totalResourceCount = 0;
                                    int plotRelated = 0;
                                    foreach ( ResourceType resource in ResourceTypeTable.SortedStrategicResources )
                                    {
                                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                                            continue;

                                        if ( resource.IsPlotRelatedResource )
                                            plotRelated++;
                                        else
                                        {
                                            totalResourceCount++;
                                            if ( !resource.DuringGame_IsUnlocked() )
                                                continue;
                                            discoveredResourceCount++;
                                        }
                                    }

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DiscoveredResources" )
                                        .AddRaw( discoveredResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TotalResources" )
                                        .AddRaw( totalResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "PlotRelated" )
                                        .AddRaw( plotRelated.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                int currentColumn = 0;

                bool hasFilterChanged = !(StrategicResources_FilterText == StrategicResources_LastFilterText && StrategicResources_lastTurn == SimCommon.Turn);
                StrategicResources_LastFilterText = StrategicResources_FilterText;
                StrategicResources_lastTurn = SimCommon.Turn;

                if ( PrimaryStrategicResourceCollection != null )
                {
                    //do the resources in order from the collection
                    foreach ( SortedResourceType sortedResource in PrimaryStrategicResourceCollection.StrategicResourcesList )
                    {
                        ResourceType resource = sortedResource.Type;
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( StrategicResources_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_StrategicResource = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_StrategicResource = !resource.GetMatchesSearchString( StrategicResources_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_StrategicResource )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }
                else
                {
                    //do the resources in alphabetical order
                    foreach ( ResourceType resource in ResourceTypeTable.SortedStrategicResources )
                    {
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( StrategicResources_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_StrategicResource = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_StrategicResource = !resource.GetMatchesSearchString( StrategicResources_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_StrategicResource )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }

                //if ( currentColumn < 3 )
                {
                    this.CalculateBoundsQuintColumn( out Rect bounds, currentColumn, runningY, RESOURCE_ITEM_HEIGHT );
                    runningY -= (bounds.height + CONTENT_ROW_GAP_RESOURCE_ITEM);
                }
            }
            #endregion

            #region OnUpdate_Content_Lifeforms
            private void OnUpdate_Content_Lifeforms()
            {
                #region Resource Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_Lifeforms" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    int discoveredResourceCount = 0;
                                    int totalResourceCount = 0;
                                    foreach ( ResourceType resource in ResourceTypeTable.SortedLifeformResources )
                                    {
                                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                                            continue;
                                        totalResourceCount++;
                                        if ( !resource.DuringGame_IsUnlocked() )
                                            continue;
                                        discoveredResourceCount++;
                                    }

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DiscoveredResources" )
                                        .AddRaw( discoveredResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TotalResources" )
                                        .AddRaw( totalResourceCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                int currentColumn = 0;

                bool hasFilterChanged = !(Lifeforms_FilterText == Lifeforms_LastFilterText && Lifeforms_lastTurn == SimCommon.Turn);
                Lifeforms_LastFilterText = Lifeforms_FilterText;
                Lifeforms_lastTurn = SimCommon.Turn;

                if ( LifeformsCollection != null )
                {
                    //do the resources in order from the collection
                    foreach ( SortedResourceType sortedResource in LifeformsCollection.LifeformsList )
                    {
                        ResourceType resource = sortedResource.Type;
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( Lifeforms_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_Lifeforms = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_Lifeforms = !resource.GetMatchesSearchString( Lifeforms_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_Lifeforms )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }
                else
                {
                    //do the resources in alphabetical order
                    foreach ( ResourceType resource in ResourceTypeTable.SortedLifeformResources )
                    {
                        if ( resource.IsAlwaysSkippedOnResourceScreen )
                            continue;
                        if ( !resource.DuringGame_IsUnlocked() )
                            continue;
                        if ( hasFilterChanged )
                        {
                            if ( Lifeforms_FilterText.IsEmpty() )
                                resource.DuringGame_HasBeenFilteredOut_Lifeforms = false;
                            else
                                resource.DuringGame_HasBeenFilteredOut_Lifeforms = !resource.GetMatchesSearchString( Lifeforms_FilterText );
                        }
                        if ( resource.DuringGame_HasBeenFilteredOut_Lifeforms )
                            continue;

                        if ( !HandleSpecificResource( resource, ref currentColumn ) )
                            continue; //time-slicing
                    }
                }

                //if ( currentColumn < 3 )
                {
                    this.CalculateBoundsQuintColumn( out Rect bounds, currentColumn, runningY, RESOURCE_ITEM_HEIGHT );
                    runningY -= (bounds.height + CONTENT_ROW_GAP_RESOURCE_ITEM);
                }
            }
            #endregion

            #region OnUpdate_Content_TPSReports
            private void OnUpdate_Content_TPSReports()
            {
                if ( !FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                {
                    #region Not Yet Unlocked
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_TPSReports" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ReportNotYetUnlocked" );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                    #endregion

                    return;
                }

                #region Report Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_TPSReports" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "FullProductionEfficiency" )
                                        .AddRaw( SimCommon.ProductionJobsWithoutIssues.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "ThroughputIssues" )
                                        .AddRaw( SimCommon.ProductionJobsWithProblems.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                #region Header Bar
                {
                    bool render = true;
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    runningY -= CONTENT_ROW_GAP_UNIT_ITEM;

                    bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        render = false;
                    if ( render )
                    {
                        float diff = bounds.yMin - yTopOffset;
                        if ( diff > 0 )
                            bounds.y -= diff;

                        bDataHeaderPool.ApplySingleItemInRow( row, bounds, false );
                        row.Element.RelevantRect.SetAsLastSibling(); //make sure it is always on top

                        row.Assign( "Totals", "TPS", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        //ExtraData.Buffer.AddRaw( yTopOffset + " " + (bounds.yMin) + " " + diff + " " + bounds.y );

                                        ExtraData.Buffer.StartSize80().AddRaw( GameSettings.CurrentLanguage.TableA1 ).StartLink( false, string.Empty, "InputAvailability", string.Empty )
                                            .AddLang( "TPS_InputAvailability" ).EndLink( false, false )
                                            .AddRaw( GameSettings.CurrentLanguage.TableA2 ).StartLink( false, string.Empty, "OutputEffectiveness", string.Empty )
                                            .AddLang( "TPS_OutputEffectiveness" ).EndLink( false, false )
                                            .AddRaw( GameSettings.CurrentLanguage.TableA3 ).AddLang( "Job" );
                                    }
                                    break;
                                case UIAction.OnHyperlinkHover:
                                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                    switch ( ExtraData.LinkData[0] )
                                    {
                                        case "InputAvailability":
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "TPSHeader", ExtraData.LinkData[0] ), element, 
                                                SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.TitleUpperLeft.AddLang( "TPS_InputAvailability" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                    .AddLangWithFirstLineBold( "TPS_InputAvailability_Details1" ).Line()
                                                    .AddLang( "TPS_InputAvailability_Details2", ColorTheme.PurpleDim );
                                            }
                                            break;
                                        case "OutputEffectiveness":
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "TPSHeader", ExtraData.LinkData[0] ), element,
                                                SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.TitleUpperLeft.AddLang( "TPS_OutputEffectiveness" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                    .AddLangWithFirstLineBold( "TPS_OutputEffectiveness_Details1" ).Line()
                                                    .AddLang( "TPS_OutputEffectiveness_Details2", ColorTheme.PurpleDim );
                                            }
                                            break;
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }
                #endregion

                bool hasFilterChanged = !(TPSReports_FilterText == TPSReports_LastFilterText && TPSReports_lastTurn == SimCommon.Turn);
                TPSReports_LastFilterText = TPSReports_FilterText;
                TPSReports_lastTurn = SimCommon.Turn;

                foreach ( MachineJob job in SimCommon.ProductionJobsWithProblems.GetDisplayList() )
                {
                    if ( hasFilterChanged )
                    {
                        if ( TPSReports_LastFilterText.IsEmpty() )
                            job.DuringGame_HasBeenFilteredOut_TPSReport = false;
                        else
                            job.DuringGame_HasBeenFilteredOut_TPSReport = !job.GetMatchesSearchString( TPSReports_LastFilterText );
                    }

                    if ( job.DuringGame_HasBeenFilteredOut_TPSReport )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleSpecificJobReport( job, bounds ) )
                        continue;
                }

                foreach ( MachineJob job in SimCommon.ProductionJobsWithoutIssues.GetDisplayList() )
                {
                    if ( hasFilterChanged )
                    {
                        if ( TPSReports_LastFilterText.IsEmpty() )
                            job.DuringGame_HasBeenFilteredOut_TPSReport = false;
                        else
                            job.DuringGame_HasBeenFilteredOut_TPSReport = !job.GetMatchesSearchString( TPSReports_LastFilterText );
                    }

                    if ( job.DuringGame_HasBeenFilteredOut_TPSReport )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleSpecificJobReport( job, bounds ) )
                        continue;
                }
            }

            private bool HandleSpecificJobReport( MachineJob job, Rect bounds )
            {
                bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataFullItemPool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "MachineJobReport", job.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                job.RenderTPSReportLine( ExtraData.Buffer );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                job.RenderTPSReportTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                            }
                            break;
                        case UIAction.OnClick:
                            job.IsTPSReportIgnored = !job.IsTPSReportIgnored;
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region OnUpdate_Content_Ledger
            private void OnUpdate_Content_Ledger()
            {
                if ( !FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                {
                    #region Not Yet Unlocked
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_Ledger" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "ReportNotYetUnlocked" );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                    #endregion

                    return;
                }

                bLargeTwoLineHeader.Instance?.Assign( null );

                #region Header Bar
                {
                    bool render = true;
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    runningY -= CONTENT_ROW_GAP_UNIT_ITEM;

                    bDataHeader row = bDataHeaderPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        render = false;
                    if ( render )
                    {
                        float diff = bounds.yMin - yTopOffset;
                        if ( diff > 0 )
                            bounds.y -= diff;

                        bDataHeaderPool.ApplySingleItemInRow( row, bounds, false );
                        row.Element.RelevantRect.SetAsLastSibling(); //make sure it is always on top

                        row.Assign( "Totals", "Ledger", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        ExtraData.Buffer.StartSize80().Position10().StartLink( false, string.Empty, "ActualTrend", string.Empty )
                                            .AddLang( "Ledger_ActualTrend" ).EndLink( false, false )
                                            .Position90().StartLink( false, string.Empty, "TargetTrend", string.Empty )
                                            .AddLang( "Ledger_TargetTrend" ).EndLink( false, false )
                                            .Position150().StartLink( false, string.Empty, "LostToCap", string.Empty )
                                            .AddLang( "Ledger_LostToCap" ).EndLink( false, false )
                                            .Position220().AddLang( "Resource" );
                                    }
                                    break;
                                case UIAction.OnHyperlinkHover:
                                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
                                    switch (ExtraData.LinkData[0] )
                                    {
                                        case "ActualTrend":
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LedgerHeader", ExtraData.LinkData[0] ), element,
                                                SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.TitleUpperLeft.AddLang( "Ledger_ActualTrend" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                    .AddLangWithFirstLineBold( "Ledger_ActualTrend_Details1" ).Line()
                                                    .AddLang( "Ledger_ActualTrend_Details2", ColorTheme.PurpleDim );
                                            }
                                            break;
                                        case "TargetTrend":
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LedgerHeader", ExtraData.LinkData[0] ), element,
                                                SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.TitleUpperLeft.AddLang( "Ledger_ActualTrend" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                    .AddLangWithFirstLineBold( "Ledger_TargetTrend_Details1" ).Line()
                                                    .AddLang( "Ledger_TargetTrend_Details2", ColorTheme.PurpleDim );
                                            }
                                            break;
                                        case "LostToCap":
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "LedgerHeader", ExtraData.LinkData[0] ), element,
                                                SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                                            {
                                                novel.ShadowStyle = TooltipShadowStyle.Standard;
                                                novel.TitleUpperLeft.AddLang( "Ledger_LostToCap" );
                                                novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                    .AddLangWithFirstLineBold( "Ledger_LostToCap_Details" ).Line();
                                            }
                                            break;
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }
                #endregion

                bool hasFilterChanged = !(Ledger_FilterText == Ledger_LastFilterText && Ledger_lastTurn == SimCommon.Turn);
                Ledger_LastFilterText = Ledger_FilterText;
                Ledger_lastTurn = SimCommon.Turn;

                foreach ( ResourceType resource in SimCommon.ProductionResourcesWithMajorProblems.GetDisplayList() )
                {
                    if ( resource.IsHidden )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( Ledger_LastFilterText.IsEmpty() )
                            resource.DuringGame_HasBeenFilteredOut_Ledger = false;
                        else
                            resource.DuringGame_HasBeenFilteredOut_Ledger = !resource.GetMatchesSearchString( Ledger_LastFilterText );
                    }

                    if ( resource.DuringGame_HasBeenFilteredOut_Ledger )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleSpecificLedgerReport( resource, bounds ) )
                        continue;
                }

                foreach ( ResourceType resource in SimCommon.ProductionResourcesWithMinorProblems.GetDisplayList() )
                {
                    if ( resource.IsHidden )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( Ledger_LastFilterText.IsEmpty() )
                            resource.DuringGame_HasBeenFilteredOut_Ledger = false;
                        else
                            resource.DuringGame_HasBeenFilteredOut_Ledger = !resource.GetMatchesSearchString( Ledger_LastFilterText );
                    }

                    if ( resource.DuringGame_HasBeenFilteredOut_Ledger )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleSpecificLedgerReport( resource, bounds ) )
                        continue;
                }

                foreach ( ResourceType resource in SimCommon.ProductionResourcesWithoutIssues.GetDisplayList() )
                {
                    if ( resource.IsHidden )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( Ledger_LastFilterText.IsEmpty() )
                            resource.DuringGame_HasBeenFilteredOut_Ledger = false;
                        else
                            resource.DuringGame_HasBeenFilteredOut_Ledger = !resource.GetMatchesSearchString( Ledger_LastFilterText );
                    }

                    if ( resource.DuringGame_HasBeenFilteredOut_Ledger )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( !HandleSpecificLedgerReport( resource, bounds ) )
                        continue;
                }
            }

            private bool HandleSpecificLedgerReport( ResourceType resource, Rect bounds )
            {
                bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataFullItemPool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "LedgerReport", resource.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                resource.RenderLedgerReportLine( ExtraData.Buffer );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            resource.RenderLedgerReportTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                            break;
                        case UIAction.OnClick:
                            resource.IsLedgerIgnored = !resource.IsLedgerIgnored;
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region OnUpdate_Content_InputOutput
            private void OnUpdate_Content_InputOutput()
            {
                if ( InputOutput_target == null && SimCommon.ProducerConsumerTargets.Count > 0 )
                    InputOutput_target = SimCommon.ProducerConsumerTargets.GetDisplayList().FirstOrDefault;

                if ( InputOutput_target != null )
                {
                    #region IO Totals
                    {
                        bool render = true;
                        this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                        if ( bounds.yMax < minYToShow )
                            render = false; //it's scrolled up far enough we can skip it, yay!
                        if ( bounds.yMax > maxYToShow )
                            render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        if ( render && bLargeTwoLineHeader.Instance != null )
                        {
                            bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                            bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        ExtraData.Buffer.AddLang( "Inventory_InputOutput" );
                                        break;
                                    case UIAction.GetOtherTextToShowFromVolatile:
                                        {
                                            if ( InputOutput_target is ResourceType resource )
                                            {
                                                ExtraData.Buffer.AddSpriteStyled_NoIndent( resource.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x()
                                                    .AddRawAndAfterLineItemHeader( resource.GetDisplayName() );

                                                Int64 actualIncome = resource.GetActualIncome();
                                                Int64 actualExpense = resource.GetActualExpense();

                                                Int64 actualTrend = actualIncome - actualExpense;

                                                if ( actualTrend > 0 )
                                                    ExtraData.Buffer.AddFormat1( "PositiveChange", actualTrend.ToStringLargeNumberAbbreviated(), ColorTheme.CategorySelectedBlue );
                                                else if ( actualTrend < 0 )
                                                {
                                                    ExtraData.Buffer.AddRaw( actualTrend.ToStringLargeNumberAbbreviated(), ColorTheme.RedOrange2 );
                                                    ExtraData.Buffer.Space1x().StartSize60();
                                                    int turnCount = (int)(resource.Current / -actualTrend);
                                                    ExtraData.Buffer.AddFormat1( "Parenthetical", turnCount );
                                                    ExtraData.Buffer.EndSize();
                                                }
                                                else
                                                    ExtraData.Buffer.AddNeverTranslated( "-", true, ColorTheme.GrayDark );
                                            }
                                            else if ( InputOutput_target is NetworkActorData networkData )
                                            {
                                                ExtraData.Buffer.AddSpriteStyled_NoIndent( networkData.Type.Icon, AdjustedSpriteStyle.InlineLarger1_2 ).Space1x()
                                                    .AddRawAndAfterLineItemHeader( networkData.Type.NetworkNameOptional.Text.IsEmpty() ? networkData.Type.GetDisplayName() : 
                                                    networkData.Type.NetworkNameOptional.Text );

                                                string colorHex = networkData.CalculateSidebarIconColorHex( false );
                                                float multiplier = networkData.EffectivenessMultiplier;

                                                if ( multiplier < 1f )
                                                    colorHex = ColorTheme.RedOrange3;

                                                {
                                                    int percentage = networkData.PercentProvided;
                                                    ExtraData.Buffer.AddRaw( percentage.ToStringIntPercent(), colorHex );
                                                }
                                            }
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( InputOutput_target is ResourceType resource )
                                                resource.RenderLedgerReportTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                                            else if ( InputOutput_target is NetworkActorData networkData )
                                            {
                                                networkData.WriteActorDataTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipExtraRules.None );
                                            }
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        break;
                                }
                            } );
                        }
                        else
                            bLargeTwoLineHeader.Instance?.Assign( null );
                    }
                    #endregion

                    ListView<KeyValuePair<MachineStructure, int>> inputs = InputOutput_target.GetProducerListView();
                    ListView<KeyValuePair<MachineStructure, int>> outputs = InputOutput_target.GetConsumerListView();
                    int maxIndex = MathA.Max( inputs.Count, outputs.Count );

                    for ( int i = 0; i < maxIndex; i++ )
                    {
                        this.CalculateBoundsDual( out Rect leftBounds, out Rect rightBounds, ref runningY, false );
                        if ( leftBounds.yMax < minYToShow )
                            continue; //it's scrolled up far enough we can skip it, yay!
                        if ( leftBounds.yMax > maxYToShow )
                            continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                        if ( i < inputs.Count )
                        {
                            KeyValuePair<MachineStructure, int> kv = inputs[i];
                            kv.Key.NonSim_WorkingProducerAmount = kv.Value;
                            HandleInputOutputStructure( kv.Key, true, leftBounds );
                        }

                        if ( i < outputs.Count )
                        {
                            KeyValuePair<MachineStructure, int> kv = outputs[i];
                            kv.Key.NonSim_WorkingConsumerAmount = kv.Value;
                            HandleInputOutputStructure( kv.Key, false, rightBounds );
                        }
                    }
                }
                else
                    bLargeTwoLineHeader.Instance?.Assign( null );
            }

            private bool HandleInputOutputStructure( MachineStructure structure, bool IsProducer, Rect bounds )
            {
                bDataAdjustedItem row = bDataAdjustedItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataAdjustedItemPool.ApplySingleItemInRow( row, bounds, true );

                row.Assign( IsProducer ? "StructureProducer" : "StructureConsumer", structure.PermanentIndexNonSimAsString, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );
                                ExtraData.Buffer.StartColor( ColorTheme.GetBasicLightTextBlue( isHovered ) );

                                ExtraData.Buffer.StartSize80();
                                if ( IsProducer )
                                    ExtraData.Buffer.AddRaw( structure.NonSim_WorkingProducerAmount.ToStringThousandsWhole(), ColorTheme.CategorySelectedBlue );
                                else
                                    ExtraData.Buffer.AddRaw( structure.NonSim_WorkingConsumerAmount.ToStringThousandsWhole(), ColorTheme.RedOrange2 );

                                ExtraData.Buffer.Position70();
                                ExtraData.Buffer.AddSpriteStyled_NoIndent( structure.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                                ExtraData.Buffer.AddRaw( structure.GetDisplayName() );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                structure.RenderTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                            }
                            break;
                        case UIAction.OnClick:
                            Instance.Close( WindowCloseReason.UserDirectRequest );
                            Engine_HotM.SetSelectedActor( structure, true, false, false );
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region OnUpdate_Content_Deals
            private void OnUpdate_Content_Deals()
            {
                #region Deals Totals
                {
                    bool render = true;
                    this.CalculateBoundsLargeTwoLineHeader( out Rect bounds, ref runningY );
                    if ( bounds.yMax < minYToShow )
                        render = false; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    if ( render && bLargeTwoLineHeader.Instance != null )
                    {
                        bLargeTwoLineHeader.Instance.ApplySingleItemInRow( bounds, false );

                        bLargeTwoLineHeader.Instance.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    ExtraData.Buffer.AddLang( "Inventory_ActiveDeals" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "ActiveDeals" )
                                        .AddRaw( SimCommon.ActiveDeals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "PriorDeals" )
                                        .AddRaw( SimCommon.PriorDeals.Count.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                    else
                        bLargeTwoLineHeader.Instance?.Assign( null );
                }
                #endregion

                foreach ( NPCDeal deal in SimCommon.ActiveDeals.GetDisplayList() )
                    HandleSpecificDeal( deal );

                foreach ( NPCDeal deal in SimCommon.PriorDeals.GetDisplayList() )
                    HandleSpecificDeal( deal );
            }
            #endregion

            #region HandleSpecificDeal
            private bool HandleSpecificDeal( NPCDeal deal )
            {
                this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                if ( bounds.yMax < minYToShow )
                    return true; //it's scrolled up far enough we can skip it, yay!
                if ( bounds.yMax > maxYToShow )
                    return true; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bDataFullItemPool.ApplySingleItemInRow( row, bounds, true );

                row.Assign( "NPCDeal", deal.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                row.SetRelatedImage0EnabledIfNeeded( isHovered );

                                deal.WriteDealInventoryLine( ExtraData.Buffer, ColorTheme.GetBasicLightTextBlue( isHovered ) );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            deal.RenderDealTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                            break;
                        case UIAction.OnClick:
                            //if ( deal.IsConsumable && deal.CostsToCreateIfConsumable.Count > 0 )
                            //{
                            //    if ( ExtraData.MouseInput.LeftButtonClicked )
                            //    {
                            //        if ( deal.TryCreateConsumableNow() )
                            //            ParticleSoundRefs.ConsumableCreatedSound.DuringGame_PlaySoundOnlyAtCamera();
                            //        else
                            //            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            //    }
                            //    else if ( ExtraData.MouseInput.RightButtonClicked )
                            //    {
                            //        if ( deal.TryDisassembleConsumableNow() )
                            //            ParticleSoundRefs.ConsumableDisassembledSound.DuringGame_PlaySoundOnlyAtCamera();
                            //        else
                            //            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            //    }
                            //}
                            //else
                            //{
                            //    if ( deal.IsLowerPriorityResource || deal.IsShownInTopBarOnlyDuringBuildModeOrPinned )
                            //        deal.IsPinnedToTopBar = !deal.IsPinnedToTopBar;
                            //}
                            break;
                        case UIAction.OnHyperlinkHover:
                            deal.HandleDealInventoryLine_Hyperlink( ExtraData.LinkData, ExtraData.MouseInput, false );
                            break;
                        case UIAction.OnHyperlinkClick:
                            deal.HandleDealInventoryLine_Hyperlink( ExtraData.LinkData, ExtraData.MouseInput, true );
                            break;
                    }
                } );
                return true;
            }
            #endregion
        }
        #endregion

        #region Supporting Elements
        public enum ResourcesDisplayType
        {
            ResourceStorage,
            Lifeforms,
            StrategicResources,

            Ledger,
            TPSReports,
            InputOutput,
            ActiveDeals,
            Length
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "HeaderBar_Inventory_Top" );
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

        #region dRightOptions
        public class dRightOptions : DropdownAbstractBase
        {
            public static dRightOptions Instance;
            public dRightOptions()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                if ( Item.GetItem() is ResourceTypeCollection resourceTypeCollection )
                {
                    switch (ResourceTypeCollection.DrawStyle )
                    {
                        case ResourceDrawStyle.Regular:
                            PrimaryRegularResourceCollection = resourceTypeCollection;
                            break;
                        case ResourceDrawStyle.Lifeforms:
                            LifeformsCollection = resourceTypeCollection;
                            break;
                        case ResourceDrawStyle.Strategic:
                            PrimaryStrategicResourceCollection = resourceTypeCollection;
                            break;
                    }
                }
                else if ( Item.GetItem() is IProducerConsumerTarget ProducerConsumerTarget )
                {
                    InputOutput_target = ProducerConsumerTarget;
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case ResourcesDisplayType.ResourceStorage:
                        #region ResourceStorage
                        {
                            List<ResourceTypeCollection> validOptions = ResourceTypeCollection.AvailableRegularCollections.GetDisplayList();

                            ResourceTypeCollection typeDataToSelect = PrimaryRegularResourceCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    PrimaryRegularResourceCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || 
                                elementAsType.CurrentlySelectedOption.GetItem() as ResourceTypeCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    ResourceTypeCollection optionItemAsType = option.GetItem() as ResourceTypeCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case ResourcesDisplayType.Lifeforms:
                        #region Lifeforms
                        {
                            List<ResourceTypeCollection> validOptions = ResourceTypeCollection.AvailableLifeformCollections.GetDisplayList();

                            ResourceTypeCollection typeDataToSelect = LifeformsCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    LifeformsCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as ResourceTypeCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    ResourceTypeCollection optionItemAsType = option.GetItem() as ResourceTypeCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case ResourcesDisplayType.InputOutput:
                        #region InputOutput
                        {
                            List<IProducerConsumerTarget> validOptions = SimCommon.ProducerConsumerTargets.GetDisplayList();

                            IProducerConsumerTarget typeDataToSelect = InputOutput_target;

                            //#region If The Selected Type Is Not Valid Right Now, Then Skip It
                            //if ( typeDataToSelect != null )
                            //{
                            //    if ( !validOptions.Contains( typeDataToSelect ) )
                            //    {
                            //        typeDataToSelect = null;
                            //        UnitTypes_sortAndFilter = null;
                            //    }
                            //}
                            //#endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as IProducerConsumerTarget != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    IProducerConsumerTarget row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    IProducerConsumerTarget optionItemAsType = option.GetItem() as IProducerConsumerTarget;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    IProducerConsumerTarget row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case ResourcesDisplayType.StrategicResources:
                        #region StrategicResources
                        {
                            List<ResourceTypeCollection> validOptions = ResourceTypeCollection.AvailableStrategicCollections.GetDisplayList();

                            ResourceTypeCollection typeDataToSelect = PrimaryStrategicResourceCollection;

                            #region If The Selected Type Is Not Valid Right Now, Then Skip It
                            if ( typeDataToSelect != null )
                            {
                                if ( !validOptions.Contains( typeDataToSelect ) )
                                {
                                    typeDataToSelect = null;
                                    PrimaryStrategicResourceCollection = null;
                                }
                            }
                            #endregion

                            #region Select Default If Blank
                            if ( typeDataToSelect == null && validOptions.Count > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as ResourceTypeCollection != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    ResourceTypeCollection optionItemAsType = option.GetItem() as ResourceTypeCollection;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Count; i++ )
                                {
                                    ResourceTypeCollection row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                }
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
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return true;
                else
                {
                    switch ( customParent.currentlyRequestedDisplayType )
                    {
                        case ResourcesDisplayType.ResourceStorage:
                        case ResourcesDisplayType.Lifeforms:
                        case ResourcesDisplayType.StrategicResources:
                        case ResourcesDisplayType.InputOutput:
                            return false;
                    }
                    return true;
                }
            }
        }
        #endregion

        #region dLeftOptions
        public class dLeftOptions : DropdownAbstractBase
        {
            public static dLeftOptions Instance;
            public dLeftOptions()
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

            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
        #endregion

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
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case ResourcesDisplayType.ResourceStorage:
                        ResourceStorage_FilterText = newString;
                        break;
                    case ResourcesDisplayType.StrategicResources:
                        StrategicResources_FilterText = newString;
                        break;
                    case ResourcesDisplayType.Lifeforms:
                        Lifeforms_FilterText = newString;
                        break;
                    case ResourcesDisplayType.Ledger:
                        Ledger_FilterText = newString;
                        break;
                    case ResourcesDisplayType.TPSReports:
                        TPSReports_FilterText = newString;
                        break;
                }
            }

            public override bool GetShouldBeHidden()
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return true;
                else
                {
                    switch (customParent.currentlyRequestedDisplayType)
                    {
                        case ResourcesDisplayType.ResourceStorage:
                        case ResourcesDisplayType.StrategicResources:
                        case ResourcesDisplayType.Lifeforms:
                        case ResourcesDisplayType.Ledger:
                        case ResourcesDisplayType.TPSReports:
                            return false;
                    }
                    return true;
                }
            }

            public override void OnEndEdit()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case ResourcesDisplayType.ResourceStorage:
                        ResourceStorage_FilterText = this.GetText();
                        break;
                    case ResourcesDisplayType.StrategicResources:
                        StrategicResources_FilterText = this.GetText();
                        break;
                    case ResourcesDisplayType.Lifeforms:
                        Lifeforms_FilterText = this.GetText();
                        break;
                    case ResourcesDisplayType.Ledger:
                        Ledger_FilterText = this.GetText();
                        break;
                    case ResourcesDisplayType.TPSReports:
                        TPSReports_FilterText = this.GetText();
                        break;
                }
            }

            private ArcenUI_Input inputField = null;
            private ResourcesDisplayType lastDisplayType = ResourcesDisplayType.Length;

            public override void OnMainThreadUpdate()
            {
                if ( lastDisplayType != customParent.currentlyRequestedDisplayType )
                {
                    switch ( customParent.currentlyRequestedDisplayType )
                    {
                        case ResourcesDisplayType.ResourceStorage:
                            this.SetText( ResourceStorage_FilterText );
                            break;
                        case ResourcesDisplayType.StrategicResources:
                            this.SetText( StrategicResources_FilterText );
                            break;
                        case ResourcesDisplayType.Lifeforms:
                            this.SetText( Lifeforms_FilterText );
                            break;
                        case ResourcesDisplayType.Ledger:
                            this.SetText( Ledger_FilterText );
                            break;
                        case ResourcesDisplayType.TPSReports:
                            this.SetText( TPSReports_FilterText );
                            break;
                    }
                    lastDisplayType = customParent.currentlyRequestedDisplayType;
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
                default:
                    VisCommands.HandleMajorWindowKeyPress( InputActionType );
                    break;
            }
        }
        #endregion

        #region bCategory
        public class bCategory : ButtonAbstractBase
        {
            public static bCategory Original;
            public bCategory() { if ( Original == null ) Original = this; }

            public ResourcesDisplayType displayType = ResourcesDisplayType.Length;
            private string NameFromLang => displayType == ResourcesDisplayType.Length ? LangCommon.None.Text :
                Lang.Get( $"Inventory_{displayType.ToString()}" );
            private string DescriptionFromLang => displayType == ResourcesDisplayType.Length ? LangCommon.None.Text :
                Lang.Get( $"Inventory_{displayType.ToString()}Description" );

            public void Assign( ResourcesDisplayType displayTypeCity )
            {
                this.displayType = displayTypeCity;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( displayType != ResourcesDisplayType.Length )
                    customParent.currentlyRequestedDisplayType = displayType;
                return MouseHandlingResult.None;
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bCategory", (int)displayType, 0 ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddRaw( NameFromLang );
                    novel.Main.AddRaw( DescriptionFromLang );
                }
            }

            private int countToShow = 0;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                countToShow = 0;
                bool showGrayedOut = false;
                bool isSelected = customParent.currentlyRequestedDisplayType == displayType;
                this.ExtraSpaceAfterInAutoSizing = 0f;
                switch ( this.displayType )
                {
                    case ResourcesDisplayType.ResourceStorage:
                        {
                            int discoveredResourceCount = 0;
                            foreach ( ResourceType resource in ResourceTypeTable.SortedRegularResources )
                            {
                                if ( !resource.DuringGame_IsUnlocked() )
                                    continue;
                                discoveredResourceCount++;
                                break; //finding one is enough
                            }

                            countToShow = discoveredResourceCount;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                        }
                        break;
                    case ResourcesDisplayType.Lifeforms:
                        {
                            int discoveredResourceCount = 0;
                            foreach ( ResourceType resource in ResourceTypeTable.SortedLifeformResources )
                            {
                                if ( !resource.DuringGame_IsUnlocked() )
                                    continue;
                                if ( resource.IsHiddenWhenNoneHad && resource.Current == 0 )
                                    continue;
                                discoveredResourceCount++;
                                break; //finding one is enough
                            }

                            countToShow = discoveredResourceCount;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                        }
                        break;
                    case ResourcesDisplayType.StrategicResources:
                        {
                            int discoveredResourceCount = 0;
                            foreach ( ResourceType resource in ResourceTypeTable.SortedStrategicResources )
                            {
                                if ( !resource.DuringGame_IsUnlocked() )
                                    continue;
                                if ( resource.IsHiddenWhenNoneHad && resource.Current == 0 )
                                    continue;
                                discoveredResourceCount++;
                                break; //finding one is enough
                            }

                            countToShow = discoveredResourceCount;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                        }
                        break;
                    case ResourcesDisplayType.TPSReports:
                        {
                            if ( FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                            {
                                int problemCount = 0;
                                if ( SimCommon.ProductionJobsWithProblems.Count > 0 )
                                {
                                    foreach ( MachineJob item in SimCommon.ProductionJobsWithProblems.GetDisplayList() )
                                    {
                                        if ( !item.IsTPSReportIgnored )
                                            problemCount++;
                                    }
                                }
                                countToShow = problemCount;
                            }
                            showGrayedOut = countToShow + SimCommon.ProductionJobsWithoutIssues.Count == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                        }
                        break;
                    case ResourcesDisplayType.Ledger:
                        {
                            if ( FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                            {
                                int problemCount = 0;
                                if ( SimCommon.ProductionResourcesWithMinorProblems.Count > 0 )
                                {
                                    foreach ( ResourceType resource in SimCommon.ProductionResourcesWithMinorProblems.GetDisplayList() )
                                    {
                                        if ( !resource.IsLedgerIgnored && !resource.IsHidden )
                                            problemCount++;
                                    }
                                }
                                if ( SimCommon.ProductionResourcesWithMajorProblems.Count > 0 )
                                {
                                    foreach ( ResourceType resource in SimCommon.ProductionResourcesWithMajorProblems.GetDisplayList() )
                                    {
                                        if ( !resource.IsLedgerIgnored && !resource.IsHidden )
                                            problemCount++;
                                    }
                                }
                                countToShow = problemCount;
                            }
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                        }
                        break;
                    case ResourcesDisplayType.InputOutput:
                        {
                            countToShow = SimCommon.ProducerConsumerTargets.Count;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                            this.ExtraSpaceAfterInAutoSizing = 10f;
                        }
                        break;
                    case ResourcesDisplayType.ActiveDeals:
                        {
                            countToShow = SimCommon.ActiveDeals.Count;
                            showGrayedOut = countToShow == 0;
                            if ( countToShow == 0 )
                                countToShow = -1;
                        }
                        break;
                    case ResourcesDisplayType.Length:
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "Forgot to set up bCategory-GetTextToShowFromVolatile for city " + this.displayType + "!", Verbosity.ShowAsError );
                        break;
                }

                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[isSelected ? 1 : 0] );
                Buffer.StartColor( isSelected ? ColorTheme.GetBasicLightTextBlue( this.Element.LastHadMouseWithin ) :
                    (showGrayedOut ? ColorTheme.GetDisabledPurple( this.Element.LastHadMouseWithin ) : ColorTheme.GetBasicLightTextPurple( this.Element.LastHadMouseWithin )) );

                Buffer.AddRaw( NameFromLang ).EndColor();
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                if ( countToShow >= 0 )
                {
                    this.SetRelatedImage1EnabledIfNeeded( true );
                    Buffer.AddRaw( countToShow.ToString() );
                }
                else
                    this.SetRelatedImage1EnabledIfNeeded( false );
            }

            public override bool GetShouldBeHidden() => displayType == ResourcesDisplayType.Length;

            public override void Clear()
            {
                displayType = ResourcesDisplayType.Length;
            }
        }
        #endregion

        #region bDataFullItem
        public class bDataFullItem : ButtonAbstractBase
        {
            public static bDataFullItem Original;
            public bDataFullItem() { if ( Original == null ) Original = this; }

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

        #region bDataAdjustedItem
        public class bDataAdjustedItem : ButtonAbstractBase
        {
            public static bDataAdjustedItem Original;
            public bDataAdjustedItem() { if ( Original == null ) Original = this; }

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

        #region bDataHeader
        public class bDataHeader : ButtonAbstractBase
        {
            public static bDataHeader Original;
            public bDataHeader() { if ( Original == null ) Original = this; }

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

        #region bResourceItem
        public class bResourceItem : ButtonAbstractBase
        {
            public static bResourceItem Original;
            public bResourceItem() { if ( Original == null ) Original = this; }

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

        #region bUnitType
        public class bUnitType : ButtonAbstractBase
        {
            public static bUnitType Original;
            public bUnitType() { if ( Original == null ) Original = this; }

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

        #region bLargeTwoLineHeader
        public class bLargeTwoLineHeader : ButtonAbstractBase
        {
            public static bLargeTwoLineHeader Instance;
            public bLargeTwoLineHeader() { if ( Instance == null ) Instance = this; }

            public GetOrSetUIData UIDataController;

            public override void OnUpdateSub() { }

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
    }
}

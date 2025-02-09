using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_PlayerHardware : ToggleableWindowController, IInputActionHandler
    {
        #region Main Controller
        public static Window_PlayerHardware Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_PlayerHardware()
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
            UnitTypes_sortedActors.Clear();

            Consumables_FilterText = string.Empty;
            UnitTypes_FilterText = string.Empty;
            UnitTypes_limitToActor = UnlockedActorType.All;

            bool resetFilterText = true;

            if ( resetFilterText )
            {
                if ( iFilterText.Instance != null )
                    iFilterText.Instance.SetText( string.Empty );
            }

            //leave these alone!
            //UnitTypes_sortAndFilter = null;
            //UnitTypes_panelDisplayStyle = null;

            base.OnOpen();
        }

        private static string Consumables_FilterText = string.Empty;
        private static string Consumables_LastFilterText = string.Empty;
        private static int Consumables_lastTurn = 0;

        private static UIX_UnlockedActorDataPanelDisplayStyle UnitTypes_panelDisplayStyle = null;
        private static UIX_UnlockedActorDataPanelDisplayStyle UnitTypes_LastPanelDisplayStyle = null;

        private static string UnitTypes_FilterText = string.Empty;
        private static string UnitTypes_LastFilterText = string.Empty;
        private static int UnitTypes_lastTurn = 0;
        private static UnlockedActorType UnitTypes_limitToActor = UnlockedActorType.All;
        private static UIX_UnlockedActorDataSortAndFilter UnitTypes_sortAndFilter = null;
        private static UIX_UnlockedActorDataSortAndFilter UnitTypes_LastSetSortAndFilter = null;
        private static float UnitTypes_LastSetSortAndFilterSet = 0f;
        private static readonly List<UnlockedActorData> UnitTypes_sortedActors = List<UnlockedActorData>.Create_WillNeverBeGCed( 40, "Window_PlayerHardware-UnitTypes_sortedActors" );

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public static HardwareDisplayType currentlyRequestedDisplayType = HardwareDisplayType.UnitTypeOverview;

            public customParent()
            {
                Window_PlayerHardware.CustomParentInstance = this;
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


                // Categories are mostly static.
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }

                for ( HardwareDisplayType displayType = HardwareDisplayType.UnitTypeOverview; displayType < HardwareDisplayType.Length; displayType++ )
                {
                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( item == null )
                        break;
                    item.Assign( displayType );
                }

                float border = 6.1f;
                float width = 1109.97f;
                width -= (border + border);
                width -= (217f + 5f) * 3;
                width *= 0.5f;

                float currentX = width;
                bCategoryPool.ApplyItemsInColumns( ref currentX, -5f, 217f + 5f, 217f, 25.6f, false );
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
                        case HardwareDisplayType.Consumables:
                            OnUpdate_Content_Consumables();
                            break;
                        case HardwareDisplayType.UnitTypeOverview:
                            OnUpdate_Content_UnitTypeOverview();
                            break;
                        case HardwareDisplayType.UnitTypeAnalysis:
                            OnUpdate_Content_UnitTypeAnalysis();
                            break;
                        case HardwareDisplayType.Length:
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( $"Error! Player Hardware tried to render with a category of {currentlyRequestedDisplayType.ToString()}, this should be impossible. Acting as though it's requesting Primary Resources instead.", Verbosity.ShowAsError );
                            currentlyRequestedDisplayType = HardwareDisplayType.UnitTypeOverview;
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
            private const float UNIT_ITEM_HEIGHT = 50f;


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

            private const float SEXTUPLE_WIDTH = 177f; //166.4
            private const float SEXTUPLE_WIDTH_SPACING = 4f;

            private const float SEXTUPLE_WIDTH_COL0 = leftBuffer;
            private const float SEXTUPLE_WIDTH_COL1 = SEXTUPLE_WIDTH_COL0 + SEXTUPLE_WIDTH + SEXTUPLE_WIDTH_SPACING;
            private const float SEXTUPLE_WIDTH_COL2 = SEXTUPLE_WIDTH_COL1 + SEXTUPLE_WIDTH + SEXTUPLE_WIDTH_SPACING;
            private const float SEXTUPLE_WIDTH_COL3 = SEXTUPLE_WIDTH_COL2 + SEXTUPLE_WIDTH + SEXTUPLE_WIDTH_SPACING;
            private const float SEXTUPLE_WIDTH_COL4 = SEXTUPLE_WIDTH_COL3 + SEXTUPLE_WIDTH + SEXTUPLE_WIDTH_SPACING;
            private const float SEXTUPLE_WIDTH_COL5 = SEXTUPLE_WIDTH_COL4 + SEXTUPLE_WIDTH + SEXTUPLE_WIDTH_SPACING;

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

            #region CalculateBoundsSextupleColumn
            protected void CalculateBoundsSextupleColumn( out Rect soleBounds, int CurrentColumn, float innerY, float HeightUsed )
            {
                float xPos;
                switch ( CurrentColumn )
                {
                    case 0:
                        xPos = SEXTUPLE_WIDTH_COL0;
                        break;
                    case 1:
                        xPos = SEXTUPLE_WIDTH_COL1;
                        break;
                    case 2:
                        xPos = SEXTUPLE_WIDTH_COL2;
                        break;
                    case 3:
                        xPos = SEXTUPLE_WIDTH_COL3;
                        break;
                    case 4:
                        xPos = SEXTUPLE_WIDTH_COL4;
                        break;
                    case 5:
                        xPos = SEXTUPLE_WIDTH_COL5;
                        break;
                    default:
                        throw new Exception( "CalculateBoundsSextupleColumn: Asked for invalid column " + CurrentColumn + "!" );
                }

                soleBounds = ArcenFloatRectangle.CreateUnityRect( xPos, innerY, SEXTUPLE_WIDTH, HeightUsed );
            }
            #endregion

            #region OnUpdate_Content_Consumables
            private void OnUpdate_Content_Consumables()
            {
                #region Consumable Totals
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
                                    ExtraData.Buffer.AddLang( "Inventory_Consumables" );
                                    break;
                                case UIAction.GetOtherTextToShowFromVolatile:

                                    int discoveredConsumableCount = 0;
                                    int totalConsumableCount = 0;
                                    foreach ( ResourceConsumable consumable in ResourceConsumableTable.SortedConsumables )
                                    {
                                        totalConsumableCount++;
                                        if ( !consumable.DuringGame_IsUnlocked() )
                                            continue;
                                        discoveredConsumableCount++;
                                    }

                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "DiscoveredConsumables" )
                                        .AddRaw( discoveredConsumableCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
                                    ExtraData.Buffer.Space8x();
                                    ExtraData.Buffer.AddLangAndAfterLineItemHeader( "TotalConsumables" )
                                        .AddRaw( totalConsumableCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( element.LastHadMouseWithin ) );
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

                bool hasFilterChanged = !( Consumables_FilterText == Consumables_LastFilterText && Consumables_lastTurn == SimCommon.Turn);
                Consumables_LastFilterText = Consumables_FilterText;
                Consumables_lastTurn = SimCommon.Turn;

                //do the resources in alphabetical order
                foreach ( ResourceConsumable consumable in ResourceConsumableTable.SortedConsumables )
                {
                    if ( !consumable.DuringGame_IsUnlocked() )
                        continue;

                    if ( hasFilterChanged )
                    {
                        if ( Consumables_FilterText.IsEmpty() )
                            consumable.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            consumable.DuringGame_HasBeenFilteredOutInInventory = !consumable.GetMatchesSearchString( Consumables_FilterText );
                    }
                    if ( consumable.DuringGame_HasBeenFilteredOutInInventory )
                        continue;

                    if ( !HandleSpecificConsumable( consumable, ref currentColumn ) )
                        continue; //time-slicing
                }

                if ( currentColumn != 0 ) //move down if the last row wasn't completely full
                {
                    this.CalculateBoundsSextupleColumn( out Rect bounds, 0, runningY, RESOURCE_ITEM_HEIGHT );
                    runningY -= (bounds.height + CONTENT_ROW_GAP_RESOURCE_ITEM);
                }
            }
            #endregion

            #region HandleSpecificConsumable
            private bool HandleSpecificConsumable( ResourceConsumable consumable, ref int CurrentColumn )
            {
                this.CalculateBoundsSextupleColumn( out Rect bounds, CurrentColumn, runningY, RESOURCE_ITEM_HEIGHT );
                CurrentColumn++;
                if ( CurrentColumn > 5 )
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

                row.Assign( "Consumable", consumable.ID, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                row.SetRelatedImage1SpriteIfNeeded( consumable.Icon.GetSpriteForUI() );
                                row.SetRelatedImage1ColorFromHexIfNeeded( string.Empty );
                                row.SetRelatedImage2EnabledIfNeeded( false );

                                ExtraData.Buffer.AddRaw( consumable.GetDisplayName(), ColorTheme.GetBasicLightTextBlue( element.LastHadMouseWithin ) );
                            }
                            break;
                        case UIAction.GetOtherTextToShowFromVolatile:
                            switch ( ExtraData.Int )
                            {
                                case 0:
                                    {

                                        int maxCould = consumable.CalculateMaxCouldCreate( false );
                                        int actualCould = consumable.CalculateMaxCouldCreate( true );
                                        string nameColor = maxCould > 0 && actualCould > 0 ? ColorTheme.GetCanAfford( element.LastHadMouseWithin ) : ColorTheme.GetCannotAfford( element.LastHadMouseWithin );
                                        ExtraData.Buffer.AddLangAndAfterLineItemHeader( "CouldCreate", ColorTheme.GetDisabledBlue( element.LastHadMouseWithin ) );
                                        ExtraData.Buffer.AddRaw( maxCould.ToStringThousandsWhole(), nameColor );
                                    }
                                    break;
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                consumable.RenderConsumableTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, null, TooltipInstruction.ForConstruction, TooltipExtraText.None, TooltipExtraRules.None );
                            }
                            break;
                        case UIAction.OnClick:
                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                            break;
                    }
                } );
                return true;
            }
            #endregion

            #region OnUpdate_Content_UnitTypeOverview
            private void OnUpdate_Content_UnitTypeOverview()
            {
                bLargeTwoLineHeader.Instance?.Assign( null );

                if ( UnitTypes_panelDisplayStyle == null )
                    UnitTypes_panelDisplayStyle = UIX_UnlockedActorDataPanelDisplayStyleTable.Instance.Rows[0];

                UIX_UnlockedActorDataPanelDisplayStyle displayStyle = UnitTypes_panelDisplayStyle;

                bool hasFilterChanged = !( UnitTypes_FilterText == UnitTypes_LastFilterText && UnitTypes_lastTurn == SimCommon.Turn && displayStyle == UnitTypes_LastPanelDisplayStyle );
                UnitTypes_LastFilterText = UnitTypes_FilterText;
                UnitTypes_lastTurn = SimCommon.Turn;
                UnitTypes_LastPanelDisplayStyle = displayStyle;

                int currentColumn = 0;
                UnlockedActorType currentActorType = UnlockedActorType.Length;
                foreach ( UnlockedActorData actorData in SimCommon.UnlockedActorTypes.GetDisplayList() )
                {
                    if ( hasFilterChanged )
                    {
                        if ( UnitTypes_FilterText.IsEmpty() )
                            actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory = !actorData.DuringGameData.GetMatchesSearchString( UnitTypes_FilterText );

                        if ( !displayStyle.Implementation.ShouldBeShown_UIXDisplayStyle( actorData, displayStyle ) )
                            actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory = true;
                    }
                    if ( actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory )
                        continue;
                    if ( UnitTypes_limitToActor != UnlockedActorType.All && actorData.ActorType != UnitTypes_limitToActor )
                        continue;

                    if ( currentActorType != actorData.ActorType )
                    {
                        currentActorType = actorData.ActorType;

                        if ( currentColumn != 0 ) //move down if the last row wasn't completely full, and mark a newline
                        {
                            this.CalculateBoundsSextupleColumn( out Rect bounds, 0, runningY, UNIT_ITEM_HEIGHT );
                            runningY -= (bounds.height + CONTENT_ROW_GAP_UNIT_ITEM);
                            currentColumn = 0;
                        }

                        //one extra line for each header
                        runningY -= 10f;

                        #region OtherType Totals
                        {
                            UnlockedActorType otherActorType = currentActorType;
                            string otherTypeIDKey;
                            string otherTypeTotalsKey;
                            switch ( otherActorType )
                            {
                                case UnlockedActorType.Android:
                                    otherTypeIDKey = "Androids";
                                    otherTypeTotalsKey = "DiscoveredAndroids";
                                    break;
                                case UnlockedActorType.BulkAndroid:
                                    otherTypeIDKey = "BulkAndroids";
                                    otherTypeTotalsKey = "DiscoveredBulkAndroids";
                                    break;
                                case UnlockedActorType.Vehicle:
                                    otherTypeIDKey = "Vehicles";
                                    otherTypeTotalsKey = "DiscoveredVehicles";
                                    break;
                                default:
                                case UnlockedActorType.Mech:
                                    otherTypeIDKey = "Mechs";
                                    otherTypeTotalsKey = "DiscoveredMechs";
                                    break;
                            }

                            bool render = true;
                            this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                            runningY -= CONTENT_ROW_GAP_UNIT_ITEM;

                            if ( bounds.yMax < minYToShow )
                                render = false; //it's scrolled up far enough we can skip it, yay!
                            if ( bounds.yMax > maxYToShow )
                                render = false; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                            bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                            if ( row == null )
                                render = false;
                            if ( render )
                            {
                                bDataFullItemPool.ApplySingleItemInRow( row, bounds, false );

                                row.Assign( "Totals", otherTypeIDKey, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                                {
                                    switch ( Action )
                                    {
                                        case UIAction.GetTextToShowFromVolatile:
                                            {
                                                bool isHovered = element.LastHadMouseWithin;

                                                int otherCount = 0;
                                                foreach ( UnlockedActorData otherActorData in SimCommon.UnlockedActorTypes.GetDisplayList() )
                                                {
                                                    if ( otherActorData.ActorType == otherActorType )
                                                        otherCount++;
                                                }

                                                ExtraData.Buffer.StartSize80().AddNeverTranslated( "<uppercase>", false );
                                                ExtraData.Buffer.AddLangAndAfterLineItemHeader( otherTypeTotalsKey, ColorTheme.GetDataBlue( isHovered ) )
                                                    .AddRaw( otherCount.ToStringThousandsWhole(), ColorTheme.GetDataBlue( isHovered ) );
                                            }
                                            break;
                                        case UIAction.OnClick:
                                            break;
                                    }
                                } );
                            }
                        }
                        #endregion
                    }

                    if ( !HandleSpecificUnitType( actorData, ref currentColumn ) )
                        continue; //time-slicing
                }

                if ( currentColumn != 0 ) //move down if the last row wasn't completely full
                {
                    this.CalculateBoundsSextupleColumn( out Rect bounds, 0, runningY, UNIT_ITEM_HEIGHT );
                    runningY -= (bounds.height + CONTENT_ROW_GAP_UNIT_ITEM);
                }
            }
            #endregion

            #region OnUpdate_Content_UnitTypeAnalysis
            private void OnUpdate_Content_UnitTypeAnalysis()
            {
                bLargeTwoLineHeader.Instance?.Assign( null );

                bool hasFilterChanged = !(UnitTypes_FilterText == UnitTypes_LastFilterText && UnitTypes_lastTurn == SimCommon.Turn);
                UnitTypes_LastFilterText = UnitTypes_FilterText;
                UnitTypes_lastTurn = SimCommon.Turn;

                if ( UnitTypes_sortAndFilter == null )
                    UnitTypes_sortAndFilter = UIX_UnlockedActorDataSortAndFilterTable.Instance.Rows[0];

                UIX_UnlockedActorDataSortAndFilter filter = UnitTypes_sortAndFilter;
                if ( UnitTypes_LastSetSortAndFilter != filter ||
                    (ArcenTime.AnyTimeSinceStartF - UnitTypes_LastSetSortAndFilterSet) >= 4f ) //even if nothing has changed, recalculate it every 4 seconds
                {
                    UnitTypes_LastSetSortAndFilter = filter;
                    UnitTypes_LastSetSortAndFilterSet = ArcenTime.AnyTimeSinceStartF;

                    UnitTypes_sortedActors.Clear();
                    foreach ( UnlockedActorData actorData in SimCommon.UnlockedActorTypes.GetDisplayList() )
                    {
                        if ( !filter.Implementation.ShouldBeShown_UIXSortAndFilter( actorData, filter ) )
                            continue; //skip!
                        UnitTypes_sortedActors.Add( actorData );
                    }

                    filter.Implementation.SortList_UIXSortAndFilter( UnitTypes_sortedActors, filter );
                }

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

                        row.Assign( "Totals", "UnitTypeAnalysis", delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                        {
                            switch ( Action )
                            {
                                case UIAction.GetTextToShowFromVolatile:
                                    {
                                        UnitTypes_sortAndFilter.Implementation.WriteTabularLineOrHeader( null, ExtraData.Buffer, filter, true );
                                    }
                                    break;
                                case UIAction.OnClick:
                                    break;
                            }
                        } );
                    }
                }

                foreach ( UnlockedActorData actorData in UnitTypes_sortedActors )
                {
                    if ( hasFilterChanged )
                    {
                        if ( UnitTypes_FilterText.IsEmpty() )
                            actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory = false;
                        else
                            actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory = !actorData.DuringGameData.GetMatchesSearchString( UnitTypes_FilterText );
                    }
                    if ( actorData.DuringGameData.DuringGame_HasBeenFilteredOutInInventory )
                        continue;
                    if ( UnitTypes_limitToActor != UnlockedActorType.All && actorData.ActorType != UnitTypes_limitToActor )
                        continue;

                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );

                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bDataFullItem row = bDataFullItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bDataFullItemPool.ApplySingleItemInRow( row, bounds, false );

                    row.Assign( "UnlockedActorDataTab", actorData.DuringGameData.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    if ( filter != null && actorData != null )
                                        filter.Implementation.WriteTabularLineOrHeader( actorData, ExtraData.Buffer, filter, false );
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( actorData.DuringGameData.ParentUnitTypeOrNull != null )
                                        actorData.DuringGameData.ParentUnitTypeOrNull.RenderUnitTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                                    else if ( actorData.DuringGameData.ParentVehicleTypeOrNull != null )
                                        actorData.DuringGameData.ParentVehicleTypeOrNull.RenderVehicleTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                                    else if ( actorData.DuringGameData.ParentNPCUnitTypeOrNull != null )
                                        actorData.DuringGameData.ParentNPCUnitTypeOrNull.RenderNPCUnitTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                                }
                                break;
                            case UIAction.OnClick:
                                Window_ActorCustomizeLoadout.Instance.OpenFromDuringGameData( actorData.DuringGameData );
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region HandleSpecificUnitType
            private bool HandleSpecificUnitType( UnlockedActorData actorData, ref int CurrentColumn )
            {
                this.CalculateBoundsSextupleColumn( out Rect bounds, CurrentColumn, runningY, UNIT_ITEM_HEIGHT );
                CurrentColumn++;
                if ( CurrentColumn > 5 )
                {
                    CurrentColumn = 0;
                    runningY -= (bounds.height + CONTENT_ROW_GAP_UNIT_ITEM);
                }

                if ( bounds.yMax < minYToShow )
                    return true; //it's scrolled up far enough we can skip it, yay!
                if ( bounds.yMax > maxYToShow )
                    return true; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                bUnitType row = bUnitTypePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( row == null )
                    return false;
                bUnitTypePool.ApplySingleItemInRow( row, bounds, false );

                row.Assign( "UnlockedActorData", actorData.DuringGameData.GetID(), delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                int currentlyExtant = actorData.DuringGameData.ActorsOfThisType.GetDisplayList().Count;

                                row.SetRelatedImage1SpriteIfNeeded( actorData.DuringGameData.GetTooltipIcon().GetSpriteForUI() );
                                row.SetRelatedImage2SpriteIfNeeded( actorData.DuringGameData.GetShapeIcon().GetSpriteForUI() );
                                row.SetRelatedImage3EnabledIfNeeded( currentlyExtant > 0 );

                                string color = ColorTheme.GetBasicLightTextBlue( element.LastHadMouseWithin );
                                ExtraData.Buffer.AddRaw( actorData.Name, color ).EndSize();

                            }
                            break;
                        case UIAction.GetOtherTextToShowFromVolatile:
                            switch ( ExtraData.Int )
                            {
                                case 0:
                                    {
                                        bool isHovered = element.LastHadMouseWithin;
                                        ExtraData.Buffer.AddRaw( actorData.DuringGameData.GetShortDescription(), ColorTheme.GetBasicLightTextBlue( isHovered ) );
                                    }
                                    break;
                                case 1:
                                    {
                                        bool isHovered = element.LastHadMouseWithin;
                                        UnitTypes_panelDisplayStyle?.Implementation.WriteSecondLine( actorData, ExtraData.Buffer, UnitTypes_panelDisplayStyle, isHovered );
                                    }
                                    break;
                                case 2:
                                    {
                                        int currentlyExtant = actorData.DuringGameData.ActorsOfThisType.GetDisplayList().Count;
                                        if ( currentlyExtant > 0 )
                                            ExtraData.Buffer.AddRaw( currentlyExtant.ToString() );
                                    }
                                    break;
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                if ( actorData.DuringGameData.ParentUnitTypeOrNull != null )
                                    actorData.DuringGameData.ParentUnitTypeOrNull.RenderUnitTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                                else if ( actorData.DuringGameData.ParentVehicleTypeOrNull != null )
                                    actorData.DuringGameData.ParentVehicleTypeOrNull.RenderVehicleTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                                else if ( actorData.DuringGameData.ParentNPCUnitTypeOrNull != null )
                                    actorData.DuringGameData.ParentNPCUnitTypeOrNull.RenderNPCUnitTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, TooltipInstruction.ForActorCustomization, TooltipExtraText.None, TooltipExtraRules.None );
                            }
                            break;
                        case UIAction.OnOtherTextHyperlinkHover:
                            if ( ExtraData.Int == 1)
                            {
                                UnitTypes_panelDisplayStyle?.Implementation.HandleSecondLineHyperlink( actorData, UnitTypes_panelDisplayStyle, element, ExtraData.LinkData );
                            }
                            break;
                        case UIAction.OnClick:
                            Window_ActorCustomizeLoadout.Instance.OpenFromDuringGameData( actorData.DuringGameData );
                            break;
                    }
                } );
                return true;
            }
            #endregion
        }
        #endregion

        #region Supporting Elements
        public enum HardwareDisplayType
        {
            UnitTypeOverview,
            UnitTypeAnalysis,
            Consumables,
            Length
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "HeaderBar_Hardware_Top" );
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

                if ( Item.GetItem() is ArbitraryArcenOption actorType )
                    UnitTypes_limitToActor = (UnlockedActorType)actorType.NumericIndex;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HardwareDisplayType.UnitTypeAnalysis:
                    case HardwareDisplayType.UnitTypeOverview:
                        #region UnitTypes
                        {
                            UnlockedActorType typeDataToSelect = UnitTypes_limitToActor;

                            bool foundMismatch = false;
                            if ( (elementAsType.CurrentlySelectedOption == null || !(elementAsType.CurrentlySelectedOption.GetItem() is ArbitraryArcenOption currentSel) ||
                                currentSel.NumericIndex != (int)typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( (int)UnlockedActorType.Length != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( UnlockedActorType i = UnlockedActorType.All; i < UnlockedActorType.Length; i++ )
                                {
                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[(int)i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    if ( !(option.GetItem() is ArbitraryArcenOption existing) )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    if ( (int)i == existing.NumericIndex )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( UnlockedActorType i = UnlockedActorType.All; i < UnlockedActorType.Length; i++ )
                                {
                                    ArbitraryArcenOption option;
                                    switch ( i )
                                    {
                                        case UnlockedActorType.Android:
                                            option.DisplayNameLangKey = "Androids";
                                            break;
                                        case UnlockedActorType.BulkAndroid:
                                            option.DisplayNameLangKey = "BulkAndroidSquads";
                                            break;
                                        case UnlockedActorType.Vehicle:
                                            option.DisplayNameLangKey = "Vehicles";
                                            break;
                                        case UnlockedActorType.Mech:
                                            option.DisplayNameLangKey = "Mechs";
                                            break;
                                        default:
                                            option.DisplayNameLangKey = "AllUnitTypes";
                                            break;
                                    }
                                    option.DescriptionLangKey = string.Empty;
                                    option.NumericIndex = (int)i;
                                    option.NumericIndexAsString = ((int)i).ToString();
                                    elementAsType.AddItem( option, i == typeDataToSelect );
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
                        case HardwareDisplayType.UnitTypeAnalysis:
                        case HardwareDisplayType.UnitTypeOverview:
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

                if ( Item.GetItem() is UIX_UnlockedActorDataSortAndFilter actorSortAndFilter )
                {
                    UnitTypes_sortAndFilter = actorSortAndFilter;
                }
                else if ( Item.GetItem() is UIX_UnlockedActorDataPanelDisplayStyle actorPanelDisplayStyle )
                {
                    UnitTypes_panelDisplayStyle = actorPanelDisplayStyle;
                }
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HardwareDisplayType.UnitTypeAnalysis:
                        #region UnitTypeAnalysis
                        {
                            UIX_UnlockedActorDataSortAndFilter[] validOptions = UIX_UnlockedActorDataSortAndFilterTable.Instance.Rows;

                            UIX_UnlockedActorDataSortAndFilter typeDataToSelect = UnitTypes_sortAndFilter;

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
                            if ( typeDataToSelect == null && validOptions.Length > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as UIX_UnlockedActorDataSortAndFilter != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Length != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    UIX_UnlockedActorDataSortAndFilter row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    UIX_UnlockedActorDataSortAndFilter optionItemAsType = option.GetItem() as UIX_UnlockedActorDataSortAndFilter;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    UIX_UnlockedActorDataSortAndFilter row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
                    case HardwareDisplayType.UnitTypeOverview:
                        #region UnitTypeOverview
                        {
                            UIX_UnlockedActorDataPanelDisplayStyle[] validOptions = UIX_UnlockedActorDataPanelDisplayStyleTable.Instance.Rows;

                            UIX_UnlockedActorDataPanelDisplayStyle typeDataToSelect = UnitTypes_panelDisplayStyle;

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
                            if ( typeDataToSelect == null && validOptions.Length > 0 )
                                typeDataToSelect = validOptions[0];
                            #endregion

                            bool foundMismatch = false;
                            if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null ||
                                elementAsType.CurrentlySelectedOption.GetItem() as UIX_UnlockedActorDataPanelDisplayStyle != typeDataToSelect) )
                            {
                                foundMismatch = true;
                                //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                            }
                            else if ( validOptions.Length != elementAsType.GetItems_DoNotAlterDirectly().Count )
                                foundMismatch = true;
                            else
                            {
                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    UIX_UnlockedActorDataPanelDisplayStyle row = validOptions[i];

                                    IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                    if ( option == null )
                                    {
                                        foundMismatch = true;
                                        break;
                                    }
                                    UIX_UnlockedActorDataPanelDisplayStyle optionItemAsType = option.GetItem() as UIX_UnlockedActorDataPanelDisplayStyle;
                                    if ( row == optionItemAsType )
                                        continue;
                                    foundMismatch = true;
                                    break;
                                }
                            }

                            if ( foundMismatch )
                            {
                                elementAsType.ClearItems();

                                for ( int i = 0; i < validOptions.Length; i++ )
                                {
                                    UIX_UnlockedActorDataPanelDisplayStyle row = validOptions[i];
                                    elementAsType.AddItem( row, row == typeDataToSelect );
                                }
                            }
                        }
                        #endregion
                        break;
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
                        case HardwareDisplayType.UnitTypeAnalysis:
                        case HardwareDisplayType.UnitTypeOverview:
                            return false;
                    }
                    return true;
                }
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
                    case HardwareDisplayType.Consumables:
                        Consumables_FilterText = newString;
                        break;
                    case HardwareDisplayType.UnitTypeAnalysis:
                    case HardwareDisplayType.UnitTypeOverview:
                        UnitTypes_FilterText = newString;
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
                        case HardwareDisplayType.Consumables:
                        case HardwareDisplayType.UnitTypeAnalysis:
                        case HardwareDisplayType.UnitTypeOverview:
                            return false;
                    }
                    return true;
                }
            }

            public override void OnEndEdit()
            {
                switch ( customParent.currentlyRequestedDisplayType )
                {
                    case HardwareDisplayType.Consumables:
                        Consumables_FilterText = this.GetText();
                        break;
                    case HardwareDisplayType.UnitTypeAnalysis:
                    case HardwareDisplayType.UnitTypeOverview:
                        UnitTypes_FilterText = this.GetText();
                        break;
                }
            }

            private ArcenUI_Input inputField = null;
            private HardwareDisplayType lastDisplayType = HardwareDisplayType.Length;

            public override void OnMainThreadUpdate()
            {
                if ( lastDisplayType != customParent.currentlyRequestedDisplayType )
                {
                    switch ( customParent.currentlyRequestedDisplayType )
                    {
                        case HardwareDisplayType.Consumables:
                            this.SetText( Consumables_FilterText );
                            break;
                        case HardwareDisplayType.UnitTypeAnalysis:
                        case HardwareDisplayType.UnitTypeOverview:
                            this.SetText( UnitTypes_FilterText );
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

            public HardwareDisplayType displayType = HardwareDisplayType.Length;
            private string NameFromLang => displayType == HardwareDisplayType.Length ? LangCommon.None.Text :
                Lang.Get( $"Inventory_{displayType.ToString()}" );
            private string DescriptionFromLang => displayType == HardwareDisplayType.Length ? LangCommon.None.Text :
                Lang.Get( $"Inventory_{displayType.ToString()}Description" );

            public void Assign( HardwareDisplayType displayTypeCity )
            {
                this.displayType = displayTypeCity;
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( displayType != HardwareDisplayType.Length )
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
                switch ( this.displayType )
                {
                    case HardwareDisplayType.Consumables:
                        {
                            int discoveredConsumableCount = 0;
                            foreach ( ResourceConsumable consumable in ResourceConsumableTable.SortedConsumables )
                            {
                                if ( !consumable.DuringGame_IsUnlocked() )
                                    continue;
                                discoveredConsumableCount++;
                            }

                            countToShow = discoveredConsumableCount;
                            showGrayedOut = countToShow == 0;
                            countToShow = -1;
                        }
                        break;
                    case HardwareDisplayType.UnitTypeAnalysis:
                    case HardwareDisplayType.UnitTypeOverview:
                        countToShow = SimCommon.UnlockedActorTypes.Count;
                        showGrayedOut = countToShow == 0;
                        countToShow = -1;
                        break;
                    case HardwareDisplayType.Length:
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

            public override bool GetShouldBeHidden() => displayType == HardwareDisplayType.Length;

            public override void Clear()
            {
                displayType = HardwareDisplayType.Length;
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

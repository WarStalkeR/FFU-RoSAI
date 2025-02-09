using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorPalette : WindowControllerAbstractBase
    {
        public static Window_LevelEditorPalette Instance;
        public Window_LevelEditorPalette()
        {
            Instance = this;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public static bool IsShowingSceneObjectListInstead = false;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false;
            return !IsShowingSceneObjectListInstead;
        }

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_LevelEditorPalette.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bLevelEditorItemPool globalItemPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LevelEditorPalette.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0.2f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0.25f;

                        if ( bLevelEditorItem.Original != null )
                        {
                            hasGlobalInitialized = true;
                            globalItemPool = new bLevelEditorItemPool( 20 );
                        }
                    }
                    #endregion
                }

                bLevelEditorItemPool.ClearAll( 10 );
                globalItemPool.ClearSingle();

                RectTransform rTran = null;
                float currentY = 0;

                #region Items In The Current Palette
                A5LevelItemCategory categoryFromPalette = CurrentPalettencounter?.GetCategory();
                if ( categoryFromPalette && categoryFromPalette.Items.Length > 0 )
                {
                    foreach ( A5LevelItem item in categoryFromPalette.Items )
                    {
                        if ( item == null )
                            continue; //if nothing to show, then skip it

                        if ( globalItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds_Icon( item ) == null )
                            break; //this would be time-slicing
                    }

                    //globalItemPool.Sort(); //we could if we want, but I don't see a need to sort it
                    ApplyLevelItemsInGrid_Icon( ref currentY, globalItemPool.GetInUseList_Icon() );
                }
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                rTran = (RectTransform)bLevelEditorItem.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region ApplyLevelItemsInGrid_Icon
            private void ApplyLevelItemsInGrid_Icon( ref float currentY, List<bLevelEditorItem> items )
            {
                bLevelEditorItem item;
                int currentColumn = 0;
                RectTransform rTran;
                for ( int i = 0; i < items.Count; i++ )
                {
                    item = items[i];

                    //ArcenDebugging.ArcenDebugLogSingleLine( "Item to sort: " + item?.ItemData.DisplayName + item.GetShouldBeHidden(), Verbosity.DoNotShow );

                    if ( item.GetShouldBeHidden() )
                        continue;

                    if ( currentColumn >= COLUMN_COUNT )
                    {
                        currentColumn = 0;
                        currentY -= ROW_ADVANCE_ICON;
                    }

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( currentColumn * COLUMN_ADVANCE_ICON, currentY );
                    rTran.localScale = Vector3.one;
                    rTran.localRotation = Quaternion.identity;
                    rTran.pivot = new Vector2( 0, 1 );
                    //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    ((RectTransform)rTran.GetChild( 0 )).sizeDelta = new Vector3( 30, 30 );
                    rTran.GetChild( 1 ).localPosition = new Vector3( 32f, 11.07f - HEIGHT_ICON_FULL, 0f );
                    currentColumn++;
                }

                //if we came partly across, then make us go down another row
                if ( currentColumn > 0 )
                    currentY -= ROW_ADVANCE_ICON;

                //currentY -= ROW_ADVANCE;
            }
            #endregion
        }

        public static LevelEditorPaletteGroup CurrentPalettencounter = null;

        public const int COLUMN_COUNT = 1;

        public const float WIDTH_ICON = 153.6f;
        public const float HEIGHT_ICON = 19.1f;
        public const float HEIGHT_ICON_FULL = 22.1f;
        public const float COLUMN_ADVANCE_ICON = WIDTH_ICON + 2f;
        public const float ROW_ADVANCE_ICON = HEIGHT_ICON + 2f;

        #region dPaletteCategory
        public class dPaletteCategory : DropdownAbstractBase
        {
            public static dPaletteCategory Instance;
            public dPaletteCategory()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                //set this locally for the current player only
                LevelEditorPaletteGroup ItemAsType = (LevelEditorPaletteGroup)Item.GetItem();
                CurrentPalettencounter = ItemAsType;
            }

            private static readonly List<LevelEditorPaletteGroup> validListOfGroups = List<LevelEditorPaletteGroup>.Create_WillNeverBeGCed( 200, "dPaletteCategory-validListOfGroups", 200 );

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                LevelType levelType = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;

                LevelEditorPaletteGroupTable.Instance.FillListMatchingLevelTypeConstraints( levelType, validListOfGroups, out LevelEditorPaletteGroup DefaultRow );

                LevelEditorPaletteGroup typeDataToSelect = CurrentPalettencounter;

                #region If The Selected Type Is Not Valid Right Now, Then Skip It
                if ( typeDataToSelect != null )
                {
                    if ( !validListOfGroups.Contains( typeDataToSelect ) )
                    {
                        typeDataToSelect = null;
                        CurrentPalettencounter = null;
                    }
                }
                #endregion

                #region Select Default If Blank
                if ( typeDataToSelect == null && DefaultRow != null )
                    typeDataToSelect = DefaultRow;
                if ( typeDataToSelect == null && validListOfGroups.Count > 0 )
                    typeDataToSelect = validListOfGroups[0];
                #endregion

                if ( CurrentPalettencounter == null && typeDataToSelect != null )
                    CurrentPalettencounter = typeDataToSelect;

                bool foundMismatch = false;
                if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (LevelEditorPaletteGroup)elementAsType.CurrentlySelectedOption.GetItem() != typeDataToSelect) )
                {
                    foundMismatch = true;
                    //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                }
                else if ( validListOfGroups.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                    foundMismatch = true;
                else
                {
                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        LevelEditorPaletteGroup row = validListOfGroups[i];

                        IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                        if ( option == null )
                        {
                            foundMismatch = true;
                            break;
                        }
                        LevelEditorPaletteGroup optionItemAsType = (LevelEditorPaletteGroup)option.GetItem();
                        if ( row == optionItemAsType )
                            continue;
                        foundMismatch = true;
                        break;
                    }
                }

                if ( foundMismatch )
                {
                    elementAsType.ClearItems();

                    for ( int i = 0; i < validListOfGroups.Count; i++ )
                    {
                        LevelEditorPaletteGroup row = validListOfGroups[i];
                        elementAsType.AddItem( row, row == typeDataToSelect );
                    }
                }
            }
            public override void HandleMouseover()
            {
                string mouseoverText = "Choose which palette group to select new items from.";
                LevelEditorPaletteGroup typeDataToSelect = CurrentPalettencounter;
                if ( typeDataToSelect != null )
                {
                    mouseoverText += "\n\nCurrently: <color=#7ab9ff>" + typeDataToSelect.GetDisplayName() + "</color>";
                }
                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, LocalizedString.AddNeverTranslated_New( mouseoverText ), TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }
            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                LevelEditorPaletteGroup ItemAsType = (LevelEditorPaletteGroup)Item.GetItem();
                Window_LevelEditorTooltip.bPanel.Instance.SetText( ItemElement, LocalizedString.AddNeverTranslated_New( "Choose which palette group to select new items from.\n\n<color=#ffc87a>" +
                    ItemAsType.GetDisplayName() + "</color>" ), TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }
        }
        #endregion

        #region bLevelEditorItem
        public class bLevelEditorItem : ImageButtonAbstractBase
        {
            public static bLevelEditorItem Original;
            public bLevelEditorItem() { if ( Original == null ) Original = this; }

            //public ShipIconFactionType FactionType;
            public A5LevelItem ItemData = null;

            public ArcenUIWrapperedTMProText DisplayText;
            public UnityEngine.UI.Image DisplayIcon;
            private Sprite lastDisplayIconSprite = null;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                A5LevelItem item = this.ItemData;
                if ( item != null )
                {
                    LevelEditorCoreGameLoop levelEditorLoop = Engine_Universal.GameLoop as LevelEditorCoreGameLoop;
                    GameObject protoObj = item.GetOriginalPrototype();
                    if ( protoObj == null )
                    {
                        ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, LocalizedString.AddNeverTranslated_New( "Error" ),
                            LocalizedString.AddNeverTranslated_New( "Could not find GameObject for '" +
                            item.GetLocalizedDisplayName() + "'.  There was probably already an error about this." ), 
                            LangCommon.Popup_Common_Ok.LocalizedString );
                        return MouseHandlingResult.PlayClickDeniedSound;
                    }
                    if ( input.RightButtonClicked && levelEditorLoop.LevelEditorHook.GetCountOfSelectedObjects( false ) > 0 )
                        levelEditorLoop.GPUInstancerHook.ReplaceAllSelectedObjectsWithThis( item );
                    else
                        levelEditorLoop.GPUInstancerHook.AddIGPUIPrototypeInFrontOfLevelEditorCamera( item );
                }
                return MouseHandlingResult.None;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LevelEditorPalette-bLevelEditorItem-tooltipBuffer" );
            public override void HandleMouseover()
            {
                LevelEditorCoreGameLoop levelEditorLoop = Engine_Universal.GameLoop as LevelEditorCoreGameLoop;
                A5LevelItem item = this.ItemData;

                tooltipBuffer.AddNeverTranslated( "Click to add: ", true ).AddNeverTranslated( item == null ? "nothing" : item.GetLocalizedDisplayName(), true );
                if ( levelEditorLoop.LevelEditorHook.GetCountOfSelectedObjects( false ) > 0 )
                    tooltipBuffer.AddNeverTranslated( "\nRight-click to replace ", true ).AddRaw( levelEditorLoop.LevelEditorHook.GetCountOfSelectedObjects( true ).ToString() ).AddNeverTranslated( " selected object(s) with this.", true );

                if ( item != null && item.ObjRoot != null )
                {
                    if ( item.ObjRoot.AllowRescaling )
                        tooltipBuffer.StartColor( ColorTheme.HeaderGold ).AddNeverTranslated( "\nVery Unusual!  This can be resized!", true ).EndColor();
                    else
                        tooltipBuffer.StartColor( ColorTheme.Gray ).AddNeverTranslated( "\nCannot be resized.", true ).EndColor();
                }

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditorItem", "AnyIsFine" ) );
            }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                InitIfNeeded( Image, SubImages, SubTexts );
                RenderContents();
            }

            public const float WIDTH = 28f;
            public const float HEIGHT = 35.7f;

            private bool hasInitialized = false;
            public void InitIfNeeded( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( hasInitialized )
                    return;
                hasInitialized = true;

                DisplayText = SubTexts[0].Text;
                DisplayIcon = SubImages[0].Image;
                lastDisplayIconSprite = DisplayIcon.sprite;

                RectTransform rTran = DisplayText.ReferenceText.rectTransform;
                rTran.anchoredPosition = new Vector2( 0, 5.55f );
                rTran.localScale = Vector3.one;
                rTran.localRotation = Quaternion.identity;

                //UnityEngine.Debug.Log( this.Element.name + " v1: " + rTran.sizeDelta );
                //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                //UnityEngine.Debug.Log( this.Element.name + " v2: " + rTran.sizeDelta );
            }

            public bool DebugRenderContents = false;
            public void RenderContents()
            {
                if ( this.ItemData == null )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    if ( DisplayText != null )
                    {
                        ArcenDoubleCharacterBuffer buffer = DisplayText.StartWritingToBuffer();
                        buffer.AddRaw( this.ItemData.GetLocalizedDisplayName() );
                        DisplayText.FinishWritingToBuffer();
                    }
                    debugStage = 2;

                    debugStage = 10;
                    if ( !ItemData.LoadedSprite && ItemData.SpriteIconPathInAssetBundle != null && ItemData.SpriteIconPathInAssetBundle.Length > 0 )
                    {
                        ItemData.LoadedSprite = ArcenAssetBundleManager.LoadUnitySpriteFromBundleSynchronous( //this comes up rare enough that doing it synchronously makes the most sense to just get it done quickly
                            "placement_icons", //Chris note: yeah, I am hardcoding this.  I'm not really looking to have folks add custom content of this type to this particular game.
                            ItemData.SpriteIconPathInAssetBundle
                            );
                    }

                    debugStage = 20;
                    if ( DisplayIcon != null )
                    {
                        if ( ItemData.LoadedSprite )
                        {
                            if ( lastDisplayIconSprite != ItemData.LoadedSprite )
                            {
                                lastDisplayIconSprite = ItemData.LoadedSprite;
                                DisplayIcon.sprite = ItemData.LoadedSprite;
                            }
                        }
                        else
                        {
                            if ( lastDisplayIconSprite != null )
                            {
                                lastDisplayIconSprite = null;
                                DisplayIcon.sprite = null;
                            }
                        }
                    }
                }
                catch ( Exception e )
                {
                    if ( DebugRenderContents )
                        ArcenDebugging.LogDebugStageWithStack( "Exception in bLevelEditorItem.RenderContents", debugStage, e, Verbosity.ShowAsError );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return ItemData == null;
            }

            public override void Clear()
            {
                this.ItemData = null;
            }
        }
        #endregion

        #region bLevelEditorItemPool
        public class bLevelEditorItemPool
        {
            private static readonly List<bLevelEditorItem> globalPoolList_Icon = List<bLevelEditorItem>.Create_WillNeverBeGCed( 3000, "Window_LevelEditorPalette-bLevelEditorItemPool-globalPoolList_Icon" );
            private static int currentGlobalIndex_Icon = -1;

            private readonly List<bLevelEditorItem> inUseList_Icon = List<bLevelEditorItem>.Create_WillNeverBeGCed( 3000, "Window_LevelEditorPalette-bLevelEditorItemPool-inUseList_Icon" );

            public bLevelEditorItemPool( int MinEntries )
            {
                if ( globalPoolList_Icon.Count == 0 )
                    globalPoolList_Icon.Add( bLevelEditorItem.Original );

                while ( globalPoolList_Icon.Count < MinEntries )
                    Create_bLevelEditorItem();
            }

            private static int maxRemainingAllowedToAddBeforeNextClear = 0;
            public static void ClearAll( int MaxToAddAtASingleTime )
            {
                maxRemainingAllowedToAddBeforeNextClear = MaxToAddAtASingleTime;

                currentGlobalIndex_Icon = -1;
            }

            public void ClearSingle()
            {
                for ( int i = 0; i < inUseList_Icon.Count; i++ )
                    inUseList_Icon[i].Clear();
                inUseList_Icon.Clear();
            }

            public List<bLevelEditorItem> GetInUseList_Icon()
            {
                return this.inUseList_Icon;
            }

            public int GetInUseCount_Icon()
            {
                return this.inUseList_Icon.Count;
            }

            public bLevelEditorItem GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds_Icon( A5LevelItem Item )
            {
                bLevelEditorItem icon;
                for ( int i = 0; i < inUseList_Icon.Count; i++ )
                {
                    icon = inUseList_Icon[i];
                    if ( icon.ItemData == Item )
                        return icon; //if we already had one for this
                }

                //didn't find it, so...
                currentGlobalIndex_Icon++;
                if ( currentGlobalIndex_Icon < globalPoolList_Icon.Count )
                {
                    //had enough items in pool to just grab one
                    icon = globalPoolList_Icon[currentGlobalIndex_Icon];
                    icon.ItemData = Item;
                    inUseList_Icon.Add( icon );
                    return icon;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now                
                icon = Create_bLevelEditorItem();
                icon.ItemData = Item;
                this.inUseList_Icon.Add( icon );
                return icon;
            }

            private static bLevelEditorItem Create_bLevelEditorItem()
            {
                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bLevelEditorItem.Original.Element.DuplicateSelf();
                bLevelEditorItem icon = (bLevelEditorItem)button.Controller;
                globalPoolList_Icon.Add( icon );
                return icon;
            }

            public const int PREFER_LEFT = -1;
            public const int PREFER_RIGHT = 1;
            public const int PREFER_NEITHER = 0;

            public void Sort()
            {
                //no need for sorting at the moment, but here's how we would do it if we need to:

                //if ( this.inUseList_Icon.Count > 1 )
                //{
                //    this.inUseList_Icon.Sort( delegate ( bLevelEditorItem Left, bLevelEditorItem Right )
                //    {
                //        int val = 0;
                //        val = Right.ItemData.GetLocalizedDisplayName().CompareTo( Left.ItemData.GetLocalizedDisplayName() );
                //        if ( val != 0 )
                //            return val;
                //        return 0;
                //    } );
                //}
            }
        }
        #endregion
    }
}

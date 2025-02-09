using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_LevelEditorObjectList : WindowControllerAbstractBase
    {
        public static Window_LevelEditorObjectList Instance;
        public Window_LevelEditorObjectList()
        {
            Instance = this;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Engine_Universal.GameLoop.IsLevelEditor )
                return false;
            bool shouldShow = Window_LevelEditorPalette.IsShowingSceneObjectListInstead;

            if ( !shouldShow )
                LastObjectIndexClicked = -1;

            return shouldShow;
        }

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_LevelEditorObjectList.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bLevelEditorObjectPool globalItemPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( Window_LevelEditorObjectList.Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0.2f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0.25f;

                        if ( bLevelEditorObject.Original != null )
                        {
                            hasGlobalInitialized = true;
                            globalItemPool = new bLevelEditorObjectPool( 20 );
                        }
                    }
                    #endregion
                }

                bLevelEditorObjectPool.ClearAll( 10 );
                globalItemPool.ClearSingle();

                RectTransform rTran = null;
                float currentY = -6;

                #region Items In The Current Level Editor Scene
                allObjects = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetListOfAllObjects( false );

                if ( allObjects != null && allObjects.Count > 0 )
                {
                    foreach ( A5Placeable obj in allObjects )
                    {
                        if ( !obj )
                            continue; //if nothing to show, then skip it

                        if ( globalItemPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds_Text( obj ) == null )
                            continue; //this would be time-slicing
                    }

                    //globalItemPool.Sort(); //we could if we want, but I don't see a need to sort it
                    ApplyLevelItemsInRows_Text( ref currentY, globalItemPool.GetInUseList_Icon() );
                }
                #endregion

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                rTran = (RectTransform)bLevelEditorObject.Original.Element.RelevantRect.parent;
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( currentY );
                rTran.sizeDelta = sizeDelta;
                #endregion
            }

            #region ApplyLevelItemsInRows_Text
            private void ApplyLevelItemsInRows_Text( ref float currentY, List<bLevelEditorObject> items )
            {
                bLevelEditorObject item;
                RectTransform rTran;
                for ( int i = 0; i < items.Count; i++ )
                {
                    item = items[i];

                    //ArcenDebugging.ArcenDebugLogSingleLine( "Item to sort: " + item?.ItemData.DisplayName + item.GetShouldBeHidden(), Verbosity.DoNotShow );

                    if ( item.GetShouldBeHidden() )
                        continue;

                    rTran = item.Element.RelevantRect;
                    rTran.anchoredPosition = new Vector2( 0f, currentY );
                    rTran.localScale = Vector3.one;
                    rTran.localRotation = Quaternion.identity;
                    rTran.pivot = new Vector2( 0, 1 );
                    //the text child of the object
                    ((RectTransform)rTran.GetChild( 0 )).offsetMin = Vector2.zero; //sets the values of the Stretch Rect Transform
                    ((RectTransform)rTran.GetChild( 0 )).offsetMax = Vector2.zero; //sets the values of the Stretch Rect Transform
                    //rTran.sizeDelta = new Vector2( WIDTH, HEIGHT );
                    currentY -= ROW_ADVANCE_TEXT;
                }

                //currentY -= ROW_ADVANCE;
            }
            #endregion
        }

        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddNeverTranslated( "Scene Objects: ", true ).AddNeverTranslated( allObjects == null ? "0" : allObjects.Count.ToStringThousandsWhole(), true );
            }
            public override void OnUpdate() { }
        }

        public const float WIDTH_TEXT = 154.9f;
        public const float HEIGHT_TEXT = 18.5f;
        public const float ROW_ADVANCE_TEXT = HEIGHT_TEXT + 2f;

        private static int LastObjectIndexClicked = -1;
        private static List<A5Placeable> allObjects = null;

        #region bLevelEditorObject
        public class bLevelEditorObject : ImageButtonAbstractBase
        {
            public static bLevelEditorObject Original;
            public bLevelEditorObject() { if ( Original == null ) Original = this; }

            public A5Placeable LevelObj = null;

            public ArcenUIWrapperedTMProText DisplayText;

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;

                if ( levelEditorHook.GetIsObjectSelected( LevelObj ) )
                {
                    //if already selected, handle this click by deselecting
                    levelEditorHook.SetIsObjectSelected( LevelObj, false );
                    LastObjectIndexClicked = -1;
                }
                else
                {
                    //if not selected, handle this click by selecting
                    levelEditorHook.SetIsObjectSelected( LevelObj, true );
                    if ( allObjects == null )
                        LastObjectIndexClicked = -1;
                    else
                    {
                        bool appendToSelection = InputActionTypeDataTable.Instance.GetRowByIDOrNullIfNotFound( "LevelEditor_Append to selection" ).CalculateIsKeyDownNow_IgnoreConflicts();
                        
                        if ( appendToSelection && LastObjectIndexClicked >= 0 )
                        {
                            //do group select
                            int newSelectIndex = allObjects.IndexOf( LevelObj );
                            if ( newSelectIndex >= 0 )
                            {
                                int lower = MathA.Min( newSelectIndex, LastObjectIndexClicked );
                                int upper = MathA.Max( newSelectIndex, LastObjectIndexClicked );

                                for ( int i = lower; i <= upper && i < allObjects.Count; i++ )
                                {
                                    levelEditorHook.SetIsObjectSelected( allObjects[i], true );
                                }
                            }
                        }

                        LastObjectIndexClicked = allObjects.IndexOf( LevelObj );
                    }
                }

                return MouseHandlingResult.None;
            }

            private static readonly ArcenDoubleCharacterBuffer tooltipBuffer = new ArcenDoubleCharacterBuffer( "Window_LevelEditorObjectList-bLevelEditorObject-tooltipBuffer" );
            public override void HandleMouseover()
            {
                if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetIsObjectSelected( LevelObj ) )
                    tooltipBuffer.StartColor( ColorTheme.HeaderGold ).AddNeverTranslated( "Click to deselect: ", true ).AddNeverTranslated( !this.LevelObj ? "nothing" : this.LevelObj.name, true );
                else
                    tooltipBuffer.AddNeverTranslated( "Click to select: ", true ).AddNeverTranslated( !this.LevelObj ? "nothing" : this.LevelObj.name, true );

                Window_LevelEditorTooltip.bPanel.Instance.SetText( this.Element, tooltipBuffer, TooltipWidth.Wide, TooltipID.Create( "LevelEditor", "AllGood" ) );
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
                if ( !this.LevelObj )
                    return;

                int debugStage = -1;
                try
                {
                    debugStage = 0;
                    if ( DisplayText != null )
                    {
                        ArcenDoubleCharacterBuffer buffer = DisplayText.StartWritingToBuffer();
                        if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.GetIsObjectSelected( LevelObj ) )
                            buffer.StartColor( ColorTheme.HeaderGold );
                        buffer.AddRaw( this.LevelObj.name );
                        DisplayText.FinishWritingToBuffer();
                    }
                    debugStage = 2;
                }
                catch ( Exception e )
                {
                    if ( DebugRenderContents )
                        ArcenDebugging.LogDebugStageWithStack( "Exception in bLevelEditorObject.RenderContents", debugStage, e, Verbosity.ShowAsError );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return !LevelObj;
            }

            public override void Clear()
            {
                this.LevelObj = null;
            }
        }
        #endregion

        #region bLevelEditorObjectPool
        public class bLevelEditorObjectPool
        {
            private static readonly List<bLevelEditorObject> globalPoolList_Icon = List<bLevelEditorObject>.Create_WillNeverBeGCed( 3000, "Window_LevelEditorObjectList-bLevelEditorObjectPool-globalPoolList_Icon" );
            private static int currentGlobalIndex_Icon = -1;

            private readonly List<bLevelEditorObject> inUseList_Icon = List<bLevelEditorObject>.Create_WillNeverBeGCed( 3000, "Window_LevelEditorObjectList-bLevelEditorObjectPool-inUseList_Icon" );

            public bLevelEditorObjectPool( int MinEntries )
            {
                if ( globalPoolList_Icon.Count == 0 )
                    globalPoolList_Icon.Add( bLevelEditorObject.Original );

                while ( globalPoolList_Icon.Count < MinEntries )
                    Create_bLevelEditorObject();
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

            public List<bLevelEditorObject> GetInUseList_Icon()
            {
                return this.inUseList_Icon;
            }

            public int GetInUseCount_Icon()
            {
                return this.inUseList_Icon.Count;
            }

            public bLevelEditorObject GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds_Text( A5Placeable obj )
            {
                bLevelEditorObject icon;
                for ( int i = 0; i < inUseList_Icon.Count; i++ )
                {
                    icon = inUseList_Icon[i];
                    if ( icon.LevelObj == obj )
                        return icon; //if we already had one for this
                }

                //didn't find it, so...
                currentGlobalIndex_Icon++;
                if ( currentGlobalIndex_Icon < globalPoolList_Icon.Count )
                {
                    //had enough items in pool to just grab one
                    icon = globalPoolList_Icon[currentGlobalIndex_Icon];
                    icon.LevelObj = obj;
                    inUseList_Icon.Add( icon );
                    return icon;
                }

                maxRemainingAllowedToAddBeforeNextClear--;
                if ( maxRemainingAllowedToAddBeforeNextClear <= 0 )
                    return null;

                //did NOT have enough items in pool, so creating a duplicate now                
                icon = Create_bLevelEditorObject();
                icon.LevelObj = obj;
                this.inUseList_Icon.Add( icon );
                return icon;
            }

            private static bLevelEditorObject Create_bLevelEditorObject()
            {
                ArcenUI_ImageButton button = (ArcenUI_ImageButton)bLevelEditorObject.Original.Element.DuplicateSelf();
                bLevelEditorObject icon = (bLevelEditorObject)button.Controller;
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
                //    this.inUseList_Icon.Sort( delegate ( bLevelEditorObject Left, bLevelEditorObject Right )
                //    {
                //        int val = 0;
                //        val = Right.ItemData.DisplayName.CompareTo( Left.ItemData.DisplayName );
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

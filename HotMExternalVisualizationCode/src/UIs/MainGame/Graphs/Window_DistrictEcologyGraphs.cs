//using System;
//using Arcen.Universal;
//using Arcen.HotM.Core;
//using Arcen.HotM.Visualization;
//using UnityEngine;

//namespace Arcen.HotM.ExternalVis
//{
//    public class Window_DistrictEcologyGraphs : ToggleableWindowController, IInputActionHandler 
//    {
//        public static Window_DistrictEcologyGraphs Instance;
//        private ArcenCachedExternalTypeDirect type_dCheckedListbox = ArcenExternalTypeManager.GetOrCreateTypeDirect(typeof(chChartCheckedListbox));

//		private const float ROW_HEIGHT = 24;
//		private const float ROW_BUFFER = 1.5f;
//		private const float FULL_WIDTH_AVOIDSCROLLBAR = 220f;
//		public Window_DistrictEcologyGraphs()
//        {
//            Instance = this;
//        }

//        public static ArcenUI_GraphHandler GraphHandler;

//        public ISimDistrict CurrentDistrict = null;

//        private static readonly List<EcoWrapperClass> ecoWrapperedItems = List<EcoWrapperClass>.Create_WillNeverBeGCed(200, "Window_DistrictEcologyPredictionGraph-ecoWrapperedItems");

//		//protected static UIXDisplayItem ChosenDisplayStyle = null;
//		//protected static UIXSortFilterItem ChosenSortAndFilter = null;

//		//protected static readonly List<UIXDisplayItem> CurrentValidDisplayItems = List<UIXDisplayItem>.Create_WillNeverBeGCed( 200, "Window_DistrictEcologyGraphs-CurrentValidDisplayItems" );
//		//protected static readonly List<UIXSortFilterItem> CurrentValidSortFilterItems = List<UIXSortFilterItem>.Create_WillNeverBeGCed( 200, "Window_DistrictEcologyGraphs-CurrentValidSortFilterItems" );

//		#region OnOpen
//		public sealed override void OnOpen()
//        {
//            if ( bCategory.Original != null ) //scroll left panel back to top when opening
//                bCategory.Original.Element.TryScrollToTop();
//            ecoWrapperedItems.Clear();
//            float angle = 0;
//            float angleChange = 5.7f * Mathf.PI / 360f;
//            foreach (EcoObjectType row in EcoObjectTypeTable.Instance.Rows)
//            {
//	            if (row.IsHidden && !showHidden) continue;
//	            if (!row.FunctionalTag.Contains(CommonRefs.WildlifeTag)) continue;
//	            Color col = Color.HSVToRGB(angle, 1f, 1f);
//	            angle += angleChange;
//	            ecoWrapperedItems.Add(new EcoWrapperClass(row, col.GetHexCode()));
//            }
//		}
//        #endregion

//        public sealed override void OnClose() { }

//        #region tHeaderText
//        public class tHeaderText : TextAbstractBase
//        {
//            public sealed override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
//            {
//                Buffer.AddLang( "EcologyGraphs_Header" );
//            }
//        }
//        #endregion

//        #region PopulateFreeFormControls
//        public sealed override void PopulateFreeFormControls( ArcenUI_SetOfCreateElementDirectives Set )
//        {
//        }
//        #endregion

//        private static ButtonAbstractBase.ButtonPool<bCategory> bCategoryPool;

//        private static bool hasClearedGraphHandler = false;
//        private static ISimDistrict hasSetGraphDataForDistrict = null;

//        private static float lastTimeUpdatedData = 0;
//		private static bool debug = false;
//		private static bool showHidden = false;

//        public class customParent : CustomUIAbstractBase
//        {
//            private bool hasGlobalInitialized = false;
//            public override void OnUpdate()
//            {
//                if ( Instance != null )
//                {
//                    #region Global Init
//                    if ( !hasGlobalInitialized )
//                    {
//                        if ( bCategory.Original != null )
//                        {
//                            hasGlobalInitialized = true;
//                            bCategoryPool = new ButtonAbstractBase.ButtonPool<bCategory>( bCategory.Original, 10, "bCategory" );
//                            GraphHandler = this.Element.GetComponentInChildren<ArcenUI_GraphHandler>();
//                            if ( GraphHandler == null )
//                                ArcenDebugging.LogSingleLine( "Could not find ArcenUI_GraphHandler in " + this.Element.name +
//                                    " for Window_DistrictEcologyGraphs", Verbosity.ShowAsError );
//                            else
//                            {
//                                GraphHandler.onPointHover = this.OnGraphPointHover;
//                                GraphHandler.onLineHover = this.OnGraphLineHover;
//                                GraphHandler.onPointClick = this.OnGraphPointClick;
//                            }
//                        }
//                    }
//                    #endregion
//                }

//                if ( !hasGlobalInitialized )
//                    return;

//                this.OnUpdateCategories();

//                if ( Instance.CurrentDistrict == null )
//                {
//                    DictionaryView<short, ISimDistrict> districts = World.Districts.GetDistrictsByID();
//                    if ( districts.Count > 0 )
//                    {
//                        foreach ( KeyValuePair<short, ISimDistrict> kv in districts )
//                        {
//                            Instance.CurrentDistrict = kv.Value;
//                            break;
//                        }
//                    }

//                    if ( Instance.CurrentDistrict == null )
//                    {
//                        hasSetGraphDataForDistrict = null;
//                        lastTimeUpdatedData = -1;
//                        if ( !hasClearedGraphHandler )
//                        {
//                            hasClearedGraphHandler = true;
//                            GraphHandler.ClearCategories();
//                        }
//                        return;
//                    }
//                }

//                this.SetUpNewDistrictChoiceIfNeeded();

//                //since this data is randomly generated, we're only updating it ever 5 seconds
//                //for real data, 1-2 seconds is maybe better
//                if ( ArcenTime.AnyTimeSinceStartF - lastTimeUpdatedData > 5f && hasSetGraphDataForDistrict != null)
//                {
//                    lastTimeUpdatedData = ArcenTime.AnyTimeSinceStartF;
//					debug = GameSettings.Current.GetBool("Debug_MainGameEcoGraphDebug");
//					showHidden = GameSettings.Current.GetBool("Debug_MainGameEcoGraphShowHidden");
//                    DictionaryViewOfQueueViews<IEcoQueryable, float> dat;

//					//this must be done before we start updating the actual data
//					GraphHandler.StartDataSourceBatch();

//					dat = hasSetGraphDataForDistrict.GetCurrentEcologyData();

//					//EcologySim preseedTurns  = 12
//					//EcologySim slicesPerTurn = 20

//					int o = SimCommon.Turn * 90 - (12 * 20);
//					DrawData(dat, ref o, out int max1);
//					//and this must be done whenever we finish that batch of data
//					GraphHandler.EndDataSourceBatch();
//                }
//            }

//			private void DrawData( DictionaryViewOfQueueViews<IEcoQueryable, float> dat, ref int offset, out int maxVal)
//			{
//				int refI=-1;
//				int _off = offset;
//				float maxSeen = 0;
//				dat.DoFor(pair =>
//				{
//					if (pair.Key.IsHidden && !showHidden) return DelReturn.Continue;
//					if (pair.Key.VisDisabled) return DelReturn.Continue;
//					float p4 = -1;
//					float p3 = -1;
//					float p2 = -1;
//					float p1 = -1;
//					int i = 0;
//					foreach (float p in pair.Value)
//					{
//						if (p1 < 0)
//						{
//							p1 = p;
//							p2 = p;
//							p3 = p;
//							p4 = p;
//						}

//						if (i++ > 0)
//						{
//							if (!debug)
//								GraphHandler.AddAddPointToCategory(pair.Key.DisplayName, (i+ _off) / 20f, (int)((p + p1 + p2 + p3 + p4) / 3f));
//							else
//								GraphHandler.AddAddPointToCategory(pair.Key.DisplayName, (i+ _off) / 20f, (int)p);
//						}
//						//GraphHandler.AddAddPointToCategory(pair.Key.originalType.DisplayName + v, i++, (int+offset)((float)p / hasSetGraphDataForDistrict.GetMapDistrict().Cells.Count));

//						p4 = p3;
//						p3 = p2;
//						p2 = p1;
//						p1 = p;
//						maxSeen = MathA.Max(maxSeen, debug ? p : (int)((p + p1 + p2 + p3 + p4) / 3f));
//					}

//					refI = MathA.Max(refI, i);
//					return DelReturn.Continue;
//				});
//				offset = (refI + offset);
//				maxVal = (int)maxSeen;
//			}

//			private void SetUpNewDistrictChoiceIfNeeded()
//            {
//				if ( hasSetGraphDataForDistrict == Instance.CurrentDistrict )
//                    return; //don't update this until we click a different district and back
//                hasSetGraphDataForDistrict = Instance.CurrentDistrict;

//				SimCommon.DistrictToDebug = hasSetGraphDataForDistrict?.GetDisplayName();
//				lastTimeUpdatedData = -1;

//                GraphHandler.ClearCategories();

//				float angle = 0;
//				float angleChange = 5.7f * Mathf.PI / 360f;
//				EcoObjectTypeTable.Instance.DoForRows(row => {
//					if(row.IsHidden && !showHidden) return DelReturn.Continue;
//					Color col = Color.HSVToRGB(angle, 0.9f, 0.9f);
//					GraphHandler.AddCategory(row.GetDisplayName(), col);
//					angle = (angle + angleChange) % 1;
//					return DelReturn.Continue;
//				});
//			}

//            public void OnUpdateCategories()
//            {
//                float currentY = -5; //the position of the first entry

//                if ( !hasGlobalInitialized )
//                    return;

//                bCategoryPool.Clear( 5 );

//                DictionaryView<short, ISimDistrict> districts = World.Districts.GetDistrictsByID();
//                foreach ( KeyValuePair<short, ISimDistrict> kv in districts )
//                {
//                    bCategory item = bCategoryPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
//                    if ( item == null )
//                        break; //time slicing, too many added right now
//                    item.Assign( kv.Value );
//                }

//                #region Positioning Logic 1
//                bCategoryPool.ApplyItemsInRows( 10, ref currentY, 27f, 218.5f, 25.6f );
//                #endregion

//                #region Positioning Logic
//                //Now size the parent, called Content, to get scrollbars to appear if needed.
//                RectTransform rTran = (RectTransform)bCategory.Original.Element.RelevantRect.parent;
//                Vector2 sizeDelta = rTran.sizeDelta;
//                sizeDelta.y = MathA.Abs( currentY );
//                rTran.sizeDelta = sizeDelta;
//                #endregion
//            }

//            private void OnGraphPointHover( GraphMouseData eventData )
//            {
//                tooltipBuffer.AddUntranslated( eventData.CategoryID ).Line();
//                tooltipBuffer.AddRaw( eventData.ValueX.ToStringThousandsWhole() );
//                tooltipBuffer.AddUntranslated( " x " );
//                tooltipBuffer.AddRaw( eventData.ValueY.ToStringThousandsWhole() );

//            }

//            private void OnGraphPointClick( GraphMouseData eventData )
//            {

//            }

//            private void OnGraphLineHover( GraphMouseData eventData )
//            {
//                tooltipBuffer.AddUntranslated( eventData.CategoryID ).Line();
//                tooltipBuffer.AddRaw( eventData.ValueX.ToStringThousandsWhole() );
//                tooltipBuffer.AddUntranslated( " x " );
//                tooltipBuffer.AddRaw( eventData.ValueY.ToStringThousandsWhole() );

//            }
//        }

//        #region tKeyText
//        public class tKeyText : TextAbstractBase
//        {
//            public sealed override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
//            {
//            }

//            public override bool GetShouldBeHidden()
//            {
//                return true;
//            }
//        }
//        #endregion

//        #region bCategory
//        public class bCategory : ButtonAbstractBase
//        {
//            public static bCategory Original;
//            public bCategory() { if ( Original == null ) Original = this; }

//            private ISimDistrict District = null;

//            public void Assign( ISimDistrict District )
//            {
//                this.District = District;
//            }

//            public override bool GetShouldBeHidden()
//            {
//                return this.District == null;
//            }

//            public override void Clear()
//            {
//                this.District = null;
//            }

//            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer buffer )
//            {
//                if ( this.District == null )
//                    return;

//                if ( Instance.CurrentDistrict == this.District )
//                    buffer.StartColor( ColorTheme.CategorySelectedBlue );

//                buffer.AddRaw( this.District.GetDisplayName() );
//            }

//            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
//            {
//                if ( this.District == null )
//                    return MouseHandlingResult.PlayClickDeniedSound;
//                Instance.CurrentDistrict = this.District;
//                return MouseHandlingResult.None;
//            }

//            public override void HandleMouseover()
//            {
//                if ( this.District == null )
//                    return;

//                this.District.WriteDataItemUIXTooltip( tooltipBuffer, out MinHeight TooltipMinHeight );

//            }
//        }
//        #endregion

//        #region dChartDropdown
//        public class dChartDropdown : DropdownAbstractBase
//        {
//            public static dChartDropdown Instance;
//            public dChartDropdown()
//            {
//                Instance = this;
//            }

//            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
//            {
//                if ( Item == null )
//                    return;
//            }

//            public override bool GetShouldBeHidden()
//            {
//                return true;
//            }

//            public override void OnUpdate()
//            {

//            }
//            public override void HandleMouseover()
//            {

//            }
//            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
//            {
//            }
//        }
//		#endregion

//		#region chChartCheckedListbox - graph legend
//		public class chChartCheckedListbox : CheckedListboxAbstractBase
//        {
//	        private int cachedNumRows = -1;

//            public static chChartCheckedListbox Instance;
//            public chChartCheckedListbox()
//            {
//                Instance = this;
//			}

//			public override void GetMainTextToShow(ArcenDoubleCharacterBuffer Buffer)
//			{
//				Buffer.AddLang("EcologyChartLegend");
//			}

//			public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
//            {
//                if ( Item == null )
//                    return;
//            }

//            public override bool GetShouldBeHidden()
//            {
//                return false;
//            }

//            public override void OnUpdate()
//            {
//				ArcenUI_CheckedListbox elementAsType = (ArcenUI_CheckedListbox)this.Element;
//				if (elementAsType == null)
//					return;

//				if (elementAsType.GetItemCount() == cachedNumRows) return;

//				elementAsType.ClearItems();
//				foreach (EcoWrapperClass row in ecoWrapperedItems)
//				{
//					if (row.Type.IsHidden && !showHidden) continue;
//					elementAsType.AddItem(row);
//					cachedNumRows++;
//				}
//			}
//            public override void HandleMouseover()
//            {

//            }

//            public override bool GetIsItemChecked( IArcenCheckedListboxOption Item )
//			{
//				EcoObjectType row = Item?.GetItem() as EcoObjectType;
//	            if (row == null) return false;

//	            IEcoQueryable thisEco = hasSetGraphDataForDistrict.GetLocalEcology().GetEcoImplementationByName(row.ID);
//				return !thisEco.VisDisabled;
//            }

//            public override void HandleItemCheckChanged( IArcenCheckedListboxOption Item, bool IsNowChecked )
//            {
//	            EcoObjectType row = Item?.GetItem() as EcoObjectType;
//	            if (row == null) return;

//				//synchronize across all districts
//				AbstractSimQueries.Instance.Get_SimWorld_Districts().GetDistrictsByID().DoFor(kvp =>
//				{
//					if (kvp.Value?.GetLocalEcology()?.GetEcoImplementationByName(row.ID) == null) return DelReturn.Continue;
//					kvp.Value.GetLocalEcology().GetEcoImplementationByName(row.ID).VisDisabled = !IsNowChecked;
//					return DelReturn.Continue;
//				});
//			}

//            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenCheckedListboxOption Item )
//            {
//            }
//        }
//        #endregion

//        public class bClose : ButtonAbstractBase
//        {
//            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
//            {
//                Buffer.AddLang( LangCommon.Popup_Common_Cancel );
//            }

//            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
//            {
//                Instance.Close();
//                //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
//                ArcenInput.BlockForAJustPartOfOneSecond();
//                return MouseHandlingResult.None;
//            }
//        }

//        public sealed override void Close()
//        {
//            base.Close();
//        }

//        //from IInputActionHandler
//        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
//        {
//            switch ( InputActionType.ID )
//            {
//                case "Return":
//                    this.Close();
//                    //make sure no other input is processed for 0.4 of a second, so that for instance this doesn't open the escape menu.
//                    ArcenInput.BlockForAJustPartOfOneSecond();
//                    break;
//                default:
//                    InputWindowCutthrough.HandleKey( InputActionType.ID );
//                    break;
//            }
//        }
//    }
//}

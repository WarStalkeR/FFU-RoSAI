using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;



namespace Arcen.HotM.ExternalVis
{
    public class Window_PopulationStatistics : AbstractWindowInfoScreen<PopulationAggregationType, IAggregatedPopulationDimension>
    {
        public static Window_PopulationStatistics Instance;
        public Window_PopulationStatistics()
        {
            Instance = this;
            InstanceG = this;
            this.ShouldCauseAllOtherWindowsToNotShow = false;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
		}

        protected override ITableLikeDataSource<PopulationAggregationType> GetTable() { return PopulationAggregationTypeTable.Instance; }
        protected override UIXDisplayTable<IAggregatedPopulationDimension> GetUIXDisplayTable() { return UIX_PopulationStatisticsDisplayStyleTable.Instance; }
        protected override UIXSortFilterTable<IAggregatedPopulationDimension> GetUIXSortFilterTable() { return null; }
        protected override void GetHeaderText( ArcenDoubleCharacterBuffer Buffer ) { Buffer.AddLang( "PopulationStatistics_Header" ); }

        #region RecalculateDataItemsInCategory_Inner
        protected override void RecalculateDataItemsInCategory_Inner()
        {
            foreach ( PopulationAggregationType aggregationType in PopulationAggregationTypeTable.Instance.Rows )
                aggregationType.NonSim_ForUI_EntriesInThisType = 0;

            foreach ( EconomicClassType row in EconomicClassTypeTable.Instance.Rows )
            {
                PopulationAggregationType aggregationType = row.GetParentAggregationType();
                aggregationType.NonSim_ForUI_EntriesInThisType++;
                if ( aggregationType == this.CurrentRowType )
                {
                    this.AddToDataItemListInCategory( row );
                }
            }

            foreach ( ProfessionType row in ProfessionTypeTable.Instance.Rows )
            {
                PopulationAggregationType aggregationType = row.GetParentAggregationType();
                aggregationType.NonSim_ForUI_EntriesInThisType++;
                if ( aggregationType == this.CurrentRowType )
                {
                    this.AddToDataItemListInCategory( row );
                }
            }
        }
        #endregion

        protected override bool CalculateIsAllowedToShowCategory( PopulationAggregationType Category )
        {
            if ( Category.NonSim_ForUI_EntriesInThisType <= 0 )
                return false; //skip any that have nothing in them
            return true;
        }

        public class customParent : customParentBase { }
        public class bCategory : bCategoryBase { }
        public class bDataItem  : bDataItemBase {}
        public class dRightOptions : dRightOptionsBase { }
        public class dLeftOptions : dLeftOptionsBase { }
        public class bClose : bCloseBase { }
        public class bExit : bExitBase { }
        public class bMainContentParent : bMainContentParentBase { }
        public class tHeaderText : tHeaderTextBase { }

        public class tColumnHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
            }
            public override bool GetShouldBeHidden()
            {
                return true;
            }
        }
    }
}

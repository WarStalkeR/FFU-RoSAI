using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_BuildingDirectory : AbstractWindowInfoScreen<BuildingTag,ISimBuilding>
    {
        public static Window_BuildingDirectory Instance;
        public Window_BuildingDirectory()
        {
            Instance = this;
            InstanceG = this;
            this.ShouldCauseAllOtherWindowsToNotShow = false;
            this.PreventsNormalInputHandlers = true;
            this.SuppressesUIScaling = true;
            this.ShouldBlurBackgroundGame = true;
            this.ShouldPauseGameWhenOpen = false;
		}

        protected override ITableLikeDataSource<BuildingTag> GetTable() { return BuildingTagTable.Instance; }
        protected override UIXDisplayTable<ISimBuilding> GetUIXDisplayTable() { return UIX_BuildingDisplayStyleTable.Instance; }
        protected override UIXSortFilterTable<ISimBuilding> GetUIXSortFilterTable() { return UIX_BuildingSortAndFilterTable.Instance; }
        protected override void GetHeaderText( ArcenDoubleCharacterBuffer Buffer ) { Buffer.AddLang( "BuildingDirectory_Header" ); }

        #region RecalculateDataItemsInCategory_Inner
        protected override void RecalculateDataItemsInCategory_Inner()
        {
            DictionaryView<int,ISimBuilding> buildingDict = World.Buildings.GetAllBuildings();

            foreach ( BuildingTag tag in BuildingTagTable.Instance.Rows )
                tag.NonSim_ForUI_BuildingsInThisType = 0;

            foreach ( IKeyValuePair kv in buildingDict )
            {
                ISimBuilding building = kv.GetValueAsObject() as ISimBuilding;
                if ( building == null )
                    continue;
                MapCell cell = building.GetParentCell();
                MapTile tile = cell?.ParentTile;
                if ( tile == null || !tile.HasEverBeenExplored )
                    continue;

                MapDistrict district = building.GetParentDistrict();
                if ( district == null || !district.HasBeenDiscovered )
                    continue;

                Dictionary<string,BuildingTag> tags = building.GetVariant()?.Tags;
                if ( tags == null || tags.Count == 0 )
                    continue;
                foreach ( KeyValuePair<string, BuildingTag> kv2 in tags )
                {
                    BuildingTag tag = kv2.Value;
                    if ( tag.IsHidden )
                        continue;
                    if ( tag != null && this.PassesFilter( building ) )
                        tag.NonSim_ForUI_BuildingsInThisType++;
                    if ( tag == this.CurrentRowType )
                    {
                        this.AddToDataItemListInCategory( building );
                    }
                }
            }
        }
        #endregion

        protected override bool CalculateIsAllowedToShowCategory( BuildingTag Category )
        {
            if ( Category.NonSim_ForUI_BuildingsInThisType <= 0 )
                return false; //skip any that have nobody in them
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

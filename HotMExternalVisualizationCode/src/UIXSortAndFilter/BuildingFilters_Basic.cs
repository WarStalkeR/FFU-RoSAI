using Arcen.HotM.Core;
using Arcen.Universal;
using System;

namespace Arcen.HotM.ExternalVis
{
    public class BuildingFilters_Basic : IUIXSortAndFilterImplementation<ISimBuilding>
    {
        public bool ShouldBeShown_UIXSortAndFilter( ISimBuilding DataItem, UIXSortFilterItem<ISimBuilding> FilterType )
        {
            switch ( FilterType.GetID() )
            {
                case "ByName":
                    return true; //show all Buildings
                case "MachineStructureOnly":
                    return (DataItem?.MachineStructureInBuilding ?? null) != null;
                default:
                    throw new Exception( "BuildingFilters_Basic ShouldBeShown_UIXSortAndFilter: Not set up for '" + FilterType.GetID() + "'!" );
            }
        }

        public void SortList_UIXSortAndFilter( List<ISimBuilding> DataItems, UIXSortFilterItem<ISimBuilding> FilterType )
        {
            switch ( FilterType.GetID() )
            {
                case "ByName":
                case "MachineStructureOnly":
                    DataItems.Sort( delegate ( ISimBuilding left, ISimBuilding right )
                    {
                        int val = left.GetDisplayName().CompareTo( right.GetDisplayName() );
                        if ( val != 0 ) return val;
                        //return left.CompareTo( right ); //if same name, sort by other building factors that are deterministic for this turn, but different each turn.
                        return left.GetBuildingID().CompareTo( right.GetBuildingID() );
                    } );
                    break;
                default:
                    throw new Exception( "BuildingFilters_Basic SortList_UIXSortAndFilter: Not set up for '" + FilterType.GetID() + "'!" );
            }
        }
    }
}

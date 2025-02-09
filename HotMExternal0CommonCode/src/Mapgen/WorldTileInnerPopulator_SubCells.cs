using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    internal static class WorldTileInnerPopulator_SubCells
    {
        public static bool Debug_LogTileCompletions = false;
        private const int MAX_TILES_AT_ONCE = 10;

        #region DoPerFrame
        public static void DoPerFrame()
        {
            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //Willard reports that sometimes the mapgen and the tile filling race, so this waits until they are done
            if ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                return; //if we haven't yet finished initializing absolutely everything, then don't run this yet

            if ( CityMap.TilesBeingPostProcessed_SubCells > MAX_TILES_AT_ONCE )
                return;

            if ( CityMap.TilesToPostProcess_SubCells.Count == 0 && CityMap.WorkingTilesToPostProcess_SubCells.Count == 0 )
                return; //nothing to do

            //move tiles from the threadsafe queue to the working list that only our thread uses
            while ( CityMap.TilesToPostProcess_SubCells.TryDequeue( out MapTile tile ) )
                CityMap.WorkingTilesToPostProcess_SubCells.Add( tile );

            Debug_LogTileCompletions = GameSettings.Current.GetBool( "Debug_LogTileCompletions" );

            while ( CityMap.WorkingTilesToPostProcess_SubCells.Count > 0 )
            {
                Interlocked.Increment( ref CityMap.TilesBeingPostProcessed_SubCells );

                MapTile tile = CityMap.WorkingTilesToPostProcess_SubCells[0];
                CityMap.WorkingTilesToPostProcess_SubCells.RemoveAt( 0 );

                tile.timeStartedPopulation_Subcell = ArcenTime.AnyTimeSinceStartF;

                if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.DoSubCellPopulationOfTile",
                    ( TaskStartData startData ) =>
                    {

                        try
                        {
                            DoPopulationOfTile_OnBackgroundThread( startData, tile );
                        }
                        catch ( Exception e )
                        {
                            if ( !startData.IsMeantToSilentlyDie() )
                                ArcenDebugging.LogWithStack( "WorldTileInnerPopulator_SubCells.DoPerFrame_SubCells background thread error: " + e, Verbosity.ShowAsError );
                        }
                        finally //normally we avoid finally, but otherwise this won't happen on thread abort
                        {
                            Interlocked.Decrement( ref CityMap.TilesBeingPostProcessed_SubCells );
                        }
                        //ArcenDebugging.LogSingleLine( "CityMap.TilesBeingPostProcessed: " + CityMap.TilesBeingPostProcessed, Verbosity.ShowAsError );

                    } ) )
                {
                    //failed to start the above
                    Interlocked.Decrement( ref CityMap.TilesBeingPostProcessed_SubCells );
                    CityMap.WorkingTilesToPostProcess_SubCells.Add( tile );
                }

                if ( CityMap.TilesBeingPostProcessed_SubCells >= MAX_TILES_AT_ONCE )
                    return;
            }
        }
        #endregion

        #region DoPopulationOfTile_OnBackgroundThread
        private static void DoPopulationOfTile_OnBackgroundThread( TaskStartData startData, MapTile tile )
        {
            tile.workingPossibleObjectCollisions_All.Clear();
            tile.workingPossibleObjectCollisions_Current.Clear();

            if ( startData.IsMeantToSilentlyDie() )
                return; //this is a thread that was killed!

            int subCellItemsAdded = DoPopulationOfAllSubCells( startData, tile, out int ItemsNoAdds );

            FinishTileSetup( tile, subCellItemsAdded, ItemsNoAdds );
        }
        #endregion

        #region DoPopulationOfAllSubCells
        private static int DoPopulationOfAllSubCells( TaskStartData startData,  MapTile tile, out int ItemsNoAdds )
        {
            int itemsAdded = 0;
            int itemsNoAdds = 0;
            int debugStage = 0;
            try
            {
                foreach ( MapCell cell in tile.CellsList )
                {
                    HandleListForSubCell( tile, cell, cell.AllRoads, ref itemsAdded, ref itemsNoAdds, TileDest.Roads );
                    //HandleListForSubCell( tile, cell, cell.AllBuildings, ref itemsAdded, ref itemsNoAdds, TileDest.Buildings ); these are not handled this way anymore!
                    HandleListForSubCell( tile, cell, cell.OtherSkeletonItems, ref itemsAdded, ref itemsNoAdds, TileDest.OtherSkeletonItems );
                    HandleListForSubCell( tile, cell, cell.Fences, ref itemsAdded, ref itemsNoAdds, TileDest.Fences );
                    HandleListForSubCell( tile, cell, cell.DecorationMajor, ref itemsAdded, ref itemsNoAdds, TileDest.DecorationMajor );
                    HandleListForSubCell( tile, cell, cell.DecorationMinor, ref itemsAdded, ref itemsNoAdds, TileDest.DecorationMinor );
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "SubCells-DoPopulationOfAllSubCells", debugStage, e, Verbosity.ShowAsError );
            }
            ItemsNoAdds = itemsNoAdds;
            return itemsAdded;
        }
        #endregion

        #region HandleListForSubCell
        private static void HandleListForSubCell( MapTile tile, MapCell cell, List<MapItem> ItemList, ref int itemsAdded, ref int itemsNoAdds, TileDest Dest )
        {
            if ( ItemList.Count <= 0 )
                return;
            bool hasMultipleCells = tile.CellsList.Count > 1;

            for ( int i = 0; i < ItemList.Count; i++ )
            {
                MapItem item = ItemList[i];
                ArcenFloatRectangle rect = item.OBBCache.GetOuterRect();
                bool hasHadAnyAdds = false;
                if ( hasMultipleCells ) //only do on the multi-celluar ones
                {
                    foreach ( MapCell otherCell in tile.CellsList )
                    {
                        if ( otherCell == cell )
                            continue;

                        if ( !cell.CellRect.BasicIntersectionTest( rect ) )
                            continue; //if the other cell fails the intersection test, then don't test the subCells either

                        for ( int j = 0; j < otherCell.SubCells.Count; j++ )
                        {
                            MapSubCell subCell = otherCell.SubCells[j];
                            if ( subCell.SubCellRect.BasicIntersectionTest( rect ))
                            {
                                hasHadAnyAdds = true;
                                subCell.PlaceMapItemIntoSubCell( Dest, item );
                                itemsAdded++;
                            }
                        }
                    }
                }

                for ( int j = 0; j < cell.SubCells.Count; j++ )
                {
                    MapSubCell subCell = cell.SubCells[j];
                    if ( subCell.SubCellRect.BasicIntersectionTest( rect ) )
                    {
                        hasHadAnyAdds = true;
                        subCell.PlaceMapItemIntoSubCell( Dest, item );
                        itemsAdded++;
                    }
                }

                if ( !hasHadAnyAdds )
                    itemsNoAdds++;
            }

        }
        #endregion

        #region FinishTileSetup
        public static void FinishTileSetup( MapTile tile, int subCellItemsAdded, int ItemsNoAdds )
        {
            foreach ( MapCell cell in tile.CellsList )
                cell.ShouldSubCellsBeUsedForRendering = true;

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile subcell population complete! " + tile.TileTopLeftCellCoordinate +
                    " subCellItemsAdded: " + subCellItemsAdded +
                    " ItemsNoAdds: " + ItemsNoAdds +
                    " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Subcell).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion
    }
}

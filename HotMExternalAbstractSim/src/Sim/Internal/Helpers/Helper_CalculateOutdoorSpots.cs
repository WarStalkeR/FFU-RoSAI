using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;
using Arcen.Universal.Deserialization;
using System.Security.AccessControl;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.External
{
    internal class Helper_CalculateOutdoorSpots
    {
        private readonly List<MapItem> possibleItems = List<MapItem>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-possibleItems" );
        private readonly List<Vector3> existingSpots = List<Vector3>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-existingSpots" );
        private readonly List<Vector3> newlyAddedSpots = List<Vector3>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-newlyAddedSpots" );
        private readonly List<Vector3> possibleSpots = List<Vector3>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-possibleSpots" );
        private readonly List<ThreeTuple<MapItem, Vector3,Vector3>> blockingBuildings = List<ThreeTuple<MapItem, Vector3, Vector3>>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-blockingBuildings" );
        //private readonly List<Pair<MapItem, AABB>> blockingRoads = List<Pair<MapItem, AABB>>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-blockingRoads" );
        private readonly List<ThreeTuple<MapItem, Vector3, Vector3>> blockingFences = List<ThreeTuple<MapItem, Vector3, Vector3>>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-blockingFences" );
        private MersenneTwister random = new MersenneTwister( 0 );

        private const float REQUIRED_DISTANCE_FROM_OTHER_SPOTS = 0.9f;
        private const float REQUIRED_DISTANCE_FROM_CELL_EDGE = 0.7f;
        private const float REQUIRED_DISTANCE_FROM_BUILDINGS = 0.4f;
        private const float REQUIRED_DISTANCE_FROM_FENCES = 0.7f;
        private const float OFFROAD_GRID_CHECK_DENSITY = 0.2f;
        private const float OFFROAD_GRID_CHECK_DENSITY_OUT_OF_BOUNDS = 4f;
        private const float BASE_HEIGHT_OUT_OF_BOUNDS = 1.3f;

        public readonly List<MapCell> AllCellsToCheck = List<MapCell>.Create_WillNeverBeGCed( 400, "Helper_CalculateOutdoorSpots-AllCellsToCheck" );

        internal void GenerateOutdoorSpots( MapCell cell )
        {
            if ( cell == null || cell.HasEverCalculatedOutdoorSpots )
                return;

            if ( cell.AllOutdoorSpots.Count > 0 && cell.ParentTile.IsOutOfBoundsTile )
            {
                cell.HasEverCalculatedOutdoorSpots = true;
                return;
            }

            random.ReinitializeWithSeed( Engine_Universal.PermanentQualityRandom.Next() );

            try
            {

                if ( cell.ParentTile.IsOutOfBoundsTile )
                    CreateFullListOfSpots_OutOfBoundsTile( cell );
                else
                {
                    FindPossibleItems_Normal( cell );
                    int cellsAtStart = existingSpots.Count;
                    CreateFullListOfSpots_Normal( cell );

                    //ArcenDebugging.LogSingleLine( "Moved from " + cellsAtStart + " to " + cell.AllOutdoorSpots.Count, Verbosity.DoNotShow );
                }

                cell.HasEverCalculatedOutdoorSpots = true;
            }
            catch ( Exception e )
            {
                cell.HasEverCalculatedOutdoorSpots = false;
                ArcenDebugging.LogSingleLine( "GenerateOutdoorSpots Exception.  Will retry in a moment.  Error: " + e, Verbosity.ShowAsError );
            }
        }

        private void FindPossibleItems_Normal( MapCell cell )
        {
            possibleItems.Clear();
            blockingBuildings.Clear();
            existingSpots.Clear();

            foreach ( MapItem item in cell.AllRoads )
            {
                if ( item?.Type == null )
                    continue;
                if ( !item.OBBCache.HasBeenSet )
                    item.FillOBBCache();

                possibleItems.Add( item );
            }

            if ( cell.ParentTile.CellsList.Count > 1 )
            {
                ArcenFloatRectangle cellRect = cell.CellRect;
                float cellMaxX = (float)cellRect.XMax;
                float cellMinX = (float)cellRect.XMin;
                float cellMaxZ = (float)cellRect.YMax;
                float cellMinZ = (float)cellRect.YMin;

                foreach ( MapCell oCell in cell.ParentTile.CellsList )
                {
                    foreach ( MapOutdoorSpot spot in oCell.AllOutdoorSpots )
                        existingSpots.Add( spot.Position );

                    foreach ( KeyValuePair<int, MapItem> kv in oCell.BuildingDict )
                    {
                        MapItem item = kv.Value;
                        if ( item?.Type == null )
                            continue;
                        if ( item.Type?.Building?.Type?.IsSkippedDuringRelaxedCollisions ?? false )
                            continue;

                        if ( !item.OBBCache.HasBeenSet )
                            item.FillOBBCache();

                        AABB outerAABB = item.OBBCache.GetOuterAABB();
                        Vector3 size = outerAABB.Size;
                        size.x += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
                        size.z += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
                        outerAABB.Size = size;

                        Vector3 min = outerAABB.Min;
                        Vector3 max = outerAABB.Max;

                        if ( max.x < cellMinX )
                            continue; //we can't intersect on the x
                        if ( max.z < cellMinZ )
                            continue; //we can't intersect on the x
                        if ( min.x > cellMaxX )
                            continue; //we can't intersect on the x
                        if ( min.z > cellMaxZ )
                            continue; //we can't intersect on the x

                        blockingBuildings.Add( ThreeTuple<MapItem, Vector3, Vector3>.Create( item, min, max ) );
                    }
                }

                blockingFences.Clear();
                foreach ( MapCell oCell in cell.ParentTile.CellsList )
                {
                    foreach ( MapItem item in oCell.Fences )
                    {
                        if ( item?.Type == null )
                            continue;
                        if ( item?.Type?.ExtraPlaceableData?.IsSkippedDuringRelaxedCollisions ?? false )
                            continue;

                        if ( !item.OBBCache.HasBeenSet )
                            item.FillOBBCache();

                        AABB outerAABB = item.OBBCache.GetOuterAABB();
                        Vector3 size = outerAABB.Size;
                        size.x += (REQUIRED_DISTANCE_FROM_FENCES + REQUIRED_DISTANCE_FROM_FENCES);
                        size.z += (REQUIRED_DISTANCE_FROM_FENCES + REQUIRED_DISTANCE_FROM_FENCES);
                        outerAABB.Size = size;

                        Vector3 min = outerAABB.Min;
                        Vector3 max = outerAABB.Max;

                        if ( max.x < cellMinX )
                            continue; //we can't intersect on the x
                        if ( max.z < cellMinZ )
                            continue; //we can't intersect on the x
                        if ( min.x > cellMaxX )
                            continue; //we can't intersect on the x
                        if ( min.z > cellMaxZ )
                            continue; //we can't intersect on the x

                        blockingFences.Add( ThreeTuple<MapItem, Vector3, Vector3>.Create( item, min, max ) );
                    }
                }
            }
            else
            {
                foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots )
                    existingSpots.Add( spot.Position );

                foreach ( KeyValuePair<int, MapItem> kv in cell.BuildingDict )
                {
                    MapItem item = kv.Value;
                    if ( item?.Type == null )
                        continue;
                    if ( item.Type?.Building?.Type?.IsSkippedDuringRelaxedCollisions ?? false )
                        continue;

                    if ( !item.OBBCache.HasBeenSet )
                        item.FillOBBCache();

                    AABB outerAABB = item.OBBCache.GetOuterAABB();
                    Vector3 size = outerAABB.Size;
                    size.x += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
                    size.z += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
                    outerAABB.Size = size;

                    blockingBuildings.Add( ThreeTuple<MapItem, Vector3, Vector3>.Create( item, outerAABB.Min, outerAABB.Max ) );
                }

                blockingFences.Clear();
                foreach ( MapItem item in cell.Fences )
                {
                    if ( item?.Type == null )
                        continue;
                    if ( item?.Type?.ExtraPlaceableData?.IsSkippedDuringRelaxedCollisions ?? false )
                        continue;

                    if ( !item.OBBCache.HasBeenSet )
                        item.FillOBBCache();

                    AABB outerAABB = item.OBBCache.GetOuterAABB();
                    Vector3 size = outerAABB.Size;
                    size.x += (REQUIRED_DISTANCE_FROM_FENCES + REQUIRED_DISTANCE_FROM_FENCES);
                    size.z += (REQUIRED_DISTANCE_FROM_FENCES + REQUIRED_DISTANCE_FROM_FENCES);
                    outerAABB.Size = size;

                    blockingFences.Add( ThreeTuple<MapItem, Vector3, Vector3>.Create( item, outerAABB.Min, outerAABB.Max ) );
                }
            }

            //ArcenDebugging.LogSingleLine( "Fences for cell " + cell.CellID + ": " + cell.Fences.Count, Verbosity.DoNotShow );

            //blockingRoads.Clear();
            //foreach ( MapItem item in cell.AllRoads )
            //{
            //    if ( !item.OBBCache.HasBeenSet )
            //        item.FillOBBCache();

            //    AABB outerAABB = item.OBBCache.OBB.GetAABBFromCornerPoints();
            //    Vector3 size = outerAABB.Size;
            //    size.x += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
            //    size.z += (REQUIRED_DISTANCE_FROM_BUILDINGS + REQUIRED_DISTANCE_FROM_BUILDINGS);
            //    outerAABB.Size = size;

            //    blockingRoads.Add( Pair<MapItem, AABB>.Create( item, outerAABB ) );
            //}
        }

        private void CreateFullListOfSpots_Normal( MapCell cell )
        {
            newlyAddedSpots.Clear();

            Vector3 cellCenter = cell.Center;
            float cellMinX = cellCenter.x - CityMap.CELL_HALF_SIZE;
            float cellMaxX = cellCenter.x + CityMap.CELL_HALF_SIZE;
            float cellMinZ = cellCenter.z - CityMap.CELL_HALF_SIZE;
            float cellMaxZ = cellCenter.z + CityMap.CELL_HALF_SIZE;

            cellMinX += REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMaxX -= REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMinZ += REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMaxZ -= REQUIRED_DISTANCE_FROM_CELL_EDGE;

            //this first set is based on roads

            ArcenArrays.Randomize( possibleItems, random, 3 );
            foreach ( MapItem spot in possibleItems )
            {
                Vector3 newSpot = spot.OBBCache.TopCenter;
                newSpot.y += MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD;

                if ( newSpot.x <= cellMinX || newSpot.x >= cellMaxX )
                    continue; //too close to the edge!
                if ( newSpot.z <= cellMinZ || newSpot.z >= cellMaxZ )
                    continue; //too close to the edge!

                if ( !TestValidityOfSpot( newSpot ) || TestBuildingIntersectionOfSpot( newSpot ) ||
                    TestFenceIntersectionOfSpot( newSpot ) )
                    continue; //too close to an existing spot!

                //if we got here, then we're valid
                existingSpots.Add( newSpot );

                MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                outdoorSpot.Position = newSpot;
                outdoorSpot.IsOnRoad = true;
                cell.AllOutdoorSpots.Add( outdoorSpot );
            }
            possibleItems.Clear();

            //this next set is based on empty space
            possibleSpots.Clear();
            for ( float x = cellMinX; x <= cellMaxX; x += OFFROAD_GRID_CHECK_DENSITY )
            {
                for ( float z = cellMinZ; z <= cellMaxZ; z += OFFROAD_GRID_CHECK_DENSITY )
                {
                    Vector3 testSpot = new Vector3( x, MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD, z );
                    if ( TestValidityOfSpot( testSpot ) && !TestBuildingIntersectionOfSpot( testSpot ) && 
                        !TestFenceIntersectionOfSpot( testSpot ) )// && 
                        //!TestRoadIntersectionOfSpot( testSpot ) )
                        possibleSpots.Add( testSpot );
                }
            }

            ArcenArrays.Randomize( possibleSpots, random, 3 );

            //so here are the items that passed the initial round of checks
            foreach ( Vector3 newSpot in possibleSpots ) 
            {
                if ( !TestIfSpotIsNearANewSpot( newSpot ) )
                    continue; //too close to an existing spot!

                //if we got here, then we're valid
                newlyAddedSpots.Add( newSpot );

                MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                outdoorSpot.Position = newSpot;
                cell.AllOutdoorSpots.Add( outdoorSpot );
            }
            possibleSpots.Clear();
        }

        private void CreateFullListOfSpots_OutOfBoundsTile( MapCell cell )
        {
            existingSpots.Clear();
            newlyAddedSpots.Clear();

            Vector3 cellCenter = cell.Center;
            float cellMinX = cellCenter.x - CityMap.CELL_HALF_SIZE;
            float cellMaxX = cellCenter.x + CityMap.CELL_HALF_SIZE;
            float cellMinZ = cellCenter.z - CityMap.CELL_HALF_SIZE;
            float cellMaxZ = cellCenter.z + CityMap.CELL_HALF_SIZE;

            cellMinX += REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMaxX -= REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMinZ += REQUIRED_DISTANCE_FROM_CELL_EDGE;
            cellMaxZ -= REQUIRED_DISTANCE_FROM_CELL_EDGE;

            possibleItems.Clear();

            //this next set is based on empty space
            possibleSpots.Clear();
            for ( float x = cellMinX; x <= cellMaxX; x += OFFROAD_GRID_CHECK_DENSITY_OUT_OF_BOUNDS )
            {
                for ( float z = cellMinZ; z <= cellMaxZ; z += OFFROAD_GRID_CHECK_DENSITY_OUT_OF_BOUNDS )
                {
                    Vector3 testSpot = new Vector3( x, BASE_HEIGHT_OUT_OF_BOUNDS, z );
                    if ( TestValidityOfSpot( testSpot ) )
                        possibleSpots.Add( testSpot );
                }
            }

            ArcenArrays.Randomize( possibleSpots, random, 3 );

            //so here are the items that passed the initial round of checks
            foreach ( Vector3 newSpot in possibleSpots )
            {
                if ( !TestIfSpotIsNearANewSpot( newSpot ) )
                    continue; //too close to an existing spot!

                //if we got here, then we're valid
                newlyAddedSpots.Add( newSpot );

                MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                outdoorSpot.Position = newSpot;
                cell.AllOutdoorSpots.Add( outdoorSpot );
            }

            possibleSpots.Clear();
        }

        #region TestIfSpotIsNearANewSpot
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private bool TestIfSpotIsNearANewSpot( Vector3 newSpot )
        {
            foreach ( Vector3 spot in newlyAddedSpots )
            {
                float xDiff = newSpot.x - spot.x;
                if ( xDiff < 0 )
                    xDiff = -xDiff;
                if ( xDiff > REQUIRED_DISTANCE_FROM_OTHER_SPOTS )
                    continue; // no conflict, far enough away
                float zDiff = newSpot.z - spot.z;
                if ( zDiff < 0 )
                    zDiff = -zDiff;
                if ( zDiff > REQUIRED_DISTANCE_FROM_OTHER_SPOTS )
                    continue; // no conflict, far enough away
                return false; //conflict, too close on both axes
            }
            return true; //this is a valid spot
        }
        #endregion

        #region TestValidityOfSpot
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private bool TestValidityOfSpot( Vector3 newSpot )
        {
            foreach ( Vector3 spot in existingSpots )
            {
                float xDiff = newSpot.x - spot.x;
                if ( xDiff < 0 )
                    xDiff = -xDiff;
                if ( xDiff > REQUIRED_DISTANCE_FROM_OTHER_SPOTS )
                    continue; // no conflict, far enough away
                float zDiff = newSpot.z - spot.z;
                if ( zDiff < 0 )
                    zDiff = -zDiff;
                if ( zDiff > REQUIRED_DISTANCE_FROM_OTHER_SPOTS )
                    continue; // no conflict, far enough away
                return false; //conflict, too close on both axes
            }
            return true; //this is a valid spot
        }
        #endregion

        #region TestBuildingIntersectionOfSpot
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private bool TestBuildingIntersectionOfSpot( Vector3 newSpot )
        {
            foreach ( ThreeTuple<MapItem, Vector3, Vector3> tuple in blockingBuildings )
            {
                Vector3 max = tuple.ThirdItem;
                //if ( max.y < newSpot.y ) ignore the y
                //    continue; //if we're above it, then skip us

                if ( max.x < newSpot.x )
                    continue; //we can't intersect on the x
                if ( max.z < newSpot.z )
                    continue; //we can't intersect on the x

                Vector3 min = tuple.SecondItem;

                if ( min.x > newSpot.x )
                    continue; //we can't intersect on the x
                if ( min.z > newSpot.z )
                    continue; //we can't intersect on the x

                return true; //we might be contained in it, yikes!
            }
            return false; //no buildings were intersected
        }
        #endregion

        #region TestFenceIntersectionOfSpot
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private bool TestFenceIntersectionOfSpot( Vector3 newSpot )
        {
            foreach ( ThreeTuple<MapItem, Vector3, Vector3> tuple in blockingFences )
            {
                Vector3 max = tuple.ThirdItem;
                //if ( max.y < newSpot.y ) ignore the y
                //    continue; //if we're above it, then skip us

                if ( max.x < newSpot.x )
                    continue; //we can't intersect on the x
                if ( max.z < newSpot.z )
                    continue; //we can't intersect on the x

                Vector3 min = tuple.SecondItem;

                if ( min.x > newSpot.x )
                    continue; //we can't intersect on the x
                if ( min.z > newSpot.z )
                    continue; //we can't intersect on the x

                return true; //we might be contained in it, yikes!
            }
            return false; //no buildings were intersected
        }
        #endregion

        //#region TestRoadIntersectionOfSpot
        //[MethodImpl( MethodImplOptions.AggressiveInlining )]
        //private bool TestRoadIntersectionOfSpot( Vector3 newSpot )
        //{
        //    foreach ( Pair<MapItem, AABB> pair in blockingRoads )
        //    {
        //        AABB testAABB = pair.RightItem;
        //        Vector3 max = testAABB.Max;
        //        //if ( max.y < newSpot.y ) ignore the y
        //        //    continue;

        //        if ( max.x < newSpot.x )
        //            continue; //we can't intersect on the x
        //        if ( max.z < newSpot.z )
        //            continue; //we can't intersect on the x

        //        Vector3 min = testAABB.Min;

        //        if ( min.x > newSpot.x )
        //            continue; //we can't intersect on the x
        //        if ( min.z > newSpot.z )
        //            continue; //we can't intersect on the x

        //        return true; //we might be contained in it, yikes!
        //    }
        //    return false; //no roads were intersected
        //}
        //#endregion
    }
}

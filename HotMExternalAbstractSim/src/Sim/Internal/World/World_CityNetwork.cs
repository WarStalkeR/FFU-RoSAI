//using Arcen.Universal;
//using System;
//using UnityEngine;
//using Arcen.HotM.Core;
//using System.Diagnostics;

//namespace Arcen.HotM.External
//{
//	/// <summary>
//	/// Central data about how the world is connected
//	/// </summary>
//	internal class World_CityNetwork : ISimWorld_CityNetwork
//    {
//        public static World_CityNetwork QueryInstance = new World_CityNetwork();

//        //
//        //Serialized data
//        //-----------------------------------------------------
//        public static bool HasStartedCalculationOfNetwork = false;
//        public static bool HasFinishedCalculationOfNetwork = false;

//        //
//        //Nonserialized data
//        //-----------------------------------------------------
//        private static readonly DictionaryOfLists<int, MapCell> CellsPerThread = DictionaryOfLists<int, MapCell>.Create_WillNeverBeGCed( 10, 300, "World_Network-CellsPerThread" );
//        private static int ThreadsStillRunning = 0;

//        //
//        //Could be serialized, but easily calculable data
//        //----------------------------------------------------- 
//        private const int MAX_THREAD_COUNT = 8;

//        public static void OnGameClear()
//        {
//            HasStartedCalculationOfNetwork = false;
//            HasFinishedCalculationOfNetwork = false;

//            CellsPerThread.Clear();
//            ThreadsStillRunning = 0;
//        }

//        #region Serialization
//        public static void Serialize( ArcenFileSerializer Serializer )
//        {
            
//        }

//        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
//        {
            
//        }
//        #endregion

//        #region CalculateNetworkOnBackgroundThreadIfNeeded
//        internal static void CalculateNetworkOnBackgroundThreadIfNeeded()
//        {
//            if ( HasStartedCalculationOfNetwork )
//                return;
//            HasStartedCalculationOfNetwork = true;

//            int randSeed = Engine_Universal.PermanentQualityRandom.Next();
//            if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.CalculateNetwork",
//                ( TaskStartData startData ) =>
//                {
//                    try
//                    {
//                        HasFinishedCalculationOfNetwork = false;

//                        rand.ReinitializeWithSeed( randSeed );
//                        ActuallyCalculateNetworkOnBackgroundThread();

//                        HasFinishedCalculationOfNetwork = true;
//                    }
//                    catch ( Exception e )
//                    {
//                        ArcenDebugging.LogWithStack( "World_Network.CalculateNetwork background thread error: " + e, Verbosity.ShowAsError );
//                    }
//                    Interlocked.Exchange( ref CityMap.IsCurrentlyAddingMoreMapTiles, 0 );
//                } ) )
//            {
//                //we failed to start!
//                Interlocked.Exchange( ref CityMap.IsCurrentlyAddingMoreMapTiles, 0 );
//            }
//        }
//        #endregion

//        private static MersenneTwister rand = new MersenneTwister( 0 );
//        private static void ActuallyCalculateNetworkOnBackgroundThread()
//        {
//            Stopwatch sw = Stopwatch.StartNew();

//            //sort into CellsPerThread.  Yes I know I could use modulus, but it makes me nervous
//            {
//                CellsPerThread.Clear();
//                int threadIndex = 0;
//                foreach ( MapCell cell in CityMap.Cells )
//                {
//                    CellsPerThread.GetListForOrCreate( threadIndex ).Add( cell );
//                    threadIndex++;
//                    if ( threadIndex >= MAX_THREAD_COUNT )
//                        threadIndex = 0;
//                }
//            }

//            ThreadsStillRunning = 0;
//            foreach ( KeyValuePair<int, List<MapCell>> kv in CellsPerThread )
//            {
//                Interlocked.Increment( ref ThreadsStillRunning );
//                List<MapCell> cellList = kv.Value;
//                if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.CalculateNetwork_SubCellList",
//                    ( TaskStartData startData ) =>
//                    {
//                        try
//                        {
//                            foreach ( MapCell cell in cellList )
//                                DoInitialPerCellIndependentLinking( cell );
//                        }
//                        catch ( Exception e )
//                        {
//                            ArcenDebugging.LogWithStack( "World_Network.CalculateNetwork_SubCellList background thread error: " + e, Verbosity.ShowAsError );
//                        }
//                        Interlocked.Decrement( ref ThreadsStillRunning );
//                    } ) )
//                {
//                    //we failed to start!
//                    Interlocked.Decrement( ref ThreadsStillRunning );
//                    ArcenDebugging.LogSingleLine( "We failed to start background thread " + kv.Key + " from _Data.CalculateNetwork_SubCellList", Verbosity.ShowAsError );
//                }
//            }

//            //wait for the threads to finish running
//            while ( ThreadsStillRunning > 0 )
//            {
//                System.Threading.Thread.Sleep( 10 );
//                continue;
//            }

//            CellsPerThread.Clear();
//            int earlySoloTiming = (int)sw.ElapsedMilliseconds;
//            int markerAtStartOfPhase = (int)sw.ElapsedMilliseconds;

//            foreach ( MapCell cell in CityMap.Cells )
//            {
//                DoRoadCrossTileRechcks( cell );
//            }

//            int recheckTiming = (int)sw.ElapsedMilliseconds - markerAtStartOfPhase;
//            markerAtStartOfPhase = (int)sw.ElapsedMilliseconds;

//            int outerCount = 0;
//			int innerCount = 0;

//            foreach ( MapCell cell in CityMap.Cells )
//            {
//                outerCount++;
//                for ( int i = cell.CityNetwork.Count -1; i >= 0; i-- )
//                {
//                    CityNetworkNode roadNode = cell.CityNetwork[i];
//                    if ( roadNode.Connections.Count <= 0 )
//                        cell.CityNetwork.RemoveAt( i );
//                }

//                innerCount += cell.CityNetwork.Count;
//            }

//            int countAndRemovalTiming = (int)sw.ElapsedMilliseconds - markerAtStartOfPhase;
//            markerAtStartOfPhase = (int)sw.ElapsedMilliseconds;

//            //then we note when we are done
//            HasFinishedCalculationOfNetwork = true;

//            ArcenDebugging.LogSingleLine( "Finished calculating network in " + sw.ElapsedMilliseconds + " ms. outerCount: " + outerCount +
//                " innerCount: " + innerCount + " earlySoloTiming: " + earlySoloTiming + " recheckTiming: " + recheckTiming +
//                " countAndRemovalTiming: " + countAndRemovalTiming, Verbosity.DoNotShow );
//        }

//        #region DoInitialPerCellIndependentLinking
//        private static void DoInitialPerCellIndependentLinking( MapCell cell )
//        {
//            float laneWidth = -1;// Can be reasonably sure the lane width is consistent between prefabs

//            // Create node per road segment
//            //-----------------------------------------------
//            cell.CityNetwork.Clear();
//            foreach ( MapItem road in cell.DrivableRoads )
//            {
//                if ( laneWidth < 0 )
//                {
//                    List<LanePrefab> allLanes = road.Type.DrivingLanes;
//                    laneWidth = allLanes.FirstOrDefault.Type.LaneWidth;
//                }
//                CityNetworkNode n = new CityNetworkNode( road, cell );
//                cell.CityNetwork.Add( n );
//            }

//            // Build node network by checking lane connectivity within parent cell only
//            //-----------------------------------------------
//            for ( int i = cell.CityNetwork.Count -1; i >= 0; i--)
//            {
//                CityNetworkNode roadNode = cell.CityNetwork[i];
//                int debugStage = 0;
//                try
//                {
//                    debugStage = 100;
//                    MapItem roadA = roadNode.Item;
//                    if ( roadA == null )
//                        continue; //this is valid now

//                    debugStage = 300;
//                    float wid = laneWidth * roadNode.Item.Scale.y;

//                    debugStage = 700;
//                    List<LanePrefab> list = roadA.Type.DrivingLanes;
//                    debugStage = 800;
//                    if ( list != null )
//                    {
//                        debugStage = 2100;
//                        foreach ( LanePrefab laneA in list )
//                        {
//                            debugStage = 2200;
//                            Vector3 p1 = roadA.TransformPoint_Threadsafe( laneA.FirstPoint );
//                            debugStage = 2300;
//                            Vector3 p2 = roadA.TransformPoint_Threadsafe( laneA.LastPoint );
//                            bool p1c = laneA.IsDeadEnd;
//                            bool p2c = laneA.IsDeadEnd;
//                            debugStage = 2200;
//                            foreach ( CityNetworkNode n in cell.CityNetwork )
//                            {
//                                MapItem roadB = n.Item;
//                                if ( roadB == null )
//                                    continue; //this is allowed now

//                                if ( DoLanesConnect( roadB, p1, wid ) )
//                                {
//                                    roadNode.AddConnection( n );
//                                    n.AddConnection( roadNode );
//                                    p1c = true;
//                                }
//                                else if ( DoLanesConnect( roadB, p2, wid ) )
//                                {
//                                    roadNode.AddConnection( n );
//                                    n.AddConnection( roadNode );
//                                    p2c = true;
//                                }
//                            }

//                            if ( !p1c )
//                            {
//                                CityNetworkNodeToRecheck recheck;
//                                recheck.LanePoint = p1;
//                                recheck.wid = wid;
//                                recheck.NetworkNode = roadNode;
//                                cell.MapGen_CityNetworkNodesToRecheck.Add( recheck );
//                            }

//                            if ( !p2c )
//                            {
//                                CityNetworkNodeToRecheck recheck;
//                                recheck.LanePoint = p2;
//                                recheck.wid = wid;
//                                recheck.NetworkNode = roadNode;
//                                cell.MapGen_CityNetworkNodesToRecheck.Add( recheck );
//                            }
//                        }
//                    }
//                }
//                catch ( Exception e )
//                {
//                    ArcenDebugging.LogDebugStageWithStack( "Inner network error", debugStage, e, Verbosity.ShowAsError );
//                }

//                if ( roadNode.Connections.Count == 0 )
//                    cell.CityNetwork.RemoveAt( i );
//            }

//            //for buildings that are meant to be in the road network, find adjacent roads within short distance
//            foreach ( MapItem bld in cell.AllBuildings )
//            {
//                if ( cell.AllRoads.Contains( bld ) ) continue;
//                BuildingType bldType = bld.SimBuilding.GetPrefab().Type;
//                if ( bldType.IsExcludedFromCityNetwork ) continue;
//                CityNetworkNode n = GetNodeForBuilding( bld, cell );
//                if ( n == null )
//                    continue;

//                ArcenFloatRectangle boundsH = bld.OBBCache.GetOuterRect().ExpandHorizontally( 0.2 ).ExpandVertically( -0.5 );
//                ArcenFloatRectangle boundsV = bld.OBBCache.GetOuterRect().ExpandVertically( 0.2 ).ExpandHorizontally( -0.5 );
//                foreach ( CityNetworkNode road in cell.CityNetwork )
//                {
//                    // ignore any dead ends and buildings
//                    if ( road.isDeadEnd || road.isBuilding )
//                        continue;
//                    ArcenFloatRectangle rect = road.Item.OBBCache.GetOuterRect();
//                    if ( rect.BasicIntersectionTest( boundsH ) || rect.BasicIntersectionTest( boundsV ) )
//                    {
//                        road.AddConnection( n );
//                        n.AddConnection( road );
//                    }
//                }
//            }
//            // after all buildings have found nearby roads...
//            // ...check to see if a building failed to find a nearby road
//            // if so, see if a different nearby building has a nearby road instead
//            foreach ( MapItem bld in cell.AllBuildings )
//            {
//                if ( cell.AllRoads.Contains( bld ) ) continue;
//                BuildingType bldType = bld.SimBuilding.GetPrefab().Type;
//                if ( bldType.IsExcludedFromCityNetwork ) continue;
//                CityNetworkNode n1 = GetNodeForBuilding( bld, cell );
//                if ( n1 == null ) continue;
//                if ( n1.Connections.Count > 0 ) continue;

//                ArcenFloatRectangle boundsH = bld.OBBCache.GetOuterRect().ExpandInAllDirections( 1 );
//                foreach ( MapItem bld2 in cell.AllBuildings )
//                {
//                    if ( bld2 == bld || cell.AllRoads.Contains( bld2 ) ) continue;
//                    BuildingType bldType2 = bld2.SimBuilding.GetPrefab().Type;
//                    if ( bldType2.IsExcludedFromCityNetwork ) continue;
//                    CityNetworkNode n2 = GetNodeForBuilding( bld2, cell );
//                    if ( n2 == null || n2.isDeadEnd || !n2.isBuilding ) continue;
//                    ArcenFloatRectangle rect = n2.Item.OBBCache.GetOuterRect();
//                    if ( !rect.BasicIntersectionTest( boundsH ) ) continue;
//                    if ( n2.Connections.GetFirstOrDefaultWhere( c => !c.isBuilding ) == null ) continue;
//                    n1.AddConnection( n2 );
//                    n2.AddConnection( n1 );
//                }
//            }
//        }
//        #endregion

//        #region DoRoadCrossTileRechcks
//        private static void DoRoadCrossTileRechcks( MapCell cell )
//        {
//            if ( cell.MapGen_CityNetworkNodesToRecheck.Count <= 0 ) 
//                return;

//            foreach ( CityNetworkNodeToRecheck recheck in cell.MapGen_CityNetworkNodesToRecheck )
//            {
//                if ( !CrossCell_CanConnectToNeighborCells( recheck.NetworkNode, recheck.LanePoint, recheck.wid, cell ) )
//                {
//                    //dead end
//                    CityNetworkNode end = new CityNetworkNode( recheck.LanePoint, cell );
//                    recheck.NetworkNode.Connections.Add( end );
//                    cell.CityNetwork.Add( end );
//                }
//            }
//        }
//        #endregion

//        private static CityNetworkNode GetNodeForBuilding(MapItem bld, MapCell cell)
//        {
//	        if (bld.SimBuilding == null) 
//                return null;

//            CityNetworkNode node = cell.CityNetwork.GetFirstOrDefaultWhere( n => n.isBuilding && n.Item == bld );
//            if ( node != null )
//                return node;

//            node = new CityNetworkNode( bld, cell );
//            cell.CityNetwork.Add( node );
//            return node;
//        }

//        #region CrossCell_CanConnectToNeighborCells
//        private static bool CrossCell_CanConnectToNeighborCells( CityNetworkNode roadA, Vector3 p1, float wid, MapCell cell )
//		{
//            if ( CrossCell_CanConnectToSpecificNeighborCell( roadA, p1, wid, cell.CellNorth ) )
//                return true;
//            if ( CrossCell_CanConnectToSpecificNeighborCell( roadA, p1, wid, cell.CellSouth ) )
//                return true;
//            if ( CrossCell_CanConnectToSpecificNeighborCell( roadA, p1, wid, cell.CellEast ) )
//                return true;
//            if ( CrossCell_CanConnectToSpecificNeighborCell( roadA, p1, wid, cell.CellWest ) )
//                return true;

//            return false;
//		}
//        #endregion

//        #region CrossCell_CanConnectToSpecificNeighborCell
//        private static bool CrossCell_CanConnectToSpecificNeighborCell( CityNetworkNode roadA, Vector3 p1, float wid, MapCell cell )
//        {
//            if ( cell == null ) 
//                return false;
//            List<CityNetworkNode> list = cell.CityNetwork;
//            foreach ( CityNetworkNode n in list )
//            {
//                if ( n.isDeadEnd ) continue;
//                if ( DoLanesConnect( n.Item, p1, wid * 2 ) )
//                {
//                    roadA.AddConnection( n );
//                    n.AddConnection( roadA );
//                    return true;
//                }
//            }

//            return false;
//        }
//        #endregion

//        private static bool DoLanesConnect(MapItem roadB, Vector3 p1, float wid )
//        {
//            if ( roadB == null ) 
//                return false;
//            List<LanePrefab> list = roadB?.Type?.DrivingLanes;
//            if ( list == null || list.Count <= 0 ) 
//                return false;

//            foreach (LanePrefab laneB in list )
//	        {
//                if ( laneB == null) 
//                    continue;

//		        Vector3 p3 = roadB.TransformPoint_Threadsafe(laneB.FirstPoint);
//		        float distToX = p1.x - p3.x;
//		        float distToY = p1.y - p3.y;
//		        float distToZ = p1.z - p3.z;
//		        distToX = distToX < 0 ? -distToX : distToX;
//		        distToY = distToY < 0 ? -distToY : distToY;
//		        distToZ = distToZ < 0 ? -distToZ : distToZ;
//		        if (distToX < wid / 2 && distToY < wid / 2 && distToZ < wid / 2) return true;

//		        Vector3 p4 = roadB.TransformPoint_Threadsafe(laneB.LastPoint);

//		        distToX = p1.x - p4.x;
//		        distToY = p1.y - p4.y;
//		        distToZ = p1.z - p4.z;
//		        distToX = distToX < 0 ? -distToX : distToX;
//		        distToY = distToY < 0 ? -distToY : distToY;
//		        distToZ = distToZ < 0 ? -distToZ : distToZ;
//		        if (distToX < wid / 2 && distToY < wid / 2 && distToZ < wid / 2) return true;
//	        }

//	        return false;
//        }

//		public bool GetHasFinishedCalculationOfCityNetwork()
//		{
//			return HasFinishedCalculationOfNetwork;
//		}
//    }
//}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;
using Arcen.Universal.Deserialization;
using System.Security.AccessControl;

namespace Arcen.HotM.External
{
    internal static class Helper_AddSeedSpotsAndPOIGuards
    {
        private static int IsRunningThread = 0;
        public static void AddSeedSpots_AndMaybePOIGuardsIfNeeded( bool SkipThePOIsPart )
        {
            if ( CityMap.GetIsAddingMoreItems() )
                return;

            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //Willard reports that sometimes the mapgen and the tile filling race, so this waits until they are done
            if ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                return; //if we haven't yet finished initializing absolutely everything, then don't run this yet

            if ( CityMap.TilesBeingPostProcessed_Decorations > 0 || CityMap.WorkingTilesToPostProcess_Decoration.Count > 0 )
                return; //not done yet!

            if ( SimCommon.NeedsVisibilityGranterRecalculation )
                return; //don't start this if the visibility granter has not calculated yet

            if ( IsRunningThread > 0 )
                return;

            if ( Interlocked.Exchange( ref IsRunningThread, 1 ) != 0 )
                return;

            int randSeed = Engine_Universal.PermanentQualityRandom.Next();
            if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.AddSeedSpotsAndPOIGuards",
                ( TaskStartData startData ) =>
                {
                    while ( CityMap.GetIsAddingMoreItems() )
                        System.Threading.Thread.Sleep( 100 ); //wait 100ms

                    try
                    {
                        MersenneTwister rand = new MersenneTwister( randSeed );
                        AddAnyMissingOutdoorSpotsOnCells();
                        if ( !SkipThePOIsPart )
                            AddAnyMissingNPCsToPOIs( rand );
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogWithStack( "_Data.AddSeedSpotsAndPOIGuards background thread error: " + e, Verbosity.ShowAsError );
                    }
                    Interlocked.Exchange( ref IsRunningThread, 0 );
                } ) )
            {
                ArcenDebugging.LogWithStack( "_Data.AddSeedSpotsAndPOIGuards background failed to start!", Verbosity.ShowAsError );
                Interlocked.Exchange( ref IsRunningThread, 0 );
            }
        }

        private static Helper_CalculateOutdoorSpots[] OutdoorSeeders = null;

        #region AddAnyMissingOutdoorSpotsOnCells
        private static void AddAnyMissingOutdoorSpotsOnCells()
        {
            Stopwatch sw = Stopwatch.StartNew();

            if ( OutdoorSeeders == null )
            {
                OutdoorSeeders = new Helper_CalculateOutdoorSpots[12];
                for ( int i = 0; i < OutdoorSeeders.Length; i++ )
                    OutdoorSeeders[i] = new Helper_CalculateOutdoorSpots();
            }

            for ( int i = 0; i < OutdoorSeeders.Length; i++ )
                OutdoorSeeders[i].AllCellsToCheck.Clear();

            int currentOutdoorSeederIndex = 0;

            int cellCount = 0;
            foreach ( MapCell cell in CityMap.Cells )
            {
                if ( !cell.HasEverCalculatedOutdoorSpots )
                {
                    if ( cell.AllOutdoorSpots.Count > 0 && cell.ParentTile.IsOutOfBoundsTile )
                    {
                        cell.HasEverCalculatedOutdoorSpots = true;
                        continue;
                    }

                    cellCount++;

                    OutdoorSeeders[currentOutdoorSeederIndex].AllCellsToCheck.Add( cell );
                    currentOutdoorSeederIndex++;
                    if ( currentOutdoorSeederIndex >= OutdoorSeeders.Length )
                        currentOutdoorSeederIndex = 0;
                }
            }

            int addedSpots = 0;
            int threadsStillRunning = 0;

            for ( int i = 0; i < OutdoorSeeders.Length; i++ )
            {
                Helper_CalculateOutdoorSpots seeder = OutdoorSeeders[i];
                if ( seeder == null || seeder.AllCellsToCheck.Count == 0 )
                    continue;

                Interlocked.Increment( ref threadsStillRunning );
                if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.AddSeedSpotsSubThread",
                    ( TaskStartData startData ) =>
                    {
                        try
                        {
                            foreach ( MapCell cell in seeder.AllCellsToCheck )
                            {
                                int initial = cell.AllOutdoorSpots.Count;
                                seeder.GenerateOutdoorSpots( cell );
                                Interlocked.Add( ref addedSpots,( cell.AllOutdoorSpots.Count - initial) );
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogWithStack( "World_Network.CalculateNetwork_SubCellList background thread error: " + e, Verbosity.ShowAsError );
                        }
                        Interlocked.Decrement( ref threadsStillRunning );
                    } ) )
                {
                    //we failed to start!
                    Interlocked.Decrement( ref threadsStillRunning );
                    ArcenDebugging.LogSingleLine( "We failed to start background thread " + i + " from _Data.AddSeedSpotsSubThread", Verbosity.ShowAsError );
                }
            }

            //wait for the threads to finish running
            while ( threadsStillRunning > 0 )
            {
                System.Threading.Thread.Sleep( 10 );
                continue;
            }

            ArcenDebugging.LogSingleLine( "Finished adding outdoor spots " + cellCount + " cells, adding " +
                addedSpots + " spots in the process.  This took " + sw.ElapsedMilliseconds.ToStringThousandsWhole() + "ms", Verbosity.DoNotShow );
        }
        #endregion

        #region AddAnyMissingNPCsToPOIs
        private static void AddAnyMissingNPCsToPOIs( MersenneTwister rand )
        {
            Stopwatch sw = Stopwatch.StartNew();
            int npcCount = World_Forces.AllNPCUnitsByID.Count;
            int poiCount = 0;
            foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
            {
                if ( kv.Value == null )
                    continue;
                MapPOI poi = kv.Value;
                if ( !poi.HasDoneInitialGuardSeeding && poi.ControlledBy != null )
                {
                    poiCount++;
                    poi.TryInitialGuardSeeding( rand );
                }
            }
            int addedNPCs = World_Forces.AllNPCUnitsByID.Count - npcCount;

            ArcenDebugging.LogSingleLine( "Finished adding initial NPC Guards for " + poiCount + " pois, adding " +
                addedNPCs + " guard NPCs in the process.  This took " + sw.ElapsedMilliseconds.ToStringThousandsWhole() + "ms", Verbosity.DoNotShow );
        }
        #endregion
    }
}

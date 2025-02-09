using System;



using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Logic for things that happen every second, whether the game is paused or not
    /// </summary>
    internal static class SimPerFullSecond
    {
        internal static readonly Stopwatch SimLoopOuterStopwatch = new Stopwatch();
        internal static readonly Stopwatch SimLoopInnerStopwatch = new Stopwatch();
        private static readonly Stopwatch SimFramingStopwatch = new Stopwatch();

        //
        //All important variables should be above this point, so we can be sure to clear them.
        //-----------------------------------------------------------------

        #region OnGameClear
        public static void OnGameClear()
        {
            SimLoopOuterStopwatch.Stop();
            SimLoopOuterStopwatch.Reset();

            SimLoopInnerStopwatch.Stop();
            SimLoopInnerStopwatch.Reset();

            SimFramingStopwatch.Stop();
            SimFramingStopwatch.Reset();
        }
        #endregion

        public static void DoPerFullSecondLogic( MersenneTwister RandForThisSecond )
        {
            SimFramingStopwatch.Restart();

            DoPerFullSecondLogic_Inner( RandForThisSecond );

            SimFramingStopwatch.Stop();
            SimTimingInfo.PerFullSecond.LogCurrentTicks( (int)SimFramingStopwatch.ElapsedTicks );
        }

        private static void DoPerFullSecondLogic_Inner( MersenneTwister RandForThisSecond )
        {
            if ( !SimCommon.HasFullyStarted )
            {
                SimLoading.RunGameStartOnBGThread( RandForThisSecond );
                World_Forces.DoPerSecondLogic( true );
                return; //prevent errors when the game is still starting up
            }

            Helper_AssignFactionWorkers.StartRoundOfAssignmentsIfNotAlreadyRunning( RandForThisSecond.Next() );
            World_People.RecalculateCacheForResidentsAndWorkers();
            SimCommon.CacheCityComputedStatusValues();
            World_Buildings.DoAnyPerSecondLogic_SimThread( false );
            World_Forces.DoPerSecondLogic( false );
            SimCommon.DoCommonPerSecond( RandForThisSecond );
            RecalculateValidMinorEvents();
            //World_CityNetwork.CalculateNetworkOnBackgroundThreadIfNeeded();

            foreach ( CityFlag flag in CityFlagTable.FlagsWithCode )
                flag.ImplementationOrNull?.DoPerSecondEvenIfAlreadyTripped( flag, RandForThisSecond );

            foreach ( MetaFlag flag in MetaFlagTable.FlagsWithCode )
                flag.ImplementationOrNull?.DoPerSecondEvenIfAlreadyTripped( flag, RandForThisSecond );    

            //if ( !SimCommon.IsCurrentlyRunningSimTurn )
            //{
            //    long msAtStart = SimFramingStopwatch.ElapsedMilliseconds;

            //    foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
            //    {
            //        NPCUnit unit = kv.Value;
            //        if ( unit == null || unit.IsFullDead && unit.IsInvalid )
            //            continue;
            //        //todo actual aggro revisions as-needed
            //    }

            //    SimTimingInfo.PerSecondTargeting.LogCurrentMilliseconds( (int)(SimFramingStopwatch.ElapsedMilliseconds - msAtStart) );
            //}
            SimCommon.HasRunAtLeastOneRealSecond = true;
        }

        #region HandleDiscoveries
        internal static void HandleDiscoveries()
        {
            int debugStage = 0;
            try
            {
                bool isFogOfWarDisabled = SimCommon.IsFogOfWarDisabled;

                debugStage = 1000;

                foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
                {
                    MapPOI pOI = kv.Value;
                    if ( pOI == null )
                        continue;

                    if ( !pOI.HasBeenDiscovered )
                    {
                        if ( isFogOfWarDisabled )
                        {
                            pOI.HasBeenDiscovered = true;
                            continue;
                        }

                        MapItem building = pOI.BuildingOrNull;
                        if ( building != null )
                        {
                            MapCell cell = building.ParentCell;
                            MapTile tile = cell?.ParentTile;
                            if ( tile != null && tile.HasEverBeenExplored )
                                pOI.HasBeenDiscovered = true;
                        }
                        else
                        {
                            MapTile tile = pOI.Tile;
                            if ( tile != null && tile.HasEverBeenExplored )
                                pOI.HasBeenDiscovered = true;
                        }
                    }
                }

                debugStage = 5000;

                debugStage = 13000;
            }
            catch ( Exception ex )
            {
                ArcenDebugging.LogDebugStageWithStack( "HandleDiscoveries", debugStage, ex, Verbosity.ShowAsError );
            }
        }
        #endregion

        public static void HandleRecalculationsFromAfterDeserialization()
        {
            World_People.RecalculateCacheForResidentsAndWorkers();
            SimCommon.CacheCityComputedStatusValues();
            RecalculateValidMinorEvents(); //let's not have these pop in
        }

        #region RecalculateValidMinorEvents
        public static void RecalculateValidMinorEvents()
        {
            EventAggregation.ValidMinorEvents.ClearConstructionListForStartingConstruction();
            foreach ( NPCEvent ev in NPCEventTable.Instance.Rows )
            {
                if ( ev.MinorData == null )
                    continue; //not the proper kind of event
                if ( !ev.Event_CalculateMeetsPrerequisites( null, GateByLogicCheckType.CitySpecific, EventCheckReason.StandardSeeding, false ) )
                    continue; //this one is not valid at the moment for whatever reason

                //this one is, pending actor and building considerations
                EventAggregation.ValidMinorEvents.AddToConstructionList( ev );
            }
            EventAggregation.ValidMinorEvents.SwitchConstructionToDisplay();
        }
        #endregion
    }
}

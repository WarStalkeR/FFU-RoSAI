using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;
using Arcen.Universal.Deserialization;
using System.Security.AccessControl;

namespace Arcen.HotM.External
{
    /// <summary>
    /// A background thread for calculating how many workers for each faction work at any given building
    /// </summary>
    internal static class Helper_AssignFactionWorkers
    {
        public static Int64 NumberOfWorkerAssignmentLoopRun = 0;

        private static int countRunningRightNow = 0;

        private static readonly ThreadData threadData1 = new ThreadData();

        public static void OnGameClear()
        {
            countRunningRightNow = 0;
            NumberOfWorkerAssignmentLoopRun = 0;
        }

        #region StartRoundOfAssignmentsIfNotAlreadyRunning
        public static void StartRoundOfAssignmentsIfNotAlreadyRunning( int newRandSeedIfNeeded )
        {
            if ( countRunningRightNow > 0 )
                return;

            NumberOfWorkerAssignmentLoopRun++;

            //ArcenDebugging.LogSingleLine( "Start worker assignment loop " + NumberOfWorkerAssignmentLoopRun, Verbosity.DoNotShow );

            //The places where I do the Interlocked.Increment() are really critical for preventing some
            //cases where this would deadlock until a restart of the game, by the way.

            //Setting the countRunningRightNow to 2 on the calling thread before calling either
            //RunTaskOnBackgroundThread seems like it would work, but if either RunTaskOnBackgroundThread
            //does not run for some reason, then that would lead to it being forever > 0 and thus not
            //executing until after the next call of OnGameClear.

            //The outer catch statements, which probably seem paranoid, are also required for preventing that sort of deadlock case.

            //The inner catch statements are for preventing a single faction's error from stopping other factions running.

            //In general, for tasky things that don't collaborate with outside data in very many ways, I think that having
            //dedicated helper classes like this are a good way to keep people's fingers out of the mechanisms.

            if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_PerSec.AssignFactionWorkers",
                ( TaskStartData startData ) =>
                {
                    Interlocked.Increment( ref countRunningRightNow );
                    Stopwatch thread1SW = Stopwatch.StartNew();

                    try
                    {
                        foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                            kv.Value.AggregateResidentsAndWorkers();
                    }
                    catch ( Exception e ) //super important we catch these here, or strange things happen later
                    {
                        ArcenDebugging.LogSingleLine( "Outer Error on AggregateResidentsAndWorkers thread: " + e, Verbosity.ShowAsError );
                    }

                    try
                    {
                        DoWorkerAssignments( threadData1, newRandSeedIfNeeded );
                    }
                    catch ( Exception e ) //super important we catch these here, or strange things happen later
                    {
                        ArcenDebugging.LogSingleLine( "Outer Error on DoWorkerAssignments thread: " + e, Verbosity.ShowAsError );
                    }

                    try
                    {
                        foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                            kv.Value.AggregateResidentsAndWorkers();
                    }
                    catch ( Exception e ) //super important we catch these here, or strange things happen later
                    {
                        ArcenDebugging.LogSingleLine( "Outer Error on AggregateResidentsAndWorkers thread: " + e, Verbosity.ShowAsError );
                    }

                    Interlocked.Decrement( ref countRunningRightNow );

                    SimTimingInfo.PerSecondWorkerAssignment?.LogCurrentTicks( (int)thread1SW.ElapsedTicks );
                    thread1SW.Stop();
                } ) )
            {
                //failed to start the above
                Interlocked.Decrement( ref countRunningRightNow );
            }
        }
        #endregion

        #region DoWorkerAssignments
        /// <summary>
        /// Assign or reassign all of the workers for a given faction.
        /// </summary>
        private static void DoWorkerAssignments( ThreadData TData, int newRandSeed )
        {
            TData.Reset( newRandSeed );
            MersenneTwister rand = TData.Random;

            foreach ( KeyValuePair<int, SimBuilding> kvOuter in World_Buildings.BuildingsByID )
            {
                SimBuilding building = kvOuter.Value;

                //make a list of all the buildings that need workers
                if ( building.Prefab.NormalMaxJobs > 0 && !building.IsBlockedFromGettingMoreCitizens )
                {
                    Dictionary<ProfessionType, int> maxJobs = building.Prefab.NormalMaxJobsByProfession;
                    //get a full accounting of all the jobs available here
                    foreach ( KeyValuePair<ProfessionType, int> kv in maxJobs )
                    {
                        if ( kv.Value <= 0 )
                            continue;

                        TData.BuildingsByProfession.AddToList( kv.Key, building );
                        TData.NeededJobsProfDict[kv.Key] += kv.Value;
                    }

                    //get a full accounting of all the workers currently doing work
                    foreach ( KeyValuePair<ProfessionType, int> kv in building.Workers )
                    {
                        TData.AlreadyWorkingProfDict[kv.Key] += kv.Value;
                    }
                }
                //get a full accounting of all the residents who can do work
                if ( building.Prefab.NormalMaxResidents > 0 && !building.IsBlockedFromGettingMoreCitizens )
                {
                    foreach ( KeyValuePair<EconomicClassType, int> kv in building.Residents )
                    {
                        TData.CurrentResidents[kv.Key] += kv.Value;
                        TData.RemainingAvailableWorkers[kv.Key] += kv.Value;
                    }
                }
            }

            //okay, now we have all our macro data.  Time to go profession by profession

            //firstly, look at who is already working, and see if there is anywhere people are over-employed
            foreach ( KeyValuePair<ProfessionType, int> kv in TData.AlreadyWorkingProfDict )
            {
                ProfessionType profession = kv.Key;
                int currentWorkers = kv.Value;
                int remainingNeededWorkers = currentWorkers;

                foreach ( EconomicClassType type in profession.CanBeWorkedBy )
                {
                    TData.RemainingAvailableWorkers.TryGetValue( type, out int remainingAvail );
                    if ( remainingAvail > 0 )
                    {
                        if ( remainingAvail <= remainingNeededWorkers )
                        {
                            remainingNeededWorkers -= remainingAvail;
                            TData.RemainingAvailableWorkers[type] = 0;
                        }
                        else
                        {
                            TData.RemainingAvailableWorkers[type] -= remainingNeededWorkers;
                            remainingNeededWorkers = 0;
                        }
                    }
                }

                //at this point, we find out if any of the professions are over-booked.
                //aka, probably they had more workers at some point, 
                if ( remainingNeededWorkers > 0 )
                    TData.ExtraWorkersWhoDoNotExist[profession] = remainingNeededWorkers;
                else
                    TData.ExtraWorkersWhoDoNotExist[profession] = 0;
            }

            //if ( TData.NeededJobsProfDict.Count == 0 )
            //{
            //    ArcenDebugging.LogSingleLine( "Faction " + fac.GetDisplayName() + " thinks it needs no workers!", Verbosity.DoNotShow );
            //}

            //This layer of things is essentially randomized shuffling of workers into jobs that need them.
            //And shuffling of them OUT of jobs where more people supposedly work than actually are available now.
            foreach ( KeyValuePair<ProfessionType, int> kv in TData.NeededJobsProfDict )
            {
                ProfessionType profession = kv.Key;
                int neededWorkers = kv.Value;
                int currentWorkers = 0; TData.AlreadyWorkingProfDict.TryGetValue( profession, out currentWorkers );

                int extraWorkersThatDoNotExist = TData.ExtraWorkersWhoDoNotExist[profession];

                if ( extraWorkersThatDoNotExist > 0 )
                {
                    int startingExtra = extraWorkersThatDoNotExist;

                    //we have more workers than we should for this profession!
                    //let's reduce staff in buildings, then.

                    #region Staff Reduction
                    List<SimBuilding> buildingsForThisProfessionOrNull = TData.BuildingsByProfession.GetListForOrNull( profession );

                    List<SimBuilding> buildingsWithWorkers = TData.WorkingBuildings;
                    buildingsWithWorkers.Clear();
                    if ( buildingsForThisProfessionOrNull != null )
                    {
                        foreach ( SimBuilding building in buildingsForThisProfessionOrNull )
                        {
                            if ( building.Workers.TryGetValue( profession, out int workersHere ) )
                            {
                                if ( workersHere > 0 )
                                    buildingsWithWorkers.Add( building );
                            }
                        }
                    }

                    int outerLoopCount = 100; //only make 100 worker changes per iteration per faction
                    while ( extraWorkersThatDoNotExist > 0 && outerLoopCount-- > 0 && buildingsWithWorkers.Count > 0 )
                    {
                        int index = rand.Next( 0, buildingsWithWorkers.Count );
                        SimBuilding building = buildingsWithWorkers[index];
                        if ( building.Workers.TryGetValue( profession, out int workersHere ) )
                        {
                            if ( workersHere > 0 )
                            {
                                if ( extraWorkersThatDoNotExist > 10 )
                                {
                                    //if at least 10 workers here, then remove 5 at a time
                                    int workersToRemove = workersHere > 5 ? 5 : workersHere;
                                    workersHere = building.Workers.Add( profession, - workersToRemove );
                                    currentWorkers -= workersToRemove;
                                    extraWorkersThatDoNotExist -= workersToRemove;
                                }
                                else
                                {
                                    //otherwise remove 1 at a time
                                    workersHere = building.Workers.Add( profession, -1 );
                                    currentWorkers -= 1;
                                    extraWorkersThatDoNotExist -= 1;
                                }
                            }

                            //if we removed all the workers at this building, then stop considering this building for removals
                            if ( workersHere <= 0 )
                                buildingsWithWorkers.RemoveAt( index );
                        }
                    }
                    #endregion

                    //if ( startingExtra > extraWorkersThatDoNotExist )
                    //    ArcenDebugging.LogSingleLine( "Removed " + (startingExtra - extraWorkersThatDoNotExist ) + " extra workers from job "  + 
                    //        profession.DisplayName + " for faction " + fac.GetDisplayName(), Verbosity.DoNotShow );
                    //else
                    //    ArcenDebugging.LogSingleLine( "Thought we had " + extraWorkersThatDoNotExist + " extra workers for job " +
                    //        profession.DisplayName + " for faction " + fac.GetDisplayName() + " but could not remove any of them.", Verbosity.DoNotShow );
                }
                else
                {
                    //we are either perfectly balanced at the capacity we have,
                    //or we have people that we can assign to jobs.  If the latter, let's do that!

                    #region Staff Assignments
                    if ( currentWorkers < neededWorkers )
                    {
                        int startingCurrent = currentWorkers;

                        //in this case, we need more workers than we currently have

                        List<SimBuilding> buildingsForThisProfessionOrNull = TData.BuildingsByProfession.GetListForOrNull( profession );

                        //find the list of buildings actually needing workers of this profession
                        List<SimBuilding> buildingsNeedingWorkers = TData.WorkingBuildings;
                        buildingsNeedingWorkers.Clear();
                        if ( buildingsForThisProfessionOrNull != null )
                        {
                            foreach ( SimBuilding building in buildingsForThisProfessionOrNull )
                            {
                                Dictionary<ProfessionType, int> maxJobs = building.Prefab.NormalMaxJobsByProfession;

                                int maxJobsAtBuilding = 0; maxJobs.TryGetValue( profession, out maxJobsAtBuilding );
                                if ( maxJobsAtBuilding <= 0 )
                                    continue;

                                building.Workers.TryGetValue( profession, out int workersHere );
                                if ( workersHere < maxJobsAtBuilding )
                                    buildingsNeedingWorkers.Add( building );
                            }
                        }

                        if ( buildingsNeedingWorkers.Count <= 0 )
                        {
                            //ArcenDebugging.LogSingleLine( "Needed " + neededWorkers + " workers for job " +
                            //    profession.DisplayName + " for faction " + fac.GetDisplayName() + " but did not find any buildings for them!", Verbosity.DoNotShow );
                            continue; //guess we did not really have any that needed it somehow?
                        }

                        foreach ( EconomicClassType econClass in profession.CanBeWorkedBy )
                        {
                            int remainingUnassignedWorkers = TData.RemainingAvailableWorkers[econClass];
                            if ( remainingUnassignedWorkers <= 0 )
                                continue; //if nobody is here to assign, then don't try

                            //if we DO have some people to assign, then let's find some buildings to assign them to

                            int outerLoopCount = 50000; //only make 50k worker changes per iteration per faction per econ class per profession
                            while ( remainingUnassignedWorkers > 0 && outerLoopCount-- > 0 && buildingsNeedingWorkers.Count > 0 )
                            {
                                int index = rand.Next( 0, buildingsNeedingWorkers.Count );
                                SimBuilding building = buildingsNeedingWorkers[index];
                                Dictionary<ProfessionType, int> maxJobs = building.Prefab.NormalMaxJobsByProfession;

                                int maxJobsAtBuilding = 0; maxJobs.TryGetValue( profession, out maxJobsAtBuilding );
                                building.Workers.TryGetValue( profession, out int workersHere );
                                if ( maxJobsAtBuilding > 0 && maxJobsAtBuilding > workersHere )
                                {
                                    int neededWorkersAtThisBuilding = maxJobsAtBuilding - workersHere;
                                    if ( neededWorkersAtThisBuilding > 50 && remainingUnassignedWorkers > 100 )
                                    {
                                        //if need at least 40 workers here, and we have at least 100 spare workers then assign 40
                                        int workersToAdd = 40;
                                        workersHere = building.Workers.Add( profession, workersToAdd );
                                        currentWorkers += workersToAdd;
                                        remainingUnassignedWorkers -= workersToAdd;
                                    }
                                    else if ( neededWorkersAtThisBuilding > 20 && remainingUnassignedWorkers > 40 )
                                    {
                                        //if need at least 20 workers here, and we have at least 40 spare workers then assign 10
                                        int workersToAdd = 10;
                                        workersHere = building.Workers.Add( profession, workersToAdd );
                                        currentWorkers += workersToAdd;
                                        remainingUnassignedWorkers -= workersToAdd;
                                    }
                                    else if ( neededWorkersAtThisBuilding > 10 && remainingUnassignedWorkers > 20 )
                                    {
                                        //if need at least 10 workers here, and we have at least 20 spare workers then assign 5
                                        int workersToAdd = 5;
                                        workersHere = building.Workers.Add( profession, workersToAdd );
                                        currentWorkers += workersToAdd;
                                        remainingUnassignedWorkers -= workersToAdd;
                                    }
                                    else
                                    {
                                        //otherwise add 1 at a time
                                        workersHere = building.Workers.Add( profession, 1 );
                                        currentWorkers += 1;
                                        remainingUnassignedWorkers -= 1;
                                    }
                                }

                                //if we have fully staffed this building for this job type, then stop assigning workers
                                if ( workersHere >= maxJobsAtBuilding )
                                    buildingsNeedingWorkers.RemoveAt( index );
                            }

                            //remember this data, since it will be relevant for more professions that might pull from this same economic class
                            TData.RemainingAvailableWorkers[econClass] = remainingUnassignedWorkers;
                        }

                        //if ( currentWorkers > startingCurrent )
                        //    ArcenDebugging.LogSingleLine( "Added " + (currentWorkers - startingCurrent) + " new workers for job " +
                        //        profession.DisplayName + " for faction " + fac.GetDisplayName(), Verbosity.DoNotShow );
                        //else
                        //    ArcenDebugging.LogSingleLine( "Wanted " + (neededWorkers - currentWorkers) + " workers for job " +
                        //        profession.DisplayName + " for faction " + fac.GetDisplayName() + " but failed to add any!", Verbosity.DoNotShow );
                    }
                    else
                    {
                        //ArcenDebugging.LogSingleLine( "Needed " + neededWorkers + " workers for job " +
                        //    profession.DisplayName + " for faction " + fac.GetDisplayName() + " but already had " + currentWorkers, Verbosity.DoNotShow );
                    }
                    #endregion
                }

                TData.AlreadyWorkingProfDict[profession] = currentWorkers;
            }

            #region UnemployedResidents
            World_People.UnemployedResidents.ClearConstructionDictForStartingConstruction();
            int total_unemployedResidents = 0;
            foreach ( KeyValuePair<EconomicClassType, int> kv in TData.RemainingAvailableWorkers )
            {
                if ( kv.Value > 0 )
                {
                    World_People.UnemployedResidents.Construction[kv.Key] = kv.Value;
                    total_unemployedResidents += kv.Value;
                }
            }
            World_People.UnemployedResidents.SwitchConstructionToDisplay();
            World_People.UnemployedResidentCount = total_unemployedResidents;
            #endregion
        }
        #endregion

        #region class ThreadData
        private class ThreadData
        {
            public readonly MersenneTwister Random = new MersenneTwister( 0 );

            public readonly List<SimBuilding> WorkingBuildings =
                List<SimBuilding>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-WorkingBuildings" );

            public readonly Dictionary<EconomicClassType, int> CurrentResidents = 
                Dictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-CurrentResidents" );

            public readonly Dictionary<EconomicClassType, int> RemainingAvailableWorkers =
                Dictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-RemainingAvailbleWorkers" );

            public readonly Dictionary<EconomicClassType, int> CurrentJobsByEcon =
                Dictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-CurrentJobsByEcon" );

            public readonly Dictionary<ProfessionType, int> AlreadyWorkingProfDict =
                Dictionary<ProfessionType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-AlreadyWorkingProfDict" );
            public readonly Dictionary<ProfessionType, int> ExtraWorkersWhoDoNotExist =
                Dictionary<ProfessionType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-WorkerShortage" );
            public readonly Dictionary<ProfessionType, int> NeededJobsProfDict =
                Dictionary<ProfessionType, int>.Create_WillNeverBeGCed( 10, "Helper_AssignFactionWorkers-ThreadData-NeededJobsProfDict" );

            public readonly DictionaryOfLists<ProfessionType,SimBuilding> BuildingsByProfession =
                DictionaryOfLists<ProfessionType, SimBuilding>.Create_WillNeverBeGCed( 30, 5000, "Helper_AssignFactionWorkers-ThreadData-BuildingsByProfession" );

            public void Reset( int newRandSeed )
            {
                Random.ReinitializeWithSeed( newRandSeed );

                WorkingBuildings.Clear();

                CurrentResidents.Clear();
                RemainingAvailableWorkers.Clear();

                CurrentJobsByEcon.Clear();

                AlreadyWorkingProfDict.Clear();
                ExtraWorkersWhoDoNotExist.Clear();
                NeededJobsProfDict.Clear();

                BuildingsByProfession.Clear();
            }
        }
        #endregion
    }
}

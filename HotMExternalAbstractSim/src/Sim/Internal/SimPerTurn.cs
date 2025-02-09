using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;
using System.Text;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Helper methods for the per-tick game loop of the abstract sim
    /// </summary>
    internal static class SimPerTurn
    {
        /// <summary>
        /// These are "messages" from the background sim worker threads to the main sim thread.  The main sim thread should not log to this directly (it should just do the work),
        /// because the whole purpose of this is syncing from "background worker threads" to the main sim thread.  If you don't need that sync, then just do that work directly.
        /// </summary>
        internal static readonly ConcurrentQueue<IBGMessageToSimThread> MessagesToSimThread = ConcurrentQueue<IBGMessageToSimThread>.Create_WillNeverBeGCed( "SimPerTurn-MessagesToSimThread" );

        private static readonly Stopwatch TurnStopwatch = new Stopwatch();

        //
        //All important variables should be above this point, so we can be sure to clear them.
        //-----------------------------------------------------------------

        #region OnGameClear
        public static void OnGameClear()
        {
            MessagesToSimThread.Clear();

            TurnStopwatch.Stop();
            TurnStopwatch.Reset();
        }
        #endregion

        #region TryHandleNPCsShootingIfReadyToMoveToNextTurn
        public static bool TryHandleNPCsShootingIfReadyToMoveToNextTurn()
        {
            if ( SimCommon.NeedsToAttemptAnotherNPCTargetingPass )
            {
                if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count == 0 && SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count == 0 )
                    return false; //only wait on things if these are true
                //else
                //    ArcenDebugging.LogSingleLine( "skipped what would have been stuck!", Verbosity.ShowAsError );
            }
            if ( SimCommon.IsDoingATargetingPassNow || SimCommon.IsCurrentlyRunningSimTurn )
                return false; //if waiting on a targeting pass, don't start the below yet, as NPCs would shoot twice at things

            foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
            {
                NPCUnit npcUnit = kv.Value;
                if ( npcUnit.GetIsMovingRightNow() )
                    return false; //wait on all the npcs moving
            }

            SimCommon.NPCsWaitingToShoot_MainThreadOnly.ClearConstructionListForStartingConstruction();

            Int64 currentFiringPass = SimCommon.CurrentNPCFiringPassAuthorized;
            foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
            {
                NPCUnit npcUnit = kv.Value;
                NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                if ( npcUnit.IsFullDead || attackPlan.TargetForStartOfNextTurn == null || attackPlan.LastNPCFiringPassAttempted == currentFiringPass ||
                    npcUnit.HasAttackedYetThisTurn )
                    continue; //if the npc is dead or already fired, don't add it to the list of units that want to do something
                SimCommon.NPCsWaitingToShoot_MainThreadOnly.AddToConstructionList( npcUnit ); //otherwise DO add it
            }

            SimCommon.NPCsWaitingToShoot_MainThreadOnly.SwitchConstructionToDisplay();

            if ( SimCommon.NPCsWaitingToShoot_MainThreadOnly.Count == 0 ) //if this is true, we're done, with this part at least!
            {
                //but before we get working on calculations, make sure all the bullets have stopped flying
                //if ( Engine_HotM.NonSim_TravelingParticleEffectsPlayingNow > 0 )
                //    return false; //EXPERIMENT: skipping this, and it seems okay.  I think this flows more fluidly
                return true; //we must be done for real!
            }
            else //we still have units that have not fired yet, so keep delaying the next turn
                return false;
        }
        #endregion

        #region DrawDownNPCWaitingToShootList
        public static void DrawDownNPCWaitingToShootList()
        {
            SimCommon.NPCsWaitingToShoot_MainThreadOnly.ClearConstructionListForStartingConstruction();

            Int64 currentFiringPass = SimCommon.CurrentNPCFiringPassAuthorized;
            foreach ( ISimNPCUnit npcUnit in SimCommon.NPCsWaitingToShoot_MainThreadOnly.GetDisplayList() )
            {
                NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                if ( npcUnit.IsFullDead || attackPlan.TargetForStartOfNextTurn == null || attackPlan.LastNPCFiringPassAttempted == currentFiringPass )
                    continue; //if the npc is dead or already fired, don't add it back to the list
                //otherwise DO add it back to the list, since it's being time-sliced.
                SimCommon.NPCsWaitingToShoot_MainThreadOnly.AddToConstructionList( npcUnit );
            }

            SimCommon.NPCsWaitingToShoot_MainThreadOnly.SwitchConstructionToDisplay();
        }
        #endregion

        #region TryRunNextTurn
        public static void TryRunNextTurn()
        {
            if ( SimCommon.IsCurrentlyRunningSimTurn )
                return;

            int randSeed = Engine_Universal.PermanentQualityRandom.Next();
            ArcenThreading.RunTaskOnBackgroundThread( "_Turn.RunNext",
                ( TaskStartData startData ) =>
                {
                    SimCommon.IsCurrentlyRunningSimTurn = true;
                    TurnStopwatch.Restart();

                    //log this every time, since the ID is different every time.
                    Engine_HotM.CurrentTurnLogicThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

                    //we have to use a random seed from PermanentQualityRandom in order to not have stale randoms being initialized in here.
                    //there needs to be an initial randomness to the start of our random, which is what this accomplishes
                    //we don't care WHAT the random seed is, just that it's not repetitive
                    MersenneTwister RandForThisTurn = new MersenneTwister( randSeed );
                    try
                    {
                        if ( SimCommon.Turn == 0 )
                        {
                            //first run some accelerated-time turns
                            for ( int i = 0; i < 5; i++ )
                                ActuallyRunSingleTurn_OnBackgroundThread( RandForThisTurn, true );

                            //then run a regular one
                            ActuallyRunSingleTurn_OnBackgroundThread( RandForThisTurn, false );
                        }
                        else
                        {
                            ActuallyRunSingleTurn_OnBackgroundThread( RandForThisTurn, false );
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogWithStack( "SimRoot.TryRunNextTurn background thread error: " + e, Verbosity.ShowAsError );
                    }

                    TurnStopwatch.Stop();
                    SimTimingInfo.SimTurn.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks );

                    SimPerQuarterSecond.RecalculateActiveActorsAndOtherCalculations( true, RandForThisTurn ); //this is critical to happen before FinishChangingToNextTurn!
                    SimCommon.FinishChangingToNextTurn();
                } );
        }
        #endregion

        #region ActuallyRunSingleTurn_OnBackgroundThread

        private static List<KeyValuePair<MachineStructure, float>> machineStructuresByJobPriority = List<KeyValuePair<MachineStructure, float>>.Create_WillNeverBeGCed( 1000, "SimPerTurn-machineStructuresByJobPriority" );

        internal static void ActuallyRunSingleTurn_OnBackgroundThread( MersenneTwister RandForThisTurn, bool RunAbbreviatedVersionForGameStart )
        {
            int ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

            if ( !RunAbbreviatedVersionForGameStart )
                SimCommon.DoPerTurn_Early( RandForThisTurn );

            //do this really early, since the player made these requests based on at least one tick old data
            HandleUIRequests();

            ExampleMessageSender.DoPerTick();

            bool performanceDebug = GameSettings.Current.GetBool("Debug_MainGameMonitorPerformance");
            bool debug = GameSettings.Current.GetBool( "Debug_MainGameSimPerTurnDebug" );

            SimTimingInfo.SimTurnEarly.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );

            //Update the buildings. This includes updating things used for the Diffusion/Metric systems
            World_Buildings.DoAnyPerTurnLogic_SimThread( RandForThisTurn, RunAbbreviatedVersionForGameStart, TurnStopwatch );
            if ( debug )
                ArcenDebugging.LogSingleLine( "SimPerTurn: UpdateSimBuildings completed ", Verbosity.DoNotShow );                       

            ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

            if ( !RunAbbreviatedVersionForGameStart )
            {
                //before units and such do anything, clear any blocking collidables
                foreach ( MapCell cell in CityMap.Cells )
                {
                    cell.BlockingCollidablesUntilNextTurn.Clear();
                }

                //first do the early construction on machine structures, and get the jobs by work order priority in a list
                machineStructuresByJobPriority.Clear();
                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                {
                    kv.Value.HandleStructurePerTurn_EarlyConstruction_SimThread( RandForThisTurn );
                    if ( kv.Value.CurrentJob != null && kv.Value.IsFunctionalJob && !kv.Value.IsJobStillInstalling ) //&& kv.Value.DoesJobHaveEnoughElectricity ) do this part even if not enough electricity.  It has things to calculate in there
                    {
                        machineStructuresByJobPriority.Add( new KeyValuePair<MachineStructure, float>( kv.Value, kv.Value.CurrentJob.JobWorkOrderPriority ) );
                    }
                    else
                        kv.Value.DoesJobHaveAnyInputShortage = true;
                }

                //do this after job installation as well
                foreach ( MachineNetworkPerTurnEvaluator evaluator in MachineNetworkPerTurnEvaluatorTable.Instance.Rows )
                    evaluator.Implementation.HandleNetworksPerTurn_Early( RandForThisTurn );

                if ( !RunAbbreviatedVersionForGameStart )
                    SimCommon.DoPerTurn_AfterJobInstallation( RandForThisTurn );

                //then do any central network calculations per turn
                SimCommon.TheNetwork?.HandleNetworkPerTurn_SimThread( RandForThisTurn );

                //then sort the machine structures by job work priority
                if ( machineStructuresByJobPriority.Count > 1 )
                    machineStructuresByJobPriority.Sort( delegate ( KeyValuePair<MachineStructure, float> Left, KeyValuePair<MachineStructure, float> Right )
                    {
                        int val = Left.Value.CompareTo( Right.Value ); //ascending
                        if ( val != 0 )
                            return val;
                        return Left.Key.SortID.CompareTo( Right.Key.SortID ); //ascending
                    } );

                //pre-work logic on jobs as a whole
                foreach ( MachineJob job in MachineJobTable.Instance.Rows )
                    job.DoPerTurn_PreStructureLogic();
                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.DoPerTurn_PreStructureLogic();

                foreach ( MachineSubnet subnet in SimCommon.Subnets )
                {
                    subnet.JobClassInputEfficiencyMultipliers.ClearConstructionDictForStartingConstruction();
                    subnet.JobClassProductionMultipliers.ClearConstructionDictForStartingConstruction();
                }

                //and then execute the job early-pass work on each individual structure
                foreach ( KeyValuePair<MachineStructure, float> kv in machineStructuresByJobPriority )
                    kv.Key.HandleStructurePerTurn_LaterEarlyPassJobActions_SimThread();

                foreach ( MachineSubnet subnet in SimCommon.Subnets )
                {
                    subnet.JobClassInputEfficiencyMultipliers.SwitchConstructionToDisplay();
                    subnet.JobClassProductionMultipliers.SwitchConstructionToDisplay();
                }

                //pre-finalization of the job logic
                //if not before HandleStructurePerTurn_LaterMainPassJobActions_SimThread, then some of those can't calculate right
                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.DoPerTurn_PreJobLogicFinalized();

                //and then execute the job main-pass work on each individual structure
                foreach ( KeyValuePair<MachineStructure,float> kv in machineStructuresByJobPriority )
                    kv.Key.HandleStructurePerTurn_LaterMainPassJobActions_SimThread( RandForThisTurn );

                //post-work logic on jobs as a whole
                SimCommon.ProductionJobsWithoutIssues.ClearConstructionListForStartingConstruction();
                SimCommon.ProductionJobsWithProblems.ClearConstructionListForStartingConstruction();
                foreach ( MachineJob job in MachineJobTable.Instance.Rows )
                    job.DoPerTurn_PostStructureLogic();
                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.DoPerTurn_PostStructureLogic();

                //and the early data calculator logic, too
                foreach ( DataCalculator calculator in DataCalculatorTable.DoPerTurnEarly )
                    calculator.Implementation.DoPerTurn_Early( calculator, RandForThisTurn );

                foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
                {
                    project.Implementation.HandleLogicForProjectOutcome( ProjectLogic.DoAnyPerTurnEarlyLogicWhileProjectActive, null, project, project.DuringGame_IntendedOutcome, RandForThisTurn, out _ );
                }

                SimCommon.SortTPSReportEntries();

                SimCommon.ProductionJobsWithoutIssues.SwitchConstructionToDisplay();
                SimCommon.ProductionJobsWithProblems.SwitchConstructionToDisplay();

                //now before units but after jobs, do the poi countdowns
                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                    kv.Value.HandlePOIPerTurn();

                //now do the deal logic
                foreach ( NPCDeal deal in SimCommon.ActiveDeals.GetDisplayList() )
                    deal.DoDealPerTurnLogic( RandForThisTurn );

                //post-finalization of the job logic
                SimCommon.ProductionResourcesWithoutIssues.ClearConstructionListForStartingConstruction();
                SimCommon.ProductionResourcesWithMinorProblems.ClearConstructionListForStartingConstruction();
                SimCommon.ProductionResourcesWithMajorProblems.ClearConstructionListForStartingConstruction();

                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.DoPerTurn_PostJobLogicFinalized();


                SimTimingInfo.SimTurnEarlyMid.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
                ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    NPCUnit unit = kv.Value;
                    unit.DoEarlyPerTurnClears( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    MachineVehicle vehicle = kv.Value;
                    vehicle.DoEarlyPerTurnClears( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    MachineUnit unit = kv.Value;
                    unit.DoEarlyPerTurnClears( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                {
                    MachineStructure structure = kv.Value;
                    structure.DoEarlyPerTurnClears( RandForThisTurn );
                    structure.IsBlockedFromAutomatedRebuildingUntilAnotherTurn = false; //at this point, repair spiders and similar already did what they could
                }

                if ( InputCaching.Debug_DoDumpEveryTurn )
                {
                    foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                    {
                        NPCUnit unit = kv.Value;
                        unit.TurnDumpLines.Clear();
                    }
                }

                SimTimingInfo.SimTurnUnitPrep.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
                ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

                NPCTimingData timingDataOrNull = null;

                if ( InputCaching.Debug_LogExtraTurnTimingsInfo )
                {
                    timingDataOrNull = new NPCTimingData(); //let this get GC'd, that's ok
                    timingDataOrNull.sw = TurnStopwatch;
                }

                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    NPCUnit unit = kv.Value;
                    unit.HandleNPCUnitPerTurnFairlyEarly( RandForThisTurn, timingDataOrNull );
                }
                
                SimTimingInfo.SimTurnNPCTotal.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
                ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

                SimTimingInfo.SimTurnNPCUnitsMain.LogCurrentTicks( (int)((timingDataOrNull?.EarlyTicks??0) ) );
                
                SimTimingInfo.SimTurnNPCUnitsStanceOuter.LogCurrentTicks( (int)((timingDataOrNull?.StanceTicks ?? 0)) );
                SimTimingInfo.SimTurnNPCUnitsStanceCount.LogCurrentTicks( timingDataOrNull?.StanceLogicCount??0 );
                SimTimingInfo.SimTurnNPCUnitsActionPlanningOuter.LogCurrentTicks( (int)((timingDataOrNull?.ActionPlanningTicks ?? 0)) );
                SimTimingInfo.SimTurnNPCUnitsActionPlanningCount.LogCurrentTicks( timingDataOrNull?.ActionPlanningCount??0 );

                SimTimingInfo.SimTurn_Wander.LogCurrentTicks( (int)((timingDataOrNull?.WanderTicks ?? 0)) );
                SimTimingInfo.SimTurn_ReturnToHomePOI.LogCurrentTicks( (int)((timingDataOrNull?.ReturnToHomePOITicks ?? 0)) );
                SimTimingInfo.SimTurn_ReturnToHomeDistrict.LogCurrentTicks( (int)((timingDataOrNull?.ReturnToHomeDistrictTicks ?? 0)) );
                SimTimingInfo.SimTurn_RunAfterOutcastThenConspicuous.LogCurrentTicks( (int)((timingDataOrNull?.RunAfterOutcastThenConspicuousTicks ?? 0)) );
                SimTimingInfo.SimTurn_RunAfterMachineStructures.LogCurrentTicks( (int)((timingDataOrNull?.RunAfterMachineStructuresTicks ?? 0)) );
                SimTimingInfo.SimTurn_RunAfterAggroedUnits.LogCurrentTicks( (int)((timingDataOrNull?.RunAfterAggroedUnitsTicks ?? 0)) );
                SimTimingInfo.SimTurn_RunAfterNearestActorThatICanAutoShoot.LogCurrentTicks( (int)((timingDataOrNull?.RunAfterNearestActorThatICanAutoShootTicks ?? 0)) );
                SimTimingInfo.SimTurn_FollowEvenHiddenPlayerUnits.LogCurrentTicks( (int)((timingDataOrNull?.FollowEvenHiddenPlayerUnits ?? 0)) );
                SimTimingInfo.SimTurn_RunAfterThreatsNearMission.LogCurrentTicks( (int)((timingDataOrNull?.RunAfterThreatsNearMissionTicks ?? 0)) );
                SimTimingInfo.SimTurn_ReturnToManagerStartArea.LogCurrentTicks( (int)((timingDataOrNull?.ReturnToManagerStartAreaTicks ?? 0)) );
                SimTimingInfo.SimTurn_ReturnToMissionArea.LogCurrentTicks( (int)((timingDataOrNull?.ReturnToMissionAreaTicks ?? 0)) );

                SimTimingInfo.SimTurn_FindNearestAggroedUnit.LogCurrentTicks( (int)((timingDataOrNull?.FindNearestAggroedUnitTicks ?? 0)) );
                SimTimingInfo.SimTurn_ChaseNearestAggroedUnit.LogCurrentTicks( (int)((timingDataOrNull?.ChaseNearestAggroedUnitTicks ?? 0)) );

                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    MachineVehicle vehicle = kv.Value;
                    vehicle.HandleVehiclePerTurnFairlyEarly( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    MachineUnit unit = kv.Value;
                    unit.HandleUnitPerTurnFairlyEarly( RandForThisTurn );
                }

                if ( InputCaching.Debug_DoDumpEveryTurn )
                {
                    ThrowawayList<NPCUnit> units = new ThrowawayList<NPCUnit>( World_Forces.AllNPCUnitsByID.Count );
                    foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                    {
                        NPCUnit unit = kv.Value;
                        if ( unit.TurnDumpLines.Count > 0 )
                            units.Add( unit );
                    }

                    units.Sort( delegate ( NPCUnit Left, NPCUnit Right )
                    {
                        return Left.ActorID.CompareTo( Right.ActorID );
                    } );

                    StringBuilder text = new StringBuilder();
                    foreach ( NPCUnit unit in units )
                    {
                        text.Append( "--------------------------------\n" );
                        text.Append( unit.ActorID ).Append( ": " ).Append( unit.UnitType.ID ).Append( "\n" );

                        List<string> lines = unit.TurnDumpLines;
                        foreach ( string line in lines )
                        {
                            text.Append( line ).Append( "\n" );
                        }

                        lines.Clear();
                    }

                    string dumpFolder = Engine_Universal.CurrentPlayerDataDirectory + "Dumps/Turns/";
                    if ( !ArcenIO.DirectoryExists( dumpFolder ) )
                        ArcenIO.CreateDirectory( dumpFolder );

                    string dumpFile = dumpFolder + "Turn-" + SimCommon.Turn + "--" + DateTime.Now.Year + "-" + DateTime.Now.Month +
                        "-" + DateTime.Now.Day + "--" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt";
                    ArcenIO.WriteStringBuilderToDisk( dumpFile, text );
                    text.Clear();
                }

                SimTimingInfo.SimTurnUnitMaintenance.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
                ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.Implementation.DoPerTurn( resource,  RandForThisTurn );

                foreach ( MachineNetworkPerTurnEvaluator evaluator in MachineNetworkPerTurnEvaluatorTable.Instance.Rows )
                    evaluator.Implementation.HandleNetworksPerTurn_Late( RandForThisTurn );

                foreach ( DataCalculator calculator in DataCalculatorTable.DoPerTurnLate )
                    calculator.Implementation.DoPerTurn_Late( calculator, RandForThisTurn );

                foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
                    resource.DoPerTurn_PostLateLogicLogicFinalized();

                SimCommon.SortResourceLedgerEntries();

                SimCommon.ProductionResourcesWithoutIssues.SwitchConstructionToDisplay();
                SimCommon.ProductionResourcesWithMinorProblems.SwitchConstructionToDisplay();
                SimCommon.ProductionResourcesWithMajorProblems.SwitchConstructionToDisplay();

                foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
                {
                    project.Implementation.HandleLogicForProjectOutcome( ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive, null, project, project.DuringGame_IntendedOutcome, RandForThisTurn, out _ );
                }

                foreach ( OtherCountdownType countdown in OtherCountdownTypeTable.Instance.Rows )
                {
                    if ( countdown.DuringGameplay_TurnsRemaining > 0 )
                        countdown.DoPerTurnLogicIfActive();
                }
                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    MachineVehicle vehicle = kv.Value;
                    vehicle.HandleVehiclePerTurnVeryLate( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    MachineUnit unit = kv.Value;
                    unit.HandleUnitPerTurnVeryLate( RandForThisTurn );
                }

                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    NPCUnit unit = kv.Value;
                    unit.HandleNPCUnitPerTurnVeryLate( RandForThisTurn, timingDataOrNull );
                }
            }
            else
            {
                SimTimingInfo.SimTurnEarlyMid.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );

                SimTimingInfo.SimTurnUnitPrep.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnNPCUnitsMain.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnNPCTotal.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnUnitMaintenance.LogCurrentTicks( 0 );

                SimTimingInfo.SimTurnNPCUnitsStanceOuter.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnNPCUnitsStanceCount.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnNPCUnitsActionPlanningOuter.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurnNPCUnitsActionPlanningCount.LogCurrentTicks( 0 );

                SimTimingInfo.SimTurn_Wander.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_ReturnToHomePOI.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_ReturnToHomeDistrict.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_RunAfterOutcastThenConspicuous.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_RunAfterMachineStructures.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_RunAfterAggroedUnits.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_RunAfterNearestActorThatICanAutoShoot.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_FollowEvenHiddenPlayerUnits.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_RunAfterThreatsNearMission.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_ReturnToManagerStartArea.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_ReturnToMissionArea.LogCurrentTicks( 0 );

                SimTimingInfo.SimTurn_FindNearestAggroedUnit.LogCurrentTicks( 0 );
                SimTimingInfo.SimTurn_ChaseNearestAggroedUnit.LogCurrentTicks( 0 );
            }

            SimTimingInfo.SimTurnLateMid.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
            ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

            if ( !RunAbbreviatedVersionForGameStart )
                ActivityScheduler.CalculateAllActivityPerTurn_OnBackgroundThread( RandForThisTurn );

            SimTimingInfo.SimTurnProjects.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
            ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

            //process any messages that were generated for us.  This is from the parallel threads communicating to us
            //things they wanted to happen in a threadsafe manner.
            while ( MessagesToSimThread.TryDequeue( out IBGMessageToSimThread message ) )
            {
                if ( message != null )
                    message();
            }

            if ( !RunAbbreviatedVersionForGameStart )
            {
                SimCommon.DoPerTurn_Late( RandForThisTurn );
                {
                    //List<NPCCohort> cohortList = NPCCohortTable.ActiveCohorts.GetDisplayList();

                    //foreach ( NPCCohort cohort in cohortList )
                    //{
                    //    if ( cohort.DuringGame_HasBeenDisbanded )
                    //        continue;
                    //    cohort.ParentType.Implementation.DoPerTurnLogic_MainSimThread( cohort, RandForThisTurn );
                    //}
                }
            }

            SimTimingInfo.SimTurnLate.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
        }
        #endregion

        #region HandleUIRequests
        private static void HandleUIRequests()
        {
            if ( World_Misc.UIRequests.Count <= 0 )
                return;

            while ( World_Misc.UIRequests.TryDequeue( out IUIRequest request ) )
            {
                if ( request is IUIRequestToBuilding buildingRequest )
                {
                    ( buildingRequest.BuildingToSendTo as SimBuilding ).HandleUIRequest( buildingRequest );
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "Unknown IUIRequest type '" + request.GetType().Name + "' in HandleUIRequests.", Verbosity.ShowAsError );
                }
            }
        }
        #endregion     
    }

    internal delegate void IBGMessageToSimThread();
}

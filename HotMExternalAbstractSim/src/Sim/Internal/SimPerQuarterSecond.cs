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
    internal static class SimPerQuarterSecond
    {
        private static readonly Stopwatch SimQuarterSecondStopwatch = new Stopwatch();

        //
        //All important variables should be above this point, so we can be sure to clear them.
        //-----------------------------------------------------------------

        #region OnGameClear
        public static void OnGameClear()
        {
            SimQuarterSecondStopwatch.Stop();
            SimQuarterSecondStopwatch.Reset();
        }
        #endregion

        public static void DoPerQuarterSecondLogic_BackgroundThread( MersenneTwister Rand )
        {
            //SimQuarterSecondStopwatch.Restart();

            DoPerQuarterSecondLogic_Inner( Rand );

            //SimQuarterSecondStopwatch.Stop();
        }

        private static void DoPerQuarterSecondLogic_Inner( MersenneTwister Rand )
        {
            if ( !SimCommon.HasFullyStarted )
                return; //prevent errors when the game is still starting up

            World_Forces.DoPerQuarterSecondLogic();
            RecalculateActiveActorsAndOtherCalculations( false, Rand );

            foreach ( MachineNetworkPerTurnEvaluator evaluator in MachineNetworkPerTurnEvaluatorTable.Instance.Rows )
                evaluator.Implementation.HandleNetworksPerQuarterSecond( Rand );

            foreach ( OtherKeyMessage message in OtherKeyMessageTable.MessagesWithCode )
            {
                if ( !message.DuringGameplay_IsReadyToBeViewed )
                    message.ImplementationOrNull.DoPerQuarterSecond_WhenNotYetReadyToBeViewed( message, Rand );
            }

            foreach ( OtherChecklistAlert alert in OtherChecklistAlertTable.Instance.Rows )
                alert.Implementation.HandleOtherAlertLogic( alert, null, OtherChecklistAlertLogic.DoPerQuarterSecond, false, null, SideClamp.Any, TooltipExtraRules.None );

            World_Buildings.DoAnyPerQuarterSecondLogic_SimThread( Rand );
        }

        public static void HandleRecalculationsFromAfterDeserialization()
        {
            MersenneTwister randAfterDeser = new MersenneTwister( Engine_Universal.PermanentQualityRandom.Next() );
            SimCommon.RecalculateActiveProjectsAndActivityLoci( randAfterDeser );
            RecalculateActiveActorsAndOtherCalculations( true, randAfterDeser );
        }

        #region RecalculateActiveActors
        private static int ActorRecalculationIsHappeningNow = 0;
        public static void RecalculateActiveActorsAndOtherCalculations( bool ForceRun, MersenneTwister Rand )
        {
            if ( ActorRecalculationIsHappeningNow == 1 ) 
                return;
            if ( Interlocked.Exchange( ref ActorRecalculationIsHappeningNow, 1 ) != 0 ) //this gets called from the per-quarter-second thread and the sim thread, so we want to block one from entering if the other is already here
                return;

            try
            {
                SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.ClearConstructionListForStartingConstruction();
                SimCommon.MachineActorsDeployedAndActingAsMechLauncher.ClearConstructionListForStartingConstruction();
                SimCommon.AllMachineActors.ClearConstructionListForStartingConstruction();
                SimCommon.MachineActorsInvestigatingOrInfiltrating.ClearConstructionListForStartingConstruction();

                if ( !ForceRun )
                {
                    if ( SimCommon.IsCurrentlyRunningSimTurn || SimCommon.IsReadyToRunNextLogicForTurn > SimCommon.Turn )
                    {
                        SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.SwitchConstructionToDisplay();
                        SimCommon.MachineActorsDeployedAndActingAsMechLauncher.SwitchConstructionToDisplay();
                        SimCommon.AllMachineActors.SwitchConstructionToDisplay();
                        return; //blocked from doing this, as we'll have strange partial results in our list otherwise
                    }
                }

                //first machine vehicles
                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    if ( kv.Value != null && !kv.Value.IsFullDead ) //if dead, also don't include us for now
                    {
                        SimCommon.AllMachineActors.AddToConstructionList( kv.Value );

                        if ( kv.Value.VehicleType.CountsAsAnAndroidLauncher )
                            SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.AddToConstructionList( kv.Value );
                        if ( kv.Value.VehicleType.CountsAsAMechLauncher )
                            SimCommon.MachineActorsDeployedAndActingAsMechLauncher.AddToConstructionList( kv.Value );
                    }
                }

                //then any units that are not dead
                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    if ( kv.Value != null && !kv.Value.IsFullDead ) //if dead, also don't include us for now
                    {
                        SimCommon.AllMachineActors.AddToConstructionList( kv.Value );

                        if ( kv.Value.UnitType.CountsAsAnAndroidLauncher && kv.Value.GetIsDeployed() )
                            SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.AddToConstructionList( kv.Value );
                        if ( kv.Value.UnitType.CountsAsAMechLauncher && kv.Value.GetIsDeployed() )
                            SimCommon.MachineActorsDeployedAndActingAsMechLauncher.AddToConstructionList( kv.Value );

                        switch ( kv.Value.CurrentActionOverTime?.Type?.ID ?? string.Empty )
                        {
                            case "InvestigateLocation":
                            case "InvestigateLocation_Infiltration":
                                {
                                    int turnsLeft = kv.Value?.CurrentActionOverTime?.GetIntData( "Target1", false )??0;
                                    if ( turnsLeft > 0 )
                                        SimCommon.MachineActorsInvestigatingOrInfiltrating.AddToConstructionList( new KeyValuePair<ISimMachineActor, int>( kv.Value, turnsLeft ) );
                                }
                                break;
                        }
                    }
                }

                SimCommon.MachineActorsDeployedAndActingAsAndroidLauncher.SwitchConstructionToDisplay();
                SimCommon.MachineActorsDeployedAndActingAsMechLauncher.SwitchConstructionToDisplay();
                SimCommon.AllMachineActors.SwitchConstructionToDisplay();
                SimCommon.MachineActorsInvestigatingOrInfiltrating.SwitchConstructionToDisplay();

                //now down here calculate the npcs waiting to act

                SimCommon.NPCsWaitingToActOnTheirOwn.ClearConstructionListForStartingConstruction();
                SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.ClearConstructionListForStartingConstruction();
                SimCommon.AllPlayerRelatedNPCUnits.ClearConstructionListForStartingConstruction();
                SimCommon.NPCHackersThreateningPlayerInfiltrators.ClearConstructionListForStartingConstruction();
                SimCommon.NPCHackersThreateningPlayerSecrets.ClearConstructionListForStartingConstruction();

                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID)
                {
                    MapPOI poi = kv.Value;
                    poi.GuardTagCount1.ClearConstructionValueForStartingConstruction();
                    poi.GuardTagCount2.ClearConstructionValueForStartingConstruction();
                    poi.GuardTagCount3.ClearConstructionValueForStartingConstruction();
                }


                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    NPCUnit npc = kv.Value;
                    NPCUnitType npcType = npc?.UnitType;
                    if ( npc != null && !npc.IsFullDead && !npc.IsDowned && npcType != null )
                    {
                        if ( npc.GetIsPartOfPlayerForcesInAnyWay() )
                            SimCommon.AllPlayerRelatedNPCUnits.AddToConstructionList( npc );
                        else
                        {
                            NPCUnitStance stance = npc.Stance;
                            if ( stance != null )
                            {
                                if ( stance.IsConsideredEnemyHackerThatThreatensInfiltrators )
                                    SimCommon.NPCHackersThreateningPlayerInfiltrators.AddToConstructionList( npc );
                                if ( stance.IsConsideredEnemyHackerThatStealsSecrets )
                                    SimCommon.NPCHackersThreateningPlayerSecrets.AddToConstructionList( npc );
                            }
                        }

                        if ( npc.GetHasAnyActionItWantsTodo() && !npc.IsFullDead && npc.GetIsNPCActionStillValid() )
                        {
                            if ( npc.GetShouldActionsBeWatchedByPlayer() )
                                SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.AddToConstructionList( npc );
                            else
                                SimCommon.NPCsWaitingToActOnTheirOwn.AddToConstructionList( npc );
                        }

                        MapPOI homePOI = npc.HomePOI;
                        if ( homePOI != null )
                        {
                            //if ( homePOI.HasBeenDestroyed )
                            //    npc.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                            //else
                            {
                                //do this with if and else-if to make sure that if there is overlap on tags, which is fine, that we don't double-count a given guard tag count
                                if ( homePOI.Type.POICheckGuard1 != null && npcType.Tags.ContainsKey( homePOI.Type.POICheckGuard1.ID ) )
                                    homePOI.GuardTagCount1.Construction++;
                                else if ( homePOI.Type.POICheckGuard2 != null && npcType.Tags.ContainsKey( homePOI.Type.POICheckGuard2.ID ) )
                                    homePOI.GuardTagCount2.Construction++;
                                else if ( homePOI.Type.POICheckGuard3 != null && npcType.Tags.ContainsKey( homePOI.Type.POICheckGuard3.ID ) )
                                    homePOI.GuardTagCount3.Construction++;
                            }
                        }
                    }
                }
                SimCommon.NPCsWaitingToActOnTheirOwn.SwitchConstructionToDisplay();
                SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.SwitchConstructionToDisplay();
                SimCommon.AllPlayerRelatedNPCUnits.SwitchConstructionToDisplay();
                SimCommon.NPCHackersThreateningPlayerInfiltrators.SwitchConstructionToDisplay();
                SimCommon.NPCHackersThreateningPlayerSecrets.SwitchConstructionToDisplay();

                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                {
                    MapPOI poi = kv.Value;
                    poi.GuardTagCount1.SwitchConstructionToDisplay();
                    poi.GuardTagCount2.SwitchConstructionToDisplay();
                    poi.GuardTagCount3.SwitchConstructionToDisplay();
                    poi.HasEverCountedGuardTags = true;

                    int normalMinGuards = 0;
                    int normalMaxGuards = 0;

                    if ( poi.Type.POICheckGuard1 != null )
                    {
                        if ( poi.GuardTagCount1.Display < poi.Type.RangeOfInitialPOIGuards1.Min )
                            poi.ShortGuards1 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards1.Min - poi.GuardTagCount1.Display);
                        if ( poi.GuardTagCount1.Display < poi.Type.RangeOfInitialPOIGuards1.Max )
                            poi.SpaceForGuards1 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards1.Max - poi.GuardTagCount1.Display);

                        if ( poi.GuardTagCount1.Display > poi.Type.RangeOfInitialPOIGuards1.Max )
                            DestroyXGuardsOfPOIWithTag( poi, poi.Type.POICheckGuard1, poi.GuardTagCount1.Display - poi.Type.RangeOfInitialPOIGuards1.Max );

                        if ( poi.Type.RangeOfInitialPOIGuards1.Min > 0 )
                            normalMinGuards += poi.Type.RangeOfInitialPOIGuards1.Min;
                        if ( poi.Type.RangeOfInitialPOIGuards1.Max > 0 )
                            normalMaxGuards += poi.Type.RangeOfInitialPOIGuards1.Max;
                    }
                    if ( poi.Type.POICheckGuard2 != null && poi.GuardTagCount2.Display < poi.Type.RangeOfInitialPOIGuards2.Min )
                    {
                        if ( poi.GuardTagCount2.Display < poi.Type.RangeOfInitialPOIGuards2.Min )
                            poi.ShortGuards2 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards2.Min - poi.GuardTagCount2.Display);
                        if ( poi.GuardTagCount2.Display < poi.Type.RangeOfInitialPOIGuards2.Max )
                            poi.SpaceForGuards2 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards2.Max - poi.GuardTagCount2.Display);

                        if ( poi.GuardTagCount2.Display > poi.Type.RangeOfInitialPOIGuards2.Max )
                            DestroyXGuardsOfPOIWithTag( poi, poi.Type.POICheckGuard2, poi.GuardTagCount2.Display - poi.Type.RangeOfInitialPOIGuards2.Max );

                        if ( poi.Type.RangeOfInitialPOIGuards2.Min > 0 )
                            normalMinGuards += poi.Type.RangeOfInitialPOIGuards2.Min;
                        if ( poi.Type.RangeOfInitialPOIGuards2.Max > 0 )
                            normalMaxGuards += poi.Type.RangeOfInitialPOIGuards2.Max;
                    }
                    if ( poi.Type.POICheckGuard3 != null && poi.GuardTagCount3.Display < poi.Type.RangeOfInitialPOIGuards3.Min )
                    {
                        if ( poi.GuardTagCount3.Display < poi.Type.RangeOfInitialPOIGuards3.Min )
                            poi.ShortGuards3 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards3.Min - poi.GuardTagCount3.Display);
                        if ( poi.GuardTagCount3.Display < poi.Type.RangeOfInitialPOIGuards3.Max )
                            poi.SpaceForGuards3 = MathA.Max( 0, poi.Type.RangeOfInitialPOIGuards3.Max - poi.GuardTagCount3.Display);

                        if ( poi.GuardTagCount3.Display > poi.Type.RangeOfInitialPOIGuards3.Max )
                            DestroyXGuardsOfPOIWithTag( poi, poi.Type.POICheckGuard3, poi.GuardTagCount3.Display - poi.Type.RangeOfInitialPOIGuards3.Max );

                        if ( poi.Type.RangeOfInitialPOIGuards3.Min > 0 )
                            normalMinGuards += poi.Type.RangeOfInitialPOIGuards3.Min;
                        if ( poi.Type.RangeOfInitialPOIGuards3.Max > 0 )
                            normalMaxGuards += poi.Type.RangeOfInitialPOIGuards3.Max;
                    }

                    poi.CurrentlyShortThisManyGuards = poi.ShortGuards1 + poi.ShortGuards2 + poi.ShortGuards3;
                    poi.CouldHaveThisManyMoreGuardsAtMost = poi.SpaceForGuards1 + poi.SpaceForGuards2 + poi.SpaceForGuards3;
                    poi.NormalMinGuardCount = normalMinGuards;
                    poi.NormalMaxGuardCount = normalMaxGuards;
                }

                //and also any machine structures needing attention
                //except this one also is going to set various tallies

                SimCommon.StructuresWithVisibleComplaints.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithLowerGradeComplaints.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithDamage.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithAnyFormOfConstruction.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresNeedingAttention.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithAnyFormOfIssue.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithNoIssues.ClearConstructionListForStartingConstruction();
                SimCommon.TerritoryControlFlags.ClearConstructionListForStartingConstruction();
                int priorFlagCount = SimCommon.TerritoryControlFlags.Count;

                bool hasEnoughElectricity = (SimCommon.TheNetwork?.GetNetworkDataAmountConsumed( ActorRefs.GeneratedElectricity )??0) <=
                    (SimCommon.TheNetwork?.GetNetworkDataAmountProvided( ActorRefs.GeneratedElectricity ) ?? 0);

                MachineJobComplaint elecComplaint1 = MachineJobComplaintTable.Instance.GetRowByID( "AllProductionHalted_Electical" );
                MachineJobComplaint elecComplaint2 = MachineJobComplaintTable.Instance.GetRowByID( "InsufficientElectricity" );

                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                {
                    if ( kv.Value != null )
                    {
                        if ( !kv.Value.IsFullDead )
                        {
                            if ( kv.Value.GetNeedsToBeGivenOrdersByPlayer() )
                                SimCommon.StructuresNeedingAttention.AddToConstructionList( kv.Value );

                            kv.Value.CurrentJob?.Implementation?.TryHandleJob( kv.Value, kv.Value?.CurrentJob, null, JobLogic.PerQuarterSecondAggregation, Rand );
                        }

                        if ( hasEnoughElectricity && (!kv.Value.DoesJobHaveEnoughElectricity ||
                            kv.Value.Complaints.GetConstructionDict().ContainsKey( elecComplaint1 ) ||
                            kv.Value.Complaints.GetDisplayDict().ContainsKey( elecComplaint2 )) )
                        {
                            kv.Value.DoesJobHaveEnoughElectricity = true;
                            kv.Value.DoesJobHaveAnyInputShortage = false;
                            kv.Value.Complaints.GetConstructionDict().Remove( elecComplaint1 );
                            kv.Value.Complaints.GetConstructionDict().Remove( elecComplaint2 );
                            kv.Value.Complaints.GetDisplayDict().Remove( elecComplaint1 );
                            kv.Value.Complaints.GetDisplayDict().Remove( elecComplaint2 );
                        }

                        bool hasLoggedAnyFormOfIssue = false;

                        if ( kv.Value.GetHasVisibleComplaint() )
                        {
                            SimCommon.StructuresWithVisibleComplaints.AddToConstructionList( kv.Value );
                            SimCommon.StructuresWithAnyFormOfIssue.AddToConstructionList( kv.Value );
                            hasLoggedAnyFormOfIssue = true;
                        }
                        else if ( kv.Value.GetHasAnyComplaint() )
                        {
                            SimCommon.StructuresWithLowerGradeComplaints.AddToConstructionList( kv.Value );
                            SimCommon.StructuresWithAnyFormOfIssue.AddToConstructionList( kv.Value );
                            hasLoggedAnyFormOfIssue = true;
                        }

                        if ( kv.Value.IsUnderConstruction || kv.Value.IsJobStillInstalling )
                            SimCommon.StructuresWithAnyFormOfConstruction.AddToConstructionList( kv.Value );
                        else if ( kv.Value.GetIsDamaged() || kv.Value.IsFullDead )
                        {
                            SimCommon.StructuresWithDamage.AddToConstructionList( kv.Value );
                            if ( !hasLoggedAnyFormOfIssue )
                            {
                                SimCommon.StructuresWithAnyFormOfIssue.AddToConstructionList( kv.Value );
                                hasLoggedAnyFormOfIssue = true;
                            }
                        }

                        if ( kv.Value.Type.IsTerritoryControlFlag )
                            SimCommon.TerritoryControlFlags.AddToConstructionList( kv.Value );

                        if ( !hasLoggedAnyFormOfIssue )
                            SimCommon.StructuresWithNoIssues.AddToConstructionList( kv.Value );
                    }
                }

                try
                {
                    SimCommon.StructuresWithAnyFormOfIssue.SortConstructionList( delegate ( MachineStructure Left, MachineStructure Right )
                    {
                        int val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() );
                        if ( val != 0 )
                            return val;
                        return Left.StructureID.CompareTo( Right.StructureID );
                    } );
                }
                catch { } //would be from the display name changing during the sort

                try
                {
                    SimCommon.StructuresWithNoIssues.SortConstructionList( delegate ( MachineStructure Left, MachineStructure Right )
                    {
                        int val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() );
                        if ( val != 0 )
                            return val;
                        return Left.StructureID.CompareTo( Right.StructureID );
                    } );
                }
                catch { } //would be from the display name changing during the sort

                SimCommon.StructuresWithVisibleComplaints.SwitchConstructionToDisplay();
                SimCommon.StructuresWithLowerGradeComplaints.SwitchConstructionToDisplay();
                SimCommon.StructuresWithDamage.SwitchConstructionToDisplay();
                SimCommon.StructuresWithAnyFormOfConstruction.SwitchConstructionToDisplay();
                SimCommon.StructuresWithAnyFormOfIssue.SwitchConstructionToDisplay();
                SimCommon.StructuresWithNoIssues.SwitchConstructionToDisplay();
                SimCommon.StructuresNeedingAttention.SwitchConstructionToDisplay();
                SimCommon.TerritoryControlFlags.SwitchConstructionToDisplay();


                if ( priorFlagCount != SimCommon.TerritoryControlFlags.Count || CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.Count > 0 )
                    SimCommon.NeedsVisibilityGranterRecalculation = true;
            }
            finally
            {
                Interlocked.Exchange( ref ActorRecalculationIsHappeningNow, 0 );
            }
        }
        #endregion

        #region DestroyXGuardsOfPOIWithTag
        private static void DestroyXGuardsOfPOIWithTag( MapPOI poi, NPCUnitTag Tag, int NumberToDestroy )
        {
            if ( poi == null || Tag == null || NumberToDestroy <= 0 )
                return;

            foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
            {
                NPCUnit npc = kv.Value;
                NPCUnitType npcType = npc?.UnitType;
                if ( npc != null && !npc.IsFullDead && !npc.IsDowned && npcType != null )
                {
                    MapPOI homePOI = npc.HomePOI;
                    if ( homePOI == poi && npcType.Tags.ContainsKey( Tag.ID ) )
                    {
                        npc.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                        NumberToDestroy--;
                        if ( NumberToDestroy <= 0 )
                            return;
                    }
                }
            }
        }
        #endregion
    }
}

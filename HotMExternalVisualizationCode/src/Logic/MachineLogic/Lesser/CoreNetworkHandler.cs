﻿using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class CoreNetworkHandler : IMachineNetworkPerTurnEvaluatorImplementation
    {
        public void HandleNetworksPerQuarterSecond( MersenneTwister Rand )
        {
            DoCoreRecalculation( Rand );
        }

        private void DoCoreRecalculation( MersenneTwister Rand )
        {
            if ( WorldSaveLoad.IsLoadingAtTheMoment )
                return;
            if ( !SimCommon.HasRunAtLeastOneRealSecond )
                return;
            if ( !SimCommon.HasDoneFirstNetworkQuarterSecondCycle )
                return;

            MachineNetwork network = SimCommon.TheNetwork;
            if ( network != null )
            {
                int requiredElectricity = 0;
                int generatedElectricity = 0;

                //if ( SimCommon.CurrentTimeline.CityStartRank >= 2 )
                //    generatedElectricity += MathRefs.ExtraElectricityDuringRank2PlusCities.IntMin;

                NetworkActorData electricityData = network.GetNetworkDataDataAndInitializeIfNeedBe( ActorRefs.GeneratedElectricity );

                electricityData.SortedJobProducers.ClearConstructionListForStartingConstruction();
                electricityData.SortedJobConsumers.ClearConstructionListForStartingConstruction();

                //ArcenDebugging.LogSingleLine( "GenCheck", Verbosity.DoNotShow );

                foreach ( KeyValuePair<int, MachineStructure> kv in network.ConnectedStructures.GetDisplayDict() )
                {
                    MachineStructure structure = kv.Value;

                    #region Electricity
                    {
                        int generatedAmount = structure.GetActorDataCurrent( ActorRefs.GeneratedElectricity, true );
                        int requiredAmount = structure.IsJobPaused ? 0 : structure.GetActorDataCurrent( ActorRefs.RequiredElectricity, true );

                        if ( !structure.IsJobPaused )
                        {
                            MachineJob job = structure.CurrentJob;
                            if ( job != null )
                                requiredAmount = Mathf.Max( requiredAmount, job.DuringGameActorData[ActorRefs.RequiredElectricity] );
                        }

                        generatedElectricity += generatedAmount;
                        requiredElectricity += requiredAmount;

                        if ( generatedAmount > 0 )
                            electricityData.SortedJobProducers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( structure, generatedAmount ) );
                        if ( requiredAmount > 0 )
                            electricityData.SortedJobConsumers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( structure, requiredAmount ) );
                    }
                    #endregion

                    //if ( generatedAmount > 0 )
                    //    ArcenDebugging.LogSingleLine( (structure.CurrentJob?.GetDisplayName() ??"[none]") + " job working: " + structure.IsFunctionalJob +
                    //        " generatedElectricity " + generatedAmount, Verbosity.DoNotShow );
                }

                //ArcenDebugging.LogSingleLine( "GenTotal: " + generatedElectricity + " count: " + network.ConnectedStructures.GetDisplayDict().Count + " requiredElectricity: " + requiredElectricity, Verbosity.DoNotShow );

                electricityData.SetAmountProvided( generatedElectricity );
                electricityData.SetAmountConsumed( requiredElectricity );

                electricityData.SortProducerAndConsumerLists();

                electricityData.SortedJobProducers.SwitchConstructionToDisplay();
                electricityData.SortedJobConsumers.SwitchConstructionToDisplay();

                #region ProducerConsumerTargets
                SimCommon.ProducerConsumerTargets.ClearConstructionListForStartingConstruction();

                if ( ActorRefs.GeneratedElectricity.DuringGameplay_GetShouldBeVisible() )
                {
                    if ( electricityData.SortedJobProducers.Count > 0 || electricityData.SortedJobConsumers.Count > 0 )
                        SimCommon.ProducerConsumerTargets.AddToConstructionList( electricityData );
                }

                foreach ( ResourceType resource in ResourceTypeTable.SortedRegularResources )
                {
                    if ( resource.IsHidden || resource.IsStrategicResource ) 
                        continue;

                    if ( resource.SortedJobProducers.Count > 0 || resource.SortedJobConsumers.Count > 0 ||
                        resource.SortedNamedProducers.Count > 0 || resource.SortedNamedConsumers.Count > 0 )
                        SimCommon.ProducerConsumerTargets.AddToConstructionList( resource );
                }

                SimCommon.ProducerConsumerTargets.SwitchConstructionToDisplay();
                #endregion ProducerConsumerTargets

                network.HasCalculatedNetworkEffectiveness = true;
            }
        }

        public void HandleNetworksPerTurn_Early( MersenneTwister RandForThisTurn )
        {
            DoCoreRecalculation( RandForThisTurn );
        }

        public void HandleNetworksPerTurn_Late( MersenneTwister RandForThisTurn )
        {
        }
    }
}

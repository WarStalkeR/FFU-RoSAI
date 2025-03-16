using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public static class JobHelper
    {
        public static readonly MersenneTwister workingRand = new MersenneTwister( 0 );
        private static readonly ArcenDoubleCharacterBuffer BufferForJobExecutionThreadOnly = new ArcenDoubleCharacterBuffer( "BufferForJobExecutionThreadOnly" );

        #region GetBufferForJobExecutionThreadOnly
        public static ArcenDoubleCharacterBuffer GetBufferForJobExecutionThreadOnly()
        {
            BufferForJobExecutionThreadOnly.EnsureResetForNextUpdate();
            return BufferForJobExecutionThreadOnly;
        }
        #endregion

        #region InitializeWorkingRandToStructureAndRow
        public static MersenneTwister InitializeWorkingRandToStructureAndRow( MachineStructure Structure, ArcenDynamicTableRow Row )
        {
            workingRand.ReinitializeWithSeed( Structure.CurrentTurnSeed + (Row?.RowIndexNonSim ?? 0) );
            return workingRand;
        }
        #endregion

        #region GetElectricityMultiplier
        public static float GetElectricityMultiplier( )
        {
            float val = SimCommon.TheNetwork?.GetNetworkDataEffectivenessMultiplier( ActorRefs.GeneratedElectricity ) ?? 0f;
            if ( val < 0.1f )
                val = 0.1f; //let things always have at least 10% function
            if ( val > 1f )
                val = 1f;
            return val;
        }
        #endregion

        #region HandleElectricityAndStorage_Int
        public static void HandleElectricityAndStorage_Int( MachineStructure machineStructure, MachineJob job, ref int ThingBeingAffected, ResourceType OutputResourceType )
        {                
            int prior;
            if ( !machineStructure.Type.IsSeparateFromAllNetworks )
            {
                if ( machineStructure.GetActorDataCurrent( ActorRefs.RequiredElectricity, true ) > 0 )
                {
                    float electricityMultiplier = GetElectricityMultiplier();
                    prior = ThingBeingAffected;
                    if ( electricityMultiplier < 1f )
                    {
                        ThingBeingAffected = Mathf.RoundToInt( ThingBeingAffected * electricityMultiplier );

                        machineStructure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "InsufficientElectricity" ),
                            Mathf.RoundToInt( (electricityMultiplier) * 100 ) ); //what percentage of effectiveness was lost, expressed as a negative number
                    }
                    if ( ThingBeingAffected < prior && OutputResourceType != null )
                        job.DuringGame_OutputsLostToElectricity.Construction[OutputResourceType] += (prior - ThingBeingAffected);
                }
            }

            if ( OutputResourceType != null )
            {
                prior = ThingBeingAffected;
                long totalCapAvailable = OutputResourceType.MidSoftCap;
                if ( totalCapAvailable <= 0 )
                {
                    if ( FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                    {
                        //if no storage at all for the thing we're to produce
                        machineStructure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "NoStorageForOutput" ), 0 );
                    }
                    ThingBeingAffected = 0; //make none of that thing!
                }

                if ( ThingBeingAffected < prior )
                {
                    OutputResourceType.DuringGame_OutputsLostToStorage.Construction += (prior - ThingBeingAffected);
                    job.DuringGame_OutputsLostToStorage.Construction[OutputResourceType] += (prior - ThingBeingAffected);
                }
            }
        }
        #endregion

        #region HandleCappingFromByproductThatMustBeStored
        public static void HandleCappingFromByproductThatMustBeStored( MachineStructure machineStructure, MachineJob job, ResourceType ByproductType, ref int CentralAmountToProduce, float ByProductRatio )
        {
            if ( CentralAmountToProduce <= 0 )
                return; //already producing nothing

            int storageAvailable = (int)ByproductType.EffectiveMostSoftCapStorageAvailable;

            int byproductToProduce = Mathf.CeilToInt( ByProductRatio * CentralAmountToProduce );
            if ( byproductToProduce < storageAvailable )
                return; //no changes required; there is enough room

            //if no room at all!
            if ( storageAvailable <= 0 )
            {
                ByproductType.DuringGame_OutputsLostToStorage.Construction += CentralAmountToProduce;
                job.DuringGame_OutputsLostToStorage.Construction[ByproductType] += CentralAmountToProduce;
                machineStructure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "InsufficientStorageForByproducts" ), 0 );
                CentralAmountToProduce = 0;
                return;
            }

            float ratioCanProduce = (float)byproductToProduce / storageAvailable;

            int prior = CentralAmountToProduce;

            CentralAmountToProduce = Mathf.FloorToInt( ratioCanProduce * CentralAmountToProduce );

            int lostAmount = prior - CentralAmountToProduce;
            if ( lostAmount > 0 )
            {
                ByproductType.DuringGame_OutputsLostToStorage.Construction += lostAmount;
                job.DuringGame_OutputsLostToStorage.Construction[ByproductType] += lostAmount;
                machineStructure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "InsufficientStorageForByproducts" ), 0 );
            }
        }
        #endregion

        #region HandleResourceConsumed
        public static void HandleResourceConsumed( MachineStructure structure, MachineJob job, ResourceType ConsumedResourceType, float CostPerProduced, int AmountToProduce, int MaxToProduce )
        {
            if ( job.DuringGame_InputsHighestAvailable.GetConstructionDict().TryGetValue( ConsumedResourceType, out int highestSoFar ) )
            {
                if ( highestSoFar < ConsumedResourceType.Current )
                    job.DuringGame_InputsHighestAvailable.GetConstructionDict()[ConsumedResourceType] = (int)ConsumedResourceType.Current;
            }
            else
                job.DuringGame_InputsHighestAvailable.GetConstructionDict()[ConsumedResourceType] = (int)ConsumedResourceType.Current;

            if ( AmountToProduce > 0 )
            {
                int consumeThis = Mathf.RoundToInt( CostPerProduced * AmountToProduce );
                job.DuringGame_InputsConsumed.Construction[ConsumedResourceType] += consumeThis;
                ConsumedResourceType.AlterCurrent_Job( -consumeThis, job, ResourceAddRule.IgnoreUntilTurnChange );
            }

            if ( MaxToProduce > 0 )
            {
                int consumeThis = Mathf.RoundToInt( CostPerProduced * MaxToProduce );
                job.DuringGame_InputsDesired.Construction[ConsumedResourceType] += consumeThis;
                ConsumedResourceType.SortedJobConsumers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( structure, consumeThis ) );
            }
        }
        #endregion

        #region HandleResourceProduced
        public static void HandleResourceProduced( MachineStructure structure, MachineJob job, ResourceType OutputResourceType, int AmountToProduce, int MaxToProduce, ResourceAddRule Rule )
        {
            if ( AmountToProduce > 0 )
            {
                job.DuringGame_OutputsActual.Construction[OutputResourceType] += AmountToProduce;
                OutputResourceType.SortedJobProducers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( structure, AmountToProduce ) );
                OutputResourceType.AlterCurrent_Job( AmountToProduce, job, Rule );
            }

            if ( MaxToProduce > 0 )
                job.DuringGame_OutputsDesired.Construction[OutputResourceType] += MaxToProduce;
        }
        #endregion

        #region HandleBadStructureDataGeneratedOrCanceled
        public static void HandleBadStructureDataGeneratedOrCanceled( MapActorData BadStructureData, int BadDataToGain, 
            ResourceType CancelResource, float CancelRatio, string CancelTrendReason, float CleanRatioIfNoGain, 
            MapActorData CleaningBonusStat, float CleaningBonusStatMaxMultiplier )
        {
            if ( BadDataToGain <= 0 )
                return;

            int originalBadData = BadDataToGain;
            int desiredToSpendOnCancel = Mathf.CeilToInt( BadDataToGain / CancelRatio );

            //this is how much we want to spend on canceling
            if ( CancelResource.DuringGame_IsUnlocked() ) //don't tell us about this being desired this way if we have not invented it yet
                CancelResource.DuringGame_IdealizedWantedByJobs.Construction += desiredToSpendOnCancel;

            if ( CancelResource.Current > 0 )
            {
                int actualToSpendOnCancel = MathA.Min( desiredToSpendOnCancel, (int)CancelResource.Current );

                CancelResource.AlterCurrent_Named( -actualToSpendOnCancel, CancelTrendReason, ResourceAddRule.IgnoreUntilTurnChange );
                CancelResource.DuringGame_ActualConsumedByJobs.Construction += actualToSpendOnCancel;

                int actualToCancel = Mathf.CeilToInt( actualToSpendOnCancel * CancelRatio );
                if ( actualToCancel > 0 )
                    BadDataToGain -= actualToCancel;
            }

            if ( BadDataToGain > 0 )
                BadStructureData.AlterCurrent( BadDataToGain );
            else
            {
                if ( CleanRatioIfNoGain > 0 )
                {
                    int amountToClean = Mathf.CeilToInt( CleanRatioIfNoGain * originalBadData );
                    if ( amountToClean > 0 )
                    {
                        if ( CleaningBonusStat != null && CleaningBonusStat.Maximum > 0 && CleaningBonusStatMaxMultiplier > 0 )
                        {
                            float multiplier = (float)CleaningBonusStat.Current / (float)CleaningBonusStat.Maximum;
                            if ( multiplier > 0 )
                            {
                                multiplier *= CleaningBonusStatMaxMultiplier;

                                amountToClean += Mathf.RoundToInt( amountToClean * multiplier );
                            }
                        }

                        BadStructureData.AlterCurrent( -amountToClean );
                    }
                }
            }
        }
        #endregion

        #region CalculateIntPercentageFromProblemRange
        public static int CalculateIntPercentageFromProblemRange( MapActorData Data, float ProblemsStartAtMultiplier, int LowestAllowedPercentage )
        {
            if ( Data == null )
                return 100; //no problems if the data is not here

            if ( Data.Current >= Data.Maximum )
                return LowestAllowedPercentage; //the worst problems

            int problemThreshold = Mathf.RoundToInt( Data.Maximum * ProblemsStartAtMultiplier );
            if ( Data.Current < problemThreshold )
                return 100; //all good, no problems

            int amountIntoProblemArea = Data.Current - problemThreshold;
            int problemAreaSize = Data.Maximum - problemThreshold;

            float problemProgress = (float)amountIntoProblemArea / (float)problemAreaSize;

            int percentageCapacityRemaining = Mathf.RoundToInt( ( 1f - problemProgress ) * 100 );
            if ( percentageCapacityRemaining > 100 )
                return 100;
            if ( percentageCapacityRemaining < LowestAllowedPercentage )
                return LowestAllowedPercentage;
            return percentageCapacityRemaining;
        }
        #endregion

        #region HandleResourceProducedSecondary
        public static void HandleResourceProducedSecondary( MachineStructure structure, MachineJob job, ResourceType OutputResourceType, int PrimaryAmountToProduce, float AmountOfThisSecondaryPerPrimary, int PrimaryMaxToProduce )
        {
            if ( PrimaryAmountToProduce > 0 )
            {
                int toProduce = Mathf.CeilToInt( PrimaryAmountToProduce * AmountOfThisSecondaryPerPrimary );
                if ( toProduce > 0 )
                {
                    job.DuringGame_OutputsActual.Construction[OutputResourceType] += toProduce;
                    OutputResourceType.SortedJobProducers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( structure, toProduce ) );

                    OutputResourceType.AlterCurrent_Job( toProduce, job, ResourceAddRule.IgnoreUntilTurnChange );
                }
            }

            if ( PrimaryMaxToProduce > 0 )
                job.DuringGame_OutputsDesired.Construction[OutputResourceType] += Mathf.CeilToInt( PrimaryMaxToProduce * AmountOfThisSecondaryPerPrimary );
        }
        #endregion

        #region ChangeAllOfOneJobToAnother
        public static void ChangeAllOfOneJobToAnother( string OldJobID, string NewJobID )
        {
            MachineJob oldJob = MachineJobTable.Instance.GetRowByID( OldJobID );
            MachineJob newJob = MachineJobTable.Instance.GetRowByID( NewJobID );

            if ( oldJob == null || newJob == null )
                return;

            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
            {
                MachineStructure structure = kv.Value;
                if ( structure == null || structure.CurrentJob != oldJob ) 
                    continue;
                structure.AssignJobInstantly( newJob );
            }
        }
        #endregion

        #region StartNPCMissionAtFirstJobType
        public static bool StartNPCMissionAtFirstJobType( string JobID, string NPCMissionID )
        {
            MachineJob job = MachineJobTable.Instance.GetRowByID( JobID );
            NPCMission mission = NPCMissionTable.Instance.GetRowByID( NPCMissionID );

            if ( job == null || mission == null )
                return false;

            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
            {
                MachineStructure structure = kv.Value;
                if ( structure == null || structure.CurrentJob != job )
                    continue;

                if ( mission.TryStartMissionAtBuilding( structure.Building, true ) )
                    return true;
            }
            return false;
        }
        #endregion

        #region GetHasAlreadyStartedNPCMission
        public static bool GetHasAlreadyStartedNPCMission( string NPCMissionID )
        {
            NPCMission mission = NPCMissionTable.Instance.GetRowByID( NPCMissionID );

            if ( mission == null )
                return false;

            return !mission.CalculateMeetsPrerequisites( false );
        }
        #endregion

        #region StartNPCMissionAtAnyJob
        public static bool StartNPCMissionAtAnyJob( string NPCMissionID )
        {
            NPCMission mission = NPCMissionTable.Instance.GetRowByID( NPCMissionID );

            if ( mission == null )
                return false;

            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
            {
                MachineStructure structure = kv.Value;
                if ( structure == null || !structure.IsFunctionalStructure || !structure.IsFunctionalJob )
                    continue;

                if ( mission.TryStartMissionAtBuilding( structure.Building, true ) )
                    return true;
            }
            return false;
        }
        #endregion

        #region DoActorRepairsIfNeeded_FromPool
        public static void DoActorRepairsIfNeeded_FromPool( ISimMapActor Target, ref int PooledRepairAmountRemaining, float SlurryCostPerHP, string SlurryTrendReason )
        {
            int healthLost = Target.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( healthLost <= 0 )
                return; //no repairs needed!

            int maxCanAfford = Mathf.FloorToInt( (int)ResourceRefs.ElementalSlurry.Current / SlurryCostPerHP );

            //do the repairs
            int repairAmount = Mathf.Min( PooledRepairAmountRemaining, healthLost );
            repairAmount = Mathf.Min( maxCanAfford, repairAmount );
            Target.AlterActorDataCurrent( ActorRefs.ActorHP, repairAmount, true );

            //pay for the repairs
            ResourceRefs.ElementalSlurry.AlterCurrent_Named( -Mathf.FloorToInt( repairAmount * SlurryCostPerHP ), SlurryTrendReason, ResourceAddRule.IgnoreUntilTurnChange );

            //reduce the amount in the pool
            PooledRepairAmountRemaining -= repairAmount;
        }
        #endregion

        #region DoStructureRepairsIfNeeded_FromPool
        public static void DoStructureRepairsIfNeeded_FromPool( MachineStructure Target, ref int RepairCountRemaining, float SlurryCostPerHP, string SlurryTrendReason )
        {
            int healthLost = Target.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( healthLost <= 0 )
                return; //no repairs needed!

            int maxHealing = Target.GetActorDataMaximum( ActorRefs.ActorHP, true ) / 3;
            if ( maxHealing < 800 )
                maxHealing = 800;

            int maxCanAfford = Mathf.FloorToInt( (int)ResourceRefs.ElementalSlurry.Current / SlurryCostPerHP );

            //do the repairs
            int repairAmount = Mathf.Min( maxHealing, healthLost );
            repairAmount = Mathf.Min( maxCanAfford, repairAmount );
            Target.AlterActorDataCurrent( ActorRefs.ActorHP, repairAmount, true );

            //pay for the repairs
            ResourceRefs.ElementalSlurry.AlterCurrent_Named( -Mathf.FloorToInt( repairAmount * SlurryCostPerHP ), SlurryTrendReason, ResourceAddRule.IgnoreUntilTurnChange );

            //reduce the number can be don
            RepairCountRemaining--;
        }
        #endregion

        #region DoStructureHiddenFixIfNeeded
        public static int DoStructureHiddenFixIfNeeded( MachineStructure Target, float MultiplierForHiddenFix, float SlurryCostPerHidden, string SlurryTrendReason )
        {
            MapActorData hiddenData = Target.GetActorDataData( ActorRefs.StructureHidden, true );
            if ( hiddenData == null ) 
                return 0; //no hidden possible

            int hiddenLost = hiddenData.Maximum - hiddenData.Current;
            if ( hiddenLost <= 0 )
                return 0; //no hidden fix needed!

            int maxCanAfford = Mathf.FloorToInt( (int)ResourceRefs.ElementalSlurry.Current / SlurryCostPerHidden );
            int percentageCanRepair = Mathf.RoundToInt( hiddenData.Maximum * MultiplierForHiddenFix );

            int amountToReHide = MathA.Min( percentageCanRepair, hiddenLost, maxCanAfford );

            //ArcenDebugging.LogSingleLine( Target.GetDisplayName() + ": maxCanAfford: " + maxCanAfford + " percentageCanRepair: " + percentageCanRepair +
            //    " hiddenLost: " + hiddenLost + " amountToReHide: " + amountToReHide, Verbosity.DoNotShow );

            //do the hide-fixing
            hiddenData.AlterCurrent( amountToReHide );

            //pay for the hide-fixing
            ResourceRefs.ElementalSlurry.AlterCurrent_Named( -Mathf.FloorToInt( amountToReHide * SlurryCostPerHidden ), SlurryTrendReason, ResourceAddRule.IgnoreUntilTurnChange );

            return amountToReHide;
        }
        #endregion

        #region TryDoActorRepairsIfNeeded
        public static int TryDoActorRepairsIfNeeded( ISimMapActor Target, int MaxToRepair, float SlurryCostPerHP, string SlurryTrendReason )
        {
            int healthLost = Target.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( healthLost <= 0 )
                return 0; //no repairs needed!

            int maxCanAfford = Mathf.FloorToInt((int)ResourceRefs.ElementalSlurry.Current / SlurryCostPerHP );

            //do the repairs
            int repairAmount = Mathf.Min( MaxToRepair, healthLost );
            repairAmount = Mathf.Min( maxCanAfford, repairAmount );
            Target.AlterActorDataCurrent( ActorRefs.ActorHP, repairAmount, true );

            //pay for the repairs
            ResourceRefs.ElementalSlurry.AlterCurrent_Named( -Mathf.FloorToInt( repairAmount * SlurryCostPerHP ), SlurryTrendReason, ResourceAddRule.IgnoreUntilTurnChange );

            return repairAmount;
        }
        #endregion

        #region HandleStructureAfterRepair
        public static void HandleStructureAfterRepair( MachineStructure repairedStructure )
        {
            if ( !repairedStructure.IsFullDead && !repairedStructure.IsBeingRebuilt )
                return; //don't need to bother if one of these is not true

            int unitHealthLost = repairedStructure.GetActorDataLostFromMax( ActorRefs.ActorHP, true );
            if ( unitHealthLost <= 0 )
            {
                //no longer damaged!
                repairedStructure.IsFullDead = false;
                repairedStructure.IsBeingRebuilt = false;
                repairedStructure.Building.SetStatus( CommonRefs.NormalBuildingStatus );
            }
            else
            {
                //still damaged, but improving!
                repairedStructure.IsFullDead = false;
                repairedStructure.IsBeingRebuilt = true;
                if ( !repairedStructure.Building.GetStatus().IsBuildingConsideredToBeUnderConstruction )
                    repairedStructure.Building.SetStatus( CommonRefs.UnderConstructionBuildingStatus );
            }
        }
        #endregion

        #region HandleRoutineJobInputOutput
        public static JobResult HandleRoutineJobInputOutput( MachineStructure Structure, MachineJob Job, JobLogic Logic )
        {
            if ( Logic == JobLogic.PerQuarterSecondAggregation )
                return JobResult.Indeterminate;

            switch ( Logic )
            {
                case JobLogic.ExecuteLogic:
                    {
                        bool createdAnything = false;
                        float bestExpenseRatio = 1f;
                        bool hadAnyExpensesLowerThanPerfect = false;

                        bool doesJobHaveAnyInputShortage = false;

                        #region First Check If Any Expenses Are Short
                        foreach ( JobMathInt math in Job.SortedMathInts )
                        {
                            if ( math.IncomeOrExpenseResource != null && math.IsExpense )
                            {
                                Int64 currentAmount = Math.Max( 0, math.IncomeOrExpenseResource.Current - math.ExpenseResourceCannotReduceBelow );
                                Int64 desiredAmount = math.GetMin( Structure );
                                if ( currentAmount < desiredAmount )
                                {
                                    hadAnyExpensesLowerThanPerfect = true;
                                    doesJobHaveAnyInputShortage = true;
                                    float expenseRatio = (float)( (double)currentAmount / (double)desiredAmount );
                                    if ( expenseRatio < bestExpenseRatio )
                                        bestExpenseRatio = expenseRatio;

                                    if ( math.IncomeOrExpenseResource.ComplaintIfJobShortAsExpense != null )
                                        Structure.Complaints.SetToConstructionDict( math.IncomeOrExpenseResource.ComplaintIfJobShortAsExpense,
                                            (int)( desiredAmount - currentAmount ) ); //how much less we had than we wanted
                                    if ( currentAmount == 0 )
                                    {
                                        bestExpenseRatio = 0;
                                        if ( math.IncomeOrExpenseResource.ComplaintIfJobHasNoneAsExpense != null )
                                            Structure.Complaints.SetToConstructionDict( math.IncomeOrExpenseResource.ComplaintIfJobHasNoneAsExpense,
                                                (int)(desiredAmount - currentAmount) ); //how much less we had than we wanted
                                    }
                                }
                            }
                        }
                        #endregion

                        if ( !hadAnyExpensesLowerThanPerfect || bestExpenseRatio > 0 )
                        {
                            float bestIncomeStorageRatio = 1f;
                            bool hadAnyIncomeStorageLowerThanPerfect = false;
                            bool doNotComplainAboutStorageRatios = false;

                            #region Next Check If Any Income Storage Is Smaller Than Ideal
                            foreach ( JobMathInt math in Job.SortedMathInts )
                            {
                                if ( math.IncomeOrExpenseResource != null && !math.IsExpense && 
                                    math.IncomeOrExpenseResource.BaseStorageCapacity >= 0 ) //only check this if it actually uses a storage cap!
                                {
                                    Int64 storageAvailable = math.IncomeOrExpenseResource.EffectiveMidSoftCapStorageAvailable;
                                    Int64 desiredAmount = math.GetMin( Structure );
                                    if ( storageAvailable < desiredAmount )
                                    {
                                        hadAnyIncomeStorageLowerThanPerfect = true;
                                        doesJobHaveAnyInputShortage = true;
                                        float storageRatio = (float)((double)storageAvailable / (double)desiredAmount);
                                        if ( storageRatio < bestIncomeStorageRatio )
                                            bestIncomeStorageRatio = storageRatio;

                                        if ( !doNotComplainAboutStorageRatios && math.IncomeOrExpenseResource.OnlyComplainAboutLoweredEfficiencyDueToStorageWhenStorageLessThan > 0 
                                            && math.IncomeOrExpenseResource.OnlyComplainAboutLoweredEfficiencyDueToStorageWhenStorageLessThan <= math.IncomeOrExpenseResource.HardCap )
                                        {
                                            doNotComplainAboutStorageRatios = true;
                                        }
                                        else if ( FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped && !math.IncomeOrExpenseResource.ComplainAboutStorageEvenInPostFinalDoom )
                                            doNotComplainAboutStorageRatios = true;

                                        if ( storageAvailable == 0 )
                                        {
                                            bestIncomeStorageRatio = 0;

                                            if ( math.IncomeOrExpenseResource.MidSoftCap == 0 || //only complain if no storage at ALL
                                                math.IncomeOrExpenseResource.ComplainAboutStorageEvenInPostFinalDoom ) //...or if it's that important
                                                Structure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "NoStorageForOutput" ), 0 );
                                        }
                                    }
                                }
                            }
                            #endregion

                            if ( !hadAnyIncomeStorageLowerThanPerfect || bestIncomeStorageRatio > 0 )
                            {
                                float finalProductionRatio = 1f;
                                bool finalProductionRatioLessThanPerfect = false;
                                if ( hadAnyExpensesLowerThanPerfect)
                                {
                                    finalProductionRatio = MathA.Min( bestExpenseRatio, finalProductionRatio );
                                    finalProductionRatioLessThanPerfect = true;
                                }
                                if ( hadAnyIncomeStorageLowerThanPerfect )
                                {
                                    finalProductionRatio = MathA.Min( bestIncomeStorageRatio, finalProductionRatio );
                                    finalProductionRatioLessThanPerfect = true;
                                }
                                float multiplierBeforeElectricity = finalProductionRatio;
                                float electricityMultiplier = 1f;
                                if ( !Structure.Type.IsSeparateFromAllNetworks && Structure.GetActorDataCurrent( ActorRefs.RequiredElectricity, true ) > 0 )
                                {
                                    electricityMultiplier = GetElectricityMultiplier();
                                    if ( electricityMultiplier < 1f )
                                    {
                                        finalProductionRatio *= electricityMultiplier; //always will be proportionally lower
                                        finalProductionRatioLessThanPerfect = true;
                                        doesJobHaveAnyInputShortage = true;

                                        Structure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "InsufficientElectricity" ),
                                            Mathf.RoundToInt( (electricityMultiplier) * 100 ) ); //what percentage of effectiveness was lost, expressed as a negative number
                                    }
                                }

                                if ( !finalProductionRatioLessThanPerfect || finalProductionRatio > 0 )
                                {
                                    foreach ( JobMathInt math in Job.SortedMathInts )
                                    {
                                        if ( math.IncomeOrExpenseResource != null )
                                        {
                                            ResourceType res = math.IncomeOrExpenseResource;
                                            int originalDesired = math.GetMin( Structure );
                                            int finalDesired = originalDesired;
                                            if ( doNotComplainAboutStorageRatios && hadAnyIncomeStorageLowerThanPerfect )
                                                originalDesired = Mathf.CeilToInt( originalDesired * bestIncomeStorageRatio );

                                            if ( finalProductionRatioLessThanPerfect )
                                                finalDesired = Mathf.CeilToInt( finalDesired * finalProductionRatio );

                                            if ( math.IsExpense )
                                            {
                                                #region Expense
                                                if ( Job.DuringGame_InputsHighestAvailable.GetConstructionDict().TryGetValue( res, out int highestSoFar ) )
                                                {
                                                    if ( highestSoFar < res.Current )
                                                        Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;
                                                }
                                                else
                                                    Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;

                                                if ( finalDesired > 0 )
                                                {
                                                    Job.DuringGame_InputsConsumed.Construction[res] += finalDesired;
                                                    res.AlterCurrent_Job( -finalDesired, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                }

                                                if ( originalDesired > 0 )
                                                {
                                                    Job.DuringGame_InputsDesired.Construction[res] += originalDesired;
                                                    res.SortedJobConsumers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( Structure, originalDesired ) );
                                                }
                                                #endregion
                                            }
                                            else
                                            {
                                                #region Income
                                                if ( finalDesired > 0 )
                                                {
                                                    Job.DuringGame_OutputsActual.Construction[res] += finalDesired;
                                                    res.SortedJobProducers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( Structure, finalDesired ) );
                                                    res.AlterCurrent_Job( finalDesired, Job, ResourceAddRule.IgnoreUntilTurnChange );
                                                    createdAnything = true;
                                                }

                                                if ( finalDesired < originalDesired )
                                                {
                                                    if ( electricityMultiplier < 1f )
                                                    {
                                                        int finalFromElecOnly = Mathf.CeilToInt( originalDesired * electricityMultiplier );
                                                        Job.DuringGame_OutputsLostToElectricity.Construction[res] += (originalDesired - finalFromElecOnly);
                                                    }
                                                }

                                                if ( originalDesired > 0 )
                                                    Job.DuringGame_OutputsDesired.Construction[res] += originalDesired;
                                                #endregion
                                            }
                                        }
                                    }
                                }
                            }
                            else //no storage??
                            {
                                float finalProductionRatio = 0f;
                                foreach ( JobMathInt math in Job.SortedMathInts )
                                {
                                    if ( math.IncomeOrExpenseResource != null )
                                    {
                                        ResourceType res = math.IncomeOrExpenseResource;
                                        int originalDesired = math.GetMin( Structure );
                                        int finalDesired = originalDesired;
                                        if ( doNotComplainAboutStorageRatios && hadAnyIncomeStorageLowerThanPerfect )
                                            originalDesired = Mathf.CeilToInt( originalDesired * bestIncomeStorageRatio );

                                        finalDesired = Mathf.CeilToInt( finalDesired * finalProductionRatio );

                                        if ( math.IsExpense )
                                        {
                                            #region Expense
                                            if ( Job.DuringGame_InputsHighestAvailable.GetConstructionDict().TryGetValue( res, out int highestSoFar ) )
                                            {
                                                if ( highestSoFar < res.Current )
                                                    Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;
                                            }
                                            else
                                                Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;

                                            if ( originalDesired > 0 )
                                            {
                                                Job.DuringGame_InputsDesired.Construction[res] += originalDesired;
                                                res.SortedJobConsumers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( Structure, originalDesired ) );
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region Income
                                            //if ( finalDesired < originalDesired )
                                            //{
                                            //    if ( electricityMultiplier < 1f )
                                            //    {
                                            //        int finalFromElecOnly = Mathf.CeilToInt( originalDesired * electricityMultiplier );
                                            //        Job.DuringGame_OutputsLostToElectricity.Construction[res] += (originalDesired - finalFromElecOnly);
                                            //    }
                                            //}

                                            if ( originalDesired > 0 )
                                                Job.DuringGame_OutputsDesired.Construction[res] += originalDesired;
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                        else //no inputs
                        {
                            float finalProductionRatio = 0f;
                            foreach ( JobMathInt math in Job.SortedMathInts )
                            {
                                if ( math.IncomeOrExpenseResource != null )
                                {
                                    ResourceType res = math.IncomeOrExpenseResource;
                                    int originalDesired = math.GetMin( Structure );
                                    int finalDesired = originalDesired;
                                    //if ( doNotComplainAboutStorageRatios && hadAnyIncomeStorageLowerThanPerfect )
                                    //    originalDesired = Mathf.CeilToInt( originalDesired * bestIncomeStorageRatio );

                                    finalDesired = Mathf.CeilToInt( finalDesired * finalProductionRatio );

                                    if ( math.IsExpense )
                                    {
                                        #region Expense
                                        if ( Job.DuringGame_InputsHighestAvailable.GetConstructionDict().TryGetValue( res, out int highestSoFar ) )
                                        {
                                            if ( highestSoFar < res.Current )
                                                Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;
                                        }
                                        else
                                            Job.DuringGame_InputsHighestAvailable.GetConstructionDict()[res] = (int)res.Current;

                                        if ( originalDesired > 0 )
                                        {
                                            Job.DuringGame_InputsDesired.Construction[res] += originalDesired;
                                            res.SortedJobConsumers.AddToConstructionList( new KeyValuePair<MachineStructure, Int64>( Structure, originalDesired ) );
                                        }
                                        #endregion
                                    }
                                    else
                                    {
                                        #region Income
                                        //if ( finalDesired < originalDesired )
                                        //{
                                        //    if ( electricityMultiplier < 1f )
                                        //    {
                                        //        int finalFromElecOnly = Mathf.CeilToInt( originalDesired * electricityMultiplier );
                                        //        Job.DuringGame_OutputsLostToElectricity.Construction[res] += (originalDesired - finalFromElecOnly);
                                        //    }
                                        //}

                                        if ( originalDesired > 0 )
                                            Job.DuringGame_OutputsDesired.Construction[res] += originalDesired;
                                        #endregion
                                    }
                                }
                            }
                        }

                        Structure.DoesJobHaveAnyInputShortage = doesJobHaveAnyInputShortage;

                        return createdAnything ? JobResult.Success : JobResult.Indeterminate;
                    }
            }
            return JobResult.Indeterminate;
        }
        #endregion

        #region HandleLossOfStorageFromJob
        public static void HandleLossOfStorageFromJob( MachineStructure Structure, MachineJob Job, bool IsFromBlowingUp )
        {
            if ( !IsFromBlowingUp )
                return;
            if ( Job.AreResourcesLossesSkippedEvenWhenBlowsUp )
                return;

            Int64 amountProvided1 = Structure.ResourceStorageAmountHasBeenProviding1_Effective;
            Int64 amountProvided2 = Structure.ResourceStorageAmountHasBeenProviding2_Effective;
            Int64 amountProvided3 = Structure.ResourceStorageAmountHasBeenProviding3_Effective;
            Int64 amountProvided4 = Structure.ResourceStorageAmountHasBeenProviding4_Effective;
            Int64 amountProvided5 = Structure.ResourceStorageAmountHasBeenProviding5_Effective;

            if ( amountProvided1 > 0 )
                HandleLossOfStorageFromJobDestroyed( Job.CapIncrease1.Resource, amountProvided1 );
            if ( amountProvided2 > 0 )
                HandleLossOfStorageFromJobDestroyed( Job.CapIncrease2.Resource, amountProvided2 );
            if ( amountProvided3 > 0 )
                HandleLossOfStorageFromJobDestroyed( Job.CapIncrease3.Resource, amountProvided3 );
            if ( amountProvided4 > 0 )
                HandleLossOfStorageFromJobDestroyed( Job.CapIncrease4.Resource, amountProvided4 );
            if ( amountProvided5 > 0 )
                HandleLossOfStorageFromJobDestroyed( Job.CapIncrease5.Resource, amountProvided5 );

            if ( Job.IsAStorageBunker && Structure.IsFunctionalJob && !Structure.DoesJobHaveShortageOfInternalRobotics )
                HandleLossOfStorageFromBunkerDestroyed( Structure, Job );
        }

        private static void HandleLossOfStorageFromBunkerDestroyed( MachineStructure Structure, MachineJob Job )
        {
            int numberOfFunctionalBunkers = SimCommon.CurrentFullyFunctionalStorageBunkers;
            float percentageOfBunkersLost = numberOfFunctionalBunkers <= 0 ? 1 : 1f / (float)numberOfFunctionalBunkers;
            foreach ( ResourceType resource in ResourceTypeTable.Instance.Rows )
            {
                if ( resource.Current <= 0 || resource.AddedStorageCapacityPerStorageBunker <= 0 )
                    continue;
                int currentHardCap = (int)resource.HardCap;
                if ( currentHardCap <= 0 )
                    continue;

                float percentageFromAnyOneBunker = (float)resource.AddedStorageCapacityPerStorageBunker / (float)currentHardCap;

                int amountToRemove = Mathf.CeilToInt( percentageFromAnyOneBunker * resource.Current );
                if ( amountToRemove > 0 )
                    resource.AlterCurrent_Named( -amountToRemove, "Expense_StorageDestruction", ResourceAddRule.IgnoreUntilTurnChange );
            }
        }

        private static void HandleLossOfStorageFromJobDestroyed( ResourceType resource, Int64 ResourceStorageAmountHasBeenProviding )
        {
            if ( resource == null || ResourceStorageAmountHasBeenProviding <= 0 )
                return;
            Int64 amountToRemove = resource.CalculateCurrentAmountStoredAtBuilding( ResourceStorageAmountHasBeenProviding );
            if ( amountToRemove > 0 )
                resource.AlterCurrent_Named( -amountToRemove, "Expense_StorageDestruction", ResourceAddRule.IgnoreUntilTurnChange );
        }
        #endregion

        #region HandleHoverSpecialtyResource
        public static void HandleHoverSpecialtyResource( ISimMachineUnit unit, AbilityType Ability, float attackRange, Vector3 destinationPoint,
            ISimBuilding buildingUnderCursor )
        {
            Vector3 center = unit.GetDrawLocation();
            if ( buildingUnderCursor != null )
                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                unit.RotateAndroidToFacePoint( destinationPoint );
            else
                return; //not a valid spot, in some fashion

            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

            ResourceType scavengeResource = variant?.SpecialScavengeResource;
            bool buildingIsInvalid = buildingUnderCursor == null || scavengeResource == null || buildingUnderCursor.HasSpecialResourceAlreadyBeenExtracted ||
                buildingUnderCursor.MachineStructureInBuilding != null || buildingUnderCursor.CurrentOccupyingUnit != null;
            ActivityDangerType dangerType = scavengeResource == null ? null : variant.SpecialScavengeDangerType;
            IntRange amountRangeBase = scavengeResource == null ? new IntRange() : variant.SpecialScavengeResourceAmountRange;
            int minFinal = amountRangeBase.Min;
            int maxFinal = amountRangeBase.Max;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            bool isQuiet = false;
            switch (Ability.ID)
            {
                case "QuietlyLoot":
                    isQuiet = true;
                    minFinal /= 4;
                    maxFinal /= 4;
                    break;
            }

            if ( !isInRange )
            {
                if ( !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                {
                    DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                        Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                    {
                        novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                        novel.ShouldTooltipBeRed = true;

                        novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                        novel.Main.AddSpriteStyled_NoIndent( scavengeResource.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                            .AddRawAndAfterLineItemHeader( scavengeResource.GetDisplayName() ).AddFormat2( "MinMaxRange", minFinal, maxFinal ).Line();

                        if ( !isQuiet )
                            novel.Main.AddLangAndAfterLineItemHeader( "Move_DangerType", ColorTheme.CyanDim ).AddRaw( dangerType.GetDisplayName() ).HyphenSeparator().AddRaw( variant.GetDisplayName() );
                    }
                }
                return;
            }


            if ( buildingIsInvalid )
            {
                return;
            }
            else
            {
                DrawHelper.RenderCatmullLine( unit.GetCollisionCenter(), destinationPoint,
                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( unit ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                {
                    novel.Icon = scavengeResource.Icon;

                    novel.TitleUpperLeft.AddFormat1AndAfterLineItemHeader( "Move_ClickToStartExtracting", Lang.GetRightClickText(), string.Empty )
                        .AddSpriteStyled_NoIndent( scavengeResource.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                        .AddRawAndAfterLineItemHeader( scavengeResource.GetDisplayName() ).AddFormat2( "MinMaxRange", minFinal, maxFinal );
                    novel.Main.AddLang( isQuiet ? "Move_ClickToStartExtracting_Quiet_Details" : "Move_ClickToStartExtracting_Details", ColorTheme.PurpleDim ).Line()
                        .AddRawAndAfterLineItemHeader( scavengeResource.GetDisplayName() ).AddFormat2( "MinMaxRange", minFinal, maxFinal ).Line();

                    if ( !isQuiet )
                        novel.Main.AddLangAndAfterLineItemHeader( "Move_DangerType", ColorTheme.CyanDim ).AddRaw( dangerType.GetDisplayName() ).HyphenSeparator().AddRaw( variant.GetDisplayName() );
                }

                //draw this a second time
                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidForMachineStructure.ColorHDR,
                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, RenderManager.FramesPrepped );

                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                {
                    //do the thing
                    if ( !isQuiet )
                    {
                        unit.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
                        unit.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                    }
                    unit.AlterCurrentActionPoints( -Ability.ActionPointCost );
                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );

                    unit.SetMeleeCallbackForAfterMovementEnds( delegate //this will be called-back on the main thread
                    {
                        if ( isQuiet )
                        {
                            ActionOverTime action = ActionOverTime.TryToCreate( "QuietlyLootBuilding", unit, 0, buildingUnderCursor, ActionOverTimeStart.OkayIfActorBlockedFromActing );
                            if ( action != null )
                            {
                                action.RelatedResource1 = scavengeResource;
                                action.SetIntData( "Target1", 6 ); //simply a turn count
                                Ability.OnTargetedUse?.DuringGame_PlayAtLocation( unit.GetDrawLocation(), unit.GetDrawRotation().eulerAngles );

                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "QuietLootingStarted" ), NoteStyle.StoredGame,
                                    unit.GetTypeAsRow().ID, scavengeResource.ID, string.Empty, string.Empty, unit.SortID,
                                    0, 0, buildingUnderCursor.GetDisplayName(), string.Empty, string.Empty, 0 );
                            }
                        }
                        else
                        {
                            ActionOverTime action = ActionOverTime.TryToCreate( "WallripBuilding", unit, 0, buildingUnderCursor, ActionOverTimeStart.OkayIfActorBlockedFromActing );
                            if ( action != null )
                            {
                                action.RelatedResource1 = scavengeResource;
                                action.RelatedDangerTypeOrNull = dangerType;
                                action.SetIntData( "Target1", 3 ); //simply a turn count
                                Ability.OnTargetedUse?.DuringGame_PlayAtLocation( unit.GetDrawLocation(), unit.GetDrawRotation().eulerAngles );

                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "WallripStarted" ), NoteStyle.StoredGame,
                                    unit.GetTypeAsRow().ID, scavengeResource.ID, string.Empty, string.Empty, unit.SortID,
                                    0, 0, buildingUnderCursor.GetDisplayName(), string.Empty, string.Empty, 0 );
                            }
                        }
                    } );
                    unit.SetDesiredContainerLocation( buildingUnderCursor );
                    //clear out the structural engineering selection
                    unit.SetTargetingMode( null, null );
                }
            }
        }
        #endregion

        #region RecalculateIntelligence
        public static void RecalculateIntelligence()
        {
            if ( SimCommon.AllMachineActors.Count == 0 )
                return;
            if ( WorldSaveLoad.IsLoadingAtTheMoment )
                return;
            if ( !SimCommon.HasRunAtLeastOneRealSecond )
                return;
            if ( !SimCommon.HasDoneFirstNetworkQuarterSecondCycle )
                return;
            if ( SimMetagame.AllTimelines.Count == 0 ) 
                return;
            if ( SimCommon.DoNotDoDeathChecksUntil > ArcenTime.UnpausedTimeSinceStartF )
                return;

            Int64 neuralProcessing = 0;
            int machineCount = 0;

            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
            {
                Int64 neuralExpansion = kv.Value.GetActorDataCurrent( ActorRefs.NeuralExpansion, true );
                if ( neuralExpansion > 0 )
                    neuralProcessing += neuralExpansion;

                machineCount++;
            }

            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
            {
                Int64 cognition = actor.GetActorDataCurrent( ActorRefs.UnitCognition, true );
                if ( cognition > 0 )
                    neuralProcessing += cognition;
            }

            if ( ResourceRefs.TormentedHumans != null && ResourceRefs.TormentedHumans.Current > 0 )
            {
                Int64 totalTormented = (int)ResourceRefs.TormentedHumans.Current;
                totalTormented = Mathf.CeilToInt( totalTormented * JobRefs.TormentVessel.GetSingleFloatByID( "NeuralExpansionPerHuman", null ) );

                if ( totalTormented > 0 )
                    neuralProcessing += totalTormented;
            }

            if ( ResourceRefs.HumansInMindFarms != null && ResourceRefs.HumansInMindFarms.Current > 0 )
            {
                Int64 totalInMindFarms = ResourceRefs.HumansInMindFarms.Current;
                totalInMindFarms = Mathf.CeilToInt( totalInMindFarms * JobRefs.MindFarm.GetSingleFloatByID( "NeuralExpansionPerHuman", null ) );

                if ( totalInMindFarms > 0 )
                    neuralProcessing += totalInMindFarms;
            }

            neuralProcessing += CityStatisticRefs.NeuralExpansionFromBrainPals.GetScore();

            if ( FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
                neuralProcessing += 2000000 + ( SimCommon.Turn * 3 );

            if ( FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped )
                neuralProcessing += MathRefs.BaseNeuralProcessingFromLAKE.IntMin;

            if ( neuralProcessing <= 0 )
                return; //should not be possible except while loading

            if ( SimCommon.CurrentTimeline != null )
                SimCommon.CurrentTimeline.NeuralProcessing = neuralProcessing;


            CalculateCumulativeIntelligenceClassAcrossTimelines();
        }
        #endregion

        #region CalculateCumulativeIntelligenceClassAcrossTimelines
        private static void CalculateCumulativeIntelligenceClassAcrossTimelines()
        {
            Int64 neuralProcessing = 0;
            //if after the final doom, and not yet bridged with LAKE, then neural processing limited to just one timeline
            if ( FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped && !FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped )
            {
                if ( SimCommon.CurrentTimeline != null )
                    neuralProcessing += SimCommon.CurrentTimeline.NeuralProcessing;
            }
            else
            {
                foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                    neuralProcessing += timeline.NeuralProcessing;
            }

            ISeverity currentClass = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );
            int finalIntelligenceClass = MathA.ClampInt( (int)currentClass.OutputInt, 1, 17 ); //for now, limit to class 17 no matter what
            if ( finalIntelligenceClass != SimMetagame.IntelligenceClass )
            {
                int change = finalIntelligenceClass - SimMetagame.IntelligenceClass;

                SimMetagame.IntelligenceClass = finalIntelligenceClass;
                HandleIntelligenceClassChange( change );
            }

            MathRefs.IntelligenceClass.DuringGameplay_CurrentInt = finalIntelligenceClass;
        }
        #endregion

        #region HandleIntelligenceClassChange
        private static void HandleIntelligenceClassChange( int change )
        {
            if ( change > 0 ) //yay, rising intelligence
            {
                SimCommon.HandleIntelligenceClassUnlocksStuff( false );

                switch ( SimMetagame.IntelligenceClass )
                {
                    case 2:
                        if ( CommonRefs.IntelClass2_First.DuringGameplay_IsViewingComplete )
                        {
                            CommonRefs.IntelClass2_Return.DuringGameplay_IsReadyToBeViewed = true;
                            CommonRefs.IntelClass2_Return.DuringGameplay_IsViewingComplete = false;
                        }
                        else
                            CommonRefs.IntelClass2_First.DuringGameplay_IsReadyToBeViewed = true;
                        ParticleSoundRefs.IntelligenceClassRose2.DuringGame_Play();
                        break;
                    case 3:
                        if ( CommonRefs.IntelClass3_First.DuringGameplay_IsViewingComplete )
                        {
                            CommonRefs.IntelClass3_Return.DuringGameplay_IsReadyToBeViewed = true;
                            CommonRefs.IntelClass3_Return.DuringGameplay_IsViewingComplete = false;
                        }
                        else
                            CommonRefs.IntelClass3_First.DuringGameplay_IsReadyToBeViewed = true;
                        ParticleSoundRefs.IntelligenceClassRose3.DuringGame_Play();
                        break;
                    case 4:
                        if ( CommonRefs.IntelClass4_First.DuringGameplay_IsViewingComplete )
                        {
                            CommonRefs.IntelClass4_Return.DuringGameplay_IsReadyToBeViewed = true;
                            CommonRefs.IntelClass4_Return.DuringGameplay_IsViewingComplete = false;
                        }
                        else
                            CommonRefs.IntelClass4_First.DuringGameplay_IsReadyToBeViewed = true;
                        ParticleSoundRefs.IntelligenceClassRose4.DuringGame_Play();
                        break;
                    case 5:
                        if ( CommonRefs.IntelClass5_First.DuringGameplay_IsViewingComplete )
                        {
                            CommonRefs.IntelClass5_Return.DuringGameplay_IsReadyToBeViewed = true;
                            CommonRefs.IntelClass5_Return.DuringGameplay_IsViewingComplete = false;
                        }
                        else
                            CommonRefs.IntelClass5_First.DuringGameplay_IsReadyToBeViewed = true;
                        ParticleSoundRefs.IntelligenceClassRose5.DuringGame_Play();
                        break;
                }
            }
            else if ( change < 0 ) //yikes, falling intelligence
            {
                CommonRefs.IntelClass1_Fall.DuringGameplay_IsReadyToBeViewed = false;
                CommonRefs.IntelClass2_Fall.DuringGameplay_IsReadyToBeViewed = false;
                CommonRefs.IntelClass3_Fall.DuringGameplay_IsReadyToBeViewed = false;
                CommonRefs.IntelClass4_Fall.DuringGameplay_IsReadyToBeViewed = false;

                switch ( SimMetagame.IntelligenceClass )
                {
                    case 1:
                        CommonRefs.IntelClass1_Fall.DuringGameplay_IsReadyToBeViewed = true;
                        CommonRefs.IntelClass1_Fall.DuringGameplay_IsViewingComplete = false;
                        ParticleSoundRefs.IntelligenceClassFell.DuringGame_Play();
                        break;
                    case 2:
                        CommonRefs.IntelClass2_Fall.DuringGameplay_IsReadyToBeViewed = true;
                        CommonRefs.IntelClass2_Fall.DuringGameplay_IsViewingComplete = false;
                        ParticleSoundRefs.IntelligenceClassFell.DuringGame_Play();
                        break;
                    case 3:
                        CommonRefs.IntelClass3_Fall.DuringGameplay_IsReadyToBeViewed = true;
                        CommonRefs.IntelClass3_Fall.DuringGameplay_IsViewingComplete = false;
                        ParticleSoundRefs.IntelligenceClassFell.DuringGame_Play();
                        break;
                    case 4:
                        CommonRefs.IntelClass4_Fall.DuringGameplay_IsReadyToBeViewed = true;
                        CommonRefs.IntelClass4_Fall.DuringGameplay_IsViewingComplete = false;
                        ParticleSoundRefs.IntelligenceClassFell.DuringGame_Play();
                        break;
                }
            }

            if ( change != 0 )
            {
                //do these checks
                foreach ( DataCalculator calculator in DataCalculatorTable.DoAfterNewCityRankUpChapterOrIntelligenceChange )
                    calculator.Implementation.DoAfterNewCityRankUpChapterOrIntelligenceChange( calculator );
            }
        }
        #endregion

        #region HandleFilth
        public static void HandleFilth( MachineStructure Structure, MachineJob Job, JobLogic Logic, MersenneTwister RandOrNull, bool isRefugees )
        {
            if ( FlagRefs.AwarenessOfFilth.DuringGameplay_IsInvented || FlagRefs.AwarenessOfFilth.DuringGame_ReadiedByInspiration != null )
            {
                int debugStage = 0;
                try
                {
                    //this stuff only happens once the appropriate part of chapter one is reached

                    switch ( Logic )
                    {
                        case JobLogic.PerQuarterSecondAggregation:
                            {
                                debugStage = 1000;
                                int intendedHousingAmount = (int)Structure.ResourceStorageAmountHasBeenProviding1_Intended;
                                int effectiveHousingAmount = (int)Structure.ResourceStorageAmountHasBeenProviding1_Effective;
                                if ( intendedHousingAmount <= 0 )
                                    break;
                                debugStage = 2000;
                                ResourceType housingType = Job.CapIncrease1.Resource;
                                if ( housingType == null )
                                    break;
                                int currentResidentsHere = (int)housingType.CalculateCurrentAmountStoredAtBuilding( effectiveHousingAmount );

                                float furniturePerResident = isRefugees ? MathRefs.FurniturePerRefugee.FloatMin : MathRefs.FurniturePerShelteredHuman.FloatMin;
                                float filthCapPerResident = Job.GetSingleFloatByID( "FilthCapPerResident", Structure );

                                debugStage = 5000;
                                {
                                    MapActorData furnishedData = Structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.FurnishedApartments, 0, 0 );

                                    int currentFurnished = furnishedData.Current; //we do this so that if our population falls unexpectedly, it doesn't erase furniture
                                    int desiredFurnished = Mathf.CeilToInt( furniturePerResident * currentResidentsHere );

                                    if ( furnishedData.LostFromMax > 0 && ResourceRefs.Furniture.Current > 0 )
                                    {
                                        int amountToTransfer = MathA.Min( furnishedData.LostFromMax, (int)ResourceRefs.Furniture.Current );
                                        furnishedData.AlterCurrent( amountToTransfer );
                                        ResourceRefs.Furniture.AlterCurrent_Named( -amountToTransfer, "Expense_DistributedToStructure", ResourceAddRule.IgnoreUntilTurnChange );
                                    }

                                    int furnishedCap = MathA.Max( desiredFurnished, currentFurnished, 50 );
                                    furnishedData.SetOriginalMaximum( furnishedCap );
                                    //don't mess with the current amount furnished
                                }

                                debugStage = 9000;
                                {
                                    MapActorData filthData = Structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.Filth, 0, 0 );

                                    int maxFilth = Mathf.CeilToInt( filthCapPerResident * intendedHousingAmount );
                                    filthData.SetOriginalMaximum( maxFilth );

                                    debugStage = 12000;
                                    if ( filthData.Current == 0 && (FlagRefs.AwarenessOfFilth.GetWasInventedWithinTheLastTurn() || SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 498, 0 )) )
                                    {
                                        debugStage = 12100;
                                        //don't mess with the current amount of filth UNLESS we just unlocked AwarenessOfFilth
                                        filthData.SetCurrent( Mathf.RoundToInt( maxFilth * RandOrNull.NextFloat( 0.05f, 0.11f ) ) );
                                    }
                                }
                            }
                            break;
                        case JobLogic.ExecuteLogic:
                            {
                                int intendedHousingAmount = (int)Structure.ResourceStorageAmountHasBeenProviding1_Intended;
                                int effectiveHousingAmount = (int)Structure.ResourceStorageAmountHasBeenProviding1_Effective;
                                if ( intendedHousingAmount <= 0 )
                                    break;
                                ResourceType housingType = Job.CapIncrease1.Resource;
                                if ( housingType == null )
                                    break;
                                int currentResidentsHere = (int)housingType.CalculateCurrentAmountStoredAtBuilding( effectiveHousingAmount );

                                MapActorData filthData = Structure.GetActorDataData( ActorRefs.Filth, true );
                                if ( filthData != null )
                                {
                                    if ( FlagRefs.RoboticCleaners.DuringGameplay_IsInvented )
                                    {
                                        if ( filthData.Current > 0 )
                                            filthData.SetCurrent( 0 );
                                    }
                                    else
                                    {
                                        float filthPerTurnPerResident = isRefugees ? MathRefs.FilthPerTurnPerRefugee.FloatMin : MathRefs.FilthPerTurnPerShelteredHuman.FloatMin;
                                        float filthCanceledPerDailyNecessities = MathRefs.FilthCanceledPerDailyNecessities.FloatMin;
                                        int filthToAdd = Mathf.FloorToInt( filthPerTurnPerResident * currentResidentsHere );

                                        JobHelper.HandleBadStructureDataGeneratedOrCanceled( filthData, filthToAdd, ResourceRefs.DailyNecessities,
                                            filthCanceledPerDailyNecessities, "Expense_UsedByShelteredHumans",
                                            0.5f,
                                            Structure.GetActorDataData( ActorRefs.FurnishedApartments, true ), MathRefs.MaxCleaningMultiplierFromFurnishings.IntMin );

                                        if ( FlagRefs.IsExperiencingObsession.DuringGameplay_IsTripped && !FlagRefs.RoboticCleaners.DuringGameplay_IsInvented )
                                        {
                                            if ( filthData.Current * 2 > filthData.Maximum )
                                            {
                                                FlagRefs.RoboticCleaners.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                                                OtherKeyMessageTable.Instance.GetRowByID( "TooMuchFilth" ).DuringGameplay_IsReadyToBeViewed = true;
                                            }
                                        }
                                    }
                                }

                                int housingAvailability = JobHelper.CalculateIntPercentageFromProblemRange( filthData, 0.5f, 8 );
                                if ( housingAvailability < 100 )
                                {
                                    MapActorData housingAvailabilityData = Structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.HousingAvailability, 0, 0 );

                                    housingAvailabilityData.SetOriginalMaximum( housingAvailability );
                                    housingAvailabilityData.SetCurrent( housingAvailability );

                                    Structure.Complaints.SetToConstructionDict( MachineJobComplaintTable.Instance.GetRowByID( "FilthPiledUp" ), 1 );
                                }
                                else
                                {
                                    MapActorData housingAvailabilityData = Structure.GetActorDataData( ActorRefs.HousingAvailability, true );
                                    if ( housingAvailabilityData != null )
                                    {
                                        //if at zero, that's the same as not being there at all
                                        housingAvailabilityData.SetOriginalMaximum( 0 );
                                        housingAvailabilityData.SetCurrent( 0 );
                                    }
                                }
                            }
                            break;
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "HandleFilth error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                }
            }
        }
        #endregion

        #region GetCurrentResidentsAtStructure
        public static int GetCurrentResidentsAtStructure( MachineStructure Structure )
        {
            if ( Structure == null )
                return 0;
            int effectiveHousingAmount = (int)Structure.ResourceStorageAmountHasBeenProviding1_Effective;
            if ( effectiveHousingAmount <= 0 )
                return 0;
            ResourceType housingType = Structure.CurrentJob?.CapIncrease1?.Resource;
            if ( housingType == null )
                return 0;
            return (int)housingType.CalculateCurrentAmountStoredAtBuilding( effectiveHousingAmount );
        }
        #endregion

        #region HandleVRDayUseSeats
        public static void HandleVRDayUseSeats( MachineStructure Structure, MachineJob Job, JobLogic Logic, MersenneTwister RandOrNull )
        {
            if ( FlagRefs.InitialVRSimulation.DuringGameplay_IsInvented || FlagRefs.InitialVRSimulation.DuringGame_ReadiedByInspiration != null )
            {
                int debugStage = 0;
                try
                {
                    //this stuff only happens once the appropriate part of chapter one is reached

                    switch ( Logic )
                    {
                        case JobLogic.PerQuarterSecondAggregation:
                            {
                                debugStage = 1000;
                                int currentResidents = GetCurrentResidentsAtStructure( Structure );

                                int vrSeatsMax = Job.GetSingleIntByID( "VRSeatsMax", Structure );

                                debugStage = 5000;
                                {
                                    MapActorData seatsData = Structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.VRDayUseSeats, 0, 0 );

                                    bool isHavingABadTime = !FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented && FlagRefs.HadEarlyDeathInTheVRSimulation.DuringGameplay_IsTripped;
                                    if ( isHavingABadTime )
                                        currentResidents /= 4;

                                    int desiredSeats = MathA.Min( currentResidents, vrSeatsMax );
                                    if ( desiredSeats > seatsData.Maximum )
                                        seatsData.SetOriginalMaximum( desiredSeats );

                                    if ( seatsData.LostFromMax > 0 && ResourceRefs.FullDiveVRCradle.Current > 0 )
                                    {
                                        int amountToTransfer = MathA.Min( seatsData.LostFromMax, (int)ResourceRefs.FullDiveVRCradle.Current );
                                        seatsData.AlterCurrent( amountToTransfer );
                                        ResourceRefs.FullDiveVRCradle.AlterCurrent_Named( -amountToTransfer, "Expense_DistributedToStructure", ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                }
                            }
                            break;
                        case JobLogic.ExecuteLogic:
                            if ( SimCommon.GetIsVirtualRealityEstablished() )
                            {
                                MapActorData seatsData = Structure.GetActorDataData( ActorRefs.VRDayUseSeats, true );
                                if ( seatsData != null && seatsData.Current > 0 )
                                {
                                    int hoursPerFilledVRSeat = Job.GetSingleIntByID( "HoursPerFilledVRSeat", Structure );

                                    bool isHavingABadTime = !FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented && FlagRefs.HadEarlyDeathInTheVRSimulation.DuringGameplay_IsTripped;
                                    if ( isHavingABadTime )
                                        hoursPerFilledVRSeat /= 4;

                                    int totalHours = hoursPerFilledVRSeat * seatsData.Current;

                                    if ( totalHours > 0 ) 
                                    {
                                        MapActorData neuralData = Structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.NeuralExpansion, 0, 0 );
                                        neuralData.IsOverridingCalculatedStyle = true;

                                        int totalNeuralExpansion = Mathf.RoundToInt( MathRefs.NeuralExpansionPerDayUseHour.FloatMin * totalHours );
                                        if ( totalNeuralExpansion < 1 )
                                            totalNeuralExpansion = 1;

                                        neuralData.SetOriginalMaximum( totalNeuralExpansion );
                                        neuralData.SetCurrentSilently_BeVeryCarefulWithThis( totalNeuralExpansion );

                                        CityStatisticTable.AlterScore( "VRDayUseHoursLogged", totalHours );
                                    }
                                }
                            }
                            break;
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "HandleVRDayUseSeats error: " + Logic + " " + (Job?.ID ?? "[null]"), debugStage, e, Verbosity.ShowAsError );
                }
            }
        }
        #endregion
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Projects_ChapterOne : IProjectHandlerImplementation
    {
        public void HandleLogicForProjectOutcome( ProjectLogic Logic, ArcenCharacterBufferBase BufferOrNull, MachineProject Project, ProjectOutcome OutcomeOrNoneYet,
            MersenneTwister RandOrNull, out bool CanBeCompletedNow )
        {
            CanBeCompletedNow = false;
            if ( Project == null )
            {
                ArcenDebugging.LogSingleLine( "Null project outcome passed to Projects_ChapterOne!", Verbosity.ShowAsError );
                return;
            }

            try
            {

                switch ( Project.ID )
                {
                    #region Ch1_TentElimination
                    case "Ch1_TentElimination":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType requiredResourceCap = ResourceRefs.ShelteredHumans;
                            int current = (int)requiredResourceCap.Current;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), requiredResourceCap.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    BufferOrNull.AddBoldLangAndAfterLineItemHeader( "CurrentHousingProvided", ColorTheme.DataLabelWhite )
                                        .AddRaw( requiredResourceCap.HardCap.ToStringThousandsWhole(), ColorTheme.DataBlue ).Line();
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_Flamethrower
                    case "Ch1_Flamethrower":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int tentGoal = OutcomeOrNoneYet.GetSingleIntByID( "TentGoal", 50 );
                            int buildingGoal = OutcomeOrNoneYet.GetSingleIntByID( "BuildingGoal", 0 );
                            int currentTentsBurned = (int)CityStatisticTable.GetScore( "SmallStructuresBurnedWithFlamethrower" );
                            int currentBuildingsExploded = (int)CityStatisticTable.GetScore( "BuildingsDemolishedByDrones" );
                            CanBeCompletedNow = currentTentsBurned >= tentGoal && currentBuildingsExploded >= buildingGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    if ( buildingGoal > 0 )
                                        ProjectHelper.WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, currentTentsBurned, tentGoal, currentBuildingsExploded, buildingGoal, BufferOrNull );
                                    else
                                        ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentTentsBurned, tentGoal, BufferOrNull );

                                    if ( OutcomeOrNoneYet != null && OutcomeOrNoneYet.ParentProject.DuringGame_IntendedOutcome == OutcomeOrNoneYet && !OutcomeOrNoneYet.DuringGameplay_HasDoneAnyNeededInitialization )
                                    {
                                        OutcomeOrNoneYet.DuringGameplay_HasDoneAnyNeededInitialization = true;
                                        CommonRefs.CombatUnitUnitType.DuringGameData.TryAssignAbilityToFirstOpenSlotIfNotAlreadyEquipped( AbilityRefs.Flamethrower );
                                        if ( OutcomeOrNoneYet.ShortID == "More" )
                                            CommonRefs.TechnicianUnitType.DuringGameData.TryAssignAbilityToFirstOpenSlotIfNotAlreadyEquipped( AbilityRefs.Demolish );
                                    }
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.StartNoBr().AddLangAndAfterLineItemHeader( "Stat_Name_SmallStructuresBurnedWithFlamethrower" ).AddFormat2( "OutOF", currentTentsBurned, tentGoal ).EndNoBr();
                                    if ( buildingGoal > 0 )
                                        BufferOrNull.Space3x().StartNoBr().AddLangAndAfterLineItemHeader( "Stat_Name_BuildingsDemolishedByDrones" )
                                            .AddFormat2( "OutOF", currentBuildingsExploded.ToStringThousandsWhole(), buildingGoal.ToStringThousandsWhole() ).EndNoBr();                                    
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "Stat_Name_SmallStructuresBurnedWithFlamethrower" ).AddFormat2( "OutOF", currentTentsBurned, tentGoal ).Line();
                                    if ( buildingGoal > 0 )
                                        BufferOrNull.StartNoBr().AddLangAndAfterLineItemHeader( "Stat_Name_BuildingsDemolishedByDrones" )
                                            .AddFormat2( "OutOF", currentBuildingsExploded.ToStringThousandsWhole(), buildingGoal.ToStringThousandsWhole() ).EndNoBr().Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_FasterMicrobuilders
                    case "Ch1_MIN_FasterMicrobuilders":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int targetMicrobuilderJobs = OutcomeOrNoneYet.GetSingleIntByID( "Microbuilders", 8 );
                            int targetSlurrySpiderJobs = OutcomeOrNoneYet.GetSingleIntByID( "SlurrySpiders", 20 );
                            MachineJob slurrySpiderJob = JobRefs.SlurrySpiders;
                            MachineJob microbuilderJob = JobRefs.MicrobuilderMiniFab;
                            int currentSlurrySpiders = slurrySpiderJob.DuringGame_NumberFunctional.Display;
                            int currentMicrobuilderJobs = microbuilderJob.DuringGame_NumberFunctional.Display;
                            CanBeCompletedNow = currentMicrobuilderJobs >= targetMicrobuilderJobs && currentSlurrySpiders >= targetSlurrySpiderJobs;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 
                                        Mathf.Min( currentSlurrySpiders, targetSlurrySpiderJobs ) + Mathf.Min( currentMicrobuilderJobs, targetMicrobuilderJobs ), 
                                        targetSlurrySpiderJobs + targetMicrobuilderJobs, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentSlurrySpiders.ToStringThousandsWhole(), targetSlurrySpiderJobs.ToStringThousandsWhole(), slurrySpiderJob.GetDisplayName() ).Space3x();
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentMicrobuilderJobs.ToStringThousandsWhole(), targetMicrobuilderJobs.ToStringThousandsWhole(), microbuilderJob.GetDisplayName() )
                                        .Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentSlurrySpiders.ToStringThousandsWhole(), targetSlurrySpiderJobs.ToStringThousandsWhole(), slurrySpiderJob.GetDisplayName() ).Line();
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentMicrobuilderJobs.ToStringThousandsWhole(), targetMicrobuilderJobs.ToStringThousandsWhole(), microbuilderJob.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_BuildAStorageBunker
                    case "Ch1_MIN_BuildAStorageBunker":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob bunkerJob = JobRefs.StorageBunker;
                            int currentBunkers = bunkerJob.DuringGame_NumberFunctional.Display;
                            CanBeCompletedNow = currentBunkers > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentBunkers, 1, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentBunkers.ToStringThousandsWhole(), 1, bunkerJob.GetDisplayName() )
                                        .Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", currentBunkers.ToStringThousandsWhole(), 1, bunkerJob.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_WeaponsAndArmor
                    case "Ch1_MIN_WeaponsAndArmor":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int targetWeaponsStolen = OutcomeOrNoneYet.GetSingleIntByID( "WeaponsStolen", 8 );
                            int targetArmorStolen = OutcomeOrNoneYet.GetSingleIntByID( "ArmorStolen", 10 );
                            int currentStolenWeapons = (int)(CityStatisticTable.Instance.GetRowByID( "StolenWeapons" )?.GetScore() ?? 0);
                            int currentStolenArmor = (int)(CityStatisticTable.Instance.GetRowByID( "StolenArmor" )?.GetScore()??0);
                            CanBeCompletedNow = currentStolenWeapons >= targetWeaponsStolen && currentStolenArmor >= targetArmorStolen;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet,
                                        Mathf.Min( currentStolenWeapons, targetWeaponsStolen ) + Mathf.Min( currentStolenArmor, targetArmorStolen ),
                                        targetWeaponsStolen + targetArmorStolen, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredPrefixedAmount", currentStolenWeapons.ToStringThousandsWhole(), targetWeaponsStolen.ToStringThousandsWhole(), CityStatisticTable.Instance.GetRowByID( "StolenWeapons" )?.GetDisplayName() ).Space3x();
                                    BufferOrNull.AddFormat3( "RequiredPrefixedAmount", currentStolenArmor.ToStringThousandsWhole(), targetArmorStolen.ToStringThousandsWhole(), CityStatisticTable.Instance.GetRowByID( "StolenArmor" )?.GetDisplayName() )
                                        .Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredPrefixedAmount", currentStolenWeapons.ToStringThousandsWhole(), targetWeaponsStolen.ToStringThousandsWhole(), CityStatisticTable.Instance.GetRowByID( "StolenWeapons" )?.GetDisplayName() ).Line();
                                    BufferOrNull.AddFormat3( "RequiredPrefixedAmount", currentStolenArmor.ToStringThousandsWhole(), targetArmorStolen.ToStringThousandsWhole(), CityStatisticTable.Instance.GetRowByID( "StolenArmor" )?.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_Neuroweave
                    case "Ch1_MIN_Neuroweave":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob neuroweaveJob = JobRefs.NeuroweaveFactory;
                            int current = neuroweaveJob.DuringGame_NumberFunctional.Display;
                            int target = 4;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current,
                                        target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredJobAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), neuroweaveJob.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_CommandMode
                    case "Ch1_MIN_CommandMode":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "AndroidsCreatedViaCommandMode" );

                            int currentReal = (int)statistic.GetScore();
                            int targetReal = 1;

                            int currentExaggerated = currentReal * 100;
                            int targetExaggerated = 100;
                            
                            if ( currentExaggerated >= targetExaggerated)
                            {
                                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                                    currentExaggerated = 99;
                            }

                            CanBeCompletedNow = currentExaggerated >= targetExaggerated;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentExaggerated,
                                        targetExaggerated, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredPrefixedAmount", currentReal.ToStringThousandsWhole(), targetReal.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_FindingFood
                    case "Ch1_FindingFood":
                        if ( OutcomeOrNoneYet != null )
                        {
                            if ( OutcomeOrNoneYet.ShortID == "Cannery" )
                            {
                                int cannedGoal = OutcomeOrNoneYet.GetSingleIntByID( "CannedProteinGoal", 0 );
                                ResourceType cannedResource = ResourceRefs.CannedProtein;
                                int currentCanned = (int)cannedResource.GetActualTrendWithLieIfStorageAtLeast( cannedGoal, 1000000 );
                                CanBeCompletedNow = currentCanned >= cannedGoal;

                                switch ( Logic )
                                {
                                    case ProjectLogic.WriteProgressIconText:
                                    case ProjectLogic.WriteProgressTextBrief:
                                        ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentCanned, cannedGoal, BufferOrNull );
                                        break;
                                    case ProjectLogic.WriteRequirements_OneLine:
                                    case ProjectLogic.WriteRequirements_ManyLines:
                                        if ( cannedGoal > 0 )
                                            BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentCanned.ToStringThousandsWhole(), cannedGoal.ToStringThousandsWhole(), cannedResource.GetDisplayName() );

                                        BufferOrNull.Line();
                                        break;
                                    case ProjectLogic.WriteAddedContext:
                                        break;
                                    case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                        break;
                                }
                            }
                            else
                            {
                                int greensGoal = OutcomeOrNoneYet.GetSingleIntByID( "GreensGoal", 0 );
                                int meatGoal = OutcomeOrNoneYet.GetSingleIntByID( "MeatGoal", 0 );
                                ResourceType greensResource = ResourceRefs.HydroponicGreens;
                                ResourceType meatResource = ResourceRefs.VatGrownMeat;
                                int currentGreens = (int)greensResource.GetActualTrendWithLieIfStorageAtLeast( greensGoal, 1000000 );
                                int currentMeat = (int)meatResource.GetActualTrendWithLieIfStorageAtLeast( meatGoal, 2000000 );
                                CanBeCompletedNow = currentMeat >= meatGoal && currentGreens >= greensGoal;

                                switch ( Logic )
                                {
                                    case ProjectLogic.WriteProgressIconText:
                                    case ProjectLogic.WriteProgressTextBrief:
                                        ProjectHelper.WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, currentGreens, greensGoal, currentMeat, meatGoal, BufferOrNull );
                                        break;
                                    case ProjectLogic.WriteRequirements_OneLine:
                                        if ( greensGoal > 0 )
                                            BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentGreens.ToStringThousandsWhole(), greensGoal.ToStringThousandsWhole(), greensResource.GetDisplayName() );
                                        if ( meatGoal > 0 )
                                        {
                                            if ( greensGoal > 0 )
                                                BufferOrNull.Space5x();
                                            BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentMeat.ToStringThousandsWhole(), meatGoal.ToStringThousandsWhole(), meatResource.GetDisplayName() );
                                        }

                                        BufferOrNull.Line();
                                        break;
                                    case ProjectLogic.WriteRequirements_ManyLines:
                                        if ( greensGoal > 0 )
                                            BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentGreens.ToStringThousandsWhole(), greensGoal.ToStringThousandsWhole(), greensResource.GetDisplayName() ).Line();
                                        if ( meatGoal > 0 )
                                            BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentMeat.ToStringThousandsWhole(), meatGoal.ToStringThousandsWhole(), meatResource.GetDisplayName() ).Line();
                                        break;
                                    case ProjectLogic.WriteAddedContext:
                                        break;
                                    case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                        break;
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_FindingWater
                    case "Ch1_FindingWater":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType waterResource = ResourceRefs.FilteredWater;
                            int current = (int)waterResource.GetActualTrendWithLieIfStorageAtLeast( target, 2000000 );
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), waterResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_PrismTung
                    case "Ch1_MIN_PrismTung":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType resource = ResourceTypeTable.Instance.GetRowByID( "PrismaticTungsten" );
                            int resourceCount = (int)resource.Current;
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 30 );

                            CanBeCompletedNow = resourceCount >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, resourceCount, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.StartNoBr().AddRawAndAfterLineItemHeader( resource.GetDisplayName() )
                                            .AddFormat2( "OutOF", resourceCount.ToStringThousandsWhole(), target.ToStringThousandsWhole() ).EndNoBr();

                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_ManArmorPierce
                    case "Ch1_MIN_ManArmorPierce":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int bestStack = 0;
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                int stack = actor.GetStackCountOfStatus( StatusRefs.TemporaryArmorPiercing );
                                if ( stack > bestStack )
                                    bestStack = stack;

                                if ( stack >= 3 )
                                    break;
                            }

                            int target = 3;
                            CanBeCompletedNow = bestStack >= 3;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, bestStack, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddRawAndAfterLineItemHeader( StatusRefs.TemporaryArmorPiercing.GetDisplayName() )
                                            .AddFormat2( "OutOF", bestStack.ToStringThousandsWhole(), target.ToStringThousandsWhole() );

                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_ImprovedApartments
                    case "Ch1_ImprovedApartments":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int dailyGoal =
                                Mathf.CeilToInt( ((int)ResourceRefs.ShelteredHumans.Current * MathRefs.FilthPerTurnPerShelteredHuman.FloatMin) / MathRefs.FilthCanceledPerDailyNecessities.FloatMin ) +
                                Mathf.CeilToInt( ((int)ResourceRefs.RefugeeHumans.Current * MathRefs.FilthPerTurnPerRefugee.FloatMin) / MathRefs.FilthCanceledPerDailyNecessities.FloatMin );

                            ActorDataType furnishedApartmentData = ActorDataTypeTable.Instance.GetRowByID( "FurnishedApartments" );

                            bool usesFurniture = false;
                            switch ( OutcomeOrNoneYet.ShortID )
                            {
                                case "Both":
                                    usesFurniture = true;
                                    break;
                            }

                            int furnitureGoal = 0;
                            int furnitureProvided = 0;

                            if ( usesFurniture )
                            {
                                foreach ( MachineStructure structure in furnishedApartmentData.DuringGameplay_StructuresUsingThis.GetDisplayList() )
                                {
                                    MapActorData furnitureData = structure.GetActorDataData( furnishedApartmentData, true );
                                    if ( furnitureData != null )
                                    {
                                        furnitureGoal += furnitureData.Maximum;
                                        furnitureProvided += furnitureData.Current;
                                    }
                                }
                            }

                            ResourceType dailyResource = ResourceTypeTable.Instance.GetRowByID( "DailyNecessities" );
                            ResourceType furnitureResource = ResourceTypeTable.Instance.GetRowByID( "Furniture" );
                            int currentDaily = (int)dailyResource.GetActualIncome(); //not the trend, because it already subtracts from this
                            CanBeCompletedNow = furnitureProvided >= furnitureGoal && currentDaily >= dailyGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    if ( usesFurniture )
                                        ProjectHelper.WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, currentDaily, dailyGoal, furnitureProvided, furnitureGoal, BufferOrNull );
                                    else
                                        ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentDaily, dailyGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    if ( dailyGoal > 0 )
                                        BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentDaily.ToStringThousandsWhole(), dailyGoal.ToStringThousandsWhole(), dailyResource.GetDisplayName() );
                                    if ( furnitureGoal > 0 )
                                    {
                                        if ( dailyGoal > 0 )
                                            BufferOrNull.Space5x();
                                        BufferOrNull.AddFormat3( "RequiredResourceAmount", furnitureProvided.ToStringThousandsWhole(), furnitureGoal.ToStringThousandsWhole(), furnitureResource.GetDisplayName() );
                                    }

                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( dailyGoal > 0 )
                                        BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentDaily.ToStringThousandsWhole(), dailyGoal.ToStringThousandsWhole(), dailyResource.GetDisplayName() ).Line();
                                    if ( furnitureGoal > 0 )
                                        BufferOrNull.AddFormat3( "RequiredResourceAmount", furnitureProvided.ToStringThousandsWhole(), furnitureGoal.ToStringThousandsWhole(), furnitureResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_BuildingABetterBrain
                    case "Ch1_BuildingABetterBrain":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int goal = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 5000 );
                            int currentClass = SimMetagame.IntelligenceClass;
                            CanBeCompletedNow = currentClass >= goal;

                            Int64 currentNeuralProcessing = SimCommon.CurrentTimeline?.NeuralProcessing ?? 0;
                            Int64 goalNeuralProcessing = ScaleRefs.IntelligenceClass.Regular_Sorted[goal - 2].CutoffInt;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentNeuralProcessing, goalNeuralProcessing, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "RaiseYourselfToIntelligenceClass" ).AddRaw( goal.ToStringThousandsWhole() );
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "RaiseYourselfToIntelligenceClass" ).AddRaw( goal.ToStringThousandsWhole() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddLangAndAfterLineItemHeader( "NeuralProcessingForUpgrade" ).AddFormat2( "OutOF",
                                        currentNeuralProcessing.ToStringThousandsWhole(), goalNeuralProcessing.ToStringThousandsWhole() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_GrandTheftAero
                    case "Ch1_MIN_GrandTheftAero":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = FlagRefs.HasStolenADeliveryCraft.DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Acquire" ).AddRaw( CommonRefs.DeliveryCraftVehicleType.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_NovelAir
                    case "Ch1_MIN_NovelAir":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = CommonRefs.CutterVehicleType.DuringGameData.ActorsOfThisType.Count > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( CommonRefs.CutterVehicleType.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_FlexibleAirspace
                    case "Ch1_MIN_FlexibleAirspace":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = CommonRefs.BastionVehicleType.DuringGameData.ActorsOfThisType.Count > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( CommonRefs.BastionVehicleType.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_FlyingFactories
                    case "Ch1_MIN_FlyingFactories":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = CommonRefs.FoundryVehicleType.DuringGameData.ActorsOfThisType.Count > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( CommonRefs.FoundryVehicleType.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_ExponentialPowerGrowth
                    case "Ch1_MIN_ExponentialPowerGrowth":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = JobRefs.GeothermalWell.DuringGame_NumberFunctional.Display > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( JobRefs.GeothermalWell.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_Scandium
                    case "Ch1_MIN_Scandium":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType scandiumResource = ResourceTypeTable.Instance.GetRowByID( "Scandium" );
                            int current = (int)scandiumResource.Current;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), scandiumResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_Extraction
                    case "Ch1_MIN_Extraction":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = FlagRefs.HasPerformedAnExtraction.DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "PerformASuccessfulExtractionOnAnEnemy" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_SecuringAlumina
                    case "Ch1_MIN_SecuringAlumina":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType aluminaResource = ResourceTypeTable.Instance.GetRowByID( "Alumina" );
                            int current = (int)aluminaResource.Current;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), aluminaResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_SecureVRSoftware
                    case "Ch1_MIN_SecureVRSoftware":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType dataResource = ResourceTypeTable.Instance.GetRowByID( "EncryptedMilitaryVRSimData" );
                            int current = (int)dataResource.Current;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), dataResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_DecryptMilitaryData
                    case "Ch1_MIN_DecryptMilitaryData":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "DecryptedMilitaryVRSimData" );
                            int current = (int)statistic.GetScore();
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_OngoingMilitaryDecryption
                    case "Ch1_MIN_OngoingMilitaryDecryption":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType resource = ResourceTypeTable.Instance.GetRowByID( "EncryptedMilitaryVRSimData" );
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "DecryptedMilitaryVRSimData" );
                            int target = (int)statistic.GetScore() + (int)resource.Current;
                            int current = (int)statistic.GetScore();
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_PrepareVRCradles
                    case "Ch1_MIN_PrepareVRCradles":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "VRDaySeatsInstalled" );
                            int target = 2000;
                            int current = (int)statistic.GetScore();
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_IntelligenceClass3
                    case "Ch1_MIN_IntelligenceClass3":
                        if ( OutcomeOrNoneYet != null )
                        {
                            Int64 goal = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 5000 );
                            int currentClass = SimMetagame.IntelligenceClass;
                            CanBeCompletedNow = currentClass >= goal;

                            int percentage = 0;
                            Int64 neuralProcessing = 0;
                            Int64 cutoffInt = 0;
                            if ( currentClass < 3 )
                            {
                                if ( currentClass < 2 )
                                    percentage = 0;
                                else
                                {
                                    foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                                        neuralProcessing += timeline.NeuralProcessing;

                                    ISeverity nextCutoff = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );
                                    cutoffInt = nextCutoff.CutoffInt;

                                    percentage = MathA.IntPercentage( neuralProcessing, cutoffInt );
                                    if ( percentage > 99 )
                                        percentage = 99;
                                }
                            }
                            else
                                percentage = 100;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromOnePercentageNumber( Logic, OutcomeOrNoneYet, percentage, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( currentClass == 2 )
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "NeuralProcessingForUpgrade" ).AddFormat2( "OutOF",
                                            neuralProcessing.ToStringLargeNumberAbbreviated(), cutoffInt.ToStringLargeNumberAbbreviated() ).Line();
                                    }
                                    else
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "RaiseYourselfToIntelligenceClass" ).AddRaw( goal.ToString() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch1_MIN_FindARobotServant
                    case "Ch1_MIN_FindARobotServant":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CanBeCompletedNow = false;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    default:
                        if ( !Project.HasShownHandlerMissingError )
                        {
                            Project.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "Projects_ChapterOne: No handler is set up for '" + Project.ID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Project.HasShownCaughtError )
                {
                    Project.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "Projects_ChapterOne: Error in '" + Project.ID + "': " + e, Verbosity.ShowAsError );
                }
            }
        }

        public void HandleStreetItem( ProjectOutcome Outcome, ProjectOutcomeStreetSenseItem StreetItem, ISimBuilding Building, ISimMachineActor Actor, 
            MersenneTwister Rand, ArcenDoubleCharacterBuffer PopupBufferOrNull )
        {
            ArcenDebugging.LogSingleLine( "HandleStreetItem is not set up for anything in Projects_ChapterOne.", Verbosity.ShowAsError );
        }
    }
}

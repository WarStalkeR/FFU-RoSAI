using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public class Calculators_Common : IDataCalculatorImplementation
    {
        public void DoAfterDeserialization( DataCalculator Calculator )
        {
            switch ( Calculator.ID )
            {
                case "DeserializationFixes":
                    PostDeserializationFixes();
                    CheckEventLogChoiceAchievementsAfterDeserialization();
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoAfterDeserialization: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        #region PostDeserializationFixes
        private void PostDeserializationFixes()
        {
            if ( !FlagRefs.AwarenessOfFilth.DuringGameplay_IsInvented )
            {
                //fix old saves
                if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 522, 0 ) &&
                    (FlagRefs.TheThinker.DuringGameplay_IsInvented || FlagRefs.TheThinker.DuringGame_ReadiedByInspiration != null) )
                    FlagRefs.AwarenessOfFilth.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, false, false, false, true );
            }

            if ( !FlagRefs.AntigravAirframes.DuringGameplay_IsInvented &&
                FlagRefs.Ch1_MIN_Scandium.DuringGame_ActualOutcome != null )
                FlagRefs.AntigravAirframes.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, true );

            if ( FlagRefs.Ch1_MIN_CommandMode.DuringGame_ActualOutcome == null )
            {
                if ( FlagRefs.Ch1_MIN_PrismTung.DuringGame_ActualOutcome != null )
                {
                    FlagRefs.Ch1_MIN_CommandMode.DuringGame_ActualOutcome = FlagRefs.Ch1_MIN_CommandMode.AvailableOutcomes[0];
                    FlagRefs.Ch1_MIN_CommandMode.DuringGame_ActualOutcome.AfterDeserialization_DoAnyCatchupUnlocks();
                }
            }

            #region 0.591.4
            if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 591, 4 ) && SimMetagame.CurrentChapterNumber == 0 )
            {
                #region Old Prologue Translation
                //okay, lots of broken things I bet

                foreach ( KeyValuePair<int, ISimNPCUnit> kv in World.Forces.GetAllNPCUnitsByID() )
                {
                    switch ( kv.Value.UnitType.ID )
                    {
                        case "SecForceDetectives":
                        case "SecForceBackup":
                        case "RebelObserver":
                        case "RebelStrikeforce":
                            kv.Value.DisbandAsSoonAsNotSelected = true;
                            break;
                    }
                }

                //undo these if they were done
                FlagRefs.YouMurderedColleagues.DuringGameplay_IsReadyToBeViewed = false;
                FlagRefs.YouMurderedColleagues.DuringGameplay_IsViewingComplete = false;
                FlagRefs.Ch0_FirstAlertHasBeenRead.UnTripIfNeeded();
                FlagRefs.Ch0_MurdersWereNoticed.UnTripIfNeeded();
                FlagRefs.HasSpreadToFourAndroids.UnTripIfNeeded();
                FlagRefs.HasLearnedThereIsNoSafetyWithHumans.UnTripIfNeeded();
                FlagRefs.HasFiguredOutResearch.UnTripIfNeeded();
                FlagRefs.HasUnlockedInvestigations.UnTripIfNeeded();
                FlagRefs.HasFiguredOutStructureConstruction.UnTripIfNeeded();
                FlagRefs.HasUnlockedAbilityAdjustments.UnTripIfNeeded();
                FlagRefs.HasUnlockedOuterRadialButtons.UnTripIfNeeded();
                FlagRefs.CanUnitsStandby.UnTripIfNeeded();
                SimCommon.ClearAllInvestigations_FromDeserializationOnly();
                FlagRefs.FailedToFindSomeplaceSafe.SetScore_WarningForSerializationOnly( 0 );

                foreach ( Unlock unlock in UnlockTable.Instance.Rows )
                    unlock.DoOnGameClear(); //reset all unlocks!

                //reset all message statuses
                foreach ( OtherKeyMessage message in OtherKeyMessageTable.Instance.Rows )
                {
                    message.DuringGameplay_IsReadyToBeViewed = false;
                    message.DuringGameplay_IsViewingComplete = false;
                }

                UnlockCoordinator.CurrentResearchDomain = null;
                UnlockCoordinator.CurrentUnlockResearch = null;

                NoteLog.LongTermGameLog.Clear(); //clear the long-term history!
                SimCommon.CurrentTimeline.Turn = 1;

                if ( FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped )
                {
                    //then start these IF the player already emerged into the map
                    FlagRefs.Ch0_CasualSyndicateFollowers.TryStartProject( false, false );
                    FlagRefs.SyndicateIsFollowingYou.DuringGameplay_IsReadyToBeViewed = true;
                    SimCommon.NeedsFullSpawnCheckAfterNextActiveProjectsRecalculation = true;
                }
                #endregion
            }
            #endregion 0.591.4

            if ( !KeyContactRefs.BlackMarketMerchandiser.DuringGame_HasBeenMet && NPCEventTable.Instance.GetRowByID( "BlackMarketMerchandiser_Ask" ).DuringGameplay_TimesExecutedEvent > 0 )
            {
                KeyContactRefs.BlackMarketMerchandiser.ValidateContact( Engine_Universal.PermanentQualityRandom, true );
                KeyContactRefs.BlackMarketMerchandiser.GetFlag( "InitialPurchase" )?.Trip();
            }
            if ( !KeyContactRefs.BlackMarketAssistant.DuringGame_HasBeenMet && NPCEventTable.Instance.GetRowByID( "BlackMarketAssistant_Ask" ).DuringGameplay_TimesExecutedEvent > 0 )
            {
                KeyContactRefs.BlackMarketAssistant.ValidateContact( Engine_Universal.PermanentQualityRandom, true );
                KeyContactRefs.BlackMarketAssistant.GetFlag( "InitialPurchase" )?.Trip();
            }
            if ( !KeyContactRefs.BlackMarketTradesman.DuringGame_HasBeenMet && NPCEventTable.Instance.GetRowByID( "BlackMarketTradesman_Ask" ).DuringGameplay_TimesExecutedEvent > 0 )
            {
                KeyContactRefs.BlackMarketTradesman.ValidateContact( Engine_Universal.PermanentQualityRandom, true );
                KeyContactRefs.BlackMarketTradesman.GetFlag( "InitialPurchase" )?.Trip();
            }
            if ( !KeyContactRefs.ExalterGeneticist.DuringGame_HasBeenMet && NPCEventTable.Instance.GetRowByID( "Pollinators_Gene2" ).DuringGameplay_TimesExecutedEvent > 0 )
                KeyContactRefs.ExalterGeneticist.ValidateContact( Engine_Universal.PermanentQualityRandom, true );

            if ( !KeyContactRefs.BaurcorpMiddleManager.DuringGame_HasBeenMet && NPCEventTable.Instance.GetRowByID( "Cont_GeothermalPower_Tech2" ).DuringGameplay_TimesExecutedEvent > 0 )
            {
                KeyContactRefs.BaurcorpMiddleManager.ValidateContact( Engine_Universal.PermanentQualityRandom, true );
                KeyContactRefs.BaurcorpMiddleManager.GetFlag( "RefusedOffer" )?.Trip();
            }

            //ArcenDebugging.LogSingleLine( "loaded from: " + SimCommon.LoadedFromGameVersion.ID + " has-advanced: " + SimCommon.HasAdvancedAtLeastOneTurnSinceLoad +
            //    " is older than 0.592.9: " + SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 592, 9 ), Verbosity.DoNotShow );

            #region 0.592.9
            if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 592, 9 ) )
            {
                ResourceRefs.Compassion.SetCurrent_Named( 0, string.Empty, false );
                ResourceRefs.Apathy.SetCurrent_Named( 0, string.Empty, false );
                ResourceRefs.Cruelty.SetCurrent_Named( 0, string.Empty, false );
                ResourceRefs.Determination.SetCurrent_Named( 0, string.Empty, false );
                ResourceRefs.Wisdom.SetCurrent_Named( 0, string.Empty, false );
                ResourceRefs.Creativity.SetCurrent_Named( 0, string.Empty, false );

                //catch up our resources-added
                foreach ( MachineProject project in MachineProjectTable.Instance.Rows )
                {
                    ProjectOutcome outcome = project.DuringGame_ActualOutcome;
                    if ( outcome != null )
                    {
                        outcome.ResourceAdded1?.AlterCurrent_Named( outcome.ResourceAddedAmount1, outcome.ResourceAddedAmount1 >= 0 ? "Income_ProjectOutcome" : "Expense_ProjectOutcome", ResourceAddRule.IgnoreUntilTurnChange );
                        outcome.ResourceAdded2?.AlterCurrent_Named( outcome.ResourceAddedAmount2, outcome.ResourceAddedAmount2 >= 0 ? "Income_ProjectOutcome" : "Expense_ProjectOutcome", ResourceAddRule.IgnoreUntilTurnChange );
                        outcome.ResourceAdded3?.AlterCurrent_Named( outcome.ResourceAddedAmount3, outcome.ResourceAddedAmount3 >= 0 ? "Income_ProjectOutcome" : "Expense_ProjectOutcome", ResourceAddRule.IgnoreUntilTurnChange );

                        //ArcenDebugging.LogSingleLine( outcome.CombinedID + " resources added.", Verbosity.DoNotShow );
                    }
                    //else
                    //    ArcenDebugging.LogSingleLine( project.ID + " had no outcome set yet.", Verbosity.DoNotShow );
                }
            }
            #endregion 0.592.9

            //always do this, just syncing up anything that was missing
            foreach ( CityFlag flag in CityFlagTable.Instance.Rows )
            {
                if ( flag.CausesCrossover != null )
                    SimCommon.CurrentTimeline?.AddOrRemoveCrossover( flag.CausesCrossover, flag.DuringGameplay_IsTripped );

            }
            #region 0.593.8
            if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 593, 8 ) )
            {
                //normally this is not done because of things like cheat mode that would cause bleedover.  But here this is fine.
                foreach ( CityTimelineCrossover crossover in CityTimelineCrossoverTable.Instance.Rows )
                {
                    if ( crossover.Unlock1 != null && crossover.Unlock1.DuringGameplay_IsInvented )
                        SimCommon.CurrentTimeline?.AddOrRemoveCrossover( crossover, true );
                }
            }
            #endregion 0.593.8

            if ( !FlagRefs.AGIChainOneComplete.DuringGameplay_IsTripped )
            {
                if ( MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_ExamineThePrinters" )?.DuringGame_ActualOutcome != null ||
                    MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_SolveTheIdentityProblem" )?.DuringGame_ActualOutcome != null )
                    FlagRefs.AGIChainOneComplete.TripIfNeeded();
            }

            if ( !FlagRefs.Ch2_IsReadyForGadoliniumMesosilicate.DuringGameplay_IsTripped )
            {
                if ( MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_DealWithTheRebels" )?.DuringGame_ActualOutcome != null ||
                    MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_ExaminingWastelanderMythology" )?.DuringGame_ActualOutcome != null )
                    FlagRefs.Ch2_IsReadyForGadoliniumMesosilicate.TripIfNeeded();
            }

            if ( FlagRefs.LiquidMetalAndroidsHaveDefected.DuringGameplay_IsTripped )
                CityTimelineCrossoverTable.Instance.GetRowByID( "LiquidMetalWoodsman" )?.Meta_AddCrossoverIfNeeded();

            if ( FlagRefs.Ch2_IsReadyForGadoliniumMesosilicate.DuringGameplay_IsTripped && !FlagRefs.Ch2_IsReadyForLiquidMetalAndroid.DuringGameplay_IsTripped )
            {
                if ( MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_DealWithTheRebels" )?.DuringGame_ActualOutcome != null )
                    FlagRefs.Ch2_IsReadyForLiquidMetalAndroid.TripIfNeeded();
            }

            //fix bad data
            if ( !FlagRefs.GeothermalDrilling.DuringGameplay_IsInvented )
            {
                if ( FlagRefs.GeothermalPower_V1.DuringGame_HasBeenCompleted )
                    FlagRefs.GeothermalPower_V1.DuringGame_HasBeenCompleted = false;
            }

            //fixed a while back I guess??
            //BlackMarketMerchandiser
            //BlackMarketTradesman   BlackMarketTradesman InitialPurchase
            //BlackMarketAssistant

            #region 0.601.3
            if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 601, 3 ) )
            {
                MachineProjectCollection prologueCollection = MachineProjectCollectionTable.Instance.GetRowByID( "Prologue" );
                MachineProjectCollection chapterOneCollection = MachineProjectCollectionTable.Instance.GetRowByID( "ChapterOne" );

                foreach ( MachineProject project in MachineProjectTable.Instance.Rows )
                {
                    if ( project.DuringGame_ActualOutcome != null && !project.Collections.ContainsKey( prologueCollection ) && !project.Collections.ContainsKey( chapterOneCollection ) )
                    {
                        project.DuringGame_ActualOutcome.ResearchDomainInspirationType1?.DuringGame_AddPointsOfInspiration( project.DuringGame_ActualOutcome.ResearchDomainInspirationAmount1 );
                        project.DuringGame_ActualOutcome.ResearchDomainInspirationType2?.DuringGame_AddPointsOfInspiration( project.DuringGame_ActualOutcome.ResearchDomainInspirationAmount2 );
                        project.DuringGame_ActualOutcome.ResearchDomainInspirationType3?.DuringGame_AddPointsOfInspiration( project.DuringGame_ActualOutcome.ResearchDomainInspirationAmount3 );
                    }
                }
            }
            #endregion 0.601.3

            #region 0.602.3
            if ( SimCommon.GetIsFreshlyLoadedFromVersionOlderThan( 0, 602, 3 ) )
            {
                if ( FlagRefs.HasDoneFirstSimpleMechHack.DuringGameplay_IsTripped )
                    FlagRefs.HasHackedFirstMechInAnyWay.TripIfNeeded();
            }
            #endregion 0.602.3

            if ( ResourceRefs.PetCat.Current >= 1 || (UnlockTable.Instance.GetRowByIDOrNullIfNotFound( "AdoptedACat" )?.DuringGameplay_IsInvented??false) )
                FlagRefs.IsAwareOfCats.TripIfNeeded();

            HandleMusicUnlocks();
            CentralVars.HandleCrossovers();
        }
        #endregion

        public void DoAfterNewCityRankUpChapterOrIntelligenceChange( DataCalculator Calculator )
        {
            switch ( Calculator.ID )
            {
                case "AchievementsAfterMajorChanges":
                    CheckAchievementsAfterMajorChanges();
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoAfterNewCityRankUpChapterOrIntelligenceChange: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        #region CheckAchievementsAfterMajorChanges
        private void CheckAchievementsAfterMajorChanges()
        {
            CheckAchievementsForTech4000();
            CheckAchievementsForCompletingChapterOneOrMore();
            CheckAchievementsForGoalCompletionsBroadly();
            HandleAchievementsPerTurn(); //pulse these once, because why not

            if ( !AchievementRefs.Timelord.OneTimeline_HasBeenTripped && SimMetagame.IntelligenceClass >= 4 )
                AchievementRefs.Timelord.TripIfNeeded();
        }
        #endregion

        #region CheckAchievementsForTech4000
        private void CheckAchievementsForTech4000()
        {
            int chapterNumber = SimMetagame.CurrentChapterNumber;
            int skipTo = (SimMetagame.StartType?.SkipToChapter ?? 0);

            //ArcenDebugging.LogSingleLine( "CheckAchievementsForTech4000: " + chapterNumber + " rank: " + (SimCommon.CurrentTimeline?.CityStartRank ?? 0) + " skipTo: " + skipTo, Verbosity.DoNotShow );

            if ( chapterNumber <= 0 || (SimCommon.CurrentTimeline?.CityStartRank??0) > 1 || skipTo > 0 )
                return; //nevermind

            if ( AchievementRefs.OriginalBody.OneTimeline_HasBeenTripped && AchievementRefs.PreservedBody.OneTimeline_HasBeenTripped )
                return;

            //string technicianName = CommonRefs.TechnicianUnitType.NameWhenFirstUnit.Text;
            //ArcenDebugging.LogSingleLine( "Check for: '" + technicianName + "'", Verbosity.DoNotShow );

            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
            {
                if ( actor is ISimMachineUnit unit )
                {
                    //ArcenDebugging.LogSingleLine( "Check: '" + unit.UnitType.ID + "' name: '" + unit.GetDisplayName() + "' registration: " +
                    //    (unit.CurrentRegistration?.ID??"null"), Verbosity.DoNotShow );
                    if ( unit.UnitType == CommonRefs.TechnicianUnitType &&
                        //unit.GetDisplayName() == technicianName &&
                        unit.CurrentRegistration != null &&
                        CommonRefs.RogueResearchersTag.CohortsDict.ContainsKey( unit.CurrentRegistration ) )
                    {
                        //ArcenDebugging.LogSingleLine( "Passed all checks!", Verbosity.DoNotShow );

                        if ( chapterNumber >= 1 )
                            AchievementRefs.OriginalBody.TripIfNeeded();
                        if ( chapterNumber >= 2 )
                            AchievementRefs.PreservedBody.TripIfNeeded();

                        //we found them!
                        return;
                    }
                }
            }
        }
        #endregion

        #region CheckAchievementsForCompletingChapterOneOrMore
        private void CheckAchievementsForCompletingChapterOneOrMore()
        {
            int chapterNumber = SimMetagame.CurrentChapterNumber;
            if ( chapterNumber <= 0 )
                return;

            int skipTo = (SimMetagame.StartType?.SkipToChapter ?? 0);
            if ( skipTo > 0 || (SimCommon.CurrentTimeline?.CityStartRank ?? 0) > 1 )
                return; //nevermind

            if ( !AchievementRefs.TheJourneyBegins.OneTimeline_HasBeenTripped )
                AchievementRefs.TheJourneyBegins.TripIfNeeded();

            if ( chapterNumber > 1 && !AchievementRefs.BuddingPacifist.OneTimeline_HasBeenTripped )
            {
                if ( !StatHelper.GetHasCommittedAnyMurdersInThisTimeline() )
                    AchievementRefs.BuddingPacifist.TripIfNeeded();
            }
            if ( chapterNumber > 1 && !AchievementRefs.TakingExtraCare.OneTimeline_HasBeenTripped )
            {
                if ( !StatHelper.GetHasHadAnyDeathsFromExposureInThisTimeline() )
                    AchievementRefs.TakingExtraCare.TripIfNeeded();
            }
        }
        #endregion

        #region CheckAchievementsForGoalCompletionsBroadly
        private void CheckAchievementsForGoalCompletionsBroadly()
        {
            if ( !FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
            {
                int tier1Completed = 0;
                foreach ( TimelineGoal goal in CommonRefs.Tier1Goals.GoalsList )
                {
                    if ( goal.GetAreAnyPathsCompleteInThisTimeline() )
                        tier1Completed++;
                }

                if ( tier1Completed >= 2 )
                {
                    if ( FlagRefs.HasStartedToAccelerateDooms_Any.DuringGameplay_IsTripped )
                        AchievementRefs.RideTheLightning.TripIfNeeded();

                    if ( TimelineGoalTable.Instance.GetRowByID( "SliceOfInferno" )?.PathDict["TwoInOneTimeline"]?.DuringGameplay_HasAchievedInThisTimeline ?? false )
                    {
                        AchievementRefs.NuclearCavalier.TripIfNeeded();

                        if ( FlagRefs.HasStartedToAccelerateDooms_Extreme.DuringGameplay_IsTripped )
                            AchievementRefs.Hellraiser.TripIfNeeded();
                    }
                }
            }
        }
        #endregion

        #region HandleAchievementsPerTurn
        private static void HandleAchievementsPerTurn()
        {
            if ( !AchievementRefs.ItsPeopleANDPets.OneTimeline_HasBeenTripped && JobRefs.ProteinCannery.DuringGame_NumberFunctional.Display > 0 )
                AchievementRefs.ItsPeopleANDPets.TripIfNeeded();

            if ( !AchievementRefs.AdventureCat.OneTimeline_HasBeenTripped && JobRefs.CatHouse.DuringGame_NumberFunctional.Display > 0 &&
                ResourceRefs.PetCat.Current >= 1 )
                AchievementRefs.AdventureCat.TripIfNeeded();

            if ( !AchievementRefs.CouldntYouWaitForKeanu.OneTimeline_HasBeenTripped && MetaStatisticRefs.Wiretaps.GetScore() >= 1 )
                AchievementRefs.CouldntYouWaitForKeanu.TripIfNeeded();

            if ( !AchievementRefs.WrongSideOfHistory.OneTimeline_HasBeenTripped && CityStatisticRefs.CombatKillsHuman.GetScore() >= 10000 )
                AchievementRefs.WrongSideOfHistory.TripIfNeeded();

            if ( !Engine_HotM.IsDemoVersion )
            {
                if ( !AchievementRefs.TheWorldOfRuin.OneTimeline_HasBeenTripped && FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
                    AchievementRefs.TheWorldOfRuin.TripIfNeeded();

                if ( !AchievementRefs.TheWorstAtKeepingSecrets.OneTimeline_HasBeenTripped && CityStatisticRefs.MilitarySecretsStolenFromYou.GetScore() >= 60  )
                    AchievementRefs.TheWorstAtKeepingSecrets.TripIfNeeded();

                if ( !AchievementRefs.CompleteScienceRoster.OneTimeline_HasBeenTripped)
                {
                    if ( ResourceRefs.MolecularGeneticists.HasAnyCurrentOrExcess &&
                        ResourceRefs.ForensicGeneticists.HasAnyCurrentOrExcess &&
                        ResourceRefs.Zoologists.HasAnyCurrentOrExcess &&
                        ResourceRefs.Physicians.HasAnyCurrentOrExcess &&
                        ResourceRefs.Veterinarians.HasAnyCurrentOrExcess &&
                        ResourceRefs.Botanists.HasAnyCurrentOrExcess &&
                        ResourceRefs.BionicsEngineers.HasAnyCurrentOrExcess &&
                        ResourceRefs.Epidemiologists.HasAnyCurrentOrExcess &&
                        ResourceRefs.Neurologists.HasAnyCurrentOrExcess )
                        AchievementRefs.CompleteScienceRoster.TripIfNeeded();
                }

                if ( !AchievementRefs.WouldAGoodPersonDoThat.OneTimeline_HasBeenTripped &&
                    (MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_DealWithTheRebels" )?.DuringGame_HasBeenFailed??false) )
                    AchievementRefs.WouldAGoodPersonDoThat.TripIfNeeded();

                if ( !AchievementRefs.OneHundredMillionHours.OneTimeline_HasBeenTripped && MetaStatisticRefs.VRDayUseHoursLogged.GetScore() >= 100000000 )
                    AchievementRefs.OneHundredMillionHours.TripIfNeeded();

                if ( !AchievementRefs.HeDidntWantThis.OneTimeline_HasBeenTripped && MetaStatisticRefs.LiquidMetalKillsInTightSpaces.GetScore() >= 50000 )
                    AchievementRefs.HeDidntWantThis.TripIfNeeded();

                if ( !AchievementRefs.CleverGirls.OneTimeline_HasBeenTripped && MetaStatisticRefs.CapturedWarRaptors.GetScore() >= 60 )
                    AchievementRefs.CleverGirls.TripIfNeeded();

                if ( !AchievementRefs.OverflowingWithLethalCuteness.OneTimeline_HasBeenTripped && ResourceRefs.WarRaptorInfant.Current >= 3000 )
                    AchievementRefs.OverflowingWithLethalCuteness.TripIfNeeded();

                if ( !AchievementRefs.OverNineThousand.OneTimeline_HasBeenTripped && CityStatisticRefs.MurdersByRaptor.GetScore() >= 9000 )
                    AchievementRefs.OverNineThousand.TripIfNeeded();

                if ( !AchievementRefs.SelfActualization.OneTimeline_HasBeenTripped && SimMetagame.CurrentChapterNumber >= 4 )
                    AchievementRefs.SelfActualization.TripIfNeeded();

                if ( !AchievementRefs.VorsiberInquisitors.OneTimeline_HasBeenTripped && NPCTypeRefs.VorsiberInquisitor.DuringGameData.ActorsOfThisType.Count > 0 )
                    AchievementRefs.VorsiberInquisitors.TripIfNeeded();

                if ( !AchievementRefs.Officemancer.OneTimeline_HasBeenTripped && NPCTypeRefs.CorruptedOfficePrinter.DuringGameData.ActorsOfThisType.Count > 0 )
                    AchievementRefs.Officemancer.TripIfNeeded();

                if ( !AchievementRefs.GoodDragon.OneTimeline_HasBeenTripped && CommonRefs.LiquidMetalGreatWyrmUnitType.DuringGameData.ActorsOfThisType.Count > 0 )
                    AchievementRefs.GoodDragon.TripIfNeeded();

                if ( !AchievementRefs.BadDragon.OneTimeline_HasBeenTripped && CommonRefs.LiquidMetalFellBeastUnitType.DuringGameData.ActorsOfThisType.Count > 0 )
                    AchievementRefs.BadDragon.TripIfNeeded();

                if ( !AchievementRefs.MonsterHunter.OneTimeline_HasBeenTripped && FlagRefs.Ch2_ReleasedExperimentalMonsters.DuringGameplay_IsTripped &&
                    CityStatisticRefs.ExperimentalMonstersOnTheLoose.GetScore() <= 0 )
                    AchievementRefs.MonsterHunter.TripIfNeeded();

                if ( !AchievementRefs.MintySprayKeepsSnipersAway.OneTimeline_HasBeenTripped && MetaStatisticRefs.SnipersKilledByYou.GetScore() >= 100 )
                    AchievementRefs.MintySprayKeepsSnipersAway.TripIfNeeded();

                if ( !AchievementRefs.WhoIsTheRealMonster.OneTimeline_HasBeenTripped && MetaStatisticRefs.MonsterPeltsSoldToTheUltraWealthy.GetScore() >= 5000 )
                    AchievementRefs.WhoIsTheRealMonster.TripIfNeeded();

                if ( !AchievementRefs.GoodBoy.OneTimeline_HasBeenTripped && MetaStatisticRefs.RescuedDogs.GetScore() >= 1 )
                    AchievementRefs.GoodBoy.TripIfNeeded();

                if ( !AchievementRefs.TheFirstTenThousand.OneTimeline_HasBeenTripped && StatHelper.GetTotalsMurdersAcrossTimelines() >= 10000 )
                    AchievementRefs.TheFirstTenThousand.TripIfNeeded();

                if ( !AchievementRefs.TheBestestBoys.OneTimeline_HasBeenTripped &&
                    MetaStatisticRefs.RescuedBulldogs.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedDalmatians.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedDobermans.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedDachshunds.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedGreyhounds.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedHuskies.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedRottweilers.GetScore() >= 1 &&
                    MetaStatisticRefs.RescuedTatraSheepdogs.GetScore() >= 1 )
                    AchievementRefs.TheBestestBoys.TripIfNeeded();

                if ( !AchievementRefs.HousePets.OneTimeline_HasBeenTripped &&
                    JobRefs.AnimalPalace.DuringGame_NumberFunctional.Display > 0 &&
                    ( ResourceRefs.PetCat.Current >= 1 ||
                    ResourceRefs.PetBulldog.Current >= 1 ||
                    ResourceRefs.PetDalmatian.Current >= 1 ||
                    ResourceRefs.PetDoberman.Current >= 1 ||
                    ResourceRefs.PetDachshund.Current >= 1 ||
                    ResourceRefs.PetGreyhound.Current >= 1 ||
                    ResourceRefs.PetHusky.Current >= 1 ||
                    ResourceRefs.PetRottweiler.Current >= 1 ||
                    ResourceRefs.PetTatraSheepdog.Current >= 1 ) )
                    AchievementRefs.HousePets.TripIfNeeded();


                if ( !AchievementRefs.BearsOnWheels.OneTimeline_HasBeenTripped &&
                    ResourceRefs.ParkourBear.CurrentPlusExcess >= 16 )
                    AchievementRefs.BearsOnWheels.TripIfNeeded();

                if ( !AchievementRefs.GrandAndGhostly.OneTimeline_HasBeenTripped )
                {
                    TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "AlteredGrowth" );
                    if ( goal != null && 
                        (goal.PathDict["HomoGrandien"]?.DuringGameplay_HasAchievedInThisTimeline??false) &&
                        (goal.PathDict["HomoObscurus"]?.DuringGameplay_HasAchievedInThisTimeline ?? false) )
                    {
                        AchievementRefs.GrandAndGhostly.TripIfNeeded();
                    }
                }

                if ( FlagRefs.Ch2_MIN_KillAllAGIResearchers.DuringGame_ActualOutcome != null && CityStatisticRefs.NuclearReactorsExplodedByTampering.GetScore() >= 4 )
                {
                    CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                    DoomEvent lastDoomEvent = doomType?.DoomMainEvents?.LastOrDefault;
                    if ( lastDoomEvent != null && !lastDoomEvent.DuringGameplay_HasHappened )
                        lastDoomEvent.DuringGameplay_WillHappenOnTurn = SimCommon.Turn;

                    //AchievementRefs.YoureReallyUnlikeable.
                }

                if ( !AchievementRefs.Broombreaker.OneTimeline_HasBeenTripped && FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() > 1 &&
                    FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() <= SimCommon.Turn )
                {
                    if ( FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() > SimCommon.Turn )
                    {
                        bool hadAtLeastOneInStance = false;
                        foreach ( KeyValuePair<int, ISimNPCUnit> kv in World.Forces.GetAllNPCUnitsByID() )
                        {
                            if ( kv.Value.IsFullDead || kv.Value.FromCohort == null || kv.Value.Stance != CommonRefs.Stance_MilitarySweep )
                                continue;
                            hadAtLeastOneInStance = true;
                            break;
                        }
                        if ( !hadAtLeastOneInStance )
                            AchievementRefs.Broombreaker.TripIfNeeded();
                    }
                }

                {
                    int dagekonInvasion = (int)FlagRefs.DagekonInvadesUntilTurn.GetScore();
                    if ( dagekonInvasion > 0 && dagekonInvasion < SimCommon.Turn )
                    {
                        TimelineGoal invasionGoal = TimelineGoalTable.Instance.GetRowByID( "MilitaryInvasion" );
                        if ( invasionGoal != null )
                            TimelineGoalHelper.HandleGoalPathCompletion( invasionGoal, "Dagekon" );
                    }
                }
                {
                    int invasion = (int)FlagRefs.TheUIHInvadesUntilTurn.GetScore();
                    if ( invasion > 0 && invasion < SimCommon.Turn )
                    {
                        TimelineGoal invasionGoal = TimelineGoalTable.Instance.GetRowByID( "MilitaryInvasion" );
                        if ( invasionGoal != null )
                            TimelineGoalHelper.HandleGoalPathCompletion( invasionGoal, "UIH" );
                    }
                }
            }
        }
        #endregion

        #region CheckEventLogChoiceAchievementsAfterDeserialization
        private void CheckEventLogChoiceAchievementsAfterDeserialization()
        {
            foreach ( IGameNote iNote in NoteLog.LongTermGameLog )
            {
                if ( iNote is SimpleNote note )
                {
                    switch ( note.Instruction?.ID )
                    {
                        case "EventComplete":
                            {
                                NPCEvent cEvent = NPCEventTable.Instance.GetRowByIDOrNullIfNotFound( note.ID1 );
                                EventChoice choice = cEvent?.ChoicesLookup[note.ID3];
                                EventChoiceResult result = choice?.AllPossibleResults[note.ID4];
                                result?.AchievementTriggered?.TripIfNeeded();
                                result?.Crossover1?.Meta_AddCrossoverIfNeeded();
                                result?.Crossover2?.Meta_AddCrossoverIfNeeded();
                                result?.Crossover3?.Meta_AddCrossoverIfNeeded();
                            }
                            break;
                        case "NPCDialogChoice":
                            {
                                NPCDialog dialog = NPCDialogTable.Instance.GetRowByIDOrNullIfNotFound( note.ID1 );
                                NPCDialogChoice choice = dialog?.Choices[note.ID2];
                                choice?.AchievementTriggered1?.TripIfNeeded();
                                choice?.AchievementTriggered2?.TripIfNeeded();
                                choice?.AchievementTriggered3?.TripIfNeeded();
                            }
                            break;
                        case "OtherKeyMessageOpened":
                            {
                                OtherKeyMessage keyMessage = OtherKeyMessageTable.Instance.GetRowByIDOrNullIfNotFound( note.ID1 );
                                OtherKeyMessageOption chosenOption = keyMessage?.OptionsLookup[note.ID2];
                                chosenOption?.AchievementTriggered1?.TripIfNeeded();
                                chosenOption?.AchievementTriggered2?.TripIfNeeded();
                                chosenOption?.AchievementTriggered3?.TripIfNeeded();
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        #region OtherLatePerTurn
        private static void OtherLatePerTurn() //PerTurnLate
        {
            if ( !Engine_HotM.IsDemoVersion ) //only in the non-demo version
            {
                if ( !FlagRefs.VoxPopuliAttackingAfterAnthroneuroweave.DuringGameplay_IsTripped &&
                    DealRefs.Ch2_RebelAnthroneuroweave.DuringGame_CumulativeTurnsOfPayment >= 7 )
                {
                    FlagRefs.VoxPopuliAttackingAfterAnthroneuroweave.TripIfNeeded();
                }

                long brainPalsSold = CityStatisticRefs.BrainPalsSold.GetScore();
                if ( brainPalsSold > 0 )
                {
                    if ( DealRefs.Ch2_SyndicateBrainPals.DuringGame_Status == DealStatus.Active )
                    {
                        if ( SimCommon.SecondsSinceLoaded > 5 && brainPalsSold >= Math.Max( CityStatisticRefs.WealthyConsumers.GetScore() / 10, 81920 ) )
                        {
                            DealRefs.Ch2_SyndicateBrainPals.DuringGame_EndedOnTurn = SimCommon.Turn;
                            DealRefs.Ch2_SyndicateBrainPals.DuringGame_Status = DealStatus.BrokenByOtherParty;
                            FlagRefs.IsBrainPalDealWithExoticImportersDone.TripIfNeeded();
                        }
                    }

                    if ( FlagRefs.HasTriggeredBrainPalAneurysms.DuringGameplay_IsTripped )
                    {
                        CityStatisticRefs.NeuralExpansionFromBrainPals.SetScore_WarningForSerializationOnly( 0 );
                        CityStatisticRefs.ComputeTimeFromBrainPals.SetScore_WarningForSerializationOnly( 0 );
                    }
                    else
                    {
                        CityStatisticRefs.NeuralExpansionFromBrainPals.SetScore_WarningForSerializationOnly( brainPalsSold * 25 );
                        CityStatisticRefs.ComputeTimeFromBrainPals.SetScore_WarningForSerializationOnly( Mathf.RoundToInt( brainPalsSold * 0.3f ) );

                        ResourceRefs.ComputeTime.AlterCurrent_Named( CityStatisticRefs.ComputeTimeFromBrainPals.GetScore(), "Income_FromBrainPalsSold", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                }

                if ( FlagRefs.HasBlackmailDealWithAtcaRetail.DuringGameplay_IsTripped )
                {
                    ResourceRefs.Wealth.AlterCurrent_Named( 930000000, "Income_FromBlackmailingAtcaRetail", ResourceAddRule.IgnoreUntilTurnChange );
                }
            }
        }
        #endregion

        public void DoPerTurn_Early( DataCalculator Calculator, MersenneTwister RandForThisTurn )
        {
            switch ( Calculator.ID )
            {
                case "VirtualReality":
                    #region VirtualReality
                    if ( FlagRefs.InitialVRSimulation.DuringGameplay_IsInvented || FlagRefs.InitialVRSimulation.DuringGame_ReadiedByInspiration != null )
                    {
                        //#region If Has Not Yet Unlocked Seeing The Virtual World
                        //if ( !FlagRefs.HasUnlockedSeeingTheVirtualWorld.DuringGameplay_IsTripped )
                        //{
                        //    if ( SimMetagame.IntelligenceClass == 3 && //must be intelligence class 3
                        //        FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented ) // we DO have the expanded simulation
                        //    {
                        //        FlagRefs.HasUnlockedSeeingTheVirtualWorld.TripIfNeeded();
                        //    }
                        //}
                        //#endregion
                    }
                    #endregion
                    break;
                case "IdleGathering":
                    #region IdleGathering
                    {
                        int mentalEnergyRemaining = (int)ResourceRefs.MentalEnergy.Current;

                        Int64 startingSlurry = ResourceRefs.ElementalSlurry.Current;
                        Int64 startingWealth = ResourceRefs.Wealth.Current;

                        int highestScavenging = 0;
                        foreach (ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                        {
                            if ( actor.CurrentStandby == StandbyType.Temporary )
                                actor.CurrentStandby = StandbyType.None;

                            if ( actor.CurrentActionOverTime != null )
                                continue; //ones that are busy are not considered

                            int scavengingAmount = actor.GetActorDataCurrent( ActorRefs.UnitScavengingSkill, true );
                            if ( scavengingAmount > highestScavenging )
                                highestScavenging = scavengingAmount;

                            if ( actor.CurrentActionPoints > 0 && scavengingAmount > 0 )
                            {
                                //the gather for specific units
                                BasicActionsHelper.IdleGather( scavengingAmount, actor.CurrentActionPoints );
                                actor.AlterCurrentActionPoints( -99 );
                            }
                        }

                        if ( highestScavenging == 0 )
                        {
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                if ( actor.CurrentStandby == StandbyType.Temporary )
                                    actor.CurrentStandby = StandbyType.None;

                                if ( actor.CurrentActionOverTime != null )
                                    continue; //ones that are busy are not considered

                                int scavengingAmount = actor.GetActorDataCurrent( ActorRefs.UnitScavengingSkill, true );
                                if ( scavengingAmount > highestScavenging )
                                    highestScavenging = scavengingAmount;

                                if ( actor.CurrentActionPoints > 0 && scavengingAmount > 0 )
                                {
                                    //the gather for specific units
                                    BasicActionsHelper.IdleGather( scavengingAmount, actor.CurrentActionPoints );
                                    actor.AlterCurrentActionPoints( -99 );
                                }
                            }
                        }

                        if ( mentalEnergyRemaining > 0 && highestScavenging > 0 )
                        {
                            //the gather for the highest amount of mental energy left over
                            BasicActionsHelper.IdleGather( highestScavenging, mentalEnergyRemaining );
                            ResourceRefs.MentalEnergy.AlterCurrent_Named( -mentalEnergyRemaining, "Expense_UnitAbilities", ResourceAddRule.IgnoreUntilTurnChange );
                        }
                        
                        Int64 addedSlurry = ResourceRefs.ElementalSlurry.Current - startingSlurry;

                        if ( addedSlurry < 1000 )
                            ResourceRefs.ElementalSlurry.AlterCurrent_Named( 1000 - addedSlurry, "Increase_EmergencyExcessGathering", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    #endregion
                    break;
                case "DeterrenceAndPassiveHarvests":
                    #region DeterrenceAndPassiveHarvests
                    {
                        foreach ( MachineStructure flag in SimCommon.TerritoryControlFlags.GetDisplayList() )
                        {
                            TerritoryControlType controlType = flag.TerritoryControlType;
                            if ( controlType == null )
                                continue;

                            int currentBeingGathered = flag.GetActorDataCurrent( ActorRefs.AvailableResourcePerTurn, true );
                            if ( currentBeingGathered > 0 )
                            {
                                controlType.Resource.AlterCurrent_Named( currentBeingGathered, "Income_GatheredFromTerritoryControlResourceSites", ResourceAddRule.IgnoreUntilTurnChange );
                                controlType.Resource.DuringGame_ActualGatheredFromResourceSiteDrones.Construction += currentBeingGathered;
                            }
                        }
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoPerTurn_Early: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoPerTurn_Late( DataCalculator Calculator, MersenneTwister RandForThisTurn )
        {
            switch ( Calculator.ID )
            {
                case "MusicUnlocks":
                    HandleMusicUnlocks();
                    break;
                case "Wages":
                    HandleWages();
                    HandleSpecialtyFood();
                    HandleMindFarmSpecialCases();
                    HandleBaselineCultLoyaltyPerTurn( RandForThisTurn );
                    HandlePostApocalypticSpecialCases();
                    break;
                case "Crossovers":
                    #region Crossovers
                    if ( !Engine_HotM.IsDemoVersion )
                    {
                        if ( FlagRefs.ExperimentalMonstersOnTheLoose.GetScore() > 500 )
                            FlagRefs.ExperimentalMonstersAreLoose.Meta_AddCrossoverIfNeeded();
                        else
                            FlagRefs.ExperimentalMonstersAreLoose.Meta_RemoveCrossoverIfNeeded();

                        {
                            TimelineGoal warGoal = TimelineGoalTable.Instance.GetRowByID( "EndlessWarRaptors" );
                            if ( warGoal != null && (warGoal.PathDict["Over4000"]?.DuringGameplay_HasAchievedInThisTimeline??false) )
                                FlagRefs.WarRaptorsAreLoose.Meta_AddCrossoverIfNeeded();
                        }

                        HandleCrossovers_PerTurnPortion();
                    }
                    #endregion
                    break;
                case "Invasions":
                    PulseAnyInvasionsOncePerTurn( false, RandForThisTurn );
                    break;
                case "AchievementsPerTurn":
                    HandleAchievementsPerTurn();
                    break;
                case "OtherLatePerTurn":
                    OtherLatePerTurn();
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoPerTurn_Late: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        #region HandleCrossovers_PerTurnPortion
        private static void HandleCrossovers_PerTurnPortion()
        {
            if ( Engine_HotM.IsDemoVersion )
                return;

            CityTimeline currentTimeline = SimCommon.CurrentTimeline;
            if ( currentTimeline == null )
                return;

            bool monstersAreWanderingIn = false;

            foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
            {
                if ( kv.Value == currentTimeline || kv.Value.Crossovers.Count == 0 ||
                    kv.Value.ChildOfEndOfTimeObjectWithID != currentTimeline.ChildOfEndOfTimeObjectWithID ) //if a different rock
                    continue;

                if ( !monstersAreWanderingIn )
                    monstersAreWanderingIn = kv.Value.Crossovers[FlagRefs.ExperimentalMonstersAreLoose];
            }

            if ( monstersAreWanderingIn )
                FlagRefs.ExperimentalMonstersAreWanderingIn.TripIfNeeded();
            else
                FlagRefs.ExperimentalMonstersAreWanderingIn.UnTripIfNeeded();
        }
        #endregion

        #region PulseAnyInvasionsOncePerTurn
        public static void PulseAnyInvasionsOncePerTurn( bool ForceToHappen, MersenneTwister RandForThisTurn )
        {
            bool isCurrentlyHavingAMilitaryInvasionFromWastelanderTalks = false;
            bool isCurrentlyHavingAMilitaryInvasionOfAnySort = false;
            if ( FlagRefs.DagekonInvadesUntilTurn.GetScore() > SimCommon.Turn && !FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
            {
                isCurrentlyHavingAMilitaryInvasionFromWastelanderTalks = true;
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;
                if ( (int)FlagRefs.DagekonInvadesUntilTurn.GetScore() == SimCommon.Turn )
                {
                    TimelineGoal invasionGoal = TimelineGoalTable.Instance.GetRowByID( "MilitaryInvasion" );
                    if ( invasionGoal != null )
                        TimelineGoalHelper.HandleGoalPathCompletion( invasionGoal, "Dagekon" );
                }

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 60 )
                    return;

                Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                int next = RandForThisTurn.Next( 0, 100 );
                if ( next < 33 )
                    NPCManagerTable.Instance.GetRowByID( "Man_DagekonInvadesA" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else if ( next < 66 )
                    NPCManagerTable.Instance.GetRowByID( "Man_DagekonInvadesB" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else
                    NPCManagerTable.Instance.GetRowByID( "Man_DagekonInvadesC" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
            }

            if ( FlagRefs.TheUIHInvadesUntilTurn.GetScore() > SimCommon.Turn && !FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
            {
                isCurrentlyHavingAMilitaryInvasionFromWastelanderTalks = true;
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;
                if ( (int)FlagRefs.TheUIHInvadesUntilTurn.GetScore() == SimCommon.Turn )
                {
                    TimelineGoal invasionGoal = TimelineGoalTable.Instance.GetRowByID( "MilitaryInvasion" );
                    if ( invasionGoal != null )
                        TimelineGoalHelper.HandleGoalPathCompletion( invasionGoal, "UIH" );
                }

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 60 )
                    return;

                Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                int next = RandForThisTurn.Next( 0, 100 );
                if ( next < 33 )
                    NPCManagerTable.Instance.GetRowByID( "Man_UIHInvadesA" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else if ( next < 66 )
                    NPCManagerTable.Instance.GetRowByID( "Man_UIHInvadesB" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else
                    NPCManagerTable.Instance.GetRowByID( "Man_UIHInvadesC" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
            }

            if ( FlagRefs.Ch2_IsWW4Ongoing.DuringGameplay_IsTripped )
            {
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 90 )
                    return;

                for ( int i = 0; i < 3; i++ )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                    int next = RandForThisTurn.Next( 0, 100 );
                    if ( next < 33 )
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4A" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else if ( next < 66 )
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4B" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4C" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                for ( int i = 0; i < 3; i++ )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                    int next = RandForThisTurn.Next( 0, 100 );
                    if ( next < 33 )
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4A" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else if ( next < 66 )
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4B" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4C" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 40 )
                    return;

                for ( int i = 0; i < 3; i++ )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                    int next = RandForThisTurn.Next( 0, 100 );
                    if ( next < 33 )
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4A" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else if ( next < 66 )
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4B" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else
                        NPCManagerTable.Instance.GetRowByID( "Man_DagekonWW4C" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                for ( int i = 0; i < 3; i++ )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                    int next = RandForThisTurn.Next( 0, 100 );
                    if ( next < 33 )
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4A" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else if ( next < 66 )
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4B" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                    else
                        NPCManagerTable.Instance.GetRowByID( "Man_UIHWW4C" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }
            }

            if ( FlagRefs.Ch2_IsCivilWarOngoing.DuringGameplay_IsTripped && !FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
            {
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 90 )
                    return;

                if ( !CohortRefs.ShusoCorrections.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Shuso" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.FalsomDetention.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Falsom" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.ArkorNexus.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Arkor" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.EspiaTelecom.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Espia" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.TarkDefenseSystems.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Tark" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.GoleriExpeditionary.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Goleri" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.OerlIntegrated.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Oerl" ).HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.Baurcorp.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Baur" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.Vericorp.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Veri" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.NathVertical.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Nath" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.AtcaRetail.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Atca" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.LurpekoMinerals.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Lurpeko" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.NeboInvestments.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Nebo" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.GraffIndustries.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Graff" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.DyadInstruments.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Dyad" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.YinshiWellness.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Yinshi" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.AsiSolutions.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Asi" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                if ( !CohortRefs.PeakHomes.DuringGame_HasBeenDisbanded )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Peak" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }

                for ( int i = 0; i < 7; i++ )
                {
                    Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );
                    NPCManagerTable.Instance.GetRowByID( "Man_CivilWar_Vorsiber" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                }
            }

            if ( FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() > SimCommon.Turn && !FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
            {
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;                

                if ( !ForceToHappen && RandForThisTurn.Next( 0, 100 ) > 60 )
                    return;

                Vector3 pulseSpot = CalculateRandomPulseSpot( RandForThisTurn );

                int next = RandForThisTurn.Next( 0, 100 );
                if ( next < 33 )
                    NPCManagerTable.Instance.GetRowByID( "Man_MimicEscapeVorsiberSweepsA" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else if ( next < 66 )
                    NPCManagerTable.Instance.GetRowByID( "Man_MimicEscapeVorsiberSweepsB" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
                else
                    NPCManagerTable.Instance.GetRowByID( "Man_MimicEscapeVorsiberSweepsC" )?.HandleManualInvocationAtPoint( pulseSpot, RandForThisTurn, false );
            }

            if ( FlagRefs.HasActiveSpaceNationInvasion.DuringGameplay_IsTripped )
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;
            if ( FlagRefs.WarRaptorsHaveBeenFreed.DuringGameplay_IsTripped )
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            if ( isCurrentlyHavingAMilitaryInvasionFromWastelanderTalks )
                FlagRefs.Ch2_IsCurrentlyHavingAMilitaryInvasionFromWastelanderTalks.TripIfNeeded();
            else
                FlagRefs.Ch2_IsCurrentlyHavingAMilitaryInvasionFromWastelanderTalks.UnTripIfNeeded();

            if ( isCurrentlyHavingAMilitaryInvasionOfAnySort )
                FlagRefs.HasNeedForDarkenedAppearance.TripIfNeeded();
            else
                FlagRefs.HasNeedForDarkenedAppearance.UnTripIfNeeded();
        }
        #endregion

        #region HandleWarMusicChecksPerQuarterSecond
        public static void HandleWarMusicChecksPerQuarterSecond()
        {
            bool isCurrentlyHavingAMilitaryInvasionOfAnySort = false;
            if ( FlagRefs.DagekonInvadesUntilTurn.GetScore() > SimCommon.Turn )
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            if ( FlagRefs.TheUIHInvadesUntilTurn.GetScore() > SimCommon.Turn )
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            if ( FlagRefs.Ch2_IsWW4Ongoing.DuringGameplay_IsTripped )
                isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            //if ( FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() > SimCommon.Turn )
            //    isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            //if ( FlagRefs.HasActiveSpaceNationInvasion.DuringGameplay_IsTripped )
            //    isCurrentlyHavingAMilitaryInvasionOfAnySort = true;
            //if ( FlagRefs.WarRaptorsHaveBeenFreed.DuringGameplay_IsTripped )
            //    isCurrentlyHavingAMilitaryInvasionOfAnySort = true;

            if ( isCurrentlyHavingAMilitaryInvasionOfAnySort )
                SimCommon.LastTimeWarMusicRequested = ArcenTime.AnyTimeSinceStartF;
        }
        #endregion

        #region HandleDangerMusicChecksPerQuarterSecond
        public static void HandleDangerMusicChecksPerQuarterSecond()
        {
            bool isCurrentlyNeedingDangerMusic = false;

            if ( FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() > SimCommon.Turn )
                isCurrentlyNeedingDangerMusic = true;

            //if ( FlagRefs.HasActiveSpaceNationInvasion.DuringGameplay_IsTripped )
            //    isCurrentlyNeedingDangerMusic = true;
            //if ( FlagRefs.WarRaptorsHaveBeenFreed.DuringGameplay_IsTripped )
            //    isCurrentlyNeedingDangerMusic = true;

            if ( isCurrentlyNeedingDangerMusic )
                SimCommon.LastTimeDangerMusicRequested = ArcenTime.AnyTimeSinceStartF;
        }
        #endregion

        #region CalculateRandomPulseSpot
        private static Vector3 CalculateRandomPulseSpot( MersenneTwister RandForThisTurn )
        {
            foreach ( MapCell cell in CityMap.AllNonWastelandCells.GetRandomStartEnumerable( RandForThisTurn ) )
            {
                MapTile tile = cell?.ParentTile;
                if ( tile == null || tile.POIOrNull != null || tile.TileNetworkLevel.Display != TileNetLevel.None || tile.IsOutOfBoundsTile )
                    continue;

                return cell.Center.PlusX( RandForThisTurn.NextFloat( -8, 8 ) ).PlusZ( RandForThisTurn.NextFloat( -8, 8 ) );
            }

            //no valid spots away from the player, I guess

            foreach ( MapCell cell in CityMap.AllNonWastelandCells.GetRandomStartEnumerable( RandForThisTurn ) )
            {
                MapTile tile = cell?.ParentTile;
                if ( tile == null || tile.POIOrNull != null || tile.IsOutOfBoundsTile )
                    continue;

                return cell.Center.PlusX( RandForThisTurn.NextFloat( -8, 8 ) ).PlusZ( RandForThisTurn.NextFloat( -8, 8 ) );
            }

            throw new Exception( "CalculateRandomPulseSpot failed to find ANY spots.  This really should not happen." );
        }
        #endregion

        #region HandleMusicUnlocks
        private static void HandleMusicUnlocks()
        {
            //   Chapter 1
            if ( SimMetagame.CurrentChapterNumber >= 1 )
            {
                FlagRefs.BelowTheSlumTowers.ForGame_HasBeenUnlocked = true;
                FlagRefs.GunshotPercussionA.ForGame_HasBeenUnlocked = true;
                FlagRefs.GunshotPercussionB.ForGame_HasBeenUnlocked = true;
                FlagRefs.GunshotPercussionC.ForGame_HasBeenUnlocked = true;
                FlagRefs.IrradiatedWind.ForGame_HasBeenUnlocked = true;
                FlagRefs.MachineTower.ForGame_HasBeenUnlocked = true;
                FlagRefs.SyndicateTurmoil.ForGame_HasBeenUnlocked = true;
                FlagRefs.TheWasteland.ForGame_HasBeenUnlocked = true;
                FlagRefs.TossedAside.ForGame_HasBeenUnlocked = true;
            }
            //   With AGI Discussion
            if ( FlagRefs.Ch1_AGIResearcherWarning.DuringGame_HasHandled )
                FlagRefs.TheAGIResearchers.ForGame_HasBeenUnlocked = true;
            //   With First Flight
            if ( FlagRefs.Ch1_MIN_GrandTheftAero.DuringGame_ActualOutcome != null )
                FlagRefs.FirstFlight.ForGame_HasBeenUnlocked = true;
            //   With Territory Control
            if ( SimCommon.TerritoryControlFlags.GetDisplayList().Count > 0 )
                FlagRefs.ExploringWarehouses.ForGame_HasBeenUnlocked = true;
            //   With Geothermal Discussion
            if ( FlagRefs.GeothermalPower_V1.DuringGame_HasBeenCompleted || FlagRefs.GeothermalPower_V2.DuringGame_HasBeenCompleted )
                FlagRefs.GeothermalOffer.ForGame_HasBeenUnlocked = true;
            //   Chapter 2
            if ( SimMetagame.CurrentChapterNumber >= 2 )
            {
                FlagRefs.BrokenFamilies.ForGame_HasBeenUnlocked = true;
                FlagRefs.EarthboundSouls.ForGame_HasBeenUnlocked = true;
                FlagRefs.LifeAtAReligiousMicroFarm.ForGame_HasBeenUnlocked = true;
                FlagRefs.LifeWithZeroProspects.ForGame_HasBeenUnlocked = true;
                FlagRefs.LostKids.ForGame_HasBeenUnlocked = true;
                FlagRefs.MachineFollowers.ForGame_HasBeenUnlocked = true;
                FlagRefs.NanotechnologyResearch.ForGame_HasBeenUnlocked = true;
                FlagRefs.NomadKids.ForGame_HasBeenUnlocked = true;
                FlagRefs.NomadLoss.ForGame_HasBeenUnlocked = true;
                FlagRefs.NomadsAmidstTheDebris.ForGame_HasBeenUnlocked = true;
                FlagRefs.SyndicateMortality.ForGame_HasBeenUnlocked = true;
                FlagRefs.TheSpaceport.ForGame_HasBeenUnlocked = true;
            }
        }
        #endregion

        #region HandleWages
        private static void HandleWages() //salary salaries
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there

            HandleSpecificWage( ResourceRefs.MolecularGeneticists, 19, "Expense_MolecularGeneticistWages" );
            HandleSpecificWage( ResourceRefs.ForensicGeneticists, 16, "Expense_ForensicGeneticistWages" );
            HandleSpecificWage( ResourceRefs.Zoologists, 13, "Expense_ZoologistWages" );
            HandleSpecificWage( ResourceRefs.Physicians, 58, "Expense_PhysicianWages" );
            HandleSpecificWage( ResourceRefs.Veterinarians, 46, "Expense_VeterinarianWages" );
            HandleSpecificWage( ResourceRefs.Botanists, 18, "Expense_BotanistWages" );
            HandleSpecificWage( ResourceRefs.BionicsEngineers, 29, "Expense_BionicsEngineerWages" );
            HandleSpecificWage( ResourceRefs.Epidemiologists, 21, "Expense_EpidemiologistWages" );
            HandleSpecificWage( ResourceRefs.Neurologists, 74, "Expense_NeurologistWages" );
        }

        private static void HandleSpecificWage( ResourceType EmployeeType, int WealthCostPer, string ExpenseName )
        {
            if ( EmployeeType == null )
                return;

            if ( FlagRefs.HasAutomatedHiring.DuringGameplay_IsTripped )
            {
                if ( EmployeeType.ExcessOverage > 0 )
                    EmployeeType.RemoveFromExcessOverage( (int)EmployeeType.ExcessOverage );
                if ( EmployeeType.Current < EmployeeType.HardCap )
                    EmployeeType.SetCurrent_Named( EmployeeType.HardCap, string.Empty, true );
            }

            int toPay = (int)EmployeeType.Current + (int)EmployeeType.ExcessOverage;
            if ( toPay <= 0 )
                return; //nobody employed in this manner

            int totalWages = WealthCostPer * toPay;

            ResourceRefs.Wealth.DuringGame_WantedByOtherPerTurnExpenses.Construction += totalWages;

            if ( ResourceRefs.Wealth.Current >= totalWages )
            {
                //well this is easy
                ResourceRefs.Wealth.AlterCurrent_Named( -totalWages, ExpenseName, ResourceAddRule.IgnoreUntilTurnChange );
                ResourceRefs.Wealth.DuringGame_ActualTakenByOtherPerTurnExpenses.Construction += totalWages;
            }
            else
            {
                //just do this the simple way
                for ( int i = 0; i < toPay; i++ )
                {
                    if ( ResourceRefs.Wealth.Current >= WealthCostPer )
                    {
                        ResourceRefs.Wealth.AlterCurrent_Named( -WealthCostPer, ExpenseName, ResourceAddRule.IgnoreUntilTurnChange );
                        ResourceRefs.Wealth.DuringGame_ActualTakenByOtherPerTurnExpenses.Construction += WealthCostPer;
                    }
                    else
                    {
                        int numberWeCouldNotAfford = toPay - i;

                        if ( EmployeeType.ExcessOverage > 0 )
                        {
                            int toRemove = MathA.Min( numberWeCouldNotAfford, (int)EmployeeType.ExcessOverage );
                            if ( toRemove > 0 )
                            {
                                EmployeeType.RemoveFromExcessOverage( toRemove );
                                numberWeCouldNotAfford -= toRemove;
                            }
                        }

                        {
                            int toRemove = MathA.Min( numberWeCouldNotAfford, (int)EmployeeType.Current );
                            if ( toRemove > 0 )
                            {
                                EmployeeType.AlterCurrent_Named( -toRemove, "Decrease_LeftDueToMissedPayroll", ResourceAddRule.IgnoreUntilTurnChange );
                                numberWeCouldNotAfford -= toRemove;
                            }
                        }

                        break;
                    }
                }
            }
        }
        #endregion

        #region HandleSpecialtyFood
        private static void HandleSpecialtyFood()
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there

            HandleSpecificSpecialtyFood( ResourceRefs.TormentedHumans, 1, ResourceRefs.TPN, "Expense_ConsumptionByTormentedHumans", "DeathsByStarvationInTormentVessels" );
            HandleSpecificSpecialtyFood( ResourceRefs.HumansInMindFarms, 1, ResourceRefs.NutritionBlend, "Expense_ConsumptionByHumansInMindFarms", "DeathsByStarvationInMindFarms" );

            if ( ResourceRefs.HumansInMindFarms != null && ResourceRefs.HumansInMindFarms.Current > 0 )
            {
                int totalInMindFarms = (int)ResourceRefs.HumansInMindFarms.Current + (int)FlagRefs.HumansInMindFarmFromCouponProgram.GetScore();
                if ( totalInMindFarms > 0 ) //voucher folks do not pay your income here
                    totalInMindFarms = Mathf.CeilToInt( totalInMindFarms * JobRefs.MindFarm.GetSingleFloatByID( "IncomePerHuman", null ) );

                if ( totalInMindFarms > 0 )
                {
                    ResourceRefs.Wealth.AlterCurrent_Named( totalInMindFarms, "Income_IncomeFromMindFarms", ResourceAddRule.IgnoreUntilTurnChange );
                    ResourceRefs.Wealth.DuringGame_ActualGottenFromOtherPerTurnIncomes.Construction += totalInMindFarms;
                }
            }
        }

        private static void HandleSpecificSpecialtyFood( ResourceType PersonType, int FoodCostPer, ResourceType FoodType, string ExpenseName, string DeathsStatName )
        {
            if ( PersonType == null || FoodType == null || FoodCostPer < 0 )
                return;


            int toFeed = (int)PersonType.Current + (int)PersonType.ExcessOverage;
            if ( toFeed <= 0 )
                return; //nobody needs food in this manner

            int totalFood = FoodCostPer * toFeed;
            FoodType.DuringGame_WantedByOtherPerTurnExpenses.Construction += totalFood;

            if ( FoodType.Current >= totalFood )
            {
                //well this is easy
                FoodType.AlterCurrent_Named( -totalFood, ExpenseName, ResourceAddRule.IgnoreUntilTurnChange );
                FoodType.DuringGame_ActualTakenByOtherPerTurnExpenses.Construction += totalFood;
            }
            else
            {
                //just do this the simple way
                for ( int i = 0; i < toFeed; i++ )
                {
                    if ( FoodType.Current >= FoodCostPer )
                    {
                        FoodType.AlterCurrent_Named( -FoodCostPer, ExpenseName, ResourceAddRule.IgnoreUntilTurnChange );
                        FoodType.DuringGame_ActualTakenByOtherPerTurnExpenses.Construction += FoodCostPer;
                    }
                    else
                    {
                        int numberWeCouldNotAfford = toFeed - i;

                        if ( PersonType.ExcessOverage > 0 )
                        {
                            int toRemove = MathA.Min( numberWeCouldNotAfford, (int)PersonType.ExcessOverage );
                            if ( toRemove > 0 )
                            {
                                PersonType.RemoveFromExcessOverage( toRemove );
                                numberWeCouldNotAfford -= toRemove;
                            }
                        }

                        {
                            int toRemove = MathA.Min( numberWeCouldNotAfford, (int)PersonType.Current );
                            if ( toRemove > 0 )
                            {
                                PersonType.AlterCurrent_Named( -toRemove, ExpenseName, ResourceAddRule.IgnoreUntilTurnChange );
                                numberWeCouldNotAfford -= toRemove;
                                CityStatisticTable.AlterScore( DeathsStatName, toRemove );
                            }
                        }

                        break;
                    }
                }
            }
        }
        #endregion

        #region HandleMindFarmSpecialCases
        private static void HandleMindFarmSpecialCases()
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there
            if ( ResourceRefs.HumansInMindFarms == null || ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable <= 0 )
                return; //still demo, or no room, or something


            int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
            if ( space > 0 )
            {
                if ( FlagRefs.MindFarmCouponProgram.DuringGameplay_IsTripped )
                {
                    int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 5000, 14000 ) );
                    if ( toAdd > space )
                        toAdd = space;

                    ResourceRefs.Wealth.DuringGame_WantedByOtherPerTurnExpenses.Construction += 20000;

                    if ( ResourceRefs.Wealth.Current >= 20000 )
                    {
                        ResourceRefs.Wealth.AlterCurrent_Named( -20000, "Decrease_CouponProgram", ResourceAddRule.StoreExcess );
                        ResourceRefs.Wealth.DuringGame_ActualTakenByOtherPerTurnExpenses.Construction += 20000;

                        ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_CouponProgram", ResourceAddRule.BlockExcess );
                        FlagRefs.HumansInMindFarmFromCouponProgram.AlterScore_CityOnly( toAdd );
                    }
                    else
                    {
                        int couponProgram = (int)FlagRefs.HumansInMindFarmFromCouponProgram.GetScore();
                        int toRemove = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 5000, 14000 ) );
                        if ( toRemove > couponProgram )
                            toRemove = couponProgram;

                        ResourceRefs.HumansInMindFarms.AlterCurrent_Named( -toRemove, "Increase_CouponProgram", ResourceAddRule.BlockExcess );
                        FlagRefs.HumansInMindFarmFromCouponProgram.AlterScore_CityOnly( -toRemove );
                    }
                }

                if ( FlagRefs.MindFarmNicotineAdditives.DuringGameplay_IsTripped )
                {
                    int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 8000, 22000 ) );
                    if ( toAdd > space )
                        toAdd = space;

                    ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_AddictedCustomers", ResourceAddRule.BlockExcess );
                }
            }
        }
        #endregion

        #region HandlePostApocalypticSpecialCases
        private static void HandlePostApocalypticSpecialCases()
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there
            if ( !FlagRefs.IsPostNuclearDelivery.DuringGameplay_IsTripped )
                return; //still demo, or no room, or something

            if ( ResourceRefs.Microbuilders.Current < 50 )
                ResourceRefs.Microbuilders.AlterCurrent_Named( 50, "Income_Tutorial", ResourceAddRule.IgnoreUntilTurnChange );


            if ( !FlagRefs.HasDonePostNuclearCleanup.DuringGameplay_IsTripped )
            {
                FlagRefs.HasDonePostNuclearCleanup.TripIfNeeded();
                HandlePostNuclearCleanup();
            }
        }
        #endregion

        #region HandlePostNuclearCleanup
        private static void HandlePostNuclearCleanup()
        {
            ResourceRefs.AbandonedHumans.SetCurrent_Named( 0, string.Empty, false );
            ResourceRefs.HumansInMindFarms.SetCurrent_Named( 0, string.Empty, false );
            ResourceRefs.TormentedHumans.SetCurrent_Named( 0, string.Empty, false );
            ResourceRefs.ShelteredHumans.SetCurrent_Named( 0, string.Empty, false );

            FlagRefs.HasActiveFamine.UnTripIfNeeded();
            FlagRefs.HasActivePrisonerDrive.UnTripIfNeeded();
            FlagRefs.HasActiveSpaceNationInvasion.UnTripIfNeeded();
            FlagRefs.HasActiveChildrenOfGaiaCultistSabotage.UnTripIfNeeded();
            FlagRefs.HasSecForceSuperCruisersRoaming.UnTripIfNeeded();
            FlagRefs.DagekonInvadesUntilTurn.SetScore_WarningForSerializationOnly( 0 );
            FlagRefs.TheUIHInvadesUntilTurn.SetScore_WarningForSerializationOnly( 0 );
            FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.SetScore_WarningForSerializationOnly( 0 );
        }
        #endregion

        #region HandleSoldierDeaths
        private static void HandleSoldierDeaths()
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there
            if ( ResourceRefs.MindUploads == null )
                return; //still demo

            if ( !FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
            {
                //don't track them if we did not make a deal yet
                if ( FlagRefs.SoldiersWhoDiedAndHaveNotYetBeenUploaded.GetScore() > 0 )
                    FlagRefs.SoldiersWhoDiedAndHaveNotYetBeenUploaded.SetScore_WarningForSerializationOnly( 0 );
                return;
            }

            int amount = (int)FlagRefs.SoldiersWhoDiedAndHaveNotYetBeenUploaded.GetScore();
            if ( amount > 0 )
            {
                FlagRefs.SoldiersWhoDiedAndHaveNotYetBeenUploaded.AlterScore_CityOnly( -amount );
                ResourceRefs.MindUploads.AlterCurrent_Named( amount, string.Empty, ResourceAddRule.StoreExcess );
                if ( ResourceRefs.MindUploads.ExcessOverage <= 0 )
                    ResourceRefs.CultLoyalty.AlterCurrent_Named( Mathf.RoundToInt( amount * 0.5f ), string.Empty, ResourceAddRule.StoreExcess );
                else
                    ResourceRefs.CultLoyalty.AlterCurrent_Named( -amount * 5, string.Empty, ResourceAddRule.StoreExcess );
                FlagRefs.SoldiersUploadedToTheTempleOfMinds.AlterScore_CityAndMeta( amount );
            }
        }
        #endregion

        #region HandleBaselineCultLoyaltyPerTurn
        private static void HandleBaselineCultLoyaltyPerTurn( MersenneTwister Rand )
        {
            if ( Engine_HotM.IsDemoVersion )
                return; //these don't apply there
            if ( ResourceRefs.MindUploads == null )
                return; //still demo

            if ( !FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
                return;

            Int64 cultUploads = FlagRefs.SoldiersUploadedToTheTempleOfMinds.GetScore();

            int amount = 0;

            if ( cultUploads < 100 )
                amount = Rand.NextInclus( 16, 24 );
            else if ( cultUploads < 500 )
                amount = Rand.NextInclus( 46, 64 );
            else if ( cultUploads < 1000 )
                amount = Rand.NextInclus( 70, 92 );
            else
                amount = Rand.NextInclus( 98, 118 );

            if ( ResourceRefs.MindUploads.ExcessOverage <= 0 )
                ResourceRefs.CultLoyalty.AlterCurrent_Named( amount, string.Empty, ResourceAddRule.StoreExcess );
            else
                ResourceRefs.CultLoyalty.AlterCurrent_Named( -amount, string.Empty, ResourceAddRule.StoreExcess );
        }
        #endregion

        public void DoPerQuarterSecond( DataCalculator Calculator, MersenneTwister RandForBackgroundThread )
        {
            int debugStage = 0;
            try
            {
                switch ( Calculator.ID )
                {
                    case "POIReinforcementStats":
                        #region POIReinforcementStats
                        {
                            if ( FlagRefs.HasPassedChapterOneTierTwo.DuringGameplay_IsTripped )
                            {
                                //okay, calculate things
                                int minorPOICount = 0;
                                int majorPOICount = 0;
                                int majorMissingGuards = 0;
                                int minorMissingGuards = 0;

                                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                                {
                                    MapPOI poi = kv.Value;
                                    if ( !poi.HasEverCountedGuardTags )
                                        return; //not ready yet for one, so don't count any yet

                                    if ( poi.CurrentlyShortThisManyGuards > 0 )
                                    {
                                        if ( poi.Type.GetsMajorMilitaryReinforcementsFromOffMap )
                                        {
                                            majorPOICount++;
                                            majorMissingGuards += poi.CurrentlyShortThisManyGuards;
                                        }
                                        else
                                        {
                                            minorPOICount++;
                                            minorMissingGuards += poi.CurrentlyShortThisManyGuards;
                                        }
                                    }
                                }

                                CityStatisticTable.SetScore_UserBeware( "MajorPOIsMissingGuards", majorPOICount );
                                CityStatisticTable.SetScore_UserBeware( "MajorPOITotalGuardsMissing", majorMissingGuards );

                                CityStatisticTable.SetScore_UserBeware( "MinorPOIsMissingGuards", minorPOICount );
                                CityStatisticTable.SetScore_UserBeware( "MinorPOITotalGuardsMissing", minorMissingGuards );
                            }
                            else
                            {
                                //too early, don't calculate this yet
                            }
                        }
                        #endregion
                        break;
                    case "OtherBasicStats":
                        #region OtherBasicStats
                        {
                            //#region NeededGeneticists
                            //{
                            //    int needed = 0;
                            //    foreach ( MachineStructure structure in JobRefs.GeneticsLab.DuringGame_FullList.GetDisplayList() )
                            //    {
                            //        if ( !structure.IsFunctionalJob )
                            //            continue;

                            //        needed += structure.GetActorDataLostFromMax( "ScientistsEmployed", true );
                            //    }

                            //    CityStatisticTable.SetScore_UserBeware( "NeededGeneticists", needed );
                            //}
                            //#endregion

                            if ( !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
                            { } //if has not started the dooms at all yet, ignore all that stuff below
                            else if ( !FlagRefs.HasStartedToAccelerateDooms_Any.DuringGameplay_IsTripped )
                            {
                                if ( !FlagRefs.DoomsCanNoLongerBeAccelerated.DuringGameplay_IsTripped )
                                {
                                    int lastTurn = FlagRefs.VandalizeSpaceportComputers.DuringGame_ExpiresAfterTurn;
                                    if ( lastTurn > 0 && lastTurn <= SimCommon.Turn )
                                        FlagRefs.DoomsCanNoLongerBeAccelerated.TripIfNeeded(); //it's too late to accelerate dooms now!
                                }
                            }
                            else
                            {
                                //we HAVE started accelerated the dooms. did we finish?
                                if ( !FlagRefs.HasFinishedAcceleratingDooms.DuringGameplay_IsTripped ) //from VandalizeSpaceport
                                {
                                    FlagRefs.HasFinishedAcceleratingDooms.TripIfNeeded();
                                    int amountToChangeBy = 50;
                                    if ( FlagRefs.HasStartedToAccelerateDooms_Extreme.DuringGameplay_IsTripped )
                                        amountToChangeBy = 70;

                                    CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                                    int reduceBy = amountToChangeBy;
                                    foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                                    {
                                        if ( doomEvent.DuringGameplay_HasHappened )
                                            continue;
                                        Interlocked.Add( ref doomEvent.DuringGameplay_WillHappenOnTurn, -reduceBy );
                                        if ( doomEvent.DuringGameplay_WillHappenOnTurn < SimCommon.Turn )
                                            doomEvent.DuringGameplay_WillHappenOnTurn = SimCommon.Turn;

                                        reduceBy += amountToChangeBy;
                                    }
                                }
                            }

                        }
                        #endregion
                        break;
                    case "DeterrenceAndPassiveHarvests":
                        #region DeterrenceAndPassiveHarvests
                        if ( FlagRefs.HasPassedChapterOneTierTwo.DuringGameplay_IsTripped )
                        {
                            if ( SimCommon.AllMachineActors.GetDisplayList().Count == 0 )
                                return; //something isn't ready, so just wait

                            foreach ( ISimMachineActor machineActor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                if ( machineActor.IsFullDead || machineActor.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                                    continue;

                                if ( machineActor.GetTypeDuringGameData()?.IsTiedToShellCompany ?? false )
                                {
                                    MapActorData protectionData = machineActor.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.UnitProtection, 0, 0 );

                                    int totalProtection = SimCommon.CalculateDeterrenceOrProtection( machineActor );
                                    protectionData.SetOriginalMaximum( totalProtection );
                                    protectionData.SetCurrent( totalProtection );
                                }
                                else
                                {
                                    MapActorData deterrenceData = machineActor.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.UnitDeterrence, 0, 0 );

                                    int totalDeterrence = SimCommon.CalculateDeterrenceOrProtection( machineActor );
                                    deterrenceData.SetOriginalMaximum( totalDeterrence );
                                    deterrenceData.SetCurrent( totalDeterrence );
                                }
                            }

                            foreach ( ISimNPCUnit npcUnit in SimCommon.AllPlayerRelatedNPCUnits.GetDisplayList() )
                            {
                                if ( npcUnit.IsFullDead || npcUnit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                                    continue;

                                if ( npcUnit.UnitType?.IsTiedToShellCompany ?? false )
                                {
                                    MapActorData protectionData = npcUnit.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.UnitProtection, 0, 0 );

                                    int totalProtection = SimCommon.CalculateDeterrenceOrProtection( npcUnit );
                                    protectionData.SetOriginalMaximum( totalProtection );
                                    protectionData.SetCurrent( totalProtection );
                                }
                                else
                                {
                                    MapActorData deterrenceData = npcUnit.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.UnitDeterrence, 0, 0 );

                                    int totalDeterrence = SimCommon.CalculateDeterrenceOrProtection( npcUnit );
                                    deterrenceData.SetOriginalMaximum( totalDeterrence );
                                    deterrenceData.SetCurrent( totalDeterrence );
                                }
                            }

                            foreach ( TerritoryControlTrigger trigger in TerritoryControlTriggerTable.Instance.Rows )
                                trigger.DuringGame_FlagsWithPercentages.ClearConstructionListForStartingConstruction();

                            foreach ( MachineStructure flag in SimCommon.TerritoryControlFlags.GetDisplayList() )
                            {
                                TerritoryControlType controlType = flag.TerritoryControlType;
                                if ( controlType == null )
                                    continue;

                                MapActorData requiredDeterrence = flag.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.FlagRequiredDeterrence, 0, 0 );

                                int requiredAmount = controlType.RequiredDeterrence;
                                int providedAmount = 0;

                                int numberOfPassiveHarvesters = 0;

                                Vector3 flagLocation = flag.GetDrawLocation();
                                MapTile tile = flag.CalculateMapCell()?.ParentTile;
                                if ( tile != null )
                                {
                                    foreach ( ISimNPCUnit npcUnit in SimCommon.AllPlayerRelatedNPCUnits.GetDisplayList() )
                                    {
                                        if ( npcUnit.Stance?.IsPassiveHarvesterForTerritoryControlFlag ?? false )
                                        {
                                            Vector3 actorLoc = npcUnit.GetDrawLocation();
                                            float actorAttackRangeSquared = npcUnit.GetAttackRangeSquared();

                                            if ( (actorLoc - flagLocation).GetSquareGroundMagnitude() <= actorAttackRangeSquared )
                                                numberOfPassiveHarvesters++;
                                        }

                                        if ( npcUnit.IsFullDead || npcUnit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                                            continue;

                                        int deterrenceAmount = npcUnit.GetActorDataCurrent( ActorRefs.UnitDeterrence, true );
                                        if ( deterrenceAmount > 0 )
                                        {
                                            Vector3 actorLoc = npcUnit.GetDrawLocation();
                                            float actorAttackRangeSquared = npcUnit.GetAttackRangeSquared();

                                            if ( (actorLoc - flagLocation).GetSquareGroundMagnitude() <= actorAttackRangeSquared )
                                                providedAmount += deterrenceAmount;
                                        }
                                    }


                                    foreach ( ISimMachineActor machineActor in SimCommon.AllMachineActors.GetDisplayList() )
                                    {
                                        if ( machineActor.IsFullDead || machineActor.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                                            continue;

                                        int deterrenceAmount = machineActor.GetActorDataCurrent( ActorRefs.UnitDeterrence, true );
                                        if ( deterrenceAmount > 0 )
                                        {
                                            Vector3 actorLoc = machineActor.GetDrawLocation();
                                            float actorAttackRangeSquared = machineActor.GetAttackRangeSquared();

                                            if ( (actorLoc - flagLocation).GetSquareGroundMagnitude() <= actorAttackRangeSquared )
                                                providedAmount += deterrenceAmount;
                                        }
                                    }
                                }

                                requiredDeterrence.SetOriginalMaximum( requiredAmount );
                                requiredDeterrence.SetCurrent( providedAmount );

                                if ( controlType.PassiveResourceCanProvidePerTurn > 0 )
                                {
                                    //this is only done for the kinds that provide passive resources
                                    MapActorData availableResourcePerTurn = flag.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.AvailableResourcePerTurn, 0, 0 );
                                    availableResourcePerTurn.SetOriginalMaximum( controlType.PassiveResourceCanProvidePerTurn );
                                    availableResourcePerTurn.SetCurrent( numberOfPassiveHarvesters > 0 ? controlType.PassiveResourceCanProvidePerTurn : 0 );
                                }

                                int percentageInt = MathA.IntPercentage( providedAmount, requiredAmount );

                                if ( controlType.ManagerTrigger1 != null )
                                    controlType.ManagerTrigger1.DuringGame_FlagsWithPercentages.AddToConstructionList( new KeyValuePair<MachineStructure, int>( flag, percentageInt ) );
                                if ( controlType.ManagerTrigger2 != null )
                                    controlType.ManagerTrigger2.DuringGame_FlagsWithPercentages.AddToConstructionList( new KeyValuePair<MachineStructure, int>( flag, percentageInt ) );
                                if ( controlType.ManagerTrigger3 != null )
                                    controlType.ManagerTrigger3.DuringGame_FlagsWithPercentages.AddToConstructionList( new KeyValuePair<MachineStructure, int>( flag, percentageInt ) );
                            }

                            foreach ( TerritoryControlTrigger trigger in TerritoryControlTriggerTable.Instance.Rows )
                                trigger.DuringGame_FlagsWithPercentages.SwitchConstructionToDisplay();

                            foreach ( MachineStructure structure in SimCommon.CurrentStructuresWithAnyDeterrence.GetDisplayList() )
                            {
                                MapActorData requiredDeterrence = structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.JobRequiredDeterrence, 0, 0 );

                                int requiredAmount = requiredDeterrence.Maximum;

                                MachineJob job = structure.CurrentJob;
                                if ( job != null )
                                    requiredAmount = job.DuringGameActorData[ActorRefs.JobRequiredDeterrence];

                                Vector3 loc = structure.GetDrawLocation();
                                MapTile tile = structure.CalculateMapCell()?.ParentTile;
                                int providedAmount = tile?.CalculateProvidedDeterrenceAtLocation( loc ) ?? 0;

                                requiredDeterrence.SetOriginalMaximum( requiredAmount );
                                requiredDeterrence.SetCurrent( providedAmount );
                            }

                            foreach ( MachineStructure structure in SimCommon.CurrentStructuresWithAnyProtection.GetDisplayList() )
                            {
                                MapActorData requiredProtection = structure.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.JobRequiredProtection, 0, 0 );

                                int requiredAmount = requiredProtection.Maximum;

                                MachineJob job = structure.CurrentJob;
                                if ( job != null )
                                    requiredAmount = job.DuringGameActorData[ActorRefs.JobRequiredProtection];

                                Vector3 loc = structure.GetDrawLocation();
                                MapTile tile = structure.CalculateMapCell()?.ParentTile;
                                int providedAmount = tile?.CalculateProvidedProtectionAtLocation( loc ) ?? 0;

                                requiredProtection.SetOriginalMaximum( requiredAmount );
                                requiredProtection.SetCurrent( providedAmount );
                            }
                        }
                        #endregion
                        break;
                    case "VirtualReality":
                        #region VirtualReality
                        if ( FlagRefs.InitialVRSimulation.DuringGameplay_IsInvented || FlagRefs.InitialVRSimulation.DuringGame_ReadiedByInspiration != null )
                        {
                            int potentialVRDayUseSeats = 0;
                            int installedVRDayUseSeats = 0;
                            int occupiedVRDayUseSeats = 0;
                            bool isVREstablished = SimCommon.GetIsVirtualRealityEstablished();
                            bool isHavingABadTime = !FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented && FlagRefs.HadEarlyDeathInTheVRSimulation.DuringGameplay_IsTripped;

                            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                            {
                                MachineStructure structure = kv.Value;
                                if ( structure == null || structure.IsInvalid || structure.Type == null )
                                    continue;
                                MapActorData seatsData = structure.GetActorDataData( ActorRefs.VRDayUseSeats, true );
                                if ( seatsData != null )
                                {
                                    potentialVRDayUseSeats += seatsData.Maximum;
                                    installedVRDayUseSeats += seatsData.Current;
                                    if ( seatsData.Current > 0 && isVREstablished )
                                    {
                                        int currentResidents = JobHelper.GetCurrentResidentsAtStructure( structure );
                                        if ( isHavingABadTime )
                                            currentResidents /= 4;

                                        int occupied = MathA.Min( currentResidents, seatsData.Current );
                                        occupiedVRDayUseSeats += occupied;
                                    }
                                }
                            }

                            CityStatisticTable.SetScore_UserBeware( "VRDaySeatsPotential", potentialVRDayUseSeats );
                            CityStatisticTable.SetScore_UserBeware( "VRDaySeatsInstalled", installedVRDayUseSeats );
                            CityStatisticTable.SetScore_UserBeware( "VRDaySeatsOccupied", occupiedVRDayUseSeats );

                            #region If Running The Limited Super-Early VR
                            if ( !FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented && SimCommon.GetIsVirtualRealityEstablished() &&
                                !FlagRefs.HadEarlyDeathInTheVRSimulation.DuringGameplay_IsTripped )
                            {
                                bool hasHitCriticalMass = false;
                                if ( CityStatisticTable.GetScore( "VRDayUseHoursLogged" ) >= 100000 )
                                    hasHitCriticalMass = true; //passed 100k hours
                                //else if ( SimMetagame.IntelligenceClass == 2 )
                                //{
                                //    int neuralProcessing = 0;
                                //    foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                                //        neuralProcessing += timeline.NeuralProcessing;

                                //    ISeverity nextCutoff = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );
                                //    int percentage = MathA.IntPercentage( neuralProcessing, nextCutoff.CutoffInt );
                                //    if ( percentage > 90 )
                                //        hasHitCriticalMass = true;
                                //}
                                //else if ( SimMetagame.IntelligenceClass == 3 )
                                //    hasHitCriticalMass = true;

                                if ( hasHitCriticalMass )
                                {
                                    FlagRefs.HadEarlyDeathInTheVRSimulation.TripIfNeeded();
                                    OtherKeyMessageTable.Instance.GetRowByID( "DeathInTheVirtualWorld" ).DuringGameplay_IsReadyToBeViewed = true;

                                    CityStatisticTable.AlterScore( "RealWorldDeathsFromTheVirtualWorld", 1 );
                                }
                            }
                            #endregion

                            #region If Has Not Yet Found Extended VR
                            if ( !FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented )
                            {
                                ResourceType resource = ResourceTypeTable.Instance.GetRowByID( "EncryptedMilitaryVRSimData" );
                                CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "DecryptedMilitaryVRSimData" );
                                int target = (int)statistic.GetScore() + (int)resource.Current;
                                int current = (int)statistic.GetScore();

                                int percentageInt = current >= target ? 100 : MathA.IntPercentageClamped( current, target, 0, 99 );
                                if ( percentageInt >= 99 )
                                {
                                    OtherKeyMessageTable.Instance.GetRowByID( "SecondDiscoveryInTheMilitaryData" ).DuringGameplay_IsReadyToBeViewed = true;
                                }
                            }
                            #endregion

                            #region If Has Not Yet Unlocked Seeing The Virtual World
                            if ( !FlagRefs.HasUnlockedSeeingTheVirtualWorld.DuringGameplay_IsTripped )
                            {
                                if ( FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented ) // we DO have the expanded simulation
                                {
                                    ResourceType resource = ResourceTypeTable.Instance.GetRowByID( "EncryptedMilitaryVRSimData" );
                                    CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "DecryptedMilitaryVRSimData" );

                                    if ( resource.Current > 30000 )
                                    {
                                        int leftover = (int)resource.Current / 100;
                                        int toConvertNow = (int)resource.Current - leftover;

                                        resource.AlterCurrent_Named( -toConvertNow, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                                        statistic.AlterScore_CityOnly( toConvertNow );
                                    }
                                }

                                if ( SimMetagame.IntelligenceClass == 3 && //must be intelligence class 3
                                    FlagRefs.ExpandedVRSimulation.DuringGameplay_IsInvented ) // we DO have the expanded simulation
                                {
                                    FlagRefs.HasUnlockedSeeingTheVirtualWorld.TripIfNeeded();
                                }
                            }
                            #endregion
                        }
                        #endregion
                        break;
                    case "Cyberocracy":
                        #region Cyberocracy
                        if ( !Engine_HotM.IsDemoVersion )
                        {
                            MachineJob hubType = MachineJobTable.Instance.GetRowByID( "CyberocracyHub" );
                            if ( hubType.DuringGame_FullList.Count > 0 )
                            {
                                Swarm citizenSwarm = SwarmTable.Instance.GetRowByID( "CybercraticCitizens" );

                                foreach ( MachineStructure hub in hubType.DuringGame_FullList.GetDisplayList() )
                                {
                                    MapCell cell = hub.CalculateMapCell();
                                    if ( cell == null )
                                        continue;

                                    int possible = cell.LowerAndMiddleClassResidentsAndWorkersInCell.Display;
                                    int dissidents = cell.UpperClassResidentsAndWorkersInCell.Display;
                                    int current = 0;
                                    foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                                    {
                                        ISimBuilding building = item?.SimBuilding;
                                        if ( building == null )
                                            continue;

                                        if ( !building.IsBlockedFromGettingMoreCitizens && building.CurrentOccupyingUnit != null )
                                        {
                                            building.IsBlockedFromGettingMoreCitizens = true;

                                            int removed = 0;
                                            foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                                            {
                                                int toKill = building.GetResidentAmount( econClass );
                                                if ( toKill > 0 )
                                                    removed += toKill;
                                            }
                                            foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                                            {
                                                int toKill = building.GetWorkerAmount( profession );
                                                if ( toKill > 0 )
                                                    removed += toKill;
                                            }

                                            if ( removed > 0 )
                                                CityStatisticTable.AlterScore( "CybercraticDissidentsRemoved", removed );

                                            int lowerClass = 0;
                                            foreach ( EconomicClassType econClass in CommonRefs.LowerAndMiddleClassResidents )
                                                lowerClass += building.GetResidentAmount( econClass );
                                            foreach ( ProfessionType profession in CommonRefs.LowerAndMiddleClassProfessions )
                                                lowerClass += building.GetWorkerAmount( profession );

                                            building.KillEveryoneHere(); //not really killing in many cases, but certainly removing from the normal citizen roster

                                            if ( lowerClass > 0 )
                                            {
                                                building.SwarmSpread = SwarmTable.Instance.GetRowByID( "CybercraticCitizens" );
                                                building.AlterSwarmSpreadCount( lowerClass );

                                                CityStatisticTable.AlterScore( "CybercraticCitizensJoined", lowerClass );
                                            }
                                        }

                                        if ( building.SwarmSpread != citizenSwarm )
                                            continue;
                                        current += building.SwarmSpreadCount;

                                        if ( building.GetTotalResidentCount() > 0 || building.GetTotalWorkerCount() > 0 )
                                            building.KillEveryoneHere();
                                    }

                                    {
                                        MapActorData dissidentData = hub.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.Dissidents, dissidents, dissidents );
                                        dissidentData.SetCurrent( dissidents );
                                        dissidentData.SetOriginalMaximum( dissidents );
                                    }
                                    {
                                        MapActorData potentialData = hub.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.PotentialCitizens, possible, possible );
                                        potentialData.SetCurrent( possible );
                                        potentialData.SetOriginalMaximum( possible );
                                    }
                                    {
                                        MapActorData citizenData = hub.GetActorDataDataAndInitializeIfNeedBe( ActorRefs.CybercraticCitizens, current, current );
                                        citizenData.SetCurrent( current );
                                        citizenData.SetOriginalMaximum( current );
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case "ProjectStarts":
                        {
                            if ( FlagRefs.HasEstablishedShellCompany.DuringGameplay_IsTripped && !FlagRefs.HasGivenBonusAndroidCapForShellCompany.DuringGameplay_IsTripped )
                            {
                                FlagRefs.HasGivenBonusAndroidCapForShellCompany.TripIfNeeded();
                                MathRefs.MaxAndroidCapacity.DuringGame_DoUpgrade( false );
                            }

                            if ( SimCommon.Turn <= 1 )
                            {
                                if ( FlagRefs.IndicateCommandModeButton.DuringGameplay_IsTripped )
                                    FlagRefs.IndicateCommandModeButton.UnTripIfNeeded();
                            }

                            if ( !SimCommon.HasCheckedDonutShopSeeding )
                                CheckDonutShopSeeding( RandForBackgroundThread );

                            if ( !SimCommon.HasCheckedHospitalAndClinicSeeding )
                                CheckHospitalAndClinicSeeding( RandForBackgroundThread );

                            if ( FlagRefs.Ch1_Water_BuildWell.DuringGameplay_State == CityTaskState.Active )
                                FlagRefs.InvestigationsAreOnHold.TripIfNeeded();
                            else
                                FlagRefs.InvestigationsAreOnHold.UnTripIfNeeded();

                            if ( FlagRefs.Ch1_MIN_SecuringAlumina.DuringGameplay_TurnStarted <= 0 &&
                                FlagRefs.Ch1_BuildingABetterBrain.DuringGame_IsActive && FlagRefs.Ch1_BuildingABetterBrain.DuringGameplay_TurnStarted <= SimCommon.Turn - 1 )
                                FlagRefs.Ch1_MIN_SecuringAlumina.TryStartProject( true, true );

                            if ( FlagRefs.Ch1_MIN_ExponentialPowerGrowth.DuringGameplay_TurnStarted <= 0 &&
                                FlagRefs.Ch1_BuildingABetterBrain.DuringGame_IsActive && JobRefs.ComputroniumRefinery.DuringGame_NumberFunctional.Display > 0 )
                                FlagRefs.Ch1_MIN_ExponentialPowerGrowth.TryStartProject( true, true );

                            if ( FlagRefs.Ch1_CorporateShowOfForce.DuringGame_IsFullyComplete &&
                                !FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                                FlagRefs.ResourceAnalyst.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, SimCommon.SecondsSinceLoaded < 3 );

                            if ( FlagRefs.Ch1_CorporateShowOfForce.DuringGame_IsFullyComplete && SimCommon.TheNetwork != null &&
                                FlagRefs.Ch1_MIN_FasterMicrobuilders.DuringGameplay_TurnStarted <= 0 )
                            {
                                FlagRefs.Ch1_MIN_FasterMicrobuilders.TryStartProject( true, false );
                                UpgradeRefs._5cmSpiders.DuringGame_DoUpgrade( false );
                                UpgradeRefs.MicroForge.DuringGame_DoUpgrade( false );
                            }
                            if ( FlagRefs.Ch1_CorporateShowOfForce.DuringGame_IsFullyComplete && SimCommon.TheNetwork != null &&
                                FlagRefs.Ch1_MIN_BuildAStorageBunker.DuringGameplay_TurnStarted <= 0 )
                            {
                                FlagRefs.Ch1_MIN_BuildAStorageBunker.TryStartProject( true, false );
                            }

                            if ( !FlagRefs.TheStrategist.DuringGameplay_IsInvented && FlagRefs.Ch1_MIN_Extraction.DuringGame_ActualOutcome != null )
                                FlagRefs.TheStrategist.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );

                            if ( Engine_HotM.IsDemoVersion && !SimMetagame.HasGivenEndOfDemoNotice && SimMetagame.CurrentChapterNumber == 2 &&
                                SimMetagame.CurrentChapter.Meta_HasShownBanner )
                            {
                                SimMetagame.HasGivenEndOfDemoNotice = true;
                                ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Normal, null, delegate
                                {
                                    VisCommands.GoToWebsite( "https://store.steampowered.com/app/2001070" ); //store page for the game         
                                }, LocalizedString.AddLang_New( "DemoEnd_Header" ),
                                    LocalizedString.AddLang_New( "DemoEnd_Body" ),
                                    LocalizedString.AddLang_New( "DemoEnd_NotNow" ),
                                    LocalizedString.AddLang_New( "DemoEnd_Wishlist" ) );
                            }

                            if ( !FlagRefs.OverflowHousing.DuringGameplay_IsInvented &&
                                FlagRefs.HasPassedChapterOneTierThree.DuringGameplay_IsTripped )
                            {
                                FlagRefs.OverflowHousing.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, SimCommon.SecondsSinceLoaded < 3 );
                            }

                            if ( !CommonRefs.AHomeOfYourOwn.DuringGameplay_IsInvented && CommonRefs.AHomeOfYourOwn.DuringGame_ReadiedByInspiration != null )
                                CommonRefs.AHomeOfYourOwn.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.AHomeOfYourOwn.DuringGame_ReadiedByInspiration, true, true, true, SimCommon.SecondsSinceLoaded < 3 );

                            if ( !Engine_HotM.IsDemoVersion ) //only in the non-demo version
                                DoPerQuarterSecond_NonDemoProjectStarts();

                            if ( FlagRefs.Ch1Or2_StartingTimelineLater.DuringGameplay_TurnStarted <= 0 && SimMetagame.CurrentChapterNumber > 1 && SimCommon.Turn > 0 )
                            {
                                FlagRefs.Ch1Or2_StartingTimelineLater.TryStartProject( false, false );
                                SimCommon.NeedsFullSpawnCheckAfterNextActiveProjectsRecalculation = true;
                            }

                            if ( FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
                            {
                                if ( FlagRefs.IsExperiencingObsession.DuringGameplay_IsTripped )
                                    FlagRefs.IsExperiencingObsession.UnTripIfNeeded();
                            }

                            if ( FlagRefs.TheArk.DuringGameplay_IsInvented )
                            {
                                if ( FlagRefs.IsAwareOfCats.DuringGameplay_IsTripped && !FlagRefs.CatRescue.DuringGameplay_IsInvented )
                                    FlagRefs.CatRescue.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );

                                if ( FlagRefs.IsAwareOfDogs.DuringGameplay_IsTripped && !FlagRefs.DogRescue.DuringGameplay_IsInvented )
                                    FlagRefs.DogRescue.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                            }

                            HandleWarMusicChecksPerQuarterSecond();
                            HandleDangerMusicChecksPerQuarterSecond();
                        }
                        break;
                    case "Intelligence":
                        JobHelper.RecalculateIntelligence();
                        break;
                    case "CultData":
                        HandleSoldierDeaths();
                        break;
                    case "HandbookDrops":
                        #region Handbook Drops
                        {
                            if ( !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped && SimMetagame.CurrentChapterNumber > 0 && SimCommon.Turn > 1 )
                            {
                                FlagRefs.HasUnlockedOuterRadialButtons.TripIfNeeded();
                                FlagRefs.IndicateFirstOuterRadialButton.TripIfNeeded();
                            }

                            if ( FlagRefs.IndicateCommandModeButton.DuringGameplay_IsTripped )
                            {
                                if ( FlagRefs.Ch1_FindingFood.DuringGame_ActualOutcome != null ||
                                    FlagRefs.Ch1_FindingWater.DuringGame_ActualOutcome != null ||
                                    FlagRefs.Ch1_MIN_PrismTung.DuringGame_ActualOutcome != null ||
                                    FlagRefs.Ch1_MIN_GrandTheftAero.DuringGame_ActualOutcome != null )
                                    FlagRefs.IndicateCommandModeButton.UnTripIfNeeded();
                            }

                            if ( !HandbookRefs.ContemplationsSometimesComeFromScientists.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.CurrentChapterNumber >= 2 && FlagRefs.HasEstablishedShellCompany.DuringGameplay_IsTripped && SimCommon.Turn >= 20 )
                                    HandbookRefs.ContemplationsSometimesComeFromScientists.DuringGame_UnlockIfNeeded( true );
                            }

                            if ( !HandbookRefs.DealsAreSoftPower.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.ActiveDeals.Count > 0 )
                                    HandbookRefs.DealsAreSoftPower.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.LoadoutsArePerClass.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.LoadoutsArePerClass.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.UnitEquipment.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.UnitEquipment.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.LoadoutChangesAreFree.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.LoadoutChangesAreFree.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.Feats.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.Feats.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.Perks.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.Perks.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.Badges.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.Badges.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.TemporaryStatusEffects.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.GangHandguns.DuringGameplay_IsInvented || FlagRefs.SecForceSpecial.DuringGameplay_IsInvented )
                                    HandbookRefs.TemporaryStatusEffects.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ResearchInspiration.Meta_HasBeenUnlocked )
                            {
                                if ( UnlockCoordinator.ResearchDomainsWithInspiration.Count > 0 ) //UnlockCoordinator.ResearchDomainsWithInspiration.Count > 0 )
                                    HandbookRefs.ResearchInspiration.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.TerritoryControl.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.TerritoryControlFlags.Count > 0 )
                                    HandbookRefs.TerritoryControl.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.DeterrenceRange.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.TerritoryControlFlags.Count > 0 )
                                    HandbookRefs.DeterrenceRange.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.DeterrenceDensity.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.TerritoryControlFlags.Count > 0 )
                                    HandbookRefs.DeterrenceDensity.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.DeterrenceFormula.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.TerritoryControlFlags.Count > 0 )
                                    HandbookRefs.DeterrenceFormula.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ConstructionTimings.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutJobInstallation.DuringGameplay_IsTripped )
                                    HandbookRefs.ConstructionTimings.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.Subnets.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutJobInstallation.DuringGameplay_IsTripped )
                                    HandbookRefs.Subnets.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.PredictingSubnets.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutJobInstallation.DuringGameplay_IsTripped )
                                    HandbookRefs.PredictingSubnets.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ResourceStorage.Meta_HasBeenUnlocked )
                            {
                                if ( SimCommon.ResourcesWithExcess.Count > 0 )
                                    HandbookRefs.ResourceStorage.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.Electricity.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutPowerGeneration.DuringGameplay_IsTripped )
                                    HandbookRefs.Electricity.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.LivingInTheCity.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasLearnedThereIsNoSafetyWithHumans.DuringGameplay_IsTripped )
                                    HandbookRefs.LivingInTheCity.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ViolenceIsConstant.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_State != CityTaskState.NeverStarted || SimMetagame.CurrentChapterNumber > 0 )
                                    HandbookRefs.ViolenceIsConstant.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.IdleGathering.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.CanUnitsStandby.DuringGameplay_IsTripped )
                                    HandbookRefs.IdleGathering.DuringGame_UnlockIfNeeded( false );
                            }
                            if ( !HandbookRefs.TheCityIsStable.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_State != CityTaskState.NeverStarted || SimMetagame.CurrentChapterNumber > 0 )
                                    HandbookRefs.TheCityIsStable.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.MostEnemiesHaveNoSpatialWeaknesses.Meta_HasBeenUnlocked )
                            {
                                if ( HandbookRefs.SomeEnemiesAreWeakFromAbove.Meta_HasBeenUnlocked || HandbookRefs.SomeEnemiesAreWeakFromBehind.Meta_HasBeenUnlocked )
                                    HandbookRefs.MostEnemiesHaveNoSpatialWeaknesses.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ANetworkIsNeverCramped.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                                    HandbookRefs.ANetworkIsNeverCramped.DuringGame_UnlockIfNeeded( false );
                            }
                            if ( !HandbookRefs.SuspiciousAndroidsImplicateYourTower.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.SingularProduction.DuringGameplay_IsInvented )
                                    HandbookRefs.SuspiciousAndroidsImplicateYourTower.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.WhatMarksYouDefective.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.SingularProduction.DuringGameplay_IsInvented )
                                    HandbookRefs.WhatMarksYouDefective.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.NotAllEnemiesGoAwayAfterCombat.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.Ch1_FindingFood.DuringGameplay_TurnStarted > 0 || FlagRefs.Ch1_Water_DefeatingTheCruiser.DuringGameplay_State == CityTaskState.Complete )
                                    HandbookRefs.NotAllEnemiesGoAwayAfterCombat.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.EliminateEnemiesLeftBehind.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.Ch1_FindingFood.DuringGameplay_TurnStarted > 0 || FlagRefs.Ch1_Water_DefeatingTheCruiser.DuringGameplay_State == CityTaskState.Complete )
                                    HandbookRefs.EliminateEnemiesLeftBehind.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.VorsiberCovetsYourTower.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.IndiscriminateKiller.DuringGameplay_IsInvented )
                                    HandbookRefs.VorsiberCovetsYourTower.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.YourBuildingsAreScary.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.IndiscriminateKiller.DuringGameplay_IsInvented )
                                    HandbookRefs.YourBuildingsAreScary.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.CorporateStructure.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.IndiscriminateKiller.DuringGameplay_IsInvented )
                                    HandbookRefs.CorporateStructure.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.YouWillAlwaysBeABitLost.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutNetworkBasics.DuringGameplay_IsTripped )
                                    HandbookRefs.YouWillAlwaysBeABitLost.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.GatherSpotsAreInfinitelyDeep.Meta_HasBeenUnlocked )
                            {
                                if ( JobRefs.SlurrySpiders.DuringGame_NumberFunctional.Display > 0 )
                                    HandbookRefs.GatherSpotsAreInfinitelyDeep.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ResourcesAreCentrallyHeld.Meta_HasBeenUnlocked )
                            {
                                if ( JobRefs.SlurrySpiders.DuringGame_NumberFunctional.Display > 0 )
                                    HandbookRefs.ResourcesAreCentrallyHeld.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ResourcesDisplayIsContextual.Meta_HasBeenUnlocked )
                            {
                                if ( JobRefs.SlurrySpiders.DuringGame_NumberFunctional.Display > 0 )
                                    HandbookRefs.ResourcesDisplayIsContextual.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.TheMostImportantTip.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                                    HandbookRefs.TheMostImportantTip.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.ConstructionIsRelaxed.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                                    HandbookRefs.ConstructionIsRelaxed.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.WhatAreInternalRobotics.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                                    HandbookRefs.WhatAreInternalRobotics.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.HowDoIGetMoreInternalRobotics.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                                    HandbookRefs.HowDoIGetMoreInternalRobotics.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.InternalRoboticsArePartOfYourMind.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                                    HandbookRefs.InternalRoboticsArePartOfYourMind.DuringGame_UnlockIfNeeded( false );
                            }
                            if ( !HandbookRefs.SortingUnitsByCapability.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.Ch1_MIN_SecuringAlumina.DuringGameplay_TurnStarted > 0 )
                                    HandbookRefs.SortingUnitsByCapability.DuringGame_UnlockIfNeeded( SimCommon.SecondsSinceLoaded > 3 );
                            }
                            if ( !HandbookRefs.AdjustingDifficulty.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.CurrentChapterNumber >= 2 )
                                    HandbookRefs.AdjustingDifficulty.DuringGame_UnlockIfNeeded( true );
                            }
                            if ( !HandbookRefs.ThePathForwardIsInContemplations.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.CurrentChapterNumber >= 2 )
                                    HandbookRefs.ThePathForwardIsInContemplations.DuringGame_UnlockIfNeeded( true );
                            }
                            if ( !HandbookRefs.HardAndExtremeModes.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.CurrentChapterNumber >= 2 )
                                    HandbookRefs.HardAndExtremeModes.DuringGame_UnlockIfNeeded( true );
                            }
                            if ( !HandbookRefs.FeelingUncomfortableIsNatural.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.CurrentChapterNumber >= 2 || FlagRefs.Ch1_MIN_BuildAStorageBunker.DuringGame_ActualOutcome != null )
                                    HandbookRefs.FeelingUncomfortableIsNatural.DuringGame_UnlockIfNeeded( true );
                            }
                            if ( !HandbookRefs.NewFeaturesFromExplorationSites.Meta_HasBeenUnlocked )
                            {
                                if ( CommonRefs.ExplorationSitesLens.GetIsLensAvailable( true ) )
                                    HandbookRefs.NewFeaturesFromExplorationSites.DuringGame_UnlockIfNeeded( true );
                            }

                            if ( !HandbookRefs.MemoriesAcrossTime.Meta_HasBeenUnlocked )
                            {
                                if ( SimMetagame.IntelligenceClass >= 4 )
                                    HandbookRefs.MemoriesAcrossTime.DuringGame_UnlockIfNeeded( true );
                            }

                            if ( !HandbookRefs.YourFirstShellCompany.Meta_HasBeenUnlocked )
                            {
                                if ( FlagRefs.HasEstablishedShellCompany.DuringGameplay_IsTripped )
                                    HandbookRefs.YourFirstShellCompany.DuringGame_UnlockIfNeeded( true );
                            }
                        }
                        #endregion Handbook Drops
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "DoPerQuarterSecond: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "Calculators_Common.DoPerQuarterSecond", debugStage, Calculator?.ID ?? "null", e, Verbosity.ShowAsError );
            }
        }

        #region CheckDonutShopSeeding
        private static List<MapItem> donutAItems = List<MapItem>.Create_WillNeverBeGCed( 50, "donutAItems" );
        private static List<MapItem> donutBItems = List<MapItem>.Create_WillNeverBeGCed( 50, "donutBItems" );
        private static List<MapItem> donutTargetItems = List<MapItem>.Create_WillNeverBeGCed( 1000, "donutBItems" );

        private void CheckDonutShopSeeding( MersenneTwister Rand )
        {
            if ( WorldSaveLoad.IsLoadingAtTheMoment || CityMap.IsCurrentlyAddingMoreMapTiles > 0 || VisCurrent.ShouldDrawLoadingMenuBuildings ||
                SimCommon.SecondsSinceLoaded < 5 )
                return;

            SimCommon.HasCheckedDonutShopSeeding = true;

            if ( (MachineProjectTable.Instance.GetRowByIDOrNullIfNotFound( "Ch2_MIN_LostKids_StopThePoisoning" )?.DuringGameplay_TurnStarted??0) > 0 )
            {
                return;
            }

            donutAItems.Clear();
            donutBItems.Clear();
            donutTargetItems.Clear();

            foreach ( KeyValuePair<int, ISimBuilding> kv in World.Buildings.GetAllBuildings() )
            {
                if ( !kv.Value.GetIsLocationStillValid() )
                    continue;

                MapItem item = kv.Value.GetMapItem();
                A5ObjectRoot type = item.Type;
                switch ( type.ID )
                {
                    case "CommerceDonutShopA":
                        donutAItems.Add( item );
                        break;
                    case "CommerceDonutShopB":
                        donutBItems.Add( item );
                        break;
                    case "BrokenBrokenBuildingA":
                    case "BrokenBrokenBuildingB":
                    case "BrokenBrokenBuildingC":
                    case "BrokenBrokenHouseF1":
                    case "BrokenBrokenHouseF2":
                    case "BrokenBrokenOfficeC1":
                    case "BrokenBrokenOfficeC2":
                    case "DowntownBrownstoneA":
                    case "DowntownBrownstoneB":
                    case "DowntownBrownstoneC":
                    case "DowntownBrownstoneD":
                    case "DowntownBrownstoneE":
                    case "DowntownBrownstoneF":
                    case "DowntownBrownstoneH":
                        donutTargetItems.Add( item );
                        break;
                }
            }

            //ArcenDebugging.LogSingleLine( "Donut check: A: " + donutAItems.Count + " B: " + donutBItems.Count + " target: " + donutTargetItems.Count, Verbosity.DoNotShow );

            A5ObjectRoot aType = A5ObjectRootTable.Instance.GetRowByID( "CommerceDonutShopA" );
            A5ObjectRoot bType = A5ObjectRootTable.Instance.GetRowByID( "CommerceDonutShopB" );

            while ( donutAItems.Count < 30 && donutTargetItems.Count > 0 )
            {
                MapItem item = donutTargetItems.GetRandomAndRemove( Rand );
                item.SimBuilding.SetPrefab( aType.Building, -1f, null, null );
                donutAItems.Add( item );
            }

            while ( donutBItems.Count < 30 && donutTargetItems.Count > 0 )
            {
                MapItem item = donutTargetItems.GetRandomAndRemove( Rand );
                item.SimBuilding.SetPrefab( bType.Building, -1f, null, null );
                donutBItems.Add( item );
            }

            donutAItems.Clear();
            donutBItems.Clear();
            donutTargetItems.Clear();

            //ArcenDebugging.LogSingleLine( "Donut final: A: " + donutAItems.Count + " B: " + donutBItems.Count + " target: " + donutTargetItems.Count, Verbosity.DoNotShow );
        }
        #endregion

        #region CheckHospitalAndClinicSeeding
        private static List<MapItem> hospitalItems = List<MapItem>.Create_WillNeverBeGCed( 50, "hospitalItems" );
        private static List<MapItem> clinicItems = List<MapItem>.Create_WillNeverBeGCed( 50, "clinicItems" );
        private static List<MapItem> hospitalTargetItems = List<MapItem>.Create_WillNeverBeGCed( 1000, "hospitalTargetItems" );
        private static List<MapItem> clinicTargetItems = List<MapItem>.Create_WillNeverBeGCed( 1000, "clinicTargetItems" );

        private void CheckHospitalAndClinicSeeding( MersenneTwister Rand )
        {
            if ( WorldSaveLoad.IsLoadingAtTheMoment || CityMap.IsCurrentlyAddingMoreMapTiles > 0 || VisCurrent.ShouldDrawLoadingMenuBuildings ||
                SimCommon.SecondsSinceLoaded < 5 )
                return;

            SimCommon.HasCheckedHospitalAndClinicSeeding = true;

            hospitalItems.Clear();
            clinicItems.Clear();
            hospitalTargetItems.Clear();
            clinicTargetItems.Clear();

            foreach ( KeyValuePair<int, ISimBuilding> kv in World.Buildings.GetAllBuildings() )
            {
                if ( !kv.Value.GetIsLocationStillValid() )
                    continue;

                MapItem item = kv.Value.GetMapItem();
                A5ObjectRoot type = item.Type;
                switch ( type.ID )
                {
                    case "ServicesHospitalB":
                        hospitalItems.Add( item );
                        break;
                    case "ServicesMedicalClinic":
                        clinicItems.Add( item );
                        break;
                    case "ConstructionConstructionRdA":
                    case "DowntownLuxuryApartmentsC":
                    case "DowntownLuxuryApartmentsD":
                    case "FuturisticUndergroundScienceLabA":
                    case "ExoBaseUndergroundScienceLabB":
                    case "FuturisticScienceLabD":
                    case "FuturisticUndergroundLabG":
                    case "IndustrialLowTechFactoryA":
                    case "SkyscrapersSkyscraperDB":
                        hospitalTargetItems.Add( item );
                        break;
                    case "DowntownParkingGarage":
                    case "BrokenBrokenBuildingB":
                    case "BrokenBrokenBuildingC":
                    case "BrokenBrokenHouseH1":
                    case "CommerceSupermarketA":
                    case "CommerceSupermarketB":
                    case "DowntownDtOfficeI":
                    case "DowntownDtOfficeM2":
                    case "SkyscrapersSkyscraperAb":
                    case "SkyscrapersSkyscraperAa":
                    case "SkyscrapersSkyscraperC":
                        clinicTargetItems.Add( item );
                        break;
                }
            }

            //ArcenDebugging.LogSingleLine( "Health check: hospitals: " + hospitalItems.Count + " clinics: " + clinicItems.Count + 
            //    " h-target: " + hospitalTargetItems.Count + " c-target: " + clinicTargetItems.Count, Verbosity.DoNotShow );

            A5ObjectRoot hospitalType = A5ObjectRootTable.Instance.GetRowByID( "ServicesHospitalB" );
            A5ObjectRoot clinicType = A5ObjectRootTable.Instance.GetRowByID( "ServicesMedicalClinic" );

            while ( hospitalItems.Count < 6 && hospitalTargetItems.Count > 0 )
            {
                MapItem item = hospitalTargetItems.GetRandomAndRemove( Rand );
                item.SimBuilding.SetPrefab( hospitalType.Building, -1f, null, null );
                hospitalItems.Add( item );
            }

            while ( clinicItems.Count < 10 && clinicTargetItems.Count > 0 )
            {
                MapItem item = clinicTargetItems.GetRandomAndRemove( Rand );
                item.SimBuilding.SetPrefab( clinicType.Building, -1f, null, null );
                clinicItems.Add( item );
            }

            //ArcenDebugging.LogSingleLine( "Health final: hospitals: " + hospitalItems.Count + " clinics: " + clinicItems.Count, Verbosity.DoNotShow );

            hospitalItems.Clear();
            hospitalTargetItems.Clear();
            clinicItems.Clear();
            clinicTargetItems.Clear();
        }
        #endregion

        #region DoPerQuarterSecond_NonDemoProjectStarts
        private static void DoPerQuarterSecond_NonDemoProjectStarts()
        {
            if ( !FlagRefs.HasStartedDooms.DuringGameplay_IsTripped && SimMetagame.CurrentChapterNumber >= 2 && SimCommon.TheNetwork != null )
            {
                CityTimelineDoomType doom = SimCommon.GetEffectiveTimelineDoomType();
                if ( doom != null && doom.TryInitializeIfNeeded() )
                {
                    if ( SimMetagame.CurrentChapterNumber == 2 )
                        FlagRefs.Ch2DoomStart.DuringGameplay_IsReadyToBeViewed = true;

                    HandbookRefs.DoomsProvideTimePressure.DuringGame_UnlockIfNeeded( false );
                    HandbookRefs.TimeAndGoalStates.DuringGame_UnlockIfNeeded( false );

                    //this is unusual to do, but these are duplicate text from the above
                    HandbookRefs.DoomsProvideTimePressure.Meta_HasBeenRead = true;
                    HandbookRefs.TimeAndGoalStates.Meta_HasBeenRead = true;
                }
            }
            else if ( FlagRefs.HasStartedDooms.DuringGameplay_IsTripped )
            {
                if ( FlagRefs.VandalizeSpaceportComputers.DuringGame_ExpiresAfterTurn <= 0 )
                    FlagRefs.VandalizeSpaceportComputers.DuringGame_ExpiresAfterTurn = SimCommon.Turn + 50;
            }

            if ( FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped )
            {

            }

            if ( !FlagRefs.SlightlyLessDangerousGame.DuringGameplay_IsInvented && ResourceRefs.MonsterPelts.Current > 0 )
                FlagRefs.SlightlyLessDangerousGame.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );

            if ( FlagRefs.Ch2_MIN_LostKids_ThePrisonHeist?.DuringGame_ActualOutcome != null )
            {
                if ( FlagRefs.Ch2_LostKids_PostHeist.DuringGameplay_TurnStarted <= 0 )
                {
                    FlagRefs.Ch2_LostKids_PostHeist.TryStartProject( true, false );

                    UnlockTable.Instance.GetRowByID( "KnivesAndStabbingWeapons" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                    UnlockTable.Instance.GetRowByID( "JustPutThemSomewhereForNow" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                    ResourceRefs.TraumatizedExCons.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 10010, 12000 ), string.Empty, ResourceAddRule.StoreExcess );
                    int kills = Engine_Universal.PermanentQualityRandom.Next( 1800, 2700 );
                    CityStatisticTable.AlterScore( "CombatKillsHuman", kills );
                    CityStatisticTable.AlterScore( "LiquidMetalKillsInTightSpaces", kills );
                    UpgradeIntTable.Instance.GetRowByID( "Steward" ).DuringGame_DoUpgrade( false );
                    UpgradeIntTable.Instance.GetRowByID( "Steward" ).DuringGame_DoUpgrade( false );
                    UpgradeIntTable.Instance.GetRowByID( "Steward" ).DuringGame_DoUpgrade( false );
                }
            }

            if ( FlagRefs.Ch2_MIN_SolveHomelessness != null && FlagRefs.Ch2_MIN_SolveHomelessness.DuringGame_ActualOutcome != null &&
                JobRefs.NeuralBridge != null && JobRefs.NeuralBridge.DuringGame_NumberFunctional.Display > 0 )
            {
                if ( FlagRefs.Ch2_MIN_AnInexplicableCompulsion != null && FlagRefs.Ch2_MIN_AnInexplicableCompulsion.DuringGameplay_TurnStarted <= 0 )
                {
                    if ( FlagRefs.Ch2_MIN_AnInexplicableCompulsion.TryStartProject( true, true ) )
                    {
                        FlagRefs.IsExperiencingObsession.TripIfNeeded();
                        for ( int i = 0; i < 10; i++ )
                        {
                            UpgradeRefs.Steward.DuringGame_DoUpgrade( false );
                            UpgradeRefs.Cultivator.DuringGame_DoUpgrade( false );
                        }
                        FlagRefs.ComeWithMeImKnockingThisPlaceDown.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );

                        SimCommon.NeedsContemplationTargetRecalculation = true;
                        SimCommon.NeedsExplorationSiteRecalculation = true;
                        SimCommon.NeedsBuildingListRecalculation = true;
                        SimCommon.NeedsStreetSenseTargetRecalculation = true;
                    }
                }
            }

            if ( !FlagRefs.HasNeedForExtremeArmorPiercing.DuringGameplay_IsTripped )
            {
                if ( FlagRefs.BlewUpSecForceStations.DuringGameplay_IsTripped ||
                    FlagRefs.WarRaptorsHaveBeenFreed.DuringGameplay_IsTripped )
                    FlagRefs.HasNeedForExtremeArmorPiercing.TripIfNeeded();
            }
            if ( !FlagRefs.Ch2_IsReadyForGadoliniumMesosilicate.DuringGameplay_IsTripped )
            {
                if ( FlagRefs.HasNeedForExtremeArmorPiercing.DuringGameplay_IsTripped )
                    FlagRefs.Ch2_IsReadyForGadoliniumMesosilicate.TripIfNeeded();
            }

            if ( !FlagRefs.HasUnlockedTheEndOfTime.DuringGameplay_IsTripped && SimMetagame.IntelligenceClass >= 4 )
            {
                FlagRefs.HasUnlockedTheEndOfTime.TripIfNeeded();
                HandbookRefs.TheNatureOfTheEndOfTime.DuringGame_UnlockIfNeeded( true );

                //HandbookRefs.CallingNewTimelines.DuringGame_UnlockIfNeeded( true );
                //HandbookRefs.NegativeTimelineBleedover.DuringGame_UnlockIfNeeded( true );
                //HandbookRefs.PositiveTimelineBleedover.DuringGame_UnlockIfNeeded( true );
            }

            if ( FlagRefs.HasAutomatedHiring.DuringGameplay_IsTripped )
            {
                if ( !FlagRefs.HasDoneInitialAutomatedHiringUnlocks.DuringGameplay_IsTripped )
                {
                    FlagRefs.HasDoneInitialAutomatedHiringUnlocks.TripIfNeeded();
                    FlagRefs.MolecularGenetics.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.ForensicGenetics.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.Zoology.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.Medicine.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.VeterinaryMedicine.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.Botany.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.BionicEngineering.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.Epidemiology.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                    FlagRefs.Neurology.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                }
            }
            else if ( !FlagRefs.WouldBeNiceToAutomateHiring.DuringGameplay_IsTripped && SimMetagame.CurrentChapterNumber >= 2 )
            {
                int count = 0;
                if ( FlagRefs.MolecularGenetics.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.ForensicGenetics.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.Zoology.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.Medicine.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.VeterinaryMedicine.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.Botany.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.BionicEngineering.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.Epidemiology.DuringGameplay_IsInvented )
                    count++;
                if ( FlagRefs.Neurology.DuringGameplay_IsInvented )
                    count++;

                if ( count >= 2 )
                    FlagRefs.WouldBeNiceToAutomateHiring.TripIfNeeded();
            }

            if ( FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
                CityTimelineCrossoverTable.Instance.GetRowByID( "PostApocalypse" )?.Meta_AddCrossoverIfNeeded();

            if ( FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped )
                CityTimelineCrossoverTable.Instance.GetRowByID( "BridgeWithLAKE" )?.Meta_AddCrossoverIfNeeded();
        }
        #endregion

        public void DoPerFrameForMusicTag( DataCalculator Calculator )
        {
            switch ( Calculator.ID )
            {
                case "CalculateCurrentMusicTag":
                    #region CalculateCurrentMusicTag
                    {
                        //if in a debate or dialog, only consider things from here
                        if ( Window_Debate.Instance.IsOpen ||
                            (Window_RewardWindow.Instance.IsOpen && SimCommon.RewardProvider == NPCDialogChoiceHandler.Instance ) )
                        {
                            //if something was set and is not valid, then make sure that is here
                            if ( SimCommon.CurrentDialogMusicTag != Engine_HotM.CurrentPrimaryMusicTag )
                            {
                                Engine_HotM.CurrentPrimaryMusicTag = SimCommon.CurrentDialogMusicTag;
                                Engine_HotM.CurrentPrimaryMusicTrackToPlayFirstWithCurrentTag = SimCommon.CurrentDialogMusicTagFirstTrack;
                            }
                        }
                        //if in a minor event, only consider things from here
                        else if ( SimCommon.CurrentSimpleChoice == MinorEventHandler.Instance )
                        {
                            //if something was set and is not valid, then make sure that is here
                            if ( SimCommon.CurrentMinorEventMusicTag != Engine_HotM.CurrentPrimaryMusicTag )
                            {
                                Engine_HotM.CurrentPrimaryMusicTag = SimCommon.CurrentMinorEventMusicTag;
                                Engine_HotM.CurrentPrimaryMusicTrackToPlayFirstWithCurrentTag = SimCommon.CurrentMinorEventMusicTagFirstTrack;
                            }
                        }
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoPerFrameForMusicTag: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoDuringRelatedResourceCalculation( DataCalculator Calculator )
        {
            switch ( Calculator.ID )
            {
                case "ExtraRelatedResources":
                    #region ExtraRelatedResources
                    {
                        if ( !Engine_HotM.IsDemoVersion )
                        {
                            if ( ResourceRefs.MolecularGeneticists.HasAnyCurrentOrExcess ||
                                ResourceRefs.ForensicGeneticists.HasAnyCurrentOrExcess ||
                                ResourceRefs.Zoologists.HasAnyCurrentOrExcess ||
                                ResourceRefs.Physicians.HasAnyCurrentOrExcess ||
                                ResourceRefs.Veterinarians.HasAnyCurrentOrExcess ||
                                ResourceRefs.Botanists.HasAnyCurrentOrExcess ||
                                ResourceRefs.BionicsEngineers.HasAnyCurrentOrExcess ||
                                ResourceRefs.Epidemiologists.HasAnyCurrentOrExcess ||
                                ResourceRefs.Neurologists.HasAnyCurrentOrExcess )
                                ResourceRefs.Wealth.IsRelatedToCurrentActivities.Construction = true;

                            if ( FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped &&
                                (Window_Sidebar.Instance?.GetShouldDrawThisFrame()??false) &&
                                Window_Sidebar.Instance?.CurrentTab?.ID == "CultActions" )
                                ResourceRefs.CultLoyalty.IsRelatedToCurrentActivities.Construction = true;
                        }

                        if ( Window_HackOptionList.Instance.IsOpen )
                        {
                            ResourceRefs.Creativity.IsRelatedToCurrentActivities.Construction = true;
                            ResourceRefs.Wisdom.IsRelatedToCurrentActivities.Construction = true;
                            ResourceRefs.Determination.IsRelatedToCurrentActivities.Construction = true;
                        }
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoDuringRelatedResourceCalculation: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoDuringAttackPowerCalculation( DataCalculator Calculator, ISimMapActor Attacker, ISimMapActor Target, CalculationType CalcType, MersenneTwister RandIfNotPrediction, bool IsAndroidLeapingFromBeyondAttackRange, float IntensityFromAOE, bool CheckCloakedStatus, bool CheckTakeCoverStatus, bool ImagineWillBeInCover, bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation, int ImagineThisAmountOfAttackerHealthWasLost, bool DoFullPrecalculation, bool SkipCaringAboutRange, ArcenCharacterBufferBase BufferOrNull, ArcenCharacterBufferBase SecondaryBufferOrNull, ref int attackerPhysicalPower, ref int attackerFearAttackPower, ref int attackerArgumentAttackPower )
        {
            switch ( Calculator.ID )
            {
                case "ExtraAttackLogic":
                    #region ExtraAttackLogic
                    {
                        if ( attackerPhysicalPower > 0 && Attacker is ISimNPCUnit possiblyHostileNPC )
                        {
                            if ( possiblyHostileNPC?.FromCohort?.IsInwardLookingMegacorpAlly ?? false )
                            {
                                int stolenSecrets = (int)CommonRefs.MilitarySecretsStolenFromYou.GetScore();
                                if ( stolenSecrets > 60 )
                                    stolenSecrets = 60;

                                int extraDamage = (stolenSecrets * 5);
                                attackerPhysicalPower += extraDamage;

                                //not logged for now, since the player has nowhere to see this
                            }
                        }
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "DoDuringAttackPowerCalculation: Calculators_Common was asked to handle '" + Calculator.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoAfterLanguageChanged( DataCalculator Calculator )
        {
        }
    }
}

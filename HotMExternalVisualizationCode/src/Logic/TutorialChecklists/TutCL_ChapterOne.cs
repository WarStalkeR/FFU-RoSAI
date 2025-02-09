using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class TutCL_ChapterOne : ITutorialChecklistImplementation
    {
        public bool PerFrame_HandleTutorialTaskStackLogic( TutorialChecklist Logic )
        {
            if ( !FlagRefs.HasPlacedNetworkTower.DuringGameplay_IsTripped )
            {
                if ( SimCommon.TheNetwork != null )
                    FlagRefs.HasPlacedNetworkTower.TripIfNeeded();
                else
                {
                    if ( !FlagRefs.Ch1Or2_StartingTimelineLater.DuringGame_IsActive )
                    {
                        FlagRefs.Ch1Or2_StartingTimelineLater.TryStartProject( false, false );
                        SimCommon.NeedsFullSpawnCheckAfterNextActiveProjectsRecalculation = true;
                    }
                    if ( FlagRefs.BuildBunker.DuringGameplay_IsViewingComplete )
                        FlagRefs.Ch0_BuildTower.DuringGameplay_StartIfNeeded();

                    return true; //if we are in chapter one or further, then we did not
                }
            }

            if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                return false;
            if ( !FlagRefs.HasFiguredOutFreestandingStructures.DuringGameplay_IsTripped )
                return false; //if we're already in chapter one, but this is not tripped yet, then skip the checklist

            if ( FlagRefs.IsReadyToTalkToPeople.DuringGameplay_IsTripped )
            {
                FlagRefs.IsChapterOneTutorialDone.TripIfNeeded();
                return false;
            }
            else
            {
                if ( HaveEnoughSlurrySpiders() )
                {
                    if ( ResearchMicrobuilderMiniFabs() )
                        HaveEnoughMicrobuilderMiniFabs();
                }
                if ( FlagRefs.HasFiguredOutNetworkBasics.DuringGameplay_IsTripped )
                {
                    InstallALargePowerGenerator();
                }
            }

            return true;
        }

        #region HaveEnoughSlurrySpiders
        private static bool HaveEnoughSlurrySpiders()
        {
            int slurrySpidersNeeded = GMathIntTable.GetSingleValueByID( "Chapter1_SlurrySpidersPlayerMustHave", 2 );
            bool isComplete = JobRefs.SlurrySpiders.DuringGame_NumberFunctional.Display >= slurrySpidersNeeded;

            ChecklistHelper.RenderObjectiveBoxFormat2( "HaveEnoughSlurrySpiders", "HaveEnoughSlurrySpiders_Explanation", "HaveEnoughSlurrySpiders_StrategyTip",
                isComplete, slurrySpidersNeeded, 1 );

            return isComplete;
        }
        #endregion

        #region ResearchMicrobuilderMiniFabs
        private static bool ResearchMicrobuilderMiniFabs()
        {
            bool isComplete = JobRefs.MicrobuilderMiniFab.DuringGame_IsUnlocked();

            ChecklistHelper.RenderObjectiveBoxFormat1( "ResearchMicrobuilderMiniFabs", "ResearchMicrobuilderMiniFabs_Explanation", "ResearchMicrobuilderMiniFabs_StrategyTip",
                isComplete, InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn() );
            return isComplete;
        }
        #endregion

        #region HaveEnoughMicrobuilderMiniFabs
        private static void HaveEnoughMicrobuilderMiniFabs()
        {
            int miniFabsNeeded = GMathIntTable.GetSingleValueByID( "Chapter1_MicrobuilderMiniFabsPlayerMustHave", 2 );
            bool isComplete = JobRefs.MicrobuilderMiniFab.DuringGame_NumberFunctional.Display >= miniFabsNeeded;

            ChecklistHelper.RenderObjectiveBoxFormat2( "HaveEnoughMicrobuilderMiniFabs", "HaveEnoughMicrobuilderMiniFabs_Explanation", "HaveEnoughMicrobuilderMiniFabs_StrategyTip",
                isComplete, miniFabsNeeded, 1 );
        }
        #endregion

        #region InstallALargePowerGenerator
        private static void InstallALargePowerGenerator()
        {
            bool isComplete = JobRefs.LargeWindGenerator.DuringGame_NumberFunctional.Display >= 1;
            if ( isComplete && !FlagRefs.HasFiguredOutPowerGeneration.DuringGameplay_IsTripped )
            {
                FlagRefs.HasFiguredOutPowerGeneration.TripIfNeeded();
                ActivityScheduler.CalculateActivityWithoutCountdowns_MainThread( Engine_Universal.PermanentQualityRandom ); //make it so that the new units spawn immediately
            }

            if ( !isComplete && !FlagRefs.HasGivenMicrobuilderBonus.DuringGameplay_IsTripped )
            {
                FlagRefs.HasGivenMicrobuilderBonus.TripIfNeeded();
                ResourceRefs.Microbuilders.AlterCurrent_Named( 3200, "Income_Tutorial", ResourceAddRule.IgnoreUntilTurnChange );
            }

            ChecklistHelper.RenderObjectiveBoxFormat2( "InstallALargePowerGenerator", "InstallALargePowerGenerator_Explanation", "InstallALargePowerGenerator_StrategyTip",
                isComplete, 1, 1 );
        }
        #endregion
    }
}

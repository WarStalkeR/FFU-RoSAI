using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Projects_ChapterZero : IProjectHandlerImplementation
    {
        public void HandleLogicForProjectOutcome( ProjectLogic Logic, ArcenCharacterBufferBase BufferOrNull, MachineProject Project, ProjectOutcome OutcomeOrNoneYet,
            MersenneTwister RandOrNull, out bool CanBeCompletedNow )
        {
            CanBeCompletedNow = false;
            if ( Project == null )
            {
                ArcenDebugging.LogSingleLine( "Null project outcome passed to Projects_ChapterZero!", Verbosity.ShowAsError );
                return;
            }

            try
            {

                switch ( Project.ID )
                {
                    #region Ch0_GroupMurder
                    case "Ch0_GroupMurder":
                        {
                            CanBeCompletedNow = SimMetagame.CurrentChapterNumber > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared( Project.ID, RandOrNull );  
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch0_CasualSyndicateFollowers
                    case "Ch0_CasualSyndicateFollowers":
                        {
                            CanBeCompletedNow = SimMetagame.CurrentChapterNumber > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared( Project.ID, RandOrNull );
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch0_MurderedOneColleague
                    case "Ch0_MurderedOneColleague":
                        {
                            CanBeCompletedNow = SimMetagame.CurrentChapterNumber > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared( Project.ID, RandOrNull );
                                    break;
                            }
                        }
                        break;
                    #endregion
                    case "Ch1Or2_StartingTimelineLater":
                        {

                            CanBeCompletedNow = SimCommon.TheNetwork != null;

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                        actor.AddOrRemoveBadge( CommonRefs.WatchedBySecForce, true );
                                    break;
                            }
                        }
                        break;
                    default:
                        if ( !Project.HasShownHandlerMissingError )
                        {
                            Project.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "Projects_ChapterZero: No handler is set up for '" + Project.ID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Project.HasShownCaughtError )
                {
                    Project.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "Projects_ChapterZero: Error in '" + Project.ID + "': " + e, Verbosity.ShowAsError );
                }
            }
        }

        #region FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared
        private void FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared( string ProjectID, MersenneTwister Rand )
        {
            bool ambushIsGang = false;
            bool ambushIsRebel = false;

            switch ( ProjectID )
            {
                //A criminal syndicate is shadowing you in the wake up your mass murder.
                case "Ch0_GroupMurder":
                    {
                        ambushIsGang = true;

                        //if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                        {
                            //early on, make sure everyone is watched at all times
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                            {
                                actor.AddOrRemoveBadge( CommonRefs.WatchedByCriminals, true );
                                actor.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                            }
                        }
                    }
                    break;
                //A criminal syndicate is shadowing you for reasons that are unclear.
                case "Ch0_CasualSyndicateFollowers":
                    {
                        ambushIsGang = true;

                        //if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                        {
                            //early on, make sure everyone is watched at all times
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                actor.AddOrRemoveBadge( CommonRefs.WatchedByCriminals, true );
                        }
                    }
                    break;
                //SecForce seems to be following you for reasons unrelated to your transgressions.
                case "Ch0_MurderedOneColleague":
                    {
                        ambushIsRebel = true;

                        //if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                        {
                            //early on, make sure everyone is watched at all times
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                actor.AddOrRemoveBadge( CommonRefs.WatchedBySecForce, true );
                        }
                    }
                    break;
            }

            if ( SimCommon.Turn == 1 )
            {
                //on turn 1, you are extra tired
                if ( ResourceRefs.MentalEnergy.Current > 4 )
                {
                    int overage = (int)ResourceRefs.MentalEnergy.Current - 4;
                    ResourceRefs.MentalEnergy.AlterCurrent_Named( -overage, "Expense_ExhaustedByNewExperiences", ResourceAddRule.IgnoreUntilTurnChange );
                }

                if ( FlagRefs.Ch0_UnitMovement.DuringGameplay_State == CityTaskState.Complete && !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped )
                {
                    FlagRefs.HasUnlockedOuterRadialButtons.TripIfNeeded();
                    FlagRefs.IndicateFirstOuterRadialButton.TripIfNeeded();
                }
            }
            else if ( SimCommon.Turn == 2 )
            {
                if ( !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped )
                {
                    FlagRefs.HasUnlockedOuterRadialButtons.TripIfNeeded();
                    FlagRefs.IndicateFirstOuterRadialButton.TripIfNeeded();
                }

                //if we have been nice on the first turn and did not shoot anyone yet, give us StreetSense and look for cover
                if ( !FlagRefs.ShotACh0Tail.DuringGameplay_IsTripped )
                {
                    CommonRefs.Observer.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, false, false, true, false );
                    FlagRefs.Ch0_RunTalkOrShoot.DuringGameplay_CompleteIfActive();
                    FlagRefs.Ch0_FindSafety.DuringGameplay_StartIfNeeded();
                }
                else
                {
                    if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                    {
                        FlagRefs.Ch0_RunTalkOrShoot.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch0_FindSafety.DuringGameplay_CompleteIfActive();
                        FlagRefs.Ch0_TrapHasBeenSprung.TripIfNeeded();
                        if ( ambushIsGang )
                            FlagRefs.Ch0_AmbushedByGang_Killer.DuringGameplay_IsReadyToBeViewed = true;
                        if ( ambushIsRebel )
                            FlagRefs.Ch0_AmbushedByRebels_Killer.DuringGameplay_IsReadyToBeViewed = true;

                        //open up StreetSense mode
                        CommonRefs.Observer.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, false, false, true, false );

                        //we shot someone already, so make the gang show up quickly, or something along those lines
                        ActivityScheduler.DoFullSpawnCheckForAllManagers_FromAnyThread( false, Rand );
                    }
                }
            }

            if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped &&
                (FlagRefs.FailedToFindSomeplaceSafe.GetScore() == 1 && !FlagRefs.HasLearnedThereIsNoSafetyWithHumans.DuringGameplay_IsTripped) &&
                !FlagRefs.Ch0_DejaVu.DuringGameplay_IsViewingComplete )
            {
                FlagRefs.Ch0_DejaVu.DuringGameplay_IsReadyToBeViewed = true;
            }

            if ( !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped && 
                ( FlagRefs.FailedToFindSomeplaceSafe.GetScore() >= 2 || FlagRefs.HasLearnedThereIsNoSafetyWithHumans.DuringGameplay_IsTripped ) )
            {
                FlagRefs.Ch0_TrapHasBeenSprung.TripIfNeeded();
                FlagRefs.Ch0_RunTalkOrShoot.DuringGameplay_CompleteIfActive();
                FlagRefs.Ch0_FindSafety.DuringGameplay_CompleteIfActive();
                FlagRefs.HasLearnedThereIsNoSafetyWithHumans.TripIfNeeded();
                if ( ambushIsGang )
                    FlagRefs.Ch0_AmbushedByGang_Peaceful.DuringGameplay_IsReadyToBeViewed = true;
                if ( ambushIsRebel )
                    FlagRefs.Ch0_AmbushedByRebels_Peaceful.DuringGameplay_IsReadyToBeViewed = true;
                ActivityScheduler.DoFullSpawnCheckForAllManagers_FromAnyThread( false, Rand );
            }

            if ( ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped || !HandbookRefs.ExamineEnemies.Meta_HasBeenUnlocked ) &&
                (FlagRefs.Ch0_AmbushedByGang_Peaceful.DuringGameplay_IsViewingComplete || FlagRefs.Ch0_AmbushedByRebels_Peaceful.DuringGameplay_IsViewingComplete ||
                FlagRefs.Ch0_AmbushedByGang_Killer.DuringGameplay_IsViewingComplete || FlagRefs.Ch0_AmbushedByRebels_Killer.DuringGameplay_IsViewingComplete) )
            {
                FlagRefs.HasFiguredOutResearch.TripIfNeeded(); //do this as the interface is opened, so the player can see it
                ResearchDomainTable.Instance.GetRowByID( "GenerateIdeasChapterZero" ).AddMoreInspiration( 1 );

                HandbookRefs.ExamineEnemies.DuringGame_UnlockIfNeeded( true );
                UnlockRefs.TacticalCover.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                UnlockRefs.FieldRepairs.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
            }

            if ( HandbookRefs.ExamineEnemies.Meta_HasBeenRead && FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_State == CityTaskState.NeverStarted )
            {
                FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_StartIfNeeded();
            }

            if ( FlagRefs.Ch0_ReadHandbookEntry.DuringGameplay_State == CityTaskState.Complete && FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_State == CityTaskState.NeverStarted )
            {
                FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_StartIfNeeded();
            }

            if ( FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_State == CityTaskState.Complete && FlagRefs.HasAlreadyIndicatedForcesSidebar.DuringGameplay_IsTripped &&
                !FlagRefs.InvestigationsIntro.DuringGameplay_IsViewingComplete && CommonRefs.ImposingTowerType.DuringGame_IsUnlocked() )
            {
                FlagRefs.InvestigationsIntro.DuringGameplay_IsReadyToBeViewed = true;
            }

            if ( FlagRefs.Ch0_InvestigateViaMap.DuringGameplay_State == CityTaskState.NeverStarted && SimCommon.VisibleAndroidInvestigations.Count > 0 )
            {
                if ( SimCommon.VisibleAndroidInvestigations[0]?.InvestigationAttemptsSoFar > 0 )
                    FlagRefs.Ch0_InvestigateViaMap.DuringGameplay_StartIfNeeded();
            }

            if ( FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped && FlagRefs.Ch0_BuildTower.DuringGameplay_State == CityTaskState.NeverStarted )
            {
                FlagRefs.Ch0_BuildTower.DuringGameplay_StartIfNeeded();
            }
        }
        #endregion FirstProjectTrio_DoAnyPerQuarterSecondLogicWhileProjectActive_Shared

        public void HandleStreetItem( ProjectOutcome Outcome, ProjectOutcomeStreetSenseItem StreetItem, ISimBuilding Building, ISimMachineActor Actor, 
            MersenneTwister Rand, ArcenDoubleCharacterBuffer PopupBufferOrNull )
        {
            ArcenDebugging.LogSingleLine( "HandleStreetItem is not set up for anything in Projects_ChapterZero.", Verbosity.ShowAsError );
        }
    }
}

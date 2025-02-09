using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class MetaFlags_Basics : IMetaFlagImplementation
    {
        public void DoPerSecondEvenIfAlreadyTripped( MetaFlag Flag, MersenneTwister RandForThisSecond )
        {
            switch ( Flag.ID )
            {
                case "HasPassedChapterOneTierTwo":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    //otherwise trip if both the finding-food and finding-water projects are done
                    if ( MachineProjectTable.Instance.GetRowByID( "Ch1_FindingWater" ).DuringGame_ActualOutcome != null &&
                        MachineProjectTable.Instance.GetRowByID( "Ch1_FindingFood" ).DuringGame_ActualOutcome != null )
                    {
                        //start this project
                        MachineProjectTable.Instance.GetRowByID( "Ch1_MIN_GrandTheftAero" )?.TryStartProject( true, true );
                        Flag.TripIfNeeded();
                        ActivityScheduler.DoFullSpawnCheckForAllManagers_FromAnyThread( false, RandForThisSecond );
                    }
                    break;
                case "HasPassedChapterOneTierThree":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    //otherwise trip if these two conditions are both true
                    if ( FlagRefs.Ch1_MIN_NovelAir.DuringGame_ActualOutcome != null && FlagRefs.HasPerformedAnExtraction.DuringGameplay_IsTripped )
                    {
                        Flag.TripIfNeeded();
                        ActivityScheduler.DoFullSpawnCheckForAllManagers_FromAnyThread( false, RandForThisSecond );
                    }
                    break;
                case "HasPassedChapterOneTierFour":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    //otherwise trip if these two conditions are both true
                    if ( FlagRefs.Ch1_BuildingABetterBrain.DuringGame_ActualOutcome != null && SimMetagame.IntelligenceClass == 2 )
                    {
                        Flag.TripIfNeeded();
                        //do not trigger the next thing that quickly
                        //ActivityScheduler.DoFullSpawnCheckForAllManagers_FromAnyThread( false, RandForThisSecond );
                    }
                    break;
                case "FlyingFactoryInspiration":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    //otherwise trip if both these conditions are true
                    if ( FlagRefs.UnobservedFlight.DuringGameplay_IsInvented && FlagRefs.BulkProduction.DuringGameplay_IsInvented )
                    {
                        Flag.TripIfNeeded();

                        //then give me this tech and also start this project
                        UnlockTable.Instance.GetRowByID( "FactoryInTheSky" )?.DuringGameplay_ReadyForInventionIfNeedBe( CommonRefs.FollowUpIdeaInspiration, true );
                        MachineProjectTable.Instance.GetRowByID( "Ch1_MIN_FlyingFactories" )?.TryStartProject( true, true );
                    }
                    break;
                case "FlexibleAirspaceInspiration":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    //otherwise trip if this condition is true
                    if ( FlagRefs.AwarenessOfFilth.DuringGameplay_IsInvented )
                    {
                        Flag.TripIfNeeded();

                        //then start this project
                        MachineProjectTable.Instance.GetRowByID( "Ch1_MIN_FlexibleAirspace" )?.TryStartProject( true, true );
                    }
                    break;
                case "HasUnlockedOuterRadialButtons":
                    if ( Flag.DuringGameplay_IsTripped )
                        break; //nothing to do if already tripped

                    if ( SimMetagame.CurrentChapterNumber >= 1 )
                    {
                        Flag.TripIfNeeded();
                        FlagRefs.IndicateFirstOuterRadialButton.TripIfNeeded();
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "MetaFlags_Basics: Called DoPerSecondEvenIfAlreadyTripped for '" + Flag.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoLogicAfterCleared( MetaFlag Flag )
        {
        }

        public void DoLogicAfterTripped( MetaFlag Flag )
        {
        }
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class CityFlags_Basics : ICityFlagImplementation
    {
        public void DoPerSecondEvenIfAlreadyTripped( CityFlag Flag, MersenneTwister RandForThisSecond )
        {
            switch ( Flag.ID )
            {
                case "HasRoomForMoreShelteredHumans":
                    if ( SimCommon.AllNPCsThatAreDeepThreatToShelteredHumans.Count > 0 )
                        Flag.UnTripIfNeeded(); //if there is a deep threat around, then no gathering of humans
                    else
                        Flag.SetTripTo( ResourceRefs.ShelteredHumans.EffectiveHardCapStorageAvailable > 10 ); //note!  It won't do partial buildings, so requiring at least space of 10 is good for keeping units from being stuck.
                    break;
                case "HasRoomForMoreTormentedHumans":
                    Flag.SetTripTo( ResourceRefs.TormentedHumans?.EffectiveHardCapStorageAvailable >= 1 );
                    break;
                case "HasRoomForMoreApartmentFurnishings":
                    {
                        ActorDataType furnished = ActorRefs.FurnishedApartments;
                        if ( furnished.DuringGameplay_StructuresUsingThis.Count > 0 )
                        {
                            bool hadAnyRoom = false;
                            foreach ( MachineStructure structure in furnished.DuringGameplay_StructuresUsingThis.GetDisplayList() )
                            {
                                int available = structure.GetActorDataLostFromMax( ActorRefs.FurnishedApartments, true );
                                if ( available > 0 )
                                {
                                    hadAnyRoom = true;
                                    break;
                                }
                            }
                            Flag.SetTripTo( hadAnyRoom );
                        }
                        else
                            Flag.UnTripIfNeeded();
                    }
                    break;
                case "HasPlacedNetworkTower":
                    if ( !Flag.DuringGameplay_IsTripped )
                    {
                        if ( SimCommon.TheNetwork != null )
                            Flag.SetTripTo( SimCommon.TheNetwork.Tower != null );
                    }
                    break;
                case "HasEstablishedVREnvironment":
                    if ( !Flag.DuringGameplay_IsTripped )
                    {
                        if ( SimCommon.GetIsVirtualRealityEstablished() )
                            Flag.TripIfNeeded();
                    }
                    break;
                case "ShouldShowRevelations":
                    if ( !WorldSaveLoad.IsLoadingAtTheMoment && SimCommon.SecondsSinceLoaded > 5 )
                    {
                        bool shouldBeShown = true;
                        foreach ( ContemplationType cont in CommonRefs.TimelineGoalCriticalPath.List )
                        {
                            if ( cont.DuringGame_ShouldBeAvailable && !cont.DoesNotCountForTimelineContinuation && cont.DuringGame_CurrentChosenBuilding.Display != null &&
                                !cont.DuringGame_HasBeenCompleted )
                            {
                                shouldBeShown = false;
                                // ArcenDebugging.LogSingleLine( "ShouldShowRevelations false from " + cont.ID, Verbosity.DoNotShow );
                                break;
                            }
                        }
                        if ( shouldBeShown )
                        {
                            foreach ( ContemplationType cont in CommonRefs.PathToTimelineGoalStart.List )
                            {
                                if ( cont.DuringGame_ShouldBeAvailable && !cont.DoesNotCountForTimelineContinuation && cont.DuringGame_CurrentChosenBuilding.Display != null &&
                                    !cont.DuringGame_HasBeenCompleted )
                                {
                                    shouldBeShown = false;
                                    //ArcenDebugging.LogSingleLine( "ShouldShowRevelations false from " + cont.ID, Verbosity.DoNotShow );
                                    break;
                                }
                            }
                        }

                        if ( !shouldBeShown )
                        {
                            int countOf = 0;
                            foreach ( TimelineGoal goal in CommonRefs.Tier1Goals.GoalsList )
                            {
                                if ( goal.GetAreAnyPathsCompleteInThisTimeline() )
                                {
                                    countOf++;
                                    if ( countOf >= 2 )
                                    {
                                        shouldBeShown = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if ( FlagRefs.Ch2_IsCivilWarOngoing.DuringGameplay_IsTripped )
                            shouldBeShown = true;

                        Flag.SetTripTo( shouldBeShown );
                    }
                    break;
                case "ShouldShowTheComingWar":
                    if ( !WorldSaveLoad.IsLoadingAtTheMoment && SimCommon.SecondsSinceLoaded > 5 )
                    {
                        bool shouldBeShown = true;

                        if ( FlagRefs.Ch2_IsWW4Ongoing.DuringGameplay_IsTripped ||
                            FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped )
                            shouldBeShown = false;
                        else
                        {
                            if ( !FlagRefs.LiquidMetalWoodsman.DuringGameplay_IsTripped )
                                shouldBeShown = false;
                            else if ( !FlagRefs.CrossoverWithHomoGrandien.DuringGameplay_IsTripped )
                                shouldBeShown = false;
                            else if ( KeyContactRefs.BlackMarketAssistant.DuringGame_IsDead )
                                shouldBeShown = false;
                            else if ( FlagRefs.Ch2_VorsiberAssassinationsHappened.DuringGameplay_IsTripped )
                                shouldBeShown = false;
                        }

                        Flag.SetTripTo( shouldBeShown );
                    }
                    break;
                case "ShouldShowAnimalPalace":
                    if ( !WorldSaveLoad.IsLoadingAtTheMoment && SimCommon.SecondsSinceLoaded > 5 )
                    {
                        bool shouldBeShown = false;

                        if ( FlagRefs.IsAwareOfCats.DuringGameplay_IsTripped )
                            shouldBeShown = true;
                        else if ( FlagRefs.IsAwareOfDogs.DuringGameplay_IsTripped )
                            shouldBeShown = true;

                        Flag.SetTripTo( shouldBeShown );
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "CityFlags_Basics: Called DoPerSecondEvenIfAlreadyTripped for '" + Flag.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoLogicAfterCleared( CityFlag Flag )
        {
        }

        public void DoLogicAfterTripped( CityFlag Flag )
        {
        }
    }
}

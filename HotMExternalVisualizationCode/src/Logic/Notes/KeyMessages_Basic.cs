using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class KeyMessages_Basic : IKeyMessageImplementation
    {
        public void DoPerQuarterSecond_WhenNotYetReadyToBeViewed( IKeyMessage Message, MersenneTwister Rand )
        {
            try
            {
                switch ( Message.GetKeyMessageID() )
                {
                    case "OKM-BuildBunker":
                        {
                            if ( SimCommon.TheNetwork != null || SimMetagame.CurrentChapterNumber >= 2 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( ResourceRefs.NanoSeed.Current > 0 && CommonRefs.AHomeOfYourOwn.DuringGameplay_IsInvented/* &&
                                    SimCommon.TotalMachineVehiclesExistingOrInProgress > 0*/ )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-Rank2City":
                        {
                            if ( SimCommon.TheNetwork != null )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else if ( SimMetagame.CurrentChapterNumber > 1 )
                            {
                                if ( ResourceRefs.NanoSeed.Current > 0 && CommonRefs.AHomeOfYourOwn.DuringGameplay_IsInvented/* &&
                                    SimCommon.TotalMachineVehiclesExistingOrInProgress > 0*/ )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-NotABunker":
                        {
                            if ( SimMetagame.CurrentChapterNumber == 1 && FlagRefs.HasPlacedNetworkTower.DuringGameplay_IsTripped )
                                Message.SetIsReadyToBeViewed();
                        }
                        break;
                    case "OKM-FirstProductionChain":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 )
                                {
                                    int spidersNeeded = GMathIntTable.GetSingleValueByID( "Chapter1_SlurrySpidersPlayerMustHave", 4 );
                                    MachineJob job = JobRefs.SlurrySpiders;
                                    if ( (job.DuringGame_NumberFunctional.Display + job.DuringGame_NumberInstalling.Display) >= spidersNeeded )
                                        Message.SetIsReadyToBeViewed();
                                }
                            }
                        }
                        break;
                    case "OKM-PowerGeneration":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 )
                                {
                                    int slurrySpidersNeeded = GMathIntTable.GetSingleValueByID( "Chapter1_SlurrySpidersPlayerMustHave", 2 );
                                    int miniFactoriesNeeded = GMathIntTable.GetSingleValueByID( "Chapter1_MicrobuilderMiniFabsPlayerMustHave", 2 );
                                    if ( JobRefs.SlurrySpiders.DuringGame_NumberFunctional.Display >= slurrySpidersNeeded &&
                                        JobRefs.MicrobuilderMiniFab.DuringGame_NumberFunctional.Display >= miniFactoriesNeeded )
                                        Message.SetIsReadyToBeViewed();
                                }
                            }
                        }
                        break;
                    case "OKM-ACrowdIsForming":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && FlagRefs.HasFiguredOutPowerGeneration.DuringGameplay_IsTripped )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-AlternativesToPhysicalViolence":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && ( FlagRefs.Ch1_MIN_PrismTung.DuringGame_ActualOutcome != null ||
                                    FlagRefs.Ch1_TarkVPVisit4.DuringGame_HasHandled || FlagRefs.Ch1_EspiaVPVisit5.DuringGame_HasHandled ) )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-ThinkingForYourself":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 || FlagRefs.TheThinker.DuringGameplay_IsInvented )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && FlagRefs.AndroidSecurityPatch.DuringGameplay_HasEverCompleted &&
                                    FlagRefs.Ch1_MIN_CommandMode.DuringGame_ActualOutcome != null )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-HumansAreLivestock":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( FlagRefs.Ch1_Flamethrower.DuringGame_ActualOutcome != null )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-RepairSpiders":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 || FlagRefs.HasThoughtOfRepairSpiders.DuringGameplay_IsTripped )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && World.Buildings.GetBuildingsWithDamagedMachineStructures().Count > 0 )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-RepairSpidersBulk":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 || FlagRefs.HasThoughtOfRepairSpiders.DuringGameplay_IsTripped )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimCommon.TotalOnlineBulkUnitSquads > 0 )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-ContrabandScanned":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && FlagRefs.HasBeenContrabandScanned.DuringGameplay_IsTripped )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    case "OKM-EmergentPersonality":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 && 
                                    ( FlagRefs.Ch1_TentElimination.DuringGameplay_TurnStarted > 0 || FlagRefs.Ch1_Flamethrower.DuringGameplay_TurnStarted > 0) )
                                    Message.SetIsReadyToBeViewed();
                            }
                        }
                        break;
                    //case "OKM-ParasiticPower":
                    //    {
                    //        if ( SimMetagame.CurrentChapterNumber > 1 || FlagRefs.ParasiticTower.DuringGameplay_IsInvented )
                    //            Message.SetAsDoesNotNeedToBeViewed();
                    //        else
                    //        {
                    //            if ( SimMetagame.CurrentChapterNumber == 1 && FlagRefs.AwarenessOfFilth.DuringGameplay_IsInvented )
                    //                Message.SetIsReadyToBeViewed(); //whoops, we passed it.  Catch up
                    //            else if ( SimCommon.TerritoryControlFlags.Count > 0 )
                    //                Message.SetIsReadyToBeViewed(); //here's the actual impetus for most new people
                    //        }
                    //    }
                    //    break;
                    case "OKM-AfterHackingMech":
                        {
                            if ( SimMetagame.CurrentChapterNumber > 1 )
                                Message.SetAsDoesNotNeedToBeViewed();
                            else
                            {
                                if ( SimMetagame.CurrentChapterNumber == 1 )
                                {
                                    if ( FlagRefs.HasHackedFirstMechInAnyWay.DuringGameplay_IsTripped )
                                        Message.SetIsReadyToBeViewed();
                                }
                            }
                        }
                        break;
                    case "OKM-WeShouldGatherLate":
                        {
                            if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                Message.SetIsReadyToBeViewed();
                        }
                        break;
                    case "OKM-Ch0_AmbushedByGang_Peaceful":
                    case "OKM-Ch0_AmbushedByGang_Killer":
                    case "OKM-Ch0_AmbushedByRebels_Peaceful":
                    case "OKM-Ch0_AmbushedByRebels_Killer":
                        break; //nothing to do
                    //note: use has_no_code="true" for ones without any special code
                    default:
                        if ( !Message.HasShownHandlerMissingError )
                        {
                            Message.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "KeyMessages_Basic: No handler is set up for '" + Message.GetKeyMessageID() + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Message.HasShownCaughtError )
                {
                    Message.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "KeyMessages_Basic: Error in '" + Message.GetKeyMessageID() + "': " + e, Verbosity.ShowAsError );
                }
            }
        }

        public void DoOnOpenWindowForMessage( IKeyMessage Message )
        {
            try
            {
                //switch ( Message.GetKeyMessageID() )
                //{
                //    case "OKM-FurtherResearchRequired":
                //        {
                //            if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                //            {
                //                FlagRefs.HasFiguredOutResearch.TripIfNeeded(); //do this as the interface is opened, so the player can see it
                //                ResearchDomainTable.Instance.GetRowByID( "GenerateIdeasChapterZero" ).AddMoreInspiration( 1 );
                //            }
                //        }
                //        break;
                //}
            }
            catch ( Exception e )
            {
                if ( !Message.HasShownCaughtError )
                {
                    Message.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "KeyMessages_Basic: DoOnOpenWindowForMessage Error in '" + Message.GetKeyMessageID() + "': " + e, Verbosity.ShowAsError );
                }
            }
        }
    }
}

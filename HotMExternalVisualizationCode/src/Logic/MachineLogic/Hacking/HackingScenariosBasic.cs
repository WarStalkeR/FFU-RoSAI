using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.Hacking;

namespace Arcen.HotM.ExternalVis
{
    using h = Window_Hacking;
    using scene = HackingScene;

    public class HackingScenariosBasic : IHackingScenarioImplementation
    {
        public void TryHandleHackingScenarioLogic( HackingScenarioType Scenario, HackingScenarioLogic Logic, MersenneTwister Rand )
        {
            if ( Scenario == null )
                return; //was error

            if ( Logic == HackingScenarioLogic.DoInitialPopulation )
            {
                scene.IsInfiltrationSysOpScenario = false;
                scene.CurrentHack = null;
            }

            switch ( Scenario.ID )
            {
                case "TitanMechProbeComms":
                    #region TitanMechProbeComms
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    scene.HackingSeedDifficulty = 100; 
                                    scene.HackingBlockagesDifficulty = 10;

                                    bool isJointHacking = false;
                                    if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_HackTheTitan" ).DuringGame_ActualOutcome == null &&
                                        MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_HackTheTitan" ).DuringGameplay_TurnStarted > 0 )
                                    {
                                        isJointHacking = true;
                                        scene.HackingSeedDifficulty = 1200;
                                        scene.HackingBlockagesDifficulty = 60;
                                        (scene.TargetUnit as ISimNPCUnit)?.ChangeCohort( NPCCohortTable.Instance.GetRowByID( "LAKE" ) );
                                    }

                                    CityStatisticTable.AlterScore( "CommsProbeAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "CommsProbeLog_Online" ) );

                                    HackingHelper.PopulateStandardNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateStandardFirstShards( Rand );

                                    if ( isJointHacking )
                                    {
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "LAKE" ), false, 3, 2, 3, Rand, 0 );
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "LAKE" ), false, 3, 2, 3, Rand, 0 );
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "LAKE" ), false, 3, 2, 3, Rand, 0 );

                                    }
                                    else if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_ProbeTheTitan" ).DuringGame_ActualOutcome == null )
                                    {
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "TitanCommsLeak" ), false, 3, 2, 3, Rand, 0 );
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "TitanCommsGap" ), false, 3, 2, 3, Rand, 0 );
                                    }
                                    else
                                    {
                                        NPCUnitStance deactivatedStance = NPCUnitStanceTable.Instance.GetRowByID( "SpaceNationTitanDeactivated" );

                                        if ( (scene.TargetUnit as ISimNPCUnit)?.Stance != deactivatedStance )
                                        {
                                            HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "TitanHumanContact" ), false, 3, 2, 3, Rand, 0 );
                                            HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "TitanSystemContact" ), false, 3, 2, 3, Rand, 0 );
                                        }
                                        else
                                            HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "TitanFreedSystemContact" ), false, 3, 2, 3, Rand, 0 );
                                    }

                                    //seed the actual daemon bits
                                    HackingHelper.FillDaemonTypeBagFromTagUse( "InitialDaemons" );
                                    int remainingDifficulty = scene.HackingSeedDifficulty;
                                    int loopCount = 1000;
                                    while ( remainingDifficulty > 0 && loopCount-- > 0 )
                                    {
                                        HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                        if ( daemonType != null )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 0 );
                                            if ( newDaemon != null )
                                                remainingDifficulty -= daemonType.ReducesDangerLevelBy;
                                        }
                                    }
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                {
                                    if ( !scene.HasLoggedFailure )
                                    {
                                        if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_HackTheTitan" ).DuringGame_ActualOutcome == null &&
                                            MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_HackTheTitan" ).DuringGameplay_TurnStarted > 0 )
                                        {
                                            int hostileDaemonCount = 0;
                                            foreach ( Daemon daemon in scene.Daemons )
                                            {
                                                if ( daemon?.DaemonType?.CanDaemonBeHuntedByFriendlyDaemons ?? false )
                                                {
                                                    hostileDaemonCount++;
                                                    break;
                                                }
                                            }

                                            if ( hostileDaemonCount == 0 )
                                            {
                                                CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );

                                                NPCEvent minorEvent = NPCEventTable.Instance.GetRowByID( "LAKE_HackingSuccess" );

                                                //START_MINOR_EVENT
                                                minorEvent.ClearForMinorEvent();
                                                MinorEventHandler.TryStart( scene.HackerUnit, minorEvent, null, minorEvent.MinorData.SpecificCohort, true );
                                            }
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    return;
                #endregion
            }

            int hackingSkill = scene.HackerUnit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
            int resistance = scene.TargetUnit.GetActorDataCurrent( ActorRefs.NPCHackingResistance, true );

            int difficulty = resistance;
            if ( hackingSkill < resistance )
            {
                int diff = resistance - hackingSkill;
                diff *= diff;

                difficulty += diff;
            }

            scene.HackingSeedDifficulty = difficulty;

            scene.HackingBlockagesDifficulty = scene.HackingSeedDifficulty * 10;
            if ( scene.HackingBlockagesDifficulty < 100 )
                scene.HackingBlockagesDifficulty = 100;
            if ( scene.HackingBlockagesDifficulty > 3000 )
                scene.HackingBlockagesDifficulty = 3000;

            if ( scene.HackingSeedDifficulty < 100 )
                scene.HackingSeedDifficulty = 100;            

            switch ( Scenario.ID )
            {
                case "SimpleMechIntro":
                    #region SimpleMechIntro
                    {
                        switch (Logic)
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "HackingAttempts", 1 );
                                    scene.AddToHackingHistory( Lang.Get( "HackingLog_Online" ) );
                                    HackingHelper.PopulateStandardNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateStandardFirstShards( Rand );

                                    int determinationShortage = 20 - (int)ResourceRefs.Determination.Current;
                                    if ( determinationShortage > 0 )
                                        ResourceRefs.Determination.AlterCurrent_Named( determinationShortage, "Income_Other", ResourceAddRule.IgnoreUntilTurnChange );

                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "LeafNode" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "Gnath" ), false, 5, 2, 1, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "CodePriest" ), false, 5, 2, 1, Rand, 0 );

                                    FlagRefs.ResetHackingTutorial();
                                    FlagRefs.HackTut_OverallGoal.DuringGameplay_StartIfNeeded();
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                    #endregion
                case "SimpleMech":
                    #region SimpleMech
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "HackingAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "HackingLog_Online" ) );

                                    HackingHelper.PopulateStandardNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateStandardFirstShards( Rand );

                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "LeafNode" ), false, 3, 2, 3, Rand, 0 );

                                    //seed the actual daemon bits
                                    HackingHelper.FillDaemonTypeBagFromTagUse( "InitialDaemons" );
                                    int remainingDifficulty = scene.HackingSeedDifficulty;
                                    int loopCount = 1000;
                                    while ( remainingDifficulty > 0 && loopCount-- > 0 )
                                    {
                                        HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                        if ( daemonType != null )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 0 );
                                            if ( newDaemon != null )
                                                remainingDifficulty -= daemonType.ReducesDangerLevelBy;
                                        }
                                    }

                                    //seed the mech-specific things that we can hack
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "ArmorPlatingServicePanels" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "WeaponSystems" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "HydraulicActuators" ), false, 3, 2, 3, Rand, 0 );
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                #endregion
                case "BionicHuman":
                    #region BionicHuman
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "HackingAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "HackingLog_Online" ) );

                                    HackingHelper.PopulateStandardNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateStandardFirstShards( Rand );

                                    //seed the actual daemon bits
                                    HackingHelper.FillDaemonTypeBagFromTagUse( "InitialDaemons" );
                                    int remainingDifficulty = scene.HackingSeedDifficulty;
                                    int loopCount = 1000;
                                    while ( remainingDifficulty > 0 && loopCount-- > 0 )
                                    {
                                        HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                        if ( daemonType != null )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 0 );
                                            if ( newDaemon != null )
                                                remainingDifficulty -= daemonType.ReducesDangerLevelBy;
                                        }
                                    }

                                    //seed the bionic-human-specific things that we can hack
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "AugmentedOrgans" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "AdrenalineRegulator" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "AugmentedVision" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "BionicUplink" ), false, 3, 2, 3, Rand, 0 );
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                #endregion
                case "TitanMechProbeComms":
                    #region TitanMechProbeComms
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "HackingAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "HackingLog_Online" ) );

                                    HackingHelper.PopulateStandardNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateStandardFirstShards( Rand );

                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "RingNode" ), false, 3, 2, 3, Rand, 0 );

                                    //seed the actual daemon bits
                                    HackingHelper.FillDaemonTypeBagFromTagUse( "InitialDaemons" );
                                    int remainingDifficulty = scene.HackingSeedDifficulty;
                                    int loopCount = 1000;
                                    while ( remainingDifficulty > 0 && loopCount-- > 0 )
                                    {
                                        HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                        if ( daemonType != null )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 0 );
                                            if ( newDaemon != null )
                                                remainingDifficulty -= daemonType.ReducesDangerLevelBy;
                                        }
                                    }

                                    //seed the mech-specific things that we can hack
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "ArmorPlatingServicePanels" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "WeaponSystems" ), false, 3, 2, 3, Rand, 0 );
                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "HydraulicActuators" ), false, 3, 2, 3, Rand, 0 );
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "HackingScenariosBasic: Called TryHandleHackingScenarioLogic for '" + Scenario.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return; //was error
            }
        }
    }
}

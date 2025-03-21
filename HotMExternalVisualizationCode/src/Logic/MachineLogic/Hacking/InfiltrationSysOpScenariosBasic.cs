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

    public class InfiltrationSysOpScenariosBasic : IHackingScenarioImplementation
    {
        public void TryHandleHackingScenarioLogic( HackingScenarioType Scenario, HackingScenarioLogic Logic, MersenneTwister Rand )
        {
            if ( Scenario == null )
                return; //was error

            if ( Logic == HackingScenarioLogic.DoInitialPopulation )
            {
                scene.IsInfiltrationSysOpScenario = true;

                int netControlSkill = scene.HackerUnit.GetActorDataCurrent( ActorRefs.UnitNetControl, true );
                int resistance = scene.TargetDifficultyForAlly;
                if ( resistance < 100 )
                    resistance = 100;

                int difficulty = resistance;
                if ( netControlSkill < resistance )
                {
                    int diff = resistance - netControlSkill;
                    diff *= diff;

                    difficulty += diff;
                }

                scene.HackingSeedDifficulty = difficulty;

                scene.HackingBlockagesDifficulty = scene.HackingSeedDifficulty * 20;
                if ( scene.HackingBlockagesDifficulty < 100 )
                    scene.HackingBlockagesDifficulty = 100;

                if ( scene.HackingSeedDifficulty < 100 )
                    scene.HackingSeedDifficulty = 100; ;
            }                    

            switch ( Scenario.ID )
            {
                case "DataCenterInfiltrationSysOp":
                    #region DataCenterInfiltrationSysOp
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "InfiltrationSysOpAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "InfiltrationSysOpLog_Online" ) );

                                    HackingHelper.PopulateInfiltrationSysOpNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateInfiltrationSysOpShard( Rand );

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

                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.SecurityCamerasOffline ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpSecurityCameras" ), false, 3, 2, 3, Rand, 0 );
                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.VentilationFansOffline ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpVentilationFans" ), false, 3, 2, 3, Rand, 0 );
                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.SecurityDoorsOpen ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpSecurityDoors" ), false, 3, 2, 3, Rand, 0 );

                                    HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpHalogenFireSuppression" ), false, 3, 2, 3, Rand, 0 );
                                    if ( scene.TargetUnit.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) > 100 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpFalseAlarm" ), false, 3, 2, 3, Rand, 0 );
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                #endregion
                case "HospitalInfiltrationSysOp":
                    #region HospitalInfiltrationSysOp
                    {
                        switch ( Logic )
                        {
                            case HackingScenarioLogic.DoInitialPopulation:
                                {
                                    CityStatisticTable.AlterScore( "InfiltrationSysOpAttempts", 1 );

                                    scene.AddToHackingHistory( Lang.Get( "InfiltrationSysOpLog_Online" ) );

                                    HackingHelper.PopulateInfiltrationSysOpNumberSubstrate( Rand );
                                    HackingHelper.PopulateStandardBlockages( Rand );
                                    HackingHelper.PopulateInfiltrationSysOpShard( Rand );

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

                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.SecurityCamerasOffline ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpSecurityCameras" ), false, 3, 2, 3, Rand, 0 );
                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.VentilationFansOffline ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpVentilationFans" ), false, 3, 2, 3, Rand, 0 );
                                    if ( scene.TargetUnit.GetStackCountOfStatus( StatusRefs.SecurityDoorsOpen ) == 0 )
                                        HackingHelper.TryPlaceADaemon( HackingDaemonTypeTable.Instance.GetRowByID( "InfiltrationSysOpSecurityDoors" ), false, 3, 2, 3, Rand, 0 );
                                }
                                break;
                            case HackingScenarioLogic.DoPerFrame:
                                break;
                        }
                    }
                    break;
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "InfiltrationSysOpScenariosBasic: Called TryHandleHackingScenarioLogic for '" + Scenario.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return; //was error
            }
        }
    }
}

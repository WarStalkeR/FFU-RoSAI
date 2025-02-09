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

    public class DaemonsBasic : IHackingDaemonImplementation
    {
        private static readonly ArcenDoubleCharacterBuffer perFrameBuffer = new ArcenDoubleCharacterBuffer( "DaemonsBasic-perFrameBuffer" );

        public HackingDaemonResult TryHandleDaemonLogic( HackingDaemonType DaemonType, object DaemonObject, object OtherObject, HackingDaemonLogic Logic )
        {
            if ( DaemonType == null )
                return HackingDaemonResult.Indeterminate; //was error
            Daemon daemon = DaemonObject as Daemon;
            if ( daemon == null || daemon.DaemonType == null || daemon.CurrentCell == null )
                return HackingDaemonResult.Indeterminate; //not a daemon, or is a dead one

            Daemon otherDaemon = OtherObject as Daemon;
            PlayerShard shard = OtherObject as PlayerShard;

            if ( Logic == HackingDaemonLogic.HitByCorruptionFire && DaemonType.CostTypeToCorrupt != null && DaemonType.CostAmountToCorrupt > 0 )
            {
                if ( DaemonType.CostTypeToCorrupt.Current < DaemonType.CostAmountToCorrupt )
                {
                    //cannot afford!
                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                    buffer.AddFormat1( "HackingLog_DaemonFailedToCorruptP1", daemon.DaemonType.GetDisplayName() );
                    scene.AddToHackingHistory( buffer );

                    buffer.Clear();
                    buffer.AddFormat2( "HackingLog_DaemonFailedToCorruptP2", DaemonType.CostAmountToCorrupt, DaemonType.CostTypeToCorrupt.GetDisplayName() );
                    scene.AddToHackingHistory( buffer );

                    daemon.DestroyEntity();

                    //any special logic on failing to corrupt
                    switch ( DaemonType.ID )
                    {
                        case "TitanHumanContact":
                        case "TitanSystemContact":
                            #region CommsProbeFailure
                            {
                                scene.HasLoggedFailure = true;
                                buffer.Clear();
                                buffer.AddLang( "HackingLog_SessionTerminated", ColorTheme.RedOrange2 );
                                scene.AddToHackingHistory( buffer );
                                ParticleSoundRefs.Hacking_Fail.DuringGame_PlaySoundOnlyAtCamera();

                                CityStatisticTable.AlterScore( "CommsProbeFailures", 1 );
                            }
                            #endregion
                            break;
                    }

                    return HackingDaemonResult.KilledTarget;
                }
            }

            if ( Logic == HackingDaemonLogic.HitByOtherDaemon )
            {
                //if a daemon hunter hit a daemon that can be hunted, destroy the hunted daemon
                if ( (otherDaemon?.DaemonType?.HuntsHostileDaemons??false ) && DaemonType.CanDaemonBeHuntedByFriendlyDaemons )
                {
                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                    buffer.AddFormat2( "HackingLog_DaemonDestroyedByOtherDaemon", DaemonType.GetDisplayName(), otherDaemon.DaemonType.GetDisplayName() );
                    scene.AddToHackingHistory( buffer );
                    daemon.DestroyEntity();
                    return HackingDaemonResult.KilledTarget;
                }

                //if a hunted daemon lands on the daemon hunter, then destroy the hunter
                if ( (otherDaemon?.DaemonType?.CanDaemonBeHuntedByFriendlyDaemons ?? false) && DaemonType.HuntsHostileDaemons )
                {
                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                    buffer.AddFormat2( "HackingLog_DaemonDestroyedByOtherDaemon", DaemonType.GetDisplayName(), otherDaemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                    scene.AddToHackingHistory( buffer );
                    daemon.DestroyEntity();
                    return HackingDaemonResult.KilledTarget;
                }
            }

            switch ( DaemonType.ID )
            {
                case "Gnath":
                case "TriswarmFragment":
                    #region Gnath / TriswarmFragment
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoBasicDaemonChaseLogic( daemon );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                HackingHelper.DoBasicDaemonThreatenLogic( daemon );                                
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.DaemonDestroyPlayerShard( shard, daemon );
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( DaemonType.TryPayCorruptionCosts() )
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );
                                        daemon.DestroyEntity();

                                        return HackingDaemonResult.KilledTarget;
                                    }
                                    break;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "Triswarm":
                    #region Triswarm
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoBasicDaemonChaseLogic( daemon );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                HackingHelper.DoBasicDaemonThreatenLogic( daemon );
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.DaemonDestroyPlayerShard( shard, daemon );
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    DaemonType.TryPayCorruptionCosts();
                                    daemon.DestroyEntity();

                                    HackingDaemonType daemonType = HackingDaemonTypeTable.Instance.GetRowByID( "TriswarmFragment" );
                                    if ( daemonType != null )
                                    {
                                        int splitCount = 0;
                                        for ( int i = 0; i < 3; i++ )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom );
                                            if ( newDaemon != null )
                                                splitCount++;
                                        }

                                        if ( splitCount > 0 )
                                        {
                                            buffer.AddFormat2( "HackingLog_DaemonSplitSuccess", daemon.DaemonType.GetDisplayName(), splitCount, ColorTheme.RedOrange2 );
                                            scene.AddToHackingHistory( buffer );

                                            buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                            scene.AddToHackingHistory( buffer );
                                        }
                                        else
                                        {
                                            buffer.AddFormat1( "HackingLog_DaemonSplitFailed", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                            scene.AddToHackingHistory( buffer );

                                            buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                            scene.AddToHackingHistory( buffer );
                                        }
                                    }
                                    else
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );
                                    }

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "SecurityGuard":
                    #region SecurityGuard
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoDaemonWanderOrLeapLogic( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                HackingHelper.DoBasicDaemonThreatenLogic( daemon );
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.DaemonDestroyPlayerShard( shard, daemon );
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( DaemonType.TryPayCorruptionCosts() )
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonDistracted", daemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );
                                        daemon.DestroyEntity();

                                        return HackingDaemonResult.KilledTarget;
                                    }
                                    break;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "CodePriest":
                    #region CodePriest
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoBasicDaemonChaseLogic( daemon );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                HackingHelper.DoBasicDaemonThreatenLogic( daemon );
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.DaemonDestroyPlayerShard( shard, daemon );
                                HackingHelper.FillDaemonTypeBagFromTagUse( "CodePriestSpawns" );
                                HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                if ( daemonType != null )
                                {
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom );
                                    if ( newDaemon == null )
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonConversionFailed", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat2( "HackingLog_DaemonConversionSuccess", daemon.DaemonType.GetDisplayName(), newDaemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                }
                                else
                                {
                                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    buffer.AddFormat1( "HackingLog_DaemonConversionFailed", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                    scene.AddToHackingHistory( buffer );
                                }
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    DaemonType.TryPayCorruptionCosts();
                                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                    scene.AddToHackingHistory( buffer );
                                    daemon.DestroyEntity();
                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "LAKE":
                    #region LAKE
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoDaemonChaseDaemonLogic( daemon );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                //HackingHelper.DoBasicDaemonThreatenLogic( daemon );
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                //this happens when it hits the kind it can't kill
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    DaemonType.TryPayCorruptionCosts();
                                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                    scene.AddToHackingHistory( buffer );
                                    daemon.DestroyEntity();
                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "RecursiveLoop":
                    #region RecursiveLoop
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.Move:
                                HackingHelper.DoBasicDaemonChaseLogic( daemon );
                                break;
                            case HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives:
                                HackingHelper.DoBasicDaemonThreatenLogic( daemon );
                                break;
                            case HackingDaemonLogic.HitPlayerShard:
                                HackingHelper.DaemonDestroyPlayerShard( shard, daemon );
                                HackingDaemonType daemonType = daemon.DaemonType;
                                if ( daemonType != null )
                                {
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom );
                                    if ( newDaemon == null )
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonDuplicationFailed", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonDuplicationSuccess", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                }
                                break;
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToNearestFreeSpaceOrKillIt( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    DaemonType.TryPayCorruptionCosts();
                                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                    scene.AddToHackingHistory( buffer );
                                    daemon.DestroyEntity();
                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "LeafNode":
                case "RingNode3":
                    #region LeafNode / RingNode3
                    {
                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.OnAllHostileDaemonsGone:
                                HackingHelper.FillDaemonTypeBagFromTagUse( "LeafNodeContents" );
                                HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                if ( daemonType != null )
                                {
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom );

                                    if ( newDaemon == null )
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonGotAway", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        buffer.AddFormat2( "HackingLog_DaemonRevealsOtherDaemon", daemon.DaemonType.GetDisplayName(), newDaemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );

                                        if ( FlagRefs.HackTut_FinishTheJob.DuringGameplay_CompleteIfActive() )
                                            FlagRefs.HackTut_CorruptThePilotCradle.DuringGameplay_StartIfNeeded();
                                    }
                                }

                                {
                                    daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                                    daemon.DestroyEntity();
                                }
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                buffer.AddFormat2( "HackingLog_DaemonImmuneToDirectCorruption", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                scene.AddToHackingHistory( buffer );
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                return HackingDaemonResult.Indeterminate;
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "RingNode":
                case "RingNode1":
                case "RingNode2":
                    #region Lower RingNodes
                    {
                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                {
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( DaemonType.InsteadOfDeathBecomes, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom );
                                    if ( newDaemon == null )
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonGotAway", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonHealthLossAndMoved", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                                    daemon.DestroyEntity();
                                    break;
                                }
                            case HackingDaemonLogic.OnAllHostileDaemonsGone:
                                HackingHelper.FillDaemonTypeBagFromTagUse( "LeafNodeContents" );
                                HackingDaemonType daemonType = HackingHelper.GetValidDaemonTypeFromBag( Engine_Universal.PermanentQualityRandom );
                                if ( daemonType != null )
                                {
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom );

                                    if ( newDaemon == null )
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonGotAway", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        buffer.AddFormat2( "HackingLog_DaemonRevealsOtherDaemon", daemon.DaemonType.GetDisplayName(), newDaemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                }

                                {
                                    daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                                    daemon.DestroyEntity();
                                }
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                buffer.AddFormat2( "HackingLog_DaemonImmuneToDirectCorruption", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                scene.AddToHackingHistory( buffer );
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                return HackingDaemonResult.Indeterminate;
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "ArmorPlatingServicePanels":
                case "HydraulicActuators":
                case "WeaponSystems":
                case "AugmentedOrgans":
                case "AdrenalineRegulator":
                case "AugmentedVision":
                case "BionicUplink":
                    #region ArmorPlatingServicePanels / HydraulicActuators / WeaponSystems
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( DaemonType.InsteadOfDeathBecomes != null )
                                    {
                                        Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( DaemonType.InsteadOfDeathBecomes, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom );
                                        if ( newDaemon == null )
                                        {
                                            ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                            buffer.AddFormat1( "HackingLog_DaemonGotAway", daemon.DaemonType.GetDisplayName(), ColorTheme.Gray );
                                            scene.AddToHackingHistory( buffer );
                                        }
                                        else
                                        {
                                            ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                            buffer.AddFormat1( "HackingLog_DaemonHealthLossAndMoved", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                            scene.AddToHackingHistory( buffer );
                                        }
                                        DaemonType.TryPayCorruptionCosts();
                                        daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                                        daemon.DestroyEntity();
                                        return HackingDaemonResult.Indeterminate;
                                    }
                                    else
                                    {
                                        if ( scene.TargetUnit != null )
                                        {
                                            DaemonType.TryPayCorruptionCosts();

                                            DoFinalDaemonLogicForDeathOrQuickHack( DaemonType, scene.HackerUnit, scene.TargetUnit );

                                            //if ( Engine_Universal.PermanentQualityRandom.Next( 0, 100 ) < 20 )
                                            {
                                                int rand = Engine_Universal.PermanentQualityRandom.Next( 0, 100 );
                                                if ( rand < 15 )
                                                    ResearchDomainTable.Instance.GetRowByID( "SmallArmsResearch" )?.AddMoreInspiration( 1 );
                                                else if ( rand  < 30 )
                                                    ResearchDomainTable.Instance.GetRowByID( "StructuralImprovement" )?.AddMoreInspiration( 1 );
                                                else if ( rand < 45 )
                                                    ResearchDomainTable.Instance.GetRowByID( "ItemDevelopment" )?.AddMoreInspiration( 1 );
                                                else if ( rand < 60 )
                                                    ResearchDomainTable.Instance.GetRowByID( "VehicularDevelopment" )?.AddMoreInspiration( 1 );
                                                else if ( rand < 75 )
                                                    ResearchDomainTable.Instance.GetRowByID( "MartialExpansion" )?.AddMoreInspiration( 1 );
                                                else if ( rand < 90 )
                                                    ResearchDomainTable.Instance.GetRowByID( "ProcurementEfficacy" )?.AddMoreInspiration( 1 );
                                                else
                                                    ResearchDomainTable.Instance.GetRowByID( "AndroidOptimization" )?.AddMoreInspiration( 1 );
                                            }

                                            //these are sneaky
                                            //(scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );

                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "HackingSuccesses", 1 );
                                        }
                                        daemon.DestroyEntity();
                                        return HackingDaemonResult.KilledTarget;
                                    }
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "MechPilotCradle":
                    #region MechPilotCradle
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    //ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                    //buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                    //scene.AddToHackingHistory( buffer );
                                    //daemon.DestroyEntity();

                                    if ( scene.TargetUnit != null && ((scene.TargetUnit as ISimNPCUnit)?.UnitType.IsMechStyleMovement??false) )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            DoFinalDaemonLogicForDeathOrQuickHack( DaemonType, scene.HackerUnit, scene.TargetUnit );

                                            ResearchDomainTable.Instance.GetRowByID( "AndroidOptimization" )?.AddMoreInspiration( 1 );
                                            ResearchDomainTable.Instance.GetRowByID( "ItemDevelopment" )?.AddMoreInspiration( 1 );

                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            FlagRefs.HasDoneFirstSimpleMechHack.TripIfNeeded();

                                            CityStatisticTable.AlterScore( "HackingSuccesses", 1 );
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "TitanCommsLeak":
                    #region TitanCommsLeak
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            (scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );
                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );
                                            DaemonType.TryHaveUnitGetTheseGains_MainThreadOnly( scene.HackerUnit );
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "TitanCommsGap":
                    #region TitanCommsGap
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            (scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );
                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );
                                            DaemonType.TryHaveUnitGetTheseGains_MainThreadOnly( scene.HackerUnit );
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "TitanHumanContact":
                    #region TitanHumanContact
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            (scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );
                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );
                                            DaemonType.TryHaveUnitGetTheseGains_MainThreadOnly( scene.HackerUnit );

                                            if ( scene.TargetUnit is ISimNPCUnit npc )
                                                npc.Stance = NPCUnitStanceTable.Instance.GetRowByID( "SpaceNationTitanDeactivated" );
                                            CityFlagTable.Instance.GetRowByID( "ImmobilizedTheTiitan" ).TripIfNeeded();
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "TitanSystemContact":
                    #region TitanSystemContact
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            (scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );
                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );
                                            DaemonType.TryHaveUnitGetTheseGains_MainThreadOnly( scene.HackerUnit );
                                            if ( scene.TargetUnit is ISimNPCUnit npc )
                                                npc.Stance = NPCUnitStanceTable.Instance.GetRowByID( "SpaceNationTitanDeactivated" );
                                            CityFlagTable.Instance.GetRowByID( "ImmobilizedTheTiitan" ).TripIfNeeded();
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "TitanFreedSystemContact":
                    #region TitanFreedSystemContact
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        if ( DaemonType.TryPayCorruptionCosts() )
                                        {
                                            (scene.TargetUnit as ISimNPCUnit)?.DoOnPostHitWithHostileAction( scene.HackerUnit, 100, Engine_Universal.PermanentQualityRandom, false );
                                            h.Instance.Close( WindowCloseReason.UserDirectRequest );

                                            CityStatisticTable.AlterScore( "CommsProbeSuccesses", 1 );
                                            DaemonType.TryHaveUnitGetTheseGains_MainThreadOnly( scene.HackerUnit );

                                            NPCEvent minorEvent;
                                            if ( CityFlagTable.Instance.GetRowByID( "LAKEDebriefDone" ).DuringGameplay_IsTripped )
                                                minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact6_PostEndOfTimeDebrief" );
                                            else if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_HackTheTitan" ).DuringGame_ActualOutcome != null )
                                            {
                                                if ( SimMetagame.CurrentChapterNumber < 3 )
                                                    minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact4_PreEndOfTime" );
                                                else
                                                    minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact5_PostEndOfTime" );
                                            }
                                            else if ( CityFlagTable.Instance.GetRowByID( "OfferedToHelpTheTitan" ).DuringGameplay_IsTripped )
                                            {
                                                if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_SpaceNationFugitives" ).DuringGame_ActualOutcome == null )
                                                    minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact1_HelpInProgress" );
                                                else if ( MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_NegotiateWithTheTitan" ).DuringGame_ActualOutcome == null )
                                                    minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact2_Negotiation" );
                                                else
                                                    minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact3_FurtherDiscussion" );
                                            }
                                            else if ( CityFlagTable.Instance.GetRowByID( "TurnedDownHelpingTheTitan" ).DuringGameplay_IsTripped )
                                                minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact1_HelpRequestReturn" );
                                            else if ( CityFlagTable.Instance.GetRowByID( "SkippedHearingAboutHelpFromTheTitan" ).DuringGameplay_IsTripped )
                                                minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact1_HelpRequest" );
                                            else
                                                minorEvent = NPCEventTable.Instance.GetRowByID( "Titan_Contact1" );

                                            //START_MINOR_EVENT
                                            minorEvent.ClearForMinorEvent();
                                            MinorEventHandler.TryStart( scene.HackerUnit, minorEvent, null, minorEvent.MinorData.SpecificCohort, true );
                                        }
                                    }
                                    else
                                        ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((scene.TargetUnit as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "InfiltrationSysOpHalogenFireSuppression":
                case "InfiltrationSysOpSecurityCameras":
                case "InfiltrationSysOpVentilationFans":
                case "InfiltrationSysOpSecurityDoors":
                case "InfiltrationSysOpFalseAlarm":
                    #region InfiltrationSysOp Stuff
                    {
                        switch ( Logic )
                        {
                            case HackingDaemonLogic.HitByOtherDaemon:
                                HackingHelper.JumpDaemonToRandomNearSpaceOrFarSpace( daemon, Engine_Universal.PermanentQualityRandom );
                                break;
                            case HackingDaemonLogic.HitByCorruptionFire:
                                {
                                    if ( scene.TargetUnit != null )
                                    {
                                        DaemonType.TryPayCorruptionCosts();
                                        if ( daemon.DaemonType.BadgeToApplyToTargetOnDeath != null )
                                        {
                                            scene.TargetUnit.AddOrRemoveBadge( daemon.DaemonType.BadgeToApplyToTargetOnDeath, true );

                                            Vector3 startLocation = scene.TargetUnit.GetPositionForCollisions();
                                            Vector3 endLocation = startLocation.PlusY( scene.TargetUnit.GetHalfHeightForCollisions() + 0.2f );
                                            ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                            if ( buffer != null )
                                            {
                                                buffer.AddRaw( daemon.DaemonType.BadgeToApplyToTargetOnDeath.GetDisplayName(), IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                                newDamageText.PhysicalDamageIncluded = 0;
                                                newDamageText.MoraleDamageIncluded = 0;
                                                newDamageText.SquadDeathsIncluded = 0;
                                            }
                                        }

                                        if ( daemon.DaemonType.StatusToApplyToTargetOnDeath != null )
                                        {
                                            int statusAmount = 0;

                                            switch (DaemonType.ID)
                                            {
                                                case "InfiltrationSysOpSecurityCameras":
                                                    statusAmount = 100;
                                                    break;
                                                case "InfiltrationSysOpVentilationFans":
                                                case "InfiltrationSysOpSecurityDoors":
                                                    statusAmount = 50;
                                                    break;
                                            }

                                            int turnsOfStatus = Engine_Universal.PermanentQualityRandom.NextInclus( 2, 3 );
                                            scene.TargetUnit.AddStatus( daemon.DaemonType.StatusToApplyToTargetOnDeath, statusAmount, turnsOfStatus );

                                            Vector3 startLocation = scene.TargetUnit.GetPositionForCollisions();
                                            Vector3 endLocation = startLocation.PlusY( scene.TargetUnit.GetHalfHeightForCollisions() + 0.2f );
                                            ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                            if ( buffer != null )
                                            {
                                                buffer.AddRaw( daemon.DaemonType.StatusToApplyToTargetOnDeath.GetDisplayName(), IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                                newDamageText.PhysicalDamageIncluded = 0;
                                                newDamageText.MoraleDamageIncluded = 0;
                                                newDamageText.SquadDeathsIncluded = 0;
                                            }
                                        }

                                        switch ( DaemonType.ID )
                                        {
                                            case "InfiltrationSysOpHalogenFireSuppression":
                                                {
                                                    for ( int i = scene.Daemons.Count - 1; i >= 0; i--)
                                                    {
                                                        Daemon other = scene.Daemons[i];
                                                        if ( (other?.DaemonType?.ID??string.Empty) == "SecurityGuard" )
                                                            other.DestroyEntity();
                                                    }
                                                }
                                                break;
                                            case "InfiltrationSysOpFalseAlarm":
                                                {
                                                    scene.TargetUnit.AlterActorDataCurrent( ActorRefs.ActorHP, 200, true );

                                                    Vector3 startLocation = scene.TargetUnit.GetPositionForCollisions();
                                                    Vector3 endLocation = startLocation.PlusY( scene.TargetUnit.GetHalfHeightForCollisions() + 0.2f );
                                                    ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                                    if ( buffer != null )
                                                    {
                                                        buffer.AddRaw( daemon.DaemonType.GetDisplayName(), IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                                                        DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                                            startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                                        newDamageText.PhysicalDamageIncluded = 0;
                                                        newDamageText.MoraleDamageIncluded = 0;
                                                        newDamageText.SquadDeathsIncluded = 0;
                                                    }
                                                }
                                                break;
                                        }

                                        switch ( DaemonType.ID )
                                        {
                                            case "InfiltrationSysOpHalogenFireSuppression":
                                                break;
                                            default:
                                                h.Instance.Close( WindowCloseReason.UserDirectRequest );
                                                CityStatisticTable.AlterScore( "HackingSuccesses", 1 );
                                                break;
                                        }
                                    }
                                    daemon.DestroyEntity();
                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "DaemonsBasic: Called TryHandleCommandDaemon for '" + DaemonType.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return HackingDaemonResult.Indeterminate; //was error
            }
        }

        public void DoFinalDaemonLogicForDeathOrQuickHack( HackingDaemonType DaemonType, ISimMachineActor Hacker, ISimMapMobileActor Target )
        {
            switch ( DaemonType.ID )
            {
                case "ArmorPlatingServicePanels":
                case "HydraulicActuators":
                case "WeaponSystems":
                case "AugmentedOrgans":
                case "AdrenalineRegulator":
                case "AugmentedVision":
                case "BionicUplink":
                    #region ArmorPlatingServicePanels / HydraulicActuators / WeaponSystems
                    {
                        if ( DaemonType.BadgeToApplyToTargetOnDeath != null )
                        {
                            Target.AddOrRemoveBadge( DaemonType.BadgeToApplyToTargetOnDeath, true );

                            Vector3 startLocation = Target.GetPositionForCollisions();
                            Vector3 endLocation = startLocation.PlusY( Target.GetHalfHeightForCollisions() + 0.2f );
                            ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                            if ( buffer != null )
                            {
                                buffer.AddRaw( DaemonType.BadgeToApplyToTargetOnDeath.GetDisplayName(), IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                newDamageText.PhysicalDamageIncluded = 0;
                                newDamageText.MoraleDamageIncluded = 0;
                                newDamageText.SquadDeathsIncluded = 0;
                            }
                        }

                        if ( DaemonType.StatusToApplyToTargetOnDeath != null )
                        {
                            int hackerSkill = Hacker.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                            if ( hackerSkill < 150 )
                                hackerSkill = 150;

                            int turnsOfStatus = 2 + Mathf.RoundToInt( hackerSkill / 150f );
                            Target.AddStatus( DaemonType.StatusToApplyToTargetOnDeath, hackerSkill, turnsOfStatus );

                            Vector3 startLocation = Target.GetPositionForCollisions();
                            Vector3 endLocation = startLocation.PlusY( Target.GetHalfHeightForCollisions() + 0.2f );
                            ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                            if ( buffer != null )
                            {
                                buffer.AddRaw( DaemonType.StatusToApplyToTargetOnDeath.GetDisplayName(), IconRefs.RobotConverted.DefaultColorHexWithHDRHex );
                                DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                    startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                newDamageText.PhysicalDamageIncluded = 0;
                                newDamageText.MoraleDamageIncluded = 0;
                                newDamageText.SquadDeathsIncluded = 0;
                            }
                        }

                        switch ( DaemonType.ID )
                        {
                            case "AugmentedOrgans":
                                {
                                    if ( !AchievementRefs.HateYouInParticular.OneTimeline_HasBeenTripped )
                                    {
                                        if ( Target is ISimNPCUnit npcUnit )
                                        {
                                            if ( npcUnit.GetStatusIntensity( StatusRefs.EmotionalOverload ) > 0 &&
                                                npcUnit.GetStatusIntensity( StatusRefs.Blindness ) > 0 )
                                                AchievementRefs.HateYouInParticular.TripIfNeeded();
                                        }
                                    }

                                    AttackHelper.InstantlyKill( Hacker, Target, Engine_Universal.PermanentQualityRandom );
                                }
                                break;
                        }
                    }
                    #endregion
                    break;
                case "MechPilotCradle":
                    #region MechPilotCradle
                    {
                        if ( Target != null && ((Target as ISimNPCUnit)?.UnitType.IsMechStyleMovement ?? false) )
                        {
                            string npcTypeID = (Target as ISimNPCUnit)?.UnitType?.ID ?? string.Empty;
                            (Target as ISimNPCUnit)?.DoOnPostHitWithHostileAction( Hacker, 100, Engine_Universal.PermanentQualityRandom, false );
                            (Target as ISimNPCUnit)?.ConvertEnemyRobotToPlayerForces();

                            HackingHelper.MarkHackerDefective();

                            FlagRefs.HackTut_CorruptThePilotCradle.DuringGameplay_CompleteIfActive();

                            if ( !AchievementRefs.Wololo.OneTimeline_HasBeenTripped )
                                AchievementRefs.Wololo.TripIfNeeded();

                            FlagRefs.HasHackedFirstMechInAnyWay.TripIfNeeded();

                            switch ( npcTypeID )
                            {
                                case "MechConvicter":
                                case "MechConvicterV2":
                                case "MechConvicterV3":
                                case "MechConvicterV4":
                                case "MechConvicterV5":
                                    if ( !AchievementRefs.WhatsYoursIsMine.OneTimeline_HasBeenTripped )
                                        AchievementRefs.WhatsYoursIsMine.TripIfNeeded();
                                    break;
                            }
                        }
                        else
                            ArcenDebugging.LogSingleLine( "Error, wrong kind of target unit '" + ((Target as ISimNPCUnit)?.UnitType?.ID ?? "[null]") + "'!", Verbosity.ShowAsError );
                    }
                    break;
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "DaemonsBasic: Called DoFinalDaemonLogicForDeathOrQuickHack for '" + DaemonType.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

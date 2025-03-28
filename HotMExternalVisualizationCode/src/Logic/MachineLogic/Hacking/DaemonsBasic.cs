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

            if ( daemon.HasBeenDestroyed && Logic == HackingDaemonLogic.HitByCorruptionFire )
                return HackingDaemonResult.Indeterminate; //not a daemon, or is a dead one

            if ( Logic == HackingDaemonLogic.HitByCorruptionFire && DaemonType.CostTypeToCorrupt != null && DaemonType.CostAmountToCorruptDiscounted > 0 )
            {
                if ( DaemonType.CostTypeToCorrupt.Current < DaemonType.CostAmountToCorruptDiscounted )
                {
                    //cannot afford!
                    ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                    buffer.AddFormat1( "HackingLog_DaemonFailedToCorruptP1", daemon.DaemonType.GetDisplayName() );
                    scene.AddToHackingHistory( buffer );

                    buffer.Clear();
                    buffer.AddFormat2( "HackingLog_DaemonFailedToCorruptP2", DaemonType.CostAmountToCorruptDiscounted, DaemonType.CostTypeToCorrupt.GetDisplayName() );
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
                                    if ( DaemonType.TryPayCorruptionCostsDiscounted() )
                                    {
                                        ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                        buffer.AddFormat1( "HackingLog_DaemonDestroyedByCorruption", daemon.DaemonType.GetDisplayName() );
                                        scene.AddToHackingHistory( buffer );
                                        daemon.DestroyEntity();

                                        return HackingDaemonResult.KilledTarget;
                                    }
                                    break;
                                }
                            case HackingDaemonLogic.ReachedMovementDestination:
                                {
                                    switch ( DaemonType.ID )
                                    {
                                        case "TriswarmFragment":
                                            if ( daemon.MovesHasMade > 0 )
                                            {
                                                ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
                                                buffer.AddFormat1( "HackingLog_DaemonDestroyedByExpiration", DaemonType.GetDisplayName() );
                                                scene.AddToHackingHistory( buffer );
                                                daemon.DestroyEntity();
                                            }
                                            break;
                                    }
                                }
                                break;
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
                                    DaemonType.TryPayCorruptionCostsDiscounted();
                                    daemon.DestroyEntity();

                                    HackingDaemonType daemonType = HackingDaemonTypeTable.Instance.GetRowByID( "TriswarmFragment" );
                                    if ( daemonType != null )
                                    {
                                        int splitCount = 0;
                                        for ( int i = 0; i < 3; i++ )
                                        {
                                            Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom, 2 );
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
                case "Bomb":
                    #region Bomb
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
                                    h.hCell cell = daemon.CurrentCell;

                                    DaemonType.TryPayCorruptionCostsDiscounted();
                                    daemon.DestroyEntity();

                                    if ( cell != null )
                                    {
                                        cell.IsBlocked = false;
                                        cell.SetCurrentNumber( 0 );
                                        foreach ( h.hCell cell2 in cell.AdjacentCellsAndDiagonal )
                                        {
                                            cell2.IsBlocked = false;
                                            cell2.SetCurrentNumber( 0 );

                                            if ( cell2.CurrentEntity != null )
                                            {
                                                if ( cell2.CurrentEntity is Daemon daemon2 && !daemon.DaemonType.WillEndHackingSessionIfCorrupted )
                                                {
                                                    HackingDaemonResult result = daemon2.DaemonType.Implementation.TryHandleDaemonLogic( daemon2.DaemonType, daemon2, null, HackingDaemonLogic.HitByCorruptionFire );
                                                    if ( result == HackingDaemonResult.KilledTarget )
                                                    {
                                                        daemon2.DaemonType.OnDeath.DuringGame_PlayAtUILocation( cell2.CalculateScreenPos(), true );
                                                        CityStatisticTable.AlterScore( "HackingDaemonsCorrupted", 1 );
                                                    }
                                                }
                                                else if ( cell2.CurrentEntity is PlayerShard shard2 )
                                                {
                                                    HackingHelper.DaemonDestroyPlayerShard( shard2, daemon );
                                                }
                                                else
                                                {
                                                    DaemonType.OnDeath.DuringGame_PlayAtUILocation( cell2.CalculateScreenPos(), true );
                                                    cell.CurrentEntity.DestroyEntity();
                                                }
                                            }
                                            else
                                                DaemonType.OnDeath.DuringGame_PlayAtUILocation( cell2.CalculateScreenPos(), true );
                                        }
                                    }

                                    return HackingDaemonResult.KilledTarget;
                                }
                        }
                    }
                    return HackingDaemonResult.Indeterminate;
                #endregion
                case "LesserWanderer":
                case "GreaterWanderer":
                    #region LesserWanderer / GreaterWanderer
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
                                    if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                    if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom, 1 );
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
                                    DaemonType.TryPayCorruptionCostsDiscounted();
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
                                    DaemonType.TryPayCorruptionCostsDiscounted();
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
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonAdjacent( daemonType, daemon.CurrentCell, false, Engine_Universal.PermanentQualityRandom, 1 );
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
                                    DaemonType.TryPayCorruptionCostsDiscounted();
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
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 0 );

                                    if ( newDaemon == null )
                                    {
                                        buffer.AddFormat1( "HackingLog_DaemonGotAway", daemon.DaemonType.GetDisplayName(), ColorTheme.RedOrange2 );
                                        scene.AddToHackingHistory( buffer );
                                    }
                                    else
                                    {
                                        h.hCell daemonCell = newDaemon.CurrentCell;
                                        if ( daemonCell != null )
                                        {
                                            foreach ( h.hCell adjacent in daemonCell.AdjacentCellsAndDiagonal2X )
                                            {
                                                if ( adjacent.IsBlocked )
                                                    adjacent.IsBlocked = false;
                                                if ( adjacent.CurrentNumber == 0 )
                                                    adjacent.SetCurrentNumber( Engine_Universal.PermanentQualityRandom.Next( 11, 19 ) );
                                            }
                                        }

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
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( DaemonType.InsteadOfDeathBecomes, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 1 );
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
                                    Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( daemonType, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 1 );

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
                                        Daemon newDaemon = HackingHelper.TryPlaceADaemonSoftening( DaemonType.InsteadOfDeathBecomes, false, 6, 4, 2, Engine_Universal.PermanentQualityRandom, 1 );
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
                                        DaemonType.TryPayCorruptionCostsDiscounted();
                                        daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                                        daemon.DestroyEntity();
                                        return HackingDaemonResult.Indeterminate;
                                    }
                                    else
                                    {
                                        if ( scene.TargetUnit != null )
                                        {
                                            DaemonType.TryPayCorruptionCostsDiscounted();

                                            DoFinalDaemonLogicForDeathOrQuickHack( DaemonType, scene.HackerUnit, scene.TargetUnit );
                                            
                                            switch ( DaemonType.ID )
                                            {
                                                case "ArmorPlatingServicePanels":
                                                case "HydraulicActuators":
                                                case "WeaponSystems":
                                                    if ( !ResourceRefs.TungstenScraps.DuringGame_IsUnlocked() )
                                                        ResourceRefs.TungstenScraps.ResourceMustBeUnlockedBy.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( 
                                                            CommonRefs.WorldExperienceInspiration, true, true, true, false );
                                                    switch ( DaemonType.ID )
                                                    {
                                                        case "ArmorPlatingServicePanels":
                                                            ResourceRefs.TungstenScraps.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 200, 300 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                        case "HydraulicActuators":
                                                            ResourceRefs.TungstenScraps.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 20, 45 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                        case "WeaponSystems":
                                                            ResourceRefs.TungstenScraps.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 50, 75 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                    }
                                                    break;
                                                case "AugmentedOrgans":
                                                case "AdrenalineRegulator":
                                                case "AugmentedVision":
                                                case "BionicUplink":
                                                    if ( !ResourceRefs.HumanBiodata.DuringGame_IsUnlocked() )
                                                        ResourceRefs.HumanBiodata.ResourceMustBeUnlockedBy.DuringGameplay_ImmediatelyInventIfNotAlreadyDone(
                                                            CommonRefs.WorldExperienceInspiration, true, true, true, false );
                                                    switch ( DaemonType.ID )
                                                    {
                                                        case "AugmentedOrgans":
                                                            ResourceRefs.HumanBiodata.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 100, 200 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                        case "AdrenalineRegulator":
                                                            ResourceRefs.HumanBiodata.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 200, 300 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                        case "AugmentedVision":
                                                            ResourceRefs.HumanBiodata.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 400, 500 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                        case "BionicUplink":
                                                            ResourceRefs.HumanBiodata.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 600, 700 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                                            break;
                                                    }
                                                    break;
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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
                                        {
                                            DoFinalDaemonLogicForDeathOrQuickHack( DaemonType, scene.HackerUnit, scene.TargetUnit );

                                            if ( !ResourceRefs.TungstenScraps.DuringGame_IsUnlocked() )
                                                ResourceRefs.TungstenScraps.ResourceMustBeUnlockedBy.DuringGameplay_ImmediatelyInventIfNotAlreadyDone(
                                                    CommonRefs.WorldExperienceInspiration, true, true, true, false );
                                            if ( !ResourceRefs.HumanBiodata.DuringGame_IsUnlocked() )
                                                ResourceRefs.HumanBiodata.ResourceMustBeUnlockedBy.DuringGameplay_ImmediatelyInventIfNotAlreadyDone(
                                                    CommonRefs.WorldExperienceInspiration, true, true, true, false );

                                            ResourceRefs.TungstenScraps.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 800, 900 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );
                                            ResourceRefs.HumanBiodata.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 600, 700 ), "Increase_FullHackOfAnEnemy", ResourceAddRule.IgnoreUntilTurnChange );

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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                        if ( DaemonType.TryPayCorruptionCostsDiscounted() )
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
                                        DaemonType.TryPayCorruptionCostsDiscounted();
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

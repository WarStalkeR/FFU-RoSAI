using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.ExternalVis.Hacking
{
    using h = Window_Hacking;
    using scene = HackingScene;

    public static class HackingHelper
    {
        #region GetBestShard_RunRules
        public static PlayerShard GetBestShard_RunRules( out h.hCell blockedBy )
        {
            blockedBy = null;
            if ( scene.AnimatingItems.Count > 0 )
            {
                //debugText = "animating";
                return null; //no targeting while animating
            }
            h.hCell target = scene.HoveredCell;
            if ( target == null || target.CurrentEntity is PlayerShard )
            {
                //debugText = "target null or blocked or something";
                return null;
            }

            h.hCell bestBlocked = null;
            int bestBlockedDistance = 99;

            //debugText = "testing shards: " + PlayerShards.Count + "\n";
            PlayerShard bestShard = null;
            int bestShardDistance = 99;
            foreach ( PlayerShard shard in scene.PlayerShards )
            {
                h.hCell current = shard.CurrentCell;
                if ( current == null )
                {
                    //debugText = "null current cell\n";
                    continue;
                }

                GetDistanceBetween_RunRules( shard, current, target, ref bestBlocked, ref bestBlockedDistance, ref bestShardDistance, ref bestShard );
            }

            if ( bestShard == null )
                blockedBy = bestBlocked;

            return bestShard;
        }
        #endregion

        #region GetDistanceBetween_RunRules
        public static void GetDistanceBetween_RunRules( PlayerShard shard, h.hCell current, h.hCell target, ref h.hCell bestBlocked, 
            ref int bestBlockedDistance, ref int bestShardDistance, ref PlayerShard bestShard )
        {
            if ( target.CurrentNumber == 0 )
                return;

            if ( current.ArrayX == target.ArrayX )
            {
                int distance = MathA.Abs( current.ArrayY - target.ArrayY );
                if ( distance >= bestShardDistance )
                {
                    //debugText = "further than current best\n";
                    return;
                }

                if ( current.ArrayY > target.ArrayY )
                {
                    h.hCell test = current.North;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.IsBlocked || test.CurrentEntity != null || test.CurrentNumber > current.CurrentNumber )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayY == target.ArrayY )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.North;
                    }
                    //debugText = "loopsN: " + loops + "\n";
                }
                else
                {
                    h.hCell test = current.South;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.IsBlocked || test.CurrentEntity != null || test.CurrentNumber > current.CurrentNumber )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayY == target.ArrayY )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.South;
                    }
                    //debugText = "loopsS: " + loops + "\n";
                }
            }
            else if ( current.ArrayY == target.ArrayY )
            {
                int distance = MathA.Abs( current.ArrayX - target.ArrayX );
                if ( distance >= bestShardDistance )
                {
                    //debugText = "further than current best\n";
                    return;
                }

                if ( current.ArrayX < target.ArrayX )
                {
                    h.hCell test = current.East;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.IsBlocked || test.CurrentEntity != null || test.CurrentNumber > current.CurrentNumber )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayX == target.ArrayX )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.East;
                    }
                    //debugText = "loopsE: " + loops + "\n";
                }
                else
                {
                    h.hCell test = current.West;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.IsBlocked || test.CurrentEntity != null || test.CurrentNumber > current.CurrentNumber )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayX == target.ArrayX )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.West;
                    }
                    //debugText = "loopsW: " + loops + "\n";
                }
            }
        }
        #endregion

        #region GetBestShard_JumpRules
        public static PlayerShard GetBestShard_JumpRules( out h.hCell blockedBy )
        {
            blockedBy = null;
            if ( scene.AnimatingItems.Count > 0 )
            {
                //debugText = "animating";
                return null; //no targeting while animating
            }
            h.hCell target = scene.HoveredCell;
            if ( target == null || target.CurrentEntity is PlayerShard || target.CurrentNumber <= 1 )
            {
                //debugText = "target null or blocked or something";
                return null;
            }
            
            h.hCell bestBlocked = null;
            int bestBlockedDistance = 99;

            //debugText = "testing shards: " + PlayerShards.Count + "\n";
            PlayerShard bestShard = null;
            int bestShardDistance = 99;
            foreach ( PlayerShard shard in scene.PlayerShards )
            {
                h.hCell current = shard.CurrentCell;
                if ( current == null )
                {
                    //debugText = "null current cell\n";
                    continue;
                }

                GetDistanceBetween_JumpRules( shard, current, target, ref bestBlocked, ref bestBlockedDistance, ref bestShardDistance, ref bestShard );
            }

            if ( bestShard == null )
                blockedBy = bestBlocked;

            return bestShard;
        }
        #endregion

        #region GetBestShard_InfiltrationSysOpRules
        public static PlayerShard GetBestShard_InfiltrationSysOpRules( out h.hCell blockedBy )
        {
            blockedBy = null;
            if ( scene.AnimatingItems.Count > 0 )
            {
                //debugText = "animating";
                return null; //no targeting while animating
            }
            h.hCell target = scene.HoveredCell;
            if ( target == null || target.CurrentEntity is PlayerShard || target.CurrentNumber <= 1 )
            {
                //debugText = "target null or blocked or something";
                return null;
            }

            h.hCell bestBlocked = null;
            int bestBlockedDistance = 99;

            //debugText = "testing shards: " + PlayerShards.Count + "\n";
            PlayerShard bestShard = null;
            int bestShardDistance = 99;
            foreach ( PlayerShard shard in scene.PlayerShards )
            {
                h.hCell current = shard.CurrentCell;
                if ( current == null )
                {
                    //debugText = "null current cell\n";
                    continue;
                }

                GetDistanceBetween_JumpRules( shard, current, target, ref bestBlocked, ref bestBlockedDistance, ref bestShardDistance, ref bestShard );
            }

            if ( bestShard == null )
                blockedBy = bestBlocked;

            return bestShard;
        }
        #endregion

        #region ClampCellToDistance
        public static void ClampCellToDistance( PlayerShard shard, ref h.hCell effectiveCell, int ClampDistance )
        {
            if ( shard == null || effectiveCell == null )
                return;

            int biggestDist = MathA.Max( MathA.Abs( shard.CurrentCell.ArrayX - effectiveCell.ArrayX ),
                MathA.Abs( shard.CurrentCell.ArrayY - effectiveCell.ArrayY ) );
            if ( biggestDist > ClampDistance )
            {
                //too far
                if ( shard.CurrentCell.ArrayX == effectiveCell.ArrayX )
                {
                    if ( shard.CurrentCell.ArrayY < effectiveCell.ArrayY )
                        effectiveCell = scene.HackingCellArray[shard.CurrentCell.ArrayX, shard.CurrentCell.ArrayY + ClampDistance];
                    else
                        effectiveCell = scene.HackingCellArray[shard.CurrentCell.ArrayX, shard.CurrentCell.ArrayY - ClampDistance];
                }
                else
                {
                    if ( shard.CurrentCell.ArrayX < effectiveCell.ArrayX )
                        effectiveCell = scene.HackingCellArray[shard.CurrentCell.ArrayX + ClampDistance, shard.CurrentCell.ArrayY];
                    else
                        effectiveCell = scene.HackingCellArray[shard.CurrentCell.ArrayX - ClampDistance, shard.CurrentCell.ArrayY];
                }

                if ( effectiveCell == null || effectiveCell.IsBlocked || effectiveCell.CurrentEntity != null )
                    effectiveCell = null;
            }
        }
        #endregion

        #region GetDistanceBetween_JumpRules
        public static void GetDistanceBetween_JumpRules( PlayerShard shard, h.hCell current, h.hCell target, ref h.hCell bestBlocked,
            ref int bestBlockedDistance, ref int bestShardDistance, ref PlayerShard bestShard )
        {
            //if ( target.CurrentNumber == 0 )
            //    return;

            if ( target.IsBlocked || target.CurrentEntity != null || target.CurrentNumber > current.CurrentNumber || target.CurrentNumber <= 1 )
            {
                int blockedDistance = MathA.Min( MathA.Abs( current.ArrayX - target.ArrayX ), MathA.Abs( current.ArrayY - target.ArrayY ) );
                if ( bestBlocked == null || blockedDistance < bestBlockedDistance )
                {
                    bestBlocked = target;
                    bestBlockedDistance = blockedDistance;
                }
                return;
            }

            if ( current.ArrayX == target.ArrayX )
            {
                int distance = MathA.Abs( current.ArrayY - target.ArrayY );
                if ( distance >= bestShardDistance )
                {
                    //debugText = "further than current best\n";
                    return;
                }

                if ( current.ArrayY > target.ArrayY )
                {
                    h.hCell test = current.North;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.CurrentEntity != null )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayY == target.ArrayY )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.North;
                    }
                    //debugText = "loopsN: " + loops + "\n";
                }
                else
                {
                    h.hCell test = current.South;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.CurrentEntity != null )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayY == target.ArrayY )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.South;
                    }
                    //debugText = "loopsS: " + loops + "\n";
                }
            }
            else if ( current.ArrayY == target.ArrayY )
            {
                int distance = MathA.Abs( current.ArrayX - target.ArrayX );
                if ( distance >= bestShardDistance )
                {
                    //debugText = "further than current best\n";
                    return;
                }

                if ( current.ArrayX < target.ArrayX )
                {
                    h.hCell test = current.East;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.CurrentEntity != null )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayX == target.ArrayX )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.East;
                    }
                    //debugText = "loopsE: " + loops + "\n";
                }
                else
                {
                    h.hCell test = current.West;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.CurrentEntity != null )
                        {
                            if ( distance < bestBlockedDistance )
                            {
                                bestBlocked = test;
                                bestBlockedDistance = distance;
                            }
                            break;
                        }
                        if ( test.ArrayX == target.ArrayX )
                        {
                            bestShard = shard;
                            bestShardDistance = distance;
                            break;
                        }
                        else
                            test = test.West;
                    }
                    //debugText = "loopsW: " + loops + "\n";
                }
            }
        }
        #endregion

        #region HandleCorruption_Text
        public static void HandleCorruption_Text( PlayerShard shard )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( scene.PlayerShards.Count > 1 || GetWouldCorruptionEndSession( shard ) )
            {
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                {
                    novel.TitleUpperLeft.AddLang( "Hacking_Corrupt_Brief", ColorTheme.HeaderGoldMoreRich ).Space2x()
                        .AddSpriteStyled_NoIndent( IconRefs.HackingCorrupt.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                }

                CalculateAdjacentCellsOnFire( shard, true, true );
            }
            else
            {
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                {
                    novel.TitleUpperLeft.AddLang( "Hacking_YourShard1" );
                    novel.Main.AddLang( "Hacking_YourShard2", ColorTheme.PurpleDim ).Line()
                        .AddLang( "Hacking_YourShard3", ColorTheme.RedOrange ).Line();
                }

                scene.LastColorSetForHoveredCell = scene.HoveredCell;
                scene.SpecialColorForHoveredCell = ColorTheme.Hack_YourShard_Hovered;
            }
        }
        #endregion

        #region GetWouldCorruptionEndSession
        public static bool GetWouldCorruptionEndSession( PlayerShard shard )
        {
            if ( shard == null )
                return false;

            h.hCell cell = shard.CurrentCell;
            if ( cell  == null ) 
                return false;
            {
                if ( cell.North != null && cell.North.CurrentEntity is Daemon dNorth )
                {
                    if ( dNorth.DaemonType.WillEndHackingSessionIfCorrupted )
                        return true;
                }
            }
            {
                if ( cell.South != null && cell.South.CurrentEntity is Daemon dSouth )
                {
                    if ( dSouth.DaemonType.WillEndHackingSessionIfCorrupted )
                        return true;
                }
            }
            {
                if ( cell.East != null && cell.East.CurrentEntity is Daemon dEast )
                {
                    if ( dEast.DaemonType.WillEndHackingSessionIfCorrupted )
                        return true;
                }
            }
            {
                if ( cell.West != null && cell.West.CurrentEntity is Daemon dWest )
                {
                    if ( dWest.DaemonType.WillEndHackingSessionIfCorrupted )
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region MarkHackerDefective
        public static void MarkHackerDefective()
        {
            ISimMachineActor hacker = scene.HackerUnit;
            if ( hacker == null )
                return;

            hacker.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
            hacker.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );

            if ( hacker is ISimMachineUnit unit && unit.ContainerLocation.Get() is ISimMachineVehicle containerVehicle && containerVehicle.OutcastLevel < 1 )
            {
                containerVehicle.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                containerVehicle.ApplyVisibilityFromAction( ActionVisibility.IsAttack );
            }
        }
        #endregion

        #region HandleInfiltrationSysOpCorruption_Text
        public static void HandleInfiltrationSysOpCorruption_Text( PlayerShard shard )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
            {
                novel.TitleUpperLeft.AddLang( "Hacking_Corrupt_Brief", ColorTheme.HeaderGoldMoreRich ).Space2x()
                    .AddSpriteStyled_NoIndent( IconRefs.HackingCorrupt.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
            }

            CalculateAdjacentCellsOnFire( shard, false, true );
        }
        #endregion

        #region HandleFirewall_Text
        public static void HandleFirewall_Text( PlayerShard shard, HackingAction Action )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( scene.PlayerShards.Count > 1 )
            {
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                {
                    novel.ShouldTooltipBeRed = ResourceRefs.MentalEnergy.Current == 0;
                    novel.TitleUpperLeft.AddLang( "Hacking_CreateFirewall_Brief", ColorTheme.HeaderGoldMoreRich ).Space2x()
                        .AddSpriteStyled_NoIndent( Action.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                    novel.MainHeader.AddLangAndAfterLineItemHeader( "Cost", ColorTheme.CyanDim )
                        .AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.HeaderGoldMoreRich )
                        .AddFormat2( "OutOF", 1, ResourceRefs.MentalEnergy.Current.ToStringWholeBasic(),
                        ResourceRefs.MentalEnergy.Current > 0 ? ColorTheme.HeaderGoldMoreRich : ColorTheme.RedOrange2 );
                }

                CalculateFireLines( shard );
            }
            else
            {
                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                {
                    novel.TitleUpperLeft.AddLang( "Hacking_YourShard1" );
                    novel.Main.AddLang( "Hacking_YourShard2", ColorTheme.PurpleDim ).Line()
                        .AddLang( "Hacking_YourShard3", ColorTheme.RedOrange ).Line();
                }

                scene.LastColorSetForHoveredCell = scene.HoveredCell;
                scene.SpecialColorForHoveredCell = ColorTheme.Hack_YourShard_Hovered;
            }
        }
        #endregion

        #region HandleCorruption_Click
        public static void HandleCorruption_Click( PlayerShard shard, bool DoFireLines )
        {
            if ( FlagRefs.HackTut_OverallGoal.DuringGameplay_CompleteIfActive() )
                FlagRefs.HackTut_LeafNode.DuringGameplay_StartIfNeeded();

            if ( DoFireLines )
            {
                if ( scene.PlayerShards.Count <= 1 )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }
            }
            else
            {
                if ( scene.PlayerShards.Count <= 1 && !GetWouldCorruptionEndSession( shard ) )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return;
                }
            }

            if ( DoFireLines )
                CalculateFireLines( shard );
            else
                CalculateAdjacentCellsOnFire( shard, true, true );

            if ( scene.NextCellsOnFire.Count > 0 )
            {
                bool hadAnyShardThatWillLive = false;
                if ( DoFireLines )
                    hadAnyShardThatWillLive = true;
                else
                {
                    foreach ( PlayerShard otherShard in scene.PlayerShards )
                    {
                        if ( otherShard.CurrentCell != null && !scene.NextCellsOnFire.ContainsKey( otherShard.CurrentCell ) )
                        {
                            hadAnyShardThatWillLive = true;
                            break;
                        }
                    }
                }

                if ( !hadAnyShardThatWillLive )
                {
                    if ( DoFireLines )
                    {
                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                        return;
                    }
                    else
                    {
                        if ( !GetWouldCorruptionEndSession( shard ) )
                        {
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                            return;
                        }
                    }
                }

                int daemonsKilled = 0;

                bool isBeyondFirstCorrupt = false;
                bool isBeyondFirstDeath = false;
                foreach ( KeyValuePair<h.hCell, bool> kv in scene.NextCellsOnFire )
                {
                    bool didAnyChanges = false;
                    if ( kv.Key.CurrentNumber > 0 || kv.Key.IsBlocked )
                    {
                        if ( DoFireLines && kv.Key.CurrentEntity != null && kv.Key.CurrentEntity is PlayerShard )
                        { } //do not zero this one!
                        else
                        {
                            didAnyChanges = true;
                            kv.Key.SetCurrentNumber( 0 );
                            kv.Key.IsBlocked = false;
                        }
                    }

                    Vector3 screenPos = CommonRefs.HackingScene.MainCamera.WorldToScreenPoint( kv.Key.Trans.position );

                    if ( kv.Key.CurrentEntity != null )
                    {
                        if ( kv.Key.CurrentEntity is PlayerShard )
                        {
                            if ( !DoFireLines )
                            {
                                didAnyChanges = true;
                                ParticleSoundRefs.Hacking_Corrupt_ShardDeath.DuringGame_PlayAtUILocation( screenPos, true );
                                kv.Key.CurrentEntity.DestroyEntity();
                                CityStatisticTable.AlterScore( "ConsciousnessShardsCorrupted", 1 );
                            }
                        }
                        else
                        {
                            if ( kv.Key.CurrentEntity is Daemon daemon )
                            {
                                HackingDaemonResult result = daemon.DaemonType.Implementation.TryHandleDaemonLogic( daemon.DaemonType, daemon, null, HackingDaemonLogic.HitByCorruptionFire );
                                if ( result == HackingDaemonResult.KilledTarget )
                                {
                                    didAnyChanges = true;
                                    daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstDeath );
                                    isBeyondFirstDeath = true;
                                    CityStatisticTable.AlterScore( "HackingDaemonsCorrupted", 1 );
                                    daemonsKilled++;

                                    if ( FlagRefs.HackTut_DestroyADaemon.DuringGameplay_CompleteIfActive() )
                                        FlagRefs.HackTut_FinishTheJob.DuringGameplay_StartIfNeeded();
                                }
                            }
                            else
                            {
                                ParticleSoundRefs.Hacking_Corrupt_ShardDeath.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstDeath );
                                isBeyondFirstDeath = true;
                                didAnyChanges = true;
                            }
                        }
                    }

                    if ( didAnyChanges )
                    {
                        ParticleSoundRefs.Hacking_Corrupt.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstCorrupt );
                        isBeyondFirstCorrupt = true;
                    }
                }
                if ( isBeyondFirstCorrupt )
                {
                    if ( DoFireLines )
                    {
                        CityStatisticTable.AlterScore( "HackingFirewalls", 1 );
                        ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    scene.IsDaemonTurnToMove = true;
                }

                if ( daemonsKilled > 1 && !DoFireLines )
                {
                    if ( !AchievementRefs.KillStreak.OneTimeline_HasBeenTripped )
                        AchievementRefs.KillStreak.TripIfNeeded();
                }
            }
        }
        #endregion

        #region HandleInfiltrationSysOpCorruption_Click
        public static void HandleInfiltrationSysOpCorruption_Click( PlayerShard shard )
        {
            if ( FlagRefs.HackTut_OverallGoal.DuringGameplay_CompleteIfActive() )
                FlagRefs.HackTut_LeafNode.DuringGameplay_StartIfNeeded();

            if ( scene.PlayerShards.Count < 1 )
            {
                ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                return;
            }

            CalculateAdjacentCellsOnFire( shard, false, true );
            if ( scene.NextCellsOnFire.Count > 0 )
            {
                bool isBeyondFirstCorrupt = false;
                bool isBeyondFirstDeath = false;
                foreach ( KeyValuePair<h.hCell, bool> kv in scene.NextCellsOnFire )
                {
                    bool didAnyChanges = false;
                    if ( kv.Key.CurrentNumber > 0 || kv.Key.IsBlocked )
                    {
                        didAnyChanges = true;
                        kv.Key.SetCurrentNumber( 10 );
                        kv.Key.IsBlocked = false;
                    }

                    Vector3 screenPos = CommonRefs.HackingScene.MainCamera.WorldToScreenPoint( kv.Key.Trans.position );

                    if ( kv.Key.CurrentEntity != null )
                    {
                        if ( kv.Key.CurrentEntity is PlayerShard )
                        {
                            //nothing to do to ourselves
                        }
                        else
                        {
                            if ( kv.Key.CurrentEntity is Daemon daemon )
                            {
                                HackingDaemonResult result = daemon.DaemonType.Implementation.TryHandleDaemonLogic( daemon.DaemonType, daemon, null, HackingDaemonLogic.HitByCorruptionFire );
                                if ( result == HackingDaemonResult.KilledTarget )
                                {
                                    didAnyChanges = true;
                                    daemon.DaemonType.OnDeath.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstDeath );
                                    isBeyondFirstDeath = true;
                                    CityStatisticTable.AlterScore( "HackingDaemonsCorrupted", 1 );

                                    if ( FlagRefs.HackTut_DestroyADaemon.DuringGameplay_CompleteIfActive() )
                                        FlagRefs.HackTut_FinishTheJob.DuringGameplay_StartIfNeeded();
                                }
                            }
                            else
                            {
                                ParticleSoundRefs.Hacking_Corrupt_ShardDeath.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstDeath );
                                isBeyondFirstDeath = true;
                                didAnyChanges = true;
                            }
                        }
                    }

                    if ( didAnyChanges )
                    {
                        ParticleSoundRefs.Hacking_Corrupt.DuringGame_PlayAtUILocation( screenPos, isBeyondFirstCorrupt );
                        isBeyondFirstCorrupt = true;
                    }
                }
                if ( isBeyondFirstCorrupt )
                {
                    scene.IsDaemonTurnToMove = true;
                }
            }
        }
        #endregion

        #region CalculateAdjacentCellsOnFire
        public static void CalculateAdjacentCellsOnFire( PlayerShard shardDoingTheCorruption, bool BurnsSelf, bool CanBurnBlocked )
        {
            scene.NextCellsOnFire.Clear();

            h.hCell startingCell = shardDoingTheCorruption?.CurrentCell;
            if ( startingCell == null || shardDoingTheCorruption == null )
                return;

            foreach ( h.hCell adjacent in startingCell.AdjacentCells )
            {
                if ( //( !MustBeHigherThan || adjacent.CurrentNumber <= startingCell.CurrentNumber ) &&
                    (CanBurnBlocked || !adjacent.IsBlocked ) )
                {
                    //setAnyAdjacentOnFire = true;
                    scene.NextCellsOnFire[adjacent] = true;
                }
            }

            if ( BurnsSelf )
                scene.NextCellsOnFire[startingCell] = true;
        }
        #endregion

        #region CalculateFireLines
        public static void CalculateFireLines( PlayerShard shardDoingTheCorruption )
        {
            scene.NextCellsOnFire.Clear();

            h.hCell startingCell = shardDoingTheCorruption?.CurrentCell;
            if ( startingCell == null || shardDoingTheCorruption == null )
                return;

            if ( scene.PlayerShards.Count > 1 )
            {
                foreach ( PlayerShard otherShard in scene.PlayerShards )
                {
                    if ( otherShard == shardDoingTheCorruption )
                        continue;
                    h.hCell otherCell = otherShard?.CurrentCell;
                    if ( otherCell == null )
                        continue;

                    if ( GetIsClearLineBetween_CorruptionRules( startingCell, otherCell ) )
                        SetLineOnFireBetween_CorruptionRules( startingCell, otherCell );
                }
            }
        }
        #endregion

        #region GetIsClearLineBetween_CorruptionRules
        public static bool GetIsClearLineBetween_CorruptionRules( h.hCell current, h.hCell target )
        {
            int comparisonNumber = MathA.Max( current.CurrentNumber, target.CurrentNumber );

            if ( current.ArrayX == target.ArrayX )
            {
                if ( current.ArrayY > target.ArrayY )
                {
                    h.hCell test = current.North;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.ArrayY == target.ArrayY )
                            return true;
                        else
                        {
                            if ( test.CurrentNumber > comparisonNumber && test.CurrentEntity == null )
                                return false;
                            test = test.North;
                        }
                    }
                }
                else
                {
                    h.hCell test = current.South;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.ArrayY == target.ArrayY )
                            return true;
                        else
                        {
                            if ( test.CurrentNumber > comparisonNumber && test.CurrentEntity == null )
                                return false;
                            test = test.South;
                        }
                    }
                }
            }
            else if ( current.ArrayY == target.ArrayY )
            {
                if ( current.ArrayX < target.ArrayX )
                {
                    h.hCell test = current.East;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.ArrayX == target.ArrayX )
                            return true;
                        else
                        {
                            if ( test.CurrentNumber > comparisonNumber && test.CurrentEntity == null )
                                return false;
                            test = test.East;
                        }
                    }
                }
                else
                {
                    h.hCell test = current.West;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        if ( test.ArrayX == target.ArrayX )
                            return true;
                        else
                        {
                            if ( test.CurrentNumber > comparisonNumber && test.CurrentEntity == null )
                                return false;
                            test = test.West;
                        }
                    }
                }
            }

            return false;
        }
        #endregion

        #region SetLineOnFireBetween_CorruptionRules
        public static void SetLineOnFireBetween_CorruptionRules( h.hCell current, h.hCell target )
        {
            scene.NextCellsOnFire[current] = true;
            scene.NextCellsOnFire[target] = true;
            if ( current.ArrayX == target.ArrayX )
            {
                if ( current.ArrayY > target.ArrayY )
                {
                    h.hCell test = current.North;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        scene.NextCellsOnFire[test] = true;
                        if ( test.ArrayY == target.ArrayY )
                            return;
                        else
                            test = test.North;
                    }
                }
                else
                {
                    h.hCell test = current.South;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        scene.NextCellsOnFire[test] = true;
                        if ( test.ArrayY == target.ArrayY )
                            return;
                        else
                            test = test.South;
                    }
                }
            }
            else if ( current.ArrayY == target.ArrayY )
            {
                if ( current.ArrayX < target.ArrayX )
                {
                    h.hCell test = current.East;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        scene.NextCellsOnFire[test] = true;
                        if ( test.ArrayX == target.ArrayX )
                            return;
                        else
                            test = test.East;
                    }
                }
                else
                {
                    h.hCell test = current.West;
                    int loops = 0;
                    while ( test != null && loops++ < 100 )
                    {
                        scene.NextCellsOnFire[test] = true;
                        if ( test.ArrayX == target.ArrayX )
                            return;
                        else
                            test = test.West;
                    }
                }
            }
        }
        #endregion

        #region PopulateStandardNumberSubstrate
        public static void PopulateStandardNumberSubstrate( MersenneTwister Rand )
        {
            foreach ( h.hCell item in scene.AllHackingCells )
            {
                int range = Rand.Next( 0, 100 );
                byte result;
                if ( range < 25 )
                    result = (byte)Rand.Next( 30, 40 );
                else if ( range < 35 )
                    result = (byte)Rand.Next( 40, 50 );
                else if ( range < 43 )
                    result = (byte)Rand.Next( 50, 60 );
                else if ( range < 50 )
                    result = (byte)Rand.Next( 60, 70 );
                else if ( range < 54 )
                    result = (byte)Rand.Next( 10, 20 );
                else
                    result = (byte)Rand.Next( 20, 30 );

                item.SetCurrentNumber( result );
            }
        }
        #endregion

        #region PopulateInfiltrationSysOpNumberSubstrate
        public static void PopulateInfiltrationSysOpNumberSubstrate( MersenneTwister Rand )
        {
            foreach ( h.hCell item in scene.AllHackingCells )
            {
                item.SetCurrentNumber( 10 );
            }
        }
        #endregion

        #region PopulateStandardBlockages
        public static void PopulateStandardBlockages( MersenneTwister Rand )
        {
            int blockedItems = Mathf.CeilToInt( scene.HackingBlockagesDifficulty / 40f );
            if ( blockedItems < 8 )
                blockedItems = 8;
            if ( blockedItems > 150 )
                blockedItems = 150;

            int outerAttemptCount = 100;
            while ( blockedItems > 0 && outerAttemptCount-- > 0 )
            {
                int blockageLength = Rand.Next( 4, 8 );
                if ( blockageLength > blockedItems )
                    blockageLength = blockedItems;

                h.hCell blockedItem = null;
                int attemptCount = 100;
                while ( blockedItem == null && attemptCount-- > 0 )
                {
                    blockedItem = scene.AllHackingCells.GetRandom( Rand );
                    if ( blockedItem.IsBlocked )
                        blockedItem = null; //skip because was already done
                }
                while ( blockageLength > 0 && blockedItem != null )
                {
                    if ( !blockedItem.IsBlocked )
                    {
                        blockedItem.IsBlocked = true;
                        blockageLength--;
                        blockedItems--;
                    }

                    int dir = Rand.Next( 0, 4 );
                    switch ( dir )
                    {
                        case 0:
                            {
                                if ( blockedItem.North != null && !blockedItem.North.IsBlocked )
                                    blockedItem = blockedItem.North;
                                else if ( blockedItem.South != null && !blockedItem.South.IsBlocked )
                                    blockedItem = blockedItem.South;
                                else
                                    blockedItem = null;
                            }
                            break;
                        case 1:
                            {
                                if ( blockedItem.South != null && !blockedItem.South.IsBlocked )
                                    blockedItem = blockedItem.South;
                                else if ( blockedItem.North != null && !blockedItem.North.IsBlocked )
                                    blockedItem = blockedItem.North;
                                else
                                    blockedItem = null;
                            }
                            break;
                        case 2:
                            {
                                if ( blockedItem.East != null && !blockedItem.East.IsBlocked )
                                    blockedItem = blockedItem.East;
                                else if ( blockedItem.West != null && !blockedItem.West.IsBlocked )
                                    blockedItem = blockedItem.West;
                                else
                                    blockedItem = null;
                            }
                            break;
                        default:
                            {
                                if ( blockedItem.West != null && !blockedItem.West.IsBlocked )
                                    blockedItem = blockedItem.West;
                                else if ( blockedItem.East != null && !blockedItem.East.IsBlocked )
                                    blockedItem = blockedItem.East;
                                else
                                    blockedItem = null;
                            }
                            break;
                    }
                }
            }
        }
        #endregion

        #region PopulateInfiltrationSysOpShard
        public static void PopulateInfiltrationSysOpShard( MersenneTwister Rand )
        {
            int shardCount = 1;

            scene.AddToHackingHistory( Lang.Get( "InfiltrationSysOpLog_OneShard" ) );

            int attemptCount = 400;
            while ( shardCount > 0 && attemptCount-- > 0 )
            {
                h.hCell cell = scene.AllHackingCells.GetRandom( Rand );
                if ( cell == null || cell.IsBlocked || cell.CurrentEntity != null )
                    continue;

                CreateNewPlayerShardAtCell( cell, 10 );
                shardCount--;
            }
        }
        #endregion

        #region PopulateStandardFirstShards
        public static void PopulateStandardFirstShards( MersenneTwister Rand )
        {
            int hackerSkill = scene.HackerUnit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
            int shardHealth = Mathf.RoundToInt( hackerSkill / 4f );
            if ( shardHealth < 40 )
                shardHealth = 40;

            int shardCount = shardHealth < 80 ? 1 : Mathf.CeilToInt( shardHealth / 80f );
            if ( shardCount > 3 )
                shardCount = 3;
            if ( shardHealth > 80 )
                shardHealth = 80;

            if ( shardCount == 1 )
                scene.AddToHackingHistory( Lang.Get( "HackingLog_OneShard" ) );
            else
                scene.AddToHackingHistory( Lang.Format1( "HackingLog_MultipleShards", shardCount ) );

            int attemptCount = 400;
            while ( shardCount > 0 && attemptCount-- > 0 )
            {
                h.hCell cell = scene.AllHackingCells.GetRandom( Rand );
                if ( cell == null || cell.IsBlocked || cell.CurrentEntity != null )
                    continue;

                CreateNewPlayerShardAtCell( cell, shardHealth );
                shardCount--;
            }
        }
        #endregion

        #region TryPlaceADaemon
        public static Daemon TryPlaceADaemon( HackingDaemonType DaemonType, bool IsHidden, int MinDistanceFromShards, int MinDistanceFromOtherDaemons, int MinDistanceFromEdge, MersenneTwister Rand )
        {
            if ( DaemonType == null )
            {
                ArcenDebugging.LogSingleLine( "Null DaemonType passed in to TryPlaceADaemon!", Verbosity.ShowAsError );
                return null; 
            }

            int attemptCount = 400;
            while ( attemptCount-- > 0 )
            {
                h.hCell cell = scene.AllHackingCells.GetRandom( Rand );
                if ( cell == null || cell.CurrentEntity != null ) //cell.IsBlocked || 
                    continue;

                if ( MinDistanceFromEdge > 0 )
                {
                    if ( cell.ArrayX < MinDistanceFromEdge || 
                        cell.ArrayY < MinDistanceFromEdge ||
                        scene.ArrayWidth - cell.ArrayX < MinDistanceFromEdge ||
                        scene.ArrayHeight - cell.ArrayY < MinDistanceFromEdge )
                        continue; //too close to an edge
                }

                bool foundBlockage = false;
                foreach ( PlayerShard shard in scene.PlayerShards )
                {
                    if ( MathA.Abs( shard.CurrentCell.ArrayX - cell.ArrayX ) < MinDistanceFromShards &&
                        MathA.Abs( shard.CurrentCell.ArrayY - cell.ArrayY ) < MinDistanceFromShards )
                    {
                        foundBlockage = true; 
                        break;
                    }
                }
                if ( foundBlockage )
                    continue;
                foreach ( Daemon daemon in scene.Daemons )
                {
                    if ( MathA.Abs( daemon.CurrentCell.ArrayX - cell.ArrayX ) < MinDistanceFromOtherDaemons &&
                        MathA.Abs( daemon.CurrentCell.ArrayY - cell.ArrayY ) < MinDistanceFromOtherDaemons )
                    {
                        foundBlockage = true;
                        break;
                    }
                }
                if ( foundBlockage )
                    continue;

                return CreateNewDaemonAtCell( cell, DaemonType, IsHidden );
            }
            return null;
        }
        #endregion

        #region TryPlaceADaemonAdjacent
        public static Daemon TryPlaceADaemonAdjacent( HackingDaemonType DaemonType, h.hCell Cell, bool StartAsHidden, MersenneTwister Rand )
        {
            if ( DaemonType == null )
            {
                ArcenDebugging.LogSingleLine( "Null DaemonType passed in to TryPlaceADaemonAdjacent!", Verbosity.ShowAsError );
                return null;
            }

            foreach ( h.hCell adjacent in Cell.AdjacentCells.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return CreateNewDaemonAtCell( adjacent, DaemonType, StartAsHidden );
            }

            foreach ( h.hCell adjacent in Cell.AdjacentCellsAndDiagonal.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return CreateNewDaemonAtCell( adjacent, DaemonType, StartAsHidden );
            }

            foreach ( h.hCell adjacent in Cell.AdjacentCellsAndDiagonal2X.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return CreateNewDaemonAtCell( adjacent, DaemonType, StartAsHidden );
            }

            return null;
        }
        #endregion

        #region CreateNewPlayerShardAtCell
        public static PlayerShard CreateNewPlayerShardAtCell( h.hCell cell, int amountToSetOnCell )
        {
            if ( cell == null )
                return null;
            PlayerShard shard = new PlayerShard();
            shard.InitializeAtCell( cell, amountToSetOnCell );
            scene.PlayerShards.Add( shard );
            return shard;
        }
        #endregion

        #region TryPlaceADaemonSoftening
        public static Daemon TryPlaceADaemonSoftening( HackingDaemonType DaemonType, bool IsHidden, int minDistanceFromShards, 
            int minDistanceFromOtherDaemons, int minDistanceFromEdge, MersenneTwister Rand )
        {
            if ( DaemonType == null )
                return null;

            while ( minDistanceFromShards > 0 || minDistanceFromOtherDaemons > 0 || minDistanceFromEdge > 0 )
            {
                Daemon result = TryPlaceADaemon( DaemonType, IsHidden, minDistanceFromShards, minDistanceFromOtherDaemons, minDistanceFromEdge, Rand );
                if ( result != null ) 
                    return result;
                if ( minDistanceFromOtherDaemons > 2 )
                    minDistanceFromOtherDaemons--;
                else if ( minDistanceFromShards > 3 )
                    minDistanceFromShards--;
                else if ( minDistanceFromEdge > 1 )
                    minDistanceFromEdge--;
                else if( minDistanceFromOtherDaemons > 0 )
                    minDistanceFromOtherDaemons--;
                else if ( minDistanceFromShards > 0 )
                    minDistanceFromShards--;
                else if ( minDistanceFromEdge > 0 )
                    minDistanceFromEdge--;
            }
            return null; //all failed!
        }
        #endregion

        #region CreateNewDaemonAtCell
        public static Daemon CreateNewDaemonAtCell( h.hCell cell, HackingDaemonType DaemonType, bool IsHidden )
        {
            if ( cell == null )
            {
                ArcenDebugging.LogSingleLine( "Null cell passed in to CreateNewDaemonAtCell!", Verbosity.ShowAsError );
                return null;
            }
            if ( DaemonType == null )
            {
                ArcenDebugging.LogSingleLine( "Null DaemonType passed in to CreateNewDaemonAtCell!", Verbosity.ShowAsError );
                return null;
            }
            Daemon daemon = new Daemon();
            daemon.InitializeAtCell( cell, DaemonType, IsHidden );
            scene.Daemons.Add( daemon );
            return daemon;
        }
        #endregion

        #region DoBasicDaemonChaseLogic
        public static void DoBasicDaemonChaseLogic( Daemon daemon )
        {
            if ( daemon == null || daemon.DaemonType == null )
                return;
            PlayerShard shardToChase = FindClosestShard_FlyingCardinal( daemon.CurrentCell );
            if ( shardToChase != null )
            {
                int moveDist = daemon.DaemonType.GetSingleIntByID( "MoveRange", 2 );
                h.hCell dest = HackingHelper.FlyingCardinalMoveToward( daemon.CurrentCell, shardToChase.CurrentCell, moveDist );
                if ( dest != null )
                {
                    ParticleSoundRefs.Hacking_DaemonMove_Start.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                    daemon.MoveTo( dest );
                    int moveFrequency = daemon.DaemonType.GetSingleIntByID( "MoveFrequency", 1 );
                    if ( moveFrequency > 1 )
                    {
                        moveFrequency--;
                        daemon.AdditionalMovesToSkip = moveFrequency;
                    }
                }
            }
        }
        #endregion

        #region DoDaemonWanderOrLeapLogic
        public static void DoDaemonWanderOrLeapLogic( Daemon daemon, MersenneTwister Rand )
        {
            if ( daemon == null || daemon.DaemonType == null )
                return;
            PlayerShard shardToChase = FindClosestShard_FlyingCardinal( daemon.CurrentCell );
            if ( shardToChase != null )
            {
                int moveDist = daemon.DaemonType.GetSingleIntByID( "MoveRange", 2 );
                h.hCell dest = HackingHelper.FlyingCardinalMoveToward( daemon.CurrentCell, shardToChase.CurrentCell, moveDist );
                if ( dest != null && dest == shardToChase.CurrentCell )//only go there if this is actually within range
                {
                    ParticleSoundRefs.Hacking_DaemonMove_Start.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                    daemon.MoveTo( dest );
                    int moveFrequency = daemon.DaemonType.GetSingleIntByID( "MoveFrequency", 1 );
                    if ( moveFrequency > 1 )
                    {
                        moveFrequency--;
                        daemon.AdditionalMovesToSkip = moveFrequency;
                    }
                }
                else
                {
                    dest = FindRandomAdjacentCellsAndDiagonal2XWithoutEntity( daemon.CurrentCell, Rand );
                    ParticleSoundRefs.Hacking_DaemonMove_Start.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                    daemon.MoveTo( dest );
                    int moveFrequency = daemon.DaemonType.GetSingleIntByID( "MoveFrequency", 1 );
                    if ( moveFrequency > 1 )
                    {
                        moveFrequency--;
                        daemon.AdditionalMovesToSkip = moveFrequency;
                    }
                }
            }
        }
        #endregion

        #region DoDaemonChaseDaemonLogic
        public static void DoDaemonChaseDaemonLogic( Daemon daemon )
        {
            if ( daemon == null || daemon.DaemonType == null )
                return;

            Daemon daemonToChase = FindClosestDaemonThatCaneBeHunted_FlyingCardinal( daemon.CurrentCell );

            //ArcenDebugging.LogSingleLine( daemon.DaemonType.ID + " wants to hunt daemons.  Found: " + (daemonToChase?.DaemonType?.ID??"null"), Verbosity.DoNotShow );
            if ( daemonToChase != null )
            {
                int moveDist = daemon.DaemonType.GetSingleIntByID( "MoveRange", 2 );
                h.hCell dest = FlyingCardinalMoveToward( daemon.CurrentCell, daemonToChase.CurrentCell, moveDist );
                if ( dest != null )
                {
                    ParticleSoundRefs.Hacking_DaemonMove_Start.DuringGame_PlayAtUILocation( daemon.CurrentCell.CalculateScreenPos() );
                    daemon.MoveTo( dest );
                    int moveFrequency = daemon.DaemonType.GetSingleIntByID( "MoveFrequency", 1 );
                    if ( moveFrequency > 1 )
                    {
                        moveFrequency--;
                        daemon.AdditionalMovesToSkip = moveFrequency;
                    }
                }
            }
        }
        #endregion

        #region DoBasicDaemonThreatenLogic
        public static void DoBasicDaemonThreatenLogic( Daemon daemon )
        {
            if ( daemon == null || daemon.DaemonType == null )
                return;
            if ( daemon.AdditionalMovesToSkip > 0 )
                return;

            h.hCell source = daemon.CurrentCell;
            if ( source == null )
                return;

            int moveDist = daemon.DaemonType.GetSingleIntByID( "MoveRange", 2 );

            if ( moveDist <= 0 )
                return;

            if ( moveDist == 1 )
            {
                foreach ( h.hCell adjacent in source.AdjacentCells )
                    scene.NextCellsThreatened[adjacent]++;
            }
            else
            {
                int xMin = source.ArrayX - moveDist;
                int xMax = source.ArrayX + moveDist;
                int yMin = source.ArrayY - moveDist;
                int yMax = source.ArrayY + moveDist;

                if ( xMin < 0 ) xMin = 0;
                if ( yMin < 0 ) yMin = 0;
                if ( xMax > scene.ArrayWidth - 1 ) xMax = scene.ArrayWidth - 1;
                if ( yMax > scene.ArrayHeight - 1 ) yMax = scene.ArrayHeight - 1;

                for ( int x = xMin; x <= xMax; x++ )
                {
                    for ( int y = yMin; y <= yMax; y++ )
                    {
                        int dist = MathA.Abs( x - source.ArrayX ) + MathA.Abs( y - source.ArrayY );
                        if ( dist > moveDist )
                            continue;

                        h.hCell other = scene.HackingCellArray[x, y];
                        if ( other != null )
                            scene.NextCellsThreatened[other]++;

                    }
                }
            }
        }
        #endregion

        #region FindClosestDaemonThatCaneBeHunted_FlyingCardinal
        /// <summary>
        /// Can pass through blockages (flying), and only moves in cardinal directions
        /// </summary>
        public static Daemon FindClosestDaemonThatCaneBeHunted_FlyingCardinal( h.hCell startingPoint )
        {
            if ( startingPoint == null )
                return null;
            int bestDist = 99;
            Daemon bestDaemon = null;
            foreach ( Daemon daemon in scene.Daemons )
            {
                if ( !daemon.DaemonType.CanDaemonBeHuntedByFriendlyDaemons )
                    continue;

                int dist = MathA.Abs( daemon.CurrentCell.ArrayX - startingPoint.ArrayX ) + MathA.Abs( daemon.CurrentCell.ArrayY - startingPoint.ArrayY );
                if ( dist < bestDist || bestDaemon == null )
                {
                    bestDist = dist;
                    bestDaemon = daemon;
                }
            }
            return bestDaemon;
        }
        #endregion

        #region FindClosestShard_FlyingCardinal
        /// <summary>
        /// Can pass through blockages (flying), and only moves in cardinal directions
        /// </summary>
        public static PlayerShard FindClosestShard_FlyingCardinal( h.hCell startingPoint )
        {
            if ( startingPoint == null )
                return null;
            int bestDist = 99;
            PlayerShard bestShard = null;
            foreach ( PlayerShard shard in scene.PlayerShards )
            {
                int dist = MathA.Abs( shard.CurrentCell.ArrayX - startingPoint.ArrayX ) + MathA.Abs( shard.CurrentCell.ArrayY - startingPoint.ArrayY );
                if ( dist < bestDist || bestShard == null )
                {
                    bestDist = dist;
                    bestShard = shard;
                }
            }
            return bestShard;
        }
        #endregion

        #region FlyingCardinalMoveToward
        /// <summary>
        /// Can pass through blockages (flying), and only moves in cardinal directions
        /// </summary>
        public static h.hCell FlyingCardinalMoveToward( h.hCell startingPoint, h.hCell targetPoint, int DistToMove )
        {
            if ( startingPoint == null || targetPoint == null || DistToMove <= 0 )
                return null;

            //ArcenDebugging.LogSingleLine( "DistToMove: " + DistToMove + " from " + startingPoint.ArrayX + "," + startingPoint.ArrayY +
            //    " to " + targetPoint.ArrayX + "," + targetPoint.ArrayY, Verbosity.DoNotShow );

            h.hCell currentCell = startingPoint;
            h.hCell priorCell = startingPoint;
            for ( int i = 0; i < DistToMove; i++ )
            {
                int diffX = MathA.Abs( targetPoint.ArrayX - currentCell.ArrayX );
                int diffY = MathA.Abs( targetPoint.ArrayY - currentCell.ArrayY );
                if ( diffX == 0 && diffY == 0 )
                    return currentCell; //we made it!

                priorCell = currentCell;

                //ArcenDebugging.LogSingleLine( "i: " + i + " " + currentCell.ArrayX + "," + currentCell.ArrayY +
                //    " diffs: " + diffX + "," + diffY, Verbosity.DoNotShow );

                if ( diffX > diffY )
                {
                    //move in the x axis
                    if ( targetPoint.ArrayX < currentCell.ArrayX )
                    {
                        currentCell = currentCell.West;
                        //ArcenDebugging.LogSingleLine( "moveWest: " + " " + (currentCell?.ArrayX ?? -3) + "," + (currentCell?.ArrayY ?? -3), Verbosity.DoNotShow );
                    }
                    else
                    {
                        currentCell = currentCell.East;
                        //ArcenDebugging.LogSingleLine( "moveEast: " + " " + (currentCell?.ArrayX ?? -3) + "," + (currentCell?.ArrayY ?? -3), Verbosity.DoNotShow );
                    }
                }
                else
                {
                    //move in the y axis
                    if ( targetPoint.ArrayY < currentCell.ArrayY )
                    {
                        currentCell = currentCell.North;
                        //ArcenDebugging.LogSingleLine( "moveNorth: " + " " + (currentCell?.ArrayX??-3) + "," + (currentCell?.ArrayY ?? -3), Verbosity.DoNotShow );
                    }
                    else
                    {
                        currentCell = currentCell.South;
                        //ArcenDebugging.LogSingleLine( "moveSouth: " + " " + (currentCell?.ArrayX ?? -3) + "," + (currentCell?.ArrayY ?? -3), Verbosity.DoNotShow );
                    }
                }

                if ( currentCell == null )
                    return priorCell;
            }

            return currentCell;
        }
        #endregion

        public readonly static DrawBag<HackingDaemonType> DaemonTypeBag = DrawBag<HackingDaemonType>.Create_WillNeverBeGCed( 20, "HackingHelper-DaemonTypeBag" );

        #region FillDaemonTypeBagFromTagUse
        public static DrawBag<HackingDaemonType> FillDaemonTypeBagFromTagUse( string TagUseName )
        {
            DaemonTypeBag.Clear();
            HackingDaemonTagUse tagUse = HackingDaemonTagUseTable.Instance.GetRowByIDOrNullIfNotFound( TagUseName );
            if ( tagUse == null )
            {
                ArcenDebugging.LogSingleLine( "Asked for HackingDaemonTagUse '" + TagUseName + "', but that's not in the table!", Verbosity.ShowAsError );
                return DaemonTypeBag;
            }

            if ( !scene.Scenario.TagUses.TryGetValue( tagUse, out HackingDaemonTag tag ) )
            {
                ArcenDebugging.LogSingleLine( "Asked for HackingDaemonTagUse '" + TagUseName + "', the scenario '" + scene.Scenario.ID + "' does not include it!", Verbosity.ShowAsError );
                return DaemonTypeBag;
            }

            foreach ( HackingDaemonType daemonType in tag.Types )
            {
                if ( scene.HackingSeedDifficulty > 0 && scene.HackingSeedDifficulty < daemonType.RequiredDangerLevel )
                    continue;
                DaemonTypeBag.AddItem( daemonType, daemonType.SeedWeight );
            }

            if ( !DaemonTypeBag.GetHasItems() )
            {
                ArcenDebugging.LogSingleLine( "Asked for HackingDaemonTagUse '" + TagUseName + "', the scenario '" + scene.Scenario.ID + 
                    "' includes that as the tag '" + tag.ID + "', which has " + tag.Types.Count + " types in it, yielding an empty bag.  Seed difficulty: " + 
                    scene.HackingSeedDifficulty, Verbosity.ShowAsError );
                return DaemonTypeBag;
            }

            return DaemonTypeBag;
        }
        #endregion

        #region GetValidDaemonTypeFromBag
        public static HackingDaemonType GetValidDaemonTypeFromBag( MersenneTwister Rand )
        {
            if ( !DaemonTypeBag.GetHasItems() )
                return null;

            int failures = 0;
            while ( DaemonTypeBag.GetHasItems() && failures++ < 1000 )
            {
                HackingDaemonType daemonType = DaemonTypeBag.PickRandom( Rand );
                int max = daemonType.MaxToSeed;

                foreach ( Daemon existing in scene.Daemons )
                {
                    if ( existing.DaemonType == daemonType )
                        max--;
                }
                if ( max > 0 ) //valid!
                    return daemonType;

                DaemonTypeBag.RemoveAnyItemsMatching( daemonType );
            }
            return null; //failed!
        }
        #endregion

        #region DaemonDestroyPlayerShard
        public static void DaemonDestroyPlayerShard( PlayerShard Shard, Daemon DestroyedBy )
        {
            if ( Shard == null || Shard.CurrentCell == null )
                return;

            if ( DestroyedBy != null && DestroyedBy.IsHidden )
                DestroyedBy.IsHidden = true;

            int staminaLost = Shard.CurrentCell.CurrentNumber;
            Shard.CurrentCell.SetCurrentNumber( 0 );

            Shard.DestroyEntity();

            ArcenDoubleCharacterBuffer buffer = scene.GetHackingHistoryBuffer();
            if ( DestroyedBy?.DaemonType != null )
                buffer.AddFormat2( "HackingLog_ShardDestroyedKnown", staminaLost, DestroyedBy.DaemonType.GetDisplayName() );
            else
                buffer.AddFormat1( "HackingLog_ShardDestroyedUnknown", staminaLost );
            scene.AddToHackingHistory( buffer );

            CityStatisticTable.AlterScore( "ConsciousnessShardsEatenByDaemons", 1 );
        }
        #endregion

        #region JumpDaemonToNearestFreeSpaceOrKillIt
        public static void JumpDaemonToNearestFreeSpaceOrKillIt( Daemon daemon, MersenneTwister Rand )
        {
            if ( daemon == null )
                return;

            h.hCell cell = daemon.CurrentCell;
            h.hCell result = FindRandomAdjacentCellWithoutEntity( cell, Rand );
            if ( result == null )
                result = FindRandomAdjacentCellsAndDiagonalWithoutEntity( cell, Rand );

            if ( result == null )
                daemon.DestroyEntity();
            else
                daemon.MoveTo( result );
        }
        #endregion

        #region JumpDaemonToRandomNearSpaceOrFarSpace
        public static void JumpDaemonToRandomNearSpaceOrFarSpace( Daemon daemon, MersenneTwister Rand )
        {
            if ( daemon == null )
                return;

            h.hCell cell = daemon.CurrentCell;
            h.hCell result = FindRandomAdjacentCellWithoutEntity( cell, Rand );
            if ( result == null )
                result = FindRandomAdjacentCellsAndDiagonalWithoutEntity( cell, Rand );
            if ( result == null )
                result = FindRandomAdjacentCellsAndDiagonal2XWithoutEntity( cell, Rand );
            if ( result == null )
                result = FindRandomAnyCellWithoutEntity( Rand );

            if ( result == null )
                return;
            daemon.MoveTo( result );
        }
        #endregion

        #region FindRandomAdjacentCellWithoutEntity
        public static h.hCell FindRandomAdjacentCellWithoutEntity( h.hCell cell, MersenneTwister Rand )
        {
            if ( cell == null )
                return null;
            foreach ( h.hCell adjacent in cell.AdjacentCells.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return adjacent;
            }
            return null;
        }
        #endregion

        #region FindRandomAdjacentCellsAndDiagonalWithoutEntity
        public static h.hCell FindRandomAdjacentCellsAndDiagonalWithoutEntity( h.hCell cell, MersenneTwister Rand )
        {
            if ( cell == null )
                return null;
            foreach ( h.hCell adjacent in cell.AdjacentCellsAndDiagonal.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return adjacent;
            }
            return null;
        }
        #endregion

        #region FindRandomAdjacentCellsAndDiagonal2XWithoutEntity
        public static h.hCell FindRandomAdjacentCellsAndDiagonal2XWithoutEntity( h.hCell cell, MersenneTwister Rand )
        {
            if ( cell == null )
                return null;
            foreach ( h.hCell adjacent in cell.AdjacentCellsAndDiagonal2X.GetRandomStartEnumerable( Rand ) )
            {
                if ( adjacent.CurrentEntity == null )
                    return adjacent;
            }
            return null;
        }
        #endregion

        #region FindRandomAnyCellWithoutEntity
        public static h.hCell FindRandomAnyCellWithoutEntity( MersenneTwister Rand )
        {
            foreach ( h.hCell cell in scene.AllHackingCells.GetRandomStartEnumerable( Rand ) )
            {
                if ( cell.CurrentEntity == null )
                    return cell;
            }
            return null;
        }
        #endregion
    }
}

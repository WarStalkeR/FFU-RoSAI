using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.ExternalVis.Hacking
{
    using h = Window_Hacking;

    public static class HackingScene
    {
        public static ISimMachineActor HackerUnit;
        public static ISimMapMobileActor TargetUnit;
        public static int TargetDifficultyForAlly;
        public static HackingScenarioType Scenario;
        public static bool HasPopulatedScene = false;

        public static HackingAction CurrentHack = null;
        public static HackingAction[] HacksByIndex = new HackingAction[40];
        public static h.hCell HoveredCell = null;

        public static h.hCell LastColorSetForHoveredCell = null;
        public static string SpecialColorForHoveredCell = string.Empty;

        public static int ArrayWidth = -1;
        public static int ArrayHeight = -1;

        public static bool IsDaemonTurnToMove = false;
        public static int DaemonMovesSoFar = 0;

        public static int HackingBlockagesDifficulty = 0;
        public static int HackingSeedDifficulty = 0;
        public static bool HasLoggedFailure = false;
        public static bool IsInfiltrationSysOpScenario = false;

        public static readonly List<PlayerShard> PlayerShards = List<PlayerShard>.Create_WillNeverBeGCed( 100, "HackingScene-PlayerShards" );
        public static readonly List<IHEntity> AnimatingItems = List<IHEntity>.Create_WillNeverBeGCed( 100, "HackingScene-AnimatingItems" );
        public static readonly List<Daemon> Daemons = List<Daemon>.Create_WillNeverBeGCed( 100, "HackingScene-Daemons" );
        private static readonly List<Daemon> workingDaemons = List<Daemon>.Create_WillNeverBeGCed( 100, "HackingScene-workingDaemons" );

        public static h.hCell[,] HackingCellArray;
        public static readonly List<h.hCell> AllHackingCells = List<h.hCell>.Create_WillNeverBeGCed( 500, "HackingScene-AllHackingCells" );

        public static Dictionary<h.hCell, bool> PriorCellsOnFire = Dictionary<h.hCell, bool>.Create_WillNeverBeGCed( 500, "HackingScene-PriorCellsOnFire" );
        public static Dictionary<h.hCell, bool> NextCellsOnFire = Dictionary<h.hCell, bool>.Create_WillNeverBeGCed( 500, "HackingScene-NextCellsOnFire" );

        public static Dictionary<h.hCell, int> PriorCellsThreatened = Dictionary<h.hCell, int>.Create_WillNeverBeGCed( 500, "HackingScene-PriorCellsThreatened" );
        public static Dictionary<h.hCell, int> NextCellsThreatened = Dictionary<h.hCell, int>.Create_WillNeverBeGCed( 500, "HackingScene-NextCellsThreatened" );

        public static readonly TargetingIndicator MainTargetingIndicator = new TargetingIndicator();
        public static readonly BlockedIndicator MainBlockedIndicator = new BlockedIndicator();

        public const int MAX_HACKING_HISTORY = 30;
        public static readonly Queue<string> HackingHistory = Queue<string>.Create_WillNeverBeGCed( MAX_HACKING_HISTORY, "HackingScene-HackingHistory" );

        private static int lastNumberOfSelectDataReloads = 0;
        private static bool lastHadAnyHostileDaemons = false;

        #region DoPerFrame
        public static void DoPerFrame( bool IsFullyReadyAndPopulated )
        {
            #region Animate Any Items
            if ( AnimatingItems.Count > 0 )
            {
                for ( int i = AnimatingItems.Count - 1; i >= 0; i-- )
                {
                    if ( AnimatingItems[i].DoAnyMovement() )
                        AnimatingItems.RemoveAt( i );
                }
            }
            #endregion

            if ( lastNumberOfSelectDataReloads != Engine_HotM.NumberOfSelectDataReloads )
            {
                lastNumberOfSelectDataReloads = Engine_HotM.NumberOfSelectDataReloads;
                foreach ( Daemon daemon in Daemons )
                    daemon.RefreshVisuals();
            }

            if ( IsFullyReadyAndPopulated )
            {
                if ( HoveredCell != null && !HoveredCell.GetIsHovered() )
                    HoveredCell = null;

                {
                    Dictionary<h.hCell, bool> onFire = PriorCellsOnFire;
                    PriorCellsOnFire = NextCellsOnFire;
                    NextCellsOnFire = onFire;
                    NextCellsOnFire.Clear();
                }
                {
                    Dictionary<h.hCell, int> threatened = PriorCellsThreatened;
                    PriorCellsThreatened = NextCellsThreatened;
                    NextCellsThreatened = threatened;
                    NextCellsThreatened.Clear();
                }

                if ( CurrentHack != null )
                    CurrentHack.Implementation.TryHandleHackingAction( CurrentHack, HackingActionLogic.ExtraDrawPerFrameWhileActive );

                foreach ( Daemon daemon in Daemons )
                {
                    if ( daemon.DaemonType != null )
                        daemon.DaemonType.Implementation.TryHandleDaemonLogic( daemon.DaemonType, daemon, null, HackingDaemonLogic.ExtraDrawPerFrameWhileDaemonLives );
                }

                #region OnFire
                if ( PriorCellsOnFire.Count > 0 )
                {
                    foreach ( KeyValuePair<h.hCell, bool> kv in PriorCellsOnFire )
                    {
                        if ( NextCellsOnFire.ContainsKey( kv.Key ) )
                            continue;
                        kv.Key.SetIsOnFire( false );
                    }
                }
                if ( NextCellsOnFire.Count > 0 )
                {
                    foreach ( KeyValuePair<h.hCell, bool> kv in NextCellsOnFire )
                        kv.Key.SetIsOnFire( true );
                }
                #endregion

                #region Threatened
                if ( PriorCellsThreatened.Count > 0 )
                {
                    foreach ( KeyValuePair<h.hCell, int> kv in PriorCellsThreatened )
                    {
                        if ( NextCellsThreatened.ContainsKey( kv.Key ) )
                            continue;
                        kv.Key.SetIsBeingThreatened( false );
                    }
                }
                if ( NextCellsThreatened.Count > 0 )
                {
                    foreach ( KeyValuePair<h.hCell, int> kv in NextCellsThreatened )
                        kv.Key.SetIsBeingThreatened( true );
                }
                #endregion

                MainTargetingIndicator.DoAnyMovement();

                if ( IsDaemonTurnToMove && AnimatingItems.Count == 0 )
                    DoNextDaemonMove( Engine_Universal.PermanentQualityRandom );

                if ( PlayerShards.Count == 0 && !HasLoggedFailure )
                {
                    HasLoggedFailure = true;
                    HackingHelper.MarkHackerDefective();
                    ArcenDoubleCharacterBuffer buffer = GetHackingHistoryBuffer();
                    buffer.AddLang( "HackingLog_AllShardsDestroyed", ColorTheme.RedOrange );
                    AddToHackingHistory( buffer );
                    buffer.AddLang( "HackingLog_SessionTerminated", ColorTheme.RedOrange2 );
                    AddToHackingHistory( buffer );
                    ParticleSoundRefs.Hacking_Fail.DuringGame_PlaySoundOnlyAtCamera();
                    if ( (TargetUnit as ISimNPCUnit)?.UnitType?.ProbeCommsScenario == Scenario )
                    {
                        CityStatisticTable.AlterScore( "CommsProbeFailures", 1 );
                    }
                    else
                    {
                        if ( IsInfiltrationSysOpScenario )
                            CityStatisticTable.AlterScore( "InfiltrationSysOpFailures", 1 );
                        else
                            CityStatisticTable.AlterScore( "HackingFailures", 1 );
                    }
                }

                bool hasAnyHostileDaemons = false;
                foreach ( Daemon daemon in Daemons )
                {
                    if ( daemon?.DaemonType?.IsHostileDaemon ?? false )
                    {
                        hasAnyHostileDaemons =true;
                        break;
                    }
                }

                if ( lastHadAnyHostileDaemons != hasAnyHostileDaemons )
                {
                    lastHadAnyHostileDaemons = hasAnyHostileDaemons;
                    if ( !hasAnyHostileDaemons )
                    {
                        foreach ( Daemon daemon in Daemons )
                            daemon?.DaemonType?.Implementation?.TryHandleDaemonLogic( daemon?.DaemonType, daemon, null, HackingDaemonLogic.OnAllHostileDaemonsGone );
                    }
                }
            }
            else
            {
                HoveredCell = null;
                MainTargetingIndicator.ClearExistingData();
                MainBlockedIndicator.ClearExistingData();
            }

            h.iIndicatorBorder.Instance.UpdateToMatchDrawDesires();
        }
        #endregion

        #region ClearExistingData
        public static void ClearExistingData()
        {
            for ( int i = PlayerShards.Count - 1; i >= 0; i-- )
                PlayerShards[i].DestroyEntity();
            PlayerShards.Clear();

            for ( int i = Daemons.Count - 1; i >= 0; i-- )
                Daemons[i].DestroyEntity();
            Daemons.Clear();

            foreach ( h.hCell cell in AllHackingCells )
                cell.ClearExistingData();

            IsDaemonTurnToMove = false;
            DaemonMovesSoFar = 0;

            HackingBlockagesDifficulty = 0;
            HackingSeedDifficulty = 0;
            HasLoggedFailure = false;

            HackingHistory.Clear();
        }
        #endregion

        #region PopulateScene
        public static void PopulateScene( MersenneTwister Rand )
        {
            HasPopulatedScene = true;
            ClearExistingData();

            AddToHackingHistory( (Scenario?.GetDisplayName() ?? "[null]") + Lang.Get( "AfterLineItemHeader" ) +
                (TargetUnit?.GetDisplayName() ?? "[null]") );

            Scenario.Implementation.TryHandleHackingScenarioLogic( Scenario, HackingScenarioLogic.DoInitialPopulation, Rand );
        }
        #endregion

        #region DoNextDaemonMove
        private static void DoNextDaemonMove( MersenneTwister Rand )
        {
            if ( !IsDaemonTurnToMove )
                return;
            IsDaemonTurnToMove = false;
            DaemonMovesSoFar++;

            CityStatisticTable.AlterScore( "HackingTotalMoves", 1 );

            workingDaemons.Clear();
            workingDaemons.AddRange( Daemons );

            foreach ( Daemon daemon in workingDaemons ) //this keeps newly-added daemons from being able to act instantly
            {
                if ( daemon.AdditionalMovesToSkip > 0 )
                {
                    daemon.AdditionalMovesToSkip--;
                    daemon.RefreshVisuals();
                }
                else
                    daemon.DaemonType.Implementation.TryHandleDaemonLogic( daemon.DaemonType, daemon, null, HackingDaemonLogic.Move );
            }
            workingDaemons.Clear();
        }
        #endregion

        #region GetHackingHistoryBuffer
        private static readonly ArcenDoubleCharacterBuffer hackingHistoryBuffer = new ArcenDoubleCharacterBuffer( "HackingScene-hackingHistoryBuffer" );
        public static ArcenDoubleCharacterBuffer GetHackingHistoryBuffer()
        {
            hackingHistoryBuffer.EnsureResetForNextUpdate();
            return hackingHistoryBuffer;
        }
        #endregion

        #region AddToHackingHistory
        public static void AddToHackingHistory( string RawText )
        {
            if ( HackingHistory.Count >= MAX_HACKING_HISTORY - 1 )
                HackingHistory.Dequeue();

            HackingHistory.Enqueue( RawText );
        }

        public static void AddToHackingHistory( ArcenDoubleCharacterBuffer Buffer )
        {
            string text = Buffer.GetStringAndResetForNextUpdate();
            if ( text.IsEmpty() )
                return;

            if ( HackingHistory.Count >= MAX_HACKING_HISTORY - 1 )
                HackingHistory.Dequeue();

            if ( DaemonMovesSoFar > 0 )
                text = "<size=70%>" + Lang.Format1( "Hacking_MovePrefix", DaemonMovesSoFar ) + "</size>  " + text;

            HackingHistory.Enqueue( text );
        }
        #endregion
    }
}

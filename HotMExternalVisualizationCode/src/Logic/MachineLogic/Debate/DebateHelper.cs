using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    using deb = Window_Debate;

    public static class DebateHelper
    {
        #region SetStartingDebateStats
        public static void SetStartingDebateStats( int Target, int DiscardCount, int Mistrust, int Defiance )
        {
            deb.StartingTarget = Target;
            deb.CurrentTarget = Target;
            deb.MovesSoFar = 0;
            deb.StartingMistrust = Mistrust;
            deb.CurrentMistrust = Mistrust;
            deb.StartingDefiance = Defiance;
            deb.CurrentDefiance = Defiance;

            deb.StartingDiscards = DiscardCount;
            deb.RemainingDiscards = DiscardCount;

            deb.RewardsWon = 0;

            deb.DiscardedActions.Clear();
            deb.UpgradesToApplyOnVictory.Clear();

            deb.Phase = DebatePhase.Ongoing;
        }
        #endregion

        #region ClearTilesForFreshStart
        public static void ClearTilesForFreshStart( bool IsUpperRowAvailable, bool IsLowerRowAvailable )
        {
            for ( int y = 0; y < deb.ROW_COUNT; y++ )
            {
                for ( int x = 0; x < deb.COLUMN_COUNT; x++ )
                {
                    deb.bTile tile = deb.Tiles[x, y];
                    tile.RewardType = null;
                    tile.RewardCategory = null;
                    tile.Action = null;
                    tile.ActionScore = 0;

                    if ( (y == 0 && !IsUpperRowAvailable) || (y == 3 && !IsLowerRowAvailable) )
                    {
                        tile.Status = DebateTileStatus.OuterRow;

                        if ( y == 0 )
                        {
                            switch ( x )
                            {
                                case 1:
                                    deb.RewardSpotA = tile;
                                    break;
                                case 4:
                                    deb.RewardSpotB = tile;
                                    break;
                                case 7:
                                    deb.RewardSpotC = tile;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if ( x < 3 )
                            tile.Status = DebateTileStatus.Available;
                        else
                            tile.Status = DebateTileStatus.Normal;
                    }
                }
            }
        }
        #endregion

        private static readonly List<NPCDebateReward> rewardTypes = List<NPCDebateReward>.Create_WillNeverBeGCed( 10, "DebateHelper-rewardTypes" );
        private static readonly List<NPCDebateActionCategory> rewardCategories = List<NPCDebateActionCategory>.Create_WillNeverBeGCed( 10, "DebateHelper-rewardCategories" );
        private static readonly List<deb.bTile> tilesRemainingToFill = List<deb.bTile>.Create_WillNeverBeGCed( 10, "DebateHelper-tilesRemainingToFill" );

        #region FillRewardTypes
        private static void FillRewardTypes()
        {
            rewardTypes.Clear();
            int maxDifficulty = SimCommon.DebateStartingChoice?.DebateMaxBonusDifficulty ?? 1;
            if ( maxDifficulty < 1 )
                maxDifficulty = 1;

            foreach ( NPCDebateReward reward in NPCDebateRewardTable.Instance.Rows )
            {
                if ( reward.IsHidden || reward.PatternArray == null || reward.DifficultyLevel > maxDifficulty )
                    continue;
                rewardTypes.Add( reward );
            }
        }
        #endregion

        #region FillRewardCategories
        private static void FillRewardCategories()
        {
            rewardCategories.Clear();

            NPCDebateScenarioType scenario = SimCommon.DebateScenarioType;
            if ( scenario == null )
                return;

            foreach ( NPCDebateActionCategory cat in NPCDebateActionCategoryTable.Instance.Rows )
            {
                bool found = false;
                foreach ( NPCDebateAction action in scenario.Actions )
                {
                    if ( action.Categories.ContainsKey( cat.ID ) )
                    {
                        found = true;
                        break;
                    }
                }

                if ( found )
                    rewardCategories.Add( cat );
            }
        }
        #endregion

        #region AssignRewards
        public static void AssignRewards( MersenneTwister Rand )
        {
            FillRewardTypes();
            FillRewardCategories();

            deb.RewardSpotA.RewardType = null;
            deb.RewardSpotA.RewardCategory = null;

            deb.RewardSpotB.RewardType = null;
            deb.RewardSpotB.RewardCategory = null;

            deb.RewardSpotC.RewardType = null;
            deb.RewardSpotC.RewardCategory = null;

            int remainingRewards = SimCommon.DebateStartingChoice?.DebateBonuses ?? 0;
            if ( remainingRewards <= 0 )
                return;

            if ( remainingRewards > rewardCategories.Count )
                remainingRewards = rewardCategories.Count;

            if ( remainingRewards <= 0 )
                return;
            if ( remainingRewards > 3 )
                remainingRewards = 3;

            tilesRemainingToFill.Clear();
            switch ( remainingRewards )
            {
                case 1:
                    tilesRemainingToFill.Add( deb.RewardSpotB );
                    break;
                case 2:
                    tilesRemainingToFill.Add( deb.RewardSpotA );
                    tilesRemainingToFill.Add( deb.RewardSpotC );
                    break;
                case 3:
                    tilesRemainingToFill.Add( deb.RewardSpotA );
                    tilesRemainingToFill.Add( deb.RewardSpotB );
                    tilesRemainingToFill.Add( deb.RewardSpotC );
                    break;
            }

            while ( tilesRemainingToFill.Count > 0 )
            {
                if ( rewardTypes.Count == 0 )
                    FillRewardTypes(); //these we can refill, to have duplicates, but that's not allowed with categories

                NPCDebateReward rewardType = rewardTypes.GetRandomAndRemove( Rand );
                NPCDebateActionCategory rewardCategory = rewardCategories.GetRandomAndRemove( Rand );

                deb.bTile tile = tilesRemainingToFill[0];
                tilesRemainingToFill.RemoveAt( 0 );

                tile.RewardType = rewardType;
                tile.RewardCategory = rewardCategory;

                remainingRewards--;
            }
        }
        #endregion

        #region DoAction
        public static void DoAction( int TargetProgress, int MistrustChange, int DefianceChange )
        {
            deb.CurrentTarget -= TargetProgress;
            if ( deb.CurrentTarget <= 0 )
                deb.CurrentTarget = 0;

            deb.CurrentMistrust += MistrustChange;
            if ( deb.CurrentMistrust <= 0 )
                deb.CurrentMistrust = 0;
            if ( deb.CurrentMistrust >= 100 )
                deb.CurrentMistrust = 100;

            deb.CurrentDefiance += DefianceChange;
            if ( deb.CurrentDefiance <= 0 )
                deb.CurrentDefiance = 0;
            if ( deb.CurrentDefiance >= 100 )
                deb.CurrentDefiance = 100;

            deb.MovesSoFar++;

            //advance them all by one
            for ( int i = 0; i < deb.UpcomingList.Length - 1; i++ )
                deb.UpcomingList[i].Action = deb.UpcomingList[i+1].Action;

            //assign a new one into the last spot
            deb.UpcomingList[deb.UpcomingList.Length - 1].Action = CalculateNextUpcomingAction( deb.UpcomingList[deb.UpcomingList.Length - 2].Action );

            //update available tiles
            foreach ( deb.bTile tile in deb.MainTiles )
            {
                if ( tile.Action != null )
                {
                    if ( (tile.East?.Status ?? DebateTileStatus.OuterRow) == DebateTileStatus.Normal && tile.East?.Action == null )
                        tile.East.Status = DebateTileStatus.Available;

                    if ( (tile.East?.East?.Status ?? DebateTileStatus.OuterRow) == DebateTileStatus.Normal && tile.East?.East?.Action == null )
                        tile.East.East.Status = DebateTileStatus.Available;

                    //this is a bit too easy
                    //if ( (tile.East?.East?.East?.Status ?? DebateTileStatus.OuterRow) == DebateTileStatus.Normal && tile.East?.East?.East?.Action == null )
                    //    tile.East.East.East.Status = DebateTileStatus.Available;
                }
            }

            //check for any bonus rewards that have been triggered by this
            if ( deb.RewardSpotA.RewardType != null && deb.RewardSpotA.RewardCategory != null )
                deb.RewardSpotA.RewardType.Implementation.TryHandleNPCDebateReward( deb.RewardSpotA.RewardType, deb.RewardSpotA.RewardCategory, NPCDebateRewardLogic.CheckForRealSuccess );
            if ( deb.RewardSpotB.RewardType != null && deb.RewardSpotB.RewardCategory != null )
                deb.RewardSpotB.RewardType.Implementation.TryHandleNPCDebateReward( deb.RewardSpotB.RewardType, deb.RewardSpotB.RewardCategory, NPCDebateRewardLogic.CheckForRealSuccess );
            if ( deb.RewardSpotC.RewardType != null && deb.RewardSpotC.RewardCategory != null )
                deb.RewardSpotC.RewardType.Implementation.TryHandleNPCDebateReward( deb.RewardSpotC.RewardType, deb.RewardSpotC.RewardCategory, NPCDebateRewardLogic.CheckForRealSuccess );

            if ( deb.CurrentTarget <= 0 )
                deb.DoOnWinDebate();
            else if ( deb.CurrentMistrust >= 100 )
                deb.DoOnLoseDebate( DebateLossReason.MistrustTooHigh);
            else if (deb.CurrentDefiance >= 100 )
                deb.DoOnLoseDebate( DebateLossReason.DefianceTooHigh );
            else
            {
                bool hasAnyOpenTiles = false;
                foreach ( deb.bTile tile in deb.MainTiles )
                {
                    if ( tile.Action == null )
                    {
                        hasAnyOpenTiles = true;
                        break;
                    }
                }
                if ( !hasAnyOpenTiles )
                    deb.DoOnLoseDebate( DebateLossReason.OutOfSpace );
            }
        }
        #endregion

        #region CalculateNextUpcomingAction
        public static NPCDebateAction CalculateNextUpcomingAction( NPCDebateAction priorAction )
        {
            NPCDebateAction action = SimCommon.DebateScenarioType.Actions.GetRandom( DebateScenariosBasic.Rand );

            int duplicateEliminationCheckCount = 12;
            while ( ( action == priorAction || deb.DiscardedActions.ContainsKey( action ) ) && duplicateEliminationCheckCount-- > 0 )
                action = SimCommon.DebateScenarioType.Actions.GetRandom( DebateScenariosBasic.Rand );

            int disallowedActionCheckCount = 40;
            while ( deb.DiscardedActions.ContainsKey( action ) && disallowedActionCheckCount-- > 0 )
                action = SimCommon.DebateScenarioType.Actions.GetRandom( DebateScenariosBasic.Rand );

            return action;
        }
        #endregion

        #region TryToTrashAction
        public static void TryToTrashAction( NPCDebateAction ActionToTrash, RectTransform RelevantRect )
        {
            if ( ActionToTrash == null || deb.DiscardedActions.ContainsKey( ActionToTrash ) || deb.RemainingDiscards <= 0 )
            {
                ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                return;
            }

            deb.RemainingDiscards--;
            deb.DiscardedActions[ActionToTrash] = true;

            //strip out any matches
            for ( int j = deb.UpcomingList.Length - 1; j >= 0; j-- )
            {
                if ( deb.UpcomingList[j].Action == ActionToTrash )
                {
                    deb.UpcomingList[j].Action = null;

                    //advance them all by one
                    for ( int i = j; i < deb.UpcomingList.Length - 1; i++ )
                        deb.UpcomingList[i].Action = deb.UpcomingList[i + 1].Action;
                    //and null the last one
                    deb.UpcomingList[deb.UpcomingList.Length-1].Action = null;
                }
            }

            //now fill in any holes
            for ( int i = 0; i < deb.UpcomingList.Length; i++ )
            {
                if ( deb.UpcomingList[i].Action == null )
                    deb.UpcomingList[i].Action = CalculateNextUpcomingAction( i == 0 ? null : deb.UpcomingList[i-1].Action );
            }

            ParticleSoundRefs.Debate_Discard.DuringGame_PlayAtUILocation( RelevantRect.CalculateScreenCenter() );
        }
        #endregion
    }
}

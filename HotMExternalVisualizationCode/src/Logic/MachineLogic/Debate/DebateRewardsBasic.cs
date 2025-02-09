using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    using deb = Window_Debate;

    public class DebateRewardsBasic : INPCDebateRewardImplementation
    {
        public void TryHandleNPCDebateReward( NPCDebateReward Reward, NPCDebateActionCategory RelatedCategory, NPCDebateRewardLogic Logic )
        {
            if ( Reward == null || RelatedCategory == null )
                return; //was error

            //all of the debate rewards right now use the same logic.
            //this is mainly here so that it can be swapped out or forked if later desired.

            switch ( Logic )
            {
                case NPCDebateRewardLogic.CheckForRealSuccess:
                    {
                        foreach ( deb.bTile tile in deb.MainTiles )
                        {
                            if ( DoesPatternMatchHere( tile, Reward, RelatedCategory, true ) )
                            {
                                deb.RewardsWon++;

                                //success!
                                if ( Reward.DoublesScore || Reward.TriplesScore )
                                {
                                    foreach ( deb.bTile match in matchingTilesOfCategory )
                                    {
                                        int toAdd = Reward.TriplesScore ? match.ActionScore + match.ActionScore : match.ActionScore;
                                        match.ActionScore += toAdd;
                                        deb.CurrentTarget -= toAdd;
                                    }
                                }

                                upgrades.Clear();
                                foreach ( deb.bTile match in matchingTilesOfCategory )
                                {
                                    if ( match.Action.TargetIncrease1 != null && !deb.UpgradesToApplyOnVictory.Contains( match.Action.TargetIncrease1 ) )
                                        upgrades.AddIfNotAlreadyIn( match.Action.TargetIncrease1 );
                                    if ( match.Action.TargetIncrease2 != null && !deb.UpgradesToApplyOnVictory.Contains( match.Action.TargetIncrease2 ) )
                                        upgrades.AddIfNotAlreadyIn( match.Action.TargetIncrease2 );
                                    if ( match.Action.TargetIncrease3 != null && !deb.UpgradesToApplyOnVictory.Contains( match.Action.TargetIncrease3 ) )
                                        upgrades.AddIfNotAlreadyIn( match.Action.TargetIncrease3 );
                                }

                                if ( Reward.LeastUsedCategorySkillWillImprove )
                                {
                                    int lowestNumber = 999;
                                    foreach ( UpgradeInt upgrade in upgrades )
                                    {
                                        if ( upgrade.DuringGameplay_CurrentInt < lowestNumber )
                                            lowestNumber = upgrade.DuringGameplay_CurrentInt;
                                    }

                                    for ( int i = upgrades.Count - 1; i >= 0; i--)
                                    {
                                        if ( upgrades[i].DuringGameplay_CurrentInt > lowestNumber )
                                            upgrades.RemoveAt(i);
                                    }

                                    UpgradeInt chosenUpgrade = upgrades.GetRandom( DebateScenariosBasic.Rand );
                                    if ( chosenUpgrade != null )
                                        deb.UpgradesToApplyOnVictory.Add( chosenUpgrade );
                                    upgrades.Clear();
                                }
                                else //if ( Reward.MostUsedCategorySkillWillImprove )
                                {
                                    int highestNumber = 0;
                                    foreach ( UpgradeInt upgrade in upgrades )
                                    {
                                        if ( upgrade.DuringGameplay_CurrentInt > highestNumber )
                                            highestNumber = upgrade.DuringGameplay_CurrentInt;
                                    }

                                    for ( int i = upgrades.Count - 1; i >= 0; i-- )
                                    {
                                        if ( upgrades[i].DuringGameplay_CurrentInt < highestNumber )
                                            upgrades.RemoveAt( i );
                                    }

                                    UpgradeInt chosenUpgrade = upgrades.GetRandom( DebateScenariosBasic.Rand );
                                    if ( chosenUpgrade != null )
                                        deb.UpgradesToApplyOnVictory.Add( chosenUpgrade );
                                    upgrades.Clear();
                                }

                                matchingTilesOfCategory.Clear();

                                //play the sound and such
                                Reward.OnComplete.DuringGame_Play();

                                if ( deb.RewardSpotA.RewardType == Reward && deb.RewardSpotA.RewardCategory == RelatedCategory )
                                {
                                    deb.RewardSpotA.RewardType = null;
                                    deb.RewardSpotA.RewardCategory = null;
                                }
                                if ( deb.RewardSpotB.RewardType == Reward && deb.RewardSpotB.RewardCategory == RelatedCategory )
                                {
                                    deb.RewardSpotB.RewardType = null;
                                    deb.RewardSpotB.RewardCategory = null;
                                }
                                if ( deb.RewardSpotC.RewardType == Reward && deb.RewardSpotC.RewardCategory == RelatedCategory )
                                {
                                    deb.RewardSpotC.RewardType = null;
                                    deb.RewardSpotC.RewardCategory = null;
                                }

                                return; //done with the success!
                            }
                        }
                    }
                    break;
                case NPCDebateRewardLogic.CheckForTheoreticalSuccess:
                    //todo
                    break;
            }
        }

        private static List<deb.bTile> matchingTilesOfCategory = List<deb.bTile>.Create_WillNeverBeGCed( 10, "DebateRewardsBasic-matchingTilesOfCategory" );
        private static List<UpgradeInt> upgrades = List<UpgradeInt>.Create_WillNeverBeGCed( 10, "DebateRewardsBasic-upgrades" );

        #region DoesPatternMatchHere
        private bool DoesPatternMatchHere( deb.bTile StartingTile, NPCDebateReward Reward, NPCDebateActionCategory RelatedCategory, bool RealOnly )
        {
            matchingTilesOfCategory.Clear();
            for ( int y = 0; y < Reward.PatternHeight; y++ )
            {
                for ( int x = 0; x < Reward.PatternWidth; x++ )
                {
                    int finalY = StartingTile.Row + y;
                    int finalX = StartingTile.Column + x;

                    if ( finalY > 2 || finalX >= deb.COLUMN_COUNT )
                        return false; //out of range, so cannot possibly match here

                    deb.bTile tile = deb.Tiles[finalX, finalY];
                    if ( !DoesTileMatch( tile, Reward.PatternArray[x, y], RelatedCategory, RealOnly ) )
                    {
                        matchingTilesOfCategory.Clear();
                        return false; //part of the pattern does not match, so none of it does
                    }
                }
            }

            return true; //we had no match failures, so it must be a success!
        }
        #endregion

        #region DoesTileMatch
        private bool DoesTileMatch( deb.bTile tile, RewardPatternPart PatternPart, NPCDebateActionCategory RelatedCategory, bool RealOnly )
        {
            switch (PatternPart)
            {
                case RewardPatternPart.Ignore:
                    return true; //anything matches, so long as it is not out of bounds
                case RewardPatternPart.MustMatch:
                    if ( tile.Action == null )
                    {
                        if ( !RealOnly )
                        {
                            if ( deb.HoveredTile != null && deb.HoveredTile == tile )
                            {
                                NPCDebateAction nextAction = deb.UpcomingList[0].Action;

                                if ( !nextAction.Categories.ContainsKey( RelatedCategory.ID ) )
                                    return false;
                                matchingTilesOfCategory.Add( tile );
                                return true; //success!
                            }
                        }
                        return false;
                    }
                    if ( !tile.Action.Categories.ContainsKey( RelatedCategory.ID ) )
                        return false;
                    matchingTilesOfCategory.Add( tile );
                    return true;
                case RewardPatternPart.MustNotMatch:
                    if ( tile.Action == null )
                    {
                        if ( !RealOnly )
                        {
                            if ( deb.HoveredTile != null && deb.HoveredTile == tile )
                            {
                                NPCDebateAction nextAction = deb.UpcomingList[0].Action;

                                if ( nextAction.Categories.ContainsKey( RelatedCategory.ID ) )
                                    return false; //fail, it matches!
                            }
                        }
                        return true; //this is fine!
                    }
                    if ( tile.Action.Categories.ContainsKey( RelatedCategory.ID ) )
                        return false;
                    return true;
            }
            return false;
        }
        #endregion
    }
}

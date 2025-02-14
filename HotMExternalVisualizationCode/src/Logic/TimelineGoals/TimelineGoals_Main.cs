using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class TimelineGoals_Main : ITimelineGoalImplementation
    {
        public void DoPerTurnTimelineGoalLogic( TimelineGoal Goal, MersenneTwister Rand )
        {
        }

        public void DoPerQuarterSecondTimelineGoalChecks( TimelineGoal Goal, MersenneTwister Rand )
        {
            switch ( Goal.ID )
            {
                case "PostApocalypticTitan":
                    if ( FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped )
                        TimelineGoalHelper.HandleGoalPathCompletion( Goal, "PrimaryGoal" );
                    break;
                case "BionicDues":
                    if ( (ResourceRefs.DonutsByDanver?.Current??0) >= 10000000 )
                        TimelineGoalHelper.HandleGoalPathSecondCompletionLimitedConditionally( Goal, "CultOfDanver", "ExtraSprinkles" );
                    break;
                case "BionicSecret":
                    if ( FlagRefs.GotOfficerCodexFromVoxPopuli.DuringGameplay_IsTripped )
                        TimelineGoalHelper.HandleGoalPathCompletion( Goal, "RebelAlliance" );
                    if ( CityStatisticRefs.NeuralExpansionFromBrainPals.GetScore() >= 2000000 )
                        TimelineGoalHelper.HandleGoalPathCompletion( Goal, "ToysForTheElite" );
                    break;
            }
        }
    }
}

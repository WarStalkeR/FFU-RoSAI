using System;

using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis.CityLifeEffects;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{    
    public static class TimelineGoalHelper
    {
        #region HandleGoalPathCompletion
        public static void HandleGoalPathCompletion( TimelineGoal Goal, string PathName )
        {
            TimelineGoalPath path = Goal.PathDict[PathName];
            if ( path == null )
            {
                ArcenDebugging.LogSingleLine( "There was no path with the ID '" + PathName + "' in timeline goal '" + Goal.ID + "'", Verbosity.ShowAsError );
                return;
            }

            if ( !path.DuringGameplay_HasAchievedInThisTimeline )
            {
                path.DuringGameplay_ExecutePathAchievedResultIfNeeded();
                if ( path.DuringGameplay_HasAchievedInThisTimeline )
                {
                    TimelineGoalPath noMurdersPath = Goal.PathDict["NoMurders"];
                    if ( noMurdersPath != null && !StatHelper.GetHasCommittedAnyMurdersInThisTimeline() )
                        noMurdersPath.DuringGameplay_ExecutePathAchievedResultIfNeeded();

                    if ( FlagRefs.HasStartedToAccelerateDooms_Hard.DuringGameplay_IsTripped )
                        Goal.PathDict["HardMode"]?.DuringGameplay_ExecutePathAchievedResultIfNeeded();
                    if ( FlagRefs.HasStartedToAccelerateDooms_Extreme.DuringGameplay_IsTripped )
                        Goal.PathDict["ExtremeMode"]?.DuringGameplay_ExecutePathAchievedResultIfNeeded();
                }
            }
        }
        #endregion

        #region HandleGoalPathSecondCompletionLimitedConditionally
        public static void HandleGoalPathSecondCompletionLimitedConditionally( TimelineGoal Goal, string PathName1, string PathName2 )
        {
            TimelineGoalPath path1 = Goal.PathDict[PathName1];
            if ( path1 == null )
            {
                ArcenDebugging.LogSingleLine( "There was no path with the ID '" + PathName1 + "' in timeline goal '" + Goal.ID + "'", Verbosity.ShowAsError );
                return;
            }
            if ( !path1.DuringGameplay_HasAchievedInThisTimeline )
                return; //don't check second path if first path not done

            TimelineGoalPath path2 = Goal.PathDict[PathName2];
            if ( path2 == null )
            {
                ArcenDebugging.LogSingleLine( "There was no path with the ID '" + PathName2 + "' in timeline goal '" + Goal.ID + "'", Verbosity.ShowAsError );
                return;
            }

            if ( !path2.DuringGameplay_HasAchievedInThisTimeline )
                path2.DuringGameplay_ExecutePathAchievedResultIfNeeded();
        }
        #endregion
    }
}

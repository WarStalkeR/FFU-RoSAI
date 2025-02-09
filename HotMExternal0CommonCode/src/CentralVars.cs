using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    public static class CentralVars
    {
        public static bool GetShouldNotRunGameStyleLogic()
        {
            if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
                return true; //we're shutting things off, so yeah don't run game-style logic
            return false;
        }

        #region HandleCrossovers
        public static void HandleCrossovers()
        {
            CityTimeline currentTimeline = SimCommon.CurrentTimeline;
            if ( currentTimeline == null )
                return;

            foreach ( KeyValuePair<int, CityTimeline> kv in SimMetagame.AllTimelines )
            {
                if ( kv.Value == currentTimeline || kv.Value.Crossovers.Count == 0 || 
                    kv.Value.ChildOfEndOfTimeObjectWithID != currentTimeline.ChildOfEndOfTimeObjectWithID ) //if a different rock
                    continue;

                foreach ( KeyValuePair<CityTimelineCrossover, bool> kv2 in kv.Value.Crossovers )
                {
                    if ( kv2.Value )
                        kv2.Key.ApplyTheCrossoverLogicIfNeeded();
                }
            }
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public static class StatHelper
    {
        #region GetHasCommittedAnyMurdersInThisTimeline
        public static bool GetHasCommittedAnyMurdersInThisTimeline()
        {
            if ( CityStatisticRefs.Murders.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.AttemptedMurders.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.MurdersByRaptor.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.MurdersByNuke.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.MurdersByBuildingCollapse.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.MurdersByConventionalExplosion.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.MurdersByWorkerAndroids.GetScore() > 0 )
                return true;
            return false;
        }
        #endregion

        #region GetHasHadAnyDeathsFromExposureInThisTimeline
        public static bool GetHasHadAnyDeathsFromExposureInThisTimeline()
        {
            if ( CityStatisticRefs.AbandonedHumanDeathsFromExposure.GetScore() > 0 )
                return true;
            if ( CityStatisticRefs.DesperateHomelessDeathsFromExposure.GetScore() > 0 )
                return true;
            return false;
        }
        #endregion

        #region GetTotalsMurdersAcrossTimelines
        public static Int64 GetTotalsMurdersAcrossTimelines()
        {
            return MetaStatisticRefs.Murders.GetScore() +
                MetaStatisticRefs.AttemptedMurders.GetScore() +
                MetaStatisticRefs.MurdersByRaptor.GetScore() +
                MetaStatisticRefs.MurdersByNuke.GetScore() +
                MetaStatisticRefs.MurdersByBuildingCollapse.GetScore() +
                MetaStatisticRefs.MurdersByConventionalExplosion.GetScore() +
                MetaStatisticRefs.MurdersByWorkerAndroids.GetScore();
        }
        #endregion
    }
}

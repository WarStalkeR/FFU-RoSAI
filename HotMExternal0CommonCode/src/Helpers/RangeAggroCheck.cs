using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public struct RangeAggroCheck
    {
        public ISimMapActor best;
        public float bestRange;
        public int bestAggro;

        public static RangeAggroCheck InitializeNew()
        {
            RangeAggroCheck check;
            check.best = null;
            check.bestRange = 100000000;
            check.bestAggro = 0;
            return check;
        }

        #region HandleRangeCheckWhileEvaluatingAggro
        public void HandleRangeCheckWhileEvaluatingAggro( ISimMapActor prospect, float range, int aggro )
        {
            if ( best == null )
            {
                best = prospect;
                bestRange = range;
                bestAggro = aggro;
            }
            else
            {
                if ( bestAggro > aggro + aggro )
                    return; //if the best aggro is more than twice that of the new aggro, then ignore the new option

                if ( bestAggro + bestAggro < aggro )
                {
                    //if the new aggro is more than twice that of the prior best, then ignore ranges and just assign this
                    best = prospect;
                    bestRange = range;
                    bestAggro = aggro;
                }
                else
                {
                    //if the aggro is kind of similar, then we do a range check.  If the new thing is closer, use the new thing.
                    if ( bestRange > range )
                    {
                        best = prospect;
                        bestRange = range;
                        bestAggro = aggro;
                    }
                }
            }
        }
        #endregion
    }
}

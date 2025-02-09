using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class NPCAction_NoCode : INPCUnitActionImplementation
    {
        public bool TryHandleActionLogicForNPCUnit( ISimNPCUnit Unit, NPCActionConsideration Consideration, NPCUnitAction Action, MersenneTwister Rand, NPCUnitActionLogic Logic, NPCTimingData TimingDataOrNull )
        {
            if ( Action == null || Unit == null )
                return false;

            //intentionally nothing to do here
            return true; //this is success always
        }
    }
}

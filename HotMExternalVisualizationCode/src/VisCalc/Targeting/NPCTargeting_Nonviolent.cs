using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class NPCTargeting_Nonviolent : INPCUnitTargetingLogicImplementation
    {
        public ISimMapActor ChooseATargetInRangeThatCanBeShotRightNow( NPCUnitTargetingLogic Logic, ISimNPCUnit NPCUnit, List<ISimMapActor> CurrentActorSet, MersenneTwister Rand, bool ShouldDoTargetingDump )
        {
            return null; //nothing found, ever
        }
    }
}

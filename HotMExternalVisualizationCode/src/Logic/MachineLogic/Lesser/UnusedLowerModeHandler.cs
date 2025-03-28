using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class UnusedLowerModeHandler : ILowerModeImplementation
    {
        public void FlagAllRelatedResources( LowerModeData Mode )
        {
        }

        public void HandleCancelButton()
        {
            Engine_HotM.CurrentLowerMode = null;
        }

        public void HandlePassedInput( int Int1, InputActionTypeData InputActionType )
        {
        }

        public void TriggerSlotNumber( int SlotNumber )
        {
        }
    }
}

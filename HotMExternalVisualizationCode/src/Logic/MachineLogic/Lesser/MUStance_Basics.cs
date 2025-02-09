using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class MUStance_Basics : IMachineUnitStanceImplementation
    {
        public void HandleLogicForUnitInStance( ISimMachineUnit Unit, MachineUnitStance Stance, MersenneTwister RandForThisTurnOrNull,
            int APRemainingAtEndOfTurn, ArcenDoubleCharacterBuffer BufferOrNull, MachineUnitStanceLogic Logic )
        {
            if ( Unit == null || Stance == null )
                return;
            switch ( Stance.ID )
            {
                case "AndroidActive":
                case "Combat":
                case "Defiant":
                case "MechActiveStance":
                    {
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "MUStance_Basics: Called HandleLogicForUnitInStance for '" + Stance.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

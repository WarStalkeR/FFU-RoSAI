using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class MVStance_Basics : IMachineVehicleStanceImplementation
    {
        public void HandleLogicForVehicleInStance( ISimMachineVehicle Vehicle, MachineVehicleStance Stance, MersenneTwister RandForThisTurnOrNull,
            int APRemainingAtEndOfTurn, ArcenDoubleCharacterBuffer BufferOrNull, VehicleStanceLogic Logic )
        {
            if ( Vehicle == null || Stance == null )
                return;
            switch ( Stance.ID )
            {
                case "VehicleActive":
                    break; //nothing to do here!
                default:
                    ArcenDebugging.LogSingleLine( "MVStance_Basics: Called HandleLogicForVehicleInStance for '" + Stance.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

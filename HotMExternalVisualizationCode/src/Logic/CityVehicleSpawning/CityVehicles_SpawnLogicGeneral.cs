using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public class CityVehicles_SpawnLogicGeneral : ICityVehicleTypeLogicImplementation
    {
        public void DoPerSecondLogic_BackgroundThread( CityVehicleType VehicleType, MersenneTwister Rand )
        {
            if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                return; //don't create these in the city map

            CityVehiclesSpawner.SpawnFromRules( VehicleType, Rand );
        }

        public void AddExtraTooltipInformation( CityVehicleType VehicleType, ISimCityVehicle Vehicle, ArcenDoubleCharacterBuffer TooltipBuffer )
        {

        }
    }
}

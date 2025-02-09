using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Helper methods for coordinating the serialization and deserialization of the SimWorld
    /// No actual data should be stored here
    /// </summary>
    internal static class SimSerialization
    {
        public static void SerializeData( ArcenFileSerializer Serializer )
        {
            //start main set: this is done in this order as much as possible
            MapPOI.SerializeAllPOIs( Serializer );
            World_People.Serialize( Serializer );
            World_Buildings.Serialize( Serializer );
            World_Misc.Serialize( Serializer );
            World_CityVehicles.Serialize( Serializer );
            World_Forces.Serialize( Serializer );
            //World_CityNetwork.Serialize( Serializer );
            //end main set
        }

        public static void DeserializeData( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {
            //start main set: these are done in whatever order is required for the deserialization logic
            MapPOI.DeserializeAllPOIs( Data, RandToUse );
            World_People.Deserialize( Data, RandToUse );
            World_Buildings.Deserialize( Data, RandToUse ); //must be done before forces
            World_Forces.Deserialize( Data, RandToUse ); //must be done after buildings, and after POIs
            World_Misc.Deserialize( Data, RandToUse );
            World_CityVehicles.Deserialize( Data, RandToUse );
            //World_CityNetwork.Deserialize( Data, RandToUse );
            //end main set

            World_Forces.FinishDeserializeLate(); //must be after buildings at the least

            SimPerFullSecond.HandleRecalculationsFromAfterDeserialization();
            SimPerQuarterSecond.HandleRecalculationsFromAfterDeserialization();
            //SimCommon.HandleRecalculationsFromAfterDeserialization(); this would be too early!  This must happen elsewhere
        }
    }
}

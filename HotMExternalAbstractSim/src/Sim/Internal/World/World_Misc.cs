using System;



using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Various simworld data that isn't yet split out into its own file
    /// </summary>
    internal class World_Misc : ISimWorld_Misc
    {
        public static World_Misc QueryInstance = new World_Misc();

        //
        //Serialized data
        //-----------------------------------------------------

        //
        //NonSerialized data
        //-----------------------------------------------------        
        public static readonly ConcurrentQueue<IUIRequest> UIRequests = ConcurrentQueue<IUIRequest>.Create_WillNeverBeGCed( "SimWorld-UIRequests" );

        //
        //All important variables should be above this point, so we can be sure to clear them.
        //-----------------------------------------------------------------

        public static void OnGameClear()
        {
            UIRequests.Clear();
        }

        #region Serialization
        public static void Serialize( ArcenFileSerializer Serializer )
        {
        }

        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {

        }
        #endregion

        #region ISimWorld_Misc
        public ConcurrentQueue<IUIRequest> GetUIRequests()
        {
            return UIRequests;
        }
        #endregion
    }
}

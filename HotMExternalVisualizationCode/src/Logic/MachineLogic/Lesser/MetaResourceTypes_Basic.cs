using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class MetaResourceTypes_Basic : IMetaResourceTypeImplementation
    {
        public string CalculateColorHex( MetaResourceType Resource )
        {
            return Resource.IconColorHex;
        }

        public void DoPerGeneration( MetaResourceType Resource, MersenneTwister RandForThisTurn )
        {
            
        }
    }
}

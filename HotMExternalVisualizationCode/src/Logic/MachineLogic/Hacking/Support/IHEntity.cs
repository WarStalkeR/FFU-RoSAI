using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.ExternalVis.Hacking
{
    public interface IHEntity
    {
        bool DoAnyMovement();
        void DestroyEntity();
    }
}

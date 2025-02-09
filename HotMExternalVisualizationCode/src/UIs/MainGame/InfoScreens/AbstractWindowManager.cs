using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class AbstractWindowManager
    {
        public static readonly List<ToggleableWindowController> AbstractInfoWindows = List<ToggleableWindowController>.Create_WillNeverBeGCed( 20, "AbstractWindowManager-AbstractInfoWindows" );
    }
}

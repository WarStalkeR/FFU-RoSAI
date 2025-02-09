using Arcen.Universal;
using Arcen.HotM.Core;
using System;

using Arcen.HotM.External;

namespace Arcen.HotM.External
{
    internal class InitialSetupForDLL : IArcenExternalDllInitialLoadCall
    {
        public void RunOnFirstTimeExternalAssemblyLoaded()
        {
        }
    }
}
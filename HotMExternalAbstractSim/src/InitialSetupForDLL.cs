using Arcen.Universal;
using Arcen.HotM.Core;
using System;

using Arcen.HotM.External;

namespace Arcen.HotM.External
{
    public class InitialSetupForDLL : IArcenExternalDllInitialLoadCall
    {
        public void RunOnFirstTimeExternalAssemblyLoaded()
        {
        }
    }
}
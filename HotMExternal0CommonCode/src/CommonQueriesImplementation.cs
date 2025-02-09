using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    /// <summary>
    /// This is how the main game and/or the UI ask questions of the common dll
    /// </summary>
    internal class CommonQueriesImplementation : CommonQueries
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            
        }

        public CommonQueriesImplementation()
        {
            CommonQueries.Instance = this;
        }

        public override void InitializePoolsIfNeeded( ref long poolCount, ref long poolItemCount )
        {
        }
    }
}

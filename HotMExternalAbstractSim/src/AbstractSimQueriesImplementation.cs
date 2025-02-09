using Arcen.HotM.Core;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.External
{
    /// <summary>
    /// This is how the main game and/or the UI ask questions of the sim
    /// </summary>
    public class AbstractSimQueriesImplementation : AbstractSimQueries
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }

        public override void InitializePoolsIfNeeded( ref long poolCount, ref long poolItemCount )
        {
            SimBuilding.InitializePoolIfNeeded( ref poolCount, ref poolItemCount );
            CityVehicle.InitializePoolIfNeeded( ref poolCount, ref poolItemCount );
        }

        public AbstractSimQueriesImplementation()
        {
            AbstractSimQueries.Instance = this;
        }

        public override ISimWorld_People Get_SimWorld_People()
        {
            return World_People.QueryInstance;
        }

        public override ISimWorld_Buildings Get_SimWorld_Buildings()
        {
            return World_Buildings.QueryInstance;
        }

        public override ISimWorld_Misc Get_SimWorld_Misc()
        {
            return World_Misc.QueryInstance;
        }

        public override ISimWorld_CityVehicles Get_SimWorld_CityVehicles()
        {
            return World_CityVehicles.QueryInstance;
        }

        public override ISimWorld_Forces Get_SimWorld_Forces()
        {
            return World_Forces.QueryInstance;
		}

        //public override ISimWorld_CityNetwork Get_SimWorld_CityNetwork()
        //{
	       // return World_CityNetwork.QueryInstance;
        //}
    }
}

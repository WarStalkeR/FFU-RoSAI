using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class CityConflicts_Basic : ICityConflictImplementation
    {
        public void HandleConflictLogic( CityConflict Conflict, CityConflictLogic Logic, EventChoice ChoiceOrNull, MersenneTwister Rand )
        {
            if ( Conflict == null )
                return;

            //for now, nothing to do
            //switch ( Conflict.ID )
            //{
            //    case "Gnath":
            //        #region Gnath / TriswarmFragment
            //        {
            //            switch ( Logic )
            //            {
            //                case CityConflictLogic.HandlePerTurn:
            //                    break;
            //                //case CityConflictLogic.HandleEventChoice:
            //                //    break;
            //            }
            //        }
            //        break;
            //    #endregion
            //    default:
            //        ArcenDebugging.LogSingleLine( "CityConflicts_Basic: Called HandleConflictLogic for '" + Conflict.ID + "', which does not support it!", Verbosity.ShowAsError );
            //        break;
            //}
        }
    }
}

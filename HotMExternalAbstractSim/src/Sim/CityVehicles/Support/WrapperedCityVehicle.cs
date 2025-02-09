 using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.Universal.Deserialization;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Use this when we want to have a reference that safely expires
    /// </summary>
    internal struct WrapperedCityVehicle
    {
        private CityVehicle RawRef;
        private Int64 CityVehicleID;

        #region Create
        internal static WrapperedCityVehicle Create( CityVehicle Orig )
        {
            WrapperedCityVehicle obj;
            obj.RawRef = Orig;
            obj.CityVehicleID = Orig?.CityVehicleID ?? -1;
            return obj;
        }
        #endregion

        #region GetRefOrNull
        public CityVehicle GetRefOrNull()
        {
            if ( this.RawRef == null )
                return null;
            if ( this.RawRef?.CityVehicleID != this.CityVehicleID )
                return null;
            return this.RawRef;
        }
        #endregion
    }
}

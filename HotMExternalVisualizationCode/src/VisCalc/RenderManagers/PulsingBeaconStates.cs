// Ignore Spelling: IDB dt

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;
using System.Net;

namespace Arcen.HotM.ExternalVis
{
    public static class PulsingBeaconStates
    {
        public static float IntermittentDouble = 0f;
        private static float IDB = 0f;
        private static PulsingPhase IDBPhase = PulsingPhase.RestingBeforeUp;

        public static void UpdatePerFrame()
        {
            float deltaTime = ArcenTime.UnpausedDeltaTime;
            if ( deltaTime < 0 )
                return;

            #region IntermittentDouble
            {
                float dt2 = deltaTime + deltaTime + deltaTime;

                switch ( IDBPhase )
                {
                    case PulsingPhase.RestingBeforeUp:
                        IntermittentDouble = 0;
                        IDB += deltaTime;
                        if ( IDB > 1.6f )
                        {
                            IDB = 0;
                            IDBPhase = PulsingPhase.Up;
                        }
                        break;
                    case PulsingPhase.Up:
                        if ( IDB < 0.3f )
                            IDB += dt2 * 0.8f;
                        else if ( IDB < 0.6f )
                            IDB += dt2;
                        else
                            IDB += dt2 * 3;

                        if ( IDB >= 1f )
                        {
                            IDB = 1f;
                            IDBPhase = PulsingPhase.Down;
                        }
                        IntermittentDouble = IDB;
                        break;
                    case PulsingPhase.Down:
                        if ( IDB < 0.3f )
                            IDB -= dt2;
                        else
                            IDB -= dt2 * 4;

                        if ( IDB <= 0f )
                        {
                            IDB = 0f;
                            IDBPhase = PulsingPhase.RestingBeforeUp;
                        }
                        IntermittentDouble = IDB;
                        break;
                }
            }
            #endregion
        }

        private enum PulsingPhase
        {
            RestingBeforeUp,
            Up,
            Down
        }
    }

    public struct PulsingBeaconModelData
    {
        public float ExtraY;
        public float YScaleDivisor;
        public float XZScaleMult;

        #region CreateFromSinTimeClamped
        public static PulsingBeaconModelData CreateFromSinTimeClamped( float MaxExtraYHeight, float MaxYShrinkage, float Speed )
        {
            float progress = Mathf.Sin( ArcenTime.UnpausedTimeSinceStartF * Speed );
            if ( progress < 0 ) //clamp all the negative stuff to just be "normal unchanged" values, then the positives are the places where it shrinks and such
                progress = 0;

            float inverseProgress = 1f - progress;

            float inverseProgressSquared = inverseProgress * inverseProgress;

            PulsingBeaconModelData modelData;
            modelData.ExtraY = MathA.Max( MaxExtraYHeight - (inverseProgressSquared * MaxExtraYHeight), 0f );

            float scaleMultiplier = MathA.Min( 0.01f + (inverseProgressSquared * 0.99f), 1f );
            modelData.YScaleDivisor = Mathf.Max( MaxYShrinkage, scaleMultiplier );
            modelData.XZScaleMult = scaleMultiplier;

            return modelData;
        }
        #endregion

        #region CreateFromIntermittentDouble
        public static PulsingBeaconModelData CreateFromIntermittentDouble( float MaxExtraYHeight, float MaxYShrinkage, float MaxXZShrinkage )
        {
            float progress = PulsingBeaconStates.IntermittentDouble;
            if ( progress < 0 )
                progress = 0;

            float inverseProgress = 1f - progress;

            float inverseProgressSquared = inverseProgress * inverseProgress;

            PulsingBeaconModelData modelData;
            modelData.ExtraY = MathA.Max( MaxExtraYHeight - (inverseProgressSquared * MaxExtraYHeight), 0f );

            float scaleMultiplier = MathA.Min( 0.01f + (inverseProgressSquared * 0.99f), 1f );
            modelData.YScaleDivisor = Mathf.Max( MaxYShrinkage, scaleMultiplier );
            modelData.XZScaleMult = Mathf.Max( MaxXZShrinkage, scaleMultiplier );

            return modelData;
        }
        #endregion
    }
}

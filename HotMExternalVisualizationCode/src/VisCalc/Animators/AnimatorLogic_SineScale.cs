using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class AnimatorLogic_SineScale : IVisAnimatorLogicImplementation
    {
        public void DoPerFrameLogic( VisAnimatorLogic Logic, VisAnimatedObject Object, float deltaTime )
        {
            float sinT = Mathf.Sin( Object.Time * Logic.Float1 );

            Vector3 mostScaled = Object.InitialScale.ComponentWiseMult( Logic.Vector1 );

            Vector3 diff = mostScaled - Object.InitialScale;

            Vector3 mid = Object.InitialScale + ( diff * 0.5f);

            Vector3 final = mid + (diff * sinT);

            Object.CurrentScale = final;

            //if ( Object.Debug )
            //{
            //    ArcenDebugging.LogSingleLine( "n" + Object.ObjectNumber + "n sinT:" + sinT + " f1: " + Logic.Float1 + " time: " + Object.Time +
            //        " v1: " + Logic.Vector1 + " center: " + center + " final: " + final, Verbosity.DoNotShow );
            //}
        }
    }
}

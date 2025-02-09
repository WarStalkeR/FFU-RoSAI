using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class AnimatorLogic_SineMove : IVisAnimatorLogicImplementation
    {
        public void DoPerFrameLogic( VisAnimatorLogic Logic, VisAnimatedObject Object, float deltaTime )
        {
            float sinT = Mathf.Sin( Object.Time * Logic.Float1 );

            Vector3 diff = Vector3.zero;

            if ( Logic.Vector1.x != 0 || Logic.Vector1.y != 0 || Logic.Vector1.z != 0 )
            {
                Vector3 center = Object.InitialOffsetCalculated + Logic.Vector1;
                Vector3 final = center + (Logic.Vector1 * sinT);

                diff += ( final - Object.InitialOffsetCalculated );
            }

            if ( Logic.Float2 != 0 )
            {
                Vector3 vectorAlongForward = (Object.Forward * Logic.Float2);
                Vector3 center = Object.InitialOffsetCalculated + vectorAlongForward;
                Vector3 final = center + (vectorAlongForward * sinT);

                diff += ( final - Object.InitialOffsetCalculated );
            }

            Object.CurrentOffset = Object.InitialOffsetCalculated + diff;

            //if ( Object.Debug )
            //{
            //    ArcenDebugging.LogSingleLine( "n" + Object.ObjectNumber + "n sinT:" + sinT + " f1: " + Logic.Float1 + " time: " + Object.Time +
            //        " v1: " + Logic.Vector1 + " center: " + center + " final: " + final, Verbosity.DoNotShow );
            //}
        }
    }
}

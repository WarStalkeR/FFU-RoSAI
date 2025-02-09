using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    internal static class DrawTests
    {
        public static void DrawBlueCubeNearWorldCenter()
        {
            //you can render shapes to represent the result like this:
            List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
            shapes.Clear();

            DrawShape_SolidBoxOriented solidBox;
            solidBox.Center = Vector3.zero.PlusY( 1 );
            solidBox.Size = Vector3.one;
            solidBox.Color = ColorMath.Blue;
            solidBox.Rotation = Quaternion.Euler( 0, 90, 0 );

            shapes.Add( solidBox );
        }
    }
}

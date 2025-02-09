using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class VisRangeAndSimilarDrawing : IShapeToDrawHolder
    {
        public readonly List<IShapeToDraw> Shapes = List<IShapeToDraw>.Create_WillNeverBeGCed( 100, "VisRangeAndSimilarDrawing-Shapes" );
        public List<IShapeToDraw> GetListOfShapesToDraw() => Shapes;

        public static VisRangeAndSimilarDrawing Instance;

        public VisRangeAndSimilarDrawing()
        {
	        Instance = this;
            Engine_HotM.CurrentDebugShapeList = this.Shapes;
            DrawHelper.DrawList = this.Shapes;
        }

		public void DoPerFrameLogicOnMainThread()
        {
            Shapes.Clear();
        }
    }
}

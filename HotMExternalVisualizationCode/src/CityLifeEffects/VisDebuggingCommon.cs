using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	internal static class VisDebuggingCommon
	{
		public static List<IShapeToDraw> DebugDrawShapes
		{
			get => Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
		}

		private static List<IShapeToDraw> shapes => DebugDrawShapes;
		public static bool DoDebug_Pathfinding = false;
        public static bool DoDebug_CityNetwork = false;
        public static bool DoDebug_HaultMovement = false;
        public static bool Debug_ShowRoadDetails = false;
		private static readonly ConcurrentQueue<RefPair<IShapeToDraw,float>> fromThread = ConcurrentQueue<RefPair<IShapeToDraw, float>>.Create_WillNeverBeGCed( "VisDebuggingCommon-fromThread" );
		private static readonly List<RefPair<IShapeToDraw, float>> drawnOverTime = List<RefPair<IShapeToDraw, float>>.Create_WillNeverBeGCed( 100, "VisDebuggingCommon-drawnOverTime" );

        public static void JumpCamera(Vector3 pos)
		{
			VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition(pos, false );
		}

		public static void Debug_DrawCheckedPoint(Vector3 tp, int count)
		{
			Color color = Color.HSVToRGB((count * 0.4f) % 1, 1, 1);
			Debug_DrawCheckedPoint(tp, color);
		}

		public static void Debug_DrawCheckedPoint(Vector3 tp, Color color, float size=0.2f)
		{
			DrawShape_SolidBox box;
			box.Center = tp;
			box.Size = Vector3.one * size;
			box.Color = color;
			shapes.Add(box);
		}

		public static void Debug_DrawWireframe(Vector3 tp, float size, Color color)
		{
			{
				DrawShape_WireBox box;
				box.Center = tp;
				box.Size = Vector3.one * size;
				box.Color = color;
				box.Thickness = -1f;
                shapes.Add( box );
			}
			{
				DrawShape_WireBox box;
				box.Center = tp;
				box.Size = Vector3.one / 10;
				box.Color = color;
                box.Thickness = -1f;
                shapes.Add( box );
			}
		}

		public static void Debug_DrawLine(Vector3 p1, Vector3 p2, Color color, float Thickness = -1f )
		{
			DrawShape_Line line;
			line.Start = p1;
			line.End = p2;
			line.Color = color;
            line.Thickness = Thickness;
            shapes.Add(line);
		}

		public static Mesh GetPrimitiveMesh(PrimitiveType type)
		{
			GameObject gameObject = GameObject.CreatePrimitive(type);
			Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
			GameObject.Destroy(gameObject);
			return mesh;
		}

		internal static void QueueFromThread(IShapeToDraw v, float TimeToDraw )
		{
			fromThread.Enqueue( RefPair<IShapeToDraw, float>.Create( v, TimeToDraw ) );
		}

		internal static void HandleQueue()
		{
            shapes.Clear();

			for ( int i = drawnOverTime.Count - 1; i-- > 0; )
			{
				RefPair<IShapeToDraw, float> pair = drawnOverTime[i];
				pair.RightItem -= ArcenTime.AnyDeltaTime;
				if ( pair.RightItem <= 0 )
					drawnOverTime.RemoveAt( i );
				else
					shapes.Add( pair.LeftItem );
            }

            while (fromThread.TryDequeue(out RefPair<IShapeToDraw, float> v ))
			{
                drawnOverTime.Add(v);
                shapes.Add( v.LeftItem );
            }
		}

		internal static IShapeToDraw Debug_DrawObbThreaded(OBBUnity buildingObb)
		{
			Color color = Color.green;
			DrawShape_WireBox box;
			box.Center = buildingObb.Center;
			box.Size = buildingObb.Extents*2;
			box.Color = color;
			box.Thickness = -1f;
			return box;
		}

		internal static IShapeToDraw Debug_DrawLineThreaded(Vector3 p1, Vector3 p2, Color color, float Thickness = -1f )
		{
			DrawShape_Line line;
			line.Start = p1;
			line.End = p2;
			line.Color = color;
			line.Thickness = Thickness;
            return line;
		}
	}
}

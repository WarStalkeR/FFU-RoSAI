using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	internal static class PathHelperFuncs
	{
		public static bool LineBoundsIntersection(Vector3 segmentBegin, Vector3 segmentEnd, AABB boxSize, List<Vector3> intersections)
		{
			intersections.Clear();
			Vector3 beginToEnd = segmentEnd - segmentBegin;
			//Vector3 minToMax = boxSize.extents;
			Vector3 min = boxSize.Min; //boxSize.center - minToMax / 2;
			Vector3 max = boxSize.Max;//boxSize.center + minToMax / 2;
			Vector3 beginToMin = min - segmentBegin;
			Vector3 beginToMax = max - segmentBegin;
			float tNear = float.MinValue;
			float tFar = float.MaxValue;

			for (int axis = 0; axis < 3; axis++)
			{
				if (MathA.Abs(beginToEnd[axis]) < 0.0001f)
				{
					if (beginToMin[axis] > 0 || beginToMax[axis] < 0)
					{
						return intersections.Count > 0;
					}
				}
				else
				{
					float t1 = beginToMin[axis] / beginToEnd[axis];
					float t2 = beginToMax[axis] / beginToEnd[axis];
					float tMin = MathA.Min(t1, t2);
					float tMax = MathA.Max(t1, t2);
					if (tMin > tNear) tNear = tMin;
					if (tMax < tFar) tFar = tMax;
					if (tNear > tFar || tFar < 0)
					{
						return intersections.Count > 0;
					}
				}
			}

			if (tNear >= 0 && tNear <= 1)
			{
				intersections.Add(segmentBegin + beginToEnd * tNear);
				//ArcenDebugging.LogSingleLine($"+ {segmentBegin + beginToEnd * tNear}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			}
			else
			{
				//ArcenDebugging.LogSingleLine($"tNear outside bounds {tNear} ({segmentBegin + beginToEnd * tNear})", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			}

			if (tFar >= 0 && tFar <= 1)
			{
				intersections.Add(segmentBegin + beginToEnd * tFar);
				//ArcenDebugging.LogSingleLine($"+ {segmentBegin + beginToEnd * tFar}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			}
			else
			{
				//ArcenDebugging.LogSingleLine($"tFar outside bounds {tFar} ({segmentBegin + beginToEnd * tFar})", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			}

			return intersections.Count > 0;
		}

		public static (bool, Vector2) LineCircleIntersection(Vector2 pos1, Vector2 pos2, Vector2 center, float radius, bool bl)
		{
			Vector2 v1, v2;
			v1.x = pos2.x - pos1.x;
			v1.y = pos2.y - pos1.y;
			v2.x = pos1.x - center.x;
			v2.y = pos1.y - center.y;
			float b = (v1.x * v2.x) + (v1.y * v2.y);
			float c = 2 * ((v1.x * v1.x) + (v1.y * v1.y));
			b *= -2;
			float d = (b * b - 2 * c * (v2.x * v2.x + v2.y * v2.y - radius * radius));
			if (d <= 0)
			{
				//no intersection if d is negative
				return (false, Vector2.negativeInfinity);
			}

			d = Mathf.Sqrt(d);
			float u1 = (b - d) / c;
			float u2 = (b + d) / c;

			//actual route segment doesn't intersect
			if (!(u1 <= 1 && u1 >= 0 && u2 <= 1 && u2 >= 0))
				return (false, Vector2.negativeInfinity);

			//intersection along the infinite line path
			Vector2 interP1 = new Vector2(pos1.x + v1.x * u1, pos1.y + v1.y * u1);
			Vector2 interP2 = new Vector2(pos1.x + v1.x * u2, pos1.y + v1.y * u2);
			Vector2 midpoint = new Vector2(interP1.x + interP2.x, interP1.y + interP2.y) / 2;
			if ((midpoint - center).sqrMagnitude < float.Epsilon)
			{
				//passes through the exact building center
				//take perpendicular vector and offset
				midpoint = midpoint + new Vector2(-midpoint.y, midpoint.x).normalized * radius;
				return (true, midpoint);
			}

			midpoint = ((midpoint - center).normalized * radius) + center;

			return (true, midpoint);
		}

		#region Bresenham's Algorithm

		internal static void GetLine(Waypoint waypoint1, Waypoint waypoint2, List<MapCell> cellList)
		{
			ArcenGroundPoint c1 = PushCell(GetCellFor(waypoint1), cellList);
			ArcenGroundPoint c2 = PushCell(GetCellFor(waypoint2), cellList);

			int x0 = c1.X;
			int x1 = c2.X;
			int y0 = c1.Z;
			int y1 = c2.Z;

			if (MathA.Abs(y1 - y0) < MathA.Abs(x1 - x0))
			{
				if (x0 > x1)
					PlotLineLow(x1, y1, x0, y0, cellList);
				else
					PlotLineLow(x0, y0, x1, y1, cellList);
			}
			else if (y0 > y1)
				PlotLineHigh(x1, y1, x0, y0, cellList);
			else
				PlotLineHigh(x0, y0, x1, y1, cellList);
		}

		private static ArcenGroundPoint PushCell(MapCell cell, List<MapCell> cellList)
		{
			if (cell == null) return ArcenGroundPoint.ZeroZeroPoint;
			cellList.AddIfNotAlreadyIn(cell);
			return cell.CellLocation;
		}

		public static MapCell GetCellFor(Waypoint wp)
		{
			return CityMap.TryGetWorldCellAtCoordinates(wp);
		}

		private static MapCell GetCellFor(ArcenGroundPoint gp)
		{
			return CityMap.TryGetExistingCellAtLocation(gp);
		}

		private static void PlotLineLow(int x0, int y0, int x1, int y1, List<MapCell> cellList)
		{
			int dx = x1 - x0;
			int dy = y1 - y0;
			int yi = 1;
			if (dy < 0)
			{
				yi = -1;
				dy = -dy;
			}

			int D = (2 * dy) - dx;
			int y = y0;

			for (int x = x0; x <= x1; x++)
			{
				PushCell(GetCellFor(new ArcenGroundPoint(x0, y0)), cellList);
				if (D > 0)
				{
					y += yi;
					D += 2 * (dy - dx);
				}
				else
					D += 2 * dy;
			}
		}

		private static void PlotLineHigh(int x0, int y0, int x1, int y1, List<MapCell> cellList)
		{
			int dx = x1 - x0;
			int dy = y1 - y0;
			int xi = 1;
			if (dx < 0)
			{
				xi = -1;
				dx = -dx;
			}

			int D = (2 * dx) - dy;
			int x = x0;

			for (int y = y0; y <= y1; y++)
			{
				PushCell(GetCellFor(new ArcenGroundPoint(x0, y0)), cellList);
				if (D > 0)
				{
					x += xi;
					D += 2 * (dx - dy);
				}
				else
					D += 2 * dx;
			}
		}

		#endregion
	}
}

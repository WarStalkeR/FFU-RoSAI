using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class OpenGroundPathfinder
	{
		//in-game values

		//private static readonly List<MapCell> cellList = List<MapCell>.Create_WillNeverBeGCed(25, "DronePathfinder-CellList", 15);
		private static readonly List<MapItem> buildingsSortable = List<MapItem>.Create_WillNeverBeGCed(25, "OpenGroundPathfinder-Buildings", 25);
		private static readonly List<Vector3> WorkingList = List<Vector3>.Create_WillNeverBeGCed(25, "OpenGroundPathfinder-WorkingList", 25);
		private static float flightHeight = 0.01f;
		private const float MaxFlightCeiling = 0.1f;
		
		public static float MinFlightFloor = 0.01f;
		public static float FlightClearance = 0.01f;

		private static MersenneTwister rand = new MersenneTwister(Engine_Universal.PermanentQualityRandom.Next());

		//private static bool once = true;
		
		public static void GetPathBetween(Vector3 waypoint1, Vector3 waypoint2, List<Vector3> buffer, IReadOnlyList<MapCell> cellList)
		{
			buffer.Clear();
			//PathHelperFuncs.GetLine(waypoint1, waypoint2, cellList); //this is just not working out well
			flightHeight = 0.1f; //MathA.Max(MinFlightFloor, MathA.Max(waypoint1.y, waypoint2.y) + FlightClearance);


			foreach (MapCell cell in cellList)
			{
				buildingsSortable.Clear();
				buildingsSortable.AddRange(cell.BuildingList.GetDisplayList() );
				buildingsSortable.Sort((a, b) => Vector3.Distance(a.CenterPoint, waypoint1).CompareTo(Vector3.Distance(b.CenterPoint, waypoint1)));
				
				DoBuildingIntersections(waypoint1, waypoint2, buffer);
			}

			buffer.Insert(0, new Vector3(waypoint1.x, flightHeight, waypoint1.z));

			buffer.Add(new Vector3(waypoint2.x, flightHeight, waypoint2.z));
			buffer.Add(waypoint2);
		}

		private static void DoBuildingIntersections(Vector3 waypoint1, Vector3 waypoint2, List<Vector3> buffer)
		{
			float hue = 0;
			Color c = Color.HSVToRGB(hue, 1, 1);
			float adelta = Mathf.PI / 90;
			foreach (MapItem building in buildingsSortable)
			{
				Vector3 prevpoint = buffer.Count == 0 ? waypoint1 : buffer[buffer.Count - 1];
				if (building.MaxY + FlightClearance <= flightHeight) continue;
				Vector3 cent = new Vector3(building.CenterPoint.x, prevpoint.y, building.CenterPoint.z);
				if ((cent - prevpoint).sqrMagnitude < 0.5f) continue;
				//cent = new Vector3(building.CenterPoint.x, waypoint2.y, building.CenterPoint.z);
				if ((cent - waypoint2).sqrMagnitude < 0.5f) continue;
				float radius = 0.002f + building.OBBCache.GetExpensiveRadius();
				(bool doesIntersect, Vector2 v1) = PathHelperFuncs.LineCircleIntersection(new Vector2(prevpoint.x, prevpoint.z), new Vector2(waypoint2.x, waypoint2.z), new Vector2(cent.x, cent.z), radius, true);
				if (doesIntersect)
				{
					building.OBBCache.GetOuterRect();
                    AABB b = building.OBBCache.GetOuterAABB();
					//buffer.Add(new Vector3(v1.x, flightHeight, v1.y));
					//VisDebuggingCommon.Debug_DrawObbThreaded(buildingObb);
					if (PathHelperFuncs.LineBoundsIntersection(prevpoint, waypoint2, b, WorkingList) && WorkingList.Count > 1)
					{
						VisDebuggingCommon.Debug_DrawLine(prevpoint, waypoint2, Color.blue);

						VisDebuggingCommon.Debug_DrawCheckedPoint(building.OBBCache.TopCenter, c);
						Vector3 intersect1 = WorkingList[0];
						Vector3 intersect2 = WorkingList[1];

						VisDebuggingCommon.Debug_DrawLine(WorkingList[0], WorkingList[1], Color.magenta);

						Vector3 min = b.Min;
                        Vector3 max = b.Max;
                        Vector3[] allCorners =
						{
							new Vector3(min.x, flightHeight, min.z),
							new Vector3(min.x, flightHeight, max.z),
							new Vector3(max.x, flightHeight, min.z),
							new Vector3(max.x, flightHeight, max.z)
						};

						Vector3 ao = (building.CenterPoint - intersect1).normalized;
						Vector3 ab = (intersect2 - intersect1).normalized;
						Vector3 d = Vector3.Cross(Vector3.Cross(ao, ab), ab).normalized.ReplaceY(0); //vector that points towards building center

						Vector3 best = Vector3.positiveInfinity;
						foreach (Vector3 cr in allCorners)
						{
							if (best.y > 10000 || Vector3.Dot(d, cr) > Vector3.Dot(d, best))
							{
								best = cr;
							}
						}

						VisDebuggingCommon.Debug_DrawCheckedPoint(best, c, 0.1f);

						Vector3 newpoint = best - d * 0.02f;
						//check building intersections with prevpoint and new point
						//DoBuildingIntersections(prevpoint, newpoint, buffer);

						buffer.Add(newpoint);
					}
					else
					{
						VisDebuggingCommon.Debug_DrawLine(new Vector3(building.OBBCache.TopCenter.x, 0, building.OBBCache.TopCenter.z), new Vector3(v1.x, 0, v1.y), Color.yellow);
					}
				}

				hue += adelta;
				c = Color.HSVToRGB(hue, 1, 1);
				if (doesIntersect)
					VisDebuggingCommon.Debug_DrawCheckedPoint(building.OBBCache.TopCenter, c);
				//else
				//VisDebuggingCommon.Debug_DrawWireframe(building.OBBCache.OBB.TopCenter, 0.2f, c);
			}
		}
	}
}

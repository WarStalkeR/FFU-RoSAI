using Arcen.HotM.Core;
using Arcen.Universal;
using System;

using Unity.Mathematics;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class StreetPathfinder
	{
		private static int MaxPathLength = 150;
		private static readonly List<MapCell> cellList = List<MapCell>.Create_WillNeverBeGCed(25, "StreetPathfinder-CellList", 15);
		private static readonly List<Vector3> tempPath = List<Vector3>.Create_WillNeverBeGCed(25, "StreetPathfinder-tempPath");

		public static void FillRandomWalk(RandomGenerator rand, TransportMethod transMethod, List<Waypoint> waypoints, float MaxDistanceBetweenPoints,
            List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
			int i = 1;
			MapCell currentCell = null;
			foreach ( MapCell cell in CityMap.CityLifeMapCells )
			{
				bool cellHasRoads = cell.AllRoads.Count > 0;
				if ( !cellHasRoads || !CheckForValidRoads( transMethod, cell ) || rand.Next( i ) != 0 ) continue;
				currentCell = cell;
				i++;
			}
			if (currentCell == null) return;
			FillRandomWalkStartingIn(rand, TransportMethod.Driving, waypoints, currentCell, MaxDistanceBetweenPoints, workingLaneList );
		}

		public static void FillCityVehiclePath(MersenneTwister rand, List<Vector3> waypoints, MapCell currentCell, Vector3 exactStart, float turnRadius, float speed,
            List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
            waypoints.Clear(); //Chris added.  Seems important

            List<MapItem> roadsWithTransport = GetCurrentValidRoads(currentCell, TransportMethod.Driving);
			if (roadsWithTransport == null || roadsWithTransport.Count == 0) return;
			MapItem currentRoad = roadsWithTransport.GetRandom(rand);
			if (exactStart.x > float.NegativeInfinity)
			{
				currentRoad = roadsWithTransport.GetFirstOrDefaultWhere(r => (exactStart - r.CenterPoint).sqrMagnitude <= 0.5f) ?? currentRoad;
			}

            List<LanePrefab> drivingRoadList = currentRoad.Type.LanesForTransportMethod.GetListForOrNull( TransportMethod.Driving );
			if ( drivingRoadList == null || drivingRoadList.Count <= 1) return;
			LanePrefab curLanePrefab = drivingRoadList.GetRandom(rand);
			if (curLanePrefab.LanePoints.Count < 2) return;

			//waypoints.Add(currentRoad.CenterPoint);
			Vector3 lastWaypoint = currentRoad.CenterPoint;
			Vector3 lastTransformedPoint = currentRoad.TransformPoint_Threadsafe(curLanePrefab.LastPoint);
			for (int path = 0; path < MaxPathLength; path++)
			{
				float laneWidth = curLanePrefab.Type.LaneWidth * currentRoad.Scale.y;
				currentCell = GetNextCell(TransportMethod.Driving, currentCell, ref roadsWithTransport, lastTransformedPoint, laneWidth, 
					currentCell.CellBounds_Min, currentCell.CellBounds_Max );
				if (currentCell == null) break;
				roadsWithTransport = GetCurrentValidRoads(currentCell, TransportMethod.Driving);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0 || (roadsWithTransport.Count == 1 && roadsWithTransport[0] == currentRoad)) break;

				LanePrefab newLane = ContinueDownRoad(currentRoad, curLanePrefab, lastTransformedPoint, roadsWithTransport, 
					TransportMethod.Driving, rand, out int index, out _, out MapItem newRoad, workingLaneList );

				if (newLane == null && path < 30) break;
				if (newLane == null) break;
				if (newLane.IsDeadEnd && path < 30) break;
				
				AddPointWithRadius(waypoints, currentRoad.CenterPoint, newRoad.CenterPoint, turnRadius, speed);
				
				currentRoad = newRoad;
				//waypoints.Add(currentRoad.CenterPoint);

				//TODO Clean this up

				//lastTransformedPoint = currentRoad.TransformPoint_Threadsafe(newLane.Points[newLane.Points.Count - (index+1)]);

				int traverseDir = index == 0 ? 1 : -1;
				for (int j = index + traverseDir; j >= 0 && j < newLane.LanePoints.Count; j += traverseDir)
				{
					Vector3 tp = currentRoad.TransformPoint_Threadsafe(newLane.LanePoints[j]);
					lastTransformedPoint = tp;
				}

				curLanePrefab = newLane;
			}
		}

		public static void FillCityVehiclePath(MersenneTwister rand, List<Vector3> waypoints, MapCell currentCell, float turnRadius, float speed,
            List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
			FillCityVehiclePath(rand, waypoints, currentCell, Vector3.negativeInfinity, turnRadius, speed, workingLaneList );
		}

		public static void GetMilitaryPath(MapItem origin, MapItem destination, List<Vector3> waypoints, float turnRadius, float speed, MersenneTwister rand,
            List<MapPathingNode> workingStack, List<MapPathingNode> workingVisited )
		{
            waypoints.Clear(); //Chris added.  Seems important

            if (origin == null || destination == null || origin.ParentCell == null || destination.ParentCell == null) return;
			if (origin.ParentCell == destination.ParentCell)
			{
				GetPathFromToWithinCell(origin.CenterPoint, destination.CenterPoint, rand, waypoints, origin.ParentCell, turnRadius, speed,
					workingStack, workingVisited );
			}
			else
			{
				cellList.Clear();
				MapCell currentCell = origin.ParentCell;
				PathHelperFuncs.GetLine(origin.CenterPoint, destination.CenterPoint, cellList);
				MapCell c = PathHelperFuncs.GetCellFor(origin.CenterPoint);
				cellList.Sort((a,b) => Vector2.Distance(a.rawCellLocation.ToVector2(), c.rawCellLocation.ToVector2()).CompareTo(Vector2.Distance(b.rawCellLocation.ToVector2(), c.rawCellLocation.ToVector2())) );
				MapItem currentStartItem = origin;
				foreach (MapCell mapCell in cellList)
				{
					if(!(mapCell is MapCell nextCell) || nextCell == origin.ParentCell) continue;
					Vector3 nextCellCenter = nextCell.Center;
					MapItem endRoad = GetNearestRoadTo(nextCellCenter, currentCell) as MapItem;
					
					if (endRoad == null)
					{
						//probably a park, oof, fly over it?
						continue;
					}
					tempPath.Clear();
					GetMilitaryPath(currentStartItem, endRoad, tempPath, turnRadius, speed, rand, workingStack, workingVisited );
					tempPath.Reverse();
					waypoints.AddRange(tempPath);

					currentStartItem = GetNearestRoadTo(endRoad.CenterPoint, nextCell) as MapItem;
					currentCell = nextCell;

					if (nextCell == destination.ParentCell)
					{
						endRoad = GetNearestRoadTo(destination.CenterPoint, nextCell) as MapItem;
						tempPath.Clear();
						GetMilitaryPath(currentStartItem, endRoad, tempPath, turnRadius, speed, rand, workingStack, workingVisited );
						tempPath.Reverse();
						waypoints.AddRange(tempPath);
					}
				}
			}
		}

		private static MapItem GetNearestRoadTo(Vector3 NextCellCenter, MapCell CurrentCell)
		{
			MapItem best = null;
			float bestDist = float.MaxValue;

			foreach ( MapItem r in CurrentCell.DrivableRoads )
			{
				float d = (r.CenterPoint - NextCellCenter).sqrMagnitude;
				if ( bestDist > d )
				{
					bestDist = d;
					best = r;
				}
			}
			if (best == null)
			{
				ArcenDebugging.LogSingleLine($"Failed to find a road! {CurrentCell.DrivableRoads.Count}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			}
			return best;
		}

		public static void FleeSingleBuilding(RandomGenerator Rand, TransportMethod transMethod, List<Waypoint> Waypoints, MapItem fleeAwayFromBuilding, MapCell cell,
            List<MapPathingNode> workingStack, List<MapPathingNode> workingVisited )
		{
			//MapCell cell = fleeAwayFromBuilding.ParentICell as MapCell;
			Vector3 start = fleeAwayFromBuilding.CenterPoint;
			start.Scale(new Vector3(1,0,1));
			ISimBuilding goHere = CityVehiclesHelper.GetDestinationBuildingOrNull(null, null, false, 3, 18, start, Rand, null);
			MapItem r2 = GetNearestRoadTo(goHere.GetMapItem().CenterPoint, cell);
			Vector3 p1 = CityVehiclesSpawner.GetPositionNearBuilding(Rand, fleeAwayFromBuilding);
			Waypoints.Add(start);
			int max = 5;
			do
			{
				MapItem r = GetNearestRoadTo(p1,cell);
				if ((r.CenterPoint - start).sqrMagnitude > (p1 - start).sqrMagnitude)
				{
					break;
				}

				p1 += (p1 - start).normalized * 0.1f;

				if (max-- <= 0)
				{
					max = 5;
					p1 = CityVehiclesSpawner.GetPositionNearBuilding(Rand, fleeAwayFromBuilding);
				}
			} while (true);

			Waypoints.Add(p1);

			GetPathFromToWithinCell(p1, r2.CenterPoint, Rand, Waypoints, cell, transMethod, workingStack, workingVisited );
		}

		public static void FleeBuildingRandom(RandomGenerator Rand, List<Waypoint> Waypoints, MapItem fleeOrigin, MapCell Cell)
		{
			Vector3 start = fleeOrigin.CenterPoint;
			Waypoints.Add(start);
			Vector3 p1 = CityVehiclesSpawner.GetPositionNearBuilding(Rand, fleeOrigin);
			Vector3 dir = (p1 - start);
			dir.Scale(new Vector3(1,0,1));
			dir.Normalize();
			dir.Scale(new Vector3(0.25f, 0, 0.25f));
			Waypoints.Add(p1);
			int max = Rand.Next(10, 15);
			for (int i = 0; i < max; i++)
			{
				p1 += dir;
				CityVehiclesSpawner.GetRandomPointNear(Rand, p1, 0.15f);
				Waypoints.Add(p1);
			}
		}

		public static void FillRandomWalkStartingIn( RandomGenerator rand, TransportMethod transMethod, List<Waypoint> waypoints, MapCell currentCell,
			float MaxDistanceBetweenPoints, List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
			float maxDistanceSquared = MaxDistanceBetweenPoints * MaxDistanceBetweenPoints;

			waypoints.Clear();
			int debugStage = 100;
			bool driveSide = GameSettings.Current.GetBool( "MainGameCarsDriveOnTheRight" );
			try
			{
				debugStage = 200;
				List<MapItem> roadsWithTransport = GetCurrentValidRoads( currentCell, transMethod );
				if ( roadsWithTransport == null || roadsWithTransport.Count == 0 ) return;
				MapItem currentRoad = roadsWithTransport.GetRandom( rand );

				List<LanePrefab> currentTransRoadList = currentRoad.Type.LanesForTransportMethod.GetListForOrNull( transMethod );
				if ( currentTransRoadList == null || currentTransRoadList.Count <= 1 ) return;
				MapItem prevRoad = null;
				Vector3 prevPoint = Vector3.negativeInfinity;
				LanePrefab curLanePrefab = currentTransRoadList.GetRandom( rand );
				if ( curLanePrefab.LanePoints.Count < 2 )
				{
					ArcenDebugging.LogWithStack( $"Lane prefab `{curLanePrefab.ID}` did not have any points!", Verbosity.DoNotShow );
					return;
				}
				//can't reverse a parking lane easily
				if ( curLanePrefab.IsDeadEnd && (curLanePrefab.TurnType == LaneTurnType.Right) == driveSide ) return;
				//might be able to do it programatically; shift the last two points 4 units to local-right and the 3rd 2 units
				Vector3 lastTransformedPoint = Vector3.negativeInfinity;

				foreach ( Vector3 p in curLanePrefab.LanePoints )
				{
					Vector3 tp = currentRoad.TransformPoint_Threadsafe( p );
					waypoints.Add( tp );
					lastTransformedPoint = tp;
				}
				AdjustForRighthandedness( currentRoad, curLanePrefab, waypoints, transMethod, driveSide );
				if ( curLanePrefab.IsDeadEnd )
				{
					waypoints.Insert( 0, waypoints[0] );
				}

				lastTransformedPoint = waypoints.LastOrDefault;
				debugStage = 300;
				for ( int path = 0; path < MaxPathLength; path++ )
				{
					float laneWidth = curLanePrefab.Type.LaneWidth * currentRoad.Scale.y;
					currentCell = GetNextCell( transMethod, currentCell, ref roadsWithTransport, lastTransformedPoint, laneWidth, 
						currentCell.CellBounds_Min, currentCell.CellBounds_Max );
					if ( currentCell == null ) break;
					debugStage = 400;
					roadsWithTransport = GetCurrentValidRoads( currentCell, transMethod );
					if ( roadsWithTransport == null || roadsWithTransport.Count == 0 || (roadsWithTransport.Count == 1 && roadsWithTransport[0] == currentRoad) ) break;

					debugStage = 500;
					LanePrefab newLane = null;
					int index = -1;
					Vector3 pos = Vector3.negativeInfinity;
					newLane = ContinueDownRoad( currentRoad, curLanePrefab, lastTransformedPoint, roadsWithTransport, transMethod, rand, 
						out index, out pos, out MapItem newRoad, workingLaneList );

					if ( newLane == null && path < 30 ) continue;
					if ( newLane == null ) break;
					if ( newLane.IsDeadEnd && path < 30 ) continue;

					prevRoad = currentRoad;
					currentRoad = newRoad;

					if ( (pos - lastTransformedPoint).GetSquareGroundMagnitude() > maxDistanceSquared )
						break; //this is suggesting we go further than we are allowed, so stop trying to do that

					debugStage = 600;
					waypoints.Add( pos );
					//traverse lane
					int traverseDir = index == 0 ? 1 : -1;
					prevPoint = lastTransformedPoint;
					for ( int j = index + traverseDir; j >= 0 && j < newLane.LanePoints.Count; j += traverseDir )
					{
						Vector3 tp = currentRoad.TransformPoint_Threadsafe( newLane.LanePoints[j] );
						waypoints.Add( tp );
						lastTransformedPoint = tp;
					}
					debugStage = 700;
					curLanePrefab = newLane;
				}
				if ( curLanePrefab.IsDeadEnd )
				{
					waypoints.Add( new Waypoint( lastTransformedPoint, WaypointTask.STOP ) );
				}
			}
			catch ( System.Exception e )
			{
				ArcenDebugging.LogDebugStageWithStack( "FillRandomWalk", debugStage, e, Verbosity.ShowAsError );
			}
		}

		// Presently there are no tiles supplying this data to really do anything with it.
		private static void UseMapPathingPoint(RandomGenerator rand, List<Waypoint> waypoints, MapCell currentCell)
		{
			MapPathingPoint pt = currentCell.ParentTile.PathingPoints.GetRandom(rand);
			for(int i = 0; i < 25; i++)
			{
				waypoints.Add(new Waypoint(pt.TransformPoint_Threadsafe(pt.Position), WaypointTask.NONE));
				pt = pt.LinkedPoints.GetRandom(rand);
			}
		}

		// Presently there are no tiles supplying this data to really do anything with it.
		private static void UseMapPathingRegion(RandomGenerator rand, List<Waypoint> waypoints, MapCell currentCell)
		{
			MapPathingRegion pt = currentCell.ParentTile.PathingZones.GetRandom(rand);
			waypoints.Add(pt.TransformPoint_Threadsafe(pt.OBBCache.Center));
			switch(pt.PathingType)
			{
                          default:
                            break;
                        }
		}

		private static LanePrefab ContinueDownRoad(MapItem currentRoad, LanePrefab curLanePrefab, Vector3 lastTransformedPoint, 
			List<MapItem> roadsWithTransport, TransportMethod transMethod, RandomGenerator rand, out int index, 
			out Vector3 newTransformedPos, out MapItem newRoad, List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
			float wid = curLanePrefab.Type.LaneWidth * currentRoad.Scale.y;
			LanePrefab newLane;
			foreach (MapItem road in roadsWithTransport)
			{
				if (road == currentRoad) continue;
				//if (currentRoad.GetRoughIsFurtherAwayThan(road, 5)) continue;
				if (road.GetBasicDistanceToPointXZ(lastTransformedPoint, wid) > road.Diameter + wid) continue;
				newLane = GetRandomLane(road, transMethod, lastTransformedPoint, wid/2, out index, out newTransformedPos, rand, workingLaneList );
				if (newLane == null) continue;
				//Vector3 p2 = road.TransformPoint_Threadsafe(newLane.Points[(newLane.Points.Count - 1) - index]);
				//if (newTransformedPos == lastTransformedPoint || p2 == lastTransformedPoint) continue; //I shouldn't have to do this
				newRoad = road;
				return newLane;
			}
			newRoad = null;
			index = -1;
			newTransformedPos = Vector3.negativeInfinity;
			return null;
		}

		private static void AdjustForRighthandedness(MapItem startRoad, LanePrefab lanePrefab, List<Waypoint> waypoints, TransportMethod transport, bool isRightHanded)
		{
			if (transport != TransportMethod.Driving) return;
			List<LanePrefab> allLanes = startRoad.Type.LanesForTransportMethod.GetListForOrNull( transport );
			if ( allLanes == null || allLanes.Count <= 1) return;

			if (lanePrefab.TurnType != LaneTurnType.None)
			{
				if ( VisDebuggingCommon.Debug_ShowRoadDetails )
					startRoad.DebugMessageForTooltip = $"{startRoad.Type.ID} lane {lanePrefab.ID}: reverse points ({isRightHanded}).";
				if (!isRightHanded)
				{
					waypoints.Reverse();
				}
				return;
			}

			Vector3 dirThis = lanePrefab.LanePoints[1] - lanePrefab.FirstPoint;
			LanePrefab otherLane = null;
			if (startRoad.Type.ID.Contains("Hwy"))
			{
				float maxDist = 0;
				for (int i = 0; i < allLanes.Count; i++)
				{
					LanePrefab tempLane = allLanes[i];
					if (tempLane == lanePrefab) continue;
					if (tempLane.TurnType != LaneTurnType.None) continue;
					float d = (lanePrefab.FirstPoint - tempLane.FirstPoint).sqrMagnitude;
					if(d > maxDist)
					{
						otherLane = tempLane;
						maxDist = d;
					}
				}
			}
			else
			{
				for (int i = 0; i < allLanes.Count; i++)
				{
					otherLane = allLanes[i];
					if (otherLane.TurnType != LaneTurnType.None) continue;
					if (otherLane != lanePrefab
						&& (lanePrefab.FirstPoint  - otherLane.FirstPoint).sqrMagnitude > 0
						&& (lanePrefab.LanePoints[1] - otherLane.LanePoints[1]).sqrMagnitude > 0)
					{
						Vector3 dirOther = otherLane.LanePoints[1] - otherLane.FirstPoint;
						Vector3 checkB = new Vector3(dirThis.x, dirThis.z, 0) - new Vector3(dirOther.x, dirOther.z, 0);
						if (Mathf.Approximately(checkB.x, 0) || Mathf.Approximately(checkB.y, 0))
							break;
					}
				}
			}
			Vector3 dirToOther = otherLane.LanePoints[1] - otherLane.FirstPoint; //try this

			Vector3 cross = Vector3.Cross(new Vector3(dirThis.x, dirThis.z, 0), new Vector3(dirToOther.x, dirToOther.z, 0));
            if ( VisDebuggingCommon.Debug_ShowRoadDetails )
                startRoad.DebugMessageForTooltip += $"{startRoad.Type.ID} lane {lanePrefab.ID}: reverse points ({(cross.z < 0) == isRightHanded}).\n";

			if ((cross.z < 0) == isRightHanded)
			{
				waypoints.Reverse();
			}
		}

		private static MapCell GetNextCell(TransportMethod transport, MapCell startcell, ref List<MapItem> roadsWithTransport, Vector3 lastTransformedPoint, float wid, 
			Vector3 boundsMin, Vector3 boundsMax )
		{
			if (lastTransformedPoint.x - wid < boundsMin.x)
			{
				startcell = startcell.CellWest;
				if (startcell == null) return startcell;
				roadsWithTransport = GetCurrentValidRoads(startcell, transport);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return startcell;
			}
			if (lastTransformedPoint.x + wid > boundsMax.x)
			{
				startcell = startcell.CellEast;
				if (startcell == null) return startcell;
				roadsWithTransport = GetCurrentValidRoads(startcell, transport);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return startcell;
			}
			if (lastTransformedPoint.z - wid < boundsMin.z)
			{
				startcell = startcell.CellSouth;
				if (startcell == null) return startcell;
				roadsWithTransport = GetCurrentValidRoads(startcell, transport);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return startcell;
			}
			if (lastTransformedPoint.z + wid > boundsMax.z)
			{
				startcell = startcell.CellNorth;
				if (startcell == null) return startcell;
				roadsWithTransport = GetCurrentValidRoads(startcell, transport);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return startcell;
			}
			return startcell;
		}

		private static List<MapItem> GetCurrentValidRoads(MapCell startCell, TransportMethod transport)
		{
			if ( startCell == null) 
				return null;
			switch (transport)
			{
				case TransportMethod.Driving:
					return startCell.DrivableRoads;
				case TransportMethod.Walking:
					return startCell.WalkableRoads;
				case TransportMethod.Train:
					return startCell.Tracks;
				default:
					ArcenDebugging.LogWithStack($"Asked for roads with invalid transport type {transport} during StreetPathfinder.FillRandomWalk.", Verbosity.ShowAsError);
					return null;
			}
		}

		private static bool CheckForValidRoads(TransportMethod transport, MapCell cell)
		{
			switch (transport)
			{
				case TransportMethod.Driving:
					if (cell.DrivableRoads.Count == 0) return false;
					break;
				case TransportMethod.Walking:
					if (cell.WalkableRoads.Count == 0) return false;
					break;
				case TransportMethod.Train:
					if (cell.Tracks.Count == 0) return false;
					break;
			}
			return true;
		}

		private static LanePrefab GetRandomLane(MapItem road, TransportMethod transMethod, Vector3 lastTransformedPoint, float wid, out int pointIndex, 
			out Vector3 transformedPoint, RandomGenerator rand, List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList )
		{
			GetNearbyLanes(road, transMethod, lastTransformedPoint, wid, false, workingLaneList );
			if ( workingLaneList.Count == 0)
			{
				pointIndex = -1;
				transformedPoint = Vector3.negativeInfinity;
				return null;
			}
			(LanePrefab lane, int index, Vector3 tPos) = workingLaneList.GetRandom(rand);
			pointIndex = index;
			transformedPoint = tPos;
            workingLaneList.Clear();
            return lane;
		}

		private static void GetNearbyLanes(MapItem road, TransportMethod transport, Vector3 lastTransformedPoint, float wid, bool onlyUnique,
            List<(LanePrefab lane, int index, Vector3 tPos)> results )
		{
			results.Clear();
            List<LanePrefab> lanesToCheck = road.Type.LanesForTransportMethod.GetListForOrNull( transport );
			if ( lanesToCheck == null )
				return;

            //float wid = lanePrefab.Type.Row.LaneWidth * road.Scale.y;
            foreach ( LanePrefab otherlane in lanesToCheck )
			{
				for (int i = 0; i < otherlane.LanePoints.Count; i += otherlane.LanePoints.Count - 1)
				{
					Vector3 p = otherlane.LanePoints[i];
					Vector3 tp = road.TransformPoint_Threadsafe(p);
					float distToX = lastTransformedPoint.x - tp.x;
					float distToY = lastTransformedPoint.y - tp.y;
					float distToZ = lastTransformedPoint.z - tp.z;
					distToX = distToX < 0 ? -distToX : distToX;
					distToY = distToY < 0 ? -distToY : distToY;
					distToZ = distToZ < 0 ? -distToZ : distToZ;
					if (distToX < wid / 2 && distToY < wid / 2 && distToZ < wid / 2)
					{
						results.Add((otherlane, i, tp));

						//cheap weighting to drive straight through more often
						if (!onlyUnique && results.Count == 0 && otherlane.TurnType == LaneTurnType.None) results.Add((otherlane, i, tp));
						//cheap weighting to take parking less often
						if (!onlyUnique && !otherlane.IsDeadEnd) results.Add((otherlane, i, tp));
					}
				}
			}
		}

		public static void GetPathFromToWithinCell(Vector3 start, Vector3 end, RandomGenerator rand, List<Vector3> waypoints, MapCell currentCell, float turnRadius, float speed,
            List<MapPathingNode> workingStack, List<MapPathingNode> workingVisited )
		{
			start.Scale(new Vector3(1, 0, 1));
			end.Scale(new Vector3(1, 0, 1));
			float origDist = (start - end).sqrMagnitude;
			if (origDist < 5f) return;
			TransportMethod transMethod = TransportMethod.Driving;
			int debugStage = 0;
			try
			{
				debugStage = 100;

				List<MapItem> roadsWithTransport = GetCurrentValidRoads(currentCell, transMethod);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return;
				roadsWithTransport.Sort((a, b) => (a.CenterPoint - start).sqrMagnitude.CompareTo((b.CenterPoint - start).sqrMagnitude));
				MapItem currentRoad = roadsWithTransport.GetFirstOrDefaultWhere(r => r.Type.LanesForTransportMethod.GetListForOrNull( transMethod )?.Exists(l => !l.IsDeadEnd && l.LanePoints.Count > 2)??false);
				debugStage = 130;
                List<LanePrefab> transportationList = currentRoad.Type.LanesForTransportMethod.GetListForOrNull( transMethod );
				if ( transportationList == null || transportationList.Count <= 1) return;
				
				LanePrefab curLanePrefab = transportationList.GetRandom(rand);

				//might be able to do it programatically; shift the last two points 4 units to local-right and the 3rd 2 units
				debugStage = 300;

                //waypoints.Add(currentRoad.CenterPoint);

                workingStack.Clear();
                workingVisited.Clear();
                workingStack.Add(new MapPathingNode()
				{
					cell = currentCell,
					parentNode = null,
					road = currentRoad,
					endPos = currentRoad.CenterPoint,
					distance = origDist
				});

                MapPathingNode activeNode = null;
				while ( workingStack.Count > 0)
				{
					activeNode = workingStack.RemoveAtAndReturn(0);
					if ( workingVisited.Contains(activeNode)) continue;
                    workingVisited.Add(activeNode);

					if (activeNode.distance < .5f) break; //close enough

					foreach (MapItem newRoad in GetConnections(activeNode.road, roadsWithTransport, transMethod))
					{
						float dist = (newRoad.CenterPoint - end).sqrMagnitude;
                        MapPathingNode newNode = new MapPathingNode()
						{
							cell = currentCell,
							parentNode = activeNode,
							road = newRoad,
							endPos = newRoad.CenterPoint,
							distance = dist
						};
						if ( workingVisited.Contains(newNode)) continue;
                        /*if (stack.Contains(newNode))
						{
							continue;
						}*/

                        workingStack.AddIfNotAlreadyIn(newNode);
					}

                    workingStack.Sort((a, b) => a.distance.CompareTo(b.distance));
				} 
				debugStage = 100000;
                workingVisited.Sort((a, b) => a.distance.CompareTo(b.distance));
				activeNode = workingVisited[0];
				while (activeNode != null)
				{
					if (activeNode.parentNode != null && activeNode.parentNode.distance > activeNode.distance)
					{
						AddPointWithRadius(waypoints, activeNode, turnRadius, speed);
					}
					activeNode = activeNode.parentNode;
				}
			}
			catch (Exception e)
			{
				ArcenDebugging.LogDebugStageWithStack($"Find path from {start} to {end} CityVehicleed a problem.", debugStage, e, Verbosity.ShowAsError);
			}
		}

		public static void GetPathFromToWithinCell(Vector3 start, Vector3 end, RandomGenerator rand, List<Waypoint> waypoints, MapCell currentCell, TransportMethod transMethod,
            List<MapPathingNode> workingStack, List<MapPathingNode> workingVisited )
		{
			start.Scale(new Vector3(1, 0, 1));
			end.Scale(new Vector3(1, 0, 1));
			float origDist = (start - end).sqrMagnitude;
			if (origDist < 5f) return;
			int debugStage = 0;
			try
			{
				debugStage = 100;

				List<MapItem> roadsWithTransport = GetCurrentValidRoads(currentCell, transMethod);
				if (roadsWithTransport == null || roadsWithTransport.Count == 0) return;
				roadsWithTransport.Sort((a, b) => (a.CenterPoint - start).sqrMagnitude.CompareTo((b.CenterPoint - start).sqrMagnitude));
				MapItem currentRoad = roadsWithTransport.GetFirstOrDefaultWhere(r => r.Type.LanesForTransportMethod.GetListForOrNull(transMethod)?.Exists(l => !l.IsDeadEnd && l.LanePoints.Count > 2) ?? false);
				debugStage = 130;
				List<LanePrefab> transportationList = currentRoad.Type.LanesForTransportMethod.GetListForOrNull(transMethod);
				if (transportationList == null || transportationList.Count <= 1) return;

				LanePrefab curLanePrefab = transportationList.GetRandom(rand);

				//might be able to do it programatically; shift the last two points 4 units to local-right and the 3rd 2 units
				debugStage = 300;

				//waypoints.Add(currentRoad.CenterPoint);

				workingStack.Clear();
                workingVisited.Clear();
                workingStack.Add(new MapPathingNode()
				{
					cell = currentCell,
					parentNode = null,
					road = currentRoad,
					endPos = currentRoad.CenterPoint,
					distance = origDist
				});

                MapPathingNode activeNode = null;
				while ( workingStack.Count > 0)
				{
					activeNode = workingStack.RemoveAtAndReturn(0);
					if ( workingVisited.Contains(activeNode)) continue;
                    workingVisited.Add(activeNode);

					if (activeNode.distance < .5f) break; //close enough

					foreach (MapItem newRoad in GetConnections(activeNode.road, roadsWithTransport, transMethod))
					{
						float dist = (newRoad.CenterPoint - end).sqrMagnitude;
                        MapPathingNode newNode = new MapPathingNode()
						{
							cell = currentCell,
							parentNode = activeNode,
							road = newRoad,
							endPos = newRoad.CenterPoint,
							distance = dist
						};
						if ( workingVisited.Contains(newNode)) continue;

                        workingStack.AddIfNotAlreadyIn(newNode);
					}

                    workingStack.Sort((a, b) => a.distance.CompareTo(b.distance));
				}
				debugStage = 100000;
                workingVisited.Sort((a, b) => a.distance.CompareTo(b.distance));
				activeNode = workingVisited[0];
				while (activeNode != null)
				{
					if (activeNode.parentNode != null && activeNode.parentNode.distance > activeNode.distance)
					{
						waypoints.Add(activeNode.road.CenterPoint);
					}
					activeNode = activeNode.parentNode;
				}
			}
			catch (Exception e)
			{
				ArcenDebugging.LogDebugStageWithStack($"Find path from {start} to {end} CityVehicleed a problem.", debugStage, e, Verbosity.ShowAsError);
			}
		}

		private static void AddPointWithRadius(List<Vector3> Waypoints, MapPathingNode ActiveNode, float TurnRadius, float speed)
		{
			
			Vector3 angularPoint = ActiveNode.road.CenterPoint;
			Vector3 CurrentRoadCenterPoint = ActiveNode.parentNode.road.CenterPoint;

			if (Waypoints.Count == 0 || TurnRadius <= float.Epsilon)
			{
				Waypoints.Add(angularPoint);
				return;
			}

			AddPointWithRadius(Waypoints, angularPoint, CurrentRoadCenterPoint, TurnRadius, speed);
		}
		
		private static void AddPointWithRadius(List<Vector3> Waypoints, Vector3 angularPoint, Vector3 CurrentRoadCenterPoint, float TurnRadius, float speed)
		{
			if (Waypoints.Count == 0 || TurnRadius <= float.Epsilon)
			{
				Waypoints.Add(angularPoint);
				return;
			}

			Vector3 p1 = Waypoints.LastOrDefault;
			Vector3 p2 = CurrentRoadCenterPoint;

			//Vector 1
			float dx1 = angularPoint.x - p1.x;
			float dy1 = angularPoint.z - p1.z;

			//Vector 2
			float dx2 = angularPoint.x - p2.x;
			float dy2 = angularPoint.z - p2.z;

			//Angle between vector 1 and vector 2 divided by 2
			float angle = (Mathf.Atan2(dy1, dx1) - Mathf.Atan2(dy2, dx2)) / 2;

			// The length of segment between angular point and the
			// points of intersection with the circle of a given radius
			float tan = MathA.Abs(Mathf.Tan(angle));
			float segment = TurnRadius / tan;

			//Check the segment
			float length1 = GetLength(dx1, dy1);
			float length2 = GetLength(dx2, dy2);

			float length = MathA.Min(length1, length2);

			if (segment > length)
			{
				if (segment > length1)
				{
					Waypoints.RemoveAt(Waypoints.Count - 1);
					AddPointWithRadius(Waypoints, angularPoint, CurrentRoadCenterPoint, TurnRadius, speed);
				}
				else
					Waypoints.Add(angularPoint);
				return;
			}

			// Points of intersection are calculated by the proportion between 
			// the coordinates of the vector, length of vector and the length of the segment.
			Vector3 p1Cross = GetProportionPoint(angularPoint, segment, length1, dx1, dy1);
			Vector3 p2Cross = GetProportionPoint(angularPoint, segment, length2, dx2, dy2);

			// Calculation of the coordinates of the circle 
			// center by the addition of angular vectors.
			float dx = angularPoint.x * 2 - p1Cross.x - p2Cross.x;
			float dy = angularPoint.z * 2 - p1Cross.z - p2Cross.z;

			float L = GetLength(dx, dy);
			float d = GetLength(segment, TurnRadius);

			if (L < 0.1f)
			{
				Waypoints.Add(angularPoint);
				return;
			}

			Vector3 circlePoint = GetProportionPoint(angularPoint, d, L, dx, dy);

			//StartAngle and EndAngle of arc
			float startAngle = Mathf.Atan2(p1Cross.z - circlePoint.z, p1Cross.x - circlePoint.x);
			float endAngle = Mathf.Atan2(p2Cross.z - circlePoint.z, p2Cross.x - circlePoint.x);

			//Sweep angle
			float sweepAngle = endAngle - startAngle;

			//Some additional checks
			/*if (sweepAngle < 0)
			{
				startAngle = endAngle;
				sweepAngle = -sweepAngle;
				reverse = true;
			}*/

			if (sweepAngle > Mathf.PI)
				sweepAngle  = - (2 * Mathf.PI - sweepAngle);

			if (sweepAngle < -Mathf.PI)
				sweepAngle = -(2 * -Mathf.PI - sweepAngle);

			if (float.IsNaN(sweepAngle))
			{
				ArcenDebugging.LogSingleLine($"sweepAngle was NaN: {L}, {d}, {TurnRadius}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
				Waypoints.Add(angularPoint);
				return;
			}
			
			float degreeFactor = 9 / Mathf.PI;

			int pointsCount = (int)MathA.Abs(sweepAngle * degreeFactor);
			
			int sign = Math.Sign(sweepAngle);

			if (pointsCount < 1)
			{
				pointsCount = 1;
			}

			Vector3[] points = new Vector3[pointsCount];

			for (int i = 0; i < pointsCount; ++i)
			{
				float pointX =
					(circlePoint.x
					        + Mathf.Cos(startAngle + sign * i / degreeFactor)
					        * TurnRadius);

				float pointY =
					(circlePoint.z
					        + Mathf.Sin(startAngle + sign * i / degreeFactor)
					        * TurnRadius);

				points[i] = new Vector3(pointX, 0, pointY);
			}
			
			Waypoints.AddRange(points);
		}

		private static float GetLength(float dx, float dy)
		{
			return Mathf.Sqrt(dx * dx + dy * dy);
		}

		private static Vector3 GetProportionPoint(Vector3 point, float segment, float length, float dx, float dy)
		{
			float factor = segment / length;

			return new Vector3(point.x - dx * factor, 0, point.z - dy * factor);
		}

		private static void ReverseSection(List<Vector3> Waypoints, int OrigCount)
		{
			int len = Waypoints.Count;
			for (int i = OrigCount; i < Waypoints.Count / 2; i++)
			{
				// ReSharper disable once - SwapViaDeconstruction
				Vector3 tmp = Waypoints[i];
				Waypoints[i] = Waypoints[len - (i - OrigCount) - 1];
				Waypoints[len - (i - OrigCount) - 1] = tmp;
			}
		}

		private static System.Collections.Generic.IEnumerable<MapItem> GetConnections(MapItem currentRoad, List<MapItem> roadsWithTransport, TransportMethod transMethod)
		{
            List<LanePrefab> list = currentRoad.Type.LanesForTransportMethod.GetListForOrNull( transMethod );

            float laneWidth = list == null || list.Count <= 0 ? 0 : list[0].Type.LaneWidth * currentRoad.Scale.y;
			foreach (MapItem road in roadsWithTransport)
			{
				if (road == currentRoad)
				{
					continue;
				}

				if (currentRoad.GetRoughIsFurtherAwayThan(road, 3f))
				{
					continue;
				}
				bool res = HaveAnyLanesThatOverlap(currentRoad, road, transMethod, laneWidth*2);
				if (res) yield return road;
			}
		}

		private static bool HaveAnyLanesThatOverlap(MapItem roadA, MapItem roadB, TransportMethod transMethod, float wid)
		{
            List<LanePrefab> listRoadA = roadA.Type.LanesForTransportMethod.GetListForOrNull( transMethod );
            if ( listRoadA == null || listRoadA.Count <= 0 )
                return false;

            List<LanePrefab> listRoadB = roadB.Type.LanesForTransportMethod.GetListForOrNull( transMethod );
			if ( listRoadB == null || listRoadB.Count <= 0 )
				return false;

            foreach (LanePrefab laneA in listRoadA )
			{
				foreach (LanePrefab laneB in listRoadB )
				{
					Vector3 point1 = roadA.TransformPoint_Threadsafe(laneA.FirstPoint);
					Vector3 point2 = roadB.TransformPoint_Threadsafe(laneB.FirstPoint);
					if ((point1 - point2).magnitude < wid)
						return true;
					Vector3 point3 = roadA.TransformPoint_Threadsafe(laneA.LastPoint);
					if ((point3 - point2).magnitude < wid)
						return true;
					Vector3 point4 = roadB.TransformPoint_Threadsafe(laneB.LastPoint);
					if ((point1 - point4).magnitude < wid)
						return true;
					if ((point3 - point4).magnitude < wid)
						return true;
				}
			}

			return false;
		}

		public static void GetPathFromTo(Vector3 start, Vector3 end, RandomGenerator rand, TransportMethod transMethod, 
			List<Waypoint> waypoints, MapCell cellIn, List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList,
            List<MapPathingNode> workingOpenSet, List<MapPathingNode> workingClosedSet )
		{
			int debugStage = 0;
			try
			{
				debugStage = 100;
				MapCell currentCell = CityMap.TryGetExistingCellAtLocation(new ArcenGroundPoint((int)start.x, (int)start.z));
				if(currentCell != cellIn)
				{
					currentCell = cellIn;
				}
				debugStage = 110;
				List<MapItem> validRoads = GetCurrentValidRoads(currentCell, transMethod);
				debugStage = 120;
				if (validRoads == null || validRoads.Count == 0) return;
				MapItem currentRoad = validRoads.GetRandom(rand);
				debugStage = 130;

                List<LanePrefab> roadLaneList = currentRoad.Type.LanesForTransportMethod.GetListForOrNull( transMethod );
                if ( roadLaneList == null || roadLaneList.Count <= 1) return;
				
				Vector3 prevPoint = Vector3.negativeInfinity;
				List<MapItem> roadsWithTransport = null;
				debugStage = 200;
				LanePrefab curLanePrefab = roadLaneList.GetRandom(rand);
				if (curLanePrefab.LanePoints.Count < 2)
				{
					ArcenDebugging.LogWithStack($"Lane prefab `{curLanePrefab.ID}` did not have any points!", Verbosity.DoNotShow);
					return;
				}
				//can't reverse a parking lane easily
				if (curLanePrefab.IsDeadEnd) return;
				//might be able to do it programatically; shift the last two points 4 units to local-right and the 3rd 2 units
				Vector3 lastTransformedPoint = Vector3.negativeInfinity;
				debugStage = 300;

				foreach (Vector3 p in curLanePrefab.LanePoints )
				{
					Vector3 tp = currentRoad.TransformPoint_Threadsafe(p);
					waypoints.Add(tp);
					lastTransformedPoint = tp;
				}

				AdjustForRighthandedness(currentRoad, curLanePrefab, waypoints, transMethod, GameSettings.Current.GetBool("MainGameCarsDriveOnTheRight"));
				lastTransformedPoint = waypoints[waypoints.Count - 1];
				workingOpenSet.Clear();
				workingClosedSet.Clear();
                MapPathingNode goalFinal = null;
                MapPathingNode prevNode = null;
				debugStage = 400;
				do
				{
					debugStage += 1;
					float laneWidth = curLanePrefab.Type.LaneWidth * currentRoad.Scale.y;
					currentCell = GetNextCell(transMethod, currentCell, ref roadsWithTransport, lastTransformedPoint, laneWidth, 
						currentCell.CellBounds_Min, currentCell.CellBounds_Max );
					if (currentCell == null) break;
					//debugStage = 400;
					roadsWithTransport = GetCurrentValidRoads(currentCell, transMethod);
					if (roadsWithTransport == null || roadsWithTransport.Count == 0 || (roadsWithTransport.Count == 1 && roadsWithTransport[0] == currentRoad))
						break;

					//debugStage = 500;
					//int index = -1;
					Vector3 pos = Vector3.negativeInfinity;
					foreach (MapItem road in roadsWithTransport)
					{
						if (road == currentRoad) continue;
						if ( workingOpenSet.GetFirstOrDefaultWhere(x => x.road == road) != null) continue;
						if (currentRoad.GetRoughIsFurtherAwayThan(road, 5)) continue;
						if (road.GetBasicDistanceToPointXZ( lastTransformedPoint, laneWidth) > road.Diameter + laneWidth) continue;
						GetNearbyLanes(road, transMethod, lastTransformedPoint, laneWidth, true, workingLaneList );
						foreach ((LanePrefab lane, int index, Vector3 tPos) l in workingLaneList )
						{
							int ind = l.lane.LanePoints.Count - l.index - 1;
							Vector3 lastPos = road.TransformPoint_Threadsafe(l.lane.LanePoints[ind]);
							float dist = /*(start - l.tPos).sqrMagnitude + */(end - lastPos).sqrMagnitude;

                            MapPathingNode isOpen = workingOpenSet.Count == 0 ? null : workingOpenSet.GetFirstOrDefaultWhere(x => x.road == road && x.lane.ID == l.lane.ID);

							if (isOpen != null)
							{
								continue;
							}
                            MapPathingNode isClosed = workingClosedSet.Count == 0 ? null : workingClosedSet.GetFirstOrDefaultWhere(x => x.road == road && x.lane.ID == l.lane.ID) ;
							if (isClosed != null)
							{
								continue;
							}
                            MapPathingNode next = new MapPathingNode()
							{
								cell = currentCell,
								parentNode = prevNode,
								road = road,
								lane = l.lane,
								index = l.index,
								startPos = l.tPos,
								endPos = lastPos,
								distance = dist
							};
                            workingOpenSet.Add(next);
						}
					}
					if ( workingOpenSet.Count == 0) break;
                    workingOpenSet.Sort((a, b) => a.distance.CompareTo(b.distance));
					prevNode = goalFinal;
					goalFinal = workingOpenSet.RemoveAtAndReturn(0);
                    workingClosedSet.Add(goalFinal);
					currentRoad = goalFinal.road;
					curLanePrefab = goalFinal.lane;
					lastTransformedPoint = goalFinal.endPos;
					currentCell = goalFinal.cell;
					debugStage += 1000;
					if (goalFinal.distance < 2.5 && workingOpenSet.Count == 0)
					{
						break;
					}
				} while ( workingOpenSet.Count > 0);
				debugStage = 100000;
				if (goalFinal == null) return;
				while (goalFinal != null && goalFinal.parentNode != null)
				{
					int traverseDir = goalFinal.index == 0 ? 1 : -1;
					for (int j = goalFinal.index + traverseDir; j >= 0 && j < goalFinal.lane.LanePoints.Count; j += traverseDir)
					{
						debugStage += 1;
						Vector3 tp = goalFinal.road.TransformPoint_Threadsafe(goalFinal.lane.LanePoints[j]);
						waypoints.Add(tp);
					}
					goalFinal = goalFinal.parentNode;
				}
			}
			catch (Exception e)
			{
				ArcenDebugging.LogDebugStageWithStack($"Find path from {start} to {end} CityVehicled a problem.", debugStage, e, Verbosity.ShowAsError);
			}
		}
	}
}

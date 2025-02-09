using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public static class CityVehiclesHelper
    {
	    #region Delivery drones
	    public static void CivilianFlier(CityVehicleType Type, CityVehicleMoveFromBuildingToBuilding movementLogic, MapItem origin, MapItem destination, MersenneTwister RandForThisTurn)
		{
			ISimCityVehicle CityVehicle = World.CityVehicles.CreateNewCityVehicle(Type,
				origin.OBBCache.TopCenter,
				RandForThisTurn);

            List<Vector3> pathToSet = CityVehicle.GetPathListToAlter_FromSimThreadOnly();
            pathToSet.Clear();
            pathToSet.Add( origin.OBBCache.TopCenter );

			Vector3 targetPoint = destination.OBBCache.TopCenter;
			if ( movementLogic.ShouldLaunchPodCountAtTargetOnArrival > 0 )
			{
				CityVehicle.PointToFirePodsAtWhenGettingNewPath = targetPoint;
				CityVehicle.HasPointToFirePodsAtWhenGettingNewPath = true;
            }

            if ( movementLogic.ShouldGoToTwiceTheHeightOfTargetBuilding )
				targetPoint.y *= 2;

            if ( movementLogic.ShouldFlyInArcs )
                AddArcPointsToPath( pathToSet, CityVehicle.GetVisWorldLocation(), targetPoint, 1.5f, 5f, 7f, 3 );

            pathToSet.Add( targetPoint );

            CityVehicle.SetFuncCallForNewPath( GetNewFlightPath );
        }

	    private static void GetNewFlightPath(ISimCityVehicle CityVehicle, MersenneTwister Rand)
        {
            CityVehicleType vehicleType = CityVehicle.GetCityVehicleType();
			if ( vehicleType == null )
				return;
            CityVehicleMoveFromBuildingToBuilding movementLogic = vehicleType.MoveFromBuildingToBuilding;
			if ( movementLogic == null ) 
				return;

            ISimBuilding destBuilding = GetDestinationBuildingOrNull( vehicleType, null, true,
                movementLogic.MinDistanceOfDestinationFromSpawn, movementLogic.MaxDistanceOfDestinationFromSpawn,
                CityVehicle.GetVisWorldLocation(), Rand );
		    if (destBuilding == null) return;

            List<Vector3> pathToSet = CityVehicle.GetPathListToAlter_FromSimThreadOnly();
			pathToSet.Clear();
			pathToSet.Add( CityVehicle.GetVisWorldLocation() );

            Vector3 targetPoint = destBuilding.GetMapItem().OBBCache.TopCenter;

			bool drawPods = true;
			if ( InputCaching.SkipDrawing_SmallFliers && vehicleType.IsSmallFlier )
				drawPods = false;
            if ( InputCaching.SkipDrawing_StreetVehicles && vehicleType.IsStreetVehicle )
                drawPods = false;

            int podCountToFire = movementLogic.ShouldLaunchPodCountAtTargetOnArrival;
			if ( podCountToFire > 0 && drawPods )
            {
				MapCell cell = CityVehicle.GetVisCurrentMapCell();
				//only do this when there's city life to be had
                if ( CityVehicle.HasPointToFirePodsAtWhenGettingNewPath && cell != null && cell.ShouldHaveCityLifeRightNow )
				{
					Vector3 originForPods = CityVehicle.GetVisWorldLocation();
					originForPods.y += vehicleType.VisObjectExtraOffset;

                    float baseDegrees = 360f / podCountToFire;
					Vector3 podTarget = CityVehicle.PointToFirePodsAtWhenGettingNewPath;
                    
					for ( int i = 0; i < podCountToFire; i++ )
					{
                        AngleDegrees deg = AngleDegrees.Create( i * baseDegrees + (Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) * 0.2f - 0.1f) );
                        ArcenGroundPoint p = MathA.GetPointFromCircleCenter( new ArcenGroundPoint( 0, 0 ), 200, deg );

                        BreachingPodRequest request;
						request.OriginForPod = originForPods;
						request.PodTarget = podTarget;
                        request.Rotation = Quaternion.LookRotation( new Vector3( p.X, 0, p.Z ).RotateAroundPivot( Vector3.up, originForPods ), Vector3.up );

                        CityLifeVis.GameSim.BreachingPodQueue.Enqueue( request );
                    }
					if ( SoundBroadAmbientCoordinator.EnableDeliveryCraftSounds )
						ParticleSoundRefs.DeliveryCraftDeliver.DuringGame_PlayAtLocation( originForPods );
                }

                CityVehicle.PointToFirePodsAtWhenGettingNewPath = targetPoint;
                CityVehicle.HasPointToFirePodsAtWhenGettingNewPath = true;
            }

            if ( movementLogic.ShouldGoToTwiceTheHeightOfTargetBuilding )
                targetPoint.y *= 2;

			if ( movementLogic.ShouldFlyInArcs )
				AddArcPointsToPath( pathToSet, CityVehicle.GetVisWorldLocation(), targetPoint, 1.5f, 5f, 7f, 7 );

            pathToSet.Add( targetPoint );
        }

		private static void AddArcPointsToPath( List<Vector3> pathToSet, Vector3 start, Vector3 end, float HeightMultiplier, 
			float MinHeightToAdd, float MaxHeightToAdd, int NumberOfIntermediatePointsToAdd )
		{
			float initialMax = MathA.Max( start.y, end.y );
            float targetY = MathA.Max( start.y, end.y ) * HeightMultiplier;
			if ( targetY < MinHeightToAdd )
				targetY = MinHeightToAdd;
			if ( targetY > MaxHeightToAdd )
				targetY = MaxHeightToAdd;
			if ( targetY < initialMax + 1f )
				targetY = initialMax + 1f;

			float lerpAmountPer = 1f / ( NumberOfIntermediatePointsToAdd + 2 );

			for ( int i = 1; i <= NumberOfIntermediatePointsToAdd; i++ )
			{
				float lerpAmount = lerpAmountPer * i;
                Vector3 pos = Vector3.Lerp( start, end, lerpAmount );
				if ( lerpAmount <= 0.33f ) //going up to midpoint
					pos.y += (targetY - start.y) * (lerpAmount + lerpAmount + lerpAmount);
				else if ( lerpAmount <= 0.66f ) //cruising altitude
                    pos.y = targetY;
                else //going down from midpoint
				{
					float lerpSecondHalf = lerpAmount - 0.66f;
                    pos.y += (targetY - end.y) * (1 - (lerpSecondHalf + lerpSecondHalf + lerpSecondHalf));
				}

                pathToSet.Add( pos );
            }
		}
	    #endregion

		#region SetRoadWander
		public static ISimCityVehicle SetRoadWander(CityVehicleType Type, CityVehicleMovementBase movementLogic, Vector3 nearbySpot, MersenneTwister RandForThisTurn)
        {
			ISimCityVehicle cityVehicle = World.CityVehicles.CreateNewCityVehicle( Type,
				nearbySpot, RandForThisTurn );

			List<Vector3> pathToSet = cityVehicle.GetPathListToAlter_FromSimThreadOnly();
			pathToSet.Clear(); //Chris added; seems important
            MapCell cell = CityMap.ActiveSimulationMapCells.GetRandom(RandForThisTurn);
			StreetPathfinder.FillCityVehiclePath( RandForThisTurn, pathToSet, cell, movementLogic.TurnRadius, movementLogic.MovementSpeed,
                cityVehicle.GetWorkingLaneList() );

            cityVehicle.SetSimAndVisWorldLocation( pathToSet[0] );
            cityVehicle.SetRotation();

            if (!movementLogic.ShouldDespawnWhenPathCompletes)
            {
                cityVehicle.SetFuncCallForNewPath((e, r) =>
	            {
		            StreetPathfinder.FillCityVehiclePath(r, e.GetPathListToAlter_FromSimThreadOnly(), e.GetVisCurrentMapCell(), e.GetVisWorldLocation(), 
						movementLogic.TurnRadius, movementLogic.MovementSpeed,
						cityVehicle.GetWorkingLaneList() );
	            });
            }

            return cityVehicle;
        }
		#endregion

		#region Pathing along roads (basic)

		public static void PathAToB(CityVehicleType Type, CityVehicleMoveFromBuildingToBuilding movementLogic, MapItem origin, 
			MapItem destination, MersenneTwister RandForThisTurn,
            List<MapPathingNode> workingStack, List<MapPathingNode> workingVisited )
		{
			if (origin == null || destination == null) return;

			int debugStage = 0;
			try
			{
				ISimCityVehicle CityVehicle = World.CityVehicles.CreateNewCityVehicle(Type,
					origin.CenterPoint,
					RandForThisTurn);
				debugStage = 100;

				List<Vector3> pathToSet = CityVehicle.GetPathListToAlter_FromSimThreadOnly();
				pathToSet.Clear(); //Chris added.  Seems important
                debugStage = 200;
				MapCell cell = CityMap.ActiveSimulationMapCells.GetRandom(RandForThisTurn);
				debugStage = 300;

				StreetPathfinder.GetMilitaryPath(origin, destination, pathToSet, movementLogic.TurnRadius, movementLogic.MovementSpeed, RandForThisTurn,
					workingStack, workingVisited);

				debugStage = 10000;
				if (pathToSet.Count < 1) return;
				CityVehicle.SetSimAndVisWorldLocation(pathToSet[0]);
				debugStage = 10100;

				#region VisDebugging

				float finDist = (destination.CenterPoint - pathToSet[pathToSet.Count - 1]).magnitude;

				if (!movementLogic.ShouldDespawnWhenPathCompletes)
				{
					CityVehicle.SetFuncCallForNewPath((e, r) =>
					{
						CityVehicleMoveFromBuildingToBuilding mv = e.GetCityVehicleType().GetMovementType() as CityVehicleMoveFromBuildingToBuilding;
						MapItem newOrigin = destination;
						//oh god, using outer scope variable for persistence
						destination = GetDestinationBuildingOrNull(e.GetCityVehicleType(), mv, newOrigin.SimBuilding, r).GetMapItem();
						StreetPathfinder.GetMilitaryPath(newOrigin, destination, pathToSet, mv.TurnRadius, mv.MovementSpeed, r,
							workingStack, workingVisited );
					});
				}

				if (!(finDist > 3) || !VisDebuggingCommon.DoDebug_Pathfinding) return;

				float angle = 0;
				const float angleDelta = (float)(5 * Math.PI / 180);
				Vector3 last = origin.CenterPoint;
				foreach (Vector3 vec in pathToSet)
				{
					Color col = Color.HSVToRGB(angle % 1, 1, 1);
					angle += angleDelta;
					VisDebuggingCommon.Debug_DrawCheckedPoint(vec, col);
					VisDebuggingCommon.Debug_DrawLine(last, vec, col);
					last = vec;
				}

				VisDebuggingCommon.Debug_DrawLine(last, destination.CenterPoint, Color.white);
				//if (VisDebuggingCommon.OnlyDrawTheOnce)
				//	VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( cell.ParentTile.CalculateTileCenter(), false );
				//VisDebuggingCommon.OnlyDrawTheOnce = false;

				#endregion
			}
			catch (Exception e)
			{
				ArcenDebugging.LogDebugStageWithStack("CityVehicle Helper SetRoadWander failed!", debugStage, e, Verbosity.ShowAsError);
			}
		}
		#endregion

		#region SetParkWander
		public static void SetParkWander(CityVehicleType Type, Vector3 nearbySpot, MersenneTwister RandForThisTurn)
        {
			Vector3 pos = GetNearbyPath(nearbySpot, RandForThisTurn, out MapPathingPoint pathPoint);
			ISimCityVehicle CityVehicle = World.CityVehicles.CreateNewCityVehicle( Type,
				pos, RandForThisTurn);

			List<Vector3> pathToSet = CityVehicle.GetPathListToAlter_FromSimThreadOnly();
			pathToSet.Clear(); //Chris added.  Seems important
            //ArcenDebugging.LogSingleLine("Bleep", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
            BuildAPath(pathPoint, pathToSet, RandForThisTurn);
		}
        #endregion

        #region BuildAPath
        public static void BuildAPath(MapPathingPoint PathPoint, List<Vector3> PathToSet, MersenneTwister RandForThisTurn)
        {
			PathToSet.Clear(); //Chris added.  Seems important

            if ( PathPoint == null )
				return;

	        float totalPathLength = 0;
	        while (totalPathLength < 10)
	        {
		        MapPathingPoint newPoint = PathPoint.LinkedPoints.GetRandom(RandForThisTurn);
				if ( newPoint == null )
					break;
				
		        //avoid turning around if possible
		        while (PathPoint.LinkedPoints.Count > 1 && newPoint == PathPoint)
		        {
			        newPoint = PathPoint.LinkedPoints.Count == 2
					        ? PathPoint.LinkedPoints[1 - PathPoint.LinkedPoints.IndexOf(newPoint)] //if there's only two, just get the other one
					        : PathPoint.LinkedPoints.GetRandom(RandForThisTurn);
		        }

		        totalPathLength += (PathPoint.Position - newPoint.Position).magnitude;

				PathToSet.Add(newPoint.Position );
				PathPoint = newPoint;
	        }
		}
        #endregion

        #region IsTooCloseToOthers
        public static bool IsTooCloseToOthers(Vector3 Pos)
        {
	        bool result = false;
            ListView<ISimCityVehicle> CityVehicles = World.CityVehicles.GetAllCityVehicles();
			foreach ( ISimCityVehicle vehicle in CityVehicles )
            {
                float d = (vehicle.GetVisWorldLocation() - Pos).sqrMagnitude;
                if ( d > 10 )
                    continue;
                result = true;
            }

            return result;
        }
        #endregion

        #region GetNearbyPath
        public static Vector3 GetNearbyPath(Vector3 NearbySpot, MersenneTwister RandForThisTurn, out MapPathingPoint pointOut)
        {
			Vector3 result = NearbySpot.PlusX(RandForThisTurn.Next(-10, 10)).PlusZ(RandForThisTurn.Next(-10, 10));
            MapPathingPoint point = null;
            foreach ( MapCell cell in CityMap.ActiveSimulationMapCells.GetRandomStartEnumerable( RandForThisTurn ) )
			{
				if ((cell.ParentTile.PathingPoints?.Count ?? 0) == 0) 
					continue;
				point = cell.ParentTile.PathingPoints.GetRandom(RandForThisTurn);
				result = point.Position;
                break;
			}
			pointOut = point;
            return result;
		}
        #endregion

        #region GetSpawnDroneDeliveryBuildingOrNull_Anywhere
        public static ISimBuilding GetSpawnDroneDeliveryBuildingOrNull_Anywhere( RandomGenerator RandForThisTurn )
        {
            List<ISimBuilding> buildingList = CityMap.AllDroneDeliveryBuildings.GetDisplayList();
            if ( buildingList.Count == 0 )
                return null;
            return buildingList.GetRandom( RandForThisTurn );
        }
        #endregion

        #region GetSpawnBuildingOrNull_Anywhere
        public static ISimBuilding GetSpawnBuildingOrNull_Anywhere( BuildingTag Tag, RandomGenerator RandForThisTurn )
        {
			if ( Tag == null )
				return null;

            List<ISimBuilding> buildingsWithTag = Tag.DuringGame_Buildings.GetDisplayList();
            if ( buildingsWithTag.Count == 0 ) 
				return null;
			return buildingsWithTag.GetRandom( RandForThisTurn );
        }
        #endregion

        #region GetSpawnBuildingOrNull_Nearby_Drone
        public static ISimBuilding GetSpawnBuildingOrNull_Nearby_Drone( bool MustBeExplored, RandomGenerator RandForThisTurn )
        {
            List<ISimBuilding> buildingList = MustBeExplored ? 
				CityMap.NearbyVisibleDroneDeliveryBuildings.GetDisplayList() :
                CityMap.NearbyAllDroneDeliveryBuildings.GetDisplayList();

            if ( buildingList == null || buildingList.Count == 0 )
                return null;

			int startingRandomIndex = RandForThisTurn.Next( 0, buildingList.Count );
			for ( int i = startingRandomIndex; i < buildingList.Count; i++ )
			{
				ISimBuilding bld = buildingList[i];
				if ( bld != null ) //&& bld.GetClaimedByCityVehicle() == null )
					return bld;
            }

            for ( int i = 0; i < startingRandomIndex; i++ )
            {
                ISimBuilding bld = buildingList[i];
                if ( bld != null ) //&& bld.GetClaimedByCityVehicle() == null )
                    return bld;
            }

            return null;
        }
        #endregion

        #region GetSpawnBuildingOrNull_Nearby_Any
        public static ISimBuilding GetSpawnBuildingOrNull_Nearby_Any( bool MustBeExplored, RandomGenerator RandForThisTurn )
        {
            List<ISimBuilding> buildingList = MustBeExplored ?
                CityMap.NearbyVisibleBuildings.GetDisplayList() :
                CityMap.NearbyAllBuildings.GetDisplayList();

            if ( buildingList == null || buildingList.Count == 0 )
                return null;

            int startingRandomIndex = RandForThisTurn.Next( 0, buildingList.Count );
            for ( int i = startingRandomIndex; i < buildingList.Count; i++ )
            {
                ISimBuilding bld = buildingList[i];
                if ( bld != null ) //&& bld.GetClaimedByCityVehicle() == null )
                    return bld;
            }

            for ( int i = 0; i < startingRandomIndex; i++ )
            {
                ISimBuilding bld = buildingList[i];
                if ( bld != null ) //&& bld.GetClaimedByCityVehicle() == null )
                    return bld;
            }

            return null;
        }
        #endregion

        #region GetSpawnSpotOrNull_Nearby
        public static MapOutdoorSpot GetSpawnSpotOrNull_Nearby( bool MustBeExplored, RandomGenerator RandForThisTurn )
        {
            List<MapOutdoorSpot> CityVehicleSpotList = MustBeExplored ?
                CityMap.NearbyNonFogOfWarOutdoorSpots.GetDisplayList() :
                CityMap.NearbyAllOutdoorSpots.GetDisplayList();

            if ( CityVehicleSpotList == null || CityVehicleSpotList.Count == 0 )
                return null;

            int startingRandomIndex = RandForThisTurn.Next( 0, CityVehicleSpotList.Count );
            for ( int i = startingRandomIndex; i < CityVehicleSpotList.Count; i++ )
            {
                MapOutdoorSpot spot = CityVehicleSpotList[i];
                if ( spot != null ) //&& spot.CurrentlyClaimedByCityVehicle.GetRefOrNull() == null )
                    return spot;
            }

            for ( int i = 0; i < startingRandomIndex; i++ )
            {
                MapOutdoorSpot spot = CityVehicleSpotList[i];
                if ( spot != null ) //&& spot.CurrentlyClaimedByCityVehicle.GetRefOrNull() == null )
                    return spot;
            }

            return null;
        }
        #endregion

        #region GetDestinationBuildingOrNull
        public static ISimBuilding GetDestinationBuildingOrNull(CityVehicleType TypeOrNull, CityVehicleMoveFromBuildingToBuilding movementLogic, ISimBuilding SpawnBuilding, RandomGenerator RandForThisTurn)
        {
	        if (SpawnBuilding == null)
		        return null; //can't find a destination if we have no spawn source!
			
	        Vector3 spawnPoint = SpawnBuilding.GetMapItem().CenterPoint;
	        return GetDestinationBuildingOrNull(TypeOrNull, null, true, 
				movementLogic.MinDistanceOfDestinationFromSpawn, movementLogic.MaxDistanceOfDestinationFromSpawn,
                spawnPoint, RandForThisTurn, SpawnBuilding);
        }

        public static ISimBuilding GetDestinationBuildingOrNull(CityVehicleType TypeOrNull, BuildingTag TargetBuildingTagOrNull, bool UseDroneDeliveryTargets, float requiredDistance, 
			float maxDistance, Vector3 spawnPoint, RandomGenerator RandForThisTurn, ISimBuilding SpawnBuilding=null)
		{
			ISimBuilding dest = null;
			int debugStage = 0;
			try
			{
                List<ISimBuilding> buildingList = (UseDroneDeliveryTargets ? CityMap.AllDroneDeliveryBuildings.GetDisplayList() : 
					TargetBuildingTagOrNull.DuringGame_Buildings.GetDisplayList());
				if (buildingList.Count == 0)
					return null;
				debugStage = 10;

                foreach ( ISimBuilding potential in buildingList.GetRandomStartEnumerable( RandForThisTurn ) )
				{
					if (potential == SpawnBuilding)
						continue; //don't even check going to ourselves
					debugStage = 20;

					Vector3 potentialPoint = potential.GetMapItem().CenterPoint;
					float checkDistX = potentialPoint.x - spawnPoint.x;
					if (checkDistX < 0) checkDistX = -checkDistX;

                    float checkDistZ = potentialPoint.z - spawnPoint.z;
                    if ( checkDistZ < 0 ) checkDistZ = -checkDistZ;

                    debugStage = 30;

                    if ( checkDistX > maxDistance || checkDistZ > maxDistance ) //it's too far x or z axis, yikes, skip this one
                    {
                        continue;
                    }

                    if ( checkDistX >= requiredDistance) //it's far enough on the x axis, awesome, use this one
					{
						dest = potential;
						break;
					}
					debugStage = 40;

					if ( checkDistZ >= requiredDistance) //it's far enough on the z axis, awesome, use this one
					{
						dest = potential;
						break;
					}
					debugStage = 50;

					//it wasn't far enough, keep looking
				}
			}
			catch (Exception ex)
			{
				ArcenDebugging.LogDebugStageWithStack("GetDestinationBuildingOrNull", debugStage,ex, Verbosity.ShowAsError);
			}

			return dest;
        }
        #endregion
    }
}

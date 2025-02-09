using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public static class CityVehiclesSpawner
    {
        //note: these are assuming that this always happens on the main thread, which for now it does
        private static List<MapPathingNode> workingStack = List<MapPathingNode>.Create_WillNeverBeGCed( 10, "CityVehiclesSpawner-workingStack" );
        private static List<MapPathingNode> workingVisited = List<MapPathingNode>.Create_WillNeverBeGCed( 10, "CityVehiclesSpawner-workingVisited" );

        #region SpawnFromRules
        public static void SpawnFromRules( CityVehicleType VehicleType, MersenneTwister RandForThisTurn )
        {
            if ( CityLifeVis.GameSim.DisableDeliveryDrones )
            {
                ListView<ISimCityVehicle> vehicles = World.CityVehicles.GetAllCityVehicles();
                if ( vehicles.Count > 0 )
                {
                    foreach ( ISimCityVehicle vehicle in vehicles )
                        vehicle.Disband( false );
                }
                return;
            }

            if ( VehicleType.SpawnsNearby )
                SpawnFromRules_Nearby( VehicleType, RandForThisTurn );
            else
                SpawnFromRules_GeneralCity( VehicleType, RandForThisTurn );
        }

        private static void SpawnFromRules_GeneralCity( CityVehicleType VehicleType, MersenneTwister RandForThisTurn)
        {
			if ( VehicleType.DesiredCountAtAllTimesInTheCity <= 0 )
				return;

			ListView<ISimCityVehicle> existingCityVehiclesForType = World.CityVehicles.GetCityVehiclesForType( VehicleType );
			if ( existingCityVehiclesForType.Count < VehicleType.DesiredCountAtAllTimesInTheCity)
            {
                int countToAdd = VehicleType.DesiredCountAtAllTimesInTheCity - existingCityVehiclesForType.Count; //handle it this way because the count will not update until the next tick
				for (int i = 0; i < countToAdd; i++)
				{
					CityVehicleMoveFromBuildingToBuilding movementLogic = VehicleType.MoveFromBuildingToBuilding;
					if ( movementLogic != null && movementLogic.IsActive)
                    {
                        ISimBuilding spawnBuilding = CityVehiclesHelper.GetSpawnDroneDeliveryBuildingOrNull_Anywhere( RandForThisTurn );
                        if ( spawnBuilding == null )
                            continue;
                        ISimBuilding destBuilding = CityVehiclesHelper.GetDestinationBuildingOrNull( VehicleType, movementLogic, spawnBuilding, RandForThisTurn );
                        if ( destBuilding == null )
                            continue;

						if ( movementLogic.UsesRoads )
                        {
                            CityVehiclesHelper.PathAToB( VehicleType, movementLogic, spawnBuilding.GetMapItem(), destBuilding.GetMapItem(), RandForThisTurn,
                                workingStack, workingVisited );
                        }
                        else
                        {
                            CityVehiclesHelper.CivilianFlier( VehicleType, movementLogic, spawnBuilding.GetMapItem(), destBuilding.GetMapItem(), RandForThisTurn );
                        }
                    }
                    else
                        ArcenDebugging.LogSingleLine( "SpawnFromRules_GeneralCity: Could not spawn '" + VehicleType.ID + 
                            "', because it had no valid movement logic!", Verbosity.ShowAsError );
				}
			}
		}

        #endregion

        #region SpawnFromRules_Nearby
        private static void SpawnFromRules_Nearby( CityVehicleType VehicleType, MersenneTwister RandForThisTurn )
        {
            if ( VehicleType.DesiredCountAtAllTimesInTheCity <= 0 )
                return;

            ListView<ISimCityVehicle> existingCityVehiclesForType = World.CityVehicles.GetCityVehiclesForType( VehicleType );
            if ( existingCityVehiclesForType.Count < VehicleType.DesiredCountAtAllTimesInTheCity )
            {
                Vector3 nearbySpot = CameraCurrent.CameraBodyPosition;

                int countToAdd = VehicleType.DesiredCountAtAllTimesInTheCity - existingCityVehiclesForType.Count; //handle it this way because the count will not update until the next tick
                for ( int i = 0; i < countToAdd; i++ )
                {
                    CityVehicleMoveAlongRoadsAtRandom moveAlongRoads = VehicleType.MoveAlongRoadsAtRandom;
                    CityVehicleMoveFromBuildingToBuilding movementLogic = VehicleType.MoveFromBuildingToBuilding;
					if ( moveAlongRoads != null && moveAlongRoads.IsActive)
                    {
                        //find a spot on a road at random and wander it, since we can move
                        ISimCityVehicle CityVehicle = CityVehiclesHelper.SetRoadWander( VehicleType, moveAlongRoads, nearbySpot, RandForThisTurn );
                    }
					else if (movementLogic != null && movementLogic.IsActive)
                    {
						ISimBuilding spawnBuilding = CityVehiclesHelper.GetSpawnBuildingOrNull_Nearby_Drone( false, RandForThisTurn);
	                    if (spawnBuilding == null)
		                    continue;
	                    ISimBuilding destBuilding = CityVehiclesHelper.GetDestinationBuildingOrNull( VehicleType, movementLogic, spawnBuilding, RandForThisTurn);
	                    if (destBuilding == null)
		                    continue;
						
	                    if (movementLogic.UsesRoads)
	                    {
		                    CityVehiclesHelper.PathAToB( VehicleType, movementLogic, spawnBuilding.GetMapItem(), destBuilding.GetMapItem(), RandForThisTurn,
                                workingStack, workingVisited );
	                    }
	                    else
	                    {
		                    CityVehiclesHelper.CivilianFlier( VehicleType, movementLogic, spawnBuilding.GetMapItem(), destBuilding.GetMapItem(), RandForThisTurn);
	                    }
                    }
					else
                    {
                        if ( VehicleType.SpawnOnOutdoorSpot )
                        {
                            MapOutdoorSpot mapEncSpot = CityVehiclesHelper.GetSpawnSpotOrNull_Nearby( VehicleType.SpawnOnlyOnExploredCells, RandForThisTurn );

                            if ( mapEncSpot != null )
                            {
                                ISimCityVehicle CityVehicle = World.CityVehicles.CreateNewCityVehicle( VehicleType, mapEncSpot.Position, RandForThisTurn );
                                CityVehicle.SetSimAndVisWorldLocation( mapEncSpot.Position );
                            }
                        }
                        else
                        {
                            ISimBuilding building = CityVehiclesHelper.GetSpawnBuildingOrNull_Nearby_Any( VehicleType.SpawnOnlyOnExploredCells, RandForThisTurn );

                            if ( building != null )
                            {
                                Vector3 buildingSpot = building.GetMapItem().OBBCache.TopCenter + Vector3.up;

                                ISimCityVehicle CityVehicle = World.CityVehicles.CreateNewCityVehicle( VehicleType, buildingSpot, RandForThisTurn );
                                CityVehicle.SetSimAndVisWorldLocation( buildingSpot );
                            }
                        }
                    }
                }
            }
        }
		#endregion
		
		public static Vector3 GetPositionNearBuilding(RandomGenerator rand, MapItem target)
		{
			//if (target == null) return Vector3.zero;
			Vector3 p = target?.CenterPoint ?? Vector3.zero;
			p.y = 0;

			float radius = 0.025f + target?.OBBCache?.GetExpensiveRadius()??1f;

			p = GetRandomPointNear(rand, p, radius);
			//ArcenDebugging.LogSingleLine($"From shootout; {p} - {target.CenterPoint}. Building OBB {target.OBBCache.OBB}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			return p;
		}

		public static Vector3 GetRandomPointNear(RandomGenerator rand, Vector3 pointIn, float radius=1)
		{
			if (rand == null) return pointIn;
			ArcenPoint np = MathA.GetRandomPointFromCircleCenter(rand, new ArcenPoint((int)(pointIn.x * 1000), (int)(pointIn.z * 1000)), (int)(radius * 1000) + 250, (int)(radius * 1000) + 750);
			Vector3 pointOut = new Vector3(np.X / 1000f, 0, np.Y / 1000f);
			return pointOut;
		}
    }
}

using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.External;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class CityLifeVis
	{
		public static readonly CityLifeVis GameSim = new CityLifeVis();

		private int curTrafficDensity = -1;
		//private int curPedestrianDensity = -1;

		public Int64 CellFills = 0;
		public Int64 PlanningThreadsRun = 0;

		public bool DisableStreetTraffic = false;
        public bool DisableDeliveryDrones = false;
        public bool DisableBackgroundGunfights = false;

        public void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
		{
		}

		private readonly Stopwatch perQuarterSecondStopwatch = new Stopwatch();

		private readonly ProtectedList<IStreetMob> AllMobs_BGThreadOnly = ProtectedList<IStreetMob>.Create_WillNeverBeGCed( 300, "CityLifeVis-AllMobs_BGThreadOnly", 300 );
		public readonly ConcurrentQueue<BreachingPodRequest> BreachingPodQueue = ConcurrentQueue<BreachingPodRequest>.Create_WillNeverBeGCed( "CityLifeVis-BreachingPodQueue" );

		#region DoPerQuarterSecond_BackgroundThread
		public void DoPerQuarterSecond_BackgroundThread( MersenneTwister Rand )
		{
			if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
			{
                BreachingPodQueue.Clear();
                return;
			}

			if ( !perQuarterSecondStopwatch.IsRunning ) //only start it if it is not already running, which could happen from a coroutine
			{
				perQuarterSecondStopwatch.Reset();
				perQuarterSecondStopwatch.Start();
			}

			#region Settings Pulling
			VisDebuggingCommon.DoDebug_HaultMovement = GameSettings.Current.GetBool( "Debug_PauseVehicles" );
			VisDebuggingCommon.DoDebug_Pathfinding = GameSettings.Current.GetBool( "Debug_ShowPathfinding" );
			VisDebuggingCommon.DoDebug_CityNetwork = GameSettings.Current.GetBool( "Debug_ShowRoadNetwork" );
			VisDebuggingCommon.Debug_ShowRoadDetails = GameSettings.Current.GetBool( "Debug_ShowRoadDetails" );
            curTrafficDensity = GameSettings.Current.GetInt( "TrafficDensity" );
            DisableStreetTraffic = GameSettings.Current.GetBool( "DisableStreetTraffic" );
            DisableDeliveryDrones = GameSettings.Current.GetBool( "DisableDeliveryDrones" );
            DisableBackgroundGunfights = GameSettings.Current.GetBool( "DisableBackgroundGunfights" );
			//curPedestrianDensity = GameSettings.Current.GetInt( "PedestrianDensity" );
			#endregion

			if ( DisableStreetTraffic )
				curTrafficDensity = 0;

            //do any cell population, which may early-out as it time slices things
            this.PerQuarterSecond_PopulateCells( Rand );

			SimTimingInfo.CityLifePlanningThread.LogCurrentTicks( (int)perQuarterSecondStopwatch.ElapsedTicks );
			perQuarterSecondStopwatch.Stop();

			Interlocked.Increment( ref PlanningThreadsRun );
		}
		#endregion

		#region DoPerFrame
		public bool DoPerFrame()
		{
			//if (!isInitialized) return false;

			VisDebuggingCommon.HandleQueue();

			if ( Engine_HotM.GameMode == MainGameMode.CityMap )
				return false; //pause all that!

			//do any movement for existing city life entities, regardless of what the time slicing was like
			{
				//we only care about the population density on the actual game cells
				//if any of those are wrong, go ahead and repopulate them at the correct amounts
				IReadOnlyList<MapCell> cityLifeTiles = CityMap.CityLifeMapCells;
				for ( int i = 0; i < cityLifeTiles.Count; i++ )
				{
					MapCell cell = cityLifeTiles[i];
					{
						List<IStreetMob> allMobs = cell.ActiveStreetMobs.GetDisplayList();

						for ( int j = allMobs.Count - 1; j >= 0; j-- )
						{
							IStreetMob mob = allMobs[j];
							if ( mob == null || mob.GetInPoolStatus() )
								continue; //this can happen on producer/consumer lists, just ignore it
							if ( mob.IsReadyForDespawn() )
								continue; //waiting on the bg thread

							if ( mob.TimeSinceSpawn < 0.01f && /*mob.Fade < 0.01f &&*/ AreAnyNearbyOnSpawnOnly( allMobs, mob ) )
								continue;
							mob.DoMobPerFrame();

							if ( cell.IsConsideredInCameraView )
								mob.DoMobRender();
						}
					}

					#region Show Pathfinding Of City Vehicles
					if ( VisDebuggingCommon.DoDebug_Pathfinding )
					{
						//loop over all city vehicles
						ListView<ISimCityVehicle> vehicles = World.CityVehicles.GetAllCityVehicles();
						if ( vehicles.Count > 0 )
						{
							foreach ( ISimCityVehicle vehicle in vehicles )
							{
								List<Vector3> path = vehicle.GetPathListToAlter_FromSimThreadOnly();

								Vector3 prior = vehicle.GetVisWorldLocation();
								foreach ( Vector3 spot in path )
								{
									VisDebuggingCommon.Debug_DrawLine( prior, spot, ColorMath.LeafGreen, 1f );
									prior = spot;
								}
							}
						}
						List<IStreetMob> pedestrians = cell.ActiveStreetMobs.GetDisplayList();
						if (pedestrians.Count > 0)
						{
							foreach (IStreetMob mob in pedestrians)
							{
								if (mob is AbstractPedestrian ped)
								{
									IList<Waypoint> path = ped.GetCurrentPath();
									if(path.Count == 0) continue;
									Vector3 p = ped.Pos;
									Vector3 e = ped.GetCurrentWaypoint();
									VisDebuggingCommon.Debug_DrawLine(p, e, ColorMath.BlueTopaz, 1f);

									Vector3 pr = e;
									foreach (Waypoint spot in path)
									{
										VisDebuggingCommon.Debug_DrawLine(pr, spot, ColorMath.LightGreen, 1f);
										pr = spot;
									}
								}
							}
						}
					}
					#endregion

					//road network debug draw
					//               if ( World.CityNetwork.GetHasFinishedCalculationOfCityNetwork() && VisDebuggingCommon.DoDebug_CityNetwork )
					//{
					//	List<CityNetworkNode> list = cell.CityNetwork;

					//	foreach ( CityNetworkNode node in list )
					//	{
					//		/*if (node.isBuilding)
					//		{
					//			ListView<INetworkNode> connectedNodes = node.GetConnections();
					//			foreach (INetworkNode conn in connectedNodes)
					//				VisDebuggingCommon.Debug_DrawLine(node.Item.CenterPoint, conn.SecondaryPos, Color.blue);
					//		}*/
					//		if (node.isDeadEnd) continue;
					//		ThrowawayList<CityNetworkNode> connectedNodes = node.Connections;
					//		if (connectedNodes.Count == 0)
					//		{
					//			VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, node.Item.CenterPoint.ReplaceY(2), Color.red);
					//			continue;
					//		}
					//		foreach ( CityNetworkNode conn in connectedNodes)
					//		{
					//			if (node.isBuilding)
					//			{
					//				if (conn.isBuilding)
					//				{
					//					VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, conn.SecondaryPos, Color.yellow, 2f );
					//				}

					//				if (conn.isDeadEnd)
					//				{
					//					VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, conn.SecondaryPos, Color.red, 3f );
					//				} 
					//			}
					//			else if (conn.isDeadEnd)
					//			{
					//				VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, conn.SecondaryPos, Color.red, 3f );
					//			}
					//			else if (conn.isBuilding)
					//			{
					//				VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, conn.SecondaryPos, Color.blue, 2f );
					//			}
					//			else 
					//			{
					//				VisDebuggingCommon.Debug_DrawLine(node.Item.GroundCenterPoint, conn.Item.GroundCenterPoint, Color.cyan, 2f );
					//			}
					//		}
					//	}
					//}
				}
			}

			return true;
		}
		#endregion

		#region PerQuarterSecond_PopulateCells
		private void PerQuarterSecond_PopulateCells( MersenneTwister Rand )
		{
			bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;

            foreach ( MapCell cell in CityMap.Cells )
				cell.ActiveStreetMobs.ClearConstructionListForStartingConstruction();

            #region Check Despawning
            {
                for ( int i = AllMobs_BGThreadOnly.Count - 1; i >= 0; i-- )
				{
					IStreetMob existingMob = AllMobs_BGThreadOnly[i];
					if ( existingMob.GetInPoolStatus() )
                    {
                        AllMobs_BGThreadOnly.RemoveAt( i, true );
                        continue;
                    }

                    MapCell cell = CityMap.TryGetWorldCellAtCoordinates( existingMob.Pos );
                    if ( cell == null || !cell.GetIsReadyForBuildingsQuery() )
                    {
                        AllMobs_BGThreadOnly.RemoveAt( i, true );
                        continue;
                    }

                    if ( existingMob.IsReadyForDespawn() )
                    {
						if ( cell.IsConsideredInCameraView && existingMob.TimeSinceSpawn > 1.5f )
							existingMob.SpawnFadingOutCopyOfSelf();
                        AllMobs_BGThreadOnly.RemoveAt( i, true );
                        continue;
                    }

                    bool shouldHaveCityLife = cell.ShouldHaveCityLifeRightNow && !isMapMode;
                    bool wouldHaveCityLifeExceptForSomethingElse = shouldHaveCityLife;
                    if ( cell.IsCellConsideredIrradiated )
                        shouldHaveCityLife = false;

                    if ( !shouldHaveCityLife )
                    {
						if ( wouldHaveCityLifeExceptForSomethingElse )
                        {
                            if ( cell.IsConsideredInCameraView && existingMob.TimeSinceSpawn > 1.5f )
                                existingMob.SpawnFadingOutCopyOfSelf();
                        }
                        AllMobs_BGThreadOnly.RemoveAt( i, true );
                        continue;
                    }

                    //if we got here, then this mob is ok!
                    cell.ActiveStreetMobs.AddToConstructionList( existingMob );
                }
			}
			#endregion

			while ( BreachingPodQueue.TryDequeue( out BreachingPodRequest request ) )
				this.SpawnBreachingPodFromRequest_BackgroundThread( request, Rand );

            foreach ( MapCell cell in CityMap.Cells )
			{
				if ( cell == null || !cell.GetIsReadyForBuildingsQuery() )
					continue; //if not ready yet, the below also would fail

				bool shouldHaveCityLife = cell.ShouldHaveCityLifeRightNow && !isMapMode;
				if ( cell.IsCellConsideredIrradiated )
					shouldHaveCityLife = false;

				if ( !shouldHaveCityLife )
				{
					if ( cell.HasDoneSeedOfCityLifeSinceHasHadIt )
						cell.HasDoneSeedOfCityLifeSinceHasHadIt = false;
					continue; //skip the rest
				}

				if ( shouldHaveCityLife )
				{
					if ( !cell.HasDoneSeedOfCityLifeSinceHasHadIt )
					{
						cell.HasDoneSeedOfCityLifeSinceHasHadIt = true;
						PopulateCityLifeOnMapCell_BackgroundThread( cell, true, Rand ); //yes regions
						Interlocked.Increment( ref CellFills );
					}
					else
					{
						PopulateCityLifeOnMapCell_BackgroundThread( cell, false, Rand ); //no regions
					}
				}
			}

            foreach ( MapCell cell in CityMap.Cells )
                cell.ActiveStreetMobs.SwitchConstructionToDisplay();
        }
		#endregion

		#region AreAnyNearbyOnSpawnOnly
		private bool AreAnyNearbyOnSpawnOnly( List<IStreetMob> streetMobs, IStreetMob mob )
		{
			for ( int j = streetMobs.Count - 1; j >= 0; j-- )
			{
				IStreetMob otherMob = streetMobs[j];
				if ( otherMob == null || otherMob.GetInPoolStatus() )
					continue;
				if ( otherMob == mob )
					continue;

				if ( (otherMob.Pos - mob.Pos).sqrMagnitude > 2 ) continue;
				if ( Vector3.Dot( otherMob.Forward, mob.Forward ) < 0.8 ) continue;

				return true;
			}
			return false;
		}
		#endregion

		#region PopulateCityLifeOnMapCell
		private void PopulateCityLifeOnMapCell_BackgroundThread( MapCell cell, bool doRegions, MersenneTwister Rand )
		{
			if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
				return; //nevermind then!

			//int frameStarted = Time.frameCount;

			List<IStreetMob> constructingMobs = cell.ActiveStreetMobs.GetConstructionList(); //this is the one we are building!

			if ( !cell.ShouldHaveCityLifeRightNow )
				return; //unlikely to happen, but just in case

			int pedestrianCount = 0;
            int vehicleCount = 0;

			foreach( IStreetMob mob in constructingMobs )
			{
				if ( mob is Pedestrian )
					pedestrianCount++;
                else if ( mob is StreetVehicle )
                    vehicleCount++;
            }

            if ( vehicleCount > curTrafficDensity )
            {
                #region Remove Excessive Vehicles
                int toRemove = vehicleCount - curTrafficDensity;
                for ( int j = constructingMobs.Count - 1; j >= 0; j-- )
                {
                    IStreetMob mob = constructingMobs[j];
                    if ( mob is StreetVehicle )
					{
						toRemove--;
						mob.SpawnFadingOutCopyOfSelf();
						constructingMobs.RemoveAt( j );
						AllMobs_BGThreadOnly.Remove( mob, true );
                        if ( toRemove <= 0 )
							break;
                    }
                }
                #endregion
            }
            else
                SpawnOrDespawnMobsForCell_BackgroundThread( constructingMobs, vehicleCount, curTrafficDensity, cell, true, Rand );

   //         if ( pedestrianCount > curPedestrianDensity )
			//{
   //             #region Remove Excessive Pedestrians
   //             int toRemove = pedestrianCount - curPedestrianDensity;
   //             for ( int j = constructingMobs.Count - 1; j >= 0; j-- )
   //             {
   //                 IStreetMob mob = constructingMobs[j];
   //                 if ( mob is Pedestrian )
   //                 {
   //                     toRemove--;
   //                     mob.SpawnFadingOutCopyOfSelf();
   //                     constructingMobs.RemoveAt( j );
   //                     AllMobs_BGThreadOnly.Remove( mob, true );
   //                     if ( toRemove <= 0 )
   //                         break;
   //                 }
   //             }
   //             #endregion
   //         }
   //         else
   //             SpawnOrDespawnMobsForCell_BackgroundThread( constructingMobs, pedestrianCount, curPedestrianDensity, cell, false, Rand );

			if ( !doRegions )
				return;

			if ( !DisableBackgroundGunfights )
			{
				if ( Rand.Next( 0, 100 ) < 35 ) //only a 35% chance of this now
					RandomBuildingShootout_BackgroundThread( constructingMobs, cell, Rand );
			}

			//GenerateZoneActivities_BackgroundThread( cell, Rand );
		}

		private void SpawnOrDespawnMobsForCell_BackgroundThread( List<IStreetMob> constructingMobs, int currentMobCount, int actualTargetEntities, MapCell cell,
			bool IsForVehicles, MersenneTwister Rand )
		{
			if ( currentMobCount > actualTargetEntities )
				return; //nothing to do, apparently!

			if ( cell.ParentTile.POIOrNull != null && cell.ParentTile.POIOrNull.Type.BlocksNormalTraffic )
				return;

			while ( currentMobCount < actualTargetEntities )
			{
				IStreetMob mob;
				if ( IsForVehicles )
					mob = SpawnStreetMob_BackgroundThread( constructingMobs, cell, StreetVehicle.GetFromPoolOrCreate, Rand );
				else
					mob = SpawnStreetMob_BackgroundThread( constructingMobs, cell, Pedestrian.GetFromPoolOrCreate, Rand );

                mob.MarkAsReadyToUse();
                currentMobCount++;

                if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
					return; //nevermind then!
			}
		}

		private void RandomBuildingShootout_BackgroundThread( List<IStreetMob> constructingMobs, MapCell cell, MersenneTwister Rand )
		{
			MapPOI poi = cell.ParentTile?.POIOrNull;
			if ( poi != null )
			{
				int clearance = poi?.Type?.RequiredClearance?.Level ?? 0;
				if ( clearance > 1 )
					return; //not on this high of a clearance
            }


            MapItem building = cell.BuildingsValidForShootouts.GetDisplayList().GetRandom( Rand )?.GetMapItem();
			if ( building == null )
				return; //it's possible that there are literally no buildings on the cell!  This was throwing errors when that was not accounted-for

			VisDrawingObjectTag drawTag = null;
			int drawTagIndex = Rand.Next( 0, 4 );
			switch ( drawTagIndex )
			{
				case 1:
					drawTag = CommonRefs.BGWarrior_Gang;
					break;
                case 2:
                    drawTag = CommonRefs.BGWarrior_Military;
                    break;
                case 3:
                    drawTag = CommonRefs.BGWarrior_Paramilitary;
                    break;
				default:
                    drawTag = CommonRefs.BGWarrior_SecForce;
                    break;
            }

            int number = Rand.Next( 5, 12 );
			for ( int i = 0; i < number; i++ )
			{
				Vector3 loc = BuildingShootout.GetPositionNearBuilding( building );
				Gangster ped = SpawnStreetMob_BackgroundThread( constructingMobs, cell, () =>
				{
					Gangster p = Gangster.GetFromPoolOrCreate();
					p.Tag = drawTag;
					return p;
				}, Rand );
				Vector3 target = building.CenterPoint;
				target.y = Rand.NextFloat( building.MaxY * 0.6f ) + 0.2f;

				ped.JustStandThere( Rand, loc, target );
				ped.MarkAsReadyToUse();
			}
		}
		#endregion

		#region SpawnBreachingPodFromRequest_BackgroundThread
		private void SpawnBreachingPodFromRequest_BackgroundThread( BreachingPodRequest Request, MersenneTwister Rand )
        {

            Vector3 originForPod = Request.OriginForPod;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( originForPod );
			if ( cell == null )
                return;
            if ( !cell.ShouldHaveCityLifeRightNow )
                return;

            Quaternion rotation = Request.Rotation;
            Vector3 podTarget = Request.PodTarget;

            BreachingPod pod = SpawnStreetMob_BackgroundThread( cell.ActiveStreetMobs.GetConstructionList(), cell, BreachingPod.GetFromPoolOrCreate, Rand );
            pod.SetPathSimpleToPoint( originForPod, podTarget, rotation );
			pod.MarkAsReadyToUse();
        }
        #endregion

        //private void GenerateZoneActivities_BackgroundThread( MapCell cell, MersenneTwister Rand )
        //{
        //	int debugStage = 0;
        //	try
        //	{
        //		debugStage = 100;

        //		if ( cell == null || cell.PathingRegions == null || cell.PathingRegions.Count < 1 ) return;
        //		MapPathingRegion region = cell.PathingRegions.GetRandom( Rand );
        //		if ( region == null || region.PathingType == null ) return;

        //		debugStage = 400;

        //		AbstractMob ped;
        //		int max;
        //		switch ( region.PathingType.ID )
        //		{
        //			case "CrowdStanding":
        //				debugStage = 2100;
        //				max = Mathf.CeilToInt( region.OBBCache.OBBExtents.x * region.OBBCache.OBBExtents.z / 25 );
        //				for ( int i = 0; i < max; i++ )
        //				{
        //					Vector3 pos = region.Position;
        //					pos.x += Rand.NextFloat( -region.OBBCache.OBBExtents.x, region.OBBCache.OBBExtents.x );
        //					pos.z += Rand.NextFloat( -region.OBBCache.OBBExtents.z, region.OBBCache.OBBExtents.z );
        //					ped = SpawnStreetMob_BackgroundThread( cell, () =>
        //					{
        //						Pedestrian p = Pedestrian.GetFromPoolOrCreate();
        //						p.JustStandThere( Rand, pos, Vector3.negativeInfinity );
        //						return p;
        //					}, Rand );
        //				}
        //				break;
        //			case "CrowdWander":
        //				debugStage = 5100;
        //				break;
        //			case "MilitaryRows":
        //				debugStage = 7100;
        //				break;
        //			case "MilitaryMarching":
        //				debugStage = 9100;
        //				break;
        //			case "FarmerWander":
        //				debugStage = 12100;
        //				max = (int)(region.OBBCache.OBBExtents.x / 0.115f);
        //				for ( int i = 0; i < max; i++ )
        //				{
        //					ped = SpawnStreetMob_BackgroundThread( cell, () =>
        //					{
        //						Farmer p = Farmer.GetFromPoolOrCreate();
        //						p.SetActivity( region, i, max );
        //						return p;
        //					}, Rand );
        //				}
        //				break;
        //			case "Basketball":
        //			case "Soccer":
        //				debugStage = 15100;
        //				max = 16;
        //				for ( int i = 0; i < max; i++ )
        //				{
        //					ped = SpawnStreetMob_BackgroundThread( cell, () =>
        //					{
        //						Sportser p = Sportser.GetFromPoolOrCreate();
        //						p.SetActivity( region, i, max );
        //						return p;
        //					}, Rand );
        //				}
        //				break;
        //		}

        //		debugStage = 99100;
        //	}
        //	catch ( Exception e )
        //	{
        //		ArcenDebugging.LogDebugStageWithStack( "GenerateZoneActivities", debugStage, e, Verbosity.ShowAsError );
        //	}
        //}

        private T SpawnStreetMob_BackgroundThread<T>( List<IStreetMob> constructingMobs, MapCell originalCell, GetNewEntryFromAny<T> provider, MersenneTwister Rand ) where T : IStreetMob
		{
			T mob = provider.Invoke();
			mob.InitializeMobFromBackgroundThread( Rand, originalCell );
            AllMobs_BGThreadOnly.Add( mob );
            constructingMobs.Add( mob );
			return mob;
		}

		//public void CheckIntersectionWithCivilians(Vector3 origin, Vector3 destination)
		//{
		//	destination.y = origin.y = 0.01f;
		//	List<MapCell> cityLifeTiles = CityMap.CityLifeMapCells.GetDisplayList();
		//	foreach (MapCell cell in cityLifeTiles)
		//	{
		//		List<IStreetMob> mobs = cell.StreetMobs;
		//		mobs.DoFor(m =>
		//		{
		//			(bool hit, Vector2 _) = PathHelperFuncs.LineCircleIntersection(origin, destination, m.Pos, 0.1f, true);
		//			if (hit)
		//			{
		//				m.ReturnToPool();
		//				//return DelReturn.RemoveAndContinue;
		//			}
		//			return DelReturn.Continue;
		//		});
		//	}
		//}

		//public void CheckOverlapWithCivilians(Vector3 origin, float range)
		//{
		//	List<MapCell> cityLifeTiles = CityMap.CityLifeMapCells.GetDisplayList();
		//	foreach (MapCell cell in cityLifeTiles)
		//	{
		//		List<IStreetMob> mobs = cell.StreetMobs;
		//		mobs.DoFor(m =>
		//		{
		//			if (Vector3.Distance(m.Pos, origin) < range)
		//			{
		//				m.ReturnToPool();
		//			}
		//			return DelReturn.Continue;
		//		});
		//	}
		//}
	}
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using System.Diagnostics;

namespace Arcen.HotM.External
{
	/// <summary>
	/// Central world data and lookups about buildings.
	/// </summary>
	internal class World_Buildings : ISimWorld_Buildings
    {
        public static World_Buildings QueryInstance = new World_Buildings();

        //
        //Serialized data
        //-----------------------------------------------------
        public static readonly ConcurrentDictionary<int, SimBuilding> BuildingsByID = ConcurrentDictionary<int, SimBuilding>.Create_WillNeverBeGCed( "World_Buildings-BuildingsByID" );
        private static int NextBuildingID = 1;

        //
        //Nonserialized data
        //-----------------------------------------------------        
        public static readonly DoubleBufferedList<SimBuilding> BuildingsWithMachineStructures = DoubleBufferedList<SimBuilding>.Create_WillNeverBeGCed( 60, "World_Buildings-BuildingsWithMachineStructures", 60 );
        public static readonly DoubleBufferedList<SimBuilding> BuildingsWithBrokenMachineStructures = DoubleBufferedList<SimBuilding>.Create_WillNeverBeGCed( 60, "World_Buildings-BuildingsWithBrokenMachineStructures", 60 );
        public static readonly DoubleBufferedList<SimBuilding> BuildingsWithDamagedMachineStructures = DoubleBufferedList<SimBuilding>.Create_WillNeverBeGCed( 60, "World_Buildings-BuildingsWithDamagedMachineStructures", 60 );
        public static readonly DoubleBufferedList<SimBuilding> BuildingsWithPausedMachineStructures = DoubleBufferedList<SimBuilding>.Create_WillNeverBeGCed( 60, "World_Buildings-BuildingsWithPausedMachineStructures", 60 );
        public static readonly DoubleBufferedList<SimBuilding> BuildingsWithMachineStructuresOutOfNetwork = DoubleBufferedList<SimBuilding>.Create_WillNeverBeGCed( 60, "World_Buildings-BuildingsWithMachineStructuresOutOfNetwork", 60 );

        private static readonly Stopwatch BuildingUpdateStopwatch = new Stopwatch();
        public static readonly List<MapItem> workingMapItems_General = List<MapItem>.Create_WillNeverBeGCed( 5000, "World_Buildings-workingMapItems_General" );
        public static readonly List<MapItem> workingMapItems_MainThread = List<MapItem>.Create_WillNeverBeGCed( 5000, "World_Buildings-workingMapItems_MainThread" );

		public static void OnGameClear()
        {
            BuildingsByID.Clear();

            BuildingsWithMachineStructures.ClearAllVersions();
            BuildingsWithBrokenMachineStructures.ClearAllVersions();
            BuildingsWithDamagedMachineStructures.ClearAllVersions();
            BuildingsWithPausedMachineStructures.ClearAllVersions();
            BuildingsWithMachineStructuresOutOfNetwork.ClearAllVersions();

            BuildingUpdateStopwatch.Stop();
            BuildingUpdateStopwatch.Reset();
            workingMapItems_General.Clear();
            workingMapItems_MainThread.Clear();

            //this is a funky thing, otherwise some other stuff does not work right
            NextBuildingID = 1;
        }

        #region Serialization
        public static void Serialize( ArcenFileSerializer Serializer )
        {
            Serializer.AddInt32( "NextBuildingID", NextBuildingID );

            foreach ( KeyValuePair<int, SimBuilding> kv in BuildingsByID )
            {
                SimBuilding building = kv.Value;
                if ( building == null || building.IsInPoolAtAll )
                    continue;
                //if ( !building.Item.ParentCell.AllBuildings.Contains( building.Item ) )
                //    continue; //must have been deleted

                Serializer.StartObject( "Building" );
                building.Serialize( Serializer );
                Serializer.EndObject( "Building" );
            }
        }

        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {
            NextBuildingID = Data.GetInt32( "NextBuildingID", false );

            if ( Data.ChildLayersByName.TryGetValue( "Building", out List<DeserializedObjectLayer> building ) )
            {
                int poiCount = CityMap.POIsByID.Count;  
                if ( building.Count > 0 )
                {
                    for ( int i = 0; i < building.Count; i++ )
                    {
                        SimBuilding bldg = SimBuilding.Deserialize( building[i] );
                        if ( bldg != null )
                        {
                            AddBuilding( bldg );

                            if ( bldg.BuildingID > NextBuildingID )
                                NextBuildingID = bldg.BuildingID + 1;
                        }
                    }

                    //ArcenDebugging.LogSingleLine( "Deserializing " + building.Count + " buildings.", Verbosity.DoNotShow );
                }
                else
                    ArcenDebugging.LogSingleLine( "Expected at least 1 Building, but found zero of them!", Verbosity.DoNotShow );

                int added = CityMap.POIsByID.Count - poiCount;

                if ( added > 0 )
                    ArcenDebugging.LogSingleLine( "Added " + added + " new pois that were missing.", Verbosity.DoNotShow );
            }
            else
                ArcenDebugging.LogSingleLine( "Could not find any Building entries in this savegame!", Verbosity.DoNotShow );
        }
        #endregion

        #region DoAnyPerFrameLogic
        public static void DoAnyPerFrameLogic()
        {

        }
        #endregion

        #region DoAnyPerQuarterSecondLogic_SimThread
        internal static void DoAnyPerQuarterSecondLogic_SimThread( MersenneTwister Rand )
        {
            SimCommon.ValidEmbeddedStructureBuildTargets.ClearConstructionListForStartingConstruction();

            MachineStructureType structureType = Engine_HotM.CurrentEmbeddedStructureTypeToFocus;
            if ( structureType != null && structureType.IsEmbeddedInHumanBuildingOfTag != null )
            {
                MachineJob startingJob = Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith;
                List<ISimBuilding> buildings = structureType.IsEmbeddedInHumanBuildingOfTag.DuringGame_Buildings.GetDisplayList();
                foreach ( ISimBuilding building in buildings )
                {
                    if ( building.CalculateIsValidTargetForMachineStructureRightNow( structureType, startingJob ) )
                        SimCommon.ValidEmbeddedStructureBuildTargets.AddToConstructionList( building );
                }
            }

            SimCommon.ValidEmbeddedStructureBuildTargets.SwitchConstructionToDisplay();
        }
        #endregion

        #region DoAnyPerSecondLogic_SimThread
        internal static void DoAnyPerSecondLogic_SimThread( bool SkipExpensiveCalculations )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                bool debug = GameSettings.Current.GetBool( "Debug_MainGameBuildingDebug" );

                SimPerFullSecond.SimLoopOuterStopwatch.Stop();
                SimPerFullSecond.SimLoopInnerStopwatch.Reset();
                SimPerFullSecond.SimLoopInnerStopwatch.Start();

                int ticksAtStartOfPhase = (int)SimPerFullSecond.SimLoopInnerStopwatch.ElapsedTicks;

                debugStage = 6000;

                foreach ( MapTile tile in CityMap.Tiles )
                {
                    tile.IsTileContainingMachineActors.ClearConstructionValueForStartingConstruction();
                }

                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearConstructionListForStartingConstruction();
                }

                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearConstructionListForStartingConstruction();
                }
                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.ClearConstructionListForStartingConstruction();
                }

                debugStage = 12000;

                bool wasMissingSomeOutdoorSpotsInBounds = false;
                bool wasMissingSomeOutdoorSpotsOutOfBounds = false;
                foreach ( MapCell cell in CityMap.Cells )
                {
                    if ( cell.HasEverCalculatedOutdoorSpots )
                    {
                        if ( cell.AllOutdoorSpots.Count == 0 && cell.ParentTile.IsOutOfBoundsTile )
                            cell.HasEverCalculatedOutdoorSpots = false;
                    }
                    if ( !cell.HasEverCalculatedOutdoorSpots )
                    {
                        if ( cell.ParentTile.IsOutOfBoundsTile )
                            wasMissingSomeOutdoorSpotsOutOfBounds = true;
                        else
                            wasMissingSomeOutdoorSpotsInBounds = true;
                    }
                    cell.LowerAndMiddleClassResidentsAndWorkersInCell.ClearConstructionValueForStartingConstruction();
                    cell.UpperClassResidentsAndWorkersInCell.ClearConstructionValueForStartingConstruction();
                    cell.PrisonersInCell.ClearConstructionValueForStartingConstruction();
                    cell.HomelessInCell.ClearConstructionValueForStartingConstruction();
                    cell.IsPotentialCyberocracyCell.ClearConstructionValueForStartingConstruction();

                    cell.BuildingsToDrawInMap.ClearConstructionListForStartingConstruction();
                    cell.BuildingsWithMachineStructures.ClearConstructionListForStartingConstruction();
                    cell.BuildingsWithSwarms.ClearConstructionListForStartingConstruction();
                    cell.BuildingsWithSpecialtyResources.ClearConstructionListForStartingConstruction();
                    cell.BuildingsWithBeacons.ClearConstructionListForStartingConstruction();
                    cell.BuildingsValidForShootouts.ClearConstructionListForStartingConstruction();

                    cell.Cached_CalculateIsValidStartingCellForTower = cell.CalculateIsValidStartingCellForTower();
                    cell.LowestRoughDistanceToMachines.ClearConstructionValueForStartingConstruction();
                }

                if ( SimCommon.NeedsBuildingListRecalculation || CityBuildingListCalculator.GetIsCalculatingNow() )
                { } //not ready yet
                else
                {
                    //ready to do this
                    if ( wasMissingSomeOutdoorSpotsInBounds )
                        Helper_AddSeedSpotsAndPOIGuards.AddSeedSpots_AndMaybePOIGuardsIfNeeded( false );
                    else if ( wasMissingSomeOutdoorSpotsOutOfBounds )
                        Helper_AddSeedSpotsAndPOIGuards.AddSeedSpots_AndMaybePOIGuardsIfNeeded( true ); //don't give me the POI guard bits
                }

                debugStage = 18000;

                foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                {
                    kv.Value.AllBuildings.ClearConstructionListForStartingConstruction();
                    kv.Value.BuildingsWithMachineStructures.ClearConstructionListForStartingConstruction();
                }

                foreach ( BuildingTag tag in BuildingTagTable.Instance.Rows )
                    tag.DuringGame_Buildings.ClearConstructionListForStartingConstruction();
                foreach ( POITag tag in POITagTable.Instance.Rows )
                {
                    tag.DuringGame_POIBuildings.ClearConstructionListForStartingConstruction();
                    tag.DuringGame_POIs.ClearConstructionListForStartingConstruction();
                }
                foreach ( DistrictTag tag in DistrictTagTable.Instance.Rows )
                    tag.DuringGame_Buildings.ClearConstructionListForStartingConstruction();
                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                {
                    kv.Value.DuringGame_BuildingsInPOI.ClearConstructionListForStartingConstruction();
                    if ( kv.Value?.Type != null )
                    {
                        foreach ( KeyValuePair<string, POITag> kvv in kv.Value.Type.Tags )
                            kvv.Value.DuringGame_POIs.AddToConstructionList( kv.Value );
                    }
                }

                foreach ( Swarm swarm in SwarmTable.Instance.Rows )
                {
                    swarm.DuringGame_BuildingsInSwarm.ClearConstructionListForStartingConstruction();
                    swarm.DuringGame_TotalSwarmCount.ClearConstructionValueForStartingConstruction();
                }

                debugStage = 25000;

                World_People.AllJobs.ClearConstructionDictForStartingConstruction();
                World_People.ResidentialCapacity.ClearConstructionDictForStartingConstruction();
                World_People.CurrentResidents.ClearConstructionDictForStartingConstruction();
                World_People.CurrentWorkers.ClearConstructionDictForStartingConstruction();
                BuildingsWithMachineStructures.ClearConstructionListForStartingConstruction();
                BuildingsWithBrokenMachineStructures.ClearConstructionListForStartingConstruction();
                BuildingsWithDamagedMachineStructures.ClearConstructionListForStartingConstruction();
                BuildingsWithPausedMachineStructures.ClearConstructionListForStartingConstruction();
                BuildingsWithMachineStructuresOutOfNetwork.ClearConstructionListForStartingConstruction();
                SimCommon.StructuresWithJobRange.ClearConstructionListForStartingConstruction();

                bool veryVerboseDebug = false;

                CityMap.ItemsToHaveCollidersInMapMode.ClearConstructionListForStartingConstruction();
                CityMap.AllDroneDeliveryBuildings.ClearConstructionListForStartingConstruction();
                CityMap.NearbyAllDroneDeliveryBuildings.ClearConstructionListForStartingConstruction();
                CityMap.NearbyVisibleDroneDeliveryBuildings.ClearConstructionListForStartingConstruction();
                CityMap.NearbyAllBuildings.ClearConstructionListForStartingConstruction();
                CityMap.NearbyVisibleBuildings.ClearConstructionListForStartingConstruction();

                debugStage = 150000;

                foreach ( KeyValuePair<int, SimBuilding> BuildingIDPair in BuildingsByID )
                {
                    debugStage = 150100;

                    SimBuilding building = BuildingIDPair.Value;
                    if ( building == null || building.IsInPoolAtAll )
                    {
                        BuildingsByID.TryRemove( BuildingIDPair.Key, 10 );
                        continue;
                    }

                    MapItem item = building.GetMapItem();
                    if ( item == null )
                        continue;

                    debugStage = 150200;

                    debugStage = 150300;

                    bool isDestroyedBuilding = false;
                    bool isFunctionalBuilding = true;

                    BuildingStatus status = building.Status;
                    if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually ) )//|| status.IsBuildingConsideredToBeUnderConstruction) )
                        isDestroyedBuilding = true;
                    if ( status != null && status.ShouldBuildingBeNonfunctional )
                        isFunctionalBuilding = false;

                        debugStage = 154000;

                    MapCell cell = building.GetParentCell();
                    if ( cell == null )
                        continue;
                    BuildingPrefab prefab = building.Prefab;
                    BuildingTypeVariant variant = building.Variant;

                    debugStage = 156000;

                    if ( variant.OverridingAuthorityCohortTag != null && building.overridingAuthorityGroup == null )
                        building.overridingAuthorityGroup = variant.OverridingAuthorityCohortTag.CohortsList.GetRandom( Engine_Universal.PermanentQualityRandom );

                    if ( isFunctionalBuilding ) //only do this for buildings that are functional
                    {
                        debugStage = 158000;

                        if ( veryVerboseDebug )
                            ArcenDebugging.LogSingleLine( "Building " + building.ToDebugString() + " has available jobs: ", Verbosity.DoNotShow );

                        Dictionary<ProfessionType, int> maxJobs = prefab.NormalMaxJobsByProfession;
                        foreach ( KeyValuePair<ProfessionType, int> pair in maxJobs )
                        {
                            if ( pair.Value <= 0 )
                                continue;
                            ProfessionType profType = pair.Key;
                            if ( veryVerboseDebug )
                                ArcenDebugging.LogSingleLine( "\t" + profType + " -> " + pair.Value, Verbosity.DoNotShow );
                            World_People.AllJobs.Construction[profType] += pair.Value;
                        }
                    }

                    if ( isFunctionalBuilding ) //only do this for buildings that are functional
                    {
                        debugStage = 161000;

                        if ( veryVerboseDebug )
                            ArcenDebugging.LogSingleLine( "and available housing", Verbosity.DoNotShow );

                        Dictionary<EconomicClassType, int> maxResidents = prefab.NormalMaxResidentsByEconomicClass;
                        foreach ( KeyValuePair<EconomicClassType, int> pair in maxResidents )
                        {
                            EconomicClassType classType = pair.Key;
                            if ( veryVerboseDebug )
                                ArcenDebugging.LogSingleLine( "\t" + classType + " -> " + pair.Value, Verbosity.DoNotShow );
                            World_People.ResidentialCapacity.Construction[classType] += pair.Value;
                        }
                    }

                    debugStage = 178000;

                    if ( building.MachineStructureInBuilding != null )
                    {
                        debugStage = 178100;
                        building.roughDistanceFromMachines = 0;
                        cell.LowestRoughDistanceToMachines.Construction = 0;
                    }
                    else
                    {
                        debugStage = 178200;
                        float bestDistance = 999999;
                        Vector3 buildingPoint = item.CenterPoint;

                        foreach ( MachineSubnet subnet in SimCommon.Subnets )
                        {
                            if ( subnet == null )
                                continue;

                            debugStage = 178300;
                            float distX = buildingPoint.x - subnet.BoundsCenter.x;
                            float distZ = buildingPoint.z - subnet.BoundsCenter.z;

                            if ( distX < 0 )
                                distX = -distX;
                            if ( distZ < 0 )
                                distZ = -distZ;

                            float largerDist = distX > distZ ? distX : distZ;
                            largerDist -= subnet.BoundsRadius;
                            if ( largerDist < 0 )
                            {
                                bestDistance = 0;
                                break;
                            }

                            if ( largerDist < bestDistance )
                                bestDistance = largerDist;
                        }

                        debugStage = 178400;
                        if ( bestDistance > 0 )
                        {
                            debugStage = 178500;
                            MachineNetwork network = SimCommon.TheNetwork;
                            if ( network != null )
                            {
                                ISimBuilding tower = network?.Tower?.Building;
                                if ( tower != null )
                                {
                                    MapItem towerItem = tower.GetMapItem();
                                    if ( towerItem != null )
                                    {
                                        debugStage = 178600;
                                        Vector3 towerPos = towerItem.CenterPoint;

                                        float distX = buildingPoint.x - towerPos.x;
                                        float distZ = buildingPoint.z - towerPos.z;

                                        if ( distX < 0 )
                                            distX = -distX;
                                        if ( distZ < 0 )
                                            distZ = -distZ;

                                        float largerDist = distX > distZ ? distX : distZ;

                                        largerDist -= 15f; //simple magic number faker number to give a basic radius around towers.  Can put something better later if we care

                                        if ( largerDist < bestDistance )
                                            bestDistance = largerDist;
                                    }
                                }
                            }
                        }

                        debugStage = 178900;
                        building.roughDistanceFromMachines = bestDistance;

                        debugStage = 178910;
                        if ( bestDistance < cell.LowestRoughDistanceToMachines.Construction )
                            cell.LowestRoughDistanceToMachines.Construction = bestDistance;
                    }

                    MapTile tileOrNull = cell?.ParentTile;
                    MapDistrict districtOrNull = building.GetParentDistrict();
                    {
                        debugStage = 179000;

                        MachineStructure machineStructure = building.MachineStructureInBuilding;
                        MachineJob job = machineStructure?.CurrentJob;
                        if ( machineStructure != null )
                        {
                            BuildingsWithMachineStructures.AddToConstructionList( building );
                            if ( districtOrNull != null )
                                districtOrNull.BuildingsWithMachineStructures.AddToConstructionList( building );

                            if ( machineStructure.IsJobPaused )
                                BuildingsWithPausedMachineStructures.AddToConstructionList( building );

                            if ( !machineStructure.GetIsConnectedToNetwork() )
                                BuildingsWithMachineStructuresOutOfNetwork.AddToConstructionList( building );

                            bool isBrokenStructure = false;
                            if ( isDestroyedBuilding || machineStructure.GetActorDataCurrent( ActorRefs.ActorHP, true ) <= 0 || machineStructure.IsFullDead || machineStructure.IsBeingRebuilt )
                            {
                                isBrokenStructure = true;
                                BuildingsWithBrokenMachineStructures.AddToConstructionList( building );
                                BuildingsWithDamagedMachineStructures.AddToConstructionList( building );
                            }
                            else if ( !machineStructure.IsUnderConstruction && machineStructure.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) > 0 )
                                BuildingsWithDamagedMachineStructures.AddToConstructionList( building );

                            float jobEffectRange = 0;
                            bool hasJobEffectRange = job?.GetJobEffectRange( out jobEffectRange )??false;
                            float jobEffectRangeSquared = jobEffectRange * jobEffectRange;
                            Vector3 structureLocation = building.Item.OBBCache.Center;

                            machineStructure.ActorsWithinEffectiveRangeOfThisMachineStructure.ClearConstructionListForStartingConstruction();
                            machineStructure.StructuresWithinEffectiveRangeOfThisMachineStructure.ClearConstructionListForStartingConstruction();

                            #region Log Boosting Structures Only For Non-Broken Ones
                            if ( !isBrokenStructure && hasJobEffectRange )
                            {
                                SimCommon.StructuresWithJobRange.AddToConstructionList( machineStructure );

                                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                                {
                                    float groundDistanceSquared = (kv.Value.GetDrawLocation() - structureLocation).GetSquareGroundMagnitude();

                                    if ( groundDistanceSquared <= jobEffectRangeSquared )
                                        machineStructure.ActorsWithinEffectiveRangeOfThisMachineStructure.AddToConstructionList( kv.Value );
                                    if ( (job?.BoostsUnitsInRange ?? false) )
                                    {
                                        if ( groundDistanceSquared <= jobEffectRangeSquared )
                                            kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.AddToConstructionList( building );
                                    }
                                }
                                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                                {
                                    float groundDistanceSquared = (kv.Value.GetDrawLocation() - structureLocation).GetSquareGroundMagnitude();

                                    if ( groundDistanceSquared <= jobEffectRangeSquared )
                                        machineStructure.ActorsWithinEffectiveRangeOfThisMachineStructure.AddToConstructionList( kv.Value );
                                    if ( (job?.BoostsUnitsInRange ?? false) )
                                    {
                                        if ( groundDistanceSquared <= jobEffectRangeSquared )
                                            kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.AddToConstructionList( building );
                                    }
                                }
                                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                                {
                                    float groundDistanceSquared = (kv.Value.WorldLocation - structureLocation).GetSquareGroundMagnitude();

                                    if ( groundDistanceSquared <= jobEffectRangeSquared )
                                        machineStructure.ActorsWithinEffectiveRangeOfThisMachineStructure.AddToConstructionList( kv.Value );
                                    if ( (job?.BoostsVehiclesInRange ?? false) )
                                    {
                                        if ( groundDistanceSquared <= jobEffectRangeSquared )
                                            kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.AddToConstructionList( building );
                                    }
                                }

                                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                                {
                                    MachineStructure otherStructure = kv.Value;
                                    if ( otherStructure == null || otherStructure == machineStructure )
                                        continue; //don't include self
                                    MapItem otherItem = otherStructure.Building?.GetMapItem();
                                    if ( otherItem == null )
                                        continue;

                                    float groundDistanceSquared = (otherItem.OBBCache.Center - structureLocation).GetSquareGroundMagnitude();
                                    if ( groundDistanceSquared <= jobEffectRangeSquared )
                                        machineStructure.StructuresWithinEffectiveRangeOfThisMachineStructure.AddToConstructionList( otherStructure );
                                }
                            }
                            #endregion Log Boosting Structures Only For Non-Broken Ones

                            machineStructure.ActorsWithinEffectiveRangeOfThisMachineStructure.SwitchConstructionToDisplay();
                            machineStructure.StructuresWithinEffectiveRangeOfThisMachineStructure.SwitchConstructionToDisplay();
                        }

                        Swarm swarm = building.SwarmSpread;
                        if ( swarm != null )
                        {
                            int swarmCount = building.SwarmSpreadCount;
                            if ( swarmCount <= 0 )
                                building.SwarmSpread = null;
                            else
                            {
                                swarm.DuringGame_BuildingsInSwarm.AddToConstructionList( building );
                                swarm.DuringGame_TotalSwarmCount.Construction += swarmCount;
                            }
                        }

                        if ( districtOrNull != null )
                        {
                            districtOrNull.AllBuildings.AddToConstructionList( building );
                            foreach ( KeyValuePair<string, DistrictTag> kv2 in districtOrNull.Type.Tags )
                                kv2.Value.DuringGame_Buildings.AddToConstructionList( building );

                            if ( !districtOrNull.HasBeenDiscovered && cell != null && cell.ParentTile.HasEverBeenExplored )
                                districtOrNull.HasBeenDiscovered = true;
                        }

                        foreach ( KeyValuePair<string, BuildingTag> kv2 in variant.Tags )
                            kv2.Value.DuringGame_Buildings.AddToConstructionList( building );

                        MapPOI poi = building.CalculateLocationPOI();
                        if ( poi != null )
                        {
                            if ( !poi.HasBeenDiscovered && cell != null && cell.ParentTile.HasEverBeenExplored )
                                poi.HasBeenDiscovered = true;

                            poi.DuringGame_BuildingsInPOI.AddToConstructionList( building );
                            foreach ( KeyValuePair<string, POITag> kv2 in poi.Type.Tags )
                                kv2.Value.DuringGame_POIBuildings.AddToConstructionList( building );
                        }
                    }

                    debugStage = 183000;

                    //bool shouldHaveCollidersInMapMode = false;

                    debugStage = 192000;

                    switch ( variant.Type.ID )
                    {
                        case "MachineTower":
                            if ( building.MachineStructureInBuilding == null && SimCommon.SecondsSinceLoaded > 3 )
                            {
                                Vector3 position = item.GroundCenterPoint;

                                //destroy the building as well, because this structure is the building
                                building.SetStatus( CommonRefs.DemolishedBuildingStatus );
                                item.DropBurningEffect();
                                building.FullyDeleteBuilding(); //AND fully delete the building

                                //add an outdoor spot that units can move to like they used to move to the structure
                                if ( cell != null )
                                {
                                    MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                                    outdoorSpot.IsOnRoad = false;
                                    outdoorSpot.Position = position.ReplaceY( MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD );
                                    cell.AllOutdoorSpots.Add( outdoorSpot );
                                }
                            }
                            break;
                    }

                    debugStage = 193000;

                    debugStage = 194000;

                    if ( cell != null )
                    {
                        if ( building.Prefab.ShouldBeShownInMapMode && isFunctionalBuilding )
                            cell.BuildingsToDrawInMap.AddToConstructionList( building );
                        else
                        {
                            if ( building.MachineStructureInBuilding != null && isFunctionalBuilding )
                                cell.BuildingsToDrawInMap.AddToConstructionList( building );
                        }

                        if ( building.MachineStructureInBuilding != null )
                        {
                            cell.BuildingsWithMachineStructures.AddToConstructionList( building );

                            if ( building?.MachineStructureInBuilding?.Type?.IsCyberocracyHub ?? false )
                                cell.IsPotentialCyberocracyCell.Construction = true;
                        }
                        if ( building.SwarmSpread != null )
                            cell.BuildingsWithSwarms.AddToConstructionList( building );
                        if ( !isDestroyedBuilding && building.Variant?.BeaconToShow != null )
                            cell.BuildingsWithBeacons.AddToConstructionList( building );
                        if ( !isDestroyedBuilding && building.MachineStructureInBuilding == null && ( building.Variant?.IsShootoutTarget??false) )
                            cell.BuildingsValidForShootouts.AddToConstructionList( building );

                        if ( !isDestroyedBuilding && building.Variant?.SpecialScavengeResource != null && !building.HasSpecialResourceAlreadyBeenExtracted )
                            cell.BuildingsWithSpecialtyResources.AddToConstructionList( building );

                        if ( !isDestroyedBuilding )
                        {
                            {
                                int lowerClass = 0;
                                foreach ( EconomicClassType econClass in CommonRefs.LowerAndMiddleClassResidents )
                                    lowerClass += building.GetResidentAmount( econClass );
                                foreach ( ProfessionType profession in CommonRefs.LowerAndMiddleClassProfessions )
                                    lowerClass += building.GetWorkerAmount( profession );

                                if ( lowerClass > 0 )
                                    cell.LowerAndMiddleClassResidentsAndWorkersInCell.Construction += lowerClass;

                                building.IsPeacefulCyberocracyTarget = lowerClass > 0;
                                building.LowerClassCitizenGrabCount = lowerClass;
                            }

                            {
                                int upperClass = 0;
                                foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                                    upperClass += building.GetResidentAmount( econClass );
                                foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                                    upperClass += building.GetWorkerAmount( profession );

                                if ( upperClass > 0 )
                                    cell.UpperClassResidentsAndWorkersInCell.Construction += upperClass;

                                building.IsViolentCyberocracyTarget = upperClass > 0;
                                building.UpperClassCitizenGrabCount = upperClass;
                            }

                            {
                                int prisoners = building.GetResidentAmount( CommonRefs.PrisonerClass );
                                if ( prisoners > 0 )
                                    cell.PrisonersInCell.Construction += prisoners;
                            }

                            {
                                int homeless = building.GetResidentAmount( CommonRefs.HomelessClass );
                                if ( homeless > 0 )
                                    cell.HomelessInCell.Construction += homeless;
                            }
                        }
                    }
                    
                    debugStage = 196000;

                    debugStage = 198000;

                    if ( isFunctionalBuilding && //don't include non-functional buildings in this
                        cell != null && cell.ShouldHaveCityLifeRightNow ) //means it is nearby
                    {
                        debugStage = 198100;
                        bool isVisible = !item.IsItemInFogOfWar;

                        CityMap.NearbyAllBuildings.AddToConstructionList( building );
                        if ( isVisible )
                            CityMap.NearbyVisibleBuildings.AddToConstructionList( building );

                        if ( variant != null && variant.IsDroneDeliveryTarget )
                        {
                            CityMap.NearbyAllDroneDeliveryBuildings.AddToConstructionList( building ); 
                            if ( isVisible )
                                CityMap.NearbyVisibleDroneDeliveryBuildings.AddToConstructionList( building );
                        }
                    }

                    if ( variant != null && variant.IsDroneDeliveryTarget )
                        CityMap.AllDroneDeliveryBuildings.AddToConstructionList( building );
                } //end the loop of BuildingsByID

                debugStage = 301000;

                //all machine vehicles give vision on their own cells
                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    MapTile tile = kv.Value?.GetCurrentMapCell()?.ParentTile;
                    if ( tile != null )
                    {
                        tile.IsTileContainingMachineActors.Construction = true;
                        tile.HasEverBeenExplored = true;
                    }
                }
                //now so do machine units
                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    MapTile tile = kv.Value?.CalculateMapCell()?.ParentTile;
                    if ( tile != null )
                    {
                        tile.IsTileContainingMachineActors.Construction = true;
                        tile.HasEverBeenExplored = true;
                    }
                }

                debugStage = 303000;

                CityMap.ItemsToHaveCollidersInMapMode.SwitchConstructionToDisplay();
                CityMap.AllDroneDeliveryBuildings.SwitchConstructionToDisplay();
                CityMap.NearbyAllDroneDeliveryBuildings.SwitchConstructionToDisplay();
                CityMap.NearbyVisibleDroneDeliveryBuildings.SwitchConstructionToDisplay();
                CityMap.NearbyAllBuildings.SwitchConstructionToDisplay();
                CityMap.NearbyVisibleBuildings.SwitchConstructionToDisplay();

                debugStage = 306000;

                debugStage = 312000;

                foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                {
                    MapDistrict district = kv.Value;

                    debugStage = 324000;

                    for ( int i = 0; i < EconomicClassTypeTable.Instance.Rows.Length; i++ )
                    {
                        EconomicClassType classType = EconomicClassTypeTable.Instance.Rows[i];
                        district.TryGetTotalCurrentResidents( classType, out int amount );
                        World_People.CurrentResidents.Construction[classType] += amount;
                    }

                    debugStage = 328000;

                    foreach ( KeyValuePair<ProfessionType, int> pair in district.CachedCurrentWorkers.GetDisplayDict() )
                    {
                        ProfessionType profType = pair.Key;
                        World_People.CurrentWorkers.Construction[profType] += pair.Value;
                    }
                }

                debugStage = 410000;

                World_People.AllJobs.SwitchConstructionToDisplay();
                World_People.ResidentialCapacity.SwitchConstructionToDisplay();
                World_People.CurrentWorkers.SwitchConstructionToDisplay();
                World_People.CurrentResidents.SwitchConstructionToDisplay();
                BuildingsWithMachineStructures.SwitchConstructionToDisplay();
                BuildingsWithBrokenMachineStructures.SwitchConstructionToDisplay();
                BuildingsWithPausedMachineStructures.SwitchConstructionToDisplay();
                BuildingsWithMachineStructuresOutOfNetwork.SwitchConstructionToDisplay();
                BuildingsWithDamagedMachineStructures.SwitchConstructionToDisplay();
                SimCommon.StructuresWithJobRange.SwitchConstructionToDisplay();

                debugStage = 416000;

                CityMap.NearbyAllOutdoorSpots.ClearConstructionListForStartingConstruction();
                CityMap.NearbyNonFogOfWarOutdoorSpots.ClearConstructionListForStartingConstruction();

                debugStage = 422000;

                foreach ( MapCell cell in CityMap.Cells )
                {
                    debugStage = 426000;

                    cell.LowerAndMiddleClassResidentsAndWorkersInCell.SwitchConstructionToDisplay();
                    cell.UpperClassResidentsAndWorkersInCell.SwitchConstructionToDisplay();
                    cell.PrisonersInCell.SwitchConstructionToDisplay();
                    cell.HomelessInCell.SwitchConstructionToDisplay();
                    cell.IsPotentialCyberocracyCell.SwitchConstructionToDisplay();

                    cell.BuildingsToDrawInMap.SwitchConstructionToDisplay();
                    cell.BuildingsWithMachineStructures.SwitchConstructionToDisplay();
                    cell.BuildingsWithSwarms.SwitchConstructionToDisplay();
                    cell.BuildingsWithSpecialtyResources.SwitchConstructionToDisplay();
                    cell.BuildingsWithBeacons.SwitchConstructionToDisplay();
                    cell.BuildingsValidForShootouts.SwitchConstructionToDisplay();
                    cell.LowestRoughDistanceToMachines.SwitchConstructionToDisplay();

                    if ( cell.ShouldHaveCityLifeRightNow ) //means it is nearby
                    {
                        foreach ( MapOutdoorSpot outdoorSpot in cell.AllOutdoorSpots )
                        {
                            CityMap.NearbyAllOutdoorSpots.AddToConstructionList( outdoorSpot );
                            if ( !outdoorSpot.IsSpotInFogOfWar )
                                CityMap.NearbyNonFogOfWarOutdoorSpots.AddToConstructionList( outdoorSpot );
                        }
                    }

                    float lowestRough = cell.LowestRoughDistanceToMachines.Display;
                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots )
                        spot.RoughDistanceFromMachines = lowestRough;
                }

                debugStage = 431000;

                CityMap.NearbyAllOutdoorSpots.SwitchConstructionToDisplay();
                CityMap.NearbyNonFogOfWarOutdoorSpots.SwitchConstructionToDisplay();

                foreach ( MapTile tile in CityMap.Tiles )
                {
                    if ( SimCommon.IsFogOfWarDisabled )
                        tile.IsTileContainingMachineActors.Construction = tile.HasEverBeenExplored = true;
                    tile.IsTileContainingMachineActors.SwitchConstructionToDisplay();
                }

                foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.SwitchConstructionToDisplay();
                }
                foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.SwitchConstructionToDisplay();
                }
                foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                {
                    kv.Value.BoostingMachineStructuresThatBoostThisActorTypeWithinTheEffectiveRangeOfThatJob.SwitchConstructionToDisplay();
                }

                debugStage = 438000;

                foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                {
                    kv.Value.AllBuildings.SwitchConstructionToDisplay();
                    kv.Value.BuildingsWithMachineStructures.SwitchConstructionToDisplay();
                }

                foreach ( BuildingTag tag in BuildingTagTable.Instance.Rows )
                    tag.DuringGame_Buildings.SwitchConstructionToDisplay();
                foreach ( POITag tag in POITagTable.Instance.Rows )
                {
                    tag.DuringGame_POIBuildings.SwitchConstructionToDisplay();
                    tag.DuringGame_POIs.SwitchConstructionToDisplay();
                }
                foreach ( DistrictTag tag in DistrictTagTable.Instance.Rows )
                    tag.DuringGame_Buildings.SwitchConstructionToDisplay();
                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                    kv.Value.DuringGame_BuildingsInPOI.SwitchConstructionToDisplay();


                foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                {
                    kv.Value.DuringGame_StructuresThatCanAffectPOI.ClearConstructionListForStartingConstruction();

                    foreach ( ISimBuilding building in kv.Value.DuringGame_BuildingsInPOI.GetDisplayList() )
                    {
                        MachineStructure structure = building.MachineStructureInBuilding;
                        MachineJob job = structure?.CurrentJob;

                        if ( job != null && ( job.POIStatusesBlocked.Count > 0 || job.POIStatusesCaused.Count > 0 ) )
                            kv.Value.DuringGame_StructuresThatCanAffectPOI.AddToConstructionList( structure );
                    }

                    ArcenFloatRectangle outerRect = kv.Value.GetOuterRect();
                    Vector3 outerRectCenter = kv.Value.GetCenter();
                    float poiRadius = ((float)Math.Max( outerRect.Width, outerRect.Height ) / 2f);

                    foreach ( MachineStructure structure in SimCommon.StructuresWithJobRange.GetDisplayList() )
                    {
                        MachineJob job = structure?.CurrentJob;

                        if ( job != null && (job.POIStatusesBlocked.Count > 0 || job.POIStatusesCaused.Count > 0) )
                        {
                            float jobEffectRange = 0;
                            bool hasJobEffectRange = job?.GetJobEffectRange( out jobEffectRange ) ?? false;
                            jobEffectRange += poiRadius;
                            Vector3 structureLocation = structure.GetGroundCenterLocation();

                            //if ( kv.Value.GetDisplayName().Contains( "Babanin" ) )
                            //    ArcenDebugging.LogSingleLine( kv.Value.POIName + " poi and job " + job.ID + " dist: " +
                            //        Mathf.Sqrt( (structureLocation - outerRectCenter).GetSquareGroundMagnitude() ) + " range: " + jobEffectRange +
                            //        " radius: " + poiRadius + " structureLoc: " + structureLocation + " rectCenter: " + outerRectCenter, Verbosity.DoNotShow );

                            if ( (structureLocation - outerRectCenter).GetSquareGroundMagnitude() < jobEffectRange * jobEffectRange )
                                kv.Value.DuringGame_StructuresThatCanAffectPOI.AddToConstructionListIfNotAlreadyIn( structure );
                        }
                    }

                    kv.Value.DuringGame_StructuresThatCanAffectPOI.SwitchConstructionToDisplay();
                }

                SwarmTable.Instance.ActiveSwarms.ClearConstructionListForStartingConstruction();
                foreach ( Swarm swarm in SwarmTable.Instance.Rows )
                {
                    swarm.DuringGame_BuildingsInSwarm.SwitchConstructionToDisplay();
                    swarm.DuringGame_TotalSwarmCount.SwitchConstructionToDisplay();

                    if ( swarm.DuringGame_BuildingsInSwarm.GetDisplayList().Count > 0 )
                    {
                        SwarmTable.Instance.ActiveSwarms.AddToConstructionList( swarm );

                        if ( swarm.MessageOnFirstAppears != null && !swarm.MessageOnFirstAppears.DuringGameplay_IsViewingComplete )
                            swarm.MessageOnFirstAppears.DuringGameplay_IsReadyToBeViewed = true;
                        swarm.CityFlagToTripOnFirstAppears?.TripIfNeeded();
                    }
                }
                SwarmTable.Instance.ActiveSwarms.SwitchConstructionToDisplay();

                debugStage = 447000;

                SimPerFullSecond.HandleDiscoveries();

                debugStage = 512000;

                SimTimingInfo.PerSecondBuildingWork.LogCurrentTicks( (int)SimPerFullSecond.SimLoopInnerStopwatch.ElapsedTicks - ticksAtStartOfPhase );
                //this is done in two steps to be slightly more accurate in terms of the instrumentation not delaying a few ms
                ticksAtStartOfPhase = (int)SimPerFullSecond.SimLoopInnerStopwatch.ElapsedTicks;

                debugStage = 518000;

                if ( veryVerboseDebug )
                {
                    foreach ( KeyValuePair<ProfessionType, int> pair in World_People.AllJobs.GetDisplayDict() )
                    {
                        ArcenDebugging.LogSingleLine( "\t" + pair.Key.GetDisplayName() + " -> " + pair.Value, Verbosity.DoNotShow );
                    }
                    ArcenDebugging.LogSingleLine( "All residences:", Verbosity.DoNotShow );
                    foreach ( KeyValuePair<EconomicClassType, int> pair in World_People.ResidentialCapacity.GetDisplayDict() )
                    {
                        ArcenDebugging.LogSingleLine( "\t" + pair.Key.GetDisplayName() + " -> " + pair.Value, Verbosity.DoNotShow );
                    }
                }
                SimPerFullSecond.SimLoopInnerStopwatch.Stop();
                if ( GameSettings.Current.GetBool( "Debug_MainGameMonitorPerformance" ) )
                    ArcenDebugging.LogSingleLine( "Total elapsed time for UpdateSimBuildingsPerSecond: " + SimPerFullSecond.SimLoopInnerStopwatch.ElapsedMilliseconds, Verbosity.DoNotShow );
                SimPerFullSecond.SimLoopInnerStopwatch.Reset();
            }
            catch ( Exception ex )
            {
                ArcenDebugging.LogDebugStageWithStack( "UpdateSimBuildingsPerSecond", debugStage, ex, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region DoAnyPerTurnLogic_SimThread
        public static void DoAnyPerTurnLogic_SimThread( MersenneTwister Rand, bool RunAbbreviatedVersionForGameStart, Stopwatch TurnStopwatch )
        {
            bool debug = GameSettings.Current.GetBool( "Debug_MainGameBuildingDebug" );

            int ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;

            bool veryVerboseDebug = false;

            int maxRand = Int16.MaxValue; //this number is used just to keep serialization smaller
            foreach ( KeyValuePair<int, SimBuilding> kv in BuildingsByID )
            {
                SimBuilding build = kv.Value;
                if ( build == null )
                    continue;
                build.BuildingTurnRandomNumber = Rand.Next( maxRand );
            }

            SimTimingInfo.SimTurnBuildingWork.LogCurrentTicks( (int)TurnStopwatch.ElapsedTicks - ticksAtStartOfPhase );
            ticksAtStartOfPhase = (int)TurnStopwatch.ElapsedTicks;            

            if ( veryVerboseDebug )
            {
                foreach ( KeyValuePair<ProfessionType, int> pair in World_People.AllJobs.GetDisplayDict() )
                {
                    ArcenDebugging.LogSingleLine( "\t" + pair.Key.GetDisplayName() + " -> " + pair.Value, Verbosity.DoNotShow );
                }
                ArcenDebugging.LogSingleLine( "All residences:", Verbosity.DoNotShow );
                foreach ( KeyValuePair<EconomicClassType, int> pair in World_People.ResidentialCapacity.GetDisplayDict() )
                {
                    ArcenDebugging.LogSingleLine( "\t" + pair.Key.GetDisplayName() + " -> " + pair.Value, Verbosity.DoNotShow );
                }
            }

        }
        #endregion

        #region AddBuilding
        public static void AddBuilding( SimBuilding building )
        {
            AddBuilding_InternalListsOnly( building );
        }

        private static void AddBuilding_InternalListsOnly( SimBuilding building )
        {
            if ( building == null )
            {
                ArcenDebugging.LogSingleLine( "Building add fail full null!", Verbosity.ShowAsError );
                return;
            }
            if ( building.Item == null )
            {
                ArcenDebugging.LogSingleLine( "Building add fail building.Item null!", Verbosity.ShowAsError );
                return;
            }
            if ( !BuildingsByID.TryAdd( building.BuildingID, building ) )
                ArcenDebugging.LogWithStack( "Duplicate BuildingsByID building.BuildingID '" + building.BuildingID + "' in SimWorld", Verbosity.ShowAsError );
        }
        #endregion

        #region GetRandomBuilding
        public static SimBuilding GetRandomBuilding( MersenneTwister RandToUse )
        {
            int retries = 100;
            while ( retries-- > 0 )
            {
                int rand = RandToUse.Next( 1, NextBuildingID );
                if ( BuildingsByID.TryGetValue( rand, out SimBuilding build ) )
                    return build;
            }
            return null;
        }
        #endregion

        #region CreateSingleSimBuildingFromMapItem
        public ISimBuilding CreateSingleSimBuildingFromMapItem( MapItem item, MersenneTwister RandToUse )
        {
            SimBuilding building = SimBuilding.CreateSimBuilding( item, Interlocked.Increment( ref NextBuildingID ), true, RandToUse );
            if ( building != null )
                AddBuilding_InternalListsOnly( building );
            return building;
        }
        #endregion

        #region FillAnyMissingSimBuildings_FromSimThread
        public static void FillAnyMissingSimBuildings_FromSimThread( MersenneTwister RandToUse )
        {
            FillBuildingsList_UnassignedOnly( workingMapItems_MainThread, false, out int SkippedBuildings, out int AddedBuildings, out int CellCountChecked );
            int missingBuildingsFilledIn = workingMapItems_MainThread.Count;

            for ( int i = 0; i < workingMapItems_MainThread.Count; i++ )
            {
                SimBuilding building = SimBuilding.CreateSimBuilding( workingMapItems_MainThread[i], Interlocked.Increment( ref NextBuildingID ), true, RandToUse );
                if ( building != null )
                    AddBuilding_InternalListsOnly( building );
            }

            workingMapItems_MainThread.Clear();

            if ( missingBuildingsFilledIn > 0 )
                ArcenDebugging.LogSingleLine( "Finished FillAnyMissingSimBuildings, we had " + missingBuildingsFilledIn + " new buildings, and " + 
                    BuildingsByID.Count + " total items. SkippedBuildings: " + SkippedBuildings + 
                    " AddedBuildings: " + AddedBuildings  + " CellCountChecked: " + CellCountChecked, Verbosity.DoNotShow );

            if ( BuildingsByID.Count == 0 )
            {
                ArcenDebugging.LogSingleLine( "Was unable to find any buildings.  Uh oh. SkippedBuildings: " + SkippedBuildings +
                    " AddedBuildings: " + AddedBuildings + " CellCountChecked: " + CellCountChecked, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region GetBuildingByID
        public static SimBuilding GetBuildingByID( int id )
        {
            return BuildingsByID[id];
        }
        #endregion

        #region FillBuildingsList_UnassignedOnly
        public static void FillBuildingsList_UnassignedOnly( List<MapItem> output, bool debug, out int SkippedBuildings, out int AddedBuildings, out int CellCountChecked )
        {
            output.Clear();
            int skipped = 0;
            int added = 0;
            int cellCountChecked = 0;
            foreach ( MapTile tile in CityMap.Tiles )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine( "We have " + tile.CellsList.Count + " cells on tile " + tile.ToDebugString(), Verbosity.DoNotShow );
                for ( int i = 0; i < tile.CellsList.Count; i++ )
                {
                    MapCell cell = tile.CellsList[i];
                    if ( debug )
                        ArcenDebugging.LogSingleLine( "\tTile Cell " + i + " has " + cell.BuildingDict.Count + " buildings", Verbosity.DoNotShow );
                    cellCountChecked++;
                    foreach ( KeyValuePair<int, MapItem> kv in cell.BuildingDict )
                    {
                        MapItem item = kv.Value;
                        if ( item.SimBuilding != null )
                        {
                            skipped++;
                            continue; //if this MapItem already has a SimBuilding linked to it, then nothing new to do.
                        }

                        if ( debug )
                            ArcenDebugging.LogSingleLine( "\t\t" + item.Type + " id " + item.MapItemID + " and we have " + (item.Type.Building == null ? 0 : 1) + " prefabs here", Verbosity.DoNotShow );
                        output.Add( item );
                        added++;
                    }
                }
            }

            SkippedBuildings = skipped;
            AddedBuildings = added;
            CellCountChecked = cellCountChecked;
        }
        #endregion

        #region ISimWorld_Buildings
        public DictionaryView<int, ISimBuilding> GetAllBuildings()
        {
            return DictionaryView<int, ISimBuilding>.Create( BuildingsByID );
        }

        public ListView<ISimBuilding> GetBuildingsWithMachineStructures()
        {
            return ListView<ISimBuilding>.Create( BuildingsWithMachineStructures.GetDisplayList() );
        }

        public ListView<ISimBuilding> GetBuildingsWithBrokenMachineStructures()
        {
            return ListView<ISimBuilding>.Create( BuildingsWithBrokenMachineStructures.GetDisplayList() );
        }

        public ListView<ISimBuilding> GetBuildingsWithPausedMachineStructures()
        {
            return ListView<ISimBuilding>.Create( BuildingsWithPausedMachineStructures.GetDisplayList() );
        }

        public ListView<ISimBuilding> GetBuildingsWithMachineStructuresOutOfNetwork()
        {
            return ListView<ISimBuilding>.Create( BuildingsWithMachineStructuresOutOfNetwork.GetDisplayList() );
        }

        public ListView<ISimBuilding> GetBuildingsWithDamagedMachineStructures()
        {
            return ListView<ISimBuilding>.Create( BuildingsWithDamagedMachineStructures.GetDisplayList() );
        }
        #endregion
    }
}

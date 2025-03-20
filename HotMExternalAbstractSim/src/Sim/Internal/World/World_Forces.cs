using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Central world data and lookups about machine and NPC forces in general
    /// </summary>
    internal class World_Forces : ISimWorld_Forces
    {
        public static World_Forces QueryInstance = new World_Forces();

        //
        //Serialized data
        //-----------------------------------------------------

        public static readonly ConcurrentDictionary<int, MachineUnit> MachineUnitsByID = ConcurrentDictionary<int, MachineUnit>.Create_WillNeverBeGCed( 8, 400, "World_Forces-UnitsByID" );
        public static readonly ConcurrentDictionary<int, NPCUnit> AllNPCUnitsByID = ConcurrentDictionary<int, NPCUnit>.Create_WillNeverBeGCed( 8, 5000, "World_Forces-AllNPCUnitsByID" );
        public static readonly ConcurrentDictionary<int, NPCUnit> ManagedNPCUnitsByID = ConcurrentDictionary<int, NPCUnit>.Create_WillNeverBeGCed( 8, 5000, "World_Forces-ManagedNPCUnitsByID" );
        public static readonly ConcurrentDictionary<int, NPCUnit> CityConflictNPCUnitsByID = ConcurrentDictionary<int, NPCUnit>.Create_WillNeverBeGCed( 8, 5000, "World_Forces-CityConflictNPCUnitsByID" );

        public static readonly ConcurrentDictionary<int, MachineVehicle> MachineVehiclesByID = ConcurrentDictionary<int, MachineVehicle>.Create_WillNeverBeGCed( 8, 400, "World_Forces-VehiclesByID" );

        //
        //Nonserialized data
        //-----------------------------------------------------
        public static readonly DoubleBufferedList<MachineUnit> UnitsDeployed = DoubleBufferedList<MachineUnit>.Create_WillNeverBeGCed( 50, "World_Forces-UnitsDeployed", 20 );
        public static readonly DoubleBufferedList<MachineUnit> UnitsRiding = DoubleBufferedList<MachineUnit>.Create_WillNeverBeGCed( 50, "World_Forces-UnitsRiding", 20 );
        public static readonly DoubleBufferedList<ISimMachineActor> MachineActorsAutomated = DoubleBufferedList<ISimMachineActor>.Create_WillNeverBeGCed( 50, "World_Forces-MachineActorsAutomated", 20 );
        public static readonly DoubleBufferedList<ISimMapActor> LivingMapActorsNotBeingABasicGuard = DoubleBufferedList<ISimMapActor>.Create_WillNeverBeGCed( 50, "World_Forces-LivingMapActorsNotBeingABasicGuard", 20 );
        public static readonly DoubleBufferedList<ISimMapActor> AllLivingMapActors = DoubleBufferedList<ISimMapActor>.Create_WillNeverBeGCed( 50, "World_Forces-AllLivingMapActors", 20 );
        private static readonly List<SimBuilding> workingBuildings = List<SimBuilding>.Create_WillNeverBeGCed( 400, "World_Forces-workingBuildings" );

        public static void OnGameClear()
        {
            MachineUnitsByID.Clear();
            AllNPCUnitsByID.Clear();
            ManagedNPCUnitsByID.Clear();
            CityConflictNPCUnitsByID.Clear();

            MachineVehiclesByID.Clear();

            UnitsDeployed.ClearAllVersions();
            UnitsRiding.ClearAllVersions();
            LivingMapActorsNotBeingABasicGuard.ClearAllVersions();
            AllLivingMapActors.ClearAllVersions();
            MachineActorsAutomated.ClearAllVersions();
            workingBuildings.Clear();
        }

        #region Serialization
        public static void Serialize( ArcenFileSerializer Serializer )
        {
            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
            {
                MachineVehicle vehicle = kv.Value;
                if ( vehicle == null || vehicle.IsFullDead )
                    continue;

                Serializer.StartObject( "MachineVehicle" );
                vehicle.Serialize( Serializer );
                Serializer.EndObject( "MachineVehicle" );
            }

            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
            {
                MachineUnit unit = kv.Value;
                if ( unit == null || unit.IsFullDead )
                    continue;

                Serializer.StartObject( "MachineUnit" );
                unit.Serialize( Serializer );
                Serializer.EndObject( "MachineUnit" );
            }

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
            {
                NPCUnit unit = kv.Value;
                if ( unit == null || unit.IsFullDead )
                    continue;

                Serializer.StartObject( "NPCUnit" );
                unit.Serialize( Serializer );
                Serializer.EndObject( "NPCUnit" );

            }
        }

        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {
            //must do vehicles before machine units, so that riding ones can just go ahead and find them
            if ( Data.ChildLayersByName.TryGetValue( "MachineVehicle", out List<DeserializedObjectLayer> machineVehicles ) )
            {
                if ( machineVehicles.Count > 0 )
                {
                    for ( int i = 0; i < machineVehicles.Count; i++ )
                    {
                        DeserializedObjectLayer machineVehicleData = machineVehicles[i];
                        MachineVehicle.Deserialize( machineVehicleData ); //must be deserialized after all the units
                    }
                }
            }

            if ( Data.ChildLayersByName.TryGetValue( "MachineUnit", out List<DeserializedObjectLayer> machineUnits ) )
            {
                if ( machineUnits.Count > 0 )
                {
                    for ( int i = 0; i < machineUnits.Count; i++ )
                    {
                        DeserializedObjectLayer machineUnitData = machineUnits[i];
                        MachineUnit.Deserialize( machineUnitData );
                    }
                }
            }

            //int npcDesiredCount = 0;
            if ( Data.ChildLayersByName.TryGetValue( "NPCUnit", out List<DeserializedObjectLayer> npcUnits ) )
            {
                //npcDesiredCount = npcUnits.Count;
                if ( npcUnits.Count > 0 )
                {
                    for ( int i = 0; i < npcUnits.Count; i++ )
                    {
                        DeserializedObjectLayer npcUnitData = npcUnits[i];
                        NPCUnit.Deserialize( npcUnitData );
                    }
                }
            }

            //ArcenDebugging.LogSingleLine( "Wanted to deserialize " + npcDesiredCount + " npcs, and got " + AllNPCUnitsByID.Count + " out of that.", Verbosity.DoNotShow );   
        }

        public static void FinishDeserializeLate()
        {
            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
                kv.Value.FinishDeserializeLate();

            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
                kv.Value.FinishDeserializeLate();

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
                kv.Value.FinishDeserializeLate();
        }
        #endregion

        #region DoAnyPerFrameLogic
        public static void DoAnyPerFrameLogic()
        {
            GrantInitialForcesIfHaveNotYet( Engine_Universal.PermanentQualityRandom );

            //we DO do this in any section, because these are reacting to us.

            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
                kv.Value.DoPerFrameLogic();
            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
                kv.Value.DoPerFrameLogic();

            SimCommon.NPCsMoving_MainThreadOnly.ClearConstructionListForStartingConstruction();
            SimCommon.NPCsWithTargets_MainThreadOnly.ClearConstructionListForStartingConstruction();

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
                kv.Value.DoNPCPerFrameLogic();

            SimCommon.NPCsMoving_MainThreadOnly.SwitchConstructionToDisplay();
            SimCommon.NPCsWithTargets_MainThreadOnly.SwitchConstructionToDisplay();
        }
        #endregion

        public static void DoPerQuarterSecondLogic()
        {
            RecalculateActiveForces();
        }

        #region RecalculateActiveForces
        public static void RecalculateActiveForces()
        {
            int totalVehicleCount = 0;
            int totalVehicleCapacity = 0;
            int totalAndroidCount = 0;
            int totalAndroidCapacity = 0;
            int totalMechCount = 0;
            int totalMechCapacity = 0;

            int totalBulkAndroidSquads = 0;
            int totalBulkAndroidSquadCapacity = 0;
            int totalCapturedSquads = 0;
            int totalCapturedSquadCapacity = 0;

            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
            {
                totalVehicleCount++;
                if ( kv.Value?.CurrentActionOverTime?.Type?.BlocksUnitCountingTowardCap ?? false )
                { } //ooh, no cost!
                else
                    totalVehicleCapacity += kv.Value?.VehicleType?.VehicleCapacityCost??0;
            }
            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
            {
                if ( kv.Value.UnitType.IsConsideredAndroid )
                {
                    totalAndroidCount++;
                    if ( kv.Value?.CurrentActionOverTime?.Type?.BlocksUnitCountingTowardCap??false )
                    { } //ooh, no cost!
                    else
                        totalAndroidCapacity += kv.Value?.UnitType?.UnitCapacityCost ?? 0;
                }
                if ( kv.Value.UnitType.IsConsideredMech )
                {
                    totalMechCount++;
                    if ( kv.Value?.CurrentActionOverTime?.Type?.BlocksUnitCountingTowardCap ?? false )
                    { } //ooh, no cost!
                    else
                        totalMechCapacity += kv.Value?.UnitType?.UnitCapacityCost ?? 0;
                }
            }


            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
            {
                NPCUnitType unitType = kv.Value.UnitType;
                if ( unitType == null )
                    continue;
                if ( kv.Value.IsFullDead || !kv.Value.GetIsPlayerControlled() )
                    continue;
                if ( unitType.CostsToCreateIfBulkAndroid.Count > 0 )
                {
                    totalBulkAndroidSquads++;
                    totalBulkAndroidSquadCapacity += unitType.BulkUnitCapacityRequired;
                }
                else
                {
                    totalCapturedSquads++;
                    totalCapturedSquadCapacity += unitType.CapturedUnitCapacityRequired;
                }
            }

            SimCommon.TotalOnline_Androids = totalAndroidCount;
            SimCommon.TotalCapacityUsed_Androids = totalAndroidCapacity;
            SimCommon.TotalOnline_Mechs = totalMechCount;
            SimCommon.TotalCapacityUsed_Mechs = totalMechCapacity;
            SimCommon.TotalOnline_Vehicles = totalVehicleCount;
            SimCommon.TotalCapacityUsed_Vehicles = totalVehicleCapacity;

            SimCommon.TotalOnlineBulkUnitSquads = totalBulkAndroidSquads;
            SimCommon.TotalBulkUnitSquadCapacityUsed = totalBulkAndroidSquadCapacity;
            SimCommon.TotalOnlineCapturedUnitSquads = totalCapturedSquads;
            SimCommon.TotalCapturedUnitSquadCapacityUsed = totalCapturedSquadCapacity;
        }
        #endregion

        public static void DoPerSecondLogic( bool OnlyRecalculateCaches )
        {
            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
            {
                kv.Value.DoPerSecondRecalculations( false );
            }
            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
            {
                kv.Value.DoPerSecondRecalculations( false );
            }

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
            {
                kv.Value.DoPerSecondRecalculations( false );
            }

            //must be done before DoPerSecondRecalculationsForStructure!
            foreach ( UpgradeInt upgrade in UpgradeIntTable.Instance.Rows )
                upgrade.DoPerSecondRecalculationsForUpgradeInt();

            foreach ( MachineJob job in MachineJobTable.Instance.Rows )
                job.DoPerSecondMachineJobRecalculations();
            foreach ( MachineStructureType structureType in MachineStructureTypeTable.Instance.Rows )
                structureType.DoPerSecondMachineStructureRecalculations();

            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                kv.Value.DoPerSecondRecalculationsForStructure( false );

            foreach ( UnitEquipmentType equipment in UnitEquipmentTypeTable.Instance.Rows )
                equipment.DoPerSecondEquipmentRecalculations();

            foreach ( AbilityType abilityType in AbilityTypeTable.Instance.Rows )
                abilityType.DoPerSecondAbilityTypeRecalculations();
            foreach ( ActionOverTimeType actionOverTimeType in ActionOverTimeTypeTable.Instance.Rows )
                actionOverTimeType.DoPerSecondActionOverTimeTypeRecalculations();

            if ( !OnlyRecalculateCaches )
            {

            }
        }

        #region GrantInitialForcesIfHaveNotYet
        private static void GrantInitialForcesIfHaveNotYet( MersenneTwister Rand )
        {
            if ( SimCommon.HasPlayerBeenGrantedInitialForces )
                return;

            MapCell startingCell = CityMap.ChosenStartingCell;
            if ( startingCell == null )
                return; //cannot do this yet if we don't have a starting cell yet!

            //SimCommon.HasPlayerBeenGrantedInitialForces = true; //this needs to be done lower!

            bool isPastPrologue = SimMetagame.AllTimelines.Count > 1 //if not our first timeline, then we are definitely past the prologue
                || SimMetagame.CurrentChapterNumber > 0; //or if we're past chapter zero
            if ( !isPastPrologue )
            {
                if ( (SimMetagame.StartType?.SkipToChapter ?? 0) > 0 )
                    isPastPrologue = true;
            }
            bool isPastChapterOne = SimMetagame.CurrentChapterNumber > 1; //if past chapter one, we are of course past chapter one
            if ( !isPastChapterOne )
            {
                if ( (SimMetagame.StartType?.SkipToChapter ?? 0) > 1 )
                {
                    isPastChapterOne = true;
                    isPastPrologue = true;
                }
            }

            workingBuildings.Clear();
            foreach ( KeyValuePair<int, MapItem> kv in startingCell.BuildingDict )
            {
                MapItem building = kv.Value;
                SimBuilding sBuild = building?.SimBuilding as SimBuilding;
                if ( sBuild == null )
                    continue;

                if ( isPastPrologue )
                {
                    if ( sBuild.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_FirstTower ?? false )
                    {
                        if ( sBuild.CalculateLocationSecurityClearanceInt() <= 0 )
                            workingBuildings.Add( sBuild );
                    }
                }
                else
                {
                    if ( sBuild.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_Prologue ?? false )
                    {
                        if ( sBuild.CalculateLocationSecurityClearanceInt() <= 0 )
                            workingBuildings.Add( sBuild );
                    }
                }
            }

            if ( workingBuildings.Count == 0  )
            {
                ArcenDebugging.LogSingleLine( "startingCell: Failure to find any valid buildings that are a starting spot for player units or the machine tower!", Verbosity.ShowAsError );
                return;
            }

            bool hasFocusedCamera = false;
            if ( isPastPrologue )
            {
                //erupt the machine tower at the specified building
                SimBuilding targetBuilding = workingBuildings.GetRandom( Rand );
                //targetBuilding.SetPrefab( CommonRefs.MachineTowers.DrawRandomItem( Rand ).Building, 32f,
                //    ParticleSoundRefs.MachineTowerEruptionStart, ParticleSoundRefs.MachineTowerEruptionEnd );

                //MachineNetwork net = MachineNetwork.CreateNew( NetworkHelper.GenerateNewNetworkName( Rand ), targetBuilding );

                //if ( net.Tower != null )
                {
                    hasFocusedCamera = true;
                    VisManagerVeryBase.Instance.MainCamera_ResetOrientation();
                    VisManagerVeryBase.Instance.MainCamera_FocusLockCameraOnPosition( targetBuilding, false );
                }

                workingBuildings.Clear();
                foreach ( KeyValuePair<int, MapItem> kv in targetBuilding.GetParentCell().BuildingDict )
                {
                    MapItem building = kv.Value;
                    if ( building.SimBuilding == targetBuilding || building.CalculateLocationSecurityClearanceInt() > 0 ||
                        building.SimBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        continue;
                    workingBuildings.Add( building.SimBuilding as SimBuilding );
                }

                if ( workingBuildings.Count == 0 )
                {
                    ArcenDebugging.LogSingleLine( "Failed to find spot to seed player units near the machine tower!", Verbosity.ShowAsError );
                    return;
                }
            }

            MachineUnit firstUnit = null;

            foreach ( MachineUnitType unitType in MachineUnitTypeTable.Instance.Rows )
            {
                if ( unitType.IsGivenInitiallyToPlayers && workingBuildings.Count > 0 )
                {
                    SimBuilding building = workingBuildings.GetRandom( Rand );
                    workingBuildings.Remove( building );
                    startingCell.ParentTile.HasEverBeenExplored = true;
                    startingCell.ParentTile.IsTileContainingMachineActors.SetBothAtOnce( true );

                    MachineUnit unit = MachineUnit.CreateNew( unitType, building, -1, Rand, true, false );
                    if ( unit != null )
                    {
                        MachineUnitsByID[unit.UnitID] = unit;
                        SimCommon.AllActorsByID[unit.UnitID] = unit;
                        if ( firstUnit == null )
                            firstUnit = unit;
                        if ( unit.UnitType.IsConsideredMech )
                            unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                        else
                            unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;
                        unit.UnitName = unit.UnitType.NameWhenFirstUnit.Text;
                        unit.CurrentRegistration = CommonRefs.RogueResearchersTag.CohortsList.GetRandom( Rand );

                        building.CurrentOccupyingUnit = unit;
                        unit.SetActualContainerLocation( building );
                        unit.SetStartingActionPoints( true );

                        CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                            RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );

                        if ( !hasFocusedCamera )
                        {
                            hasFocusedCamera = true;
                            VisManagerVeryBase.Instance.MainCamera_ResetOrientation();
                            VisManagerVeryBase.Instance.MainCamera_FocusLockCameraOnPosition( unit, false );
                        }
                    }

                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                }
            }

            if ( firstUnit == null )
                ArcenDebugging.LogSingleLine( "No unit was granted!  The player will not be able to continue!", Verbosity.ShowAsError );
            else
            {
                if ( isPastPrologue )
                {
                    #region Extra Units If Skipping Prologue
                    foreach ( MachineUnitType unitType in MachineUnitTypeTable.Instance.Rows )
                    {
                        if ( unitType.IsGivenInitiallyToPlayersIfSkippingChapterZero )
                        {
                            ISimMachineUnit newUnit = firstUnit.TryCreateNewMachineUnitAsCloseAsPossibleToThisOne( unitType, string.Empty, CellRange.CellAndAdjacent2x, Rand, true, CollisionRule.Strict, true );
                            if ( newUnit != null )
                                newUnit.CurrentRegistration = CommonRefs.RogueResearchersTag.CohortsList.GetRandom( Rand );
                        }
                    }

                    foreach ( MachineVehicleType vehicleType in MachineVehicleTypeTable.Instance.Rows )
                    {
                        if ( vehicleType.IsGivenInitiallyToPlayersIfSkippingChapterZero )
                        {
                            ISimMachineVehicle newVehicle = firstUnit.TryCreateNewVehicleAsCloseAsPossibleToThisOne( vehicleType, string.Empty, Rand, true, CollisionRule.Strict, true );
                            if ( newVehicle != null )
                                newVehicle.CurrentRegistration = CommonRefs.RogueResearchersTag.CohortsList.GetRandom( Rand );
                        }
                    }
                    #endregion

                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                    SimCommon.HandleAnySkipPrologueStuff( true );

                    if ( isPastChapterOne )
                    {
                        SimCommon.HandleAnySkipChapterOneStuff( true );
                        SimCommon.HandleAnyHigherRankCityInitialStuff();
                        if ( SimMetagame.CurrentChapterNumber < 2 )
                            SimMetagame.AdvanceToChapterTwoIfNotThereYet();
                    }
                    else
                    {
                        if ( SimMetagame.CurrentChapterNumber < 1 )
                            SimMetagame.AdvanceToNextChapter();
                    }
                }
                else
                {
                    NPCEventTable.Instance.GetRowByID( "FirstEvent" ).StartThisEvent( firstUnit, true );
                }
            }

            workingBuildings.Clear();

            RecalculateActiveForces(); //this has to be done first!
            SimCommon.HasPlayerBeenGrantedInitialForces = true;
        }
        #endregion

        #region TryCreateMachineUnitAtRandomMapSeedSpotOnCell
        public static ISimMachineUnit TryCreateMachineUnitAtRandomMapSeedSpotOnCell( MapCell cell, MachineUnitType UnitTypeToGrant, MersenneTwister Rand, 
            string NewUnitOverridingName, bool StartAsReadyToAct, CollisionRule Rule )
        {
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
            {
                if ( spot.CurrentOccupyingUnit == null )
                {
                    ISimMachineUnit result = QueryInstance.CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                    if ( result != null )
                        return result;
                }
            }
            return null;
        }
        #endregion

        #region TryCreateMachineVehicleAtRandomMapSeedSpotOnCell
        public static ISimMachineVehicle TryCreateMachineVehicleAtRandomMapSeedSpotOnCell( MapCell cell, MachineVehicleType VehicleTypeToGrant, MersenneTwister Rand, 
            string NewVehicleOverridingName, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
            {
                ISimMachineVehicle result = QueryInstance.TryCreateNewMachineVehicleAsCloseAsPossibleToLocation( spot.Position, cell, VehicleTypeToGrant,
                    NewVehicleOverridingName, CellRange.CellAndAdjacent2x, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                if ( result != null )
                    return result;
            }
            return null;
        }
        #endregion

        #region TryCreateNPCUnitAtRandomMapSeedSpotOnCell
        public static ISimNPCUnit TryCreateNPCUnitAtRandomMapSeedSpotOnCell( MapCell cell, NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, 
            NPCUnitStance Stance, float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, 
            MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
            {
                if ( spot.CurrentOccupyingUnit == null )
                {
                    ISimNPCUnit result = QueryInstance.CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance,
                        SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, spot, AllowMechWiggle, Rand, null, -1, null, null, Rule, CreationReason );
                    if ( result != null ) 
                        return result;
                }
            }
            return null;
        }
        #endregion

        #region CreateNewMachineUnitAtOutdoorSpot
        public ISimMachineUnit CreateNewMachineUnitAtOutdoorSpot( MachineUnitType Type, MapOutdoorSpot spot, MersenneTwister Rand, 
            string OverridingUnitName, bool StartAsReadyToAct, CollisionRule Rule )
        {
            if ( Type == null || spot == null ) 
                return null;

            MachineUnit unit = null;
            if ( Type.IsConsideredMech )
            {
                Vector3 location = spot.Position.ReplaceY( 0 );
                float rotation = Rand.Next( 0, 360 );
                ProposedMachineMechLocation proposedLoc = new ProposedMachineMechLocation( Type, location, rotation );
                bool foundValidSpot = false;
                for ( int outerAttempt = 0; outerAttempt < 1200; outerAttempt++ )
                {
                    location = spot.Position.ReplaceY( 0 );
                    proposedLoc.Position = location;
                    float wiggleMin = ((float)outerAttempt / 300f ) + 0.1f;
                    float wiggleMax = ((float)outerAttempt / 200f) + 0.4f;
                    if ( outerAttempt > 600 )
                    {
                        wiggleMin = ((float)outerAttempt / 100f) + 0.1f;
                        wiggleMax = ((float)outerAttempt / 50f) + 0.4f;
                    }

                    for ( int attempts = 0; attempts < 20; attempts++ )
                    {
                        if ( CityMap.CalculateIsValidLocationForCollidable( proposedLoc, proposedLoc.Position, rotation, true,
                            CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, false, Rule ) )
                        {
                            foundValidSpot = true;
                            break; //this was a valid spot, hooray!
                        }
                        //if we are here, this was NOT a valid spot, boo.
                        if ( Rand.NextBool() )
                            location.x += Rand.NextFloat( wiggleMin, wiggleMax );
                        else
                            location.x += Rand.NextFloat( wiggleMin, wiggleMax );

                        if ( Rand.NextBool() )
                            location.z += Rand.NextFloat( wiggleMin, wiggleMax );
                        else
                            location.z += Rand.NextFloat( wiggleMin, wiggleMax );

                        rotation = Rand.Next( 0, 360 );
                        proposedLoc.RotationY = rotation;

                        proposedLoc.Position = location;
                    }
                    if ( foundValidSpot )
                        break;
                }
                if ( !foundValidSpot )
                    return null;

                unit = MachineUnit.CreateNew( Type, null, -1, Rand, true, false );
                unit.SetRotationY( rotation );
                unit.SetActualGroundLocation( location );
            }
            else
            {
                unit = MachineUnit.CreateNew( Type, spot, -1, Rand, true, false );
                if ( unit != null )
                {
                    spot.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( spot );
                }
            }

            if ( unit != null )
            {
                MachineUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;
                if ( unit.UnitType.IsConsideredMech )
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                else
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;

                unit.SetStartingActionPoints( StartAsReadyToAct );

                bool generateName = true;
                if ( OverridingUnitName.Length > 0 )
                {
                    if ( !GetHasMachineUnitNameEverBeenUsed( OverridingUnitName ) )
                    {
                        unit.UnitName = OverridingUnitName;
                        generateName = false;
                    }
                    else //this name was already used, so use something else
                        generateName = true;
                }
                if ( generateName )
                {
                    unit.UnitName = unit.UnitType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineUnit( 
                        unit.UnitType.NameStyle, unit, Rand );
                }

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewMachineVehicleAtOutdoorSpot
        public ISimMachineVehicle CreateNewMachineVehicleAtOutdoorSpot( MachineVehicleType Type, MapOutdoorSpot spot, MersenneTwister Rand,
            string OverridingVehicleName, bool StartAsReadyToAct, CollisionRule Rule )
        {
            if ( Type == null || spot == null )
                return null;

            MachineVehicle vehicle = null;
            {
                float requiredHeight = Type.InitialHeight;
                Vector3 location = spot.Position.ReplaceY( requiredHeight );
                ProposedVehicleLocation proposedLoc = new ProposedVehicleLocation( Type, location );
                bool foundValidSpot = false;
                for ( int outerAttempt = 0; outerAttempt < 1200; outerAttempt++ )
                {
                    location = spot.Position.ReplaceY( requiredHeight );
                    proposedLoc.Position = location;
                    float wiggleMin = ((float)outerAttempt / 300f) + 0.1f;
                    float wiggleMax = ((float)outerAttempt / 200f) + 0.4f;
                    if ( outerAttempt > 600 )
                    {
                        wiggleMin = ((float)outerAttempt / 100f) + 0.1f;
                        wiggleMax = ((float)outerAttempt / 50f) + 0.4f;
                    }

                    for ( int attempts = 0; attempts < 20; attempts++ )
                    {
                        if ( CityMap.CalculateIsValidLocationForCollidable( proposedLoc, proposedLoc.Position, 0, true, CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, Rule ) )
                        {
                            foundValidSpot = true;
                            break; //this was a valid spot, hooray!
                        }
                        //if we are here, this was NOT a valid spot, boo.
                        if ( Rand.NextBool() )
                            location.x += Rand.NextFloat( wiggleMin, wiggleMax );
                        else
                            location.x += Rand.NextFloat( wiggleMin, wiggleMax );

                        if ( Rand.NextBool() )
                            location.z += Rand.NextFloat( wiggleMin, wiggleMax );
                        else
                            location.z += Rand.NextFloat( wiggleMin, wiggleMax );

                        proposedLoc.Position = location;
                    }
                    if ( foundValidSpot )
                        break;
                }
                if ( !foundValidSpot )
                    return null;

                vehicle = MachineVehicle.CreateNew( Type, location, -1, Rand, true, false );
                vehicle.SetWorldLocation( location ); //the initial height was overriding it, so set it again
            }

            if ( vehicle != null )
            {
                MachineVehiclesByID[vehicle.VehicleID] = vehicle;
                vehicle.SetStartingActionPoints( StartAsReadyToAct );
                SimCommon.AllActorsByID[vehicle.ActorID] = vehicle;

                bool generateName = true;
                if ( OverridingVehicleName.Length > 0 )
                {
                    if ( !GetHasPartialVehicleNameEverBeenUsed( OverridingVehicleName ) )
                    {
                        vehicle.VehicleName = OverridingVehicleName;
                        generateName = false;
                    }
                    else //this name was already used, so use something else
                        generateName = true;
                }
                if ( generateName )
                {
                    vehicle.VehicleName = vehicle.VehicleType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineVehicle(
                        vehicle.VehicleType.NameStyle, vehicle, Rand );
                }

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( vehicle, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return vehicle;
        }
        #endregion

        #region CreateNewMachineUnitAtBuilding
        public ISimMachineUnit CreateNewMachineUnitAtBuilding( MachineUnitType Type, MapItem building, MersenneTwister Rand,
            string OverridingUnitName, bool StartAsReadyToAct )
        {
            if ( Type == null || building == null )
                return null;
            ISimBuilding simBuilding = building.SimBuilding;
            if ( simBuilding == null || simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                return null;

            MachineUnit unit = null;
            if ( Type.IsConsideredMech )
                return null; //mechs cannot be created at buildings!
            else
            {
                unit = MachineUnit.CreateNew( Type, simBuilding, -1, Rand, true, false );
                if ( unit != null )
                {
                    simBuilding.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( simBuilding );
                }
            }

            if ( unit != null )
            {
                MachineUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;
                if ( unit.UnitType.IsConsideredMech )
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                else
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;

                unit.SetStartingActionPoints( StartAsReadyToAct );

                bool generateName = true;
                if ( OverridingUnitName.Length > 0 )
                {
                    if ( !GetHasMachineUnitNameEverBeenUsed( OverridingUnitName ) )
                    {
                        unit.UnitName = OverridingUnitName;
                        generateName = false;
                    }
                    else //this name was already used, so use something else
                        generateName = true;
                }
                if ( generateName )
                {
                    unit.UnitName = unit.UnitType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineUnit(
                        unit.UnitType.NameStyle, unit, Rand );
                }

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewMachineUnitInVehicle
        public ISimMachineUnit CreateNewMachineUnitInVehicle( MachineUnitType Type, ISimMachineVehicle Vehicle, MersenneTwister Rand, string OverridingUnitName, 
            bool StartAsReadyToAct )
        {
            if ( Type == null || Vehicle == null )
                return null;

            MachineUnit unit = MachineUnit.CreateNew( Type, Vehicle, -1, Rand, true, false );
            if ( unit != null )
            {
                MachineUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;
                if ( unit.UnitType.IsConsideredMech )
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                else
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;

                unit.SetStartingActionPoints( StartAsReadyToAct );

                bool generateName = true;
                if ( OverridingUnitName.Length > 0 )
                {
                    if ( !GetHasMachineUnitNameEverBeenUsed( OverridingUnitName ) )
                    {
                        unit.UnitName = OverridingUnitName;
                        generateName = false;
                    }
                    else //this name was already used, so use something else
                        generateName = true;
                }
                if ( generateName )
                {
                    unit.UnitName = unit.UnitType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineUnit(
                        unit.UnitType.NameStyle, unit, Rand );
                }
                unit.SetActualContainerLocation( Vehicle );

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewNPCUnitAtOutdoorSpot
        public ISimNPCUnit CreateNewNPCUnitAtOutdoorSpot( NPCUnitType Type, NPCCohort FromCohort, NPCUnitStance Stance,
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, MapOutdoorSpot spot, bool AllowMechWiggle, MersenneTwister Rand,
            ISimBuilding ManagerStartLocation, int ManagerOriginalMachineActorFocusID, NPCManagedUnit IsManagedUnit, CityConflictUnit IsCityConflictUnit, CollisionRule Rule, string CreationReason )
        {
            return CreateNewNPCUnitAtOutdoorSpot( Type, FromCohort, Stance,
                SquadSizeMultiplier, RotateToFaceThisPoint, RotationYIfZeroPlus, spot, AllowMechWiggle, false, Rand,
                ManagerStartLocation, ManagerOriginalMachineActorFocusID, IsManagedUnit, IsCityConflictUnit, Rule, CreationReason );
        }

        public ISimNPCUnit CreateNewNPCUnitAtOutdoorSpot( NPCUnitType Type, NPCCohort FromCohort, NPCUnitStance Stance, 
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, MapOutdoorSpot spot, bool AllowMechWiggle, bool IgnoreAllCollisions, MersenneTwister Rand,
            ISimBuilding ManagerStartLocation, int ManagerOriginalMachineActorFocusID, NPCManagedUnit IsManagedUnit, CityConflictUnit IsCityConflictUnit, CollisionRule Rule, string CreationReason )
        {
            if ( Type == null || spot == null )
                return null;

            NPCUnit unit = null;
            bool hasSetRotation = false;
            if ( Type.IsMechStyleMovement && !IgnoreAllCollisions )
            {
                Vector3 location = spot.Position.ReplaceY( 0 );
                float rotationY = RotationYIfZeroPlus >= 0 ? RotationYIfZeroPlus : location.GetRotationYTo( RotateToFaceThisPoint,
                    Rand.Next( 0, 360 ) );
                hasSetRotation = true;
                ProposedNPCMechLocation proposedLoc = new ProposedNPCMechLocation( Type, location, rotationY );
                if ( !AllowMechWiggle )
                {
                    if ( !CityMap.CalculateIsValidLocationForCollidable( proposedLoc, proposedLoc.Position, rotationY, true, CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, Rule ) )
                        return null;
                }
                else
                {
                    bool foundValidSpot = false;
                    for ( int outerAttempt = 0; outerAttempt < 1200; outerAttempt++ )
                    {
                        location = spot.Position.ReplaceY( 0 );
                        proposedLoc.Position = location;
                        proposedLoc.RotationY = rotationY = RotationYIfZeroPlus >= 0 ? RotationYIfZeroPlus : location.GetRotationYTo( RotateToFaceThisPoint,
                            Rand.Next( 0, 360 ) );
                        float wiggleMin = ((float)outerAttempt / 300f) + 0.1f;
                        float wiggleMax = ((float)outerAttempt / 200f) + 0.4f;
                        if ( outerAttempt > 600 )
                        {
                            wiggleMin = ((float)outerAttempt / 100f) + 0.1f;
                            wiggleMax = ((float)outerAttempt / 50f) + 0.4f;
                        }

                        for ( int attempts = 0; attempts < 20; attempts++ )
                        {
                            if ( CityMap.CalculateIsValidLocationForCollidable( proposedLoc, proposedLoc.Position, rotationY, true, CollisionBuildingCheckType.ForgiveBuildingsWeCanStepOn, true, Rule ) )
                            {
                                foundValidSpot = true;
                                break; //this was a valid spot, hooray!
                            }
                            //if we are here, this was NOT a valid spot, boo.
                            if ( Rand.NextBool() )
                                location.x += Rand.NextFloat( wiggleMin, wiggleMax );
                            else
                                location.x += Rand.NextFloat( wiggleMin, wiggleMax );

                            if ( Rand.NextBool() )
                                location.z += Rand.NextFloat( wiggleMin, wiggleMax );
                            else
                                location.z += Rand.NextFloat( wiggleMin, wiggleMax );

                            proposedLoc.Position = location;
                        }
                        if ( foundValidSpot )
                            break;
                    }
                    if ( !foundValidSpot )
                        return null;
                }

                unit = NPCUnit.CreateNew( Type, FromCohort, null, -1, SquadSizeMultiplier, Rand, true, false, CreationReason );
                unit.SetRotationY( rotationY );
                unit.SetActualGroundLocation( location );
                unit.CreationPosition = location;
            }
            else
            {
                unit = NPCUnit.CreateNew( Type, FromCohort, spot, -1, SquadSizeMultiplier, Rand, true, false, CreationReason );
                if ( unit != null )
                {
                    spot.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( spot );
                    unit.CreationPosition = spot.GetEffectiveWorldLocationForContainedUnit();
                }
            }            

            if ( unit != null )
            {
                unit.Stance = Stance;
                unit.ManagerStartLocation = new WrapperedSimBuilding( ManagerStartLocation );
                unit.ManagerOriginalMachineActorFocusID = ManagerOriginalMachineActorFocusID;
                unit.IsManagedUnit = IsManagedUnit;
                unit.IsCityConflictUnit = IsCityConflictUnit;

                AllNPCUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;

                if ( unit.IsManagedUnit != null )
                    ManagedNPCUnitsByID[unit.UnitID] = unit;
                if ( unit.IsCityConflictUnit != null )
                    CityConflictNPCUnitsByID[unit.UnitID] = unit;

                if ( !hasSetRotation )
                {
                    if ( RotationYIfZeroPlus >= 0 )
                        unit.SetRotationY( RotationYIfZeroPlus );
                    else if ( RotateToFaceThisPoint.x == float.NegativeInfinity )
                        unit.SetRotationY( Rand.Next( 0, 360 ) );
                    else
                        unit.SetRotationY( spot.Position.GetRotationYTo( RotateToFaceThisPoint,
                            Rand.Next( 0, 360 )  ) );
                }

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );

                //{
                //    Vector3 loc = unit.GetPositionForCollisions();
                //    float radius = unit.GetRadiusForCollisions();
                //    float halfHeight = unit.GetHalfHeightForCollisions();
                //    float maxY = loc.y + halfHeight;
                //    float minY = loc.y - halfHeight;
                //    float extraBufferRadius = unit.GetExtraRadiusBufferWhenTestingForNew();

                //    int firstTestCount = CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.Count;
                //    foreach ( KeyValuePair<int, RefPair<ICollidable, long>> kv in CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation )
                //    {
                //        ICollidable otherColl = kv.Value.LeftItem;
                //        if ( otherColl.GetEquals( unit ) )
                //            continue; //don't worry about colliding with self

                //        float otherHalfHeight = otherColl.GetHalfHeightForCollisions();
                //        Vector3 otherLoc = otherColl.GetPositionForCollisions();
                //        float otherMaxY = otherLoc.y + otherHalfHeight;
                //        float otherMinY = otherLoc.y - otherHalfHeight;

                //        if ( otherMaxY < minY )
                //        {
                //            //otherColl.DebugText = "below " + ArcenTime.AnyTimeSinceStartF;
                //            continue; //if the top of the other one is below our bottom, stop checking
                //        }
                //        if ( otherMinY > maxY )
                //        {
                //            //otherColl.DebugText = "above " + ArcenTime.AnyTimeSinceStartF;
                //            continue; //if the bottom of the other one is above our top, stop checking
                //        }

                //        float otherRadius = otherColl.GetRadiusForCollisions();

                //        //we are in y range, so check if we are in radius range

                //        float totalRadius = otherRadius + radius + extraBufferRadius;
                //        if ( (loc - otherLoc).GetGroundDistanceWithinBox_ThenCheckSquaredDistance( totalRadius ) )
                //        {
                //            ArcenDebugging.LogSingleLine( "A-Type: Unit " + unit.UnitType.ID + " collided with other unit " + otherColl.GetCollidableTypeID() + " firstTestCount: " + firstTestCount, Verbosity.ShowAsError );
                //        }
                //    }

                //    foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
                //    {
                //        ISimNPCUnit otherColl = kv.Value;
                //        if ( otherColl.GetEquals( unit ) )
                //            continue; //don't worry about colliding with self

                //        float otherHalfHeight = otherColl.GetHalfHeightForCollisions();
                //        Vector3 otherLoc = otherColl.GetPositionForCollisions();
                //        float otherMaxY = otherLoc.y + otherHalfHeight;
                //        float otherMinY = otherLoc.y - otherHalfHeight;

                //        if ( otherMaxY < minY )
                //        {
                //            //otherColl.DebugText = "below " + ArcenTime.AnyTimeSinceStartF;
                //            continue; //if the top of the other one is below our bottom, stop checking
                //        }
                //        if ( otherMinY > maxY )
                //        {
                //            //otherColl.DebugText = "above " + ArcenTime.AnyTimeSinceStartF;
                //            continue; //if the bottom of the other one is above our top, stop checking
                //        }

                //        float otherRadius = otherColl.GetRadiusForCollisions();

                //        //we are in y range, so check if we are in radius range

                //        float totalRadius = otherRadius + radius + extraBufferRadius;
                //        if ( (loc - otherLoc).GetGroundDistanceWithinBox_ThenCheckSquaredDistance( totalRadius ) )
                //        {
                //            ArcenDebugging.LogSingleLine( "B-Type Unit " + unit.UnitType.ID + " collided with other unit " + otherColl.GetCollidableTypeID() + " firstTestCount: " + firstTestCount, Verbosity.ShowAsError );
                //        }
                //        else
                //        {
                //            if ( unit.UnitType.IsMechStyleMovement && otherColl is ISimNPCUnit otherUnit && otherUnit.UnitType.IsMechStyleMovement )
                //            {
                //                float dist = Mathf.Sqrt( (loc - otherLoc).GetSquareGroundMagnitude() );
                //                if ( dist < totalRadius )
                //                {
                //                    ArcenDebugging.LogSingleLine( "B-Type Unit fail " + unit.UnitType.ID + " against " + otherColl.GetCollidableTypeID() + " firstTestCount: " + firstTestCount + "totalRadius: " +
                //                        totalRadius + " dist: " + dist, Verbosity.DoNotShow );
                //                }
                //            }
                //        }
                //    }
                //}
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewNPCUnitAtBuilding
        public ISimNPCUnit CreateNewNPCUnitAtBuilding( NPCUnitType Type, NPCCohort FromCohort, NPCUnitStance Stance,
            float SquadSizeMultiplier, Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, MapItem building, MersenneTwister Rand,
            ISimBuilding ManagerStartLocation, int ManagerOriginalMachineActorFocusID, NPCManagedUnit IsManagedUnit, CityConflictUnit IsCityConflictUnit, CollisionRule Rule, string CreationReason )
        {
            if ( Type == null || building == null )
                return null;
            ISimBuilding simBuilding = building.SimBuilding;
            if ( simBuilding == null )
                return null;
            if ( simBuilding.GetIsNPCBlockedFromComingHere( Stance ) )
                return null;

            NPCUnit unit = null;
            if ( Type.IsMechStyleMovement )
            {
                //no mechs on buildings!
                return null;
            }
            else
            {
                unit = NPCUnit.CreateNew( Type, FromCohort, simBuilding, -1, SquadSizeMultiplier, Rand, true, false, CreationReason );
                if ( unit != null )
                {
                    simBuilding.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( simBuilding );
                    unit.CreationPosition = simBuilding.GetEffectiveWorldLocationForContainedUnit();
                }
            }

            if ( unit != null )
            {
                unit.Stance = Stance;
                unit.ManagerStartLocation = new WrapperedSimBuilding( ManagerStartLocation );
                unit.ManagerOriginalMachineActorFocusID = ManagerOriginalMachineActorFocusID;
                unit.IsManagedUnit = IsManagedUnit;
                unit.IsCityConflictUnit = IsCityConflictUnit;

                AllNPCUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;

                if ( unit.IsManagedUnit != null )
                    ManagedNPCUnitsByID[unit.UnitID] = unit;
                if ( unit.IsCityConflictUnit != null )
                    CityConflictNPCUnitsByID[unit.UnitID] = unit;

                if ( RotationYIfZeroPlus >= 0 )
                    unit.SetRotationY( RotationYIfZeroPlus );
                else if ( RotateToFaceThisPoint.x == float.NegativeInfinity )
                    unit.SetRotationY( Rand.Next( 0, 360 ) );
                else
                    unit.SetRotationY( simBuilding.GetEffectiveWorldLocationForContainedUnit().GetRotationYTo( RotateToFaceThisPoint,
                        Rand.Next( 0, 360 ) ) );

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewNPCUnitAtExactSpotOrBuildingForPlayer
        public ISimNPCUnit CreateNewNPCUnitAtExactSpotOrBuildingForPlayer( NPCUnitType Type, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier,
            float RotationYIfZeroPlus, MapItem buildingOrNull, Vector3 LooseLocationIfNoBuilding, MersenneTwister Rand, string CreationReason )
        {
            if ( Type == null )
                return null;

            NPCUnit unit = null;
            if ( buildingOrNull != null )
            {
                ISimBuilding simBuilding = buildingOrNull.SimBuilding;
                if ( simBuilding == null )
                    return null;
                if ( simBuilding.GetIsNPCBlockedFromComingHere( Stance ) )
                    return null;

                unit = NPCUnit.CreateNew( Type, FromCohort, simBuilding, -1, SquadSizeMultiplier, Rand, true, false, CreationReason );
                if ( unit != null )
                {
                    simBuilding.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( simBuilding );
                    unit.CreationPosition = simBuilding.GetEffectiveWorldLocationForContainedUnit();
                }
            }
            else
            {
                unit = NPCUnit.CreateNew( Type, FromCohort, null, -1, SquadSizeMultiplier, Rand, true, false, CreationReason );
                if ( unit != null )
                {
                    unit.SetActualGroundLocation( LooseLocationIfNoBuilding );
                    unit.CreationPosition = LooseLocationIfNoBuilding;
                }
            }


            if ( unit != null )
            {
                unit.Stance = Stance;

                //let's not do that, it's slow and like enemies
                unit.Materializing = MaterializeType.None;

                AllNPCUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;

                if ( unit.IsManagedUnit != null )
                    ManagedNPCUnitsByID[unit.UnitID] = unit;
                if ( unit.IsCityConflictUnit != null )
                    CityConflictNPCUnitsByID[unit.UnitID] = unit;

                if ( RotationYIfZeroPlus >= 0 )
                    unit.SetRotationY( RotationYIfZeroPlus );
                else
                    unit.SetRotationY( Rand.Next( 0, 360 ) );

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewMachineUnitAtExactSpotOrBuildingForPlayer
        public ISimMachineUnit CreateNewMachineUnitAtExactSpotOrBuildingForPlayer( MachineUnitType Type,
            float RotationYIfZeroPlus, MapItem buildingOrNull, Vector3 LooseLocationIfNoBuilding, MersenneTwister Rand, bool StartAsReadyToAct )
        {
            if ( Type == null )
                return null;

            MachineUnit unit = null;
            if ( buildingOrNull != null )
            {
                ISimBuilding simBuilding = buildingOrNull.SimBuilding;
                if ( simBuilding == null )
                    return null;

                unit = MachineUnit.CreateNew( Type, simBuilding, -1, Rand, true, false );
                if ( unit != null )
                {
                    simBuilding.CurrentOccupyingUnit = unit;
                    unit.SetActualContainerLocation( simBuilding );
                }
            }
            else
            {
                unit = MachineUnit.CreateNew( Type, null, -1, Rand, true, false );
                if ( unit != null )
                    unit.SetActualGroundLocation( LooseLocationIfNoBuilding );
            }


            if ( unit != null )
            {
                MachineUnitsByID[unit.UnitID] = unit;
                SimCommon.AllActorsByID[unit.UnitID] = unit;
                if ( unit.UnitType.IsConsideredMech )
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForMechs;
                else
                    unit.Stance = MachineUnitStanceTable.BasicActiveStanceForAndroids;

                unit.SetStartingActionPoints( StartAsReadyToAct );

                if ( RotationYIfZeroPlus >= 0 )
                    unit.SetRotationY( RotationYIfZeroPlus );
                else
                    unit.SetRotationY( Rand.Next( 0, 360 ) );

                unit.UnitName = unit.UnitType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineUnit(
                        unit.UnitType.NameStyle, unit, Rand );

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( unit, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return unit;
        }
        #endregion

        #region CreateNewMachineVehicleAtExactSpotForPlayer
        public ISimMachineVehicle CreateNewMachineVehicleAtExactSpotForPlayer( MachineVehicleType Type,
            float RotationYIfZeroPlus, Vector3 LooseLocation, MersenneTwister Rand, bool StartAsReadyToAct )
        {
            if ( Type == null )
                return null;

            MachineVehicle vehicle = MachineVehicle.CreateNew( Type, LooseLocation, -1, Rand, true, false );

            if ( vehicle != null )
            {
                vehicle.SetWorldLocation( LooseLocation ); //the initial height was overriding it, so set it again

                MachineVehiclesByID[vehicle.VehicleID] = vehicle;
                vehicle.SetStartingActionPoints( StartAsReadyToAct );
                SimCommon.AllActorsByID[vehicle.ActorID] = vehicle;

                if ( RotationYIfZeroPlus >= 0 )
                    vehicle.SetRotationY( RotationYIfZeroPlus );
                else
                    vehicle.SetRotationY( Rand.Next( 0, 360 ) );

                vehicle.VehicleName = vehicle.VehicleType.NameStyle.Implementation.GenerateNewRandomUniqueName_ForMachineVehicle(
                        vehicle.VehicleType.NameStyle, vehicle, Rand );

                CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.AddEntry(
                    RefPair<ICollidable, Int64>.Create( vehicle, SimCommon.VisibilityGranterCycle ) );
            }

            SimCommon.NeedsVisibilityGranterRecalculation = true;
            return vehicle;
        }
        #endregion

        #region TryCreateNewNPCUnitInPOI
        public ISimNPCUnit TryCreateNewNPCUnitInPOI( MapPOI POI, NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, 
            NPCUnitStance Stance, float SquadSizeMultiplier, MersenneTwister Rand, int PercentChanceOfSpawningOnBuildingIfNotMech, CollisionRule Rule, string CreationReason )
        {
            if ( POI == null ) 
                return null;

            ListView<MapCell> allCells = POI.GetAllCells();
            if ( allCells.Count == 0 )
                return null;

            ISimNPCUnit created = null;
            foreach ( MapCell cell in allCells.GetRandomStartEnumerable( Rand ) )
            {
                if ( UnitTypeToGrant.IsMechStyleMovement )
                {
                    created = TryCreateNewNPCUnitInPOI_OutdoorSpots( POI, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Rand, cell, Rule, CreationReason );
                    if ( created != null )
                        return created;
                    continue;
                }

                {
                    if ( Rand.Next( 0, 100 ) < PercentChanceOfSpawningOnBuildingIfNotMech )
                    {
                        created = TryCreateNewNPCUnitInPOI_Buildings( POI, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Rand, cell, Rule, CreationReason );
                        if ( created == null )
                            created = TryCreateNewNPCUnitInPOI_OutdoorSpots( POI, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Rand, cell, Rule, CreationReason );
                    }
                    else
                    {
                        created = TryCreateNewNPCUnitInPOI_OutdoorSpots( POI, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Rand, cell, Rule, CreationReason );
                        if ( created == null )
                            created = TryCreateNewNPCUnitInPOI_Buildings( POI, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Rand, cell, Rule, CreationReason );
                    }
                }
                if ( created != null )
                    return created;
            }
            return null;
        }

        #region TryCreateNewNPCUnitInPOI_OutdoorSpots
        private ISimNPCUnit TryCreateNewNPCUnitInPOI_OutdoorSpots( MapPOI POI,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, MersenneTwister Rand,
            MapCell cell, CollisionRule Rule, string CreationReason )
        {
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
            {
                if ( spot.CalculateLocationPOI() != POI )
                    continue;
                if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                {
                    float randomRot = Rand.Next( 0, 360 );
                    if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                        randomRot, Rule, false ) )
                        continue; //if we would intersect some existing collidable

                    //we did it!
                    ISimNPCUnit created = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Vector3.negativeInfinity, 
                        randomRot, spot, false, Rand, null, -1, null, null, Rule, CreationReason );
                    if ( created != null )
                    {
                        spot.MarkAsBlockedUntilNextTurn( created );
                        created.HomePOI = POI;
                        return created;
                    }
                }
            }
            return null;
        }
        #endregion TryCreateNewNPCUnitInPOI_OutdoorSpots

        #region TryCreateNewNPCUnitInPOI_Buildings
        private ISimNPCUnit TryCreateNewNPCUnitInPOI_Buildings( MapPOI POI,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, MersenneTwister Rand,
            MapCell cell, CollisionRule Rule, string CreationReason )
        {
            if ( UnitTypeToGrant.IsMechStyleMovement )
                return null; //cannot be put on a building!

            foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
            {
                if ( building.GetParentPOIOrNull() != POI )
                    continue;

                ISimBuilding simBuilding = building.SimBuilding;
                if ( simBuilding == null )
                    continue;

                if ( !simBuilding.GetIsNPCBlockedFromComingHere( Stance ) )
                {
                    float randomRot = Rand.Next( 0, 360 );
                    if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                        randomRot, Rule, false ) )
                        continue; //if we would intersect some existing collidable

                    //we did it!
                    ISimNPCUnit created = CreateNewNPCUnitAtBuilding( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, Vector3.negativeInfinity, 
                        randomRot, building, Rand, null, -1, null, null, Rule, CreationReason );
                    if ( created != null )
                    {
                        simBuilding.MarkAsBlockedUntilNextTurn( created );
                        created.HomePOI = POI;
                        return created;
                    }
                }
            }
            return null;
        }
        #endregion TryCreateNewNPCUnitInPOI_Buildings

        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToLocation_OneCell
        private ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToLocation_OneCell( Vector3 unitSpot, MapCell cell,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier,
            Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( cell != null )
            {
                //keep trying, by increasing the clearance gradually in each loop
                for ( int clearance = MaxDesiredClearance; clearance <= 5; clearance++ )
                {
                    {
                        float closestSquareDistance = 1000000;
                        MapOutdoorSpot closestSpot = null;

                        foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                        {
                            if ( clearance < 5 )
                            {
                                if ( spot.CalculateLocationSecurityClearanceInt() > clearance )
                                    continue; //if the clearance is too high, then skip
                            }

                            if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                            {
                                if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                    continue; //if we would intersect some existing collidable

                                float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                                if ( closestSpot == null || squareDistance < closestSquareDistance )
                                {
                                    closestSpot = spot;
                                    closestSquareDistance = squareDistance;
                                }
                            }
                        }

                        if ( closestSpot != null )
                        {
                            ISimNPCUnit result = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                                RotationYIfZeroPlus, closestSpot, false, true, //we already validated this was fine, so skip more checks
                                Rand, null, -1, null, null, Rule, CreationReason );
                            if ( result != null )
                                return result;
                        }
                    }

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( clearance < 5 )
                        {
                            if ( spot.CalculateLocationSecurityClearanceInt() > clearance )
                                continue; //if the clearance is too high, then skip
                        }

                        if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimNPCUnit result = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                                RotationYIfZeroPlus, spot, false, true, //we already validated this was fine, so skip more checks
                                Rand, null, -1, null, null, Rule, CreationReason );
                            if ( result != null )
                                return result;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToLocation_CellList
        private ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToLocation_CellList( Vector3 unitSpot, IReadOnlyList<MapCell> cells,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier,
            Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( cells != null )
            {
                //keep trying, by increasing the clearance gradually in each loop
                for ( int clearance = MaxDesiredClearance; clearance <= 5; clearance++ )
                {
                    {
                        float closestSquareDistance = 1000000;
                        MapOutdoorSpot closestSpot = null;

                        foreach ( MapCell cell in cells )
                        {
                            int checkedCount = 0;
                            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                            {
                                if ( clearance < 5 )
                                {
                                    if ( spot.CalculateLocationSecurityClearanceInt() > clearance )
                                        continue; //if the clearance is too high, then skip
                                }

                                checkedCount++;

                                if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                                {
                                    if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                        0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                        continue; //if we would intersect some existing collidable

                                    float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                                    if ( closestSpot == null || squareDistance < closestSquareDistance )
                                    {
                                        closestSpot = spot;
                                        closestSquareDistance = squareDistance;
                                    }
                                }
                            }

                            //ArcenDebugging.LogSingleLine( "clear" + clearance + " cell " + cell.CellLocation + " checked " + checkedCount + 
                            //    " closestSpot: " + (closestSpot == null ? "null" : "Found" ) + " dist " + closestSquareDistance, Verbosity.DoNotShow );

                        }

                        if ( closestSpot != null )
                        {
                            //ArcenDebugging.LogSingleLine( "clear" + clearance + " final seed " + closestSpot.ParentCell.CellLocation + 
                            //    " closestSpot: " + (closestSpot == null ? "null" : "Found") + " dist " + closestSquareDistance, Verbosity.DoNotShow );

                            ISimNPCUnit result = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                                RotationYIfZeroPlus, closestSpot, false, true, //we already validated this was fine, so skip more checks
                                Rand, null, -1, null, null, Rule, CreationReason );
                            if ( result != null )
                                return result;
                        }
                    }

                    foreach ( MapCell cell in cells )
                    {
                        foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                        {
                            if ( clearance < 5 )
                            {
                                if ( spot.CalculateLocationSecurityClearanceInt() > clearance )
                                    continue; //if the clearance is too high, then skip
                            }

                            if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                            {
                                if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                    continue; //if we would intersect some existing collidable

                                ISimNPCUnit result = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                                    RotationYIfZeroPlus, spot, false, true, //we already validated this was fine, so skip more checks
                                    Rand, null, -1, null, null, Rule, CreationReason );
                                if ( result != null )
                                    return result;
                            }
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewNPCUnitAsCloseAsPossibleToLocation
        public ISimNPCUnit TryCreateNewNPCUnitAsCloseAsPossibleToLocation( Vector3 unitSpot, MapCell cell,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier, 
            Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, int MaxDesiredClearance, CellRange CRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( cell != null )
            {
                IReadOnlyList<MapCell> list = null;
                switch ( CRange )
                {
                    case CellRange.CellAndAdjacent:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal;
                        break;
                    case CellRange.CellAndAdjacent2x:
                        {
                            list = cell.AdjacentCellsAndSelfIncludingDiagonal2x;

                            //ArcenDebugging.LogSingleLine( "cell " + cell.CellLocation, Verbosity.DoNotShow );
                            //foreach ( MapCell cell2 in list )
                            //{
                            //    ArcenDebugging.LogSingleLine( "from list: " + cell2.CellLocation, Verbosity.DoNotShow );
                            //}
                        }
                        break;
                    case CellRange.ThisCell:
                        return TryCreateNewNPCUnitAsCloseAsPossibleToLocation_OneCell( unitSpot, cell, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier,
                            RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, Rand, Rule, CreationReason );
                }
                if ( list != null) //blah, looks like we need to look further out
                {
                    return TryCreateNewNPCUnitAsCloseAsPossibleToLocation_CellList( unitSpot, list, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier,
                            RotateToFaceThisPoint, RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, Rand, Rule, CreationReason );
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewNPCUnitWithinThisRadius
        public ISimNPCUnit TryCreateNewNPCUnitWithinThisRadius( Vector3 unitSpot, MapCell cell,
            NPCUnitType UnitTypeToGrant, NPCCohort FromCohort, NPCUnitStance Stance, float SquadSizeMultiplier,
            Vector3 RotateToFaceThisPoint, float RotationYIfZeroPlus, bool AllowMechWiggle, float Radius, int MaxDesiredClearance, 
            CellRange FailoverCRange, MersenneTwister Rand, CollisionRule Rule, string CreationReason )
        {
            if ( Radius <= 0 )
                return TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                    RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, FailoverCRange, Rand, Rule, CreationReason );

            if ( cell != null )
            {
                if ( MaxDesiredClearance < 0 || MaxDesiredClearance > 5 )
                    MaxDesiredClearance = 5;

                float radiusSquared = Radius * Radius;

                //keep trying, by increasing the clearance gradually in each loop
                for ( int clearance = MaxDesiredClearance; clearance <= 5; clearance++ )
                {
                    foreach ( MapCell targetCell in cell.AdjacentCellsAndSelf.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( targetCell.ParentTile?.IsOutOfBoundsTile ?? true )
                            continue;

                        foreach ( MapOutdoorSpot spot in targetCell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                        {
                            if ( !spot.GetIsNPCBlockedFromComingHere( Stance ) )
                            {
                                if ( clearance < 5 )
                                {
                                    if ( spot.CalculateLocationSecurityClearanceInt() > clearance )
                                        continue; //if the clearance is too high, then skip
                                }

                                if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                    continue; //if we would intersect some existing collidable

                                float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                                if ( squareDistance <= radiusSquared )
                                {
                                    ISimNPCUnit result = CreateNewNPCUnitAtOutdoorSpot( UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                                        RotationYIfZeroPlus, spot, AllowMechWiggle, Rand, null, -1, null, null, Rule, CreationReason );
                                    if ( result != null )
                                        return result;
                                }
                            }
                        }
                    }
                }

                return this.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( unitSpot, cell, UnitTypeToGrant, FromCohort, Stance, SquadSizeMultiplier, RotateToFaceThisPoint,
                    RotationYIfZeroPlus, AllowMechWiggle, MaxDesiredClearance, FailoverCRange, Rand, Rule, CreationReason );
            }

            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAsCloseAsPossibleToLocation_OneCell
        private ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToLocation_OneCell( Vector3 unitSpot, MapCell cell, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cell != null && !cell.ParentTile.IsOutOfBoundsTile && ( CanSpawnInIrradiated || !cell.IsCellConsideredIrradiated ) )
            {
                {
                    float closestSquareDistance = 1000000;
                    MapOutdoorSpot closestSpot = null;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                            if ( closestSpot == null || squareDistance < closestSquareDistance )
                            {
                                closestSpot = spot;
                                closestSquareDistance = squareDistance;
                            }
                        }
                    }

                    if ( closestSpot != null )
                    {
                        ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, closestSpot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                        if ( result != null )
                            return result;
                    }
                }

                {
                    float closestSquareDistance = 1000000;
                    MapItem closestBuilding = null;

                    foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuilding = building.SimBuilding;
                        if ( simBuilding == null )
                            continue;
                        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            float squareDistance = (building.rawReadPos.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                            if ( closestBuilding == null || squareDistance < closestSquareDistance )
                            {
                                closestBuilding = building;
                                closestSquareDistance = squareDistance;
                            }
                        }
                    }

                    if ( closestBuilding != null )
                    {
                        ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, closestBuilding, Rand, NewUnitOverridingName, StartAsReadyToAct );
                        if ( result != null )
                            return result;
                    }
                }

                {
                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                {
                    foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuilding = building.SimBuilding;
                        if ( simBuilding == null )
                            continue;
                        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                            if ( result != null )
                                return result;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAsCloseAsPossibleToLocation_CellList
        private ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToLocation_CellList( Vector3 unitSpot, IReadOnlyList<MapCell> cells, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cells != null )
            {
                {
                    float closestSquareDistance = 1000000;
                    MapOutdoorSpot closestSpot = null;

                    foreach ( MapCell cell in cells )
                    {
                        if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                            continue;

                        foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                        {
                            if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                            {
                                if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                    continue; //if we would intersect some existing collidable

                                float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                                if ( closestSpot == null || squareDistance < closestSquareDistance )
                                {
                                    closestSpot = spot;
                                    closestSquareDistance = squareDistance;
                                }
                            }
                        }
                    }

                    if ( closestSpot != null )
                    {
                        ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, closestSpot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                        if ( result != null )
                            return result;
                    }
                }

                {
                    float closestSquareDistance = 1000000;
                    MapItem closestBuilding = null;

                    foreach ( MapCell cell in cells )
                    {
                        if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                            continue;

                        foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                        {
                            ISimBuilding simBuilding = building.SimBuilding;
                            if ( simBuilding == null )
                                continue;
                            if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                            {
                                if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                    continue; //if we would intersect some existing collidable

                                float squareDistance = (building.rawReadPos.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                                if ( closestBuilding == null || squareDistance < closestSquareDistance )
                                {
                                    closestBuilding = building;
                                    closestSquareDistance = squareDistance;
                                }
                            }
                        }
                    }

                    if ( closestBuilding != null )
                    {
                        ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, closestBuilding, Rand, NewUnitOverridingName, StartAsReadyToAct );
                        if ( result != null )
                            return result;
                    }
                }

                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuilding = building.SimBuilding;
                        if ( simBuilding == null )
                            continue;
                        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                            if ( result != null )
                                return result;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAsCloseAsPossibleToLocation
        public ISimMachineUnit TryCreateNewMachineUnitAsCloseAsPossibleToLocation( Vector3 unitSpot, MapCell cell, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cell != null )
            {
                IReadOnlyList<MapCell> list = null;
                switch ( CRange )
                {
                    case CellRange.CellAndAdjacent:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal;
                        break;
                    case CellRange.CellAndAdjacent2x:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal2x;
                        break;
                    case CellRange.ThisCell:
                        return TryCreateNewMachineUnitAsCloseAsPossibleToLocation_OneCell( unitSpot, cell, UnitTypeToGrant,
                            NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
                if ( list != null ) //blah, looks like we need to look further out
                {
                    return TryCreateNewMachineUnitAsCloseAsPossibleToLocation_CellList( unitSpot, list, UnitTypeToGrant,
                        NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAtRandomLocationOnCell_OneCell
        private ISimMachineUnit TryCreateNewMachineUnitAtRandomLocationOnCell_OneCell( MapCell cell, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cell != null && !cell.ParentTile.IsOutOfBoundsTile && (CanSpawnInIrradiated || !cell.IsCellConsideredIrradiated) )
            {
                {
                    foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuilding = building.SimBuilding;
                        if ( simBuilding == null || simBuilding.CalculateLocationSecurityClearanceInt() > 0 )
                            continue;
                        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                {
                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAtRandomLocationOnCell_CellList
        private ISimMachineUnit TryCreateNewMachineUnitAtRandomLocationOnCell_CellList( IReadOnlyList<MapCell> cells, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cells != null )
            {
                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapItem building in cell.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuilding = building.SimBuilding;
                        if ( simBuilding == null || simBuilding.CalculateLocationSecurityClearanceInt() > 0 )
                            continue;
                        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false//we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit(),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineUnit result = CreateNewMachineUnitAtOutdoorSpot( UnitTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineUnitAtRandomLocationOnCell
        public ISimMachineUnit TryCreateNewMachineUnitAtRandomLocationOnCell( MapCell cell, MachineUnitType UnitTypeToGrant,
            string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cell != null )
            {
                IReadOnlyList<MapCell> list = null;
                switch ( CRange )
                {
                    case CellRange.CellAndAdjacent:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal;
                        break;
                    case CellRange.CellAndAdjacent2x:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal2x;
                        break;
                    case CellRange.ThisCell:
                        return TryCreateNewMachineUnitAtRandomLocationOnCell_OneCell( cell, UnitTypeToGrant,
                                NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
                if ( list != null ) //blah, looks like we need to look further out
                {
                    return TryCreateNewMachineUnitAtRandomLocationOnCell_CellList( list, UnitTypeToGrant,
                            NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineVehicleAsCloseAsPossibleToLocation
        public ISimMachineVehicle TryCreateNewMachineVehicleAsCloseAsPossibleToLocation( Vector3 unitSpot, MapCell cell, MachineVehicleType VehicleTypeToGrant,
            string NewUnitOverridingName, CellRange CRange, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            if ( cell != null )
            {
                IReadOnlyList<MapCell> list = null;
                switch ( CRange )
                {
                    case CellRange.CellAndAdjacent:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal;
                        break;
                    case CellRange.CellAndAdjacent2x:
                        list = cell.AdjacentCellsAndSelfIncludingDiagonal2x;
                        break;
                    case CellRange.ThisCell:
                        return TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_OneCell( unitSpot, cell, VehicleTypeToGrant,
                            NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
                if ( list != null ) //blah, looks like we need to look further out
                {
                    return TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_CellList( unitSpot, list, VehicleTypeToGrant,
                        NewUnitOverridingName, Rand, StartAsReadyToAct, Rule, CanSpawnInIrradiated );
                }
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_OneCell
        private ISimMachineVehicle TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_OneCell( Vector3 unitSpot, MapCell cell, MachineVehicleType VehicleTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            float requiredHeight = VehicleTypeToGrant.InitialHeight;
            if ( cell != null && !cell.ParentTile.IsOutOfBoundsTile && (CanSpawnInIrradiated || !cell.IsCellConsideredIrradiated) )
            {
                {
                    float closestSquareDistance = 1000000;
                    MapOutdoorSpot closestSpot = null;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit().ReplaceY( requiredHeight ),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                            if ( closestSpot == null || squareDistance < closestSquareDistance )
                            {
                                closestSpot = spot;
                                closestSquareDistance = squareDistance;
                            }
                        }
                    }

                    if ( closestSpot != null )
                    {
                        ISimMachineVehicle result = CreateNewMachineVehicleAtOutdoorSpot( VehicleTypeToGrant, closestSpot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                        if ( result != null )
                            return result;
                    }
                }

                //{
                //    float closestSquareDistance = 1000000;
                //    MapItem closestBuilding = null;

                //    foreach ( MapItem building in cell.AllBuildings.GetRandomStartEnumerable( Rand ) )
                //    {
                //        ISimBuilding simBuilding = building.SimBuilding;
                //        if ( simBuilding == null )
                //            continue;
                //        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                //        {
                //            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                //                0 //we will ignore rotation, as this only applies to sub-collidables
                //                ) )
                //                continue; //if we would intersect some existing collidable

                //            float squareDistance = (building.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                //            if ( closestBuilding == null || squareDistance < closestSquareDistance )
                //            {
                //                closestBuilding = building;
                //                closestSquareDistance = squareDistance;
                //            }
                //        }
                //    }

                //    if ( closestBuilding != null )
                //    {
                //        ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, closestBuilding, Rand, NewUnitOverridingName, StartAsReadyToAct );
                //        if ( result != null )
                //            return result;
                //    }
                //}

                {
                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit().ReplaceY( requiredHeight ),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineVehicle result = CreateNewMachineVehicleAtOutdoorSpot( VehicleTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                //{
                //    foreach ( MapItem building in cell.AllBuildings.GetRandomStartEnumerable( Rand ) )
                //    {
                //        ISimBuilding simBuilding = building.SimBuilding;
                //        if ( simBuilding == null )
                //            continue;
                //        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                //        {
                //            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                //                0 //we will ignore rotation, as this only applies to sub-collidables
                //                ) )
                //                continue; //if we would intersect some existing collidable

                //            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                //            if ( result != null )
                //                return result;
                //        }
                //    }
                //}
            }
            return null;
        }
        #endregion

        #region TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_CellList
        private ISimMachineVehicle TryCreateNewMachineVehicleAsCloseAsPossibleToLocation_CellList( Vector3 unitSpot, IReadOnlyList<MapCell> cells, MachineVehicleType VehicleTypeToGrant,
            string NewUnitOverridingName, MersenneTwister Rand, bool StartAsReadyToAct, CollisionRule Rule, bool CanSpawnInIrradiated )
        {
            float requiredHeight = VehicleTypeToGrant.InitialHeight;
            if ( cells != null )
            {
                float closestSquareDistance = 1000000;
                MapOutdoorSpot closestSpot = null;

                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit().ReplaceY( requiredHeight ),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            float squareDistance = (spot.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                            if ( closestSpot == null || squareDistance < closestSquareDistance )
                            {
                                closestSpot = spot;
                                closestSquareDistance = squareDistance;
                            }
                        }
                    }
                }

                if ( closestSpot != null )
                {
                    ISimMachineVehicle result = CreateNewMachineVehicleAtOutdoorSpot( VehicleTypeToGrant, closestSpot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                    if ( result != null )
                        return result;
                }

                //{
                //    float closestSquareDistance = 1000000;
                //    MapItem closestBuilding = null;

                //    foreach ( MapItem building in cell.AllBuildings.GetRandomStartEnumerable( Rand ) )
                //    {
                //        ISimBuilding simBuilding = building.SimBuilding;
                //        if ( simBuilding == null )
                //            continue;
                //        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                //        {
                //            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                //                0 //we will ignore rotation, as this only applies to sub-collidables
                //                ) )
                //                continue; //if we would intersect some existing collidable

                //            float squareDistance = (building.Position.ReplaceY( 0 ) - unitSpot).GetSquareGroundMagnitude();
                //            if ( closestBuilding == null || squareDistance < closestSquareDistance )
                //            {
                //                closestBuilding = building;
                //                closestSquareDistance = squareDistance;
                //            }
                //        }
                //    }

                //    if ( closestBuilding != null )
                //    {
                //        ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, closestBuilding, Rand, NewUnitOverridingName, StartAsReadyToAct );
                //        if ( result != null )
                //            return result;
                //    }
                //}

                foreach ( MapCell cell in cells )
                {
                    if ( cell.ParentTile.IsOutOfBoundsTile || (!CanSpawnInIrradiated && cell.IsCellConsideredIrradiated) )
                        continue;

                    foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( !spot.GetAreMoreUnitsBlockedFromComingHere() )
                        {
                            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( VehicleTypeToGrant, spot.GetEffectiveWorldLocationForContainedUnit().ReplaceY( requiredHeight ),
                                0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                ) )
                                continue; //if we would intersect some existing collidable

                            ISimMachineVehicle result = CreateNewMachineVehicleAtOutdoorSpot( VehicleTypeToGrant, spot, Rand, NewUnitOverridingName, StartAsReadyToAct, Rule );
                            if ( result != null )
                                return result;
                        }
                    }
                }

                //{
                //    foreach ( MapItem building in cell.AllBuildings.GetRandomStartEnumerable( Rand ) )
                //    {
                //        ISimBuilding simBuilding = building.SimBuilding;
                //        if ( simBuilding == null )
                //            continue;
                //        if ( !simBuilding.GetAreMoreUnitsBlockedFromComingHere() )
                //        {
                //            if ( cell.CalculateIfCollidableTypeWouldIntersectAnotherCollidableHere( UnitTypeToGrant, simBuilding.GetEffectiveWorldLocationForContainedUnit(),
                //                0 //we will ignore rotation, as this only applies to sub-collidables
                //                ) )
                //                continue; //if we would intersect some existing collidable

                //            ISimMachineUnit result = CreateNewMachineUnitAtBuilding( UnitTypeToGrant, building, Rand, NewUnitOverridingName, StartAsReadyToAct );
                //            if ( result != null )
                //                return result;
                //        }
                //    }
                //}
            }
            return null;
        }
        #endregion

        public bool GetHasMachineUnitNameEverBeenUsed( string Name )
        {
            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
            {
                if ( kv.Value.UnitName.EqualsCaseInvariant( Name ) )
                    return true;
            }
            return false;
        }

        public bool GetHasPartialVehicleNameEverBeenUsed( string PartialName )
        {
            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
            {
                string name = kv.Value.VehicleName;
                if ( name.Contains( PartialName, StringComparison.InvariantCultureIgnoreCase ) )
                    return true;
            }
            return false;
        }

        #region RecalculateUnitsDeployedAndStored
        public void RecalculateUnitsDeployedAndStored()
        {
            foreach ( MapCell cell in CityMap.Cells )
            {
                cell.LooseUnitsInCell.ClearConstructionListForStartingConstruction();
                cell.NPCUnitsInCell.ClearConstructionListForStartingConstruction();
            }

            UnitsDeployed.ClearConstructionListForStartingConstruction();
            UnitsRiding.ClearConstructionListForStartingConstruction();
            MachineActorsAutomated.ClearConstructionListForStartingConstruction();
            LivingMapActorsNotBeingABasicGuard.ClearConstructionListForStartingConstruction();
            AllLivingMapActors.ClearConstructionListForStartingConstruction();
            foreach ( KeyValuePair<int, MachineUnit> kv in MachineUnitsByID )
            {
                MachineUnit unit = kv.Value;

                bool wasAutomated = false;
                if ( unit.Stance.ShouldNotBeAutoSelectedForOrders )
                {
                    MachineActorsAutomated.AddToConstructionList( unit );
                    wasAutomated = true;
                }

                if ( !wasAutomated && !unit.GetIsDeployed() )
                {
                    UnitsRiding.AddToConstructionList( unit );
                    continue;
                }

                MapCell cell = CityMap.TryGetWorldCellAtCoordinates( unit.GroundLocation );
                if ( cell != null )
                    cell.LooseUnitsInCell.AddToConstructionList( unit );

                if ( !wasAutomated )
                    UnitsDeployed.AddToConstructionList( unit );
                LivingMapActorsNotBeingABasicGuard.AddToConstructionList( unit );
                AllLivingMapActors.AddToConstructionList( unit );
            }

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
            {
                NPCUnit unit = kv.Value;
                MapCell cell = CityMap.TryGetWorldCellAtCoordinates( unit.GroundLocation );
                if ( cell != null )
                {
                    cell.LooseUnitsInCell.AddToConstructionList( unit );
                    cell.NPCUnitsInCell.AddToConstructionList( unit );
                }
            }

            foreach ( KeyValuePair<int, MachineVehicle> kv in MachineVehiclesByID )
            {
                MachineVehicle vehicle = kv.Value;

                if ( vehicle.Stance.ShouldNotBeAutoSelectedForOrders )
                    MachineActorsAutomated.AddToConstructionList( vehicle );

                LivingMapActorsNotBeingABasicGuard.AddToConstructionList( vehicle );
                AllLivingMapActors.AddToConstructionList( vehicle );
            }

            foreach ( KeyValuePair<int, NPCUnit> kv in AllNPCUnitsByID )
            {
                NPCUnit unit = kv.Value;
                if ( unit.IsDowned || unit.IsFullDead )
                    continue;

                if ( !unit.Stance?.IsConsideredBasicGuard??true )
                    LivingMapActorsNotBeingABasicGuard.AddToConstructionList( unit );
                AllLivingMapActors.AddToConstructionList( unit );
            }

            UnitsDeployed.SwitchConstructionToDisplay();
            UnitsRiding.SwitchConstructionToDisplay();
            MachineActorsAutomated.SwitchConstructionToDisplay();
            LivingMapActorsNotBeingABasicGuard.SwitchConstructionToDisplay();
            AllLivingMapActors.SwitchConstructionToDisplay();

            foreach ( MapCell cell in CityMap.Cells )
            {
                cell.LooseUnitsInCell.SwitchConstructionToDisplay();
                cell.NPCUnitsInCell.SwitchConstructionToDisplay();
            }
        }
        #endregion

        #region ISimWorld_Forces
        public DictionaryView<int, ISimMachineVehicle> GetMachineVehiclesByID()
        {
            return DictionaryView<int, ISimMachineVehicle>.Create( MachineVehiclesByID );
        }

        public DictionaryView<int, ISimMachineUnit> GetMachineUnitsByID()
        {
            return DictionaryView<int, ISimMachineUnit>.Create( MachineUnitsByID );
        }

        public DictionaryView<int, ISimNPCUnit> GetAllNPCUnitsByID()
        {
            return DictionaryView<int, ISimNPCUnit>.Create( AllNPCUnitsByID );
        }

        public DictionaryView<int, ISimNPCUnit> GetManagedNPCUnitsByID()
        {
            return DictionaryView<int, ISimNPCUnit>.Create( ManagedNPCUnitsByID );
        }

        public DictionaryView<int, ISimNPCUnit> GetCityConflictNPCUnitsByID()
        {
            return DictionaryView<int, ISimNPCUnit>.Create( CityConflictNPCUnitsByID );
        }

        public ListView<ISimMachineUnit> GetMachineUnitsDeployed()
        {
            return ListView<ISimMachineUnit>.Create( UnitsDeployed.GetDisplayList() );
        }

        public ListView<ISimMachineUnit> GetMachineUnitsRiding()
        {
            return ListView<ISimMachineUnit>.Create( UnitsRiding.GetDisplayList() );
        }

        public ListView<ISimMachineActor> GetMachineActorsAutomated()
        {
            return ListView<ISimMachineActor>.Create( MachineActorsAutomated.GetDisplayList() );
        }

        public ListView<ISimMapActor> GetLivingMapActorsNotBeingABasicGuard()
        {
            return ListView<ISimMapActor>.Create( LivingMapActorsNotBeingABasicGuard.GetDisplayList() );
        }

        public ListView<ISimMapActor> GetAllLivingMapActors()
        {
            return ListView<ISimMapActor>.Create( AllLivingMapActors.GetDisplayList() );
        }
        #endregion
    }
}

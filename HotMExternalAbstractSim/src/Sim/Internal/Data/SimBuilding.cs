using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.Universal.Deserialization;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace Arcen.HotM.External
{
    internal class SimBuilding : TimeBasedPoolable<SimBuilding>, ISimBuilding, ISerializableAsInt32
    {
        //
        //Serialized data
        //-----------------------------------------------------
        public MapItem Item;
        public int BuildingID;
        public int BuildingTurnRandomNumber;
        public BuildingPrefab Prefab;
        public BuildingPrefab OriginalPrefab;
        public BuildingTypeVariant Variant;
        public BuildingTypeVariant OriginalVariant;
        public BuildingStatus Status;
        internal NPCCohort overridingAuthorityGroup;
        public BuildingKeyModifier KeyModifierFromPlayer { get; set; } = null;
        public BuildingKeyModifier KeyModifierFromNPCs { get; set; } = null;
        public Swarm SwarmSpread { get; set; } = null;
        private int swarmSpreadCount = 0;
        public int BlockingUnitsUntilTurn = 0;
        public readonly ThreadsafeTableDictionary32<EconomicClassType> Residents = ThreadsafeTableDictionary32<EconomicClassType>.Create_WillNeverBeGCed( EconomicClassTypeTable.Instance, "SimBuilding-Residents" );
        public readonly ThreadsafeTableDictionary32<ProfessionType> Workers = ThreadsafeTableDictionary32<ProfessionType>.Create_WillNeverBeGCed( ProfessionTypeTable.Instance, "SimBuilding-Residents" );

        public bool HasSpecialResourceAlreadyBeenExtracted { get; set; } = false;
        public bool IsBlockedFromGettingMoreCitizens { get; set; } = false;
        public bool IsViolentCyberocracyTarget { get; set; } = false;
        public bool IsPeacefulCyberocracyTarget { get; set; } = false;
        public int LowerClassCitizenGrabCount { get; set; } = 0;
        public int UpperClassCitizenGrabCount { get; set; } = 0;

        //
        //NonSerialized data
        //-----------------------------------------------------
        public MachineStructure MachineStructureInBuilding { get; set; } = null; //this is serialized at the structure itself
        public ISimUnit CurrentOccupyingUnit { get; set; } = null;
        private readonly DoubleBufferedValue<ContemplationType> currentContemplation = new DoubleBufferedValue<ContemplationType>( null );
        public DoubleBufferedValue<ContemplationType> CurrentContemplationRaw { get { return currentContemplation; } }
        private readonly DoubleBufferedValue<ExplorationSiteType> currentExplorationSite = new DoubleBufferedValue<ExplorationSiteType>( null );
        public DoubleBufferedValue<ExplorationSiteType> CurrentExplorationSiteRaw { get { return currentExplorationSite; } }
        private readonly DoubleBufferedValue<CityConflict> currentCityConflict = new DoubleBufferedValue<CityConflict>( null );
        public DoubleBufferedValue<CityConflict> CurrentCityConflict { get { return currentCityConflict; } }
        private readonly DoubleBufferedClass<StreetSenseDataAtBuilding> currentStreetSenseAction = new DoubleBufferedClass<StreetSenseDataAtBuilding>( new StreetSenseDataAtBuilding(), new StreetSenseDataAtBuilding() );
        public DoubleBufferedClass<StreetSenseDataAtBuilding> CurrentStreetSenseActionRaw { get { return currentStreetSenseAction; } }
        private int CachedResidentCount = 0;
        private int CachedWorkerCount = 0;
        private bool Debug = false;
        public NPCMission Mission { get; set; } = null;
        internal float roughDistanceFromMachines = 999999;
        public float RoughDistanceFromMachines { get { return roughDistanceFromMachines; } }
        public string DebugText = string.Empty;
        private static Dictionary<LocationDataType, int> serializationBuildingData = Dictionary<LocationDataType, int>.Create_WillNeverBeGCed( 90, "SimBuilding-serializationBuildingData" );
        private readonly LocationCalculationCache locationCalculationCache; //initialized in constructor
        
        #region Cleanup
        public override void DoMidCleanupWhenLeavingQuarantineBackIntoMainPool_ClearAsMuchAsPossibleIncludingOutgoingReferences()
        {
            Item = null;
            BuildingID = -1;
            BuildingTurnRandomNumber = 0;
            Prefab = null;
            OriginalPrefab = null;
            Variant = null;
            OriginalVariant = null;
            Status = null;
            overridingAuthorityGroup = null;
            KeyModifierFromPlayer = null;
            KeyModifierFromNPCs = null;
            SwarmSpread = null;
            swarmSpreadCount = 0;
            BlockingUnitsUntilTurn = 0;
            Residents.Clear();
            Workers.Clear();
            this.HasSpecialResourceAlreadyBeenExtracted = false;
            this.IsBlockedFromGettingMoreCitizens = false;
            this.IsViolentCyberocracyTarget = false;
            this.IsPeacefulCyberocracyTarget = false;
            this.LowerClassCitizenGrabCount = 0;
            this.UpperClassCitizenGrabCount = 0;

            MachineStructureInBuilding = null;
            CurrentOccupyingUnit = null;
            currentContemplation.ClearAllVersions();
            currentExplorationSite.ClearAllVersions();
            currentCityConflict.ClearAllVersions();
            currentStreetSenseAction.ClearAllVersions();
            CachedResidentCount = 0;
            CachedWorkerCount = 0;
            Debug = false;
            Mission = null;
            this.roughDistanceFromMachines = 999999;
            this.DebugText = string.Empty;
            //serializationBuildingData is static and does not need clearing this way, nor should it be used by this thread
            //locationCalculationCache has nothing to clean up
        }

        public override void Optional_DoAnyEarlyInits()
        {
            
        }
        #endregion

        #region GetCurrentStreetSenseActionThatShouldShowOnMap  / GetCurrentStreetSenseActionThatShouldShowOnFilteredList
        public StreetSenseDataAtBuilding GetCurrentStreetSenseActionThatShouldShowOnMap()
        {
            StreetSenseDataAtBuilding streetSense = this.currentStreetSenseAction.Display;
            if ( streetSense?.ActionType == null )
                return null;

            if ( this.GetIsDestroyed() )
                return null;

            return GetCurrentStreetSenseActionThatShouldShow_Inner( streetSense, SimCommon.CurrentStreetSenseCollection, true );
        }

        public StreetSenseDataAtBuilding GetCurrentStreetSenseActionThatShouldShowOnFilteredList( StreetSenseCollection FilteredToCollection )
        {
            StreetSenseDataAtBuilding streetSense = this.currentStreetSenseAction.Display;
            if ( streetSense?.ActionType == null )
                return null;

            if ( this.GetIsDestroyed() )
                return null;

            return GetCurrentStreetSenseActionThatShouldShow_Inner( streetSense, FilteredToCollection, false );
        }

        private StreetSenseDataAtBuilding GetCurrentStreetSenseActionThatShouldShow_Inner( StreetSenseDataAtBuilding streetSense, StreetSenseCollection FilteredTo, bool IsForMap )
        {
            if ( streetSense.EventOrNull != null )
            {
                if ( !(streetSense?.EventOrNull?.DuringGame_GetShouldShowOnMapForStreetSense( FilteredTo ) ?? false) )
                    return null;
            }
            else
            {

                if ( !(streetSense?.ActionType?.DuringGame_GetShouldShowOnMapForStreetSense( FilteredTo ) ?? false) )
                    return null;
            }

            if ( streetSense == null )
                return null;

            if ( IsForMap )
            {
                if ( SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false )
                    return streetSense;

                if ( SimCommon.CurrentCityLens?.ShowProjectRelatedStreetSense?.Display ?? false )
                {
                    if ( streetSense.ProjectItemOrNull != null )
                        return streetSense;
                }

                return null;
            }
            else
                return streetSense;
        }
        #endregion

        #region GetCurrentContemplationThatShouldShowOnMap
        public ContemplationType GetCurrentContemplationThatShouldShowOnMap()
        {
            ContemplationType contemplation = this.currentContemplation.Display;
            if ( contemplation == null )
                return null;

            if ( this.GetIsDestroyed() )
                return null;

            if ( !contemplation.DuringGame_GetShouldShowOnMap() )
                return null;
            return contemplation;
        }
        #endregion

        #region GetCurrentExplorationSiteThatShouldShowOnMap
        public ExplorationSiteType GetCurrentExplorationSiteThatShouldShowOnMap()
        {
            ExplorationSiteType explorationSite = this.currentExplorationSite.Display;
            if ( explorationSite == null )
                return null;

            if ( this.GetIsDestroyed() )
                return null;

            if ( !explorationSite.DuringGame_GetShouldShowOnMap() )
                return null;
            return explorationSite;
        }
        #endregion

        #region Serialize Building
        public void Serialize( ArcenFileSerializer Serializer )
        {
            //serializing a map item reference requires these two pieces of data
            Serializer.AddInt32( "ItemID", this.Item.MapItemID );
            Serializer.AddRepeatedlyUsedString_Condensed( "Variant", this.Variant.ShortID );
            Serializer.AddArcenGroundPoint( "ItemCellLoc", this.Item.ParentCell.CellLocation );

            if ( !this.Item.ParentCell.BuildingDict.ContainsKey( this.Item.MapItemGlobalIndex ) )
            {
                ArcenDebugging.LogSingleLine( "Warning, saved a building that was not in the buildings list of its cell.  Type was: " + this.Item.Type.ID, Verbosity.ShowAsError );
            }

            Serializer.AddInt32( "BuildingIndex", this.BuildingID );
            Serializer.AddInt32( "BuildingTurnRandomNumber", this.BuildingTurnRandomNumber );

            //Serializer.AddRepeatedlyUsedString_Condensed( "Prefab", this.Prefab.ID ); we get to skip the building prefab because linking to the MapItem will pull that out for us
            if ( this.OriginalPrefab != null )
                Serializer.AddRepeatedlyUsedString_Condensed( "OriginalPrefab", this.OriginalPrefab.ID );
            if ( this.OriginalVariant != null )
                Serializer.AddRepeatedlyUsedString_Condensed( "OriginalVariant", this.OriginalVariant.ShortID );

            if ( this.Status != null && !this.Status.IsDefault )
                Serializer.AddRepeatedlyUsedString_Condensed( "Status", this.Status.ID );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "AuthorityGroup", this.overridingAuthorityGroup?.ID ?? string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "KeyModifierFromPlayer", this.KeyModifierFromPlayer?.ID ?? string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "KeyModifierFromNPCs", this.KeyModifierFromNPCs?.ID ?? string.Empty );
            Serializer.AddRepeatedlyUsedString_CondensedIfNotBlank( "SwarmSpread", this.SwarmSpread?.ID ?? string.Empty );
            Serializer.AddInt32IfGreaterThanZero( "SwarmSpreadCount", this.swarmSpreadCount );
            Serializer.AddBoolIfTrue( "HasSpecialResourceAlreadyBeenExtracted", this.HasSpecialResourceAlreadyBeenExtracted );
            Serializer.AddBoolIfTrue( "IsBlockedFromGettingMoreCitizens", this.IsBlockedFromGettingMoreCitizens );
            Serializer.AddBoolIfTrue( "IsViolentCyberocracyTarget", this.IsViolentCyberocracyTarget );
            Serializer.AddBoolIfTrue( "IsPeacefulCyberocracyTarget", this.IsPeacefulCyberocracyTarget );
            Serializer.AddInt32IfGreaterThanZero( "LowerClassCitizenGrabCount", this.LowerClassCitizenGrabCount );
            Serializer.AddInt32IfGreaterThanZero( "UpperClassCitizenGrabCount", this.UpperClassCitizenGrabCount );

            if ( this.BlockingUnitsUntilTurn < SimCommon.Turn )
                this.BlockingUnitsUntilTurn = 0;
            Serializer.AddInt32IfGreaterThanZero( "BlockedUntilTurn", this.BlockingUnitsUntilTurn );

            Serializer.AddDictionaryIfHasAnyEntries( ArcenSerializedDataType.RepeatedStringCondensed, ArcenSerializedDataType.Int32, "Residents", this.Residents );
            Serializer.AddDictionaryIfHasAnyEntries( ArcenSerializedDataType.RepeatedStringCondensed, ArcenSerializedDataType.Int32, "Workers", this.Workers );
        }

        public static SimBuilding Deserialize( DeserializedObjectLayer Data )
        {
            int mapItemID = Data.GetInt32( "ItemID", true );
            ArcenGroundPoint itemGroundPoint = Data.GetArcenGroundPoint( "ItemCellLoc", true );

            Int32 buildingIndex = Data.GetInt32( "BuildingIndex", true );

            MapCell parentCell = CityMap.TryGetExistingCellAtLocation( itemGroundPoint );
            if ( parentCell == null )
            {
                ArcenDebugging.LogSingleLine( "Warning: Could not find MapCell at location " + itemGroundPoint + " for SimBuilding " + buildingIndex + "!", Verbosity.DoNotShow );
                return null;
            }
            MapItem mapItem = parentCell.TryGetBuildingWithMapID( mapItemID );
            if ( mapItem == null )
            {
                mapItem = parentCell.TryGetNonBuildingWithMapID( mapItemID );
                if ( mapItem != null )
                {
                    ArcenDebugging.LogSingleLine( "Warning: MapItem with ID " + mapItemID + " was evidently demoted from being a building " + buildingIndex + "!", Verbosity.DoNotShow );
                    return null; //this was a building that was demoted to no longer be a building
                }

                ArcenDebugging.LogSingleLine( "Warning: Could not find MapItem with id " + mapItemID + " on cell at location " + itemGroundPoint +
                    " for SimBuilding! (" + parentCell.BuildingDict.Count + " buildings on that cell.)  Also could not find any other sort of item, so the building " + buildingIndex + " was not seemingly demoted."/*\nItems: " + parentCell.Buildings.ToDebugString()*/, Verbosity.DoNotShow );
                return null;
            }

            //Data.TryGetAttribute( "Prefab", out DeserializedString PrefabID ); we get to skip the building prefab because linking to the MapItem will pull that out for us
            //BuildingPrefab prefab = BuildingPrefabTable.Instance.GetRowByID( PrefabID.value );

            
            SimBuilding building = CreateSimBuilding( mapItem, buildingIndex, false, Engine_Universal.PermanentQualityRandom );
            if ( building == null )
            {
                ArcenDebugging.LogSingleLine( "Warning: old building type " + (mapItem?.Type?.ID ??"[null]") + " was evidently removed! Building " + buildingIndex + " doesn't have a building type linked to it.", Verbosity.DoNotShow );
                return null; //this happens when we remove a building type
            }

            building.BuildingTurnRandomNumber = Data.GetInt32( "BuildingTurnRandomNumber", false );

            BuildingType bType = building.Prefab.Type;
            if ( Data.TryGetString( "Variant", out string VariantID ) ) //note: a random variant is already set at this point, so if we can't find one, that's ok
            {
                if ( bType.Variants.TryGetValue( VariantID, out BuildingTypeVariant variant ) )
                {
                    if ( variant.Weight > 0 )
                        building.Variant = variant;
                }
            }

            if ( building.Variant == null )
                ArcenDebugging.LogSingleLine( "Null variant on deserializing building of type " + bType.ID + "!", Verbosity.ShowAsError );


            if ( Data.TryGetTableRow( "OriginalPrefab", BuildingPrefabTable.Instance, out BuildingPrefab originalPrefab ) )
                building.OriginalPrefab = originalPrefab;

            if ( Data.TryGetString( "OriginalVariant", out string OriginalVariantID ) ) //note: a random variant is already set at this point, so if we can't find one, that's ok
            {
                if ( bType.Variants.TryGetValue( OriginalVariantID, out BuildingTypeVariant originalVariant ) )
                {
                    if ( originalVariant.Weight > 0 )
                        building.OriginalVariant = originalVariant;
                }
            }

            building.Status = Data.GetTableRowOrDefaultRow( "Status", BuildingStatusTable.Instance );

            if ( Data.TryGetTableRow( "AuthorityGroup", NPCCohortTable.Instance, out NPCCohort overridingAuthorityGroup ) )
                building.overridingAuthorityGroup = overridingAuthorityGroup;

            building.KeyModifierFromPlayer = Data.GetTableRow( "KeyModifierFromPlayer", BuildingKeyModifierTable.Instance, false );
            building.KeyModifierFromNPCs = Data.GetTableRow( "KeyModifierFromNPCs", BuildingKeyModifierTable.Instance, false );
            building.SwarmSpread = Data.GetTableRow( "SwarmSpread", SwarmTable.Instance, false );
            building.swarmSpreadCount = Data.GetInt32( "SwarmSpreadCount", false );

            building.HasSpecialResourceAlreadyBeenExtracted = Data.GetBool( "HasSpecialResourceAlreadyBeenExtracted", false );
            building.IsBlockedFromGettingMoreCitizens = Data.GetBool( "IsBlockedFromGettingMoreCitizens", false );
            building.IsViolentCyberocracyTarget = Data.GetBool( "IsViolentCyberocracyTarget", false );
            building.IsPeacefulCyberocracyTarget = Data.GetBool( "IsPeacefulCyberocracyTarget", false );
            building.LowerClassCitizenGrabCount = Data.GetInt32( "LowerClassCitizenGrabCount", false );
            building.UpperClassCitizenGrabCount = Data.GetInt32( "UpperClassCitizenGrabCount", false );

            if ( Data.TryGetInt32( "BlockedUntilTurn", out Int32 blockedUntilTurn ) )
                building.BlockingUnitsUntilTurn = blockedUntilTurn;

            if ( Data.TryGetDictionary( "Residents", out Dictionary<string, int> residentsDict ) )
            {
                foreach ( KeyValuePair<string, int> kv in residentsDict )
                {
                    EconomicClassType key = EconomicClassTypeTable.Instance.GetRowByID( kv.Key );
                    building.Residents.Set_BeVeryCarefulOfUsing( key, kv.Value );
                }
            }

            if ( Data.TryGetDictionary( "Workers", out Dictionary<string, int> workersDict ) )
            {
                foreach ( KeyValuePair<string, int> kv in workersDict )
                {
                    ProfessionType key = ProfessionTypeTable.Instance.GetRowByID( kv.Key );
                    building.Workers.Set_BeVeryCarefulOfUsing( key, kv.Value );
                }
            }

            if ( building.Variant.Type.POIToSpawn != null && (mapItem.GetParentPOIOrNull() == null || mapItem.GetParentPOIOrNull().Type != building.Variant.Type.POIToSpawn) )
            {
                //if ( mapItem.ParentTile != null && !building.GetIsDestroyed() )
                //    ArcenDebugging.LogSingleLine( "status: " + building.Status.ID, Verbosity.DoNotShow );

                if ( mapItem.ParentTile != null && !building.GetIsDestroyed() && !mapItem.ParentCell.IsCellConsideredIrradiated )
                {
                    bool foundSelf = false;
                    foreach ( KeyValuePair<short, MapPOI> kv in CityMap.POIsByID )
                    {
                        if ( kv.Value.BuildingOrNull == mapItem )
                        {
                            foundSelf = true;
                            //ArcenDebugging.LogSingleLine( "building: " + building.Variant.Type.ID + " status: " + building.Status.ID + " wants " + 
                            //    building.Variant.Type.POIToSpawn.ID + " and has " + kv.Value.Type.ID, Verbosity.DoNotShow );
                            break;
                        }
                    }

                    if ( !foundSelf )
                    {
                        //ArcenDebugging.LogSingleLine( "building: " + building.Variant.Type.ID + " status: " + building.Status.ID, Verbosity.DoNotShow );
                        MapPOI.Mapgen_CreateNewPOI_Building( building.Variant.Type.POIToSpawn, mapItem.ParentTile, mapItem );
                    }
                }

                //ArcenDebugging.LogSingleLine( "missing poi of type " + building.Variant.Type.POIToSpawn.ID + "' on building '" + building.Variant.Type.ID + "'", Verbosity.DoNotShow );
            }

            return building;
        }
        #endregion

        #region CreateSimBuilding
        public static SimBuilding CreateSimBuilding( MapItem item, int idx, bool DoFreshInitialization, MersenneTwister RandToUse )
        {
            if ( item?.Type?.Building == null )
            {
                //Chris says: don't complain if this happens, this is probably an older savegame.
                //ArcenDebugging.LogSingleLine( "Tried to create a SimBuilding with a MapItem (" + item.Type.ID + ") that had " + (item.Type.Building == null ? 0 : 1) + " building prefabs", Verbosity.ShowAsError );
                return null;
            }
            SimBuilding building = SimBuilding.GetFromPoolOrCreate();
            building.Item = item;
            building.Prefab = item.Type.Building;
            building.BuildingID = idx;
            building.Status = BuildingStatusTable.Instance.DefaultRow;

            BuildingType bType = item.Type.Building.Type;
            if ( bType.Variants.Count > 1 && bType.VariantDrawBag.HasAnyContent )
                building.Variant = bType.VariantDrawBag.PickRandom( RandToUse );
            else
                building.Variant = bType.Variants.FirstOrDefault;

            if ( building.Variant == null )
                ArcenDebugging.LogSingleLine( "Null variant on creating building of type " + bType.ID + "!", Verbosity.ShowAsError );

            if ( building.Debug )
            {
                //ArcenDebugging.LogSingleLine("Starting Resources: " + Variant.ResourcesToString(), Verbosity.DoNotShow );
            }
            item.SimBuilding = building; //let the rest of the game query our status here
            return building;
        }
        #endregion

        #region GetSecurityClearanceOfPOI
        public SecurityClearance GetSecurityClearanceOfPOI()
        {
            return this.Item?.GetParentPOIOrNull()?.Type?.RequiredClearance;
        }
        #endregion

        #region CalculateLocationSecurityClearanceType
        public SecurityClearance CalculateLocationSecurityClearanceType()
        {
            SecurityClearance tilePOIClear = this.Item?.ParentTile.POIOrNull?.Type?.RequiredClearance;
            SecurityClearance poiClear = this.Item?.GetParentPOIOrNull()?.Type?.RequiredClearance;
            SecurityClearance myClear = this.Variant.RequiredClearance;
            if ( poiClear == null || (myClear?.Level ?? 0) > (poiClear?.Level ?? 0) )
            {
                return myClear;
            }


            return poiClear;
        }
        #endregion

        #region CalculateLocationSecurityClearanceInt
        public int CalculateLocationSecurityClearanceInt()
        {
            int poiClear = this.Item?.GetParentPOIOrNull()?.Type?.RequiredClearance?.Level??0;
            int myClear = this.Variant?.RequiredClearance?.Level??0;
            int tilePOIClear = this.Item?.ParentTile.POIOrNull?.Type?.RequiredClearance?.Level ?? 0;

            return MathA.Max( poiClear, myClear, tilePOIClear );
        }
        #endregion

        public NPCCohort CalculateLocationLocalAuthority()
        {
            if ( this.overridingAuthorityGroup != null )
                return this.overridingAuthorityGroup;

            MapPOI poiOrNull = this.Item?.GetParentPOIOrNull();
            NPCCohort group = poiOrNull?.ControlledBy;
            if ( group != null )
                return group;
            return this.Item?.ParentTile?.District?.ControlledBy;
        }

        public void SetPrefab( BuildingPrefab NewPrefab, float RiseSpeed, VisParticleAndSoundUsage OnRiseStart, VisParticleAndSoundUsage OnRiseEnd )
        {
            if ( NewPrefab == null )
                return;
            if ( NewPrefab == this.Prefab ) 
                return;

            if ( this.OriginalPrefab == null )
                this.OriginalPrefab = this.Prefab;

            if ( this.OriginalVariant == null )
                this.OriginalVariant = this.Variant;

            this.Prefab = NewPrefab;
            this.Item.Type = NewPrefab.PlaceableRoot;
            this.Item.Scale = NewPrefab.PlaceableRoot.OriginalScale;
            this.Item.Position = this.Item.OBBCache.Center.ReplaceY( NewPrefab.PlaceableRoot.AlwaysDropTo )
                .PlusX( NewPrefab.PlaceableRoot.OriginalPivot.x ).PlusZ( NewPrefab.PlaceableRoot.OriginalPivot.z );
            this.Item.FillOBBCache();

            this.Variant = NewPrefab.Type.VariantDrawBag.PickRandom( Engine_Universal.PermanentQualityRandom );

            if ( RiseSpeed > 0 )
            {
                this.Item.RiseSpeed = RiseSpeed;
                this.Item.NonSimDrawOffset = -this.Item.OBBCache.OBBSize.y;
                this.Item.AfterRiseComplete = OnRiseEnd;
            }

            MapCollidersCoordinator.CityItemsNeedingColliders_LaterOn.Enqueue( this.Item );

            if ( OnRiseStart != null )
                OnRiseStart.DuringGame_PlayAtLocation( this.Item.CenterPoint );
        }

        public bool GetIsBlockedFromHavingMissionAddedHere()
        {
            if ( this.Mission != null )
                return true;
            return false;
        }

        #region GetEffectiveAuthority
        public NPCCohort GetEffectiveAuthority()
        {
            if ( this.overridingAuthorityGroup != null )
                return this.overridingAuthorityGroup;

            MapPOI poiOrNull = this.Item?.GetParentPOIOrNull();
            NPCCohort poiOwner = poiOrNull?.ControlledBy;
            if (poiOwner != null)
                return poiOwner;
            MapCell cell = this.GetLocationMapCell();
            NPCCohort districtGroup = cell?.ParentTile?.District?.ControlledBy;
            if ( districtGroup != null )
                return districtGroup;
            return CohortRefs.MegaCorpEnforcement;
        }
        #endregion

        #region SwarmSpreadCount
        public int SwarmSpreadCount => this.swarmSpreadCount;

        public void AlterSwarmSpreadCount( int AlterBy )
        {
            if ( AlterBy == 0 )
                return;
            Interlocked.Add( ref this.swarmSpreadCount, AlterBy );
            if ( this.swarmSpreadCount < 0 )
                Interlocked.Add( ref this.swarmSpreadCount, -this.swarmSpreadCount );
        }

        public void SetSwarmSpreadCount( int SetTo )
        {
            Interlocked.Exchange( ref this.swarmSpreadCount, SetTo );
        }
        #endregion

        #region RecalculateCacheForResidentsAndWorkers
        public void RecalculateCacheForResidentsAndWorkers()
        {
            #region CachedResidentCount
            int residentCount = 0;

            foreach ( KeyValuePair<EconomicClassType, int> kv in this.Residents )
            {
                int val = kv.Value;
                if ( val > 0 )
                    residentCount += val;
                else if ( val < 0 )
                    this.Residents.Set_BeVeryCarefulOfUsing( kv.Key, 0 );
            }

            this.CachedResidentCount = residentCount;
            #endregion

            #region CachedWorkerCount
            int workerCount = 0;

            foreach ( KeyValuePair<ProfessionType, int> kv in this.Workers )
            {
                int val = kv.Value;
                if ( val > 0 )
                    workerCount += val;
                else if ( val < 0 )
                    this.Workers.Set_BeVeryCarefulOfUsing( kv.Key, 0 );
            }

            this.CachedWorkerCount = workerCount;
            #endregion
        }
        #endregion

        #region CalculateIsValidTargetForMachineStructureRightNow
        public bool CalculateIsValidTargetForMachineStructureRightNow( MachineStructureType StructureType, MachineJob StartingJobOrNull )
        {
            if ( StructureType == null )
                return false;
            if ( this.MachineStructureInBuilding != null )
                return false;
            if ( this.Prefab?.MarkerPrefab == null )
                return false;
            MapItem item = this.GetMapItem();
            if ( item == null )
                return false;

            BuildingStatus status = this.Status;
            if ( status == null )
                return false;

            if ( status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually || status.IsBuildingConsideredToBeUnderConstruction )
                return false;

            BuildingTypeVariant variant = this.Variant;
            if ( variant == null )
                return false;

            MapPOI poi = item.GetParentPOIOrNull();
            if ( poi != null && (poi.Type?.RequiredClearance?.Level??0) > StructureType.MaxSecurityClearanceOfPOIForPlacement )
                return false;
            if ( StructureType.RequiredPOITagForPlacement != null )
            {
                if ( poi == null || !poi.Type.Tags.ContainsKey( StructureType.RequiredPOITagForPlacement.ID ) )
                    return false;
            }


            if ( poi != null && poi.BuildingOrNull == item )
                return false; //no building in structures that are a poi all on their own

            if ( StructureType == null || StructureType.IsEmbeddedInHumanBuildingOfTag == null )
                return false;
            if ( !variant.Tags.ContainsKey( StructureType.IsEmbeddedInHumanBuildingOfTag.ID ) )
            {
                return false;
            }
            if ( StructureType.EstablishesNetworkOnBuild && !StructureType.IsNotPickyAboutCellValidityForNetworkStart )
            {
                MapCell cell = this.GetLocationMapCell();
                if ( cell == null || !cell.Cached_CalculateIsValidStartingCellForTower )
                    return false;

                MachineNetwork network = SimCommon.TheNetwork;
                if ( network != null && network.Tower != null )
                    return false; //can only have one network!

                BeaconType beacon = variant.BeaconToShow;
                if ( beacon != null )
                    return false; //no building on beacon sites if a network

                if ( poi != null )
                    return false;
            }

            if ( StructureType.CanOnlyBeBuiltOutsideExistingNetworkRange )
            {
                MapTile tile = this.GetLocationMapTile();
                if ( tile == null || tile.TileNetworkLevel.Display != TileNetLevel.None )
                    return false;
            }

            if ( StartingJobOrNull != null )
            {
                if ( StartingJobOrNull.DistanceRestriction != null )
                {
                    Vector3 myPos = this.GetPositionForCameraFocus();

                    foreach ( MachineStructure structure in StartingJobOrNull.DistanceRestriction.DuringGame_FullList.GetDisplayList() )
                    {
                        if ( structure == null ) 
                            continue;

                        Vector3 pos = structure.GetGroundCenterLocation();
                        float dist = (pos - myPos).GetSquareGroundMagnitude();
                        if ( dist > StartingJobOrNull.DistanceRestriction.DistanceTheseMustBeFromOneAnotherSquared )
                            continue; //far enough away, no complaints!

                        return false; //blocked by distance restriction!
                    }
                }
            }

            if ( StructureType.IsCyberocracyHub )
            {
                MapCell cell = this.GetLocationMapCell();
                if ( cell == null )
                    return false;

                if ( cell.PrisonersInCell.Display > 0 )
                    return false; //cannot build over prisons

                if ( cell.LowerAndWorkingClassResidentsAndWorkersInCell.Display < MathRefs.CyberocracyHubMinLowerClassesPresent.IntMin )
                    return false; //if too few lower and middle class residents
                if ( cell.UpperClassResidentsAndWorkersInCell.Display > MathRefs.CyberocracyHubMaxUpperClassesPresent.IntMin )
                    return false; //if too many upper class workers and residents

                if ( cell.IsPotentialCyberocracyCell.Display )
                    return false; //if already has one there
            }

            return true;
        }
        #endregion

        #region Debug Strings
        public string ToDebugString()
        {
            string output = "Variant " + BuildingID + " " + this.Prefab.GetDisplayName();
            if (this.CachedResidentCount > 0)
            {
                output += " Residents: ";
				//do this if we want
                /*this.CurrentResidents.DoFor(delegate (KeyValuePair<EconomicClassType, int> pair)
                {
                    output += ", " + pair.Key + " " + pair.Value;

                    //ArcenDebugging.LogSingleLine(this.ToDebugString() + " in current residents: " + pair.Key + " " + pair.Value, Verbosity.DoNotShow);
                    return DelReturn.Continue;
                });*/
            }
            else
                output += " No Residents";
            
            return output;

            //at " + this.Item.CenterPoint + " diam " + this.Item.Diameter;
        }
        #endregion

        #region Pooling Logic
        public static Int64 NumberRawSimBuildingsCreated_Ever = 0;
        public readonly Int64 PermaDebugNonSimID;

        public static readonly ReferenceTracker RefTracker = new ReferenceTracker( "SimBuilding_Building" );
        public static int NonSimUniqueIDGenerator = 0;
        public int NonSimPermanentUniqueID = -1;
        private SimBuilding()
        {
            if ( RefTracker != null )
                RefTracker.IncrementObjectCount();
            PermaDebugNonSimID = System.Threading.Interlocked.Add( ref NumberRawSimBuildingsCreated_Ever, 1 );
            this.NonSimPermanentUniqueID = ++NonSimUniqueIDGenerator;
            this.locationCalculationCache = new LocationCalculationCache( this );
        }

        private static readonly TimeBasedPool<SimBuilding> internalTimeBasedPool = TimeBasedPool<SimBuilding>.Create_WillNeverBeGCed( "SimBuilding-Building-Pool", 2, 30,
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new SimBuilding(); } );

        public static void InitializePoolIfNeeded( ref long poolCount, ref long poolItemCount )
        {
            poolCount++;
            poolItemCount += internalTimeBasedPool.DoInitializationIfNeeded( 32000 ); //32000 should be enough to handle most of the map
        }

        public static SimBuilding GetFromPoolOrCreate()
        {
            return internalTimeBasedPool.GetFromPoolOrCreate();
        }

        public static int TotalSimBuildingsQuarantined
        {
            get { return internalTimeBasedPool.GetCountOfQuarantinedItems(); }
        }

        public void ReturnToPool()
        {
            internalTimeBasedPool.ReturnToPool( this );
        }
        #region PoolingCleanup
        public override void DoEarlyCleanupWhenGoingIntoQuarantine_ClearIncomingPointersButNotOutgoingReferences()
        {

        }
        public override void DoAnyBelatedCleanupWhenComingOutOfPool_ShouldBeVeryLittleNeeded()
        {

        }
        #endregion PoolingCleanup
        #endregion PoolingLogic

        internal void HandleUIRequest( IUIRequestToBuilding Request )
        {
            //if ( Request is SwitchMachineApplianceType switchApplianceType )
            //    this.HandleUIRequest_SwitchMachineApplianceType( switchApplianceType );
            //else if ( Request is SwitchMachineApplianceSlotType switchApplianceSlotType )
            //    this.HandleUIRequest_SwitchMachineApplianceSlotType( switchApplianceSlotType );
            //else
            {
                ArcenDebugging.LogSingleLine( "Unknown IUIRequestToBuilding type '" + Request.GetType().Name + "' in Building HandleUIRequest.", Verbosity.ShowAsError );
            }
        }

		#region PercentagesDouble
		/// <summary>
		/// Ranges from 0 to 1
		/// </summary>
		public double GetTotalWorkerPercentageDouble( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxJobs <= 0 )
                return NotApplicableIs100 ? 1 : 0;
            return ((double)this.CachedWorkerCount / (double)this.Prefab.NormalMaxJobs);
        }

        /// <summary>
        /// Ranges from 0 to 1
        /// </summary>
        public double GetTotalResidentPercentageDouble( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxResidents <= 0 )
                return NotApplicableIs100 ? 1 : 0;
           return ((double)this.CachedResidentCount / (double)this.Prefab.NormalMaxResidents);
        }
        #endregion

        #region PercentagesFloat
        /// <summary>
        /// Ranges from 0 to 1
        /// </summary>
        public float GetTotalWorkerPercentageFloat( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxJobs <= 0 )
                return NotApplicableIs100 ? 1 : 0;
            return ((float)this.CachedWorkerCount / (float)this.Prefab.NormalMaxJobs);
        }

        /// <summary>
        /// Ranges from 0 to 1
        /// </summary>
        public float GetTotalResidentPercentageFloat( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxResidents <= 0 )
                return NotApplicableIs100 ? 1 : 0;
            return ((float)this.CachedResidentCount / (float)this.Prefab.NormalMaxResidents);
        }
        #endregion

        #region GetShouldBeIncludedInMachineActorLists
        public bool GetShouldBeIncludedInMachineActorLists()
        {
            BuildingType buildingType = this.Prefab?.Type;
            if ( buildingType == null )
                return false;
            //include these!  It's a problem when they are skipped
            //if ( buildingType.IsConsideredNotToHavePeopleInsideEvenWhenHasWorkers || buildingType.IsExcludedFromCityNetwork )
            //    return false;
            if ( this.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                return false;
            return true;
        }
        #endregion

        #region ISimBuilding
        public MapItem GetMapItem()
        {
            return this.Item;
        }

        public BuildingPrefab GetPrefab()
        {
            return this.Prefab;
        }

        public BuildingTypeVariant GetVariant()
        {
            return this.Variant;
        }

        public BuildingStatus GetStatus()
        {
            if ( this.Status == null )
                this.Status = BuildingStatusTable.Instance.DefaultRow;
            return this.Status;
        }
        public void SetStatus( BuildingStatus NewStatus )
        {
            if ( this.Status ==  NewStatus ) 
                return; //if already the same, nevermind

            this.Status = NewStatus;

            if ( this.CurrentOccupyingUnit != null )
                this.CurrentOccupyingUnit.SetActualContainerLocation( this ); //reset the location just in case
        }

        public bool GetIsDestroyed()
        {
            BuildingStatus status = this.GetStatus();
            if ( status == null )
                return true;
            if ( status.ShouldBuildingBeNonfunctional )
                return true;

            return false;
        }

        public MapCell GetParentCell()
        {
            return this.Item?.ParentCell;
        }

        public MapDistrict GetParentDistrict()
        {
            return this.Item?.ParentCell?.ParentTile?.District;
        }

        public Vector3 GetEffectiveWorldLocationForContainedUnit() //if any changes are made here, be sure to inline them to GetIsNPCBlockedFromComingHere
        {
            BuildingStatus status = this.Status;
            if ( status != null )
            {
                if ( status.ShouldBuildingBeNonfunctional )
                    return (this.Item?.GroundCenterPoint ?? Vector3.zero).PlusY( 0.1f );
            }

            return (this.Item?.TopCenterPoint??Vector3.zero).PlusY( 0.1f );
        }

        public bool GetIsVisibleOnMapWorldLocationForContainedUnit()
        {
            return this.Item != null;
        }

        public string GetLocationNameForNPCEvents()
        {
            return this.Prefab?.Type?.GetDisplayName() ?? string.Empty;
        }

        public MapCell GetLocationMapCell()
        {
            return this.Item?.ParentCell;
        }

        public MapTile GetLocationMapTile()
        {
            return this.Item?.ParentTile;
        }

        public MapDistrict GetLocationDistrict()
        {
            return this.Item?.ParentTile?.District;
        }

        public DictionaryView<LocationDataType,int> GetBuildingData()
        {
            return DictionaryView<LocationDataType, int>.Create( this.Variant?.InitialBuildingRatings );
        }

        public int GetBuildingDataValue( LocationDataType Type )
        {
            return this.Variant?.InitialBuildingRatings[Type]??0;
        }

        public int GetBuildingDataValue( string TypeName )
        {
            LocationDataType type = LocationDataTypeTable.Instance.GetRowByID( TypeName );
            if ( type == null )
                return 0;
            return this.Variant?.InitialBuildingRatings[type]??0;
        }
        
        public LocationCalculationCache GetLocationCalculationCache()
        {
            return this.locationCalculationCache;
        }

        public int GetBuildingID()
        {
            return this.BuildingID;
        }

        public int GetInt32ValueForSerialization()
        {
            return this.BuildingID;
        }

        public int GetBuildingTurnRandomNumber()
        {
            return this.BuildingTurnRandomNumber;
        }

        public int CompareTo( ISimBuilding Other )
        {
            if ( Other == null )
                return 1;
            int val = this.BuildingTurnRandomNumber.CompareTo( Other.GetBuildingTurnRandomNumber() );
            if ( val != 0 )
                return val;
            return this.BuildingID.CompareTo( Other.GetBuildingID() );
        }

        public bool EqualsRelated( IAutoRelatedObject Other )
        {
            if ( Other is SimBuilding Building )
                return this.BuildingID == Building.BuildingID;
            return false;
        }

        /// <summary>
        /// Ranges from 0 to 100
        /// </summary>
        public int GetTotalWorkerPercentage( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxJobs <= 0 )
                return NotApplicableIs100 ? 100 : 0;
            return Mathf.RoundToInt( ((float)this.CachedWorkerCount / (float)this.Prefab.NormalMaxJobs) * 100 );
        }

        /// <summary>
        /// Ranges from 0 to 100
        /// </summary>
        public int GetTotalResidentPercentage( bool NotApplicableIs100 )
        {
            if ( this.Prefab.NormalMaxResidents <= 0 )
                return NotApplicableIs100 ? 100 : 0;
            return Mathf.RoundToInt( ( (float)this.CachedResidentCount / (float)this.Prefab.NormalMaxResidents) * 100 );
        }

        public int GetTotalWorkerCount()
        {
            return this.CachedWorkerCount;
        }

        public int GetTotalResidentCount()
        {
            return this.CachedResidentCount;
        }

        public int GetResidentAmount( EconomicClassType classTypeOrNull)
        {
            if (classTypeOrNull == null) 
                return GetTotalResidentCount();
	        return this.Residents[classTypeOrNull];
        }

        public int GetWorkerAmount( ProfessionType professionTypeOrNull )
        {
            if ( professionTypeOrNull == null ) 
                return GetTotalWorkerCount();
            return this.Workers[professionTypeOrNull];
        }

        public ThreadsafeTableDictionary32<EconomicClassType> GetResidents()
        {
            return this.Residents;
        }
        public ThreadsafeTableDictionary32<ProfessionType> GetWorkers()
        {
            return this.Workers;
        }

        public void FullyDeleteBuilding()
        {
            World_Buildings.BuildingsByID.TryRemove( this.BuildingID, 10 );

            MapItem item = this.Item;

            this.ReturnToPool();

            if ( item == null )
                return;

            item.SimBuilding = null;
            item.FullyDeleteMapItem_Building();
        }

        public int KillEveryoneHere()
        {
            int numberKilled = 0;
            foreach ( KeyValuePair<ProfessionType, int> kv in this.Workers )
            {
                if ( kv.Value > 0 )
                {
                    numberKilled += kv.Value;
                    int amount = kv.Value;
                    World_People.QueryInstance.KillResidentsCityWide( kv.Key, ref amount, Engine_Universal.PermanentQualityRandom );
                    this.Workers.Set_BeVeryCarefulOfUsing(kv.Key, 0 );
                }
            }
            this.CachedWorkerCount = 0;

            foreach ( KeyValuePair<EconomicClassType, int> kv in this.Residents )
            {
                if ( kv.Value > 0 )
                {
                    numberKilled += kv.Value;
                    int amount = kv.Value;
                    World_People.QueryInstance.KillResidentsCityWide( kv.Key, ref amount, Engine_Universal.PermanentQualityRandom );
                    this.Residents.Set_BeVeryCarefulOfUsing( kv.Key, 0 );
                }
            }
            this.CachedResidentCount = 0;

            return numberKilled;
        }

        public void KillRandomHere( int AmountToKill, RandomGenerator Random )
        {
            int halfAmount = AmountToKill / 2;
            int workingAmount = halfAmount;

            this.KillSomeWorkersHere( null, ref workingAmount, Random );

            workingAmount += halfAmount;
            this.KillSomeResidentsHere( null, ref workingAmount, Random );

            if ( workingAmount > 0 )
                this.KillSomeWorkersHere( null, ref workingAmount, Random );
        }

        public int AbandonEveryoneHere()
        {
            //the workers lose their jobs, but that's it
            foreach ( KeyValuePair<ProfessionType, int> kv in this.Workers )
            {
                if ( kv.Value > 0 )
                    this.Workers.Set_BeVeryCarefulOfUsing( kv.Key, 0 );
            }
            this.CachedWorkerCount = 0;

            int numberAbandoned = 0;
            foreach ( KeyValuePair<EconomicClassType, int> kv in this.Residents )
            {
                if ( kv.Value > 0 )
                {
                    numberAbandoned += kv.Value;
                    int amount = kv.Value;
                    World_People.QueryInstance.KillResidentsCityWide( kv.Key, ref amount, Engine_Universal.PermanentQualityRandom );
                    this.Residents.Set_BeVeryCarefulOfUsing( kv.Key, 0 );
                }
            }
            this.CachedResidentCount = 0;

            if ( numberAbandoned > 0 )
                ResourceRefs.AbandonedHumans.AlterCurrent_Named( numberAbandoned, "Increase_DisplacedByConstruction", ResourceAddRule.IgnoreUntilTurnChange );

            return numberAbandoned;
        }

        public void KillSomeWorkersHere(ProfessionType professionTypeOrNull, ref int NumberToKill, RandomGenerator Random)
        {
	        int amt = NumberToKill;
	        if (amt < 1) return;
			if (professionTypeOrNull == null)
	        {
                foreach ( KeyValuePair<int, ProfessionType> kv in ProfessionTypeTable.Instance.Rows.GetRandomStartEnumerable( Random ) )
				{
					if( kv.Value != null)
						KillSomeWorkersHere( kv.Value, ref amt, Random);
				}
				return;
	        }

            int workersCount = this.Workers[professionTypeOrNull];

            amt = MathA.Min(amt, workersCount );
            workersCount = this.Workers.Add( professionTypeOrNull, -amt );
            this.CachedWorkerCount -= amt;
            if ( this.CachedWorkerCount < 0 )
                this.CachedWorkerCount = 0;
            if ( workersCount < 0 ) //this would just be a race condition
                this.Workers.Add( professionTypeOrNull, -workersCount );
            World_People.QueryInstance.KillResidentsCityWide(professionTypeOrNull, ref amt, Random );
			NumberToKill -= amt;
        }

        public void KillSomeResidentsHere_BiasedLower(int NumberToKill, RandomGenerator Random)
        {
	        int amt = NumberToKill;
	        int j = Random.NextInclus(0, 1);
	        for (int i = 0; i < 2; i++)
	        {
                int index = (i + j) % EconomicClassTypeTable.Instance.Rows.Length;
                if ( index < 0 )
                    index = 0;
                else if ( index >= CommonRefs.LowerClassResidents.Length )
                    index = CommonRefs.LowerClassResidents.Length - 1;

                EconomicClassType econClass = CommonRefs.LowerClassResidents[index];

		        KillSomeResidentsHere(econClass, ref amt, Random);
	        }
	        if (amt > 0)
	        {
		        KillSomeResidentsHere(CommonRefs.ManagerialClass, ref amt, Random);
	        }
	        if (amt > 0)
	        {
		        KillSomeResidentsHere(CommonRefs.ScientistClass, ref amt, Random);
	        }
	        if (amt > 0)
	        {
		        KillSomeResidentsHere(CommonRefs.HomelessClass, ref amt, Random);
	        }
		}
        public void KillSomeResidentsHere(EconomicClassType economicClassTypeOrNull, ref int NumberToKill, RandomGenerator Random)
		{
			int amt = NumberToKill;
			if (amt < 1) return;
			if (economicClassTypeOrNull == null)
			{
				KillSomeResidentsHere_BiasedLower(NumberToKill, Random);
				return;
	        }

            int residentsCount = this.Residents[economicClassTypeOrNull];
            amt = MathA.Min( amt, residentsCount );
            residentsCount = this.Residents.Add( economicClassTypeOrNull, -amt );
            this.CachedResidentCount -= amt;
            if ( this.CachedResidentCount < 0 )
                this.CachedResidentCount = 0;
            if ( residentsCount < 0 ) //this would just be a race condition
                this.Residents.Add( economicClassTypeOrNull, -residentsCount );
        }

        public int KillAnyWorkersHereOfThisType( ProfessionType professionType )
        {
            int workersCount = this.Workers[professionType];
            if ( workersCount <= 0 ) 
                return 0;

            workersCount = this.Workers.Add( professionType, -workersCount );
            this.CachedWorkerCount -= workersCount;
            if ( this.CachedWorkerCount < 0 )
                this.CachedWorkerCount = 0;
            if ( workersCount < 0 ) //this would just be a race condition
                this.Workers.Add( professionType, -workersCount );
            World_People.QueryInstance.KillResidentsCityWide( professionType, ref workersCount, Engine_Universal.PermanentQualityRandom );
            return workersCount;
        }

        public int KillAnyResidentsHereOfThisType( EconomicClassType economicClassType )
        {
            int resCount = this.Residents[economicClassType];
            if ( resCount <= 0 )
                return 0;

            resCount = this.Residents.Add( economicClassType, -resCount );
            this.CachedResidentCount -= resCount;
            if ( this.CachedResidentCount < 0 )
                this.CachedResidentCount = 0;
            if ( resCount < 0 ) //this would just be a race condition
                this.Residents.Add( economicClassType, -resCount );
            World_People.QueryInstance.KillResidentsCityWide( economicClassType, ref resCount, Engine_Universal.PermanentQualityRandom );
            return resCount;
        }

        public string GetDisplayName()
        {
            if ( this.Variant == null )
                return "[null!?!?]";

            return this.Variant.GetDisplayName();
        }

        public Vector3 GetPositionForCameraFocus()
        {
            return this.Item.OBBCache.Center;
        }

        public void ClearOccupyingUnitIfThisOne( ISimUnit Unit )
        {
            if ( this.CurrentOccupyingUnit == Unit )
                this.CurrentOccupyingUnit = null;
        }

        public void SetOccupyingUnitToThisOne( ISimUnit Unit )
        {
            this.CurrentOccupyingUnit = Unit;
        }

        public bool GetIsNPCBlockedFromComingHere( NPCUnitStance Stance )
        {
            if ( this.MachineStructureInBuilding?.TerritoryControlType != null )
            {
                if ( !Stance.IsPassiveHarvesterForTerritoryControlFlag )
                    return true; //all npcs except harvesters blocked from being directly on the territory control flag
            }

            if ( Stance != null && Stance.MaxHeightAllowed > 0 )
            {
                //inlined from GetEffectiveWorldLocationForContainedUnit 

                float yTop = 0;
                BuildingStatus status = this.Status;
                if ( status != null && status.ShouldBuildingBeNonfunctional )
                    yTop = ( this.Item?.GroundCenterPoint.y ?? 0) + 0.1f;
                else
                    yTop = (this.Item?.TopCenterPoint.y ?? 0) + 0.1f;

                if ( yTop > Stance.MaxHeightAllowed )
                {
                    //ArcenDebugging.LogSingleLine( "Blocked npc with yTop " + yTop + ", max allowed was " + Stance.MaxHeightAllowed, Verbosity.DoNotShow );
                    return true; //NPC not allowed to go that high
                }
                else
                {
                    //ArcenDebugging.LogSingleLine( "Allowed npc with yTop " + yTop + ", max allowed was " + Stance.MaxHeightAllowed, Verbosity.DoNotShow );
                }
            }
            //then do GetAreMoreUnitsBlockedFromComingHere
            return this.CurrentOccupyingUnit != null || this.BlockingUnitsUntilTurn > SimCommon.Turn;
        }

        public bool GetAreMoreUnitsBlockedFromComingHere()
        {
            if ( this.BlockingUnitsUntilTurn > SimCommon.Turn )
                return true;

            ISimUnit unit = this.CurrentOccupyingUnit;
            if ( unit != null && unit.ContainerLocation.Get() == this )
                return true;
            return false;
        }

        public void MarkAsBlockedUntilNextTurn( ISimUnit Blocker )
        {
            this.BlockingUnitsUntilTurn = SimCommon.Turn + 1;
            if ( Blocker != null && this.Item?.ParentCell != null )
            {
                Vector3 position = this.GetEffectiveWorldLocationForContainedUnit();
                if ( Blocker.GetMustStayOnGround() )
                    position.y = 0;

                IReadOnlyList<MapCell> cells = this.Item.ParentCell.AdjacentCellsAndSelf;
                foreach ( MapCell cell in cells )
                    cell.BlockingCollidablesUntilNextTurn.AddEntry( Blocker, position );
            }
        }

        public void DoOnFocus_Streets()
        {
        }

        public void DoOnFocus_CityMap()
        {
        }

        public MapPOI CalculateLocationPOI()
        {
            return this.Item?.GetParentPOIOrNull();
        }

        public bool GetIsLocationInFogOfWar()
        {
            return this.Item?.IsItemInFogOfWar??true;
        }

        public bool GetIsGroundStyleLocation()
        {
            return false;
        }

        public bool ShouldFreezeCameraAtTheMoment => false;
        #endregion

        #region Interface Writing
        public void WriteDataItemUIXTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( this ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
            {
                //what do you want to know about this building in the tooltip on the building directory?  Write that here.

                novel.TitleUpperLeft.AddRaw( this.GetDisplayName() );
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_ResidencyPercent", ColorTheme.DataLabelWhite )
                    .AddRaw( this.GetTotalResidentPercentage( false ).ToStringIntPercent(), ColorTheme.DataBlue ).Line();
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_StaffingPercent", ColorTheme.DataLabelWhite )
                    .AddRaw( this.GetTotalWorkerPercentage( false ).ToStringIntPercent(), ColorTheme.DataBlue ).Line();

                novel.Main.Line().AddRightClickFormat( "BuildingDirectory_TooltipFooter", ColorTheme.SoftGold );
            }
        }

        public void WriteDataItemUIXClickedDetails( ArcenDoubleCharacterBuffer Buffer )
        {
            this.WriteWorldExamineDetails( Buffer );
        }

        public void WriteDataItemUIXClickedDetails_SubTooltipLinkHover( string[] TooltipLinkData )
        {
            this.WriteWorldExamineDetails_SubTooltipLinkHover( TooltipLinkData );
        }
       
        public void WriteWorldExamineDetails( ArcenDoubleCharacterBuffer Buffer )
        {
            //what do you want to see about this building when clicking into the details of this building on the main map?  Write that here.

            if ( this.Prefab.NormalMaxResidents > 0 || this.Prefab.NormalMaxJobs > 0 )
            {
                //percentages staffed and residents
                if ( this.Prefab.NormalMaxResidents > 0 )
                    Buffer.AddFormat1( "PopulationStatistics_ResidencyPercent", this.GetTotalResidentPercentage( false ) );

                if ( this.Prefab.NormalMaxJobs > 0 )
                {
                    if ( this.Prefab.NormalMaxResidents > 0 )
                        Buffer.Space8x();
                    Buffer.AddFormat1( "PopulationStatistics_StaffingPercent", this.GetTotalWorkerPercentage( false ) );
                }
                Buffer.Line();
            }

            //Status
            Buffer.AddLangAndAfterLineItemHeader( "BuildingStatus", ColorTheme.Gray );
            BuildingStatus status = this.GetStatus();
            Buffer.AddRaw( status.GetDisplayName(), status.ColorHex ).Line();

            //Local Authority
            Buffer.AddLangAndAfterLineItemHeader( "BuildingLocalAuthority", ColorTheme.Gray );
            NPCCohort cohort = this.GetEffectiveAuthority();
            Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "NPCCohort", cohort.ID );
            Buffer.AddRaw( cohort.GetDisplayName() ).EndLink( false, true ).Line();

            if ( cohort != null )
                cohort.DuringGame_DiscoverIfNeed();
            
            SecurityClearance clearance = this.CalculateLocationSecurityClearanceType();
            if ( clearance != null )
            {
                //Security Clearance Required
                Buffer.AddLangAndAfterLineItemHeader( "BuildingSecurityClearance", ColorTheme.Gray );
                Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "SecurityClearance", clearance.ID );
                Buffer.AddRaw( clearance.GetDisplayName() );
                Buffer.EndLink( false, true ).Line();
            }

            //MachineStructure
            MachineStructure machineStructure = this.MachineStructureInBuilding;
            MachineJob job = machineStructure?.CurrentJob;
            if ( job != null )
            {
                Buffer.AddLangAndAfterLineItemHeader( "MachineStructure", ColorTheme.Gray );
                Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "MachineStructure", job.ID );
                Buffer.AddRaw( job.GetDisplayName() );
                Buffer.EndLink( false, true );
                Buffer.Line();                   
			}

            //POI
            MapPOI poi = this.CalculateLocationPOI();
            if ( poi != null )
            {
                Buffer.AddLangAndAfterLineItemHeader( "MapPOI", ColorTheme.Gray );
                Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "POI", poi.POIID.ToString() );
                Buffer.AddRaw( poi.GetDisplayName() );
                Buffer.EndLink( false, true );
                if ( poi.ControlledBy != null )
                {
                    Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "NPCCohort", poi.ControlledBy.ID );
                    Buffer.Space1x().AddFormat1( LangCommon.Parenthetical, poi.ControlledBy.GetDisplayName() );
                    Buffer.EndLink( false, true );
                }

                Buffer.Line();
            }

            //District
            MapDistrict district = this.GetParentDistrict();
            if ( district != null )
            {
                Buffer.AddLangAndAfterLineItemHeader( "MapDistrict", ColorTheme.Gray );
                Buffer.AddRaw( district.GetDisplayName() );
                if ( district.ControlledBy != null )
                    Buffer.Space1x().AddFormat1( LangCommon.Parenthetical, district.ControlledBy.GetDisplayName() );
                Buffer.Line();
            }

            //Buffer.AddRawAndAfterLineItemHeader( "Distance From Machine Buildings", ColorTheme.Gray );
            //Buffer.AddRaw( this.roughDistanceFromMachines.ToStringThousandsDecimal() );
            //Buffer.Line();

            if ( this.DebugText.Length > 0)
            {
                Buffer.AddRawAndAfterLineItemHeader( "Debug Text", ColorTheme.Gray );
                Buffer.AddRaw( this.DebugText );
                Buffer.Line();
            }

            #region Description From POI Or Beacon
            if ( poi != null && poi.BuildingOrNull == this.Item )
            {
                Buffer.StartSize90();
                if ( poi.Type.GetDescription() != null && poi.Type.GetDescription().Length > 0 )
                    Buffer.AddRaw( poi.Type.GetDescription() ).Line();
                if ( poi.Type.StrategyTip != null && poi.Type.StrategyTip.Text.Length > 0 )
                    Buffer.AddRaw( poi.Type.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                Buffer.EndSize();
            }
            else if ( this.Variant.BeaconToShow != null )
            {
                Buffer.StartSize90();
                BeaconType beacon = this.Variant.BeaconToShow;
                if ( beacon.GetDescription() != null && beacon.GetDescription().Length > 0 )
                    Buffer.AddRaw( beacon.GetDescription() ).Line();
                if ( beacon.StrategyTip != null && beacon.StrategyTip.Text.Length > 0 )
                    Buffer.AddRaw( beacon.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                Buffer.EndSize();
            }
            #endregion

            #region Building Data
            {
                //extra spacing
                Buffer.Line().AddLangAndAfterLineItemHeader( "Building_Info", ColorTheme.HeaderGold );

                foreach ( LocationDataType dataType in LocationDataTypeTable.Instance.Rows )
                {
                    if ( this.Variant.InitialBuildingRatings.TryGetValue( dataType, out int val ) )
                    {
                        if ( val <= 0 )
                            continue;
                        Buffer.Line();

                        Buffer.AddRaw( val.ToStringLargeNumberAbbreviated() );
                        Buffer.Position40();

                        Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "BuildingData", dataType.ID );
                        Buffer.AddSpriteStyled_NoIndent( dataType.Icon, AdjustedSpriteStyle.InlineLarger1_2, dataType.IconColorHex ).Space1x();
                        Buffer.AddRaw( dataType.GetDisplayName() );
                        Buffer.EndLink( false, true );
                    }
                }
            }
            #endregion

            #region Residents
            if ( this.Prefab.NormalMaxResidents > 0 && !this.Status.ShouldBuildingBeNonfunctional )
            {
                //extra spacing
                Buffer.Line2x().AddLangAndAfterLineItemHeader( "Building_Residents", ColorTheme.HeaderGold );
                foreach ( KeyValuePair<EconomicClassType, int> kv in this.Prefab.NormalMaxResidentsByEconomicClass )
                {
                    if ( kv.Value <= 0 )
                        continue;

                    Buffer.Line();

                    Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "Econ", kv.Key.ID );
                    Buffer.AddRaw( kv.Key.GetDisplayName() );
                    Buffer.EndLink( false, true );

                    Buffer.Position120();

                    Buffer.AddLang( "PopulationStatistics_Residents", ColorTheme.CyanDim );
                    Buffer.Space1x();
					int residents = this.GetResidentAmount( kv.Key );
                    Buffer.AddRaw( residents.ToStringLargeNumberAbbreviated() );

                    Buffer.Position220();

                    Buffer.AddLang( "PopulationStatistics_Housing", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    Buffer.AddRaw( kv.Value.ToStringLargeNumberAbbreviated() );
                }
            }
            #endregion

            #region Staff
            if ( this.Prefab.NormalMaxJobs > 0 && !this.Status.ShouldBuildingBeNonfunctional )
            {
                //extra spacing
                Buffer.Line2x().AddLangAndAfterLineItemHeader( "Building_Staff", ColorTheme.HeaderGold );
                foreach ( KeyValuePair<ProfessionType, int> kv in this.Prefab.NormalMaxJobsByProfession )
                {
                    if ( kv.Value <= 0 )
                        continue;

                    Buffer.Line();

                    Buffer.StartLink( false, ColorTheme.LinkColor_FadedBlue, "Prof", kv.Key.ID );
                    Buffer.AddRaw( kv.Key.GetDisplayName() );
                    Buffer.EndLink( false, true );

                    Buffer.Position120();

                    Buffer.AddLang( "PopulationStatistics_Workers", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    int workers = this.GetWorkerAmount( kv.Key );
					Buffer.AddRaw( workers.ToStringLargeNumberAbbreviated() );

                    Buffer.Position220();

                    Buffer.AddLang( "PopulationStatistics_Jobs", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    Buffer.AddRaw( kv.Value.ToStringLargeNumberAbbreviated() );
                }
            }
            #endregion

            #region Dimensions
            if ( this.Prefab.BuildingFloors.Count > 0 )
            {
                //extra spacing
                Buffer.Line2x().AddLangAndAfterLineItemHeader( "Building_Dimensions", ColorTheme.HeaderGold );

                #region Floors And Basement Levels
                {
                    int floors = this.Prefab.BuildingFloors.Count;
                    int basements = -this.Prefab.MinFloor;

                    Buffer.Line();

                    Buffer.AddLang( "Building_Floors", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    Buffer.AddRaw( floors.ToStringLargeNumberAbbreviated() );
                    Buffer.Space8x();

                    Buffer.AddLang( "Building_BasementLevels", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    Buffer.AddRaw( basements.ToStringLargeNumberAbbreviated() );
                }
                #endregion

                #region Building Volume And Floor Area
                Buffer.Line();

                Buffer.AddLang( "Building_TotalBuildingVolume", ColorTheme.CyanDim );
                Buffer.Space1x();
                Buffer.AddRaw( this.Prefab.NormalTotalBuildingVolumeFullDimensions.ToStringLargeNumberAbbreviated() );
                Buffer.Space8x();

                Buffer.AddLang( "Building_TotalBuildingFloorArea", ColorTheme.CyanDim );
                Buffer.Space1x();
                Buffer.AddRaw( this.Prefab.NormalTotalBuildingFloorAreaFullDimensions.ToStringLargeNumberAbbreviated() );
                #endregion

                #region Storage (Only For Non-MachineStructures
                if ( this.Prefab.NormalTotalStorageVolumeFullDimensions > 0 && machineStructure == null )
                {
                    Buffer.Line2x();

                    Buffer.AddLang( "Building_TotalStorageVolume", ColorTheme.CyanDim );
                    Buffer.Space1x();
                    Buffer.AddRaw( this.Prefab.NormalTotalStorageVolumeFullDimensions.ToStringLargeNumberAbbreviated() );
                }
                #endregion
            }
            #endregion
            Buffer.Line2x();
        }

        public void WriteWorldExamineDetails_SubTooltipLinkHover( string[] TooltipLinkData )
        {
            string linkID = TooltipLinkData[0];
            switch ( linkID )
            {                
                case "BuildingData":
                    {
                        LocationDataType dataType = LocationDataTypeTable.Instance.GetRowByID( TooltipLinkData[1] );
                        dataType?.RenderBriefLocationDataTypeTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                    }
                    break;
                case "MachineStructure":
                    {
                        MachineStructure machineStructure = this.MachineStructureInBuilding;
                        machineStructure?.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard, true, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    }
                    break;
                case "SecurityClearance":
                    {
                        SecurityClearance clearance = SecurityClearanceTable.Instance.GetRowByID( TooltipLinkData[1] );
                        clearance?.RenderBriefSecurityClearanceTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                    }
                    break;
                case "Econ":
                    {
                        EconomicClassType econ = EconomicClassTypeTable.Instance.GetRowByID( TooltipLinkData[1] );
                        econ?.RenderBriefEconomicClassTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                    }
                    break;
                case "Prof":
                    {
                        ProfessionType prof = ProfessionTypeTable.Instance.GetRowByID( TooltipLinkData[1] );
                        prof?.RenderBriefProfessionTypeTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                    }
                    break;
                case "NPCCohort":
                    {
                        NPCCohort cohort = NPCCohortTable.Instance.GetRowByID( TooltipLinkData[1] );
                        cohort?.RenderNPCCohortTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                    }
                    break;
                case "POI":
                    {
                        MapPOI poi = CityMap.POIsByID[Convert.ToInt16(TooltipLinkData[1])];
                        poi?.WriteDataItemUIXTooltip( null, SideClamp.Any );
                    }
                    break;
                default:
                    break;
            }
        }

        public MouseHandlingResult WriteWorldExamineDetails_SubTooltipLinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
        {
            //string linkID = TooltipLinkData[0];
            //switch ( linkID )
            //{
            //    case "Faction":
            //        {
            //            Faction fac = World_Factions.GetFactionDirectByID( Convert.ToInt32( TooltipLinkData[1] ) );
            //            if ( fac != null )
            //            {
            //                Engine_Universal.NonModalPopupsSidePrimary.Clear(); //close the building window
            //                VisQueries.Instance.ShowDetailsForFaction( fac );
            //            }
            //        }
            //        break;
            //}
            return MouseHandlingResult.None;
        }

        public bool DataItemUIX_TryHandlePrimaryClick( out bool ShouldCloseWindow )
        {
            this.DataItemUIX_HandleAltClick( out ShouldCloseWindow );
            return true;
        }

        public void DataItemUIX_HandleAltClick( out bool ShouldCloseWindow )
        {
            if ( this.Item != null ) //move to the spot of this building!
            {
                ShouldCloseWindow = true;
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( this.Item.CenterPoint, false );

                //then after moving there, also highlight this building:
                Engine_Universal.NonModalPopupsSidePrimary.Clear(); //keep these from stacking up on repeat clicks
                ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.TallSidePrimary, null, LocalizedString.AddRaw_New( this.GetDisplayName() ),
                LangCommon.Popup_Common_Close.LocalizedString, this.Item, delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
                {
                    if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                        this.WriteWorldExamineDetails_SubTooltipLinkHover( TooltipLinkData );
                    else
                    {
                        this.WriteWorldExamineDetails( Buffer );
                    }
                }, 1f,
                delegate ( MouseHandlingInput Input, string[] TooltipLinkData )
                {
                    return this.WriteWorldExamineDetails_SubTooltipLinkClick( Input, TooltipLinkData );
                } );
            }
            else
                ShouldCloseWindow = false;
        }

        #region GetIsCurrentlyInvisible
        public bool GetIsCurrentlyInvisible( InvisibilityPurpose ForPurpose )
        {
            switch ( ForPurpose )
            {
                case InvisibilityPurpose.ForPlayerTargeting:
                case InvisibilityPurpose.ForNPCTargeting:
                    return this.Status?.ShouldBuildingBeInvisible ?? false;
                case InvisibilityPurpose.ForCameraFocus:
                default:
                    return false;
            }
        }
        #endregion

        public int GetUniqueLocationID()
        {
            return this.BuildingID;
        }

        public bool GetIsLocationStillValid()
        {
            if ( this.IsInPoolAtAll )
                return false;

            BuildingStatus status = this.Status;
            if ( status == null || status.ShouldBuildingBeNonfunctional )
                return false;

            return true;
        }
        #endregion
    }
}

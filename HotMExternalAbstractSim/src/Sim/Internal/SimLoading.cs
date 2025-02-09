using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;


namespace Arcen.HotM.External
{
    /// <summary>
    /// Helper methods for actually loading the sim
    /// </summary>
    internal static class SimLoading
    {
        //
        //All important variables should be above this point, so we can be sure to clear them.
        //-----------------------------------------------------------------

        #region OnGameClear
        public static void OnGameClear()
        {
        }
        #endregion

        #region RunGameStartOnBGThread
        public static void RunGameStartOnBGThread( MersenneTwister RandForGameStart )
        {
            while ( CityMap.TilesBeingPostProcessed_Structural > 0 || CityMap.TilesToPostProcess_Structural.Count > 0 )
            {
                System.Threading.Thread.Sleep( 200 ); //sleep us until these are ready, with extra buffer!
            }

            //while ( CityMap.TilesBeingPostProcessed_Decorations > 0 || CityMap.TilesToPostProcess_Decoration.Count > 0 )
            //{
            //    System.Threading.Thread.Sleep( 10 ); //sleep us until these are ready, but less!
            //}

            if ( SimCommon.HasInitiatedStart )
                return;
            SimCommon.HasInitiatedStart = true;

            Stopwatch sw = Stopwatch.StartNew();

            //ArcenDebugging.LogSingleLine("Running BG game start", Verbosity.DoNotShow );

            PopulateSimDistrictsFromMapDistricts( RandForGameStart );
            CalculateDistrictAdjacency();
            PopulateAnyBlankPOINames( RandForGameStart ); //must be done after the district names are in!
            World_Buildings.FillAnyMissingSimBuildings_FromSimThread( RandForGameStart );
            PopulateDistrictAndPOIControllers( RandForGameStart );

            //ArcenDebugging.LogSingleLine( "Finished RunGameStartOnBGThread P1 in " + sw.ElapsedMilliseconds.ToStringThousandsWhole() +
            //    "ms.", Verbosity.DoNotShow );

            InitializePopulationAndDiffusionIfNeeded( RandForGameStart );

            //ArcenDebugging.LogSingleLine( "Finished RunGameStartOnBGThread P2 in " + sw.ElapsedMilliseconds.ToStringThousandsWhole() +
            //    "ms.", Verbosity.DoNotShow );

            //ArcenDebugging.LogSingleLine( "Unassigned buildings: " + workingMapItems.Count, Verbosity.DoNotShow );

            OnGameStart_FromBGThread( RandForGameStart );

            ArcenDebugging.LogSingleLine( "Finished RunGameStartOnBGThread in " + sw.ElapsedMilliseconds.ToStringThousandsWhole() +
                "ms.  We have " + World_Buildings.BuildingsByID.Count + " total items.", Verbosity.DoNotShow );
        }
        #endregion

        #region OnGameStart_FromBGThread
        /// <summary>
        /// This is a callback from NOT the main sim thread, instead being on the background thread of it calculating the spatial grid logic.
        /// If this is called during a time when the sim thread is running separately, expect some deadlocks from UpdateSimBuildings being called twice
        /// </summary>
        private static void OnGameStart_FromBGThread( MersenneTwister RandForGameStart )
        {
            if ( Engine_Universal.CalculateIsCurrentThreadMainThread() )
            {
                ArcenDebugging.LogWithStack( "Called OnGameStart_FromBGThread from the main thread; that's super slow and will be problematic!", Verbosity.ShowAsError );
                return;
            }

            bool wasLoadedFromSavegame = CityMap.WasMapLoadedFromSavegame;

            World_Buildings.DoAnyPerSecondLogic_SimThread( true );

            // ArcenDebugging.LogSingleLine("Doing game initialization! We have " + ActionPrefabDataTable.Instance.Rows.Length + " action types", Verbosity.DoNotShow );
            // for ( int i = 0; i < ActionPrefabDataTable.Instance.Rows.Length; i++ )
            // {
            //     ActionPrefab actType = ActionPrefabDataTable.Instance.Rows[i];
            //     ArcenDebugging.LogSingleLine("\t" + actType.ToDebugString(), Verbosity.DoNotShow );
            // }

            SimCommon.HasFullyStarted = true;
            //ArcenDebugging.LogSingleLine( "has fully started", Verbosity.DoNotShow );

            CentralVars.HandleCrossovers();
        }
        #endregion

        #region PopulateSimDistrictsFromMapDistricts
        private static void PopulateSimDistrictsFromMapDistricts( MersenneTwister Rand )
        {
            foreach ( MapDistrict district in CityMap.AllDistricts )
            {
                if ( district.DistrictName == null || district.DistrictName.Length <= 0 || district.DistrictName.Contains( "[too few" ) )
                {
                    district.DistrictName = GenerateDistrictNameFromParts( district.Type, Rand, CityMap.AllDistricts );
                }
            }
        }
        #endregion

        #region PopulateDistrictAndPOIControllers
        private static void PopulateDistrictAndPOIControllers( MersenneTwister Rand )
        {
            foreach ( MapDistrict district in CityMap.AllDistricts )
            {
                if ( district.ControlledBy == null )
                    district.ControlledBy = district.Type.DistrictOwnerChosenFrom.CohortsList.GetRandom( Rand );

                if ( district.HasBeenDiscovered )
                {
                    if ( district.ControlledBy != null )
                        district.ControlledBy.DuringGame_DiscoverIfNeed();
                }
            }

            foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
            {
                if ( kv.Value == null )
                    continue;
                MapPOI poi = kv.Value;
                if ( poi.Type.CopyOwnerFromDistrict )
                    poi.ControlledBy = poi.Tile?.District?.ControlledBy;
                else
                {
                    if ( poi.ControlledBy == null )
                        poi.ControlledBy = poi.Type.POIOwnerChosenFrom.CohortsList.GetRandom( Rand );
                }

                if ( poi.HasBeenDiscovered )
                {
                    if ( poi.ControlledBy != null )
                        poi.ControlledBy.DuringGame_DiscoverIfNeed();
                }
            }
        }
        #endregion

        #region PopulateAnyBlankPOINames
        private static void PopulateAnyBlankPOINames( MersenneTwister Rand )
        {
            int count = 0;
            foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
            {
                MapPOI poi = kv.Value;
                if ( poi.HasBeenDestroyed )
                    continue; //ignore it
                if ( poi.BuildingOrNull != null && (poi.BuildingOrNull?.SimBuilding?.GetIsDestroyed() ?? true) )
                    continue;

                if ( poi.Type.CopyNameFromDistrict )
                {
                    poi.POIName = poi.Tile.District.DistrictName;
                    continue;
                }
                if ( !poi.POIName.IsEmpty() && !poi.POIName.Contains( "[too few" ) )
                    continue; //if we already have a name, then don't worry about it!

                poi.POIName = GeneratePOINameFromParts( poi.Type,
                    Rand, CityMap.POIsByID );
                count++;
            }

            if ( count > 0 )
                ArcenDebugging.LogSingleLine( "Generated names for " + count + " pois with missing names.", Verbosity.DoNotShow );
        }
        #endregion

        #region CalculateDistrictAdjacency
        private static void CalculateDistrictAdjacency()
        {
            //loop over all the cells, and check their north and east sides
            //we won't check south and west simply because that would be duplicative
            foreach ( MapCell cell in CityMap.Cells )
            {
                MapDistrict myDistrict = cell.ParentTile.District;
                MapDistrict districtToNorth = cell.CellNorth?.ParentTile?.District;
                MapDistrict districtToEast = cell.CellNorth?.ParentTile?.District;

                if ( myDistrict != districtToNorth && districtToNorth != null )
                {
                    //two neighbors, hooray!
                    myDistrict.AdjacentDistricts[districtToNorth]++;
                    districtToNorth.AdjacentDistricts[myDistrict]++;
                }

                if ( myDistrict != districtToEast && districtToEast != null )
                {
                    //two neighbors, hooray!
                    myDistrict.AdjacentDistricts[districtToEast]++;
                    districtToEast.AdjacentDistricts[myDistrict]++;
                }
            }
        }
        #endregion

        #region GenerateDistrictNameFromParts
        public static string GenerateDistrictNameFromParts( DistrictType Type, MersenneTwister Rand, ProtectedList<MapDistrict> OtherDistrictsToCompareAgainst )
        {
            workingNameOptions.Clear();
            string sourceName = "null source";
            if ( Type.UseNamePool )
            {
                sourceName = "name pool '" + Type.NamePool.ID + "'";
                foreach ( Name name in Type.NamePool.NamesList )
                    workingNameOptions.Add( name.GetDisplayName() );
            }
            else if ( Type.UseNumberedNameStyle )
            {
                sourceName = "numbered style '" + Type.MinNameNumber + " to " + Type.MaxNameNumber + "'";
                for ( int i = Type.MinNameNumber; i <= Type.MaxNameNumber; i++ )
                    workingNameOptions.Add( i.ToString() );
            }

            string newNamePart = Helper_GetDistrictNamePartFromList_MakeSureNoDuplicates( Type, Rand, OtherDistrictsToCompareAgainst, sourceName );
            if ( Type.UseNumberedNameStyle )
                return Lang.Format1( Type.LangKeyForNumberedName, newNamePart );
            return newNamePart;
        }

        private static string Helper_GetDistrictNamePartFromList_MakeSureNoDuplicates( DistrictType Type, MersenneTwister Rand,
            ProtectedList<MapDistrict> OtherDistrictsToCompareAgainst, string SourceName )
        {
            int originalOptionCount = workingNameOptions.Count;
            int maxOptionsChecked = 0;

            string newNamePart = string.Empty;
            for ( int i = 0; i < 400 && workingNameOptions.Count > 0; i++ )
            {
                newNamePart = workingNameOptions.GetRandomAndRemove( Rand );
                if ( OtherDistrictsToCompareAgainst != null )
                {
                    int optionsChecked = 0;
                    bool foundFailure = false;
                    foreach ( MapDistrict dist in OtherDistrictsToCompareAgainst )
                    {
                        if ( dist.Type != Type || dist.DistrictName.IsEmpty() )
                            continue;
                        optionsChecked++;

                        if ( dist.DistrictName.Contains( newNamePart, StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            foundFailure = true;
                            break; //this is considered invalid, skip it
                        }
                    }

                    if ( !foundFailure )
                        return newNamePart;

                    if ( optionsChecked > maxOptionsChecked )
                        maxOptionsChecked = optionsChecked;
                }
                else
                    return newNamePart;
            }

            ArcenDebugging.LogSingleLine( "Too few of name parts for district type: '" + Type.ID + "' for all the district from source " + SourceName +
                ".  Original options: " + originalOptionCount + ".  Max district checked: " + maxOptionsChecked + ".  Remaining options: " + workingNameOptions.Count, Verbosity.ShowAsError );
            return "[too few name parts]";
        }
        #endregion

        private static readonly List<string> workingNameOptions = List<string>.Create_WillNeverBeGCed( 100, "SimLoading-workingNameOptions" );

        #region GeneratePOINameFromParts
        private static string GeneratePOINameFromParts( POIType Type,
            MersenneTwister Rand, Dictionary<Int16, MapPOI> OtherPOIsToCompareAgainst )
        {
            workingNameOptions.Clear();
            string sourceName = "null source";
            if ( Type.UseNamePool )
            {
                sourceName = "name pool '" + Type.NamePool.ID + "'";
                foreach ( Name name in Type.NamePool.NamesList )
                    workingNameOptions.Add( name.GetDisplayName() );
            }
            else if ( Type.UseNumberedNameStyle )
            {
                sourceName = "numbered style '" + Type.MinNameNumber + " to " + Type.MaxNameNumber + "'";
                for ( int i = Type.MinNameNumber; i <= Type.MaxNameNumber; i++)
                    workingNameOptions.Add( i.ToString() );
            }

            string newNamePart = Helper_GetPOINamePartFromList_MakeSureNoDuplicates( Type, Rand, OtherPOIsToCompareAgainst, sourceName );
            if ( Type.UseNumberedNameStyle )
                return Lang.Format1( Type.LangKeyForNumberedName, newNamePart );
            return newNamePart;
        }

        private static string Helper_GetPOINamePartFromList_MakeSureNoDuplicates( POIType Type, MersenneTwister Rand,
            Dictionary<Int16, MapPOI> OtherPOIsToCompareAgainst, string SourceName )
        {
            int originalOptionCount = workingNameOptions.Count;
            int maxOptionsChecked = 0;

            string newNamePart = string.Empty;
            for ( int i = 0; i < 400 && workingNameOptions.Count > 0; i++ )
            {
                newNamePart = workingNameOptions.GetRandomAndRemove( Rand );
                if ( OtherPOIsToCompareAgainst != null )
                {
                    int optionsChecked = 0;
                    bool foundFailure = false;
                    foreach ( KeyValuePair<Int16, MapPOI> kv in OtherPOIsToCompareAgainst )
                    {
                        if ( Type.NamePool != null )
                        {
                            if ( kv.Value.Type.NamePool != Type.NamePool || kv.Value.POIName.IsEmpty() )
                                continue;
                        }
                        else
                        {

                            if ( kv.Value.Type != Type || kv.Value.POIName.IsEmpty() )
                                continue;
                        }
                        optionsChecked++;

                        if ( kv.Value.POIName.Contains( newNamePart, StringComparison.InvariantCultureIgnoreCase ) )
                        {
                            foundFailure = true;
                            break; //this is considered invalid, skip it
                        }
                    }

                    if ( !foundFailure )
                        return newNamePart;

                    if ( optionsChecked > maxOptionsChecked )
                        maxOptionsChecked = optionsChecked;
                }
                else
                    return newNamePart;
            }

            if ( Type.AddRandomDigitsAtStartOfNameFromPoolIfTooFew > 0 && Type.NamePool != null && Type.NamePool.NamesList.Count > 0 )
            {
                string finalName = Type.NamePool.NamesList.GetRandom( Rand ).GetDisplayName();
                for ( int i = 0; i < Type.AddRandomDigitsAtStartOfNameFromPoolIfTooFew; i++ )
                    finalName = Rand.NextInclus( 1, 9 ) + finalName;
                return finalName;
            }

            ArcenDebugging.LogSingleLine( "Too few of name parts for poi type: '" + Type.ID + "' for all the POIs from source " + SourceName + 
                ".  Original options: " + originalOptionCount + ".  Max pois checked: " + maxOptionsChecked + ".  Remaining options: " + workingNameOptions.Count, Verbosity.ShowAsError );
            return "[too few name parts]";
        }
        #endregion

        #region InitializePopulationIfNeeded
        private static void InitializePopulationAndDiffusionIfNeeded( MersenneTwister RandForGameStart )
        {
            if ( CityMap.WasMapLoadedFromSavegame )
                return;

            if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Initialize.PopulationAndDiffusion",
            ( TaskStartData startData ) =>
            {
                ArcenDebugging.LogSingleLine( "InitializePopulation WAS needed, " + World_Buildings.BuildingsByID.Count + " buildings to handle.", Verbosity.DoNotShow );

                foreach ( KeyValuePair<int, SimBuilding> kv in World_Buildings.BuildingsByID )
                {
                    SimBuilding building = kv.Value;
                    BuildingPrefab prefab = building.Prefab;

                    if ( prefab.NormalMaxResidents > 0 )
                    {
                        Dictionary<EconomicClassType, int> maxResidents = building.Prefab.NormalMaxResidentsByEconomicClass;

                        foreach ( KeyValuePair<EconomicClassType, int> pair in maxResidents )
                        {
                            int addedAmount = Mathf.RoundToInt( pair.Value * RandForGameStart.NextFloat( pair.Key.StartingMinMax.x, pair.Key.StartingMinMax.y ) );
                            if ( addedAmount < 1 )
                                addedAmount = 1;
                            building.Residents.Set_BeVeryCarefulOfUsing( pair.Key, addedAmount );
                        }
                    }
                }

            } ) )
            { //this is part of the "if not RunTaskOnBackgroundThread" statement, above
                ArcenDebugging.LogSingleLine( "Could not start _Sim.InitializePopulationAndDiffusion!", Verbosity.ShowAsError );
            }            
        }
        #endregion
    }
}

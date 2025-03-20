using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.ExternalVis
{
    public static class VisCentralData
    {
        private static readonly List<MapCell> workingCells1 = List<MapCell>.Create_WillNeverBeGCed( 400, "VisCentralData-workingCells1" );
        private static readonly List<MapCell> workingCells2 = List<MapCell>.Create_WillNeverBeGCed( 400, "VisCentralData-workingCells2" );
        private static readonly List<MapCell> workingCells3= List<MapCell>.Create_WillNeverBeGCed( 400, "VisCentralData-workingCells3" );
        private static readonly List<MapCell> workingCells4 = List<MapCell>.Create_WillNeverBeGCed( 400, "VisCentralData-workingCells4" );
        public static bool IsWaitingToStartSimOnMapgen = false;
        public static float LastInitialCameraPositionBeenSet = -1;

        public static void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            CityMap.ChosenStartingCell = null;

            IsWaitingToStartSimOnMapgen = false;
            LastInitialCameraPositionBeenSet = -1;
            workingCells1.Clear();
            workingCells2.Clear();
            workingCells3.Clear();
            workingCells4.Clear();

            VisibilityGranterCalculator.OnGameClear();
            NPCTargetingCalculator.OnGameClear();
        }

        public static void DoPerFrame()
        {
            if ( IsWaitingToStartSimOnMapgen )
                TryHandlePostMapgenLogic();
        }

        #region TryHandlePostMapgenLogic
        private static void TryHandlePostMapgenLogic()
        {
            if ( !IsWaitingToStartSimOnMapgen )
                return; //don't need to do this after all
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings || CityMap.Tiles.Count <= 5 || !SimCommon.HasFullyStarted )
                return; //not ready yet
            if ( SimCommon.NeedsBuildingListRecalculation || CityBuildingListCalculator.GetIsCalculatingNow() )
                return; //not ready yet

            //ArcenDebugging.LogSingleLine( "Hey community sim start", Verbosity.DoNotShow );

            //okay, ready!
            IsWaitingToStartSimOnMapgen = false;

            if ( CityMap.ChosenStartingCell == null ) //only choose a new starting tile and such if one was not already extant
            {
                workingCells1.Clear();
                workingCells2.Clear();
                workingCells3.Clear();
                workingCells4.Clear();

                int minX = 0;
                int minZ = 0;
                int maxX = 0;
                int maxZ = 0;

                bool isPastPrologue = SimMetagame.AllTimelines.Count > 1; //if not our first timeline, then we are definitely past the prologue
                if ( !isPastPrologue )
                {
                    if ( (SimMetagame.StartType?.SkipToChapter ?? 0) > 0 )
                        isPastPrologue = true;
                }

                foreach ( MapCell cell in CityMap.Cells )
                {
                    if ( cell == null || cell.ParentTile == null || cell.ParentTile.SeedingLogic == null )
                        continue;
                    if ( cell.ParentTile.IsOutOfBoundsTile )
                        continue;

                    ArcenGroundPoint loc = cell.rawCellLocation;
                    if ( loc.X < minX )
                        minX = loc.X;
                    if ( loc.X > maxX ) 
                        maxX = loc.X;
                    if ( loc.Z < minZ )
                        minZ = loc.Z;
                    if ( loc.Z > maxZ )
                        maxZ = loc.Z;

                    if ( cell.ParentTile.SeedingLogic.IsValidPlayerStartingLocation )
                    {
                        bool hasAtLeastOneStartingValidBuild = false;
                        foreach ( MapItem building in cell.BuildingList.GetDisplayList() )
                        {
                            if ( isPastPrologue )
                            {
                                if ( building?.SimBuilding?.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_FirstTower ?? false )
                                {
                                    if ( building.CalculateLocationSecurityClearanceInt() <= 0 )
                                    {
                                        hasAtLeastOneStartingValidBuild = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if ( building?.SimBuilding?.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_Prologue ?? false )
                                {
                                    if ( building.CalculateLocationSecurityClearanceInt() <= 0 )
                                    {
                                        hasAtLeastOneStartingValidBuild = true;
                                        break;
                                    }
                                }
                            }
                        }
                        //can't start here because none of the initial buildings are valid starting spots
                        if ( !hasAtLeastOneStartingValidBuild )
                            continue;

                        workingCells3.Add( cell );
                        workingCells4.Add( cell );

                        if ( cell.BuildingList.GetDisplayList().Count < 30 ) //magic number, yeah.  If there are too few buildings, don't start us there
                            continue;

                        if ( !cell.CalculateIsValidStartingCellForTower() )
                            continue;

                        workingCells1.Add( cell );
                    }
                    else
                    {
                        bool hasAtLeastOneStartingValidBuild = false;
                        foreach ( MapItem building in cell.BuildingList.GetDisplayList() )
                        {
                            if ( isPastPrologue )
                            {
                                if ( building?.SimBuilding?.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_FirstTower ?? false )
                                {
                                    if ( building.CalculateLocationSecurityClearanceInt() <= 0 )
                                    {
                                        hasAtLeastOneStartingValidBuild = true;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if ( building?.SimBuilding?.GetPrefab()?.PlaceableRoot?.ExtraPlaceableData?.IsValidStartingSpotForPlayers_Prologue ?? false )
                                {
                                    if ( building.CalculateLocationSecurityClearanceInt() <= 0 )
                                    {
                                        hasAtLeastOneStartingValidBuild = true;
                                        break;
                                    }
                                }
                            }
                        }
                        //can't start here because none of the initial buildings are valid starting spots
                        if ( !hasAtLeastOneStartingValidBuild )
                            continue;

                        workingCells4.Add( cell );
                    }
                }

                minX += 2;
                minZ += 2;

                maxX -= 2;
                maxZ -= 2;

                workingCells2.Clear();
                List<MapCell> workingCellsSource = workingCells1.Count > 0 ? workingCells1 : workingCells3;
                workingCellsSource = workingCellsSource.Count > 0 ? workingCellsSource : workingCells4;
                for ( int i = workingCellsSource.Count - 1; i >= 0; i-- )
                {
                    ArcenGroundPoint loc = workingCellsSource[i].rawCellLocation;
                    if ( loc.X <= minX || loc.X >= maxX ||
                        loc.Z <= minZ || loc.Z >= maxZ )
                    { }
                    else
                        workingCells2.Add( workingCellsSource[i] );
                }

                List<MapCell> workingCells = workingCells2.Count > 0 ? workingCells2 : workingCellsSource;

                if ( workingCells.Count == 0 )
                    ArcenDebugging.LogSingleLine( "Could not find any cells that are a valid player starting location!", Verbosity.ShowAsError );
                else
                {
                    CityMap.ChosenStartingCell = workingCells.GetRandom( Engine_Universal.PermanentQualityRandom );
                    //move the player to looking at this
                    VisManagerVeryBase.Instance.MainCamera_ResetVisualInfo();
                    VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( CityMap.ChosenStartingCell.Center, true );

                    //the starting cell lights up, and we get a starting unit there
                    CityMap.ChosenStartingCell.IsStartingCell = true;
                }
            }

            LastInitialCameraPositionBeenSet = ArcenTime.AnyTimeSinceStartF;
        }
        #endregion

        #region Serialization
        public static void SerializeData( ArcenFileSerializer Serializer )
        {
            if ( CityMap.ChosenStartingCell != null )
                Serializer.AddInt16( "ChosenStartingCell", CityMap.ChosenStartingCell.CellID );
        }

        public static void DeserializeData( DeserializedObjectLayer Data )
        {
            if ( Data.TryGetInt16( "ChosenStartingCell", out Int16 chosenStartingCell ) )
            {
                CityMap.ChosenStartingCell = CityMap.TryGetCellByID( chosenStartingCell );
                CityMap.ChosenStartingCell.IsStartingCell = true;
            }
        }
        #endregion

        #region ClearAllObjectsBecauseOfUnload
        public static void ClearAllObjectsBecauseOfUnload()
        {
            ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
        }
        #endregion
    }
}

using System;



using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public static class RenderManager_CityMap
    {
        internal const float MAP_CELL_SPACING = 0.5f;
        internal static readonly Vector3 MAP_GROUND_CUBE_SCALE = new Vector3( 20 - MAP_CELL_SPACING, 2f, 20 - MAP_CELL_SPACING );
        internal const float MAP_GROUND_DRAW_Y = 0f;
        internal static VisSimpleDrawingObject mapModeCube = null;
        private static bool hasEnabledIndicator = false;

        #region PreRenderFrame
        public static void PreRenderFrame()
        {
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
            {
                return;
            }

            if ( mapModeCube == null )
            {
                mapModeCube = VisSimpleDrawingObjectTable.Instance.GetRowByID( "MapModeCube" );
                if ( mapModeCube == null )
                    return;
            }

            SharedRenderManagerData.stopwatch.Restart();


            int debugStage = 0;
            try
            {
                debugStage = 1000;
                //BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();

                Color myMarkerColor = ColorRefs.MapMyMarker.ColorWithoutHDR;
                Color exploredNoVisionColor = ColorRefs.MapExploredNoVisionCell.ColorWithoutHDR;
                Color exploredYesVisionColor = ColorRefs.MapExploredAndVisionCell.ColorWithoutHDR;
                Color unexploredColor = ColorRefs.MapUnexploredCell.ColorWithoutHDR;
                Color outOfBoundsColor = ColorRefs.MapOutOfBoundsCell.ColorWithoutHDR;
                Color irradiatedColor = ColorRefs.MapIrradiatedColorCell.ColorWithoutHDR;
                Int64 framesPrepped = RenderManager.FramesPrepped;

                debugStage = 4000;

                RenderManager.GetMapGroundMaterial().color = ColorRefs.MapBackgroundColor.ColorWithoutHDR;


                bool drawJobs = (SimCommon.CurrentCityLens?.ShowJobs?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowJobs ?? false);
                bool drawSwarms = (SimCommon.CurrentCityLens?.ShowSwarms?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowSwarms ?? false);
                bool drawContemplations = (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false);
                bool drawExplorationSites = (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false);
                bool drawCityConflictsLarge = (SimCommon.CurrentCityLens?.ShowCityConflictsLarge?.Display ?? false);
                bool drawCityConflictsSmall = (SimCommon.CurrentCityLens?.ShowCityConflictsSmall?.Display ?? false);
                bool drawSpecialtyResources = (SimCommon.CurrentCityLens?.ShowSpecialResources?.Display ?? false) || (Engine_HotM.SelectedActorAbilityTargetingMode?.ShowsSpecialtyResources ?? false);
                bool drawAllBeacons = (SimCommon.CurrentCityLens?.ShowAllBeacons?.Display ?? false);
                bool drawKeyBeacons = (SimCommon.CurrentCityLens?.ShowKeyBeacons?.Display ?? false);
                bool drawAllPOIs = (SimCommon.CurrentCityLens?.ShowAllPOIs?.Display ?? false);
                bool drawAllPOIStatuses = (SimCommon.CurrentCityLens?.ShowAllPOIStatuses?.Display ?? false);
                bool drawKeyPOIStatuses = (SimCommon.CurrentCityLens?.ShowKeyPOIStatuses?.Display ?? false);
                bool drawPOIShapes = (SimCommon.CurrentCityLens?.ShowPOIShapes?.Display ?? false);
                bool showAllNPCUnits = (SimCommon.CurrentCityLens?.ShowAllNPCUnits?.Display ?? false);
                bool showKeyNPCUnits = (SimCommon.CurrentCityLens?.ShowKeyNPCUnits?.Display ?? false);
                bool showHostileUnits = (SimCommon.CurrentCityLens?.ShowHostileUnits?.Display ?? false);
                bool skipPassiveGuards = (SimCommon.CurrentCityLens?.SkipPassiveGuardsOnMap?.Display ?? false);
                bool skipEconomicUnits = (SimCommon.CurrentCityLens?.SkipEconomicUnits?.Display ?? false);
                bool showAllStreetSense = (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false);
                bool showProjectRelatedStreetSense = (SimCommon.CurrentCityLens?.ShowProjectRelatedStreetSense?.Display ?? false);

                if ( InputCaching.IsInInspectMode_FocusOnLens && ( SimCommon.CurrentCityLens?.HidesKeyNPCUnitsDuringInspectFocus??false) )
                {
                    showAllNPCUnits = false;
                    showKeyNPCUnits = false;
                    showHostileUnits = false;
                    skipPassiveGuards = false;
                    skipEconomicUnits = false;
                }

                ISimMachineActor selectedMachineActorOrMull = Engine_HotM.SelectedActor as ISimMachineActor;

                debugStage = 8000;

                //bool doGroundVisMode = false;
                //if ( Engine_HotM.IsInVisualizationMode && effectiveOverlayType != null && effectiveOverlayType.Implementation != null )
                //{
                //    defaultColor = effectiveOverlayType.Implementation.GetDefaultInCurrentOverlay( effectiveOverlayType );
                //    MainGameCoreGameLoop mainLoop = Engine_HotM.Instance.GameLoop as MainGameCoreGameLoop;
                //    if ( mainLoop && mainLoop.GroundPlaneColorVisualizationMat )
                //    {
                //        mainLoop.GroundPlaneColorVisualizationMat.color = defaultColor;
                //        doGroundVisMode = true;
                //    }
                //}

                debugStage = 21000;
                if ( CameraControllerBase_Streets.Instance != null && CameraControllerBase_Streets.Instance.LastUsedPosition.x != float.NegativeInfinity )
                {
                    debugStage = 22000;
                    if ( !hasEnabledIndicator && VisCurrent.MapModeCameraIndicator )
                    {
                        hasEnabledIndicator = true;
                        VisCurrent.MapModeCameraIndicator.SetActive( true );
                        if ( VisCurrent.MapModeCameraIndicatorTransform )
                            VisCurrent.MapModeCameraIndicatorTransform.localScale = new Vector3( 10, 10, 10 );
                    }

                    debugStage = 23000;
                    if ( VisCurrent.MapModeCameraIndicatorTransform )
                    {
                        VisCurrent.MapModeCameraIndicatorTransform.position = CameraControllerBase_Streets.Instance.LastUsedPosition.ReplaceY( 1.02f );
                        VisCurrent.MapModeCameraIndicatorTransform.rotation = Quaternion.Euler( 0, CameraCurrent.LastStreetCameraYAngle, 0 );
                    }
                }
                else
                {
                    debugStage = 31000;
                    if ( hasEnabledIndicator && VisCurrent.MapModeCameraIndicator )
                    {
                        hasEnabledIndicator = false;
                        VisCurrent.MapModeCameraIndicator.SetActive( false );
                    }
                }

                debugStage = 41000;
                Investigation currentInvestigation = SimCommon.GetEffectiveCurrentInvestigation();
                if ( currentInvestigation == null )
                    currentInvestigation = BuildModeHandler.TargetingTerritoryControlInvestigation;

                debugStage = 51000;
                if ( currentInvestigation != null )
                {
                    debugStage = 51200;
                    //drawJobs = false;
                    //drawSwarms = false;
                    //drawAllBeacons = false;
                    ////drawKeyBeacons = false;
                    //drawAllPOIs = false;
                    ////drawPOIShapes = false;
                    //renderDistrictBorders = false;
                    //showPOIsThatAreSingleTile = false;
                    //showAllNPCUnits = false;
                    ////showKeyNPCUnits = false;

                    #region Investigation Highlight Drawing For All Cells
                    bool allCellRings = currentInvestigation.Type.Style.ShowRingsAroundEveryCell;
                    bool isOnFinalBuildingsOfInvestigation = currentInvestigation.Type.Style.ShowAllResultsAsBeaconStyle || currentInvestigation.PossibleBuildings.Count < 4;
                    PulsingBeaconModelData beaconData = isOnFinalBuildingsOfInvestigation ?
                        PulsingBeaconModelData.CreateFromIntermittentDouble( 0.2f, 0.02f, 0.9f ) :
                        (currentInvestigation.PossibleBuildings.Count < 15 || currentInvestigation.CountByDistrict.Count == 1 ?
                        PulsingBeaconModelData.CreateFromIntermittentDouble( 0.1f, 0.7f, 0.9f ) : PulsingBeaconModelData.CreateFromIntermittentDouble( 0.01f, 0.9f, 1f ));

                    VisColorUsage investigationBeacon = currentInvestigation.Type.Style.IsTerritoryControlStyle ? ColorRefs.BuildingPartOfTerritoryControl : ColorRefs.BuildingPartOfInvestigation;
                    VisColorUsage buildingColor = currentInvestigation.Type.Style.IsTerritoryControlStyle ? ColorRefs.BuildingPartOfTerritoryControlMapMode : ColorRefs.BuildingPartOfInvestigationMapMode;

                    foreach ( KeyValuePair<ISimBuilding, bool> kv in currentInvestigation.PossibleBuildings )
                    {
                        ISimBuilding building = kv.Key;
                        if ( building == null )
                            continue;
                        MapItem item = building.GetMapItem();
                        if ( item == null )
                            continue;
                        if ( !item.ParentCell.IsConsideredInCameraView )
                            continue;

                        RenderManager_Streets.DrawMapItemHighlightedBorderPulsingBeacon( item, buildingColor.ColorHDR, true, HighlightPass.First, beaconData, framesPrepped );
                        FrameBufferManagerData.BuildingOverlayCount.Construction++;

                        if ( allCellRings )
                            SharedRenderManagerData.DrawBeaconRingIfFirstInCell( item, investigationBeacon );
                        else if ( isOnFinalBuildingsOfInvestigation )
                            SharedRenderManagerData.DrawBeaconRingOrAddToAverage( item, currentInvestigation.PossibleBuildings.Count, investigationBeacon );
                    }

                    SharedRenderManagerData.DrawAllExistingBeaconRings( investigationBeacon );
                    #endregion
                }

                debugStage = 81000;
                //if ( hasBoxToDraw )
                //{
                //    List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
                //    shapes.Clear();
                //    shapes.Add( priorBox );
                //}
                //List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
                //shapes.Clear();

                debugStage = 121000;
                foreach ( MapTile tile in CityMap.Tiles )
                {
                    #region Draw Any Multi Cell Tiles
                    if ( tile.CellsList.Count > 1 && ( !tile.HasAnyIrradiatedCells || tile.HasOnlyIrradiatedCells ) )
                    {
                        //hooray, they all match, so we can draw this as a whole tile
                        bool isTileVeryDark = !tile.HasEverBeenExplored && tile.TileNetworkLevel.Display != TileNetLevel.Full;
                        bool isTileMidDark = tile.TileNetworkLevel.Display != TileNetLevel.Full;// !tile.IsTileContainingMachineActors.Display;
                        bool isTileSuperDark = tile.IsOutOfBoundsTile;
                        bool isTileIrradiated = tile.HasOnlyIrradiatedCells;

                        A5RendererGroup rGroup = mapModeCube.RendererGroup as A5RendererGroup;
                        Vector3 cellDraw = tile.Center;
                        float x = tile.HalfSizeX;
                        float z = tile.HalfSizeZ;
                        x += x;
                        z += z;
                        x -= MAP_CELL_SPACING;
                        z -= MAP_CELL_SPACING;

                        //isTileVeryDark = false;
                        //isTileMidDark = false;
                        //isTileSuperDark = false;

                        //switch (tile.TileNetworkLevel.Display)
                        //{
                        //    case TileNetLevel.None:
                        //        isTileSuperDark = true;
                        //        break;
                        //    case TileNetLevel.Preliminary:
                        //        isTileVeryDark = true;
                        //        break;
                        //    case TileNetLevel.Full:
                        //        isTileVeryDark = false;
                        //        isTileMidDark = false;
                        //        break;
                        //}

                        cellDraw.y = MAP_GROUND_DRAW_Y;
                        rGroup.WriteToDrawBufferForOneFrame_BasicColor( cellDraw, Quaternion.identity, new Vector3( x, MAP_GROUND_CUBE_SCALE.y, z ), RenderColorStyle.SelfColor,
                            isTileSuperDark ? outOfBoundsColor :
                            (isTileIrradiated ? irradiatedColor :
                            ( isTileVeryDark ? unexploredColor : (isTileMidDark ? exploredNoVisionColor : exploredYesVisionColor)) ),
                            RenderOpacity.Normal, false );
                    }
                    #endregion
                }

                debugStage = 221000;
                //CityMap.DoForMapCells( delegate ( MapCell cell )
                IReadOnlyList<MapCell> visibleCells = CityMap.CellsInCameraView;
                foreach ( MapCell cell in visibleCells )
                {
                    debugStage = 231000;
                    MapTile tile = cell.ParentTile;

                    bool isThisCellVeryDark = !cell.ParentTile.HasEverBeenExplored && tile.TileNetworkLevel.Display != TileNetLevel.Full;
                    bool isThisCellMidDark = tile.TileNetworkLevel.Display != TileNetLevel.Full;// && !cell.ParentTile.IsTileContainingMachineActors.Display;
                    bool isTileSuperDark = tile.IsOutOfBoundsTile;
                    bool isCellIrradiated = cell.IsCellConsideredIrradiated;
                    UseColorType buildingColorType = UseColorType.MapExploredAndVisionColor;
                    if ( isThisCellVeryDark )
                        buildingColorType = UseColorType.MapUnexploredColor;
                    else if ( isThisCellMidDark )
                        buildingColorType = UseColorType.MapExploredNoVisionColor;

                    //isTileSuperDark = false;
                    //isThisCellVeryDark = false;
                    //isThisCellMidDark = false;

                    //switch ( tile.TileNetworkLevel.Display )
                    //{
                    //    case TileNetLevel.None:
                    //        isTileSuperDark = true;
                    //        break;
                    //    case TileNetLevel.Preliminary:
                    //        isThisCellMidDark = true;
                    //        break;
                    //    case TileNetLevel.Full:
                    //        isThisCellMidDark = false;
                    //        isThisCellMidDark = false;
                    //        break;
                    //}

                    debugStage = 241000;

                    #region Draw The Ground Under The Cell If Just One
                    if ( tile.CellsList.Count == 1 || (tile.HasAnyIrradiatedCells && !tile.HasOnlyIrradiatedCells) )
                    {
                        A5RendererGroup rGroup = mapModeCube.RendererGroup as A5RendererGroup;
                        Vector3 cellDraw = cell.Center;
                        cellDraw.y = MAP_GROUND_DRAW_Y;
                        rGroup.WriteToDrawBufferForOneFrame_BasicColor( cellDraw, Quaternion.identity, MAP_GROUND_CUBE_SCALE, RenderColorStyle.SelfColor,
                            isTileSuperDark ? outOfBoundsColor :
                            (isCellIrradiated ? irradiatedColor :
                            ( isThisCellVeryDark ? unexploredColor : (isThisCellMidDark ? exploredNoVisionColor : exploredYesVisionColor)) ),
                            RenderOpacity.Normal, false );
                    }
                    #endregion

                    debugStage = 251000;
                    if ( currentInvestigation == null && !cell.IsCellConsideredIrradiated )
                    {
                        debugStage = 252000;
                        #region Regular Building Drawing Of This Cell
                        foreach ( ISimBuilding building in cell.BuildingsToDrawInMap.GetDisplayList() )
                        {
                            if ( building == null || building.GetIsDestroyed() )
                                continue;
                            debugStage = 252020;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            debugStage = 252100;
                            if ( building.MachineStructureInBuilding?.Type?.IsTerritoryControlFlag ?? false )
                            {
                                debugStage = 252200;
                                if ( RenderManager_Streets.TryDrawMapItemTerritoryControl( item,
                                    cell.DrawGroup_Buildings, true, 0f, framesPrepped ) )
                                {
                                    debugStage = 252300;
                                    FrameBufferManagerData.BuildingMainCount.Construction++;

                                    if ( !drawJobs && building.MachineStructureInBuilding != null )
                                    {
                                        debugStage = 252400;
                                        RenderManager_Streets.RenderMachineStructureAtBuilding( building.MachineStructureInBuilding, item, true, cell.DrawGroup_Buildings, false, framesPrepped );
                                    }
                                }
                            }
                            else
                            {
                                debugStage = 253100;

                                if ( RenderManager_Streets.TryDrawMapItem( item, buildingColorType,
                                    cell.DrawGroup_Buildings, ColorMath.White,
                                    RenderOpacity.Normal, true, 0f, framesPrepped ) ) //skyscrapers in particular look insane if we don't use this
                                {
                                    debugStage = 253200;
                                    FrameBufferManagerData.BuildingMainCount.Construction++;

                                    if ( !drawJobs && building.MachineStructureInBuilding != null )
                                    {
                                        RenderManager_Streets.RenderMachineStructureAtBuilding( building.MachineStructureInBuilding, item, true, cell.DrawGroup_Buildings, false, framesPrepped );
                                    }
                                }
                            }
                        }
                        #endregion
                    }

                    debugStage = 261000;

                    #region Structure Jobs Of This Cell
                    if ( drawJobs )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithMachineStructures.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            MapCell parentCell = item.ParentCell;
                            MachineStructure structure = item?.SimBuilding?.MachineStructureInBuilding;
                            if ( structure != null )
                                RenderManager_Streets.RenderMachineStructureAtBuilding( structure, item, true, parentCell.DrawGroup_Buildings, true, framesPrepped );
                        }
                    }
                    #endregion
                    
                    debugStage = 265000;
                    #region Swarms Of This Cell
                    if ( drawSwarms )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithSwarms.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            MapCell parentCell = item.ParentCell;
                            Swarm swarm = item?.SimBuilding?.SwarmSpread;
                            if ( swarm != null )
                            {
                                RenderManager_Streets.RenderSwarmAtBuilding( swarm, item, parentCell.DrawGroup_Buildings, framesPrepped );
                            }
                        }
                    }
                    #endregion

                    debugStage = 268000;

                    #region Contemplations Of This Cell
                    if ( drawContemplations )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithContemplations.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            ContemplationType contemplation = building.GetCurrentContemplationThatShouldShowOnMap();
                            if ( contemplation != null )
                            {
                                RenderManager_Streets.RenderContemplationAtBuilding( contemplation, item, cell.DrawGroup_Buildings, framesPrepped );
                            }
                        }
                    }
                    #endregion

                    #region Exploration Sites Of This Cell
                    if ( drawExplorationSites )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithExplorationSites.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            ExplorationSiteType explorationSite = building.GetCurrentExplorationSiteThatShouldShowOnMap();
                            if ( explorationSite != null )
                            {
                                RenderManager_Streets.RenderExplorationSiteAtBuilding( explorationSite, item, cell.DrawGroup_Buildings, framesPrepped );
                            }
                        }
                    }
                    #endregion

                    #region City Conflicts Of This Cell
                    if ( drawCityConflictsLarge || drawCityConflictsSmall )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithCityConflicts.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            CityConflict conflict = building.CurrentCityConflict.Display;
                            if ( conflict != null )
                            {
                                RenderManager_Streets.RenderCityConflictAtBuilding( conflict, item, cell.DrawGroup_Buildings, drawCityConflictsLarge, framesPrepped );
                            }
                        }
                    }
                    #endregion

                    #region StreetSense Of This Cell
                    if ( showAllStreetSense || showProjectRelatedStreetSense )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithStreetSenseActions.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;
                            RenderManager_Streets.DrawStreetSenseActionAtBuilding( item, building, selectedMachineActorOrMull );
                        }
                    }
                    #endregion

                    debugStage = 271000;

                    #region Specialty Resources Of This Cell
                    if ( drawSpecialtyResources )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithSpecialtyResources.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;
                            RenderManager_Streets.RenderSpecialResourceAtBuilding( item, null, framesPrepped );
                        }
                    }
                    #endregion

                    debugStage = 275000;

                    if ( cell.FadingItems_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 275100;
                        FrameBufferManagerData.FadingCount.Construction += cell.FadingItems_MainThreadOnly.Count;

                        for ( int i = cell.FadingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( cell.FadingItems_MainThreadOnly[i].DoPerFrame( framesPrepped ) ) //returns true when it is done
                                cell.FadingItems_MainThreadOnly.RemoveAt( i, true );
                        }
                    }

                    debugStage = 278000;

                    if ( cell.Particles_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 278100;
                        FrameBufferManagerData.ParticleCount.Construction += cell.Particles_MainThreadOnly.Count;

                        for ( int i = cell.Particles_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( cell.Particles_MainThreadOnly[i].DoPerFrameForParticle( framesPrepped ) ) //returns true when it is done
                                cell.Particles_MainThreadOnly.RemoveAt( i, true );
                        }
                    }

                    debugStage = 281000;
                    if ( cell.GlowingIndicators_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 281200;
                        FrameBufferManagerData.ParticleCount.Construction += cell.GlowingIndicators_MainThreadOnly.Count;

                        for ( int i = cell.GlowingIndicators_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            MapGlowingIndicator indicator = cell.GlowingIndicators_MainThreadOnly[i];
                            if ( indicator.DoPerFrameForGlowingIndicator( framesPrepped ) ) //returns true when it is done
                                cell.GlowingIndicators_MainThreadOnly.RemoveAt( i, true );
                            else
                                RenderManager_Streets.DrawMapGlowingIndicator( indicator, false, framesPrepped );
                        }
                    }

                    debugStage = 291000;

                    //if ( renderDistrictBorders )
                    //{
                    //    debugStage = 291200;
                    //    cell.Draw_District.RecalculateIfNeeded();
                    //    DrawMapCellDistrict_MapView( cell.Draw_District, Engine_HotM.IsInVisualizationMode, ColorMath.Black );
                    //}

                    debugStage = 296000;
                    #region Beacons Of This Cell
                    if ( drawAllBeacons || drawKeyBeacons )
                    {
                        foreach ( ISimBuilding building in cell.BuildingsWithBeacons.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;

                            MapCell parentCell = item.ParentCell;
                            BeaconType beacon = item?.SimBuilding?.GetVariant()?.BeaconToShow;
                            if ( beacon != null )
                            {
                                if ( drawAllBeacons || beacon.IsConsideredKeyBeacon )
                                    RenderManager_Streets.RenderBeaconAtBuilding( beacon, item, parentCell.DrawGroup_Buildings, framesPrepped );
                            }
                        }
                    }
                    #endregion

                } //end the loop of cells

                debugStage = 321000;

                RenderAllStructureNetworkRanges();
                if ( drawJobs )
                {
                    int debugStageB = 0;
                    RenderManager_Streets.RenderNetworkConnections( true, ref debugStageB, framesPrepped );
                }

                MachineStructure networkTower = SimCommon.TheNetwork?.Tower;
                if ( networkTower != null && !InputCaching.IsInInspectMode_FocusOnLens )
                {
                    MapItem item = networkTower?.Building?.GetMapItem();
                    if ( item != null )
                    {
                        Vector3 loc = item.OBBCache.TopCenter;
                        //if ( IsMapMode )
                            loc.y += RenderManager_Streets.EXTRA_Y_IN_MAP_MODE + 3f;

                        float distSquared = (loc - CameraCurrent.CameraBodyPosition).GetSquareGroundMagnitude();
                        float scale = IconRefs.NetworkBeacon.DefaultScale;
                        if ( distSquared > 100 ) //10 squared
                        {
                            float squareDistanceAbove = distSquared - 100;
                            if ( squareDistanceAbove >= 1600 ) //40 squared
                                scale *= 4f;
                            else
                                scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                        }

                        IconRefs.NetworkBeacon.Icon.WriteToDrawBufferForOneFrame( true, loc, scale, IconRefs.NetworkBeacon.DefaultColorHDR, false, false, true );
                    }
                }

                debugStage = 421000;

                bool drewPOIS = false;
                #region All POIs
                if ( drawAllPOIs || drawAllBeacons || drawKeyBeacons )
                {
                    drewPOIS = true;
                    foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
                    {
                        MapPOI poi = kv.Value;
                        if ( poi == null || poi.HasBeenDestroyed )
                            continue;
                        POIType poiType = poi.Type;

                        bool shouldDraw = false;
                        if ( drawAllPOIs )
                            shouldDraw = true; //hooray, draw
                        else
                        {
                            if ( !shouldDraw && (drawAllBeacons || drawKeyBeacons) )
                            {
                                if ( !poiType.ActsLikeBeacon )
                                    shouldDraw = false; //if we don't act like a beacon, do not draw
                                else if ( drawAllBeacons || poiType.IsConsideredKeyPOI )
                                    shouldDraw = true; //hooray, draw!
                                else
                                    shouldDraw = false; //we did not match
                            }
                        }

                        if ( !shouldDraw )
                            continue;

                        RenderManager_Streets.RenderPOIBeaconIcon( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );

                        //if we want to draw the shape of a poi, and it's not just a building poi, do that here
                        if ( poi.BuildingOrNull == null && drawPOIShapes )
                        {
                            ArcenFloatRectangle rect = poi.GetOuterRect();
                            Vector3 center = poi.GetCenter().PlusY( 1.5f + poiType.RegionOffsetY );
                            Vector3 size = new Vector3( (float)rect.Width, 2f, (float)rect.Height );
                            Quaternion rotation = Quaternion.identity;
                            if ( poi.SubRegionOrNull != null && poi.SubRegionOrNull.Rotation != 0 )
                                rotation = Quaternion.Euler( 0, poi.SubRegionOrNull.Rotation, 0 );

                            RenderHelper_Objects.DrawPOICubeTransparent( poiType.RegionColorWithAlpha, center, size, rotation );
                        }
                    }
                }
                #endregion

                if ( !drewPOIS )
                {
                    debugStage = 422000;
                    if ( drawKeyPOIStatuses && SimCommon.POIsWithKeyStatus.Count > 0 )
                    {
                        IReadOnlyList<MapTile> visibleTiles = CityMap.TilesInCameraView;
                        foreach ( MapPOI poi in SimCommon.POIsWithKeyStatus.GetDisplayList() )
                        {
                            if ( poi == null || poi.HasBeenDestroyed )
                                continue;

                            if ( visibleTiles.Contains( poi.Tile ) )
                                RenderManager_Streets.RenderPOIStatusIcons( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );
                        }
                    }
                    else if ( drawAllPOIStatuses && SimCommon.POIsWithStatus.Count > 0 )
                    {
                        IReadOnlyList<MapTile> visibleTiles = CityMap.TilesInCameraView;
                        foreach ( MapPOI poi in SimCommon.POIsWithStatus.GetDisplayList() )
                        {
                            if ( poi == null || poi.HasBeenDestroyed )
                                continue;

                            if ( visibleTiles.Contains( poi.Tile ) )
                                RenderManager_Streets.RenderPOIStatusIcons( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );
                        }
                    }

                    if ( drawPOIShapes )
                    {
                        Vector2 mousePoint = new Vector2( Engine_HotM.MouseWorldHitLocation.x, Engine_HotM.MouseWorldHitLocation.z );
                        foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
                        {
                            MapPOI poi = kv.Value;
                            if ( poi == null || poi.HasBeenDestroyed )
                                continue;
                            POIType poiType = poi.Type;
                            if ( poiType == null )
                                continue;

                            //if we want to draw the shape of a poi, and it's not just a building poi, do that here
                            if ( poi.BuildingOrNull == null )
                            {
                                ArcenFloatRectangle rect = poi.GetOuterRect();

                                if ( !InputCaching.IsInInspectMode_FocusOnLens &&
                                    ( InputCaching.ShouldShowDetailedTooltips || InputCaching.IsInInspectMode_ShowMoreStuff || rect.ContainsPoint( mousePoint.x, mousePoint.y ) ) )
                                    RenderManager_Streets.RenderPOIBeaconIcon( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );

                                Vector3 center = poi.GetCenter().PlusY( 1.5f + poiType.RegionOffsetY );
                                Vector3 size = new Vector3( (float)rect.Width, 2f, (float)rect.Height );
                                Quaternion rotation = Quaternion.identity;
                                if ( poi.SubRegionOrNull != null && poi.SubRegionOrNull.Rotation != 0 )
                                    rotation = Quaternion.Euler( 0, poi.SubRegionOrNull.Rotation, 0 );

                                RenderHelper_Objects.DrawPOICubeTransparent( poiType.RegionColorWithAlpha, center, size, rotation );
                            }
                        }
                    }
                }

                debugStage = 521000;

                if ( CityMap.MaterializingItems_MainThreadOnly.Count > 0 ) //do them all!
                {
                    debugStage = 522000;
                    FrameBufferManagerData.FadingCount.Construction += CityMap.MaterializingItems_MainThreadOnly.Count;

                    for ( int i = CityMap.MaterializingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                    {
                        if ( CityMap.MaterializingItems_MainThreadOnly[i].DoPerFrame( framesPrepped ) ) //returns true when it is done
                            CityMap.MaterializingItems_MainThreadOnly.RemoveAt( i, true );
                    }
                }

                debugStage = 621000;

                if ( Engine_HotM.SelectedActor is MachineStructure selectedStructure )
                {
                    ISimBuilding building = selectedStructure?.Building;
                    MapItem item = building?.GetMapItem();

                    if ( building != null && item != null )
                    {
                        RenderManager_Streets.RenderJobRangeInfos( selectedStructure, true, framesPrepped );

                        RenderManager_Streets.DrawMapItemHighlightOutlineLarge( item, HighlightPass.AlwaysHappen, selectedStructure.TerritoryControlType != null, true, framesPrepped );

                        float addedYOffset = 0.1f + RenderManager_Streets.EXTRA_Y_IN_MAP_MODE;
                        float addedXZSize = item.OBBCache.GetCheapRadiusFromExtents();

                        SharedRenderManagerData.DrawSelectionHexAroundActor( selectedStructure, ColorRefs.UnitNewStyleSelectionA,
                        MathRefs.SelectedHexA_XZ_Size.FloatMin, MathRefs.SelectedHexA_XZ_AddedSize.FloatMin, MathRefs.SelectedHexA_XZ_Speed.FloatMin,
                        MathRefs.SelectedHexA_XZ_Thickness.FloatMin, MathRefs.SelectedHexA_Y_Offset.FloatMin + addedYOffset );
                        SharedRenderManagerData.DrawSelectionHexAroundActor( selectedStructure, ColorRefs.UnitNewStyleSelectionB,
                            MathRefs.SelectedHexB_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexB_XZ_AddedSize.FloatMin, MathRefs.SelectedHexB_XZ_Speed.FloatMin,
                            MathRefs.SelectedHexB_XZ_Thickness.FloatMin, MathRefs.SelectedHexB_Y_Offset.FloatMin + addedYOffset );
                        SharedRenderManagerData.DrawSelectionHexAroundActor( selectedStructure, ColorRefs.UnitNewStyleSelectionC,
                            MathRefs.SelectedHexC_XZ_Size.FloatMin + addedXZSize, MathRefs.SelectedHexC_XZ_AddedSize.FloatMin, MathRefs.SelectedHexC_XZ_Speed.FloatMin,
                            MathRefs.SelectedHexC_XZ_Thickness.FloatMin, MathRefs.SelectedHexC_Y_Offset.FloatMin + addedYOffset );
                    }
                }

                debugStage = 721000;

                //loop over all machine vehicles
                DictionaryView<int, ISimMachineVehicle> machineVehicles = World.Forces.GetMachineVehiclesByID();
                if ( machineVehicles.Count > 0 ) //there should not be THAT many of these, so just loop them all
                {
                    debugStage = 722000;
                    foreach ( KeyValuePair<int, ISimMachineVehicle> kv in machineVehicles )
                    {
                        ISimMachineVehicle vehicle = kv.Value;

                        vehicle.RenderColliderIcon( IconLogic.Normal );
                    }
                }

                debugStage = 821000;
                //loop over all machine units
                DictionaryView<int, ISimMachineUnit> machineUnits = World.Forces.GetMachineUnitsByID();
                if ( machineUnits.Count > 0 ) //there should not be THAT many of these, so just loop them all
                {
                    debugStage = 822000;
                    foreach ( KeyValuePair<int, ISimMachineUnit> kv in machineUnits )
                    {
                        ISimMachineUnit unit = kv.Value;
                        if ( unit == null || !unit.GetIsDeployed() || unit.IsFullDead )
                            continue;

                        unit.RenderColliderIcon( IconLogic.Normal );
                    }
                }

                {
                    debugStage = 841000;
                    //loop over all npc units
                    DictionaryView<int, ISimNPCUnit> npcUnits = World.Forces.GetAllNPCUnitsByID();
                    if ( npcUnits.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 842000;
                        foreach ( KeyValuePair<int, ISimNPCUnit> kv in npcUnits )
                        {
                            ISimNPCUnit unit = kv.Value;
                            if ( unit == null )
                                continue;
                            NPCUnitStance stance = unit.Stance;
                            if ( stance == null )
                                continue;

                            //do this so that actions happen
                            unit.DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, framesPrepped,
                                out _, out _, out _ );

                            if ( unit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                                continue;

                            bool canRender = (showAllNPCUnits || showKeyNPCUnits || showHostileUnits);
                            NPCUnitType unitType = unit.UnitType;
                            
                            IconLogic logic = IconLogic.Normal;
                            if ( unit.GetIsPlayerControlled() )
                            { } //always draw these!
                            else if ( canRender )
                            {
                                //hijack the normal rendering
                                if ( drawSpecialtyResources && unitType != null &&
                                    (unitType.Resource1RecoveredOnDeath != null || unitType.Resource1RecoveredOnExtract != null ||
                                    unitType.Resource2RecoveredOnDeath != null || unitType.Resource2RecoveredOnExtract != null ||
                                    unitType.Resource3RecoveredOnDeath != null || unitType.Resource3RecoveredOnExtract != null) )
                                {
                                    //we're showing specialty resources, and this includes some
                                    logic = IconLogic.ResourceScavenging;
                                }
                                else //okay, we're not drawing specialty resources, but we can render.  Should we?
                                {
                                    if ( !showAllNPCUnits )
                                    {
                                        if ( showKeyNPCUnits )
                                        {
                                            if ( stance.IsStanceAlwaysDrawnInCityMap )
                                            { } //hooray, this is allowed, as they have that stat
                                            else
                                                continue; //skip, since we are not showing all the npc units
                                        }
                                        else if ( showHostileUnits )
                                        {
                                            if ( !unit.GetIsConsideredHostileToPlayer() )
                                                continue; //only show the ones considered hostile
                                        }
                                    }
                                    if ( skipPassiveGuards && stance.IsConsideredPassiveGuard )
                                        continue;
                                    if ( skipEconomicUnits && (stance.IsPartOfCityEconomy || stance.IsPartOfRegionalEconomy || stance.IsPartOfInternationalEconomy) )
                                        continue;
                                }
                            }
                            else //we supposedly can't render.  But are we drawing specialty resources?
                            {
                                if ( drawSpecialtyResources && unitType != null &&
                                    (unitType.Resource1RecoveredOnDeath != null || unitType.Resource1RecoveredOnExtract != null ||
                                    unitType.Resource2RecoveredOnDeath != null || unitType.Resource2RecoveredOnExtract != null ||
                                    unitType.Resource3RecoveredOnDeath != null || unitType.Resource3RecoveredOnExtract != null) )
                                {
                                    //we're showing specialty resources, and this includes some
                                    logic = IconLogic.ResourceScavenging;
                                }
                                else
                                    continue; //normally would skip this
                            }

                            if ( unitType.RendersOnTheCityMap )
                            {
                                if ( unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject,
                                    out IAutoPooledFloatingObject floatingSimpleObject, out Color drawColor ) )
                                {
                                    bool isTheSelectedUnit = unit == Engine_HotM.SelectedActor;
                                    if ( floatingLODObject != null )
                                        RenderHelper_Objects.DrawNPCUnit_LOD( unit, floatingLODObject, drawColor, isTheSelectedUnit, IsMouseOver, false ); //silhouette on the city map
                                    else
                                        RenderHelper_Objects.DrawNPCUnit_Simple( unit, floatingSimpleObject, drawColor, isTheSelectedUnit, IsMouseOver, false ); //silhouette on the city map
                                }
                            }

                            unit.RenderColliderIcon( logic );
                        }
                    }
                }

                debugStage = 861000;
                IReadOnlyList<MapCell> outOfBoundsCells = CityMap.CellsOutOfCameraView;
                foreach ( MapCell nonVisibleCell in outOfBoundsCells )
                {
                    debugStage = 862000;
                    if ( nonVisibleCell.FadingItems_MainThreadOnly.Count > 0 )
                    {
                        FrameBufferManagerData.FadingCount.Construction += nonVisibleCell.FadingItems_MainThreadOnly.Count;

                        for ( int i = nonVisibleCell.FadingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( nonVisibleCell.FadingItems_MainThreadOnly[i].DoPerFrame( framesPrepped ) ) //returns true when it is done
                                nonVisibleCell.FadingItems_MainThreadOnly.RemoveAt( i, true );

                        }
                    }

                    if ( nonVisibleCell.Particles_MainThreadOnly.Count > 0 )
                    {
                        FrameBufferManagerData.ParticleCount.Construction += nonVisibleCell.Particles_MainThreadOnly.Count;

                        for ( int i = nonVisibleCell.Particles_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( nonVisibleCell.Particles_MainThreadOnly[i].DoPerFrameForParticle( framesPrepped ) ) //returns true when it is done
                                nonVisibleCell.Particles_MainThreadOnly.RemoveAt( i, true );

                        }
                    }
                } //end loop over non-visible cells

                debugStage = 921000;
                RenderHelper_EventCamera.RenderIfNeeded();
                A5ObjectAggregation.FloatingIconListPool.DrawAllActiveInstructions();
                A5ObjectAggregation.FloatingIconColliderPool.DrawAllActiveItems();

                debugStage = 931000;
                foreach ( Lockdown lockdown in SimCommon.Lockdowns_MainThreadOnly )
                    RenderManager_Streets.TryDrawLockdown( lockdown );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "PreRenderFrame-CityMap", debugStage, e, Verbosity.ShowAsError );
            }

            SharedRenderManagerData.stopwatch.Stop();
        }
        #endregion

        #region DrawMapRegion
        private static void DrawMapRegion( MapSubRegion region, bool UseVisMaterial,  Color ColorForVis )
        {
            if ( region == null )
                return;

            if ( SharedRenderManagerData.regionCube == null )
            {
                SharedRenderManagerData.regionCube = VisSimpleDrawingObjectTable.Instance.GetRowByID( "MetallicCubeTransparent" );
                if ( SharedRenderManagerData.regionCube == null )
                    return;
            }

            A5RendererGroup rendGroup = SharedRenderManagerData.regionCube.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
                return;

            {                
                Quaternion rot = region.Rotation;

                Vector3 size = region.OBBCache.OBBSize;
                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( region.Position, rot, new Vector3( size.x, 1f, size.z ),
                    RenderColorStyle.SelfColor, ColorForVis, 
                    RenderOpacity.Normal, false ); //this will be transparent, but not in a way we need to manage ourselves
            }
        }
        #endregion

        private static readonly Vector3 holoWallScaleMap = new Vector3( 20f, 2.5f, 1f );

        //#region DrawMapCellDistrict_MapView
        //private static void DrawMapCellDistrict_MapView( MapCellDistrictDrawer MapCellDistrict, bool UseVisMaterial, Color ColorForVis )
        //{
        //    if ( MapCellDistrict == null )
        //        return;

        //    if ( SharedRenderManagerData.districtHoloWallMap == null )
        //    {
        //        SharedRenderManagerData.districtHoloWallMap = VisSimpleDrawingObjectTable.Instance.GetRowByID( "BattleZoneWall" );
        //        if ( SharedRenderManagerData.districtHoloWallMap == null )
        //            return;
        //    }

        //    A5RendererGroup rendGroupHoloWall = SharedRenderManagerData.districtHoloWallMap.RendererGroup as A5RendererGroup;

        //    if ( rendGroupHoloWall != null && MapCellDistrict.MapHoloWallSegments.Count > 0 )
        //    {
        //        foreach ( HoloWallSegment holoWallSegment in MapCellDistrict.MapHoloWallSegments )
        //        {
        //            rendGroupHoloWall.WriteToDrawBufferForOneFrame_BasicNoColor( holoWallSegment.Pos, holoWallSegment.Rotation, holoWallScaleMap,
        //                RenderColorStyle.NoColor );
        //        }
        //    }
        //}
        //#endregion

        #region RenderAllStructureNetworkRanges
        public static void RenderAllStructureNetworkRanges()
        {
            if ( SharedRenderManagerData.districtHoloWallMap == null )
            {
                SharedRenderManagerData.districtHoloWallMap = VisSimpleDrawingObjectTable.Instance.GetRowByID( "BattleZoneWall" );
                if ( SharedRenderManagerData.districtHoloWallMap == null )
                    return;
            }

            A5RendererGroup rendGroupHoloWall = SharedRenderManagerData.districtHoloWallMap.RendererGroup as A5RendererGroup;

            foreach ( MapCell cell in CityMap.CellsInCameraView )
            {
                cell.Draw_Network.RecalculateSegmentsOnMainThreadIfNeeded();
                if ( rendGroupHoloWall != null && cell.Draw_Network.MapHoloWallSegments.Count > 0 )
                {
                    foreach ( HoloWallSegment holoWallSegment in cell.Draw_Network.MapHoloWallSegments )
                    {
                        rendGroupHoloWall.WriteToDrawBufferForOneFrame_BasicNoColor( holoWallSegment.Pos, holoWallSegment.Rotation, holoWallScaleMap,
                            RenderColorStyle.NoColor );
                    }
                }
            }

            //todo ranges
            //foreach ( MachineStructure structure in SimCommon.CurrentNetworkProviders.GetDisplayList() )
            //{
            //    if ( structure?.Type == null )
            //        continue;
            //    ISimBuilding building = structure.Building;
            //    if ( building == null )
            //        continue;
            //    MapItem item = building.GetMapItem();
            //    if ( item == null )
            //        continue;
            //    if ( structure.LastFramePrepRendered_NetworkRange >= RenderManager.FramesPrepped )
            //        continue;
            //    structure.LastFramePrepRendered_NetworkRange = RenderManager.FramesPrepped;

            //    //draw the network range, if it has one!  It should!
            //    if ( structure.GetStructureNetworkRange( out float networkRange ) )
            //    {
            //        //DrawHelper.RenderRangeCircle( ,
            //        //    networkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );

            //        Vector3 center = item.OBBCache.Center;

            //        VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCylinderVis;
            //        A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
            //        center.y = 0.2f;

            //        if ( CameraCurrent.TestFrustumColliderInternalFast( center, 1f, networkRange ) )  //frustum cull these, as they are large
            //        {
            //            float diameter = networkRange + networkRange;
            //            Vector3 scale = new Vector3( diameter, 2f, diameter );

            //            rGroup.WriteToDrawBufferForOneFrame_BasicColor( center, Quaternion.identity, scale, RenderColorStyle.SelfColor, 
            //                ColorRefs.MachineNetworkMapGroundColor.ColorWithoutHDR, RenderOpacity.Normal, false );
            //        }
            //    }
            //}
        }
        #endregion
    }
}

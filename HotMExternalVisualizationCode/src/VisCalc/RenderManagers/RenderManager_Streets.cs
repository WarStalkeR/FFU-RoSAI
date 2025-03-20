using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Runtime.CompilerServices;

namespace Arcen.HotM.ExternalVis
{
    public static class RenderManager_Streets
    {
        internal const float EXTRA_Y_IN_MAP_MODE = 1.04f;

        internal const float EXTRA_Y_FOR_STRUCTURE_ERRORS = 0.5f;

        internal static readonly Vector3 GROUND_CUBE_SCALE = new Vector3( 20, 2, 20 );
        internal static readonly Vector3 HYPER_GROUND_CUBE_SCALE = new Vector3( 1000, 2, 1000 );
        internal static readonly Vector3 GROUND_CUBE_INFINITY_SCALE = new Vector3( 15000, 0.2f, 15000 );
        internal const float GROUND_DRAW_Y_CUBES_INFINITY = -8.05f;
        internal const float GROUND_DRAW_Y_CUBES_BG = -1.05f;
        internal const float GROUND_DRAW_Y_MAIN = -1.04f;
        internal const float GROUND_DRAW_Y_OUT_OF_BOUNDS = -1.02f;
        private const float GROUND_MAX_MOVE_DOWN = -1.5f;

        private static Color defaultGrayColor = new Color( 0.3f, 0.3f, 0.3f, 1f );

        public static bool SubCellRenderingForcedOff = false;

        private static int totalItemsConsidered = 0;

        #region PreRenderFrame
        public static void PreRenderFrame()
        {
            totalItemsConsidered = 0;
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
            {
                return;
            }

            SharedRenderManagerData.stopwatch.Restart();

            bool renderDistrictBorders = GameSettings.Current.GetBool( "City_RenderDistrictBorders" );
            bool isFogOfWarDisabled = SimCommon.IsFogOfWarDisabled;

            BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();

            Color defaultColor = SharedRenderManagerData.defaultColor_Global;
            bool doGroundVisMode = false;
            if ( Engine_HotM.IsInVisualizationMode && effectiveOverlayType != null && effectiveOverlayType.Implementation != null )
            {
                defaultColor = effectiveOverlayType.Implementation.GetDefaultInCurrentOverlay( effectiveOverlayType );
                if ( effectiveOverlayType.ChangeGroundVisualization )
                {
                    MainGameCoreGameLoop mainLoop = Engine_HotM.Instance.GameLoop as MainGameCoreGameLoop;
                    if ( mainLoop && MaterialRefs.GroundVis )
                    {
                        MaterialRefs.GroundVis.color = defaultColor;
                        doGroundVisMode = true;
                    }
                }
            }

            //if ( hasBoxToDraw )
            //{
            //    List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
            //    shapes.Clear();
            //    shapes.Add( priorBox );
            //}
            //List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
            //shapes.Clear();

            MapItem selectedBuildingItem = VisManagerVeryBase.GetSelectedBuilding();
            MapItem targetedBuildingItem = null;
            Color targetingColor = ColorRefs.HoveredBuilding.ColorHDR;
            targetedBuildingItem = MouseHelper.BuildingUnderCursor?.GetMapItem();

            bool isSelectedOrHoveredMode = (selectedBuildingItem != null || targetedBuildingItem != null);
            Engine_HotM.IsInSelectedOrHoveredBuildingHighlightMode = isSelectedOrHoveredMode;

            bool doVisualizationForBuildings = Engine_HotM.IsInVisualizationMode;
            bool doVisualizationForOtherStuff = Engine_HotM.IsInVisualizationMode && doGroundVisMode;

            IReadOnlyList<MapCell> visibleCells = CityMap.CellsInCameraView;
            IReadOnlyList<MapTile> visibleTiles = CityMap.TilesInCameraView;
            IReadOnlyList<MapSubCell> visibleSubCells = CityMap.SubCellsInCameraView;
            bool rendersContentsBySubCell = visibleSubCells.Count > 0 && !SubCellRenderingForcedOff;
            bool isHighlightMode = (Engine_HotM.IsInVisualizationMode || Engine_HotM.IsInPerBuildingHighlightMode) &&
                effectiveOverlayType != null && effectiveOverlayType.Implementation != null;
            Int64 framesPrepped = RenderManager.FramesPrepped;

            bool drawAllBeacons = (SimCommon.CurrentCityLens?.ShowAllBeacons?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowBeacons ?? false);
            bool drawKeyBeacons = (SimCommon.CurrentCityLens?.ShowKeyBeacons?.Display ?? false);

            bool drawAllPOIStatuses = (SimCommon.CurrentCityLens?.ShowAllPOIStatuses?.Display ?? false);
            bool drawKeyPOIStatuses = (SimCommon.CurrentCityLens?.ShowKeyPOIStatuses?.Display ?? false);
            bool drawAllStreetSense = (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false);
            bool drawProjectRelatedStreetSense = (SimCommon.CurrentCityLens?.ShowProjectRelatedStreetSense?.Display ?? false);
            bool debug_ShowOutdoorSpots = InputCaching.Debug_ShowOutdoorSpots;
            bool skipDrawing_Roads = InputCaching.SkipDrawing_Roads;
            bool skipDrawing_MajorDecorations = InputCaching.SkipDrawing_MajorDecorations;
            bool skipDrawing_MinorDecorations = InputCaching.SkipDrawing_MinorDecorations;

            BuildingMarkerColor defaultMarkerColor = BuildingMarkerColorTable.Instance.DefaultRow;
            bool canHighlightBuildingsAsTargets = MouseHelper.CanTriggerBuildingHighlights();

            bool shouldDrawJobsAsIcons = (SimCommon.CurrentCityLens?.ShowJobs?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowJobs ?? false);
            bool shouldDrawSwarms = (SimCommon.CurrentCityLens?.ShowSwarms?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowSwarms ?? false);
            bool shouldDrawContemplations = (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false);
            bool shouldDrawExplorationSites = (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false);
            bool shouldDrawCityConflictsLarge = (SimCommon.CurrentCityLens?.ShowCityConflictsLarge?.Display ?? false);
            bool shouldDrawCityConflictsSmall = (SimCommon.CurrentCityLens?.ShowCityConflictsSmall?.Display ?? false);
            bool shouldDrawSpecialtyResources = (SimCommon.CurrentCityLens?.ShowSpecialResources?.Display ?? false) || (Engine_HotM.SelectedActorAbilityTargetingMode?.ShowsSpecialtyResources ?? false);
            bool isInPerBuildingHighlightMode = Engine_HotM.IsInPerBuildingHighlightMode;
            bool overrideShowAllBuildingMarkers = Engine_HotM.OverrideShowAllBuildingMarkers;

            int selectedBuildingItemGlobalIndex = selectedBuildingItem?.MapItemGlobalIndex ?? -3;
            int targetedBuildingItemGlobalIndex = targetedBuildingItem?.MapItemGlobalIndex ?? -3;

            ISimMachineActor selectedActor = Engine_HotM.SelectedActor as ISimMachineActor;
            if ( selectedActor != null )
            {
                if ( selectedActor.GetIsBlockedFromActingInGeneral() )
                    selectedActor = null;
                else
                {
                    ISimMachineUnit selectedUnit = Engine_HotM.SelectedActor as ISimMachineUnit;
                    if ( selectedUnit != null )
                    {
                        if ( !selectedUnit.GetIsDeployed() )
                            selectedActor = null;
                    }
                }
            }
            Vector3 actorLoc = selectedActor == null ? Vector3.zero : selectedActor.GetDrawLocation();
            float actorLocationActionRange = 0;
            float actorLocationActionRangeSquared = 0;
            if ( selectedActor != null )
            {
                actorLocationActionRange = selectedActor.GetMovementRange();
                actorLocationActionRangeSquared = actorLocationActionRange * actorLocationActionRange;
            }

            //if ( VisCurrent.ShouldCurrentlyHaveInfiniteBuildingDrawDistance )
            {
                //draw the infinite ground
                VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCubeUnder;
                A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                Vector3 cellDraw = Vector3.zero;
                cellDraw.y = GROUND_DRAW_Y_CUBES_INFINITY;
                rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( cellDraw, Quaternion.identity, GROUND_CUBE_INFINITY_SCALE, RenderColorStyle.NoColor );
            }

            int visibleCellCount = visibleCells.Count;
            for ( int ci = 0; ci < visibleCellCount; ci++ )
            {
                MapCell cell = visibleCells[ci];
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    if ( debug_ShowOutdoorSpots )
                    {
                        foreach ( MapOutdoorSpot outdoorSpot in cell.AllOutdoorSpots )
                            DrawHelper.RenderCube( outdoorSpot.Position, 0.2f, ColorMath.White, 1f );
                    }

                    debugStage = 1100;
                    bool isOutOfBounds = cell.ParentTile.IsOutOfBoundsTile;
                    float extraYOfGround = isOutOfBounds ? 0.51f : 0f;

                    debugStage = 1200;
                    #region Draw The Ground Under The Cell
                    {
                        VisSimpleDrawingObject groundToDraw = (isOutOfBounds ? CommonRefs.GroundCubeOutOfBounds : ((doGroundVisMode ? CommonRefs.GroundCubeVis :
                            (isFogOfWarDisabled ? CommonRefs.GroundCube : CommonRefs.GroundCubeFogOfWar))));
                        A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                        Vector3 cellDraw = cell.Center;
                        cellDraw.y = (isOutOfBounds ? GROUND_DRAW_Y_OUT_OF_BOUNDS : (doGroundVisMode || isFogOfWarDisabled ? GROUND_DRAW_Y_MAIN : GROUND_DRAW_Y_CUBES_BG)) + extraYOfGround;
                        rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( cellDraw, Quaternion.identity, GROUND_CUBE_SCALE, RenderColorStyle.NoColor );
                    }
                    #endregion

                    debugStage = 2100;
                    if ( cell.DrawGroup_Roads.ShouldDrawBasedOnDistance && !skipDrawing_Roads )
                    {
                        Color roadsColor = cell.DrawGroup_Roads.GetEffectiveColor( defaultColor );
                        Color fogColor = cell.DrawGroup_Roads.GetEffectiveColor( SharedRenderManagerData.defaultColor_Global );

                        if ( rendersContentsBySubCell )
                        {
                            debugStage = 2200;
                            List<MapSubCell> subCells = cell.SubCells;
                            int subCellCount = subCells.Count;
                            for ( int sci = 0; sci < subCellCount; sci++ )
                            {
                                MapSubCell subCell = subCells[sci];
                                if ( subCell == null )
                                    continue;
                                if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                                    continue;
                                //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                                //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                                DrawRoads( subCell.Roads, cell.DrawGroup_Roads, doVisualizationForOtherStuff, roadsColor, fogColor, framesPrepped );
                            }
                        }
                        else
                        {
                            debugStage = 2300;
                            DrawRoads( cell.AllRoads, cell.DrawGroup_Roads, doVisualizationForOtherStuff, roadsColor, fogColor, framesPrepped );
                        }
                    }

                    debugStage = 5100;
                    if ( cell.DrawGroup_Buildings.ShouldDrawBasedOnDistance )
                    {
                        Color otherColor = cell.DrawGroup_Buildings.GetEffectiveColor( defaultColor );
                        Color fogColor = cell.DrawGroup_Buildings.GetEffectiveColor( SharedRenderManagerData.defaultColor_Global );

                        if ( rendersContentsBySubCell )
                        {
                            debugStage = 5200;
                            List<MapSubCell> subCells = cell.SubCells;
                            int subCellCount = subCells.Count;
                            for ( int sci = 0; sci < subCellCount; sci++ )
                            {
                                MapSubCell subCell = subCells[sci];
                                if ( subCell == null )
                                    continue;
                                if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                                    continue;
                                //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                                //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                                DrawOtherSkeletonItemsOrFences( subCell.OtherSkeletonItems, isOutOfBounds, extraYOfGround, cell.DrawGroup_Buildings, doVisualizationForOtherStuff, otherColor, fogColor, framesPrepped );
                                DrawOtherSkeletonItemsOrFences( subCell.Fences, isOutOfBounds, extraYOfGround, cell.DrawGroup_Buildings, doVisualizationForOtherStuff, otherColor, fogColor, framesPrepped );
                            }
                        }
                        else
                        {
                            debugStage = 5300;
                            DrawOtherSkeletonItemsOrFences( cell.OtherSkeletonItems, isOutOfBounds, extraYOfGround, cell.DrawGroup_Buildings, doVisualizationForOtherStuff, otherColor, fogColor, framesPrepped );
                            DrawOtherSkeletonItemsOrFences( cell.Fences, isOutOfBounds, extraYOfGround, cell.DrawGroup_Buildings, doVisualizationForOtherStuff, otherColor, fogColor, framesPrepped );
                        }
                    }

                    debugStage = 7100;
                    if ( cell.DrawGroup_Buildings.ShouldDrawBasedOnDistance )
                    {
                        Color buildingsColor = cell.DrawGroup_Buildings.GetEffectiveColor( defaultColor );
                        Color fogColor = cell.DrawGroup_Buildings.GetEffectiveColor( SharedRenderManagerData.defaultColor_Global );

                        if ( rendersContentsBySubCell )
                        {
                            debugStage = 7200;
                            List<MapSubCell> subCells = cell.SubCells;
                            int subCellCount = subCells.Count;
                            if ( isHighlightMode )
                            {
                                for ( int sci = 0; sci < subCellCount; sci++ )
                                {
                                    MapSubCell subCell = subCells[sci];
                                    if ( subCell == null )
                                        continue;
                                    if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                                        continue;
                                    //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                                    //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                                    DrawBuildings_Highlighted( subCell.BuildingList.GetDisplayList(), cell.DrawGroup_Buildings, doVisualizationForBuildings, buildingsColor, targetingColor,
                                        isSelectedOrHoveredMode, selectedBuildingItemGlobalIndex, targetedBuildingItemGlobalIndex, cell, effectiveOverlayType,
                                        doGroundVisMode, isFogOfWarDisabled, drawAllBeacons, drawKeyBeacons, drawAllStreetSense || drawProjectRelatedStreetSense, framesPrepped,

                                        defaultMarkerColor, fogColor, canHighlightBuildingsAsTargets, shouldDrawJobsAsIcons,
                                        shouldDrawSwarms, shouldDrawContemplations, shouldDrawExplorationSites, shouldDrawCityConflictsLarge, shouldDrawCityConflictsSmall,
                                        shouldDrawSpecialtyResources, isInPerBuildingHighlightMode, overrideShowAllBuildingMarkers,

                                        selectedActor, actorLoc, actorLocationActionRange, actorLocationActionRangeSquared );
                                }
                            }
                            else
                            {
                                for ( int sci = 0; sci < subCellCount; sci++ )
                                {
                                    MapSubCell subCell = subCells[sci];
                                    if ( subCell == null )
                                        continue;
                                    if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                                        continue;
                                    //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                                    //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                                    DrawBuildings_Normal( subCell.BuildingList.GetDisplayList(), cell.DrawGroup_Buildings, doVisualizationForBuildings, buildingsColor, targetingColor,
                                        isSelectedOrHoveredMode, selectedBuildingItemGlobalIndex, targetedBuildingItemGlobalIndex, cell, effectiveOverlayType,
                                        doGroundVisMode, isFogOfWarDisabled, drawAllBeacons, drawKeyBeacons, drawAllStreetSense || drawProjectRelatedStreetSense, framesPrepped,

                                        defaultMarkerColor, fogColor, canHighlightBuildingsAsTargets, shouldDrawJobsAsIcons,
                                        shouldDrawSwarms, shouldDrawContemplations, shouldDrawExplorationSites, shouldDrawCityConflictsLarge, shouldDrawCityConflictsSmall,
                                        shouldDrawSpecialtyResources, isInPerBuildingHighlightMode, overrideShowAllBuildingMarkers,

                                        selectedActor, actorLoc, actorLocationActionRange, actorLocationActionRangeSquared );
                                }
                            }
                        }
                        else
                        {
                            debugStage = 7300;
                            if ( isHighlightMode )
                                DrawBuildings_Highlighted( cell.BuildingList.GetDisplayList(), cell.DrawGroup_Buildings, doVisualizationForBuildings, buildingsColor, targetingColor,
                                    isSelectedOrHoveredMode, selectedBuildingItemGlobalIndex, targetedBuildingItemGlobalIndex, cell, effectiveOverlayType,
                                    doGroundVisMode, isFogOfWarDisabled, drawAllBeacons, drawKeyBeacons, drawAllStreetSense || drawProjectRelatedStreetSense, framesPrepped,

                                    defaultMarkerColor, fogColor, canHighlightBuildingsAsTargets, shouldDrawJobsAsIcons,
                                    shouldDrawSwarms, shouldDrawContemplations, shouldDrawExplorationSites, shouldDrawCityConflictsLarge, shouldDrawCityConflictsSmall,
                                    shouldDrawSpecialtyResources, isInPerBuildingHighlightMode, overrideShowAllBuildingMarkers,

                                    selectedActor, actorLoc, actorLocationActionRange, actorLocationActionRangeSquared );
                            else
                                DrawBuildings_Normal( cell.BuildingList.GetDisplayList(), cell.DrawGroup_Buildings, doVisualizationForBuildings, buildingsColor, targetingColor,
                                    isSelectedOrHoveredMode, selectedBuildingItemGlobalIndex, targetedBuildingItemGlobalIndex, cell, effectiveOverlayType,
                                    doGroundVisMode, isFogOfWarDisabled, drawAllBeacons, drawKeyBeacons, drawAllStreetSense || drawProjectRelatedStreetSense, framesPrepped,

                                    defaultMarkerColor, fogColor, canHighlightBuildingsAsTargets, shouldDrawJobsAsIcons,
                                    shouldDrawSwarms, shouldDrawContemplations, shouldDrawExplorationSites, shouldDrawCityConflictsLarge, shouldDrawCityConflictsSmall,
                                    shouldDrawSpecialtyResources, isInPerBuildingHighlightMode, overrideShowAllBuildingMarkers,

                                    selectedActor, actorLoc, actorLocationActionRange, actorLocationActionRangeSquared );
                        }
                    }

                    debugStage = 8100;
                    MapCellDrawGroup majorDecoDrawGroup = isOutOfBounds ? cell.DrawGroup_Buildings : cell.DrawGroup_MajorDecorations;

                    if ( majorDecoDrawGroup.ShouldDrawBasedOnDistance && !skipDrawing_MajorDecorations )
                    {
                        Color majorDecColor = majorDecoDrawGroup.GetEffectiveColor( defaultColor );
                        Color fogColor = majorDecoDrawGroup.GetEffectiveColor( SharedRenderManagerData.defaultColor_Global );

                        //if ( rendersContentsBySubCell )
                        //{
                        //    debugStage = 8200;
                        //    List<MapSubCell> subCells = cell.SubCells;
                        //    int subCellCount = subCells.Count;
                        //    for ( int sci = 0; sci < subCellCount; sci++ )
                        //    {
                        //        MapSubCell subCell = subCells[sci];
                                    //if ( subCell == null )
                                    //    continue;
                        //        if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                        //            continue;
                        //        //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                        //        //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                        //        DrawMajorDecorations( subCell.DecorationMajor, isOutOfBounds, extraYOfGround, majorDecoDrawGroup, doVisualizationForOtherStuff, majorDecColor, fogColor, framesPrepped );
                        //    }
                        //}
                        //else
                        {
                            debugStage = 8300;
                            DrawMajorDecorations( cell.DecorationMajor, isOutOfBounds, extraYOfGround, majorDecoDrawGroup, doVisualizationForOtherStuff, majorDecColor, fogColor, framesPrepped );
                        }
                    }

                    debugStage = 12100;
                    if ( cell.DrawGroup_MinorDecorations.ShouldDrawBasedOnDistance && !skipDrawing_MinorDecorations )
                    {
                        Color minorDecColor = cell.DrawGroup_MinorDecorations.GetEffectiveColor( defaultColor );
                        Color fogColor = cell.DrawGroup_MinorDecorations.GetEffectiveColor( SharedRenderManagerData.defaultColor_Global );

                        //if ( rendersContentsBySubCell )
                        //{
                        //    debugStage = 12200;
                        //    List<MapSubCell> subCells = cell.SubCells;
                        //    int subCellCount = subCells.Count;
                        //    for ( int sci = 0; sci < subCellCount; sci++ )
                        //    {
                        //        MapSubCell subCell = subCells[sci];
                        //if ( subCell == null )
                        //    continue;
                        //        if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                        //            continue;
                        //        //note, this may include items from other cells that overlap this subcell, so it's more accurate than drawing the cell
                        //        //so even when all of the subcells of a cell are visible, the subcells should be rendered instead of the cell itself.
                        //        DrawMinorDecorations( subCell.DecorationMinor, isOutOfBounds, extraYOfGround, cell.DrawGroup_MinorDecorations, doVisualizationForOtherStuff, minorDecColor, fogColor, framesPrepped );
                        //    }
                        //}
                        //else
                        {
                            debugStage = 12300;
                            DrawMinorDecorations( cell.DecorationMinor, isOutOfBounds, extraYOfGround, cell.DrawGroup_MinorDecorations, doVisualizationForOtherStuff, minorDecColor, fogColor, framesPrepped );
                        }
                    }

                    debugStage = 14100;
                    if ( cell.FadingItems_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 14200;
                        FrameBufferManagerData.FadingCount.Construction += cell.FadingItems_MainThreadOnly.Count;

                        for ( int i = cell.FadingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( cell.FadingItems_MainThreadOnly[i].DoPerFrame( framesPrepped ) ) //returns true when it is done
                                cell.FadingItems_MainThreadOnly.RemoveAt( i, true );

                        }
                    }

                    debugStage = 16100;
                    if ( cell.Particles_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 16200;
                        FrameBufferManagerData.ParticleCount.Construction += cell.Particles_MainThreadOnly.Count;

                        for ( int i = cell.Particles_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( cell.Particles_MainThreadOnly[i].DoPerFrameForParticle( framesPrepped ) ) //returns true when it is done
                                cell.Particles_MainThreadOnly.RemoveAt( i, true );

                        }
                    }

                    debugStage = 18100;
                    if ( cell.GlowingIndicators_MainThreadOnly.Count > 0 ) //must also be done on non-visible cells!
                    {
                        debugStage = 18200;
                        FrameBufferManagerData.ParticleCount.Construction += cell.GlowingIndicators_MainThreadOnly.Count;

                        for ( int i = cell.GlowingIndicators_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            MapGlowingIndicator indicator = cell.GlowingIndicators_MainThreadOnly[i];
                            if ( indicator.DoPerFrameForGlowingIndicator( framesPrepped ) ) //returns true when it is done
                                cell.GlowingIndicators_MainThreadOnly.RemoveAt( i, true );
                            else
                                DrawMapGlowingIndicator( indicator, false, framesPrepped );
                        }
                    }

                    debugStage = 20100;
                    if ( cell.ActiveStreetMobs.Count > 0 )
                        FrameBufferManagerData.StreetMobCount.Construction += cell.ActiveStreetMobs.Count;

                    //if ( renderDistrictBorders )
                    //{
                    //    debugStage = 20200;
                    //    cell.Draw_District.RecalculateIfNeeded();
                    //    DrawMapCellDistrict_StreetsView( cell.Draw_District, Engine_HotM.IsInVisualizationMode, ColorMath.Black );
                    //}
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "PreRenderFrame-Streets-Cell", debugStage, e, Verbosity.ShowAsError );
                }

            } //end loop over visible cells

            SimTimingInfo.PerFrameVisItemsConsideredCount.LogCurrentTicks( totalItemsConsidered );

            {
                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    foreach ( MapTile tile in visibleTiles )
                    {
                        if ( drawAllBeacons || drawKeyBeacons )
                        {
                            debugStage = 200;
                            foreach ( MapPOI poi in tile.AllPOIs )
                            {
                                if ( poi == null || poi.HasBeenDestroyed )
                                    continue;
                                POIType poiType = poi.Type;
                                if ( !poiType.ActsLikeBeacon )
                                    continue;

                                RenderPOIBeaconIcon( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );
                            }
                        }
                    } //end loop over visible tiles

                    debugStage = 1100;
                    if ( drawKeyPOIStatuses && SimCommon.POIsWithKeyStatus.Count > 0 )
                    {
                        debugStage = 1200;
                        foreach ( MapPOI poi in SimCommon.POIsWithKeyStatus.GetDisplayList() )
                        {
                            if ( poi == null || poi.HasBeenDestroyed )
                                continue;
                            if ( visibleTiles.Contains( poi.Tile ) )
                                RenderPOIStatusIcons( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );
                        }
                    }
                    else if ( drawAllPOIStatuses && SimCommon.POIsWithStatus.Count > 0 )
                    {
                        debugStage = 1600;
                        foreach ( MapPOI poi in SimCommon.POIsWithStatus.GetDisplayList() )
                        {
                            if ( poi == null || poi.HasBeenDestroyed )
                                continue;
                            if ( visibleTiles.Contains( poi.Tile ) )
                                RenderPOIStatusIcons( poi, poi.Tile.CellsList[0].DrawGroup_Buildings, framesPrepped );
                        }
                    }

                    debugStage = 2100;
                    if ( (SimCommon.CurrentCityLens?.ShowJobs?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowJobs ?? false) )
                    {
                        debugStage = 2200;
                        RenderAllStructureNetworkRanges( ref debugStage );
                        RenderNetworkConnections( false, ref debugStage, framesPrepped );
                    }

                    debugStage = 3100;
                    if ( Engine_HotM.SelectedActor is MachineStructure selectedStructure )
                    {
                        debugStage = 3200;
                        MapItem selectedMapItem = selectedStructure?.Building?.GetMapItem();
                        if ( selectedMapItem != null )
                        {
                            debugStage = 3300;
                            RenderJobRangeInfos( selectedStructure, false, framesPrepped );

                            DrawMapItemHighlightOutlineLarge( selectedMapItem, HighlightPass.AlwaysHappen, selectedStructure.TerritoryControlType != null, false, framesPrepped );

                            float addedYOffset = 0.1f;
                            float addedXZSize = selectedMapItem.OBBCache.GetCheapRadiusFromExtents();

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

                    debugStage = 4100;
                    Investigation currentInvestigation = SimCommon.GetEffectiveCurrentInvestigation();
                    if ( currentInvestigation == null )
                        currentInvestigation = BuildModeHandler.TargetingTerritoryControlInvestigation;
                    if ( currentInvestigation != null && currentInvestigation.Type != null )
                    {
                        debugStage = 4200;
                        Vector3 cameraLoc = CameraCurrent.CameraBodyPosition;
                        #region Investigation Highlight Drawing For All Cells

                        SharedRenderManagerData.ClearInvalidInvestigationBuildings( currentInvestigation );

                        bool allCellRings = currentInvestigation.Type.Style.ShowRingsAroundEveryCell;
                        bool isOnFinalBuildingsOfInvestigation = currentInvestigation.Type.Style.ShowAllResultsAsBeaconStyle || currentInvestigation.PossibleBuildings.Count < 4;
                        PulsingBeaconModelData beaconData = isOnFinalBuildingsOfInvestigation ?
                            PulsingBeaconModelData.CreateFromIntermittentDouble( 0.2f, 0.02f, 0.9f ) :
                            (currentInvestigation.PossibleBuildings.Count < 15 || currentInvestigation.CountByDistrict.Count == 1 ?
                            PulsingBeaconModelData.CreateFromIntermittentDouble( 0.1f, 0.7f, 0.9f ) : PulsingBeaconModelData.CreateFromIntermittentDouble( 0f, 1f, 1f ));

                        VisColorUsage investigationBeacon = currentInvestigation.Type.Style.IsTerritoryControlStyle ? ColorRefs.BuildingPartOfTerritoryControl : ColorRefs.BuildingPartOfInvestigation;
                        VisColorUsage buildingColor = currentInvestigation.Type.Style.IsTerritoryControlStyle ? ColorRefs.BuildingPartOfTerritoryControl : ColorRefs.BuildingPartOfInvestigation;

                        foreach ( KeyValuePair<ISimBuilding, bool> kv in currentInvestigation.PossibleBuildings )
                        {
                            ISimBuilding building = kv.Key;
                            if ( building == null )
                                continue;
                            MapItem item = building.GetMapItem();
                            if ( item == null )
                                continue;
                            //if ( item.LastFramePrepRendered_General < framesPrepped )
                            //    continue; //if did not render this frame, skip
                            if ( !item.ParentCell.IsConsideredInCameraView )
                                continue; //if did not render this frame, skip
                            if ( (cameraLoc - item.CenterPoint).GetSquareGroundMagnitude() > 8100 ) //90 squared
                                continue; //too far away

                            DrawMapItemHighlightedBorderPulsingBeacon( item, buildingColor.ColorHDR, false, HighlightPass.First, beaconData, framesPrepped );
                            FrameBufferManagerData.BuildingOverlayCount.Construction++;

                            if ( allCellRings )
                                SharedRenderManagerData.DrawBeaconRingIfFirstInCell( item, investigationBeacon );
                            else if ( isOnFinalBuildingsOfInvestigation )
                                SharedRenderManagerData.DrawBeaconRingOrAddToAverage( item, currentInvestigation.PossibleBuildings.Count, investigationBeacon );
                        }

                        SharedRenderManagerData.DrawAllExistingBeaconRings( investigationBeacon );
                        #endregion
                    }

                    debugStage = 7100;
                    if ( CityMap.MaterializingItems_MainThreadOnly.Count > 0 ) //do them all!
                    {
                        debugStage = 7200;
                        FrameBufferManagerData.FadingCount.Construction += CityMap.MaterializingItems_MainThreadOnly.Count;

                        for ( int i = CityMap.MaterializingItems_MainThreadOnly.Count - 1; i >= 0; i-- )
                        {
                            if ( CityMap.MaterializingItems_MainThreadOnly[i].DoPerFrame( framesPrepped ) ) //returns true when it is done
                                CityMap.MaterializingItems_MainThreadOnly.RemoveAt( i, true );
                        }
                    }

                    MachineStructure networkTower = SimCommon.TheNetwork?.Tower;
                    if ( networkTower != null && networkTower.Type != CommonRefs.NetworkTowerStructure )
                    {
                        MapItem item = networkTower?.Building?.GetMapItem();
                        if ( item != null )
                        {
                            Vector3 loc = item.OBBCache.TopCenter;

                            float distSquared = (loc - CameraCurrent.CameraBodyPosition).GetSquareGroundMagnitude();
                            float scale = IconRefs.NetworkBeacon.DefaultScale * 0.5f; //smaller when not in map mode
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

                    debugStage = 12100;
                    //loop over all city vehicles
                    ListView<ISimCityVehicle> vehicles = World.CityVehicles.GetAllCityVehicles();
                    if ( vehicles.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 12200;
                        foreach ( ISimCityVehicle vehicle in vehicles )
                        {
                            vehicle.DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, framesPrepped, out bool ShouldSkipDrawing );
                            if ( ShouldSkipDrawing )
                                continue;

                            if ( vehicle.GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingObject, out Color drawColor ) )
                            {
                                MapCell cell = vehicle.GetVisCurrentMapCell();
                                if ( cell != null && !cell.ShouldHaveExtended2xCityVehiclesRightNow )
                                    continue; //this really is too far away

                                bool drawAsDarkened = false;
                                if ( vehicle.GetCityVehicleType().TakesOnUnexploredColorWhenInUnexplored )
                                {
                                    drawAsDarkened = !VisibilityGranterCalculator.CalculateEfficientIsOutOfFogOfWar_Basic( cell.ParentTile?.FogOfWarCuttersWithinRange?.GetDisplayList(),
                                        vehicle.GetVisWorldLocation() );
                                }

                                RenderHelper_Objects.DrawCityVehicle( vehicle, floatingObject, drawColor, drawAsDarkened );
                            }
                        }
                    }

                    debugStage = 22100;
                    //loop over all machine vehicles
                    DictionaryView<int, ISimMachineVehicle> machineVehicles = World.Forces.GetMachineVehiclesByID();
                    if ( machineVehicles.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 22200;
                        foreach ( KeyValuePair<int, ISimMachineVehicle> kv in machineVehicles )
                        {
                            ISimMachineVehicle vehicle = kv.Value;
                            bool isTheSelectedUnit = vehicle == Engine_HotM.SelectedActor;

                            vehicle.DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, framesPrepped );

                            if ( vehicle.GetDataForActualObjectDraw( out IAutoPooledFloatingObject floatingObject, out Color drawColor ) )
                            {
                                MapCell cell = vehicle.GetCurrentMapCell();
                                if ( cell != null && !cell.ShouldHaveExtended2xCityVehiclesRightNow )
                                    continue; //this really is too far away

                                #region Draw The Ground Under The Vehicle
                                if ( !doGroundVisMode && !isFogOfWarDisabled )
                                {
                                    VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCylinder;
                                    A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                                    FogOfWarCutter cutter = vehicle.FogOfWarCutting;
                                    Vector3 vehicleDraw = cutter.Point;
                                    vehicleDraw.y = GROUND_DRAW_Y_MAIN;

                                    if ( CameraCurrent.TestFrustumColliderInternalFast( vehicleDraw, 1f, cutter.CutRange ) ) //frustum cull these, as they are large
                                    {
                                        float diameter = cutter.CutRange + cutter.CutRange;
                                        Vector3 scale = new Vector3( diameter, 2f, diameter );

                                        rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( vehicleDraw, Quaternion.identity, scale, RenderColorStyle.NoColor );
                                    }
                                }
                                #endregion

                                RenderHelper_Objects.DrawMachineVehicle( vehicle, floatingObject, drawColor, false, isTheSelectedUnit );
                            }

                            if ( IsMouseOver )
                            {
                                MoveHelper.TryConsiderDrawingThreatLinesAgainst( vehicle, true, ThreatLineLogic.Normal );
                            }
                        }
                    }

                    debugStage = 32100;
                    //loop over all machine units
                    DictionaryView<int, ISimMachineUnit> machineUnits = World.Forces.GetMachineUnitsByID();
                    if ( machineUnits.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 32200;
                        foreach ( KeyValuePair<int, ISimMachineUnit> kv in machineUnits )
                        {
                            ISimMachineUnit unit = kv.Value;
                            if ( unit == null || !unit.GetIsDeployed() )
                                continue;
                            RenderMachineUnitIfNeeded( unit, framesPrepped, doGroundVisMode, isFogOfWarDisabled );
                        }
                    }

                    debugStage = 32100;
                    //loop over all npc units
                    DictionaryView<int, ISimNPCUnit> npcUnits = World.Forces.GetAllNPCUnitsByID();
                    if ( npcUnits.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 32200;
                        foreach ( KeyValuePair<int, ISimNPCUnit> kv in npcUnits )
                        {
                            ISimNPCUnit unit = kv.Value;
                            if ( unit == null )
                                continue;
                            RenderNPCUnitIfNeeded( unit, framesPrepped );
                        }
                    }

                    debugStage = 42100;
                    //loop over all functional structures
                    List<MachineStructure> functionalStructures = SimCommon.CurrentFullyFunctionalStructures.GetDisplayList();
                    if ( functionalStructures.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 42200;
                        foreach ( MachineStructure structure in functionalStructures )
                        {
                            MachineStructureType structureType = structure.Type;
                            if ( structureType == null )
                                continue;
                            if ( structureType.IsTerritoryControlFlag )
                                continue;
                            if ( structure.IsFullDead || structure.IsUnderConstruction )
                                continue;
                            int scanRange = structure.GetActorDataCurrent( ActorRefs.ScanRange, true );
                            if ( scanRange <= 0 )
                                continue; //if no scan range, then don't bother

                            #region Draw The Ground Under The Subnet
                            if ( !doGroundVisMode && !isFogOfWarDisabled )
                            {
                                VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCylinder;
                                A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                                FogOfWarCutter cutter = structure.FogOfWarCutting;
                                Vector3 subnetDraw = cutter.Point;
                                subnetDraw.y = GROUND_DRAW_Y_MAIN;

                                if ( !CameraCurrent.TestFrustumColliderInternalFast( subnetDraw, 1f, cutter.CutRange ) )
                                    continue; //frustum cull these, as they are large

                                float diameter = cutter.CutRange + cutter.CutRange;
                                Vector3 scale = new Vector3( diameter, 2f, diameter );

                                rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( subnetDraw, Quaternion.identity, scale, RenderColorStyle.NoColor );
                            }
                            #endregion
                        }
                    }

                    debugStage = 52100;
                    //loop over all territory control flags
                    List<MachineStructure> flags = SimCommon.TerritoryControlFlags.GetDisplayList();
                    if ( flags.Count > 0 ) //there should not be THAT many of these, so just loop them all
                    {
                        debugStage = 52200;
                        float cutRange = MathRefs.TerritoryControlFogCutRange.FloatMin;
                        foreach ( MachineStructure flag in flags )
                        {
                            #region Draw The Ground Under The Flag
                            if ( !doGroundVisMode && !isFogOfWarDisabled )
                            {
                                VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCylinder;
                                A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                                //FogOfWarCutter cutter = subnet.FogOfWarCutting;
                                Vector3 flagDrawPoint = flag.GetGroundCenterLocation(); //cutter.Point;
                                flagDrawPoint.y = GROUND_DRAW_Y_MAIN;

                                if ( !CameraCurrent.TestFrustumColliderInternalFast( flagDrawPoint, 1f, cutRange ) )//cutter.CutRange ) )
                                    continue; //frustum cull these, as they are large

                                float diameter = cutRange + cutRange;// cutter.CutRange + cutter.CutRange;
                                Vector3 scale = new Vector3( diameter, 2f, diameter );

                                rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( flagDrawPoint, Quaternion.identity, scale, RenderColorStyle.NoColor );
                            }
                            #endregion
                        }
                    }

                    debugStage = 62100;
                    IReadOnlyList<MapCell> outOfBoundsCells = CityMap.CellsOutOfCameraView;
                    foreach ( MapCell nonVisibleCell in outOfBoundsCells )
                    {
                        debugStage = 62200;
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

                    debugStage = 72100;
                    RenderHelper_EventCamera.RenderIfNeeded();
                    debugStage = 82100;
                    RenderHelper_Objects.DrawBuildingFloorsIfNeeded();
                    debugStage = 92100;
                    A5ObjectAggregation.FloatingIconListPool.DrawAllActiveInstructions();
                    debugStage = 102100;
                    A5ObjectAggregation.FloatingIconColliderPool.DrawAllActiveItems();

                    debugStage = 112100;
                    foreach ( Lockdown lockdown in SimCommon.Lockdowns_MainThreadOnly )
                    {
                        debugStage = 112200;
                        TryDrawLockdown( lockdown );
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "PreRenderFrame-Streets-PostCells", debugStage, e, Verbosity.ShowAsError );
                }
            }

            SharedRenderManagerData.stopwatch.Stop();
        }
        #endregion

        #region DrawRoads        
        private static void DrawRoads( List<MapItem> Roads, 
            MapCellDrawGroup DrawGroup, bool DoVisualization, Color roadsColor, Color fogColor,
            Int64 framesPrepped )
        {
            int itemCount = Roads.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = Roads[it];
                if ( item == null || item.IsItemHidden || item.LastFramePrepRendered_General >= framesPrepped )
                    continue;
                if ( item.IsNonBuildingItemBurned )
                {
                    if ( item.NonSimBurnMaskOffset < 0 )
                        item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 0.1f );
                    RenderHelper_MapItems.TryDrawMapItemBurned_AlreadyValidated( item, DrawGroup, BurnedRuinsType.Road, framesPrepped );
                }
                else if ( item.IsItemInFogOfWar )
                    RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                else
                {
                    if ( DoVisualization )
                        RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, roadsColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, 0f, framesPrepped );
                    else
                        RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, roadsColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, 0f, framesPrepped );
                }
            }

            FrameBufferManagerData.RoadCount.Construction += Roads.Count;
        }
        #endregion

        #region DrawOtherSkeletonItemsOrFences        
        private static void DrawOtherSkeletonItemsOrFences( List<MapItem> Items, bool IsOutOfBounds, float extraY,
            MapCellDrawGroup DrawGroup, bool DoVisualization, Color otherColor, Color fogColor,
            Int64 framesPrepped )
        {
            int itemCount = Items.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = Items[it];
                if ( item == null || item.IsItemHidden || item.LastFramePrepRendered_General >= framesPrepped )
                    continue;
                if ( item.IsNonBuildingItemBurned )
                {
                    if ( item.NonSimBurnMaskOffset < 0 )
                    {
                        int chance = Engine_Universal.PermanentQualityRandom.Next( 0, 100 );
                        if ( chance < 70 )
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 0.1f );
                        else if ( chance < 90 )
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0.1f, 0.4f );
                        else
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0.4f, 1.6f );
                    }
                    RenderHelper_MapItems.TryDrawMapItemBurned_AlreadyValidated( item, DrawGroup, BurnedRuinsType.Other, framesPrepped );
                }
                else if ( !IsOutOfBounds //ignore fog of war and draw normally if out of bounds
                    && item.IsItemInFogOfWar )
                    RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                else
                {
                    if ( DoVisualization )
                        RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, otherColor,
                            true, //worried about sorting artifacts with walls fading in, otherwise
                            false, extraY, framesPrepped );
                    else
                        RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, otherColor,
                            true, //worried about sorting artifacts with walls fading in, otherwise
                            false, extraY, framesPrepped );
                }
            }

            FrameBufferManagerData.OtherSkeletonItemCount.Construction += Items.Count;
        }
        #endregion

        #region DrawBuildings        
        private static void DrawBuildings_Highlighted( List<MapItem> Buildings, MapCellDrawGroup DrawGroup,
            bool DoVisualization, Color buildingsColor, Color targetingColor, bool isSelectedOrHoveredMode,
            int selectedBuildingItemGlobalIndex, int targetedBuildingItemGlobalIndex, MapCell parentCell, BuildingOverlayType effectiveOverlayType,
            bool doGroundVisMode, bool isFogOfWarDisabled, bool showAllBeacons, bool showKeyBeacons, bool showStreetSense,
            Int64 framesPrepped,

            BuildingMarkerColor defaultMarkerColor, Color fogColor, bool canHighlightBuildingsAsTargets,
            bool shouldDrawJobsAsIcons, bool shouldDrawSwarms, bool shouldDrawContemplations, bool shouldDrawExplorationSites, bool shouldDrawCityConflictsLarge,
            bool shouldDrawCityConflictsSmall, bool shouldDrawSpecialtyResources,
            bool IsInPerBuildingHighlightMode, bool OverrideShowAllBuildingMarkers,

            ISimMachineActor selectedActor, Vector3 actorLoc, float actorLocationActionRange, float actorLocationActionRangeSquared )
        {
            int itemCount = Buildings.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = Buildings[it];
                if ( item.LastFramePrepRendered_General >= framesPrepped )
                    continue;

                ISimBuilding simBuilding = item?.SimBuilding;
                if ( simBuilding == null || item.IsItemHidden )
                    continue;
                BuildingTypeVariant variant = simBuilding.GetVariant();
                if ( variant == null )
                    continue;

                MachineStructure structure = simBuilding.MachineStructureInBuilding;

                if ( structure?.Type?.IsTerritoryControlFlag ?? false )
                {
                    if ( TryDrawMapItemTerritoryControl( item,
                        parentCell.DrawGroup_Buildings, false, 0f, framesPrepped ) )
                    {
                        FrameBufferManagerData.BuildingMainCount.Construction++;
                        RenderMachineStructureAtBuilding( structure, item, false, item.ParentCell.DrawGroup_Buildings, false, framesPrepped );
                    }
                    continue;
                }

                Color thisBuildingColor = buildingsColor;

                bool shouldDrawMainBuildingStyle = true;
                bool drewBuilding = false;
                bool tryDrawHighlight = false;
                if ( item.IsItemInFogOfWar && !item.Type.Building.Type.NeverShowsAsFogOfWarStyle )
                {
                    thisBuildingColor = effectiveOverlayType.Implementation.GetColorInCurrentOverlay(
                        simBuilding, effectiveOverlayType, SharedRenderManagerData.stopwatch );

                    //if we are pretty much just white, then draw the fog of war instead
                    if ( (thisBuildingColor.r >= 1f && thisBuildingColor.g >= 1f && thisBuildingColor.b >= 1f) ||
                        (thisBuildingColor.r <= 0 && thisBuildingColor.g <= 0 && thisBuildingColor.b <= 0) )
                    {
                        drewBuilding = RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                        shouldDrawMainBuildingStyle = false;
                    }
                }
                else if ( IsInPerBuildingHighlightMode )
                {
                    //if we're in highlight mode, draw the building as normal but draw ourselves a second time
                    if ( (parentCell.CurrentDistanceFromCamera <= 2 ||
                        //if it's the district we are in, then draw it always
                        parentCell.ParentTile.District == CameraCurrent.FocusDistrictOrNull) )
                    {
                        tryDrawHighlight = true;
                    }
                }
                else
                {
                    thisBuildingColor = effectiveOverlayType.Implementation.GetColorInCurrentOverlay(
                        simBuilding, effectiveOverlayType, SharedRenderManagerData.stopwatch );
                }

                if ( shouldDrawMainBuildingStyle )
                {
                    if ( !DrawGroup.DrawsWithFullOpacity )
                        thisBuildingColor.a = DrawGroup.CurrentAlphaIfFadingIn;

                    if ( canHighlightBuildingsAsTargets && targetedBuildingItemGlobalIndex == item.MapItemGlobalIndex )
                        drewBuilding = TryDrawMapItem( item, UseColorType.ColorOverride, DrawGroup, ColorRefs.MouseoverBuilding.ColorHDR,
                            RenderOpacity.Transparent_Sorted, false, 0f, framesPrepped ); //skyscrapers in particular look insane if we don't use this
                    else
                    {
                        if ( DoVisualization )
                            drewBuilding = RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, thisBuildingColor,
                                true, //skyscrapers in particular look insane if we don't use this
                                false, 0f, framesPrepped );
                        else
                            drewBuilding = RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, thisBuildingColor,
                                true, //skyscrapers in particular look insane if we don't use this
                                false, 0f, framesPrepped );
                    }
                }

                if ( drewBuilding )
                {
                    FrameBufferManagerData.BuildingMainCount.Construction++;

                    if ( showStreetSense )
                        DrawStreetSenseActionAtBuilding( item, simBuilding, selectedActor );

                    if ( tryDrawHighlight )
                    {
                        Color highlightColor = effectiveOverlayType.Implementation.GetColorInCurrentOverlay(
                            simBuilding, effectiveOverlayType, SharedRenderManagerData.stopwatch );
                        if ( highlightColor.a >= 0.25f )
                        {
                            //only draw the highlight if it would not be super transparent
                            DrawMapItemHighlighted( item, highlightColor, HighlightPass.First, framesPrepped );
                            FrameBufferManagerData.BuildingOverlayCount.Construction++;
                        }
                    }

                    if ( selectedBuildingItemGlobalIndex == item.MapItemGlobalIndex ) //draw the highlight for any selected building
                    {
                        Color selectedColor = buildingsColor * ColorMath.LightCyan;
                        DrawMapItemHighlighted( item, selectedColor, HighlightPass.First, framesPrepped );
                        FrameBufferManagerData.BuildingOverlayCount.Construction++;
                    }
                    //if ( canHighlightBuildingsAsTargets && targetedBuildingItem == item ) //draw the highlight for any targeted building
                    //{
                    //    Color selectedColor = buildingsColor * targetingColor;
                    //    DrawMapItemHighlighted( item, DrawGroup, selectedColor );
                    //}

                    if ( structure != null )
                        RenderMachineStructureAtBuilding( structure, item, false, DrawGroup, shouldDrawJobsAsIcons, framesPrepped );
                    else if ( OverrideShowAllBuildingMarkers )
                        DrawDebugBuildingMarkerOnMapItem( item, DrawGroup, defaultMarkerColor,
                            RenderOpacity.Transparent_Sorted, framesPrepped ); //it's a minor thing, but may as well

                    if ( shouldDrawSwarms )
                    {
                        Swarm swarm = simBuilding.SwarmSpread;
                        if ( swarm != null )
                            RenderSwarmAtBuilding( swarm, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawSpecialtyResources && structure == null && !simBuilding.HasSpecialResourceAlreadyBeenExtracted )
                    {
                        if ( variant.SpecialScavengeResource != null )
                            RenderSpecialResourceAtBuilding( item, null, framesPrepped );
                    }

                    if ( shouldDrawContemplations )
                    {
                        ContemplationType contemplation = simBuilding.GetCurrentContemplationThatShouldShowOnMap();
                        if ( contemplation != null )
                            RenderContemplationAtBuilding( contemplation, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawExplorationSites )
                    {
                        ExplorationSiteType explorationSite = simBuilding.GetCurrentExplorationSiteThatShouldShowOnMap();
                        if ( explorationSite != null )
                            RenderExplorationSiteAtBuilding( explorationSite, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawCityConflictsLarge || shouldDrawCityConflictsSmall )
                    {
                        CityConflict conflict = simBuilding.CurrentCityConflict?.Display;
                        if ( conflict != null )
                            RenderCityConflictAtBuilding( conflict, item, DrawGroup, shouldDrawCityConflictsLarge, framesPrepped );
                    }

                    if ( showAllBeacons )
                    {
                        BeaconType beacon = variant.BeaconToShow;
                        if ( beacon != null )
                            RenderBeaconAtBuilding( beacon, item, DrawGroup, framesPrepped );
                    }
                    else if ( showKeyBeacons )
                    {
                        BeaconType beacon = variant.BeaconToShow;
                        if ( beacon != null && beacon.IsConsideredKeyBeacon )
                            RenderBeaconAtBuilding( beacon, item, DrawGroup, framesPrepped );
                    }

                    NPCMission mission = simBuilding.Mission;
                    if ( mission != null )
                        RenderNPCMissionAtBuilding( mission );

                    LocationCalculationCache cache = simBuilding.GetLocationCalculationCache();
                    if ( cache != null )
                    {
                        if ( cache.LocationFloatingText != null && cache.RenderLocationFloatingTextUntil >= ArcenTime.AnyTimeSinceStartF )
                            cache.LocationFloatingText.MarkAsStillInUseThisFrame();
                    }
                }
            }
        }

        private static void DrawBuildings_Normal( List<MapItem> Buildings, MapCellDrawGroup DrawGroup,
            bool DoVisualization, Color buildingsColor, Color targetingColor, bool isSelectedOrHoveredMode,
            int selectedBuildingItemGlobalIndex, int targetedBuildingItemGlobalIndex, MapCell parentCell, BuildingOverlayType effectiveOverlayType,
            bool doGroundVisMode, bool isFogOfWarDisabled, bool showAllBeacons, bool showKeyBeacons, bool showStreetSense,
            Int64 framesPrepped,

            BuildingMarkerColor defaultMarkerColor, Color fogColor, bool canHighlightBuildingsAsTargets,
            bool shouldDrawJobsAsIcons, bool shouldDrawSwarms, bool shouldDrawContemplations, bool shouldDrawExplorationSites, bool shouldDrawCityConflictsLarge,
            bool shouldDrawCityConflictsSmall, bool shouldDrawSpecialtyResources,
            bool IsInPerBuildingHighlightMode, bool OverrideShowAllBuildingMarkers,

            ISimMachineActor selectedActor, Vector3 actorLoc, float actorLocationActionRange, float actorLocationActionRangeSquared )
        {
            int itemCount = Buildings.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = Buildings[it];
                if ( item.LastFramePrepRendered_General >= framesPrepped )
                    continue;

                ISimBuilding simBuilding = item?.SimBuilding;
                if ( simBuilding == null || item.IsItemHidden )
                    continue;
                BuildingTypeVariant variant = simBuilding.GetVariant();
                if ( variant == null )
                    continue;

                MachineStructure structure = simBuilding.MachineStructureInBuilding;

                if ( structure?.Type?.IsTerritoryControlFlag ?? false )
                {
                    if ( TryDrawMapItemTerritoryControl( item,
                        parentCell.DrawGroup_Buildings, false, 0f, framesPrepped ) )
                    {
                        FrameBufferManagerData.BuildingMainCount.Construction++;
                        RenderMachineStructureAtBuilding( structure, item, false, item.ParentCell.DrawGroup_Buildings, false, framesPrepped );
                    }
                    continue;
                }

                BuildingStatus status = simBuilding.GetStatus();
                bool drewBuilding = false;
                if ( status != null )
                {
                    if ( status.ShouldBuildingBeInvisible )
                        continue;
                    if ( status.ShouldBuildingBeBurnedVisually )
                    {
                        if ( item.NonSimBurnMaskOffset < 0 )
                        {
                            int chance = Engine_Universal.PermanentQualityRandom.Next( 0, 100 );
                            if ( chance < 70 )
                                item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 0.2f );
                            else if ( chance < 90 )
                                item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0.2f, 1f );
                            else if ( chance < 95 )
                                item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 1f, 3f );
                            else
                                item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 3f, 12f );
                        }
                        drewBuilding = RenderHelper_MapItems.TryDrawMapItemBurned_AlreadyValidated( item, DrawGroup, BurnedRuinsType.Building, framesPrepped );
                        continue;
                    }
                    if ( status.IsBuildingConsideredToBeUnderConstruction )
                    {
                        DrawMapItemHighlightedGhost( item, ColorRefs.BuildingUnderConstructionGhostColor.ColorHDR, false, framesPrepped );
                        DrawMapItemHighlightedFallingCubes( item, ColorRefs.BuildingUnderConstructionFallingCubesColor.ColorHDR, framesPrepped );

                        if ( structure != null )
                            RenderMachineStructureAtBuilding( structure, item, false, DrawGroup, shouldDrawJobsAsIcons, framesPrepped );

                        continue;
                    }
                }

                if ( item.IsItemInFogOfWar && !item.Type.Building.Type.NeverShowsAsFogOfWarStyle )
                {
                    drewBuilding = RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                }
                else
                {
                    if ( canHighlightBuildingsAsTargets && targetedBuildingItemGlobalIndex == item.MapItemGlobalIndex )
                        drewBuilding = TryDrawMapItem( item, UseColorType.ColorOverride, DrawGroup, ColorRefs.MouseoverBuilding.ColorHDR,
                            RenderOpacity.Transparent_Sorted, false, 0f, framesPrepped ); //skyscrapers in particular look insane if we don't use this
                    else
                    {
                        if ( DoVisualization )
                            drewBuilding = RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, buildingsColor,
                                true, //skyscrapers in particular look insane if we don't use this
                                false, 0f, framesPrepped );
                        else
                            drewBuilding = RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, buildingsColor,
                                true, //skyscrapers in particular look insane if we don't use this
                                false, 0f, framesPrepped );
                    }
                }

                if ( drewBuilding )
                {
                    FrameBufferManagerData.BuildingMainCount.Construction++;

                    if ( showStreetSense )
                        DrawStreetSenseActionAtBuilding( item, simBuilding, selectedActor );

                    if ( selectedBuildingItemGlobalIndex == item.MapItemGlobalIndex ) //draw the highlight for any selected building
                    {
                        Color selectedColor = buildingsColor * ColorRefs.SelectedBuilding.ColorHDR;
                        DrawMapItemHighlighted( item, selectedColor, HighlightPass.First, framesPrepped );
                        FrameBufferManagerData.BuildingOverlayCount.Construction++;
                    }
                    //if ( canHighlightBuildingsAsTargets && targetedBuildingItem == item ) //draw the highlight for any targeted building
                    //{
                    //    //Color selectedColor = buildingsColor * targetingColor;
                    //    //DrawMapItemHighlighted( item, DrawGroup, selectedColor );
                    //    //DrawMapItemGlassyUncolored( item, DrawGroup );
                    //}

                    if ( structure != null )
                        RenderMachineStructureAtBuilding( structure, item, false, DrawGroup, shouldDrawJobsAsIcons, framesPrepped );
                    else if ( OverrideShowAllBuildingMarkers )
                        DrawDebugBuildingMarkerOnMapItem( item, DrawGroup, defaultMarkerColor,
                            RenderOpacity.Transparent_Sorted, framesPrepped ); //it's a minor thing, but may as well

                    if ( shouldDrawSwarms )
                    {
                        Swarm swarm = simBuilding.SwarmSpread;
                        if ( swarm != null )
                            RenderSwarmAtBuilding( swarm, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawSpecialtyResources && structure == null && !simBuilding.HasSpecialResourceAlreadyBeenExtracted )
                    {
                        if ( variant.SpecialScavengeResource != null )
                            RenderSpecialResourceAtBuilding( item, null, framesPrepped );
                    }

                    if ( shouldDrawContemplations )
                    {
                        ContemplationType contemplation = simBuilding.GetCurrentContemplationThatShouldShowOnMap();
                        if ( contemplation != null )
                            RenderContemplationAtBuilding( contemplation, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawExplorationSites )
                    {
                        ExplorationSiteType explorationSite = simBuilding.GetCurrentExplorationSiteThatShouldShowOnMap();
                        if ( explorationSite != null )
                            RenderExplorationSiteAtBuilding( explorationSite, item, DrawGroup, framesPrepped );
                    }

                    if ( shouldDrawCityConflictsLarge || shouldDrawCityConflictsSmall )
                    {
                        CityConflict conflict = simBuilding.CurrentCityConflict?.Display;
                        if ( conflict != null )
                            RenderCityConflictAtBuilding( conflict, item, DrawGroup, shouldDrawCityConflictsLarge, framesPrepped );
                    }

                    if ( showAllBeacons )
                    {
                        BeaconType beacon = variant.BeaconToShow;
                        if ( beacon != null )
                            RenderBeaconAtBuilding( beacon, item, DrawGroup, framesPrepped );
                    }
                    else if ( showKeyBeacons )
                    {
                        BeaconType beacon = variant.BeaconToShow;
                        if ( beacon != null && beacon.IsConsideredKeyBeacon )
                            RenderBeaconAtBuilding( beacon, item, DrawGroup, framesPrepped );
                    }

                    NPCMission mission = simBuilding.Mission;
                    if ( mission != null )
                        RenderNPCMissionAtBuilding( mission );

                    LocationCalculationCache cache = simBuilding.GetLocationCalculationCache();
                    if ( cache != null )
                    {
                        if ( cache.LocationFloatingText != null && cache.RenderLocationFloatingTextUntil >= ArcenTime.AnyTimeSinceStartF )
                            cache.LocationFloatingText.MarkAsStillInUseThisFrame();
                    }
                }
            }
        }
        #endregion

        #region RenderMachineStructureAtBuilding        
        public static void RenderMachineStructureAtBuilding( MachineStructure structure, MapItem item, bool IsMapMode, MapCellDrawGroup DrawGroup, bool DrawAsIcon, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null || structure == null )
                return; //this should not be possible, but just in case
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            if ( structure.IsUnderConstruction || structure.IsJobStillInstalling )
                DrawMapItemHighlightedFallingCubes( item, ColorRefs.BuildingUnderConstructionFallingCubesColor.ColorHDR, framesPrepped );

            MachineJob currentJob = structure.CurrentJob;

            bool isMouseOverThisStructure = MouseHelper.StructureUnderCursor == structure;
            bool shouldDrawJobIcon = isMouseOverThisStructure || DrawAsIcon || Engine_HotM.SelectedActor == structure;

            //if ( building.CurrentOccupyingUnit != null )
            //    shouldDrawJobIcon = false; //never draw the job icon when there is a unit there

            if ( item.LastFramePrepRendered_Jobs >= framesPrepped )
                return;
            item.LastFramePrepRendered_Jobs = framesPrepped;

            bool shouldSkipIcon = false;
            if ( !isMouseOverThisStructure && currentJob != null )
            {
                if ( ( CommonRefs.MachineStructuresLens?.SkipStorageJobs?.Display ?? false ) && //note, always use this lens so that it also carries into build mode
                    currentJob.Tags.ContainsKey( CommonRefs.StorageTag.ID ) ) 
                    shouldSkipIcon = true;

                if ( (CommonRefs.MachineStructuresLens?.SkipRefineryJobs?.Display ?? false) && //note, always use this lens so that it also carries into build mode
                    currentJob.Tags.ContainsKey( CommonRefs.RefineryTag.ID ) )
                    shouldSkipIcon = true;

                if ( (CommonRefs.MachineStructuresLens?.SkipProcurementJobs?.Display ?? false) && //note, always use this lens so that it also carries into build mode
                    currentJob.Tags.ContainsKey( CommonRefs.ProcurementTag.ID ) )
                    shouldSkipIcon = true;

                if ( (CommonRefs.MachineStructuresLens?.SkipElectricalJobs?.Display ?? false) && //note, always use this lens so that it also carries into build mode
                    currentJob.Tags.ContainsKey( CommonRefs.ElectricalTag.ID ) )
                    shouldSkipIcon = true;
            }

            //bool drewIcon = false;
            if ( !shouldSkipIcon && ( InputCaching.IsInInspectMode_ShowMoreStuff || shouldDrawJobIcon ) )
            {
                IA5Sprite jobSprite = currentJob == null && structure?.Type == CommonRefs.NetworkTowerStructure ? null : structure.GetShapeIcon();
                if ( jobSprite != null )
                {
                    Vector3 loc = item.OBBCache.BottomCenter;
                    if ( IsMapMode )
                        loc.y += EXTRA_Y_IN_MAP_MODE;

                    float distSquared = (loc - CameraCurrent.CameraBodyPosition).GetSquareGroundMagnitude();
                    if ( distSquared <= InputCaching.MaxDistanceToShowJobIcons_Squared || IsMapMode )
                    {
                        float scale = currentJob != null && currentJob.IconScaleMultiplier > 0 ? 0.5f * currentJob.IconScaleMultiplier : 0.5f;
                        if ( distSquared > 100 ) //10 squared
                        {
                            float squareDistanceAbove = distSquared - 100;
                            if ( squareDistanceAbove >= 1600 ) //40 squared
                                scale *= 4f;
                            else
                                scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                        }

                        jobSprite.WriteToDrawBufferForOneFrame( true, loc, scale, currentJob == null ? CommonRefs.TerritoryControlMarkerColor.ColorHDR : currentJob.MarkerColor.ColorHDR, false, false, true );
                        //drewIcon = true;
                    }
                }
            }

            if ( !building.GetIsDestroyed() ) //don't draw these if destroyed
            {
                //always draw the marker if there is a job
                if ( currentJob != null )
                    DrawMarkerTypeOfMapItem( item, currentJob.MarkerColor, DrawGroup,
                        RenderOpacity.Transparent_Sorted, IsMapMode, framesPrepped );
            }

            if ( isMouseOverThisStructure ) //only do this for mouseover here, and do it for the selected structure elsewhere
                RenderJobRangeInfos( structure, IsMapMode, framesPrepped ); 

            if ( structure != null && ( isMouseOverThisStructure || ( SimCommon.CurrentCityLens?.ShowJobComplaints?.Display??false) ) )
            {
                float extraYForErrors = EXTRA_Y_FOR_STRUCTURE_ERRORS;
                if ( IsMapMode )
                    extraYForErrors += EXTRA_Y_IN_MAP_MODE;

                if ( structure.IsFullDead && !structure.IsBeingRebuilt )
                {
                    //show error message of not being broken
                    CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.BrokenStructure,
                        item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                }
                else if ( structure.DoesJobHaveShortageOfInternalRobotics )
                {
                    //show error message of being short internal robotics
                    CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.JobShortageOfInternalRobotics,
                        item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                }
                else if ( !structure.GetIsConnectedToNetwork() )
                {
                    //show error message of not being in a network
                    CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.OutOfNetworkRange,
                        item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                }
                else if ( structure.IsStunnedForXMoreTurns > 0 )
                {
                    //show error message of being stunned
                    CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.BuildingStun,
                        item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                }
                //else if ( !structure.DoesJobHaveEnoughElectricity )
                //{
                //    //show error message of not having enough power
                //    CursorHelper.RenderSpecificScalingIconAtSpot( false, ActorRefs.RequiredElectricity.Icon, IconRefs.NoJobOnStructure,
                //        item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                //}
                else
                {
                    Dictionary<MachineJobComplaint, int> complaints = structure.Complaints.GetDisplayDict();
                    if ( complaints.Count > 0 )
                    {
                        float complaintXOffset = MathRefs.ComplaintIcons_XMovementPer.FloatMin;
                        CursorHelper.RenderSpecificScalingIconDictionaryAtSpot( false, DictionaryView<IVisIconUsageHolder, int>.Create( complaints ), complaintXOffset,
                             item.TopCenterPoint.PlusY( extraYForErrors ), isMouseOverThisStructure );
                    }
                }
            }
        }
        #endregion

        #region RenderJobRangeInfos
        
        public static void RenderJobRangeInfos( MachineStructure structure, bool IsMapMode, Int64 framesPrepped )
        {
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            MapItem item = structure.Building?.GetMapItem();
            MachineJob currentJob = structure.CurrentJob;

            if ( currentJob != null && currentJob.GetJobEffectRange( out float JobEffectRange ) )
                DrawHelper.RenderRangeCircle( item.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                    JobEffectRange, ColorRefs.MachineJobEffectRangeBorder.ColorHDR );

            if ( structure.Type != null && structure.GetStructureNetworkRangeForTiles( out float NetworkRange ) )
                DrawHelper.RenderRangeCircle( item.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                    NetworkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );

            //draw the network uplink if it has one.  That's not guaranteed at all!
            if ( structure.LastFramePrepRendered_NetworkUplink < framesPrepped && (structure.NetworkUplink.Display != null || structure.SubnetUplink.Display != null) )
            {
                structure.LastFramePrepRendered_NetworkUplink = framesPrepped;

                if ( structure.SubnetUplink.Display != null )
                    DrawHelper.RenderLine( structure.Building.GetMapItem().OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        structure.SubnetUplink.Display.Building.GetMapItem().OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        ColorRefs.MachineNetworkSubnetConnection.ColorHDR, 1.5f );
                else if ( structure.NetworkUplink.Display != null )
                    DrawHelper.RenderLine( structure.Building.GetMapItem().OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        structure.NetworkUplink.Display.Building.GetMapItem().OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        ColorRefs.MachineNetworkUplink.ColorHDR, 1.5f );

                if ( currentJob != null && currentJob.MaxExplosionRangeOnDeath > 0 )
                {
                    float currentRange = structure.CalculateCurrentExplosionRangeOnDeath();
                    if ( currentRange > 0 )
                        DrawHelper.RenderRangeCircle( structure.GetGroundCenterLocation(), currentRange, IconRefs.JobExplosionRadius.DefaultColorHDR );
                    if ( currentRange < currentJob.MaxExplosionRangeOnDeath )
                        DrawHelper.RenderRangeCircle( structure.GetGroundCenterLocation(), currentJob.MaxExplosionRangeOnDeath, IconRefs.JobExplosionRadius.FillColorHDR );
                }
            }
        }
        #endregion

        #region RenderAllStructureNetworkRanges
        
        public static void RenderAllStructureNetworkRanges( ref int debugStage )
        {
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            if ( SharedRenderManagerData.districtHoloWallNormal == null )
            {
                SharedRenderManagerData.districtHoloWallNormal = VisSimpleDrawingObjectTable.Instance.GetRowByID( "HoloWall" );
                if ( SharedRenderManagerData.districtHoloWallNormal == null )
                    return;
            }

            A5RendererGroup rendGroupHoloWall = SharedRenderManagerData.districtHoloWallNormal.RendererGroup as A5RendererGroup;

            foreach ( MapCell cell in CityMap.CellsInCameraView )
            {
                cell.Draw_Network.RecalculateSegmentsOnMainThreadIfNeeded();
                if ( rendGroupHoloWall != null && cell.Draw_Network.NormalHoloWallSegments.Count > 0 )
                {
                    foreach ( HoloWallSegment holoWallSegment in cell.Draw_Network.NormalHoloWallSegments )
                    {
                        rendGroupHoloWall.WriteToDrawBufferForOneFrame_BasicNoColor( holoWallSegment.Pos, holoWallSegment.Rotation, holoWallScale,
                            RenderColorStyle.NoColor );
                    }
                }
            }
        }
        #endregion

        #region RenderNetworkConnections        
        public static void RenderNetworkConnections( bool IsMapMode, ref int debugStage, Int64 framesPrepped )
        {
            if ( (CommonRefs.MachineStructuresLens?.SkipNetworkingLines?.Display ?? false) ) //note, always use this lens so that it also carries into build mode
                return;

            foreach ( MachineStructure structure in SimCommon.CurrentFunctionalOrConstructingStructures.GetDisplayList() )
            {
                debugStage = 2600;
                if ( structure?.Type == null )
                    continue;

                if ( structure.LastFramePrepRendered_NetworkUplink >= framesPrepped )
                    continue;
                structure.LastFramePrepRendered_NetworkUplink = framesPrepped;

                ISimBuilding building = structure.Building;
                if ( building == null )
                    continue;
                MapItem item = building.GetMapItem();
                if ( item == null )
                    continue;

                debugStage = 2620;

                debugStage = 2710;

                //also draw the subnet uplink if it has one
                if ( structure.SubnetUplink.Display != null )
                {
                    if ( InputCaching.ShowSubnetLinks )
                    {
                        debugStage = 2810;
                        ISimBuilding subnetUplinkBuilding = structure.SubnetUplink.Display?.Building;
                        MapItem subnetUplinkItem = subnetUplinkBuilding?.GetMapItem();
                        if ( subnetUplinkItem != null )
                        {
                            debugStage = 2820;
                            DrawHelper.RenderLine( item.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            subnetUplinkItem.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            ColorRefs.MachineNetworkSubnetConnection.ColorHDR, 1.5f );
                        }
                    }
                }
                //draw the network uplink if it has one.  That's not guaranteed at all!  And ONLY do this if it is not in a subnet!
                else if ( structure.NetworkUplink.Display != null )
                {
                    if ( InputCaching.ShowNetworkLinks )
                    {
                        debugStage = 2720;
                        ISimBuilding networkUplinkBuilding = structure.NetworkUplink.Display?.Building;
                        MapItem networkUplinkItem = networkUplinkBuilding?.GetMapItem();
                        if ( networkUplinkItem != null )
                        {
                            debugStage = 273;
                            DrawHelper.RenderLine( item.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                                networkUplinkItem.OBBCache.Center.ReplaceY( IsMapMode ? EXTRA_Y_IN_MAP_MODE : 0.1f ),
                                ColorRefs.MachineNetworkUplink.ColorHDR, 1.5f );
                        }
                    }
                }
            }
        }
        #endregion

        #region RenderSwarmAtBuilding        
        public static void RenderSwarmAtBuilding( Swarm swarm, MapItem item, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            if ( swarm == null )
                return;
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene
            int swarmCount = building.SwarmSpreadCount;
            if ( swarmCount <= 0 ) 
                return;

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( swarm.Icon, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.DrawFrameStyle = false;
            cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
            cache.ColliderIcon.Sprite = swarm.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            float scaleMultiplier = isMap ? MathRefs.SwarmMapIconScale.FloatMin : MathRefs.SwarmStreetsIconScale.FloatMin;

            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = swarm.IconScale * scaleMultiplier;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = swarm.ColorHoveredHDR;
                swarm.RenderSwarmTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, swarmCount );
            }
            else
            {
                cache.ColliderIcon.Color = swarm.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderContemplationAtBuilding        
        public static void RenderContemplationAtBuilding( ContemplationType contemplation, MapItem item, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( contemplation.Icon, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.DrawFrameStyle = false;
            cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
            cache.ColliderIcon.Sprite = contemplation.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = contemplation.IconScale * MathRefs.ContemplationIconScale.FloatMin;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = contemplation.ColorHoveredHDR;
                contemplation.RenderContemplationTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, false );
            }
            else
            {
                cache.ColliderIcon.Color = contemplation.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderExplorationSiteAtBuilding        
        public static void RenderExplorationSiteAtBuilding( ExplorationSiteType explorationSite, MapItem item, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( explorationSite.Icon, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.DrawFrameStyle = false;
            cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
            cache.ColliderIcon.Sprite = explorationSite.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = explorationSite.IconScale * MathRefs.ExplorationSiteIconScale.FloatMin;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = explorationSite.ColorHoveredHDR;
                explorationSite.RenderExplorationSiteTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false );
            }
            else
            {
                cache.ColliderIcon.Color = explorationSite.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderCityConflictAtBuilding        
        public static void RenderCityConflictAtBuilding( CityConflict conflict, MapItem item, MapCellDrawGroup DrawGroup, bool DrawLarge, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            if ( conflict.DuringGameplay_State != CityConflictState.Active )
                return; //don't show if no longer active

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( conflict.Icon, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.DrawFrameStyle = false;
            cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
            cache.ColliderIcon.Sprite = conflict.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap ||
                cache.lastColliderIconWasLarge != DrawLarge )
            {
                float scale = conflict.IconScale * MathRefs.ContemplationIconScale.FloatMin;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };

                if ( !DrawLarge )
                    scale *= 0.3f;

                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
                cache.lastColliderIconWasLarge = DrawLarge;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = conflict.ColorHoveredHDR;

                conflict.RenderCityConflictTooltip( null, SideClamp.Any, true );
            }
            else
            {
                cache.ColliderIcon.Color = conflict.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region DrawStreetSenseActionAtBuilding
        
        public static void DrawStreetSenseActionAtBuilding( MapItem Item, ISimBuilding building, ISimMachineActor selectedActor )
        {
            if ( Engine_HotM.IsBigBannerBeingShown || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General )
                return;

            StreetSenseDataAtBuilding streetSenseDataOrNull = building.GetCurrentStreetSenseActionThatShouldShowOnMap();
            LocationActionType actionToTake = streetSenseDataOrNull?.ActionType;
            if ( actionToTake == null )
                return;

            NPCEvent eventOrNull = streetSenseDataOrNull?.EventOrNull;
            string actionToTakeOptionalID = streetSenseDataOrNull?.OtherOptionalID;

            //render action icons if there should be any
            if ( actionToTake.Implementation.TryHandleLocationAction( selectedActor, building, Item.OBBCache.Center,
                actionToTake, eventOrNull, streetSenseDataOrNull?.ProjectItemOrNull, actionToTakeOptionalID, ActionLogic.PredictIcons, out _, 0 ) == ActionResult.Blocked )
            {
            }
        }
        #endregion

        #region RenderSimpleStreetSenseIconAtBuilding
        
        public static void RenderSimpleStreetSenseIconAtBuilding( LocationActionType Action, MapItem item, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( Action.Icon, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
            cache.ColliderIcon.DrawFrameStyle = false;
            cache.ColliderIcon.Sprite = Action.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = Action.IconScale * ( isMap ? MathRefs.StreetSenseMapIconScale.FloatMin :MathRefs.StreetSenseStreetsIconScale.FloatMin );
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = Action.ColorHoveredHDR;
            }
            else
            {
                cache.ColliderIcon.Color = Action.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderFrameStyleStreetSenseIconAtBuilding
        
        public static void RenderFrameStyleStreetSenseIconAtBuilding( LocationActionType Action, MapItem item, IA5Sprite SpriteToDraw, 
            VisIconUsage Usage, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( SpriteToDraw, building );
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.DrawFrameStyle = true;
            cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
            cache.ColliderIcon.FrameColRow = Usage.FrameMaskRowCol;
            cache.ColliderIcon.Sprite = SpriteToDraw;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap ||
                cache.lastColliderVisIconUsage != Usage )
            {
                float scale = Usage.DefaultScale * (isMap ? MathRefs.StreetSenseMapIconScale.FloatMin : MathRefs.StreetSenseStreetsIconScale.FloatMin);
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
                cache.lastColliderVisIconUsage = Usage;
            }

            Color mainColor = Usage.DefaultColorHDR;
            Color frameColor = Usage.FrameColorHDR;
            //Color fillColor = Usage.FillColorHDR;
            if ( cache.ColliderIcon.IsMouseover )
            {
                mainColor = Usage.HoverDefaultColorHDR;
                frameColor = Usage.HoverFrameColorHDR;
                //fillColor = Usage.HoverFillColorHDR;
            }

            cache.ColliderIcon.Color = mainColor;
            cache.ColliderIcon.FrameColor = frameColor;
            //cache.ColliderIcon.BackingColor = fillColor;

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderMinorEventStreetSenseIconAtBuilding
        
        public static void RenderMinorEventStreetSenseIconAtBuilding( LocationActionType Action, MapItem item, NPCEvent minorEvent, 
            bool isBlockedAtAll, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( minorEvent.Icon, building );
                cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.DrawFrameStyle = false;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.Sprite = minorEvent.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = minorEvent.MinorData.IconScale * (isMap ? MathRefs.StreetSenseMapIconScale.FloatMin : MathRefs.StreetSenseStreetsIconScale.FloatMin);
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = isBlockedAtAll ? minorEvent.MinorData.ColorData.BlockedColorHoveredHDR : minorEvent.MinorData.ColorData.ColorHoveredHDR;
            }
            else
            {
                cache.ColliderIcon.Color = isBlockedAtAll ? minorEvent.MinorData.ColorData.BlockedColorHDR : minorEvent.MinorData.ColorData.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderProjectStreetItemStreetSenseIconAtBuilding
        
        public static void RenderProjectStreetItemStreetSenseIconAtBuilding( LocationActionType Action, MapItem item, ProjectOutcomeStreetSenseItem projectStreetItem, 
            bool isBlockedAtAll, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( projectStreetItem.Icon, building );
                cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.DrawFrameStyle = false;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.Sprite = projectStreetItem.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = projectStreetItem.IconScale * (isMap ? MathRefs.StreetSenseMapIconScale.FloatMin : MathRefs.StreetSenseStreetsIconScale.FloatMin);
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = isBlockedAtAll ? projectStreetItem.ColorData.BlockedColorHoveredHDR : projectStreetItem.ColorData.ColorHoveredHDR;
            }
            else
            {
                cache.ColliderIcon.Color = isBlockedAtAll ? projectStreetItem.ColorData.BlockedColorHDR : projectStreetItem.ColorData.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderSpecialResourceAtBuilding        
        public static void RenderSpecialResourceAtBuilding( MapItem item, Action OnHoverOrNull, Int64 framesPrepped )
        {
            ISimBuilding building = item.SimBuilding;
            if ( building == null || building.HasSpecialResourceAlreadyBeenExtracted )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            BuildingTypeVariant variant = building.GetVariant();
            if ( variant == null || variant.SpecialScavengeResource == null )
                return; //definitely is frequently possible

            if ( !variant.SpecialScavengeResource.DuringGame_IsUnlocked() )
                return;

            if ( SimCommon.CurrentScavengingCollection != null && !variant.SpecialScavengeResource.ScavengingCollections.ContainsKey( SimCommon.CurrentScavengingCollection ) )
                return;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene
            if ( !FlagRefs.PropertyDamage.DuringGameplay_IsInvented )
                return; //hide if not yet able to extract

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            ResourceType scavengeResource = variant.SpecialScavengeResource;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( scavengeResource.Icon, building );
                cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.DrawFrameStyle = false;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.Sprite = scavengeResource.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = scavengeResource.ScavengeIconUsage.DefaultScale;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = scavengeResource.ScavengeIconUsage.HoverDefaultColorHDR;

                ActivityDangerType dangerType = scavengeResource == null ? null : variant.SpecialScavengeDangerType;
                IntRange amountRange = scavengeResource == null ? new IntRange() : variant.SpecialScavengeResourceAmountRange;

                LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                if ( lowerLeft.TryStartBasicTooltip( TooltipID.Create( building ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                {
                    lowerLeft.Icon = scavengeResource.Icon;
                    lowerLeft.IconColorHex = scavengeResource.IconColorHex;
                    lowerLeft.CanExpand = CanExpandType.Longer;

                    lowerLeft.TitleUpperLeft.AddRawAndAfterLineItemHeader( scavengeResource.GetDisplayName() ).AddFormat2( "MinMaxRange", amountRange.Min, amountRange.Max );
                    lowerLeft.TitleLowerLeft.AddLang( "ScavengingSite_Subheader" );

                    if ( scavengeResource.GetDescription().Length > 0 )
                        lowerLeft.Main.AddRaw( scavengeResource.GetDescription(), ColorTheme.NarrativeColor ).Line();

                    if ( InputCaching.ShouldShowDetailedTooltips )
                    {
                        if ( scavengeResource.StrategyTip.Text.Length > 0 )
                            lowerLeft.Main.AddRaw( scavengeResource.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                    }

                    lowerLeft.Main.AddBoldLangAndAfterLineItemHeader( "Move_DangerType", ColorTheme.DataLabelWhite )
                        .AddRaw( dangerType.GetDisplayName(), ColorTheme.DataBlue ).HyphenSeparator().AddRaw( variant.GetDisplayName(), ColorTheme.DataBlue );
                }

                if ( OnHoverOrNull != null )
                    OnHoverOrNull();
            }
            else
            {
                cache.ColliderIcon.Color = scavengeResource.ScavengeIconUsage.DefaultColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderBeaconAtBuilding
        
        public static void RenderBeaconAtBuilding( BeaconType beacon, MapItem item, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            ISimBuilding building = item?.SimBuilding;
            if ( building == null || beacon == null )
                return; //this should not be possible, but just in case
            LocationCalculationCache cache = building.GetLocationCalculationCache();
            if ( cache == null )
                return; //again should not be possible

            if ( cache.LastFramePrepRendered_ColliderIcon >= framesPrepped )
                return;
            cache.LastFramePrepRendered_ColliderIcon = framesPrepped;

            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            Vector3 effectivePoint = item.CenterPoint;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                Engine_HotM.GameMode == MainGameMode.Streets )
                return;

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( building ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( beacon.Icon, building );
                cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.DrawFrameStyle = false;
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.Sprite = beacon.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = beacon.IconScale;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = beacon.ColorHoveredHDR;

                LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                if ( lowerLeft.TryStartBasicTooltip( TooltipID.Create( beacon ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                {
                    lowerLeft.Icon = beacon.Icon;
                    lowerLeft.IconColorHex = beacon.ColorHex;

                    lowerLeft.TitleUpperLeft.AddRaw( beacon.GetDisplayName() );
                    lowerLeft.TitleLowerLeft.AddLang( "Beacon_Subheader" );


                    if ( beacon.GetDescription().Length > 0 )
                        lowerLeft.Main.AddRaw( beacon.GetDescription(), ColorTheme.NarrativeColor ).Line();
                    if ( beacon.StrategyTip.Text.Length > 0 )
                        lowerLeft.Main.AddRaw( beacon.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                }
            }
            else
            {
                cache.ColliderIcon.Color = beacon.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();
        }
        #endregion

        #region RenderPOIBeaconIcon
        
        public static void RenderPOIBeaconIcon( MapPOI poi, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            if ( poi == null )
                return; //this should not be possible, but just in case

            if ( poi.LastFramePrepRendered_BeaconPart >= framesPrepped )
                return;
            poi.LastFramePrepRendered_BeaconPart = framesPrepped;

            POIType poiType = poi.Type;
            if ( poiType == null )
                return; //this should not be possible, but just in case
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            LocationCalculationCache cache = poi.LocationCalculationCache;
            if ( cache == null )
                return; //again should not be possible

            Vector3 effectivePoint = poi.GetCenter();

            bool isStreets = Engine_HotM.GameMode == MainGameMode.Streets;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 25600 && //160 squared
                isStreets )
                return; //could have also used InputCaching.MaxDistanceToShowMinorIcons_Squared

            if ( isStreets && poi.BuildingOrNull == null )
                effectivePoint = effectivePoint.PlusY( 4f );
            effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that

            if ( cache.ColliderIcon != null && cache.ColliderIcon.GetIsValidToUse( poi ) )
            { } //already exists, so just update it
            else
            {
                //does not already exist, so establish it
                cache.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( poiType.Icon, poi );
                cache.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                cache.ColliderIcon.IsForOverlayCamera = true;
                cache.ColliderIcon.WorldLocation = effectivePoint;
                cache.ColliderIcon.DrawFrameStyle = false;
                cache.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                cache.lastColliderIconDistance = -1f; //reset the below
            }

            cache.ColliderIcon.Sprite = poiType.Icon;

            bool isMap = Engine_HotM.GameMode == MainGameMode.CityMap;
            if ( MathA.Abs( cache.lastColliderIconDistance - squareDistanceFromCamera ) > 10f || cache.lastColliderIconWasMap != isMap )
            {
                float scale = poiType.IconScale;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scale *= 4f;
                    else
                        scale *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                };
                cache.ColliderIcon.ObjectScale = scale;
                cache.lastColliderIconDistance = squareDistanceFromCamera;
                cache.lastColliderIconWasMap = isMap;
            }

            if ( cache.ColliderIcon.IsMouseover )
            {
                cache.ColliderIcon.Color = poiType.ColorHoveredHDR;

                poi.RenderPOITooltip( null, SideClamp.Any, TooltipShadowStyle.None, false );
            }
            else
            {
                cache.ColliderIcon.Color = poiType.ColorHDR;
            }

            cache.ColliderIcon.MarkAsStillInUseThisFrame();

            RenderPOIStatusIcons( poi, DrawGroup, framesPrepped );
        }
        #endregion

        #region RenderPOIStatusIcons
        
        public static void RenderPOIStatusIcons( MapPOI poi, MapCellDrawGroup DrawGroup, Int64 framesPrepped )
        {
            if ( poi == null )
                return; //this should not be possible, but just in case

            if ( poi.LastFramePrepRendered_StatusPart >= framesPrepped )
                return;
            poi.LastFramePrepRendered_StatusPart = framesPrepped;

            POIType poiType = poi.Type;
            if ( poiType == null )
                return; //this should not be possible, but just in case
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            if ( poi.StatusesOverTime.Count == 0 )
            {
                if ( poi.StatusDrawStates.Count > 0 )
                    poi.StatusDrawStates.Clear();
                return;
            }

            Vector3 effectivePoint = poi.GetCenter();

            bool isStreets = Engine_HotM.GameMode == MainGameMode.Streets;

            //but only render the icon if we are close enough
            float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - effectivePoint).sqrMagnitude;
            if ( squareDistanceFromCamera > 25600 && //160 squared
                isStreets )
                return; //could have also used InputCaching.MaxDistanceToShowMinorIcons_Squared

            if ( isStreets && poi.BuildingOrNull == null )
                effectivePoint = effectivePoint.PlusY( 4f );
            effectivePoint = effectivePoint.PlusY( 1.5f ); //raise this up if there is a job or unit in the way -- actually, just always do that

            {
                float extraYForErrors = EXTRA_Y_FOR_STRUCTURE_ERRORS;
                if ( !isStreets )
                    extraYForErrors += EXTRA_Y_IN_MAP_MODE;

                effectivePoint = effectivePoint.PlusY( extraYForErrors ); //inverted

                float complaintXOffset = MathRefs.ComplaintIcons_XMovementPer.FloatMin;
                //CursorHelper.RenderSpecificScalingIconDictionaryAtSpot( false, DictionaryView<ICustomIconHolder, int>.Create( poi.StatusesOverTime ), complaintXOffset,
                //     effectivePoint.PlusY( extraYForErrors ), isMouseOver );

                float scaleMultiplier = 1f;
                if ( squareDistanceFromCamera > 100 ) //10 squared
                {
                    float squareDistanceAbove = squareDistanceFromCamera - 100;
                    if ( squareDistanceAbove >= 1600 ) //40 squared
                        scaleMultiplier *= 4f;
                    else
                        scaleMultiplier *= (1 + ((squareDistanceAbove / 1600f) * 3f));
                }

                int countToActuallyDraw = poi.StatusesOverTime.Count;
                Vector3 pos = effectivePoint;
                Vector3 xMovementPer = (CameraCurrent.CameraRight * (complaintXOffset * scaleMultiplier));
                if ( countToActuallyDraw > 1 )
                    pos -= (xMovementPer * countToActuallyDraw * 0.5f);

                int index = 0;
                foreach ( KeyValuePair<POIStatus, int> kv in poi.StatusesOverTime.GetDisplayDict() )
                {
                    POIStatusToDraw drawState;
                    bool needsToEstablish = true;
                    if ( poi.StatusDrawStates.Count > index )
                    {
                        drawState = poi.StatusDrawStates[index];
                        if ( drawState.ColliderIcon != null && drawState.ColliderIcon.GetIsValidToUse( drawState ) )
                            needsToEstablish = false;//hooray, nothing more needed
                        else
                        {
                            drawState.Status = kv.Key;
                            drawState.ColliderIcon = null;
                            drawState.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( kv.Key.Icon, drawState );
                            poi.StatusDrawStates[index] = drawState;
                        }
                    }
                    else
                    {
                        drawState.Status = kv.Key;
                        drawState.ColliderIcon = null;
                        drawState.ColliderIcon = A5ObjectAggregation.FloatingIconColliderPool.GetFromPool( kv.Key.Icon, drawState );
                        poi.StatusDrawStates.Add( drawState );
                    }

                    if ( needsToEstablish )
                    {
                        drawState.ColliderIcon.CollisionLayer = CollisionLayers.IconOverlay;
                        drawState.ColliderIcon.IsForOverlayCamera = true;
                        drawState.ColliderIcon.DrawFrameStyle = false;
                        drawState.ColliderIcon.FrameColRow = IconRefs.BlankIconBacking;
                    }

                    float finalScale = kv.Key.GetIconScale() * scaleMultiplier;
                    drawState.ColliderIcon.WorldLocation = pos;
                    drawState.ColliderIcon.Sprite = kv.Key.Icon;
                    drawState.ColliderIcon.ObjectScale = finalScale;

                    if ( drawState.ColliderIcon.IsMouseover )
                    {
                        drawState.ColliderIcon.Color = kv.Key.GetColorHoveredHDR();

                        poi.RenderPOITooltip( null, SideClamp.Any, TooltipShadowStyle.None, false );

                        //this is the poi status
                        NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                        if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( "POI", poi.POIID, 0 ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            atMouse.Icon = kv.Key.Icon;
                            atMouse.CanExpand = !kv.Key.GetDescription().IsEmpty() ? CanExpandType.Brief : CanExpandType.No;

                            atMouse.TitleUpperLeft.AddRaw( kv.Key.GetDisplayName() );
                            if ( InputCaching.ShouldShowDetailedTooltips )
                            {
                                if ( !kv.Key.GetDescription().IsEmpty() )
                                    atMouse.Main.AddRaw( kv.Key.GetDescription() ).Line();
                            }
                        }
                    }
                    else
                    {
                        drawState.ColliderIcon.Color = kv.Key.GetColorHDR();
                    }

                    drawState.ColliderIcon.MarkAsStillInUseThisFrame();

                    pos += xMovementPer;
                    index++;
                }

                //clean up any excess
                while ( poi.StatusDrawStates.Count > poi.StatusesOverTime.Count )
                    poi.StatusDrawStates.RemoveAt( poi.StatusDrawStates.Count - 1 );
            }
        }
        #endregion

        public static void RenderNPCMissionAtBuilding( NPCMission mission )
        {
            if ( VisCurrent.GetShouldBeBlurred() )
                return; //hide when in a blurred scene

            mission.HandleStartingBuildingFloatingIcon();
        }

        #region RenderMachineUnitIfNeeded
        
        private static void RenderMachineUnitIfNeeded( ISimMachineUnit unit, Int64 renderManagerFramesPrepped, bool doGroundVisMode, bool isFogOfWarDisabled )
        {
            bool isTheSelectedUnit = unit == Engine_HotM.SelectedActor;

            unit.DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, renderManagerFramesPrepped,
                out bool TooltipShouldBeAtCursor, out bool ShouldSkipDrawing, out bool ShouldDrawAsSilhouette );
            if ( ShouldSkipDrawing )
                return;

            if ( unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject, out Color drawColor ) )
            {
                RenderHelper_Objects.DrawMachineUnit( unit, floatingLODObject, drawColor, isTheSelectedUnit, IsMouseOver, unit.GetIsBlockedFromActingInGeneral() );
            }

            if ( isTheSelectedUnit && unit.GetIsCurrentStanceInvalid() && !unit.GetIsBlockedFromActingInGeneral() )
            {
                if ( !Window_ActorStanceChange.Instance.IsOpen )
                {
                    Window_ActorCustomizeLoadout.Instance.Close( WindowCloseReason.UserDirectRequest );

                    Window_ActorStanceChange.Instance.ActorToEdit = unit;
                    Window_ActorStanceChange.Instance.Open();
                }
            }

            //if ( ShouldDrawWireframeOfBuilding )
            //{
            //    RenderHelper_Objects.DrawThisBuildingMachineUnitIsIn = item;
            //    RenderHelper_Objects.DrawFloorForThisMachineUnit = unit;
            //}

            if ( IsMouseOver )
            {
                MoveHelper.TryConsiderDrawingThreatLinesAgainst( unit, true, ThreatLineLogic.Normal );
            }

            #region Draw The Ground Under The Unit
            if ( !doGroundVisMode && !isFogOfWarDisabled )
            {
                FogOfWarCutter cutter = unit.FogOfWarCutting;
                if ( cutter.CutRangeSquared < 1f )
                    return;

                VisSimpleDrawingObject groundToDraw = CommonRefs.GroundCylinder;
                A5RendererGroup rGroup = groundToDraw.RendererGroup as A5RendererGroup;
                Vector3 unitDraw = cutter.Point;
                unitDraw.y = GROUND_DRAW_Y_MAIN;

                if ( CameraCurrent.TestFrustumColliderInternalFast( unitDraw, 1f, cutter.CutRange ) )  //frustum cull these, as they are large
                {
                    float diameter = cutter.CutRange + cutter.CutRange;
                    Vector3 scale = new Vector3( diameter, 2f, diameter );

                    rGroup.WriteToDrawBufferForOneFrame_BasicNoColor( unitDraw, Quaternion.identity, scale, RenderColorStyle.NoColor );
                }
            }
            #endregion
        }
        #endregion

        #region RenderNPCUnitIfNeeded
        
        private static void RenderNPCUnitIfNeeded( ISimNPCUnit unit, Int64 renderManagerFramesPrepped )
        {
            bool isTheSelectedUnit = unit == Engine_HotM.SelectedActor;

            unit.DoPerFrameDrawBecauseExistsInOrOutOfCameraView( out bool IsMouseOver, renderManagerFramesPrepped,
                out bool TooltipShouldBeAtCursor, out bool ShouldSkipDrawing, out bool ShouldDrawAsSilhouette );
            if ( ShouldSkipDrawing )
                return;

            if ( unit.GetDataForActualObjectDraw( out IAutoPooledFloatingLODObject floatingLODObject,
                out IAutoPooledFloatingObject floatingSimpleObject, out Color drawColor ) )
            {
                if ( floatingLODObject != null )
                    RenderHelper_Objects.DrawNPCUnit_LOD( unit, floatingLODObject, drawColor, isTheSelectedUnit, IsMouseOver, ShouldDrawAsSilhouette );
                else
                    RenderHelper_Objects.DrawNPCUnit_Simple( unit, floatingSimpleObject, drawColor, isTheSelectedUnit, IsMouseOver, ShouldDrawAsSilhouette );
            }

            //if ( IsMouseOver )
            //{
            //    MoveHelper.TryConsiderDrawingThreatLinesAgainst( unit, true, true, ThreatLineLogic.Normal );
            //}
        }
        #endregion

        #region DrawMajorDecorations        
        private static void DrawMajorDecorations( List<MapItem> MajorDecorations, bool IsOutOfBounds, float extraY,
            MapCellDrawGroup DrawGroup, bool DoVisualization, Color majorDecColor, Color fogColor, Int64 framesPrepped )
        {
            int itemCount = MajorDecorations.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = MajorDecorations[it];
                if ( item == null || item.IsItemHidden || item.LastFramePrepRendered_General >= framesPrepped )
                    continue;
                if ( item.IsNonBuildingItemBurned )
                {
                    if ( item.NonSimBurnMaskOffset < 0 )
                    {
                        int chance = Engine_Universal.PermanentQualityRandom.Next( 0, 100 );
                        if ( chance < 70 )
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 0.2f );
                        else if ( chance < 90 )
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0.2f, 0.8f );
                        else
                            item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0.8f, 2f );
                    }
                    RenderHelper_MapItems.TryDrawMapItemBurned_AlreadyValidated( item, DrawGroup, BurnedRuinsType.Other, framesPrepped );
                }
                else if ( !IsOutOfBounds //ignore fog of war and draw normally if out of bounds
                    && item.IsItemInFogOfWar )
                    RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                else
                {
                    if ( DoVisualization )
                        RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, majorDecColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, extraY, framesPrepped );
                    else
                        RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, majorDecColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, extraY, framesPrepped );
                }
            }

            FrameBufferManagerData.MajorDecorationCount.Construction += MajorDecorations.Count;
        }
        #endregion

        #region DrawMinorDecorations
        private static void DrawMinorDecorations( List<MapItem> MinorDecorations, bool IsOutOfBounds, float extraY,
            MapCellDrawGroup DrawGroup, bool DoVisualization, Color minorDecColor, Color fogColor, Int64 framesPrepped )
        {
            int itemCount = MinorDecorations.Count;
            totalItemsConsidered += itemCount;
            for ( int it = 0; it < itemCount; it++ )
            {
                MapItem item = MinorDecorations[it];
                if ( item == null || item.IsItemHidden || item.LastFramePrepRendered_General >= framesPrepped )
                    continue;
                if ( item.IsNonBuildingItemBurned )
                {
                    if ( item.NonSimBurnMaskOffset < 0 )
                        item.NonSimBurnMaskOffset = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 0.1f );
                    RenderHelper_MapItems.TryDrawMapItemBurned_AlreadyValidated( item, DrawGroup, BurnedRuinsType.Decoration, framesPrepped );
                }
                else if ( !IsOutOfBounds //ignore fog of war and draw normally if out of bounds
                    && item.IsItemInFogOfWar )
                    RenderHelper_MapItems.TryDrawMapItemFogOfWar_AlreadyValidated( item, DrawGroup, framesPrepped, fogColor );
                else
                {
                    if ( DoVisualization )
                        RenderHelper_MapItems.TryDrawMapItemVisualization_AlreadyValidated( item, DrawGroup, minorDecColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, extraY, framesPrepped );
                    else
                        RenderHelper_MapItems.TryDrawMapItemSimple_AlreadyValidated( item, DrawGroup, minorDecColor,
                            false, //not very worried about bad overlaps, all is so low
                            false, extraY, framesPrepped );
                }
            }

            FrameBufferManagerData.MinorDecorationCount.Construction += MinorDecorations.Count;
        }
        #endregion

        #region TryDrawMapItem        
        internal static bool TryDrawMapItem( MapItem item, UseColorType ColorType, MapCellDrawGroup DrawGroupOrNull, Color ColorForVis,
            RenderOpacity OpacityToUseIfTransparencyIsNeeded, bool IsMapMode, float extraY, Int64 framesPrepped )
        {
            if ( item == null )
                return false;

            //if ( item.Type.AnimatesAndSoAlwaysIsGameObject && item.PhysicalPlaceableObjectDuringGameplay != null && ColorType == UseColorType.Normal )
            //{
            //    FrameBufferManagerData.AnimatedObjectCount.Construction++;
            //    return true; //this happens for things that are meant to draw directly because they have animators
            //}

            if ( item.LastFramePrepRendered_General >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_General = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.RiseSpeed > 0 )
            {
                if ( item.NonSimDrawOffset < 0 )
                {
                    item.NonSimDrawOffset += ArcenTime.UnpausedDeltaTime * item.RiseSpeed;
                    if ( item.NonSimDrawOffset >= 0 )
                    {
                        item.NonSimDrawOffset = -10000;
                        item.RiseSpeed = -1;
                        if ( item.AfterRiseComplete != null )
                        {
                            item.AfterRiseComplete.DuringGame_PlayAtLocation( item.CenterPoint );
                            item.AfterRiseComplete = null;
                        }
                    }
                    else
                        extraY += item.NonSimDrawOffset;
                }
            }

            RenderColorStyle colorStyle = RenderColorStyle.NoColor;
            RenderOpacity opacity = (DrawGroupOrNull?.DrawsWithFullOpacity??true) ? RenderOpacity.Normal : OpacityToUseIfTransparencyIsNeeded;
            switch (ColorType)
            {
                case UseColorType.VisColor:
                    colorStyle = RenderColorStyle.VisColor;
                    break;
                case UseColorType.MapUnexploredColor:
                    colorStyle = RenderColorStyle.MapModeColor;
                    opacity = RenderOpacity.Normal;
                    ColorForVis = ColorRefs.MapUnexploredCell.ColorWithoutHDR;
                    break;
                case UseColorType.MapExploredAndVisionColor:
                    colorStyle = RenderColorStyle.MapModeColor;
                    opacity = RenderOpacity.Normal;
                    ColorForVis = ColorRefs.MapExploredAndVisionCell.ColorWithoutHDR;
                    break;
                case UseColorType.MapExploredNoVisionColor:
                    colorStyle = RenderColorStyle.MapModeColor;
                    opacity = RenderOpacity.Normal;
                    ColorForVis = ColorRefs.MapExploredNoVisionCell.ColorWithoutHDR;
                    break;
                case UseColorType.ColorOverride:
                    colorStyle = RenderColorStyle.SelfColor;
                    break;
            }

            MatrixCache matrixCache = IsMapMode ? item.OBBCache.Map_MatrixCache : item.OBBCache.Normal_MatrixCache;

            Matrix4x4 parentMatrix;
            if ( matrixCache.HasMatrixBeenSet && !primaryRend.Rotates && !IsMapMode && extraY == 0 )
            {
                parentMatrix = matrixCache.MatrixForRendering;
                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( parentMatrix, colorStyle, ColorForVis, opacity, false );
            }
            else
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;
                pos.y += extraY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, item.Scale, colorStyle, ColorForVis, opacity, false );
                if ( !primaryRend.Rotates )
                {
                    matrixCache.MatrixForRendering = parentMatrix;
                    matrixCache.HasMatrixBeenSet = true;
                }
            }

            List<SecondaryRenderer> secondaries = item.Type.SecondarysRenderersOfThisRoot;
            if ( secondaries.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < secondaries.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = secondaries[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, colorStyle, ColorForVis, opacity, false );
                }
            }
            return true;
        }
        #endregion

        #region TryDrawMapItemTerritoryControl        
        internal static bool TryDrawMapItemTerritoryControl( MapItem item, MapCellDrawGroup DrawGroupOrNull, bool IsMapMode, float extraY, Int64 framesPrepped )
        {
            if ( item == null )
                return false;

            if ( item.LastFramePrepRendered_General >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return false;
            }
            item.LastFramePrepRendered_General = framesPrepped;

            MachineStructure territoryControlStructure = item?.SimBuilding?.MachineStructureInBuilding;
            if ( territoryControlStructure == null )
                return false;

            TerritoryControlType territoryControlType = territoryControlStructure.TerritoryControlType;

            ISimBuilding territoryControlLink = territoryControlStructure.LinkedToForTerritoryControl.Get();

            territoryControlStructure.GetTerritoryControlPercentageAndColorToShow( out float flagPercent, out Color flagControlColor );

            float overallScale = IsMapMode ? MathRefs.TerritoryControlFlagScaleCityMap.FloatMin : MathRefs.TerritoryControlFlagScaleNormal.FloatMin;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return false;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return false;

            if ( item.RiseSpeed > 0 )
            {
                if ( item.NonSimDrawOffset < 0 )
                {
                    item.NonSimDrawOffset += ArcenTime.UnpausedDeltaTime * item.RiseSpeed;
                    if ( item.NonSimDrawOffset >= 0 )
                    {
                        item.NonSimDrawOffset = -10000;
                        item.RiseSpeed = -1;
                        if ( item.AfterRiseComplete != null )
                        {
                            item.AfterRiseComplete.DuringGame_PlayAtLocation( item.CenterPoint );
                            item.AfterRiseComplete = null;
                        }
                    }
                    else
                        extraY += item.NonSimDrawOffset;
                }
            }

            if ( item.RawTransformOfCollider )
            {
                if ( item.lastScaleMultiplier != overallScale )
                {
                    item.lastScaleMultiplier = overallScale;
                    item.RawTransformOfCollider.localScale = item.Scale * overallScale;
                }
            }


            Vector3 pos = item.rawReadPos;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;
                pos.y += extraY;

                rendGroup.WriteToDrawBufferForOneFrame_SliderColor( pos, rot, item.Scale * overallScale, flagControlColor, flagPercent );
            }

            if ( !IsMapMode )
            {
                if ( territoryControlLink != null )
                {
                    //draw a line from the building to the flag
                    DrawHelper.RenderLine( territoryControlLink.GetMapItem().OBBCache.Center.ReplaceY( IsMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        item.rawReadPos.ReplaceY( IsMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        ColorRefs.BuildingPartOfTerritoryControl.ColorHDR );
                }
            }

            if ( territoryControlType != null && territoryControlType.Resource != null )
            {
                IA5Sprite resourceIcon = territoryControlType.Resource.Icon;

                float yRot = item.yRot;

                Quaternion iconRotation = Quaternion.Euler( 0, yRot + 90, 0 );

                Vector3 forwardVector = iconRotation * Vector3.forward;

                resourceIcon.WriteToDrawBufferForOneFrame( InputCaching.IsInInspectMode_Any, pos.PlusY( MathRefs.TerritoryControlFlagIconYOffset.FloatMin * overallScale ) +
                    (forwardVector * MathRefs.TerritoryControlFlagIconShiftsForwardBy.FloatMin * overallScale), iconRotation,
                    MathRefs.TerritoryControlFlagIconBaseScale.FloatMin * overallScale, territoryControlType.ResourceColor, false, false, false );
            }

            return true;
        }
        #endregion

        #region DrawMapItemHighlighted        
        internal static void DrawMapItemHighlighted( MapItem item, Color ColorForHighlight, HighlightPass Pass, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch ( Pass )
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( item.rawReadPos, rot, 
                    item.Scale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                    RenderColorStyle.HighlightColor, ColorForHighlight, 
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightedBorder        
        internal static void DrawMapItemHighlightedBorder( MapItem item, Color ColorForHighlight, HighlightPass Pass, bool IsMapMode, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch (Pass)
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;

                Vector3 scale = item.Scale.ComponentWiseMult( SharedRenderManagerData.highlightBorder_ScaleMult ); //we only do this here, not on the children, because their scale will be relative to this

                float scaleMultiplierY = item.Type?.Building?.MultiplierYForHighlights ?? 0;
                if ( scaleMultiplierY > 1f )
                    scale.y *= scaleMultiplierY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, scale,
                    RenderColorStyle.HighlightColor, ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }

        
        internal static void DrawMapItemHighlightedBorder( MapItem item, Color ColorForHighlight, Vector3 ScaleMult, HighlightPass Pass, bool IsMapMode, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch ( Pass )
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot,
                    item.Scale.ComponentWiseMult( ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                    RenderColorStyle.HighlightColor, ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightedBorderPulsingBeacon        
        internal static void DrawMapItemHighlightedBorderPulsingBeacon( MapItem item, Color ColorForHighlight, bool IsMapMode, HighlightPass Pass, PulsingBeaconModelData BeaconData, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch ( Pass )
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;
                pos.y += BeaconData.ExtraY;

                Vector3 scale = item.Scale.ComponentWiseMult( SharedRenderManagerData.highlightBorder_ScaleMult ); //we only do this here, not on the children, because their scale will be relative to this

                float scaleMultiplierY = item.Type?.Building?.MultiplierYForHighlights??0;
                if ( scaleMultiplierY > 1f )
                    scale.y *= scaleMultiplierY;

                float minY = scale.y;

                if ( BeaconData.YScaleDivisor > 0 )
                {
                    scale.y /= BeaconData.YScaleDivisor;
                    scale.x *= BeaconData.XZScaleMult;
                    scale.z *= BeaconData.XZScaleMult;

                    if ( scale.y < minY )
                        scale.y = minY; //never make it shorter, only taller
                }

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, scale, 
                    RenderColorStyle.HighlightColor, ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapGlowingIndicator        
        internal static void DrawMapGlowingIndicator( MapGlowingIndicator item, bool IsMapMode, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            if ( item.LastFramePrepRendered >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            item.LastFramePrepRendered = framesPrepped;

            PrimaryRenderer primaryRend = item.ItemRoot?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.Rotation;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.Position;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;

                Vector3 scale = item.Scale;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, rot, scale,
                    RenderColorStyle.HighlightColor, item.ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.ItemRoot.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.ItemRoot.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.ItemRoot.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, item.ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightOutlineLarge        
        internal static void DrawMapItemHighlightOutlineLarge( MapItem item, HighlightPass Pass, bool IsTerritoryControl, bool IsMapMode, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch ( Pass )
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            float overallScale = IsTerritoryControl ? ( IsMapMode ? MathRefs.TerritoryControlFlagScaleCityMap.FloatMin : MathRefs.TerritoryControlFlagScaleNormal.FloatMin ) : 1f;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;
                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot,
                    item.Scale * overallScale,
                    RenderColorStyle.HighlightOutlineLarge );
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightOutlineLarge );
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightOutlineSmall        
        internal static void DrawMapItemHighlightOutlineSmall( MapItem item, HighlightPass Pass, bool IsTerritoryControl, bool IsMapMode, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            switch ( Pass )
            {
                case HighlightPass.First:
                    if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_Highlight = framesPrepped;
                    break;
                case HighlightPass.Second:
                    if ( item.LastFramePrepRendered_HighlightSecond >= framesPrepped )
                    {
                        //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                        //    " more than once in a single frame!", Verbosity.ShowAsError );
                        return;
                    }
                    item.LastFramePrepRendered_HighlightSecond = framesPrepped;
                    break;
                case HighlightPass.AlwaysHappen:
                    break;
            }

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            float overallScale = IsTerritoryControl ? (IsMapMode ? MathRefs.TerritoryControlFlagScaleCityMap.FloatMin : MathRefs.TerritoryControlFlagScaleNormal.FloatMin) : 1f;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 pos = item.rawReadPos;
                if ( IsMapMode )
                    pos.y += EXTRA_Y_IN_MAP_MODE;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( pos, rot,
                    item.Scale * overallScale,
                    RenderColorStyle.HighlightOutlineSmall );
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightOutlineSmall );
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightedFallingCubes        
        internal static void DrawMapItemHighlightedFallingCubes( MapItem item, Color ColorForHighlight, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            if ( item.LastFramePrepRendered_HighlightFalling >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            item.LastFramePrepRendered_HighlightFalling = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 scale = item.Scale.ComponentWiseMult( SharedRenderManagerData.highlightBorder_ScaleMult ); //we only do this here, not on the children, because their scale will be relative to this

                float scaleMultiplierY = item.Type?.Building?.MultiplierYForHighlights ?? 0;
                if ( scaleMultiplierY > 1f )
                    scale.y *= scaleMultiplierY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( item.rawReadPos, rot,  scale, 
                    RenderColorStyle.FallingCubes, ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.FallingCubes, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightedFallingRain        
        internal static void DrawMapItemHighlightedFallingRain( MapItem item, Color ColorForHighlight, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            if ( item.LastFramePrepRendered_HighlightFalling >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            item.LastFramePrepRendered_HighlightFalling = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 scale = item.Scale.ComponentWiseMult( SharedRenderManagerData.highlightBorder_ScaleMult ); //we only do this here, not on the children, because their scale will be relative to this

                float scaleMultiplierY = item.Type?.Building?.MultiplierYForHighlights ?? 0;
                if ( scaleMultiplierY > 1f )
                    scale.y *= scaleMultiplierY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( item.rawReadPos, rot, scale,
                    RenderColorStyle.FallingRain, ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.FallingRain, ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemHighlightedGhost        
        internal static void DrawMapItemHighlightedGhost( MapItem item, Color ColorForHighlight, bool IncludeDepth, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            item.LastFramePrepRendered_Highlight = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( item.rawReadPos, rot,
                    item.Scale.ComponentWiseMult( SharedRenderManagerData.highlight_ScaleMult ), //we only do this here, not on the children, because their scale will be relative to this
                    (IncludeDepth ? RenderColorStyle.HighlightGhostDepth : RenderColorStyle.HighlightGhost ), ColorForHighlight,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, (IncludeDepth ? RenderColorStyle.HighlightGhostDepth : RenderColorStyle.HighlightGhost), ColorForHighlight,
                        RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
                }
            }
        }
        #endregion

        #region DrawMapItemGlassyUncolored        
        internal static void DrawMapItemGlassyUncolored( MapItem item, Int64 framesPrepped )
        {
            if ( item == null )
                return;

            if ( item.LastFramePrepRendered_Highlight >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render highlighted version of item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation +
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            item.LastFramePrepRendered_Highlight = framesPrepped;

            PrimaryRenderer primaryRend = item.Type?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix;
            {
                Quaternion rot = item.rawReadRot;
                if ( primaryRend.Rotates )
                    rot *= primaryRend.RotationForInGameGlobal;

                Vector3 scale = item.Scale.ComponentWiseMult( SharedRenderManagerData.highlightBorder_ScaleMult ); //we only do this here, not on the children, because their scale will be relative to this

                float scaleMultiplierY = item.Type?.Building?.MultiplierYForHighlights ?? 0;
                if ( scaleMultiplierY > 1f )
                    scale.y *= scaleMultiplierY;

                parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( item.rawReadPos, rot, scale,
                    RenderColorStyle.GlassyUncolored );
            }

            if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < item.Type.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = item.Type.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.GlassyUncolored );
                }
            }
        }
        #endregion

        #region TryDrawLockdown        
        public static bool TryDrawLockdown( Lockdown lockdown )
        {
            if ( lockdown == null)
                return false;
            LockdownType lockdownType = lockdown.Type;
            if ( lockdownType == null )
                return false;

            A5Renderer rend = lockdownType.BubbleVisuals.Renderer as A5Renderer;
            if ( rend == null )
                return false;
            IA5RendererGroup rendGroup = rend.ParentGroup;
            if ( rendGroup == null )
                return false;

            rendGroup.WriteToDrawBufferForOneFrame_BasicNoColor( lockdown.Position, Quaternion.identity, lockdownType.BubbleScale, RenderColorStyle.NoColor );

            if ( lockdownType.RenderRadiusOfLockdownSeparateFromBubble )
                DrawHelper.RenderRangeCircle( lockdown.Position, lockdownType.Radius, ColorMath.Red );
            return true;
        }
        #endregion

        #region DrawDebugBuildingMarkerOnMapItem        
        internal static void DrawDebugBuildingMarkerOnMapItem( MapItem parentItem, MapCellDrawGroup DrawGroupOrNull, BuildingMarkerColor MarkerColor,
            RenderOpacity OpacityToUseIfTransparencyIsNeeded, Int64 framesPrepped )
        {
            if ( MarkerColor == null )
                return;

            if ( parentItem.LastFramePrepRendered_Addon >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            parentItem.LastFramePrepRendered_Addon = framesPrepped;

            BuildingPrefab buildingPrefab = parentItem.Type.Building;
            if ( buildingPrefab.MarkerPrefab == null )
                return; //nevermind!

            A5ObjectRoot markerPlace = buildingPrefab.MarkerPrefab.PlaceableRoot;

            PrimaryRenderer primaryRend = markerPlace?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Quaternion obbRot = parentItem.OBBCache.GetOBB_ExpensiveToUse().Rotation;
            Vector3 pos = parentItem.OBBCache.TopCenter;
            pos += (obbRot * buildingPrefab.MarkerOffset );

            Quaternion finalRot = obbRot;
            if ( buildingPrefab.MarkerHasRotation )
                finalRot *= buildingPrefab.MarkerRotation;

            if ( primaryRend.Rotates )
                finalRot *= primaryRend.RotationForInGameGlobal;

            Vector3 scale = markerPlace.OriginalScale;
            if ( buildingPrefab.MarkerPrefab.ScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerPrefab.ScaleMultiplier;
            if ( buildingPrefab.MarkerScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerScaleMultiplier;

            Color emissionColor = MarkerColor.ColorHDR;
            if ( buildingPrefab.MarkerPrefab.ColorIntensityMultiplier > 0 )
                emissionColor *= buildingPrefab.MarkerPrefab.ColorIntensityMultiplier;

            Matrix4x4 parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, finalRot, scale,
                RenderColorStyle.EmissiveColor, emissionColor,
                DrawGroupOrNull?.DrawsWithFullOpacity??true ? RenderOpacity.Normal : OpacityToUseIfTransparencyIsNeeded, false );

            if ( markerPlace.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                for ( int i = 0; i < markerPlace.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = markerPlace.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    finalRot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        finalRot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, finalRot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.EmissiveColor, emissionColor,
                        DrawGroupOrNull?.DrawsWithFullOpacity??true ? RenderOpacity.Normal : OpacityToUseIfTransparencyIsNeeded, false );
                }
            }
        }
        #endregion        

        #region DrawMarkerTypeOfMapItem        
        internal static void DrawMarkerTypeOfMapItem( MapItem parentItem, BuildingMarkerColor MarkerColor, MapCellDrawGroup DrawGroupOrNull, 
            RenderOpacity OpacityToUseIfTransparencyIsNeeded, bool IsMapMode, Int64 framesPrepped )
        {
            if ( MarkerColor == null )
                return;

            if ( parentItem.LastFramePrepRendered_Addon >= framesPrepped )
            {
                //ArcenDebugging.LogSingleLine( "Tried to render item " + item.MapItemID + " from cell " + item.ParentCell.CellLocation + 
                //    " more than once in a single frame!", Verbosity.ShowAsError );
                return;
            }
            parentItem.LastFramePrepRendered_Addon = framesPrepped;

            BuildingPrefab buildingPrefab = parentItem?.SimBuilding?.GetPrefab();
            if ( buildingPrefab == null )
                return;
            if ( buildingPrefab.MarkerPrefab == null )
                return;

            A5ObjectRoot markerPlace = buildingPrefab.MarkerPrefab.PlaceableRoot;

            PrimaryRenderer primaryRend = markerPlace?.FirstRendererOfThisRoot;
            if ( primaryRend == null )
                return;
            IA5RendererGroup rendGroup = primaryRend.Group;
            if ( rendGroup == null )
                return;

            Quaternion obbRot = parentItem.OBBCache.GetOBB_ExpensiveToUse().Rotation;
            Vector3 pos = parentItem.OBBCache.TopCenter;

            //do this after the stun progress bar, so that the bar is centered over the building instead of the marker
            pos += (obbRot * buildingPrefab.MarkerOffset);

            if ( IsMapMode )
                pos.y += EXTRA_Y_IN_MAP_MODE;

            Quaternion finalRot = obbRot;
            if ( buildingPrefab.MarkerHasRotation )
                finalRot *= buildingPrefab.MarkerRotation;

            if ( primaryRend.Rotates )
                finalRot *= primaryRend.RotationForInGameGlobal;

            Vector3 scale = markerPlace.OriginalScale;
            if ( buildingPrefab.MarkerPrefab.ScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerPrefab.ScaleMultiplier;
            if ( buildingPrefab.MarkerScaleMultiplier > 0 )
                scale *= buildingPrefab.MarkerScaleMultiplier;

            Color emissionColor = MarkerColor.ColorHDR;
            if ( buildingPrefab.MarkerPrefab.ColorIntensityMultiplier > 0 )
                emissionColor *= buildingPrefab.MarkerPrefab.ColorIntensityMultiplier;

            Matrix4x4 parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, finalRot, scale,
                RenderColorStyle.EmissiveColor, emissionColor,
                DrawGroupOrNull?.DrawsWithFullOpacity??true ? RenderOpacity.Normal : OpacityToUseIfTransparencyIsNeeded, false );

            if ( markerPlace.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                for ( int i = 0; i < markerPlace.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = markerPlace.SecondarysRenderersOfThisRoot[i];
                    if ( secondaryRend == null )
                        continue;
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    finalRot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        finalRot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, finalRot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.EmissiveColor, emissionColor,
                        DrawGroupOrNull?.DrawsWithFullOpacity??true ? RenderOpacity.Normal : OpacityToUseIfTransparencyIsNeeded, false );
                }
            }
        }
        #endregion

        #region GetIsPointInRangeEfficient
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static bool GetIsPointInRangeEfficient( Vector3 Origin, Vector3 TestPoint, float Range, float RangeSquared )
        {
            float num = TestPoint.x - Origin.x;
            if ( num < 0f )
            {
                num = 0f - num;
            }

            if ( num > Range )
            {
                return false;
            }

            float num2 = TestPoint.z - Origin.z;
            if ( num2 < 0f )
            {
                num2 = 0f - num2;
            }

            if ( num2 > Range )
            {
                return false;
            }

            float num3 = num * num + num2 * num2;
            return num3 <= RangeSquared;
        }
        #endregion

        private static readonly Vector3 holoWallScale = new Vector3( 20f, 0.6f, 1f );
        private const float HOLO_WALL_MAX_MOVE_DOWN = -1.5f;

        //#region DrawMapCellDistrict_StreetsView
        //private static void DrawMapCellDistrict_StreetsView( MapCellDistrictDrawer MapCellDistrict, bool UseVisMaterial, Color ColorForVis )
        //{
        //    if ( MapCellDistrict == null )
        //        return;

        //    if ( SharedRenderManagerData.districtCube == null )
        //    {
        //        SharedRenderManagerData.districtCube = VisSimpleDrawingObjectTable.Instance.GetRowByID( "BasicCube" );
        //        if ( SharedRenderManagerData.districtCube == null )
        //            return;
        //    }

        //    if ( SharedRenderManagerData.districtHoloWallNormal == null )
        //    {
        //        SharedRenderManagerData.districtHoloWallNormal = VisSimpleDrawingObjectTable.Instance.GetRowByID( "HoloWall" );
        //        if ( SharedRenderManagerData.districtHoloWallNormal == null )
        //            return;
        //    }

        //    A5RendererGroup rendGroupCube = SharedRenderManagerData.districtCube.RendererGroup as A5RendererGroup;
        //    A5RendererGroup rendGroupHoloWall = SharedRenderManagerData.districtHoloWallNormal.RendererGroup as A5RendererGroup;            

        //    if ( rendGroupCube != null && Engine_HotM.IsInDrawDistrictColorsMode )
        //    {
        //        Quaternion rot = Quaternion.identity;
        //        rendGroupCube.WriteToDrawBufferForOneFrame_BasicColor( MapCellDistrict.DrawCenter, rot, MapCellDistrict.DrawSize,
        //            RenderColorStyle.SelfColor, ColorForVis,
        //            RenderOpacity.Normal, false ); //this will be transparent, but not in a way we need to manage ourselves
        //    }

        //    if ( rendGroupHoloWall != null && MapCellDistrict.NormalHoloWallSegments.Count > 0 )
        //    {
        //        foreach ( HoloWallSegment holoWallSegment in MapCellDistrict.NormalHoloWallSegments )
        //        {
        //            rendGroupHoloWall.WriteToDrawBufferForOneFrame_BasicNoColor( holoWallSegment.Pos, holoWallSegment.Rotation, holoWallScale,
        //                RenderColorStyle.NoColor );
        //        }
        //    }
        //}
        //#endregion

        #region DebugDrawAllCells
        public static void DebugDrawAllCells( MainGameHookBase mainGameHookBase )
        {
            List<IShapeToDraw> commandList = mainGameHookBase.GetListOfDebugShapesToDraw();
            commandList.Clear();

            foreach ( MapCell cell in CityMap.Cells )
            {
                Bounds cellBounds = cell.CellBounds_MoreExpensive;

                DrawShape_WireBox cellBoundsBox;
                cellBoundsBox.Center = cellBounds.center;
                cellBoundsBox.Size = cellBounds.size;
                cellBoundsBox.Color = ColorMath.BlueTopaz;
                cellBoundsBox.Thickness = 1f;
                commandList.Add( cellBoundsBox );
            }
        }
        #endregion
    }

    public enum HighlightPass
    {
        First,
        Second,
        AlwaysHappen
    }
}

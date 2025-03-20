using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    internal static class WorldTileInnerPopulator_Structural
    {
        public static bool Debug_LogTileCompletions = false;

        #region DoPerFrame
        public static void DoPerFrame()
        {
            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //Willard reports that sometimes the mapgen and the tile filling race, so this waits until they are done
            if ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                return; //if we haven't yet finished initializing absolutely everything, then don't run this yet

            if ( CityMap.TilesToPostProcess_Structural.Count == 0 )
                return; //nothing to do!
            if ( CityMap.TilesBeingPostProcessed_Structural > 0 )
                return;

            Debug_LogTileCompletions = GameSettings.Current.GetBool( "Debug_LogTileCompletions" );

            int randSeed = Engine_Universal.PermanentQualityRandom.Next();
            if ( !ArcenThreading.RunTaskOnBackgroundThread( "_Data.DoPopulationOfManyTiles",
                ( TaskStartData startData ) =>
                {
                    while ( CityMap.TilesToPostProcess_Structural.TryDequeue( out MapTile tile ) )
                    {
                        if ( tile.IsLoadedFromSavegame )
                            continue; //nothing to do structurally, that's a whoops that this is in here at all, but it's fine

                        Interlocked.Increment( ref CityMap.TilesBeingPostProcessed_Structural );

                        tile.timeStartedPopulation_Structural = ArcenTime.AnyTimeSinceStartF;
                        try
                        {
                            DoPopulationOfTile_OnBackgroundThread( startData, randSeed, tile );
                            randSeed++;
                        }
                        catch ( Exception e )
                        {
                            if ( !startData.IsMeantToSilentlyDie() )
                                ArcenDebugging.LogWithStack( "WorldTileInnerPopulator_Structural.DoPerFrame_Structural background thread error: " + e, Verbosity.ShowAsError );
                        }
                        finally //normally we avoid finally, but otherwise this won't happen on thread abort
                        {
                            Interlocked.Decrement( ref CityMap.TilesBeingPostProcessed_Structural );
                        }

                        //now handle the decorations
                        CityMap.TilesToPostProcess_Decoration.Enqueue( tile );
                        //ArcenDebugging.LogSingleLine( "CityMap.TilesBeingPostProcessed_Structural: " + CityMap.TilesBeingPostProcessed_Structural, Verbosity.ShowAsError );
                    }

                    //ArcenDebugging.LogSingleLine( "hole tiles: " + CityMap.CountHoleTiles(), Verbosity.ShowAsError );
                } ) )
            {
                //failed to start the above
            }
        }
        #endregion

        #region DoPopulationOfTile_OnBackgroundThread
        private static void DoPopulationOfTile_OnBackgroundThread( TaskStartData startData, int randSeed, MapTile tile )
        {
            MersenneTwister rand = new MersenneTwister( randSeed );

            MapPOI effectivePOI = null;

            if ( startData.IsMeantToSilentlyDie() )
                return; //this is a thread that was killed!

            if ( tile.IsOutOfBoundsTile )
            {
                DoPopulationOfHoleTile( startData, rand, tile, out effectivePOI );

                //ArcenDebugging.LogSingleLine( "Populated OOB as '" + tile.LevelSourceFilenameForTile + "'.", Verbosity.DoNotShow );
            }
            else if ( tile.TileType == null || tile.IsHoleTile )
            {
                DoPopulationOfHoleTile( startData, rand, tile, out effectivePOI );
            }
            else
            {
                DoPopulationOfRegularTile( startData, rand, tile );
            }

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile roads done! " + tile.TileID + " seconds spent: " +
                    ( ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Structural ).ToString( "0.00"), Verbosity.DoNotShow );

            DoDetailPopulationOfTile( startData, rand, tile, effectivePOI );
            FinishTileSetup( tile );
        }
        #endregion

        #region DoPopulationOfRegularTile
        private static void DoPopulationOfRegularTile( TaskStartData startData, MersenneTwister rand, MapTile tile )
        {
            LevelType levelType = LevelTypeTable.Instance.GetRowByID( tile.TileType.TileTypeID );
            if ( levelType.AvailableContent.Count == 0 )
            {
                ArcenDebugging.LogWithStack( "WorldTileInnerPopulator_Structural.DoPopulationOfRegularTile was asked to fill a tile with skeleton type of '" + 
                    levelType.ID + "', but it had no available files for it.", Verbosity.ShowAsError );
                return;
            }

            ReferenceLevelData levelSource = levelType.AvailableContent[rand.Next( 0, levelType.AvailableContent.Count )];
            tile.LevelSourceFilenameForTile = ArcenIO.GetFileNameWithoutExtension( levelSource.SourceFilename );

            Vector3 tileCenter = tile.Center;

            float tileRotation = (float)tile.Rotation;
            bool isXMirrored = tile.Mirroring.IsX();
            bool isZMirrored = tile.Mirroring.IsZ();

            foreach ( ReferenceObject obj in levelSource.AllObjects )
            {
                if ( obj.ObjRoot.IsPathingPoint || obj.ObjRoot.IsPathingRegion )
                    continue; //we skip these in regular tiles

                A5ObjectRoot objRoot = obj.ObjRoot;
                PlaceableExtraData extraData = objRoot.ExtraPlaceableData;

                Vector3 objPos = obj.pos;
                Vector3 objRot = obj.rot;

                #region Mirroring Logic
                //no matter what you do, the position is mirrored.  Some things also rotate, but that depends on their symmetry
                if ( isXMirrored )
                    objPos = objPos.ReplaceX( -objPos.x );
                if ( isZMirrored )
                    objPos = objPos.ReplaceZ( -objPos.z );

                if ( extraData.IsSymmetricalForXMirroring && extraData.IsSymmetricalForZMirroring )
                {
                    //this is the case where we don't need to do any rotation at all for mirroring, so we can skip all the rest
                }
                else
                {
                    //at least one of our sides is not symmetrical, so we need to handle mirroring.
                    if ( isXMirrored && isZMirrored )
                    {
                        //this is the simple case!  A double mirror is identical to a 180 degree rotation
                        objRot.y += 180;
                    }
                    else
                    {
                        if ( isXMirrored || isZMirrored )
                        {
                            int rotationY = Mathf.RoundToInt( objRot.y );
                            if ( rotationY == 90 || rotationY == 270 )
                            {
                                isXMirrored = !isXMirrored;
                                isZMirrored = !isZMirrored;
                            }
                        }

                        if ( isXMirrored )
                        {
                            //this is x mirrored, but not z mirrored
                            if ( !extraData.IsSymmetricalForXMirroring )
                            { 
                                //we are not symmetrical in the x axis, so we must do something.
                                if ( extraData.PathOfMirrorXReplacement.Length > 0 )
                                {
                                    if ( !A5ObjectAggregation.ObjectRootsByPath.TryGetValue( extraData.PathOfMirrorXReplacement, out A5ObjectRoot newRoot ) )
                                        ArcenDebugging.LogSingleLine( "PathOfMirrorXReplacement of '" + extraData.PathOfMirrorXReplacement +
                                            "' could not be found for '" + extraData.ID + "'.", Verbosity.ShowAsError );
                                    else
                                        objRoot = newRoot; // we replace objRoot, but we do NOT replace extraData.  We keep using that
                                }

                                //this might be the same object, or a replaced object
                                if ( extraData.Rotate180DegreesForXMirror )
                                    objRot.y += 180;
                            }
                        }
                        else if ( isZMirrored )
                        {
                            //this is z mirrored, but not x mirrored
                            if ( !extraData.IsSymmetricalForZMirroring )
                            {
                                //we are not symmetrical in the z axis, so we must do something.
                                if ( extraData.PathOfMirrorZReplacement.Length > 0 )
                                {
                                    if ( !A5ObjectAggregation.ObjectRootsByPath.TryGetValue( extraData.PathOfMirrorZReplacement, out A5ObjectRoot newRoot ) )
                                        ArcenDebugging.LogSingleLine( "PathOfMirrorZReplacement of '" + extraData.PathOfMirrorXReplacement +
                                            "' could not be found for '" + extraData.ID + "'.", Verbosity.ShowAsError );
                                    else
                                        objRoot = newRoot; // we replace objRoot, but we do NOT replace extraData.  We keep using that
                                }

                                //this might be the same object, or a replaced object
                                if ( extraData.Rotate180DegreesForZMirror )
                                    objRot.y += 180;
                            }
                        }
                    }
                }
                #endregion

                Quaternion rot = Quaternion.Euler( objRot.x, objRot.y, objRot.z );
                if ( tileRotation != 0 )
                    MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), tileRotation );
                
                objPos = objPos.PlusX( tileCenter.x );
                objPos = objPos.PlusZ( tileCenter.z );

                MapCell cell = tile.GetCellFromWorldPointInTile( objPos, tile.LevelSourceFilenameForTile );
                if ( cell == null )
                    continue;

                //World: O.transform.rotation = Quaternion * O.transform.rotation   <- this rotates around world axis
                //Local: O.transform.rotation = O.transform.rotation * Quaternion < -this rotates around its local axis
                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( objPos, rot, obj.scale );

                #region Meta Sub Regions
                if ( objRoot.IsMetaRegion )
                {
                    MapSubRegion subRegion = new MapSubRegion();
                    subRegion.Type = objRoot;
                    subRegion.Position = objPos;
                    subRegion.Rotation = rot;
                    subRegion.Scale = obj.scale;
                    subRegion.OBBCache.SetToOBB( newOBB );

                    if ( objRoot.HasNSEWIndicators )
                    {
                        subRegion.DirectionN = obj.DirectionNorth;
                        subRegion.DirectionS = obj.DirectionSouth;
                        subRegion.DirectionE = obj.DirectionEast;
                        subRegion.DirectionW = obj.DirectionWest;
                        if ( isXMirrored )
                        {
                            subRegion.DirectionE = !subRegion.DirectionE;
                            subRegion.DirectionW = !subRegion.DirectionW;
                        }
                        if ( isZMirrored )
                        {
                            subRegion.DirectionN = !subRegion.DirectionN;
                            subRegion.DirectionS = !subRegion.DirectionS;
                        }
                    }

                    tile.SkeletonMetas.Add( subRegion );

                    continue; //we don't actually seed the meta regions directly, we just have those in the list of SkeletonMetas
                }
                #endregion

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = objRoot;
                item.SetPosition( objPos );
                item.SetRotation( rot );
                item.Scale = obj.scale;
                //note, not setting the ParentPOIOrNull, as these are all non-buildings
                item.OBBCache.SetToOBB( newOBB );

                if ( objRoot.Roads.Count > 0 )
                {
                    cell.AllRoads.Add( item );
                    if ( item.Type.DrivingLanes.Count > 0 )
                    {
                        cell.DrivableRoads.Add( item );
                    }
                    if ( item.Type.GetHasAnyRoadsSupportingTransportMethod( TransportMethod.Walking ) )
                    {
                        cell.WalkableRoads.Add( item );
                    }
                    if ( item.Type.GetHasAnyRoadsSupportingTransportMethod( TransportMethod.Train ) )
                    {
                        cell.Tracks.Add( item );
                    }
                }
                else
                {
                    if ( item.Type.ExtraPlaceableData.IsFence )
                        cell.Fences.Add( item );
                    else
                        cell.OtherSkeletonItems.Add( item );
                }
            }

            //{
            //    MapCell cell = tile.CellsList[0];
            //    ArcenDebugging.LogSingleLine( "Cell roads: " + cell.Roads.Count + " OtherSkeletonItems: " + cell.OtherSkeletonItems.Count, Verbosity.DoNotShow );
            //}
        }
        #endregion

        #region DoPopulationOfHoleTile
        private static void DoPopulationOfHoleTile( TaskStartData startData, MersenneTwister rand, MapTile tile, out MapPOI ParentPOIOrNull )
        {
            //later: wait for the background seeding thread to be done, then look at adjacent tiles and figure out things to add as caps.
            //we have to wait for background seeding specifically because we are not supposed to look in that dictionary while it's doing its work.

            int debugStage = 0;
            try
            {
                debugStage = 100;
                LevelType chosenType = tile.District.DecorationSource;
                debugStage = 200;
                tile.LevelSourceFilenameForTile = ArcenIO.GetFileNameWithoutExtension( chosenType.ID );

                debugStage = 500;
                foreach ( MapCell cell in tile.CellsList )
                {
                    debugStage = 600;
                    if ( chosenType.AvailableContent.Count == 0 )
                    {
                        ArcenDebugging.LogSingleLine( "No available files in chosenType '" + chosenType.ID + "' for tile!", Verbosity.ShowAsError );
                        continue;
                    }
                    debugStage = 700;
                    ReferenceLevelData decorationSource = chosenType.AvailableContent[rand.Next( 0, chosenType.AvailableContent.Count )];
                    debugStage = 800;
                    cell.NonSerialized_HoleTileDecorationSeedingFile = decorationSource.SourceFilenameID;
                    cell.NonSerialized_HoleTileDecorationRandomSeed = rand.Next();
                }

                debugStage = 1100;
                if ( tile.HoleLevelType != null && tile.HoleLevelData != null )
                {
                    debugStage = 1200;
                    if ( tile.HoleLevelType.POIToSpawn != null )
                    {
                        debugStage = 1200;
                        //pretty common for these to also spawn a POI
                        ParentPOIOrNull = MapPOI.Mapgen_CreateNewPOI_FullTile( tile.HoleLevelType.POIToSpawn, tile );
                        tile.POIOrNull = ParentPOIOrNull;
                    }
                    else
                        ParentPOIOrNull = null;

                    debugStage = 2100;
                    PopulateFullMapTile_FromHoleLevelData( startData, rand, tile, tile.HoleLevelType, tile.HoleLevelData, ParentPOIOrNull );
                    debugStage = 2200;
                    DoDetailPopulationOfTile( startData, rand, tile, ParentPOIOrNull );
                }
                else
                    ParentPOIOrNull = null;
            }
            catch ( Exception e )
            {
                ParentPOIOrNull = null;
                ArcenDebugging.LogDebugStageWithStack( "DoPopulationOfHoleTile-Structural", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region PopulateFullMapTile_FromHoleLevelData
        private static void PopulateFullMapTile_FromHoleLevelData( TaskStartData startData, MersenneTwister rand, MapTile tile, LevelType HoleLevelType,
            ReferenceLevelData HoleLevelData, MapPOI ParentPOIOrNull )
        {
            //ArcenDebugging.LogSingleLine( "Populate hole data from type " + HoleLevelType.ID, Verbosity.DoNotShow );

            float tileSizeX = CityMap.CELL_FULL_SIZE * tile.WidthInCells;
            float tileSizeZ = CityMap.CELL_FULL_SIZE * tile.HeightInCells;

            float levelSizeX = HoleLevelType.SizeX + HoleLevelType.SizeX;
            float levelSizeZ = HoleLevelType.SizeZ + HoleLevelType.SizeZ;

            if ( levelSizeX > tileSizeX )
            {
                ArcenDebugging.LogSingleLine( "Level size X " + levelSizeX + " was more than tileSizeX " + tileSizeX +
                    " when looking at a meta region zone here, and the hole level type " + HoleLevelType.ID, Verbosity.ShowAsError );
                return;
            }
            if ( levelSizeZ > tileSizeZ )
            {
                ArcenDebugging.LogSingleLine( "Level size Z " + levelSizeZ + " was more than tileSizeZ " + tileSizeZ +
                    " when looking at a meta region zone here, and the hole level type " + HoleLevelType.ID, Verbosity.ShowAsError );
                return;
            }

            tile.LevelSourceFilenameForTile = ArcenIO.GetFileNameWithoutExtension( HoleLevelData.SourceFilename );

            Vector3 tileCenter = tile.Center;
            //ArcenFloatRectangle levelRect = ArcenFloatRectangle.CreateFromCenterAndHalfSize( tileCenter.x, tileCenter.z, 
            //    tile.CalculateLevelHalfSizeX(), tile.CalculateLevelHalfSizeZ() );

            float xDestOffset = ( tileSizeX - levelSizeX ) / 2;
            float zDestOffset = ( tileSizeZ - levelSizeZ ) / 2;

            float tileRotation = tile.CellsList.Count > 1 ? 0 : (float)tile.Rotation;
            bool isXMirrored = tile.CellsList.Count <= 1 && tile.Mirroring.IsX();
            bool isZMirrored = tile.CellsList.Count <= 1 && tile.Mirroring.IsZ();

            ThrowawayDictionary<int, MapPathingPoint> pathfindingNodes = new ThrowawayDictionary<int, MapPathingPoint>( 40 );

            foreach ( ReferenceObject obj in HoleLevelData.AllObjects )
            {
                A5ObjectRoot objRoot = obj.ObjRoot;
                PlaceableExtraData extraData = objRoot.ExtraPlaceableData;

                Vector3 objPos = obj.pos;
                Vector3 objRot = obj.rot;

                #region Mirroring Logic
                //no matter what you do, the position is mirrored.  Some things also rotate, but that depends on their symmetry
                if ( isXMirrored )
                    objPos = objPos.ReplaceX( -objPos.x );
                if ( isZMirrored )
                    objPos = objPos.ReplaceZ( -objPos.z );

                if ( extraData.IsSymmetricalForXMirroring && extraData.IsSymmetricalForZMirroring )
                {
                    //this is the case where we don't need to do any rotation at all for mirroring, so we can skip all the rest
                }
                else
                {
                    //at least one of our sides is not symmetrical, so we need to handle mirroring.
                    if ( isXMirrored && isZMirrored )
                    {
                        //this is the simple case!  A double mirror is identical to a 180 degree rotation
                        objRot.y += 180;
                    }
                    else
                    {
                        if ( isXMirrored || isZMirrored )
                        {
                            int rotationY = Mathf.RoundToInt( objRot.y );
                            if ( rotationY == 90 || rotationY == 270 )
                            {
                                isXMirrored = !isXMirrored;
                                isZMirrored = !isZMirrored;
                            }
                        }

                        if ( isXMirrored )
                        {
                            //this is x mirrored, but not z mirrored
                            if ( !extraData.IsSymmetricalForXMirroring )
                            {
                                //we are not symmetrical in the x axis, so we must do something.
                                if ( extraData.PathOfMirrorXReplacement.Length > 0 )
                                {
                                    if ( !A5ObjectAggregation.ObjectRootsByPath.TryGetValue( extraData.PathOfMirrorXReplacement, out A5ObjectRoot newRoot ) )
                                        ArcenDebugging.LogSingleLine( "PathOfMirrorXReplacement of '" + extraData.PathOfMirrorXReplacement +
                                            "' could not be found for '" + extraData.ID + "'.", Verbosity.ShowAsError );
                                    else
                                        objRoot = newRoot; // we replace objRoot, but we do NOT replace extraData.  We keep using that
                                }

                                //this might be the same object, or a replaced object
                                if ( extraData.Rotate180DegreesForXMirror )
                                    objRot.y += 180;
                            }
                        }
                        else if ( isZMirrored )
                        {
                            //this is z mirrored, but not x mirrored
                            if ( !extraData.IsSymmetricalForZMirroring )
                            {
                                //we are not symmetrical in the z axis, so we must do something.
                                if ( extraData.PathOfMirrorZReplacement.Length > 0 )
                                {
                                    if ( !A5ObjectAggregation.ObjectRootsByPath.TryGetValue( extraData.PathOfMirrorZReplacement, out A5ObjectRoot newRoot ) )
                                        ArcenDebugging.LogSingleLine( "PathOfMirrorZReplacement of '" + extraData.PathOfMirrorXReplacement +
                                            "' could not be found for '" + extraData.ID + "'.", Verbosity.ShowAsError );
                                    else
                                        objRoot = newRoot; // we replace objRoot, but we do NOT replace extraData.  We keep using that
                                }

                                //this might be the same object, or a replaced object
                                if ( extraData.Rotate180DegreesForZMirror )
                                    objRot.y += 180;
                            }
                        }
                    }
                }
                #endregion

                Quaternion rot = Quaternion.Euler( objRot.x, objRot.y, objRot.z );
                if ( tileRotation != 0 )
                    MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), tileRotation );

                objPos = objPos.PlusX( tileCenter.x + xDestOffset );
                objPos = objPos.PlusZ( tileCenter.z + zDestOffset );

                MapCell cell = tile.GetCellFromWorldPointInTile( objPos, tile.LevelSourceFilenameForTile );
                if ( cell == null )
                    continue;

                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( objPos, rot, obj.scale );

                #region Meta Sub Regions
                if ( objRoot.IsMetaRegion )
                {
                    MapSubRegion subRegion = new MapSubRegion();
                    subRegion.Type = objRoot;
                    subRegion.Position = objPos;
                    subRegion.Rotation = rot;
                    subRegion.Scale = obj.scale;
                    subRegion.OBBCache.SetToOBB( newOBB );

                    if ( objRoot.HasNSEWIndicators )
                    {
                        subRegion.DirectionN = obj.DirectionNorth;
                        subRegion.DirectionS = obj.DirectionSouth;
                        subRegion.DirectionE = obj.DirectionEast;
                        subRegion.DirectionW = obj.DirectionWest;
                    }

                    tile.SkeletonMetas.Add( subRegion );

                    continue; //we don't actually seed the meta regions directly, we just have those in the list of SkeletonMetas
                }
                #endregion

                if ( obj.ObjRoot.IsPathingPoint )
                {
                    MapPathingPoint pathing = new MapPathingPoint();
                    pathing.Type = objRoot;
                    pathing.Position = objPos;
                    pathing.Rotation = rot;
                    pathing.OBBCache.SetToOBB( newOBB );
                    pathing.PointID = obj.PathingLinkID;
                    pathfindingNodes[pathing.PointID] = pathing;

                    tile.PathingPoints.Add( pathing );
                    continue;
                }
                if ( obj.ObjRoot.IsPathingRegion )
                {
                    MapPathingRegion pathing = new MapPathingRegion();
                    pathing.Type = objRoot;
                    pathing.Position = objPos;
                    pathing.Rotation = rot;
                    pathing.Scale = obj.scale;
                    pathing.OBBCache.SetToOBB( newOBB );
                    pathing.PathingType = obj.PathingType;
                    if ( pathing.PathingType == null )
                        ArcenDebugging.LogSingleLine( "Error!  MapPathingRegion from hole tile '" + HoleLevelData.SourceFilename + "' had a null PathingType.", Verbosity.ShowAsError );

                    tile.PathingZones.Add( pathing );
                    continue;
                }

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = obj.ObjRoot;
                item.SetPosition( objPos );
                item.SetRotation( rot );
                item.Scale = obj.scale;
                item.OBBCache.SetToOBB( newOBB );

                if ( objRoot.Roads.Count > 0 )
                {
                    cell.AllRoads.Add( item );
                    if ( item.Type.DrivingLanes.Count > 0 )
                    {
                        cell.DrivableRoads.Add( item );
                    }
                    if ( item.Type.GetHasAnyRoadsSupportingTransportMethod( TransportMethod.Walking ) )
                    {
                        cell.WalkableRoads.Add( item );
                    }
                    if ( item.Type.GetHasAnyRoadsSupportingTransportMethod( TransportMethod.Train ) )
                    {
                        cell.Tracks.Add( item );
                    }
                }

                if ( obj.ObjRoot.Building != null )
                {
                    item.SetParentPOI( ParentPOIOrNull );
                    cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );
                }
                else if ( objRoot.Roads.Count == 0 )
                    cell.PlaceMapItemIntoCell( obj.ObjRoot.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor : 
                        (obj.ObjRoot.ExtraPlaceableData.IsFence ? TileDest.Fences : TileDest.OtherSkeletonItems ), item, true );
            }

            if ( pathfindingNodes.Count > 0 )
            {
                foreach ( KeyValuePair<int, MapPathingPoint> outerKV in pathfindingNodes )
                {
                    int pathingLinkID = outerKV.Key;
                    foreach ( RefPair<int, int> kv in HoleLevelData.PathingLinks )
                    {
                        int otherID = -1;
                        if ( kv.LeftItem == pathingLinkID )
                            otherID = kv.RightItem;
                        else if ( kv.RightItem == pathingLinkID )
                            otherID = kv.LeftItem;
                        else
                            continue;

                        MapPathingPoint otherObj = pathfindingNodes[otherID];
                        outerKV.Value.ConnectPathPoints( otherObj );
                    }
                }
            }

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile hole filled! " + tile.TileID + " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Structural).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion

        #region DoDetailPopulationOfTile
        public static void DoDetailPopulationOfTile( TaskStartData startData, MersenneTwister rand, MapTile tile, MapPOI ParentPOIOrNull )
        {
            LevelType chosenType = tile.District.DecorationSource;

            ThrowawayDictionary<BuildingType,int> tileBuildingTypesSoFar = new ThrowawayDictionary<BuildingType, int>( 80 );

            #region Building Zones Outer Logic
            //this part has to be done on the central population thread, and it loops through and starts all the threads for buildings
            //each one does building placement, and then decoration zone placement
            foreach ( MapSubRegion mapSubRegion in tile.SkeletonMetas )
            {
                A5ObjectRoot objRoot = mapSubRegion.Type;
                if ( objRoot.ExtraPlaceableData == null )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_ShortID.Length <= 0 )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                    continue; //don't handle any decoration zones here

                tile.buildingZonesStillOutstanding++;

                int randSeed = rand.Next();
                PopulateRegion_Buildings( startData, randSeed, tile, mapSubRegion, objRoot.ExtraPlaceableData.MetaOnly_ShortID, ParentPOIOrNull,
                    tileBuildingTypesSoFar );

                if ( chosenType.AvailableContent.Count == 0 )
                {
                    ArcenDebugging.LogSingleLine( "No available files in ChosenType '" + chosenType.ID + "' for tile!", Verbosity.ShowAsError );
                    continue;
                }

                ReferenceLevelData decorationSource = chosenType.AvailableContent[rand.Next( 0, chosenType.AvailableContent.Count )];
                mapSubRegion.DecorationSeedingFile = decorationSource.SourceFilenameID;
                mapSubRegion.DecorationRandomSeed = randSeed;

                tile.buildingZonesStillOutstanding--;
                //ArcenDebugging.LogSingleLine( "buildingZonesStillOutstanding: " + buildingZonesStillOutstanding, Verbosity.ShowAsError );
            }
            #endregion Building Zones Outer Logic

            #region Decoration Zones Outer Logic
            //this part has to be done on the central population thread, and it loops through and starts all the threads for decorations
            foreach ( MapSubRegion mapSubRegion in tile.SkeletonMetas )
            {
                A5ObjectRoot objRoot = mapSubRegion.Type;
                if ( objRoot.ExtraPlaceableData == null )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_ShortID.Length <= 0 )
                    continue;
                if ( !objRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                    continue; //don't handle any non-decoration zones here

                int randSeed = rand.Next();
                tile.decorationZonesStillOutstanding++;

                if ( chosenType.AvailableContent.Count == 0 )
                {
                    ArcenDebugging.LogSingleLine( "No available files in ChosenType '" + chosenType.ID + "' for tile!", Verbosity.ShowAsError );
                    continue;
                }
                ReferenceLevelData decorationSource = chosenType.AvailableContent[rand.Next( 0, chosenType.AvailableContent.Count )];
                mapSubRegion.DecorationSeedingFile = decorationSource.SourceFilenameID;
                mapSubRegion.DecorationRandomSeed = randSeed;
                tile.decorationZonesStillOutstanding--;
                //ArcenDebugging.LogSingleLine( "decorationZonesStillOutstanding: " + decorationZonesStillOutstanding, Verbosity.ShowAsError );                
            }
            #endregion Decoration Zones Outer Logic
        }
        #endregion

        #region PopulateRegion_Buildings
        private static void PopulateRegion_Buildings( TaskStartData startData, int randSeed, MapTile tile, MapSubRegion SubRegion, 
            string ShortID, MapPOI ParentPOIOrNull, ThrowawayDictionary<BuildingType, int> tileBuildingTypesSoFar )
        {
            if ( tile.SeedingLogic == null )
            {
                ArcenDebugging.LogSingleLine( "PopulateRegion_Buildings had a null seeding logic!", Verbosity.ShowAsError );
                return;
            }

            if ( ParentPOIOrNull == null && tile.POIOrNull != null )
                ParentPOIOrNull = tile.POIOrNull;

            OBBUnity regionOBB = SubRegion.OBBCache.GetOBB_ExpensiveToUse();
            Vector3 regionOBBExtents = regionOBB.Extents;
            MersenneTwister rand = new MersenneTwister( randSeed );

            ThrowawayList<MapItem> addedBuildings = new ThrowawayList<MapItem>();
            ThrowawayDictionary<int, bool> testedTypes = new ThrowawayDictionary<int, bool>( 500 );

            //in here we assume that the only thing we could collide with are other buildings we already put in

            MapUtility.GenerateRegionAxisVectors( regionOBB, out Vector3 rightAxis, out Vector3 lookAxis );
            MapUtility.GenerateRegionStartingPoints( regionOBB, out Vector3 startN, out Vector3 startS, out Vector3 startE, out Vector3 startW );


            LevelType levelTypeToSeed = null;
            #region Check MiniTilesToSeedInRegions for A levelTypeToSeed That Will Fit
            int randNumForMiniTile = rand.Next( 0, 100000 );

            if ( tile.SeedingLogic.MiniTilesToSeedInRegions.Count > 0 &&
                randNumForMiniTile < tile.SeedingLogic.PercentChanceOfGeneralMiniTileTypeAppearingOutOf100Thousand )
            {
                int startIndex = rand.Next( 0, tile.SeedingLogic.MiniTilesToSeedInRegions.Count );
                for ( int i = startIndex; i < tile.SeedingLogic.MiniTilesToSeedInRegions.Count; i++ )
                {
                    LevelType miniTile = tile.SeedingLogic.MiniTilesToSeedInRegions[i].GetActualRow( "PopulateRegion_BuildingsA", tile.SeedingLogic.ID );
                    if ( miniTile.AvailableContent.Count <= 0 )
                        continue; //skip if no content present!
                    if ( miniTile.SizeX < regionOBBExtents.x && miniTile.SizeZ < regionOBBExtents.z )
                    {
                        //check it forward
                        levelTypeToSeed = miniTile;
                        break;
                    }
                    else if ( miniTile.SizeZ < regionOBBExtents.x && miniTile.SizeX < regionOBBExtents.z )
                    {
                        //check it reversed
                        levelTypeToSeed = miniTile;
                        break;
                    }
                }
                if ( levelTypeToSeed == null )
                {
                    for ( int i = 0; i < startIndex; i++ )
                    {
                        LevelType miniTile = tile.SeedingLogic.MiniTilesToSeedInRegions[i].GetActualRow( "PopulateRegion_BuildingsB", tile.SeedingLogic.ID );
                        if ( miniTile.AvailableContent.Count <= 0 )
                            continue; //skip if no content present!
                        if ( miniTile.SizeX < regionOBBExtents.x && miniTile.SizeZ < regionOBBExtents.z )
                        {
                            //check it forward
                            levelTypeToSeed = miniTile;
                            break;
                        }
                        else if ( miniTile.SizeZ < regionOBBExtents.x && miniTile.SizeX < regionOBBExtents.z )
                        {
                            //check it reversed
                            levelTypeToSeed = miniTile;
                            break;
                        }
                    }
                }
            }
            #endregion

            #region If No levelTypeToSeed, Look For PercentChanceOfGeneralMiniTileTypeAppearing
            if ( levelTypeToSeed == null && tile.SeedingLogic.PercentChanceOfGeneralMiniTileTypeAppearingOutOf100Thousand > 0 &&
                randNumForMiniTile < tile.SeedingLogic.PercentChanceOfGeneralMiniTileTypeAppearingOutOf100Thousand )
            {
                for ( int attempt = 0; attempt < 30; attempt++ ) //how many attempts
                {
                    LevelType miniTile = LevelTypeTable.GeneralPurposeRegionFillerDrawBag.PickRandom( rand );
                    if ( miniTile.AvailableContent.Count <= 0 )
                        continue; //skip if no content present!
                    if ( miniTile.SizeX < regionOBBExtents.x && miniTile.SizeZ < regionOBBExtents.z )
                    {
                        //check it forward
                        levelTypeToSeed = miniTile;
                        break;
                    }
                    else if ( miniTile.SizeZ < regionOBBExtents.x && miniTile.SizeX < regionOBBExtents.z )
                    {
                        //check it reversed
                        levelTypeToSeed = miniTile;
                        break;
                    }
                }
            }
            #endregion

            int populatedBuildingCount = 0;
            if ( levelTypeToSeed != null && levelTypeToSeed.AvailableContent.Count > 0 )
            {
                PopulateRegion_FromMiniCell( startData, rand, tile, SubRegion, ShortID, levelTypeToSeed,
                    levelTypeToSeed.AvailableContent[rand.Next( 0, levelTypeToSeed.AvailableContent.Count )], addedBuildings, 
                    ParentPOIOrNull, ref populatedBuildingCount, tileBuildingTypesSoFar );
                //ArcenDebugging.LogSingleLine( "Seed mini tile: " + levelTypeToSeed.ID + " randNumForMiniTile: " + randNumForMiniTile +
                //    " less than " + ( tile.SeedingLogic?.PercentChanceOfGeneralMiniTileTypeAppearingOutOf100Thousand??0 ), Verbosity.DoNotShow );
            }

            if ( !SubRegion.Type.HasNSEWIndicators )
            {
                //just pack these in however!  We will start from the north, though
                PlaceableSeedingMegaGroup seedingGroup = tile.SeedingLogic.DrawRandomItem_NoNSEWIndicator( rand );

                Vector3 originPoint = startN;

                int buildingsAddedCount = populatedBuildingCount;
                int priorBuildingsAddedCount = 0;
                int loopCount = 0;
                //bool wouldHaveFailed = false;
                do
                {
                    priorBuildingsAddedCount = buildingsAddedCount;
                    GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, false, ParentPOIOrNull, ref buildingsAddedCount,
                        tileBuildingTypesSoFar );

                    if ( buildingsAddedCount == 0 )
                        seedingGroup = tile.SeedingLogic.DrawRandomItem_NoNSEWIndicator( rand );

                    //if ( buildingsAddedCount == 0 )
                    //    wouldHaveFailed = true;
                }
                while ( ( priorBuildingsAddedCount != addedBuildings.Count || buildingsAddedCount == 0 )
                    && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less

                if ( buildingsAddedCount == 0 )
                    ArcenDebugging.LogSingleLine( "Failed to add any buildings at non-oriented sGroup: '" + seedingGroup.ID + "'", Verbosity.ShowAsError );
                else
                {
                    //if ( wouldHaveFailed )
                    //    ArcenDebugging.LogSingleLine( "Would have failed to add any buildings at non-oriented sGroup: '" + 
                    //        seedingGroup.ID + "', but instead added " + buildingsAddedCount + ".", Verbosity.DoNotShow );
                }
            }
            else
            {
                PlaceableSeedingMegaGroup seedingGroup = tile.SeedingLogic.DrawRandomItem_Regular( rand );

                int buildingsAddedCount = populatedBuildingCount;
                if ( SubRegion.DirectionN )
                {
                    Vector3 originPoint = startN;

                    GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, true, ParentPOIOrNull, ref buildingsAddedCount,
                        tileBuildingTypesSoFar );
                }
                if ( SubRegion.DirectionS )
                {
                    Vector3 originPoint = startS;

                    GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.South, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, true, ParentPOIOrNull, ref buildingsAddedCount,
                        tileBuildingTypesSoFar );
                }
                if ( SubRegion.DirectionE )
                {
                    Vector3 originPoint = startE;

                    GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.East, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, true, ParentPOIOrNull, ref buildingsAddedCount,
                        tileBuildingTypesSoFar );
                }
                if ( SubRegion.DirectionW )
                {
                    Vector3 originPoint = startW;

                    GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.West, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, true, ParentPOIOrNull, ref buildingsAddedCount,
                        tileBuildingTypesSoFar );
                }

                if ( buildingsAddedCount == 0 )
                {
                    string originalSeedingGroup = seedingGroup.ID;
                    //just pack these in however!  We will start from the north, though
                    seedingGroup = tile.SeedingLogic.DrawRandomItem_NoNSEWIndicator( rand );

                    Vector3 originPoint = startN;

                    buildingsAddedCount = 0;
                    int priorBuildingsAddedCount = 0;
                    int loopCount = 0;
                    #region The One To Try Only If Failure
                    do
                    {
                        priorBuildingsAddedCount = buildingsAddedCount;
                        GenerateInitialBuildingSetInLocalSpace( startData, FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                            0.01f, 0.2f, rand, tile, addedBuildings, testedTypes, true, ParentPOIOrNull, ref buildingsAddedCount,
                            tileBuildingTypesSoFar );

                        if ( buildingsAddedCount == 0 )
                            seedingGroup = tile.SeedingLogic.DrawRandomItem_NoNSEWIndicator( rand );
                    }
                    while ( priorBuildingsAddedCount != addedBuildings.Count && loopCount++ <= 10 ); //add as long as we keep adding them, or 10 loops, whichever is less
                    #endregion

                    //if ( buildingsAddedCount > 0 )
                    //    ArcenDebugging.LogSingleLine( "Would have failed to add any buildings at non-oriented sGroup: '" +
                    //        originalSeedingGroup + "', but instead added " + buildingsAddedCount + ".", Verbosity.DoNotShow );

                    //if ( buildingsAddedCount == 0 )
                    {
                        PlaceableSeedingLogicType seedingLogic = tile.SeedingLogic.FallbackSeedingLogicType;
                        if ( seedingLogic != null )
                        {
                            seedingGroup = seedingLogic.DrawRandomItem_NoNSEWIndicator( rand );
                            do
                            {
                                priorBuildingsAddedCount = buildingsAddedCount;
                                GenerateExtraBuildingsInLocalSpace( startData, regionOBB, buildingsAddedCount == 0 ? 500 : 10, seedingGroup,
                                    rand, tile, addedBuildings, ParentPOIOrNull, testedTypes, ref buildingsAddedCount,
                                    tileBuildingTypesSoFar );

                                seedingGroup = seedingLogic.DrawRandomItem_NoNSEWIndicator( rand );
                            }
                            while ( (priorBuildingsAddedCount != addedBuildings.Count || buildingsAddedCount == 0) //&& addedBuildings.Count == 0
                                && loopCount++ <= 30 ); //add as long as we keep adding them, or 30 loops, whichever is less

                            if ( buildingsAddedCount == 0 )
                                ArcenDebugging.LogSingleLine( "Failed to add any buildings at oriented sGroup: '" +
                                    originalSeedingGroup + "', and the fallback of '" + seedingLogic.ID + "' also failed!", Verbosity.ShowAsError );

                            //if ( buildingsAddedCount > 0 )
                            //    ArcenDebugging.LogSingleLine( "Would have failed to add any buildings at non-oriented sGroup: '" +
                            //        originalSeedingGroup + "', but instead added " + buildingsAddedCount + ".", Verbosity.DoNotShow );
                        }
                        else
                        {
                            if ( buildingsAddedCount == 0 )
                                ArcenDebugging.LogSingleLine( "Failed to add any buildings at oriented sGroup: '" + originalSeedingGroup + "', and there was no fallback!", Verbosity.ShowAsError );
                        }
                    }
                }
            }

            if ( tile.SeedingLogic.ExtraBuildings1ToTryToSeedPerRegion > 0 )
            {
                PlaceableSeedingMegaGroup seedingGroup = tile.SeedingLogic.DrawRandomItem_Extra( rand );
                int buildingsAddedCount = 0;
                GenerateExtraBuildingsInLocalSpace( startData, regionOBB, tile.SeedingLogic.ExtraBuildings1ToTryToSeedPerRegion,
                    seedingGroup, rand, tile, addedBuildings, ParentPOIOrNull, testedTypes, ref buildingsAddedCount,
                    tileBuildingTypesSoFar );
            }

            if ( tile.SeedingLogic.ExtraBuildings2ToTryToSeedPerRegion > 0 )
            {
                PlaceableSeedingMegaGroup seedingGroup = tile.SeedingLogic.DrawRandomItem_Extra( rand );
                int buildingsAddedCount = 0;
                GenerateExtraBuildingsInLocalSpace( startData, regionOBB, tile.SeedingLogic.ExtraBuildings2ToTryToSeedPerRegion,
                    seedingGroup, rand, tile, addedBuildings, ParentPOIOrNull, testedTypes, ref buildingsAddedCount,
                    tileBuildingTypesSoFar );
            }

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile buildings done! " + tile.TileID + " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Structural).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion

        #region GenerateInitialBuildingSetInLocalSpace
        private static void GenerateInitialBuildingSetInLocalSpace( TaskStartData startData, FourDirection Dir,
            Vector3 currentPoint, Vector3 rightAxis, Vector3 lookAxis, OBBUnity regionOBB,
            PlaceableSeedingMegaGroup SeedMegaGroup, float MinSpacingBetween, float MaxSpacingBetween, MersenneTwister rand, MapTile tile, ThrowawayList<MapItem> addedBuildings,
            ThrowawayDictionary<int, bool> testedTypes, bool MoveLaterallyWhenHaveBuildingCollisions, MapPOI ParentPOIOrNull, ref int buildingsAddedCount, 
            ThrowawayDictionary<BuildingType, int> tileBuildingTypesSoFar )
        {
            if ( SeedMegaGroup == null )
                return;

            //these will be safely reused in the following areas
            MapUtility.GenerateOrientedZoneMovement( Dir, rightAxis, lookAxis, out Vector3 LateralMove, out Vector3 DepthMove );
            MapUtility.GetRegionXZForFitCheckingFromSideOrientation( Dir, regionOBB, out float regionSizeX, out float regionSizeZ );
            float remainRegionSizeX = regionSizeX;
            float remainRegionSizeZ = regionSizeZ;

            int addedBuildingCountWhenWeStarted = addedBuildings.Count;
            A5ObjectRoot priorRoot1 = null;
            A5ObjectRoot priorRoot2 = null;

            //this is all unique for each building being tested
            testedTypes.Clear();
            for ( int attemptCount = 0; attemptCount < 200; attemptCount++ )
            {
                A5ObjectRoot objRoot = SeedMegaGroup.DrawRandomItem( rand );
                if ( objRoot == null )
                {
                    ArcenDebugging.LogSingleLine( "Null objRoot in SeedMegaGroup '" + SeedMegaGroup.ID + "'", Verbosity.ShowAsError );
                    continue;
                }
                int innerTypeTestCount = 0;
                while ( ( objRoot == priorRoot1 || //not the same as the last one
                    objRoot == priorRoot2 ||
                    testedTypes.ContainsKey( objRoot.ObjectRootID ) ) && 
                    innerTypeTestCount++ < 400 ) //try to find something we've not tested yet
                {
                    objRoot = SeedMegaGroup.DrawRandomItem( rand );

                    if ( objRoot == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null objRoot in SeedMegaGroup '" + SeedMegaGroup.ID + "'", Verbosity.ShowAsError );
                        return;
                    }
                }

                if ( testedTypes.ContainsKey( objRoot.ObjectRootID ) )
                    break; //if we could not find anything, then we're done

                if ( objRoot.Building == null )
                {
                    ArcenDebugging.LogSingleLine( "Null Building on objRoot '" + objRoot.ID + "'", Verbosity.ShowAsError );
                    continue;
                }

                if ( objRoot.Building.Type.SoftMaxToSeedPerCity > 0 &&
                    objRoot.Building.Type.DuringMapgen_BuildingTypeCountsForOnesThatLimit >= objRoot.Building.Type.SoftMaxToSeedPerCity )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    continue; //we have too many of these, so skip it.  And skip any more checks
                }
                if ( objRoot.Building.Type.HardMaxToSeedPerTile > 0 &&
                    tileBuildingTypesSoFar[objRoot.Building.Type] >= objRoot.Building.Type.HardMaxToSeedPerTile )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    continue; //we have too many of these, so skip it.
                }

                int yRot = objRoot.GetRotationToFaceFront( Dir );
                MapUtility.GetFullXZForFitCheckingFromRotationY( objRoot, yRot, out float buildingX, out float buildingZ );
                if ( buildingX > remainRegionSizeX || buildingZ > remainRegionSizeZ )
                {
                    //testedTypes[objRoot.ObjectRootID] = true;
                    continue; //this won't fit, so skip it.  And skip any more checks
                }

                //this WILL fit, so far as we know right now.  So generate the instruction and see if it will fit well
                MapInstruction inst = MapUtility.GenerateInstructionForOrientedBuildingInOrientedZone( Dir, objRoot,
                    currentPoint, yRot, regionOBB, rightAxis, lookAxis, MapInstructionReason.TestData_Clickable );
                OBBUnity newOBB = objRoot.CalculateOBBForItem( inst.Position, inst.Rotation, inst.Scale );

                //remember this so it's not two in a row
                priorRoot2 = priorRoot1;
                priorRoot1 = objRoot;

                #region test against existing buildings we added this cycle
                if ( addedBuildingCountWhenWeStarted > 0 )
                {
                    bool foundAnyHits = false;
                    for ( int i = 0; i < addedBuildingCountWhenWeStarted; i++ )
                    {
                        MapItem existingItem = addedBuildings[i];
                        if ( existingItem.OBBCache.GetOBB_ExpensiveToUse().IntersectsOBB( newOBB ) )
                        {
                            //whoops, hit another building we added
                            foundAnyHits = true;

                            if ( MoveLaterallyWhenHaveBuildingCollisions )
                            {
                                //we're just trying to build a line of buildings, BUT we probably hit part of another line, so we need to move to the side.
                                //lets go ahead and move ourselves laterally by the smaller dimension of the other building just to be conservative about it
                                //if that means an extra loop, that's okay
                                float smallerSize = MathA.Min( existingItem.Type.OriginalAABBSize.x, existingItem.Type.OriginalAABBSize.z );
                                remainRegionSizeX -= smallerSize;
                                currentPoint += smallerSize * LateralMove;
                            }
                            else
                            {
                                //hey, we're building a grid of buildings!  So we're going to move back on the depth instead of laterally.
                                float smallerSize = MathA.Min( existingItem.Type.OriginalAABBSize.x, existingItem.Type.OriginalAABBSize.z );
                                remainRegionSizeZ -= smallerSize;
                                currentPoint += smallerSize * DepthMove;
                            }
                            break;
                        }
                    }
                    if ( foundAnyHits )
                        continue; //don't mark the current type as being invalid, because it could be valid again after this
                }
                #endregion

                if ( !regionOBB.ContainsOBB_Footprint( newOBB ) )
                    continue;

                //we don't actually ever submit the instruction, because we don't want to draw it that way!

                MapCell cell =  tile.FindCellForMapItem( inst.Position );

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = inst.Type;
                item.SetPosition( inst.Position );
                item.SetRotation( inst.Rotation );
                item.Scale = inst.Scale;
                item.OBBCache.SetToOBB( newOBB );
                item.SetParentPOI( ParentPOIOrNull );
                addedBuildings.Add( item );
                cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );
                tileBuildingTypesSoFar[objRoot.Building.Type]++;
                if ( objRoot.Building.Type.HardMaxToSeedPerTile == 2 && rand.Next( 0, 100 ) < 60 )
                    tileBuildingTypesSoFar[objRoot.Building.Type]++; //if cap of two, then 60% chance of lying and blocking half the cap.  Breaks up patterns

                float spacingToAdd = rand.NextFloat( MinSpacingBetween, MaxSpacingBetween );
                remainRegionSizeX -= (spacingToAdd + buildingX);
                currentPoint += (spacingToAdd + buildingX) * LateralMove;
                buildingsAddedCount++;
            }
        }
        #endregion

        #region PopulateRegion_FromMiniCell
        private static void PopulateRegion_FromMiniCell( TaskStartData startData, MersenneTwister rand, MapTile tile, MapSubRegion SubRegion,
            string ShortID, LevelType MiniCellType, ReferenceLevelData MiniCellSource, ThrowawayList<MapItem> addedBuildings, MapPOI LargerParentPOIOrNull,
            ref int PopulatedBuildingCount, ThrowawayDictionary<BuildingType, int> tileBuildingTypesSoFar )
        {
            OBBUnity regionOBB = SubRegion.OBBCache.GetOBB_ExpensiveToUse();
            float regionLargestSize = regionOBB.Size.LargestComponent();

            //decide which side we are starting from, which is arbitrary.  This leads to rotation in one of the cardinal directions
            FourDirection dir = (FourDirection)rand.Next( (int)FourDirection.North, (int)FourDirection.Length );
            float levelSizeX = MiniCellType.SizeX + MiniCellType.SizeX;
            float levelSizeZ = MiniCellType.SizeZ + MiniCellType.SizeZ;            

            //now calculate all of the region OBB bits from the direction we chose
            MapUtility.GetRegionXZForFitCheckingFromSideOrientation( dir, regionOBB, out float regionSizeX, out float regionSizeZ );

            if ( levelSizeX > regionSizeX || levelSizeZ > regionSizeZ )
            {
                dir++;
                if ( dir == FourDirection.Length )
                    dir = FourDirection.North;

                //recalculate after rotating!
                MapUtility.GetRegionXZForFitCheckingFromSideOrientation( dir, regionOBB, out regionSizeX, out regionSizeZ );

                if ( levelSizeX > regionSizeX || levelSizeZ > regionSizeZ )
                {
                    ArcenDebugging.LogSingleLine( "Tried rotating twice, and it did not work! levelSizeX: " + levelSizeX + " regionSizeX: " + regionSizeX +
                        " levelSizeZ: " + levelSizeZ + " regionSizeZ: " + regionSizeZ, Verbosity.ShowAsError );
                    return;
                }
            }

            float regionRotation = SubRegion.Rotation.y;
            switch ( dir )
            {
                case FourDirection.East:
                    regionRotation += 90;
                    break;
                case FourDirection.South:
                    regionRotation += 180;
                    break;
                case FourDirection.West:
                    regionRotation += 270;
                    break;
            }

            if ( regionRotation > 360 )
                regionRotation -= 360;

            float xSourceOffset = rand.NextFloat( 0, levelSizeX - regionSizeX ) / 2;
            float zSourceOffset = rand.NextFloat( 0, levelSizeZ - regionSizeZ ) / 2;

            ThrowawayDictionary<int, MapPathingPoint> pathfindingNodes = new ThrowawayDictionary<int, MapPathingPoint>( 40 );

            MapPOI effectivePOI = LargerParentPOIOrNull;

            if ( MiniCellType.POIToSpawn != null )
            {
                //ooh, looks like this is important enough to be a POI!
                Vector2 poiCenter = new Vector2( regionOBB.Center.x, regionOBB.Center.z );
                Vector2 poiWidthHeight = new Vector2( levelSizeX, levelSizeZ );
                float poiRotation = regionRotation;
                while ( poiRotation >= 90f )
                    poiRotation -= 90f;
                while ( poiRotation < 0 )
                    poiRotation += 90f;

                effectivePOI = MapPOI.Mapgen_CreateNewPOI_SubRegion( MiniCellType.POIToSpawn, tile, poiCenter, poiWidthHeight, poiRotation );
            }

            foreach ( ReferenceObject obj in MiniCellSource.AllObjects )
            {
                if ( obj.ObjRoot.IsMetaRegion )
                    continue; //we don't actually seed the meta regions from mini cells

                Vector3 objPos = obj.pos;
                Quaternion rot = Quaternion.Euler( obj.rot.x, obj.rot.y, obj.rot.z );
                MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), regionRotation );

                objPos = objPos.PlusX( regionOBB.Center.x - xSourceOffset );
                objPos = objPos.PlusZ( regionOBB.Center.z - zSourceOffset );

                Vector3 destPoint = objPos;
                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( objPos, rot, obj.scale );

                if ( !regionOBB.ContainsOBB_Footprint( newOBB ) )
                    continue;

                if ( obj.ObjRoot.IsPathingPoint )
                {
                    MapPathingPoint pathing = new MapPathingPoint();
                    pathing.Type = obj.ObjRoot;
                    pathing.Position = objPos;
                    pathing.Rotation = rot;
                    pathing.OBBCache.SetToOBB( newOBB );
                    pathing.PointID = obj.PathingLinkID;
                    pathfindingNodes[pathing.PointID] = pathing;

                    tile.PathingPoints.Add( pathing );
                    continue;
                }
                if ( obj.ObjRoot.IsPathingRegion )
                {
                    MapPathingRegion pathing = new MapPathingRegion();
                    pathing.Type = obj.ObjRoot;
                    pathing.Position = objPos;
                    pathing.Rotation = rot;
                    pathing.Scale = obj.scale;
                    pathing.OBBCache.SetToOBB( newOBB );
                    pathing.PathingType = obj.PathingType;
                    if ( pathing.PathingType == null )
                        ArcenDebugging.LogSingleLine( "Error!  MapPathingRegion from mini cell '" + MiniCellSource.SourceFilename + "' had a null PathingType.", Verbosity.ShowAsError );

                    tile.PathingZones.Add( pathing );
                    continue;
                }

                MapCell cell = tile.FindCellForMapItem( destPoint );

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = obj.ObjRoot;
                item.SetPosition( destPoint );
                item.SetRotation( rot );
                item.Scale = obj.scale;
                item.OBBCache.SetToOBB( newOBB );
                tile.workingPossibleObjectCollisions_All.Add( item );

                if ( obj.ObjRoot.Building != null )
                {
                    item.SetParentPOI( effectivePOI );
                    cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );

                    tileBuildingTypesSoFar[obj.ObjRoot.Building.Type]++; //keep track of them, but do not limit on them
                    if ( obj.ObjRoot.Building.Type.HardMaxToSeedPerTile == 2 && rand.Next( 0, 100 ) < 60 )
                        tileBuildingTypesSoFar[obj.ObjRoot.Building.Type]++; //if cap of two, then 60% chance of lying and blocking half the cap.  Breaks up patterns
                }
                else
                    cell.PlaceMapItemIntoCell( obj.ObjRoot.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor :
                        (obj.ObjRoot.ExtraPlaceableData.IsFence ? TileDest.Fences : TileDest.OtherSkeletonItems), item, true );

                //whether it is a building or not, treat it like it is for a mini-tile
                addedBuildings.Add( item );
                PopulatedBuildingCount++;
            }

            if ( pathfindingNodes.Count > 0 )
            {
                foreach ( KeyValuePair<int, MapPathingPoint> outerKV in pathfindingNodes )
                {
                    int pathingLinkID = outerKV.Key;
                    foreach ( RefPair<int, int> kv in MiniCellSource.PathingLinks )
                    {
                        int otherID = -1;
                        if ( kv.LeftItem == pathingLinkID )
                            otherID = kv.RightItem;
                        else if ( kv.RightItem == pathingLinkID )
                            otherID = kv.LeftItem;
                        else
                            continue;

                        MapPathingPoint otherObj = pathfindingNodes[otherID];
                        outerKV.Value.ConnectPathPoints( otherObj );
                    }
                }
            }

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile mini-tile done! " + tile.TileID + " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Structural).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion

        #region GenerateExtraBuildingsInLocalSpace
        private static void GenerateExtraBuildingsInLocalSpace( TaskStartData startData, OBBUnity regionOBB, int NumberToTryToSeed,
            PlaceableSeedingMegaGroup SeedMegaGroup, MersenneTwister rand, MapTile tile, ThrowawayList<MapItem> addedBuildings,
            MapPOI ParentPOIOrNull, ThrowawayDictionary<int, bool> testedTypes, ref int buildingsAddedCount, 
            ThrowawayDictionary<BuildingType, int> tileBuildingTypesSoFar )
        {            
            A5ObjectRoot priorRoot1 = null;
            A5ObjectRoot priorRoot2 = null;

            int regionRot = Mathf.RoundToInt( regionOBB.Rotation.eulerAngles.y );
            float minX = regionOBB.Center.x - regionOBB.Extents.x;
            float maxX = regionOBB.Center.x + regionOBB.Extents.x;
            float minZ = regionOBB.Center.z - regionOBB.Extents.z;
            float maxZ = regionOBB.Center.z + regionOBB.Extents.z;

            int actuallyAdded = 0;
            testedTypes.Clear();
            for ( int attemptCount = 0; attemptCount < NumberToTryToSeed; attemptCount++ )
            {
                A5ObjectRoot objRoot = SeedMegaGroup.DrawRandomItem( rand );
                if ( objRoot == null )
                {
                    ArcenDebugging.LogSingleLine( "Null objRoot in SeedMegaGroup '" + SeedMegaGroup.ID + "'", Verbosity.ShowAsError );
                    continue;
                }
                int innerTypeTestCount = 0;
                while ( (objRoot == priorRoot1 || //not the same as the last one
                    objRoot == priorRoot2 ||
                    testedTypes.ContainsKey( objRoot.ObjectRootID )) &&
                    innerTypeTestCount++ < 400 ) //try to find something we've not tested yet
                {
                    objRoot = SeedMegaGroup.DrawRandomItem( rand );

                    if ( objRoot == null )
                    {
                        ArcenDebugging.LogSingleLine( "Null objRoot in SeedMegaGroup '" + SeedMegaGroup.ID + "'", Verbosity.ShowAsError );
                        return;
                    }
                }

                if ( testedTypes.ContainsKey( objRoot.ObjectRootID ) )
                    break; //if we could not find anything, then we're done

                if ( objRoot.Building == null )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    ArcenDebugging.LogSingleLine( "Null Building on objRoot '" + objRoot.ID + "'", Verbosity.ShowAsError );
                    continue;
                }

                if ( objRoot.Building.Type.SoftMaxToSeedPerCity > 0 &&
                    objRoot.Building.Type.DuringMapgen_BuildingTypeCountsForOnesThatLimit >= objRoot.Building.Type.SoftMaxToSeedPerCity )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    continue; //we have too many of these, so skip it.
                }
                if ( objRoot.Building.Type.HardMaxToSeedPerTile > 0 && 
                    tileBuildingTypesSoFar[objRoot.Building.Type] >= objRoot.Building.Type.HardMaxToSeedPerTile )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    continue; //we have too many of these, so skip it.
                }

                if ( objRoot.OriginalAABBSize.x > maxX - minX ||
                    objRoot.OriginalAABBSize.z > maxZ - minZ )
                {
                    testedTypes[objRoot.ObjectRootID] = true;
                    continue; //this is too big for here, so skip it
                }

                int yRot = regionRot;
                float buildingX = rand.NextFloat( minX + objRoot.OriginalAABBHalfSize.x, maxX - objRoot.OriginalAABBHalfSize.x );
                float buildingZ = rand.NextFloat( minZ + objRoot.OriginalAABBHalfSize.z, maxZ - objRoot.OriginalAABBHalfSize.z );

                MapInstruction inst;
                inst.Type = objRoot;
                inst.Position = new Vector3( buildingX, 0, buildingZ );
                float alwaysDropTo = objRoot.AlwaysDropTo;
                if ( alwaysDropTo > -900f )
                    inst.Position = inst.Position.ReplaceY( alwaysDropTo );

                inst.Scale = objRoot.OriginalScale;
                inst.Rotation = regionOBB.Rotation;
                inst.Reason = MapInstructionReason.TestData_Clickable;
                inst.DebugText = string.Empty;

                OBBUnity newOBB = objRoot.CalculateOBBForItem( inst.Position, inst.Rotation, inst.Scale );

                //remember this so it's not two in a row
                priorRoot2 = priorRoot1;
                priorRoot1 = objRoot;

                #region test against existing buildings we added this cycle
                {
                    bool foundAnyHits = false;
                    for ( int i = 0; i < addedBuildings.Count; i++ )
                    {
                        MapItem existingItem = addedBuildings[i];
                        if ( existingItem.OBBCache.GetOBB_ExpensiveToUse().IntersectsOBB( newOBB ) )
                        {
                            //whoops, hit another building we added
                            foundAnyHits = true;
                            break;
                        }
                    }
                    if ( foundAnyHits )
                        continue; //don't mark the current type as being invalid, because it could be valid again after this
                }
                #endregion

                if ( !regionOBB.ContainsOBB_Footprint( newOBB ) )
                    continue;

                //we don't actually ever submit the instruction, because we don't want to draw it that way!

                MapCell cell = tile.FindCellForMapItem( inst.Position );

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = inst.Type;
                item.SetPosition( inst.Position );
                item.SetRotation( inst.Rotation );
                item.Scale = inst.Scale;
                item.OBBCache.SetToOBB( newOBB );
                item.SetParentPOI( ParentPOIOrNull );
                addedBuildings.Add( item );
                cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );
                tileBuildingTypesSoFar[objRoot.Building.Type]++;
                if ( objRoot.Building.Type.HardMaxToSeedPerTile == 2 && rand.Next( 0, 100 ) < 60 )
                    tileBuildingTypesSoFar[objRoot.Building.Type]++; //if cap of two, then 60% chance of lying and blocking half the cap.  Breaks up patterns
                actuallyAdded++;
                buildingsAddedCount++;
            }

            //ArcenDebugging.LogSingleLine( "Seeded " + actuallyAdded + " out of desired " + NumberToTryToSeed + " items in " + SeedMegaGroup.ID, Verbosity.DoNotShow );
        }
        #endregion

        #region FinishTileSetup
        public static void FinishTileSetup( MapTile tile )
        {
            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile complete! " + tile.TileID + 
                    " builds-out: " + tile.buildingZonesStillOutstanding +
                    " deco-out: " + tile.decorationZonesStillOutstanding +
                    " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Structural).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion
    }
}

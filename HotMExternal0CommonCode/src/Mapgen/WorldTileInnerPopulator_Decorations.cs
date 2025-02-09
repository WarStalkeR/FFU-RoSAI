using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    internal static class WorldTileInnerPopulator_Decorations
    {
        public static bool Debug_LogTileCompletions = false;

        private const int MAX_TILES_AT_ONCE = 10;

        #region DoPerFrame
        public static void DoPerFrame()
        {
            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //Willard reports that sometimes the mapgen and the tile filling race, so this waits until they are done
            if ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                return; //if we haven't yet finished initializing absolutely everything, then don't run this yet

            if ( CityMap.TilesBeingPostProcessed_Decorations > MAX_TILES_AT_ONCE )
                return;

            if ( CityMap.TilesToPostProcess_Decoration.Count == 0 && CityMap.WorkingTilesToPostProcess_Decoration.Count == 0 )
                return; //nothing to do

            //move tiles from the threadsafe queue to the working list that only our thread uses
            while ( CityMap.TilesToPostProcess_Decoration.TryDequeue( out MapTile tile ) )
                CityMap.WorkingTilesToPostProcess_Decoration.Add( tile );

            Debug_LogTileCompletions = GameSettings.Current.GetBool( "Debug_LogTileCompletions" );

            while ( CityMap.WorkingTilesToPostProcess_Decoration.Count > 0 )
            {
                Interlocked.Increment( ref CityMap.TilesBeingPostProcessed_Decorations );

                MapTile tile = CityMap.WorkingTilesToPostProcess_Decoration[0];
                CityMap.WorkingTilesToPostProcess_Decoration.RemoveAt( 0 );

                tile.timeStartedPopulation_Decoration = ArcenTime.AnyTimeSinceStartF;
                int randSeed = Engine_Universal.PermanentQualityRandom.Next();

                if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.DoDecorationPopulationOfTile",
                    ( TaskStartData startData ) =>
                    {

                        try
                        {
                            DoPopulationOfTile_OnBackgroundThread( startData, randSeed, tile );
                        }
                        catch ( Exception e )
                        {
                            if ( !startData.IsMeantToSilentlyDie() )
                                ArcenDebugging.LogWithStack( "WorldTileInnerPopulator_Decorations.DoPerFrame_Decorations background thread error: " + e, Verbosity.ShowAsError );
                        }
                        finally //normally we avoid finally, but otherwise this won't happen on thread abort
                        {
                            Interlocked.Decrement( ref CityMap.TilesBeingPostProcessed_Decorations );
                        }
                        //ArcenDebugging.LogSingleLine( "CityMap.TilesBeingPostProcessed: " + CityMap.TilesBeingPostProcessed, Verbosity.ShowAsError );

                    } ) )
                {
                    //failed to start the above
                    Interlocked.Decrement( ref CityMap.TilesBeingPostProcessed_Decorations );
                    CityMap.WorkingTilesToPostProcess_Decoration.Add( tile );
                }

                if ( CityMap.TilesBeingPostProcessed_Decorations >= MAX_TILES_AT_ONCE )
                    return;
            }
        }
        #endregion

        #region DoPopulationOfTile_OnBackgroundThread
        private static void DoPopulationOfTile_OnBackgroundThread( TaskStartData startData, int randSeed, MapTile tile )
        {
            MersenneTwister rand = new MersenneTwister( randSeed );

            tile.workingPossibleObjectCollisions_All.Clear();
            tile.workingPossibleObjectCollisions_Current.Clear();

            if ( startData.IsMeantToSilentlyDie() )
                return; //this is a thread that was killed!

            if ( tile.IsOutOfBoundsTile )
            {

            }
            else if ( tile.TileType == null || tile.IsHoleTile )
            {
                DoPopulationOfHoleTile( startData, rand, tile );
            }
            else
            {
                PopulateDecorationZonesForTile( startData, tile );
            }

            FinishTileSetup( tile );
        }
        #endregion

        #region DoPopulationOfHoleTile
        private static void DoPopulationOfHoleTile( TaskStartData startData, MersenneTwister rand, MapTile tile )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;
                ArcenFloatRectangle rectToDisallow = new ArcenFloatRectangle();
                bool usesDisallowedRect = false;
                debugStage = 200;
                if ( tile.HoleLevelType != null )
                {
                    debugStage = 300;
                    float levelSizeX = tile.HoleLevelType.SizeX;
                    float levelSizeZ = tile.HoleLevelType.SizeZ;

                    debugStage = 400;
                    Vector3 tileCenter = tile.Center;
                    debugStage = 500;
                    rectToDisallow = ArcenFloatRectangle.CreateFromCenterAndHalfSize( tileCenter.x, tileCenter.z, levelSizeX, levelSizeZ );
                    usesDisallowedRect = true;

                    debugStage = 900;
                    //hole tiles filled like this will have decoration zones!
                    PopulateDecorationZonesForTile( startData, tile );
                }

                debugStage = 2100;
                foreach ( MapCell cell in tile.CellsList )
                {
                    debugStage = 2200;
                    tile.LevelSourceFilenameForTile = ArcenIO.GetFileNameWithoutExtension( cell.NonSerialized_HoleTileDecorationSeedingFile );
                    debugStage = 2300;
                    {
                        debugStage = 2400;
                        ReferenceLevelData levelData = LevelTypeTable.Instance.ReferenceLevelLookup[cell.NonSerialized_HoleTileDecorationSeedingFile];
                        if ( levelData == null )
                            continue;

                        debugStage = 2500;
                        PopulateFullMapCell_Decoration( startData, cell.NonSerialized_HoleTileDecorationRandomSeed, tile, cell, levelData.ParentLevelType, levelData,
                            rectToDisallow, usesDisallowedRect );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "Decoration-DoPopulationOfHoleTile", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region PopulateDecorationZonesForTile
        public static void PopulateDecorationZonesForTile( TaskStartData startData, MapTile tile )
        {
            if ( tile == null )
                return;
            //ArcenDebugging.LogSingleLine( "PopulateDecorationZonesForTile:  " + tile.TopLeftPoint + 
            //    (tile.HoleTile ? " (hole tile)" : tile.TileType == null ? " (null tile)" : " (normal tile)") + 
            //    " metas: " + tile.SkeletonMetas.Count, Verbosity.DoNotShow );

            if ( tile.SkeletonMetas.Count == 0 )
                return; //if no metas, then nothing to do!

            //first find all of the workingPossibleObjectCollisions
            tile.workingPossibleObjectCollisions_All.Clear();
            foreach ( MapCell cell in tile.CellsList )
            {
                if ( cell == null )
                    continue;

                if ( cell.AllRoads.Count > 0 )
                    tile.workingPossibleObjectCollisions_All.AddRange( cell.AllRoads);
                if ( cell.BuildingDict.Count > 0 )
                {
                    foreach ( KeyValuePair<int, MapItem> kv in cell.BuildingDict )
                    {
                        if ( kv.Value != null )
                            tile.workingPossibleObjectCollisions_All.Add( kv.Value );
                    }
                }
                if ( cell.OtherSkeletonItems.Count > 0 )
                    tile.workingPossibleObjectCollisions_All.AddRange( cell.OtherSkeletonItems );
                if ( cell.Fences.Count > 0 )
                    tile.workingPossibleObjectCollisions_All.AddRange( cell.Fences );
                if ( cell.DecorationMajor.Count > 0 )
                    tile.workingPossibleObjectCollisions_All.AddRange( cell.DecorationMajor );
                if ( cell.DecorationMinor.Count > 0 )
                    tile.workingPossibleObjectCollisions_All.AddRange( cell.DecorationMinor );
            }

            //ArcenDebugging.LogSingleLine( "PopulateDecorationZonesForTile:  " + tile.TopLeftPoint +
            //    " workingPossibleObjectCollisions: " + tile.workingPossibleObjectCollisions.Count, Verbosity.DoNotShow );

            //then populate all of the regions
            foreach ( MapSubRegion subRegion in tile.SkeletonMetas )
            {
                if ( subRegion == null )
                    continue;
                PlaceableExtraData extraData = subRegion?.Type?.ExtraPlaceableData;
                if ( extraData == null )
                    continue;

                if ( extraData.MetaOnly_HidesWhenDecorationZonesHidden )
                {
                    //if we are a decoration zone
                    //if ( SkipLoadingDecorations_DecorationZones )
                    //    continue;
                }
                else
                {
                    //if we are a building zone
                    //if ( SkipLoadingDecorations_BuildingZones )
                    //    continue;
                }

                //ArcenDebugging.LogSingleLine( "PopulateDecorationZonesForTile:  " + tile.TopLeftPoint +
                //    (tile.HoleTile ? " (hole tile)" : tile.TileType == null ? " (null tile)" : " (normal tile)") +
                //    " DecorationSeedingFile: " + ( subRegion.DecorationSeedingFile == null ? "null" : subRegion.DecorationSeedingFile ), Verbosity.DoNotShow );

                if ( subRegion.DecorationSeedingFile == null || subRegion.DecorationSeedingFile.Length == 0 )
                    continue;

                ReferenceLevelData levelData = LevelTypeTable.Instance.ReferenceLevelLookup[subRegion.DecorationSeedingFile];
                PopulateRegion_Decoration( startData, true, subRegion.DecorationRandomSeed, tile, subRegion,
                    extraData.MetaOnly_ShortID, levelData.ParentLevelType, levelData );
            }
        }
        #endregion

        #region PopulateRegion_Decoration
        private static void PopulateRegion_Decoration( TaskStartData startData, bool isForExistingBuildingZone, int randSeed, MapTile tile, MapSubRegion SubRegion, 
            string ShortID, LevelType ChosenType, ReferenceLevelData DecorationSource )
        {
            if ( ChosenType.AvailableContent.Count == 0 )
            {
                ArcenDebugging.LogSingleLine( "No available files in ChosenType '" + ChosenType.ID + "' for tile!", Verbosity.ShowAsError );
                return;
            }

            OBBUnity regionOBB = SubRegion.OBBCache.GetOBB_ExpensiveToUse();
            float regionLargestSize = regionOBB.Size.LargestComponent();
            MersenneTwister rand = new MersenneTwister( randSeed );

            //decide which side we are starting from, which is arbitrary.  This leads to rotation in one of the cardinal directions
            FourDirection dir = (FourDirection)rand.Next( (int)FourDirection.North, (int)FourDirection.Length );
            //now calculate all of the region OBB bits from the direction we chose
            MapUtility.GetRegionXZForFitCheckingFromSideOrientation( dir, regionOBB, out float regionSizeX, out float regionSizeZ );

            float levelSizeX = ChosenType.SizeX + ChosenType.SizeX;
            float levelSizeZ = ChosenType.SizeZ + ChosenType.SizeZ;

            if ( levelSizeX < regionSizeX )
            {
                ArcenDebugging.LogSingleLine( "Level size X " + levelSizeX + " was less than regionSizeX " + regionSizeX +
                    " when looking at a meta region zone here, and the level type " + ChosenType.ID, Verbosity.ShowAsError );
                return;
            }
            if ( levelSizeZ < regionSizeZ )
            {
                ArcenDebugging.LogSingleLine( "Level size Z " + levelSizeZ + " was less than regionSizeZ " + regionSizeZ +
                    " when looking at a meta region zone here, and the level type " + ChosenType.ID, Verbosity.ShowAsError );
                return;
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

            Vector3 tileCenter = tile.Center;
            ArcenFloatRectangle levelRect = ArcenFloatRectangle.CreateFromCenterAndHalfSize( tileCenter.x, tileCenter.z, tile.HalfSizeX, 
                 tile.HalfSizeZ );

            float xSourceOffset = rand.NextFloat( 0, levelSizeX - regionSizeX ) / 2;
            float zSourceOffset = rand.NextFloat( 0, levelSizeZ - regionSizeZ ) / 2;

            #region Fill workingPossibleObjectCollisions_Current As A Subset
            //we need to use an inflated OBB, because things like roads on the edges will otherwise be missed (of course)
            //this keeps the performance up, but also gets us the extra hits that we want
            OBBUnity regionOBBInflated = new OBBUnity( regionOBB );
            regionOBBInflated.Size *= 1.3f;
            float regionLargestSizeInflated = regionLargestSize * 1.3f;

            tile.workingPossibleObjectCollisions_Current.Clear();
            foreach ( MapItem existing in tile.workingPossibleObjectCollisions_All )
            {
                OBBUnity obb = existing.OBBCache.GetOBB_ExpensiveToUse();
                float newOBBMaxSize = obb.Size.LargestComponent();
                newOBBMaxSize += newOBBMaxSize; //double it for a fudge factor

                //looking for a cheap early exclusion
                if ( (regionOBBInflated.Center - obb.Center).sqrMagnitude > (newOBBMaxSize + regionLargestSizeInflated) * (newOBBMaxSize + regionLargestSizeInflated) )
                    continue;
                if ( !obb.IntersectsOBB( regionOBBInflated ) )
                    continue; //if this is not part of the region's area, then skip it!

                tile.workingPossibleObjectCollisions_Current.Add( existing );
            }
            #endregion

            foreach ( ReferenceObject obj in DecorationSource.AllObjects )
            {
                if ( obj.ObjRoot == null ) 
                    continue;
                if ( obj.ObjRoot.IsMetaRegion )
                    continue; //we don't actually seed the meta regions from decoration zones
                else if ( obj.ObjRoot.IsPathingPoint || obj.ObjRoot.IsPathingRegion )
                    continue; //we skip these, also

                Vector3 objPos = obj.pos;
                Quaternion rot = Quaternion.Euler( obj.rot.x, obj.rot.y, obj.rot.z );
                MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), regionRotation );

                objPos = objPos.PlusX( regionOBB.Center.x - xSourceOffset );
                objPos = objPos.PlusZ( regionOBB.Center.z - zSourceOffset );

                Vector3 destPoint = objPos;

                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( objPos, rot, obj.scale );

                float newOBBMaxSize = newOBB.Size.LargestComponent();
                newOBBMaxSize += newOBBMaxSize; //double it for a fudge factor
                
                //looking for a cheap early exclusion
                if ( (regionOBB.Center - newOBB.Center).sqrMagnitude > ( newOBBMaxSize + regionLargestSize ) * (newOBBMaxSize + regionLargestSize ) )
                    continue;

                if ( !newOBB.IntersectsOBB( regionOBB ) )
                    continue; //if this is not part of the region's area, then skip it!

                ArcenFloatRectangle boundsRect = ArcenFloatRectangle.CreateFromBoundsXZ( new Bounds( newOBB.Center, newOBB.Size ) );

                if ( !levelRect.ContainsRectangle( boundsRect ) ) //if one rectangle does not contain the other, then out of bounds
                    continue;

                #region test against anything and everything to make sure there are no hits!
                if ( tile.workingPossibleObjectCollisions_Current.Count > 0 )
                {
                    bool foundAnyHits = false;

                    foreach ( MapItem existingItem in tile.workingPossibleObjectCollisions_Current )
                    {
                        //looking for a cheap early exclusion
                        float existingLargestSize = existingItem.OBBCache.OBBSize.LargestComponent();
                        if ( (existingItem.OBBCache.Center - newOBB.Center).sqrMagnitude > (newOBBMaxSize + existingLargestSize) * (newOBBMaxSize + existingLargestSize) )
                            continue;

                        if ( existingItem.OBBCache.GetOBB_ExpensiveToUse().IntersectsOBB( newOBB ) )
                        {
                            //whoops, hit another object we added
                            if ( existingItem.Type.ExtraPlaceableData.RequiresIndividualColliderCheckingDuringBoundingCollisionChecks ||
                                existingItem.Type.CollisionBoxes.Count > 0 ) //if we have the extra detail, then let's use it, regardless
                            {
                                int rotY = Mathf.RoundToInt( existingItem.OBBCache.GetOBB_ExpensiveToUse().Rotation.eulerAngles.y );
                                bool hadActualInnerCollision = false;
                                //OBB collision is not good enough, but it's a start.  Now that we know our OBBs intersect, we have to ask -- does that also happen with any of our
                                //colliders on here?
                                foreach ( CollisionBox box in existingItem.Type.CollisionBoxes )
                                {
                                    Vector3 worldCenter = existingItem.TransformPoint_Threadsafe( box.Center );

                                    Vector3 worldSize = box.Size.ComponentWiseMultWithSimpleYRot( existingItem.Scale, rotY );

                                    if ( existingItem.Type.ExtraPlaceableData.ExpandIndividualColliderCheckingUpAndDownALotInCollisionChecks )
                                    {
                                        //make sure that the Y is very tall and we don't miss it for that reason, but only if specified
                                        worldSize = worldSize.ReplaceY( 10 );
                                    }

                                    if ( BoxMath.BoxIntersectsBox( newOBB.Center, newOBB.Size, newOBB.Rotation,
                                        worldCenter, worldSize, existingItem.Rotation ) )
                                    {
                                        hadActualInnerCollision = true;
                                        break;
                                    }
                                }

                                if ( hadActualInnerCollision )
                                {
                                    //we DID have a collision, so warn us now!
                                    foundAnyHits = true;
                                    break;
                                }
                            }
                            else
                            {
                                //OBB collision is good enough for us, let's move on.
                                foundAnyHits = true;
                                break;
                            }
                        }
                        if ( foundAnyHits )
                            break; //stop on the first hit found
                    }
                    if ( foundAnyHits )
                        continue;
                }
                #endregion

                MapCell cell = tile.FindCellForMapItem( destPoint );

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = obj.ObjRoot;
                item.Position = destPoint;
                item.Rotation = rot;
                item.Scale = obj.scale;
                item.OBBCache.SetToOBB( newOBB );
                tile.workingPossibleObjectCollisions_All.Add( item );
                if ( obj.ObjRoot.Building != null )
                    cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );
                else
                    cell.PlaceMapItemIntoCell( obj.ObjRoot.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor : TileDest.DecorationMajor, item, true );
            }
            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile decoration done! " + tile.TileID + " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Decoration).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion

        #region PopulateFullMapCell_Decoration
        private static void PopulateFullMapCell_Decoration( TaskStartData startData, int randSeed, MapTile tile, MapCell outerCell, LevelType ChosenType,
            ReferenceLevelData DecorationSource, ArcenFloatRectangle rectToDisallow, bool usesDisallowedRect )
        {
            MersenneTwister rand = new MersenneTwister( randSeed );

            //decide which side we are starting from, which is arbitrary.  This leads to rotation in one of the cardinal directions
            FourDirection dir = (FourDirection)rand.Next( (int)FourDirection.North, (int)FourDirection.Length );
            float cellSizeXZ = CityMap.CELL_FULL_SIZE;
            Vector3 cellCenter = outerCell.Calculate_Center();

            float levelSizeX = ChosenType.SizeX + ChosenType.SizeX;
            float levelSizeZ = ChosenType.SizeZ + ChosenType.SizeZ;

            if ( levelSizeX < cellSizeXZ )
            {
                ArcenDebugging.LogSingleLine( "Level size X " + levelSizeX + " was less than cellSizeXZ " + cellSizeXZ +
                    " when looking at a meta region zone here, and the level type " + ChosenType.ID, Verbosity.ShowAsError );
                return;
            }
            if ( levelSizeZ < cellSizeXZ )
            {
                ArcenDebugging.LogSingleLine( "Level size Z " + levelSizeZ + " was less than cellSizeXZ " + cellSizeXZ +
                    " when looking at a meta region zone here, and the level type " + ChosenType.ID, Verbosity.ShowAsError );
                return;
            }

            float regionRotation = 0;
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

            Vector3 tileCenter = tile.Center;
            ArcenFloatRectangle levelRect = ArcenFloatRectangle.CreateFromCenterAndHalfSize( tileCenter.x, tileCenter.z, tile.HalfSizeX,
                 tile.HalfSizeZ );

            float xSourceOffset = rand.NextFloat( 0, levelSizeX - cellSizeXZ ) / 2;
            float zSourceOffset = rand.NextFloat( 0, levelSizeZ - cellSizeXZ ) / 2;

            #region Fill workingPossibleObjectCollisions_Current As A Subset
            tile.workingPossibleObjectCollisions_Current.Clear();
            foreach ( MapItem existing in tile.workingPossibleObjectCollisions_All )
            {
                OBBUnity obb = existing.OBBCache.GetOBB_ExpensiveToUse();
                float newOBBMaxSize = obb.Size.LargestComponent();
                newOBBMaxSize += newOBBMaxSize; //double it for a fudge factor

                //looking for a cheap early exclusion
                if ( (cellCenter - obb.Center).sqrMagnitude > (newOBBMaxSize + cellSizeXZ) * (newOBBMaxSize + cellSizeXZ) )
                    continue;

                ArcenFloatRectangle boundsRect = existing.OBBCache.GetOuterRect();

                if ( usesDisallowedRect )
                {
                    if ( rectToDisallow.BasicIntersectionTest( boundsRect ) )
                        continue; //if these overlap, then skip them!
                }

                if ( !levelRect.BasicIntersectionTest( boundsRect ) )
                    continue; //if this is not part of the level's area, then skip it!

                tile.workingPossibleObjectCollisions_Current.Add( existing );
            }
            #endregion

            foreach ( ReferenceObject obj in DecorationSource.AllObjects )
            {
                if ( obj.ObjRoot.IsMetaRegion )
                    continue; //we don't actually seed the meta regions from decoration zones
                else if ( obj.ObjRoot.IsPathingPoint || obj.ObjRoot.IsPathingRegion )
                    continue; //we skip these, also

                Vector3 objPos = obj.pos;
                Quaternion rot = Quaternion.Euler( obj.rot.x, obj.rot.y, obj.rot.z );
                MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), regionRotation );

                objPos = objPos.PlusX( cellCenter.x - xSourceOffset );
                objPos = objPos.PlusZ( cellCenter.z - zSourceOffset );

                Vector3 destPoint = objPos;

                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( objPos, rot, obj.scale );

                float newOBBMaxSize = newOBB.Size.LargestComponent();
                newOBBMaxSize += newOBBMaxSize; //double it for a fudge factor

                //looking for a cheap early exclusion
                if ( (cellCenter - newOBB.Center).sqrMagnitude > (newOBBMaxSize + cellSizeXZ) * (newOBBMaxSize + cellSizeXZ) )
                    continue;

                ArcenFloatRectangle boundsRect = ArcenFloatRectangle.CreateFromBoundsXZ( new Bounds( newOBB.Center, newOBB.Size ) );

                if ( usesDisallowedRect )
                {
                    if ( rectToDisallow.BasicIntersectionTest( boundsRect ) )
                        continue; //if these overlap, then skip them!
                }

                if ( !levelRect.ContainsRectangle( boundsRect ) ) //if one rectangle does not contain the other, then out of bounds
                    continue;

                #region test against anything and everything to make sure there are no hits!
                if ( tile.workingPossibleObjectCollisions_Current.Count > 0 )
                {
                    bool foundAnyHits = false;

                    foreach ( MapItem existingItem in tile.workingPossibleObjectCollisions_Current )
                    {
                        //looking for a cheap early exclusion
                        float existingLargestSize = existingItem.OBBCache.OBBSize.LargestComponent();
                        if ( (existingItem.OBBCache.Center - newOBB.Center).sqrMagnitude > (newOBBMaxSize + existingLargestSize) * (newOBBMaxSize + existingLargestSize) )
                            continue;

                        if ( existingItem.OBBCache.GetOBB_ExpensiveToUse().IntersectsOBB( newOBB ) )
                        {
                            //whoops, hit another object we added
                            if ( existingItem.Type.ExtraPlaceableData.RequiresIndividualColliderCheckingDuringBoundingCollisionChecks ||
                                existingItem.Type.CollisionBoxes.Count > 0 ) //if we have the extra detail, then let's use it, regardless
                            {
                                int rotY = Mathf.RoundToInt( existingItem.OBBCache.GetOBB_ExpensiveToUse().Rotation.eulerAngles.y );
                                bool hadActualInnerCollision = false;
                                //OBB collision is not good enough, but it's a start.  Now that we know our OBBs intersect, we have to ask -- does that also happen with any of our
                                //colliders on here?
                                foreach ( CollisionBox box in existingItem.Type.CollisionBoxes )
                                {
                                    Vector3 worldCenter = existingItem.TransformPoint_Threadsafe( box.Center );

                                    Vector3 worldSize = box.Size.ComponentWiseMultWithSimpleYRot( existingItem.Scale, rotY );

                                    if ( existingItem.Type.ExtraPlaceableData.ExpandIndividualColliderCheckingUpAndDownALotInCollisionChecks )
                                    {
                                        //make sure that the Y is very tall and we don't miss it for that reason, but only if specified
                                        worldSize = worldSize.ReplaceY( 10 );
                                    }

                                    if ( BoxMath.BoxIntersectsBox( newOBB.Center, newOBB.Size, newOBB.Rotation,
                                        worldCenter, worldSize, existingItem.Rotation ) )
                                    {
                                        hadActualInnerCollision = true;
                                        break;
                                    }
                                }

                                if ( hadActualInnerCollision )
                                {
                                    //we DID have a collision, so warn us now!
                                    foundAnyHits = true;
                                    break;
                                }
                            }
                            else
                            {
                                //OBB collision is good enough for us, let's move on.
                                foundAnyHits = true;
                                break;
                            }
                        }
                        if ( foundAnyHits )
                            break; //stop on the first hit found
                    }
                    if ( foundAnyHits )
                        continue;
                }
                #endregion

                MapCell cell = tile.FindCellForMapItem( destPoint );

                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                item.Type = obj.ObjRoot;
                item.Position = destPoint;
                item.Rotation = rot;
                item.Scale = obj.scale;
                item.OBBCache.SetToOBB( newOBB );
                tile.workingPossibleObjectCollisions_All.Add( item );
                if ( obj.ObjRoot.Building != null )
                    cell.PlaceMapItemIntoCell( TileDest.Buildings, item, true );
                else
                    cell.PlaceMapItemIntoCell( obj.ObjRoot.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor : TileDest.DecorationMajor, item, true );
            }
            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile decoration done! " + tile.TileID + " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Decoration).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion

        #region FinishTileSetup
        public static void FinishTileSetup( MapTile tile )
        {
            tile.workingPossibleObjectCollisions_All.Clear();
            tile.workingPossibleObjectCollisions_Current.Clear();

            foreach ( MapCell cell in tile.CellsList )
            {
                for ( int x = 0; x < CityMap.SUB_CELLS_PER_CELL; x++ )
                {
                    for ( int z = 0; z < CityMap.SUB_CELLS_PER_CELL; z++ )
                    {
                        MapSubCell sub = MapSubCell.GetFromPoolOrCreate();
                        sub.ParentCell = cell;
                        sub.SubCellLocation = new ArcenGroundPoint( x, z );
                        cell.SubCells.Add( sub );
                        Interlocked.Increment( ref CityMap.TotalSubCellCount );
                    }
                }
            }
            //now log for sub-cell calculations, here at the very end of the process
            CityMap.TilesToPostProcess_SubCells.Enqueue( tile );

            if ( Debug_LogTileCompletions )
                ArcenDebugging.LogSingleLine( "Tile decorations complete! " + tile.TileTopLeftCellCoordinate + 
                    " builds-out: " + tile.buildingZonesStillOutstanding +
                    " deco-out: " + tile.decorationZonesStillOutstanding +
                    " seconds spent: " +
                    (ArcenTime.AnyTimeSinceStartF - tile.timeStartedPopulation_Decoration).ToStringSmallFixedDecimal( 2 ), Verbosity.DoNotShow );
        }
        #endregion
    }
}

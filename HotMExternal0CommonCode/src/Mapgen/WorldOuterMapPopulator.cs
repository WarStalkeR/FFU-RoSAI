using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Unity.Mathematics;

namespace Arcen.HotM.External
{
    internal static class CityMapPopulator
    {
        #region SeedStartingMapTile
        public static void SeedStartingMapTile()
        {
            if ( CityMap.WasMapLoadedFromSavegame )
                return;

            //let's start by clearing the entire map, just in case there was something
            CityMap.ClearAll( false );

            if ( LevelTypeTable.AllValidStartingTiles.Count == 0  )
            {
                ArcenDebugging.LogSingleLine( "SeedStartingMapTile: LevelTypeTable.AllValidStartingTiles had no tile types!", Verbosity.ShowAsError );
                return;
            }
            LevelType randomTile = GetStartingTileType();
            if ( randomTile == null )
            {
                ArcenDebugging.LogSingleLine( "SeedStartingMapTile: randomTile was null!", Verbosity.ShowAsError );
                return;
            }

            if ( randomTile.MapGenFullTileType == null )
            {
                ArcenDebugging.LogSingleLine( "SeedStartingMapTile: MapGenFullTileType was null on LevelType '" + randomTile.ID + "'!", Verbosity.ShowAsError );
                return;
            }

            int x = 0;
            int z = 0;
            {
                {
                    MapTile mapTile = MapTile.GetFromPoolOrCreate();
                    mapTile.SetTileTypes( null, randomTile.MapGenFullTileType );
                    mapTile.SetTileTopLeftCellCoordinate( new ArcenGroundPoint( x, z ) );
                    if ( mapTile.TileType.CanRotate() )
                    {
                        int randRot = Engine_Universal.PermanentQualityRandom.Next( 0, 4 );
                        switch ( randRot )
                        {
                            default:
                            case 0:
                                mapTile.Rotation = TileRotation.Zero;
                                break;
                            case 1:
                                mapTile.Rotation = TileRotation.Ninety;
                                break;
                            case 2:
                                mapTile.Rotation = TileRotation.OneEighty;
                                break;
                            case 3:
                                mapTile.Rotation = TileRotation.TwoSeventy;
                                break;
                        }
                    }
                    else //not allowed to rotate
                        mapTile.Rotation = TileRotation.Zero;

                    bool xMirror = false;// Engine_Universal.PermanentQualityRandom.NextBool();
                    bool zMirror = false;// Engine_Universal.PermanentQualityRandom.NextBool();

                    TileMirrorFlags mirroring = TileMirrorFlags.None;
                    if ( xMirror )
                        mirroring |= TileMirrorFlags.X;
                    if ( zMirror )
                        mirroring |= TileMirrorFlags.Z;
                    bool debug = GameSettings.Current.GetBool( "Debug_MainGameMapgenLogging" );

                    mapTile.Mirroring = mirroring;
                    if ( !CityMap.TryAddNewPopulatedTile( mapTile ) )
                    {
                        ArcenDebugging.LogWithStack( "Failed to add a populated tile right on the start?", Verbosity.ShowAsError );
                        return;
                    }

                    if ( debug )
                    {
                        ArcenDebugging.LogSingleLine("Seeding " + mapTile.ToDebugStringWithConnectors() + " as the initial tile.", Verbosity.DoNotShow );
                        ArcenDebugging.LogSingleLine("Printing the road data again ", Verbosity.DoNotShow );
                        for ( int i = 0; i < (int)FourDirection.Length; i++ )
                        {
                            FourDirection direction = (FourDirection)i;
                            ArcenDebugging.LogSingleLine( "\tUnrotated: " + mapTile.TileType.ConnectionsToDebugString( direction, TileRotation.Zero, TileMirrorFlags.None, true ), Verbosity.DoNotShow );
                            ArcenDebugging.LogSingleLine( "\tRotate/Mirror " + mapTile.TileType.ConnectionsToDebugString( direction, mapTile.Rotation, mapTile.Mirroring, true ), Verbosity.DoNotShow );
                        }
                        bool addAdditionalTile = false;
                        if ( addAdditionalTile )
                        {
                            //this was instrumental in figuring out some of the problems with mirroring/rotation by giving a reference to look at 
                            mapTile = MapTile.GetFromPoolOrCreate();
                            mapTile.SetTileTypes( null, randomTile.MapGenFullTileType );
                            mapTile.Mirroring = TileMirrorFlags.None;
                            mapTile.Rotation = TileRotation.Zero;
                            mapTile.SetTileTopLeftCellCoordinate( new ArcenGroundPoint( 2, 2 ) );
                            if ( !CityMap.TryAddNewPopulatedTile( mapTile ) )
                                ArcenDebugging.LogWithStack( "Failed to add a populated tile (Spot B) right on the start?", Verbosity.ShowAsError );

                            mapTile = MapTile.GetFromPoolOrCreate();
                            mapTile.SetTileTypes( null, randomTile.MapGenFullTileType );
                            mapTile.Mirroring = TileMirrorFlags.None;
                            mapTile.Rotation = TileRotation.TwoSeventy;
                            mapTile.SetTileTopLeftCellCoordinate( new ArcenGroundPoint( 2, 3 ) );
                            if ( !CityMap.TryAddNewPopulatedTile( mapTile ) )
                                ArcenDebugging.LogWithStack( "Failed to add a populated tile (Spot C) right on the start?", Verbosity.ShowAsError );
                        }

                    }
                }
            }
        }
        #endregion

        #region SeedInitialGridOfTiles
        public static void SeedInitialGridOfTiles( CityStyle Style )
        {
            if ( CityMap.WasMapLoadedFromSavegame )
                return;

            if ( Style == null )
            {
                ArcenDebugging.LogSingleLine( "Null CityStyle passed to SeedInitialGridOfTiles, so could not do any seeding!", Verbosity.ShowAsError );
                return;
            }

            if ( CityMap.IsCurrentlyAddingMoreMapTiles != 0 )
            {
                ArcenDebugging.LogSingleLine( "CityMapPopulator: Was asked to generate a city pattern '" + Style.ID + "' grid of map tiles, when there were already map tiles still being generated!", Verbosity.ShowAsError );
                return;
            }
            Interlocked.Exchange( ref CityMap.IsCurrentlyAddingMoreMapTiles, 1 );

            int randSeed = Engine_Universal.PermanentQualityRandom.Next();
            if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.SeedMoreMapTiles",
                ( TaskStartData startData ) =>
                {
                    while ( !A5ObjectAggregation.IsLoadingCompletelyDone )
                        System.Threading.Thread.Sleep( 10 ); //if we haven't yet finished initializing absolutely everything, then don't run this yet

                    try
                    {

                        ActuallyDoSeedingOfInitialTileGrid_OnBackgroundThread( Style, randSeed );

                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogWithStack( "CityMapPopulator.SeedMoreMapTiles background thread error: " + e, Verbosity.ShowAsError );
                    }
                    Interlocked.Exchange( ref CityMap.IsCurrentlyAddingMoreMapTiles, 0 );
                } ) )
            {
                //we failed to start!
                Interlocked.Exchange( ref CityMap.IsCurrentlyAddingMoreMapTiles, 0 );
            }
        }
        #endregion

        private struct TileOrientation
        {
            //used to figure out what valid orientations a tile has when considering what to place
            public int relativeX;
            public int relativeZ;
            public TileRotation rotation;
            public TileMirrorFlags mirrorFlag;
            public TileOrientation( int x, int z, TileRotation rot, TileMirrorFlags flg )
            {
                relativeX = x;
                relativeZ = z;
                rotation = rot;
                mirrorFlag = flg;
            }
            public override bool Equals(object obj)
            {
                if ( obj == null )
                    return false;
                if ( obj is TileOrientation )
                {
                    TileOrientation other = (TileOrientation)obj;
                    if ( other.rotation == this.rotation && other.mirrorFlag == this.mirrorFlag )
                        return true;
                }
                return false;
            }
            
            public override int GetHashCode()
            {
                //pretty random? Dunno if this will work
                return this.relativeX * 5 + relativeZ * 2 + (int)rotation * 9 + (int)mirrorFlag * 100 ;
            }
            public string ToDebugString()
            {
                return "relativeX "  + relativeX + " relativeZ " + relativeZ + " rotation " + rotation + " mirroring " + mirrorFlag;
            }
        }

        private static void ActuallyDoSeedingOfInitialTileGrid_OnBackgroundThread( CityStyle Style, int randSeed)
        {
            if ( CityMap.WasMapLoadedFromSavegame )
                return;

            MersenneTwister rand = new MersenneTwister( randSeed );
            bool debug = GameSettings.Current.GetBool( "Debug_MainGameMapgenLogging" );
            if ( debug )
                ArcenDebugging.LogSingleLine("trying to a grid of tiles,  " + Style.CityWidthInCells + " by " + Style.CityHeightInCells + "", Verbosity.DoNotShow );
            int totalCellsToSeed = ( Style.CityWidthInCells * Style.CityHeightInCells ) - 1;

            ArcenDebugging.LogSingleLine( "Will generate map: " + totalCellsToSeed + " cells targeted. Existing tile count: " + CityMap.Tiles.Count, Verbosity.DoNotShow );

            if ( CityMap.Tiles.Count == 0 )
                SeedStartingMapTile();

            ThrowawayList<ArcenGroundPoint> AdjacentUnfilledCells= new ThrowawayList<ArcenGroundPoint>();
            //some minor magic here to take into account the fact that 0 is a row
            int maxX = Style.CityWidthInCells / 2;
            int minX = -maxX;
            int maxZ = Style.CityHeightInCells / 2;
            int minZ = -maxZ;
            if ( debug )
                ArcenDebugging.LogSingleLine("X " + Style.CityWidthInCells + " Z " + Style.CityHeightInCells + " maxX " + maxX + " minX " + minX + " maxZ " + maxZ + " minZ " + minZ , Verbosity.DoNotShow );

            int cellsLeftToSeed = totalCellsToSeed;
            
            foreach ( LevelType type in LevelTypeTable.Instance.Rows )
            {
                if ( type.IsKeyMacroTile || type.IsForFillingHoleTiles )
                    type.RefillUnusedContentList();
            }

            #region Macro Style Cell Seeding, If Needed
            if ( Style.MacroStyleCells.Count > 0 )
            {
                foreach ( CityMacroStyleCell pattern in Style.MacroStyleCells )
                {
                    if ( pattern.LevelType.AvailableContent.Count <= 0 )
                    {
                        ArcenDebugging.LogSingleLine( "Skip " + pattern.LevelType.ID + " from MacroStyleCells because it had no files of that type.", Verbosity.DoNotShow );
                        continue; //skip due to lack of content
                    }

                    int tilesWide = pattern.LevelType.HoleTileWidth;
                    int tilesTall = pattern.LevelType.HoleTileHeight;
                    if ( tilesWide <= 0 || tilesTall <= 0 )
                    {
                        ArcenDebugging.LogSingleLine( "Skip " + pattern.LevelType.ID + " from MacroStyleCells because it had a tile width or height that was <= 0.", Verbosity.ShowAsError );
                        continue;
                    }

                    //seed as many as stated in the definition
                    for ( int seeded = 0; seeded < pattern.DesiredCount; seeded++ )
                    {
                        for ( int tries = 0; tries < 400; tries++ )
                        {
                            int targetX = rand.Next( minX, maxX - tilesWide - 1 );
                            int targetZ = rand.Next( minZ, maxZ - tilesTall - 1 );

                            if ( CityMap.GetIsRegionOfMapEmpty( targetX, targetZ, tilesWide, tilesTall) )
                            {
                                if ( AddSpecialHoleTileToExactPreCheckedPoint( targetX, targetZ, pattern.LevelType, pattern.DistrictType, rand ) )
                                {
                                    //ArcenDebugging.LogSingleLine( "Seed macro cell " + pattern.LevelType.Row.ID + " from MacroStyleCells  and use up this many cells: " +
                                    //    (tilesWide * tilesTall), Verbosity.DoNotShow );
                                    cellsLeftToSeed -= (tilesWide * tilesTall);
                                }
                                break; //we can stop trying now!
                            }
                        }
                    }
                }
            }
            #endregion

            int traditionalHoleTilesRemainingRequired = Style.RequiredTraditionalHoleCells;
            int attempts = Style.MainLogicAttemptCount;
            //if ( attempts < cellsLeftToSeed )
            //    attempts = cellsLeftToSeed;

            while ( cellsLeftToSeed > 0 && attempts-- > 0 )
            {
                if ( debug )
                {
                    ArcenDebugging.LogSingleLine("For this iteration we have " + cellsLeftToSeed + " tiles left to seed, and our map currently has " + CityMap.Tiles.Count + " tiles: " , Verbosity.DoNotShow );
                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        ArcenDebugging.LogSingleLine("\t" + tile.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
                    }
                }
                //Note: right now we only examine one of the AdjacentUnfilledCells at a time before
                //rebuilding the entire list (since the newly added tile might be adjacent to the next hole we want to look at)
                //which means we rebuild the list a lot ( and we are strongly single-threaded).
                //If we need performance, we could put in a rule about "try to fill X widely-separated tiles, each on a different thread" with some tweaks to the algorithm.
                //NOTE: at the moment MapGenFullTile.DoSidesMatch() is not thread; I'm not aware of any other underlying functions that aren't thread safe, but someone should check before adding threading
                CityMap.FillListWithEmptyAdjacentCellsInRange( AdjacentUnfilledCells, minX, maxX, minZ, maxZ );
                if ( AdjacentUnfilledCells.Count == 0 ) //well, then let us go out of range I guess
                    CityMap.FillListWithEmptyAdjacentCells( AdjacentUnfilledCells );

                if ( AdjacentUnfilledCells.Count == 0 )
                    throw new Exception( "The world seems to be full?" );

                //ArcenGroundPoint point = AdjacentUnfilledCells[ rand.Next(0, AdjacentUnfilledCells.Count)]; //picking randomly seems not to work as well
                if ( debug )
                    ArcenDebugging.LogSingleLine("We have " + AdjacentUnfilledCells.Count + " adjacent cells to choose from", Verbosity.DoNotShow );

                int maxRetriesForTile = -1; //this means "Try every possible tile"
                int retriesBeforeGivingHoleTile = 5;
                if ( TryAddTileToAnyOneOfThesePoints( AdjacentUnfilledCells, maxRetriesForTile, rand, debug, retriesBeforeGivingHoleTile,
                    ref traditionalHoleTilesRemainingRequired ) )
                    cellsLeftToSeed--;
            }

            #region Do Late Fills
            int lateFills = 0;
            for ( int x = minX; x <= maxX; x++ )
            {
                for ( int z = minZ; z <= maxZ; z++ )
                {
                    if ( CityMap.TryGetExistingCellAtLocation(x, z) == null )
                    {
                        //this is a null tile, time to populate it
                        if ( TryAddTileToPointOrHoleTileIfNotFound( new ArcenGroundPoint( x, z ), -1, rand, debug, ref traditionalHoleTilesRemainingRequired ) )
                            lateFills++;
                    }
                }
            }

            //if ( lateFills > 0 )
            //    ArcenDebugging.LogSingleLine( "Late fills: " + lateFills, Verbosity.ShowAsError );
            #endregion

            #region If Not Enough Holes, Add Some Back
            if ( traditionalHoleTilesRemainingRequired > 0 )
            {
                ThrowawayList<MapTile> singleCellTiles = new ThrowawayList<MapTile>();
                foreach ( MapTile tile in CityMap.Tiles )
                {
                    if ( tile.CellsList.Count != 1 || //can't use tiles that are more than one cell for this
                        tile.IsHoleTile )//can't use those that are already hole tiles
                        continue;
                    singleCellTiles.Add( tile );
                }

                int holeTilesConverted = 0;
                while ( traditionalHoleTilesRemainingRequired > 0 )
                {
                    int index = rand.Next( 0, singleCellTiles.Count );
                    MapTile tile = singleCellTiles[ index ];
                    singleCellTiles.RemoveAt( index );
                    traditionalHoleTilesRemainingRequired--;

                    tile.IsHoleTile = true;
                    tile.DuringSeeding_DistrictType = null;
                    holeTilesConverted++;
                    tile.SetTileTypes( null, null );
                }

                //ArcenDebugging.LogSingleLine( "Converted " + holeTilesConverted + " regular tiles into hole tiles to make sure we have the " +
                //    CityPattern.RequiredTraditionalHoleCells + " requested by the city definition.", Verbosity.DoNotShow );
            }
            #endregion

            #region Now Allocate All The Empty Hole Tiles
            {
                ThrowawayList<MapTile> emptySingleTileHoles = new ThrowawayList<MapTile>();
                foreach ( MapTile tile in CityMap.Tiles )
                {
                    if ( tile.CellsList.Count != 1 || //can't use tiles that are more than one cell for this
                        !tile.IsHoleTile || //if not a hole tile, can't use it here
                        tile.HoleLevelType != null ) //if already filled what it is, skip it also
                        continue;
                    emptySingleTileHoles.Add( tile );
                }

                #region Required Hole Filler Cells, If Needed
                if ( Style.RequiredHoleFillerCells.Count > 0 )
                {
                    foreach ( CityHoleFillerCell filler in Style.RequiredHoleFillerCells )
                    {
                        if ( filler.LevelType.AvailableContent.Count <= 0 )
                        {
                            ArcenDebugging.LogSingleLine( "Skip " + filler.LevelType.ID + " from RequiredHoleFillerCells because it had no files of that type.", Verbosity.DoNotShow );
                            continue; //skip due to lack of content
                        }

                        //seed as many as required in the definition
                        for ( int seeded = 0; seeded < filler.RequiredMinimum; seeded++ )
                        {
                            for ( int tries = 0; tries < 400; tries++ ) //probably will work every time on the first try
                            {
                                if ( ConvertRandomEmptyHoleTileToNewStyle( emptySingleTileHoles, filler.LevelType, filler.DistrictType, rand ) )
                                    break;
                            }
                        }
                    }
                }
                #endregion

                #region Secondary Randomized Hole Filler Cells, If Needed
                if ( emptySingleTileHoles.Count > 0 )
                {
                    if ( !Style.SecondaryRandomizedHoleFillerCells.HasAnyContent )
                    {
                        ArcenDebugging.LogSingleLine( "Could not fill " + emptySingleTileHoles.Count +
                            " leftover holes in the city because there were no secondary_randomized_hole_filler_cell entries on the city definition type.", Verbosity.ShowAsError );

                    }
                    else
                    {
                        while ( emptySingleTileHoles.Count > 0 )
                        {
                            CityCellType holeTypeToSeed = Style.SecondaryRandomizedHoleFillerCells.PickRandom( rand );
                            ConvertRandomEmptyHoleTileToNewStyle( emptySingleTileHoles, holeTypeToSeed.LevelType, holeTypeToSeed.DistrictType, rand );
                        }
                    }
                }
                #endregion
            }
            #endregion

            //these are the general bounds for the city.  Of course, certain things may be seeded outside of this.
            ArcenIntRectangle cityBounds = ArcenIntRectangle.Create( minX, minZ, maxX - minX, maxZ - minZ );

            SeedCityBlobsToExistingTiles( Style, rand, cityBounds );
            GenerateDistricts( rand );
            LogMapResults( cityBounds );

            {
                LevelType outOfBoundsLevelType = LevelTypeTable.Instance.GetRowByID( "AncientRuins" );
                if ( outOfBoundsLevelType == null )
                    ArcenDebugging.LogSingleLine( "could not find LevelType with ID 'AncientRuins'!", Verbosity.ShowAsError );

                //now seed the ring of 4 out of bounds rows
                for ( int z = minZ - 4; z <= maxZ + 4; z++ )
                {
                    for ( int x = minX - 4; x <= maxX + 4; x++ )
                    {
                        if ( CityMap.TryGetExistingCellAtLocation( x, z ) == null )
                        {
                            //this is a null tile, time to populate it
                            if ( TryAddOutOfBoundsTileToPoint( new ArcenGroundPoint( x, z ), outOfBoundsLevelType, rand ) )
                                lateFills++;
                        }
                    }
                }
            }
            //and put them in a district
            GenerateOutOfBoundsDistrict( rand );
        }

        #region ActuallyDoSeedingOfMoreMapTiles_OnBackgroundThread
        //private static void ActuallyDoSeedingOfMoreMapTiles_OnBackgroundThread( int CountToSeed, int randSeed)
        //{
        //    //This seeds tiles at random that are adjacent to pre-existing tiles.
        //    MersenneTwister rand = new MersenneTwister( randSeed );

        //    int cellsLeftToSeed = CountToSeed;
        //    int maxRetries = 100000;
        //    bool debug = GameSettings.Current.GetBool( "Debug_MainGameMapgenLogging" );
        //    if ( debug )
        //        ArcenDebugging.LogSingleLine("trying to seed " + CountToSeed + " additional tiles", Verbosity.DoNotShow );

        //    ThrowawayList<ArcenGroundPoint> AdjacentUnfilledCells= new ThrowawayList<ArcenGroundPoint>();


        //    while ( cellsLeftToSeed > 0 && maxRetries-- > 0 )
        //    {
        //        bool logDebug = debug;
        //        if ( logDebug )
        //        {
        //            ArcenDebugging.LogSingleLine("For this iteration we have " + cellsLeftToSeed + " tiles left to seed, and our map currently has " + CityMap.Tiles.Count + " tiles: " , Verbosity.DoNotShow );
        //            CityMap.DoForTiles( delegate ( MapTile tileInMap)
        //            {
        //                ArcenDebugging.LogSingleLine("\t" + tileInMap.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
        //                return DelReturn.Continue;
        //            } );
        //        }
        //        //Note: right now we only examine one of the AdjacentUnfilledCells at a time before
        //        //rebuilding the entire list (since the newly added tile might be adjacent to the next hole we want to look at)
        //        //which means we rebuild the list a lot ( and we are strongly single-threaded).
        //        //If we need performance, we could put in a rule about "try to fill X widely-separated tiles, each on a different thread" with some tweaks to the algorithm.
        //        //NOTE: at the moment MapGenFullTile.DoSidesMatch() is not thread; I'm not aware of any other underlying functions that aren't thread safe, but someone should check before adding threading
        //        CityMap.FillListWithEmptyAdjacentCells( AdjacentUnfilledCells );
        //        if ( AdjacentUnfilledCells.Count == 0 )
        //            throw new Exception("The world seems to be full?");

        //        //ArcenGroundPoint point = AdjacentUnfilledCells[ rand.Next(0, AdjacentUnfilledCells.Count)]; //picking randomly seems not to work as well

        //        int maxRetriesForTile = -1;
        //        int retriesBeforeGivingHoleTile = 10;
        //        AddTileToAnyOneOfThesePoints( AdjacentUnfilledCells, maxRetriesForTile, rand, debug, retriesBeforeGivingHoleTile );
        //        cellsLeftToSeed--;
        //    }
        //    if ( debug )
        //    {
        //        ArcenDebugging.LogSingleLine("Outer mapgen now has " + CityMap.Tiles.Count + " tiles: ", Verbosity.DoNotShow );
        //        CityMap.DoForTiles( delegate ( MapTile tileInMap)
        //        {
        //            ArcenDebugging.LogSingleLine("\t" + tileInMap.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
        //            return DelReturn.Continue;
        //        } );
        //    }
        //}
        #endregion

        #region AddSpecialHoleTileToExactPreCheckedPoint
        private static bool AddSpecialHoleTileToExactPreCheckedPoint( int X, int Z, LevelType HoleType, DistrictType DistrictType, MersenneTwister Rand )
        {
            if ( HoleType == null )
                return false;

            //don't reuse content unless we don't have enough for each to be unique
            if ( HoleType.UnusedContentSoFar.Count == 0 )
            {
                HoleType.RefillUnusedContentList();
                if ( HoleType.UnusedContentSoFar.Count == 0 )
                    return false; //guess we are done
            }

            int index = Rand.Next( 0, HoleType.UnusedContentSoFar.Count );
            ReferenceLevelData data = HoleType.UnusedContentSoFar[index];
            HoleType.UnusedContentSoFar.RemoveAt( index );

            MapTile potentialMapTile = MapTile.GetFromPoolOrCreate();
            //hole tile:
            potentialMapTile.IsHoleTile = true;
            potentialMapTile.HoleLevelData = data;
            potentialMapTile.SetTileTypes( HoleType, null );
            potentialMapTile.DuringSeeding_DistrictType = DistrictType;
            potentialMapTile.SetTileTopLeftCellCoordinate( new ArcenGroundPoint( X, Z ) ); //must happen after the above stuff is set

            if ( !CityMap.TryAddNewPopulatedTile( potentialMapTile ) )
            {
                potentialMapTile.ReturnToPool(); //put it back in the pool and don't try to do this
                return false;
            }
            return true;

        }
        #endregion

        #region ConvertRandomEmptyHoleTileToNewStyle
        private static bool ConvertRandomEmptyHoleTileToNewStyle( IList<MapTile> Tiles, LevelType HoleType, DistrictType DistrictType, MersenneTwister Rand )
        {
            if ( HoleType == null )
                return false;
            if ( Tiles.Count== 0 ) 
                return false; //also guess we are done

            //don't reuse content unless we don't have enough for each to be unique
            if ( HoleType.UnusedContentSoFar.Count == 0 )
            {
                HoleType.RefillUnusedContentList();
                if ( HoleType.UnusedContentSoFar.Count == 0 )
                    return false; //guess we are done
            }

            int tileIndex = Rand.Next( 0, Tiles.Count );
            MapTile tile = Tiles[tileIndex];
            Tiles.RemoveAt( tileIndex );

            int contentIndex = Rand.Next( 0, HoleType.UnusedContentSoFar.Count );
            ReferenceLevelData data = HoleType.UnusedContentSoFar[contentIndex];
            HoleType.UnusedContentSoFar.RemoveAt( contentIndex );

            tile.HoleLevelData = data;
            tile.DuringSeeding_DistrictType = DistrictType;
            tile.SetTileTypes( HoleType, null );
            return true;
        }
        #endregion

        #region AddTileToAnyOneOfThesePoints
        private static bool TryAddTileToAnyOneOfThesePoints( ThrowawayList<ArcenGroundPoint> PotentialTileSpots, int retriesForThisTile, MersenneTwister rand, 
            bool debug, int retriesBeforeGivingHoleTile, ref int TraditionalHoleTilesRemainingRequired )
        {
            MapTile potentialMapTile = MapTile.GetFromPoolOrCreate();
            ArcenGroundPoint lastDitchPoint = PotentialTileSpots[rand.Next( 0, PotentialTileSpots.Count )];

            while ( PotentialTileSpots.Count > 0 )
            {
                int index = rand.Next( 0, PotentialTileSpots.Count );
                ArcenGroundPoint point = PotentialTileSpots[index];
                PotentialTileSpots.RemoveAt( index );

                if ( CityMap.TryGetExistingCellAtLocation( point ) != null )
                    continue; //whoops, found an already-filled point for some reason.  Skip it!

                potentialMapTile.TileTopLeftCellCoordinate = point; //yo this is going to be a terrible time, be sure to set this via SetTileTopLeftCellCoordinate below!

                if ( TryToCharacterizeAddTileToAtPoint( point, retriesForThisTile, rand, debug, potentialMapTile ) )
                {
                    if ( CityMap.TryAddNewPopulatedTile( potentialMapTile ) )
                    {
                        potentialMapTile.SetTileTopLeftCellCoordinate( potentialMapTile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
                        return true; //found one that worked.  Yes!
                    }
                }

                retriesBeforeGivingHoleTile--;
                if ( retriesBeforeGivingHoleTile <= 0 )
                {
                    if ( CityMap.TryAddNewPopulatedTile( potentialMapTile ) )
                    {
                        //hole tile:
                        potentialMapTile.IsHoleTile = true;
                        potentialMapTile.DuringSeeding_DistrictType = null;
                        TraditionalHoleTilesRemainingRequired--;
                        potentialMapTile.SetTileTypes( null, null );
                        potentialMapTile.SetTileTopLeftCellCoordinate( potentialMapTile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
                        return true; //just made it a hole tile early
                    }
                }
            }

            //if we got here, then it's a hole tile
            {
                if ( CityMap.TryGetExistingCellAtLocation( lastDitchPoint ) != null )
                {
                    potentialMapTile.ReturnToPool(); //put this back in the pool
                    return false; //whoops, found an already-filled point for some reason.  Skip it!
                }

                if ( !TryToCharacterizeAddTileToAtPoint( lastDitchPoint, retriesForThisTile, rand, debug, potentialMapTile ) )
                {
                    //hole tile:
                    potentialMapTile.IsHoleTile = true;
                    potentialMapTile.DuringSeeding_DistrictType = null;
                    TraditionalHoleTilesRemainingRequired--;
                    potentialMapTile.SetTileTypes( null, null );
                    potentialMapTile.SetTileTopLeftCellCoordinate( lastDitchPoint );
                }

                if ( !CityMap.TryAddNewPopulatedTile( potentialMapTile ) )
                {
                    potentialMapTile.ReturnToPool();
                    return false;
                }
                potentialMapTile.SetTileTopLeftCellCoordinate( potentialMapTile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
                return true;
            }
        }
        #endregion

        #region TryAddTileToPointOrHoleTileIfNotFound
        private static bool TryAddTileToPointOrHoleTileIfNotFound( ArcenGroundPoint point, int retriesForThisTile, MersenneTwister rand, bool debug,
            ref int TraditionalHoleTilesRemainingRequired )
        {
            MapTile potentialMapTile = MapTile.GetFromPoolOrCreate();
            potentialMapTile.TileTopLeftCellCoordinate = point; //yo this is going to be a terrible time, be sure to set this via SetTileTopLeftCellCoordinate below!

            if ( !TryToCharacterizeAddTileToAtPoint( point, retriesForThisTile, rand, debug, potentialMapTile ) )
            {
                //hole tile:
                potentialMapTile.IsHoleTile = true;
                potentialMapTile.DuringSeeding_DistrictType = null;
                TraditionalHoleTilesRemainingRequired--;
                potentialMapTile.SetTileTypes( null, null );
                potentialMapTile.SetTileTopLeftCellCoordinate( potentialMapTile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
            }

            if ( !CityMap.TryAddNewPopulatedTile( potentialMapTile ) )
            {
                potentialMapTile.ReturnToPool();
                return false;
            }
            potentialMapTile.SetTileTopLeftCellCoordinate( potentialMapTile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
            return true;
        }
        #endregion

        #region TryAddOutOfBoundsTileToPoint
        private static bool TryAddOutOfBoundsTileToPoint( ArcenGroundPoint point, LevelType HoleType, MersenneTwister Rand )
        {
            MapTile tile = MapTile.GetFromPoolOrCreate();
            tile.TileTopLeftCellCoordinate = point; //yo this is going to be a terrible time, be sure to set this via SetTileTopLeftCellCoordinate below!

            tile.IsOutOfBoundsTile = true;
            tile.DuringSeeding_DistrictType = null;

            int randRot = Engine_Universal.PermanentQualityRandom.Next( 0, 4 );
            switch ( randRot )
            {
                default:
                case 0:
                    tile.Rotation = TileRotation.Zero;
                    break;
                case 1:
                    tile.Rotation = TileRotation.Ninety;
                    break;
                case 2:
                    tile.Rotation = TileRotation.OneEighty;
                    break;
                case 3:
                    tile.Rotation = TileRotation.TwoSeventy;
                    break;
            }

            bool xMirror = false;// Engine_Universal.PermanentQualityRandom.NextBool();
            bool zMirror = false;//Engine_Universal.PermanentQualityRandom.NextBool();

            TileMirrorFlags mirroring = TileMirrorFlags.None;
            if ( xMirror )
                mirroring |= TileMirrorFlags.X;
            if ( zMirror )
                mirroring |= TileMirrorFlags.Z;

            tile.Mirroring = mirroring;

            if ( !CityMap.TryAddNewPopulatedTile( tile ) )
            {
                tile.ReturnToPool();
                return false;
            }

            if ( HoleType.AvailableContent.Count < 1 )
            {
                ArcenDebugging.LogSingleLine( "Had no AvailableContent in LevelType '" + HoleType.ID + "'!", Verbosity.ShowAsError );
            }
            else
            {
                int contentIndex = Rand.Next( 0, HoleType.AvailableContent.Count );
                ReferenceLevelData data = HoleType.AvailableContent[contentIndex];

                tile.HoleLevelData = data;
                tile.SetTileTypes( HoleType, null );
            }

            tile.SetTileTopLeftCellCoordinate( tile.TileTopLeftCellCoordinate ); //this prevents all sorts of things from being wrong
            return true;
        }
        #endregion

        private static readonly ListOfLists<TileOrientation> OrientationsForAllAdjacentTiles = ListOfLists<TileOrientation>.Create_WillNeverBeGCed( 10, 10, "CityMapPopulator-OrientationsForAllAdjacentTiles" );

        #region TryToCharacterizeAddTileToAtPoint
        private static bool TryToCharacterizeAddTileToAtPoint( ArcenGroundPoint point, int retriesForThisTile, MersenneTwister rand, bool debug, MapTile potentialMapTile )
        {
            //this will add a new Tile (either a Hole or a "real tile") to a given point
            //eventually we want to minimize the number of null tiles, but for now lets make sure they occur
            ThrowawayList<MapTile> TilesAdjacentToPoint= new ThrowawayList<MapTile>();
            bool foundOrientation = false;
            TileOrientation orientationToUse;
            OrientationsForAllAdjacentTiles.Clear();
            ThrowawayList<TileOrientation> ValidOrientationsForAllAdjacentTiles = new ThrowawayList<TileOrientation>();

            CityMap.GetNonNullTilesAdjacentToPoint( TilesAdjacentToPoint, point );
            if ( debug )
            {
                ArcenDebugging.LogSingleLine("For point " + point + " we have " + TilesAdjacentToPoint.Count + " adjacent tiles:", Verbosity.DoNotShow );
                for ( int i = 0; i < TilesAdjacentToPoint.Count; i++ )
                    ArcenDebugging.LogSingleLine("\t" + TilesAdjacentToPoint[i].ToDebugStringWithConnectors(), Verbosity.DoNotShow );
            }
            bool tryAllTiles = false;
            if ( retriesForThisTile == -1 )
            {
                tryAllTiles = true;
                retriesForThisTile = TotalAvailableTiles();
            }
            for ( int i = 0; i < retriesForThisTile; i++ )
            {
                bool innerDebug = GameSettings.Current.GetBool( "Debug_MainGameMapgenLogging" );
                if ( tryAllTiles )
                    potentialMapTile.SetTileTypes( null, GetTileByIndex( i ) );
                else
                    potentialMapTile.SetTileTypes( null, GetRandomTileType( rand ) );
                if ( debug )
                {
                    ArcenDebugging.LogSingleLine("Checking if we could place " + potentialMapTile.TileType.ToDebugString() + ". We will make " + TilesAdjacentToPoint.Count + " adjacent tile checks. Attempt " + i + " to find a tile.", Verbosity.DoNotShow );
                }
                OrientationsForAllAdjacentTiles.Clear();
                for ( int j = 0; j < TilesAdjacentToPoint.Count; j++ )
                {
                    MapTile tileInMap = TilesAdjacentToPoint[j];
                    if ( debug )
                    {
                        ArcenDebugging.LogSingleLine("For point " + point + ", check for valid orientations to match " + tileInMap.ToDebugString(), Verbosity.DoNotShow );
                    }
                    OrientationsForAllAdjacentTiles.AddInnerList();
                    GetValidOrientationsForTile( tileInMap, potentialMapTile, OrientationsForAllAdjacentTiles[j], innerDebug );
                }

                ValidOrientationsForAllAdjacentTiles.Clear();
                GetValidOrientationsForAllTiles ( ValidOrientationsForAllAdjacentTiles, OrientationsForAllAdjacentTiles );
                if ( ValidOrientationsForAllAdjacentTiles.Count > 0 )
                {
                    foundOrientation = true;

                    orientationToUse = ValidOrientationsForAllAdjacentTiles[ Engine_Universal.PermanentQualityRandom.Next( 0, ValidOrientationsForAllAdjacentTiles.Count )];
                    potentialMapTile.Mirroring = orientationToUse.mirrorFlag;
                    potentialMapTile.Rotation = orientationToUse.rotation;
                    if ( debug )
                        ArcenDebugging.LogSingleLine("We have " + ValidOrientationsForAllAdjacentTiles.Count + " valid orientations of " + potentialMapTile.ToDebugStringWithConnectors() + " we can use, so this tile will go in " + point +". We chose orientation <" + orientationToUse.ToDebugString() + ">", Verbosity.DoNotShow );
                    break;
                }
            }
            if ( !foundOrientation )
            {
                if ( debug )
                    ArcenDebugging.LogSingleLine("We could find no tile to place in " + point + " so place a null tile ", Verbosity.DoNotShow );
                return false;
            }
            return true;
        }
        #endregion

        private static LevelType GetStartingTileType( )
        {
            LevelType randomTile = LevelTypeTable.AllValidStartingTiles[Engine_Universal.PermanentQualityRandom.Next( 0, LevelTypeTable.AllValidStartingTiles.Count )];
            //show all the valid starting tiles
            // for ( int i = 0; i < LevelTypeTable.AllValidStartingTiles.Count; i++ )
            //     ArcenDebugging.LogSingleLine("Tile " + i + " " + LevelTypeTable.AllValidStartingTiles[i].ID, Verbosity.DoNotShow );
            return randomTile;
            //return randomTile;
        }
        private static int TotalAvailableTiles()
        {
            return LevelTypeTable.AllFullTiles.Count;
        }
        private static MapGenFullTile GetTileByIndex( int idx )
        {
            return LevelTypeTable.AllFullTiles[idx];
        }
        private static MapGenFullTile GetRandomTileType( MersenneTwister rand )
        {
            MapGenFullTile randomTile = null;
            int retries = 1000;

            int startingIndex = rand.Next( 0, LevelTypeTable.AllFullTiles.Count );
            int currentIndex = startingIndex;
            bool hasLooped = false;
            while ( randomTile == null && retries-- > 0)
            {
                randomTile = LevelTypeTable.AllFullTiles[startingIndex];
                if ( !randomTile.CanRotate() || randomTile.TileTypeID.Contains("Train") )
                {
                    currentIndex++;
                    if ( currentIndex >= LevelTypeTable.AllFullTiles.Count )
                    {
                        if ( hasLooped )
                            break;
                        else
                        {
                            hasLooped = true;
                            currentIndex = 0;
                        }
                    }

                    randomTile = null; //Don't use any trains: https://www.youtube.com/watch?v=KaHEsLuWM4M
                    continue;
                }
            }
            if ( randomTile == null )
                throw new Exception("GetRandomTileType: Could not find any 1x1 tiles");

            return randomTile;
        }
        public static MapGenFullTile GetTileByName( string name )
        {
            for ( int i = 0; i < LevelTypeTable.AllFullTiles.Count; i++ )
            {
                MapGenFullTile tile = LevelTypeTable.AllFullTiles[i];
                if ( tile.TileTypeID == name )
                    return tile;
            }
            throw new Exception("Could not find tile with name " + name);
        }
        private static void GetValidOrientationsForAllTiles( ThrowawayList<TileOrientation> output, ListOfLists<TileOrientation> listsToCheck )
        {
            //if there's any TileOrientation that appears on all lists, add it to output
            output.Clear();
            if ( listsToCheck.OuterListCount == 0 )
                return;
            List<TileOrientation> firstList = listsToCheck[0];
            for ( int i = 0; i < firstList.Count; i++ )
            {
                TileOrientation orientation = firstList[i];
                if ( listsToCheck.OuterListCount == 1 )
                {
                    //if we only have one adjacent tile, every orientation is valid
                    output.Add( orientation );
                    continue;
                }
                bool invalidOrientation = false;

                for ( int j = 1; j < listsToCheck.OuterListCount; j++ )
                {
                    List<TileOrientation> otherList = listsToCheck[j];
                    bool foundMatchForList = false;
                    for ( int k = 0; k < otherList.Count; k++ )
                    {
                        TileOrientation otherOrientation = otherList[k];
                        if ( orientation.Equals( otherOrientation ) )
                        {
                            foundMatchForList = true;
                            break;
                        }
                    }
                    if ( !foundMatchForList )
                    {
                        invalidOrientation = true;
                        break;
                    }
                }
                if ( !invalidOrientation )
                    output.Add( orientation );
            }

        }
        private static void GetValidOrientationsForTile( MapTile tileInMap, MapTile potentialMapTile, List<TileOrientation> ValidOrientationsOutput, bool debug = false )
        {
            //Check whether we can find a successful match between tileInMap and potentialMapTile
            //return a list of all the ways this can be done
            //Note: potentialMapTile has its TopLeftPoint already set
            int relativeX = -1;
            int relativeZ = -1;

            TileMirrorFlags mirroring = TileMirrorFlags.None;
            TileRotation rotation = TileRotation.Zero;
            bool matchFound = false;
            if ( ValidOrientationsOutput == null )
                throw new Exception("Huh?");
            ValidOrientationsOutput.Clear();
            relativeX = tileInMap.TileTopLeftCellCoordinate.X - potentialMapTile.TileTopLeftCellCoordinate.X;
            relativeZ = tileInMap.TileTopLeftCellCoordinate.Z - potentialMapTile.TileTopLeftCellCoordinate.Z;
            FourDirection direction = potentialMapTile.TileType.GetMyFourDirectionFromOffset( tileInMap.TileType, relativeX, relativeZ );
            // if ( tileInMap.TopLeftPoint.X > potentialMapTile.tileInMap.X )
            //     relativeX = tileInMap.TopLeftPoint.X - potentialMapTile.tileInMap.X;
            if ( debug )
                ArcenDebugging.LogSingleLine("GetValidOrientationsForTile between me " + potentialMapTile.ToDebugStringWithConnectors() + " and " + tileInMap.ToDebugStringWithConnectors() + " as X "   + relativeX + " Z " + relativeZ, Verbosity.DoNotShow );
            bool simpleTest = false;
            if ( simpleTest )
            {
                //for testing. In real code we should use the below
                matchFound = potentialMapTile.TileType.DoSidesMatch( rotation, mirroring, tileInMap.TileType,
                                                              tileInMap.Rotation, tileInMap.Mirroring, relativeX, relativeZ, debug );
                if ( matchFound )
                {
                    TileOrientation orientation = new TileOrientation( relativeX, relativeZ, rotation, mirroring);
                    if ( debug )
                        ArcenDebugging.LogSingleLine("GetValidOrientations: found valid orientation for this map tile. Rotation " + rotation + " mirroring " + mirroring +" <"+orientation.ToDebugString() +">; debug path ", Verbosity.DoNotShow );

                    ValidOrientationsOutput.Add( orientation );
                }
            }
            //In production code, try all potential mirrorings/rotations
            //for ( int j = 0; j < 4; j++ )
            {
                //the count here is for all the mirrorings
                bool xMirror = false;
                bool zMirror = false;
                //if ( j == 0 )
                //{}
                //else if ( j == 1 )
                //{
                //    xMirror = true; zMirror = false;
                //}
                //else if ( j == 2 )
                //{
                //    xMirror = false; zMirror = true;
                //}
                //else
                //{
                //    xMirror = true; zMirror = true;
                //}
                if ( xMirror )
                    mirroring |= TileMirrorFlags.X;
                if ( zMirror )
                    mirroring |= TileMirrorFlags.Z;

                //For this mirroring
                if ( !potentialMapTile.TileType.CanRotate() )
                {
                    //we can't rotate, so just check this rotation
                    matchFound = potentialMapTile.TileType.DoSidesMatch( rotation, mirroring, tileInMap.TileType, tileInMap.Rotation, tileInMap.Mirroring,
                                                                         relativeX, relativeZ, debug );
                    if ( matchFound )
                    {
                        TileOrientation orientation = new TileOrientation( relativeX, relativeZ, rotation, mirroring);
                        if ( debug )
                            ArcenDebugging.LogSingleLine("found a valid orientation for the new tile. <"+orientation.ToDebugString()  +"> no rotation path ", Verbosity.DoNotShow );

                        ValidOrientationsOutput.Add( orientation );
                    }
                }
                else
                {
                    //try all the rotations
                    for ( int k = 0; k <= (int)TileRotation.TwoSeventy; k += 90 )
                    {
                        rotation = (TileRotation)k;
                        matchFound = potentialMapTile.TileType.DoSidesMatch( rotation, mirroring, tileInMap.TileType, tileInMap.Rotation, tileInMap.Mirroring,
                                                                         relativeX, relativeZ, debug );
                        if ( matchFound )
                        {
                            TileOrientation orientation = new TileOrientation( relativeX, relativeZ, rotation, mirroring);
                            if ( debug )
                            {
                                ArcenDebugging.LogSingleLine("found a valid orientation for this tile. <" + orientation.ToDebugString()  +", " + potentialMapTile.TileType.ConnectionsToDebugString( direction, rotation, mirroring) + ">, multi-rotation path", Verbosity.DoNotShow );
                            }

                            ValidOrientationsOutput.Add( orientation );
                            return;
                        }
                    }
                }
            }
            return;
        }
        #region AddDebugMapTiles
        public static void AddDebugMapTiles()
        {
            //this is a debug function, used to seed specific tiles to allow for visual inspection/testing
            //LevelType tileType = LevelTypeTable.AllValidStartingTiles[0];
            //MapGenFullTile tile = tileType.MapGenFullTileType;

            MapGenFullTile tile = GetTileByName("SkeletonExtDowntownGridA");
            MapTile mapTile2 = MapTile.GetFromPoolOrCreate();
            mapTile2.SetTileTypes( null, tile );
            mapTile2.SetTileTopLeftCellCoordinate( new ArcenGroundPoint(0, 0) );
            mapTile2.Mirroring = TileMirrorFlags.None;
            mapTile2.Rotation = TileRotation.Zero;
            ArcenDebugging.LogSingleLine("Seeding " + mapTile2.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
            if ( !CityMap.TryAddNewPopulatedTile( mapTile2 ) )
                ArcenDebugging.LogWithStack( "Failed to add mapTile", Verbosity.ShowAsError );
            
            mapTile2 = MapTile.GetFromPoolOrCreate();
            mapTile2.SetTileTypes( null, tile );
            mapTile2.SetTileTopLeftCellCoordinate( new ArcenGroundPoint(0, 1) );
            mapTile2.Mirroring = TileMirrorFlags.X;
            mapTile2.Rotation = TileRotation.Ninety;
            ArcenDebugging.LogSingleLine("Seeding " + mapTile2.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
            if ( !CityMap.TryAddNewPopulatedTile( mapTile2 ) )
                ArcenDebugging.LogWithStack( "Failed to add mapTile", Verbosity.ShowAsError );

            mapTile2 = MapTile.GetFromPoolOrCreate();
            mapTile2.SetTileTypes( null, tile );
            mapTile2.SetTileTopLeftCellCoordinate( new ArcenGroundPoint(0, 2) );
            mapTile2.Mirroring = TileMirrorFlags.X;
            mapTile2.Rotation = TileRotation.Zero;
            ArcenDebugging.LogSingleLine("Seeding " + mapTile2.ToDebugStringWithConnectors(), Verbosity.DoNotShow );
            if ( !CityMap.TryAddNewPopulatedTile( mapTile2 ) )
                ArcenDebugging.LogWithStack( "Failed to add mapTile", Verbosity.ShowAsError );
        }
        #endregion

        #region SeedCityBlobsToExistingTiles
        private static void SeedCityBlobsToExistingTiles( CityStyle Style, MersenneTwister rand, ArcenIntRectangle CityBounds )
        {
            //ArcenDebugging.LogSingleLine( "Will generate map:" + CityBounds + " Existing tile count: " + CityMap.Tiles.Count + " was from save: " + CityMap.WasMapLoadedFromSavegame, Verbosity.DoNotShow );

            //seed each blob, one after the other, in order
            foreach ( CityStyleBlob blob in Style.StyleBlobs )
                SeedSingleCityBlobToExistingTiles( blob, rand, CityBounds );

            //now fill in all the rest based on general probabilities
            foreach ( MapTile tile in CityMap.Tiles )
            {
                if ( tile.TileType != null && !tile.IsHoleTile && tile.SeedingLogic == null )
                {
                    CityCellProbabilityType toSeed = Style.DrawRandomItem_GeneralProbabilities( rand );
                    tile.SeedingLogic = toSeed.LogicType;
                    tile.DuringSeeding_DistrictType = toSeed.DistrictType;
                }
            }
        }
        #endregion

        #region GenerateDistricts
        private static void GenerateDistricts( MersenneTwister rand )
        {
            bool districtDetailLogging = GameSettings.Current.GetBool( "Debug_CityDistrictDetailLogging" );

            int debugStage = 0;
            try
            {
                //since mapgen is rare, I'll use throwaway collections
                ThrowawayList<MapCell> cellsForDistrict = new ThrowawayList<MapCell>();
                ThrowawayDictionary<ArcenGroundPoint, bool> workingPoints = new ThrowawayDictionary<ArcenGroundPoint, bool>( 600 );

                debugStage = 100;

                char currentChar = '!';

                #region Make Real Districts, Pass 1
                //we have all our district types, now actually make real district!
                foreach ( MapCell cell in CityMap.Cells )
                {
                    debugStage = 1200;
                    if ( cell.ParentTile.District != null )
                        continue; //this one was apparently already added to a district!

                    debugStage = 1300;

                    DistrictType districtType = cell.ParentTile.DuringSeeding_DistrictType;
                    if ( districtType == null )
                        continue; //skip this for now, I guess.  Will be merged into a neighbor later

                    debugStage = 1400;

                    int maxCount = rand.Next( 5, 11 );

                    CityMap.FloodFillCellsFromPoint( cellsForDistrict, cell.CellLocation, workingPoints,
                        delegate ( MapCell other )
                        {
                            return other.ParentTile.DuringSeeding_DistrictType == districtType &&
                            other.ParentTile.District == null;
                        }, maxCount );

                    debugStage = 1600;

                    if ( cellsForDistrict.Count > 0 &&
                        cellsForDistrict.Count >= districtType.DoNotAssignIfFewerThanXCells ) //if it's too small, skip this and we will fold it into a nearby district later
                    {
                        debugStage = 2100;

                        MapDistrict district = MapDistrict.GetFromPoolOrCreate();
                        district.Type = districtType;
                        CityMap.AllDistricts.Add( district );
                        CityMap.DistrictsByType.GetListForOrCreate( districtType ).Add( district );
                        district.DistrictID = (Int16)CityMap.AllDistricts.Count;
                        CityMap.DistrictsByID.TryAdd( district.DistrictID, district, true );
                        district.MapGenVisualizationCharacter = currentChar;
                        currentChar++;
                        if ( currentChar == '' ) //delete char
                            currentChar = 'À';

                        debugStage = 2600;

                        foreach ( MapCell finalCell in cellsForDistrict )
                            district.AddTileToDistrict( finalCell.ParentTile );

                        if ( districtDetailLogging )
                            ArcenDebugging.LogSingleLine( "Added district '" + district.MapGenVisualizationCharacter + "' with cells: " + cellsForDistrict.Count +
                                " in district type " + districtType.ID, Verbosity.DoNotShow );

                        if ( district.Cells.Count < cellsForDistrict.Count )
                            ArcenDebugging.LogSingleLine( "Only got '" + district.MapGenVisualizationCharacter + "' with cells: " + district.Cells.Count +
                                " from the " + cellsForDistrict.Count + " we were supposed to add!", Verbosity.ShowAsError );
                    }
                }
                #endregion Make Real Districts, Pass 1

                debugStage = 6100;

                #region Merge Too-Small Districts With Neighbors
                //we assigned all of the districts in a starting fashion, but some got left out from being too small
                //those we now should add to a neighboring region
                {
                    int changeCount = 0;
                    do
                    {
                        debugStage = 6300;

                        changeCount = 0;
                        foreach ( MapCell cell in CityMap.Cells )
                        {
                            debugStage = 6400;
                            if ( cell.ParentTile.District != null )
                                continue; //this one is fine!

                            debugStage = 6700;
                            foreach ( MapCell neighbor in cell.AdjacentCells.GetRandomStartEnumerable( rand ) )
                            {
                                debugStage = 6800;
                                if ( neighbor.ParentTile.District != null && !neighbor.ParentTile.District.Type.PreferAvoidAddingOtherCellsToSelf )
                                {
                                    debugStage = 6900;
                                    neighbor.ParentTile.District.AddTileToDistrict( cell.ParentTile );

                                    debugStage = 6910;
                                    if ( districtDetailLogging )
                                        ArcenDebugging.LogSingleLine( "Added cell that would have been '" +
                                            (cell.ParentTile.DuringSeeding_DistrictType == null ? "[null]" : cell.ParentTile.DuringSeeding_DistrictType.ID ) + 
                                            "' to neighboring district of: " +
                                            neighbor.ParentTile.District.Type.ID + " due to too few cells in the original would-be district.", Verbosity.DoNotShow );

                                    debugStage = 6920;
                                    cell.ParentTile.DuringSeeding_DistrictType = neighbor.ParentTile.District.Type;
                                    changeCount++;
                                    break;
                                }
                            }
                        }
                    } while ( changeCount > 0 );

                    //when we have finished with the above, try again but with ones that are prefer-not assignments

                    debugStage = 7100;

                    do
                    {
                        changeCount = 0;
                        foreach ( MapCell cell in CityMap.Cells )
                        {
                            debugStage = 7200;
                            if ( cell.ParentTile.District != null )
                                continue; //this one is fine!

                            debugStage = 7700;
                            foreach ( MapCell neighbor in cell.AdjacentCells.GetRandomStartEnumerable( rand ) )
                            {
                                if ( neighbor.ParentTile.District != null )
                                {
                                    neighbor.ParentTile.District.AddTileToDistrict( cell.ParentTile );

                                    if ( districtDetailLogging )
                                        ArcenDebugging.LogSingleLine( "Added cell that would have been '" +
                                            cell.ParentTile.DuringSeeding_DistrictType.ID + "' to neighboring district of: " +
                                            neighbor.ParentTile.District.Type.ID + " due to too few cells in the original would-be district, even though the new region preferred not.", Verbosity.DoNotShow );
                                    cell.ParentTile.DuringSeeding_DistrictType = neighbor.ParentTile.District.Type;
                                    changeCount++;
                                    break;
                                }
                            }
                        }
                    } while ( changeCount > 0 );

                    debugStage = 8100;

                    //error if any nulls!
                    foreach ( MapCell cell in CityMap.Cells )
                    {
                        if ( cell.ParentTile.District == null )
                        {
                            debugStage = 8200;
                            ArcenDebugging.LogSingleLine( "Null district on cell that originally wanted '" + cell.ParentTile.DuringSeeding_DistrictType.ID +
                                "' and failed to merge with any other cells!", Verbosity.ShowAsError );
                        }
                    }
                }
                #endregion Merge Too-Small Districts With Neighbors

                debugStage = 13100;

                //#region Split Too-Large Districts
                //{
                //    int changeCount = 0;
                //    do
                //    {
                //        changeCount = 0;
                //        debugStage = 13200;
                //        for ( int i = CityMap.AllDistricts.Count - 1; i >= 0; i-- )
                //        {
                //            debugStage = 13300;
                //            MapDistrict district = CityMap.AllDistricts[i];

                //            debugStage = 13400;
                //            int maxToHave = rand.NextInclus( district.Type.RandSplitDistrictsThatAreAboveSizeMin, district.Type.RandSplitDistrictsThatAreAboveSizeMax );
                //            debugStage = 13500;
                //            if ( district.Tiles.Count > maxToHave )
                //            {
                //                debugStage = 13600;
                //                changeCount++;
                //                district.SplitCellsOffOfDistrict( maxToHave, rand, ref currentChar, districtDetailLogging );
                //            }
                //        }
                //    } while ( changeCount > 0 );

                //}
                //#endregion Split Too-Large Districts

                debugStage = 16100;

                debugStage = 17100;

                debugStage = 18100;

                #region Assign District Biomes
                foreach ( MapDistrict district in CityMap.AllDistricts )
                {
                    if ( district.Type.DecorationBiome.RegularLevelTypesWithFiles.Count <= 0 )
                    {
                        ArcenDebugging.LogSingleLine( "Could not find any regular level types with files in the decoration biome tag '" +
                            district.Type.DecorationBiome.ID + "'.", Verbosity.ShowAsError );
                        continue;
                    }
                    district.DecorationSource = district.Type.DecorationBiome.RegularLevelTypesWithFiles.GetRandom( rand );
                    if ( district.DecorationSource.Biome == null )
                        ArcenDebugging.LogSingleLine( "District Creation: Null biome definition on decoration source '" + district.DecorationSource + "'", Verbosity.ShowAsError );
                    else
                        district.Biome = district.DecorationSource.Biome;
                }
                #endregion
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "GenerateDistricts", debugStage, e, Verbosity.ShowAsError );
            }            
        }
        #endregion

        #region GenerateOutOfBoundsDistrict
        public static void GenerateOutOfBoundsDistrict( MersenneTwister rand )
        {
            MapDistrict district = MapDistrict.GetFromPoolOrCreate();
            district.Type = DistrictTypeTable.Instance.GetRowByID( "AncientRuins" );
            CityMap.AllDistricts.Add( district );
            CityMap.DistrictsByType.GetListForOrCreate( district.Type ).Add( district );
            district.DistrictID = (Int16)CityMap.AllDistricts.Count;
            CityMap.DistrictsByID.TryAdd( district.DistrictID, district, true );

            if ( district.Type.DecorationBiome.RegularLevelTypesWithFiles.Count <= 0 )
            {
                ArcenDebugging.LogSingleLine( "Could not find any regular level types with files in the decoration biome tag '" +
                    district.Type.DecorationBiome.ID + "'.", Verbosity.ShowAsError );
            }
            district.DecorationSource = district.Type.DecorationBiome.RegularLevelTypesWithFiles.GetRandom( rand );
            if ( district.DecorationSource.Biome == null )
                district.Biome = BiomeTypeTable.Instance.GetRowByID( "CutTreesThin" );
            else
                district.Biome = district.DecorationSource.Biome;

            foreach ( MapTile tile in CityMap.Tiles )
            {
                if ( tile.IsOutOfBoundsTile )
                    district.AddTileToDistrict( tile );
            }
        }
        #endregion

        private static void LogMapResults( ArcenIntRectangle CityBounds )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                bool districtDetailLogging = GameSettings.Current.GetBool( "Debug_CityDistrictDetailLogging" );

                debugStage = 1000;

                //#region Generated Map Cells Export
                //{
                //    string data = string.Empty;
                //    for ( int y = CityBounds.Top; y <= CityBounds.Bottom; y++ )
                //    {

                //        for ( int x = CityBounds.Left; x <= CityBounds.Right; x++ )
                //        {
                //            MapCell cell = CityMap.TryGetExistingCellAtLocation( x, y );
                //            if ( cell != null )
                //            {
                //                if ( cell.ParentTile.IsHoleTile || cell.ParentTile.TileType == null )
                //                    data += "_";
                //                else
                //                    data += cell.ParentTile.SeedingLogicCharForDebugMap;
                //            }
                //            else
                //                data += "-";
                //        }
                //        data += "\n";
                //    }

                //    ArcenDebugging.LogSingleLine( "Generated map cells:\n" + data, Verbosity.DoNotShow );
                //}
                //#endregion

                debugStage = 2000;

                #region Generated Map District Types Export
                if ( districtDetailLogging )
                {
                    string data = string.Empty;
                    for ( int y = CityBounds.YMin; y <= CityBounds.YMax; y++ )
                    {
                        for ( int x = CityBounds.XMin; x <= CityBounds.XMax; x++ )
                        {
                            MapCell cell = CityMap.TryGetExistingCellAtLocation( x, y );
                            if ( cell?.ParentTile?.DuringSeeding_DistrictType == null )
                                data += "_";
                            else
                                data += cell?.ParentTile?.DuringSeeding_DistrictType.CharForDistrictDebugMap;
                        }
                        data += "\n";
                    }

                    ArcenDebugging.LogSingleLine( "Generated map district types:\n" + data, Verbosity.DoNotShow );
                }
                #endregion

                debugStage = 3000;

                //#region Generated Map District Groups Export
                //{
                //    string data = string.Empty;
                //    for ( int y = CityBounds.Top; y <= CityBounds.Bottom; y++ )
                //    {
                //        for ( int x = CityBounds.Left; x <= CityBounds.Right; x++ )
                //        {
                //            MapCell cell = CityMap.TryGetExistingCellAtLocation( x, y );
                //            if ( cell?.ParentTile?.District == null )
                //                data += "_";
                //            else
                //                data += cell?.ParentTile?.District.MapGenVisualizationCharacter;
                //        }
                //        data += "\n";
                //    }

                //    ArcenDebugging.LogSingleLine( "Generated map district groups:\n" + data, Verbosity.DoNotShow );
                //}
                //#endregion

                if ( districtDetailLogging )
                {
                    debugStage = 4000;
                    for ( int i = CityMap.AllDistricts.Count - 1; i >= 0; i-- )
                    {
                        debugStage = 13300;
                        MapDistrict district = CityMap.AllDistricts[i];

                        ArcenDebugging.LogSingleLine( "District " + district.Type.ID + " size: " + district.Cells.Count, Verbosity.DoNotShow );
                    }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "LogMapResults", debugStage, e, Verbosity.ShowAsError );
            }
        }

        #region SeedSingleCityBlobToExistingTiles
        private static readonly List<MapCell> diggers = List<MapCell>.Create_WillNeverBeGCed( 16, "CityMapPopulator-diggers" );
        private static int remainingTilesToDig = 0;
        //loosely based on the ideas from http://noelberry.ca/posts/thecaves/index.html by Noel Berry
        //adapted from SeedCavernsInGround in A Valley Without Wind, which had adapted it further; the SeedCavernsInGroundInnerFat variant particularly

        private static void SeedSingleCityBlobToExistingTiles( CityStyleBlob Blob, MersenneTwister rand, ArcenIntRectangle CityBounds )
        {
            MapCell startingCell = null;
            int checkCount = 0;

            ThrowawayList<MapCell> potentialCells = new ThrowawayList<MapCell>();
            ThrowawayList<MapCell> cellsWeCanBlob = new ThrowawayList<MapCell>();
            ThrowawayDictionary<ArcenGroundPoint, bool> workingPoints = new ThrowawayDictionary<ArcenGroundPoint, bool>( 600 );
            foreach ( MapCell cell in CityMap.Cells )
            {
                if ( cell.IsStillNeedingSeedingLogic )
                    potentialCells.Add( cell );
            }

            remainingTilesToDig = Blob.DesiredSizeInCells;
            while ( remainingTilesToDig > 0 ) //if it runs out of diggers in one spot, it will pick a new random spot and try again
            {
                do
                {
                    if ( checkCount++ > 500 )
                        return; //fail!
                    if ( potentialCells.Count <= 0 )
                        return; //fail!

                    startingCell = potentialCells.GetRandom( rand );
                    potentialCells.Remove( startingCell );

                    if ( startingCell != null )
                    {
                        if ( !startingCell.IsStillNeedingSeedingLogic )
                        {
                            startingCell = null;
                            continue;
                        }

                        CityMap.FloodFillCellsFromPoint( cellsWeCanBlob, startingCell.CellLocation, workingPoints,
                            delegate ( MapCell other ) {
                                return other.IsStillNeedingSeedingLogic;
                            }, Blob.DesiredSizeInCells + 1 );

                        //no room to dig here, so look elsewhere
                        if ( cellsWeCanBlob.Count < Blob.DesiredSizeInCells )
                        {
                            startingCell = null;
                            continue;
                        }
                    }
                }
                while ( startingCell == null );

                SeedSingleCityBlobToExistingTiles_SetCell( startingCell, Blob.LogicType, Blob.DistrictType, Blob.CharForDebugMap );
                diggers.Clear();
                diggers.Add( startingCell );

                MapCell diggerCell;
                FourDirection digDirectionOriginal;
                FourDirection digDirection;
                MapCell tile;
                while ( remainingTilesToDig > 0 && diggers.Count > 0 )
                {
                    for ( int diggerIndex = 0; diggerIndex < diggers.Count; diggerIndex++ )
                    {
                        diggerCell = diggers[diggerIndex];

                        #region Find A Direction To Dig
                        digDirection = (FourDirection)rand.Next(
                            (int)FourDirection.North, (int)FourDirection.Length );

                        switch ( digDirection )
                        {
                            case FourDirection.East:
                                if ( diggerCell.CellLocation.X >= CityBounds.XMax - 10 )
                                    digDirection = FourDirection.West;
                                break;
                            case FourDirection.West:
                                if ( diggerCell.CellLocation.X <= CityBounds.XMin + 10 )
                                    digDirection = FourDirection.East;
                                break;
                            case FourDirection.South:
                                if ( diggerCell.CellLocation.Z >= CityBounds.YMax - 10 )
                                    digDirection = FourDirection.North;
                                break;
                            case FourDirection.North:
                                if ( diggerCell.CellLocation.Z <= CityBounds.YMin + 10 )
                                    digDirection = FourDirection.South;
                                break;
                        }

                        digDirectionOriginal = digDirection;

                        tile = null;
                        do
                        {
                            tile = diggerCell.GetAdjacentCellInDirection( digDirection );

                            if ( tile != null && !tile.IsStillNeedingSeedingLogic )
                                tile = null;

                            digDirection++;
                            if ( digDirection >= FourDirection.Length )
                                digDirection = FourDirection.North;
                        }
                        while ( tile == null && digDirection != digDirectionOriginal );
                        #endregion

                        if ( tile == null )
                        {
                            diggers.RemoveAt( diggerIndex );
                            diggerIndex--;
                        }
                        else
                        {
                            diggerCell = tile;
                            SeedSingleCityBlobToExistingTiles_SetCell( diggerCell, Blob.LogicType, Blob.DistrictType, Blob.CharForDebugMap );
                            diggers[diggerIndex] = diggerCell;

                            if ( rand.Next( 0, 100 ) < Blob.PercentChanceOfSpawningDiggers )
                            {
                                #region Find A Direction In Which To Spawn A Digger
                                digDirection = digDirectionOriginal = (FourDirection)rand.Next(
                                    (int)FourDirection.North, (int)FourDirection.Length );
                                do
                                {
                                    tile = null;
                                    switch ( digDirection )
                                    {
                                        case FourDirection.North:
                                            if ( diggerCell.CellLocation.Z > CityBounds.YMin + 10 ) //this is actually in the south
                                                tile = diggerCell.CellNorth;
                                            break;
                                        case FourDirection.South:
                                            if ( diggerCell.CellLocation.Z < CityBounds.YMax - 10 ) //this is actually in the north
                                                tile = diggerCell.CellSouth;
                                            break;
                                        case FourDirection.East:
                                            if ( diggerCell.CellLocation.X < CityBounds.XMax - 10 )
                                                tile = diggerCell.CellEast;
                                            break;
                                        case FourDirection.West:
                                            if ( diggerCell.CellLocation.X > CityBounds.XMin + 10 )
                                                tile = diggerCell.CellWest;
                                            break;
                                    }

                                    if ( tile != null && !tile.IsStillNeedingSeedingLogic )
                                        tile = null;

                                    digDirection++;
                                    if ( digDirection >= FourDirection.Length )
                                        digDirection = FourDirection.North;
                                }
                                while ( tile == null && digDirection != digDirectionOriginal );
                                #endregion

                                if ( tile != null )
                                {
                                    diggerCell = tile;
                                    SeedSingleCityBlobToExistingTiles_SetCell( diggerCell, Blob.LogicType, Blob.DistrictType, Blob.CharForDebugMap );
                                    diggers.Add( diggerCell );
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void SeedSingleCityBlobToExistingTiles_SetCell( MapCell Cell, PlaceableSeedingLogicType SeedingLogic, DistrictType DistrictType, char CharForDebug )
        {
            MapTile parentTile = Cell.ParentTile;
            if ( parentTile == null)
            {
                ArcenDebugging.LogSingleLine( "Tried to set seeding logic on cell where Cell.ParentTile was null!", Verbosity.ShowAsError );
                return;
            }
            if ( parentTile.SeedingLogic != null )
            {
                ArcenDebugging.LogSingleLine( "Tried to set seeding logic on cell where Cell.ParentTile.SeedingLogic was already set!", Verbosity.ShowAsError );
                return;
            }

            parentTile.SeedingLogic = SeedingLogic;
            parentTile.SeedingLogicCharForDebugMap = CharForDebug;
            parentTile.DuringSeeding_DistrictType = DistrictType;
            remainingTilesToDig -= MathA.Max( 1, parentTile.CellsList.Count );
        }
        #endregion SeedSingleCityBlobToExistingTiles
    }
}

using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public static class MetaRegionPopulator
    {
        private static int buildingZonesStillOutstanding = 0;
        private static int decorationZonesStillOutstanding = 0;

        #region Properties
        public static int ZonesStillWorking
        {
            get { return buildingZonesStillOutstanding + decorationZonesStillOutstanding; }
        }

        public static bool IsReadyToStart
        {
            get { return buildingZonesStillOutstanding <= 0 && decorationZonesStillOutstanding <= 0; }
        }
        #endregion

        private static readonly ConcurrentQueue<MapItem> allPossibleObjectCollisions = ConcurrentQueue<MapItem>.Create_WillNeverBeGCed( "MetaRegionPopulator-allPossibleCollisions" );
        private static readonly ConcurrentQueue<IShapeToDraw> debugShapesToDraw = ConcurrentQueue<IShapeToDraw>.Create_WillNeverBeGCed( "MetaRegionPopulator-debugShapesToDraw" );

        public static void PopulateAllRegionsInLevelEditor()
        {
            if ( !IsReadyToStart )
                return; //don't let us get in there twice at once

            allPossibleObjectCollisions.Clear();
            debugShapesToDraw.Clear();

            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
            //in case we already had any test objects, clear those!
            levelEditorHook.ClearAllLevelEditorTestObjects();

            #region First Build A List Of Possible Collisions For Objects
            //we will add to this list as we go and are adding objects.
            //threads need to be considering things that may not exist yet
            foreach ( A5ObjectRoot objRoot in A5ObjectRootTable.Instance.Rows )
            {
                if ( objRoot.InEditorInstances_IncludesDeleted.Count <= 0 )
                    continue;
                if ( objRoot.IsMetaRegion )
                    continue; //skip all of these!

                foreach ( IA5Placeable iPlace in objRoot.InEditorInstances_IncludesDeleted )
                {
                    if ( !iPlace.IsValid || !iPlace.IsObjectActive )
                        continue; //this was deleted
                    A5Placeable place = iPlace as A5Placeable;
                    if ( !place )
                        continue;
                    if ( !place.OBBAndBoundsCache.HasBeenSet ) //if not already set, try to set it now
                    {
                        if ( !place.TryCalculateAndCacheTRSAndOBBAndBounds() )
                            continue;
                    }

                    MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( null ); //test purpose only in the level editor, so don't care
                    item.Type = place.ObjRoot;
                    item.SetPosition( place.TRSCache.LocalPos );
                    item.SetRotation( place.TRSCache.Rotation );
                    item.Scale = place.TRSCache.LocalScale;
                    item.OBBCache.SetToOBB( place.OBBAndBoundsCache.OBB );
                    allPossibleObjectCollisions.Enqueue( item );
                }
            }
            #endregion

            LevelTag downtownAccents = LevelTagTable.Instance.GetRowByID( "VegetationWildLarge" );
            LevelType chosenType = downtownAccents.RegularLevelTypesWithFiles.Count == 0 ? null :
                downtownAccents.RegularLevelTypesWithFiles[Engine_Universal.PermanentQualityRandom.Next( 0, downtownAccents.RegularLevelTypesWithFiles.Count )];

            #region Building Zones Outer Logic
            //this part has to be done on the main thread, and it loops through and starts all the threads for buildings
            //each one does building placement, and then decoration zone placement
            foreach ( A5ObjectRoot objRoot in A5ObjectAggregation.MetaRegions )
            {
                if ( objRoot.InEditorInstances_IncludesDeleted.Count <= 0 )
                    continue;
                if ( objRoot.ExtraPlaceableData == null )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_ShortID.Length <= 0 )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                    continue; //don't handle any decoration zones here

                foreach ( IA5Placeable iPlace in objRoot.InEditorInstances_IncludesDeleted )
                {
                    if ( !iPlace.IsValid || !iPlace.IsObjectActive )
                        continue; //this was deleted
                    if ( !iPlace.OBBAndBoundsCache.HasBeenSet )
                        continue;
                    A5Placeable place = iPlace as A5Placeable;
                    if ( !place ) 
                        continue;

                    Interlocked.Increment( ref buildingZonesStillOutstanding );

                    int randSeed = Engine_Universal.PermanentQualityRandom.Next();
                    if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.PopulateRegion",
                        ( TaskStartData startData ) =>
                        {
                            try
                            {
                                PopulateRegion_Buildings( randSeed, objRoot, place, objRoot.ExtraPlaceableData.MetaOnly_ShortID );
                                PopulateRegion_Decoration( true, randSeed, objRoot, place, objRoot.ExtraPlaceableData.MetaOnly_ShortID, chosenType );
                            }
                            catch ( Exception e )
                            {
                                if ( !startData.IsMeantToSilentlyDie() )
                                    ArcenDebugging.LogWithStack( "MetaRegionPopulator.PopulateAllRegionsInLevelEditor background thread error: " + e, Verbosity.ShowAsError );
                            }
                            Interlocked.Decrement( ref buildingZonesStillOutstanding );
                            FinishTestSetupIfLastThread();
                            //ArcenDebugging.LogSingleLine( "buildingZonesStillOutstanding: " + buildingZonesStillOutstanding, Verbosity.ShowAsError );
                        } ) )
                    {
                        //failed to start the above
                        Interlocked.Decrement( ref buildingZonesStillOutstanding );
                        FinishTestSetupIfLastThread();
                    }
                }
            }
            #endregion Building Zones Outer Logic

            #region Decoration Zones Outer Logic
            //this part has to be done on the main thread, and it loops through and starts all the threads for decorations
            foreach ( A5ObjectRoot objRoot in A5ObjectAggregation.MetaRegions )
            {
                if ( objRoot.InEditorInstances_IncludesDeleted.Count <= 0 )
                    continue;
                if ( objRoot.ExtraPlaceableData == null )
                    continue;
                if ( objRoot.ExtraPlaceableData.MetaOnly_ShortID.Length <= 0 )
                    continue;
                if ( !objRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                    continue; //don't handle any non-decoration zones here

                foreach ( IA5Placeable iPlace in objRoot.InEditorInstances_IncludesDeleted )
                {
                    if ( !iPlace.IsValid || !iPlace.IsObjectActive )
                        continue; //this was deleted
                    if ( !iPlace.OBBAndBoundsCache.HasBeenSet )
                        continue;
                    A5Placeable place = iPlace as A5Placeable;
                    if ( !place )
                        continue;

                    Interlocked.Increment( ref decorationZonesStillOutstanding );

                    int randSeed = Engine_Universal.PermanentQualityRandom.Next();
                    if ( !ArcenThreading.RunTaskOnBackgroundThreadAndErrorIfCannotStart( "_Data.PopulateRegion",
                        ( TaskStartData startData ) =>
                        {
                            try
                            {
                                PopulateRegion_Decoration( false, randSeed, objRoot, place, objRoot.ExtraPlaceableData.MetaOnly_ShortID, chosenType );
                            }
                            catch ( Exception e )
                            {
                                if ( !startData.IsMeantToSilentlyDie() )
                                    ArcenDebugging.LogWithStack( "MetaRegionPopulator.PopulateAllRegionsInLevelEditor background thread error: " + e, Verbosity.ShowAsError );
                            }
                            Interlocked.Decrement( ref decorationZonesStillOutstanding );
                            FinishTestSetupIfLastThread();
                            //ArcenDebugging.LogSingleLine( "decorationZonesStillOutstanding: " + decorationZonesStillOutstanding, Verbosity.ShowAsError );
                        } ) )
                    {
                        //failed to start the above
                        Interlocked.Decrement( ref decorationZonesStillOutstanding );
                        FinishTestSetupIfLastThread();
                    }
                }
            }
            #endregion Decoration Zones Outer Logic
        }

        private static readonly Color hotPink = ColorMath.HexToColor( "ff4faa" );
        private static readonly Color limeGreen = ColorMath.HexToColor( "bfff4f" );
        private static readonly Color brightCyan = ColorMath.HexToColor( "38e6ec" );
        private static readonly Color gold = ColorMath.HexToColor( "f5db62" );

        #region PopulateRegion_Buildings
        private static void PopulateRegion_Buildings( int randSeed, A5ObjectRoot MetaRegionObjRoot, A5Placeable MetaRegionPlace, string ShortID )
        {
            OBBUnity regionOBB = MetaRegionPlace.OBBAndBoundsCache.OBB;
            MersenneTwister rand = new MersenneTwister( randSeed );

            ThrowawayList<MapItem> addedBuildings = new ThrowawayList<MapItem>();
            ThrowawayDictionary<int, bool> testedTypes = new ThrowawayDictionary<int, bool>( 500 );

            //in here we assume that the only thing we could collide with are other buildings we already put in

            MapUtility.GenerateRegionAxisVectors( regionOBB, out Vector3 rightAxis, out Vector3 lookAxis );
            MapUtility.GenerateRegionStartingPoints( regionOBB, out Vector3 startN, out Vector3 startS, out Vector3 startE, out Vector3 startW );

            if ( !MetaRegionPlace.GetHasNSEWIndicators() )
            {
                //just pack these in however!  We will start from the north, though
                PlaceableSeedingMegaGroup seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID( "DowntownSkyscrapersAndSimilarWithNoSides" );

                Vector3 originPoint = startN;

                int priorCount;
                int loopCount = 0;
                do
                {
                    priorCount = addedBuildings.Count;
                    GenerateInitialBuildingSetInLocalSpace( FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, addedBuildings, testedTypes, false );
                }
                while ( priorCount != addedBuildings.Count && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less
            }
            else
            {
                int totalSidesEnabled = 0;
                if ( MetaRegionPlace.DirEnabledN )
                    totalSidesEnabled++;
                if ( MetaRegionPlace.DirEnabledS )
                    totalSidesEnabled++;
                if ( MetaRegionPlace.DirEnabledE )
                    totalSidesEnabled++;
                if ( MetaRegionPlace.DirEnabledW )
                    totalSidesEnabled++;

                PlaceableSeedingMegaGroup seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID(
                    rand.Next( 0, 100 ) < 70 ? "DowntownMidriseResidentialFrontFacing" : "MostlySkyscrapers" );

                if ( MetaRegionPlace.DirEnabledN )
                {
                    Vector3 originPoint = startN;

                    GenerateInitialBuildingSetInLocalSpace( FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, addedBuildings, testedTypes, true );

                    #region If Building A Single-Sided Stack
                    //if ( totalSidesEnabled == 1 )
                    //{
                    //    seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID( "DowntownSkyscrapersAndSimilarWithNoSides" );

                    //    int priorCount;
                    //    int loopCount = 0;
                    //    do
                    //    {
                    //        priorCount = addedBuildings.Count;
                    //        GenerateInitialBuildingSetInLocalSpace( FourDirection.North, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                    //            0.01f, 0.2f, rand, addedBuildings, testedTypes, false );
                    //    }
                    //    while ( priorCount != addedBuildings.Count && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less
                    //}
                    #endregion
                }
                if ( MetaRegionPlace.DirEnabledS )
                {
                    Vector3 originPoint = startS;

                    GenerateInitialBuildingSetInLocalSpace( FourDirection.South, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, addedBuildings, testedTypes, true );

                    #region If Building A Single-Sided Stack
                    //if ( totalSidesEnabled == 1 )
                    //{
                    //    seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID( "DowntownSkyscrapersAndSimilarWithNoSides" );

                    //    int priorCount;
                    //    int loopCount = 0;
                    //    do
                    //    {
                    //        priorCount = addedBuildings.Count;
                    //        GenerateInitialBuildingSetInLocalSpace( FourDirection.South, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                    //            0.01f, 0.2f, rand, addedBuildings, testedTypes, false );
                    //    }
                    //    while ( priorCount != addedBuildings.Count && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less
                    //}
                    #endregion
                }
                if ( MetaRegionPlace.DirEnabledE )
                {
                    Vector3 originPoint = startE;

                    GenerateInitialBuildingSetInLocalSpace( FourDirection.East, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, addedBuildings, testedTypes, true );

                    #region If Building A Single-Sided Stack
                    //if ( totalSidesEnabled == 1 )
                    //{
                    //    seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID( "DowntownSkyscrapersAndSimilarWithNoSides" );

                    //    int priorCount;
                    //    int loopCount = 0;
                    //    do
                    //    {
                    //        priorCount = addedBuildings.Count;
                    //        GenerateInitialBuildingSetInLocalSpace( FourDirection.East, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                    //            0.01f, 0.2f, rand, addedBuildings, testedTypes, false );
                    //    }
                    //    while ( priorCount != addedBuildings.Count && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less
                    //}
                    #endregion
                }
                if ( MetaRegionPlace.DirEnabledW )
                {
                    Vector3 originPoint = startW;

                    GenerateInitialBuildingSetInLocalSpace( FourDirection.West, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                        0.01f, 0.2f, rand, addedBuildings, testedTypes, true );

                    #region If Building A Single-Sided Stack
                    //if ( totalSidesEnabled == 1 )
                    //{
                    //    seedingGroup = PlaceableSeedingMegaGroupTable.Instance.GetRowByID( "DowntownSkyscrapersAndSimilarWithNoSides" );

                    //    int priorCount;
                    //    int loopCount = 0;
                    //    do
                    //    {
                    //        priorCount = addedBuildings.Count;
                    //        GenerateInitialBuildingSetInLocalSpace( FourDirection.West, originPoint, rightAxis, lookAxis, regionOBB, seedingGroup,
                    //            0.01f, 0.2f, rand, addedBuildings, testedTypes, false );
                    //    }
                    //    while ( priorCount != addedBuildings.Count && loopCount++ <= 100 ); //add as long as we keep adding them, or 100 loops, whichever is less
                    //}
                    #endregion
                }
            }
        }
        #endregion

        #region GenerateInitialBuildingSetInLocalSpace
        private static void GenerateInitialBuildingSetInLocalSpace( FourDirection Dir, Vector3 currentPoint, Vector3 rightAxis, Vector3 lookAxis, OBBUnity regionOBB,
            PlaceableSeedingMegaGroup SeedMegaGroup, float MinSpacingBetween, float MaxSpacingBetween, MersenneTwister rand, ThrowawayList<MapItem> addedBuildings,
            ThrowawayDictionary<int, bool> testedTypes, bool MoveLaterallyWhenHaveBuildingCollisions )
        {
            //these will be safely reused in the following areas
            MapUtility.GenerateOrientedZoneMovement( Dir, rightAxis, lookAxis, out Vector3 LateralMove, out Vector3 DepthMove );
            MapUtility.GetRegionXZForFitCheckingFromSideOrientation( Dir, regionOBB, out float regionSizeX, out float regionSizeZ );
            float remainRegionSizeX = regionSizeX;
            float remainRegionSizeZ = regionSizeZ;

            int totalAttempts = 0;
            int addedBuildingCountWhenWeStarted = addedBuildings.Count;

            //this is all unique for each building being tested
            testedTypes.Clear();
            do
            {
                bool didSeed = false;
                for ( int attemptCount = 0; attemptCount < 500; attemptCount++ )
                {
                    totalAttempts++;
                    A5ObjectRoot objRoot = SeedMegaGroup.DrawRandomItem( rand );
                    if ( testedTypes.ContainsKey( objRoot.ObjectRootID ) )
                        continue; //don't bother checking more if that's the case, on to the next loop in our iteration

                    int yRot = objRoot.GetRotationToFaceFront( Dir );
                    MapUtility.GetFullXZForFitCheckingFromRotationY( objRoot, yRot, out float buildingX, out float buildingZ );
                    if ( buildingX > remainRegionSizeX || buildingZ > remainRegionSizeZ )
                    {
                        testedTypes[objRoot.ObjectRootID] = true;
                        continue; //this won't fit, so skip it.  And skip any more checks
                    }

                    //this WILL fit, so far as we know right now.  So generate the instruction and see if it will fit well
                    MapInstruction inst = MapUtility.GenerateInstructionForOrientedBuildingInOrientedZone( Dir, objRoot,
                        currentPoint, yRot, regionOBB, rightAxis, lookAxis, MapInstructionReason.TestData_Clickable );
                    OBBUnity newOBB = objRoot.CalculateOBBForItem( inst.Position, inst.Rotation, inst.Scale );

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

                    //inst.DebugText = "remainRegX: " + remainRegionSizeX + "  regX: " + regionSizeX + " remainRegZ: " + remainRegionSizeZ + " regZ: " + regionSizeZ +
                    //    " buildX: " + buildingX + " buildZ: " + buildingZ;

                    //inst.DebugText = "origX: " + objRoot.OriginalAABBSize.x + "  origZ: " + objRoot.OriginalAABBSize.z + " yRot: " + yRot +
                    //    " \nbuildX: " + buildingX + " buildZ: " + buildingZ + " Dir: " + Dir + " NSF: " + objRoot.placeable.GetIsNorthOrSouthFaceFront();

                    //this will fit for sure!
                    MapGenerationCoordinator.LevelEditorInstructionQueue.Enqueue( inst );

                    MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( null ); //test purpose only in the level editor, so don't care
                    item.Type = inst.Type;
                    item.SetPosition( inst.Position );
                    item.SetRotation( inst.Rotation );
                    item.Scale = inst.Scale;
                    item.OBBCache.SetToOBB( newOBB );
                    addedBuildings.Add( item );
                    allPossibleObjectCollisions.Enqueue( item );

                    float spacingToAdd = rand.NextFloat( MinSpacingBetween, MaxSpacingBetween );
                    remainRegionSizeX -= (spacingToAdd + buildingX);
                    currentPoint += (spacingToAdd + buildingX) * LateralMove;

                }
                if ( !didSeed )
                    break; //must be too tight
            }
            while ( totalAttempts < 5000 );
        }
        #endregion

        #region PopulateRegion_Decoration
        private static void PopulateRegion_Decoration( bool isForExistingBuildingZone, int randSeed, A5ObjectRoot MetaRegionObjRoot, 
            A5Placeable MetaRegionPlace, string ShortID, LevelType ChosenType )
        {
            if ( !isForExistingBuildingZone )
            {
                int waits = 0;
                while ( buildingZonesStillOutstanding > 0 && waits++ < 1000 ) //so don't wait more than 10s
                    System.Threading.Thread.Sleep( 10 ); //wait 10ms
            }

            if ( ChosenType.AvailableFileNames.Count == 0 )
                return; //uh... whoops?

            OBBUnity regionOBB = MetaRegionPlace.OBBAndBoundsCache.OBB;
            MersenneTwister rand = new MersenneTwister( randSeed );

            ReferenceLevelData decorationSource = new ReferenceLevelData( ChosenType.AvailableFileNames[rand.Next( 0, ChosenType.AvailableFileNames.Count )] );
            decorationSource.LoadOnBackgroundThread(); //we are already on a background thread, so we just do it directly inline here.

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

            float regionRotation = MetaRegionPlace.TRSCache.Rotation.y;
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

            LevelType levelType = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.CurrentLevelType;
            ArcenFloatRectangle levelRect = ArcenFloatRectangle.CreateFromCenterAndHalfSize( 0, 0, levelType.SizeX, levelType.SizeZ );

            float xSourceOffset = rand.NextFloat( 0, levelSizeX - regionSizeX ) / 2;
            float zSourceOffset = rand.NextFloat( 0, levelSizeZ - regionSizeZ ) / 2;

            foreach ( ReferenceObject obj in decorationSource.AllObjects )
            {
                if ( obj.ObjRoot.IsMetaRegion )
                    continue; //we don't actually seed the meta regions from decoration zones

                Vector3 objPos = obj.pos;
                Quaternion rot = Quaternion.Euler( obj.rot.x, obj.rot.y, obj.rot.z );
                MathA.RotateAround( ref objPos, ref rot, Vector3.zero, new Vector3( 0, 1, 0 ), regionRotation );

                objPos = objPos.PlusX( regionOBB.Center.x - xSourceOffset );
                objPos = objPos.PlusZ( regionOBB.Center.z - zSourceOffset );

                Vector3 destPoint = objPos;
                OBBUnity newOBB = obj.ObjRoot.CalculateOBBForItem( destPoint, rot, obj.scale );
                if ( !newOBB.IntersectsOBB( regionOBB ) )
                    continue; //if this is not part of the region's area, then skip it!

                ArcenFloatRectangle boundsRect = ArcenFloatRectangle.CreateFromBoundsXZ( new Bounds( newOBB.Center, newOBB.Size ) );

                if ( !levelRect.ContainsRectangle( boundsRect ) ) //if one rectangle does not contain the other, then out of bounds
                    continue;

                //bool debugCollisions = false;
                //if ( obj.ObjRoot.IDPath.Contains( "Tree Scoop C" ) || obj.ObjRoot.IDPath.Contains( "Autumn Scoop A" ) )
                //{
                //    debugCollisions = true;
                //    //ArcenDebugging.LogSingleLine( obj.ObjRoot.IDPath + " OBB: " + newOBB + " orig: " +
                //    //    obj.ObjRoot.OriginalAABBSize + " final: " + aabb.Size, Verbosity.DoNotShow );
                //}

                #region test against anything and everything to make sure there are no hits!
                if ( allPossibleObjectCollisions.Count > 0 )
                {
                    bool foundAnyHits = false;
                    foreach ( MapItem existingItem in allPossibleObjectCollisions )
                    {
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
                                        worldCenter, worldSize, existingItem.rawReadRot ) )
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
                                else
                                {
                                    //if ( debugCollisions && existingItem.Type.IDPath.Contains( "Hwy" ) )
                                    //{
                                    //    //ArcenDebugging.LogSingleLine( obj.ObjRoot.IDPath + " OBB: " + newOBB + " hit " + existingItem.Type.IDPath + 
                                    //    //    " but then did not hit any sub-colliders.", Verbosity.DoNotShow );

                                    //    {
                                    //        DrawShape_WireBoxOriented wireBox;
                                    //        wireBox.Center = newOBB.Center;
                                    //        wireBox.Size = newOBB.Size;
                                    //        wireBox.Rotation = newOBB.Rotation;
                                    //        wireBox.Color = ColorMath.Red;
                                    //        wireBox.InLocalSpaceOfPlaceable = false;
                                    //        wireBox.DrawSlightlyLargerAndPulsing = false;
                                    //        debugShapesToDraw.Enqueue( wireBox );

                                    //        ArcenDebugging.LogSingleLine( "add source wire box", Verbosity.DoNotShow );
                                    //    }

                                    //    foreach ( CollisionBox box in existingItem.Type.CollisionBoxes )
                                    //    {
                                    //        Vector3 worldCenter = existingItem.TransformPoint_Threadsafe( box.Center );
                                    //        Vector3 worldSize = box.Size.ComponentWiseMult( existingItem.Scale );
                                    //        if ( existingItem.Type.ExtraPlaceableData.ExpandIndividualColliderCheckingUpAndDownALotInCollisionChecks )
                                    //        {
                                    //            //make sure that the Y is very tall and we don't miss it for that reason, but only if specified
                                    //            worldSize = worldSize.ReplaceY( 10 );
                                    //        }

                                    //        if ( BoxMath.BoxIntersectsBox( newOBB.Center, newOBB.Size, newOBB.Rotation,
                                    //            worldCenter, worldSize, existingItem.Rotation ) )
                                    //        {
                                    //            hadActualInnerCollision = true;
                                    //            //ArcenDebugging.LogSingleLine( obj.ObjRoot.IDPath + " wait now a success??", Verbosity.DoNotShow );
                                    //            throw new Exception( "Wait now a success?");
                                    //        }
                                    //        else
                                    //        {
                                    //            DrawShape_WireBoxOriented wireBox;
                                    //            wireBox.Center = worldCenter;
                                    //            wireBox.Size = worldSize;
                                    //            wireBox.Rotation = existingItem.Rotation;
                                    //            wireBox.Color = ColorMath.LeafGreen;
                                    //            wireBox.InLocalSpaceOfPlaceable = false;
                                    //            wireBox.DrawSlightlyLargerAndPulsing = false;
                                    //            debugShapesToDraw.Enqueue( wireBox );
                                    //            //ArcenDebugging.LogSingleLine( "box coll fail: " + newOBB.Center + " " + newOBB.Size + " " + newOBB.Rotation +
                                    //            //    "\n vs " + worldCenter + " " + worldSize + " " + existingItem.Rotation, Verbosity.DoNotShow );

                                    //            ArcenDebugging.LogSingleLine( "add dest wire box", Verbosity.DoNotShow );
                                    //        }
                                    //    }
                                    //}
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

                //now build the instruction since we are going to populate it
                MapInstruction inst;
                inst.Type = obj.ObjRoot;
                inst.Position = destPoint;
                inst.Scale = obj.scale;
                inst.Rotation = rot;
                inst.Reason = MapInstructionReason.TestData_Clickable;
                inst.DebugText = String.Empty;

                //this will fit for sure!
                MapGenerationCoordinator.LevelEditorInstructionQueue.Enqueue( inst );

                //don't do any collision detections now
                //MapItem item;
                //item.Type = inst.Type;
                //item.OBB = newOBB;
                //item.TRSCache.HasBeenSet = true;
                //item.TRSCache.LocalPos = inst.Position;
                //item.TRSCache.LocalScale = inst.Scale;
                //item.TRSCache.Rotation = inst.Rotation;
                //allPossibleObjectCollisions.Enqueue( item );
            }
        }
        #endregion

        #region FinishTestSetupIfLastThread
        private static void FinishTestSetupIfLastThread()
        {
            if ( buildingZonesStillOutstanding > 0 || decorationZonesStillOutstanding > 0 )
                return; //we're not the last one, so skip!

            //we are the last one, so transfer our debug shapes to the main list
            LevelEditorHookBase levelEditorHook = (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook;
            List<IShapeToDraw> list = levelEditorHook.GetListOfDebugShapesToDraw();
            list.Clear();
            while ( debugShapesToDraw.Count > 0 )
            {
                if ( debugShapesToDraw.TryDequeue( out IShapeToDraw draw ) )
                    list.Add( draw );
            }
        }
        #endregion
    }
}
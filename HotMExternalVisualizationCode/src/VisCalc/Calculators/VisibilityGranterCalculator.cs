using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using System.CodeDom;

namespace Arcen.HotM.ExternalVis
{
    public static class VisibilityGranterCalculator
    {
        private static int IsCalculatingNow = 0;
        public static float LastStartedOrFinished = 0f;

        public static bool GetIsCalculatingNow()
        {
            return IsCalculatingNow > 0;
        }

        public static void OnGameClear()
        {
            IsCalculatingNow = 0;
        }

        public static void Recalculate()
        {
            if ( IsCalculatingNow > 0 )
                return;
            if ( Interlocked.Exchange( ref IsCalculatingNow, 1 ) != 0 )
                return;
            SimCommon.NeedsVisibilityGranterRecalculation = false;

            if ( !ArcenThreading.RunTaskOnBackgroundThread( "_Inter.RunVisibilityHelperCalculations",
                ( TaskStartData startData ) =>
                {
                    LastStartedOrFinished = ArcenTime.AnyTimeSinceStartF;
                    SimCommon.VisibilityGranterCycle++;
                    try
                    {
                        RecalculateInner();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogWithStack( "VisibilityGranterCalculator.RunVisibilityHelperCalculations background thread error: " + e, Verbosity.ShowAsError );
                    }

                    Interlocked.Exchange( ref IsCalculatingNow, 0 );
                    LastStartedOrFinished = ArcenTime.AnyTimeSinceStartF;
                    //since targeting may not have worked great while this was in progress, do that now
                    SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true;
                } ) )
            {
                SimCommon.NeedsVisibilityGranterRecalculation = true;
                Interlocked.Exchange( ref IsCalculatingNow, 0 );
            }
        }

        private static void RecalculateInner()
        {
            Stopwatch sw = Stopwatch.StartNew();
            World.Forces.RecalculateUnitsDeployedAndStored(); //this must be done before the below is done!
            RecalculateVisibilityGranters();
            SimTimingInfo.VisibilityGranters.LogCurrentTicks( (int)sw.ElapsedTicks );

            int atStart = (int)sw.ElapsedTicks;
            RecalculateCollidables();

            #region Expire Older CollidablesCreatedSinceLastVisibilityGranterCalculation
            Int64 currentCycle = SimCommon.VisibilityGranterCycle;
            foreach ( KeyValuePair<Int64, RefPair<ICollidable, long>> kv in CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation )
            {
                if ( kv.Value.RightItem < currentCycle )
                    CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.RemoveEntry( kv.Key );
            }
            #endregion

            SimTimingInfo.Collidables.LogCurrentTicks( (int)sw.ElapsedTicks - atStart );
        }

        private static void RecalculateVisibilityGranters()
        {
            #region Early - Calculate VisibilityGrantersWithinRange and UnitsWithinAttackRange
            foreach ( MapTile tile in CityMap.Tiles )
            {
                tile.FogOfWarCuttersWithinRange.ClearConstructionListForStartingConstruction();
                tile.NPCRevealersWithinRange.ClearConstructionListForStartingConstruction();
                tile.ActorsWithinMaxNPCAttackRange.ClearConstructionListForStartingConstruction();
            }

            SimCommon.AllActorsForTargeting.ClearConstructionListForStartingConstruction();

            //vehicles of the player always grant visibility
            {
                DictionaryView<int, ISimMachineVehicle> machineVehicles = World.Forces.GetMachineVehiclesByID();
                foreach ( KeyValuePair<int, ISimMachineVehicle> kv in machineVehicles )
                {
                    ISimMachineVehicle vehicle = kv.Value;
                    if ( vehicle.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || vehicle.IsFullDead )
                    {
                        vehicle.FogOfWarCutting = FogOfWarCutter.CreateBlank();
                        vehicle.NPCRevealing = NPCRevealer.CreateBlank();
                        continue;
                    }

                    SimCommon.AllActorsForTargeting.AddToConstructionList( vehicle );

                    Vector3 loc = vehicle.WorldLocation;
                    float actualCutRange = vehicle.GetAttackRange();
                    float extendedCutRange = MathA.Min( SimCommon.MaxNPCAttackRange, vehicle.GetMovementRange() );
                    float revealRange = vehicle.GetNPCRevealRange();
                    if ( actualCutRange > extendedCutRange )
                        extendedCutRange = actualCutRange;
                    if ( revealRange < actualCutRange )
                        revealRange = actualCutRange;

                    float largestRange = MathA.Max( MathA.Max( actualCutRange, extendedCutRange, revealRange ), SimCommon.MaxNPCAttackRange );

                    FogOfWarCutter cutter = FogOfWarCutter.Create( loc, actualCutRange, extendedCutRange, vehicle );
                    vehicle.FogOfWarCutting = cutter;
                    NPCRevealer revealer = NPCRevealer.Create( loc, revealRange, actualCutRange, vehicle );
                    vehicle.NPCRevealing = revealer;

                    //the following code is repeated in the units section, as it's inlined for performance reasons
                    float cut_minX = loc.x - extendedCutRange;
                    float cut_maxX = loc.x + extendedCutRange;
                    float cut_minZ = loc.z - extendedCutRange;
                    float cut_maxZ = loc.z + extendedCutRange;
                    float reveal_minX = loc.x - revealRange;
                    float reveal_maxX = loc.x + revealRange;
                    float reveal_minZ = loc.z - revealRange;
                    float reveal_maxZ = loc.z + revealRange;

                    float max_minX = loc.x - largestRange;
                    float max_maxX = loc.x + largestRange;
                    float max_minZ = loc.z - largestRange;
                    float max_maxZ = loc.z + largestRange;

                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        bool addedCut = false;
                        bool addedReveal = false;
                        bool addedMax = false;
                        for ( int i = 0; i < tile.CellsList.Count; i++ )
                        {
                            MapCell cell = tile.CellsList[i];
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                                rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                                continue;
                            if ( !addedMax )
                            {
                                addedMax = true;
                                tile.ActorsWithinMaxNPCAttackRange.AddToConstructionList( vehicle );
                            }

                            if ( !addedCut )
                            {
                                if ( rect.XMax <= cut_minX || rect.XMin >= cut_maxX ||
                                    rect.YMax <= cut_minZ || rect.YMin >= cut_maxZ )
                                { }
                                else
                                {
                                    addedCut = true;
                                    //we intersected one of these cells, so log us at this tile
                                    tile.FogOfWarCuttersWithinRange.AddToConstructionList( cutter );
                                }
                            }
                            if ( !addedReveal )
                            {
                                if ( rect.XMax <= reveal_minX || rect.XMin >= reveal_maxX ||
                                    rect.YMax <= reveal_minZ || rect.YMin >= reveal_maxZ )
                                { }
                                else
                                {
                                    addedReveal = true;
                                    //we intersected one of these cells, so log us at this tile
                                    tile.NPCRevealersWithinRange.AddToConstructionList( revealer );
                                }
                            }
                            if ( addedCut && addedReveal )
                                break;
                        }
                    }
                }
            }

            //units of the player that are visible on the map (not in some vehicle) also grant visibility
            {
                DictionaryView<int, ISimMachineUnit> machineUnits = World.Forces.GetMachineUnitsByID();
                foreach ( KeyValuePair<int, ISimMachineUnit> kv in machineUnits )
                {
                    ISimMachineUnit unit = kv.Value;
                    bool grantsVisibility = true;
                    FogOfWarCutter cutter = FogOfWarCutter.CreateBlank();
                    NPCRevealer revealer = NPCRevealer.CreateBlank();
                    bool isOutAndAvailable = true;
                    if ( !unit.GetIsDeployed() || unit.IsFullDead || unit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                    {
                        unit.FogOfWarCutting = cutter;
                        unit.NPCRevealing = revealer;
                        grantsVisibility = false;
                        isOutAndAvailable = false;
                    }
                    else
                    {
                        if ( !unit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                            SimCommon.AllActorsForTargeting.AddToConstructionList( unit );
                    }

                    Vector3 loc = unit.GetDrawLocation();
                    float actualCutRange = unit.GetAttackRange();
                    float extendedCutRange = MathA.Min( SimCommon.MaxNPCAttackRange, unit.GetMovementRange() );
                    float revealRange = unit.GetNPCRevealRange();
                    if ( actualCutRange > extendedCutRange )
                        extendedCutRange = actualCutRange;
                    if ( revealRange < actualCutRange )
                        revealRange = actualCutRange;

                    float largestRange = MathA.Max( MathA.Max( actualCutRange, extendedCutRange, revealRange ), SimCommon.MaxNPCAttackRange );

                    if ( grantsVisibility )
                    {
                        cutter = FogOfWarCutter.Create( loc, actualCutRange, extendedCutRange, unit );
                        unit.FogOfWarCutting = cutter;
                        revealer = NPCRevealer.Create( loc, revealRange, actualCutRange, unit );
                        unit.NPCRevealing = revealer;
                    }

                    //the following code is repeated in the vehicle section, as it's inlined for performance reasons
                    float cut_minX = loc.x - extendedCutRange;
                    float cut_maxX = loc.x + extendedCutRange;
                    float cut_minZ = loc.z - extendedCutRange;
                    float cut_maxZ = loc.z + extendedCutRange;
                    float reveal_minX = loc.x - revealRange;
                    float reveal_maxX = loc.x + revealRange;
                    float reveal_minZ = loc.z - revealRange;
                    float reveal_maxZ = loc.z + revealRange;

                    float max_minX = loc.x - largestRange;
                    float max_maxX = loc.x + largestRange;
                    float max_minZ = loc.z - largestRange;
                    float max_maxZ = loc.z + largestRange;

                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        bool addedCut = false;
                        bool addedReveal = false;
                        bool addedMax = false;
                        for ( int i = 0; i < tile.CellsList.Count; i++ )
                        {
                            MapCell cell = tile.CellsList[i];
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                                rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                                continue;
                            if ( !addedMax )
                            {
                                addedMax = true;
                                if ( isOutAndAvailable )
                                    tile.ActorsWithinMaxNPCAttackRange.AddToConstructionList( unit );
                            }

                            if ( grantsVisibility )
                            {
                                if ( !addedCut )
                                {
                                    if ( rect.XMax <= cut_minX || rect.XMin >= cut_maxX ||
                                        rect.YMax <= cut_minZ || rect.YMin >= cut_maxZ )
                                    { }
                                    else
                                    {
                                        addedCut = true;
                                        //we intersected one of these cells, so log us at this tile
                                        tile.FogOfWarCuttersWithinRange.AddToConstructionList( cutter );
                                    }
                                }
                                if ( !addedReveal )
                                {
                                    if ( rect.XMax <= reveal_minX || rect.XMin >= reveal_maxX ||
                                        rect.YMax <= reveal_minZ || rect.YMin >= reveal_maxZ )
                                    { }
                                    else
                                    {
                                        addedReveal = true;
                                        //we intersected one of these cells, so log us at this tile
                                        tile.NPCRevealersWithinRange.AddToConstructionList( revealer );
                                    }
                                }
                            }
                            if ( addedCut && addedReveal )
                                break;
                        }
                    }
                }
            }

            //npc units never grant visibility, but do log as being within max action range of this cell or not, and also reveal
            {
                DictionaryView<int, ISimNPCUnit> npcUnits = World.Forces.GetAllNPCUnitsByID();
                foreach ( KeyValuePair<int, ISimNPCUnit> kv in npcUnits )
                {
                    ISimNPCUnit unit = kv.Value;
                    //unit.UnitsWithinMyRange.Clear(); do this later, so that it's less time with it done
                    if ( unit.IsFullDead )// key note: we don't care about GetIsCurrentlyInvisible, as it's mostly based on being in FogOfWar or not for NPC units.  Doing that here would make it never reveal anything.
                        continue;

                    if ( !unit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                        SimCommon.AllActorsForTargeting.AddToConstructionList( unit );

                    Vector3 loc = unit.GetDrawLocation();
                    float revealRange = unit.GetNPCRevealRange();

                    NPCRevealer revealer = NPCRevealer.CreateBlank();
                    bool doesRevealing = true;
                    if ( unit.GetDoesRevealingForPlayer() )
                        revealer = NPCRevealer.Create( loc, revealRange, unit.CalculatePOI() );
                    else
                        doesRevealing = false;
                    unit.NPCRevealing = revealer;

                    float largestRange = MathA.Max( revealRange, SimCommon.MaxNPCAttackRange );

                    //the following code is repeated in the vehicle section, as it's inlined for performance reasons
                    float minX = loc.x - largestRange;
                    float maxX = loc.x + largestRange;
                    float minZ = loc.z - largestRange;
                    float maxZ = loc.z + largestRange;

                    float reveal_minX = loc.x - revealRange;
                    float reveal_maxX = loc.x + revealRange;
                    float reveal_minZ = loc.z - revealRange;
                    float reveal_maxZ = loc.z + revealRange;

                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        bool addedReveal = false;
                        bool addedMax = false;
                        for ( int i = 0; i < tile.CellsList.Count; i++ )
                        {
                            MapCell cell = tile.CellsList[i];
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= minX || rect.XMin >= maxX ||
                                rect.YMax <= minZ || rect.YMin >= maxZ )
                                continue;
                            if ( !addedMax )
                            {
                                addedMax = true;
                                tile.ActorsWithinMaxNPCAttackRange.AddToConstructionList( unit );
                            }

                            if ( doesRevealing )
                            {
                                if ( !addedReveal )
                                {
                                    if ( rect.XMax <= reveal_minX || rect.XMin >= reveal_maxX ||
                                        rect.YMax <= reveal_minZ || rect.YMin >= reveal_maxZ )
                                    { }
                                    else
                                    {
                                        addedReveal = true;
                                        //we intersected one of these cells, so log us at this tile
                                        tile.NPCRevealersWithinRange.AddToConstructionList( revealer );
                                    }
                                }
                            }

                            if ( addedReveal )
                                break;
                        }
                    }
                }
            }

            //structures with scan capability of the player grant visibility
            {
                ConcurrentDictionary<int, MachineStructure> machineStructures = SimCommon.MachineStructuresByID;
                foreach ( KeyValuePair<int, MachineStructure> kv in machineStructures )
                {
                    MachineStructure structure = kv.Value;
                    if ( structure.IsUnderConstruction )
                        continue;

                    if ( !structure.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                        SimCommon.AllActorsForTargeting.AddToConstructionList( structure );

                    bool isDead = structure.IsFullDead;
                    int scanRange = isDead ? 0 : structure.GetActorDataCurrent( ActorRefs.ScanRange, true );

                    bool doesRevealing = scanRange > 0;

                    int networkRange = isDead ? 0 : structure.GetActorDataCurrent( ActorRefs.NetworkRange, true );
                    int jobEffectRange = isDead ? 0 : structure.GetActorDataCurrent( ActorRefs.JobEffectRange, true );

                    int revealRange = Mathf.Max( scanRange, networkRange, jobEffectRange );

                    Vector3 loc = structure.GetGroundCenterLocation();

                    FogOfWarCutter cutter = FogOfWarCutter.Create( loc, scanRange, scanRange, null );
                    structure.FogOfWarCutting = cutter;
                    NPCRevealer revealer = NPCRevealer.Create( loc, revealRange, revealRange, null );
                    structure.NPCRevealing = revealer;
                    //flag.ShouldRenderBase = true;

                    float largestRange = MathA.Max( revealRange, SimCommon.MaxNPCAttackRange );

                    //the following code is repeated in the units section, as it's inlined for performance reasons
                    float minX = loc.x - largestRange;
                    float maxX = loc.x + largestRange;
                    float minZ = loc.z - largestRange;
                    float maxZ = loc.z + largestRange;

                    float reveal_minX = loc.x - revealRange;
                    float reveal_maxX = loc.x + revealRange;
                    float reveal_minZ = loc.z - revealRange;
                    float reveal_maxZ = loc.z + revealRange;

                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        bool addedReveal = false;
                        bool addedMax = false;
                        for ( int i = 0; i < tile.CellsList.Count; i++ )
                        {
                            MapCell cell = tile.CellsList[i];
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= minX || rect.XMin >= maxX ||
                                rect.YMax <= minZ || rect.YMin >= maxZ )
                                continue;
                            if ( !addedMax )
                            {
                                addedMax = true;
                                tile.ActorsWithinMaxNPCAttackRange.AddToConstructionList( structure );
                            }

                            if ( doesRevealing )
                            {
                                if ( !addedReveal )
                                {
                                    if ( rect.XMax <= reveal_minX || rect.XMin >= reveal_maxX ||
                                        rect.YMax <= reveal_minZ || rect.YMin >= reveal_maxZ )
                                    { }
                                    else
                                    {
                                        addedReveal = true;
                                        //we intersected one of these cells, so log us at this tile
                                        tile.NPCRevealersWithinRange.AddToConstructionList( revealer );
                                        tile.FogOfWarCuttersWithinRange.AddToConstructionList( cutter );
                                    }
                                }
                            }

                            if ( addedReveal )
                                break;
                        }
                    }
                }
            }

            //territory control flags of the player grant visibility
            {
                float cutRange = MathRefs.TerritoryControlFogCutRange.FloatMin;
                List<MachineStructure> flags = SimCommon.TerritoryControlFlags.GetDisplayList();
                foreach ( MachineStructure flag in flags )
                {
                    Vector3 loc = flag.GetGroundCenterLocation();

                    FogOfWarCutter cutter = FogOfWarCutter.Create( loc, cutRange, cutRange, null );
                    flag.FogOfWarCutting = cutter;
                    NPCRevealer revealer = NPCRevealer.Create( loc, cutRange, cutRange, null );
                    flag.NPCRevealing = revealer;
                    //flag.ShouldRenderBase = true;

                    //the following code is repeated in the units section, as it's inlined for performance reasons
                    float minX = loc.x - cutRange;
                    float maxX = loc.x + cutRange;
                    float minZ = loc.z - cutRange;
                    float maxZ = loc.z + cutRange;

                    foreach ( MapTile tile in CityMap.Tiles )
                    {
                        for ( int i = 0; i < tile.CellsList.Count; i++ )
                        {
                            MapCell cell = tile.CellsList[i];
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= minX || rect.XMin >= maxX ||
                                rect.YMax <= minZ || rect.YMin >= maxZ )
                                continue;

                            tile.FogOfWarCuttersWithinRange.AddToConstructionList( cutter );
                            tile.NPCRevealersWithinRange.AddToConstructionList( revealer );
                            break;
                        }
                    }
                }
            }

            foreach ( MapTile tile in CityMap.Tiles )
            {
                tile.FogOfWarCuttersWithinRange.SwitchConstructionToDisplay();
                tile.NPCRevealersWithinRange.SwitchConstructionToDisplay();
                tile.ActorsWithinMaxNPCAttackRange.SwitchConstructionToDisplay();
            }

            SimCommon.AllActorsForTargeting.SwitchConstructionToDisplay();
            #endregion

            #region Next - Calculate All Cells And Their Contents
            foreach ( MapCell cell in CityMap.Cells )
            {
                List<FogOfWarCutter> cutters = cell.ParentTile.FogOfWarCuttersWithinRange.GetDisplayList();
                List<NPCRevealer> revealers = cell.ParentTile.NPCRevealersWithinRange.GetDisplayList();

                if ( SimCommon.IsFogOfWarDisabled )
                {
                    GrantNonFogOfWarEverythingToEntireCell( cell );
                    if ( cutters.Count > 0 )
                    {
                        //this is still needed in cheat mode to make sure that the lists of buildings on units are set correctly
                        foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                            CalculateEfficientIsOutOfFogOfWar_Building( item.SimBuilding, cutters, item.CenterPoint );
                    }

                    if ( revealers.Count > 0 )
                    {
                        //this is also still needed in cheat mode to make sure the list of NPCUnitsInVisibilityRangeOrNull is correct
                        foreach ( ISimNPCUnit npcUnit in cell.NPCUnitsInCell )
                            CalculateEfficientIsRevealed_NPCUnit( npcUnit, revealers, npcUnit.GetDrawLocation() );
                    }
                }
                else if ( cutters.Count == 0 )
                    BlankOutToFogOfWarEntireCell( cell, revealers );
                else
                    DoInteractionTestsForCell( cutters, revealers, cell );
            }
            #endregion
        }

        #region GrantNonFogOfWarEverythingToEntireCell
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static void GrantNonFogOfWarEverythingToEntireCell( MapCell cell )
        {
            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                item.IsItemInFogOfWar = false;
            foreach ( MapItem item in cell.AllRoads )
                item.IsItemInFogOfWar = false;
            foreach ( MapItem item in cell.OtherSkeletonItems )
                item.IsItemInFogOfWar = false;
            foreach ( MapItem item in cell.Fences )
                item.IsItemInFogOfWar = false;
            foreach ( MapItem item in cell.DecorationMajor )
                item.IsItemInFogOfWar = false;
            foreach ( MapItem item in cell.DecorationMinor )
                item.IsItemInFogOfWar = false;
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots )
                spot.IsSpotInFogOfWar = false;
            foreach ( ISimNPCUnit npcUnit in cell.NPCUnitsInCell )
                npcUnit.IsNPCInFogOfWar = false;
        }
        #endregion

        #region BlankOutToFogOfWarEntireCell
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static void BlankOutToFogOfWarEntireCell( MapCell cell, List<NPCRevealer> revealers )
        {
            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
            {
                if ( item.SimBuilding?.MachineStructureInBuilding != null )
                    item.IsItemInFogOfWar = false;
                else
                    item.IsItemInFogOfWar = true;
            }
            foreach ( MapItem item in cell.AllRoads )
                item.IsItemInFogOfWar = true;
            foreach ( MapItem item in cell.OtherSkeletonItems )
                item.IsItemInFogOfWar = true;
            foreach ( MapItem item in cell.Fences )
                item.IsItemInFogOfWar = true;
            foreach ( MapItem item in cell.DecorationMajor )
                item.IsItemInFogOfWar = true;
            foreach ( MapItem item in cell.DecorationMinor )
                item.IsItemInFogOfWar = true;
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots )
                spot.IsSpotInFogOfWar = true;
            if ( revealers.Count == 0 )
            {
                foreach ( ISimNPCUnit npcUnit in cell.NPCUnitsInCell )
                    npcUnit.IsNPCInFogOfWar = true;
            }
            else
            {
                foreach ( ISimNPCUnit npcUnit in cell.NPCUnitsInCell )
                    npcUnit.IsNPCInFogOfWar = !CalculateEfficientIsRevealed_NPCUnit( npcUnit, revealers, npcUnit.GetDrawLocation() );
            }
        }
        #endregion

        #region DoInteractionTestsForCell
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static void DoInteractionTestsForCell( List<FogOfWarCutter> cutters, List<NPCRevealer> revealers, MapCell cell )
        {
            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
            {
                if ( item.SimBuilding?.MachineStructureInBuilding != null )
                    item.IsItemInFogOfWar = false;
                else
                    item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Building( item.SimBuilding, cutters, item.CenterPoint );
            }
            foreach ( MapItem item in cell.AllRoads )
                item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, item.CenterPoint );
            foreach ( MapItem item in cell.OtherSkeletonItems )
                item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, item.CenterPoint );
            foreach ( MapItem item in cell.Fences )
                item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, item.CenterPoint );
            foreach ( MapItem item in cell.DecorationMajor )
                item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, item.CenterPoint );
            foreach ( MapItem item in cell.DecorationMinor )
                item.IsItemInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, item.CenterPoint );
            foreach ( MapOutdoorSpot spot in cell.AllOutdoorSpots )
                spot.IsSpotInFogOfWar = !CalculateEfficientIsOutOfFogOfWar_Basic( cutters, spot.Position );
            foreach ( ISimNPCUnit npcUnit in cell.NPCUnitsInCell )
                npcUnit.IsNPCInFogOfWar = !CalculateEfficientIsRevealed_NPCUnit( npcUnit, revealers, npcUnit.GetDrawLocation() );
        }
        #endregion

        #region CalculateEfficientIsOutOfFogOfWar_NonBuilding
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool CalculateEfficientIsOutOfFogOfWar_Basic( List<FogOfWarCutter> cutters, Vector3 Position )
        {
            if ( cutters == null || cutters.Count == 0 )
                return false;
            for ( int i = 0; i < cutters.Count; i++ )
            {
                FogOfWarCutter cutter = cutters[i];
                if ( cutter.GetIsPointInCutRange( Position ) )
                    return true;
            }
            return false;
        }
        #endregion

        #region CalculateEfficientIsOutOfFogOfWar_Building
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool CalculateEfficientIsOutOfFogOfWar_Building( ISimBuilding Building, List<FogOfWarCutter> cutters, Vector3 Position )
        {
            if ( cutters == null || cutters.Count == 0 )
                return false;
            bool result = false;
            for ( int i = 0; i < cutters.Count; i++ )
            {
                FogOfWarCutter cutter = cutters[i];
                if ( cutter.GetIsPointInExtendedRange( Position ) )
                {
                    if ( cutter.GetIsPointInCutRange( Position ) )
                    {
                        result = true;
                    }
                }
            }
            return result;
        }
        #endregion

        #region CalculateEfficientIsRevealed_NPCUnit
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool CalculateEfficientIsRevealed_NPCUnit( ISimNPCUnit NPCUnit, List<NPCRevealer> revealers, Vector3 Position )
        {
            if ( revealers == null || revealers.Count == 0 )
                return false;
            bool result = false;
            for ( int i = 0; i < revealers.Count; i++ )
            {
                NPCRevealer revealer = revealers[i];
                if ( revealer.GetIsPointInRevealRange( Position ) )
                {
                    if ( NPCUnit.HomePOI != null && !(NPCUnit.HomePOI?.IsPOIAlarmed_Any ?? false) )
                    {
                        //this is a dormant NPC in a POI
                        if ( NPCUnit.HomePOI != revealer.RevealerPOI )
                        {
                            //the revealer is not in the same POI as the npc
                            if ( revealer.GetIsPointInVisibilityRange( Position ) )
                            {
                                result = true; //since the npc is in visibility range, reveal it anyway
                            }
                            continue; //the normal logic below is skipped in these cases
                        }
                        else
                        {} //if the revealer IS in the same poi as the npc, then this should all be fine
                    }

                    result = true;
                }
            }
            return result;
        }
        #endregion

        private static void RecalculateCollidables()
        {
            #region First Calculate The Collidables On Each Tile
            foreach ( MapTile tile in CityMap.Tiles )
            {
                tile.CollidablesIntersectingTile.ClearConstructionListForStartingConstruction();
                tile.CollidablesThatHideDecorations.ClearConstructionListForStartingConstruction();
            }
            foreach ( MapCell cell in CityMap.Cells )
            {
                cell.CollidablesIntersectingCell.ClearConstructionListForStartingConstruction();
            }

            foreach ( KeyValuePair<int, ISimMapActor> kv in SimCommon.AllActorsByID )
            {
                if ( !kv.Value.GetIsValidForCollisions() ) //|| kv.Value.IsCurrentlyDead || kv.Value.GetIsCurrentlyInvisible() )
                    continue; //but not if they are in a vehicle //, or are dead, or are invisible
                AddCollidableToRelatedTiles( kv.Value );
            }

            foreach ( MapTile tile in CityMap.Tiles )
            {
                tile.CollidablesIntersectingTile.SwitchConstructionToDisplay();
                tile.CollidablesThatHideDecorations.SwitchConstructionToDisplay();
            }
            foreach ( MapCell cell in CityMap.Cells )
            {
                cell.CollidablesIntersectingCell.SwitchConstructionToDisplay();
            }
            #endregion

            #region Then calculate If Anything Is Hidden By These 
            foreach ( MapCell cell in CityMap.Cells )
            {
                List<ICollidable> collidablesThatHide = cell.ParentTile.CollidablesThatHideDecorations.GetDisplayList();

                if ( collidablesThatHide.Count == 0 )
                    MarkEntireCellAsNotHavingAnyHidden( cell );
                else
                    DoHiddenTestsForCell( collidablesThatHide, cell );
            }
            #endregion
        }

        #region AddCollidableToRelatedTiles
        private static void AddCollidableToRelatedTiles( ICollidable collidable )
        {
            if ( collidable == null )
                return;

            float range = collidable.GetRadiusForCollisions();
            Vector3 loc = collidable.GetPositionForCollisions();

            float minX = loc.x - range;
            float maxX = loc.x + range;
            float minZ = loc.z - range;
            float maxZ = loc.z + range;

            foreach ( MapTile tile in CityMap.Tiles )
            {
                bool hasDoneTileAdds = false;
                for ( int i = 0; i < tile.CellsList.Count; i++ )
                {
                    MapCell cell = tile.CellsList[i];
                    ArcenFloatRectangle rect = cell.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue;

                    cell.CollidablesIntersectingCell.AddToConstructionList( collidable );

                    if ( !hasDoneTileAdds )
                    {
                        hasDoneTileAdds = true;
                        //we intersected one of these cells, so log us at this tile
                        tile.CollidablesIntersectingTile.AddToConstructionList( collidable );
                        //only add it to this second list if it's the type that does that
                        if ( collidable.GetShouldHideIntersectingDecorations() )
                            tile.CollidablesThatHideDecorations.AddToConstructionList( collidable );
                    }
                }
            }
        }
        #endregion

        #region MarkEntireCellAsNotHavingAnyHidden
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static void MarkEntireCellAsNotHavingAnyHidden( MapCell cell )
        {
            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.AllRoads )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.OtherSkeletonItems )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.Fences )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.DecorationMajor )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.DecorationMinor )
                item.IsItemHidden = false;
        }
        #endregion

        #region DoHiddenTestsForCell
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private static void DoHiddenTestsForCell( List<ICollidable> collidablesThatHide, MapCell cell )
        {
            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.AllRoads )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.Fences )
                item.IsItemHidden = false;
            foreach ( MapItem item in cell.OtherSkeletonItems )
            {
                if ( item.Type.ExtraPlaceableData.SkipsCollidableHiding )
                    item.IsItemHidden = false;
                else
                    item.IsItemHidden = CalculateEfficientIsInHiddenRange( collidablesThatHide, item.CenterPoint );
            }
            foreach ( MapItem item in cell.DecorationMajor )
            {
                if ( item.Type.ExtraPlaceableData.SkipsCollidableHiding )
                    item.IsItemHidden = false;
                else
                    item.IsItemHidden = CalculateEfficientIsInHiddenRange( collidablesThatHide, item.CenterPoint );
            }
            foreach ( MapItem item in cell.DecorationMinor )
            {
                if ( item.Type.ExtraPlaceableData.SkipsCollidableHiding )
                    item.IsItemHidden = false;
                else
                    item.IsItemHidden = CalculateEfficientIsInHiddenRange( collidablesThatHide, item.CenterPoint );
            }
        }
        #endregion

        #region CalculateEfficientIsInHiddenRange
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool CalculateEfficientIsInHiddenRange( List<ICollidable> collidablesThatHide, Vector3 Position )
        {
            if ( collidablesThatHide == null || collidablesThatHide.Count == 0 )
                return false;
            for ( int i = 0; i < collidablesThatHide.Count; i++ )
            {
                ICollidable collidable = collidablesThatHide[i];
                float squaredRadius = collidable.GetSquaredRadiusForCollisions();
                Vector3 loc = collidable.GetPositionForCollisions();

                if ( ( loc - Position ).GetSquareGroundMagnitude() <= squaredRadius )
                    return true;
            }
            return false;
        }
        #endregion
    }
}
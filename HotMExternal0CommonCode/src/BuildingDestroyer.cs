using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.Universal.Deserialization;
using UnityEngine;

namespace Arcen.HotM.External
{
    public static class BuildingDestroyer
    {
        private static int IsCalculatingNow = 0;

        public static void OnGameClear()
        {
            IsCalculatingNow = 0;
        }

        #region DoPerFrame
        public static void DoPerFrame()
        {
            SimCommon.IsBuildingDestructionCalculatingNow = IsCalculatingNow > 0;

            if ( IsCalculatingNow > 0 || SimCommon.QueuedBuildingDestruction.Count == 0 )
                return; //already busy, or nothing to do

            if ( SimCommon.QueuedBuildingDestruction.TryDequeue( out QueuedBuildingDestructionData destruction ) )
            {
                if ( !DestroyBuildingsInRadius( destruction.Epicenter, destruction.Range, destruction.StatusToApply, destruction.AlsoDestroyOtherItems, 
                    destruction.AlsoDestroyUnits, destruction.SkipUnitsWithArmorPlating, destruction.IrradiateCells, destruction.UnitsToSpawnAfter,
                    destruction.StatisticForDeaths, destruction.IsCausedByPlayer, destruction.IsFromJob, destruction.ExtraCode ) )
                {
                    //if it failed to start, then put this back in
                    SimCommon.QueuedBuildingDestruction.Enqueue( destruction );
                }
            }
        }
        #endregion

        #region DestroyBuildingsInRadius
        private static bool DestroyBuildingsInRadius( Vector3 Epicenter, float Range, BuildingStatus StatusToApply, bool AlsoDestroyOtherItems, 
            bool AlsoDestroyUnits, bool SkipUnitsWithArmorPlating, bool IrradiateCells, NPCManager SpawnAfter, CityStatistic StatisticForDeaths, bool IsCausedByPlayer,
            MachineJob IsFromJob, ExtraCodeHandler ExtraCode )
        {
            if ( Interlocked.Exchange( ref IsCalculatingNow, 1 ) != 0 )
                return false;
            SimCommon.IsBuildingDestructionCalculatingNow = true;

            //multiple of these can be run at a time, no problem
            if ( !ArcenThreading.RunTaskOnBackgroundThread( "_Inter.DestroyBuildingsInRadius",
                ( TaskStartData startData ) =>
                {
                    int debugStage = 0;
                    try
                    {
                        float max_minX = Epicenter.x - Range;
                        float max_maxX = Epicenter.x + Range;
                        float max_minZ = Epicenter.z - Range;
                        float max_maxZ = Epicenter.z + Range;

                        float rangeSquared = Range * Range;

                        //do all the buildings first, since they're the most noticeable
                        foreach ( MapCell cell in CityMap.Cells )
                        {
                            ArcenFloatRectangle rect = cell.CellRect;
                            if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                                rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                                continue; //the cell is out of range of us

                            if ( IrradiateCells )
                                cell.IsCellConsideredIrradiated = true;

                            foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                            {
                                if ( item.SimBuilding == null || item.SimBuilding.GetStatus() == StatusToApply )
                                    continue; //if already the status, don't apply it

                                float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                if ( dist > rangeSquared )
                                    continue; //out of the blast range, skip!

                                if ( ExtraCode != null )
                                    ExtraCode.Implementation.HandleExplosionImminentBuilding( ExtraCode, item.SimBuilding, StatusToApply, IrradiateCells, IsCausedByPlayer, IsFromJob );

                                //if there's a structure in this building, make sure it gets destroyed also
                                if ( item.SimBuilding.MachineStructureInBuilding != null )
                                    item.SimBuilding.MachineStructureInBuilding.ScrapStructureNow( ScrapReason.CaughtInExplosion, Engine_Universal.PermanentQualityRandom );

                                //in the blast range, so hit it
                                int kills = item.SimBuilding.KillEveryoneHere();
                                item.SimBuilding.SetStatus( StatusToApply );
                                item.SimBuilding.GetMapItem().DropBurningEffect_Slow();

                                if ( kills > 0 )
                                    StatisticForDeaths?.AlterScore_CityAndMeta( kills );

                                //if ( item.Type.SecondarysRenderersOfThisRoot.Count > 0 )
                                //{
                                //    foreach ( SecondaryRenderer secondary in item.Type.SecondarysRenderersOfThisRoot )
                                //    {
                                //        MapMaterializingItem materializingItem = MapMaterializingItem.GetFromPoolOrCreate();

                                //        materializingItem.Position = item.Position;
                                //        materializingItem.Rotation = item.Rotation;
                                //        materializingItem.Scale = item.Scale;

                                //        materializingItem.RendererGroup = secondary.Rend.ParentGroup;
                                //        materializingItem.Materializing = MaterializeType.BurnDownLargeSlow;

                                //        MapEffectCoordinator.AddMaterializingItem( materializingItem );
                                //    }
                                //}
                            }
                        }

                        //do npc units and player units next
                        if ( AlsoDestroyUnits )
                        {
                            foreach ( KeyValuePair<int, ISimNPCUnit> kv in World.Forces.GetAllNPCUnitsByID() )
                            {
                                ISimNPCUnit unit = kv.Value;
                                float dist = (unit.GetDrawLocation() - Epicenter).GetSquareGroundMagnitude();
                                if ( dist > rangeSquared )
                                    continue; //out of the blast range, skip!

                                if ( SkipUnitsWithArmorPlating )
                                {
                                    if ( unit.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true ) > 0 )
                                        continue; //if any powered armor, and skipping units with powered armor, skip this unit!
                                }

                                if ( ExtraCode != null )
                                    ExtraCode.Implementation.HandleExplosionImminentActor( ExtraCode, unit, IrradiateCells, IsCausedByPlayer, IsFromJob );

                                unit.DisbandNPCUnit( NPCDisbandReason.CaughtInExplosion ); //KILL!
                            }

                            foreach ( KeyValuePair<int, ISimMachineUnit> kv in World.Forces.GetMachineUnitsByID() )
                            {
                                ISimMachineUnit unit = kv.Value; //we don't care if they are invisible or in a vehicle or what
                                float dist = (unit.GetDrawLocation() - Epicenter).GetSquareGroundMagnitude();
                                if ( dist > rangeSquared )
                                    continue; //out of the blast range, skip!

                                if ( SkipUnitsWithArmorPlating )
                                {
                                    if ( unit.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true ) > 0 )
                                        continue; //if any powered armor, and skipping units with powered armor, skip this unit!
                                }

                                if ( ExtraCode != null )
                                    ExtraCode.Implementation.HandleExplosionImminentActor( ExtraCode, unit, IrradiateCells, IsCausedByPlayer, IsFromJob );

                                unit.TryScrapRightNowWithoutWarning_Danger( ScrapReason.CaughtInExplosion ); //KILL!
                            }

                            foreach ( KeyValuePair<int, ISimMachineVehicle> kv in World.Forces.GetMachineVehiclesByID() )
                            {
                                ISimMachineVehicle vehicle = kv.Value;
                                float dist = (vehicle.GetDrawLocation() - Epicenter).GetSquareGroundMagnitude();
                                if ( dist > rangeSquared )
                                    continue; //out of the blast range, skip!

                                if ( SkipUnitsWithArmorPlating )
                                {
                                    if ( vehicle.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true ) > 0 )
                                        continue; //if any powered armor, and skipping units with powered armor, skip this unit!
                                }

                                if ( ExtraCode != null )
                                    ExtraCode.Implementation.HandleExplosionImminentActor( ExtraCode, vehicle, IrradiateCells, IsCausedByPlayer, IsFromJob );

                                vehicle.TryScrapRightNowWithoutWarning_Danger( ScrapReason.CaughtInExplosion ); //KILL!
                            }
                        }

                        foreach ( ISimCityVehicle vehicle in World.CityVehicles.GetAllCityVehicles() )
                        {
                            float dist = (vehicle.GetVisWorldLocation() - Epicenter).GetSquareGroundMagnitude();
                            if ( dist > rangeSquared )
                                continue; //out of the blast range, skip!

                            vehicle.DisbandAndBurn(); //KILL!
                        }

                        //do the other items next, if they need it
                        if ( AlsoDestroyOtherItems )
                        {
                            foreach ( MapCell cell in CityMap.Cells )
                            {
                                ArcenFloatRectangle rect = cell.CellRect;
                                if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                                    rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                                    continue; //the cell is out of range of us

                                foreach ( MapItem item in cell.DecorationMajor )
                                {
                                    if ( item.IsNonBuildingItemBurned )
                                        continue; //if already burned, don't burn it again

                                    float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                    if ( dist > rangeSquared )
                                        continue; //out of the blast range, skip!

                                    //in the blast range, so hit it
                                    item.IsNonBuildingItemBurned = true;
                                }

                                foreach ( MapItem item in cell.DecorationMinor )
                                {
                                    if ( item.IsNonBuildingItemBurned )
                                        continue; //if already burned, don't burn it again

                                    float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                    if ( dist > rangeSquared )
                                        continue; //out of the blast range, skip!

                                    //in the blast range, so hit it
                                    item.IsNonBuildingItemBurned = true;
                                }

                                foreach ( MapItem item in cell.Fences )
                                {
                                    if ( item.IsNonBuildingItemBurned )
                                        continue; //if already burned, don't burn it again

                                    float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                    if ( dist > rangeSquared )
                                        continue; //out of the blast range, skip!

                                    //in the blast range, so hit it
                                    item.IsNonBuildingItemBurned = true;
                                }

                                foreach ( MapItem item in cell.OtherSkeletonItems )
                                {
                                    if ( item.IsNonBuildingItemBurned )
                                        continue; //if already burned, don't burn it again

                                    float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                    if ( dist > rangeSquared )
                                        continue; //out of the blast range, skip!

                                    //in the blast range, so hit it
                                    item.IsNonBuildingItemBurned = true;
                                }

                                foreach ( MapItem item in cell.AllRoads )
                                {
                                    if ( item.IsNonBuildingItemBurned )
                                        continue; //if already burned, don't burn it again

                                    float dist = (item.CenterPoint - Epicenter).GetSquareGroundMagnitude();
                                    if ( dist > rangeSquared )
                                        continue; //out of the blast range, skip!

                                    //in the blast range, so hit it
                                    item.IsNonBuildingItemBurned = true;
                                }
                            }
                        }

                        if ( SpawnAfter != null )
                        {
                            //now spawn some units here if needed
                            SpawnAfter.HandleManualInvocationAtPoint( Epicenter, Engine_Universal.PermanentQualityRandom, false );
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogDebugStageWithStack( "BuildingDestroyer.DestroyBuildingsInRadius background thread", debugStage, e, Verbosity.ShowAsError );
                    }

                    //finishing up on the background thread
                    Interlocked.Exchange( ref IsCalculatingNow, 0 );

                } ) ) //end the outer if
            {
                Interlocked.Exchange( ref IsCalculatingNow, 0 );
                return false;
            }
            else
                return true; //we did successfully start the destruction
        }
        #endregion DestroyBuildingsInRadius
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.FFU.RoSAI {
    public class BasicConsumables : IResourceConsumableImplementation {
        public bool HandleConsumableHardTargeting(ISimMachineActor Actor, ResourceConsumable Consumable, Vector3 center, float attackRange, float moveRange) {
            if (Consumable == null || Actor == null) return false;
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
            HandleDataRefsPreload();
            int debugStage = 0;

            try {
                debugStage = 100;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY(groundLevel);

                switch (Consumable.ID) {
                    case "DebugTooltip": {
                        debugStage = 1200;
                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            if (buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey(Consumable.DirectUseConsumable.TargetsBuildingTag.ID)) buildingUnderCursor = null;
                        }

                        debugStage = 2200;
                        if (buildingUnderCursor != null) destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                            destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity) {
                        } else return false;

                        debugStage = 3200;
                        BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();
                        BuildingPrefab buildingPrefab = buildingUnderCursor?.GetPrefab();
                        MachineStructure buildingStructure = buildingUnderCursor?.MachineStructureInBuilding;
                        ThreadsafeTableDictionaryView<ActorDataType, MapActorData>? buildingData = buildingStructure?.ActorData;
                        MachineJob buildingJob = buildingStructure?.CurrentJob;
                        bool buildingIsInvalid = buildingUnderCursor == null || variant == null || buildingPrefab == null;

                        debugStage = 4200;
                        if (!buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice(destinationPoint)) {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpot(true, IconRefs.MouseMoveMode_Valid, destinationPoint, 0.2f);

                            debugStage = 4300;
                            if (novel.TryStartSmallerTooltip(TooltipID.Create(Consumable), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = Consumable.Icon;
                                novel.TitleUpperLeft.AddLang("DebugTooltipInfo");

                                debugStage = 4400;
                                var costInfo = novel.Main.AddRaw(variant.GetDisplayName()).StartLineHeight50().Line();
                                if (buildingUnderCursor.GetTotalResidentCount() > 0) costInfo.AddFormat1("InfoResidents", 
                                    buildingUnderCursor.GetTotalResidentCount().ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingUnderCursor.GetTotalWorkerCount() > 0) costInfo.AddFormat1("InfoWorkers",
                                    buildingUnderCursor.GetTotalWorkerCount().ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingPrefab.NormalTotalBuildingVolumeFullDimensions > 0) costInfo.AddFormat1("InfoVolume",
                                    buildingPrefab.NormalTotalBuildingVolumeFullDimensions.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingPrefab.NormalTotalStorageVolumeFullDimensions > 0) costInfo.AddFormat1("InfoStorage",
                                    buildingPrefab.NormalTotalStorageVolumeFullDimensions.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingPrefab.NormalTotalBuildingFloorAreaFullDimensions > 0) costInfo.AddFormat1("InfoArea",
                                    buildingPrefab.NormalTotalBuildingFloorAreaFullDimensions.ToStringThousandsWhole()).StartLineHeight50().Line();

                                debugStage = 4500;
                                if (buildingStructure != null) {
                                    if (buildingData != null && buildingData.Value.HasAnyItems) {
                                        using (var dataEnum = buildingData.Value.GetEnumerator()) {
                                            var dataCurrent = dataEnum.Current;
                                            while (true) {
                                                bool hasNext = dataEnum.MoveNext();
                                                bool isLast = !hasNext;
                                                try {
                                                    string dataValCurr = dataCurrent.Value.Current.ToStringThousandsWhole();
                                                    string dataValMax = dataCurrent.Value.Maximum.ToStringThousandsWhole();
                                                    if (!string.IsNullOrEmpty(dataCurrent.Key.GetDisplayName())) {
                                                        if (isLast) costInfo.AddRaw($"{dataCurrent.Key.GetDisplayName()}: {dataValCurr} / {dataValMax}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{dataCurrent.Key.GetDisplayName()}: {dataValCurr} / {dataValMax}", "FFFFFF").StartLineHeight10().Line();
                                                    } else {
                                                        if (isLast) costInfo.AddRaw($"{dataCurrent.Key.ID}: {dataValCurr} / {dataValMax}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{dataCurrent.Key.ID}: {dataValCurr} / {dataValMax}", "FFFFFF").StartLineHeight10().Line();
                                                    }
                                                } catch { }
                                                if (!hasNext) break;
                                                dataCurrent = dataEnum.Current;
                                            }
                                        }
                                    }
                                }

                                debugStage = 4600;
                                if (buildingJob != null) {
                                    JobResourceCapIncrease[] jobCaps = new JobResourceCapIncrease[] {
                                        buildingJob.CapIncrease1,
                                        buildingJob.CapIncrease2,
                                        buildingJob.CapIncrease3,
                                        buildingJob.CapIncrease4,
                                        buildingJob.CapIncrease5
                                    };
                                    bool hasDataAfter = buildingJob.MathInts.Count > 0 || buildingJob.MathFloats.Count > 0;
                                    for (int i = 0; i < jobCaps.Length; i++) {
                                        try {
                                            if (jobCaps[i] != null && jobCaps[i].Resource != null) {
                                                bool isLast = (i == (jobCaps.Length - 1) || jobCaps[i + 1].Resource == null) && !hasDataAfter;
                                                long capValue = GetCapIncreaseValue(jobCaps[i], buildingPrefab);
                                                string capTextProcessed = capValue > 0 ? 
                                                    $"+{capValue.ToStringThousandsWhole()}" : 
                                                    $"{capValue.ToStringThousandsWhole()}";
                                                if (!string.IsNullOrEmpty(jobCaps[i].Resource.GetDisplayName())) {
                                                    if (isLast) costInfo.AddRaw($"{jobCaps[i].Resource.GetDisplayName()} Capacity: {capTextProcessed}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{jobCaps[i].Resource.GetDisplayName()} Capacity: {capTextProcessed}", "FFFFFF").StartLineHeight10().Line();
                                                } else {
                                                    if (isLast) costInfo.AddRaw($"{jobCaps[i].Resource.ID} Capacity: {capTextProcessed}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{jobCaps[i].Resource.ID} Capacity: {capTextProcessed}", "FFFFFF").StartLineHeight10().Line();
                                                }
                                            }
                                        } catch { }
                                    }
                                }

                                debugStage = 4700;
                                if (buildingStructure != null && buildingJob != null && 
                                    buildingJob.MathInts != null && buildingJob.MathInts.HasAnyItems) {
                                    bool hasDataAfter = buildingJob.MathFloats.Count > 0;
                                    using (var mIntEnum = buildingJob.MathInts.GetEnumerator()) {
                                        var mIntCurrent = mIntEnum.Current;
                                        while (true) {
                                            bool hasNext = mIntEnum.MoveNext();
                                            bool isLast = !hasNext && !hasDataAfter;
                                            try {
                                                string opType = GetMathTypeText(mIntCurrent.Value.MathType);
                                                string opValue = $"{mIntCurrent.Value.GetMin(buildingStructure)}";
                                                if (mIntCurrent.Value.IncomeOrExpenseResource != null) {
                                                    var refResource = mIntCurrent.Value.IncomeOrExpenseResource;
                                                    if (!string.IsNullOrEmpty(refResource.GetDisplayName())) {
                                                        if (isLast) costInfo.AddRaw($"{refResource.GetDisplayName()} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{refResource.GetDisplayName()} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                    } else {
                                                        if (isLast) costInfo.AddRaw($"{refResource.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{refResource.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                    }
                                                } else if (!string.IsNullOrEmpty(mIntCurrent.Value.DisplayName)) {
                                                    if (isLast) costInfo.AddRaw($"{mIntCurrent.Value.DisplayName} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{mIntCurrent.Value.DisplayName} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                } else {
                                                    if (isLast) costInfo.AddRaw($"{mIntCurrent.Value.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{mIntCurrent.Value.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                }
                                            } catch { }
                                            if (!hasNext) break;
                                            mIntCurrent = mIntEnum.Current;
                                        }
                                    }
                                }

                                debugStage = 4800;
                                if (buildingStructure != null && buildingJob != null && 
                                    buildingJob.MathFloats != null && buildingJob.MathFloats.HasAnyItems) {
                                    using (var mFloatEnum = buildingJob.MathFloats.GetEnumerator()) {
                                        var mFloatCurrent = mFloatEnum.Current;
                                        while (true) {
                                            bool hasNext = mFloatEnum.MoveNext();
                                            bool isLast = !hasNext;
                                            try {
                                                string opType = GetMathTypeText(mFloatCurrent.Value.MathType);
                                                string opValue = $"{mFloatCurrent.Value.GetMin(buildingStructure)}";
                                                if (mFloatCurrent.Value.IncomeOrExpenseResource != null) {
                                                    var refResource = mFloatCurrent.Value.IncomeOrExpenseResource;
                                                    if (!string.IsNullOrEmpty(refResource.GetDisplayName())) {
                                                        if (isLast) costInfo.AddRaw($"{refResource.GetDisplayName()} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{refResource.GetDisplayName()} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                    } else {
                                                        if (isLast) costInfo.AddRaw($"{refResource.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                        else costInfo.AddRaw($"{refResource.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                    }
                                                } else if (!string.IsNullOrEmpty(mFloatCurrent.Value.DisplayName)) {
                                                    if (isLast) costInfo.AddRaw($"{mFloatCurrent.Value.DisplayName} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{mFloatCurrent.Value.DisplayName} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                } else {
                                                    if (isLast) costInfo.AddRaw($"{mFloatCurrent.Value.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight50().Line();
                                                    else costInfo.AddRaw($"{mFloatCurrent.Value.ID} {opType}: {opValue}", "FFFFFF").StartLineHeight10().Line();
                                                }
                                            } catch { }
                                            if (!hasNext) break;
                                            mFloatCurrent = mFloatEnum.Current;
                                        }
                                    }
                                }

                                debugStage = 4900;
                                costInfo.AddRaw("Unique ID: " + buildingUnderCursor.GetBuildingID(), "FFFFFF");
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.InfoVis.ColorHDR,
                                new Vector3(1.05f, 1.05f, 1.05f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            return false;
                        }
                        return false;
                    }
                    case "EvictionDrone": {
                        debugStage = 1200;
                        if (attackRange < moveRange) attackRange = moveRange;
                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        DrawHelper.RenderRangeCircle(groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR);

                        debugStage = 2200;
                        TargetingHelper.DoForAllBuildingsWithinRangeTight(Actor, attackRange, Consumable.DirectUseConsumable.TargetsBuildingTag, delegate (ISimBuilding Building) {
                            MapItem item = Building.GetMapItem();
                            if (item == null) return false;

                            if (item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped) return false;
                            item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                            MapCell cell = item.ParentCell;
                            MapTile tile = item.ParentTile;
                            if (!cell.IsConsideredInCameraView) return false;
                            if (Building.MachineStructureInBuilding != null) return false;
                            if (tile.TileNetworkLevel.Display < TileNetLevel.Full) return false;

                            ModHelpers.DrawMapItemHighlightedBorder(item, DataRefs.EvictionVis.ColorHDR,
                                new Vector3(1.05f, 1.05f, 1.05f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            return false;
                        });

                        debugStage = 3200;
                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            if (buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey(Consumable.DirectUseConsumable.TargetsBuildingTag.ID)) buildingUnderCursor = null;
                        }

                        debugStage = 4200;
                        if (buildingUnderCursor != null) destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                            destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity) {
                        } else return false;

                        debugStage = 5200;
                        bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;
                        bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null ||
                            (buildingUnderCursor?.GetMapItem()?.ParentTile?.TileNetworkLevel?.Display ?? TileNetLevel.None) < TileNetLevel.Full;
                        BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();
                        int peopleTotal = 0;
                        int peopleResidents = 0;
                        int peopleWorkers = 0;
                        int buildingVolume = 0;
                        int buildingStorage = 0;
                        int buildingFloorArea = 0;
                        int totalEvictionCost = 0;
                        if (!buildingIsInvalid) {
                            peopleResidents = buildingUnderCursor.GetTotalResidentCount();
                            peopleWorkers = buildingUnderCursor.GetTotalWorkerCount();
                            peopleTotal = peopleResidents + peopleWorkers;
                            BuildingPrefab buildingPrefab = buildingUnderCursor.GetPrefab();
                            buildingVolume = buildingPrefab.NormalTotalBuildingVolumeFullDimensions;
                            buildingStorage = buildingPrefab.NormalTotalStorageVolumeFullDimensions;
                            buildingFloorArea = buildingPrefab.NormalTotalBuildingFloorAreaFullDimensions;
                            totalEvictionCost = (int)(25 +
                            buildingVolume * DataRefs.EVICT_VOLUME_SILICA_MULT +
                            buildingStorage * DataRefs.EVICT_STORAGE_SILICA_MULT +
                            buildingFloorArea * DataRefs.EVICT_AREA_SILICA_MULT);
                        }

                        debugStage = 6200;
                        if (!isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice(destinationPoint)) {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpot(true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(Consumable), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.TitleUpperLeft.AddLang("Move_OutOfRange");
                                var costInfo = novel.Main.AddRaw(variant.GetDisplayName());
                                if (peopleTotal > 0) costInfo.AddFormat2("EvictionSilicaPeople", totalEvictionCost.ToStringThousandsWhole(), peopleTotal.ToStringThousandsWhole());
                                else costInfo.AddFormat1("EvictionSilicaOnly", totalEvictionCost.ToStringThousandsWhole());
                                if (ResourceRefs.Wealth.Current < totalEvictionCost) costInfo.AddLang("EvictionSilicaMissing");
                            }
                            return false;
                        }

                        debugStage = 7200;
                        if (!isInRange || buildingIsInvalid) return false;
                        else {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpotWithColor(true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(Consumable), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = Consumable.Icon;
                                novel.TitleUpperLeft.AddFormat1("Move_ClickToEvict", Lang.GetRightClickText());
                                var costInfo = novel.Main.AddRaw(variant.GetDisplayName());
                                if (peopleTotal > 0) costInfo.AddFormat2("EvictionSilicaPeople", totalEvictionCost.ToStringThousandsWhole(), peopleTotal.ToStringThousandsWhole());
                                else costInfo.AddFormat1("EvictionSilicaOnly", totalEvictionCost.ToStringThousandsWhole());
                                if (ResourceRefs.Wealth.Current < totalEvictionCost) costInfo.AddLang("EvictionSilicaMissing");
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.EvictionVis.ColorHDR,
                                new Vector3(1.08f, 1.08f, 1.08f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            if (ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume()) {
                                DataRefs.LiquidMetal.AlterCurrent_Named(-totalEvictionCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange);

                                Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;
                                Consumable.TryToDirectlyUseByActorAgainstTargetBuilding(Actor, buildingUnderCursor, delegate {
                                    int debugStageInner = 0;
                                    try {
                                        debugStageInner = 100;
                                        buildingUnderCursor.KillEveryoneHere();

                                        debugStageInner = 200;
                                        ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation(buildingUnderCursor.GetMapItem().OBBCache.BottomCenter,
                                            new Vector3(0, Engine_Universal.PermanentQualityRandom.Next(0, 360), 0));
                                        buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                        buildingUnderCursor.FullyDeleteBuilding();

                                        debugStageInner = 300;
                                        if (peopleTotal > 0) {
                                            CityStatisticTable.AlterScore("CitizensForciblyRescuedFromSlums", peopleTotal);
                                            ResourceRefs.ShelteredHumans.AlterCurrent_Named(peopleTotal, string.Empty, ResourceAddRule.StoreExcess);
                                            ArcenNotes.SendSimpleNoteToGameOnly(300, NoteInstructionTable.Instance.GetRowByID("GainedResource"),
                                                NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleTotal, 0, 0, 0);
                                        }
                                    } catch (Exception e) {
                                        ArcenDebugging.LogDebugStageWithStack("EvictionDroneHit", debugStageInner, e, Verbosity.ShowAsError);
                                    }
                                });
                            }
                            return true;
                        }
                    }
                    default: {
                        return MachineMovePlannerImplementation.HandleNonBuildingConsumableTargetingModeForAnyMachineActor(Actor);
                    }
                }
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicConsumables.HandleConsumableHardTargeting", debugStage, Consumable?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
                return false;
            }
        }

        public long GetCapIncreaseValue(JobResourceCapIncrease refCapInc, BuildingPrefab refPref) {
            if (refCapInc.ResourceCapIncreasedPerUnitOfArea > 0)
                return Mathf.RoundToInt(refPref.NormalTotalBuildingFloorAreaFullDimensions * refCapInc.ResourceCapIncreasedPerUnitOfArea);
            else if (refCapInc.ResourceCapIncreasedPerUnitOfVolume > 0) 
                return Mathf.RoundToInt(refPref.NormalTotalBuildingVolumeFullDimensions * refCapInc.ResourceCapIncreasedPerUnitOfVolume);
            else if (refCapInc.ResourceCapIncreasedFlat > 0)
                return refCapInc.ResourceCapIncreasedFlat;
            return 0;
        }

        public string GetMathTypeText(JobMathType mType) {
            switch (mType) {
                case JobMathType.InputCost: return "Input";
                case JobMathType.OutputResult: return "Output";
                case JobMathType.SomethingElse: return "Effect";
                default: return "Unknown";
            }
        }

        public void HandleDataRefsPreload() {
            if (DataRefs.EvictionVis == null) DataRefs.EvictionVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
            if (DataRefs.LiquidMetal == null) DataRefs.LiquidMetal = ResourceTypeTable.Instance.GetRowByID("GadoliniumMesosilicate");
            if (DataRefs.InfoVis == null) DataRefs.InfoVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidInfoTarget");
        }
    }
}

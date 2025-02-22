using Arcen.HotM.FFU.RoSAI;
using Arcen.Universal;
using HarmonyLib;
using System;
using System.Linq;

namespace Arcen.HotM.Core {
    public class BuildingPrefabTableOverride : ArcenDynamicTable<BuildingPrefab> {
        public static readonly BuildingPrefabTableOverride Instance = new BuildingPrefabTableOverride();
        public const double DIMENSION_BASE = 18.0;
        public const double DIMENSION_SQUARED = 324.0;
        public const double DIMENSION_CUBED = 5832.0;

        private BuildingPrefabTableOverride() : base("Override_BuildingPrefab", 
            ArcenDynamicTableType.XMLDirectory, ReloadDuringRuntime.Allow, 
            TableLocalization.Skip) {
        }

        public override BuildingPrefab GetNewRowFromPool() {
            return BuildingPrefab.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater() {
        }

        public override void DoPreInitializationLogic() {
        }

        public override void DoPostFinalLoadCrossTableWork_OnLoadingThread() {
            DoPostFinalLoadCrossTableWork_OnLoadingThread(false);
        }

        public void DoPostFinalLoadCrossTableWork_OnLoadingThread(bool IsFromSelectReload) {
            foreach (BuildingPrefab modBuildingPrefab in Rows) {
                int debugStage = 0;
                try {
                    debugStage = 100;
                    BuildingPrefab refBuildingPrefab = BuildingPrefabTable.Instance.Rows.FirstOrDefault(x => x.ID == modBuildingPrefab.ID);
                    if (refBuildingPrefab != null) {
                        ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Override: BuildingPrefab -> {refBuildingPrefab.ID}", Verbosity.DoNotShow);

                        debugStage = 1000;
                        var refEconomicClassWrappered = AccessTools.FieldRefAccess<BuildingPrefab,
                            Dictionary<LazyLoadRowRef<EconomicClassType>, int>>("NormalMaxResidentsByEconomicClassWrappered");
                        if (!refEconomicClassWrappered(modBuildingPrefab).IsNull()) {
                            refBuildingPrefab.NormalMaxResidents = 0;
                            AccessTools.FieldRefAccess<BuildingPrefab, Dictionary<LazyLoadRowRef<EconomicClassType>, int>>("NormalMaxResidentsByEconomicClassWrappered")(refBuildingPrefab) = 
                                AccessTools.FieldRefAccess<BuildingPrefab, Dictionary<LazyLoadRowRef<EconomicClassType>, int>>("NormalMaxResidentsByEconomicClassWrappered")(modBuildingPrefab);
                            if (!refEconomicClassWrappered(refBuildingPrefab).IsNull()) {
                                foreach (var refEconomicClass in refEconomicClassWrappered(refBuildingPrefab)) {
                                    refBuildingPrefab.NormalMaxResidents += refEconomicClass.Value;
                                    refBuildingPrefab.NormalMaxResidentsByEconomicClass[refEconomicClass.Key.GetActualRow("BuildingPrefab", refBuildingPrefab.ID)] = refEconomicClass.Value;
                                }
                            }
                        }

                        debugStage = 3000;
                        var refProfessionWrappered = AccessTools.FieldRefAccess<BuildingPrefab,
                            Dictionary<LazyLoadRowRef<ProfessionType>, int>>("NormalMaxJobsByProfessionWrappered");
                        if (!refProfessionWrappered(modBuildingPrefab).IsNull()) {
                            refBuildingPrefab.NormalMaxJobs = 0;
                            AccessTools.FieldRefAccess<BuildingPrefab, Dictionary<LazyLoadRowRef<ProfessionType>, int>>("NormalMaxJobsByProfessionWrappered")(refBuildingPrefab) =
                                AccessTools.FieldRefAccess<BuildingPrefab, Dictionary<LazyLoadRowRef<ProfessionType>, int>>("NormalMaxJobsByProfessionWrappered")(modBuildingPrefab);
                            if (!refProfessionWrappered(refBuildingPrefab).IsNull()) {
                                foreach (var refProfession in refProfessionWrappered(refBuildingPrefab)) {
                                    refBuildingPrefab.NormalMaxJobs += refProfession.Value;
                                    refBuildingPrefab.NormalMaxJobsByProfession[refProfession.Key.GetActualRow("BuildingPrefab", refBuildingPrefab.ID)] = refProfession.Value;
                                }
                            }
                        }

                        debugStage = 6000;
                        if (!modBuildingPrefab.BuildingFloorDefinitions.IsNull() && modBuildingPrefab.BuildingFloorDefinitions.Count > 0) {
                            AccessTools.FieldRefAccess<BuildingPrefab, List<BuildingFloor>>("BuildingFloorDefinitions")(refBuildingPrefab) = 
                                AccessTools.FieldRefAccess<BuildingPrefab, List<BuildingFloor>>("BuildingFloorDefinitions")(modBuildingPrefab);
                            if (refBuildingPrefab.BuildingFloorDefinitions.Count > 0) {
                                double refVolTotal = 0.0;
                                double refAreaTotal = 0.0;
                                double refVolSurface = 0.0;
                                double refAreaSurface = 0.0;
                                double refVolStorage = 0.0;
                                double refVolResidence = 0.0;
                                double refVolProduction = 0.0;
                                double refAreaStorage = 0.0;
                                double refAreaProduction = 0.0;
                                refBuildingPrefab.BuildingFloors.Clear();
                                if (refBuildingPrefab.BuildingFloorDefinitions.Count == 1 && 
                                    refBuildingPrefab.BuildingFloorDefinitions.First().MinFloor == -9000) {
                                    refBuildingPrefab.MinFloor = 0;
                                    refBuildingPrefab.MaxFloor = 0;
                                } else {
                                    int refFloorLimitUpper = 999;
                                    int refFloorLimitLower = -999;
                                    foreach (BuildingFloor refFloorDef in refBuildingPrefab.BuildingFloorDefinitions) {
                                        int refTotalFloors = 0;
                                        for (int currFloor = refFloorDef.MinFloor; currFloor <= refFloorDef.MaxFloor; currFloor++) {
                                            if (!refBuildingPrefab.BuildingFloors.ContainsKey(currFloor)) refBuildingPrefab.BuildingFloors[currFloor] = refFloorDef;
                                            if (currFloor < refFloorLimitUpper) refFloorLimitUpper = currFloor;
                                            if (currFloor > refFloorLimitLower) refFloorLimitLower = currFloor;
                                            if (currFloor == 0) refBuildingPrefab.GroundFloorDimensions = refFloorDef.FloorSize.x * refFloorDef.FloorSize.z;
                                            refTotalFloors++;
                                        }
                                        refFloorDef.PercentageStorageSpaceMultiplier = refFloorDef.PercentageStorageSpace / 100f;
                                        refFloorDef.PercentageLivingSpaceMultiplier = refFloorDef.PercentageLivingSpace / 100f;
                                        refFloorDef.PercentageWorkSpaceMultiplier = refFloorDef.PercentageWorkSpace / 100f;
                                        refFloorDef.TotalVolumeNonDimensioned_SingleFloor = refFloorDef.FloorSize.x * refFloorDef.FloorSize.y * refFloorDef.FloorSize.z;
                                        refFloorDef.TotalAreaNonDimensioned_SingleFloor = refFloorDef.FloorSize.x * refFloorDef.FloorSize.z;
                                        refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange = refFloorDef.TotalVolumeNonDimensioned_SingleFloor * refTotalFloors;
                                        refFloorDef.TotalAreaNonDimensioned_EntireFloorRange = refFloorDef.TotalAreaNonDimensioned_SingleFloor * refTotalFloors;
                                        int refTotalSpacePercent = refFloorDef.PercentageLivingSpace + refFloorDef.PercentageStorageSpace + refFloorDef.PercentageWorkSpace;
                                        if (refTotalSpacePercent <= 100) {
                                            if (refFloorDef.PercentageStorageSpaceMultiplier > 0.0) {
                                                refVolStorage += refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange * refFloorDef.PercentageStorageSpaceMultiplier;
                                                refAreaStorage += refFloorDef.TotalAreaNonDimensioned_EntireFloorRange * refFloorDef.PercentageStorageSpaceMultiplier;
                                            }
                                            if (refFloorDef.PercentageLivingSpaceMultiplier > 0.0) {
                                                refVolResidence += refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange * refFloorDef.PercentageLivingSpaceMultiplier;
                                            }
                                            if (refFloorDef.PercentageWorkSpaceMultiplier > 0.0) {
                                                refVolProduction += refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange * refFloorDef.PercentageWorkSpaceMultiplier;
                                                refAreaProduction += refFloorDef.TotalAreaNonDimensioned_EntireFloorRange * refFloorDef.PercentageWorkSpaceMultiplier;
                                            }
                                        }
                                        refVolTotal += refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange;
                                        refAreaTotal += refFloorDef.TotalAreaNonDimensioned_EntireFloorRange;
                                        if (refFloorDef.MinFloor >= 0) {
                                            refVolSurface += refFloorDef.TotalVolumeNonDimensioned_EntireFloorRange;
                                            refAreaSurface += refFloorDef.TotalAreaNonDimensioned_EntireFloorRange;
                                        }
                                    }
                                    refBuildingPrefab.MinFloor = refFloorLimitUpper;
                                    refBuildingPrefab.MaxFloor = refFloorLimitLower;
                                    for (int k = refBuildingPrefab.MinFloor; k <= refBuildingPrefab.MaxFloor; k++) {
                                        if (!refBuildingPrefab.BuildingFloors.ContainsKey(k)) {
                                            ArcenDebugging.LogSingleLine("Missing floor " + k + " from building prefab '" + refBuildingPrefab.ID + "'!  Floor numbers cannot be skipped.", Verbosity.ShowAsError);
                                        }
                                    }
                                }
                                refBuildingPrefab.NormalTotalBuildingVolumeFullDimensions = (int)Math.Round(refVolTotal * DIMENSION_CUBED);
                                refBuildingPrefab.NormalTotalBuildingFloorAreaFullDimensions = (int)Math.Round(refAreaTotal * DIMENSION_SQUARED);
                                refBuildingPrefab.NormalTotalStorageVolumeFullDimensions = (int)Math.Round(refVolStorage * DIMENSION_CUBED);
                                refBuildingPrefab.NormalTotalNonStorageVolumeFullDimensions = refBuildingPrefab.NormalTotalBuildingVolumeFullDimensions - refBuildingPrefab.NormalTotalStorageVolumeFullDimensions;
                                refBuildingPrefab.NormalTotalResidentialVolumeFullDimensions = (int)Math.Round(refVolResidence * DIMENSION_CUBED);
                                refBuildingPrefab.NormalTotalWorkVolumeFullDimensions = (int)Math.Round(refVolProduction * DIMENSION_CUBED);
                                refBuildingPrefab.NormalTotalStorageFloorAreaFullDimensions = (int)Math.Round(refAreaStorage * DIMENSION_SQUARED);
                                refBuildingPrefab.NormalTotalWorkFloorAreaFullDimensions = (int)Math.Round(refAreaProduction * DIMENSION_SQUARED);
                            }
                        }

                        debugStage = 10000;
                        var refMarkerWrappered = AccessTools.FieldRefAccess<BuildingPrefab, LazyLoadRowRef<BuildingMarkerPrefab>>("MarkerPrefabWrappered");
                        if (!refMarkerWrappered(modBuildingPrefab).IsNull()) {
                            AccessTools.FieldRefAccess<BuildingPrefab, LazyLoadRowRef<BuildingMarkerPrefab>>("MarkerPrefabWrappered")(refBuildingPrefab) =
                                AccessTools.FieldRefAccess<BuildingPrefab, LazyLoadRowRef<BuildingMarkerPrefab>>("MarkerPrefabWrappered")(modBuildingPrefab);
                            if (!refMarkerWrappered(refBuildingPrefab).IsNull()) {
                                refBuildingPrefab.MarkerPrefab = refMarkerWrappered(refBuildingPrefab)?.GetActualRow("BuildingPrefab", refBuildingPrefab.ID);
                            }
                        }

                        debugStage = 15000;
                        if (refBuildingPrefab.Type.IsConsideredNotToHavePeopleInsideEvenWhenHasWorkers)
                            refBuildingPrefab.IsConsideredBuildingWithPeople = false;
                        else if (refBuildingPrefab.NormalMaxJobs > 0 || refBuildingPrefab.NormalMaxResidents > 0)
                            refBuildingPrefab.IsConsideredBuildingWithPeople = true;
                        else refBuildingPrefab.IsConsideredBuildingWithPeople = false;
                    } else {
                        ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Type 'BuildingPrefab' with ID '{modBuildingPrefab.ID}' doesn't exist! Ignoring override attempt.", Verbosity.DoNotShow);
                        continue;
                    }
                } catch (Exception e) {
                    ArcenDebugging.LogDebugStageWithStack("Override_BuildingPrefab.DoPostFinalLoadCrossTableWork_OnLoadingThread", debugStage, e, Verbosity.ShowAsError);
                }
            }
        }
    }
}

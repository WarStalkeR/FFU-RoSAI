using Arcen.Universal;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arcen.HotM.Core {
    public class BuildingPrefabTableOverride : ArcenDynamicTable<BuildingPrefab> {
        public static readonly BuildingPrefabTableOverride Instance = new BuildingPrefabTableOverride();
        public const double DIMENSION_BASE = 18.0;
        public const double DIMENSION_SQUARED = 324.0;
        public const double DIMENSION_CUBED = 5832.0;

        private BuildingPrefabTableOverride() : base("Override_BuildingPrefab", ArcenDynamicTableType.XMLDirectory, ReloadDuringRuntime.Allow, TableLocalization.Skip) {
        }

        public override BuildingPrefab GetNewRowFromPool() {
            return BuildingPrefab.GetFromPoolOrCreate();
        }

        protected override void PrepForCompleteReloadLater() {
        }

        public override void DoPreInitializationLogic() {
            for (int i = 0; i < A5ObjectRootTable.Instance.Rows.Length; i++) {
                A5ObjectRootTable.Instance.Rows[i].Building = null;
            }
        }
        public override void DoPostFinalLoadCrossTableWork_OnLoadingThread() {
            DoPostFinalLoadCrossTableWork_OnLoadingThread(false);
        }

        public void DoPostFinalLoadCrossTableWork_OnLoadingThread(bool IsFromSelectReload) {
            bool isLevelEditor = Engine_Universal.GameLoop.IsLevelEditor;
            BuildingPrefab[] array = Rows;
            foreach (BuildingPrefab buildingPrefab in array) {
                ArcenDebugging.LogSingleLine($"[FFU: RoSAI] BuildingPrefabTableOverride: {buildingPrefab.ID}", Verbosity.DoNotShow);
                /*
                int debugStage = 0;
                try {
                    debugStage = 1000;
                    if (!isLevelEditor) {
                        buildingPrefab.SetOriginalDisplayName("WRONG USAGE: USE BUILDING TYPE NAME");
                    }
                    var refTypeWrappered = AccessTools.FieldRefAccess<BuildingPrefab, LazyLoadRowRef<BuildingType>>("TypeWrappered")(buildingPrefab);
                    buildingPrefab.Type = refTypeWrappered.GetActualRow("BuildingPrefab", buildingPrefab.ID);
                    debugStage = 3000;
                    if (!IsFromSelectReload) {
                        if (A5ObjectAggregation.ObjectRootsByPath.TryGetValue(buildingPrefab.PathToPlaceable, out buildingPrefab.PlaceableRoot)) {
                            if (buildingPrefab.PlaceableRoot.Building != null) {
                                ArcenDebugging.LogSingleLine("A5ObjectRoot with path '" + buildingPrefab.PathToPlaceable + "' already had a BuildingPrefab on it, so could not add a second one from BuildingPrefab '" + buildingPrefab.ID + "'", Verbosity.ShowAsError);
                            } else {
                                buildingPrefab.PlaceableRoot.Building = buildingPrefab;
                            }
                        } else {
                            ArcenDebugging.LogSingleLine("Could not find A5ObjectRoot with path '" + buildingPrefab.PathToPlaceable + "' from BuildingPrefab '" + buildingPrefab.ID + "'", Verbosity.ShowAsError);
                        }
                    }
                    debugStage = 9000;
                    buildingPrefab.NormalMaxResidents = 0;
                    var refEconomicClassWrappered = AccessTools.FieldRefAccess<BuildingPrefab, 
                        Universal.Dictionary<LazyLoadRowRef<EconomicClassType>, int>>("NormalMaxResidentsByEconomicClassWrappered")(buildingPrefab);
                    foreach (Universal.KeyValuePair<LazyLoadRowRef<EconomicClassType>, int> item in refEconomicClassWrappered) {
                        buildingPrefab.NormalMaxResidents += item.Value;
                        buildingPrefab.NormalMaxResidentsByEconomicClass[item.Key.GetActualRow("BuildingPrefab", buildingPrefab.ID)] = item.Value;
                    }
                    debugStage = 13000;
                    buildingPrefab.NormalMaxJobs = 0;
                    var refProfessionWrappered = AccessTools.FieldRefAccess<BuildingPrefab,
                        Universal.Dictionary<LazyLoadRowRef<ProfessionType>, int>>("NormalMaxJobsByProfessionWrappered")(buildingPrefab);
                    foreach (Universal.KeyValuePair<LazyLoadRowRef<ProfessionType>, int> item2 in refProfessionWrappered) {
                        buildingPrefab.NormalMaxJobs += item2.Value;
                        buildingPrefab.NormalMaxJobsByProfession[item2.Key.GetActualRow("BuildingPrefab", buildingPrefab.ID)] = item2.Value;
                    }
                    debugStage = 17000;
                    buildingPrefab.BuildingFloors.Clear();
                    double num = 0.0;
                    double num2 = 0.0;
                    double num3 = 0.0;
                    double num4 = 0.0;
                    double num5 = 0.0;
                    double num6 = 0.0;
                    double num7 = 0.0;
                    double num8 = 0.0;
                    double num9 = 0.0;
                    if (buildingPrefab.BuildingFloorDefinitions.Count <= 0) {
                        buildingPrefab.MinFloor = 0;
                        buildingPrefab.MaxFloor = 0;
                    } else {
                        int num10 = 999;
                        int num11 = -999;
                        foreach (BuildingFloor buildingFloorDefinition in buildingPrefab.BuildingFloorDefinitions) {
                            int num12 = 0;
                            for (int j = buildingFloorDefinition.MinFloor; j <= buildingFloorDefinition.MaxFloor; j++) {
                                if (buildingPrefab.BuildingFloors.ContainsKey(j)) {
                                    ArcenDebugging.LogSingleLine("Tried to add floor " + j + " more than once in building prefab '" + buildingPrefab.ID + "'!", Verbosity.ShowAsError);
                                } else {
                                    buildingPrefab.BuildingFloors[j] = buildingFloorDefinition;
                                }
                                if (j < num10) {
                                    num10 = j;
                                }
                                if (j > num11) {
                                    num11 = j;
                                }
                                if (j == 0) {
                                    buildingPrefab.GroundFloorDimensions = buildingFloorDefinition.FloorSize.x * buildingFloorDefinition.FloorSize.z;
                                }
                                num12++;
                            }
                            buildingFloorDefinition.PercentageStorageSpaceMultiplier = (float)buildingFloorDefinition.PercentageStorageSpace / 100f;
                            buildingFloorDefinition.PercentageLivingSpaceMultiplier = (float)buildingFloorDefinition.PercentageLivingSpace / 100f;
                            buildingFloorDefinition.PercentageWorkSpaceMultiplier = (float)buildingFloorDefinition.PercentageWorkSpace / 100f;
                            if (buildingFloorDefinition.PercentageStorageSpace + buildingFloorDefinition.PercentageLivingSpace + buildingFloorDefinition.PercentageWorkSpace > 100) {
                                ArcenDebugging.LogSingleLine("Defined higher than 1x multiplier used space on floor def (floors " + buildingFloorDefinition.MinFloor + "-" + buildingFloorDefinition.MaxFloor + ") for building prefab '" + buildingPrefab.ID + "'!  That should be 100% or less.\n PercentageStorageSpaceMultiplier: " + buildingFloorDefinition.PercentageStorageSpaceMultiplier + "\n PercentageLivingSpaceMultiplier: " + buildingFloorDefinition.PercentageLivingSpaceMultiplier + "\n PercentageWorkSpaceMultiplier: " + buildingFloorDefinition.PercentageWorkSpaceMultiplier, Verbosity.ShowAsError);
                            }
                            buildingFloorDefinition.TotalVolumeNonDimensioned_SingleFloor = buildingFloorDefinition.FloorSize.x * buildingFloorDefinition.FloorSize.y * buildingFloorDefinition.FloorSize.z;
                            buildingFloorDefinition.TotalAreaNonDimensioned_SingleFloor = buildingFloorDefinition.FloorSize.x * buildingFloorDefinition.FloorSize.z;
                            buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange = buildingFloorDefinition.TotalVolumeNonDimensioned_SingleFloor * (double)num12;
                            buildingFloorDefinition.TotalAreaNonDimensioned_EntireFloorRange = buildingFloorDefinition.TotalAreaNonDimensioned_SingleFloor * (double)num12;
                            int num13 = buildingFloorDefinition.PercentageLivingSpace + buildingFloorDefinition.PercentageStorageSpace + buildingFloorDefinition.PercentageWorkSpace;
                            if (num13 > 100) {
                                ArcenDebugging.LogSingleLine("Defined " + num13 + "% used space on floor def (floors " + buildingFloorDefinition.MinFloor + "-" + buildingFloorDefinition.MaxFloor + ") for building prefab '" + buildingPrefab.ID + "'!  That should be 100% or less.  (Less would mean there is some 'other' space, which is fine.)", Verbosity.ShowAsError);
                            } else {
                                if (buildingFloorDefinition.PercentageStorageSpaceMultiplier > 0.0) {
                                    num5 += buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange * buildingFloorDefinition.PercentageStorageSpaceMultiplier;
                                    num8 += buildingFloorDefinition.TotalAreaNonDimensioned_EntireFloorRange * buildingFloorDefinition.PercentageStorageSpaceMultiplier;
                                }
                                if (buildingFloorDefinition.PercentageLivingSpaceMultiplier > 0.0) {
                                    num6 += buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange * buildingFloorDefinition.PercentageLivingSpaceMultiplier;
                                }
                                if (buildingFloorDefinition.PercentageWorkSpaceMultiplier > 0.0) {
                                    num7 += buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange * buildingFloorDefinition.PercentageWorkSpaceMultiplier;
                                    num9 += buildingFloorDefinition.TotalAreaNonDimensioned_EntireFloorRange * buildingFloorDefinition.PercentageWorkSpaceMultiplier;
                                }
                            }
                            num += buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange;
                            num2 += buildingFloorDefinition.TotalAreaNonDimensioned_EntireFloorRange;
                            if (buildingFloorDefinition.MinFloor >= 0) {
                                num3 += buildingFloorDefinition.TotalVolumeNonDimensioned_EntireFloorRange;
                                num4 += buildingFloorDefinition.TotalAreaNonDimensioned_EntireFloorRange;
                            }
                        }
                        buildingPrefab.MinFloor = num10;
                        buildingPrefab.MaxFloor = num11;
                        for (int k = buildingPrefab.MinFloor; k <= buildingPrefab.MaxFloor; k++) {
                            if (!buildingPrefab.BuildingFloors.ContainsKey(k)) {
                                ArcenDebugging.LogSingleLine("Missing floor " + k + " from building prefab '" + buildingPrefab.ID + "'!  Floor numbers cannot be skipped.", Verbosity.ShowAsError);
                            }
                        }
                    }
                    buildingPrefab.NormalTotalBuildingVolumeFullDimensions = (int)Math.Round(num * 5832.0);
                    buildingPrefab.NormalTotalBuildingFloorAreaFullDimensions = (int)Math.Round(num2 * 324.0);
                    debugStage = 22000;
                    buildingPrefab.NormalTotalStorageVolumeFullDimensions = (int)Math.Round(num5 * 5832.0);
                    buildingPrefab.NormalTotalNonStorageVolumeFullDimensions = buildingPrefab.NormalTotalBuildingVolumeFullDimensions - buildingPrefab.NormalTotalStorageVolumeFullDimensions;
                    buildingPrefab.NormalTotalResidentialVolumeFullDimensions = (int)Math.Round(num6 * 5832.0);
                    buildingPrefab.NormalTotalWorkVolumeFullDimensions = (int)Math.Round(num7 * 5832.0);
                    buildingPrefab.NormalTotalStorageFloorAreaFullDimensions = (int)Math.Round(num8 * 324.0);
                    buildingPrefab.NormalTotalWorkFloorAreaFullDimensions = (int)Math.Round(num9 * 324.0);
                    debugStage = 27000;
                    var refPrefabWrappered = AccessTools.FieldRefAccess<BuildingPrefab,
                       LazyLoadRowRef<BuildingMarkerPrefab>>("MarkerPrefabWrappered")(buildingPrefab);
                    buildingPrefab.MarkerPrefab = refPrefabWrappered?.GetActualRow("BuildingPrefab", buildingPrefab.ID);
                    debugStage = 36000;
                    if (buildingPrefab.Type.IsConsideredNotToHavePeopleInsideEvenWhenHasWorkers) {
                        buildingPrefab.IsConsideredBuildingWithPeople = false;
                    } else if (buildingPrefab.NormalMaxJobs > 0 || buildingPrefab.NormalMaxResidents > 0) {
                        buildingPrefab.IsConsideredBuildingWithPeople = true;
                    } else {
                        buildingPrefab.IsConsideredBuildingWithPeople = false;
                    }
                    debugStage = 37000;
                } catch (Exception e) {
                    ArcenDebugging.LogDebugStageWithStack("BuildingPrefab.DoPostFinalLoadCrossTableWork_OnLoadingThread", debugStage, e, Verbosity.ShowAsError);
                }
                */
            }
        }
    }
}

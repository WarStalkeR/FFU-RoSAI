using Arcen.HotM.Core;
using Arcen.Universal;
using HarmonyLib;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModPatch {
        public static bool ProcessNodeLoadOrReload_Plus(BuildingPrefab __instance, ArcenXMLElement Data, bool IsForReload) {
            bool bReplaceFloors = false;
            bool bReplaceMarker = false;
            Data.Fill("custom_replace_floors", ref bReplaceFloors, false);
            Data.Fill("custom_replace_marker", ref bReplaceMarker, false);
            if (bReplaceFloors) {
                __instance.BuildingFloorDefinitions.Clear();
            }
            if (bReplaceMarker) {
                AccessTools.FieldRefAccess<BuildingPrefab, LazyLoadRowRef<BuildingMarkerPrefab>>("MarkerPrefabWrappered")(__instance) = null;
                //__instance.MarkerPrefabWrappered = null;
                __instance.MarkerPrefab = null;
            }
            return true;
        }
    }
}
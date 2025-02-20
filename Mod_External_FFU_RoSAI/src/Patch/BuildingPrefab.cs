using Arcen.HotM.Core;
using Arcen.Universal;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModPatch {
        public static bool BuildingPrefab_Extended(BuildingPrefab __instance, ArcenXMLElement Data, bool IsForReload) {
            bool ResetFloors = false;
            Data.Fill("custom_reset_floors", ref ResetFloors, false);
            if (ResetFloors) __instance.BuildingFloorDefinitions.Clear();
            ArcenDebugging.LogSingleLine("[FFU: RoSAI] BuildingPrefab is using patched method!", Verbosity.DoNotShow);
            return true;
        }
    }
}
using Arcen.HotM.Core;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModPatch {
        public static void ReloadSelectDataFromXml_Extended(Engine_HotM __instance) {
            if (!A5ObjectAggregation.GetIsReloadBlocked()) {
                BuildingPrefabTableOverride.Instance.ReloadSelectData();
                BuildingPrefabTableOverride.Instance.DoPostFinalLoadCrossTableWork_OnLoadingThread(true);
            }
        }
    }
}
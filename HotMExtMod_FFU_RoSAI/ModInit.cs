using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.FFU.RoSAI {
    public class ModInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            DataRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            DataRefs.EvictionTarget = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            DataRefs.BuildingValidEvictionTarget = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
        }
    }

    public static class DataRefs {
        public static AbilityType Eviction;
        public static BuildingTag EvictionTarget;
        public static VisColorUsage BuildingValidEvictionTarget;
    }
}
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.FFU.RoSAI {
    public class ModInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            //DataRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            //DataRefs.EvictionTarget = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            //DataRefs.BuildingValidEvictionTarget = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
        }
    }

    public static class DataRefs {
        public const float LEGAL_MILITARY = 25f;
        public const float LEGAL_SECURITY = 17.5f;
        public const float LEGAL_SYNDICATE = 5f;
        public const float LEGAL_HACKERS = 10f;
        public const float LEGAL_CULT = 7.5f;
        public const float LEGAL_GANGS = 2.5f;
        public const float LEGAL_MARKET = 3.5f;
        public const float LEGAL_VERMIN = 0.75f;
        public const float LEGAL_BACTERIA = 0.25f;
        public const float LEGAL_DEFAULT = 1f;

        public const float EVICT_VOLUME = 0.05f;
        public const float EVICT_AREA = 0.25f;
        public const float EVICT_STORAGE = 0.1f;

        public static AbilityType Eviction;
        public static BuildingTag EvictionTarget;
        public static VisColorUsage BuildingValidEvictionTarget;
    }
}
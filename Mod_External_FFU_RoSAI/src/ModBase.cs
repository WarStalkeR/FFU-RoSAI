using Arcen.Universal;
using Arcen.HotM.Core;
using System.Collections;
using System;
using HarmonyLib;
using UnityEngine;

namespace Arcen.HotM.FFU.RoSAI {
    public partial class ModInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            ArcenDebugging.LogSingleLine("[FFU: RoSAI] initializing...", Verbosity.DoNotShow);
            Engine_Universal.SafelyWrappedStartCoroutine(ExecuteLoadFlow());
        }
        private IEnumerator ExecuteLoadFlow() {
            //yield return Engine_Universal.SafelyWrappedStartCoroutine(ModPatchClasses());
            yield return new WaitUntil(() => Engine_Universal.HasFinishedInitialXmlLoad);
            yield return Engine_Universal.SafelyWrappedStartCoroutine(ModLoadOverrides());
            ModLoadRefData();
        }
        public IEnumerator ModPatchClasses() {
            ArcenDebugging.LogSingleLine("[FFU: RoSAI] Applying patches via Harmony...", Verbosity.DoNotShow);
            var Mod = new Harmony("arcen.hotm.ffu.rosai");

            try { ArcenDebugging.LogSingleLine("[FFU: RoSAI] Patching: Arcen.HotM.Core.BuildingPrefab -> ProcessNodeLoadOrReload", Verbosity.DoNotShow);
                var refMethod = AccessTools.Method(typeof(BuildingPrefab), "ProcessNodeLoadOrReload");
                var prefixPatch = SymbolExtensions.GetMethodInfo(() => 
                    ModPatch.BuildingPrefab_Extended(default, default, default));
                Mod.Patch(refMethod, prefix: new HarmonyMethod(prefixPatch));
            } catch (Exception ex) { ArcenDebugging.LogSingleLine($"[FFU: RoSAI] Failed: {ex}", Verbosity.DoNotShow); }

            yield break;
        }
        public IEnumerator ModLoadOverrides() {
            yield return new WaitUntil(() => BuildingPrefabTable.Instance.InitializationStage.IsComplete());
            //BuildingPrefabTableOverride.Instance.Initialize(false);
            //BuildingPrefabTable.Instance.Initialize(false);
            //BuildingPrefabTable.Instance.ReloadSelectData();
            //BuildingPrefabTable.Instance.DoPostFinalLoadCrossTableWork_OnLoadingThread(true);
            yield break;
        }
        public void ModLoadRefData() {
            ArcenDebugging.LogSingleLine("[FFU: RoSAI] Loading data references...", Verbosity.DoNotShow);
            ModRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            ModRefs.EvictionTag = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            ModRefs.EvictionVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
            ModRefs.LiquidMetal = ResourceTypeTable.Instance.GetRowByID("GadoliniumMesosilicate");
            ModRefs.InfoVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidInfoTarget");
        }
    }

    public static partial class ModRefs {
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

        public const float EVICT_VOLUME_MULT = 0.05f;
        public const float EVICT_STORAGE_MULT = 0.1f;
        public const float EVICT_AREA_MULT = 0.25f;

        public static AbilityType Eviction = null;
        public static BuildingTag EvictionTag = null;
        public static VisColorUsage EvictionVis = null;
        public static ResourceType LiquidMetal = null;
        public static VisColorUsage InfoVis = null;
    }
}
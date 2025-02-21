using Arcen.Universal;
using Arcen.HotM.Core;
using System.Collections;
using System;
using HarmonyLib;
using UnityEngine;
using System.IO;

namespace Arcen.HotM.FFU.RoSAI {
    public partial class ModInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Initializing...", Verbosity.DoNotShow);
            Engine_Universal.SafelyWrappedStartCoroutine(ExecuteLoadFlow());
        }
        private IEnumerator ExecuteLoadFlow() {
            yield return Engine_Universal.SafelyWrappedStartCoroutine(ModPatchClasses());
            yield return Engine_Universal.SafelyWrappedStartCoroutine(ModLoadInitializers());
            yield return new WaitUntil(() => Engine_Universal.HasFinishedInitialXmlLoad);
            yield return Engine_Universal.SafelyWrappedStartCoroutine(ModLoadOverrides());
            ModLoadRefData();
        }
        public IEnumerator ModPatchClasses() {
            ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Applying patches via Harmony:", Verbosity.DoNotShow);
            Harmony Mod = new Harmony("arcen.hotm.ffu.rosai");

            try { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Patch: Arcen.HotM.Core.Engine_HotM -> ReloadSelectDataFromXml", Verbosity.DoNotShow);
                var refMethod = AccessTools.Method(typeof(Engine_HotM), "ReloadSelectDataFromXml");
                var postfixPatch = SymbolExtensions.GetMethodInfo(() =>
                    ModPatch.ReloadSelectDataFromXml_Extended(default));
                Mod.Patch(refMethod, postfix: new HarmonyMethod(postfixPatch));
            } catch (Exception ex) { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Failed: {ex}", Verbosity.DoNotShow); }

            ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Patching routine complete!", Verbosity.DoNotShow);
            yield break;
        }
        public IEnumerator ModLoadInitializers() {
            yield return new WaitUntil(() => BuildingPrefabTable.Instance.InitializationStage.IsLoaded());
            BuildingPrefabTableOverride.Instance.Initialize(false);
            yield break;
        }
        public IEnumerator ModLoadOverrides() {
            yield return new WaitUntil(() => BuildingPrefabTable.Instance.InitializationStage.IsComplete());
            //BuildingPrefabTableOverride.Instance.ReloadSelectData();
            //BuildingPrefabTableOverride.Instance.DoPostFinalLoadCrossTableWork_OnLoadingThread(true);
            //ModPatch.BuildingPrefabOverride();
            //BuildingPrefabTable.Instance.Initialize(false);
            //BuildingPrefabTable.Instance.ReloadSelectData();
            //BuildingPrefabTable.Instance.DoPostFinalLoadCrossTableWork_OnLoadingThread(true);
            yield break;
        }
        public void ModLoadRefData() {
            ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Loading data references...", Verbosity.DoNotShow);
            ModRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            ModRefs.EvictionTag = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            ModRefs.EvictionVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
            ModRefs.LiquidMetal = ResourceTypeTable.Instance.GetRowByID("GadoliniumMesosilicate");
            ModRefs.InfoVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidInfoTarget");
        }
    }

    public static partial class ModRefs {
        public const string MOD_NAME = "Fight For Universe: Rise of Super AI";
        public const string MOD_LOG = "[FFU:RoSAI]";

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
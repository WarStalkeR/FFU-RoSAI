﻿using Arcen.Universal;
using Arcen.HotM.Core;
using System.Collections;
using System;
using HarmonyLib;
using UnityEngine;

namespace Arcen.HotM.FFU.RoSAI {
    public partial class ModInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Initializing...", Verbosity.DoNotShow);
            Engine_Universal.SafelyWrappedStartCoroutine(ExecuteLoadFlow());
        }
        public void RunImmediatelyOnHandlerProcessed(ArcenExternalDllInitialLoadCall Loader) {
        }
        public void RunAfterAllTableImportsComplete(ArcenExternalDllInitialLoadCall Loader) {
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

            try { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Arcen.HotM.Core.BuildingPrefab -> ProcessNodeLoadOrReload", Verbosity.DoNotShow);
                var refMethod = AccessTools.Method(typeof(BuildingPrefab), "ProcessNodeLoadOrReload");
                var prefixPatch = SymbolExtensions.GetMethodInfo(() =>
                    ModPatch.ProcessNodeLoadOrReload_Plus(default, default, default));
                Mod.Patch(refMethod, prefix: new HarmonyMethod(prefixPatch));
            } catch (Exception ex) { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Failed: {ex}", Verbosity.DoNotShow); }

            try { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Arcen.HotM.Core.BuildingType -> ProcessNodeLoadOrReload", Verbosity.DoNotShow);
                var refMethod = AccessTools.Method(typeof(BuildingType), "ProcessNodeLoadOrReload");
                var postfixPatch = SymbolExtensions.GetMethodInfo(() =>
                    ModPatch.ProcessNodeLoadOrReload_TagOps(default, default, default));
                Mod.Patch(refMethod, postfix: new HarmonyMethod(postfixPatch));
            } catch (Exception ex) { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Failed: {ex}", Verbosity.DoNotShow); }

            try { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Arcen.HotM.Core.Engine_HotM -> ReloadSelectDataFromXml", Verbosity.DoNotShow);
                var refMethod = AccessTools.Method(typeof(Engine_HotM), "ReloadSelectDataFromXml");
                var postfixPatch = SymbolExtensions.GetMethodInfo(() =>
                    ModPatch.ReloadSelectDataFromXml_Extended(default));
                Mod.Patch(refMethod, postfix: new HarmonyMethod(postfixPatch));
            } catch (Exception ex) { ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} Failed: {ex}", Verbosity.DoNotShow); }
            yield break;
        }
        public IEnumerator ModLoadInitializers() {
            yield break;
        }
        public IEnumerator ModLoadOverrides() {
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

        public const float EVICT_POP_DIV = 2500f;
        public const float EVICT_SIZE_DIV = 75000f;
        public const float EVICT_TENANT_COST = 15f;
        public const float EVICT_WORKER_COST = 5f;
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
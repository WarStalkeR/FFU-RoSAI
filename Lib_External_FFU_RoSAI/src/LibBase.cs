using Arcen.Universal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Arcen.HotM.FFU.RoSAI {
    public partial class LibInit : IArcenExternalDllInitialLoadCall {
        public void RunOnFirstTimeExternalAssemblyLoaded() {
            LoadDependency("FFU_RoSAI", "0Harmony");
        }
        public void RunImmediatelyOnHandlerProcessed(ArcenExternalDllInitialLoadCall Loader) {
        }
        public void RunAfterAllTableImportsComplete(ArcenExternalDllInitialLoadCall Loader) {
        }
        public static void LoadDependency(string refModName, string refDllName) {
            Assembly[] refAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool isAlreadyLoaded = refAssemblies.Any(a => string.Equals(a.GetName().Name, refDllName, StringComparison.OrdinalIgnoreCase));
            if (isAlreadyLoaded) {
                ArcenDebugging.LogSingleLine($"[LoadDependency] Already Loaded: {refDllName}", Verbosity.DoNotShow);
                return;
            }
            string pModFolder = XmlModTable.GetModByNameOrAltName(refModName).FullDirectoryPath;
            if (string.IsNullOrEmpty(pModFolder)) {
                ArcenDebugging.LogSingleLine($"[LoadDependency] Error! Invalid name or inexistent path for '{refModName}' mod.", Verbosity.ShowAsError);
                return;
            }
            string pDllFolder = Path.Combine(pModFolder, "ModdableLogicDLLs");
            string pDllFile = Path.Combine(pDllFolder, refDllName) + ".dll";
            if (!File.Exists(pDllFile)) {
                ArcenDebugging.LogSingleLine($"[LoadDependency] Error! File '{refDllName}' is not found in '{pDllFolder}' folder.", Verbosity.ShowAsError);
                return;
            }
            Assembly aLoadState = Assembly.LoadFrom(pDllFile);
            if (aLoadState == null) {
                ArcenDebugging.LogSingleLine($"[LoadDependency] Error! Couldn't load '{pDllFile}' dynamic library.", Verbosity.ShowAsError);
                return;
            }
            ArcenDebugging.LogSingleLine($"[LoadDependency] Successfully Loaded: {aLoadState.GetName().Name}", Verbosity.DoNotShow);
        }
    }
}
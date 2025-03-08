using Arcen.HotM.Core;
using Arcen.Universal;
using HarmonyLib;

namespace Arcen.HotM.FFU.RoSAI {
    public static partial class ModPatch {
        public static void BuildingVariantNodeProcessor_TagOps(BuildingTypeVariant __instance, ArcenXMLElement Data, bool IsForReprocessing, ArcenDynamicTableRow ParentRow) {
            List<LazyLoadRowRef<BuildingTag>> aTagsAdd = List<LazyLoadRowRef<BuildingTag>>.Create_WillNeverBeGCed(1, "BuildingTypeVariant-CustomTagsAdd", 1);
            List<LazyLoadRowRef<BuildingTag>> aTagsDel = List<LazyLoadRowRef<BuildingTag>>.Create_WillNeverBeGCed(1, "BuildingTypeVariant-CustomTagsDel", 1);
            Data.FillListFromTable("custom_tags_add", BuildingTagTable.Instance, aTagsAdd, false, IfPresent.ReplaceExistingList, Uniqueness.Required, ClearBeforeReading.IfNotPartialRecord);
            Data.FillListFromTable("custom_tags_del", BuildingTagTable.Instance, aTagsDel, false, IfPresent.ReplaceExistingList, Uniqueness.Required, ClearBeforeReading.IfNotPartialRecord);
            if (aTagsAdd.HasAnyItems || aTagsDel.HasAnyItems) {
                List<LazyLoadRowRef<BuildingTag>> refTags = AccessTools.FieldRefAccess<BuildingTypeVariant, List<LazyLoadRowRef<BuildingTag>>>("TagsWrappered")(__instance);
                //ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} '{__instance.FullSerializationID}' Original Tags: {refTags.Join(x => x.ID, ",")}", Verbosity.DoNotShow);
                if (aTagsAdd.HasAnyItems) {
                    refTags.AddRange(aTagsAdd);
                }
                if (aTagsDel.HasAnyItems) {
                    foreach (var refDelTag in aTagsDel) {
                        refTags.Remove(refDelTag);
                    }
                }
                //ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} '{__instance.FullSerializationID}' Modified Tags: {refTags.Join(x => x.ID, ",")}", Verbosity.DoNotShow);
                AccessTools.FieldRefAccess<BuildingTypeVariant, List<LazyLoadRowRef<BuildingTag>>>("TagsWrappered")(__instance) = refTags;
            }
        }

        public static void ProcessNodeLoadOrReload_TagOps(BuildingType __instance, ArcenXMLElement Data, bool IsForReload) {
            if (IsForReload) return;
            /*foreach (var refVariant in __instance.Variants) {
                List<LazyLoadRowRef<BuildingTag>> refTagsLog = AccessTools.FieldRefAccess<BuildingTypeVariant, List<LazyLoadRowRef<BuildingTag>>>("TagsWrappered")(refVariant.Value);
                ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} '{refVariant.Value.FullSerializationID}' Tags: {refTagsLog.Join(x => x.ID, ",")}", Verbosity.DoNotShow);
            }*/
            List<LazyLoadRowRef<BuildingTag>> aTagsAdd = List<LazyLoadRowRef<BuildingTag>>.Create_WillNeverBeGCed(1, "BuildingType-CustomTagsAdd", 1);
            List<LazyLoadRowRef<BuildingTag>> aTagsDel = List<LazyLoadRowRef<BuildingTag>>.Create_WillNeverBeGCed(1, "BuildingType-CustomTagsDel", 1);
            Data.FillListFromTable("custom_tags_add", BuildingTagTable.Instance, aTagsAdd, false, IfPresent.ReplaceExistingList, Uniqueness.Required, ClearBeforeReading.IfNotPartialRecord);
            Data.FillListFromTable("custom_tags_del", BuildingTagTable.Instance, aTagsDel, false, IfPresent.ReplaceExistingList, Uniqueness.Required, ClearBeforeReading.IfNotPartialRecord);
            if (aTagsAdd.HasAnyItems || aTagsDel.HasAnyItems) {
                foreach (var refVariant in __instance.Variants) {
                    List<LazyLoadRowRef<BuildingTag>> refTags = AccessTools.FieldRefAccess<BuildingTypeVariant, List<LazyLoadRowRef<BuildingTag>>>("TagsWrappered")(refVariant.Value);
                    //ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} '{refVariant.Value.FullSerializationID}' Original Tags: {refTags.Join(x => x.ID, ",")}", Verbosity.DoNotShow);
                    if (aTagsAdd.HasAnyItems) {
                        refTags.AddRange(aTagsAdd);
                    }
                    if (aTagsDel.HasAnyItems) {
                        foreach (var refDelTag in aTagsDel) {
                            refTags.Remove(refDelTag);
                        }
                    }
                    //ArcenDebugging.LogSingleLine($"{ModRefs.MOD_LOG} '{refVariant.Value.FullSerializationID}' Modified Tags: {refTags.Join(x => x.ID, ",")}", Verbosity.DoNotShow);
                    AccessTools.FieldRefAccess<BuildingTypeVariant, List<LazyLoadRowRef<BuildingTag>>>("TagsWrappered")(refVariant.Value) = refTags;
                }
            }
        }
    }
}
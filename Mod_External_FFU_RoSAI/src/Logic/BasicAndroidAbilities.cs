using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.FFU.RoSAI {
    public class BasicAndroidAbilities : IAbilityImplementation {
        public ActorAbilityResult TryHandleAbility(ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation, AbilityType Ability, ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic) {
            if (Ability == null || Actor == null) return ActorAbilityResult.PlayErrorSound;
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
            HandleDataRefsPreload();
            int debugStage = 0;

            try {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                if (unit == null) {
                    debugStage = 200;
                    if (Actor is ISimMachineVehicle vehicle)
                        ArcenDebugging.LogSingleLine("BasicAndroidAbilities: Called HandleAbility for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError);
                    return ActorAbilityResult.PlayErrorSound;
                }

                debugStage = 300;
                if (Ability.MustBeTargeted) {
                    switch (Logic) {
                        default: return ActorAbilityResult.OpenedInterface;
                        case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                        case ActorAbilityLogic.ExecuteAbilityAutomated:
                        case ActorAbilityLogic.TriggerAbilityAltView: {
                            debugStage = 700;
                            if (Actor.IsInAbilityTypeTargetingMode == Ability) Actor.SetTargetingMode(null, null);
                            else Actor.SetTargetingMode(Ability, null);
                            return ActorAbilityResult.OpenedInterface;
                        }
                    }
                }

                debugStage = 500;
                switch (Ability.ID) {
                    case "Eviction":
                        return ActorAbilityResult.PlayErrorSound;
                    default: {
                        ArcenDebugging.LogSingleLine("BasicAndroidAbilities: Called HandleAbility for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError);
                        return ActorAbilityResult.PlayErrorSound;
                    }
                }
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicAndroidAbilities.TryHandleAbility", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
                return ActorAbilityResult.PlayErrorSound;
            }
        }

        public void HandleAbilityHardTargeting(ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange) {
        }

        public void HandleAbilityMixedTargeting(ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange) {
            if (Ability == null || Actor == null) return;
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
            HandleDataRefsPreload();
            int debugStage = 0;
            try {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                if (unit == null) {
                    debugStage = 200;
                    if (Actor is ISimMachineVehicle vehicle) 
                        ArcenDebugging.LogSingleLine("BasicAndroidAbilities: Called HandleAbilityHardTargeting for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError);
                    return;
                }
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY(groundLevel);

                debugStage = 500;
                switch (Ability.ID) {
                    case "Eviction": {
                        debugStage = 1200;
                        if (attackRange < moveRange) attackRange = moveRange;
                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        DrawHelper.RenderRangeCircle(groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR);

                        debugStage = 2200;
                        TargetingHelper.DoForAllBuildingsWithinRangeTight(unit, attackRange, DataRefs.EvictionTag, delegate (ISimBuilding Building) {
                            MapItem item = Building.GetMapItem();
                            if (item == null) return false;

                            if (item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped) return false;
                            item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                            MapCell cell = item.ParentCell;
                            MapTile tile = item.ParentTile;
                            if (!cell.IsConsideredInCameraView) return false;
                            if (Building.MachineStructureInBuilding != null) return false;
                            if (tile.TileNetworkLevel.Display < TileNetLevel.Full) return false;

                            ModHelpers.DrawMapItemHighlightedBorder(item, DataRefs.EvictionVis.ColorHDR,
                                new Vector3(1.05f, 1.05f, 1.05f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            return false;
                        });

                        debugStage = 3200;
                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            else destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        }
                        if (unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost) {
                            unit.SetTargetingMode(null, null);
                            return;
                        }

                        debugStage = 4200;
                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                            destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity)
                            unit.RotateAndroidToFacePoint(destinationPoint);
                        else return;

                        debugStage = 5200;
                        bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;
                        bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null ||
                            (buildingUnderCursor?.GetMapItem()?.ParentTile?.TileNetworkLevel?.Display ?? TileNetLevel.None) < TileNetLevel.Full ||
                            !(buildingUnderCursor?.GetVariant()?.Tags?.ContainsKey(DataRefs.EvictionTag.ID) ?? false);
                        /*
                        ResourceType usedResType = 
                            FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped ? 
                            ResourceRefs.Wealth : ResourceRefs.DailyNecessities;
                        string usedResLang = 
                            FlagRefs.IsPostFinalDoom.DuringGameplay_IsTripped ? 
                            "MissingGoods" : "MissingFunds";
                        */
                        BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();
                        int baseCost = 1000;
                        float costMult = 1f;
                        int peopleTotal = 0;
                        int peopleResidents = 0;
                        int peopleWorkers = 0;
                        int legalIssues = 0;
                        int buildingVolume = 0;
                        int buildingStorage = 0;
                        int buildingFloorArea = 0;
                        int totalEvictionCost = 0;
                        if (!buildingIsInvalid) {
                            peopleResidents = buildingUnderCursor.GetTotalResidentCount();
                            peopleWorkers = buildingUnderCursor.GetTotalWorkerCount();
                            peopleTotal = peopleResidents + peopleWorkers;
                            float legalOverhead = 0;
                            foreach (var legalEntity in buildingUnderCursor.GetBuildingData()) {
                                legalIssues += legalEntity.Value;
                                switch (legalEntity.Key.ID) {
                                    case "MilitaryPresence": legalOverhead += legalEntity.Value * DataRefs.LEGAL_MILITARY; break;
                                    case "SecForcesPresence": legalOverhead += legalEntity.Value * DataRefs.LEGAL_SECURITY; break;
                                    case "CrimeSyndicatePresence": legalOverhead += legalEntity.Value * DataRefs.LEGAL_SYNDICATE; break;
                                    case "HackerPresence": legalOverhead += legalEntity.Value * DataRefs.LEGAL_HACKERS; break;
                                    case "HostileCultPresence": legalOverhead += legalEntity.Value * DataRefs.LEGAL_CULT; break;
                                    case "GangCrime": legalOverhead += legalEntity.Value * DataRefs.LEGAL_GANGS; break;
                                    case "BlackMarket": legalOverhead += legalEntity.Value * DataRefs.LEGAL_MARKET; break;
                                    case "Vermin": legalOverhead += legalEntity.Value * DataRefs.LEGAL_VERMIN; break;
                                    case "BacterialLoad": legalOverhead += legalEntity.Value * DataRefs.LEGAL_BACTERIA; break;
                                    default: legalOverhead += legalEntity.Value * 1f; break;
                                }
                            }
                            BuildingPrefab buildingPrefab = buildingUnderCursor.GetPrefab();
                            buildingVolume = buildingPrefab.NormalTotalBuildingVolumeFullDimensions;
                            buildingStorage = buildingPrefab.NormalTotalStorageVolumeFullDimensions;
                            buildingFloorArea = buildingPrefab.NormalTotalBuildingFloorAreaFullDimensions;
                            totalEvictionCost = (int)(costMult * (baseCost + peopleTotal + legalOverhead +
                            buildingVolume * DataRefs.EVICT_VOLUME_MULT +
                            buildingStorage * DataRefs.EVICT_STORAGE_MULT +
                            buildingFloorArea * DataRefs.EVICT_AREA_MULT));
                        }

                        debugStage = 6200;
                        if (!isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice(destinationPoint)) {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpot(true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(buildingUnderCursor), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.TitleUpperLeft.AddLang("Move_OutOfRange");
                                novel.Main_ExtraSizePerLine = 0f;

                                var costInfo = novel.Main.StartLineHeight10().AddRaw(variant.GetDisplayName()).Line();
                                if (peopleResidents > 0) costInfo.Line().AddFormat1("InfoResidents", peopleResidents.ToStringThousandsWhole());
                                if (peopleWorkers > 0) costInfo.Line().AddFormat1("InfoWorkers", peopleWorkers.ToStringThousandsWhole());
                                if (legalIssues > 0) costInfo.Line().AddFormat1("InfoLegalIssues", legalIssues.ToStringThousandsWhole());
                                if (buildingVolume > 0) costInfo.Line().AddFormat1("InfoVolume", buildingVolume.ToStringThousandsWhole());
                                if (buildingStorage > 0) costInfo.Line().AddFormat1("InfoStorage", buildingStorage.ToStringThousandsWhole());
                                if (buildingFloorArea > 0) costInfo.Line().AddFormat1("InfoArea", buildingFloorArea.ToStringThousandsWhole());
                                if (peopleResidents > 0 || peopleWorkers > 0 || legalIssues > 0 || buildingVolume > 0 || buildingStorage > 0 || buildingFloorArea > 0) costInfo.Line();
                                if (peopleTotal > 0) costInfo.Line().AddFormat1("ResultPeople", peopleTotal.ToStringThousandsWhole());
                                if (totalEvictionCost > 0) costInfo.Line().AddFormat1("ResultFunds", totalEvictionCost.ToStringThousandsWhole());
                                if (ResourceRefs.Wealth.Current < totalEvictionCost) costInfo.Line().AddLang("MissingFunds");
                                novel.Main.EndLineHeight();
                            }
                            return;
                        }

                        debugStage = 7200;
                        if (!isInRange || buildingIsInvalid) return;
                        else {
                            DrawHelper.RenderCatmullLine(unit.GetCollisionCenter(), destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpotWithColor(true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(buildingUnderCursor), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = Ability.Icon;
                                novel.TitleUpperLeft.AddRightClickFormat("Move_ClickToEvict");
                                novel.Main_ExtraSizePerLine = 0f;

                                var costInfo = novel.Main.StartLineHeight10().AddRaw(variant.GetDisplayName()).Line();
                                if (peopleResidents > 0) costInfo.Line().AddFormat1("InfoResidents", peopleResidents.ToStringThousandsWhole());
                                if (peopleWorkers > 0) costInfo.Line().AddFormat1("InfoWorkers", peopleWorkers.ToStringThousandsWhole());
                                if (legalIssues > 0) costInfo.Line().AddFormat1("InfoLegalIssues", legalIssues.ToStringThousandsWhole());
                                if (buildingVolume > 0) costInfo.Line().AddFormat1("InfoVolume", buildingVolume.ToStringThousandsWhole());
                                if (buildingStorage > 0) costInfo.Line().AddFormat1("InfoStorage", buildingStorage.ToStringThousandsWhole());
                                if (buildingFloorArea > 0) costInfo.Line().AddFormat1("InfoArea", buildingFloorArea.ToStringThousandsWhole());
                                if (peopleResidents > 0 || peopleWorkers > 0 || legalIssues > 0 || buildingVolume > 0 || buildingStorage > 0 || buildingFloorArea > 0) costInfo.Line();
                                if (peopleTotal > 0) costInfo.Line().AddFormat1("ResultPeople", peopleTotal.ToStringThousandsWhole());
                                if (totalEvictionCost > 0) costInfo.Line().AddFormat1("ResultFunds", totalEvictionCost.ToStringThousandsWhole());
                                if (ResourceRefs.Wealth.Current < totalEvictionCost) costInfo.Line().AddLang("MissingFunds");
                                novel.Main.EndLineHeight();
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.EvictionVis.ColorHDR,
                                new Vector3(1.08f, 1.08f, 1.08f), HighlightPass.AlwaysHappen, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            if (ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume()) {
                                unit.AlterCurrentActionPoints(-Ability.ActionPointCost);
                                ResourceRefs.MentalEnergy.AlterCurrent_Named(-Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange);
                                ResourceRefs.Wealth.AlterCurrent_Named(-totalEvictionCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange);

                                ProjectileInstructionPrePacket prePacket = ProjectileInstructionPrePacket.CreateVerySimple(center, destinationPoint.ReplaceY(groundLevel), unit, delegate {
                                    int debugStageInner = 0;
                                    try {
                                        debugStageInner = 100;
                                        buildingUnderCursor.KillEveryoneHere();

                                        debugStageInner = 200;
                                        buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                        buildingUnderCursor.FullyDeleteBuilding();

                                        debugStageInner = 300;
                                        if (peopleTotal > 0) {
                                            CityStatisticTable.AlterScore("CitizensForciblyRescuedFromSlums", peopleTotal);
                                            ResourceRefs.ShelteredHumans.AlterCurrent_Named(peopleTotal, string.Empty, ResourceAddRule.StoreExcess);
                                            ArcenNotes.SendSimpleNoteToGameOnly(300, NoteInstructionTable.Instance.GetRowByID("GainedResource"),
                                                NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleTotal, 0, 0, 0);
                                        }
                                    } catch (Exception e) {
                                        ArcenDebugging.LogDebugStageWithStack("EvictionAbilityHit", debugStageInner, e, Verbosity.ShowAsError);
                                    }
                                });
                                Ability.OnTargetedUse.DuringGame_PlayAtLocationWithTarget(buildingUnderCursor.GetMapItem().OBBCache.BottomCenter, prePacket, false);
                            }
                        }
                        break;
                    }
                    default: {
                        ArcenDebugging.LogSingleLine("BasicAndroidAbilities: Called HandleAbilityMixedTargeting for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError);
                        break;
                    }
                }
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicAndroidAbilities.HandleAbilityMixedTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
            }
        }

        public void HandleDataRefsPreload() {
            if (DataRefs.Eviction == null) DataRefs.Eviction = AbilityTypeTable.Instance.GetRowByID("Eviction");
            if (DataRefs.EvictionTag == null) DataRefs.EvictionTag = BuildingTagTable.Instance.GetRowByID("EvictionTarget");
            if (DataRefs.EvictionVis == null) DataRefs.EvictionVis = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
        }
    }
}

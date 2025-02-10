﻿using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.FFU.RoSAI {
    public class BasicConsumables : IResourceConsumableImplementation {
        public bool HandleConsumableHardTargeting(ISimMachineActor Actor, ResourceConsumable Consumable, Vector3 center, float attackRange, float moveRange) {
            if (Consumable == null || Actor == null) return false;
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
            DataRefs.BuildingValidEvictionTarget = VisColorUsageTable.Instance.GetRowByID("BuildingValidEvictionTarget");
            int debugStage = 0;

            try {
                debugStage = 100;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY(groundLevel);

                switch (Consumable.ID) {
                    case "EvictionDrone":
                        debugStage = 1200;
                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        DrawHelper.RenderRangeCircle(groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR);

                        debugStage = 2200;
                        TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera(Consumable.DirectUseConsumable.TargetsBuildingTag,
                            delegate (ISimBuilding Building) {
                                MapItem item = Building.GetMapItem();
                                if (item == null) return false;

                                if (item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped) return false;
                                item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                MapCell cell = item.ParentCell;
                                if (!cell.IsConsideredInCameraView) return false;
                                if (Building.MachineStructureInBuilding != null) return false;

                                ModHelpers.DrawMapItemHighlightedBorder(item, DataRefs.BuildingValidEvictionTarget.ColorHDR,
                                    new Vector3(1.05f, 1.05f, 1.05f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                                return false;
                            });

                        debugStage = 3200;
                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            if (buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey(Consumable.DirectUseConsumable.TargetsBuildingTag.ID)) buildingUnderCursor = null;
                        }

                        debugStage = 4200;
                        if (buildingUnderCursor != null) destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                            destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity) {
                        } else return false;

                        debugStage = 5200;
                        bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;
                        int peopleInBuilding = buildingUnderCursor == null ? 0 : buildingUnderCursor.GetTotalResidentCount() + buildingUnderCursor.GetTotalWorkerCount();
                        int legalOverhead = 0;
                        int legalIssues = 0;
                        if (buildingUnderCursor != null) {
                            float legalValue = 0;
                            foreach (var legalEntity in buildingUnderCursor.GetBuildingData()) {
                                legalIssues += legalEntity.Value;
                                switch (legalEntity.Key.ID) {
                                    case "MilitaryPresence": legalValue += legalEntity.Value * DataRefs.LEGAL_MILITARY; break;
                                    case "SecForcesPresence": legalValue += legalEntity.Value * DataRefs.LEGAL_SECURITY; break;
                                    case "CrimeSyndicatePresence": legalValue += legalEntity.Value * DataRefs.LEGAL_SYNDICATE; break;
                                    case "HackerPresence": legalValue += legalEntity.Value * DataRefs.LEGAL_HACKERS; break;
                                    case "HostileCultPresence": legalValue += legalEntity.Value * DataRefs.LEGAL_CULT; break;
                                    case "GangCrime": legalValue += legalEntity.Value * DataRefs.LEGAL_GANGS; break;
                                    case "BlackMarket": legalValue += legalEntity.Value * DataRefs.LEGAL_MARKET; break;
                                    case "Vermin": legalValue += legalEntity.Value * DataRefs.LEGAL_VERMIN; break;
                                    case "BacterialLoad": legalValue += legalEntity.Value * DataRefs.LEGAL_BACTERIA; break;
                                    default: legalValue += legalEntity.Value * 1f; break;
                                }
                            }
                            legalOverhead = (int)legalValue;
                        }
                        BuildingPrefab buildingPrefab = buildingUnderCursor?.GetPrefab();
                        int buildingVolume = buildingPrefab?.NormalTotalBuildingVolumeFullDimensions ?? 0;
                        int buildingStorage = buildingPrefab?.NormalTotalStorageVolumeFullDimensions ?? 0;
                        int buildingFloorArea = buildingPrefab?.NormalTotalBuildingFloorAreaFullDimensions ?? 0;
                        int totalEvictionCost = (int)(1000 +
                            peopleInBuilding + legalOverhead +
                            buildingVolume * DataRefs.EVICT_VOLUME +
                            buildingStorage * DataRefs.EVICT_STORAGE +
                            buildingFloorArea * DataRefs.EVICT_AREA);
                        BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();
                        bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                        debugStage = 6200;
                        if (!isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice(destinationPoint)) {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpot(true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f);
                            if (novel.TryStartSmallerTooltip(TooltipID.Create(Consumable), null, SideClamp.Any, TooltipNovelWidth.Simple)) {
                                novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.TitleUpperLeft.AddLang("Move_OutOfRange");
                                if (!buildingIsInvalid) {
                                    var costInfo = novel.Main.AddRaw(variant.GetDisplayName()).StartLineHeight50().Line();
                                    if (peopleInBuilding > 0) costInfo.AddFormat1("EvictionResidents", peopleInBuilding.ToStringThousandsWhole()).StartLineHeight10().Line();
                                    if (legalIssues > 0) costInfo.AddFormat1("EvictionLegalIssues", legalIssues.ToStringThousandsWhole()).StartLineHeight10().Line();
                                    if (buildingVolume > 0) costInfo.AddFormat1("EvictionVolume", buildingVolume.ToStringThousandsWhole()).StartLineHeight10().Line();
                                    if (buildingStorage > 0) costInfo.AddFormat1("EvictionStorage", buildingStorage.ToStringThousandsWhole()).StartLineHeight10().Line();
                                    if (buildingFloorArea > 0) costInfo.AddFormat1("EvictionArea", buildingFloorArea.ToStringThousandsWhole()).StartLineHeight50().Line();
                                    costInfo.AddFormat1("EvictionCostTotal", totalEvictionCost.ToStringThousandsWhole());
                                }
                            }
                            return false;
                        }

                        debugStage = 7200;
                        if (buildingIsInvalid) return false;
                        else {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpotWithColor(true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(Consumable), null, SideClamp.Any, TooltipNovelWidth.Simple)) {
                                novel.Icon = Consumable.Icon;
                                novel.TitleUpperLeft.AddFormat1("Move_ClickToEvict", Lang.GetRightClickText());
                                var costInfo = novel.Main.AddRaw(variant.GetDisplayName()).StartLineHeight50().Line();
                                if (peopleInBuilding > 0) costInfo.AddFormat1("EvictionResidents", peopleInBuilding.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (legalIssues > 0) costInfo.AddFormat1("EvictionLegalIssues", legalIssues.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingVolume > 0) costInfo.AddFormat1("EvictionVolume", buildingVolume.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingStorage > 0) costInfo.AddFormat1("EvictionStorage", buildingStorage.ToStringThousandsWhole()).StartLineHeight10().Line();
                                if (buildingFloorArea > 0) costInfo.AddFormat1("EvictionArea", buildingFloorArea.ToStringThousandsWhole()).StartLineHeight50().Line();
                                if (ResourceRefs.Wealth.Current < totalEvictionCost) {
                                    costInfo.AddFormat1("EvictionCostTotal", totalEvictionCost.ToStringThousandsWhole()).StartLineHeight50().Line();
                                    costInfo.AddFormat1("EvictionMissingFunds", string.Empty);
                                    return false;
                                } else costInfo.AddFormat1("EvictionCostTotal", totalEvictionCost.ToStringThousandsWhole());
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.BuildingValidEvictionTarget.ColorHDR,
                                new Vector3(1.08f, 1.08f, 1.08f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                            if (ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume()) {
                                Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;
                                Consumable.TryToDirectlyUseByActorAgainstTargetBuilding(Actor, buildingUnderCursor,
                                    delegate {
                                        int debugStageInner = 0;
                                        try {
                                            debugStageInner = 200;
                                            buildingUnderCursor.KillEveryoneHere();

                                            ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation(buildingUnderCursor.GetMapItem().OBBCache.BottomCenter,
                                                new Vector3(0, Engine_Universal.PermanentQualityRandom.Next(0, 360), 0));
                                            buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                            buildingUnderCursor.FullyDeleteBuilding();

                                            if (peopleInBuilding > 0) {
                                                CityStatisticTable.AlterScore("CitizensForciblyRescuedFromSlums", peopleInBuilding);
                                                ResourceRefs.ShelteredHumans.AlterCurrent_Named(peopleInBuilding, string.Empty, ResourceAddRule.StoreExcess);
                                                ArcenNotes.SendSimpleNoteToGameOnly(300, NoteInstructionTable.Instance.GetRowByID("GainedResource"),
                                                    NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0);
                                            }
                                        } catch (Exception e) {
                                            ArcenDebugging.LogDebugStageWithStack("DecrownerDronesHit", debugStageInner, e, Verbosity.ShowAsError);
                                        }
                                    });
                            }
                            return true;
                        }
                    default: {
                        return MachineMovePlannerImplementation.HandleNonBuildingConsumableTargetingModeForAnyMachineActor(Actor);
                    }
                }
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicConsumables.HandleConsumableHardTargeting", debugStage, Consumable?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
                return false;
            }
        }
    }
}

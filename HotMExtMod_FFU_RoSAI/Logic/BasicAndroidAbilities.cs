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
            int debugStage = 0;

            try {
                debugStage = 100;
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
            int debugStage = 0;
            try {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY(groundLevel);
                switch (Ability.ID) {
                    case "Eviction":
                        /*
                        debugStage = 1200;
                        if (attackRange < moveRange) attackRange = moveRange;

                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        DrawHelper.RenderRangeCircle(groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR);

                        TargetingHelper.DoForAllBuildingsWithinRangeTight(unit, attackRange, DataRefs.EvictionTarget,
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

                        if (unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost) {
                            unit.SetTargetingMode(null, null);
                            return;
                        }

                        debugStage = 3200;
                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            else destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        }

                        debugStage = 4200;
                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity)
                            unit.RotateAndroidToFacePoint(destinationPoint);
                        else return;

                        debugStage = 5200;
                        bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;
                        int peopleInBuilding = buildingUnderCursor == null ? 0 : buildingUnderCursor.GetTotalResidentCount() + buildingUnderCursor.GetTotalWorkerCount();
                        BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();
                        bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                        debugStage = 6200;
                        if (!isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice(destinationPoint)) {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint, Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpot(true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f);
                            if (novel.TryStartSmallerTooltip(TooltipID.Create(buildingUnderCursor), null, SideClamp.Any, TooltipNovelWidth.Simple)) {
                                novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                novel.ShouldTooltipBeRed = true;
                                novel.TitleUpperLeft.AddLang("Move_OutOfRange");
                                if (!buildingIsInvalid) novel.Main.AddRaw(variant.GetDisplayName()).AddFormat1("PeopleInsideCountParenthetical", peopleInBuilding.ToStringThousandsWhole());
                            }
                            return;
                        }

                        debugStage = 7200;
                        if (buildingIsInvalid || peopleInBuilding == 0) return;
                        else {
                            DrawHelper.RenderCatmullLine(Actor.GetCollisionCenter(), destinationPoint,ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpotWithColor(true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(buildingUnderCursor), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = Ability.Icon;
                                novel.TitleUpperLeft.AddRightClickFormat("Move_ClickToEvict");
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.BuildingValidEvictionTarget.ColorHDR, 
                                new Vector3(1.08f, 1.08f, 1.08f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);

                            if (ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume()) {
                                Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                //do the thing
                                ProjectileInstructionPrePacket prePacket = ProjectileInstructionPrePacket.CreateVerySimple(center, destinationPoint.ReplaceY(groundLevel),
                                    unit, delegate {
                                    int debugStageInner = 0;
                                    try {
                                        debugStageInner = 200;
                                        buildingUnderCursor.KillEveryoneHere();

                                        ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation(buildingUnderCursor.GetMapItem().OBBCache.BottomCenter,
                                            new Vector3(0, Engine_Universal.PermanentQualityRandom.Next(0, 360), 0));
                                        buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                        buildingUnderCursor.FullyDeleteBuilding();

                                        CityStatisticTable.AlterScore("CitizensForciblyRescuedFromSlums", peopleInBuilding);
                                        ResourceRefs.ShelteredHumans.AlterCurrent_Named(peopleInBuilding, string.Empty, ResourceAddRule.StoreExcess);
                                        ArcenNotes.SendSimpleNoteToGameOnly(300, NoteInstructionTable.Instance.GetRowByID("GainedResource"), 
                                            NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0);
                                    } catch (Exception e) {
                                        ArcenDebugging.LogDebugStageWithStack("DecrownerDronesHit", debugStageInner, e, Verbosity.ShowAsError);
                                    }
                                });
                            }
                            return;
                        }
                        */

                        /*
                        debugStage = 1200;
                        if (attackRange < moveRange) attackRange = moveRange;

                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        DrawHelper.RenderRangeCircle(groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR);

                        TargetingHelper.DoForAllBuildingsWithinRangeTight(unit, attackRange, DataRefs.EvictionTarget,
                            delegate (ISimBuilding Building) {
                                MapItem item = Building.GetMapItem();
                                if (item == null) return false;

                                if (item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped) return false;
                                item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                MapCell cell = item.ParentCell;
                                if (!cell.IsConsideredInCameraView) return false;
                                if (Building.MachineStructureInBuilding != null) return false;

                                ModHelpers.DrawMapItemHighlightedBorder(item, DataRefs.BuildingValidEvictionTarget.ColorHDR,
                                    new Vector3(1.08f, 1.08f, 1.08f), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);
                                return false;
                            });

                        if (unit.CurrentActionPoints < Ability.ActionPointCost || ResourceRefs.MentalEnergy.Current < Ability.MentalEnergyCost) {
                            unit.SetTargetingMode(null, null);
                            return;
                        }

                        Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                        ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor;
                        if (buildingUnderCursor != null) {
                            BuildingStatus status = buildingUnderCursor.GetStatus();
                            if (status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually)) buildingUnderCursor = null;
                            else destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;
                        }

                        if (!Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                            destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity)
                            unit.RotateAndroidToFacePoint(destinationPoint);
                        else return;

                        bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;
                        if (!isInRange) return;

                        if (buildingUnderCursor == null || !(buildingUnderCursor?.GetVariant()?.Tags?.ContainsKey(DataRefs.EvictionTarget.ID) ?? false) ||
                            buildingUnderCursor.MachineStructureInBuilding != null || buildingUnderCursor.CurrentOccupyingUnit != null) {
                            return;
                        } else {
                            DrawHelper.RenderCatmullLine(unit.GetCollisionCenter(), destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting);
                            CursorHelper.RenderSpecificMouseCursorAtSpotWithColor(true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR);

                            if (novel.TryStartSmallerTooltip(TooltipID.Create(buildingUnderCursor), null, SideClamp.Any, TooltipNovelWidth.Smaller)) {
                                novel.Icon = Ability.Icon;
                                novel.TitleUpperLeft.AddRightClickFormat("Move_ClickToEvict");
                            }

                            ModHelpers.DrawMapItemHighlightedBorder(buildingUnderCursor.GetMapItem(), DataRefs.BuildingValidEvictionTarget.ColorHDR,
                                new Vector3(1.1f, 1.1f, 1.1f), HighlightPass.AlwaysHappen, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped);

                            if (ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume()) {
                                unit.ApplyVisibilityFromAction(ActionVisibility.IsAttack);
                                unit.AddOrRemoveBadge(CommonRefs.MarkedDefective, true);
                                unit.AlterCurrentActionPoints(-Ability.ActionPointCost);
                                ResourceRefs.MentalEnergy.AlterCurrent_Named(-Ability.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange);
                                ProjectileInstructionPrePacket prePacket = ProjectileInstructionPrePacket.CreateVerySimple(center, destinationPoint.ReplaceY(groundLevel), unit, delegate {
                                    int residentCount = buildingUnderCursor.GetTotalResidentCount() + buildingUnderCursor.GetTotalWorkerCount();
                                    if (residentCount > 0) {
                                        CityStatisticTable.AlterScore("CitizensForciblyRescuedFromSlums", residentCount);
                                        ResourceRefs.ShelteredHumans.AlterCurrent_Named(residentCount, string.Empty, ResourceAddRule.StoreExcess);
                                        ArcenNotes.SendSimpleNoteToGameOnly(300, NoteInstructionTable.Instance.GetRowByID("GainedResource"),
                                            NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, residentCount, 0, 0, 0);
                                    }
                                    BuildingTypeVariant variant = buildingUnderCursor.GetVariant();
                                    buildingUnderCursor.KillEveryoneHere();
                                    ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation(buildingUnderCursor.GetMapItem().OBBCache.BottomCenter,
                                            new Vector3(0, Engine_Universal.PermanentQualityRandom.Next(0, 360), 0));
                                    buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                    buildingUnderCursor.FullyDeleteBuilding();
                                });

                                Ability.OnTargetedUse.DuringGame_PlayAtLocationWithTarget(center, prePacket, false);
                                if (!InputCaching.ShouldKeepDoingAction) unit.SetTargetingMode(null, null);
                            }
                        }
                        */
                        break;
                    default: {
                        ArcenDebugging.LogSingleLine("BasicAndroidAbilities: Called HandleAbilityMixedTargeting for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError);
                        break;
                    }
                }
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicAndroidAbilities.HandleAbilityMixedTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
            }
        }
    }
}

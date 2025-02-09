using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.FFU.RoSAI
{
    public class BasicAndroidAbilities : IAbilityImplementation
    {
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
            } catch (Exception e) {
                ArcenDebugging.LogDebugStageWithStack("BasicAndroidAbilities.HandleAbilityMixedTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError);
            }
        }
    }
}

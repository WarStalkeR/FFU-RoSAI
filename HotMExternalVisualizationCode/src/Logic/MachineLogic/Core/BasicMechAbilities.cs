using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class BasicMechAbilities : IAbilityImplementation
    {
        public ActorAbilityResult TryHandleAbility( ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            if ( Ability == null || Actor == null )
                return ActorAbilityResult.PlayErrorSound;

            ISimMachineUnit unit = Actor as ISimMachineUnit;
            if ( unit == null )
            {
                if ( Actor is ISimMachineVehicle vehicle )
                    ArcenDebugging.LogSingleLine( "BasicMechAbilities: Called HandleAbility for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError );
                return ActorAbilityResult.PlayErrorSound;
            }

            if ( Ability.MustBeTargeted )
            {
                switch ( Logic )
                {
                    default:
                        return ActorAbilityResult.OpenedInterface;
                    case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                    case ActorAbilityLogic.ExecuteAbilityAutomated:
                    case ActorAbilityLogic.TriggerAbilityAltView:
                        {
                            if ( Actor.IsInAbilityTypeTargetingMode == Ability )
                                Actor.SetTargetingMode( null, null );
                            else
                                Actor.SetTargetingMode( Ability, null );
                            return ActorAbilityResult.OpenedInterface;
                        }
                }
            }

            switch ( Ability.ID )
            {
                case "MechStandby":
                    return BasicActionsHelper.FullHandleStandby( Actor, Ability, BufferOrNull, Logic );
                case "MechBattleRecharge":
                    return BasicActionsHelper.FullHandleBattleRecharge( Actor, Ability, BufferOrNull, Logic );                
                case "MechShieldsUp":
                    #region MechShieldsUp
                    {
                        switch ( Logic )
                        {
                            case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                {
                                    if ( unit.GetHasBadge( Ability.RelatedBadge ) )
                                    {
                                        if ( BufferOrNull != null )
                                            BufferOrNull.AddLang( "UnitAlreadyHasShieldsUp" );
                                        return ActorAbilityResult.PlayErrorSound;
                                    }
                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                            case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                            case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                                {

                                }
                                break;
                            case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                            case ActorAbilityLogic.ExecuteAbilityAutomated:
                                if ( !unit.GetIsBlockedFromActingForAbility( Ability, true ) && !unit.GetHasBadge( Ability.RelatedBadge ) )
                                {
                                    unit.AddOrRemoveBadge( Ability.RelatedBadge, true );

                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                                break;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                #endregion
                case "MechRepairNearby":
                    return BasicActionsHelper.FullHandleRepairNearby( Actor, Ability, BufferOrNull, Logic );
                case "MechUseConsumable":
                    return BasicActionsHelper.HandleConsumablesAbility( Actor, Ability, BufferOrNull, Logic );
                case "MechMercurialForm":
                    #region MechMercurialForm
                    {
                        switch ( Logic )
                        {
                            case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                {
                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                            case ActorAbilityLogic.AppendToAbilityTooltip_Full:
                            case ActorAbilityLogic.AppendToAbilityTooltip_ForPossibleUnit:
                                {

                                }
                                break;
                            case ActorAbilityLogic.ExecuteAbilityFromPlayerDirect:
                            case ActorAbilityLogic.ExecuteAbilityAutomated:
                                if ( !unit.GetIsBlockedFromActingForAbility( Ability, true ) )
                                {
                                    unit.AddOrRemoveBadge( Ability.RelatedBadge, !unit.GetHasBadge( Ability.RelatedBadge ) );
                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                                break;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "BasicMechAbilities: Called HandleAbility for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return ActorAbilityResult.PlayErrorSound;
            }
        }

        public void HandleAbilityHardTargeting( ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange )
        {
        }

        public void HandleAbilityMixedTargeting( ISimMachineActor Actor, AbilityType Ability, Vector3 center, float attackRange, float moveRange )
        {
            if ( Ability == null || Actor == null )
                return;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                ISimMachineUnit unit = Actor as ISimMachineUnit;
                if ( unit == null )
                {
                    debugStage = 200;
                    if ( Actor is ISimMachineVehicle vehicle )
                        ArcenDebugging.LogSingleLine( "BasicMechAbilities: Called HandleAbilityHardTargeting for '" + Ability.ID + "' with a vehicle instead of a unit!", Verbosity.ShowAsError );
                    return;
                }


                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );

                switch ( Ability.ID )
                {
                    case "Later":
                        #region Later
                        {
                            debugStage = 1200;
                        }
                        break;
                        #endregion
                    default:
                        ArcenDebugging.LogSingleLine( "BasicMechAbilities: Called HandleAbilityHardTargeting for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BasicMechAbilities.HandleAbilityHardTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError );
            }
        }
    }
}

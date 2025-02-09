using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using UnityEngine.Experimental.GlobalIllumination;

namespace Arcen.HotM.ExternalVis
{
    public class BasicVehicleAbilities : IAbilityImplementation
    {
        public ActorAbilityResult TryHandleAbility( ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation, AbilityType Ability,
            ArcenCharacterBufferBase BufferOrNull, ActorAbilityLogic Logic )
        {
            if ( Ability == null || Actor == null )
                return ActorAbilityResult.PlayErrorSound;

            ISimMachineVehicle vehicle = Actor as ISimMachineVehicle;
            if ( vehicle == null )
            {
                if ( Actor is ISimMachineUnit unit )
                    ArcenDebugging.LogSingleLine( "BasicVehicleAbilities: Called HandleAbility for '" + Ability.ID + "' with a unit instead of a vehicle!", Verbosity.ShowAsError );
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
                case "VehicleStandby":
                    return BasicActionsHelper.FullHandleStandby( Actor, Ability, BufferOrNull, Logic );
                case "VehicleBattleRecharge":
                    return BasicActionsHelper.FullHandleBattleRecharge( Actor, Ability, BufferOrNull, Logic );
                case "VehicleShieldsUp":
                    #region VehicleShieldsUp
                    {
                        switch ( Logic )
                        {
                            case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                {
                                    if ( vehicle.GetHasBadge( Ability.RelatedBadge ) )
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
                                if ( !vehicle.GetIsBlockedFromActingForAbility( Ability, true ) && !vehicle.GetHasBadge( Ability.RelatedBadge ) )
                                {
                                    vehicle.AddOrRemoveBadge( Ability.RelatedBadge, true );

                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                                break;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                #endregion
                case "VehicleCloak":
                    #region VehicleCloak
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
                                if ( !vehicle.GetIsBlockedFromActingForAbility( Ability, true ) )
                                {
                                    vehicle.AddOrRemoveBadge( Ability.RelatedBadge, !vehicle.GetHasBadge( Ability.RelatedBadge ) );

                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                                break;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                #endregion
                case "VehicleRepairNearby":
                    return BasicActionsHelper.FullHandleRepairNearby( Actor, Ability, BufferOrNull, Logic );
                case "VehicleUseConsumable":
                    return BasicActionsHelper.HandleConsumablesAbility( Actor, Ability, BufferOrNull, Logic );
                case "VehicleUnload":
                    #region VehicleUnload
                    {
                        switch ( Logic )
                        {
                            case ActorAbilityLogic.CalculateIfAbilityBlocked:
                                {
                                    if ( vehicle.GetStoredUnits().Count == 0 )
                                    {
                                        if ( BufferOrNull != null )
                                            BufferOrNull.AddLang( "VehicleHasNoPassengers" );
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
                                if ( !vehicle.GetIsBlockedFromActingForAbility( Ability, false ) )
                                {
                                    if ( Window_VehicleUnitPanel.Instance.IsOpen && Window_VehicleUnitPanel.VehicleToShow == vehicle )
                                        Window_VehicleUnitPanel.Instance.Close( WindowCloseReason.UserDirectRequest );
                                    else
                                    {
                                        Window_VehicleUnitPanel.VehicleToShow = vehicle;
                                        Window_VehicleUnitPanel.Instance.Open();
                                    }

                                    return ActorAbilityResult.SuccessDidFullAbilityNow;
                                }
                                break;
                        }
                        return ActorAbilityResult.PlayErrorSound;
                    }
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "BasicVehicleAbilities: Called HandleAbility for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
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
                ISimMachineVehicle vehicle = Actor as ISimMachineVehicle;
                if ( vehicle == null )
                {
                    debugStage = 200;
                    if ( Actor is ISimMachineUnit unit )
                        ArcenDebugging.LogSingleLine( "BasicVehicleAbilities: Called HandleAbilityHardTargeting for '" + Ability.ID + "' with a unit instead of a vehicle!", Verbosity.ShowAsError );
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
                        ArcenDebugging.LogSingleLine( "BasicVehicleAbilities: Called HandleAbilityMixedTargeting for '" + Ability.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BasicVehicleAbilities.HandleAbilityMixedTargeting", debugStage, Ability?.ID ?? "[null-ability]", e, Verbosity.ShowAsError );
            }
        }
    }
}

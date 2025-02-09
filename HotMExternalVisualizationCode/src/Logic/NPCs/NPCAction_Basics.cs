using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class NPCAction_Basics : INPCUnitActionImplementation
    {
        public bool TryHandleActionLogicForNPCUnit( ISimNPCUnit Unit, NPCActionConsideration Consideration, NPCUnitAction Action, MersenneTwister Rand, NPCUnitActionLogic Logic, NPCTimingData TimingDataOrNull )
        {
            if ( Action == null || Unit == null )
                return false;

            NPCUnitStance stance = Unit.Stance;
            bool stayInDistrict = stance?.IsContainedToDistrict??false;
            bool stayInPOI = stance?.IsContainedToPOI??false;

            switch ( Action.ID )
            {
                case "Wander":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to wander if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryWander( Unit, Rand, stayInDistrict, stayInPOI );
                            TimingDataOrNull.WanderTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryWander( Unit, Rand, stayInDistrict, stayInPOI ) )
                                return true;
                        }
                    }
                    break;
                case "ReturnToHomePOIIfNotInIt":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryMoveTowardHomePOIIfNeeded( Unit, Rand );
                            TimingDataOrNull.ReturnToHomePOITicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryMoveTowardHomePOIIfNeeded( Unit, Rand ) )
                                return true;
                        }

                    }
                    break;
                case "ReturnToHomeDistrictIfNotInIt":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryMoveTowardHomeDistrictIfNeeded( Unit, Rand );
                            TimingDataOrNull.ReturnToHomeDistrictTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryMoveTowardHomeDistrictIfNeeded( Unit, Rand ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterOutcastThenConspicuousUnits":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                        {
                            return false; //do not try run after units if has a current or next objective!
                        }

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterOutcastThenConspicuousUnits( Unit, Rand, Logic, 1 );
                            TimingDataOrNull.RunAfterOutcastThenConspicuousTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterOutcastThenConspicuousUnits( Unit, Rand, Logic, 1 ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterMachineStructures":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterNearestMachineStructureThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic );
                            TimingDataOrNull.RunAfterMachineStructuresTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterNearestMachineStructureThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterAnyEnemiesDoingActionOverTime":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, false, TimingDataOrNull );
                            TimingDataOrNull.RunAfterAggroedUnitsTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, false, TimingDataOrNull ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterEnemiesWeCanShootDoingActionOverTime":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, true, TimingDataOrNull );
                            TimingDataOrNull.RunAfterAggroedUnitsTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, true, TimingDataOrNull ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterAnyAggroedUnits":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, false, TimingDataOrNull );
                            TimingDataOrNull.RunAfterAggroedUnitsTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, false, TimingDataOrNull ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterWatchedBySecForce":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks; 
                            bool result = NPCActionHelper.TryRunAfterUnitsWithBadge( Unit, Rand, Logic, CommonRefs.WatchedBySecForce, false, TimingDataOrNull );
                            TimingDataOrNull.RunAfterAggroedUnitsTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, false, TimingDataOrNull ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterAggroedUnitsWeCanShoot":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, true, TimingDataOrNull );
                            TimingDataOrNull.RunAfterAggroedUnitsTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterAggroedUnits( Unit, Rand, Logic, true, TimingDataOrNull ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterNearestActorThatICanAutoShoot_WithinRangeOfCell":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!
                        if ( stayInPOI )
                        {
                            if ( Unit.HomePOI?.IsPOIAlarmed_Any ?? false )
                                stayInPOI = false; //if the poi's alarm is on
                        }

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false );
                            TimingDataOrNull.RunAfterNearestActorThatICanAutoShootTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterNearestNonPlayerActorThatICanAutoShoot_WithinRangeOfCell":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!
                        if ( stayInPOI )
                        {
                            if ( Unit.HomePOI?.IsPOIAlarmed_Any ?? false )
                                stayInPOI = false; //if the poi's alarm is on
                        }

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            
                            bool result = NPCActionHelper.TryRunAfterNearestNonPlayerActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false );
                            TimingDataOrNull.RunAfterNearestActorThatICanAutoShootTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterNearestNonPlayerActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false ) )
                                return true;
                        }
                    }
                    break;
                case "RunAfterNearestActorThatICanAutoShoot_Anywhere":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!
                        if ( stayInPOI )
                        {
                            if ( Unit.HomePOI?.IsPOIAlarmed_Any ?? false )
                                stayInPOI = false; //if the poi's alarm is on
                        }

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, true );
                            TimingDataOrNull.RunAfterNearestActorThatICanAutoShootTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, true ) )
                                return true;
                        }
                    }
                    break;
                case "FollowEvenHiddenPlayerUnits":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!
                        if ( stayInPOI )
                        {
                            if ( Unit.HomePOI?.IsPOIAlarmed_Any ?? false )
                                stayInPOI = false; //if the poi's alarm is on
                        }

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.FollowEvenHiddenPlayerUnits( Unit, Rand,  Logic );
                            TimingDataOrNull.FollowEvenHiddenPlayerUnits += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.FollowEvenHiddenPlayerUnits( Unit, Rand, Logic ) )
                                return true;
                        }
                    }
                    break;                    
                case "RunAfterThreatsNearMission":
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try run after units if has a current or next objective!

                        ISimBuilding missionStartBuilding = Unit.ManagerStartLocation.Get();
                        NPCManager manager = Unit.ParentManager;

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result;
                            if ( manager?.PeriodicData != null && missionStartBuilding != null ) //we have a mission and a location, so use that
                                result = NPCActionHelper.TryRunAfterNearestActorThatICanAutoShootWithinRangeOfBuilding( Unit, Rand, missionStartBuilding, manager.PeriodicData.ThreatRadius, Logic );
                            else
                                result = NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false );
                            TimingDataOrNull.RunAfterThreatsNearMissionTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( manager?.PeriodicData != null && missionStartBuilding != null )
                            {
                                //we have a mission and a location, so use that
                                if ( NPCActionHelper.TryRunAfterNearestActorThatICanAutoShootWithinRangeOfBuilding( Unit, Rand, missionStartBuilding, manager.PeriodicData.ThreatRadius, Logic ) )
                                    return true;
                            }
                            else
                            {
                                if ( NPCActionHelper.TryRunAfterNearestActorThatICanAutoShoot( Unit, Rand, stayInDistrict, stayInPOI, Logic, false ) )
                                    return true;
                            }
                        }
                        
                    }
                    break;
                case "ReturnToManagerStartAreaIfAwayFromIt":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        ISimBuilding missionStartBuilding = Unit.ManagerStartLocation.Get();
                        NPCManager manager = Unit.ParentManager;
                        if ( manager?.PeriodicData != null && missionStartBuilding != null )
                        {
                            if ( TimingDataOrNull != null )
                            {
                                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                                bool result = NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, missionStartBuilding, manager.PeriodicData.HomeRadius );
                                TimingDataOrNull.ReturnToManagerStartAreaTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                                if ( result )
                                    return true; //we did an action, hooray!
                            }
                            else
                            {
                                if ( NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, missionStartBuilding, manager.PeriodicData.HomeRadius ) )
                                    return true;
                            }
                        }
                    }
                    break;
                case "ReturnToMissionAreaIfAwayFromIt":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        NPCManager manager = Unit.ParentManager;
                        NPCMission mission = manager?.PeriodicData?.GateByCity?.RequiredNPCMissionActive;
                        if ( mission != null )
                        {
                            ISimBuilding projectStartBuilding = mission?.DuringGameplay_StartedAtBuilding;
                            if ( mission != null && projectStartBuilding != null )
                            {
                                if ( TimingDataOrNull != null )
                                {
                                    long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                                    bool result = NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, projectStartBuilding, manager.PeriodicData.HomeRadius );
                                    TimingDataOrNull.ReturnToMissionAreaTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                                    if ( result )
                                        return true; //we did an action, hooray!
                                }
                                else
                                {
                                    if ( NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, projectStartBuilding, manager.PeriodicData.HomeRadius ) )
                                        return true;
                                }
                            }
                        }
                        else
                        {
                            ISimBuilding missionStartBuilding = Unit.ManagerStartLocation.Get();
                            if ( manager?.PeriodicData != null && missionStartBuilding != null )
                            {
                                if ( TimingDataOrNull != null )
                                {
                                    long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                                    bool result = NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, missionStartBuilding, manager.PeriodicData.HomeRadius );
                                    TimingDataOrNull.ReturnToMissionAreaTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                                    if ( result )
                                        return true; //we did an action, hooray!
                                }
                                else
                                {
                                    if ( NPCActionHelper.TryMoveTowardRadiusFromBuildingIfNeeded( Unit, Rand, missionStartBuilding, manager.PeriodicData.HomeRadius ) )
                                        return true;
                                }
                            }
                        }
                    }
                    break;
                case "GetCloserToFocusIfPossible":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        ISimBuilding focusBuilding = Unit.ManagerStartLocation.Get();
                        if ( focusBuilding == null ) 
                            return false; //do not try to go to the focus if one no specified

                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryMoveTowardPosition( Unit, focusBuilding.GetPositionForCameraFocus() );
                            TimingDataOrNull.ReturnToManagerStartAreaTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryMoveTowardPosition( Unit, focusBuilding.GetPositionForCameraFocus() ) )
                                return true;
                        }
                    }
                    break;
                case "ReturnToCreationPosition":
                case "ReturnToCreationPositionAndThenDespawn":
                    if ( Logic == NPCUnitActionLogic.PlanningPerTurn ) //the execution is centrally handled, don't worry about that
                    {
                        if ( Unit.CurrentObjective != null || Unit.NextObjective != null )
                            return false; //do not try to go home if has a current or next objective!

                        Vector3 creationPosition = Unit.CreationPosition;
                        if ( TimingDataOrNull != null )
                        {
                            long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                            bool result = NPCActionHelper.TryMoveTowardPosition( Unit, creationPosition );
                            TimingDataOrNull.ReturnToManagerStartAreaTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                            if ( result )
                                return true; //we did an action, hooray!
                        }
                        else
                        {
                            if ( NPCActionHelper.TryMoveTowardPosition( Unit, creationPosition ) )
                                return true;
                        }

                        //if we got here, then essentially we are to the place
                        switch ( Action.ID )
                        {
                            case "ReturnToCreationPositionAndThenDespawn":
                                Consideration?.DoPopupTextOnDisbandIfNeeded_FromTurnLogicThread( Unit );
                                Unit.DisbandAsSoonAsNotSelected = true;
                                break;
                        }
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCAction_Basics: Called HandleActionLogicForNPCUnit for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
            return false;
        }
    }
}

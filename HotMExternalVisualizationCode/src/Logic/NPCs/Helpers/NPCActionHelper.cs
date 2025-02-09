using System;

using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{    
    public static class NPCActionHelper
    {
        #region GetShouldNPCSilentlyDoThisMove
        public static bool GetShouldNPCSilentlyDoThisMove( ISimNPCUnit Unit, ISimUnitLocation TargetLoc, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                Unit.TurnDumpLines.Add( "GetShouldNPCSilentlyDoThisMove" );

            if ( !TargetLoc.GetIsLocationInFogOfWar() || !Unit.GetIsInFogOfWar() )
            {
                if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                    Unit.TurnDumpLines.Add( "GetShouldNPCSilentlyDoThisMove " + World.Forces.GetMachineUnitsDeployed().Count + " + " + World.Forces.GetMachineVehiclesByID().Count );

                float myAttackRangeSquared = Unit.GetAttackRangeSquared();
                Vector3 targetLoc = TargetLoc.GetEffectiveWorldLocationForContainedUnit();
                bool wouldBeInRangeOfAnyUnitsOrVehicles = false;
                foreach ( ISimMachineUnit machineUnit in World.Forces.GetMachineUnitsDeployed() )
                {
                    if ( (machineUnit.GetDrawLocation() - targetLoc).GetSquareGroundMagnitude() <= myAttackRangeSquared )
                    {
                        wouldBeInRangeOfAnyUnitsOrVehicles = true;
                        break;
                    }
                }
                foreach ( KeyValuePair<int, ISimMachineVehicle> kv in World.Forces.GetMachineVehiclesByID() )
                {
                    ISimMachineVehicle machineVehicle = kv.Value;
                    if ( (machineVehicle.GetDrawLocation() - targetLoc).GetSquareGroundMagnitude() <= myAttackRangeSquared )
                    {
                        wouldBeInRangeOfAnyUnitsOrVehicles = true;
                        break;
                    }
                }
                return !wouldBeInRangeOfAnyUnitsOrVehicles;
            }
            return true;
        }
        #endregion

        #region TryHandleReallyCoreBits
        public static bool TryHandleReallyCoreBits( ISimNPCUnit Unit, NPCUnitStance Stance, NPCUnitStanceLogic Logic, 
            MersenneTwister Rand, ArcenCharacterBufferBase BufferOrNull )
        {
            switch ( Logic )
            {
                case NPCUnitStanceLogic.ChecksForNPCAfterMove:
                    {
                        if ( CheckIfInRangeOfNextObjectiveAndStartItIfSo( Unit ) )
                        {
                            NPCUnitObjective currentObjective = Unit.CurrentObjective;
                            if ( currentObjective != null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "NAH-CheckAfterMove-StartNewObjectiveLogic\n";
                                //note: this does NOT cost the unit its action.  So it can shoot at things in range, probably.
                                currentObjective.Implementation.DoCurrentObjectiveLogicForNPCUnit( Unit, Rand );
                            }
                        }
                    }
                    return true;
                case NPCUnitStanceLogic.PerTurnLogic:
                    {
                        #region ReinforcementChance
                        if ( Stance.ReinforcementChance > 0 )
                        {
                            if ( Stance.ReinforcementChance >= 100 || Rand.Next( 0, 100 ) < Stance.ReinforcementChance )
                            {
                                //doing the reinforce action!
                                Unit.WantsToPerformAction = NPCActionDesire.CustomStance;
                                Unit.SpecialtyActionStringToBeDoneNext = "Reinforce";                                
                                return true;
                            }
                        }
                        #endregion

                        #region DisbandChance
                        if ( Stance.DisbandChance > 0 )
                        {
                            if ( Stance.DisbandChance >= 100 || Rand.Next( 0, 100 ) < Stance.DisbandChance )
                            {
                                //doing the disband action!
                                Unit.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                                return true;
                            }
                        }
                        #endregion

                        if ( Stance.DisbandAfterXConsecutiveTurnsInThisStance > 0 && Unit.TurnsInCurrentStance >= Stance.DisbandAfterXConsecutiveTurnsInThisStance )
                        {
                            Unit.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                            return true;
                        }
                    }
                    break;
                case NPCUnitStanceLogic.CheckForSpecialtyNPCAction:
                    switch ( Unit.SpecialtyActionStringToBeDoneNext )
                    {
                        case "Reinforce":
                            {
                                Unit.Stance = Stance.SwitchToStanceWhenReinforcing;
                                NPCUnitType newUnitType = Stance.ReinforcementTag?.NPCUnitTypesList?.GetRandom( Rand );
                                ISimNPCUnit newUnit = null;
                                if ( newUnitType != null )
                                {
                                    newUnit = Unit.TryCreateNewNPCUnitAsCloseAsPossibleToThisOne( newUnitType, Unit.FromCohort,
                                        Stance.SwitchToStanceWhenReinforcing, -1f, Vector3.negativeInfinity, -1, true,
                                        Unit.ContainerLocation.Get()?.CalculateLocationSecurityClearanceInt()?? 2, CellRange.CellAndAdjacent2x, Rand, CollisionRule.Strict, "Reinforce" );

                                    Vector3 startLocation = Unit.GetPositionForCollisions();
                                    Vector3 endLocation = startLocation.PlusY( Unit.GetHalfHeightForCollisions() + 0.2f );
                                    ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBuffer();
                                    buffer.AddLang( "CalledReinforcements", IconRefs.ReinforcementsCalled.DefaultColorHexWithHDRHex );
                                    DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                        startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                                    newDamageText.PhysicalDamageIncluded = 0;
                                    newDamageText.MoraleDamageIncluded = 0;
                                    newDamageText.SquadDeathsIncluded = 0;
                                }
                                if ( Stance.DisbandWhenCallingReinforcements )
                                    Unit.DisbandAtTheStartOfNextTurn = true; //do this next turn so that the camera and such is not crazy in the meantime.
                            }
                            return true;
                    }
                    break;
                case NPCUnitStanceLogic.TooltipAddendum:
                    if ( Stance.DisbandAfterXConsecutiveTurnsInThisStance > 0 )
                    {
                        int turnsUntilDisband = MathA.Max( 0, Stance.DisbandAfterXConsecutiveTurnsInThisStance + 1 - Unit.TurnsInCurrentStance );
                        BufferOrNull.Line().AddLangAndAfterLineItemHeader( "UnitWillDisperseInTurns", ColorTheme.CyanDim )
                            .AddRaw( turnsUntilDisband.ToStringWholeBasic() );
                    }
                    break; //do not return true because more may be added
            }
            return false;
        }
        #endregion

        #region TryRunAfterOutcastThenConspicuousUnits
        public static bool TryRunAfterOutcastThenConspicuousUnits( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitActionLogic Logic, int OutcastLevelMustBeAtLeast )
        {
            ISimMachineActor nearestHuntedUnit = TargetingHelper.FindNearestOutcastMachineUnit( Unit, Unit.GetDrawLocation(), Logic, OutcastLevelMustBeAtLeast );
            if ( nearestHuntedUnit == null )
                nearestHuntedUnit = TargetingHelper.FindNearestConspicuousMachineActor( Unit, Unit.GetDrawLocation(), true, Unit.GetMovementRangeSquared(), Logic );
            if ( nearestHuntedUnit == null )
                return false;

            //if somebody to hunt, give chase!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestHuntedUnit, false, false, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );

                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            Unit.TargetActor = nearestHuntedUnit;
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }                
            }
            return true;
        }
        #endregion

        #region TryRunAfterAggroedUnits
        public static bool TryRunAfterAggroedUnits( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitActionLogic Logic, bool MustBeAbleToShootThem, NPCTimingData TimingDataOrNull )
        {
            ISimMapActor nearestAggroedUnit;
            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                nearestAggroedUnit = TargetingHelper.FindNearestAggroedUnit( Unit, false, false, false, MustBeAbleToShootThem, Logic );
                TimingDataOrNull.FindNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                nearestAggroedUnit = TargetingHelper.FindNearestAggroedUnit( Unit, false, false, false, MustBeAbleToShootThem, Logic );

            if ( nearestAggroedUnit == null )
                return false;

            //if somebody to hunt, give chase!
            ISimUnitLocation unitLoc;

            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );
                TimingDataOrNull.ChaseNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );

            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );

                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            Unit.TargetActor = nearestAggroedUnit;
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunAfterUnitsWithBadge
        public static bool TryRunAfterUnitsWithBadge( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitActionLogic Logic, ActorBadge Badge, bool MustBeAbleToShootThem, NPCTimingData TimingDataOrNull )
        {
            ISimMapActor nearestAggroedUnit;
            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                nearestAggroedUnit = TargetingHelper.FindNearestUnitWithBadge( Unit, Badge, false, false, false, MustBeAbleToShootThem, Logic );
                TimingDataOrNull.FindNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                nearestAggroedUnit = TargetingHelper.FindNearestUnitWithBadge( Unit, Badge, false, false, false, MustBeAbleToShootThem, Logic );

            if ( nearestAggroedUnit == null )
                return false;

            //if somebody to hunt, give chase!
            ISimUnitLocation unitLoc;

            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );
                TimingDataOrNull.ChaseNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );

            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );

                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            Unit.TargetActor = nearestAggroedUnit;
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunAfterEnemiesDoingActionOverTime
        public static bool TryRunAfterEnemiesDoingActionOverTime( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitActionLogic Logic, NPCTimingData TimingDataOrNull )
        {
            ISimMapActor nearestAggroedUnit;
            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                TargetingHelper.FindNearestActorThatICanAutoShootWhoIsDoingAnActionOverTime( Unit, false, false, false, out _, out nearestAggroedUnit, Logic );
                TimingDataOrNull.FindNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                TargetingHelper.FindNearestActorThatICanAutoShootWhoIsDoingAnActionOverTime( Unit, false, false, false, out _, out nearestAggroedUnit, Logic );

            if ( nearestAggroedUnit == null )
                return false;

            //if somebody to hunt, give chase!
            ISimUnitLocation unitLoc;

            if ( TimingDataOrNull != null )
            {
                long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );
                TimingDataOrNull.ChaseNearestAggroedUnitTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
            }
            else
                unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, nearestAggroedUnit, false, false, Logic );

            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );

                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            Unit.TargetActor = nearestAggroedUnit;
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region FollowEvenHiddenPlayerUnits
        public static bool FollowEvenHiddenPlayerUnits( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                Unit.TurnDumpLines.Add( "FollowEvenHiddenPlayerUnits" );

            ISimMachineActor target = TargetingHelper.FindNearestMachineActorEvenIfHidden( Unit, Unit.GetDrawLocation(), false, Unit.GetAttackRangeSquared(), Logic );
            if ( target == null )
                return false;

            Unit.TargetActor = target;
            //move toward this actor!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, target, false, false, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunAfterNearestActorThatICanAutoShoot
        public static bool TryRunAfterNearestActorThatICanAutoShoot( ISimNPCUnit Unit, MersenneTwister Rand, bool limitToDistrict, bool limitToPOI, NPCUnitActionLogic Logic, bool CheckAllActorsOnMap )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                Unit.TurnDumpLines.Add( "TryRunAfterNearestActorThatICanAutoShoot" );

            TargetingHelper.FindNearestActorThatICanAutoShoot( Unit, false, limitToDistrict, limitToPOI,
                out ISimMapActor NearestInRestriction, out ISimMapActor NearestOverall, Logic, CheckAllActorsOnMap );

            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                Unit.DebugText += "TryRunAfterNearestActorThatICanAutoShoot NearestInRestriction: " + (NearestInRestriction== null ? null : NearestInRestriction.GetDisplayName() ) +
                    " NearestOverall: " + (NearestOverall == null ? null : NearestOverall.GetDisplayName()) + " \n";

            if ( NearestInRestriction == null )
                return false;

            Unit.TargetActor = NearestInRestriction;
            //move toward this actor!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, NearestInRestriction,
                limitToDistrict, limitToPOI, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }                
            }
            return true;
        }
        #endregion

        #region TryRunAfterNearestNonPlayerActorThatICanAutoShoot
        public static bool TryRunAfterNearestNonPlayerActorThatICanAutoShoot( ISimNPCUnit Unit, MersenneTwister Rand, bool limitToDistrict, bool limitToPOI, NPCUnitActionLogic Logic, bool CheckAllActorsOnMap )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                Unit.TurnDumpLines.Add( "TryRunAfterNearestNonPlayerActorThatICanAutoShoot" );

            TargetingHelper.FindNearestNonPlayerActorThatICanAutoShoot( Unit, false, limitToDistrict, limitToPOI,
                out ISimMapActor NearestInRestriction, out ISimMapActor NearestOverall, Logic, CheckAllActorsOnMap );

            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                Unit.DebugText += "TryRunAfterNearestNonPlayerActorThatICanAutoShoot NearestInRestriction: " + (NearestInRestriction == null ? null : NearestInRestriction.GetDisplayName()) +
                    " NearestOverall: " + (NearestOverall == null ? null : NearestOverall.GetDisplayName()) + " \n";

            if ( NearestInRestriction == null )
                return false;

            Unit.TargetActor = NearestInRestriction;
            //move toward this actor!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, NearestInRestriction,
                limitToDistrict, limitToPOI, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunAfterNearestActorThatICanAutoShootWithinRangeOfBuilding
        public static bool TryRunAfterNearestActorThatICanAutoShootWithinRangeOfBuilding( ISimNPCUnit Unit, MersenneTwister Rand, ISimBuilding RootBuilding, 
            float RadiusAroundBuilding, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                Unit.TurnDumpLines.Add( "TryRunAfterNearestActorThatICanAutoShootWithinRangeOfBuilding" );

            TargetingHelper.FindNearestActorThatICanAutoShootWithinRangeOfBuilding( Unit, false, RootBuilding, RadiusAroundBuilding,
                out ISimMapActor Nearest, Logic );
            if ( Nearest == null )
                return false;

            Unit.TargetActor = Nearest;
            //move toward this actor!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, Nearest,
                false, false, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunAfterNearestMachineStructureThatICanAutoShoot
        public static bool TryRunAfterNearestMachineStructureThatICanAutoShoot( ISimNPCUnit Unit, MersenneTwister Rand, bool limitToDistrict, bool limitToPOI, NPCUnitActionLogic Logic )
        {
            TargetingHelper.FindNearestMachineStructureThatICanAutoShoot( Unit, false, limitToDistrict, limitToPOI,
                out ISimMapActor NearestInRestriction, out ISimMapActor NearestOverall, Logic );
            if ( NearestInRestriction == null )
                return false;

            Unit.TargetActor = NearestInRestriction;
            //move toward this actor!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitChasingTarget( Unit, NearestInRestriction,
                limitToDistrict, limitToPOI, Logic );
            if ( unitLoc != null )
            {
                switch ( Logic )
                {
                    case NPCUnitActionLogic.PlanningPerTurn:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, Logic ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitActionLogic.ExecutionOfPriorPlan:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
            }
            return true;
        }
        #endregion

        #region TryRunToObjectiveBuilding
        public static bool TryRunToObjectiveBuilding( ISimNPCUnit Unit, ISimBuilding Building, NPCUnitStanceLogic StanceLogic )
        {
            if ( Unit == null || Building == null )
                return false;

            //move toward this building!
            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationForNPCUnitFocusedOnBuilding( Unit, Building );
            if ( unitLoc != null )
            {
                switch ( StanceLogic )
                {
                    case NPCUnitStanceLogic.PerTurnLogic:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            //if the current spot or the destination spot is within visible range, then schedule it
                            if ( !GetShouldNPCSilentlyDoThisMove( Unit, unitLoc, NPCUnitActionLogic.ExecutionOfPriorPlan ) )
                            {
                                Unit.TargetLocation = unitLoc.GetEffectiveWorldLocationForContainedUnit();
                                Unit.WantsToPerformAction = NPCActionDesire.Movement;
                            }
                            else //otherwise instantly do it
                            {
                                Unit.SetDesiredContainerLocation( unitLoc );
                                Unit.NextMoveIsSilent = true;
                            }
                        }
                        break;
                    case NPCUnitStanceLogic.MoveLogic:
                        {
                            unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                            Unit.SetDesiredContainerLocation( unitLoc );
                            if ( Unit.UnitType.OnMoveStart != null )
                                Unit.UnitType.OnMoveStart.DuringGame_PlayAtLocation( Unit.GetDrawLocation(), Unit.GetDrawRotation().eulerAngles );
                        }
                        break;
                }
                return true;
            }
            else
                return false;
        }
        #endregion

        #region TryWander
        public static bool TryWander( ISimNPCUnit Unit, MersenneTwister Rand, bool limitToDistrict, bool limitToPOI )
        {
            ISimUnitLocation unitLoc = PathingHelper.FindRandomUnitLocationForNPCUnit( Unit,
                    limitToDistrict, limitToPOI, Rand );
            if ( unitLoc != null )
            {
                unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                Unit.NextMoveIsSilent = true;
                //always instantly do it, this is just wandering after all
                Unit.SetDesiredContainerLocation( unitLoc );
                //also do it without sound effects
                return true;
            }
            return false;
        }
        #endregion

        #region TryMoveTowardHomePOIIfNeeded
        public static bool TryMoveTowardHomePOIIfNeeded( ISimNPCUnit Unit, MersenneTwister Rand )
        {
            MapPOI currentPOI = Unit.CalculatePOI();
            MapPOI homePOI = Unit.HomePOI;
            if ( homePOI == null || homePOI == currentPOI )
                return false; //nothing to do!

            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationTowardsPOI( Unit,
                homePOI, Rand );
            if ( unitLoc != null )
            {
                unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                Unit.NextMoveIsSilent = true;
                //always instantly do it, this is just wandering after all
                Unit.SetDesiredContainerLocation( unitLoc );
                //also do it without sound effects
                return true;
            }
            return false;
        }
        #endregion

        #region TryMoveTowardHomeDistrictIfNeeded
        public static bool TryMoveTowardHomeDistrictIfNeeded( ISimNPCUnit Unit, MersenneTwister Rand )
        {
            MapDistrict currentDistrict = Unit.CalculateMapDistrict();
            MapDistrict homeDistrict = Unit.HomeDistrict;
            if ( homeDistrict == null || homeDistrict == currentDistrict )
                return false; //nothing to do!

            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationTowardsDistrict( Unit,
                homeDistrict, Rand );
            if ( unitLoc != null )
            {
                unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                Unit.NextMoveIsSilent = true;
                //always instantly do it, this is just wandering after all
                Unit.SetDesiredContainerLocation( unitLoc );
                //also do it without sound effects
                return true;
            }
            return false;
        }
        #endregion

        #region TryMoveTowardRadiusFromBuildingIfNeeded
        public static bool TryMoveTowardRadiusFromBuildingIfNeeded( ISimNPCUnit Unit, MersenneTwister Rand, ISimBuilding RootBuilding, float RadiusAroundBuilding )
        {
            float radiusSquared = RadiusAroundBuilding * RadiusAroundBuilding;
            Vector3 targetPoint = RootBuilding.GetMapItem().CenterPoint;

            float distance = (Unit.GetDrawLocation() - targetPoint).GetSquareGroundMagnitude();
            if ( distance <= radiusSquared )
                return false; //nothing to do!  Already within that radius

            ISimUnitLocation unitLoc = PathingHelper.FindBestRandomUnitLocationTowardsSquaredRadiusAroundPoint( Unit, radiusSquared, targetPoint, Rand );
            if ( unitLoc != null )
            {
                unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                Unit.NextMoveIsSilent = true;
                //always instantly do it, this is just wandering after all
                Unit.SetDesiredContainerLocation( unitLoc );
                //also do it without sound effects
                return true;
            }
            return false;
        }
        #endregion

        #region TryMoveTowardPosition
        public static bool TryMoveTowardPosition( ISimNPCUnit Unit, Vector3 targetPoint )
        {
            Vector3 unitPos = Unit.GetDrawLocation();
            float currentDistance = (unitPos - targetPoint).GetSquareGroundMagnitude();

            ISimUnitLocation unitLoc = PathingHelper.FindBestUnitLocationNearCoordinates( Unit, targetPoint, Unit.UnitType.IsMechStyleMovement ? CollisionRule.Relaxed : CollisionRule.Strict );
            if ( unitLoc != null )
            {
                float newDist = (unitPos - unitLoc.GetEffectiveWorldLocationForContainedUnit()).GetSquareGroundMagnitude();
                if ( newDist >= currentDistance * 0.9f )
                    return false; //if it's not appreciably closer, then return false; we're basically there

                unitLoc.MarkAsBlockedUntilNextTurn( Unit );
                Unit.NextMoveIsSilent = true;
                //always instantly do it, this is just wandering after all
                Unit.SetDesiredContainerLocation( unitLoc );
                //also do it without sound effects
                return true;
            }
            return false;
        }
        #endregion

        #region TryStartObjective
        public static bool TryStartObjective( ISimNPCUnit Unit, NPCUnitObjective Objective, NPCActionConsideration Consideration, MersenneTwister Rand )
        {
            if ( Objective == null || Unit == null || Consideration == null )
            {
                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                    Unit.DebugText += "TryStartObjective-FAIL-SomethingNull\n";
                return false;
            }

            //start by clearing everything if we're in here
            Unit.WipeAllObjectiveData();

            switch ( Objective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionInPlace:
                case NPCObjectiveStyle.CustomActionInPlace:
                    {
                        Unit.CurrentObjective = Objective; //set the current objective, since we have nowhere we need to go
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            Unit.DebugText += "TryStartObjective-SUCCESS\n";
                        return true;
                    }
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                    {
                        if ( Consideration.TargetBuildingTag == null )
                        {
                            ArcenDebugging.LogSingleLine( "NPCActionHelper.TryStartObjective: Objective '" + Objective.ID +
                                "' uses the objective style " + Objective.ObjectiveStyle + ", but then has a null building tag on it!", Verbosity.ShowAsError );
                            return false;
                        }

                        ISimBuilding targetBuilding;
                        if ( Consideration.TargetPOITag != null )
                        {
                            //look POI-first, then building-tag next
                            targetBuilding = PathingHelper.FindRandomGlobalBuildingOfPOITagForNPCWithinRange( Unit, Consideration.TargetPOITag, Consideration.TargetBuildingTag, Consideration.CanTargetBuildingHaveMachineStructure, Rand,
                                !Consideration.SkipAnyHomeDistrictOrPOIRestrictions, Consideration.AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.PreferredMinDistanceOfObjectiveFromCurrentLocation, Consideration.PreferredMaxDistanceOfObjectiveFromCurrentLocation,
                                Consideration.AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.UseMissionCenterForPreferredDistancesIfRelevant, Consideration.UseProjectCenterForPreferredDistancesIfRelevant,
                                Consideration.POIMustBeAbleToAcceptReinforcementsToGetToMinimum, Consideration.POIMustBeAbleToAcceptAnyReinforcementsBelowMaximum,
                                Consideration.TargetBuildingMustHaveNoSwarm, Consideration.TargetBuildingCanHaveThisSwarm,
                                Consideration.TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, Consideration.TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
                                Consideration.TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, Consideration.TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
                                Consideration.TargetClosestPossibleBuilding );
                        }
                        else if ( Consideration.TargetDistrictTag != null )
                        {
                            //look District-first, then building-tag next
                            targetBuilding = PathingHelper.FindRandomGlobalBuildingOfDistrictTagForNPCWithinRange( Unit, Consideration.TargetDistrictTag, Consideration.TargetBuildingTag, Consideration.CanTargetBuildingHaveMachineStructure, Rand,
                                !Consideration.SkipAnyHomeDistrictOrPOIRestrictions, Consideration.AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.PreferredMinDistanceOfObjectiveFromCurrentLocation, Consideration.PreferredMaxDistanceOfObjectiveFromCurrentLocation,
                                Consideration.AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.UseMissionCenterForPreferredDistancesIfRelevant, Consideration.UseProjectCenterForPreferredDistancesIfRelevant,
                                Consideration.TargetBuildingMustHaveNoSwarm, Consideration.TargetBuildingCanHaveThisSwarm,
                                Consideration.TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, Consideration.TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
                                Consideration.TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, Consideration.TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
                                Consideration.TargetClosestPossibleBuilding );
                        }
                        else
                        {
                            //look just for the building tag
                            targetBuilding = PathingHelper.FindRandomGlobalBuildingOfTagForNPCWithinRange( Unit, Consideration.TargetBuildingTag, Consideration.CanTargetBuildingHaveMachineStructure, Rand,
                                !Consideration.SkipAnyHomeDistrictOrPOIRestrictions, Consideration.AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.PreferredMinDistanceOfObjectiveFromCurrentLocation, Consideration.PreferredMaxDistanceOfObjectiveFromCurrentLocation,
                                Consideration.AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
                                Consideration.UseMissionCenterForPreferredDistancesIfRelevant, Consideration.UseProjectCenterForPreferredDistancesIfRelevant,
                                Consideration.TargetBuildingMustHaveNoSwarm, Consideration.TargetBuildingCanHaveThisSwarm,
                                Consideration.TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, Consideration.TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
                                Consideration.TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, Consideration.TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
                                Consideration.TargetClosestPossibleBuilding );
                        }

                        if ( targetBuilding == null )
                        {
                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                Unit.DebugText += "TryStartObjective-FAIL-NullBuilding\n";
                            return false; //it happens, especially after a nuke
                        }

                        if ( Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMaximumOfTurns > 0 )
                        {
                            MapPOI poi = targetBuilding.CalculateLocationPOI();
                            if ( poi != null )
                            {
                                int toAdd = Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMaximumOfTurns;
                                if ( Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMinimumOfTurns < Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMaximumOfTurns )
                                    toAdd = Rand.NextInclus( Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMinimumOfTurns,
                                        Consideration.WhenTargetingBuildingBlockPOIReinforcementsForMaximumOfTurns );

                                if ( toAdd > 0 )
                                    Interlocked.Add( ref poi.IsBlockedFromBeingTargetedForReinforcementsForXMoreTurns, toAdd );
                            }
                        }

                        Unit.NextObjective = Objective; //set the next objective, but not yet the current one until we reach it
                        Unit.ObjectiveBuilding = new WrapperedSimBuilding( targetBuilding ); //set the building
                        TryRunToObjectiveBuilding( Unit, targetBuilding, NPCUnitStanceLogic.PerTurnLogic );
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            Unit.DebugText += "TryStartObjective-SUCCESS\n";
                        return true;
                    }
                default:
                    ArcenDebugging.LogSingleLine( "NPCActionHelper.TryStartObjective: Objective '" + Objective.ID +
                        "' uses the objective style " + Objective.ObjectiveStyle + ", which has not yet been implemented!", Verbosity.ShowAsError );
                    return false;
            }
        }
        #endregion

        #region CheckIfInRangeOfNextObjectiveAndStartItIfSo
        public static bool CheckIfInRangeOfNextObjectiveAndStartItIfSo( ISimNPCUnit Unit )
        {
            if ( Unit == null )
                return false;
            NPCUnitObjective nextObjective = Unit.NextObjective;
            if ( nextObjective == null )
                return false;

            switch ( nextObjective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionInPlace:
                case NPCObjectiveStyle.CustomActionInPlace:
                    return true; //it's always true
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                    {
                        ISimBuilding objectiveBuilding = Unit.ObjectiveBuilding.Get();
                        if ( objectiveBuilding == null )
                            return false;

                        //if the unit is standing on a building, then check it
                        if ( Unit.ContainerLocation.Get() is ISimBuilding building )
                        {
                            //if this building the type of our objective?
                            if ( building == objectiveBuilding )
                            {
                                //hey, we are there!  Very exciting!  Switch this to being the current objective
                                Unit.CurrentObjective = nextObjective;
                                Unit.NextObjective = null;
                                return true;
                            }
                        }

                        float requiredDistanceSingle = nextObjective.RequiredDistanceXZFromBuildingObjective + Unit.GetRadiusForCollisions();
                        float requiredDistanceSquared = requiredDistanceSingle * requiredDistanceSingle;

                        //if this unit is close enough, then go ahead and do it
                        if ( (objectiveBuilding.GetMapItem().GroundCenterPoint - Unit.GetDrawLocation()).GetSquareGroundMagnitude() <= requiredDistanceSquared )
                        {
                            //hey, we are close enough!  Very exciting!  Switch this to being the current objective
                            Unit.CurrentObjective = nextObjective;
                            Unit.NextObjective = null;
                            return true;
                        }

                        return false;
                    }
                default:
                    ArcenDebugging.LogSingleLine( "NPCActionHelper.CheckIfInRangeOfNextObjectiveAndStartItIfSo: Objective '" + nextObjective.ID +
                        "' uses the objective style " + nextObjective.ObjectiveStyle + ", which has not yet been implemented!", Verbosity.ShowAsError );
                    return false;
            }
        }
        #endregion

        #region Basic_TrySetThisObjectiveForNPCUnit
        public static bool Basic_TrySetThisObjectiveForNPCUnit( ISimNPCUnit Unit, NPCUnitObjective Objective, NPCActionConsideration Consideration, MersenneTwister Rand )
        {
            //if ( Unit.NextObjective != null ) //this was a bug!  It was causing units to get locked in place!
            //{
            //    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
            //        Unit.DebugText += "TSTOFN-FAIL-NextObjectiveAlreadySet\n";
            //    return false; //this happens and is expected, just ignore it
            //}
            if ( Unit.CurrentObjective != null )
            {
                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                    Unit.DebugText += "TSTOFN-FAIL-CurrentObjectiveAlreadySet\n";
                return false; //this happens and is expected, just ignore it
            }
            return TryStartObjective( Unit, Objective, Consideration, Rand );
        }
        #endregion

        #region Basic_HandleObjectiveCompletion
        public static void Basic_HandleObjectiveCompletion( ISimNPCUnit Unit, NPCUnitObjective Objective, MersenneTwister Rand )
        {
            //objective complete!
            if ( Objective.OnObjectiveComplete != null )
            {
                //if we have a particle effect, then wait
                Unit.WantsToPerformAction = NPCActionDesire.ObjectiveComplete;
                Unit.ObjectiveIsCompleteAndWaiting = true;
            }
            else //otherwise do it now!
                HandleCompletedObjective( Unit, Objective, Rand );
        }
        #endregion

        #region TryPursueNextObjective
        public static bool TryPursueNextObjective( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitStanceLogic StanceLogic )
        {
            if ( Unit == null)
                return false;
            NPCUnitObjective Objective = Unit.NextObjective;
            if ( Objective == null )
            {
                ArcenDebugging.LogSingleLine( "NPCActionHelper.TryPursueNextObjective: Somehow had null NextObjective for '" +
                    Unit.GetDisplayName() + "'!", Verbosity.ShowAsError );
                Unit.WipeAllObjectiveData();
                return false;
            }

            switch ( Objective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionInPlace:
                case NPCObjectiveStyle.CustomActionInPlace:
                    {
                        //we are always there!  Swap this in right now
                        Unit.CurrentObjective = Objective;
                        Unit.NextObjective = null;
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            Unit.DebugText += "TryPursueActionInPlace\n";
                        return true;
                    }
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                    {
                        int debugStage = 0;
                        try
                        {
                            debugStage = 100;
                            ISimBuilding objectiveBuilding = Unit.ObjectiveBuilding.Get();
                            if ( objectiveBuilding == null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "BuildingWasGone\n";
                                Unit.WipeAllObjectiveData();
                                return false;
                            }
                            debugStage = 1200;
                            if ( objectiveBuilding.GetMapItem() == null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "BuildingWasGone\n";
                                Unit.WipeAllObjectiveData(); //this building probably got deleted, and that's fine.  Just do something new
                                return false;
                            }

                            debugStage = 2100;
                            //if the unit is standing on a building, then check it
                            if ( Unit.ContainerLocation.Get() is ISimBuilding building )
                            {
                                debugStage = 2200;
                                //if this building the type of our objective?
                                if ( building == objectiveBuilding )
                                {
                                    //hey, we are there!  Very exciting!  Switch this to being the current objective
                                    Unit.CurrentObjective = Objective;
                                    Unit.NextObjective = null;
                                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                        Unit.DebugText += "TryPursueAlreadyOnBuilding\n";
                                    return true;
                                }
                            }

                            debugStage = 3100;

                            float requiredDistanceSingle = Objective.RequiredDistanceXZFromBuildingObjective + Unit.GetRadiusForCollisions();
                            float requiredDistanceSquared = requiredDistanceSingle * requiredDistanceSingle;

                            debugStage = 3200;

                            //if this unit is close enough, then go ahead and do it
                            if ( (objectiveBuilding.GetMapItem().GroundCenterPoint - Unit.GetDrawLocation()).GetSquareGroundMagnitude() <= requiredDistanceSquared )
                            {
                                //hey, we are close enough!  Very exciting!  Switch this to being the current objective
                                Unit.CurrentObjective = Objective;
                                Unit.NextObjective = null;
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "TryPursueAlreadyClose\n";
                                return true;
                            }

                            debugStage = 4100;
                            if ( !TryRunToObjectiveBuilding( Unit, objectiveBuilding, StanceLogic ) )
                            {
                                debugStage = 4200;
                                if ( StanceLogic == NPCUnitStanceLogic.PerTurnLogic )
                                {
                                    NPCActionConsideration objectiveConsideration = Unit.Stance?.GetNPCActionConsiderationForObjectiveType( Objective );
                                    if ( objectiveConsideration != null && objectiveConsideration.TargetPOITag != null )
                                    {
                                        #region POI Re-targeting
                                        MapPOI targetPOI = objectiveBuilding.CalculateLocationPOI();
                                        BuildingTag targetTag = objectiveConsideration.TargetBuildingTag;
                                        if ( targetTag != null && targetPOI != null )
                                        {
                                            Vector3 unitLocation = Unit.GetDrawLocation();
                                            foreach ( ISimBuilding otherBuilding in targetPOI.DuringGame_BuildingsInPOI.GetDisplayList() )
                                            {
                                                if ( !objectiveConsideration.CanTargetBuildingHaveMachineStructure && otherBuilding.MachineStructureInBuilding != null )
                                                    continue;

                                                if ( (otherBuilding.GetMapItem().GroundCenterPoint - unitLocation).GetSquareGroundMagnitude() <= requiredDistanceSquared )
                                                {
                                                    //another building from the same poi is in range!
                                                    if ( otherBuilding.GetVariant().Tags.ContainsKey( targetTag.ID ) )
                                                    {
                                                        //and it has the right type!
                                                        Unit.ObjectiveBuilding = new WrapperedSimBuilding( otherBuilding );

                                                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                                            Unit.DebugText += "FoundAlternativFromSamePOI\n";
                                                        Unit.ObjectiveFailureWaitTurns = 0;
                                                        return true; //hooray, we ran after the building
                                                    }
                                                }
                                            }

                                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                                Unit.DebugText += "Fail-ButTestedOthersInPOI:" + targetPOI.DuringGame_BuildingsInPOI.GetDisplayList().Count + "\n";
                                        }
                                        #endregion
                                    }
                                    else if ( objectiveConsideration != null && objectiveConsideration.TargetDistrictTag != null )
                                    {
                                        #region District Re-targeting
                                        MapDistrict targetDistrict = objectiveBuilding.GetLocationDistrict();
                                        BuildingTag targetTag = objectiveConsideration.TargetBuildingTag;
                                        if ( targetTag != null && targetDistrict != null )
                                        {
                                            Vector3 unitLocation = Unit.GetDrawLocation();
                                            foreach ( ISimBuilding otherBuilding in targetDistrict.AllBuildings.GetDisplayList() )
                                            {
                                                if ( !objectiveConsideration.CanTargetBuildingHaveMachineStructure && otherBuilding.MachineStructureInBuilding != null )
                                                    continue;

                                                if ( (otherBuilding.GetMapItem().GroundCenterPoint - unitLocation).GetSquareGroundMagnitude() <= requiredDistanceSquared )
                                                {
                                                    //another building from the same district is in range!
                                                    if ( otherBuilding.GetVariant().Tags.ContainsKey( targetTag.ID ) )
                                                    {
                                                        //and it has the right type!
                                                        Unit.ObjectiveBuilding = new WrapperedSimBuilding( otherBuilding );

                                                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                                            Unit.DebugText += "FoundAlternativFromSameDistrict\n";
                                                        Unit.ObjectiveFailureWaitTurns = 0;
                                                        return true; //hooray, we ran after the building
                                                    }
                                                }
                                            }

                                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                                Unit.DebugText += "Fail-ButTestedOthersInDistrict:" + targetDistrict.AllBuildings.GetDisplayList().Count + "\n";
                                        }
                                        #endregion
                                    }
                                }

                                debugStage = 4300;
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "TryPursueCouldNotRun\n";
                                if ( StanceLogic == NPCUnitStanceLogic.PerTurnLogic )
                                {
                                    //if we failed, we are probably blocked by someone else, so try something different
                                    Unit.ObjectiveFailureWaitTurns++;
                                    int maxFailTurns = Unit.Stance?.GetNPCActionConsiderationForObjectiveType( Objective )?.MaxFailureWaitTurnsForBlockedObjective ?? 5;
                                    if ( Unit.ObjectiveFailureWaitTurns > maxFailTurns )
                                        Unit.WipeAllObjectiveData();
                                }
                                return false;
                            }
                            debugStage = 5100;
                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                Unit.DebugText += "TryPursueDidRun\n";
                            Unit.ObjectiveFailureWaitTurns = 0;
                            return true; //hooray, we ran after the building
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogDebugStageWithStack( "TryPursueNextObjective", debugStage, e, Verbosity.ShowAsError ); ;
                            return false;
                        }
                    }
                default:
                    ArcenDebugging.LogSingleLine( "NPCActionHelper.TryPursueNextObjective: Objective '" + Objective.ID +
                        "' uses the objective style " + Objective.ObjectiveStyle + ", which has not yet been implemented!", Verbosity.ShowAsError );
                    return false;
            }
        }
        #endregion

        #region CalculatePerTurnGainForPointsObjective
        public static int CalculatePerTurnGainForPointsObjective( ISimNPCUnit Unit, NPCUnitObjective Objective )
        {
            if ( Unit == null || Objective == null )
                return 1;

            double ongoingAmount = 0;
            foreach ( KeyValuePair<ActorDataType, float> kv in Objective.PointChangePerTurnPerActorData )
            {
                if ( kv.Value == 0 )
                    continue;
                ongoingAmount += (Unit.GetActorDataCurrent( kv.Key, true ) * kv.Value);
            }

            ISimBuilding building = Unit.ObjectiveBuilding.Get();
            if ( building == null ) //use the location if the objective is not a building
                building = Unit.ContainerLocation.Get() as ISimBuilding;

            if ( building != null )
            {
                foreach ( KeyValuePair<LocationDataType, float> kv in Objective.PointChangePerTurnPerLocationData )
                {
                    if ( kv.Value == 0 )
                        continue;
                    ongoingAmount += (building.GetBuildingDataValue( kv.Key ) * kv.Value);
                }
            }

            if ( ongoingAmount < Objective.MinPointsPerTurnFromActors )
                return MathA.Max( 1, Objective.MinPointsPerTurnFromActors );

            return MathA.Max( 1, Mathf.RoundToInt( (float)ongoingAmount ) );
        }
        #endregion

        #region HandleCompletedObjective
        public static void HandleCompletedObjective( ISimNPCUnit Unit, NPCUnitObjective Objective, MersenneTwister Rand )
        {
            if ( Unit == null || Objective == null )
                return;

            foreach ( KeyValuePair<ActorDataType, int> kv in Objective.ActingActorDataChanges )
            {
                if ( kv.Value == 0 )
                    continue;
                Unit.AlterActorDataCurrent( kv.Key, kv.Value, true );
            }

            foreach ( KeyValuePair<NPCUnitAccumulator, IntRange> kv in Objective.ActingActorAccumulatorChanges )
            {
                int amount = kv.Value.GetRandom( Engine_Universal.PermanentQualityRandom );
                if ( amount == 0 )
                    continue;
                Unit.AlterAccumulatorAmount( kv.Key, amount );
            }

            NPCMission relatedMission = Unit?.ParentManager?.PeriodicData?.GateByCity?.RequiredNPCMissionActive;
            if ( relatedMission != null )
            {
                if ( Objective.MissionProgressWhenCompleted != 0 )
                    relatedMission.AlterProgress( Objective.MissionProgressWhenCompleted );
            }

            ISimMapActor objectiveActor = Unit.ObjectiveActor;
            if ( objectiveActor != null )
            {
                foreach ( KeyValuePair<ActorDataType, int> kv in Objective.TargetActorDataChanges )
                {
                    if ( kv.Value == 0 )
                        continue;
                    objectiveActor.AlterActorDataCurrent( kv.Key, kv.Value, true );
                }
            }

            if ( Objective.DoReinforcementForTargetedPOIWhenCompleted )
            {
                MapPOI poi = Unit.ObjectiveBuilding.Get()?.CalculateLocationPOI();
                if ( poi == null )
                {
                    //ArcenDebugging.LogSingleLine( "Null poi.  Also null objective building? " + (Unit.ObjectiveBuilding.Get() == null), Verbosity.DoNotShow );
                }
                else
                {
                    int added = poi.TryReinforcementGuardSeeding( Rand );
                    //ArcenDebugging.LogSingleLine( "Added " + added + " units from " + Objective.ID, Verbosity.DoNotShow );
                }
            }

            if ( Objective.SwitchToStanceWhenObjectiveComplete != null )
                Unit.Stance = Objective.SwitchToStanceWhenObjectiveComplete;

            if ( Objective.DisbandAsSoonAsNotSelectedAfterPerformingObjective )
                Unit.DisbandAsSoonAsNotSelected = true;
            else if ( Objective.DisbandNextTurnAfterPerformingObjective )
                Unit.DisbandAtTheStartOfNextTurn = true;

            if ( Objective.SwarmToApplyToBuilding != null )
            {
                ISimBuilding building = Unit.ObjectiveBuilding.Get();
                if ( building != null && ( building.SwarmSpread == null || building.SwarmSpread == Objective.SwarmToApplyToBuilding ) )
                {
                    building.SwarmSpread = Objective.SwarmToApplyToBuilding;
                    if ( Objective.ApplySquadCountToSwarmSpread )
                        building.AlterSwarmSpreadCount( Unit.CurrentSquadSize );
                    else
                        building.AlterSwarmSpreadCount( 1 );

                    if ( Objective.MaxPercentageToKillAtBuildingOnSwarmRelease > 0 )
                    {
                        int toKillPercent = Rand.Next( Objective.MinPercentageToKillAtBuildingOnSwarmRelease, Objective.MaxPercentageToKillAtBuildingOnSwarmRelease );

                        if ( toKillPercent > 0 )
                        {
                            int totalHere = building.GetTotalResidentCount() + building.GetTotalWorkerCount();

                            int toKill = Mathf.CeilToInt( totalHere * ((float)toKillPercent / 100f) );
                            if ( toKill > 0 )
                            {
                                building.KillRandomHere( toKill, Rand );

                                if ( Objective.StatisticToUseForSwarmKills != null )
                                    Objective.StatisticToUseForSwarmKills.AlterScore_CityAndMeta( toKill );
                            }
                        }
                    }
                }
            }

            if ( Objective.ExtraCodeToRunOnObjectiveComplete != null )
            {
                Objective.ExtraCodeToRunOnObjectiveComplete.Implementation.HandleExtraNPCCompletedObjectiveConsequences( Objective.ExtraCodeToRunOnObjectiveComplete,
                    Unit, Objective, Rand, relatedMission, objectiveActor, Unit.ObjectiveBuilding.Get() );
            }

            Unit.WipeAllObjectiveData();

            if ( !Unit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
            {
                if ( !Objective.PopupTextOnComplete.Text.IsEmpty() )
                {
                    Vector3 startLocation = Unit.GetPositionForCollisions();
                    Vector3 endLocation = startLocation.PlusY( Unit.GetHalfHeightForCollisions() + 0.2f );

                    ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                    if ( buffer != null )
                    {
                        buffer.AddRaw( Objective.PopupTextOnComplete.Text, Objective.PopupColorHexWithHDRHex );
                        DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                            startLocation, endLocation, Objective.PopupTextScale, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                        newDamageText.PhysicalDamageIncluded = 0;
                        newDamageText.MoraleDamageIncluded = 0;
                        newDamageText.SquadDeathsIncluded = 0;
                    }
                }

                if ( Objective.LogCompletionToEventLog )
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCObjectiveComplete" ),
                        NoteStyle.StoredGame, Objective.ID, Unit.UnitType.ID, Unit.FromCohort.ID, string.Empty,
                        Unit.ActorID, 0, 0,
                        string.Empty, string.Empty, string.Empty,
                        SimCommon.Turn + 15 );
            }

            if ( Objective.DisbandInstantlyAfterPerformingObjective )
                Unit.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
        }
        #endregion

        #region HandleFailedObjective
        public static void HandleFailedObjective( ISimNPCUnit Unit, NPCUnitObjective ObjectiveOrNull )
        {
            if ( Unit == null )
                return;

            if ( ObjectiveOrNull != null )
            {
                if ( ObjectiveOrNull.DisbandAsSoonAsNotSelectedAfterPerformingObjective )
                    Unit.DisbandAsSoonAsNotSelected = true;
                else if ( ObjectiveOrNull.DisbandNextTurnAfterPerformingObjective )
                    Unit.DisbandAtTheStartOfNextTurn = true;
            }

            Unit.WipeAllObjectiveData();
        }
        #endregion
    }
}

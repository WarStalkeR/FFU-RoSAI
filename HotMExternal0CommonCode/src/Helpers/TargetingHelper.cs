using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public static class TargetingHelper
    {
        #region FindNearestOutcastMachineUnit
        public static ISimMachineUnit FindNearestOutcastMachineUnit( ISimNPCUnit NPCUnit, Vector3 Location, NPCUnitActionLogic Logic, int OutcastLevelMustBeAtLeast )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "FindNearestOutcastMachineUnit" );

            NPCCohort fromCohort = NPCUnit.FromCohort;

            int numberChecked = 0;
            ISimMachineUnit nearest = null;
            float nearestRange = 100000000;
            foreach ( ISimMachineUnit unitDeployed in World.Forces.GetMachineUnitsDeployed() )
            {
                numberChecked++;
                if ( ( unitDeployed.OutcastLevel >= OutcastLevelMustBeAtLeast || unitDeployed.GetIsTrackedByCohort( fromCohort ) ) && !unitDeployed.IsCloaked &&
                    !unitDeployed.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) && !unitDeployed.IsFullDead )
                {
                    float range = (unitDeployed.GetDrawLocation() - Location).GetSquareGroundMagnitude();
                    if ( nearest == null || nearestRange > range )
                    {
                        nearest = unitDeployed;
                        nearestRange = range;
                    }
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            return nearest;
        }
        #endregion

        #region FindNearestConspicuousMachineActor
        public static ISimMachineActor FindNearestConspicuousMachineActor( ISimNPCUnit NPCUnit, Vector3 Location, bool MustBeInRange, float RangeSquared, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "FindNearestConspicuousMachineActor" );

            int numberChecked = 0;
            ISimMachineActor nearest = null;
            float nearestRange = 100000000;
            foreach ( ISimMachineUnit unitDeployed in World.Forces.GetMachineUnitsDeployed() )
            {
                if ( unitDeployed.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || unitDeployed.IsFullDead || unitDeployed.IsCloaked )
                    continue;
                if ( NPCUnit.GetIsValidToAutomaticallyShootAt_Current( unitDeployed ) )
                {
                    numberChecked++;
                    float range = (unitDeployed.GetDrawLocation() - Location).GetSquareGroundMagnitude();
                    if ( MustBeInRange )
                    {
                        if ( range > RangeSquared )
                            continue;
                    }

                    if ( nearest == null || nearestRange > range )
                    {
                        nearest = unitDeployed;
                        nearestRange = range;
                    }
                }
            }
            foreach ( KeyValuePair<int, ISimMachineVehicle> kv in World.Forces.GetMachineVehiclesByID() )
            {
                ISimMachineVehicle vehicle = kv.Value;
                if ( vehicle.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) || vehicle.IsFullDead || vehicle.IsCloaked )
                    continue;

                if ( NPCUnit.GetIsValidToAutomaticallyShootAt_Current( vehicle ) )
                {
                    numberChecked++;
                    float range = (vehicle.GetDrawLocation() - Location).GetSquareGroundMagnitude();
                    if ( MustBeInRange )
                    {
                        if ( range > RangeSquared )
                            continue;
                    }

                    if ( nearest == null || nearestRange > range )
                    {
                        nearest = vehicle;
                        nearestRange = range;
                    }
                }
            }


            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            return nearest;
        }
        #endregion

        #region FindNearestMachineActorEvenIfHidden
        public static ISimMachineActor FindNearestMachineActorEvenIfHidden( ISimNPCUnit NPCUnit, Vector3 Location, bool MustBeInRange, float RangeSquared, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "FindNearestMachineActorEvenIfHidden" );

            int numberChecked = 0;
            ISimMachineActor nearest = null;
            float nearestRange = 100000000;
            foreach ( ISimMachineUnit unitDeployed in World.Forces.GetMachineUnitsDeployed() )
            {
                if ( unitDeployed.GetIsCurrentlyInvisible( InvisibilityPurpose.ForCameraFocus ) || unitDeployed.IsFullDead || unitDeployed.IsCloaked )
                    continue;
                {
                    numberChecked++;
                    float range = (unitDeployed.GetDrawLocation() - Location).GetSquareGroundMagnitude();
                    if ( MustBeInRange )
                    {
                        if ( range > RangeSquared )
                            continue;
                    }

                    if ( nearest == null || nearestRange > range )
                    {
                        nearest = unitDeployed;
                        nearestRange = range;
                    }
                }
            }
            foreach ( KeyValuePair<int, ISimMachineVehicle> kv in World.Forces.GetMachineVehiclesByID() )
            {
                ISimMachineVehicle vehicle = kv.Value;
                if ( vehicle.GetIsCurrentlyInvisible( InvisibilityPurpose.ForCameraFocus ) || vehicle.IsFullDead || vehicle.IsCloaked )
                    continue;

                {
                    numberChecked++;
                    float range = (vehicle.GetDrawLocation() - Location).GetSquareGroundMagnitude();
                    if ( MustBeInRange )
                    {
                        if ( range > RangeSquared )
                            continue;
                    }

                    if ( nearest == null || nearestRange > range )
                    {
                        nearest = vehicle;
                        nearestRange = range;
                    }
                }
            }


            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                NPCUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            return nearest;
        }
        #endregion

        #region FindNearestAggroedUnit
        public static ISimMapActor FindNearestAggroedUnit( ISimNPCUnit SearcherNPCUnit, bool LimitToThoseInFiringRangeOfMe, 
            bool LimitToSameDistrict, bool LimitToSamePOI, bool MustBeAbleToShootThem, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "FindNearestAggroedUnit" );

            RangeAggroCheck restrict = RangeAggroCheck.InitializeNew();
            RangeAggroCheck overall = RangeAggroCheck.InitializeNew();

            Vector3 searcherLocation = SearcherNPCUnit.GetDrawLocation();
            MapCell curCell = SearcherNPCUnit.CalculateMapCell();
            if ( curCell == null )
                return null;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherNPCUnit.CalculatePOI() : null;

            ListView<ISimMapActor> actors = World.Forces.GetAllLivingMapActors();
            if ( actors.Count == 0 )
                return null;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "AllLivingMapActors: " + actors.Count );

            NPCUnitStance searcherStance = SearcherNPCUnit.Stance;
            if ( searcherStance == null )
                return null;

            NPCCohort mustHaveAggroed = SearcherNPCUnit.FromCohort;
            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor == null || actor.IsFullDead )
                    continue;
                if ( actor.GetEquals( SearcherNPCUnit ) )
                    continue; //don't look at self

                int amountAggroed = actor.GetAmountHasAggroedNPCCohort( mustHaveAggroed, searcherStance, (actor as ISimNPCUnit)?.Stance );
                if ( actor.GetIsTrackedByCohort( mustHaveAggroed ))
                    amountAggroed += 100;

                if ( amountAggroed <= 0 )
                    continue; //only look at ones that have aggroed us
                if ( MustBeAbleToShootThem )
                {
                    if ( !SearcherNPCUnit.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                        continue; //if the searcher is not supposed to shoot this actor, then ignore it
                }

                numberChecked++;

                float range = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( range > actor.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != actor.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != actor.CalculatePOI() )
                        meetsRestrictions = false;
                }

                if ( meetsRestrictions )
                    restrict.HandleRangeCheckWhileEvaluatingAggro( actor, range, amountAggroed );

                overall.HandleRangeCheckWhileEvaluatingAggro( actor, range, amountAggroed );
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            if ( restrict.best != null )
                return restrict.best;
            return overall.best;
        }
        #endregion

        #region FindNearestUnitWithBadge
        public static ISimMapActor FindNearestUnitWithBadge( ISimNPCUnit SearcherNPCUnit, ActorBadge Badge, bool LimitToThoseInFiringRangeOfMe,
            bool LimitToSameDistrict, bool LimitToSamePOI, bool MustBeAbleToShootThem, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "FindNearestAggroedUnit" );

            RangeAggroCheck restrict = RangeAggroCheck.InitializeNew();
            RangeAggroCheck overall = RangeAggroCheck.InitializeNew();

            Vector3 searcherLocation = SearcherNPCUnit.GetDrawLocation();
            MapCell curCell = SearcherNPCUnit.CalculateMapCell();
            if ( curCell == null )
                return null;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherNPCUnit.CalculatePOI() : null;

            ListView<ISimMapActor> actors = World.Forces.GetAllLivingMapActors();
            if ( actors.Count == 0 )
                return null;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "AllLivingMapActors: " + actors.Count );

            NPCCohort mustHaveAggroed = SearcherNPCUnit.FromCohort;
            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor == null || actor.IsFullDead )
                    continue;
                if ( actor.GetEquals( SearcherNPCUnit ) )
                    continue; //don't look at self

                if ( !actor.GetHasBadge( Badge ) )
                    continue;

                if ( MustBeAbleToShootThem )
                {
                    if ( !SearcherNPCUnit.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                        continue; //if the searcher is not supposed to shoot this actor, then ignore it
                }

                numberChecked++;

                float range = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( range > actor.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != actor.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != actor.CalculatePOI() )
                        meetsRestrictions = false;
                }

                if ( meetsRestrictions )
                    restrict.HandleRangeCheckWhileEvaluatingAggro( actor, range, 100 );

                overall.HandleRangeCheckWhileEvaluatingAggro( actor, range, 100 );
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherNPCUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            if ( restrict.best != null )
                return restrict.best;
            return overall.best;
        }
        #endregion

        #region FindNearestActorThatICanAutoShoot
        public static void FindNearestActorThatICanAutoShoot( ISimNPCUnit SearcherActor, bool LimitToThoseInFiringRangeOfMe, bool LimitToSameDistrict, bool LimitToSamePOI,
            out ISimMapActor NearestWithRestrictions, out ISimMapActor NearestOverall, NPCUnitActionLogic Logic, bool CheckAllActorsOnMap )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "FindNearestActorThatICanAutoShoot" );

            ISimMapActor nearestRestrict = null;
            float nearestRestrictRange = 100000000;
            ISimMapActor nearestOverall = null;
            float nearestOverallRange = 100000000;

            NearestWithRestrictions = null;
            NearestOverall = null;

            Vector3 searcherLocation = SearcherActor.GetDrawLocation();
            MapCell curCell = SearcherActor.CalculateMapCell();
            if ( curCell == null )
                return;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherActor.CalculatePOI() : null;

            List<ISimMapActor> actors;
            if ( CheckAllActorsOnMap )
                actors = SimCommon.AllActorsForTargeting.GetDisplayList();
            else
                actors = curCell?.ParentTile?.ActorsWithinMaxNPCAttackRange?.GetDisplayList();

            if ( actors == null || actors.Count == 0 )
                return;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "ActorsWithinMaxNPCAttackRange: " + actors.Count + " cell: " + curCell.CellID );

            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor.IsFullDead ) 
                    continue;
                if ( actor.GetEquals( SearcherActor ) )
                    continue; //don't look at self
                if ( !SearcherActor.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                    continue; //if the searcher is not supposed to shoot this actor, then ignore it

                numberChecked++;

                float range = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( range > actor.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != actor.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != actor.CalculatePOI() )
                        meetsRestrictions = false;
                }

                if ( meetsRestrictions )
                {
                    if ( nearestRestrict == null || nearestRestrictRange > range )
                    {
                        nearestRestrict = actor;
                        nearestRestrictRange = range;
                    }
                }

                if ( nearestOverall == null || nearestOverallRange > range )
                {
                    nearestOverall = actor;
                    nearestOverallRange = range;
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            NearestWithRestrictions = nearestRestrict;
            NearestOverall = nearestOverall;
        }
        #endregion

        #region FindNearestNonPlayerActorThatICanAutoShoot
        public static void FindNearestNonPlayerActorThatICanAutoShoot( ISimNPCUnit SearcherActor, bool LimitToThoseInFiringRangeOfMe, bool LimitToSameDistrict, bool LimitToSamePOI,
            out ISimMapActor NearestWithRestrictions, out ISimMapActor NearestOverall, NPCUnitActionLogic Logic, bool CheckAllActorsOnMap )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "FindNearestNonPlayerActorThatICanAutoShoot" );

            ISimMapActor nearestRestrict = null;
            float nearestRestrictRange = 100000000;
            ISimMapActor nearestOverall = null;
            float nearestOverallRange = 100000000;

            NearestWithRestrictions = null;
            NearestOverall = null;

            Vector3 searcherLocation = SearcherActor.GetDrawLocation();
            MapCell curCell = SearcherActor.CalculateMapCell();
            if ( curCell == null )
                return;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherActor.CalculatePOI() : null;

            List<ISimMapActor> actors;
            if ( CheckAllActorsOnMap )
                actors = SimCommon.AllActorsForTargeting.GetDisplayList();
            else
                actors = curCell?.ParentTile?.ActorsWithinMaxNPCAttackRange?.GetDisplayList();

            if ( actors == null || actors.Count == 0 )
                return;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "ActorsWithinMaxNPCAttackRange: " + actors.Count + " cell: " + curCell.CellID );

            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor.IsFullDead || actor.GetIsPartOfPlayerForcesInAnyWay() )
                    continue;
                if ( actor.GetEquals( SearcherActor ) )
                    continue; //don't look at self
                if ( !SearcherActor.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                    continue; //if the searcher is not supposed to shoot this actor, then ignore it

                numberChecked++;

                float range = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( range > actor.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != actor.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != actor.CalculatePOI() )
                        meetsRestrictions = false;
                }

                if ( meetsRestrictions )
                {
                    if ( nearestRestrict == null || nearestRestrictRange > range )
                    {
                        nearestRestrict = actor;
                        nearestRestrictRange = range;
                    }
                }

                if ( nearestOverall == null || nearestOverallRange > range )
                {
                    nearestOverall = actor;
                    nearestOverallRange = range;
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            NearestWithRestrictions = nearestRestrict;
            NearestOverall = nearestOverall;
        }
        #endregion

        #region FindNearestActorThatICanAutoShootWhoIsDoingAnActionOverTime
        public static void FindNearestActorThatICanAutoShootWhoIsDoingAnActionOverTime( ISimNPCUnit SearcherActor, bool LimitToThoseInFiringRangeOfMe, bool LimitToSameDistrict, bool LimitToSamePOI,
            out ISimMapActor NearestWithRestrictions, out ISimMapActor NearestOverall, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "FindNearestActorThatICanAutoShoot" );

            ISimMapActor nearestRestrict_AnyRange = null;
            float nearestRestrictRange_AnyRange = 100000000;
            ISimMapActor nearestOverall_AnyRange = null;
            float nearestOverallRange_AnyRange = 100000000;

            ISimMapActor bestAggroRestrict_InRange = null;
            int bestAggroRestrict = 0;
            ISimMapActor bestAggroOverall_InRange = null;
            int bestAggroOverall = 0;

            NearestWithRestrictions = null;
            NearestOverall = null;

            Vector3 searcherLocation = SearcherActor.GetDrawLocation();
            MapCell curCell = SearcherActor.CalculateMapCell();
            if ( curCell == null )
                return;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherActor.CalculatePOI() : null;

            List<ISimMapActor> actors = curCell?.ParentTile?.ActorsWithinMaxNPCAttackRange?.GetDisplayList();
            if ( actors == null || actors.Count == 0 )
                return;

            NPCUnitStance searcherStance = SearcherActor.Stance;
            if ( searcherStance == null )
                return;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "ActorsWithinMaxNPCAttackRange: " + actors.Count + " cell: " + curCell.CellID );

            NPCCohort attackerGroup = SearcherActor?.FromCohort;

            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor.IsFullDead )
                    continue;
                if ( actor.GetEquals( SearcherActor ) )
                    continue; //don't look at self

                ISimMachineActor machineActor = actor as ISimMachineActor;
                if ( machineActor != null )
                {
                    if ( machineActor.CurrentActionOverTime == null  //no action over time
                        && machineActor.GetStatusIntensity( CommonRefs.ServiceDisruption ) <= 0 ) //and no fake action over time
                        continue;
                }
                else
                    continue; //no action over time, since not a machine actor

                if ( !SearcherActor.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                    continue; //if the searcher is not supposed to shoot this actor, then ignore it

                numberChecked++;

                float range = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();
                bool isInRange = range <= actor.GetAttackRangeSquared();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( !isInRange )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != actor.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != actor.CalculatePOI() )
                        meetsRestrictions = false;
                }

                int amountAggroed = actor.GetAmountHasAggroedNPCCohort( attackerGroup, searcherStance, (actor as ISimNPCUnit)?.Stance );
                if ( actor.GetIsTrackedByCohort( attackerGroup ) )
                    amountAggroed += 100;

                if ( machineActor.CurrentActionOverTime != null )
                    amountAggroed += machineActor.CurrentActionOverTime.Type.AggroAmountForActionOverTimeHunters;

                int serviceDisruptionAmount = machineActor.GetStatusIntensity( CommonRefs.ServiceDisruption );
                if ( serviceDisruptionAmount > 0 )
                    amountAggroed += serviceDisruptionAmount;

                if ( isInRange )
                {
                    if ( meetsRestrictions )
                    {
                        if ( bestAggroRestrict_InRange == null || bestAggroRestrict < amountAggroed )
                        {
                            bestAggroRestrict_InRange = actor;
                            bestAggroRestrict = amountAggroed;
                        }
                    }

                    if ( bestAggroOverall_InRange == null || bestAggroOverall < amountAggroed )
                    {
                        bestAggroOverall_InRange = actor;
                        bestAggroOverall = amountAggroed;
                    }
                }

                if ( meetsRestrictions )
                {
                    if ( nearestRestrict_AnyRange == null || nearestRestrictRange_AnyRange > range )
                    {
                        nearestRestrict_AnyRange = actor;
                        nearestRestrictRange_AnyRange = range;
                    }
                }

                if ( nearestOverall_AnyRange == null || nearestOverallRange_AnyRange > range )
                {
                    nearestOverall_AnyRange = actor;
                    nearestOverallRange_AnyRange = range;
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            NearestWithRestrictions = bestAggroRestrict_InRange != null ? bestAggroRestrict_InRange : nearestRestrict_AnyRange;
            NearestOverall = bestAggroOverall_InRange != null ? bestAggroOverall_InRange : nearestOverall_AnyRange;
        }
        #endregion

        #region FindNearestActorThatICanAutoShootWithinRangeOfBuilding
        public static void FindNearestActorThatICanAutoShootWithinRangeOfBuilding( ISimNPCUnit SearcherActor, bool LimitToThoseInFiringRangeOfMe, ISimBuilding RootBuilding,
            float RadiusAroundBuilding, out ISimMapActor Nearest, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "FindNearestActorThatICanAutoShootWithinRangeOfBuilding" );

            ISimMapActor nearest = null;
            float nearestRange = 100000000;

            Nearest = null;

            Vector3 searcherLocation = SearcherActor.GetDrawLocation();
            MapCell curCell = SearcherActor.CalculateMapCell();
            if ( curCell == null )
                return;

            ListView<ISimMapActor> actors = World.Forces.GetLivingMapActorsNotBeingABasicGuard();
            if ( actors.Count == 0 )
                return;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "LivingMapActorsNotBeingABasicGuard: " + actors.Count + " cell: " + curCell.CellID );

            Vector3 centerLoc = RootBuilding.GetMapItem().CenterPoint;
            float maxRadiusSquaredAroundCenter = RadiusAroundBuilding * RadiusAroundBuilding;

            int numberChecked = 0;

            foreach ( ISimMapActor actor in actors )
            {
                if ( actor.IsFullDead )
                    continue;
                if ( actor.GetEquals( SearcherActor ) )
                    continue; //don't look at self

                numberChecked++;

                float rangeFromCenter = (actor.GetDrawLocation() - centerLoc).GetSquareGroundMagnitude();
                if ( rangeFromCenter > maxRadiusSquaredAroundCenter )
                    continue; //too far from the center, so discard

                if ( !SearcherActor.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                    continue; //if the searcher is not supposed to shoot this actor, then ignore it

                float rangeFromSearcher = (actor.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( rangeFromSearcher > actor.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the actor is out of the range where they can shoot at us
                }

                if ( nearest == null || nearestRange > rangeFromSearcher )
                {
                    nearest = actor;
                    nearestRange = rangeFromSearcher;
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            Nearest = nearest;
        }
        #endregion

        #region FindNearestMachineStructureThatICanAutoShoot
        public static void FindNearestMachineStructureThatICanAutoShoot( ISimNPCUnit SearcherActor, bool LimitToThoseInFiringRangeOfMe, bool LimitToSameDistrict, bool LimitToSamePOI,
            out ISimMapActor NearestWithRestrictions, out ISimMapActor NearestOverall, NPCUnitActionLogic Logic )
        {
            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "FindNearestMachineStructureThatICanAutoShoot" );

            ISimMapActor nearestRestrict = null;
            float nearestRestrictRange = 100000000;
            ISimMapActor nearestOverall = null;
            float nearestOverallRange = 100000000;

            NearestWithRestrictions = null;
            NearestOverall = null;

            Vector3 searcherLocation = SearcherActor.GetDrawLocation();
            MapCell curCell = SearcherActor.CalculateMapCell();
            if ( curCell == null )
                return;
            MapDistrict district = LimitToSameDistrict ? curCell?.ParentTile?.District : null;
            MapPOI poi = LimitToSamePOI ? SearcherActor.CalculatePOI() : null;

            ConcurrentDictionary<int, MachineStructure> structures = SimCommon.MachineStructuresByID;
            if ( structures == null || structures.Count == 0 )
                return;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "StructuresInWorld: " + structures.Count + " cell: " + curCell.CellID );

            int numberChecked = 0;

            foreach ( KeyValuePair<int, MachineStructure> kv in structures )
            {
                MachineStructure structure = kv.Value;
                if ( structure.IsFullDead )
                    continue;
                if ( !SearcherActor.GetIsValidToAutomaticallyShootAt_Current( structure ) )
                    continue; //if the searcher is not supposed to shoot this structure, then ignore it

                numberChecked++;

                float range = (structure.GetDrawLocation() - searcherLocation).GetSquareGroundMagnitude();

                if ( LimitToThoseInFiringRangeOfMe )
                {
                    if ( range > structure.GetAttackRangeSquared() )
                        continue; //in this case, completely skip it, because the structure is out of the range where they can shoot at us
                }

                bool meetsRestrictions = true;
                if ( LimitToSameDistrict )
                {
                    if ( district != structure.CalculateMapDistrict() )
                        meetsRestrictions = false;
                }
                if ( LimitToSamePOI )
                {
                    if ( poi != structure.CalculatePOI() )
                        meetsRestrictions = false;
                }

                if ( meetsRestrictions )
                {
                    if ( nearestRestrict == null || nearestRestrictRange > range )
                    {
                        nearestRestrict = structure;
                        nearestRestrictRange = range;
                    }
                }

                if ( nearestOverall == null || nearestOverallRange > range )
                {
                    nearestOverall = structure;
                    nearestOverallRange = range;
                }
            }

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                SearcherActor.TurnDumpLines.Add( "numberChecked: " + numberChecked );

            NearestWithRestrictions = nearestRestrict;
            NearestOverall = nearestOverall;
        }
        #endregion

        #region DoForAllBuildingsWithinRangeTight
        public static void DoForAllBuildingsWithinRangeTight( ISimMapActor Actor, float Range, BuildingTag LimitToTag,
            ActionThatCanBeStopped<ISimBuilding> PerBuilding )
        {
            Vector3 loc = Actor.GetDrawLocation();

            MapCell centerCell = Actor.CalculateMapCell();
            if ( centerCell == null )
                return;

            //the following code is repeated in the vehicle section, as it's inlined for performance reasons
            float max_minX = loc.x - Range;
            float max_maxX = loc.x + Range;
            float max_minZ = loc.z - Range;
            float max_maxZ = loc.z + Range;

            System.Collections.IEnumerable list = null;
            if ( Range < 30 )
                list = centerCell.AdjacentCellsAndSelfIncludingDiagonal2x;
            else if ( Range < 50 )
                list = centerCell.AdjacentCellsAndSelfIncludingDiagonal3x;
            else
                list = CityMap.Cells;

            float rangeSquared = Range * Range;

            foreach ( MapCell cell in list )
            {
                ArcenFloatRectangle rect = cell.CellRect;
                if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                    rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                    continue;

                foreach ( MapItem building in cell.BuildingList.GetDisplayList() )
                {
                    if ( LimitToTag != null )
                    {
                        BuildingTypeVariant variant = building?.SimBuilding?.GetVariant();
                        if ( variant == null )
                            continue;
                        if ( !variant.Tags.ContainsKey( LimitToTag.ID ) )
                            continue;
                    }

                    if ( ( building.CenterPoint - loc ).GetSquareGroundMagnitude() > rangeSquared )
                        continue; //too far away

                    ISimBuilding sBuild = building?.SimBuilding;
                    if ( sBuild == null )
                        continue;

                    BuildingStatus status = sBuild.GetStatus();
                    if ( status != null && (status.ShouldBuildingBeBurnedVisually || status.ShouldBuildingBeInvisible) )
                        continue; //already destroyed, so skip

                    if ( PerBuilding( sBuild ) )
                        return; //stop if returned true
                }
            }
        }
        #endregion

        #region DoForAllBuildingsWithinRangeTight
        public static void DoForAllBuildingsWithinRangeTight( ISimBuilding Building, float Range, BuildingTag LimitToTag,
            ActionThatCanBeStopped<ISimBuilding> PerBuilding )
        {
            MapItem item = Building?.GetMapItem();
            if ( item == null )
                return;

            Vector3 loc = item.CenterPoint;

            MapCell centerCell = item.ParentCell;
            if ( centerCell == null )
                return;

            //the following code is repeated in the vehicle section, as it's inlined for performance reasons
            float max_minX = loc.x - Range;
            float max_maxX = loc.x + Range;
            float max_minZ = loc.z - Range;
            float max_maxZ = loc.z + Range;

            System.Collections.IEnumerable list = null;
            if ( Range < 30 )
                list = centerCell.AdjacentCellsAndSelfIncludingDiagonal2x;
            else if ( Range < 50 )
                list = centerCell.AdjacentCellsAndSelfIncludingDiagonal3x;
            else
                list = CityMap.Cells;

            float rangeSquared = Range * Range;

            foreach ( MapCell cell in list )
            {
                ArcenFloatRectangle rect = cell.CellRect;
                if ( rect.XMax <= max_minX || rect.XMin >= max_maxX ||
                    rect.YMax <= max_minZ || rect.YMin >= max_maxZ )
                    continue;

                foreach ( MapItem building in cell.BuildingList.GetDisplayList() )
                {
                    if ( LimitToTag != null )
                    {
                        BuildingTypeVariant variant = building?.SimBuilding?.GetVariant();
                        if ( variant == null )
                            continue;
                        if ( !variant.Tags.ContainsKey( LimitToTag.ID ) )
                            continue;
                    }

                    if ( (building.CenterPoint - loc).GetSquareGroundMagnitude() > rangeSquared )
                        continue; //too far away

                    ISimBuilding sBuild = building?.SimBuilding;
                    if ( sBuild == null )
                        continue;

                    BuildingStatus status = sBuild.GetStatus();
                    if ( status != null && (status.ShouldBuildingBeBurnedVisually || status.ShouldBuildingBeInvisible) )
                        continue; //already destroyed, so skip

                    if ( PerBuilding( sBuild ) )
                        return; //stop if returns true
                }
            }
        }
        #endregion

        #region DoForAllBuildingsOfTagWithinRangeOfCamera
        public static void DoForAllBuildingsOfTagWithinRangeOfCamera( BuildingTag LimitToTag,
            ActionThatCanBeStopped<ISimBuilding> PerBuilding )
        {
            foreach ( MapCell cell in CityMap.CellsInCameraView )
            {
                foreach ( MapItem building in cell.BuildingList.GetDisplayList() )
                {
                    if ( LimitToTag != null )
                    {
                        BuildingTypeVariant variant = building?.SimBuilding?.GetVariant();
                        if ( variant == null )
                            continue;
                        if ( !variant.Tags.ContainsKey( LimitToTag.ID ) )
                            continue;
                    }

                    ISimBuilding sBuild = building?.SimBuilding;
                    if ( sBuild == null )
                        continue;

                    BuildingStatus status = sBuild.GetStatus();
                    if ( status != null && (status.ShouldBuildingBeBurnedVisually || status.ShouldBuildingBeInvisible) )
                        continue; //already destroyed, so skip

                    //but only render the icon if we are close enough
                    float squareDistanceFromCamera = (CameraCurrent.CameraBodyPosition - building.CenterPoint ).sqrMagnitude;
                    if ( squareDistanceFromCamera > InputCaching.MaxDistanceToShowJobIcons_Squared &&
                        Engine_HotM.GameMode == MainGameMode.Streets )
                        continue;

                    if ( PerBuilding( sBuild ) )
                        return; //stop if returns true
                }
            }
        }
        #endregion

        #region GetRotationAngleBetweenPoints
        public static Vector3 GetRotationAngleBetweenPoints( Vector3 SourcePoint, Vector3 TargetPoint )
        {
            TargetPoint.y = SourcePoint.y;
            Vector3 angle = SourcePoint - TargetPoint;
            if ( angle.sqrMagnitude < 0.1f )
                return Vector3.zero; //too close.  would give wrong results

            float targetY = MathA.AngleXZInDegrees( SourcePoint, TargetPoint ) + 90f; //the extra 90 degrees is to get into the correct rotation frame

            while ( targetY > 360f )
                targetY -= 360f;

            while ( targetY < 0f )
                targetY += 360f;

            return new Vector3( 0, targetY, 0 );
        }
        #endregion
    }
}

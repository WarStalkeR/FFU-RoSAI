using System;

using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{    
    public static class PathingHelper
    {
        #region FindBestUnitLocationNearCoordinates
        public static ISimUnitLocation FindBestUnitLocationNearCoordinates( ISimNPCUnit Unit, Vector3 TargetLocation, CollisionRule Rule )
        {
            if ( Unit == null )
                return null;

            ISimUnitLocation best = null;
            float bestRange = 100000000;

            NPCUnitStance Stance = Unit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = Unit.GetDrawLocation();
            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, TargetLocation, moveRange );
            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    //check buildings
                    foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                    {
                        ISimBuilding simBuild = building?.SimBuilding;
                        if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                            continue;
                        Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                    0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = simBuild;
                            bestRange = range;
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;

                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, Rule, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindBestNPCUnitLocationNearCoordinatesFromSearchSpot
        public static ISimUnitLocation FindBestNPCUnitLocationNearCoordinatesFromSearchSpot( ISimNPCUnit Unit, Vector3 FromSearchSpot, Vector3 TargetLocation,
            bool MustStayInDistrict, bool MustStayInPOI )
        {
            if ( Unit == null )
                return null;

            ISimUnitLocation best = null;
            float bestRange = 100000000;

            NPCUnitStance Stance = Unit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = FromSearchSpot;
            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = Unit.GetMustStayOnGround();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, TargetLocation, moveRange );
            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                MapDistrict district = MustStayInDistrict ? origCell?.ParentTile?.District : null;
                MapPOI poi = MustStayInPOI ? Unit.CalculatePOI() : null;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    if ( MustStayInDistrict )
                    {
                        if ( neighbor.ParentTile.District != district )
                            continue;
                    }
                    if ( MustStayInPOI )
                    {
                        if ( !neighbor.ParentTile.AllPOIs.Contains( poi ) )
                            continue;
                    }

                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                        {
                            if ( MustStayInPOI && building.GetParentPOIOrNull() != poi )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                            if ( best == null || range < bestRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                best = simBuild;
                                bestRange = range;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( MustStayInPOI && outdoorSpot.CalculateLocationPOI() != poi )
                            continue;
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindBestMachineUnitLocationNearCoordinatesFromSearchSpot
        public static ISimUnitLocation FindBestMachineUnitLocationNearCoordinatesFromSearchSpot( ISimMachineUnit Unit, Vector3 FromSearchSpot, 
            Vector3 TargetLocation, float MaxDistanceAllowedFromSpot, int IgnoreClearancesAtOrBelow )
        {
            if ( Unit == null )
                return null;

            ISimUnitLocation bestReal = null;
            float bestRealRange = 100000000;

            ISimUnitLocation bestAnyClearance = null;
            float bestAnyClearanceRange = 100000000;

            Vector3 unitStartingSpot = Unit.GetActualPositionForMovementOrPlacement();

            Vector3 startingLoc = FromSearchSpot;
            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = Unit.GetMustStayOnGround();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, TargetLocation, moveRange );
            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            bool isBadClearance = false;
                            int neededClearance = building.CalculateLocationSecurityClearanceInt();
                            if ( maxBuildingClearance < 5 && neededClearance > maxBuildingClearance && neededClearance > IgnoreClearancesAtOrBelow )
                                isBadClearance = true;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetAreMoreUnitsBlockedFromComingHere() )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( simBuild.GetStatus()?.ShouldBuildingBeNonfunctional ?? false )
                                continue; //don't send us to burned-out ones or ones under construction

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            #region Calculate Lockdown Blockages
                            if ( SimCommon.Lockdowns_MainThreadOnly.Count > 0 )
                            {
                                bool isBlockedByLockdown = false;
                                foreach ( Lockdown lockdown in SimCommon.Lockdowns_MainThreadOnly )
                                {
                                    if ( lockdown == null )
                                        continue;
                                    LockdownType lockdownType = lockdown.Type;
                                    if ( lockdownType == null )
                                        continue;

                                    bool sourceIsInRange = (unitStartingSpot - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;
                                    bool destIsInRange = (loc - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;

                                    if ( sourceIsInRange == destIsInRange )
                                        continue; //not crossing the barrier in either direction, so skip!

                                    if ( !destIsInRange )
                                    {
                                        //moving out of the lockdown area
                                        if ( lockdownType.BlocksPlayerUnitsMovingOut )
                                        {
                                            isBlockedByLockdown = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        //moving into the lockdown area
                                        if ( lockdownType.BlocksPlayerUnitsMovingIn )
                                        {
                                            isBlockedByLockdown = true;
                                            break;
                                        }
                                    }
                                }
                                if ( isBlockedByLockdown )
                                    continue; //if this is blocked by a lockdown, then skip it
                            }
                            #endregion

                            float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                            if ( isBadClearance )
                            {
                                if ( bestAnyClearance == null || range < bestAnyClearanceRange )
                                {
                                    if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                            0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                            ) )
                                        continue; //if we would intersect some existing collidable

                                    bestAnyClearance = simBuild;
                                    bestAnyClearanceRange = range;
                                }
                            }
                            else
                            {
                                if ( bestReal == null || range < bestRealRange )
                                {
                                    if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                            0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                            ) )
                                        continue; //if we would intersect some existing collidable

                                    bestReal = simBuild;
                                    bestRealRange = range;
                                }
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetAreMoreUnitsBlockedFromComingHere() )
                            continue; //if not a valid spot, or blocked by another unit
                        bool isBadClearance = false;
                        int neededClearance = outdoorSpot.CalculateLocationSecurityClearanceInt();
                        if ( maxGroundClearance < 5 && neededClearance > maxGroundClearance && neededClearance > IgnoreClearancesAtOrBelow )
                            isBadClearance = true;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        #region Calculate Lockdown Blockages
                        if ( SimCommon.Lockdowns_MainThreadOnly.Count > 0 )
                        {
                            bool isBlockedByLockdown = false;
                            foreach ( Lockdown lockdown in SimCommon.Lockdowns_MainThreadOnly )
                            {
                                if ( lockdown == null )
                                    continue;
                                LockdownType lockdownType = lockdown.Type;
                                if ( lockdownType == null )
                                    continue;

                                bool sourceIsInRange = (unitStartingSpot - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;
                                bool destIsInRange = (loc - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;

                                if ( sourceIsInRange == destIsInRange )
                                    continue; //not crossing the barrier in either direction, so skip!

                                if ( !destIsInRange )
                                {
                                    //moving out of the lockdown area
                                    if ( lockdownType.BlocksPlayerUnitsMovingOut )
                                    {
                                        isBlockedByLockdown = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    //moving into the lockdown area
                                    if ( lockdownType.BlocksPlayerUnitsMovingIn )
                                    {
                                        isBlockedByLockdown = true;
                                        break;
                                    }
                                }
                            }
                            if ( isBlockedByLockdown )
                                continue; //if this is blocked by a lockdown, then skip it
                        }
                        #endregion

                        float range = (loc - TargetLocation).GetSquareGroundMagnitude();
                        if ( isBadClearance )
                        {
                            if ( bestAnyClearance == null || range < bestAnyClearanceRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                bestAnyClearance = outdoorSpot;
                                bestAnyClearanceRange = range;
                            }
                        }
                        else
                        {
                            if ( bestReal == null || range < bestRealRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                bestReal = outdoorSpot;
                                bestRealRange = range;
                            }
                        }
                    }
                }
            }

            if ( bestReal != null && bestRealRange <= MaxDistanceAllowedFromSpot * MaxDistanceAllowedFromSpot )
                return bestReal;

            if ( bestAnyClearance != null && bestAnyClearanceRange <= MaxDistanceAllowedFromSpot * MaxDistanceAllowedFromSpot )
                return bestAnyClearance;
            return null;
        }
        #endregion

        #region FindBestUnitLocationForNPCUnitChasingTarget
        public static ISimUnitLocation FindBestUnitLocationForNPCUnitChasingTarget( ISimNPCUnit ChaserUnit, ISimMapActor Target, bool MustStayInDistrict, bool MustStayInPOI, NPCUnitActionLogic Logic )
        {
            if ( ChaserUnit == null || Target == null )
                return null;

            if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                ChaserUnit.TurnDumpLines.Add( "FindBestUnitLocationForNPCUnitChasingTarget" );

            ISimUnitLocation bestAny = null;
            ISimUnitLocation bestAboveMin = null;
            float bestAnyRange = 100000000;
            float bestAboveMinRange = 100000000;

            float minRangeToStriveFor = ChaserUnit.GetAttackRange() * 0.4f; //try to be within the midpoint of their own targeting range
            float minRangeToStriveForSquared = minRangeToStriveFor * minRangeToStriveFor;

            Vector3 targetLoc = Target.GetDrawLocation();

            NPCUnitStance Stance = ChaserUnit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = ChaserUnit.GetDrawLocation();
            float moveRange = ChaserUnit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = ChaserUnit?.UnitType?.IsMechStyleMovement ?? false;
            float mustBeLowerThanThisStartingRange = (startingLoc - targetLoc).GetSquareGroundMagnitude();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, targetLoc, moveRange );
            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( curCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                MapDistrict district = MustStayInDistrict ? curCell?.ParentTile?.District : null;
                MapPOI poi = MustStayInPOI ? ChaserUnit.CalculatePOI() : null;

                ChaserUnit.CalculateEffectiveClearances( curCell, out int maxGroundClearance, out int maxBuildingClearance );

                if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                    ChaserUnit.TurnDumpLines.Add( "maxGroundClearance: " + maxGroundClearance + " maxBuildingClearance: " + maxBuildingClearance );

                int numberChecked = 0;
                int numberExistingCollidablesChecked = 0;
                int numberUntilNextTurnCollidablesChecked = 0;
                int numberSinceLastVisibilityGranterCalculation = 0;

                foreach ( MapCell neighbor in curCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    if ( MustStayInDistrict )
                    {
                        if ( neighbor.ParentTile.District != district )
                            continue;
                    }
                    if ( MustStayInPOI )
                    {
                        if ( !neighbor.ParentTile.AllPOIs.Contains( poi ) )
                            continue;
                    }

                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                        {
                            if ( building == null )
                                continue;
                            if ( MustStayInPOI && building.GetParentPOIOrNull() != poi )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            numberChecked++;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there


                            float range = (loc - targetLoc).GetSquareGroundMagnitude();
                            if ( range >= mustBeLowerThanThisStartingRange )
                                continue; //we already are closer than this, so ignore it.
                            if ( bestAboveMin == null || range < bestAboveMinRange )
                            {
                                numberExistingCollidablesChecked += neighbor.CollidablesIntersectingCell.Count;
                                numberUntilNextTurnCollidablesChecked += neighbor.BlockingCollidablesUntilNextTurn.Count;
                                numberSinceLastVisibilityGranterCalculation += CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.Count;
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( ChaserUnit, loc,
                                        0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                if ( range > minRangeToStriveForSquared )
                                {
                                    if ( range < bestAboveMinRange )
                                    {
                                        bestAboveMin = simBuild;
                                        bestAboveMinRange = range;
                                    }
                                }
                                else
                                {
                                    if ( range < bestAnyRange )
                                    {
                                        bestAny = simBuild;
                                        bestAnyRange = range;
                                    }
                                }
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( MustStayInPOI && outdoorSpot.CalculateLocationPOI() != poi )
                            continue;
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        numberChecked++;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - targetLoc).GetSquareGroundMagnitude();
                        if ( range >= mustBeLowerThanThisStartingRange )
                            continue; //we already are closer than this, so ignore it.
                        if ( bestAboveMin == null || range < bestAboveMinRange )
                        {
                            numberExistingCollidablesChecked += neighbor.CollidablesIntersectingCell.Count;
                            numberUntilNextTurnCollidablesChecked += neighbor.BlockingCollidablesUntilNextTurn.Count;
                            numberSinceLastVisibilityGranterCalculation += CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.Count;
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( ChaserUnit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            if ( range > minRangeToStriveForSquared )
                            {
                                if ( range < bestAboveMinRange )
                                {
                                    bestAboveMin = outdoorSpot;
                                    bestAboveMinRange = range;
                                }
                            }
                            else
                            {
                                if ( range < bestAnyRange )
                                {
                                    bestAny = outdoorSpot;
                                    bestAnyRange = range;
                                }
                            }
                        }
                    }
                }

                if ( InputCaching.Debug_DoDumpEveryTurn && Logic == NPCUnitActionLogic.PlanningPerTurn )
                {
                    ChaserUnit.TurnDumpLines.Add( "numberChecked: " + numberChecked );
                    ChaserUnit.TurnDumpLines.Add( "numberExistingCollidablesChecked: " + numberExistingCollidablesChecked );
                    ChaserUnit.TurnDumpLines.Add( "numberUntilNextTurnCollidablesChecked: " + numberUntilNextTurnCollidablesChecked );
                    ChaserUnit.TurnDumpLines.Add( "numberSinceLastVisibilityGranterCalculation: " + numberSinceLastVisibilityGranterCalculation );
                }
            }

            if ( bestAboveMin != null )
                return bestAboveMin;
            return bestAny;
        }
        #endregion

        #region FindBestUnitLocationForNPCUnitFocusedOnBuilding
        public static ISimUnitLocation FindBestUnitLocationForNPCUnitFocusedOnBuilding( ISimNPCUnit ChaserUnit, ISimBuilding FocusedBuilding )
        {
            if ( ChaserUnit == null || FocusedBuilding == null )
                return null;
            MapItem focusedItem = FocusedBuilding.GetMapItem();
            if ( focusedItem == null ) 
                return null;

            ISimUnitLocation best = null;
            float bestRange = 100000000;

            Vector3 targetLoc = focusedItem.CenterPoint;

            NPCUnitStance Stance = ChaserUnit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = ChaserUnit.GetDrawLocation();
            float moveRange = ChaserUnit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = ChaserUnit?.UnitType?.IsMechStyleMovement??false;
            float mustBeLowerThanThisStartingRange = (startingLoc - targetLoc).GetSquareGroundMagnitude();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, targetLoc, moveRange );
            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( curCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                ChaserUnit.CalculateEffectiveClearances( curCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in curCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                        {
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null )
                                continue;

                            bool isTheFocusBuilding = simBuild == FocusedBuilding;
                            if ( !isTheFocusBuilding )
                            {
                                //only care about clearances when it's not the focus building
                                if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                    continue;
                            }
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there


                            if ( isTheFocusBuilding ) //if we found the actual building, and it's open, then go there!
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( ChaserUnit, loc,
                                        0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable
                                return simBuild;
                            }

                            float range = (loc - targetLoc).GetSquareGroundMagnitude();
                            if ( range >= mustBeLowerThanThisStartingRange )
                                continue; //we already are closer than this, so ignore it.

                            if ( best == null || range < bestRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( ChaserUnit, loc,
                                        0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                best = simBuild;
                                bestRange = range;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - targetLoc).GetSquareGroundMagnitude();
                        if ( range >= mustBeLowerThanThisStartingRange )
                            continue; //we already are closer than this, so ignore it.
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( ChaserUnit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindRandomUnitLocationForNPCUnit
        public static ISimUnitLocation FindRandomUnitLocationForNPCUnit( ISimNPCUnit WanderUnit, bool MustStayInDistrict, bool MustStayInPOI, MersenneTwister Rand )
        {
            if ( WanderUnit == null )
                return null;

            ISimUnitLocation randomLocAny = null;
            ISimUnitLocation randomLocAbovePreferred = null;

            NPCUnitStance Stance = WanderUnit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = WanderUnit.GetDrawLocation();
            float moveRange = WanderUnit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            float minDesiredMoveRangeSquared = moveRangeSquared * 0.7f;
            bool mustStayOnGround = WanderUnit?.UnitType?.IsMechStyleMovement ?? false;

            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                MapDistrict district = MustStayInDistrict ? origCell?.ParentTile?.District : null;
                MapPOI poi = MustStayInPOI ? WanderUnit.CalculatePOI() : null;

                WanderUnit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in CityMap.AllNonWastelandCells.GetRandomStartEnumerable( Rand ) )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( MustStayInDistrict )
                    {
                        if ( neighbor.ParentTile.District != district )
                            continue;
                    }
                    if ( MustStayInPOI )
                    {
                        if ( !neighbor.ParentTile.AllPOIs.Contains( poi ) )
                            continue;
                    }

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                        {
                            if ( building == null )
                                continue;
                            if ( MustStayInPOI && building.GetParentPOIOrNull() != poi )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            float distanceSquared = (loc - startingLoc).GetSquareGroundMagnitude();
                            if ( distanceSquared > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            bool alreadyPassed = false;
                            if ( distanceSquared > minDesiredMoveRangeSquared )
                            {
                                if ( randomLocAbovePreferred == null || Rand.Next( 0, 100 ) < 25 )
                                {
                                    if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( WanderUnit, loc,
                                            0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                            ) )
                                        continue; //if we would intersect some existing collidable
                                    alreadyPassed = true;
                                    randomLocAbovePreferred = simBuild;
                                    return simBuild;
                                }
                            }
                            if ( randomLocAny == null || Rand.Next( 0, 100 ) < 25 )
                            {
                                if ( !alreadyPassed )
                                {
                                    if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( WanderUnit, loc,
                                            0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                            ) )
                                        continue; //if we would intersect some existing collidable
                                }
                                randomLocAny = simBuild;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( MustStayInPOI && outdoorSpot.CalculateLocationPOI() != poi )
                            continue;
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        float distanceSquared = (loc - startingLoc).GetSquareGroundMagnitude();
                        if ( distanceSquared > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        bool alreadyPassed = false;
                        if ( distanceSquared > minDesiredMoveRangeSquared )
                        {
                            if ( randomLocAbovePreferred == null || Rand.Next( 0, 100 ) < 25 )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( WanderUnit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable
                                alreadyPassed = true;
                                randomLocAbovePreferred = outdoorSpot;
                                return outdoorSpot;
                            }
                        }
                        if ( randomLocAny == null || Rand.Next( 0, 100 ) < 25 )
                        {
                            if ( !alreadyPassed )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( WanderUnit, loc,
                                        0, CollisionRule.Strict, false//we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable
                            }
                            randomLocAny = outdoorSpot;
                        }
                    }
                }
            }

            if ( randomLocAbovePreferred != null )
                return randomLocAbovePreferred;
            return randomLocAny;
        }
        #endregion

        #region FindClosestNearbyBuildingOfTagForNPC
        public static ISimBuilding FindClosestNearbyBuildingOfTagForNPC( ISimNPCUnit NPCUnit, BuildingTag Tag )
        {
            if ( NPCUnit == null || Tag == null )
                return null;

            ISimBuilding best = null;
            float bestRange = 100000000;

            NPCUnitStance Stance = NPCUnit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                foreach ( MapCell neighbor in curCell.AdjacentCellsAndSelfIncludingDiagonal2x )
                {
                    foreach ( MapItem building in neighbor.BuildingList.GetDisplayList() )
                    {
                        ISimBuilding simBuild = building?.SimBuilding;
                        if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( !simBuild.GetVariant().Tags.ContainsKey( Tag.ID ) )
                            continue; //if not the correct tag, then move along

                        if ( maxPOIClearance < 5 )
                        {
                            //if this is in a poi with higher clearance than we should be going for, then ignore it.
                            //unless this building IS the poi, in which case go for it.
                            MapPOI poi = simBuild.CalculateLocationPOI();
                            if ( poi != null && !poi.IsThisPOIForASingleBuilding &&
                                (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                                continue;
                        }

                        Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                        float range = (loc - startingLoc).GetSquareGroundMagnitude();
                        if ( best == null || range < bestRange )
                        {
                            best = simBuild;
                            bestRange = range;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindRandomNearbyBuildingOfTagForNPC
        public static ISimBuilding FindRandomNearbyBuildingOfTagForNPC( ISimNPCUnit NPCUnit, BuildingTag Tag, MersenneTwister Rand )
        {
            if ( NPCUnit == null || Tag == null )
                return null;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();
            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnitStance Stance = NPCUnit.Stance;
                if ( Stance == null )
                    return null;

                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                foreach ( MapCell neighbor in curCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
                {
                    foreach ( MapItem building in neighbor.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        ISimBuilding simBuild = building?.SimBuilding;
                        if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( !simBuild.GetVariant().Tags.ContainsKey( Tag.ID ) )
                            continue; //if not the correct tag, then move along

                        if ( maxPOIClearance < 5 )
                        {
                            //if this is in a poi with higher clearance than we should be going for, then ignore it.
                            //unless this building IS the poi, in which case go for it.
                            MapPOI poi = simBuild.CalculateLocationPOI();
                            if ( poi != null && !poi.IsThisPOIForASingleBuilding &&
                                (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                                continue;
                        }
                        //hooray, this is our random choice
                        return simBuild;
                    }
                }
            }
            return null; //we didn't find anything!
        }
        #endregion

        #region FindClosestGlobalBuildingOfTagForNPC
        public static ISimBuilding FindClosestGlobalBuildingOfTagForNPC( ISimNPCUnit NPCUnit, BuildingTag Tag, 
            bool UseAnyHomeDistrictAndPOIRestrictions, int MinimumDistanceObjectiveMustBeFromCurrentLocation )
        {
            if ( NPCUnit == null || Tag == null )
                return null;

            ISimBuilding best = null;
            float bestRange = 100000000;

            float minDistanceSquared = MinimumDistanceObjectiveMustBeFromCurrentLocation * MinimumDistanceObjectiveMustBeFromCurrentLocation;

            NPCUnitStance Stance = NPCUnit.Stance;
            if ( Stance == null )
                return null;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                List<ISimBuilding> buildingsWithTag = Tag.DuringGame_Buildings.GetDisplayList();

                NPCUnitStance stance = NPCUnit.Stance;
                bool stayInDistrict = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToDistrict ?? false);
                bool stayInPOI = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToPOI ?? false);
                MapDistrict districtToStayIn = stayInDistrict ? NPCUnit.HomeDistrict : null;
                MapPOI poiToStayIn = stayInPOI ? NPCUnit.HomePOI : null;

                foreach ( ISimBuilding simBuild in buildingsWithTag )
                {
                    if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                        continue; //if not a valid spot, or blocked by another unit

                    if ( maxPOIClearance < 5 )
                    {
                        //if this is in a poi with higher clearance than we should be going for, then ignore it.
                        //unless this building IS the poi, in which case go for it.
                        MapPOI poi = simBuild.CalculateLocationPOI();
                        if ( poi != null && !poi.IsThisPOIForASingleBuilding &&
                            (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                            continue;
                    }

                    if ( poiToStayIn != null )
                    {
                        if ( simBuild.CalculateLocationPOI() != poiToStayIn )
                            continue;
                    }

                    if ( districtToStayIn != null )
                    {
                        if ( simBuild.GetParentDistrict() != districtToStayIn )
                            continue;
                    }

                    Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                    float range = (loc - startingLoc).GetSquareGroundMagnitude();

                    if ( range < minDistanceSquared )
                        continue;

                    if ( best == null || range < bestRange )
                    {
                        best = simBuild;
                        bestRange = range;
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindRandomGlobalBuildingOfTagForNPCWithinRange
        public static ISimBuilding FindRandomGlobalBuildingOfTagForNPCWithinRange( ISimNPCUnit NPCUnit, BuildingTag Tag, bool CanIncludeMachineStructure, MersenneTwister Rand,
            bool UseAnyHomeDistrictAndPOIRestrictions, int AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation, 
            int PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation, int PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation,
            int AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
            bool UseActivityCenterForPreferredDistancesIfRelevant, bool UseParentMissionCenterForPreferredDistancesIfRelevant, 
            bool TargetBuildingMustHaveNoSwarm, Swarm TargetBuildingCanHaveThisSwarm,
            bool TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, bool TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
            int TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, int TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
            bool TargetClosestPossibleBuilding )
        {
            if ( NPCUnit == null || Tag == null )
                return null;

            ISimBuilding bestAlternative = null;
            ISimBuilding bestReal = null;
            float bestAlternativeDistance = 999999;
            float bestRealDistance = 999999;
            int timesSetAlternative = 0;
            int timesSetReal = 0;

            float absoluteMinDistanceSquared = AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float absoluteMaxDistanceSquared = AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMinDistanceSquared = PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation * PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMaxDistanceSquared = PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation * PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();
            bool setStartingLocation = false;
            if ( UseParentMissionCenterForPreferredDistancesIfRelevant )
            {
                NPCMission mission = NPCUnit.ParentManager?.PeriodicData?.GateByCity?.RequiredNPCMissionActive;
                if ( mission != null )
                {
                    if ( mission.DuringGameplay_StartedAtBuilding != null )
                    {
                        startingLoc = mission.DuringGameplay_StartedAtBuilding?.GetMapItem()?.CenterPoint ?? startingLoc;
                        setStartingLocation = true;
                    }
                }
            }

            if ( !setStartingLocation && UseActivityCenterForPreferredDistancesIfRelevant && NPCUnit.ManagerStartLocation.Get() != null )
                startingLoc = NPCUnit.ManagerStartLocation.Get()?.GetMapItem()?.CenterPoint ?? startingLoc;

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                List<ISimBuilding> buildingsWithTag = Tag.DuringGame_Buildings.GetDisplayList();

                NPCUnitStance stance = NPCUnit.Stance;
                if ( stance == null )
                    return null;

                bool stayInDistrict = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToDistrict ?? false);
                bool stayInPOI = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToPOI ?? false);
                MapDistrict districtToStayIn = stayInDistrict ? NPCUnit.HomeDistrict : null;
                MapPOI poiToStayIn = stayInPOI ? NPCUnit.HomePOI : null;

                foreach ( ISimBuilding simBuild in buildingsWithTag.GetRandomStartEnumerable( Rand ) )
                {
                    if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( stance ) )
                        continue; //if not a valid spot, or blocked by another unit

                    if ( !CanIncludeMachineStructure && simBuild.MachineStructureInBuilding != null )
                        continue; //if has a structure and can not

                    if ( TargetBuildingMustHaveNoSwarm && simBuild.SwarmSpread != null )
                    {
                        if ( TargetBuildingCanHaveThisSwarm == null || simBuild.SwarmSpread != TargetBuildingCanHaveThisSwarm )
                            continue; //if has a swarm and should not, skip
                    }

                    if ( TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsViolentCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsPeacefulCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.LowerClassCitizenGrabCount < TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast )
                            continue;
                    }

                    if ( TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.UpperClassCitizenGrabCount < TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast )
                            continue;
                    }

                    Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();
                    float range = (loc - startingLoc).GetSquareGroundMagnitude();

                    if ( range < absoluteMinDistanceSquared || range > absoluteMaxDistanceSquared )
                        continue;

                    if ( maxPOIClearance < 5 )
                    {
                        //if this is in a poi with higher clearance than we should be going for, then ignore it.
                        //unless this building IS the poi, in which case go for it.
                        MapPOI poi = simBuild.CalculateLocationPOI();
                        if ( poi != null && !poi.IsThisPOIForASingleBuilding &&
                            (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                            continue;
                    }

                    if ( poiToStayIn != null )
                    {
                        if ( simBuild.CalculateLocationPOI() != poiToStayIn )
                            continue;
                    }

                    if ( districtToStayIn != null )
                    {
                        if ( simBuild.GetParentDistrict() != districtToStayIn )
                            continue;
                    }

                    if ( TargetClosestPossibleBuilding )
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || range < bestRealDistance )
                            {
                                bestReal = simBuild;
                                bestRealDistance = range;
                            }
                        }

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || range < bestAlternativeDistance )
                            {
                                bestAlternative = simBuild;
                                bestAlternativeDistance = range;
                            }
                        }
                    }
                    else
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || Rand.Next( 0, 100 ) < timesSetReal * 10 )
                            {
                                bestReal = simBuild;
                                timesSetReal++;

                                if ( Rand.Next( 0, 100 ) < timesSetReal * 10 )
                                    return bestReal;
                            }
                        }

                        if ( bestReal != null )
                            continue;

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || Rand.Next( 0, 100 ) < timesSetAlternative * 10 )
                            {
                                bestAlternative = simBuild;
                                if ( timesSetAlternative < 5 )
                                    timesSetAlternative++;
                            }
                        }
                    }
                }
            }

            if ( bestReal != null )
                return bestReal;

            return bestAlternative;
        }
        #endregion

        #region FindRandomGlobalBuildingOfPOITagForNPCWithinRange
        public static ISimBuilding FindRandomGlobalBuildingOfPOITagForNPCWithinRange( ISimNPCUnit NPCUnit, POITag POITag, BuildingTag BuildingTag, bool CanIncludeMachineStructure, MersenneTwister Rand,
            bool UseAnyHomeDistrictAndPOIRestrictions, int AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation,
            int PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation, int PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation,
            int AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
            bool UseActivityCenterForPreferredDistancesIfRelevant, bool UseParentMissionCenterForPreferredDistancesIfRelevant,
            bool POIMustBeAbleToAcceptReinforcementsToGetToMinimum, bool POIMustBeAbleToAcceptAnyReinforcementsBelowMaximum, 
            bool TargetBuildingMustHaveNoSwarm, Swarm TargetBuildingCanHaveThisSwarm,
            bool TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, bool TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
            int TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, int TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
            bool TargetClosestPossibleBuilding )
        {
            if ( NPCUnit == null || POITag == null )
                return null;

            ISimBuilding bestAlternative = null;
            ISimBuilding bestReal = null;
            float bestAlternativeDistance = 999999;
            float bestRealDistance = 999999;
            int timesSetAlternative = 0;
            int timesSetReal = 0;

            float absoluteMinDistanceSquared = AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float absoluteMaxDistanceSquared = AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMinDistanceSquared = PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation * PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMaxDistanceSquared = PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation * PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();
            bool setStartingLocation = false;
            if ( UseParentMissionCenterForPreferredDistancesIfRelevant )
            {
                NPCMission mission = NPCUnit.ParentManager?.PeriodicData?.GateByCity?.RequiredNPCMissionActive;
                if ( mission != null )
                {
                    if ( mission.DuringGameplay_StartedAtBuilding != null )
                    {
                        startingLoc = mission.DuringGameplay_StartedAtBuilding?.GetMapItem()?.CenterPoint ?? startingLoc;
                        setStartingLocation = true;
                    }
                }
            }

            if ( !setStartingLocation && UseActivityCenterForPreferredDistancesIfRelevant && NPCUnit.ManagerStartLocation.Get() != null )
                startingLoc = NPCUnit.ManagerStartLocation.Get()?.GetMapItem()?.CenterPoint ?? startingLoc;

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                List<ISimBuilding> buildingsWithTag = POITag.DuringGame_POIBuildings.GetDisplayList();

                NPCUnitStance stance = NPCUnit.Stance;
                if ( stance == null )
                    return null;

                bool stayInDistrict = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToDistrict ?? false);
                bool stayInPOI = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToPOI ?? false);
                MapDistrict districtToStayIn = stayInDistrict ? NPCUnit.HomeDistrict : null;
                MapPOI poiToStayIn = stayInPOI ? NPCUnit.HomePOI : null;

                foreach ( ISimBuilding simBuild in buildingsWithTag.GetRandomStartEnumerable( Rand ) )
                {
                    if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( stance ) )
                        continue; //if not a valid spot, or blocked by another unit

                    if ( !CanIncludeMachineStructure && simBuild.MachineStructureInBuilding != null )
                        continue; //if has a structure and can not

                    if ( TargetBuildingMustHaveNoSwarm && simBuild.SwarmSpread != null )
                    {
                        if ( TargetBuildingCanHaveThisSwarm == null || simBuild.SwarmSpread != TargetBuildingCanHaveThisSwarm )
                            continue; //if has a swarm and should not, skip
                    }

                    if ( TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsViolentCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsPeacefulCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.LowerClassCitizenGrabCount < TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast )
                            continue;
                    }

                    if ( TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.UpperClassCitizenGrabCount < TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast )
                            continue;
                    }

                    Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();
                    float range = (loc - startingLoc).GetSquareGroundMagnitude();

                    if ( range < absoluteMinDistanceSquared || range > absoluteMaxDistanceSquared )
                        continue;

                    MapPOI poi = simBuild.CalculateLocationPOI();
                    if ( poi == null )
                        continue; //uh...
                    
                    if ( POIMustBeAbleToAcceptReinforcementsToGetToMinimum )
                    {
                        if ( poi.IsBlockedFromBeingTargetedForReinforcementsForXMoreTurns > 0 )
                            continue;
                        if ( poi.CurrentlyShortThisManyGuards <= 0 )
                            continue;
                    }
                    if ( POIMustBeAbleToAcceptAnyReinforcementsBelowMaximum )
                    {
                        if ( poi.IsBlockedFromBeingTargetedForReinforcementsForXMoreTurns > 0 )
                            continue;
                        if ( poi.CouldHaveThisManyMoreGuardsAtMost <= 0 )
                            continue;
                    }

                    if ( BuildingTag != null )
                    {
                        BuildingTypeVariant variant = simBuild.GetVariant();
                        if ( !variant.Tags.ContainsKey( BuildingTag.ID ) )
                            continue;
                    }    

                    if ( maxPOIClearance < 5 )
                    {
                        //if this is in a poi with higher clearance than we should be going for, then ignore it.
                        //unless this building IS the poi, in which case go for it.
                        if ( !poi.IsThisPOIForASingleBuilding && (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                            continue;
                    }

                    if ( poiToStayIn != null )
                    {
                        if ( simBuild.CalculateLocationPOI() != poiToStayIn )
                            continue;
                    }

                    if ( districtToStayIn != null )
                    {
                        if ( simBuild.GetParentDistrict() != districtToStayIn )
                            continue;
                    }

                    if ( TargetClosestPossibleBuilding )
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || range < bestRealDistance )
                            {
                                bestReal = simBuild;
                                bestRealDistance = range;
                            }
                        }

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || range < bestAlternativeDistance )
                            {
                                bestAlternative = simBuild;
                                bestAlternativeDistance = range;
                            }
                        }
                    }
                    else
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || Rand.Next( 0, 100 ) < timesSetReal * 10 )
                            {
                                bestReal = simBuild;
                                timesSetReal++;

                                if ( Rand.Next( 0, 100 ) < timesSetReal * 10 )
                                    return bestReal;
                            }
                        }

                        if ( bestReal != null )
                            continue;

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || Rand.Next( 0, 100 ) < timesSetAlternative * 10 )
                            {
                                bestAlternative = simBuild;
                                if ( timesSetAlternative < 5 )
                                    timesSetAlternative++;
                            }
                        }
                    }
                }
            }

            if ( bestReal != null )
                return bestReal;

            return bestAlternative;
        }
        #endregion

        #region FindRandomGlobalBuildingOfDistrictTagForNPCWithinRange
        public static ISimBuilding FindRandomGlobalBuildingOfDistrictTagForNPCWithinRange( ISimNPCUnit NPCUnit, DistrictTag DistrictTag, BuildingTag BuildingTag, bool CanIncludeMachineStructure, MersenneTwister Rand,
            bool UseAnyHomeDistrictAndPOIRestrictions, int AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation,
            int PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation, int PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation,
            int AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation,
            bool UseActivityCenterForPreferredDistancesIfRelevant, bool UseParentMissionCenterForPreferredDistancesIfRelevant, 
            bool TargetBuildingMustHaveNoSwarm, Swarm TargetBuildingCanHaveThisSwarm,
            bool TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell, bool TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell,
            int TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast, int TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast,
            bool TargetClosestPossibleBuilding )
        {
            if ( NPCUnit == null || DistrictTag == null )
                return null;

            ISimBuilding bestAlternative = null;
            ISimBuilding bestReal = null;
            float bestAlternativeDistance = 999999;
            float bestRealDistance = 999999;
            int timesSetAlternative = 0;
            int timesSetReal = 0;

            float absoluteMinDistanceSquared = AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float absoluteMaxDistanceSquared = AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation * AbsoluteMaximumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMinDistanceSquared = PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation * PreferredMinimumDistanceObjectiveMustBeFromCurrentLocation;
            float preferredMaxDistanceSquared = PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation * PreferredMaximumDistanceObjectiveMustBeFromCurrentLocation;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();
            bool setStartingLocation = false;
            if ( UseParentMissionCenterForPreferredDistancesIfRelevant )
            {
                NPCMission mission = NPCUnit.ParentManager?.PeriodicData?.GateByCity?.RequiredNPCMissionActive;
                if ( mission != null )
                {
                    if ( mission.DuringGameplay_StartedAtBuilding != null )
                    {
                        startingLoc = mission.DuringGameplay_StartedAtBuilding?.GetMapItem()?.CenterPoint?? startingLoc;
                        setStartingLocation = true;
                    }
                }
            }

            if ( !setStartingLocation && UseActivityCenterForPreferredDistancesIfRelevant && NPCUnit.ManagerStartLocation.Get() != null )
                startingLoc = NPCUnit.ManagerStartLocation.Get()?.GetMapItem()?.CenterPoint ?? startingLoc;

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                List<ISimBuilding> buildingsWithTag = DistrictTag.DuringGame_Buildings.GetDisplayList();

                NPCUnitStance stance = NPCUnit.Stance;
                if ( stance == null )
                    return null;

                bool stayInDistrict = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToDistrict ?? false);
                bool stayInPOI = UseAnyHomeDistrictAndPOIRestrictions && (stance?.IsContainedToPOI ?? false);
                MapDistrict districtToStayIn = stayInDistrict ? NPCUnit.HomeDistrict : null;
                MapPOI poiToStayIn = stayInPOI ? NPCUnit.HomePOI : null;

                foreach ( ISimBuilding simBuild in buildingsWithTag.GetRandomStartEnumerable( Rand ) )
                {
                    if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( stance ) )
                        continue; //if not a valid spot, or blocked by another unit

                    if ( !CanIncludeMachineStructure && simBuild.MachineStructureInBuilding != null )
                        continue; //if has a structure and can not

                    if ( TargetBuildingMustHaveNoSwarm && simBuild.SwarmSpread != null )
                    {
                        if ( TargetBuildingCanHaveThisSwarm == null || simBuild.SwarmSpread != TargetBuildingCanHaveThisSwarm )
                            continue; //if has a swarm and should not, skip
                    }

                    if ( TargetBuildingMustBeViolentCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsViolentCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustBePeacefulCyberocracyTargetOnCyberocracyCell )
                    {
                        if ( !simBuild.IsPeacefulCyberocracyTarget || !(simBuild.GetParentCell()?.IsPotentialCyberocracyCell?.Display ?? false) )
                            continue;
                    }

                    if ( TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.LowerClassCitizenGrabCount < TargetBuildingMustHaveLowerClassCitizenCountOfAtLeast )
                            continue;
                    }

                    if ( TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast > 0 )
                    {
                        if ( simBuild.UpperClassCitizenGrabCount < TargetBuildingMustHaveUpperClassCitizenCountOfAtLeast )
                            continue;
                    }

                    Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();
                    float range = (loc - startingLoc).GetSquareGroundMagnitude();

                    if ( range < absoluteMinDistanceSquared || range > absoluteMaxDistanceSquared )
                        continue;

                    if ( BuildingTag != null )
                    {
                        BuildingTypeVariant variant = simBuild.GetVariant();
                        if ( !variant.Tags.ContainsKey( BuildingTag.ID ) )
                            continue;
                    }

                    if ( maxPOIClearance < 5 )
                    {
                        //if this is in a poi with higher clearance than we should be going for, then ignore it.
                        //unless this building IS the poi, in which case go for it.
                        MapPOI poi = simBuild.CalculateLocationPOI();
                        if ( poi != null && !poi.IsThisPOIForASingleBuilding && (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                            continue;
                    }

                    if ( poiToStayIn != null )
                    {
                        if ( simBuild.CalculateLocationPOI() != poiToStayIn )
                            continue;
                    }

                    if ( districtToStayIn != null )
                    {
                        if ( simBuild.GetParentDistrict() != districtToStayIn )
                            continue;
                    }

                    if ( TargetClosestPossibleBuilding )
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || range < bestRealDistance )
                            {
                                bestReal = simBuild;
                                bestRealDistance = range;
                            }
                        }

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || range < bestAlternativeDistance )
                            {
                                bestAlternative = simBuild;
                                bestAlternativeDistance = range;
                            }
                        }
                    }
                    else
                    {
                        //hey, we found one in the best range!
                        if ( range >= preferredMinDistanceSquared && range <= preferredMaxDistanceSquared )
                        {
                            if ( bestReal == null || Rand.Next( 0, 100 ) < timesSetReal * 10 )
                            {
                                bestReal = simBuild;
                                timesSetReal++;

                                if ( Rand.Next( 0, 100 ) < timesSetReal * 10 )
                                    return bestReal;
                            }
                        }

                        if ( bestReal != null )
                            continue;

                        //we did not find one in the best range, but here is a partial result
                        if ( bestAlternative == null || range >= preferredMinDistanceSquared || range <= preferredMaxDistanceSquared )
                        {
                            if ( bestAlternative == null || Rand.Next( 0, 100 ) < timesSetAlternative * 10 )
                            {
                                bestAlternative = simBuild;
                                if ( timesSetAlternative < 5 )
                                    timesSetAlternative++;
                            }
                        }
                    }
                }
            }

            if ( bestReal != null )
                return bestReal;

            return bestAlternative;
        }
        #endregion

        #region FindRandomGlobalBuildingOfTagForNPC
        public static ISimBuilding FindRandomGlobalBuildingOfTagForNPC( ISimNPCUnit NPCUnit, BuildingTag Tag, MersenneTwister Rand )
        {
            if ( NPCUnit == null || Tag == null )
                return null;

            Vector3 startingLoc = NPCUnit.GetDrawLocation();
            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                NPCUnitStance Stance = NPCUnit.Stance;
                if ( Stance == null )
                    return null;

                NPCUnit.CalculateEffectiveClearances( curCell, out int maxPOIClearance, out _ );

                List<ISimBuilding> buildingsWithTag = Tag.DuringGame_Buildings.GetDisplayList();
                foreach ( MapItem building in buildingsWithTag.GetRandomStartEnumerable( Rand ) )
                {
                    ISimBuilding simBuild = building?.SimBuilding;
                    if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                        continue; //if not a valid spot, or blocked by another unit

                    if ( maxPOIClearance < 5 )
                    {
                        //if this is in a poi with higher clearance than we should be going for, then ignore it.
                        //unless this building IS the poi, in which case go for it.
                        MapPOI poi = simBuild.CalculateLocationPOI();
                        if ( poi != null && !poi.IsThisPOIForASingleBuilding &&
                            (poi.Type?.RequiredClearance?.Level ?? 0) > maxPOIClearance )
                            continue;
                    }
                    //hooray, this is our random choice
                    return simBuild;
                }
            }
            return null; //we didn't find anything!
        }
        #endregion

        #region FindBestUnitLocationTowardsPOI
        public static ISimUnitLocation FindBestUnitLocationTowardsPOI( ISimNPCUnit Unit, MapPOI POI, MersenneTwister Rand )
        {
            if ( Unit == null || POI == null )
                return null;

            ISimUnitLocation best = null;
            float bestRange = 100000000;

            Vector3 targetLocation = POI.GetCenter();
            Vector3 startingLoc = Unit.GetDrawLocation();
            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = Unit.GetMustStayOnGround();
            float mustBeLowerThanThisStartingRange = (startingLoc - targetLocation).GetSquareGroundMagnitude();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, targetLocation, moveRange );
            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                NPCUnitStance Stance = Unit.Stance;
                if ( Stance == null )
                    return null;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                        {
                            if ( building == null )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            float range = (loc - targetLocation).GetSquareGroundMagnitude();
                            if ( range >= mustBeLowerThanThisStartingRange )
                                continue; //we already are closer than this, so ignore it.
                            if ( best == null || range < bestRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                best = simBuild;
                                bestRange = range;

                                if ( best.CalculateLocationPOI() == POI )
                                    return best;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - targetLocation).GetSquareGroundMagnitude();
                        if ( range >= mustBeLowerThanThisStartingRange )
                            continue; //we already are closer than this, so ignore it.
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;

                            if ( best.CalculateLocationPOI() == POI )
                                return best;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindBestUnitLocationTowardsDistrict
        public static ISimUnitLocation FindBestUnitLocationTowardsDistrict( ISimNPCUnit Unit, MapDistrict District, MersenneTwister Rand )
        {
            if ( Unit == null || District == null || District.Cells.Count == 0 )
                return null;


            ISimUnitLocation best = null;
            float bestRange = 100000000;

            Vector3 startingLoc = Unit.GetDrawLocation();
            Vector3 targetLocation = District.Cells[0].Center;

            #region Find Closest Cell
            {
                float bestTargetDist = (startingLoc - targetLocation).GetSquareGroundMagnitude();

                for ( int i = 1; i < District.Cells.Count; i++ )
                {
                    Vector3 centerLoc = District.Cells[i].Center;
                    float dist = (startingLoc - centerLoc).GetSquareGroundMagnitude();
                    if ( dist < bestTargetDist )
                    {
                        targetLocation = centerLoc;
                        bestTargetDist = dist;
                    }
                }
            }
            #endregion

            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = Unit.GetMustStayOnGround();
            float mustBeLowerThanThisStartingRange = (startingLoc - targetLocation).GetSquareGroundMagnitude();

            Vector3 searchSpot = Vector3.MoveTowards( startingLoc, targetLocation, moveRange );
            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( searchSpot );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                NPCUnitStance Stance = Unit.Stance;
                if ( Stance == null )
                    return null;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                        {
                            if ( building == null )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            float range = (loc - targetLocation).GetSquareGroundMagnitude();
                            if ( range >= mustBeLowerThanThisStartingRange )
                                continue; //we already are closer than this, so ignore it.
                            if ( best == null || range < bestRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                best = simBuild;
                                bestRange = range;

                                if ( best.GetLocationDistrict() == District )
                                    return best;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - targetLocation).GetSquareGroundMagnitude();
                        if ( range >= mustBeLowerThanThisStartingRange )
                            continue; //we already are closer than this, so ignore it.
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;

                            if ( best.GetLocationDistrict() == District )
                                return best;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindBestRandomUnitLocationTowardsSquaredRadiusAroundPoint
        public static ISimUnitLocation FindBestRandomUnitLocationTowardsSquaredRadiusAroundPoint( ISimNPCUnit Unit, float SquaredTargetRadius, Vector3 TargetPoint, MersenneTwister Rand )
        {
            if ( Unit == null )
                return null;

            ISimUnitLocation best = null;
            float bestRange = 100000000;

            Vector3 startingLoc = Unit.GetDrawLocation();

            float moveRange = Unit.GetMovementRange();
            float moveRangeSquared = moveRange * moveRange;
            bool mustStayOnGround = Unit.GetMustStayOnGround();
            float mustBeLowerThanThisStartingRange = (startingLoc - TargetPoint).GetSquareGroundMagnitude();

            MapCell origCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( origCell != null )
            {
                float minX = startingLoc.x - moveRange;
                float maxX = startingLoc.x + moveRange;
                float minZ = startingLoc.z - moveRange;
                float maxZ = startingLoc.z + moveRange;

                NPCUnitStance Stance = Unit.Stance;
                if ( Stance == null )
                    return null;

                Unit.CalculateEffectiveClearances( origCell, out int maxGroundClearance, out int maxBuildingClearance );

                foreach ( MapCell neighbor in origCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;
                    if ( rect.XMax <= minX || rect.XMin >= maxX ||
                        rect.YMax <= minZ || rect.YMin >= maxZ )
                        continue; //this whole cell is out of range, so ignore it!

                    if ( !mustStayOnGround )
                    {
                        //check buildings
                        foreach ( MapItem building in neighbor.BuildingList.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                        {
                            if ( building == null )
                                continue;
                            if ( maxBuildingClearance < 5 && building.CalculateLocationSecurityClearanceInt() > maxBuildingClearance )
                                continue;
                            ISimBuilding simBuild = building?.SimBuilding;
                            if ( simBuild == null || simBuild.GetIsNPCBlockedFromComingHere( Stance ) )
                                continue; //if not a valid spot, or blocked by another unit
                            Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                            if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                                continue; //if out of our action range, we can't go there

                            float range = (loc - TargetPoint).GetSquareGroundMagnitude();
                            if ( range >= mustBeLowerThanThisStartingRange )
                                continue; //we already are closer than this, so ignore it.
                            if ( best == null || range < bestRange )
                            {
                                if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, loc,
                                        0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                        ) )
                                    continue; //if we would intersect some existing collidable

                                best = simBuild;
                                bestRange = range;

                                if ( range <= SquaredTargetRadius )
                                    return best;
                            }
                        }
                    }

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots.GetRandomStartEnumerable( Rand ) )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetIsNPCBlockedFromComingHere( Stance ) )
                            continue; //if not a valid spot, or blocked by another unit
                        if ( maxGroundClearance < 5 && outdoorSpot.CalculateLocationSecurityClearanceInt() > maxGroundClearance )
                            continue;
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        if ( (loc - startingLoc).GetSquareGroundMagnitude() > moveRangeSquared )
                            continue; //if out of our action range, we can't go there

                        float range = (loc - TargetPoint).GetSquareGroundMagnitude();
                        if ( range >= mustBeLowerThanThisStartingRange )
                            continue; //we already are closer than this, so ignore it.
                        if ( best == null || range < bestRange )
                        {
                            if ( neighbor.CalculateIfCollidableWouldIntersectAnotherCollidableHere( Unit, outdoorSpot.GetEffectiveWorldLocationForContainedUnit(),
                                    0, CollisionRule.Strict, false //we will ignore rotation, as this only applies to sub-collidables
                                    ) )
                                continue; //if we would intersect some existing collidable

                            best = outdoorSpot;
                            bestRange = range;

                            if ( range <= SquaredTargetRadius )
                                return best;
                        }
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindClosestGlobalBuildingOfTag_NotForNPC
        public static ISimBuilding FindClosestGlobalBuildingOfTag_NotForNPC( Vector3 SearchStart, BuildingTag Tag )
        {
            if ( Tag == null )
                return null;

            ISimBuilding best = null;
            float bestRange = 100000000;

            Vector3 startingLoc = SearchStart;

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                List<ISimBuilding> buildingsWithTag = Tag.DuringGame_Buildings.GetDisplayList();

                foreach ( ISimBuilding simBuild in buildingsWithTag )
                {
                    if ( simBuild == null || simBuild.GetAreMoreUnitsBlockedFromComingHere() )
                        continue; //if not a valid spot, or blocked by another unit

                    Vector3 loc = simBuild.GetEffectiveWorldLocationForContainedUnit();

                    float range = (loc - startingLoc).GetSquareGroundMagnitude();

                    if ( best == null || range < bestRange )
                    {
                        best = simBuild;
                        bestRange = range;
                    }
                }
            }

            return best;
        }
        #endregion

        #region FindClosestUnoccupiedGroundSpotToLocation_NotForNPC
        public static MapOutdoorSpot FindClosestUnoccupiedGroundSpotToLocation_NotForNPC( Vector3 SearchFrom )
        {
            MapOutdoorSpot best = null;
            float bestRange = 100000000;

            Vector3 startingLoc = SearchFrom;

            MapCell curCell = CityMap.TryGetWorldCellAtCoordinates( startingLoc );
            if ( curCell != null )
            {
                foreach ( MapCell neighbor in curCell.AdjacentCellsAndSelf )
                {
                    ArcenFloatRectangle rect = neighbor.CellRect;

                    //check outdoor spots
                    foreach ( MapOutdoorSpot outdoorSpot in neighbor.AllOutdoorSpots )
                    {
                        if ( outdoorSpot == null || outdoorSpot.GetAreMoreUnitsBlockedFromComingHere() )
                            continue; //if not a valid spot, or blocked by another unit
                        Vector3 loc = outdoorSpot.GetEffectiveWorldLocationForContainedUnit();

                        float range = (loc - startingLoc).GetSquareGroundMagnitude();
                        if ( best == null || range < bestRange )
                        {
                            best = outdoorSpot;
                            bestRange = range;
                        }
                    }
                }
            }

            return best;
        }
        #endregion
    }
}

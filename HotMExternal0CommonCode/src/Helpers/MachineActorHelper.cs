using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public static class MachineActorHelper
    {
        #region GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Vehicle
        public static bool GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Vehicle( NPCCohort FromCohort, ISimBuilding BuildingOrNull, Vector3 Location, 
            bool WillHaveDoneAttack, bool SkipInconspicuousCheck, 

            bool IsCloaked, int AggroAmount, int IsUnremarkableAnywhereUpToClearanceInt )
        {
            //note, if we are considering this, then we don't care about being dead, invisible, since all of that would be not-true for a theoretical move.

            if ( IsCloaked && !WillHaveDoneAttack ) //any attack at all will take us out of stealth
                return true;
            if ( SkipInconspicuousCheck )
                return false;
            if ( AggroAmount > 0 )
                return false; //if we have aggroed that group, then no hiding from them

            int unremarkableUpTo = IsUnremarkableAnywhereUpToClearanceInt; //we will ignore the building variant for vehicles, since they cannot be at buildings
            if ( unremarkableUpTo >= 0 ) //if this is less than zero, doesn't matter what our clearance is, we're being hunted
            {
                Vector3 drawLocation = Location;
                MapTile tile = CityMap.TryGetWorldCellAtCoordinates( drawLocation )?.ParentTile;
                if ( tile == null )
                    return true; //we are off the map somehow

                int effectiveClearance = IsUnremarkableAnywhereUpToClearanceInt;

                foreach ( MapPOI poi in tile.RestrictedPOIs )
                {
                    if ( poi == null || poi.HasBeenDestroyed )
                        continue;
                    ArcenFloatRectangle rect = poi.GetOuterRect();
                    if ( !rect.ContainsPointXZ( drawLocation ) )
                        continue; //destination not in here, so nevermind

                    SecurityClearance clearance = poi.Type.RequiredClearance;
                    if ( clearance != null && clearance.Level > effectiveClearance )
                        return false; //this poi has a higher security clearance than us, so shoot at us
                }

                return true;
            }
            else //shoot us, we're a scary machine!
                return false;
        }
        #endregion

        #region GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Unit
        public static bool GetIsAbleToAvoidAutoTargetingShotAtAtProposedLocation_Unit( NPCCohort FromCohort, ISimBuilding BuildingOrNull, Vector3 Location, 
            bool WillHaveDoneAttack, bool SkipInconspicuousCheck,

            bool IsCloaked, int AggroAmount, bool IsUnremarkableRightNow_NotCountingActualClearanceChecks, int EffectiveClearance )
        {
            //note, we do not care about our hiding status, because we'd be moving
            //similarly, if we are considering this, then we don't care about being dead, invisible, or not-deployed, since all of that would be not-true for a theoretical move.

            if ( IsCloaked && !WillHaveDoneAttack ) //any attack at all will take us out of stealth
                return true;

            if ( SkipInconspicuousCheck )
                return false;
            if ( AggroAmount > 0 )
                return false; //if we have aggroed that group, then no hiding from them

            ClearanceCheckType clearanceCheck = BuildingOrNull != null ? ClearanceCheckType.MovingToBuilding : ClearanceCheckType.MovingToNonBuilding;

            if ( IsUnremarkableRightNow_NotCountingActualClearanceChecks )
            {
                int effectiveClearance = EffectiveClearance;
                if ( BuildingOrNull != null )
                {
                    int clearanceInt = BuildingOrNull.CalculateLocationSecurityClearanceInt();
                    if ( clearanceInt > effectiveClearance )
                        return false; //this building has a higher security clearance than us, so shoot at us
                    return true; //some other building, so don't shoot
                }
                else
                {
                    Vector3 drawLocation = Location;
                    MapTile tile = CityMap.TryGetWorldCellAtCoordinates( drawLocation )?.ParentTile;
                    if ( tile == null )
                        return true; //we are off the map somehow

                    foreach ( MapPOI poi in tile.RestrictedPOIs )
                    {
                        if ( poi == null || poi.HasBeenDestroyed )
                            continue;

                        ArcenFloatRectangle rect = poi.GetOuterRect();
                        if ( !rect.ContainsPointXZ( drawLocation ) )
                            continue; //destination not in here, so nevermind

                        SecurityClearance clearance = poi.Type.RequiredClearance;
                        if ( clearance != null && clearance.Level > effectiveClearance )
                            return false; //this poi has a higher security clearance than us, so shoot at us
                    }
                }

                return true;
            }
            else //shoot us, we're a scary machine!
                return false;
        }
        #endregion
    }
}

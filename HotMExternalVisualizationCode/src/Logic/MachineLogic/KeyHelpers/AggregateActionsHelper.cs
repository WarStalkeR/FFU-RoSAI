using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{    
    public static class AggregateActionsHelper
    {
        public static readonly Dictionary<ISimBuilding, int> WorkingBuildings = Dictionary<ISimBuilding, int>.Create_WillNeverBeGCed( 100, "BasicActionsHelper-WorkingBuildings" );

        #region ApplyToRandomValidBuildingInRangeOfActor
        public static bool ApplyToRandomValidBuildingInRangeOfActor( LocationCalculationType Calc, NPCEvent EventOrNull, string OtherOptionalID, ISimMachineActor Actor, 
            bool UnitsBlockThemselves, LockdownBufferType BufferType, MersenneTwister Rand )
        {
            if ( Actor == null || Calc == null )
                return false;

            MapCell centerCell = Actor.CalculateMapCell();
            if ( centerCell == null )
                return false;

            WorkingBuildings.Clear();

            int securityMin = Calc.SecurityMin;
            int securityMax = Calc.SecurityMax;

            //if ( Calc.BlockBuildingsWithHigherClearanceThanThisUnitHas )
            //{
            //    int actorClearance = Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
            //    if ( actorClearance < securityMax )
            //        securityMax = actorClearance;
            //}

            Vector3 loc = Actor.GetActualPositionForMovementOrPlacement();
            float maxRange = Actor.GetMovementRange();
            maxRange -= 2f; //a bit of extra buffer

            //the following code is repeated in the units section, as it's inlined for performance reasons
            float minX = loc.x - maxRange;
            float maxX = loc.x + maxRange;
            float minZ = loc.z - maxRange;
            float maxZ = loc.z + maxRange;

            foreach ( MapCell cell in centerCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
            {
                ArcenFloatRectangle rect = cell.CellRect;
                if ( rect.XMax <= minX || rect.XMin >= maxX ||
                    rect.YMax <= minZ || rect.YMin >= maxZ )
                    continue;

                foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                {
                    ISimBuilding building = item?.SimBuilding;
                    if ( building == null ) 
                        continue;
                    if ( building.GetIsDestroyed() )
                        continue;

                    StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                    if ( streetSense.ActionType != null )
                        continue; //already filled

                    if ( building.GetAreMoreUnitsBlockedFromComingHere() && (UnitsBlockThemselves || !(building.CurrentOccupyingUnit?.GetEquals( Actor ) ?? false)) )
                        continue; //if another unit standing here, block it
                    if ( building.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                        continue; //for some reason this building is invisible, like burning down or whatnot

                    if ( securityMin > 0 || securityMax < 5 )
                    {
                        int buildingSecurity = building.CalculateLocationSecurityClearanceInt();
                        if ( buildingSecurity < securityMin || buildingSecurity > securityMax )
                            continue;
                    }

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

                            float targetDist = (building.GetMapItem().CenterPoint - lockdown.Position).GetSquareGroundMagnitude();
                            if ( !lockdownType.CalculateIsSquaredRangeValid( BufferType, targetDist ) ) //too close to the border of this lockdown
                            {
                                isBlockedByLockdown = true;
                                break;
                            }

                            bool destIsInRange = targetDist <= lockdownType.RadiusSquared;
                            bool sourceIsInRange = (loc - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;

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

                    streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, EventOrNull, null );
                    streetSense.OtherOptionalID = OtherOptionalID;

                    return true;
                }
            }
            return false;
        }
        #endregion

        #region ApplyToRandomValidBuildingInRangeOfActorThatMatchTag
        public static bool ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( LocationCalculationType Calc, NPCEvent EventOrNull, string OtherOptionalID, ISimMachineActor Actor,
            bool UnitsBlockThemselves, LockdownBufferType BufferType, BuildingTag RequiredTag, MersenneTwister Rand )
        {
            if ( Actor == null || Calc == null )
                return false;

            MapCell centerCell = Actor.CalculateMapCell();
            if ( centerCell == null )
                return false;

            WorkingBuildings.Clear();

            int securityMin = Calc.SecurityMin;
            int securityMax = Calc.SecurityMax;

            //if ( Calc.BlockBuildingsWithHigherClearanceThanThisUnitHas )
            //{
            //    int actorClearance = Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding );
            //    if ( actorClearance < securityMax )
            //        securityMax = actorClearance;
            //}

            Vector3 loc = Actor.GetActualPositionForMovementOrPlacement();
            float maxRange = Actor.GetMovementRange();
            maxRange -= 2f; //a bit of extra buffer

            //the following code is repeated in the units section, as it's inlined for performance reasons
            float minX = loc.x - maxRange;
            float maxX = loc.x + maxRange;
            float minZ = loc.z - maxRange;
            float maxZ = loc.z + maxRange;

            foreach ( MapCell cell in centerCell.AdjacentCellsAndSelfIncludingDiagonal2x.GetRandomStartEnumerable( Rand ) )
            {
                if ( cell == null ) 
                    continue;
                ArcenFloatRectangle rect = cell.CellRect;
                if ( rect.XMax <= minX || rect.XMin >= maxX ||
                    rect.YMax <= minZ || rect.YMin >= maxZ )
                    continue;

                foreach ( MapItem item in cell.BuildingList.GetDisplayList() )
                {
                    ISimBuilding building = item?.SimBuilding;
                    if ( building == null )
                        continue;
                    if ( building.GetIsDestroyed() )
                        continue;

                    StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                    if ( streetSense?.ActionType != null )
                        continue; //already filled

                    BuildingTypeVariant variant = building.GetVariant();
                    if ( variant == null || !variant.Tags.ContainsKey( RequiredTag.ID ) )
                        continue;

                    if ( building.GetAreMoreUnitsBlockedFromComingHere() && (UnitsBlockThemselves || !(building.CurrentOccupyingUnit?.GetEquals( Actor ) ?? false)) )
                        continue; //if another unit standing here, block it
                    if ( building.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                        continue; //for some reason this building is invisible, like burning down or whatnot

                    if ( securityMin > 0 || securityMax < 5 )
                    {
                        int buildingSecurity = building.CalculateLocationSecurityClearanceInt();
                        if ( buildingSecurity < securityMin || buildingSecurity > securityMax )
                            continue;
                    }

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
                            float targetDist = (building.GetMapItem().CenterPoint - lockdown.Position).GetSquareGroundMagnitude();
                            if ( !lockdownType.CalculateIsSquaredRangeValid( BufferType, targetDist ) ) //too close to the border of this lockdown
                            {
                                isBlockedByLockdown = true;
                                break;
                            }

                            bool destIsInRange = targetDist <= lockdownType.RadiusSquared;
                            bool sourceIsInRange = (loc - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;

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

                    streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, EventOrNull, null );
                    streetSense.OtherOptionalID = OtherOptionalID;

                    return true;
                }
            }
            return false;
        }
        #endregion

        public static readonly List<NPCEvent> WorkingMinorEventList = List<NPCEvent>.Create_WillNeverBeGCed( 100, "BasicActionsHelper-WorkingMinorEventList" );
        public static readonly DrawBag<NPCEvent> WorkingMinorEventFinalDrawBag = DrawBag<NPCEvent>.Create_WillNeverBeGCed( 100, "BasicActionsHelper-WorkingMinorEventFinalDrawBag" );

        #region FillDrawBagOfPotentialMinorEvents
        public static void FillDrawBagOfPotentialMinorEvents( bool shouldWriteDebugLog )
        {
            WorkingMinorEventList.Clear();
            WorkingMinorEventFinalDrawBag.Clear();

            foreach ( NPCEvent minorEvent in EventAggregation.ValidMinorEvents.GetDisplayList() )
            {
                if ( minorEvent.MinorData.IsKeyLocationEvent )
                    continue;
                if ( !minorEvent.Event_CalculateMeetsPrerequisites( null, GateByLogicCheckType.CitySpecific, EventCheckReason.StandardSeeding, false ) )
                    continue; //do this again in case the other thread has not had time to catch up.

                //if ( !minorEvent.Event_CalculateMeetsPrerequisites( Actor, GateByLogicCheckType.ActorSpecific, EventCheckReason.StandardSeeding, shouldWriteDebugLog ) )
                //    continue; //not valid for this actor; the other non-actor-specific stuff was already checked

                WorkingMinorEventList.Add( minorEvent );
                minorEvent.MinorData.WorkingPotentialBuildings.Clear();
            }
        }
        #endregion

        #region FillDrawBagOfPotentialKeyMinorEvents
        public static void FillDrawBagOfPotentialKeyMinorEvents( bool shouldWriteDebugLog )
        {
            WorkingMinorEventList.Clear();
            WorkingMinorEventFinalDrawBag.Clear();

            foreach ( NPCEvent minorEvent in EventAggregation.ValidMinorEvents.GetDisplayList() )
            {
                if ( !minorEvent.MinorData.IsKeyLocationEvent )
                    continue;
                if ( !minorEvent.Event_CalculateMeetsPrerequisites( null, GateByLogicCheckType.CitySpecific, EventCheckReason.StandardSeeding, false ) )
                    continue; //do this again in case the other thread has not had time to catch up.

                //the other non-actor-specific stuff was already checked

                WorkingMinorEventList.Add( minorEvent );
                minorEvent.MinorData.WorkingPotentialBuildings.Clear();
            }
        }
        #endregion

        public static readonly List<ProjectOutcomeStreetSenseItem> WorkingProjectItemList = List<ProjectOutcomeStreetSenseItem>.Create_WillNeverBeGCed( 100, "BasicActionsHelper-WorkingProjectItemList" );
        public static readonly DrawBag<ProjectOutcomeStreetSenseItem> WorkingProjectItemFinalDrawBag = DrawBag<ProjectOutcomeStreetSenseItem>.Create_WillNeverBeGCed( 100, "BasicActionsHelper-WorkingProjectItemFinalDrawBag" );

        #region FillDrawBagOfPotentialProjectItems
        public static void FillDrawBagOfPotentialProjectItems( bool shouldWriteDebugLog )
        {
            WorkingProjectItemList.Clear();
            WorkingProjectItemFinalDrawBag.Clear();

            foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
            {
                if ( !project.DuringGame_IsActive )
                    continue;
                ProjectOutcome outcome = project.DuringGame_IntendedOutcome;
                if ( outcome == null || outcome.StreetSenseItems.Count == 0 )
                    continue;

                foreach ( KeyValuePair<string, ProjectOutcomeStreetSenseItem> kv in outcome.StreetSenseItems )
                {
                    if ( !kv.Value.DuringGame_CanStillDo() || kv.Value.DoesNotSeedUnlessFallback_Calculated )
                    {
                        kv.Value.DuringGameplay_NumberStreetSenseItemsAssigned = 0;
                        continue;
                    }
                    WorkingProjectItemList.Add( kv.Value );
                    kv.Value.WorkingPotentialBuildings.Clear();
                }
            }
        }
        #endregion

        #region HandleProjectOutcomeStreetSenseItem
        public static void HandleProjectOutcomeStreetSenseItem( ProjectOutcomeStreetSenseItem streetSenseItem, LocationCalculationType Calc, MersenneTwister Rand, int recursionDepth )
        {
            if ( recursionDepth >= 8 )
            {
                ArcenDebugging.LogWithStack( "Got to recursion depth 8 on HandleProjectOutcomeStreetSenseItem. Did someone define a circular loop for '" +
                    (streetSenseItem?.ParentProject?.ID ?? "null") + "' StreetSense entries?  Looks like it.", Verbosity.ShowAsError );
                return;
            }

            int finalItemsAssigned = 0;
            if ( streetSenseItem.SeedsOnePerDistrict )
            {
                #region One Per District
                if ( !streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired && streetSenseItem.CalcThreadOnly_LastCachedBuildings.Count > 0 && streetSenseItem.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                {
                    foreach ( ISimBuilding building in streetSenseItem.CalcThreadOnly_LastCachedBuildings )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;
                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassRetryLogic( building ) )
                            continue;
                        finalItemsAssigned++;
                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                        streetSense.OtherOptionalID = string.Empty;
                    }

                    streetSenseItem.DuringGameplay_NumberStreetSenseItemsAssigned = finalItemsAssigned;
                    return;
                }

                streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired = false;
                streetSenseItem.CalcThreadOnly_LastCachedBuildings.Clear();

                bool hasSeededOne = false;
                foreach ( MapDistrict district in CityMap.AllDistricts.GetRandomStartEnumerable( Rand ) )
                {
                    if ( streetSenseItem.MustBeAtDistrictTag != null && !district.Type.Tags.ContainsKey( streetSenseItem.MustBeAtDistrictTag.ID ) )
                        continue; //wrong tag

                    if ( hasSeededOne )
                    {
                        if ( streetSenseItem.ChanceOfSeedingPerDistrict < 100 )
                        {
                            if ( Rand.Next( 0, 100 ) > streetSenseItem.ChanceOfSeedingPerDistrict )
                                continue; //failed chance of seeding, so skip!
                        }
                    }

                    foreach ( ISimBuilding building in district.AllBuildings.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;

                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        if ( streetSense.ActionType != null )
                            continue;

                        BuildingTypeVariant variant = building.GetVariant();
                        if ( variant == null )
                            continue;
                        if ( streetSenseItem.MustBeAtBuildingTag != null && !variant.Tags.ContainsKey( streetSenseItem.MustBeAtBuildingTag.ID ) )
                            continue; //wrong tag

                        if ( building.GetAreMoreUnitsBlockedFromComingHere() || building.GetIsBlockedFromHavingMissionAddedHere() )
                            continue;

                        POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                        MapDistrict districtOrNull = building.GetLocationDistrict();
                        int requiredClearance = building.CalculateLocationSecurityClearanceInt();

                        float roughDist = building.RoughDistanceFromMachines;

                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassLogic( building, variant, poiTypeOrNull, districtOrNull, requiredClearance, roughDist, false ) )
                        {
                            //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                            continue; //this one is blocked!
                        }

                        streetSenseItem.CalcThreadOnly_LastCachedBuildings.Add( building );
                        streetSenseItem.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                        streetSense.OtherOptionalID = string.Empty;
                        hasSeededOne = true;
                        finalItemsAssigned++;
                        break; //only one per district
                    }
                }

                if ( !hasSeededOne )
                {
                    if ( streetSenseItem.Fallback != null ) //since the first failed, go into the fallback, if there is a fallback
                        HandleProjectOutcomeStreetSenseItem( streetSenseItem.Fallback, Calc, Rand, recursionDepth + 1 );
                }

                #endregion One Per District
            }
            else if ( streetSenseItem.SeedsOnePerPOI )
            {
                #region One Per POI
                if ( !streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired && streetSenseItem.CalcThreadOnly_LastCachedBuildings.Count > 0 && streetSenseItem.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                {
                    foreach ( ISimBuilding building in streetSenseItem.CalcThreadOnly_LastCachedBuildings )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;
                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassRetryLogic( building ) )
                            continue;

                        finalItemsAssigned++;
                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                        streetSense.OtherOptionalID = string.Empty;
                    }
                    streetSenseItem.DuringGameplay_NumberStreetSenseItemsAssigned = finalItemsAssigned;
                    return;
                }

                streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired = false;
                streetSenseItem.CalcThreadOnly_LastCachedBuildings.Clear();

                bool hasSeededOne = false;
                foreach ( MapPOI poi in streetSenseItem.MustBeAtPOITag.DuringGame_POIs.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                {
                    if ( poi == null || poi.HasBeenDestroyed )
                        continue;

                    if ( hasSeededOne )
                    {
                        if ( streetSenseItem.ChanceOfSeedingPerPOI < 100 )
                        {
                            if ( Rand.Next( 0, 100 ) > streetSenseItem.ChanceOfSeedingPerPOI )
                                continue; //failed chance of seeding, so skip!
                        }
                    }

                    foreach ( ISimBuilding building in poi.DuringGame_BuildingsInPOI.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;
                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassRetryLogic( building ) )
                            continue;

                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        if ( streetSense.ActionType != null )
                            continue;

                        BuildingTypeVariant variant = building.GetVariant();
                        if ( variant == null )
                            continue;
                        if ( streetSenseItem.MustBeAtBuildingTag != null && !variant.Tags.ContainsKey( streetSenseItem.MustBeAtBuildingTag.ID ) )
                            continue; //wrong tag

                        if ( building.GetAreMoreUnitsBlockedFromComingHere() || building.GetIsBlockedFromHavingMissionAddedHere() )
                            continue;

                        POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                        MapDistrict districtOrNull = building.GetLocationDistrict();
                        int requiredClearance = building.CalculateLocationSecurityClearanceInt();

                        float roughDist = building.RoughDistanceFromMachines;

                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassLogic( building, variant, poiTypeOrNull, districtOrNull, requiredClearance, roughDist, false ) )
                        {
                            //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                            continue; //this one is blocked!
                        }

                        streetSenseItem.CalcThreadOnly_LastCachedBuildings.Add( building );
                        streetSenseItem.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                        streetSense.OtherOptionalID = string.Empty;
                        hasSeededOne = true;
                        finalItemsAssigned++;
                        break; //only one per POI
                    }
                }

                if ( !hasSeededOne )
                {
                    if ( streetSenseItem.Fallback != null ) //since the first failed, go into the fallback, if there is a fallback
                        HandleProjectOutcomeStreetSenseItem( streetSenseItem.Fallback, Calc, Rand, recursionDepth + 1 );
                }
                #endregion One Per POI
            }
            else
            {
                #region 1+ Per Item
                if ( !streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired && streetSenseItem.CalcThreadOnly_LastCachedBuildings.Count > 0 && streetSenseItem.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                {
                    foreach ( ISimBuilding building in streetSenseItem.CalcThreadOnly_LastCachedBuildings )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;
                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassRetryLogic( building ) )
                            continue;
                        finalItemsAssigned++;
                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                        streetSense.OtherOptionalID = string.Empty;
                    }

                    streetSenseItem.DuringGameplay_NumberStreetSenseItemsAssigned = finalItemsAssigned;
                    return;
                }

                ISimBuilding bestDistanceBuilding = null;
                float bestDistance = 0;

                streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired = false;
                streetSenseItem.CalcThreadOnly_LastCachedBuildings.Clear();
                streetSenseItem.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;

                int numberHasAssigned = 0;
                while ( numberHasAssigned < streetSenseItem.SeedXAtOnceInGeneral )
                {
                    bool madeAnyAssignments = false;
                    //done this way so that adjacent buildings are not picked each time
                    foreach ( ISimBuilding building in streetSenseItem.MustBeAtBuildingTag.DuringGame_Buildings.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                    {
                        if ( building.GetIsDestroyed() )
                            continue;
                        if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassRetryLogic( building ) )
                            continue;

                        ISimUnit unitAtBuilding = building.CurrentOccupyingUnit;
                        if ( unitAtBuilding != null && unitAtBuilding.ContainerLocation.Get() == building )
                            continue;

                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                        if ( streetSense.ActionType == null )
                        {
                            BuildingTypeVariant variant = building.GetVariant();
                            if ( variant == null )
                                continue;

                            POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                            MapDistrict districtOrNull = building.GetLocationDistrict();
                            int requiredClearance = building.CalculateLocationSecurityClearanceInt();
                            float roughDist = building.RoughDistanceFromMachines;

                            if ( !streetSenseItem.SeedStreetSenseLogic_CalculateDoesBuildingPassLogic( building, variant, poiTypeOrNull, districtOrNull, requiredClearance, roughDist, false ) )
                            {
                                //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                                continue; //this one is blocked!
                            }

                            if ( building.GetAreMoreUnitsBlockedFromComingHere() )//|| building.GetIsBlockedFromHavingMissionAddedHere() )
                                continue;

                            if ( streetSenseItem.CareAboutBeingInRangeOfAnyAndroids )
                            {
                                bool isInRangeOfAnyAndroid = false;
                                foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                                {
                                    if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                        continue; //only do this for machine units that are deployed\

                                    Vector3 drawLocation = actor.GetDrawLocation();
                                    float movementRange = actor.GetMovementRange();
                                    movementRange -= 2f;
                                    movementRange *= movementRange;
                                    float squareGroundMagnitude = (drawLocation - building.GetMapItem().CenterPoint).GetSquareGroundMagnitude();
                                    if ( squareGroundMagnitude < movementRange )
                                    {
                                        isInRangeOfAnyAndroid = true;
                                        break;
                                    }

                                    if ( bestDistanceBuilding == null || squareGroundMagnitude < bestDistance )
                                    {
                                        bestDistanceBuilding = building;
                                        bestDistance = squareGroundMagnitude;
                                    }
                                }
                                if ( !isInRangeOfAnyAndroid )
                                    continue;
                            }

                            streetSenseItem.CalcThreadOnly_LastCachedBuildings.Add( building );
                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                            streetSense.OtherOptionalID = string.Empty;
                            numberHasAssigned++;
                            finalItemsAssigned++;
                            madeAnyAssignments = true;
                            break; //have reached the max to seed
                        }
                    }

                    if ( !madeAnyAssignments )
                        break; //prevent infinite loops
                }

                if ( numberHasAssigned == 0 && bestDistanceBuilding != null )
                {
                    streetSenseItem.DuringGameplay_AreLastCachedBuildingsExpired = false;
                    streetSenseItem.CalcThreadOnly_LastCachedBuildings.Clear();
                    streetSenseItem.CalcThreadOnly_LastCachedBuildings.Add( bestDistanceBuilding );
                    streetSenseItem.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                    StreetSenseDataAtBuilding streetSense = bestDistanceBuilding.CurrentStreetSenseActionRaw.Construction;
                    streetSense.ApplyAction( bestDistanceBuilding, Calc.ActionOnBuildingArrive, null, streetSenseItem );
                    streetSense.OtherOptionalID = string.Empty;
                    numberHasAssigned++;
                    finalItemsAssigned++;
                }

                if ( finalItemsAssigned == 0 )
                {
                    if ( streetSenseItem.Fallback != null ) //since the first failed, go into the fallback, if there is a fallback
                        HandleProjectOutcomeStreetSenseItem( streetSenseItem.Fallback, Calc, Rand, recursionDepth + 1 );
                }

                #endregion 1+ Per Item
            }

            streetSenseItem.DuringGameplay_NumberStreetSenseItemsAssigned = finalItemsAssigned;
        }
        #endregion
    }
}

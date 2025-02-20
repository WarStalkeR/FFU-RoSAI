using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using System.Runtime.Remoting.Messaging;

namespace Arcen.HotM.ExternalVis
{
    public class LocCalcs_Basic : ILocationCalculationImplementation
    {
        public void RecalculateFromStreetSenseThread( LocationCalculationType Calc, MersenneTwister Rand )
        {
            int debugStage = 0;
            try
            {
                switch ( Calc.ID )
                {
                    case "StealVehicle":
                        debugStage = 1200;
                        #region StealVehicle
                        {
                            if ( !FlagRefs.HasPassedChapterOneTierTwo.DuringGameplay_IsTripped || FlagRefs.VehicularSecurityPatch.DuringGameplay_HasEverCompleted )
                                return;
                            if ( World.Forces.GetMachineVehiclesByID().Count > 0 )
                                return; //only can do this if you have no vehicles

                            debugStage = 1400;

                            int maxToAdd = Rand.NextInclus( 2, 4 );
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                            {
                                if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                    continue; //only do this for machine units that are deployed
                                debugStage = 1600;

                                if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, string.Empty, LockdownBufferType.Mid ) )
                                    continue; //already one close enough, so ignore this

                                debugStage = 1800;
                                if ( AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( Calc, null, string.Empty, actor, true, LockdownBufferType.Mid, Calc.RelatedBuildingTag, Rand ) )
                                {
                                    maxToAdd--;
                                    if ( maxToAdd <= 0 )
                                        break;
                                }
                            }
                        }
                        #endregion
                        break;
                    case "RecruitAndroid":
                        #region RecruitAndroid
                        {
                            debugStage = 1200;
                            if ( FlagRefs.AndroidSecurityPatch.DuringGameplay_HasEverCompleted )
                                break;
                            if ( SimMetagame.CurrentChapterNumber == 0 && !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                                break;
                            debugStage = 1400;

                            for ( int outerCount = 0; outerCount <= 2; outerCount++ )
                            {
                                string unitName = "Technician";
                                switch (outerCount)
                                {
                                    case 1:
                                        unitName = "CombatUnit";
                                        break;
                                    case 2:
                                        unitName = "Nickelbot";
                                        break;
                                }

                                int maxToAdd = Rand.NextInclus( 2, 4 );
                                foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                                {
                                    if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                        continue; //only do this for machine units that are deployed
                                    debugStage = 1600;

                                    if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, unitName, LockdownBufferType.Mid ) )
                                        continue; //already one close enough, so ignore this

                                    debugStage = 1800;
                                    if ( AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( Calc, null, unitName, actor, true, LockdownBufferType.Mid, Calc.RelatedBuildingTag, Rand ) )
                                    {
                                        maxToAdd--;
                                        if ( maxToAdd <= 0 )
                                            break;
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case "ColdBlood":
                        #region ColdBlood
                        {
                            debugStage = 1200;
                            if ( CityStatisticTable.GetScore( "Murders" ) <= 0 || FlagRefs.GaveUpColdBlood.DuringGameplay_IsTripped )
                                return; //no cold blood option if you have not already murdered someone in this specific city
                            if ( SimMetagame.CurrentChapterNumber == 0 )
                                break;

                            int maxToAdd = Rand.NextInclus( 1, 3 );
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                            {
                                if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                    continue; //only do this for machine units that are deployed
                                debugStage = 1600;

                                if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, LockdownBufferType.Mid ) )
                                    continue; //already one close enough, so ignore this

                                debugStage = 1800;
                                if ( AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( Calc, null, string.Empty, actor, true, LockdownBufferType.Mid, Calc.RelatedBuildingTag, Rand ) )
                                {
                                    maxToAdd--;
                                    if ( maxToAdd <= 0 )
                                        break;
                                }
                            }
                        }
                        #endregion
                        break;
                    case "MurderAndroidForRegistration":
                        #region MurderAndroidForRegistration
                        {
                            if ( SimMetagame.CurrentChapterNumber == 0 )
                                break;

                            debugStage = 1500;
                            int maxToAdd = Rand.NextInclus( 1, 3 );
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                            {
                                {
                                    if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                        continue; //only do this for machine units that are deployed
                                }
                                debugStage = 1600; 
                                if ( actor.OutcastLevel == 0 && !actor.GetHasAggroedAnyNPCCohort() )
                                    continue; //no reason to do this, so don't show it near them

                                debugStage = 1700;
                                {
                                    bool cannotBenefit = false;
                                    if ( actor is ISimMachineUnit unit && unit.UnitType.DefaultPerks.Count > 0 )
                                    {
                                        foreach ( KeyValuePair<ActorPerk, Unlock> kv in unit.UnitType.DefaultPerks )
                                        {
                                            if ( (kv.Value?.DuringGameplay_IsInvented??true) && kv.Key.CausesActorToBeConsideredOutcastLevel > 0 )
                                            {
                                                cannotBenefit = true; //doing this would not help this kind of unit, so skip it
                                                break;
                                            }
                                        }
                                    }
                                    if ( cannotBenefit )
                                        continue;
                                }

                                if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, LockdownBufferType.Mid ) )
                                    continue; //already one close enough, so ignore this

                                debugStage = 1800;
                                if ( AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( Calc, null, string.Empty, actor, true, LockdownBufferType.Mid, Calc.RelatedBuildingTag, Rand ) )
                                {
                                    maxToAdd--;
                                    if ( maxToAdd <= 0 )
                                        break;
                                }
                            }
                        }
                        #endregion
                        break;
                    case "Wiretap":
                        #region Wiretap
                        {
                            if ( SimMetagame.CurrentChapterNumber == 0 || !FlagRefs.HasFiguredOutCommandMode.DuringGameplay_IsTripped )
                                break;

                            debugStage = 1500;
                            int maxToAdd = Rand.NextInclus( 1, 3);
                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                            {
                                if ( !(actor is ISimMachineUnit unit) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                    continue; //only do this for machine units that are deployed
                                debugStage = 1600;

                                if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, LockdownBufferType.Mid ) )
                                    continue; //already one close enough, so ignore this

                                debugStage = 1800;
                                if ( AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActorThatMatchTag( Calc, null, string.Empty, actor, true, LockdownBufferType.Mid, Calc.RelatedBuildingTag, Rand ) )
                                {
                                    maxToAdd--;
                                    if ( maxToAdd <= 0 )
                                        break;
                                }
                            }
                        }
                        #endregion
                        break;
                    case "HideAndSelfRepair":
                        #region HideAndSelfRepair
                        {
                            if ( SimMetagame.CurrentChapterNumber == 0 && !FlagRefs.Ch0_TrapHasBeenSprung.DuringGameplay_IsTripped )
                                break;

                            foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                            {
                                if ( !(actor is ISimMachineUnit unit ) || !unit.GetIsDeployed() || !unit.UnitType.IsConsideredAndroid )
                                    continue; //only do this for machine units that are deployed
                                debugStage = 1200;
                                if ( actor.GetActorDataLostFromMax( ActorRefs.ActorHP, true ) <= 0 )
                                    continue; //no reason to do this, so don't show it near them

                                if ( Calc.ActionOnBuildingArrive.CalculateIsAlreadyConstructionActionWithinRangeOfActor( actor, LockdownBufferType.Mid ) )
                                    continue; //already one close enough, so ignore this

                                debugStage = 1400;
                                AggregateActionsHelper.ApplyToRandomValidBuildingInRangeOfActor( Calc, null, string.Empty, actor, true, LockdownBufferType.Mid, Rand );
                            }
                        }
                        #endregion
                        break;
                    case "StreetSense_MinorEvents":
                        #region StreetSense_MinorEvents
                        {
                            debugStage = 1200;

                            debugStage = 1500;
                            VisOtherReports.GetIfShouldDoReport( "StreetSense_MinorEventLocationEventReport", out bool doReport, out ArcenDoubleCharacterBuffer debugBuffer );
                            if ( doReport ) debugBuffer.AddNeverTranslated( "Started at: " + ArcenTime.AnyTimeSinceStartF, true ).Line();

                            AggregateActionsHelper.FillDrawBagOfPotentialMinorEvents( false );

                            if ( doReport ) debugBuffer.AddNeverTranslated( "WorkingMinorEventList: " + AggregateActionsHelper.WorkingMinorEventList.Count, true ).Line();

                            foreach ( NPCEvent minorEvent in AggregateActionsHelper.WorkingMinorEventList )
                            {
                                if ( minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag == null )
                                    continue;

                                if ( minorEvent.MinorData.SeedsOnePerDistrict )
                                {
                                    #region One Per District

                                    if ( !minorEvent.DuringGameplay_AreLastCachedBuildingsExpired && minorEvent.CalcThreadOnly_LastCachedBuildings.Count > 0 && minorEvent.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                                    {
                                        foreach ( ISimBuilding building in minorEvent.CalcThreadOnly_LastCachedBuildings )
                                        {
                                            if ( building.GetIsDestroyed() )
                                                continue;
                                            StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                        }
                                        continue;
                                    }

                                    minorEvent.DuringGameplay_AreLastCachedBuildingsExpired = false;
                                    minorEvent.CalcThreadOnly_LastCachedBuildings.Clear();

                                    bool hasSeededOne = false;
                                    foreach ( MapDistrict district in CityMap.AllDistricts.GetRandomStartEnumerable( Rand ) )
                                    {
                                        if ( hasSeededOne )
                                        {
                                            if ( minorEvent.MinorData.ChanceOfSeedingPerDistrict < 100 )
                                            {
                                                if ( Rand.Next( 0, 100 ) > minorEvent.MinorData.ChanceOfSeedingPerDistrict )
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
                                            if ( !variant.Tags.ContainsKey( minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag.ID ) )
                                                continue; //wrong tag

                                            if ( building.GetAreMoreUnitsBlockedFromComingHere() || building.GetIsBlockedFromHavingMissionAddedHere() )
                                                continue;

                                            POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                                            int requiredClearance = building.CalculateLocationSecurityClearanceInt();

                                            DictionaryView<LocationDataType, int> buildingData = building.GetBuildingData();
                                            float roughDist = building.RoughDistanceFromMachines;

                                            if ( !minorEvent.MinorData.SeedAt.SeedAtLogic_CalculateDoesBuildingPassLogic( SeedAtCheckPass.First, variant, poiTypeOrNull, requiredClearance, buildingData, roughDist, false ) )
                                            {
                                                //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                                                continue; //this one is blocked!
                                            }

                                            minorEvent.CalcThreadOnly_LastCachedBuildings.Add( building );
                                            minorEvent.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                            hasSeededOne = true;
                                            break; //only one per district
                                        }
                                    }
                                    #endregion One Per District
                                }
                                else if ( minorEvent.MinorData.SeedsOnePerRelatedPOI )
                                {
                                    #region One Per POI

                                    if ( minorEvent.MinorData.SeedAt.POISeedTag == null )
                                    {
                                        ArcenDebugging.LogSingleLine( "Null POISeedTag for minorEvent '" + minorEvent.ID + "'", Verbosity.ShowAsError );
                                        continue;
                                    }

                                    if ( !minorEvent.DuringGameplay_AreLastCachedBuildingsExpired && minorEvent.CalcThreadOnly_LastCachedBuildings.Count > 0 && minorEvent.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                                    {
                                        foreach ( ISimBuilding building in minorEvent.CalcThreadOnly_LastCachedBuildings )
                                        {
                                            if ( building.GetIsDestroyed() )
                                                continue;

                                            StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                        }
                                        continue;
                                    }

                                    minorEvent.DuringGameplay_AreLastCachedBuildingsExpired = false;
                                    minorEvent.CalcThreadOnly_LastCachedBuildings.Clear();

                                    bool hasSeededOne = false;
                                    foreach ( MapPOI poi in minorEvent.MinorData.SeedAt.POISeedTag.DuringGame_POIs.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                                    {
                                        if ( poi == null || poi.HasBeenDestroyed )
                                            continue;

                                        if ( minorEvent.MinorData.CanOnlyAppearInPOIsWithActiveAlarm_Any )
                                        {
                                            if ( !poi.IsPOIAlarmed_Any )
                                                continue;
                                        }
                                        else if ( minorEvent.MinorData.CanOnlyAppearInPOIsWithActiveAlarm_Player )
                                        {
                                            if ( !poi.IsPOIAlarmed_AgainstPlayer )
                                                continue;
                                        }
                                        else if ( minorEvent.MinorData.CanOnlyAppearInPOIsWithActiveAlarm_ThirdParty )
                                        {
                                            if ( !poi.IsPOIAlarmed_ThirdParty )
                                                continue;
                                        }
                                        else if ( minorEvent.MinorData.CanOnlyAppearInPOIsWithoutActiveAlarm )
                                        {
                                            if ( poi.IsPOIAlarmed_Any )
                                                continue;
                                        }
                                        else if ( minorEvent.MinorData.CanOnlyAppearInPOIsWhereTheftIsNotBlocked )
                                        {
                                            if ( poi.IsPOIBlockedFromHavingMoreThefts )
                                                continue;
                                        }

                                        if ( hasSeededOne )
                                        {
                                            if ( minorEvent.MinorData.ChanceOfSeedingPerPOI < 100  )
                                            {
                                                if ( Rand.Next( 0, 100 ) > minorEvent.MinorData.ChanceOfSeedingPerPOI )
                                                    continue; //failed chance of seeding, so skip!
                                            }
                                        }

                                        foreach ( ISimBuilding building in poi.DuringGame_BuildingsInPOI.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                                        {
                                            if ( building.GetIsDestroyed() )
                                                continue;

                                            StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                            if ( streetSense.ActionType != null )
                                                continue;

                                            BuildingTypeVariant variant = building.GetVariant();
                                            if ( variant == null )
                                                continue;
                                            if ( !variant.Tags.ContainsKey( minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag.ID ) )
                                                continue; //wrong tag

                                            if ( building.GetAreMoreUnitsBlockedFromComingHere() || building.GetIsBlockedFromHavingMissionAddedHere() )
                                                continue;

                                            POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                                            int requiredClearance = building.CalculateLocationSecurityClearanceInt();

                                            DictionaryView<LocationDataType, int> buildingData = building.GetBuildingData();
                                            float roughDist = building.RoughDistanceFromMachines;

                                            if ( !minorEvent.MinorData.SeedAt.SeedAtLogic_CalculateDoesBuildingPassLogic( SeedAtCheckPass.First, variant, poiTypeOrNull, requiredClearance, buildingData, roughDist, false ) )
                                            {
                                                //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                                                continue; //this one is blocked!
                                            }

                                            minorEvent.CalcThreadOnly_LastCachedBuildings.Add( building );
                                            minorEvent.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                            hasSeededOne = true;
                                            break; //only one per POI
                                        }
                                    }
                                    #endregion One Per POI
                                }
                                else
                                {
                                    #region One Per Event
                                    if ( !minorEvent.DuringGameplay_AreLastCachedBuildingsExpired && minorEvent.CalcThreadOnly_LastCachedBuildings.Count > 0 && minorEvent.DuringGameplay_LastCachedBuildingTurn == SimCommon.Turn )
                                    {
                                        foreach ( ISimBuilding building in minorEvent.CalcThreadOnly_LastCachedBuildings )
                                        {
                                            if ( building.GetIsDestroyed() )
                                                continue;
                                            StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                        }
                                        continue;
                                    }

                                    ISimBuilding bestDistanceBuilding = null;
                                    float bestDistance = 0;

                                    bool hasAssignedOne = false;
                                    foreach ( ISimBuilding building in minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag.DuringGame_Buildings.GetDisplayList().GetRandomStartEnumerable( Rand ) )
                                    {
                                        if ( building.GetIsDestroyed() )
                                            continue;

                                        StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                        if ( streetSense.ActionType == null )
                                        {
                                            BuildingTypeVariant variant = building.GetVariant();
                                            if ( variant == null )
                                                continue;

                                            POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                                            int requiredClearance = building.CalculateLocationSecurityClearanceInt();
                                            DictionaryView<LocationDataType, int> buildingData = building.GetBuildingData();
                                            float roughDist = building.RoughDistanceFromMachines;

                                            if ( !minorEvent.MinorData.SeedAt.SeedAtLogic_CalculateDoesBuildingPassLogic( SeedAtCheckPass.First, variant, poiTypeOrNull, requiredClearance, buildingData, roughDist, false ) )
                                            {
                                                //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                                                continue; //this one is blocked!
                                            }

                                            if ( building.GetAreMoreUnitsBlockedFromComingHere() || building.GetIsBlockedFromHavingMissionAddedHere() )
                                                continue;

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

                                            minorEvent.DuringGameplay_AreLastCachedBuildingsExpired = false;
                                            minorEvent.CalcThreadOnly_LastCachedBuildings.Clear();
                                            minorEvent.CalcThreadOnly_LastCachedBuildings.Add( building );
                                            minorEvent.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                                            streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                            streetSense.OtherOptionalID = string.Empty;
                                            hasAssignedOne = true;
                                            break; //only do one per, for these
                                        }
                                    }

                                    if ( !hasAssignedOne && bestDistanceBuilding != null )
                                    {
                                        minorEvent.DuringGameplay_AreLastCachedBuildingsExpired = false;
                                        minorEvent.CalcThreadOnly_LastCachedBuildings.Clear();
                                        minorEvent.CalcThreadOnly_LastCachedBuildings.Add( bestDistanceBuilding );
                                        minorEvent.DuringGameplay_LastCachedBuildingTurn = SimCommon.Turn;
                                        StreetSenseDataAtBuilding streetSense = bestDistanceBuilding.CurrentStreetSenseActionRaw.Construction;
                                        streetSense.ApplyAction( bestDistanceBuilding, Calc.ActionOnBuildingArrive, minorEvent, null );
                                        streetSense.OtherOptionalID = string.Empty;
                                        hasAssignedOne = true;
                                    }
                                    #endregion One Per Event
                                }
                            }
                        }
                        #endregion
                        break;
                    case "StreetSense_KeyLocationEvents":
                        #region StreetSense_KeyLocationEvents
                        {
                            if ( SimMetagame.CurrentChapterNumber == 0 )
                                break;

                            debugStage = 1200;
                            VisOtherReports.GetIfShouldDoReport( "StreetSense_KeyLocationEventReport", out bool doReport, out ArcenDoubleCharacterBuffer debugBuffer );
                            if ( doReport ) debugBuffer.AddNeverTranslated( "Started at: " + ArcenTime.AnyTimeSinceStartF, true ).Line();

                            AggregateActionsHelper.FillDrawBagOfPotentialKeyMinorEvents( doReport );

                            if ( doReport ) debugBuffer.AddNeverTranslated( "WorkingMinorEventList: " + AggregateActionsHelper.WorkingMinorEventList.Count, true ).Line();

                            foreach ( NPCEvent minorEvent in AggregateActionsHelper.WorkingMinorEventList )
                            {
                                if ( minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag == null )
                                    continue;
                                foreach ( ISimBuilding building in minorEvent.MinorData.SeedAt.BuildingSeedPreferredTag.DuringGame_Buildings.GetDisplayList() )
                                {
                                    if ( building.GetIsDestroyed() )
                                        continue;

                                    StreetSenseDataAtBuilding streetSense = building.CurrentStreetSenseActionRaw.Construction;
                                    if ( streetSense.ActionType == null )
                                    {
                                        BuildingTypeVariant variant = building.GetVariant();
                                        if ( variant == null )
                                            continue;

                                        POIType poiTypeOrNull = building.CalculateLocationPOI()?.Type;
                                        int requiredClearance = building.CalculateLocationSecurityClearanceInt();
                                        DictionaryView<LocationDataType, int> buildingData = building.GetBuildingData();
                                        float roughDist = building.RoughDistanceFromMachines;

                                        if ( !minorEvent.MinorData.SeedAt.SeedAtLogic_CalculateDoesBuildingPassLogic( SeedAtCheckPass.First, variant, poiTypeOrNull, requiredClearance, buildingData, roughDist, false ) )
                                        {
                                            //if ( doReportLineItem ) debugBuffer.AddNeverTranslated( minorEvent.ID + " failed " + variant.GetDisplayName(), ColorTheme.RedOrange3 ).Line();
                                            continue; //this one is blocked!
                                        }

                                        streetSense.ApplyAction( building, Calc.ActionOnBuildingArrive, minorEvent, null );
                                        streetSense.OtherOptionalID = string.Empty;
                                    }
                                }
                            }
                        }
                        #endregion
                        break;
                    case "Projects_StreetSenseFromActive":
                        #region Projects_StreetSenseFromActive
                        {
                            debugStage = 1200;

                            debugStage = 1500;
                            VisOtherReports.GetIfShouldDoReport( "StreetSense_MinorEventLocationEventReport", out bool doReport, out ArcenDoubleCharacterBuffer debugBuffer );
                            if ( doReport ) debugBuffer.AddNeverTranslated( "Started at: " + ArcenTime.AnyTimeSinceStartF, true ).Line();

                            AggregateActionsHelper.FillDrawBagOfPotentialProjectItems( false );

                            if ( doReport ) debugBuffer.AddNeverTranslated( "WorkingProjectItemList: " + AggregateActionsHelper.WorkingProjectItemList.Count, true ).Line();

                            foreach ( ProjectOutcomeStreetSenseItem streetSenseItem in AggregateActionsHelper.WorkingProjectItemList )
                            {
                                AggregateActionsHelper.HandleProjectOutcomeStreetSenseItem( streetSenseItem, Calc, Rand, 0 );
                            }
                        }
                        #endregion
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "LocCalcs_Basic: Called HandleAggregateLocationCalculation for '" + Calc.ID + "', which does not support it!", Verbosity.ShowAsError );
                        break;
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "LocCalcs_Basic-HandleAggregateLocationCalculation", debugStage, Calc?.ID ?? "[null-calc]", e, Verbosity.ShowAsError );
            }
        }
    }
}

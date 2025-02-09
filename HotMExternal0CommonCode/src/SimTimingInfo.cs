using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.External
{
    /// <summary>
    /// These things are written for ui purposes
    /// </summary>
    public static class SimTimingInfo
    {
        public static readonly TimingInfoData SimTurn = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnEarly = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnBuildingWork = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnEarlyMid = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnUnitPrep = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnNPCTotal = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnUnitMaintenance = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnLateMid = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnProjects = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnLate = new TimingInfoData( 10 );

        public static readonly TimingInfoData PerFullSecond = new TimingInfoData( 10 );
        public static readonly TimingInfoData PerSecondPeopleStats = new TimingInfoData( 10 );
        public static readonly TimingInfoData PerSecondBuildingWork = new TimingInfoData( 10 );
        public static readonly TimingInfoData PerSecondWorkerAssignment = new TimingInfoData( 10 );
        public static readonly TimingInfoData VisibilityGranters = new TimingInfoData( 10 );
        public static readonly TimingInfoData Collidables = new TimingInfoData( 10 );
        public static readonly TimingInfoData TargetingPass = new TimingInfoData( 10 );

        public static readonly TimingInfoData Vis = new TimingInfoData( 120 );
        public static readonly TimingInfoData VisFrameEarlyRenderCalculations = new TimingInfoData( 120 );
        public static readonly TimingInfoData VisFrameRenderCalculations = new TimingInfoData( 120 );
        public static readonly TimingInfoData VisRender = new TimingInfoData( 120 );
        public static readonly TimingInfoData CityLifeMainThread = new TimingInfoData( 120 );
        public static readonly TimingInfoData CityLifePlanningThread = new TimingInfoData( 120 );
        public static readonly TimingInfoData DroneWork = new TimingInfoData( 120 );
        public static readonly TimingInfoData VisUICalculations = new TimingInfoData( 120 );
        public static readonly TimingInfoData SimMainThreadPerFrameCalculations = new TimingInfoData( 120 );

        public static readonly TimingInfoData PerFrameVisItemsConsideredCount = new TimingInfoData( 10 );

        public static readonly TimingInfoData SimTurnNPCUnitsMain = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnNPCUnitsStanceOuter = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnNPCUnitsStanceCount = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnNPCUnitsActionPlanningOuter = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurnNPCUnitsActionPlanningCount = new TimingInfoData( 10 );

        public static readonly TimingInfoData SimTurn_Wander = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_ReturnToHomePOI = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_ReturnToHomeDistrict = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_RunAfterOutcastThenConspicuous = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_RunAfterMachineStructures = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_RunAfterAggroedUnits = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_RunAfterNearestActorThatICanAutoShoot = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_FollowEvenHiddenPlayerUnits = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_RunAfterThreatsNearMission = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_ReturnToManagerStartArea = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_ReturnToMissionArea = new TimingInfoData( 10 );
        
        public static readonly TimingInfoData SimTurn_FindNearestAggroedUnit = new TimingInfoData( 10 );
        public static readonly TimingInfoData SimTurn_ChaseNearestAggroedUnit = new TimingInfoData( 10 );

        public static void Clear()
        {
            SimTurn.Clear();
            SimTurnEarly.Clear();
            SimTurnBuildingWork.Clear();
            SimTurnEarlyMid.Clear();
            SimTurnUnitPrep.Clear();
            SimTurnNPCTotal.Clear();
            SimTurnUnitMaintenance.Clear();
            SimTurnLateMid.Clear();
            SimTurnProjects.Clear();
            SimTurnLate.Clear();

            PerFullSecond.Clear();
            PerSecondPeopleStats.Clear();
            PerSecondBuildingWork.Clear();
            PerSecondWorkerAssignment.Clear();
            VisibilityGranters.Clear();
            Collidables.Clear();
            TargetingPass.Clear();

            Vis.Clear();
            VisFrameEarlyRenderCalculations.Clear();
            VisFrameRenderCalculations.Clear();
            VisRender.Clear();
            CityLifeMainThread.Clear();
            CityLifePlanningThread.Clear();
            DroneWork.Clear();
			VisUICalculations.Clear();
            SimMainThreadPerFrameCalculations.Clear();

            PerFrameVisItemsConsideredCount.Clear();

            SimTurnNPCUnitsMain.Clear();
            SimTurnNPCUnitsStanceOuter.Clear();
            SimTurnNPCUnitsStanceCount.Clear();
            SimTurnNPCUnitsActionPlanningOuter.Clear();
            SimTurnNPCUnitsActionPlanningCount.Clear();

            SimTurn_Wander.Clear();
            SimTurn_ReturnToHomePOI.Clear();
            SimTurn_ReturnToHomeDistrict.Clear();
            SimTurn_RunAfterOutcastThenConspicuous.Clear();
            SimTurn_RunAfterMachineStructures.Clear();
            SimTurn_RunAfterAggroedUnits.Clear();
            SimTurn_RunAfterNearestActorThatICanAutoShoot.Clear();
            SimTurn_FollowEvenHiddenPlayerUnits.Clear();
            SimTurn_RunAfterThreatsNearMission.Clear();
            SimTurn_ReturnToManagerStartArea.Clear();
            SimTurn_ReturnToMissionArea.Clear();

            SimTurn_FindNearestAggroedUnit.Clear();
            SimTurn_ChaseNearestAggroedUnit.Clear();
        }
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class ExtraCode_Common : IExtraCodeHandlerImplementation
    {
        public void HandleExtraEventChoiceConsequences( ExtraCodeHandler Handler, ISimMachineActor ForMachineActor, 
            NPCCohort ForCohort, NPCCohort LocalAuthority, ISimBuilding ForBuilding, int LocalSecurityClearance, MersenneTwister workingRand, 
            ArcenDoubleCharacterBuffer ExtraBodyTextBuffer, ChoiceResultType ResultType, NPCEvent Event, EventChoice Choice, EventChoiceResult Result, ref bool shouldForceNoAfterActionReport )
        {
            if ( Handler == null || workingRand == null || Event == null || Choice == null || Result == null )
                return;

            switch ( Handler.ID )
            {
                case "NuclearSiloTampering":
                    #region NuclearSiloTampering
                    {
                        if ( ForBuilding != null )
                        {
                            {
                                Vector3 epicenter = ForBuilding.GetPositionForCameraFocus().ReplaceY( 0.1f );
                                ParticleSoundRefs.FullNuke.DuringGame_PlayAtLocation( epicenter,
                                    new Vector3( 0, workingRand.NextFloat( 0, 360f ), 0 ) );
                                ParticleSoundRefs.NukeSound2.DuringGame_PlayAtLocation( epicenter );
                                ParticleSoundRefs.NukeSound3.DuringGame_PlayAtLocation( epicenter );
                                ParticleSoundRefs.NukeSound4.DuringGame_PlayAtLocation( epicenter );


                                QueuedBuildingDestructionData destructionData;
                                destructionData.Epicenter = epicenter;
                                destructionData.Range = MathRefs.FullNukeRadius.FloatMin;
                                destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                                destructionData.AlsoDestroyOtherItems = true;
                                destructionData.AlsoDestroyUnits = true;
                                destructionData.DestroyAllPlayerUnits = true;
                                destructionData.SkipUnitsWithArmorPlatingAbove = 0;
                                destructionData.SkipUnitsAboveHeight = 0;
                                destructionData.IrradiateCells = true;
                                destructionData.UnitsToSpawnAfter = null;
                                destructionData.StatisticForDeaths = CityStatisticRefs.MurdersByNuke;
                                destructionData.IsCausedByPlayer = true;
                                destructionData.IsFromJob = null;
                                destructionData.ExtraCode = CommonRefs.PlayerNukeExplosionExtraCode;

                                MapEffectCoordinator.AddQueuedBuildingDestruction( destructionData );
                            }

                            MapTile tile = ForBuilding.GetLocationMapTile();

                            BuildingTag tag = BuildingTagTable.Instance.GetRowByID( "NuclearPowerPlant" );
                            foreach ( ISimBuilding other in tag.DuringGame_Buildings.GetDisplayList() )
                            {
                                if ( other == ForBuilding )//|| other.GetIsDestroyed() )
                                    continue;
                                if ( other.GetLocationMapTile() != tile )
                                    continue;

                                {
                                    Vector3 epicenter = other.GetPositionForCameraFocus().ReplaceY( 0.1f );
                                    ParticleSoundRefs.FullNuke.DuringGame_PlayAtLocation( epicenter,
                                        new Vector3( 0, workingRand.NextFloat( 0, 360f ), 0 ) );


                                    QueuedBuildingDestructionData destructionData;
                                    destructionData.Epicenter = epicenter;
                                    destructionData.Range = MathRefs.FullNukeRadius.FloatMin;
                                    destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                                    destructionData.AlsoDestroyOtherItems = true;
                                    destructionData.AlsoDestroyUnits = true;
                                    destructionData.DestroyAllPlayerUnits = true;
                                    destructionData.SkipUnitsWithArmorPlatingAbove = 0;
                                    destructionData.SkipUnitsAboveHeight = 0;
                                    destructionData.IrradiateCells = true;
                                    destructionData.UnitsToSpawnAfter = null;
                                    destructionData.StatisticForDeaths = CityStatisticRefs.MurdersByNuke;
                                    destructionData.IsCausedByPlayer = true;
                                    destructionData.IsFromJob = null;
                                    destructionData.ExtraCode = CommonRefs.PlayerNukeExplosionExtraCode;

                                    MapEffectCoordinator.AddQueuedBuildingDestruction( destructionData );
                                }
                            }
                        }

                        CityStatisticRefs.NuclearReactorsExplodedByTampering.AlterScore_CityAndMeta( 1 );

                        TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "SliceOfInferno" );
                        {
                            TimelineGoalPath firstPath = goal.PathDict["ViaReactorTampering"];
                            if ( !firstPath.DuringGameplay_HasAchievedInThisTimeline )
                                firstPath.DuringGameplay_ExecutePathAchievedResultIfNeeded();
                            else
                            {
                                //if we already had the first one...
                                TimelineGoalPath secondPath = goal.PathDict["TwoInOneTimeline"];
                                if ( !secondPath.DuringGameplay_HasAchievedInThisTimeline )
                                    secondPath.DuringGameplay_ExecutePathAchievedResultIfNeeded();
                            }
                        }


                        int currentTurn = SimCommon.Turn;
                        CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;

                            int turnsFromNow = doomEvent.DuringGameplay_WillHappenOnTurn - currentTurn;
                            if ( turnsFromNow <= 0 )
                                continue;

                            int newTurn = currentTurn + (turnsFromNow / 2);

                            Interlocked.Exchange( ref doomEvent.DuringGameplay_WillHappenOnTurn, newTurn );
                            if ( doomEvent.DuringGameplay_WillHappenOnTurn < SimCommon.Turn )
                                doomEvent.DuringGameplay_WillHappenOnTurn = SimCommon.Turn;
                        }

                        shouldForceNoAfterActionReport = true;
                    }
                    #endregion
                    break;
                case "DestroyBuilding":
                    #region DestroyBuilding
                    {
                        if ( ForBuilding != null )
                        {
                            ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( ForBuilding.GetMapItem().OBBCache.BottomCenter,
                                new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                            ForBuilding.GetMapItem().DropBurningEffect_Slow();
                            ForBuilding.FullyDeleteBuilding();
                        }

                        shouldForceNoAfterActionReport = true;
                    }
                    #endregion
                    break;
                case "JumpToFinalDoom":
                    #region JumpToFinalDoom
                    {
                        int currentTurn = SimCommon.Turn;
                        CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                        DoomEvent lastDoomEvent = null;
                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;

                            int turnsFromNow = doomEvent.DuringGameplay_WillHappenOnTurn - currentTurn;
                            if ( turnsFromNow <= 0 )
                                continue;

                            lastDoomEvent = doomEvent;

                            Interlocked.Exchange( ref doomEvent.DuringGameplay_WillHappenOnTurn, -1 ); //make it not happen
                        }

                        if ( lastDoomEvent != null )
                            Interlocked.Exchange( ref lastDoomEvent.DuringGameplay_WillHappenOnTurn, currentTurn );

                        //if ( doomType != null && doomType.DuringGame_StartedOnTurn > 0 )
                        //    doomType.Implementation.HandleDoomLogic( doomType, CityDoomLogic.HandlePerTurn, workingRand );

                        shouldForceNoAfterActionReport = true;

                        SimCommon.MarkAsReadyForNextTurn();
                    }
                    #endregion
                    break;
                case "JumpTo9thDoom":
                    #region JumpTo9thDoom
                    {
                        int currentTurn = SimCommon.Turn;
                        CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                        DoomEvent lastDoomEvent = null;
                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;

                            if ( doomEvent.DoomNumber > 9 )
                            {
                                int desiredTurnsFromNow = doomEvent.DuringGameplay_WillHappenOnTurn - currentTurn;
                                if ( FlagRefs.HasStartedToAccelerateDooms_Extreme.DuringGameplay_IsTripped )
                                {
                                    if ( desiredTurnsFromNow > 30 )
                                        desiredTurnsFromNow = 30;
                                }
                                else if ( FlagRefs.HasStartedToAccelerateDooms_Hard.DuringGameplay_IsTripped )
                                {
                                    if ( desiredTurnsFromNow > 50 )
                                        desiredTurnsFromNow = 50;
                                }
                                else
                                {
                                    if ( desiredTurnsFromNow > 100 )
                                        desiredTurnsFromNow = 100;
                                }

                                doomEvent.DuringGameplay_WillHappenOnTurn = currentTurn + desiredTurnsFromNow;
                                continue;
                            }

                            int turnsFromNow = doomEvent.DuringGameplay_WillHappenOnTurn - currentTurn;
                            if ( turnsFromNow <= 0 )
                                continue;

                            lastDoomEvent = doomEvent;

                            Interlocked.Exchange( ref doomEvent.DuringGameplay_WillHappenOnTurn, SimCommon.Turn ); //make it happen now
                        }

                        if ( lastDoomEvent != null )
                            Interlocked.Exchange( ref lastDoomEvent.DuringGameplay_WillHappenOnTurn, currentTurn );

                        //if ( doomType != null && doomType.DuringGame_StartedOnTurn > 0 )
                        //    doomType.Implementation.HandleDoomLogic( doomType, CityDoomLogic.HandlePerTurn, workingRand );

                        shouldForceNoAfterActionReport = true;

                        SimCommon.MarkAsReadyForNextTurn();
                    }
                    #endregion
                    break;
                case "ObsessionBrainSurgery":
                    #region ObsessionBrainSurgery
                    {
                        MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_AnInexplicableCompulsion" )?.DoOnProjectFailIfActive( string.Empty, workingRand, false, false );
                        MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_FrustrationMounts" )?.DoOnProjectFailIfActive( string.Empty, workingRand, false, false );
                        FlagRefs.IsExperiencingObsession.UnTripIfNeeded();
                    }
                    #endregion
                    break;
                case "BrainPalAneurysms":
                    #region BrainPalAneurysms
                    {
                        FlagRefs.HasTriggeredBrainPalAneurysms.TripIfNeeded();
                        CityStatisticRefs.NeuralExpansionFromBrainPals.SetScore_WarningForSerializationOnly( 0 );
                        CityStatisticRefs.ComputeTimeFromBrainPals.SetScore_WarningForSerializationOnly( 0 );

                        EconomicClassType managerialClass = EconomicClassTypeTable.Instance.GetRowByID( "Managerial" );
                        EconomicClassType scientistsClass = EconomicClassTypeTable.Instance.GetRowByID( "Scientists" );

                        int toKill = (int)CityStatisticRefs.BrainPalsSold.GetScore();
                        int managersToKill = Mathf.CeilToInt( toKill * 0.6667f );
                        int scientistsToKill = toKill - managersToKill;

                        World.People.KillResidentsCityWide( managerialClass, ref managersToKill, workingRand );
                        if ( managersToKill > 0 )
                            scientistsToKill += managersToKill;

                        World.People.KillResidentsCityWide( scientistsClass, ref scientistsToKill, workingRand );

                        CityStatisticRefs.MurdersByBionicImplant.AlterScore_CityAndMeta( toKill );

                        OtherKeyMessageTable.Instance.GetRowByID( "AfterBrainPalAneurysms" ).DuringGameplay_IsReadyToBeViewed = true;
                    }
                    #endregion
                    break;
                case "ReleaseParkourBearsNow":
                    #region ReleaseParkourBearsNow
                    {
                        Vector3 position;
                        if ( ForBuilding == null )
                        {
                            if ( ForMachineActor != null  )
                                position = ForMachineActor.GetDrawLocation();
                            else
                                position = Engine_HotM.SelectedActor.GetDrawLocation();
                        }
                        else
                            position = ForBuilding.GetEffectiveWorldLocationForContainedUnit();

                        MapCell cell = CityMap.TryGetWorldCellAtCoordinates( position );

                        {
                            NPCUnitType parkourBear = NPCUnitTypeTable.Instance.GetRowByID( "ParkourBear" );
                            NPCUnitStance runningWild = NPCUnitStanceTable.Instance.GetRowByID( "ParkourBearRunning" );

                            for ( int i = 0; i < 16; i++ )
                            {
                                if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( position, cell, parkourBear, CohortRefs.WildAnimals, runningWild, 1f, Vector3.zero, -1f,
                                    false, 30, 1, CellRange.CellAndAdjacent2x, workingRand, CollisionRule.Relaxed, "ReleaseParkourBearsNow" ) == null )
                                {
                                    World.Forces.TryCreateNewNPCUnitWithinThisRadius( position, cell, parkourBear, CohortRefs.WildAnimals, runningWild, 1f, Vector3.zero, -1f,
                                    false, 60, 5, CellRange.CellAndAdjacent2x, workingRand, CollisionRule.Relaxed, "ReleaseParkourBearsNow" );
                                }
                            }
                        }

                        {
                            NPCUnitType humeOwnedCombatUnit = NPCUnitTypeTable.Instance.GetRowByID( "HumeOwnedCombatUnit" );
                            NPCUnitStance androidSecurityResponseTeam = NPCUnitStanceTable.Instance.GetRowByID( "AndroidSecurityResponseTeam" );

                            for ( int i = 0; i < 6; i++ )
                            {
                                World.Forces.TryCreateNewNPCUnitWithinThisRadius( position, cell, humeOwnedCombatUnit, CohortRefs.PeakHomes, androidSecurityResponseTeam, 1f, Vector3.zero, -1f,
                                    false, 30, 5, CellRange.CellAndAdjacent2x, workingRand, CollisionRule.Relaxed, "ReleaseParkourBearsNow" );
                            }
                        }

                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraEventChoiceConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraFinishInvestigatingASpecificBuildingOverTimeConsequences( ExtraCodeHandler Handler, ISimMachineActor Actor, 
            ISimBuilding BuildingThatWasInvestigated, InvestigationType Type, int InvestigationAttemptsSoFar, bool IsLastOne )
        {
            if ( Handler == null || Actor == null || BuildingThatWasInvestigated == null || Type == null )
                return;

            switch ( Handler.ID )
            {
                case "LiquidMetalFurtherPrisonBreaks":
                    #region LiquidMetalFurtherPrisonBreaks
                    {
                        int kills = Engine_Universal.PermanentQualityRandom.Next( 1800, 2700 );
                        CityStatisticTable.AlterScore( "CombatKillsHuman", kills );
                        CityStatisticTable.AlterScore( "LiquidMetalKillsInTightSpaces", kills );

                        ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( BuildingThatWasInvestigated.GetMapItem().OBBCache.BottomCenter,
                                new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                        BuildingThatWasInvestigated.GetMapItem().DropBurningEffect_Slow();
                        BuildingThatWasInvestigated.FullyDeleteBuilding();

                        FlagRefs.LiquidMetalFurtherPrisonBreaks.AlterScore_CityOnly( 1 );

                        switch ( FlagRefs.LiquidMetalFurtherPrisonBreaks.GetScore() )
                        {
                            case 1:
                                ResourceRefs.TraumatizedExCons.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 10010, 12000 ), string.Empty, ResourceAddRule.StoreExcess );
                                OtherKeyMessageTable.Instance.GetRowByID( "LiquidMetalFurtherPrisonBreaks1" ).DuringGameplay_IsReadyToBeViewed = true;
                                break;
                            case 2:
                                ResourceRefs.TraumatizedExCons.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 10010, 12000 ), string.Empty, ResourceAddRule.StoreExcess );
                                OtherKeyMessageTable.Instance.GetRowByID( "LiquidMetalFurtherPrisonBreaks2" ).DuringGameplay_IsReadyToBeViewed = true;
                                break;
                            case 3:
                                ResourceRefs.TraumatizedExCons.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 6010, 8000 ), string.Empty, ResourceAddRule.StoreExcess );
                                ResourceRefs.ExperimentalMonsters.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 2010, 3400 ), string.Empty, ResourceAddRule.StoreExcess );
                                OtherKeyMessageTable.Instance.GetRowByID( "LiquidMetalFurtherPrisonBreaks3" ).DuringGameplay_IsReadyToBeViewed = true;
                                break;
                            case 4:
                                ResourceRefs.TraumatizedExCons.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 10010, 12000 ), string.Empty, ResourceAddRule.StoreExcess );
                                ResourceRefs.ExperimentalMonsters.SetCurrent_Named( 0, string.Empty, false );
                                ResourceRefs.ExperimentalMonsters.RemoveFromExcessOverage( (int)ResourceRefs.ExperimentalMonsters.ExcessOverage );

                                OtherKeyMessageTable.Instance.GetRowByID( "LiquidMetalFurtherPrisonBreaks4" ).DuringGameplay_IsReadyToBeViewed = true;

                                FlagRefs.LiquidMetalAndroidsHaveDefected.TripIfNeeded();
                                break;
                        }
                    }
                    #endregion
                    break;
                case "Ch2YinshiUmbilicals":
                    #region Ch2YinshiUmbilicals
                    {
                        //if not done here, it won't happen during each investigation, only at the end
                        ResourceRefs.HumanUmbilicalCords.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 30, 50 ), string.Empty, ResourceAddRule.StoreExcess );
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraFinishInvestigatingASpecificBuildingOverTimeConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraCountdownCompleteConsequences( ExtraCodeHandler Handler, OtherCountdownType Countdown )
        {
            if ( Handler == null || Countdown == null )
                return;

            switch ( Handler.ID )
            {
                case "DagekonInvades":
                    #region DagekonInvades
                    {
                        FlagRefs.DagekonInvadesUntilTurn.SetScore_WarningForSerializationOnly( SimCommon.Turn + Engine_Universal.PermanentQualityRandom.Next( 45, 55 ) );
                        //yeah you will need it
                        FlagRefs.HasNeedForExtremeArmorPiercing.TripIfNeeded();
                        Calculators_Common.PulseAnyInvasionsOncePerTurn( true, Engine_Universal.PermanentQualityRandom );
                    }
                    #endregion
                    break;
                case "TheUIHInvades":
                    #region TheUIHInvades
                    {
                        FlagRefs.TheUIHInvadesUntilTurn.SetScore_WarningForSerializationOnly( SimCommon.Turn + Engine_Universal.PermanentQualityRandom.Next( 45, 55 ) );
                        Calculators_Common.PulseAnyInvasionsOncePerTurn( true, Engine_Universal.PermanentQualityRandom );

                        UnlockTable.Instance.GetRowByID( "HumaneTrap" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                    }
                    #endregion
                    break;
                case "WW4Starts":
                    #region WW4Starts
                    {
                        FlagRefs.Ch2_IsWW4Ongoing.TripIfNeeded();
                        Calculators_Common.PulseAnyInvasionsOncePerTurn( true, Engine_Universal.PermanentQualityRandom );

                        UnlockTable.Instance.GetRowByID( "HumaneTrap" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                        UnlockTable.Instance.GetRowByID( "MechArmor" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                        UnlockTable.Instance.GetRowByID( "MechWeapons" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );
                        UnlockTable.Instance.GetRowByID( "MechUltimate" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );

                        UpgradeIntTable.Instance.GetRowByID( "MaxMechs" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxMechs" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxMechs" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxMechs" )?.DuringGame_DoUpgrade( false );

                        UpgradeIntTable.Instance.GetRowByID( "MaxAndroids" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxAndroids" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxAndroids" )?.DuringGame_DoUpgrade( false );

                        UpgradeIntTable.Instance.GetRowByID( "MaxVehicles" )?.DuringGame_DoUpgrade( false );
                        UpgradeIntTable.Instance.GetRowByID( "MaxVehicles" )?.DuringGame_DoUpgrade( false );
                    }
                    #endregion
                    break;
                case "HeardBackFromMimicWithThumper":
                    #region HeardBackFromMimicWithThumper
                    {
                        FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.SetScore_WarningForSerializationOnly( SimCommon.Turn + Engine_Universal.PermanentQualityRandom.Next( 25, 30 ) );
                        Calculators_Common.PulseAnyInvasionsOncePerTurn( true, Engine_Universal.PermanentQualityRandom );
                        //yeah you will need it
                        FlagRefs.HasNeedForExtremeArmorPiercing.TripIfNeeded();

                        OtherKeyMessageTable.Instance.GetRowByID( "MimicThumperResult" ).DuringGameplay_IsReadyToBeViewed = true;

                        {
                            TimelineGoal escapeGoal = TimelineGoalTable.Instance.GetRowByID( "EscapeTheCity" );
                            if ( escapeGoal != null )
                                TimelineGoalHelper.HandleGoalPathCompletion( escapeGoal, "AtLeastOneMimicEscaped" );
                        }
                    }
                    #endregion
                    break;
                case "HeardBackFromMimicWithoutThumper":
                    #region HeardBackFromMimicWithoutThumper
                    {
                        OtherKeyMessageTable.Instance.GetRowByID( "MimicNoThumperResult" ).DuringGameplay_IsReadyToBeViewed = true;
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraCountdownCompleteConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraNPCCompletedObjectiveConsequences( ExtraCodeHandler Handler, ISimNPCUnit Unit, NPCUnitObjective Objective, MersenneTwister Rand, 
            NPCMission relatedMission, ISimMapActor objectiveActor, ISimBuilding objectiveBuilding )
        {
            if ( Handler == null || Unit == null || Objective == null || Rand == null )
                return;

            switch ( Handler.ID )
            {
                case "PlayerWorkerMurderDissidents":
                    #region PlayerWorkerMurderDissidents
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() )
                    {
                        objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        int murders = 0;
                        foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                        {
                            int toKill = objectiveBuilding.GetResidentAmount( econClass );
                            if ( toKill > 0 )
                                murders += toKill;
                        }
                        foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                        {
                            int toKill = objectiveBuilding.GetWorkerAmount( profession );
                            if ( toKill > 0 )
                                murders += toKill;
                        }

                        if ( murders > 0 )
                            CityStatisticTable.AlterScore( "MurdersByWorkerAndroids", murders );

                        int lowerClass = 0;
                        foreach ( EconomicClassType econClass in CommonRefs.LowerAndWorkingClassResidents )
                            lowerClass += objectiveBuilding.GetResidentAmount( econClass );
                        foreach ( ProfessionType profession in CommonRefs.LowerAndWorkingClassProfessions )
                            lowerClass += objectiveBuilding.GetWorkerAmount( profession );

                        objectiveBuilding.KillEveryoneHere(); //not really killing in many cases, but certainly removing from the normal citizen roster

                        if ( lowerClass > 0 )
                        {
                            objectiveBuilding.SwarmSpread = SwarmTable.Instance.GetRowByID( "CybercraticCitizens" );
                            objectiveBuilding.AlterSwarmSpreadCount( lowerClass );

                            CityStatisticTable.AlterScore( "CybercraticCitizensJoined", lowerClass );
                        }
                    }
                    #endregion
                    break;
                case "PlayerWorkerRemoveDissidents":
                case "PlayerWorkerInstallCyberocracy":
                    #region PlayerWorkerRemoveDissidents / PlayerWorkerInstallCyberocracy
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() )
                    {
                        objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        int removed = 0;
                        foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                        {
                            int toKill = objectiveBuilding.GetResidentAmount( econClass );
                            if ( toKill > 0 )
                                removed += toKill;
                        }
                        foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                        {
                            int toKill = objectiveBuilding.GetWorkerAmount( profession );
                            if ( toKill > 0 )
                                removed += toKill;
                        }

                        if ( removed > 0 )
                            CityStatisticTable.AlterScore( "CybercraticDissidentsRemoved", removed );

                        int lowerClass = 0;
                        foreach ( EconomicClassType econClass in CommonRefs.LowerAndWorkingClassResidents )
                            lowerClass += objectiveBuilding.GetResidentAmount( econClass );
                        foreach ( ProfessionType profession in CommonRefs.LowerAndWorkingClassProfessions )
                            lowerClass += objectiveBuilding.GetWorkerAmount( profession );

                        objectiveBuilding.KillEveryoneHere(); //not really killing in many cases, but certainly removing from the normal citizen roster

                        if ( lowerClass > 0 )
                        {
                            objectiveBuilding.SwarmSpread = SwarmTable.Instance.GetRowByID( "CybercraticCitizens" );
                            objectiveBuilding.AlterSwarmSpreadCount( lowerClass );

                            CityStatisticTable.AlterScore( "CybercraticCitizensJoined", lowerClass );
                        }
                    }
                    #endregion
                    break;
                case "PlayerWorkerKidnapUpperClass":
                case "PlayerWorkerKidnapLowerClass":
                    #region PlayerWorkerKidnapUpperClass / PlayerWorkerKidnapLowerClass
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() )
                    {
                        bool isLowerClass = Handler.ID == "PlayerWorkerKidnapLowerClass";

                        //objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        if ( isLowerClass )
                        {
                            int total = 0;
                            foreach ( EconomicClassType econClass in CommonRefs.LowerAndWorkingClassResidents )
                            {
                                int toAdd = objectiveBuilding.GetResidentAmount( econClass );
                                if ( toAdd > 0 )
                                {
                                    total += toAdd;
                                    objectiveBuilding.KillSomeResidentsHere( econClass, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                }
                            }
                            foreach ( ProfessionType profession in CommonRefs.LowerAndWorkingClassProfessions )
                            {
                                int toAdd = objectiveBuilding.GetWorkerAmount( profession );
                                if ( toAdd > 0 )
                                {
                                    total += toAdd;
                                    objectiveBuilding.KillSomeWorkersHere( profession, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                }
                            }

                            if ( total > 0 )
                            {
                                CityStatisticTable.AlterScore( "LowerClassHumansShovedIntoTormentVessels", total );
                                ResourceRefs.TormentedHumans.AlterCurrent_Named( total, "Increase_KidnappedByYourWorkers", ResourceAddRule.StoreExcess );
                            }
                        }
                        else
                        {
                            int total = 0;
                            foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                            {
                                int toAdd = objectiveBuilding.GetResidentAmount( econClass );
                                if ( toAdd > 0 )
                                {
                                    total += toAdd;
                                    objectiveBuilding.KillSomeResidentsHere( econClass, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                }
                            }
                            foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                            {
                                int toAdd = objectiveBuilding.GetWorkerAmount( profession );
                                if ( toAdd > 0 )
                                {
                                    total += toAdd;
                                    objectiveBuilding.KillSomeWorkersHere( profession, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                }
                            }

                            if ( total > 0 )
                            {
                                CityStatisticTable.AlterScore( "UpperClassHumansShovedIntoTormentVessels", total );
                                ResourceRefs.TormentedHumans.AlterCurrent_Named( total, "Increase_KidnappedByYourWorkers", ResourceAddRule.StoreExcess );
                            }
                        }
                    }
                    #endregion
                    break;
                case "ClearOutSlumBuilding":
                    #region ClearOutSlumBuilding
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() && objectiveBuilding.GetMapItem() != null &&
                        !objectiveBuilding.GetMapItem().IsInPoolAtAll )
                    {
                        //objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        Vector3 epicenter = objectiveBuilding.GetMapItem().OBBCache.BottomCenter;
                        int peopleInBuilding = objectiveBuilding.GetTotalResidentCount() + objectiveBuilding.GetTotalWorkerCount();

                        objectiveBuilding.KillEveryoneHere();

                        ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                        objectiveBuilding.GetMapItem().DropBurningEffect_Slow();
                        objectiveBuilding.FullyDeleteBuilding();

                        CityStatisticTable.AlterScore( "CitizensForciblyRescuedFromSlums", peopleInBuilding );

                        ResourceRefs.ShelteredHumans.AlterCurrent_Named( peopleInBuilding, string.Empty, ResourceAddRule.StoreExcess );

                        //ArcenNotes.SendSimpleNoteToGameOnly( NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                        //    NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0 );

                        if ( peopleInBuilding > 1000 )
                            ManagerRefs.Man_SlumRescueWeakReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                    }
                    #endregion
                    break;
                case "DemolishBank":
                    #region DemolishBank
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() && objectiveBuilding.GetMapItem() != null &&
                        !objectiveBuilding.GetMapItem().IsInPoolAtAll )
                    {
                        //objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        Vector3 epicenter = objectiveBuilding.GetMapItem().OBBCache.BottomCenter;
                        int peopleInBuilding = objectiveBuilding.GetTotalResidentCount() + objectiveBuilding.GetTotalWorkerCount();

                        objectiveBuilding.KillEveryoneHere();

                        ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                        objectiveBuilding.GetMapItem().DropBurningEffect_Slow();
                        objectiveBuilding.FullyDeleteBuilding();

                        CityStatisticTable.AlterScore( "BanksDemolished", 1 );
                        CityStatisticRefs.MurdersByWorkerAndroids.AlterScore_CityAndMeta( peopleInBuilding );
                        
                        //ArcenNotes.SendSimpleNoteToGameOnly( NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                        //    NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0 );

                        ManagerRefs.Man_BankDemolishedReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                    }
                    #endregion
                    break;
                case "DestroySlumBuilding":
                    #region DestroySlumBuilding
                    if ( objectiveBuilding != null && !objectiveBuilding.GetIsDestroyed() && objectiveBuilding.GetMapItem() != null &&
                        !objectiveBuilding.GetMapItem().IsInPoolAtAll )
                    {
                        //objectiveBuilding.IsBlockedFromGettingMoreCitizens = true;

                        Vector3 epicenter = objectiveBuilding.GetMapItem().OBBCache.BottomCenter;
                        int peopleInBuilding = objectiveBuilding.GetTotalResidentCount() + objectiveBuilding.GetTotalWorkerCount();

                        objectiveBuilding.KillEveryoneHere();

                        ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                        objectiveBuilding.GetMapItem().DropBurningEffect_Slow();
                        objectiveBuilding.FullyDeleteBuilding();

                        CityStatisticTable.AlterScore( "DeathsFromCorporateRevengeOnSlums", peopleInBuilding );
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraNPCCompletedObjectiveConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraNPCDialogChoiceConsequences( ExtraCodeHandler Handler, ISimMachineActor YourMachineActor, ISimNPCUnit WithUnit,
            MersenneTwister workingRand, bool IsFromAfterDebate, NPCDialog Dialog, NPCDialogChoice Choice )
        {
            if ( Handler == null || workingRand == null || Dialog == null || Choice == null )
                return;

            switch ( Handler.ID )
            {
                case "MurderedAtcaRetailPresident":
                    #region HasBlackmailDealWithAtcaRetail
                    {
                        CityStatisticRefs.Murders.AlterScore_CityAndMeta( 1 );
                    }
                    #endregion
                    break;
                case "PostProtest_Experiment":
                    #region PostProtest_Experiment
                    {
                        TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "AdvocateEncounter" );
                        TimelineGoalHelper.HandleGoalPathCompletion( goal, "EarnedSomeRespect" );
                    }
                    #endregion
                    break;
                case "PostProtest_Hate":
                    #region PostProtest_Hate
                    {
                        TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "AdvocateEncounter" );
                        TimelineGoalHelper.HandleGoalPathCompletion( goal, "EarnedSomeRespect" );
                    }
                    #endregion
                    break;
                case "PostProtest_Wish":
                    #region PostProtest_Wish
                    {
                        TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "AdvocateEncounter" );
                        TimelineGoalHelper.HandleGoalPathCompletion( goal, "EarnedSomeRespect" );
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraNPCDialogChoiceConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraOtherKeyMessageOptionConsequences( ExtraCodeHandler Handler, OtherKeyMessageOption Option, OtherKeyMessage Message )
        {
            if ( Handler == null || Option == null || Message == null )
                return;

            switch ( Handler.ID )
            {
                case "KillAllAGIAndDestroyBridge":
                    int current = (int)ResourceRefs.FugitiveAGIResearchers.Current;
                    if ( current > 0 )
                    {
                        ResourceRefs.FugitiveAGIResearchers.SetCurrent_Named( 0, string.Empty, true );
                        CityStatisticTable.AlterScore( "Murders", current );
                    }

                    foreach ( MachineStructure structure in JobRefs.NeuralBridge.DuringGame_FullList.GetDisplayList() )
                    {
                        structure.ScrapStructureNow( ScrapReason.Cheat, Engine_Universal.PermanentQualityRandom );
                    }
                    break;
                case "BrainPalAneurysmsP2":
                    #region BrainPalAneurysmsP2
                    {
                        TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "CollapseACriminalSyndicate" );
                        TimelineGoalHelper.HandleGoalPathCompletion( goal, "ExoticImporters" );

                        NPCCohortTable.Instance.GetRowByID( "ExoticImporters" ).DuringGame_HasBeenDisbanded = true;
                    }
                    #endregion
                    break;
                case "AfterEconomicCollapseP2":
                    #region BrainPalAneurysmsP2
                    {
                        {
                            TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "CollapseAFederatedCorporation" );
                            TimelineGoalHelper.HandleGoalPathCompletion( goal, "AtcaRetail" );
                        }
                        {
                            TimelineGoal goal = TimelineGoalTable.Instance.GetRowByID( "CauseACivilWar" );
                            TimelineGoalHelper.HandleGoalPathCompletion( goal, "Billionaire" );
                        }

                        NPCCohortTable.Instance.GetRowByID( "AtcaRetail" ).DuringGame_HasBeenDisbanded = true;

                        FlagRefs.Ch2_IsCivilWarOngoing.TripIfNeeded();

                        ResourceRefs.Wealth.AlterCurrent_Named( 16000000000000000, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraOtherKeyMessageOptionConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExplosionImminentBuilding( ExtraCodeHandler Handler, ISimBuilding BuildingAboutToDie, BuildingStatus StatusToApply, bool IrradiateCells, bool IsCausedByPlayer, MachineJob IsFromJob )
        {
            if ( Handler == null || BuildingAboutToDie == null )
                return;

            switch ( Handler.ID )
            {
                case "LOXBunkerExplosion":                    
                    break; //nothing to do
                case "PlayerNukeExplosion":
                    if ( SimCommon.TheNetwork?.Tower != null && SimCommon.TheNetwork?.Tower == BuildingAboutToDie?.MachineStructureInBuilding )
                        AchievementRefs.IMeantToDoThat.TripIfNeeded();
                    break; //nothing to do
                default:
                    ArcenDebugging.LogSingleLine( "HandleExplosionImminentBuilding: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExplosionImminentActor( ExtraCodeHandler Handler, ISimMapMobileActor ActorAboutToDie, bool IrradiateCells, bool IsCausedByPlayer, MachineJob IsFromJob )
        {
            if ( Handler == null || ActorAboutToDie == null )
                return;

            switch ( Handler.ID )
            {
                case "LOXBunkerExplosion":
                    if ( AchievementRefs.OxygenBomb.OneTimeline_HasBeenTripped )
                        return; //nothing more to do

                    if ( ActorAboutToDie is ISimNPCUnit npcUnit )
                    {
                        if ( !npcUnit.GetIsPartOfPlayerForcesInAnyWay() )
                            AchievementRefs.OxygenBomb.TripIfNeeded();
                    }
                    break;
                case "PlayerNukeExplosion":
                    break; //nothing to do
                default:
                    ArcenDebugging.LogSingleLine( "HandleExplosionImminentActor: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleCityLensRightClick( ExtraCodeHandler Handler, CityLensType Lens )
        {
            if ( Handler == null || Lens == null )
                return;

            switch ( Handler.ID )
            {
                case "BasicCityLens":
                    switch (Lens.ID)
                    {
                        case "StreetSense":
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.StreetSense );
                            break;
                        case "Contemplations":
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.Contemplations );
                            break;
                        case "CityConflicts":
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.CityConflicts );
                            break;
                        case "ExplorationSites":
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.ExplorationSites );
                            break;
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleCityLensRightClick: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void HandleExtraNPCKilledByPlayerConsequences( ExtraCodeHandler Handler, ISimNPCUnit UnitDoingTheDying, Vector3 loc, MersenneTwister workingRand, bool HasBeenPhysicallyDamagedByPlayer )
        {
            if ( Handler == null || UnitDoingTheDying == null )
                return;

            switch ( Handler.ID )
            {
                case "SlumWealthProtesterKilled":
                    {
                        CityFlagTable.Instance.GetRowByID( "SlumWealthProtestersDispersed" )?.TripIfNeeded();

                        //if ( UnitDoingTheDying.HasBeenPhysicallyDamagedByPlayer )
                        //{
                            SimCommon.TheNetwork?.Tower?.ScrapStructureNow( ScrapReason.CaughtInExplosion, workingRand );
                            OtherKeyMessageTable.Instance.GetRowByID( "AdvocatesDestroyedTower" ).DuringGameplay_IsReadyToBeViewed = true;
                            AchievementTable.Instance.GetRowByID( "ReallyNotTheWay" )?.TripIfNeeded();
                        //}
                        //else
                        //{
                        //    OtherKeyMessageTable.Instance.GetRowByID( "AfterSlumWealthProtesterTasered" ).DuringGameplay_IsReadyToBeViewed = true;
                        //}
                    }
                    break;
                case "SlumWealthProtesterTasered":
                    {
                        CityFlagTable.Instance.GetRowByID( "SlumWealthProtestersDispersed" )?.TripIfNeeded();

                        OtherKeyMessageTable.Instance.GetRowByID( "AfterSlumWealthProtesterTasered" ).DuringGameplay_IsReadyToBeViewed = true;
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "HandleExtraNPCKilledByPlayerConsequences: ExtraCode_Common was asked to handle '" + Handler.ID + "', but no entry was set up for that!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Dooms_Basic : ICityTimelineDoomTypeImplementation
    {
        public void HandleDoomLogic( CityTimelineDoomType DoomType, CityDoomLogic Logic, MersenneTwister Rand )
        {
            if ( DoomType == null )
                return;

            switch ( DoomType.ID )
            {
                case "VorsibersWrath":
                    #region VorsibersWrath
                    {
                        switch ( Logic )
                        {
                            case CityDoomLogic.HandlePerTurn:
                                VorsiberWrathPerTurn( DoomType, Rand );
                                break;
                        }
                    }
                    break;
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "Dooms_Basic: Called HandleDoomLogic for '" + DoomType.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        private static readonly List<ISimNPCUnit> workingNPCList = List<ISimNPCUnit>.Create_WillNeverBeGCed( 10, "Dooms_Basic-workingNPCList" );

        #region VorsiberWrathPerTurn
        private void VorsiberWrathPerTurn( CityTimelineDoomType DoomType, MersenneTwister Rand )
        {
            int debugStage = 0;
            try
            {
                bool isJustNowTriggeringSecondDoom = false;
                bool hasNeverTriggeredSecondDoom = false;
                int largestDoomNumberDone = 0;
                foreach ( DoomEvent doomEvent in DoomType.DoomMainEvents )
                {
                    if ( doomEvent.DuringGameplay_HasHappened )
                        largestDoomNumberDone = MathA.Max( largestDoomNumberDone, doomEvent.DoomNumber );

                    switch ( doomEvent.ID )
                    {
                        case "1":
                            #region Death In The Black Market
                            {
                                DoomAlternativeEvent altEvent = null;
                                if ( FlagRefs.CrossoverWithHomoGrandien.DuringGameplay_IsTripped )
                                    altEvent = DoomType.DoomAltEventDict["1PreventedHomoGrandien"];

                                if ( doomEvent.TryTriggerNowIfNeeded( altEvent ) )
                                {
                                    if ( altEvent == null )
                                    {
                                        if ( !KeyContactRefs.BlackMarketTradesman.DuringGame_IsDead )
                                        {
                                            KeyContactRefs.BlackMarketTradesman.DuringGame_IsDead = true;
                                            KeyContactRefs.BlackMarketTradesman.DuringGame_KilledOnTurn = SimCommon.Turn;
                                        }
                                        if ( !KeyContactRefs.BlackMarketAssistant.DuringGame_IsDead )
                                        {
                                            KeyContactRefs.BlackMarketAssistant.DuringGame_IsDead = true;
                                            KeyContactRefs.BlackMarketAssistant.DuringGame_KilledOnTurn = SimCommon.Turn;
                                        }
                                    }
                                }
                                else if ( doomEvent.DuringGameplay_HasHappened && doomEvent.DuringGameplay_AlternativeHappened == null )
                                {
                                    if ( !KeyContactRefs.BlackMarketTradesman.DuringGame_IsDead )
                                    {
                                        KeyContactRefs.BlackMarketTradesman.DuringGame_IsDead = true;
                                        KeyContactRefs.BlackMarketTradesman.DuringGame_KilledOnTurn = SimCommon.Turn;
                                    }
                                    if ( !KeyContactRefs.BlackMarketAssistant.DuringGame_IsDead )
                                    {
                                        KeyContactRefs.BlackMarketAssistant.DuringGame_IsDead = true;
                                        KeyContactRefs.BlackMarketAssistant.DuringGame_KilledOnTurn = SimCommon.Turn;
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "2":
                            #region Famine
                            if ( doomEvent.TryTriggerNowIfNeeded( null ) )
                            {
                                isJustNowTriggeringSecondDoom = true;
                                FlagRefs.HasActiveFamine.TripIfNeeded();
                            }
                            else if ( doomEvent.DuringGameplay_HasHappened )
                            {
                                if ( FlagRefs.HasActiveFamine.DuringGameplay_IsTripped ) //if false, the famine finished
                                {
                                    int numberOfNewDesperateHomeless = Rand.NextInclus( 2, 380 );
                                    CityStatisticTable.AlterScore( "DesperateHomeless", numberOfNewDesperateHomeless );
                                }
                            }

                            if ( !doomEvent.DuringGameplay_HasHappened && doomEvent.DuringGameplay_AlternativeHappened == null && doomEvent.DuringGameplay_WillHappenOnTurn > 0 )
                                hasNeverTriggeredSecondDoom = true;
                            #endregion
                            break;
                        case "3":
                            #region Prisoner Drive
                            if ( doomEvent.TryTriggerNowIfNeeded( null ) )
                            {
                                FlagRefs.HasActivePrisonerDrive.TripIfNeeded();
                                FlagRefs.HasActiveFamine.UnTripIfNeeded();
                            }
                            else if ( doomEvent.DuringGameplay_HasHappened )
                            {
                                if ( FlagRefs.HasActivePrisonerDrive.DuringGameplay_IsTripped ) //if false, the prisoner drive finished
                                {
                                    //prisoner stuff happens from a manager
                                }
                            }
                            #endregion
                            break;
                        case "4":
                            #region Religious Schism
                            {
                                DoomAlternativeEvent altEvent = null;
                                if ( FlagRefs.CrossoverWithHomoGrandien.DuringGameplay_IsTripped )
                                    altEvent = DoomType.DoomAltEventDict["4PreventedHomoGrandien"];

                                if ( doomEvent.TryTriggerNowIfNeeded( altEvent ) )
                                {
                                    if ( altEvent == null )
                                    {
                                        if ( !KeyContactRefs.ExalterGeneticist.DuringGame_IsDead )
                                        {
                                            KeyContactRefs.ExalterGeneticist.DuringGame_IsDead = true;
                                            KeyContactRefs.ExalterGeneticist.DuringGame_KilledOnTurn = SimCommon.Turn;
                                        }
                                        CohortRefs.NurturismExalters.DuringGame_HasBeenDisbanded = true;
                                    }

                                    FlagRefs.HasActivePrisonerDrive.UnTripIfNeeded();
                                }
                                else if ( doomEvent.DuringGameplay_HasHappened && doomEvent.DuringGameplay_AlternativeHappened == null )
                                {
                                    if ( !KeyContactRefs.ExalterGeneticist.DuringGame_IsDead )
                                    {
                                        KeyContactRefs.ExalterGeneticist.DuringGame_IsDead = true;
                                        KeyContactRefs.ExalterGeneticist.DuringGame_KilledOnTurn = SimCommon.Turn;
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "5":
                            #region Space Nation Invasion
                            if ( doomEvent.TryTriggerNowIfNeeded( null ) )
                            {
                                FlagRefs.HasActiveSpaceNationInvasion.TripIfNeeded();
                            }
                            else if ( doomEvent.DuringGameplay_HasHappened )
                            {
                                if ( FlagRefs.HasActiveSpaceNationInvasion.DuringGameplay_IsTripped ) //if false, the prisoner drive finished
                                {
                                    //space nation invasions happen in managers
                                }
                            }
                            #endregion
                            break;
                        case "6":
                            #region Vorsiber Assassinations
                            if ( !doomEvent.DuringGameplay_HasHappened )
                            {
                                DoomAlternativeEvent altEvent = null;
                                if ( FlagRefs.LiquidMetalWoodsman.DuringGameplay_IsTripped )
                                    altEvent = DoomType.DoomAltEventDict["6PreventedLiq"];

                                if ( doomEvent.TryTriggerNowIfNeeded( altEvent ) )
                                {
                                    FlagRefs.HasActiveSpaceNationInvasion.UnTripIfNeeded();
                                    if ( altEvent == null )
                                    {
                                        FlagRefs.HasSecForceSuperCruisersRoaming.TripIfNeeded();
                                        FlagRefs.Ch2_VorsiberAssassinationsHappened.TripIfNeeded();
                                    }
                                    else
                                        FlagRefs.Ch2_VorsiberAssassinationsPrevented.TripIfNeeded();
                                }
                            }
                            #endregion
                            break;
                        case "7":
                            #region Vehicular Cyber-Attack
                            if ( doomEvent.TryTriggerNowIfNeeded( null ) )
                            {
                                workingNPCList.Clear();
                                foreach ( ISimMapActor unit in SimCommon.AllActorsForTargeting.GetDisplayList() )
                                {
                                    if ( unit is ISimNPCUnit npcUnit && npcUnit.UnitType.IsVehicle && !npcUnit.UnitType.IsImmuneToThirdPartyCyberAttacks )
                                    {
                                        if ( npcUnit.FromCohort.IsInwardLookingMegacorpAlly )
                                        {
                                            switch (npcUnit.UnitType.ID)
                                            {
                                                case "SecForceSuperCruiser":
                                                case "MilitaryTroopCarrier":
                                                    workingNPCList.Add( npcUnit );
                                                    break;
                                            }
                                        }
                                    }
                                }

                                if ( workingNPCList.Count > 0 )
                                {
                                    int toDestroy = workingNPCList.Count / 2;
                                    if ( toDestroy < 2 )
                                        toDestroy = 2;

                                    while ( toDestroy > 0 && workingNPCList.Count > 0 )
                                    {
                                        ISimNPCUnit npcUnit = workingNPCList.GetRandom( Rand );
                                        workingNPCList.Remove( npcUnit );

                                        float radius = npcUnit.UnitType.RadiusForCollisions;
                                        radius *= 2; //explosion is 2x larger than the unit

                                        Vector3 epicenter = npcUnit.GetCollisionCenter();
                                        switch ( npcUnit.UnitType.ID )
                                        {
                                            case "SecForceSuperCruiser": //explosion radius: 11.6
                                                ParticleSoundRefs.VehicleExplosionA.DuringGame_PlayAtLocation( epicenter,
                                                       new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );
                                                break;
                                            case "MilitaryTroopCarrier": //explosion radius: 8.8
                                                ParticleSoundRefs.VehicleExplosionB.DuringGame_PlayAtLocation( epicenter,
                                                        new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );
                                                break;
                                        }

                                        QueuedBuildingDestructionData destructionData;
                                        destructionData.Epicenter = epicenter;
                                        destructionData.Range = radius;
                                        destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                                        destructionData.AlsoDestroyOtherItems = true;
                                        destructionData.AlsoDestroyUnits = true;
                                        destructionData.DestroyAllPlayerUnits = true;
                                        destructionData.SkipUnitsWithArmorPlatingAbove = 800;
                                        destructionData.SkipUnitsAboveHeight = 10;
                                        destructionData.IrradiateCells = true;
                                        destructionData.UnitsToSpawnAfter = null;
                                        destructionData.StatisticForDeaths = CityStatisticRefs.DeathsDuringVehicularCyberAttack;
                                        destructionData.IsCausedByPlayer = false;
                                        destructionData.IsFromJob = null;
                                        destructionData.ExtraCode = null;

                                        SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );

                                        npcUnit.DisbandNPCUnit( NPCDisbandReason.CaughtInExplosion );
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "8":
                            #region Cultist Sabotage
                            if ( doomEvent.TryTriggerNowIfNeeded( null ) )
                            {
                                FlagRefs.HasActiveChildrenOfGaiaCultistSabotage.TripIfNeeded();
                            }
                            else if ( doomEvent.DuringGameplay_HasHappened )
                            {
                                if ( FlagRefs.HasActiveChildrenOfGaiaCultistSabotage.DuringGameplay_IsTripped ) //if false, the prisoner drive finished
                                {
                                    //cultist sabotage things happen in a manager
                                }
                            }
                            #endregion
                            break;
                        case "9":
                            #region Religious Sites Eliminated
                            if ( !doomEvent.DuringGameplay_HasHappened )
                            {
                                DoomAlternativeEvent altEvent = null;
                                if ( DoomType.DoomMainEventDict["6"].DuringGameplay_AlternativeHappened == DoomType.DoomAltEventDict["6PreventedLiq"] )
                                    altEvent = DoomType.DoomAltEventDict["9DeepCalm"];

                                if ( doomEvent.TryTriggerNowIfNeeded( altEvent ) )
                                {
                                    FlagRefs.HasActiveChildrenOfGaiaCultistSabotage.UnTripIfNeeded();

                                    if ( altEvent == null )
                                    {
                                        foreach ( ISimBuilding building in CommonRefs.HistoricTempleTag.DuringGame_Buildings.GetDisplayList() )
                                        {
                                            float radius = 4;
                                            Vector3 epicenter = building.GetMapItem().OBBCache.BottomCenter;
                                            ParticleSoundRefs.TempleExplosion.DuringGame_PlayAtLocation( epicenter,
                                                   new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                            QueuedBuildingDestructionData destructionData;
                                            destructionData.Epicenter = epicenter;
                                            destructionData.Range = radius;
                                            destructionData.StatusToApply = CommonRefs.DemolishedBuildingStatus;
                                            destructionData.AlsoDestroyOtherItems = false;
                                            destructionData.AlsoDestroyUnits = true;
                                            destructionData.DestroyAllPlayerUnits = true;
                                            destructionData.SkipUnitsWithArmorPlatingAbove = 800;
                                            destructionData.SkipUnitsAboveHeight = 7;
                                            destructionData.IrradiateCells = false;
                                            destructionData.UnitsToSpawnAfter = null;
                                            destructionData.StatisticForDeaths = CityStatisticRefs.DeathsDuringReligiousSiteElimination;
                                            destructionData.IsCausedByPlayer = false;
                                            destructionData.IsFromJob = null;
                                            destructionData.ExtraCode = null;

                                            SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );
                                        }
                                    }
                                }
                            }
                            #endregion
                            break;
                        case "10":
                            #region Nuclear Delivery
                            {
                                DoomAlternativeEvent altEvent = null;
                                if ( FlagRefs.Ch2_IsWW4Ongoing.DuringGameplay_IsTripped )
                                    altEvent = DoomType.DoomAltEventDict["10WW4"];

                                if ( doomEvent.TryTriggerNowIfNeeded( altEvent ) )
                                {
                                    if ( altEvent != null )
                                    {
                                        if ( SimMetagame.CurrentChapterNumber < 4 )
                                            SimMetagame.AdvanceToNextChapter();

                                        {
                                            TimelineGoal ww4Goal = TimelineGoalTable.Instance.GetRowByID( "WorldWar4" );
                                            if ( ww4Goal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( ww4Goal, "Averted" );
                                        }
                                    }
                                    else
                                    {
                                        //note, this would be too early
                                        //FlagRefs.IsPostFinalDoom.TripIfNeeded();
                                        //FlagRefs.IsPostNuclearDelivery.TripIfNeeded();

                                        if ( isJustNowTriggeringSecondDoom || hasNeverTriggeredSecondDoom )
                                            AchievementRefs.YoureReallyUnlikeable.TripIfNeeded();

                                        ISimMachineUnit unitToUse = null;
                                        if ( Engine_HotM.SelectedActor is ISimMachineUnit existingU && existingU.UnitType.IsConsideredAndroid )
                                            unitToUse = existingU; //we all good already
                                        else
                                        {
                                            foreach ( ISimMachineActor machineActor in SimCommon.AllMachineActors.GetDisplayList() )
                                            {
                                                if ( machineActor is ISimMachineUnit otherU && otherU.UnitType.IsConsideredAndroid )
                                                {
                                                    Engine_HotM.SetSelectedActor( otherU, false, true, true );
                                                    unitToUse = otherU;
                                                    break;
                                                }
                                            }
                                        }

                                        //player may be trying to be clever
                                        if ( unitToUse == null )
                                        {
                                            MapCell effectiveCell = CityMap.CurrentCameraCenterCell;

                                            //generate one Technician
                                            unitToUse = World.Forces.TryCreateNewMachineUnitAtRandomLocationOnCell( effectiveCell, CommonRefs.TechnicianUnitType, string.Empty,
                                                CellRange.CellAndAdjacent2x, Engine_Universal.PermanentQualityRandom, true, CollisionRule.Strict, true );

                                            if ( unitToUse == null )
                                            {
                                                foreach ( MapCell cell in CityMap.Cells )
                                                {
                                                    unitToUse = World.Forces.TryCreateNewMachineUnitAtRandomLocationOnCell( cell, CommonRefs.TechnicianUnitType, string.Empty,
                                                        CellRange.ThisCell, Engine_Universal.PermanentQualityRandom, true, CollisionRule.Strict, true );
                                                    if ( unitToUse != null )
                                                        break;
                                                }
                                                if ( unitToUse == null )
                                                    ArcenDebugging.LogSingleLine( "Null unitToUse after trying all cells!", Verbosity.ShowAsError );
                                            }
                                        }

                                        //START_MINOR_EVENT
                                        FlagRefs.Doom_NuclearDelivery.ClearForMinorEvent();

                                        QueuedMinorEvent mEvent = new QueuedMinorEvent();
                                        mEvent.Actor = unitToUse;
                                        mEvent.MinorEvent = FlagRefs.Doom_NuclearDelivery;
                                        mEvent.BuildingOrNull = SimCommon.TheNetwork?.Tower?.Building;
                                        mEvent.EventCohort = CohortRefs.Yourself;
                                        mEvent.ClearAnyExistingMusicTagsIfAnyExist = false;
                                        SimCommon.QueuedMinorEvents.Enqueue( mEvent );
                                    }
                                }
                            }
                            #endregion
                            break;
                    }
                }

                if ( SimCommon.CurrentTimeline != null )
                    SimCommon.CurrentTimeline.DoomNumber = largestDoomNumberDone;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "VorsibersWrath", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}

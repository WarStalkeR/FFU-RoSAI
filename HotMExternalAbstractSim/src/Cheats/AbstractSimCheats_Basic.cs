using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using DiffLib;
using System.Diagnostics;

namespace Arcen.HotM.External
{
    public class AbstractSimCheats_Basic : ICheatTypeImplementation
    {
        public void FillListOfSecondaryDataSource(  CheatType CheatType, List<ICheatSecondaryDataSource> RowsToFill, 
            out ICheatSecondaryDataSource DefaultRow )
        {
            RowsToFill.Clear();
            if ( CheatType.SubOptions.Count > 0 )
            {
                DefaultRow = CheatType.SubOptions[0];
                foreach ( CheatSubOption item in CheatType.SubOptions )
                    RowsToFill.Add( item );
                return;
            }

            switch ( CheatType.ID )
            {
                case "GoToNextBuilding":
                    {
                        DefaultRow = BuildingTypeTable.Instance.DefaultRow == null ? BuildingTypeTable.Instance.Rows[0] : BuildingTypeTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( BuildingType row in BuildingTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "ChangeWeather":
                    {
                        DefaultRow = WeatherStyleTable.Instance.DefaultRow == null ? WeatherStyleTable.Instance.Rows[0] : WeatherStyleTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( WeatherStyle row in WeatherStyleTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "SpawnMachineUnit":
                    {

                        DefaultRow = MachineUnitTypeTable.Instance.DefaultRow == null ? MachineUnitTypeTable.Instance.Rows[0] : MachineUnitTypeTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( MachineUnitType row in MachineUnitTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "SpawnNPCUnitForSelf":
                case "SpawnNPCUnitForLocalAuthority":
                    {

                        DefaultRow = NPCUnitTypeTable.Instance.DefaultRow == null ? NPCUnitTypeTable.Instance.Rows[0] : NPCUnitTypeTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( NPCUnitType row in NPCUnitTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "SpawnMachineVehicle":
                    {
                        DefaultRow = MachineVehicleTypeTable.Instance.DefaultRow == null ? MachineVehicleTypeTable.Instance.Rows[0] : MachineVehicleTypeTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( MachineVehicleType row in MachineVehicleTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "ResourceGrant":
                    {
                        DefaultRow = ResourceTypeTable.Instance.DefaultRow == null ? ResourceTypeTable.Instance.Rows[0] : ResourceTypeTable.Instance.DefaultRow;
                        foreach ( ResourceType row in ResourceTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                                RowsToFill.Add( row );
                        }
                    }
                    return;
                case "InspirationGrant":
                    {
                        DefaultRow = ResearchDomainTable.Instance.DefaultRow == null ? ResearchDomainTable.Instance.Rows[0] : ResearchDomainTable.Instance.DefaultRow;
                        foreach ( ResearchDomain row in ResearchDomainTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                                RowsToFill.Add( row );
                        }
                    }
                    return;
                case "ClickToBuildEmbeddedStructure":
                    {
                        DefaultRow = null;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( MachineStructureType row in MachineStructureTypeTable.Instance.Rows )
                        {
                            if ( !row.IsHidden && row.IsEmbeddedInHumanBuildingOfTag != null && !row.EstablishesNetworkOnBuild )
                            {
                                if ( DefaultRow == null )
                                    DefaultRow = row;
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "ClickToInstallJob":
                    {
                        DefaultRow = MachineJobTable.Instance.DefaultRow == null ? MachineJobTable.Instance.Rows[0] : MachineJobTable.Instance.DefaultRow;
                        bool searchForText = Engine_HotM.CurrentCheatText != null && Engine_HotM.CurrentCheatText.Length > 0;
                        foreach ( MachineJob row in MachineJobTable.Instance.Rows )
                        {
                            if ( !row.IsHidden )
                            {
                                if ( searchForText && !row.GetDisplayName().Contains( Engine_HotM.CurrentCheatText, StringComparison.InvariantCultureIgnoreCase ) )
                                    continue;
                                RowsToFill.Add( row );
                            }
                        }
                    }
                    return;
                case "ClearAllNPCUnits":
                case "DiscoverAllLocations":
                case "UnlockEverything":
                case "CompleteAllActiveProjects":
                case "SwitchToCheatMode":
                case "ToggleFogOfWar":
                case "ToggleDeleteAnyUnit":
                case "ToggleDeleteAllAttackers":
                case "ToggleAllDomains":
                case "ResetAPForAllMyUnits":
                case "RotateSun":
                case "ClickToNuke":
                case "ClickToMicroNuke":
                case "ClickToDestroyBuilding":
                case "ClickToEruptMachineTower":
                case "ClickToFillMissionProgress":
                case "ClickToExpireMission":
                case "IncreaseGeneration":
                case "ReseedEndOfTimeMap":
                case "SpawnRebelWar":
                case "BigStrategy":
                case "NextDoom":
                case "DelayDoom":
                    DefaultRow = null;
                    break;
                default:
                    throw new Exception( "AbstractSimCheats_Basic FillListOfSecondaryDataSource: Not set up for '" + CheatType.ID + "'!" );
            }
        }

        public bool GetShouldBeVisibleAtMoment( CheatType CheatType )
        {
            //switch ( CheatType.ID )
            //{                
            //    default:
            //        return false;
            //}
            return true;

        }

        public bool GetUsesSecondaryDataSources( CheatType CheatType )
        {
            if ( CheatType.SubOptions.Count > 0 )
                return true;

            switch ( CheatType.ID )
            {
                case "GoToNextBuilding":
                case "ChangeWeather":
                case "SpawnMachineUnit":
                case "SpawnMachineVehicle":
                case "SpawnNPCUnitForSelf":
                case "SpawnNPCUnitForLocalAuthority":
                case "ResourceGrant":
                case "InspirationGrant":
                case "ClickToBuildEmbeddedStructure":
                case "ClickToInstallJob":
                    return true;
                case "SwitchToCheatMode":
                case "ClearAllNPCUnits":
                case "DiscoverAllLocations":
                case "UnlockEverything":
                case "CompleteAllActiveProjects":
                case "ToggleFogOfWar":
                case "ToggleDeleteAnyUnit":
                case "ToggleDeleteAllAttackers":
                case "ToggleAllDomains":
                case "ResetAPForAllMyUnits":
                case "RotateSun":
                case "ClickToNuke":
                case "ClickToMicroNuke":
                case "ClickToDestroyBuilding":
                case "ClickToEruptMachineTower":
                case "ClickToFillMissionProgress":
                case "ClickToExpireMission":
                case "IncreaseGeneration":
                case "ReseedEndOfTimeMap":
                case "SpawnRebelWar":
                case "BigStrategy":
                case "NextDoom":
                case "DelayDoom":
                    return false;
                default:
                    throw new Exception( "AbstractSimCheats_Basic GetUsesSecondaryDataSources: Not set up for '" + CheatType.ID + "'!" );
            }
        }

        public bool GetUsesTextbox( CheatType CheatType )
        {
            switch ( CheatType.ID )
            {
                case "GoToNextBuilding":
                case "ChangeWeather":
                case "SpawnMachineUnit":
                case "SpawnMachineVehicle":
                case "SpawnNPCUnitForSelf":
                case "SpawnNPCUnitForLocalAuthority":
                case "ResourceGrant":
                case "InspirationGrant":
                case "ClickToBuildEmbeddedStructure":
                case "ClickToInstallJob":
                case "IncreaseGeneration":
                case "BigStrategy":
                    return true;
                case "SwitchToCheatMode":
                case "ClearAllNPCUnits":
                case "DiscoverAllLocations":
                case "UnlockEverything":
                case "CompleteAllActiveProjects":
                case "ToggleFogOfWar":
                case "ToggleDeleteAnyUnit":
                case "ToggleDeleteAllAttackers":
                case "ToggleAllDomains":
                case "ResetAPForAllMyUnits":
                case "RotateSun":
                case "ClickToNuke":
                case "ClickToMicroNuke":
                case "ClickToDestroyBuilding":
                case "ClickToEruptMachineTower":
                case "ClickToFillMissionProgress":
                case "ClickToExpireMission":
                case "ReseedEndOfTimeMap":
                case "SpawnRebelWar":
                case "NextDoom":
                case "DelayDoom":
                    return false;
                default:
                    throw new Exception( "AbstractSimCheats_Basic GetUsesTextbox: Not set up for '" + CheatType.ID + "'!" );
            }
        }

        public void AppendToDropdownBuffer( CheatType Cheat, ArcenDoubleCharacterBuffer Buffer )
        {
            switch ( Cheat.ID )
            {
                case "ToggleFogOfWar":
                    {
                        if ( SimCommon.IsFogOfWarDisabled )
                            Buffer.Space3x().AddLang( "Disabled", ColorTheme.SkillPaleGreen );
                        else
                            Buffer.Space3x().AddLang( "Enabled", ColorTheme.Gray );
                    }
                    break;
                case "ToggleDeleteAnyUnit":
                    {
                        if ( SimCommon.IsDeleteAnyUnitEnabled )
                            Buffer.Space3x().AddLang( "Enabled", ColorTheme.SkillPaleGreen );
                        else
                            Buffer.Space3x().AddLang( "Disabled", ColorTheme.Gray );
                    }
                    break;
                case "ToggleDeleteAllAttackers":
                    {
                        if ( SimCommon.IsDeleteAllAttackersEnabled )
                            Buffer.Space3x().AddLang( "Enabled", ColorTheme.SkillPaleGreen );
                        else
                            Buffer.Space3x().AddLang( "Disabled", ColorTheme.Gray );
                    }
                    break;
            }
        }

        public bool ExecuteCheat( CheatType Cheat, ICheatSecondaryDataSource Row, string TextboxText )
        {
            switch ( Cheat.ID )
            {
                case "ResourceGrant":
                    {
                        ResourceType resourceType = Row as ResourceType;
                        long resAmount = 0;
                        if ( !long.TryParse( TextboxText, out resAmount ) )
                            resAmount = 1000000;

                        GrantResource( resourceType, resAmount );

                        return false; //don't close the window
                    }
                case "BigStrategy":
                    {
                        long resAmount = 0;
                        if ( !long.TryParse( TextboxText, out resAmount ) )
                            resAmount = 1000;

                        GrantResource( ResourceRefs.Creativity, resAmount );
                        GrantResource( ResourceRefs.Apathy, resAmount );
                        GrantResource( ResourceRefs.Cruelty, resAmount );
                        GrantResource( ResourceRefs.Determination, resAmount );
                        GrantResource( ResourceRefs.Wisdom, resAmount );
                        GrantResource( ResourceRefs.Compassion, resAmount );

                        return false; //don't close the window
                    }
                case "NextDoom":
                    {
                        CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                        int currentTurn = SimCommon.Turn;
                        int reduceBy = 0;
                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;
                            if ( doomEvent.DuringGameplay_WillHappenOnTurn > currentTurn )
                            {
                                reduceBy = doomEvent.DuringGameplay_WillHappenOnTurn - currentTurn;
                                break;
                            }
                        }

                        if ( reduceBy <= 0 )
                        {
                            ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                            return false;
                        }

                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;
                            Interlocked.Add( ref doomEvent.DuringGameplay_WillHappenOnTurn, -reduceBy );
                            if ( doomEvent.DuringGameplay_WillHappenOnTurn < SimCommon.Turn )
                                doomEvent.DuringGameplay_WillHappenOnTurn = SimCommon.Turn;
                        }

                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );

                        return false; //don't close the window
                    }
                case "DelayDoom":
                    {
                        CityTimelineDoomType doomType = SimCommon.GetEffectiveTimelineDoomType();
                        int currentTurn = SimCommon.Turn;
                        foreach ( DoomEvent doomEvent in doomType.DoomMainEvents )
                        {
                            if ( doomEvent.DuringGameplay_HasHappened )
                                continue;
                            if ( doomEvent.DuringGameplay_WillHappenOnTurn > currentTurn )
                            {
                                Interlocked.Add( ref doomEvent.DuringGameplay_WillHappenOnTurn, 50 );

                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                                    NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                                return false; //don't close the window
                            }
                        }

                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                        return false; //don't close the window
                    }
                case "InspirationGrant":
                    {
                        ResearchDomain domainType = Row as ResearchDomain;
                        int inspirationAmount = 0;
                        if ( !int.TryParse( TextboxText, out inspirationAmount ) )
                            inspirationAmount = 3;


                        if ( domainType != null )
                        {
                            domainType.AddMoreInspiration( inspirationAmount );

                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "InspirationCheat" ),
                                NoteStyle.BothGame, domainType.ID, inspirationAmount, 0, 0, 0 );
                        }

                        return false; //don't close the window
                    }
                case "ReseedEndOfTimeMap":
                    {
                        if ( !EndOfTimeMap.GetCanReseedMap() )
                        {
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "EndOfTimeMapCannotBeReseeded" ), NoteStyle.ShowInPassing, 3f );
                            return false;
                        }

                        EndOfTimeMap.TryReseedMap();
                        return false;
                    }
                //case "GoToNextBuilding":
                //    {
                //        BuildingType buildingType = Row as BuildingType;
                //        if ( buildingType != null )
                //        {
                //            List<SimBuilding> buildingList = World_Buildings.BuildingsByType.GetDisplayDict().GetListForOrNull( buildingType );
                //            if ( buildingList != null && buildingList.Count > 0 )
                //            {
                //                if ( buildingType.Cheat_WorkingNextIndex >= buildingList.Count )
                //                    buildingType.Cheat_WorkingNextIndex = 0;

                //                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( buildingList[buildingType.Cheat_WorkingNextIndex].Item.OBBCache.Center, false );

                //                ArcenNotes.SendNoteToGameOnly( LocalizedString.AddFormat3_New( "JumpedToBuilding", buildingType.Cheat_WorkingNextIndex, 
                //                    buildingList.Count, buildingType.GetDisplayName() ), NoteStyle.ShowInPassing, 3f );

                //                buildingType.Cheat_WorkingNextIndex++;
                //            }
                //            else
                //                ArcenNotes.SendNoteToGameOnly( LocalizedString.AddFormat1_New( "CouldNotFindBuilding", buildingType.GetDisplayName() ), 
                //                    NoteStyle.ShowInPassing, 3f );
                //        }
                //        else
                //            ArcenNotes.SendNoteToGameOnly( LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                //        return false; //never close the window
                //    }
                case "ChangeWeather":
                    {
                        WeatherStyle weatherStyle = Row as WeatherStyle;
                        if ( weatherStyle != null )
                            SimCommon.SetWeather( weatherStyle, true );
                        else
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                        return false; //never close the window
                    }
                case "RotateSun":
                    VisCurrent.CurrentSunAngle += 10;
                    if ( VisCurrent.CurrentSunAngle > 360 )
                        VisCurrent.CurrentSunAngle -= 360;
                    return false;
                case "ClickToNuke":
                case "ClickToMicroNuke":
                case "ClickToDestroyBuilding":
                case "ClickToEruptMachineTower":
                case "ClickToBuildEmbeddedStructure":
                case "ClickToInstallJob":
                case "ClickToFillMissionProgress":
                case "ClickToExpireMission":
                    if ( Engine_HotM.CurrentCheatOverridingClickMode == Cheat )
                        Engine_HotM.CurrentCheatOverridingClickMode = null;
                    else
                        Engine_HotM.CurrentCheatOverridingClickMode = Cheat;
                    return false;
                case "SpawnMachineUnit":
                    {
                        MachineUnitType unitType = Row as MachineUnitType;
                        MapCell cell = CameraCurrent.GetMostRelevantCell();
                        if ( unitType != null && cell != null )
                        {
                            ISimMachineUnit unit = World_Forces.TryCreateMachineUnitAtRandomMapSeedSpotOnCell( cell, unitType, Engine_Universal.PermanentQualityRandom, string.Empty, true, CollisionRule.Relaxed );
                            if ( unit != null )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatMachineUnit" ), NoteStyle.BothGame,
                                    unitType.ID, string.Empty, string.Empty, string.Empty, unit.ActorID, 0, 0, unit.GetDisplayName(), string.Empty, string.Empty, 0 );
                            else
                                ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddFormat1_New( "CouldNotFindSpaceToCreate", unitType.GetDisplayName() ),
                                    NoteStyle.ShowInPassing, 3f );
                        }
                        else
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                        return false; //never close the window
                    }
                case "SpawnMachineVehicle":
                    {
                        MachineVehicleType vehicleType = Row as MachineVehicleType;
                        MapCell cell = CameraCurrent.GetMostRelevantCell();
                        if ( vehicleType != null && cell != null )
                        {
                            ISimMachineVehicle vehicle = World_Forces.TryCreateMachineVehicleAtRandomMapSeedSpotOnCell( cell, vehicleType, Engine_Universal.PermanentQualityRandom, 
                                string.Empty, true, CollisionRule.Relaxed, true );
                            if ( vehicle != null )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatMachineVehicle" ), NoteStyle.BothGame,
                                    vehicleType.ID, string.Empty, string.Empty, string.Empty, vehicle.ActorID, 0, 0, vehicle.GetDisplayName(), string.Empty, string.Empty, 0 );
                            else
                                ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddFormat1_New( "CouldNotFindSpaceToCreate", vehicleType.GetDisplayName() ),
                                    NoteStyle.ShowInPassing, 3f );
                        }
                        else
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                        return false; //never close the window
                    }
                case "SpawnNPCUnitForSelf":
                    {
                        NPCUnitType unitType = Row as NPCUnitType;
                        MapCell cell = CameraCurrent.GetMostRelevantCell();
                        if ( unitType != null && cell != null )
                        {
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByID( "Yourself" );
                            if ( cohort != null )
                            {
                                ISimNPCUnit unit = World_Forces.TryCreateNPCUnitAtRandomMapSeedSpotOnCell( cell, unitType,
                                    cohort, CommonRefs.Player_DeterAndDefend, -1f,
                                    Vector3.negativeInfinity, -1, false,
                                    Engine_Universal.PermanentQualityRandom, CollisionRule.Relaxed, "CheatCode" );
                                if ( unit != null )
                                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatNPCUnit" ), NoteStyle.BothGame,
                                        unitType.ID, unit.FromCohort.ID, string.Empty, string.Empty, unit.ActorID, 0, 0, string.Empty, string.Empty, string.Empty, 0 );
                                else
                                    ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddFormat1_New( "CouldNotFindSpaceToCreate", unitType.GetDisplayName() ),
                                        NoteStyle.ShowInPassing, 3f );
                            }
                        }
                        else
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                        return false; //never close the window
                    }
                case "SpawnNPCUnitForLocalAuthority":
                    {
                        NPCUnitType npcUnitType = Row as NPCUnitType;
                        MapCell cell = CameraCurrent.GetMostRelevantCell();
                        NPCCohort localAuthority = cell?.ParentTile?.District?.ControlledBy;
                        if ( npcUnitType != null && cell != null && localAuthority != null )
                        {
                            NPCUnitStance stance = CommonRefs.DistrictHumanGuard;
                            if ( npcUnitType.IsMechStyleMovement )
                                stance = CommonRefs.DistrictMechGuard;
                            else if ( npcUnitType.IsVehicle )
                                stance = CommonRefs.DistrictVehicleGuard;

                            ISimNPCUnit unit = null;
                            if ( Engine_HotM.SelectedActor is MachineStructure structure )
                            {
                                unit = World.Forces.TryCreateNewNPCUnitAsCloseAsPossibleToLocation( structure.GetPositionForCollisions(), structure.CalculateMapCell(), npcUnitType,
                                    localAuthority, stance, -1f,
                                    structure.GetPositionForCollisions(), -1, true, 2, CellRange.CellAndAdjacent2x,
                                    Engine_Universal.PermanentQualityRandom, CollisionRule.Relaxed, "CheatCode" );
                            }
                            else
                            {
                                unit = World_Forces.TryCreateNPCUnitAtRandomMapSeedSpotOnCell( cell, npcUnitType,
                                    localAuthority, stance, -1f,
                                    Vector3.negativeInfinity, -1, false,
                                    Engine_Universal.PermanentQualityRandom, CollisionRule.Relaxed, "CheatCode" );
                            }    

                            if ( unit != null )
                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatNPCUnit" ), NoteStyle.BothGame,
                                    npcUnitType.ID, unit.FromCohort.ID, string.Empty, string.Empty, unit.ActorID, 0, 0, 
                                    string.Empty, string.Empty, string.Empty, 0 );
                            else
                                ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddFormat1_New( "CouldNotFindSpaceToCreate", npcUnitType.GetDisplayName() ),
                                    NoteStyle.ShowInPassing, 3f );
                        }
                        else
                            ArcenNotes.SendNoteToGameOnly( 100, LocalizedString.AddLang_New( "InvalidCommandNoData" ), NoteStyle.ShowInPassing, 3f );
                        return false; //never close the window
                    }
                case "SpawnRebelWar":
                    {
                        NPCCohort rebelCohort = NPCCohortTagTable.Instance.GetRowByID( "StandardRebels" ).CohortsList.GetRandom( Engine_Universal.PermanentQualityRandom );
                        NPCCohort corporateCohort = NPCCohortTagTable.Instance.GetRowByID( "CorporationPropertyOwner" ).CohortsList.GetRandom( Engine_Universal.PermanentQualityRandom );

                        NPCUnitTag rebelUnitTag = NPCUnitTagTable.Instance.GetRowByID( "RebelMidleveReinforcements" );
                        NPCUnitTag corporateUnitTag = NPCUnitTagTable.Instance.GetRowByID( "CorporateVariedWarrior" );

                        NPCUnitTag mechUnitTag = NPCUnitTagTable.Instance.GetRowByID( "Mk1MechsByHumans" );

                        NPCUnitStance rebelStance = NPCUnitStanceTable.Instance.GetRowByID( "RebelAntiCorpOp_AutoBattle" );
                        NPCUnitStance corporateStance = NPCUnitStanceTable.Instance.GetRowByID( "MilitaryShowOfForce_AutoBattle" );

                        int rebelsSpawned = 0;
                        int corporatesSpawned = 0;

                        int rebelMechsSpawned = 0;
                        int corporateMechsSpawned = 0;

                        float radiusPerCell = CityMap.CELL_FULL_SIZE * 100;

                        MersenneTwister rand = new MersenneTwister( Engine_Universal.PermanentQualityRandom.Next() );

                        ArcenThreading.RunTaskOnBackgroundThread( "_Inter.DoCheatLogic",
                        ( TaskStartData startData ) =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();
                            foreach ( MapCell cell in CityMap.Cells )
                            {
                                if ( cell.BuildingList.Count == 0 )
                                    continue;

                                Vector3 cellCenter = cell.Center;

                                //spawn rebel
                                if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, rebelUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                    rebelCohort, rebelStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                    rebelsSpawned++;

                                //spawn rebel
                                if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, rebelUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                    rebelCohort, rebelStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                    rebelsSpawned++;

                                //spawn corporate
                                if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, corporateUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                    corporateCohort, corporateStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                    corporatesSpawned++;

                                //spawn corporate
                                if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, corporateUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                    corporateCohort, corporateStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                    corporatesSpawned++;

                                //20% chance of mechs, and not all of them will even fit
                                if ( rand.Next( 0, 100 ) < 20 )
                                {
                                    //spawn rebel mech
                                    if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, mechUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                        rebelCohort, rebelStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                        rebelMechsSpawned++;

                                    //spawn corporate mech
                                    if ( World.Forces.TryCreateNewNPCUnitWithinThisRadius( cellCenter, cell, mechUnitTag.NPCUnitTypesList.GetRandom( rand ),
                                        corporateCohort, corporateStance, 1f, Vector3.zero, -1, true, radiusPerCell, 5, CellRange.CellAndAdjacent, rand, CollisionRule.Relaxed, "WarCheat" ) != null )
                                        corporateMechsSpawned++;
                                }
                            }

                            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatRebelWar" ), NoteStyle.BothGame,
                                rebelMechsSpawned.ToString(), corporateMechsSpawned.ToString(), string.Empty, string.Empty, rebelsSpawned, corporatesSpawned, Mathf.CeilToInt( (float)sw.Elapsed.TotalSeconds ),
                                string.Empty, string.Empty, string.Empty, 0 );
                        } );
                    }
                    return true; //close after doing
                case "ClearAllNPCUnits":
                    {
                        foreach ( KeyValuePair<int, NPCUnit> kv in World_Forces.AllNPCUnitsByID )
                        {
                            kv.Value.DisbandNPCUnit( NPCDisbandReason.Cheat );
                        }

                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                    }
                    return true;
                case "DiscoverAllLocations":
                    {
                        foreach ( KeyValuePair<Int16, MapPOI> kv in CityMap.POIsByID )
                        {
                            kv.Value.HasBeenDiscovered = true;
                            if ( kv.Value.ControlledBy != null )
                                kv.Value.ControlledBy.DuringGame_DiscoverIfNeed();
                        }
                        foreach ( KeyValuePair<short, MapDistrict> kv in CityMap.DistrictsByID )
                        {
                            kv.Value.HasBeenDiscovered = true;
                            if ( kv.Value.ControlledBy != null )
                                kv.Value.ControlledBy.DuringGame_DiscoverIfNeed();
                        }

                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                    }
                    return true;
                case "UnlockEverything":
                    {
                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );

                        UnlockInspirationType inspiration = UnlockInspirationTypeTable.Instance.GetRowByID( "CheatCode" );

                        foreach ( Unlock unlock in UnlockTable.Instance.Rows )
                            unlock.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( inspiration, false, false, false, false );
                    }
                    return true;
                case "CompleteAllActiveProjects":
                    {
                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );

                        foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
                        {
                            if ( project.DuringGame_IntendedOutcome != null && project.DuringGame_ActualOutcome == null )
                                project.DoOnProjectWin( project.DuringGame_IntendedOutcome, Engine_Universal.PermanentQualityRandom, true, true );
                        }
                    }
                    return true;
                case "SwitchToCheatMode":
                    SimCommon.IsCheatTimeline = true;
                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                        NoteStyle.BothGame, Cheat.ID, SimCommon.CurrentTimeline.SecondsInTimeline, SimCommon.CurrentTimeline.Turn, DateTime.Now.Ticks, 0 );
                    return true; //yes close the window
                case "ToggleFogOfWar":
                    SimCommon.IsFogOfWarDisabled = !SimCommon.IsFogOfWarDisabled;
                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                        NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                    return false; //do not close the window
                case "ToggleDeleteAnyUnit":
                    SimCommon.IsDeleteAnyUnitEnabled = !SimCommon.IsDeleteAnyUnitEnabled;
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                        NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                    return false; //do not close the window
                case "ToggleDeleteAllAttackers":
                    SimCommon.IsDeleteAllAttackersEnabled = !SimCommon.IsDeleteAllAttackersEnabled;
                    ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                        NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                    return false; //do not close the window
                case "ResetAPForAllMyUnits":
                    {
                        foreach ( KeyValuePair<int, MachineUnit> kv in World_Forces.MachineUnitsByID )
                            kv.Value.SetStartingActionPoints( true );
                        foreach ( KeyValuePair<int, MachineVehicle> kv in World_Forces.MachineVehiclesByID )
                            kv.Value.SetStartingActionPoints( true );

                        ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "CheatByName" ),
                            NoteStyle.BothGame, Cheat.ID, 0, 0, 0, 0 );
                        return false; //do not close the window
                    }
                default:
                    throw new Exception( "AbstractSimCheats_Basic ExecuteCheat: Not set up for '" + Cheat.ID + "'!" );
            }
        }

        public void ExecuteCheatClick( CheatType Cheat, ICheatSecondaryDataSource Row, string TextboxText, ISimBuilding ClickedBuilding, CheatClickType ClickType )
        {
            switch ( Cheat.ID )
            {
                case "ClickToNuke":
                    {
                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }
                        if ( ClickType == CheatClickType.Right )
                            return;

                        if ( ClickedBuilding == null || ClickType != CheatClickType.Left )
                            return;
                        Vector3 epicenter = ClickedBuilding.GetMapItem().OBBCache.BottomCenter;
                        ParticleSoundRefs.FullNuke.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );
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
                        destructionData.SkipUnitsWithArmorPlatingAbove = 200;
                        destructionData.SkipUnitsAboveHeight = 0;
                        destructionData.IrradiateCells = true;
                        destructionData.UnitsToSpawnAfter = null;
                        destructionData.StatisticForDeaths = CityStatisticRefs.MurdersByNuke;
                        destructionData.IsCausedByPlayer = true;
                        destructionData.IsFromJob = null;
                        destructionData.ExtraCode = CommonRefs.PlayerNukeExplosionExtraCode;

                        SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );
                    }
                    break;
                case "ClickToMicroNuke":
                    {
                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }
                        if ( ClickType == CheatClickType.Right )
                            return;

                        if ( ClickedBuilding == null || ClickType != CheatClickType.Left )
                            return;
                        Vector3 epicenter = ClickedBuilding.GetMapItem().OBBCache.BottomCenter;
                        ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                        QueuedBuildingDestructionData destructionData;
                        destructionData.Epicenter = epicenter;
                        destructionData.Range = MathRefs.MicroNukeRadius.FloatMin;
                        destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                        destructionData.AlsoDestroyOtherItems = true;
                        destructionData.AlsoDestroyUnits = true;
                        destructionData.DestroyAllPlayerUnits = true;
                        destructionData.SkipUnitsWithArmorPlatingAbove = 200;
                        destructionData.SkipUnitsAboveHeight = 0;
                        destructionData.IrradiateCells = true;
                        destructionData.UnitsToSpawnAfter = null;
                        destructionData.StatisticForDeaths = CityStatisticRefs.MurdersByNuke;
                        destructionData.IsCausedByPlayer = true;
                        destructionData.IsFromJob = null;
                        destructionData.ExtraCode = CommonRefs.PlayerNukeExplosionExtraCode;

                        SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );
                    }
                    break;
                case "ClickToFillMissionProgress":
                    {
                        NPCMission mission = CursorHelper.FindNPCMissionUnderCursor();

                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( mission != null && mission.ProgressPointsNeededToComplete > 0 )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }

                        if ( mission == null || mission.ProgressPointsNeededToComplete <= 0 || ClickType != CheatClickType.Left )
                            return;
                        if ( ClickType == CheatClickType.Right )
                            return;

                        mission.AlterProgress( mission.ProgressPointsNeededToComplete );
                    }
                    break;
                case "ClickToExpireMission":
                    {
                        NPCMission mission = CursorHelper.FindNPCMissionUnderCursor();

                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( mission != null )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }

                        if ( mission == null || ClickType != CheatClickType.Left )
                            return;
                        if ( ClickType == CheatClickType.Right )
                            return;

                        mission.DoOnMissionComplete(); //will expire it
                    }
                    break;
                case "ClickToDestroyBuilding":
                    {
                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null && !(ClickedBuilding.GetStatus()?.ShouldBuildingBeNonfunctional??false) )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }

                        if ( ClickedBuilding == null || (ClickedBuilding.GetStatus()?.ShouldBuildingBeNonfunctional ?? false) )
                            return;
                        if ( ClickType == CheatClickType.Right )
                            return;

                        //if there's a structure in this building, make sure it gets destroyed also
                        if ( ClickedBuilding.MachineStructureInBuilding != null )
                            ClickedBuilding.MachineStructureInBuilding.ScrapStructureNow( ScrapReason.CaughtInExplosion, Engine_Universal.PermanentQualityRandom );

                        ClickedBuilding.SetStatus( CommonRefs.DemolishedBuildingStatus );
                        ClickedBuilding.GetMapItem().DropBurningEffect_Slow();

                    }
                    break;
                case "ClickToEruptMachineTower":
                    {
                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null && ClickedBuilding.CalculateIsValidTargetForMachineStructureRightNow( CommonRefs.NetworkTowerStructure, null ) )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }

                        if ( ClickedBuilding == null || !ClickedBuilding.CalculateIsValidTargetForMachineStructureRightNow( CommonRefs.NetworkTowerStructure, null ) )
                            return;
                        if ( ClickType == CheatClickType.Right )
                            return;

                        ClickedBuilding.GetMapItem().DropBurningEffect();
                        ClickedBuilding.SetPrefab( CommonRefs.MachineTowers.DrawRandomItem( Engine_Universal.PermanentQualityRandom ).Building, 32f,
                            ParticleSoundRefs.MachineTowerEruptionStart, ParticleSoundRefs.MachineTowerEruptionEnd );
                    }
                    break;
                case "ClickToBuildEmbeddedStructure":
                    {
                        MachineStructureType structureType = Row as MachineStructureType;
                        if ( structureType == null || structureType.IsEmbeddedInHumanBuildingOfTag == null )
                            return;

                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }
                        if ( ClickType == CheatClickType.Right )
                        {
                            if ( ClickedBuilding?.MachineStructureInBuilding != null && ClickedBuilding.MachineStructureInBuilding.Type.IsEmbeddedInHumanBuildingOfTag != null )
                                ClickedBuilding.MachineStructureInBuilding.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                            return;
                        }

                        if ( ClickedBuilding == null || ClickedBuilding.MachineStructureInBuilding != null )
                            return;

                        MachineStructure.Create( structureType, ClickedBuilding );
                        SimCommon.NeedsVisibilityGranterRecalculation = true;
                        SimCommon.NeedsNetworkRecalculation = true;
                    }
                    break;
                case "ClickToInstallJob":
                    {
                        MachineJob job = Row as MachineJob;
                        if ( job == null )
                            return;

                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null && ClickedBuilding.MachineStructureInBuilding != null && 
                                job.GetIsValidForThisJobToGoAtThatStructure( ClickedBuilding.MachineStructureInBuilding ) )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }
                        if ( ClickType == CheatClickType.Right )
                            return;

                        if ( ClickedBuilding == null || ClickedBuilding.MachineStructureInBuilding == null || !job.GetIsValidForThisJobToGoAtThatStructure( ClickedBuilding.MachineStructureInBuilding ) )
                            return;

                        ClickedBuilding.MachineStructureInBuilding.AssignJobInstantly( job );
                    }
                    break;
                default:
                    break; //normal for this to do nothing
            }
        }

        #region GrantResource
        private static void GrantResource( ResourceType resourceType, Int64 resAmount )
        {
            if ( resourceType != null )
            {
                long amountAvailableToCap = 10000000000000000;
                if ( resourceType.MidSoftCap >= 0 )
                    amountAvailableToCap = resourceType.MidSoftCap - resourceType.Current;

                if ( resAmount > amountAvailableToCap )
                    resAmount = amountAvailableToCap;

                long newTotal = resourceType.AlterCurrent_Named( resAmount, "Income_Cheat", ResourceAddRule.IgnoreUntilTurnChange );
                if ( resourceType.MidSoftCap >= 0 && newTotal > resourceType.MidSoftCap )
                {
                    Int64 toSubtract = newTotal - resourceType.MidSoftCap;
                    resourceType.AlterCurrent_Named( -toSubtract, "Decrease_HadMoreThanCap", ResourceAddRule.IgnoreUntilTurnChange );
                }

                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "ResourceCheat" ),
                    NoteStyle.BothGame, resourceType.ID, resAmount, 0, 0, 0 );
            }
        }
        #endregion
    }
}

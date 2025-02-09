using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;
using System.IO;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public class Input_MainUIHandler : BaseInputHandler
    {
        private static readonly List<A5Placeable> workingObjects = List<A5Placeable>.Create_WillNeverBeGCed( 100, "Input_MainUIHandler-workingObjects", 50 );

        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( ArcenInput.IsInputBlockedForAmountOfTime )
                return;

            bool areAllInputsBlocked = false;
            if ( Engine_Universal.GameLoop.IsLevelEditor )
            {
                if ( (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorInputDisabledByModalPopup )
                    areAllInputsBlocked = true;
			}

			string InputActionID = InputActionType.ID;

			if (Engine_Universal.IsAnyTextboxFocused && InputActionID != "Cancel")
				return;

            switch ( InputActionID )
            {
                case "Cancel": //this mostly handles the normal escape handling now
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !Window_LevelEditorSystemMenu.Instance.IsOpen )
                        {
                            Window_LevelEditorSystemMenu.Instance.Open();
                            ArcenInput.BlockForAJustPartOfOneSecond();
                            return;
                        }
                    }
                    else
                    {
                        if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                            return;

                        //if we got here, there was nothing on the stack to close
                        if ( VisCurrent.IsInPhotoMode )
                        {
                            InputCaching.ExitPhotoModeNext = true;
                            return;
                        }

                        if ( VisCurrent.IsShowingActualEvent ) //this is an unusual case
                        {
                            //toggle the sidebar; don't do any of the lower logic
                            VisCommands.ToggleSystemMenu();
                            return;
                        }

                        LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                        if ( lowerMode != null )
                        {
                            if ( lowerMode.DoesNotCloseFromHotkeys )
                                return;
                            if ( lowerMode.ClosesLikeAWindow )
                            {
                                Engine_HotM.CurrentLowerMode = null;
                                return;
                            }
                        }
                        else
                        {
                            if ( Engine_HotM.SelectedMachineActionMode != null )
                            {
                                if ( Engine_HotM.SelectedMachineActionMode?.Implementation?.HandleCancelButton()??false )
                                    return;
                                Engine_HotM.SelectedMachineActionMode = null;
                                return;
                            }

                            if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor )
                            {
                                if ( (machineActor.IsInAbilityTypeTargetingMode != null &&
                                    !machineActor.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !machineActor.IsInAbilityTypeTargetingMode.IsMixedTargetingMode) )
                                {
                                    machineActor.SetTargetingMode( null, null );
                                    return;
                                }
                                if ( machineActor.IsInConsumableTargetingMode != null )
                                {
                                    machineActor.SetTargetingMode( null, null );
                                    return;
                                }
                            }
                        }

                        ////deselect current selected actor
                        //if ( VisCommands.TryDeselectCurrentActorOrOtherwiseStepBackOne() )
                        //    return;

                        //toggle the system menu
                        VisCommands.ToggleSystemMenu();
                        ArcenInput.BlockForAJustPartOfOneSecond();
                    }
                    break;
                case "ToggleVisualPause":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( Time.timeScale == 0 )
                        {
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                        }
                        else
                            Time.timeScale = 0;
                    }
                    break;
                case "DecreaseVisualSpeed":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        SimCommon.DesiredTimeScaleIndexNotPaused++;
                        if ( SimCommon.DesiredTimeScaleIndexNotPaused > SimCommon.TimeScaleIndexes.Length - 1 )
                            SimCommon.DesiredTimeScaleIndexNotPaused = SimCommon.TimeScaleIndexes.Length - 1;
                        if ( Time.timeScale > 0 )
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                    }
                    break;
                case "IncreaseVisualSpeed":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        SimCommon.DesiredTimeScaleIndexNotPaused--;
                        if ( SimCommon.DesiredTimeScaleIndexNotPaused < 0 )
                            SimCommon.DesiredTimeScaleIndexNotPaused = 0;
                        if ( Time.timeScale > 0 )
                            Time.timeScale = SimCommon.TimeScaleIndexes[SimCommon.DesiredTimeScaleIndexNotPaused];
                    }
                    break;
                case "ToggleUI":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        VisCurrent.IsUIHiddenExceptForSidebar = !VisCurrent.IsUIHiddenExceptForSidebar;
                    }
                    break;
                case "ToggleWorldIcons":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        VisCurrent.AreAllGameIconsHidden = !VisCurrent.AreAllGameIconsHidden;
                    }
                    break;
                case "SkipToNextMusicTrack":
                    if ( ArcenMusicPlayer.Instance != null )
                    {
                        ArcenMusicPlayer.Instance.CurrentOneShotOverridingMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.CurrentSecondaryMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.CurrentPrimaryMusicTrackPlaying = null;
                        ArcenMusicPlayer.Instance.RemainingTimeAfterCurrentPrimaryTrackPlays = 0f;
                    }
                    break;
                case "Tab":
                case "ShiftTab":
                    {
                        //nothing done here for now
                    }
                    break;
                case "ToggleMapMode":
                    if ( areAllInputsBlocked )
                        return;
                    if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                        return;
                    VisCommands.ToggleCityMapMode();
                    break;
                case "ToggleTheZodiacMode":
                    if ( areAllInputsBlocked )
                        return;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    VisCommands.ToggleTheVirtualWorld();
                    break;
                case "ToggleEndOfTimeMode":
                    if ( areAllInputsBlocked )
                        return;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    VisCommands.ToggleEndOfTimeMode();
                    break;
                case "ToggleForcesSidebar":
                    if ( areAllInputsBlocked )
                        return;
                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.Forces );
                    break;
                case "ToggleCultActionsSidebar":
                    if ( areAllInputsBlocked )
                        return;
                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.CultActions );
                    break;
                case "ToggleNetworksSidebar":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                        return;
                    VisCommands.ToggleNetworksSidebar();
                    break;
                case "ToggleDeepCheatSidebar":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped ) //the item right before the right header
                        return;
                    VisCommands.ToggleDeepCheatSidebar();
                    break;
                case "ToggleTimelinesSidebar":
                    if ( areAllInputsBlocked )
                        return;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    VisCommands.ToggleTimelinesSidebar();
                    break;
                case "ToggleBuildMode":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    if ( FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                        VisCommands.ToggleBuildMode();
                    else
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    break;
                case "ToggleCommandMode":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    if ( FlagRefs.HasFiguredOutCommandMode.DuringGameplay_IsTripped )
                        VisCommands.ToggleCommandMode();
                    else
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    break;
                case "ToggleHistory":
                    if ( areAllInputsBlocked )
                        return;
                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.History );
                    break;
                case "ToggleVictoryPath":
                    if ( areAllInputsBlocked )
                        return;
                    VisCommands.ToggleMajorWindowMode( MajorWindowMode.VictoryPath );
                    break;
                case "GoStraightToNextTurn":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                        break;
                    if ( !FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped )
                        return;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    VisCommands.HandleGoToNextTurnOrActor( true );
                    break;
                case "NextNetworkTower":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.GetIsNetworkTower() );
                    break;
                case "NextStructureWithAnyComplaint":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.GetHasAnyComplaint() );
                    break;
                case "NextStructureWithMinorComplaint":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.GetHasAnyComplaint() && !s.GetHasVisibleComplaint() );
                    break;
                case "NextStructureWithAnyIssue":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructuresWithAnyIssue( false, ( MachineStructure s ) => true );
                    break;
                case "NextStructureWithMajorComplaint":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.GetHasVisibleComplaint() );
                    break;
                case "NextStructure":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => true );
                    break;
                case "NextDamagedStructure":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.GetIsDamaged() );
                    break;
                case "NextUnderConstructionStructure":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.IsUnderConstruction || s.IsJobStillInstalling );
                    break;
                case "NextStructureWithoutJob":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.CurrentJob == null && s.Type.DuringGame_CurrentlyAvailableJobs.Count > 0 );
                    break;
                case "NextActiveNPCCombatant":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughNPCUnits( false, ( ISimNPCUnit u ) => !u.UnitType.DeathsCountAsMurders && !(u.Stance?.IsConsideredBasicGuard??false) );
                    break;
                case "NextNPCNoncombatant":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughNPCUnits( false, ( ISimNPCUnit u ) => u.UnitType.DeathsCountAsMurders && !(u.Stance?.IsConsideredBasicGuard ?? false) );
                    break;
                case "NextSubnet":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughSubnets( ( MachineSubnet s ) => true );
                    break;
                case "NextMachineActor":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || Engine_HotM.CurrentLowerMode != null )
                        break;
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    SimCommon.CycleThroughMachineActors( true, ( ISimMachineActor a ) => true );
                    break;
                case "JumpToNextActorOrEndTurn":
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                            return;
                        if ( Window_LoadingWindow.Instance.GetShouldDrawThisFrame() ) 
                            return;

                        ArcenInput.BlockUntilNextFrame();

                        if ( !Window_RadialMenu.Instance?.GetShouldDrawThisFrame() ?? false )
                        {
                            //EscapeWindowStackController.HandleCancel( WindowCloseReason.UserCasualRequest ); //do this instead (this did not work)
                            break; //blocked from normal function!
                        }
                        VisCommands.HandleGoToNextTurnOrActor( false );
                    }
                    break;
                case "PreviousLens":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.GoToPreviousLens();
                    }
                    break;
                case "NextLens":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.GoToNextLens();
                    }
                    break;
                case "Lens_Versatile":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.VersatileLens );
                    }
                    break;
                case "Lens_StreetSense":
                    {
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.StreetSenseLens );
                    }
                    break;
                case "Lens_Investigations":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.InvestigationsLens );
                    }
                    break;
                case "Lens_Contemplations":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ContemplationsLens );
                    }
                    break;
                case "Lens_CityConflicts":
                    {
                        if ( !FlagRefs.TheStrategist.DuringGameplay_IsInvented ) //must have unlocked this
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.CityConflictLens );
                    }
                    break;
                case "Lens_Navigation":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.NavigationLens );
                    }
                    break;
                case "Lens_Forces":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ForcesLens );
                    }
                    break;
                case "Lens_MachineStructures":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.MachineStructuresLens );
                    }
                    break;
                case "Lens_ScavengingSites":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ScavengingSitesLens );
                    }
                    break;
                case "Lens_ExplorationSites":
                    {
                        if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                            return;
                        ArcenInput.BlockUntilNextFrame();
                        SimCommon.SetCurrentCityLensIfAvailable( CommonRefs.ExplorationSitesLens );
                    }
                    break;
                case "RotateStructureLeft":
                case "RotateStructureRight":
                case "RotateBulkUnitLeft":
                case "RotateBulkUnitRight":
                    if ( Engine_HotM.SelectedMachineActionMode != null )
                        Engine_HotM.SelectedMachineActionMode.Implementation.HandlePassedInput( Int1, InputActionType );
                    break;
                case "LevelEditor_SwitchPaletteAndSceneObjectList":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        Window_LevelEditorPalette.IsShowingSceneObjectListInstead = !Window_LevelEditorPalette.IsShowingSceneObjectListInstead;
                    break;
                case "LevelEditor_Save":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        Window_LevelEditorHeaderBar.bSave.SaveOrSaveAs( false );
                    break;
                case "LevelEditor_SaveAs":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        Window_LevelEditorHeaderBar.bSave.SaveOrSaveAs( true );
                    break;
                case "LevelEditor_ToggleGrid":
                    if ( areAllInputsBlocked )
                        return;
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorGridOn =
                            !(Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.IsLevelEditorGridOn;
                    break;
                case "Debug_ReloadSelectXmlData":
                    {
                        Engine_HotM.Instance.ReloadSelectDataFromXml();
                        ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddLang_New( "Debug_ReloadedSelectXml" ), NoteStyle.ShowInPassing, 3f );
                    }
                    break;
                case "Debug_CycleAllBuildingMarkerVisibleStyles":
                    {
                        Engine_HotM.OverrideShowAllBuildingMarkers = !Engine_HotM.OverrideShowAllBuildingMarkers;
                        //ArcenDebugging.LogSingleLine( "Marker To Show Draw: " + Engine_HotM.OverridingShowAllBuildingMarkers + " out of " +
                        //    BuildingPrefabTable.MaxBuildingMarkerCountOnAnyBuilding, Verbosity.DoNotShow );
                    }
                    break;
                case "Debug_DumpAllData":
                    {
                        string dumpFolder = Engine_Universal.CurrentPlayerDataDirectory + "Dumps/";
                        if ( !ArcenIO.DirectoryExists( dumpFolder ) )
                            ArcenIO.CreateDirectory( dumpFolder );

                        string dumpFile = dumpFolder + "Dump-" + DateTime.Now.Year + "-" + DateTime.Now.Month +
                            "-" + DateTime.Now.Day + "--" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt";
                        using ( FileStream stream = new FileStream( dumpFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite ) )
                        {
                            using ( StreamWriter writer = new StreamWriter( stream ) )
                                ArcenExternalTypeManager.DumpAllData( writer );
                        }
                        ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddLang_New( "Debug_DumpedAllData" )
                            .AddLang( "AfterLineItemHeader" ).AddRaw( dumpFile ), NoteStyle.ShowInPassing, 3f );
                    }
                    break;
                case "Debug_AbortAllThreads":
                    {
                        ArcenThreading.AbortAllThreads( false );
                        ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddLang_New( "Debug_AbortedAllThreads" ), NoteStyle.ShowInPassing, 3f );
                    }
                    break;
                case "Debug_DumpFrameData":
                    {
                        VisCommands.DumpFrameData();
                        ArcenNotes.SendNoteToGameOnly( 50, LocalizedString.AddLang_New( "Debug_DumpedFrameData" ), NoteStyle.ShowInPassing, 3f );
                    }
                    break;
                case "LevelEditor_SelectAll":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.SelectAll();
                    break;
                case "LevelEditor_Copy":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.Copy();
                    break;
                case "LevelEditor_Paste":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.Paste();
                    break;
                case "LevelEditor_HideMetaLayers":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AreMetaLayersVisible =
                            !(Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AreMetaLayersVisible;
                    break;
                case "LevelEditor_HideDecorationMetaLayers":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AreDecorationMetaLayersVisible =
                            !(Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AreDecorationMetaLayersVisible;
                    break;
                case "LevelEditor_HidePathingRegionsAndPoints":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.ArePathingLayersAndPointsVisible =
                            !(Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.ArePathingLayersAndPointsVisible;
                    break;
                case "LevelEditor_ConnectPathingPoints":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        #region LevelEditor_ConnectPathingPoints
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.FillListOfSelectedObjects( workingObjects );
                        if ( workingObjects.Count <= 0 )
                            break; //nothing to do!

                        //make a local list, so that can be used in the undo action.  It will stay in memory until the undo action goes out of scope.
                        ThrowawayList<RefPair<A5Placeable, A5Placeable>> localDataList = new ThrowawayList<RefPair<A5Placeable, A5Placeable>>();
                        foreach ( A5Placeable placeable in workingObjects )
                        {
                            if ( !placeable || !placeable.AdvancedIsPathingPoint )
                                continue;

                            foreach ( A5Placeable other in workingObjects )
                            {
                                if ( !other || other == placeable || !other.AdvancedIsPathingPoint )
                                    continue;
                                if ( !placeable.GetCanBeConnected( other ) )
                                    continue;

                                localDataList.Add( RefPair<A5Placeable, A5Placeable>.Create( placeable, other ) );
                            }
                        }

                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                        {
                            //in here we can only localDataList, because that's held in memory for us.
                            //if we try to mess with workingObjects, it's going to be in an indeterminate state
                            switch ( Stage )
                            {
                                case GeneralUndoActionStage.Execute:
                                case GeneralUndoActionStage.Redo:
                                    foreach ( RefPair<A5Placeable, A5Placeable> kvv in localDataList )
                                    {
                                        if ( !kvv.LeftItem || !kvv.RightItem )
                                            continue;
                                        if ( !kvv.LeftItem.RootGO.activeInHierarchy || !kvv.RightItem.RootGO.activeInHierarchy )
                                            continue;

                                        kvv.LeftItem.ConnectPathPoints( kvv.RightItem );
                                    }
                                    break;
                                case GeneralUndoActionStage.Undo:
                                    foreach ( RefPair<A5Placeable, A5Placeable> kvv in localDataList )
                                    {
                                        if ( !kvv.LeftItem || !kvv.RightItem )
                                            continue;
                                        if ( !kvv.LeftItem.RootGO.activeInHierarchy || !kvv.RightItem.RootGO.activeInHierarchy )
                                            continue;

                                        kvv.LeftItem.DisconnectPathPoints( kvv.RightItem );
                                    }
                                    break;
                            }
                        } );
                        #endregion LevelEditor_ConnectPathingPoints
                    }
                    break;
                case "LevelEditor_DisconnectPathingPoints":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        #region LevelEditor_DisconnectPathingPoints
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.FillListOfSelectedObjects( workingObjects );
                        if ( workingObjects.Count <= 0 )
                            break; //nothing to do!

                        //make a local list, so that can be used in the undo action.  It will stay in memory until the undo action goes out of scope.
                        ThrowawayList<RefPair<A5Placeable, A5Placeable>> localDataList = new ThrowawayList<RefPair<A5Placeable, A5Placeable>>();
                        foreach ( A5Placeable placeable in workingObjects )
                        {
                            if ( !placeable || !placeable.AdvancedIsPathingPoint )
                                continue;

                            foreach ( A5Placeable other in workingObjects )
                            {
                                if ( !other || other == placeable || !other.AdvancedIsPathingPoint )
                                    continue;
                                if ( !placeable.GetAreAlreadyConnected( other ) )
                                    continue;

                                localDataList.Add( RefPair<A5Placeable, A5Placeable>.Create( placeable, other ) );
                            }
                        }

                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                        {
                            //in here we can only localDataList, because that's held in memory for us.
                            //if we try to mess with workingObjects, it's going to be in an indeterminate state
                            switch ( Stage )
                            {
                                case GeneralUndoActionStage.Execute:
                                case GeneralUndoActionStage.Redo:
                                    foreach ( RefPair<A5Placeable, A5Placeable> kvv in localDataList )
                                    {
                                        if ( !kvv.LeftItem || !kvv.RightItem )
                                            continue;
                                        if ( !kvv.LeftItem.RootGO.activeInHierarchy || !kvv.RightItem.RootGO.activeInHierarchy )
                                            continue;

                                        kvv.LeftItem.DisconnectPathPoints( kvv.RightItem );
                                    }
                                    break;
                                case GeneralUndoActionStage.Undo:
                                    foreach ( RefPair<A5Placeable, A5Placeable> kvv in localDataList )
                                    {
                                        if ( !kvv.LeftItem || !kvv.RightItem )
                                            continue;
                                        if ( !kvv.LeftItem.RootGO.activeInHierarchy || !kvv.RightItem.RootGO.activeInHierarchy )
                                            continue;

                                        kvv.LeftItem.ConnectPathPoints( kvv.RightItem );
                                    }
                                    break;
                            }
                        } );
                        #endregion LevelEditor_DisconnectPathingPoints
                    }
                    break;
                case "LevelEditor_RandomYRotation":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        #region LevelEditor_RandomYRotation
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.FillListOfSelectedObjects( workingObjects );
                        if ( workingObjects.Count <= 0 )
                            break; //nothing to do!

                        //make a local list, so that can be used in the undo action.  It will stay in memory until the undo action goes out of scope.
                        ThrowawayList<RefThreeTuple<A5Placeable, Vector3, Vector3>> localDataList = new ThrowawayList<RefThreeTuple<A5Placeable, Vector3, Vector3>>();
                        foreach ( A5Placeable placeable in workingObjects )
                        {
                            if ( !placeable )
                                continue;

                            Vector3 startRotation = placeable.transform.localEulerAngles;

                            //we need to precalculate this so that redo will work consistently
                            Vector3 endRotation = new Vector3( startRotation.x, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 359 ),
                                startRotation.z );

                            localDataList.Add( RefThreeTuple<A5Placeable, Vector3, Vector3>.Create( placeable, startRotation, endRotation ) );
                        }

                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                        {
                            //in here we can only localDataList, because that's held in memory for us.
                            //if we try to mess with workingObjects, it's going to be in an indeterminate state
                            switch ( Stage )
                            {
                                case GeneralUndoActionStage.Execute:
                                case GeneralUndoActionStage.Redo:
                                    foreach ( RefThreeTuple<A5Placeable, Vector3, Vector3> kvv in localDataList )
                                    {
                                        if ( !kvv.FirstItem )
                                            continue;

                                        kvv.FirstItem.transform.localEulerAngles = kvv.ThirdItem; //endRotation
                                    }
                                    break;
                                case GeneralUndoActionStage.Undo:
                                    foreach ( RefThreeTuple<A5Placeable, Vector3, Vector3> kvv in localDataList )
                                    {
                                        if ( !kvv.FirstItem )
                                            continue;

                                        kvv.FirstItem.transform.localEulerAngles = kvv.SecondItem; //startRotation
                                    }
                                    break;
                            }
                        } );
                        #endregion
                    }
                    break;
                case "LevelEditor_JitterXZPosition":
                    if ( Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        #region LevelEditor_JitterXZPosition
                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.FillListOfSelectedObjects( workingObjects );
                        if ( workingObjects.Count <= 0 )
                            break; //nothing to do!

                        bool jitterLarger = InputActionTypeDataTable.Instance.GetRowByIDOrNullIfNotFound( "LevelEditor_JitterXZPosition_Larger" ).CalculateIsKeyDownNow_IgnoreConflicts();
                        float jitterScale = 0.1f;
                        if ( jitterLarger )
                            jitterScale = 2f;

                        //make a local list, so that can be used in the undo action.  It will stay in memory until the undo action goes out of scope.
                        ThrowawayList<RefThreeTuple<A5Placeable, Vector3, Vector3>> localDataList = new ThrowawayList<RefThreeTuple<A5Placeable, Vector3, Vector3>>();
                        foreach ( A5Placeable placeable in workingObjects )
                        {
                            if ( !placeable )
                                continue;

                            //we need to precalculate this so that redo will work consistently
                            Vector3 offsetAmount = new Vector3( Engine_Universal.PermanentQualityRandom.NextFloat( -jitterScale, jitterScale ), 0,
                                Engine_Universal.PermanentQualityRandom.NextFloat( -jitterScale, jitterScale ) );

                            localDataList.Add( RefThreeTuple<A5Placeable, Vector3, Vector3>.Create( placeable, placeable.transform.localPosition, offsetAmount ) );
                        }

                        (Engine_Universal.GameLoop as LevelEditorCoreGameLoop).LevelEditorHook.AddNewUndoAction( delegate ( GeneralUndoActionStage Stage )
                        {
                            //in here we can only localDataList, because that's held in memory for us.
                            //if we try to mess with workingObjects, it's going to be in an indeterminate state
                            switch ( Stage )
                            {
                                case GeneralUndoActionStage.Execute:
                                case GeneralUndoActionStage.Redo:
                                    foreach ( RefThreeTuple<A5Placeable, Vector3, Vector3> kvv in localDataList )
                                    {
                                        if ( !kvv.FirstItem )
                                            continue;

                                        kvv.FirstItem.transform.localPosition = kvv.SecondItem + kvv.ThirdItem; //add the jitter
                                    }
                                    break;
                                case GeneralUndoActionStage.Undo:
                                    foreach ( RefThreeTuple<A5Placeable, Vector3, Vector3> kvv in localDataList )
                                    {
                                        if ( !kvv.FirstItem )
                                            continue;

                                        kvv.FirstItem.transform.localPosition = kvv.SecondItem; //go back to the original
                                    }
                                    break;
                            }
                        } );
                        #endregion LevelEditor_JitterXZPosition
                    }
                    break;
                case "ResetCameraRotationAndTilt":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        VisManagerVeryBase.Instance.MainCamera_ResetOrientation();
                    }
                    break;
                case "Inventory":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.Resources );
                    break;
                case "ToggleHardware":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.Hardware );
                    break;
                case "ToggleStructuresWithComplaints":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.StructuresWithComplaints );
                    break;
                case "ChangeStance":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( (Engine_HotM.SelectedActor is MachineStructure) )
                            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                        else
                            VisCommands.ToggleStanceWindow();
                    }
                    break;
                case "OpenLensFilters":
                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        return;
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        Window_LensFilters.HandleOpenCloseToggle();
                    break;
                case "ScrapSelected":
                    VisCommands.ScrapSelected();
                    break;
                case "TriggerStructureAbility":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( (Engine_HotM.SelectedActor is MachineStructure structure) )
                        {
                            structure?.CurrentJob?.Implementation?.HandleJobActivationLogic( structure, structure?.CurrentJob, null, JobActivationLogic.HandleActivationTrigger, out _, out _ );
                        }
                    }
                    break;
                case "LoadoutWindow":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( (Engine_HotM.SelectedActor is MachineStructure) )
                        { }
                        else if ( (Engine_HotM.SelectedActor is ISimNPCUnit npc && !npc.GetIsPlayerControlled() ) )
                        { }
                        else
                            Window_ActorCustomizeLoadout.Instance.ToggleOpen();
                    }
                    break;
                case "ToggleResearch":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.Research );
                    break;
                case "SaveGame":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        Window_SaveGameMenu.Instance.Open();
                    break;
                case "LoadGame":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        Window_LoadGameMenu.Instance.Open();
                    break;
                case "ShowPerformanceStats":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ShowPerformance();
                    break;
                case "ShowSFXTestWindow":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( Window_SFXTestWindow.Instance.IsOpen )
                            Window_SFXTestWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                        else
                            Window_SFXTestWindow.Instance.Open();
                    }
                    break;
                case "ShowParticleTestWindow":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( Window_ParticleTestWindow.Instance.IsOpen )
                            Window_ParticleTestWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                        else
                            Window_ParticleTestWindow.Instance.Open();
                    }
                    break;
                case "MakeSelectedMaterialize_Appear":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor &&
                            Engine_HotM.SelectedActor is ISimMapMobileActor mobileActor )
                        {
                            mobileActor.Materializing = MaterializeType.Appear;
                            mobileActor.MaterializingProgress = 0;
                        }
                    }
                    break;
                case "MakeSelectedMaterialize_Reveal":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor &&
                            Engine_HotM.SelectedActor is ISimMapMobileActor mobileActor )
                        {
                            mobileActor.Materializing = MaterializeType.Reveal;
                            mobileActor.MaterializingProgress = 0;
                        }
                    }
                    break;
                case "MakeSelectedMaterialize_BurnDown":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor &&
                            Engine_HotM.SelectedActor is ISimMapMobileActor mobileActor )
                        {
                            mobileActor.Materializing = mobileActor.GetBurnDownType();
                            mobileActor.MaterializingProgress = 0;
                        }
                    }
                    break;
                case "InstantKillSelectedNPC":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor && SimCommon.IsCheatTimeline &&
                            Engine_HotM.SelectedActor != null && Engine_HotM.SelectedActor is ISimNPCUnit npcUnit )
                        {
                            npcUnit.UnitType.OnDeath.DuringGame_PlayAtLocation( npcUnit.GetEmissionLocation() );
                            npcUnit.DisbandNPCUnit( NPCDisbandReason.Cheat );
                        }
                    }
                    break;
                case "TestFireSelectedUnit":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        {
                            if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor && machineActor.IsInConsumableTargetingMode != null )
                            {
                                machineActor.IsInConsumableTargetingMode.TestFireConsumableVisualsAgainstTarget( machineActor,
                                    MouseHelper.ActorUnderCursor, Engine_Universal.PermanentQualityRandom );
                            }
                            else
                                Engine_HotM.SelectedActor?.TestFireWeapons( Engine_Universal.PermanentQualityRandom );
                        }
                    }
                    break;
                case "WarpOutSelectedNPC":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor && SimCommon.IsCheatTimeline &&
                            Engine_HotM.SelectedActor != null && Engine_HotM.SelectedActor is ISimNPCUnit npcUnit )
                        {
                            npcUnit.DisbandNPCUnit( NPCDisbandReason.WantedToLeave ); //also a cheat, but we want that effect
                        }
                    }
                    break;
                case "InstantKillSelectedPlayerUnit":
                    {
                        if ( !Engine_Universal.GameLoop.IsLevelEditor && 
                            Engine_HotM.SelectedActor != null && Engine_HotM.SelectedActor is ISimMachineActor machineActor )
                        {
                            machineActor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.Cheat );
                        }
                    }
                    break;
                #region Abilities
                case "Ability1":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 1, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability2":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 2, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability3":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 3, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability4":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 4, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability5":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 5, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability6":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 6, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability7":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 7, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability8":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 8, TriggerStyle.DirectByPlayer );
                    break;
                case "Ability9":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        MouseHelper.TriggerNumberedAbility( 9, TriggerStyle.DirectByPlayer );
                    break;
                #endregion Abilities
                #region PhotoModePositions
                case "PhotoModeSetPosition1":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[0].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition1":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[0].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition2":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[1].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition2":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[1].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition3":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[2].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition3":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[2].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition4":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[3].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition4":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[3].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition5":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[4].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition5":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[4].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition6":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[5].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition6":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[5].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition7":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[6].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition7":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[6].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition8":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[7].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition8":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[7].ReturnCameraToPosition();
                    break;
                case "PhotoModeSetPosition9":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[8].SetPositionToCurrent();
                    break;
                case "PhotoModeGoToPosition9":
                    if ( VisCurrent.IsInPhotoMode )
                        SimCommon.PhotoModePositions[8].ReturnCameraToPosition();
                    break;
                #endregion PhotoModePositions
                case "OpenCheats":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( !Window_CheatWindow.Instance.IsOpen )
                            Window_CheatWindow.Instance.Open();
                        else
                            Window_CheatWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
                    break;
                case "OpenSettings":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !Window_SettingsMenu.Instance.IsOpen )
                        {
                            if ( VisCurrent.IsInPhotoMode )
                                Window_SettingsMenu.Instance.ShowOnlyCategory = "PhotoMode";
                            else
                                Window_SettingsMenu.Instance.ShowOnlyCategory = string.Empty;

                            Window_SettingsMenu.Instance.Open();
                        }
                        else
                            Window_SettingsMenu.Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
                    break;
                case "OpenControls":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !Window_ControlBindingsMenu.Instance.IsOpen )
                            Window_ControlBindingsMenu.Instance.Open();
                        else
                            Window_ControlBindingsMenu.Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
                    break;
                case "OpenBuildingOverlays":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                    {
                        if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                            return;
                        if ( !Window_FilterOverlayWindow.Instance.IsOpen )
                            Window_FilterOverlayWindow.Instance.Open();
                        else
                            Window_FilterOverlayWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                    }
                    break;
                case "RecentEvents":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.RecentEvents);
                    break;
                case "Handbook":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.MachineHandbook );
                    break;
                case "QuitToMainMenu":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.HandleQuitToMainMenu();
                    break;
                case "ExitToOS":
                    if ( !Engine_Universal.GameLoop.IsLevelEditor )
                        VisCommands.HandleExitToOS();
                    break;
            }
        }
    }
}
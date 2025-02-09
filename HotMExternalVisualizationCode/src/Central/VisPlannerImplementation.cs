using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using Arcen.HotM.ExternalVis.CityLifeEffects;
using System;
using System.Diagnostics;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    /// <summary>
    /// These items are run on various threads, and help us wire together various dlls into being able to call to each other
    /// </summary>
    public class VisPlannerImplementation : VisPlanner
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            CityLifeVis.GameSim.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
            VisCentralData.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
            NoteLog.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
            VisCommands.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
            SharedRenderManagerData.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap();
        }

        public VisPlannerImplementation()
        {
            VisPlanner.Instance = this;
        }

        public override void DoStartOfGameFromMainMenu_OnMainThread( GameStartData StartData )
        {
            if ( ArcenThreading.IsCurrentlyBlockedForShutdown.IsBusy() )
            {
                ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null, 
                    LocalizedString.AddLang_New( "Popup_LoadFail" ),
                    LocalizedString.AddLang_New( "Popup_LoadFail_BackgroundThreadsShuttingDown" ),
                    LangCommon.Popup_Common_Ok.LocalizedString );
                return;
            }
            if ( Engine_Universal.CheckIfErrorShouldBeIgnored() )
                Engine_Universal.ResetErrorIgnoring();

            Engine_HotM.GameStatus = MainGameStatus.Ingame;
                        
            if ( !CityMap.WasMapLoadedFromSavegame ) //only do these things if we are NOT loading from a savegame!
            {
                if ( StartData == null )
                {
                    ModalPopupData.CreateAndLogOKStyle( PopupSizeStyle.Normal, null,
                    LocalizedString.AddLang_New( "Popup_LoadFail" ),
                    LocalizedString.AddLang_New( "Popup_LoadFail_MissingStartData" ),
                    LangCommon.Popup_Common_Ok.LocalizedString );
                    return;
                }

                int cityStartRank = SimMetagame.AllTimelines.Count > 0 ? 2 : 1;

                if ( StartData.IsNewProfile )
                {
                    SimMetagame.ProfileGUID = StartData.ProfileGUID;
                    SimMetagame.ProfileName = StartData.ProfileName;
                    SimMetagame.StartType = StartData.StartType;

                    if ( SimMetagame.StartType.SkipToChapter >= 2 )
                        cityStartRank = 2;
                }

                EndOfTimeMap.CreateInitialEndOfTimealMap();

                if ( EndOfTimeMap.RockOutcrops.Count == 0 )
                {
                    ArcenDebugging.LogSingleLine( "No rock outcrops were seeded in the end of time!", Verbosity.ShowAsError );
                    return;
                }

                EndOfTimeItem firstRock = EndOfTimeMap.RockOutcrops[0];

                CityTimeline timeline = CityTimeline.CreateNew( StartData.CityName, StartData.CityGUID, cityStartRank );
                timeline.ChildOfEndOfTimeObjectWithID = firstRock.ItemID;
                timeline.CitySlotIndexUsedFromParent = firstRock.TryGetRandomUnusedSlotIndex( Engine_Universal.PermanentQualityRandom );
                firstRock.TrySetSubObjectSlotAsBeingInUse( timeline.CitySlotIndexUsedFromParent, timeline );
                SimCommon.CurrentTimeline = timeline;

                if ( timeline.CitySlotIndexUsedFromParent < 0 )
                {
                    ArcenDebugging.LogSingleLine( "No CitySlotIndexUsedFromParent able to be set for the first city!  First rock outcrop reports slot count: " + 
                        firstRock.GetSubObjectCount(), Verbosity.ShowAsError );
                    return;
                }
            }

            //all of these things have already been done if it was a savegame load!
            if ( !CityMap.WasMapLoadedFromSavegame )
            {
                CommonPlanner.Instance.OnMainGameStarted();
                AbstractSimPlanner.Instance.OnMainGameStarted();
                VisPlanner.Instance.OnMainGameStarted();
            }

            VisCentralData.IsWaitingToStartSimOnMapgen = true;
        }

        public override void DoPerFrame_PreUI()
        {
            if ( Engine_Universal.GameLoop == null )
                return;

            if ( Engine_Universal.GameLoop.IsLevelEditor )
                this.DoPerFrame_LevelEditor_PreUI();
            else
                this.DoPerFrame_MainGame_PreUI();

            //A5BackgroundRenderManager.DebugDrawAllCells( mainGameHookBase );
        }

        public override void DoPerFrame_PostUI()
        {
            RenderManager.BlockFurtherRenderCalls = true;

            if ( Engine_Universal.GameLoop == null )
                return;

            if ( Engine_Universal.GameLoop.IsLevelEditor )
                this.DoPerFrame_LevelEditor_PostUI();
            else
                this.DoPerFrame_MainGame_PostUI();

            //A5BackgroundRenderManager.DebugDrawAllCells( mainGameHookBase );
        }

        #region DoPerFrame_LevelEditor_PreUI
        public void DoPerFrame_LevelEditor_PreUI()
        {
            //nothing to do!
        }
        #endregion

        #region DoPerFrame_LevelEditor_PostUI
        public void DoPerFrame_LevelEditor_PostUI()
        {
            LevelEditorCoreGameLoop levelEditorGameLoop = Engine_Universal.GameLoop as LevelEditorCoreGameLoop;
            if ( !levelEditorGameLoop )
                return;
            LevelEditorHookBase levelEditorHookBase = levelEditorGameLoop.LevelEditorHook;
            if ( !levelEditorHookBase )
                return;

            Camera cam = Engine_HotM.GameModeData?.MainCamera;
            Camera iconMixedCam = Engine_HotM.GameModeData?.IconCamera_MixedIn;
            Camera iconOverlayCam = Engine_HotM.GameModeData?.IconCamera_Overlay;

            if ( cam && iconMixedCam && iconOverlayCam )
            {
                RenderManager_LevelEditor.RenderFrame( cam, iconMixedCam, iconOverlayCam );
            }
        }
        #endregion

        private Stopwatch stopwatch = new Stopwatch();

        private bool mainGameWantsToRenderInPostUI = false;
        private float nextEventQueueCheck = 0f;

        #region DoPerFrame_MainGame_PreUI
        public void DoPerFrame_MainGame_PreUI()
        {
            mainGameWantsToRenderInPostUI = false;

            MainGameCoreGameLoop mainGameLoop = Engine_Universal.GameLoop as MainGameCoreGameLoop;
            if ( !mainGameLoop )
                return;
            MainGameHookBase mainGameHookBase = mainGameLoop.MainGameHook;
            if ( !mainGameHookBase )
                return;

            stopwatch.Reset();
            stopwatch.Start();

            VisCentralData.DoPerFrame();

            Camera renderCam = Engine_HotM.GameModeData?.MainCamera;
            Transform rCamTransform = Engine_HotM.GameModeData?.MainCameraTransform;

            if ( renderCam && rCamTransform )
            {
                switch (SimCommon.OpenWindowRequest )
                {
                    case OpenWindowRequest.Reward:
                        if ( SimCommon.RewardProvider != null )
                        {
                            if ( !Window_RewardWindow.Instance.IsOpen )
                                Window_RewardWindow.Instance.Open();
                        }
                        break;
                    case OpenWindowRequest.Research:
                        VisCommands.ToggleMajorWindowMode( MajorWindowMode.Research );
                        break;
                    case OpenWindowRequest.StructureSpecialty:
                        if ( Engine_HotM.SelectedActor is MachineStructure structure )
                        {
                            if ( !Window_StructureChoiceList.Instance.IsOpen || Window_StructureChoiceList.RelatedStructure != structure ||
                                Window_StructureChoiceList.Mode != StructureChoiceMode.SpecialtyList )
                                Window_StructureChoiceList.HandleOpenCloseToggle( StructureChoiceMode.SpecialtyList, structure );
                        }
                        break;
                    case OpenWindowRequest.Handbook:
                        {
                            VisCommands.ToggleMajorWindowMode( MajorWindowMode.MachineHandbook );
                        }
                        break;
                    case OpenWindowRequest.ProjectHistory:
                        {
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.ProjectHistory );
                        }
                        break;
                    case OpenWindowRequest.DoomList:
                        {
                            VisCommands.ToggleVictoryPath_TargetTab( Window_VictoryPath.VictoryTabType.Dooms );
                        }
                        break;
                    case OpenWindowRequest.TechHistory:
                        {
                            VisCommands.ToggleHistory_TargetTab( Window_PlayerHistory.HistoryType.UnlockHistory );
                        }
                        break;
                    case OpenWindowRequest.Debate:
                        if ( SimCommon.DebateScenarioType != null && SimCommon.DebateTarget != null && SimCommon.DebateSource != null && SimCommon.DebateStartingChoice != null )
                        {
                            if ( !Window_Debate.Instance.IsOpen )
                                Window_Debate.Instance.Open();
                        }
                        break;
                }
                SimCommon.OpenWindowRequest = OpenWindowRequest.None;

                if ( SimCommon.PointAtThisHandbookEntry != null )
                {
                    Window_Handbook.EntryToShow = SimCommon.PointAtThisHandbookEntry;
                    if ( !Window_Handbook.Instance.IsOpen )
                        Window_Handbook.Instance.Open();
                    SimCommon.PointAtThisHandbookEntry = null;
                }

                if ( SimCommon.Turn == 1 && SimMetagame.CurrentChapterNumber == 0 && !FlagRefs.ShotACh0Tail.DuringGameplay_IsTripped )
                {
                    if ( !(Engine_HotM.SelectedActor is ISimMachineUnit) )
                        Engine_HotM.SetSelectedActor( SimCommon.AllMachineActors.GetDisplayList().FirstOrDefault, false, true, true );
                }
                else if ( SimCommon.Turn == 1 && FlagRefs.HasFinishedUITour != null && !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                {
                    if ( !(Engine_HotM.SelectedActor is ISimMachineUnit) )
                        Engine_HotM.SetSelectedActor( SimCommon.AllMachineActors.GetDisplayList().FirstOrDefault, false, true, true );
                }

                if ( !WorldSaveLoad.IsLoadingAtTheMoment )
                {
                    if ( SimCommon.Turn > 1 && FlagRefs.HasFinishedUITour != null && !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                        FlagRefs.FinishUITour();
                    else if ( FlagRefs.HasBeenAskedAboutUITour != null && !FlagRefs.HasBeenAskedAboutUITour.DuringGameplay_IsTripped &&
                        SimMetagame.CurrentChapter != null && SimMetagame.CurrentChapter.Meta_HasShownBanner && Engine_Universal.CurrentPopups.Count == 0 && SimCommon.Turn == 1 )
                    {
                        ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small,
                            delegate
                            {
                                FlagRefs.HasBeenAskedAboutUITour.TripIfNeeded();
                            }, delegate { FlagRefs.FinishUITour(); },
                            LocalizedString.AddLang_New( "UITour_OfferHeader" ),
                            LocalizedString.AddLang_New( "UITour_OfferBody1" ).AddRaw( "\n" )
                                .AddLang( "UITour_OfferBody2" ).AddRaw( "\n" )
                                .AddLang( "UITour_OfferBody3" ).AddRaw( "\n" )
                                .AddLang( "UITour_OfferBody4" ),
                            LocalizedString.AddLang_New( "UITour_OfferYes" ),
                            LocalizedString.AddLang_New( "UITour_OfferNo" ) );
                    }
                }

                if ( SimCommon.CurrentSimpleChoice == null && !VisCommands.GetIsAnyBigWindowOpen() && nextEventQueueCheck < ArcenTime.AnyTimeSinceStartF &&
                    !WorldSaveLoad.IsSavingOrLoadingAtTheMoment )
                {
                    nextEventQueueCheck = ArcenTime.AnyTimeSinceStartF + 0.5f;
                    if ( SimCommon.QueuedMinorEvents.TryDequeue( out QueuedMinorEvent mEvent ) )
                    {
                        mEvent.DoQueuedEventNow();
                        //ArcenDebugging.LogSingleLine( "Unpack event!", Verbosity.DoNotShow );
                    }
                }

                //these ones will also cause dimming!
                Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General = Window_ActorCustomizeLoadout.Instance.IsOpen ||
                    Window_ActorStanceChange.Instance.IsOpen ||
                    Window_AbilityOptionList.Instance.IsOpen;

                if ( Window_RewardWindow.Instance.IsOpen && SimCommon.RewardProvider == NPCDialogChoiceHandler.Instance )
                {
                    VisCurrent.ShouldShowBlurForSomeOtherReason = true; //blur this one
                    Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General = true; //dim this also
                }
                else if ( SimCommon.CurrentSimpleChoice == MinorEventHandler.Instance )
                {
                    VisCurrent.ShouldShowBlurForSomeOtherReason = true; //blur this one
                    Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General = true; //dim this also
                }
                else if ( Window_Debate.Instance.IsOpen )
                {
                    VisCurrent.ShouldShowBlurForSomeOtherReason = true; //blur this one
                    Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General = true; //dim this also
                }
                else
                    VisCurrent.ShouldShowBlurForSomeOtherReason = false;

                Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_AlsoDim = Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General;

                //these ones will NOT cause dimming
                if ( !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General )
                {
                    Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General = SimCommon.CurrentSimpleChoice != null ||
                        Window_RewardWindow.Instance.IsOpen ||
                        Window_NetworkNameWindow.Instance.IsOpen;
                }

                Engine_HotM.IsBigBannerBeingShown = VisCurrent.IsShowingChapterChange ||
                    !(SimMetagame.CurrentChapter?.Meta_HasShownBanner??false) ||
                    Window_ChapterChange.Instance.GetShouldDrawThisFrame() || //project display not counting
                    Window_GameOverBanner.Instance.GetShouldDrawThisFrame();

                VisCurrent.IsInHackingInterface = Engine_HotM.CurrentLowerMode == CommonRefs.HackingScene;// Window_Hacking.Instance.GetShouldDrawThisFrame();

                if ( ( Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || ArcenUI.CurrentlyShownWindowsWith_ShouldPretendDoesNotBlockUIForMouseWhenOpen.Count > 0 ) && 
                    !VisCurrent.IsShowingActualEvent ) //if showing an event, do not do the handling below
                {
                    if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //cancel
                        EscapeWindowStackController.HandleCancel( WindowCloseReason.UserCasualRequest );
                }

                CameraCurrent.IsIndoorRainSounds = VisCurrent.GetShouldBeBlurred() ||
                    SimCommon.CurrentSimpleChoice != null ||
                    //Window_ProjectOutcomeWindow.Instance.IsOpen ||
                    //Window_ProjectRewardWindow.Instance.IsOpen ||
                    VisCurrent.IsShowingActualEvent ||
                    Engine_HotM.GameMode == MainGameMode.CityMap;

                if ( SimCommon.CurrentSimpleChoice == MinorEventHandler.Instance )
                    SimCommon.CurrentMinorEvent = MinorEventHandler.Instance.MinorEvent;
                else
                    SimCommon.CurrentMinorEvent = null;

                //Camera iconMixedCam = Engine_HotM.GameModeData?.IconCamera_MixedIn;
                //Camera iconOverlayCam = Engine_HotM.GameModeData?.IconCamera_Overlay;
                bool allowActualRenderLogic = true;

                int startingTicks = (int)stopwatch.ElapsedTicks;
                if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                {
                    //this is the main menu!

                    SimTimingInfo.VisFrameEarlyRenderCalculations.LogCurrentTicks( 0 );
                    SimTimingInfo.VisFrameRenderCalculations.LogCurrentTicks( (int)stopwatch.ElapsedTicks - startingTicks );

                    RenderManager.PrepForRender();

                    try
                    {
                        ClearWorldMapSettings();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "Main menu frame error: " + e, Verbosity.ShowAsError );
                    }
                    SimCommon.NeedsVisibilityGranterRecalculation = true;
                }
                else
                {
                    //this is the actual game!

                    if ( VisCurrent.ShouldDrawLoadingMenuBuildings )
                        allowActualRenderLogic = false;

                    if ( allowActualRenderLogic )
                    {
                        if ( SimCommon.NeedsVisibilityGranterRecalculation || CityMap.CollidablesCreatedSinceLastVisibilityGranterCalculation.Count > 0 )
                            VisibilityGranterCalculator.Recalculate();

                        NPCTargetingCalculator.RecalculateIfNeeded();

                        if ( SimCommon.NeedsNetworkRecalculation )
                            NetworkConnectionCalculator.Recalculate();
                        else
                        {
                            if ( !NetworkConnectionCalculator.GetIsCalculatingNow() && ArcenTime.AnyTimeSinceStartF - NetworkConnectionCalculator.LastStartedOrFinished > 1f )
                                NetworkConnectionCalculator.Recalculate();
                        }

                        Engine_Universal.BeginProfilerSample( "CameraCurrent-PerFrame" );
                        CameraCurrent.PerFrame( renderCam, rCamTransform );
                        PulsingBeaconStates.UpdatePerFrame();
                        SharedRenderManagerData.RecalculateIndicators();
                        SharedRenderManagerData.DoUniversalClearBeforeNewFrame();
                        Engine_Universal.EndProfilerSample( "CameraCurrent-PerFrame" );

                        Engine_Universal.BeginProfilerSample( "PreRenderFrame" );
                        try
                        {
                            FrameBufferManagerData.StartFrameBuffers();
                            RenderManager.PrepForRender();

                            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                            if ( lowerMode != null )
                            {
                                RenderHelper_EventCamera.RenderIfNeeded();

                                int newMS = (int)stopwatch.ElapsedTicks;
                                SimTimingInfo.VisFrameEarlyRenderCalculations.LogCurrentTicks( newMS - startingTicks );
                                startingTicks = newMS;

                                //todo on lower modes

                                A5ObjectAggregation.FloatingIconListPool.DrawAllActiveInstructions();
                                A5ObjectAggregation.FloatingIconColliderPool.DrawAllActiveItems();
                            }
                            else
                            {
                                int newMS = (int)stopwatch.ElapsedTicks;
                                SimTimingInfo.VisFrameEarlyRenderCalculations.LogCurrentTicks( newMS - startingTicks );
                                startingTicks = newMS;

                                switch ( Engine_HotM.GameMode )
                                {
                                    case MainGameMode.Streets:
                                        RenderManager_Streets.PreRenderFrame();
                                        break;
                                    case MainGameMode.CityMap:
                                        RenderManager_CityMap.PreRenderFrame();
                                        break;
                                    case MainGameMode.TheEndOfTime:
                                        RenderManager_TheEndOfTime.PreRenderFrame();
                                        break;
                                }
                            }
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "PreRenderFrame Outer Error: " + e, Verbosity.ShowAsError );
                        }
                        Engine_Universal.EndProfilerSample( "PreRenderFrame" );
                    }
                    else
                    {
                        SimTimingInfo.VisFrameEarlyRenderCalculations.LogCurrentTicks( 0 );

                        //without this logic, a bunch of stuff will linger and still be drawn!
                        FrameBufferManagerData.StartFrameBuffers();
                        RenderManager.PrepForRender();
                    }

                    SimTimingInfo.VisFrameRenderCalculations.LogCurrentTicks( (int)stopwatch.ElapsedTicks - startingTicks );

                    try
                    {
                        CalculateWorldMapSettings();

                        startingTicks = (int)stopwatch.ElapsedTicks;

                        if ( allowActualRenderLogic )
                        {
                            Engine_Universal.BeginProfilerSample( "TooltipsLogic" );
                            VisBottomSecondarySideTooltip.DoPerFrame();
                            VisAbilityBarNote.DoPerFrame();

                            NoteLog.DoPerFrame();
                            ExampleMessageHandler.DoPerFrame();

                            Engine_Universal.EndProfilerSample( "TooltipsLogic" );
                        }

                        SimTimingInfo.VisUICalculations.LogCurrentTicks( (int)stopwatch.ElapsedTicks - startingTicks );

                        if ( allowActualRenderLogic )
                        {
                            Engine_Universal.BeginProfilerSample( "CityLifeAndDrones" );
                            startingTicks = (int)stopwatch.ElapsedTicks;
                            bool didCityLife = CityLifeVis.GameSim.DoPerFrame();
                            SimTimingInfo.CityLifeMainThread.LogCurrentTicks( (int)stopwatch.ElapsedTicks - startingTicks );

                            startingTicks = (int)stopwatch.ElapsedTicks;
                            SimTimingInfo.DroneWork.LogCurrentTicks( (int)stopwatch.ElapsedTicks - startingTicks );
                            Engine_Universal.EndProfilerSample( "CityLifeAndDrones" );

                            //if ( (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "CommandMode" )
                                Engine_HotM.CurrentCommandModeActionTargeting?.HandleExtraDrawPerFrameWhileActive();

                            //Note: all of the mouse stuff is down in there, along with the MoveHelperStuff
                            Engine_Universal.BeginProfilerSample( "HandleMouseCalculations" );
                            //these bits must be done before the call to ComeBackAndFinishFrameBuffersAndRender or it's just invisible!
                            MouseHelper.HandleMouseCalculations();
                            Engine_Universal.EndProfilerSample( "HandleMouseCalculations" );

                            //this must be done after HandleMouseCalculations, and also after PreRenderFrame
                            //both of those other methods wind up adding to this.
                            ThreatLineData.RenderAnyAggroAndThreatLines();
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "Primary game frame error: " + e, Verbosity.ShowAsError );
                    }
                }

                if ( allowActualRenderLogic )
                {
                    mainGameWantsToRenderInPostUI = true;
                }
            }

            stopwatch.Stop();
            SimTimingInfo.Vis.LogCurrentTicks( (int)stopwatch.ElapsedTicks );
        }
        #endregion

        #region DoPerFrame_MainGame_PostUI
        public void DoPerFrame_MainGame_PostUI()
        {
            stopwatch.Reset();
            stopwatch.Start();

            if ( mainGameWantsToRenderInPostUI )
            {
                Camera renderCam = Engine_HotM.GameModeData?.MainCamera;
                Camera iconMixedCam = Engine_HotM.GameModeData?.IconCamera_MixedIn;
                Camera iconOverlayCam = Engine_HotM.GameModeData?.IconCamera_Overlay;

                Engine_Universal.BeginProfilerSample( "FinalRender" );
                //we do this down here because things like the street mobs and drones also submit themselves to this
                //we don't want those things to be a frame behind or not counted in the frame buffers
                SharedRenderManagerData.ComeBackAndFinishFrameBuffersAndRender_PostUI( renderCam, iconMixedCam, iconOverlayCam );
                Engine_Universal.EndProfilerSample( "FinalRender" );
            }

            stopwatch.Stop();
            SimTimingInfo.VisRender.LogCurrentTicks( (int)stopwatch.ElapsedTicks );
        }
        #endregion

        #region World Map Settings
        public static bool DebugShowMousePosition = false;
        public static bool DebugShowHoveredPlaceableTooltip = false;
        public static bool DebugShowHoveredWorldCellTooltip = false;

        private void CalculateWorldMapSettings()
        {
            DebugShowMousePosition = GameSettings.Current.GetBool( "MainGameDebugMousePosition" );
            DebugShowHoveredPlaceableTooltip = GameSettings.Current.GetBool( "MainGameShowHoveredPlaceableTooltip" );
            DebugShowHoveredWorldCellTooltip = GameSettings.Current.GetBool( "MainGameShowHoveredWorldCellTooltip" );
        }

        private void ClearWorldMapSettings()
        {
            DebugShowMousePosition = false;
            DebugShowHoveredPlaceableTooltip = false;
            DebugShowHoveredWorldCellTooltip = false;
        }
        #endregion

        public override void DoPerFullSecond_BackgroundThread( MersenneTwister Rand )
        {
            MainGameCoreGameLoop mainGameLoop = Engine_Universal.GameLoop as MainGameCoreGameLoop;
            if ( !mainGameLoop )
                return;
            MainGameHookBase mainGameHookBase = mainGameLoop.MainGameHook;
            if ( !mainGameHookBase )
                return;

            NoteLog.DoPerFullSecond_BackgroundThread();
        }

        public override void DoPerQuarterSecond_BackgroundThread( MersenneTwister Rand )
        {
            MainGameCoreGameLoop mainGameLoop = Engine_Universal.GameLoop as MainGameCoreGameLoop;
            if ( !mainGameLoop )
                return;
            MainGameHookBase mainGameHookBase = mainGameLoop.MainGameHook;
            if ( !mainGameHookBase )
                return;

            CityLifeVis.GameSim.DoPerQuarterSecond_BackgroundThread( Rand );

            NoteLog.DoPerQuarterSecond_BackgroundThread();
        }

        /// <summary>
        /// This is right after the player has clicked the "play" button on the main menu.
        /// Right now we're on the main thread, and being told about that.
        /// </summary>
        public override void OnMainGameStarted()
        {

        }

        public override void DoExtraRenderingForScreenshotFrame( Camera screenshotCamera )
        {
            RenderManager.ReRenderPriorFrame( screenshotCamera, null, null );
        }

        #region LevelEditor_CalculateBoundsOverlapDataOnBackgroundThread
        public override void LevelEditor_CalculateBoundsOverlapDataOnBackgroundThread( List<A5Placeable> DirectListOfAllObjectsIncludingDeleted )
        {
            //this is set to only run one at a time, so if one is still running this one will not run again
            ArcenThreading.RunTaskOnBackgroundThread( "_Data.LevelEditor_CalculateBoundsOverlap",
                    ( TaskStartData startData ) =>
                    {
                        try
                        {
                            Helper_CalculateBoundsOverlaps_CalledSeriallyOnly( DirectListOfAllObjectsIncludingDeleted );
                        }
                        catch ( Exception e )
                        {
                            if ( !startData.IsMeantToSilentlyDie() )
                                ArcenDebugging.LogWithStack( "VisPlannerImplementation.LevelEditor_CalculateBoundsOverlapDataOnBackgroundThread background thread error: " + e, Verbosity.ShowAsError );
                        }
                    } );
        }
        #endregion LevelEditor_CalculateBoundsOverlapDataOnBackgroundThread

        #region Helper_CalculateBoundsOverlaps_CalledSeriallyOnly
        private static readonly List<A5Placeable> boundOverlaps_nonDeletedObjects = List<A5Placeable>.Create_WillNeverBeGCed( 4000, "VisPlannerImplementation-boundOverlaps_nonDeletedObjects", 2000 );
        private static readonly List<A5Placeable> boundOverlaps_metaRegionObjects = List<A5Placeable>.Create_WillNeverBeGCed( 400, "VisPlannerImplementation-boundOverlaps_metaRegionObjects", 200 );
        private static readonly List<A5Placeable> boundOverlaps_metaCollisionTargetObject = List<A5Placeable>.Create_WillNeverBeGCed( 4000, "VisPlannerImplementation-boundOverlaps_metaCollisionTargetObject", 2000 );
        private static readonly List<A5Placeable> boundOverlaps_metaDecorationZones = List<A5Placeable>.Create_WillNeverBeGCed( 4000, "VisPlannerImplementation-boundOverlaps_metaDecorationZones", 2000 );
        private static readonly List<A5Placeable> boundOverlaps_Roads = List<A5Placeable>.Create_WillNeverBeGCed( 4000, "VisPlannerImplementation-boundOverlaps_Roads", 2000 );

        /// <summary>
        /// This method is only ever called serially, aka one at a time.  
        /// This is ensured by the _Data.LevelEditor_CalculateBoundsOverlap task.
        /// If that xml is changed, then funky things will happen here
        /// </summary>
        private static void Helper_CalculateBoundsOverlaps_CalledSeriallyOnly( List<A5Placeable> DirectListOfAllObjectsIncludingDeleted )
        {
            #region First Fill The Working Lists And Clear The Construction Values
            boundOverlaps_nonDeletedObjects.Clear();
            boundOverlaps_metaRegionObjects.Clear();
            boundOverlaps_metaCollisionTargetObject.Clear();
            boundOverlaps_metaDecorationZones.Clear();
            boundOverlaps_Roads.Clear();
            for ( int outerIndex = 0; outerIndex < DirectListOfAllObjectsIncludingDeleted.Count; outerIndex++ )
            {
                A5Placeable place = DirectListOfAllObjectsIncludingDeleted[outerIndex];
                if ( !place || !place.IsObjectActive )
                    continue; //skip if deleted
                if ( !place.OBBAndBoundsCache.HasBeenSet )
                    continue; //skip if no OBB
                if ( place.ObjRoot == null || place.ObjRoot.ExtraPlaceableData == null )
                    continue; //skip if this data not populated yet

                place.LevelEditorHasOtherCollision.ClearConstructionValueForStartingConstruction();
                place.LevelEditorOtherCollisions.ClearConstructionListForStartingConstruction();

                boundOverlaps_nonDeletedObjects.Add( place );
                if ( place.AdvancedIsMetaRegion )
                    boundOverlaps_metaRegionObjects.Add( place );
                if ( place.ObjRoot.ExtraPlaceableData.IsMetaOtherObjectCollisionTarget )
                    boundOverlaps_metaCollisionTargetObject.Add( place );
                if ( place.ObjRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                    boundOverlaps_metaDecorationZones.Add( place );
                if ( place.ObjRoot.Roads.Count > 0 && !place.ObjRoot.ExtraPlaceableData.SkipsRoadCollisions )
                    boundOverlaps_Roads.Add( place );
            }
            #endregion

            //now loop over all the meta regions and complain if we find problems
            foreach ( A5Placeable placeOuter in boundOverlaps_metaRegionObjects )
            {
                OBBUnity outerOBB = new OBBUnity( placeOuter.OBBAndBoundsCache.OBB );
                if ( placeOuter.ObjRoot.ExtraPlaceableData.MetaOnly_CollidesWithOtherObjects )
                {
                    //look at all the other objects that are IsMetaOtherObjectCollisionTarget (that's filtered above)
                    foreach ( A5Placeable placeInner in boundOverlaps_metaCollisionTargetObject )
                    {
                        if ( placeInner == placeOuter )
                            continue;
                        OBBUnity innerOBB = placeInner.OBBAndBoundsCache.OBB;

                        if ( outerOBB.IntersectsOBB( innerOBB ) )
                        {
                            //yikes!  we collided with some other object
                            if ( placeInner.ObjRoot.ExtraPlaceableData.RequiresIndividualColliderCheckingDuringBoundingCollisionChecks )
                            {
                                int rotY = Mathf.RoundToInt( placeInner.TRSCache.Rotation.eulerAngles.y );
                                bool hadActualInnerCollision = false;
                                //OBB collision is not good enough, but it's a start.  Now that we know our OBBs intersect, we have to ask -- does that also happen with any of our
                                //colliders on here?
                                foreach ( CollisionBox box in placeInner.ObjRoot.CollisionBoxes )
                                {
                                    Vector3 worldCenter = placeInner.TRSCache.TransformPoint_Threadsafe( box.Center );

                                    Vector3 worldSize = box.Size.ComponentWiseMultWithSimpleYRot( placeInner.TRSCache.LocalScale, rotY )
                                        //this last part is to make sure that the Y is very tall and we don't miss it for that reason
                                        .ReplaceY( 10 );

                                    if ( BoxMath.BoxIntersectsBox( outerOBB.Center, outerOBB.Size, outerOBB.Rotation,
                                        worldCenter, worldSize, innerOBB.Rotation ) )
                                    {
                                        hadActualInnerCollision = true;
                                        break;
                                    }
                                }

                                if ( !hadActualInnerCollision )
                                    continue; //skip us, because we did not have an actual inner collision!
                            }
                            else
                            {
                                //OBB collision is good enough for us, let's move on.
                            }
                            #region Tell Each Object About It
                            //tell the outer about it
                            placeOuter.LevelEditorHasOtherCollision.Construction = true;
                            placeOuter.LevelEditorOtherCollisions.AddToConstructionList( placeInner );

                            //tell the inner about it
                            placeInner.LevelEditorHasOtherCollision.Construction = true;
                            placeInner.LevelEditorOtherCollisions.AddToConstructionList( placeOuter );
                            #endregion
                        }
                    }
                }

                if ( placeOuter.ObjRoot.ExtraPlaceableData.MetaOnly_CollidesWithOtherRegions )
                {
                    //look just at other meta regions
                    foreach ( A5Placeable placeInner in boundOverlaps_metaRegionObjects )
                    {
                        if ( placeInner == placeOuter )
                            continue;
                        if ( !placeInner.ObjRoot.ExtraPlaceableData.MetaOnly_IsRegionCollisionTarget )
                            continue; //if it's not a collision target type of region, then skip!

                        if ( placeOuter.OBBAndBoundsCache.OBB.IntersectsOBB( placeInner.OBBAndBoundsCache.OBB ) )
                        {
                            //yikes!  we collided with another region
                            #region Tell Each Object About It
                            //tell the outer about it
                            placeOuter.LevelEditorHasOtherCollision.Construction = true;
                            placeOuter.LevelEditorOtherCollisions.AddToConstructionList( placeInner );

                            //tell the inner about it
                            placeInner.LevelEditorHasOtherCollision.Construction = true;
                            placeInner.LevelEditorOtherCollisions.AddToConstructionList( placeOuter );
                            #endregion
                        }
                    }
                }

                if ( placeOuter.ObjRoot.ExtraPlaceableData.MetaOnly_HidesWhenDecorationZonesHidden )
                {
                    //if we are a decoration zone, then look at other decoration zones
                    foreach ( A5Placeable placeInner in boundOverlaps_metaDecorationZones )
                    {
                        if ( placeInner == placeOuter )
                            continue;

                        if ( placeOuter.OBBAndBoundsCache.OBB.IntersectsOBB( placeInner.OBBAndBoundsCache.OBB ) )
                        {
                            //yikes!  we collided with another region
                            #region Tell Each Object About It
                            //tell the outer about it
                            placeOuter.LevelEditorHasOtherCollision.Construction = true;
                            placeOuter.LevelEditorOtherCollisions.AddToConstructionList( placeInner );

                            //tell the inner about it
                            placeInner.LevelEditorHasOtherCollision.Construction = true;
                            placeInner.LevelEditorOtherCollisions.AddToConstructionList( placeOuter );
                            #endregion
                        }
                    }
                }
            }

            //now loop over the roads and look for problems
            foreach ( A5Placeable placeOuter in boundOverlaps_Roads )
            {
                OBBUnity outerOBB = new OBBUnity( placeOuter.OBBAndBoundsCache.OBB );
                outerOBB.Inflate( -0.1f, 0, -0.1f );

                foreach ( A5Placeable placeInner in boundOverlaps_Roads )
                {
                    if ( placeInner == placeOuter )
                        continue; //don't collide against ourselves
                    if ( placeInner.ObjRoot.CategoryString != placeOuter.ObjRoot.CategoryString )
                        continue; //don't collide against things from different categories

                    OBBUnity innerOBB = placeInner.OBBAndBoundsCache.OBB;

                    if ( outerOBB.IntersectsOBB( innerOBB ) )
                    {
                        //yikes!  we collided with some other object
                        //OBB collision is good enough for us, let's move on.
                        #region Tell Each Object About It
                        //tell the outer about it
                        placeOuter.LevelEditorHasOtherCollision.Construction = true;
                        placeOuter.LevelEditorOtherCollisions.AddToConstructionList( placeInner );

                        //tell the inner about it
                        placeInner.LevelEditorHasOtherCollision.Construction = true;
                        placeInner.LevelEditorOtherCollisions.AddToConstructionList( placeOuter );
                        #endregion
                    }
                }
            }

            #region Lastly Flip The Construction Values to Display
            foreach ( A5Placeable place in boundOverlaps_nonDeletedObjects )
            {
                place.LevelEditorHasOtherCollision.SwitchConstructionToDisplay();
                place.LevelEditorOtherCollisions.SwitchConstructionToDisplay();
            }
            #endregion
        }
        #endregion

        public override void SerializeData( ArcenFileSerializer Serializer )
        {
            VisCentralData.SerializeData( Serializer );
            NoteLog.SerializeData( Serializer );
        }

        public override void DeserializeData( DeserializedObjectLayer Data )
        {
            VisCentralData.DeserializeData( Data );
            NoteLog.DeserializeData( Data );
		}

        public override void OnEngineNote_FromAnyThread( IGameNote Note, NoteStyle NoteStyle )
        {
            NoteLog.LogEntry( Note, NoteStyle );
        }
    }
}

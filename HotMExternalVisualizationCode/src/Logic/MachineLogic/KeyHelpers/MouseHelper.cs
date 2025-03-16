using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public static class MouseHelper
    {
        public static MachineStructure StructureUnderCursor = null;
        public static ISimBuilding BuildingUnderCursor = null;
        public static ISimBuilding BuildingNoFilterUnderCursor = null;
        public static ISimCityVehicle CityVehicleUnderCursor = null;
        public static ISimMapActor ActorUnderCursor = null;

        private static void ClearAllHovers()
        {
            StructureUnderCursor = null;
            BuildingUnderCursor = null;
            BuildingNoFilterUnderCursor = null;
            CityVehicleUnderCursor = null;
            ActorUnderCursor = null;
            //Engine_HotM.HoveredJob = null; //this probably will cause issues
        }

        #region CalculateActorUnderCursor
        public static ISimMapActor CalculateActorUnderCursor()
        {
            ISimMapActor actorUnderCursor = null;
            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
            {
                if ( icon != null )
                    actorUnderCursor = icon.GetCurrentRelated() as ISimMapActor;
            }
            return actorUnderCursor;
        }
        #endregion

        #region HandleMouseCalculations
        public static void HandleMouseCalculations()
        {
            if ( Engine_HotM.CurrentLowerMode != null )
            {
                ClearAllHovers();
                return; //don't even do the cheat checks in here!
            }

            bool areInteractionModesAllowed = true;
            if ( VisCurrent.IsInPhotoMode )
                areInteractionModesAllowed = false; //do nothing!  No clicks, etc, in here

            if ( Engine_HotM.CurrentCheatOverridingClickMode != null )
            {
                areInteractionModesAllowed = false;

                ISimBuilding clickedBuilding = CursorHelper.FindBuildingUnderCursorNoFilters();

                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //simply mapped to CheatClickType.Left 
                {
                    Engine_HotM.CurrentCheatOverridingClickMode.Implementation.ExecuteCheatClick( Engine_HotM.CurrentCheatOverridingClickMode,
                        Engine_HotM.CurrentCheatSelection, Engine_HotM.CurrentCheatText, clickedBuilding, CheatClickType.Left );
                    return;
                }
                else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //simply mapped to CheatClickType.Right 
                {
                    Engine_HotM.CurrentCheatOverridingClickMode.Implementation.ExecuteCheatClick( Engine_HotM.CurrentCheatOverridingClickMode,
                        Engine_HotM.CurrentCheatSelection, Engine_HotM.CurrentCheatText, clickedBuilding, CheatClickType.Right );
                    return;
                }

                Engine_HotM.CurrentCheatOverridingClickMode.Implementation.ExecuteCheatClick( Engine_HotM.CurrentCheatOverridingClickMode,
                    Engine_HotM.CurrentCheatSelection, Engine_HotM.CurrentCheatText, clickedBuilding, CheatClickType.HoverOnly );
                return;
            }

            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
            {
                if ( TryHandleEndOfTimeCreatorMode() )
                {
                    ClearAllHovers();
                    return;
                }
            }

            if ( Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow || 
                !areInteractionModesAllowed || VisCurrent.IsShowingActualEvent || !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
            {
                ClearAllHovers();
                return; //only check for clicks of any sort when not blocked by other stuff
            }

            if ( Engine_Universal.IsMouseOverGUI )
            {
                if ( Engine_HotM.SelectedMachineActionMode != null ) //handle the selected machine action modes even when over the GUI
                {
                    if ( Engine_HotM.SelectedMachineActionMode.Implementation.HandleActionModeMouseInteractionsAndAnyExtraRendering( Engine_HotM.SelectedMachineActionMode ) )
                        return;
                }
                ClearAllHovers();
                return; //only check for clicks of any sort when not over the UI
            }

            ISimMachineActor selectedMachineActor = Engine_HotM.SelectedActor as ISimMachineActor; //will be null if a different type

            #region First Calculate What The Cursor Is Over
            ISimMapActor actorUnderCursor = null;
            //if ( selectedActor == null || selectedActor.GetIsBlockedFromActing() || InputCaching.IsInInspectMode || 
            //    selectedActor is ISimMachineVehicle )
            {
                if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
                {
                    if ( icon != null )
                        actorUnderCursor = icon.GetCurrentRelated() as ISimMapActor;
                }
            }
            ActorUnderCursor = actorUnderCursor;

            ISimCityVehicle vehicleUnderCursor = null;
            if ( actorUnderCursor == null/* &&
                (selectedActor == null || selectedActor.GetIsBlockedFromActing() || InputCaching.IsInInspectMode)*/ )
            {
                if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated icon )
                {
                    if ( icon != null )
                        vehicleUnderCursor = icon.GetCurrentRelated() as ISimCityVehicle;
                }
            }
            CityVehicleUnderCursor = vehicleUnderCursor;

            BuildingNoFilterUnderCursor = CursorHelper.FindBuildingUnderCursorNoFilters();

            if ( CityVehicleUnderCursor != null || ActorUnderCursor != null )
            {
                BuildingUnderCursor = null;
                StructureUnderCursor = null;
                //Engine_HotM.HoveredJob = null; //this causes it to not work
            }
            else if ( selectedMachineActor != null && selectedMachineActor is ISimMachineActor && ActorUnderCursor != null )
            {
                BuildingUnderCursor = null; //if a machine actor is selected, and hovering over another actor, don't select a building
                StructureUnderCursor = null;
                //Engine_HotM.HoveredJob = null;//this causes it to not work
            }
            else if ( selectedMachineActor != null && (selectedMachineActor.GetIsBlockedFromActingInGeneral() ||
                selectedMachineActor is ISimMachineVehicle) && !InputCaching.IsInInspectMode_Any )
            {
                BuildingUnderCursor = null;

                ISimBuilding building = BuildingNoFilterUnderCursor;
                if ( building != null && building.MachineStructureInBuilding != null )
                    StructureUnderCursor = building.MachineStructureInBuilding;
                else
                    StructureUnderCursor = null;

                if ( StructureUnderCursor != null && InputCaching.IsInInspectMode_Any )
                    Engine_HotM.HoveredJob = StructureUnderCursor.CurrentJob;
            }
            else
            {
                BuildingUnderCursor = BuildingNoFilterUnderCursor;

                if ( BuildingUnderCursor != null && BuildingUnderCursor.MachineStructureInBuilding != null )
                    StructureUnderCursor = BuildingUnderCursor.MachineStructureInBuilding;
                else
                    StructureUnderCursor = null;

                if ( StructureUnderCursor != null && InputCaching.IsInInspectMode_Any )
                    Engine_HotM.HoveredJob = StructureUnderCursor.CurrentJob;

                #region If No Building Under Cursor But Actor Selected, Check Via Inflation
                if ( BuildingUnderCursor == null && selectedMachineActor != null &&
                    !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow )
                {
                    Vector3 destinationPoint = Engine_HotM.MouseWorldHitLocation;
                    MapCell cell = null;
                    if ( destinationPoint.x == float.NegativeInfinity || destinationPoint.x == float.PositiveInfinity )
                    { } //invalid location
                    else
                        cell = CityMap.TryGetWorldCellAtCoordinates( destinationPoint );
                    if ( cell != null )
                    {
                        foreach ( MapItem bld in cell.BuildingList.GetDisplayList() )
                        {
                            if ( bld == null )
                                continue;
                            if ( !bld.OBBCache.HasBeenSet )
                                bld.FillOBBCache();

                            if ( bld.OBBCache.GetOuterRect().ContainsPointUsingExtraPadding(
                                destinationPoint.x, destinationPoint.z, 0.1 ) ) //this extra padding makes it so that collisions on the side of the building are not possible
                            {
                                BuildingUnderCursor = bld.SimBuilding;
                                break;
                            }
                        }
                    }
                }
                #endregion
            }
            #endregion

            bool alreadyTriedMachineInteractionMode = false;

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                    if ( Engine_HotM.SelectedMachineActionMode == null )
                    {
                        if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor)
                        {
                            if ( machineActor.IsInAbilityTypeTargetingMode != null && 
                                !machineActor.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !machineActor.IsInAbilityTypeTargetingMode.IsMixedTargetingMode &&
                                !( !InputCaching.UseLeftClickForVehicleAndMechMovement && machineActor.IsInAbilityTypeTargetingMode.IfVehicleOrMechEnablesMovement) )
                            {
                                //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear action mode
                                //    machineActor.SetTargetingMode( null, null );
                                //else
                                    MachineMovePlanner.Instance?.HandleActionTypeMouseInteractionsAndAnyExtraRendering();
                                return;//skip any normal handling, because we're in a non-soft targeting mode
                            }
                        }
                    }
                    if ( InputCaching.IsInInspectMode_Any )
                    {
                        if ( BuildingUnderCursor != null )
                        {
                            CursorHelper.RenderSpecificMouseCursor( true, IconRefs.MouseInspect );

                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //inspect
                               Inspect( BuildingUnderCursor );
                        }
                        else
                        {
                            if ( StructureUnderCursor != null )
                            {
                                RenderWouldSelectTooltipAndCursor( StructureUnderCursor );
                                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                    Engine_HotM.SetSelectedActor( StructureUnderCursor, true, true, true );
                            }
                            else if ( ActorUnderCursor != null )
                            {
                                RenderWouldSelectTooltipAndCursor( ActorUnderCursor );
                                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                                    Engine_HotM.SetSelectedActor( ActorUnderCursor, true, true, true );
                            }
                        }

                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            BasicLeftClickHandler( true );
                        return;
                    }
                    else
                    {
                        if ( Engine_HotM.SelectedMachineActionMode != null && !alreadyTriedMachineInteractionMode )
                        {
                            alreadyTriedMachineInteractionMode = true;
                            if ( Engine_HotM.SelectedMachineActionMode.Implementation.HandleActionModeMouseInteractionsAndAnyExtraRendering( Engine_HotM.SelectedMachineActionMode ) )
                                return;
                        }

                        if ( Engine_HotM.SelectedActor is ISimMachineActor )
                        { } //skip if machine actor selected
                        else if ( Engine_HotM.SelectedActor is ISimNPCUnit npcUnit && npcUnit.UnitType.CostsToCreateIfBulkAndroid.Count > 0 )
                        { } //skip if bulk android selected
                        else if ( BuildingUnderCursor != null )
                        {
                            StreetSenseDataAtBuilding streetSenseDataOrNull = BuildingUnderCursor.GetCurrentStreetSenseActionThatShouldShowOnMap();
                            LocationActionType actionToTake = streetSenseDataOrNull?.ActionType;
                            if ( actionToTake != null ) //bulk androids cannot do actions of this sort at all
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( actionToTake ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.ShouldTooltipBeRed = true;
                                    atMouse.Icon = actionToTake.Icon;
                                    atMouse.TitleUpperLeft.AddRaw( actionToTake.GetDisplayName() );

                                    bool hasDescription = !actionToTake.GetDescription().IsEmpty();
                                    if ( hasDescription )
                                        atMouse.CanExpand = CanExpandType.Brief;

                                    actionToTake.Implementation.TryHandleLocationAction( null, BuildingUnderCursor, BuildingUnderCursor.GetMapItem().CenterPoint, actionToTake, 
                                        streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                        streetSenseDataOrNull?.OtherOptionalID, ActionLogic.AppendToTooltip, out bool IncludeRestrictedAreaNoticeInTooltip, 0 );

                                    atMouse.Main.AddLang( "SelectAUnitToDoThisAction", ColorTheme.RedOrange2 );

                                    if ( hasDescription && InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        atMouse.FrameTitle.AddLang( "Move_ActionDetails" );

                                        if ( !actionToTake.GetDescription().IsEmpty() )
                                            atMouse.FrameBody.AddRaw( actionToTake.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                        if ( !actionToTake.StrategyTipOptional.Text.IsEmpty() )
                                            atMouse.FrameBody.AddRaw( actionToTake.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                    }
                                }

                                return;
                            }
                        }

                        if ( StructureUnderCursor != null )
                        {
                            RenderWouldSelectTooltipAndCursor( ActorUnderCursor );
                            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            {
                                if ( StructureUnderCursor != null )
                                    Engine_HotM.SetSelectedActor( StructureUnderCursor, false, true, true );
                            }
                        }

                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            BasicLeftClickHandler( true );
                    }
                    break;
                case MainGameMode.CityMap:
                    {
                        if ( Engine_HotM.SelectedMachineActionMode == null )
                        {
                            if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor )
                            {
                                if ( machineActor.IsInAbilityTypeTargetingMode != null && 
                                    !machineActor.IsInAbilityTypeTargetingMode.IsSoftTargetingMode && !machineActor.IsInAbilityTypeTargetingMode.IsMixedTargetingMode &&
                                    !(!InputCaching.UseLeftClickForVehicleAndMechMovement && machineActor.IsInAbilityTypeTargetingMode.IfVehicleOrMechEnablesMovement) )
                                {
                                    //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear action mode
                                    //    machineActor.SetTargetingMode( null, null );
                                    //else
                                        MachineMovePlanner.Instance?.HandleActionTypeMouseInteractionsAndAnyExtraRendering();
                                    return; //skip any normal handling, because we're in a non-soft targeting mode
                                }
                            }
                        }

                        if ( Engine_HotM.SelectedActor is ISimMachineActor )
                        { } //skip if machine actor selected
                        else if ( Engine_HotM.SelectedActor is ISimNPCUnit npcUnit && npcUnit.UnitType.CostsToCreateIfBulkAndroid.Count > 0 )
                        { } //skip if bulk android selected
                        else if ( BuildingUnderCursor != null )
                        {
                            StreetSenseDataAtBuilding streetSenseDataOrNull = BuildingUnderCursor.GetCurrentStreetSenseActionThatShouldShowOnMap();
                            LocationActionType actionToTake = streetSenseDataOrNull?.ActionType;
                            if ( actionToTake != null ) //bulk androids cannot do actions of this sort at all
                            {
                                NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                                if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( actionToTake ), null, SideClamp.Any,
                                    InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
                                {
                                    atMouse.ShouldTooltipBeRed = true;
                                    atMouse.Icon = actionToTake.Icon;
                                    atMouse.TitleUpperLeft.AddRaw( actionToTake.GetDisplayName() );

                                    bool hasDescription = !actionToTake.GetDescription().IsEmpty();
                                    if ( hasDescription )
                                        atMouse.CanExpand = CanExpandType.Brief;

                                    actionToTake.Implementation.TryHandleLocationAction( null, BuildingUnderCursor, BuildingUnderCursor.GetMapItem().CenterPoint, actionToTake, 
                                        streetSenseDataOrNull?.EventOrNull, streetSenseDataOrNull?.ProjectItemOrNull,
                                        streetSenseDataOrNull?.OtherOptionalID, ActionLogic.AppendToTooltip, out bool IncludeRestrictedAreaNoticeInTooltip, 0 );

                                    atMouse.Main.AddLang( "SelectAUnitToDoThisAction", ColorTheme.RedOrange2 );

                                    if ( hasDescription && InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        atMouse.FrameTitle.AddLang( "Move_ActionDetails" );

                                        if ( !actionToTake.GetDescription().IsEmpty() )
                                            atMouse.FrameBody.AddRaw( actionToTake.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                        if ( !actionToTake.StrategyTipOptional.Text.IsEmpty() )
                                            atMouse.FrameBody.AddRaw( actionToTake.StrategyTipOptional.Text, ColorTheme.PurpleDim ).Line();
                                    }
                                }

                                return;
                            }
                        }

                        if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                        {
                            if ( ActorUnderCursor != null )
                            {
                                if ( ActorUnderCursor != Engine_HotM.SelectedActor ) //select
                                {
                                    Engine_HotM.SetSelectedActor( ActorUnderCursor, false, true, true );
                                    if ( Engine_HotM.SelectedMachineActionMode != null )
                                    {
                                        if ( Engine_HotM.SelectedMachineActionMode.Implementation.GetShouldCloseOnUnitSelection( Engine_HotM.SelectedMachineActionMode ) )
                                            Engine_HotM.SelectedMachineActionMode = null;
                                    }
                                }
                                else //that unit is already selected
                                {
                                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        VisCommands.ToggleCityMapMode();
                                }
                                return;
                            }

                            if ( StructureUnderCursor != null )
                            {
                                if ( StructureUnderCursor != Engine_HotM.SelectedActor ) //select
                                    Engine_HotM.SetSelectedActor( StructureUnderCursor, false, true, true );
                                else //that unit is already selected
                                {
                                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                                        VisCommands.ToggleCityMapMode();
                                }
                                return;
                            }
                            if ( BuildingUnderCursor != null )
                            {
                                if ( InputCaching.IsInInspectMode_Any )
                                {
                                    Inspect( BuildingUnderCursor );
                                    return;
                                }
                            }

                            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
                            if ( mousePoint.x == float.NegativeInfinity || mousePoint.x == float.PositiveInfinity )
                                return;
                            if ( mousePoint.x < CityMap.MinX_Clamped ||
                                mousePoint.x > CityMap.MaxX_Clamped ||
                                mousePoint.z < CityMap.MinZ_Clamped ||
                                mousePoint.z > CityMap.MaxZ_Clamped )
                                return; //clicked out of bounds, so nevermind

                            //if there was a valid point, first switch to streets view...
                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                            CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                            //...then jump to the position instantly
                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( mousePoint, true );
                            return;
                        }
                    }
                    break;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                case MainGameMode.CityMap:
                    if ( selectedMachineActor != null ) //&& !selectedMachineActor.GetIsBlockedFromActingInGeneral() ) do it anyway!
                    {
                        //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //nevermind
                        //{
                        //    if ( VisCommands.TryDeselectCurrentActorOrOtherwiseStepBackOne() )
                        //        return;
                        //}

                        if ( Engine_HotM.SelectedMachineActionMode == null )
                        {
                            MachineMovePlanner.Instance?.HandleActionTypeMouseInteractionsAndAnyExtraRendering();
                            return;
                        }
                    }
                    else if ( Engine_HotM.SelectedActor is ISimNPCUnit npcUnit && npcUnit.GetIsPlayerControlled() )
                    {
                        //if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //nevermind
                        //{
                        //    if ( VisCommands.TryDeselectCurrentActorOrOtherwiseStepBackOne() )
                        //        return;
                        //}

                        if ( Engine_HotM.SelectedMachineActionMode == null )
                        {
                            MachineMovePlanner.Instance?.HandleActionTypeMouseInteractionsAndAnyExtraRendering();
                            return;
                        }
                    }
                    break;
            }

            if ( Engine_HotM.SelectedMachineActionMode != null && !alreadyTriedMachineInteractionMode )
            {
                alreadyTriedMachineInteractionMode = true;
                if ( Engine_HotM.SelectedMachineActionMode.Implementation.HandleActionModeMouseInteractionsAndAnyExtraRendering( Engine_HotM.SelectedMachineActionMode ) )
                    return;
            }

            HandleMouseInteractionsWhenNoMode();
        }
        #endregion

        #region GetShouldSkipOutOfRangeNotice
        public static bool GetShouldSkipOutOfRangeNotice( Vector3 destinationPoint )
        {
            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                return false;
            return true;
        }
        #endregion

        #region HandleMouseInteractionsWhenNoMode
        public static void HandleMouseInteractionsWhenNoMode()
        {
            if ( VisCurrent.IsInPhotoMode )
                return;

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                    HandleStreets();
                    break;
                case MainGameMode.CityMap:
                    HandleCityMap();
                    break;
                case MainGameMode.TheEndOfTime:
                    //todo zodiac and end of time
                    break;
            }
        }
        #endregion

        #region HandleStreets
        private static void HandleStreets()
        {
            if ( VisCurrent.IsShowingActualEvent )
                return;

            if ( !TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() ) //if any of the center-screen tooltips drew, this will be true
            {
                if ( ActorUnderCursor != null )
                    RenderWouldSelectTooltipAndCursor( ActorUnderCursor );
                else if ( StructureUnderCursor != null )
                    RenderWouldSelectTooltipAndCursor( StructureUnderCursor );
            }

            #region Left-Clicked
            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
            {
                if ( ActorUnderCursor != null ) //if clicking an actor, just select them
                {
                    Engine_HotM.SetSelectedActor( ActorUnderCursor, false, true, true );
                    return;
                }

                if ( StructureUnderCursor != null ) //if clicking a machine structure, just select them
                {
                    Engine_HotM.SetSelectedActor( StructureUnderCursor, false, true, true );
                    return;
                }
            }
            #endregion
        }
        #endregion

        #region BasicLeftClickHandler
        public static void BasicLeftClickHandler( bool AlsoClearGameModeIfSelectingNew )
        {
            ISimMapActor actorMousingOver = MouseHelper.ActorUnderCursor;
            if ( actorMousingOver != null )
            {
                if ( actorMousingOver != Engine_HotM.SelectedActor ) //select
                {
                    Engine_HotM.SetSelectedActor( actorMousingOver, false, true, true );
                    if ( AlsoClearGameModeIfSelectingNew )
                    {
                        if ( Engine_HotM.SelectedMachineActionMode != null )
                        {
                            if ( Engine_HotM.SelectedMachineActionMode.Implementation.GetShouldCloseOnUnitSelection( Engine_HotM.SelectedMachineActionMode ) )
                                Engine_HotM.SelectedMachineActionMode = null;
                        }
                    }

                }
                else //that unit is already selected
                {
                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                        VisCommands.ToggleCityMapMode();
                }
                return;
            }
            else
            {
                MachineStructure structureUnder = StructureUnderCursor;
                if ( structureUnder != null ) //if clicking a machine structure, just select them unless already selected
                {
                    if ( actorMousingOver != Engine_HotM.SelectedActor ) //select
                    {
                        Engine_HotM.SetSelectedActor( structureUnder, false, true, true );
                        if ( AlsoClearGameModeIfSelectingNew )
                        {
                            if ( Engine_HotM.SelectedMachineActionMode != null )
                            {
                                if ( Engine_HotM.SelectedMachineActionMode.Implementation.GetShouldCloseOnUnitSelection( Engine_HotM.SelectedMachineActionMode ) )
                                    Engine_HotM.SelectedMachineActionMode = null;
                            }
                        }
                    }
                    else //that structure is already selected
                    {
                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                            VisCommands.ToggleCityMapMode();
                    }
                    return;
                }

                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.CityMap:
                        {
                            Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
                            if ( mousePoint.x == float.NegativeInfinity || mousePoint.x == float.PositiveInfinity )
                                return;
                            if ( mousePoint.x < CityMap.MinX_Clamped ||
                                mousePoint.x > CityMap.MaxX_Clamped ||
                                mousePoint.z < CityMap.MinZ_Clamped ||
                                mousePoint.z > CityMap.MaxZ_Clamped )
                                return; //clicked out of bounds, so nevermind

                            //if there was a valid point, first switch to streets view...
                            Engine_HotM.SetGameMode( MainGameMode.Streets );
                            CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                            //...then jump to the position instantly
                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( mousePoint, true );
                        }
                        break;
                    case MainGameMode.Streets:
                        {
                            if ( Engine_HotM.SelectedActor != null ) //deselect
                                Engine_HotM.SetSelectedActor( null, false, true, true );
                            //Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
                            //if ( mousePoint.x == float.NegativeInfinity || mousePoint.x == float.PositiveInfinity )
                        }
                        break;
                }
            }
        }
        #endregion

        #region HandleCityMap
        private static void HandleCityMap()
        {
            if ( VisCurrent.IsShowingActualEvent )
                return;

            if ( !TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() ) //if any of the center-screen tooltips drew, this will be true
            {
                if ( ActorUnderCursor != null )
                {
                    RenderWouldSelectTooltipAndCursor( ActorUnderCursor );
                }
                else if ( StructureUnderCursor != null )
                {
                    RenderWouldSelectTooltipAndCursor( StructureUnderCursor );
                }
            }

            if ( !TooltipRefs.AtMouseBasicNovel.GetWasAlreadyDrawnThisFrame() && !TooltipRefs.LowerLeftBasicNovel.GetWasAlreadyDrawnThisFrame() )
            {
                ArcenGroundPoint worldCellPoint = Engine_HotM.CalculateWorldCellPointUnderMouse();
                MapCell cellUnderMouse = CityMap.TryGetExistingCellAtLocation( worldCellPoint );

                if ( cellUnderMouse != null )
                    RenderMapCellTooltip( cellUnderMouse );
            }

            #region Left-Clicked
            if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
            {
                if ( ActorUnderCursor != null ) //if clicking an actor, just select them
                {
                    if ( ActorUnderCursor != Engine_HotM.SelectedActor ) //select
                        Engine_HotM.SetSelectedActor( ActorUnderCursor, false, true, true );
                    else //that unit is already selected
                    {
                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                            VisCommands.ToggleCityMapMode();
                    }
                    return;
                }

                if ( StructureUnderCursor != null ) //if clicking a machine structure, just select them
                {
                    if ( StructureUnderCursor != Engine_HotM.SelectedActor ) //select
                        Engine_HotM.SetSelectedActor( StructureUnderCursor, false, true, true );
                    else //that structure is already selected
                    {
                        if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                            VisCommands.ToggleCityMapMode();
                    }
                    return;
                }

                //if not clicking an actor
                Vector3 mousePoint = Engine_HotM.MouseWorldHitLocation;
                if ( mousePoint.x == float.NegativeInfinity || mousePoint.x == float.PositiveInfinity )
                    return;
                if ( mousePoint.x < CityMap.MinX_Clamped ||
                    mousePoint.x > CityMap.MaxX_Clamped ||
                    mousePoint.z < CityMap.MinZ_Clamped ||
                    mousePoint.z > CityMap.MaxZ_Clamped )
                    return; //clicked out of bounds, so nevermind

                //if there was a valid point, first switch to streets view...
                Engine_HotM.SetGameMode( MainGameMode.Streets );

                if ( Engine_HotM.SelectedActor != null )
                {
                    //if the player has an actor selected that does not allow for lateral camera movement right now, then deselect that
                    if ( MachineMovePlanner.Instance?.GetShouldCameraLateralMovementBeAllowed() ?? false )
                        Engine_HotM.SetSelectedActor( null, false, true, true );
                }

                CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();

                //...then jump to the position instantly
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( mousePoint, true );

            }
            #endregion
        }
        #endregion

        #region TryHandleEndOfTimeCreatorMode
        private static bool TryHandleEndOfTimeCreatorMode()
        {
            if ( (SimMetagame.CurrentEndOfTimeLens?.ID??string.Empty) != "Creator" )
                return false;

            A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
            if ( place )
            {
                EndOfTimeItem endItem = place.GetEndOfTimeItem();
                if ( endItem != null )
                {
                    EndOfTimeSubItemPosition pos = endItem.GetNearestSubObjectToPoint( Engine_HotM.MouseWorldHitLocation, 5f ); //smaller than this and some will be missed
                    if ( pos.SlotIndex >= 0 )
                    {
                        if ( (endItem?.Type?.ExtraPlaceableData?.IsZiggurat ?? true) && SimMetagame.CurrentChapterNumber < 4 && SimMetagame.IntelligenceClass < 7 )
                        {
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );

                            NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                            if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( "EOTPlacement", "IsZiggurat" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                            {
                                atMouse.CanExpand = CanExpandType.No;
                                atMouse.Icon = SimMetagame.CurrentEndOfTimeLens.Icon;
                                atMouse.ShouldTooltipBeRed = true;
                                atMouse.TitleUpperLeft.AddLang( "CannotCallTimelinesHereYet_Header" );
                                atMouse.Main.AddLang( "CannotCallTimelinesHereYet_ZigguratBody" );
                            }

                            return true;
                        }

                        if ( SimMetagame.AllTimelines.Count < 4 && endItem != EndOfTimeMap.RockOutcrops.FirstOrDefault )
                        {
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );

                            NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                            if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( "EOTPlacement", "IsZiggurat" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                            {
                                atMouse.CanExpand = CanExpandType.No;
                                atMouse.Icon = SimMetagame.CurrentEndOfTimeLens.Icon;
                                atMouse.ShouldTooltipBeRed = true;
                                atMouse.TitleUpperLeft.AddLang( "CannotCallTimelinesHereYet_Header" );
                                atMouse.Main.AddLang( "CannotCallTimelinesHereYet_LimitedToFirstRock" );
                            }

                            return true;
                        }

                        CityTimeline existingTimeline = endItem.GetCityAtSubObjectIndex( pos.SlotIndex );
                        if ( existingTimeline != null )
                        {
                            //nothing to do, already a city there
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return true;
                        }
                        else
                        {
                            RenderManager_TheEndOfTime.TryDrawTimelineGhost( pos, CommonRefs.EndOfTimeCityGhost );
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );

                            if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ||
                                ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                            {
                                if ( ResourceRefs.Aetagest.Current < 100 )
                                {
                                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                }
                                else
                                {
                                    Window_NewTimeline.CreateOnItem = endItem;
                                    Window_NewTimeline.CreateOnItemAtIndex = pos.SlotIndex;
                                    Window_CheatWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                                    Window_NewTimeline.Instance.Open();
                                }
                            }
                        }

                        return true;
                    }
                    else
                    {
                        CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                        return false;
                    }
                }
                else
                    return false;
            }
            else
                return false;
        }
        #endregion

        #region Inspect
        /// <summary>
        /// Tell me about a given building
        /// </summary>
        public static void Inspect( ISimBuilding Building )
        {
            Engine_Universal.NonModalPopupsSidePrimary.Clear(); //keep these from stacking up on repeat clicks

            ModalPopupData.CreateAndLogSelfUpdatingOKStyle( PopupSizeStyle.TallSidePrimary, null, LocalizedString.AddRaw_New( Building.GetDisplayName() ),
            LangCommon.Popup_Common_Close.LocalizedString, Building.GetMapItem(), delegate ( ArcenDoubleCharacterBuffer Buffer, string[] TooltipLinkData )
            {
                if ( TooltipLinkData != null && TooltipLinkData.Length > 0 )
                    Building.WriteWorldExamineDetails_SubTooltipLinkHover( TooltipLinkData );
                else
                {
                    Building.WriteWorldExamineDetails( Buffer );
                }
            }, 1f,
            delegate ( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                Building.WriteWorldExamineDetails_SubTooltipLinkClick( Input, TooltipLinkData );
                return MouseHandlingResult.None;
            } );
        }
        #endregion

        #region CanTriggerBuildingHighlights
        public static bool CanTriggerBuildingHighlights()
        {
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                case MainGameMode.CityMap:
                    if ( InputCaching.IsInInspectMode_Any )
                        return true;
                    if ( Engine_HotM.SelectedActor is ISimMachineActor Machine )
                    {
                        if ( !Machine.GetIsBlockedFromActingInGeneral() )
                        {
                            //we are in move mode, allow highlight
                            return true;
                        }
                        return false; //no highlight
                    }
                    break;
            }
            return false;
        }
        #endregion

        #region TriggerNumberedAbility
        public static void TriggerNumberedAbility( int Index, TriggerStyle Style )
        {
            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                return;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                case MainGameMode.CityMap:
                    if ( Engine_HotM.SelectedMachineActionMode != null )
                    {
                        Engine_HotM.SelectedMachineActionMode.Implementation.TriggerSlotNumber( Index );
                        break;
                    }
                    if ( Window_VehicleUnitPanel.Instance.IsOpen )
                    {
                        Window_VehicleUnitPanel.Instance.TriggerSlotIndex( Index, Style );
                    }
                    else
                    {
                        if ( Engine_HotM.SelectedActor is ISimMachineActor Machine )
                            Machine.TryPerformActorAbilityInSlot( Index, Style );
                    }
                    break;
            }
        }
        #endregion

        #region RenderWouldSelectTooltipAndCursor
        public static void RenderWouldSelectTooltipAndCursor( ISimMapActor actor )
        {
            if ( actor == null ) 
                return;
            if ( TooltipRefs.AtMouseSmallerNovel.GetWasAlreadyDrawnThisFrame() )
                return;

            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.WillSelectTarget, MathRefs.WillSelectScalingRangeMin.FloatMin, MathRefs.WillSelectScalingRangeMax.FloatMin,
                MathRefs.WillSelectScalingMultiplierMax.FloatMin );

            RenderHoveredActor_InnerDetails( actor );
        }
        #endregion

        #region RenderWouldSelectTooltipAndCursor
        public static void RenderHoveredActor_InnerDetails( ISimMapActor actor )
        {
            if ( actor == null )
                return;

            bool buildModeActiveAndTargeting = BuildModeHandler.IsActiveAndTargeting;

            if ( !InputCaching.IsInInspectMode_Any && buildModeActiveAndTargeting )
            {
                if ( actor is MachineStructure structure )
                {
                    if ( BuildModeHandler.TargetingJob != null )
                    {
                        if ( structure.CurrentJob == BuildModeHandler.TargetingJob )
                            return;
                    }
                    else
                    {
                        if ( BuildModeHandler.TargetingStructure != null && structure.Type == BuildModeHandler.TargetingStructure )
                            return;
                    }
                }
            }

            NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

            if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( actor ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
            {
                atMouse.CanExpand = CanExpandType.No;
                atMouse.Icon = actor.GetShapeIcon();

                int currentMentalEnergy = (int)ResourceRefs.MentalEnergy.Current;

                if ( actor is ISimMachineActor machine )
                {
                    int currentAP = machine.CurrentActionPoints;

                    if ( currentMentalEnergy <= 0 )
                        atMouse.ShouldTooltipBeRed = true; //unable to do anything
                    else if ( currentAP <= 0 && currentMentalEnergy < 2 )
                        atMouse.ShouldTooltipBeRed = true; //unable to reload

                    atMouse.TitleUpperLeft.AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.HeaderGoldMoreRich )
                        .AddRaw( machine.CurrentActionPoints.ToString(), currentAP > 0 ? string.Empty : ColorTheme.RedOrange2 );
                    atMouse.TitleUpperLeft.Space2x().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                        .AddRaw( ResourceRefs.MentalEnergy.Current.ToString(), ResourceRefs.MentalEnergy.Current > 0 ? string.Empty : ColorTheme.RedOrange2 );

                    ISimBuilding currentBuilding = (machine as ISimMachineUnit)?.ContainerLocation.Get() as ISimBuilding;
                    Vector3 currentSpot = machine.GetDrawLocation();
                    bool hasCombatLines = CombatTextHelper.GetHasAnyPredictionsToShow( machine );
                    bool hasRestrictedArea = CombatTextHelper.GetIsMovingToRestrictedArea( machine, currentBuilding, currentSpot, true );

                    if ( hasCombatLines || hasRestrictedArea )
                    {
                        if ( machine.CurrentActionOverTime != null )
                            atMouse.MainHeader.Space2x().AddLangAndAfterLineItemHeader( "Busy_Short", ColorTheme.RedOrange2 ).AddRaw( machine.CurrentActionOverTime.Type.GetDisplayName() );

                        if ( InputCaching.ShouldShowDetailedTooltips && atMouse.TooltipWidth < 310 )
                            atMouse.TooltipWidth = 310;

                        if ( hasCombatLines && !InputCaching.ShouldShowDetailedTooltips )
                        {
                            CombatTextHelper.AppendLastPredictedDamageBrief( machine, atMouse.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );
                            //if ( hasRestrictedArea )
                            //    CombatTextHelper.AppendRestrictedAreaShort( machine, novel.MainHeader, false, currentBuilding, currentSpot, true );
                        }
                        else
                        {
                            if ( hasCombatLines )
                            {
                                CombatTextHelper.AppendLastPredictedDamageBrief( machine, atMouse.TitleUpperLeft, TTTextBefore.SpacingIfNotEmpty, TTTextAfter.None );

                                CombatTextHelper.AppendLastPredictedDamageLongSecondary( machine,
                                    InputCaching.ShouldShowDetailedTooltips, false, false, true );

                                if ( hasRestrictedArea )
                                    CombatTextHelper.AppendRestrictedAreaShort( machine, atMouse.Main,
                                        InputCaching.ShouldShowDetailedTooltips, currentBuilding, currentSpot, true );
                            }
                            else
                            {
                                if ( hasRestrictedArea && InputCaching.ShouldShowDetailedTooltips )
                                {
                                    CombatTextHelper.AppendRestrictedAreaLong( machine, atMouse.Main, atMouse.Main,
                                        InputCaching.ShouldShowDetailedTooltips, currentBuilding, currentSpot, true );
                                }
                            }
                        }

                        atMouse.CanExpand = CanExpandType.Brief;
                    }
                    else
                    {
                        if ( machine.CurrentActionOverTime != null )
                            atMouse.TitleUpperLeft.Space2x().AddLangAndAfterLineItemHeader( "Busy_Short", ColorTheme.RedOrange2 ).AddRaw( machine.CurrentActionOverTime.Type.GetDisplayName() );
                    }
                }
                else if ( actor is ISimNPCUnit npc )
                {
                    //if ( npc.Stance != null )
                    //    novel.Icon = npc.Stance.Icon;

                    atMouse.TitleUpperLeft.AddRaw( npc.UnitType.GetDisplayName() );
                }
                else if ( actor is MachineStructure structure )
                {
                    atMouse.TitleUpperLeft.AddRaw( structure.GetDisplayName() );

                    if ( buildModeActiveAndTargeting )
                        atMouse.MainHeader.AddFormat1( "HoldToSeeDetails", InputCaching.GetGetHumanReadableKeyComboForInspectMode(), ColorTheme.SoftGold );
                }
            }
        }
        #endregion

        #region RenderBuildingTooltip
        public static void RenderBuildingTooltip( ISimBuilding building )
        {
            if ( building == null ) return;

            MachineStructure machineStructure = building.MachineStructureInBuilding;
            MachineJob job = machineStructure?.CurrentJob;
            if ( job != null )
            {
                job.RenderJobTooltip( null, SideClamp.Any, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, machineStructure, TooltipExtraText.None, TooltipExtraRules.None );
                return;
            }

            LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

            if ( lowerLeft.TryStartSmallerTooltip( TooltipID.Create( building ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
            {
                lowerLeft.TitleUpperLeft.AddLangAndAfterLineItemHeader( "MapBuilding", ColorTheme.Gray ).AddRaw( building.GetDisplayName() );

                lowerLeft.MainHeader.AddLangAndAfterLineItemHeader( "MapDistrict", ColorTheme.Gray );
                MapDistrict district = building.GetParentDistrict();
                lowerLeft.MainHeader.AddRaw( district == null ? Lang.Get( "BuildingOwner_None" ) : district.DistrictName );

                //lowerLeft.Main.Add( "\nDiffs: " );
                //MapCell cell = building.GetParentCell();
                //if ( !cell.Draw_District.SameDistrictToN )
                //    lowerLeft.Main.Add( "N" );
                //if ( !cell.Draw_District.SameDistrictToS )
                //    lowerLeft.Main.Add( "S" );
                //if ( !cell.Draw_District.SameDistrictToE )
                //    lowerLeft.Main.Add( "E" );
                //if ( !cell.Draw_District.SameDistrictToW )
                //    lowerLeft.Main.Add( "W" );

                BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();

                if ( Engine_HotM.IsInVisualizationMode && effectiveOverlayType?.Implementation != null )
                {
                    effectiveOverlayType.Implementation.WriteExtraTooltipInformation( lowerLeft.Main, building, effectiveOverlayType );
                }
                // Engine_HotM.IsInVisualizationMode returns false for BuildApplianceModeOverlay
                // but we still have some info we want to write
                else if ( effectiveOverlayType?.Implementation != null
                    && effectiveOverlayType.ID == "BuildApplianceModeOverlay" )
                {
                    effectiveOverlayType.Implementation.WriteExtraTooltipInformation( lowerLeft.Main, building, effectiveOverlayType );
                }

                MapItem mItem = building.GetMapItem();
                if ( mItem != null && mItem.DebugMessageForTooltip != null && mItem.DebugMessageForTooltip.Length > 0 )
                {
                    if ( !lowerLeft.Main.GetIsEmpty() )
                        lowerLeft.Main.Line();

                    lowerLeft.Main.AddRaw( mItem.DebugMessageForTooltip );
                }
            }
        }
        #endregion

        #region RenderMapCellTooltip
        public static bool RenderMapCellTooltip( MapCell Cell )
        {
            if ( Cell == null ) 
                return false;

            MapDistrict district = Cell.ParentTile?.District;
            if ( district == null )
                return false;

            MapPOI poi = Cell.ParentTile?.POIOrNull;

            if ( poi != null )
            {
                poi.RenderPOITooltip( null, SideClamp.Any, TooltipShadowStyle.None, false );
                return true;
            }
            else
            {
                district.RenderBriefMapTooltipLowerLeft( null, SideClamp.Any );
                return true;
            }

            //BuildingOverlayType effectiveOverlayType = Engine_HotM.GetEffectiveOverlayType();

            //if ( Engine_HotM.IsInVisualizationMode && effectiveOverlayType?.Implementation != null )
            //{
            //    effectiveOverlayType.Implementation.WriteExtraTooltipInformation( tooltipBuffer, building, effectiveOverlayType );
            //}

            //// Engine_HotM.IsInVisualizationMode returns false for BuildApplianceModeOverlay
            //// but we still have some info we want to write
            //else if ( effectiveOverlayType?.Implementation != null
            //         && effectiveOverlayType.ID == "BuildApplianceModeOverlay" )
            //{
            //    effectiveOverlayType.Implementation.WriteExtraTooltipInformation( tooltipBuffer, building, effectiveOverlayType );
            //}
        }
        #endregion

        #region AppendIconToString
        public static ArcenDoubleCharacterBuffer AppendIconToString( ArcenDoubleCharacterBuffer Buffer, IA5Sprite Sprite,
            ISimBuilding BuildingOrNull, VisIconUsage Usage )
        {
            if ( !Buffer.GetIsEmpty() )
                Buffer.Line();
            Buffer.AddSpriteStyled_NoIndent( Sprite, AdjustedSpriteStyle.InlineSmaller095,
                BuildingUnderCursor == BuildingOrNull ? Usage.HoverDefaultColorHexWithHDRHex : Usage.DefaultColorHexWithHDRHex );
            return Buffer;
        }
        #endregion

        #region RenderSpecificUIMouseCursorAtMouse
        public static void RenderSpecificUIMouseCursorAtMouse( VisIconUsage Usage )
        {
            Window_AtMouseIcon.AtMouseIcon.Instance.SetIconAndColor( Usage.Icon, Usage.DefaultColorHDR );
        }
        #endregion
    }
}

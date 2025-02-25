using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class BuildModeHandler : IMachineActionModeImplementation
    {
        public static MachineBuildModeCategory currentCategory = null;
        public static MachineStructureType TargetingStructure;
        public static MachineJob TargetingJob;
        public static Investigation TargetingTerritoryControlInvestigation = null;

        public static bool IsActive
        {
            get { return (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "BuildMode"; }
        }

        public static bool IsActiveAndTargeting
        {
            get 
            { 
                if ( TargetingStructure != null || TargetingJob  != null || TargetingTerritoryControlInvestigation != null )
                    return (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "BuildMode";
                return false;
            }
        }

        public static void ClearAllTargeting()
        {
            TargetingStructure = null;
            TargetingJob = null;
            TargetingTerritoryControlInvestigation = null;

            ClearLastValidSpot();
        }

        private static void ClearLastValidSpot()
        {
            lastValidSpot_HadOne = false;
            lastValidSpot_StructureType = null;
        }

        #region HandleCancelButton
        public bool HandleCancelButton()
        {
            if ( TargetingStructure != null || TargetingJob != null || TargetingTerritoryControlInvestigation != null )
            {
                TargetingStructure = null;
                TargetingJob = null;
                TargetingTerritoryControlInvestigation = null;
                return true;
            }

            Engine_HotM.SelectedMachineActionMode = null;
            return true;
        }
        #endregion

        public static MachineStructureType LastHoveredStructureType = null;
        public static MachineJob LastHoveredJob = null;
        public static float HoverExpireTime = 0;

        public void FlagAllRelatedResources( MachineActionMode Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "BuildMode" )
                return;

            //currentCategory nothing to do?

            TargetingStructure?.FlagAllRelatedResources();
            TargetingJob?.FlagAllJobRelatedResources();
            TargetingTerritoryControlInvestigation?.FlagAllRelatedResources();
        }

        public bool GetShouldCloseOnUnitSelection( MachineActionMode Mode )
        {
            return true;
        }

        public bool HandleActionModeMouseInteractionsAndAnyExtraRendering( MachineActionMode Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "BuildMode" )
            {
                Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                return false;
            }

            bool useLeftBuildStyle = InputCaching.UseLeftClickForBuildingConstruction;

            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null )
            {
                switch ( lowerMode.ID )
                {
                    case "ZodiacNeuronScene":
                        Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                        Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                        break;
                }
                return false;
            }
            else
            {
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.TheEndOfTime:
                        Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                        Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                        Engine_HotM.SelectedMachineActionMode = null;
                        return false;
                }
            }

            if ( Engine_HotM.CurrentEmbeddedStructureTypeToFocus != null )
                HandleRenderEmbeddedBuildings();

            #region Hovered Only
            if ( ArcenTime.AnyTimeSinceStartF < HoverExpireTime )
            {
                if ( !Engine_Universal.IsMouseOverGUI ) //if mouse no longer over the gui, then expire it
                    HoverExpireTime = 0;
            }

            if ( ArcenTime.AnyTimeSinceStartF < HoverExpireTime )
            {
                MachineStructureType structureType = LastHoveredStructureType;
                if ( structureType != null && structureType.DuringGame_IsUnlocked() )
                {
                    if ( structureType.IsTheDeleteFunction )
                        return this.HandleDeleteStructureMode( true );
                    else if ( structureType.IsThePauseFunction )
                        return this.HandlePauseStructureMode( true );
                    else if ( structureType.IsBuiltOnSiteOfExistingBuilding )
                        return this.HandleEmbeddedStructureType( structureType, null, true );
                    else
                        return false; //nothing to do with hovering freestanding structures
                }
                else
                {
                    MachineJob job = LastHoveredJob;
                    if ( job != null )
                    {
                        Engine_HotM.HoveredJob = job;
                        if ( !job.DuringGame_IsUnlocked() || job.GetIsAtCap( true, null ) )
                        { }
                        else
                        {
                            return this.HandleInstallJobInStructure( job, true );
                        }
                    }
                }
            }
            #endregion

            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
            {
                ClearLastValidSpot();
                return false;
            }

            if ( MouseHelper.ActorUnderCursor is ISimMachineActor machineActor )
            {
                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                    MouseHelper.BasicLeftClickHandler( true );
                return false;
            }
            else if ( MouseHelper.ActorUnderCursor is ISimNPCUnit npcUnit )
            {
                if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                    MouseHelper.BasicLeftClickHandler( true );
                return false;
            }

            if ( TargetingTerritoryControlInvestigation != null )
            {
                MachineStructureType structureType = CommonRefs.TerritoryControlFlag;
                bool result = this.HandleFreestandingStructureType( structureType, null );
                if ( result )
                    return result;

                if ( useLeftBuildStyle && ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //go back
                {
                    if ( GoBackOneStep() )
                        return true;
                }

                return false;
            }
            else if ( currentCategory != null )
            {
                Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                MachineStructureType structureType = TargetingStructure;
                bool result = false;
                if ( structureType != null && structureType.DuringGame_IsUnlocked() )
                {
                    if ( structureType.IsTheDeleteFunction )
                    {
                        result = this.HandleDeleteStructureMode( false );
                        ClearLastValidSpot();
                    }
                    else if ( structureType.IsThePauseFunction )
                    {
                        result = this.HandlePauseStructureMode( false );
                        ClearLastValidSpot();
                    }
                    else if ( structureType.IsBuiltOnSiteOfExistingBuilding )
                    {
                        result = this.HandleEmbeddedStructureType( structureType, null, false );
                        ClearLastValidSpot();
                    }
                    else
                        result = this.HandleFreestandingStructureType( structureType, null );
                    if ( result )
                        return result;

                    if ( useLeftBuildStyle && ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //go back
                    {
                        if ( GoBackOneStep() )
                            return true;
                    }
                }
                else
                {
                    MachineJob job = TargetingJob;
                    if ( job != null )
                    {
                        if ( !job.DuringGame_IsUnlocked() || job.GetIsAtCap( true, null ) )
                            GoBackOneStep();
                        else
                        {
                            Engine_HotM.HoveredJob = job;
                            result = this.HandleInstallJobInStructure( job, false );
                            if ( result )
                                return result;
                        }
                    }

                    if ( useLeftBuildStyle && ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //go back
                    {
                        if ( GoBackOneStep() )
                            return true;
                    }
                }
            }
            else
            {
                ClearLastValidSpot();
                Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                if ( useLeftBuildStyle && ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //go back
                {
                    if ( GoBackOneStep() )
                        return true;
                }
            }

            return false;
        }

        #region GoBackOneStep
        private static bool GoBackOneStep()
        {
            if ( TargetingJob != null || TargetingStructure != null || TargetingTerritoryControlInvestigation != null )
            {
                TargetingJob = null;
                TargetingStructure = null;
                TargetingTerritoryControlInvestigation = null;
                return true;
            }

            Engine_HotM.SelectedMachineActionMode = null;
            return true;
        }
        #endregion

        #region HandleRenderEmbeddedBuildings
        public static void HandleRenderEmbeddedBuildings()
        {
            bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;
            Int64 framesPrepped = RenderManager.FramesPrepped;

            foreach ( ISimBuilding building in SimCommon.ValidEmbeddedStructureBuildTargets.GetDisplayList() )
            {
                MapItem item = building.GetMapItem();
                if ( item == null )
                    continue;

                MapCell cell = item.ParentCell;
                if ( !cell.IsConsideredInCameraView )
                    continue;

                if ( building.MachineStructureInBuilding != null )
                    continue; //if no machine structure possible here right now, or already has one
                //if ( building.GetAreMoreUnitsBlockedFromComingHere() )
                //    continue; //blocked building because a unit is there
                //do NOT call CalculateIsValidTargetForMachineStructureRightNow, as that's already implicit in this list
                
                if ( item.LastFramePrepRendered_StructureHighlight >= framesPrepped )
                    continue;
                item.LastFramePrepRendered_StructureHighlight = framesPrepped;

                //if we reached this point, this is a valid option!
                if ( building == MouseHelper.BuildingNoFilterUnderCursor )
                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidHoveredForMachineStructure.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                else
                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidForMachineStructure.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
            }
        }
        #endregion

        #region HandleDeleteStructureMode
        private bool HandleDeleteStructureMode( bool OnlyDoUIHover )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                debugStage = 500;

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7200;

                    debugStage = 7300;

                    debugStage = 8300;

                    debugStage = 9300;

                    Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                    Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;

                    ISimBuilding validBuildingToDelete = null;
                    Int64 framesPrepped = RenderManager.FramesPrepped;

                    #region Highlight Valid Building Targets
                    bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;
                    foreach ( ISimBuilding building in World.Buildings.GetBuildingsWithMachineStructures() )
                    {
                        if ( building == null )
                            continue;
                        if ( building.MachineStructureInBuilding == null )
                            continue; //if no machine structure here at the moment

                        MapItem item = building.GetMapItem();

                        if ( item.LastFramePrepRendered_StructureHighlight >= framesPrepped )
                            continue;
                        item.LastFramePrepRendered_StructureHighlight = framesPrepped;

                        //if we reached this point, this is a valid option!
                        if ( building == MouseHelper.BuildingNoFilterUnderCursor && !OnlyDoUIHover )
                        {
                            validBuildingToDelete = building;
                            RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidHoveredForMachineStructureDeletion.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                        }
                        else
                            RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidForMachineStructureDeletion.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                    }
                    #endregion

                    #region Tooltip
                    if ( !OnlyDoUIHover && validBuildingToDelete != null && validBuildingToDelete.MachineStructureInBuilding != null )
                    {
                        debugStage = 3100;

                        NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                        if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( validBuildingToDelete ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                        {
                            atMouse.Icon = CommonRefs.DeleteStructureType.Icon;

                            MachineJob job = validBuildingToDelete.MachineStructureInBuilding.CurrentJob;

                            if ( job != null )
                                atMouse.TitleUpperLeft
                                    .AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                    .AddRaw( job.GetDisplayName(), ColorTheme.RedOrange2 );
                            else
                                atMouse.TitleUpperLeft
                                    .AddSpriteStyled_NoIndent( validBuildingToDelete.MachineStructureInBuilding.Type.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                    .AddRaw( validBuildingToDelete.MachineStructureInBuilding.Type.GetDisplayName(), ColorTheme.RedOrange2 );


                            if ( validBuildingToDelete?.MachineStructureInBuilding?.GetIsNetworkTower() ?? false )
                            {
                                atMouse.Main.AddLang( "CannotDeleteNetworkSource", ColorTheme.RedOrange2 );
                                atMouse.ShouldTooltipBeRed = true;
                            }
                            else
                                atMouse.MainHeader.AddFormat1( "ClickToDelete",
                                        InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText(), ColorTheme.SoftGold );
                        }
                    }
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                    {
                        if ( ArcenInput.LeftMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                        {
                            HandleDeleteStructureMode_Click( validBuildingToDelete, OnlyDoUIHover );
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //deselect in build mode
                            return GoBackOneStep();
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                            HandleDeleteStructureMode_Click( validBuildingToDelete, OnlyDoUIHover );
                        else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }

                    
                    #endregion
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BuildModeHandler.HandleDeleteStructureMode Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }

        private static float blockedUntil = 0;

        private bool HandleDeleteStructureMode_Click( ISimBuilding validBuildingToDelete, bool OnlyDoUIHover )
        {
            MachineStructure structure = validBuildingToDelete?.MachineStructureInBuilding;

            if ( structure != null && !OnlyDoUIHover )
            {
                if ( structure.GetIsNetworkTower() )
                {
                    if ( blockedUntil < ArcenTime.AnyTimeSinceStartF )
                    {
                        VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotDeleteNetworkSource" ),
                            TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3, 1f );
                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete the primary network!
                    }
                    blockedUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                    return false;
                }
                else if ( (structure?.CurrentJob?.CannotBeScrappedOrDisabled ?? false) )
                {
                    if ( blockedUntil < ArcenTime.AnyTimeSinceStartF )
                    {
                        VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddFormat1_New( "CannotScrapBuilding", structure.GetDisplayName() ),
                        TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3, 1f );
                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete this other kind of job!
                    }
                    blockedUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                    return false;
                }
            }

            if ( Engine_HotM.GameMode == MainGameMode.CityMap && structure != null ) //if on the city map, focus on the building spot rather than doing the work
            {
                Engine_HotM.SetGameMode( MainGameMode.Streets );
                CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( validBuildingToDelete.GetMapItem().CenterPoint, true );
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                return true;
            }
            else if ( !OnlyDoUIHover && structure != null && validBuildingToDelete.MachineStructureInBuilding != null )
            {
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it

                //instantly deleting a structure, given this mode
                structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );

                //TryScrapSelectedStructure( validBuildingToDelete.MachineStructureInBuilding );
                return true;
            }
            return false;
        }
        #endregion

        #region HandlePauseStructureMode
        private bool HandlePauseStructureMode( bool OnlyDoUIHover )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                debugStage = 500;

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7200;

                    debugStage = 7300;

                    debugStage = 8300;

                    debugStage = 9300;

                    Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                    Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;

                    ISimBuilding validBuildingToTogglePause = null;
                    Int64 framesPrepped = RenderManager.FramesPrepped;

                    #region Highlight Valid Building Targets
                    bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;
                    foreach ( ISimBuilding building in World.Buildings.GetBuildingsWithMachineStructures() )
                    {
                        if ( building == null )
                            continue;
                        if ( building.MachineStructureInBuilding == null )
                            continue; //if no machine structure here at the moment
                        if ( !building.MachineStructureInBuilding.GetCanBePaused() )
                            continue; //if not valid for pausing

                        MapItem item = building.GetMapItem();

                        if ( item.LastFramePrepRendered_StructureHighlight >= framesPrepped )
                            continue;
                        item.LastFramePrepRendered_StructureHighlight = framesPrepped;

                        bool isPaused = building.MachineStructureInBuilding.IsJobPaused;

                        //if we reached this point, this is a valid option!
                        if ( building == MouseHelper.BuildingNoFilterUnderCursor && !OnlyDoUIHover )
                        {
                            validBuildingToTogglePause = building;
                            RenderManager_Streets.DrawMapItemHighlightedBorder( item,
                                isPaused ? ColorRefs.BuildingValidHoveredForMachineStructureUnPause.ColorHDR :
                                ColorRefs.BuildingValidHoveredForMachineStructurePause.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                        }
                        else
                            RenderManager_Streets.DrawMapItemHighlightedBorder( item, isPaused ? ColorRefs.BuildingValidForMachineStructureUnPause.ColorHDR :
                                ColorRefs.BuildingValidForMachineStructurePause.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                    }
                    #endregion

                    #region Tooltip
                    if ( !OnlyDoUIHover && validBuildingToTogglePause != null && validBuildingToTogglePause.MachineStructureInBuilding != null )
                    {
                        debugStage = 3100;

                        NovelTooltipBuffer atMouse = NovelTooltipBuffer.Instance;

                        if ( atMouse.TryStartSmallerTooltip( TooltipID.Create( validBuildingToTogglePause ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                        {
                            atMouse.Icon = CommonRefs.DeleteStructureType.Icon;

                            MachineJob job = validBuildingToTogglePause.MachineStructureInBuilding.CurrentJob;

                            if ( job != null )
                                atMouse.TitleUpperLeft
                                    .AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                    .AddRaw( job.GetDisplayName(), ColorTheme.RedOrange2 );
                            else
                                atMouse.TitleUpperLeft
                                    .AddSpriteStyled_NoIndent( validBuildingToTogglePause.MachineStructureInBuilding.Type.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 )
                                    .AddRaw( validBuildingToTogglePause.MachineStructureInBuilding.Type.GetDisplayName(), ColorTheme.RedOrange2 );

                            bool isPaused = validBuildingToTogglePause.MachineStructureInBuilding.IsJobPaused;

                            //if ( validBuildingToTogglePause.MachineStructureInBuilding.Type.IsEmbeddedInHumanBuildingOfTag != null )
                            //{
                            //    atMouse.Main.StartStyleLineHeightA();

                            //    //Building name if it's a human one
                            //    //atMouse.Main.AddRaw( validBuildingToTogglePause.GetDisplayName() );

                            //    atMouse.Main.AddFormat1( isPaused ? "ClickToUnPause" : "ClickToPause",
                            //        InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText(), ColorTheme.SoftGold );

                            //    atMouse.Main.EndLineHeight();
                            //}
                            //else
                                atMouse.MainHeader.AddFormat1( isPaused ? "ClickToUnPause" : "ClickToPause",
                                    InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText(), ColorTheme.SoftGold );
                        }
                    }
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                    {
                        if ( ArcenInput.LeftMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                        {
                            HandlePauseStructureMode_Click( validBuildingToTogglePause, OnlyDoUIHover );
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //deselect in build mode
                            return GoBackOneStep();
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                            HandlePauseStructureMode_Click( validBuildingToTogglePause, OnlyDoUIHover );
                        else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }


                    #endregion
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BuildModeHandler.HandleDeleteStructureMode Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }

        private bool HandlePauseStructureMode_Click( ISimBuilding validBuildingToPause, bool OnlyDoUIHover )
        {
            if ( Engine_HotM.GameMode == MainGameMode.CityMap && validBuildingToPause != null ) //if on the city map, focus on the building spot rather than doing the work
            {
                Engine_HotM.SetGameMode( MainGameMode.Streets );
                CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( validBuildingToPause.GetMapItem().CenterPoint, true );
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                return true;
            }
            else if ( !OnlyDoUIHover && validBuildingToPause != null && validBuildingToPause.MachineStructureInBuilding != null )
            {
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //action build; now consume it

                //instantly toggling pause, given this mode
                validBuildingToPause.MachineStructureInBuilding.IsJobPaused = !validBuildingToPause.MachineStructureInBuilding.IsJobPaused;

                //TryScrapSelectedStructure( validBuildingToDelete.MachineStructureInBuilding );
                return true;
            }
            return false;
        }
        #endregion

        #region TryScrapSelectedStructure
        public static void TryScrapSelectedStructure( MachineStructure structure )
        {
            if ( structure == null )
                return;

            if ( structure.GetIsNetworkTower() )
            {
                if ( blockedUntil < ArcenTime.AnyTimeSinceStartF )
                {
                    VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotDeleteNetworkSource" ),
                        TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3, 1f );
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete the primary network!
                }
                blockedUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                return;
            }
            else if ( (structure?.CurrentJob?.CannotBeScrappedOrDisabled ?? false) )
            {
                if ( blockedUntil < ArcenTime.AnyTimeSinceStartF )
                {
                    VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddFormat1_New( "CannotScrapBuilding", structure.GetDisplayName() ),
                    TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3, 1f );
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete this other kind of job!
                }
                blockedUntil = ArcenTime.AnyTimeSinceStartF + 0.25f;
                return;
            }

            //ArcenDebugging.LogSingleLine( "structure: " + structure.GetIsNetworkTower(), Verbosity.DoNotShow );

            if ( structure.IsUnderConstruction ||
                !structure.IsFunctionalStructure ) //probably because it was shot
            {
                //instantly deleting a structure that is under construction or shot to bits
                structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
            }
            else
            {
                if ( structure.CurrentJob != null && !structure.IsJobStillInstalling &&
                    structure.IsFunctionalJob && structure.IsFunctionalStructure )
                {
                    if ( structure.CurrentJob?.Implementation.HandleJobDeletionLogic( structure, structure.CurrentJob,
                        JobDeletionLogic.IsBlockedFromDeletion, null, false ) ?? false )
                    {
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera(); //apparently we were blocked, so don't do the deletion
                        return;
                    }
                }

                if ( structure.GetIsNetworkTower() )
                {
                    VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddLang_New( "CannotDeleteNetworkSource" ), 
                        TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3 );
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete the primary network!
                    return;
                }
                else if ( (structure?.CurrentJob?.CannotBeScrappedOrDisabled ?? false) )
                {
                    VisQueries.Instance.AddToExistingFloatingTextAtCurrentMousePosition( LocalizedString.AddFormat1_New( "CannotScrapBuilding", structure?.GetDisplayName() ),
                        TooltipID.Create( "TryScrapSelectedStructure", structure.StructureID, 0 ), TooltipWidth.Narrow, 3 );
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera(); //can't delete this other kind of job!
                    return;
                }
                else
                {
                    if ( structure.CurrentJob != null && !structure.IsJobStillInstalling &&
                        structure.IsFunctionalJob && structure.IsFunctionalStructure )
                    {
                        if ( structure.CurrentJob?.Implementation.HandleJobDeletionLogic( structure, structure.CurrentJob,
                            JobDeletionLogic.ShouldHaveDeletionPrompt, null, false ) ?? false )
                        {
                            structure.CurrentJob?.Implementation.HandleJobDeletionLogic( structure, structure.CurrentJob,
                                JobDeletionLogic.HandleDeletionPrompt,
                                delegate
                                {
                                    //the player said okay, so do it
                                    structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                                }, false );
                        }
                        else
                        {
                            //the job is not complaining, so do the regular deleting check
                            ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                            {
                                structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                            }, null, LocalizedString.AddFormat1_New( "DeleteStructure_Header", structure.GetDisplayName() ),
                                LocalizedString.AddLang_New( "DeleteStructure_BodyRegular" ),
                                LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                        }
                    }
                    else
                    {
                        //deleting a regular structure
                        ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small, delegate
                        {
                            structure.ScrapStructureNow( ScrapReason.ArbitraryPlayer, Engine_Universal.PermanentQualityRandom );
                        }, null, LocalizedString.AddFormat1_New( "DeleteStructure_Header", structure.GetDisplayName() ),
                            LocalizedString.AddLang_New( "DeleteStructure_BodyRegular" ),
                            LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                    }
                }
            }
        }
        #endregion

        #region AppendInfrastructure
        private void AppendInfrastructure( MachineJob job, MachineNetwork network, Vector3 loc, bool isCompletelyOutOfBounds, NovelTooltipBufferBase bufferBase )
        {
            bool showDetailed = InputCaching.ShouldShowDetailedTooltips;

            int requiredElectricity = job.DuringGameActorData[ActorRefs.RequiredElectricity];
            int generatedElectricity = job.DuringGameActorData[ActorRefs.GeneratedElectricity];
            int availableElectricity = network.GetNetworkDataAmountProvided( ActorRefs.GeneratedElectricity ) -
                network.GetNetworkDataAmountConsumed( ActorRefs.GeneratedElectricity );

            bufferBase.FrameBody.StartStyleLineHeightA();

            if ( requiredElectricity > 0 )
            {
                if ( showDetailed )
                    bufferBase.FrameBody.AddSpriteStyled_NoIndent( ActorRefs.RequiredElectricity.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.RequiredElectricity.TooltipIconColorHex )
                        .AddRawAndAfterLineItemHeader( ActorRefs.RequiredElectricity.GetDisplayName(), ActorRefs.RequiredElectricity.TooltipIconColorHex )
                        .AddRaw( requiredElectricity.ToStringThousandsWhole(), requiredElectricity < availableElectricity ? ActorRefs.RequiredElectricity.TooltipIconColorHex : ColorTheme.RedOrange2 )
                        .Space1x().AddFormat1( "ParentheticalAvailable", availableElectricity.ToStringThousandsWhole(), ColorTheme.Gray )
                        .Line();
                else
                    bufferBase.FrameBody.AddBoldLangAndAfterLineItemHeader( "Required_Resource_Brief", ColorTheme.DataLabelWhite )
                        .AddSpriteStyled_NoIndent( ActorRefs.RequiredElectricity.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.RequiredElectricity.TooltipIconColorHex )
                        .AddRaw( requiredElectricity.ToStringThousandsWhole(), requiredElectricity < availableElectricity ? ActorRefs.RequiredElectricity.TooltipIconColorHex : ColorTheme.RedOrange2 )
                        .Space1x().StartSize90().AddFormat1( "OutOF_SecondPart", availableElectricity.ToStringThousandsWhole(), ColorTheme.GrayLess ).EndSize()
                        .Line();
            }
            else if ( generatedElectricity > 0 )
            {
                if ( showDetailed )
                    bufferBase.FrameBody.AddSpriteStyled_NoIndent( ActorRefs.GeneratedElectricity.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.GeneratedElectricity.TooltipIconColorHex )
                        .AddRawAndAfterLineItemHeader( ActorRefs.GeneratedElectricity.GetDisplayName(), ActorRefs.GeneratedElectricity.TooltipIconColorHex )
                        .AddRaw( generatedElectricity.ToStringThousandsWhole(), ActorRefs.GeneratedElectricity.TooltipIconColorHex )
                        .Space1x().AddFormat1( "ParentheticalAvailable", availableElectricity.ToStringThousandsWhole(), ColorTheme.Gray )
                        .Line();
                else
                    bufferBase.FrameBody.AddBoldLangAndAfterLineItemHeader( "Generated_Resource_Brief", ColorTheme.DataLabelWhite )
                        .AddSpriteStyled_NoIndent( ActorRefs.GeneratedElectricity.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.GeneratedElectricity.TooltipIconColorHex )
                        .AddRaw( generatedElectricity.ToStringThousandsWhole() )
                        .Line();
            }

            int requiredDeterrence = job.DuringGameActorData[ActorRefs.JobRequiredDeterrence];
            int providedDeterrence = 0;
            if ( requiredDeterrence > 0 && !isCompletelyOutOfBounds )
            {
                MapTile tile = CityMap.TryGetWorldCellAtCoordinates( loc )?.ParentTile;
                providedDeterrence = tile?.CalculateProvidedDeterrenceAtLocation( loc ) ?? 0;
            }

            if ( requiredDeterrence > 0 )
            {
                string deterrenceHex = requiredDeterrence <= providedDeterrence ? ColorTheme.GrayLess : ColorTheme.RedOrange2;
                bufferBase.FrameBody.AddSpriteStyled_NoIndent( ActorRefs.JobRequiredDeterrence.Icon, AdjustedSpriteStyle.InlineLarger1_2, deterrenceHex )
                    .AddRawAndAfterLineItemHeader( ActorRefs.JobRequiredDeterrence.GetDisplayName(), deterrenceHex )
                    .AddRaw( requiredDeterrence.ToStringThousandsWhole(), deterrenceHex )
                    .Space1x().AddFormat1( "ParentheticalProvidedByNearbyUnits", providedDeterrence.ToStringThousandsWhole(), ColorTheme.Gray )
                    .Line();
            }

            int requiredProtection = job.DuringGameActorData[ActorRefs.JobRequiredProtection];
            int providedProtection = 0;
            if ( requiredProtection > 0 && !isCompletelyOutOfBounds )
            {
                MapTile tile = CityMap.TryGetWorldCellAtCoordinates( loc )?.ParentTile;
                providedProtection = tile?.CalculateProvidedProtectionAtLocation( loc ) ?? 0;
            }

            if ( requiredProtection > 0 )
            {
                string protectionHex = requiredProtection <= providedProtection ? ColorTheme.GrayLess : ColorTheme.RedOrange2;
                bufferBase.FrameBody.AddSpriteStyled_NoIndent( ActorRefs.JobRequiredProtection.Icon, AdjustedSpriteStyle.InlineLarger1_2, protectionHex )
                    .AddRawAndAfterLineItemHeader( ActorRefs.JobRequiredProtection.GetDisplayName(), protectionHex )
                    .AddRaw( requiredProtection.ToStringThousandsWhole(), protectionHex )
                    .Space1x().AddFormat1( "ParentheticalProvidedByNearbyUnits", providedProtection.ToStringThousandsWhole(), ColorTheme.Gray )
                    .Line();
            }

            bool isCyberocracyHub = false;

            if ( (job?.RequiredStructureType?.IsCyberocracyHub ?? false) && !isCompletelyOutOfBounds )
            {
                MapCell cell = CityMap.TryGetWorldCellAtCoordinates( loc );
                if ( cell != null )
                {
                    isCyberocracyHub = true;

                    bufferBase.FrameBody.AddBoldLangAndAfterLineItemHeader( "LowerAndMiddleClass", ColorTheme.DataLabelWhite )
                        .AddRaw( cell.LowerAndMiddleClassResidentsAndWorkersInCell.Display.ToStringThousandsWhole(), ColorTheme.DataGood )
                        .Line();

                    bufferBase.FrameBody.AddBoldLangAndAfterLineItemHeader( "UpperClass", ColorTheme.DataLabelWhite )
                        .AddRaw( cell.UpperClassResidentsAndWorkersInCell.Display.ToStringThousandsWhole(), ColorTheme.DataProblem )
                        .Line();
                }
            }

            bufferBase.FrameBody.EndLineHeight();

            if ( requiredElectricity > 0 || requiredDeterrence > 0 || requiredProtection > 0 || isCyberocracyHub )
                bufferBase.FrameTitle.AddLang( "RequiredInfrastructure" );
            else if ( generatedElectricity > 0 )
                bufferBase.FrameTitle.AddLang( "RequiredInfrastructure" );
        }
        #endregion

        #region HandleRangeAndConnectivityAndPredictionAndLocationIcons
        private void HandleRangeAndConnectivityAndPredictionAndLocationIcons( MachineStructureType structureType, BuildingPrefab prefab, MachineJob startingJob, Vector3 destinationPoint, 
            MachineStructure existingStructureOrNull,
            out bool canAfford, out bool isOutOfRangeOfAnyNetwork, out bool isBlockedByDistanceRestrictions, out float closestDistanceRestriction, 
            out MachineNetwork networkWouldConnectTo, out MachineSubnet subnetWouldConnectTo, 
            out ISimBuilding territoryBuildingWouldLinkTo )
        {
            bool isCompletelyOutOfBounds = float.IsInfinity( destinationPoint.x ) || float.IsNaN( destinationPoint.x );
            if ( isCompletelyOutOfBounds )
            {
                canAfford = false;
                isOutOfRangeOfAnyNetwork = true;
                isBlockedByDistanceRestrictions = true;
                closestDistanceRestriction = 0;
                networkWouldConnectTo = null;
                subnetWouldConnectTo = null;
                territoryBuildingWouldLinkTo = null;
                return;
            }

            canAfford = true;
            //if ( ResourceRefs.MentalEnergy.Current <= 0 )
            //{
            //    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_OutOfMentalEnergy, destinationPoint );
            //    canAfford = false;
            //}
            //else
            {
                if ( existingStructureOrNull != null && ( startingJob == null || !startingJob.GetCanAffordAnother( false, existingStructureOrNull?.CurrentJob ) ) )
                {
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_CannotAfford, destinationPoint );
                    canAfford = false;
                }
                else if ( !structureType.GetCanAfford( startingJob ) )
                {
                    CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_CannotAfford, destinationPoint );
                    canAfford = false;
                }
            }

            if ( startingJob != null && startingJob.DistanceRestriction != null && startingJob.DistanceRestriction.DuringGame_FullList.Count > 0 )
            {
                isBlockedByDistanceRestrictions = false;
                closestDistanceRestriction = 9999999f;
                foreach ( MachineStructure structure in startingJob.DistanceRestriction.DuringGame_FullList.GetDisplayList() )
                {
                    if ( existingStructureOrNull != null && structure == existingStructureOrNull )
                        continue; //don't count itself

                    Vector3 pos = structure.GetGroundCenterLocation();
                    float dist = (pos - destinationPoint).GetSquareGroundMagnitude();
                    if ( dist > startingJob.DistanceRestriction.DistanceTheseMustBeFromOneAnotherSquared )
                        continue; //far enough away, no complaints!

                    isBlockedByDistanceRestrictions = true;
                    if ( dist < closestDistanceRestriction )
                        closestDistanceRestriction = dist;

                    DrawHelper.RenderLine( pos.ReplaceY( destinationPoint.y ), destinationPoint, Color.red, 2f );
                }

                if ( isBlockedByDistanceRestrictions )
                    closestDistanceRestriction = Mathf.Sqrt( closestDistanceRestriction );
            }
            else
            {
                isBlockedByDistanceRestrictions = false;
                closestDistanceRestriction = 0f;
            }

            bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;


            if ( existingStructureOrNull != null )
            {
                networkWouldConnectTo = SimCommon.TheNetwork;
                subnetWouldConnectTo = existingStructureOrNull.CurrentSubnet.Display;
                isOutOfRangeOfAnyNetwork = (subnetWouldConnectTo == null);
                territoryBuildingWouldLinkTo = null;
            }
            else if ( structureType.IsTerritoryControlFlag )
            {
                //nothing to draw
                isOutOfRangeOfAnyNetwork = false;
                networkWouldConnectTo = null;
                subnetWouldConnectTo = null;
                territoryBuildingWouldLinkTo = null;

                if ( TargetingTerritoryControlInvestigation != null )
                {
                    float territoryControlRange = MathRefs.TerritoryControlLinkRange.FloatMin;

                    ISimBuilding bestBuilding = null;
                    float bestBuildingRange = 10000;

                    foreach ( KeyValuePair<ISimBuilding, bool> kv in TargetingTerritoryControlInvestigation.PossibleBuildings )
                    {
                        if ( !kv.Value )
                            continue;
                        ISimBuilding building = kv.Key;
                        if ( building == null )
                            continue;

                        float range = building.GetMapItem()?.GetBasicDistanceToPointXZ( destinationPoint, 0f )??10000;
                        if ( range < territoryControlRange )
                        {
                            if ( bestBuilding == null || bestBuildingRange > range )
                            {
                                bestBuilding = building;
                                bestBuildingRange = range;
                            }
                        }
                    }

                    territoryBuildingWouldLinkTo = bestBuilding;

                    if ( bestBuilding != null )
                    {
                        //draw a line from the building to the flag
                        DrawHelper.RenderLine( bestBuilding.GetMapItem().OBBCache.Center.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            destinationPoint.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            ColorRefs.BuildingPartOfTerritoryControl.ColorHDR );
                    }
                }
            }
            else if ( structureType.EstablishesNetworkOnBuild || structureType.IsSeparateFromAllNetworks )
            {
                //nothing to draw
                isOutOfRangeOfAnyNetwork = false;
                networkWouldConnectTo = null;
                subnetWouldConnectTo = null;
                territoryBuildingWouldLinkTo = null;
            }
            else if ( structureType.CanOnlyBeBuiltOutsideExistingNetworkRange )
            {
                //nothing to draw
                isOutOfRangeOfAnyNetwork = false;
                networkWouldConnectTo = null;
                subnetWouldConnectTo = null;
                territoryBuildingWouldLinkTo = null;
            }
            else if ( structureType.IsSeparateFromAllSubnets )
            {
                territoryBuildingWouldLinkTo = null;
                isOutOfRangeOfAnyNetwork = false; //start by assuming no
                subnetWouldConnectTo = null;

                MachineStructure bestProvider = null;
                MapTile tile = CityMap.TryGetWorldCellAtCoordinates( destinationPoint )?.ParentTile;
                if ( tile != null && tile.TileNetworkLevel.Display > TileNetLevel.None )
                    bestProvider = tile.TileNetworkUplink.Display;

                if ( bestProvider == null )
                {
                    //show error message
                    CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.OutOfNetworkRange,
                        destinationPoint.PlusY( prefab.PlaceableRoot.OriginalAABBSize.y + RenderManager_Streets.EXTRA_Y_FOR_STRUCTURE_ERRORS ), false );
                    isOutOfRangeOfAnyNetwork = true; //okay, we found out the answer is yes

                    networkWouldConnectTo = null;
                }
                else
                {
                    networkWouldConnectTo = SimCommon.TheNetwork;

                    //draw a line from the provider to the new structure if in range
                    DrawHelper.RenderLine( bestProvider.Building.GetMapItem().OBBCache.Center.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        destinationPoint.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        ColorRefs.MachineNetworkUplink.ColorHDR );
                }
            }
            else
            {
                territoryBuildingWouldLinkTo = null;
                isOutOfRangeOfAnyNetwork = false; //start by assuming no

                MachineStructure bestProvider = null;
                MapTile tile = CityMap.TryGetWorldCellAtCoordinates( destinationPoint )?.ParentTile;
                if ( tile != null && tile.TileNetworkLevel.Display == TileNetLevel.Full )
                    bestProvider = tile.TileNetworkUplink.Display;
                else if ( tile != null && tile.HasFullNetworkOrIsAdjacentToTileThatDoes.Display && structureType.CanBeBuiltAdjacentToNetwork )
                    bestProvider = tile.TileNetworkUplink.Display;

                MachineStructure bestSubnetConnection = null;
                if ( bestProvider != null ) //only do this if it's on a tile we can connect to
                {
                    //if this needs to connect to an existing network, see if this is in range of any of them
                    foreach ( MachineSubnet subnet in SimCommon.Subnets )
                    {
                        bestSubnetConnection = subnet.CalculateMachineStructureToConnectToPoint( destinationPoint );
                        if ( bestSubnetConnection != null )
                            break;
                    }
                }

                if ( bestSubnetConnection != null )
                {
                    networkWouldConnectTo = SimCommon.TheNetwork;
                    subnetWouldConnectTo = bestSubnetConnection.CurrentSubnet.Display;

                    //draw a line from the subnet connection to the new structure
                    DrawHelper.RenderLine( bestSubnetConnection.Building.GetMapItem().OBBCache.Center.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        destinationPoint.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                        ColorRefs.MachineNetworkSubnetConnection.ColorHDR );
                }
                else //null bestSubnetConnection
                {
                    if ( bestProvider == null )
                    {
                        //show error message
                        CursorHelper.RenderSpecificScalingIconAtSpot( false, IconRefs.OutOfNetworkRange,
                            destinationPoint.PlusY( prefab.PlaceableRoot.OriginalAABBSize.y + RenderManager_Streets.EXTRA_Y_FOR_STRUCTURE_ERRORS ), false );
                        isOutOfRangeOfAnyNetwork = true; //okay, we found out the answer is yes

                        networkWouldConnectTo = null;
                        subnetWouldConnectTo = null;
                    }
                    else
                    {
                        networkWouldConnectTo = SimCommon.TheNetwork;

                        Vector3 providerLoc = bestProvider.GetGroundCenterLocation();
                        float dist = (bestProvider.GetGroundCenterLocation() - destinationPoint).GetSquareGroundMagnitude();

                        if ( dist < NetworkConnectionCalculator.MAX_SUBNET_LINK_DISTANCE_SQUARED )
                        {
                            MapCell centerCell = bestProvider.CalculateMapCell();

                            float minX = Mathf.Min( destinationPoint.x, providerLoc.x );
                            float maxX = Mathf.Max( destinationPoint.x, providerLoc.x );
                            float minZ = Mathf.Min( destinationPoint.z, providerLoc.z );
                            float maxZ = Mathf.Max( destinationPoint.z, providerLoc.z );

                            bool wasBlockedByRoad = false;
                            foreach ( MapCell cell in centerCell.AdjacentCellsAndSelfIncludingDiagonal )
                            {
                                ArcenFloatRectangle rect = cell.CellRect;
                                if ( rect.XMax <= minX || rect.XMin >= maxX ||
                                    rect.YMax <= minZ || rect.YMin >= maxZ )
                                    continue; //this whole cell is out of range, so ignore it!

                                foreach ( MapItem road in cell.AllRoads )
                                {
                                    ArcenFloatRectangle roadRect = road.OBBCache.GetOuterRect();
                                    if ( MathA.LineIntersectsRectangleXZ( roadRect, destinationPoint, providerLoc ) )
                                    {
                                        wasBlockedByRoad = true;
                                        break;
                                    }
                                }

                                if ( wasBlockedByRoad )
                                    break;
                            }

                            if ( !wasBlockedByRoad )
                                subnetWouldConnectTo = bestProvider.CurrentSubnet.Display;
                            else
                                subnetWouldConnectTo = null;
                        }
                        else
                            subnetWouldConnectTo = null;

                        //draw a line from the provider to the new structure if in range
                        DrawHelper.RenderLine( bestProvider.Building.GetMapItem().OBBCache.Center.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            destinationPoint.ReplaceY( isMapMode ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0.1f ),
                            ColorRefs.MachineNetworkUplink.ColorHDR );
                    }
                }
            }
        }
        #endregion

        private static MachineStructureType lastValidSpot_StructureType = null;
        private static Vector3 lastValidSpot_Position = Vector3.zero;
        private static bool lastValidSpot_HadOne = false;
        private static int lastValidSpot_TheoreticalRotation = 0;
        private static bool isXSlideCheck = false;

        #region HandleFreestandingStructureType
        private bool HandleFreestandingStructureType( MachineStructureType structureType, MachineJob startingJob )
        {
            BuildingPrefab prefab = structureType?.Prefab;
            if ( prefab == null )
                return false;

            int debugStage = 0;
            try
            {
                debugStage = 100;
                Vector3 destinationPoint = Engine_HotM.MouseWorldHitLocation;

                Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;

                debugStage = 500;

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7200;

                    destinationPoint.y = prefab.PlaceableRoot.AlwaysDropTo;

                    debugStage = 7300;

                    #region Render Range Circles
                    debugStage = 8300;

                    bool hasValidDestinationPoint = !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow;
                    bool isCompletelyOutOfBounds = float.IsInfinity( destinationPoint.x ) || float.IsNaN( destinationPoint.x );
                    if ( isCompletelyOutOfBounds )
                        hasValidDestinationPoint = false;

                    if ( hasValidDestinationPoint && structureType.SnappingScale > 1f )
                    {
                        destinationPoint.x = Mathf.Ceil( destinationPoint.x * structureType.SnappingScale ) / structureType.SnappingScale;
                        destinationPoint.z = Mathf.Ceil( destinationPoint.z * structureType.SnappingScale ) / structureType.SnappingScale;
                    }

                    debugStage = 9300;

                    int theoreticalRotation = structureType.DuringGame_TheoreticalStructureRotation;

                    //this was just a debug test, and it worked great
                    //CommonRefs.GeneralTerritoryFlag.FirstRendererOfThisRoot.ParentGroup.WriteToDrawBufferForOneFrame_SliderColor(
                    //    destinationPoint, Quaternion.Euler( 0, theoreticalRotation, 0 ), Vector3.one, Color.blue,
                    //    Mathf.Clamp01( Mathf.Sin( ArcenTime.AnyTimeSinceStartF ) ) );

                    bool isUsingOldDest = false;
                    Vector3 newerDest = destinationPoint;

                    bool isBlocked = false;
                    string blockingString = string.Empty;
                    if ( hasValidDestinationPoint )
                    {
                        debugStage = 12300;
                        bool hadValidSpotInner = true;
                        if ( !CityMap.CalculateIsValidLocationForBuilding( prefab, destinationPoint, theoreticalRotation, 
                            structureType.MaxSecurityClearanceOfPOIForPlacement, structureType.RequiredPOITagForPlacement, out IMapBlockingItem BlockingItem, out bool WasInCorrectPOIType,
                            true, out bool IsBlockedByOutOfBounds ) ) //yes, allow overbuild
                        {
                            hadValidSpotInner = false;

                            if ( lastValidSpot_HadOne )
                            {
                                if ( lastValidSpot_StructureType != structureType || lastValidSpot_TheoreticalRotation != theoreticalRotation )
                                    lastValidSpot_HadOne = false;
                                else
                                {
                                    float dist = GameSettings.Current.GetFloat( "MaxBuildingSnapDistance" );
                                    if ( dist <= 0 )
                                        lastValidSpot_HadOne = false;
                                    else if ( (lastValidSpot_Position - destinationPoint ).GetSquareGroundMagnitude() > dist * dist )
                                        lastValidSpot_HadOne = false;
                                }
                            }

                            if ( lastValidSpot_HadOne )
                            {
                                Vector3 newPos = lastValidSpot_Position;
                                bool checkNewPos = false;

                                float slideAmount = 0.2f;
                                if ( checkNewPos && structureType.SnappingScale > 1f )
                                {
                                    slideAmount = 1f / structureType.SnappingScale;
                                }

                                if ( isXSlideCheck )
                                {
                                    if ( newPos.x < destinationPoint.x )
                                    {
                                        checkNewPos = true;
                                        newPos.x += slideAmount;
                                        if ( newPos.x > destinationPoint.x )
                                            newPos.x = destinationPoint.x;
                                    }
                                    else if ( newPos.x > destinationPoint.x )
                                    {
                                        checkNewPos = true;
                                        newPos.x -= slideAmount;
                                        if ( newPos.x < destinationPoint.x )
                                            newPos.x = destinationPoint.x;
                                    }
                                }
                                else
                                {
                                    if ( newPos.z < destinationPoint.z )
                                    {
                                        checkNewPos = true;
                                        newPos.z += slideAmount;
                                        if ( newPos.z > destinationPoint.z )
                                            newPos.z = destinationPoint.z;
                                    }
                                    else if ( newPos.z > destinationPoint.z )
                                    {
                                        checkNewPos = true;
                                        newPos.z -= slideAmount;
                                        if ( newPos.z < destinationPoint.z )
                                            newPos.z = destinationPoint.z;
                                    }
                                }
                                isXSlideCheck = !isXSlideCheck;

                                if ( checkNewPos && structureType.SnappingScale > 1f )
                                {
                                    newPos.x = Mathf.Ceil( newPos.x * structureType.SnappingScale ) / structureType.SnappingScale;
                                    newPos.z = Mathf.Ceil( newPos.z * structureType.SnappingScale ) / structureType.SnappingScale;
                                }

                                if ( checkNewPos )
                                {
                                    if ( CityMap.CalculateIsValidLocationForBuilding( prefab, newPos, theoreticalRotation,
                                        structureType.MaxSecurityClearanceOfPOIForPlacement, structureType.RequiredPOITagForPlacement, out IMapBlockingItem BlockingItem2, out bool WasInCorrectPOIType2,
                                        true, out bool IsBlockedByOutOfBounds2 ) )
                                    {
                                        hadValidSpotInner = true;
                                        destinationPoint = newPos;
                                        isUsingOldDest = true;

                                        BlockingItem = BlockingItem2;
                                        WasInCorrectPOIType = WasInCorrectPOIType2;
                                        IsBlockedByOutOfBounds = IsBlockedByOutOfBounds2;
                                    }
                                }

                                if ( !hadValidSpotInner )
                                {
                                    if ( CityMap.CalculateIsValidLocationForBuilding( prefab, lastValidSpot_Position, theoreticalRotation,
                                        structureType.MaxSecurityClearanceOfPOIForPlacement, structureType.RequiredPOITagForPlacement, out IMapBlockingItem BlockingItem2, out bool WasInCorrectPOIType2,
                                        true, out bool IsBlockedByOutOfBounds2 ) )
                                    {
                                        hadValidSpotInner = true;
                                        destinationPoint = lastValidSpot_Position;
                                        isUsingOldDest = true;

                                        BlockingItem = BlockingItem2;
                                        WasInCorrectPOIType = WasInCorrectPOIType2;
                                        IsBlockedByOutOfBounds = IsBlockedByOutOfBounds2;
                                    }
                                }
                            }

                        }

                        if ( !hadValidSpotInner )
                        {
                            isBlocked = true;
                            hasValidDestinationPoint = false;

                            if ( IsBlockedByOutOfBounds )
                                blockingString = Lang.Get( "BlockedBy_OutOfBoundsRuins" );
                            else if ( structureType.RequiredPOITagForPlacement != null && !WasInCorrectPOIType )
                                blockingString = structureType.RequiredPOITagForPlacementText.Text;
                            else if ( BlockingItem != null )
                            {
                                if ( BlockingItem is MapItem item )
                                {
                                    if ( item.SimBuilding != null )
                                        blockingString = item.SimBuilding.GetDisplayName();
                                    else
                                    {
                                        if ( item.Type.Roads.Count > 0 )
                                            blockingString = Lang.Get( "BlockedBy_Road" );
                                        else
                                            blockingString = Lang.Get( "BlockedBy_SmallObject" );
                                    }
                                    RenderManager_Streets.DrawMapItemHighlightedGhost( item, ColorRefs.ObjectBlockingBuild.ColorHDR, false, RenderManager.FramesPrepped );
                                }
                                else if ( BlockingItem is MapPOI poi )
                                {
                                    ArcenFloatRectangle rect = poi.GetOuterRect();
                                    Vector3 poiCenter = poi.GetCenter().ReplaceY( 0f );
                                    Vector3 size = new Vector3( (float)rect.Width, 1.4f, (float)rect.Height );
                                    Quaternion rotation = Quaternion.identity;
                                    if ( poi.SubRegionOrNull != null && poi.SubRegionOrNull.Rotation != 0 )
                                        rotation = Quaternion.Euler( 0, poi.SubRegionOrNull.Rotation, 0 );

                                    Color drawColor = poi.Type.RegionColorWithAlpha;
                                    if ( drawColor.a < 0.9f )
                                        drawColor.a = 0.9f;

                                    blockingString = poi.GetDisplayName();
                                    RenderHelper_Objects.DrawPOICubeTransparent( drawColor, poiCenter, size, rotation );
                                }
                                else if ( BlockingItem is ISimNPCUnit npcUnit )
                                {
                                    blockingString = npcUnit.GetDisplayName();
                                    RenderHelper_Objects.DrawNPCUnit_HighlightColor( npcUnit, ColorRefs.ObjectBlockingBuild.ColorHDR );
                                }
                                else if ( BlockingItem is ISimMachineUnit machineUnit )
                                {
                                    blockingString = machineUnit.GetDisplayName();
                                    RenderHelper_Objects.DrawMachineUnit_HighlightColor( machineUnit, ColorRefs.ObjectBlockingBuild.ColorHDR );
                                }
                                else if ( BlockingItem is ISimMachineVehicle machineVehicle )
                                {
                                    blockingString = machineVehicle.GetDisplayName();
                                    RenderHelper_Objects.DrawMachineVehicle_HighlightColor( machineVehicle, ColorRefs.ObjectBlockingBuild.ColorHDR );
                                }
                            }
                        }
                        else
                        {
                            lastValidSpot_HadOne = true;
                            lastValidSpot_StructureType = structureType;
                            lastValidSpot_Position = destinationPoint;
                            lastValidSpot_TheoreticalRotation = theoreticalRotation;
                        }
                    }
                    else
                    {
                        lastValidSpot_HadOne = false;
                        lastValidSpot_StructureType = null;
                    }

                    debugStage = 13100;
                    HandleRangeAndConnectivityAndPredictionAndLocationIcons( structureType, prefab, startingJob, destinationPoint, null, out bool canAfford, 
                        out bool isOutOfRangeOfAnyNetwork, out bool isBlockedByDistanceRestrictions, out float closestDistanceRestriction, out MachineNetwork networkWouldConnectTo, out MachineSubnet subnetWouldConnectTo,
                        out ISimBuilding territoryBuildingWouldLinkTo );
                    debugStage = 13200;


                    if ( startingJob != null && !isCompletelyOutOfBounds )
                    {
                        if ( startingJob.MaxExplosionRangeOnDeath > 0 )
                            DrawHelper.RenderRangeCircle( destinationPoint, startingJob.MaxExplosionRangeOnDeath, IconRefs.JobExplosionRadius.DefaultColorHDR );

                        if ( startingJob.GetJobEffectRange( out float JobEffectRange ) )
                            DrawHelper.RenderRangeCircle( destinationPoint,
                                JobEffectRange, ColorRefs.MachineJobEffectRangeBorder.ColorHDR );
                    }

                    debugStage = 14300;
                    if ( isCompletelyOutOfBounds )
                    { }
                    else if ( hasValidDestinationPoint && !isOutOfRangeOfAnyNetwork && !isBlockedByDistanceRestrictions )
                    {
                        MoveHelper.RenderBuildingColoredForBuildTarget( prefab, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostValid );

                        if ( isUsingOldDest )
                        {
                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( newerDest, destinationPoint, newerDest.y + 0.1f, ColorRefs.VehicleMoveGhostValid.ColorHDR, 1f );
                        }
                    }
                    else
                    {
                        if ( isBlocked || isOutOfRangeOfAnyNetwork || isBlockedByDistanceRestrictions )
                            MoveHelper.RenderBuildingColoredForBuildTarget( prefab, destinationPoint, theoreticalRotation, ColorRefs.VehicleMoveGhostBlocked );
                        else
                            CursorHelper.RenderSpecificMouseCursor( true, IconRefs.Mouse_OutOfRange );
                    }
                    #endregion

                    if ( !isCompletelyOutOfBounds && structureType.GetStructureTypeNetworkRange( out float networkRange ) )
                    {
                        float startingNetworkRange = networkRange;
                        
                        if ( startingJob != null )
                            networkRange += startingJob.DuringGameActorData[ActorRefs.NetworkRange];

                        if ( networkRange > 0 )
                        {
                            if ( startingNetworkRange != networkRange )
                            {
                                DrawHelper.RenderRangeCircle( destinationPoint, startingNetworkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );
                                DrawHelper.RenderRangeCircle( destinationPoint, networkRange, ColorRefs.MachineNetworkRangeAfterJobBorder.ColorHDR );
                            }
                            else
                                DrawHelper.RenderRangeCircle( destinationPoint, networkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );
                        }
                    }

                    if ( startingJob  != null && !isCompletelyOutOfBounds )
                        TryHandleJobDeploymentRangeRendering( startingJob, destinationPoint );

                    bool canConstruct = !isCompletelyOutOfBounds && hasValidDestinationPoint && !isBlocked && canAfford && !isOutOfRangeOfAnyNetwork && !isBlockedByDistanceRestrictions;

                    #region Tooltip
                    {
                        debugStage = 3100;

                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartSmallerTooltip( startingJob == null ? TooltipID.Create( structureType ) : TooltipID.Create( startingJob ), 
                            null, SideClamp.Any, InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.Smaller ) )
                        {
                            lowerLeft.ShouldTooltipBeRed = !canConstruct;
                            lowerLeft.CanExpand = CanExpandType.Longer;

                            InvestigationType territoryControlType = TargetingTerritoryControlInvestigation?.Type;

                            lowerLeft.Main.StartStyleLineHeightA();

                            if ( territoryControlType != null )
                            {
                                lowerLeft.Icon = territoryControlType.TerritoryControl.Resource.Icon;
                                lowerLeft.TitleUpperLeft.AddRaw( territoryControlType.GetDisplayName() );
                            }
                            else if ( startingJob != null )
                            {
                                lowerLeft.Icon = startingJob.Icon;
                                lowerLeft.TitleUpperLeft.AddRaw( startingJob.GetDisplayName() );
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    lowerLeft.Main.StartBold().AddRaw( structureType.GetDisplayName() ).EndBold().Line();
                            }
                            else
                            {
                                lowerLeft.Icon = structureType.Icon;
                                lowerLeft.TitleUpperLeft.AddRaw( structureType.GetDisplayName() );
                            }

                            if ( structureType.EstablishesNetworkOnBuild || structureType.CanOnlyBeBuiltOutsideExistingNetworkRange )
                            { } //nothing to say
                            else if ( structureType.IsTerritoryControlFlag )
                            {
                                if ( (territoryBuildingWouldLinkTo == null || territoryControlType == null || territoryControlType.TerritoryControl == null) )
                                {
                                    lowerLeft.Main.AddLang( "TooFarFromResourceSite", ColorTheme.RedOrange2 ).Line();
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                    {
                                        lowerLeft.Main.AddLang( "TooFarFromResourceSite_Details" ).Line();
                                    }
                                }
                                else
                                    lowerLeft.Main.AddBoldLangAndAfterLineItemHeader( "WillConnectToResourceSite", ColorTheme.DataLabelWhite )
                                        .AddRaw( territoryBuildingWouldLinkTo.GetDisplayName() ).Line();
                            }
                            else
                            {
                                if ( subnetWouldConnectTo != null )
                                {
                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                        lowerLeft.Main.AddFormat1( "SubnetName", subnetWouldConnectTo.SubnetIndex, ColorTheme.DataBlue ).Line();
                                }
                                else
                                {
                                    if ( networkWouldConnectTo != null )
                                    {
                                        if ( InputCaching.ShouldShowDetailedTooltips )
                                            lowerLeft.Main.AddLang( "NewSubnet", ColorTheme.DataBlue ).Line();
                                    }
                                    else
                                        lowerLeft.Main.AddLang( "OutOfNetworkRange", ColorTheme.RedOrange2 ).Line();
                                }
                            }

                            if ( isBlocked )
                                lowerLeft.Main.AddBoldLangAndAfterLineItemHeader( "BlockedBy", ColorTheme.DataLabelWhite )
                                    .AddRaw( blockingString, ColorTheme.RedOrange2 ).Line();

                            if ( isBlockedByDistanceRestrictions && startingJob?.DistanceRestriction != null )
                                lowerLeft.Main.AddBoldLangAndAfterLineItemHeader( "TooCloseToAnother", ColorTheme.DataLabelWhite )
                                    .AddRaw( startingJob.DistanceRestriction.GetDisplayName(), ColorTheme.RedOrange2 )
                                    .Space1x().AddFormat2( "ParentheticalOutOF", Mathf.FloorToInt( closestDistanceRestriction ),
                                    Mathf.CeilToInt( startingJob.DistanceRestriction.DistanceTheseMustBeFromOneAnother ) ).Line();

                            if ( !canAfford )
                            {
                                //if ( ResourceRefs.MentalEnergy.Current <= 0 )
                                //    lowerLeft.Main.AddLang( "YouAreOutOfMentalEnergyForThisTurn", ColorTheme.RedOrange2 ).Line();

                                structureType.WriteResourceCostFailures( lowerLeft.Main, startingJob );
                            }

                            debugStage = 3200;

                            if ( startingJob != null && networkWouldConnectTo != null )
                            {
                                if ( InputCaching.ShouldShowDetailedTooltips || canConstruct )
                                    AppendInfrastructure( startingJob, networkWouldConnectTo, destinationPoint, isCompletelyOutOfBounds, lowerLeft );
                            }

                            lowerLeft.Main.AddFormat1( "ClickToBuild",
                                InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText(), ColorTheme.SoftGold ).Space1x();

                            lowerLeft.Main.AddFormat2( "RotateStructure", InputCaching.GetGetHumanReadableKeyComboForRotateStructureLeft(),
                                InputCaching.GetGetHumanReadableKeyComboForRotateStructureRight(), ColorTheme.SoftGold )
                                    .Line();

                            lowerLeft.Main.EndLineHeight();
                        }
                    }
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                    {
                        if ( ArcenInput.LeftMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                        {
                            if ( !isCompletelyOutOfBounds )
                                HandleFreestandingStructureType_Click( structureType, startingJob, canConstruct, destinationPoint, territoryBuildingWouldLinkTo, theoreticalRotation, prefab );
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //deselect in build mode
                            return GoBackOneStep();
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                        {
                            if ( !isCompletelyOutOfBounds )
                                HandleFreestandingStructureType_Click( structureType, startingJob, canConstruct, destinationPoint, territoryBuildingWouldLinkTo, theoreticalRotation, prefab );
                        }
                        else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                    #endregion
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BuildModeHandler.HandleFreestandingStructureType Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }

        private bool HandleFreestandingStructureType_Click( MachineStructureType structureType, MachineJob startingJob, bool canConstruct, 
            Vector3 destinationPoint, ISimBuilding territoryBuildingWouldLinkTo, int theoreticalRotation, BuildingPrefab prefab )
        {
            if ( Engine_HotM.GameMode == MainGameMode.CityMap ) //action build; if on the city map, focus on the building spot rather than doing the work
            {
                Engine_HotM.SetGameMode( MainGameMode.Streets );
                CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( destinationPoint, true );
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                return true;
            }
            else if ( canConstruct )
            {
                InvestigationType territoryControlType = TargetingTerritoryControlInvestigation?.Type;
                if ( structureType.IsTerritoryControlFlag && (territoryBuildingWouldLinkTo == null || territoryControlType == null || territoryControlType.TerritoryControl == null) )
                {
                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                        ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                    else
                        ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                    return true;
                }

                ISimBuilding building = CityMap.TryPlaceBuildingFromMainThread( prefab, destinationPoint,
                    theoreticalRotation, structureType.MaxSecurityClearanceOfPOIForPlacement, structureType.RequiredPOITagForPlacement,
                    true ); //yes, allow overbuild
                if ( building != null )
                {
                    structureType.PayResourceCosts( startingJob );
                    //ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_BuiltStructures", ResourceAddRule.IgnoreUntilTurnChange );

                    MachineStructure structure = MachineStructure.Create( structureType, building );
                    building.SetStatus( CommonRefs.UnderConstructionBuildingStatus );
                    if ( structure != null )
                    {
                        structure.StartFreshConstruction();
                        if ( startingJob != null )
                            structure.StartFreshJobInstallation( startingJob, Engine_Universal.PermanentQualityRandom );

                        //the extra linkages for territory control
                        if ( structureType.IsTerritoryControlFlag )
                        {
                            structure.LinkedToForTerritoryControl = new WrapperedSimBuilding( territoryBuildingWouldLinkTo );
                            structure.TerritoryControlInvestigation = territoryControlType;
                            structure.TerritoryControlType = territoryControlType.TerritoryControl;
                            structure.TerritoryControlType?.DuringGame_DoOnStartUnlocksIfNeeded( false );
                            TargetingTerritoryControlInvestigation?.ReturnToPool();
                            TargetingTerritoryControlInvestigation = null;
                        }
                        else
                        {
                        }
                    }
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                        ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                    else
                        ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region HandleEmbeddedStructureType
        private bool HandleEmbeddedStructureType( MachineStructureType structureType, MachineJob startingJob, bool OnlyDoUIHover )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                debugStage = 500;

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7200;

                    debugStage = 7300;

                    debugStage = 8300;

                    debugStage = 9300;

                    ISimBuilding validBuildingToUse = MouseHelper.BuildingNoFilterUnderCursor;
                    if ( validBuildingToUse != null )
                    {
                        if ( validBuildingToUse.MachineStructureInBuilding != null )
                            validBuildingToUse = null; //if no machine structure possible here right now, or already has one
                        else if ( //validBuildingToUse.GetAreMoreUnitsBlockedFromComingHere() ||
                            !validBuildingToUse.CalculateIsValidTargetForMachineStructureRightNow( structureType, startingJob ) )
                            validBuildingToUse = null; //blocked building because a unit is there, or the wrong type
                    }

                    Engine_HotM.CurrentEmbeddedStructureTypeToFocus = structureType;
                    Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = startingJob;

                    bool canAfford = false;
                    bool isOutOfRangeOfAnyNetwork = true;
                    bool isBlockedByDistanceRestrictions = false;
                    float closestDistanceRestriction = 0;
                    MachineNetwork networkWouldConnectTo = null;

                    MachineSubnet subnetWouldConnectTo = null;

                    if ( !OnlyDoUIHover && validBuildingToUse != null )
                    {
                        Vector3 destinationPoint = validBuildingToUse.GetMapItem().GroundCenterPoint;
                        BuildingPrefab prefab = validBuildingToUse.GetPrefab();
                        HandleRangeAndConnectivityAndPredictionAndLocationIcons( structureType, prefab, startingJob, destinationPoint, null, out canAfford, 
                            out isOutOfRangeOfAnyNetwork, out isBlockedByDistanceRestrictions, out closestDistanceRestriction, out networkWouldConnectTo, out subnetWouldConnectTo,
                            out ISimBuilding territoryBuildingWouldLinkTo );

                        if ( startingJob != null )
                        {
                            if ( startingJob.MaxExplosionRangeOnDeath > 0 )
                                DrawHelper.RenderRangeCircle( validBuildingToUse.GetMapItem().GroundCenterPoint, startingJob.MaxExplosionRangeOnDeath, IconRefs.JobExplosionRadius.DefaultColorHDR );

                            if ( startingJob.GetJobEffectRange( out float JobEffectRange ) )
                                DrawHelper.RenderRangeCircle( validBuildingToUse.GetMapItem().GroundCenterPoint,
                                    JobEffectRange, ColorRefs.MachineJobEffectRangeBorder.ColorHDR );
                        }

                        if ( structureType.GetStructureTypeNetworkRange( out float networkRange ) )
                        {
                            float startingNetworkRange = networkRange;

                            if ( startingJob != null )
                                networkRange += startingJob.DuringGameActorData[ActorRefs.NetworkRange];

                            if ( networkRange > 0 )
                            {
                                if ( startingNetworkRange != networkRange )
                                {
                                    DrawHelper.RenderRangeCircle( destinationPoint, startingNetworkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );
                                    DrawHelper.RenderRangeCircle( destinationPoint, networkRange, ColorRefs.MachineNetworkRangeAfterJobBorder.ColorHDR );
                                }
                                else
                                    DrawHelper.RenderRangeCircle( destinationPoint, networkRange, ColorRefs.MachineNetworkRangeBorder.ColorHDR );
                            }
                        }
                    }

                    bool canConstruct = canAfford && !isOutOfRangeOfAnyNetwork && !isBlockedByDistanceRestrictions;

                    #region Tooltip
                    if ( !OnlyDoUIHover && validBuildingToUse != null )
                    {
                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartSmallerTooltip( startingJob == null ? TooltipID.Create( structureType ) : TooltipID.Create( startingJob ),
                            null, SideClamp.Any, InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.Smaller ) )
                        {
                            lowerLeft.ShouldTooltipBeRed = !canConstruct;
                            lowerLeft.CanExpand = CanExpandType.Longer;

                            lowerLeft.Main.StartStyleLineHeightA();

                            if ( startingJob != null )
                            {
                                lowerLeft.Icon = startingJob.Icon;
                                lowerLeft.TitleUpperLeft.AddRaw( startingJob.GetDisplayName() );
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                {
                                    lowerLeft.Main.StartBold().AddRaw( structureType.GetDisplayName() ).EndBold().Line();
                                    lowerLeft.Main.StartBold().AddRaw( validBuildingToUse.GetDisplayName() ).EndBold().Line();
                                }
                            }
                            else
                            {
                                lowerLeft.Icon = structureType.Icon;
                                lowerLeft.TitleUpperLeft.AddRaw( structureType.GetDisplayName() );
                                if ( InputCaching.ShouldShowDetailedTooltips )
                                    lowerLeft.Main.StartBold().AddRaw( validBuildingToUse.GetDisplayName() ).EndBold().Line();
                            }

                            if ( structureType.EstablishesNetworkOnBuild || structureType.CanOnlyBeBuiltOutsideExistingNetworkRange )
                            { } //nothing to say
                            else if ( subnetWouldConnectTo != null )
                                lowerLeft.Main.AddFormat1( "SubnetName", subnetWouldConnectTo.SubnetIndex, ColorTheme.DataBlue ).Line();
                            else
                            {
                                if ( networkWouldConnectTo != null )
                                    lowerLeft.Main.AddLang( "NewSubnet", ColorTheme.DataBlue ).Line();
                                else
                                    lowerLeft.Main.AddLang( "OutOfNetworkRange", ColorTheme.RedOrange2 ).Line();
                            }

                            if ( isBlockedByDistanceRestrictions && startingJob?.DistanceRestriction != null )
                                lowerLeft.Main.AddBoldLangAndAfterLineItemHeader( "TooCloseToAnother", ColorTheme.DataLabelWhite )
                                    .AddRaw( startingJob.DistanceRestriction.GetDisplayName(), ColorTheme.RedOrange2 )
                                    .Space1x().AddFormat2( "ParentheticalOutOF", Mathf.FloorToInt( closestDistanceRestriction ),
                                    Mathf.CeilToInt( startingJob.DistanceRestriction.DistanceTheseMustBeFromOneAnother ) ).Line();

                            if ( !canAfford )
                            {
                                //if ( ResourceRefs.MentalEnergy.Current <= 0 )
                                //    lowerLeft.Main.AddLang( "YouAreOutOfMentalEnergyForThisTurn", ColorTheme.RedOrange2 ).Line();

                                structureType.WriteResourceCostFailures( lowerLeft.Main, startingJob );
                            }

                            debugStage = 3200;

                            if ( startingJob != null && networkWouldConnectTo != null )
                            {
                                if ( InputCaching.ShouldShowDetailedTooltips || canConstruct )
                                    AppendInfrastructure( startingJob, networkWouldConnectTo, validBuildingToUse.GetMapItem().CenterPoint, false, lowerLeft );
                            }


                            lowerLeft.Main.AddFormat1( "ClickToBuild",
                                InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText(), ColorTheme.SoftGold );

                            //rotation does not apply here!
                            //lowerLeft.Main.AddFormat2( "RotateStructure", InputCaching.GetGetHumanReadableKeyComboForRotateStructureLeft(),
                            //    InputCaching.GetGetHumanReadableKeyComboForRotateStructureRight(), ColorTheme.SoftGold )
                            //        .Line();


                            lowerLeft.Main.EndLineHeight();
                        }
                    }
                    else if ( !OnlyDoUIHover )
                    {
                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartSmallerTooltip( startingJob == null ? TooltipID.Create( structureType ) : TooltipID.Create( startingJob ), null, SideClamp.Any, 
                            TooltipNovelWidth.Simple ) )
                        {
                            if ( startingJob != null )
                                lowerLeft.Icon = startingJob.Icon;
                            else
                                lowerLeft.Icon = structureType.Icon;

                            lowerLeft.TitleUpperLeft.AddLang( "Embededded_Buildings_Header" ).Line();
                            lowerLeft.Main.StartStyleLineHeightA();
                            lowerLeft.Main.StartColor( ColorTheme.NarrativeColor )
                                .AddFormat1( "Embeded_ByMouseCursor_1", InputCaching.UseLeftClickForBuildingConstruction ? Lang.GetLeftClickText() : Lang.GetRightClickText() ).Line();
                            lowerLeft.Main.AddLang( "Embeded_ByMouseCursor_2" );
                            lowerLeft.Main.EndLineHeight();
                        }
                    }
                    #endregion

                    debugStage = 29300;
                    #region Handle Clicks
                    if ( InputCaching.UseLeftClickForBuildingConstruction )
                    {
                        if ( ArcenInput.LeftMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                        {
                            HandleEmbeddedStructureType_Click( structureType, startingJob, validBuildingToUse, OnlyDoUIHover, canConstruct );
                        }
                        else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //deselect in build mode
                            return GoBackOneStep();
                    }
                    else
                    {
                        if ( ArcenInput.RightMouseNonUI.PeekIsBrieflyClicked() ) //action build; do not consume it yet
                            HandleEmbeddedStructureType_Click( structureType, startingJob, validBuildingToUse, OnlyDoUIHover, canConstruct );
                        else if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //select
                            MouseHelper.BasicLeftClickHandler( false );
                    }
                    #endregion
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BuildModeHandler.HandleEmbeddedStructureType Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }

        private bool HandleEmbeddedStructureType_Click( MachineStructureType structureType, MachineJob startingJob, ISimBuilding validBuildingToUse, bool OnlyDoUIHover, bool canConstruct )
        {
            if ( Engine_HotM.GameMode == MainGameMode.CityMap && validBuildingToUse != null ) //if on the city map, focus on the building spot rather than doing the work
            {
                Engine_HotM.SetGameMode( MainGameMode.Streets );
                CameraCurrent.MarkSelectedActorAsAlreadyHavingBeenFocusedOn_Streets();
                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( validBuildingToUse.GetMapItem().CenterPoint, true );
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                return true;
            }
            else if ( !OnlyDoUIHover && validBuildingToUse != null && canConstruct )
            {
                if ( InputCaching.UseLeftClickForBuildingConstruction )
                    ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                else
                    ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume(); //now consume it
                if ( structureType.EstablishesNetworkOnBuild && SimCommon.TheNetwork == null )
                {
                    Window_NetworkNameWindow.BuildingToCreateAt = validBuildingToUse;
                    Window_NetworkNameWindow.StructureTypeForCosts = structureType;
                    Window_NetworkNameWindow.Instance.Open();
                }
                else
                {
                    if ( structureType.EstablishesNetworkOnBuild && SimCommon.TheNetwork != null && SimCommon.TheNetwork.Tower != null )
                        return false;
                        
                    structureType.PayResourceCosts( startingJob );
                    //ResourceRefs.MentalEnergy.AlterCurrent_Named( -1, "Expense_BuiltStructures", ResourceAddRule.IgnoreUntilTurnChange );
                    MachineStructure structure = MachineStructure.Create( structureType, validBuildingToUse );
                    if ( structure != null )
                    {
                        structure.StartFreshConstruction();
                        if ( startingJob != null )
                            structure.StartFreshJobInstallation( startingJob, Engine_Universal.PermanentQualityRandom );

                        if ( structureType.EstablishesNetworkOnBuild && SimCommon.TheNetwork != null )
                        {
                            SimCommon.TheNetwork.Tower = structure;
                            TargetingStructure = null;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region HandleInstallJobInStructure
        private bool HandleInstallJobInStructure( MachineJob job, bool OnlyDoUIHover )
        {
            int debugStage = 0;
            try
            {
                debugStage = 100;

                debugStage = 500;

                debugStage = 2200;

                debugStage = 3200;
                {
                    debugStage = 7200;

                    debugStage = 7300;

                    debugStage = 8300;

                    debugStage = 9300;

                    #region Highlight Valid Building Targets
                    Vector3 centerCellPoint = /*hasValidDestinationPoint ? destinationPoint : */CameraCurrent.CameraBodyPosition;

                    MapCell centerCell = CityMap.TryGetWorldCellAtCoordinates( centerCellPoint );
                    if ( centerCell != null )
                    {
                        bool isMapMode = Engine_HotM.GameMode == MainGameMode.CityMap;
                        Int64 framesPrepped = RenderManager.FramesPrepped;
                        foreach ( MapCell cell in centerCell.AdjacentCellsAndSelfIncludingDiagonal3x )
                        {
                            if ( !cell.IsConsideredInCameraView )
                                continue;

                            foreach ( MapSubCell subCell in cell.SubCells )
                            {
                                if ( !subCell.ShouldBeRenderedIfParentCellIsRendered )
                                    continue;

                                foreach ( MapItem item in subCell.BuildingList.GetDisplayList() )
                                {
                                    if ( item.LastFramePrepRendered_JobHighlight >= framesPrepped )
                                        continue;
                                    item.LastFramePrepRendered_JobHighlight = framesPrepped;

                                    ISimBuilding building = item.SimBuilding;
                                    if ( building == null )
                                        continue;
                                    MachineStructure structure = building.MachineStructureInBuilding;
                                    if ( structure == null )
                                        continue; //if no machine structure here yet, then ignore it

                                    if ( !job.GetIsValidForThisJobToGoAtThatStructure( structure ) )
                                        continue; //if not a relevant structure type, don't show anything
                                    if ( structure.CurrentJob == job )
                                    {
                                        RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingAlreadyThisMachineStructureType.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                                        continue; //if already has that kind of job there, then draw that
                                    }

                                    //if we reached this point, this is a valid option!
                                    if ( building == MouseHelper.BuildingNoFilterUnderCursor && !OnlyDoUIHover )
                                        RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingSoftBlockedHoveredForMachineStructure.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                                    else
                                    {
                                        if ( !structure.IsFunctionalStructure || structure.CurrentJob != null )
                                            RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingSoftBlockedForMachineStructure.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                                        else
                                            RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidForMachineStructure.ColorHDR, HighlightPass.First, isMapMode, framesPrepped );
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    //look at creating the structure
                    MachineStructureType preferredStructureType = job.RequiredStructureType;
                    if ( preferredStructureType != null )
                    {
                        if ( preferredStructureType.IsEmbeddedInHumanBuildingOfTag != null )
                        {
                            ClearLastValidSpot();
                            return this.HandleEmbeddedStructureType( preferredStructureType, job, OnlyDoUIHover );
                        }
                        else
                        {
                            Engine_HotM.CurrentEmbeddedStructureTypeToFocus = null;
                            Engine_HotM.CurrentEmbeddedStructureTypeJobToGoWith = null;
                            if ( !OnlyDoUIHover )
                                return this.HandleFreestandingStructureType( preferredStructureType, job );
                        }
                    }
                }

                debugStage = 101200;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BuildModeHandler.HandleEmbeddedStructureType Error", debugStage,
                    string.Empty, e, Verbosity.ShowAsError );
            }
            return false;
        }
        #endregion

        #region TryDoActualInstallationOfJobIntoStructure
        public static bool TryDoActualInstallationOfJobIntoStructure( MachineStructure Structure, MachineJob JobToInstall, MersenneTwister Rand )
        {
            if ( Structure.CurrentJob != null && !Structure.IsJobStillInstalling &&
                Structure.IsFunctionalJob && Structure.IsFunctionalStructure )
            {
                if ( Structure.CurrentJob?.Implementation.HandleJobDeletionLogic( Structure, Structure.CurrentJob,
                    JobDeletionLogic.IsBlockedFromDeletion, null, false ) ?? false )
                    return false; //apparently we were blocked, so don't do the replacement

                if ( Structure.CurrentJob?.Implementation.HandleJobDeletionLogic( Structure, Structure.CurrentJob,
                    JobDeletionLogic.ShouldHaveDeletionPrompt, null, false ) ?? false )
                {
                    Structure.CurrentJob?.Implementation.HandleJobDeletionLogic( Structure, Structure.CurrentJob,
                        JobDeletionLogic.HandleDeletionPrompt,
                        delegate
                        {
                            //the player said okay, so do it
                            JobToInstall.PayResourceCosts();
                            Structure.StartFreshJobInstallation( JobToInstall, Rand );
                        }, false );
                }
                else
                {
                    //the job is not complaining, so do its
                    JobToInstall.PayResourceCosts();
                    Structure.StartFreshJobInstallation( JobToInstall, Rand );
                }
            }
            else
            {
                //fresh installation, just do it
                JobToInstall.PayResourceCosts();
                Structure.StartFreshJobInstallation( JobToInstall, Rand );
            }
            return true;
        }
        #endregion

        #region TryHandleJobDeploymentRangeRendering
        public void TryHandleJobDeploymentRangeRendering( MachineJob job, Vector3 position )
        {
            if ( job == JobRefs.AerospaceHangar )
            {
                //render the other jobs
                ISimMapActor unused = null;
                Vector3 unused2 = Vector3.zero;
                CommandModeHandler.FindBestVehicleDeployer( position, ref unused, ref unused2, true );

                //render this job
                if ( position.x != float.NegativeInfinity )
                    DrawHelper.RenderCircle( position.ReplaceY( ThreatLineData.BaselineHeight ), MathRefs.VehicleMaxDeploymentDistanceFromHangar.FloatMin,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }
            else if ( job == JobRefs.AndroidLauncher )
            {
                //render the other jobs
                ISimMapActor unused = null;
                Vector3 unused2 = Vector3.zero;
                CommandModeHandler.FindBestAndroidDeployer( position, ref unused, ref unused2, true );

                //render this job
                if ( position.x != float.NegativeInfinity )
                    DrawHelper.RenderCircle( position.ReplaceY( ThreatLineData.BaselineHeight ), MathRefs.VehicleMaxDeploymentDistanceFromHangar.FloatMin,
                        ColorRefs.DeploymentRangeColor.ColorHDR, 1f );
            }
        }
        #endregion

        public void HandlePassedInput( int Int1, InputActionTypeData InputActionType )
        {
            string InputActionID = InputActionType.ID;
            switch ( InputActionID )
            {
                case "RotateStructureLeft":
                    {
                        ArcenInput.BlockUntilNextFrame();
                        MachineStructureType structureType = TargetingTerritoryControlInvestigation != null ? CommonRefs.TerritoryControlFlag : TargetingStructure;
                        if ( structureType == null )
                        {
                            MachineJob job = TargetingJob;
                            structureType = job?.RequiredStructureType;
                        }

                        if ( structureType != null && !structureType.IsBuiltOnSiteOfExistingBuilding )
                        {
                            structureType.DuringGame_TheoreticalStructureRotation -= 90;
                            while ( structureType.DuringGame_TheoreticalStructureRotation < 0 )
                                structureType.DuringGame_TheoreticalStructureRotation += 360;
                        }
                    }
                    break;
                case "RotateStructureRight":
                    {
                        ArcenInput.BlockUntilNextFrame();
                        MachineStructureType structureType = TargetingTerritoryControlInvestigation != null ? CommonRefs.TerritoryControlFlag : TargetingStructure;
                        if ( structureType == null )
                        {
                            MachineJob job = TargetingJob;
                            structureType = job?.RequiredStructureType;
                        }

                        if ( structureType != null && !structureType.IsBuiltOnSiteOfExistingBuilding )
                        {
                            structureType.DuringGame_TheoreticalStructureRotation += 90;
                            while ( structureType.DuringGame_TheoreticalStructureRotation >= 360 )
                                structureType.DuringGame_TheoreticalStructureRotation -= 360;
                        }
                    }
                    break;
            }
        }

        public void TriggerSlotNumber( int SlotNumber )
        {
            
        }
    }
}

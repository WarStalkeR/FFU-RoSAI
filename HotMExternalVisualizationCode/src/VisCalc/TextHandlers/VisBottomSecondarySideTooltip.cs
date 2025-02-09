using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public static class VisBottomSecondarySideTooltip
    {
        #region DoPerFrame
        public static void DoPerFrame()
        {
            if ( TooltipRefs.LowerLeftBasicNovel.GetWasAlreadyDrawnThisFrame() )
                return;

            if ( Engine_Universal.NonModalPopupsSidePrimary.Count > 0 )
                return;

            if ( VisCurrent.IsShowingActualEvent || !FlagRefs.HasEmergedIntoMap.DuringGameplay_IsTripped || VisCurrent.IsShowingChapterChange )
                return; //we don't do any of this when in an event, or if not yet emerged into the city
            if ( (Window_MajorEventWindow.Instance?.GetShouldDrawThisFrame() ?? false) || 
                (Window_SimpleChoiceWindow.Instance?.GetShouldDrawThisFrame() ?? false) ||
                (Window_RewardWindow.Instance?.GetShouldDrawThisFrame() ?? false) ||
                (Window_Debate.Instance?.GetShouldDrawThisFrame() ?? false) ||
                (Window_NetworkNameWindow.Instance?.GetShouldDrawThisFrame() ?? false) )
                return;
            
            bool areInteractionModesAllowed = true;
            if ( VisCurrent.IsInPhotoMode )
                areInteractionModesAllowed = false; //do nothing!  No clicks, etc, in here

            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || 
                Engine_Universal.IsMouseOutsideGameWindow || !areInteractionModesAllowed )
            {
                return;
            }

            if ( Engine_HotM.CurrentLowerMode != null )
            {
                //todo on lower modes
                return;
            }

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    RenderBottomSecondarySideTheEndOfTimeTooltip();
                    return;
            }

            ISimMapActor selectedActor = Engine_HotM.SelectedActor;
            if ( selectedActor != null )//&& !InputCaching.IsInInspectMode )
            {
                ISimMapActor actorUnderCursor = MouseHelper.ActorUnderCursor; //use this rather than the thing from place because of filtering

                if ( !InputCaching.IsInInspectMode_ShowMoreStuff && BuildModeHandler.IsActiveAndTargeting && actorUnderCursor is MachineStructure )
                    return;

                if ( actorUnderCursor != null ) //if we are to here, we can trigger the highlights of it
                {
                    actorUnderCursor.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    return;
                }

                MachineStructure structure = MouseHelper.StructureUnderCursor;
                if ( structure != null )
                {
                    if ( !InputCaching.IsInInspectMode_ShowMoreStuff && BuildModeHandler.IsActiveAndTargeting )
                        return;
                    structure.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    return;
                }

                if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                {
                    ArcenGroundPoint worldCellPoint = Engine_HotM.CalculateWorldCellPointUnderMouse();
                    MapCell cellUnderMouse = CityMap.TryGetExistingCellAtLocation( worldCellPoint );

                    if ( cellUnderMouse != null )
                        MouseHelper.RenderMapCellTooltip( cellUnderMouse );
                }
                return; //if an actor is selected and we're not using machine vision, then do not do this kind of tooltip
            }

            {
                MachineStructure structure = MouseHelper.StructureUnderCursor;
                if ( structure != null )
                {
                    if ( !InputCaching.IsInInspectMode_ShowMoreStuff && BuildModeHandler.IsActiveAndTargeting )
                        return;
                    structure.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    return;
                }
            }

            RenderBottomSecondarySideTooltip();
        }
        #endregion

        #region RenderBottomSecondarySideTooltip
        private static void RenderBottomSecondarySideTooltip()
        {
            int debugStage = 0;
            try
            {
                debugStage = 11000;

                bool drewTooltipAlready = false;

                debugStage = 51000;

                ISimMapActor actor = MouseHelper.ActorUnderCursor; //use this rather than the thing from place because of filtering
                if ( actor != null ) //if we are to here, we can trigger the highlights of it
                {
                    actor.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                    return;
                }

                debugStage = 55000;

                ISimBuilding building = MouseHelper.BuildingUnderCursor; //use this rather than the thing from place because of filtering
                if ( building != null && !drewTooltipAlready && MouseHelper.CanTriggerBuildingHighlights() )
                {
                    MouseHelper.RenderBuildingTooltip( building );
                    drewTooltipAlready = true;
                }
                else
                {
                    MachineStructure structure = MouseHelper.StructureUnderCursor;
                    if ( structure != null )
                    {
                        if ( !InputCaching.IsInInspectMode_ShowMoreStuff && BuildModeHandler.IsActiveAndTargeting )
                            return;
                        structure.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
                        return;
                    }
                }

                debugStage = 61000;

                #region PlaceableUnderMouse (debugging, primarily)
                A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
                if ( place )
                {
                    MapItem mItem = place.GetCityMapItem();
                    if ( VisPlannerImplementation.DebugShowHoveredPlaceableTooltip || place.DebugMessageForTooltip.Length > 0 ||
                        ( mItem != null && mItem.DebugMessageForTooltip != null && mItem.DebugMessageForTooltip.Length > 0 ) )
                    {
                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartBasicTooltip( TooltipID.Create( "Building", "Debug" ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            lowerLeft.TitleUpperLeft.AddNeverTranslated( "Debug", true );

                            if ( VisPlannerImplementation.DebugShowHoveredPlaceableTooltip )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();

                                lowerLeft.Main.AddRaw( place.name );
                            }

                            if ( place.DebugMessageForTooltip.Length > 0 )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();

                                lowerLeft.Main.AddRaw( place.DebugMessageForTooltip );
                            }

                            if ( mItem != null && mItem.DebugMessageForTooltip != null && mItem.DebugMessageForTooltip.Length > 0 )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();

                                lowerLeft.Main.AddRaw( mItem.DebugMessageForTooltip );
                            }
                        }
                    }
                }
                #endregion

                debugStage = 72000;

                ArcenGroundPoint worldCellPoint = Engine_HotM.CalculateWorldCellPointUnderMouse();
                MapCell cellUnderMouse = CityMap.TryGetExistingCellAtLocation( worldCellPoint );

                if ( Engine_HotM.GameMode == MainGameMode.CityMap && !drewTooltipAlready && Engine_HotM.MarkableUnderMouse == null )
                {
                    debugStage = 72100;

                    #region Map Mode Cell Tooltip

                    if ( cellUnderMouse != null )
                        MouseHelper.RenderMapCellTooltip( cellUnderMouse );
                    #endregion
                }

                debugStage = 102000;


                if ( VisPlannerImplementation.DebugShowMousePosition || (cellUnderMouse?.DebugText?.Length ?? 0) > 0 || (cellUnderMouse?.ParentTile?.DebugText?.Length ?? 0) > 0 ||
                    VisPlannerImplementation.DebugShowHoveredWorldCellTooltip )
                {
                    LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                    if ( lowerLeft.TryStartBasicTooltip( TooltipID.Create( "MainGame", "Debug" ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                    {
                        lowerLeft.TitleUpperLeft.AddNeverTranslated( "Debug", true );

                        #region DebugShowMousePosition
                        if ( VisPlannerImplementation.DebugShowMousePosition )
                        {
                            //this is all never translated because it is only for debug purposes anyway
                            if ( !lowerLeft.Main.GetIsEmpty() )
                                lowerLeft.Main.Line();
                            lowerLeft.Main.AddNeverTranslated( "Screen (X,Y): ", true ).AddNeverTranslated( ArcenInput.MouseScreenX.ToStringWholeBasic(), true )
                                .AddNeverTranslated( ", ", true ).AddNeverTranslated( ArcenInput.MouseScreenY.ToStringWholeBasic(), true );

                            lowerLeft.Main.Line();

                            Vector3 worldLoc = Engine_HotM.MouseWorldLocation;
                            lowerLeft.Main.AddNeverTranslated( "WorldLoc (X,Y,Z): ", true ).AddNeverTranslated( worldLoc.x.ToStringWholeBasic(), true )
                                .AddNeverTranslated( ", ", true ).AddNeverTranslated( worldLoc.y.ToStringWholeBasic(), true ).AddNeverTranslated( ", ", true )
                                .AddNeverTranslated( worldLoc.z.ToStringWholeBasic(), true );

                            lowerLeft.Main.Line();

                            worldLoc = Engine_HotM.MouseWorldHitLocation;
                            lowerLeft.Main.AddNeverTranslated( "WorldHitLoc (X,Y,Z): ", true ).AddNeverTranslated( worldLoc.x.ToStringWholeBasic(), true )
                                    .AddNeverTranslated( ", ", true ).AddNeverTranslated( worldLoc.y.ToStringWholeBasic(), true ).AddNeverTranslated( ", ", true ).AddNeverTranslated( worldLoc.z.ToStringWholeBasic(), true );

                            lowerLeft.Main.Line();
                        }
                        #endregion

                        #region WorldCellUnderMouse
                        if ( (cellUnderMouse?.DebugText?.Length ?? 0) > 0 || (cellUnderMouse?.ParentTile?.DebugText?.Length ?? 0) > 0 )
                        {
                            if ( cellUnderMouse.DebugText.Length > 0 )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();
                                lowerLeft.Main.AddRaw( cellUnderMouse.DebugText );
                            }
                            if ( cellUnderMouse.ParentTile.DebugText.Length > 0 )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();
                                lowerLeft.Main.AddRaw( cellUnderMouse.ParentTile.DebugText );
                            }
                        }

                        if ( VisPlannerImplementation.DebugShowHoveredWorldCellTooltip )
                        {
                            if ( !lowerLeft.Main.GetIsEmpty() )
                                lowerLeft.Main.Line();

                            //never-translated because it's for debug only

                            lowerLeft.Main.AddNeverTranslated( "Cell-Coords: ", true ).AddNeverTranslated( worldCellPoint.X.ToString(), true )
                                .AddNeverTranslated( "x", true ).AddNeverTranslated( worldCellPoint.Z.ToString(), true );

                            if ( cellUnderMouse != null )
                            {
                                lowerLeft.Main.AddNeverTranslated( " (Occupied)", true );
                                lowerLeft.Main.AddNeverTranslated( " Seeded '", true ).AddNeverTranslated( cellUnderMouse.ParentTile?.SeedingLogic?.ID, true ).AddNeverTranslated( "'", true );
                            }
                            else
                                lowerLeft.Main.AddNeverTranslated( " (Empty)", true );

                            if ( cellUnderMouse != null )
                            {
                                lowerLeft.Main.Line().AddNeverTranslated( "LooseUnits: ", true ).AddNeverTranslated( cellUnderMouse.LooseUnitsInCell.Count.ToString(), true );
                                lowerLeft.Main.Line().AddNeverTranslated( "Collidables: ", true ).AddNeverTranslated( (cellUnderMouse?.CollidablesIntersectingCell?.Count ?? -1).ToString(), true );
                            }

                            if ( cellUnderMouse != null )
                            {
                                // lowerLeft.Main.Add( "Cell: " ).Add( cellUnderMouse.CellLocation.X ).Add( "x" ).Add( cellUnderMouse.CellLocation.Z );
                                lowerLeft.Main.Line();
                                lowerLeft.Main.AddNeverTranslated( cellUnderMouse.ParentTile.ToDebugStringWithConnectors(), true );
                                lowerLeft.Main.Line2x();
                                lowerLeft.Main.AddNeverTranslated( cellUnderMouse.ParentTile.LevelSourceFilenameForTile, true );
                                // lowerLeft.Main.AddNeverTranslated( "Tile Rot: " ).AddNeverTranslated( cellUnderMouse.ParentTile.Rotation.ToString() );
                                // lowerLeft.Main.AddNeverTranslated( " Mirr: " ).AddNeverTranslated( cellUnderMouse.ParentTile.Mirroring.IsX() ? "X" : String.Empty )
                                //     .AddNeverTranslated( cellUnderMouse.ParentTile.Mirroring.IsX() ? "Z" : String.Empty );
                            }
                        }
                        #endregion
                    }
                }


                debugStage = 153000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BottomRightPopupError", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        #region RenderBottomSecondarySideTheEndOfTimeTooltip
        private static void RenderBottomSecondarySideTheEndOfTimeTooltip()
        {
            int debugStage = 0;
            try
            {
                debugStage = 11000;

                debugStage = 51000;

                debugStage = 55000;

                debugStage = 61000;

                #region PlaceableUnderMouse
                A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
                if ( place )
                {
                    EndOfTimeItem endItem = place.GetEndOfTimeItem();
                    if ( endItem != null)
                    {
                        EndOfTimeSubItemPosition pos = endItem.GetNearestSubObjectToPoint( Engine_HotM.MouseWorldHitLocation, 5f ); //smaller than this and some will be missed
                        if ( pos.SlotIndex >= 0 )
                        {
                            CityTimeline timeline = endItem.GetCityAtSubObjectIndex( pos.SlotIndex );
                            if ( timeline != null )
                            {
                                timeline.RenderTimelineTooltip( null, SideClamp.Any, TooltipShadowStyle.None, false, TooltipExtraRules.None );

                                RenderManager_TheEndOfTime.TryDrawTimelineGhost( pos, timeline.GhostRoot );

                                if ( Engine_HotM.CurrentCheatOverridingClickMode == null )
                                {
                                    if ( ArcenInput.LeftMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //tooltip primary interact
                                    {
                                        //clicked a timeline when not in a cheat-overriding mode.
                                        Window_ExamineTimeline.TimelineToExamine = timeline;
                                        Window_ExamineTimeline.Instance.Open();
                                    }
                                }
                            }
                            //else
                            //    RenderManager_TheEndOfTime.TryDrawMetaCityGhost( pos, CommonRefs.EndOfTimeCityGhost );
                        }
                    }

                    if ( VisPlannerImplementation.DebugShowHoveredPlaceableTooltip || place.DebugMessageForTooltip.Length > 0 )
                    {
                        LowerLeftBuffer lowerLeft = LowerLeftBuffer.Instance;

                        if ( lowerLeft.TryStartBasicTooltip( TooltipID.Create( "EndOfTime", "Debug" ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                        {
                            lowerLeft.TitleUpperLeft.AddNeverTranslated( "Debug", true );

                            if ( VisPlannerImplementation.DebugShowHoveredPlaceableTooltip )
                                lowerLeft.Main.AddRaw( place.name );

                            if ( place.DebugMessageForTooltip.Length > 0 )
                            {
                                if ( !lowerLeft.Main.GetIsEmpty() )
                                    lowerLeft.Main.Line();

                                lowerLeft.Main.AddRaw( place.DebugMessageForTooltip );
                            }
                        }                        
                    }
                }
                #endregion

                debugStage = 72000;

                debugStage = 153000;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BottomRightTheEndOfTimePopupError", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public static class SharedRenderManagerData
    {
        internal static Stopwatch stopwatch = new Stopwatch();

        //internal static DrawShape_WireBox priorBox;
        //internal static bool hasBoxToDraw = false;

        internal static VisRangeAndSimilarDrawing RangeAndSimilarDrawer = null;

        public static void ComeBackAndFinishFrameBuffersAndRender_PostUI( Camera TargetCamera, Camera IconMixedInCamera, Camera IconOverlayCamera )
        {
            //whatever has been batched up, go ahead and render it
            RenderManager.Render( TargetCamera, IconMixedInCamera, IconOverlayCamera );
            FrameBufferManagerData.FinishFrameBuffers();

            mapCellsBeaconRingAverages.Clear();
            mapCellCenterBeacons.Clear();
        }

        public static void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            VisRangeAndSimilarDrawing.Instance?.Shapes?.Clear();
            RenderManager.PrepForRender();
        }

        #region DoUniversalClearBeforeNewFrame
        public static void DoUniversalClearBeforeNewFrame()
        {
            if ( RangeAndSimilarDrawer == null )
            {
                RangeAndSimilarDrawer = new VisRangeAndSimilarDrawing();
                Engine_HotM.Instance.RegisteredShapeDrawers.Add( RangeAndSimilarDrawer );
            }
            RangeAndSimilarDrawer.DoPerFrameLogicOnMainThread();
            VisRangeAndSimilarDrawing.Instance.Shapes.Clear();
            VisCurrent.PerFloorWireShapes.Clear();
            DrawHelper.ResetDrawPoolsForNewFrame();
            ThreatLineData.ResetForNewFrame();
        }
        #endregion

        internal static Color defaultColor_Global = Color.white;

        internal static readonly Vector3 highlight_ScaleMult = new Vector3( 1.02f, 1.02f, 1.02f );
        internal static readonly Vector3 highlightBorder_ScaleMult = new Vector3( 1.05f, 1.05f, 1.05f );

        internal static VisSimpleDrawingObject regionCube = null;

        internal static VisSimpleDrawingObject districtCube = null;
        internal static VisSimpleDrawingObject districtHoloWallNormal = null;
        internal static VisSimpleDrawingObject districtHoloWallMap = null;

        private static List<ISimBuilding> investigationBuildingsToRemove = List<ISimBuilding>.Create_WillNeverBeGCed( 400, "SharedRenderManagerData-investigationBuildingsToRemove" );

        #region ClearInvalidInvestigationBuildings
        public static void ClearInvalidInvestigationBuildings( Investigation investigation )
        {
            if ( investigation == null || !(investigation.Type?.Style?.IsTerritoryControlStyle??false) )
                return;

            investigationBuildingsToRemove.Clear();

            foreach ( KeyValuePair<ISimBuilding, bool> kv in investigation.PossibleBuildings )
            {
                ISimBuilding building = kv.Key;
                if ( building == null )
                    continue;
                if ( !investigation.GetIsValidBuilding( building ) )
                    investigationBuildingsToRemove.Add( building );
            }

            if ( investigationBuildingsToRemove.Count > 0 )
            {
                foreach ( ISimBuilding building in investigationBuildingsToRemove )
                    investigation.PossibleBuildings.Remove( building );
            }
        }
        #endregion

        #region DrawBeaconRingIfFirstInCell
        private static Dictionary<MapCell, bool> mapCellCenterBeacons = Dictionary<MapCell, bool>.Create_WillNeverBeGCed( 400, "SharedRenderManagerData-mapCellCenterBeacons" );

        public static void DrawBeaconRingIfFirstInCell( MapItem Item, VisColorUsage ColorUsage )
        {
            MapCell cell = Item.ParentCell;
            if ( cell == null )
                return;

            if ( mapCellCenterBeacons.ContainsKey( cell ) )
                return;
            mapCellCenterBeacons[cell] = true;

            Vector3 groundCenter = cell.Center;            

            DrawHelper.RenderRangeCircle( groundCenter.ReplaceY( Engine_HotM.GameMode == MainGameMode.CityMap ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0 ),
                9 + (4 * Mathf.Sin( 3 * ArcenTime.UnpausedTimeSinceStartF )), ColorUsage.ColorHDR );
        }
        #endregion

        #region DrawBeaconRing
        private static Dictionary<MapCell, BeaconAverageData> mapCellsBeaconRingAverages = Dictionary<MapCell, BeaconAverageData>.Create_WillNeverBeGCed( 400, "SharedRenderManagerData-mapCellsBeaconRingAverages" );

        public static void DrawBeaconRingOrAddToAverage( MapItem Item, int CountForRings, VisColorUsage ColorUsage )
        {
            MapCell cell = Item.ParentCell;
            if ( cell == null )
                return;

            Vector3 groundCenter = Item.GroundCenterPoint;

            if ( CountForRings <= 30 )
            {
                DrawHelper.RenderRangeCircle( groundCenter.ReplaceY( Engine_HotM.GameMode == MainGameMode.CityMap ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0 ),
                    9 + (4 * Mathf.Sin( 3 * ArcenTime.UnpausedTimeSinceStartF )), ColorUsage.ColorHDR );
                return;
            }

            if ( mapCellsBeaconRingAverages.TryGetValue( cell, out BeaconAverageData averageData ) )
            {
                averageData.SumX += groundCenter.x;
                averageData.SumZ += groundCenter.z;
                averageData.ItemCount++;
                mapCellsBeaconRingAverages[cell] = averageData; //struct, so must reassign
            }
            else
            {
                averageData.SumX = groundCenter.x;
                averageData.SumZ = groundCenter.z;
                averageData.ItemCount = 1;
                mapCellsBeaconRingAverages[cell] = averageData;
            }
        }

        public static void DrawAllExistingBeaconRings( VisColorUsage ColorUsage )
        {
            foreach ( KeyValuePair<MapCell, BeaconAverageData> kv in mapCellsBeaconRingAverages )
            {
                if ( kv.Value.ItemCount == 0 ) 
                    continue;
                Vector3 centroid = new Vector3( kv.Value.SumX / kv.Value.ItemCount,
                    Engine_HotM.GameMode == MainGameMode.CityMap ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0,
                    kv.Value.SumZ / kv.Value.ItemCount );

                DrawHelper.RenderRangeCircle( centroid, 9 + (4 * Mathf.Sin( 3 * ArcenTime.UnpausedTimeSinceStartF )), ColorUsage.ColorHDR );
            }
        }
        #endregion

        #region DrawRingAroundActor
        public static void DrawRingAroundActor( ISimMapActor Actor, VisColorUsage ColorUsage, float XZSize = 9, float AddedXZSize = 4f, float Speed = 3f )
        {
            DrawHelper.RenderRangeCircle( Actor.GetDrawLocation() .ReplaceY( Engine_HotM.GameMode == MainGameMode.CityMap ? RenderManager_Streets.EXTRA_Y_IN_MAP_MODE : 0 ),
                XZSize + (AddedXZSize * Mathf.Sin( Speed * ArcenTime.UnpausedTimeSinceStartF )), ColorUsage.ColorHDR );
        }
        #endregion

        #region DrawSelectionHexAroundActor
        public static void DrawSelectionHexAroundActor( ISimMapActor Actor, VisColorUsage ColorUsage, float XZSize, float AddedXZSize, float Speed, float Thickness, float yOffset )
        {
            DrawHelper.RenderRangeHex( Actor.GetBottomCenterPosition().PlusY( yOffset ),
                XZSize + (AddedXZSize * Mathf.Sin( Speed * ArcenTime.UnpausedTimeSinceStartF )), Quaternion.identity /*Quaternion.Euler( 0, Actor.GetRotationYForCollisions(), 0 )*/, 
                ColorUsage.ColorHDR, Thickness );
        }
        #endregion

        #region DrawDeploymentHexAtPoint
        public static void DrawDeploymentHexAtPoint( Vector3 Point, VisColorUsage ColorUsage, float XZSize, float AddedXZSize, float YAngle, float Speed, float Thickness, float yOffset )
        {
            DrawHelper.RenderRangeHex( Point.PlusY( yOffset ),
                XZSize + (AddedXZSize * Mathf.Sin( Speed * ArcenTime.UnpausedTimeSinceStartF )), Quaternion.Euler( 0, YAngle, 0 ),
                ColorUsage.ColorHDR, Thickness );
        }
        #endregion

        #region RenderDeploymentHexes
        public static void RenderDeploymentHexes( Vector3 Point, bool IsValid )
        {
            DrawDeploymentHexAtPoint( Point, IsValid ? ColorRefs.DeploymentHexAValid : ColorRefs.DeploymentHexABlocked,
                MathRefs.DeploymentHexA_XZ_Size.FloatMin, MathRefs.DeploymentHexA_XZ_AddedSize.FloatMin, MathRefs.DeploymentHexA_Y_Angle.FloatMin, MathRefs.DeploymentHexA_XZ_Speed.FloatMin,
                MathRefs.DeploymentHexA_XZ_Thickness.FloatMin, MathRefs.DeploymentHexA_Y_Offset.FloatMin );
            DrawDeploymentHexAtPoint( Point, IsValid ? ColorRefs.DeploymentHexBValid : ColorRefs.DeploymentHexBBlocked,
                MathRefs.DeploymentHexB_XZ_Size.FloatMin, MathRefs.DeploymentHexB_XZ_AddedSize.FloatMin, MathRefs.DeploymentHexB_Y_Angle.FloatMin, MathRefs.DeploymentHexB_XZ_Speed.FloatMin,
                MathRefs.DeploymentHexB_XZ_Thickness.FloatMin, MathRefs.DeploymentHexB_Y_Offset.FloatMin );
            DrawDeploymentHexAtPoint( Point, IsValid ? ColorRefs.DeploymentHexCValid : ColorRefs.DeploymentHexCBlocked,
                MathRefs.DeploymentHexC_XZ_Size.FloatMin, MathRefs.DeploymentHexC_XZ_AddedSize.FloatMin, MathRefs.DeploymentHexC_Y_Angle.FloatMin, MathRefs.DeploymentHexC_XZ_Speed.FloatMin,
                MathRefs.DeploymentHexC_XZ_Thickness.FloatMin, MathRefs.DeploymentHexC_Y_Offset.FloatMin );
        }
        #endregion

        public static Indicator CurrentIndicator = Indicator.None;

        #region RecalculateIndicators
        public static void RecalculateIndicators()
        {
            Indicator priorIndicator = CurrentIndicator;
            CurrentIndicator = Indicator.None; //will not cause a flicker because this is all main-thread

            if ( Window_Debate.Instance.IsOpen && !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
            {
                if ( !FlagRefs.HasBeenAskedAboutDebateTour.DuringGameplay_IsTripped || Engine_Universal.CurrentPopups.Count > 0 )
                { } //nothing yet
                else if ( !FlagRefs.DebateTour1_Target.DuringGameplay_IsTripped )
                {
                    CurrentIndicator = Indicator.DebateTour1_Target;
                    return;
                }
                else if ( !FlagRefs.DebateTour2_Failures.DuringGameplay_IsTripped )
                {
                    CurrentIndicator = Indicator.DebateTour2_Failures;
                    return;
                }
                else if ( !FlagRefs.DebateTour3_ActiveSlot.DuringGameplay_IsTripped )
                {
                    CurrentIndicator = Indicator.DebateTour3_ActiveSlot;
                    return;
                }
                else if ( !FlagRefs.DebateTour4_Discard.DuringGameplay_IsTripped )
                {
                    CurrentIndicator = Indicator.DebateTour4_Discard;
                    return;
                }
                else if ( !FlagRefs.DebateTour5_Queue.DuringGameplay_IsTripped )
                { 
                    CurrentIndicator = Indicator.DebateTour5_Queue;
                    return;
                }
                else if ( !FlagRefs.DebateTour6_Bonus.DuringGameplay_IsTripped )
                { 
                    CurrentIndicator = Indicator.DebateTour6_Bonus;
                    return;
                }
                else if ( !FlagRefs.DebateTour7_Opponent.DuringGameplay_IsTripped )
                { 
                    CurrentIndicator = Indicator.DebateTour7_Opponent;
                    return;
                }
                else if ( !FlagRefs.DebateTour8_FinalAdvice.DuringGameplay_IsTripped )
                { 
                    CurrentIndicator = Indicator.DebateTour8_FinalAdvice;
                    return;
                }
                else
                {
                    FlagRefs.HasFinishedDebateTour.TripIfNeeded();
                    ParticleSoundRefs.SmallForwardChoice.DuringGame_Play();
                }
            }

            if ( FlagRefs.Ch0_DejaVu.DuringGameplay_IsWaitingNow )
                return;

            if ( Window_Handbook.Instance.IsOpen || 
                Window_PlayerHardware.Instance.IsOpen ||
                Window_PlayerResources.Instance.IsOpen ||
                Window_PlayerHistory.Instance.IsOpen )
                return;

            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
            {
                if ( !FlagRefs.HasBeenAskedAboutUITour.DuringGameplay_IsTripped )
                { } //nothing yet
                else if ( !FlagRefs.UITour1_LeftHeader.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour1_LeftHeader;
                else if ( !FlagRefs.UITour2_RightHeader.DuringGameplay_IsTripped )
                {
                    if ( Window_RecentEvents.Instance.IsOpen )
                        return;
                    CurrentIndicator = Indicator.UITour2_RightHeader;
                }
                else if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour3_TaskStack;
                else if ( !FlagRefs.UITour4_RadialMenu.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour4_RadialMenu;
                else if ( !FlagRefs.UITour5_Toast.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour5_Toast;
                else if ( !FlagRefs.UITour6_UnitSidebar.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour6_UnitSidebar;
                else if ( !FlagRefs.UITour7_AbilityBar.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.UITour7_AbilityBar;
                else
                {
                    FlagRefs.HasFinishedUITour.TripIfNeeded();
                    ParticleSoundRefs.SmallForwardChoice.DuringGame_Play();
                }
            }
            else if ( Window_Debate.Instance.IsOpen && !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
            {
                CurrentIndicator = Indicator.None;
            }
            else if ( FlagRefs.IndicateToast.DuringGameplay_IsTripped )
                CurrentIndicator = Indicator.Toast;
            else
            {
                MachineStructure selectedStructure = Engine_HotM.SelectedActor as MachineStructure;

                if ( FlagRefs.Ch0_FindSafety.DuringGameplay_State == CityTaskState.Active && SimCommon.CurrentCityLens != CommonRefs.StreetSenseLens )
                    CurrentIndicator = Indicator.RadialStreetSense;
                else if ( FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_State == CityTaskState.Active && SimCommon.CurrentCityLens != CommonRefs.StreetSenseLens )
                    CurrentIndicator = Indicator.RadialStreetSense;
                else if ( FlagRefs.Ch0_FindSafety.DuringGameplay_State == CityTaskState.Active && Engine_HotM.GameMode != MainGameMode.CityMap )
                    CurrentIndicator = Indicator.MapButton_StreetSense;
                else if ( FlagRefs.HasUnlockedInvestigations.DuringGameplay_IsTripped && !FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped &&
                    SimCommon.CurrentCityLens != CommonRefs.InvestigationsLens && SimCommon.VisibleAndroidInvestigations.Count > 0 &&
                    FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_State == CityTaskState.Complete )
                    CurrentIndicator = Indicator.RadialInvestigation;
                else if ( FlagRefs.IndicateBuildMenuButton.DuringGameplay_IsTripped )
                {
                    if ( Engine_HotM.SelectedMachineActionMode == CommonRefs.BuildMode )
                        FlagRefs.IndicateBuildMenuButton.UnTripIfNeeded();
                    else
                        CurrentIndicator = Indicator.BuildMenuButton;
                }
                else if ( FlagRefs.IndicateMapButton.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.MapButton_Investigate;
                else if ( FlagRefs.IndicateFirstOuterRadialButton.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.FirstOuterRadialButton;
                else if ( FlagRefs.IndicateCommandModeButton.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.CommandModeButton;
                else if ( !FlagRefs.HasAlreadyIndicatedForcesSidebar.DuringGameplay_IsTripped &&
                    (SimCommon.AllMachineActors.Count >= 4 || FlagRefs.Ch0_SpreadToMoreAndroids.DuringGameplay_State == CityTaskState.Complete) )
                    CurrentIndicator = Indicator.ForcesSidebar;
                else if ( !FlagRefs.HasAlreadyIndicatedMapRadialContemplation.DuringGameplay_IsTripped && SimCommon.ContemplationsAvailable.Count > 0 && Engine_HotM.GameMode == MainGameMode.CityMap &&
                    CommonRefs.ContemplationsLens.GetIsLensVisible() )
                    CurrentIndicator = Indicator.MapRadialContemplation;
                else if ( SimCommon.PointAtThisCountdown != null )
                {
                    if ( SimCommon.PointAtThisCountdown.DuringGameplay_TurnsRemaining > 0 )
                        CurrentIndicator = Indicator.PointAtCountdown;
                    else
                        SimCommon.PointAtThisCountdown = null;
                }
                else if ( FlagRefs.HasUnlockedSeeingTheVirtualWorld.DuringGameplay_IsTripped && !FlagRefs.HasGoneIntoTheVirtualWorld.DuringGameplay_IsTripped )
                    CurrentIndicator = Indicator.VirtualWorldButton;
                else if ( !FlagRefs.HasAlreadyIndicatedTheEndOfTime.DuringGameplay_IsTripped && VisCommands.GetCanSeeEndOfTime( true, false ) )
                    CurrentIndicator = Indicator.EndOfTimeButton;
                else if ( !FlagRefs.HasAlreadyIndicatedTheEndOfTimeCreatorMode.DuringGameplay_IsTripped && VisCommands.GetCanSeeEndOfTime( true, false ) )
                    CurrentIndicator = Indicator.EndOfTimeCreatorLens;
            }

            if ( CurrentIndicator == Indicator.ForcesSidebar )
            {
                if ( ( Window_Sidebar.Instance.IsOpen && Window_Sidebar.Instance.CurrentTab == CommonRefs.ForcesSidebar ) || SimMetagame.CurrentChapterNumber > 0  )
                    FlagRefs.HasAlreadyIndicatedForcesSidebar.TripIfNeeded();
            }

            if ( CurrentIndicator == Indicator.None && priorIndicator != Indicator.None )
            {
                TooltipRefs.ArrowIndicatorDark.ClearImmediately();
                TooltipRefs.ArrowIndicatorDark.ClearImmediately();
            }
        }
        #endregion

        private struct BeaconAverageData
        {
            public float SumX;
            public float SumZ;
            public int ItemCount;
        }
    }
}

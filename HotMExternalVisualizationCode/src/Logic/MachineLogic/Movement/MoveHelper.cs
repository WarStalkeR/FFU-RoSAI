using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public static class MoveHelper
    {
        #region CalculateRotationForMove
        public static Quaternion CalculateRotationForMove( Quaternion Rotation, Vector3 OriginalPosition, Vector3 TheoreticalPosition )
        {
            Quaternion rot = Rotation;
            float dist = (OriginalPosition - TheoreticalPosition).sqrMagnitude;
            if ( dist > 0.01f )
            {
                Vector3 targetLook = TheoreticalPosition - OriginalPosition;
                targetLook.y = 0f;
                rot = Quaternion.LookRotation( targetLook, Vector3.up );
            }
            return rot;
        }
        #endregion

        #region RenderVehicleTypeColoredForMoveTarget
        public static void RenderVehicleTypeColoredForMoveTarget( MachineVehicleType vehicleType, Vector3 Position, Quaternion TheoreticalRotation, VisColorUsage Color )
        {
            if ( vehicleType == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            if ( Position.x == float.NegativeInfinity || Position.x == float.PositiveInfinity )
                return;

            A5RendererGroup rendGroup = vehicleType.VisObjectToDraw.RendererGroup as A5RendererGroup;
            if ( rendGroup == null )
                return;

            Vector3 drawPos = Position.PlusY( vehicleType.VisObjectExtraOffset + vehicleType.ExtraOffsetForIconAndObject );

            {
                float scalePart = vehicleType.VisObjectScale;// / 50f;
                Vector3 scale = new Vector3( scalePart, scalePart, scalePart );

                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( drawPos, TheoreticalRotation, scale,
                    RenderColorStyle.HighlightColor, Color.ColorHDR,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

        }
        #endregion

        #region RenderBuildingColoredForBuildTarget
        public static void RenderBuildingColoredForBuildTarget( BuildingPrefab Prefab, Vector3 Position, int TheoreticalRotation, VisColorUsage Color )
        {
            if ( Prefab == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            if ( float.IsInfinity( Position.x ) || float.IsNaN( Position.x ) )
                return;

            A5ObjectRoot root = Prefab.PlaceableRoot;

            IA5RendererGroup rendGroup = root.FirstRendererOfThisRoot?.Group;
            if ( rendGroup == null )
                return;

            Matrix4x4 parentMatrix = rendGroup.WriteToDrawBufferForOneFrame_BasicColor( Position, Quaternion.Euler( 0, TheoreticalRotation, 0 ), root.OriginalScale,
                RenderColorStyle.HighlightColor, Color.ColorHDR,
                RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it

            if ( root.SecondarysRenderersOfThisRoot.Count > 0 )
            {
                Quaternion rot;
                for ( int i = 0; i < root.SecondarysRenderersOfThisRoot.Count; i++ )
                {
                    SecondaryRenderer secondaryRend = root.SecondarysRenderersOfThisRoot[i];
                    rendGroup = secondaryRend.Group;
                    if ( rendGroup == null )
                        continue;

                    rot = secondaryRend.LocalRot;
                    if ( secondaryRend.Rotates )
                        rot *= secondaryRend.RotationForInGameGlobal;

                    rendGroup.WriteToDrawBufferForOneFrame_BasicColor( secondaryRend.LocalPos, rot, secondaryRend.LocalScale,
                        parentMatrix, RenderColorStyle.HighlightColor, Color.ColorHDR,
                        RenderOpacity.Normal, false );
                }
            }

        }
        #endregion

        #region RenderUnitTypeColoredForMoveTarget
        public static void RenderUnitTypeColoredForMoveTarget( MachineUnitType unitType, Vector3 Position, Quaternion TheoreticalRotation, VisColorUsage Color, bool DrawAggressive )
        {
            if ( unitType == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            if ( Position.x == float.NegativeInfinity || Position.x == float.PositiveInfinity )
                return;

            VisLODDrawingObject drawingData = DrawAggressive ? unitType.VisObjectAggressive : unitType.VisObjectCasual;
            if ( drawingData == null )
                return;

            Vector3 pos = Position.PlusY( unitType.VisObjectExtraOffset );

            float distSquared = (CameraCurrent.CameraBodyPosition - pos).sqrMagnitude;
            int lodToDraw = -1;
            List<float> lodDistanceSquares = drawingData.GetEffectiveDistanceSquares();
            for ( int i = 0; i < lodDistanceSquares.Count; i++ )
            {
                if ( distSquared <= lodDistanceSquares[i] )
                {
                    lodToDraw = i;
                    break;
                }
            }

            switch ( lodToDraw )
            {
                case 0:
                    FrameBufferManagerData.LOD0Count.Construction++;
                    break;
                case 1:
                    FrameBufferManagerData.LOD1Count.Construction++;
                    break;
                case 2:
                    FrameBufferManagerData.LOD2Count.Construction++;
                    break;
                case 3:
                    FrameBufferManagerData.LOD3Count.Construction++;
                    break;
                case 4:
                default:
                    FrameBufferManagerData.LOD4Count.Construction++;
                    break;
            }

            if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
            {
                FrameBufferManagerData.LODCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            {
                float scalePart = unitType.VisObjectScale;// / 50f;
                Vector3 scale = new Vector3( scalePart, scalePart, scalePart );

                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, TheoreticalRotation, scale,
                    RenderColorStyle.HighlightColor, Color.ColorHDR,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

        }
        #endregion

        #region RenderNPCUnitColoredForMoveTarget
        public static void RenderNPCUnitColoredForMoveTarget( NPCUnitType UnitType, Vector3 Position, Quaternion TheoreticalRotation, VisColorUsage Color )
        {
            if ( UnitType == null )
                return;
            if ( Engine_Universal.IsMouseOverGUI || Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General || Engine_Universal.IsMouseOutsideGameWindow )
                return;

            if ( Position.x == float.NegativeInfinity || Position.x == float.PositiveInfinity )
                return;

            VisLODDrawingObject drawingData = UnitType.DrawingObjectTag.LODObjects.FirstOrDefault;
            if ( drawingData == null )
                return; //todo, if we ever care about the kind that are based on simple drawing objects, add that

            Vector3 pos = Position.PlusY( UnitType.VisObjectExtraOffset );

            float distSquared = (CameraCurrent.CameraBodyPosition - pos).sqrMagnitude;
            int lodToDraw = -1;
            List<float> lodDistanceSquares = drawingData.GetEffectiveDistanceSquares();
            for ( int i = 0; i < lodDistanceSquares.Count; i++ )
            {
                if ( distSquared <= lodDistanceSquares[i] )
                {
                    lodToDraw = i;
                    break;
                }
            }

            switch ( lodToDraw )
            {
                case 0:
                    FrameBufferManagerData.LOD0Count.Construction++;
                    break;
                case 1:
                    FrameBufferManagerData.LOD1Count.Construction++;
                    break;
                case 2:
                    FrameBufferManagerData.LOD2Count.Construction++;
                    break;
                case 3:
                    FrameBufferManagerData.LOD3Count.Construction++;
                    break;
                case 4:
                default:
                    FrameBufferManagerData.LOD4Count.Construction++;
                    break;
            }

            if ( lodToDraw < 0 || lodToDraw >= drawingData.LODRenderGroups.Count )
            {
                FrameBufferManagerData.LODCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }

            A5RendererGroup rendGroup = drawingData.LODRenderGroups[lodToDraw] as A5RendererGroup;
            if ( rendGroup == null )
            {
                //ArcenDebugging.LogSingleLine( "Draw unit fail: rendGroup null", Verbosity.DoNotShow );
                return;
            }

            {
                float scalePart = UnitType.VisObjectScale;// / 50f;
                Vector3 scale = new Vector3( scalePart, scalePart, scalePart );

                rendGroup.WriteToDrawBufferForOneFrame_BasicColor( pos, TheoreticalRotation, scale,
                    RenderColorStyle.HighlightColor, Color.ColorHDR,
                    RenderOpacity.Normal, false ); //already transparent if it will be, don't need to mess with it
            }

        }
        #endregion

        #region DrawThreatLinesAgainstMapMobileActor
        public static void DrawThreatLinesAgainstMapMobileActor( ISimMapMobileActor Actor, Vector3 drawnDestination,
            ISimBuilding BuildingUnitWillBeAtOrNull, bool UnitWillHaveMoved,
            out int NextTurn_EnemySquadInRange, out int NextTurn_EnemiesTargeting, out AttackAmounts NextTurn_DamageFromEnemies,
            out int AttackOfOpportunity_EnemySquadInRange, out int AttackOfOpportunity_EnemiesTargeting, out int AttackOfOpportunity_MinDamage, out int AttackOfOpportunity_MaxDamage,
            ISimUnit AttackedUnitByPlayer, AttackAmounts PredictedAttack, EnemyTargetingReason Reason, bool SkipThreatLinesFromDestination, ThreatLineLogic Logic )
        {
            if ( Actor == null || Actor.IsFullDead )
            {
                NextTurn_EnemySquadInRange = 0;
                NextTurn_EnemiesTargeting = 0;
                NextTurn_DamageFromEnemies = AttackAmounts.Zero();
                AttackOfOpportunity_EnemySquadInRange = 0;
                AttackOfOpportunity_EnemiesTargeting = 0;
                AttackOfOpportunity_MinDamage = 0;
                AttackOfOpportunity_MaxDamage = 0;
                return;
            }

            //this is the part that gives us the numbers we want above, for tooltips and such
            ThreatLineData.HandleCalculationsWithoutDrawingYet( Actor, drawnDestination, BuildingUnitWillBeAtOrNull, UnitWillHaveMoved,
                out NextTurn_EnemySquadInRange, out NextTurn_EnemiesTargeting, out NextTurn_DamageFromEnemies,
                out AttackOfOpportunity_EnemySquadInRange, out AttackOfOpportunity_EnemiesTargeting, out AttackOfOpportunity_MinDamage, out AttackOfOpportunity_MaxDamage,
                AttackedUnitByPlayer as ISimNPCUnit, PredictedAttack, Reason, SkipThreatLinesFromDestination, Logic );

            //this is the part that will actually cause lines to be drawn, later
            ThreatLineData.SetVariableActorInfo( Actor, drawnDestination, BuildingUnitWillBeAtOrNull, UnitWillHaveMoved, AttackedUnitByPlayer as ISimNPCUnit, 
                PredictedAttack, Reason, SkipThreatLinesFromDestination, Logic );
        }
        #endregion

        #region UnpackActionFromBuildingForActor
        public static void UnpackActionFromBuildingForActor( ISimMachineUnit Unit, ISimBuilding IsTargetingBuilding, bool UseStanceDefaultWhenEmpty,
            ref LocationActionType actionToTake, ref NPCEvent actionToTakeEventOrNull, ref ProjectOutcomeStreetSenseItem actionToTakeStreetItemOrNull, ref string actionToTakeOtherOptionalID )
        {
            if ( IsTargetingBuilding == null )
                return;

            Investigation investigation = SimCommon.GetEffectiveCurrentInvestigation();
            if ( investigation != null && investigation.Type != null )
            {
                string investigationTypeID = investigation.Type?.ID;
                if ( investigation.PossibleBuildings.ContainsKey( IsTargetingBuilding ) )
                {
                    actionToTake = MathRefs.AndroidInvestigate;
                    actionToTakeEventOrNull = null;
                    actionToTakeStreetItemOrNull = null;
                    actionToTakeOtherOptionalID = investigationTypeID;
                    return;
                }
            }

            bool contemplateActive = (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false);
            bool explorationSiteActive = (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false);
            bool cityConflictActive = (SimCommon.CurrentCityLens?.ShowCityConflictsLarge?.Display ?? false) || (SimCommon.CurrentCityLens?.ShowCityConflictsSmall?.Display ?? false);

            if ( contemplateActive )
            {
                if ( IsTargetingBuilding.GetCurrentContemplationThatShouldShowOnMap() != null )
                {
                    actionToTake = MathRefs.AndroidContemplate;
                    actionToTakeEventOrNull = null;
                    actionToTakeStreetItemOrNull = null;
                    actionToTakeOtherOptionalID = string.Empty;
                    return;
                }
            }

            if ( explorationSiteActive )
            {
                if ( IsTargetingBuilding.GetCurrentExplorationSiteThatShouldShowOnMap() != null )
                {
                    actionToTake = MathRefs.AndroidExploreSite;
                    actionToTakeEventOrNull = null;
                    actionToTakeStreetItemOrNull = null;
                    actionToTakeOtherOptionalID = string.Empty;
                    return;
                }
            }

            if ( cityConflictActive )
            {
                if ( IsTargetingBuilding.CurrentCityConflict.Display != null )
                {
                    actionToTake = MathRefs.AndroidEnterCityConflict;
                    actionToTakeEventOrNull = null;
                    actionToTakeStreetItemOrNull = null;
                    actionToTakeOtherOptionalID = string.Empty;
                    return;
                }
            }

            {
                StreetSenseDataAtBuilding streetSenseDataOrNull = IsTargetingBuilding.GetCurrentStreetSenseActionThatShouldShowOnMap();
                if ( streetSenseDataOrNull?.ActionType != null )
                {
                    actionToTake = streetSenseDataOrNull?.ActionType;
                    if ( actionToTake == null )
                    { } //nothing to do
                    else
                    {
                        if ( actionToTake.DuringGame_BlockedUntilTurn > SimCommon.Turn )
                            actionToTake = null;
                        else
                        {
                            actionToTakeEventOrNull = streetSenseDataOrNull?.EventOrNull;
                            actionToTakeStreetItemOrNull = streetSenseDataOrNull?.ProjectItemOrNull;
                            actionToTakeOtherOptionalID = streetSenseDataOrNull?.OtherOptionalID;

                            if ( actionToTakeStreetItemOrNull != null && !actionToTakeStreetItemOrNull.DuringGame_CanStillDo() )
                                actionToTakeStreetItemOrNull = null;
                        }
                    }
                }
                else
                    actionToTake = null;
            }

            if ( actionToTake == null && UseStanceDefaultWhenEmpty )
            {
                actionToTake = Unit.Stance?.ActionOnBuildingInRangeNoActionArrive;
            }
        }
        #endregion

        public static int MaxClearanceNotToCareAbout = 1; //we don't care if the level is only clearance 1

        #region MachineAndroid_ActualDestinationIsValid
        public static bool MachineAndroid_ActualDestinationIsValid( ISimMachineActor Actor, ref Vector3 DestinationPoint, ref bool CanMoveIntoRestrictedAreas, out bool IsBlocked,
            out MapItem IsToBuilding, out SecurityClearance RequiredClearance, out bool IsIntersectingRoad, out bool IsBlockedBySecurityClearance, out string DebugText )
        {
            IsBlocked = false;
            IsToBuilding = null;
            RequiredClearance = null;
            DebugText = string.Empty;
            IsIntersectingRoad = false;
            IsBlockedBySecurityClearance = false;

            if ( Actor == null )
                return false;

            bool contemplateActive = ( SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false);
            bool explorationSiteActive = (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false);

            int debugStage = 0;
            try
            {
                debugStage = 1200;
                if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) )
                    return false;
                debugStage = 1300;
                MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
                if ( cell == null )
                    return false;

                debugStage = 1500;
                MapTile tile = cell.ParentTile;
                if ( tile == null )
                    return false;
                debugStage = 1800;

                if ( Actor.OutcastLevel >= MathRefs.OutcastLevelAtWhichMachineActorsStopCaringAboutPOIClearance.IntMin )
                    CanMoveIntoRestrictedAreas = true;

                debugStage = 3100;
                //if there is a tile-wide poi
                if ( (tile.POIOrNull?.Type?.RequiredClearance?.Level ?? 0) > MaxClearanceNotToCareAbout )
                {
                    debugStage = 3200;
                    foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                    {
                        if ( actor == null || actor.IsInvalid || actor.IsCloaked || actor.IsFullDead )
                            continue; //if cloaked or dead, nevermind
                        if ( actor.CalculateMapCell()?.ParentTile != tile )
                            continue; //nevermind, skip; not this tile

                        //one of the player's units/vehicles is already in here and not cloaked (don't care if it's this one).
                        //so further movement is fine for now
                        CanMoveIntoRestrictedAreas = true;
                        break;
                    }
                }

                SecurityClearance highestClearance = null;

                debugStage = 5100;
                ISimBuilding buildingMouseIsOver = null;

                if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated auto )
                {
                    if ( auto.GetCurrentRelated() is ISimBuilding build )
                    {
                        buildingMouseIsOver = build;
                        CanMoveIntoRestrictedAreas = true;
                    }
                }

                if ( buildingMouseIsOver == null )
                {
                    if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                    {
                        debugStage = 5200;
                        if ( Engine_HotM.MarkableUnderMouse != null )
                            buildingMouseIsOver = ((Engine_HotM.MarkableUnderMouse as IMarkableAutoRelated)?.GetCurrentRelated() as ISimBuilding);
                        if ( buildingMouseIsOver == null )
                        {
                            Investigation investigation = SimCommon.GetEffectiveCurrentInvestigation();
                            if ( investigation != null )
                            {
                                debugStage = 6100;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float closestDistance = 99999;
                                foreach ( KeyValuePair<ISimBuilding, bool> kv in investigation.PossibleBuildings )
                                {
                                    debugStage = 6200;
                                    MapItem item = kv.Key.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    InGameOBBCache cache = item.OBBCache;
                                    ArcenFloatRectangle rect = cache.GetOuterRect();
                                    if ( rect.ContainsPointXZ( DestinationPoint ) )
                                    {
                                        buildingMouseIsOver = kv.Key;
                                        break; //literally in the footprint of this, so do it
                                    }

                                    //snap to the closest item when investigating
                                    float distance = (DestinationPoint - cache.Center).GetSquareGroundMagnitude();
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = kv.Key;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else if ( contemplateActive )
                            {
                                debugStage = 9100;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float closestDistance = 99999;
                                foreach ( ISimBuilding building in SimCommon.ContemplationsBuildings.GetDisplayList() )
                                {
                                    debugStage = 9200;
                                    MapItem item = building.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    InGameOBBCache cache = item.OBBCache;
                                    ArcenFloatRectangle rect = cache.GetOuterRect();
                                    if ( rect.ContainsPointXZ( DestinationPoint ) )
                                    {
                                        buildingMouseIsOver = building;
                                        break; //literally in the footprint of this, so do it
                                    }

                                    //snap to the closest item when contemplating
                                    float distance = (DestinationPoint - cache.Center).GetSquareGroundMagnitude();
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = building;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else if ( explorationSiteActive )
                            {
                                debugStage = 9100;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float closestDistance = 99999;
                                foreach ( ISimBuilding building in SimCommon.ExplorationSiteBuildings.GetDisplayList() )
                                {
                                    debugStage = 9200;
                                    MapItem item = building.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    InGameOBBCache cache = item.OBBCache;
                                    ArcenFloatRectangle rect = cache.GetOuterRect();
                                    if ( rect.ContainsPointXZ( DestinationPoint ) )
                                    {
                                        buildingMouseIsOver = building;
                                        break; //literally in the footprint of this, so do it
                                    }

                                    //snap to the closest item when looking for exploration sites
                                    float distance = (DestinationPoint - cache.Center).GetSquareGroundMagnitude();
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = building;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else
                            {
                                if ( (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false) ||
                                    (SimCommon.CurrentCityLens?.ShowProjectRelatedStreetSense?.Display ?? false) )
                                {
                                    debugStage = 12100;
                                    //if there are other actions nearby, then snap to those
                                    debugStage = 12200;
                                    if ( buildingMouseIsOver != null && buildingMouseIsOver.GetCurrentStreetSenseActionThatShouldShowOnMap()?.ActionType != null )
                                    {
                                        debugStage = 12300;
                                        //if this building has an action at it
                                        CanMoveIntoRestrictedAreas = true; //allow us to go there
                                    }
                                    else
                                    {
                                        debugStage = 14100;
                                        Dictionary<int, ISimBuilding> possibleActions = SimCommon.CurrentStreetSenseBuildings.GetDisplayDict();
                                        if ( possibleActions.Count > 0 )
                                        {
                                            debugStage = 14200;
                                            float mustBeWithin = 5;
                                            float closestDistance = 99999;
                                            foreach ( KeyValuePair<int, ISimBuilding> kv in possibleActions )
                                            {
                                                debugStage = 14300;
                                                MapItem item = kv.Value.GetMapItem();
                                                if ( item == null )
                                                    continue;
                                                if ( item?.SimBuilding?.GetCurrentStreetSenseActionThatShouldShowOnMap() == null )
                                                    continue;

                                                float distance = (DestinationPoint - item.OBBCache.Center).GetSquareGroundMagnitude();
                                                if ( distance > mustBeWithin )
                                                    continue; //if not very close by, then skip

                                                //if close by and missing the correct building, snap to the nearest one of relevance
                                                if ( distance < closestDistance )
                                                {
                                                    buildingMouseIsOver = kv.Value;
                                                    closestDistance = distance;
                                                    CanMoveIntoRestrictedAreas = true; //allow us to go here
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        debugStage = 18100;
                        if ( buildingMouseIsOver == null )
                        {
                            debugStage = 18200;
                            foreach ( MapSubCell subCell in cell.SubCells )
                            {
                                debugStage = 18300;
                                ArcenFloatRectangle rect = subCell.SubCellRect;
                                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                    continue; //destination not in this subcell, so nevermind

                                debugStage = 18400;
                                foreach ( MapItem building in subCell.BuildingList.GetDisplayList() )
                                {
                                    rect = building.OBBCache.GetOuterRect();
                                    if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                        continue; //destination not in this building's footprint, so nevermind

                                    if ( building.SimBuilding != null )
                                    {
                                        buildingMouseIsOver = building.SimBuilding;
                                        break;
                                    }
                                }
                                if ( buildingMouseIsOver != null )
                                    break;
                            }
                        }
                    }
                    else //NOT the city map
                    {
                        debugStage = 26100;
                        buildingMouseIsOver = MouseHelper.BuildingNoFilterUnderCursor;
                        if ( buildingMouseIsOver == null )
                        {
                            ISimMapActor actorMousingOver = CursorHelper.FindMapActorUnderCursor();
                            if ( actorMousingOver != null && actorMousingOver == Actor && Actor is ISimMachineUnit unit )
                                buildingMouseIsOver = unit.ContainerLocation.Get() as ISimBuilding;
                        }

                        Investigation investigation = SimCommon.GetEffectiveCurrentInvestigation();
                        if ( investigation != null )
                        {
                            debugStage = 27100;
                            //if investigating, snap to nearby buildings
                            if ( buildingMouseIsOver == null || !investigation.PossibleBuildings.ContainsKey( buildingMouseIsOver ) )
                            {
                                debugStage = 27200;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float mustBeWithin = 5;
                                float closestDistance = 99999;
                                foreach ( KeyValuePair<ISimBuilding, bool> kv in investigation.PossibleBuildings )
                                {
                                    debugStage = 27300;
                                    MapItem item = kv.Key.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    debugStage = 27400;
                                    float distance = (DestinationPoint - item.OBBCache.Center).GetSquareGroundMagnitude();
                                    if ( distance > mustBeWithin )
                                        continue; //if not very close by, then skip

                                    //if close by and missing the correct building, snap to the nearest one of relevance
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = kv.Key;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else if ( buildingMouseIsOver != null && investigation.PossibleBuildings.ContainsKey( buildingMouseIsOver ) )
                            {
                                //if this building has an action at it
                                CanMoveIntoRestrictedAreas = true; //allow us to go there
                            }
                        }
                        else if ( contemplateActive )
                        {
                            debugStage = 29100;
                            //if contemplating, snap to nearby buildings
                            if ( buildingMouseIsOver == null || !SimCommon.ContemplationsBuildings.GetDisplayList().Contains( buildingMouseIsOver ) )
                            {
                                debugStage = 29200;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float mustBeWithin = 5;
                                float closestDistance = 99999;
                                foreach ( ISimBuilding building in SimCommon.ContemplationsBuildings )
                                {
                                    debugStage = 29300;
                                    MapItem item = building.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    float distance = (DestinationPoint - item.OBBCache.Center).GetSquareGroundMagnitude();
                                    if ( distance > mustBeWithin )
                                        continue; //if not very close by, then skip

                                    //if close by and missing the correct building, snap to the nearest one of relevance
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = building;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else if ( buildingMouseIsOver != null && SimCommon.ContemplationsBuildings.GetDisplayList().Contains( buildingMouseIsOver ) )
                            {
                                //if this building has an action at it
                                CanMoveIntoRestrictedAreas = true; //allow us to go there
                            }
                        }
                        else if ( explorationSiteActive )
                        {
                            debugStage = 29100;
                            //if exploring sites, snap to nearby buildings
                            if ( buildingMouseIsOver == null || !SimCommon.ExplorationSiteBuildings.GetDisplayList().Contains( buildingMouseIsOver ) )
                            {
                                debugStage = 29200;
                                IReadOnlyList<MapCell> adjacentCells = cell.AdjacentCells;
                                float mustBeWithin = 5;
                                float closestDistance = 99999;
                                foreach ( ISimBuilding building in SimCommon.ExplorationSiteBuildings )
                                {
                                    debugStage = 29300;
                                    MapItem item = building.GetMapItem();
                                    if ( item == null )
                                        continue;
                                    MapCell itemCell = item.ParentCell;
                                    if ( itemCell == null )
                                        continue;
                                    if ( itemCell != cell && !adjacentCells.Contains( itemCell ) )
                                        continue; //if not this cell or an adjacent one

                                    float distance = (DestinationPoint - item.OBBCache.Center).GetSquareGroundMagnitude();
                                    if ( distance > mustBeWithin )
                                        continue; //if not very close by, then skip

                                    //if close by and missing the correct building, snap to the nearest one of relevance
                                    if ( distance < closestDistance )
                                    {
                                        buildingMouseIsOver = building;
                                        closestDistance = distance;
                                        CanMoveIntoRestrictedAreas = true; //allow us to go here
                                    }
                                }
                            }
                            else if ( buildingMouseIsOver != null && SimCommon.ExplorationSiteBuildings.GetDisplayList().Contains( buildingMouseIsOver ) )
                            {
                                //if this building has an action at it
                                CanMoveIntoRestrictedAreas = true; //allow us to go there
                            }
                        }
                        else
                        {
                            debugStage = 35100;
                            debugStage = 35200;
                            if ( (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false) ||
                                (SimCommon.CurrentCityLens?.ShowProjectRelatedStreetSense?.Display ?? false) )
                            {
                                if ( buildingMouseIsOver != null && buildingMouseIsOver.GetCurrentStreetSenseActionThatShouldShowOnMap()?.ActionType != null )
                                {
                                    //if this building has an action at it
                                    CanMoveIntoRestrictedAreas = true; //allow us to go there
                                }
                                else
                                {
                                    debugStage = 36100;
                                    Dictionary<int, ISimBuilding> possibleActions = SimCommon.CurrentStreetSenseBuildings.GetDisplayDict();
                                    if ( possibleActions.Count > 0 && (buildingMouseIsOver == null || !possibleActions.ContainsKey( buildingMouseIsOver.GetBuildingID() )) )
                                    {
                                        debugStage = 36200;
                                        float mustBeWithin = 5;
                                        float closestDistance = 99999;
                                        foreach ( KeyValuePair<int, ISimBuilding> kv in possibleActions )
                                        {
                                            debugStage = 36300;
                                            MapItem item = kv.Value.GetMapItem();
                                            if ( item == null )
                                                continue;
                                            if ( item?.SimBuilding?.GetCurrentStreetSenseActionThatShouldShowOnMap() == null )
                                                continue;
                                            float distance = (DestinationPoint - item.OBBCache.Center).GetSquareGroundMagnitude();
                                            if ( distance > mustBeWithin )
                                                continue; //if not very close by, then skip

                                            //if close by and missing the correct building, snap to the nearest one of relevance
                                            if ( distance < closestDistance )
                                            {
                                                buildingMouseIsOver = kv.Value;
                                                closestDistance = distance;
                                                CanMoveIntoRestrictedAreas = true; //allow us to go here
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                debugStage = 44100;
                //if over a building, then adjust the point now and possibly also adjust the cell
                if ( buildingMouseIsOver != null )
                {
                    debugStage = 44200;
                    DestinationPoint = buildingMouseIsOver.GetMapItem().CenterPoint;
                    cell = buildingMouseIsOver.GetLocationMapCell();
                }

                debugStage = 47100;
                //if we collide with a restricted poi, keep that in mind
                foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
                {
                    if ( poi == null || poi.HasBeenDestroyed )
                        continue;

                    debugStage = 47200;
                    ArcenFloatRectangle rect = poi.GetOuterRect();

                    if ( !rect.ContainsPointXZ( DestinationPoint ) )
                        continue; //destination not in here, so nevermind

                    SecurityClearance clear = poi.Type?.RequiredClearance;
                    if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                        continue;

                    if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                        highestClearance = clear;
                }

                debugStage = 48100;
                if ( (buildingMouseIsOver?.MachineStructureInBuilding?.Type?.IsTerritoryControlFlag ?? false) )
                    return false;  //do not allow climbing on the flags!

                debugStage = 49100;
                //if we are hovering over a building, that is the building we will go to
                if ( buildingMouseIsOver != null )
                {
                    debugStage = 49200;
                    IsToBuilding = buildingMouseIsOver.GetMapItem();
                    debugStage = 49300;
                    SecurityClearance clear = buildingMouseIsOver?.GetVariant()?.RequiredClearance;
                    debugStage = 49500;
                    if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                        highestClearance = clear;

                    RequiredClearance = highestClearance;

                    if ( !CanMoveIntoRestrictedAreas )
                    {
                        debugStage = 50100;
                        if ( RequiredClearance != null && RequiredClearance.Level > Actor.GetEffectiveClearance( ClearanceCheckType.MovingToBuilding ) )
                        {
                            IsBlockedBySecurityClearance = true;
                            return false;
                        }
                    }
                    return true;
                }
                RequiredClearance = highestClearance;

                debugStage = 52100;
                //if we got to here, we're not mousing over a building apparently
                if ( !CanMoveIntoRestrictedAreas )
                {
                    debugStage = 52200;
                    if ( RequiredClearance != null && RequiredClearance.Level > MaxClearanceNotToCareAbout &&
                        RequiredClearance.Level > Actor.GetEffectiveClearance( ClearanceCheckType.MovingToNonBuilding ) )
                    {
                        IsBlockedBySecurityClearance = true;
                        return false;
                    }
                }

                debugStage = 56100;
                //we are allowed to stand on roads, but we must raise the unit a bit
                foreach ( MapItem road in cell.AllRoads )
                {
                    if ( road == null )
                        continue;
                    debugStage = 56200;
                    if ( !road.OBBCache.HasBeenSet )
                        road.FillOBBCache();

                    if ( road.OBBCache.GetOuterRect().ContainsPoint( DestinationPoint.x, DestinationPoint.z ) )
                    {
                        IsIntersectingRoad = true;
                        break;
                    }
                }

                debugStage = 59100;

                return true;
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "MachineAndroid_ActualDestinationIsValid", debugStage, e, Verbosity.ShowAsError );
                return false;
            }
        }
        #endregion

        #region MachineOther_ActualDestinationIsValid
        public static bool MachineOther_ActualDestinationIsValid( ISimMachineActor Actor, Vector3 DestinationPoint, int HeldClearanceInt, bool CanMoveIntoRestrictedAreas, out bool IsBlocked,
            out SecurityClearance RequiredClearance, out string DebugText )
        {
            IsBlocked = false;
            RequiredClearance = null;
            DebugText = string.Empty;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            if ( Actor.OutcastLevel >= MathRefs.OutcastLevelAtWhichMachineActorsStopCaringAboutPOIClearance.IntMin )
                CanMoveIntoRestrictedAreas = true;

            SecurityClearance highestClearance = null;

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }
            RequiredClearance = highestClearance;

            if ( !CanMoveIntoRestrictedAreas )
            {
                if ( RequiredClearance != null && RequiredClearance.Level > MaxClearanceNotToCareAbout &&
                    RequiredClearance.Level > HeldClearanceInt )
                    return false;
            }

            return true;
        }
        #endregion

        #region MachineNPC_ActualDestinationIsValid
        public static bool MachineNPC_ActualDestinationIsValid( ISimNPCUnit Actor, Vector3 DestinationPoint, int HeldClearanceInt, bool CanMoveIntoRestrictedAreas, out bool IsBlocked,
            out SecurityClearance RequiredClearance, out string DebugText )
        {
            IsBlocked = false;
            RequiredClearance = null;
            DebugText = string.Empty;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            if ( Actor.OutcastLevel >= MathRefs.OutcastLevelAtWhichMachineActorsStopCaringAboutPOIClearance.IntMin )
                CanMoveIntoRestrictedAreas = true;

            SecurityClearance highestClearance = null;

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }
            RequiredClearance = highestClearance;

            if ( !CanMoveIntoRestrictedAreas )
            {
                if ( RequiredClearance != null && RequiredClearance.Level > MaxClearanceNotToCareAbout &&
                    RequiredClearance.Level > HeldClearanceInt )
                    return false;
            }

            return true;
        }
        #endregion

        #region MachineAny_DestinationBlockedByLockdown
        public static bool MachineAny_DestinationBlockedByLockdown( Vector3 SourcePoint, Vector3 DestinationPoint, out Lockdown Lock )
        {
            if ( SimCommon.Lockdowns_MainThreadOnly.Count == 0 )
            {
                Lock = null;
                return false;
            }

            foreach ( Lockdown lockdown in SimCommon.Lockdowns_MainThreadOnly )
            {
                if ( lockdown == null )
                    continue;
                LockdownType lockdownType = lockdown.Type;
                if ( lockdownType == null )
                    continue;

                bool sourceIsInRange = (SourcePoint - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;
                bool destIsInRange = (DestinationPoint - lockdown.Position).GetSquareGroundMagnitude() <= lockdownType.RadiusSquared;

                if ( sourceIsInRange == destIsInRange )
                    continue; //not crossing the barrier in either direction, so skip!

                if ( !destIsInRange )
                {
                    //moving out of the lockdown area
                    if ( lockdownType.BlocksPlayerUnitsMovingOut )
                    {
                        Lock = lockdown;
                        return true;
                    }
                }
                else
                {
                    //moving into the lockdown area
                    if ( lockdownType.BlocksPlayerUnitsMovingIn )
                    {
                        Lock = lockdown;
                        return true;
                    }
                }
            }

            Lock = null;
            return false;
        }
        #endregion

        #region MachineBulkAndroid_ActualDestinationIsValid
        public static bool MachineBulkAndroid_ActualDestinationIsValid( NPCUnitType UnitType, ref Vector3 DestinationPoint, out bool IsBlocked,
            out MapItem IsToBuilding, out SecurityClearance RequiredClearance, out bool IsIntersectingRoad, out string DebugText )
        {
            IsBlocked = false;
            IsToBuilding = null;
            RequiredClearance = null;
            DebugText = string.Empty;
            IsIntersectingRoad = false;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            MapTile tile = cell.ParentTile;
            if ( tile == null )
                return false;

            SecurityClearance highestClearance = null;

            ISimBuilding buildingMouseIsOver = null;

            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated auto )
            {
                if ( auto.GetCurrentRelated() is ISimBuilding build )
                    buildingMouseIsOver = build;
            }

            if ( buildingMouseIsOver == null )
            {
                buildingMouseIsOver = MouseHelper.BuildingNoFilterUnderCursor;
                if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                {
                    if ( buildingMouseIsOver == null )
                    {
                        foreach ( MapSubCell subCell in cell.SubCells )
                        {
                            ArcenFloatRectangle rect = subCell.SubCellRect;
                            if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                continue; //destination not in this subcell, so nevermind

                            foreach ( MapItem building in subCell.BuildingList.GetDisplayList() )
                            {
                                rect = building.OBBCache.GetOuterRect();
                                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                    continue; //destination not in this building's footprint, so nevermind

                                if ( building.SimBuilding != null )
                                {
                                    buildingMouseIsOver = building.SimBuilding;
                                    break;
                                }
                            }
                            if ( buildingMouseIsOver != null )
                                break;
                        }
                    }
                }
            }

            //if over a building, then adjust the point now and possibly also adjust the cell
            if ( buildingMouseIsOver != null )
            {
                DestinationPoint = buildingMouseIsOver.GetMapItem().CenterPoint;
                cell = buildingMouseIsOver.GetLocationMapCell();
            }

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }

            if ( (buildingMouseIsOver?.MachineStructureInBuilding?.Type?.IsTerritoryControlFlag ?? false) )
                return false;  //do not allow climbing on the flags!

            //if we are hovering over a building, that is the building we will go to
            if ( buildingMouseIsOver != null )
            {
                IsToBuilding = buildingMouseIsOver.GetMapItem();
                SecurityClearance clear = buildingMouseIsOver?.GetVariant()?.RequiredClearance;
                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;

                RequiredClearance = highestClearance;
                return true;
            }
            RequiredClearance = highestClearance;

            //we are allowed to stand on roads, but we must raise the unit a bit
            foreach ( MapItem road in cell.AllRoads )
            {
                if ( road == null )
                    continue;
                if ( !road.OBBCache.HasBeenSet )
                    road.FillOBBCache();

                if ( road.OBBCache.GetOuterRect().ContainsPoint( DestinationPoint.x, DestinationPoint.z ) )
                {
                    IsIntersectingRoad = true;
                    break;
                }
            }

            return true;
        }
        #endregion

        #region MachineAndroid_ActualDestinationIsValid
        public static bool MachineAndroid_ActualDestinationIsValid( MachineUnitType UnitType, ref Vector3 DestinationPoint, out bool IsBlocked,
            out MapItem IsToBuilding, out SecurityClearance RequiredClearance, out bool IsIntersectingRoad, out string DebugText )
        {
            IsBlocked = false;
            IsToBuilding = null;
            RequiredClearance = null;
            DebugText = string.Empty;
            IsIntersectingRoad = false;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            MapTile tile = cell.ParentTile;
            if ( tile == null )
                return false;

            SecurityClearance highestClearance = null;

            ISimBuilding buildingMouseIsOver = null;

            if ( Engine_HotM.MarkableUnderMouse is IMarkableAutoRelated auto )
            {
                if ( auto.GetCurrentRelated() is ISimBuilding build )
                    buildingMouseIsOver = build;
            }

            if ( buildingMouseIsOver == null )
            {
                buildingMouseIsOver = MouseHelper.BuildingNoFilterUnderCursor;
                if ( Engine_HotM.GameMode == MainGameMode.CityMap )
                {
                    if ( buildingMouseIsOver == null )
                    {
                        foreach ( MapSubCell subCell in cell.SubCells )
                        {
                            ArcenFloatRectangle rect = subCell.SubCellRect;
                            if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                continue; //destination not in this subcell, so nevermind

                            foreach ( MapItem building in subCell.BuildingList.GetDisplayList() )
                            {
                                rect = building.OBBCache.GetOuterRect();
                                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                                    continue; //destination not in this building's footprint, so nevermind

                                if ( building.SimBuilding != null )
                                {
                                    buildingMouseIsOver = building.SimBuilding;
                                    break;
                                }
                            }
                            if ( buildingMouseIsOver != null )
                                break;
                        }
                    }
                }
            }        

            //if over a building, then adjust the point now and possibly also adjust the cell
            if ( buildingMouseIsOver != null )
            {
                DestinationPoint = buildingMouseIsOver.GetMapItem().CenterPoint;
                cell = buildingMouseIsOver.GetLocationMapCell();
            }

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }

            if ( (buildingMouseIsOver?.MachineStructureInBuilding?.Type?.IsTerritoryControlFlag ?? false) )
                return false;  //do not allow climbing on the flags!

            //if we are hovering over a building, that is the building we will go to
            if ( buildingMouseIsOver != null )
            {
                IsToBuilding = buildingMouseIsOver.GetMapItem();
                SecurityClearance clear = buildingMouseIsOver?.GetVariant()?.RequiredClearance;
                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;

                RequiredClearance = highestClearance;
                return true;
            }
            RequiredClearance = highestClearance;

            //we are allowed to stand on roads, but we must raise the unit a bit
            foreach ( MapItem road in cell.AllRoads )
            {
                if ( road == null )
                    continue;
                if ( !road.OBBCache.HasBeenSet )
                    road.FillOBBCache();

                if ( road.OBBCache.GetOuterRect().ContainsPoint( DestinationPoint.x, DestinationPoint.z ) )
                {
                    IsIntersectingRoad = true;
                    break;
                }
            }

            return true;
        }
        #endregion

        #region MachineMech_ActualDestinationIsValid
        public static bool MachineMech_ActualDestinationIsValid( MachineUnitType UnitType, ref Vector3 DestinationPoint, out bool IsBlocked,
            out SecurityClearance RequiredClearance, out string DebugText )
        {
            IsBlocked = false;
            RequiredClearance = null;
            DebugText = string.Empty;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            MapTile tile = cell.ParentTile;
            if ( tile == null )
                return false;

            SecurityClearance highestClearance = null;

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }

            RequiredClearance = highestClearance;

            return true;
        }
        #endregion

        #region MachineVehicle_ActualDestinationIsValid
        public static bool MachineVehicle_ActualDestinationIsValid( MachineVehicleType VehicleType, ref Vector3 DestinationPoint, out bool IsBlocked,
            out SecurityClearance RequiredClearance, out string DebugText )
        {
            IsBlocked = false;
            RequiredClearance = null;
            DebugText = string.Empty;
            if ( !CityMap.WorldBounds_Rect.ContainsPointXZ( DestinationPoint ) ) return false;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( DestinationPoint );
            if ( cell == null )
                return false;

            MapTile tile = cell.ParentTile;
            if ( tile == null )
                return false;

            SecurityClearance highestClearance = null;

            //if we collide with a restricted poi, keep that in mind
            foreach ( MapPOI poi in cell.ParentTile.RestrictedPOIs )
            {
                if ( poi == null || poi.HasBeenDestroyed )
                    continue;

                ArcenFloatRectangle rect = poi.GetOuterRect();

                if ( !rect.ContainsPointXZ( DestinationPoint ) )
                    continue; //destination not in here, so nevermind

                SecurityClearance clear = poi.Type.RequiredClearance;
                if ( (clear?.Level ?? 0) <= MaxClearanceNotToCareAbout )
                    continue;

                if ( highestClearance == null || (clear?.Level ?? 0) > (highestClearance?.Level ?? 0) )
                    highestClearance = clear;
            }

            RequiredClearance = highestClearance;

            return true;
        }
        #endregion

        #region CalculateSprint
        public static void CalculateSprint( float sqrDistToDest, float moveRange, out bool isBeyondEvenSprinting, out bool canAffordExtraEnergy, out int extraEnergyCostFromMovingFar )
        {
            isBeyondEvenSprinting = false;
            canAffordExtraEnergy = true; //because there is no extra energy yet
            extraEnergyCostFromMovingFar = 0;
            if ( sqrDistToDest > moveRange * moveRange )
            {
                bool foundSpot = false;
                for ( int extraMoveRatio = 1; extraMoveRatio < 10; extraMoveRatio++ )
                {
                    float totalMove = moveRange + (extraMoveRatio * moveRange);
                    if ( sqrDistToDest <= totalMove * totalMove )
                    {
                        extraEnergyCostFromMovingFar = extraMoveRatio;
                        foundSpot = true;
                        break;
                    }
                }

                if ( !foundSpot )
                    isBeyondEvenSprinting = true;

                if ( extraEnergyCostFromMovingFar > 0 )
                    canAffordExtraEnergy = extraEnergyCostFromMovingFar + 1 <= ResourceRefs.MentalEnergy.Current;
                else
                    canAffordExtraEnergy = false;
            }
        }
        #endregion

        #region RenderLockdownWarning
        public static void RenderLockdownWarning( TooltipID ToolID, Lockdown blockedByLockdown )
        {
            if ( blockedByLockdown == null )
                return;
            LockdownType blockedByLockdownType = blockedByLockdown.Type;
            if ( blockedByLockdownType == null )
                return;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( ToolID, null, SideClamp.Any,
                InputCaching.ShouldShowDetailedTooltips ? TooltipNovelWidth.Simple : TooltipNovelWidth.SizeToText ) )
            {
                novel.ShouldTooltipBeRed = true;
                novel.Icon = IconRefs.Mouse_BlockedByLockdown.Icon;
                novel.CanExpand = CanExpandType.Brief;
                novel.TitleUpperLeft.AddLang( "Move_BlockedByLockdown" );
                novel.MainHeader.AddRaw( blockedByLockdownType.GetDisplayName() );

                if ( InputCaching.ShouldShowDetailedTooltips )
                {
                    if ( !blockedByLockdownType.GetDescription().IsEmpty() )
                        novel.Main.AddRaw( blockedByLockdownType.GetDescription() ).Line();

                    if ( blockedByLockdownType.BlocksPlayerUnitsMovingIn && blockedByLockdownType.BlocksPlayerUnitsMovingOut )
                        novel.Main.AddLang( "Move_BlockedByLockdown_Both" );
                    else if ( blockedByLockdownType.BlocksPlayerUnitsMovingIn )
                        novel.Main.AddLang( "Move_BlockedByLockdown_InOnly" );
                    else
                        novel.Main.AddLang( "Move_BlockedByLockdown_OutOnly" );
                }
            }            
        }
        #endregion

        #region TryConsiderDrawingThreatLinesAgainst
        public static bool TryConsiderDrawingThreatLinesAgainst( ISimMachineActor Actor, bool IsHovered, ThreatLineLogic Logic )
        {
            if ( Actor == null )
                return false;

            if ( Actor.GetIsCurrentlyInvisible( InvisibilityPurpose.ForPlayerTargeting ) )
                return false;

            DrawThreatLinesAgainstMapMobileActor( Actor, Actor.GetDrawLocation().PlusY( Actor.GetHalfHeightForCollisions() ), null,
                    false, out CombatTextHelper.NextTurn_EnemySquadsTargeting, out CombatTextHelper.NextTurn_EnemiesTargeting, out CombatTextHelper.NextTurn_DamageFromEnemies,
                    out CombatTextHelper.AttackOfOpportunity_EnemySquadsTargeting, out CombatTextHelper.AttackOfOpportunity_EnemiesTargeting, 
                    out CombatTextHelper.AttackOfOpportunity_MinDamageFromEnemies, out CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies,
                    null, AttackAmounts.Zero(), EnemyTargetingReason.CurrentLocation_NoOp, false, Logic );

            return false;
        }
        #endregion
    }
}

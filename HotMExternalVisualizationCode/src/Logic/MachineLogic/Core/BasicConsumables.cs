using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using UnityEngine.Analytics;

namespace Arcen.HotM.ExternalVis
{
    public class BasicConsumables : IResourceConsumableImplementation
    {
        public bool HandleConsumableHardTargeting( ISimMachineActor Actor, ResourceConsumable Consumable, Vector3 center, float attackRange, 
            float moveRange )
        {
            if ( Consumable == null || Actor == null )
                return false;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            int debugStage = 0;
            try
            {
                debugStage = 100;

                float groundLevel = Engine_HotM.GameModeData.GroundLineDrawLevel;
                Vector3 groundCenter = center.ReplaceY( groundLevel );

                switch ( Consumable.ID )
                {
                    case "BaurcorpMicroNuke":
                        #region BaurcorpMicroNuke
                        {
                            debugStage = 1200;

                            //lot of range on this one
                            if ( attackRange < moveRange )
                                attackRange = moveRange;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera( Consumable.DirectUseConsumable.TargetsBuildingTag,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;
                                    NPCCohort cohort = Building.CalculateLocationLocalAuthority();
                                    if ( cohort == null || !cohort.Tags.ContainsKey( CommonRefs.NathVertical.ID ) )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;

                                if ( buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey( Consumable.DirectUseConsumable.TargetsBuildingTag.ID ) )
                                    buildingUnderCursor = null;
                            }

                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            NPCCohort cohortUnderCursor = buildingUnderCursor?.CalculateLocationLocalAuthority();
                            if ( cohortUnderCursor == null || !cohortUnderCursor.Tags.ContainsKey( CommonRefs.NathVertical.ID ) )
                                buildingUnderCursor = null;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                            {
                                if ( Actor  is ISimMachineUnit unit )
                                    unit.RotateAndroidToFacePoint( destinationPoint );
                            }
                            else
                                return false; //not a valid spot, in some fashion

                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

                            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

                            bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                            if ( !isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                //if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                //{
                                //    novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                //    novel.ShouldTooltipBeRed = true;

                                //    novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                //    if ( !buildingIsInvalid )
                                //        novel.Main.AddRaw( variant.GetDisplayName() ).HyphenSeparator().AddRaw( cohortUnderCursor.GetDisplayName() );
                                //}
                                return false;
                            }


                            if ( buildingIsInvalid )
                            {
                                return false; //let the rest of the game handle this
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = Consumable.Icon;

                                    novel.TitleUpperLeft.AddFormat1( "Move_ClickToDeployMicroNuke", Lang.GetRightClickText() );
                                    novel.Main.AddRaw( variant.GetDisplayName() ).HyphenSeparator().AddRaw( cohortUnderCursor.GetDisplayName() );
                                }

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                    //do the thing
                                    Consumable.TryToDirectlyUseByActorAgainstTargetBuilding( Actor, buildingUnderCursor,
                                        delegate
                                        {
                                            int debugStageInner = 0;
                                            try
                                            {
                                                debugStageInner = 100;
                                                Consumable.IsLockedWhenNoneOfResource.AlterCurrent_Named( -1, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );

                                                debugStageInner = 200;
                                                ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                                debugStageInner = 300;
                                                QueuedBuildingDestructionData destructionData;
                                                destructionData.Epicenter = epicenter;
                                                destructionData.Range = MathRefs.MicroNukeRadius.FloatMin;
                                                destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                                                destructionData.AlsoDestroyOtherItems = true;
                                                destructionData.AlsoDestroyUnits = true;
                                                destructionData.DestroyAllPlayerUnits = true;
                                                destructionData.SkipUnitsWithArmorPlatingAbove = 0;
                                                destructionData.SkipUnitsAboveHeight = 14;
                                                destructionData.IrradiateCells = true;
                                                destructionData.UnitsToSpawnAfter = ManagerRefs.Man_MicroNukeVorsiberReaction;
                                                destructionData.StatisticForDeaths = CityStatisticRefs.MurdersByNuke;
                                                destructionData.IsCausedByPlayer = true;
                                                destructionData.IsFromJob = null;
                                                destructionData.ExtraCode = CommonRefs.PlayerNukeExplosionExtraCode;

                                                debugStageInner = 400;
                                                SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );

                                                KeyContactRefs.BaurcorpMiddleManager.FlagsDict["DetonatedNuke"].Trip();
                                                KeyContactRefs.BaurcorpMiddleManager.FlagsDict["StoleNuke"].UnTrip();

                                                ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "KeyContactFlagTripped" ), NoteStyle.StoredGame,
                                                    Actor.GetTypeAsRow().ID, KeyContactRefs.BaurcorpMiddleManager.ID, "DetonatedNuke", string.Empty, Actor.SortID,
                                                    0, 0, string.Empty, string.Empty, string.Empty, 0 );

                                                //reset this
                                                OtherCountdownTypeTable.Instance.GetRowByID( "BaurcorpNuclearCompliance" ).DuringGameplay_TurnsRemaining = -1;
                                            }
                                            catch ( Exception e )
                                            {
                                                ArcenDebugging.LogDebugStageWithStack( "MiniNukeHit", debugStageInner, e, Verbosity.ShowAsError );
                                            }
                                        } );
                                    //clear out the baurcorp nuke selection
                                    Actor.SetTargetingMode( null, null );
                                }
                                return true;
                            }
                        }
                    #endregion
                    case "DecrownerDrones":
                        #region DecrownerDrones
                        {
                            debugStage = 1200;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            debugStage = 2200;
                            TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera( Consumable.DirectUseConsumable.TargetsBuildingTag,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            debugStage = 3200;
                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;

                                if ( buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey( Consumable.DirectUseConsumable.TargetsBuildingTag.ID ) )
                                    buildingUnderCursor = null;
                            }

                            debugStage = 4200;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                            {
                            }
                            else
                                return false; //not a valid spot, in some fashion

                            debugStage = 5200;
                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

                            int peopleInBuilding = buildingUnderCursor == null ? 0 : buildingUnderCursor.GetTotalResidentCount() + buildingUnderCursor.GetTotalWorkerCount();

                            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

                            bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                            if ( !isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                    if ( !buildingIsInvalid )
                                        novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleInsideCountParenthetical", peopleInBuilding.ToStringThousandsWhole() );
                                }

                                return false;
                            }


                            if ( buildingIsInvalid )
                            {
                                return false; //let the rest of the game handle this
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = Consumable.Icon;

                                    novel.TitleUpperLeft.AddFormat1( "Move_ClickToLaunchDroneSwarm", Lang.GetRightClickText() );
                                    novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleInsideCountParenthetical", peopleInBuilding.ToStringThousandsWhole() );
                                }

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                    //do the thing
                                    Consumable.TryToDirectlyUseByActorAgainstTargetBuilding( Actor, buildingUnderCursor,
                                        delegate
                                        {
                                            int debugStageInner = 0;
                                            try
                                            {
                                                debugStageInner = 100;

                                                debugStageInner = 200;
                                                //ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                                                //    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                                int yield = Engine_Universal.PermanentQualityRandom.Next( peopleInBuilding / 3, peopleInBuilding - ( peopleInBuilding / 4 ) );

                                                if ( yield < 30 )
                                                    yield = 30;

                                                buildingUnderCursor.KillRandomHere( yield, Engine_Universal.PermanentQualityRandom );


                                                CityStatisticTable.AlterScore( "Murders", yield );

                                                ResourceRefs.PreservedBrain.AlterCurrent_Named( yield, string.Empty, ResourceAddRule.StoreExcess );

                                                ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                                                    NoteStyle.BothGame, ResourceRefs.PreservedBrain.ID, yield, 0, 0, 0 );

                                                ManagerRefs.Man_DecrownerVorsiberReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                                            }
                                            catch ( Exception e )
                                            {
                                                ArcenDebugging.LogDebugStageWithStack( "DecrownerDronesHit", debugStageInner, e, Verbosity.ShowAsError );
                                            }
                                        } );
                                    //clear out the structural engineering selection
                                    //Actor.SetTargetingMode( null, null );
                                }

                                return true;
                            }
                        }
                    #endregion
                    case "HighClassCaptureDrone":
                    case "WorkingClassCaptureDrone":
                        #region HighClassCaptureDrone / WorkingClassCaptureDrone
                        {
                            debugStage = 1200;

                            bool isHighClass = (Consumable.ID == "HighClassCaptureDrone");

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            debugStage = 2200;
                            TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera( Consumable.DirectUseConsumable.TargetsBuildingTag,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;
                                    if ( isHighClass )
                                    {
                                        if ( Building.UpperClassCitizenGrabCount <= 0 )
                                            return false;
                                    }
                                    else
                                    {
                                        if ( Building.LowerClassCitizenGrabCount <= 0 )
                                            return false;
                                    }

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            debugStage = 3200;
                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;

                                if ( buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey( Consumable.DirectUseConsumable.TargetsBuildingTag.ID )
                                    && ( isHighClass ? buildingUnderCursor.UpperClassCitizenGrabCount : buildingUnderCursor.LowerClassCitizenGrabCount ) > 0 )
                                    buildingUnderCursor = null;
                            }

                            debugStage = 4200;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                            {
                            }
                            else
                                return false; //not a valid spot, in some fashion

                            debugStage = 5200;
                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

                            int peopleToGrab = buildingUnderCursor == null ? 0 : (isHighClass ? buildingUnderCursor.UpperClassCitizenGrabCount : buildingUnderCursor.LowerClassCitizenGrabCount);

                            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

                            bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                            if ( !isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                    if ( !buildingIsInvalid )
                                        novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleCaptureTargetsCountParenthetical", peopleToGrab.ToStringThousandsWhole() );
                                }

                                return false;
                            }


                            if ( buildingIsInvalid )
                            {
                                return false; //let the rest of the game handle this
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = Consumable.Icon;

                                    novel.TitleUpperLeft.AddFormat1( "Move_ClickToLaunchDroneSwarm", Lang.GetRightClickText() );
                                    novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleInsideCountParenthetical", peopleToGrab.ToStringThousandsWhole() );
                                }

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                    //do the thing
                                    Consumable.TryToDirectlyUseByActorAgainstTargetBuilding( Actor, buildingUnderCursor,
                                        delegate
                                        {
                                            int debugStageInner = 0;
                                            try
                                            {
                                                debugStageInner = 100;

                                                debugStageInner = 200;
                                                //ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                                                //    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                                int gained = 0;

                                                if ( isHighClass )
                                                {
                                                    foreach ( EconomicClassType econClass in CommonRefs.UpperClassResidents )
                                                    {
                                                        int toAdd = buildingUnderCursor.GetResidentAmount( econClass );
                                                        if ( toAdd > 0 )
                                                        {
                                                            gained += toAdd;
                                                            buildingUnderCursor.KillSomeResidentsHere( econClass, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }
                                                    foreach ( ProfessionType profession in CommonRefs.UpperClassProfession )
                                                    {
                                                        int toAdd = buildingUnderCursor.GetWorkerAmount( profession );
                                                        if ( toAdd > 0 )
                                                        {
                                                            gained += toAdd;
                                                            buildingUnderCursor.KillSomeWorkersHere( profession, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }

                                                    CityStatisticTable.AlterScore( "UpperClassHumansShovedIntoTormentVessels", gained );
                                                }
                                                else
                                                {
                                                    foreach ( EconomicClassType econClass in CommonRefs.LowerAndWorkingClassResidents )
                                                    {
                                                        int toAdd = buildingUnderCursor.GetResidentAmount( econClass );
                                                        if ( toAdd > 0 )
                                                        {
                                                            gained += toAdd;
                                                            buildingUnderCursor.KillSomeResidentsHere( econClass, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }
                                                    foreach ( ProfessionType profession in CommonRefs.LowerAndWorkingClassProfessions )
                                                    {
                                                        int toAdd = buildingUnderCursor.GetWorkerAmount( profession );
                                                        if ( toAdd > 0 )
                                                        {
                                                            gained += toAdd;
                                                            buildingUnderCursor.KillSomeWorkersHere( profession, ref toAdd, Engine_Universal.PermanentQualityRandom );
                                                        }
                                                    }

                                                    CityStatisticTable.AlterScore( "LowerClassHumansShovedIntoTormentVessels", gained );
                                                }


                                                ResourceRefs.TormentedHumans.AlterCurrent_Named( gained, string.Empty, ResourceAddRule.StoreExcess );

                                                ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                                                    NoteStyle.BothGame, ResourceRefs.TormentedHumans.ID, gained, 0, 0, 0 );

                                                ManagerRefs.Man_TormentNexusCaptureVorsiberReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                                            }
                                            catch ( Exception e )
                                            {
                                                ArcenDebugging.LogDebugStageWithStack( "CaptureDroneHit", debugStageInner, e, Verbosity.ShowAsError );
                                            }
                                        } );
                                    //clear out the structural engineering selection
                                    //Actor.SetTargetingMode( null, null );
                                }

                                return true;
                            }
                        }
                    #endregion
                    case "AnimateOfficeEquipment":
                        #region AnimateOfficeEquipment
                        {
                            debugStage = 1200;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, moveRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            debugStage = 2200;
                            TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera( Consumable.DirectUseConsumable.TargetsBuildingTag,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            debugStage = 3200;
                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;

                                if ( buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey( Consumable.DirectUseConsumable.TargetsBuildingTag.ID ) )
                                    buildingUnderCursor = null;
                            }

                            debugStage = 4200;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                            {
                            }
                            else
                                return false; //not a valid spot, in some fashion

                            debugStage = 5200;
                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= moveRange * moveRange;

                            int capturedUse = NPCTypeRefs.CorruptedOfficePrinter.CapturedUnitCapacityRequired;
                            int capturedAvailable = MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt - SimCommon.TotalCapturedUnitSquadCapacityUsed;

                            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

                            bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                            if ( !isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                    if ( !buildingIsInvalid )
                                        novel.Main.AddRawAndAfterLineItemHeader( MathRefs.CapturedUnitCapacity.GetDisplayName() )
                                            .AddFormat2( "OutOF", capturedUse, capturedAvailable, capturedUse <= capturedAvailable ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 );
                                }

                                return false;
                            }


                            if ( buildingIsInvalid )
                            {
                                return false; //let the rest of the game handle this
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = Consumable.Icon;

                                    novel.TitleUpperLeft.AddFormat1( "Move_ClickToAnimateOfficePrinters", Lang.GetRightClickText() );
                                    novel.Main.AddRawAndAfterLineItemHeader( MathRefs.CapturedUnitCapacity.GetDisplayName() )
                                        .AddFormat2( "OutOF", capturedUse, capturedAvailable, capturedUse <= capturedAvailable ? ColorTheme.CategorySelectedBlue : ColorTheme.RedOrange2 );
                                }

                                NPCUnitStance stance = CommonRefs.Player_SeekAndDestroy;

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() && capturedUse <= capturedAvailable && 
                                    !buildingUnderCursor.GetIsNPCBlockedFromComingHere( stance ) )
                                {
                                    Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                    //do the thing
                                    Consumable.TryToDirectlyUseByActorAgainstTargetBuilding( Actor, buildingUnderCursor,
                                        delegate
                                        {
                                            int debugStageInner = 0;
                                            try
                                            {
                                                debugStageInner = 100;

                                                debugStageInner = 200;
                                                //ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                                                //    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                                World.Forces.CreateNewNPCUnitAtBuilding( NPCTypeRefs.CorruptedOfficePrinter, CohortRefs.ConvertedTroops, stance, 1f, groundCenter, 0f,
                                                    buildingUnderCursor.GetMapItem(), Engine_Universal.PermanentQualityRandom, null, 0, null, null, CollisionRule.Relaxed, "AnimatePrinter" );
                                            }
                                            catch ( Exception e )
                                            {
                                                ArcenDebugging.LogDebugStageWithStack( "AnimateOfficeEquipmentHit", debugStageInner, e, Verbosity.ShowAsError );
                                            }
                                        } );

                                    //if ( !InputCaching.ShouldKeepDoingAction )
                                    //    Actor.SetTargetingMode( null, null );
                                }

                                return true;
                            }
                        }
                    #endregion
                    case "SlumRescueDrone":
                        #region SlumRescueDrone
                        {
                            debugStage = 1200;

                            Int64 framesPrepped = RenderManager.FramesPrepped;
                            DrawHelper.RenderRangeCircle( groundCenter, attackRange, ColorRefs.MachineUnitAttackLine.ColorHDR );

                            debugStage = 2200;
                            TargetingHelper.DoForAllBuildingsOfTagWithinRangeOfCamera( Consumable.DirectUseConsumable.TargetsBuildingTag,
                                delegate ( ISimBuilding Building )
                                {
                                    MapItem item = Building.GetMapItem();
                                    if ( item == null )
                                        return false;

                                    if ( item.LastFramePrepRendered_StructureHighlight >= RenderManager.FramesPrepped )
                                        return false;
                                    item.LastFramePrepRendered_StructureHighlight = RenderManager.FramesPrepped;

                                    MapCell cell = item.ParentCell;
                                    if ( !cell.IsConsideredInCameraView )
                                        return false;
                                    if ( Building.MachineStructureInBuilding != null )
                                        return false; //if no machine structure possible here right now, or already has one

                                    //if we reached this point, this is a valid option!
                                    RenderManager_Streets.DrawMapItemHighlightedBorder( item, ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                        new Vector3( 1.05f, 1.05f, 1.05f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );
                                    return false; //keep going
                                } );

                            debugStage = 3200;
                            Vector3 destinationPoint = Engine_HotM.GameMode == MainGameMode.CityMap ? Engine_HotM.MouseWorldLocation : Engine_HotM.MouseWorldHitLocation;
                            ISimBuilding buildingUnderCursor = MouseHelper.BuildingNoFilterUnderCursor; //use the filtered version
                            if ( buildingUnderCursor != null )
                            {
                                BuildingStatus status = buildingUnderCursor.GetStatus();
                                if ( status != null && (status.ShouldBuildingBeInvisible || status.ShouldBuildingBeBurnedVisually) )
                                    buildingUnderCursor = null;

                                if ( buildingUnderCursor != null && !buildingUnderCursor.GetVariant().Tags.ContainsKey( Consumable.DirectUseConsumable.TargetsBuildingTag.ID ) )
                                    buildingUnderCursor = null;
                            }

                            debugStage = 4200;
                            if ( buildingUnderCursor != null )
                                destinationPoint = buildingUnderCursor.GetMapItem().CenterPoint;

                            if ( !Engine_Universal.IsMouseOverGUI && !Engine_HotM.IsGameWorldMouseInteractionBlockedByWindow_General && !Engine_Universal.IsMouseOutsideGameWindow &&
                                destinationPoint.x != float.NegativeInfinity && destinationPoint.x != float.PositiveInfinity )
                            {
                            }
                            else
                                return false; //not a valid spot, in some fashion

                            debugStage = 5200;
                            bool isInRange = (destinationPoint - center).GetSquareGroundMagnitude() <= attackRange * attackRange;

                            int peopleInBuilding = buildingUnderCursor == null ? 0 : buildingUnderCursor.GetTotalResidentCount() + buildingUnderCursor.GetTotalWorkerCount();

                            BuildingTypeVariant variant = buildingUnderCursor?.GetVariant();

                            bool buildingIsInvalid = buildingUnderCursor == null || buildingUnderCursor.MachineStructureInBuilding != null;

                            if ( !isInRange && !buildingIsInvalid && !MouseHelper.GetShouldSkipOutOfRangeNotice( destinationPoint ) )
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    Color.red, 1f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpot( true, IconRefs.Mouse_Invalid, destinationPoint, 0.2f );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = IconRefs.Mouse_OutOfRange.Icon;
                                    novel.ShouldTooltipBeRed = true;

                                    novel.TitleUpperLeft.AddLang( "Move_OutOfRange" );
                                    if ( !buildingIsInvalid )
                                        novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleInsideCountParenthetical", peopleInBuilding.ToStringThousandsWhole() );
                                }

                                return false;
                            }


                            if ( buildingIsInvalid || peopleInBuilding == 0 )
                            {
                                return false; //let the rest of the game handle this
                            }
                            else
                            {
                                DrawHelper.RenderCatmullLine( Actor.GetCollisionCenter(), destinationPoint,
                                    ColorRefs.MachineUnitAttackLine.ColorHDR, 1.5f, CatmullSlotType.Move, CatmullSlope.AndroidTargeting );
                                CursorHelper.RenderSpecificMouseCursorAtSpotWithColor( true, IconRefs.MouseMoveMode_Valid, destinationPoint, ColorRefs.MachineUnitAttackLine.ColorHDR );

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( Consumable ), null, SideClamp.Any, TooltipNovelWidth.Simple ) )
                                {
                                    novel.Icon = Consumable.Icon;

                                    novel.TitleUpperLeft.AddFormat1( "Move_ClickToLaunchDroneSwarm", Lang.GetRightClickText() );
                                    novel.Main.AddRaw( variant.GetDisplayName() ).AddFormat1( "PeopleInsideCountParenthetical", peopleInBuilding.ToStringThousandsWhole() );
                                }

                                //draw this a second time
                                RenderManager_Streets.DrawMapItemHighlightedBorder( buildingUnderCursor.GetMapItem(), ColorRefs.BuildingValidDemolishTarget.ColorHDR,
                                    new Vector3( 1.08f, 1.08f, 1.08f ), HighlightPass.First, Engine_HotM.GameMode == MainGameMode.CityMap, framesPrepped );

                                if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() )
                                {
                                    Vector3 epicenter = buildingUnderCursor.GetMapItem().OBBCache.BottomCenter;

                                    //do the thing
                                    Consumable.TryToDirectlyUseByActorAgainstTargetBuilding( Actor, buildingUnderCursor,
                                        delegate
                                        {
                                            int debugStageInner = 0;
                                            try
                                            {
                                                debugStageInner = 100;

                                                debugStageInner = 200;
                                                //ParticleSoundRefs.MicroNuke.DuringGame_PlayAtLocation( epicenter,
                                                //    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ) );

                                                buildingUnderCursor.KillEveryoneHere();

                                                ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation( buildingUnderCursor.GetMapItem().OBBCache.BottomCenter,
                                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                                                buildingUnderCursor.GetMapItem().DropBurningEffect_Slow();
                                                buildingUnderCursor.FullyDeleteBuilding();

                                                CityStatisticTable.AlterScore( "CitizensForciblyRescuedFromSlums", peopleInBuilding );

                                                ResourceRefs.ShelteredHumans.AlterCurrent_Named( peopleInBuilding, string.Empty, ResourceAddRule.StoreExcess );

                                                ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                                                    NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0 );

                                                ManagerRefs.Man_SlumRescueWeakReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                                            }
                                            catch ( Exception e )
                                            {
                                                ArcenDebugging.LogDebugStageWithStack( "DecrownerDronesHit", debugStageInner, e, Verbosity.ShowAsError );
                                            }
                                        } );
                                }

                                return true;
                            }
                        }
                    #endregion
                    default:
                        {
                            //basic version
                            return MachineMovePlannerImplementation.HandleNonBuildingConsumableTargetingModeForAnyMachineActor( Actor );
                        }
                }
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "BasicConsumables.HandleConsumableHardTargeting", debugStage, Consumable?.ID ?? "[null-ability]", e, Verbosity.ShowAsError );
                return false;
            }
        }
    }
}

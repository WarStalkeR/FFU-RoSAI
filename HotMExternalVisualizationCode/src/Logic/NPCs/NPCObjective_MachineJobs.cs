using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class NPCObjective_MachineJobs : INPCUnitObjectiveImplementation
    {
        public bool TrySetThisObjectiveForNPCUnit( ISimNPCUnit Unit, NPCUnitObjective Objective, NPCActionConsideration Consideration, MersenneTwister Rand )
        {
            return NPCActionHelper.Basic_TrySetThisObjectiveForNPCUnit( Unit, Objective, Consideration, Rand );
        }

        public bool TryPursueNextObjectiveForNPCUnit( ISimNPCUnit Unit, MersenneTwister Rand, NPCUnitStanceLogic StanceLogic )
        {
            return NPCActionHelper.TryPursueNextObjective( Unit, Rand, StanceLogic );
        }

        public bool DoCurrentObjectiveLogicForNPCUnit( ISimNPCUnit Unit, MersenneTwister Rand )
        {
            NPCUnitObjective Objective = Unit?.CurrentObjective;
            if ( Objective == null || Unit == null )
                return false;

            switch ( Objective.ID )
            {
                case "TentConversion":
                    {
                        ResourceType shelteredHumans = ResourceRefs.ShelteredHumans;
                        if ( shelteredHumans.EffectiveHardCapStorageAvailable > 0 )
                        {
                            NPCActionHelper.Basic_HandleObjectiveCompletion( Unit, Objective, Rand );
                            return true; //this one should just finish immediately, it's very simple.  If there's room!
                        }
                        else
                            return false; //if there's no room, then it can't work!
                    }
                case "LeaveAreaAndFurnish":
                    {
                        ActorDataType furnished = ActorRefs.FurnishedApartments;
                        if ( furnished.DuringGameplay_StructuresUsingThis.Count > 0 )
                        {
                            bool hadAnyRoom = false;
                            foreach ( MachineStructure structure in furnished.DuringGameplay_StructuresUsingThis.GetDisplayList() )
                            {
                                int available = structure.GetActorDataLostFromMax( ActorRefs.FurnishedApartments, true );
                                if ( available > 0 )
                                {
                                    hadAnyRoom = true;
                                    break;
                                }
                            }
                            if ( !hadAnyRoom )
                                return false;
                            NPCActionHelper.Basic_HandleObjectiveCompletion( Unit, Objective, Rand );
                            return true; //this one should just finish immediately, it's very simple.  If there's room!
                        }
                        else
                            return false; //if there's no room, then it can't work!
                    }
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_MachineJobs: Called DoCurrentObjectiveLogicForNPCUnit for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
            return false;
        }

        public void DoObjectiveCompleteLogicForNPCUnit( ISimNPCUnit Unit, NPCUnitObjective Objective, MersenneTwister Rand, bool DoVisualsAndSound )
        {
            if ( Objective == null || Unit == null )
                return;

            bool didSuccessfullyComplete = false;

            switch ( Objective.ID )
            {
                #region TentConversion
                case "TentConversion":
                    {
                        ResourceType shelteredHumans = ResourceRefs.ShelteredHumans;
                        if ( shelteredHumans.EffectiveHardCapStorageAvailable <= 0 || 
                            (JobRefs.HousingAgency?.DuringGame_NumberFunctional?.Display??0) > 0 ) //if housing agency now around, stop the shelter coordinators doing anything
                            break;
                        {
                            int humansToMoveIn = 0;
                            int homelessTentsReplacedWithTrees = 0;
                            //homeless people moving into our housing
                            EconomicClassType homelessClass = EconomicClassTypeTable.Instance.GetRowByID( "Homeless" );
                            MapCell centerCell = Unit.CalculateMapCell();
                            bool stopped = false;
                            foreach ( MapCell cell in centerCell.AdjacentCellsAndSelfIncludingDiagonal )
                            {
                                if ( cell.BuildingList.GetDisplayList().Count > 0 )
                                {
                                    for ( int i = cell.BuildingList.GetDisplayList().Count - 1; i >= 0; i--)
                                    {
                                        MapItem buildingItem = cell.BuildingList.GetDisplayList()[i];
                                        if ( buildingItem.IsInPoolAtAll )
                                            continue;

                                        ISimBuilding building = buildingItem.SimBuilding;
                                        if ( building == null )
                                            continue;
                                        if ( !building.GetVariant().Tags.ContainsKey( "HomelessTent" ) )
                                        {
                                            //MapGlowingIndicator indicator = new MapGlowingIndicator();
                                            //indicator.Position = buildingItem.Position;
                                            //indicator.Rotation = buildingItem.Rotation;
                                            //indicator.Scale = buildingItem.Scale;
                                            //indicator.ColorForHighlight = ColorRefs.BuildingGlowSkip.ColorHDR;
                                            //indicator.ItemRoot = buildingItem.Type;
                                            //indicator.RemainingTimeToLive = 10f;
                                            //indicator.ApplyScalesAndOffsets( 0f, 0.6f, 1f );
                                            //MapEffectCoordinator.AddMapGlowingIndicator( cell, indicator );

                                            continue; //skip any non-tents
                                        }

                                        int residentCount = building.GetResidentAmount( homelessClass );
                                        if ( residentCount + humansToMoveIn > shelteredHumans.EffectiveHardCapStorageAvailable )
                                        {
                                            stopped = true;
                                            break; //stop for now, in that case
                                        }

                                        humansToMoveIn += residentCount;
                                        building.KillEveryoneHere(); //from the normal perspective we are killing, but really they are moving out of the human society
                                        Vector3 position = buildingItem.GroundCenterPoint;
                                        Quaternion rotation = buildingItem.rawReadRot;

                                        BiomeType biome = buildingItem.ParentTile?.District?.Biome;
                                        buildingItem.DropBurningEffect();
                                        building.FullyDeleteBuilding(); //note!  a foreach moving forward would miss items because of this!
                                        homelessTentsReplacedWithTrees++;

                                        {
                                            MapGlowingIndicator indicator = new MapGlowingIndicator();
                                            indicator.Position = buildingItem.rawReadPos;
                                            indicator.Rotation = rotation;
                                            indicator.Scale = buildingItem.Scale;
                                            indicator.ColorForHighlight = ColorRefs.BuildingRemovedAura.ColorHDR;
                                            indicator.ItemRoot = buildingItem.Type;
                                            indicator.RemainingTimeToLive = 4f;
                                            indicator.ApplyScalesAndOffsets( 0f, 1f, 1f );
                                            MapEffectCoordinator.AddMapGlowingIndicator( cell, indicator );
                                        }

                                        //add an outdoor spot that units can move to like they used to move to the tent
                                        if ( cell != null )
                                        {
                                            MapOutdoorSpot outdoorSpot = MapOutdoorSpot.GetFromPoolOrCreate_NotFromSavegame( cell );
                                            outdoorSpot.IsOnRoad = false;
                                            outdoorSpot.Position = position.ReplaceY( MapOutdoorSpot.BASE_PLACEMENT_HEIGHT_OFFROAD );
                                            cell.AllOutdoorSpots.Add( outdoorSpot );
                                        }

                                        //plant a tree
                                        if ( biome != null )
                                        {
                                            A5ObjectRoot treeToPlant = biome.TreeDrawBag.PickRandom( Rand );
                                            if ( treeToPlant != null && cell != null )
                                            {
                                                MapItem item = MapItem.GetFromPoolOrCreate_NotFromSavegame( cell );
                                                item.Type = treeToPlant;
                                                item.SetPosition( position );
                                                item.SetRotation( rotation );
                                                item.Scale = treeToPlant.OriginalScale;
                                                item.FillOBBCache();
                                                cell.PlaceMapItemIntoCell( treeToPlant.ExtraPlaceableData.IsMinorDecoration ? TileDest.DecorationMinor : TileDest.DecorationMajor, item, false );
                                            }
                                            else if ( treeToPlant == null )
                                                ArcenDebugging.LogSingleLine( "Null treeToPlant for planting a tree!", Verbosity.ShowAsError );
                                            else if ( cell == null )
                                                ArcenDebugging.LogSingleLine( "Null cell for planting a tree!", Verbosity.ShowAsError );
                                        }
                                        else
                                            ArcenDebugging.LogSingleLine( "Null biome for planting a tree!", Verbosity.ShowAsError );
                                    }
                                }
                                if ( stopped )
                                    break;
                            }

                            //move the people into our structures
                            if ( humansToMoveIn > 0 )
                            {
                                shelteredHumans.AlterCurrent_Named( humansToMoveIn, "Increase_HomelessMovingOutOfTents", ResourceAddRule.IgnoreUntilTurnChange );
                                CityStatisticTable.AlterScore( "HomelessIndividualsHoused", humansToMoveIn );
                            }
                            if ( homelessTentsReplacedWithTrees > 0 )
                            {
                                CityStatisticTable.AlterScore( "HomelessTentsReplacedWithTrees", homelessTentsReplacedWithTrees );
                                didSuccessfullyComplete = true;
                            }
                        }
                    }
                    break;
                #endregion TentConversion
                #region LeaveAreaAndFurnish
                case "LeaveAreaAndFurnish":
                    {
                        int furnishingsToAdd = MathRefs.FurnishingsGatheredPerCrowd.GetRandomBetweenInclusive( Rand );

                        ActorDataType furnished = ActorRefs.FurnishedApartments;
                        if ( furnished.DuringGameplay_StructuresUsingThis.Count > 0 )
                        {
                            while ( furnishingsToAdd > 0 )
                            {
                                int leastMissing = 9999999;
                                MapActorData leastMissingData = null;
                                foreach ( MachineStructure structure in furnished.DuringGameplay_StructuresUsingThis.GetDisplayList() )
                                {
                                    MapActorData data = structure.GetActorDataData( ActorRefs.FurnishedApartments, true );
                                    int lost = data.LostFromMax;
                                    if ( data != null && data.LostFromMax > 0 )
                                    {
                                        if ( leastMissingData == null || lost < leastMissing )
                                        {
                                            leastMissing = lost;
                                            leastMissingData = data;
                                        }
                                    }
                                }

                                if ( leastMissingData == null )
                                    break;

                                int amountToFurnish = MathA.Min( leastMissingData.LostFromMax, furnishingsToAdd );
                                if ( amountToFurnish > 0 )
                                {
                                    leastMissingData.AlterCurrent( amountToFurnish );
                                    furnishingsToAdd -= amountToFurnish;
                                    didSuccessfullyComplete = true;
                                    if ( furnishingsToAdd <= 0 )
                                        break;
                                }
                            }
                        }
                    }
                    break;
                #endregion TentConversion
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_MachineJobs: Called DoObjectiveCompleteLogicForNPCUnit for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }

            if ( didSuccessfullyComplete )
            {
                if ( DoVisualsAndSound )
                    Objective.OnObjectiveComplete.DuringGame_PlayAtLocation( Unit.GetDrawLocation() );

                NPCActionHelper.HandleCompletedObjective( Unit, Objective, Rand );
            }
            else
                NPCActionHelper.HandleFailedObjective( Unit, Objective );
        }

        public void RenderObjectivePercentComplete( ISimNPCUnit Unit, NPCUnitObjective Objective, ArcenCharacterBufferBase Buffer )
        {
            if ( Objective == null || Unit == null )
                return;

            switch ( Objective.ID )
            {
                case "TentConversion":
                case "LeaveAreaAndFurnish":
                    Buffer.AddRaw( 99.ToStringIntPercent() ); //since this happens immediately, we should just always show 99%
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_MachineJobs: Called RenderObjectivePercentComplete for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void RenderObjectiveExtraTooltipData( ISimNPCUnit Unit, NPCUnitObjective Objective, ArcenCharacterBufferBase Buffer )
        {
            if ( Objective == null || Unit == null )
                return;

            switch ( Objective.ID )
            {
                case "TentConversion":
                case "LeaveAreaAndFurnish":
                    //nothing to say for now
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_MachineJobs: Called RenderObjectiveExtraTooltipData for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

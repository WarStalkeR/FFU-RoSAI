using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class VisCheats_Basic : ICheatTypeImplementation
    {
        public void FillListOfSecondaryDataSource( CheatType CheatType, List<ICheatSecondaryDataSource> RowsToFill,
            out ICheatSecondaryDataSource DefaultRow )
        {
            RowsToFill.Clear();
            switch ( CheatType.ID )
            {
                case "AbortAllThreads":
                case "ClickToEstablishNetwork":
                case "ClickToEraseTimeline":
                    DefaultRow = null;
                    return;
                case "ClickToSpawnTimeline":
                    {
                        DefaultRow = TimelineTypeHolder.Create( TimelineType.ActualNewTimeline );
                        RowsToFill.Clear();
                        RowsToFill.Add( DefaultRow );
                        for ( TimelineType type = TimelineType.ActualNewTimeline + 1; type <= TimelineType.FailedTimeline; type++ )
                            RowsToFill.Add( TimelineTypeHolder.Create( type ) );
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "VisCheats_Basic FillListOfSecondaryDataSource: Not set up for '" + CheatType.ID + "'!", Verbosity.ShowAsError );
                    DefaultRow = null;
                    return;
            }
        }

        public bool GetShouldBeVisibleAtMoment( CheatType CheatType )
        {
            switch ( CheatType.ID )
            {
                case "AbortAllThreads":
                case "ClickToEstablishNetwork":
                case "ClickToSpawnTimeline":
                case "ClickToEraseTimeline":
                    return true;
                default:
                    ArcenDebugging.LogSingleLine( "VisCheats_Basic GetShouldBeVisibleAtMoment: Not set up for '" + CheatType.ID + "'!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public bool GetUsesSecondaryDataSources( CheatType CheatType )
        {
            switch ( CheatType.ID )
            {
                case "AbortAllThreads":
                case "ClickToEstablishNetwork":
                case "ClickToEraseTimeline":
                    return false;
                case "ClickToSpawnTimeline":
                    return true;
                default:
                    ArcenDebugging.LogSingleLine( "VisCheats_Basic GetUsesSecondaryDataSources: Not set up for '" + CheatType.ID + "'!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public bool GetUsesTextbox( CheatType CheatType )
        {
            switch ( CheatType.ID )
            {
                case "AbortAllThreads":
                case "ClickToEstablishNetwork":
                case "ClickToSpawnTimeline":
                case "ClickToEraseTimeline":
                    return false;
                default:
                    ArcenDebugging.LogSingleLine( "VisCheats_Basic GetUsesTextbox: Not set up for '" + CheatType.ID + "'!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public bool ExecuteCheat( CheatType Cheat, ICheatSecondaryDataSource Row, string TextboxText )
        {
            switch ( Cheat.ID )
            {
                case "AbortAllThreads":
                    ArcenThreading.AbortAllThreads( false );
                    return true;
                case "ClickToEstablishNetwork":
                case "ClickToSpawnTimeline":
                case "ClickToEraseTimeline":
                    if ( Engine_HotM.CurrentCheatOverridingClickMode == Cheat )
                        Engine_HotM.CurrentCheatOverridingClickMode = null;
                    else
                        Engine_HotM.CurrentCheatOverridingClickMode = Cheat;
                    return false;
                default:
                    ArcenDebugging.LogSingleLine( "VisCheats_Basic ExecuteCheat: Not set up for '" + Cheat.ID + "'!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public void ExecuteCheatClick( CheatType Cheat, ICheatSecondaryDataSource Row, string TextboxText, ISimBuilding ClickedBuilding, CheatClickType ClickType )
        {
            switch ( Cheat.ID )
            {
                case "ClickToEstablishNetwork":
                    {
                        if ( ClickType == CheatClickType.HoverOnly )
                        {
                            if ( ClickedBuilding != null && ClickedBuilding.CalculateIsValidTargetForMachineStructureRightNow( CommonRefs.NetworkTowerStructure, null ) )
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            return;
                        }
                        if ( ClickType == CheatClickType.Right )
                            return;

                        if ( ClickedBuilding == null || !ClickedBuilding.CalculateIsValidTargetForMachineStructureRightNow( CommonRefs.NetworkTowerStructure, null ) )
                            return;

                        Window_NetworkNameWindow.BuildingToCreateAt = ClickedBuilding;
                        Window_NetworkNameWindow.StructureTypeForCosts = null;
                        Window_NetworkNameWindow.Instance.Open();
                    }
                    break;
                case "ClickToSpawnTimeline":
                    {
                        A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
                        if ( place )
                        {
                            EndOfTimeItem endItem = place.GetEndOfTimeItem();
                            if ( endItem != null && !(endItem?.Type?.ExtraPlaceableData?.IsZiggurat??true) )
                            {
                                EndOfTimeSubItemPosition pos = endItem.GetNearestSubObjectToPoint( Engine_HotM.MouseWorldHitLocation, 5f ); //smaller than this and some will be missed
                                if ( pos.SlotIndex >= 0 )
                                {
                                    CityTimeline existingTimeline = endItem.GetCityAtSubObjectIndex( pos.SlotIndex );
                                    if ( existingTimeline != null )
                                    {
                                        //nothing to do, already a city there
                                        CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                                    }
                                    else
                                    {
                                        RenderManager_TheEndOfTime.TryDrawTimelineGhost( pos, CommonRefs.EndOfTimeCityGhost );
                                        if ( !(Row is TimelineTypeHolder TimelineTypeHolder) )
                                        {
                                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                                            return;
                                        }
                                        CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                                        if ( ClickType == CheatClickType.HoverOnly )
                                            return;
                                        if ( ClickType == CheatClickType.Right )
                                            return;

                                        TimelineType TimelineType = TimelineTypeHolder.Type;
                                        switch ( TimelineType )
                                        {
                                            case TimelineType.ActualNewTimeline:
                                                {
                                                    Window_NewTimeline.CreateOnItem = endItem;
                                                    Window_NewTimeline.CreateOnItemAtIndex = pos.SlotIndex;
                                                    Window_CheatWindow.Instance.Close( WindowCloseReason.UserDirectRequest );
                                                    Window_NewTimeline.Instance.Open();
                                                }
                                                break;
                                            default:
                                                {
                                                    CityTimeline city = CityTimeline.CreateNew( Window_NewTimeline.GenerateNewCityName( Engine_Universal.PermanentQualityRandom ), 
                                                        string.Empty, 2 ); //blank GUID until populated
                                                    city.ChildOfEndOfTimeObjectWithID = endItem.ItemID;
                                                    city.CitySlotIndexUsedFromParent = pos.SlotIndex;
                                                    endItem.TrySetSubObjectSlotAsBeingInUse( city.CitySlotIndexUsedFromParent, city );

                                                    switch ( TimelineType )
                                                    {
                                                        case TimelineType.FailedTimeline:
                                                            city.IsTimelineAFailure = true;
                                                            break;
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                }
                                else
                                    CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            }
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                        }
                        else
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                    }
                    break;
                case "ClickToEraseTimeline":
                    {
                        A5Placeable place = Engine_HotM.PlaceableUnderMouse as A5Placeable;
                        if ( place )
                        {
                            EndOfTimeItem endItem = place.GetEndOfTimeItem();
                            if ( endItem != null )
                            {
                                EndOfTimeSubItemPosition pos = endItem.GetNearestSubObjectToPoint( Engine_HotM.MouseWorldHitLocation, 5f ); //smaller than this and some will be missed
                                if ( pos.SlotIndex >= 0 )
                                {
                                    CityTimeline existingTimeline = endItem.GetCityAtSubObjectIndex( pos.SlotIndex );
                                    if ( existingTimeline != null )
                                    {
                                        if ( existingTimeline == SimCommon.CurrentTimeline )
                                        {
                                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                                            return; //we cannot delete the one we are currently in
                                        }
                                        //there is a timeline here, let's erase it
                                        CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_OtherTargeting );
                                        if ( ClickType == CheatClickType.HoverOnly )
                                            return;
                                        if ( ClickType == CheatClickType.Right )
                                            return;

                                        endItem.TrClearSubObjectSlot( pos.SlotIndex );
                                        SimMetagame.AllTimelines.Remove( existingTimeline.TimelineID );
                                        existingTimeline.ReturnToPool();
                                    }
                                    else
                                        CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                                }
                                else
                                    CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                            }
                            else
                                CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                        }
                        else
                            CursorHelper.RenderSpecificScalingMouseCursor( true, IconRefs.Mouse_Invalid );
                    }
                    break;
                default:
                    break; //normal for this to do nothing
            }
        }

        public void AppendToDropdownBuffer( CheatType Cheat, ArcenDoubleCharacterBuffer Buffer )
        {
        }

        #region TimelineType, TimelineTypeHolder
        private enum TimelineType
        {
            ActualNewTimeline,
            QuickTimeline,
            FailedTimeline
        }

        private struct TimelineTypeHolder : ICheatSecondaryDataSource
        {
            public TimelineType Type;
            public string TypeAsString;
            public string NameKey;
            public string DescriptionKey;

            public static TimelineTypeHolder Create( TimelineType Type )
            {
                TimelineTypeHolder holder;
                holder.Type = Type;
                holder.TypeAsString = Type.ToString();
                holder.NameKey = holder.TypeAsString + "_Name";
                holder.DescriptionKey = holder.TypeAsString + "_Description";
                return holder;
            }

            public bool Equals( IArcenDropdownOption obj )
            {
                if ( obj is TimelineTypeHolder other )
                    return this.Type == other.Type;
                return false;
            }

            public bool Equals( IArcenCheckedListboxOption obj )
            {
                if ( obj is TimelineTypeHolder other )
                    return this.Type == other.Type;
                return false;
            }

            public string GetDisplayName()
            {
                return Lang.Get( this.NameKey );
            }

            public string GetDescription()
            {
                return Lang.Get( this.DescriptionKey );
            }

            public object GetItem()
            {
                return this;
            }

            public string GetOptionValueForDropdown()
            {
                return this.TypeAsString;
            }

            public string GetStringForDropdownMatch()
            {
                return Lang.Get( this.NameKey );
            }

            public void WriteOptionDisplayTextForDropdown( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( this.NameKey );
            }
        }
        #endregion
    }
}

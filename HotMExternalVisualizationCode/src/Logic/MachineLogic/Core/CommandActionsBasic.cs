using Arcen.Universal;
using System;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class CommandActionsBasic : ICommandActionImplementation
    {
        public CommandActionResult TryHandleCommandAction( MachineCommandModeAction Action, ArcenCharacterBufferBase BufferOrNull, CommandActionLogic Logic )
        {
            if ( Action == null )
                return CommandActionResult.Indeterminate; //was error

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Action.ID )
            {
                case "DeterAndDefend":
                case "DeterAndAttack":
                case "LocalIntimidation":
                case "SeekAndDestroy":
                    #region DeterAndDefend and SeekAndDestroy
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                return CommandActionResult.Indeterminate;
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.ExtraDrawPerFrameWhileActive:
                                {
                                }
                                break;
                            case CommandActionLogic.WorldMousePerFrameWhileActive:
                                {
                                    NPCUnitStance stance = null;
                                    switch ( Action.ID )
                                    {
                                        case "DeterAndDefend":
                                            stance = CommonRefs.Player_DeterAndDefend;
                                            break;
                                        case "DeterAndAttack":
                                            stance = CommonRefs.Player_DeterAndAttack;
                                            break;
                                        case "LocalIntimidation":
                                            stance = CommonRefs.Player_LocalIntimidation;
                                            break;
                                        case "SeekAndDestroy":
                                            stance = CommonRefs.Player_SeekAndDestroy;
                                            break;
                                    }

                                    if ( stance != null && MouseHelper.ActorUnderCursor is ISimNPCUnit npcUnit && npcUnit.GetIsPlayerControlled() )
                                    {
                                        //CursorHelper.RenderSpecificScalingMouseCursor( true, stance.Icon, IconRefs.WillSelectTarget, MathRefs.WillSelectScalingRangeMin.FloatMin,
                                        //    MathRefs.WillSelectScalingRangeMax.FloatMin, MathRefs.WillSelectScalingMultiplierMax.FloatMin );

                                        if ( npcUnit.Stance != stance )
                                        {
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( npcUnit ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                            {
                                                novel.Icon = stance.Icon;
                                                novel.TitleUpperLeft.AddRightClickFormat( "ClickToApplyStance_SuperBrief" );
                                            }

                                            if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //ability-action
                                            {
                                                npcUnit.Stance = stance;
                                                Action.OnUse.DuringGame_PlayAtLocation( npcUnit.GetDrawLocation() );
                                            }
                                        }
                                        else
                                        {
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( npcUnit ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                            {
                                                novel.Icon = stance.Icon;
                                                novel.ShouldTooltipBeRed = true;
                                                novel.TitleUpperLeft.AddLang( "AlreadyThisStance" );
                                            }
                                        }
                                    }
                                    //else if ( ArcenInput.RightMouseNonUI.GetIsBrieflyClicked_AndConsume() ) //clear ability-action mode
                                    //    Engine_HotM.CurrentCommandModeActionTargeting = null;

                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StopShelterCoordinators":
                    #region StopShelterCoordinators
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopShelterCoordinators" ).TripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StartShelterCoordinators":
                    #region StartShelterCoordinators
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopShelterCoordinators" ).UnTripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StopWorkerSledges":
                    #region StopWorkerSledges
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerSledges" ).TripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StartWorkerSledges":
                    #region StartWorkerSledges
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerSledges" ).UnTripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StopWorkerPMCImpostors":
                    #region StopWorkerPMCImpostors
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerPMCImpostors" ).TripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StartWorkerPMCImpostors":
                    #region StartWorkerPMCImpostors
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerPMCImpostors" ).UnTripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StopWorkerPredators":
                    #region StopWorkerPredators
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerPredators" ).TripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                case "StartWorkerPredators":
                    #region StartWorkerPredators
                    {
                        switch ( Logic )
                        {
                            case CommandActionLogic.GetShouldCloseOnUnitSelection:
                                return CommandActionResult.Yes;
                            case CommandActionLogic.MenuClick_IfNotWorldTargeted:
                                {
                                    CityFlagTable.Instance.GetRowByID( "StopWorkerPredators" ).UnTripIfNeeded();
                                    return CommandActionResult.ShouldBlockOtherMouseInteractions;
                                }
                        }
                        return CommandActionResult.Indeterminate;
                    }
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "CommandActionsBasic: Called TryHandleCommandAction for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return CommandActionResult.Indeterminate; //was error
            }
        }
    }
}

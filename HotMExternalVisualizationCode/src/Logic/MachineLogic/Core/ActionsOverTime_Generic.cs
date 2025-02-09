using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class ActionsOverTime_Generic : IMachineActorActionOverTimeImplementation
    {        
        public ActionOverTimeResult TryHandleActionOverTime( ActionOverTime Action, ArcenCharacterBufferBase BufferOrNull, ActionOverTimeLogic Logic, 
            MersenneTwister RandOrNull, IArcenUIElementForSizing MustBeAboveOrBelow, SideClamp Clamp, TooltipExtraText ExtraText, TooltipExtraRules ExtraRules )
        {
            if ( Action == null || Action.Actor == null || Action.Type == null )
                return ActionOverTimeResult.Fail;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Logic )
            {
                case ActionOverTimeLogic.TryCancel_FromUser:
                case ActionOverTimeLogic.FailureCancellation:
                    {

                    }
                    break;
                case ActionOverTimeLogic.WriteTooltip:
                    {
                        //int target = Action.GetIntData( "Target1", false );

                        if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                        {
                            Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                        }
                    }
                    break;
                case ActionOverTimeLogic.ExecuteActionPerTurn:
                    {
                        int target = Action.GetIntData( "Target1", false );
                        target -= 1;
                        Action.SetIntData( "Target1", target );
                        if ( target <= 0 )
                            return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                    }
                    break;
                case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                    {
                        //we completed it!
                        ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "GeneralActionOverTimeComplete" ), NoteStyle.BothGame,
                            Action.Type.ID, Action.Actor.GetTypeAsRow().ID, string.Empty, string.Empty, Action.Actor.SortID,
                            0, 0, string.Empty, string.Empty, string.Empty, 0 );

                        return ActionOverTimeResult.ActualFinalSuccess;
                    }
                case ActionOverTimeLogic.PredictFloatingText:
                case ActionOverTimeLogic.PredictSidebarButtonText:
                case ActionOverTimeLogic.PredictTurnsRemainingText:
                    {
                        int turnsToTake = Action.GetIntData( "Target1", false );
                        BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                    }
                    break;
            }

            return ActionOverTimeResult.IntermediateSuccess;
        }

        public bool GetPreActionLogicGetShouldBeShownForActor( ActionOverTimeType AOT, ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation,
            AbilityType FromAbility )
        {
            if ( Actor == null )
                return false;
            switch ( AOT.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActionsOverTime_Generic: Called GetPreActionLogicGetShouldBeShownForActor for '" + AOT.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public void HandlePreActionLogicAsAbilityLineItem( ActionOverTimeType AOT, ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation,
            PreActionOverTimeLineItemLogic Logic, AbilityType FromAbility, ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( AOT.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActionsOverTime_Generic: Called HandlePreActionLogicAsAbilityLineItem for '" + AOT.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return;
            }
        }
    }
}

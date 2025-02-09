using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class NPCObjective_Basics : INPCUnitObjectiveImplementation
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

            switch ( Objective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.PointsCollectionAgainstMachineStructure:
                case NPCObjectiveStyle.PointsCollectionAgainstSwarm:
                case NPCObjectiveStyle.PointsCollectionAgainstUnit:
                case NPCObjectiveStyle.PointsCollectionInPlace:
                    {
                        int toAdd = NPCActionHelper.CalculatePerTurnGainForPointsObjective( Unit, Objective );
                        if ( toAdd > 0 )
                            Unit.ObjectiveProgressPoints += toAdd;

                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            Unit.DebugText += "DCOLN-Points-" + toAdd + "\n";

                        //ArcenDebugging.LogSingleLine( SimCommon.Turn + " Turn: " + toAdd + " added, total now " + Unit.ObjectiveProgressPoints, Verbosity.DoNotShow );
                        if ( Unit.ObjectiveProgressPoints > Objective.PointsRequiredToCompleteObjective )
                        {
                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                Unit.DebugText += "DCOLN-Points-Complete\n";
                            //objective complete!
                            NPCActionHelper.Basic_HandleObjectiveCompletion( Unit, Objective, Rand );
                        }
                    }
                    return true;
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstMachineStructure:
                case NPCObjectiveStyle.CustomActionAgainstSwarm:
                case NPCObjectiveStyle.CustomActionAgainstUnit:
                case NPCObjectiveStyle.CustomActionInPlace:
                    {
                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                            Unit.DebugText += "DCOLN-Custom\n";
                    }
                    break; //handle this below!
                default:
                    {
                        ArcenDebugging.LogSingleLine( "NPCObjective_Basics.DoCurrentObjectiveLogicForNPCUnit: Objective '" + Objective.ID +
                            "' uses the objective style " + Objective.ObjectiveStyle + ", which has not yet been implemented!", Verbosity.ShowAsError );
                        return false;
                    }
            }

            switch ( Objective.ID )
            {
                case "Custom":
                    ArcenDebugging.LogSingleLine( "NPCObjective_Basics:Custom logic '" + Objective.ID + "' not set to really do anything yet!", Verbosity.ShowAsError );
                    //do whatever is needed
                    return true;
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_Basics: Called DoCurrentObjectiveLogicForNPCUnit for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
            return false;
        }

        public void DoObjectiveCompleteLogicForNPCUnit( ISimNPCUnit Unit, NPCUnitObjective Objective, MersenneTwister Rand, bool DoVisualsAndSound )
        {
            if ( Objective == null || Unit == null )
                return;

            if ( DoVisualsAndSound )
                Objective.OnObjectiveComplete.DuringGame_PlayAtLocation( Unit.GetDrawLocation() );
            NPCActionHelper.HandleCompletedObjective( Unit, Objective, Rand );

            switch ( Objective.ID )
            {
                case "Custom":
                    //do whatever is needed
                    break;
            }
        }

        public void RenderObjectivePercentComplete( ISimNPCUnit Unit, NPCUnitObjective Objective, ArcenCharacterBufferBase Buffer )
        {
            if ( Objective == null || Unit == null )
                return;

            switch ( Objective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.PointsCollectionAgainstMachineStructure:
                case NPCObjectiveStyle.PointsCollectionAgainstSwarm:
                case NPCObjectiveStyle.PointsCollectionAgainstUnit:
                case NPCObjectiveStyle.PointsCollectionInPlace:
                    {
                        int required = Objective.PointsRequiredToCompleteObjective;
                        if ( required < 1 )
                            required = 1;
                        int percentage = Mathf.RoundToInt( ((float)Unit.ObjectiveProgressPoints / (float)required) * 100 );
                        Buffer.AddRaw( percentage.ToStringIntPercent() );

                        int toAdd = NPCActionHelper.CalculatePerTurnGainForPointsObjective( Unit, Objective );
                        int percentagePerTurn = Mathf.RoundToInt( ((float)toAdd / (float)required) * 100 );
                        Buffer.Space1x().AddFormat1( "IncreasePerTurnParenthetical", percentagePerTurn.ToStringIntPercent() );
                    }
                    return;
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstMachineStructure:
                case NPCObjectiveStyle.CustomActionAgainstSwarm:
                case NPCObjectiveStyle.CustomActionAgainstUnit:
                case NPCObjectiveStyle.CustomActionInPlace:
                    break; //handle this below!
                default:
                    {
                        Buffer.AddNeverTranslated( Objective.ObjectiveStyle + " not implemented.", true );
                        ArcenDebugging.LogSingleLine( "NPCObjective_Basics: Called RenderObjectivePercentComplete for '" + 
                            Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                        return;
                    }
            }

            switch ( Objective.ID )
            {
                case "Custom":
                    //do whatever is needed
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_Basics: Called RenderObjectivePercentComplete for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void RenderObjectiveExtraTooltipData( ISimNPCUnit Unit, NPCUnitObjective Objective, ArcenCharacterBufferBase Buffer )
        {
            if ( Objective == null || Unit == null )
                return;

            switch ( Objective.ObjectiveStyle )
            {
                case NPCObjectiveStyle.PointsCollectionAgainstBuilding:
                case NPCObjectiveStyle.PointsCollectionAgainstMachineStructure:
                case NPCObjectiveStyle.PointsCollectionAgainstSwarm:
                case NPCObjectiveStyle.PointsCollectionAgainstUnit:
                case NPCObjectiveStyle.PointsCollectionInPlace:
                    //nothing to say for now
                    return;
                case NPCObjectiveStyle.CustomActionAgainstBuilding:
                case NPCObjectiveStyle.CustomActionAgainstMachineStructure:
                case NPCObjectiveStyle.CustomActionAgainstSwarm:
                case NPCObjectiveStyle.CustomActionAgainstUnit:
                case NPCObjectiveStyle.CustomActionInPlace:
                    break; //handle this below!
                default:
                    {
                        ArcenDebugging.LogSingleLine( "NPCObjective_Basics.RenderObjectiveExtraTooltipData: Objective '" + Objective.ID +
                            "' uses the objective style " + Objective.ObjectiveStyle + ", which has not yet been implemented!", Verbosity.ShowAsError );
                        return;
                    }
            }

            switch ( Objective.ID )
            {
                case "Custom":
                    //say whatever is needed
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCObjective_Basics: Called RenderObjectiveExtraTooltipData for '" + Objective.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

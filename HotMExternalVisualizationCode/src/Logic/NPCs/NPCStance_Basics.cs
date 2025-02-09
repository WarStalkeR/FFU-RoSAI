using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class NPCStance_Basics : INPCUnitStanceImplementation
    {
        public void HandleLogicForNPCUnitInStance( ISimNPCUnit Unit, NPCUnitStance Stance, NPCUnitStanceLogic Logic, MersenneTwister Rand,
            ArcenCharacterBufferBase BufferOrNull, ISimMapActor OtherActorOrNull, NPCTimingData TimingDataOrNull )
        {
            if ( Stance == null || Unit == null || Unit.IsFullDead || Unit.IsInvalid )
                return;

            if ( NPCActionHelper.TryHandleReallyCoreBits( Unit, Stance, Logic, Rand, BufferOrNull ) )
                return;

            switch ( Logic )
            {
                case NPCUnitStanceLogic.PerTurnLogic:
                    {
                        if ( Stance.IsContainedToPOI )
                        {
                            if ( Unit.HomePOI == null )
                            {
                                MapPOI currentPOI = Unit.CalculatePOI();
                                if ( currentPOI != null )
                                    Unit.HomePOI = currentPOI;
                            }
                        }
                        if ( Stance.IsContainedToDistrict )
                        {
                            if ( Unit.HomeDistrict == null )
                            {
                                MapDistrict currentDistrict = Unit.CalculateMapDistrict();
                                if ( currentDistrict != null )
                                    Unit.HomeDistrict = currentDistrict;
                            }
                        }

                        NPCUnitObjective currentObjective = Unit.CurrentObjective;
                        if ( currentObjective != null )
                        {
                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                Unit.DebugText += "HLNUS-Basic-PT-DoCurrentObjectiveLogic\n";
                            //note: this does NOT cost the unit its action.  So it can shoot at things in range, probably.
                            currentObjective.Implementation.DoCurrentObjectiveLogicForNPCUnit( Unit, Rand );
                        }
                        else
                        {
                            NPCUnitObjective nextObjective = Unit.NextObjective;
                            if ( nextObjective != null )
                            {
                                //note: this does NOT cost the npc its turn
                                if ( nextObjective.Implementation.TryPursueNextObjectiveForNPCUnit( Unit, Rand, Logic ) )
                                {
                                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                        Unit.DebugText += "HLNUS-Basic-PT-TryPursueNextObjectiveSuccess\n";
                                    //however, if the current objective is now set, it can do the first turn for that objective
                                    currentObjective = Unit.CurrentObjective;
                                    if ( currentObjective != null )
                                    {
                                        if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                            Unit.DebugText += "HLNUS-Basic-PT-DoCurrentObjectiveAFTER\n";
                                        //note: this does NOT cost the unit its action.  So it can shoot at things in range, probably.
                                        currentObjective.Implementation.DoCurrentObjectiveLogicForNPCUnit( Unit, Rand );
                                    }
                                    //return; //no costing them their turn with this
                                }
                                else
                                {

                                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                        Unit.DebugText += "HLNUS-Basic-PT-TryPursueNextObjectiveFAIL\n";
                                }
                            }
                        }

                        if ( Unit.ObjectiveIsCompleteAndWaiting )
                        {
                            if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                Unit.DebugText += "HLNUS-Basic-PT-ObjectiveIsCompleteAndWaiting\n";
                            return;
                        }

                        if ( Unit.NextObjective != null )
                            return; //already moving to that thing, don't do something else in the middle

                        foreach ( NPCActionConsideration cons in Stance.ActionConsiderations )
                        {
                            if ( !cons.CheckIfShouldAttempt( Unit, Rand ) )
                                continue;

                            if ( cons.ActionToAttempt != null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-PT-ActionAttempt-" + cons.ID + "\n";

                                if ( TimingDataOrNull != null )
                                {
                                    long ticksAtStart = TimingDataOrNull.sw.ElapsedTicks;

                                    bool result = cons.ActionToAttempt.Implementation.TryHandleActionLogicForNPCUnit( Unit, cons, cons.ActionToAttempt, Rand, NPCUnitActionLogic.PlanningPerTurn, TimingDataOrNull );

                                    TimingDataOrNull.ActionPlanningTicks += (TimingDataOrNull.sw.ElapsedTicks - ticksAtStart);
                                    TimingDataOrNull.ActionPlanningCount++;

                                    if ( result )
                                    {
                                        cons.HandleAnyLogicIfThisActionSuccessfullyStarts( Unit );
                                        return; //we did an action, hooray!
                                    }
                                }
                                else
                                {
                                    if ( cons.ActionToAttempt.Implementation.TryHandleActionLogicForNPCUnit( Unit, cons, cons.ActionToAttempt, Rand, NPCUnitActionLogic.PlanningPerTurn, TimingDataOrNull ) )
                                    {
                                        cons.HandleAnyLogicIfThisActionSuccessfullyStarts( Unit );
                                        return; //we did an action, hooray!
                                    }
                                }
                            }

                            if ( cons.ObjectiveToAttemptToStart != null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-PT-ObjectiveAttemptStart-" + cons.ObjectiveToAttemptToStart.ID + "\n";
                                if ( cons.ObjectiveToAttemptToStart.Implementation.TrySetThisObjectiveForNPCUnit( Unit, cons.ObjectiveToAttemptToStart, cons, Rand ) )
                                {
                                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                        Unit.DebugText += "HLNUS-Basic-PT-ObjectiveAttemptStartSUCCESS\n";
                                    cons.HandleAnyLogicIfThisActionSuccessfullyStarts( Unit );
                                    return; //we set a new objective, hooray!
                                }
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-PT-ObjectiveAttemptStartFAIL\n";
                            }

                            //we failed to do anything from this consideration
                            cons.HandleAnyLogicIfThisActionFailsToStart( Unit );
                        }
                    }
                    break;
                case NPCUnitStanceLogic.MoveLogic:
                    {
                        NPCUnitObjective nextObjective = Unit.NextObjective;
                        if ( nextObjective != null )
                        {
                            //note: this DOES cost the unit its turn, since it was probably running to the place
                            if ( nextObjective.Implementation.TryPursueNextObjectiveForNPCUnit( Unit, Rand, Logic ) )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-MOVE-TryPursueNextObjective\n";
                                return;
                            }
                        }

                        foreach ( NPCActionConsideration cons in Stance.ActionConsiderations )
                        {
                            if ( !cons.CheckIfShouldAttempt( Unit, Rand ) )
                                continue;

                            if ( cons.ActionToAttempt != null )
                            {
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-MOVE-ActionAttempt-" + cons.ID + "\n";
                                if ( cons.ActionToAttempt.Implementation.TryHandleActionLogicForNPCUnit( Unit, cons, cons.ActionToAttempt, Rand, NPCUnitActionLogic.ExecutionOfPriorPlan, TimingDataOrNull ) )
                                {
                                    if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                        Unit.DebugText += "HLNUS-Basic-MOVE-ActionAttemptSUCCESS\n";
                                    cons.HandleAnyLogicIfThisActionSuccessfullyStarts( Unit );
                                    return; //we did a thing, hooray!
                                }
                                if ( InputCaching.Debug_LogNPCUnitLogicToTooltip )
                                    Unit.DebugText += "HLNUS-Basic-MOVE-ActionAttemptFAIL\n";
                            }

                            //we failed to do anything from this consideration
                            cons.HandleAnyLogicIfThisActionFailsToStart( Unit );
                        }
                    }
                    break;
            }            
        }

        public void HandleSecondaryPerTurnLogicForNPCUnitInStance( ISimNPCUnit Unit, NPCUnitStance Stance, MersenneTwister Rand )
        {
            if ( Stance == null || Unit == null )
                return;

            switch ( Stance.ID )
            {
                case "Corpo_StealingShelteredHumans":
                    #region Corpo_StealingShelteredHumans
                    try
                    {
                        int toSteal = Rand.NextInclus( 380, 700 );
                        int available = (int)ResourceRefs.ShelteredHumans.Current;
                        toSteal = MathA.Min( toSteal, available );

                        ResourceRefs.ShelteredHumans.AlterCurrent_Named( -toSteal, "Decrease_StolenByCorporation", ResourceAddRule.StoreExcess );

                        Vector3 startLocation = Unit.GetPositionForCollisions();
                        Vector3 endLocation = startLocation.PlusY( Unit.GetHalfHeightForCollisions() + 0.2f );
                        ArcenDoubleCharacterBuffer buffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                        if ( buffer != null )
                        {
                            buffer.AddFormat1( "KidnappedCount", toSteal, IconRefs.KidnappedCount.DefaultColorHexWithHDRHex );
                            DamageTextPopups.FloatingDamageTextPopup newDamageText = DamageTextPopups.CreateNewFromTextBuffer( buffer,
                                startLocation, endLocation, 0.8f, MathA.Max( 2, GameSettings.Current.GetInt( "DamagePopup_LingerTime" ) ) );
                            newDamageText.PhysicalDamageIncluded = 0;
                            newDamageText.MoraleDamageIncluded = 0;
                            newDamageText.SquadDeathsIncluded = 0;
                        }
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "HandleSecondaryPerTurnLogicForNPCUnitInStance for " + Stance.ID + " error: " + e, Verbosity.ShowAsError );
                    }
                    break;
                    #endregion
            }
        }
    }
}

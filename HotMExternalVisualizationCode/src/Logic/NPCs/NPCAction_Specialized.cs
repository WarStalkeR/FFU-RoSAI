using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class NPCAction_Specialized : INPCUnitActionImplementation
    {
        public bool TryHandleActionLogicForNPCUnit( ISimNPCUnit Unit, NPCActionConsideration Consideration, NPCUnitAction Action, MersenneTwister Rand, NPCUnitActionLogic Logic, NPCTimingData TimingDataOrNull )
        {
            if ( Action == null || Unit == null )
                return false;

            NPCUnitStance stance = Unit.Stance;

            switch ( Action.ID )
            {
                case "DamageInfiltrators":
                    #region DamageInfiltrators
                    switch ( Logic )
                    {
                        case NPCUnitActionLogic.PlanningPerTurn: //do the damage during the planning phase
                            {
                                Vector3 loc = Unit.GetDrawLocation();
                                int hackingSkill = Unit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                                //ArcenDebugging.LogSingleLine( "Hacker: " + Unit.UnitType.ID + " hacking: " + hackingSkill + " statuses: " + Unit.StatusEffectCount + 
                                //    " machine actors: " + SimCommon.AllMachineActors.GetDisplayList().Count, Verbosity.DoNotShow );

                                if ( hackingSkill <= 0 )
                                    break;

                                foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                {
                                    if ( actor.IsFullDead )
                                        continue;

                                    switch ( actor.CurrentActionOverTime?.Type?.ID ?? string.Empty )
                                    {
                                        case "InvestigateLocation_Infiltration":
                                            {
                                                //ArcenDebugging.LogSingleLine( "target: " + actor.GetDisplayName(), Verbosity.DoNotShow );

                                                float distSquared = ( loc - actor.GetCollisionCenter() ).GetSquareGroundMagnitude();
                                                //ArcenDebugging.LogSingleLine( "dist was: " + Mathf.Sqrt( distSquared ), Verbosity.DoNotShow );
                                                if ( distSquared < 80 * 80 )
                                                {
                                                    int agility = actor.GetActorDataCurrent( ActorRefs.ActorAgility, true );

                                                    int damageToDo = hackingSkill - agility;
                                                    //ArcenDebugging.LogSingleLine( "Wants to damage infiltrator: " + damageToDo + " vs " + agility, Verbosity.DoNotShow );

                                                    int damageReduction = actor.GetStatusIntensity( StatusRefs.NetworkShield );
                                                    if ( damageReduction > 0 )
                                                        damageToDo -= damageReduction;

                                                    if ( damageToDo > 0 )
                                                    {
                                                        ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                                        if ( popupBuffer != null )
                                                        {
                                                            popupBuffer.AddFormat1( "SearchingforInfiltratorPopup", damageToDo, IconRefs.SearchingColor.DefaultColorHexWithHDRHex );
                                                            AttackHelper.DoPopupTextAgainstNPCTarget( Unit, popupBuffer );
                                                        }

                                                        actor.AlterActorDataCurrent( ActorRefs.ActorHP, -damageToDo, true );
                                                        actor.DoOnPostTakeDamage( Unit, damageToDo, 0, 0, Rand, true );

                                                        Action.OnUseOptional?.DuringGame_PlayAtLocation( Unit.GetDrawLocation() );

                                                        return true;
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }

                                //ArcenDebugging.LogSingleLine( "Not in range of any infiltrator", Verbosity.DoNotShow );
                            }
                            break;
                    }
                    #endregion
                    break;
                case "BigAlarmAgainstInfiltrator":
                    #region DamageInfiltrators
                    switch ( Logic )
                    {
                        case NPCUnitActionLogic.ReactingToHostileAction:
                        case NPCUnitActionLogic.ReactingToDamage:
                            {
                                Vector3 loc = Unit.GetDrawLocation();
                                int hackingSkill = Unit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                                if ( hackingSkill <= 0 )
                                    break;

                                hackingSkill += hackingSkill; //double it

                                foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                                {
                                    if ( actor.IsFullDead )
                                        continue;

                                    switch ( actor.CurrentActionOverTime?.Type?.ID ?? string.Empty )
                                    {
                                        case "InvestigateLocation_Infiltration":
                                            {
                                                float distSquared = (loc - actor.GetCollisionCenter()).GetSquareGroundMagnitude();
                                                //ArcenDebugging.LogSingleLine( "dist was: " + Mathf.Sqrt( distSquared ), Verbosity.DoNotShow );
                                                if ( distSquared < 80 * 80 )
                                                {
                                                    //ArcenDebugging.LogSingleLine( "Wants to damage infiltrator", Verbosity.DoNotShow );

                                                    int agility = actor.GetActorDataCurrent( ActorRefs.ActorAgility, true );

                                                    int damageToDo = hackingSkill - agility;

                                                    int damageReduction = actor.GetStatusIntensity( StatusRefs.NetworkShield );
                                                    if ( damageReduction > 0 )
                                                        damageToDo -= damageReduction;

                                                    if ( damageToDo > 0 )
                                                    {
                                                        ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                                        if ( popupBuffer != null )
                                                        {
                                                            popupBuffer.AddFormat1( "AlarmEndangersInfiltratorPopup", damageToDo, IconRefs.SearchingColor.DefaultColorHexWithHDRHex );
                                                            AttackHelper.DoPopupTextAgainstNPCTarget( Unit, popupBuffer, -0.3f );
                                                        }

                                                        actor.AlterActorDataCurrent( ActorRefs.ActorHP, -damageToDo, true );
                                                        actor.DoOnPostTakeDamage( Unit, damageToDo, 0, 0, Rand, true );

                                                        Action.OnUseOptional?.DuringGame_PlayAtLocation( Unit.GetDrawLocation() );

                                                        return true;
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }

                                //ArcenDebugging.LogSingleLine( "Not in range of any infiltrator", Verbosity.DoNotShow );
                            }
                            break;
                    }
                    #endregion
                    break;
                case "StealNeuralSecrets":
                    #region StealNeuralSecrets
                    switch ( Logic )
                    {
                        case NPCUnitActionLogic.PlanningPerTurn: //do the damage during the planning phase
                            {
                                Vector3 loc = Unit.GetDrawLocation();
                                int hackingSkill = Unit.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                                if ( hackingSkill <= 0 )
                                    break;

                                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                                {
                                    MachineStructure structure = kv.Value;
                                    if ( structure.IsFullDead )
                                        continue;
                                    int secretProtectionMax = structure.GetActorDataMaximum( ActorRefs.NeuralSecretProtection, true );
                                    if ( secretProtectionMax > 0 )
                                    {
                                        float distSquared = (loc - structure.GetCollisionCenter()).GetSquareGroundMagnitude();
                                        //ArcenDebugging.LogSingleLine( "dist was: " + Mathf.Sqrt( distSquared ), Verbosity.DoNotShow );
                                        if ( distSquared < 80 * 80 )
                                        {
                                            //ArcenDebugging.LogSingleLine( "Wants to damage infiltrator", Verbosity.DoNotShow );


                                            if ( hackingSkill > 0 )
                                            {
                                                structure.AlterActorDataCurrent( ActorRefs.NeuralSecretProtection, -hackingSkill, true );
                                                int secretProtection = structure.GetActorDataCurrent( ActorRefs.NeuralSecretProtection, true );

                                                ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                                if ( secretProtection > 0 )
                                                {
                                                    if ( popupBuffer != null )
                                                    {
                                                        popupBuffer.AddFormat1( "CrackingYourDefenses", hackingSkill, IconRefs.SearchingColor.DefaultColorHexWithHDRHex );
                                                        AttackHelper.DoPopupTextAgainstNPCTarget( Unit, popupBuffer );
                                                    }
                                                }
                                                else
                                                {
                                                    if ( popupBuffer != null )
                                                    {
                                                        popupBuffer.AddLang( "StoleMilitarySecrets" );
                                                        AttackHelper.DoPopupTextAgainstNPCTarget( Unit, popupBuffer );
                                                        CommonRefs.MilitarySecretsStolenFromYou.AlterScore_CityOnly( 1 );

                                                        HandbookRefs.YourMilitarySecrets.DuringGame_UnlockIfNeeded( true );
                                                    }
                                                }

                                                Action.OnUseOptional?.DuringGame_PlayAtLocation( Unit.GetDrawLocation() );

                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "NPCAction_Specialized: Called HandleActionLogicForNPCUnit for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
            return false;
        }
    }
}

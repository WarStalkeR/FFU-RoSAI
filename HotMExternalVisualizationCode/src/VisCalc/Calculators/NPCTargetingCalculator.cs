using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;
using System.Diagnostics;
using System.Text;

namespace Arcen.HotM.ExternalVis
{
    public static class NPCTargetingCalculator
    {
        private static int IsCalculatingNow = 0;
        public static float LastStartedOrFinished = 0f;

        public static bool GetIsCalculatingNow()
        {
            return IsCalculatingNow > 0;
        }

        public static void OnGameClear()
        {
            IsCalculatingNow = 0;
        }

        public static void RecalculateIfNeeded()
        {
            if ( IsCalculatingNow > 0 )
                return;

            //do we need to do this?
            {
                if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count > 0 || SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count > 0 )
                    return; //if any of this is going on, we're not ready for a new pass

                if ( Engine_HotM.NonSim_TravelingParticleEffectsPlayingNow > 0 )
                    return; //this is true if there are bullets still on their way to their targets.

                if ( SimCommon.NPCsMoving_MainThreadOnly.Count > 0 )
                    return; //if NPCs are still moving, then also don't do this yet

                if ( SimCommon.IsCurrentlyRunningSimTurn )
                    return; //if is currently changing turns, don't mess with it.
                //having the below creates a soft lock, because it absolutely will wait on us in this stage, and we don't want to wait on it in return!
                //if ( SimCommon.IsReadyToRunNextLogicForTurn > SimCommon.Turn )
                //    return;

                if ( SimCommon.NeedsVisibilityGranterRecalculation || VisibilityGranterCalculator.GetIsCalculatingNow() )
                    return; //if the visibility calculations are not up to date, then get those up to date before we do this

                if ( !SimCommon.HasRunFirstDeserializationSecondLogic )
                    return; //we need this to happen first
            }

            if ( !SimCommon.NeedsToAttemptAnotherNPCTargetingPass )
            {
                if ( SimCommon.PlayerHasIndicatedADesireToGoToThisTurn > SimCommon.Turn )
                    return; //if the player said they wanted to go to the next turn, and nothing else said to do a targeting pass, then don't

                if ( ArcenTime.AnyTimeSinceStartF - 1f <= LastStartedOrFinished )
                    return; //if nobody requested it, and it's been less than 1 second since we last started or finished, don't try again either

                //otherwise if it HAS been more than 1 second, go ahead and see if anything is different
            }

            if ( Interlocked.Exchange( ref IsCalculatingNow, 1 ) != 0 )
                return;

            SimCommon.NeedsToAttemptAnotherNPCTargetingPass = false; //do this early in case more changes cause this to be set true again out of here
            SimCommon.IsDoingATargetingPassNow = true; //set this to block the main thread

            if ( !ArcenThreading.RunTaskOnBackgroundThread( "_Inter.NPCTargeting",
                ( TaskStartData startData ) =>
                {
                    LastStartedOrFinished = ArcenTime.AnyTimeSinceStartF;
                    SimCommon.VisibilityGranterCycle++;
                    try
                    {
                        RecalculateInner();
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogWithStack( "NPCTargetingCalculator.NPCTargeting background thread error: " + e, Verbosity.ShowAsError );
                    }

                    Interlocked.Exchange( ref IsCalculatingNow, 0 );
                    LastStartedOrFinished = ArcenTime.AnyTimeSinceStartF;
                    SimCommon.IsDoingATargetingPassNow = false; //set this at the end
                } ) )
            {
                SimCommon.NeedsToAttemptAnotherNPCTargetingPass = true; //if it failed, set this back to true to try again
                SimCommon.IsDoingATargetingPassNow = false; //if failed, set this back to false to just not block the wrong things
                Interlocked.Exchange( ref IsCalculatingNow, 0 );
            }
        }

        private static void RecalculateInner()
        {
            Stopwatch sw = Stopwatch.StartNew();
            DoPreTargetingPass();
            RecalculateNPCTargets();
            DoPostTargetingPass();
            SimTimingInfo.TargetingPass.LogCurrentTicks( (int)sw.ElapsedTicks );

            //do this after the targeting pass timing
            DoPostTargetingDumpIfNeeded();
        }

        #region DoPreTargetingPass
        private static void DoPreTargetingPass()
        {
            sortedNPCs.Clear();
            currentPossibleMapActors.Clear();
            fullListMapActors.Clear();

            //this clears stuff like incoming damage,
            //but it does not clear ActorIWillAttackAtTheStartOfNextTurn or DamageIWillDoToTheTargetActor
            //this just prevents those from flickering if the same values happen to be chosen again
            foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
            {
                //this absolutely should not be needed, but it seems to be for some reason
                kv.Value.IncomingDamage.ClearConstructionValueForStartingConstruction();
                fullListMapActors.Add( kv.Value );

                if ( !kv.Value.IsFullDead && !kv.Value.IsInvalid && AttackHelper.GetTargetVisibleForNPCs( kv.Value ) )
                    currentPossibleMapActors.Add( kv.Value );
            }
            foreach ( KeyValuePair<int, ISimMachineUnit> kv in World.Forces.GetMachineUnitsByID() )
            {
                //this absolutely should not be needed, but it seems to be for some reason
                kv.Value.IncomingDamage.ClearConstructionValueForStartingConstruction();
                fullListMapActors.Add( kv.Value );

                if ( !kv.Value.IsFullDead && !kv.Value.IsInvalid && AttackHelper.GetTargetVisibleForNPCs( kv.Value ) )
                    currentPossibleMapActors.Add( kv.Value );
            }
            foreach ( KeyValuePair<int, ISimMachineVehicle> kv in World.Forces.GetMachineVehiclesByID() )
            {
                //this absolutely should not be needed, but it seems to be for some reason
                kv.Value.IncomingDamage.ClearConstructionValueForStartingConstruction();
                fullListMapActors.Add( kv.Value );

                if ( !kv.Value.IsFullDead && !kv.Value.IsInvalid && AttackHelper.GetTargetVisibleForNPCs( kv.Value ) )
                    currentPossibleMapActors.Add( kv.Value );
            }
            foreach ( KeyValuePair<int, ISimNPCUnit> kv in World.Forces.GetAllNPCUnitsByID() )
            {
                //this absolutely should not be needed, but it seems to be for some reason
                kv.Value.IncomingDamage.ClearConstructionValueForStartingConstruction();
                fullListMapActors.Add( kv.Value );

                if ( !kv.Value.IsFullDead && !kv.Value.IsInvalid && AttackHelper.GetTargetVisibleForNPCs( kv.Value ) )
                    currentPossibleMapActors.Add( kv.Value );

                //then if this is an NPC unit, it sorts us into two groups, one for area attacks and one for point attacks
                ISimNPCUnit npcUnit = kv.Value;
                if ( npcUnit.IsFullDead )
                    continue;
                NPCUnitStance stance = npcUnit.Stance;
                if ( stance == null )
                    continue;

                //we precalculate this stuff so that it's both faster in the sort, and also so it's stable in the sort
                NPCToSort toSort;
                toSort.Unit = npcUnit;
                if ( npcUnit.GetHasAreaOfAttack() )
                    toSort.TargetingOrder = (stance.TargetingOrder * 10);
                else
                    toSort.TargetingOrder = stance.TargetingOrder;
                toSort.UnitID = npcUnit.ActorID;
                toSort.AttackPower = npcUnit.GetActorDataCurrent( ActorRefs.ActorPower, true );

                sortedNPCs.Add( toSort );
            }

            sortedNPCs.Sort( delegate ( NPCToSort Left, NPCToSort Right )
            {
                int val = Left.TargetingOrder.CompareTo( Right.TargetingOrder ); //asc, so do lower numbers first
                if ( val != 0 )
                    return val;
                val = Left.AttackPower.CompareTo( Right.AttackPower ); //asc, so do lower numbers first. Causes less overkill.
                if ( val != 0 )
                    return val;
                return Left.UnitID.CompareTo( Right.UnitID ); //asc, so do older units first
            } );
        }
        #endregion

        #region DoPostTargetingPass
        private static void DoPostTargetingPass()
        {
            foreach ( ISimMapActor actor in fullListMapActors )
                actor.IncomingDamage.SwitchConstructionToDisplay();
        }
        #endregion

        private static List<NPCToSort> sortedNPCs = List<NPCToSort>.Create_WillNeverBeGCed( 4000, "NPCTargetingCalculator-sortedNPCs" );
        private static Dictionary<ISimMapActor,bool> playerDamagedActors = Dictionary<ISimMapActor, bool>.Create_WillNeverBeGCed( 4000, "NPCTargetingCalculator-playerDamagedActors" );
        private static Dictionary<ISimNPCUnit, bool> npcsDamagingPlayerUnits = Dictionary<ISimNPCUnit, bool>.Create_WillNeverBeGCed( 4000, "NPCTargetingCalculator-npcsDamagingPlayerUnits" );
        private static List<ISimMapActor> currentPossibleMapActors = List<ISimMapActor>.Create_WillNeverBeGCed( 4000, "NPCTargetingCalculator-currentPossibleMapActors" );
        private static List<ISimMapActor> fullListMapActors = List<ISimMapActor>.Create_WillNeverBeGCed( 4000, "NPCTargetingCalculator-fullListMapActors" );

        #region RecalculateNPCTargets
        private static void RecalculateNPCTargets()
        {
            bool isDoingDumpOfAll = InputCaching.Debug_DoDumpNPCTargetingAll || InputCaching.Debug_DoLogNPCTargetingToTooltip;
            bool isDoingDumpOfNearby = InputCaching.Debug_DoDumpNPCTargetingInRangeOfCameraOnly;

            List<ISimMapActor> listMustBeInToDump = isDoingDumpOfNearby ? CameraCurrent.BodyCellOrNull?.ParentTile?.ActorsWithinMaxNPCAttackRange?.GetDisplayList() : null;

            SimCommon.NPCsWithAttackPlansThatHitPlayer.ClearConstructionListForStartingConstruction();

            playerDamagedActors.Clear();
            npcsDamagingPlayerUnits.Clear();

            foreach ( NPCToSort npcUnitSorted in sortedNPCs )
            {
                npcUnitSorted.Unit.AttackPlan.ClearConstructionValueForStartingConstruction();
                RecalculateTargetForSingleNPCUnit( npcUnitSorted.Unit, npcUnitSorted.TargetingOrder, isDoingDumpOfAll, listMustBeInToDump );
                npcUnitSorted.Unit.AttackPlan.SwitchConstructionToDisplay();
            }

            SimCommon.NPCsWithAttackPlansThatHitPlayer.SwitchConstructionToDisplay();

            CalculateAttackPlanIncoming();
        }
        #endregion

        #region RecalculateTargetForSingleNPCUnit
        private static MersenneTwister workingRand = new MersenneTwister( 0 );
        private static void RecalculateTargetForSingleNPCUnit( ISimNPCUnit npcUnit, int TargetingOrder, bool isDoingDumpOfAll, List<ISimMapActor> listMustBeInToDump )
        {
            if ( npcUnit == null )
                return; //just in case

            if ( npcUnit.TargetingDumpLines.Count > 0 )
                npcUnit.TargetingDumpLines.Clear();

            bool isDoingDump = isDoingDumpOfAll || (listMustBeInToDump != null && listMustBeInToDump.Contains( npcUnit ));

            if ( isDoingDump )
                npcUnit.TargetingDumpLines.Add( "Starting consideration.  TargetingOrder: " + TargetingOrder );

            workingRand.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );

            int individualsInThisNPC = npcUnit.CurrentSquadSize;

            //note: we don't care about any prior targeting passes, we're going to calculate this completely fresh

            NPCAttackPlan attackPlan = npcUnit.AttackPlan.Construction;
            attackPlan.SecondaryTargetsDamaged.ClearConstructionDictForStartingConstruction();

            NPCUnitStance stance = npcUnit.Stance;
            if ( stance == null )
            {
                if ( isDoingDump )
                    npcUnit.TargetingDumpLines.Add( "No stance!" );
                attackPlan.ClearTargetingBitsOnly();
                attackPlan.SecondaryTargetsDamaged.SwitchConstructionToDisplay(); //critical to do before returning!
                return;
            }

            MobileActorTypeDuringGameData dgd = npcUnit.GetTypeDuringGameData();
            if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
            {
                bool couldDoAll = true;
                foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                {
                    if ( kv.Value > kv.Key.Current )
                    {
                        couldDoAll = false;
                        break;
                    }
                }
                if ( !couldDoAll )
                {
                    if ( isDoingDump )
                        npcUnit.TargetingDumpLines.Add( "Could not afford all costs!" );
                    return;
                }
            }

            //always look for a fresh target, but using the same seed each time on a given turn
            ISimMapActor target = stance.TargetingLogic.Implementation.ChooseATargetInRangeThatCanBeShotRightNow( stance.TargetingLogic, npcUnit, currentPossibleMapActors, workingRand, isDoingDump );
            AttackAmounts damageToDo = AttackAmounts.Zero();
            if ( target != null )
            {
                //reset this before the next calc so it's consistent.
                workingRand.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                damageToDo = AttackHelper.PredictNPCDamageForImmediateFiringSolution_PrimaryTarget( npcUnit, target, workingRand );
            }

            if ( target == null || damageToDo.IsEmpty() )
            {
                if ( isDoingDump )
                    npcUnit.TargetingDumpLines.Add( "No target." );
                //evidently no target was in range, so skip it.
                attackPlan.ClearTargetingBitsOnly();
                attackPlan.SecondaryTargetsDamaged.SwitchConstructionToDisplay(); //critical to do before returning!
                return;
            }

            if ( isDoingDump )
            {
                npcUnit.TargetingDumpLines.Add( "New target: " + target.GetDisplayName() + "  Damage: " + damageToDo );
                if ( target.IncomingDamage.Construction.GetWouldBeDeadFromIncomingDamageTargeting() )
                    npcUnit.TargetingDumpLines.Add( "WouldBeDeadFromIncomingDamageTargeting already true one target!!" );
            }

            //log the damage
            attackPlan.TargetForStartOfNextTurn = target;
            attackPlan.PhysicalDamageIWillDoToThePrimaryTarget = damageToDo.Physical;
            attackPlan.MoraleDamageIWillDoToThePrimaryTarget = damageToDo.Morale;

            if ( !npcUnit.UnitType.IsMechStyleMovement && !npcUnit.UnitType.IsVehicle )
            {
                //if not a mech or vehicle, then rotate to the new target
                npcUnit.RotateObjectToFace( npcUnit.GetDrawLocation(), target.GetDrawLocation(), 1000 ); //do it instantly
            }

            //this keeps various units from overkilling the target
            target.IncomingDamage.Construction.AlterIncomingDamageTargeting( damageToDo.Physical, 1, individualsInThisNPC, damageToDo.Morale );

            if ( isDoingDump )
            {
                npcUnit.TargetingDumpLines.Add( "target incoming logged: physical: " + target.IncomingDamage.Construction.IncomingPhysicalDamageTargeting +
                    " morale: " + target.IncomingDamage.Construction.IncomingMoraleDamageTargeting + " WouldBeDeadFromIncomingDamageTargeting: " + target.IncomingDamage.Construction.GetWouldBeDeadFromIncomingDamageTargeting() );
            }

            //now see if there is an area of attack also happening
            float attackAreaSquared = npcUnit.GetAreaOfAttackSquared();
            attackPlan.AreaAttackRangeSquared = attackAreaSquared;
            bool wouldAreaOrPrimaryAttackAMachineActorRoot = target.GetIsPartOfPlayerForcesInAnyWay();
            bool wouldAreaOrPrimaryAttackAMachineActor = wouldAreaOrPrimaryAttackAMachineActorRoot;
            bool wouldAreaAttackSecondaryStrikeAMachineActor = target.GetIsPartOfPlayerForcesInAnyWay();
            if ( attackAreaSquared > 0 )
            {
                float intensityMultiplier = npcUnit.GetAreaOfAttackIntensityMultiplier();
                Vector3 targetLoc = target.GetDrawLocation();
                foreach ( ISimMapActor prospect in currentPossibleMapActors )
                {
                    if ( prospect.IsFullDead || prospect.IsInvalid || prospect == target || prospect == npcUnit )
                        continue; //this prospect is completely invalid, or is the same as the target or the attacker

                    Vector3 pos = prospect.GetDrawLocation();
                    if ( (pos - targetLoc).GetSquareGroundMagnitude() > attackAreaSquared )
                        continue; //this prospective target is out of range, so skip it

                    if ( !npcUnit.GetIsValidToCatchInAreaOfEffectExplosion_Current( prospect ) )
                        continue;

                    //reset this before the next calc so it's consistent.
                    workingRand.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                    damageToDo = AttackHelper.PredictNPCDamageForImmediateFiringSolution_SecondaryTarget( npcUnit, prospect, workingRand, intensityMultiplier );
                    if ( !damageToDo.IsEmpty() )
                    {
                        attackPlan.SecondaryTargetsDamaged.Construction[prospect] = damageToDo;

                        if ( prospect.GetIsPartOfPlayerForcesInAnyWay() )
                        {
                            playerDamagedActors[prospect] = true;
                            npcsDamagingPlayerUnits[npcUnit] = true;
                            wouldAreaOrPrimaryAttackAMachineActor = true;
                            wouldAreaAttackSecondaryStrikeAMachineActor = true;
                        }

                        //this keeps various units from overkilling the target
                        prospect.IncomingDamage.Construction.AlterIncomingDamageTargeting( damageToDo.Physical, 1, individualsInThisNPC, damageToDo.Morale );
                    }
                }
            }

            if ( wouldAreaOrPrimaryAttackAMachineActorRoot )
            {
                if ( target.GetIsPartOfPlayerForcesInAnyWay() )
                    playerDamagedActors[target] = true;
                npcsDamagingPlayerUnits[npcUnit] = true;
            }

            attackPlan.WouldAreaOrPrimaryAttackAMachineActor = wouldAreaOrPrimaryAttackAMachineActor;
            attackPlan.WouldAreaAttackSecondaryStrikeAMachineActor = wouldAreaAttackSecondaryStrikeAMachineActor;
            attackPlan.SecondaryTargetsDamaged.SwitchConstructionToDisplay();

            if ( wouldAreaOrPrimaryAttackAMachineActor || wouldAreaAttackSecondaryStrikeAMachineActor )
                SimCommon.NPCsWithAttackPlansThatHitPlayer.AddToConstructionList( npcUnit );
        }
        #endregion

        #region DoPostTargetingDumpIfNeeded
        private static void DoPostTargetingDumpIfNeeded()
        {
            if ( InputCaching.Debug_DoDumpNPCTargetingAll || InputCaching.Debug_DoDumpNPCTargetingInRangeOfCameraOnly || InputCaching.Debug_DoLogNPCTargetingToTooltip )
            {
                bool isForDumpLog = InputCaching.Debug_DoDumpNPCTargetingAll || InputCaching.Debug_DoDumpNPCTargetingInRangeOfCameraOnly;
                bool isForTooltip = InputCaching.Debug_DoLogNPCTargetingToTooltip;
                StringBuilder text = isForDumpLog ? new StringBuilder() : null;
                foreach ( NPCToSort sort in sortedNPCs )
                {
                    ISimNPCUnit unit = sort.Unit;
                    if ( unit.TargetingDumpLines.Count <= 0 )
                    {
                        if ( isForTooltip )
                            sort.Unit.DebugText = "No targeting lines.";
                        continue;
                    }

                    if ( isForDumpLog )
                    {
                        text.Append( "--------------------------------\n" );
                        text.Append( unit.ActorID ).Append( ": " ).Append( unit.UnitType.ID ).Append( "\n" );
                    }

                    string tooltipText = string.Empty;
                    List<string> lines = unit.TargetingDumpLines;
                    foreach ( string line in lines )
                    {
                        if ( isForDumpLog )
                            text.Append( line ).Append( "\n" );

                        if ( isForTooltip )
                        {
                            if ( tooltipText.Length > 0 )
                                tooltipText += "\n";
                            tooltipText += line;
                        }
                    }

                    if ( isForTooltip )
                    {
                        if ( tooltipText.Length <= 0 )
                            sort.Unit.DebugText = "Targeting lines were blank.";
                        else
                            sort.Unit.DebugText = tooltipText;
                    }

                    lines.Clear();
                }

                if ( isForDumpLog )
                {
                    string dumpFolder = Engine_Universal.CurrentPlayerDataDirectory + "Dumps/NPCTargeting/";
                    if ( !ArcenIO.DirectoryExists( dumpFolder ) )
                        ArcenIO.CreateDirectory( dumpFolder );

                    string dumpFile = dumpFolder + "Turn-" + SimCommon.Turn + "--" + DateTime.Now.Year + "-" + DateTime.Now.Month +
                        "-" + DateTime.Now.Day + "--" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + ".txt";
                    ArcenIO.WriteStringBuilderToDisk( dumpFile, text );
                    text.Clear();
                }
            }
        }
        #endregion

        #region CalculateAttackPlanIncoming
        private static void CalculateAttackPlanIncoming()
        {
            SimCommon.AttackPlan_IncomingDamageToPlayerUnits.ClearConstructionListForStartingConstruction();
            SimCommon.AttackPlan_AttackersAfterPlayerUnits.ClearConstructionListForStartingConstruction();

            if ( playerDamagedActors.Count > 0 )
            {
                foreach ( KeyValuePair<ISimMapActor, bool> kv in playerDamagedActors )
                {
                    ISimMapActor actor = kv.Key;
                    if ( actor.IsFullDead || actor.IsInvalid )
                        continue;

                    int incomingPhysicalDamage = actor.IncomingDamage.Construction.IncomingPhysicalDamageTargeting;
                    int incomingMoraleDamage = actor.IncomingDamage.Construction.IncomingMoraleDamageTargeting;
                    if ( incomingPhysicalDamage <= 0 && incomingMoraleDamage <= 0 )
                        continue;

                    bool wouldDie = actor.IncomingDamage.Construction.GetWouldBeDeadFromIncomingDamageTargeting();

                    int typeSortCode = 3;
                    if ( actor is ISimMachineUnit )
                        typeSortCode = 0;
                    else if ( actor is ISimMachineVehicle )
                        typeSortCode = 1;
                    else if ( actor is MachineStructure )
                        typeSortCode = 2;

                    AttackPlanIncoming incoming;
                    incoming.Target = actor;
                    incoming.IncomingPhysicalDamage = incomingPhysicalDamage;
                    incoming.IncomingMoraleDamage = incomingMoraleDamage;
                    incoming.WouldBeFatal = wouldDie;
                    incoming.TypeSortCode = typeSortCode;

                    SimCommon.AttackPlan_IncomingDamageToPlayerUnits.AddToConstructionList( incoming );
                }
            }

            if ( npcsDamagingPlayerUnits.Count > 0 )
            {
                foreach ( KeyValuePair<ISimNPCUnit, bool> kv in npcsDamagingPlayerUnits )
                {
                    ISimNPCUnit npc = kv.Key;
                    if ( npc.IsFullDead || npc.IsInvalid )
                        continue;

                    SimCommon.AttackPlan_AttackersAfterPlayerUnits.AddToConstructionList( npc );
                }
            }

            if ( SimCommon.AttackPlan_IncomingDamageToPlayerUnits.GetConstructionList().Count > 1 )
            {
                SimCommon.AttackPlan_IncomingDamageToPlayerUnits.SortConstructionList( delegate ( AttackPlanIncoming Left, AttackPlanIncoming Right )
                {
                    int val = Left.TypeSortCode.CompareTo( Right.TypeSortCode ); //asc
                    if ( val != 0)
                        return val;
                    val = Left.Target.GetDisplayName().CompareTo( Right.Target.GetDisplayName() ); //asc
                    if ( val != 0 )
                        return val;
                    val = Right.IncomingPhysicalDamage.CompareTo( Left.IncomingPhysicalDamage ); //desc
                    if ( val != 0 )
                        return val;

                    return Right.IncomingMoraleDamage.CompareTo( Left.IncomingMoraleDamage ); //desc
                } );
            }

            if ( SimCommon.AttackPlan_AttackersAfterPlayerUnits.GetConstructionList().Count > 1 )
            {
                SimCommon.AttackPlan_AttackersAfterPlayerUnits.SortConstructionList( delegate ( ISimNPCUnit Left, ISimNPCUnit Right )
                {
                    int val = Left.GetDisplayName().CompareTo( Right.GetDisplayName() ); //asc
                    if ( val != 0 )
                        return val;

                    return Left.ActorID.CompareTo( Right.ActorID ); //asc
                } );
            }

            SimCommon.AttackPlan_IncomingDamageToPlayerUnits.SwitchConstructionToDisplay();
            SimCommon.AttackPlan_AttackersAfterPlayerUnits.SwitchConstructionToDisplay();
        }
        #endregion

        private struct NPCToSort //apparently just gc it
        {
            public ISimNPCUnit Unit;
            public int TargetingOrder;
            public int AttackPower;
            public int UnitID;
        }
    }
}
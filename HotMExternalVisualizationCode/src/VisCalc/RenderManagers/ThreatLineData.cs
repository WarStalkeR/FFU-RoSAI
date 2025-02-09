using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;

namespace Arcen.HotM.ExternalVis
{
    public static class ThreatLineData
    {
        private static ISimMapMobileActor VariableActorMakingConsiderations = null;
        private static Vector3 VariableActor_DrawnDestination;
        private static ISimBuilding VariableActor_BuildingUnitWillBeAtOrNull;
        private static bool VariableActor_WillHaveMoved;
        private static ISimNPCUnit VariableActor_AttackedUnitByPlayer;
        private static AttackAmounts VariableActor_PredictedAttack;
        private static EnemyTargetingReason VariableActor_Reason;
        private static bool VariableActor_SkipThreatLinesFromDestination;
        private static ThreatLineLogic VariableActor_Logic;

        private static float AGAINST_PLAYER_ATTACK = 1.5f;

        public static float BaselineHeight = 0.008f;

        public static void ResetForNewFrame()
        {
            VariableActorMakingConsiderations = null;
            VariableActor_DrawnDestination = Vector3.negativeInfinity;
            VariableActor_BuildingUnitWillBeAtOrNull = null;
            VariableActor_WillHaveMoved = false;
            VariableActor_AttackedUnitByPlayer = null;
            VariableActor_PredictedAttack = AttackAmounts.Zero();
            VariableActor_Reason = EnemyTargetingReason.CurrentLocation_NoOp;
            VariableActor_SkipThreatLinesFromDestination = false;
            VariableActor_Logic = ThreatLineLogic.Normal;
        }

        #region SetVariableActorInfo
        public static void SetVariableActorInfo( ISimMapMobileActor Actor, Vector3 drawnDestination,
            ISimBuilding BuildingUnitWillBeAtOrNull, bool UnitWillHaveMoved, ISimNPCUnit AttackedUnitByPlayer, AttackAmounts PredictedAttack, 
            EnemyTargetingReason Reason, bool SkipThreatLinesFromDestination, ThreatLineLogic Logic )
        {
            if ( Reason == EnemyTargetingReason.CurrentLocation_NoOp || Actor == null )
                return; //do not do anything for this!

            VariableActorMakingConsiderations = Actor;
            VariableActor_DrawnDestination = drawnDestination;
            VariableActor_BuildingUnitWillBeAtOrNull = BuildingUnitWillBeAtOrNull;
            VariableActor_WillHaveMoved = UnitWillHaveMoved;
            VariableActor_AttackedUnitByPlayer = AttackedUnitByPlayer;
            VariableActor_PredictedAttack = AttackAmounts.Zero();
            VariableActor_Reason = Reason;
            VariableActor_SkipThreatLinesFromDestination = SkipThreatLinesFromDestination;
            VariableActor_Logic = Logic;
        }
        #endregion

        public static void RenderAnyAggroAndThreatLines()
        {
            if ( Engine_HotM.GameMode != MainGameMode.Streets )
                return; //on the city map also do not do this, let alone zodiac and so on

            bool shouldDrawThreatLines = InputCaching.ShouldShowDetailedTooltips || InputCaching.IsInInspectMode_Any ||
                ( SimCommon.CurrentCityLens?.ShowThreatLinesAlways?.Display ?? false) || (Engine_HotM.SelectedMachineActionMode?.ShowThreatLinesAlways ?? false);
            //(predictedDamage >= Actor.GetActorDataCurrent( ActorRefs.ActorHP, true )) ??

            if ( VariableActorMakingConsiderations != null )
            {
                //this means some unit is actually moving around
                HandleThreatLinesOrJustCalculationsAroundAFocus( VariableActorMakingConsiderations, VariableActor_DrawnDestination,
                    VariableActor_BuildingUnitWillBeAtOrNull, VariableActor_WillHaveMoved, 
                    out _, out _, out _, out _, out _, out _, out _, VariableActor_AttackedUnitByPlayer,
                    VariableActor_PredictedAttack, VariableActor_Reason, true, shouldDrawThreatLines, VariableActor_SkipThreatLinesFromDestination, VariableActor_Logic );
            }
            else
            {
                ISimMapActor focusedActor = null;
                if ( MouseHelper.ActorUnderCursor != null )
                    focusedActor = MouseHelper.ActorUnderCursor;
                else if ( Engine_HotM.SelectedActor != null )
                    focusedActor = Engine_HotM.SelectedActor;

                if ( focusedActor != null )
                    HandleLinesForFocusedActor( focusedActor, shouldDrawThreatLines );
            }

            HandleRemainderLinesForCellsNearCamera( shouldDrawThreatLines );
        }

        public static void HandleCalculationsWithoutDrawingYet( ISimMapMobileActor Actor, Vector3 drawnDestination,
            ISimBuilding BuildingUnitWillBeAtOrNull, bool UnitWillHaveMoved,
            out int NextTurn_EnemySquadInRange, out int NextTurn_EnemiesTargeting, out AttackAmounts NextTurn_DamageFromEnemies,
            out int AttackOfOpportunity_EnemySquadInRange, out int AttackOfOpportunity_EnemiesTargeting, out int AttackOfOpportunity_MinDamage, out int AttackOfOpportunity_MaxDamage,
            ISimNPCUnit AttackedUnitByPlayer, AttackAmounts PredictedAttack, EnemyTargetingReason Reason, bool SkipThreatLinesFromDestination, ThreatLineLogic Logic )
        {
            HandleThreatLinesOrJustCalculationsAroundAFocus( Actor, drawnDestination, BuildingUnitWillBeAtOrNull, UnitWillHaveMoved,
                out NextTurn_EnemySquadInRange, out NextTurn_EnemiesTargeting, out NextTurn_DamageFromEnemies, out AttackOfOpportunity_EnemySquadInRange,
                out AttackOfOpportunity_EnemiesTargeting, out AttackOfOpportunity_MinDamage, out AttackOfOpportunity_MaxDamage,
                AttackedUnitByPlayer, PredictedAttack, Reason, false, false, SkipThreatLinesFromDestination, Logic );
        }

        #region HandleThreatLinesOrJustCalculationsAroundAFocus
        private static Dictionary<int, bool> HandledUnitsAroundAFocusThisPass = Dictionary<int, bool>.Create_WillNeverBeGCed( 100, "ThreatLineData-HandledUnitsAroundAFocusThisPass" );
        private static MersenneTwister workingRandomForDrawingThreatLinesOnly = new MersenneTwister( 0 );
        private static void HandleThreatLinesOrJustCalculationsAroundAFocus( ISimMapMobileActor Actor, Vector3 drawnDestination,
            ISimBuilding BuildingUnitWillBeAtOrNull, bool UnitWillHaveMoved, 
            out int NextTurn_EnemySquadInRange, out int NextTurn_EnemiesTargeting, out AttackAmounts NextTurn_DamageFromEnemies,
            out int AttackOfOpportunity_EnemySquadInRange, out int AttackOfOpportunity_EnemiesTargeting, out int AttackOfOpportunity_MinDamage, out int AttackOfOpportunity_MaxDamage,
            ISimNPCUnit AttackedUnitByPlayer, AttackAmounts PredictedAttack, EnemyTargetingReason Reason, bool ActuallyHandleLines, 
            bool RenderAttacksAtOtherUnitsOfMine, bool SkipThreatLinesFromDestination, ThreatLineLogic Logic )
        {
            bool willBeCurrentLocation = false;
            bool shouldAddCloaking = false;
            bool shouldAddTakeCover = false;
            switch ( Reason )
            {
                case EnemyTargetingReason.CurrentLocation_NoOp:
                case EnemyTargetingReason.CurrentLocation_Attacking:
                    willBeCurrentLocation = true;
                    break;
                case EnemyTargetingReason.CurrentLocation_PlusTakeCover:
                    willBeCurrentLocation = true;
                    shouldAddTakeCover = true;
                    break;
                case EnemyTargetingReason.CurrentLocation_PlusCloaking:
                    willBeCurrentLocation = true;
                    shouldAddCloaking = true;
                    break;
            }

            if ( willBeCurrentLocation )
                drawnDestination = Actor.GetCollisionCenter();

            bool willHaveDoneAttack = AttackedUnitByPlayer != null;
            NPCCohort theoreticalAggroedCohort = AttackedUnitByPlayer?.FromCohort;
            MapPOI theoreticalAggroedPOI = AttackedUnitByPlayer?.HomePOI;
            Int64 currentFramesPrepped = RenderManager.FramesPrepped;

            int nextTurn_EnemySquadsTargeting = 0;
            int nextTurn_EnemiesTargeting = 0;
            AttackAmounts nextTurn_Damage = AttackAmounts.Zero();

            int attackOfOpportunity_EnemySquadsTargeting = 0;
            int attackOfOpportunity_EnemiesTargeting = 0;
            int attackOfOpportunity_DamageMin = 0;
            int attackOfOpportunity_DamageMax = 0;

            bool isMovingOutOfRangeAlways = Logic == ThreatLineLogic.ForceConsiderMovingOutOfRange;

            bool willHaveMovedForAttackOfOpportunityPurposes = UnitWillHaveMoved;
            if ( willHaveMovedForAttackOfOpportunityPurposes )
            {
                if ( Actor is ISimMachineVehicle )
                    willHaveMovedForAttackOfOpportunityPurposes = false; //vehicle shields stay up for attacks of opportunity
                else if ( Actor is ISimMachineUnit mUnit && mUnit.UnitType.IsConsideredMech )
                    willHaveMovedForAttackOfOpportunityPurposes = false; //mechs shields stay up for attacks of opportunity
            }

            ISimMapActor limitTo = null;
            if ( InputCaching.IsInInspectMode_Any || InputCaching.ShouldShowDetailedTooltips || ( SimCommon.CurrentCityLens?.SkipCombatLinesNotAimedAtHoveredUnit?.Display??false) )
            {
                limitTo = MouseHelper.CalculateActorUnderCursor();
                if ( limitTo == null )
                    limitTo = MouseHelper.StructureUnderCursor;
            }

            HandledUnitsAroundAFocusThisPass.Clear();

            MapCell cell1 = CityMap.TryGetWorldCellAtCoordinates( Actor.GetCollisionCenter() );
            MapCell cell2 = SkipThreatLinesFromDestination ? null : CityMap.TryGetWorldCellAtCoordinates( drawnDestination );
            MapTile tile1 = cell1?.ParentTile;
            MapTile tile2 = cell2?.ParentTile;
            if ( !shouldAddCloaking ) //if we're adding cloaking, they can't get us at all
            {
                MapPOI theoreticalPOI = null;
                if ( willBeCurrentLocation )
                    drawnDestination = Actor.GetCollisionCenter();
                else
                {
                    if ( BuildingUnitWillBeAtOrNull != null )
                        theoreticalPOI = BuildingUnitWillBeAtOrNull.CalculateLocationPOI();
                    else
                        theoreticalPOI = tile2?.GetPOIOfPoint( drawnDestination );
                }

                for ( int i = 0; i < 2; i++ )
                {
                    MapCell cell = i == 0 ? cell1 : cell2;
                    MapTile tile = i == 0 ? tile1 : tile2;

                    if ( cell == null || tile == null )
                        continue;
                    if ( i > 0 && tile1 == tile2 )
                        break; //if the same tile, don't bother us a second time

                    foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                    {
                        if ( !(actor is ISimNPCUnit npcUnit) )
                            continue; //only check NPC units

                        if ( npcUnit.IsFullDead )
                            continue;
                        if ( npcUnit.Stance?.TargetingLogic?.SkipsShootingAnyEnemies??true )
                            continue;

                        if ( HandledUnitsAroundAFocusThisPass.ContainsKey( npcUnit.ActorID ) )
                            continue; //already did this one
                        HandledUnitsAroundAFocusThisPass[npcUnit.ActorID] = true;

                        NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;

                        ISimMapActor target = attackPlan.TargetForStartOfNextTurn;
                        if ( target != Actor )
                        {
                            if ( target != null )
                            {
                                if ( ActuallyHandleLines && RenderAttacksAtOtherUnitsOfMine && attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor != currentFramesPrepped )
                                {
                                    if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                    { }
                                    else
                                    {
                                        attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                        Vector3 targetLoc = target.GetCollisionCenter();
                                        if ( target is ISimNPCUnit otherUnit )
                                        {
                                            if ( otherUnit.AttackPlan.Display.TargetForStartOfNextTurn == npcUnit && otherUnit.ActorID < npcUnit.ActorID )
                                            {
                                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( npcUnit.GetCollisionCenter(), targetLoc,
                                                    npcUnit.UnitType.HeightForCollisions, otherUnit.UnitType.HeightForCollisions, 0.3f,
                                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                            }
                                            else
                                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetCollisionCenter(), targetLoc, BaselineHeight,
                                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                        }
                                        else
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetEmissionLocation(), targetLoc, BaselineHeight,
                                                ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                        if ( attackPlan.AreaAttackRangeSquared > 0 )
                                        {
                                            DrawHelper.RenderCircle( targetLoc.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                                attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? ColorRefs.NPCUnitFullAggroLine.ColorHDR : ColorRefs.NPCOnNPCAggroLine.ColorHDR,
                                                attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? AGAINST_PLAYER_ATTACK : 1f );
                                        }
                                    }
                                }

                                if ( !willBeCurrentLocation )
                                {
                                    //if I am moving, would I be going into an area of effect?
                                    if ( attackPlan.AreaAttackRangeSquared > 0 && npcUnit.GetIsValidToCatchInAreaOfEffectExplosion_Current( Actor ) )
                                    {
                                        Vector3 distFromAOE = (drawnDestination - target.GetCollisionCenter());
                                        if ( distFromAOE.GetSquareGroundMagnitude() <= attackPlan.AreaAttackRangeSquared )
                                        {
                                            float intensityMultiplier = npcUnit.GetAreaOfAttackIntensityMultiplier();
                                            //I would be moving into range of taking damage from this attack
                                            nextTurn_EnemySquadsTargeting++;
                                            nextTurn_EnemiesTargeting += npcUnit.CurrentSquadSize;
                                            workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                            nextTurn_Damage.Add( AttackHelper.PredictNPCDamageForImmediateFiringSolution_SecondaryTarget( npcUnit, Actor, 
                                                workingRandomForDrawingThreatLinesOnly, intensityMultiplier ) );
                                        }
                                    }
                                }
                                else
                                {
                                    //I am not moving, so:
                                    //if they are not already after me, and they are after someone else, am I being caught in an area of effect?
                                    if ( attackPlan.SecondaryTargetsDamaged.Count > 0 )
                                    {
                                        if ( attackPlan.SecondaryTargetsDamaged.GetDisplayDict().TryGetValue( Actor, out AttackAmounts secondaryDamageToMe ) )
                                        {
                                            if ( !secondaryDamageToMe.IsEmpty() )
                                            {
                                                nextTurn_EnemySquadsTargeting++;
                                                nextTurn_EnemiesTargeting += npcUnit.CurrentSquadSize;
                                                nextTurn_Damage.Add( secondaryDamageToMe );
                                            }
                                        }
                                    }
                                }


                                continue; //skip if they are not already after me, and they are after someone else
                            }

                            //if they are after no-one, then let's see about things
                            Vector3 npcLoc = npcUnit.GetEmissionLocation();

                            //if would not be in range, nothing to do
                            if ( (npcLoc - drawnDestination).GetSquareGroundMagnitude() > npcUnit.GetAttackRangeSquared() )
                                continue; //lose the line if moving out of range

                            //make sure they are valid to shoot this unit
                            if ( !npcUnit.GetIsValidToAutomaticallyShootAt_TheoreticalOtherLocation( Actor, theoreticalPOI, BuildingUnitWillBeAtOrNull,
                                drawnDestination, theoreticalAggroedPOI, theoreticalAggroedCohort, willHaveDoneAttack ) )
                                continue; //they are blending in or something

                            int imagineThisAmountOfHealthLost = 0;
                            if ( npcUnit == AttackedUnitByPlayer && !PredictedAttack.IsEmpty() )
                            {
                                if ( PredictedAttack.GetWouldBeFatalTo( npcUnit ) )
                                    continue; //we think that unit will be dead, so don't include it at all
                                imagineThisAmountOfHealthLost = PredictedAttack.Physical;
                            }

                            nextTurn_EnemySquadsTargeting++;
                            nextTurn_EnemiesTargeting += npcUnit.CurrentSquadSize;
                            workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                            nextTurn_Damage.Add( AttackHelper.HandleAttackPredictionAgainstPlayer( npcUnit, Actor,
                                AttackedUnitByPlayer == null, !UnitWillHaveMoved, shouldAddTakeCover, imagineThisAmountOfHealthLost, workingRandomForDrawingThreatLinesOnly ) );

                            if ( ActuallyHandleLines && attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor != currentFramesPrepped )
                            {
                                if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                { }
                                else
                                {
                                    attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                    DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcLoc, drawnDestination, BaselineHeight,
                                        ColorRefs.NPCUnitDangerPredictionLine.ColorHDR, 1f );

                                    if ( npcUnit.GetHasAreaOfAttack() )
                                    {
                                        DrawHelper.RenderCircle( drawnDestination.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                            ColorRefs.NPCUnitDangerPredictionLine.ColorHDR, 1f );
                                    }
                                }
                            }
                        }
                        else
                        {
                            //if they are after me specifically, then let's do that
                            Vector3 npcLoc = npcUnit.GetEmissionLocation();

                            int imagineThisAmountOfHealthLost = 0;
                            if ( npcUnit == AttackedUnitByPlayer && !PredictedAttack.IsEmpty() )
                            {
                                if ( PredictedAttack.GetWouldBeFatalTo( npcUnit ) )
                                    continue; //we think that unit will be dead, so don't include it at all
                                imagineThisAmountOfHealthLost = PredictedAttack.Physical;
                            }

                            if ( !willBeCurrentLocation )
                            {
                                if ( isMovingOutOfRangeAlways|| ( npcLoc - drawnDestination).GetSquareGroundMagnitude() > npcUnit.GetAttackRangeSquared() )
                                {
                                    //attack of opportunity if moving out of range!

                                    attackOfOpportunity_EnemySquadsTargeting++;
                                    attackOfOpportunity_EnemiesTargeting += npcUnit.CurrentSquadSize;
                                    workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                    AttackAmounts fullDamage = AttackHelper.HandleAttackPredictionAgainstPlayer( npcUnit, Actor,
                                        AttackedUnitByPlayer == null, !UnitWillHaveMoved, shouldAddTakeCover, imagineThisAmountOfHealthLost, workingRandomForDrawingThreatLinesOnly );

                                    if ( willHaveMovedForAttackOfOpportunityPurposes != UnitWillHaveMoved )
                                    {
                                        workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                        AttackAmounts opportunityDamage = AttackHelper.HandleAttackPredictionAgainstPlayer( npcUnit, Actor, 
                                            AttackedUnitByPlayer == null, !willHaveMovedForAttackOfOpportunityPurposes, shouldAddTakeCover, imagineThisAmountOfHealthLost, workingRandomForDrawingThreatLinesOnly );

                                        attackOfOpportunity_DamageMin += Mathf.CeilToInt( opportunityDamage.Physical * MathRefs.AttackOfOpportunityDamageRange.FloatMin );
                                        attackOfOpportunity_DamageMax += Mathf.CeilToInt( opportunityDamage.Physical * MathRefs.AttackOfOpportunityDamageRange.FloatMax );
                                    }
                                    else
                                    {
                                        attackOfOpportunity_DamageMin += Mathf.CeilToInt( fullDamage.Physical * MathRefs.AttackOfOpportunityDamageRange.FloatMin );
                                        attackOfOpportunity_DamageMax += Mathf.CeilToInt( fullDamage.Physical * MathRefs.AttackOfOpportunityDamageRange.FloatMax );
                                    }

                                    if ( ActuallyHandleLines && attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor != currentFramesPrepped )
                                    {
                                        if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                        { }
                                        else
                                        {
                                            Vector3 attackOfOpportunityLocation = Actor.GetCollisionCenter();
                                            attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcLoc, attackOfOpportunityLocation, BaselineHeight,
                                                ColorRefs.NPCUnitAttackOfOpportunityLine.ColorHDR, 1f );

                                            if ( attackPlan.AreaAttackRangeSquared > 0 )
                                            {
                                                DrawHelper.RenderCircle( attackOfOpportunityLocation.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                                    ColorRefs.NPCUnitAttackOfOpportunityLine.ColorHDR, 1f );
                                            }
                                        }
                                    }
                                    continue;
                                }
                            }

                            nextTurn_EnemySquadsTargeting++;
                            nextTurn_EnemiesTargeting += npcUnit.CurrentSquadSize;
                            workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                            nextTurn_Damage.Add( AttackHelper.HandleAttackPredictionAgainstPlayer( npcUnit, Actor,
                                AttackedUnitByPlayer == null, !UnitWillHaveMoved, shouldAddTakeCover, imagineThisAmountOfHealthLost, workingRandomForDrawingThreatLinesOnly ) );

                            if ( ActuallyHandleLines && attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor != currentFramesPrepped )
                            {
                                if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                { }
                                else
                                {
                                    attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                    DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcLoc, drawnDestination, BaselineHeight,
                                        ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                    if ( attackPlan.AreaAttackRangeSquared > 0 )
                                    {
                                        DrawHelper.RenderCircle( drawnDestination.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                            ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                    }
                                }
                            }
                        }
                    }
                }
            }

            NextTurn_EnemySquadInRange = nextTurn_EnemySquadsTargeting;
            NextTurn_EnemiesTargeting = nextTurn_EnemiesTargeting;
            NextTurn_DamageFromEnemies = nextTurn_Damage;

            AttackOfOpportunity_EnemySquadInRange = attackOfOpportunity_EnemySquadsTargeting;
            AttackOfOpportunity_EnemiesTargeting = attackOfOpportunity_EnemiesTargeting;
            AttackOfOpportunity_MinDamage = attackOfOpportunity_DamageMin;
            AttackOfOpportunity_MaxDamage = attackOfOpportunity_DamageMax;
        }
        #endregion

        #region HandleLinesForFocusedActor
        public static void HandleLinesForFocusedActor( ISimMapActor FocusedActor, bool DrawLinesUnrelatedToTheFocusActor )
        {
            if ( FocusedActor == null || FocusedActor.IsFullDead )
                return;

            MapCell cell = FocusedActor.CalculateMapCell();
            if ( cell == null )
                return;

            MapTile tile = cell.ParentTile;
            if ( tile == null )
                return;

            ISimMapActor limitTo = null;
            if ( InputCaching.IsInInspectMode_Any || InputCaching.ShouldShowDetailedTooltips || (SimCommon.CurrentCityLens?.SkipCombatLinesNotAimedAtHoveredUnit?.Display ?? false) )
            {
                limitTo = MouseHelper.CalculateActorUnderCursor();
                if ( limitTo == null )
                    limitTo = MouseHelper.StructureUnderCursor;
            }

            Int64 currentFramesPrepped = RenderManager.FramesPrepped;

            //if the focused actor is a player unit or structure, show attackers against them
            if ( FocusedActor is ISimMachineActor || FocusedActor is MachineStructure )
            {
                Vector3 drawnDestination = FocusedActor.GetCollisionCenter();

                foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                {
                    if ( !(actor is ISimNPCUnit npcUnit) )
                        continue; //only check NPC units

                    if ( npcUnit.IsFullDead )
                        continue;
                    if ( npcUnit.Stance?.TargetingLogic?.SkipsShootingAnyEnemies ?? true )
                        continue;

                    NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                    ISimMapActor target = attackPlan.TargetForStartOfNextTurn;
                    if ( target != FocusedActor )
                    {
                        if ( target != null )
                        {
                            if ( DrawLinesUnrelatedToTheFocusActor )
                            {
                                if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                { }
                                else
                                {
                                    attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                    Vector3 targetLoc = target.GetCollisionCenter();
                                    if ( target is ISimNPCUnit otherUnit )
                                    {
                                        if ( otherUnit.AttackPlan.Display.TargetForStartOfNextTurn == npcUnit && otherUnit.ActorID < npcUnit.ActorID )
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( npcUnit.GetCollisionCenter(), targetLoc,
                                                npcUnit.UnitType.HeightForCollisions, otherUnit.UnitType.HeightForCollisions, 0.3f,
                                                ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                        else
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetCollisionCenter(), targetLoc, BaselineHeight,
                                                ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                    }
                                    else
                                        DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetEmissionLocation(), targetLoc, BaselineHeight,
                                            ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                    if ( attackPlan.AreaAttackRangeSquared > 0 )
                                    {
                                        DrawHelper.RenderCircle( targetLoc.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? ColorRefs.NPCUnitFullAggroLine.ColorHDR : ColorRefs.NPCOnNPCAggroLine.ColorHDR,
                                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? AGAINST_PLAYER_ATTACK : 1f );
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                        { }
                        else
                        {
                            //if they are after the target actor specifically, then draw that
                            Vector3 npcLoc = npcUnit.GetEmissionLocation();

                            attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcLoc, drawnDestination, BaselineHeight,
                                ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                            if ( attackPlan.AreaAttackRangeSquared > 0 )
                            {
                                DrawHelper.RenderCircle( drawnDestination.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                    ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );
                            }
                        }
                    }
                }
            }
            else
            {
                if ( FocusedActor is ISimNPCUnit focusedNPC )
                {
                    //if the actor we are focusing on is an NPC, then show its attacks in particular

                    foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                    {
                        if ( !(actor is ISimNPCUnit npcUnit) )
                            continue; //only check NPC units

                        if ( npcUnit.IsFullDead )
                            continue;
                        if ( npcUnit.Stance?.TargetingLogic?.SkipsShootingAnyEnemies ?? true )
                            continue;

                        NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                        ISimMapActor target = attackPlan.TargetForStartOfNextTurn;
                        if ( npcUnit != focusedNPC ) //if considering some other NPC than our focus
                        {
                            if ( target != null ) //if that other NPC has an attack target
                            {
                                if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                { }
                                else
                                {
                                    if ( DrawLinesUnrelatedToTheFocusActor )
                                    {
                                        attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                        Vector3 targetLoc = target.GetCollisionCenter();
                                        if ( target is ISimNPCUnit otherUnit )
                                        {
                                            if ( otherUnit.AttackPlan.Display.TargetForStartOfNextTurn == npcUnit && otherUnit.ActorID < npcUnit.ActorID )
                                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( npcUnit.GetCollisionCenter(), targetLoc,
                                                    npcUnit.UnitType.HeightForCollisions, otherUnit.UnitType.HeightForCollisions, 0.3f,
                                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                            else
                                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetCollisionCenter(), targetLoc, BaselineHeight,
                                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                        }
                                        else
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetEmissionLocation(), targetLoc, BaselineHeight,
                                                ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                        if ( attackPlan.AreaAttackRangeSquared > 0 )
                                        {
                                            DrawHelper.RenderCircle( targetLoc.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                                attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? ColorRefs.NPCUnitFullAggroLine.ColorHDR : ColorRefs.NPCOnNPCAggroLine.ColorHDR,
                                                attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? AGAINST_PLAYER_ATTACK : 1f );
                                        }
                                    }
                                }
                            }
                        }
                        else //if this is about the focused npc, specifically
                        {
                            if ( target != null ) //if that focused NPC has an attack target
                            {
                                if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                                { }
                                else
                                {
                                    attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;
                                    Vector3 targetLoc = target.GetCollisionCenter();
                                    //draw any target it has
                                    if ( target is ISimNPCUnit otherUnit )
                                    {
                                        if ( otherUnit.AttackPlan.Display.TargetForStartOfNextTurn == npcUnit && otherUnit.ActorID < npcUnit.ActorID )
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( npcUnit.GetCollisionCenter(), targetLoc,
                                                npcUnit.UnitType.HeightForCollisions, otherUnit.UnitType.HeightForCollisions, 0.3f,
                                                ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                        else
                                            DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetCollisionCenter(), targetLoc, BaselineHeight,
                                                ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                                    }
                                    else
                                        DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetEmissionLocation(), targetLoc, BaselineHeight,
                                            ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                                    if ( attackPlan.AreaAttackRangeSquared > 0 )
                                    {
                                        DrawHelper.RenderCircle( targetLoc.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? ColorRefs.NPCUnitFullAggroLine.ColorHDR : ColorRefs.NPCOnNPCAggroLine.ColorHDR,
                                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? AGAINST_PLAYER_ATTACK : 1f );
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion HandleLinesForFocusedActor

        #region HandleRemainderLinesForCellsNearCamera
        public static void HandleRemainderLinesForCellsNearCamera( bool DrawLinesUnrelatedToYourUnits )
        {
            Vector3 centerPos = CameraCurrent.CameraBodyPosition;

            Int64 currentFramesPrepped = RenderManager.FramesPrepped;

            ISimMapActor limitTo = null;
            if ( InputCaching.IsInInspectMode_Any || InputCaching.ShouldShowDetailedTooltips || (SimCommon.CurrentCityLens?.SkipCombatLinesNotAimedAtHoveredUnit?.Display ?? false) )
            {
                limitTo = MouseHelper.CalculateActorUnderCursor();
                if ( limitTo == null )
                    limitTo = MouseHelper.StructureUnderCursor;
            }

            foreach ( ISimNPCUnit npcUnit in SimCommon.NPCsWithTargets_MainThreadOnly.GetDisplayList() )
            {
                NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                if ( attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor == currentFramesPrepped )
                    continue; //if we already did this unit, don't do it again!
                //mark the unit as done
                attackPlan.NonSimLastFramePrepIHaveDrawnLinesFor = currentFramesPrepped;

                bool npcIsInvisible = npcUnit.GetIsCurrentlyInvisible( InvisibilityPurpose.ForCameraFocus );

                ISimMapActor target = attackPlan.TargetForStartOfNextTurn;
                if ( target != null ) //if that other NPC has an attack target
                {
                    if ( !(npcUnit.CalculateMapCell()?.IsConsideredInCameraView??false) &&
                        !(target.CalculateMapCell()?.IsConsideredInCameraView ?? false) )
                    {
                        //if neither is in the camera frustum, don't draw it.
                        continue;
                    }
                    if ( limitTo != null && limitTo != npcUnit && limitTo != target )
                        continue;

                    if ( npcIsInvisible )
                    {
                        if ( target.GetIsCurrentlyInvisible( InvisibilityPurpose.ForCameraFocus ) )
                            continue; //if both invisible, do not show
                    }

                    bool strikeIsRelevantBecauseOfAOE = (attackPlan.AreaAttackRangeSquared > 0 && (DrawLinesUnrelatedToYourUnits || attackPlan.WouldAreaOrPrimaryAttackAMachineActor));

                    Vector3 targetLoc = target.GetCollisionCenter();
                    if ( target is ISimNPCUnit otherUnit && !otherUnit.GetIsPartOfPlayerForcesInAnyWay() )
                    {
                        if ( DrawLinesUnrelatedToYourUnits || strikeIsRelevantBecauseOfAOE || npcUnit.GetIsPartOfPlayerForcesInAnyWay() )
                        {
                            if ( otherUnit.AttackPlan.Display.TargetForStartOfNextTurn == npcUnit && otherUnit.ActorID < npcUnit.ActorID )
                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Overhead( npcUnit.GetCollisionCenter(), targetLoc,
                                    npcUnit.UnitType.HeightForCollisions, otherUnit.UnitType.HeightForCollisions, 0.3f,
                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                            else
                                DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetCollisionCenter(), targetLoc, BaselineHeight,
                                    ColorRefs.NPCOnNPCAggroLine.ColorHDR, 1f );
                        }
                    }
                    else
                        DrawHelper.RenderPolylineFromSourceToTargetPoint_CaliperStyle_Direct( npcUnit.GetEmissionLocation(), targetLoc, BaselineHeight,
                            ColorRefs.NPCUnitFullAggroLine.ColorHDR, AGAINST_PLAYER_ATTACK );

                    if ( strikeIsRelevantBecauseOfAOE )
                    {
                        DrawHelper.RenderCircle( targetLoc.ReplaceY( BaselineHeight ), npcUnit.GetAreaOfAttack(),
                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? ColorRefs.NPCUnitFullAggroLine.ColorHDR : ColorRefs.NPCOnNPCAggroLine.ColorHDR,
                            attackPlan.WouldAreaOrPrimaryAttackAMachineActor ? AGAINST_PLAYER_ATTACK : 1f );
                    }
                }
            }
        }
        #endregion HandleRemainderLinesForCellsNearCamera

        #region HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive
        private static Dictionary<int, bool> HandledAttackOfOpportunityThisPass = Dictionary<int, bool>.Create_WillNeverBeGCed( 100, "ThreatLineData-HandledAttackOfOpportunityThisPass" );
        public static void HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive( ISimMapMobileActor Actor, 
            Vector3 NewLocationForAttackCalculationPurposes, ThreatLineLogic Logic, Action DoThisIfActorLives )
        {
            if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity < 0 )
                SimCommon.CurrentlyDoingThisManyAttackOfOpportunity = 0;
            if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity > 0 )
                return;

            bool checkCoverStatus = false; //normally no checking cover, since we're moving out of cover
            if ( Actor is ISimMachineVehicle )
                checkCoverStatus = true; //but vehicle shields are still up right as they move
            else if ( Actor is ISimMachineUnit mUnit && mUnit.UnitType.IsConsideredMech )
                checkCoverStatus = true; //same with mechs

            bool isMovingOutOfRangeAlways = Logic == ThreatLineLogic.ForceConsiderMovingOutOfRange;

            HandledAttackOfOpportunityThisPass.Clear();
            MapCell cell1 = CityMap.TryGetWorldCellAtCoordinates( Actor.GetCollisionCenter() );
            MapCell cell2 = CityMap.TryGetWorldCellAtCoordinates( NewLocationForAttackCalculationPurposes );
            MapTile tile1 = cell1?.ParentTile;
            MapTile tile2 = cell2?.ParentTile;
            if ( !Actor.IsCloaked ) //if we're cloaked, then do nothing
            {
                for ( int i = 0; i < 2; i++ )
                {
                    MapCell cell = i == 0 ? cell1 : cell2;
                    MapTile tile = i == 0 ? tile1 : tile2;

                    if ( cell == null || tile == null )
                        continue;
                    if ( i > 0 && tile1 == tile2 )
                        break; //if the same tile, don't bother us a second time

                    foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                    {
                        if ( !(actor is ISimNPCUnit npcUnit) )
                            continue; //only check NPC units

                        if ( npcUnit.IsFullDead )
                            continue;
                        if ( npcUnit.Stance?.TargetingLogic?.SkipsShootingAnyEnemies ?? true )
                            continue;

                        if ( HandledAttackOfOpportunityThisPass.ContainsKey( npcUnit.ActorID ) )
                            continue; //already did this one
                        HandledAttackOfOpportunityThisPass[npcUnit.ActorID] = true;

                        NPCAttackPlan attackPlan = npcUnit.AttackPlan.Display;
                        ISimMapActor target = attackPlan.TargetForStartOfNextTurn;
                        if ( target != Actor )
                        { } //this npc is not targeting me, so is irrelevant
                        else
                        {
                            //if they are after me specifically, then let's do that
                            Vector3 npcLoc = npcUnit.GetEmissionLocation();

                            if ( isMovingOutOfRangeAlways || ( npcLoc - NewLocationForAttackCalculationPurposes).GetSquareGroundMagnitude() > npcUnit.GetAttackRangeSquared() )
                            {
                                //attack of opportunity if moving out of range!
                                SimCommon.CurrentlyDoingThisManyAttackOfOpportunity++;

                                workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                AttackAmounts fullDamage = AttackHelper.HandleAttackPredictionAgainstPlayer( npcUnit, Actor, true,
                                    checkCoverStatus, false,
                                    0, workingRandomForDrawingThreatLinesOnly );

                                int actualDamageToDo = Mathf.CeilToInt( fullDamage.Physical * MathRefs.AttackOfOpportunityDamageRange.GetRandomBetweenInclusive( Engine_Universal.PermanentQualityRandom ) );

                                //this is copied to a secondary buffer specifically to avoid issues with if another targeting pass happens in the middle of all this
                                attackPlan.SecondaryTargetDamageQueue.ClearAndCopyFrom( attackPlan.SecondaryTargetsDamaged.GetDisplayDict() );

                                HandbookRefs.AttacksOfOpportunity.DuringGame_UnlockIfNeeded( true );

                                //have the npc fire now
                                npcUnit.FireWeaponsAtTargetPoint( target.GetEmissionLocation(),
                                    Engine_Universal.PermanentQualityRandom,//this is used only for some visual bits, and we are on the main thread, so all good
                                    delegate //this will be called-back on the main thread
                                    {
                                        //this is still the main thread
                                        workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                        AttackAmounts opportunityAttack;
                                        opportunityAttack.Physical = actualDamageToDo;
                                        opportunityAttack.Morale = 0;

                                        AttackHelper.DoNPCDelayedAttack_UsePriorCalculation_PrimaryOrSecondary( npcUnit, target, workingRandomForDrawingThreatLinesOnly, opportunityAttack );

                                        //now handle any secondaries
                                        foreach ( KeyValuePair<ISimMapActor, AttackAmounts> kv in attackPlan.SecondaryTargetDamageQueue )
                                        {
                                            if ( !kv.Value.IsEmpty() )
                                            {
                                                int actualDamageToDoToSecondary = Mathf.CeilToInt( kv.Value.Physical * 
                                                    MathRefs.AttackOfOpportunityDamageRange.GetRandomBetweenInclusive( Engine_Universal.PermanentQualityRandom ) );

                                                if ( actualDamageToDoToSecondary > 0 )
                                                {
                                                    opportunityAttack.Physical = actualDamageToDoToSecondary;
                                                    workingRandomForDrawingThreatLinesOnly.ReinitializeWithSeed( npcUnit.CurrentTurnSeed + npcUnit.NonSimUniqueID );
                                                    AttackHelper.DoNPCDelayedAttack_UsePriorCalculation_PrimaryOrSecondary( npcUnit, kv.Key, workingRandomForDrawingThreatLinesOnly, opportunityAttack );
                                                }
                                            }
                                        }
                                        attackPlan.SecondaryTargetDamageQueue.Clear();

                                        //there may be multiple shots being fired...
                                        SimCommon.CurrentlyDoingThisManyAttackOfOpportunity--;

                                        //so wait until we get to the last of them
                                        if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity <= 0)
                                        {
                                            SimCommon.CurrentlyDoingThisManyAttackOfOpportunity = 0;
                                            //the player's unit lived!  So have them do that thing they wanted to do.
                                            if ( !target.IsFullDead )
                                                DoThisIfActorLives();
                                        }
                                    } );
                            }
                        }
                    }
                }
            }

            //after the above, if no attacks of opportunity were launched, just do the thing!
            if ( SimCommon.CurrentlyDoingThisManyAttackOfOpportunity == 0 )
                DoThisIfActorLives();
        }
        #endregion HandleAttackOfOpportunityIfNeeded_ThenDoPlayerActionIfActorStillAlive
    }

    public enum ThreatLineLogic
    {
        Normal,
        ForceConsiderMovingOutOfRange
    }
}

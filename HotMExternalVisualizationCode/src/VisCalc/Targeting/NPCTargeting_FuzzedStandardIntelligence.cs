using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class NPCTargeting_FuzzedStandardIntelligence : INPCUnitTargetingLogicImplementation
    {
        public struct NPCTargetingData
        {
            public ISimMapActor Target;
            public float DistanceSquared;
            public int EffectiveDamageDone;
            public int RawRealDamageDone;
            public int DamagePercentage;
            public int TargetPercentageRemain;
            public int AmountTheyHaveAggroedMe;
            public bool IsAVeryLowQualityTarget;
            public bool WouldAlreadyBeDeadFromIncoming;
            public bool IsAStructure;
        }

        public struct OverrideTargetingData
        {
            public ISimMapActor Target;
            public float DistanceSquared;
            public int RawRealDamageDone;
            public int ServiceDisruptionAmount;
            public int IncomingPhysicalDamageTargeting;
            public int TargetPhysicalOverage;
        }

        private readonly List<NPCTargetingData> targetingList = List<NPCTargetingData>.Create_WillNeverBeGCed( 40, "NPCTargeting_FuzzedStandardIntelligence-targetingData" );
        private readonly List<OverrideTargetingData> overrideList = List<OverrideTargetingData>.Create_WillNeverBeGCed( 40, "NPCTargeting_FuzzedStandardIntelligence-targetingData" );

        public ISimMapActor ChooseATargetInRangeThatCanBeShotRightNow( NPCUnitTargetingLogic Logic, ISimNPCUnit NPCUnit, List<ISimMapActor> CurrentActorSet, MersenneTwister Rand, bool ShouldDoTargetingDump )
        {
            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "FuzzedStandardIntelligence" );

            Vector3 attackerLocation = NPCUnit.GetDrawLocation();

            if ( CurrentActorSet.Count == 0 )
                return null;

            NPCUnitStance stance = NPCUnit.Stance;
            if ( stance == null )
                return null;

            MachineJobTag extraAggroMachineJob = NPCUnit?.IsManagedUnit?.ExtraAggroAgainstMachineStructures;
            int extraAggroMachineJobAmount = NPCUnit?.IsManagedUnit?.ExtraAggroAgainstMachineStructuresAmount??0;
            if ( extraAggroMachineJob == null )
                extraAggroMachineJobAmount = 0;
            if ( extraAggroMachineJobAmount <= 0 )
                extraAggroMachineJob = null;

            NPCCohort attackerGroup = NPCUnit.FromCohort;
            float attackerRangeSquared = NPCUnit.GetAttackRangeSquared();
            targetingList.Clear();
            overrideList.Clear();

            float minDistance = 100000;
            float maxDistance = 0;
            int largestAggro = 0;

            int numberChecked = 0;

            int minDamageDone = -1;
            int maxDamageDone = -1;

            foreach ( ISimMapActor actor in CurrentActorSet )
            {
                if ( actor == null || actor.IsFullDead || actor.Equals( NPCUnit ) )
                    continue;

                Vector3 targetLocation = actor.GetDrawLocation();
                float distanceSquared = (attackerLocation - targetLocation).GetSquareGroundMagnitude();
                if ( distanceSquared > attackerRangeSquared )
                {
                    //if ( stance.BackgroundWarType > 0 && ShouldDoTargetingDump )
                    //{
                    //    NPCUnit.TargetingDumpLines.Add( "skip-distance war" + stance.BackgroundWarType + " amountAggroed: " +
                    //        actor.GetAmountHasAggroedNPCCohort( attackerGroup, stance, (actor as ISimNPCUnit)?.Stance ) + " otherStance: " + ((actor as ISimNPCUnit)?.Stance?.GetDisplayName() ?? "null") + " war-" +
                    //        ((actor as ISimNPCUnit)?.Stance?.BackgroundWarType ?? -1) );
                    //}
                    //else if ( ShouldDoTargetingDump )
                    //    NPCUnit.TargetingDumpLines.Add( "skip-distance war" + stance.BackgroundWarType );
                    continue; //this potential target is out of our range, so ignore it
                }

                //if ( actor is ISimNPCUnit )
                //{
                //    if ( stance.BackgroundWarType > 0 && ShouldDoTargetingDump )
                //    {
                //        NPCUnit.TargetingDumpLines.Add( "in-range war" + stance.BackgroundWarType + " amountAggroed: " +
                //            actor.GetAmountHasAggroedNPCCohort( attackerGroup, stance, (actor as ISimNPCUnit)?.Stance ) + " otherStance: " + ((actor as ISimNPCUnit)?.Stance?.GetDisplayName() ?? "null") + " war-" +
                //            ((actor as ISimNPCUnit)?.Stance?.BackgroundWarType ?? -1) );
                //    }
                //    else if ( ShouldDoTargetingDump )
                //        NPCUnit.TargetingDumpLines.Add( "in-range war" + stance.BackgroundWarType );
                //}

                if ( actor.GetIsCurrentlyInvisible( InvisibilityPurpose.ForNPCTargeting ) )
                    continue;
                if ( !NPCUnit.GetIsValidToAutomaticallyShootAt_Current( actor ) )
                {
                    //if ( actor is ISimNPCUnit u2 && !u2.Stance.IsConsideredCohortGuard )
                    //{
                    //    if ( stance.BackgroundWarType > 0 && ShouldDoTargetingDump )
                    //    {
                    //        NPCUnit.TargetingDumpLines.Add( "skip-shot war" + stance.BackgroundWarType + " amountAggroed: " +
                    //            actor.GetAmountHasAggroedNPCCohort( attackerGroup, stance, (actor as ISimNPCUnit)?.Stance ) + " otherStance: " + ((actor as ISimNPCUnit)?.Stance?.GetDisplayName() ?? "null") + " war-" +
                    //            ((actor as ISimNPCUnit)?.Stance?.BackgroundWarType ?? -1) );
                    //    }
                    //    else if ( ShouldDoTargetingDump )
                    //        NPCUnit.TargetingDumpLines.Add( "skip-shot war" + stance.BackgroundWarType );
                    //}
                    continue;
                }
                //note: this was redundant
                //if ( !AttackHelper.GetTargetIsInRange( NPCUnit, actor ) )
                //    continue; //if not in range, don't think about anything more -- basically, that's off limits

                numberChecked++;

                if ( stance?.WillIgnoreDamageToMachineStructuresInDamageCalculations ?? false )
                {
                    if ( actor is MachineStructure structure )
                    {
                        if ( extraAggroMachineJobAmount > 0 && extraAggroMachineJob != null )
                        {
                            if ( structure?.CurrentJob?.Tags?.ContainsKey( extraAggroMachineJob.ID ) ?? false )
                            { } //attack them!
                            else
                                continue; //ignored!
                        }
                        else
                            continue; //ignored!
                    }
                }

                AttackAmounts damageDone = AttackHelper.PredictNPCDamageForTargeting( NPCUnit, actor, true, true, false, CalculationType.PredictionDuringTurnChange );

                //now see if there is an area of attack also happening
                //if we can do more damage from that attack, we want to know
                float attackAreaSquared = NPCUnit.GetAreaOfAttackSquared();
                if ( attackAreaSquared > 0 )
                {
                    float intensityMultiplier = NPCUnit.GetAreaOfAttackIntensityMultiplier();
                    foreach ( ISimMapActor prospect in CurrentActorSet )
                    {
                        if ( prospect.IsFullDead || prospect.IsInvalid || prospect == actor || prospect == NPCUnit )
                            continue; //this prospect is completely invalid, or is the same as the target or the attacker

                        Vector3 pos = prospect.GetDrawLocation();
                        if ( (pos - targetLocation).GetSquareGroundMagnitude() > attackAreaSquared )
                            continue; //this prospective target is out of range, so skip it

                        if ( !NPCUnit.GetIsValidToCatchInAreaOfEffectExplosion_Current( prospect ) )
                            continue;

                        if ( stance?.WillIgnoreDamageToMachineStructuresInDamageCalculations ?? false )
                        {
                            if ( prospect is MachineStructure )
                                continue; //ignored!
                        }

                        AttackAmounts secondaryDamageToDo = AttackHelper.PredictNPCDamageForImmediateFiringSolution_SecondaryTarget( NPCUnit, prospect, Rand, intensityMultiplier );
                        if ( !secondaryDamageToDo.IsEmpty() )
                            damageDone.Add( secondaryDamageToDo );
                    }
                }

                bool wouldBeDead = actor.IncomingDamage.Construction.GetWouldBeDeadFromIncomingDamageTargeting();

                int amountAggroed = actor.GetAmountHasAggroedNPCCohort( attackerGroup, stance, (actor as ISimNPCUnit)?.Stance );
                if ( extraAggroMachineJobAmount > 0 && extraAggroMachineJob != null )
                {
                    if ( actor is MachineStructure structure )
                    {
                        if ( structure?.CurrentJob?.Tags?.ContainsKey( extraAggroMachineJob.ID ) ?? false )
                            amountAggroed += extraAggroMachineJobAmount;
                    }
                }

                //if ( stance.BackgroundWarType > 0 && ShouldDoTargetingDump )
                //{
                //    NPCUnit.TargetingDumpLines.Add( "war" + stance.BackgroundWarType + " amountAggroed: " + amountAggroed + " otherStance: " + ((actor as ISimNPCUnit)?.Stance?.GetDisplayName() ?? "null") + " war-" +
                //        ((actor as ISimNPCUnit)?.Stance?.BackgroundWarType ?? -1) );
                //}
                //else if (ShouldDoTargetingDump )
                //    NPCUnit.TargetingDumpLines.Add( "war" + stance.BackgroundWarType + " amountAggroed: " + amountAggroed );

                damageDone.GetTargetingData( actor, out int HighestDamagePercentage, out int LowestTargetPercentageRemain, out int HighestRawNumberDone );

                int effectiveDamageDone = HighestRawNumberDone;
                if ( effectiveDamageDone > 0 ) //boost against armored targets if we can damage them
                {
                    int targetArmor = actor.GetActorDataCurrent( ActorRefs.ActorArmorPlating, true );
                    if ( targetArmor > 0 )
                    {
                        int armorBoost = targetArmor / 2;
                        armorBoost *= armorBoost;

                        effectiveDamageDone += armorBoost;
                    }
                }

                int serviceDisruptionAmount = 0;
                if ( actor is ISimMachineActor machineActor )
                {
                    if ( machineActor.CurrentActionOverTime != null )
                    {
                        amountAggroed += machineActor.CurrentActionOverTime.Type.AggroAmountForActionOverTimeHunters;
                    }

                    serviceDisruptionAmount = machineActor.GetStatusIntensity( CommonRefs.ServiceDisruption );
                    if ( serviceDisruptionAmount > 0 )
                    {
                        amountAggroed += serviceDisruptionAmount;
                    }
                }

                if ( stance.ExtraAggroAgainstNonMachineForces > 0 && !actor.GetIsPartOfPlayerForcesInAnyWay() )
                {
                    amountAggroed += stance.ExtraAggroAgainstNonMachineForces;
                    if ( effectiveDamageDone > 0 )
                    {
                        int fakeDamageBoost = stance.ExtraAggroAgainstNonMachineForces / 20;
                        fakeDamageBoost *= fakeDamageBoost;

                        effectiveDamageDone += fakeDamageBoost;
                    }
                }

                if ( amountAggroed > largestAggro && !wouldBeDead ) //don't include stuff that would be dead in this part of the aggro math
                    largestAggro = amountAggroed;

                if ( minDistance > distanceSquared )
                    minDistance = distanceSquared;
                if ( maxDistance < distanceSquared )
                    maxDistance = distanceSquared;

                if ( minDamageDone < 0 || effectiveDamageDone < minDamageDone )
                    minDamageDone = effectiveDamageDone;
                if ( effectiveDamageDone > maxDamageDone )
                    maxDamageDone = effectiveDamageDone;

                //we can shoot this target, so learn more about it
                NPCTargetingData targetData;
                targetData.Target = actor;
                targetData.DistanceSquared = distanceSquared;
                targetData.EffectiveDamageDone = effectiveDamageDone;
                targetData.RawRealDamageDone = HighestRawNumberDone;
                targetData.DamagePercentage = HighestDamagePercentage;
                targetData.TargetPercentageRemain = LowestTargetPercentageRemain;
                targetData.AmountTheyHaveAggroedMe = amountAggroed;
                targetData.IsAVeryLowQualityTarget = actor.GetAmIAVeryLowPriorityTargetRightNow();
                targetData.WouldAlreadyBeDeadFromIncoming = wouldBeDead;
                targetData.IsAStructure = actor is MachineStructure;

                targetingList.Add( targetData );

                if ( serviceDisruptionAmount > 0 )
                {
                    int health = actor.GetActorDataCurrent( ActorRefs.ActorHP, false );

                    OverrideTargetingData overrideData;
                    overrideData.Target = actor;
                    overrideData.DistanceSquared = distanceSquared;
                    overrideData.RawRealDamageDone = HighestRawNumberDone;
                    overrideData.ServiceDisruptionAmount = serviceDisruptionAmount;
                    overrideData.IncomingPhysicalDamageTargeting = actor.IncomingDamage.Construction.IncomingPhysicalDamageTargeting;
                    overrideData.TargetPhysicalOverage = overrideData.RawRealDamageDone + overrideData.IncomingPhysicalDamageTargeting - health;

                    if ( overrideData.TargetPhysicalOverage > 0 && health > 0 )
                    {
                        int overageNumber = Mathf.RoundToInt( ( (float)overrideData.TargetPhysicalOverage / (float)health ) * 1500 );
                        if ( overageNumber <= serviceDisruptionAmount )
                            overrideList.Add( overrideData );
                    }
                    else
                        overrideList.Add( overrideData );
                }
            }

            if ( overrideList.Count > 0 )
            {
                ISimMapActor closestOverride = null;
                float closetOverrideDistance = 0;

                foreach ( OverrideTargetingData target in overrideList )
                {
                    if ( closestOverride == null || closetOverrideDistance < target.DistanceSquared )
                    {
                        closestOverride = target.Target;
                        closetOverrideDistance = target.DistanceSquared;
                    }
                }

                if ( closestOverride != null )
                {
                    return closestOverride;
                }
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "largestAggro: " + largestAggro + " minDamageDone: " + minDamageDone + " maxDamageDone: " + maxDamageDone );

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "numberChecked: " + numberChecked );

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "targetingList: " + targetingList.Count );

            if ( targetingList.Count == 0 )
                return null;
            else if ( targetingList.Count == 1 )
            {
                ISimMapActor toReturn = targetingList[0].Target;
                targetingList.Clear();
                if ( ShouldDoTargetingDump )
                    NPCUnit.TargetingDumpLines.Add( "only possible target was selected" );
                //ArcenDebugging.LogSingleLine( "Only target: " + NPCUnit.GetDisplayName() + " shoots " + toReturn.GetDisplayName(), Verbosity.DoNotShow );
                return toReturn;
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "find optimal by damage" );

            //try to find something reasonably optimal
            NPCTargetingData bestInGroup = new NPCTargetingData();
            float maxDistanceToUse = minDistance + ((maxDistance - minDistance) * 0.3f);

            int minAggroForBestDamage = Mathf.RoundToInt( (float)largestAggro * 0.4f );

            foreach ( NPCTargetingData targetingData in targetingList )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.EffectiveDamageDone < minDamageDone + minDamageDone )
                    continue; //look for things where we do a really out-sized amount of damage
                if ( targetingData.AmountTheyHaveAggroedMe < minAggroForBestDamage && !targetingData.IsAStructure )
                    continue; //ignore anything that is not the fuzzed most-aggroed, unless it's a structure
                if ( targetingData.IsAVeryLowQualityTarget )
                    continue; //ignore stuff that is a very low quality target for now

                if ( bestInGroup.Target == null || targetingData.EffectiveDamageDone > bestInGroup.EffectiveDamageDone ||
                    Rand.Next( 0, 100 ) < 25 ) //the fuzz factor
                    bestInGroup = targetingData;
            }

            if ( bestInGroup.Target != null )
            {
                targetingList.Clear();
                //ArcenDebugging.LogSingleLine( "Best target group A: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                return bestInGroup.Target; //we found something relatively close, which we are aggroed against, and this is what we will damage the most out of those things
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "find optimal by distance" );

            int fuzzedLargestAggro = Mathf.RoundToInt( (float)largestAggro * 0.8f );

            //by distance

            foreach ( NPCTargetingData targetingData in targetingList )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.AmountTheyHaveAggroedMe < fuzzedLargestAggro )
                    continue; //ignore anything that is not the fuzzed most-aggroed
                if ( targetingData.DistanceSquared > maxDistanceToUse )
                    continue; //ignore stuff further away for now
                if ( targetingData.IsAVeryLowQualityTarget )
                    continue; //ignore stuff that is a very low quality target for now

                if ( bestInGroup.Target == null || targetingData.TargetPercentageRemain < bestInGroup.TargetPercentageRemain ||
                    Rand.Next( 0, 100 ) < 25 ) //the fuzz factor
                    bestInGroup = targetingData;
            }

            if ( bestInGroup.Target != null )
            {
                targetingList.Clear();
                //ArcenDebugging.LogSingleLine( "Best target group A: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                return bestInGroup.Target; //we found something relatively close, which we are aggroed against, and this is what we will damage the most out of those things
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "widen range" );

            //okay, try in a wider range, since that failed to turn up anything valid
            maxDistanceToUse = minDistance + ((maxDistance - minDistance) * 0.6f);

            foreach ( NPCTargetingData targetingData in targetingList )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.AmountTheyHaveAggroedMe < fuzzedLargestAggro )
                    continue; //ignore anything that is not the fuzzed most-aggroed
                if ( targetingData.DistanceSquared > maxDistanceToUse )
                    continue; //ignore stuff further away for now
                if ( targetingData.IsAVeryLowQualityTarget )
                    continue; //ignore stuff that is a very low quality target for now

                if ( bestInGroup.Target == null || targetingData.TargetPercentageRemain < bestInGroup.TargetPercentageRemain ||
                    Rand.Next( 0, 100 ) < 25 ) //the fuzz factor
                    bestInGroup = targetingData;
            }

            if ( bestInGroup.Target != null )
            {
                targetingList.Clear();
                //ArcenDebugging.LogSingleLine( "Best target group B: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                return bestInGroup.Target; //the best result in a relaxed range
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "ignore range" );

            //okay, ignore range then
            foreach ( NPCTargetingData targetingData in targetingList )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.AmountTheyHaveAggroedMe < fuzzedLargestAggro )
                    continue; //ignore anything that is not the fuzzed most-aggroed
                if ( targetingData.IsAVeryLowQualityTarget )
                    continue; //ignore stuff that is a very low quality target for now

                if ( bestInGroup.Target == null || targetingData.TargetPercentageRemain < bestInGroup.TargetPercentageRemain ||
                    Rand.Next( 0, 100 ) < 25 ) //the fuzz factor
                    bestInGroup = targetingData;
            }

            if ( bestInGroup.Target != null )
            {
                targetingList.Clear();
                //ArcenDebugging.LogSingleLine( "Best target group B: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                return bestInGroup.Target; //the best result in a relaxed range
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "reduce aggro" );

            //okay, fuzz the aggro down even more
            fuzzedLargestAggro = Mathf.RoundToInt( (float)largestAggro * 0.5f );

            foreach ( NPCTargetingData targetingData in targetingList )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.AmountTheyHaveAggroedMe < fuzzedLargestAggro )
                    continue; //ignore anything that is not the fuzzed most-aggroed
                if ( targetingData.IsAVeryLowQualityTarget )
                    continue; //ignore stuff that is a very low quality target for now

                if ( bestInGroup.Target == null || targetingData.TargetPercentageRemain < bestInGroup.TargetPercentageRemain ||
                    Rand.Next( 0, 100 ) < 60 ) //the fuzz factor
                    bestInGroup = targetingData;
            }

            if ( bestInGroup.Target != null )
            {
                targetingList.Clear();
                //ArcenDebugging.LogSingleLine( "Best target group C: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                return bestInGroup.Target; //the best result in any range.  This should never not be hit...
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "check low quality aggro" );

            //this can be reached if all the targets are very low-quality or would be overkilled
            foreach ( NPCTargetingData targetingData in targetingList.GetRandomStartEnumerable( Rand ) )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.AmountTheyHaveAggroedMe < fuzzedLargestAggro )
                    continue; //ignore anything that is not the fuzzed most-aggroed
                if ( targetingData.Target != null )
                {
                    targetingList.Clear();
                    //ArcenDebugging.LogSingleLine( "Failure target group D: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                    //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                    //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                    return targetingData.Target;
                }
            }

            if ( ShouldDoTargetingDump )
                NPCUnit.TargetingDumpLines.Add( "check low quality no aggro" );

            //this can be reached if all the targets are very low-quality or would be overkilled
            foreach ( NPCTargetingData targetingData in targetingList.GetRandomStartEnumerable( Rand ) )
            {
                if ( targetingData.WouldAlreadyBeDeadFromIncoming )
                    continue; //ignore stuff that would be overkill
                if ( targetingData.Target != null )
                {
                    targetingList.Clear();
                    //ArcenDebugging.LogSingleLine( "Failure target group D: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                    //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                    //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                    return targetingData.Target;
                }
            }

            //this can be reached if all the targets would be overkilled
            foreach ( NPCTargetingData targetingData in targetingList.GetRandomStartEnumerable( Rand ) )
            {
                if ( targetingData.Target != null )
                {
                    targetingList.Clear();
                    //ArcenDebugging.LogSingleLine( "Failure target group D: " + NPCUnit.GetDisplayName() + " shoots " + bestInGroup.Target.GetDisplayName() +
                    //    " aggroed: " + bestInGroup.TheyHaveAggroedMe +
                    //    " distance: " + bestInGroup.DistanceSquared + " between range " + minDistance + " and " + maxDistance, Verbosity.DoNotShow );
                    return targetingData.Target;
                }
            }


            //ArcenDebugging.LogSingleLine( "Failure target group E", Verbosity.DoNotShow );
            targetingList.Clear();
            return null; //this should REALLY never be hit, but okay
        }
    }
}

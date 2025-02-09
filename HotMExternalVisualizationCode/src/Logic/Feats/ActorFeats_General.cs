using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using System.Net.Mail;

namespace Arcen.HotM.ExternalVis
{
    public class ActorFeats_General : IActorFeatImplementation
    {
        public void DoWhenDealingDamage( ActorFeat Feat, float FeatAmount, ISimMapActor Attacker, ISimMapActor Target,
            ref int PhysicalAttackPowerSoFar, ref int FearAttackPowerSoFar, ref int ArgumentAttackPowerSoFar,
            ArcenCharacterBufferBase PhysicalBufferOrNull, ArcenCharacterBufferBase FearBufferOrNull, ArcenCharacterBufferBase ArgumentBufferOrNull,
            ArcenCharacterBufferBase SecondaryBufferOrNull, bool isAnyKindOfPrediction, out bool StopTheAttack, MersenneTwister Rand )
        {
            StopTheAttack = false;
            if ( Rand == null )
                Rand = Engine_Universal.PermanentQualityRandom;

            bool isNonPhysicalAttack = false;
            {
                if ( Attacker is ISimMachineActor machineActor )
                {
                    if ( (machineActor.IsInAbilityTypeTargetingMode?.AttacksAreFearBased ?? false) ||
                        (machineActor.IsInAbilityTypeTargetingMode?.AttacksAreArgumentBased ?? false) )
                        isNonPhysicalAttack = true;
                }
            }

            switch ( Feat.ID )
            {
                case "StructureCracker":
                    {
                        if ( isNonPhysicalAttack )
                            return; //do nothing if not a physical attack

                        if ( Target is MachineStructure )
                        {
                            int prior = PhysicalAttackPowerSoFar;
                            PhysicalAttackPowerSoFar = Mathf.RoundToInt( PhysicalAttackPowerSoFar * FeatAmount );
                            int change = PhysicalAttackPowerSoFar - prior;

                            if ( PhysicalBufferOrNull != null && change != 0 )
                                PhysicalBufferOrNull.StartSize80().AddFormat1AndAfterLineItemHeader( "BonusAgainstStructures",
                                    FeatAmount.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite )
                                    .AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole(), ColorTheme.DataBlue ).EndSize().Line();
                        }
                    }
                    break;
                case "Taser":
                    {
                        if ( isNonPhysicalAttack )
                            return; //do nothing if not a physical attack

                        if ( Target is ISimNPCUnit npc && !npc.UnitType.IsMechStyleMovement && !npc.UnitType.IsVehicle && npc.UnitType.IsHuman )
                        {
                            int remainingHP = npc.GetActorDataCurrent( ActorRefs.ActorHP, true );
                            if ( remainingHP <= Attacker.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true ) )
                            {
                                PhysicalAttackPowerSoFar = remainingHP;

                                //if ( BufferOrNull != null )
                                //    BufferOrNull.StartSize80().AddLang( "NonlethalTakedown", ColorTheme.HeaderGold ).AddRaw( "A" ).EndSize().Line();

                                if ( SecondaryBufferOrNull != null )
                                    SecondaryBufferOrNull.StartSize80().AddLang( "NonlethalTakedown", ColorTheme.HeaderGold ).EndSize().Line();

                                if ( !isAnyKindOfPrediction )
                                {
                                    StopTheAttack = true;

                                    ArcenDoubleCharacterBuffer popupBuffer = DamageTextPopups.GetTextBufferAppropriateForThreadOrNull();
                                    if ( popupBuffer != null )
                                    {
                                        popupBuffer.AddLang( "IncapacitatedPopup", IconRefs.NonlethalColor.DefaultColorHexWithHDRHex );

                                        AttackHelper.DoPopupTextAgainstNPCTarget( npc, popupBuffer );
                                    }

                                    if ( Attacker is ISimMachineActor MachineAttacker )
                                    {
                                        AttackHelper.ApplyBasicsOfAttacksToUnit( MachineAttacker, npc, true );

                                        CityStatisticTable.AlterScore( "NonlethalTakedowns", Mathf.Max( 1, npc.CurrentSquadSize ) );
                                        CityStatisticTable.AlterScore( "NonlethalSquadsIncapacitations", 1 );
                                    }

                                    ParticleSoundRefs.TaserAtTarget.DuringGame_PlayAtLocation( npc.GetPositionForCollisions() );

                                    npc.DoOnPostHitWithHostileAction( Attacker, Attacker.GetActorDataCurrent( ActorRefs.ActorEngineeringSkill, true ),
                                        Rand, false );

                                    npc.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                                }

                                return;
                            }

                            //if ( BufferOrNull != null )
                            //    BufferOrNull.StartSize80().AddLang( "BonusAgainstStructures", ColorTheme.HeaderGold ).AddRaw( "C" ).EndSize().Line();

                            int damageReduction = Mathf.RoundToInt( FeatAmount );

                            if ( SecondaryBufferOrNull != null )
                                SecondaryBufferOrNull.StartSize80().AddLangAndAfterLineItemHeader( "ShockApplied", ColorTheme.DataLabelWhite )
                                    .AddNumberPlusOrMinus( damageReduction > 0, damageReduction.ToString(), ColorTheme.DataBlue ).EndSize().Line();

                            if ( !isAnyKindOfPrediction )
                            {
                                npc.AddStatus( StatusRefs.Shocked, damageReduction, 2 );
                            }
                        }
                    }
                    break;
                case "WeaponDisruptor":
                    {
                        if ( isNonPhysicalAttack )
                            return; //do nothing if not a physical attack

                        int disruptionAmount = Mathf.RoundToInt( FeatAmount );

                        if ( SecondaryBufferOrNull != null )
                            SecondaryBufferOrNull.StartSize80().AddLangAndAfterLineItemHeader( "TargetWeaponsDisrupted", ColorTheme.DataLabelWhite )
                                .AddNumberPlusOrMinus( disruptionAmount > 0, disruptionAmount.ToStringIntPercent(), ColorTheme.DataBlue ).EndSize().Line();

                        if ( !isAnyKindOfPrediction )
                        {
                            Target.AddStatus( StatusRefs.WeaponsDisrupted, disruptionAmount, 1 );
                        }
                    }
                    break;
                case "WeakenEnemyHacker":
                    {
                        if ( isNonPhysicalAttack )
                            return; //do nothing if not a physical attack

                        if ( Target is ISimNPCUnit npc && (npc.Stance?.IsConsideredEnemyHacker??false) )
                        {
                            if ( (npc.Stance?.IsConsideredEnemyHackerThatThreatensInfiltrators ?? false) )
                            {
                                PhysicalAttackPowerSoFar = 0;
                                FearAttackPowerSoFar = 0;
                                ArgumentAttackPowerSoFar = 0;
                            }

                            int weakenedAmount = Mathf.RoundToInt( FeatAmount );

                            if ( SecondaryBufferOrNull != null )
                                SecondaryBufferOrNull.StartSize80().AddLangAndAfterLineItemHeader( "TargetHackerWeakened", ColorTheme.DataLabelWhite )
                                    .AddNumberPlusOrMinus( weakenedAmount > 0, weakenedAmount.ToStringIntPercent(), ColorTheme.DataBlue ).EndSize().Line();

                            if ( !isAnyKindOfPrediction )
                            {
                                Target.AddStatus( StatusRefs.HackerWeakened, weakenedAmount, 1 );

                                StopTheAttack = true;
                            }
                        }
                    }
                    break;
                case "SuppressingFire":
                    {
                        //works when using non-physical attacks just as well!

                        if ( !isAnyKindOfPrediction )
                        {
                            int suppressionAmount = Mathf.RoundToInt( FeatAmount );

                            MapTile tile = CityMap.TryGetWorldCellAtCoordinates( Attacker.GetPositionForCollisions() )?.ParentTile;
                            if ( tile != null )
                            {
                                foreach ( ISimMapActor actor in tile.ActorsWithinMaxNPCAttackRange.GetDisplayList() )
                                {
                                    if ( actor == Target || actor == Attacker || actor is MachineStructure )
                                        continue;

                                    if ( Attacker.GetIsValidToAutomaticallyShootAt_Current( Target ) )
                                        Target.AddStatus( StatusRefs.PinnedDownBySuppressingFire, suppressionAmount, 1 );
                                }
                            }

                            Target.AddStatus( StatusRefs.PinnedDownBySuppressingFire, suppressionAmount, 1 );
                        }
                    }
                    break;
                case "HackBreaker":
                    {
                        if ( isNonPhysicalAttack )
                            return; //do nothing if not a physical attack

                        int hackingAmount = Target.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                        if ( hackingAmount > 0 )
                        {
                            float multiplier = 0.1f;
                            if ( hackingAmount > 550 )
                                multiplier = 3f;
                            else if ( hackingAmount > 450 )
                                multiplier = 2.5f;
                            else if ( hackingAmount > 350 )
                                multiplier = 2f;
                            else if ( hackingAmount > 250 )
                                multiplier = 1.5f;
                            else if ( hackingAmount > 150 )
                                multiplier = 1f;

                            float effectiveFeat = FeatAmount * multiplier;
                            if ( effectiveFeat < 1.01f )
                                effectiveFeat = 1.01f;

                            int prior = PhysicalAttackPowerSoFar;
                            PhysicalAttackPowerSoFar = Mathf.RoundToInt( PhysicalAttackPowerSoFar * effectiveFeat );
                            int change = PhysicalAttackPowerSoFar - prior;

                            if ( PhysicalBufferOrNull != null && change != 0 )
                                PhysicalBufferOrNull.StartSize80().AddFormat1AndAfterLineItemHeader( "BonusAgainstThisHacker",
                                    effectiveFeat.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite )
                                    .AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole(), ColorTheme.DataBlue ).EndSize().Line();
                        }
                    }
                    break;
                case "Ambush":
                    {
                        //works when using non-physical attacks just as well!

                        if ( Attacker.OutcastLevel <= 0 )
                        {
                            if ( SecondaryBufferOrNull != null )
                                SecondaryBufferOrNull.StartSize80().AddFormat1( "AmbushBonus",
                                    FeatAmount.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite ).EndSize().Line();

                            {
                                int prior = PhysicalAttackPowerSoFar;
                                PhysicalAttackPowerSoFar = Mathf.RoundToInt( PhysicalAttackPowerSoFar * FeatAmount );
                                int change = PhysicalAttackPowerSoFar - prior;

                                if ( PhysicalBufferOrNull != null && change != 0 )
                                    PhysicalBufferOrNull.StartSize80().AddFormat1AndAfterLineItemHeader( "AmbushBonus",
                                        FeatAmount.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite )
                                        .AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole(), ColorTheme.DataBlue ).EndSize().Line();
                            }
                            {
                                int prior = FearAttackPowerSoFar;
                                FearAttackPowerSoFar = Mathf.RoundToInt( FearAttackPowerSoFar * FeatAmount );
                                int change = FearAttackPowerSoFar - prior;

                                if ( FearBufferOrNull != null && change != 0 )
                                    FearBufferOrNull.StartSize80().AddFormat1AndAfterLineItemHeader( "AmbushBonus",
                                        FeatAmount.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite )
                                        .AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole(), ColorTheme.DataBlue ).EndSize().Line();
                            }
                            {
                                int prior = ArgumentAttackPowerSoFar;
                                ArgumentAttackPowerSoFar = Mathf.RoundToInt( ArgumentAttackPowerSoFar * FeatAmount );
                                int change = ArgumentAttackPowerSoFar - prior;

                                if ( ArgumentBufferOrNull != null && change != 0 )
                                    ArgumentBufferOrNull.StartSize80().AddFormat1AndAfterLineItemHeader( "AmbushBonus",
                                        FeatAmount.ToStringThousandsDecimal_Optional4(), ColorTheme.DataLabelWhite )
                                        .AddNumberPlusOrMinus( change > 0, change.ToStringThousandsWhole(), ColorTheme.DataBlue ).EndSize().Line();
                            }


                        }
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "ActorFeats_General: " + Feat.ID + " was sent to DoWhenDealingDamage, but did not have an entry!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoWhenTakingDamage( ActorFeat Feat, float FeatAmount, ISimMapActor Attacker, ISimMapActor Target,
            ref int PhysicalAttackPowerSoFar, ref int FearAttackPowerSoFar, ref int ArgumentAttackPowerSoFar,
            ArcenCharacterBufferBase PhysicalBufferOrNull, ArcenCharacterBufferBase FearBufferOrNull, ArcenCharacterBufferBase ArgumentBufferOrNull,
            ArcenCharacterBufferBase SecondaryBufferOrNull, bool isAnyKindOfPrediction, out bool StopTheAttack, MersenneTwister Rand )
        {
            StopTheAttack = false;
            if ( Rand == null )
                Rand = Engine_Universal.PermanentQualityRandom;

            switch ( Feat.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActorFeats_General: " + Feat.ID + " was sent to DoWhenTakingDamage, but did not have an entry!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoWhenKilling( ActorFeat Feat, float FeatAmount, ISimMapActor Attacker, ISimMapActor Target,
            int PhysicalAttackPowerUsed, int FearAttackPowerUsed, int ArgumentAttackPowerUsed, bool IsPhysicalDeath,
            MersenneTwister Rand )
        {
            if ( Rand == null )
                Rand = Engine_Universal.PermanentQualityRandom;

            switch ( Feat.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActorFeats_General: " + Feat.ID + " was sent to DoWhenKilling, but did not have an entry!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void DoWhenDying( ActorFeat Feat, float FeatAmount, ISimMapActor Attacker, ISimMapActor Target,
            int PhysicalAttackPowerUsed, int FearAttackPowerUsed, int ArgumentAttackPowerUsed, bool IsPhysicalDeath,
            MersenneTwister Rand )
        {
            if ( Rand == null )
                Rand = Engine_Universal.PermanentQualityRandom;

            switch ( Feat.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActorFeats_General: " + Feat.ID + " was sent to DoWhenDying, but did not have an entry!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

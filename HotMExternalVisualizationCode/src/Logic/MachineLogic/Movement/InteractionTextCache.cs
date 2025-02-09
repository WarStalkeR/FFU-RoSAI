using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{    
    public static class InteractionTextCache
    {
        #region AppendPredictedDamageFromPlayerAttack
        public static void AppendPredictedDamageFromPlayerAttack( bool IsAbbreviatedVersion, ISimMachineActor Actor, ArcenDoubleCharacterBuffer Header,
            ArcenDoubleCharacterBuffer Main, ISimNPCUnit targetUnit,
            AttackAmounts PredictedAttack, bool IsAndroidLeapingFromBeyondAttackRange,
            bool ImagineAttackerWillHaveMoved, Vector3 NewAttackerLocation, ArcenDoubleCharacterBuffer SecondaryBufferForKeyNotes, int APCost, int MentalEnergyCost,
            bool AppendIncomingDamage, bool AppendRestrictedAreaLocation, ISimUnitLocation targetLocation, Vector3 ActionLocationForRestrictedArea, bool IsOutOfRange )
        {
            if ( Actor == null )
                return;
            if ( targetUnit == null )
                return;

            int targetHPRemaining = targetUnit.GetActorDataCurrent( ActorRefs.ActorHP, true );
            int targetMoraleRemaining = targetUnit.GetCanTakeMoraleDamage() ? targetUnit.GetActorDataCurrent( ActorRefs.UnitMorale, true ) : -1;

            bool isNonPhysical = PredictedAttack.Physical == 0;
            int percentagePhysical = MathA.IntPercentage( PredictedAttack.Physical, targetHPRemaining );
            if ( percentagePhysical >= 100 && PredictedAttack.Physical < targetHPRemaining )
                percentagePhysical = 99;

            int percentageMorale = targetMoraleRemaining > 0 ? MathA.IntPercentage( PredictedAttack.Morale, targetMoraleRemaining ) : 0;
            if ( percentageMorale >= 100 && PredictedAttack.Morale < targetMoraleRemaining )
                percentageMorale = 99;

            if ( IsAbbreviatedVersion )
                Main.Clear(); //let's not have the stuff before this

            bool isPhysicalKill = percentagePhysical >= 100 && percentageMorale < 100;
            bool isMoraleIncapacitate = percentageMorale >= 100;

            Main.StartStyleLineHeightA();

            Header.StartUppercase();
            if ( isNonPhysical )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;
                novel.Icon = ActorRefs.UnitMorale.Icon; //replace the other kind of attack with the morale icon, because that's what we are doing only
                Header.AddRaw( percentageMorale.ToStringIntPercent() ).Space1x();
                if ( isMoraleIncapacitate )
                    Header.AddSpriteStyled_NoIndent( IconRefs.KillOrIncapacitate.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                else
                    Header.StartSize50().AddLang( "PredictedMoraleDamageShort" ).EndSize();
            }
            else
            {
                if ( !IsAbbreviatedVersion )
                    Header.AddSpriteStyled_NoIndent( ActorRefs.ActorHP.Icon, AdjustedSpriteStyle.InlineSmaller095, !isPhysicalKill ? string.Empty : ColorTheme.RedOrange2 );
                Header.AddRaw( percentagePhysical.ToStringIntPercent() ).Space1x();
                if ( isPhysicalKill )
                    Header.AddSpriteStyled_NoIndent( IconRefs.KillOrIncapacitate.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                else
                    Header.StartSize50().AddLang( "PredictedDamageShort" ).EndSize();

                if ( targetMoraleRemaining > 0 && PredictedAttack.Morale > 0 )
                {
                    if ( IsAbbreviatedVersion )
                    {
                        Main.AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095, ColorTheme.DataLabelWhite )
                            .StartSize80().AddLangAndAfterLineItemHeader( "PredictedMoraleDamage", ColorTheme.DataLabelWhite )
                            .AddRaw( percentageMorale.ToStringIntPercent(), ColorTheme.DataBlue );
                        if ( isMoraleIncapacitate )
                            Header.Space1x().AddSpriteStyled_NoIndent( IconRefs.KillOrIncapacitate.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                        Main.EndSize().Line();
                    }
                    else
                    {
                        Header.Space3x().AddSpriteStyled_NoIndent( ActorRefs.UnitMorale.Icon, AdjustedSpriteStyle.InlineSmaller095, !isMoraleIncapacitate ? string.Empty : ColorTheme.RedOrange2 );
                        Header.AddRaw( percentageMorale.ToStringIntPercent() ).Space1x();
                        if ( isMoraleIncapacitate )
                            Header.AddSpriteStyled_NoIndent( IconRefs.KillOrIncapacitate.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RedOrange2 );
                        else
                            Header.StartSize50().AddLang( "PredictedMoraleDamageShort" ).EndSize();
                    }
                }
            }
            Header.EndUppercase();
            if ( IsAbbreviatedVersion )
                Header.StartSize70();
            Header.Space3x();

            int remainingAP = (Actor.CurrentActionPoints - APCost);
            int remainingME = (int)(ResourceRefs.MentalEnergy.Current - MentalEnergyCost);

            if ( APCost > 0 )
                Header.AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.HeaderGoldMoreRich )
                    .AddRaw( APCost.ToString(), remainingAP >= 0 ? string.Empty : ColorTheme.RedOrange2 );

            if ( MentalEnergyCost > 0 )
            {
                if ( APCost > 0 )
                    Header.Space3x();

                Header.AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.MentalEnergy.IconColorHex )
                    .AddRaw( MentalEnergyCost.ToString(), remainingME >= 0 ? string.Empty : ColorTheme.RedOrange2 );
            }

            MobileActorTypeDuringGameData dgd = Actor.GetTypeDuringGameData();
            if ( dgd != null && dgd.EffectiveCostsPerAttack.Count > 0 )
            {
                bool isBeyondFirst = APCost > 0 || MentalEnergyCost > 0;

                foreach ( KeyValuePair<ResourceType, int> kv in dgd.EffectiveCostsPerAttack.GetDisplayDict() )
                {
                    if ( isBeyondFirst )
                        Header.Space3x();
                    else
                        isBeyondFirst = true;

                    Header.AddSpriteStyled_NoIndent( kv.Key.Icon, AdjustedSpriteStyle.InlineLarger1_2, kv.Key.IconColorHex );
                    Header.AddRaw( (-kv.Value).ToStringThousandsWhole(), kv.Value > kv.Key.Current ? ColorTheme.RedOrange2 : string.Empty );
                }
            }

            Header.Space1x().StartSize20().AddNeverTranslated( ".", true, ColorTheme.SpacerColor ).EndSize();


            if ( IsAbbreviatedVersion )
            {
                if ( IsOutOfRange )
                    Main.StartSize80().AddLang( "ConsumableTargeting_OutOfRange", ColorTheme.RedOrange2 ).EndSize().Line();
                if ( remainingAP < 0 )
                    Main.StartSize80().AddLang( "InsufficientActionPoints", ColorTheme.RedOrange2 ).EndSize().Line();
                if ( remainingME < 0 )
                    Main.StartSize80().AddLang( "InsufficientMentalEnergy", ColorTheme.RedOrange2 ).EndSize().Line();
            }
            else
            {
                if ( !isNonPhysical )
                {
                    Main.StartBold().AddFormat1AndAfterLineItemHeader( "XDamageToTarget", PredictedAttack.Physical, ColorTheme.DataLabelWhite ).EndBold();
                    Main.AddFormat1( "PredictedDamageLong", percentagePhysical.ToStringIntPercent(), ColorTheme.DataBlue ).Line();
                }

                if ( targetMoraleRemaining > 0 && PredictedAttack.Morale > 0 )
                {
                    Main.StartBold().AddFormat1AndAfterLineItemHeader( "XMoraleDamageToTarget", PredictedAttack.Morale, ColorTheme.DataLabelWhite ).EndBold();
                    Main.AddFormat1( "PredictedMoraleDamageLong", percentageMorale.ToStringIntPercent(), ColorTheme.DataBlue ).Line();
                }
            }

            if ( !SecondaryBufferForKeyNotes.GetIsEmpty() )
            {
                Main.AddRaw( SecondaryBufferForKeyNotes.GetStringAndResetForNextUpdate() ).LineIfLastWrittenWasNotLine();
            }
            else
            {
                if ( !Main.GetIsEmpty() )
                    Main.Line();
            }

            if ( IsAbbreviatedVersion )
            {
                if ( AppendIncomingDamage && (CombatTextHelper.NextTurn_DamageFromEnemies.Physical > 0 || CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies > 0) )
                    CombatTextHelper.AppendLastPredictedDamageLong( Actor, Main, Main, false, ImagineAttackerWillHaveMoved, true );
            }
            else
            {
                Main.AddLang( "PredictedDamageNotes", ColorTheme.NarrativeColor ).Line();
                int lengthBefore = Main.GetLength();
                AttackHelper.HandleAttackPredictionDetailsForTooltip( Actor, targetUnit,
                    IsAndroidLeapingFromBeyondAttackRange, true, true, false, CalculationType.PredictionDuringPlayerTurn, 0, Main, ImagineAttackerWillHaveMoved, NewAttackerLocation );
                if ( lengthBefore != Main.GetLength() )
                    Main.Line();

                if ( AppendIncomingDamage && (CombatTextHelper.NextTurn_DamageFromEnemies.Physical > 0 || CombatTextHelper.AttackOfOpportunity_MaxDamageFromEnemies > 0) )
                    CombatTextHelper.AppendLastPredictedDamageLong( Actor, Main, Main, false, ImagineAttackerWillHaveMoved, true );
                if ( AppendRestrictedAreaLocation )
                    CombatTextHelper.AppendRestrictedAreaLong( Actor, Main, Main, false, targetLocation, ActionLocationForRestrictedArea, true );
            }

            Main.EndLineHeight();
        }
        #endregion
    }
}

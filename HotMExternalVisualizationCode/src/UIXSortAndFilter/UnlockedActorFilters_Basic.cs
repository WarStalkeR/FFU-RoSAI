using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class UnlockedActorFilters_Basic : UIX_UnlockedActorDataSortAndFilterImplementation
    {
        public bool ShouldDrawInTabularFashion( UIX_UnlockedActorDataSortAndFilter FilterType )
        {
            switch ( FilterType.ID )
            {
                case "Normal":
                    return false;
            }
            return true;
        }

        public void WriteTabularLineOrHeader( UnlockedActorData Actor, ArcenCharacterBufferBase Buffer, UIX_UnlockedActorDataSortAndFilter FilterType, bool WriteHeader )
        {
            switch ( FilterType.ID )
            {
                case "BestScavengers":
                case "BestScavengersPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitScavengingSkill.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorEngineeringSkill.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitScavengingSkill, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorEngineeringSkill, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                    }
                    break;
                case "BestEngineers":
                case "BestEngineersPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorEngineeringSkill.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.UnitScavengingSkill.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorEngineeringSkill, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitScavengingSkill, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                    }
                    break;
                case "HighestCognition":
                case "HighestCognitionPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitCognition.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorEngineeringSkill.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitCognition, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorEngineeringSkill, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                    }
                    break;
                case "HighestHackingSkill":
                case "HighestHackingSkillPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.UnitCognition.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorEngineeringSkill.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitCognition, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorEngineeringSkill, FilterType.ID );
                    }
                    break;
                case "HighestDroneQuality":
                case "HighestDroneQualityPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.VehicleDroneQuality.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorArmorPiercing.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.VehicleDroneQuality, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPiercing, FilterType.ID );
                    }
                    break;
                case "HighestMovementRange":
                case "HighestMovementRangePossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.UnitCognition.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.UnitStrength.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitCognition, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitStrength, FilterType.ID );
                    }
                    break;
                case "HighestPower":
                case "HighestPowerPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorArmorPlating.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorArmorPiercing.ShortName.Text );
                        Buffer.Position800();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position900();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position1000();
                        Buffer.AddRaw( ActorRefs.ActorEngineeringSkill.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPlating, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPiercing, FilterType.ID );
                        Buffer.Position800();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position900();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position1000();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorEngineeringSkill, FilterType.ID );
                    }
                    break;
                case "HighestHealth":
                case "HighestHealthPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorArmorPlating.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorArmorPiercing.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPlating, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPiercing, FilterType.ID );
                    }
                    break;
                case "HighestAgility":
                case "HighestAgilityPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorAgility.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.UnitStrength.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorAgility, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitStrength, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                    }
                    break;
                case "HighestStrength":
                case "HighestStrengthPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitStrength.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorAgility.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.UnitHackingSkill.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitStrength, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorAgility, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitHackingSkill, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                    }
                    break;
                case "HighestIntimidation":
                case "HighestIntimidationPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.UnitDeterrence.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitDeterrence, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                    }
                    break;
                case "HighestDeterrence":
                case "HighestDeterrencePossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitDeterrence.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitDeterrence, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                    }
                    break;
                case "HighestProtection":
                case "HighestProtectionPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.UnitProtection.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitProtection, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                    }
                    break;
                case "HighestArgumentAttackPower":
                case "HighestArgumentAttackPowerPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorArgumentAttackPower.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorFearAttackPower.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArgumentAttackPower, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorFearAttackPower, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                    }
                    break;
                case "HighestFearAttackPower":
                case "HighestFearAttackPowerPossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.ActorFearAttackPower.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorArgumentAttackPower.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorFearAttackPower, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArgumentAttackPower, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                    }
                    break;
                //case "HighestSupervision":
                //case "HighestSupervisionPossible":
                //    if ( WriteHeader )
                //    {
                //        Buffer.StartSize80();
                //        Buffer.AddLang( "UnitType_Header" );
                //        Buffer.StartSize60();
                //        Buffer.Position200();
                //        Buffer.AddRaw( ActorRefs.UnitSupervision.ShortName.Text );
                //        Buffer.Position300();
                //        Buffer.AddRaw( ActorRefs.UnitDeterrence.ShortName.Text );
                //        Buffer.Position400();
                //        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                //        Buffer.Position500();
                //        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                //        Buffer.Position600();
                //        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                //        Buffer.Position700();
                //        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                //    }
                //    else
                //    {
                //        Buffer.StartSize80();
                //        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                //        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                //        Buffer.Position200();
                //        WriteStatPair( Actor, Buffer, ActorRefs.UnitSupervision, FilterType.ID );
                //        Buffer.Position300();
                //        WriteStatPair( Actor, Buffer, ActorRefs.UnitDeterrence, FilterType.ID );
                //        Buffer.Position400();
                //        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                //        Buffer.Position500();
                //        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                //        Buffer.Position600();
                //        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                //        Buffer.Position700();
                //        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                //    }
                //    break;
                case "HighestAttackRange":
                case "HighestAttackRangePossible":
                    if ( WriteHeader )
                    {
                        Buffer.StartSize80();
                        Buffer.AddLang( "UnitType_Header" );
                        Buffer.StartSize60();
                        Buffer.Position200();
                        Buffer.AddRaw( ActorRefs.AttackRange.ShortName.Text );
                        Buffer.Position300();
                        Buffer.AddRaw( ActorRefs.ActorHP.ShortName.Text );
                        Buffer.Position400();
                        Buffer.AddRaw( ActorRefs.ActorPower.ShortName.Text );
                        Buffer.Position500();
                        Buffer.AddRaw( ActorRefs.ActorMoveRange.ShortName.Text );
                        Buffer.Position600();
                        Buffer.AddRaw( ActorRefs.ActorArmorPlating.ShortName.Text );
                        Buffer.Position700();
                        Buffer.AddRaw( ActorRefs.ActorArmorPiercing.ShortName.Text );
                        Buffer.Position800();
                        Buffer.AddRaw( ActorRefs.UnitIntimidation.ShortName.Text );
                        Buffer.Position900();
                        Buffer.AddRaw( ActorRefs.ActorArgumentAttackPower.ShortName.Text );
                        Buffer.Position1000();
                        Buffer.AddRaw( ActorRefs.ActorFearAttackPower.ShortName.Text );
                    }
                    else
                    {
                        Buffer.StartSize80();
                        Buffer.AddSpriteStyled_NoIndent( Actor.DuringGameData.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_8 ).Space1x();
                        Buffer.AddRaw( Actor.DuringGameData.GetDisplayName(), string.Empty );
                        Buffer.Position200();
                        WriteStatPair( Actor, Buffer, ActorRefs.AttackRange, FilterType.ID );
                        Buffer.Position300();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorHP, FilterType.ID );
                        Buffer.Position400();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorPower, FilterType.ID );
                        Buffer.Position500();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorMoveRange, FilterType.ID );
                        Buffer.Position600();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPlating, FilterType.ID );
                        Buffer.Position700();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArmorPiercing, FilterType.ID );
                        Buffer.Position800();
                        WriteStatPair( Actor, Buffer, ActorRefs.UnitIntimidation, FilterType.ID );
                        Buffer.Position900();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorArgumentAttackPower, FilterType.ID );
                        Buffer.Position1000();
                        WriteStatPair( Actor, Buffer, ActorRefs.ActorFearAttackPower, FilterType.ID );
                    }
                    break;
                default:
                    throw new Exception( "UnlockedActorFilters_Basic ShouldBeShown_UIXSortAndFilter: Not set up for '" + FilterType.ID + "'!" );
            }
        }

        #region WriteStatPair
        private void WriteStatPair( UnlockedActorData Actor, ArcenCharacterBufferBase Buffer, ActorDataType DataType, string FilterTypeID )
        {
            bool usePossible = FilterTypeID.EndsWith( "Possible" );

            int current = Actor.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[DataType].LeftItem;
            int bestPossible = Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[DataType].LeftItem;

            int larger = Mathf.Max( bestPossible, current );
            int toShow = usePossible ? bestPossible : current;
            if ( larger <= 0 )
                return;

            Buffer.StartNoBr().AddSpriteStyled_NoIndent( DataType.Icon, AdjustedSpriteStyle.InlineLarger1_4, DataType.TooltipIconColorHex ).Space1x();
            Buffer.AddRaw( toShow.ToStringWholeBasic() );

            if ( usePossible && bestPossible > current )
            {
                int increase = bestPossible - current;
                Buffer.Space1x().StartSize70().AddFormat1( "ParentheticalPositiveNumber", increase.ToStringThousandsWhole(), ColorTheme.HeaderLighterBlue ).EndSize();
            }
            else if ( !usePossible && bestPossible > current )
            {
                int decrease = current - bestPossible;
                Buffer.Space1x().StartSize70().AddFormat1( "Parenthetical", decrease.ToStringThousandsWhole(), ColorTheme.CannotAfford ).EndSize();
            }

            Buffer.EndNoBr();
        }
        #endregion

        public bool ShouldBeShown_UIXSortAndFilter( UnlockedActorData Actor, UIX_UnlockedActorDataSortAndFilter FilterType )
        {
            switch ( FilterType.ID )
            {
                case "Normal":
                    return true; //show all UnlockedActorDatas
                case "BestScavengers":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitScavengingSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "BestScavengersPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitScavengingSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "BestEngineers":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorEngineeringSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "BestEngineersPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorEngineeringSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestPower":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestPowerPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestCognition":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitCognition].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestCognitionPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitCognition].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestHackingSkill":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitHackingSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestHackingSkillPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitHackingSkill].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestDroneQuality":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.VehicleDroneQuality].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestDroneQualityPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.VehicleDroneQuality].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestMovementRange":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorMoveRange].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestMovementRangePossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorMoveRange].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestHealth":
                case "HighestHealthPossible":
                    return true;
                case "HighestAgility":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorAgility].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestAgilityPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorAgility].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestStrength":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitStrength].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestStrengthPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitStrength].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestIntimidation":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitIntimidation].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestIntimidationPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitIntimidation].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestDeterrence":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitDeterrence].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestDeterrencePossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitDeterrence].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestProtection":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitProtection].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestProtectionPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitProtection].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestArgumentAttackPower":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorArgumentAttackPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestArgumentAttackPowerPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorArgumentAttackPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestFearAttackPower":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorFearAttackPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestFearAttackPowerPossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorFearAttackPower].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                //case "HighestSupervision":
                //    {
                //        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitSupervision].LeftItem <= 0 )
                //            return false;
                //        return true;
                //    }
                //case "HighestSupervisionPossible":
                //    {
                //        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitSupervision].LeftItem <= 0 )
                //            return false;
                //        return true;
                //    }
                case "HighestAttackRange":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.AttackRange].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                case "HighestAttackRangePossible":
                    {
                        if ( Actor.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.AttackRange].LeftItem <= 0 )
                            return false;
                        return true;
                    }
                default:
                    throw new Exception( "UnlockedActorFilters_Basic ShouldBeShown_UIXSortAndFilter: Not set up for '" + FilterType.ID + "'!" );
            }
        }

        public void SortList_UIXSortAndFilter( List<UnlockedActorData> Actors, UIX_UnlockedActorDataSortAndFilter FilterType )
        {
            switch ( FilterType.ID )
            {
                case "Normal":
                    foreach ( UnlockedActorData data in Actors )
                    {
                        if ( data.DuringGameData.ParentUnitTypeOrNull != null)
                            data.WorkingSortValue = Mathf.RoundToInt( data.DuringGameData.ParentUnitTypeOrNull.SortOrder * 1000 );
                        else if ( data.DuringGameData.ParentVehicleTypeOrNull != null )
                            data.WorkingSortValue = Mathf.RoundToInt( data.DuringGameData.ParentVehicleTypeOrNull.SortOrder * 1000 );
                        else if ( data.DuringGameData.ParentNPCUnitTypeOrNull != null )
                            data.WorkingSortValue = Mathf.RoundToInt( data.DuringGameData.ParentNPCUnitTypeOrNull.SortOrder * 1000 );
                        else
                            data.WorkingSortValue = 10000000;
                    }
                    StandardSort( Actors );
                    break;
                case "BestScavengers":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitScavengingSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "BestScavengersPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitScavengingSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "BestEngineers":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorEngineeringSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "BestEngineersPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorEngineeringSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestPower":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorPower].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestPowerPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorPower].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestCognition":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitCognition].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestCognitionPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitCognition].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestHackingSkill":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitHackingSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestHackingSkillPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitHackingSkill].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestDroneQuality":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.VehicleDroneQuality].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestDroneQualityPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.VehicleDroneQuality].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestMovementRange":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorMoveRange].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestMovementRangePossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorMoveRange].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestHealth":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorHP].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestHealthPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorHP].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestAgility":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorAgility].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestAgilityPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorAgility].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestStrength":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitStrength].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestStrengthPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitStrength].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestIntimidation":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitIntimidation].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestIntimidationPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitIntimidation].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestDeterrence":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitDeterrence].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestDeterrencePossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitDeterrence].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestProtection":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitProtection].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestProtectionPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitProtection].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestArgumentAttackPower":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorArgumentAttackPower].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestArgumentAttackPowerPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorArgumentAttackPower].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestFearAttackPower":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.ActorFearAttackPower].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestFearAttackPowerPossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorFearAttackPower].LeftItem;
                    StandardSort( Actors );
                    break;
                //case "HighestSupervision":
                //    foreach ( UnlockedActorData data in Actors )
                //        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.UnitSupervision].LeftItem;
                //    StandardSort( Actors );
                //    break;
                //case "HighestSupervisionPossible":
                //    foreach ( UnlockedActorData data in Actors )
                //        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.UnitSupervision].LeftItem;
                //    StandardSort( Actors );
                //    break;
                case "HighestAttackRange":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.NonSimActorDataMinMax.GetDisplayDict()[ActorRefs.AttackRange].LeftItem;
                    StandardSort( Actors );
                    break;
                case "HighestAttackRangePossible":
                    foreach ( UnlockedActorData data in Actors )
                        data.WorkingSortValue = data.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.AttackRange].LeftItem;
                    StandardSort( Actors );
                    break;
                default:
                    throw new Exception( "UnlockedActorFilters_Basic SortList_UIXSortAndFilter: Not set up for '" + FilterType.ID + "'!" );
            }
        }

        #region StandardSort
        private void StandardSort( List<UnlockedActorData> DataItems )
        {
            //default sort is alphabetical
            DataItems.Sort( delegate ( UnlockedActorData Left, UnlockedActorData Right )
            {
                int val = Right.WorkingSortValue.CompareTo( Left.WorkingSortValue ); //desc
                if ( val != 0 )
                    return val;
                val = Left.ActorType.CompareTo( Right.ActorType ); //asc
                if ( val != 0 )
                    return val;
                val = Left.Name.CompareTo( Right.Name ); //asc
                if ( val != 0 )
                    return val;
                return Left.DuringGameData.NonSimUniqueID.CompareTo( Right.DuringGameData.NonSimUniqueID );
            } );
        }
        #endregion
    }
}

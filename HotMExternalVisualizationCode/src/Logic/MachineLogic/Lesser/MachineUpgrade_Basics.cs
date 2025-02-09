using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Upgrade_Basics : IUpgradeImplementation
    {
        public bool HandleIntTextLogic( UpgradeInt Upgrade, ArcenCharacterBufferBase Buffer, UpgradeLogic Logic )
        {
            switch (Upgrade.ID)
            {
                case "MaxAndroids":
                    {
                        string color = string.Empty;
                        int current = SimCommon.TotalCapacityUsed_Androids;
                        int max = Upgrade.DuringGameplay_CurrentInt;
                        if ( current >= max )
                            color = ColorTheme.RedOrange2;

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( max.ToStringThousandsWhole(), ColorTheme.Gray );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLang( "Androids", color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                if ( color == string.Empty )
                                    color = ColorTheme.HeaderGold;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLangAndAfterLineItemHeader( "Androids", color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();
                                if ( Upgrade.GetDescription().Length > 0 )
                                    Buffer.AddRaw( Upgrade.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCurrent", ColorTheme.CyanDim )
                                    .AddRaw( current.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCap", ColorTheme.CyanDim )
                                    .AddRaw( max.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
                case "MaxMechs":
                    {
                        string color = string.Empty;
                        int current = SimCommon.TotalCapacityUsed_Mechs;
                        int max = Upgrade.DuringGameplay_CurrentInt;
                        if ( current >= max )
                            color = ColorTheme.RedOrange2;

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( max.ToStringThousandsWhole(), ColorTheme.Gray );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLang( "Mechs", color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                if ( color == string.Empty )
                                    color = ColorTheme.HeaderGold;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLangAndAfterLineItemHeader( "Mechs", color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();
                                if ( Upgrade.GetDescription().Length > 0 )
                                    Buffer.AddRaw( Upgrade.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCurrent", ColorTheme.CyanDim )
                                    .AddRaw( current.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCap", ColorTheme.CyanDim )
                                    .AddRaw( max.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
                case "MaxVehicles":
                    {
                        string color = string.Empty;
                        int current = SimCommon.TotalCapacityUsed_Vehicles;
                        int max = Upgrade.DuringGameplay_CurrentInt;
                        if ( current >= max )
                            color = ColorTheme.RedOrange2;

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( max.ToStringThousandsWhole(), ColorTheme.Gray );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLang( "Vehicles", color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                if ( color == string.Empty )
                                    color = ColorTheme.HeaderGold;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLangAndAfterLineItemHeader( "Vehicles", color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();
                                if ( Upgrade.GetDescription().Length > 0 )
                                    Buffer.AddRaw( Upgrade.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCurrent", ColorTheme.CyanDim )
                                    .AddRaw( current.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCap", ColorTheme.CyanDim )
                                    .AddRaw( max.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
                case "BulkUnitCapacity":
                    {
                        string color = string.Empty;
                        int current = SimCommon.TotalBulkUnitSquadCapacityUsed;
                        int max = Upgrade.DuringGameplay_CurrentInt;
                        if ( current >= max )
                            color = ColorTheme.RedOrange2;

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( max.ToStringThousandsWhole(), ColorTheme.Gray );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddRaw( Upgrade.GetDisplayName(), color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                if ( color == string.Empty )
                                    color = ColorTheme.HeaderGold;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddRawAndAfterLineItemHeader( Upgrade.GetDisplayName(), color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();
                                if ( Upgrade.GetDescription().Length > 0 )
                                    Buffer.AddRaw( Upgrade.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "BulkAndroidSquads", ColorTheme.CyanDim )
                                    .AddRaw( SimCommon.TotalOnlineBulkUnitSquads.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCurrent", ColorTheme.CyanDim )
                                    .AddRaw( current.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCap", ColorTheme.CyanDim )
                                    .AddRaw( max.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
                case "CapturedUnitCapacity":
                    {
                        string color = string.Empty;
                        int current = SimCommon.TotalCapturedUnitSquadCapacityUsed;
                        int max = Upgrade.DuringGameplay_CurrentInt;
                        if ( current >= max )
                            color = ColorTheme.RedOrange2;

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( max.ToStringThousandsWhole(), ColorTheme.Gray );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddRaw( Upgrade.GetDisplayName(), color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                if ( color == string.Empty )
                                    color = ColorTheme.HeaderGold;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddRawAndAfterLineItemHeader( Upgrade.GetDisplayName(), color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();
                                if ( Upgrade.GetDescription().Length > 0 )
                                    Buffer.AddRaw( Upgrade.GetDescription() ).Line();

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "CapturedUnitSquads", ColorTheme.CyanDim )
                                    .AddRaw( SimCommon.TotalOnlineCapturedUnitSquads.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCurrent", ColorTheme.CyanDim )
                                    .AddRaw( current.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatCap", ColorTheme.CyanDim )
                                    .AddRaw( max.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumCap", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
                case "IntelligenceClass":
                    {
                        string color = string.Empty;
                        int current = SimMetagame.IntelligenceClass;
                        color = ColorTheme.HeaderGoldMoreRich;

                        Int64 neuralProcessing = 0;
                        foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                            neuralProcessing += timeline.NeuralProcessing;

                        ISeverity nextCutoff = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );

                        int percentage = MathA.IntPercentage( neuralProcessing, nextCutoff.CutoffInt );

                        switch ( Logic )
                        {
                            case UpgradeLogic.RenderInventoryLine:
                                Buffer.AddRaw( current.ToStringThousandsWhole(), color );
                                Buffer.Position50();
                                Buffer.AddRaw( percentage.ToStringIntPercent(), ColorTheme.HeaderGoldOrangeDark );
                                Buffer.Position100();
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLang( "IntelligenceClass", color );
                                return true;
                            case UpgradeLogic.ReplaceGeneralTooltip:
                                color = ColorTheme.HeaderGoldMoreRich;
                                Buffer.AddSpriteStyled_NoIndent( Upgrade.Icon, AdjustedSpriteStyle.InlineLarger1_2, color ).Space1x()
                                    .AddLangAndAfterLineItemHeader( "IntelligenceClass", color )
                                    .AddRaw( current.ToStringThousandsWhole(), color ).Line();

                                switch ( SimMetagame.IntelligenceClass )
                                {
                                    case 0:
                                    case 1:
                                        Buffer.AddLang( "IntelligenceClass1_Description", ColorTheme.PurpleDim ).Line();
                                        break;
                                    case 2:
                                        Buffer.AddLang( "IntelligenceClass2_Description", ColorTheme.PurpleDim ).Line();
                                        break;
                                    case 3:
                                        Buffer.AddLang( "IntelligenceClass3_Description", ColorTheme.PurpleDim ).Line();
                                        break;
                                    case 4:
                                        Buffer.AddLang( "IntelligenceClass4_Description", ColorTheme.PurpleDim ).Line();
                                        Buffer.AddLang( "Dimensionality_Description", ColorTheme.Gray ).Line();
                                        break;
                                    case 5:
                                    default:
                                        Buffer.AddLang( "IntelligenceClass5_Description", ColorTheme.PurpleDim ).Line();
                                        Buffer.AddLang( "Dimensionality_Description", ColorTheme.Gray ).Line();
                                        break;
                                }

                                Buffer.StartStyleLineHeightA();
                                Buffer.AddLangAndAfterLineItemHeader( "NeuralProcessingForUpgrade", ColorTheme.CyanDim ).AddFormat2( "OutOF",
                                    neuralProcessing.ToStringLargeNumberAbbreviated(), nextCutoff.CutoffInt.ToStringLargeNumberAbbreviated() )
                                    .Space1x().AddFormat1( "ParentheticalProgress", percentage.ToStringIntPercent(), ColorTheme.HeaderGoldOrangeDark ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatOriginalValue", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntOriginal.ToStringThousandsWhole() ).Line();
                                Buffer.AddLangAndAfterLineItemHeader( "StatMaximumValue", ColorTheme.CyanDim )
                                    .AddRaw( Upgrade.IntCap.ToStringThousandsWhole() ).Line();
                                Buffer.EndLineHeight();
                                return true;
                        }
                    }
                    break;
            }
            return false;
        }

        public bool HandleFloatTextLogic( UpgradeFloat MachineFloat, ArcenCharacterBufferBase Buffer, UpgradeLogic Logic )
        {
            switch ( MachineFloat.ID )
            {
                default:
                    break;
            }
            return false;
        }
    }
}

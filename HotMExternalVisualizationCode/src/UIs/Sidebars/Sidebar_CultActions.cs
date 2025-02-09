using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_CultActions : Sidebar_Base, IUISidebarTypeImplementation
    {
        #region FillListOfSidebarItems
        public void WriteAnySidebarItems( ref float currentY )
        {
            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime || !FlagRefs.MachineCultDealWithNCOs.DuringGameplay_IsTripped )
            {
                Window_Sidebar.Instance.Close( WindowCloseReason.ShowingRefused );
                return;
            }

            {
                AddItem( UISidebarBasicItemTable.Instance.GetRowByID( "CultActionsResources" ), ref currentY );
                AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "CultActionsResources" ).Items, ref currentY );

                AddItem( UISidebarBasicItemTable.Instance.GetRowByID( "CultActionsHelpingHand" ), ref currentY );
                AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "CultActionsHelpingHand" ).Items, ref currentY );

                //currentY -= 10f;

                //AddItem( item_ExitHeader );
                //AddRange( UISidebarBasicItemTagTable.Instance.GetRowByID( "Cosmetic" ).Items );
                ////currentY -= 10f;
            }
        }
        #endregion
    }

    public class Sidebar_CultActionsItems : ISidebarBasicItemImplementation
    {
        public void Sidebar_GetOrSetUIData( UISidebarBasicItem Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.GetTextToShowFromVolatile:
                    ExtraData.Buffer.AddRaw( Item.GetDisplayName() );
                    break;
                case UIAction.HandleMouseover:
                    if ( ButtonController != null )
                        break; //don't do mouseovers on the section headers 

                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Item ), Element, SideClamp.LeftOrRight, 
                        TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.ClampToSidebar ) )
                    {
                        novel.TitleUpperLeft.AddRaw( Item.GetDisplayNameForSidebar() );
                        string hotkeyToDisplay = Item.GetHotkeyToDisplayAsToggleForSidebar();
                        if ( hotkeyToDisplay.Length > 0 )
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( hotkeyToDisplay );
                        novel.Main.StartColor( ColorTheme.NarrativeColor );
                        WriteSidebarTooltip( Item, novel.Main );
                        novel.Icon = Item.Icon;
                    }
                    break;
                case UIAction.OnClick:
                    HandleClick( Item, ExtraData.MouseInput );
                    break;
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        ExtraData.Image.SetSpriteIfNeeded_Simple( Item.GetIcon()?.GetSpriteForUI() );

                        bool isHovered = Element.LastHadMouseWithin;

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddRaw( Item.GetDisplayName() );
                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();

                        if ( ExtraData.SubTextsWrapper.Length > 1 )
                        {
                            //details line
                            buffer = ExtraData.SubTextsWrapper[1].Text.StartWritingToBuffer();
                            this.WriteSidebarSecondLine( Item, buffer );
                            ExtraData.SubTextsWrapper[1].Text.FinishWritingToBuffer();
                        }
                    }
                    break;
            }
        }

        public SidebarItemType GetItemType( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                case "CultActionsResources":
                case "CultActionsHelpingHand":
                    return SidebarItemType.TextHeader;
            }
            return SidebarItemType.ImgDoubleLine;
        }

        public MouseHandlingResult HandleClick( UISidebarBasicItem Item, MouseHandlingInput input )
        {
            int loyaltyCost = CalculateLoyaltyCostFor( Item );
            if ( loyaltyCost > 0 )
            {
                bool canBeDone = CalculateCanBeDoneNow( Item ) && loyaltyCost <= ResourceRefs.CultLoyalty.Current;
                if ( !canBeDone )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                ResourceType resourceToGain = CalculateResourceTypeToGain( Item );
                if ( resourceToGain != null )
                {
                    IntRange gainRange = CalculateResourceGainRange( Item );
                    if ( gainRange.Max <= 0 )
                    {
                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                        return MouseHandlingResult.None;
                    }

                    int gainAmount;

                    if ( gainRange.Min < gainRange.Max )
                        gainAmount = Engine_Universal.PermanentQualityRandom.NextInclus( gainRange.Min, gainRange.Max );
                    else
                        gainAmount = gainRange.Max;

                    if ( gainAmount < 1 )
                        gainAmount = 1;

                    ResourceRefs.CultLoyalty.AlterCurrent_Named( -loyaltyCost, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                    resourceToGain.AlterCurrent_Named( gainAmount, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );

                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                        NoteStyle.BothGame, resourceToGain.ID, gainAmount, 0, 0, SimCommon.Turn + 100 );

                    AchievementRefs.GoGetStuff.TripIfNeeded();
                    ParticleSoundRefs.CultGather.DuringGame_Play();
                    return MouseHandlingResult.None;
                }
                else
                {
                    switch ( Item.ID )
                    {
                        //Helping Hand Actions
                        case "SpeedUpConstruction":
                            if ( Engine_HotM.SelectedActor is MachineStructure Structure && Structure.CalculateTurnsBeforeConstructionDone() > 0 )
                            {
                                ResourceRefs.CultLoyalty.AlterCurrent_Named( -loyaltyCost, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );

                                Structure.DoConstructionWork( Engine_Universal.PermanentQualityRandom );

                                AchievementRefs.GiveMeAHand.TripIfNeeded();
                                ParticleSoundRefs.CultConstruct.DuringGame_Play();
                                return MouseHandlingResult.None;
                            }
                            break;
                        case "SpeedUpAllConstruction":                            
                            {
                                bool didAny = false;
                                foreach ( MachineStructure structure in SimCommon.StructuresWithAnyFormOfConstruction.GetDisplayList() )
                                {
                                    if ( structure.CalculateTurnsBeforeConstructionDone() > 0 )
                                    {
                                        structure.DoConstructionWork( Engine_Universal.PermanentQualityRandom );
                                        didAny = true;
                                    }
                                }

                                if ( didAny )
                                {
                                    ResourceRefs.CultLoyalty.AlterCurrent_Named( -loyaltyCost, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );

                                    AchievementRefs.GiveMeAHand.TripIfNeeded();
                                    ParticleSoundRefs.CultConstruct.DuringGame_Play();
                                    return MouseHandlingResult.None;
                                }
                            }
                            break;
                    }
                }
            }

            ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
            return MouseHandlingResult.None;
        }

        public void WriteSidebarTooltip( UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer )
        {
            if ( !Item.GetDescription().IsEmpty() )
                buffer.AddRaw( Item.GetDescription() ).Line();

            int loyaltyCost = CalculateLoyaltyCostFor( Item );
            if ( loyaltyCost > 0 )
            {
                bool canBeDone = CalculateCanBeDoneNow( Item ) && loyaltyCost <= ResourceRefs.CultLoyalty.Current;
                buffer.StartStyleLineHeightA();
                buffer.AddBoldLangAndAfterLineItemHeader( "Cost", ColorTheme.DataLabelWhite ).AddSpriteStyled_NoIndent( ResourceRefs.CultLoyalty.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.CultLoyalty.IconColorHex )
                    .AddRaw( loyaltyCost.ToStringThousandsWhole(), canBeDone ? ColorTheme.DataGood : ColorTheme.DataProblem ).Line();

                ResourceType resourceToGain = CalculateResourceTypeToGain( Item );
                if ( resourceToGain != null )
                {
                    IntRange gainRange = CalculateResourceGainRange( Item );
                    buffer.AddBoldLangAndAfterLineItemHeader( "Gain", ColorTheme.DataLabelWhite ).AddSpriteStyled_NoIndent( resourceToGain.Icon, AdjustedSpriteStyle.InlineLarger1_2, resourceToGain.IconColorHex );
                    if ( gainRange.Min < gainRange.Max )
                        buffer.AddFormat2( "MinMaxRange", gainRange.Min.ToStringThousandsWhole(), gainRange.Max.ToStringThousandsWhole(), ColorTheme.DataGood );
                    else
                        buffer.AddRaw( gainRange.Max.ToStringThousandsWhole(), ColorTheme.DataGood );

                    buffer.Space1x().AddFormat1( "ParentheticalAvailable", resourceToGain.Current.ToStringThousandsWhole(), resourceToGain.Current > 0 ? ColorTheme.CategorySelectedBlue : ColorTheme.Gray );
                }

                buffer.EndLineHeight();
            }
        }

        public void WriteSidebarSecondLine( UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer )
        {
            int loyaltyCost = CalculateLoyaltyCostFor( Item );
            if ( loyaltyCost > 0 )
            {
                bool canBeDone = CalculateCanBeDoneNow( Item ) && loyaltyCost <= ResourceRefs.CultLoyalty.Current;
                buffer.AddSpriteStyled_NoIndent( ResourceRefs.CultLoyalty.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.CultLoyalty.IconColorHex )
                    .AddRaw( loyaltyCost.ToStringThousandsWhole(), canBeDone ? ColorTheme.DataGood : ColorTheme.DataProblem );
            }
        }

        #region CalculateLoyaltyCostFor
        public int CalculateLoyaltyCostFor( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                //Resource Actions
                case "GatherNeodymium":
                    return 725;
                case "GatherBastnasite":
                    return 980;
                case "GatherPrismaticTungsten":
                    return 850;
                case "GatherAlumina":
                    return 410;
                case "GatherScandium":
                    return 360;
                case "GatherPollinatorBeeQueen":
                    return 3000;
                case "GatherLiquidGallium":
                    return 1115;
                case "GatherVegetableSeeds":
                    return 93;
                case "GatherBovineTissue":
                    return 117;
                case "StealthWealth":
                    return 2060;
                //Helping Hand Actions
                case "SpeedUpConstruction":
                    return 370;
                case "SpeedUpAllConstruction":
                    return 4020;
                default:
                    return -1;
            }
        }
        #endregion

        #region CalculateCanBeDoneNow
        public bool CalculateCanBeDoneNow( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                //Helping Hand Actions
                case "SpeedUpConstruction":
                    if ( Engine_HotM.SelectedActor is MachineStructure Structure && Structure.CalculateTurnsBeforeConstructionDone() > 0 )
                        return true;
                    else
                        return false;
                case "SpeedUpAllConstruction":
                    return SimCommon.StructuresWithAnyFormOfConstruction.Count > 0;
                default:
                    return true;
            }
        }
        #endregion

        #region CalculateResourceTypeToGain
        public ResourceType CalculateResourceTypeToGain( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                case "GatherNeodymium":
                    return ResourceRefs.Neodymium;
                case "GatherBastnasite":
                    return ResourceRefs.Bastnasite;
                case "GatherPrismaticTungsten":
                    return ResourceRefs.PrismaticTungsten;
                case "GatherAlumina":
                    return ResourceRefs.Alumina;
                case "GatherScandium":
                    return ResourceRefs.Scandium;
                case "GatherPollinatorBeeQueen":
                    return ResourceRefs.PollinatorBeeQueen;
                case "GatherLiquidGallium":
                    return ResourceRefs.LiquidGallium;
                case "GatherVegetableSeeds":
                    return ResourceRefs.VegetableSeeds;
                case "GatherBovineTissue":
                    return ResourceRefs.BovineTissue;
                case "StealthWealth":
                    return ResourceRefs.Wealth;
                default:
                    return null;
            }
        }
        #endregion

        #region CalculateResourceGainRange
        public IntRange CalculateResourceGainRange( UISidebarBasicItem Item )
        {
            switch ( Item.ID )
            {
                case "GatherNeodymium":
                    return new IntRange( 1200, 1600 );
                case "GatherBastnasite":
                    return new IntRange( 2200, 2400 );
                case "GatherPrismaticTungsten":
                    return new IntRange( 370, 450 );
                case "GatherAlumina":
                    return new IntRange( 3100, 3300 );
                case "GatherScandium":
                    return new IntRange( 2500, 2800 );
                case "GatherPollinatorBeeQueen":
                    return new IntRange( 1, 1 );
                case "GatherLiquidGallium":
                    return new IntRange( 2800, 3100 );
                case "GatherVegetableSeeds":
                    return new IntRange( 1200, 2100 );
                case "GatherBovineTissue":
                    return new IntRange( 600, 900 );
                case "StealthWealth":
                    return new IntRange( 50000, 90000 );
                default:
                    return new IntRange( -1, -1 );
            }
        }
        #endregion
    }
}

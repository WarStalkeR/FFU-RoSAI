using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis.Hacking;

namespace Arcen.HotM.ExternalVis
{
    using h = Window_Hacking;
    using scene = HackingScene;

    public class HackingActionsBasic : IHackingActionImplementation
    {
        private string debugText = string.Empty;

        public HackingActionResult TryHandleHackingAction( HackingAction Action, HackingActionLogic Logic )
        {
            if ( Action == null )
                return HackingActionResult.Indeterminate; //was error

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            scene.LastColorSetForHoveredCell = null;
            scene.SpecialColorForHoveredCell = string.Empty;

            if ( scene.PlayerShards.Count <= 0 || scene.HasLoggedFailure ) //block everything!
                return HackingActionResult.Indeterminate;

            switch ( Action.ID )
            {
                case "Run":
                    #region Run
                    {
                        switch ( Logic )
                        {
                            case HackingActionLogic.ExtraDrawPerFrameWhileActive:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_RunRules( out h.hCell blockedBy );
                                    if ( scene.HoveredCell == null || bestShard == null )
                                    {
                                        scene.MainTargetingIndicator.ClearExistingData();

                                        if ( blockedBy != null )
                                        {
                                            scene.MainBlockedIndicator.Set( blockedBy );

                                            scene.LastColorSetForHoveredCell = scene.HoveredCell;
                                            scene.SpecialColorForHoveredCell = ColorTheme.Hack_InvalidSpot_Hovered;
                                        }
                                        else
                                            scene.MainBlockedIndicator.ClearExistingData();
                                    }
                                    else
                                    {
                                        scene.MainBlockedIndicator.ClearExistingData();

                                        int targetValue = bestShard.CurrentCell.CurrentNumber + (scene.HoveredCell.CurrentNumber / 2);

                                        if ( targetValue > 99 ) //split
                                            scene.MainTargetingIndicator.StartFreshMovement( bestShard, bestShard.CurrentCell, scene.HoveredCell, TargetingIndicatorType.Split );
                                        else
                                            scene.MainTargetingIndicator.StartFreshMovement( bestShard, bestShard.CurrentCell, scene.HoveredCell, TargetingIndicatorType.Main );
                                    }

                                    if ( scene.HoveredCell != null )
                                    {
                                        if ( scene.HoveredCell.CurrentEntity is PlayerShard shard )
                                            HackingHelper.HandleCorruption_Text( shard );
                                        else if ( scene.HoveredCell.CurrentEntity is Daemon daemon )
                                        {
                                            daemon.DaemonType.RenderDaemonTooltip( null, SideClamp.Any, TooltipShadowStyle.None, daemon.IsHidden );
                                            return HackingActionResult.Indeterminate;
                                        }
                                        else if ( bestShard == null )
                                        { }
                                        else
                                        {
                                            int targetValue = bestShard.CurrentCell.CurrentNumber + (scene.HoveredCell.CurrentNumber / 2);
                                            if ( targetValue > 99 )
                                            {
                                                //split
                                                targetValue = (bestShard.CurrentCell.CurrentNumber + scene.HoveredCell.CurrentNumber) / 2;

                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                                {
                                                    novel.CanExpand = CanExpandType.Brief;
                                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                                    {
                                                        novel.TitleUpperLeft.AddLang( "Hacking_Split_Brief", ColorTheme.HeaderGoldMoreRich );
                                                        novel.MainHeader.AddSpriteStyled_NoIndent( IconRefs.HackingSplit.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataBlue )
                                                            .AddLangAndAfterLineItemHeader( "Hacking_Split_Longer", ColorTheme.DataBlue ).AddRaw( targetValue.ToString() );
                                                    }
                                                    else
                                                        novel.TitleUpperLeft.AddLang( "Hacking_Split_Brief", ColorTheme.HeaderGoldMoreRich ).Space2x()
                                                            .AddSpriteStyled_NoIndent( IconRefs.HackingSplit.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                            .AddRaw( targetValue.ToString() );
                                                }

                                                scene.LastColorSetForHoveredCell = scene.HoveredCell;
                                                scene.SpecialColorForHoveredCell = ColorTheme.Hack_Split_Hovered;
                                            }
                                            else
                                            {
                                                int remainder = (bestShard.CurrentCell.CurrentNumber + scene.HoveredCell.CurrentNumber) - targetValue;

                                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                                {
                                                    novel.CanExpand = CanExpandType.Brief;
                                                    if ( InputCaching.ShouldShowDetailedTooltips )
                                                    {
                                                        novel.TitleUpperLeft.AddRaw( Action.ShortName.Text, ColorTheme.HeaderGoldMoreRich );
                                                        novel.MainHeader.AddSpriteStyled_NoIndent( IconRefs.HackingTargetValue.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataBlue )
                                                            .AddLangAndAfterLineItemHeader( "Hacking_SuccessRun_TargetValue", ColorTheme.DataBlue ).AddRaw( targetValue.ToString() ).Line()
                                                            .AddSpriteStyled_NoIndent( IconRefs.HackingRemainderValue.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataBlue )
                                                            .AddLangAndAfterLineItemHeader( "Hacking_SuccessRun_Remainder", ColorTheme.DataBlue ).AddRaw( remainder.ToString() );
                                                    }
                                                    else
                                                        novel.TitleUpperLeft.AddRaw( Action.ShortName.Text, ColorTheme.HeaderGoldMoreRich ).Space2x()
                                                            .AddSpriteStyled_NoIndent( IconRefs.HackingTargetValue.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                            .AddRaw( targetValue.ToString() ).Space2x()
                                                            .AddSpriteStyled_NoIndent( IconRefs.HackingRemainderValue.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                            .AddRaw( remainder.ToString() );
                                                }

                                                scene.LastColorSetForHoveredCell = scene.HoveredCell;
                                                scene.SpecialColorForHoveredCell = ColorTheme.Hack_ValidLocation_Hovered;
                                            }
                                        }
                                    }
                                }
                                break;
                            case HackingActionLogic.LeftClick:
                            case HackingActionLogic.RightClick:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_RunRules( out _ );
                                    if ( scene.HoveredCell != null && bestShard != null )
                                    {
                                        int targetValue = bestShard.CurrentCell.CurrentNumber + (scene.HoveredCell.CurrentNumber / 2);
                                        if ( targetValue > 99 )
                                        {
                                            //split
                                            targetValue = (bestShard.CurrentCell.CurrentNumber + scene.HoveredCell.CurrentNumber) / 2;
                                            bestShard.CurrentCell.SetCurrentNumber( targetValue );
                                            HackingHelper.CreateNewPlayerShardAtCell( scene.HoveredCell, targetValue );

                                            {
                                                ParticleSoundRefs.Hacking_Split.DuringGame_PlayAtUILocation( bestShard.CurrentCell.CalculateScreenPos() );
                                            }
                                            {
                                                ParticleSoundRefs.Hacking_Split.DuringGame_PlayAtUILocation( scene.HoveredCell.CalculateScreenPos(), true );
                                            }


                                            if ( FlagRefs.HackTut_MakeAMove.DuringGameplay_CompleteIfActive() )
                                                FlagRefs.HackTut_DestroyADaemon.DuringGameplay_StartIfNeeded();

                                            CityStatisticTable.AlterScore( "ConsciousnessShardsSplit", 1 );
                                            scene.IsDaemonTurnToMove = true;
                                        }
                                        else
                                        {
                                            int remainder = (bestShard.CurrentCell.CurrentNumber + scene.HoveredCell.CurrentNumber) - targetValue;
                                            bestShard.CurrentCell.SetCurrentNumber( remainder );
                                            scene.HoveredCell.SetCurrentNumber( targetValue );

                                            ParticleSoundRefs.Hacking_Run_Start.DuringGame_PlayAtUILocation( bestShard.CurrentCell.CalculateScreenPos() );

                                            bestShard.MoveTo( scene.HoveredCell, true );
                                            CityStatisticTable.AlterScore( "HackingRuns", 1 );
                                        }
                                    }
                                    else if ( scene.HoveredCell != null && scene.HoveredCell.CurrentEntity is PlayerShard shard )
                                        HackingHelper.HandleCorruption_Click( shard, false );
                                    else
                                    {
                                        if ( scene.HoveredCell?.CurrentEntity is Daemon daemon )
                                        {
                                            switch ( daemon?.DaemonType?.ID )
                                            {
                                                case "LeafNode":
                                                    {
                                                        if ( FlagRefs.HackTut_LeafNode.DuringGameplay_CompleteIfActive() )
                                                            FlagRefs.HackTut_RunRules.DuringGameplay_StartIfNeeded();
                                                    }
                                                    break;
                                            }
                                        }

                                        //invalid click
                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                    }
                                }
                                break;
                        }

                        return HackingActionResult.Indeterminate;
                    }
                    #endregion
                case "Jump":
                    #region Jump
                    {
                        switch ( Logic )
                        {
                            case HackingActionLogic.ExtraDrawPerFrameWhileActive:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_JumpRules( out h.hCell blockedBy );
                                    if ( scene.HoveredCell == null || bestShard == null )
                                    {
                                        scene.MainTargetingIndicator.ClearExistingData();

                                        if ( blockedBy != null )
                                        {
                                            scene.MainBlockedIndicator.Set( blockedBy );

                                            scene.LastColorSetForHoveredCell = scene.HoveredCell;
                                            scene.SpecialColorForHoveredCell = ColorTheme.Hack_InvalidSpot_Hovered;
                                        }
                                        else
                                            scene.MainBlockedIndicator.ClearExistingData();
                                    }
                                    else
                                    {
                                        scene.MainBlockedIndicator.ClearExistingData();
                                        scene.MainTargetingIndicator.StartFreshMovement( bestShard, bestShard.CurrentCell, scene.HoveredCell, TargetingIndicatorType.Main );
                                    }

                                    if ( scene.HoveredCell != null )
                                    {
                                        if ( scene.HoveredCell.CurrentEntity is PlayerShard shard )
                                            HackingHelper.HandleCorruption_Text( shard );
                                        else if ( scene.HoveredCell.CurrentEntity is Daemon daemon )
                                        {
                                            daemon.DaemonType.RenderDaemonTooltip( null, SideClamp.Any, TooltipShadowStyle.None, daemon.IsHidden );
                                            return HackingActionResult.Indeterminate;
                                        }
                                        else if ( bestShard == null )
                                        { 
                                            
                                        }
                                        else
                                        {
                                            int targetValue = scene.HoveredCell.CurrentNumber;
                                            int remainder = bestShard.CurrentCell.CurrentNumber;

                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                            {
                                                novel.CanExpand = CanExpandType.Brief;
                                                if ( InputCaching.ShouldShowDetailedTooltips )
                                                {
                                                    novel.TitleUpperLeft.AddRaw( Action.ShortName.Text, ColorTheme.HeaderGoldMoreRich );
                                                    novel.MainHeader.AddSpriteStyled_NoIndent( IconRefs.HackingTargetValue.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataBlue )
                                                        .AddLangAndAfterLineItemHeader( "Hacking_SuccessRun_TargetValue", ColorTheme.DataBlue ).AddRaw( targetValue.ToString() ).Line()
                                                        .AddSpriteStyled_NoIndent( IconRefs.HackingRemainderValue.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.DataBlue )
                                                        .AddLangAndAfterLineItemHeader( "Hacking_SuccessRun_Remainder", ColorTheme.DataBlue ).AddRaw( remainder.ToString() ).Line();
                                                }
                                                else
                                                    novel.TitleUpperLeft.AddRaw( Action.ShortName.Text, ColorTheme.HeaderGoldMoreRich ).Space2x()
                                                        .AddSpriteStyled_NoIndent( IconRefs.HackingTargetValue.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                        .AddRaw( targetValue.ToString() ).Space2x()
                                                        .AddSpriteStyled_NoIndent( IconRefs.HackingRemainderValue.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                        .AddRaw( remainder.ToString() );
                                            }

                                            scene.LastColorSetForHoveredCell = scene.HoveredCell;
                                            scene.SpecialColorForHoveredCell = ColorTheme.Hack_ValidLocation_Hovered;
                                        }
                                    }
                                }
                                break;
                            case HackingActionLogic.LeftClick:
                            case HackingActionLogic.RightClick:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_JumpRules( out _ );
                                    if ( scene.HoveredCell != null && bestShard != null )
                                    {
                                        //int targetValue = bestShard.CurrentCell.CurrentNumber;
                                        //int remainder = scene.HoveredCell.CurrentNumber;
                                        //bestShard.CurrentCell.SetCurrentNumber( remainder );
                                        //scene.HoveredCell.SetCurrentNumber( targetValue );

                                        ParticleSoundRefs.Hacking_Run_Start.DuringGame_PlayAtUILocation( bestShard.CurrentCell.CalculateScreenPos() );

                                        bestShard.MoveTo( scene.HoveredCell, true );
                                        CityStatisticTable.AlterScore( "HackingJumps", 1 );
                                    }
                                    else if ( scene.HoveredCell != null && scene.HoveredCell.CurrentEntity is PlayerShard shard )
                                        HackingHelper.HandleCorruption_Click( shard, false );
                                    else
                                    {
                                        //invalid click
                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                    }
                                }
                                break;
                        }

                        return HackingActionResult.Indeterminate;
                    }
                #endregion
                case "Firewall":
                    #region Firewall
                    {
                        switch ( Logic )
                        {
                            case HackingActionLogic.ExtraDrawPerFrameWhileActive:
                                {
                                    scene.MainBlockedIndicator.ClearExistingData();
                                    scene.MainTargetingIndicator.ClearExistingData();

                                    if ( scene.HoveredCell != null )
                                    {
                                        if ( scene.HoveredCell.CurrentEntity is PlayerShard shard )
                                        {
                                            HackingHelper.HandleFirewall_Text( shard, Action );
                                        }
                                        else if ( scene.HoveredCell.CurrentEntity is Daemon daemon )
                                        {
                                            daemon.DaemonType.RenderDaemonTooltip( null, SideClamp.Any, TooltipShadowStyle.None, daemon.IsHidden );
                                            return HackingActionResult.Indeterminate;
                                        }
                                    }
                                }
                                break;
                            case HackingActionLogic.LeftClick:
                            case HackingActionLogic.RightClick:
                                {
                                    if ( scene.HoveredCell != null && scene.HoveredCell.CurrentEntity is PlayerShard shard && ResourceRefs.MentalEnergy.Current > 0 )
                                        HackingHelper.HandleCorruption_Click( shard, true );
                                    else
                                    {
                                        //invalid click
                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                    }
                                }
                                break;
                        }

                        return HackingActionResult.Indeterminate;
                    }
                #endregion
                case "InfiltrationSysOpMove":
                    #region InfiltrationSysOpMove
                    {
                        switch ( Logic )
                        {
                            case HackingActionLogic.ExtraDrawPerFrameWhileActive:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_InfiltrationSysOpRules( out h.hCell blockedBy );
                                    h.hCell effectiveCell = scene.HoveredCell;
                                    HackingHelper.ClampCellToDistance( bestShard, ref effectiveCell, 5 );

                                    if ( effectiveCell == null || bestShard == null )
                                    {
                                        scene.MainTargetingIndicator.ClearExistingData();

                                        if ( blockedBy != null )
                                        {
                                            scene.MainBlockedIndicator.Set( blockedBy );

                                            scene.LastColorSetForHoveredCell = effectiveCell;
                                            scene.SpecialColorForHoveredCell = ColorTheme.Hack_InvalidSpot_Hovered;
                                        }
                                        else
                                            scene.MainBlockedIndicator.ClearExistingData();
                                    }
                                    else
                                    {
                                        scene.MainBlockedIndicator.ClearExistingData();
                                        scene.MainTargetingIndicator.StartFreshMovement( bestShard, bestShard.CurrentCell, effectiveCell, TargetingIndicatorType.Main );
                                    }

                                    if ( effectiveCell != null )
                                    {
                                        if ( effectiveCell.CurrentEntity is PlayerShard shard )
                                            HackingHelper.HandleInfiltrationSysOpCorruption_Text( shard );
                                        else if ( effectiveCell.CurrentEntity is Daemon daemon )
                                        {
                                            daemon.DaemonType.RenderDaemonTooltip( null, SideClamp.Any, TooltipShadowStyle.None, daemon.IsHidden );
                                            return HackingActionResult.Indeterminate;
                                        }
                                        else if ( bestShard == null )
                                        {}
                                        else
                                        {
                                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( "HackingItem", "AlwaysAlright" ), null, SideClamp.Any, TooltipNovelWidth.SizeToText ) )
                                            {
                                                novel.CanExpand = CanExpandType.No;
                                                novel.TitleUpperLeft.AddRaw( Action.ShortName.Text, ColorTheme.HeaderGoldMoreRich );
                                            }

                                            scene.LastColorSetForHoveredCell = effectiveCell;
                                            scene.SpecialColorForHoveredCell = ColorTheme.Hack_ValidLocation_Hovered;
                                        }
                                    }
                                }
                                break;
                            case HackingActionLogic.LeftClick:
                            case HackingActionLogic.RightClick:
                                {
                                    PlayerShard bestShard = HackingHelper.GetBestShard_InfiltrationSysOpRules( out _ );
                                    h.hCell effectiveCell = scene.HoveredCell;
                                    HackingHelper.ClampCellToDistance( bestShard, ref effectiveCell, 5 );

                                    if ( effectiveCell != null && bestShard != null )
                                    {
                                        //int targetValue = bestShard.CurrentCell.CurrentNumber;
                                        //int remainder = scene.HoveredCell.CurrentNumber;
                                        //bestShard.CurrentCell.SetCurrentNumber( remainder );
                                        //scene.HoveredCell.SetCurrentNumber( targetValue );

                                        ParticleSoundRefs.Hacking_Run_Start.DuringGame_PlayAtUILocation( bestShard.CurrentCell.CalculateScreenPos() );

                                        bestShard.MoveTo( effectiveCell, true );
                                        CityStatisticTable.AlterScore( "HackingJumps", 1 );
                                    }
                                    else if ( effectiveCell != null && effectiveCell.CurrentEntity is PlayerShard shard )
                                        HackingHelper.HandleInfiltrationSysOpCorruption_Click( shard );
                                    else
                                    {
                                        //invalid click
                                        ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                                    }
                                }
                                break;
                        }

                        return HackingActionResult.Indeterminate;
                    }
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "HackingActionsBasic: Called TryHandleCommandAction for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return HackingActionResult.Indeterminate; //was error
            }
        }
    }
}

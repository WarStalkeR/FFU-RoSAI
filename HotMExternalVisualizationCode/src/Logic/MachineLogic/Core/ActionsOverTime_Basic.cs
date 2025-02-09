using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class ActionsOverTime_Basic : IMachineActorActionOverTimeImplementation
    {
        private static MersenneTwister workingRand = new MersenneTwister( 0 );
        
        public ActionOverTimeResult TryHandleActionOverTime( ActionOverTime Action, ArcenCharacterBufferBase BufferOrNull, ActionOverTimeLogic Logic, 
            MersenneTwister RandOrNull, IArcenUIElementForSizing MustBeAboveOrBelow, SideClamp Clamp, TooltipExtraText ExtraText, TooltipExtraRules ExtraRules )
        {
            if ( Action == null || Action.Actor == null || Action.Type == null )
                return ActionOverTimeResult.Fail;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Action.Type.ID )
            {
                case "MurderAndroidForRegistration":
                    {
                        #region MurderAndroidForRegistration
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {
                                    
                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    //int target = Action.GetIntData( "Target1", false );

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!

                                    Action.Actor.ClearAggroedNPCCohorts();
                                    foreach ( ActorBadge badge in ActorBadgeTable.Instance.Rows )
                                    {
                                        if ( badge.CausesActorToBeConsideredOutcastLevel > 0 || badge.IsNegativeStatusTiedToID )
                                            Action.Actor.AddOrRemoveBadge( badge, false );
                                    }
                                    Action.Actor.OutcastLevel = 0; //this may be wrong, but it will recalculate shortly if so.  And most cases it will be right.

                                    CityStatisticTable.AlterScore( "AndroidMurders", 1 );
                                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "MurderedAndroidForRegistration" ), NoteStyle.BothGame,
                                        Action.Actor.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, Action.Actor.SortID, 0, 0, Action.Actor.GetDisplayName(), string.Empty, string.Empty, 0 );

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "Wiretap":
                    {
                        #region Wiretap
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    //int target = Action.GetIntData( "Target1", false );

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!

                                    int amountStolen = Engine_Universal.PermanentQualityRandom.Next( 7000, 14000 );

                                    ResourceRefs.Wealth.AlterCurrent_Named( amountStolen, "Income_WiretapTheft", ResourceAddRule.IgnoreUntilTurnChange );

                                    CityStatisticTable.AlterScore( "Wiretaps", 1 );
                                    CityStatisticTable.AlterScore( "WiretapAmountStolen", amountStolen );
                                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "Wiretapped" ), NoteStyle.BothGame,
                                        Action.Actor.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, Action.Actor.SortID, amountStolen, 0, Action.Actor.GetDisplayName(), string.Empty, string.Empty, 0 );

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "InvestigateLocation":
                case "InvestigateLocation_Infiltration":
                    {
                        #region InvestigateLocation / InvestigateLocation_Infiltration
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!
                                    InvestigationType investigationType = Action.RelatedInvestigationTypeOrNull;
                                    if ( investigationType != null )
                                    {
                                        foreach ( Investigation invest in SimCommon.AllInvestigationsIncludingInvisible )
                                        {
                                            if ( invest.Type == investigationType )
                                            {
                                                invest.FinishInvestigatingASpecificBuildingOverTime( Action.Actor, Action.TargetBuildingOrNull );
                                                break;
                                            }
                                        }
                                    }

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "HideAndSelfRepair":
                    {
                        #region HideAndSelfRepair
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    //int target = Action.GetIntData( "Target1", false );

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );
                                    int perTurn = Action.GetIntData( "Quantity", false );

                                    Action.Actor.AlterActorDataCurrent( ActorRefs.ActorHP, perTurn, true );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!  Nothing to do now

                                    ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "HideAndSelfRepairComplete" ), NoteStyle.BothGame,
                                        Action.Actor.GetTypeAsRow().ID, string.Empty, string.Empty, string.Empty, Action.Actor.SortID,
                                        Action.Actor.GetActorDataCurrent( ActorRefs.ActorHP, true ), 0, Action.Actor.GetDisplayName(), string.Empty, string.Empty, SimCommon.Turn + 20 );

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "WallripBuilding":
                    {
                        #region WallripBuilding
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    //int target = Action.GetIntData( "Target1", false );

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );

                                    BuildingTypeVariant variant = Action.TargetBuildingOrNull?.GetVariant();
                                    if ( variant == null )
                                        return ActionOverTimeResult.Fail;
                                    if ( variant.SpecialScavengeResource == null )
                                        return ActionOverTimeResult.Fail;

                                    int perTurn = variant.SpecialScavengeResourceAmountRange.GetRandom( RandOrNull );
                                    perTurn /= 3; //divided across three turns

                                    if ( perTurn <= 0 )
                                        return ActionOverTimeResult.Fail;

                                    Action.TargetBuildingOrNull.HasSpecialResourceAlreadyBeenExtracted = true; //make it so that we can't come back again, even after a partial rip

                                    variant.SpecialScavengeResource.AlterCurrent_Named( perTurn, "Income_Wallripper", ResourceAddRule.StoreExcess );

                                    Action.AlterIntData( "Wealth", perTurn );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!  Nothing to do now

                                    BuildingTypeVariant variant = Action.TargetBuildingOrNull?.GetVariant();
                                    if ( variant != null && variant.SpecialScavengeResource != null )
                                        ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "WallripComplete" ), NoteStyle.BothGame,
                                            Action.Actor.GetTypeAsRow().ID, variant.SpecialScavengeResource.ID, string.Empty, string.Empty, Action.Actor.SortID,
                                            Action.GetIntData( "Wealth", true ), 0, Action.TargetBuildingOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "QuietlyLootBuilding":
                    {
                        #region QuietlyLootBuilding
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    //int target = Action.GetIntData( "Target1", false );

                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                    {
                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    int target = Action.GetIntData( "Target1", false );

                                    BuildingTypeVariant variant = Action.TargetBuildingOrNull?.GetVariant();
                                    if ( variant == null )
                                        return ActionOverTimeResult.Fail;
                                    if ( variant.SpecialScavengeResource == null )
                                        return ActionOverTimeResult.Fail;

                                    int perTurn = variant.SpecialScavengeResourceAmountRange.GetRandom( RandOrNull ) / 4; //lower than wallrip
                                    perTurn /= 6; //divided across three turns

                                    if ( perTurn <= 0 )
                                        return ActionOverTimeResult.Fail;

                                    Action.TargetBuildingOrNull.HasSpecialResourceAlreadyBeenExtracted = true; //make it so that we can't come back again, even after a partial rip

                                    variant.SpecialScavengeResource.AlterCurrent_Named( perTurn, "Income_QuietLooting", ResourceAddRule.StoreExcess );

                                    Action.AlterIntData( "Wealth", perTurn );

                                    target -= 1;
                                    Action.SetIntData( "Target1", target );
                                    if ( target <= 0 )
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    //we completed it!  Nothing to do now

                                    BuildingTypeVariant variant = Action.TargetBuildingOrNull?.GetVariant();
                                    if ( variant != null && variant.SpecialScavengeResource != null )
                                        ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "QuietLootingComplete" ), NoteStyle.BothGame,
                                            Action.Actor.GetTypeAsRow().ID, variant.SpecialScavengeResource.ID, string.Empty, string.Empty, Action.Actor.SortID,
                                            Action.GetIntData( "Wealth", true ), 0, Action.TargetBuildingOrNull.GetDisplayName(), string.Empty, string.Empty, 0 );

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                case "AndroidExploreSite":
                    {
                        #region AndroidExploreSite
                        switch ( Logic )
                        {
                            case ActionOverTimeLogic.TryCancel_FromUser:
                            case ActionOverTimeLogic.FailureCancellation:
                                {

                                }
                                break;
                            case ActionOverTimeLogic.WriteTooltip:
                                {
                                    ExplorationSiteType explorationSite = ExplorationSiteTypeTable.Instance.GetRowByIDOrNullIfNotFound( Action.GetStringData( "ID1", true ) );

                                    if ( explorationSite != null )
                                    {
                                        explorationSite.RenderExplorationSiteTooltip( MustBeAboveOrBelow, Clamp, TooltipShadowStyle.Standard, false );

                                        Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                    }
                                    else
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( Action.Type ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, ExtraText, ExtraRules ) )
                                        {
                                            Action.AppendActionOverTimeDataToTooltip( novel, ExtraText );
                                        }
                                    }
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteActionPerTurn:
                                {
                                    ExplorationSiteType explorationSite = ExplorationSiteTypeTable.Instance.GetRowByIDOrNullIfNotFound( Action.GetStringData( "ID1", true ) );
                                    if ( explorationSite == null )
                                        return ActionOverTimeResult.Fail;

                                    if ( explorationSite.ApplyPointsFromActor( Action.Actor ) )
                                    {
                                        if ( explorationSite.DuringGame_PointsApplied < explorationSite.TargetSkillToHit )
                                            return ActionOverTimeResult.Fail;
                                        Action.SetIntData( "Target1", 0 );
                                        return ActionOverTimeResult.QueueForCompletionOnUserSelect;
                                    }

                                    explorationSite.LockToCurrentBuildingForTurns( 4 );

                                    Action.SetIntData( "Target1", explorationSite.CalculateEstimatedRemainingTurnsForActor( Action.Actor ) );
                                }
                                break;
                            case ActionOverTimeLogic.ExecuteFinalSuccessFromUserSelecting:
                                {
                                    ExplorationSiteType explorationSite = ExplorationSiteTypeTable.Instance.GetRowByIDOrNullIfNotFound( Action.GetStringData( "ID1", true ) );
                                    if ( explorationSite != null )
                                    {
                                        ArcenNotes.SendSimpleNoteToGameOnly( 300, NoteInstructionTable.Instance.GetRowByID( "ExploreSiteCompleted" ), NoteStyle.BothGame,
                                            explorationSite.ID, string.Empty, string.Empty, string.Empty, Action.Actor.SortID,
                                            0, 0, Action.Actor.GetDisplayName(), string.Empty, string.Empty, 0 );

                                        explorationSite.MarkAsCompleted();
                                    }

                                    return ActionOverTimeResult.ActualFinalSuccess;
                                }
                            case ActionOverTimeLogic.PredictFloatingText:
                            case ActionOverTimeLogic.PredictSidebarButtonText:
                            case ActionOverTimeLogic.PredictTurnsRemainingText:
                                {
                                    int turnsToTake = Action.GetIntData( "Target1", false );
                                    BasicActionsHelper.RenderTurnsInfo( Action, BufferOrNull, Logic, turnsToTake );
                                }
                                break;
                        }

                        return ActionOverTimeResult.IntermediateSuccess;
                        #endregion
                    }
                default:
                    ArcenDebugging.LogSingleLine( "ActionsOverTime_Basic: Called TryHandleActionOverTime for '" + Action.Type.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return ActionOverTimeResult.Fail;
            }
        }

        public bool GetPreActionLogicGetShouldBeShownForActor( ActionOverTimeType AOT, ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation,
            AbilityType FromAbility )
        {
            if ( Actor == null )
                return false;
            switch ( AOT.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActionsOverTime_Basic: Called GetPreActionLogicGetShouldBeShownForActor for '" + AOT.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return false;
            }
        }

        public void HandlePreActionLogicAsAbilityLineItem( ActionOverTimeType AOT, ISimMachineActor Actor, ISimBuilding BuildingOrNull, Vector3 ActionLocation,
            PreActionOverTimeLineItemLogic Logic, AbilityType FromAbility, ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( AOT.ID )
            {
                default:
                    ArcenDebugging.LogSingleLine( "ActionsOverTime_Basic: Called HandlePreActionLogicAsAbilityLineItem for '" + AOT.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return;
            }
        }
    }
}

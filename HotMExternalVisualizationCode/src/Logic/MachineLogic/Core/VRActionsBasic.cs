using Arcen.Universal;
using System;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class VRActionsBasic : IVRActionImplementation
    {
        public VRActionResult TryHandleVRAction( MachineVRModeAction Action, ArcenCharacterBufferBase BufferOrNull, VRActionLogic Logic )
        {
            if ( Action == null )
                return VRActionResult.Indeterminate; //was error

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            switch ( Action.ID )
            {
                case "SmallArmsResearch":
                case "MartialExpansion":
                case "GunResearch":
                case "MeleeResearch":
                case "AndroidOptimization":
                case "VehicularDevelopment":
                case "ItemDevelopment":
                case "StructuralImprovement":
                case "ProcurementEfficacy":
                case "ComputingOptimization":
                    #region GenerateInspiration
                    {
                        switch ( Logic )
                        {
                            case VRActionLogic.AppendToVRActionTooltip:
                                {
                                    int currentCost = (int)Action.RelatedCostScale1.CostPerLevel[Action.DuringGame_TimesDone];
                                    if ( currentCost > 0 )
                                    {
                                        BufferOrNull.AddBoldLangAndAfterLineItemHeader( "Cost", ColorTheme.DataLabelWhite );
                                        BufferOrNull.AddSpriteStyled_NoIndent( ResourceRefs.ScientificResearch.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.ScientificResearch.IconColorHex )
                                            .AddRaw( currentCost.ToStringThousandsWhole(), ResourceRefs.ScientificResearch.IconColorHex ).Line();
                                    }

                                    int nextCost = (int)Action.RelatedCostScale1.CostPerLevel[Action.DuringGame_TimesDone + 1];
                                    if ( nextCost > 0 )
                                    {
                                        BufferOrNull.AddBoldLangAndAfterLineItemHeader( "NextCost", ColorTheme.DataLabelWhite );
                                        BufferOrNull.AddSpriteStyled_NoIndent( ResourceRefs.ScientificResearch.Icon, AdjustedSpriteStyle.InlineLarger1_2, ResourceRefs.ScientificResearch.IconColorHex )
                                            .AddRaw( nextCost.ToStringThousandsWhole(), ResourceRefs.ScientificResearch.IconColorHex ).Line();
                                    }
                                }
                                break;
                            case VRActionLogic.GetCanAfford:
                                {
                                    int currentCost = (int)Action.RelatedCostScale1.CostPerLevel[Action.DuringGame_TimesDone];
                                    if ( currentCost > 0 && currentCost <= ResourceRefs.ScientificResearch.Current )
                                        return VRActionResult.Success;
                                }
                                break;
                            case VRActionLogic.TryPayCosts:
                                {
                                    int currentCost = (int)Action.RelatedCostScale1.CostPerLevel[Action.DuringGame_TimesDone];
                                    if ( currentCost > 0 && currentCost <= ResourceRefs.ScientificResearch.Current )
                                    {
                                        ResourceRefs.ScientificResearch.AlterCurrent_Named( -currentCost, "Expense_VRActions", ResourceAddRule.IgnoreUntilTurnChange );
                                        Action.DuringGame_TimesDone++;
                                        return VRActionResult.Success;
                                    }
                                }
                                break;
                            case VRActionLogic.MenuClick:
                                {
                                    UnlockRewardGroup group = UnlockRewardGroupTable.Instance.GetRowByID( Action.ID + "1" );
                                    if ( group == null )
                                        break;
                                    UnlockInspirationType inspire = UnlockInspirationTypeTable.Instance.GetRowByID( Action.ID );
                                    if ( inspire == null )
                                        break;

                                    UnopenedMysteryUnlock mysteryUnlock;
                                    mysteryUnlock.RewardGroup = group;
                                    mysteryUnlock.InspirationType = inspire;
                                    mysteryUnlock.RandSeed = Engine_Universal.PermanentQualityRandom.Next();
                                    MapEffectCoordinator.AddUnopenedMysteryUnlock( mysteryUnlock );
                                    return VRActionResult.Success;
                                }
                        }
                        return VRActionResult.Indeterminate;
                    }
                #endregion
                default:
                    ArcenDebugging.LogSingleLine( "VRActionsBasic: Called TryHandleVRAction for '" + Action.ID + "', which does not support it!", Verbosity.ShowAsError );
                    return VRActionResult.Indeterminate; //was error
            }
        }
    }
}

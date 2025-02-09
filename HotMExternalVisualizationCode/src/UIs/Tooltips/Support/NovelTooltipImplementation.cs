using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class NovelTooltipImplementation : IUITooltipTypeImplementation
    {
        public void PopulateFromBuffer( UITooltipType Type, ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, IArcenUIElementForSizing OptionalAnchor, SideClamp Clamp, 
            string ExtraData1, string ExtraData2, string ExtraData3, long ExtraInt1, long ExtraInt2, long ExtraInt3,
            TooltipWidth Width, TooltipExtraText ExtraText, TooltipExtraRules ExtraRules, MinHeight TooltipMinHeight, TooltipArrowSide ArrowSide )
        {
            ArcenDebugging.LogWithStack( "NovelTooltipImplementation.PopulateFromBuffer is never valid, but was called for '" + Type.ID + "'!", Verbosity.ShowAsError );
        }

        public void ClearImmediately( UITooltipType Type )
        {
            switch ( Type.ID )
            {
                case "AtMouseBasicNovel":
                    Window_BasicAtMouseNovelTooltip.ClearMyself();
                    break;
                case "AtMouseSmallerNovel":
                    Window_SmallerAtMouseNovelTooltip.ClearMyself();
                    break;
                case "LowerLeftBasicNovel":
                    Window_BasicLowerLeftNovelTooltip.ClearMyself();
                    break;
                case "LowerLeftSmallerNovel":
                    Window_SmallerLowerLeftNovelTooltip.ClearMyself();
                    break;
                case "AtMouseUnitStyleNovel":
                    Window_UnitStyleAtMouseNovelTooltip.ClearMyself();
                    break;
                case "LowerLeftUnitStyleNovel":
                    Window_UnitStyleLowerLeftNovelTooltip.ClearMyself();
                    break;
                case "AtMouseCondensedAttack":
                    Window_CondensedAttackTooltip.ClearMyself();
                    break;
                default:
                    ArcenDebugging.LogWithStack( "NovelTooltipImplementation.ClearImmediately does not have an entry for '" + Type.ID + "'!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void PopulateNovelTooltip( UITooltipType Type, IUITooltipDataSource Data, TooltipID ToolID, IArcenUIElementForSizing OptionalAnchor, SideClamp Clamp, 
            string ExtraData1, string ExtraData2, string ExtraData3, long ExtraInt1, long ExtraInt2, long ExtraInt3,
            TooltipExtraText ExtraText, TooltipExtraRules ExtraRules, TooltipArrowSide ArrowSide )
        {
            switch ( Type.ID )
            {
                case "AtMouseBasicNovel":
                    Window_BasicAtMouseNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "AtMouseSmallerNovel":
                    Window_SmallerAtMouseNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "LowerLeftBasicNovel":
                    Window_BasicLowerLeftNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "LowerLeftSmallerNovel":
                    Window_SmallerLowerLeftNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "AtMouseUnitStyleNovel":
                    Window_UnitStyleAtMouseNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "LowerLeftUnitStyleNovel":
                    Window_UnitStyleLowerLeftNovelTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                case "AtMouseCondensedAttack":
                    Window_CondensedAttackTooltip.SetDataSourceInner_Scalable( OptionalAnchor, Data, ExtraText, Clamp, ExtraRules, ToolID );//, ArrowSide );
                    break;
                default:
                    ArcenDebugging.LogWithStack( "NovelTooltipImplementation.PopulateFromBuffer does not have an entry for '" + Type.ID + "'!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

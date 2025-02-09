using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class TooltipFromBufferImplementation : IUITooltipTypeImplementation
    {
        public void PopulateFromBuffer( UITooltipType Type, ArcenDoubleCharacterBuffer Buffer, TooltipID ToolID, IArcenUIElementForSizing OptionalAnchor, SideClamp Clamp, 
            string ExtraData1, string ExtraData2, string ExtraData3, long ExtraInt1, long ExtraInt2, long ExtraInt3,
            TooltipWidth Width, TooltipExtraText ExtraText, TooltipExtraRules ExtraRules, MinHeight TooltipMinHeight, TooltipArrowSide ArrowSide )
        {
            switch ( Type.ID )
            {
                case "ArrowIndicatorDark":
                    if ( OptionalAnchor == null )
                    {
                        ArcenDebugging.LogWithStack( "OptionalAnchor is not optional for '" + Type.ID + "'!", Verbosity.ShowAsError );
                        break;
                    }

                    Window_ArrowIndicatorDark.bPanel.Instance.SetText_OnlyFromImplementation( OptionalAnchor, Buffer, TooltipWidth.Narrow, ToolID, ArrowSide );
                    break;
                case "LevelEditor":
                    Window_LevelEditorTooltip.bPanel.Instance.SetText( OptionalAnchor, Buffer, Width, ToolID, Clamp, TooltipMinHeight, ExtraRules );
                    break;
                default:
                    ArcenDebugging.LogWithStack( "TooltipFromBufferImplementation.PopulateFromBuffer does not have an entry for '" + Type.ID + "'!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void ClearImmediately( UITooltipType Type )
        {
            switch ( Type.ID )
            {
                case "ArrowIndicatorDark":
                    Window_ArrowIndicatorDark.ClearMyself();
                    break;
                case "LevelEditor":
                    Window_LevelEditorTooltip.ClearMyself();
                    break;
                case "AbilityBarNote":
                    Window_AbilityBarNote.ClearAndInvalidateTimeLastSet();
                    break;
                default:
                    ArcenDebugging.LogWithStack( "TooltipFromBufferImplementation.ClearImmediately does not have an entry for '" + Type.ID + "'!", Verbosity.ShowAsError );
                    break;
            }
        }

        public void PopulateNovelTooltip( UITooltipType Type, IUITooltipDataSource Data, TooltipID ToolID, IArcenUIElementForSizing OptionalAnchor, 
            SideClamp Clamp, string ExtraData1, string ExtraData2, string ExtraData3, long ExtraInt1, long ExtraInt2, long ExtraInt3, 
            TooltipExtraText ExtraText, TooltipExtraRules ExtraRules, TooltipArrowSide ArrowSide )
        {
            ArcenDebugging.LogWithStack( "TooltipFromBufferImplementation.PopulateNovelTooltip is never valid, but was called for '" + Type.ID + "'!", Verbosity.ShowAsError );
        }
    }
}

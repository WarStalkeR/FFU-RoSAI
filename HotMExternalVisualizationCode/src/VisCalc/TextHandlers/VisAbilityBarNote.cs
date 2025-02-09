using Arcen.HotM.Visualization;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public static class VisAbilityBarNote
    {
        private static readonly ArcenDoubleCharacterBuffer mainBuffer = new ArcenDoubleCharacterBuffer( "VisAbilityBarNote-mainBuffer" );

        public static void DoPerFrame()
        {
            RenderBottomRightNoteText();

            if ( mainBuffer.GetIsEmpty() )
            {
                Window_AbilityBarNote.ClearAndInvalidateTimeLastSet();
                return;
            }

            Window_AbilityBarNote.bPanel.Instance.SetText( mainBuffer, TooltipID.Create( "VisAbilityBarNote", "AlwaysSame" ) );
        }

        private static void RenderBottomRightNoteText()
        {
            if ( !Window_AbilityFooterBar.Instance.GetShouldDrawThisFrame() )
                return;

            if ( !(Engine_HotM.SelectedActor is ISimMachineActor machineActor) )
                return; //if nothing selected, or selected not-a-machine-actor, don't show

            if ( machineActor.IsFullDead )
                return; //if the machine actor is dead, don't show the ability bar

            if ( machineActor.IsInConsumableTargetingMode != null )
            {
                ResourceConsumable consumable = machineActor.IsInConsumableTargetingMode;
                mainBuffer.StartLink( false, string.Empty, "Consumable", string.Empty )
                    .AddSpriteStyled_NoIndent( consumable.Icon, AdjustedSpriteStyle.InlineLarger1_2, consumable.IconColorHex )
                    .AddRaw( consumable.GetDisplayName() )
                    .EndLink( false, false );

                if ( consumable.MustHoldShiftToStayInMode )
                    mainBuffer.Line().StartSize70().AddFormat1( "ConsumableHoldToKeepDoingAction_TooltipFooter", InputCaching.GetGetHumanReadableKeyComboForShouldKeepDoingAction(),
                            ColorTheme.TooltipFootnote_DimSteelCyan ).EndSize().Line();
                return;
            }

            if ( machineActor.IsInAbilityTypeTargetingMode == null )
                return;
        }

        public static void HandleHyperlinkHover( IArcenUIElementForSizing HoveredElement, string[] LinkData )
        {
            switch (LinkData[0] )
            {
                case "Consumable":
                    {
                        if ( !(Engine_HotM.SelectedActor is ISimMachineActor machineActor) )
                            return; //if nothing selected, or selected not-a-machine-actor, don't show
                        ResourceConsumable consumable = machineActor.IsInConsumableTargetingMode;
                        if ( consumable != null )
                        {
                            consumable.RenderConsumableTooltip( HoveredElement, SideClamp.AboveOrBelow, TooltipShadowStyle.None, machineActor, TooltipInstruction.ForDeploying, TooltipExtraText.None, TooltipExtraRules.None );
                        }
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "Unknown link : '" + LinkData[0] + "' in VisAbilityBarNote", Verbosity.ShowAsError );
                    break;
            }
        }

        public static MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] LinkData )
        {
            return MouseHandlingResult.None;
        }
    }
}

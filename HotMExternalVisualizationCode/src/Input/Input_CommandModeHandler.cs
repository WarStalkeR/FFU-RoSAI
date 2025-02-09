using Arcen.Universal;
using Arcen.HotM.Core;
using System;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using UnityEngine;
using System.IO;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{
    public class Input_CommandModeHandler : BaseInputHandler
    {
        public override void HandleInner( Int32 Int1, InputActionTypeData InputActionType )
        {
            if ( ArcenInput.IsInputBlockedForAmountOfTime )
                return;
            if ( Engine_HotM.SelectedMachineActionMode == null ||
                Engine_HotM.SelectedMachineActionMode?.ID != "CommandMode" )
                return;

			string InputActionID = InputActionType.ID;

			if (Engine_Universal.IsAnyTextboxFocused && InputActionID != "Cancel")
				return;

            if ( InputWindowCutthrough.HandleKey( InputActionID ) )
                return;

            if ( MachineCommandModeActionTable.ActionsByKeybind.TryGetValue( InputActionType, out MachineCommandModeAction action ) )
            {
                CommandModeHandler.currentCategory = action.CommandModeCategory;
                action.DoMenuClick();
            }
            else
            {
                ArcenDebugging.LogSingleLine( "Input_CommandModeHandler was asked to handle the input command '" + InputActionID +
                    "', but that was not actually found to any MachineCommandModeAction!", Verbosity.ShowAsError );
            }
        }
    }
}
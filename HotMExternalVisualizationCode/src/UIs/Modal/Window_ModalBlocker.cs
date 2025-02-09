using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ModalBlocker : ToggleableWindowController, IInputActionHandler
    {
        public static Window_ModalBlocker Instance;
        public Window_ModalBlocker()
        {
            Instance = this;
        }

        public static bool ShouldShowModalBlocker = false;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            return ShouldShowModalBlocker;
        }

        public void Handle( int Int1, InputActionTypeData InputActionType )
        {
            //nothing to do here!
        }
    }
}

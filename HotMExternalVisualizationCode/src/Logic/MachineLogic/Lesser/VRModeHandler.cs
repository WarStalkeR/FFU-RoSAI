using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class VRModeHandler : ILowerModeImplementation
    {
        public static MachineVRModeCategory currentCategory = null;

        public static MachineVRModeAction LastHoveredAction = null;
        public static float HoverExpireTime = 0;

        public void FlagAllRelatedResources( LowerModeData Mode )
        {
            if ( (Mode?.ID ?? string.Empty) != "ZodiacPodScene" )
                return;

            if ( currentCategory != null )
            {
                //currentCategory?.NonSim_TargetingDeployableNPCType?.FlagAllRelatedResources(); todo
            }

            ResourceRefs.ScientificResearch.IsRelatedToCurrentActivities.Construction = true;

            //LastHoveredAction nothing to do?

            //LastHoveredDeployableNPC?.FlagAllRelatedResources(); todo
        }

        #region HandleCancelButton
        public void HandleCancelButton()
        {
            if ( currentCategory != null )
                currentCategory = null;
            else
                Engine_HotM.CurrentLowerMode = null;
        }
        #endregion

        #region HandleAction
        private bool HandleAction( MachineVRModeAction action, bool IsHoverOnly )
        {
            return false;
        }
        #endregion

        public float currentDeployedAndroidRotationY = 0f;

        public void HandlePassedInput( int Int1, InputActionTypeData InputActionType )
        {
            if ( currentCategory != null )
            {
                //nothing do do right now I guess
            }
        }

        public void TriggerSlotNumber( int SlotNumber )
        {
            int categoryIndex = 1;
            foreach ( MachineVRModeCategory category in SimCommon.ActiveVRModeCategories.GetDisplayList() )
            {
                if ( categoryIndex == SlotNumber )
                {
                    if ( !category.GetShouldBeEnabled() )
                        return;

                    if ( currentCategory != null )
                    {
                        //this was super confusing when it remembered!
                        //Engine_HotM.CurrentVRModeActionTargeting = null;
                        //category.NonSim_TargetingDeployableNPCType = null;
                    }

                    if ( currentCategory == category )
                        currentCategory = null;
                    else
                        currentCategory = category;
                    //category.NonSim_TargetingDeployableNPCType = null;

                    return;
                }
                categoryIndex++;
            }
        }
    }
}

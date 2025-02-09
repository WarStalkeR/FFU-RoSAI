using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public abstract class bBadgeIconBase : ButtonAbstractBaseWithImage
    {
        public GetOrSetUIData UIDataController;

        public void Assign( GetOrSetUIData UIDataController )
        {
            this.UIDataController = UIDataController;
            if ( this.button != null )
                this.button.OptionalGetterAndSetter = UIDataController;
        }

        public override void Clear()
        {
            this.UIDataController = null;
            if ( this.button != null )
                this.button.OptionalGetterAndSetter = null;
        }

        public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
        {
            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                return MouseHandlingResult.PlayClickDeniedSound;
            ISimMapMobileActor mobileActor = Engine_HotM.SelectedActor as ISimMapMobileActor;
            if ( mobileActor == null || (mobileActor is ISimNPCUnit npcUnit && !npcUnit.GetIsPlayerControlled()) )
                return MouseHandlingResult.None;
            Window_ActorCustomizeLoadout.Instance.ToggleOpen();
            return MouseHandlingResult.None;
        }

        public override bool GetShouldBeHidden()
        {
            return this.UIDataController == null;
        }
    }

    public interface IBadgeIconFactory
    {
        bBadgeIconBase GetNewBadgeIconBase();
    }
}

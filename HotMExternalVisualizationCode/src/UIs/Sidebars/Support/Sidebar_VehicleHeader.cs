using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_VehicleHeader : ISidebarItemFromOtherHandler<ISimMachineVehicle>
    {
        public readonly static Sidebar_VehicleHeader Instance = new Sidebar_VehicleHeader();

        public SidebarItemType GetItemType( SidebarItemFromOther<ISimMachineVehicle> Item )
        {
            return SidebarItemType.TextHeader;
        }

        public void Sidebar_GetOrSetUIData( SidebarItemFromOther<ISimMachineVehicle> Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.GetTextToShowFromVolatile:
                    ExtraData.Buffer.AddRaw( Item.Item.GetDisplayName() );
                    break;
                case UIAction.HandleMouseover:
                    Item.Item.RenderTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.WideDark, false, ActorTooltipExtraData.SelectFocus, TooltipExtraRules.None );
                    break;
                case UIAction.OnClick:
                    if ( Item.Item != null )
                    {
                        if ( ExtraData.MouseInput.LeftButtonClicked )
                        {
                            Engine_HotM.SelectedMachineActionMode = null;
                            if ( Engine_HotM.SelectedActor == Item.Item ) //focus on the actor
                                VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( Item.Item.GetDrawLocation(), false );
                            else
                            {
                                Engine_HotM.SetSelectedActor( Item.Item, false, true, true );
                            }
                        }
                        else if ( ExtraData.MouseInput.RightButtonClicked )
                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( Item.Item.GetDrawLocation(), false );
                    }
                    break;
            }
        }
    }
}

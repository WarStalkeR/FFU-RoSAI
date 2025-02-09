using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_NetworkHandler : ISidebarItemFromOtherHandler<MachineNetwork>
    {
        public readonly static Sidebar_NetworkHandler Instance = new Sidebar_NetworkHandler();

        public SidebarItemType GetItemType( SidebarItemFromOther<MachineNetwork> Item )
        {
            return SidebarItemType.ImgSingleLine;
        }

        public void Sidebar_GetOrSetUIData( SidebarItemFromOther<MachineNetwork> Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController, 
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.HandleMouseover:
                    Item.Item.RenderNetworkTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipExtraRules.ClampToSidebar );
                    break;
                case UIAction.OnClick:
                    MachineStructure tower = Item.Item?.Tower;
                    if ( tower != null )
                        Engine_HotM.SetSelectedActor( tower, false, false, false );
                    break;
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        ExtraData.Image.SetSpriteIfNeeded_Simple( Item.Item.GetIcon().GetSpriteForUI() );

                        bool isHovered = Element.LastHadMouseWithin;

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddRaw( Item.Item.NetworkName );
                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();
                    }
                    break;
            }
        }
    }
}

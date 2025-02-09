using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_SubnetHandler : ISidebarItemFromOtherHandler<MachineSubnet>
    {
        public readonly static Sidebar_SubnetHandler Instance = new Sidebar_SubnetHandler();

        public SidebarItemType GetItemType( SidebarItemFromOther<MachineSubnet> Item )
        {
            return SidebarItemType.ImgSingleLine;
        }

        public void Sidebar_GetOrSetUIData( SidebarItemFromOther<MachineSubnet> Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController, 
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.HandleMouseover:
                    Item?.Item?.RenderSubnetTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, TooltipExtraRules.ClampToSidebar );
                    break;
                case UIAction.OnClick:
                    MachineStructure firstNode = Item.Item?.SubnetNodes?.GetDisplayList()?.FirstOrDefault;
                    if ( firstNode != null )
                        Engine_HotM.SetSelectedActor( firstNode, false, false, false );
                    break;
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        ExtraData.Image.SetSpriteIfNeeded_Simple( null );

                        bool isHovered = Element.LastHadMouseWithin;

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddFormat1( "SubnetName", Item.Item.SubnetIndex );

                        if ( Item.Item.JobClassInputEfficiencyMultipliers.Count > 0 )
                            buffer.Space1x().AddSpriteStyled_NoIndent( ActorRefs.InputEfficiencyMultiplier.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.InputEfficiencyMultiplier.TooltipIconColorHex );

                        if ( Item.Item.JobClassProductionMultipliers.Count > 0 )
                            buffer.Space1x().AddSpriteStyled_NoIndent( ActorRefs.ProductionMultiplier.Icon, AdjustedSpriteStyle.InlineLarger1_2, ActorRefs.ProductionMultiplier.TooltipIconColorHex );

                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();
                    }
                    break;
            }
        }
    }
}

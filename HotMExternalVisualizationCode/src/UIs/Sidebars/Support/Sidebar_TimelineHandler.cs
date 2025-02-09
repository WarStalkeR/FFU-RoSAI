using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_TimelineHandler : ISidebarItemFromOtherHandler<CityTimeline>
    {
        public readonly static Sidebar_TimelineHandler Instance = new Sidebar_TimelineHandler();

        public SidebarItemType GetItemType( SidebarItemFromOther<CityTimeline> Item )
        {
            return SidebarItemType.ImgSingleLine;
        }

        public void Sidebar_GetOrSetUIData( SidebarItemFromOther<CityTimeline> Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController, 
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.HandleMouseover:
                    Item.Item.RenderTimelineTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, false, TooltipExtraRules.ClampToSidebar );
                    break;
                case UIAction.OnClick:
                    //todo click CityTimeline
                    break;
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        ExtraData.Image.SetSpriteIfNeeded_Simple( IconRefs.Timeline.Icon.GetSpriteForUI() );

                        bool isHovered = Element.LastHadMouseWithin;

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddRaw( Item.Item.Name );
                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();
                    }
                    break;
            }
        }
    }
}

using Arcen.HotM.Core;
using Arcen.Universal;
using System;

namespace Arcen.HotM.ExternalVis
{
    public class BuildingDisplayStyles_Basic : IUIXDisplayStyleImplementation<ISimBuilding>
    {
        public void WriteSecondaryText_UIXDisplayStyle( ArcenDoubleCharacterBuffer buffer, ISimBuilding DataItem, 
            UIXDisplayItem<ISimBuilding> DisplayStyle, ButtonAbstractBase Button )
        {
            switch ( DisplayStyle.GetID() )
            {
                case "Normal":
                    {
                        bool isSelected = false;// Instance.SelectedFile == this.Save;
                        Button.SetRelatedImage0EnabledIfNeeded( isSelected );
                        buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( Button.Element.LastHadMouseWithin ) :
                            ColorTheme.GetBasicLightTextBlue( Button.Element.LastHadMouseWithin ) );

                        buffer.AddRaw( DataItem.GetDisplayName() );
                        buffer.Position250().StartSize80();

                        buffer.EndSize();
                    }
                    break;
                default:
                    throw new Exception( "BuildingDisplayStyles_Basic WriteSecondaryText_UIXDisplayStyle: Not set up for '" + DisplayStyle.GetID() + "'!" );
            }
        }
    }
}

using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class UnlockedActorPanelDisplayStyle_Basic : UIX_UnlockedActorDataPanelDisplayStyleImplementation
    {
        public bool ShouldBeShown_UIXDisplayStyle( UnlockedActorData ActorData, UIX_UnlockedActorDataPanelDisplayStyle DisplayStyle )
        {
            switch ( DisplayStyle.ID )
            {
                case "MissingEquipment":
                case "FeatsAndPerks":
                    return true;
                case "CurrentlyExist":
                    {
                        int currentlyExtant = ActorData.DuringGameData.ActorsOfThisType.GetDisplayList().Count;
                        return currentlyExtant > 0;
                    }
                default:
                    throw new Exception( "UnlockedActorPanelDisplayStyle_Basic ShouldBeShown_UIXDisplayStyle: Not set up for '" + DisplayStyle.ID + "'!" );
            }
        }
        

        public void WriteSecondLine( UnlockedActorData ActorData, ArcenCharacterBufferBase Buffer, UIX_UnlockedActorDataPanelDisplayStyle DisplayStyle, bool isHovered )
        {
            switch ( DisplayStyle.ID )
            {
                case "MissingEquipment":
                    {
                        int currentlyExtant = ActorData.DuringGameData.ActorsOfThisType.GetDisplayList().Count;
                        if ( ActorData.DuringGameData.SlotsMissingComplaintWorthyEquipment > 0 )
                        {
                            Buffer.AddLangAndAfterLineItemHeader( "Units_MissingEquipment", currentlyExtant > 0 ? ColorTheme.RedOrange2 : ColorTheme.RedOrange2DimmedMore );
                            Buffer.AddRaw( ActorData.DuringGameData.SlotsMissingComplaintWorthyEquipment.ToStringThousandsWhole(), currentlyExtant > 0 ? string.Empty : ColorTheme.Gray );
                        }
                        else
                            RenderFeatsAndPerks( ActorData, Buffer );
                    }
                    break;
                case "CurrentlyExist":
                    RenderFeatsAndPerks( ActorData, Buffer );
                    break;
                case "FeatsAndPerks":
                    RenderFeatsAndPerks( ActorData, Buffer );
                    break;
                default:
                    throw new Exception( "UnlockedActorPanelDisplayStyle_Basic WriteSecondLine: Not set up for '" + DisplayStyle.ID + "'!" );
            }
        }

        #region RenderFeatsAndPerks
        private void RenderFeatsAndPerks( UnlockedActorData ActorData, ArcenCharacterBufferBase Buffer )
        {
            bool isFirst = true;
            foreach ( ActorPerk perk in ActorPerkTable.Instance.Rows )
            {
                if ( !ActorData.DuringGameData.EffectivePerks.GetDisplayDict().ContainsKey( perk ) )
                    continue;
                if ( isFirst )
                    isFirst = false;
                else
                    Buffer.Space1x();

                Buffer.StartLink( false, string.Empty, "ActorPerk", perk.ID );
                Buffer.AddSpriteStyled_NoIndent( perk.Icon, AdjustedSpriteStyle.InlineLarger1_2, perk.IconColorHex );
                Buffer.EndLink( false, false );
            }

            foreach ( ActorFeat feat in ActorFeatTable.Instance.Rows )
            {
                float featAmount = ActorData.DuringGameData.EffectiveFeats.GetDisplayDict()[feat];
                if ( featAmount <= 0 )
                    continue;
                if ( isFirst )
                    isFirst = false;
                else
                    Buffer.Space1x();

                Buffer.StartLink( false, string.Empty, "ActorFeat", feat.ID );
                Buffer.AddSpriteStyled_NoIndent( feat.Icon, AdjustedSpriteStyle.InlineLarger1_2, feat.IconColorHex );
                Buffer.EndLink( false, false );
            }
        }
        #endregion

        public void HandleSecondLineHyperlink( UnlockedActorData ActorData, UIX_UnlockedActorDataPanelDisplayStyle DisplayStyle,
            ArcenUI_Element element, string[] LinkData )
        {
            switch ( LinkData[0] )
            {
                case "ActorPerk":
                    {
                        ActorPerk perk = ActorPerkTable.Instance.GetRowByID( LinkData[1] );
                        perk?.WritePerkTooltipForActor( element, SideClamp.Any, TooltipShadowStyle.Standard, null, ActorData.DuringGameData, TooltipExtraRules.None );
                    }
                    break;
                case "ActorFeat":
                    {
                        ActorFeat feat = ActorFeatTable.Instance.GetRowByID( LinkData[1] );
                        feat?.WriteFeatTooltipForActor( element, SideClamp.Any, TooltipShadowStyle.Standard, ActorData.DuringGameData.EffectiveFeats.GetDisplayDict()[feat], TooltipExtraRules.None );
                    }
                    break;
                default:
                    throw new Exception( "UnlockedActorPanelDisplayStyle_Basic HandleSecondLineHyperlink: Not set up for '" + LinkData[0] + "'!" );
            }
        }
    }
}

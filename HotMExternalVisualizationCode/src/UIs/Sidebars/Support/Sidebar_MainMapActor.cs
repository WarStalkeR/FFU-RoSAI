using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Sidebar_MainMapActor : ISidebarItemFromOtherHandler<ISimMapActor>
    {
        public readonly static Sidebar_MainMapActor Instance = new Sidebar_MainMapActor();

        public SidebarItemType GetItemType( SidebarItemFromOther<ISimMapActor> Item )
        {
            return SidebarItemType.Unit;
        }

        public void Sidebar_GetOrSetUIData( SidebarItemFromOther<ISimMapActor> Item, ImageButtonAbstractBase ImageController, ButtonAbstractBase ButtonController,
            ArcenUI_Element Element, UIAction Action, ref UIActionData ExtraData )
        {
            switch ( Action )
            {
                case UIAction.HandleMouseover:
                    Item.Item.RenderTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.TightDark, false, ActorTooltipExtraData.SelectFocus, TooltipExtraRules.None );
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
                case UIAction.UpdateImageContentFromVolatile:
                    {
                        bool isHovered = Element.LastHadMouseWithin;
                        bool isSelected = Engine_HotM.SelectedActor == Item.Item;

                        ISimMapMobileActor mobileActor = Item.Item as ISimMapMobileActor;

                        ExtraData.Image.SetSpriteIfNeeded_Simple( mobileActor?.GetTooltipIcon()?.GetSpriteForUI() );
                        ImageController.SetRelatedImage1SpriteIfNeeded( Item.Item?.GetShapeIcon()?.GetSpriteForUI() );
                        ImageController.SetRelatedImage1ColorFromHexIfNeeded( isSelected ? string.Empty : ColorTheme.GetBasicLightTextBlue( isHovered ) );

                        //plate overlay
                        ImageController.SetRelatedImage2SpriteIfNeeded( ImageController.Element.RelatedSprites[isSelected ? 3 : 2] );
                        //bg panel
                        ImageController.SetRelatedImage3SpriteIfNeeded( ImageController.Element.RelatedSprites[isSelected ? 1 : 0] );

                        ArcenDoubleCharacterBuffer buffer;

                        //main line
                        buffer = ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer();
                        buffer.AddRaw( Item.Item.GetDisplayName(), isSelected ? string.Empty : ColorTheme.GetBasicLightTextBlue( isHovered ) );
                        ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();

                        //details line
                        buffer = ExtraData.SubTextsWrapper[1].Text.StartWritingToBuffer();
                        if ( Item.Item is ISimMachineActor machineActor )
                        {
                            if ( machineActor.CurrentActionOverTime != null )
                                machineActor.CurrentActionOverTime.Type.Implementation.TryHandleActionOverTime( machineActor.CurrentActionOverTime, buffer,
                                    ActionOverTimeLogic.PredictTurnsRemainingText, null,
                                    null, SideClamp.Any, TooltipExtraText.None, TooltipExtraRules.None );
                            else
                                buffer.AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, 
                                    AdjustedSpriteStyle.InlineLarger1_2, machineActor.CurrentActionPoints > 0 ? ColorTheme.HeaderGold : ColorTheme.Gray )
                                    .AddRaw( machineActor.CurrentActionPoints.ToString(), machineActor.CurrentActionPoints > 0 ? ColorTheme.HeaderGold : ColorTheme.Gray );
                        }
                        ExtraData.SubTextsWrapper[1].Text.FinishWritingToBuffer();

                        int healthPercentage = 100;
                        MapActorData health = Item.Item.GetActorDataData( ActorRefs.ActorHP, true );
                        if ( health != null && health.LostFromMax > 0 )
                        {
                            healthPercentage = Mathf.FloorToInt( ((float)health.Current / (float)health.Maximum) * 100f );
                            if ( healthPercentage == 0 && health.Current > 0 )
                                healthPercentage = 1;
                        }

                        ImageController.SetRelatedImage0FillPercentageIfNeeded( healthPercentage );
                        ISeverity severityColor = ScaleRefs.UnitListHealth.GetSeverityFromScale( healthPercentage );
                        if ( severityColor?.VisColor != null )
                            ImageController.SetRelatedImage0ColorFromHexIfNeeded( severityColor.VisColor.ColorHex );
                    }
                    break;
            }
        }

        public void WriteSidebarSecondLine( SidebarItemFromOther<ISimMapActor> Item, ArcenDoubleCharacterBuffer buffer )
        {
            
        }

		public void WriteSidebarTopLine( SidebarItemFromOther<ISimMapActor> Item, ArcenDoubleCharacterBuffer buffer )
        {
            if ( Item.Item is ISimMachineActor machineActor )
            {
                if ( Engine_HotM.SelectedActor == Item.Item )
                {
                    if ( machineActor.GetIsBlockedFromActingForTurnCompletePurposes() )
                        buffer.StartColor( ColorTheme.HeaderGoldDim );
                    else
                        buffer.StartColor( ColorTheme.SubcategoryGold );
                }
                else
                {
                    if ( machineActor.GetIsBlockedFromActingForTurnCompletePurposes() )
                        buffer.StartColor( ColorTheme.CyanDim );
                }
            }

            buffer.AddRaw( Item.Item.GetDisplayName() );
        }
    }
}

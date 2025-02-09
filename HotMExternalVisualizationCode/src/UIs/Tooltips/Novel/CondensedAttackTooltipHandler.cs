using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class CondensedAttackTooltipHandler
    {
        public readonly TooltipNovelBundle bun = new TooltipNovelBundle();
        public CustomUIAbstractBase CPI;
        public NovelTooltipBufferBase bufferSource;

        public bool GetShouldDrawThisFrame()
        {
            if ( CPI == null || bun.CurrentData == null )
                return false;
            return bun.GetShouldDrawThisFrame_Normal();
        }

        #region tSoleTitleLeft
        public Vector2 tSoleTitleLeft_TextSize;
        public ArcenUIWrapperedTMProText tSoleTitleLeft_Text;

        public void tSoleTitleLeft_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( tSoleTitleLeft_Text == null )
                tSoleTitleLeft_Text = Text;
        }
        public void tSoleTitleLeft_CallPerFrame()
        {
            if ( tSoleTitleLeft_Text == null )
                return; //not ready yet

            tSoleTitleLeft_Text.DirectlySetNextText( bufferSource.TitleUpperLeft.GetStringAndDoNotReset() );
            tSoleTitleLeft_TextSize = tSoleTitleLeft_Text.SetTextNowIfNeededAndGetSize( false, 0f );
        }
        #endregion

        #region tMainTextHeader
        public float tMainText_lastWidth = -1f;
        private float tMainText_lastHeight = -1f;
        private bool tMainText_lastCouldExpand = true;

        private bool tMainText_wasRed = false;
        private bool tMainText_changedSizeLastFrame = true;

        public void tMainTextHeader_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
                return;

            IA5Sprite sprite = bufferSource.Icon;
            string spriteColor = bufferSource.IconColorHex;
            if ( sprite == null )
            {
                //it should not be...
            }
            else
            {
                CPI.SetRelatedImage0SpriteIfNeeded( sprite.GetSpriteForUI() );
                CPI.SetRelatedImage0ColorFromHexIfNeeded( spriteColor );
            }

            bool differentWidth = false;
            string nextText = bufferSource.MainHeader.GetStringAndDoNotReset();

            Text.DirectlySetNextText( nextText );
            Vector3 mainHeader_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 0f );
            bool hasMainHeaderText = !nextText.IsEmpty();

            float desiredWidth = 0;

            tSoleTitleLeft_CallPerFrame();
            tTooltipExpandNote_CallPerFrame( tMainText_changedSizeLastFrame );

            tMainText_changedSizeLastFrame = false;

            {
                float headerWidth = tSoleTitleLeft_TextSize.x + 2.4f + 2.4f + 29.89984f;
                float footerWidth = tTooltipExpandNote_TextSize.x;
                if ( footerWidth > 0 )
                    footerWidth += 7.2f + 7.2f;

                float mainHeaderWidth = hasMainHeaderText ? mainHeader_TextSize.x : 0;
                if ( mainHeaderWidth > 0 )
                    mainHeaderWidth += 4f + 4f;

                desiredWidth = Mathf.Max( headerWidth, footerWidth, desiredWidth, mainHeaderWidth );

                if ( tMainText_lastWidth != desiredWidth )
                {
                    tMainText_lastWidth = desiredWidth;
                    bun.MyElement.RelevantRect.UI_SetWidth( tMainText_lastWidth );
                    differentWidth = true;
                }
            }

            if ( differentWidth )
                tMainText_changedSizeLastFrame = true;

            float newHeight = hasMainHeaderText ? mainHeader_TextSize.y : 0;// * globalScale;
            if ( hasMainHeaderText )
                newHeight += 29.7f + 8f;
            else
                newHeight += 29.7f  + 4f;

            bool canExpand = bufferSource.CanExpand != CanExpandType.No;
            if ( tMainText_lastCouldExpand != canExpand )
            {
                tMainText_lastCouldExpand = canExpand;
                //Left      rectTransform.offsetMin.x;
                //Right     rectTransform.offsetMax.x;
                //Top       rectTransform.offsetMax.y;
                //Bottom    rectTransform.offsetMin.y;

                CPI.Element.RelatedTransforms[0].offsetMin = new Vector2( 1.2f, //left
                    canExpand ? 20.4f : 1.2f ); //bottom
            }
            if ( canExpand )
                newHeight += 16.24f;

            if ( tMainText_lastHeight != newHeight )
            {
                tMainText_lastHeight = newHeight;
                bun.MyElement.RelevantRect.UI_SetHeight( newHeight );
                tMainText_changedSizeLastFrame = true;
            }

            if ( bufferSource.ShouldTooltipBeRed != tMainText_wasRed )
            {
                tMainText_wasRed = bufferSource.ShouldTooltipBeRed;
                CPI.Element.RelatedImages[2].sprite = CPI.Element.RelatedSprites[tMainText_wasRed ? 1 : 0];
                CPI.Element.RelatedImages[1].sprite = CPI.Element.RelatedSprites[tMainText_wasRed ? 3 : 2];
            }
        }
        #endregion

        #region tTooltipExpandNote
        public Vector2 tTooltipExpandNote_TextSize;
        public ArcenUIWrapperedTMProText tTooltipExpandNote_Text;

        public void tTooltipExpandNote_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( tTooltipExpandNote_Text == null )
                tTooltipExpandNote_Text = Text;
        }
        public void tTooltipExpandNote_CallPerFrame( bool ForceSet )
        {
            if ( tTooltipExpandNote_Text == null )
                return; //not ready yet

            if ( bufferSource.CanExpand != CanExpandType.No )
            {
                ArcenDoubleCharacterBuffer buffer = tTooltipExpandNote_Text.StartWritingToBuffer();
                InputCaching.AppendDetailedTooltipInstructionsToTooltipPlain( buffer, bufferSource.CanExpand == CanExpandType.Brief );
                tTooltipExpandNote_Text.FinishWritingToBuffer();
                tTooltipExpandNote_TextSize = tTooltipExpandNote_Text.SetTextNowIfNeededAndGetSize( ForceSet, 0f );
            }
            else
                tTooltipExpandNote_TextSize = Vector2.zero;
        }

        public bool tTooltipExpandNote_GetShouldBeHidden()
        {
            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
            {
                tTooltipExpandNote_TextSize = Vector2.zero;
                return true;
            }
            if ( bufferSource.CanExpand == CanExpandType.No )
            {
                tTooltipExpandNote_TextSize = Vector2.zero;
                return true;
            }
            return false;
        }
        #endregion
    }
}

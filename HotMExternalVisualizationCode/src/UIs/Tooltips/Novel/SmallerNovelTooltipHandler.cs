using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class SmallerNovelTooltipHandler
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

        #region tSoleTitleRight
        public Vector2 tSoleTitleRight_TextSize;
        public ArcenUIWrapperedTMProText tSoleTitleRight_Text;

        public void tSoleTitleRight_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( tSoleTitleRight_Text == null )
                tSoleTitleRight_Text = Text;
        }
        public void tSoleTitleRight_CallPerFrame()
        {
            if ( tSoleTitleRight_Text == null )
                return; //not ready yet

            tSoleTitleRight_Text.DirectlySetNextText( bufferSource.TitleUpperRight.GetStringAndDoNotReset() );
            tSoleTitleRight_TextSize = tSoleTitleRight_Text.SetTextNowIfNeededAndGetSize( false, 0f );
        }
        #endregion

        #region tFrameTitle
        public Vector2 tFrameTitle_TextSize;
        public ArcenUIWrapperedTMProText tFrameTitle_Text;
        public bool tFrameTitle_HasText;
        public static RectTransform tFrameTitle_Rect;

        public void tFrameTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( tFrameTitle_Text == null )
                tFrameTitle_Text = Text;
            if ( tFrameTitle_Rect == null )
                tFrameTitle_Rect = Base.Element.RelevantRect;
        }
        public void tFrameTitle_CallPerFrame()
        {
            if ( tFrameTitle_Text == null )
                return; //not ready yet

            tFrameTitle_Text.DirectlySetNextText( bufferSource.FrameTitle.GetStringAndDoNotReset() );
            tFrameTitle_TextSize = tFrameTitle_Text.SetTextNowIfNeededAndGetSize( false, 0f );
            tFrameTitle_HasText = !tFrameTitle_Text.GetCurrentTextIsEmpty();
        }
        #endregion

        #region tFrameText
        public Vector2 tFrameText_TextSize;
        public ArcenUIWrapperedTMProText tFrameText_Text;
        public bool tFrameText_HasText;

        public void tFrameText_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( tFrameText_Text == null )
                tFrameText_Text = Text;
        }
        public void tFrameText_CallPerFrame( bool ForceSet )
        {
            if ( tFrameText_Text == null )
                return; //not ready yet

            tFrameText_Text.DirectlySetNextText( bufferSource.FrameBody.GetStringAndDoNotReset() );
            tFrameText_TextSize = tFrameText_Text.SetTextNowIfNeededAndGetSize( ForceSet, 1f );
            tFrameText_HasText = !tFrameText_Text.GetCurrentTextIsEmpty();
        }
        #endregion

        #region tMainTextHeader
        public Vector2 tMainTextHeader_TextSize;
        public ArcenUIWrapperedTMProText tMainTextHeader_Text;
        public bool tMainTextHeader_HasText;

        public void tMainTextHeader_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( tMainTextHeader_Text == null )
                tMainTextHeader_Text = Text;
        }
        public void tMainTextHeader_CallPerFrame( bool ForceSet )
        {
            if ( tMainTextHeader_Text == null )
                return; //not ready yet

            tMainTextHeader_Text.DirectlySetNextText( bufferSource.MainHeader.GetStringAndDoNotReset() );
            tMainTextHeader_TextSize = tMainTextHeader_Text.SetTextNowIfNeededAndGetSize( ForceSet, 0f );
            tMainTextHeader_HasText = !tMainTextHeader_Text.GetCurrentTextIsEmpty();
        }
        #endregion

        #region tMainText
        public float tMainText_lastWidth = -1f;
        private float tMainText_lastHeight = -1f;
        private bool tMainText_lastCouldExpand = true;

        private bool tMainText_haveGottenTopAndBottomYs = false;
        private float tMainText_top1Y = 0f;

        private float tMainText_lastFrameHeight = -1f;

        private bool tMainText_wasRed = false;
        private bool tMainText_changedSizeLastFrame = true;

        public void tMainText_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
                return;

            NoteLog.LastTimeAnyTooltipWasShown = ArcenTime.AnyTimeSinceStartF;

            IA5Sprite sprite = bufferSource.Icon;
            string spriteColor = bufferSource.IconColorHex;
            if ( sprite == null )
            {
                if ( CPI.SetRelatedImage0EnabledIfNeeded( false ) )
                {
                    //we are shrinking

                    CPI.Element.RelatedTransforms[1].anchoredPosition = new Vector3( 7.2f, -3.6f, 0f ); //left text
                    CPI.Element.RelatedTransforms[3].anchoredPosition = new Vector3( -4.199921f, -3.6f, 0f ); //right text
                }
            }
            else
            {
                if ( CPI.SetRelatedImage0EnabledIfNeeded( true ) )
                {
                    //we are growing

                    CPI.Element.RelatedTransforms[1].anchoredPosition = new Vector3( 24.7f, -3.6f, 0f ); //left text
                    CPI.Element.RelatedTransforms[3].anchoredPosition = new Vector3( -4.199921f, -3.6f, 0f ); //right text
                }
                CPI.SetRelatedImage0SpriteIfNeeded( sprite.GetSpriteForUI() );
                CPI.SetRelatedImage0ColorFromHexIfNeeded( spriteColor );
            }

            bool differentWidth = false;
            string nextText = bufferSource.Main.GetStringAndDoNotReset();
            bool hasMainText = !nextText.IsEmpty();

            float desiredWidth = hasMainText || tFrameText_HasText ? bufferSource.TooltipWidth : 0;

            tSoleTitleLeft_CallPerFrame();
            tSoleTitleRight_CallPerFrame();
            tTooltipExpandNote_CallPerFrame( tMainText_changedSizeLastFrame );
            tMainTextHeader_CallPerFrame( tMainText_changedSizeLastFrame );
            tFrameTitle_CallPerFrame();
            tFrameText_CallPerFrame( tMainText_changedSizeLastFrame );

            tMainText_changedSizeLastFrame = false;

            {
                float headerWidth = tSoleTitleLeft_TextSize.x + tSoleTitleRight_TextSize.x + 7.2f + (sprite == null ? 9.2f : 24.7f);
                float footerWidth = tTooltipExpandNote_TextSize.x;
                if ( footerWidth > 0 )
                    footerWidth += 7.2f + 7.2f;

                float mainHeaderWidth = tMainTextHeader_HasText ? tMainTextHeader_TextSize.x : 0;
                if ( mainHeaderWidth > 0 )
                    mainHeaderWidth += 8f + 8f;

                float frameHeaderWidth = tFrameTitle_HasText ? tFrameTitle_TextSize.x : 0;
                if ( frameHeaderWidth > 0 )
                    frameHeaderWidth += 16f + 16f;

                desiredWidth = Mathf.Max( headerWidth, footerWidth, desiredWidth, mainHeaderWidth );
                desiredWidth = Mathf.Max( desiredWidth, frameHeaderWidth );

                if ( tMainText_lastWidth != desiredWidth )
                {
                    tMainText_lastWidth = desiredWidth;
                    bun.MyElement.RelevantRect.UI_SetWidth( tMainText_lastWidth );
                    differentWidth = true;
                }
            }

            //this wanders if not set every frame
            CPI.Element.RelatedTransforms[4].offsetMax = new Vector2( -8f, //right
                tMainTextHeader_HasText ? -( 11f + tMainTextHeader_TextSize.y) : -8f ); //top

            Text.DirectlySetNextText( nextText );
            Vector2 rawTextSize = Text.SetTextNowIfNeededAndGetSize( differentWidth || tMainText_changedSizeLastFrame, bufferSource.Main_ExtraSizePerLine );
            //float globalScale = bun.GetScale();

            if ( differentWidth )
                tMainText_changedSizeLastFrame = true;

            float newHeight = hasMainText ? rawTextSize.y : 0;// * globalScale;
            if ( hasMainText )
                newHeight += 48f;
            else
                newHeight += 36f;

            if ( tMainTextHeader_HasText )
            {
                if ( hasMainText )
                    newHeight += 1f + tMainTextHeader_TextSize.y;
                else
                    newHeight += 14f + tMainTextHeader_TextSize.y;
            }

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
                newHeight += 8f;

            CPI.SetRelatedImage2EnabledIfNeeded( hasMainText || tMainTextHeader_HasText );

            bool hasFrameText = tFrameText_HasText || tFrameTitle_HasText;
            if ( hasFrameText )
            {
                if ( CPI.SetRelatedImage1EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                float frameHeight = (tFrameText_HasText ? tFrameText_TextSize.y + 26.2f : 18.2f);
                if ( frameHeight != tMainText_lastFrameHeight )
                {
                    tMainText_lastFrameHeight = frameHeight;
                    CPI.Element.RelatedTransforms[2].UI_SetHeight( frameHeight );
                    tMainText_changedSizeLastFrame = true;
                }
                newHeight += frameHeight;

                tFrameTitle_Rect.UI_SetHeight( 13.2f ); //for some reason this misbehaves
            }
            else
            {
                if ( CPI.SetRelatedImage1EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }

                if ( !canExpand )
                    newHeight -= 10f; //this is too tall in these cases
            }

            if ( tMainText_lastHeight != newHeight )
            {
                tMainText_lastHeight = newHeight;
                bun.MyElement.RelevantRect.UI_SetHeight( newHeight );
                tMainText_changedSizeLastFrame = true;
            }

            if ( !tMainText_haveGottenTopAndBottomYs )
            {
                tMainText_haveGottenTopAndBottomYs = true;
                tMainText_top1Y = CPI.Element.RelatedTransforms[1].offsetMin.y;
            }

            if ( bufferSource.ShouldTooltipBeRed != tMainText_wasRed )
            {
                tMainText_wasRed = bufferSource.ShouldTooltipBeRed;
                CPI.Element.RelatedImages[3].sprite = CPI.Element.RelatedSprites[tMainText_wasRed ? 1 : 0];
                CPI.Element.RelatedImages[2].sprite = CPI.Element.RelatedSprites[tMainText_wasRed ? 3 : 2];
            }

            bufferSource.ShadowStyle.ApplyTo( CPI.Element.RelatedShadows[0] );
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

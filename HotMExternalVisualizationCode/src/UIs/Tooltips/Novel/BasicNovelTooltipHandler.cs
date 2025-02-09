using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class BasicNovelTooltipHandler
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

        public RectTransform tUpperTitle_Rect;

        public void tUpperTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tUpperTitle_Rect == null )
                tUpperTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.TitleUpperLeft.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
        }

        public bool tUpperTitleRight_HasText;

        public void tUpperTitleRight_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            Text.DirectlySetNextText( bufferSource.TitleUpperRight.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );

            tUpperTitleRight_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tLowerTitle_Rect;
        public bool tLowerTitle_HasText;

        public void tLowerTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tLowerTitle_Rect == null )
                tLowerTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.TitleLowerLeft.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );

            tLowerTitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public bool tLowerTitleRight_HasText;

        public void tLowerTitleRight_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            Text.DirectlySetNextText( bufferSource.TitleLowerRight.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );

            tLowerTitleRight_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameTitle_Rect;
        public bool tFrameTitle_HasText;

        public void tFrameTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tFrameTitle_Rect == null )
                tFrameTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameTitle.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tFrameTitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameText_Rect;
        public Vector2 tFrameText_TextSize;
        public bool tFrameText_HasText = false;
        private float tFrameText_lastOverallWidth = -1f;

        public void tFrameText_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tFrameText_Rect == null )
                tFrameText_Rect = Base.Element.RelevantRect;

            bool forceSet = false;
            if ( tFrameText_lastOverallWidth != tMainText_lastWidth )
            {
                tFrameText_lastOverallWidth = tMainText_lastWidth;
                forceSet = true;
            }
            Text.DirectlySetNextText( bufferSource.FrameBody.GetStringAndDoNotReset() );
            tFrameText_TextSize = Text.SetTextNowIfNeededAndGetSize( forceSet, 1f );
            tFrameText_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public float tMainText_lastWidth = -1f;
        private float tMainText_lastHeight = -1f;
        private bool tMainText_LastCouldExpand = true;

        private bool tMainText_HaveGottenTopAndBottomYs = false;
        private float tMainText_top1Y = 0f;
        private float tMainText_top2Y = 0f;

        private float tMainText_lastFrameHeight = -1f;

        private bool hasGottenUpperLeftYs = false;
        private float tUpperTitle_OffsetMaxX = 0f;
        private float tUpperTitle_OffsetMaxY = 0f;
        private float tLowerTitle_OffsetMaxX = 0f;
        private float tLowerTitle_OffsetMaxY = 0f;

        public void tMainText_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
                return;

            NoteLog.LastTimeAnyTooltipWasShown = ArcenTime.AnyTimeSinceStartF;

            bool differentWidth = false;
            if ( tMainText_lastWidth != bufferSource.TooltipWidth )
            {
                tMainText_lastWidth = bufferSource.TooltipWidth;
                bun.MyElement.RelevantRect.UI_SetWidth( tMainText_lastWidth );
                differentWidth = true;
            }

            Text.DirectlySetNextText( bufferSource.Main.GetStringAndDoNotReset() );

            Vector2 rawTextSize = Text.SetTextNowIfNeededAndGetSize( differentWidth, 1f );
            //float globalScale = bun.GetScale();

            float newHeight = rawTextSize.y;// * globalScale;
            newHeight += 78f;

            bool canExpand = bufferSource.CanExpand != CanExpandType.No;
            if ( tMainText_LastCouldExpand != canExpand )
            {
                tMainText_LastCouldExpand = canExpand;
                //Left      rectTransform.offsetMin.x;
                //Right     rectTransform.offsetMax.x;
                //Top       rectTransform.offsetMax.y;
                //Bottom    rectTransform.offsetMin.y;

                CPI.Element.RelatedTransforms[0].offsetMin = new Vector2( 1.2f, //left
                    canExpand ? 20.4f : 1.2f ); //bottom
            }
            if ( canExpand )
                newHeight += 8f;

            if ( !hasGottenUpperLeftYs )
            {
                hasGottenUpperLeftYs = true;
                tUpperTitle_OffsetMaxX = tUpperTitle_Rect.offsetMax.x;
                tUpperTitle_OffsetMaxY = tUpperTitle_Rect.offsetMax.y;
                tLowerTitle_OffsetMaxX = tLowerTitle_Rect.offsetMax.x;
                tLowerTitle_OffsetMaxY = tLowerTitle_Rect.offsetMax.y;
            }

            tUpperTitle_Rect.offsetMax = new Vector2( !tUpperTitleRight_HasText ? -11.6f : tUpperTitle_OffsetMaxX, tUpperTitle_OffsetMaxY );
            tLowerTitle_Rect.offsetMax = new Vector2( !tLowerTitleRight_HasText ? -11.6f : tLowerTitle_OffsetMaxX, tLowerTitle_OffsetMaxY );

            bool hasFrameText = tFrameText_HasText || tFrameTitle_HasText;
            if ( hasFrameText )
            {
                if ( CPI.SetRelatedImage1EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                float frameHeight = (tFrameText_HasText ? tFrameText_TextSize.y : 0) + (tFrameTitle_HasText ? 26.2f : 8f );
                if ( frameHeight != tMainText_lastFrameHeight )
                {
                    tMainText_lastFrameHeight = frameHeight;
                    CPI.Element.RelatedTransforms[3].UI_SetHeight( frameHeight );
                }
                newHeight += frameHeight;

                tFrameText_Rect.offsetMax = new Vector2( -8f, //right
                    tFrameTitle_HasText ? -22.2f : -4f ); //top

                tFrameTitle_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves

                if ( canExpand )
                    newHeight += 10f; //this is too short in these cases
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
            }

            if ( !tMainText_HaveGottenTopAndBottomYs )
            {
                tMainText_HaveGottenTopAndBottomYs = true;
                tMainText_top1Y = CPI.Element.RelatedTransforms[1].offsetMin.y;
                tMainText_top2Y = CPI.Element.RelatedTransforms[2].offsetMin.y;
            }

            IA5Sprite sprite = bufferSource.Icon;
            string spriteColor = bufferSource.IconColorHex;
            if ( sprite == null )
            {
                if ( CPI.SetRelatedImage0EnabledIfNeeded( false ) )
                {
                    //we are shrinking

                    CPI.Element.RelatedTransforms[1].offsetMin = new Vector2( 11.6f, //left
                        tMainText_top1Y ); //bottom
                    CPI.Element.RelatedTransforms[2].offsetMin = new Vector2( 11.6f, //left
                        tMainText_top2Y ); //bottom
                }
            }
            else
            {
                if ( CPI.SetRelatedImage0EnabledIfNeeded( true ) )
                {
                    //we are growing

                    CPI.Element.RelatedTransforms[1].offsetMin = new Vector2( 51.6f, //left
                        tMainText_top1Y ); //bottom
                    CPI.Element.RelatedTransforms[2].offsetMin = new Vector2( 51.6f, //left
                        tMainText_top2Y ); //bottom
                }
                CPI.SetRelatedImage0SpriteIfNeeded( sprite.GetSpriteForUI() );
                CPI.SetRelatedImage0ColorFromHexIfNeeded( spriteColor );
            }

            bufferSource.ShadowStyle.ApplyTo( CPI.Element.RelatedShadows[0] );
        }

        public Vector2 tTooltipExpandNote_TextSize;
        private bool tTooltipExpandNote_LastWasHidden = false;

        public void tTooltipExpandNote_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( !tTooltipExpandNote_LastWasHidden )
            {
                ArcenDoubleCharacterBuffer buffer = Text.StartWritingToBuffer();
                if ( bufferSource.CanExpand == CanExpandType.HyperDetailed )
                    InputCaching.AppendHyperDetailedTooltipInstructionsToTooltipPlain( buffer );
                else
                    InputCaching.AppendDetailedTooltipInstructionsToTooltipPlain( buffer, bufferSource.CanExpand == CanExpandType.Brief );
                Text.FinishWritingToBuffer();

                tTooltipExpandNote_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 0f );
            }
            else
                tTooltipExpandNote_TextSize = Vector2.zero;
        }

        public bool tTooltipExpandNote_GetShouldBeHidden()
        {
            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
            {
                tTooltipExpandNote_LastWasHidden = true;
                tTooltipExpandNote_TextSize = Vector2.zero;
                return true;
            }
            if ( bufferSource.CanExpand == CanExpandType.No )
            {
                tTooltipExpandNote_LastWasHidden = true;
                tTooltipExpandNote_TextSize = Vector2.zero;
                return true;
            }
            tTooltipExpandNote_LastWasHidden = false;
            return false;
        }
    }
}

using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using FastExcel;

namespace Arcen.HotM.ExternalVis
{
    public class UnitStyleNovelTooltipHandler
    {
        public readonly TooltipNovelBundle bun = new TooltipNovelBundle();
        public CustomUIAbstractBase CPI;
        public UnitStyleNovelTooltipBuffer bufferSource;
        public USNStatManager statManager;

        public bool GetShouldDrawThisFrame()
        {
            if ( CPI == null || bun.CurrentData == null )
                return false;
            return bun.GetShouldDrawThisFrame_Normal();
        }

        public void tTitle_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            Text.DirectlySetNextText( bufferSource.Title.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
        }

        public void tUpperStats_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            Text.DirectlySetNextText( bufferSource.UpperStats.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
        }

        public void tLowerStats_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            Text.DirectlySetNextText( bufferSource.LowerStats.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
        }

        public RectTransform tFrameATitle_Rect;
        public bool tFrameATitle_HasText;

        public void tFrameATitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tFrameATitle_Rect == null )
                tFrameATitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameATitle.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tFrameATitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameAText_Rect;
        public Vector2 tFrameAText_TextSize;
        public bool tFrameAText_HasText = false;

        public void tFrameAText_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tFrameAText_Rect == null )
                tFrameAText_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameABody.GetStringAndDoNotReset() );
            tFrameAText_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tFrameAText_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameBTitle_Rect;
        public bool tFrameBTitle_HasText;

        public void tFrameBTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tFrameBTitle_Rect == null )
                tFrameBTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameBTitle.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tFrameBTitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameBText_Rect;
        public Vector2 tFrameBText_TextSize;
        public bool tFrameBText_HasText = false;

        public void tFrameBText_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tFrameBText_Rect == null )
                tFrameBText_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameBBody.GetStringAndDoNotReset() );
            tFrameBText_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tFrameBText_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameCTitle_Rect;
        public bool tFrameCTitle_HasText;

        public void tFrameCTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tFrameCTitle_Rect == null )
                tFrameCTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameCTitle.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tFrameCTitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameCText_Rect;
        public Vector2 tFrameCText_TextSize;
        public bool tFrameCText_HasText = false;

        public void tFrameCText_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tFrameCText_Rect == null )
                tFrameCText_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameCBody.GetStringAndDoNotReset() );
            tFrameCText_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tFrameCText_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tFrameDTitle_Rect;
        public bool tFrameDTitle_HasText;

        public void tFrameDTitle_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tFrameDTitle_Rect == null )
                tFrameDTitle_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.FrameDTitle.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tFrameDTitle_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tCol2Title_Rect;
        public bool tCol2Title_HasText;

        public void tCol2Title_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tCol2Title_Rect == null )
                tCol2Title_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.Col2Title.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tCol2Title_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tCol2Text_Rect;
        public Vector2 tCol2Text_TextSize;
        public bool tCol2Text_HasText = false;

        public void tCol2Text_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tCol2Text_Rect == null )
                tCol2Text_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.Col2Body.GetStringAndDoNotReset() );
            tCol2Text_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tCol2Text_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tCol3Title_Rect;
        public bool tCol3Title_HasText;

        public void tCol3Title_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;
            if ( tCol3Title_Rect == null )
                tCol3Title_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.Col3Title.GetStringAndDoNotReset() );
            Text.SetTextNowIfNeeded( true, false );
            tCol3Title_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tCol3Text_Rect;
        public Vector2 tCol3Text_TextSize;
        public bool tCol3Text_HasText = false;

        public void tCol3Text_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tCol3Text_Rect == null )
                tCol3Text_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.Col3Body.GetStringAndDoNotReset() );
            tCol3Text_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tCol3Text_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public RectTransform tLowerNarrativeText_Rect;
        public Vector2 tLowerNarrativeText_TextSize;
        public bool tLowerNarrativeText_HasText = false;

        public void tLowerNarrativeText_DoPerFrame( TextAbstractBase Base, ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() )
                return;

            if ( tLowerNarrativeText_Rect == null )
                tLowerNarrativeText_Rect = Base.Element.RelevantRect;

            Text.DirectlySetNextText( bufferSource.LowerNarrative.GetStringAndDoNotReset() );
            tLowerNarrativeText_TextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            tLowerNarrativeText_HasText = !Text.GetCurrentTextIsEmpty();
        }

        public float tMainText_lastWidth = -1f;
        private float tMainText_lastHeight = -1f;
        private bool tMainText_LastCouldExpand = true;
        private float tLastWindowWidth = -1f;

        private float tMainText_lastFrameAHeight = -1f;
        private float tMainText_lastFrameBHeight = -1f;
        private float tMainText_lastFrameCHeight = -1f;
        private float tMainText_lastFrameDHeight = -1f;
        private float tMainText_lastCol2Height = -1f;
        private float tMainText_lastCol3Height = -1f;
        private float tMainText_lastLowerNarrativeHeight = -1f;

        public void tMainText_DoPerFrame( ArcenUIWrapperedTMProText Text )
        {
            if ( !bun.GetShouldDrawThisFrame_Normal() || statManager == null )
                return;

            IUITooltipDataSource data = bun.CurrentData;
            if ( data == null )
                return;

            NoteLog.LastTimeAnyTooltipWasShown = ArcenTime.AnyTimeSinceStartF;

            Text.DirectlySetNextText( bufferSource.Main.GetStringAndDoNotReset() );

            Vector2 rawTextSize = Text.SetTextNowIfNeededAndGetSize( false, 1f );
            //float globalScale = bun.GetScale();

            float newHeight = rawTextSize.y;// * globalScale;
            newHeight += 84.9f;

            statManager.RenderStatLines( -( rawTextSize.y + 4f + 8f ), out float addedStatLineHeight );
            if ( addedStatLineHeight < 0f )
                addedStatLineHeight = -addedStatLineHeight;
            if ( addedStatLineHeight > 0f )
                addedStatLineHeight += 4f;

            newHeight += addedStatLineHeight;

            bool canExpand = bufferSource.CanExpand != HyperCanExpandType.No;
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

            float lowerNarrativeHeight = 0f;

            bool hasLowerNarrativeText = tLowerNarrativeText_HasText;
            if ( hasLowerNarrativeText && tLowerNarrativeText_Rect != null )
            {
                if ( CPI.SetRelatedImage3EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                lowerNarrativeHeight = (tLowerNarrativeText_HasText ? tLowerNarrativeText_TextSize.y : 0);
                if ( lowerNarrativeHeight != tMainText_lastLowerNarrativeHeight )
                {
                    tMainText_lastLowerNarrativeHeight = lowerNarrativeHeight;
                    tLowerNarrativeText_Rect.UI_SetHeight( lowerNarrativeHeight );
                }
                newHeight += lowerNarrativeHeight + 8f;

                tLowerNarrativeText_Rect.anchoredPosition = new Vector2( 10f, 6f );
            }

            float frameCHeight = 0f;

            bool hasFrameCText = tFrameCText_HasText || tFrameCTitle_HasText;
            if ( hasFrameCText )
            {
                if ( CPI.SetRelatedImage6EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                frameCHeight = (tFrameCText_HasText ? tFrameCText_TextSize.y : 0) + (tFrameCTitle_HasText ? 26.2f : 8f);
                if ( frameCHeight != tMainText_lastFrameCHeight )
                {
                    tMainText_lastFrameCHeight = frameCHeight;
                    CPI.Element.RelatedTransforms[6].UI_SetHeight( frameCHeight );
                }
                newHeight += frameCHeight;

                float lowerNarrativeOffset = lowerNarrativeHeight > 0 ? lowerNarrativeHeight + 6f : 0f;

                CPI.Element.RelatedTransforms[6].anchoredPosition = new Vector2( 0f, 6f + lowerNarrativeOffset );

                if ( tFrameCText_Rect != null )
                    tFrameCText_Rect.offsetMax = new Vector2( -4f, //right
                        tFrameCTitle_HasText ? -22.2f : -4f ); //top

                if ( tFrameCTitle_Rect != null )
                    tFrameCTitle_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves

                if ( canExpand )
                    newHeight += 4f; //this is too short in these cases
            }
            else
            {
                if ( CPI.SetRelatedImage6EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }

                if ( !canExpand )
                    newHeight -= 4f; //this is too tall in these cases
            }

            float frameBHeight = 0f;

            bool hasFrameBText = tFrameBText_HasText || tFrameBTitle_HasText;
            if ( hasFrameBText )
            {
                if ( CPI.SetRelatedImage3EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                frameBHeight = (tFrameBText_HasText ? tFrameBText_TextSize.y : 0) + (tFrameBTitle_HasText ? 26.2f : 8f);
                if ( frameBHeight != tMainText_lastFrameBHeight )
                {
                    tMainText_lastFrameBHeight = frameBHeight;
                    CPI.Element.RelatedTransforms[3].UI_SetHeight( frameBHeight );
                }
                newHeight += frameBHeight;

                float lowerNarrativeOffset = lowerNarrativeHeight > 0 ? lowerNarrativeHeight + 6f : 0f;
                float frameCOffset = frameCHeight > 0 ? frameCHeight + 6f : 0f;

                CPI.Element.RelatedTransforms[3].anchoredPosition = new Vector2( 0f, 6f + lowerNarrativeOffset + frameCOffset );

                if ( tFrameBText_Rect != null )
                    tFrameBText_Rect.offsetMax = new Vector2( -4f, //right
                        tFrameBTitle_HasText ? -22.2f : -4f ); //top

                if ( tFrameBTitle_Rect != null )
                    tFrameBTitle_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves

                if ( canExpand )
                    newHeight += 4f; //this is too short in these cases
            }
            else
            {
                if ( CPI.SetRelatedImage3EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }

                if ( !canExpand )
                    newHeight -= 4f; //this is too tall in these cases
            }

            float frameAHeight = 0;

            bool hasFrameAText = tFrameAText_HasText || tFrameATitle_HasText;
            if ( hasFrameAText )
            {
                if ( CPI.SetRelatedImage4EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                if ( bufferSource.FrameAIcon != null )
                {
                    CPI.SetRelatedImage5EnabledIfNeeded( true ); //the icon

                    CPI.SetRelatedImage5SpriteIfNeeded( bufferSource.FrameAIcon.GetSpriteForUI() );
                    CPI.SetRelatedImage5ColorFromHexIfNeeded( bufferSource.FrameAIconColorHex );
                }
                else
                    CPI.SetRelatedImage5EnabledIfNeeded( false ); //the icon

                frameAHeight = (tFrameAText_HasText ? tFrameAText_TextSize.y : 0) + (tFrameATitle_HasText ? 26.2f : 8f);
                if ( frameAHeight != tMainText_lastFrameAHeight )
                {
                    tMainText_lastFrameAHeight = frameAHeight;
                    CPI.Element.RelatedTransforms[4].UI_SetHeight( frameAHeight );
                }
                newHeight += frameAHeight;

                float lowerNarrativeOffset = lowerNarrativeHeight > 0 ? lowerNarrativeHeight + 6f : 0f;
                float frameBOffset = frameBHeight > 0 ? frameBHeight + 6f : 0f;
                float frameCOffset = frameCHeight > 0 ? frameCHeight + 6f : 0f;

                CPI.Element.RelatedTransforms[4].anchoredPosition = new Vector2( 0f, 6f + frameBOffset + lowerNarrativeOffset + frameCOffset );

                if ( tFrameAText_Rect != null )
                    tFrameAText_Rect.offsetMax = new Vector2( -4f, //right
                        tFrameATitle_HasText ? -22.2f : -4f ); //top

                if ( tFrameATitle_Rect != null )
                    tFrameATitle_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves

                if ( canExpand )
                    newHeight += 4f; //this is too short in these cases
            }
            else
            {
                if ( CPI.SetRelatedImage4EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }
                CPI.SetRelatedImage5EnabledIfNeeded( false ); //the icon

                if ( !canExpand )
                    newHeight -= 4f; //this is too tall in these cases
            }

            float frameDHeight = 0f;

            statManager.RenderFrameDStatLines( out float frameDStatsSize );
            if ( frameDStatsSize < 0f )
                frameDStatsSize = -frameDStatsSize;

            bool hasFrameDText = bufferSource.FrameDStats.Count > 0 || tFrameDTitle_HasText;
            if ( hasFrameDText )
            {
                if ( CPI.SetRelatedImage11EnabledIfNeeded( true ) )
                {
                    //we are visible
                }

                frameDHeight = (frameDStatsSize >= 0f ? frameDStatsSize : 0) + (tFrameDTitle_HasText ? 26.2f : 8f);
                if ( frameDHeight != tMainText_lastFrameDHeight )
                {
                    tMainText_lastFrameDHeight = frameDHeight;
                    CPI.Element.RelatedTransforms[9].UI_SetHeight( frameDHeight );
                }
                newHeight += frameDHeight;

                float lowerNarrativeOffset = lowerNarrativeHeight > 0 ? lowerNarrativeHeight + 6f : 0f;
                float frameAOffset = frameAHeight > 0 ? frameAHeight + 6f : 0f;
                float frameBOffset = frameBHeight > 0 ? frameBHeight + 6f : 0f;
                float frameCOffset = frameCHeight > 0 ? frameCHeight + 6f : 0f;

                CPI.Element.RelatedTransforms[9].anchoredPosition = new Vector2( 0f, 6f + frameAOffset + frameBOffset + lowerNarrativeOffset + frameCOffset );

                if ( tFrameCText_Rect != null )
                    tFrameCText_Rect.offsetMax = new Vector2( -4f, //right
                        tFrameCTitle_HasText ? -22.2f : -4f ); //top

                if ( tFrameCTitle_Rect != null )
                    tFrameCTitle_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves

                newHeight += 4f; //this is too short in these cases
            }
            else
            {
                if ( CPI.SetRelatedImage11EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }
            }

            if ( bufferSource.PortraitIcon != null )
            {
                CPI.SetRelatedImage0SpriteIfNeeded( bufferSource.PortraitIcon.GetSpriteForUI() );

                if ( !bufferSource.PortraitFrameColorHex.IsEmpty() )
                    CPI.SetRelatedImage1ColorFromHexIfNeeded( bufferSource.PortraitFrameColorHex );
            }

            if ( bufferSource.IconGlow != null )
            {
                CPI.SetRelatedImage2EnabledIfNeeded( true );
                CPI.SetRelatedImage2SpriteIfNeeded( bufferSource.IconGlow.GetSpriteForUI() );
                CPI.SetRelatedImage2ColorFromHexIfNeeded( bufferSource.IconGlowColorHex );
            }
            else
                CPI.SetRelatedImage2EnabledIfNeeded( false );

            if ( bufferSource.FrameAIcon != null )
            {
                CPI.SetRelatedImage5EnabledIfNeeded( true );
                CPI.SetRelatedImage5SpriteIfNeeded( bufferSource.FrameAIcon.GetSpriteForUI() );
                CPI.SetRelatedImage5ColorFromHexIfNeeded( bufferSource.FrameAIconColorHex );
            }
            else
                CPI.SetRelatedImage5EnabledIfNeeded( false );

            TooltipShadowStyle shadowStyle = bufferSource.ShadowStyle;

            bool hasCol2Text = tCol2Text_HasText || tCol2Title_HasText;
            bool hasCol3Text = tCol3Text_HasText || tCol3Title_HasText;

            if ( hasCol2Text || hasCol3Text )
                shadowStyle = TooltipShadowStyle.WideDark;

            float col2Height = 0;
            if ( hasCol2Text )
            {
                if ( CPI.SetRelatedImage7EnabledIfNeeded( true ) )
                {
                    //we are visible
                }
                CPI.SetRelatedImage9EnabledIfNeeded( true );
                CPI.Element.RelatedShadows[1].SetEnabledIfNeeded( true );
                shadowStyle.ApplyTo( CPI.Element.RelatedShadows[1] );

                col2Height = (tCol2Text_HasText ? tCol2Text_TextSize.y : 0) + (tCol2Title_HasText ? 29.2f : 8f);
                if ( col2Height != tMainText_lastCol2Height )
                {
                    tMainText_lastCol2Height = col2Height;
                    CPI.Element.RelatedTransforms[7].UI_SetHeight( col2Height );
                    CPI.Element.RelatedTransforms[10].UI_SetHeight( col2Height );
                }

                //if ( tCol2Text_Rect != null )
                //    tCol2Text_Rect.offsetMax = new Vector2( -4f, //right
                //        tCol2Title_HasText ? -22.2f : -4f ); //top

                //if ( tCol2Title_Rect != null )
                //    tCol2Title_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves
            }
            else
            {
                if ( CPI.SetRelatedImage7EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }
                CPI.SetRelatedImage9EnabledIfNeeded( false );
                CPI.Element.RelatedShadows[1].SetEnabledIfNeeded( false );
            }

            float col3Height = 0;
            if ( hasCol3Text )
            {
                if ( CPI.SetRelatedImage8EnabledIfNeeded( true ) )
                {
                    //we are visible
                }
                CPI.SetRelatedImage10EnabledIfNeeded( true );
                shadowStyle.ApplyTo( CPI.Element.RelatedShadows[2] );

                col3Height = (tCol3Text_HasText ? tCol3Text_TextSize.y : 0) + (tCol3Title_HasText ? 29.2f : 8f);
                if ( col3Height != tMainText_lastCol3Height )
                {
                    tMainText_lastCol3Height = col3Height;
                    CPI.Element.RelatedTransforms[8].UI_SetHeight( col3Height );
                    CPI.Element.RelatedTransforms[11].UI_SetHeight( col3Height );
                }

                //if ( tCol3Text_Rect != null )
                //    tCol3Text_Rect.offsetMax = new Vector2( -4f, //right
                //        tCol3Title_HasText ? -22.2f : -4f ); //top

                //if ( tCol3Title_Rect != null )
                //    tCol3Title_Rect.UI_SetHeight( 14.3f ); //for some reason this misbehaves
            }
            else
            {
                if ( CPI.SetRelatedImage8EnabledIfNeeded( false ) )
                {
                    //we are invisible
                }
                CPI.SetRelatedImage10EnabledIfNeeded( false );
                CPI.Element.RelatedShadows[2].SetEnabledIfNeeded( false );
            }

            bool isAtLeastTwoColumnsWide = hasCol2Text || hasCol3Text;
            bool isThreeColumnsWide = false;

            if ( hasCol3Text )
            {
                if ( hasCol2Text && col2Height + 2f + col3Height < newHeight )
                {
                    CPI.Element.RelatedTransforms[8].anchoredPosition = new Vector2( 242f, -0.0001525879f - col2Height - 2f );
                    CPI.Element.RelatedTransforms[11].anchoredPosition = new Vector2( 242f, -0.0001525879f - col2Height - 2f );
                }
                else
                {
                    if ( hasCol2Text )
                    {
                        CPI.Element.RelatedTransforms[8].anchoredPosition = new Vector2( 464f, -0.0001525879f );
                        CPI.Element.RelatedTransforms[11].anchoredPosition = new Vector2( 464f, -0.0001525879f );
                        isThreeColumnsWide = true;
                    }
                    else
                    {
                        CPI.Element.RelatedTransforms[8].anchoredPosition = new Vector2( 242f, -0.0001525879f );
                        CPI.Element.RelatedTransforms[11].anchoredPosition = new Vector2( 242f, -0.0001525879f );
                    }
                }
            }
            else
            {
                if ( newHeight < col2Height )
                    newHeight = col2Height;
                if ( newHeight < col3Height )
                    newHeight = col3Height;
            }

            shadowStyle.ApplyTo( CPI.Element.RelatedShadows[0] );

            if ( tMainText_lastHeight != newHeight )
            {
                tMainText_lastHeight = newHeight;
                bun.MyElement.RelevantRect.UI_SetHeight( newHeight );
            }

            float newWidth = 240f + (isAtLeastTwoColumnsWide ? 222f : 0f) + (isThreeColumnsWide ? 222f : 0f);
            if ( tLastWindowWidth != newWidth )
            {

                tLastWindowWidth = newWidth;
                bun.MyElement.RelevantRect.UI_SetWidth( newWidth );
            }
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
                if ( bufferSource.CanExpand == HyperCanExpandType.Hyper )
                    InputCaching.AppendHyperDetailedTooltipInstructionsToTooltipPlain( buffer );
                else
                    InputCaching.AppendDetailedTooltipInstructionsToTooltipPlain( buffer, false );
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
            if ( bufferSource.CanExpand == HyperCanExpandType.No )
            {
                tTooltipExpandNote_LastWasHidden = true;
                tTooltipExpandNote_TextSize = Vector2.zero;
                return true;
            }
            tTooltipExpandNote_LastWasHidden = false;
            return false;
        }

        public interface USNStatManager
        {
            void RenderStatLines( float StartingY, out float AddedHeight );
            void RenderFrameDStatLines( out float AddedHeight );
        }
    }
}

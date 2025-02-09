using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_UnitStyleAtMouseNovelTooltip : WindowControllerAbstractBase
    {
        private static Window_UnitStyleAtMouseNovelTooltip _instance;
        public Window_UnitStyleAtMouseNovelTooltip()
        {
            _instance = this;
            this.IsAtMouseTooltip = true;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
            
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return handler.GetShouldDrawThisFrame();
        }

        private static readonly UnitStyleNovelTooltipHandler handler = new UnitStyleNovelTooltipHandler();

        public static void ClearMyself()
        {
            handler.bun.ClearMyself();
        }

        public static void ClearAndInvalidateTimeLastSet()
        {
            handler.bun.ClearAndInvalidateTimeLastSet();
        }

        public static Int64 TotalSetCalls { get { return handler.bun.TotalSetCalls; } }

        public override void OnPerFrameForWindow( bool IsActiveWindow )
        {
            handler.bun.OnPerFrameForWindow();
        }

        public static void SetDataSourceInner_Scalable( IArcenUIElementForSizing BeAboveOrBelow, IUITooltipDataSource Data, 
            TooltipExtraText ExtraText, SideClamp SideClamp, TooltipExtraRules ExtraRules, TooltipID ToolID )
        {
            handler.bun.SetDataSourceInner_Scalable( BeAboveOrBelow, Data, ExtraText, SideClamp, ExtraRules, ToolID, InputCaching.Scale_GeneralTooltips * 1.1f );
        }

        public class customParent : CustomUIAbstractBase, UnitStyleNovelTooltipHandler.USNStatManager
        {
            public customParent()
            {
                handler.CPI = this;
                handler.bufferSource = UnitStyleNovelTooltipBuffer.Instance;
            }

            private static ButtonAbstractBase.ButtonPool<bStatLine> bStatLinePool;
            private static TextAbstractBase.TextPool<tStatExtraData> tStatExtraDataPool;
            private static ButtonAbstractBase.ButtonPool<bFrameDStatLine> bFrameDStatLinePool;
            private bool hasGlobalInitialized = false;

            public override void OnUpdate()
            {
                if ( !hasGlobalInitialized )
                {
                    if ( bStatLine.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bStatLinePool = new ButtonAbstractBase.ButtonPool<bStatLine>( bStatLine.Original, 10, "bStatLine" );
                        tStatExtraDataPool = new TextAbstractBase.TextPool<tStatExtraData>( tStatExtraData.Original, 10, "tStatExtraData" );
                        bFrameDStatLinePool = new ButtonAbstractBase.ButtonPool<bFrameDStatLine>( bFrameDStatLine.Original, 4, "bFrameDStatLine" );

                        handler.statManager = this;
                    }
                }
            }

            public override void OnSetElement( ArcenUI_Element Element )
            {
                handler.bun.UpdateWindowReferences( (ArcenUI_CustomUI)Element, "AtMouseUnitStyleNovel", TooltipBundle.PositionStyle.AtMouseStandard, true, 32750 );
            }

            public void RenderStatLines( float StartingY, out float AddedHeight )
            {
                bStatLinePool.Clear( 60 );
                tStatExtraDataPool.Clear( 60 );

                float currentY = StartingY;
                float alwaysX_DataLine = 8f;
                float alwaysX_SoloLine = 28.5f;

                bool lastWasStatDataLine = false;
                foreach ( UnitStyleNovelTooltipBuffer.IUnitStyleStat stat in handler.bufferSource.Stats )
                {
                    if ( stat is UnitStyleNovelTooltipBuffer.StatDataLine statData )
                    {
                        //15.6

                        bStatLine but = bStatLinePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( but != null )
                        {
                            but.Assign( statData );
                            but.Element.RelevantRect.anchoredPosition = new Vector2( alwaysX_DataLine, currentY );
                            currentY -= (15.6f + 2f);

                            lastWasStatDataLine = true;
                        }
                    }
                    else if ( stat is UnitStyleNovelTooltipBuffer.StatSoloLine statSolo )
                    {
                        //11.66

                        tStatExtraData tex = tStatExtraDataPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( tex != null )
                        {
                            if ( lastWasStatDataLine )
                                currentY += 2f;

                            currentY -= statSolo.ExtraSpacingBefore;

                            tex.Assign( statSolo );
                            tex.Element.RelevantRect.anchoredPosition = new Vector2( alwaysX_SoloLine, currentY );
                            currentY -= (11.66f + 1f);

                            lastWasStatDataLine = false;
                        }
                    }
                }

                AddedHeight = StartingY - currentY;
            }

            public void RenderFrameDStatLines( out float AddedHeight )
            {
                bFrameDStatLinePool.Clear( 60 );

                float startingY = -21.5f;
                float currentY = startingY;
                float alwaysX_DataLine = 4f;

                foreach ( UnitStyleNovelTooltipBuffer.IUnitStyleStat stat in handler.bufferSource.FrameDStats )
                {
                    if ( stat is UnitStyleNovelTooltipBuffer.StatDataLine statData )
                    {
                        //15.6

                        bFrameDStatLine but = bFrameDStatLinePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( but != null )
                        {
                            but.Assign( statData );
                            but.Element.RelevantRect.anchoredPosition = new Vector2( alwaysX_DataLine, currentY );
                            currentY -= (15.6f + 2f);
                        }
                    }
                }

                AddedHeight = startingY - currentY;
            }
        }

        public class tTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tTitle_DoPerFrame( Text );
            }
        }

        public class tUpperStats : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tUpperStats_DoPerFrame( Text );
            }
        }

        public class tLowerStats : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tLowerStats_DoPerFrame( Text );
            }
        }

        public class tFrameATitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameATitle_DoPerFrame( this, Text );
            }
        }

        public class tFrameAText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameAText_DoPerFrame( this, Text );
            }
        }

        public class tFrameBTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameBTitle_DoPerFrame( this, Text );
            }
        }

        public class tFrameBText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameBText_DoPerFrame( this, Text );
            }
        }

        public class tFrameCTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameCTitle_DoPerFrame( this, Text );
            }
        }

        public class tFrameCText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameCText_DoPerFrame( this, Text );
            }
        }

        public class tFrameDTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tFrameDTitle_DoPerFrame( this, Text );
            }
        }

        public class tCol2Title : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tCol2Title_DoPerFrame( this, Text );
            }
        }

        public class tCol2Text : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tCol2Text_DoPerFrame( this, Text );
            }
        }

        public class tCol3Title : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tCol3Title_DoPerFrame( this, Text );
            }
        }

        public class tCol3Text : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tCol3Text_DoPerFrame( this, Text );
            }
        }

        public class tMainText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tMainText_DoPerFrame( Text );
            }
        }

        public class tLowerNarrativeText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tLowerNarrativeText_DoPerFrame( this, Text );
            }
        }

        public class tTooltipExpandNote : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer ) { }

            public override void DoPerFrame( ArcenUIWrapperedTMProText Text )
            {
                handler.tTooltipExpandNote_DoPerFrame( Text );
            }

            public override bool GetShouldBeHidden()
            {
                return handler.tTooltipExpandNote_GetShouldBeHidden();
            }
        }

        #region bStatLine
        public class bStatLine : ButtonAbstractBaseWithImage
        {
            public static bStatLine Original;
            public bStatLine() { if ( Original == null ) Original = this; }

            public UnitStyleNovelTooltipBuffer.StatDataLine Line;

            public void Assign( UnitStyleNovelTooltipBuffer.StatDataLine NewLine )
            {
                this.Line = NewLine;

                if ( this.Element is ArcenUI_Button but )
                {
                    but.Text.DirectlySetNextText( this.Line.LeftText );
                    but.Text.SetTextNowIfNeeded( false, false );

                    but.OthersTexts[0].DirectlySetNextText( this.Line.RightText );
                    but.OthersTexts[0].SetTextNowIfNeeded( false, false );

                    this.SetRelatedImage0EnabledIfNeeded( !this.Line.IsFirst ); //all but the first have this

                    this.SetRelatedImage1SpriteIfNeeded( this.Line.Icon?.GetSpriteForUI() );
                    this.SetRelatedImage1ColorFromHexIfNeeded( this.Line.IconColorHex );
                }
            }

            public override void Clear()
            {
                this.Line = UnitStyleNovelTooltipBuffer.StatDataLine.Create();
            }

            public override bool GetShouldBeHidden()
            {
                return this.Line.IsEmpty;
            }
        }
        #endregion

        #region tStatExtraData
        public class tStatExtraData : TextAbstractBase
        {
            public static tStatExtraData Original;
            public tStatExtraData() { if ( Original == null ) Original = this; }

            public UnitStyleNovelTooltipBuffer.StatSoloLine Line;

            public void Assign( UnitStyleNovelTooltipBuffer.StatSoloLine NewLine )
            {
                this.Line = NewLine;

                if ( this.Element is ArcenUI_Text tex )
                {
                    tex.Text.DirectlySetNextText( this.Line.SoloText );
                    tex.Text.SetTextNowIfNeeded( false, false );
                }
            }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Text, ArcenDoubleCharacterBuffer Buffer )
            {
            }

            public override void Clear()
            {
                this.Line = UnitStyleNovelTooltipBuffer.StatSoloLine.Create();
            }

            public override bool GetShouldBeHidden()
            {
                return this.Line.IsEmpty;
            }
        }
        #endregion

        #region bFrameDStatLine
        public class bFrameDStatLine : ButtonAbstractBaseWithImage
        {
            public static bFrameDStatLine Original;
            public bFrameDStatLine() { if ( Original == null ) Original = this; }

            public UnitStyleNovelTooltipBuffer.StatDataLine Line;

            public void Assign( UnitStyleNovelTooltipBuffer.StatDataLine NewLine )
            {
                this.Line = NewLine;

                if ( this.Element is ArcenUI_Button but )
                {
                    but.Text.DirectlySetNextText( this.Line.LeftText );
                    but.Text.SetTextNowIfNeeded( false, false );

                    but.OthersTexts[0].DirectlySetNextText( this.Line.RightText );
                    but.OthersTexts[0].SetTextNowIfNeeded( false, false );

                    this.SetRelatedImage0EnabledIfNeeded( !this.Line.IsFirst ); //all but the first have this

                    this.SetRelatedImage1SpriteIfNeeded( this.Line.Icon?.GetSpriteForUI() );
                    this.SetRelatedImage1ColorFromHexIfNeeded( this.Line.IconColorHex );
                }
            }

            public override void Clear()
            {
                this.Line = UnitStyleNovelTooltipBuffer.StatDataLine.Create();
            }

            public override bool GetShouldBeHidden()
            {
                return this.Line.IsEmpty;
            }
        }
        #endregion
    }
}

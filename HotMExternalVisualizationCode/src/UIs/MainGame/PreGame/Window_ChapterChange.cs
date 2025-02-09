using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ChapterChange : WindowControllerAbstractBase
    {
        public static Window_ChapterChange Instance;
        public Window_ChapterChange()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        public override void OnHideAfterNotShowing()
        {
            if ( hasPlayedSound && SimMetagame.CurrentChapter != null ) //only do this if it has played the sound
                SimMetagame.CurrentChapter.Meta_HasShownBanner = true; //this will mark us as done if something else interrupts this window

            actBGFadeInProgress = 0.3f;
            isFadingOut = false;
            hasPlayedSound = false;
            shouldTopTextBeShown = false;
            shouldBottomTextBeShown = false;

            if ( actBGMat )
                actBGMat.SetFloat( "_Animation_Factor", actBGFadeInProgress );

            base.OnHideAfterNotShowing();
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return VisCurrent.IsShowingChapterChange && SimMetagame.CurrentChapter != null;
        }

        private static Material actBGMat;
        private static float actBGFadeInProgress = 0.3f;
        private static float fadeInSpeed = 1f;
        private static float fadeOutSpeed = 2f;
        private static bool isFadingOut = false;
        private static bool hasPlayedSound = false;
        private static bool shouldTopTextBeShown = false;
        private static bool shouldBottomTextBeShown = false;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {
                if ( !actBGMat )
                {
                    this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                    this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;

                    ArcenUI_CustomUI custom = this.Element as ArcenUI_CustomUI;
                    actBGMat = custom.RelatedImages[0].material;
                    if ( actBGMat )
                        actBGMat.SetFloat( "_Animation_Factor", actBGFadeInProgress );
                }
                else if ( Instance.GetShouldDrawThisFrame_Subclass() )
                {
                    if ( actBGFadeInProgress < 3f && !isFadingOut )
                    {
                        actBGFadeInProgress += ArcenTime.SmoothUnpausedDeltaTime * fadeInSpeed;

                        if ( !hasPlayedSound )
                        {
                            if ( actBGFadeInProgress > 0.6f )
                            {
                                hasPlayedSound = true;
                                shouldTopTextBeShown = true;
                                if ( SimMetagame.CurrentChapter != null )
                                {
                                    if ( SimMetagame.CurrentChapter.TriggerMusicOnStart != null )
                                        ArcenMusicPlayer.Instance.CurrentOneShotOverridingMusicTrackPlaying = SimMetagame.CurrentChapter.TriggerMusicOnStart.MusicListFull.GetRandom( Engine_Universal.PermanentQualityRandom );

                                    if ( SimMetagame.CurrentChapter.OnStartBecomesWeatherStyle != null )
                                        SimCommon.SetWeather( SimMetagame.CurrentChapter.OnStartBecomesWeatherStyle, true );
                                }
                            }
                        }

                        if ( actBGFadeInProgress > 0.6f )
                            shouldBottomTextBeShown = true;

                        if ( actBGFadeInProgress < 2f )
                            actBGMat.SetFloat( "_Animation_Factor", actBGFadeInProgress );
                    }
                    else
                    {
                        isFadingOut = true;

                        actBGFadeInProgress -= ArcenTime.SmoothUnpausedDeltaTime * fadeOutSpeed;
                        actBGMat.SetFloat( "_Animation_Factor", actBGFadeInProgress );

                        if ( actBGFadeInProgress < 0.4f )
                            shouldBottomTextBeShown = false;
                        if ( actBGFadeInProgress < 0.9f )
                            shouldTopTextBeShown = false;

                        if ( actBGFadeInProgress <= 0.3f )
                        {
                            if ( SimMetagame.CurrentChapter != null )
                                SimMetagame.CurrentChapter.Meta_HasShownBanner = true; //this will mark us as done
                        }
                    }
                }
            }
        }

        private static float TimeStartedShowing = 0;
        private static float TimeLastSeenLoading = 0;
        public override void ChildOnShowAfterNotShowing()
        {
            TimeStartedShowing = ArcenTime.AnyTimeSinceStartF;
            TimeLastSeenLoading = ArcenTime.AnyTimeSinceStartF;
        }

        #region tActText
        public class tActText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( shouldTopTextBeShown && SimMetagame.CurrentChapter != null )
                    Buffer.AddRaw( SimMetagame.CurrentChapter.AlienTextNotLocalized );
            }
        }
        #endregion

        #region tActTextSmaller
        public class tActTextSmaller : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( shouldBottomTextBeShown && SimMetagame.CurrentChapter != null )
                    Buffer.AddRaw( SimMetagame.CurrentChapter.Subtitle.Text );
            }
        }
        #endregion
    }
}

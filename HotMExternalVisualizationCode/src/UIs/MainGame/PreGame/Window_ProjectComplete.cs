using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_ProjectComplete : WindowControllerAbstractBase
    {
        public static Window_ProjectComplete Instance;
        public Window_ProjectComplete()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        private static MachineProject ProjectToShow = null;
        private static TimelineGoal GoalToShow = null;
        private static bool hasSetText = false;
        private static float timeHasDrawnText = 0f;
        private static float blockDrawingUntilTime = 0f;

        public override void OnHideAfterNotShowing()
        {
            ProjectToShow = null;
            GoalToShow = null;
            hasSetText = false;
            timeHasDrawnText = 0f;

            base.OnHideAfterNotShowing();
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            return !Window_ChapterChange.Instance.GetShouldDrawThisFrame() && blockDrawingUntilTime < ArcenTime.AnyTimeSinceStartF &&
                ( ProjectToShow != null || !SimCommon.QueuedProjectCompletionFanfare.IsEmpty ||
                 GoalToShow != null || !SimCommon.QueuedGoalCompletionFanfare.IsEmpty);
        }

        private static bool hasInitialized = false;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {
                if ( !hasInitialized )
                {
                    this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                    this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    this.WindowController.Window.FadeInSpeed = 4f;
                    this.WindowController.Window.FadeOutSpeed = 0.5f;
                    hasInitialized = true;
                }
                else if ( Instance.GetShouldDrawThisFrame_Subclass() )
                {
                    if ( ProjectToShow == null && GoalToShow == null )
                    {
                        while ( SimCommon.QueuedProjectCompletionFanfare.TryDequeue( out ProjectToShow ) )
                        {
                            if ( ProjectToShow == null || ProjectToShow.DuringGame_HasShownFanfareSinceLoad )
                                continue;

                            //play the sound
                            ProjectToShow.DuringGame_ActualOutcome?.OnComplete?.DuringGame_Play();
                            ProjectToShow.DuringGame_HasShownFanfareSinceLoad = true;
                            break;
                        }
                    }

                    if ( ProjectToShow == null && GoalToShow == null )
                    {
                        if ( SimCommon.QueuedGoalCompletionFanfare.TryDequeue( out GoalToShow ) )
                        {
                            //play the sound
                            GoalToShow?.OnComplete?.DuringGame_Play();
                            SimCommon.MarkGoalScreenForBeingShownIfNotAlready();
                        }
                    }

                    if ( ProjectToShow != null )
                    {
                        if ( !hasSetText )
                        {
                            if ( tUpperText.Instance?.Element != null && tLowerText.Instance?.Element != null )
                            {
                                ArcenUIWrapperedTMProText text = (tUpperText.Instance.Element as ArcenUI_Text).Text;
                                text.DirectlySetNextText( ProjectToShow.GetDisplayName() );
                                text.SetTextNowIfNeeded( true, true );

                                text = (tLowerText.Instance.Element as ArcenUI_Text).Text;
                                text.DirectlySetNextText( Lang.Get( "ProjectCompleted" ) );
                                text.SetTextNowIfNeeded( true, true );
                                hasSetText = true;
                                timeHasDrawnText = 0;
                            }
                        }
                    }

                    if ( GoalToShow != null )
                    {
                        if ( !hasSetText )
                        {
                            if ( tUpperText.Instance?.Element != null && tLowerText.Instance?.Element != null )
                            {
                                ArcenUIWrapperedTMProText text = (tUpperText.Instance.Element as ArcenUI_Text).Text;
                                text.DirectlySetNextText( GoalToShow.GetDisplayName() );
                                text.SetTextNowIfNeeded( true, true );

                                text = (tLowerText.Instance.Element as ArcenUI_Text).Text;
                                text.DirectlySetNextText( Lang.Get( "TimelineGoalCompleted" ) );
                                text.SetTextNowIfNeeded( true, true );
                                hasSetText = true;
                                timeHasDrawnText = 0;
                            }
                        }
                    }

                    if ( hasSetText )
                    {
                        timeHasDrawnText += ArcenTime.SmoothUnpausedDeltaTime;
                        if ( timeHasDrawnText > 2f )
                        {
                            ProjectToShow = null; //start us fading out
                            GoalToShow = null; //start us fading out
                            blockDrawingUntilTime = ArcenTime.AnyTimeSinceStartF + 2f;
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

        #region tUpperText
        public class tUpperText : TextAbstractBase
        {
            public static tUpperText Instance;
            public tUpperText()
            {
                Instance = this;
            }
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
            }
        }
        #endregion

        #region tLowerText
        public class tLowerText : TextAbstractBase
        {
            public static tLowerText Instance;
            public tLowerText()
            {
                Instance = this;
            }
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
            }
        }
        #endregion
    }
}

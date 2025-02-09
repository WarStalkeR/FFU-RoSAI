using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;
using System.Xml.Serialization;

namespace Arcen.HotM.ExternalVis
{
    public class Window_SimpleChoiceWindow : WindowControllerAbstractBase, IInputActionHandler
    {
        public static Window_SimpleChoiceWindow Instance;

        public Window_SimpleChoiceWindow()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( SimCommon.CurrentSimpleChoice == null )
                return false;
            if ( VisCurrent.IsShowingTransitionCamera )
                return false; //if this is still transitioning too much, then don't show this window yet
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;

            return true;
        }

        #region ExtraData And AltChoices
        private static bool IsShowingPostChoiceResults = false;
        private static ArcenDoubleCharacterBuffer ExtraBodyTextBuffer = new ArcenDoubleCharacterBuffer( "Window_SimpleChoiceWindow-ExtraBodyTextBuffer" );
        private static string ExtraBodyText = string.Empty;

        private static IList<ISimpleChoice> choices;

        private static void ClearAllExtraData()
        {
            IsShowingPostChoiceResults = false;
            ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
            ExtraBodyText = string.Empty;
        }

        private static readonly List<ISimpleChoice> altChoices = List<ISimpleChoice>.Create_WillNeverBeGCed( 6, "Window_SimpleChoiceWindow-altChoices" );
        private static IList<ISimpleChoice> GetAltChoices()
        {
            altChoices.Clear();
            if ( IsShowingPostChoiceResults )
            {
                altChoices.Add( SimpleTextOnlyChoiceOption.Create( Choice_DoClose,
                    LocalizedString.AddLang_New( "Popup_Common_Close" ),
                    LocalizedString.AddRaw_New( string.Empty ), true, true, true ) );
            }
            return altChoices;
        }

        private static void Choice_DoClose( ChoiceClickStyle ClickStyle, out PostChoiceResult PostResult )
        {
            PostResult = PostChoiceResult.CloseWindow;
            SimCommon.CurrentSimpleChoice = null;
        }
        #endregion ExtraData And AltChoices

        public override void OnHideAfterNotShowing()
        {
            ClearAllExtraData();
        }

        #region tUpperTitle
        public class tUpperTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                SimCommon.CurrentSimpleChoice?.RenderUpperTitleText( Buffer );
            }
        }
        #endregion

        #region tLowerTitle
        public class tLowerTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                SimCommon.CurrentSimpleChoice?.RenderLowerTitleText( Buffer );
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null && TooltipLinkData != null && TooltipLinkData.Length > 0 )
                {
                    cProvider.BodyText_HandleHyperlinkHover( TooltipLinkData );
                }
                else
                    TooltipRefs.AtMouseTag.ClearAllImmediately();

            }

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null )
                {
                    return cProvider.BodyText_HandleHyperlinkClick( Input, TooltipLinkData );
                }
                else
                    return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tBodyText
        public class tBodyText : TextAbstractBase
        {
            public static ArcenUIWrapperedTMProText WrapperedText;

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( WrapperedText == null )
                    WrapperedText = Tex.Text;
                WrapperedText.CalculateHeightFromContentsButDoNotUpdate = true;

                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null )
                {
                    if ( ExtraBodyText != null && ExtraBodyText.Length > 0 ) //use this instead of the normal body text, not in addition to, for now
                    {
                        Buffer.LineIfLastWrittenWasNotLine();
                        Buffer.AddRaw( ExtraBodyText );
                    }
                    else
                        cProvider.RenderBodyText( Buffer );
                }
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null && TooltipLinkData != null && TooltipLinkData.Length > 0 )
                {
                    cProvider.BodyText_HandleHyperlinkHover( TooltipLinkData );
                }
                else
                    TooltipRefs.AtMouseTag.ClearAllImmediately();
            }

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null )
                {
                    return cProvider.BodyText_HandleHyperlinkClick( Input, TooltipLinkData );
                }
                else
                    return MouseHandlingResult.None;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bChoice> bChoicePool;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_CentralChoicePopup" );

                float offsetFromSide = 0;
                if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                    offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                else //the sidebar is on, and is on the left, move right
                    offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                this.WindowController.ExtraOffsetX = offsetFromSide;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bChoice.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bChoicePool = new ButtonAbstractBase.ButtonPool<bChoice>( bChoice.Original, 10, "bChoice" );

                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                this.UpdateButtons();
            }

            private const float HEIGHT_FROM_OTHER_BITS = 52f; //header plus buffer
            private const float BUTTON_WIDTH = 440f;
            private const float BUTTON_HEIGHT = 25.7f;
            private const float HEIGHT_PER_BUTTON = BUTTON_HEIGHT + 3f;
            private const float BUTTON_STARTING_Y = -3.47f;
            private const float BUTTON_X = -2f;            

            public void UpdateButtons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bChoicePool.Clear( 5 );

                int buttonCount = 0;

                ISimpleChoiceProvider cProvider = SimCommon.CurrentSimpleChoice;
                if ( cProvider != null )
                {
                    float buttonY = BUTTON_STARTING_Y;
                    float buttonX = BUTTON_X;
                    if ( IsShowingPostChoiceResults )
                        choices = GetAltChoices();
                    else
                        choices = cProvider.GetChoices();

                    for ( int i = choices.Count - 1; i >= 0; i-- ) //iterate in reverse order, as we are building from the bottom up
                    {
                        ISimpleChoice choice = choices[i];
                        int choiceIndex = i;
                        bChoice icon = bChoicePool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                        if ( icon != null )
                        {
                            icon.ApplyItemInPositionNoTextSizing( ref buttonX, ref buttonY, false, false, BUTTON_WIDTH, BUTTON_HEIGHT, IgnoreSizeOption.IgnoreSize );
                            icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                            {
                                switch ( Action )
                                {
                                    case UIAction.GetTextToShowFromVolatile:
                                        {
                                            bool isBeingHoveredNow = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isBeingHoveredNow = true;
                                            }

                                            bool isPurple = choice.GetIsPurple();
                                            choice.RenderButtonText( ExtraData.Buffer, choice.GetIsPurple() ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) :
                                                ColorTheme.GetBasicLightTextBlue( isBeingHoveredNow ) );
                                            icon.SetRelatedImage0SpriteIfNeeded( icon.Element.RelatedSprites[isPurple ? 0 : 1] );
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            if ( !choice.RenderTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard ) )
                                                TooltipRefs.AtMouseTag.ClearAllImmediately();
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            ExtraBodyTextBuffer.EnsureResetForNextUpdate();
                                            choice.TryClick( ChoiceClickStyle.InitialAttempt, out PostChoiceResult PostResult, ExtraBodyTextBuffer );
                                            switch ( PostResult )
                                            {
                                                case PostChoiceResult.WasBlocked:
                                                    ClearAllExtraData();
                                                    break; //nothing to do
                                                case PostChoiceResult.CloseWindow:
                                                    ClearAllExtraData();
                                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                                    break;
                                                case PostChoiceResult.ShowAfterChoiceReport:
                                                    IsShowingPostChoiceResults = true;
                                                    ExtraBodyText = ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
                                                    break;
                                            }
                                        }
                                        break;
                                }
                            } );

                            buttonY += HEIGHT_PER_BUTTON;
                            buttonCount++;
                        }
                    }
                }

                if ( cProvider == null || buttonCount == 0 || (SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false) )
                    Instance.Close( WindowCloseReason.ShowingRefused ); //something is off

                #region Expand or Height Width Of This Window
                float heightForWindow = HEIGHT_FROM_OTHER_BITS + (HEIGHT_PER_BUTTON * buttonCount) + (tBodyText.WrapperedText?.LastHeightSet ?? 0);
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion
            }
            private float lastWindowHeight = -1f;
        }

        #region bChoice
        public class bChoice : ButtonAbstractBaseWithImage
        {
            public static bChoice Original;
            public bChoice() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                if ( this.Element is ArcenUI_Button but )
                    but.ClickSoundEffect = string.Empty; //make sure that we don't get the regular sound effects, as we want to play custom ones

                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void Clear()
            {
                this.UIDataController = null;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = null;
            }

            public override bool GetShouldBeHidden()
            {
                return this.UIDataController == null;
            }
        }
        #endregion

        public void Handle( int Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Cancel":
                    if ( choices != null && choices.Count > 0 )
                    {
                        foreach ( ISimpleChoice choice in choices )
                        {
                            if ( choice.GetShouldBeTriggeredByEscape() || choices.Count == 1 )
                            {
                                choice.TryClick( ChoiceClickStyle.InitialAttempt, out _, null );
                                break;
                            }
                        }
                    }
                    break;
                case "JumpToNextActorOrEndTurn":
                    if ( choices != null && choices.Count > 0 )
                    {
                        foreach ( ISimpleChoice choice in choices )
                        {
                            if ( choice.GetShouldBeTriggeredBySpacebar() || choices.Count == 1 )
                            {
                                choice.TryClick( ChoiceClickStyle.InitialAttempt, out _, null );
                                break;
                            }
                        }
                    }
                    break;
            }

            if ( InputWindowCutthrough.HandleKey( InputActionType.ID ) )
                return;
        }
    }
}

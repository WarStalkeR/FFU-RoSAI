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
    public class Window_RewardWindow : ToggleableWindowController, IInputActionHandler
    {
        public static Window_RewardWindow Instance;

        public Window_RewardWindow()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
        }

        public override void Close( WindowCloseReason Reason )
        {
            //ArcenDebugging.LogWithStack( "Window_RewardWindow: Reason: " + Reason, Verbosity.DoNotShow );

            switch (Reason)
            {
                case WindowCloseReason.ShowingRefused:
                case WindowCloseReason.OtherWindowCausingClose:
                    return; //don't clean anything up
            }

            IRewardOptionProvider rewardProvider = SimCommon.RewardProvider;
            if ( rewardProvider != null )
            {
                if ( rewardProvider.GetAllowsCasualClosing() )
                    FullClose( Reason );
                else
                {
                    ExtraBodyTextBuffer.EnsureResetForNextUpdate();
                    IRewardOption choice = rewardProvider.TryAlternativeToCasualClosing( out PostRewardResult PostResult, ExtraBodyTextBuffer );
                    if ( choice != null )
                        HandlePostClickLogic( choice, PostResult );
                }
            }
        }

        public void CloseForSure( WindowCloseReason Reason )
        {
            if ( SimCommon.OpenWindowRequest == OpenWindowRequest.Reward )
                return; //something else is opening this

            SimCommon.RewardProvider = null; //do this to prevent repeat submissions on actual close
            FullClose( Reason );
        }

        #region ExtraData And AltRewards
        private static bool IsShowingPostRewardResults = false;
        private static ArcenDoubleCharacterBuffer ExtraBodyTextBuffer = new ArcenDoubleCharacterBuffer( "Window_RewardWindow-ExtraBodyTextBuffer" );
        private static string ExtraBodyText = string.Empty;

        private static void ClearAllExtraData()
        {
            IsShowingPostRewardResults = false;
            ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
            ExtraBodyText = string.Empty;
        }

        private static readonly List<IRewardOption> altRewards = List<IRewardOption>.Create_WillNeverBeGCed( 6, "Window_RewardWindow-altRewards" );
        private static IList<IRewardOption> GetAltRewards()
        {
            altRewards.Clear();
            if ( IsShowingPostRewardResults )
            {
                altRewards.Add( TextOnlyRewardOption.Create( Choice_DoClose,
                    LocalizedString.AddLang_New( "Popup_Common_Close" ),
                    LocalizedString.AddRaw_New( string.Empty ),
                    LocalizedString.AddRaw_New( string.Empty ), true, null ) );
            }
            return altRewards;
        }

        private static void Choice_DoClose( out PostRewardResult PostResult )
        {
            PostResult = PostRewardResult.OnlyCloseWindow;
        }
        #endregion ExtraData And AltRewards

        public override void OnHideAfterNotShowing()
        {
            ClearAllExtraData();
        }

        public static void HandlePostClickLogic( IRewardOption choice, PostRewardResult PostResult )
        {
            switch ( PostResult )
            {
                case PostRewardResult.WasBlocked:
                    ClearAllExtraData();
                    break; //nothing to do
                case PostRewardResult.OnlyCloseWindow:
                    ClearAllExtraData();
                    Instance.CloseForSure( WindowCloseReason.UserDirectRequest );
                    break;
                case PostRewardResult.SuccessAndClose:
                    SimCommon.RewardProvider?.DoAnyActionAfterSuccess( choice );
                    ClearAllExtraData();
                    Instance.CloseForSure( WindowCloseReason.UserDirectRequest );
                    break;
                case PostRewardResult.ShowAfterRewardReport:
                    IsShowingPostRewardResults = true;
                    ExtraBodyText = ExtraBodyTextBuffer.GetStringAndResetForNextUpdate();
                    break;
            }
        }

        #region tUpperTitle
        public class tUpperTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                SimCommon.RewardProvider?.RenderUpperTitleText( Buffer );
            }
        }
        #endregion

        #region tLowerTitle
        public class tLowerTitle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                SimCommon.RewardProvider?.RenderLowerTitleText( Buffer );
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null && TooltipLinkData != null && TooltipLinkData.Length > 0 )
                {
                    rProvider.BodyText_HandleHyperlinkHover( TooltipLinkData );
                }
                else
                    TooltipRefs.AtMouseTag.ClearAllImmediately();

            }

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null )
                {
                    return rProvider.BodyText_HandleHyperlinkClick( Input, TooltipLinkData );
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

                Buffer.StartColor( ColorTheme.NarrativeColor );

                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null )
                {
                    if ( ExtraBodyText != null && ExtraBodyText.Length > 0 ) //use this instead of the normal body text, not in addition to, for now
                    {
                        Buffer.LineIfLastWrittenWasNotLine();
                        Buffer.AddRaw( ExtraBodyText );
                    }
                    else
                        rProvider.RenderBodyText( Buffer );
                }
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null && TooltipLinkData != null && TooltipLinkData.Length > 0 )
                {
                    rProvider.BodyText_HandleHyperlinkHover( TooltipLinkData );
                }
                else
                    TooltipRefs.AtMouseTag.ClearAllImmediately();

            }

            public override MouseHandlingResult HandleHyperlinkClick( MouseHandlingInput Input, string[] TooltipLinkData )
            {
                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null )
                {
                    return rProvider.BodyText_HandleHyperlinkClick( Input, TooltipLinkData );
                }
                else
                    return MouseHandlingResult.None;
            }
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bRewardOption> bRewardOptionPool;

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
                    Instance.CloseForSure( WindowCloseReason.OtherWindowCausingClose );
                    return;
                }
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_CentralChoicePopup" );

                //float offsetFromSide = 0;
                //if ( !Engine_Universal.PrimaryIsLeft ) //the sidebar is on, move left
                //    offsetFromSide -= Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();
                //else //the sidebar is on, and is on the left, move right
                //    offsetFromSide += Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled();

                //this.WindowController.ExtraOffsetX = offsetFromSide;

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bRewardOption.Original != null )
                    {
                        hasGlobalInitialized = true;
                        bRewardOptionPool = new ButtonAbstractBase.ButtonPool<bRewardOption>( bRewardOption.Original, 10, "bRewardOption" );

                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                this.UpdateButtons();
            }

            private const float HEIGHT_FROM_OTHER_BITS = 52f; //header plus buffer
            private const float BUTTON_WIDTH = 440f;
            private const float BUTTON_HEIGHT = 35.2f;
            private const float HEIGHT_PER_BUTTON = BUTTON_HEIGHT + 2f;
            private const float HEIGHT_PER_BUTTON_OUTER = HEIGHT_PER_BUTTON + 0.1f;
            private const float BUTTON_STARTING_Y = -22f;
            private const float BUTTON_X = -2f;            

            public void UpdateButtons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bRewardOptionPool.Clear( 5 );

                int buttonCount = 0;

                IRewardOptionProvider rProvider = SimCommon.RewardProvider;
                if ( rProvider != null )
                {
                    float buttonY = BUTTON_STARTING_Y;
                    float buttonX = BUTTON_X;
                    IList<IRewardOption> choices;
                    if ( IsShowingPostRewardResults )
                        choices = GetAltRewards();
                    else
                        choices = rProvider.GetRewardOptions();

                    for ( int i = choices.Count - 1; i >= 0; i-- ) //iterate in reverse order, as we are building from the bottom up
                    {
                        IRewardOption choice = choices[i];
                        int choiceIndex = i;
                        bRewardOption icon = bRewardOptionPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
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
                                            choice.RenderRewardButtonTopLineText( ExtraData.Buffer, choice.GetIsPurple() ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) :
                                                ColorTheme.GetBasicLightTextBlue( isBeingHoveredNow ) );
                                            icon.SetRelatedImage0SpriteIfNeeded( icon.Element.RelatedSprites[isPurple ? 0 : 1] );
                                        }
                                        break;
                                    case UIAction.GetOtherTextToShowFromVolatile:
                                        {
                                            bool isBeingHoveredNow = false;
                                            if ( element is ArcenUI_Button but )
                                            {
                                                if ( but.LastHadMouseWithin )
                                                    isBeingHoveredNow = true;
                                            }
                                            switch ( ExtraData.Int )
                                            {
                                                case 0:
                                                    choice.RenderRewardButtonIconText( ExtraData.Buffer, choice.GetIsPurple() ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) :
                                                        ColorTheme.GetBasicLightTextBlue( isBeingHoveredNow ) );
                                                    break;
                                                case 1:
                                                    choice.RenderRewardButtonSecondLineText( ExtraData.Buffer, choice.GetIsPurple() ? ColorTheme.GetBasicSecondLineTextPurple( isBeingHoveredNow ) :
                                                        ColorTheme.GetBasicSecondLineTextBlue( isBeingHoveredNow ) );
                                                    break;
                                                default:
                                                    ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                                    break;
                                            }
                                        }
                                        break;
                                    case UIAction.HandleMouseover:
                                        {
                                            choice.RenderRewardTooltipText( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard );
                                        }
                                        break;
                                    case UIAction.OnClick:
                                        {
                                            if ( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.RightButtonClicked )
                                            {
                                                ExtraBodyTextBuffer.EnsureResetForNextUpdate();
                                                choice.TryRewardClick( out PostRewardResult PostResult, ExtraBodyTextBuffer, ExtraData.MouseInput.RightButtonClicked );
                                                HandlePostClickLogic( choice, PostResult );
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

                if ( rProvider == null || buttonCount == 0 || (SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false) )
                    Instance.CloseForSure( WindowCloseReason.OtherWindowCausingClose ); //something is off

                #region Expand or Height Width Of This Window
                float heightForWindow = HEIGHT_FROM_OTHER_BITS + (HEIGHT_PER_BUTTON_OUTER * buttonCount) + (tBodyText.WrapperedText?.LastHeightSet ?? 0);
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

        #region bRewardOption
        public class bRewardOption : ButtonAbstractBaseWithImage
        {
            public static bRewardOption Original;
            public bRewardOption() { if ( Original == null ) Original = this; }

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
                case "JumpToNextActorOrEndTurn":
                case "GoStraightToNextTurn":
                    {
                        IRewardOptionProvider rewardProvider = SimCommon.RewardProvider;
                        if ( rewardProvider != null )
                        {
                            ExtraBodyTextBuffer.EnsureResetForNextUpdate();
                            IRewardOption choice = rewardProvider.TryAlternativeToCasualClosing( out PostRewardResult PostResult, ExtraBodyTextBuffer );
                            if ( choice != null )
                                HandlePostClickLogic( choice, PostResult );
                        }
                    }
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }
}

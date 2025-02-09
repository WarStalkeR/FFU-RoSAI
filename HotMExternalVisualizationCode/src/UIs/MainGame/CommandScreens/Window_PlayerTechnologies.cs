using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.Universal;
using System;
using System.Linq.Dynamic;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_PlayerTechnologies : ToggleableWindowController, IInputActionHandler
    {
        #region Main Controller
        public static Window_PlayerTechnologies Instance;
        public override bool PutMeOnTheEscapeCloseStack => true;
		public Window_PlayerTechnologies()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
        }

        public override void OnOpen()
        {

            base.OnOpen();
        }

        private static int lastYHeightOfInterior = 0;
        private static int DisplayedTechEntries = 0;

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase    
        {
            public customParent()
            {
                Window_PlayerTechnologies.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private static ButtonAbstractBase.ButtonPool<bTechOption> bTechOptionPool;

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                if ( VisCurrent.IsUIHiddenExceptForSidebar )
                {
                    Instance.Close( WindowCloseReason.ShowingRefused );
                    return;
                }
                if ( Instance != null )
                {
                    #region Global Init
                    if ( !hasGlobalInitialized )
                    {
                        //keeps it from having visual glitches, but uses more GPU.  That's fine, this is the only window open at that time.
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0f;
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0f;

                        if ( bTechOption.Original != null )
                        {
                            hasGlobalInitialized = true;
                            bTechOptionPool = new ButtonAbstractBase.ButtonPool<bTechOption>( bTechOption.Original, 10, "bTechOption" );
                        }
                    }
                    #endregion

                    OnUpdate_Content();

                    #region Expand or Shrink Size Of This Window
                    int newHeight = 58 + MathA.Min( lastYHeightOfInterior, 400 );
                    if ( heightToShow != newHeight )
                    {
                        heightToShow = newHeight;
                        this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0.5f );
                        this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0.5f );
                        this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0.5f );
                        this.Element.RelevantRect.UI_SetHeight( heightToShow );
                    }
                    #endregion
                }
            }

            private int heightToShow = 0;

            private float maxYToShow;
            private float minYToShow;
            private float runningY;

            private const float MAX_VIEWPORT_SIZE = 480; //it's actually 420, but let's have some extra room
            private const float EXTRA_BUFFER = 800; //this keeps it so that scrolling looks a lot nicer, while not letting this have infinite load

            private void OnUpdate_Content()
            {
                bTechOptionPool.Clear( 60 );

                RectTransform rTran = (RectTransform)bTechOption.Original.Element.RelevantRect.parent;

                maxYToShow = -rTran.anchoredPosition.y;
                minYToShow = maxYToShow - MAX_VIEWPORT_SIZE - EXTRA_BUFFER;
                maxYToShow += EXTRA_BUFFER;

                runningY = -4.200127f;
                OnUpdate_Content_AvailableResearch();

                #region Positioning Logic
                //Now size the parent, called Content, to get scrollbars to appear if needed.
                Vector2 sizeDelta = rTran.sizeDelta;
                sizeDelta.y = MathA.Abs( runningY );
                rTran.sizeDelta = sizeDelta;
                #endregion

                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }

            #region CalculateBoundsSingle
            protected void CalculateBoundsSingle( out Rect soleBounds, ref float innerY, bool IsTall )
            {
                soleBounds = ArcenFloatRectangle.CreateUnityRect( 0f, innerY, 200f, 35.2f );

                innerY -= ( 35.2f + 1.5f );
            }
            #endregion

            #region OnUpdate_Content_AvailableResearch
            private void OnUpdate_Content_AvailableResearch()
            {
                int displayedEntries = 0;
                foreach ( ResearchDomain domain in ResearchDomainTable.Instance.Rows )
                {
                    if ( !domain.DuringGame_ShouldBeVisible )
                        continue;

                    displayedEntries++;

                    #region Regular Unlock Option
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bTechOption row = bTechOptionPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bTechOptionPool.ApplySingleItemInRow( row, bounds, false );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool canInvent = domain.DuringGame_ShouldBeEnabled;
                        int turnsToInvent = domain.GetRemainingTurnsToResearch();

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

                                    row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[canInvent ? 0 : 2] );

                                    ExtraData.Buffer.AddRaw( domain.GetDisplayName(),
                                        canInvent ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) : ColorTheme.GetRedOrange2( isBeingHoveredNow ) );
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
                                            ExtraData.Buffer.AddSpriteStyled_NoIndent( domain.Icon, AdjustedSpriteStyle.InlineSmaller095,
                                                canInvent ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) : ColorTheme.GetRedOrange2( isBeingHoveredNow ) );
                                            break;
                                        case 1:
                                            ExtraData.Buffer.AddFormat1( "InspirationCount", domain.DuringGame_CurrentPointsOfInspiration,
                                                ColorTheme.GetBasicSecondLineTextPurple( isBeingHoveredNow ) );
                                            break;
                                        case 2:
                                            {
                                                string turnsColor = canInvent ? ColorTheme.RustLighter : ColorTheme.RedOrange;
                                                ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                                    .AddRaw( turnsToInvent.ToStringWholeBasic(), turnsColor );
                                            }
                                            break;
                                        default:
                                            ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    domain.HandleResearchDomainTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, false );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( !domain.DuringGame_ShouldBeEnabled )
                                    {
                                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                        break;
                                    }
                                    UnlockCoordinator.CurrentUnlockResearch = null;
                                    UnlockCoordinator.CurrentResearchDomain = domain;
                                    //TechCompletedAfterResearchToast.ClearAllTechToasts();
                                    if ( turnsToInvent <= 0 )
                                    {
                                        //hey, we immediately have enough to do this!
                                        domain.FinishComingUpWithNewIdea();
                                    }
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                    #endregion
                }

                foreach ( Unlock unlock in UnlockTable.Instance.Rows )
                {
                    if ( !unlock.GetCompoundShowOnResearchScreen() )
                        continue; //this prevents bleed-through in the first second after something new is invented

                    displayedEntries++;

                    #region Regular Unlock Option
                    this.CalculateBoundsSingle( out Rect bounds, ref runningY, false );
                    if ( bounds.yMax < minYToShow )
                        continue; //it's scrolled up far enough we can skip it, yay!
                    if ( bounds.yMax > maxYToShow )
                        continue; //this is below where we are scrolled, so let's skip this, too!  We won't break out, because we need runningY to be fully calculated

                    bTechOption row = bTechOptionPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                    if ( row == null )
                        continue;
                    bTechOptionPool.ApplySingleItemInRow( row, bounds, false );

                    row.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        bool canInvent = unlock.GetCompoundCanBeInventedNow();
                        int turnsToInvent = unlock.GetRemainingTurnsToResearch();

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

                                    row.SetRelatedImage0SpriteIfNeeded( row.Element.RelatedSprites[canInvent ? 0 : 2] );

                                    ExtraData.Buffer.AddRaw( unlock.GetDisplayName(),
                                        canInvent ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) : ColorTheme.GetRedOrange2( isBeingHoveredNow ) );
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
                                            ExtraData.Buffer.AddSpriteStyled_NoIndent( unlock.GetIcon(), AdjustedSpriteStyle.InlineSmaller095,
                                                canInvent ? ColorTheme.GetBasicLightTextPurple( isBeingHoveredNow ) : ColorTheme.GetRedOrange2( isBeingHoveredNow ) );
                                            break;
                                        case 1:
                                            {
                                                string colorHex = ColorTheme.GetBasicSecondLineTextPurple( isBeingHoveredNow );
                                                unlock.RenderUnlockNameAndPrefix( ExtraData.Buffer, colorHex, colorHex );
                                            }
                                            break;
                                        case 2:
                                            {
                                                string turnsColor = canInvent ? ColorTheme.RustLighter : ColorTheme.RedOrange;
                                                ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, turnsColor )
                                                    .AddRaw( turnsToInvent.ToStringWholeBasic(), turnsColor );
                                            }
                                            break;
                                        default:
                                            ArcenDebugging.LogSingleLine( "GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    unlock.RenderUnlockTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.TightDark, TooltipInstruction.ForUnlock, TooltipExtraText.None );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( !unlock.GetCompoundCanBeInventedNow() )
                                    {
                                        ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                        break;
                                    }
                                    UnlockCoordinator.CurrentUnlockResearch = unlock;
                                    UnlockCoordinator.CurrentResearchDomain = null;
                                    //TechCompletedAfterResearchToast.ClearAllTechToasts();
                                    if ( turnsToInvent <= 0 )
                                    {
                                        //hey, we immediately have enough to do this!
                                        unlock.DuringGameplay_FinishInventing();
                                    }
                                    Instance.Close( WindowCloseReason.UserDirectRequest );
                                }
                                break;
                        }
                    } );
                    #endregion
                }

                DisplayedTechEntries = displayedEntries;
            }
            #endregion
        }
        #endregion

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Technology_Research" );
            }
        }
        #endregion

        #region tChooseLabel
        public class tChooseLabel : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( DisplayedTechEntries > 0 )
                    Buffer.AddLangAndAfterLineItemHeader( "Technology_Research_Instructions" );
                else
                    Buffer.AddLang( "Technology_Research_OutOfIdeas" );
            }
        }
        #endregion

        #region bTechOption
        public class bTechOption : ButtonAbstractBaseWithImage
        {
            public static bTechOption Original;
            public bTechOption() { if ( Original == null ) Original = this; }

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

        #region Supporting Elements
        //not actually needed at this time, but needed for compilation
        public class bMainContentParent : CustomUIAbstractBase
        {
            public static Transform ParentT;
            public static RectTransform ParentRT;
            public override void OnUpdate()
            {
                if ( ParentT == null )
                {
                    ParentT = this.Element.transform;
                    ParentRT = (RectTransform)ParentT;
                }
            }
        }

        public void Handle( Int32 Int1, InputActionTypeData InputActionType )
        {
            switch ( InputActionType.ID )
            {
                case "Return":
                    Instance.Close( WindowCloseReason.UserDirectRequest );
                    break;
                default:
                    VisCommands.HandleMajorWindowKeyPress( InputActionType );
                    break;
            }
        }
        #endregion

        #region bExit
        public class bExit : ButtonAbstractBaseWithImage
        {
            public static bExit Original;
            public bExit() { if ( Original == null ) Original = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion
    }
}

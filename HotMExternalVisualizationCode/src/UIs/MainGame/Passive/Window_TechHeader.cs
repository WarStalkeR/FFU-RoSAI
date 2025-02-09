using System;
using Arcen.Universal;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class Window_TechHeader : WindowControllerAbstractBase
    {
        public static Window_TechHeader Instance;
		
		public Window_TechHeader()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
            this.ShowEvenWhenSomethingElseTryingToMakeAllOtherWindowsNotShow = true;
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

		public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                return false; //definitely do not draw, in that case
            if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                return false;
            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                return false;
            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                return false;
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null && lowerMode.HideTechnologySectionOfLeftMenu )
                return false;
            if ( Window_MainGameHeaderBarLeft.GetIsOpeningSubMenuBlocked( true ) )
                return false;
            if ( Window_BuildSidebar.Instance.GetShouldDrawThisFrame() )
                return false;

            if ( UnlockCoordinator.CurrentUnlockResearch == null )
            {
                if ( UnlockCoordinator.CurrentResearchDomain != null )
                { } //all good
                else
                {
                    if ( UnlockCoordinator.GetHasAnyFullyReadyResearch() )
                    { } //all good
                    else
                        return false; //all is blocked or not there at all
                }
            }

            return true;
        }

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            if ( !Instance.GetShouldDrawThisFrame_Subclass() )
                return 0f;
            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            height *= 1.07f; //add buffer 
            return height;
        }
        #endregion

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_HeaderBar" ) * 0.98f;
                this.WindowController.ExtraOffsetYRaw = Window_MainGameHeaderBarLeft.GetYHeightForOtherWindowOffsets();
            }      
        }

        #region bTechnologyIcon
        public class bTechnologyIcon : ButtonAbstractBaseWithImage
        {
            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return DoTechnologyClick( input );
            }

            public static MouseHandlingResult DoTechnologyClick( MouseHandlingInput input )
            {
                VisCommands.ToggleMajorWindowMode( MajorWindowMode.Research );
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                //no text
            }

            public override void OnUpdateSubSub()
            {
                if ( UnlockCoordinator.CurrentUnlockResearch != null )
                {
                    this.SetSpriteIfNeeded( UnlockCoordinator.CurrentUnlockResearch.GetIcon().GetSpriteForUI() );
                    this.SetImageColorFromHexIfNeeded( string.Empty );
                }
                else
                {
                    if ( UnlockCoordinator.CurrentResearchDomain != null )
                    {
                        this.SetSpriteIfNeeded( UnlockCoordinator.CurrentResearchDomain.Icon.GetSpriteForUI() );
                        this.SetImageColorFromHexIfNeeded( string.Empty );
                    }
                    else
                    {
                        this.SetSpriteIfNeeded( IconRefs.NoTech.Icon.GetSpriteForUI() );
                        if ( UnlockCoordinator.GetHasAnyFullyReadyResearch() )
                            this.SetImageColorFromHexIfNeeded( string.Empty );
                        else
                            this.SetImageColorFromHexIfNeeded( ColorTheme.DisabledPurpleUI );
                    }
                }
            }

            public static void DoTechnologyIconMouseover( ArcenUI_Element element )
            {
                if ( SimCommon.CurrentEvent != null )
                    return;

                if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                    return;

                if ( UnlockCoordinator.CurrentUnlockResearch != null )
                {
                    UnlockCoordinator.CurrentUnlockResearch.RenderUnlockTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForUnlock, TooltipExtraText.None );
                }
                else
                {
                    if ( UnlockCoordinator.CurrentResearchDomain != null )
                        UnlockCoordinator.CurrentResearchDomain.HandleResearchDomainTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, false );
                    else
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderLeft", "Research" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "Research_Header" );
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleResearch" );
                            novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "PleaseChooseANewIdea_Details" ).Line();
                        }
                    }
                }
            }

            public override void HandleMouseover()
            {
                DoTechnologyIconMouseover( this.Element );
            }
        }
        #endregion

        #region tTechName
        public class tTechName : TextAbstractBase
        {
            public static tTechName Instance;
            public tTechName() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                    return;

                if ( UnlockCoordinator.CurrentUnlockResearch == null )
                {
                    if ( UnlockCoordinator.CurrentResearchDomain != null )
                        Buffer.AddRaw( UnlockCoordinator.CurrentResearchDomain.GetDisplayName() );
                    else
                    {
                        if ( UnlockCoordinator.GetHasAnyFullyReadyResearch() )
                            Buffer.AddLang( "ChooseResearch" );
                    }
                }
                else
                    Buffer.AddRaw( UnlockCoordinator.CurrentUnlockResearch.GetDisplayName() );
            }

            public static void DoTechnologyBarMouseover( ArcenUI_Element element )
            {
                if ( SimCommon.CurrentEvent != null )
                    return;

                if ( !FlagRefs.HasFiguredOutResearch.DuringGameplay_IsTripped )
                    return;

                if ( UnlockCoordinator.CurrentUnlockResearch != null )
                {
                    UnlockCoordinator.CurrentUnlockResearch.RenderUnlockTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, TooltipInstruction.ForUnlock, TooltipExtraText.None );
                }
                else if ( UnlockCoordinator.CurrentResearchDomain != null )
                {
                    UnlockCoordinator.CurrentResearchDomain.HandleResearchDomainTooltip( element, SideClamp.AboveOrBelow, TooltipShadowStyle.None, false );
                }
                else
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "HeaderLeft", "Research" ), element, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                    {
                        novel.TitleUpperLeft.AddLang( "Research_Header" );
                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleResearch" );
                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "PleaseChooseANewIdea_Details" ).Line();
                    }
                }
            }

            public override void HandleMouseover()
            {
                DoTechnologyBarMouseover( this.Element );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return bTechnologyIcon.DoTechnologyClick( input );
            }

            public override bool GetShouldBeHidden()
            {
                return false;
            }
        }
        #endregion

        #region tTechProgressTurns
        public class tTechProgressTurns : TextAbstractBase
        {
            public static tTechProgressTurns Instance;
            public tTechProgressTurns() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( UnlockCoordinator.CurrentUnlockResearch != null )
                {
                    int turnsToInvent = UnlockCoordinator.CurrentUnlockResearch.GetRemainingTurnsToResearch();
                    if ( turnsToInvent <= 1 )
                        Buffer.AddLang( "1TurnToComplete" );
                    else
                        Buffer.AddFormat1( "XTurnsToComplete", turnsToInvent.ToStringThousandsWhole() );
                }
                else if ( UnlockCoordinator.CurrentResearchDomain != null )
                {
                    int turnsToInvent = UnlockCoordinator.CurrentResearchDomain.GetRemainingTurnsToResearch();
                    if ( turnsToInvent <= 1 )
                        Buffer.AddLang( "1TurnToComplete" );
                    else
                        Buffer.AddFormat1( "XTurnsToComplete", turnsToInvent.ToStringThousandsWhole() );
                }
                else
                {
                    Buffer.AddFormat1( "XResearchAvailable", UnlockCoordinator.GetAvailableResearchCount().ToStringThousandsWhole() );
                }
            }

            public override void HandleMouseover()
            {
                tTechName.DoTechnologyBarMouseover( this.Element );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return bTechnologyIcon.DoTechnologyClick( input );
            }
        }
        #endregion

        #region tTechProgressPercent
        public class tTechProgressPercent : TextAbstractBase
        {
            public static tTechProgressPercent Instance;
            public tTechProgressPercent() { if ( Instance == null ) Instance = this; }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( UnlockCoordinator.CurrentUnlockResearch != null )
                {
                    int percentage = MathA.IntPercentageClamped( UnlockCoordinator.CurrentUnlockResearch.DuringGame_TurnsAppliedToResearch,
                        UnlockCoordinator.CurrentUnlockResearch.TurnsToUnlock, 0, 100 );

                    Buffer.AddRaw( percentage.ToStringIntPercent() );
                }
                else if ( UnlockCoordinator.CurrentResearchDomain != null )
                {
                    int percentage = MathA.IntPercentageClamped( UnlockCoordinator.CurrentResearchDomain.DuringGame_TurnsAppliedToResearch,
                        UnlockCoordinator.CurrentResearchDomain.TurnsToResearch, 0, 100 );

                    Buffer.AddRaw( percentage.ToStringIntPercent() );
                }
            }

            public override void HandleMouseover()
            {
                tTechName.DoTechnologyBarMouseover( this.Element );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                return bTechnologyIcon.DoTechnologyClick( input );
            }

            public override bool GetShouldBeHidden()
            {
                return UnlockCoordinator.CurrentUnlockResearch == null && UnlockCoordinator.CurrentResearchDomain == null;
            }
        }
        #endregion
    }
}

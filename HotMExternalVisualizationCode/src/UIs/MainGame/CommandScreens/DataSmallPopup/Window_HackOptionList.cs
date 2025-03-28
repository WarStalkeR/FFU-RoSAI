using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Arcen.HotM.ExternalVis
{
    public class Window_HackOptionList : ToggleableWindowController, IInputActionHandler
    {
        public static ISimMachineActor ActorDoingHack = null;
        public static ISimNPCUnit TargetOfHack = null;
        public static AbilityType HackAbility = null;

        #region HandleOpenCloseToggle
        public static void HandleOpenCloseToggle( ISimMachineActor Actor, ISimNPCUnit Target, AbilityType Ability )
        {
            if ( Instance.IsOpen && ActorDoingHack == Actor && TargetOfHack == Target && HackAbility == Ability )
                Instance.Close( WindowCloseReason.UserDirectRequest );
            else
            {
                ActorDoingHack = Actor;
                TargetOfHack = Target;
                HackAbility = Ability;
                Instance.Open();
                Engine_HotM.SelectedMachineActionMode = null;
            }
        }
        #endregion

        #region CloseIfAlreadyTargetingThis
        public static bool CloseIfAlreadyTargetingThis( ISimMachineActor Actor, AbilityType Ability )
        {
            if ( Instance.IsOpen && ActorDoingHack == Actor && TargetOfHack == Ability )
            {
                Instance.Close( WindowCloseReason.UserDirectRequest );
                return true;
            }
            return false;
        }
        #endregion

        #region GetIsOpenAndFocusedOn
        public static bool GetIsOpenAndFocusedOn( ISimNPCUnit Target )
        {
            return Instance.IsOpen && TargetOfHack == Target;
        }
        #endregion

        #region CloseIfOpen
        public static void CloseIfOpen()
        {
            Instance.Close( WindowCloseReason.UserDirectRequest );
        }
        #endregion

        public override void OnOpen()
        {
            if ( Window_VehicleUnitPanel.Instance.IsOpen )
                Window_VehicleUnitPanel.Instance.Close( WindowCloseReason.OtherWindowCausingClose );
        }

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.TheEndOfTime:
                    return false;
            }
            if ( Engine_HotM.CurrentLowerMode != null )
                return false;

            if ( Engine_HotM.SelectedMachineActionMode != null )
                return false;

            return base.GetShouldDrawThisFrame_Subclass();
        }

        public static Window_HackOptionList Instance;
        public Window_HackOptionList()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = false;
		}

        public override void OnClose( WindowCloseReason CloseReason )
        {
            ActorDoingHack = null;
            TargetOfHack = null;
            base.OnClose( CloseReason );
        }

        private static float runningY = 0;

        #region RenderHackOption
        public static bool RenderHackOption( IA5Sprite Sprite, GetOrSetUIData UIData )
        {
            bIconRow row = customParent.bIconRowPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            if ( row == null )
                return false; //this was just time-slicing, so ignore that failure for now

            float x = 2.769974f;
            customParent.bIconRowPool.ApplySingleItemInRow( row, x, runningY );
            runningY -= 20.73f;
            row.SetRelatedImage0SpriteIfNeeded( Sprite.GetSpriteForUI() );

            row.Assign( UIData );
            return true;
        }
        #endregion RenderHackOption

        public static CustomUIAbstractBase CustomParentInstance;
        public class customParent : CustomUIAbstractBase
        {
            public customParent()
            {
                Window_HackOptionList.CustomParentInstance = this;
            }

            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            internal static ButtonAbstractBase.ButtonPool<bIconRow> bIconRowPool;

            private int heightToShow = 0;
            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 1f * GameSettings.Current.GetFloat( "Scale_AbilityFooterBar" );

                //this.WindowController.ExtraOffsetXRaw = Window_ActorSidebarStatsLowerLeft.GetXWidthForOtherWindowOffsets();
                //this.WindowController.ExtraOffsetY = -Window_AbilityFooterBar.Instance.GetAbilityBarCurrentHeight_Scaled();

                if ( Window_HackOptionList.Instance != null )
                {
                    #region Global Init
                    if ( bIconRow.Original != null && !hasGlobalInitialized )
                    {
                        this.Element.Window.MinDeltaTimeBeforeUpdates = 0; //make as responsive as possible
                        this.Element.Window.MaxDeltaTimeBeforeUpdates = 0;
                        hasGlobalInitialized = true;
                        bIconRowPool = new ButtonAbstractBase.ButtonPool<bIconRow>( bIconRow.Original, 10, "bIconRow" );
                    }
                    #endregion

                    bIconRowPool.Clear( 60 );

                    this.OnUpdate_Content();
                }

                #region Expand or Shrink Size Of This Window
                int newHeight = 25 + MathA.Min( lastYHeightOfInterior, 400 );
                if ( heightToShow != newHeight )
                {
                    heightToShow = newHeight;
                    //appear from the middle out
                    this.Element.RelevantRect.anchorMin = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.pivot = new Vector2( 0.5f, 0.5f );
                    this.Element.RelevantRect.UI_SetHeight( heightToShow );
                }
                #endregion

                if ( ActorDoingHack != Engine_HotM.SelectedActor || //ActorDoingHack.GetIsBlockedFromActingInGeneral() ||
                    ActorDoingHack.IsFullDead || ActorDoingHack.IsInvalid )
                    Instance.Close( WindowCloseReason.ShowingRefused );
            }

            #region OnUpdate_Content
            private void OnUpdate_Content()
            {
                runningY = -3.069992f; //starting

                ISimMachineActor actor = ActorDoingHack;
                if ( actor == null || actor.IsFullDead || actor.IsInvalid )
                    return;
                ISimNPCUnit npc = TargetOfHack;
                if ( npc == null )
                    return;

                HandleFullHack();

                HackingScenarioType scenario = npc.UnitType.HackingScenario.GetEffectiveScenarioType();
                if ( scenario.QuickHackOptions.Count > 0 )
                {
                    if ( SimMetagame.CurrentChapterNumber == 1 )
                    {
                        if ( scenario.ID == "SimpleMechIntro" )
                        {
                            int determinationShortage = 20 - (int)ResourceRefs.Determination.Current;
                            if ( determinationShortage > 0 )
                                ResourceRefs.Determination.AlterCurrent_Named( determinationShortage, "Income_Other", ResourceAddRule.IgnoreUntilTurnChange );
                        }
                    }

                    int hackingSkill = actor.GetActorDataCurrent( ActorRefs.UnitHackingSkill, true );
                    int resistance = npc.GetActorDataCurrent( ActorRefs.NPCHackingResistance, true );
                    if ( hackingSkill < 10 )
                        hackingSkill = 10;

                    float multiplier = 1f;
                    if ( hackingSkill < resistance )
                    {
                        multiplier = (float)resistance / (float)hackingSkill;
                    }

                    foreach ( KeyValuePair<HackingDaemonType, bool> kv in scenario.QuickHackOptions )
                        HandleQuickHackOption( kv.Key, multiplier );
                }

                if ( bMainContentParent.ParentRT )
                    bMainContentParent.ParentRT.UI_SetHeight( MathA.Abs( runningY ) );
                lastYHeightOfInterior = Mathf.CeilToInt( MathA.Abs( runningY ) );
            }
            #endregion

            private void HandleFullHack()
            {
                RenderHackOption( IconRefs.HackingMain.Icon, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, false );

                                ExtraData.Buffer.Position40().AddLang( "FullHack_Header", colorHex );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartBasicTooltip( TooltipID.Create( "FullHack", "General" ), element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    if ( novel.TooltipWidth < 200 )
                                        novel.TooltipWidth = 200; //the old DaemonCard width

                                    novel.Icon = IconRefs.HackingMain.Icon;
                                    novel.TitleUpperLeft.AddLang( "FullHack_Header" );
                                    novel.TitleLowerLeft.AddLang( "HackOptions_Header" );

                                    novel.Main.AddBoldLangAndAfterLineItemHeader( "FullHack_Header", ColorTheme.DataLabelWhite ).AddLang( "FullHack_Tooltip", ColorTheme.DataBlue ).Line();
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                //do the thing

                                ActorDoingHack.AlterCurrentActionPoints( -HackAbility.ActionPointCost );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -HackAbility.MentalEnergyCost, "Expense_UnitActions", ResourceAddRule.IgnoreUntilTurnChange );
                                HackAbility.OnTargetedUse?.DuringGame_PlaySoundOnlyAtCamera();//.DuringGame_PlayAtLocation( destinationPoint, TargetingHelper.GetRotationAngleBetweenPoints( center, destinationPoint ) );

                                Hacking.HackingScene.HackerUnit = ActorDoingHack;
                                Hacking.HackingScene.TargetUnit = TargetOfHack;
                                Hacking.HackingScene.TargetDifficultyForAlly = 0;
                                Hacking.HackingScene.Scenario = TargetOfHack.UnitType.HackingScenario.GetEffectiveScenarioType();
                                Hacking.HackingScene.HasPopulatedScene = false;
                                Engine_HotM.CurrentLowerMode = CommonRefs.HackingScene;
                            }
                            break;
                    }
                } );
            }

            private void HandleQuickHackOption( HackingDaemonType Daemon, float multiplier )
            {
                RenderHackOption( Daemon.VisibleIcon, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int finalCost = multiplier <= 1f  ? Daemon.CostAmountToCorruptQuickHack : Mathf.CeilToInt( Daemon.CostAmountToCorruptQuickHack * ( multiplier * multiplier ));
                    bool canUse = Daemon.CostTypeToCorrupt == null || Daemon.CostTypeToCorrupt.Current >= finalCost;

                    //if ( canUse )
                    //{
                    //    if ( HackAbility.ActionPointCost > ActorDoingHack.CurrentActionPoints )
                    //        canUse = false;
                    //    if ( HackAbility.MentalEnergyCost > ResourceRefs.MentalEnergy.Current )
                    //        canUse = false;
                    //}

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                string colorHex = UIHelper.SetSmallOptionBGAndGetColor( element.Controller as ButtonAbstractBaseWithImage, false, !canUse );

                                if ( Daemon.CostTypeToCorrupt != null )
                                    ExtraData.Buffer.Space2x().AddRaw( finalCost.ToStringThousandsWhole() ).Space1x()
                                        .AddSpriteStyled_NoIndent( Daemon.CostTypeToCorrupt.Icon, AdjustedSpriteStyle.InlineLarger1_2, Daemon.CostTypeToCorrupt.IconColorHex );

                                ExtraData.Buffer.Position40().AddRaw( Daemon.GetDisplayName(), colorHex );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                //Daemon.RenderDaemonTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.Standard, false );

                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartBasicTooltip( TooltipID.Create( Daemon ), element, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                                    novel.Icon = Daemon.VisibleIcon;
                                    if ( novel.TooltipWidth < 200 )
                                        novel.TooltipWidth = 200; //the old DaemonCard width

                                    novel.TitleUpperLeft.AddRaw( Daemon.GetDisplayName() );

                                    novel.TitleLowerLeft.AddLang( "QuickHack_Header" );

                                    if ( Daemon.GetDescription().Length > 0 )
                                        novel.Main.AddRaw( Daemon.GetDescription(), ColorTheme.NarrativeColor ).Line();
                                    if ( Daemon.StrategyTip.Text.Length > 0 )
                                       novel.Main.AddRaw( Daemon.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                    novel.Main.AddBoldLangAndAfterLineItemHeader( "QuickHack_Header", ColorTheme.DataLabelWhite ).AddLang( "QuickHack_Tooltip", ColorTheme.DataBlue ).Line();

                                    if ( Daemon.CostTypeToCorrupt != null && finalCost > 0 )
                                    {
                                        if ( multiplier > 1f )
                                            novel.Main.StartStyleLineHeightA();

                                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Daemon_CostToCorrupt", ColorTheme.DataLabelWhite )
                                            .AddRaw( finalCost.ToString(), ColorTheme.DataBlue ).Space1x()
                                            .AddSpriteStyled_NoIndent( Daemon.CostTypeToCorrupt.Icon, AdjustedSpriteStyle.InlineLarger1_2, Daemon.CostTypeToCorrupt.IconColorHex );
                                        novel.Main.AddRaw( Daemon.CostTypeToCorrupt.GetDisplayName(), Daemon.CostTypeToCorrupt.IconColorHex );
                                        novel.Main.Line();
                                        
                                        if ( multiplier > 1f )
                                        {
                                            novel.Main.AddBoldLangAndAfterLineItemHeader( "QuickHack_CostMultiplier", ColorTheme.DataLabelWhite )
                                                .AddFormat1( "Multiplier", (multiplier * multiplier).ToStringSmallFixedDecimal( 1 ), ColorTheme.DataProblem ).Line();
                                            novel.CanExpand = CanExpandType.Brief;

                                            novel.Main.EndLineHeight();

                                            if ( InputCaching.ShouldShowDetailedTooltips || InputCaching.ShouldShowHyperDetailedTooltips )
                                            {
                                                novel.Main.AddFormat1( "QuickHack_CostMultiplier_Explanation", multiplier.ToStringSmallFixedDecimal( 1 ), ColorTheme.PurpleDim ).Line();
                                            }
                                        }
                                    }

                                    Daemon.AppendGainsToTooltip( novel.Main, true );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                if ( !canUse )
                                {
                                    ParticleSoundRefs.ErrorSound.DuringGame_PlaySoundOnlyAtCamera();
                                    return;
                                }


                                ActorDoingHack.AlterCurrentActionPoints( -HackAbility.ActionPointCost );
                                ResourceRefs.MentalEnergy.AlterCurrent_Named( -HackAbility.MentalEnergyCost, "Expense_CorruptedADaemon", ResourceAddRule.IgnoreUntilTurnChange );

                                if ( Daemon.CostTypeToCorrupt != null && finalCost > 0 )
                                    Daemon.CostTypeToCorrupt.AlterCurrent_Named( -finalCost, "Expense_CorruptedADaemon", ResourceAddRule.IgnoreUntilTurnChange );

                                Daemon.Implementation.DoFinalDaemonLogicForDeathOrQuickHack( Daemon, ActorDoingHack, TargetOfHack );
                                HackAbility.OnTargetedUse?.DuringGame_PlaySoundOnlyAtCamera();

                                Instance.Close( WindowCloseReason.UserDirectRequest );
                            }
                            break;
                    }
                } );
            }
        }

        private static int lastYHeightOfInterior = 0;

		/// <summary>
		/// Top header
		/// </summary>
		public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( TargetOfHack != null)
                    Buffer.AddRaw( TargetOfHack.GetDisplayName() );
            }
            public override void OnUpdate() { }
        }

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
            //think about it, we'll see
        }

        #region bIconRow
        public class bIconRow : ButtonAbstractBaseWithImage
        {
            public static bIconRow Original;
            public bIconRow() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
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

        #region imgBasicWindowBG
        public class imgBasicWindowBG : ImageAbstractBase
        {
            public override void HandleClick( MouseHandlingInput input )
            {
                //if ( input.RightButtonClicked )
                Instance.Close( WindowCloseReason.UserCasualRequest );
            }
        }
        #endregion
    }
}

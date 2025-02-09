using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using UnityEngine;
using UnityEngine.UI;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class Window_Debate : ToggleableWindowController, IInputActionHandler
    {
        public static Window_Debate Instance;

        public Window_Debate()
        {
            Instance = this;
            this.PreventsNormalInputHandlers = true;
        }

        public static int StartingTarget = 0;
        public static int CurrentTarget = 0;
        public static int MovesSoFar = 0;

        public static int StartingMistrust = 0;
        public static int CurrentMistrust = 0;

        public static int StartingDefiance = 0;
        public static int CurrentDefiance = 0;

        public static int StartingDiscards = 0;
        public static int RemainingDiscards = 0;

        public static int RewardsWon = 0;

        public static bTile RewardSpotA = null;
        public static bTile RewardSpotB = null;
        public static bTile RewardSpotC = null;

        public static DebatePhase Phase = DebatePhase.Ongoing;
        private static DebateLossReason lastLossReason = DebateLossReason.OutOfSpace;

        public static Dictionary<NPCDebateAction, bool> DiscardedActions = Dictionary<NPCDebateAction, bool>.Create_WillNeverBeGCed( 10, "Window_Debate-DiscardedActions" );
        public static List<UpgradeInt> UpgradesToApplyOnVictory = List<UpgradeInt>.Create_WillNeverBeGCed( 10, "Window_Debate-UpgradesToApplyOnVictory" );
        private static Dictionary<NPCDebateActionCategory, int> ActionCategoryFrequency = Dictionary<NPCDebateActionCategory, int>.Create_WillNeverBeGCed( 10, "Window_Debate-ActionCategoryFrequency" );
        private static Dictionary<NPCDebateActionCategory, bool> ActionCategoriesToCheck = Dictionary<NPCDebateActionCategory, bool>.Create_WillNeverBeGCed( 10, "Window_Debate-ActionCategoryFrequency" );
        private static List<UpgradeFloat> ExperienceUpgradesToChooseFrom = List<UpgradeFloat>.Create_WillNeverBeGCed( 10, "Window_Debate-ExperienceUpgradesToChooseFrom" );
        private static UpgradeFloat ExperienceUpgradeChosen = null;

        #region GetIsCategoryRelatedToAnyReward
        public static bool GetIsCategoryRelatedToAnyReward( NPCDebateActionCategory Cat )
        {
            if ( Cat == null )
                return false;
            if ( RewardSpotA != null && RewardSpotA.RewardCategory == Cat )
                return true;
            if ( RewardSpotB != null && RewardSpotB.RewardCategory == Cat )
                return true;
            if ( RewardSpotC != null && RewardSpotC.RewardCategory == Cat )
                return true;
            return false;
        }
        #endregion

        public override void Close( WindowCloseReason CloseReason )
        {
            switch ( CloseReason )
            {
                case WindowCloseReason.UserCasualRequest:
                    if ( Phase == DebatePhase.Ongoing ) //if complete, then just close without any warning
                    {
                        //if ( scene.PlayerShards.Count == 0 )
                        //    break; //go ahead and close, as the player is dead

                        ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small,
                            delegate { CloseForSure( WindowCloseReason.UserDirectRequest ); }, null,
                            LocalizedString.AddLang_New( "Debate_Close_AreYouSure_Header" ),
                            LocalizedString.AddLang_New( "Debate_Close_AreYouSure_Body" ),
                            LangCommon.Popup_Common_Yes.LocalizedString, LangCommon.Popup_Common_NoWait.LocalizedString );
                        return;
                    }
                    break;
            }

            CloseForSure( CloseReason );
        }

        public void CloseForSure( WindowCloseReason Reason )
        {
            switch ( Phase )
            {
                case DebatePhase.Ongoing:
                case DebatePhase.Lost:
                    {
                        //go back to where we were
                        if ( SimCommon.DebateStartingChoice != null && SimCommon.DebateTarget != null && SimCommon.DebateSource != null )
                        {
                            SimCommon.RewardProvider = NPCDialogChoiceHandler.Start( SimCommon.DebateSource, SimCommon.DebateStartingChoice.ParentDialog, SimCommon.DebateTarget, false );
                            SimCommon.OpenWindowRequest = OpenWindowRequest.Reward;
                            ParticleSoundRefs.DialogContinue.DuringGame_PlayAtLocation( SimCommon.DebateSource.GetDrawLocation(), Vector3.zero );
                        }
                    }
                    break;
                case DebatePhase.Won:
                    {
                        //go onwards to the next spot
                        if ( SimCommon.DebateStartingChoice != null && SimCommon.DebateTarget != null && SimCommon.DebateSource != null )
                        {
                            SimCommon.DebateStartingChoice.DuringGameplay_ExecuteChoiceResult( SimCommon.DebateTarget, DebateScenariosBasic.Rand, true );
                            ParticleSoundRefs.DialogContinue.DuringGame_PlayAtLocation( SimCommon.DebateSource.GetDrawLocation(), Vector3.zero );
                        }
                    }
                    break;
            }

            SimCommon.DebateTarget = null;
            SimCommon.DebateSource = null;
            SimCommon.DebateStartingChoice = null;
            SimCommon.DebateScenarioType = null;
            FullClose( Reason );
        }

        public override void OnClose( WindowCloseReason CloseReason )
        {
            base.OnClose( CloseReason );

            HasInitializedSinceLastOpen = false;
        }

        #region tHeaderText
        public class tHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                NPCDialogChoice choice = SimCommon.DebateStartingChoice;
                if ( choice != null )
                    Buffer.AddRaw( choice.DisplayName.Text );
            }
        }
        #endregion

        #region tSubHeaderText
        public class tSubHeaderText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimNPCUnit unit = SimCommon.DebateTarget;
                if ( unit != null )
                {
                    Buffer.StartColor( ColorTheme.DataBlue );
                    Buffer.StartLink( false, string.Empty, "NPCUnit", string.Empty )
                        .AddLangAndAfterLineItemHeader( "Debating" ).AddRaw( unit.GetDisplayName() )
                        .EndLink( false, false ).Space2x();
                    Buffer.StartLink( false, string.Empty, "NPCCohort", unit.FromCohort.ID )
                        .AddLangAndAfterLineItemHeader( "FromCohort" ).AddRaw( unit.FromCohort.GetDisplayName() )
                        .EndLink( false, false ).Line();
                }
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                switch ( TooltipLinkData[0] )
                {
                    case "NPCUnit":
                        {
                            ISimNPCUnit unit = SimCommon.DebateTarget;
                            unit?.RenderTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard, true, ActorTooltipExtraData.None, TooltipExtraRules.None );
                        }
                        break;
                    case "NPCCohort":
                        {
                            NPCCohort cohort = NPCCohortTable.Instance.GetRowByID( TooltipLinkData[1] );
                            cohort?.RenderNPCCohortTooltip( null, SideClamp.Any, TooltipShadowStyle.Standard );
                        }
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "Window_Debate-tSubHeaderText: no entry for " +
                            TooltipLinkData[0], Verbosity.ShowAsError );
                        break;
                }
            }
        }
        #endregion

        public const int COLUMN_COUNT = 9;
        public const int ROW_COUNT = 4;
        public const int UPCOMING_COUNT = 11;

        private static bool HasInitializedSinceLastOpen = false;

        public static bTile[,] Tiles = new bTile[COLUMN_COUNT, ROW_COUNT];
        public static bTile[] MainTiles = new bTile[COLUMN_COUNT+ COLUMN_COUNT];
        public static bUpcoming[] UpcomingList = new bUpcoming[UPCOMING_COUNT];

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

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bTile.Original != null && bUpcoming.Original != null )
                    {
                        hasGlobalInitialized = true;

                        for ( int y = 0; y < ROW_COUNT; y++ )
                        {
                            for ( int x = 0; x < COLUMN_COUNT; x++ )
                            {
                                bTile tile;
                                if ( x == 0 && y == 0 )
                                    tile = bTile.Original;
                                else
                                    tile = (bTile)(bTile.Original.Element.DuplicateSelf()).Controller;
                                tile.Column = x;
                                tile.Row = y;

                                tile.Element.RelevantRect.anchoredPosition = new Vector2( 70.8f + (x * 56f), -8f - (y * 56f) );

                                Tiles[x, y] = tile;
                            }
                        }

                        int mainTileIndex = 0;
                        for ( int y = 1; y < ROW_COUNT - 1; y++ )
                        {
                            for ( int x = 0; x < COLUMN_COUNT; x++ )
                            {
                                bTile tile = Tiles[x, y];
                                if ( x > 0 )
                                    tile.West = Tiles[x - 1, y];
                                if ( x < COLUMN_COUNT - 1 )
                                    tile.East = Tiles[x + 1, y];
                                if ( y > 1 )
                                    tile.North = Tiles[x, y - 1];
                                if ( y == 1 )
                                    tile.South = Tiles[x, y + 1];

                                MainTiles[mainTileIndex] = tile;
                                mainTileIndex++;
                            }
                        }

                        for ( int i = 0; i < UPCOMING_COUNT; i++ )
                        {
                            bUpcoming upcoming;
                            if ( i == 0 )
                                upcoming = bUpcoming.Original;
                            else
                                upcoming = (bUpcoming)(bUpcoming.Original.Element.DuplicateSelf()).Controller;
                            upcoming.Index = i;

                            upcoming.Element.RelevantRect.anchoredPosition = new Vector2( 24.1f + ( i * 33f), -34f );
                            if ( i > 0 )
                            {
                                upcoming.SetRelatedImage0SpriteIfNeeded( upcoming.Element.RelatedSprites[1] );
                                if ( i > 5 )
                                {
                                    upcoming.SetRelatedImage0ColorFromColorAlways( new Color( 1, 1, 1, 24f / 256f ) );
                                    upcoming.SetRelatedImage1ColorFromColorAlways( new Color( 1, 1, 1, 9f / 256f ) );
                                }
                                else
                                {
                                    upcoming.SetRelatedImage0ColorFromColorAlways( new Color( 1, 1, 1, 76f / 256f ) );
                                    upcoming.SetRelatedImage1ColorFromColorAlways( new Color( 1, 1, 1, 38f / 256f ) );
                                }
                            }

                            UpcomingList[i] = upcoming;
                        }

                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                    }
                }
                #endregion

                if ( !hasGlobalInitialized )
                    return;

                if ( !HasInitializedSinceLastOpen )
                {
                    HasInitializedSinceLastOpen = true;
                    SimCommon.DebateScenarioType.Implementation.TryHandleDebateScenarioLogic( SimCommon.DebateScenarioType, SimCommon.DebateStartingChoice, 
                        NPCDebateScenarioLogic.DoInitialPopulation );
                }

                if ( SimCommon.DebateTarget == null || SimCommon.DebateSource == null )
                    Instance.CloseForSure( WindowCloseReason.OtherWindowCausingClose ); //something is off

                if ( HoveredTile != null && !HoveredTile.Element.LastHadMouseWithin )
                    HoveredTile = null;

                if ( FlagRefs.HasBeenAskedAboutDebateTour != null && !FlagRefs.HasBeenAskedAboutDebateTour.DuringGameplay_IsTripped )
                {
                    FlagRefs.HasBeenAskedAboutDebateTour.TripIfNeeded();
                    ModalPopupData.CreateAndLogYesNoStyle( PopupYesNoStyle.Small,
                        delegate
                        {
                        }, delegate { FlagRefs.FinishDebateTour(); },
                        LocalizedString.AddLang_New( "DebateTour_OfferHeader" ),
                        LocalizedString.AddLang_New( "DebateTour_OfferBody1" ).AddRaw( "\n" )
                            .AddLang( "DebateTour_OfferBody2" ).AddRaw( "\n" )
                            .AddLang( "DebateTour_OfferBody3" ),
                        LocalizedString.AddLang_New( "DebateTour_OfferYes" ),
                        LocalizedString.AddLang_New( "UITour_OfferNo" ) );
                }

                #region Tour
                if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour1_Target )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Target_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 1, FlagRefs.DebateTourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour1_Target", "AlwaysSame" ), tTargetHeader.Sole.Element, SideClamp.Any, TooltipArrowSide.Right );
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour2_Failures )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Failures_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_Failures_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 2, FlagRefs.DebateTourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour2_Failures", "AlwaysSame" ), iFailureBorder.Sole.Element, SideClamp.Any, TooltipArrowSide.Right );
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour3_ActiveSlot )
                {
                    bTile firstActiveTile = null;
                    foreach ( bTile tile in MainTiles )
                    {
                        if ( tile.Status == DebateTileStatus.Available )
                        {
                            firstActiveTile = tile;
                            break;
                        }
                    }
                    if ( firstActiveTile == null )
                        firstActiveTile = MainTiles[1];

                    if ( firstActiveTile == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour3_ActiveSlot.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_ActiveSlot_Text1" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_ActiveSlot_Text2" ).EndColor()
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 3, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour3_ActiveSlot", "AlwaysSame" ), firstActiveTile.Element, SideClamp.Any, TooltipArrowSide.BottomRight );
                    }
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour4_Discard )
                {
                    if ( bDiscard.Sole == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour4_Discard.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Discard_Text1" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 4, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour4_Discard", "AlwaysSame" ), bDiscard.Sole.Element, SideClamp.Any, TooltipArrowSide.BottomLeft );
                    }
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour5_Queue )
                {
                    bUpcoming firstQueue = UpcomingList[0];

                    if ( firstQueue == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour5_Queue.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Queue_Text1" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_Queue_Text2" ).EndColor()
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 5, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour5_Queue", "AlwaysSame" ), firstQueue.Element, SideClamp.Any, TooltipArrowSide.BottomLeft );
                    }
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour6_Bonus )
                {
                    bTile firstReward = null;
                    if ( RewardSpotA != null && RewardSpotA.RewardCategory != null && RewardSpotA.RewardType != null )
                        firstReward = RewardSpotA;
                    else if ( RewardSpotB != null && RewardSpotB.RewardCategory != null && RewardSpotB.RewardType != null )
                        firstReward = RewardSpotB;
                    else if ( RewardSpotC != null && RewardSpotC.RewardCategory != null && RewardSpotC.RewardType != null )
                        firstReward = RewardSpotC;
                    if ( firstReward == null )
                        firstReward = RewardSpotA;

                    if ( firstReward == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour6_Bonus.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Bonus_Text1" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_Bonus_Text2" ).EndColor()
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 6, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour6_Bonus", "AlwaysSame" ), firstReward.Element, SideClamp.Any, TooltipArrowSide.TopLeft );
                    }
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour7_Opponent )
                {
                    if ( bUnitEnd.Sole == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour7_Opponent.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_Opponent_Text1" )
                            .AddRaw( "\n" ).AddLang( "DebateTour_Opponent_Text2" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_Opponent_Text3" ).EndColor()
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 7, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour7_Opponent", "AlwaysSame" ), bUnitEnd.Sole.Element, SideClamp.Any, TooltipArrowSide.Right );
                    }
                }
                else if ( SharedRenderManagerData.CurrentIndicator == Indicator.DebateTour8_FinalAdvice )
                {
                    bTile middleTile = MainTiles[2];

                    if ( middleTile == null )
                    {
                        //should never happen, but let's not soft-lock if it did
                        FlagRefs.DebateTour8_FinalAdvice.TripIfNeeded();
                    }
                    else
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLangWithFirstLineBold( "DebateTour_FinalAdvice_Text1" )
                            .AddRaw( "\n" ).AddLang( "DebateTour_FinalAdvice_Text2" )
                            .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "DebateTour_FinalAdvice_Text3" ).EndColor()
                            .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomTextLast", 8, FlagRefs.DebateTourParts );

                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "DebateTour8_FinalAdvice", "AlwaysSame" ), middleTile.Element, SideClamp.Any, TooltipArrowSide.TopLeft );
                    }
                }
                #endregion Tour
            }
        }

        #region tResultsText
        public class tResultsText : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Phase == DebatePhase.Lost )
                {
                    Buffer.AddBoldLangAndAfterLineItemHeader( "Debate_Loss_Header", ColorTheme.DataLabelWhite );
                    switch ( lastLossReason )
                    {
                        case DebateLossReason.OutOfSpace:
                            Buffer.AddLang( "Debate_Loss_OutOfSpace_Explanation", ColorTheme.NarrativeColor );
                            break;
                        case DebateLossReason.MistrustTooHigh:
                            Buffer.AddLang( "Debate_Loss_MistrustTooHigh_Explanation", ColorTheme.NarrativeColor );
                            break;
                        case DebateLossReason.DefianceTooHigh:
                            Buffer.AddLang( "Debate_Loss_DefianceTooHigh_Explanation", ColorTheme.NarrativeColor );
                            break;
                    }
                }
                else
                {
                    Buffer.AddBoldLangAndAfterLineItemHeader( "Debate_Win_Header", ColorTheme.DataLabelWhite );
                    Buffer.StartColor( ColorTheme.NarrativeColor );
                    if ( ExperienceUpgradeChosen != null )
                    {
                        Buffer.StartNoBr().StartLink( false, string.Empty, "UpgradeFloat", ExperienceUpgradeChosen.ID )
                            .AddRawAndAfterLineItemHeader( ExperienceUpgradeChosen.GetDisplayName() )
                            .AddRaw( ExperienceUpgradeChosen.GetFormattedValue( ExperienceUpgradeChosen.DuringGameplay_CurrentFloat ), ColorTheme.DataBlue )
                            .EndLink( false, false ).EndNoBr().Space2x();
                    }

                    if ( UpgradesToApplyOnVictory.Count > 0 )
                    {
                        foreach ( UpgradeInt upgrade in UpgradesToApplyOnVictory )
                        {
                            Buffer.StartNoBr().StartLink( false, string.Empty, "UpgradeInt", upgrade.ID )
                                .AddRawAndAfterLineItemHeader( upgrade.GetDisplayName() )
                                .AddRaw( upgrade.DuringGameplay_CurrentInt.ToString(), ColorTheme.DataBlue )
                                .EndLink( false, false ).EndNoBr().Space2x();
                        }
                    }
                    else
                    {
                        if ( ExperienceUpgradeChosen == null )
                            Buffer.AddRaw( LangCommon.None.Text );
                    }
                }
            }

            public override void HandleHyperlinkHover( string[] TooltipLinkData )
            {
                switch ( TooltipLinkData[0] )
                {
                    case "UpgradeFloat":
                        {
                            UpgradeFloat upgrade = UpgradeFloatTable.Instance.GetRowByID( TooltipLinkData[1] );
                            upgrade?.RenderUpgradeTooltip_General( null, SideClamp.Any, TooltipShadowStyle.Standard );
                        }
                        break;
                    case "UpgradeInt":
                        {
                            UpgradeInt upgrade = UpgradeIntTable.Instance.GetRowByID( TooltipLinkData[1] );
                            upgrade?.RenderUpgradeTooltip_General( null, SideClamp.Any, TooltipShadowStyle.Standard );
                        }
                        break;
                    default:
                        ArcenDebugging.LogSingleLine( "Window_Debate-tSubHeaderText: no entry for " +
                            TooltipLinkData[0], Verbosity.ShowAsError );
                        break;
                }
            }

            public override bool GetShouldBeHidden()
            {
                return Phase == DebatePhase.Ongoing;
            }
        }
        #endregion

        #region bHelp
        public class bHelp : ButtonAbstractBaseWithImage
        {
            public static bHelp Sole;
            public bHelp() { if ( Sole == null ) Sole = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Help" );
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bHelp", "Tour" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "DebateHelp_Header" );
                    novel.Main.AddLang( "DebateHelp_Body", ColorTheme.NarrativeColor );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                FlagRefs.ResetDebateTour();

                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return FlagRefs.HasFinishedDebateTour == null || !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped;
            }
        }
        #endregion

        #region bContinue
        public class bContinue : ButtonAbstractBaseWithImage
        {
            public static bContinue Sole;
            public bContinue() { if ( Sole == null ) Sole = this; }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Phase == DebatePhase.Lost )
                    Buffer.AddLang( "Debate_LossButton_Header" );
                else
                    Buffer.AddLang( "Debate_WinButton_Header" );
            }

            public override void GetOtherTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer, int OtherTextIndex )
            {
                if ( Phase == DebatePhase.Lost )
                    Buffer.AddLang( "Debate_LossButton_SubHeader" );
                else
                    Buffer.AddLang( "Debate_WinButton_SubHeader" );
            }

            public override void HandleMouseover()
            {
                if ( Phase == DebatePhase.Lost )
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bContinue", "Lost" ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        novel.TitleUpperLeft.AddLang( "Debate_LossButton_TooltipHeader" );
                        novel.Main.AddLang( "Debate_LossButton_TooltipBody", ColorTheme.NarrativeColor ).Line();
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                Instance.Close( WindowCloseReason.UserCasualRequest );
                return MouseHandlingResult.None;
            }

            public override bool GetShouldBeHidden()
            {
                return Phase == DebatePhase.Ongoing;
            }
        }
        #endregion

        #region DoOnWinDebate
        public static void DoOnWinDebate()
        {
            if ( Phase != DebatePhase.Ongoing )
                return; 
            Phase = DebatePhase.Won;

            SimCommon.DebateScenarioType.OnWin.DuringGame_Play();

            ArcenNotes.SendSimpleNoteToGameOnly( 100, NoteInstructionTable.Instance.GetRowByID( "NPCDialogChoiceDebateWon" ),
                NoteStyle.StoredGame, SimCommon.DebateStartingChoice.ParentDialog.ID, SimCommon.DebateStartingChoice.ID,
                SimCommon.DebateTarget?.IsManagedUnit?.FullSerializationID ?? string.Empty, SimCommon.DebateTarget?.FromCohort?.ID ?? string.Empty,
                (SimCommon.DebateTarget?.ContainerLocation.Get() as ISimBuilding)?.GetBuildingID() ?? 0, 0, 0,
                string.Empty, string.Empty, string.Empty,
                0 );

            //apply these late so that players can't go through and win part of this, then exit out and then come back
            foreach ( UpgradeInt upgrade in UpgradesToApplyOnVictory )
                upgrade.DuringGame_DoUpgrade( false ); //text about this

            if ( RewardsWon == 0 )
                AchievementRefs.OnTheMerits.TripIfNeeded();
            if ( RewardsWon >= 1 )
                AchievementRefs.BonusMerit.TripIfNeeded();
            if ( RewardsWon >= 2 )
                AchievementRefs.DoubleBonus.TripIfNeeded();
            if ( RewardsWon >= 3 )
                AchievementRefs.TripleWordScore.TripIfNeeded();

            #region Choose A Best Category To Upgrade
            ActionCategoriesToCheck.Clear();
            foreach ( NPCDebateTargetGroup targetGroup in SimCommon.DebateStartingChoice.DebateTargetGroups )
            {
                foreach ( KeyValuePair<NPCDebateActionCategory, UpgradeFloat> kv in targetGroup.CategoryMultipliers )
                    ActionCategoriesToCheck[kv.Key] = true;
            }

            ActionCategoryFrequency.Clear();
            foreach ( bTile tile in MainTiles )
            {
                if ( tile.Action != null )
                {
                    foreach ( KeyValuePair<string, NPCDebateActionCategory> kv in tile.Action.Categories )
                    {
                        if ( ActionCategoriesToCheck.ContainsKey( kv.Value ) )
                            ActionCategoryFrequency[kv.Value]++;
                    }
                }
            }

            ExperienceUpgradesToChooseFrom.Clear();
            ExperienceUpgradeChosen = null;
            if ( ActionCategoryFrequency.Count > 0 )
            {
                NPCDebateActionCategory bestCat = null;
                int bestCatCount = 0;
                foreach ( KeyValuePair<NPCDebateActionCategory, int> kv in ActionCategoryFrequency )
                {
                    if ( kv.Value > bestCatCount )
                    {
                        bestCat = kv.Key;
                        bestCatCount = kv.Value;
                    }
                    else if ( kv.Value == bestCatCount && DebateScenariosBasic.Rand.Next( 0, 100 ) < 33 )
                    {
                        bestCat = kv.Key;
                        bestCatCount = kv.Value;
                    }
                }

                if ( bestCat != null )
                {
                    foreach ( NPCDebateTargetGroup targetGroup in SimCommon.DebateStartingChoice.DebateTargetGroups )
                    {
                        foreach ( KeyValuePair<NPCDebateActionCategory, UpgradeFloat> kv in targetGroup.CategoryMultipliers )
                        {
                            if ( kv.Key == bestCat )
                                ExperienceUpgradesToChooseFrom.Add( kv.Value );
                        }
                    }

                    if ( ExperienceUpgradesToChooseFrom.Count > 0 )
                    {
                        UpgradeFloat upgrade = ExperienceUpgradesToChooseFrom.GetRandom( DebateScenariosBasic.Rand );

                        upgrade.DuringGame_DoUpgrade( false ); //text about this
                        ExperienceUpgradeChosen = upgrade;
                    }
                }
            }
            #endregion
        }
        #endregion

        #region DoOnLoseDebate
        public static void DoOnLoseDebate( DebateLossReason Reason )
        {
            if ( Phase != DebatePhase.Ongoing )
                return;
            Phase = DebatePhase.Lost;
            lastLossReason = Reason;

            SimCommon.DebateScenarioType.OnLose.DuringGame_Play();
            UpgradesToApplyOnVictory.Clear();
        }
        #endregion

        #region bUnitStart
        public class bUnitStart : ImageButtonAbstractBase
        {
            public static bUnitStart Sole;
            public bUnitStart() { if ( Sole == null ) Sole = this; }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                ISimMachineActor actor = SimCommon.DebateSource;
                if ( actor == null )
                    return;

                this.SetRelatedImage0SpriteIfNeeded( actor.GetTooltipIcon().GetSpriteForUI() );
                this.SetRelatedImage1ColorFromHexIfNeeded( actor?.GetTooltipIconColorStyle()?.RelatedBorderColorHex??string.Empty );
            }

            public override void HandleMouseover()
            {
                ISimMachineActor actor = SimCommon.DebateSource;
                actor?.RenderTooltip( this.Element, SideClamp.Any, TooltipShadowStyle.Standard, true, ActorTooltipExtraData.None, TooltipExtraRules.None );
            }
        }
        #endregion

        #region bUnitEnd
        public class bUnitEnd : ImageButtonAbstractBase
        {
            public static bUnitEnd Sole;
            public bUnitEnd() { if ( Sole == null ) Sole = this; }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                ISimNPCUnit unit = SimCommon.DebateTarget;
                if ( unit == null )
                    return;

                this.SetRelatedImage0SpriteIfNeeded( unit.GetTooltipIcon().GetSpriteForUI() );
                this.SetRelatedImage1ColorFromHexIfNeeded( unit?.GetTooltipIconColorStyle()?.RelatedBorderColorHex ?? string.Empty );
            }

            public override void HandleMouseover()
            {
                ISimNPCUnit unit = SimCommon.DebateTarget;
                if ( unit == null ) 
                    return;
                NPCDialogChoice choice = SimCommon.DebateStartingChoice;
                if ( choice == null )
                    return;
                NPCDebateScenarioType scenario = SimCommon.DebateScenarioType;
                if ( scenario == null )
                    return;

                if ( !InputCaching.ShouldShowDetailedTooltips && !InputCaching.ShouldShowHyperDetailedTooltips )
                {
                    NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "bUnitEnd", "Any" ), this.Element, SideClamp.Any, TooltipNovelWidth.Smaller ) )
                    {
                        novel.ShadowStyle = TooltipShadowStyle.Standard;
                        novel.CanExpand = CanExpandType.HyperDetailed;
                        novel.Main_ExtraSizePerLine = 0f; //otherwise lots of extra spacing

                        novel.TitleUpperLeft.AddRaw( unit.GetDisplayName() );

                        novel.Main.StartStyleLineHeightA();
                        foreach ( NPCDebateTargetGroup group in choice.DebateTargetGroups )
                        {
                            novel.Main.AddBoldRawAndAfterLineItemHeader( group.GetDisplayName(), ColorTheme.DataLabelWhite ).Line();

                            foreach ( KeyValuePair<NPCDebateActionCategory, UpgradeFloat> kv in group.CategoryMultipliers )
                                novel.Main.AddSpriteStyled_NoIndent( kv.Key.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor )
                                    .AddRawAndAfterLineItemHeader( kv.Key.GetDisplayName(), ColorTheme.NarrativeColor )
                                    .AddRaw( kv.Value.GetFormattedValue( kv.Value.DuringGameplay_CurrentFloat ), 
                                    kv.Value.DuringGameplay_CurrentFloat < 1f ? ColorTheme.DataProblem : ColorTheme.DataGood ).Line();

                            novel.Main.Line(); //added spacing
                        }

                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_CurrentDebateScenario", ColorTheme.DataLabelWhite ).Line();
                        foreach ( NPCDebateAction action in scenario.Actions )
                        {
                            NPCDebateScenarioActionIntensity intensity = scenario.ActionIntensities[action.ID];

                            if ( intensity.TargetMultiplier <= 0 && intensity.TargetFlatChange == 0 && intensity.DefianceChange == 0 && intensity.MistrustChange == 0 )
                                continue; //it is included, but there are no shifts to what it normally does

                            novel.Main.AddSpriteStyled_NoIndent( action.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor )
                                .AddRawAndAfterLineItemHeader( action.GetDisplayName(), ColorTheme.NarrativeColor );

                            if ( intensity.TargetMultiplier > 0 || intensity.TargetFlatChange != 0 )
                            {
                                novel.Main.AddSpriteStyled_NoIndent( IconRefs.DebateTarget.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                if ( intensity.TargetMultiplier > 0 )
                                {
                                    novel.Main.AddRaw( Mathf.RoundToInt( intensity.TargetMultiplier * 100f ).ToStringIntPercent(),
                                        intensity.TargetMultiplier < 1f ? ColorTheme.DataProblem : ColorTheme.DataGood );

                                    if ( intensity.TargetFlatChange != 0 )
                                        novel.Main.Space1x();
                                }

                                if ( intensity.TargetFlatChange > 0 )
                                    novel.Main.AddFormat1( "PositiveChange", intensity.TargetFlatChange.ToString(), ColorTheme.DataGood );
                                else if ( intensity.TargetFlatChange < 0 )
                                    novel.Main.AddRaw( intensity.TargetFlatChange.ToString(), ColorTheme.DataProblem );

                                novel.Main.Space3x();
                            }

                            if ( intensity.MistrustChange != 0 )
                            {
                                novel.Main.AddSpriteStyled_NoIndent( IconRefs.DebateMistrust.Icon, AdjustedSpriteStyle.InlineLarger1_2 );

                                if ( intensity.MistrustChange > 0 )
                                    novel.Main.AddFormat1( "PositiveChange", intensity.MistrustChange.ToString(), ColorTheme.DataProblem );
                                else
                                    novel.Main.AddRaw( intensity.MistrustChange.ToString(), ColorTheme.DataGood );

                                novel.Main.Space3x();
                            }

                            if ( intensity.DefianceChange != 0 )
                            {
                                novel.Main.AddSpriteStyled_NoIndent( IconRefs.DebateDefiance.Icon, AdjustedSpriteStyle.InlineLarger1_2 );

                                if ( intensity.DefianceChange > 0 )
                                    novel.Main.AddFormat1( "PositiveChange", intensity.DefianceChange.ToString(), ColorTheme.DataProblem );
                                else
                                    novel.Main.AddRaw( intensity.DefianceChange.ToString(), ColorTheme.DataGood );

                                //novel.Main.Space3x();
                            }

                            novel.Main.Line();
                        }

                        novel.Main.Line().EndLineHeight();

                        novel.Main.AddLang( "Debate_StatsNote", ColorTheme.PurpleDim ).Line();
                    }
                }
                else
                {
                    unit.RenderTooltip( this.Element, SideClamp.Any, TooltipShadowStyle.Standard, true, ActorTooltipExtraData.None, TooltipExtraRules.None );
                }
            }
        }
        #endregion

        #region Target
        public class tTargetHeader : TextAbstractBase
        {
            public static tTargetHeader Sole;
            public tTargetHeader() { if ( Sole == null ) Sole = this; }

            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer/*.AddSpriteStyled_NoIndent( IconRefs.DebateTarget.Icon, AdjustedSpriteStyle.InlineLarger1_2 )*/.AddLang( "Debate_Target_Name" );
            }

            public static void HandleMouse( IArcenUIElementForSizing DrawNextTo )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "Any", "Target" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.Icon = IconRefs.DebateTarget.Icon;
                    novel.TitleUpperLeft.AddLang( "Debate_Target_Name" );
                    novel.TitleLowerLeft.AddLang( "Debate_SubHeader_VictoryCondition" );
                    novel.Main.AddLang( "Debate_Target_Description", ColorTheme.NarrativeColor ).Line();

                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_OriginalTarget", ColorTheme.DataLabelWhite ).AddRaw( StartingTarget.ToString() ).Line();
                    if ( CurrentTarget != StartingTarget )
                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_CurrentTarget", ColorTheme.DataLabelWhite ).AddRaw( CurrentTarget.ToString() ).Line();
                }
            }

            public override void HandleMouseover()
            {
                HandleMouse( this.Element );
            }
        }

        public class tTargetNumber : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( CurrentTarget.ToString() );
            }

            public override void HandleMouseover()
            {
                tTargetHeader.HandleMouse( this.Element );
            }
        }
        #endregion

        #region Moves
        public class tMovesHeader : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "Debate_Moves_Name" );
            }

            public static void HandleMouse( IArcenUIElementForSizing DrawNextTo )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Any", "Moves" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "Debate_Moves_Name" );
                    novel.Main.AddLang( "Debate_Moves_Description", ColorTheme.NarrativeColor ).Line();
                }
            }

            public override void HandleMouseover()
            {
                HandleMouse( this.Element );
            }
        }

        public class tMovesNumber : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( MovesSoFar.ToString() );
            }

            public override void HandleMouseover()
            {
                tMovesHeader.HandleMouse( this.Element );
            }
        }
        #endregion

        #region iFailureBorder
        public class iFailureBorder : ImageAbstractBase
        {
            public static iFailureBorder Sole;
            public iFailureBorder() { if ( Sole == null ) Sole = this; }

            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
            }
        }
        #endregion

        #region Mistrust
        public class iMistrustStatIcon : ImageAbstractBase
        {
            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
                Image.SetSpriteIfNeeded_Simple( IconRefs.DebateMistrust.Icon.GetSpriteForUI() );
            }

            public static void HandleMouse( IArcenUIElementForSizing DrawNextTo )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "Any", "Mistrust" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.Icon = IconRefs.DebateMistrust.Icon;
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "Debate_Mistrust_Name" );
                    novel.TitleLowerLeft.AddLang( "Debate_SubHeader_FailureCondition" );
                    novel.Main.AddLang( "Debate_Mistrust_Description", ColorTheme.NarrativeColor ).Line();


                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_StartingMistrust", ColorTheme.DataLabelWhite ).AddRaw( StartingMistrust.ToString() ).Line();
                    if ( CurrentMistrust != StartingMistrust )
                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_CurrentMistrust", ColorTheme.DataLabelWhite ).AddRaw( CurrentMistrust.ToString() ).Line();
                }
            }

            public override void HandleMouseover()
            {
                HandleMouse( iFailureBorder.Sole.Element );
            }
        }

        public class tMistrustStatNumber : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( CurrentMistrust.ToString(), CurrentMistrust < 100 ? string.Empty : ColorTheme.RedOrange2 );
            }

            public override void HandleMouseover()
            {
                iMistrustStatIcon.HandleMouse( iFailureBorder.Sole.Element );
            }
        }
        #endregion

        #region Defiance
        public class iDefianceStatIcon : ImageAbstractBase
        {
            public override void UpdateImagesFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages )
            {
                Image.SetSpriteIfNeeded_Simple( IconRefs.DebateDefiance.Icon.GetSpriteForUI() );
            }

            public static void HandleMouse( IArcenUIElementForSizing DrawNextTo )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "Any", "Defiance" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Simple ) )
                {
                    novel.Icon = IconRefs.DebateDefiance.Icon;
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "Debate_Defiance_Name" );
                    novel.TitleLowerLeft.AddLang( "Debate_SubHeader_FailureCondition" );
                    novel.Main.AddLang( "Debate_Defiance_Description", ColorTheme.NarrativeColor ).Line();


                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_StartingDefiance", ColorTheme.DataLabelWhite ).AddRaw( StartingDefiance.ToString() ).Line();
                    if ( CurrentDefiance != StartingDefiance )
                        novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_CurrentDefiance", ColorTheme.DataLabelWhite ).AddRaw( CurrentDefiance.ToString() ).Line();
                }
            }

            public override void HandleMouseover()
            {
                HandleMouse( iFailureBorder.Sole.Element );
            }
        }

        public class tDefianceStatNumber : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddRaw( CurrentDefiance.ToString(), CurrentDefiance < 100 ? string.Empty : ColorTheme.RedOrange2 );
            }

            public override void HandleMouseover()
            {
                iDefianceStatIcon.HandleMouse( iFailureBorder.Sole.Element );
            }
        }
        #endregion

        #region bDiscard
        public class bDiscard : ImageButtonAbstractBase
        {
            public static bDiscard Sole;
            public bDiscard() { if ( Sole == null ) Sole = this; }

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                SubTexts[0].Text.DirectlySetNextText( RemainingDiscards.ToString() );

                //this.SetRelatedImage1SpriteIfNeeded( IconRefs.DebateDiscard.Icon.GetSpriteForUI() );
            }

            public static void HandleMouse( IArcenUIElementForSizing DrawNextTo )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "Any", "Trash" ), DrawNextTo, SideClamp.LeftOrRight, TooltipNovelWidth.Smaller ) )
                {
                    novel.ShadowStyle = TooltipShadowStyle.Standard;
                    novel.TitleUpperLeft.AddLang( "Debate_Discard_Name" );
                    novel.Main.AddFormat1( "Debate_Discard_Description", StartingDiscards, ColorTheme.NarrativeColor ).Line();

                    novel.Main.AddBoldLangAndAfterLineItemHeader( "Debate_RemainingDiscards", ColorTheme.DataLabelWhite ).AddRaw( RemainingDiscards.ToString() ).Line();

                    if ( DiscardedActions.Count > 0 )
                    {
                        novel.FrameTitle.AddLang( "Debate_DiscardedSoFar", ColorTheme.DataLabelWhite );

                        novel.FrameBody.StartStyleLineHeightA();
                        foreach ( KeyValuePair<NPCDebateAction, bool> kv in DiscardedActions )
                        {
                            novel.FrameBody.AddSpriteStyled_NoIndent( kv.Key.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                .AddRaw( kv.Key.GetDisplayName() ).Line();
                        }

                        novel.FrameBody.EndLineHeight();
                    }
                }
            }

            public override void HandleMouseover()
            {
                HandleMouse( this.Element );
            }

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                if ( Phase != DebatePhase.Ongoing )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                DebateHelper.TryToTrashAction( UpcomingList[0].Action, this.Element.RelevantRect );

                return base.HandleClick( input );
            }

            public override bool GetShouldBeHidden()
            {
                return Phase != DebatePhase.Ongoing;
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
                if ( !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                Instance.Close( WindowCloseReason.UserCasualRequest );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        public static bTile HoveredTile = null;

        #region bTile
        public class bTile : ImageButtonAbstractBase
        {
            public static bTile Original;
            public bTile() { if ( Original == null ) Original = this; }

            public int Row;
            public int Column;

            public bTile North = null;
            public bTile South = null;
            public bTile East = null;
            public bTile West = null;

            public DebateTileStatus Status = DebateTileStatus.OuterRow;
            public NPCDebateAction Action = null;
            public int ActionScore = 0;
            public NPCDebateReward RewardType = null;
            public NPCDebateActionCategory RewardCategory = null;

            private ArcenUIWrapperedTMProText Text1;
            private ArcenUIWrapperedTMProText Text2;

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Text1 == null )
                    Text1 = SubTexts[0].Text;
                if ( Text2 == null )
                    Text2 = SubTexts[1].Text;

                if ( Phase != DebatePhase.Ongoing )
                {
                    if ( Status == DebateTileStatus.Available )
                        Status = DebateTileStatus.Normal;
                }

                switch ( Status )
                {
                    case DebateTileStatus.Available:
                        this.SetRelatedImage0AlphaIfNeeded( 1f );
                        this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[0] );
                        this.SetRelatedImage1EnabledIfNeeded( false );
                        Text1?.DirectlySetNextText( string.Empty );

                        if ( HoveredTile == this && UpcomingList[0].Action != null )
                        {
                            NPCDebateAction action = UpcomingList[0].Action;
                            this.SetRelatedImage2EnabledIfNeeded( true );
                            this.SetRelatedImage2SpriteIfNeeded( action.Icon.GetSpriteForUI() );
                            this.SetRelatedImage2AlphaIfNeeded( 0.5f );

                            int targetProgress = action.DuringGame_GetTargetProgress( null );
                            if ( Text2 != null )
                            {
                                ArcenDoubleCharacterBuffer buffer = Text2.StartWritingToBuffer();

                                foreach ( KeyValuePair<string, NPCDebateActionCategory> kv in action.Categories )
                                {
                                    if ( GetIsCategoryRelatedToAnyReward( kv.Value ) )
                                        buffer.AddSpriteStyled_NoIndent( kv.Value.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor ).Space1x();
                                }

                                if ( targetProgress > 0 )
                                    buffer.AddFormat1( "PositiveChange", targetProgress, ColorTheme.DataGood );
                                else
                                    buffer.AddRaw( targetProgress.ToString(), ColorTheme.DataProblem );
                                Text2.FinishWritingToBuffer();
                            }
                        }
                        else
                        {
                            this.SetRelatedImage2EnabledIfNeeded( false );
                            Text2?.DirectlySetNextText( string.Empty );
                        }
                        break;
                    case DebateTileStatus.OuterRow:
                        this.SetRelatedImage0AlphaIfNeeded( 0.176f );
                        this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[2] );
                        this.SetRelatedImage1EnabledIfNeeded( false );
                        Text1?.DirectlySetNextText( string.Empty );

                        if ( this.RewardType != null && this.RewardCategory != null )
                        {
                            this.SetRelatedImage2EnabledIfNeeded( true );
                            this.SetRelatedImage2SpriteIfNeeded( this.RewardType.Icon.GetSpriteForUI() );

                            if ( Text2 != null )
                            {
                                ArcenDoubleCharacterBuffer buffer = Text2.StartWritingToBuffer();
                                buffer.AddSpriteStyled_NoIndent( RewardCategory.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                    .AddRaw( RewardCategory.ShortName.Text );
                                Text2.FinishWritingToBuffer();
                            }
                        }
                        else
                        {
                            this.SetRelatedImage2EnabledIfNeeded( false );
                            Text2?.DirectlySetNextText( string.Empty );
                        }
                        break;
                    default:
                    case DebateTileStatus.Normal:
                        this.SetRelatedImage0AlphaIfNeeded( 1f );
                        this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[2] );
                        this.SetRelatedImage2AlphaIfNeeded( 1f );
                        this.SetRelatedImage1EnabledIfNeeded( false );
                        Text1?.DirectlySetNextText( string.Empty );
                        if ( this.Action != null )
                        {
                            this.SetRelatedImage2EnabledIfNeeded( true );
                            this.SetRelatedImage2SpriteIfNeeded( this.Action.Icon.GetSpriteForUI() );

                            if ( Text2 != null )
                            {
                                ArcenDoubleCharacterBuffer buffer = Text2.StartWritingToBuffer();

                                foreach ( KeyValuePair<string, NPCDebateActionCategory> kv in this.Action.Categories )
                                {
                                    if ( GetIsCategoryRelatedToAnyReward( kv.Value ) )
                                        buffer.AddSpriteStyled_NoIndent( kv.Value.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor ).Space1x();
                                }

                                if ( this.ActionScore > 0 )
                                    buffer.AddFormat1( "PositiveChange", this.ActionScore, ColorTheme.DataGood );
                                else
                                    buffer.AddRaw( this.ActionScore.ToString(), ColorTheme.DataProblem );
                                Text2.FinishWritingToBuffer();
                            }
                        }
                        else
                        {
                            this.SetRelatedImage2EnabledIfNeeded( false );
                            Text2?.DirectlySetNextText( string.Empty );
                        }
                        break;
                }
            }

            public override MouseHandlingResult HandleClick( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFinishedDebateTour.DuringGameplay_IsTripped )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                if ( Phase != DebatePhase.Ongoing )
                {
                    ParticleSoundRefs.BlockedSound.DuringGame_PlaySoundOnlyAtCamera();
                    return MouseHandlingResult.None;
                }

                switch ( Status )
                {
                    case DebateTileStatus.Available:
                        if ( this.Action == null && UpcomingList[0].Action != null )
                        {
                            NPCDebateAction action = UpcomingList[0].Action;
                            int targetProgress = action.DuringGame_GetTargetProgress( null );
                            int mistrustChange = action.DuringGame_GetMistrustChange( null );
                            int defianceChange = action.DuringGame_GetDefianceChange( null );

                            this.Action = action;
                            this.ActionScore = targetProgress;

                            DebateHelper.DoAction( targetProgress, mistrustChange, defianceChange );

                            action.OnUse.DuringGame_PlayAtUILocation( this.Element.RelevantRect.CalculateScreenCenter() );

                            this.Status = DebateTileStatus.Normal;
                        }
                        else
                        {
                        }
                        break;
                }

                return base.HandleClick( input );
            }

            public override void HandleMouseover()
            {
                HoveredTile = this;

                switch ( Status )
                {
                    case DebateTileStatus.Available:
                        if ( HoveredTile == this && UpcomingList[0].Action != null )
                        {
                            NPCDebateAction action = UpcomingList[0].Action;
                            if ( InputCaching.ShouldShowDetailedTooltips || InputCaching.ShouldShowHyperDetailedTooltips )
                            {
                                action.RenderActionTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, true );
                            }
                            else
                            {
                                int targetProgress = action.DuringGame_GetTargetProgress( null );
                                int mistrustChange = action.DuringGame_GetMistrustChange( null );
                                int defianceChange = action.DuringGame_GetDefianceChange( null );

                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "AvailableTile", this.Row, this.Column ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.SizeToText ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.TightDark;
                                    //novel.CanExpand = CanExpandType.Brief;

                                    foreach ( KeyValuePair<string, NPCDebateActionCategory> kv in action.Categories )
                                    {
                                        if ( GetIsCategoryRelatedToAnyReward( kv.Value ) )
                                            novel.TitleUpperLeft.AddSpriteStyled_NoIndent( kv.Value.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor ).Space1x();
                                    }

                                    novel.TitleUpperLeft.AddSpriteStyled_NoIndent( IconRefs.DebateTarget.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                    if ( targetProgress > 0 )
                                        novel.TitleUpperLeft.AddFormat1( "PositiveChange", targetProgress, ColorTheme.DataGood );
                                    else
                                        novel.TitleUpperLeft.AddRaw( targetProgress.ToString(), ColorTheme.DataProblem );

                                    if ( mistrustChange != 0 )
                                    {
                                        novel.TitleUpperLeft.Space2x();

                                        novel.TitleUpperLeft.AddSpriteStyled_NoIndent( IconRefs.DebateMistrust.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                        if ( mistrustChange > 0 )
                                            novel.TitleUpperLeft.AddFormat1( "PositiveChange", mistrustChange, ColorTheme.DataProblem );
                                        else
                                            novel.TitleUpperLeft.AddRaw( mistrustChange.ToString(), ColorTheme.DataGood );
                                    }

                                    if ( defianceChange != 0 )
                                    {
                                        novel.TitleUpperLeft.Space2x();

                                        novel.TitleUpperLeft.AddSpriteStyled_NoIndent( IconRefs.DebateDefiance.Icon, AdjustedSpriteStyle.InlineLarger1_2 );
                                        if ( defianceChange > 0 )
                                            novel.TitleUpperLeft.AddFormat1( "PositiveChange", defianceChange, ColorTheme.DataProblem );
                                        else
                                            novel.TitleUpperLeft.AddRaw( defianceChange.ToString(), ColorTheme.DataGood );
                                    }

                                    //this is super unusual!  All in one line to fit between stuff
                                    novel.TitleUpperLeft.Space2x().StartSize70().StartColor( ColorTheme.NarrativeColor );
                                    InputCaching.AppendDetailedTooltipInstructionsToTooltipPlain( novel.TitleUpperLeft, true );
                                    novel.TitleUpperLeft.EndSize();
                                }
                            }
                        }
                        else
                        {
                        }
                        break;
                    case DebateTileStatus.OuterRow:
                        if ( this.RewardType != null && this.RewardCategory != null )
                        {
                            this.RewardType.RenderDebateRewardTooltip( this.RewardCategory, this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard );
                        }
                        else
                        {
                        }
                        break;
                    case DebateTileStatus.Normal:
                        if ( this.Action != null )
                        {
                            NPCDebateAction action = this.Action;
                            if ( InputCaching.ShouldShowDetailedTooltips || InputCaching.ShouldShowHyperDetailedTooltips )
                            {
                                action.RenderActionTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, true );
                            }
                            else
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "NormalTile", this.Row, this.Column ), this.Element, SideClamp.AboveOrBelow, TooltipNovelWidth.SizeToText ) )
                                {
                                    novel.ShadowStyle = TooltipShadowStyle.TightDark;

                                    foreach ( KeyValuePair<string, NPCDebateActionCategory> kv in action.Categories )
                                    {
                                        if ( GetIsCategoryRelatedToAnyReward( kv.Value ) )
                                            novel.TitleUpperLeft.AddSpriteStyled_NoIndent( kv.Value.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.NarrativeColor ).Space1x();
                                    }

                                    novel.TitleUpperLeft.AddRaw( action.GetDisplayName() );

                                    //this is super unusual!  All in one line to fit between stuff
                                    novel.TitleUpperLeft.Space2x().StartSize70().StartColor( ColorTheme.NarrativeColor );
                                    InputCaching.AppendDetailedTooltipInstructionsToTooltipPlain( novel.TitleUpperLeft, true );
                                    novel.TitleUpperLeft.EndSize();
                                }
                            }
                        }
                        else
                        {
                        }
                        break;
                }
            }
        }
        #endregion

        #region bUpcoming
        public class bUpcoming : ButtonAbstractBaseWithImage
        {
            public static bUpcoming Original;
            public bUpcoming() { if ( Original == null ) Original = this; }

            public int Index;

            public NPCDebateAction Action;

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Action != null )
                {
                    this.SetRelatedImage1SpriteIfNeeded( Action.Icon.GetSpriteForUI() );
                    this.SetRelatedImage1EnabledIfNeeded( true );
                }
                else
                    this.SetRelatedImage1EnabledIfNeeded( false );
            }

            public override void HandleMouseover()
            {
                this.Action?.RenderActionTooltip( this.Element, SideClamp.AboveOrBelow, TooltipShadowStyle.Standard, false );
            }

            public override bool GetShouldBeHidden()
            {
                return Phase != DebatePhase.Ongoing;
            }
        }
        #endregion

        #region bHoverTile
        public class bHoverTile : ImageButtonAbstractBase
        {
            public static bHoverTile Sole;
            public bHoverTile() { if ( Sole == null ) Sole = this; }

            private ArcenUIWrapperedTMProText Text;

            public override void UpdateContentFromVolatile( ArcenUIWrapperedUnityImage Image, ArcenUI_Image.SubImageGroup SubImages, SubTextGroup SubTexts )
            {
                if ( Text == null )
                    Text = SubTexts[0].Text;

                Text?.DirectlySetNextText( string.Empty );

                this.SetRelatedImage0AlphaIfNeeded( 0.8f );
                this.SetRelatedImage0SpriteIfNeeded( this.Element.RelatedSprites[2] );
                this.SetRelatedImage1EnabledIfNeeded( false );
            }

            public override bool GetShouldBeHidden()
            {
                return true;
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
                        
                    }
                    break;
                default:
                    InputWindowCutthrough.HandleKey( InputActionType.ID );
                    break;
            }
        }
    }

    public enum DebateTileStatus
    {
        OuterRow,
        Available,
        Normal
    }

    public enum DebatePhase
    {
        Ongoing,
        Lost,
        Won
    }

    public enum DebateLossReason
    {
        OutOfSpace,
        MistrustTooHigh,
        DefianceTooHigh
    }
}

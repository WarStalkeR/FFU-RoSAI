using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_RadialMenu : WindowControllerAbstractBase
    {
        public static Window_RadialMenu Instance;
		
		public Window_RadialMenu()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
		}

        public override void Close( WindowCloseReason Reason )
        {

        }

        #region GetRadialMenuCurrentWidth_Scaled
        /// <summary>
        /// Gets the amount of horizontal space the radial menu will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetRadialMenuCurrentWidth_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 165f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        #region GetRadialMenuCurrentHeight_Scaled
        /// <summary>
        /// Gets the amount of vertical space the radial will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetRadialMenuCurrentHeight_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 185f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        #region GetXWidth
        public static float GetXWidth()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().x;
        }
        #endregion


        #region GetLeftPositionForOtherWindows
        public static float GetLeftPositionForOtherWindows()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceBottomLeftCorner().x;
        }
        #endregion

        #region GetYHeight
        public static float GetYHeight()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
        }
        #endregion

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            if ( Engine_Universal.GameLoop == null )
                return false;
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return false;//only render in the main game

            if ( Engine_HotM.GameStatus == MainGameStatus.MainMenu )
                return false;
            if ( VisCurrent.IsInPhotoMode )
                return false;
            if ( VisCurrent.ShouldDrawLoadingMenuBuildings || VisCurrent.IsShowingActualEvent )
                return false;
            if ( SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false )
                return false;
            if ( Engine_HotM.IsBigBannerBeingShown )
                return false;
            if ( SimCommon.CurrentSimpleChoice != null )
                return false;
            if ( Window_RewardWindow.Instance?.IsOpen ?? false )
                return false;
            if ( Window_NetworkNameWindow.Instance?.IsOpen ?? false )
                return false;
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( !FlagRefs.UITour3_TaskStack.DuringGameplay_IsTripped ) //directly before the radial menu
                return false;
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode != null && lowerMode.HideRadialMenu )
                return false;
            if ( Window_Debate.Instance?.IsOpen ?? false )
                return false;
            if ( SimCommon.ShouldBeShowingPostGoalScreen )
                return false;

            return true;
        }

        #region tLensLabel
        public class tLensLabel : TextAbstractBase
        {
            //todo disappear after a bit
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        {
                            CityLensType cityLens = SimCommon.CurrentCityLens;
                            if ( cityLens != null && (mainRadialLayout && !mainRadialLayout.IsSeeking) )
                                Buffer.AddRaw( cityLens.GetDisplayName() );
                        }
                        break;
                    case MainGameMode.TheEndOfTime:
                        {
                            EndOfTimeLensType endOfTimeLens = SimMetagame.CurrentEndOfTimeLens;
                            if ( endOfTimeLens != null && (mainRadialLayout && !mainRadialLayout.IsSeeking) )
                                Buffer.AddRaw( endOfTimeLens.GetDisplayName() );
                        }
                        break;
                }
            }
            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
            }
        }
        #endregion

        #region GetNPCCounts
        private static void GetNPCCounts( out int TotalNPCsToAct )
        {
            int total = 0;

            List<ISimNPCUnit> npcUnits = SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.GetDisplayList();
            for ( int i = 0; i < npcUnits.Count; i++ )
            {
                ISimNPCUnit unit = npcUnits[i];
                if ( unit.GetHasAnyActionItWantsTodo() && !unit.IsFullDead )
                    total++;
            }

            TotalNPCsToAct = total;
        }
        #endregion

        #region GetActionsCounts
        private static void GetActionsCounts( out int TotalActorsPossible, out int ActorsRemaining, out bool isReadyToEndTurn )
        {
            int total = 0;
            int remaining = 0;
            isReadyToEndTurn = true;

            if ( ResourceRefs.MentalEnergy.Current > 0 )
            {
                List<ISimMachineActor> actors = SimCommon.AllMachineActors.GetDisplayList();
                for ( int i = 0; i < actors.Count; i++ )
                {
                    ISimMachineActor actor = actors[i];
                    total++;
                    if ( !actor.GetIsBlockedFromAutomaticSelection( true ) && actor.CurrentStandby == StandbyType.None )
                        remaining++;
                }

                isReadyToEndTurn = remaining == 0;

                if ( remaining == 0 && ResourceRefs.MentalEnergy.Current > 1 )
                {
                    ISimMapActor actor = SimCommon.PredictNextUnfinishedActor( false, true, true );
                    if ( actor != null )
                        isReadyToEndTurn = false;
                }
            }

            TotalActorsPossible = total;
            ActorsRemaining = remaining;
        }
        #endregion

        #region GetCenterSprite
        private static IA5Sprite GetCenterSprite( out int TotalActorsPossible, out int ActorsRemaining, out int TotalNPCsToAct, out bool isReadyToEndTurn )
        {
            if ( !FlagRefs.UITour4_RadialMenu.DuringGameplay_IsTripped ) //the one directly before the toast
            {
                TotalActorsPossible = SimCommon.AllMachineActors.GetDisplayList().Count;
                ActorsRemaining = 0;
                TotalNPCsToAct = 0;
                isReadyToEndTurn = false;
                return null; //not valid until you have finished
            }

            if ( SimCommon.IsCurrentlyRunningSimTurn )
            {
                TotalActorsPossible = SimCommon.AllMachineActors.GetDisplayList().Count;
                ActorsRemaining = 0;
                TotalNPCsToAct = 0;
                isReadyToEndTurn = false;
                return IconRefs.Next_TurnChanging.Icon;
            }

            if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
            {
                TotalActorsPossible = 0;
                ActorsRemaining = 0;
                TotalNPCsToAct = 0;
                isReadyToEndTurn = false;
                return IconRefs.EndOfTime.Icon;
            }

            GetNPCCounts( out TotalNPCsToAct );
            GetActionsCounts( out TotalActorsPossible, out ActorsRemaining, out isReadyToEndTurn );

            if ( InputCaching.NotificationsAreDismissedBeforeSwitchingUnits )
            {
                if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                {
                    return SimCommon.KeyMessagesWaiting[0].GetToastIcon();
                }
                else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                {
                    return UnlockCoordinator.MinorCompletedToasts_MainThread[0].GetToastIcon();
                }
                else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                {
                    return UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].GetToastIcon();
                }
            }

            //if over cap on any of these
            if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt ||
                SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt ||
                SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt ||
                SimCommon.TotalBulkUnitSquadCapacityUsed > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt ||
                SimCommon.TotalCapturedUnitSquadCapacityUsed > MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt )
            {
                isReadyToEndTurn = false;
                return IconRefs.Next_OverCap.Icon;
            }

            if ( TotalNPCsToAct > 0 )
            {
                ISimMapActor nextNPC = SimCommon.PredictNextUnfinishedActor( false, true, false );

                if ( nextNPC == null )
                    return IconRefs.Next_NextHostileNPCUnit.Icon;
                else
                {
                    if ( nextNPC is ISimNPCUnit npcUnit )
                    {
                        if ( npcUnit.GetWillFireOnMachineUnitsBaseline() )
                            return IconRefs.Next_NextHostileNPCUnit.Icon;
                        else
                            return IconRefs.Next_NextAlliedNPCUnit.Icon;
                    }
                    else
                        return IconRefs.Next_NextHostileNPCUnit.Icon;
                }
            }

            if ( ActorsRemaining == 0 && isReadyToEndTurn )
            {
                if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                {
                    return SimCommon.KeyMessagesWaiting[0].GetToastIcon();
                }
                else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                {
                    return UnlockCoordinator.MinorCompletedToasts_MainThread[0].GetToastIcon();
                }
                else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                {
                    return UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].GetToastIcon();
                }

                if ( UnlockCoordinator.GetNeedsToAssignResearch() )
                    return IconRefs.NoTech.Icon;
                else if ( VisCommands.GetFirstProjectNeedingOutcome( out MachineProject projectToSet ) )
                    return projectToSet.Icon;
                return IconRefs.Next_NextTurn.Icon;
            }

            ISimMapActor nextActor = SimCommon.PredictNextUnfinishedActor( false, true, false );

            if ( nextActor == null )
                return IconRefs.Next_NoMoreUnits.Icon;
            else
            {
                if ( nextActor is ISimNPCUnit npcUnit )
                {
                    if ( npcUnit.GetWillFireOnMachineUnitsBaseline() )
                        return IconRefs.Next_NextHostileNPCUnit.Icon;
                    else
                        return IconRefs.Next_NextAlliedNPCUnit.Icon;
                }
                else
                {
                    if ( !SimCommon.GetHaveAllNPCsActed() )
                        return IconRefs.Next_TurnChanging.Icon;
                    else
                        return IconRefs.Next_NextMachineUnit.Icon;
                }
            }
        }
        #endregion

        #region WriteRadialCenterTooltip
        private static void WriteRadialCenterTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp )
        {
            if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                return; //not valid until you have finished

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            bool isTheEndOfTime = Engine_HotM.GameMode == MainGameMode.TheEndOfTime;

            if ( isTheEndOfTime )
            {
            }
            else 
            { 
                if ( InputCaching.NotificationsAreDismissedBeforeSwitchingUnits )
                {
                    if ( SimCommon.KeyMessagesWaiting.Count > 0 ||
                        UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 ||
                        UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                    {
                        if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                        {
                            if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                            {
                                novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                novel.Main.StartColor( ColorTheme.NarrativeColor );
                                SimCommon.KeyMessagesWaiting[0]?.RenderToastTopLineText( novel.Main, false );
                            }
                        }
                        else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                        {
                            IMinorCompletedToastItem item = UnlockCoordinator.MinorCompletedToasts_MainThread[0];
                            if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                            {
                                if ( item.GetWillOpenSomething() )
                                    novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                else
                                    novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToClose" );
                                novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );

                                novel.Main.StartColor( ColorTheme.NarrativeColor );
                                item.RenderToastTopLineText( novel.Main, false );
                            }
                        }
                        else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                        {
                            if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                            {
                                novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                novel.Main.StartColor( ColorTheme.NarrativeColor );
                                UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].RenderToastTopLineText( novel.Main, false );
                            }
                        }

                        return;
                    }
                }

                //if over cap
                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                {
                    int excess = MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt - SimCommon.TotalCapacityUsed_Androids;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Androids" ), DrawNextTo, Clamp,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                    {
                        novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Androids", excess );
                        novel.Main.AddFormat2( "MentalStrainWarning_Androids_Tooltip1", excess, MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                    }

                    return;
                }

                //if over cap
                if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
                {
                    int excess = MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt - SimCommon.TotalCapacityUsed_Mechs;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Mechs" ), DrawNextTo, Clamp,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                    {
                        novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Mechs", excess );
                        novel.Main.AddFormat2( "MentalStrainWarning_Mechs_Tooltip1", excess, MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                    }

                    return;
                }

                //if over cap
                if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt )
                {
                    int excess = MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt - SimCommon.TotalCapacityUsed_Vehicles;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Vehicles" ), DrawNextTo, Clamp,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.None ) )
                    {
                        novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Vehicles", excess );
                        novel.Main.AddFormat2( "MentalStrainWarning_Vehicles_Tooltip1", excess, MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                    }

                    return;
                }

                //if over cap
                if ( SimCommon.TotalBulkUnitSquadCapacityUsed > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt )
                {
                    int excess = MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt - SimCommon.TotalBulkUnitSquadCapacityUsed;

                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "BulkAndroids" ), DrawNextTo, Clamp,
                        TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.None ) )
                    {
                        novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_BulkUnits", excess );
                        novel.Main.AddFormat2( "MentalStrainWarning_BulkUnits_Tooltip1", excess, MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                    }

                    return;
                }

                //if over cap
                if ( SimCommon.TotalCapturedUnitSquadCapacityUsed > MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt )
                {
                    int excess = MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt - SimCommon.TotalCapturedUnitSquadCapacityUsed;
                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "CapturedUnits" ), DrawNextTo, Clamp,
                        TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.None ) )
                    {
                        novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_CapturedUnits", excess );
                        novel.Main.AddFormat2( "MentalStrainWarning_CapturedUnits_Tooltip1", excess, MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                        novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                    }

                    return;
                }

                GetActionsCounts( out int TotalActorsPossible, out int ActorsRemaining, out bool isReadyToEndTurn );
                                
                {
                    ISimMapActor nextActor = SimCommon.PredictNextUnfinishedActor( false, true, false );
                    if ( nextActor != null )
                    {
                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                        {
                            novel.Icon = nextActor.GetShapeIcon();
                            novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "RadialMenu_ClickHereToSelect", ColorTheme.Gray )
                                .AddSpriteStyled_NoIndent( nextActor.GetShapeIcon(), AdjustedSpriteStyle.InlineLarger1_2 ).Space1x().AddRaw( nextActor.GetDisplayName() );
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );

                            novel.Main.StartColor( ColorTheme.NarrativeColor );
                            if ( ActorsRemaining == 0 )
                                novel.Main.AddFormat1( "RadialMenu_StillHaveMentalEnergy1", InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn() ).Line()
                                    .AddFormat2( "RadialMenu_StillHaveMentalEnergy2", InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                                    InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn(), ColorTheme.PurpleDim ).Line();
                            else
                            {
                                novel.Main.AddBoldLangAndAfterLineItemHeader( "RadialMenu_UnitsRemaining", ColorTheme.DataLabelWhite );
                                novel.Main.AddFormat2( "OutOF", ActorsRemaining.ToString(), TotalActorsPossible.ToString(), ColorTheme.DataBlue ).Line();
                            }
                        }
                    }
                    else
                    {
                        if ( ActorsRemaining == 1 )
                        {
                            if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                            {
                                novel.TitleUpperLeft.AddLang( "RadialMenu_FinishFirst");
                                novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                            }
                        }
                        else if ( ActorsRemaining == 0 )
                        {
                            if ( SimCommon.IsCurrentlyRunningSimTurn )
                            { } //turn is changing...
                            else
                            {
                                if ( SimCommon.KeyMessagesWaiting.Count > 0 ||
                                    UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 ||
                                    UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                                {
                                    if ( SimCommon.KeyMessagesWaiting.Count > 0 )
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                            novel.Main.StartColor( ColorTheme.NarrativeColor );
                                            SimCommon.KeyMessagesWaiting[0]?.RenderToastTopLineText( novel.Main, false );
                                        }
                                    }
                                    else if ( UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 )
                                    {
                                        IMinorCompletedToastItem item = UnlockCoordinator.MinorCompletedToasts_MainThread[0];
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            if ( item.GetWillOpenSomething() )
                                                novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                            else
                                                novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToClose" );
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );

                                            novel.Main.StartColor( ColorTheme.NarrativeColor );
                                            item.RenderToastTopLineText( novel.Main, false );
                                        }
                                    }
                                    else if ( UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 )
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToOpen" );
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                            novel.Main.StartColor( ColorTheme.NarrativeColor );
                                            UnlockCoordinator.UnopenedMysteryUnlocks_MainThread[0].RenderToastTopLineText( novel.Main, false );
                                        }
                                    }
                                }
                                else if ( UnlockCoordinator.GetNeedsToAssignResearch() )
                                {
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                    {
                                        novel.TitleUpperLeft.AddLang( "MustChooseResearchBeforeEndingTurn" );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                    }
                                }
                                else if ( VisCommands.GetFirstProjectNeedingOutcome( out MachineProject projectToSet ) )
                                {
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                    {
                                        novel.TitleUpperLeft.AddRaw( projectToSet.GetDisplayName() );
                                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                        novel.Main.StartColor( ColorTheme.NarrativeColor )
                                            .AddFormat1( "MustChooseDesiredOutcomeForProjectBeforeEndingTurn", projectToSet.GetDisplayName() );
                                    }
                                }
                                else
                                {
                                    if ( isReadyToEndTurn )
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddLang( "RadialMenu_ClickHereToEndTurn" );
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                        }
                                    }
                                    else
                                    {
                                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "CenterTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
                                        {
                                            novel.TitleUpperLeft.AddFormat1( "RadialMenu_StillHaveMentalEnergyHeader", ResourceRefs.MentalEnergy.Current );
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );
                                            novel.Main.StartColor( ColorTheme.NarrativeColor )
                                                .AddFormat1( "RadialMenu_StillHaveMentalEnergy1", InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn() ).Line()
                                                .AddFormat2( "RadialMenu_StillHaveMentalEnergy2", InputCaching.GetGetHumanReadableKeyComboForJumpToNextActorOrEndTurn(),
                                                InputCaching.GetGetHumanReadableKeyComboForGoStraightToNextTurn(), ColorTheme.PurpleDim );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region WriteTurnTooltip
        private static void WriteTurnTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp )
        {
            bool isTheEndOfTime = Engine_HotM.GameMode == MainGameMode.TheEndOfTime;

            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "TurnTooltip" ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
            {
                if ( isTheEndOfTime )
                {
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "RadialMenu_MetaTime" )
                        .AddRaw( SimMetagame.TotalSeconds.ToString_TimePossiblyMinutesAndHoursFromSeconds() ).Line();
                }
                else
                    novel.TitleUpperLeft.AddLangAndAfterLineItemHeader( "RadialMenu_Turn" ).AddRaw( SimCommon.Turn.ToStringWholeBasic() );
                novel.Main.StartColor( ColorTheme.NarrativeColor );

                if ( SimMetagame.CurrentChapter != null )
                {
                    novel.TitleLowerLeft.AddRaw( SimMetagame.CurrentChapter.GetDisplayName() );

                    //if ( InputCaching.ShouldShowDetailedTooltips )
                    {
                        novel.Main.AddRaw( SimMetagame.CurrentChapter.GetDescription() ).Line();
                        novel.Main.AddRaw( SimMetagame.CurrentChapter.StrategyTip.Text, ColorTheme.PurpleDim ).Line();
                    }
                }

                novel.Main.StartStyleLineHeightA();
                
                //if ( InputCaching.ShouldShowDetailedTooltips || SimCommon.IsCheatTimeline )
                {
                    novel.Main.AddBoldLangAndAfterLineItemHeader( "RadialMenu_GameMode", ColorTheme.DataLabelWhite );
                    novel.Main.AddLang( SimCommon.IsCheatTimeline ? "RadialMenu_GameMode_CheatMode" : "RadialMenu_GameMode_Strategic", ColorTheme.DataBlue ).Line();
                }

                if ( !isTheEndOfTime )
                {
                    if ( SimCommon.IsFogOfWarDisabled )
                        novel.Main.AddLang( "FogOfWarDisabled", ColorTheme.DataBlue ).Line();

                    novel.Main.AddBoldLangAndAfterLineItemHeader( "RadialMenu_CityName", ColorTheme.DataLabelWhite );
                    novel.Main.AddRaw( SimCommon.CityName, ColorTheme.DataBlue ).Line();

                    novel.Main.AddBoldLangAndAfterLineItemHeader( "RadialMenu_TimelineTime", ColorTheme.DataLabelWhite );
                    novel.Main.AddRaw( SimCommon.CurrentTimeline.SecondsInTimeline.ToString_TimePossiblyMinutesAndHoursFromSeconds(), ColorTheme.DataBlue ).Line();
                }

                novel.Main.EndLineHeight();
            }
        }
        #endregion

        #region tTurn
        public class tTurn : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddLang( "RadialMenu_Turn" ).Space1x();
                Buffer.AddRaw( SimCommon.Turn.ToStringWholeBasic() );
            }

            public override bool GetShouldBeHidden()
            {
                return Engine_HotM.GameMode == MainGameMode.TheEndOfTime;
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                WriteTurnTooltip( broadElementToUse, SideClamp.AboveOnly );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bCenterButton.CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tChapter
        public class tChapter : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( SimMetagame.CurrentChapterNumber > 1 && SimCommon.Turn % 2 == 0 )
                    Buffer.AddRaw( SimCommon.CityName );
                else
                    Buffer.AddRaw( SimMetagame.CurrentChapter.GetDisplayName() );
            }

            public override bool GetShouldBeHidden()
            {
                return Engine_HotM.GameMode == MainGameMode.TheEndOfTime;
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                WriteTurnTooltip( broadElementToUse, SideClamp.AboveOnly );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bCenterButton.CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tMentalEnergySingle
        public class tMentalEnergySingle : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return;

                if ( SimCommon.IsCurrentlyRunningSimTurn )
                    Buffer.StartSize80().AddLang( "RadialMenu_TurnCalculating" );
                else
                {
                    ISimMapActor nextActor = SimCommon.PredictNextUnfinishedActor( false, true, false );
                    if ( nextActor != null && nextActor is ISimNPCUnit npcUnit )
                        Buffer.StartSize80().StartStyleLineHeightA().AddLang( "RadialMenu_NPCActionPhase", ColorTheme.HeaderGoldOrangeDark );
                    else if ( !SimCommon.GetHaveAllNPCsActed() )
                        Buffer.StartSize80().StartStyleLineHeightA().AddLang( "RadialMenu_NPCsFinishingMovement", ColorTheme.HeaderGoldOrangeDark );
                    else
                    {

                        Buffer.AddFormat2( "OutOF", ResourceRefs.MentalEnergy.Current, MathRefs.MentalEnergyPerTurn.DuringGameplay_CurrentInt ).Line();
                        Buffer.StartSize125().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_4, ColorTheme.HeaderGoldMoreRich );
                    }
                }
            }

            public override bool GetShouldBeHidden()
            {
                return !GetIsShowingSingularMentalEnergy();
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( SimCommon.IsCurrentlyRunningSimTurn )
                {
                    //do nothing
                }
                else
                {
                    ISimMapActor nextActor = SimCommon.PredictNextUnfinishedActor( false, true, false );
                    if ( nextActor != null && nextActor is ISimNPCUnit npcUnit )
                    {
                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "tMentalEnergySingle" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "RadialMenu_NPCActionPhase_Header" );
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );

                            novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "RadialMenu_NPCActionPhase_Details1" ).Line()
                                .AddLang( "RadialMenu_NPCActionPhase_Details2", ColorTheme.PurpleDim );
                        }
                    }
                    else if ( !SimCommon.GetHaveAllNPCsActed() )
                    {
                        if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "tMentalEnergySingle" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                        {
                            novel.TitleUpperLeft.AddLang( "RadialMenu_NPCsFinishingMovement_Header" );
                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "JumpToNextActorOrEndTurn" );

                            novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "RadialMenu_NPCsFinishingMovement_Details1" ).Line()
                                .AddLang( "RadialMenu_NPCsFinishingMovement_Details2", ColorTheme.PurpleDim );
                        }
                    }
                    else
                    {
                        ResourceRefs.MentalEnergy.WriteResourceTooltip( broadElementToUse, SideClamp.AboveOnly, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, TooltipExtraText.None );
                    }
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bCenterButton.CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tMentalEnergyDual
        public class tMentalEnergyDual : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                Buffer.AddFormat2( "OutOF", ResourceRefs.MentalEnergy.Current, MathRefs.MentalEnergyPerTurn.DuringGameplay_CurrentInt ).Line();
                Buffer.StartSize125().AddSpriteStyled_NoIndent( ResourceRefs.MentalEnergy.Icon, AdjustedSpriteStyle.InlineLarger1_4, ColorTheme.HeaderGoldMoreRich );
            }

            public override bool GetShouldBeHidden()
            {
                return GetIsShowingSingularMentalEnergy();
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                ResourceRefs.MentalEnergy.WriteResourceTooltip( broadElementToUse, SideClamp.AboveOnly, TooltipShadowStyle.None, TooltipInstruction.ForExistingObject, TooltipExtraText.None );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bCenterButton.CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region tActionPoints
        public class tActionPoints : TextAbstractBase
        {
            public override void GetTextToShowFromVolatile( ArcenUI_Text Tex, ArcenDoubleCharacterBuffer Buffer )
            {
                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor != null )
                {
                    Buffer.AddFormat2( "OutOF", machineActor.CurrentActionPoints, machineActor.GetActorDataCurrent( ActorRefs.ActorMaxActionPoints, true ) ).Line();
                    Buffer.StartSize125().AddSpriteStyled_NoIndent( ActorRefs.ActorMaxActionPoints.Icon, AdjustedSpriteStyle.InlineLarger1_4, ColorTheme.HeaderGoldMoreRich );
                }
            }

            public override bool GetShouldBeHidden()
            {
                return GetIsShowingSingularMentalEnergy();
            }

            public override void OnUpdate() { }

            public override void HandleMouseover()
            {
                if ( !(Engine_HotM.SelectedActor is ISimMachineActor machineActor) )
                    return;

                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartBasicTooltip( TooltipID.Create( "RadialMenu", "ActionPoint" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                {
                    novel.Icon = ActorRefs.ActorMaxActionPoints.Icon;
                    novel.IconColorHex = ColorTheme.HeaderGoldMoreRich;

                    novel.TitleUpperLeft.AddLang( "ActionPoint_Full" );
                    novel.TitleLowerLeft.AddLang( "PerUnitResource_Prefix" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLangWithFirstLineBold( "ActionPoint_Explanation" ).Line()
                        .AddLang( "ActionPoint_StrategyTip", ColorTheme.PurpleDim );

                    novel.FrameTitle.AddLangAndAfterLineItemHeader( "ActionPoint_Current" )
                        .AddFormat2( "OutOF", machineActor.CurrentActionPoints.ToStringWholeBasic(), machineActor.MaxActionPoints.ToStringWholeBasic() );
                    novel.FrameBody.AddLang( "ActionPoint_Footnote", ColorTheme.NarrativeColor );
                }
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                bCenterButton.CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }
        }
        #endregion

        #region bSelectedUnitIcon
        public class bSelectedUnitIcon : ButtonAbstractBaseWithImage
        {
            public static bSelectedUnitIcon Instance;
            public bSelectedUnitIcon() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor != null )
                    VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( machineActor.GetDrawLocation(), false );
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {

            }

            public override bool GetShouldBeHidden()
            {
                if ( GetIsShowingSingularMentalEnergy() )
                    return true;

                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                return machineActor == null;
            }

            //private bool lastWasNone = false;
            public static IA5Sprite lastCenterSprite = null;
            private bool lastHadNullSprite = false;

            private bool wasHovered = false;
            public override void OnUpdateSubSub()
            {
                ISimMachineActor machineActor = Engine_HotM.SelectedActor as ISimMachineActor;
                if ( machineActor != null )
                    lastCenterSprite = machineActor.GetShapeIcon();
                else
                    lastCenterSprite = null;

                this.SetSpriteIfNeeded( lastCenterSprite?.GetSpriteForUI() );

                bool isHovered = this.Element.LastHadMouseWithin;
                if ( isHovered != wasHovered || lastHadNullSprite != (lastCenterSprite == null) )
                {
                    lastHadNullSprite = lastCenterSprite == null;
                    wasHovered = isHovered;
                    this.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR : ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                }
            }

            public override void HandleMouseover()
            {
                ISimMapActor mapActor = Engine_HotM.SelectedActor;
                if ( mapActor != null )
                    mapActor.RenderTooltip( customParentElementRoot, SideClamp.LeftOrRight, TooltipShadowStyle.None, false, ActorTooltipExtraData.None, TooltipExtraRules.None );
            }
        }
        #endregion

        #region bSandboxIcon
        public class bSandboxIcon : ButtonAbstractBaseWithImage
        {
            public static bSandboxIcon Instance;
            public bSandboxIcon() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                VisCommands.ToggleDeepCheatSidebar();
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {

            }

            public override bool GetShouldBeHidden()
            {
                return !SimCommon.IsCheatTimeline;
            }

            private bool wasHovered = false;
            public override void OnUpdateSubSub()
            {
                bool isHovered = this.Element.LastHadMouseWithin;
                if ( isHovered != wasHovered )
                {
                    wasHovered = isHovered;
                    if ( isHovered )
                        this.image.material = ( this.Element as ArcenUI_Button ).ReferenceImagesHoverMaterials[0];
                    else
                        this.image.material = (this.Element as ArcenUI_Button).ReferenceImagesNormalMaterials[0];
                }
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "RadialMenu", "CheatMode" ), broadElementToUse, SideClamp.AboveOrBelow, TooltipNovelWidth.Simple ) )
                {
                    novel.Icon = IconRefs.Header_CheatMode.Icon;
                    novel.IconColorHex = ColorTheme.HealingGreen;

                    novel.TitleUpperLeft.AddLang( "HeaderBar_Cheat_Top" );
                    novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "HeaderBar_Cheat_Explanation" ).Line();

                    novel.Main.AddLeftClickFormat( "HeaderBar_Cheat_ClickHere", ColorTheme.SoftGold );
                }
            }
        }
        #endregion

        #region bCenterButton
        public class bCenterButton : ButtonAbstractBaseWithImage
        {
            public static bCenterButton Instance;
            public bCenterButton() { if ( Instance == null ) Instance = this; }

            public static void CenterMouseHandle( MouseHandlingInput input )
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return;

                VisCommands.HandleGoToNextTurnOrActor( false );
            }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                CenterMouseHandle( input );
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return;
                if ( lastCenterSprite != IconRefs.Next_NextMachineUnit.Icon )
                    return;

                GetActionsCounts( out int TotalActorsPossible, out int ActorsRemaining, out bool isReadyToEndTurn );

                Buffer.AddFormat2( "OutOF", ActorsRemaining.ToString(), TotalActorsPossible.ToString(),
                    ActorsRemaining <= 0 && isReadyToEndTurn ? ColorTheme.HeaderGoldMoreRich : string.Empty ).Line();
            }

            private bool lastWasNone = false;
            public static IA5Sprite lastCenterSprite = null;
            private bool lastHadNullSprite = false;

            private bool wasOffsetForNextMachine = false;

            private bool wasHovered = false;
            public override void OnUpdateSubSub()
            {
                lastCenterSprite = GetCenterSprite( out int TotalActorsPossible, out int ActorsRemaining, out int TotalNPCsToAct, out bool isReadyToEndTurn );

                this.SetSpriteIfNeeded( lastCenterSprite?.GetSpriteForUI() );
                bool currentIsNone = ActorsRemaining <= 0 && TotalNPCsToAct <= 0 && isReadyToEndTurn;

                if ( lastCenterSprite == IconRefs.Next_NextMachineUnit.Icon )
                {
                    if ( !wasOffsetForNextMachine )
                    {
                        wasOffsetForNextMachine = true;
                        this.Element.RelevantRect.anchoredPosition = new Vector3( 0, 7.5f, 0 );
                    }
                }
                else
                {
                    if ( wasOffsetForNextMachine )
                    {
                        wasOffsetForNextMachine = false;
                        this.Element.RelevantRect.anchoredPosition = new Vector3( 0, 1, 0 );
                    }
                }

                if ( SimCommon.KeyMessagesWaiting.Count > 0 ||
                    UnlockCoordinator.MinorCompletedToasts_MainThread.Count > 0 ||
                    UnlockCoordinator.UnopenedMysteryUnlocks_MainThread.Count > 0 ||
                    WorldSaveLoad.IsLoadingAtTheMoment ||
                    UnlockCoordinator.GetNeedsToAssignResearch() ||
                    VisCommands.GetFirstProjectNeedingOutcome( out _ ) ||
                    Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    currentIsNone = false;

                if ( currentIsNone != lastWasNone )
                {
                    lastWasNone = currentIsNone;
                    image.material = currentIsNone ? ( this.Element as ArcenUI_Button).RelatedMaterials[0] : null;
                }

                bool isHovered = this.Element.LastHadMouseWithin;
                if ( isHovered != wasHovered || lastHadNullSprite != ( lastCenterSprite == null ) )
                {
                    lastHadNullSprite = lastCenterSprite == null;
                    wasHovered = isHovered;
                    if ( lastHadNullSprite )
                        this.image.color = ColorMath.Transparent;
                    else
                        this.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR : ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                }
            }

            public override void HandleMouseover()
            {
                WriteRadialCenterTooltip( broadElementToUse, SideClamp.AboveOrBelow );
            }
        }
        #endregion

        #region bBuildMode
        public class bBuildMode : ButtonAbstractBaseWithImage
        {
            public static bBuildMode Instance;
            public bBuildMode() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                    return MouseHandlingResult.None;

                VisCommands.ToggleBuildMode();

                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            { }

            public override bool GetShouldBeHidden()
            {
                LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                if ( lowerMode != null )
                {
                    switch ( lowerMode.ID )
                    {
                        case "ZodiacNeuronScene":
                            return false;
                    }
                    return true;
                }

                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        break;
                    case MainGameMode.TheEndOfTime:
                        return true; //never show it in these modes
                }

                return false;
            }

            private bool lastWasHighlighted = false;

            private bool wasHovered = false;
            private bool wasGray = false;
            public override void OnUpdateSubSub()
            {
                if ( !FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                {
                    this.SetSpriteIfNeeded( IconRefs.AbilityLocked.Icon.GetSpriteForUI() );
                    this.image.color = ColorMath.DarkerGray;
                    wasGray = true;
                }
                else
                {
                    this.SetSpriteIfNeeded( IconRefs.Header_BuildMode.Icon.GetSpriteForUI() );
                    bool currentIsHighlighted = (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "BuildMode";

                    if ( currentIsHighlighted != lastWasHighlighted )
                    {
                        lastWasHighlighted = currentIsHighlighted;
                        image.material = currentIsHighlighted ? (this.Element as ArcenUI_Button).RelatedMaterials[0] : null;
                    }

                    bool isHovered = this.Element.LastHadMouseWithin;
                    if ( isHovered != wasHovered || wasGray )
                    {
                        wasGray = false;
                        wasHovered = isHovered;
                        this.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR : ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                    }

                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.BuildMenuButton && !this.GetShouldBeHidden() && NoteLog.TemporaryNotes.Count == 0 )
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLang( "IndicateBuildMenuButton_Text" );
                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateBuildMenuButton_Text", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.BottomRight );
                    }
                }
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( !FlagRefs.HasFiguredOutStructureConstruction.DuringGameplay_IsTripped )
                {
                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "bBuildMode" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                    {
                        novel.Icon = IconRefs.AbilityLocked.Icon;
                        novel.TitleUpperLeft.AddLang( "Locked" );
                        novel.TitleLowerLeft.AddLang( "Ability_Prefix" );
                        novel.Main.AddLang( "AbilityLocked_Details" );
                    }
                }
                else
                {
                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "bBuildMode" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                    {
                        novel.Icon = IconRefs.Header_BuildMode.Icon;

                        novel.TitleUpperLeft.AddLang( "HeaderBar_BuildMode_Top" );
                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleBuildMode" );

                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "HeaderBar_BuildMode_Explanation" ).Line();
                    }
                }
            }
        }
        #endregion

        #region bCommandMode
        public class bCommandMode : ButtonAbstractBaseWithImage
        {
            public static bCommandMode Instance;
            public bCommandMode() { if ( Instance == null ) Instance = this; }

            public override MouseHandlingResult HandleClick_Subclass( MouseHandlingInput input )
            {
                if ( !FlagRefs.HasFiguredOutCommandMode.DuringGameplay_IsTripped )
                    return MouseHandlingResult.None;

                VisCommands.ToggleCommandMode();
                return MouseHandlingResult.None;
            }

            public override void GetTextToShowFromVolatile( ArcenDoubleCharacterBuffer Buffer )
            { }

            public override bool GetShouldBeHidden()
            {
                if ( Engine_HotM.CurrentLowerMode != null )
                    return true;

                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                        break;
                    case MainGameMode.CityMap:
                        break;
                    case MainGameMode.TheEndOfTime:
                        return true; //never show it in these modes
                }

                return false;
            }

            private bool lastWasHighlighted = false;

            private bool wasHovered = false;
            private bool wasGray = false;
            public override void OnUpdateSubSub()
            {
                if ( !FlagRefs.HasFiguredOutCommandMode.DuringGameplay_IsTripped )
                {
                    this.SetSpriteIfNeeded( IconRefs.AbilityLocked.Icon.GetSpriteForUI() );
                    this.image.color = ColorMath.DarkerGray;
                    wasGray = true;
                }
                else
                {
                    this.SetSpriteIfNeeded( IconRefs.Header_CommandMode.Icon.GetSpriteForUI() );
                    bool currentIsHighlighted = (Engine_HotM.SelectedMachineActionMode?.ID ?? string.Empty) == "CommandMode";

                    if ( currentIsHighlighted != lastWasHighlighted )
                    {
                        lastWasHighlighted = currentIsHighlighted;
                        image.material = currentIsHighlighted ? (this.Element as ArcenUI_Button).RelatedMaterials[0] : null;
                    }

                    bool isHovered = this.Element.LastHadMouseWithin;
                    if ( isHovered != wasHovered || wasGray )
                    {
                        wasGray = false;
                        wasHovered = isHovered;
                        this.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR : ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                    }

                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.CommandModeButton && !this.GetShouldBeHidden() && NoteLog.TemporaryNotes.Count == 0 )
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLang( "IndicateCommandMenuButton_Text" );
                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateCommandMenuButton_Text", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.BottomRight );
                    }
                }
            }

            public override void HandleMouseover()
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                if ( !FlagRefs.HasFiguredOutCommandMode.DuringGameplay_IsTripped )
                {
                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "bCommandMode" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                    {
                        novel.Icon = IconRefs.AbilityLocked.Icon;
                        novel.TitleUpperLeft.AddLang( "Locked" );
                        novel.TitleLowerLeft.AddLang( "Ability_Prefix" );
                        novel.Main.AddLang( "AbilityLocked_Details" );
                    }
                }
                else
                {
                    if ( novel.TryStartBasicTooltip( TooltipID.Create( "Radial", "bCommandMode" ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                    {
                        novel.Icon = IconRefs.Header_CommandMode.Icon;

                        novel.TitleUpperLeft.AddLang( "HeaderBar_CommandMode_Top" );
                        novel.TitleLowerLeft.AddTooltipHotkeySecondLine( "ToggleCommandMode" );

                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddLang( "HeaderBar_CommandMode_Explanation" ).Line();
                    }
                }
            }
        }
        #endregion

        #region GetIsShowingSingularMentalEnergy
        public static bool GetIsShowingSingularMentalEnergy()
        {
            if ( Engine_HotM.CurrentLowerMode != null )
                return true;

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                case MainGameMode.CityMap:
                    {
                        if ( SimCommon.IsCurrentlyRunningSimTurn )
                            return true;
                        if ( Engine_HotM.SelectedActor is ISimMachineActor machineActor )
                        {
                            if ( SimCommon.PredictNextNPCActorUnfinished() == null )
                                return false;
                        }
                    }
                    break;
            }
            return true;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bRadialIcon> bRadialIconPool;
        private static ButtonAbstractBase.ButtonPool<bOuterRadialIcon> bOuterRadialIconPool;
        private static ButtonAbstractBase.ButtonPool<bTimedNotificationButton> bTimedNotificationButtonPool;

        private static RadialLayoutManger_FocalCircular mainRadialLayout;
        private static RadialLayoutManger_ExpandingArc outerRadialLayout;

        private static ArcenUI_Element customParentElementRoot;
        private static ArcenUI_Element dLensFocusElementRoot;
        private static ArcenUI_Element broadElementToUse;

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            private bool wasShowingSingular = true;
            public override void OnUpdate()
            {
                this.WindowController.myScale = 0.95f * GameSettings.Current.GetFloat( "Scale_RadialMenu" );
                this.WindowController.ExtraOffsetX = -(Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled() );

                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bRadialIcon.Original != null && dLensFocus.Instance?.Element != null )
                    {
                        hasGlobalInitialized = true;
                        bRadialIconPool = new ButtonAbstractBase.ButtonPool<bRadialIcon>( bRadialIcon.Original, 10, "bRadialIcon" );
                        bTimedNotificationButtonPool = new ButtonAbstractBase.ButtonPool<bTimedNotificationButton>( bTimedNotificationButton.Original, 10, "bTimedNotificationButton" );
                        bOuterRadialIconPool = new ButtonAbstractBase.ButtonPool<bOuterRadialIcon>( bOuterRadialIcon.Original, 10, "bOuterRadialIcon" );

                        mainRadialLayout = bRadialIcon.Original.Element.GetComponentInParent<RadialLayoutManger_FocalCircular>( true );
                        outerRadialLayout = bOuterRadialIcon.Original.Element.GetComponentInParent<RadialLayoutManger_ExpandingArc>( true );

                        mainRadialLayout.seekSpeed = 600f;

                        customParentElementRoot = this.Element;
                        broadElementToUse = this.Element;
                        dLensFocusElementRoot = dLensFocus.Instance.Element;
                    }
                }
                #endregion

                this.UpdateIcons();

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour4_RadialMenu )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "UITour_RadialMenu_Text1" )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_RadialMenu_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 4, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour4_RadialMenu", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.Right );
                }

                UpdateBottomPlate();

                if ( dLensFocus.Instance?.GetShouldBeHidden()??false )
                    broadElementToUse = customParentElementRoot;
                else
                    broadElementToUse = dLensFocusElementRoot;
            }

            private void UpdateBottomPlate()
            {
                bool showSingular = GetIsShowingSingularMentalEnergy();
                if ( showSingular != wasShowingSingular && this.Element != null )
                {
                    wasShowingSingular = showSingular;
                    this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[showSingular ? 1 : 0];
                }
            }

            public void UpdateIcons()
            {
                if ( !hasGlobalInitialized )
                    return;

                bRadialIconPool.Clear( 5 );
                bTimedNotificationButtonPool.Clear( 5 );
                bOuterRadialIconPool.Clear( 5 );

                #region View Types
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.Streets:
                    case MainGameMode.CityMap:
                        {
                            int lensIndex = 0;
                            int selectedLensIndex = 0;
                            foreach ( CityLensType lensType in CityLensTypeTable.Instance.Rows )
                            {
                                if ( !lensType.GetIsLensVisible() )
                                    continue;

                                if ( SimCommon.CurrentCityLens == lensType )
                                    selectedLensIndex = lensIndex;

                                DrawCityLens( lensType );
                                lensIndex++;
                            }

                            //reorder things in mainRadialLayout by selectedLensIndex
                            mainRadialLayout.DesiredFocusedIndex = selectedLensIndex;
                        }
                        break;
                    case MainGameMode.TheEndOfTime:
                        {
                            int lensIndex = 0;
                            int selectedLensIndex = 0;
                            foreach ( EndOfTimeLensType lensType in EndOfTimeLensTypeTable.Instance.Rows )
                            {
                                if ( !lensType.GetIsLensVisible() )
                                    continue;

                                if ( SimMetagame.CurrentEndOfTimeLens == lensType )
                                    selectedLensIndex = lensIndex;

                                DrawEndOfTimeLens( lensType );
                                lensIndex++;
                            }

                            //reorder things in mainRadialLayout by selectedLensIndex
                            mainRadialLayout.DesiredFocusedIndex = selectedLensIndex;
                        }
                        break;
                }
                #endregion

                #region Outer Radial Layout
                if ( FlagRefs.HasUnlockedOuterRadialButtons.DuringGameplay_IsTripped && FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                {
                    int outerRadialItems = 0;

                    foreach ( OuterRadialOption outerRadial in OuterRadialOptionTable.Instance.Rows )
                    {
                        if ( !outerRadial.GetIsOptionAvailable() )
                            continue;

                        DrawOuterRadialOption( outerRadialItems, outerRadial );
                        outerRadialItems++;
                        if ( outerRadialItems >= OuterRadialArcSizesByItemCount.Length )
                            break;
                    }

                    outerRadialLayout.Group.Arc = OuterRadialArcSizesByItemCount[outerRadialItems];
                }
                #endregion

                this.HandleTimedNotifications();
            }

            public static readonly float[] OuterRadialArcSizesByItemCount = new float[] { 62.7f, 62.7f,
                29f, //2
                46f, //3
                58.2f, //4
                75f, //5
                88.6f, //6
            };

            #region DrawCityLens
            private void DrawCityLens( CityLensType lensType )
            {
                if ( lensType == SimCommon.CurrentCityLens )
                {
                    if ( lensType != null && lensType.ShowContemplations.Display )
                        FlagRefs.HasAlreadyIndicatedMapRadialContemplation.TripIfNeeded();
                }

                bRadialIcon icon = bRadialIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.SetSpriteIfNeeded( lensType.Icon.GetSpriteForUI() );
                    if ( icon.image != null )
                    {
                        bool isHovered = icon.Element?.LastHadMouseWithin??false;
                        if ( lensType.GetIsLensAvailable( true ) )
                        {
                            if ( SimCommon.CurrentCityLens == lensType )
                            {
                                icon.SetStyle( RadialIconStyle.Selected );
                                icon.SetLargeSpriteIfNeeded( lensType.Icon.GetSpriteForUI() );
                            }
                            else
                            {
                                icon.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR :
                                    ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                                icon.SetStyle( RadialIconStyle.Normal );
                            }
                        }
                        else
                        {
                            icon.image.color = isHovered ? ColorRefs.RadialButtonDisabledHovered.ColorWithoutHDR :
                                ColorRefs.RadialButtonDisabled.ColorWithoutHDR;
                            icon.SetStyle( RadialIconStyle.Disabled );
                        }
                    }

                    //no need to apply any position, the layout group will handle that
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                //no text to show

                                switch (SharedRenderManagerData.CurrentIndicator)
                                {
                                    case Indicator.RadialStreetSense:
                                        if ( lensType != null && lensType.ShowAllStreetSense.Display && NoteLog.TemporaryNotes.Count == 0 )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateStreetSenseRadialButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( lensType ), element, SideClamp.Any, TooltipArrowSide.BottomRight );
                                        }
                                        break;
                                    case Indicator.RadialInvestigation:
                                        if ( lensType != null && lensType.ShowInvestigations.Display && NoteLog.TemporaryNotes.Count == 0 )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateInvestigationRadialButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( lensType ), element, SideClamp.Any, TooltipArrowSide.BottomRight );
                                        }
                                        break;
                                    case Indicator.MapRadialContemplation:
                                        if ( lensType != null && lensType.ShowContemplations.Display && NoteLog.TemporaryNotes.Count == 0 )
                                        {
                                            ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                            tooltipBuffer.AddLang( "IndicateContemplateRadialButton_Text" );
                                            TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( lensType ), element, SideClamp.Any, TooltipArrowSide.BottomRight );
                                        }
                                        break;
                                }
                                
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    lensType.HandleLensTooltip( broadElementToUse, SideClamp.AboveOnly, TooltipShadowStyle.None );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        SimCommon.SetCurrentCityLensIfAvailable( lensType );

                                        switch ( SharedRenderManagerData.CurrentIndicator )
                                        {
                                            case Indicator.RadialStreetSense:
                                                break;
                                            case Indicator.MapRadialContemplation:
                                                if ( lensType != null && lensType.ShowContemplations.Display )
                                                    FlagRefs.HasAlreadyIndicatedMapRadialContemplation.TripIfNeeded();
                                                break;
                                        }
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        lensType.ExtraCodeOnRightClick?.Implementation.HandleCityLensRightClick( lensType.ExtraCodeOnRightClick, lensType );
                                    }
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region DrawEndOfTimeLens
            private void DrawEndOfTimeLens( EndOfTimeLensType lensType )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                bRadialIcon icon = bRadialIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.SetSpriteIfNeeded( lensType.Icon.GetSpriteForUI() );
                    if ( icon.image != null )
                    {
                        bool isHovered = icon.Element?.LastHadMouseWithin ?? false;
                        if ( lensType.GetIsLensAvailable() )
                        {
                            if ( SimMetagame.CurrentEndOfTimeLens == lensType )
                            {
                                icon.SetStyle( RadialIconStyle.Selected );
                                icon.SetLargeSpriteIfNeeded( lensType.Icon.GetSpriteForUI() );
                            }
                            else
                            {
                                icon.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR :
                                    ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                                icon.SetStyle( RadialIconStyle.Normal );
                            }
                        }
                        else
                        {
                            icon.image.color = isHovered ? ColorRefs.RadialButtonDisabledHovered.ColorWithoutHDR :
                                ColorRefs.RadialButtonDisabled.ColorWithoutHDR;
                            icon.SetStyle( RadialIconStyle.Disabled );
                        }
                    }

                    //no need to apply any position, the layout group will handle that
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                //no text to show

                                if ( !FlagRefs.HasAlreadyIndicatedTheEndOfTimeCreatorMode.DuringGameplay_IsTripped && lensType.ID == "Creator" &&
                                    SimMetagame.CurrentEndOfTimeLens != lensType )
                                {
                                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                    tooltipBuffer.AddLang( "IndicateEndOfTimeRadialLens_Text" );
                                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( lensType ), element, SideClamp.Any, TooltipArrowSide.BottomRight );
                                }

                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( lensType ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                                    {
                                        novel.Icon = lensType.Icon;

                                        novel.TitleUpperLeft.AddRaw( lensType.GetDisplayName() );
                                        novel.TitleLowerLeft.AddLang( "Lens_Label" );

                                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( lensType.GetDescription() ).Line()
                                            .AddRaw( lensType.StrategyTip.Text, ColorTheme.PurpleDim ).Line();

                                        if ( !lensType.GetIsLensAvailable() )
                                            novel.Main.AddLang( "LensNotCurrentlyAvailable", ColorTheme.Gray ).Line();
                                    }
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( lensType.GetIsLensAvailable() )
                                        SimMetagame.CurrentEndOfTimeLens = lensType;
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            #region DrawOuterRadialOption
            private void DrawOuterRadialOption( int RadialOptionIndex, OuterRadialOption option )
            {
                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                bOuterRadialIcon icon = bOuterRadialIconPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.SetSpriteIfNeeded( option.Icon.GetSpriteForUI() );
                    if ( icon.image != null )
                    {
                        bool isHovered = icon.Element?.LastHadMouseWithin ?? false;
                        icon.image.color = isHovered ? ColorRefs.RadialButtonHovered.ColorWithoutHDR : ColorRefs.RadialButtonNormal.ColorWithoutHDR;
                    }

                    if ( RadialOptionIndex == 0 && SharedRenderManagerData.CurrentIndicator == Indicator.FirstOuterRadialButton && NoteLog.TemporaryNotes.Count == 0 )
                    {
                        ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                        tooltipBuffer.AddLang( "IndicateFirstOuterRadialButton_Text" );
                        TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( option ), icon.Element, SideClamp.Any, TooltipArrowSide.BottomRight );
                    }

                    //no need to apply any position, the layout group will handle that
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                //no text to show
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( novel.TryStartBasicTooltip( TooltipID.Create( option ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple ) )
                                    {
                                        novel.Icon = option.Icon;

                                        novel.TitleUpperLeft.AddRaw( option.GetDisplayName() );
                                        if ( option.HotkeyToDisplayAsRelated.Length > 0 )
                                            novel.TitleLowerLeft.AddTooltipHotkeySecondLine( option.HotkeyToDisplayAsRelated );

                                        novel.Main.StartColor( ColorTheme.NarrativeColor ).AddRaw( option.GetDescription() ).Line()
                                            .AddRaw( option.StrategyTip.Text, ColorTheme.PurpleDim );

                                        option.Implementation.TryHandleOuterRadialOption( option, novel.Main, OuterRadialOptionLogic.PredictTooltipAdditions );
                                    }

                                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.FirstOuterRadialButton )
                                        FlagRefs.IndicateFirstOuterRadialButton.UnTripIfNeeded();
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( option != null && option.GetIsOptionAvailable() )
                                    {
                                        if ( ExtraData.MouseInput.RightButtonClicked || ExtraData.MouseInput.RightButtonDoubleClicked )
                                            option.Implementation.TryHandleOuterRadialOption( option, ExtraData.Buffer, OuterRadialOptionLogic.HandleRightClick );
                                        else if ( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.LeftButtonDoubleClicked )
                                            option.Implementation.TryHandleOuterRadialOption( option, ExtraData.Buffer, OuterRadialOptionLogic.HandleLeftClick );
                                    }
                                }
                                break;
                        }
                    } );
                }
            }
            #endregion

            private const float TIMED_NOTE_START_Y_NO_DROPDOWN = 214.6f;
            private const float TIMED_NOTE_START_Y_IF_DROPDOWN = 236.3f;
            private const float TIMED_NOTE_X = -1.7f;
            private const float TIMED_NOTE_HEIGHT = 33.5f;
            private const float TIMED_NOTE_WIDTH = 190f;
            private const float TIMED_NOTE_OFFSET_Y_PER = 31.4f;

            #region HandleTimedNotifications
            private void HandleTimedNotifications()
            {
                if ( !FlagRefs.UITour4_RadialMenu.DuringGameplay_IsTripped )
                    return; //this is the one directly before it

                if ( !InputCaching.ShowBottomRightNotices )
                {
                    NoteLog.TemporaryNotes.Clear();
                    return;
                }

                float currentY = TIMED_NOTE_START_Y_NO_DROPDOWN;
                if ( !dLensFocus.Instance.GetShouldBeHidden() )
                    currentY = TIMED_NOTE_START_Y_IF_DROPDOWN;

                int maxNotes = 8;
                for ( int i = 0; i < NoteLog.TemporaryNotes.Count; i++ )
                {
                    TemporaryNote tNote = NoteLog.TemporaryNotes[i];
                    if ( !tNote.HasBeenRevealed )
                    {
                        if ( ArcenTime.AnyTimeSinceStartF - NoteLog.LastTimeTemporaryNoteWasRevealed < InputCaching.BottomRightNotice_DelayBetween )
                            continue; //if too soon to reveal, then skip

                        //ArcenDebugging.LogSingleLine( "Reveal: " + (ArcenTime.AnyTimeSinceStartF - NoteLog.LastTimeTemporaryNoteWasRevealed).ToStringThousandsDecimal_Optional4() +
                        //    " time " + InputCaching.BottomRightNotice_DelayBetween.ToStringThousandsDecimal_Optional4() + " gap", Verbosity.DoNotShow );

                        //if can reveal, then reveal
                        tNote.HasBeenRevealed = true;
                        NoteLog.LastTimeTemporaryNoteWasRevealed = ArcenTime.AnyTimeSinceStartF;
                    }

                    this.HandleTimedNotifications_HandleIndividualItem( tNote, ref currentY );

                    if ( maxNotes-- <= 0 )
                        break;
                }
            }
            #endregion

            #region HandleTimedNotifications_HandleIndividualItem
            private void HandleTimedNotifications_HandleIndividualItem( TemporaryNote tNote, ref float currentY )
            {
                if ( tNote == null || tNote.Note == null )
                    return;

                bTimedNotificationButton icon = bTimedNotificationButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    float iconX = TIMED_NOTE_X;
                    icon.ApplyItemInPositionNoTextSizing( ref iconX, ref currentY, false, false, TIMED_NOTE_WIDTH, TIMED_NOTE_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Element.transform.SetAsFirstSibling();
                    icon.Assign( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                    {
                        IGameNote gameNote = tNote?.Note;
                        if ( gameNote == null )
                            return;

                        switch ( Action )
                        {
                            case UIAction.GetTextToShowFromVolatile:
                                {
                                    bool isBeingHoveredNow = false;
                                    if ( element is ArcenUI_Button but )
                                    {
                                        if ( but.LastHadMouseWithin )
                                        {
                                            isBeingHoveredNow = true;
                                            NoteLog.LastTimeMouseWasOverTemporaryNote = ArcenTime.AnyTimeSinceStartF; //freeze the countdown timer
                                        }
                                    }

                                    if ( isBeingHoveredNow )
                                        ExtraData.Buffer.StartColor( ColorTheme.BasicLightTextBlue );

                                    gameNote.HandleNote( GameNoteAction.WriteText, ExtraData.Buffer, false, null, null, string.Empty, 0, true );

                                    //the countdown bar at the bottom
                                    float fillAmount = tNote.RemainingTime / InputCaching.BottomRightNotice_LingerTime;
                                    if ( fillAmount > 1 )
                                        fillAmount = 1;
                                    if ( fillAmount < 0.01f )
                                        fillAmount = 0.01f;

                                    if ( tNote.PriorityLevel >= NoteLog.PriorityLevelToPauseAtAndAbove )
                                        fillAmount = 0;

                                    element.RelatedImages[0].fillAmount = fillAmount;
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    gameNote.HandleNote( GameNoteAction.WriteTooltip, null, false, element, null, string.Empty, (int)SideClamp.LeftOrRight, true );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                                        return; //not valid until you have finished

                                    if ( ExtraData.MouseInput.LeftButtonClicked )
                                    {
                                        if ( !gameNote.HandleNote( GameNoteAction.LeftClick, null, false, null, null, string.Empty, 0, true ) )
                                        {
                                            NoteLog.LastTimeTemporaryNoteWasRevealed = 0; //reveal the next one right away
                                            NoteLog.TemporaryNotes.Remove( tNote ); //remove this one
                                        }
                                    }
                                    else if ( ExtraData.MouseInput.RightButtonClicked )
                                    {
                                        if ( !gameNote.HandleNote( GameNoteAction.RightClick, null, false, null, null, string.Empty, 0, true ) )
                                        {
                                            NoteLog.LastTimeTemporaryNoteWasRevealed = 0; //reveal the next one right away
                                            NoteLog.TemporaryNotes.Remove( tNote ); //remove this one
                                        }
                                    }
                                }
                                break;
                        }
                    } );

                    currentY += TIMED_NOTE_OFFSET_Y_PER;
                }
            }
            #endregion
        }

        #region bTimedNotificationButton
        public class bTimedNotificationButton : ButtonAbstractBaseWithImage
        {
            public static bTimedNotificationButton Original;
            public bTimedNotificationButton() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public void Assign( GetOrSetUIData UIDataController )
            {
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void OnUpdateSubSub()
            {
                base.OnUpdateSubSub();
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

        #region bRadialIcon
        public class bRadialIcon : ButtonAbstractBaseWithImage
        {
            public static bRadialIcon Original;
            public bRadialIcon() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;

            public RadialIconStyle lastStyle = RadialIconStyle.Normal;

            private Sprite priorLargeSprite = null;
            public void SetLargeSpriteIfNeeded( Sprite newSprite )
            {
                if ( !(this.Element == null) && !(newSprite == priorLargeSprite) )
                {
                    priorLargeSprite = newSprite;
                    this.Element.RelatedImages[2].sprite = newSprite;
                }
            }

            public void SetStyle( RadialIconStyle style )
            {
                if ( this.lastStyle == style || this.Element == null ) 
                    return;

                this.lastStyle = style;

                switch ( style )
                {
                    case RadialIconStyle.Normal:
                        this.Element.RelatedImages[0].enabled = true; //main background
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[0]; //regular status
                        this.Element.RelatedImages[1].gameObject.SetActive( true ); //main actual image
                        this.Element.RelatedImages[2].gameObject.SetActive( false ); //larger actual image
                        break;
                    case RadialIconStyle.Disabled:
                        this.Element.RelatedImages[0].enabled = true; //main background
                        this.Element.RelatedImages[0].sprite = this.Element.RelatedSprites[1]; //disabled status
                        this.Element.RelatedImages[1].gameObject.SetActive( true ); //main actual image
                        this.Element.RelatedImages[2].gameObject.SetActive( false ); //larger actual image
                        break;
                    case RadialIconStyle.Selected:
                        this.Element.RelatedImages[0].enabled = false; //main background
                        this.Element.RelatedImages[1].gameObject.SetActive( false ); //main actual image
                        this.Element.RelatedImages[2].gameObject.SetActive( true ); //larger actual image
                        break;
                }
            }

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

        public enum RadialIconStyle
        {
            Normal,
            Disabled,
            Selected
        }

        #region bOuterRadialIcon
        public class bOuterRadialIcon : ButtonAbstractBaseWithImage
        {
            public static bOuterRadialIcon Original;
            public bOuterRadialIcon() { if ( Original == null ) Original = this; }

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

        #region dLensFocus
        public class dLensFocus : DropdownAbstractBase
        {
            public static dLensFocus Instance;
            public dLensFocus()
            {
                Instance = this;
            }

            public override void HandleSelectionChanged( IArcenDropdownOption Item, DropdownSetType SetType )
            {
                if ( Item == null )
                    return;

                if ( Item.GetItem() is Investigation investigation )
                {
                    if ( investigation.Type != null )
                        SimCommon.CurrentInvestigation = investigation;
                }
                else if ( Item.GetItem() is StreetSenseCollection streetSenseCollection )
                {
                    if ( streetSenseCollection != null )
                        SimCommon.CurrentStreetSenseCollection = streetSenseCollection;
                }
                else if ( Item.GetItem() is ContemplationCollection contemplationCollection )
                {
                    if ( contemplationCollection != null )
                        SimCommon.CurrentContemplationCollection = contemplationCollection;
                }
                else if ( Item.GetItem() is ExplorationSiteCollection explorationSiteCollection )
                {
                    if ( explorationSiteCollection != null )
                        SimCommon.CurrentExplorationSiteCollection = explorationSiteCollection;
                }
                else if ( Item.GetItem() is ResourceScavengingCollection scavengeColl )
                {
                    if ( scavengeColl != null )
                        SimCommon.CurrentScavengingCollection = scavengeColl;
                }
            }

            public override bool GetShouldBeHidden()
            {
                if ( SimCommon.VisibleAndroidInvestigations.Count > 0 && (SimCommon.CurrentCityLens?.ShowInvestigations?.Display ?? false) )
                    return false;

                if ( (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false) )
                    return false;
                else if ( (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false) )
                    return false;
                else if ( (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false) )
                    return false;
                else if ( (SimCommon.CurrentCityLens?.ShowSpecialResources?.Display ?? false) )
                    return false;

                return true;
            }

            public override void OnUpdate()
            {
                ArcenUI_Dropdown elementAsType = (ArcenUI_Dropdown)this.Element;

                if ( SimCommon.VisibleAndroidInvestigations.Count > 0 && (SimCommon.CurrentCityLens?.ShowInvestigations?.Display ?? false) )
                {
                    #region Investigations
                    ListView<Investigation> validInvestigations = SimCommon.VisibleAndroidInvestigations;

                    if ( SimCommon.CurrentInvestigation == null )
                        SimCommon.CurrentInvestigation = validInvestigations.FirstOrDefault;

                    Investigation typeDataToSelect = SimCommon.CurrentInvestigation;

                    #region If The Selected Type Is Not Valid Right Now, Then Skip It
                    if ( typeDataToSelect != null )
                    {
                        if ( !validInvestigations.Contains( typeDataToSelect ) )
                        {
                            typeDataToSelect = null;
                            SimCommon.CurrentInvestigation = null;
                        }
                    }
                    #endregion

                    #region Select Default If Blank
                    if ( typeDataToSelect == null && validInvestigations.Count > 0 )
                        typeDataToSelect = validInvestigations.FirstOrDefault;
                    #endregion

                    if ( SimCommon.CurrentInvestigation == null && typeDataToSelect != null )
                        SimCommon.CurrentInvestigation = typeDataToSelect;

                    bool foundMismatch = false;
                    if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (elementAsType.CurrentlySelectedOption.GetItem() as Investigation ) != typeDataToSelect) )
                    {
                        foundMismatch = true;
                        //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                    }
                    else if ( validInvestigations.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                        foundMismatch = true;
                    else
                    {
                        for ( int i = 0; i < validInvestigations.Count; i++ )
                        {
                            Investigation row = validInvestigations[i];

                            IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                            if ( option == null )
                            {
                                foundMismatch = true;
                                break;
                            }
                            Investigation optionItemAsType = option.GetItem() as Investigation;
                            if ( row == optionItemAsType )
                                continue;
                            foundMismatch = true;
                            break;
                        }
                    }

                    if ( foundMismatch )
                    {
                        elementAsType.ClearItems();

                        for ( int i = 0; i < validInvestigations.Count; i++ )
                        {
                            Investigation row = validInvestigations[i];
                            elementAsType.AddItem( row, row == typeDataToSelect );
                        }
                    }
                    #endregion Investigations
                }
                else
                {
                    if ( (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false) )
                    {
                        #region ShowStreetSenseCollections
                        List<StreetSenseCollection> validCollections = StreetSenseCollection.AvailableCollections.GetDisplayList();

                        if ( SimCommon.CurrentStreetSenseCollection == null )
                            SimCommon.CurrentStreetSenseCollection = validCollections.FirstOrDefault;

                        StreetSenseCollection typeDataToSelect = SimCommon.CurrentStreetSenseCollection;

                        #region If The Selected Type Is Not Valid Right Now, Then Skip It
                        if ( typeDataToSelect != null )
                        {
                            if ( !validCollections.Contains( typeDataToSelect ) )
                            {
                                typeDataToSelect = null;
                                SimCommon.CurrentStreetSenseCollection = null;
                            }
                        }
                        #endregion

                        #region Select Default If Blank
                        if ( typeDataToSelect == null && validCollections.Count > 0 )
                            typeDataToSelect = validCollections.FirstOrDefault;
                        #endregion

                        if ( SimCommon.CurrentStreetSenseCollection == null && typeDataToSelect != null )
                            SimCommon.CurrentStreetSenseCollection = typeDataToSelect;

                        bool foundMismatch = false;
                        if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || ( elementAsType.CurrentlySelectedOption.GetItem() as StreetSenseCollection) != typeDataToSelect) )
                        {
                            foundMismatch = true;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                        }
                        else if ( validCollections.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                            foundMismatch = true;
                        else
                        {
                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                StreetSenseCollection row = validCollections[i];

                                IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                if ( option == null )
                                {
                                    foundMismatch = true;
                                    break;
                                }
                                StreetSenseCollection optionItemAsType = option.GetItem() as StreetSenseCollection;
                                if ( row == optionItemAsType )
                                    continue;
                                foundMismatch = true;
                                break;
                            }
                        }

                        if ( foundMismatch )
                        {
                            elementAsType.ClearItems();

                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                StreetSenseCollection row = validCollections[i];
                                elementAsType.AddItem( row, row == typeDataToSelect );
                            }
                        }
                        #endregion StreetSenseCollections
                    }
                    else if ( (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false) )
                    {
                        #region ShowContemplationCollections
                        List<ContemplationCollection> validCollections = ContemplationCollection.AvailableCollections.GetDisplayList();

                        if ( SimCommon.CurrentContemplationCollection == null )
                            SimCommon.CurrentContemplationCollection = validCollections.FirstOrDefault;

                        ContemplationCollection typeDataToSelect = SimCommon.CurrentContemplationCollection;

                        #region If The Selected Type Is Not Valid Right Now, Then Skip It
                        if ( typeDataToSelect != null )
                        {
                            if ( !validCollections.Contains( typeDataToSelect ) )
                            {
                                typeDataToSelect = null;
                                SimCommon.CurrentContemplationCollection = null;
                            }
                        }
                        #endregion

                        #region Select Default If Blank
                        if ( typeDataToSelect == null && validCollections.Count > 0 )
                            typeDataToSelect = validCollections.FirstOrDefault;
                        #endregion

                        if ( SimCommon.CurrentContemplationCollection == null && typeDataToSelect != null )
                            SimCommon.CurrentContemplationCollection = typeDataToSelect;

                        bool foundMismatch = false;
                        if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (elementAsType.CurrentlySelectedOption.GetItem() as ContemplationCollection) != typeDataToSelect) )
                        {
                            foundMismatch = true;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                        }
                        else if ( validCollections.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                            foundMismatch = true;
                        else
                        {
                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ContemplationCollection row = validCollections[i];

                                IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                if ( option == null )
                                {
                                    foundMismatch = true;
                                    break;
                                }
                                ContemplationCollection optionItemAsType = option.GetItem() as ContemplationCollection;
                                if ( row == optionItemAsType )
                                    continue;
                                foundMismatch = true;
                                break;
                            }
                        }

                        if ( foundMismatch )
                        {
                            elementAsType.ClearItems();

                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ContemplationCollection row = validCollections[i];
                                elementAsType.AddItem( row, row == typeDataToSelect );
                            }
                        }
                        #endregion ContemplationCollections
                    }
                    else if ( (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false) )
                    {
                        #region ShowExplorationSiteCollections
                        List<ExplorationSiteCollection> validCollections = ExplorationSiteCollection.AvailableCollections.GetDisplayList();

                        if ( SimCommon.CurrentExplorationSiteCollection == null )
                            SimCommon.CurrentExplorationSiteCollection = validCollections.FirstOrDefault;

                        ExplorationSiteCollection typeDataToSelect = SimCommon.CurrentExplorationSiteCollection;

                        #region If The Selected Type Is Not Valid Right Now, Then Skip It
                        if ( typeDataToSelect != null )
                        {
                            if ( !validCollections.Contains( typeDataToSelect ) )
                            {
                                typeDataToSelect = null;
                                SimCommon.CurrentExplorationSiteCollection = null;
                            }
                        }
                        #endregion

                        #region Select Default If Blank
                        if ( typeDataToSelect == null && validCollections.Count > 0 )
                            typeDataToSelect = validCollections.FirstOrDefault;
                        #endregion

                        if ( SimCommon.CurrentExplorationSiteCollection == null && typeDataToSelect != null )
                            SimCommon.CurrentExplorationSiteCollection = typeDataToSelect;

                        bool foundMismatch = false;
                        if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (elementAsType.CurrentlySelectedOption.GetItem() as ExplorationSiteCollection) != typeDataToSelect) )
                        {
                            foundMismatch = true;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                        }
                        else if ( validCollections.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                            foundMismatch = true;
                        else
                        {
                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ExplorationSiteCollection row = validCollections[i];

                                IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                if ( option == null )
                                {
                                    foundMismatch = true;
                                    break;
                                }
                                ExplorationSiteCollection optionItemAsType = option.GetItem() as ExplorationSiteCollection;
                                if ( row == optionItemAsType )
                                    continue;
                                foundMismatch = true;
                                break;
                            }
                        }

                        if ( foundMismatch )
                        {
                            elementAsType.ClearItems();

                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ExplorationSiteCollection row = validCollections[i];
                                elementAsType.AddItem( row, row == typeDataToSelect );
                            }
                        }
                        #endregion ExplorationSiteCollections
                    }
                    else if ( (SimCommon.CurrentCityLens?.ShowSpecialResources?.Display ?? false) )
                    {
                        #region ResourceScavengingCollections
                        List<ResourceScavengingCollection> validCollections = ResourceScavengingCollection.AvailableCollections.GetDisplayList();

                        if ( SimCommon.CurrentScavengingCollection == null )
                            SimCommon.CurrentScavengingCollection = validCollections.FirstOrDefault;

                        ResourceScavengingCollection typeDataToSelect = SimCommon.CurrentScavengingCollection;

                        #region If The Selected Type Is Not Valid Right Now, Then Skip It
                        if ( typeDataToSelect != null )
                        {
                            if ( !validCollections.Contains( typeDataToSelect ) )
                            {
                                typeDataToSelect = null;
                                SimCommon.CurrentScavengingCollection = null;
                            }
                        }
                        #endregion

                        #region Select Default If Blank
                        if ( typeDataToSelect == null && validCollections.Count > 0 )
                            typeDataToSelect = validCollections.FirstOrDefault;
                        #endregion

                        if ( SimCommon.CurrentScavengingCollection == null && typeDataToSelect != null )
                            SimCommon.CurrentScavengingCollection = typeDataToSelect;

                        bool foundMismatch = false;
                        if ( typeDataToSelect != null && (elementAsType.CurrentlySelectedOption == null || (elementAsType.CurrentlySelectedOption.GetItem() as ResourceScavengingCollection) != typeDataToSelect) )
                        {
                            foundMismatch = true;
                            //ArcenDebugging.ArcenDebugLogSingleLine( "Fixing selected item in names to be " + typeDataToSelect.ID, Verbosity.DoNotShow );
                        }
                        else if ( validCollections.Count != elementAsType.GetItems_DoNotAlterDirectly().Count )
                            foundMismatch = true;
                        else
                        {
                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ResourceScavengingCollection row = validCollections[i];

                                IArcenDropdownOption option = elementAsType.GetItems_DoNotAlterDirectly()[i];
                                if ( option == null )
                                {
                                    foundMismatch = true;
                                    break;
                                }
                                ResourceScavengingCollection optionItemAsType = option.GetItem() as ResourceScavengingCollection;
                                if ( row == optionItemAsType )
                                    continue;
                                foundMismatch = true;
                                break;
                            }
                        }

                        if ( foundMismatch )
                        {
                            elementAsType.ClearItems();

                            for ( int i = 0; i < validCollections.Count; i++ )
                            {
                                ResourceScavengingCollection row = validCollections[i];
                                elementAsType.AddItem( row, row == typeDataToSelect );
                            }
                        }
                        #endregion ResourceScavengingCollections
                    }
                }
            }

            public override void HandleMouseover()
            {
                if ( SimCommon.VisibleAndroidInvestigations.Count > 0 && (SimCommon.CurrentCityLens?.ShowInvestigations?.Display ?? false) )
                {
                    if ( SimCommon.CurrentInvestigation != null )
                    {
                        SimCommon.CurrentInvestigation.RenderActiveInvestigationTooltip( broadElementToUse, SideClamp.AboveOnly, TooltipShadowStyle.None, Engine_HotM.SelectedActor as ISimMachineActor, TooltipExtraRules.None );
                    }
                }
                else
                {
                    if ( (SimCommon.CurrentCityLens?.ShowAllStreetSense?.Display ?? false) )
                    {
                    }
                    else if ( (SimCommon.CurrentCityLens?.ShowContemplations?.Display ?? false) )
                    {
                        if ( SimCommon.CurrentContemplationCollection != null && SimCommon.CurrentContemplationCollection.GetDescription().Length > 0 )
                        {
                            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( SimCommon.CurrentContemplationCollection ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.None ) )
                            {
                                novel.ShadowStyle = TooltipShadowStyle.None;
                                novel.TitleUpperLeft.AddRaw( SimCommon.CurrentContemplationCollection.GetDisplayName() );
                                novel.Main.AddRaw( SimCommon.CurrentContemplationCollection.GetDescription(), ColorTheme.NarrativeColor ).Line();
                            }
                        }
                    }
                    else if ( (SimCommon.CurrentCityLens?.ShowExplorationSites?.Display ?? false) )
                    {
                        if ( SimCommon.CurrentExplorationSiteCollection != null && SimCommon.CurrentExplorationSiteCollection.GetDescription().Length > 0 )
                        {
                            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                            if ( novel.TryStartSmallerTooltip( TooltipID.Create( SimCommon.CurrentExplorationSiteCollection ), broadElementToUse, SideClamp.AboveOnly, TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.None ) )
                            {
                                novel.ShadowStyle = TooltipShadowStyle.None;
                                novel.TitleUpperLeft.AddRaw( SimCommon.CurrentExplorationSiteCollection.GetDisplayName() );
                                novel.Main.AddRaw( SimCommon.CurrentExplorationSiteCollection.GetDescription(), ColorTheme.NarrativeColor ).Line();
                            }
                        }
                    }
                }
            }

            public override void HandleItemMouseover( IArcenUIElementForSizing ItemElement, IArcenDropdownOption Item )
            {
                if ( Item.GetItem() is Investigation investigation )
                {
                    investigation.RenderActiveInvestigationTooltip( ItemElement, SideClamp.LeftOrRight, TooltipShadowStyle.None, Engine_HotM.SelectedActor as ISimMachineActor, TooltipExtraRules.None );
                }
                //else if ( Item.GetItem() is StreetSenseCollection streetSenseCollection )
                //{
                //    if ( streetSenseCollection != null )
                //        investigation.RenderActiveInvestigationTooltip( ItemElement, SideClamp.LeftOrRight, TooltipShadowStyle.None, Engine_HotM.SelectedActor as ISimMachineActor, TooltipExtraRules.None );
                //    SimCommon.CurrentStreetSenseCollection = streetSenseCollection;
                //}
                else if ( Item.GetItem() is ContemplationCollection contemplationCollection )
                {
                    if ( contemplationCollection != null && contemplationCollection.GetDescription().Length > 0 )
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( contemplationCollection ), ItemElement, SideClamp.LeftOrRight, TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.None ) )
                        {
                            novel.ShadowStyle = TooltipShadowStyle.None;
                            novel.TitleUpperLeft.AddRaw( contemplationCollection.GetDisplayName() );
                            novel.Main.AddRaw( contemplationCollection.GetDescription(), ColorTheme.NarrativeColor ).Line();
                        }
                    }
                }
                else if ( Item.GetItem() is ExplorationSiteCollection explorationSiteCollection )
                {
                    if ( explorationSiteCollection != null && explorationSiteCollection.GetDescription().Length > 0 )
                    {
                        NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( explorationSiteCollection ), ItemElement, SideClamp.LeftOrRight, TooltipNovelWidth.Simple, TooltipExtraText.None, TooltipExtraRules.None ) )
                        {
                            novel.ShadowStyle = TooltipShadowStyle.None;
                            novel.TitleUpperLeft.AddRaw( explorationSiteCollection.GetDisplayName() );
                            novel.Main.AddRaw( explorationSiteCollection.GetDescription(), ColorTheme.NarrativeColor ).Line();
                        }
                    }
                }
            }
        }
        #endregion
    }
}

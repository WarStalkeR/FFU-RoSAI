using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.ExternalVis
{
    public class Window_TaskStack : WindowControllerAbstractBase
    {
        public static Window_TaskStack Instance;
		
		public Window_TaskStack()
        {
            Instance = this;
            this.IsPassiveWindowThatDoesNotAffectDropdowns = true; //without this set, then whenever this window appears it will cause all dropdowns to close!
        }

        public override void Close( WindowCloseReason Reason )
        {

        }

        public static int LastTaskEntryCount = 0;

        public override bool GetShouldDrawThisFrame_Subclass()
        {
            LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
            if ( lowerMode == null )
            {
                if ( !Window_MainGameHeaderBarLeft.Instance.GetShouldDrawThisFrame() )
                    return false;
            }
            if ( VisCurrent.IsUIHiddenExceptForSidebar )
                return false;
            if ( !InputCaching.ShowTooltips )
                return false;
            if ( !FlagRefs.UITour2_RightHeader.DuringGameplay_IsTripped )
                return false;
            if ( VisCurrent.IsShowingActualEvent ||
                RenderHelper_EventCamera.IsRenderingCharactersOnTheRight() || !InputCaching.ShowTopRightChecklist )
                return false;
            //switch ( SharedRenderManagerData.CurrentIndicator )
            //{
            //    case Indicator.MapButton:
            //    case Indicator.ForcesSidebar: 
            //        return false;
            //}
            if ( SimCommon.ShouldBeShowingPostGoalScreen )
                return false;

            return true;
        }

        #region GetYHeightForOtherWindowOffsets
        public static float GetYHeightForOtherWindowOffsets()
        {
            if ( !InputCaching.ShowTopRightBar )
                return 0;

            float height = Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceSize().y;
            height *= 1.035f; //add buffer 
            return height;
        }
        #endregion

        #region GetMinXForTooltips
        public static float GetMinXForTooltips()
        {
            return Instance.Window.GetCanvasRectTransformForOneTimeChange_YouBetterKnowWhatYouAreDoing().GetWorldSpaceBottomLeftCorner().x;
        }
        #endregion

        #region GetCurrentWidth_Scaled
        /// <summary>
        /// Gets the amount of horizontal space the this will be taking up, on whichever side it happens to be right now,
        /// scaled appropriately based on its scale.
        /// </summary>
        public float GetCurrentWidth_Scaled()
        {
            if ( !GetShouldDrawThisFrame_Subclass() )
                return 0; //hidden entirely!

            return 170f * (this.Window.Controller as WindowControllerAbstractBase).myScale;
        }
        #endregion

        private static ButtonAbstractBase.ButtonPool<bTaskBox> bTaskBoxPool;
        private static ButtonAbstractBase.ButtonPool<bActionRequiredButton> bActionRequiredButtonPool;

        public const float TASK_BOX_X = 0f;
        public const float TASK_BOX_WIDTH = 170f;
        public const float TASK_BOX_HEIGHT = 17f;
        public const float ACTION_REQUIRED_BOX_HEIGHT = 30.13f;
        public const float TASK_BOX_SPACING = 1f;

        private static float currentY = 0f;
        private static int addedSoFar = 0;

        public static int AddedSoFar { get { return addedSoFar; } }

        public class customParent : CustomUIAbstractBase
        {
            public override void HandleMouseover()
            {
                ArcenUI.TimeOfLastMouseIsWithinSomeMousehandlingElement = ArcenTime.AnyTimeSinceStartF;
            }

            private bool hasGlobalInitialized = false;
            public override void OnUpdate()
            {
                this.WindowController.myScale = GameSettings.Current.GetFloat( "Scale_TaskStack" ) * 1.1f;
                this.WindowController.ExtraOffsetX = -(Window_Sidebar.Instance.GetSidebarCurrentWidth_Scaled());
                this.WindowController.ExtraOffsetY = (Window_MainGameHeaderBarRight.Instance.GetCurrentHeight_Scaled());
                
                #region Global Init
                if ( !hasGlobalInitialized )
                {
                    if ( bTaskBox.Original != null )
                    {
                        this.WindowController.Window.MinDeltaTimeBeforeUpdates = 0;
                        this.WindowController.Window.MaxDeltaTimeBeforeUpdates = 0;
                        hasGlobalInitialized = true;
                        bTaskBoxPool = new ButtonAbstractBase.ButtonPool<bTaskBox>( bTaskBox.Original, 10, "bTaskBox" );
                        bActionRequiredButtonPool = new ButtonAbstractBase.ButtonPool<bActionRequiredButton>( bActionRequiredButton.Original, 10, "bActionRequiredButton" );
                    }
                }
                #endregion

                this.UpdateTaskBoxes();

                LastTaskEntryCount = bTaskBoxPool.GetInUseCount();
            }

            #region UpdateTaskBoxes
            public void UpdateTaskBoxes()
            {
                if ( !hasGlobalInitialized )
                    return;

                bTaskBoxPool.Clear( 15 );
                bActionRequiredButtonPool.Clear( 5 );

                currentY = 0f;
                addedSoFar = 0;

                LowerModeData lowerMode = Engine_HotM.CurrentLowerMode;
                if ( lowerMode != null && lowerMode.HideTaskStack )
                {
                    bool isFirstTodoItem = false;
                    foreach ( CityTask task in SimCommon.ActiveCityTasks.GetDisplayList() )
                    {
                        if ( task.ShowsInLowerMode != lowerMode )
                            continue;
                        if ( !task.Implementation.GetShouldBeVisible( task ) )
                            continue;

                        if ( task.Line2.Text.IsEmpty() )
                            this.AddCityTaskTaskBox( task );
                        else
                            this.HandleActionRequiredPopup_HandleIndividualItem( ref isFirstTodoItem, task, ref currentY );
                    }
                }
                else
                {
                    if ( SimCommon.CurrentTimeline?.IsTimelineAFailure ?? false )
                        AddTimelineFailureBox();
                    else
                    {
                        bool hasHandledChecklist = false;
                        if ( !hasHandledChecklist )
                            hasHandledChecklist = TryRenderChapterOneChecklistIfNeeded();

                        if ( TryRenderProjectChecklistIfAnyPresent( hasHandledChecklist ) ) //always do this one
                            hasHandledChecklist = true;

                        AddConsciousnessCollapseWarningBoxIfNeeded();
                        AddMentalStrainWarning_AndroidsBoxIfNeeded();
                        AddMentalStrainWarning_MechsBoxIfNeeded();
                        AddMentalStrainWarning_VehiclesBoxIfNeeded();
                        AddMentalStrainWarning_BulkUnitsBoxIfNeeded();
                        AddMentalStrainWarning_CapturedUnitsBoxIfNeeded();

                        foreach ( CityTask task in SimCommon.ActiveCityTasks.GetDisplayList() )
                        {
                            if ( lowerMode != null && !lowerMode.HideTaskStack )
                            {
                                if ( task.ShowsInLowerMode != null )
                                {
                                    if ( task.ShowsInLowerMode != lowerMode )
                                        continue;
                                }
                            }
                            else
                            {
                                if ( lowerMode != null || task.ShowsInLowerMode != null )
                                {
                                    if ( task.ShowsInLowerMode != lowerMode )
                                        continue;
                                }
                            }
                            if ( !task.Implementation.GetShouldBeVisible( task ) )
                                continue;

                            if ( task.Line2.Text.IsEmpty() )
                                this.AddCityTaskTaskBox( task );
                        }
                    }

                    this.HandleActionRequiredPopups( lowerMode );
                }

                #region Expand or Shrink Height Of This Window
                float heightForWindow = MathA.Abs( currentY ) + EXTRA_SPACING;
                if ( lastWindowHeight != heightForWindow )
                {
                    lastWindowHeight = heightForWindow;
                    this.Element.RelevantRect.anchorMin = new Vector2( 1f, 1f );
                    this.Element.RelevantRect.anchorMax = new Vector2( 1f, 1f );
                    this.Element.RelevantRect.pivot = new Vector2( 1f, 1f );
                    this.Element.RelevantRect.UI_SetHeight( heightForWindow );
                }
                #endregion

                if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour3_TaskStack && !this.GetShouldBeHidden() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLangWithFirstLineBold( "UITour_TaskStack_Text1" )
                        .Line().StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_TaskStack_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 3, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour3_TaskStack", "AlwaysSame" ), this.Element, SideClamp.LeftOrRight, TooltipArrowSide.Right );
                }
            }
            #endregion

            private float lastWindowHeight = -1f;

            public const float EXTRA_SPACING = 0f;

            #region TryRenderChapterOneChecklistIfNeeded
            private bool TryRenderChapterOneChecklistIfNeeded()
            {
                if ( SimMetagame.CurrentChapterNumber != 1 )
                    return false;
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.TheEndOfTime:
                        return false;
                }
                if ( Engine_HotM.CurrentLowerMode != null )
                    return false;

                bool result = CommonRefs.ChapterOneChecklist?.Implementation?.PerFrame_HandleTutorialTaskStackLogic( CommonRefs.ChapterOneChecklist ) ?? false;
                if ( !result )
                    return false;
                return result;
            }
            #endregion

            #region TryRenderProjectChecklistIfAnyPresent
            private bool TryRenderProjectChecklistIfAnyPresent( bool HasDoneTutorialChecklist )
            {
                switch ( Engine_HotM.GameMode )
                {
                    case MainGameMode.TheEndOfTime:
                        return false;
                }

                if ( SimCommon.ActiveProjects.Count == 0 &&
                    SimCommon.ActiveCountdowns.Count == 0 &&
                    SimCommon.ActiveActivityAlerts.Count == 0 &&
                    SimCommon.ActiveAlerts.Count == 0 )
                    return false;

                foreach ( MachineProject project in SimCommon.ActiveProjects.GetDisplayList() )
                    AddProjectTaskBox( project );

                foreach ( OtherCountdownType countdown in SimCommon.ActiveCountdowns.GetDisplayList() )
                    AddOtherCountdownTaskBox( countdown );

                foreach ( OtherNPCActivityChecklistAlert alert in SimCommon.ActiveActivityAlerts.GetDisplayList() )
                    AddOtherNPCActivityChecklistAlertTaskBox( alert );

                foreach ( OtherChecklistAlert alert in SimCommon.ActiveAlerts.GetDisplayList() )
                    AddOtherChecklistAlertTaskBox( alert );

                return true;
            }
            #endregion

            #region AddProjectTaskBox
            private void AddProjectTaskBox( MachineProject project )
            {
                if ( project == null )
                    return;

                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( project == null )
                        return;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Normal : TaskBoxStyle.Normal );

                                bool canBeCompletedNow = false;

                                if ( project.IsMinorProject && project.DuringGame_IntendedOutcome == null && project.AvailableOutcomes.Count > 0 )
                                    project.DuringGame_IntendedOutcome = project.AvailableOutcomes[0];

                                if ( project.DuringGame_IntendedOutcome == null )
                                {
                                    ExtraData.Buffer.AddSpriteStyled_NoIndent( IconRefs.ChooseProjectTarget.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.GetRedOrange2( isHovered ) );
                                    ExtraData.Buffer.Position20();
                                }
                                else
                                {
                                    if ( !project.DuringGame_IntendedOutcome.SkipProgressDisplay )
                                    {
                                        ExtraData.Buffer.StartSize80();
                                        project.Implementation.HandleLogicForProjectOutcome( ProjectLogic.WriteProgressTextBrief,
                                            ExtraData.Buffer, project, project.DuringGame_IntendedOutcome, null, out canBeCompletedNow );
                                        ExtraData.Buffer.EndSize();

                                        ExtraData.Buffer.Position20();
                                    }
                                }

                                string mainColor = isHovered ? ColorTheme.TaskStack_Normal_Hover : string.Empty;
                                ExtraData.Buffer.AddSpriteStyled_NoIndent( project.Icon, AdjustedSpriteStyle.InlineLarger1_4, mainColor );
                                ExtraData.Buffer.AddRaw( project.GetDisplayName(), mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                project.RenderProjectTooltip( Element, SideClamp.LeftOrRight, TooltipShadowStyle.None, true, false, false );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                SimCommon.RewardProvider = ProjectOutcomeHandler.Start( project );
                                Window_RewardWindow.Instance.Open();
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddOtherCountdownTaskBox
            private void AddOtherCountdownTaskBox( OtherCountdownType countdown )
            {
                if ( countdown == null )
                    return;

                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( countdown == null )
                        return;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( countdown.ColorStyle, isHovered );

                                countdown.WriteTurnsPart( ExtraData.Buffer );
                                countdown.WriteTextPart( ExtraData.Buffer, isHovered, true );

                                if ( SharedRenderManagerData.CurrentIndicator == Indicator.PointAtCountdown && SimCommon.PointAtThisCountdown == countdown )
                                {
                                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                                    tooltipBuffer.AddLang( "CountdownProgressWillBeHere" );
                                    tooltipBuffer.StartSize80().AddLang( "PopupGeneralMessageClick_Close", ColorTheme.TooltipFootnote_DimSteelCyan );

                                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( countdown ), element, SideClamp.LeftOrRight, TooltipArrowSide.Right );
                                }
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                countdown.RenderCountdownTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraText.None );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                //nothing to do for now
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddOtherNPCActivityChecklistAlertTaskBox
            private void AddOtherNPCActivityChecklistAlertTaskBox( OtherNPCActivityChecklistAlert alert )
            {
                if ( alert == null )
                    return;
                if ( alert.IsDangerStyleMusic )
                    SimCommon.LastTimeDangerMusicRequested = ArcenTime.AnyTimeSinceStartF;

                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( alert == null )
                        return;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( alert.ColorStyle, isHovered );

                                alert.WriteIncomingPart( ExtraData.Buffer );
                                alert.WriteTextPart( ExtraData.Buffer, isHovered );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                alert.RenderOtherNPCActivityChecklistAlertTooltip( element, SideClamp.LeftOrRight, TooltipShadowStyle.None, TooltipExtraRules.MustBeToLeftOfTaskStack );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                alert.TrySelectNextNPC();
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddOtherChecklistAlertTaskBox
            private void AddOtherChecklistAlertTaskBox( OtherChecklistAlert alert )
            {
                if ( alert == null )
                    return;

                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( alert == null )
                        return;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( alert.ColorStyle, isHovered );

                                alert.Implementation.HandleOtherAlertLogic( alert, ExtraData.Buffer, OtherChecklistAlertLogic.WriteBriefText, isHovered, 
                                    null, SideClamp.Any, TooltipExtraRules.None);
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                alert.Implementation.HandleOtherAlertLogic( alert, null, OtherChecklistAlertLogic.WriteTooltip, false, 
                                    element, SideClamp.LeftOrRight, TooltipExtraRules.MustBeToLeftOfTaskStack );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                alert.Implementation.HandleOtherAlertLogic( alert, null, ExtraData.MouseInput.LeftButtonClicked ? 
                                    OtherChecklistAlertLogic.OnClicked_Left : OtherChecklistAlertLogic.OnClicked_Right, false,
                                    null, SideClamp.Any, TooltipExtraRules.None );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddCityTaskTaskBox
            private void AddCityTaskTaskBox( CityTask task )
            {
                if ( task == null )
                    return;

                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    if ( task == null )
                        return;

                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( AlertColor.Normal, isHovered );

                                ExtraData.Buffer.AddRaw( task.Implementation.GetText( task, CityTaskTextType.DisplayName ) );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                task.RenderToastTooltipText( element, SideClamp.LeftOrRight, TooltipExtraRules.MustBeToLeftOfTaskStack );
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                //nothing to do for now
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddTimelineFailureBox
            private void AddTimelineFailureBox()
            {
                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddLang( "TimelineFailed", mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "TimelineFailed", "TimelineFailed" ), element, SideClamp.LeftOrRight, 
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddLang( "TimelineFailed", ColorTheme.RedOrange2 );
                                    novel.Main.AddLang( "TimelineFailed_Tooltip" );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                //nothing to do
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddConsciousnessCollapseWarningBoxIfNeeded
            private void AddConsciousnessCollapseWarningBoxIfNeeded()
            {
                if ( SimCommon.CurrentTimeline?.Origin?.FailsIfAllAndroidsLost ?? false )
                {
                    if ( SimCommon.TotalOnline_Androids == 1 && !VisCurrent.IsShowingActualEvent )
                    { }
                    else
                        return;
                }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddLang( "ConsciousnessCollapseWarning", mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "ConsciousnessCollapseWarning", "ConsciousnessCollapseWarning" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddLang( "ConsciousnessCollapseWarning" );
                                    novel.Main.AddLang( "ConsciousnessCollapseWarning_Tooltip" );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                //nothing to do
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddMentalStrainWarning_AndroidsBoxIfNeeded
            private void AddMentalStrainWarning_AndroidsBoxIfNeeded()
            {
                if ( SimCommon.TotalCapacityUsed_Androids > MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt )
                { }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int excess = SimCommon.TotalCapacityUsed_Androids - MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddFormat1( "MentalStrainWarning_Androids", excess, mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Androids" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Androids", excess );
                                    novel.Main.AddFormat2( "MentalStrainWarning_Androids_Tooltip1", excess, MathRefs.MaxAndroidCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                                    novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Androids );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddMentalStrainWarning_MechsBoxIfNeeded
            private void AddMentalStrainWarning_MechsBoxIfNeeded()
            {
                if ( SimCommon.TotalCapacityUsed_Mechs > MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt )
                { }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int excess = SimCommon.TotalCapacityUsed_Mechs - MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddFormat1( "MentalStrainWarning_Mechs", excess, mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Mechs" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Mechs", excess );
                                    novel.Main.AddFormat2( "MentalStrainWarning_Mechs_Tooltip1", excess, MathRefs.MaxMechCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                                    novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Mechs );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddMentalStrainWarning_VehiclesBoxIfNeeded
            private void AddMentalStrainWarning_VehiclesBoxIfNeeded()
            {
                if ( SimCommon.TotalCapacityUsed_Vehicles > MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt )
                { }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int excess = SimCommon.TotalCapacityUsed_Vehicles - MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddFormat1( "MentalStrainWarning_Vehicles", excess, mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "Vehicles" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_Vehicles", excess );
                                    novel.Main.AddFormat2( "MentalStrainWarning_Vehicles_Tooltip1", excess, MathRefs.MaxVehicleCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                                    novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.Vehicles );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddMentalStrainWarning_BulkUnitsBoxIfNeeded
            private void AddMentalStrainWarning_BulkUnitsBoxIfNeeded()
            {
                if ( SimCommon.TotalBulkUnitSquadCapacityUsed > MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt )
                { }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int excess = SimCommon.TotalBulkUnitSquadCapacityUsed - MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddFormat1( "MentalStrainWarning_BulkUnits", excess, mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "BulkAndroids" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_BulkUnits", excess );
                                    novel.Main.AddFormat2( "MentalStrainWarning_BulkUnits_Tooltip1", excess, MathRefs.BulkUnitCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                                    novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.BulkAndroids );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region AddMentalStrainWarning_CapturedUnitsBoxIfNeeded
            private void AddMentalStrainWarning_CapturedUnitsBoxIfNeeded()
            {
                if ( SimCommon.TotalCapturedUnitSquadCapacityUsed > MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt )
                { }
                else
                    return;


                AddTaskBox( delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
                {
                    int excess = SimCommon.TotalCapturedUnitSquadCapacityUsed - MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt;
                    switch ( Action )
                    {
                        case UIAction.GetTextToShowFromVolatile:
                            {
                                bool isHovered = element.LastHadMouseWithin;
                                bTaskBox icon = (bTaskBox)element.Controller;
                                icon.SetBoxStyle( isHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );

                                string mainColor = isHovered ? ColorTheme.TaskStack_Crisis_Hover : string.Empty;
                                ExtraData.Buffer.AddFormat1( "MentalStrainWarning_CapturedUnits", excess, mainColor );
                            }
                            break;
                        case UIAction.HandleMouseover:
                            {
                                NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

                                if ( novel.TryStartSmallerTooltip( TooltipID.Create( "MentalStrainWarning", "CapturedUnits" ), element, SideClamp.LeftOrRight,
                                    TooltipNovelWidth.Smaller, TooltipExtraText.None, TooltipExtraRules.MustBeToLeftOfTaskStack ) )
                                {
                                    novel.TitleUpperLeft.AddFormat1( "MentalStrainWarning_CapturedUnits", excess );
                                    novel.Main.AddFormat2( "MentalStrainWarning_CapturedUnits_Tooltip1", excess, MathRefs.CapturedUnitCapacity.DuringGameplay_CurrentInt, ColorTheme.NarrativeColor ).Line();
                                    novel.Main.AddLeftClickFormat( "MentalStrainWarning_Tooltip2", ColorTheme.SoftGold );
                                }
                            }
                            break;
                        case UIAction.OnClick:
                            {
                                Window_ScrapUnitList.HandleOpenCloseToggle( ScrapUnitMode.CapturedUnits );
                            }
                            break;
                    }
                } );
            }
            #endregion

            #region HandleActionRequiredPopups
            private static List<IMinorCompletedToastItem> invalidToasts = List<IMinorCompletedToastItem>.Create_WillNeverBeGCed( 6, "Window_TaskStack-invalidToasts" );
            private void HandleActionRequiredPopups( LowerModeData lowerMode )
            {
                if ( Engine_HotM.GameMode == MainGameMode.TheEndOfTime )
                    return;

                if ( !FlagRefs.UITour4_RadialMenu.DuringGameplay_IsTripped )
                    return; //this is the one directly before it

                if ( lowerMode != null && !lowerMode.HideTaskStack )
                { }
                else
                {
                    if ( !Window_RadialMenu.Instance.GetShouldDrawThisFrame() )
                        return;
                }

                int debugStage = 0;
                try
                {
                    debugStage = 100;
                    bool isFirstTodoItem = true;

                    foreach ( CityTask task in SimCommon.ActiveCityTasks.GetDisplayList() )
                    {
                        debugStage = 1100;
                        if ( lowerMode != null && !lowerMode.HideTaskStack )
                        {
                            if ( task.ShowsInLowerMode != null )
                            {
                                if ( task.ShowsInLowerMode != lowerMode )
                                    continue;
                            }
                        }
                        else
                        {
                            if ( lowerMode != null || task.ShowsInLowerMode != null )
                            {
                                if ( task.ShowsInLowerMode != lowerMode )
                                    continue;
                            }
                        }
                        debugStage = 1200;
                        if ( !task.Implementation.GetShouldBeVisible( task ) )
                            continue;

                        debugStage = 1400;
                        if ( !task.Line2.Text.IsEmpty() )
                            this.HandleActionRequiredPopup_HandleIndividualItem( ref isFirstTodoItem, task, ref currentY );
                    }

                    debugStage = 2100;
                    foreach ( IKeyMessage keyMessage in SimCommon.KeyMessagesWaiting )
                    {
                        debugStage = 2200;
                        this.HandleActionRequiredPopup_HandleIndividualItem( ref isFirstTodoItem, keyMessage, ref currentY );
                    }

                    debugStage = 4100;
                    invalidToasts.Clear();
                    foreach ( IMinorCompletedToastItem minorToast in UnlockCoordinator.MinorCompletedToasts_MainThread )
                    {
                        if ( minorToast == null )
                            continue;
                        debugStage = 4200;
                        if ( !minorToast.GetIsValid() )
                            invalidToasts.Add( minorToast );
                        else
                        {
                            debugStage = 4400;
                            this.HandleActionRequiredPopup_HandleIndividualItem( ref isFirstTodoItem, minorToast, ref currentY );
                        }
                    }

                    debugStage = 5100;
                    if ( invalidToasts.Count > 0 )
                    {
                        debugStage = 5200;
                        foreach ( IMinorCompletedToastItem invalidToast in invalidToasts )
                            invalidToast.ClearThisToast();
                    }

                    debugStage = 8100;
                    foreach ( UnopenedMysteryUnlock mysteryUnlock in UnlockCoordinator.UnopenedMysteryUnlocks_MainThread )
                    {
                        debugStage = 8200;
                        this.HandleActionRequiredPopup_HandleIndividualItem( ref isFirstTodoItem, mysteryUnlock, ref currentY );
                    }

                    debugStage = 9100;
                    if ( isFirstTodoItem )
                    {
                        debugStage = 9200;
                        //meaning we had no todo items active
                        if ( SharedRenderManagerData.CurrentIndicator == Indicator.UITour5_Toast )
                        {
                            debugStage = 9400;
                            MachineHandbookToast toast;
                            toast.HandbookEntry = HandbookRefs.LivingInTheCity;
                            MapEffectCoordinator.AddMinorCompletedToast( toast );
                        }
                    }
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogDebugStageWithStack( "HandleActionRequiredPopups", debugStage, e, Verbosity.ShowAsError );
                }
            }
            #endregion

            #region HandleToastPopup_HandleIndividualItem
            private void HandleActionRequiredPopup_HandleIndividualItem( ref bool isFirstTodoItem, IToastPopupItem toastItem, ref float currentY )
            {
                if ( toastItem == null )
                    return;

                bActionRequiredButton icon = bActionRequiredButtonPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
                if ( icon != null )
                {
                    icon.SetSpriteIfNeeded( toastItem.GetToastIcon().GetSpriteForUI() );
                    if ( icon.image != null )
                    {
                        bool isHovered = icon.Element?.LastHadMouseWithin ?? false;
                        icon.image.color = toastItem.GetToastIconColor( isHovered );
                    }

                    if ( isFirstTodoItem ) //add spacing before the first one only
                        currentY -= ( TASK_BOX_HEIGHT * 0.5f );

                    float iconX = TASK_BOX_X;
                    icon.ApplyItemInPositionNoTextSizing( ref iconX, ref currentY, false, false, TASK_BOX_WIDTH, ACTION_REQUIRED_BOX_HEIGHT, IgnoreSizeOption.IgnoreSize );
                    icon.Element.transform.SetAsFirstSibling();
                    icon.Assign( isFirstTodoItem, delegate ( ArcenUI_Element element, UIAction Action, ref UIActionData ExtraData )
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
                                    toastItem.RenderToastTopLineText( ExtraData.Buffer, isBeingHoveredNow );
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
                                            toastItem.RenderToastSecondLineText( ExtraData.Buffer, isBeingHoveredNow );
                                            break;
                                        default:
                                            ArcenDebugging.LogSingleLine( "HandleToastPopup_HandleIndividualItem GetOtherTextToShowFromVolatile not handled for entry: " + ExtraData.Int, Verbosity.ShowAsError );
                                            break;
                                    }
                                }
                                break;
                            case UIAction.HandleMouseover:
                                {
                                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.Toast )
                                        FlagRefs.IndicateToast.UnTripIfNeeded();
                                    toastItem.RenderToastTooltipText( element, SideClamp.LeftOrRight );
                                }
                                break;
                            case UIAction.OnClick:
                                {
                                    if ( !FlagRefs.HasFinishedUITour.DuringGameplay_IsTripped )
                                        return; //not valid until you have finished

                                    if ( SharedRenderManagerData.CurrentIndicator == Indicator.Toast )
                                        FlagRefs.IndicateToast.UnTripIfNeeded();
                                    toastItem.DoToastClick( ExtraData.MouseInput.LeftButtonClicked || ExtraData.MouseInput.LeftButtonDoubleClicked ? ToastClickType.Left : ToastClickType.Right );
                                }
                                break;
                        }
                    } );

                    if ( isFirstTodoItem )
                        isFirstTodoItem = false;

                    currentY -= (TASK_BOX_SPACING + ACTION_REQUIRED_BOX_HEIGHT);
                }
            }
            #endregion
        }

        #region AddTaskBox
        public static void AddTaskBox( GetOrSetUIData Delegate )
        {
            bTaskBox icon = bTaskBoxPool.GetOrAddEntry_OrNullIfTimeSlicingTooManyAdds();
            if ( icon != null )
            {
                float x = TASK_BOX_X;
                icon.ApplyItemInPositionNoTextSizing( ref x, ref currentY, false, false, TASK_BOX_WIDTH, TASK_BOX_HEIGHT, IgnoreSizeOption.IgnoreSize );
                icon.Assign( Delegate );
                currentY -= (TASK_BOX_SPACING + TASK_BOX_HEIGHT);
                addedSoFar++;
            }
        }
        #endregion

        #region bTaskBox
        public class bTaskBox : ButtonAbstractBaseWithImage
        {
            public static bTaskBox Original;
            public bTaskBox() { if ( Original == null ) Original = this; }

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

            #region SetBoxStyle
            public TaskBoxStyle lastBoxStyle = TaskBoxStyle.Normal;

            public void SetBoxStyle( AlertColor Alert, bool IsHovered )
            {
                switch ( Alert )
                {
                    case AlertColor.Low:
                        this.SetBoxStyle( IsHovered ? TaskBoxStyle.Low : TaskBoxStyle.Low );
                        break;
                    case AlertColor.Normal:
                        this.SetBoxStyle( IsHovered ? TaskBoxStyle.Normal : TaskBoxStyle.Normal );
                        break;
                    case AlertColor.MidWarning:
                        this.SetBoxStyle( IsHovered ? TaskBoxStyle.MidWarning : TaskBoxStyle.MidWarning );
                        break;
                    case AlertColor.HighWarning:
                        this.SetBoxStyle( IsHovered ? TaskBoxStyle.HighWarning : TaskBoxStyle.HighWarning );
                        break;
                    case AlertColor.Crisis:
                        this.SetBoxStyle( IsHovered ? TaskBoxStyle.Crisis : TaskBoxStyle.Crisis );
                        break;
                }
            }

            public void SetBoxStyle( TaskBoxStyle Style )
            {
                if ( lastBoxStyle == Style ) 
                    return;

                lastBoxStyle = Style;

                switch ( Style )
                {
                    case TaskBoxStyle.Low:
                        this.Element.RelatedImages[0].enabled = false; //light off
                        this.image.sprite = this.Element.RelatedSprites[0]; //low
                        break;
                    case TaskBoxStyle.Normal:
                        this.Element.RelatedImages[0].enabled = false; //light off
                        this.image.sprite = this.Element.RelatedSprites[1]; //normal
                        break;
                    case TaskBoxStyle.MidWarning:
                        this.Element.RelatedImages[0].enabled = false; //light off
                        this.image.sprite = this.Element.RelatedSprites[2]; //mid warning
                        break;
                    case TaskBoxStyle.HighWarning:
                        this.Element.RelatedImages[0].enabled = false; //light off
                        this.image.sprite = this.Element.RelatedSprites[3]; //high warning
                        break;
                    case TaskBoxStyle.Crisis:
                        this.Element.RelatedImages[0].enabled = true; //light on
                        this.image.sprite = this.Element.RelatedSprites[4]; //red bright
                        break;
                }
            }
            #endregion
        }
        #endregion

        public enum TaskBoxStyle
        {
            Low,
            Normal,
            MidWarning,
            HighWarning,
            Crisis,
        }

        #region bActionRequiredButton
        public class bActionRequiredButton : ButtonAbstractBaseWithImage
        {
            public static bActionRequiredButton Original;
            public bActionRequiredButton() { if ( Original == null ) Original = this; }

            public GetOrSetUIData UIDataController;
            public bool IsFirstToast;

            public void Assign( bool isFirstToast, GetOrSetUIData UIDataController )
            {
                this.IsFirstToast = isFirstToast;
                this.UIDataController = UIDataController;
                if ( this.button != null )
                    this.button.OptionalGetterAndSetter = UIDataController;
            }

            public override void OnUpdateSubSub()
            {
                base.OnUpdateSubSub();

                if ( IsFirstToast && SharedRenderManagerData.CurrentIndicator == Indicator.Toast && !this.GetShouldBeHidden() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddLang( "IndicateToast_Text" );
                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "IndicateToast_Text", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.Right );
                }
                else if ( IsFirstToast && SharedRenderManagerData.CurrentIndicator == Indicator.UITour5_Toast && !this.GetShouldBeHidden() )
                {
                    ArcenDoubleCharacterBuffer tooltipBuffer = TooltipRefs.ArrowIndicatorDark.GetBufferAndEnsureReset();
                    tooltipBuffer.AddFormat1WithFirstLineBold( "UITour_Toast_Text1",
                        InputActionTypeDataTable.Instance.GetRowByID( "Cancel" ).GetHumanReadableKeyCombo() )
                        .AddRaw( "\n" ).StartColor( ColorTheme.NarrativeColor ).AddLang( "UITour_Toast_Text2" ).EndColor()
                        .AddRaw( "\n" ).StartColor( ColorTheme.TooltipFootnote_DimSteelCyanBrighter ).AddFormat2( "UITour_BottomText", 5, FlagRefs.UITourParts );

                    TooltipRefs.ArrowIndicatorDark.PopulateFromBuffer( TooltipID.Create( "UITour5_Toast", "AlwaysSame" ), this.Element, SideClamp.Any, TooltipArrowSide.Right );
                }
            }

            public override void Clear()
            {
                this.IsFirstToast = false;
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
    }
}

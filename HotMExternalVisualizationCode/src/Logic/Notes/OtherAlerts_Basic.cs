using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class OtherAlerts_Basic : IOtherChecklistAlertImplementation
    {
        public void HandleOtherAlertLogic( OtherChecklistAlert Alert, ArcenCharacterBufferBase BufferOrNull, OtherChecklistAlertLogic Logic, bool IsHovered, 
            IArcenUIElementForSizing MustBeAboveOrBelow, SideClamp Clamp, TooltipExtraRules ExtraRules )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            try
            {
                switch ( Alert.ID )
                {
                    case "DialogAvailable":
                        #region DialogAvailable
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.CurrentDialogs.Count > 0;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    SimCommon.CycleThroughNPCsWhoWantsToSpeakWithYou();
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.CurrentDialogs.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Format2( novel, SimCommon.CurrentDialogs.Count, SimCommon.AllNPCsWithDialog.Count );

                                        if ( SimCommon.CurrentDialogs.Count > 1 )
                                            novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughUnits_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                        else
                                            novel.Main.AddLeftClickFormat( "LeftClickToSelectUnit_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "InactiveTerritoryControlFlags":
                        #region InactiveTerritoryControlFlags
                        {
                            int inactiveFlags = 0;
                            if ( SimCommon.TerritoryControlFlags.Count > 0 )
                            {
                                foreach ( MachineStructure flag in SimCommon.TerritoryControlFlags.GetDisplayList() )
                                {
                                    if ( !flag.IsTerritoryControlActive )
                                        inactiveFlags++;
                                }
                            }

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = inactiveFlags > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => (s.Type?.IsTerritoryControlFlag??false) && !s.IsTerritoryControlActive );
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( inactiveFlags.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Basic( novel );

                                        novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughStructures_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "VulnerableStructures":
                        #region VulnerableStructures
                        {
                            int total = SimCommon.CurrentStructuresWithMissingDeterrenceOrProtection.Count;

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = total > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    SimCommon.CycleThroughMachineStructuresWithMissingDeterrenceOrProtection( false, ( MachineStructure s ) => true );
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( total.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Basic( novel );

                                        novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughStructures_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "InvestigationProgress":
                        #region InvestigationProgress
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.MachineActorsInvestigatingOrInfiltrating.Count > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        foreach ( KeyValuePair<ISimMachineActor, int> kv in SimCommon.MachineActorsInvestigatingOrInfiltrating.GetDisplayList() )
                                        {
                                            Engine_HotM.SetSelectedActor( kv.Key, false, false, false );
                                            break;
                                        }
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    {
                                        int highest = 0;
                                        foreach ( KeyValuePair<ISimMachineActor, int> kv in SimCommon.MachineActorsInvestigatingOrInfiltrating.GetDisplayList() )
                                        {
                                            if ( kv.Value > highest )
                                                highest= kv.Value;
                                        }
                                        BufferOrNull.AddRaw( highest.ToString() ).Position20();
                                        Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        int highest = 0;
                                        foreach ( KeyValuePair<ISimMachineActor, int> kv in SimCommon.MachineActorsInvestigatingOrInfiltrating.GetDisplayList() )
                                        {
                                            if ( kv.Value > highest )
                                                highest = kv.Value;
                                        }
                                        Alert.HandleTooltip_Format2( novel, highest, highest );

                                        novel.Main.AddLeftClickFormat( "LeftClickToSelectUnit_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "InvestigationAvailable":
                        #region InvestigationAvailable
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.VisibleAndroidInvestigations.Count > 0;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        if ( SimCommon.CurrentCityLens != CommonRefs.InvestigationsLens )
                                            SimCommon.CurrentCityLens = CommonRefs.InvestigationsLens;

                                        if ( SimCommon.CurrentInvestigation == null )
                                            SimCommon.CurrentInvestigation = SimCommon.VisibleAndroidInvestigations.FirstOrDefault;

                                        if ( SimCommon.CurrentInvestigation != null )
                                        {
                                            if ( Engine_HotM.SelectedActor is ISimMachineUnit unit && unit.UnitType.IsConsideredAndroid &&
                                                SimCommon.CurrentInvestigation.GetCanActorDoThisInvestigation( unit ) )
                                            { } //no selection change required, as this unit is already able to do its
                                            else
                                            {
                                                foreach ( ISimMachineActor otherActor in SimCommon.AllMachineActors.GetDisplayList() )
                                                {
                                                    if ( otherActor is ISimMachineUnit otherUnit && otherUnit.UnitType.IsConsideredAndroid )
                                                    {
                                                        if ( SimCommon.CurrentInvestigation.GetCanActorDoThisInvestigation( otherUnit ) )
                                                        {
                                                            Engine_HotM.SetSelectedActor( otherActor, false, false, false );
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.VisibleAndroidInvestigations.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Format2( novel, SimCommon.VisibleAndroidInvestigations.Count, SimCommon.VisibleAndroidInvestigations.Count );

                                        novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughUnitsThatCanRespondToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "UnitsWithEmptyEquipmentSlots":
                        #region UnitsWithEmptyEquipmentSlots
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.ActorTypesMissingEquipment.Count > 0;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    if ( !Window_PlayerHardware.Instance.IsOpen )
                                        Window_PlayerHardware.Instance.Open();
                                    Window_PlayerHardware.customParent.currentlyRequestedDisplayType = Window_PlayerHardware.HardwareDisplayType.UnitTypeOverview;
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.ActorTypesMissingEquipment.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        int totalMissingSlots = 0;
                                        foreach ( MobileActorTypeDuringGameData duringGameData in SimCommon.ActorTypesMissingEquipment.GetDisplayList() )
                                            totalMissingSlots += duringGameData.SlotsMissingComplaintWorthyEquipment;

                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, SimCommon.ActorTypesMissingEquipment.Count, totalMissingSlots );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "UnitsWithoutResourcesToAttack":
                        #region UnitsWithoutResourcesToAttack
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.PlayerUnitsShortResourceCostsToAttacks.Count > 0;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    SimCommon.TrySelectNextActorShortResourceCostsToAttack();
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.PlayerUnitsShortResourceCostsToAttacks.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format1( novel, SimCommon.PlayerUnitsShortResourceCostsToAttacks.Count );

                                            novel.Main.StartStyleLineHeightA();
                                            int itemCount = 0;
                                            foreach ( ISimMapActor actor in SimCommon.PlayerUnitsShortResourceCostsToAttacks.GetDisplayList() )
                                            {
                                                novel.Main.AddRaw( actor.GetDisplayName(), ColorTheme.RedOrange2 ).Line();
                                                itemCount++;

                                                if ( itemCount >= 10 )
                                                    break;
                                            }
                                            novel.Main.EndLineHeight();

                                            novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughUnits_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "DeepObsession":
                        #region DeepObsession
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = FlagRefs.IsExperiencingObsession.DuringGameplay_IsTripped;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Basic( novel );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "SyndicateShakedownOfBusiness":
                        #region SyndicateShakedownOfBusiness
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( SimMetagame.CurrentChapterNumber <= 1 )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = (FlagRefs.Ch2_VRPodRevisions?.DuringGame_IsActive??false) ||
                                            (FlagRefs.Ch2_VRPodRevisions2?.DuringGame_IsActive ?? false) ||
                                            (FlagRefs.Ch2_MIN_FailureToThrive?.DuringGame_IsActive ?? false) ||
                                            (FlagRefs.Ch2_MIN_InfluencerMarketing?.DuringGame_IsActive ?? false) ||
                                            (FlagRefs.Ch2_MIN_CompanyGrowth?.DuringGame_IsActive ?? false);
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Basic( novel );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "ExcessResources":
                        #region ExcessResources
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.ResourcesWithExcess.Count > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    if ( !Window_PlayerResources.Instance.IsOpen )
                                        Window_PlayerResources.Instance.Open();
                                    Window_PlayerResources.customParent.currentlyRequestedDisplayType = Window_PlayerResources.ResourcesDisplayType.ResourceStorage;
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.ResourcesWithExcess.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, SimCommon.ResourcesWithExcess.Count, SimCommon.ResourcesWithExcess.Count );

                                            foreach ( ResourceType resource in SimCommon.ResourcesWithExcess.GetDisplayList() )
                                            {
                                                if ( resource.ExcessOverage > 0 )
                                                    novel.Main.AddBoldRawAndAfterLineItemHeader( resource.GetDisplayName(), ColorTheme.DataLabelWhite )
                                                        .AddSpriteStyled_NoIndent( resource.Icon, AdjustedSpriteStyle.InlineLarger1_2, resource.IconColorHex )
                                                        .AddRaw( resource.ExcessOverage.ToStringThousandsWhole(), IconRefs.ResourceExcess.DefaultColorTextHex ).Line();
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "ProjectsCausingAttacks":
                        #region ProjectsCausingAttacks
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = SimCommon.ActiveProjectsThatCauseAttacks.Count > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    if ( !Window_PlayerResources.Instance.IsOpen )
                                        Window_PlayerResources.Instance.Open();
                                    Window_PlayerResources.customParent.currentlyRequestedDisplayType = Window_PlayerResources.ResourcesDisplayType.ResourceStorage;
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.ActiveProjectsThatCauseAttacks.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, SimCommon.ActiveProjectsThatCauseAttacks.Count, SimCommon.ActiveProjectsThatCauseAttacks.Count );

                                            foreach ( MachineProject project in SimCommon.ActiveProjectsThatCauseAttacks.GetDisplayList() )
                                            {
                                                novel.Main.AddRaw( project.GetDisplayName(), ColorTheme.DataBlue ).Line();
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "TPSReports":
                        #region TPSReports
                        {
                            int problemCount = 0;
                            if ( FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                            {
                                if ( SimCommon.ProductionJobsWithProblems.Count > 0 )
                                {
                                    foreach ( MachineJob item in SimCommon.ProductionJobsWithProblems.GetDisplayList() )
                                    {
                                        if ( !item.IsTPSReportIgnored )
                                            problemCount++;
                                    }
                                }
                            }

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = problemCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    if ( !Window_PlayerResources.Instance.IsOpen )
                                        Window_PlayerResources.Instance.Open();
                                    Window_PlayerResources.customParent.currentlyRequestedDisplayType = Window_PlayerResources.ResourcesDisplayType.TPSReports;
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( problemCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, problemCount, problemCount );

                                            novel.Main.StartStyleLineHeightA();
                                            int itemCount = 0;
                                            foreach ( MachineJob job in SimCommon.ProductionJobsWithProblems.GetDisplayList() )
                                            {
                                                if ( job.IsTPSReportIgnored )
                                                    continue;
                                                if ( job.DuringGame_OutputEffectivenessPercentage.Display < 100 )
                                                {
                                                    novel.Main.AddRaw( job.DuringGame_OutputEffectivenessPercentage.Display.ToStringIntPercent(), ColorTheme.RedOrange2 )
                                                        .Space2x().AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                                        .AddRaw( job.GetDisplayName() ).Line();
                                                    itemCount++;
                                                }

                                                if ( itemCount >= 10 )
                                                    break;
                                            }
                                            novel.Main.EndLineHeight();
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "Ledger":
                        #region Ledger
                        {
                            int majorProblemCount = 0;
                            if ( FlagRefs.ResourceAnalyst.DuringGameplay_IsInvented )
                            {
                                if ( SimCommon.ProductionResourcesWithMajorProblems.Count > 0 )
                                {
                                    foreach ( ResourceType resource in SimCommon.ProductionResourcesWithMajorProblems.GetDisplayList() )
                                    {
                                        if ( !resource.IsLedgerIgnored )
                                            majorProblemCount++;
                                    }
                                }
                            }

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = majorProblemCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    if ( !Window_PlayerResources.Instance.IsOpen )
                                        Window_PlayerResources.Instance.Open();
                                    Window_PlayerResources.customParent.currentlyRequestedDisplayType = Window_PlayerResources.ResourcesDisplayType.Ledger;
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( majorProblemCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, majorProblemCount, majorProblemCount );

                                            //novel.Main.StartStyleLineHeightA();
                                            //int itemCount = 0;
                                            //foreach ( ResourceType job in SimCommon.ProductionResourcesWithMajorProblems.GetDisplayList() )
                                            //{
                                            //    if ( job.DuringGame_OutputEffectivenessPercentage.Display < 100 )
                                            //    {
                                            //        novel.Main.AddRaw( job.DuringGame_OutputEffectivenessPercentage.Display.ToStringIntPercent(), ColorTheme.RedOrange2 )
                                            //            .Space2x().AddSpriteStyled_NoIndent( job.Icon, AdjustedSpriteStyle.InlineLarger1_2 )
                                            //            .AddRaw( job.GetDisplayName() ).Line();
                                            //        itemCount++;
                                            //    }

                                            //    if ( itemCount >= 10 )
                                            //        break;
                                            //}
                                            //novel.Main.EndLineHeight();
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "ExoCorpMilitaryInvasion":
                        #region ExoCorpMilitaryInvasion
                        {
                            int turnsRemaining = 0;
                            turnsRemaining = MathA.Max( turnsRemaining,( int)FlagRefs.DagekonInvadesUntilTurn.GetScore() - SimCommon.Turn );
                            turnsRemaining = MathA.Max( turnsRemaining, (int)FlagRefs.TheUIHInvadesUntilTurn.GetScore() - SimCommon.Turn );
                            if ( FlagRefs.Ch2_IsWW4Ongoing.DuringGameplay_IsTripped )
                                turnsRemaining = 99;

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = turnsRemaining > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( turnsRemaining.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format1( novel, turnsRemaining );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "VorsiberMilitarySweeps":
                        #region VorsiberMilitarySweeps
                        {
                            int turnsRemaining = 0;
                            turnsRemaining = MathA.Max( turnsRemaining, (int)FlagRefs.MimicEscapeVorsiberSweepsUntilTurn.GetScore() - SimCommon.Turn );

                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = turnsRemaining > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( turnsRemaining.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format1( novel, turnsRemaining );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "NewHandbookEntries":
                        #region NewHandbookEntries
                        {
                            int handbookCount = MachineHandbookEntry.DuringGame_NonDismissedMidVisibilityEntries;
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    Alert.DuringGameplay_ShouldBeVisibleNow = handbookCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                    {
                                        foreach ( MachineHandbookEntry entry in MachineHandbookEntryTable.Instance.Rows )
                                        {
                                            if ( entry.IsMidVisibilityEntry && entry.Meta_HasBeenUnlocked && !entry.Meta_HasMidVisibilityBeenDismissed )
                                                entry.Meta_HasMidVisibilityBeenDismissed = true;
                                        }

                                        VisCommands.CloseAllBigOpenWindows( WindowCloseReason.UserDirectRequest );
                                        Window_Handbook.Instance.Open();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        foreach ( MachineHandbookEntry entry in MachineHandbookEntryTable.Instance.Rows )
                                        {
                                            if ( entry.IsMidVisibilityEntry && entry.Meta_HasBeenUnlocked && !entry.Meta_HasMidVisibilityBeenDismissed )
                                                entry.Meta_HasMidVisibilityBeenDismissed = true;
                                        }
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false; //make it go away faster
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( handbookCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, handbookCount, handbookCount );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "AttacksPlannedAgainstYou":
                        #region AttacksPlannedAgainstYou
                        {
                            int attackCount = SimCommon.AttackPlanIncomingDamageToPlayerUnits.Count;
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( SimCommon.NPCsWaitingToActOnTheirOwn.Count > 0 || SimCommon.NPCsWaitingToActAfterPlayerLooksAtThem.Count > 0 )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = attackCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                    {
                                        //if ( Window_NoteLog.Instance.IsOpen )
                                        //    Window_NoteLog.Instance.Close( WindowCloseReason.UserDirectRequest );
                                        //Window_Handbook.Instance.Open();
                                        SimCommon.CycleThroughAttackPlanIncomingDamageToPlayerUnits();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        SimCommon.CycleThroughAttackPlanIncomingDamageToPlayerUnits();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( attackCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, attackCount, attackCount );

                                            novel.Main.StartStyleLineHeightA();
                                            List<AttackPlanIncoming> displayList = SimCommon.AttackPlanIncomingDamageToPlayerUnits.GetDisplayList();
                                            for ( int i = 0; i < 30 && i < displayList.Count; i++ )
                                            {
                                                AttackPlanIncoming incoming = displayList[i];

                                                int current = incoming.Target?.GetActorDataCurrent( ActorRefs.ActorHP, true ) ?? 0;
                                                if ( current <= 0 || incoming.IncomingPhysicalDamage <= 0 )
                                                    continue;

                                                int percentage;
                                                if ( incoming.IncomingPhysicalDamage >= current )
                                                    percentage = 100;
                                                else
                                                    percentage = Mathf.FloorToInt( ((float)incoming.IncomingPhysicalDamage / (float)current) * 100f );

                                                novel.Main.AddRaw( percentage.ToStringIntPercent(), percentage >= 100 ? ColorTheme.RedOrange2 : ColorTheme.CategorySelectedBlue )
                                                    .Position40().AddRaw( incoming.Target?.GetDisplayName() ?? "??", ColorTheme.DataLabelWhite ).Line();
                                            }

                                            novel.Main.EndLineHeight();
                                        }                                        
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "HackersTargetingYourInfiltrators":
                        #region HackersTargetingYourInfiltrators
                        {
                            int attackCount = SimCommon.NPCHackersThreateningPlayerInfiltrators.Count;
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( SimCommon.IsCurrentlyRunningSimTurn )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = attackCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                    {
                                        //if ( Window_NoteLog.Instance.IsOpen )
                                        //    Window_NoteLog.Instance.Close( WindowCloseReason.UserDirectRequest );
                                        //Window_Handbook.Instance.Open();
                                        SimCommon.CycleThroughNPCHackersThreateningPlayerInfiltrators();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        SimCommon.CycleThroughNPCHackersThreateningPlayerInfiltrators();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( attackCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, attackCount, attackCount );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "HackersTargetingYourSecrets":
                        #region HackersTargetingYourSecrets
                        {
                            int attackCount = SimCommon.NPCHackersThreateningPlayerSecrets.Count;
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( SimCommon.IsCurrentlyRunningSimTurn )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = attackCount > 0;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                    {
                                        //if ( Window_NoteLog.Instance.IsOpen )
                                        //    Window_NoteLog.Instance.Close( WindowCloseReason.UserDirectRequest );
                                        //Window_Handbook.Instance.Open();
                                        SimCommon.CycleThroughNPCHackersThreateningPlayerSecrets();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    {
                                        SimCommon.CycleThroughNPCHackersThreateningPlayerSecrets();
                                    }
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( attackCount.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    {
                                        if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                        {
                                            Alert.HandleTooltip_Format2( novel, attackCount, attackCount );
                                        }
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "EstablishVREnvironment":
                        #region EstablishVREnvironment  / PrepareVRCradles
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.InitialVRSimulation.DuringGameplay_IsInvented && !SimCommon.GetIsVirtualRealityEstablished() )
                                        Alert.DuringGameplay_ShouldBeVisibleNow = CityStatisticTable.GetScore( "VRDaySeatsInstalled" ) >= 2000;
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    Window_VirtualRealityNameWindow.Instance.Open();
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    int installed = (int)CityStatisticTable.GetScore( "VRDaySeatsInstalled" );
                                    if ( installed < 2000 )
                                        BufferOrNull.AddRaw( Mathf.RoundToInt( ((float)installed / 2000f) * 100 ).ToStringIntPercent() );
                                    else
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.ChooseProjectTarget.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.GetRedOrange2( IsHovered ) );

                                    BufferOrNull.Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Format2( novel, CityStatisticTable.GetScore( "VRDaySeatsInstalled" ),
                                            CityStatisticTable.GetScore( "VRDaySeatsPotential" ) );
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    case "TooManyStructures":
                        #region TooManyStructures
                        {
                            switch ( Logic )
                            {
                                case OtherChecklistAlertLogic.DoPerQuarterSecond:
                                    if ( FlagRefs.IsChapterOneTutorialDone.DuringGameplay_IsTripped )
                                    {
                                        if ( SimCommon.StructuresWithInternalRoboticsShortage.Count > 0 )
                                        {
                                            bool foundOneStillShort = false;
                                            foreach ( MachineStructure structure in SimCommon.StructuresWithInternalRoboticsShortage.GetDisplayList() )
                                            {
                                                bool isReallyShort = (structure?.CurrentJob?.InternalRoboticsTypeNeeded?.DuringGame_CalculateRemainingForInternalRoboticsForJobs()??0) < 0;
                                                if ( isReallyShort )
                                                {
                                                    foundOneStillShort = true;
                                                    break;
                                                }
                                            }
                                            Alert.DuringGameplay_ShouldBeVisibleNow = foundOneStillShort;
                                        }
                                        else
                                            Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    }
                                    else
                                        Alert.DuringGameplay_ShouldBeVisibleNow = false;
                                    break;
                                case OtherChecklistAlertLogic.OnClicked_Left:
                                case OtherChecklistAlertLogic.OnClicked_Right:
                                    SimCommon.CycleThroughMachineStructures( false, ( MachineStructure s ) => s.CurrentJob != null && s.DoesJobHaveShortageOfInternalRobotics );
                                    break;
                                case OtherChecklistAlertLogic.WriteBriefText:
                                    BufferOrNull.AddRaw( SimCommon.StructuresWithInternalRoboticsShortage.Count.ToString() ).Position20();
                                    Alert.WriteTextPart( BufferOrNull, IsHovered );
                                    break;
                                case OtherChecklistAlertLogic.WriteTooltip:
                                    if ( novel.TryStartSmallerTooltip( TooltipID.Create( Alert ), MustBeAboveOrBelow, Clamp, TooltipNovelWidth.Simple, TooltipExtraText.None, ExtraRules ) )
                                    {
                                        Alert.HandleTooltip_Format2( novel, SimCommon.StructuresWithInternalRoboticsShortage.Count, SimCommon.AllNPCsWithDialog.Count );

                                        novel.Main.AddLeftClickFormat( "LeftClickToCycleThroughStructures_RelatedToNotice", ColorTheme.SoftGold ).Line();
                                    }
                                    break;
                            }
                        }
                        #endregion
                        break;
                    default:
                        if ( !Alert.HasShownHandlerMissingError )
                        {
                            Alert.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "OtherAlerts_Basic: No handler is set up for '" + Alert.ID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Alert.HasShownCaughtError )
                {
                    Alert.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "OtherAlerts_Basic: Error in '" + Alert.ID + "': " + e, Verbosity.ShowAsError );
                }
            }
        }
    }
}

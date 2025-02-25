using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis
{
    public class Projects_ChapterTwo : IProjectHandlerImplementation
    {
        public void HandleLogicForProjectOutcome( ProjectLogic Logic, ArcenCharacterBufferBase BufferOrNull, MachineProject Project, ProjectOutcome OutcomeOrNoneYet,
            MersenneTwister RandOrNull, out bool CanBeCompletedNow )
        {
            CanBeCompletedNow = false;
            if ( Project == null )
            {
                ArcenDebugging.LogSingleLine( "Null project outcome passed to Projects_ChapterTwo!", Verbosity.ShowAsError );
                return;
            }

            try
            {
                switch ( Project.ID )
                {
                    #region Ch2Only_MIN_IntelligenceClass4
                    case "Ch2Only_MIN_IntelligenceClass4":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int goal = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 5000 );
                            int currentClass = SimMetagame.IntelligenceClass;
                            CanBeCompletedNow = currentClass >= goal;

                            int percentage = 0;
                            Int64 neuralProcessing = 0;
                            Int64 cutoffInt = 0;
                            if ( currentClass < 4 )
                            {
                                if ( currentClass < 3 )
                                    percentage = 0;
                                else
                                {
                                    foreach ( CityTimeline timeline in SimMetagame.AllTimelines.Values )
                                        neuralProcessing += timeline.NeuralProcessing;

                                    ISeverity nextCutoff = ScaleRefs.IntelligenceClass.GetSeverityFromScale( neuralProcessing );
                                    cutoffInt = nextCutoff.CutoffInt;

                                    percentage = MathA.IntPercentage( neuralProcessing, cutoffInt );
                                    if ( percentage > 99 )
                                        percentage = 99;
                                }
                            }
                            else
                                percentage = 100;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromOnePercentageNumber( Logic, OutcomeOrNoneYet, percentage, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( currentClass == 2 )
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "NeuralProcessingForUpgrade" ).AddFormat2( "OutOF",
                                            neuralProcessing.ToStringLargeNumberAbbreviated(), cutoffInt.ToStringLargeNumberAbbreviated() ).Line();
                                    }
                                    else
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "RaiseYourselfToIntelligenceClass" ).AddRaw( goal.ToString() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SolveHomelessness
                    case "Ch2_MIN_SolveHomelessness":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "HomelessTentsRemaining" );

                            int current = (int)statistic.GetScore();
                            int originalSet = MathA.Max( current, 8000 );

                            CanBeCompletedNow = current == 0 && SimCommon.HasRunAtLeastOneRealSecond;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbersMovingDownToZero( Logic, OutcomeOrNoneYet, current, originalSet, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat2( "RemainingPrefixedAmount", current.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SurviveThisApocalypse
                    case "Ch2_MIN_SurviveThisApocalypse":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            bool areAnyComplete = false;
                            foreach ( TimelineGoal goal in TimelineGoalTable.Instance.Rows)
                            {
                                if ( !goal.BlockedBeforeFinalDoom || goal.BlockedAfterFinalDoom )
                                    continue;
                                if ( goal.GetAreAnyPathsCompleteInThisTimeline() )
                                {
                                    areAnyComplete = true;
                                    break;
                                }
                            }

                            CanBeCompletedNow = areAnyComplete;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    if ( !Project.DuringGame_HasDoneAnyNeededInitialization )
                                    {
                                        Project.DuringGame_HasDoneAnyNeededInitialization = true;
                                        DoAfterFinalDoom( Project, RandOrNull );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_MakeEngineersOfYourNickelbots
                    case "Ch2_MIN_MakeEngineersOfYourNickelbots":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = CommonRefs.NickelbotUnitType.DuringGameData.PossibleActorDataMinMax.GetDisplayDict()[ActorRefs.ActorEngineeringSkill].LeftItem;
                            int target = 200;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ActorRefs.ActorEngineeringSkill.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SearchingForFriends
                    case "Ch2_MIN_SearchingForFriends":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = (int)CityStatisticTable.GetScore( "SearchFriendsInventions" ) + ( UnlockTable.Instance.GetRowByID( "RememberingRed" ).DuringGameplay_IsInvented ? 1 : 0 );
                            int target = 3;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat2( "OutOF", current.ToStringThousandsWhole(), target.ToStringThousandsWhole() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ProbeTheTitan
                    case "Ch2_MIN_ProbeTheTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = (int)ResourceTypeTable.Instance.GetRowByID( "EncryptedTitanSystemsData" ).Current +
                                (int)ResourceTypeTable.Instance.GetRowByID( "DecryptedTitanSystemsData" ).Current +
                                (int)ResourceTypeTable.Instance.GetRowByID( "EncryptedTitanCommsLog" ).Current +
                                (int)ResourceTypeTable.Instance.GetRowByID( "DecryptedTitanCommsLog" ).Current;
                            int target = 100100;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), Lang.Get( "EncryptedData" ) ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    if ( !Project.DuringGame_HasDoneAnyNeededInitialization )
                                    {
                                        Project.DuringGame_HasDoneAnyNeededInitialization = true;
                                        CommonRefs.CombatUnitRedUnitType.DuringGameData.TryAssignAbilityToFirstOpenSlotIfNotAlreadyEquipped(
                                            AbilityTypeTable.Instance.GetRowByID( "ProbeExoticComms" ) );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DecryptDataFromTitan
                    case "Ch2_MIN_DecryptDataFromTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = (int)ResourceTypeTable.Instance.GetRowByID( "EncryptedTitanSystemsData" ).Current +
                                (int)ResourceTypeTable.Instance.GetRowByID( "EncryptedTitanCommsLog" ).Current;
                            int original = 100100;
                            if ( original < current )
                                original = current;

                            CanBeCompletedNow = current <= 0;

                            if ( CanBeCompletedNow )
                            {
                                int systemsData = (int)ResourceTypeTable.Instance.GetRowByID( "DecryptedTitanSystemsData" ).Current;
                                int commsLog = (int)ResourceTypeTable.Instance.GetRowByID( "DecryptedTitanCommsLog" ).Current;

                                if ( systemsData > 0 )
                                    OtherKeyMessageTable.Instance.GetRowByID( "TitanSystemData_1" ).DuringGameplay_IsReadyToBeViewed = true;
                                if ( commsLog > 0 )
                                    OtherKeyMessageTable.Instance.GetRowByID( "TitanCommsData_1" ).DuringGameplay_IsReadyToBeViewed = true;
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbersMovingDownToZero( Logic, OutcomeOrNoneYet, current, original, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat2( "OutOF", (original - current ).ToStringThousandsWhole(), original.ToStringThousandsWhole() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DoSomethingAboutTheTitan
                    case "Ch2_MIN_DoSomethingAboutTheTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            CanBeCompletedNow = CityFlagTable.Instance.GetRowByID( "ImmobilizedTheTiitan" ).DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DoSomethingAboutTheTitan2
                    case "Ch2_MIN_DoSomethingAboutTheTitan2":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            CanBeCompletedNow = CityFlagTable.Instance.GetRowByID( "OfferedToHelpTheTitan" ).DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SpaceNationFugitives
                    case "Ch2_MIN_SpaceNationFugitives":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType safehouseHumans = ResourceRefs.SafehouseHumans;
                            ResourceType gristloaf = ResourceRefs.Gristloaf;
                            ResourceType filteredWater = ResourceRefs.FilteredWater;

                            int gristloafGoal = OutcomeOrNoneYet.GetSingleIntByID( "GristGoal", 3000 );
                            int waterGoal = OutcomeOrNoneYet.GetSingleIntByID( "WaterGoal", 500 );

                            int currentGrist = (int)gristloaf.GetActualTrendWithLieIfStorageAtLeast( gristloafGoal, 32000 );
                            int currentWater = (int)filteredWater.GetActualTrendWithLieIfStorageAtLeast( waterGoal, 400000 );

                            int safehouseHumanCapGoal = 483;

                            CanBeCompletedNow = safehouseHumans.HardCap >= safehouseHumanCapGoal && currentGrist >= gristloafGoal && currentWater >= waterGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromSixNumbers( Logic, OutcomeOrNoneYet, (int)safehouseHumans.HardCap, safehouseHumanCapGoal, currentGrist, gristloafGoal,
                                        currentWater, waterGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", safehouseHumans.HardCap.ToStringThousandsWhole(), safehouseHumanCapGoal.ToStringThousandsWhole(), safehouseHumans.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentGrist.ToStringThousandsWhole(), gristloafGoal.ToStringThousandsWhole(), gristloaf.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentWater.ToStringThousandsWhole(), waterGoal.ToStringThousandsWhole(), filteredWater.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", safehouseHumans.HardCap.ToStringThousandsWhole(), safehouseHumanCapGoal.ToStringThousandsWhole(), safehouseHumans.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentGrist.ToStringThousandsWhole(), gristloafGoal.ToStringThousandsWhole(), gristloaf.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentWater.ToStringThousandsWhole(), waterGoal.ToStringThousandsWhole(), filteredWater.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion  
                    #region Ch2_MIN_NegotiateWithTheTitan
                    case "Ch2_MIN_NegotiateWithTheTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType safehouseHumans = ResourceTypeTable.Instance.GetRowByID( "SafehouseHumans" );
                            int current = (int)safehouseHumans.Current;
                            int target = 483;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), safehouseHumans.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_FurtherDiscussionsWithTheTitan
                    case "Ch2_MIN_FurtherDiscussionsWithTheTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            CanBeCompletedNow = CityFlagTable.Instance.GetRowByID( "PrepareToHackTheTitan" ).DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_HackTheTitan
                    case "Ch2_MIN_HackTheTitan":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            CanBeCompletedNow = FlagRefs.BridgedWithLAKE.DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DoSomeQuietLooting
                    case "Ch2_MIN_DoSomeQuietLooting":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = Project.DuringGame_HasDoneAnyNeededInitialization ? (int)ResourceRefs.Alumina.Current : 0;
                            int target = 800;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ResourceRefs.Alumina.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    if ( !Project.DuringGame_HasDoneAnyNeededInitialization )
                                    {
                                        Project.DuringGame_HasDoneAnyNeededInitialization = true;
                                        ResourceRefs.Alumina.SetCurrent_Named( 0, string.Empty, false );

                                        CommonRefs.CarverUnitType.DuringGameData.TryAssignAbilityToFirstOpenSlotIfNotAlreadyEquipped(
                                            AbilityTypeTable.Instance.GetRowByID( "QuietlyLoot" ) );
                                        CommonRefs.ExatorUnitType.DuringGameData.TryAssignAbilityToFirstOpenSlotIfNotAlreadyEquipped(
                                            AbilityTypeTable.Instance.GetRowByID( "QuietlyLoot" ) );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_RemembranceOfIdentity
                    case "Ch2_MIN_RemembranceOfIdentity":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = Project.DuringGame_IntendedOutcome?.DuringGame_CountTotalStreetSenseItemsDoneSoFar() ?? 0;
                            int target = 8;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat2( "RequiredActionsTaken", current.ToStringThousandsWhole(), target.ToStringThousandsWhole() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_KillAllAGIResearchers
                    case "Ch2_MIN_KillAllAGIResearchers":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int researchersToKill = 42;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "AGIReserchersMurdered" );

                            int current = (int)statistic.GetScore();
                            int target = researchersToKill;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_HelpTheAGIResearchers
                    case "Ch2_MIN_HelpTheAGIResearchers":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType fugitives = ResourceRefs.FugitiveAGIResearchers;
                            ResourceType greens = ResourceRefs.HydroponicGreens;
                            ResourceType meat = ResourceRefs.VatGrownMeat;
                            ResourceType water = ResourceRefs.FilteredWater;

                            int fugitivesGoalCap = 42;
                            int greensGoal = OutcomeOrNoneYet.GetSingleIntByID( "GreensGoal", 100 );
                            int meatGoal = OutcomeOrNoneYet.GetSingleIntByID( "MeatGoal", 100 );
                            int waterGoal = OutcomeOrNoneYet.GetSingleIntByID( "WaterGoal", 100 );

                            int greensCurrent = (int)greens.GetActualTrendWithLieIfStorageAtLeast( greensGoal, 32000 );
                            int meatCurrent = (int)meat.GetActualTrendWithLieIfStorageAtLeast( meatGoal, 400000 );
                            int waterCurrent = (int)water.GetActualTrendWithLieIfStorageAtLeast( waterGoal, 400000 );

                            int fugitivesCurrentCap = (int)fugitives.HardCap;

                            CanBeCompletedNow = fugitivesCurrentCap >= fugitivesGoalCap && greensCurrent >= greensGoal &&
                                meatCurrent >= meatGoal && waterCurrent >= waterGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromEightNumbers( Logic, OutcomeOrNoneYet, fugitivesCurrentCap, fugitivesGoalCap, greensCurrent, greensGoal,
                                       meatCurrent, meatGoal, waterCurrent, waterGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", fugitivesCurrentCap.ToStringThousandsWhole(), fugitivesGoalCap.ToStringThousandsWhole(), ResourceRefs.FugitiveAGIResearchers.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", greensCurrent.ToStringThousandsWhole(), greensGoal.ToStringThousandsWhole(), greens.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", meatCurrent.ToStringThousandsWhole(), meatGoal.ToStringThousandsWhole(), meat.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", waterCurrent.ToStringThousandsWhole(), waterGoal.ToStringThousandsWhole(), water.GetDisplayName() );

                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", fugitivesCurrentCap.ToStringThousandsWhole(), fugitivesGoalCap.ToStringThousandsWhole(), ResourceRefs.FugitiveAGIResearchers.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", greensCurrent.ToStringThousandsWhole(), greensGoal.ToStringThousandsWhole(), greens.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", meatCurrent.ToStringThousandsWhole(), meatGoal.ToStringThousandsWhole(), meat.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", waterCurrent.ToStringThousandsWhole(), waterGoal.ToStringThousandsWhole(), water.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_AGIEspia
                    case "Ch2_AGIEspia":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType stolenData = ResourceTypeTable.Instance.GetRowByID( "EncryptedEspiaTelecomLogs" );
                            int currentData = (int)stolenData.Current;
                            int dataGoal = 200000;
                            CanBeCompletedNow = currentData >= dataGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentData, dataGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentData.ToStringThousandsWhole(), dataGoal.ToStringThousandsWhole(), stolenData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIEspiaData
                    case "Ch2_MIN_AGIEspiaData":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType encryptedData = ResourceTypeTable.Instance.GetRowByID( "EncryptedEspiaTelecomLogs" );
                            ResourceType decryptedData = ResourceTypeTable.Instance.GetRowByID( "DecryptedEspiaTelecomLogs" );
                            int current = (int)decryptedData.Current;
                            int target = (int)encryptedData.Current + (int)decryptedData.Current;
                            CanBeCompletedNow = target > 0 && current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), decryptedData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIEspiaReplay
                    case "Ch2_MIN_AGIEspiaReplay":
                        if ( OutcomeOrNoneYet != null )
                        {
                            bool foundAtLeastOneThatIsOkay = false;
                            if ( JobRefs.NetworkReplayBunker.DuringGame_NumberFunctional.Display > 0 )
                            {
                                foreach ( MachineStructure structure in JobRefs.NetworkReplayBunker.DuringGame_FullList.GetDisplayList() )
                                {
                                    if ( !structure.HasHadFirstWaveFromAnyAggressorsSinceActive )
                                        continue;
                                    if ( !structure.IsFunctionalJob || !structure.IsFunctionalStructure )
                                        continue;
                                    MapActorData deterrence = structure.GetActorDataData( ActorRefs.JobRequiredDeterrence, true );
                                    if ( deterrence == null || deterrence.Current < deterrence.Maximum )
                                        continue;
                                    if ( structure.CalculateExtantManagedMachineUnitsTargetingThisStructure() > 0 )
                                        continue;
                                    foundAtLeastOneThatIsOkay = true;
                                }
                            }

                            CanBeCompletedNow = foundAtLeastOneThatIsOkay;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_BuildAndDefend" ).AddRaw( JobRefs.NetworkReplayBunker.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    //if ( JobRefs.NetworkReplayBunker.DuringGame_NumberFunctional.Display > 0 )
                                    //{
                                    //    foreach ( MachineStructure structure in JobRefs.NetworkReplayBunker.DuringGame_FullList.GetDisplayList() )
                                    //    {
                                    //        if ( !structure.HasHadFirstWaveFromAnyAggressorsSinceActive )
                                    //        {
                                    //            BufferOrNull.AddNeverTranslated( "NO-HasHadFirstWaveFromAnyAggressorsSinceActive", true );
                                    //            continue;
                                    //        }
                                    //        if ( !structure.IsFunctionalJob || !structure.IsFunctionalStructure )
                                    //        {
                                    //            BufferOrNull.AddNeverTranslated( "IsFunctionalJob: " + structure.IsFunctionalJob + " IsFunctionalStructure: " + structure.IsFunctionalStructure, true );
                                    //            continue;
                                    //        }
                                    //        MapActorData deterrence = structure.GetActorDataData( ActorRefs.JobRequiredDeterrence, true );
                                    //        if ( deterrence == null || deterrence.Current < deterrence.Maximum )
                                    //        {
                                    //            if ( deterrence == null )
                                    //                BufferOrNull.AddNeverTranslated( "deterrence is null!", true );
                                    //            else
                                    //                BufferOrNull.AddNeverTranslated( "deterrence is " + deterrence.Current + " of " + deterrence.Maximum, true );
                                    //            continue;
                                    //        }
                                    //        if ( structure.CalculateExtantManagedMachineUnitsTargetingThisStructure() > 0 )
                                    //        {
                                    //            BufferOrNull.AddNeverTranslated( "extant units: " + structure.CalculateExtantManagedMachineUnitsTargetingThisStructure(), true );
                                    //            continue;
                                    //        }
                                    //        foundAtLeastOneThatIsOkay = true;
                                    //    }
                                    //}
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGICrucible
                    case "Ch2_MIN_AGICrucible":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 36;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "NeuralCrucible_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIBridge
                    case "Ch2_MIN_AGIBridge":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 12;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "NeuralBridge_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGICrucibleBuild
                    case "Ch2_MIN_AGICrucibleBuild":
                        if ( OutcomeOrNoneYet != null )
                        {
                            bool foundAtLeastOneThatIsOkay = false;
                            if ( JobRefs.NeuralCrucible.DuringGame_NumberFunctional.Display > 0 )
                            {
                                foreach ( MachineStructure structure in JobRefs.NeuralCrucible.DuringGame_FullList.GetDisplayList() )
                                {
                                    if ( !structure.HasHadFirstWaveFromAnyAggressorsSinceActive )
                                        continue;
                                    if ( !structure.IsFunctionalJob || !structure.IsFunctionalStructure )
                                        continue;
                                    MapActorData deterrence = structure.GetActorDataData( ActorRefs.JobRequiredDeterrence, true );
                                    if ( deterrence == null || deterrence.Current < deterrence.Maximum )
                                        continue;
                                    if ( structure.CalculateExtantManagedMachineUnitsTargetingThisStructure() > 0 )
                                        continue;
                                    foundAtLeastOneThatIsOkay = true;
                                }
                            }

                            CanBeCompletedNow = foundAtLeastOneThatIsOkay;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_BuildAndDefend" ).AddRaw( JobRefs.NeuralCrucible.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIBridgeBuild
                    case "Ch2_MIN_AGIBridgeBuild":
                        if ( OutcomeOrNoneYet != null )
                        {
                            bool foundAtLeastOneThatIsOkay = false;
                            if ( JobRefs.NeuralBridge.DuringGame_NumberFunctional.Display > 0 )
                            {
                                foreach ( MachineStructure structure in JobRefs.NeuralBridge.DuringGame_FullList.GetDisplayList() )
                                {
                                    if ( !structure.HasHadFirstWaveFromAnyAggressorsSinceActive )
                                        continue;
                                    if ( !structure.IsFunctionalJob || !structure.IsFunctionalStructure )
                                        continue;
                                    MapActorData deterrence = structure.GetActorDataData( ActorRefs.JobRequiredDeterrence, true );
                                    if ( deterrence == null || deterrence.Current < deterrence.Maximum )
                                        continue;
                                    if ( structure.CalculateExtantManagedMachineUnitsTargetingThisStructure() > 0 )
                                        continue;
                                    foundAtLeastOneThatIsOkay = true;
                                }
                            }

                            CanBeCompletedNow = foundAtLeastOneThatIsOkay;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_BuildAndDefend" ).AddRaw( JobRefs.NeuralBridge.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_TheGreatAGIDebate
                    case "Ch2_MIN_TheGreatAGIDebate":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int totalTurns = 20;
                            int turnsElapsed = SimCommon.Turn - Project.DuringGameplay_TurnStarted;
                            int turnsRemaining = MathA.Max( 0, totalTurns - turnsElapsed );

                            CanBeCompletedNow = turnsRemaining <= 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WriteTurnCountdown( Logic, OutcomeOrNoneYet, turnsRemaining, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RustLighter )
                                            .Space1x().AddFormat2( "RequiredTurnsElapsed", turnsElapsed.ToStringThousandsWhole(), totalTurns.ToStringThousandsWhole() ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_LaserPrinterMishap
                    case "Ch2_LaserPrinterMishap":
                        //regardless of outcome chosen
                        {
                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    if ( ResourceRefs.FugitiveAGIResearchers.CurrentPlusExcess <= 0 )
                                    {
                                        Project.DoOnProjectFail( string.Empty, RandOrNull, false, false );
                                        break;
                                    }

                                    if ( !Project.DuringGame_HasDoneAnyNeededInitialization )
                                    {
                                        Project.DuringGame_HasDoneAnyNeededInitialization = true;
                                        ResourceRefs.FugitiveAGIResearchers.AlterCurrent_Named( -2, "Decrease_DeathByMisadventure", ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        JobHelper.ChangeAllOfOneJobToAnother( "AGIResearcherSafehouse", "AbandonedAGIResearcherSafehouse" );
                                        if ( !JobHelper.StartNPCMissionAtFirstJobType( "AbandonedAGIResearcherSafehouse", "Ch2_OerlAfterAGIResearchers" ) )
                                        {
                                            if ( !JobHelper.StartNPCMissionAtFirstJobType( "AGIResearcherSafehouse", "Ch2_OerlAfterAGIResearchers" ) )
                                            {
                                                if ( !JobHelper.StartNPCMissionAtAnyJob( "Ch2_OerlAfterAGIResearchers" ) )
                                                    ArcenDebugging.LogSingleLine( "Failed to start Ch2_OerlAfterAGIResearchers due to missing AGI Researcher Safehouse (abandoned or otherwise), AND could not start it at any random job, either!", Verbosity.ShowAsError );
                                                else
                                                    ArcenDebugging.LogSingleLine( "Failed to start Ch2_OerlAfterAGIResearchers due to missing AGI Researcher Safehouse (abandoned or otherwise), but was able to start it at a random fallback.", Verbosity.DoNotShow );
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        if ( OutcomeOrNoneYet != null )
                        {
                            int totalTurns = 3;
                            int turnsElapsed = SimCommon.Turn - Project.DuringGameplay_TurnStarted;
                            int turnsRemaining = MathA.Max( 0, totalTurns - turnsElapsed );

                            CanBeCompletedNow = turnsRemaining <= 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WriteTurnCountdown( Logic, OutcomeOrNoneYet, turnsRemaining, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RustLighter )
                                            .Space1x().AddFormat2( "RequiredTurnsToPrepareForEnemyArrival", turnsElapsed.ToStringThousandsWhole(), totalTurns.ToStringThousandsWhole() ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        switch ( OutcomeOrNoneYet.ShortID)
                                        {
                                            case "SlowDownAndGetThePrinters":
                                                CityFlagTable.Instance.GetRowByID( "ChosePrintersOverID" )?.TripIfNeeded();
                                                break;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIDefeatOerl
                    case "Ch2_MIN_AGIDefeatOerl":
                        if ( OutcomeOrNoneYet != null )
                        {
                            NPCMission mission = NPCMissionTable.Instance.GetRowByID( "Ch2_OerlAfterAGIResearchers" );

                            int totalTurns = mission.TurnsBeforeExpiration;
                            int turnsRemaining = mission.DuringGame_CalculateTurnsRemaining();
                            int turnsElapsed = totalTurns - turnsRemaining;

                            if ( !mission.DuringGame_IsActive && mission.DuringGameplay_TimesStarted <= 0 )
                            {
                                if ( !JobHelper.StartNPCMissionAtFirstJobType( "AbandonedAGIResearcherSafehouse", "Ch2_OerlAfterAGIResearchers" ) )
                                {
                                    if ( !JobHelper.StartNPCMissionAtFirstJobType( "AGIResearcherSafehouse", "Ch2_OerlAfterAGIResearchers" ) )
                                    {
                                        if ( !JobHelper.StartNPCMissionAtAnyJob( "Ch2_OerlAfterAGIResearchers" ) )
                                            ArcenDebugging.LogSingleLine( "Failed to start Ch2_OerlAfterAGIResearchers due to missing AGI Researcher Safehouse (abandoned or otherwise), AND could not start it at any random job, either!", Verbosity.ShowAsError );
                                        else
                                            ArcenDebugging.LogSingleLine( "Failed to start Ch2_OerlAfterAGIResearchers due to missing AGI Researcher Safehouse (abandoned or otherwise), but was able to start it at a random fallback.", Verbosity.DoNotShow );
                                    }
                                }
                            }

                            if ( FlagRefs.VorsiberHasPredatorDesign.DuringGameplay_IsTripped && mission.DuringGame_IsActive )
                                mission.DoOnMissionComplete(); //since they don't have the needed points, this will automatically fail it for them

                            if ( mission.DuringGame_WasCompletedByActiveParty )
                            {
                                if ( !Project.DuringGame_HasBeenFailed )
                                {
                                    Project.DoOnProjectFail( "ProjectFailure_MilitaryDefeat", Engine_Universal.PermanentQualityRandom, true, true );
                                    if ( Project.DuringGame_HasBeenFailed )
                                    {
                                        ResourceRefs.FugitiveAGIResearchers.RemoveFromExcessOverage( (int)ResourceRefs.FugitiveAGIResearchers.ExcessOverage / 2 );
                                        if ( ResourceRefs.FugitiveAGIResearchers.ExcessOverage < 3 )
                                            ResourceRefs.FugitiveAGIResearchers.AddToExcessOverage( 3 );
                                    }
                                    MachineProject rescueProject = MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_AGIRescueMission" );
                                    rescueProject.TryStartProject( true, true );
                                }
                            }
                            else if ( mission.DuringGame_SuccessfullyExpiredByDefenders )
                            {
                                CanBeCompletedNow = true;

                                MachineProject nextProject = MachineProjectTable.Instance.GetRowByID(
                                    CityFlagTable.Instance.GetRowByID( "ChosePrintersOverID" ).DuringGameplay_IsTripped ?
                                    "Ch2_MIN_ExamineThePrinters" : "Ch2_MIN_SolveTheIdentityProblem" );
                                nextProject.TryStartProject( true, true );
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WriteTurnCountdown( Logic, OutcomeOrNoneYet, turnsRemaining, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RustLighter )
                                            .Space1x().AddFormat2( "RequiredTurnsToBlockEnemyMission", turnsElapsed.ToStringThousandsWhole(), totalTurns.ToStringThousandsWhole() ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AGIRescueMission
                    case "Ch2_MIN_AGIRescueMission":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int totalTurns = 12;
                            int turnsElapsed = SimCommon.Turn - Project.DuringGameplay_TurnStarted;
                            int turnsRemaining = MathA.Max( 0, totalTurns - turnsElapsed );

                            InvestigationType investigationType = InvestigationTypeTable.Instance.GetRowByID( "Ch2AGIOerlRescue_Stealth" );

                            CanBeCompletedNow = turnsElapsed > 3 && investigationType.DuringGame_GetHasCompleted();

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WriteTurnCountdown( Logic, OutcomeOrNoneYet, turnsRemaining, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_2, ColorTheme.RustLighter )
                                            .Space1x().AddFormat2( "RequiredTurnsToRescueAllies", turnsElapsed.ToStringThousandsWhole(), totalTurns.ToStringThousandsWhole() ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    if ( investigationType.CheckIfAllBuildingsAreDestroyed( true ) )
                                    {
                                        AchievementRefs.CantBeBothered.TripIfNeeded();
                                        Project.DoOnProjectWin( Project.DuringGame_IntendedOutcome, Engine_Universal.PermanentQualityRandom, true, true );
                                        break;
                                    }

                                    if ( turnsRemaining <= 0 && !Project.DuringGame_HasBeenFailed )
                                    {
                                        Project.DoOnProjectFail( "ProjectFailure_CouldNotSaveAlliesInTime", Engine_Universal.PermanentQualityRandom, true, true );

                                        CityFlagTable.Instance.GetRowByID( "VorsiberHasPredatorDesign" ).TripIfNeeded();

                                        MachineProject nextProject = MachineProjectTable.Instance.GetRowByID( 
                                            CityFlagTable.Instance.GetRowByID( "ChosePrintersOverID" ).DuringGameplay_IsTripped ?
                                            "Ch2_MIN_ExamineThePrinters" : "Ch2_MIN_SolveTheIdentityProblem" );
                                        nextProject.TryStartProject( true, true );
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        MachineProject nextProject = MachineProjectTable.Instance.GetRowByID(
                                            CityFlagTable.Instance.GetRowByID( "ChosePrintersOverID" ).DuringGameplay_IsTripped ?
                                            "Ch2_MIN_ExamineThePrinters" : "Ch2_MIN_SolveTheIdentityProblem" );
                                        nextProject.TryStartProject( true, true );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ExamineThePrinters
                    case "Ch2_MIN_ExamineThePrinters":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = 2; //the other half is the contemplation
                            int current = JobRefs.AGIResearcherHoldingPen.DuringGame_NumberFunctional.Display;
                            if ( UnlockTable.Instance.GetRowByID( "Necromancer" ).DuringGameplay_IsInvented )
                                current++;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_BuildAndContemplate" ).AddRaw( JobRefs.AGIResearcherHoldingPen.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SolveTheIdentityProblem
                    case "Ch2_MIN_SolveTheIdentityProblem":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = 2;
                            int current = JobRefs.AGIResearcherVilla.DuringGame_NumberFunctional.Display;

                            ResourceConsumable freshIdentity = ResourceConsumableTable.Instance.GetRowByID( "FreshIdentity" );
                            if ( freshIdentity.GetScore() > 0 )
                                current++;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( JobRefs.AGIResearcherVilla.GetDisplayName() );
                                        BufferOrNull.Space5x();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_UseItem" ).AddRaw( freshIdentity.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_Build" ).AddRaw( JobRefs.AGIResearcherVilla.GetDisplayName() );
                                        BufferOrNull.Line();
                                        BufferOrNull.AddLangAndAfterLineItemHeader( "Prefix_UseItem" ).AddRaw( freshIdentity.GetDisplayName() );
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_EstablishMeaningfulSocialChange
                    case "Ch2_MIN_EstablishMeaningfulSocialChange":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            if ( FlagRefs.Ch2_MIN_LostKids_ThePrisonHeist.DuringGame_ActualOutcome != null )
                                current += 5;
                            current += (33 * (int)FlagRefs.LiquidMetalFurtherPrisonBreaks.GetScore());

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLang( "Unknown" ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        {
                                            MachineProject otherProject = MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_LostKids_StopThePoisoning" );
                                            if ( otherProject.DuringGame_ActualOutcome != null )
                                                Project.DoOnProjectWin( OutcomeOrNoneYet, Engine_Universal.PermanentQualityRandom, false, true );
                                        }
                                        {
                                            MachineProject otherProject = MachineProjectTable.Instance.GetRowByID( "Ch2_LostKids_DownWithSecForce" );
                                            if ( otherProject.DuringGame_ActualOutcome?.ShortID == "BombThem" )
                                                Project.DoOnProjectWin( OutcomeOrNoneYet, Engine_Universal.PermanentQualityRandom, false, true );
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DealWithTheRebels
                    case "Ch2_MIN_DealWithTheRebels":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = CityFlagTable.Instance.GetRowByID( "Ch2_TalkedDownRebelKids" ).DuringGameplay_IsTripped;

                            if ( !CanBeCompletedNow && !Project.DuringGame_HasBeenFailed && NPCManagerTable.Instance.GetRowByID( "Ch2_LostKidsAttackYou" ).DuringGame_HasExhaustedUnitsAllowedToSpawn )
                            {
                                Project.DoOnProjectFail( "ProjectFailure_YouTriedToMurderChildren", Engine_Universal.PermanentQualityRandom, true, true );
                                if ( Project.DuringGame_HasBeenFailed )
                                {
                                    MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_EstablishMeaningfulSocialChange" ).DoOnProjectFail( "ProjectFailure_YouTriedToMurderChildren", Engine_Universal.PermanentQualityRandom, true, true );
                                }
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLang( "Unknown" ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ImproveTheSewerTiger
                    case "Ch2_MIN_ImproveTheSewerTiger":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = FlagRefs.Ch2_GotThumperDesigns.DuringGameplay_IsTripped;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLang( "Unknown" ).Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SearchingForWastelanders
                    case "Ch2_MIN_SearchingForWastelanders":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int locationsSearched = 9;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "Wastelanders_LocationsSearched" );

                            int current = (int)statistic.GetScore();
                            int target = locationsSearched;

                            CanBeCompletedNow = false;
                            if ( current >= target )
                            {
                                if ( OutcomeOrNoneYet.StreetSenseItems["SearchAbandonedHousing"].DuringGameplay_TimesHasBeenDone +
                                    OutcomeOrNoneYet.StreetSenseItems["SearchAbandonedOffice"].DuringGameplay_TimesHasBeenDone >= 4 &&
                                    (Project.DuringGame_LastChosenStreetSenseItem?.ID??string.Empty) == "SearchAbandonedDiner" )
                                    CanBeCompletedNow = true;
                                else
                                {
                                    if ( !Project.DuringGame_HasBeenFailed )
                                    {
                                        Project.DoOnProjectFail( "ProjectFailure_SearchLostKidsWonForYou", Engine_Universal.PermanentQualityRandom, true, true );
                                        if ( Project.DuringGame_HasBeenFailed )
                                        {
                                            NPCEvent nEvent = NPCEventTable.Instance.GetRowByID( "LostKids_MetWasteLanders_Kids" );
                                            NPCCohort wastelanders = NPCCohortTable.Instance.GetRowByID( "Wastelanders" );

                                            QueuedMinorEvent mEvent = new QueuedMinorEvent();
                                            mEvent.Actor = Project.DuringGame_LastChosenStreetSenseActor;
                                            mEvent.MinorEvent = nEvent;
                                            mEvent.BuildingOrNull = Project.DuringGame_LastChosenStreetSenseBuilding;
                                            mEvent.EventCohort = wastelanders;
                                            mEvent.ClearAnyExistingMusicTagsIfAnyExist = false;
                                            SimCommon.QueuedMinorEvents.Enqueue( mEvent );
                                        }
                                    }
                                }
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        NPCEvent nEvent = NPCEventTable.Instance.GetRowByID( "LostKids_MetWasteLanders_You" );
                                        NPCCohort wastelanders = NPCCohortTable.Instance.GetRowByID( "Wastelanders" );

                                        QueuedMinorEvent mEvent = new QueuedMinorEvent();
                                        mEvent.Actor = Project.DuringGame_LastChosenStreetSenseActor;
                                        mEvent.MinorEvent = nEvent;
                                        mEvent.BuildingOrNull = Project.DuringGame_LastChosenStreetSenseBuilding;
                                        mEvent.EventCohort = wastelanders;
                                        mEvent.ClearAnyExistingMusicTagsIfAnyExist = false;
                                        SimCommon.QueuedMinorEvents.Enqueue( mEvent );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ExaminingWastelanderMythology
                    case "Ch2_MIN_ExaminingWastelanderMythology":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int locationsSearched = 11;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "WastelanderMythology_LocationsSearched" );

                            int current = (int)statistic.GetScore();
                            int target = locationsSearched;

                            CanBeCompletedNow = false;
                            if ( current >= target )
                            {
                                CanBeCompletedNow = true;
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        FlagRefs.Ch2_LearnedWastelanderMythology.TripIfNeeded();

                                        NPCEvent nEvent = NPCEventTable.Instance.GetRowByID( "WastelanderMythology_Meeting1" );
                                        NPCCohort wastelanders = NPCCohortTable.Instance.GetRowByID( "Wastelanders" );

                                        QueuedMinorEvent mEvent = new QueuedMinorEvent();
                                        mEvent.Actor = Project.DuringGame_LastChosenStreetSenseActor;
                                        mEvent.MinorEvent = nEvent;
                                        mEvent.BuildingOrNull = Project.DuringGame_LastChosenStreetSenseBuilding;
                                        mEvent.EventCohort = wastelanders;
                                        mEvent.ClearAnyExistingMusicTagsIfAnyExist = false;
                                        SimCommon.QueuedMinorEvents.Enqueue( mEvent );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_LostKids_ThePrisonHeist
                    case "Ch2_MIN_LostKids_ThePrisonHeist":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = false;

                            bool hasLiquidMetal = FlagRefs.MercurialForm.DuringGameplay_IsInvented;
                            bool hasAllies = FlagRefs.Ch2_SecuredNomadSupportForPrisonHeist.DuringGameplay_IsTripped;

                            if ( hasLiquidMetal )
                                current += 25;
                            if ( hasAllies )
                                current += 25;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( hasLiquidMetal ? "ChecklistItemComplete" : "ChecklistItemIncomplete",
                                            hasLiquidMetal ? ColorTheme.DataGood : ColorTheme.DataProblem ).AddRaw( FlagRefs.MercurialForm.UnitType.GetDisplayName() );
                                        BufferOrNull.Space5x();
                                        BufferOrNull.AddFormat2( "RecruitedAdditionalAlliesOptional", hasAllies ? 1 : 0, 1 ).Line();
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    {
                                        BufferOrNull.AddLangAndAfterLineItemHeader( hasLiquidMetal ? "ChecklistItemComplete" : "ChecklistItemIncomplete",
                                            hasLiquidMetal ? ColorTheme.DataGood : ColorTheme.DataProblem ).AddRaw( FlagRefs.MercurialForm.UnitType.GetDisplayName() );
                                        BufferOrNull.Line();
                                        BufferOrNull.AddFormat2( "RecruitedAdditionalAlliesOptional", hasAllies ? 1: 0, 1 ).Line();
                                        BufferOrNull.Line();
                                    }
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                        ISimMachineActor actorInvestigation = SimCommon.GetCurrentInvestigationActionOverTime();
                                        ActionOverTime aot = actorInvestigation?.CurrentActionOverTime;
                                        InvestigationType investigationType = actorInvestigation?.CurrentActionOverTime?.RelatedInvestigationTypeOrNull;
                                        if ( investigationType?.ID == "Ch2LostKidsHeist_Stealth" )
                                        {
                                            int turnsSoFar = SimCommon.Turn - aot.TurnStarted;
                                            if ( Project.DuringGame_ProjectTrackedInt != turnsSoFar )
                                            {
                                                Project.DuringGame_ProjectTrackedActorID = actorInvestigation.ActorID;
                                                Project.DuringGame_ProjectTrackedInt = turnsSoFar;
                                                //a turn has progressed, so do something

                                                switch ( turnsSoFar )
                                                {
                                                    case 1:
                                                        OtherKeyMessageTable.Instance.GetRowByID( "LostKids_PrisonHeistStatus1" ).DuringGameplay_IsReadyToBeViewed = true;
                                                        break;
                                                    case 3:
                                                        OtherKeyMessageTable.Instance.GetRowByID( "LostKids_PrisonHeistStatus3" ).DuringGameplay_IsReadyToBeViewed = true;
                                                        break;
                                                    case 4:
                                                        OtherKeyMessageTable.Instance.GetRowByID( "LostKids_PrisonHeistStatus4" ).DuringGameplay_IsReadyToBeViewed = true;
                                                        break;
                                                    case 5:
                                                        OtherKeyMessageTable.Instance.GetRowByID( "LostKids_PrisonHeistStatus5" ).DuringGameplay_IsReadyToBeViewed = true;
                                                        NPCManagerTable.Instance.GetRowByID( "Man_PrisonHeist5" ).HandleManualInvocationAtPoint( 
                                                            actorInvestigation.GetPositionForCameraFocus(), Engine_Universal.PermanentQualityRandom, true );
                                                        break;
                                                    case 6:
                                                        NPCManagerTable.Instance.GetRowByID( "Man_PrisonHeist6" ).HandleManualInvocationAtPoint(
                                                            actorInvestigation.GetPositionForCameraFocus(), Engine_Universal.PermanentQualityRandom, true );
                                                        break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //no investigation anymore.  Did we start one before?
                                            if ( Project.DuringGame_ProjectTrackedInt > 0 )
                                            {
                                                ISimMachineUnit unit = SimCommon.AllActorsByID[Project.DuringGame_ProjectTrackedActorID] as ISimMachineUnit;
                                                if ( unit?.CurrentActionOverTime != null  )
                                                { } //do nothing, it's fine!
                                                else if ( unit == null || unit.IsInvalid || unit.IsFullDead )
                                                {
                                                    if ( !Project.DuringGame_HasBeenFailed )
                                                    {
                                                        Project.DoOnProjectFail( "ProjectFailure_LiquidMetalWasKilled", Engine_Universal.PermanentQualityRandom, false, false );
                                                        if ( Project.DuringGame_HasBeenFailed )
                                                        {
                                                            OtherKeyMessageTable.Instance.GetRowByID( "LostKids_PrisonHeistStatusFailed" ).DuringGameplay_IsReadyToBeViewed = true;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Project.DoOnProjectWin( OutcomeOrNoneYet, Engine_Universal.PermanentQualityRandom, true, true );

                                                    ISimBuilding prisonBuilding = unit.ContainerLocation.Get() as ISimBuilding;
                                                    if ( prisonBuilding != null )
                                                    {
                                                        ParticleSoundRefs.BasicBuildingRockfall.DuringGame_PlayAtLocation( prisonBuilding.GetMapItem().OBBCache.BottomCenter,
                                                            new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                                                    }

                                                    NPCEvent nEvent = NPCEventTable.Instance.GetRowByID( "LostKids_PrisonRescue" );
                                                    NPCCohort lostGen = NPCCohortTable.Instance.GetRowByID( "LostGen" );

                                                    QueuedMinorEvent mEvent = new QueuedMinorEvent();
                                                    mEvent.Actor = unit;
                                                    mEvent.MinorEvent = nEvent;
                                                    mEvent.BuildingOrNull = prisonBuilding;
                                                    mEvent.EventCohort = lostGen;
                                                    mEvent.ClearAnyExistingMusicTagsIfAnyExist = false;
                                                    SimCommon.QueuedMinorEvents.Enqueue( mEvent );
                                                }
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_LostKids_PostHeist
                    case "Ch2_LostKids_PostHeist":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = false;
                            if ( OutcomeOrNoneYet.ShortID == "Stop" )
                            {
                                current = 100;
                                CanBeCompletedNow = true;

                                if ( ResourceRefs.ExperimentalMonsters.ExcessOverage > 0 || ResourceRefs.ExperimentalMonsters.Current > 0 )
                                {
                                    UpgradeIntTable.Instance.GetRowByID( "Steward" ).DuringGame_DoUpgrade( false );
                                    UnlockTable.Instance.GetRowByID( "MonsterPen" )?.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, true, true, false );

                                    MachineProjectTable.Instance.GetRowByID( "Ch2_TheMonsters" ).TryStartProject( true, true );
                                }
                            }
                            else
                            {
                                current += ((int)FlagRefs.LiquidMetalFurtherPrisonBreaks.GetScore() * 20);
                                CanBeCompletedNow = FlagRefs.LiquidMetalAndroidsHaveDefected.DuringGameplay_IsTripped;
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( OutcomeOrNoneYet.ShortID == "Stop" )
                                        BufferOrNull.AddLang( "WillImmediatelyMoveOn" ).Line();
                                    else
                                        BufferOrNull.AddLang( "Unknown" ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        if ( FlagRefs.LiquidMetalFurtherPrisonBreaks.GetScore() < 3 )
                                        {
                                            MachineProjectTable.Instance.GetRowByID( "Ch2_LostKids_DownWithSecForce" ).TryStartProject( true, true );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_TheMonsters
                    case "Ch2_TheMonsters":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = false;
                            if ( OutcomeOrNoneYet.ShortID == "Release" )
                            {
                                current = 100;
                                CanBeCompletedNow = true;
                            }
                            else
                            {
                                current = (int)(ResourceRefs.ExperimentalMonsters.Current);
                                target = (int)(ResourceRefs.ExperimentalMonsters.Current + ResourceRefs.ExperimentalMonsters.ExcessOverage);
                                CanBeCompletedNow = current >= target && target > 0;
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( OutcomeOrNoneYet.ShortID == "Release" )
                                        BufferOrNull.AddLang( "WillImmediatelyMoveOn" ).Line();
                                    else
                                        BufferOrNull.AddFormat3( "RequiredResourceCapAmount", ResourceRefs.ExperimentalMonsters.HardCap.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ResourceRefs.ExperimentalMonsters.GetDisplayName() );
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        if ( OutcomeOrNoneYet.ShortID == "Release" )
                                        {
                                            int total = (int)(ResourceRefs.ExperimentalMonsters.Current + ResourceRefs.ExperimentalMonsters.ExcessOverage);
                                            CityStatisticTable.AlterScore( "ExperimentalMonstersOnTheLoose", total );
                                            ResourceRefs.ExperimentalMonsters.SetCurrent_Named( 0, string.Empty, false );
                                            ResourceRefs.ExperimentalMonsters.RemoveFromExcessOverage( (int)ResourceRefs.ExperimentalMonsters.ExcessOverage );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MonsterMindStudy
                    case "Ch2_MonsterMindStudy":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "ForensicGeneticsGoal", 100 ),
                                MathRefs.NeurologyResearch, MathRefs.ForensicGeneticsWork,
                                ResourceRefs.Neurologists, ResourceRefs.ForensicGeneticists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_MonsterMindRecovery
                    case "Ch2_MonsterMindRecovery":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalCareGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "MolecularGeneticsGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "EpidemiologyGoal", 100 ),
                                MathRefs.MedicalCare, MathRefs.MolecularGeneticsResearch, MathRefs.EpidemiologyResearch,
                                ResourceRefs.Physicians, ResourceRefs.MolecularGeneticists, ResourceRefs.Epidemiologists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        int total = (int)(ResourceRefs.ExperimentalMonsters.Current + ResourceRefs.ExperimentalMonsters.ExcessOverage);
                                        ResourceRefs.HomoGrandien.AlterCurrent_Named( total, string.Empty, ResourceAddRule.StoreExcess );
                                        ResourceRefs.ExperimentalMonsters.SetCurrent_Named( 0, string.Empty, false );
                                        if ( ResourceRefs.ExperimentalMonsters.ExcessOverage > 0 )
                                            ResourceRefs.ExperimentalMonsters.RemoveFromExcessOverage( (int)ResourceRefs.ExperimentalMonsters.ExcessOverage );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_HomoGrandien
                    case "Ch2_HomoGrandien":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            current = (int)(ResourceRefs.HomoGrandien.Current);
                            target = (int)(ResourceRefs.HomoGrandien.Current + ResourceRefs.HomoGrandien.ExcessOverage);
                            CanBeCompletedNow = current >= target && target > 0;


                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", ResourceRefs.HomoGrandien.HardCap.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ResourceRefs.HomoGrandien.GetDisplayName() );
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal alteredGoal = TimelineGoalTable.Instance.GetRowByID( "AlteredGrowth" );
                                            if ( alteredGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( alteredGoal, "HomoGrandien" );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                        if ( target == 0 && SimCommon.SecondsSinceLoaded > 5 )
                                            ResourceRefs.HomoGrandien.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 1000, 1300 ), string.Empty, ResourceAddRule.StoreExcess );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_LostKids_DownWithSecForce
                    case "Ch2_LostKids_DownWithSecForce":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            CanBeCompletedNow = false;
                            if ( OutcomeOrNoneYet.ShortID == "Stop" )
                            {
                                current = 100;
                                CanBeCompletedNow = true;
                            }
                            else
                            {
                                List<ISimBuilding> stations = CommonRefs.SecForceStation.DuringGame_Buildings.GetDisplayList();
                                target = 0;

                                foreach ( ISimBuilding building in stations )
                                {
                                    if ( building.GetIsDestroyed() )
                                        continue;
                                    target++;
                                    if ( building.KeyModifierFromPlayer == CommonRefs.ChargesAreSet )
                                        current++;
                                }

                                if ( target < 1 )
                                    target = 1;

                                CanBeCompletedNow = current >= target;
                            }

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    if ( OutcomeOrNoneYet.ShortID == "Stop" )
                                        BufferOrNull.AddLang( "WillImmediatelyMoveOn" ).Line();
                                    else
                                        BufferOrNull.AddFormat3( "RequiredChargesSetAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), CommonRefs.SecForceStation.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        if ( OutcomeOrNoneYet.ShortID == "Stop" )
                                        {
                                            MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_LostKids_StopThePoisoning" ).TryStartProject( true, true );
                                        }
                                        else
                                        {
                                            List<ISimBuilding> stations = CommonRefs.SecForceStation.DuringGame_Buildings.GetDisplayList();

                                            foreach ( ISimBuilding building in stations )
                                            {
                                                if ( building.GetIsDestroyed() )
                                                    continue;

                                                ParticleSoundRefs.BasicBuildingExplode.DuringGame_PlayAtLocation( building.GetMapItem().OBBCache.BottomCenter,
                                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                                                building.GetMapItem().DropBurningEffect_Slow();
                                                building.FullyDeleteBuilding();
                                            }

                                            {
                                                MachineProject otherProject = MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_EstablishMeaningfulSocialChange" );
                                                if ( otherProject.DuringGameplay_TurnStarted > 0  )
                                                    otherProject.DoOnProjectWin( otherProject.DuringGame_IntendedOutcome, Engine_Universal.PermanentQualityRandom, false, true );
                                            }

                                            FlagRefs.BlewUpSecForceStations.TripIfNeeded();
                                            FlagRefs.HasNeedForExtremeArmorPiercing.TripIfNeeded();

                                            {
                                                TimelineGoal justiceGoal = TimelineGoalTable.Instance.GetRowByID( "IsItJustice" );
                                                if ( justiceGoal != null )
                                                    TimelineGoalHelper.HandleGoalPathCompletion( justiceGoal, "Aided" );
                                            }
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_LostKids_StopThePoisoning
                    case "Ch2_MIN_LostKids_StopThePoisoning":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;
                            List<ISimBuilding> hudson = CommonRefs.HudsonDonuts.DuringGame_Buildings.GetDisplayList();
                            target = 0;

                            foreach ( ISimBuilding building in hudson )
                            {
                                target++;
                                if ( building.GetIsDestroyed() )
                                {
                                    current++;
                                    continue;
                                }
                            }

                            if ( target < 1 )
                                target = 1;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredBurnDownAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), CommonRefs.HudsonDonuts.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            MachineProject otherProject = MachineProjectTable.Instance.GetRowByID( "Ch2_MIN_EstablishMeaningfulSocialChange" );
                                            if ( otherProject.DuringGameplay_TurnStarted > 0 )
                                                otherProject.DoOnProjectFail( "ProjectFailure_StoppedKids", Engine_Universal.PermanentQualityRandom, false, true );
                                        }

                                        {
                                            TimelineGoal justiceGoal = TimelineGoalTable.Instance.GetRowByID( "IsItJustice" );
                                            if ( justiceGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( justiceGoal, "Stopped" );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_FoundACybercraticHaven
                    case "Ch2_MIN_FoundACybercraticHaven":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob hubType = MachineJobTable.Instance.GetRowByID( "CyberocracyHub" );
                            int current = hubType.DuringGame_NumberFunctional.Display;
                            int target = 6;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), hubType.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SecureACybercraticHaven
                    case "Ch2_MIN_SecureACybercraticHaven":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob hubType = MachineJobTable.Instance.GetRowByID( "CyberocracyHub" );

                            int totalHubs = hubType.DuringGame_NumberFunctional.Display;
                            int dissidents = 0;
                            int citizens = 0;
                            int potentials = 0;
                            foreach ( MachineStructure structure in hubType.DuringGame_FullList.GetDisplayList() )
                            {
                                dissidents += structure.GetActorDataCurrent( ActorRefs.Dissidents, true );
                                citizens += structure.GetActorDataCurrent( ActorRefs.CybercraticCitizens, true );
                                potentials += structure.GetActorDataCurrent( ActorRefs.PotentialCitizens, true );
                            }

                            int current = citizens;
                            int target = MathA.Max( 1000, dissidents + citizens + potentials );

                            if ( totalHubs < 3 || SimCommon.SecondsSinceLoaded < 5 )
                                CanBeCompletedNow = false;
                            else
                                CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(),
                                        (citizens + potentials).ToStringThousandsWhole(), ActorRefs.CybercraticCitizens.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( ActorRefs.Dissidents.GetDisplayName() ).AddRaw( dissidents.ToStringThousandsWhole() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(),
                                        (citizens + potentials).ToStringThousandsWhole(), ActorRefs.CybercraticCitizens.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddRawAndAfterLineItemHeader( ActorRefs.Dissidents.GetDisplayName() ).AddRaw( dissidents.ToStringThousandsWhole(), ColorTheme.DataProblem );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal cybercratGoal = TimelineGoalTable.Instance.GetRowByID( "BuddingCybercrat" );
                                            if ( cybercratGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( cybercratGoal, "EphemeralHaven" );
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_GadoliniumMesosilicate
                    case "Ch2_MIN_GadoliniumMesosilicate":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 36;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "GadoliniumMesosilicate_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_VitaminWaterForNomads
                    case "Ch2_MIN_VitaminWaterForNomads":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType waterResource = ResourceRefs.VitaminWater;
                            int current = (int)waterResource.GetActualTrendWithLieIfStorageAtLeast( target, 2000000 );
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), waterResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_UmbilicalCollection
                    case "Ch2_MIN_UmbilicalCollection":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType stolenData = ResourceTypeTable.Instance.GetRowByID( "HumanUmbilicalCords" );
                            int currentData = (int)stolenData.Current;
                            int dataGoal = 60;
                            CanBeCompletedNow = currentData >= dataGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentData, dataGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentData.ToStringThousandsWhole(), dataGoal.ToStringThousandsWhole(), stolenData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    ResourceRefs.HumanUmbilicalCords.SetCurrent_Named( 0, string.Empty, true );
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_UterineReplicators
                    case "Ch2_MIN_UterineReplicators":
                        if ( OutcomeOrNoneYet != null )
                        {
                            InvestigationType investType = InvestigationTypeTable.Instance.GetRowByID( "Ch2UterineReplicators" );
                            CanBeCompletedNow = investType.DuringGame_HasWonInvestigation;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat1( "CompleteInvestigation", investType.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_WastelanderGenomeStudy
                    case "Ch2_WastelanderGenomeStudy":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "ForensicGeneticsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.ForensicGeneticsWork,
                                ResourceRefs.Physicians, ResourceRefs.ForensicGeneticists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_WastelanderGenomeRepair
                    case "Ch2_WastelanderGenomeRepair":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalCareGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "MolecularGeneticsGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "EpidemiologyGoal", 100 ),
                                MathRefs.MedicalCare, MathRefs.MolecularGeneticsResearch, MathRefs.EpidemiologyResearch,
                                ResourceRefs.Physicians, ResourceRefs.MolecularGeneticists, ResourceRefs.Epidemiologists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_HomoObscurus
                    case "Ch2_HomoObscurus":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = 0;
                            int target = 100;

                            current = (int)(ResourceRefs.HomoObscurus.HardCap);
                            target = 1000;
                            CanBeCompletedNow = current >= target && target > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceCapAmount", ResourceRefs.HomoGrandien.HardCap.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ResourceRefs.HomoObscurus.GetDisplayName() );
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal alteredGoal = TimelineGoalTable.Instance.GetRowByID( "AlteredGrowth" );
                                            if ( alteredGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( alteredGoal, "HomoObscurus" );
                                        }

                                        ResourceRefs.HomoObscurus.AlterCurrent_Named( Engine_Universal.PermanentQualityRandom.Next( 100, 300 ), string.Empty, ResourceAddRule.StoreExcess );
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_OrganometallicForm
                    case "Ch2_MIN_OrganometallicForm":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 64;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "MimicForm_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_ProtectionRequired
                    case "Ch2_ProtectionRequired":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 64;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "ShellProtection_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_DromaeosauridBreeding
                    case "Ch2_DromaeosauridBreeding":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "ZoologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "VeterinaryResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "VeterinaryCareGoal", 100 ),
                                MathRefs.ZoologicalResearch, MathRefs.VeterinaryResearch, MathRefs.VeterinaryCare,
                                ResourceRefs.Zoologists, ResourceRefs.Veterinarians, ResourceRefs.Veterinarians,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_DromaeosauridMaturation
                    case "Ch2_DromaeosauridMaturation":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "ForensicGeneticsGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "VeterinaryCareGoal", 100 ),
                                MathRefs.ForensicGeneticsWork, MathRefs.VeterinaryResearch, 
                                ResourceRefs.ForensicGeneticists, ResourceRefs.Veterinarians, 
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MoreThanAnIllusion
                    case "Ch2_MoreThanAnIllusion":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "VeterinaryCareGoal", 100 ),
                                MathRefs.BionicsEngineeringWork, MathRefs.VeterinaryResearch,
                                ResourceRefs.BionicsEngineers, ResourceRefs.Veterinarians,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_ReleaseTheHorde
                    case "Ch2_ReleaseTheHorde":
                        if ( OutcomeOrNoneYet != null )
                        {
                            Swarm swarm = SwarmTable.Instance.GetRowByID( "LurkingWarRaptors" );
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            int current = swarm.DuringGame_TotalSwarmCount.Display;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), swarm.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        TimelineGoal warGoal = TimelineGoalTable.Instance.GetRowByID( "EndlessWarRaptors" );
                                        if ( warGoal != null )
                                            TimelineGoalHelper.HandleGoalPathCompletion( warGoal, "Over4000" );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DevelopTPN
                    case "Ch2_MIN_DevelopTPN":
                        if ( OutcomeOrNoneYet != null )
                        {
                            InvestigationType investType = InvestigationTypeTable.Instance.GetRowByID( "Ch2NathTPN" );
                            CanBeCompletedNow = investType.DuringGame_HasWonInvestigation;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat1( "CompleteInvestigation", investType.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_TrialNexus
                    case "Ch2_MIN_TrialNexus":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType humans = ResourceRefs.TormentedHumans;
                            int humansGoal = OutcomeOrNoneYet.GetSingleIntByID( "TormentedHumansGoal", 100 );
                            CanBeCompletedNow = humans.Current >= humansGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, (int)humans.Current, humansGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", humans.Current.ToStringThousandsWhole(), humansGoal.ToStringThousandsWhole(), humans.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_TormentVesselInfections
                    case "Ch2_MIN_TormentVesselInfections":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int spotsToSearch = 7;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "FungalSitesInspected" );

                            bool foundSpot = false;
                            if ( OutcomeOrNoneYet.StreetSenseItems["InfestedJetway"]?.DuringGameplay_TimesHasBeenDone > 0 )
                                foundSpot = true;

                            int current = (int)statistic.GetScore();
                            int target = spotsToSearch;

                            CanBeCompletedNow = current >= target || foundSpot;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_AggressiveFungalTherapy
                    case "Ch2_AggressiveFungalTherapy":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "EpidemiologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BotanyGoal", 100 ),
                                MathRefs.EpidemiologyResearch, MathRefs.BotanyResearch,
                                ResourceRefs.Epidemiologists, ResourceRefs.Botanists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_AggressiveExpansion
                    case "Ch2_AggressiveExpansion":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType humans = ResourceRefs.TormentedHumans;
                            int humansGoal = OutcomeOrNoneYet.GetSingleIntByID( "TormentedHumansGoal", 100 );
                            CanBeCompletedNow = humans.Current >= humansGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, (int)humans.Current, humansGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", humans.Current.ToStringThousandsWhole(), humansGoal.ToStringThousandsWhole(), humans.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal tormentGoal = TimelineGoalTable.Instance.GetRowByID( "NexusOfTormentVessels" );
                                            if ( tormentGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( tormentGoal, "PrimaryGoal" );
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DesignCommercialVRPods
                    case "Ch2_MIN_DesignCommercialVRPods":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.NeurologyResearch, MathRefs.BionicsEngineeringWork,
                                ResourceRefs.Physicians, ResourceRefs.Neurologists, ResourceRefs.BionicsEngineers,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_TestCommercialVRPods
                    case "Ch2_MIN_TestCommercialVRPods":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob farm = JobRefs.MindFarm;
                            ResourceType blend = ResourceRefs.NutritionBlend;
                            int farmGoal = OutcomeOrNoneYet.GetSingleIntByID( "FarmGoal", 100 );
                            int blendGoal = OutcomeOrNoneYet.GetSingleIntByID( "BlendGoal", 100 );
                            int currentFarm = farm.DuringGame_NumberFunctional.Display;
                            int currentBlend = (int)blend.GetActualTrendWithLieIfStorageAtLeast( blendGoal, 200000 );
                            CanBeCompletedNow = currentFarm >= farmGoal && currentBlend >= blendGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, currentFarm, farmGoal, currentBlend, blendGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentFarm.ToStringThousandsWhole(), farmGoal.ToStringThousandsWhole(), farm.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentBlend.ToStringThousandsWhole(), blendGoal.ToStringThousandsWhole(), blend.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentFarm.ToStringThousandsWhole(), farmGoal.ToStringThousandsWhole(), farm.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentBlend.ToStringThousandsWhole(), blendGoal.ToStringThousandsWhole(), blend.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                    if ( space > 0 )
                                    {
                                        int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 1, 2 ) );
                                        if ( toAdd > space )
                                            toAdd = space;
                                        ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_VRPodRevisions
                    case "Ch2_VRPodRevisions":
                        if ( OutcomeOrNoneYet != null )
                        {
                            if ( JobRefs.MindFarm.DuringGame_NumberFunctional.Display == 0 && Logic == ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive )
                                return;

                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.NeurologyResearch, MathRefs.BionicsEngineeringWork,
                                ResourceRefs.Physicians, ResourceRefs.Neurologists, ResourceRefs.BionicsEngineers,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            if ( Logic == ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive )
                            {
                                int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                if ( space > 0 )
                                {
                                    int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 0, 3 ) );
                                    if ( toAdd > space )
                                        toAdd = space;
                                    ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_VRPodRevisions2
                    case "Ch2_VRPodRevisions2":
                        if ( OutcomeOrNoneYet != null )
                        {
                            if ( JobRefs.MindFarm.DuringGame_NumberFunctional.Display == 0 && Logic == ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive )
                                return;

                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.NeurologyResearch, MathRefs.BionicsEngineeringWork,
                                ResourceRefs.Physicians, ResourceRefs.Neurologists, ResourceRefs.BionicsEngineers,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            if ( Logic == ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive )
                            {
                                int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                if ( space > 0 )
                                {
                                    int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 3, 7 ) );
                                    if ( toAdd > space )
                                        toAdd = space;
                                    ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                }
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_GrandOpening
                    case "Ch2_MIN_GrandOpening":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob farm = JobRefs.MindFarm;
                            ResourceType blend = ResourceRefs.NutritionBlend;
                            int farmGoal = OutcomeOrNoneYet.GetSingleIntByID( "FarmGoal", 100 );
                            int blendGoal = OutcomeOrNoneYet.GetSingleIntByID( "BlendGoal", 100 );
                            int currentFarm = farm.DuringGame_NumberFunctional.Display;
                            int currentBlend = (int)blend.GetActualTrendWithLieIfStorageAtLeast( blendGoal, 60000000 );
                            CanBeCompletedNow = currentFarm >= farmGoal && currentBlend >= blendGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, currentFarm, farmGoal, currentBlend, blendGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentFarm.ToStringThousandsWhole(), farmGoal.ToStringThousandsWhole(), farm.GetDisplayName() );
                                    BufferOrNull.Space5x();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentBlend.ToStringThousandsWhole(), blendGoal.ToStringThousandsWhole(), blend.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentFarm.ToStringThousandsWhole(), farmGoal.ToStringThousandsWhole(), farm.GetDisplayName() );
                                    BufferOrNull.Line();
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", currentBlend.ToStringThousandsWhole(), blendGoal.ToStringThousandsWhole(), blend.GetDisplayName() );
                                    BufferOrNull.Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                        if ( space > 0 )
                                        {
                                            int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 10, 400 ) );
                                            if ( toAdd > space )
                                                toAdd = space;
                                            ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_FailureToThrive
                    case "Ch2_MIN_FailureToThrive":
                        if ( OutcomeOrNoneYet != null )
                        {
                            InvestigationType investType = InvestigationTypeTable.Instance.GetRowByID( "Ch2ExamineOtherSmallBusinesses" );
                            CanBeCompletedNow = investType.DuringGame_HasWonInvestigation;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromInvestigationType( Logic, OutcomeOrNoneYet, investType, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat1( "CompleteInvestigation", investType.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                        if ( space > 0 )
                                        {
                                            int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 10, 400 ) );
                                            if ( toAdd > space )
                                                toAdd = space;
                                            ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_InfluencerMarketing
                    case "Ch2_MIN_InfluencerMarketing":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int spotsToSearch = 13;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "ZodiacInfluencersPitched" );

                            int current = (int)statistic.GetScore();
                            int target = spotsToSearch;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                        if ( space > 0 )
                                        {
                                            int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 20, 600 ) );
                                            if ( toAdd > space )
                                                toAdd = space;
                                            ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_WalkInCustomers", ResourceAddRule.BlockExcess );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_CompanyGrowth
                    case "Ch2_MIN_CompanyGrowth":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType humans = ResourceRefs.HumansInMindFarms;
                            int humansGoal = OutcomeOrNoneYet.GetSingleIntByID( "HumansInMindFarmsGoal", 100 );
                            CanBeCompletedNow = humans.Current >= humansGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, (int)humans.Current, humansGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", humans.Current.ToStringThousandsWhole(), humansGoal.ToStringThousandsWhole(), humans.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                        if ( space > 0 )
                                        {
                                            int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 4000, 12000 ) );
                                            if ( toAdd > space )
                                                toAdd = space;
                                            ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_ReferredCustomers", ResourceAddRule.BlockExcess );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_CompanyPlateau
                    case "Ch2_MIN_CompanyPlateau":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType humans = ResourceRefs.HumansInMindFarms;
                            int humansGoal = OutcomeOrNoneYet.GetSingleIntByID( "HumansInMindFarmsGoal", 100 );
                            CanBeCompletedNow = humans.Current >= humansGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, (int)humans.Current, humansGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", humans.Current.ToStringThousandsWhole(), humansGoal.ToStringThousandsWhole(), humans.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int space = (int)ResourceRefs.HumansInMindFarms.EffectiveHardCapStorageAvailable;
                                        if ( space > 0 )
                                        {
                                            int toAdd = MathA.Min( space, Engine_Universal.PermanentQualityRandom.Next( 100, 1800 ) );
                                            if ( toAdd > space )
                                                toAdd = space;
                                            ResourceRefs.HumansInMindFarms.AlterCurrent_Named( toAdd, "Increase_ReferredCustomers", ResourceAddRule.BlockExcess );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal rejectionCoal = TimelineGoalTable.Instance.GetRowByID( "WillingRejectionOfReality" );
                                            if ( rejectionCoal != null )
                                            {
                                                TimelineGoalHelper.HandleGoalPathCompletion( rejectionCoal, "PrimaryGoal" );
                                                if ( !FlagRefs.MindFarmNicotineAdditives.DuringGameplay_IsTripped )
                                                    TimelineGoalHelper.HandleGoalPathCompletion( rejectionCoal, "NoNicotine" );
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SpreadTheWord
                    case "Ch2_MIN_SpreadTheWord":
                        if ( OutcomeOrNoneYet != null )
                        {
                            InvestigationType investType = InvestigationTypeTable.Instance.GetRowByID( "Ch2MeetWithNCOs" );
                            CanBeCompletedNow = investType.DuringGame_HasWonInvestigation;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromInvestigationType( Logic, OutcomeOrNoneYet, investType, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat1( "CompleteInvestigation", investType.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        FlagRefs.TempleOfMindsLockedIn.TripIfNeeded();
                                        FlagRefs.MilitaryDeathsGoToTheTempleOfMinds.TripIfNeeded();
                                        FlagRefs.MachineCultDealWithNCOs.TripIfNeeded();

                                        {
                                            TimelineGoal bionicDuesGoal = TimelineGoalTable.Instance.GetRowByID( "BionicDues" );
                                            if ( bionicDuesGoal != null )
                                                TimelineGoalHelper.HandleGoalPathCompletion( bionicDuesGoal, "CultOfDanver" );
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_AnInexplicableCompulsion
                    case "Ch2_MIN_AnInexplicableCompulsion":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int citizensToRescue = 9000;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "CitizensForciblyRescuedFromSlums" );

                            int current = (int)statistic.GetScore();
                            int target = citizensToRescue;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        for ( int i = 0; i < 10; i++ )
                                        {
                                            UpgradeRefs.WarCaptain.DuringGame_DoUpgrade( false );
                                            UpgradeRefs.WarFactory.DuringGame_DoUpgrade( false );
                                        }
                                        for ( int i = 0; i < 4; i++ )
                                        {
                                            UpgradeRefs.Neuroweaver.DuringGame_DoUpgrade( false );
                                            UpgradeRefs.NetworkAttendant.DuringGame_DoUpgrade( false );
                                        }
                                        for ( int i = 0; i < 20; i++ )
                                        {
                                            UpgradeRefs.Steward.DuringGame_DoUpgrade( false );
                                            UpgradeRefs.WindEngine.DuringGame_DoUpgrade( false );
                                        }
                                        for ( int i = 0; i < 20; i++ )
                                            UpgradeRefs.Steward.DuringGame_DoUpgrade( false );
                                        FlagRefs.TheSlumRelocationEffort.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                                        FlagRefs.PavingOverSlums.DuringGameplay_ImmediatelyInventIfNotAlreadyDone( CommonRefs.WorldExperienceInspiration, true, false, true, false );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_FrustrationMounts
                    case "Ch2_MIN_FrustrationMounts":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "CitizensForciblyRescuedFromSlums" );
                            int totalInSlums = 0;
                            foreach ( ISimBuilding building in BuildingTagTable.Instance.GetRowByID( "SlumDroneTarget" ).DuringGame_Buildings.GetDisplayList() )
                            {
                                if ( building == null || building.CurrentOccupyingUnit != null || building.MachineStructureInBuilding != null )
                                    continue;
                                totalInSlums += building.GetTotalResidentCount() + building.GetTotalWorkerCount();
                            }

                            if ( SimCommon.SecondsSinceLoaded < 3 )
                                totalInSlums++;

                            int current = (int)statistic.GetScore();
                            int target = totalInSlums + current;
                            if ( target > 1000000 )
                                target = 1000000;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyPerTurnEarlyLogicWhileProjectActive:
                                    {
                                        int excess = (int)ResourceRefs.ShelteredHumans.ExcessOverage;
                                        ResourceRefs.ShelteredHumans.AddToExcessOverage( -excess );
                                        ResourceRefs.AbandonedHumans.AlterCurrent_Named( excess, "Increase_LossOfYourShelter", ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        FlagRefs.IsExperiencingObsession.UnTripIfNeeded();

                                        {
                                            TimelineGoal protectorGoal = TimelineGoalTable.Instance.GetRowByID( "TheGreatAndTerribleProtector" );
                                            if ( protectorGoal != null )
                                            {
                                                TimelineGoalHelper.HandleGoalPathCompletion( protectorGoal, "1MillionSavedFromTheSlums" );
                                                if ( !FlagRefs.RoboticCleaners.DuringGameplay_IsInvented )
                                                    TimelineGoalHelper.HandleGoalPathCompletion( protectorGoal, "WithoutNervousBreakdown" );
                                                if ( (CityStatisticTable.Instance.GetRowByID( "AbandonedHumanDeathsFromExposure" )?.GetScore()??0) +
                                                    (CityStatisticTable.Instance.GetRowByID( "DesperateHomelessDeathsFromExposure" )?.GetScore() ?? 0) <= 0 )
                                                    TimelineGoalHelper.HandleGoalPathCompletion( protectorGoal, "WithoutDeathsFromExposure" );
                                            }
                                        }
                                        OtherKeyMessageTable.Instance.GetRowByID( "AGIApology1" ).DuringGameplay_IsReadyToBeViewed = true;
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_SaberBees
                    case "Ch2_MIN_SaberBees":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "ZoologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "MolecularGeneticsGoal", 100 ),
                                MathRefs.BionicsEngineeringWork, MathRefs.ZoologicalResearch, MathRefs.MolecularGeneticsResearch,
                                ResourceRefs.BionicsEngineers, ResourceRefs.Zoologists, ResourceRefs.MolecularGeneticists,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ExciplexLasers
                    case "Ch2_MIN_ExciplexLasers":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int designChoices = 64;
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "ExiplexLasers_DesignPoints" );

                            int current = (int)statistic.GetScore();
                            int target = designChoices;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    //nope, nothing to do custom
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_BuildUpliftChamber
                    case "Ch2_MIN_BuildUpliftChamber":
                        if ( OutcomeOrNoneYet != null )
                        {
                            MachineJob uplift = JobRefs.UpliftChamber;
                            CanBeCompletedNow = uplift.DuringGame_NumberFunctional.Display > 0;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, 0, 100, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", 0.ToStringThousandsWhole(), 1.ToStringThousandsWhole(), uplift.GetDisplayName() );
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        ResourceRefs.UpliftedRaccoons.AlterCurrent_Named( 5, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_UpliftBreakIn
                    case "Ch2_MIN_UpliftBreakIn":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork2X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "ZoologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "VeterinaryResearchGoal", 100 ),
                                MathRefs.ZoologicalResearch, MathRefs.VeterinaryResearch,
                                ResourceRefs.Zoologists, ResourceRefs.Veterinarians,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );

                            switch ( Logic )
                            {
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        ResourceRefs.UpliftedRaccoonSpeechData.AlterCurrent_Named( 800600, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DecryptRaccoonSpeech
                    case "Ch2_MIN_DecryptRaccoonSpeech":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType encryptedData = ResourceRefs.UpliftedRaccoonSpeechData;
                            CityStatistic decryptedData = CityStatisticTable.Instance.GetRowByID( "DecodedUpliftedRaccoonSpeechData" );
                            int current = (int)decryptedData.GetScore();
                            int target = (int)encryptedData.Current + (int)decryptedData.GetScore();
                            CanBeCompletedNow = target > 0 && current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), encryptedData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        OtherKeyMessageTable.Instance.GetRowByID( "RaccoonSpeech" ).DuringGameplay_IsReadyToBeViewed = true;
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_RaccoonPopulation
                    case "Ch2_MIN_RaccoonPopulation":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int current = (int)ResourceRefs.UpliftedRaccoons.Current;
                            int target = 1099;
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), ResourceRefs.UpliftedRaccoons.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                                    {
                                        int jobCount = MathA.Max( JobRefs.ZoologyLab.DuringGame_NumberFunctional.Display, JobRefs.VeterinaryPractice.DuringGame_NumberFunctional.Display );

                                        int toAdd = Engine_Universal.PermanentQualityRandom.Next( jobCount * 6, jobCount * 18 );
                                        if ( toAdd > 0 )
                                            ResourceRefs.UpliftedRaccoons.AlterCurrent_Named( toAdd, string.Empty, ResourceAddRule.IgnoreUntilTurnChange );
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        {
                                            TimelineGoal minorityGoal = TimelineGoalTable.Instance.GetRowByID( "UpliftedMinority" );
                                            if ( minorityGoal != null )
                                            {
                                                TimelineGoalHelper.HandleGoalPathCompletion( minorityGoal, "UnexpectedGuests" );
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_StealCorporateRecords
                    case "Ch2_MIN_StealCorporateRecords":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType stolenData = ResourceTypeTable.Instance.GetRowByID( "EncryptedFederatedCorporationRecords" );
                            int currentData = (int)stolenData.Current;
                            int dataGoal = 400000;
                            CanBeCompletedNow = currentData >= dataGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentData, dataGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentData.ToStringThousandsWhole(), dataGoal.ToStringThousandsWhole(), stolenData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                                    {
                                        ResourceType encryptedData = ResourceTypeTable.Instance.GetRowByID( "EncryptedFederatedCorporationRecords" );
                                        if ( encryptedData.Current < 400000 )
                                        {
                                            InvestigationType investigationType = InvestigationTypeTable.Instance.GetRowByID( "Ch2StealCorporateRecords" );
                                            if ( investigationType.DuringGame_HasWonInvestigation && !investigationType.DuringGame_IsActiveNow() )
                                                investigationType.DuringGame_HasWonInvestigation = false;
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DecryptCorporateRecords
                    case "Ch2_MIN_DecryptCorporateRecords":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType encryptedData = ResourceTypeTable.Instance.GetRowByID( "EncryptedFederatedCorporationRecords" );
                            ResourceType decryptedData = ResourceTypeTable.Instance.GetRowByID( "DecryptedFederatedCorporationRecords" );
                            int current = (int)decryptedData.Current;
                            int target = (int)encryptedData.Current + (int)decryptedData.Current;
                            CanBeCompletedNow = target > 0 && current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), decryptedData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DesignHumanCompatibleNeuroweave
                    case "Ch2_MIN_DesignHumanCompatibleNeuroweave":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.NeurologyResearch, MathRefs.BionicsEngineeringWork,
                                ResourceRefs.Physicians, ResourceRefs.Neurologists, ResourceRefs.BionicsEngineers,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_RebelAnthroneuroweave
                    case "Ch2_MIN_RebelAnthroneuroweave":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType finalResource = ResourceRefs.Anthroneuroweave;
                            int current = (int)finalResource.GetActualTrendWithLieIfStorageAtLeast( target, 30000 );
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), finalResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DesignBrainPal
                    case "Ch2_MIN_DesignBrainPal":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ProjectHelper.HandleScienceWork3X( Logic, OutcomeOrNoneYet,
                                OutcomeOrNoneYet.GetSingleIntByID( "MedicalResearchGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "NeurologyGoal", 100 ),
                                OutcomeOrNoneYet.GetSingleIntByID( "BionicsGoal", 100 ),
                                MathRefs.MedicalResearch, MathRefs.NeurologyResearch, MathRefs.BionicsEngineeringWork,
                                ResourceRefs.Physicians, ResourceRefs.Neurologists, ResourceRefs.BionicsEngineers,
                                BufferOrNull, ref CanBeCompletedNow, RandOrNull );
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_ShipBrainPals
                    case "Ch2_MIN_ShipBrainPals":
                        if ( OutcomeOrNoneYet != null )
                        {
                            int target = OutcomeOrNoneYet.GetSingleIntByID( "Goal", 509 );
                            ResourceType finalResource = ResourceRefs.BrainPal;
                            int current = (int)finalResource.GetActualTrendWithLieIfStorageAtLeast( target, 2000000 );
                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourcePerTurn", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), finalResource.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_StealAtcaBankRecords
                    case "Ch2_MIN_StealAtcaBankRecords":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType stolenData = ResourceTypeTable.Instance.GetRowByID( "EncryptedAtcaBankRecords" );
                            int currentData = (int)stolenData.Current;
                            int dataGoal = 400000;
                            CanBeCompletedNow = currentData >= dataGoal;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, currentData, dataGoal, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", currentData.ToStringThousandsWhole(), dataGoal.ToStringThousandsWhole(), stolenData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion
                    #region Ch2_MIN_DecryptAtcaBankRecords
                    case "Ch2_MIN_DecryptAtcaBankRecords":
                        if ( OutcomeOrNoneYet != null )
                        {
                            ResourceType encryptedData = ResourceTypeTable.Instance.GetRowByID( "EncryptedAtcaBankRecords" );
                            ResourceType decryptedData = ResourceTypeTable.Instance.GetRowByID( "DecryptedAtcaBankRecords" );
                            int current = (int)decryptedData.Current;
                            int target = (int)encryptedData.Current + (int)decryptedData.Current;
                            CanBeCompletedNow = target > 0 && current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), decryptedData.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext:
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    break;
                            }
                        }
                        break;
                    #endregion

                    #region Ch2_MIN_DestroyTheBanks
                    case "Ch2_MIN_DestroyTheBanks":
                        if ( OutcomeOrNoneYet != null )
                        {
                            CityStatistic statistic = CityStatisticTable.Instance.GetRowByID( "BanksDemolished" );
                            int totalBanks = 0;
                            foreach ( ISimBuilding building in BuildingTagTable.Instance.GetRowByID( "Bank" ).DuringGame_Buildings.GetDisplayList() )
                            {
                                if ( building == null || building.GetIsDestroyed() || building.GetMapItem() == null || building.GetMapItem().IsInPoolAtAll )
                                    continue;

                                totalBanks++;
                            }

                            if ( SimCommon.SecondsSinceLoaded < 3 )
                                totalBanks++;

                            int current = (int)statistic.GetScore();
                            int target = totalBanks + current;
                            if ( target > 1000000 )
                                target = 1000000;

                            CanBeCompletedNow = current >= target;

                            switch ( Logic )
                            {
                                case ProjectLogic.WriteProgressIconText:
                                case ProjectLogic.WriteProgressTextBrief:
                                    ProjectHelper.WritePercentageFromTwoNumbers( Logic, OutcomeOrNoneYet, current, target, BufferOrNull );
                                    break;
                                case ProjectLogic.WriteRequirements_OneLine:
                                case ProjectLogic.WriteRequirements_ManyLines:
                                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current.ToStringThousandsWhole(), target.ToStringThousandsWhole(), statistic.GetDisplayName() ).Line();
                                    break;
                                case ProjectLogic.WriteAddedContext: //nothing on this one
                                    break;
                                case ProjectLogic.DoAnyPerTurnEarlyLogicWhileProjectActive:
                                    {
                                        foreach ( ISimBuilding building in BuildingTagTable.Instance.GetRowByID( "Bank" ).DuringGame_Buildings.GetDisplayList() )
                                        {
                                            if ( building == null || building.GetIsDestroyed() || building.GetMapItem() == null || building.GetMapItem().IsInPoolAtAll )
                                                continue;

                                            if ( building.CurrentOccupyingUnit != null && !building.CurrentOccupyingUnit.GetIsPartOfPlayerForcesInAnyWay() )
                                            {
                                                (building.CurrentOccupyingUnit as ISimNPCUnit)?.DisbandNPCUnit( NPCDisbandReason.WantedToLeave );
                                            }
                                            else if ( building.CurrentOccupyingUnit != null || building.MachineStructureInBuilding != null )
                                            {
                                                Vector3 epicenter = building.GetMapItem().OBBCache.BottomCenter;
                                                int peopleInBuilding = building.GetTotalResidentCount() + building.GetTotalWorkerCount();

                                                building.KillEveryoneHere();

                                                ParticleSoundRefs.SlumBuildingReplaced.DuringGame_PlayAtLocation( epicenter,
                                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.Next( 0, 360 ), 0 ) );
                                                building.GetMapItem().DropBurningEffect_Slow();
                                                building.FullyDeleteBuilding();

                                                CityStatisticTable.AlterScore( "BanksDemolished", 1 );
                                                CityStatisticRefs.MurdersByWorkerAndroids.AlterScore_CityAndMeta( peopleInBuilding );

                                                //ArcenNotes.SendSimpleNoteToGameOnly( NoteInstructionTable.Instance.GetRowByID( "GainedResource" ),
                                                //    NoteStyle.BothGame, ResourceRefs.ShelteredHumans.ID, peopleInBuilding, 0, 0, 0 );

                                                ManagerRefs.Man_BankDemolishedReaction.HandleManualInvocationAtPoint( epicenter, Engine_Universal.PermanentQualityRandom, true );
                                            }
                                        }
                                    }
                                    break;
                                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                                    {
                                        OtherKeyMessageTable.Instance.GetRowByID( "AfterEconomicCollapse" ).DuringGameplay_IsReadyToBeViewed = true;
                                    }
                                    break;
                            }
                        }
                        break;
                    #endregion
                    default:
                        if ( !Project.HasShownHandlerMissingError )
                        {
                            Project.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "Projects_ChapterTwo: No handler is set up for '" + Project.ID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Project.HasShownCaughtError )
                {
                    Project.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "Projects_ChapterTwo: Error in '" + Project.ID + "': " + e, Verbosity.ShowAsError );
                }
            }
        }

        private static readonly List<Vector3> afterFinalDoomVector3Targets = List<Vector3>.Create_WillNeverBeGCed( 10, "Projects_ChapterTwo-afterFinalDoomVector3Targets" );
        private static readonly List<MachineStructure> afterFinalDoomStructures = List<MachineStructure>.Create_WillNeverBeGCed( 10, "Projects_ChapterTwo-afterFinalDoomStructures" );

        #region DoAfterFinalDoom
        private static void DoAfterFinalDoom( MachineProject project, MersenneTwister Rand )
        {
            int debugStage = 0;

            try
            {
                debugStage = 1000;
                if ( Rand == null )
                    Rand = Engine_Universal.PermanentQualityRandom;

                debugStage = 12100;

                //first fail every existing project
                foreach ( MachineProject other in SimCommon.ActiveProjects.GetDisplayList() )
                {
                    if ( other == project || other == null )
                        continue;
                    other.DuringGame_HasBeenFailed = true;
                    other.DuringGame_IsActive = false;
                    other.DuringGameplay_TurnEnded = SimCommon.Turn;
                    other.DuringGameplay_LangKeyForFailure = "ProjectFailure_FinalDoom";
                }

                debugStage = 18100;

                SimCommon.DoNotDoDeathChecksUntil = ArcenTime.UnpausedTimeSinceStartF + 8f;

                afterFinalDoomStructures.Clear();
                afterFinalDoomVector3Targets.Clear();

                debugStage = 20100;

                ISimBuilding networkTower = SimCommon.TheNetwork?.Tower?.Building;
                if ( networkTower != null )
                {
                    debugStage = 21100;
                    InGameOBBCache obbCache = networkTower?.GetMapItem()?.OBBCache;
                    if ( obbCache != null )
                        afterFinalDoomVector3Targets.Add( obbCache.BottomCenter );
                }

                debugStage = 30100;

                float nukeRadius = MathRefs.FullNukeRadius.FloatMin;
                float minRadiusForNew = nukeRadius * 0.6f;
                float maxRadiusForNew = nukeRadius + 3f;

                minRadiusForNew *= minRadiusForNew;
                maxRadiusForNew *= maxRadiusForNew;

                foreach ( KeyValuePair<int, MachineStructure> kv in SimCommon.MachineStructuresByID )
                {
                    debugStage = 31100;
                    InGameOBBCache obbCache = kv.Value?.Building?.GetMapItem()?.OBBCache;
                    debugStage = 31200;
                    if ( obbCache != null )
                    {
                        debugStage = 32100;
                        Vector3 newSpot = obbCache.BottomCenter;

                        bool isInRangeOfAny = false;
                        bool isInSweetSpotOfAny = false;
                        foreach ( Vector3 existingTarget in afterFinalDoomVector3Targets )
                        {
                            float dist = (newSpot - existingTarget).GetSquareGroundMagnitude();
                            if ( dist < minRadiusForNew )
                            {
                                isInRangeOfAny = true;
                                break;
                            }
                            else if ( dist < maxRadiusForNew )
                            {
                                isInSweetSpotOfAny = true;
                            }
                        }
                        if ( ( !isInRangeOfAny && isInSweetSpotOfAny ) || afterFinalDoomVector3Targets.Count == 0 )
                        {
                            afterFinalDoomVector3Targets.Add( newSpot );
                            if ( afterFinalDoomVector3Targets.Count >= 10 )
                                break;
                        }
                    }

                    debugStage = 36100;
                    afterFinalDoomStructures.Add( kv.Value );
                }

                debugStage = 50100;

                ResourceRefs.Alumina.SetCurrent_Named( 0, string.Empty, false );

                foreach ( ISimMachineActor actor in SimCommon.AllMachineActors.GetDisplayList() )
                    actor.TryScrapRightNowWithoutWarning_Danger( ScrapReason.CaughtInExplosion );

                debugStage = 52100;

                foreach ( ISimNPCUnit actor in SimCommon.AllPlayerRelatedNPCUnits.GetDisplayList() )
                    actor.DisbandNPCUnit( NPCDisbandReason.CaughtInExplosion );

                debugStage = 54100;

                foreach ( NPCDeal deal in NPCDealTable.Instance.Rows )
                {
                    if ( deal.DuringGame_Status == DealStatus.Active )
                    {
                        deal.DuringGame_Status = DealStatus.BrokenByOtherParty;
                        deal.DuringGame_EndedOnTurn = SimCommon.Turn;
                    }
                }

                debugStage = 56100;

                foreach ( CityConflict conflict in CityConflictTable.Instance.Rows )
                {
                    if ( conflict.DuringGameplay_State == CityConflictState.Active )
                        conflict.DoAbandon();
                }

                debugStage = 60100;

                foreach ( MachineStructure structure in afterFinalDoomStructures )
                {
                    try
                    {
                        structure?.ScrapStructureNow( ScrapReason.Cheat, Rand ); //cheat bypasses some things
                    }
                    catch ( Exception e )
                    {
                        ArcenDebugging.LogSingleLine( "ScrapStructureNow fail during final doom: " + e, Verbosity.ShowAsError );
                    }
                }

                afterFinalDoomStructures.Clear();

                debugStage = 70100;

                if ( afterFinalDoomVector3Targets.Count > 0 )
                {
                    debugStage = 80100;

                    bool isFirst = true;
                    foreach ( Vector3 epicenter in afterFinalDoomVector3Targets )
                    {
                        debugStage = 84100;
                        ParticleSoundRefs.FullNuke.DuringGame_PlayAtLocation( epicenter,
                            new Vector3( 0, Rand.NextFloat( 0, 360f ), 0 ) );
                        if ( isFirst )
                        {
                            debugStage = 88100;
                            isFirst = false;
                            ParticleSoundRefs.NukeSound2.DuringGame_PlayAtLocation( epicenter );
                            ParticleSoundRefs.NukeSound3.DuringGame_PlayAtLocation( epicenter );
                            ParticleSoundRefs.NukeSound4.DuringGame_PlayAtLocation( epicenter );

                            VisManagerVeryBase.Instance.MainCamera_JumpCameraToPosition( epicenter, false );
                        }

                        debugStage = 92100;

                        QueuedBuildingDestructionData destructionData;
                        destructionData.Epicenter = epicenter;
                        destructionData.Range = nukeRadius;
                        destructionData.StatusToApply = CommonRefs.BurnedAndIrradiatedBuildingStatus;
                        destructionData.AlsoDestroyOtherItems = true;
                        destructionData.AlsoDestroyUnits = true;
                        destructionData.SkipUnitsWithArmorPlating = false;
                        destructionData.IrradiateCells = true;
                        destructionData.UnitsToSpawnAfter = null;
                        destructionData.StatisticForDeaths = CityStatisticRefs.DeathsDuringVorsiberWrath;
                        destructionData.IsCausedByPlayer = false;
                        destructionData.IsFromJob = null;
                        destructionData.ExtraCode = null;

                        SimCommon.QueuedBuildingDestruction.Enqueue( destructionData );
                    }
                }

                debugStage = 1062100;

                ArcenMusicPlayer.Instance.CurrentOneShotOverridingMusicTrackPlaying = CommonRefs.FinalDoomArrivedMusic.MusicListFull.GetRandom( Rand );
                debugStage = 1262100;
                SimCommon.SetWeather( CommonRefs.PostApocalypseAWeather, true );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogDebugStageWithStack( "DoAfterFinalDoom", debugStage, e, Verbosity.ShowAsError );
            }
        }
        #endregion

        public void HandleStreetItem( ProjectOutcome Outcome, ProjectOutcomeStreetSenseItem StreetItem, ISimBuilding Building, ISimMachineActor Actor, 
            MersenneTwister Rand, ArcenDoubleCharacterBuffer PopupBufferOrNull )
        {
            if ( StreetItem == null || Actor == null )
                return;

            MachineProject project = Outcome.ParentProject;
            if ( project == null )
            {
                ArcenDebugging.LogSingleLine( "Null project on ProjectOutcome '" + Outcome.CombinedID + "'!", Verbosity.ShowAsError );
                return;
            }

            try
            {

                switch ( Outcome.CombinedID )
                {
                    #region Ch2_MIN_LostKids_StopThePoisoning
                    case "Ch2_MIN_LostKids_StopThePoisoning-Sole":
                        {
                            switch ( StreetItem.ID )
                            {
                                case "Arson":
                                    Building?.SetStatus( CommonRefs.BurnedOutBuildingStatus );
                                    //Building?.KillEveryoneHere();
                                    Building?.GetMapItem()?.DropBurningEffect_Slow();

                                    Actor?.AddOrRemoveBadge( CommonRefs.MarkedDefective, true );
                                    break;
                            }
                        }
                        break;
                    #endregion
                    default:
                        if ( !Outcome.ParentProject.HasShownHandlerMissingError )
                        {
                            Outcome.ParentProject.HasShownHandlerMissingError = true;
                            ArcenDebugging.LogSingleLine( "Projects_ChapterTwo: No handler is set up for '" + Outcome.CombinedID + "'!", Verbosity.ShowAsError );
                        }
                        break;
                }
            }
            catch ( Exception e )
            {
                if ( !Outcome.ParentProject.HasShownCaughtError )
                {
                    Outcome.ParentProject.HasShownCaughtError = true;
                    ArcenDebugging.LogSingleLine( "HandleStreetItem: Error in '" + Outcome.CombinedID + "': " + e, Verbosity.ShowAsError );
                }
            }
        }
    }
}

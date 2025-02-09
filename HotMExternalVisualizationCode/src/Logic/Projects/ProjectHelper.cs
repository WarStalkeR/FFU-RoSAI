using System;

using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.ExternalVis.CityLifeEffects;

namespace Arcen.HotM.ExternalVis
{    
    public static class ProjectHelper
    {
        #region WriteTurnCountdown
        public static void WriteTurnCountdown( ProjectLogic Logic, ProjectOutcome Outcome, int TurnsRemaining, ArcenCharacterBufferBase Buffer )
        {
            if ( TurnsRemaining < 1 )
                TurnsRemaining = 1;

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_1, ColorTheme.RustLighter )
                            .AddRaw( TurnsRemaining.ToStringThousandsWhole(), ColorTheme.RustLighter );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddSpriteStyled_NoIndent( IconRefs.Next_NextTurn.Icon, AdjustedSpriteStyle.InlineLarger1_1, ColorTheme.RustLighter )
                            .AddRaw( TurnsRemaining.ToStringThousandsWhole(), ColorTheme.RustLighter );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromOnePercentageNumber
        public static void WritePercentageFromOnePercentageNumber( ProjectLogic Logic, ProjectOutcome Outcome, int Percentage0To100, ArcenCharacterBufferBase Buffer )
        {
            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( Percentage0To100.ToStringIntPercent(), Percentage0To100 >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( Percentage0To100.ToStringIntPercent(), Percentage0To100 >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromTwoNumbersMovingDownToZero
        public static void WritePercentageFromTwoNumbersMovingDownToZero( ProjectLogic Logic, ProjectOutcome Outcome, int CurrentVal, int OriginalVal, ArcenCharacterBufferBase Buffer )
        {
            if ( CurrentVal < 0 )
                CurrentVal = 0;

            int progress = OriginalVal - CurrentVal;

            int percentageInt = CurrentVal <= 0 ? 100 : MathA.IntPercentageClamped( progress, OriginalVal, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromInvestigationType
        public static void WritePercentageFromInvestigationType( ProjectLogic Logic, ProjectOutcome Outcome, InvestigationType investType, ArcenCharacterBufferBase Buffer )
        {
            int percentageInt = 0;
            foreach ( Investigation investigation in SimCommon.AllInvestigationsIncludingInvisible )
            {
                if ( investigation.Type == investType )
                {
                    percentageInt = MathA.IntPercentageClamped( investigation.OriginalPossibleBuildingCount - investigation.PossibleBuildings.Count, investigation.OriginalPossibleBuildingCount, 0, 99 );
                    break;
                }
            }

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromTwoNumbers
        public static void WritePercentageFromTwoNumbers( ProjectLogic Logic, ProjectOutcome Outcome, int Current, int Target, ArcenCharacterBufferBase Buffer )
        {
            if ( Current < 0 )
                Current = 0;
            if ( Target < 1 )
                Target = 1;

            int percentageInt = Current >= Target ? 100 : MathA.IntPercentageClamped( Current, Target, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }

        public static void WritePercentageFromTwoNumbers( ProjectLogic Logic, ProjectOutcome Outcome, Int64 Current, Int64 Target, ArcenCharacterBufferBase Buffer )
        {
            if ( Current < 0 )
                Current = 0;
            if ( Target < 1 )
                Target = 1;

            int percentageInt = Current >= Target ? 100 : MathA.IntPercentageClamped( Current, Target, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromFourNumbers
        public static void WritePercentageFromFourNumbers( ProjectLogic Logic, ProjectOutcome Outcome, int CurrentA, int TargetA, int CurrentB, int TargetB, ArcenCharacterBufferBase Buffer )
        {
            if ( CurrentA < 0 )
                CurrentA = 0;
            if ( TargetA < 1 )
                TargetA = 1;

            if ( CurrentB < 0 )
                CurrentB = 0;
            if ( TargetB < 1 )
                TargetB = 1;

            int percentageIntA = CurrentA >= TargetA ? 100 : MathA.IntPercentageClamped( CurrentA, TargetA, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntB = CurrentB >= TargetB ? 100 : MathA.IntPercentageClamped( CurrentB, TargetB, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            int percentageInt;
            if ( percentageIntA >= 100 && percentageIntB >= 100 )
                percentageInt = 100;
            else
                percentageInt = Mathf.FloorToInt( (float)(percentageIntA+ percentageIntB) * 0.5f);

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromSixNumbers
        public static void WritePercentageFromSixNumbers( ProjectLogic Logic, ProjectOutcome Outcome, int CurrentA, int TargetA, int CurrentB, int TargetB, int CurrentC, int TargetC, ArcenCharacterBufferBase Buffer )
        {
            if ( CurrentA < 0 )
                CurrentA = 0;
            if ( TargetA < 1 )
                TargetA = 1;

            if ( CurrentB < 0 )
                CurrentB = 0;
            if ( TargetB < 1 )
                TargetB = 1;

            if ( CurrentC < 0 )
                CurrentC = 0;
            if ( TargetC < 1 )
                TargetC = 1;

            int percentageIntA = CurrentA >= TargetA ? 100 : MathA.IntPercentageClamped( CurrentA, TargetA, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntB = CurrentB >= TargetB ? 100 : MathA.IntPercentageClamped( CurrentB, TargetB, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntC = CurrentC >= TargetC ? 100 : MathA.IntPercentageClamped( CurrentC, TargetC, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            int percentageInt;
            if ( percentageIntA >= 100 && percentageIntB >= 100 && percentageIntC >= 100 )
                percentageInt = 100;
            else
                percentageInt = Mathf.FloorToInt( (float)(percentageIntA + percentageIntB + percentageIntC) * 0.333f );

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region WritePercentageFromEightNumbers
        public static void WritePercentageFromEightNumbers( ProjectLogic Logic, ProjectOutcome Outcome, int CurrentA, int TargetA, int CurrentB, int TargetB, 
            int CurrentC, int TargetC, int CurrentD, int TargetD, ArcenCharacterBufferBase Buffer )
        {
            if ( CurrentA < 0 )
                CurrentA = 0;
            if ( TargetA < 1 )
                TargetA = 1;

            if ( CurrentB < 0 )
                CurrentB = 0;
            if ( TargetB < 1 )
                TargetB = 1;

            if ( CurrentC < 0 )
                CurrentC = 0;
            if ( TargetC < 1 )
                TargetC = 1;

            if ( CurrentD < 0 )
                CurrentD = 0;
            if ( TargetD < 1 )
                TargetD = 1;

            int percentageIntA = CurrentA >= TargetA ? 100 : MathA.IntPercentageClamped( CurrentA, TargetA, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntB = CurrentB >= TargetB ? 100 : MathA.IntPercentageClamped( CurrentB, TargetB, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntC = CurrentC >= TargetC ? 100 : MathA.IntPercentageClamped( CurrentC, TargetC, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100
            int percentageIntD = CurrentD >= TargetD ? 100 : MathA.IntPercentageClamped( CurrentD, TargetD, 0, 99 ); //make sure it never rounds up to 100 until it actually reaches 100

            int percentageInt;
            if ( percentageIntA >= 100 && percentageIntB >= 100 && percentageIntC >= 100 && percentageIntD > 100 )
                percentageInt = 100;
            else
                percentageInt = Mathf.FloorToInt( (float)(percentageIntA + percentageIntB + percentageIntC + percentageIntD) * 0.25f );

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                    {
                        bool isHovered = !Buffer.GetIsEmpty(); //only reason something would be there so far is because it was hovered
                        bool isChosen = Outcome.ParentProject.DuringGame_IntendedOutcome == Outcome || Outcome.ParentProject.DuringGame_ActualOutcome == Outcome;
                        Buffer.StartSize40().AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ?
                            (isChosen ? ColorTheme.GetProjectTextReadyToCompleteAndChosen( isHovered ) : ColorTheme.GetProjectTextReadyToComplete( isHovered )) : string.Empty );
                    }
                    break;
                case ProjectLogic.WriteProgressTextBrief:
                    {
                        bool isHovered = ArcenUI.GetIsLinkIDHovered( Outcome.ParentProject.ID );
                        Buffer.AddRaw( percentageInt.ToStringIntPercent(), percentageInt >= 100 ? ColorTheme.GetHeaderGoldMoreRich( isHovered ) : ColorTheme.GetNone( isHovered ) );
                    }
                    break;
            }
        }
        #endregion

        #region HandleScienceWork2X
        public static void HandleScienceWork2X( ProjectLogic Logic, ProjectOutcome OutcomeOrNoneYet, 
            int goal1, int goal2, 
            GMathInt Science1, GMathInt Science2,
            ResourceType Worker1, ResourceType Worker2,
            ArcenCharacterBufferBase BufferOrNull, ref bool CanBeCompletedNow, MersenneTwister RandOrNull )
        {
            int current1 = OutcomeOrNoneYet.DuringGameplay_Accumulator1;
            int current2 = OutcomeOrNoneYet.DuringGameplay_Accumulator2;

            CanBeCompletedNow = current1 >= goal1 && current2 >= goal2;

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                case ProjectLogic.WriteProgressTextBrief:
                    WritePercentageFromFourNumbers( Logic, OutcomeOrNoneYet, current1, goal1, current2, goal2, BufferOrNull );
                    break;
                case ProjectLogic.WriteRequirements_OneLine:
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current1.ToStringThousandsWhole(), goal1.ToStringThousandsWhole(), Science1.GetDisplayName() );
                    BufferOrNull.Space5x();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current2.ToStringThousandsWhole(), goal2.ToStringThousandsWhole(), Science2.GetDisplayName() );
                    BufferOrNull.Line();
                    break;
                case ProjectLogic.WriteRequirements_ManyLines:
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current1.ToStringThousandsWhole(), goal1.ToStringThousandsWhole(), Science1.GetDisplayName() );
                    BufferOrNull.Line();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current2.ToStringThousandsWhole(), goal2.ToStringThousandsWhole(), Science2.GetDisplayName() );
                    BufferOrNull.Line();
                    break;
                case ProjectLogic.WriteAddedContext: //nothing on this one
                    break;
                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                    {
                        OutcomeOrNoneYet.DuringGameplay_Accumulator1 += (int)Worker1.Current * Science1.GetRandomBetweenInclusive( RandOrNull );
                        OutcomeOrNoneYet.DuringGameplay_Accumulator2 += (int)Worker2.Current * Science2.GetRandomBetweenInclusive( RandOrNull );

                        if ( OutcomeOrNoneYet.DuringGameplay_Accumulator1 > goal1 )
                            OutcomeOrNoneYet.DuringGameplay_Accumulator1 = goal1;
                        if ( OutcomeOrNoneYet.DuringGameplay_Accumulator2 > goal2 )
                            OutcomeOrNoneYet.DuringGameplay_Accumulator2 = goal2;
                    }
                    break;
                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                    {
                    }
                    break;
                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                    {
                    }
                    break;
            }
        }
        #endregion

        #region HandleScienceWork3X
        public static void HandleScienceWork3X( ProjectLogic Logic, ProjectOutcome OutcomeOrNoneYet,
            int goal1, int goal2, int goal3,
            GMathInt Science1, GMathInt Science2, GMathInt Science3,
            ResourceType Worker1, ResourceType Worker2, ResourceType Worker3,
            ArcenCharacterBufferBase BufferOrNull, ref bool CanBeCompletedNow, MersenneTwister RandOrNull )
        {
            int current1 = OutcomeOrNoneYet.DuringGameplay_Accumulator1;
            int current2 = OutcomeOrNoneYet.DuringGameplay_Accumulator2;
            int current3 = OutcomeOrNoneYet.DuringGameplay_Accumulator3;

            CanBeCompletedNow = current1 >= goal1 && current2 >= goal2;

            switch ( Logic )
            {
                case ProjectLogic.WriteProgressIconText:
                case ProjectLogic.WriteProgressTextBrief:
                    WritePercentageFromSixNumbers( Logic, OutcomeOrNoneYet, current1, goal1, current2, goal2, current3, goal3, BufferOrNull );
                    break;
                case ProjectLogic.WriteRequirements_OneLine:
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current1.ToStringThousandsWhole(), goal1.ToStringThousandsWhole(), Science1.GetDisplayName() );
                    BufferOrNull.Space5x();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current2.ToStringThousandsWhole(), goal2.ToStringThousandsWhole(), Science2.GetDisplayName() );
                    BufferOrNull.Space5x();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current3.ToStringThousandsWhole(), goal3.ToStringThousandsWhole(), Science3.GetDisplayName() );
                    BufferOrNull.Line();
                    break;
                case ProjectLogic.WriteRequirements_ManyLines:
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current1.ToStringThousandsWhole(), goal1.ToStringThousandsWhole(), Science1.GetDisplayName() );
                    BufferOrNull.Line();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current2.ToStringThousandsWhole(), goal2.ToStringThousandsWhole(), Science2.GetDisplayName() );
                    BufferOrNull.Line();
                    BufferOrNull.AddFormat3( "RequiredResourceAmount", current3.ToStringThousandsWhole(), goal3.ToStringThousandsWhole(), Science3.GetDisplayName() );
                    BufferOrNull.Line();
                    break;
                case ProjectLogic.WriteAddedContext: //nothing on this one
                    break;
                case ProjectLogic.DoAnyPerTurnLateLogicWhileProjectActive:
                    {
                        OutcomeOrNoneYet.DuringGameplay_Accumulator1 += (int)Worker1.Current * Science1.GetRandomBetweenInclusive( RandOrNull );
                        OutcomeOrNoneYet.DuringGameplay_Accumulator2 += (int)Worker2.Current * Science2.GetRandomBetweenInclusive( RandOrNull );
                        OutcomeOrNoneYet.DuringGameplay_Accumulator3 += (int)Worker3.Current * Science3.GetRandomBetweenInclusive( RandOrNull );

                        if ( OutcomeOrNoneYet.DuringGameplay_Accumulator1 > goal1 )
                            OutcomeOrNoneYet.DuringGameplay_Accumulator1 = goal1;
                        if ( OutcomeOrNoneYet.DuringGameplay_Accumulator2 > goal2 )
                            OutcomeOrNoneYet.DuringGameplay_Accumulator2 = goal2;
                        if ( OutcomeOrNoneYet.DuringGameplay_Accumulator3 > goal3 )
                            OutcomeOrNoneYet.DuringGameplay_Accumulator3 = goal3;
                    }
                    break;
                case ProjectLogic.DoAnyCustomLogicOnCompletionAttempt:
                    {
                    }
                    break;
                case ProjectLogic.DoAnyPerQuarterSecondLogicWhileProjectActive:
                    {
                    }
                    break;
            }
        }
        #endregion
    }
}

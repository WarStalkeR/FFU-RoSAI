using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class EventsBasic : IEventImplementation
    {
        public void HandleEventLogic( NPCEvent Event, NPCEventLogic Logic, ChoiceResultType ResultType, EventChoiceResult ResultOrNull, EventChoice FromChoiceOrNull, MersenneTwister workingRandOrNull, ArcenDoubleCharacterBuffer ExtraBodyTextBufferOrNull )
        {
            if ( Event == null )
                return;

            switch ( Event.ID )
            {
                case "Repeat_SellingToTheCasino":
                    #region Repeat_SellingToTheCasino
                    switch ( Logic )
                    {
                        case NPCEventLogic.OnStart:
                            {
                                string earningsString = "EarningsFromTheUltraWealthy";
                                string unitsString = "UnitsSoldToTheUltraWealthy";

                                {
                                    EventChoice greensChoice = Event.ChoicesLookup["Sell80PercentOfYourHydroponicGreens"];
                                    EventChoiceResult greensResult = greensChoice.AllPossibleResults.FirstOrDefault;

                                    int toSell = Mathf.RoundToInt( (int)ResourceRefs.HydroponicGreens.Current * 0.8f );
                                    float earningsPer = 0.16f;
                                    int totalEarnings = Mathf.CeilToInt( toSell * earningsPer );
                                    if ( toSell < 100000 )
                                    {
                                        greensChoice.DuringGame_CalculatedErrorStringToShow = Lang.Format1( "YouCannotSellFewerThan", 100000 );
                                        greensChoice.TrySetResourceCostToNewValue( ResourceRefs.HydroponicGreens, 100000 );

                                    }
                                    else
                                    {
                                        greensChoice.DuringGame_CalculatedErrorStringToShow = string.Empty;
                                        greensChoice.TrySetResourceCostToNewValue( ResourceRefs.HydroponicGreens, toSell );
                                        greensResult.TrySetResourceResultToNewValue( ResourceRefs.Wealth, totalEarnings, totalEarnings );
                                        greensResult.TrySetCityStatisticToNewValue( earningsString, totalEarnings );
                                        greensResult.TrySetCityStatisticToNewValue( unitsString, toSell );
                                    }
                                }

                                {
                                    EventChoice waterChoice = Event.ChoicesLookup["Sell80PercentOfYourFilteredWater"];
                                    EventChoiceResult waterResult = waterChoice.AllPossibleResults.FirstOrDefault;

                                    int toSell = Mathf.RoundToInt( (int)ResourceRefs.FilteredWater.Current * 0.8f );
                                    float earningsPer = 0.55f;
                                    int totalEarnings = Mathf.CeilToInt( toSell * earningsPer );
                                    if ( toSell < 80000 )
                                    {
                                        waterChoice.DuringGame_CalculatedErrorStringToShow = Lang.Format1( "YouCannotSellFewerThan", 80000 );
                                        waterChoice.TrySetResourceCostToNewValue( ResourceRefs.FilteredWater, 80000 );
                                    }
                                    else
                                    {
                                        waterChoice.DuringGame_CalculatedErrorStringToShow = string.Empty;
                                        waterChoice.TrySetResourceCostToNewValue( ResourceRefs.FilteredWater, toSell );
                                        waterResult.TrySetResourceResultToNewValue( ResourceRefs.Wealth, totalEarnings, totalEarnings );
                                        waterResult.TrySetCityStatisticToNewValue( earningsString, totalEarnings );
                                        waterResult.TrySetCityStatisticToNewValue( unitsString, toSell );
                                    }
                                }

                                if ( ResourceRefs.MonsterPelts != null )
                                {
                                    EventChoice peltsChoice = Event.ChoicesLookup["SellAllYourMonsterPelts"];
                                    EventChoiceResult peltResult = peltsChoice.AllPossibleResults.FirstOrDefault;

                                    int toSell = (int)ResourceRefs.MonsterPelts?.Current;
                                    float earningsPer = 5000f;
                                    int totalEarnings = Mathf.CeilToInt( toSell * earningsPer );
                                    if ( toSell < 1 )
                                    {
                                        peltsChoice.DuringGame_CalculatedErrorStringToShow = Lang.Format1( "YouCannotSellFewerThan", 1 );
                                        peltsChoice.TrySetResourceCostToNewValue( ResourceRefs.MonsterPelts, 1 );
                                    }
                                    else
                                    {
                                        peltsChoice.DuringGame_CalculatedErrorStringToShow = string.Empty;
                                        peltsChoice.TrySetResourceCostToNewValue( ResourceRefs.MonsterPelts, toSell );
                                        peltResult.TrySetResourceResultToNewValue( ResourceRefs.Wealth, totalEarnings, totalEarnings );
                                        peltResult.TrySetCityStatisticToNewValue( earningsString, totalEarnings );

                                        peltResult.TrySetCityStatisticToNewValue( unitsString, toSell );

                                        string unitsString2 = "MonsterPeltsSoldToTheUltraWealthy";

                                        peltResult.TrySetCityStatisticToNewValue( unitsString2, toSell );
                                    }
                                }
                            }
                            break;
                    }
                    #endregion
                    break;
                case "Repeat_SellingToTheReligiousCoOp":
                    #region Repeat_SellingToTheReligiousCoOp
                    switch ( Logic )
                    {
                        case NPCEventLogic.OnStart:
                            {
                                string earningsString = "EarningsFromReligiousCoOps";
                                string unitsString = "UnitsSoldToReligiousCoOps";

                                {
                                    EventChoice greensChoice = Event.ChoicesLookup["Sell80PercentOfYourHydroponicGreens"];
                                    EventChoiceResult greensResult = greensChoice.AllPossibleResults.FirstOrDefault;

                                    int toSell = Mathf.RoundToInt( (int)ResourceRefs.HydroponicGreens.Current * 0.8f );
                                    float earningsPer = 0.03f;
                                    int totalEarnings = Mathf.CeilToInt( toSell * earningsPer );
                                    if ( toSell < 50000 )
                                    {
                                        greensChoice.DuringGame_CalculatedErrorStringToShow = Lang.Format1( "YouCannotSellFewerThan", 50000 );
                                        greensChoice.TrySetResourceCostToNewValue( ResourceRefs.HydroponicGreens, 50000 );
                                    }
                                    else
                                    {
                                        greensChoice.DuringGame_CalculatedErrorStringToShow = string.Empty;
                                        greensChoice.TrySetResourceCostToNewValue( ResourceRefs.HydroponicGreens, toSell );
                                        greensResult.TrySetResourceResultToNewValue( ResourceRefs.Wealth, totalEarnings, totalEarnings );
                                        greensResult.TrySetCityStatisticToNewValue( earningsString, totalEarnings );
                                        greensResult.TrySetCityStatisticToNewValue( unitsString, toSell );
                                    }
                                }

                                {
                                    EventChoice waterChoice = Event.ChoicesLookup["Sell80PercentOfYourFilteredWater"];
                                    EventChoiceResult waterResult = waterChoice.AllPossibleResults.FirstOrDefault;

                                    int toSell = Mathf.RoundToInt( (int)ResourceRefs.FilteredWater.Current * 0.8f );
                                    float earningsPer = 0.11f;
                                    int totalEarnings = Mathf.CeilToInt( toSell * earningsPer );
                                    if ( toSell < 40000 )
                                    {
                                        waterChoice.DuringGame_CalculatedErrorStringToShow = Lang.Format1( "YouCannotSellFewerThan", 40000 );
                                        waterChoice.TrySetResourceCostToNewValue( ResourceRefs.FilteredWater, 40000 );
                                    }
                                    else
                                    {
                                        waterChoice.DuringGame_CalculatedErrorStringToShow = string.Empty;
                                        waterChoice.TrySetResourceCostToNewValue( ResourceRefs.FilteredWater, toSell );
                                        waterResult.TrySetResourceResultToNewValue( ResourceRefs.Wealth, totalEarnings, totalEarnings );
                                        waterResult.TrySetCityStatisticToNewValue( earningsString, totalEarnings );
                                        waterResult.TrySetCityStatisticToNewValue( unitsString, toSell );
                                    }
                                }
                            }
                            break;
                    }
                    #endregion
                    break;
                case "Repeat_DisableAlarm":
                    #region Repeat_DisableAlarm
                    switch ( Logic )
                    {
                        case NPCEventLogic.OnStart:
                            {
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithCompassion"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Compassion, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithApathy"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Apathy, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithCruelty"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Cruelty, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithDetermination"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Determination, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithWisdom"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Wisdom, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                                {
                                    EventChoice choice = Event.ChoicesLookup["BribeWithCreativity"];
                                    EventChoiceResult result = choice.AllPossibleResults.FirstOrDefault;
                                    choice.TrySetResourceCostToNewValue( ResourceRefs.Creativity, MathA.ToMultipleInt_Slow( 5, 2, choice.DuringGameplay_TimesChosenPreviouslyThisTimeline ) );
                                }
                            }
                            break;
                    }
                    #endregion
                    break;
                default:
                    ArcenDebugging.LogSingleLine( "EventsBasic: Called HandleEventLogic for '" + Event.ID + "', which does not support it!", Verbosity.ShowAsError );
                    break;
            }
        }
    }
}

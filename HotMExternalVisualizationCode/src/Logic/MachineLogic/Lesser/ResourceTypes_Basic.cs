using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis
{
    public class ResourceTypes_Basic : IResourceTypeImplementation
    {
        public string CalculateColorHex( ResourceType Resource )
        {
            return Resource.IconColorHex;
        }

        public void DoPerTurn( ResourceType Resource, MersenneTwister RandForThisTurn )
        {
            switch ( Resource.ID )
            {
                case "MentalEnergy":
                    {
                        if ( Resource.Current > 0 )
                            Resource.SetCurrent_Named( 0, string.Empty, false );

                        int amountToGain = MathRefs.MentalEnergyPerTurn.DuringGameplay_CurrentInt;
                        Resource.AlterCurrent_Named( amountToGain, "Income_BaselineMentalEnergyBudget", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    break;
                case "ShelteredHumans":
                    {
                        HandleMovingIn( RandForThisTurn );

                        {
                            ResourceType refugeeHumans = ResourceRefs.RefugeeHumans;
                            if ( refugeeHumans.Current > 0 )
                            {
                                int numberCanMoveTheGoodDirection = (int)Resource.EffectiveHardCapStorageAvailable;
                                if ( numberCanMoveTheGoodDirection > 0 )
                                {
                                    numberCanMoveTheGoodDirection = Mathf.Min( numberCanMoveTheGoodDirection, (int)refugeeHumans.Current );

                                    refugeeHumans.AlterCurrent_Named( -numberCanMoveTheGoodDirection, "Decrease_RefugeesMovingIntoPermanentHousing", ResourceAddRule.IgnoreUntilTurnChange );
                                    Resource.AlterCurrent_Named( numberCanMoveTheGoodDirection, "Increase_AbandonedCitizensMovingBackIn", ResourceAddRule.IgnoreUntilTurnChange );

                                    //do not further log this, because it would be double-counting
                                }
                            }
                        }
                    }
                    break;
                case "RefugeeHumans":
                    {
                        {
                            if ( ResourceRefs.AbandonedHumans.Current > 0 ) //at least some humans are abandoned
                            {
                                int numberCanMoveIn = (int)Resource.EffectiveHardCapStorageAvailable;
                                if ( numberCanMoveIn > 0 )
                                {
                                    numberCanMoveIn = Mathf.Min( numberCanMoveIn, (int)ResourceRefs.AbandonedHumans.Current );

                                    ResourceRefs.AbandonedHumans.AlterCurrent_Named( -numberCanMoveIn, "Expense_MovedIntoHousing", ResourceAddRule.IgnoreUntilTurnChange );
                                    Resource.AlterCurrent_Named( numberCanMoveIn, "Increase_AbandonedCitizensBecomingRefugees", ResourceAddRule.IgnoreUntilTurnChange );

                                    //log the move-ins!
                                    CityStatisticTable.AlterScore( "AbandonedHumansWhoBecameRefugees", numberCanMoveIn );
                                }
                            }
                        }
                        {
                            CityStatistic desperateHomeless = CityStatisticTable.Instance.GetRowByID( "DesperateHomeless" );
                            if ( desperateHomeless.GetScore() > 0 ) //at least some humans are abandoned
                            {
                                int numberCanMoveIn = (int)Resource.EffectiveHardCapStorageAvailable;
                                if ( numberCanMoveIn > 0 )
                                {
                                    numberCanMoveIn = Mathf.Min( numberCanMoveIn, (int)desperateHomeless.GetScore() );

                                    CityStatisticTable.AlterScore( "DesperateHomeless", -numberCanMoveIn ); //moving in, hooray!
                                    Resource.AlterCurrent_Named( numberCanMoveIn, "Increase_DesperateHomelessMovingIn", ResourceAddRule.IgnoreUntilTurnChange );

                                    //log the move-ins!
                                    CityStatisticTable.AlterScore( "DesperateHomelessWhoBecameRefugees", numberCanMoveIn );
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void DoPerQuarterSecond( ResourceType Resource, MersenneTwister RandForBackgroundThread )
        {
            switch ( Resource.ID )
            {
                case "ShelteredHumans":
                    {
                        if ( Resource.HardCap > 0 )
                        {
                            BuildingTag tents = BuildingTagTable.Instance.GetRowByID( "HomelessTent" );
                            EconomicClassType homelessClass = EconomicClassTypeTable.Instance.GetRowByID( "Homeless" );

                            CityStatisticTable.SetScore_UserBeware( "HomelessPopulationRemaining", World.People.GetCurrentResidents()[homelessClass] );
                            CityStatisticTable.SetScore_UserBeware( "HomelessTentsRemaining", tents.DuringGame_Buildings.Count );

                            EconomicClassType managerialClass = EconomicClassTypeTable.Instance.GetRowByID( "Managerial" );
                            EconomicClassType scientistsClass = EconomicClassTypeTable.Instance.GetRowByID( "Scientists" );

                            CityStatisticTable.SetScore_UserBeware( "WealthyConsumers", Mathf.CeilToInt( World.People.GetCurrentResidents()[managerialClass] * 0.92f ) +
                                Mathf.CeilToInt( World.People.GetCurrentResidents()[scientistsClass] * 0.76f ) );

                            EconomicClassType workingClass = EconomicClassTypeTable.Instance.GetRowByID( "WorkingClass" );
                            EconomicClassType uneducatedClass = EconomicClassTypeTable.Instance.GetRowByID( "Uneducated" );

                            CityStatisticTable.SetScore_UserBeware( "WorkingClassConsumers", Mathf.CeilToInt( World.People.GetCurrentResidents()[workingClass] * 0.98f ) +
                                Mathf.CeilToInt( World.People.GetCurrentResidents()[uneducatedClass] * 0.41f ) );
                        }

                        {
                            int totalPopulation = 0;
                            foreach ( KeyValuePair<EconomicClassType, int> kv in World.People.GetCurrentResidents() )
                                totalPopulation += kv.Value;

                            CityStatisticTable.SetScore_UserBeware( "CityHumanCitizenPopulation", totalPopulation );
                        }
                    }
                    break;
                case "RefugeeHumans":
                    break; //nothing to do; the ShelteredHumans section above handles this
            }
        }

        public void ReactToLossesDueToBeingOverCap( ResourceType Resource, long AmountLost )
        {
            switch ( Resource.ID )
            {
                case "ShelteredHumans":
                    {
                        ResourceType refugeeHumans = ResourceRefs.RefugeeHumans;
                        if ( refugeeHumans.EffectiveHardCapStorageAvailable > 0 && AmountLost > 0 )
                        {
                            int numberCanMoveTheBadDirection = (int)MathA.Min( refugeeHumans.EffectiveHardCapStorageAvailable, AmountLost );
                            if ( numberCanMoveTheBadDirection > 0 )
                            {
                                AmountLost -= numberCanMoveTheBadDirection;
                                Resource.AlterCurrent_Named( -numberCanMoveTheBadDirection, "Decrease_ShelteredHumansBecomingRefugees", ResourceAddRule.IgnoreUntilTurnChange );
                                refugeeHumans.AlterCurrent_Named( numberCanMoveTheBadDirection, "Increase_ShelteredHumansBecomingRefugees", ResourceAddRule.IgnoreUntilTurnChange );

                                //do not further log this, because it would be double-counting
                            }
                        }
                        //if could not do the above, or nothing left over
                        ResourceRefs.AbandonedHumans.AlterCurrent_Named( AmountLost, "Increase_LossOfYourShelter", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    break;
                case "RefugeeHumans":
                    {
                        ResourceRefs.AbandonedHumans.AlterCurrent_Named( AmountLost, "Increase_LossOfYourShelter", ResourceAddRule.IgnoreUntilTurnChange );
                    }
                    break;
            }
        }

        #region HandleMovingIn
        private static void HandleMovingIn( MersenneTwister RandForThisTurn )
        {
            if ( ResourceRefs.AbandonedHumans.Current > 0 ) //at least some humans are abandoned
            {
                int numberCanMoveInShelter = (int)ResourceRefs.ShelteredHumans.EffectiveHardCapStorageAvailable;
                int numberCanMoveInRefugee = (int)ResourceRefs.RefugeeHumans.EffectiveHardCapStorageAvailable;
                if ( numberCanMoveInShelter > 0 )
                {
                    numberCanMoveInShelter = Mathf.Min( numberCanMoveInShelter, (int)ResourceRefs.AbandonedHumans.Current );

                    ResourceRefs.AbandonedHumans.AlterCurrent_Named( -numberCanMoveInShelter, "Expense_MovedIntoHousing", ResourceAddRule.IgnoreUntilTurnChange );
                    ResourceRefs.ShelteredHumans.AlterCurrent_Named( numberCanMoveInShelter, "Increase_AbandonedCitizensMovingBackIn", ResourceAddRule.IgnoreUntilTurnChange );

                    //log the move-ins!
                    CityStatisticTable.AlterScore( "AbandonedHumansWhoMovedBackIn", numberCanMoveInShelter );
                }
                else if ( numberCanMoveInRefugee > 0 )
                {
                    numberCanMoveInRefugee = Mathf.Min( numberCanMoveInRefugee, (int)ResourceRefs.AbandonedHumans.Current );

                    ResourceRefs.AbandonedHumans.AlterCurrent_Named( -numberCanMoveInRefugee, "Expense_MovedIntoHousing", ResourceAddRule.IgnoreUntilTurnChange );
                    ResourceRefs.RefugeeHumans.AlterCurrent_Named( numberCanMoveInRefugee, "Increase_AbandonedCitizensBecomingRefugees", ResourceAddRule.IgnoreUntilTurnChange );

                    //log the move-ins!
                    CityStatisticTable.AlterScore( "AbandonedHumansWhoBecameRefugees", numberCanMoveInRefugee );
                }
                else
                {
                    //if no humans can move back in, have some die from exposure
                    int deaths = Mathf.CeilToInt( (int)ResourceRefs.AbandonedHumans.Current * RandForThisTurn.NextFloat( 0.02f, 0.25f ) ); //between 2 and 25 percent die each turn
                    deaths = Mathf.Min( deaths, (int)ResourceRefs.AbandonedHumans.Current );

                    ResourceRefs.AbandonedHumans.AlterCurrent_Named( -deaths, "Expense_DeathsFromExposure", ResourceAddRule.IgnoreUntilTurnChange );

                    //log the deaths!
                    CityStatisticTable.AlterScore( "AbandonedHumanDeathsFromExposure", deaths );
                }
            }

            {
                CityStatistic desperateHomeless = CityStatisticTable.Instance.GetRowByID( "DesperateHomeless" );
                if ( desperateHomeless.GetScore() > 0 ) //at least some humans are abandoned
                {
                    int numberCanMoveInShelter = (int)ResourceRefs.ShelteredHumans.EffectiveHardCapStorageAvailable;
                    int numberCanMoveInRefugee = (int)ResourceRefs.RefugeeHumans.EffectiveHardCapStorageAvailable;
                    if ( numberCanMoveInShelter > 0 )
                    {
                        numberCanMoveInShelter = Mathf.Min( numberCanMoveInShelter, (int)desperateHomeless.GetScore() );

                        CityStatisticTable.AlterScore( "DesperateHomeless", -numberCanMoveInShelter ); //moving in, hooray!
                        ResourceRefs.ShelteredHumans.AlterCurrent_Named( numberCanMoveInShelter, "Increase_DesperateHomelessMovingIn", ResourceAddRule.IgnoreUntilTurnChange );

                        //log the move-ins!
                        CityStatisticTable.AlterScore( "DesperateHomelessWhoMovedIn", numberCanMoveInShelter );
                    }
                    else if ( numberCanMoveInRefugee > 0 )
                    {
                        numberCanMoveInRefugee = Mathf.Min( numberCanMoveInRefugee, (int)desperateHomeless.GetScore() );

                        CityStatisticTable.AlterScore( "DesperateHomeless", -numberCanMoveInRefugee ); //moving in, hooray!
                        ResourceRefs.RefugeeHumans.AlterCurrent_Named( numberCanMoveInRefugee, "Increase_DesperateHomelessMovingIn", ResourceAddRule.IgnoreUntilTurnChange );

                        //log the move-ins!
                        CityStatisticTable.AlterScore( "DesperateHomelessWhoBecameRefugees", numberCanMoveInRefugee );
                    }
                    else
                    {
                        //if no humans can move in, have some die from exposure
                        int deaths = Mathf.CeilToInt( (int)desperateHomeless.GetScore() * RandForThisTurn.NextFloat( 0.02f, 0.25f ) ); //between 2 and 25 percent die each turn
                        deaths = Mathf.Min( deaths, (int)desperateHomeless.GetScore() );

                        CityStatisticTable.AlterScore( "DesperateHomeless", -deaths ); //deaths, boo!

                        //log the deaths!
                        CityStatisticTable.AlterScore( "DesperateHomelessDeathsFromExposure", deaths );
                    }
                }
            }
        }
        #endregion
    }
}

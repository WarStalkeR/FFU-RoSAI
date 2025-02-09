using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis
{
    public class PopulationStatisticsDisplayStyles_Basic : IUIXDisplayStyleImplementation<IAggregatedPopulationDimension>
    {
        public void WriteSecondaryText_UIXDisplayStyle( ArcenDoubleCharacterBuffer buffer, IAggregatedPopulationDimension DataItemGeneral, 
            UIXDisplayItem<IAggregatedPopulationDimension> DisplayStyle, ButtonAbstractBase Button )
        {
            switch ( DisplayStyle.GetID() )
            {
                case "Normal":
                    {
                        bool isSelected = false;// Instance.SelectedFile == this.Save;
                        Button.SetRelatedImage0EnabledIfNeeded( isSelected );
                        buffer.StartColor( isSelected ? ColorTheme.GetInvertibleListTextBlue_Selected( Button.Element.LastHadMouseWithin ) :
                            ColorTheme.GetBasicLightTextBlue( Button.Element.LastHadMouseWithin ) );

                        buffer.AddRaw( DataItemGeneral.GetDisplayName() );
                        buffer.Position300().StartSize90();

                        if ( DataItemGeneral is EconomicClassType )
                        {
                            EconomicClassType DataItem = (EconomicClassType)DataItemGeneral;
                            DictionaryView<EconomicClassType, int> population = World.People.GetCurrentResidents();
                            DictionaryView<EconomicClassType, int> residentialCap = World.People.GetResidentialCapacity();
                            DictionaryView<EconomicClassType, int> unemployedResidents = World.People.GetUnemployedResidents();                            

                            int populationCount = population[DataItem];
                            int residentialAvailability = residentialCap[DataItem];
                            int unemployedResidentCount = unemployedResidents[DataItem];

                            buffer.AddLang( "PopulationStatistics_Residents", populationCount > residentialAvailability ? ColorTheme.RedLess : ColorTheme.CyanDim );
                            buffer.Space1x();
                            buffer.AddRaw( populationCount.ToStringLargeNumberAbbreviated() );
                            buffer.Space8x();

                            buffer.AddLang( "PopulationStatistics_Housing", ColorTheme.CyanDim );
                            buffer.Space1x();
                            buffer.AddRaw( residentialAvailability.ToStringLargeNumberAbbreviated() );
                            buffer.Space8x();

                            buffer.AddLang( "PopulationStatistics_Unemployed", unemployedResidentCount > 0 && DataItem.ValidProfessions.Count > 0 ? ColorTheme.RedLess : ColorTheme.CyanDim );
                            buffer.Space1x();
                            buffer.AddRaw( unemployedResidentCount.ToStringLargeNumberAbbreviated() );
                            buffer.Space8x();
                        }
                        else if ( DataItemGeneral is ProfessionType )
                        {
                            ProfessionType DataItem = (ProfessionType)DataItemGeneral;
                            DictionaryView<ProfessionType, int> workers = World.People.GetCurrentWorkers();
                            DictionaryView<ProfessionType, int> jobs = World.People.GetAllJobs();
                            int workerCount = workers[DataItem];
                            int jobCount = jobs[DataItem];

                            buffer.AddLang( "PopulationStatistics_Workers", ColorTheme.CyanDim );
                            buffer.Space1x();
                            buffer.AddRaw( workerCount.ToStringLargeNumberAbbreviated() );
                            buffer.Space8x();

                            buffer.AddLang( "PopulationStatistics_Jobs", ColorTheme.CyanDim );
                            buffer.Space1x();
                            buffer.AddRaw( jobCount.ToStringLargeNumberAbbreviated() );
                            buffer.Space8x();
                        }
                        else
                            throw new Exception( "Normal PopulationStatisticsDisplayStyles_Basic WriteSecondaryText_UIXDisplayStyle: Not set up for Data type '" +
                                DataItemGeneral.GetType() + "'!" );
                        

                        buffer.EndSize();
                    }
                    break;
                default:
                    throw new Exception( "PopulationStatisticsDisplayStyles_Basic WriteSecondaryText_UIXDisplayStyle: Not set up for '" + DisplayStyle.GetID() + "'!" );
            }
        }
    }
}

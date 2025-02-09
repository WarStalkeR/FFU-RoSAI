using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Random = System.Random;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Central world data and lookups about people as residents, workers, participants, etc.
    /// </summary>
    internal class World_People : ISimWorld_People
    {
        public static World_People QueryInstance = new World_People();

        //
        //Serialized data
        //-----------------------------------------------------

        //
        //Nonserialized data
        //-----------------------------------------------------
        public static readonly DoubleBufferedDictionary<ProfessionType, int> AllJobs = DoubleBufferedDictionary<ProfessionType, int>.Create_WillNeverBeGCed( 20, "World_People-AllJobs" );
        public static readonly DoubleBufferedDictionary<ProfessionType, int> CurrentWorkers = DoubleBufferedDictionary<ProfessionType, int>.Create_WillNeverBeGCed( 20, "World_People-CurrentWorkers" );
        public static readonly DoubleBufferedDictionary<EconomicClassType, int> ResidentialCapacity = DoubleBufferedDictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 20, "World_People-ResidentialCapacity" );
        public static readonly DoubleBufferedDictionary<EconomicClassType, int> CurrentResidents = DoubleBufferedDictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 20, "World_People-CurrentResidents" );
        public static readonly DoubleBufferedDictionary<EconomicClassType, int> UnemployedResidents = DoubleBufferedDictionary<EconomicClassType, int>.Create_WillNeverBeGCed( 10, "World_People-UnemployedResidents" );
        public static int UnemployedResidentCount = 0;
        private static SortedDictionary<BuildingType, RefPair<int, int>> workingCountByBuildingType = SortedDictionary<BuildingType, RefPair<int, int>>.Create_WillNeverBeGCed( 600, "World_People-workingCountByBuildingType", 200 );

        public static void OnGameClear()
        {
            AllJobs.ClearAllVersions();
            ResidentialCapacity.ClearAllVersions();
            CurrentResidents.ClearAllVersions();
            UnemployedResidents.ClearAllVersions();
            CurrentWorkers.ClearAllVersions();
            UnemployedResidentCount = 0;
            workingCountByBuildingType.Clear();
        }

        #region Serialization
        public static void Serialize( ArcenFileSerializer Serializer )
        {
        }

        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {

        }
        #endregion

        #region RecalculateCacheForResidentsAndWorkers
        public static void RecalculateCacheForResidentsAndWorkers()
        {
			foreach ( KeyValuePair<int, SimBuilding> kv in World_Buildings.BuildingsByID )
            {
                if ( kv.Value != null )
                    kv.Value.RecalculateCacheForResidentsAndWorkers();
            }
        }
        #endregion

        #region EconomicClass Interface Writers
        public void EconomicClassType_WriteDataItemUIXTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp, TooltipShadowStyle ShadowStyle, EconomicClassType EconClass )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( EconClass ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
            {
                //anything we want to say about an economic class's data goes here
                DictionaryView<EconomicClassType, int> population = this.GetCurrentResidents();
                DictionaryView<EconomicClassType, int> residentialCap = this.GetResidentialCapacity();

                int populationCount = population[EconClass];
                int residentialAvailability = residentialCap[EconClass];

                novel.ShadowStyle = ShadowStyle;
                novel.TitleUpperLeft.AddRaw( EconClass.GetDisplayName() );
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_Residents", ColorTheme.DataLabelWhite )
                    .AddRaw( populationCount.ToStringLargeNumberAbbreviated(), populationCount > residentialAvailability ? ColorTheme.RedLess : ColorTheme.DataBlue ).Line();
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_Housing", ColorTheme.DataLabelWhite )
                    .AddRaw( residentialAvailability.ToStringLargeNumberAbbreviated(), ColorTheme.DataBlue ).Line();
            }
        }

        public void EconomicClassType_WriteDataItemUIXClickedDetails( ArcenDoubleCharacterBuffer Buffer, EconomicClassType EconClass )
        {
            //anything we want to say about an economic class's data goes here
            DictionaryView<EconomicClassType, int> population = this.GetCurrentResidents();
            DictionaryView<EconomicClassType, int> residentialCap = this.GetResidentialCapacity();

            int populationCount = population[EconClass];
            int residentialAvailability = residentialCap[EconClass];

            Buffer.AddLang( "PopulationStatistics_Residents", populationCount > residentialAvailability ? ColorTheme.RedLess : ColorTheme.CyanDim );
            Buffer.Position150();
            Buffer.AddRaw( populationCount.ToStringLargeNumberAbbreviated() );
            Buffer.Line();

            Buffer.AddLang( "PopulationStatistics_Housing", ColorTheme.CyanDim );
            Buffer.Position150();
            Buffer.AddRaw( residentialAvailability.ToStringLargeNumberAbbreviated() );
            Buffer.Line();

            #region Populate workingCountByBuildingType From Buildings List And Sort Into listCountByBuildingType
            workingCountByBuildingType.Clear();
            foreach ( KeyValuePair<int, SimBuilding> kv in World_Buildings.BuildingsByID )
            {
                SimBuilding building = kv.Value;
                int maxResidents = 0;
                if ( !building.Prefab.NormalMaxResidentsByEconomicClass.TryGetValue( EconClass, out maxResidents ) )
                    maxResidents = 0;
                if ( maxResidents > 0 )
                {
                    RefPair<int, int> values = null;
                    if ( !workingCountByBuildingType.TryGetValue( building.Prefab.Type, out values ) )
                    {
                        values = new RefPair<int, int>( 0, 0 );
                        workingCountByBuildingType[building.Prefab.Type] = values;
                    }

                    values.RightItem += maxResidents;
                    values.LeftItem += building.Residents[EconClass];

                }
            }

            List<KeyValuePair<BuildingType, RefPair<int, int>>> listCountByBuildingType = workingCountByBuildingType.SortIntoList(
                delegate ( KeyValuePair<BuildingType, RefPair<int, int>> Left, KeyValuePair<BuildingType, RefPair<int, int>> Right )
                {
                    int val = Right.Value.RightItem.CompareTo( Left.Value.RightItem ); //desc
                    if ( val != 0 )
                        return val;
                    val = Right.Value.LeftItem.CompareTo( Left.Value.LeftItem ); //desc
                    if ( val != 0 )
                        return val;
                    return Left.Key.GetDisplayName().CompareTo( Left.Key.GetDisplayName() );
                } );
            #endregion

            if ( listCountByBuildingType.Count > 0 )
            {
                Buffer.Line();
                Buffer.AddLang( "PopulationStatistics_ByBuildingType", ColorTheme.CyanDim );
                Buffer.Position250();
                Buffer.AddLang( "PopulationStatistics_Residents", ColorTheme.CyanDim );
                Buffer.Position400();
                Buffer.AddLang( "PopulationStatistics_Housing", ColorTheme.CyanDim );

                Buffer.Line();

                foreach ( KeyValuePair<BuildingType, RefPair<int, int>> kv in listCountByBuildingType )
                {
                    Buffer.AddRaw( kv.Key.GetDisplayName() );
                    Buffer.Position250();
                    Buffer.AddRaw( kv.Value.LeftItem.ToStringLargeNumberAbbreviated() );
                    Buffer.Position400();
                    Buffer.AddRaw( kv.Value.RightItem.ToStringLargeNumberAbbreviated() );
                    Buffer.Line();
                }
            }
        }

        public void EconomicClassType_WriteDataItemUIXClickedDetails_SubTooltipLinkHover(
            EconomicClassType EconClass, string[] TooltipLinkData )
        {
            string linkID = TooltipLinkData[0];
            switch ( linkID )
            {
                default:
                    break;
            }
        }
        #endregion

        #region ProfessionType Interface Writers
        public void ProfessionType_WriteDataItemUIXTooltip( IArcenUIElementForSizing DrawNextTo, SideClamp Clamp, TooltipShadowStyle ShadowStyle, ProfessionType Profession )
        {
            NovelTooltipBuffer novel = NovelTooltipBuffer.Instance;

            if ( novel.TryStartSmallerTooltip( TooltipID.Create( Profession ), DrawNextTo, Clamp, TooltipNovelWidth.Simple ) )
            {
                //anything we want to say about a profession's data goes here
                DictionaryView<ProfessionType, int> workers = this.GetCurrentWorkers();
                DictionaryView<ProfessionType, int> jobs = this.GetAllJobs();
                int workerCount = workers[Profession];
                int jobCount = jobs[Profession];

                novel.ShadowStyle = ShadowStyle;
                novel.TitleUpperLeft.AddRaw( Profession.GetDisplayName() );
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_Workers", ColorTheme.DataLabelWhite )
                    .AddRaw( workerCount.ToStringLargeNumberAbbreviated(), ColorTheme.DataBlue ).Line();
                novel.Main.AddBoldLangAndAfterLineItemHeader( "PopulationStatistics_Jobs", ColorTheme.DataLabelWhite )
                    .AddRaw( jobCount.ToStringLargeNumberAbbreviated(), ColorTheme.DataBlue ).Line();
            }
        }

        public void ProfessionType_WriteDataItemUIXClickedDetails( ArcenDoubleCharacterBuffer Buffer, ProfessionType Profession )
        {
            //anything we want to say about a profession's data goes here
            DictionaryView<ProfessionType, int> workers = this.GetCurrentWorkers();
            DictionaryView<ProfessionType, int> jobs = this.GetAllJobs();
            int workerCount = workers[Profession];
            int jobCount = jobs[Profession];

            Buffer.AddLang( "PopulationStatistics_Workers", ColorTheme.CyanDim );
            Buffer.Position150();
            Buffer.AddRaw( workerCount.ToStringLargeNumberAbbreviated() );
            Buffer.Line();

            Buffer.AddLang( "PopulationStatistics_Jobs", ColorTheme.CyanDim );
            Buffer.Position150();
            Buffer.AddRaw( jobCount.ToStringLargeNumberAbbreviated() );
            Buffer.Line();

            #region Populate workingCountByBuildingType From Buildings List And Sort Into listCountByBuildingType
            workingCountByBuildingType.Clear();
            foreach ( KeyValuePair<int, SimBuilding> kv in World_Buildings.BuildingsByID )
            {
                SimBuilding building = kv.Value;
                int maxWorkers = 0;
                if ( !building.Prefab.NormalMaxJobsByProfession.TryGetValue( Profession, out maxWorkers ) )
                    maxWorkers = 0;
                if ( maxWorkers > 0 )
                {
                    RefPair<int, int> values = null;
                    if ( !workingCountByBuildingType.TryGetValue( building.Prefab.Type, out values ) )
                    {
                        values = new RefPair<int, int>( 0, 0 );
                        workingCountByBuildingType[building.Prefab.Type] = values;
                    }

                    values.RightItem += maxWorkers;
                    values.LeftItem += building.Workers[Profession];

                }
            }

            List<KeyValuePair<BuildingType, RefPair<int, int>>> listCountByBuildingType = workingCountByBuildingType.SortIntoList(
                delegate ( KeyValuePair<BuildingType, RefPair<int, int>> Left, KeyValuePair<BuildingType, RefPair<int, int>> Right )
                {
                    int val = Right.Value.RightItem.CompareTo( Left.Value.RightItem ); //desc
                    if ( val != 0 )
                        return val;
                    val = Right.Value.LeftItem.CompareTo( Left.Value.LeftItem ); //desc
                    if ( val != 0 )
                        return val;
                    return Left.Key.GetDisplayName().CompareTo( Left.Key.GetDisplayName() );
                } );
            #endregion

            if ( listCountByBuildingType.Count > 0 )
            {
                Buffer.Line();
                Buffer.AddLang( "PopulationStatistics_ByBuildingType", ColorTheme.CyanDim );
                Buffer.Position250();
                Buffer.AddLang( "PopulationStatistics_Workers", ColorTheme.CyanDim );
                Buffer.Position400();
                Buffer.AddLang( "PopulationStatistics_Jobs", ColorTheme.CyanDim );

                Buffer.Line();

                foreach ( KeyValuePair<BuildingType, RefPair<int, int>> kv in listCountByBuildingType )
                {
                    Buffer.AddRaw( kv.Key.GetDisplayName() );
                    Buffer.Position250();
                    Buffer.AddRaw( kv.Value.LeftItem.ToStringLargeNumberAbbreviated() );
                    Buffer.Position400();
                    Buffer.AddRaw( kv.Value.RightItem.ToStringLargeNumberAbbreviated() );
                    Buffer.Line();
                }
            }
        }

        public void ProfessionType_WriteDataItemUIXClickedDetails_SubTooltipLinkHover( ProfessionType Profession,
            string[] TooltipLinkData)
        {
            string linkID = TooltipLinkData[0];
            switch ( linkID )
            {
                default:
                    break;
            }
        }
        #endregion

        #region ISimWorld_People
        public DictionaryView<ProfessionType, int> GetAllJobs()
        {
            return DictionaryView<ProfessionType, int>.Create( AllJobs );
        }

        public DictionaryView<EconomicClassType, int> GetResidentialCapacity()
        {
            return DictionaryView<EconomicClassType, int>.Create( ResidentialCapacity );
        }

        public DictionaryView<EconomicClassType, int> GetCurrentResidents()
        {
            return DictionaryView<EconomicClassType, int>.Create( CurrentResidents );
        }

        public DictionaryView<EconomicClassType, int> GetUnemployedResidents()
        {
            return DictionaryView<EconomicClassType, int>.Create( UnemployedResidents );
        }

        public DictionaryView<ProfessionType, int> GetCurrentWorkers()
        {
            return DictionaryView<ProfessionType, int>.Create( CurrentWorkers );
		}
        #endregion

        #region KillResidentsCityWide_BiasedLower
        public void KillResidentsCityWide_BiasedLower( int NumberToKill, RandomGenerator Random )
        {
            int amt = NumberToKill;
            int j = Random.NextInclus( 0, 1 );
            for ( int i = 0; i < 2; i++ )
            {
                int index = (i + j) % EconomicClassTypeTable.Instance.Rows.Length;
                if ( index < 0 )
                    index = 0;
                else if ( index >= CommonRefs.LowerClassResidents.Length )
                    index = CommonRefs.LowerClassResidents.Length - 1;

                EconomicClassType econClass = CommonRefs.LowerClassResidents[index];

                KillResidentsCityWide( econClass, ref amt, Random );
            }
            if ( amt > 0 )
            {
                KillResidentsCityWide( CommonRefs.ManagerialClass, ref amt, Random );
            }
            if ( amt > 0 )
            {
                KillResidentsCityWide( CommonRefs.ScientistClass, ref amt, Random );
            }
            if ( amt > 0 )
            {
                KillResidentsCityWide( CommonRefs.HomelessClass, ref amt, Random );
            }
        }
        #endregion

        public void KillResidentsCityWide(ProfessionType Profession, ref int NumberToKill, RandomGenerator Random )
		{
            foreach ( EconomicClassType econType in Profession.CanBeWorkedBy.GetRandomStartEnumerable( Random ) )
            {
                foreach ( MapDistrict district in CityMap.AllDistricts.GetRandomStartEnumerable( Random ) )
				{
					if (NumberToKill < 1)
                        return; //completely done, excellent
					district.TryGetCachedHousingForResidentType(econType, out int availableHere);
					if (availableHere < 1)
						continue;
					int amtToKill = Random.Next(1, MathA.Min(availableHere, NumberToKill));
					NumberToKill -= amtToKill;
				}
			}
		}

		public void KillResidentsCityWide(EconomicClassType economicClassType, ref int NumberToKill, RandomGenerator Random)
		{
			if (NumberToKill < 1) 
                return;
            foreach ( MapDistrict district in CityMap.AllDistricts.GetRandomStartEnumerable( Random ) )
			{
                if ( NumberToKill < 1 )
                    return;

				district.TryGetCachedHousingForResidentType(economicClassType, out int availableHere);
				if (availableHere < 1)
					continue;
				int amtToKill = Random.Next(1, MathA.Min(availableHere, NumberToKill));
				NumberToKill -= amtToKill;
			}
		}
	}
}

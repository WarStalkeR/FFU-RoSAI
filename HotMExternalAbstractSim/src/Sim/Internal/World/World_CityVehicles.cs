using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Central world data and lookups about the vehicles of the city
    /// </summary>
    internal class World_CityVehicles : ISimWorld_CityVehicles
    {
        public static World_CityVehicles QueryInstance = new World_CityVehicles();

        //
        //Serialized data
        //-----------------------------------------------------
        private static readonly ConcurrentDictionary<Int64, CityVehicle> CityVehiclesByID = ConcurrentDictionary<Int64, CityVehicle>.Create_WillNeverBeGCed( "World_CityVehicles-CityVehiclesByID" );
        public static Int64 LastCityVehicleID = 0;

        //
        //Nonserialized data
        //-----------------------------------------------------
        public static readonly DoubleBufferedList<CityVehicle> AllCityVehicles = DoubleBufferedList<CityVehicle>.Create_WillNeverBeGCed( 30, "World_CityVehicles-AllCityVehicles", 30 );
        public static readonly DoubleBufferedDictionaryOfLists<CityVehicleType, CityVehicle> CityVehiclesByType = DoubleBufferedDictionaryOfLists<CityVehicleType, CityVehicle>.Create_WillNeverBeGCed( 100, 12, "Faction-CityVehiclesByType" );
        
        public static void OnGameClear()
        {
            CityVehiclesByID.Clear();
            LastCityVehicleID = 0;

            AllCityVehicles.ClearAllVersions();
            CityVehiclesByType.ClearAllVersions();
        }

        #region Serialization

        public static void Serialize( ArcenFileSerializer Serializer )
        {
            Serializer.StartObject( "World_CityVehicles" );
            SerializeInner( Serializer );
            Serializer.EndObject( "World_CityVehicles" );
        }

        private static void SerializeInner( ArcenFileSerializer Serializer )
        {
            //already on a World_CityVehicles sub object

            Serializer.AddInt64( "LastCityVehicleID", LastCityVehicleID );

            #region CityVehicles
            foreach ( KeyValuePair<Int64, CityVehicle> kv in CityVehiclesByID )
            {
                if ( kv.Value != null && kv.Value.CityVehicleID > 0 )
                {
                    if ( !kv.Value.GetCanBeSerialized() )
                        continue; //skip those that are not valid for serialization
                    Serializer.StartObject( "CityVehicle" );
                    kv.Value.Serialize( Serializer );
                    Serializer.EndObject( "CityVehicle" );
                }
            }
            #endregion
        }

        public static void Deserialize( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {
            if ( Data.ChildLayersByName.TryGetValue( "World_CityVehicles", out List<DeserializedObjectLayer> CityVehicles ) )
            {
                for ( int i = 0; i < CityVehicles.Count; i++ ) //should only be one anyway
                    DeserializeInner( CityVehicles[i], RandToUse );
            }
        }

        private static void DeserializeInner( DeserializedObjectLayer Data, MersenneTwister RandToUse )
        {
            LastCityVehicleID = Data.GetInt64( "LastCityVehicleID", false );

            #region CityVehicles
            if ( Data.ChildLayersByName.TryGetValue( "CityVehicle", out List<DeserializedObjectLayer> CityVehicles ) )
            {
                for ( int i = 0; i < CityVehicles.Count; i++ )
                    CityVehicle.Deserialize( CityVehicles[i] );
            }
            #endregion
        }
        #endregion

        #region GetCityVehicleDirectByID
        public static CityVehicle GetCityVehicleDirectByID( Int64 CityVehicleID )
        {
            if ( CityVehicleID == -1 )
                return null;
            return CityVehiclesByID[CityVehicleID];
        }
        #endregion

        #region GetCityVehicleWrapperedByID
        public static WrapperedCityVehicle GetCityVehicleWrapperedByID( Int64 CityVehicleID )
        {
            if ( CityVehicleID == -1 )
                return WrapperedCityVehicle.Create( null );
            return WrapperedCityVehicle.Create( CityVehiclesByID[CityVehicleID] );
        }
        #endregion

        #region TryAddCityVehicle
        public static bool TryAddCityVehicle( CityVehicle CityVehicle )
        {
			//ArcenDebugging.LogSingleLine($"Add CityVehicle {CityVehicle.Type.DisplayName}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
            if ( !CityVehiclesByID.TryAdd( CityVehicle.CityVehicleID, CityVehicle ) )
            {
                ArcenDebugging.LogSingleLine( "Tried to add two CityVehicles with the ID " + CityVehicle.CityVehicleID + "!", Verbosity.SilentError );
                return false;
            }

            return true;
        }
        #endregion

        #region RemoveCityVehicle
        public static void RemoveCityVehicle( CityVehicle CityVehicle )
        {
            CityVehiclesByID.TryRemove( CityVehicle.CityVehicleID, 4 );
            CityVehicle.CityVehicleID = -1;
            CityVehicle.ReturnToPool();
        }
        #endregion

        #region CreateNewCityVehicle
        public ISimCityVehicle CreateNewCityVehicle( CityVehicleType Type, Vector3 InitialWorldLocation, RandomGenerator Rand )
        {
            CityVehicle CityVehicle = CityVehicle.CreateNew( Type, Rand );
            CityVehicle.SetSimAndVisWorldLocation( InitialWorldLocation );
            return CityVehicle;
        }
        #endregion

        #region HandlePerSecond_BackgroundThread
        public static void HandlePerSecond_BackgroundThread( MersenneTwister Rand )
        {
            //main sim thread logic for each CityVehicle spawn logic type in general
            foreach ( CityVehicleType vehicleType in CityVehicleTypeTable.Instance.Rows )
                vehicleType.Implementation.DoPerSecondLogic_BackgroundThread( vehicleType, Rand );

            #region Now Update All The Double Buffered Collections
            AllCityVehicles.ClearConstructionListForStartingConstruction();
            CityVehiclesByType.ClearConstructionDictForStartingConstruction();


            foreach ( KeyValuePair<long, CityVehicle> kv in CityVehiclesByID )
            {
                CityVehicle CityVehicle = kv.Value;
                CityVehicleType type = CityVehicle.Type;
                if ( CityVehicle == null || type == null )
                    continue;

                AllCityVehicles.AddToConstructionList( CityVehicle );
                CityVehiclesByType.Construction[type].Add( CityVehicle );
            }

            AllCityVehicles.SwitchConstructionToDisplay();
            CityVehiclesByType.SwitchConstructionToDisplay();
            #endregion

            //main sim thread logic for each specific ephemeral group
            foreach ( CityVehicle CityVehicle in AllCityVehicles.GetDisplayList() )
            {
                CityVehicle.DoPerSecondLogic_BackgroundThread( Rand );
            }
        }
        #endregion

        #region HandlePerQuarterSecond_BackgroundThread
        public static void HandlePerQuarterSecond_BackgroundThread( MersenneTwister Rand )
        {
            if ( !SimCommon.ShouldCityLifeAnimate )
                return; //don't update CityVehicles in this way when sim-paused, because this is mainly about animation and movement

            foreach ( CityVehicle CityVehicle in AllCityVehicles.GetDisplayList() )
                CityVehicle.DoPerQuarterSecondLogic_BackgroundThread( Rand );
        }
        #endregion

        #region DoAnyPerFrameLogic
        public static void DoAnyPerFrameLogic()
        {
            if ( !SimCommon.ShouldCityLifeAnimate )
                return; //don't update CityVehicles in this way when sim-paused, because this is mainly about animation and movement

            foreach ( CityVehicle CityVehicle in AllCityVehicles.GetDisplayList() )
                CityVehicle.DoPerFrameLogic();
        }
        #endregion

        #region ISimWorld_CityVehicles
        public ListView<ISimCityVehicle> GetAllCityVehicles()
        {
            return ListView<ISimCityVehicle>.Create( AllCityVehicles.GetDisplayList() );
        }

        public ListView<ISimCityVehicle> GetCityVehiclesForType( CityVehicleType Type )
        {
            return ListView<ISimCityVehicle>.Create( CityVehiclesByType.Display[Type] );
        }

        public ISimCityVehicle GetCityVehicleByID( Int64 ID )
        {
            if ( ID == -1 )
                return null;
            return CityVehiclesByID[ID];
        }
        #endregion

        #region CheckForCityVehiclesOfType
        public bool CheckForCityVehiclesOfType( CityVehicleType Type )
        {
            if ( CityVehiclesByType.GetDisplayDict().GetListForOrNull( Type )?.Exists( enc =>
            {
                return true;
            } ) ?? false ) return true;

            bool exist = false;
            foreach ( KeyValuePair<long, CityVehicle> kvp in CityVehiclesByID )
            {
                if ( kvp.Value.Type != Type ) 
                    continue;

                exist = true;
                break;
            }

            return exist;
        }
        #endregion

        public void DisbandAllCityVehiclesOfType( CityVehicleType Type )
        {
            foreach ( KeyValuePair<long, CityVehicle> kvp in CityVehiclesByID )
            {
                if ( kvp.Value.Type == Type )
                    kvp.Value.Disband( true );
            }
        }
    }
}

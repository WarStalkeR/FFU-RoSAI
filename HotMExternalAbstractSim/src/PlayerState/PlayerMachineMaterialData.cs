 using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.External
{
    internal class PlayerMachineMaterialData : ConcurrentPoolable<PlayerMachineMaterialData>, IProtectedListable, ISimMachineMaterialTypeData
    {
        public MachineMaterialType Type { get; private set; }

        /// <summary> Current max amount we are allowed to have of this kind of material at this time.</summary>
        public int MaxCapacity { get { return this._maxCapacity; } }

        /// <summary> Current amount of this material that is on hand and actually ready for use.</summary>
        public int TotalCreatedAndReady { get { return this._totalCreatedAndReady; } }

        /// <summary> Current amount of this resource that is income per turn, if any.</summary>
        public int CurrentPerTurnIncomeRate { get { return this._currentPerTurnIncomeRate; } }

        /// <summary> Current amount of this resource that is created per turn, if any, ignoring resource conversion shortfalls.</summary>
        public float CurrentIdealPerTurnCreated { get { return this._currentIdealPerTurnCreated; } }

        /// <summary> Current amount of this resource that is created per turn, if any, including resource conversion factors.</summary>
        public float CurrentActualPerTurnCreated { get { return this._currentActualPerTurnCreated; } }

        /// <summary> If this is something that we gain over time by computing resources, what is our progress level on that?</summary>
        public int ProgressTowardNextGained { get { return this._progressTowardNextGained; } }

        private int _maxCapacity = 0;
        private int _totalCreatedAndReady = 0;
        private int _currentPerTurnIncomeRate = 0;
        private float _currentIdealPerTurnCreated = 0;
        private float _currentActualPerTurnCreated = 0;
        private int _progressTowardNextGained = 0;

        public void ClearData()
        {
            this.Type = null;

            this._maxCapacity = 0;
            this._totalCreatedAndReady = 0;
            this._currentPerTurnIncomeRate = 0;
            this._currentIdealPerTurnCreated = 0;
            this._currentActualPerTurnCreated = 0;
            this._progressTowardNextGained = 0;
        }

        #region Pooling
        private static ReferenceTracker RefTracker;
        private PlayerMachineMaterialData()
        {
            if ( RefTracker == null )
                RefTracker = new ReferenceTracker( "PlayerMachineMaterialDatas" );
            RefTracker.IncrementObjectCount();
        }

        private static readonly ConcurrentPool<PlayerMachineMaterialData> Pool = new ConcurrentPool<PlayerMachineMaterialData>( "PlayerMachineMaterialData",
             KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnXmlReload, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PlayerMachineMaterialData(); } );

        public static PlayerMachineMaterialData GetFromPoolOrCreate( MachineMaterialType Type )
        {
            PlayerMachineMaterialData res = Pool.GetFromPoolOrCreate();
            res.Type = Type;
            return res;
        }

        public override void DoEarlyCleanupWhenGoingBackIntoPool()
        {
            this.ClearData();
        }

        public override void DoAnyBelatedCleanupWhenComingOutOfPool()
        {
        }

        public void DoBeforeRemoveOrClear()
        {
            this.ReturnToPool(); //when I am remove from a list, put me back in the pool
        }

        public void ReturnToPool()
        {
            Pool.ReturnToPool( this );
        }
        #endregion

        public void SetCapacity( int NewCapacity )
        {
            this._maxCapacity = NewCapacity;
        }

        public int AlterCapacity( int AddedCapacity )
        {
            return Interlocked.Add( ref this._maxCapacity, AddedCapacity );
        }

        public void SetCreatedAndReady( int NewCreatedAndReady )
        {
            this._totalCreatedAndReady = NewCreatedAndReady;
        }

        public int AlterCreatedAndReady( int AddedCreatedAndReady )
        {
            return Interlocked.Add( ref this._totalCreatedAndReady, AddedCreatedAndReady );
        }

        public void SetCurrentPerTurnIncomeRate( int NewPerTurnIncomeRate )
        {
            this._currentPerTurnIncomeRate = NewPerTurnIncomeRate;
        }

        public void SetCurrentIdealPerTurnCreated( float NewPerTurnCreated )
        {
            this._currentIdealPerTurnCreated = NewPerTurnCreated;
        }

        public void SetCurrentActualPerTurnCreated( float NewPerTurnCreated )
        {
            this._currentActualPerTurnCreated = NewPerTurnCreated;
        }

        public void SetProgressTowardNextGained( int NewProgressTowardNextGained )
        {
            this._progressTowardNextGained = NewProgressTowardNextGained;
        }

        public void SerializeData( ArcenFileSerializer Serializer )
        {
            //don't bother serializing Type, as we already did that outside of here

            Serializer.AddInt32IfGreaterThanZero( "Capacity", this._maxCapacity );
            Serializer.AddInt32IfGreaterThanZero( "CreatedAndReady", this._totalCreatedAndReady );
            Serializer.AddInt32IfGreaterThanZero( "PerTurnIncomeRate", this._currentPerTurnIncomeRate );
            Serializer.AddFloat( "IdealPerTurnCreated", this._currentIdealPerTurnCreated );
            Serializer.AddFloat( "ActualPerTurnCreated", this._currentActualPerTurnCreated );
            Serializer.AddInt32IfGreaterThanZero( "ProgressTowardNextGained", this._progressTowardNextGained );
        }

        public void DeserializeData( DeserializedObjectLayer PlayerMachineMaterialDataData )
        {
            this._maxCapacity = PlayerMachineMaterialDataData.GetInt32( "Capacity", false );
            this._totalCreatedAndReady = PlayerMachineMaterialDataData.GetInt32( "CreatedAndReady", false );
            this._currentPerTurnIncomeRate = PlayerMachineMaterialDataData.GetInt32( "PerTurnIncomeRate", false );
            this._currentIdealPerTurnCreated = PlayerMachineMaterialDataData.GetFloat( "IdealPerTurnCreated", false );
            this._currentActualPerTurnCreated = PlayerMachineMaterialDataData.GetFloat( "ActualPerTurnCreated", false );
            this._progressTowardNextGained = PlayerMachineMaterialDataData.GetInt32( "ProgressTowardNextGained", false );
        }
    }
}

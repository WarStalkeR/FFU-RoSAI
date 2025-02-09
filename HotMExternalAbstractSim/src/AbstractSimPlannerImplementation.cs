using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using System.Diagnostics;

namespace Arcen.HotM.External
{
    /// <summary>
    /// These items are run on various threads, and help us wire together various dlls into being able to call to each other
    /// </summary>
    public class AbstractSimPlannerImplementation : AbstractSimPlanner
    {
        private readonly MersenneTwister deserializationRand = new MersenneTwister( 0 );
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return; //do nothing in the level editor!

            //start main set: this is done in this order as much as possible
            World_People.OnGameClear();
            World_Buildings.OnGameClear();
            World_Misc.OnGameClear();
            World_CityVehicles.OnGameClear();
            World_Forces.OnGameClear();
            //World_CityNetwork.OnGameClear();
            //end main set

            SimLoading.OnGameClear();
            SimPerTurn.OnGameClear();
            SimPerFullSecond.OnGameClear();
            SimPerQuarterSecond.OnGameClear();
            Helper_AssignFactionWorkers.OnGameClear();

            deserializationRand.ReinitializeWithSeed( Engine_Universal.PermanentQualityRandom.Next() );
        }

        public AbstractSimPlannerImplementation()
        {
            AbstractSimPlanner.Instance = this;
        }

        private readonly Stopwatch perFrameStopwatch = new Stopwatch();
        public override void DoPerFrame()
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return;
            if ( CityMap.GetIsMapStillGeneratingForSimPurposes() )
                return;

            //the per-frame bits of abstract sim

            perFrameStopwatch.Restart();
            World_CityVehicles.DoAnyPerFrameLogic();

            World_Forces.DoAnyPerFrameLogic();
            World_Buildings.DoAnyPerFrameLogic();

            if ( SimCommon.IsReadyToRunNextLogicForTurn > SimCommon.Turn &&
                !VisCurrent.ShouldDrawLoadingMenuBuildings && //must have fully generated the game
                SimCommon.HasFullyStarted ) //and must have fully initialized the starting state
            {
                if ( SimPerTurn.TryHandleNPCsShootingIfReadyToMoveToNextTurn() )
                    SimPerTurn.TryRunNextTurn(); //if have finished any NPC shooting, then actually run the next turn
            }
            else
                SimPerTurn.DrawDownNPCWaitingToShootList();

            SimTimingInfo.SimMainThreadPerFrameCalculations.LogCurrentTicks( (int)perFrameStopwatch.ElapsedTicks );
        }

        public override void DoPerFullSecond_BackgroundThread( MersenneTwister Rand )
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return;
            DoPerFullSecond_MainGame( Rand );

            //this stuff happens once per second but on the main thread
            World_CityVehicles.HandlePerSecond_BackgroundThread( Rand );
        }

        public override void DoPerQuarterSecond_BackgroundThread( MersenneTwister Rand )
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return;
            DoPerQuarterSecond_MainGame( Rand );

            SimCommon.HandlePerQuarterSecond_BackgroundThread( Rand );
            World_CityVehicles.HandlePerQuarterSecond_BackgroundThread( Rand );
        }

        #region DoPerFullSecond_MainGame
        private void DoPerFullSecond_MainGame( MersenneTwister Rand )
        {
            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //we don't want this doing anything until the map tiles are all in!

            SimPerFullSecond.DoPerFullSecondLogic( Rand );
        }
        #endregion

        #region DoPerQuarterSecond_MainGame
        private void DoPerQuarterSecond_MainGame( MersenneTwister Rand )
        {
            if ( CityMap.IsCurrentlyAddingMoreMapTiles > 0 )
                return; //we don't want this doing anything until the map tiles are all in!

            SimPerQuarterSecond.DoPerQuarterSecondLogic_BackgroundThread( Rand );
        }
        #endregion

        public void ResetMapForEntities()
        {
        }

        /// <summary>
        /// In the level editor, this means we just loaded a level.  In the regular game, this has a not very well defined meaning yet.
        /// </summary>
        public override void OnAllContentHasLoaded()
        {
        }

        /// <summary>
        /// This is right after the player has clicked the "play" button on the main menu.
        /// Right now we're on the main thread, and being told about that.
        /// </summary>
        public override void OnMainGameStarted()
        {
            //this is happening on the main thread

            SimCommon.InitializeFromMainThread();
        }

        public override void ClearAllObjectsBecauseOfUnload()
        {
            ClearAllMyDataForQuitToMainMenuOrBeforeNewMap(); //these are a bit duplicative.  Call one from the other
        }

        public override void SerializeData( ArcenFileSerializer Serializer )
        {
            SimSerialization.SerializeData( Serializer );
        }

        public override void DeserializeData( DeserializedObjectLayer Data )
        {
            deserializationRand.ReinitializeWithSeed( Engine_Universal.PermanentQualityRandom.Next() );
            SimSerialization.DeserializeData( Data, deserializationRand );
        }
    }
}

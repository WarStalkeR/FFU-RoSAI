using System;



using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.Universal.Deserialization;

namespace Arcen.HotM.External
{
    /// <summary>
    /// These items are run on various threads, and help us wire together various dlls into being able to call to each other
    /// </summary>
    internal class CommonPlannerImplementation : CommonPlanner
    {
        public override void ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
            SimTimingInfo.Clear();
            BuildingDestroyer.OnGameClear();
        }

        public CommonPlannerImplementation()
        {
            CommonPlanner.Instance = this;
        }
        public override void DoPerFrame()
        {
            WorldTileInnerPopulator_Structural.DoPerFrame();
            WorldTileInnerPopulator_Decorations.DoPerFrame();
            WorldTileInnerPopulator_SubCells.DoPerFrame();
            BuildingDestroyer.DoPerFrame();
        }

        /// <summary>
        /// This only happens when the game is unpaused!
        /// </summary>
        public override void DoPerFullSecond_BackgroundThread( MersenneTwister Rand )
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return;
        }

        public override void DoPerQuarterSecond_BackgroundThread( MersenneTwister Rand )
        {
            if ( Engine_Universal.GameLoop.IsLevelEditor )
                return;
        }

        /// <summary>
        /// In the level editor, this means we just loaded a level.  In the regular game, this has a not very well defined meaning yet.
        /// </summary>
        public override void OnAllContentHasLoaded()
        {
            bool debug = false;
            if ( debug )
                MathTests.DoMatrixTests();
        }

        /// <summary>
        /// This is right after the player has clicked the "play" button on the main menu.
        /// Right now we're on the main thread, and being told about that.
        /// </summary>
        public override void OnMainGameStarted()
        {
            if ( CityMap.WasMapLoadedFromSavegame )
                return; //if we're loading a savegame, then don't try to run this other logic!

            bool debugMode = false;
            if ( debugMode )
            {
                CityMapPopulator.AddDebugMapTiles();
                return;
            }

            CityStyle style = SimCommon.CurrentTimeline?.SeedStyle;
            if ( style == null )
                style = CityStyleTable.Instance.DefaultRow;
            if ( style == null )
                style = CityStyleTable.Instance.Rows[0];

            CityMapPopulator.SeedInitialGridOfTiles( style );
        }

        public override void ClearAllObjectsBecauseOfUnload()
        {
            ClearAllMyDataForQuitToMainMenuOrBeforeNewMap(); //these are a bit duplicative.  Call one from the other
        }

        public override void SerializeData( ArcenFileSerializer Serializer )
        {
        }

        public override void DeserializeData( DeserializedObjectLayer Data )
        {
        }
    }
}

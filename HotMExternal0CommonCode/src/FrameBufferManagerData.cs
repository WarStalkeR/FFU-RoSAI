using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System.Diagnostics;
using DiffLib;

namespace Arcen.HotM.External
{
    public static class FrameBufferManagerData
    {
        public static DoubleBufferedValue<int> AnimatedObjectCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> StreetMobCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> FadingCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> ParticleCount = new DoubleBufferedValue<int>( 0 );

        public static DoubleBufferedValue<int> BuildingMainCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> BuildingOverlayCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> MajorDecorationCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> MinorDecorationCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> RoadCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> OtherSkeletonItemCount = new DoubleBufferedValue<int>( 0 );

        public static DoubleBufferedValue<int> SelectedLOD = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<float> SelectedLODDistance = new DoubleBufferedValue<float>( 0 );
        public static DoubleBufferedValue<int> LOD0Count = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> LOD1Count = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> LOD2Count = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> LOD3Count = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> LOD4Count = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> LODCullCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> IndividualFrustumCullCount = new DoubleBufferedValue<int>( 0 );
        public static DoubleBufferedValue<int> CellFrustumCullCount = new DoubleBufferedValue<int>( 0 );

        #region Start/Finish Frame Buffers
        public static void StartFrameBuffers()
        {
            AnimatedObjectCount.ClearConstructionValueForStartingConstruction();
            StreetMobCount.ClearConstructionValueForStartingConstruction();
            FadingCount.ClearConstructionValueForStartingConstruction();
            ParticleCount.ClearConstructionValueForStartingConstruction();

            BuildingMainCount.ClearConstructionValueForStartingConstruction();
            BuildingOverlayCount.ClearConstructionValueForStartingConstruction();
            MajorDecorationCount.ClearConstructionValueForStartingConstruction();
            MinorDecorationCount.ClearConstructionValueForStartingConstruction();
            RoadCount.ClearConstructionValueForStartingConstruction();
            OtherSkeletonItemCount.ClearConstructionValueForStartingConstruction();

            SelectedLOD.ClearConstructionValueForStartingConstruction();
            SelectedLOD.Construction = -99; //this is an unusual one

            SelectedLODDistance.ClearConstructionValueForStartingConstruction();
            SelectedLODDistance.Construction = -99; //this is an unusual one

            LOD0Count.ClearConstructionValueForStartingConstruction();
            LOD1Count.ClearConstructionValueForStartingConstruction();
            LOD2Count.ClearConstructionValueForStartingConstruction();
            LOD3Count.ClearConstructionValueForStartingConstruction();
            LOD4Count.ClearConstructionValueForStartingConstruction();
            LODCullCount.ClearConstructionValueForStartingConstruction();
            IndividualFrustumCullCount.ClearConstructionValueForStartingConstruction();
            CellFrustumCullCount.ClearConstructionValueForStartingConstruction();
        }

        public static void FinishFrameBuffers()
        {
            AnimatedObjectCount.SwitchConstructionToDisplay();
            StreetMobCount.SwitchConstructionToDisplay();
            FadingCount.SwitchConstructionToDisplay();
            ParticleCount.SwitchConstructionToDisplay();

            BuildingMainCount.SwitchConstructionToDisplay();
            BuildingOverlayCount.SwitchConstructionToDisplay();
            MajorDecorationCount.SwitchConstructionToDisplay();
            MinorDecorationCount.SwitchConstructionToDisplay();
            RoadCount.SwitchConstructionToDisplay();
            OtherSkeletonItemCount.SwitchConstructionToDisplay();

            SelectedLOD.SwitchConstructionToDisplay();
            SelectedLODDistance.SwitchConstructionToDisplay();
            LOD0Count.SwitchConstructionToDisplay();
            LOD1Count.SwitchConstructionToDisplay();
            LOD2Count.SwitchConstructionToDisplay();
            LOD3Count.SwitchConstructionToDisplay();
            LOD4Count.SwitchConstructionToDisplay();
            LODCullCount.SwitchConstructionToDisplay();
            IndividualFrustumCullCount.SwitchConstructionToDisplay();
            CellFrustumCullCount.SwitchConstructionToDisplay();
        }
        #endregion
    }
}

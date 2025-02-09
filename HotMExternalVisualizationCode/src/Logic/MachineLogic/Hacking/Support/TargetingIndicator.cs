using Arcen.Universal;
using System;
using System.Diagnostics;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.ExternalVis;

namespace Arcen.HotM.ExternalVis.Hacking
{
    using h = Window_Hacking;
    using scene = HackingScene;

    public class TargetingIndicator //let this be GC'd
    {
        public PlayerShard LastHacker;
        public h.hCell LastStartingCell;
        public h.hCell LastFinalCell;
        public Vector3 Ending;
        public h.iIndicatorTargetingB MainVisuals;
        public h.iIndicatorWater SplitVisuals;
        public TargetingIndicatorType LastIndicator;

        public bool IsMoving = false;
        public float MoveSpeedMultiplier = 1f;
        private Vector3 location;

        public void StartFreshMovement( PlayerShard RelatedHacker, h.hCell StartingCell, h.hCell FinalCell, TargetingIndicatorType Indicator )
        {
            if ( LastHacker == RelatedHacker && LastStartingCell == StartingCell && LastFinalCell == FinalCell )
            {
                if ( Indicator != LastIndicator )
                {
                    InitializeFromIndicator( Indicator );
                    SetIndicatorPositionFromLocal();
                }
                return;  //nothing to do, as it was already done
            }

            this.Ending = FinalCell.CenterPos;

            InitializeFromIndicator( Indicator );

            bool setVal = false;
            if ( LastHacker != RelatedHacker )
            {
                location = StartingCell.CenterPos;
                setVal = true;
            }

            LastHacker = RelatedHacker;
            LastStartingCell = StartingCell;
            LastFinalCell = FinalCell;
            this.IsMoving = true;
            this.MoveSpeedMultiplier = (location - this.Ending).magnitude / 160f;
            if ( this.MoveSpeedMultiplier < 0.8f )
                this.MoveSpeedMultiplier = 0.8f;

            if ( setVal )
                SetIndicatorPositionFromLocal();
        }

        private void InitializeFromIndicator( TargetingIndicatorType Indicator )
        {
            switch ( Indicator )
            {
                case TargetingIndicatorType.Main:
                    if ( this.SplitVisuals != null )
                    {
                        this.SplitVisuals.ReturnToParentPool();
                        this.SplitVisuals = null;
                    }
                    if ( this.MainVisuals == null )
                        this.MainVisuals = h.iIndicatorTargetingBPool.GetOrAddEntry();
                    break;
                case TargetingIndicatorType.Split:
                    if ( this.SplitVisuals == null )
                        this.SplitVisuals = h.iIndicatorWaterPool.GetOrAddEntry();
                    if ( this.MainVisuals != null )
                    {
                        this.MainVisuals.ReturnToParentPool();
                        this.MainVisuals = null;
                    }
                    break;
            }
            LastIndicator = Indicator;
        }

        private void SetIndicatorPositionFromLocal()
        {
            if ( this.MainVisuals != null )
                this.MainVisuals.Trans.localPosition = location;
            if ( this.SplitVisuals != null )
                this.SplitVisuals.Trans.localPosition = location;

            h.iIndicatorBorder.Instance.DrawBetween( location, LastStartingCell.CenterPos, LastStartingCell.ArrayY == LastFinalCell.ArrayY );
        }

        public void ClearExistingData()
        {
            if ( this.MainVisuals != null )
            {
                this.MainVisuals.ReturnToParentPool();
                this.MainVisuals = null;
            }

            if ( this.SplitVisuals != null )
            {
                this.SplitVisuals.ReturnToParentPool();
                this.SplitVisuals = null;
            }

            this.LastHacker = null;
            this.LastStartingCell = null;
            this.LastFinalCell = null;
            this.IsMoving = false;

            h.iIndicatorBorder.Instance.WantsToDraw = false;
        }

        public void DoAnyMovement()
        {
            if ( !this.IsMoving )
                return;

            location = MathA.MoveTowards( location, Ending,
                MathRefs.PlayerHackerTargetingMoveSpeed.FloatMin * ArcenTime.AnyDeltaTime * this.MoveSpeedMultiplier, out bool hasReachedDestination );
            SetIndicatorPositionFromLocal();

            if ( hasReachedDestination )
                this.IsMoving = false;
        }
    }

    public enum TargetingIndicatorType
    {
        Main,
        Split
    }
}

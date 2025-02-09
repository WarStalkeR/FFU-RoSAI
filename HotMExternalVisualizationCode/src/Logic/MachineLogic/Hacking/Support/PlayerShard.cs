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

    public class PlayerShard : IHEntity //let this be GC'd
    {
        public h.hCell CurrentCell;
        public h.iHacker Visuals;

        private h.hCell MovingToTargetCell = null;
        private bool MarkAsMovedOnFinish = false;

        public void InitializeAtCell( h.hCell Cell, int StartingHealth )
        {
            this.CurrentCell = Cell;
            Cell.CurrentEntity = this;
            Cell.SetCurrentNumber( StartingHealth );

            if ( this.Visuals == null )
                this.Visuals = h.iHackerPool.GetOrAddEntry();
            this.Visuals.Trans.localPosition = this.CurrentCell.CenterPos;
        }

        public void ClearVisualData()
        {
            if ( this.Visuals != null )
            {
                this.Visuals.ReturnToParentPool();
                this.Visuals = null;
            }
        }

        public void MoveTo( h.hCell Moving, bool MarkAsMovedOnFinish )
        {
            this.MovingToTargetCell = Moving;
            this.MarkAsMovedOnFinish= MarkAsMovedOnFinish;
            scene.AnimatingItems.Add( this );
        }

        public bool DoAnyMovement()
        {
            if ( this.CurrentCell == null )
                return true; //must be done

            if ( this.Visuals == null )
                this.Visuals = h.iHackerPool.GetOrAddEntry();
            Visuals.Trans.localPosition = MathA.MoveTowards( Visuals.Trans.localPosition, MovingToTargetCell.CenterPos,
                MathRefs.PlayerHackerMoveSpeed.FloatMin * ArcenTime.AnyDeltaTime, out bool hasReachedDestination );
            if ( hasReachedDestination )
            {
                if ( this.CurrentCell?.CurrentEntity == this )
                    this.CurrentCell.CurrentEntity = null;
                this.MovingToTargetCell.CurrentEntity = this;
                this.CurrentCell = this.MovingToTargetCell;
                this.MovingToTargetCell = null;

                ParticleSoundRefs.Hacking_Run_End.DuringGame_PlayAtUILocation( this.CurrentCell.CalculateScreenPos() );

                if ( this.MarkAsMovedOnFinish )
                {
                    this.MarkAsMovedOnFinish = false;
                    scene.IsDaemonTurnToMove = true;
                }
            }
            return hasReachedDestination;
        }

        public void DestroyEntity()
        {
            this.ClearVisualData();
            if ( this.CurrentCell?.CurrentEntity == this )
                this.CurrentCell.CurrentEntity = null;

            scene.PlayerShards.Remove( this );
            scene.AnimatingItems.Remove( this );
        }
    }
}

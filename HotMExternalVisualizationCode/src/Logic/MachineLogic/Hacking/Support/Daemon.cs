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

    public class Daemon : IHEntity //let this be GC'd
    {
        public h.hCell CurrentCell;
        public h.iParticle Visuals;
        public HackingDaemonType DaemonType;
        public bool IsHidden = false;

        public int AdditionalMovesToSkip = 0;

        private h.hCell MovingToTargetCell = null;

        public void InitializeAtCell( h.hCell Cell, HackingDaemonType DaemonType, bool IsHidden )
        {
            this.CurrentCell = Cell;
            Cell.CurrentEntity = this;

            if ( DaemonType.ForcesNumericSubstrateTo > 0 )
                Cell.SetCurrentNumber( DaemonType.ForcesNumericSubstrateTo );

            this.DaemonType = DaemonType;
            this.IsHidden = IsHidden;

            this.RefreshVisuals();
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

        public void MoveTo( h.hCell Moving )
        {
            this.MovingToTargetCell = Moving;
            scene.AnimatingItems.Add( this );
        }

        public void RefreshVisuals()
        {
            if ( this.Visuals == null )
                this.Visuals = h.iParticlePool.GetOrAddEntry();
            this.Visuals.SetIconDataAsNeeded(
                //icon
                this.IsHidden ? this.DaemonType.HiddenIcon : this.DaemonType.VisibleIcon,
                //color
                this.IsHidden ? this.DaemonType.HiddenColor : ( this.AdditionalMovesToSkip > 0 && this.MovingToTargetCell == null ? this.DaemonType.VisibleIdleColor : this.DaemonType.VisibleColor ),
                //scale
                this.IsHidden ? this.DaemonType.HiddenScale : this.DaemonType.VisibleScale,
                //intensity
                this.IsHidden ? this.DaemonType.HiddenHDRIntensity : (this.AdditionalMovesToSkip > 0 && this.MovingToTargetCell == null ? this.DaemonType.VisibleIdleHDRIntensity : this.DaemonType.VisibleHDRIntensity) );
        }

        public bool DoAnyMovement()
        {
            if ( this.CurrentCell == null || this.MovingToTargetCell == null )
                return true; //must be done

            this.RefreshVisuals();
            Visuals.Trans.localPosition = MathA.MoveTowards( Visuals.Trans.localPosition, MovingToTargetCell.CenterPos,
                MathRefs.OtherHackerEntityMoveSpeed.FloatMin * ArcenTime.AnyDeltaTime, out bool hasReachedDestination );
            if ( hasReachedDestination )
            {
                if ( this.CurrentCell?.CurrentEntity == this )
                    this.CurrentCell.CurrentEntity = null;

                IHEntity entityAtDest = this.MovingToTargetCell.CurrentEntity;                

                this.MovingToTargetCell.CurrentEntity = this;
                this.CurrentCell = this.MovingToTargetCell;
                this.MovingToTargetCell = null;

                ParticleSoundRefs.Hacking_DaemonMove_End.DuringGame_PlayAtUILocation( this.CurrentCell.CalculateScreenPos() );

                if ( entityAtDest != null )
                {
                    if ( entityAtDest is PlayerShard shard )
                    {
                        try
                        {
                            this.DaemonType.Implementation.TryHandleDaemonLogic( this.DaemonType, this, shard, HackingDaemonLogic.HitPlayerShard );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Exception in HackingDaemonLogic.HitPlayerShard: " + e, Verbosity.ShowAsError );
                        }
                    }
                    else if ( entityAtDest is Daemon otherDaemon )
                    {
                        try
                        {
                            otherDaemon.DaemonType.Implementation.TryHandleDaemonLogic( otherDaemon.DaemonType, otherDaemon, this, HackingDaemonLogic.HitByOtherDaemon );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogSingleLine( "Exception in HackingDaemonLogic.HitByOtherDaemon: " + e, Verbosity.ShowAsError );
                        }
                    }
                }
                if ( this.DaemonType != null )
                    this.RefreshVisuals();
            }
            return hasReachedDestination;
        }

        public void DestroyEntity()
        {
            this.ClearVisualData();
            if ( this.CurrentCell?.CurrentEntity == this )
                this.CurrentCell.CurrentEntity = null;

            scene.Daemons.Remove( this );
            scene.AnimatingItems.Remove( this );
        }
    }
}

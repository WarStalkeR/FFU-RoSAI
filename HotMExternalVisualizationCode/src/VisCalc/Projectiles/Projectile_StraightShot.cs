using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class Projectile_StraightShot : IVisProjectileMovementLogicImplementation
    {
        public void CreateMovingProjectiles( IProjectileLowLevelParticleSpawner LowLevelParticleSpawner, VisParticleEffect ParticleEffect, 
            VisParticleAndSoundUsage FullParticleDefinition, Vector3 Location,
            Vector3 Rotation, Vector3 Scale, ProjectileInstructionPacket ProjectilePacket )
        {
            StraightShot shot = pool.GetFromPoolOrCreate();
            if ( shot != null )
            {
                ParticleInstance effectThatWasSpawned = LowLevelParticleSpawner.CreateProjectileLowLevelParticle( shot, true, true );
                if ( effectThatWasSpawned.Transform != null )
                    shot.Initialize( effectThatWasSpawned, ProjectilePacket, ParticleEffect, FullParticleDefinition, Location, Rotation, Scale,
                        ProjectilePacket.PrimaryTargetPoint, ProjectilePacket.TravelSpeed, ProjectilePacket.OnFinalHit );
                else
                    pool.ReturnToPool( shot );
            }
        }

        private static ConcurrentPool<StraightShot> pool = new ConcurrentPool<StraightShot>( "Projectile_StraightShot-pool", 
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            () => new StraightShot() );


        #region class StraightShot
        private class StraightShot : ConcurrentPoolable<StraightShot>, IVisParticleData
        {
            public ParticleInstance Particle;
            public VisParticleAndSoundUsage FullParticleDefinition;
            public ProjectileInstructionPacket Packet;
            public Vector3 CurrentPoint;
            public Vector3 OriginalScale;
            public Vector3 TargetPoint;
            public float TravelSpeed;
            public float ShrinkMultiplierSpeed;
            public float SpeedReductionAfterImpact;
            public float ExpiresAtTime;
            public float TimeToLastAfterDestination;
            public float HighestPriorSquareMagnitude = 100000;
            public bool HasReachedDestination;
            public bool StopAtDestination;
            public Action OnComplete;
            private bool HasDoneFirstFrame = false;
            private Vector3 OriginalForward;

            public VisParticleEffect ParentParticleEffect;
            public VisParticleEffect GetParentRow() => ParentParticleEffect;

            public ParticleInstance GetParticle() => Particle;

            public override void DoEarlyCleanupWhenGoingBackIntoPool()
            {
                this.Particle = new ParticleInstance();
                this.FullParticleDefinition = null;
                this.OriginalScale = Vector3.zero;
                this.CurrentPoint = Vector3.zero;
                this.TargetPoint = Vector3.zero;
                this.TravelSpeed = 0;
                this.ShrinkMultiplierSpeed = 0;
                this.SpeedReductionAfterImpact = 0;
                this.ExpiresAtTime = 0;
                this.TimeToLastAfterDestination = 0;
                this.HighestPriorSquareMagnitude = 100000;
                this.StopAtDestination = false;
                this.HasReachedDestination = false;
                this.OnComplete = null;
                this.ParentParticleEffect = null;
                this.HasDoneFirstFrame = false;
                this.OriginalForward = Vector3.zero;
            }

            #region Pooling
            public override void DoAnyBelatedCleanupWhenComingOutOfPool()
            {
            }

            public void DoOnParticleCleaningUp()
            {
                pool.ReturnToPool( this );
            }
            #endregion

            public void Initialize( ParticleInstance EffectThatWasSpawned, ProjectileInstructionPacket Packet, VisParticleEffect ParticleEffect,
                VisParticleAndSoundUsage FullParticleDefinition, Vector3 Location, Vector3 Rotation, Vector3 Scale,
                Vector3 Target, float TravelSpeed, Action OnComplete )
            {
                this.Particle = EffectThatWasSpawned;
                this.FullParticleDefinition = FullParticleDefinition;
                this.Packet = Packet;
                this.OriginalScale = this.Particle.Transform.localScale;
                this.CurrentPoint = Location;
                this.TargetPoint = Target;

                this.ShrinkMultiplierSpeed = Packet.MovementLogic.Float6;

                if ( FullParticleDefinition != null && FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact > 0 )
                    this.ShrinkMultiplierSpeed *= FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact;

                this.SpeedReductionAfterImpact = Packet.MovementLogic.Float1;

                this.TravelSpeed = TravelSpeed;
                this.ExpiresAtTime = ArcenTime.UnpausedTimeSinceStartF + 4f; //no shots last more than 4 seconds
                if ( ParticleEffect.TimeToLastAfterDestination > 0 )
                    this.TimeToLastAfterDestination = ParticleEffect.TimeToLastAfterDestination  + Packet.MovementLogic.Float2;
                else
                    this.TimeToLastAfterDestination = Packet.MovementLogic.Float2;
                this.OnComplete = OnComplete;
                this.ParentParticleEffect = ParticleEffect;

                this.StopAtDestination = Packet.MovementLogic.Int1 > 0;

                //only do this once, since we're traveling in a straight line anyway!
                this.RotateToFaceTargetFrom( this.CurrentPoint );

                this.OriginalForward = this.Particle.Transform.forward;
            }

            #region RotateToFaceTargetFrom
            public void RotateToFaceTargetFrom( Vector3 currentPoint )
            {
                Vector3 targetLook = this.TargetPoint - currentPoint;

                if ( targetLook.sqrMagnitude <= 0.1f )
                    return; //if the range is really short, don't rotate us; it will give us invalid rotations anyway, and cause error cascades

                Quaternion lookRot = Quaternion.LookRotation( targetLook, Vector3.up );

                this.Particle.Transform.rotation = lookRot;
            }
            #endregion

            public bool DoPerFrameUpdate( float DeltaTime )
            {
                if ( ArcenTime.UnpausedTimeSinceStartF >= this.ExpiresAtTime )
                {
                    if ( !this.HasReachedDestination )
                    {
                        if ( this.OnComplete != null )
                            this.OnComplete();
                        if ( this.FullParticleDefinition?.OnHit != null )
                            this.FullParticleDefinition.OnHit.DuringGame_PlayAtLocationWithAreaMultiplier( this.CurrentPoint,
                                new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ), this.Packet.AreaRange, false );
                    }
                    return true;
                }

                if ( !this.HasDoneFirstFrame )
                {
                    this.HasDoneFirstFrame = true;
                    return false;
                }

                if ( !this.HasReachedDestination || !this.StopAtDestination )
                {
                    //move toward point
                    float moveSpeed = this.TravelSpeed * DeltaTime;
                    this.CurrentPoint += this.OriginalForward * moveSpeed;
                    this.Particle.Transform.position = this.CurrentPoint;

                    Vector3 dist = (this.TargetPoint - this.CurrentPoint);

                    float squareMag = dist.sqrMagnitude;
                    if ( squareMag < 0.1f || squareMag > this.HighestPriorSquareMagnitude )
                    {
                        if ( !this.HasReachedDestination )
                        {
                            this.HasReachedDestination = true;

                            if ( this.OnComplete != null )
                                this.OnComplete();
                            if ( this.FullParticleDefinition?.OnHit != null )
                                this.FullParticleDefinition.OnHit.DuringGame_PlayAtLocationWithAreaMultiplier( this.CurrentPoint,
                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ), this.Packet.AreaRange, false );
                        }
                    }
                    else
                        this.HighestPriorSquareMagnitude = squareMag;

                    if ( this.HasReachedDestination )
                    {
                        this.TravelSpeed -= (this.SpeedReductionAfterImpact * DeltaTime);
                        if ( this.TravelSpeed <= 0f )
                        {
                            this.TravelSpeed = 0f;
                            this.StopAtDestination = true;
                        }
                    }
                }

                //DrawHelper.RenderLine( this.CurrentPoint, this.TargetPoint, ColorMath.Green );
                //DrawHelper.RenderCircle( this.CurrentPoint, 0.3f, ColorMath.Pink, 2f );

                if ( this.HasReachedDestination )
                {
                    float shrinkSpeed = (1f - (DeltaTime * this.ShrinkMultiplierSpeed));
                    float newScale = this.OriginalScale.x * shrinkSpeed;
                    if ( newScale <= 0.01f )
                        return true; //call it done
                    this.OriginalScale = new Vector3( newScale, newScale, newScale );
                    this.Particle.ApplyScale( this.OriginalScale );
                    if ( this.TimeToLastAfterDestination > 0 )
                        this.TimeToLastAfterDestination -= DeltaTime;
                    return this.TimeToLastAfterDestination <= 0;
                }
                else
                    return false;
            }

            public bool GetIsATravelingEffect() => !this.HasReachedDestination; //if it has reached its destination, then don't make it sound like we have not
        }
        #endregion class StraightShot
    }
}

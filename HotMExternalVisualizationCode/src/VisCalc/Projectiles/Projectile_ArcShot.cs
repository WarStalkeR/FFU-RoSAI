using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class Projectile_ArcShot : IVisProjectileMovementLogicImplementation
    {
        public void CreateMovingProjectiles( IProjectileLowLevelParticleSpawner LowLevelParticleSpawner, VisParticleEffect ParticleEffect, 
            VisParticleAndSoundUsage FullParticleDefinition, Vector3 Location,
            Vector3 Rotation, Vector3 Scale, ProjectileInstructionPacket ProjectilePacket )
        {
            ArcShot shot = pool.GetFromPoolOrCreate();
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

        private static ConcurrentPool<ArcShot> pool = new ConcurrentPool<ArcShot>( "Projectile_ArcShot-pool", 
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            () => new ArcShot() );


        #region class ArcShot
        private class ArcShot : ConcurrentPoolable<ArcShot>, IVisParticleData
        {
            public ParticleInstance Particle;
            public VisParticleAndSoundUsage FullParticleDefinition;
            public ProjectileInstructionPacket Packet;
            public Vector3 OriginalScale;
            public Vector3 CurrentPoint;
            public Vector3 TargetPoint;
            public Vector3 CurrentForward;
            public float ProgressSoFar = 0;
            public float TravelSpeed;
            public float ShrinkMultiplierSpeed;
            public float ExpiresAtTime;
            public float TimeToLastAfterDestination;
            public float HighestPriorSquareMagnitude = 100000;
            public readonly Vector3[] MidPoints = new Vector3[6];
            public int CurrentMidpointTarget = 0;
            public bool HasReachedFinalDestination;
            public Quaternion Rotation = Quaternion.identity;
            public Action OnComplete;
            private bool HasDoneFirstFrame = false;

            public VisParticleEffect ParentParticleEffect;
            public VisParticleEffect GetParentRow() => ParentParticleEffect;

            public ParticleInstance GetParticle() => Particle;

            public override void DoEarlyCleanupWhenGoingBackIntoPool()
            {
                this.Particle = new ParticleInstance();
                this.FullParticleDefinition = null;
                this.OriginalScale = Vector3.zero;
                this.CurrentPoint = Vector3.zero;
                for ( int i = 0; i < MidPoints.Length; i++ )
                    MidPoints[i] = Vector3.zero;
                this.TargetPoint = Vector3.zero;
                this.CurrentForward= Vector3.zero;
                this.ProgressSoFar = 0;
                this.TravelSpeed = 0;
                this.ShrinkMultiplierSpeed = 0;
                this.ExpiresAtTime = 0;
                this.TimeToLastAfterDestination = 0;
                this.HighestPriorSquareMagnitude = 100000;
                this.CurrentMidpointTarget = 0;
                this.HasReachedFinalDestination = false;
                this.Rotation = Quaternion.identity;
                this.OnComplete = null;
                this.ParentParticleEffect = null;
                this.HasDoneFirstFrame = false;
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
                float distPer = 1f / (this.MidPoints.Length + 1 );
                float curDist = distPer;
                for ( int i = 0; i < this.MidPoints.Length; i++ )
                {
                    this.MidPoints[i] = this.CalculateParabola( this.CurrentPoint, this.TargetPoint, Packet.MovementLogic.Float1, curDist ); //Float1 = parabola height
                    curDist += distPer;
                }

                float dist = (Location - Target).magnitude; //yeah this does a square root, but just once per particle

                float speedMultiplier = dist / 8f; //if the general distance traveled is 8 as a baseline, then anything else can be normalized against that
                if ( speedMultiplier < 1f ) //if slowing it down, do it by less than the amount we calculated
                {
                    float amountUnder = 1f - speedMultiplier;
                    amountUnder *= 0.2f;
                    speedMultiplier = 1f - amountUnder;
                    //this.ShrinkMultiplierSpeed = amountUnder * 0.5f;
                }
                //else
                //    this.ShrinkMultiplierSpeed = 0.5f;

                this.ShrinkMultiplierSpeed = Packet.MovementLogic.Float6;

                if ( FullParticleDefinition != null && FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact > 0 )
                    this.ShrinkMultiplierSpeed *= FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact;

                this.TravelSpeed = TravelSpeed * speedMultiplier; //here's where we normalize the speed base on the distance to travel
                this.ExpiresAtTime = ArcenTime.UnpausedTimeSinceStartF + 4f; //no shots last more than 4 seconds
                if ( ParticleEffect.TimeToLastAfterDestination > 0 )
                    this.TimeToLastAfterDestination = ParticleEffect.TimeToLastAfterDestination + Packet.MovementLogic.Float2;
                else
                    this.TimeToLastAfterDestination = Packet.MovementLogic.Float2;
                this.OnComplete = OnComplete;
                this.ParentParticleEffect = ParticleEffect;

                //this also needs to be done every frame, since it's moving in an arc
                this.RotateToFaceTargetFrom( this.CurrentPoint, this.MidPoints[0] );
                this.CurrentForward = this.Particle.Transform.forward;
            }

            #region CalculateParabola
            public Vector3 CalculateParabola( Vector3 start, Vector3 end, float height, float t )
            {
                if ( t > 1f )
                    return end; //clamped

                float fx = -4 * height * t * t + 4 * height * t;

                Vector3 mid = Vector3.Lerp( start, end, t );

                return new Vector3( mid.x, fx + Mathf.Lerp( start.y, end.y, t ), mid.z );
            }
            #endregion

            #region RotateToFaceTargetFrom
            public void RotateToFaceTargetFrom( Vector3 currentPoint, Vector3 currentTarget )
            {
                Vector3 targetLook = currentTarget - currentPoint;
                //targetLook.y = 0f;

                if ( targetLook.sqrMagnitude <= 0.01f )
                    return; //if the range is really short, don't rotate us; it will give us invalid rotations anyway, and cause error cascades

                Quaternion lookRot = Quaternion.LookRotation( targetLook, Vector3.up );

                this.Rotation = lookRot;
                this.Particle.Transform.rotation = lookRot;
            }
            #endregion

            public bool DoPerFrameUpdate( float DeltaTime )
            {
                if ( ArcenTime.UnpausedTimeSinceStartF >= this.ExpiresAtTime )
                {
                    if ( !this.HasReachedFinalDestination && this.OnComplete != null )
                    {
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

                if ( !this.HasReachedFinalDestination )
                {
                    Vector3 targetLoc;
                    if ( this.CurrentMidpointTarget >= this.MidPoints.Length )
                        targetLoc = this.TargetPoint;
                    else
                        targetLoc = this.MidPoints[this.CurrentMidpointTarget];

                    //move toward point
                    float moveSpeed = this.TravelSpeed * DeltaTime;
                    this.CurrentPoint += this.CurrentForward * moveSpeed;
                    this.Particle.Transform.position = this.CurrentPoint;

                    Vector3 dist = (targetLoc - this.CurrentPoint);

                    //rotate toward point
                    this.RotateToFaceTargetFrom( this.CurrentPoint, targetLoc );
                    this.CurrentForward = this.Particle.Transform.forward;

                    float squareMag = dist.sqrMagnitude;
                    if ( squareMag < 0.01f || squareMag > this.HighestPriorSquareMagnitude )
                    {
                        if ( this.CurrentMidpointTarget >= this.MidPoints.Length )
                        {
                            this.HasReachedFinalDestination = true;

                            if ( this.OnComplete != null )
                                this.OnComplete();
                            if ( this.FullParticleDefinition?.OnHit != null )
                                this.FullParticleDefinition.OnHit.DuringGame_PlayAtLocationWithAreaMultiplier( this.CurrentPoint,
                                    new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ), this.Packet.AreaRange, false );
                        }
                        else
                        {
                            this.CurrentMidpointTarget++;
                            this.HighestPriorSquareMagnitude = 100000;
                        }
                    }
                    else
                        this.HighestPriorSquareMagnitude = squareMag;

                    //DrawHelper.RenderLine( this.CurrentPoint, targetLoc, ColorMath.White );
                    //DrawHelper.RenderLine( this.CurrentPoint, this.MidPoint[], ColorMath.Green );
                    //DrawHelper.RenderLine( this.CurrentPoint, this.TargetPoint, ColorMath.Pink );
                }

                if ( this.HasReachedFinalDestination )
                {
                    float shrinkSpeed = (1f - (DeltaTime * this.ShrinkMultiplierSpeed));
                    float newScale = this.OriginalScale.x * shrinkSpeed;
                    if ( newScale <= 0.01f )
                    {
                        return true; //call it done
                    }
                    this.OriginalScale = new Vector3( newScale, newScale, newScale );
                    this.Particle.ApplyScale( this.OriginalScale );
                    if ( this.TimeToLastAfterDestination > 0 )
                        this.TimeToLastAfterDestination -= DeltaTime;
                    return this.TimeToLastAfterDestination <= 0;
                }
                else
                    return false;
            }

            public bool GetIsATravelingEffect() => !this.HasReachedFinalDestination; //if it has reached its destination, then don't make it sound like we have not
        }
        #endregion class ArcShot
    }
}

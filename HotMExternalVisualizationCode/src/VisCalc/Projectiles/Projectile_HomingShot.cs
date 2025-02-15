using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.External;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Arcen.HotM.ExternalVis
{
    public class Projectile_HomingShot : IVisProjectileMovementLogicImplementation
    {
        public void CreateMovingProjectiles( IProjectileLowLevelParticleSpawner LowLevelParticleSpawner, VisParticleEffect ParticleEffect, 
            VisParticleAndSoundUsage FullParticleDefinition, Vector3 Location,
            Vector3 Rotation, Vector3 Scale, ProjectileInstructionPacket ProjectilePacket )
        {
            HomingShot shot = pool.GetFromPoolOrCreate();
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

        private static ConcurrentPool<HomingShot> pool = new ConcurrentPool<HomingShot>( "Projectile_HomingShot-pool", 
            KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads,
            () => new HomingShot() );


        #region class HomingShot
        private class HomingShot : ConcurrentPoolable<HomingShot>, IVisParticleData
        {
            public ParticleInstance Particle;
            public VisParticleAndSoundUsage FullParticleDefinition;
            public ProjectileInstructionPacket Packet;
            public Vector3 OriginalScale;
            public Vector3 CurrentPoint;
            public Vector3 FirstTargetPoint;
            public Vector3 FinalTargetPoint;
            public Quaternion Rotation = Quaternion.identity;
            public float RotationSpeed;
            public float AddedRotationPerSecond;
            public float TravelSpeed;
            public float ShrinkMultiplierSpeed;
            public float ExpiresAtTime;
            public float TimeToLastAfterDestination;
            public float HighestPriorSquareMagnitude = 10000000;
            public bool CareAboutHighestDistance = true;
            public bool HasReachedFirstDestination;
            public bool HasReachedFinalDestination;
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
                this.FirstTargetPoint = Vector3.zero;
                this.FinalTargetPoint = Vector3.zero;
                this.Rotation = Quaternion.identity;
                this.RotationSpeed = 0;
                this.AddedRotationPerSecond = 0;
                this.TravelSpeed = 0;
                this.ShrinkMultiplierSpeed = 0;
                this.ExpiresAtTime = 0;
                this.TimeToLastAfterDestination = 0;
                this.HighestPriorSquareMagnitude = 10000000;
                this.CareAboutHighestDistance = true;
                this.HasReachedFirstDestination = false;
                this.HasReachedFinalDestination = false;
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

            /*
                    float_1:	How far up to go before aiming at the target.
		            float_2:	How far away from the center of mass of the firing unit to go.
		            float_3:	Rotation rate for steering.
		            float_4:	Added rotation rate per second shot exists.
		            float_5:	Initial y offset from firing ports.
		            float_6:	Shrink speed after reaching the target.
		            int_1:		Care about highest distance if above 0
		            vector_1:	Initial rotation of the shot.
            */

            public void Initialize( ParticleInstance EffectThatWasSpawned, ProjectileInstructionPacket Packet, VisParticleEffect ParticleEffect,
                VisParticleAndSoundUsage FullParticleDefinition, Vector3 Location, Vector3 ParentRotation, Vector3 Scale,
                Vector3 Target, float TravelSpeed, Action OnComplete )
            {
                this.Particle = EffectThatWasSpawned;
                this.FullParticleDefinition = FullParticleDefinition;
                this.Packet = Packet;
                this.OriginalScale = this.Particle.Transform.localScale;

                Location = Location.PlusY( this.Packet.MovementLogic.Float5 );
                this.CurrentPoint = Location;
                this.FinalTargetPoint = Target;

                float angleFromCenter = MathA.AngleXZInRadians( this.Packet.CenterOfUnitFiring, Location );
                this.FirstTargetPoint = MathA.GetVectorFromCircleCenterAtDistanceAndAngleRadians( Location, this.Packet.MovementLogic.Float2, angleFromCenter )
                    .PlusY( this.Packet.MovementLogic.Float1 );

                this.Rotation = Quaternion.Euler( this.Packet.MovementLogic.Vector1 ) * Quaternion.Euler( ParentRotation );
                this.Particle.Transform.rotation = this.Rotation;

                this.RotationSpeed = this.Packet.MovementLogic.Float3;
                this.AddedRotationPerSecond = this.Packet.MovementLogic.Float4;

                this.ShrinkMultiplierSpeed = Packet.MovementLogic.Float6;
                if ( FullParticleDefinition != null && FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact > 0 )
                    this.ShrinkMultiplierSpeed *= FullParticleDefinition.ShrinkSpeedMultiplierAfterImpact;

                this.TravelSpeed = TravelSpeed;
                this.ExpiresAtTime = ArcenTime.UnpausedTimeSinceStartF + 4f; //no shots last more than 4 seconds
                if ( ParticleEffect.TimeToLastAfterDestination > 0 )
                    this.TimeToLastAfterDestination = ParticleEffect.TimeToLastAfterDestination + Packet.MovementLogic.Float2;
                else
                    this.TimeToLastAfterDestination = Packet.MovementLogic.Float2;
                this.OnComplete = OnComplete;
                this.ParentParticleEffect = ParticleEffect;

                this.CareAboutHighestDistance = this.Packet.MovementLogic.Int1 > 0;
            }

            public bool DoPerFrameUpdate( float DeltaTime )
            {
                if ( ArcenTime.UnpausedTimeSinceStartF >= this.ExpiresAtTime )
                {
                    if ( !this.HasReachedFinalDestination )
                    {
                        if ( this.OnComplete != null )
                            this.OnComplete();
                        if ( this.FullParticleDefinition?.OnHit != null )
                            this.FullParticleDefinition.OnHit.DuringGame_PlayAtLocationWithAreaMultiplier( this.CurrentPoint,
                                new Vector3( 0, Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360f ), 0 ), this.Packet.AreaRange, false );
                    }
                    return true;
                }

                if ( this.HasReachedFinalDestination )
                {
                    //float slowdown = (1f - (DeltaTime * 6f));
                    //this.TravelSpeed *= slowdown;
                }
                else
                {
                    if ( !this.HasDoneFirstFrame )
                    {
                        this.HasDoneFirstFrame = true;
                        return false;
                    }

                    Vector3 targetLoc;
                    if ( this.HasReachedFirstDestination )
                        targetLoc = this.FinalTargetPoint;
                    else
                        targetLoc = this.FirstTargetPoint;

                    //move toward point
                    float moveSpeed = this.TravelSpeed * DeltaTime;
                    this.CurrentPoint += this.Particle.Transform.forward * moveSpeed;
                    this.Particle.Transform.position = this.CurrentPoint;

                    Vector3 dist = (targetLoc - this.CurrentPoint);

                    //rotate toward point
                    Vector3 direction = dist.normalized;
                    Quaternion lookRotation = Quaternion.LookRotation( new Vector3( direction.x, direction.y, direction.z ) );
                    this.Rotation = Quaternion.RotateTowards( this.Rotation, lookRotation, this.RotationSpeed * DeltaTime );
                    this.Particle.Transform.rotation = this.Rotation;

                    float squareMag = dist.sqrMagnitude;
                    if ( squareMag < 0.1f || ( this.CareAboutHighestDistance && squareMag > this.HighestPriorSquareMagnitude ) )
                    {
                        if ( this.HasReachedFirstDestination )
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
                            this.HasReachedFirstDestination = true;
                            this.HighestPriorSquareMagnitude = 10000000;
                        }
                    }
                    else
                        this.HighestPriorSquareMagnitude = squareMag;

                    //if ( InputCaching.Debug_ShowEntityShotEmissionPoints )
                    //{
                    //    DrawHelper.RenderLine( this.CurrentPoint, targetLoc, ColorMath.White );
                    //    DrawHelper.RenderLine( this.CurrentPoint, this.FirstTargetPoint, ColorMath.Green );
                    //    DrawHelper.RenderLine( this.CurrentPoint, this.FinalTargetPoint, ColorMath.Pink );
                    //}

                    this.RotationSpeed += (this.AddedRotationPerSecond * DeltaTime);
                }                    

                if ( !this.HasReachedFinalDestination )
                {
                    return false;
                }
                else
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
            }

            public bool GetIsATravelingEffect() => !this.HasReachedFinalDestination; //if it has reached its destination, then don't make it sound like we have not
        }
        #endregion class HomingShot
    }
}

using Arcen.HotM.Core;
using Arcen.Universal;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class BreachingPod : AbstractMob, IConcurrentPoolable<BreachingPod>, IProtectedListable
    {
        private VisSimpleDrawingObject drawingObject;

        private readonly ParticleSpawner smokeSpawner = new ParticleSpawner();
		public readonly int BreachingPodIndex;

        #region Pooling
        private static ReferenceTracker RefTracker;
		private static int lastBreachingPodIndex = 1;

        private BreachingPod() : base( "BreachingPod", 6, 2 )
        {
			this.BreachingPodIndex = Interlocked.Increment( ref lastBreachingPodIndex );

			if (RefTracker == null)
				RefTracker = new ReferenceTracker("BreachingPods");
			RefTracker.IncrementObjectCount();
		}

		private static readonly ConcurrentPool<BreachingPod> Pool = new ConcurrentPool<BreachingPod>("BreachingPods",
			KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new BreachingPod(); });

		public static BreachingPod GetFromPoolOrCreate()
		{
			return Pool.GetFromPoolOrCreate();
		}

        public static int GetCountEverCreated()
        {
            return Pool.ItemsCreated;
        }

        public override void ReturnMobToPool()
		{
			Pool.ReturnToPool(this);
        }

		public void DoAnyBelatedCleanupWhenComingOutOfPool()
		{
			this.SetToDefaults(true);
		}

		public void DoEarlyCleanupWhenGoingBackIntoPool()
		{
			this.SetToDefaults(false);
		}

		public override void DoBeforeRemoveOrClear()
		{
			this.SetToDefaults(false);
			this.ReturnMobToPool();
		}

		public void SetToDefaults(bool ForExitingPool)
		{
			if (waypoints != null) waypoints.Clear();
			waypointIndex = -1;
			TimeSinceSpawn = -1;
		}

		public override bool GetInPoolStatus()
		{
			return this.isInPool;
		}

		public void SetInPoolStatus(bool IsInPool)
		{
			this.isInPool = IsInPool;
        }

        public void Optional_DoAnyEarlyInits()
        {
        }
        #endregion

        protected override void ChildMobInit( RandomGenerator rand, MapCell cell )
        {
			Speed = BaseSpeed * 2.25f;

            smokeSpawner.smoke2DTrailObject = VisSimpleDrawingObjectTable.Instance.GetRowByID( "SmokePuff" );
            smokeSpawner.smoke3DTrailObject = VisSimpleDrawingObjectTable.Instance.GetRowByID( "3DLightCloud2" );

            SteeringStiffness = 0.2f;
			canFly = true;
			isFlying = true;
			WaypointWaitTime = 1000f;
        }

		protected override void SetupRendering(RandomGenerator rand)
		{
			drawingObject = VisSimpleDrawingObjectTable.Instance.GetRowByID("PlayerBreachingPod");
		}

		protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRot, float maxDelta)
		{
			return (lookRot, maxDelta);
		}

		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
		{
			if ((GetCurrentWaypointPos() - Pos).sqrMagnitude < Speed * Speed / 40) return true;
			//if (Mathf.Approximately(GetCurrentWaypointPos().y/1000, Pos.y/1000)) return true;
			if (MathA.Abs(Pos.y - GetCurrentWaypointPos().y) < 0.01f)
			{
				Quaternion hardDir = Quaternion.LookRotation(GetCurrentWaypointPos() - Pos);
				float angle = Quaternion.Angle(Rotation, hardDir);
				return angle > 70 && angle < 110;
			}
			return false;
		}

		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
		{
			return true;
		}

		protected override void DoChildFrameLogicNoWaypoints(float deltaTime)
		{
			if (Pos.y < -1 || TimeSinceSpawn > 1.5f)
			{
				//I shouldn't need this. This is stupid.
				//But if I don't, the pods never get returned to the pool
				this.readyForDespawn = true;
            }
		}

		protected override void DoChildFrameLogic(float deltaTime)
        {
			bool drawSmoke = true;

            if ( SimCommon.ShouldCityLifeAnimate && drawSmoke )
            {
				//we want to drop a smoke trail object, but we want that to be managed by the actual particle system, not ourselves
				//only drop 40 per second, and have them last for 1 second
				//here's a version that drops two 3D objects instead
				smokeSpawner.Spawn3DSmokeStyleParticleAtCurrentLocation( this.Pos, ColorMath.White, ColorMath.A180Gray160, 40,
					0.025f, 1f, BREACHING_POD_SMOKE_STARTING_SCALE_1, BREACHING_POD_SMOKE_STARTING_SCALE_2, BREACHING_POD_SCALE_GROWTH, 0.005f );
			}

            if (Pos.y < -10 || TimeSinceSpawn > 15)
			{
                //I shouldn't need this. This is stupid.
                //But if I don't, the pods never get returned to the pool
                this.readyForDespawn = true;
            }
			SteeringStiffness += 0.02f;
		}

		protected override void DoOnNewWaypoint()
		{
			if (waypointIndex >= waypoints.Count)
			{
				waypointIndex = -1;
				this.readyForDespawn = true;
            }
		}
        internal static readonly Vector3 BREACHING_POD_SCALE = Vector3.one / 8;

        internal static readonly Vector3 BREACHING_POD_SMOKE_STARTING_SCALE_1 = Vector3.one / 16;
        internal static readonly Vector3 BREACHING_POD_SMOKE_STARTING_SCALE_2 = Vector3.one / 9;
        internal static readonly float BREACHING_POD_SCALE_GROWTH = 0.25f;

		protected override bool GetShouldDespawnOnInitIfNoValidPath() => true;

        protected override void DoMobRenderInner()
		{
            Color drawColor = Color.white;
            if ( !this.IsFullyFadedIn )
                drawColor.a = this.AlphaAmountFromFadeIn;

            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;
            if ( !this.CalculateEfficientIsInVisualRange() )
                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, BREACHING_POD_SCALE, RenderColorStyle.FogOfWar,
                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
            else
                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, BREACHING_POD_SCALE, RenderColorStyle.NoColor, 
					drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
		}

		public override void SpawnFadingOutCopyOfSelf()
        {
            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
			this.SpawnFadingOutObjectAtCurrentLocation( rGroup, 3f, Color.white, RenderColorStyle.NoColor, BREACHING_POD_SCALE );
		}

        internal void SetPathSimpleToPoint( Vector3 pos, Vector3 centerPoint, Quaternion rotation )
        {
            Pos = pos;
            Rotation = rotation;
            waypoints.Clear();

            Forward = Vector3.forward;
            RotateToFace( Rotation, float.PositiveInfinity );

            waypoints.Add( new Waypoint( pos, WaypointTask.DROP_POD ) );
            waypoints.Add( new Waypoint( centerPoint, WaypointTask.DROP_POD ) );

            WaypointWaitTime = (Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) * Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ));
            this.TimeSinceSpawn = 0.02f;
            this.waypointIndex = 1;
        }

        internal void SetPathComplexIntoGround(Vector3 pos, Vector3 centerPoint, Quaternion rotation)
		{
			Pos = pos;
			Rotation = rotation;
			waypoints.Clear();

			Forward = Vector3.forward;
			RotateToFace(Rotation, float.PositiveInfinity);

			waypoints.Add(new Waypoint(pos, WaypointTask.DROP_POD));
			waypoints.Add(new Waypoint(centerPoint + Vector3.down * 1f, WaypointTask.DROP_POD));
			waypoints.Add(new Waypoint(centerPoint + Vector3.down * 4f, WaypointTask.DROP_POD));
			waypoints.Add(new Waypoint(centerPoint + Vector3.down * 8f, WaypointTask.DROP_POD));

			waypoints.Add(new Waypoint(new Vector3(centerPoint.x, -100, centerPoint.z), WaypointTask.DROP_POD));

			WaypointWaitTime = (Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) * Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ));
			this.TimeSinceSpawn = 0.02f;
			this.waypointIndex = 1;
		}
	}

	public struct BreachingPodRequest
	{
		public Vector3 OriginForPod;
		public Vector3 PodTarget;
		public Quaternion Rotation;
    }
}
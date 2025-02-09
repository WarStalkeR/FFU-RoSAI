using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class Pedestrian : AbstractPedestrian, IConcurrentPoolable<Pedestrian>
	{
        #region Pooling
        private static ReferenceTracker RefTracker;
		private Pedestrian() : base( "Pedestrian" )
        {
			if (RefTracker == null)
				RefTracker = new ReferenceTracker("Pedestrians");
			RefTracker.IncrementObjectCount();
		}

		private static readonly ConcurrentPool<Pedestrian> Pool = new ConcurrentPool<Pedestrian>("Pedestrians",
				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Pedestrian(); });

        public static Pedestrian GetFromPoolOrCreate()
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
			TimeSinceSpawn = 0.02f;
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

        protected override bool GetShouldDespawnOnInitIfNoValidPath() => true;

        public readonly List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList =
			List<(LanePrefab lane, int index, Vector3 tPos)>.Create_WillNeverBeGCed( 12, "Pedestrian-workingLaneList", 12 );

        protected override void ChildMobInit( RandomGenerator rand, MapCell cell )
        {
			StreetPathfinder.FillRandomWalkStartingIn(rand, TransportMethod.Walking, waypoints, cell, 2f, workingLaneList );
			if (waypoints.Count < 10)
			{
				SetToDefaults(true);
				return;
			}
			Speed = BaseSpeed * SPEED_ADJUSTMENT;
            DriverAccuracy = rand.NextFloat(0.03f) + .007f;
			DriverAccuracy += rand.NextFloat(1) < 0.01 ? .2f : 0; //1% chance of just being drunk of his rocker
			SteeringStiffness = rand.NextFloat(0.2f) + 1.35f - DriverAccuracy;
			//pedestrians tend to just stay where they are and not meander back and forth
			Vector2 v2 = MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy * 2;
			WaypointAccuracy = new Vector3(v2.x, 0, v2.y);
		}

		public override void JustStandThere(RandomGenerator rand, Vector3 loc, Vector3 target)
		{
			Pos = loc;
			for (int i = 0; i< 20; i++)
			{
				this.waypoints.Add(new Waypoint(loc, WaypointTask.WAIT));
			}
			Speed = 0;
			waypointIndex = 1;

			FadeDelay = 0;
			Steering = 0;
			TimeSinceSpawn = 0.02f;
			WaypointAccuracy = Vector3.zero;

			WaypointWaitTime = 0;

			//face a random direction
			ArcenPoint np = MathA.GetRandomPointFromCircleCenter(rand, new ArcenPoint((int)(Pos.x * 1000), (int)(Pos.z * 1000)), 1250, 1750);
			Vector3 p = new Vector3(np.X / 1000f, 0, np.Y / 1000f);

			(Quaternion lookRot, float maxDelta) = GetBaseRotationParams(p, 5000);
			(lookRot, maxDelta) = ChildMutateRotationParams(p, 5000, lookRot, maxDelta);
			RotateToFace(lookRot, maxDelta);
		}

		internal static readonly Vector3 PEDESTRIAN_SCALE = Vector3.one / 3;
        internal const float MAX_PED_DRAW_DIST_STREETS = 200 * 200;
        internal const float PED_RADIUS = 0.4f;
        internal const float PED_HALF_HEIGHT = 0.2f;

        protected override void DoMobRenderInner()
        {
			if (waypointIndex < 0) return;

            if ( InputCaching.SkipDrawing_Pedestrians )
                return;

            float distSquared = (CameraCurrent.CameraBodyPosition - Pos).GetSquareGroundMagnitude();
            if ( distSquared > MAX_PED_DRAW_DIST_STREETS )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }

            if ( !CameraCurrent.TestFrustumColliderInternalFast( Pos, PED_HALF_HEIGHT, PED_RADIUS ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            Color drawColor = Color.white;
            if ( !this.IsFullyFadedIn )
                drawColor.a = this.AlphaAmountFromFadeIn;

            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;
            if ( this.IsFullyFadedIn )
                rGroup?.WriteToDrawBufferForOneFrame_BasicNoColor( Pos, Rotation, PEDESTRIAN_SCALE, RenderColorStyle.NoColor );
            else
                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, PEDESTRIAN_SCALE, RenderColorStyle.NoColor,
                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
        }

        public override void SpawnFadingOutCopyOfSelf()
        {
            if ( !this.CalculateEfficientIsInVisualRange() )
                return;
            if ( InputCaching.SkipDrawing_Pedestrians )
                return;

            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
            this.SpawnFadingOutObjectAtCurrentLocation( rGroup, 3f, Color.white, RenderColorStyle.NoColor, PEDESTRIAN_SCALE );
        }
    }
}

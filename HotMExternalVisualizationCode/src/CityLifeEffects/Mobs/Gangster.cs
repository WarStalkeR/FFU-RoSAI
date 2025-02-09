using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class Gangster : AbstractPedestrian, IConcurrentPoolable<Gangster>
    {
        private readonly ParticleSpawner shotSpawner = new ParticleSpawner();
		private Vector3 shootThis = Vector3.zero;

		#region Pooling
		private static ReferenceTracker RefTracker;
		private Gangster() : base( "Gangster" )
        {
			if (RefTracker == null)
				RefTracker = new ReferenceTracker("Gangsters");
			RefTracker.IncrementObjectCount();
		}

		private static readonly ConcurrentPool<Gangster> Pool = new ConcurrentPool<Gangster>("Gangsters",
				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Gangster(); });

		public static Gangster GetFromPoolOrCreate()
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

        protected override void ChildMobInit( RandomGenerator rand, MapCell cell )
        {
			Speed = 0;
            shotSpawner.smoke3DTrailObject = VisSimpleDrawingObjectTable.Instance.GetRowByID("ShotLong");
		}

		public override void JustStandThere(RandomGenerator rand, Vector3 loc, Vector3 target)
		{
			Pos = loc;
			for (int i = 0; i < 20; i++)
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
			shootThis = target;

			target.y = 0;
			(Quaternion lookRot, float maxDelta) = GetBaseRotationParams(target, 5000);
			(lookRot, maxDelta) = ChildMutateRotationParams(target, 5000, lookRot, maxDelta);
			RotateToFace(lookRot, maxDelta);
		}

		protected override void DoChildFrameLogic(float deltaTime)
		{
			if ( !SimCommon.ShouldCityLifeAnimate )
                return;
            if ( InputCaching.SkipDrawing_Pedestrians )
                return;

			if ( CityLifeVis.GameSim.DisableBackgroundGunfights )
			{
				this.readyForDespawn = true;
				return;
			}

            if (Random.value < 0.005f && Pos.y < 0.1f)
			{
				shotSpawner.SpawnTracerAtCurrentLocation(this.Pos + Vector3.up * GANGSTER_FIRING_OFFSET_Y * 0.65f, shootThis, ColorMath.White, ColorMath.White, 40, 2, 0.05f, GUNSHOT_SCALE, GUNSHOT_SCALE, Quaternion.LookRotation((shootThis - Pos), Vector3.up), 0);

                //ParticleSoundRefs.GangsFiring.DuringGame_PlayAtLocation( this.Pos );
            }
		}

		/*protected override void DoOnNewWaypoint()
		{
			
		}

		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
		{
			return false;
		}*/

		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
		{
			return false;
		}

		/*protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRotIn, float maxDelta)
		{
			return (lookRotIn, Steering * deltaTime);
		}*/

		internal static readonly Vector3 GANGSTER_SCALE = Vector3.one / 3;
        internal static readonly float GANGSTER_FIRING_OFFSET_Y = 0.1f;
        internal static readonly Vector3 GUNSHOT_SCALE = Vector3.one / 9;

        protected override bool GetShouldDespawnOnInitIfNoValidPath() => false;

        protected override void DoMobRenderInner()
        {
			if (waypointIndex < 0) return;

            if ( InputCaching.SkipDrawing_Pedestrians )
                return;
            if ( CityLifeVis.GameSim.DisableBackgroundGunfights )
            {
                this.readyForDespawn = true;
                return;
            }

            float distSquared = (CameraCurrent.CameraBodyPosition - Pos).GetSquareGroundMagnitude();
            if ( distSquared > Pedestrian.MAX_PED_DRAW_DIST_STREETS )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }

            if ( !CameraCurrent.TestFrustumColliderInternalFast( Pos, Pedestrian.PED_HALF_HEIGHT, Pedestrian.PED_RADIUS ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            Color drawColor = Color.white;
			if ( !this.IsFullyFadedIn )
				drawColor.a = this.AlphaAmountFromFadeIn;

            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;
            if ( this.IsFullyFadedIn )
                rGroup?.WriteToDrawBufferForOneFrame_BasicNoColor( Pos, Rotation, GANGSTER_SCALE, RenderColorStyle.NoColor );
            else
                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, GANGSTER_SCALE, RenderColorStyle.NoColor,
                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
        }

		public override void SpawnFadingOutCopyOfSelf()
        {
            if ( !this.CalculateEfficientIsInVisualRange() )
                return;
            if ( InputCaching.SkipDrawing_Pedestrians )
                return;
            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
			this.SpawnFadingOutObjectAtCurrentLocation(rGroup, 3f, Color.white, RenderColorStyle.NoColor, GANGSTER_SCALE );
		}
	}
}

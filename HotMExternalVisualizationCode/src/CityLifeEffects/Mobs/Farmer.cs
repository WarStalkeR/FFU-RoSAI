//using Arcen.Universal;
//using System;
//using UnityEngine;
//using Arcen.HotM.Core;
//using Arcen.HotM.Visualization;

//namespace Arcen.HotM.ExternalVis.CityLifeEffects
//{
//	public class Farmer : AbstractMob, IConcurrentPoolable<Farmer>, IProtectedListable
//	{
//		private const float SpeedAdjustment = 1f/4f;
//		private const float SteeringAdjustment = 3f / 5f;//slower moving objects don't need as rapid of direction changes.

//        private VisSimpleDrawingObject drawingObject;
//        private VisSimpleDrawingObject drawingObjectMarker;

//        #region Pooling
//        private static ReferenceTracker RefTracker;
//		private Farmer()
//		{
//			if (RefTracker == null)
//				RefTracker = new ReferenceTracker("Farmers");
//			RefTracker.IncrementObjectCount();
//		}

//		private static readonly ConcurrentPool<Farmer> Pool = new ConcurrentPool<Farmer>("Farmers",
//				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Farmer(); });

//		public static Farmer GetFromPoolOrCreate()
//		{
//			return Pool.GetFromPoolOrCreate();
//		}

//		public override void ReturnToPool()
//		{
//			Pool.ReturnToPool(this);
//		}

//		public void DoAnyBelatedCleanupWhenComingOutOfPool()
//		{
//			this.SetToDefaults(true);
//		}

//		public void DoEarlyCleanupWhenGoingBackIntoPool()
//		{
//			this.SetToDefaults(false);
//		}

//		public void DoBeforeRemoveOrClear()
//		{
//			this.SetToDefaults(false);
//			this.ReturnToPool();
//		}

//		public void SetToDefaults(bool ForExitingPool)
//		{
//			if (waypoints != null) waypoints.Clear();
//			waypointIndex = -1;
//			TimeSinceSpawn = 0.02f;
//		}

//		public override bool GetInPoolStatus()
//		{
//			return this.isInPool;
//		}

//		public void SetInPoolStatus(bool IsInPool)
//		{
//			this.isInPool = IsInPool;
//        }

//        public void Optional_DoAnyEarlyInits()
//        {
//        }
//        #endregion

//        public override void ChildInit(RandomGenerator rand, MapCell cell )
//		{
//			Speed = BaseSpeed * SpeedAdjustment;
//            DriverAccuracy = rand.NextFloat(0.03f) + .007f;
//			SteeringStiffness = rand.NextFloat(0.2f) + 0.85f - DriverAccuracy;
//			//Farmers tend to just stay where they are and not meander back and forth
//			WaypointAccuracy = Vector2.zero;
//		}

//		protected override void SetupRendering(RandomGenerator rand)
//		{
//			drawingObject = VisDrawingObjectTagTable.Instance.GetRowByID("Person").DrawingObjects.GetRandom(rand);
//			drawingObjectMarker = VisSimpleDrawingObjectTable.Instance.GetRowByID("MetallicCube");
//		}

//		public override bool ShouldSelfRespawn()
//		{
//			return true;
//		}

//		public void SetActivity(MapPathingRegion region, int participantNum, int maxParticipants)
//		{
//			XmlFrontFace f = region.Type.FrontFace; //Z?
//			float z = MathA.Max( region.OBBCache.OBBExtents.x, region.OBBCache.OBBExtents.z)*2; //length
//			float x = MathA.Min( region.OBBCache.OBBExtents.x, region.OBBCache.OBBExtents.z); //width
//			x = x * 2 / maxParticipants * participantNum - x; //width

//			Vector3 origin = region.OBBCache.Center;
//			Vector3 forward = region.OBBCache.GetOBB_ExpensiveToUse().Look;
//			Vector3 right = region.OBBCache.GetOBB_ExpensiveToUse().Right;

//			Vector2 v2 = (MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy * 2) / 25;

//			waypoints.Clear(); //Chris added.  Seems important?

//            for (int i=0;i<=12;i++)
//			{
//				Vector3 p1 = origin + (forward * (z / 12 * i + v2.x)) + (right * (x + v2.y / 8)) - (forward * z / 2);
//				waypoints.Add(new Waypoint(p1, WaypointTask.WAIT));
//			}
//			for (int i = 12; i >= 0; i--)
//			{
//				Vector3 p1 = origin + (forward * (z / 12 * i + v2.x)) + (right * (x + v2.y / 8)) - (forward * z / 2);
//				waypoints.Add(new Waypoint(p1, WaypointTask.WAIT));
//			}
//			WaypointWaitTime = Engine_Universal.PermanentQualityRandom.NextFloat( 0, 1f ) * 3;

//			Pos = waypoints[0];
//			waypointIndex = 1;
//			WaypointWaitTime = waypoints[0].data == WaypointTask.WAIT ? 3 : 0;

//			Forward = Vector3.forward;
//			(Quaternion lookRot, float maxDelta) = GetBaseRotationParams(waypoints[1], float.PositiveInfinity);
//			RotateToFace(lookRot, maxDelta);
//			Speed *= (Engine_Universal.PermanentQualityRandom.NextFloat( 0, 1f ) - 0.5f) / 25f;
//		}

//		protected override void DoChildFrameLogicNoWaypoints(float deltaTime)
//		{

//		}

//		protected override void DoChildFrameLogic(float deltaTime)
//		{

//		}

//		protected override void DoOnNewWaypoint()
//		{
//			if (DriverAccuracy >= 0.2f && Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) < 0.2f)
//			{
//				Vector2 v2 = (MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy * 2) / 25;
//				WaypointAccuracy = new Vector3(v2.x, 0, v2.y);
//			}
//			WaypointWaitTime += (Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) - 0.5f)/2;
//		}

//		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
//		{
//			return false;
//		}

//		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
//		{
//			return (nextPoint - Pos).sqrMagnitude < 0.09 + (DriverAccuracy / 2 * SpeedAdjustment);
//		}

//		protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRotIn, float maxDelta)
//		{
//			return (lookRotIn, Steering * deltaTime * SteeringAdjustment);
//		}

//        internal static readonly Color MARKER_COLOR = new Color( 0.933f, 0.737f, 0.094f );
//        internal static readonly Vector3 MARKER_SCALE = new Vector3( 1.35f, 0.05f, 1.35f ) / 8;

//        public override void DoRender()
//		{
//			if (waypointIndex < 0) return;

//			if ( InputCaching.SkipDrawing_Pedestrians )
//				return;

//            Color drawColor = Color.white;
//            if ( !this.IsFullyFadedIn )
//                drawColor.a = this.AlphaAmountFromFadeIn;

//            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;
//            if ( this.IsFullyFadedIn )
//                rGroup?.WriteToDrawBufferForOneFrame_BasicNoColor( Pos, Rotation, Pedestrian.PEDESTRIAN_SCALE, RenderColorStyle.NoColor );
//            else
//                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, Pedestrian.PEDESTRIAN_SCALE, RenderColorStyle.NoColor,
//                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );

//            //         rGroup = drawingObjectMarker.RendererGroup as A5RendererGroup;
//            //rGroup?.WriteToDrawBufferForOneFrame(Pos + Vector3.up * 1.45f / 16, Rotation, MARKER_SCALE, RenderColorStyle.SelfColor, MARKER_COLOR, RenderOpacity.Normal );
//        }

//        public override void SpawnFadingOutCopyOfSelf()
//        {
//            if ( !this.CalculateEfficientIsInVisualRange() )
//                return;
//            if ( InputCaching.SkipDrawing_Pedestrians )
//                return;

//            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
//            this.SpawnFadingOutObjectAtCurrentLocation( rGroup, 3f, Color.white, RenderColorStyle.NoColor, Pedestrian.PEDESTRIAN_SCALE );
//        }
//    }
//}

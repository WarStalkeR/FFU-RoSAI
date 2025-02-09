//using Arcen.Universal;
//using System;
//using UnityEngine;
//using Arcen.HotM.Core;
//using Arcen.HotM.Visualization;

//namespace Arcen.HotM.ExternalVis.CityLifeEffects
//{
//	public class Sportser : AbstractMob, IConcurrentPoolable<Sportser>, IProtectedListable
//	{
//		private const float SpeedAdjustment = 1f / 4f;
//		private float SteeringAdjustment = 3f / 5f;//slower moving objects don't need as rapid of direction changes.
//		private int teamNum = 0;
//		private float waypointTime = 0;

//		private VisSimpleDrawingObject drawingObjectCohort1;
//        private VisSimpleDrawingObject drawingObjectCohort2;

//        #region Pooling
//        private static ReferenceTracker RefTracker;
//		private Sportser()
//		{
//			if (RefTracker == null)
//				RefTracker = new ReferenceTracker("Sportsers");
//			RefTracker.IncrementObjectCount();
//		}

//		private static readonly ConcurrentPool<Sportser> Pool = new ConcurrentPool<Sportser>("Sportsers",
//				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new Sportser(); });

//		public static Sportser GetFromPoolOrCreate()
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
//            DriverAccuracy = 0;
//			SteeringStiffness = 0.95f - DriverAccuracy;
//			WaypointAccuracy = Vector2.zero;
//		}

//		protected override void SetupRendering(RandomGenerator rand)
//		{
//			drawingObjectCohort1 = VisDrawingObjectTagTable.Instance.GetRowByID("PersonSportsCohort1").DrawingObjects.GetRandom(rand);
//			drawingObjectCohort2 = VisDrawingObjectTagTable.Instance.GetRowByID("PersonSportsCohort2").DrawingObjects.GetRandom(rand);
//		}

//		public override bool ShouldSelfRespawn()
//		{
//			return true;
//		}

//		public void SetActivity(MapPathingRegion region, int participantNum, int maxParticipants)
//		{
//			teamNum = participantNum % 2;
//			XmlFrontFace f = region.Type.FrontFace; //Z?
//			float z = MathA.Max( region.OBBCache.OBBExtents.x, region.OBBCache.OBBExtents.z); //length
//			float x = MathA.Min( region.OBBCache.OBBExtents.x, region.OBBCache.OBBExtents.z); //width

//			Vector3 origin = region.OBBCache.Center;
//			//Vector3 forward = obb.Look;
//			//Vector3 right = obb.Right;
//			//Vector2 v2 = (MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy * 2) / 25;

//			waypoints.Clear(); //Chris added.  Seems important

//			if (participantNum <= 1)
//			{
//				SteeringAdjustment = 10000;
//				for (int i = 0; i <= 12; i++)
//				{
//					Vector3 p1 = new Vector3((float)Math.Sin(i * 60 * MathA.PIOver180) * x, 0, participantNum == 0 ? -z : z);
//					waypoints.Add(new Waypoint(origin + p1, WaypointTask.NONE));
//				}
//				waypoints.Add(new Waypoint(origin + new Vector3((float)Math.Sin(0) * x, 0, participantNum == 0 ? -z : z), WaypointTask.NONE));
//			}
//			else
//			{
//				x *= 0.9f;
//				z *= 0.7f;
//				for (int i = 12; i >= 0; i--)
//				{
//					Vector3 p2 = i == 0 ? Vector2.zero : MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) / 2;
//					Vector3 p1 = new Vector3(((float)Math.Sin(i * 60 * MathA.PIOver180) + p2.x) * x, 0, ((float)Math.Cos(i * 30 * MathA.PIOver180) + p2.y) * z);

//					waypoints.Add(new Waypoint(origin + p1, WaypointTask.NONE));
//				}
//				waypoints.Add(new Waypoint(origin + new Vector3(((float)Math.Sin(0)) * x, 0, ((float)Math.Cos(0)) * z), WaypointTask.NONE));
//			}
//			WaypointWaitTime = Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) * 3;

//			Pos = waypoints[0];
//			waypointIndex = 1;
//			WaypointWaitTime = waypoints[0].data == WaypointTask.WAIT ? 3 : 0;

//			Forward = Vector3.forward;
//			(Quaternion lookRot, float maxDelta) = GetBaseRotationParams(waypoints[1], float.PositiveInfinity);
//			RotateToFace(lookRot, maxDelta);
//            //Speed *= (Engine_Universal.PermanentQualityRandom.NextFloat( 0.01f, 1f ) - 0.5f) / 25f;
//        }

//        protected override void DoChildFrameLogicNoWaypoints(float deltaTime)
//		{

//		}

//		protected override void DoChildFrameLogic(float deltaTime)
//		{
//			if((GetCurrentWaypointPos() - Pos).sqrMagnitude < 0.09 + (DriverAccuracy / 2 * SpeedAdjustment))
//			{
//				WaypointWaitTime = deltaTime*3;
//			}
//			if (waypointIndex + 1 >= waypoints.Count || waypointIndex < 0)
//			{
//				waypointIndex = 0;
//			}
//			waypointTime -= deltaTime;
//		}

//		protected override void DoOnNewWaypoint()
//		{
//			if(waypointIndex+1 >= waypoints.Count || waypointIndex < 0)
//			{
//				waypointIndex = 0;
//			}
//			waypointTime = 0.75f;
//		}

//		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
//		{
//			return waypointTime < 0;
//		}

//		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
//		{
//			return waypointTime < 0;// (nextPoint - Pos).sqrMagnitude < 0.09 + (DriverAccuracy / 2 * SpeedAdjustment);
//		}

//		protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRotIn, float maxDelta)
//		{
//			return (lookRotIn, maxDelta * SteeringAdjustment);
//		}

//		public override void DoRender()
//		{
//			if (waypointIndex < 0) return;

//            if ( InputCaching.SkipDrawing_Pedestrians )
//                return;

//            Color drawColor = Color.white;
//            if ( !this.IsFullyFadedIn )
//                drawColor.a = this.AlphaAmountFromFadeIn;

//            A5RendererGroup rGroup = ( teamNum == 0 ? drawingObjectCohort1 : drawingObjectCohort2 ).RendererGroup as A5RendererGroup;
//			if ( this.IsFullyFadedIn )
//				rGroup?.WriteToDrawBufferForOneFrame_BasicNoColor( Pos, Rotation, Pedestrian.PEDESTRIAN_SCALE, RenderColorStyle.NoColor );
//            else
//                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, Pedestrian.PEDESTRIAN_SCALE, RenderColorStyle.NoColor,
//                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
//        }

//        public override void SpawnFadingOutCopyOfSelf()
//        {
//            if ( !this.CalculateEfficientIsInVisualRange() )
//                return;
//            if ( InputCaching.SkipDrawing_Pedestrians )
//                return;

//            A5RendererGroup rGroup = (teamNum == 0 ? drawingObjectCohort1 : drawingObjectCohort2)?.RendererGroup as A5RendererGroup;
//            this.SpawnFadingOutObjectAtCurrentLocation( rGroup, 3f, Color.white, RenderColorStyle.NoColor, Pedestrian.PEDESTRIAN_SCALE );
//        }
//    }
//}

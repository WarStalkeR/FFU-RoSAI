using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;
using Arcen.HotM.External;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public class StreetVehicle : AbstractMob, IConcurrentPoolable<StreetVehicle>, IProtectedListable
    {
        private VisSimpleDrawingObject drawingObject;
		private Color randomColor;

        public readonly List<(LanePrefab lane, int index, Vector3 tPos)> workingLaneList =
            List<(LanePrefab lane, int index, Vector3 tPos)>.Create_WillNeverBeGCed( 12, "StreetVehicle-workingLaneList", 12 );

        #region Pooling
        private static ReferenceTracker RefTracker;
		private StreetVehicle() : base( "StreetVehicle", 8, 4 )
        {
			if (RefTracker == null)
				RefTracker = new ReferenceTracker( "StreetVehicles" );
			RefTracker.IncrementObjectCount();
		}

		private static readonly ConcurrentPool<StreetVehicle> Pool = new ConcurrentPool<StreetVehicle>( "StreetVehicles",
				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOn_EvenPartial_GameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new StreetVehicle(); });

		public static StreetVehicle GetFromPoolOrCreate()
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
            /*
			 * Placeholder testing code to try and get point-to-point routes working
			 * May be useful for things like police (etc).
			 * 
			if (cell == null || cell.AllRoads == null || cell.AllRoads.Count < 1) return;
			Vector3 end = cell.CalculateCellCenter(); //cell.AllRoads.GetRandom(rand).CenterPoint;
			Vector3 start = (MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * 10);
			while(start.sqrMagnitude < 3.6)
			{
				start = (MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * 10);
			}
			start = new Vector3(start.x, 0, start.y);
			ArcenDebugging.LogSingleLine($"trying {start} -> {end}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			start += end;
			StreetPathfinder.GetPathFromTo(end, start, rand, TransportMethod.Driving, waypoints, cell);
			//StreetPathfinder.FillRandomWalkStartingIn(rand, TransportMethod.Driving, waypoints, cell);
			if (waypoints.Count < 10)
			{
				SetToDefaults(true);
				return;
			}
			if (doOnce)
			{
				ArcenDebugging.LogSingleLine($"  Spawned at: {waypoints[0]}", Verbosity.ShowAsError);
				List<IShapeToDraw> shapes = Engine_HotM.Instance.GameLoop.GetListOfDebugShapesToDraw();
				shapes.Clear();
				doOnce = false;
				foreach(Vector3 tp in waypoints) {
					shapes.Add(Debug_DrawCheckedPoint(tp));
				}
			}*/
            StreetPathfinder.FillRandomWalkStartingIn(rand, TransportMethod.Driving, waypoints, cell as MapCell, 2f, workingLaneList );
			if (waypoints.Count < 50)
			{
				SetToDefaults(true);
				return;
			}
			Pos = waypoints[0];
			waypointIndex = 1;
			if(Pos == waypoints[1])
			{
				waypointIndex++;
            }

            DriverAccuracy = rand.NextFloat(0.065f)+.005f;
			DriverAccuracy += rand.NextFloat(1) < 0.01 ? .15f : 0; //1% chance of just being drunk of his rocker
			SteeringStiffness = rand.NextFloat(0.2f) + 0.90f - (DriverAccuracy/4);
		}

        protected override bool GetShouldDespawnOnInitIfNoValidPath() => true;

        protected override void SetupRendering(RandomGenerator rand)
		{
			drawingObject = VisDrawingObjectTagTable.Instance.GetRowByID("RoadVehicle").SimpleObjects.GetRandom(rand);
			randomColor = drawingObject.RandomColorType == null ? Color.white : drawingObject.RandomColorType.ColorList.GetRandom(rand).Color;
		}

		protected override void DoChildFrameLogicNoWaypoints(float deltaTime)
		{
            if ( TimeSinceSpawn > 1.5f )
                this.readyForDespawn = true;
        }

		protected override void DoChildFrameLogic(float deltaTime)
		{
			bool willPark = waypointIndex >= waypoints.Count - 3 && waypointIndex < waypoints.Count - 1 && waypoints[waypoints.Count - 1].data == WaypointTask.STOP;
			
			if (willPark)
			{
				readyForDespawn = false;
				FadeDelay = MathA.Max(FadeDelay+deltaTime,0.2f);
				if (FadeDelay > 6)
				{
					waypointIndex += 5;
					readyForDespawn = true;
				}
				return;
			}
		}

		protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRot, float maxDelta)
		{
			return (lookRot, maxDelta);
		}

		protected override void MoveForwards(float dt)
		{
			Speed = Mathf.Lerp(BaseSpeed, 0, (FadeDelay - 0.15f) * 2.25f);
			if (Speed < 0.001f)
			{
				Speed = 0;
				return;
			}
			Speed = Mathf.Lerp(BaseSpeed, BaseSpeed * 2.5f, (Pos.y-0.1f)*2);
			Pos += Forward * dt * Speed * (Steering >= .75 ? 1f : 1.25f);
		}

		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
		{
			try
			{
				if ( (waypoints[waypointIndex] - Pos).sqrMagnitude > 0.01f )
				{
					Quaternion hardDir = Quaternion.LookRotation( waypoints[waypointIndex] - Pos );
					return Quaternion.Angle( Rotation, hardDir ) > 90;
				}
			}catch { } //grr, another one
			return false;
		}

		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
		{
			bool willPark = waypointIndex >= waypoints.Count - 3 && waypointIndex < waypoints.Count - 1 && waypoints[waypoints.Count - 1].data == WaypointTask.STOP;
			return !willPark;
		}

		protected override void DoOnNewWaypoint()
		{
			try
			{
				bool willPark = waypointIndex >= waypoints.Count - 3 && waypointIndex < waypoints.Count - 1 && waypoints[waypoints.Count - 1].data == WaypointTask.STOP;

				if ( !willPark && Engine_Universal.PermanentQualityRandom.NextFloat( 0f, 1f ) < 0.2f )
				{
					Vector2 v2 = MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy / 2;
					WaypointAccuracy = new Vector3( v2.x, 0, v2.y );
				}
				if ( willPark )
				{
					WaypointAccuracy /= 5;
				}

				if ( waypointIndex >= waypoints.Count ) return;

				Vector3 distanceToNext = waypoints[waypointIndex].pos - Pos;
				if ( distanceToNext.GetSquareGroundMagnitude() > 9 ) //3 squared
				{
					this.readyForDespawn = true;
					return;
				}

				if ( VisDebuggingCommon.DoDebug_Pathfinding )
				{
					Vector3 diff = waypoints[waypointIndex].pos - waypoints[waypointIndex - 1];
					if ( diff.magnitude > 4 && MathA.Abs( diff.x ) > 0.1f && MathA.Abs( diff.z ) > 0.1f )
					{
						for ( int i = 0; i < waypoints.Count; i++ )
						{
							VisDebuggingCommon.Debug_DrawCheckedPoint( waypoints[i].pos, Color.red );
						}
						VisDebuggingCommon.Debug_DrawWireframe( waypoints[waypointIndex - 1], 0.3f, Color.white );
						VisDebuggingCommon.Debug_DrawWireframe( waypoints[waypointIndex], 0.3f, Color.magenta );
						VisDebuggingCommon.Debug_DrawLine( waypoints[waypointIndex - 1], waypoints[waypointIndex], Color.red );
						VisDebuggingCommon.Debug_DrawWireframe( this.Pos, 0.4f, Color.blue );
						VisDebuggingCommon.JumpCamera( waypoints[waypointIndex - 1] );
						ArcenDebugging.LogSingleLine( $"ABORT! Next waypoint is invalid! {waypointIndex}/{waypoints.Count} {waypoints[waypointIndex - 1]} -> {waypoints[waypointIndex]}. VisibleFuckeryCheck failed!", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor );
					}
				}
			}
			catch
			{
				this.readyForDespawn = true;
			}
		}

        internal static readonly Vector3 ROAD_VEHICLE_SCALE = Vector3.one / 4.25f;
        internal const float MAX_VEHICLE_DRAW_DIST_STREETS = 200 * 200;
        internal const float VEHICLE_RADIUS = 1f;
        internal const float VEHICLE_HALF_HEIGHT = 0.2f;

        protected override void DoMobRenderInner()
        {
			if (waypointIndex < 0) return;

            if ( InputCaching.SkipDrawing_StreetVehicles )
                return;

            float distSquared = (CameraCurrent.CameraBodyPosition - Pos).GetSquareGroundMagnitude();
            if ( distSquared > MAX_VEHICLE_DRAW_DIST_STREETS )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return; //out of range, too far to bother drawing.  This is a perfectly valid case!
            }

            if ( !CameraCurrent.TestFrustumColliderInternalFast( Pos, VEHICLE_HALF_HEIGHT, VEHICLE_RADIUS ) )
            {
                FrameBufferManagerData.IndividualFrustumCullCount.Construction++;
                return;
            }

            Color drawColor = randomColor;
            if ( !this.IsFullyFadedIn )
                drawColor.a = this.AlphaAmountFromFadeIn;

            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;
			if ( !this.CalculateEfficientIsInVisualRange() )
				rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, ROAD_VEHICLE_SCALE, RenderColorStyle.FogOfWar,
					drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, true );
			else
                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, ROAD_VEHICLE_SCALE, RenderColorStyle.SelfColor,
                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, true );
        }

        public override void SpawnFadingOutCopyOfSelf()
        {
            if ( !this.CalculateEfficientIsInVisualRange() )
                return;

            if ( InputCaching.SkipDrawing_StreetVehicles )
                return;

            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
            this.SpawnFadingOutObjectAtCurrentLocation( rGroup, 3f, randomColor, RenderColorStyle.SelfColor, ROAD_VEHICLE_SCALE );
        }
    }
}

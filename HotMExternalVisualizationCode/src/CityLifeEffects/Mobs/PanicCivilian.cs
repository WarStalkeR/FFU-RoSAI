//using Arcen.Universal;
//using System;
//using UnityEngine;
//using Arcen.HotM.Core;
//using Arcen.HotM.Visualization;

//namespace Arcen.HotM.ExternalVis.CityLifeEffects
//{
//	public class PanicCivilian : AbstractPedestrian, IConcurrentPoolable<PanicCivilian>, IProtectedListable
//	{
//		private const float CUST_SPEED_ADJUSTMENT = 4f / 9f;
//		private const float CUST_STEERING_ADJUSTMENT = 3.5f / 5f;

//		private MapItem FleeFromLocation;

//		#region Pooling
//		private static ReferenceTracker RefTracker;
//		private PanicCivilian()
//		{
//			if (RefTracker == null)
//				RefTracker = new ReferenceTracker("PanicCivilians");
//			RefTracker.IncrementObjectCount();
//		}

//		private static readonly ConcurrentPool<PanicCivilian> Pool = new ConcurrentPool<PanicCivilian>("PanicCivilians",
//				KeepTrackOfPooledItems.Yes_AndRefillTheMainListWithThatOnGameRestart, PoolBehaviorDuringShutdown.BlockAllThreads, delegate { return new PanicCivilian(); });


//		/*
//		 * PanicCivilian ped = CityLifeVis.GameSim.SpawnStreetMob(cell, () =>
//		 * {
//		 *		return PanicCivilian.GetFromPoolOrCreate(attackedBuilding);
//		 * });
//		 */
//		public static PanicCivilian GetFromPoolOrCreate(MapItem fleeTarget)
//		{
//			PanicCivilian p = Pool.GetFromPoolOrCreate();
//			p.FleeFromLocation = fleeTarget;
//			return p;
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
//			waypoints?.Clear();
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

//        public override void ChildInit(RandomGenerator rand, MapCell icell)
//		{
//			if (!(icell is MapCell cell)) return;

//			/*if (Engine_HotM.IsInBattleCamMode || cell.CalculateIsConsideredInBattleZone())
//			{
//				StreetPathfinder.FleeBuildingRandom(rand, waypoints, FleeFromLocation, cell);
//				WaypointWaitTime = rand.NextFloat(8);
//			}
//			else
//			{
//				StreetPathfinder.FleeSingleBuilding(rand, TransportMethod.Walking, waypoints, FleeFromLocation, cell);
//			}*/
//			//waypoints.Add(new Vector3(FleeFromLocation.CenterPoint.x, Pos.y, FleeFromLocation.CenterPoint.z));
//			StreetPathfinder.FleeBuildingRandom(rand, waypoints, FleeFromLocation, cell);
//			WaypointWaitTime = rand.NextFloat(4)-0.2f;

//			if (waypoints.Count < 4)
//			{
//				SetToDefaults(true);
//				return;
//			}
//			Speed = BaseSpeed * CUST_SPEED_ADJUSTMENT;
//			DriverAccuracy = rand.NextFloat(0.03f) + .007f;
//			DriverAccuracy += rand.NextFloat(1) < 0.01 ? .2f : 0; //1% chance of just being drunk of his rocker
//			SteeringStiffness = rand.NextFloat(0.2f) + 1.35f - DriverAccuracy;
//			//PanicCivilians tend to just stay where they are and not meander back and forth
//			Vector2 v2 = MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy * 2;
//			WaypointAccuracy = new Vector3(v2.x, 0, v2.y);
//		}

//		public override bool ShouldSelfRespawn()
//		{
//			return false;
//		}

//		public override void JustStandThere(RandomGenerator rand, Vector3 loc, Vector3 target)
//		{
//			throw new InvalidOperationException("PanicCivilians do not support JustStandThere");
//		}

//		internal static readonly Vector3 PANIC_CIVILIAN_SCALE = Vector3.one / 3;

//		public override void DoRender()
//		{
//			if (waypointIndex < 0) return;

//            if ( InputCaching.SkipDrawing_Pedestrians )
//                return;

//            Color drawColor = Color.white;
//			if (!this.IsFullyFadedIn)
//				drawColor.a = this.AlphaAmountFromFadeIn;

//            A5RendererGroup rGroup = drawingObject.RendererGroup as A5RendererGroup;

//            if ( this.IsFullyFadedIn )
//                rGroup?.WriteToDrawBufferForOneFrame_BasicNoColor( Pos, Rotation, PANIC_CIVILIAN_SCALE, RenderColorStyle.NoColor );
//            else
//                rGroup?.WriteToDrawBufferForOneFrame_BasicColor( Pos, Rotation, PANIC_CIVILIAN_SCALE, RenderColorStyle.NoColor,
//                    drawColor, IsFullyFadedIn ? RenderOpacity.Normal : RenderOpacity.Transparent_Batched, false );
//        }

//		public override void SpawnFadingOutCopyOfSelf()
//        {
//            if ( !this.CalculateEfficientIsInVisualRange() )
//                return;
//            if ( InputCaching.SkipDrawing_Pedestrians )
//                return;
//            A5RendererGroup rGroup = drawingObject?.RendererGroup as A5RendererGroup;
//			this.SpawnFadingOutObjectAtCurrentLocation(rGroup, 3f, Color.white, RenderColorStyle.NoColor, PANIC_CIVILIAN_SCALE);
//		}
//	}
//}

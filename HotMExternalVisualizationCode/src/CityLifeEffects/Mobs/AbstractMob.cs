using Arcen.HotM.Core;
using Arcen.HotM.External;
using Arcen.HotM.Visualization;
using Arcen.Universal;
using System;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public abstract class AbstractMob : IStreetMob
	{
		public readonly Int64 AbstractMobUniqueNonSimID = 0;

		private static Int64 lastAbstractMobUniqueNonSimID = 0;

		protected AbstractMob( string Type, int WaypointStart, int WaypointGrowth )
		{
            waypoints = List<Waypoint>.Create_WillNeverBeGCed( WaypointStart, "CityLife-" + Type + "-Waypoints", WaypointGrowth );

            AbstractMobUniqueNonSimID = Interlocked.Increment( ref lastAbstractMobUniqueNonSimID );
        }

        protected const float BaseSpeed = 1.1176f;//~25 mph
		protected const float FixedDeltaTime = 1 / 40f;

        public float TimeSinceSpawn { get; protected set; }
		public Vector3 Pos { get; protected set; }
		//public float Fade { get; protected set; }
		public Vector3 Forward { get; protected set; }
		public Quaternion Rotation { get; protected set; } = Quaternion.identity;
		public float Speed { get; protected set; }
		protected float VerticalSpeed = BaseSpeed;
		protected bool isInPool;

		protected float FadeDelay = 0;
		protected float Steering;
		protected float DriverAccuracy = 1;
		protected float SteeringStiffness;
		protected Vector3 WaypointAccuracy;
		protected float WaypointWaitTime = 10;

		protected readonly List<Waypoint> waypoints;
		protected int waypointIndex = 0;
		protected bool canFly = false;
		protected bool isFlying = false;

		public void InitializeMobFromBackgroundThread( RandomGenerator rand, MapCell cell )
		{
			readyToUse = false;
            FadeDelay = -1;
			Steering = 0;
			Speed = BaseSpeed;
			TimeSinceSpawn = 0.02f;
			WaypointAccuracy = Vector3.zero;
			WaypointWaitTime = 0;

            ChildMobInit( rand, cell );
			SetupRendering(rand);

			bool hadWaypoints = false;
			if ( waypoints.Count > 0 )
			{
                hadWaypoints = true;
                Pos = waypoints[0];
				waypointIndex = 1;
				WaypointWaitTime = waypoints[0].data == WaypointTask.WAIT ? 3 : WaypointWaitTime;

				IsFullyFadedIn = false;
				AlphaAmountFromFadeIn = 0;
				FadeInSpeed = 0.6f;

				Forward = Vector3.forward;
				(Quaternion lookRot, float maxDelta) = GetBaseRotationParams( waypoints[1], 5000f ); //using infinity may make it wrap
				RotateToFace( lookRot, maxDelta );
			}

			//for threading reasons, this must be done last!
			if ( hadWaypoints )
				readyForDespawn = false;
			else
			{
				if ( this.GetShouldDespawnOnInitIfNoValidPath() )
					readyForDespawn = true; //prevent them from fading in and then back out again if no waypoints
			}

            //if we have things to do, let the first task know we're on our way
            /*for (int j = 0; j < waypoints.Count; j++)
			{
				if (waypoints[j].data == WaypointTask.EVENT)
				{
					waypoints[j].transitionCallback(this, Pos);
					break;
				}
			}*/
        }

		protected abstract void SetupRendering(RandomGenerator rand);

		#region CalculateCellFromCurrentPos
		//the below variables do not need to be cleaned up at any point, they can just persist as stale as long as needed.
		private MapCell _lastCalcCell = null;
        private Vector3 _lastCalcGPPos = Vector3.negativeInfinity;
        private ArcenGroundPoint _lastCalcGroundPoint = ArcenGroundPoint.OutOfRange;
		public MapCell CalculateCellFromCurrentPos()
		{
			Vector3 pos = this.Pos;
			ArcenGroundPoint groundPt;
            if ( _lastCalcCell != null )
			{
				if ( pos == this._lastCalcGPPos )
					return _lastCalcCell;

                groundPt = CityMap.CalculateWorldCellPointAtWorldPoint( pos );
				if ( groundPt == _lastCalcGroundPoint )
					return _lastCalcCell;
            }
            else
                groundPt = CityMap.CalculateWorldCellPointAtWorldPoint( pos );

			this._lastCalcGPPos = pos;
			this._lastCalcGroundPoint = groundPt;
			this._lastCalcCell = CityMap.TryGetExistingCellAtLocation( groundPt );
			return this._lastCalcCell;
        }
        #endregion

        #region CalculateEfficientIsInVisualRange
        public bool CalculateEfficientIsInVisualRange()
		{
            MapCell cell = this.CalculateCellFromCurrentPos();
            if ( cell != null && !cell.ShouldHaveExtended2xCityVehiclesRightNow )
				return false; //this is too far to even consider
            if ( SimCommon.IsFogOfWarDisabled )
                return true;

            return VisibilityGranterCalculator.CalculateEfficientIsOutOfFogOfWar_Basic( cell.ParentTile?.FogOfWarCuttersWithinRange?.GetDisplayList(), this.Pos );
        }
        #endregion

        protected abstract bool GetShouldDespawnOnInitIfNoValidPath();

        protected abstract void ChildMobInit( RandomGenerator rand, MapCell cell );
		public abstract void ReturnMobToPool();
		public abstract bool GetInPoolStatus();
        public abstract void DoBeforeRemoveOrClear();

		public bool IsReadyForDespawn()
		{
			return readyForDespawn;
		}

		public bool IsFullySpawned()
		{
			return IsFullyFadedIn;
		}

		public void MarkAsReadyToUse()
		{
			this.readyToUse = true;
		}

        public virtual bool ShouldReturnToPoolOnWorldCellRemoved()
		{
			return true;
		}

		public bool ShouldRenderBeyondBounds()
		{
			return false;
		}

		public void DoMobPerFrame()
		{
			if ( this.readyForDespawn || !this.readyToUse )
				return;

            if ( this.isInPool )
			{
				//ArcenDebugging.LogWithStack( "Tried to call DoPerFrame on a mob that was added back to its pool!", Verbosity.ShowAsError );
				return;
			}

			switch ( Engine_HotM.GameMode)
			{
				case MainGameMode.Streets:
					break;
				default: //take no actions and do no drawing in all the other modes
					return;
			}

            float dt = ArcenTime.UnpausedDeltaTime * SimCommon.CurrentVisualSpeed * SimCommon.BackgroundAnimationMoveSpeed;

            if (waypointIndex < 0 || waypoints.Count == 0 || waypointIndex > waypoints.Count - 1)
            {
                this.readyForDespawn = true;
                return;
			}
			while (dt > 0)
			{
				float idt = MathA.Min(dt, FixedDeltaTime);
				WaypointWaitTime -= idt;
				DoPerFrameInternal(idt);
				if (WaypointWaitTime < 0)
				{
					if (!VisDebuggingCommon.DoDebug_HaultMovement)
						MoveForwards(idt);
				}
				DoChildFrameLogic(idt);
				dt -= FixedDeltaTime;
			}

			if (waypointIndex > 0 && !canFly)
			{
				// hill hugging
				try
				{
					float t = Vector3.Distance( Pos, GetCurrentWaypointPos() ) / Vector3.Distance( waypoints[waypointIndex - 1] + WaypointAccuracy, waypoints[waypointIndex] + WaypointAccuracy );
					if ( t <= 1 )
					{
						float yVal = Mathf.Lerp( waypoints[waypointIndex - 1].y, waypoints[waypointIndex].y, 1 - t );
						Pos = new Vector3( Pos.x, yVal, Pos.z );
					}
				}
				catch { }
			}

			if ( !this.IsFullyFadedIn )
			{
				this.AlphaAmountFromFadeIn += ArcenTime.UnpausedDeltaTime * this.FadeInSpeed;
				if ( this.AlphaAmountFromFadeIn >= 1f )
					this.IsFullyFadedIn = true;
            }
		}

		public void DoMobRender()
        {
            if ( this.readyForDespawn || !this.readyToUse )
                return;
            if ( this.isInPool )
                return;

            switch ( Engine_HotM.GameMode )
            {
                case MainGameMode.Streets:
                    break;
                default: //take no actions and do no drawing in all the other modes
                    return;
            }

            if ( CityMap.WorldBounds_Rect.ContainsPointXZ( Pos ) || ShouldRenderBeyondBounds() )
            {
                if ( SimCommon.ShouldCityLifeAnimate )
                    DoMobRenderInner();
            }
        }

        protected abstract void DoMobRenderInner();

        protected float FadeInSpeed = 0.6f;
        protected bool IsFullyFadedIn = false;
        protected float AlphaAmountFromFadeIn = 0;
		protected bool readyForDespawn = false;
        protected bool readyToUse = false;

        protected abstract void DoChildFrameLogic(float deltaTime);
		protected abstract void DoChildFrameLogicNoWaypoints(float deltaTime);

		private void DoPerFrameInternal(float deltaTime) {
			TimeSinceSpawn += deltaTime;
			if (waypointIndex > waypoints.Count - 1 || GetInPoolStatus() || waypointIndex < 0)
			{
				readyForDespawn = true;
                return;
			}
			if (waypointIndex == 0) waypointIndex++;
			Vector3 wp = GetCurrentWaypointPos();

			(Quaternion lookRot, float maxDelta) = GetBaseRotationParams(wp, deltaTime);
			(lookRot, maxDelta) = ChildMutateRotationParams(wp, deltaTime, lookRot, maxDelta);
			RotateToFace(lookRot, maxDelta);

			if (GetAtNextWaypoint())
			{
				DoOnWaypointArrivalInternal();
				waypointIndex++;
				DoOnNewWaypoint();
			}
		}

		protected void DoOnWaypointArrivalInternal()
		{
			try
			{
				//ArcenDebugging.LogSingleLine($"At event with waypoint index {waypointIndex}, {waypoints[waypointIndex].data}.", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
				if ( waypoints[waypointIndex].data == WaypointTask.WAIT )
				{
					WaypointWaitTime = 3;
				}
				//if we just *got to* a task location
				else if ( waypoints[waypointIndex].data == WaypointTask.EVENT )
				{
					bool moreWaypoints = false;
					//figure out if we have more future tasks
					for ( int j = waypointIndex + 1; j < waypoints.Count; j++ )
					{
						if ( waypoints[j].data == WaypointTask.EVENT )
						{
							//ArcenDebugging.LogSingleLine($"At event with waypoint index {waypointIndex}, future event found at {j}. Task not yet done.", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
							moreWaypoints = true;
							break;
						}
					}
					//tell the current task location we showed up
					WaypointWaitTime = DoOnWaypointArrival( waypoints[waypointIndex].arriveCallback, !moreWaypoints );
				}
				//if we either are just starting or have just *left* a task location...
				else if ( waypointIndex <= 1 || waypoints[waypointIndex - 1].data == WaypointTask.EVENT )
				{
					//check all future waypoints...
					for ( int j = waypointIndex + 1; j < waypoints.Count; j++ )
					{
						//if one is a task location...
						if ( waypoints[j].data == WaypointTask.EVENT )
						{
							//tell it we're on our way
							waypoints[j].transitionCallback( this, Pos );
							//and not the ones after it
							break;
						}
					}
				}
			}
			catch
			{
				this.readyForDespawn = true;
			}
		}

		protected virtual float DoOnWaypointArrival(Waypoint.OnArrive callback, bool taskComplete)
		{
			return callback(this, Pos, taskComplete);
		}

		protected abstract void DoOnNewWaypoint();

		protected bool GetAtNextWaypoint()
		{
			try
			{
				Vector3 pt = GetCurrentWaypointPos();
				if ( waypointIndex < 0 || waypointIndex >= waypoints.Count ) return true;
				Waypoint waypoint = waypoints[waypointIndex];
				if ( waypoint.data == WaypointTask.ONGROUND )
				{
					isFlying = false;
					return true;
				}
				if ( waypoint.data == WaypointTask.VERTICAL )
				{
					return ContinueToNextWaypointEarly( pt ) || MathA.Abs( Pos.y - waypoint.y ) < VerticalSpeed / 30 && ContinueToNextWaypointLate( pt );
				}
				return ContinueToNextWaypointEarly( pt ) || ((pt - Pos).sqrMagnitude < (Speed + DriverAccuracy) / 30 && ContinueToNextWaypointLate( pt ));
			}
			catch
			{
				return false;
			}
		}

		protected abstract bool ContinueToNextWaypointEarly(Vector3 nextPoint);
		protected abstract bool ContinueToNextWaypointLate(Vector3 nextPoint);

		protected Vector3 GetCurrentWaypointPos()
		{
			try
			{
				//if(waypoints[waypointIndex].data == WaypointTask.VERTICAL)
				//{
				//	//avoid any inaccuracy offsets on both sides, all we care about is getting to the correct height.
				//	//return new Vector3(Pos.x, waypoints[waypointIndex].y, Pos.z);
				//}
				if ( waypointIndex < 0 || waypointIndex >= waypoints.Count ) return Pos;
				return waypoints[waypointIndex] + (FadeDelay < 0.01f ? WaypointAccuracy : Vector3.zero);
			}
			catch
			{
				return this.Pos;
            }
		}

		protected abstract (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRot, float maxDelta);

		protected (Quaternion lookRot, float maxDelta) GetBaseRotationParams(Vector3 targetPoint, float deltaTime)
		{
			Vector3 weAreHere = Pos;
			if (FadeDelay > .5f)
			{
				if (FadeDelay > 1f) return (Rotation, 0);
				targetPoint = waypoints[waypoints.Count - 1];
				weAreHere = waypoints[waypoints.Count - 2];
			}
			Vector3 targetLook = targetPoint - weAreHere;
            //targetLook.y = 0f; //we DO care about rotation in the y axis: breaching pods, bridges, etc

            if (targetLook.sqrMagnitude <= 0.01f) return (Rotation, 0); //if the range is really short, don't rotate us; it will give us invalid rotations anyway, and cause error cascades
            targetLook.Normalize(); //if we have non-zero magnitude, this should result in a non-zero vector.
			Quaternion lookRot = Quaternion.LookRotation(targetLook, Vector3.up);

			float angleA = Mathf.Atan2(Forward.x, Forward.z) * Mathf.Rad2Deg;
            float angleB = Mathf.Atan2(targetLook.x, targetLook.z) * Mathf.Rad2Deg;
            float angleDiff = MathA.Abs(Mathf.DeltaAngle(angleA, angleB));

			if (angleDiff > .05 && Steering < 30)
			{
				Steering += .1f * SteeringStiffness;
			}
			else if (Steering > 30)
			{
				Steering -= .1f * SteeringStiffness;
			}

			if (angleDiff > 2.5 && Steering < 90)
			{
				Steering += .5f * SteeringStiffness;
			}
			else if (Steering > 90)
			{
				Steering -= .5f * SteeringStiffness;
			}

			if (angleDiff > 30 && Steering < 180)
			{
				Steering += 15f;
			}
			else if (Steering > 180)
			{
				Steering -= 15f;
			}

			if (angleDiff < .05 && Steering > 0)
			{
				Steering -= 1f * SteeringStiffness;
			}
			return (lookRot, Steering * deltaTime);
		}

		protected void RotateToFace(Quaternion lookRot, float maxDelta)
        {
			if ( lookRot.x == float.NaN )
				return; //don't try to rotate to this!

			if ( maxDelta > 900 || !Engine_Universal.CalculateIsCurrentThreadMainThread() ) //if we want to get there so bad, just get there; or if we're on a bg thread
				Rotation = lookRot;
			else
			{
				Rotation = Quaternion.RotateTowards( Rotation, lookRot, maxDelta );
				if ( Rotation.x == float.NaN ) //if we got a NaN result, then just make us the lookRot
					Rotation = lookRot;
			}

            Forward = Rotation * Vector3.forward;
		}

		protected virtual void MoveForwards(float dt)
		{
			if (Speed < 0.001f) return;
			if(waypointIndex >= 0 && waypoints[waypointIndex].data == WaypointTask.VERTICAL)
			{
				if (!canFly) throw new System.Exception("Non-flying vehicle trying to fly!");
				float sign = Mathf.Sign(waypoints[waypointIndex].y - Pos.y);
				Pos += Vector3.up * dt * VerticalSpeed * (Steering >= .75 ? 1f : 1.25f) * sign;
				if (sign > 0 && !isFlying) isFlying = true;
				return;
			}
			Pos += Forward * dt * Speed * (Steering >= .75 ? 1f : 1.25f);
		}

		protected static float ConvertDegToRad(float degrees)
		{
			return Mathf.PI / 180 * degrees;
		}

		public abstract void SpawnFadingOutCopyOfSelf();

		private float LastTimeSpawnedFadingOutCopy = 0f;

        #region SpawnFadingOutObjectAtCurrentLocation
        protected void SpawnFadingOutObjectAtCurrentLocation( IA5RendererGroup Group, float FadeOutSpeed, Color BaseColor, RenderColorStyle ColorStyle, Vector3 Scale )
		{
			if ( Group == null )
				return; //this happens, mainly on startup

			if ( ArcenTime.AnyTimeSinceStartF - this.LastTimeSpawnedFadingOutCopy < 3f )
				return; //prevent spamming of these, which was happening
			this.LastTimeSpawnedFadingOutCopy = ArcenTime.AnyTimeSinceStartF;

            Vector3 pos = this.Pos;
			MapCell cell = CityMap.TryGetWorldCellAtCoordinates( pos );
			if ( cell == null )
				return; //this is valid to happen; we might be out of where any cells are.

			MapFadingItem fadingItem = MapFadingItem.GetFromPoolOrCreate();

			fadingItem.Position = pos;
			fadingItem.Rotation = this.Rotation;
			fadingItem.Scale = Scale;

			fadingItem.RendererGroup = Group;
			fadingItem.ColorStyle = ColorStyle;

			fadingItem.FadeOutSpeed = FadeOutSpeed;
			fadingItem.BaseColor = BaseColor;

			float speed = this.Speed;
            if ( speed > 0.01f || FadeDelay > 0)
			{
				fadingItem.HasRemainingMotion = true;
				fadingItem.RemainingMotionDirectionAndSpeed = speed * this.Forward;
            }

            MapEffectCoordinator.AddMapFadingItem( cell, fadingItem );
        }
        #endregion
    }
}

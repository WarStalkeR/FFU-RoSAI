using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.HotM.Core;
using Arcen.HotM.Visualization;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public abstract class AbstractPedestrian : AbstractMob
	{
        protected AbstractPedestrian( string Type ) : base( Type, 2, 2 )
        {
        }

        protected const float SPEED_ADJUSTMENT = 1f / 3f;
		protected const float STEERING_ADJUSTMENT = 3f / 5f;//slower moving objects don't need as rapid of direction changes.

		protected VisSimpleDrawingObject drawingObject;
		public VisDrawingObjectTag Tag;

        protected override void SetupRendering(RandomGenerator rand)
		{
			if ( this.Tag == null )
				drawingObject = CommonRefs.Person.SimpleObjects.GetRandom(rand);
            else
                drawingObject = this.Tag.SimpleObjects.GetRandom( rand );
        }

		public abstract void JustStandThere(RandomGenerator rand, Vector3 loc, Vector3 target);

		protected override void DoChildFrameLogicNoWaypoints(float deltaTime)
		{
			if ( TimeSinceSpawn > 1.5f )
				this.readyForDespawn = true;
        }

		protected override void DoChildFrameLogic(float deltaTime)
		{

		}

		protected override void DoOnNewWaypoint()
		{
			if (DriverAccuracy >= 0.2f && Engine_Universal.PermanentQualityRandom.NextFloat( 0, 1f ) < 0.2f)
			{
				Vector2 v2 = MathA.GetRandomNormalizedVector2InsideCircle( Engine_Universal.PermanentQualityRandom, 1f ) * DriverAccuracy;
				WaypointAccuracy = new Vector3(v2.x, 0, v2.y);
			}
		}

		protected override bool ContinueToNextWaypointEarly(Vector3 nextPoint)
		{
			return false;
		}

		protected override bool ContinueToNextWaypointLate(Vector3 nextPoint)
		{
			return (nextPoint - Pos).sqrMagnitude < 0.09 + (DriverAccuracy / 2 * SPEED_ADJUSTMENT);
		}

		protected override (Quaternion lookRot, float maxDelta) ChildMutateRotationParams(Vector3 targetPoint, float deltaTime, Quaternion lookRotIn, float maxDelta)
		{
			float angleA = Mathf.Atan2(Forward.x, Forward.z) * Mathf.Rad2Deg;
			float angleB = Mathf.Atan2(targetPoint.x, targetPoint.z) * Mathf.Rad2Deg;
			float angleDiff = MathA.Abs(Mathf.DeltaAngle(angleA, angleB));

			if (angleDiff > 80)
			{
				return (lookRotIn, 5000);
			}

			return (lookRotIn, Steering * deltaTime * STEERING_ADJUSTMENT + deltaTime * 3.5f);
		}
		public ThrowawayList<Waypoint> GetCurrentPath()
		{
			ThrowawayList<Waypoint> list = new ThrowawayList<Waypoint>();
			list.AddRange(waypoints);
			if (waypointIndex > 1)
				list.RemoveRange(0, Mathf.Min(waypointIndex + 1, list.Count));
			return list;
		}

		public Vector3 GetCurrentWaypoint()
		{
			return GetCurrentWaypointPos();
		}
	}
}
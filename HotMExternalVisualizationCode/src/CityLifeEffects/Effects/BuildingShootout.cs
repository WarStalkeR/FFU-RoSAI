using Arcen.HotM.Core;
using Arcen.Universal;
using System;



using System.Threading.Tasks;
using UnityEngine;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	internal static class BuildingShootout
	{
		private static MersenneTwister rand = new MersenneTwister(Engine_Universal.PermanentQualityRandom.Next());

		public static Vector3 GetPositionNearBuilding(MapItem target)
		{
			//if (target == null) return Vector3.zero;
			Vector3 p = target?.CenterPoint ?? Vector3.zero;
			p.y = 0;

			float radius = 0.025f + (target?.OBBCache?.GetExpensiveRadius()??1f);
			
			ArcenPoint np = MathA.GetRandomPointFromCircleCenter(rand, new ArcenPoint((int)(p.x * 1000), (int)(p.z * 1000)), (int)(radius * 1000) + 250, (int)(radius * 1000) + 750);
			p = new Vector3(np.X / 1000f, 0, np.Y / 1000f);
			//ArcenDebugging.LogSingleLine($"From shootout; {p} - {target.CenterPoint}. Building OBB {target.OBBCache.OBB}", Verbosity.DoNotShowButSendToUnityLogEvenOutsideEditor);
			return p;
		}

		//spherical attempt, stashing
		/*Vector3 offset;
		float theta = 2 * Mathf.PI * rand.NextFloat(0, 1);
		float phi = Mathf.Acos(2 * rand.NextFloat(0, 1) - 1);
		//full sphere y = Mathf.Sin(theta) * Mathf.Cos(phi)
		//while this does result in a bias towards the outer edge, that's fine, as that's where most assault
		//forces would be located. The rest end up on top of the building.
		offset = new Vector3(Mathf.Cos(theta) * Mathf.Sin(phi) * radius, 0, Mathf.Cos(phi) * radius);

		if(MathA.Abs(offset.x) < extents.x && MathA.Abs(offset.z) < extents.z)
		{
			offset.y = extents.y * 2;
		}

		p += offset;

		Vector3 d = (p - target.CenterPoint);
		d.y = 0;

		return p;*/
	}
}

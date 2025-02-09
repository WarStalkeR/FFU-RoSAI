using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using Arcen.Universal.Deserialization;
using UnityEngine;

namespace Arcen.HotM.External
{
    /// <summary>
    /// Helper class to make it easier to spawn particles, and to store any needed data about them
    /// </summary>
    public class ParticleSpawner
    {
        private float LastTimeSpawnedParticle = 0f;

        public VisSimpleDrawingObject smoke2DTrailObject;
        public VisSimpleDrawingObject smoke3DTrailObject;

        #region Spawn2DSmokeStyleParticleAtCurrentLocation
        public void Spawn2DSmokeStyleParticleAtCurrentLocation( Vector3 Pos, float RequiredSecondsGapBetweenEmissions,
            float ParticleTimeToLive, Vector3 Scale, float ScaleChangeDirectionAndSpeed )
        {
            IA5RendererGroup Group = smoke2DTrailObject?.RendererGroup;
            if ( Group == null )
                return; //this happens, mainly on startup

            if ( ArcenTime.UnpausedTimeSinceStartF - this.LastTimeSpawnedParticle < RequiredSecondsGapBetweenEmissions / (float)SimCommon.CurrentVisualSpeed )
                return; //prevent spamming of these every frame
            this.LastTimeSpawnedParticle = ArcenTime.UnpausedTimeSinceStartF;

            Vector3 pos = Pos;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( pos );
            if ( cell == null )
                return; //this is valid to happen; we might be out of where any cells are.

            MapParticle_FadeAndScale smokeParticle = MapParticle_FadeAndScale.GetFromPoolOrCreate();

            smokeParticle.Position = pos;
            smokeParticle.Rotation = Quaternion.identity; //literally does not matter, it rotates to face the camera
            smokeParticle.Scale = Scale;
            smokeParticle.ScaleChangeSpeed = new Vector3( ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed );
            smokeParticle.AlphaFadeIsExponential = true;

            smokeParticle.RendererGroup = Group;
            smokeParticle.BaseColor = Color.white;

            smokeParticle.SetTimeToLive( ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed );

            MapEffectCoordinator.AddMapParticle( cell, smokeParticle );
        }
        #endregion

        #region Spawn3DSmokeStyleParticleAtCurrentLocation
        public void Spawn3DSmokeStyleParticleAtCurrentLocation( Vector3 Pos, Color Color1, Color Color2, int ChanceOfHavingSecondItem, float RequiredSecondsGapBetweenEmissions,
            float ParticleTimeToLive, Vector3 Scale1, Vector3 Scale2, float ScaleChangeDirectionAndSpeed, float PositionJitter )
        {
            IA5RendererGroup Group = smoke3DTrailObject?.RendererGroup;
            if ( Group == null )
                return; //this happens, mainly on startup

            if ( ArcenTime.UnpausedTimeSinceStartF - this.LastTimeSpawnedParticle < RequiredSecondsGapBetweenEmissions / (float)SimCommon.CurrentVisualSpeed )
                return; //prevent spamming of these every frame
            this.LastTimeSpawnedParticle = ArcenTime.UnpausedTimeSinceStartF;

            Vector3 pos = Pos;
            MapCell cell = CityMap.TryGetWorldCellAtCoordinates( pos );
            if ( cell == null )
                return; //this is valid to happen; we might be out of where any cells are.

            {
                MapParticle_FadeAndScale smokeParticle = MapParticle_FadeAndScale.GetFromPoolOrCreate();

                smokeParticle.Position = Helper_GetRandomPositionJitter( pos, PositionJitter );
                smokeParticle.Rotation = Helper_GetRandomRotation(); //this matters a lot, since it's a 3D object
                smokeParticle.Scale = Scale1 * Engine_Universal.PermanentQualityRandom.NextFloat( 0.95f, 1.05f );
                smokeParticle.ScaleChangeSpeed = new Vector3( ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed );
                smokeParticle.AlphaFadeIsExponential = true;

                smokeParticle.RendererGroup = Group;
                smokeParticle.BaseColor = Color1;

                smokeParticle.SetTimeToLive( ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed );

                MapEffectCoordinator.AddMapParticle( cell, smokeParticle );
            }

            if ( Engine_Universal.PermanentQualityRandom.Next( 0, 100 ) <= ChanceOfHavingSecondItem )
            {
                MapParticle_FadeAndScale smokeParticle = MapParticle_FadeAndScale.GetFromPoolOrCreate();

                smokeParticle.Position = Helper_GetRandomPositionJitter( pos, PositionJitter );
                smokeParticle.Rotation = Helper_GetRandomRotation(); //this matters a lot, since it's a 3D object
                smokeParticle.Scale = Scale2 * Engine_Universal.PermanentQualityRandom.NextFloat( 0.95f, 1.05f );
                smokeParticle.ScaleChangeSpeed = new Vector3( ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed, ScaleChangeDirectionAndSpeed );
                smokeParticle.AlphaFadeIsExponential = true;

                smokeParticle.RendererGroup = Group;
                smokeParticle.BaseColor = Color2;

                smokeParticle.SetTimeToLive( ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed );

                MapEffectCoordinator.AddMapParticle( cell, smokeParticle );
            }
        }
		#endregion

		public void SpawnTracerAtCurrentLocation(Vector3 Pos, Vector3 Pos2, Color Color1, Color Color2, int ChanceOfHavingSecondItem, float RequiredSecondsGapBetweenEmissions,
			float ParticleTimeToLive, Vector3 Scale1, Vector3 Scale2, Quaternion rotation, float PositionJitter)
		{
			IA5RendererGroup Group = smoke3DTrailObject?.RendererGroup;
			if (Group == null)
				return; //this happens, mainly on startup

			if (ArcenTime.UnpausedTimeSinceStartF - this.LastTimeSpawnedParticle < RequiredSecondsGapBetweenEmissions / (float)SimCommon.CurrentVisualSpeed)
				return; //prevent spamming of these every frame
			this.LastTimeSpawnedParticle = ArcenTime.UnpausedTimeSinceStartF;

			Vector3 pos = Pos;
			Vector3 pos2 = Pos2;
			MapCell cell = CityMap.TryGetWorldCellAtCoordinates(pos);
			if (cell == null)
				return; //this is valid to happen; we might be out of where any cells are.

			{
				/*MapParticle_LinearMotion smokeParticle = MapParticle_LinearMotion.GetFromPoolOrCreate();
				cell.Particles.Add(smokeParticle);

				smokeParticle.Position = Helper_GetRandomPositionJitter(pos, PositionJitter);
				smokeParticle.Rotation = rotation;
				smokeParticle.Scale = Scale1 * Engine_Universal.PermanentQualityRandom.NextFloat(0.95f, 1.05f);
				//smokeParticle.ScaleChangeSpeed = Vector3.zero;
				//smokeParticle.AlphaFadeIsExponential = true;
				smokeParticle.MotionDirectionAndSpeed = (rotation * Vector3.forward) * 5;

				smokeParticle.RendererGroup = Group;
				smokeParticle.BaseColor = Color1;

				smokeParticle.SetTimeToLive(ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed);*/
				MapParticle_Line smokeParticle = MapParticle_Line.GetFromPoolOrCreate();

				smokeParticle.Position = Helper_GetRandomPositionJitter(pos, PositionJitter);
				smokeParticle.Rotation = rotation;
				smokeParticle.Scale = Scale1 * Engine_Universal.PermanentQualityRandom.NextFloat(0.95f, 1.05f);
				//smokeParticle.ScaleChangeSpeed = Vector3.zero;
				//smokeParticle.AlphaFadeIsExponential = true;
				//smokeParticle.MotionDirectionAndSpeed = (rotation * Vector3.forward) * 5;
				smokeParticle.start = pos;
				smokeParticle.end = pos2;

				smokeParticle.RendererGroup = Group;
				smokeParticle.BaseColor = Color1;

				smokeParticle.SetTimeToLive(ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed);

                MapEffectCoordinator.AddMapParticle( cell, smokeParticle );
            }

			/*if (Engine_Universal.PermanentQualityRandom.Next(0, 100) <= ChanceOfHavingSecondItem)
			{
				MapParticle_FadeAndScale smokeParticle = MapParticle_FadeAndScale.GetFromPoolOrCreate();
				cell.Particles.Add(smokeParticle);

				smokeParticle.Position = Helper_GetRandomPositionJitter(pos, PositionJitter);
				smokeParticle.Rotation = Helper_GetRandomRotation(); //this matters a lot, since it's a 3D object
				smokeParticle.Scale = Scale2 * Engine_Universal.PermanentQualityRandom.NextFloat(0.95f, 1.05f);
				smokeParticle.ScaleChangeSpeed = Vector3.zero;
				smokeParticle.AlphaFadeIsExponential = true;

				smokeParticle.RendererGroup = Group;
				smokeParticle.BaseColor = Color2;

				smokeParticle.SetTimeToLive(ParticleTimeToLive / (float)SimCommon.CurrentVisualSpeed);
			}*/
		}

		private static Quaternion Helper_GetRandomRotation()
        {
            return Quaternion.Euler( Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360 ),
                Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360 ),
                Engine_Universal.PermanentQualityRandom.NextFloat( 0, 360 ) );
        }

        private static Vector3 Helper_GetRandomPositionJitter( Vector3 pos, float PositionJitter )
        {
            return new Vector3( pos.x + Engine_Universal.PermanentQualityRandom.NextFloat( -PositionJitter, PositionJitter ),
                pos.y + Engine_Universal.PermanentQualityRandom.NextFloat( -PositionJitter, PositionJitter ),
                pos.z + Engine_Universal.PermanentQualityRandom.NextFloat( -PositionJitter, PositionJitter ) );
        }
    }
}

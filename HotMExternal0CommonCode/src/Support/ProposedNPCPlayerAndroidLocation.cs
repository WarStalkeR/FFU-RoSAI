using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.External
{
    public class ProposedNPCPlayerAndroidLocation : ICollidable
    {
        public NPCUnitType UnitType;
        public Vector3 Position;
        public float RotationY;

        public ProposedNPCPlayerAndroidLocation( NPCUnitType UnitType, Vector3 Position, float RotationY )
        {
            this.UnitType = UnitType;
            this.Position = Position;
            this.RotationY = RotationY;
        }

        public string DebugText { get; set; } = string.Empty;

        public bool GetEquals( ICollidable other )
        {
            return false;
        }

        public float GetHalfHeightForCollisions()
        {
            return this.UnitType.HalfHeightForCollisions;
        }

        public Vector3 GetBottomCenterPosition()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCollisions()
        {
            Vector3 loc = this.Position;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            loc = loc.PlusY( this.UnitType?.HalfHeightForCollisions ?? 0 ).PlusY( this.UnitType?.YOffsetForCollisionBase??0 );
            return loc;
        }

        public Vector3 GetCollisionCenter()
        {
            return this.GetPositionForCollisions();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            Vector3 loc = TheoreticalPoint;
            if ( (this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0) > 0 )
                loc = loc.ReplaceY( this.UnitType?.EntireObjectAlwaysThisHeightAboveGround ?? 0 );
            loc = loc.PlusY( this.UnitType?.HalfHeightForCollisions ?? 0 ).PlusY( this.UnitType?.YOffsetForCollisionBase ?? 0 );
            return loc;
        }

        public float GetRadiusForCollisions()
        {
            return this.UnitType.RadiusForCollisions;
        }

        public float GetSquaredRadiusForCollisions()
        {
            return this.UnitType.RadiusSquaredForCollisions;
        }

        public float GetRotationYForCollisions()
        {
            return this.RotationY;
        }

        public float GetExtraRadiusBufferWhenTestingForNew()
        {
            return this.UnitType.ExtraRadiusBufferWhenTestingForNew;
        }

        public bool GetShouldHideIntersectingDecorations()
        {
            return false;
        }

        public List<SubCollidable> GetSubCollidablesOrNull()
        {
            return this.UnitType.SubCollidables;
        }

        public string GetCollidableTypeID()
        {
            return "[proposal]";
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return this.UnitType?.DestroyIntersectingBuildingsStrength ?? 0;
        }
    }
}
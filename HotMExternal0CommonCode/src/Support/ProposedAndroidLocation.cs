using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.External
{
    public class ProposedAndroidLocation : ICollidable
    {
        public MachineUnitType UnitType;
        public Vector3 Position;
        public float RotationY;

        public ProposedAndroidLocation( MachineUnitType UnitType, Vector3 Position, float RotationY )
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
            return this.Position.ReplaceY( this.UnitType.HalfHeightForCollisions ).PlusY( this.UnitType.YOffsetForCollisionBase ); //mechs are always at position 0
        }

        public Vector3 GetCollisionCenter()
        {
            return this.GetPositionForCollisions();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            return TheoreticalPoint.ReplaceY( this.UnitType.HalfHeightForCollisions ).PlusY( this.UnitType.YOffsetForCollisionBase );  //mechs are always at position 0
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
            return 0; //for now
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
            return this.UnitType?.DestroyIntersectingBuildingsStrength??0;
        }
    }
}
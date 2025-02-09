using System;
using Arcen.HotM.Core;
using Arcen.Universal;
using UnityEngine;

namespace Arcen.HotM.External
{
    public class ProposedVehicleLocation : ICollidable
    {
        public MachineVehicleType VehicleType;
        public Vector3 Position;

        public ProposedVehicleLocation( MachineVehicleType VehicleType, Vector3 Position )
        {
            this.VehicleType = VehicleType;
            this.Position = Position;
        }

        public string DebugText { get; set; } = string.Empty;

        public bool GetEquals( ICollidable other )
        {
            return false;
        }

        public float GetHalfHeightForCollisions()
        {
            return this.VehicleType.HalfHeightForCollisions;
        }

        public Vector3 GetBottomCenterPosition()
        {
            throw new NotImplementedException();
        }

        public Vector3 GetPositionForCollisions()
        {
            return this.Position.PlusY( this.VehicleType?.YOffsetForCollisionBase??0 );
        }

        public Vector3 GetCollisionCenter()
        {
            return this.GetPositionForCollisions();
        }

        public Vector3 GetPositionForCollisionsFromTheoretic( Vector3 TheoreticalPoint )
        {
            return TheoreticalPoint.PlusY( this.VehicleType?.YOffsetForCollisionBase??0 );
        }

        public float GetRadiusForCollisions()
        {
            return this.VehicleType.RadiusForCollisions;
        }

        public float GetSquaredRadiusForCollisions()
        {
            return this.VehicleType.RadiusSquaredForCollisions;
        }

        public float GetRotationYForCollisions()
        {
            return 0;
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
            return null;
        }

        public string GetCollidableTypeID()
        {
            return "[proposal]";
        }

        public int GetCollidableSmashesBuildingStrength()
        {
            return 0;// this.UnitType?.ShouldDestroyIntersectingBuildings ?? false;
        }
    }
}
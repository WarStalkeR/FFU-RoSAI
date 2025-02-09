using System;
using UnityEngine;
using Arcen.HotM.Core;

namespace Arcen.HotM.ExternalVis.CityLifeEffects
{
	public struct Waypoint
	{
		public delegate float OnArrive(IStreetMob actor, Vector3 pos, bool taskComplete);
		public delegate void OnCancel();
		public delegate void OnNewWaypoint(IStreetMob actor, Vector3 pos);
		public readonly Vector3 pos;
		public readonly WaypointTask data;
		public readonly OnNewWaypoint transitionCallback;
		public readonly OnArrive arriveCallback;
		public readonly OnCancel cancelCallback;

		public Waypoint(Vector3 v3, WaypointTask wpData)
		{
			if (wpData == WaypointTask.EVENT) throw new ArgumentException("Event waypoints require an onArrive callback");
			pos = v3;
			data = wpData;
			transitionCallback = (d,p) => { };
			arriveCallback = null;
			cancelCallback = null;
		}

		public Waypoint(Vector3 v3, WaypointTask wpData, OnNewWaypoint onNewWaypoint, OnArrive onArrive = null, OnCancel onCancel = null) : this()
		{
			if (wpData == WaypointTask.EVENT && onArrive == null) throw new ArgumentException("Event waypoints require an onArrive callback");
			pos = v3;
			data = wpData;
			arriveCallback = onArrive;
			transitionCallback = onNewWaypoint;
			cancelCallback = onCancel;
		}

		public float x
		{
			get => pos.x;
		}
		public float y
		{
			get => pos.y;
		}
		public float z
		{
			get => pos.z;
		}

		public static implicit operator Waypoint(Vector3 v3)
		{
			return new Waypoint(v3, WaypointTask.NONE);
		}

		public static implicit operator Vector3(Waypoint wp)
		{
			return wp.pos;
		}

		public override string ToString()
		{
			return $"{pos} {data}";
		}
	}

	public enum WaypointTask
	{
		NONE, STOP, HOME, VERTICAL, WAIT, EVENT, ONGROUND, DROP_POD
	}
}

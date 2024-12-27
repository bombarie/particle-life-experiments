using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DotSettings", menuName = "ScriptableObjects/DotSettings", order = 1)]
public class DotSettings : ScriptableObject {

	[System.Serializable]
	public struct MinMax {
		public float min;
		public float max;
	}

	public enum DotRepelMethod {
		MY_ORIGINAL = 1,
		OFFICIAL = 2
	}
	public DotRepelMethod dotRepelMethod;

	[Header("init values")]
	public MinMax dotSize;
	public MinMax dotAttractionDistance;

	[Space(10)]
	[Range(0f, 1f)]
	public float globalFriction = .9f;
	public float frictionUpperLimit = .96f;
	public float frictionLowerLimit = .7f;


	[Space(10)]
	public float boundsDeflectMinDistance = 10f;
	public float boundsDeflectPowFactor = 2f;
	public float boundsDeflectStrength = 2f;

	// TODO remove these two -> are now part of InteractSettings struct aka unique per dot type
	[Space(10)]
	public float dotsAttractRange = 90f;
	public float dotsMinDistance = 3f;

	// TODO remove these two -> are now part of InteractSettings struct aka unique per dot type
	[Space(10)]
	[Range(1, 600)]
	public int attractionForce = 1;
	[Range(.1f, 350f)]
	public float proximityRepulseForce = 60f;

	[Space(10)]
	public float maxDotSpeed = 1.5f;

}

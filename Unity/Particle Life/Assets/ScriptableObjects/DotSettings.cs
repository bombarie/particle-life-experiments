using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DotSettings", menuName = "ScriptableObjects/DotSettings", order = 1)]
public class DotSettings : ScriptableObject {

	[Range(0f, 1f)]
	public float globalFriction = .9f;
	public float frictionUpperLimit = .96f;
	public float frictionLowerLimit = .7f;

	[Space(10)]
	public float dotsAttractRange = 90f;
	public float dotsMinDistance = 3f;

	[Space(10)]
	[Range(1, 600)]
	public int attractionForce = 1;
	[Range(.1f, 350f)]
	public float proximityRepulseForce = 60f;

	[Space(10)]
	public float maxDotSpeed = 1.5f;
}

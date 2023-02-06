using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dot : MonoBehaviour {

	private MainScript.DotData data;
	private bool IsSet = false;

	private Material mat;

	void Awake () {
		//GetComponent<Renderer>().material = new Material(GetComponent<Renderer>().sharedMaterial);
		mat = GetComponent<Renderer>().material;
	}

	void Update () {
		if (IsSet) {
			transform.position = data.position;
			transform.localScale = Vector3.one * data.size;
		}
	}

	public void SetData(MainScript.DotData data) {
		this.data = data;
		IsSet = true;
	}

	public void SetColor(Color c) {
		c.a = 1f;
		mat.color = c;
	}

	public MainScript.DotData GetData() {
		return data;
	}
}

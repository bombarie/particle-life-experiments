using UnityEngine;
using System.Collections;

public class BufferExample : MonoBehaviour {
	public Material material;
	ComputeBuffer buffer;

	const int count = 1024;

	const float size = 5.0f;
	void Start () {
		//Debug.Log("SystemInfo.supportsComputeShaders: " + SystemInfo.supportsComputeShaders);
		//Debug.Log("SystemInfo.maxComputeBufferInputsVertex: " + SystemInfo.maxComputeBufferInputsVertex);

		buffer = new ComputeBuffer(count, sizeof(float) * 3, ComputeBufferType.Structured);
		float[] points = new float[count * 3];

		Random.InitState(0);

		for (int i = 0; i < count; i++) {
			points[i * 3 + 0] = Random.Range(-size, size);
			points[i * 3 + 1] = Random.Range(-size, size);
			points[i * 3 + 2] = 0.0f;
		}

		buffer.SetData(points);
	}

	void OnPostRender () {
		if (!material.SetPass(0)) {
			Debug.Log("returned false");
		}
		material.SetBuffer("buffer", buffer);
		Graphics.DrawProceduralNow(MeshTopology.Points, count, 1);
	}

	void OnDestroy () {
		buffer.Release();
	}
}
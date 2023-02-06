using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static MainScript;

public class MainScript : MonoBehaviour {

	public enum CalculationMethod {
		CPU,
		GPU
	}
	public CalculationMethod calculationMethod;

	public enum RenderTarget {
		GAMEOBJECTS,
		PARTICLESYSTEM,
		VFXGRAPH
	}
	public RenderTarget renderTarget;

	public enum ParticleLifeDimensions {
		_2D,
		_3D
	}
	public ParticleLifeDimensions particleLifeDimensions;


	[Space(10)]
	public int numDots = 1500;

	[Space(10)]
	public ParticleSystem ps;
	public VisualEffect vfx;

	[Space(10)]
	public DotSettings dotSettings_script;
	public DotSettings dotSettings_compute;

	[System.Serializable]
	public struct DotData {
		public int colorIndex;
		public Vector3 position;
		public Vector3 speed;
		public float friction;
		public float size;
		public float repelDistance;
		public float attractDistance;

		public static int Size {
			get {
				return
					1 * sizeof(int) +           // int
					2 * 3 * sizeof(float) +     // vector3
					4 * sizeof(float)           // float
					;
			}
		}
	}
	private DotData[] dotsData;
	private List<Dot> dots = new List<Dot>();

	[System.Serializable]
	public struct ColorAttractRepelData {
		public float interactDistance;
		public float interactStrength;
		public float minDistance;
		public float minDistanceRepelStrength;
		public float minDistanceRepelPowFactor;

		public static int Size {
			get {
				return 5 * sizeof(float);
			}
		}
	}
	private ColorAttractRepelData[,] colorAttractAndRepelMatrix;

	private float[,] attractionMatrix = new float[,] {
		  // R    G    B    O    P
		  { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f }, // RED
		  { 0.0f, 1.0f, 0.0f, 0.0f, 0.0f }, // GREEN
		  { 0.0f, 0.0f, 1.0f, 0.0f, 0.0f }, // BLUE
		  { 0.0f, 0.0f, 0.0f, 1.0f, 0.0f }, // ORANGE
		  { 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }    // PURPLE
		};

	private float[] dotSizesPerColor = { 3f, 3f, 3f, 1f, 1f };

	[Space(10)]
	public Color[] dotColors = new Color[5];

	public Vector3 boundsMin = new Vector3(-150f, -100f, 0f);
	public Vector3 boundsMax = new Vector3(150f, 100f, 0f);

	public ComputeShader dotsComputeShader2D;
	public ComputeShader dotsComputeShader3D;
	public ComputeShader dotsComputeShaderGeneric;
	private static int ThreadGroupSize = 1024;

	private float attractionForceDivFactor = 100000f;

	[System.Serializable]
	public struct DotSize {
		public float min;
		public float max;
	}
	[Space(10)]
	public DotSize dotSize;

	[Space(10)]
	public Dot dotPrefab;
	public Transform dotsHolder;

	private GraphicsBuffer dotsPositionBuffer;
	private GraphicsBuffer dotsColorsBuffer;
	private GraphicsBuffer dotsSizeBuffer;
	ComputeBuffer cb;
	ComputeBuffer _colorMatrixBuffer;

	void Start () {

		initDots();

		// init compute buffers
		cb = new ComputeBuffer(dotsData.Length, DotData.Size);
		cb.SetData(dotsData);

		_colorMatrixBuffer = new ComputeBuffer(attractionMatrix.Length, attractionMatrix.Length * sizeof(float));
		updateAttractionRulesComputeBuffer();


		// Shuriken particle syste,
		var main = ps.main;
		main.maxParticles = numDots;

		ps.gameObject.SetActive(renderTarget == RenderTarget.PARTICLESYSTEM);

		Debug.Log("attractionMatrix length: " + attractionMatrix.Length);
	}

	private void OnDestroy () {
		if (cb != null) {
			cb.Release();
		}
		if (_colorMatrixBuffer != null) {
			_colorMatrixBuffer.Release();
		}
	}

	private void initDotsSizes () {
		for (int i = 0; i < dotSizesPerColor.Length; i++) {
			dotSizesPerColor[i] = Random.Range(dotSize.min, dotSize.max);
		}
	}

	private void initDots () {
		if (dotsPositionBuffer != null) dotsPositionBuffer.Dispose();
		if (dotsColorsBuffer != null) dotsColorsBuffer.Dispose();
		if (dotsSizeBuffer != null) dotsSizeBuffer.Dispose();

		dotsData = new DotData[numDots];
		dots = new List<Dot>();
		Dot _dot;

		initDotsSizes();

		/* init attractors per color (new style)
		colorAttractAndRepelMatrix = new ColorAttractRepelData[dotColors.Length, dotColors.Length];
		for (int i = 0; i < dotColors.Length; i++) {
			for (int j = 0; j < dotColors.Length; j++) {

				colorAttractAndRepelMatrix[i, j] = 

			}

		}
		//*/

		dotsPositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numDots, sizeof(float) * 3);
		dotsColorsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numDots, sizeof(float) * 4);
		dotsSizeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, numDots, sizeof(float));

		Vector3[] positions = new Vector3[numDots];
		Color[] colors = new Color[numDots];
		float[] sizes = new float[numDots];

		for (int i = 0; i < numDots; i++) {
			DotData d = new DotData();

			d.colorIndex = Random.Range(0, dotColors.Length);

			d.position = new Vector3(
							Random.Range(boundsMin.x, boundsMax.x),
							Random.Range(boundsMin.y, boundsMax.y),
							Random.Range(boundsMin.z, boundsMax.z)
							);

			if (particleLifeDimensions == ParticleLifeDimensions._2D) {
				d.position.z = 0f;
			}

			//d.size = 2f;
			d.colorIndex = Random.Range(0, 5);
			d.size = dotSizesPerColor[d.colorIndex];
			//d.size = Random.Range(dotSize.min, dotSize.max); // tmp
			d.friction = 0.95f;

			dotsData[i] = d;
			positions[i] = d.position;
			colors[i] = dotColors[d.colorIndex];
			sizes[i] = d.size;

			if (renderTarget == RenderTarget.GAMEOBJECTS) {
				_dot = Instantiate(dotPrefab, dotsHolder);
				_dot.SetData(d);
				_dot.SetColor(dotColors[d.colorIndex]);
				dots.Add(_dot);
			}
		}

		dotsPositionBuffer.SetData(positions);
		dotsColorsBuffer.SetData(colors);
		dotsSizeBuffer.SetData(sizes);

		IsInitted = true;
	}


	private bool IsInitted = false;
	void Update () {

		handleKeyboardInput();

		if (IsInitted) {

			// update Dots
			switch (calculationMethod) {
				case CalculationMethod.CPU:
					calcInScript();
					break;
				case CalculationMethod.GPU:
					calcComputeShaderGeneric();
					//switch (particleLifeDimensions) {
					//	case ParticleLifeDimensions._2D:
					//		calcComputeShader2D();
					//		break;
					//	case ParticleLifeDimensions._3D:
					//		calcComputeShader3D();
					//		break;
					//}
					break;
			}

			//Debug.Log("dots[0] data >> position: " + dots[0].GetData().position + ", speed: " + dots[0].GetData().speed + ", colorIndex: " + dots[0].GetData().colorIndex);

			// Render dots
			switch (renderTarget) {
				case RenderTarget.GAMEOBJECTS:
					updateDotsGameObjects();
					break;
				case RenderTarget.PARTICLESYSTEM:
					// set Dots to ParticleSystem
					updateParticleSystem();
					break;
				case RenderTarget.VFXGRAPH:
					// set Dots to ParticleSystem

					// not needed anymore -> is the binding mechanism more efficient than setting properties, btw?
					//updateVFXGraphTexture2D();

					updateVFXGraphGraphicsBuffer();

					break;
			}
		}


	}

	private void handleKeyboardInput () {

		// randomize all matrix values
		if (Input.GetKeyUp(KeyCode.Alpha1)) {
			attractionMatrix = new float[,] {
											{ Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) },
											{ Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) },
											{ Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) },
											{ Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) },
											{ Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f) }
										};

			// update _colorMatrixBuffer
			updateAttractionRulesComputeBuffer();
		}

		// randomize only one color 
		if (Input.GetKeyUp(KeyCode.Alpha2)) {
			int randCol = Random.Range(0, attractionMatrix.GetLength(0));
			for (int i = 0; i < attractionMatrix.GetLength(1); i++) {
				attractionMatrix[randCol, i] = Random.Range(-1f, 1f);
			}

			// update _colorMatrixBuffer
			updateAttractionRulesComputeBuffer();
		}

		// randomize only one value of one color
		if (Input.GetKeyUp(KeyCode.Alpha3)) {
			attractionMatrix[Random.Range(0, attractionMatrix.GetLength(0)), Random.Range(0, attractionMatrix.GetLength(1))] = Random.Range(-1f, 1f);

			// update _colorMatrixBuffer
			updateAttractionRulesComputeBuffer();
		}

		// reset
		if (Input.GetKeyUp(KeyCode.Space)) {
			IsInitted = false;
			initDots();

			// update computebuffer with new particles definitions
			cb.SetData(dotsData);
		}
	}

	public Color dotIndexToColor (int colorIndex) {
		return dotColors[colorIndex];
	}

	DotData _d1;
	DotData _d2;
	Vector3 v;
	float colorInteraction;
	int i, j;
	private void calcInScript () {
		for (i = 0; i < dotsData.Length; i++) {
			_d1 = dotsData[i];
			for (j = 0; j < dotsData.Length; j++) {
				_d2 = dotsData[j];

				if (_d1.Equals(_d2)) continue;

				if (Vector3.Distance(_d1.position, _d2.position) < (_d1.size + dotSettings_script.dotsMinDistance)) {
					v = _d2.position - _d1.position;
					v = v.normalized;

					_d1.speed += v * (-dotSettings_script.proximityRepulseForce * dotSettings_script.attractionForce / attractionForceDivFactor);
				} else if (Vector3.Distance(_d1.position, _d2.position) < dotSettings_script.dotsAttractRange) {
					colorInteraction = GetColorInteractionByIndex(_d1.colorIndex, _d2.colorIndex);

					v = _d2.position - _d1.position;
					v = v.normalized;

					_d1.speed += v * (colorInteraction * dotSettings_script.attractionForce / attractionForceDivFactor); // replace '1f' with colorForce
				}
			}

			// update positions and apply friction
			_d1.position += _d1.speed;
			//_d1.speed *= _d1.friction;
			_d1.speed *= dotSettings_script.globalFriction;


			// constrain to world bounds
			if (_d1.position.x < boundsMin.x) {
				_d1.speed.x *= -1.0f;
				_d1.position.x = boundsMin.x;
			}
			if (_d1.position.y < boundsMin.y) {
				_d1.speed.y *= -1.0f;
				_d1.position.y = boundsMin.y;
			}

			if (_d1.position.x > boundsMax.x) {
				_d1.speed.x *= -1.0f;
				_d1.position.x = boundsMax.x;
			}
			if (_d1.position.y > boundsMax.y) {
				_d1.speed.y *= -1.0f;
				_d1.position.y = boundsMax.y;
			}

			dotsData[i] = _d1;
		}
	}

	private float GetColorInteractionByIndex (int p1, int p2) {
		//float f = 0f;

		//f = attractionMatrix[p1, p2];

		return attractionMatrix[p1, p2]; ;
	}

	private void calcComputeShader2D () {
		int kernelID = dotsComputeShader2D.FindKernel("CSMain");

		//ComputeBuffer cb = new ComputeBuffer(dotsData.Length, DotData.Size);
		//cb.SetData(dotsData);
		dotsComputeShader2D.SetBuffer(kernelID, "DotsBuffer", cb);
		dotsComputeShader2D.SetInt("numDots", dotsData.Length);

		ComputeBuffer _colorMatrixBuffer = new ComputeBuffer(attractionMatrix.Length, attractionMatrix.Length * sizeof(float));
		float[] colMatrix = new float[attractionMatrix.Length];
		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				colMatrix[i + j * attractionMatrix.GetLength(1)] = attractionMatrix[i, j];
			}
		}
		_colorMatrixBuffer.SetData(colMatrix);
		dotsComputeShader2D.SetBuffer(kernelID, "colorMatrixBuffer", _colorMatrixBuffer);
		dotsComputeShader2D.SetInt("numColors", 5);

		dotsComputeShader2D.SetFloat("minRange", dotSettings_compute.dotsMinDistance);
		dotsComputeShader2D.SetFloat("maxRange", dotSettings_compute.dotsAttractRange);

		dotsComputeShader2D.SetFloat("maxDotSpeed", dotSettings_compute.maxDotSpeed);

		dotsComputeShader2D.SetFloat("worldBoundsMinX", boundsMin.x);
		dotsComputeShader2D.SetFloat("worldBoundsMinY", boundsMin.y);
		dotsComputeShader2D.SetFloat("worldBoundsMaxX", boundsMax.x);
		dotsComputeShader2D.SetFloat("worldBoundsMaxY", boundsMax.y);

		dotsComputeShader2D.SetFloat("proximityRepulseForce", dotSettings_compute.proximityRepulseForce);
		dotsComputeShader2D.SetFloat("attractionForce", dotSettings_compute.attractionForce);
		dotsComputeShader2D.SetFloat("attractionForceDivFactor", attractionForceDivFactor);
		dotsComputeShader2D.SetFloat("globalFriction", dotSettings_compute.globalFriction);

		int threadGroups = Mathf.CeilToInt(numDots / (float)ThreadGroupSize);
		dotsComputeShader2D.Dispatch(kernelID, threadGroups, 1, 1);


		DotData[] d = new DotData[dotsData.Length];
		cb.GetData(d);

		for (int i = 0; i < dotsData.Length; i++) {
			dotsData[i] = d[i];

			//* update positions
			dotsData[i].position += dotsData[i].speed;
			// DotsBuffer[id.x].speed *= DotsBuffer[id.x].friction;
			dotsData[i].speed *= dotSettings_compute.globalFriction;

			//*/

			//* constrain to world
			if (dotsData[i].position.x < boundsMin.x) {
				dotsData[i].speed.x *= -1f;
				dotsData[i].position.x = boundsMin.x;
			}
			if (dotsData[i].position.y < boundsMin.y) {
				dotsData[i].speed.y *= -1f;
				dotsData[i].position.y = boundsMin.y;
			}

			if (dotsData[i].position.x > boundsMax.x) {
				dotsData[i].speed.x *= -1f;
				dotsData[i].position.x = boundsMax.x;
			}
			if (dotsData[i].position.y > boundsMax.y) {
				dotsData[i].speed.y *= -1f;
				dotsData[i].position.y = boundsMax.y;
			}
			dotsData[i].position.z = 0f; // necessary to constrain?

		}

		//cb.Release();
		_colorMatrixBuffer.Release();
	}

	private void calcComputeShader3D () {
		int kernelID = dotsComputeShader3D.FindKernel("CSMain");

		dotsComputeShader3D.SetBuffer(kernelID, "DotsBuffer", cb);
		dotsComputeShader3D.SetBuffer(kernelID, "DotsPositionBuffer", dotsPositionBuffer);
		dotsComputeShader3D.SetInt("numDots", dotsData.Length);

		dotsComputeShader3D.SetBuffer(kernelID, "colorMatrixBuffer", _colorMatrixBuffer);
		dotsComputeShader3D.SetInt("numColors", 5);


		dotsComputeShader3D.SetFloat("minRange", dotSettings_compute.dotsMinDistance);
		dotsComputeShader3D.SetFloat("maxRange", dotSettings_compute.dotsAttractRange);

		dotsComputeShader3D.SetFloat("maxDotSpeed", dotSettings_compute.maxDotSpeed);

		dotsComputeShader3D.SetFloat("proximityRepulseForce", dotSettings_compute.proximityRepulseForce);
		dotsComputeShader3D.SetFloat("attractionForce", dotSettings_compute.attractionForce);
		dotsComputeShader3D.SetFloat("attractionForceDivFactor", attractionForceDivFactor);
		dotsComputeShader3D.SetFloat("globalFriction", dotSettings_compute.globalFriction);

		int threadGroups = Mathf.CeilToInt(numDots / (float)ThreadGroupSize);
		dotsComputeShader3D.Dispatch(kernelID, threadGroups, 1, 1);




		kernelID = dotsComputeShader3D.FindKernel("CalcPositions");

		dotsComputeShader3D.SetFloat("worldBoundsMinX", boundsMin.x);
		dotsComputeShader3D.SetFloat("worldBoundsMinY", boundsMin.y);
		dotsComputeShader3D.SetFloat("worldBoundsMinZ", boundsMin.z);
		dotsComputeShader3D.SetFloat("worldBoundsMaxX", boundsMax.x);
		dotsComputeShader3D.SetFloat("worldBoundsMaxY", boundsMax.y);
		dotsComputeShader3D.SetFloat("worldBoundsMaxZ", boundsMax.z);

		dotsComputeShader3D.SetFloat("globalFriction", dotSettings_compute.globalFriction);

		dotsComputeShader3D.SetBuffer(kernelID, "DotsPositionBuffer", dotsPositionBuffer);
		dotsComputeShader3D.SetBuffer(kernelID, "DotsBuffer", cb);
		dotsComputeShader3D.Dispatch(kernelID, threadGroups, 1, 1);

		//DotData[] d = new DotData[dotsData.Length];
		//cb.GetData(dotsData);
		//dotsData
		/*

		for (int i = 0; i < dotsData.Length; i++) {
			dotsData[i] = d[i];

			// update positions
			dotsData[i].position += dotsData[i].speed;
			// DotsBuffer[id.x].speed *= DotsBuffer[id.x].friction;
			dotsData[i].speed *= dotSettings_compute.globalFriction;

			// constrain to world
			if (dotsData[i].position.x < boundsMin.x) {
				dotsData[i].speed.x *= -1f;
				dotsData[i].position.x = boundsMin.x;
			}
			if (dotsData[i].position.y < boundsMin.y) {
				dotsData[i].speed.y *= -1f;
				dotsData[i].position.y = boundsMin.y;
			}
			if (dotsData[i].position.z < boundsMin.z) {
				dotsData[i].speed.z *= -1f;
				dotsData[i].position.z = boundsMin.z;
			}

			if (dotsData[i].position.x > boundsMax.x) {
				dotsData[i].speed.x *= -1f;
				dotsData[i].position.x = boundsMax.x;
			}
			if (dotsData[i].position.y > boundsMax.y) {
				dotsData[i].speed.y *= -1f;
				dotsData[i].position.y = boundsMax.y;
			}
			if (dotsData[i].position.z > boundsMax.z) {
				dotsData[i].speed.z *= -1f;
				dotsData[i].position.z = boundsMax.z;
			}
		}
		//*/

		//cb.Release();
		//_colorMatrixBuffer.Release();
	}

	private void calcComputeShaderGeneric () {
		int kernelID = dotsComputeShaderGeneric.FindKernel("CSMain");

		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsBuffer", cb);
		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsPositionBuffer", dotsPositionBuffer);
		dotsComputeShaderGeneric.SetInt("numDots", dotsData.Length);

		dotsComputeShaderGeneric.SetBuffer(kernelID, "colorMatrixBuffer", _colorMatrixBuffer);
		dotsComputeShaderGeneric.SetInt("numColors", 5);


		dotsComputeShaderGeneric.SetFloat("minRange", dotSettings_compute.dotsMinDistance);
		dotsComputeShaderGeneric.SetFloat("maxRange", dotSettings_compute.dotsAttractRange);

		dotsComputeShaderGeneric.SetFloat("maxDotSpeed", dotSettings_compute.maxDotSpeed);

		dotsComputeShaderGeneric.SetFloat("deltaTime", Time.deltaTime);
		dotsComputeShaderGeneric.SetFloat("proximityRepulseForce", dotSettings_compute.proximityRepulseForce);
		dotsComputeShaderGeneric.SetFloat("attractionForce", dotSettings_compute.attractionForce);
		dotsComputeShaderGeneric.SetFloat("attractionForceDivFactor", attractionForceDivFactor);
		dotsComputeShaderGeneric.SetFloat("globalFriction", dotSettings_compute.globalFriction);

		int threadGroups = Mathf.CeilToInt(numDots / (float)ThreadGroupSize);
		dotsComputeShaderGeneric.Dispatch(kernelID, threadGroups, 1, 1);



		switch (particleLifeDimensions) {
			case ParticleLifeDimensions._2D:
				kernelID = dotsComputeShaderGeneric.FindKernel("CalcPositions2D");
				break;
			case ParticleLifeDimensions._3D:
				kernelID = dotsComputeShaderGeneric.FindKernel("CalcPositions3D");
				break;
		}

		dotsComputeShaderGeneric.SetFloat("worldBoundsMinX", boundsMin.x);
		dotsComputeShaderGeneric.SetFloat("worldBoundsMinY", boundsMin.y);
		dotsComputeShaderGeneric.SetFloat("worldBoundsMinZ", boundsMin.z);
		dotsComputeShaderGeneric.SetFloat("worldBoundsMaxX", boundsMax.x);
		dotsComputeShaderGeneric.SetFloat("worldBoundsMaxY", boundsMax.y);
		dotsComputeShaderGeneric.SetFloat("worldBoundsMaxZ", boundsMax.z);

		dotsComputeShaderGeneric.SetFloat("deltaTime", Time.deltaTime);
		dotsComputeShaderGeneric.SetFloat("globalFriction", dotSettings_compute.globalFriction);

		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsPositionBuffer", dotsPositionBuffer);
		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsBuffer", cb);
		dotsComputeShaderGeneric.Dispatch(kernelID, threadGroups, 1, 1);

		// retrieve the calculated only if not using VfxGraph as target
		if (renderTarget != RenderTarget.VFXGRAPH) {
			cb.GetData(dotsData);
		}
	}

	private ParticleSystem.Particle[] m_Particles;
	private ParticleSystem.Particle p;
	private ParticleSystem.MainModule psMain;
	private int numParticlesAlive;
	private void updateParticleSystem () {
		psMain = ps.main;

		if (m_Particles == null || m_Particles.Length == 0) {
			m_Particles = new ParticleSystem.Particle[psMain.maxParticles];
		}
		numParticlesAlive = ps.GetParticles(m_Particles);

		for (int i = 0; i < numParticlesAlive; i++) {
			if (i < dotsData.Length) {
				p = m_Particles[i];

				p.position = dotsData[i].position;
				p.startSize = dotsData[i].size;
				p.startColor = dotColors[dotsData[i].colorIndex];

				m_Particles[i] = p;
			}
		}

		ps.SetParticles(m_Particles, numParticlesAlive);
	}

	private void updateVFXGraphTexture2D () {
		// src: https://www.reddit.com/r/Unity3D/comments/dlyihn/controlling_the_positions_of_individual_particles/

		// Create texture
		var dotPosTex = new Texture2D(4096, Mathf.CeilToInt(dotsData.Length / 4096), TextureFormat.ARGB32, false);
		var dotColTex = new Texture2D(4096, Mathf.CeilToInt(dotsData.Length / 4096), TextureFormat.ARGB32, false);

		// Set all of your particle positions in the texture
		var positions = new Color[dotsData.Length];
		var colors = new Color[dotsData.Length];

		// Begin do this on every frame
		for (int i = 0; i < dotsData.Length; i++) {
			positions[i] = new Color(dotsData[i].position.x / 1000f + .5f, dotsData[i].position.y / 1000f + .5f, dotsData[i].position.z / 1000f + .5f, 0);
			//positions[i] = new Color(dotsData[i].position.x, dotsData[i].position.y, dotsData[i].position.z, 0);
			colors[i] = dotColors[dotsData[i].colorIndex];
			colors[i].a = dotsData[i].size / 10f;
		}

		dotPosTex.SetPixels(positions);
		dotColTex.SetPixels(colors);

		dotPosTex.Apply();
		dotColTex.Apply();

		vfx.SetTexture("DotsPositions", dotPosTex);
		vfx.SetTexture("DotsColors", dotColTex);
	}

	private void updateVFXGraphGraphicsBuffer () {
		vfx.SetGraphicsBuffer("DotsPositionsBuffer", dotsPositionBuffer);
		vfx.SetGraphicsBuffer("DotsColorsBuffer", dotsColorsBuffer);
		vfx.SetGraphicsBuffer("DotsSizeBuffer", dotsSizeBuffer);
	}

	private void updateAttractionRulesComputeBuffer () {
		float[] colMatrix = new float[attractionMatrix.Length];
		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				colMatrix[i + j * attractionMatrix.GetLength(1)] = attractionMatrix[i, j];
			}
		}
		_colorMatrixBuffer.SetData(colMatrix);
	}

	private void updateDotsGameObjects () {
		for (int i = 0; i < dotsData.Length; i++) {
			dots[i].SetData(dotsData[i]);
		}
	}

	public GraphicsBuffer GetDotsPositionBuffer () {
		return this.dotsPositionBuffer != null ? this.dotsPositionBuffer : null;
	}
}

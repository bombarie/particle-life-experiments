using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using static MainScript;

public class MainScript : MonoBehaviour {

	public enum CalculationTarget {
		CPU,
		GPU
	}
	public CalculationTarget calculationTarget;

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

	public enum ParticlesInitShape {
		RANDOM_POSITION,
		BIG_BANG
	}
	public ParticlesInitShape particlesInitShape;


	[Space(10)]
	public int numDots = 1500;

	[Space(10)]
	public ParticleSystem ps;
	public VisualEffect vfx;

	[Space(10)]
	public DotSettings dotSettings_script;
	public DotSettings dotSettings_compute;
	private DotSettings dotSettings;

	[System.Serializable]
	public struct DotData {
		public int dotType;
		public Vector3 position;
		public Vector3 speed;

		public static int Size {
			get {
				return
					1 * sizeof(int) +           // int
					2 * 3 * sizeof(float)       // vector3
					;
			}
		}
	}
	private DotData[] dotsData;
	private List<Dot> dots = new List<Dot>();

	[System.Serializable]
	public struct DotType {
		public int colorIndex;
		public float friction;
		public float size;
		public InteractSettings interactSettings;

		public static int Size {
			get {
				return
					1 * sizeof(int) +           // int
					2 * sizeof(float) +         // float
					1 * InteractSettings.Size
					;
			}
		}
	}
	private DotType[] dotTypes;

	[System.Serializable]
	public struct InteractSettings {
		public float minDistance;
		public float minDistanceRepelPowFactor;
		public float minDistanceRepelStrength;
		public float interactDistance;
		public float interactStrength; // range [-1,1]

		public static int Size {
			get {
				return
					5 * sizeof(float)           // float
					;
			}
		}
		public string toString () {
			//return "minDistance: " + minDistance + ", minDistanceRepelStrength: " + minDistanceRepelStrength + ", ";
			return "minDistance: " + minDistance + ", interactDistance: " + interactDistance;
		}
	}

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

	[Header("World bounds")]
	public Vector3 boundsMin = new Vector3(-150f, -100f, 0f);
	public Vector3 boundsMax = new Vector3(150f, 100f, 0f);

	[Space(10)]
	public ComputeShader dotsComputeShaderGeneric;
	private static int ThreadGroupSize = 1024;

	private float attractionForceDivFactor = 100000f;

	[Space(10)]
	public Dot dotPrefab;
	public Transform dotsHolder;

	private GraphicsBuffer dotsPositionBuffer;
	private GraphicsBuffer dotsColorsBuffer;
	private GraphicsBuffer dotsSizeBuffer;
	private ComputeBuffer dots_cb;
	private ComputeBuffer dotTypes_cb;
	private ComputeBuffer colorMatrix_cb;

	private bool IsInitted = false;

	// Shuriken particle system variables
	private ParticleSystem.Particle[] m_Particles;
	private ParticleSystem.Particle p;
	private ParticleSystem.MainModule psMain;
	private int numParticlesAlive;

	[Header("Scene init settings")]
	[Space(10)]
	public float bigbangRandStartPos = 10f;

	// used in calcInScript()
	private DotData _d1;
	private DotData _d2;
	private Vector3 _v;
	private float colorInteraction;
	private int i, j;

	void Start () {
		switch (calculationTarget) {
			case CalculationTarget.CPU:
				dotSettings = dotSettings_script;
				break;
			case CalculationTarget.GPU:
				dotSettings = dotSettings_compute;
				break;
		}

		generateColors();
		initDots();
		initAttractionMatrix();

		// init compute buffers
		dots_cb = new ComputeBuffer(dotsData.Length, DotData.Size);
		dots_cb.SetData(dotsData);

		dotTypes_cb = new ComputeBuffer(dotTypes.Length, DotType.Size);
		dotTypes_cb.SetData(dotTypes);

		// Shuriken particle syste,
		var main = ps.main;
		main.maxParticles = numDots;

		ps.gameObject.SetActive(renderTarget == RenderTarget.PARTICLESYSTEM);

		Debug.Log("attractionMatrix length: " + attractionMatrix.Length);
		//Debug.Log("dotColors length: " + dotColors.Length);
		//Debug.Log("dotTypes length: " + dotTypes.Length);

		/* a few random prints
		for (int i = 0; i < Mathf.Min(10, dotsData.Length); i++) {
			int r = Random.Range(0, dotsData.Length);
			DotData d = dotsData[r];
			DotType dt = dotTypes[d.dotType];
			Debug.Log("Random sampple at index " + r + " >> size: " + dt.size + ", colorIndex: " + dt.colorIndex + ", interactionsettings: " + dt.interactSettings.toString());
		}
		Debug.Log("");
		//*/

		for (i = 0; i < dotColors.Length; i++) {
			Debug.Log("dotColors[" + i + "] = " + dotColors[i] + ", dotTypes[" + i + "].colorIndex = " + dotTypes[i].colorIndex + ", dotTypes[" + i + "].size = " + dotTypes[i].size);
		}
	}

	void Update () {
		switch (calculationTarget) {
			case CalculationTarget.CPU:
				dotSettings = dotSettings_script;
				break;
			case CalculationTarget.GPU:
				dotSettings = dotSettings_compute;
				break;
		}


		handleKeyboardInput();

		if (IsInitted) {

			// update Dots
			switch (calculationTarget) {
				case CalculationTarget.CPU:
					calcInScript();
					break;
				case CalculationTarget.GPU:
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

					// not needed anymore -> is the binding mechanism more efficient than manually setting properties, btw? (I don't think so)
					//updateVFXGraphTexture2D();

					updateVFXGraphGraphicsBuffer();

					break;
			}
		}


	}

	#region control Dots helper functions

	public void randomizeAllAttractionSettings () {
		Debug.Log("f:randomizeAllAttractionSettings()");

		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				attractionMatrix[i, j] = Random.Range(-1f, 1f);
			}
		}

		// update _colorMatrixBuffer
		updateAttractionRulesComputeBuffer();
	}

	public void randomizeAllAttractionSettingsExtreme  () {
		Debug.Log("f:randomizeAllAttractionSettingsExtreme()");

		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				attractionMatrix[i, j] = (Random.Range(0f, 1f) < 0.5f) ? -1f : 1f;
			}
		}

		// update _colorMatrixBuffer
		updateAttractionRulesComputeBuffer();
	}

	public void setEqualishAttractionSettings () {
		Debug.Log("f:setEqualishAttractionSettings()");

		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				attractionMatrix[i, j] = -.35f;
				if (i == j) attractionMatrix[i, j] = 1f;
			}
			attractionMatrix[i, Random.Range(0, attractionMatrix.GetLength(1))] = .65f;
		}

		// update _colorMatrixBuffer
		updateAttractionRulesComputeBuffer();
	}

	public void randomizeSingleAttractionSettings () {
		Debug.Log("f:randomizeSingleAttractionSettings()");

		int randCol = Random.Range(0, attractionMatrix.GetLength(0));
		for (int i = 0; i < attractionMatrix.GetLength(1); i++) {
			attractionMatrix[randCol, i] = Random.Range(-1f, 1f);
		}

		// update colormatrix compute buffer
		updateAttractionRulesComputeBuffer();
	}

	public void randomizeDotFrictions () {
		Debug.Log("f:randomizeDotFrictions()");

		for (int i = 0; i < dotTypes.Length; i++) {
			dotTypes[i].friction = Random.Range(.84f, .985f);
		}
		dotTypes_cb.SetData(dotTypes);
	}

	public void resetScene () {
		Debug.Log("f:resetScene()");

		IsInitted = false;

		generateColors();
		initDots();
		initAttractionMatrix();

		// update computebuffer with new particles definitions
		// init compute buffers
		dots_cb = new ComputeBuffer(dotsData.Length, DotData.Size);
		dots_cb.SetData(dotsData);

		dotTypes_cb = new ComputeBuffer(dotTypes.Length, DotType.Size);
		dotTypes_cb.SetData(dotTypes);
	}

	public void setRepelMethod (int r) {
		dotSettings.dotRepelMethod = (DotSettings.DotRepelMethod)r;
	}

	#endregion

	private void handleKeyboardInput () {

		// randomize all matrix values
		if (Input.GetKeyUp(KeyCode.Alpha1)) {
			randomizeAllAttractionSettings();
		}

		// randomize only one color 
		if (Input.GetKeyUp(KeyCode.Alpha2)) {
			randomizeSingleAttractionSettings();
		}

		// randomize all the frictions
		if (Input.GetKeyUp(KeyCode.Alpha3)) {
			randomizeDotFrictions();
		}

		// randomize all attraction matrix but extreme (-1f or 1f, nothing in between)
		if (Input.GetKeyUp(KeyCode.Alpha4)) {
			randomizeAllAttractionSettingsExtreme();
		}

		// reset attraction matrix and apply a semi-regular repel and attract force (does this create more symmetrical layouts?)
		if (Input.GetKeyUp(KeyCode.Alpha5)) {
			setEqualishAttractionSettings();
		}

		// change color palette
		if (Input.GetKeyUp(KeyCode.Alpha6)) {
			generateColors();

			Color[] colors = new Color[numDots];
			// for now: manual update
			for (int i = 0; i < numDots; i++) {
				colors[i] = dotColors[dotTypes[dotsData[i].dotType].colorIndex];
			}
			dotsColorsBuffer.SetData(colors);
		}

		// reset
		if (Input.GetKeyUp(KeyCode.Space)) {
			resetScene();
		}

		if (Input.GetKeyUp(KeyCode.LeftBracket)) {
			setRepelMethod(1);
			dotSettings.proximityRepulseForce = 15f;
		}
		if (Input.GetKeyUp(KeyCode.RightBracket)) {
			setRepelMethod(2);
			dotSettings.proximityRepulseForce = .8f;
		}
	}

	#region Init

	private void initColorsList() {
		Debug.Log("f:initColorsList");

		int r = Random.Range(3, 6);
		dotColors = new Color[r];
		generateColors();
	}

	public void generateColors () {
		Debug.Log("f:generateColors");

		for (int i = 0; i < dotColors.Length; i++) {
			dotColors[i] = Color.HSVToRGB(Random.Range(0f, 1f), Random.Range(.6f, 1f), Random.Range(.6f, 1f));
		}
	}

	private void initDots () {
		if (dotsPositionBuffer != null) dotsPositionBuffer.Dispose();
		if (dotsColorsBuffer != null) dotsColorsBuffer.Dispose();
		if (dotsSizeBuffer != null) dotsSizeBuffer.Dispose();

		dotsData = new DotData[numDots];
		dots = new List<Dot>();
		Dot _dot;

		createDotTypes();
		initDotsSizes(); // this is old approach

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

			d.dotType = Random.Range(0, dotColors.Length);

			switch (particlesInitShape) {
				case ParticlesInitShape.RANDOM_POSITION:
					d.position = new Vector3(
						Random.Range(boundsMin.x, boundsMax.x),
						Random.Range(boundsMin.y, boundsMax.y),
						Random.Range(boundsMin.z, boundsMax.z)
						);
					break;
				case ParticlesInitShape.BIG_BANG:
					d.position = new Vector3(
						(boundsMin.x + boundsMax.x) / 2f + Random.Range(-bigbangRandStartPos, bigbangRandStartPos),
						(boundsMin.y + boundsMax.y) / 2f + Random.Range(-bigbangRandStartPos, bigbangRandStartPos),
						(boundsMin.z + boundsMax.z) / 2f + Random.Range(-bigbangRandStartPos, bigbangRandStartPos)
						);
					break;
			}

			if (particleLifeDimensions == ParticleLifeDimensions._2D) {
				d.position.z = 0f;
			}

			dotsData[i] = d;
			positions[i] = d.position;
			colors[i] = dotColors[dotTypes[d.dotType].colorIndex];
			sizes[i] = dotTypes[d.dotType].size;

			if (renderTarget == RenderTarget.GAMEOBJECTS) {
				_dot = Instantiate(dotPrefab, dotsHolder);
				_dot.SetData(d, dotTypes[d.dotType]);
				_dot.SetColor(dotColors[d.dotType]);
				dots.Add(_dot);
			}
		}

		dotsPositionBuffer.SetData(positions);
		dotsColorsBuffer.SetData(colors);
		dotsSizeBuffer.SetData(sizes);

		IsInitted = true;
	}

	private void createDotTypes () {

		dotTypes = new DotType[dotColors.Length];

		for (int i = 0; i < dotTypes.Length; i++) {
			dotTypes[i] = new DotType();
			//dotTypes[i].colorIndex = Random.Range(0, dotColors.Length);
			dotTypes[i].colorIndex = i;
			dotTypes[i].size = Random.Range(dotSettings.dotSize.min, dotSettings.dotSize.max);
			dotTypes[i].friction = Random.Range(.84f, .985f);
			dotTypes[i].interactSettings = createInteractSettings(dotTypes[i].size);

		}
	}

	private InteractSettings createInteractSettings (float dotSize) {
		InteractSettings i = new InteractSettings();

		i.minDistance = (dotSize / 2f) + Mathf.Pow(Random.Range(0f, 3.5f), 2f);
		i.minDistanceRepelPowFactor = Random.Range(1f, 4f);
		i.minDistanceRepelStrength = Random.Range(1f, 4.5f);
		i.interactDistance = i.minDistance + Random.Range(dotSettings.dotAttractionDistance.min, dotSettings.dotAttractionDistance.max);
		i.interactStrength = Random.Range(-1f, 1f);

		return i;
	}

	private void initAttractionMatrix () {
		attractionMatrix = new float[dotColors.Length, dotColors.Length];
		for (int i = 0; i < attractionMatrix.GetLength(0); i++) {
			for (int j = 0; j < attractionMatrix.GetLength(1); j++) {
				//attractionMatrix[i, j] = Random.Range(-1f, 1f);
				attractionMatrix[i, j] = 0f;
				if (i == j) attractionMatrix[i, j] = 1f;
			}
		}

		// update _colorMatrixBuffer
		updateAttractionRulesComputeBuffer();
	}

	private void initDotsSizes () {
		for (int i = 0; i < dotSizesPerColor.Length; i++) {
			dotSizesPerColor[i] = Random.Range(dotSettings.dotSize.min, dotSettings.dotSize.max);
		}
	}

	#endregion

	private void calcInScript () {
		int dotsDataLength = dotsData.Length;
		for (i = 0; i < dotsDataLength; i++) {
			_d1 = dotsData[i];
			for (j = 0; j < dotsDataLength; j++) {
				_d2 = dotsData[j];

				if (_d1.Equals(_d2)) continue;

				if (Vector3.Distance(_d1.position, _d2.position) < (dotTypes[_d1.dotType].size + dotSettings_script.dotsMinDistance)) {
					_v = _d2.position - _d1.position;
					_v = _v.normalized;

					_d1.speed += _v * (-dotSettings_script.proximityRepulseForce * dotSettings_script.attractionForce / attractionForceDivFactor);
				} else if (Vector3.Distance(_d1.position, _d2.position) < dotSettings_script.dotsAttractRange) {
					colorInteraction = GetColorInteractionByIndex(dotTypes[_d1.dotType].colorIndex, dotTypes[_d2.dotType].colorIndex);

					_v = _d2.position - _d1.position;
					_v = _v.normalized;

					_d1.speed += _v * (colorInteraction * dotSettings_script.attractionForce / attractionForceDivFactor); // replace '1f' with colorForce
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

	private void calcComputeShaderGeneric () {
		int kernelID = dotsComputeShaderGeneric.FindKernel("CSMain");

		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsBuffer", dots_cb);
		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotTypesBuffer", dotTypes_cb);
		dotsComputeShaderGeneric.SetInt("numDots", dotsData.Length);
		dotsComputeShaderGeneric.SetInt("numDotTypes", dotTypes.Length);

		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsPositionBuffer", dotsPositionBuffer);

		dotsComputeShaderGeneric.SetBuffer(kernelID, "colorMatrixBuffer", colorMatrix_cb);
		dotsComputeShaderGeneric.SetInt("numColors", dotTypes.Length);


		// TODO remove these -> are part of the new InteractSettings struct
		dotsComputeShaderGeneric.SetFloat("minRange", dotSettings_compute.dotsMinDistance);
		dotsComputeShaderGeneric.SetFloat("maxRange", dotSettings_compute.dotsAttractRange);

		dotsComputeShaderGeneric.SetFloat("maxDotSpeed", dotSettings_compute.maxDotSpeed);

		dotsComputeShaderGeneric.SetFloat("boundsDeflectMinDistance", dotSettings_compute.boundsDeflectMinDistance);
		dotsComputeShaderGeneric.SetFloat("boundsDeflectPowFactor", dotSettings_compute.boundsDeflectPowFactor);
		dotsComputeShaderGeneric.SetFloat("boundsDeflectStrength", dotSettings_compute.boundsDeflectStrength);

		dotsComputeShaderGeneric.SetInt("dotRepelMethod", (int)dotSettings_compute.dotRepelMethod);

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
		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotTypesBuffer", dotTypes_cb);
		dotsComputeShaderGeneric.SetBuffer(kernelID, "DotsBuffer", dots_cb);
		dotsComputeShaderGeneric.Dispatch(kernelID, threadGroups, 1, 1);

		// retrieve the calculated only if not using VfxGraph as target
		if (renderTarget != RenderTarget.VFXGRAPH) {
			dots_cb.GetData(dotsData);
		}
	}

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
				p.startSize = dotTypes[dotsData[i].dotType].size;
				p.startColor = dotColors[dotTypes[dotsData[i].dotType].colorIndex]; // haha, this really did 

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
			colors[i] = dotColors[dotTypes[dotsData[i].dotType].colorIndex];
			colors[i].a = dotTypes[dotsData[i].dotType].size / 10f;
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

		colorMatrix_cb = new ComputeBuffer(attractionMatrix.Length, attractionMatrix.Length * sizeof(float));
		colorMatrix_cb.SetData(colMatrix);
	}

	private void updateDotsGameObjects () {
		for (int i = 0; i < dotsData.Length; i++) {
			dots[i].SetData(dotsData[i], dotTypes[dotsData[i].dotType]);
		}
	}

	public GraphicsBuffer GetDotsPositionBuffer () {
		return this.dotsPositionBuffer != null ? this.dotsPositionBuffer : null;
	}

	public Color dotIndexToColor (int colorIndex) {
		return dotColors[colorIndex];
	}


	private void OnDestroy () {

		// ComputeBuffers
		if (dots_cb != null) {
			dots_cb.Release();
		}
		if (dotTypes_cb != null) {
			dotTypes_cb.Release();
		}
		if (colorMatrix_cb != null) {
			colorMatrix_cb.Release();
		}

		// GraphicsBuffers
		if (dotsPositionBuffer != null) {
			dotsPositionBuffer.Release();
		}
		if (dotsColorsBuffer != null) {
			dotsColorsBuffer.Release();
		}
		if (dotsSizeBuffer != null) {
			dotsSizeBuffer.Release();
		}
	}


}

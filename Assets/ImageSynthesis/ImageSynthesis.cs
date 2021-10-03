using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
// @TODO:
// . support custom color wheels in optical flow via lookup textures
// . support custom depth encoding
// . support multiple overlay cameras
// . tests
// . better example scene(s)

// @KNOWN ISSUES
// . Motion Vectors can produce incorrect results in Unity 5.5.f3 when
//      1) during the first rendering frame
//      2) rendering several cameras with different aspect ratios - vectors do stretch to the sides of the screen

[RequireComponent (typeof(Camera))]
public class ImageSynthesis : MonoBehaviour {
    
    public RenderTexture cubemap1;
    public RenderTexture cubemap2;
    public string FileName;
    public int i=0;
    public int j=0;
    public RenderTexture equirect;
    public bool renderStereo = true;
    public float stereoSeparation = 0.064f;
	// pass configuration
	private CapturePass[] capturePasses = new CapturePass[] {
		new CapturePass() { name = "_img" },
		new CapturePass() { name = "_id", supportsAntialiasing = false },
		new CapturePass() { name = "_layer", supportsAntialiasing = false },
		new CapturePass() { name = "_depth" },
		new CapturePass() { name = "_normals" },
		new CapturePass() { name = "_flow", supportsAntialiasing = false, needsRescale = true } // (see issue with Motion Vectors in @KNOWN ISSUES)
	};

	struct CapturePass {
		// configuration
		public string name;
		public bool supportsAntialiasing;
		public bool needsRescale;
		public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; }

		// impl
		public Camera camera;
	};
	
	public Shader uberReplacementShader;
	public Shader opticalFlowShader;

	public float opticalFlowSensitivity = 1.0f;

	// cached materials
	private Material opticalFlowMaterial;

	void Start()
	{
		// default fallbacks, if shaders are unspecified
		if (!uberReplacementShader)
			uberReplacementShader = Shader.Find("Hidden/UberReplacement");

		if (!opticalFlowShader)
			opticalFlowShader = Shader.Find("Hidden/OpticalFlow");

		// use real camera to capture final image
		capturePasses[0].camera = GetComponent<Camera>();
		for (int q = 1; q < capturePasses.Length; q++)
			capturePasses[q].camera = CreateHiddenCamera (capturePasses[q].name);

		OnCameraChange();
		OnSceneChange();
	}

	void LateUpdate()
	{

		OnCameraChange();
	}
	
	private Camera CreateHiddenCamera(string name)
	{
		var go = new GameObject (name, typeof (Camera));
		go.hideFlags = HideFlags.HideAndDontSave;
		go.transform.parent = transform;

		var newCamera = go.GetComponent<Camera>();
		return newCamera;
	}


	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode)
	{
		SetupCameraWithReplacementShader(cam, shader, mode, Color.black);
	}

	static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, ReplacelementModes mode, Color clearColor)
	{
		var cb = new CommandBuffer();
		cb.SetGlobalFloat("_OutputMode", (int)mode); // @TODO: CommandBuffer is missing SetGlobalInt() method
		cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
		cam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
		cam.SetReplacementShader(shader, "");
		cam.backgroundColor = clearColor;
		cam.clearFlags = CameraClearFlags.SolidColor;
	}

	static private void SetupCameraWithPostShader(Camera cam, Material material, DepthTextureMode depthTextureMode = DepthTextureMode.None)
	{
		var cb = new CommandBuffer();
		cb.Blit(null, BuiltinRenderTextureType.CurrentActive, material);
		cam.AddCommandBuffer(CameraEvent.AfterEverything, cb);
		cam.depthTextureMode = depthTextureMode;


	}

	enum ReplacelementModes {
		ObjectId 			= 0,
		CatergoryId			= 1,
		DepthCompressed		= 2,
		DepthMultichannel	= 3,
		Normals				= 4
	};

	public void OnCameraChange()
	{
		int targetDisplay = 1;
		var mainCamera = GetComponent<Camera>();
		foreach (var pass in capturePasses)
		{
			if (pass.camera == mainCamera)
				continue;

			// cleanup capturing camera
			pass.camera.RemoveAllCommandBuffers();

			// copy all "main" camera parameters into capturing camera
			pass.camera.CopyFrom(mainCamera);

			// set targetDisplay here since it gets overriden by CopyFrom()
			pass.camera.targetDisplay = targetDisplay++;
            
		}

		// cache materials and setup material properties
		if (!opticalFlowMaterial || opticalFlowMaterial.shader != opticalFlowShader)
			opticalFlowMaterial = new Material(opticalFlowShader);
		opticalFlowMaterial.SetFloat("_Sensitivity", opticalFlowSensitivity);

		// setup command buffers and replacement shaders
		SetupCameraWithReplacementShader(capturePasses[1].camera, uberReplacementShader, ReplacelementModes.ObjectId);
		SetupCameraWithReplacementShader(capturePasses[2].camera, uberReplacementShader, ReplacelementModes.CatergoryId);
		SetupCameraWithReplacementShader(capturePasses[3].camera, uberReplacementShader, ReplacelementModes.DepthCompressed, Color.white);
		SetupCameraWithReplacementShader(capturePasses[4].camera, uberReplacementShader, ReplacelementModes.Normals);
		SetupCameraWithPostShader(capturePasses[5].camera, opticalFlowMaterial, DepthTextureMode.Depth | DepthTextureMode.MotionVectors);

		for (j=1;j<6;j++)
		{
			capturePasses[j].camera.stereoSeparation = stereoSeparation;
            capturePasses[j].camera.RenderToCubemap(cubemap1, 63, Camera.MonoOrStereoscopicEye.Left);
            capturePasses[j].camera.RenderToCubemap(cubemap2, 63, Camera.MonoOrStereoscopicEye.Right);
            cubemap1.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
            cubemap2.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);
            Texture2D tempTexture = new Texture2D(equirect.width, equirect.height);
            RenderTexture currentActiveRT = RenderTexture.active;
            RenderTexture.active = equirect;
            tempTexture.ReadPixels(new Rect(0, 0, equirect.width, equirect.height), 0, 0);
            //for (i=0 ; i < 3 ; i++)
            //{
            var bytes = tempTexture.EncodeToJPG();
            System.IO.File.WriteAllBytes(string.Format("hello{0}_{1}.jpg", capturePasses[j], j), bytes);
            //}
            // Restores the active render texture
            RenderTexture.active = currentActiveRT;
            
		}

	}


	public void OnSceneChange()
	{
		var renderers = Object.FindObjectsOfType<Renderer>();
		var mpb = new MaterialPropertyBlock();
		foreach (var r in renderers)
		{
			var id = r.gameObject.GetInstanceID();
			var layer = r.gameObject.layer;
			var tag = r.gameObject.tag;

			mpb.SetColor("_ObjectColor", ColorEncoding.EncodeIDAsColor(id));
			mpb.SetColor("_CategoryColor", ColorEncoding.EncodeLayerAsColor(layer));
			r.SetPropertyBlock(mpb);
		}
	}

}

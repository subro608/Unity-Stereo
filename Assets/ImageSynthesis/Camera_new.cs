using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;


//attach this script to your camera object
public class Camera_new : MonoBehaviour
{
    public RenderTexture cubemap1;
    public RenderTexture cubemap2;
    public string FileName;
    public int i=0;
    public RenderTexture equirect;
    public bool renderStereo = true;
    public float stereoSeparation = 0.064f;

    void LateUpdate()
    {
        Camera cam = GetComponent<Camera>();

        if (cam == null)
        {
            cam = GetComponentInParent<Camera>();
        }

        if (cam == null)
        {
            Debug.Log("stereo 360 capture node has no camera or parent camera");
        }

        if (renderStereo)
        {
            cam.stereoSeparation = stereoSeparation;
            cam.RenderToCubemap(cubemap1, 63, Camera.MonoOrStereoscopicEye.Left);
            cam.RenderToCubemap(cubemap2, 63, Camera.MonoOrStereoscopicEye.Right);
        }
        else
        {
            cam.RenderToCubemap(cubemap1, 63, Camera.MonoOrStereoscopicEye.Mono);
        }

        //optional: convert cubemaps to equirect

        if (equirect == null)
            return;

        cubemap1.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Left);
        cubemap2.ConvertToEquirect(equirect, Camera.MonoOrStereoscopicEye.Right);

        Texture2D tempTexture = new Texture2D(equirect.width, equirect.height);

        // Copies EquirectTexture into the tempTexture
        RenderTexture currentActiveRT = RenderTexture.active;
        RenderTexture.active = equirect;
        tempTexture.ReadPixels(new Rect(0, 0, equirect.width, equirect.height), 0, 0);

        // Exports to a JPG
        if (i < 3)
        {
            var bytes = tempTexture.EncodeToJPG();
            System.IO.File.WriteAllBytes(string.Format("hello{0}_{1}.jpg", FileName, i++), bytes);
        }
        // Restores the active render texture
        RenderTexture.active = currentActiveRT;

    }
}



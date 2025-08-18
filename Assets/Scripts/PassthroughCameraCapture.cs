using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using PassthroughCameraSamples;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PassthroughCameraCapture : MonoBehaviour
{
    [SerializeField] private WebCamTextureManager webCamTextureManager;
    [SerializeField] private TextMeshProUGUI webCamTextureDebugInfo;
    [SerializeField] private TextMeshProUGUI webCamDeviceDebugInfo;
    [SerializeField] private RawImage webCamRawImage;
    
    
    [SerializeField] private TextMeshProUGUI buttonDebugText;
    [SerializeField] private AudioSource cameraShutter;
    private int pressCount = 0;
    
    [SerializeField] private SAM2Api SAM2Api;
    
    private Texture2D snapshot1;
    private Texture2D snapshot2;
    private List<Texture2D> snapshots;
    private List<int[]> values;
    
    private Texture2D result; // Texture for resized images
    
    private const int targetWidth = 512; // Target width for resized images
    private const int targetHeight = 512; // Target height for resized images
    
    // Update is called once per frame
    void Update()
    {
        // Example: Spacebar for PC, replace with OVRInput for Quest trigger
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            pressCount++;
            Debug.Log("A button pressed!");
            buttonDebugText.text = $"BUTTON PRESSED x{pressCount.ToString()}";
            // DoAction();
            
            CaptureFrame();
        }
    }

    private void CaptureFrame()
    {
        Debug.Log("CaptureFrame");
        WebCamTexture camTex = webCamTextureManager.WebCamTexture;
        Debug.Log("Set webcam texture");
        if (camTex != null && camTex.isPlaying)
        {
            // Copy pixels into snapshot
            snapshot1.SetPixels(camTex.GetPixels());
            snapshot1.Apply();
            Debug.Log("Webcam texture to Texture 2d");
            //snapshot2.SetPixels(camTex.GetPixels());
            //snapshot2.Apply();

            // Show the snapshot on the RawImage
            webCamRawImage.texture = snapshot1;
            //webCamRawImage.texture = snapshot2;
            
            // Obtain snapshots and values lists
            snapshots = new List<Texture2D>
            {
                //snapshot1,
                //snapshot2
                resizeTexture(snapshot1, targetWidth, targetHeight)
            };
            values = new List<int[]>
            {
                new [] { 231, 231, 281, 281 }
                //new [] { 640, 480, 880, 600 }
            };
            
            SAM2Api.SendFrames(snapshots, values, targetWidth, targetHeight);

            cameraShutter.Play();
            webCamTextureDebugInfo.text = $"Snapshot captured at {Time.time:F2}s";
        }
        else
        {
            webCamTextureDebugInfo.text = "WebCamTexture not ready!";
        }
    }

    private IEnumerator Start()
    {
        
        result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        
        // Wait until passthrough camera is ready
        while (webCamTextureManager.WebCamTexture == null)
        {
            yield return null;
        }

        webCamDeviceDebugInfo.text = "WebCamTexture object currently ready and playing";

        // Show intrinsics info (optional)
        var cameraEye = webCamTextureManager.Eye;
        var cameraDetails = PassthroughCameraUtils.GetCameraIntrinsics(cameraEye);
        webCamDeviceDebugInfo.text = $"PrincipalPoint: {cameraDetails.PrincipalPoint}"
                                     + $"\nFocalLength: {cameraDetails.FocalLength}"
                                     + $"\nResolution: {cameraDetails.Resolution}"
                                     + $"\nSkew: {cameraDetails.Skew}";

        // Prepare snapshot texture
        snapshot1 = new Texture2D(
            webCamTextureManager.WebCamTexture.width,
            webCamTextureManager.WebCamTexture.height,
            TextureFormat.RGB24,
            false
        );
        
        // snapshot2 = new Texture2D(
        //     webCamTextureManager.WebCamTexture.width,
        //     webCamTextureManager.WebCamTexture.height,
        //     TextureFormat.RGB24,
        //     false
        // );
    }
    
    private Texture2D resizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        rt.filterMode = FilterMode.Bilinear;
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}

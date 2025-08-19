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
    [SerializeField] private RawImage inputRawImage;
    [SerializeField] private RawImage outputRawImage;
    
    [SerializeField] private TextMeshProUGUI buttonDebugText;
    [SerializeField] private AudioSource cameraShutter;
    private int pressCount = 0;
    
    [SerializeField] private SAM2Api SAM2Api;
    
    private Texture2D snapshot1;
    private Texture2D snapshot2;
    private List<Texture2D> snapshots = new List<Texture2D>();
    private List<int[]> values = new List<int[]>();
    
    private Texture2D result; // Texture for resized images
    
    private const int targetWidth = 1280; // Target width for resized images
    private const int targetHeight = 960; // Target height for resized images

    private const int aiFrameCap = 2;
    private bool isCapturing = false;
    public Transform p1;
    public Transform p2;
    
    private Vector2 screenTopLeft;
    private Vector2 screenBottomRight;
    private bool rectangleValid;
    
    private WebCamTexture _webcamTexture;
    
    void Update()
    {

        if (OVRInput.GetDown(OVRInput.Button.One) || isCapturing == true)
        {
            
            _webcamTexture = webCamTextureManager.WebCamTexture;
            // --- Project controllers into left-eye camera plane ---
            Vector2 pixel1 = WorldToTextureXY(p1.position);
            Vector2 pixel2 = WorldToTextureXY(p2.position);

            // Compute top-left and bottom-right in pixel coordinates
            Vector2 topLeft = new Vector2(
                Mathf.Min(pixel1.x, pixel2.x),
                Mathf.Min(pixel1.y, pixel2.y)
            );
            Vector2 bottomRight = new Vector2(
                Mathf.Max(pixel1.x, pixel2.x),
                Mathf.Max(pixel1.y, pixel2.y)
            );

            var box = new[] { (int)topLeft.x, (int)topLeft.y, (int)bottomRight.x, (int)bottomRight.y };
            rectangleValid = true;
            pressCount++;
            Debug.Log("A button pressed!");
            buttonDebugText.text = $"BUTTON PRESSED x{pressCount.ToString()}";
            // DoAction();
            if (isCapturing == false)
            {
                isCapturing = true;
            }
            CaptureFrame(box);

            // Optional: visualize on a UI RawImage
        }
    }
    
    private Vector2 WorldToTextureXY(Vector3 worldPoint)
    {
        var cameraPose = PassthroughCameraUtils.GetCameraPoseInWorld(webCamTextureManager.Eye);
        var localPoint = Quaternion.Inverse(cameraPose.rotation) * (worldPoint - cameraPose.position);
        var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye);

        if (localPoint.z <= 0.0001f)
        {
            Debug.LogWarning("Point too close to camera!");
            return Vector2.zero;
        }

        // Project to image plane (note inverted y)
        float uPixel = intrinsics.FocalLength.x * (localPoint.x / localPoint.z) + intrinsics.PrincipalPoint.x;
        float vPixel = intrinsics.FocalLength.y * (-localPoint.y / localPoint.z) + intrinsics.PrincipalPoint.y;

        // Optional: scale if intrinsics.Resolution != WebCamTexture resolution
        if (intrinsics.Resolution.x != _webcamTexture.width || intrinsics.Resolution.y != _webcamTexture.height)
        {
            float scaleX = _webcamTexture.width / (float)intrinsics.Resolution.x;
            float scaleY = _webcamTexture.height / (float)intrinsics.Resolution.y;
            uPixel *= scaleX;
            vPixel *= scaleY;
        }

        // Clamp to valid pixel coordinates
        int x = Mathf.Clamp(Mathf.RoundToInt(uPixel), 0, _webcamTexture.width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(vPixel), 0, _webcamTexture.height - 1);

        return new Vector2(x, y);
    }


    private void CaptureFrame(int[] box)
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
            inputRawImage.texture = snapshot1;
            //inputRawImage.texture = snapshot2;
            
            // Obtain snapshots and values lists

            // snapshots.Add(resizeTexture(snapshot1, targetWidth, targetHeight));
            snapshots.Add(snapshot1);
            values.Add(box);

            if (snapshots.Count == aiFrameCap)
            {
                SAM2Api.SendFrames(snapshots, values, targetWidth, targetHeight);
                isCapturing = false;
                snapshots.Clear();
                values.Clear();
                cameraShutter.Play();
            }
            
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
    
    // private Texture2D resizeTexture(Texture2D source, int targetWidth, int targetHeight)
    // {
    //     var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
    //     rt.filterMode = FilterMode.Bilinear;
    //     var previous = RenderTexture.active;
    //     RenderTexture.active = rt;
    //     Graphics.Blit(source, rt);
    //     result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
    //     result.Apply();
    //     RenderTexture.active = previous;
    //     RenderTexture.ReleaseTemporary(rt);
    //     return result;
    // }
    
    private void OnEnable()
    {
        // Subscribe to the event
        if (SAM2Api != null)
            SAM2Api.OnResponseReceived += HandleResponse;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        if (SAM2Api != null)
            SAM2Api.OnResponseReceived -= HandleResponse;
    }
    
    // This method will be called whenever OnResponseReceived is invoked
    private void HandleResponse(Texture2D tex)
    {
        Debug.Log("Received response texture! Size: " + tex.width + "x" + tex.height);

        // Example: assign to a RawImage in your UI
        // rawImage.texture = tex;
        outputRawImage.texture = tex;
    }
}

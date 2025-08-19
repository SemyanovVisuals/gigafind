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
    // Update is called once per frame
    // void Update()
    // {
    //     // Example: Spacebar for PC, replace with OVRInput for Quest trigger
 
    //     // --- NEW PART: project controllers into left-eye camera plane ---
    //     if (OVRInput.GetDown(OVRInput.Button.One))
    //     {
    //         // use p1 and p2 to get location ON webCamTextureManager.WebCamTexture here
    //         Vector2 pixel1 = WorldToPassthroughPixel(p1);
    //         Vector2 pixel2 = WorldToPassthroughPixel(p2);
    //
    //         Vector2 topLeft = new Vector2(Mathf.Min(pixel1.x, pixel2.x), Mathf.Min(pixel1.y, pixel2.y));
    //         Vector2 bottomRight = new Vector2(Mathf.Max(pixel1.x, pixel2.x), Mathf.Max(pixel1.y, pixel2.y));
    //
    //         Debug.Log($"Projected rectangle: TopLeft={topLeft}, BottomRight={bottomRight}");
    //         
    //
    //     }
    // }
    // private Vector2 WorldToPassthroughPixel(Transform point)
    // {
    //     if (webCamTextureManager == null || webCamTextureManager.WebCamTexture == null)
    //         return Vector2.zero;
    //
    //     // Use the actual left-eye camera transform
    //     Transform leftEyeCamera = webCamTextureManager.transform; // replace with actual LeftEyeAnchor if needed
    //
    //     // Get camera intrinsics
    //     var intrinsics = PassthroughCameraUtils.GetCameraIntrinsics(webCamTextureManager.Eye);
    //
    //     // Convert world position to camera space
    //     Vector3 camSpace = leftEyeCamera.worldToLocalMatrix.MultiplyPoint(point.position);
    //
    //     if (camSpace.z <= 0.01f)
    //     {
    //         Debug.LogWarning("Point is behind or too close to the camera! z=" + camSpace.z);
    //         camSpace.z = 0.01f; // avoid division by zero
    //     }
    //
    //     // If intrinsics are normalized, convert to pixels
    //     float fx = intrinsics.FocalLength.x;
    //     float fy = intrinsics.FocalLength.y;
    //     float cx = intrinsics.PrincipalPoint.x;
    //     float cy = intrinsics.PrincipalPoint.y;
    //
    //     if (cx <= 1f && cy <= 1f) // normalized
    //     {
    //         cx *= intrinsics.Resolution.x;
    //         cy *= intrinsics.Resolution.y;
    //         fx *= intrinsics.Resolution.x;
    //         fy *= intrinsics.Resolution.y;
    //     }
    //
    //     // Project
    //     float u = fx * (camSpace.x / camSpace.z) + cx;
    //     float v = fy * (camSpace.y / camSpace.z) + cy;
    //
    //     // Flip v for top-left origin
    //     v = intrinsics.Resolution.y - v;
    //
    //     // Clamp to texture bounds
    //     u = Mathf.Clamp(u, 0, intrinsics.Resolution.x);
    //     v = Mathf.Clamp(v, 0, intrinsics.Resolution.y);
    //     
    //     Vector3 camSpace = leftEyeCamera.worldToLocalMatrix.MultiplyPoint(point.position);
    //     Debug.Log($"{point.name} camSpace = {camSpace}");
    //
    //     return new Vector2(u, v);
    // }
    
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
            Debug.LogWarning("ColorPicker: Point too close.");
            return Vector2.zero;
        }

        var scaleX = _webcamTexture.width / (float)intrinsics.Resolution.x;
        var scaleY = _webcamTexture.height / (float)intrinsics.Resolution.y;

        var uPixel = intrinsics.FocalLength.x * (localPoint.x / localPoint.z) + intrinsics.PrincipalPoint.x;
        var vPixel = intrinsics.FocalLength.y * (localPoint.y / localPoint.z) + intrinsics.PrincipalPoint.y;

        uPixel *= scaleX;
        vPixel *= scaleY;

        var u = uPixel / _webcamTexture.width;
        var v = vPixel / _webcamTexture.height;
        
        var x = Mathf.Clamp(Mathf.RoundToInt(u * _webcamTexture.width), 0, _webcamTexture.width - 1);
        var y = Mathf.Clamp(Mathf.RoundToInt(v * _webcamTexture.height), 0, _webcamTexture.height - 1);
        
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

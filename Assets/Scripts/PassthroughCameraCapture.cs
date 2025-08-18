using System.Collections;
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
    
    private Texture2D snapshot;

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
        WebCamTexture camTex = webCamTextureManager.WebCamTexture;

        if (camTex != null && camTex.isPlaying)
        {
            // Copy pixels into snapshot
            snapshot.SetPixels(camTex.GetPixels());
            snapshot.Apply();

            // Show the snapshot on the RawImage
            webCamRawImage.texture = snapshot;
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
        snapshot = new Texture2D(
            webCamTextureManager.WebCamTexture.width,
            webCamTextureManager.WebCamTexture.height,
            TextureFormat.RGB24,
            false
        );
    }
}

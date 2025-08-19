using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace DefaultNamespace
{
    public class SAM2Api: MonoBehaviour
    {
        // [SerializeField] private List<Texture2D> textures = new List<Texture2D>();
        // [SerializeField] private List<float[]> values = new List<float[]>(); 
        public event Action<(Texture2D texture, string name)> OnResponseReceived;
        [SerializeField] private Canvas outputCanvas;
        
        private string serverUrl = "http://172.20.10.4:8000/boxes";
        
        public void SendFrames(List<Texture2D> textures, List<int[]> values, int  targetWidth, int targetHeight)
        {
            Debug.Log("SendFrames");
            StartCoroutine(UploadFramesAndBbox(textures, values, targetWidth, targetHeight));
        }

        private IEnumerator UploadFramesAndBbox(List<Texture2D> frames, List<int[]> bboxCoords, int  targetWidth, int targetHeight)
        {
            var outputWidth = bboxCoords[0][2] - bboxCoords[0][0];  // right x - left x
            var outputHeight = bboxCoords[0][3] - bboxCoords[0][1];    // right y - left y
            Debug.Log("Output width: " + outputWidth + ", output height: " + outputHeight);
            RectTransform rt = outputCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(outputWidth, outputHeight);
            
            Debug.Log("UploadFramesAndBbox");
            WWWForm form = new WWWForm();
            string boxBatchStr = string.Join(",", bboxCoords.SelectMany(b => b));
            form.AddField("box_batch_str", boxBatchStr);

            for (int i = 0; i < frames.Count; i++)
            {
                Texture2D tex = frames[i];
                byte[] bytes = tex.EncodeToPNG();
                form.AddBinaryData("frames", bytes, i + ".png", "image/jpeg");
            }
            Debug.Log("UploadFramesAndBbox after encoding");

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                Debug.Log("Send request");
                yield return www.SendWebRequest();
                Debug.Log("Got response");
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Upload failed: " + www.error);
                }
                else
                {
                    Debug.Log("Upload successful! Received " + www.downloadHandler.data.Length + " bytes");

                    // Optional: convert response to Texture2D
                    
                    Texture2D responseTex = new Texture2D(outputWidth, outputHeight);
                    //Texture2D responseTex = new Texture2D(targetWidth, targetHeight);
                    responseTex.LoadImage(www.downloadHandler.data);
                    string llmText = www.GetResponseHeader("x-llm-text");
                    
                    var texWithString = (texture: responseTex, llmText: llmText);

                    // Notify listeners that the response arrived
                    OnResponseReceived?.Invoke(texWithString);
                }
            }
        }
    }
}
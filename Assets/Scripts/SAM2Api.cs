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
        [SerializeField] private List<Texture2D> textures = new List<Texture2D>();
        [SerializeField] private List<float[]> values = new List<float[]>(); 
        public event Action<Texture2D> OnResponseReceived;
        
        private string serverUrl = "http://192.168.137.1:8000/boxes";
        
        public void SendFrames()
        {
            StartCoroutine(UploadFramesAndBbox(textures, values));
        }

        private IEnumerator UploadFramesAndBbox(List<Texture2D> frames, List<float[]> bboxCoords)
        {
            WWWForm form = new WWWForm();
            string boxBatchStr = string.Join(",", bboxCoords.SelectMany(b => b));
            form.AddField("box_batch_str", boxBatchStr);

            for (int i = 0; i < frames.Count; i++)
            {
                Texture2D tex = frames[i];
                byte[] bytes = tex.EncodeToPNG();
                form.AddBinaryData("frames", bytes, i + ".png", "image/jpeg");
            }

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                yield return www.SendWebRequest();
                
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Upload failed: " + www.error);
                }
                else
                {
                    Debug.Log("Upload successful! Received " + www.downloadHandler.data.Length + " bytes");

                    // Optional: convert response to Texture2D
                    Texture2D responseTex = new Texture2D(1280, 960);
                    responseTex.LoadImage(www.downloadHandler.data);

                    // Notify listeners that the response arrived
                    OnResponseReceived?.Invoke(responseTex);
                }
            }
        }
    }
}
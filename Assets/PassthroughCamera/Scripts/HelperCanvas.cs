using Meta.WitAi.TTS.Utilities;
using UnityEngine;

public class HelperCanvas : MonoBehaviour
{
    [SerializeField] private TTSSpeaker speaker;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        speaker.Speak("THIS IS A TEST IGNORE IT OR NOT UP TO YOU");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

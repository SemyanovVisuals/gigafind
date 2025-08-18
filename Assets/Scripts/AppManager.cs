using TMPro;
using UnityEngine;

public class AppManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonDebugText;
    private int pressCount = 0;

    // Update is called once per frame
    void Update()
    {
        // Detect A button press on Oculus Touch controller
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            pressCount++;
            Debug.Log("A button pressed!");
            buttonDebugText.text = $"BUTTON PRESSED x{pressCount.ToString()}";
            // DoAction();
        }
    }
}

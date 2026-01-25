# Gigafind 📓

This project uses XR interaction and modern segmentation models to generate real-time “stickers” and object descriptions from controller-defined regions on the Meta Quest 3.

### Overview

Gigafind enables users to select an object in the real world by marking two corner points with VR controllers. The system captures several passthrough camera frames, performs segmentation on a remote server, and returns both a refined 2D mask and a concise textual description of the selected object. The project explores how XR interfaces can enhance computer vision workflows for technical environments where quick identification of components is valuable.

### Hardware / Software Setup
- Headset: Meta Quest 3
- Client (Unity/MR): Lenovo Yoga Pro 9i (RTX 4060)
- Inference Server (SAM2): MacBook Pro M1 Max
- Frameworks: Unity, FastAPI, PyTorch, Meta XR SDK

### Data Pipeline
- The headset collects the last N passthrough frames (N=5 in our implementation) and computes a 2D projected bounding box from two controller positions.
- Frames and bounding boxes are sent to a FastAPI server hosting a SAM2 segmentation model.
- The model produces three candidate masks; the highest-confidence mask is selected and cropped.
- The cropped mask is sent to the Groq LLM API to produce a short textual description.
- The final 2D mask and description are returned to the device and displayed on a UI canvas.

### Challenges
- Passthrough access on Quest: Standalone APKs limited direct camera access; data transfer required using UnityWebRequest over a hotspot connection. Windows hotspot routing caused latency; iPhone hotspot performed reliably.
- 3D–to–2D projection issues: Controller Y-coordinates repeatedly appeared 35–45 px below the target, indicating a misalignment in projection or reference calibration.
- Segmentation noise: SAM2 often segmented fine textures (e.g., wood grain, ceiling patterns). Pre-processing with blur was required to reduce false positives.

### Accomplishments
- Established a real-time inference pipeline between the headset and a remote server with enough performance headroom to extend to more complex detection tasks.
- Achieved reliable mapping of 3D controller positions into 2D camera pixel coordinates.
- Demonstrated a practical XR-based workflow for interactive segmentation, a capability not commonly explored in real-time XR applications.

### Future Work
Gigafind could evolve into a tool for professionals working with complex machinery, electronics, or training systems, where rapid component identification and labeling can improve accuracy and efficiency. The system may also support future multimodal CV tasks requiring high-quality labeled data.

### Tech Stack
Unity, C#, Meta XR SDK, Mixed Reality, Passthrough Camera API (PCA)
Python, FastAPI, PyTorch
SAM2, Hugging Face, Groq LLM
OpenCV (cv2), Matplotlib
MaskGen, UnityWebRequest

### Trailer
[![Demo Video](https://img.youtube.com/vi/VKBsvkuEDrY/0.jpg)](https://youtube.com/shorts/VKBsvkuEDrY)


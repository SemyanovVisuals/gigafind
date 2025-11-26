import random

import matplotlib
import numpy
import torch

device = "mps"

from PIL import Image
import numpy as np

import matplotlib.pyplot as plt

matplotlib.use("Agg")  # Use the non-interactive Agg backend

from sam2_image_predictor import SAM2ImagePredictor

image_path = "seq/0.jpg"
image = Image.open(image_path).convert("RGB")  # ensure 3 channels
image = np.array(image)  # shape (H, W, 3), dtype=uint8

predictor = SAM2ImagePredictor.from_pretrained("facebook/sam2.1-hiera-small", device=device)
predictor.set_image(image)

with torch.inference_mode(), torch.autocast("mps", dtype=torch.bfloat16):
    masks, scores, logits = predictor.predict(box=np.array([286, 149, 380, 260]))

plt.figure(figsize=(8, 8))
plt.imshow(image)
print("IMAGE SHAPE", image.shape)
print(len(masks))
for i, mask in enumerate(masks):
    print(scores[i], logits[i])
    print(mask.shape)
    # mask = np.transpose(mask, (1, 2, 0))  # H, W, C
    plt.imshow(mask, alpha=0.4)  # overlay masks
    break
plt.axis("off")
plt.savefig("plot.png")

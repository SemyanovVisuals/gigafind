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

BATCH_SIZE = 10
images = list()
for i in range(BATCH_SIZE):
    path = f"seq/{i}.jpg"
    img = Image.open(path).convert("RGB")  # ensure 3 channels
    img = np.array(img)  # shape (H, W, 3), dtype=uint8
    images.append(img)


predictor = SAM2ImagePredictor.from_pretrained("facebook/sam2.1-hiera-tiny", device=device)

predictor.set_image_batch(images)

with torch.inference_mode(), torch.autocast("mps", dtype=torch.float16):
    box = [np.array([286, 149, 380, 260]) for _ in range(BATCH_SIZE)]
    masks_list, scores_list, logits_list = predictor.predict_batch(box_batch=box)


# Process each frame
for j in range(BATCH_SIZE):
    masks = masks_list[j]
    scores = scores_list[j]
    logits = logits_list[j]
    image = images[j]

    print("IMAGE SHAPE", image.shape)

    plt.figure(figsize=(6, 6))
    plt.imshow(image)

    print(len(masks))

    best_idx = np.argmax(scores)
    mask = masks[best_idx]

    #for i, mask in enumerate(masks):
    #print(scores[i], logits[i])

    print(mask.shape)
    # mask = np.transpose(mask, (1, 2, 0))  # H, W, C
    if mask.ndim == 3 and mask.shape[0] == 3:  # (C, H, W)
        mask = np.transpose(mask, (1, 2, 0))  # now shape is (290, 640, 3)

    plt.imshow(mask, alpha=0.4)  # overlay masks

    plt.axis("off")
    plt.savefig(f"det/{j}.png")

    # RETURN MASK
    threshold = 0.5
    filtered_pixels = np.zeros_like(image)
    filtered_pixels[mask > threshold] = image[mask > threshold]
    plt.figure(figsize=(5, 5))
    plt.imshow(filtered_pixels)
    plt.axis("off")
    plt.savefig(f"det/{j}_mask.png")

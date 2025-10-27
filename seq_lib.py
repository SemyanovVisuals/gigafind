import io
import uuid

import cv2
import matplotlib
import torch
from matplotlib.patches import Rectangle
from skimage import measure

device = "mps"

from PIL import Image, ImageFile
import numpy as np

import matplotlib.pyplot as plt

matplotlib.use("Agg")  # Use the non-interactive Agg backend

from sam2_image_predictor import SAM2ImagePredictor

predictor = SAM2ImagePredictor.from_pretrained("facebook/sam2.1-hiera-base-plus", device=device)


def inference(frames: list[Image], device_boxes: list[tuple[int, int, int, int]]) -> bytes:
    """
    Inference the frames and point

    :param device_boxes: list of boxes associated with the frame
    :param frames: list of frames
    :return:
    """

    batch_size = len(frames)

    images = list()

    for image in frames:
        img = image.convert("RGB")  # ensure 3 channels

        img = np.array(img)  # shape (H, W, 3), dtype=uint8
        image_blur = cv2.GaussianBlur(img, (5, 5), 0)
        images.append(image_blur)

    predictor.set_image_batch(images)

    with torch.inference_mode(), torch.autocast("mps", dtype=torch.float16):
        box_batch = [np.array(box) for box in device_boxes]
        masks_list, scores_list, logits_list = predictor.predict_batch(box_batch=box_batch)

    # Process each frame
    for j in range(batch_size):
        unique_id = str(uuid.uuid4())[:15]
        print(unique_id)
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

        # for i, mask in enumerate(masks):
        # print(scores[i], logits[i])

        print(mask.shape)
        # mask = np.transpose(mask, (1, 2, 0))  # H, W, C
        if mask.ndim == 3 and mask.shape[0] == 3:  # (C, H, W)
            mask = np.transpose(mask, (1, 2, 0))  # now shape is (290, 640, 3)

        plt.imshow(mask, alpha=0.4)  # overlay masks

        x1, y1, x2, y2 = device_boxes[j]
        x = min(x1, x2)
        y = min(y1, y2)
        w = abs(x2 - x1)
        h = abs(y2 - y1)
        rect = Rectangle((x, y), w, h, fill=False, edgecolor="red", linewidth=2)

        plt.gca().add_patch(rect)
        plt.axis("off")
        plt.savefig(f"det/{unique_id}{j}.png")

        # RETURN MASK
        threshold = 0.5

        # suppose image is RGB (H, W, 3)
        h, w, c = image.shape

        # add alpha channel
        filtered_pixels = np.zeros((h, w, 4), dtype=np.uint8)

        # keep RGB where mask > threshold
        filtered_pixels[mask > threshold, :3] = image[mask > threshold]

        # set alpha=255 where mask > threshold (visible), 0 otherwise
        filtered_pixels[mask > threshold, 3] = 255

        binary_mask = (mask > threshold).astype(np.uint8)

        contours, _ = cv2.findContours(binary_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        filtered_pixels = cv2.drawContours(filtered_pixels, contours, -1, (0, 0, 0, 255), thickness=2)
        filtered_pixels = cv2.drawContours(filtered_pixels, contours, -1, (255, 255, 255, 255), thickness=1)

        # save as PNG with transparency
        out_img = Image.fromarray(filtered_pixels, mode="RGBA").crop((x1, y1, x2, y2))
        out_img.save(f"det/{unique_id}{j}_mask.png")
        #plt.savefig(f"det/{j}_mask.png")
        buffer = io.BytesIO()
        out_img.save(buffer, format="PNG")
        return buffer.getvalue()
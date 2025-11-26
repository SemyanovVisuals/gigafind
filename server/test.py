from PIL import Image

from seq_lib import inference

if __name__ == '__main__':
    BATCH_SIZE = 10
    images = list()
    for i in range(BATCH_SIZE):
        path = f"seq/{i}.jpg"
        img = Image.open(path)
        images.append(img)

    boxes_list = [(286, 149, 380, 260) for _ in range(BATCH_SIZE)]

    inference(images, boxes_list)

import cv2

frameNr = 0


capture = cv2.VideoCapture('budapest_short.mp4')

while (True):
    success, frame = capture.read()
    if not success:
        print("Finished reading video")
        break

    cv2.imwrite(f"seq/{frameNr}.jpg", frame)

    frameNr += 1

capture.release()

import io

import requests
from PIL import Image

url = "http://localhost:8000/boxes"
payload = {'box_batch_str': '286,149,380,260,286,149,380,260'}
files=[
  ('frames',('0.jpg',open('/Users/misha/Documents/XRAI_SAM/scripts/seq/0.jpg','rb'),'image/jpeg')),
  ('frames',('1.jpg',open('/Users/misha/Documents/XRAI_SAM/scripts/seq/1.jpg','rb'),'image/jpeg'))
]
headers = {}

response = requests.request("POST", url, headers=headers, data=payload, files=files)

print(response.content)
image = Image.open(fp=io.BytesIO(response.content), formats=["png"])
image.save("res.png")
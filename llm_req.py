import base64

from groq import Groq

client = Groq(api_key="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")



def get_llm_text(image_bytes: bytes):
    try:
        image_b64 = base64.b64encode(image_bytes).decode("utf-8")

        completion = client.chat.completions.create(
            model="meta-llama/llama-4-maverick-17b-128e-instruct",
            messages=[
                {
                    "role": "user",
                    "content": [
                        {"type": "text", "text": "Detect the object in the picture. Give a very brief explaination of what this object is. Make it max 40 characters. "},
                        {
                            "type": "image_url",
                            "image_url": {"url": f"data:image/jpeg;base64,{image_b64}"}
                        }
                    ]
                }
            ],
            temperature=1,
            max_completion_tokens=40,
            top_p=1,
            stream=True,
            stop=None
        )
        return_str = ""
        for chunk in completion:
            return_str += chunk.choices[0].delta.content or ""
            #print(chunk.choices[0].delta.content or "", end="")
        print(return_str)
        if len(return_str) == 0:
            return_str = "Nothing got detected :C"
        return return_str
    except Exception as e:
        print("EXCEPTION", e)
        return "Out of request quota"


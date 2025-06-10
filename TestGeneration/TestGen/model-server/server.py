#!/usr/bin/env python
# coding: utf-8

# In[1]:

import os
import subprocess
import sys

def install_packages():
    try:
        import fastapi
        import uvicorn
        import transformers
        import torch
    except ImportError:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        requirements_path = os.path.join(script_dir, "requirements.txt")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "-r", requirements_path])

install_packages()


from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
import logging

app = FastAPI()

class GenerationRequest(BaseModel):
    inputs: list[str]

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
best_model_dir = os.path.join(BASE_DIR, "best_model")
tokenizer = AutoTokenizer.from_pretrained(best_model_dir)
model = AutoModelForSeq2SeqLM.from_pretrained(best_model_dir)

@app.post("/generate")
def generate_code(req: GenerationRequest):
    try:
        inputs = tokenizer(req.inputs, max_length=128, truncation=True, padding="max_length", return_tensors="pt")
        outputs = model.generate(
            input_ids=inputs["input_ids"],
            attention_mask=inputs["attention_mask"],
            max_length=256,
            num_beams=5,
            early_stopping=True
        )
        predicted_texts = tokenizer.batch_decode(outputs, skip_special_tokens=True)
        return {"outputs": predicted_texts}
    except Exception as e:
        logging.error(f"Generation error: {e}")
        raise HTTPException(status_code=500, detail="Internal server error")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=8000)


# In[ ]:





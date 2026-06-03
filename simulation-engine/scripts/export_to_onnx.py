#!/usr/bin/env python3
"""
Utility script to export the fine-tuned PyTorch GPT-2 model to ONNX format.
"""

import os
import sys
from optimum.onnxruntime import ORTModelForCausalLM
from transformers import AutoTokenizer

def main():
    # Set paths
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    model_path = os.path.join(base_dir, "gpt2-football-finetuned")
    onnx_path = os.path.join(base_dir, "gpt2-football-finetuned-onnx")
    
    print(f"Loading and exporting PyTorch model from: {model_path}")
    print(f"Exporting to ONNX path: {onnx_path}")
    
    try:
        # Load tokenizer
        tokenizer = AutoTokenizer.from_pretrained(model_path)
        
        # Load and export the causal LM to ONNX
        model = ORTModelForCausalLM.from_pretrained(model_path, export=True)
        
        # Save exported ONNX model and tokenizer files
        model.save_pretrained(onnx_path)
        tokenizer.save_pretrained(onnx_path)
        
        print("ONNX model successfully exported and saved!")
        
    except Exception as e:
        print(f"Failed to export model to ONNX: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()

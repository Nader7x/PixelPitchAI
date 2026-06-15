#!/usr/bin/env python3
"""
Utility script to quantize the exported ONNX model to INT8 format.
"""

import os
import sys
from optimum.onnxruntime import ORTQuantizer
from optimum.onnxruntime.configuration import AutoQuantizationConfig

def main():
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    onnx_path = os.path.join(base_dir, "gpt2-football-finetuned-onnx")
    quantized_path = os.path.join(base_dir, "gpt2-football-finetuned-onnx-int8")
    
    print(f"Loading ONNX model from: {onnx_path}")
    print(f"Quantizing to path: {quantized_path}")
    
    if not os.path.exists(onnx_path):
        print(f"Error: ONNX model path {onnx_path} does not exist. Run export_to_onnx.py first.")
        sys.exit(1)
        
    try:
        os.makedirs(quantized_path, exist_ok=True)
        
        # Initialize the quantizer from the exported ONNX model
        quantizer = ORTQuantizer.from_pretrained(onnx_path)
        
        # Configure dynamic quantization (best for CPU/ARM/x86 performance of decoders)
        # We use avx2 for general CPU compatibility
        qconfig = AutoQuantizationConfig.avx2(is_static=False, per_channel=True)
        
        # Quantize the model
        quantizer.export(
            onnx_model_path=os.path.join(onnx_path, "model.onnx"),
            onnx_quantized_model_output_path=os.path.join(quantized_path, "model_quantized.onnx"),
            quantization_config=qconfig,
        )
        
        # Copy configuration, tokenizer files and merges to the quantized directory
        # so that it is a fully self-contained HF model folder
        import shutil
        for filename in os.listdir(onnx_path):
            if filename != "model.onnx":
                src = os.path.join(onnx_path, filename)
                dst = os.path.join(quantized_path, filename)
                if os.path.isdir(src):
                    shutil.copytree(src, dst, dirs_exist_ok=True)
                else:
                    shutil.copy2(src, dst)
                    
        print("ONNX model successfully quantized and saved to INT8 folder!")
        
    except Exception as e:
        import traceback
        traceback.print_exc()
        print(f"Failed to quantize model: {str(e)}")
        sys.exit(1)

if __name__ == "__main__":
    main()

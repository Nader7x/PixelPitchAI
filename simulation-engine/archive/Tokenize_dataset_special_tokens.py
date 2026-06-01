import os
import torch
from transformers import GPT2Tokenizer

# ✅ File path to your input text
file_path = "../Matches/match_266653.txt"

# ✅ Define and add special tokens
special_tokens = {
    "additional_special_tokens": ["[MATCH START]", "[EVENTS START]", "[SECOND HALF]", "[MATCH END]"]
}

# 🔁 Load tokenizer and add special tokens
tokenizer = GPT2Tokenizer.from_pretrained("gpt2")
tokenizer.add_special_tokens(special_tokens)
tokenizer.pad_token = tokenizer.eos_token  # Required for GPT2

# 🧾 Load match text and extract initial part (up to [EVENTS START])
with open(file_path, "r", encoding="utf-8") as f:
    full_text = f.read()

# Find the position of [EVENTS START] tag
events_start_pos = full_text.find("[EVENTS START]")
if events_start_pos == -1:
    print("⚠️ Warning: [EVENTS START] tag not found in the file. Using the entire file.")
    initial_text = full_text
else:
    # Extract text from beginning up to and including [EVENTS START]
    initial_text = full_text[:events_start_pos + len("[EVENTS START]")]
    print(f"✅ Found [EVENTS START] tag at position {events_start_pos}")
    print(f"✅ Extracted initial text ({len(initial_text)} characters)")

print("🔍 Initial text preview:")
print(initial_text[:200] + "..." if len(initial_text) > 200 else initial_text)

print("\n🔁 Tokenizing the initial match text...")

# 🔐 Tokenize initial text as a single sequence
input_ids = tokenizer.encode(initial_text, return_tensors="pt")  # shape: (1, sequence_length)

# (Optional) Confirm special tokens were added and used correctly
print(f"🧩 Example encoding for '[MATCH START]': {tokenizer.encode('[MATCH START]')}")
print(f"🧩 Example encoding for '[EVENTS START]': {tokenizer.encode('[EVENTS START]')}")

# Extract match teams from the file name or content for a more descriptive output file name
match_name = os.path.basename(file_path).replace(".txt", "")

# Save to disk (keep on CPU)
save_path = "./input_tokens.pt"  # Save as input_tokens.pt for direct use with the model
torch.save(input_ids, save_path)

print(f"✅ Tokenization complete. Shape: {input_ids.shape}")
print(f"💾 Saved tokenized tensor to: {save_path}")
print(f"🏆 This input_tokens.pt file can now be used to simulate a match based on {match_name}")

import torch
from sys import prefix
from transformers import GPT2LMHeadModel, GPT2Tokenizer

# Load fine-tuned model
model_path = '../gpt2-football-finetuned'
special_tokens = {
    "additional_special_tokens": ["[MATCH START]", "[EVENTS START]", "[SECOND HALF]", "[MATCH END]"]
}

tokenizer = GPT2Tokenizer.from_pretrained(model_path)
tokenizer.add_special_tokens(special_tokens)
tokenizer.pad_token = tokenizer.eos_token
model = GPT2LMHeadModel.from_pretrained(model_path)

model.eval()

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model.to(device)

print(device)

# Load your input tokens file
input_tokens = torch.load("input_tokens.pt").to(device)

# Decode and print the first input tokens
initial_text = tokenizer.decode(input_tokens[0])
print(f"Initial input tokens: {initial_text}")

# Initialize variables
generated_tokens = input_tokens.clone()

max_length = 1024  # Maximum number of tokens the model can process at once
num_tokens_to_generate = 10000  # Number of tokens to generate after the input tokens

# Create attention mask where 1 is for valid tokens, 0 is for padding
attention_mask = (input_tokens != tokenizer.pad_token_id).long()
# Save match stats as frozen prefix
frozen_prefix = input_tokens.clone()

num = 0
for _ in range(num_tokens_to_generate):
    if generated_tokens.shape[1] + 100 >= 1024:
        keep_last_n = 400
        context_tail = generated_tokens[:, -keep_last_n:]

        # Reset generated_tokens
        generated_tokens = torch.cat((frozen_prefix, context_tail), dim=1)

        # Recompute attention mask using pad_token_id
        attention_mask = (generated_tokens != tokenizer.pad_token_id).long()

    # print(len(generated_tokens[0]))
    # Current input to model
    current_input = generated_tokens[:, -max_length:]
    current_attention_mask = attention_mask[:, -max_length:]

    # Generate
    output = model.generate(
        input_ids=current_input,
        attention_mask=current_attention_mask,
        max_new_tokens=100,
        temperature=0.9,
        top_p=0.95,
        top_k=50,
        do_sample=True,
        pad_token_id=tokenizer.eos_token_id,
    )
    num += 100

    # Append new tokens
    new_token = output[:, -100:]
    generated_tokens = torch.cat((generated_tokens, new_token), dim=1)
    new_attention_mask = torch.ones_like(new_token).to(device)
    attention_mask = torch.cat((attention_mask, new_attention_mask), dim=1)

    # Decode and display
    new_text = tokenizer.decode(new_token[0])
    initial_text += new_text

    print(new_text)

    if num >= num_tokens_to_generate:
        break
    # print(new_text)

    # # Optionally, print intermediate output
    # if len(generated_tokens[0]) % 250 == 0:  # Print after every 100 tokens
    #     print(f"Generated tokens so far: {len(generated_tokens[0])}")
    #     print(f"Partial output: {initial_text[-300:]}")

# Save generated events to file
with open("SimulatedMatch.txt", "w", encoding="utf-8") as f:
    f.write(initial_text)

# Final output after generationA
print(f"Final Generated Text: {initial_text}")

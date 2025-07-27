import torch
from sys import prefix
from transformers import GPT2LMHeadModel, GPT2Tokenizer

special_list = ["Deportivo_Alavés_2016", "Deportivo_Alavés_2017", "Deportivo_Alavés_2018", "Deportivo_Alavés_2019",
                "Deportivo_Alavés_2020", "Deportivo_Alavés_2021",
                "Barcelona_2016", "Barcelona_2017", "Barcelona_2018", "Barcelona_2019", "Barcelona_2020",
                "Barcelona_2021",
                "Granada_2016", "Granada_2017", "Granada_2018", "Granada_2019", "Granada_2020", "Granada_2021",
                "Celta_Vigo_2016", "Celta_Vigo_2017", "Celta_Vigo_2018", "Celta_Vigo_2019", "Celta_Vigo_2020",
                "Celta_Vigo_2021",
                "Real_Betis_2016", "Real_Betis_2017", "Real_Betis_2018", "Real_Betis_2019", "Real_Betis_2020",
                "Real_Betis_2021",
                "Osasuna_2016", "Osasuna_2017", "Osasuna_2018", "Osasuna_2019", "Osasuna_2020", "Osasuna_2021",
                "Real_Madrid_2016", "Real_Madrid_2017", "Real_Madrid_2018", "Real_Madrid_2019", "Real_Madrid_2020",
                "Real_Madrid_2021",
                "Levante_UD_2016", "Levante_UD_2017", "Levante_UD_2018", "Levante_UD_2019", "Levante_UD_2020",
                "Levante_UD_2021",
                "Villarreal_2016", "Villarreal_2017", "Villarreal_2018", "Villarreal_2019", "Villarreal_2020",
                "Villarreal_2021",
                "Huesca_2016", "Huesca_2017", "Huesca_2018", "Huesca_2019", "Huesca_2020", "Huesca_2021",
                "Sevilla_2016", "Sevilla_2017", "Sevilla_2018", "Sevilla_2019", "Sevilla_2020", "Sevilla_2021",
                "Getafe_2016", "Getafe_2017", "Getafe_2018", "Getafe_2019", "Getafe_2020", "Getafe_2021",
                "Atlético_Madrid_2016", "Atlético_Madrid_2017", "Atlético_Madrid_2018", "Atlético_Madrid_2019",
                "Atlético_Madrid_2020", "Atlético_Madrid_2021",
                "Valencia_2016", "Valencia_2017", "Valencia_2018", "Valencia_2019", "Valencia_2020", "Valencia_2021",
                "Real_Sociedad_2016", "Real_Sociedad_2017", "Real_Sociedad_2018", "Real_Sociedad_2019",
                "Real_Sociedad_2020", "Real_Sociedad_2021",
                "Real_Valladolid_2016", "Real_Valladolid_2017", "Real_Valladolid_2018", "Real_Valladolid_2019",
                "Real_Valladolid_2020", "Real_Valladolid_2021",
                "Cádiz_2016", "Cádiz_2017", "Cádiz_2018", "Cádiz_2019", "Cádiz_2020", "Cádiz_2021",
                "Athletic_Club_2016", "Athletic_Club_2017", "Athletic_Club_2018", "Athletic_Club_2019",
                "Athletic_Club_2020", "Athletic_Club_2021",
                "Elche_2016", "Elche_2017", "Elche_2018", "Elche_2019", "Elche_2020", "Elche_2021",
                "Eibar_2016", "Eibar_2017", "Eibar_2018", "Eibar_2019", "Eibar_2020", "Eibar_2021",
                "Leganés_2016", "Leganés_2017", "Leganés_2018", "Leganés_2019", "Leganés_2020", "Leganés_2021",
                "Mallorca_2016", "Mallorca_2017", "Mallorca_2018", "Mallorca_2019", "Mallorca_2020", "Mallorca_2021",
                "Espanyol_2016", "Espanyol_2017", "Espanyol_2018", "Espanyol_2019", "Espanyol_2020", "Espanyol_2021",
                "Girona_2016", "Girona_2017", "Girona_2018", "Girona_2019", "Girona_2020", "Girona_2021",
                "Rayo_Vallecano_2016", "Rayo_Vallecano_2017", "Rayo_Vallecano_2018", "Rayo_Vallecano_2019",
                "Rayo_Vallecano_2020", "Rayo_Vallecano_2021",
                "RC_Deportivo_La_Coruña_2016", "RC_Deportivo_La_Coruña_2017", "RC_Deportivo_La_Coruña_2018",
                "RC_Deportivo_La_Coruña_2019", "RC_Deportivo_La_Coruña_2020", "RC_Deportivo_La_Coruña_2021",
                "Las_Palmas_2016", "Las_Palmas_2017", "Las_Palmas_2018", "Las_Palmas_2019", "Las_Palmas_2020",
                "Las_Palmas_2021",
                "Málaga_2016", "Málaga_2017", "Málaga_2018", "Málaga_2019", "Málaga_2020", "Málaga_2021",
                "Sporting_Gijón_2016", "Sporting_Gijón_2017", "Sporting_Gijón_2018", "Sporting_Gijón_2019",
                "Sporting_Gijón_2020", "Sporting_Gijón_2021",
                "[MATCH START]", "[EVENTS START]", "[STOPPAGE TIME - FIRST HALF]", "[END OF FIRST HALF]",
                "[SECOND HALF START]", "[STOPPAGE TIME - SECOND HALF]",
                "[MATCH END]"
                ]

# Load fine-tuned model
model_path = "../gpt2-football-finetuned"
special_tokens = {
    "additional_special_tokens": special_list
}

tokenizer = GPT2Tokenizer.from_pretrained(model_path)
tokenizer.add_special_tokens(special_tokens)
tokenizer.pad_token = tokenizer.eos_token
model = GPT2LMHeadModel.from_pretrained(model_path)

bad_words = ["Real_Madrid_2021", "Barcelona_2018"]
bad_words_ids = tokenizer(bad_words, add_special_tokens=False).input_ids

model.eval()

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model.to(device)

print(device)

# Load your input tokens file
input_tokens = torch.load("prompt2.pt").to(device)

# Decode and print the first input tokens
initial_text = tokenizer.decode(input_tokens[0])
print(f"Initial input tokens: {initial_text}")

# Initialize variables
generated_tokens = input_tokens.clone()

max_length = 1024  # Maximum number of tokens the model can process at once
num_tokens_to_generate = 400  # Number of tokens to generate after the input tokens

# Create attention mask where 1 is for valid tokens, 0 is for padding
attention_mask = (input_tokens != tokenizer.pad_token_id).long()
# Save match stats as frozen prefix
frozen_prefix = input_tokens.clone()

num = 0
first_half_kickoff_detected = False
second_half_kickoff_inserted = False
first_half_kickoff_team = None
teamA_name = "Barcelona_2021"
teamB_name = "Real_Madrid_2018"

for _ in range(num_tokens_to_generate):
    if generated_tokens.shape[1] + 100 >= 1024:
        keep_last_n = 400
        context_tail = generated_tokens[:, -keep_last_n:]
        generated_tokens = torch.cat((frozen_prefix, context_tail), dim=1)
        attention_mask = (generated_tokens != tokenizer.pad_token_id).long()

    current_input = generated_tokens[:, -max_length:]
    current_attention_mask = attention_mask[:, -max_length:]

    output = model.generate(
        input_ids=current_input,
        attention_mask=current_attention_mask,
        max_new_tokens=100,
        temperature=0.9,
        top_p=0.95,
        top_k=50,
        do_sample=True,
        pad_token_id=tokenizer.eos_token_id,
        bad_words_ids=bad_words_ids,  # <-- Prevents forbidden team names

    )
    num += 100

    new_token = output[:, -100:]
    generated_tokens = torch.cat((generated_tokens, new_token), dim=1)
    new_attention_mask = torch.ones_like(new_token).to(device)
    attention_mask = torch.cat((attention_mask, new_attention_mask), dim=1)

    new_text = tokenizer.decode(new_token[0])

    ### ---- Detect kickoff team ---- ###
    if not first_half_kickoff_detected:
        import re

        kickoff_match = re.search(r"\d{2}:\d{2} - ([^ ]+_\d{4}) - pass by .*Kick Off", new_text)
        if kickoff_match:
            first_half_kickoff_team = kickoff_match.group(1)
            print(f"Detected first half kickoff team: {first_half_kickoff_team}")
            first_half_kickoff_detected = True

    ### ---- Inject second half kickoff event ---- ###
    if "[SECOND HALF START]" in new_text and not second_half_kickoff_inserted and first_half_kickoff_team:
        # Cut off the text at [SECOND HALF] so we don't keep unwanted auto-generation
        split_point = new_text.index("[SECOND HALF START]") + len("[SECOND HALF START]")
        before_second_half = new_text[:split_point]

        # Append only the part before/including [SECOND HALF]
        initial_text += before_second_half + "\n"

        # Manually inject second half kickoff event
        second_half_kickoff_team = teamB_name if first_half_kickoff_team in teamA_name else teamA_name
        kickoff_event = f"45:00 - {second_half_kickoff_team}"
        print(f"\n--- Injecting Second Half Kickoff: {kickoff_event.strip()} ---\n")

        initial_text += kickoff_event

        # Add [SECOND HALF] + kickoff event tokens
        injected_text = "[SECOND HALF START]\n" + kickoff_event
        injected_tokens = tokenizer.encode(injected_text, return_tensors="pt").to(device)

        # Update generation state
        generated_tokens = torch.cat((frozen_prefix, injected_tokens), dim=1)
        attention_mask = torch.ones_like(generated_tokens).to(device)

        second_half_kickoff_inserted = True

        # Skip printing the rest of auto-generated content in this step
        continue

    initial_text += new_text

    print(new_text)

    if num >= num_tokens_to_generate:
        break

    # Stop if [MATCH END] is found
    if "[MATCH END]" in new_text:
        print("Special token '[MATCH END]' found. Stopping generation.")
        break

    if num >= num_tokens_to_generate:
        break

# Final output after generationA
print(f"Final Generated Text: {initial_text}")

# Save the generated text to a file
with open("generated_match_output.txt", "w", encoding="utf-8") as f:
    f.write(initial_text)

print("Generated text saved to 'generated_match_output1.txt'")

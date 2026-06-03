import encodings
import os
import torch
from transformers import GPT2Tokenizer


class MatchStatProcessor:
    def __init__(self, model, tokenizer_path="gpt2", special_tokens=None):
        self.model = model  # Your trained model
        self.tokenizer = GPT2Tokenizer.from_pretrained(tokenizer_path)
        if special_tokens:
            self.tokenizer.add_special_tokens({"additional_special_tokens": special_tokens})
            print("special tokens added")
        self.tokenizer.pad_token = self.tokenizer.eos_token

    def generate_features(self, teamA_id: int, teamB_id: int, team_A_season: int, team_B_season: int):
        # Generates Features Dictionary From the model
        input_data = [[teamA_id, teamB_id, team_A_season, team_B_season]]
        prediction = self.model.predict(input_data)[0]
        feature_names = [
            "A_possession_percentage", "B_possession_percentage",
            "A_pass_attempts", "B_pass_attempts",
            "A_pass_completion_rate", "B_pass_completion_rate",
            "A_ground_pass_perc", "B_ground_pass_perc",
            "A_low_pass_perc", "B_low_pass_perc",
            "A_high_pass_perc", "B_high_pass_perc",
            "A_total_shots", "B_total_shots",
            "A_avg_xg_per_shot", "B_avg_xg_per_shot",
            "A_pressures_applied", "B_pressures_applied",
            "A_total_tackles", "B_total_tackles",
            "A_tackle_success_rate", "B_tackle_success_rate",
        ]
        # Convert prediction values conditionally
        return {
            name: int(value) if float(value) >= 1 else round(float(value), 4)
            for name, value in zip(feature_names, prediction)
        }

    def convert_to_text(self, team_A, team_B, features):
        # Returns the header lines
        def categorize_possession(percentage):
            if percentage > 0.6:
                return "dominant possession"
            elif 0.5 <= percentage <= 0.6:
                return "high possession"
            elif 0.4 <= percentage < 0.5:
                return "medium possession"
            else:
                return "low possession"

        def categorize_pass_accuracy(rate):
            if rate > 0.9:
                return "elite passing accuracy"
            elif 0.85 <= rate <= 0.9:
                return "excellent passing accuracy"
            elif 0.75 <= rate < 0.85:
                return "good passing accuracy"
            else:
                return "poor passing accuracy"

        def categorize_pass_type(percentage):
            if percentage > 0.5:
                return "mostly"
            elif 0.2 <= percentage <= 0.5:
                return "some"
            else:
                return "few"

        def categorize_shots(shots):
            if shots > 15:
                return "many shots"
            elif 8 <= shots <= 15:
                return "moderate shots"
            else:
                return "few shots"

        def categorize_xg_per_shot(xg):
            if xg > 0.12:
                return "very dangerous chances"
            elif 0.08 <= xg <= 0.12:
                return "good chances"
            elif 0.05 <= xg < 0.08:
                return "low-quality chances"
            else:
                return "poor chances"

        def categorize_pressures(pressures):
            if pressures > 250:
                return "very high pressing"
            elif 150 <= pressures <= 250:
                return "moderate pressing"
            else:
                return "low pressing"

        def categorize_tackles(tackles):
            if tackles > 15:
                return "many tackles"
            elif 8 <= tackles <= 15:
                return "moderate tackles"
            else:
                return "few tackles"

        def categorize_tackle_success(rate):
            if rate > 0.8:
                return "very effective in tackles"
            elif 0.6 <= rate <= 0.8:
                return "effective in tackles"
            else:
                return "less effective in tackles"

        return [
            "[MATCH START]",
            f"{team_A} faced {team_B} .",
            f"{team_A} had {categorize_possession(features['A_possession_percentage'])}; {team_B} had {categorize_possession(features['B_possession_percentage'])}.",
            f"{team_A} had {categorize_pass_accuracy(features['A_pass_completion_rate'])}; {team_B} had {categorize_pass_accuracy(features['B_pass_completion_rate'])}.",
            f"{team_A} used {categorize_pass_type(features['A_ground_pass_perc'])} ground passes; {team_B} used {categorize_pass_type(features['B_ground_pass_perc'])} ground passes.",
            f"{team_A} used {categorize_pass_type(features['A_low_pass_perc'])} low passes; {team_B} used {categorize_pass_type(features['B_low_pass_perc'])} low passes.",
            f"{team_A} used {categorize_pass_type(features['A_high_pass_perc'])} high passes; {team_B} used {categorize_pass_type(features['B_high_pass_perc'])} high passes.",
            f"{team_A} had {categorize_shots(features['A_total_shots'])}; {team_B} had {categorize_shots(features['B_total_shots'])}.",
            f"{team_A} created {categorize_xg_per_shot(features['A_avg_xg_per_shot'])}; {team_B} created {categorize_xg_per_shot(features['B_avg_xg_per_shot'])}.",
            f"{team_A} applied {categorize_pressures(features['A_pressures_applied'])}; {team_B} applied {categorize_pressures(features['B_pressures_applied'])}.",
            f"{team_A} made {categorize_tackles(features['A_total_tackles'])}; {team_B} made {categorize_tackles(features['B_total_tackles'])}.",
            f"{team_A} were {categorize_tackle_success(features['A_tackle_success_rate'])}; {team_B} were {categorize_tackle_success(features['B_tackle_success_rate'])}.",
            "[EVENTS START]"
        ]

    def save_text_file(self, header_lines, path):
        with open(path, "w", encoding="utf-8") as f:
            f.write("\n".join(header_lines))
        print(f"✅ Header text saved to {path}")

    def tokenize_and_save(self, file_path, save_path):
        with open(file_path, "r", encoding="utf-8") as f:
            text = f.read()
        input_ids = self.tokenizer.encode(text, return_tensors="pt")
        torch.save(input_ids, save_path)
        print(f"✅ Tokenization complete. Shape: {input_ids.shape}")
        print(f"💾 Saved tokenized tensor to: {save_path}")

    def tokenize_text_and_save(self, text, save_path):
        input_ids = self.tokenizer.encode(text, return_tensors="pt")
        print(input_ids)
        torch.save(input_ids, save_path)
        print(f"✅ Tokenization complete. Shape: {input_ids.shape}")
        print(f"💾 Saved tokenized tensor to: {save_path}")

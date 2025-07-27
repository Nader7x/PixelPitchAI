import logging
import time
import torch
import uvicorn
import xgboost as xgb
from fastapi import FastAPI, HTTPException, BackgroundTasks, WebSocket, WebSocketDisconnect, Request
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates
from pydantic import BaseModel
from transformers import GPT2LMHeadModel, GPT2Tokenizer
from xgboost import XGBRegressor

from Parser_with_mq import parse_and_publish
from XgBoostClass import MatchStatProcessor

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

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="Football Match Simulation API",
    description="API for generating football match simulations using a fine-tuned GPT-2 model",
    version="1.0.0"
)


# Function to generate features
def generate_features(home_team_id, away_team_id, home_team_season, away_team_season, home_team_name, away_team_name):
    """
    Generates features for the match using XGBoost model

    Args:
        home_team_id: ID of the home team
        away_team_id: ID of the away team
        home_team_season: Season of the home team
        away_team_season: Season of the away team
        home_team_name: Name of the home team
        away_team_name: Name of the away team

    Returns:
        A dictionary with generated features
    """
    # Load XGBoost model
    booster = xgb.Booster()
    booster.load_model('tuned_xgboost_model.json')

    xgboost_model = XGBRegressor()
    xgboost_model._Booster = booster

    match_stat = MatchStatProcessor(xgboost_model, special_tokens=special_list)

    # Generate features
    features = match_stat.generate_features(home_team_id, away_team_id, home_team_season, away_team_season)
    header_lines = match_stat.convert_to_text(home_team_name, away_team_name, features)
    logger.info(f"Generated features for {home_team_name} vs {away_team_name}: {features}")
    match_stat.save_text_file(header_lines, f"./HeaderLines/{home_team_name}_vs_{away_team_name}_header_lines.txt")
    input_tokens_path = f"./InputTokens/{home_team_name}_vs_{away_team_name}_input_tokens.pt"
    match_stat.tokenize_text_and_save(header_lines, input_tokens_path)
    return input_tokens_path


# Mount static files
app.mount("/static", StaticFiles(directory="static"), name="static")

# Set up Jinja2 templates
templates = Jinja2Templates(directory="templates")

# Model configuration
MODEL_PATH = '../gpt2-football-finetuned'
SPECIAL_TOKENS = {
    "additional_special_tokens": ["[MATCH START]", "[EVENTS START]", "[SECOND HALF]", "[MATCH END]"]
}


# Load model and tokenizer
@app.on_event("startup")
async def startup_event():
    global tokenizer, model, device

    # Set device
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    # Load tokenizer
    tokenizer = GPT2Tokenizer.from_pretrained(MODEL_PATH)
    tokenizer.add_special_tokens(SPECIAL_TOKENS)
    tokenizer.pad_token = tokenizer.eos_token

    # Load model
    model = GPT2LMHeadModel.from_pretrained(MODEL_PATH)
    model.eval()
    model.to(device)

    print(f"Model loaded on {device}")


class MatchRequest(BaseModel):
    home_team_id: int
    away_team_id: int
    home_team_name: str
    away_team_name: str
    home_team_season: str
    away_team_season: str
    match_id: int


class MatchResponse(BaseModel):
    match_id: int
    home_team_name: str
    away_team_name: str
    home_team_season: str
    away_team_season: str
    events_count: int
    execution_time: float = 0.0
    preview: str = ""


# Helper function to load input tokens
def load_input_tokens(input_tokens_path):
    try:
        return torch.load(input_tokens_path).to(device)
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Failed to load input tokens: {str(e)}")


# Helper function to generate text
def generate_text(
        input_tokens_file=None,
        num_tokens_to_generate=200000,
        max_length=1024,
        temperature=0.7,
        top_p=0.9,
        top_k=50,
        callback=None,
        parse_events=False
):
    """
    Generate text using the model with optional event parsing and callback.

    Args:
        input_tokens: Pre-encoded input tokens (optional)
        input_text: Input text to encode (used if input_tokens is None)
        num_tokens_to_generate: Number of tokens to generate
        temperature: Temperature for sampling
        top_p: Top-p sampling parameter
        top_k: Top-k sampling parameter
        max_length: Maximum length of context window
        callback: Optional callback function to process generated chunks and events
                  Signature: callback(chunk_text, events, is_final)
        parse_events: Whether to parse events from generated text

    Returns:
        The generated text
        :param parse_events:
        :param callback:
        :param top_p:
        :param top_k:
        :param temperature:
        :param max_length:
        :param num_tokens_to_generate:
        :param input_tokens_file:
    """
    # Load input tokens
    input_tokens = load_input_tokens(input_tokens_file) if input_tokens_file else None
    # Initialize variables
    generated_tokens = input_tokens.clone()

    # Create attention mask where 1 is for valid tokens, 0 is for padding
    attention_mask = (input_tokens != tokenizer.pad_token_id).long()

    # Save match stats as frozen prefix
    frozen_prefix = input_tokens.clone()

    # Decode initial text
    initial_text = tokenizer.decode(input_tokens[0])

    num = 0
    for _ in range(num_tokens_to_generate):  # Generate one token at a time
        if generated_tokens.shape[1] + 100 >= max_length:
            keep_last_n = 400
            context_tail = generated_tokens[:, -keep_last_n:]

            # Reset generated_tokens
            generated_tokens = torch.cat((frozen_prefix, context_tail), dim=1)

            # Recompute attention mask using pad_token_id
            attention_mask = (generated_tokens != tokenizer.pad_token_id).long()

        # Current input to model
        current_input = generated_tokens[:, -max_length:]
        current_attention_mask = attention_mask[:, -max_length:]

        # Generate
        output = model.generate(
            input_ids=current_input,
            attention_mask=current_attention_mask,
            max_new_tokens=100,
            temperature=temperature,
            top_p=top_p,
            top_k=top_k,
            do_sample=True,
            pad_token_id=tokenizer.eos_token_id,
        )
        num += 100

        # Append new tokens
        new_token = output[:, -100:]
        generated_tokens = torch.cat((generated_tokens, new_token), dim=1)
        new_attention_mask = torch.ones_like(new_token).to(device)
        attention_mask = torch.cat((attention_mask, new_attention_mask), dim=1)

        # Decode and append new text
        new_text = tokenizer.decode(new_token[0])
        initial_text += new_text

        if num >= num_tokens_to_generate:
            break
    return initial_text


@app.get("/api")
async def api_root():
    """
    API root endpoint
    """
    return {"message": "Football Match Simulation API is running"}


@app.post("/startMatch", response_model=MatchResponse)
async def start_match(request: MatchRequest, background_tasks: BackgroundTasks):
    """
    Start a match simulation between two teams, generate events, and send them to RabbitMQ.
    New flow: generate full match events, save to file, then parse and publish events.
    """
    start_time = time.time()
    try:
        # splitting the season to make it one year "2015/2016" => 2016
        home_team_season = request.home_team_season.split("/")[1]
        away_team_season = request.away_team_season.split("/")[1]
        # Convert team names and combine them with season "Real Madrid" => Real_Madrid_2016
        home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
        away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"
        input_tokens_path = generate_features(request.home_team_id, request.away_team_id, home_team_season,
                                              away_team_season, home_team_name, away_team_name)
        # 1. Generate full match events and save to file
        generated_text = generate_text(
            input_tokens_file=input_tokens_path,
            num_tokens_to_generate=request.num_tokens_to_generate,
            temperature=request.temperature,
            top_p=request.top_p,
            top_k=request.top_k,
            max_length=request.max_length
        )
        simulated_match_path = f"match_{request.home_team_name}_{request.home_team_season}_vs_{request.away_team_name}_{request.away_team_season}_{request.match_id}.txt"
        with open(f"./SimulatedMatches/{simulated_match_path}", "w", encoding="utf-8") as f:
            f.write(generated_text)
        logger.info(f"Generated match events saved to ./SimulatedMatches/{simulated_match_path}")
        # 2. Parse events and publish to RabbitMQ
        events = parse_and_publish(f"./SimulatedMatches/{simulated_match_path}")

        # Create a preview of the generated text (first 200 chars)
        preview = generated_text[:200] + "..." if len(generated_text) > 2000 else generated_text

        # Calculate execution time
        execution_time = time.time() - start_time

        # Return response
        return MatchResponse(
            match_id=request.match_id,
            home_team_name=request.home_team_name,
            away_team_name=request.away_team_name,
            home_team_season=request.home_team_season,
            away_team_season=request.away_team_season,
            events_count=len(events),
            execution_time=execution_time,
            preview=preview
        )

    except Exception as e:
        logger.error(f"Error in start match: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Match simulation failed: {str(e)}")


if __name__ == "__main__":
    uvicorn.run("api:app", host="0.0.0.0", port=8000, reload=True)

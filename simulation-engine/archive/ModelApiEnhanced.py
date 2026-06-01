"""
Enhanced Football Match Simulation API v2.0.0
Advanced API for generating football match simulations using fine-tuned GPT-2 and XGBoost models.

Features:
- Asynchronous match simulation processing
- JWT and API Key authentication with API key management
- Real-time simulation status tracking
- Enhanced error handling and logging
- Comprehensive API documentation
- Statistics and monitoring endpoints
"""

import asyncio
import json
import logging
import os
import secrets
import time
import torch
import uuid
import uvicorn
from contextlib import asynccontextmanager
from datetime import datetime, timedelta
from fastapi import FastAPI, HTTPException, BackgroundTasks, Depends, status, Security, Query
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import APIKeyHeader, OAuth2PasswordBearer, OAuth2PasswordRequestForm
from pydantic import BaseModel, Field
from transformers import GPT2LMHeadModel, GPT2Tokenizer
from typing import Optional, List, Dict

# Suppress warnings
logging.getLogger("passlib.handlers.bcrypt").setLevel(logging.ERROR)

# Authentication libraries
from jose import JWTError, jwt
from passlib.context import CryptContext

# Local imports
from Parser import parse_and_publish
from XgBoostClass import MatchStatProcessor
import xgboost as xgb
from xgboost import XGBRegressor

# Additional imports for new endpoints
import aiohttp
from fastapi.responses import StreamingResponse

# --- Configuration ---
MODEL_PATH = os.getenv('MODEL_PATH', './gpt2-football-finetuned')
XGB_MODEL_PATH = os.getenv('XGB_MODEL_PATH', 'tuned_xgboost_model.json')
HEADERLINES_DIR = os.getenv('HEADERLINES_DIR', './HeaderLines')
INPUTTOKENS_DIR = os.getenv('INPUTTOKENS_DIR', './InputTokens')
SIMMATCHES_DIR = os.getenv('SIMMATCHES_DIR', './SimulatedMatches')
MATCHES_DIR = os.getenv('MATCHES_DIR', './Matches')
PARSED_DIR = os.getenv('PARSED_DIR', './Matches_Parsed')
STATUS_DIR = os.getenv('STATUS_DIR', './SimulationStatus')

SPECIAL_TOKENS = {
    "additional_special_tokens": ["[MATCH START]", "[EVENTS START]", "[SECOND HALF]", "[MATCH END]"]
}

# Authentication Configuration
SECRET_KEY = os.getenv("SECRET_KEY", "REDACTED_SECRET")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 30

API_KEY_NAME = "X-API-Key"
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

# Enhanced API Keys storage with metadata
API_KEYS: Dict[str, dict] = {
    os.getenv("API_KEY", "REDACTED_TEST_KEY"): {
        "username": "admin",
        "key_id": "default_key",
        "description": "Default test API key",
        "created_at": datetime.utcnow().isoformat(),
        "expires_at": None,
        "last_used": None,
        "is_active": True
    }
}

# In-memory storage for user API keys (in production, use a database)
user_api_keys: Dict[str, List[str]] = {
    "admin": [os.getenv("API_KEY", "REDACTED_TEST_KEY")]
}

oauth2_scheme = OAuth2PasswordBearer(tokenUrl="token", auto_error=False)
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

# Enhanced Logging Configuration
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('football_api.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)

# Team and season data
special_list = [
    "Deportivo_Alavés_2016", "Deportivo_Alavés_2017", "Deportivo_Alavés_2018", "Deportivo_Alavés_2019",
    "Deportivo_Alavés_2020", "Deportivo_Alavés_2021",
    "Barcelona_2016", "Barcelona_2017", "Barcelona_2018", "Barcelona_2019", "Barcelona_2020", "Barcelona_2021",
    "Granada_2016", "Granada_2017", "Granada_2018", "Granada_2019", "Granada_2020", "Granada_2021",
    "Celta_Vigo_2016", "Celta_Vigo_2017", "Celta_Vigo_2018", "Celta_Vigo_2019", "Celta_Vigo_2020", "Celta_Vigo_2021",
    "Real_Betis_2016", "Real_Betis_2017", "Real_Betis_2018", "Real_Betis_2019", "Real_Betis_2020", "Real_Betis_2021",
    "Osasuna_2016", "Osasuna_2017", "Osasuna_2018", "Osasuna_2019", "Osasuna_2020", "Osasuna_2021",
    "Real_Madrid_2016", "Real_Madrid_2017", "Real_Madrid_2018", "Real_Madrid_2019", "Real_Madrid_2020",
    "Real_Madrid_2021",
    "Levante_UD_2016", "Levante_UD_2017", "Levante_UD_2018", "Levante_UD_2019", "Levante_UD_2020", "Levante_UD_2021",
    "Villarreal_2016", "Villarreal_2017", "Villarreal_2018", "Villarreal_2019", "Villarreal_2020", "Villarreal_2021",
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
    "Las_Palmas_2016", "Las_Palmas_2017", "Las_Palmas_2018", "Las_Palmas_2019", "Las_Palmas_2020", "Las_Palmas_2021",
    "Málaga_2016", "Málaga_2017", "Málaga_2018", "Málaga_2019", "Málaga_2020", "Málaga_2021",
    "Sporting_Gijón_2016", "Sporting_Gijón_2017", "Sporting_Gijón_2018", "Sporting_Gijón_2019",
    "Sporting_Gijón_2020", "Sporting_Gijón_2021",
    "[MATCH START]", "[EVENTS START]", "[STOPPAGE TIME - FIRST HALF]", "[END OF FIRST HALF]",
    "[SECOND HALF START]", "[STOPPAGE TIME - SECOND HALF]", "[MATCH END]"
]


# Utility functions
def ensure_directories():
    """Ensure all required directories exist"""
    directories = [HEADERLINES_DIR, INPUTTOKENS_DIR, SIMMATCHES_DIR, MATCHES_DIR, PARSED_DIR, STATUS_DIR]
    for directory in directories:
        os.makedirs(directory, exist_ok=True)
        logger.info(f"Ensured directory exists: {directory}")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application lifecycle"""
    logger.info("Starting Football Match Simulation API v2.0.0...")
    ensure_directories()
    logger.info("All directories ready")
    yield
    logger.info("Shutting down Football Match Simulation API...")


# FastAPI App
app = FastAPI(
    title="Football Match Simulation API",
    description="Advanced API for generating football match simulations using fine-tuned GPT-2 and XGBoost models",
    version="2.0.0",
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc"
)

# CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# --- Pydantic Models ---
class Token(BaseModel):
    """JWT Token response model"""
    access_token: str
    token_type: str


class TokenData(BaseModel):
    """Token data for validation"""
    username: Optional[str] = None


class User(BaseModel):
    """User information model"""
    username: str
    email: Optional[str] = None
    full_name: Optional[str] = None
    disabled: Optional[bool] = None


class UserInDB(User):
    """User in database with hashed password"""
    hashed_password: str


class MatchRequest(BaseModel):
    """Enhanced match simulation request model with validation"""
    home_team_id: int = Field(..., ge=1, description="Home team ID (must be positive)")
    away_team_id: int = Field(..., ge=1, description="Away team ID (must be positive)")
    home_team_name: str = Field(..., min_length=1, max_length=100, description="Home team name")
    away_team_name: str = Field(..., min_length=1, max_length=100, description="Away team name")
    home_team_season: str = Field(..., pattern=r'^\d{4}/\d{4}$', description="Home team season (e.g., 2020/2021)")
    away_team_season: str = Field(..., pattern=r'^\d{4}/\d{4}$', description="Away team season (e.g., 2020/2021)")
    match_id: int = Field(..., ge=1, description="Unique match identifier")
    num_tokens_to_generate: int = Field(200000, gt=0, le=300000, description="Number of tokens to generate")
    temperature: float = Field(0.7, ge=0.0, le=2.0, description="Sampling temperature for randomness")
    top_p: float = Field(0.9, ge=0.0, le=1.0, description="Top-p sampling parameter")
    top_k: int = Field(50, ge=1, le=1000, description="Top-k sampling parameter")
    max_length: int = Field(1024, ge=128, le=2048, description="Maximum sequence length")

    class Config:
        json_schema_extra = {
            "example": {
                "home_team_id": 1,
                "away_team_id": 2,
                "home_team_name": "Real Madrid",
                "away_team_name": "Barcelona",
                "home_team_season": "2020/2021",
                "away_team_season": "2020/2021",
                "match_id": 12345,
                "num_tokens_to_generate": 200000,
                "temperature": 0.7,
                "top_p": 0.9,
                "top_k": 50,
                "max_length": 1024
            }
        }


class MatchResponse(BaseModel):
    """Match simulation response model"""
    match_id: int
    home_team_name: str
    away_team_name: str
    home_team_season: str
    away_team_season: str
    events_count: int = 0
    execution_time: float = 0.0
    preview: str = ""
    simulation_id: str = ""
    status: str = "pending"


class SimulationStatus(BaseModel):
    """Enhanced simulation status model"""
    simulation_id: str
    match_id: int
    status: str = Field(..., description="Status: pending, processing, completed, failed")
    progress: float = Field(0.0, ge=0.0, le=100.0, description="Progress percentage (0-100)")
    start_time: float
    end_time: Optional[float] = None
    error_message: Optional[str] = None
    events_count: Optional[int] = None
    result_path: Optional[str] = None
    metadata: dict = {}


class APIInfo(BaseModel):
    """API information model"""
    name: str
    version: str
    description: str
    status: str
    timestamp: str
    model_info: dict


class HealthCheck(BaseModel):
    """Health check response model"""
    status: str
    timestamp: str
    version: str
    model_loaded: bool
    xgboost_loaded: bool


class APIKeyRequest(BaseModel):
    """Request model for generating an API key"""
    description: Optional[str] = Field(None, max_length=200, description="Optional description for the API key")
    expires_in_days: Optional[int] = Field(30, ge=1, le=365, description="Number of days until the API key expires")


class APIKeyResponse(BaseModel):
    """Response model for API key generation"""
    api_key: str
    description: Optional[str] = None
    created_at: str
    expires_at: Optional[str] = None
    key_id: str


class APIKeyInfo(BaseModel):
    """Information about an API key (without the actual key)"""
    key_id: str
    description: Optional[str] = None
    created_at: str
    expires_at: Optional[str] = None
    last_used: Optional[str] = None
    is_active: bool


class UserAPIKeysResponse(BaseModel):
    """Response model for listing user's API keys"""
    api_keys: List[APIKeyInfo]
    total_count: int


class WebhookRequest(BaseModel):
    """Request model for webhook configuration"""
    webhook_url: str = Field(..., description="URL to call when simulation completes")
    webhook_secret: Optional[str] = Field(None, description="Optional secret for webhook authentication")


class SimulationWaitRequest(BaseModel):
    """Request model for waiting simulation result"""
    timeout_seconds: int = Field(300, ge=1, le=1800, description="Maximum time to wait in seconds (1-1800)")
    poll_interval: float = Field(2.0, ge=0.5, le=10.0, description="Polling interval in seconds")


class WebhookResponse(BaseModel):
    """Response model for webhook notifications"""
    simulation_id: str
    status: str
    result_url: Optional[str] = None
    error_message: Optional[str] = None
    timestamp: str


# --- Model Resources ---
class ModelResources:
    def __init__(self):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.tokenizer = GPT2Tokenizer.from_pretrained(MODEL_PATH)
        self.tokenizer.add_special_tokens(SPECIAL_TOKENS)
        self.tokenizer.pad_token = self.tokenizer.eos_token
        self.model = GPT2LMHeadModel.from_pretrained(MODEL_PATH)
        self.model.eval()
        self.model.to(self.device)

        booster = xgb.Booster()
        booster.load_model(XGB_MODEL_PATH)
        self.xgboost_model = XGBRegressor()
        self.xgboost_model._Booster = booster
        self.match_stat = MatchStatProcessor(self.xgboost_model, special_tokens=special_list)


resources = ModelResources()


def get_resources():
    return resources


# Mock users database
fake_users_db = {
    "admin": {
        "username": "admin",
        "full_name": "Administrator",
        "email": "admin@example.com",
        "hashed_password": pwd_context.hash("adminpassword"),
        "disabled": False,
    }
}

# In-memory simulation status tracking
simulation_status = {}


# --- API Key Management Functions ---
def generate_api_key() -> str:
    """Generate a secure random API key"""
    return f"fsa_{secrets.token_urlsafe(32)}"


def create_api_key_for_user(username: str, description: Optional[str] = None,
                            expires_in_days: Optional[int] = None) -> dict:
    """Create a new API key for a user"""
    try:
        api_key = generate_api_key()
        key_id = str(uuid.uuid4())
        created_at = datetime.utcnow()
        expires_at = created_at + timedelta(days=expires_in_days) if expires_in_days else None

        # Store API key metadata
        API_KEYS[api_key] = {
            "username": username,
            "key_id": key_id,
            "description": description,
            "created_at": created_at.isoformat(),
            "expires_at": expires_at.isoformat() if expires_at else None,
            "last_used": None,
            "is_active": True
        }

        # Add to user's API keys list
        if username not in user_api_keys:
            user_api_keys[username] = []
        user_api_keys[username].append(api_key)

        logger.info(f"Created new API key for user {username}: {key_id}")

        return {
            "api_key": api_key,
            "key_id": key_id,
            "description": description,
            "created_at": created_at.isoformat(),
            "expires_at": expires_at.isoformat() if expires_at else None
        }
    except Exception as e:
        logger.error(f"Error creating API key: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to create API key")


def revoke_api_key(api_key: str, username: str) -> bool:
    """Revoke an API key for a user"""
    try:
        if api_key in API_KEYS and API_KEYS[api_key]["username"] == username:
            API_KEYS[api_key]["is_active"] = False
            if username in user_api_keys and api_key in user_api_keys[username]:
                user_api_keys[username].remove(api_key)
            logger.info(f"Revoked API key for user {username}: {API_KEYS[api_key]['key_id']}")
            return True
        return False
    except Exception as e:
        logger.error(f"Error revoking API key: {str(e)}")
        return False


def get_user_api_keys(username: str) -> List[APIKeyInfo]:
    """Get all API keys for a user"""
    try:
        user_keys = []
        if username in user_api_keys:
            for api_key in user_api_keys[username]:
                if api_key in API_KEYS and API_KEYS[api_key]["is_active"]:
                    key_info = API_KEYS[api_key]
                    user_keys.append(APIKeyInfo(
                        key_id=key_info["key_id"],
                        description=key_info["description"],
                        created_at=key_info["created_at"],
                        expires_at=key_info["expires_at"],
                        last_used=key_info["last_used"],
                        is_active=key_info["is_active"]
                    ))
        return user_keys
    except Exception as e:
        logger.error(f"Error getting user API keys: {str(e)}")
        return []


def update_api_key_last_used(api_key: str):
    """Update the last used timestamp for an API key"""
    try:
        if api_key in API_KEYS:
            API_KEYS[api_key]["last_used"] = datetime.utcnow().isoformat()
    except Exception as e:
        logger.error(f"Error updating API key last used: {str(e)}")


# --- Utility Functions ---
def generate_features(home_team_id, away_team_id, home_team_season, away_team_season, home_team_name, away_team_name,
                      match_stat):
    """Generate features for match simulation using XGBoost model"""
    try:
        features = match_stat.generate_features(home_team_id, away_team_id, home_team_season, away_team_season)
        header_lines = match_stat.convert_to_text(home_team_name, away_team_name, features)
        logger.info(f"Generated features for {home_team_name} vs {away_team_name}: {features}")

        os.makedirs(HEADERLINES_DIR, exist_ok=True)
        os.makedirs(INPUTTOKENS_DIR, exist_ok=True)

        header_path = os.path.join(HEADERLINES_DIR, f"{home_team_name}_vs_{away_team_name}_header_lines.txt")
        match_stat.save_text_file(header_lines, header_path)

        input_tokens_path = os.path.join(INPUTTOKENS_DIR, f"{home_team_name}_vs_{away_team_name}_input_tokens.pt")
        match_stat.tokenize_text_and_save(header_lines, input_tokens_path)

        return input_tokens_path
    except Exception as e:
        logger.error(f"Error generating features: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to generate features: {str(e)}")


def load_input_tokens(input_tokens_path, device):
    """Load input tokens from file"""
    try:
        return torch.load(input_tokens_path).to(device)
    except Exception as e:
        logger.error(f"Error loading input tokens: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to load input tokens: {str(e)}")


def generate_text(resources, input_tokens_file=None, num_tokens_to_generate=200000, max_length=1024, temperature=0.7,
                  top_p=0.9, top_k=50):
    """Generate text using the fine-tuned GPT-2 model"""
    try:
        tokenizer = resources.tokenizer
        model = resources.model
        device = resources.device

        input_tokens = load_input_tokens(input_tokens_file, device) if input_tokens_file else None
        if input_tokens is None:
            raise HTTPException(status_code=400, detail="No input tokens provided")

        generated_tokens = input_tokens.clone()
        attention_mask = (input_tokens != tokenizer.pad_token_id).long()
        frozen_prefix = input_tokens.clone()
        initial_text = tokenizer.decode(input_tokens[0])
        num = 0

        logger.info(f"Starting text generation with {num_tokens_to_generate} tokens")

        for _ in range(num_tokens_to_generate):
            if generated_tokens.shape[1] + 100 >= max_length:
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
                temperature=temperature,
                top_p=top_p,
                top_k=top_k,
                do_sample=True,
                pad_token_id=tokenizer.eos_token_id,
            )

            num += 100
            new_token = output[:, -100:]
            generated_tokens = torch.cat((generated_tokens, new_token), dim=1)
            new_attention_mask = torch.ones_like(new_token).to(device)
            attention_mask = torch.cat((attention_mask, new_attention_mask), dim=1)
            new_text = tokenizer.decode(new_token[0])
            initial_text += new_text

            if num >= num_tokens_to_generate:
                break

        logger.info(f"Text generation completed. Generated {len(initial_text)} characters")
        return initial_text

    except Exception as e:
        logger.error(f"Error in text generation: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Text generation failed: {str(e)}")


def update_simulation_status(sim_id, status_update):
    """Update the status of a simulation in memory and on disk"""
    try:
        if sim_id in simulation_status:
            current_status = simulation_status[sim_id].dict()
            current_status.update(status_update)
            simulation_status[sim_id] = SimulationStatus(**current_status)
        else:
            logger.warning(f"Simulation {sim_id} not found in memory, creating new entry")
            simulation_status[sim_id] = SimulationStatus(simulation_id=sim_id, **status_update)

        os.makedirs(STATUS_DIR, exist_ok=True)
        status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")
        with open(status_path, 'w') as f:
            json.dump(simulation_status[sim_id].dict(), f, indent=2)

        return simulation_status[sim_id]
    except Exception as e:
        logger.error(f"Error updating simulation status: {str(e)}")
        return None


async def process_match_simulation(simulation_id: str, request: MatchRequest, model_resources: ModelResources):
    """Process a match simulation asynchronously"""
    start_time = time.time()

    try:
        update_simulation_status(simulation_id, {"status": "processing", "progress": 5.0})

        home_team_season = request.home_team_season.split("/")[-1]
        away_team_season = request.away_team_season.split("/")[-1]
        home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
        away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"

        input_tokens_path = generate_features(
            request.home_team_id, request.away_team_id, home_team_season, away_team_season,
            home_team_name, away_team_name, model_resources.match_stat
        )
        update_simulation_status(simulation_id, {"progress": 20.0})

        os.makedirs(SIMMATCHES_DIR, exist_ok=True)
        generated_text = generate_text(
            resources=model_resources,
            input_tokens_file=input_tokens_path,
            num_tokens_to_generate=request.num_tokens_to_generate,
            temperature=request.temperature,
            top_p=request.top_p,
            top_k=request.top_k,
            max_length=request.max_length
        )
        update_simulation_status(simulation_id, {"progress": 80.0})

        simulated_match_path = os.path.join(SIMMATCHES_DIR, f"match_{request.match_id}_{simulation_id}.txt")
        with open(simulated_match_path, "w", encoding="utf-8") as f:
            f.write(generated_text)

        logger.info(f"Generated match events saved to {simulated_match_path}")

        events = parse_and_publish(simulated_match_path)
        logger.info(f"Successfully parsed and published {len(events)} events")

        update_simulation_status(simulation_id, {
            "status": "completed",
            "progress": 100.0,
            "end_time": time.time(),
            "events_count": len(events),
            "result_path": simulated_match_path
        })

        # Trigger any registered webhooks
        await trigger_webhooks(simulation_id)

    except Exception as e:
        logger.error(f"Error in match simulation: {str(e)}")

        update_simulation_status(simulation_id, {
            "status": "failed",
            "end_time": time.time(),
            "error_message": str(e)
        })

        # Trigger any registered webhooks for failed simulations
        await trigger_webhooks(simulation_id)
        raise


# --- Authentication Functions ---
def verify_password(plain_password, hashed_password):
    return pwd_context.verify(plain_password, hashed_password)


def get_password_hash(password):
    return pwd_context.hash(password)


def get_user(db, username: str):
    if username in db:
        user_dict = db[username]
        return UserInDB(**user_dict)
    return None


def authenticate_user(fake_db, username: str, password: str):
    user = get_user(fake_db, username)
    if not user:
        return False
    if not verify_password(password, user.hashed_password):
        return False
    return user


def create_access_token(data: dict, expires_delta: Optional[timedelta] = None):
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=15)
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)
    return encoded_jwt


async def get_current_user(token: str = Depends(oauth2_scheme)):
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )

    # Handle case where token is None (no Authorization header)
    if token is None:
        raise credentials_exception

    try:
        payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
        username: str = payload.get("sub")
        if username is None:
            raise credentials_exception
        token_data = TokenData(username=username)
    except JWTError:
        raise credentials_exception
    user = get_user(fake_users_db, username=token_data.username)
    if user is None:
        raise credentials_exception
    return user


async def get_current_active_user(current_user: User = Depends(get_current_user)):
    if current_user.disabled:
        raise HTTPException(status_code=400, detail="Inactive user")
    return current_user


async def get_api_key(api_key_header: str = Security(api_key_header)):
    if api_key_header in API_KEYS:
        key_info = API_KEYS[api_key_header]
        # Check if key is active and not expired
        if not key_info["is_active"]:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="API Key has been revoked",
            )

        # Check expiration
        if key_info["expires_at"]:
            expires_at = datetime.fromisoformat(key_info["expires_at"])
            if datetime.utcnow() > expires_at:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="API Key has expired",
                )

        # Update last used timestamp
        update_api_key_last_used(api_key_header)
        return api_key_header
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid API Key",
    )


async def get_api_key_optional(api_key_header: str = Security(api_key_header)):
    """Get API key if present, return None if not present (for flexible auth)"""
    if not api_key_header:
        return None

    if api_key_header in API_KEYS:
        key_info = API_KEYS[api_key_header]
        # Check if key is active and not expired
        if not key_info["is_active"]:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="API Key has been revoked",
            )

        # Check expiration
        if key_info["expires_at"]:
            expires_at = datetime.fromisoformat(key_info["expires_at"])
            if datetime.utcnow() > expires_at:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="API Key has expired",
                )

        # Update last used timestamp
        update_api_key_last_used(api_key_header)
        return api_key_header

    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid API Key",
    )


async def get_current_user_flexible(
        token: str = Depends(oauth2_scheme),
        api_key: str = Depends(get_api_key_optional)
):
    """Get current user from either JWT token or API key"""
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )

    # First try API key authentication
    if api_key:
        username = API_KEYS[api_key]["username"]
        user = get_user(fake_users_db, username=username)
        if user is None:
            raise credentials_exception
        return user

    # Then try JWT token authentication
    if token:
        try:
            payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
            username: str = payload.get("sub")
            if username is None:
                raise credentials_exception
            token_data = TokenData(username=username)
            user = get_user(fake_users_db, username=token_data.username)
            if user is None:
                raise credentials_exception
            return user
        except JWTError:
            raise credentials_exception

    # No valid authentication found
    raise credentials_exception


async def get_current_active_user_flexible(current_user: User = Depends(get_current_user_flexible)):
    if current_user.disabled:
        raise HTTPException(status_code=400, detail="Inactive user")
    return current_user


async def get_auth(api_key: str = Depends(get_api_key), token: str = Depends(oauth2_scheme)):
    """Support both API key and JWT authentication methods"""
    if api_key:
        return {"auth_type": "api_key", "username": API_KEYS[api_key]["username"]}
    if token:
        try:
            payload = jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
            username = payload.get("sub")
            if username and username in fake_users_db:
                return {"auth_type": "jwt", "username": username}
        except JWTError:
            pass
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Authentication required",
        headers={"WWW-Authenticate": "Bearer"},
    )


# --- Webhook Functions ---
async def send_webhook_notification(webhook_url: str, payload: dict, secret: Optional[str] = None):
    """Send webhook notification when simulation completes"""
    try:
        import aiohttp
        import hmac
        import hashlib

        headers = {"Content-Type": "application/json"}

        # Add signature if secret provided
        if secret:
            payload_str = json.dumps(payload, sort_keys=True)
            signature = hmac.new(
                secret.encode('utf-8'),
                payload_str.encode('utf-8'),
                hashlib.sha256
            ).hexdigest()
            headers["X-Webhook-Signature"] = f"sha256={signature}"

        async with aiohttp.ClientSession() as session:
            async with session.post(
                    webhook_url,
                    json=payload,
                    headers=headers,
                    timeout=aiohttp.ClientTimeout(total=30)
            ) as response:
                if response.status == 200:
                    logger.info(f"Webhook notification sent successfully to {webhook_url}")
                    return True
                else:
                    logger.warning(f"Webhook notification failed with status {response.status}")
                    return False

    except Exception as e:
        logger.error(f"Error sending webhook notification: {str(e)}")
        return False


async def trigger_webhooks(simulation_id: str):
    """Trigger all registered webhooks for a simulation"""
    if simulation_id not in simulation_status:
        return

    sim_status = simulation_status[simulation_id]
    webhooks = getattr(sim_status, 'webhooks', [])

    if not webhooks:
        return

    # Create webhook payload
    payload = WebhookResponse(
        simulation_id=simulation_id,
        status=sim_status.status,
        result_url=f"/simulationResult/{simulation_id}" if sim_status.status == "completed" else None,
        error_message=sim_status.error_message,
        timestamp=datetime.utcnow().isoformat()
    ).dict()

    # Send notifications to all registered webhooks
    for webhook in webhooks:
        asyncio.create_task(
            send_webhook_notification(
                webhook["url"],
                payload,
                webhook.get("secret")
            )
        )


# --- API Endpoints ---
@app.post("/token", response_model=Token, tags=["Authentication"])
async def login_for_access_token(form_data: OAuth2PasswordRequestForm = Depends()):
    """Endpoint for users to get a JWT token by providing username and password"""
    user = authenticate_user(fake_users_db, form_data.username, form_data.password)
    if not user:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Incorrect username or password",
            headers={"WWW-Authenticate": "Bearer"},
        )
    access_token_expires = timedelta(minutes=ACCESS_TOKEN_EXPIRE_MINUTES)
    access_token = create_access_token(
        data={"sub": user.username}, expires_delta=access_token_expires
    )
    return {"access_token": access_token, "token_type": "bearer"}


# --- API Key Management Endpoints ---

@app.post("/api-keys/generate", response_model=APIKeyResponse, tags=["API Keys"])
async def generate_api_key_endpoint(
        request: APIKeyRequest,
        current_user: User = Depends(get_current_active_user_flexible)
):
    """Generate a new API key for the authenticated user"""
    try:
        key_data = create_api_key_for_user(
            username=current_user.username,
            description=request.description,
            expires_in_days=request.expires_in_days
        )

        return APIKeyResponse(
            api_key=key_data["api_key"],
            description=key_data["description"],
            created_at=key_data["created_at"],
            expires_at=key_data["expires_at"],
            key_id=key_data["key_id"]
        )
    except Exception as e:
        logger.error(f"Error generating API key for user {current_user.username}: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to generate API key")


@app.get("/api-keys", response_model=UserAPIKeysResponse, tags=["API Keys"])
async def list_user_api_keys(
        current_user: User = Depends(get_current_active_user_flexible)
):
    """List all API keys for the authenticated user"""
    try:
        user_keys = get_user_api_keys(current_user.username)
        return UserAPIKeysResponse(
            api_keys=user_keys,
            total_count=len(user_keys)
        )
    except Exception as e:
        logger.error(f"Error listing API keys for user {current_user.username}: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to retrieve API keys")


@app.delete("/api-keys/{key_id}", tags=["API Keys"])
async def revoke_api_key_endpoint(
        key_id: str,
        current_user: User = Depends(get_current_active_user_flexible)
):
    """Revoke an API key by its ID"""
    try:
        # Find the API key by key_id
        api_key_to_revoke = None
        for api_key, key_info in API_KEYS.items():
            if key_info["key_id"] == key_id and key_info["username"] == current_user.username:
                api_key_to_revoke = api_key
                break

        if not api_key_to_revoke:
            raise HTTPException(
                status_code=404,
                detail="API key not found or does not belong to current user"
            )

        success = revoke_api_key(api_key_to_revoke, current_user.username)
        if success:
            return {"message": "API key successfully revoked", "key_id": key_id}
        else:
            raise HTTPException(status_code=500, detail="Failed to revoke API key")

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error revoking API key {key_id} for user {current_user.username}: {str(e)}")
        raise HTTPException(status_code=500, detail="Failed to revoke API key")


@app.get("/users/me", response_model=User, tags=["Users"])
async def get_current_user_info(current_user: User = Depends(get_current_active_user_flexible)):
    """Get current user information (works with both JWT and API key authentication)"""
    return current_user


@app.get("/", response_model=APIInfo, tags=["Info"])
async def api_info():
    """Enhanced API information endpoint"""
    return APIInfo(
        name="Football Match Simulation API",
        version="2.0.0",
        description="Advanced API for generating football match simulations using fine-tuned GPT-2 and XGBoost",
        status="active",
        timestamp=datetime.utcnow().isoformat(),
        model_info={
            "gpt2_model_path": MODEL_PATH,
            "xgboost_model_path": XGB_MODEL_PATH,
            "device": str(resources.device),
            "special_tokens_count": len(SPECIAL_TOKENS["additional_special_tokens"])
        }
    )


@app.get("/health", response_model=HealthCheck, tags=["Health"])
async def health_check():
    """Enhanced health check endpoint with model status"""
    try:
        model_loaded = resources.model is not None and resources.tokenizer is not None
        xgboost_loaded = resources.xgboost_model is not None

        return HealthCheck(
            status="healthy" if model_loaded and xgboost_loaded else "degraded",
            timestamp=datetime.utcnow().isoformat(),
            version="2.0.0",
            model_loaded=model_loaded,
            xgboost_loaded=xgboost_loaded
        )
    except Exception as e:
        logger.error(f"Health check failed: {str(e)}")
        raise HTTPException(status_code=503, detail="Service unavailable")


@app.post("/startMatch", response_model=MatchResponse, tags=["Simulation"])
async def start_match(
        request: MatchRequest,
        background_tasks: BackgroundTasks,
        resources: ModelResources = Depends(get_resources),
        auth: dict = Depends(get_auth)
):
    """Start a match simulation asynchronously"""
    start_time = time.time()
    simulation_id = str(uuid.uuid4())

    try:
        simulation_status[simulation_id] = SimulationStatus(
            simulation_id=simulation_id,
            match_id=request.match_id,
            status="pending",
            progress=0.0,
            start_time=start_time,
            metadata={
                "home_team": request.home_team_name,
                "away_team": request.away_team_name,
                "home_team_season": request.home_team_season,
                "away_team_season": request.away_team_season,
                "requester": auth.get("username", "unknown")
            }
        )

        background_tasks.add_task(process_match_simulation, simulation_id, request, resources)

        return MatchResponse(
            match_id=request.match_id,
            home_team_name=request.home_team_name,
            away_team_name=request.away_team_name,
            home_team_season=request.home_team_season,
            away_team_season=request.away_team_season,
            events_count=0,
            execution_time=time.time() - start_time,
            simulation_id=simulation_id,
            status="pending"
        )

    except Exception as e:
        logger.error(f"Error starting match simulation: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to start match simulation: {str(e)}"
        )


@app.get("/simulationStatus/{simulation_id}", response_model=SimulationStatus, tags=["Simulation"])
async def get_simulation_status(simulation_id: str, auth: dict = Depends(get_auth)):
    """Get the status of a simulation by its ID"""
    if simulation_id not in simulation_status:
        try:
            status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
            if os.path.exists(status_path):
                with open(status_path, 'r') as f:
                    loaded_status = json.load(f)
                    simulation_status[simulation_id] = SimulationStatus(**loaded_status)
            else:
                raise HTTPException(
                    status_code=status.HTTP_404_NOT_FOUND,
                    detail=f"Simulation with ID {simulation_id} not found"
                )
        except Exception as e:
            logger.error(f"Error loading simulation status: {str(e)}")
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Simulation with ID {simulation_id} not found"
            )

    return simulation_status[simulation_id]


@app.get("/simulationResult/{simulation_id}", tags=["Simulation"])
async def get_simulation_result(simulation_id: str, auth: dict = Depends(get_auth)):
    """Get the raw text result of a completed simulation"""
    if simulation_id not in simulation_status:
        status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
        if os.path.exists(status_path):
            with open(status_path, 'r') as f:
                loaded_status = json.load(f)
                simulation_status[simulation_id] = SimulationStatus(**loaded_status)
        else:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Simulation with ID {simulation_id} not found"
            )

    sim_status = simulation_status[simulation_id]

    if sim_status.status != "completed":
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Simulation is not completed. Current status: {sim_status.status}"
        )

    if not sim_status.result_path or not os.path.exists(sim_status.result_path):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="Simulation result file not found"
        )

    try:
        with open(sim_status.result_path, 'r', encoding='utf-8') as f:
            content = f.read()
        return {"simulation_id": simulation_id, "content": content}
    except Exception as e:
        logger.error(f"Error reading simulation result: {str(e)}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to read simulation result: {str(e)}"
        )


# --- New Enhanced Simulation Result Endpoints ---

@app.post("/simulations/{simulation_id}/webhook", tags=["Simulation"])
async def register_simulation_webhook(
        simulation_id: str,
        webhook_request: WebhookRequest,
        auth: dict = Depends(get_auth)
):
    """Register a webhook to be called when simulation completes"""

    # Check if simulation exists
    if simulation_id not in simulation_status:
        # Try to load from disk
        status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
        if os.path.exists(status_path):
            with open(status_path, 'r') as f:
                loaded_status = json.load(f)
                simulation_status[simulation_id] = SimulationStatus(**loaded_status)
        else:
            raise HTTPException(
                status_code=status.HTTP_404_NOT_FOUND,
                detail=f"Simulation with ID {simulation_id} not found"
            )

    sim_status = simulation_status[simulation_id]

    # Don't allow webhook registration for completed/failed simulations
    if sim_status.status in ["completed", "failed"]:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Cannot register webhook for {sim_status.status} simulation"
        )

    # Initialize webhooks list if it doesn't exist
    if not hasattr(sim_status, 'webhooks'):
        sim_status.webhooks = []

    # Add webhook to the list
    sim_status.webhooks.append({
        "url": webhook_request.webhook_url,
        "secret": webhook_request.webhook_secret
    })

    # Update the simulation status
    update_simulation_status(simulation_id, {"webhooks": sim_status.webhooks})

    return {"message": "Webhook registered successfully", "simulation_id": simulation_id}


@app.get("/simulationResult/{simulation_id}/wait", tags=["Simulation"])
async def get_simulation_result_with_wait(
        simulation_id: str,
        timeout_seconds: int = Query(300, ge=1, le=1800, description="Maximum time to wait in seconds"),
        poll_interval: float = Query(2.0, ge=0.5, le=10.0, description="Polling interval in seconds"),
        auth: dict = Depends(get_auth)
):
    """
    Wait for simulation completion and return the result
    
    This endpoint will poll the simulation status and wait up to the specified timeout
    for the simulation to complete, then return the result immediately.
    """
    start_time = time.time()

    while (time.time() - start_time) < timeout_seconds:
        # Check if simulation exists
        if simulation_id not in simulation_status:
            # Try to load from disk
            status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
            if os.path.exists(status_path):
                with open(status_path, 'r') as f:
                    loaded_status = json.load(f)
                    simulation_status[simulation_id] = SimulationStatus(**loaded_status)
            else:
                raise HTTPException(
                    status_code=status.HTTP_404_NOT_FOUND,
                    detail=f"Simulation with ID {simulation_id} not found"
                )

        sim_status = simulation_status[simulation_id]

        # Check if simulation is completed
        if sim_status.status == "completed":
            # Return the result immediately
            if not sim_status.result_path or not os.path.exists(sim_status.result_path):
                raise HTTPException(
                    status_code=status.HTTP_404_NOT_FOUND,
                    detail="Simulation result file not found"
                )

            try:
                with open(sim_status.result_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                return {
                    "simulation_id": simulation_id,
                    "content": content,
                    "waited_seconds": round(time.time() - start_time, 2),
                    "status": "completed"
                }
            except Exception as e:
                logger.error(f"Error reading simulation result: {str(e)}")
                raise HTTPException(
                    status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                    detail=f"Failed to read simulation result: {str(e)}"
                )

        # Check if simulation failed
        elif sim_status.status == "failed":
            return {
                "simulation_id": simulation_id,
                "status": "failed",
                "error_message": sim_status.error_message,
                "waited_seconds": round(time.time() - start_time, 2)
            }

        # Wait before next poll
        await asyncio.sleep(poll_interval)

    # Timeout reached
    current_status = simulation_status.get(simulation_id)
    return {
        "simulation_id": simulation_id,
        "status": "timeout",
        "message": f"Simulation did not complete within {timeout_seconds} seconds",
        "current_simulation_status": current_status.status if current_status else "unknown",
        "current_progress": current_status.progress if current_status else 0.0,
        "waited_seconds": timeout_seconds
    }


@app.get("/simulationResult/{simulation_id}/stream", tags=["Simulation"])
async def stream_simulation_status(
        simulation_id: str,
        auth: dict = Depends(get_auth)
):
    """
    Stream simulation status updates using Server-Sent Events (SSE)
    
    This endpoint streams real-time status updates and returns the final result
    when the simulation completes.
    """
    import asyncio
    from fastapi.responses import StreamingResponse

    async def event_stream():
        last_status = None
        last_progress = None

        while True:
            try:
                # Check if simulation exists
                if simulation_id not in simulation_status:
                    # Try to load from disk
                    status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
                    if os.path.exists(status_path):
                        with open(status_path, 'r') as f:
                            loaded_status = json.load(f)
                            simulation_status[simulation_id] = SimulationStatus(**loaded_status)
                    else:
                        yield f"event: error\ndata: {{\"error\": \"Simulation not found\"}}\n\n"
                        break

                sim_status = simulation_status[simulation_id]

                # Send status update if changed
                if sim_status.status != last_status or sim_status.progress != last_progress:
                    status_data = {
                        "simulation_id": simulation_id,
                        "status": sim_status.status,
                        "progress": sim_status.progress,
                        "events_count": sim_status.events_count,
                        "timestamp": datetime.utcnow().isoformat()
                    }

                    if sim_status.error_message:
                        status_data["error_message"] = sim_status.error_message

                    yield f"event: status\ndata: {json.dumps(status_data)}\n\n"

                    last_status = sim_status.status
                    last_progress = sim_status.progress

                # If simulation is completed, send the final result
                if sim_status.status == "completed":
                    if sim_status.result_path and os.path.exists(sim_status.result_path):
                        try:
                            with open(sim_status.result_path, 'r', encoding='utf-8') as f:
                                content = f.read()

                            result_data = {
                                "simulation_id": simulation_id,
                                "status": "completed",
                                "content": content[:1000] + "..." if len(content) > 1000 else content,
                                # Truncate for SSE
                                "full_content_available": "/simulationResult/" + simulation_id,
                                "timestamp": datetime.utcnow().isoformat()
                            }
                            yield f"event: result\ndata: {json.dumps(result_data)}\n\n"

                        except Exception as e:
                            error_data = {
                                "simulation_id": simulation_id,
                                "error": f"Failed to read result: {str(e)}",
                                "timestamp": datetime.utcnow().isoformat()
                            }
                            yield f"event: error\ndata: {json.dumps(error_data)}\n\n"

                    # Send completion event and close stream
                    yield f"event: complete\ndata: {{\"simulation_id\": \"{simulation_id}\"}}\n\n"
                    break

                # If simulation failed, send error and close
                elif sim_status.status == "failed":
                    error_data = {
                        "simulation_id": simulation_id,
                        "status": "failed",
                        "error_message": sim_status.error_message,
                        "timestamp": datetime.utcnow().isoformat()
                    }
                    yield f"event: error\ndata: {json.dumps(error_data)}\n\n"
                    yield f"event: complete\ndata: {{\"simulation_id\": \"{simulation_id}\"}}\n\n"
                    break

                # Wait before next check
                await asyncio.sleep(1.0)

            except Exception as e:
                error_data = {
                    "error": f"Stream error: {str(e)}",
                    "timestamp": datetime.utcnow().isoformat()
                }
                yield f"event: error\ndata: {json.dumps(error_data)}\n\n"
                break

    return StreamingResponse(
        event_stream(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Headers": "Cache-Control"
        }
    )


if __name__ == "__main__":
    uvicorn.run("ModelApiEnhanced:app", host="0.0.0.0", port=8000, reload=True)

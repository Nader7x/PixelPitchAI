import asyncio
import json
import logging
import os
import time
import torch
import uuid
import uvicorn
from contextlib import asynccontextmanager
from datetime import datetime, timedelta
from fastapi import FastAPI, HTTPException, BackgroundTasks, Depends, status, Security
from fastapi.middleware.cors import CORSMiddleware
from fastapi.security import APIKeyHeader, OAuth2PasswordBearer, OAuth2PasswordRequestForm
from pydantic import BaseModel, Field
from transformers import GPT2LMHeadModel, GPT2Tokenizer
from typing import Optional, List

# Suppress the bcrypt warnings by setting up logging before imports
logging.getLogger("passlib.handlers.bcrypt").setLevel(logging.ERROR)

# Now import the authentication libraries
from jose import JWTError, jwt
from passlib.context import CryptContext

from Parser import parse_and_publish
from XgBoostClass import MatchStatProcessor
import xgboost as xgb
from xgboost import XGBRegressor

# --- Configuration ---
MODEL_PATH = os.getenv('MODEL_PATH', './gpt2-football-finetuned')
XGB_MODEL_PATH = os.getenv('XGB_MODEL_PATH', 'tuned_xgboost_model.json')
HEADERLINES_DIR = os.getenv('HEADERLINES_DIR', './HeaderLines')
INPUTTOKENS_DIR = os.getenv('INPUTTOKENS_DIR', './InputTokens')
SIMMATCHES_DIR = os.getenv('SIMMATCHES_DIR', './SimulatedMatches')
STATUS_DIR = os.getenv('STATUS_DIR', './SimulationStatus')  # Directory for match status tracking
SPECIAL_TOKENS = {
    "additional_special_tokens": ["[MATCH START]", "[EVENTS START]", "[SECOND HALF]", "[MATCH END]"]
}

# --- Authentication Configuration ---
SECRET_KEY = os.getenv("SECRET_KEY", "REDACTED_SECRET")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 30

# API Key authentication as an alternative to JWT
API_KEY_NAME = "X-API-Key"
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)
API_KEYS = {
    os.getenv("API_KEY", "REDACTED_TEST_KEY"): "admin"
}

# For OAuth2 with password flow
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="token", auto_error=False)
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

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

# --- Enhanced Logging Configuration ---
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('football_api.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


# Ensure required directories exist
def ensure_directories():
    """Ensure all required directories exist"""
    directories = [HEADERLINES_DIR, INPUTTOKENS_DIR, SIMMATCHES_DIR, STATUS_DIR]
    for directory in directories:
        os.makedirs(directory, exist_ok=True)
        logger.info(f"Ensured directory exists: {directory}")


# Application lifecycle management
@asynccontextmanager
async def lifespan(app: FastAPI):
    """Manage application lifecycle"""
    # Startup
    logger.info("Starting Football Match Simulation API...")
    ensure_directories()
    logger.info("All directories ready")
    yield
    # Shutdown
    logger.info("Shutting down Football Match Simulation API...")


# --- FastAPI App ---
app = FastAPI(
    title="Football Match Simulation API",
    description="Advanced API for generating football match simulations using a fine-tuned GPT-2 model with XGBoost features",
    version="2.0.0",
    lifespan=lifespan,
    docs_url="/docs",
    redoc_url="/redoc"
)

# CORS (allow all origins for now, restrict in production)
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


# --- Dependency Injection ---
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


# --- Enhanced Pydantic Models ---
class Token(BaseModel):
    """JWT Token response model"""
    access_token: str
    token_type: str


class TokenData(BaseModel):
    """Token data for validation"""
    username: str = None


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
        schema_extra = {
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

    class Config:
        schema_extra = {
            "example": {
                "match_id": 12345,
                "home_team_name": "Real Madrid",
                "away_team_name": "Barcelona",
                "home_team_season": "2020/2021",
                "away_team_season": "2020/2021",
                "events_count": 0,
                "execution_time": 0.123,
                "preview": "",
                "simulation_id": "550e8400-e29b-41d4-a716-446655440000",
                "status": "pending"
            }
        }


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

    class Config:
        schema_extra = {
            "example": {
                "simulation_id": "550e8400-e29b-41d4-a716-446655440000",
                "match_id": 12345,
                "status": "completed",
                "progress": 100.0,
                "start_time": 1640995200.0,
                "end_time": 1640995800.0,
                "error_message": None,
                "events_count": 45,
                "result_path": "./SimulatedMatches/match_12345_550e8400.txt",
                "metadata": {
                    "home_team": "Real Madrid",
                    "away_team": "Barcelona",
                    "home_team_season": "2020/2021",
                    "away_team_season": "2020/2021",
                    "requester": "admin"
                }
            }
        }


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


class SimulationListResponse(BaseModel):
    """Response model for listing simulations"""
    simulations: List[SimulationStatus]
    total_count: int
    page: int
    page_size: int


# --- Authentication and Security ---
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
        return api_key_header
    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid API Key",
    )


# Allow either API key or JWT authentication
async def get_auth(api_key: str = Depends(get_api_key), token: str = Depends(oauth2_scheme)):
    """Support both API key and JWT authentication methods"""
    if api_key:
        return {"auth_type": "api_key", "username": API_KEYS.get(api_key)}
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


# --- Enhanced Endpoints ---

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


@app.get("/simulations", response_model=SimulationListResponse, tags=["Simulation"])
async def list_simulations(
        page: int = 1,
        page_size: int = 10,
        status_filter: Optional[str] = None,
        auth: dict = Depends(get_auth)
):
    """List all simulations with pagination and filtering"""
    try:
        # Get all simulations from memory and disk
        all_simulations = list(simulation_status.values())

        # Also load from disk if not in memory
        if os.path.exists(STATUS_DIR):
            for filename in os.listdir(STATUS_DIR):
                if filename.endswith('.json'):
                    sim_id = filename[:-5]  # Remove .json extension
                    if sim_id not in simulation_status:
                        try:
                            with open(os.path.join(STATUS_DIR, filename), 'r') as f:
                                loaded_status = json.load(f)
                                all_simulations.append(SimulationStatus(**loaded_status))
                        except Exception as e:
                            logger.warning(f"Failed to load simulation {sim_id}: {str(e)}")

        # Filter by status if provided
        if status_filter:
            all_simulations = [s for s in all_simulations if s.status == status_filter]

        # Sort by start time (newest first)
        all_simulations.sort(key=lambda x: x.start_time, reverse=True)

        # Paginate
        total_count = len(all_simulations)
        start_idx = (page - 1) * page_size
        end_idx = start_idx + page_size
        paginated_simulations = all_simulations[start_idx:end_idx]

        return SimulationListResponse(
            simulations=paginated_simulations,
            total_count=total_count,
            page=page,
            page_size=page_size
        )

    except Exception as e:
        logger.error(f"Error listing simulations: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to list simulations: {str(e)}")


@app.delete("/simulations/{simulation_id}", tags=["Simulation"])
async def delete_simulation(
        simulation_id: str,
        auth: dict = Depends(get_auth)
):
    """Delete a simulation and its associated files"""
    try:
        # Check if simulation exists
        if simulation_id not in simulation_status:
            # Try to load from disk
            status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
            if not os.path.exists(status_path):
                raise HTTPException(
                    status_code=404,
                    detail=f"Simulation {simulation_id} not found"
                )

        # Remove from memory
        if simulation_id in simulation_status:
            sim_status = simulation_status[simulation_id]
            # Delete result file if it exists
            if sim_status.result_path and os.path.exists(sim_status.result_path):
                os.remove(sim_status.result_path)
                logger.info(f"Deleted result file: {sim_status.result_path}")

            del simulation_status[simulation_id]

        # Remove status file
        status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
        if os.path.exists(status_path):
            os.remove(status_path)
            logger.info(f"Deleted status file: {status_path}")

        return {"message": f"Simulation {simulation_id} deleted successfully"}

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Error deleting simulation: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to delete simulation: {str(e)}")


@app.get("/stats", tags=["Statistics"])
async def get_api_statistics(auth: dict = Depends(get_auth)):
    """Get API usage statistics"""
    try:
        # Count simulations by status
        status_counts = {"pending": 0, "processing": 0, "completed": 0, "failed": 0}
        total_events = 0
        total_execution_time = 0.0
        completed_count = 0

        # Count from memory
        for sim in simulation_status.values():
            status_counts[sim.status] = status_counts.get(sim.status, 0) + 1
            if sim.events_count:
                total_events += sim.events_count
            if sim.status == "completed" and sim.start_time and sim.end_time:
                total_execution_time += (sim.end_time - sim.start_time)
                completed_count += 1

        # Count from disk
        if os.path.exists(STATUS_DIR):
            for filename in os.listdir(STATUS_DIR):
                if filename.endswith('.json'):
                    sim_id = filename[:-5]
                    if sim_id not in simulation_status:
                        try:
                            with open(os.path.join(STATUS_DIR, filename), 'r') as f:
                                loaded_status = json.load(f)
                                status = loaded_status.get('status', 'unknown')
                                status_counts[status] = status_counts.get(status, 0) + 1
                                if loaded_status.get('events_count'):
                                    total_events += loaded_status['events_count']
                                if (status == "completed" and
                                        loaded_status.get('start_time') and
                                        loaded_status.get('end_time')):
                                    total_execution_time += (loaded_status['end_time'] - loaded_status['start_time'])
                                    completed_count += 1
                        except Exception:
                            pass

        avg_execution_time = total_execution_time / completed_count if completed_count > 0 else 0.0
        avg_events_per_match = total_events / completed_count if completed_count > 0 else 0.0

        return {
            "status_counts": status_counts,
            "total_simulations": sum(status_counts.values()),
            "total_events_generated": total_events,
            "average_execution_time_seconds": round(avg_execution_time, 2),
            "average_events_per_match": round(avg_events_per_match, 1),
            "api_version": "2.0.0",
            "timestamp": datetime.utcnow().isoformat()
        }

    except Exception as e:
        logger.error(f"Error getting statistics: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Failed to get statistics: {str(e)}")


@app.post("/startMatch", response_model=MatchResponse, tags=["Simulation"])
async def start_match(
        request: MatchRequest,
        background_tasks: BackgroundTasks,
        resources: ModelResources = Depends(get_resources),
        auth: dict = Depends(get_auth)
):
    """
    Start a match simulation asynchronously

    This endpoint immediately returns with a simulation ID, and the processing happens in the background.
    Use the /simulationStatus/{simulation_id} endpoint to check progress and get results.
    """
    start_time = time.time()
    simulation_id = str(uuid.uuid4())

    try:
        # Create initial status entry
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

        # Start the background task for simulation
        background_tasks.add_task(
            process_match_simulation,
            simulation_id,
            request,
            resources
        )

        # Return immediate response
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
async def get_simulation_status(
        simulation_id: str,
        auth: dict = Depends(get_auth)
):
    """
    Get the status of a simulation by its ID

    Returns information about the progress, status, and results when completed
    """
    if simulation_id not in simulation_status:
        # Try to load from disk if not in memory
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
async def get_simulation_result(
        simulation_id: str,
        auth: dict = Depends(get_auth)
):
    """
    Get the raw text result of a completed simulation

    Returns the full generated match text if the simulation is completed
    """
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


# Mock users database - replace with actual DB in production
fake_users_db = {
    "admin": {
        "username": "admin",
        "full_name": "Administrator",
        "email": "admin@example.com",
        "hashed_password": pwd_context.hash("adminpassword"),
        "disabled": False,
    }
}

# In-memory simulation status tracking - replace with DB in production
simulation_status = {}


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


# --- Simulation Processing Functions ---
def update_simulation_status(sim_id, status_update):
    """Update the status of a simulation in memory and on disk"""
    try:
        if sim_id in simulation_status:
            # Update existing status
            current_status = simulation_status[sim_id].dict()
            current_status.update(status_update)
            simulation_status[sim_id] = SimulationStatus(**current_status)
        else:
            # This shouldn't happen, but handle gracefully
            logger.warning(f"Simulation {sim_id} not found in memory, creating new entry")
            simulation_status[sim_id] = SimulationStatus(simulation_id=sim_id, **status_update)

        # Also save to disk for persistence
        os.makedirs(STATUS_DIR, exist_ok=True)
        status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")
        with open(status_path, 'w') as f:
            json.dump(simulation_status[sim_id].dict(), f, indent=2)

        return simulation_status[sim_id]
    except Exception as e:
        logger.error(f"Error updating simulation status: {str(e)}")
        return None


async def process_match_simulation(
        simulation_id: str,
        request: MatchRequest,
        model_resources: ModelResources,
):
    """Process a match simulation asynchronously"""
    start_time = time.time()

    try:
        # Update status to processing
        update_simulation_status(simulation_id, {"status": "processing", "progress": 5.0})

        # Prepare the input data
        home_team_season = request.home_team_season.split("/")[-1]
        away_team_season = request.away_team_season.split("/")[-1]
        home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
        away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"

        # Generate features and update progress
        input_tokens_path = generate_features(
            request.home_team_id, request.away_team_id, home_team_season, away_team_season,
            home_team_name, away_team_name, model_resources.match_stat
        )
        update_simulation_status(simulation_id, {"progress": 20.0})

        # Generate the match text
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

        # Save the generated match to a file
        simulated_match_path = os.path.join(
            SIMMATCHES_DIR,
            f"match_{request.match_id}_{simulation_id}.txt"
        )
        with open(simulated_match_path, "w", encoding="utf-8") as f:
            f.write(generated_text)

        logger.info(f"Generated match events saved to {simulated_match_path}")

        # Parse and publish events
        events = parse_and_publish(simulated_match_path)
        logger.info(f"Successfully parsed and published {len(events)} events")

        # Update final status
        update_simulation_status(simulation_id, {
            "status": "completed",
            "progress": 100.0,
            "end_time": time.time(),
            "events_count": len(events),
            "result_path": simulated_match_path
        })

    except Exception as e:
        logger.error(f"Error in match simulation: {str(e)}")
        update_simulation_status(simulation_id, {
            "status": "failed",
            "end_time": time.time(),
            "error_message": str(e)
        })
        raise


if __name__ == "__main__":
    uvicorn.run("ModelApi:app", host="0.0.0.0", port=8000, reload=True)

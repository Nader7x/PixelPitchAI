"""
Pydantic Models for the Football Match Simulation API
All request/response models and data validation schemas
"""

from datetime import datetime
from pydantic import BaseModel, Field
from typing import Optional, List


# --- Authentication Models ---
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


# --- API Key Models ---
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


# --- Match Simulation Models ---
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
                "home_team_id": 7,
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
    webhooks: List[dict] = []


# --- Enhanced Simulation Models ---
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


# --- System Models ---
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

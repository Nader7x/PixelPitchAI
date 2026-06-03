"""
API Configuration Settings
All configuration variables and constants for the Football Match Simulation API
"""

import os

# --- Model Paths and Directories ---
MODEL_PATH = os.getenv('MODEL_PATH', './gpt2-football-finetuned')
ONNX_MODEL_PATH = os.getenv('ONNX_MODEL_PATH', './gpt2-football-finetuned-onnx-int8')
XGB_MODEL_PATH = os.getenv('XGB_MODEL_PATH', 'tuned_xgboost_model.json')
HEADERLINES_DIR = os.getenv('HEADERLINES_DIR', './HeaderLines')
INPUTTOKENS_DIR = os.getenv('INPUTTOKENS_DIR', './InputTokens')
SIMMATCHES_DIR = os.getenv('SIMMATCHES_DIR', './SimulatedMatches')
MATCHES_DIR = os.getenv('MATCHES_DIR', './Matches')
PARSED_DIR = os.getenv('PARSED_DIR', './Matches_Parsed')
STATUS_DIR = os.getenv('STATUS_DIR', './SimulationStatus')

# --- Team and Season Data ---
SPECIAL_LIST = ["Deportivo_Alavés_2016", "Deportivo_Alavés_2017", "Deportivo_Alavés_2018", "Deportivo_Alavés_2019",
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

# --- Special Tokens Configuration ---
SPECIAL_TOKENS = {
    "additional_special_tokens": SPECIAL_LIST
}

# --- Authentication Configuration ---
SECRET_KEY = os.getenv("SECRET_KEY")
ALGORITHM = "HS256"
ACCESS_TOKEN_EXPIRE_MINUTES = 920
API_KEY_NAME = "X-API-Key"

# --- Default API Keys Configuration ---
DEFAULT_API_KEY = os.getenv("API_KEY")
PIXEL_PITCH_API_KEY = os.getenv("PIXEL_PITCH_API_KEY")

# --- Logging Configuration ---
LOGGING_CONFIG = {
    "level": "INFO",
    "format": '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    "file": './logs/football_api.log',
    "directory": './logs'
}

# --- API Information ---
API_INFO = {
    "title": "Football Match Simulation API",
    "description": "Advanced API for generating football match simulations using fine-tuned GPT-2 and XGBoost models",
    "version": "1.0.0",
    "docs_url": "/docs",
    "redoc_url": "/redoc"
}

# --- CORS Configuration ---
CORS_CONFIG = {
    "allow_origins": ["*"],
    "allow_credentials": True,
    "allow_methods": ["*"],
    "allow_headers": ["*"]
}

# --- Default Generation Parameters ---
DEFAULT_GENERATION_PARAMS = {
    "num_tokens_to_generate": 200000,
    "temperature": 0.7,
    "top_p": 0.9,
    "top_k": 50,
    "max_length": 1024
}

# --- Enhanced Features ---
WEBHOOK_TIMEOUT = 30  # seconds
STREAMING_POLL_INTERVAL = 1.0  # seconds
MAX_WAIT_TIMEOUT = 1800  # seconds (30 minutes)
DEFAULT_WAIT_TIMEOUT = 300  # seconds (5 minutes)


def API_TITLE():
    return API_INFO["title"]


def API_DESCRIPTION():
    return API_INFO["description"]


def API_VERSION():
    return API_INFO["version"]


def CORS_ORIGINS():
    return CORS_CONFIG


def CORS_CREDENTIALS():
    return CORS_CONFIG["allow_credentials"]


def CORS_METHODS():
    return CORS_CONFIG["allow_methods"]


def CORS_HEADERS():
    return CORS_CONFIG["allow_headers"]


def LOG_LEVEL():
    return LOGGING_CONFIG["level"]


def LOG_FORMAT():
    return LOGGING_CONFIG["format"]


def FEATURES_DIR():
    return HEADERLINES_DIR


def TOKENS_PATH():
    return INPUTTOKENS_DIR


def LOG_FILE():
    return LOGGING_CONFIG["file"]


def LOG_DIR():
    return LOGGING_CONFIG["directory"]

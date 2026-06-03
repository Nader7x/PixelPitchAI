"""
Authentication and Authorization Module
Handles JWT tokens, API keys, and user authentication
"""

import secrets
import uuid
from datetime import datetime, timedelta
from fastapi import HTTPException, Depends, Security, status
from fastapi.security import APIKeyHeader, OAuth2PasswordBearer
import bcrypt
from jose import JWTError, jwt
from typing import Optional, Dict, List

from ..config import PIXEL_PITCH_API_KEY
from ..config.settings import SECRET_KEY, ALGORITHM, ACCESS_TOKEN_EXPIRE_MINUTES, API_KEY_NAME, DEFAULT_API_KEY
from ..models.schemas import User, UserInDB, TokenData, APIKeyInfo
from ..utils.logging import get_logger

logger = get_logger(__name__)
# --- Authentication Setup ---
oauth2_scheme = OAuth2PasswordBearer(tokenUrl="/auth/login", auto_error=False)
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

# --- Password Hashing & Verification ---
def hash_password(password: str) -> str:
    """
    Hash a plain text password using native bcrypt.
    """
    password_bytes = password.encode('utf-8')
    salt = bcrypt.gensalt()
    hashed_bytes = bcrypt.hashpw(password_bytes, salt)
    return hashed_bytes.decode('utf-8')


def verify_password(plain_password: str, hashed_password: str) -> bool:
    """
    Verify a plain text password against a hashed bcrypt password string.
    """
    try:
        return bcrypt.checkpw(
            plain_password.encode('utf-8'),
            hashed_password.encode('utf-8')
        )
    except Exception:
        return False


def get_password_hash(password: str) -> str:
    """
    Generate password hash (alias for hash_password).
    """
    return hash_password(password)

# --- In-Memory Storage (Replace with database in production) ---
fake_users_db = {
    "admin": {
        "username": "admin",
        "full_name": "Administrator",
        "email": "admin@example.com",
        "hashed_password": hash_password("adminpassword"),
        "disabled": False,
    }
}

# Enhanced API Keys storage with metadata
API_KEYS: Dict[str, dict] = {
    DEFAULT_API_KEY: {
        "username": "admin",
        "key_id": "default_key",
        "description": "Default test API key",
        "created_at": datetime.utcnow().isoformat(),
        "expires_at": None,
        "last_used": None,
        "is_active": True
    },
    PIXEL_PITCH_API_KEY: {
        "username": "admin",
        "key_id": "280a422a-9504-4981-a78a-1a0bd507dd16",
        "description": "Admin key for Pixel Pitch API",
        "created_at": "2025-06-06T14:33:47.644111",
        "expires_at": "2026-06-01T14:33:47.644111",
        "last_used": None,
        "is_active": True
    }
}

# In-memory storage for user API keys
user_api_keys: Dict[str, List[str]] = {
    "admin": [DEFAULT_API_KEY, PIXEL_PITCH_API_KEY]
}

# --- User Functions ---
def get_user(db: dict, username: str) -> Optional[UserInDB]:
    """Get user from database"""
    if username in db:
        user_dict = db[username]
        return UserInDB(**user_dict)
    return None


def authenticate_user(fake_db: dict, username: str, password: str) -> Optional[UserInDB]:
    """Authenticate user with username and password"""
    user = get_user(fake_db, username)
    if not user:
        return False
    if not verify_password(password, user.hashed_password):
        return False
    return user


# --- JWT Token Functions ---
def create_access_token(data: dict, expires_delta: Optional[timedelta] = None) -> str:
    """Create JWT access token"""
    to_encode = data.copy()
    if expires_delta:
        expire = datetime.utcnow() + expires_delta
    else:
        expire = datetime.utcnow() + timedelta(minutes=15)
    to_encode.update({"exp": expire})
    encoded_jwt = jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)
    return encoded_jwt


# --- API Key Functions ---
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
                if api_key in API_KEYS:
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


# --- Dependency Functions ---
async def get_current_user(token: str = Depends(oauth2_scheme)) -> UserInDB:
    """Get current user from JWT token"""
    credentials_exception = HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Could not validate credentials",
        headers={"WWW-Authenticate": "Bearer"},
    )

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


async def get_current_active_user(current_user: User = Depends(get_current_user)) -> User:
    """Get current active user"""
    if current_user.disabled:
        raise HTTPException(status_code=400, detail="Inactive user")
    return current_user


async def get_api_key(api_key_header: str = Security(api_key_header)) -> str:
    """Validate API key"""
    if api_key_header in API_KEYS:
        logger.info(api_key_header)
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


async def get_api_key_optional(api_key_header: str = Security(api_key_header)) -> Optional[str]:
    """Get API key if present, return None if not present"""
    if not api_key_header:
        return None

    if api_key_header in API_KEYS:
        key_info = API_KEYS[api_key_header]
        if not key_info["is_active"]:
            raise HTTPException(
                status_code=status.HTTP_401_UNAUTHORIZED,
                detail="API Key has been revoked",
            )

        if key_info["expires_at"]:
            expires_at = datetime.fromisoformat(key_info["expires_at"])
            if datetime.utcnow() > expires_at:
                raise HTTPException(
                    status_code=status.HTTP_401_UNAUTHORIZED,
                    detail="API Key has expired",
                )

        update_api_key_last_used(api_key_header)
        return api_key_header

    raise HTTPException(
        status_code=status.HTTP_401_UNAUTHORIZED,
        detail="Invalid API Key",
    )


async def get_current_user_flexible(
        token: str = Depends(oauth2_scheme),
        api_key: str = Depends(get_api_key_optional)
) -> UserInDB:
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
            username = payload.get("sub")
            if username:
                user = get_user(fake_users_db, username=username)
                if user:
                    return user
        except JWTError:
            pass

    raise credentials_exception


async def get_current_active_user_flexible(current_user: User = Depends(get_current_user_flexible)) -> User:
    """Get current active user (flexible auth)"""
    if current_user.disabled:
        raise HTTPException(status_code=400, detail="Inactive user")
    return current_user


async def get_auth(api_key: str = Depends(get_api_key), token: str = Depends(oauth2_scheme)) -> dict:
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

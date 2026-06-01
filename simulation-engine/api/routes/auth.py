"""
Authentication Routes

This module contains all authentication-related API endpoints including
login, API key management, and user authentication.
"""

from datetime import timedelta
from fastapi import APIRouter, Depends, HTTPException, status
from fastapi.security import OAuth2PasswordRequestForm
from typing import List

from ..auth.security import (
    authenticate_user, create_access_token, get_current_active_user,
    create_api_key_for_user, revoke_api_key, get_user_api_keys
)
from ..auth.security import fake_users_db
from ..config.settings import ACCESS_TOKEN_EXPIRE_MINUTES
from ..models.schemas import (
    Token, User, APIKeyRequest, APIKeyResponse, APIKeyInfo
)

router = APIRouter(prefix="/auth", tags=["Authentication"])


@router.post("/login", response_model=Token)
async def login_for_access_token(form_data: OAuth2PasswordRequestForm = Depends()):
    """
    OAuth2 compatible token login, get an access token for future requests
    
    Args:
        form_data: OAuth2 form data containing username and password
        
    Returns:
        Token: Access token and token type
        
    Raises:
        HTTPException: If credentials are invalid
    """
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


@router.get("/me", response_model=User)
async def read_users_me(current_user: User = Depends(get_current_active_user)):
    """
    Get current user information
    
    Args:
        current_user: Current authenticated user
        
    Returns:
        User: Current user information
    """
    return current_user


@router.post("/api-keys", response_model=APIKeyResponse)
async def create_api_key(
        request: APIKeyRequest,
        current_user: User = Depends(get_current_active_user)
):
    """
    Create a new API key for the current user
    
    Args:
        request: API key creation request
        current_user: Current authenticated user
        
    Returns:
        APIKeyResponse: Created API key information
    """
    api_key_data = create_api_key_for_user(
        username=current_user.username,
        description=request.description,
        expires_in_days=request.expires_in_days
    )

    return APIKeyResponse(
        api_key=api_key_data["api_key"],
        description=api_key_data["description"],
        expires_at=api_key_data["expires_at"],
        created_at=api_key_data["created_at"],
        key_id=api_key_data["key_id"]
    )


@router.get("/api-keys", response_model=List[APIKeyInfo])
async def list_api_keys(current_user: User = Depends(get_current_active_user)):
    """
    List all API keys for the current user
    
    Args:
        current_user: Current authenticated user
        
    Returns:
        List[APIKeyInfo]: List of user's API keys (without the actual key values)
    """
    # The get_user_api_keys function already returns APIKeyInfo objects
    return get_user_api_keys(current_user.username)


@router.delete("/api-keys/{api_key}")
async def delete_api_key(
        api_key: str,
        current_user: User = Depends(get_current_active_user)
):
    """
    Revoke/delete an API key
    
    Args:
        api_key: API key to revoke
        current_user: Current authenticated user
        
    Returns:
        dict: Success message
        
    Raises:
        HTTPException: If API key not found or doesn't belong to user
    """
    success = revoke_api_key(api_key, current_user.username)

    if not success:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="API key not found or doesn't belong to you"
        )

    return {"message": "API key revoked successfully"}

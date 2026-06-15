"""
Football Match Simulation API v2.0.0

Main application module that assembles all components into a complete FastAPI application.
This is the refactored, modular version of the original ModelApiEnhanced.py.

Architecture:
- Modular design with separation of concerns
- Centralized configuration management
- Dependency injection for services
- Comprehensive error handling
- Production-ready logging and monitoring

Features:
- Asynchronous match simulation processing
- JWT and API Key authentication with API key management
- Real-time simulation status tracking with webhooks
- Enhanced error handling and comprehensive logging
- Statistics and monitoring endpoints
- CORS support for web applications
"""

import time
import uvicorn
from contextlib import asynccontextmanager
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.openapi.docs import get_swagger_ui_html
from fastapi.openapi.utils import get_openapi

# Internal imports
from api.config.settings import (
    API_TITLE, API_DESCRIPTION, API_VERSION,
    CORS_ORIGINS, CORS_CREDENTIALS, CORS_METHODS, CORS_HEADERS,
    API_KEY_NAME, LOG_FILE
)
from .routes import auth, simulation, system
from .services.optimized_simulation_service import get_optimized_simulation_service as get_simulation_service
from .utils.directories import ensure_directories, cleanup_old_files
from .utils.logging import setup_logging, get_logger

# Setup logging with file output
logger = setup_logging(log_file=LOG_FILE())
app_logger = get_logger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Manage application lifecycle
    
    Handles startup and shutdown events for the FastAPI application.
    - Startup: Initialize directories, load models, setup services
    - Shutdown: Cleanup resources, save state
    """
    # Startup
    app_logger.info("Starting Football Match Simulation API v2.0.0")

    try:
        # Ensure required directories exist
        ensure_directories()
        app_logger.info("✓ Directory structure initialized")

        # Initialize simulation service and load models
        simulation_service = get_simulation_service()
        await simulation_service.initialize()
        app_logger.info("✓ Models and services initialized")

        # Cleanup old files (optional)
        try:
            cleanup_old_files("./simulated_matches", max_age_hours=48)
            cleanup_old_files("./status", max_age_hours=72)
        except Exception as e:
            app_logger.warning(f"Cleanup warning: {str(e)}")

        app_logger.info("🚀 API startup completed successfully")

        yield

    except Exception as e:
        app_logger.error(f"Startup failed: {str(e)}")
        raise

    # Shutdown
    app_logger.info("Shutting down Football Match Simulation API")

    try:
        # Cleanup resources
        simulation_service = get_simulation_service()
        await simulation_service.cleanup()
        app_logger.info("✓ Services cleanup completed")

        # Add any additional shutdown handling here if needed
        # (Previously in the on_event("shutdown") handler)

    except Exception as e:
        app_logger.error(f"Shutdown error: {str(e)}")

    app_logger.info("👋 API shutdown completed")


# Create FastAPI application
app = FastAPI(
    title=API_TITLE(),
    description=API_DESCRIPTION(),
    version=API_VERSION(),
    lifespan=lifespan,
    docs_url="/docs",  # We'll create a custom docs endpoint
    redoc_url="/redoc"
)

# Configure CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=CORS_ORIGINS(),
    allow_credentials=CORS_CREDENTIALS(),
    allow_methods=CORS_METHODS(),
    allow_headers=CORS_HEADERS(),
)


# Add middleware for request logging
@app.middleware("http")
async def log_requests(request, call_next):
    """Log HTTP requests and responses"""
    from .utils.logging import log_request

    start_time = time.time()
    response = await call_next(request)
    process_time = time.time() - start_time

    log_request(
        app_logger,
        request.method,
        str(request.url.path),
        response.status_code,
        process_time
    )

    # Add timing header
    response.headers["X-Process-Time"] = str(process_time)

    return response


# Include routers
app.include_router(system.router)
app.include_router(auth.router)
app.include_router(simulation.router)


# Global exception handler
@app.exception_handler(Exception)
async def global_exception_handler(request, exc):
    """Handle unexpected exceptions"""
    from fastapi import HTTPException
    from fastapi.responses import JSONResponse

    app_logger.error(f"Unhandled exception: {str(exc)}", exc_info=True)

    if isinstance(exc, HTTPException):
        return JSONResponse(
            status_code=exc.status_code,
            content={"detail": exc.detail}
        )

    return JSONResponse(
        status_code=500,
        content={
            "detail": "Internal server error",
            "error_id": f"err_{int(time.time())}"
        }
    )


if __name__ == "__main__":
    # Development server configuration
    uvicorn.run(
        "api.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        log_level="info",
        access_log=True
    )

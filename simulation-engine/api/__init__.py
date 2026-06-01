"""
Football Match Simulation API Package

A modular, production-ready FastAPI application for generating football match
simulations using fine-tuned GPT-2 and XGBoost models.

This package provides:
- Asynchronous match simulation processing
- Dual authentication (JWT tokens and API keys)
- Real-time status tracking and webhooks
- Comprehensive logging and monitoring
- RESTful API with OpenAPI documentation
"""

__version__ = "2.0.0"
__title__ = "Football Match Simulation API"
__description__ = "Advanced API for generating football match simulations"
__author__ = "Football Simulation Team"

from .main import app

__all__ = ["app"]

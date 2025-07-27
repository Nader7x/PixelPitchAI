"""
Logging Utilities

This module provides centralized logging configuration and utilities
for the Football Match Simulation API.
"""

import sys
from pathlib import Path
from typing import Optional

import logging
from ..config.settings import LOG_LEVEL, LOG_FORMAT


def setup_logging(
        log_level: str = LOG_LEVEL(),
        log_format: str = LOG_FORMAT(),
        log_file: Optional[str] = None,
        logger_name: Optional[str] = None
) -> logging.Logger:
    """
    Setup logging configuration for the application
    
    Args:
        log_level: Logging level (DEBUG, INFO, WARNING, ERROR, CRITICAL)
        log_format: Log message format string
        log_file: Optional log file path
        logger_name: Optional logger name, defaults to __name__
        
    Returns:
        logging.Logger: Configured logger instance
    """
    # Convert string level to logging constant
    numeric_level = getattr(logging, log_level.upper(), logging.INFO)

    # Create logger
    logger_name = logger_name or __name__
    logger = logging.getLogger(logger_name)
    logger.setLevel(numeric_level)

    # Clear existing handlers to avoid duplicates
    logger.handlers.clear()

    # Create formatter
    formatter = logging.Formatter(log_format)

    # Console handler
    console_handler = logging.StreamHandler(sys.stdout)
    console_handler.setLevel(numeric_level)
    console_handler.setFormatter(formatter)
    logger.addHandler(console_handler)

    # File handler (optional)
    if log_file:
        # Ensure log directory exists
        log_path = Path(log_file)
        log_path.parent.mkdir(parents=True, exist_ok=True)

        file_handler = logging.FileHandler(log_file, encoding='utf-8')
        file_handler.setLevel(numeric_level)
        file_handler.setFormatter(formatter)
        logger.addHandler(file_handler)

    # Configure root logger as well to ensure all loggers inherit file handler
    root_logger = logging.getLogger()
    root_logger.setLevel(numeric_level)

    # Add file handler to root logger if not already present and log_file is specified
    if log_file and not any(isinstance(h, logging.FileHandler) for h in root_logger.handlers):
        log_path = Path(log_file)
        log_path.parent.mkdir(parents=True, exist_ok=True)

        root_file_handler = logging.FileHandler(log_file, encoding='utf-8')
        root_file_handler.setLevel(numeric_level)
        root_file_handler.setFormatter(formatter)
        root_logger.addHandler(root_file_handler)

    # Suppress noisy third-party loggers
    logging.getLogger("passlib.handlers.bcrypt").setLevel(logging.ERROR)
    logging.getLogger("transformers.tokenization_utils_base").setLevel(logging.WARNING)
    logging.getLogger("transformers.generation_utils").setLevel(logging.WARNING)

    return logger


def get_logger(name: str) -> logging.Logger:
    """
    Get a logger instance with the application's configuration
    
    Args:
        name: Logger name (usually __name__)
        
    Returns:
        logging.Logger: Configured logger instance
    """
    return logging.getLogger(name)


def log_request(logger: logging.Logger, method: str, path: str, status_code: int, duration: float):
    """
    Log an HTTP request
    
    Args:
        logger: Logger instance
        method: HTTP method
        path: Request path
        status_code: HTTP status code
        duration: Request duration in seconds
    """
    level = logging.INFO
    if status_code >= 400:
        level = logging.WARNING
    if status_code >= 500:
        level = logging.ERROR

    logger.log(
        level,
        f"{method} {path} - {status_code} - {duration:.3f}s"
    )


def log_simulation_event(
        logger: logging.Logger,
        simulation_id: str,
        event: str,
        details: Optional[dict] = None
):
    """
    Log a simulation-related event
    
    Args:
        logger: Logger instance
        simulation_id: Unique simulation identifier
        event: Event description
        details: Optional additional details
    """
    message = f"[{simulation_id[:8]}] {event}"
    if details:
        message += f" - {details}"

    logger.info(message)


def log_webhook_event(
        logger: logging.Logger,
        webhook_url: str,
        simulation_id: str,
        success: bool,
        details: Optional[str] = None
):
    """
    Log a webhook notification event
    
    Args:
        logger: Logger instance
        webhook_url: Webhook URL
        simulation_id: Simulation ID
        success: Whether the webhook was successful
        details: Optional additional details
    """
    status = "SUCCESS" if success else "FAILED"
    message = f"Webhook {status} [{simulation_id[:8]}] -> {webhook_url}"

    if details:
        message += f" - {details}"

    level = logging.INFO if success else logging.WARNING
    logger.log(level, message)


class ContextLogger:
    """
    Logger with context information for structured logging
    """

    def __init__(self, logger: logging.Logger, context: dict):
        self.logger = logger
        self.context = context

    def _format_message(self, message: str) -> str:
        """Format message with context"""
        context_str = " ".join(f"{k}={v}" for k, v in self.context.items())
        return f"[{context_str}] {message}"

    def debug(self, message: str, **kwargs):
        self.logger.debug(self._format_message(message), **kwargs)

    def info(self, message: str, **kwargs):
        self.logger.info(self._format_message(message), **kwargs)

    def warning(self, message: str, **kwargs):
        self.logger.warning(self._format_message(message), **kwargs)

    def error(self, message: str, **kwargs):
        self.logger.error(self._format_message(message), **kwargs)

    def critical(self, message: str, **kwargs):
        self.logger.critical(self._format_message(message), **kwargs)


def get_context_logger(name: str, **context) -> ContextLogger:
    """
    Get a context logger with additional context information
    
    Args:
        name: Logger name
        **context: Context key-value pairs
        
    Returns:
        ContextLogger: Logger with context
    """
    logger = get_logger(name)
    return ContextLogger(logger, context)


# Default application logger
from ..config.settings import LOG_FILE

app_logger = setup_logging(logger_name="football_api", log_file=LOG_FILE())

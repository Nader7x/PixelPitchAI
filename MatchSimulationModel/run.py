"""
Entry point for the Football Match Simulation API

This script provides a clean entry point for running the API,
avoiding RuntimeWarnings related to module imports.
"""

import uvicorn

if __name__ == "__main__":
    # Development server configuration
    uvicorn.run(
        "api.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
        reload_dirs=["api", "tests"],
        log_level="info",
        access_log=True
    )

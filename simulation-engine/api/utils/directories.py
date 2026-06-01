"""
Directory Management Utilities

This module provides utilities for managing directories and file operations
required by the Football Match Simulation API.
"""

import json
import os
import shutil
from pathlib import Path
from typing import Optional, List, Dict, Any

from ..config import LOG_DIR
from ..config.settings import (
    SIMMATCHES_DIR, STATUS_DIR, FEATURES_DIR, MODEL_PATH,
    TOKENS_PATH, LOG_FILE
)
from ..utils.logging import get_logger

logger = get_logger(__name__)


def ensure_directories() -> None:
    """
    Ensure all required directories exist
    
    Creates the necessary directory structure for the application:
    - Simulated matches output directory
    - Simulation status tracking directory  
    - Generated features directory
    - Log files directory
    """
    directories = [
        SIMMATCHES_DIR,
        STATUS_DIR,
        FEATURES_DIR(),  # Call the function to get the string value
        LOG_DIR()
    ]

    for directory in directories:
        try:
            os.makedirs(directory, exist_ok=True)
            logger.debug(f"Ensured directory exists: {directory}")
        except Exception as e:
            logger.error(f"Failed to create directory {directory}: {str(e)}")
            raise

    try:
        # Create or clear the log file
        with open(LOG_FILE(), 'w') as f:
            pass  # Just create an empty file

        logger.debug(f"Created new log file: {LOG_FILE()}")
        return True

    except Exception as e:
        logger.error(f"Failed to create log file {LOG_FILE()}: {str(e)}")
        return False


def cleanup_old_files(directory: str, max_age_hours: int = 24) -> int:
    """
    Clean up old files in a directory
    
    Args:
        directory: Directory path to clean
        max_age_hours: Maximum age of files in hours before deletion
        
    Returns:
        int: Number of files deleted
    """
    if not os.path.exists(directory):
        return 0

    import time
    current_time = time.time()
    cutoff_time = current_time - (max_age_hours * 3600)
    deleted_count = 0

    try:
        for filename in os.listdir(directory):
            file_path = os.path.join(directory, filename)

            if os.path.isfile(file_path):
                file_mtime = os.path.getmtime(file_path)

                if file_mtime < cutoff_time:
                    os.remove(file_path)
                    deleted_count += 1
                    logger.debug(f"Deleted old file: {file_path}")

        if deleted_count > 0:
            logger.info(f"Cleaned up {deleted_count} old files from {directory}")

    except Exception as e:
        logger.error(f"Error during cleanup of {directory}: {str(e)}")

    return deleted_count


def save_json_file(data: Dict[Any, Any], file_path: str) -> bool:
    """
    Save data to a JSON file
    
    Args:
        data: Data to save
        file_path: Path to save the file
        
    Returns:
        bool: True if successful, False otherwise
    """
    try:
        # Ensure directory exists
        os.makedirs(os.path.dirname(file_path), exist_ok=True)

        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        logger.debug(f"Saved JSON file: {file_path}")
        return True

    except Exception as e:
        logger.error(f"Failed to save JSON file {file_path}: {str(e)}")
        return False


def load_json_file(file_path: str) -> Optional[Dict[Any, Any]]:
    """
    Load data from a JSON file
    
    Args:
        file_path: Path to the JSON file
        
    Returns:
        dict: Loaded data or None if failed
    """
    try:
        if not os.path.exists(file_path):
            return None

        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        logger.debug(f"Loaded JSON file: {file_path}")
        return data

    except Exception as e:
        logger.error(f"Failed to load JSON file {file_path}: {str(e)}")
        return None


def get_file_size(file_path: str) -> int:
    """
    Get file size in bytes
    
    Args:
        file_path: Path to the file
        
    Returns:
        int: File size in bytes, 0 if file doesn't exist
    """
    try:
        return os.path.getsize(file_path) if os.path.exists(file_path) else 0
    except Exception:
        return 0


def get_directory_size(directory: str) -> int:
    """
    Get total size of all files in a directory
    
    Args:
        directory: Directory path
        
    Returns:
        int: Total size in bytes
    """
    total_size = 0

    try:
        for dirpath, dirnames, filenames in os.walk(directory):
            for filename in filenames:
                file_path = os.path.join(dirpath, filename)
                try:
                    total_size += os.path.getsize(file_path)
                except (OSError, IOError):
                    continue

    except Exception:
        pass

    return total_size


def list_simulation_files(simulation_id: Optional[str] = None) -> List[Dict[str, Any]]:
    """
    List simulation-related files
    
    Args:
        simulation_id: Optional simulation ID to filter by
        
    Returns:
        List[Dict]: List of file information dictionaries
    """
    files = []

    # Check simulated matches directory
    if os.path.exists(SIMMATCHES_DIR):
        for filename in os.listdir(SIMMATCHES_DIR):
            if simulation_id and simulation_id not in filename:
                continue

            file_path = os.path.join(SIMMATCHES_DIR, filename)
            if os.path.isfile(file_path):
                files.append({
                    "type": "match_result",
                    "filename": filename,
                    "path": file_path,
                    "size": get_file_size(file_path),
                    "modified": os.path.getmtime(file_path)
                })

    # Check status directory
    if os.path.exists(STATUS_DIR):
        for filename in os.listdir(STATUS_DIR):
            if simulation_id and not filename.startswith(simulation_id):
                continue

            file_path = os.path.join(STATUS_DIR, filename)
            if os.path.isfile(file_path):
                files.append({
                    "type": "status",
                    "filename": filename,
                    "path": file_path,
                    "size": get_file_size(file_path),
                    "modified": os.path.getmtime(file_path)
                })

    return files


def delete_simulation_files(simulation_id: str) -> int:
    """
    Delete all files related to a simulation
    
    Args:
        simulation_id: Simulation ID
        
    Returns:
        int: Number of files deleted
    """
    deleted_count = 0
    files = list_simulation_files(simulation_id)

    for file_info in files:
        try:
            os.remove(file_info["path"])
            deleted_count += 1
            logger.debug(f"Deleted simulation file: {file_info['path']}")
        except Exception as e:
            logger.error(f"Failed to delete file {file_info['path']}: {str(e)}")

    if deleted_count > 0:
        logger.info(f"Deleted {deleted_count} files for simulation {simulation_id}")

    return deleted_count


def check_disk_space(directory: str, min_free_gb: float = 1.0) -> bool:
    """
    Check if there's enough free disk space
    
    Args:
        directory: Directory to check
        min_free_gb: Minimum free space required in GB
        
    Returns:
        bool: True if enough space available
    """
    try:
        import shutil
        total, used, free = shutil.disk_usage(directory)
        free_gb = free / (1024 ** 3)

        return free_gb >= min_free_gb

    except Exception:
        return True  # Assume space is available if check fails


def create_backup(source_path: str, backup_dir: str) -> Optional[str]:
    """
    Create a backup of a file or directory
    
    Args:
        source_path: Path to backup
        backup_dir: Backup destination directory
        
    Returns:
        str: Backup file path or None if failed
    """
    try:
        import datetime

        # Ensure backup directory exists
        os.makedirs(backup_dir, exist_ok=True)

        # Create backup filename with timestamp
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        source_name = os.path.basename(source_path)
        backup_name = f"{source_name}_{timestamp}.backup"
        backup_path = os.path.join(backup_dir, backup_name)

        if os.path.isfile(source_path):
            shutil.copy2(source_path, backup_path)
        elif os.path.isdir(source_path):
            shutil.copytree(source_path, backup_path)
        else:
            return None

        logger.info(f"Created backup: {backup_path}")
        return backup_path

    except Exception as e:
        logger.error(f"Failed to create backup of {source_path}: {str(e)}")
        return None


def get_system_stats() -> Dict[str, Any]:
    """
    Get system storage statistics
    
    Returns:
        dict: System statistics
    """
    stats = {}

    try:
        # Disk usage for main directories
        for name, directory in [
            ("simmatches", SIMMATCHES_DIR),
            ("status", STATUS_DIR),
            ("features", FEATURES_DIR),
            ("logs", LOG_FILE)
        ]:
            if os.path.exists(directory):
                stats[f"{name}_size_mb"] = get_directory_size(directory) / (1024 ** 2)
                stats[f"{name}_file_count"] = len([
                    f for f in os.listdir(directory)
                    if os.path.isfile(os.path.join(directory, f))
                ])

        # Total disk usage
        stats["total_app_size_mb"] = sum(
            stats.get(f"{name}_size_mb", 0)
            for name in ["simmatches", "status", "features", "logs"]
        )

        # Disk space
        if os.path.exists(SIMMATCHES_DIR):
            total, used, free = shutil.disk_usage(SIMMATCHES_DIR)
            stats.update({
                "disk_total_gb": total / (1024 ** 3),
                "disk_used_gb": used / (1024 ** 3),
                "disk_free_gb": free / (1024 ** 3),
                "disk_usage_percent": (used / total) * 100
            })

    except Exception as e:
        logger.error(f"Error getting system stats: {str(e)}")
        stats["error"] = str(e)

    return stats

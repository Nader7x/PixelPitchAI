"""
Performance Optimization Configuration
"""

# Text Generation Optimization Settings
GENERATION_CONFIG = {
    # Batch size for token generation (larger = better GPU utilization)
    "batch_size": 800,

    # Maximum context length to maintain
    "max_context_length": 1024,

    # Context retention when truncating
    "context_retention": 400,

    # Enable mixed precision for faster inference
    "use_mixed_precision": True,

    # Enable KV cache for faster generation
    "use_kv_cache": True,

    # GPU memory cleanup frequency (every N tokens)
    "memory_cleanup_frequency": 5000,

    # Enable torch.compile optimization (PyTorch 2.0+)
    "enable_torch_compile": True,

    # Compilation mode for torch.compile
    "compile_mode": "reduce-overhead"  # Options: "default", "reduce-overhead", "max-autotune"
}

# Memory Management Settings
MEMORY_CONFIG = {
    # Maximum cache size for bad words tokenization
    "bad_words_cache_size": 50,

    # GPU memory cleanup threshold (0.0-1.0)
    "gpu_cleanup_threshold": 0.8,

    # Enable automatic garbage collection
    "auto_gc": True,

    # GC frequency (every N generations)
    "gc_frequency": 10
}

# Threading and Process Pool Settings
CONCURRENCY_CONFIG = {
    # Use process pool for CPU-intensive tasks
    "use_process_pool_for_features": True,

    # Use process pool for parsing
    "use_process_pool_for_parsing": True,

    # Maximum workers for process pool
    "max_process_workers": 1,  # Single worker to avoid conflicts

    # Maximum workers for thread pool
    "max_thread_workers": 1,  # Single thread for GPU operations

    # Enable async status updates
    "async_status_updates": True,

    # Status update batch size
    "status_batch_size": 10,

    # Status update timeout (seconds)
    "status_update_timeout": 1.0
}

# Model Optimization Settings
MODEL_CONFIG = {
    # Enable FP16 precision
    "use_fp16": True,

    # Enable CUDA optimizations
    "cuda_benchmarking": True,

    # Disable deterministic mode for better performance
    "deterministic": False,

    # Pre-allocate GPU memory
    "pre_allocate_memory": True,

    # Model compilation settings
    "compile_model": True,
    "compile_mode": "reduce-overhead"
}

# Monitoring and Logging Settings
MONITORING_CONFIG = {
    # Enable performance monitoring
    "enable_monitoring": True,

    # Track generation times
    "track_generation_times": True,

    # Maximum performance history
    "max_history_size": 100,

    # Log performance warnings
    "performance_warnings": True,

    # Performance warning thresholds
    "slow_generation_threshold": 300.0,  # seconds
    "high_memory_threshold": 0.9  # 90% GPU memory usage
}

# Feature Generation Optimization
FEATURE_CONFIG = {
    # Cache feature generation results
    "cache_features": False,  # Disabled by default for accuracy

    # Feature cache size
    "feature_cache_size": 100,

    # Enable parallel feature processing
    "parallel_features": False,  # Disabled to avoid conflicts

    # Feature generation timeout
    "feature_timeout": 300  # 5 minutes
}

# Text Processing Optimization
TEXT_CONFIG = {
    # Use list for string building instead of concatenation
    "use_list_building": True,

    # Pre-compile regex patterns
    "precompile_regex": True,

    # Batch text processing
    "batch_text_processing": True,

    # Text processing chunk size
    "text_chunk_size": 10000
}

# Complete performance configuration
PERFORMANCE_CONFIG = {
    "generation": GENERATION_CONFIG,
    "memory": MEMORY_CONFIG,
    "concurrency": CONCURRENCY_CONFIG,
    "model": MODEL_CONFIG,
    "monitoring": MONITORING_CONFIG,
    "features": FEATURE_CONFIG,
    "text": TEXT_CONFIG
}

# Environment-specific overrides
ENVIRONMENT_OVERRIDES = {
    "development": {
        "generation": {
            "batch_size": 300,  # Smaller for development
            "memory_cleanup_frequency": 2000
        },
        "monitoring": {
            "enable_monitoring": True,
            "performance_warnings": True
        }
    },
    "production": {
        "generation": {
            "batch_size": 1000,  # Larger for production
            "memory_cleanup_frequency": 10000
        },
        "monitoring": {
            "enable_monitoring": False,  # Reduce overhead
            "performance_warnings": False
        }
    },
    "benchmarking": {
        "generation": {
            "batch_size": 500,
            "memory_cleanup_frequency": 1000
        },
        "monitoring": {
            "enable_monitoring": True,
            "track_generation_times": True,
            "performance_warnings": True
        }
    }
}


def get_performance_config(environment: str = "development"):
    """Get performance configuration for specified environment"""
    config = PERFORMANCE_CONFIG.copy()

    if environment in ENVIRONMENT_OVERRIDES:
        overrides = ENVIRONMENT_OVERRIDES[environment]
        for category, settings in overrides.items():
            if category in config:
                config[category].update(settings)

    return config


def apply_cuda_optimizations():
    """Apply CUDA-specific optimizations"""
    import torch

    if torch.cuda.is_available():
        # Enable CUDA optimizations
        torch.backends.cudnn.benchmark = MODEL_CONFIG["cuda_benchmarking"]
        torch.backends.cudnn.deterministic = MODEL_CONFIG["deterministic"]

        # Clear cache
        torch.cuda.empty_cache()

        print("CUDA optimizations applied")
    else:
        print("CUDA not available, skipping CUDA optimizations")


def get_optimal_batch_size(available_memory_gb: float, model_size_gb: float = 1.5) -> int:
    """Calculate optimal batch size based on available GPU memory"""
    # Conservative estimate: use 60% of available memory
    usable_memory = available_memory_gb * 0.6

    # Reserve memory for model
    available_for_batch = usable_memory - model_size_gb

    # Estimate tokens per GB (rough approximation)
    tokens_per_gb = 200000

    optimal_batch = min(int(available_for_batch * tokens_per_gb), 1500)

    # Ensure minimum batch size
    return max(optimal_batch, 300)


def auto_configure_performance():
    """Automatically configure performance settings based on system"""
    import torch
    import psutil

    config = get_performance_config()

    if torch.cuda.is_available():
        # Get GPU memory
        gpu_memory_gb = torch.cuda.get_device_properties(0).total_memory / (1024 ** 3)
        optimal_batch = get_optimal_batch_size(gpu_memory_gb)

        config["generation"]["batch_size"] = optimal_batch
        print(f"Auto-configured batch size: {optimal_batch}")

    # Configure based on CPU cores
    cpu_cores = psutil.cpu_count()
    if cpu_cores >= 8:
        config["concurrency"]["max_process_workers"] = 2
        config["concurrency"]["max_thread_workers"] = 2

    return config

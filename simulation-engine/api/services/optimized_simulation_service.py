"""
Ultra-Optimized Simulation Service with Advanced Performance Improvements
"""

import asyncio
import concurrent.futures
import functools
import gc
import io
import json
import mmap
import os
import psutil
import queue
import re
import sys
import threading
import time
import torch
import traceback
import xgboost as xgb
from collections import deque
from contextlib import contextmanager
from dataclasses import dataclass
from fastapi import HTTPException
from transformers import GPT2LMHeadModel, GPT2Tokenizer, BatchEncoding, StoppingCriteria, StoppingCriteriaList
from typing import Optional, List, Tuple, Dict, Any
from xgboost import XGBRegressor

# Advanced optimization imports
try:
    from transformers.models.gpt2.modeling_gpt2 import GPT2Attention
    from torch.nn.utils.rnn import pad_sequence
    from torch.utils.data import DataLoader
    import torch.nn.functional as F
    try:
        from accelerate import Accelerator
        ACCELERATE_AVAILABLE = True
    except ImportError:
        ACCELERATE_AVAILABLE = False
    from torch.profiler import profile, record_function, ProfilerActivity

    ADVANCED_OPTIMIZATIONS_AVAILABLE = True
except ImportError:
    ADVANCED_OPTIMIZATIONS_AVAILABLE = False
    ACCELERATE_AVAILABLE = False

    class GPT2Attention:
        pass

from ..config.settings import (
    MODEL_PATH, ONNX_MODEL_PATH, XGB_MODEL_PATH, SPECIAL_TOKENS, SPECIAL_LIST,
    HEADERLINES_DIR, INPUTTOKENS_DIR, SIMMATCHES_DIR, STATUS_DIR
)
from ..models.schemas import MatchRequest, SimulationStatus
from ..utils.logging import get_logger

logger = get_logger(__name__)

from ..core.parser import parse_and_publish
from ..core.xgboost_class import MatchStatProcessor


def _generate_features_wrapper(home_team_id: int, away_team_id: int,
                               home_team_season: str, away_team_season: str,
                               home_team_name: str, away_team_name: str) -> str:
    """Standalone wrapper for feature generation to use in process pool"""
    try:
        # Initialize required resources within the process
        import sys
        import os
        sys.path.append(os.path.dirname(os.path.dirname(os.path.dirname(__file__))))

        from api.core.xgboost_class import MatchStatProcessor
        import torch
        from transformers import GPT2Tokenizer
        import xgboost as xgb
        from xgboost import XGBRegressor

        # Import configuration
        from api.config.settings import (
            MODEL_PATH, XGB_MODEL_PATH, SPECIAL_TOKENS, SPECIAL_LIST,
            HEADERLINES_DIR, INPUTTOKENS_DIR
        )

        # Load models within the process
        tokenizer = GPT2Tokenizer.from_pretrained(MODEL_PATH)
        tokenizer.add_special_tokens(SPECIAL_TOKENS)
        tokenizer.pad_token = tokenizer.eos_token

        # Load XGBoost model
        booster = xgb.Booster()
        booster.load_model(XGB_MODEL_PATH)
        xgboost_model = XGBRegressor()
        xgboost_model._Booster = booster
        match_stat = MatchStatProcessor(xgboost_model, special_tokens=SPECIAL_LIST)
        
        # Generate features
        feature_dict = match_stat.generate_features(
            home_team_id,
            away_team_id,
            int(home_team_season),
            int(away_team_season)
        )
        features = match_stat.convert_to_text(home_team_name, away_team_name, feature_dict)

        # Create header lines
        header_lines_path = os.path.join(HEADERLINES_DIR, f"headerlines_{home_team_name}_{away_team_name}.txt")
        with open(header_lines_path, "w") as f:
            f.write(f"{home_team_name} vs {away_team_name}\n")
            f.write(f"Season: {home_team_season}/{away_team_season}\n")
            f.write("\n".join(features) + "\n")

        # Tokenize header lines
        with open(header_lines_path, "r") as f:
            header_text = f.read()

        input_ids = tokenizer.encode(header_text, return_tensors="pt")

        # Save input tokens
        input_tokens_path = os.path.join(INPUTTOKENS_DIR, f"inputtokens_{home_team_name}_{away_team_name}.pt")
        torch.save(input_ids, input_tokens_path)

        return input_tokens_path

    except Exception as e:
        # Can't use logger here since we're in a subprocess
        print(f"Error in feature generation: {str(e)}")
        raise



class SecondHalfStoppingCriteria(StoppingCriteria):
    def __init__(self, target_sequence_ids: List[int]):
        self.target_sequence_ids = target_sequence_ids
        self.target_length = len(target_sequence_ids)

    def __call__(self, input_ids: torch.LongTensor, scores: torch.FloatTensor, **kwargs) -> bool:
        if input_ids.shape[-1] < self.target_length:
            return False
        # Check if the last tokens match the target sequence
        last_tokens = input_ids[0, -self.target_length:].tolist()
        return last_tokens == self.target_sequence_ids

class MemoryMonitor:
    """Real-time memory monitoring and optimization"""

    def __init__(self):
        self.process = psutil.Process()
        self.gpu_memory_threshold = 0.8
        self.system_memory_threshold = 0.85

    def get_memory_stats(self) -> Dict[str, float]:
        """Get current memory usage statistics"""
        stats = {
            'system_memory_percent': psutil.virtual_memory().percent,
            'process_memory_mb': self.process.memory_info().rss / 1024 / 1024
        }

        if torch.cuda.is_available():
            stats['gpu_memory_allocated'] = torch.cuda.memory_allocated() / 1024 ** 3  # GB
            stats['gpu_memory_reserved'] = torch.cuda.memory_reserved() / 1024 ** 3  # GB
            stats['gpu_memory_percent'] = (torch.cuda.memory_allocated() /
                                           torch.cuda.get_device_properties(0).total_memory)

        return stats

    def should_cleanup(self) -> bool:
        """Check if memory cleanup is needed"""
        stats = self.get_memory_stats()

        return (stats.get('gpu_memory_percent', 0) > self.gpu_memory_threshold or
                stats.get('system_memory_percent', 0) > self.system_memory_threshold)

    def aggressive_cleanup(self):
        """Perform aggressive memory cleanup"""
        gc.collect()

        if torch.cuda.is_available():
            torch.cuda.empty_cache()
            torch.cuda.synchronize()


@dataclass
class MemoryPool:
    """Pre-allocated memory pool for tensor operations"""
    max_size: int = 50
    tensor_cache: Dict[tuple, torch.Tensor] = None

    def __post_init__(self):
        if self.tensor_cache is None:
            self.tensor_cache = {}
        self.lock = threading.Lock()

    def get_tensor(self, shape: tuple, dtype: torch.dtype, device: torch.device) -> torch.Tensor:
        """Get or create tensor from pool"""
        cache_key = (shape, dtype, device.type)

        with self.lock:
            if cache_key in self.tensor_cache:
                tensor = self.tensor_cache.pop(cache_key)
                tensor.zero_()
                return tensor

            return torch.zeros(shape, dtype=dtype, device=device)

    def return_tensor(self, tensor: torch.Tensor):
        """Return tensor to pool"""
        if len(self.tensor_cache) >= self.max_size:
            return  # Pool is full

        cache_key = (tensor.shape, tensor.dtype, tensor.device.type)

        with self.lock:
            self.tensor_cache[cache_key] = tensor.detach()


class AdvancedTokenizer:
    """Optimized tokenizer with caching and batching"""

    def __init__(self, tokenizer: GPT2Tokenizer):
        self.tokenizer = tokenizer
        self.cache = {}
        self.cache_lock = threading.Lock()
        self.max_cache_size = 1000

    def encode_cached(self, text: str, **kwargs) -> list[int] | Any:
        """Encode text with caching"""
        cache_key = (text, str(sorted(kwargs.items())))

        with self.cache_lock:
            if cache_key in self.cache:
                return self.cache[cache_key].clone()

        result = self.tokenizer.encode(text, return_tensors="pt", **kwargs)

        result = self.tokenizer.encode(text, return_tensors="pt", **kwargs)

        with self.cache_lock:
            if len(self.cache) < self.max_cache_size:
                if isinstance(result, torch.Tensor):
                    self.cache[cache_key] = result.clone()
                else:
                    self.cache[cache_key] = torch.tensor(result).clone()

        return result

    def batch_encode(self, texts: List[str], **kwargs) -> BatchEncoding:
        """Efficiently encode multiple texts"""
        return self.tokenizer(texts, return_tensors="pt", padding=True, **kwargs)


class StreamingGenerator:
    """Streaming text generator for reduced latency"""

    def __init__(self, model, tokenizer, device):
        self.model = model
        self.tokenizer = tokenizer
        self.device = device
        self.kv_cache = {}

    @torch.inference_mode()
    def generate_streaming(self, input_ids: torch.Tensor, **kwargs):
        """Generate tokens with streaming and KV cache reuse"""
        batch_size = input_ids.shape[0]
        max_new_tokens = kwargs.get('max_new_tokens', 100)
        temperature = kwargs.get('temperature', 1.0)
        top_p = kwargs.get('top_p', 0.9)
        top_k = kwargs.get('top_k', 50)

        # Initialize KV cache if not exists
        cache_key = id(input_ids)
        if cache_key not in self.kv_cache:
            self.kv_cache[cache_key] = {}

        current_input = input_ids.to(self.device)
        generated_tokens = []

        for _ in range(max_new_tokens):
            with torch.amp.autocast('cuda', enabled=torch.cuda.is_available()):
                outputs = self.model(current_input, use_cache=True,
                                     past_key_values=self.kv_cache[cache_key].get('past_key_values'))

                # Update KV cache
                self.kv_cache[cache_key]['past_key_values'] = outputs.past_key_values

                logits = outputs.logits[:, -1, :] / temperature

                # Apply top-k and top-p filtering
                if top_k > 0:
                    top_k_logits, top_k_indices = torch.topk(logits, top_k)
                    logits = torch.full_like(logits, float('-inf'))
                    logits.scatter_(1, top_k_indices, top_k_logits)

                if top_p < 1.0:
                    sorted_logits, sorted_indices = torch.sort(logits, descending=True)
                    cumulative_probs = torch.cumsum(F.softmax(sorted_logits, dim=-1), dim=-1)
                    sorted_indices_to_remove = cumulative_probs > top_p
                    sorted_indices_to_remove[..., 1:] = sorted_indices_to_remove[..., :-1].clone()
                    sorted_indices_to_remove[..., 0] = 0
                    indices_to_remove = sorted_indices_to_remove.scatter(1, sorted_indices, sorted_indices_to_remove)
                    logits[indices_to_remove] = float('-inf')

                probs = F.softmax(logits, dim=-1)
                next_token = torch.multinomial(probs, num_samples=1)

                generated_tokens.append(next_token)
                current_input = next_token

                yield next_token

        # Cleanup cache periodically
        if len(self.kv_cache) > 10:
            oldest_key = next(iter(self.kv_cache))
            del self.kv_cache[oldest_key]


class DynamicBatcher:
    """Dynamic batching for improved throughput"""

    def __init__(self, max_batch_size: int = 8, max_wait_time: float = 0.1):
        self.max_batch_size = max_batch_size
        self.max_wait_time = max_wait_time
        self.request_queue = asyncio.Queue()
        self.response_futures = {}

    async def add_request(self, request_id: str, input_data: Dict[str, Any]) -> Any:
        """Add request to batch queue"""
        future = asyncio.Future()
        self.response_futures[request_id] = future
        await self.request_queue.put((request_id, input_data))
        return await future

    async def process_batches(self, processor_func):
        """Process requests in batches"""
        while True:
            batch = []
            batch_ids = []

            # Collect requests for batch
            start_time = time.time()
            while (len(batch) < self.max_batch_size and
                   (time.time() - start_time) < self.max_wait_time):
                try:
                    request_id, request_data = await asyncio.wait_for(
                        self.request_queue.get(), timeout=0.01
                    )
                    batch.append(request_data)
                    batch_ids.append(request_id)
                except asyncio.TimeoutError:
                    if batch:  # Process partial batch
                        break
                    continue

            if batch:
                try:
                    # Process batch
                    results = await processor_func(batch)

                    # Distribute results
                    for batch_id, result in zip(batch_ids, results):
                        if batch_id in self.response_futures:
                            self.response_futures[batch_id].set_result(result)
                            del self.response_futures[batch_id]

                except Exception as e:
                    # Handle batch errors
                    for batch_id in batch_ids:
                        if batch_id in self.response_futures:
                            self.response_futures[batch_id].set_exception(e)
                            del self.response_futures[batch_id]


class UltraOptimizedModelResources:
    """Ultra-optimized model resource management with advanced techniques"""

    def __init__(self):
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        logger.info(f"Using device: {self.device}")

        # System resource monitoring
        self.memory_monitor = MemoryMonitor()

        # Memory pool for tensor operations
        self.memory_pool = MemoryPool()

        # Pre-compiled regex patterns for performance
        self.kickoff_pattern = re.compile(r"\d{2}:\d{2}\s*-\s*([^\s]+_\d{4})\s*-\s*pass by .*?Kick Off")
        self.second_half_pattern = re.compile(r"\[SECOND HALF START\]")
        self.match_end_pattern = re.compile(r"\[MATCH END\]")

        # Advanced caching systems
        self._bad_words_cache = {}
        self._text_cache = {}
        self._feature_cache = {}

        # Performance settings
        self.max_cache_size = 200
        self.cleanup_threshold = 0.75  # Cleanup when GPU memory usage > 75%
        self.enable_streaming = True
        self.enable_quantization = True

        # Initialize models with advanced optimizations
        self.bad_words = []
        self.bad_words_ids = None
        self.xgboost_model = XGBRegressor()

        self._load_and_optimize_models()

        # Initialize streaming generator and batcher
        self.streaming_generator = StreamingGenerator(self.model, self.tokenizer, self.device)
        self.dynamic_batcher = DynamicBatcher(max_batch_size=4, max_wait_time=0.05)

        # Start background optimization tasks
        self._start_background_tasks()        
        logger.info("Ultra-optimized models loaded successfully with advanced features")

    def _load_and_optimize_models(self):
        """Load and apply ultra-advanced optimizations to models"""
        try:
            # Determine path to use based on device
            is_cpu = self.device.type == "cpu"
            model_load_path = ONNX_MODEL_PATH if is_cpu else MODEL_PATH
            
            if is_cpu and not os.path.exists(model_load_path):
                # Fallback to standard ONNX path if INT8 folder is not found
                fallback_path = ONNX_MODEL_PATH.replace("-int8", "")
                if os.path.exists(fallback_path):
                    model_load_path = fallback_path
            
            # Load GPT-2 tokenizer
            self.tokenizer = GPT2Tokenizer.from_pretrained(model_load_path)
            self.tokenizer.add_special_tokens(SPECIAL_TOKENS)
            self.tokenizer.pad_token = self.tokenizer.eos_token

            # Wrap tokenizer with advanced caching
            self.advanced_tokenizer = AdvancedTokenizer(self.tokenizer)

            if is_cpu:
                logger.info(f"Loading ONNX model from {model_load_path} for CPU inference...")
                from optimum.onnxruntime import ORTModelForCausalLM
                import onnxruntime as ort
                import psutil

                # Configure optimized session options for CPU execution
                session_options = ort.SessionOptions()
                session_options.graph_optimization_level = ort.GraphOptimizationLevel.ORT_ENABLE_ALL
                physical_cores = psutil.cpu_count(logical=False) or 1
                session_options.intra_op_num_threads = physical_cores
                session_options.inter_op_num_threads = 1
                session_options.execution_mode = ort.ExecutionMode.ORT_SEQUENTIAL

                # Check if quantized model is available in the directory
                file_name = "model.onnx"
                if os.path.exists(os.path.join(model_load_path, "model_quantized.onnx")):
                    file_name = "model_quantized.onnx"
                    logger.info("Quantized INT8 ONNX model detected, loading it...")

                self.model = ORTModelForCausalLM.from_pretrained(
                    model_load_path,
                    file_name=file_name,
                    session_options=session_options
                )
                logger.info(f"ONNX model ({file_name}) loaded successfully for CPU execution")
            else:
                logger.info(f"Loading PyTorch model from {MODEL_PATH} for GPU inference...")
                self.model = GPT2LMHeadModel.from_pretrained(MODEL_PATH)
                self.model.eval()
                # Apply advanced model optimizations
                self._apply_ultra_optimizations()
                self.model.to(self.device)

            # Load XGBoost model
            booster = xgb.Booster()
            booster.load_model(XGB_MODEL_PATH)
            self.xgboost_model._Booster = booster
            self.match_stat = MatchStatProcessor(self.xgboost_model, tokenizer_path=MODEL_PATH, special_tokens=SPECIAL_LIST)

            logger.info(f"Ultra-optimized models loaded successfully")

        except Exception as e:
            logger.error(f"Failed to load ultra-optimized models: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Failed to load models: {str(e)}")

    def _apply_ultra_optimizations(self):
        """Apply ultra-advanced model optimizations appropriately for CPU/GPU"""
        try:
            if torch.cuda.is_available():
                # Use channels_last memory format for better performance
                for module in self.model.modules():
                    if hasattr(module, 'weight') and module.weight is not None:
                        if len(module.weight.shape) == 4:  # Conv layers
                            module.weight.data = module.weight.data.to(memory_format=torch.channels_last)

                # Use mixed precision with advanced settings
                self.model.half()  # FP16
                logger.info("Applied memory format optimizations and FP16 for GPU")

            # 4. Gradient Checkpointing (for memory efficiency during inference)
            if hasattr(self.model, 'gradient_checkpointing_enable'):
                self.model.gradient_checkpointing_enable()
                logger.info("Enabled gradient checkpointing")

            # 5. Attention Optimization
            self._optimize_attention_mechanism()

        except Exception as e:
            logger.warning(f"Could not apply all ultra-optimizations: {str(e)}")

    def _optimize_attention_mechanism(self):
        """Optimize attention mechanism for better performance"""
        try:
            # Replace standard attention with optimized version if available
            if ADVANCED_OPTIMIZATIONS_AVAILABLE:
                for name, module in self.model.named_modules():
                    if isinstance(module, GPT2Attention):
                        # Apply attention optimizations
                        module.attention_dropout = torch.nn.Dropout(0.0)  # Disable dropout in eval

                        # Enable flash attention if available
                        if hasattr(module, 'flash_attention'):
                            module.flash_attention = True

                logger.info("Applied attention mechanism optimizations")

        except Exception as e:
            logger.warning(f"Could not optimize attention mechanism: {str(e)}")

    def _start_background_tasks(self):
        """Background optimization tasks (Disabled to prevent GC stutters)"""
        pass

    def _optimize_caches(self):
        """Optimize all caches for memory efficiency"""
        # Optimize bad words cache
        if len(self._bad_words_cache) > self.max_cache_size:
            # Keep only most recently used items
            items = list(self._bad_words_cache.items())
            items.sort(key=lambda x: getattr(x[1], 'last_used', 0))
            self._bad_words_cache = dict(items[-self.max_cache_size // 2:])

        # Optimize text cache
        if len(self._text_cache) > self.max_cache_size:
            items = list(self._text_cache.items())
            self._text_cache = dict(items[-self.max_cache_size // 2:])

        # Optimize feature cache
        if len(self._feature_cache) > self.max_cache_size:
            items = list(self._feature_cache.items())
            self._feature_cache = dict(items[-self.max_cache_size // 2:])

    def _analyze_and_optimize_caches(self):
        """Analyze cache usage patterns and optimize accordingly"""
        # Get memory stats
        stats = self.memory_monitor.get_memory_stats()

        # Adjust cache sizes based on memory pressure
        if stats.get('gpu_memory_percent', 0) > 0.7:
            self.max_cache_size = max(50, self.max_cache_size - 10)
        elif stats.get('gpu_memory_percent', 0) < 0.5:
            self.max_cache_size = min(500, self.max_cache_size + 20)

    def get_cached_bad_words_ids(self, home_team: str, away_team: str) -> List[List[int]]:
        """Get cached bad words IDs with usage tracking"""
        cache_key = f"{home_team}_{away_team}"

        if cache_key in self._bad_words_cache:
            result = self._bad_words_cache[cache_key]
            # Update last used time for LRU
            if isinstance(result, dict):
                result['last_used'] = time.time()
                return result['data']
            return result

        # Generate bad words IDs
        bad_words = [t for t in SPECIAL_LIST if t not in {home_team, away_team}]
        bad_words = bad_words[:-7]
        bad_words_ids = self.tokenizer(bad_words, add_special_tokens=False).input_ids

        # Cache with metadata
        self._bad_words_cache[cache_key] = {
            'data': bad_words_ids,
            'last_used': time.time(),
            'use_count': 1
        }

        # Cleanup cache if needed
        self._optimize_caches()

        return bad_words_ids

    @contextmanager
    def optimized_inference_context(self):
        """Context manager for optimized inference settings"""
        if torch.cuda.is_available():
            # Enable optimized CUDA settings
            with torch.backends.cudnn.flags(enabled=True, benchmark=True, deterministic=False):
                with torch.amp.autocast('cuda', enabled=True, dtype=torch.float16):
                    yield
        else:
            yield

    def cleanup_gpu_memory(self):
        """Enhanced GPU memory cleanup with more aggressive strategies"""
        if torch.cuda.is_available():
            # Clear all caches
            torch.cuda.empty_cache()
            torch.cuda.synchronize()

            # Force garbage collection
            gc.collect()

            # Clear streaming generator cache
            if hasattr(self, 'streaming_generator'):
                self.streaming_generator.kv_cache.clear()

            # Return tensors to memory pool
            if hasattr(self, 'memory_pool'):
                # Memory pool cleanup is automatic
                pass

            logger.debug("Aggressive GPU memory cleanup completed")


class UltraOptimizedSimulationService:
    """Ultra-optimized simulation service with maximum performance improvements"""

    def __init__(self):
        self.simulation_status = {}
        self.model_resources = None

        # Advanced performance monitoring
        self.generation_times = deque(maxlen=1000)
        self.avg_generation_time = 0
        self.throughput_history = deque(maxlen=100)
        self.performance_stats = {
            'total_tokens_generated': 0,
            'total_time_spent': 0,
            'cache_hit_rate': 0,
            'memory_efficiency': 0
        }

        # Advanced async processing
        self.status_update_queue = asyncio.Queue(maxsize=1000)
        self.status_update_task = None
        self.generation_pool = None

        # High-performance text processing
        self.text_processor_pool = concurrent.futures.ThreadPoolExecutor(
            max_workers=min(4, os.cpu_count() or 1)
        )

        # Advanced request batching
        self.request_batcher = None

    async def initialize(self):
        """Initialize the ultra-optimized simulation service"""
        try:
            logger.info("Initializing ultra-optimized simulation service with advanced features")

            # Initialize ultra-optimized model resources
            self.model_resources = UltraOptimizedModelResources()

            # Initialize advanced request batching
            self.request_batcher = DynamicBatcher(max_batch_size=6, max_wait_time=0.05)

            # Ensure required directories exist with optimized I/O
            await self._create_directories_async()

            # Start high-performance async processors
            self.status_update_task = asyncio.create_task(self._process_status_updates())

            # Start request batching processor
            asyncio.create_task(
                self.request_batcher.process_batches(self._process_generation_batch)
            )

            # Load existing statuses with memory mapping for large files
            await self._load_existing_statuses_optimized()

            # Initialize generation pool with optimal worker count
            optimal_workers = min(2, max(1, (os.cpu_count() or 1) // 2))
            self.generation_pool = concurrent.futures.ThreadPoolExecutor(
                max_workers=optimal_workers,
                thread_name_prefix="UltraGen"
            )

            logger.info("Ultra-optimized simulation service initialized with maximum performance")

        except Exception as e:
            logger.error(f"Error initializing ultra-optimized simulation service: {str(e)}")
            raise  

    async def _create_directories_async(self):
        """Create directories asynchronously for better I/O performance"""
        directories = [HEADERLINES_DIR, INPUTTOKENS_DIR, SIMMATCHES_DIR, STATUS_DIR]

        async def create_dir(directory):
            loop = asyncio.get_event_loop()
            await loop.run_in_executor(None, lambda: os.makedirs(directory, exist_ok=True))

        await asyncio.gather(*[create_dir(d) for d in directories])

    async def _load_existing_statuses_optimized(self):
        """Load existing statuses with memory mapping and async I/O"""
        if not os.path.exists(STATUS_DIR):
            return

        loop = asyncio.get_event_loop()

        def load_status_file(filepath):
            try:
                # Use memory mapping for large files
                with open(filepath, 'rb') as f:
                    if os.path.getsize(filepath) > 1024:  # Use mmap for files > 1KB
                        with mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ) as mmapped_file:
                            data = json.loads(mmapped_file.read().decode('utf-8'))
                    else:
                        data = json.load(f)

                sim_id = os.path.basename(filepath).split('.')[0]
                return sim_id, SimulationStatus(**data)

            except Exception as e:
                logger.warning(f"Could not load status file {filepath}: {str(e)}")
                return None, None

        # Load files in parallel
        status_files = [
            os.path.join(STATUS_DIR, f)
            for f in os.listdir(STATUS_DIR)
            if f.endswith('.json')
        ]

        if status_files:
            tasks = [loop.run_in_executor(None, load_status_file, f) for f in status_files]
            results = await asyncio.gather(*tasks, return_exceptions=True)

            for result in results:
                if isinstance(result, tuple) and result[0] is not None:
                    sim_id, status = result
                    self.simulation_status[sim_id] = status

    async def _process_status_updates(self):
        """Ultra-optimized status update processing with adaptive batching"""
        batch = []
        max_batch_size = 20
        adaptive_timeout = 0.5

        while True:
            try:
                start_time = time.time()

                # Adaptive batching based on system load
                current_load = psutil.cpu_percent(interval=0.1)
                dynamic_batch_size = max(5, min(max_batch_size, int(max_batch_size * (100 - current_load) / 100)))

                # Collect updates with adaptive timeout
                while (len(batch) < dynamic_batch_size and
                       (time.time() - start_time) < adaptive_timeout):
                    try:
                        update = await asyncio.wait_for(
                            self.status_update_queue.get(),
                            timeout=0.01
                        )
                        batch.append(update)
                    except asyncio.TimeoutError:
                        if batch:
                            break
                        continue

                if batch:
                    await self._write_status_batch_optimized(batch)
                    batch = []

            except Exception as e:
                logger.error(f"Error in ultra-optimized status processor: {str(e)}")
                await asyncio.sleep(1)

    async def _write_status_batch_optimized(self, batch: List[Tuple[str, dict]]):
        """Write status batch with optimized I/O and compression"""

        async def write_single_status(sim_id, status_data):
            try:
                status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")

                # Use StringIO for in-memory JSON serialization
                json_str = json.dumps(status_data, separators=(',', ':'))  # Compact JSON

                # Write with optimized I/O
                loop = asyncio.get_event_loop()
                await loop.run_in_executor(
                    None,
                    lambda: self._write_file_atomic(status_path, json_str)
                )

            except Exception as e:
                logger.error(f"Error writing optimized status for {sim_id}: {str(e)}")

        # Write all status files concurrently
        tasks = [write_single_status(sim_id, status_data) for sim_id, status_data in batch]
        await asyncio.gather(*tasks, return_exceptions=True)

    def _write_file_atomic(self, filepath: str, content: str):
        """Atomic file write for data consistency"""
        temp_path = f"{filepath}.tmp"
        try:
            with open(temp_path, 'w', encoding='utf-8') as f:
                f.write(content)
                f.flush()
                os.fsync(f.fileno())  # Force write to disk
            os.replace(temp_path, filepath)  # Atomic move
        except Exception as e:
            if os.path.exists(temp_path):
                os.remove(temp_path)
            raise e

    async def _process_generation_batch(self, batch_requests: List[Dict[str, Any]]) -> List[str]:
        """Process a batch of generation requests for maximum throughput"""
        try:
            with self.model_resources.optimized_inference_context():
                results = []

                # Process batch in parallel with optimal resource utilization
                for request in batch_requests:
                    # Use streaming generation for better performance                    
                    result = await self._generate_text_ultra_optimized(**request)
                    results.append(result)

                return results

        except Exception as e:
            logger.error(f"Error processing generation batch: {str(e)}")
            raise

    async def _generate_text_ultra_optimized(self, home_team: str, away_team: str,
                                             input_tokens_file: str, **kwargs) -> str:
        """Ultra-optimized text generation with maximum performance"""
        loop = asyncio.get_event_loop()

        # Create a wrapper function that handles the keyword arguments
        def generate_wrapper():
            return self.generate_text_ultra_optimized(
                home_team, away_team, input_tokens_file, **kwargs
            )

        return await loop.run_in_executor(
            self.generation_pool,
            generate_wrapper
        )

    def generate_text_ultra_optimized(self, home_team: str, away_team: str, input_tokens_file: str,
                                      num_tokens_to_generate: int = 300000,
                                      max_length: int = 1024, temperature: float = 0.7,
                                      top_p: float = 0.9, top_k: int = 50) -> str:
        """Ultra-optimized text generation handling max_length constraints efficiently"""
        import io
        start_time = time.time()
        generation_start = time.perf_counter()

        try:
            with self.model_resources.optimized_inference_context():
                tokenizer = self.model_resources.tokenizer
                model = self.model_resources.model
                device = self.model_resources.device

                input_tokens = torch.load(input_tokens_file, map_location=device)
                if input_tokens is None:
                    raise HTTPException(status_code=400, detail="No input tokens provided")

                bad_words_ids = self.model_resources.get_cached_bad_words_ids(home_team, away_team)

                logger.info(f"Starting ultra-optimized generation: {num_tokens_to_generate} tokens")

                frozen_prefix = input_tokens.clone()
                generated_tokens = input_tokens.clone()
                
                num_generated = 0
                first_half_kickoff_detected = False
                second_half_kickoff_inserted = False
                first_half_kickoff_team = None

                text_buffer = io.StringIO()
                text_buffer.write(tokenizer.decode(input_tokens[0]))
                
                with torch.amp.autocast('cuda', enabled=torch.cuda.is_available(), dtype=torch.float16):
                    with torch.inference_mode():
                        while num_generated < num_tokens_to_generate:
                            tokens_left = num_tokens_to_generate - num_generated
                            
                            # Calculate space left in the model's max_length context
                            space_left = max_length - generated_tokens.shape[1] - 10
                            
                            # If context is near max_length, trim it to preserve the recent history
                            if space_left < 100:
                                keep_last_n = 400
                                context_tail = generated_tokens[:, -keep_last_n:]
                                generated_tokens = torch.cat((frozen_prefix, context_tail), dim=1)
                                space_left = max_length - generated_tokens.shape[1] - 10
                            
                            step_tokens = min(space_left, tokens_left)
                            if step_tokens <= 0:
                                break

                            current_input = generated_tokens[:, -max_length:]
                            current_attention_mask = (current_input != tokenizer.pad_token_id).long()
                            
                            output = model.generate(
                                input_ids=current_input,
                                attention_mask=current_attention_mask,
                                max_new_tokens=step_tokens,
                                temperature=temperature,
                                top_p=top_p,
                                top_k=top_k,
                                do_sample=True,
                                pad_token_id=tokenizer.eos_token_id,
                                bad_words_ids=bad_words_ids,
                                use_cache=True
                            )
                            
                            new_tokens = output[:, current_input.shape[1]:]
                            generated_tokens = torch.cat((generated_tokens, new_tokens), dim=1)
                            
                            new_text = tokenizer.decode(new_tokens[0], skip_special_tokens=False)
                            
                            if not first_half_kickoff_detected:
                                kickoff_match = self.model_resources.kickoff_pattern.search(new_text)
                                if kickoff_match:
                                    first_half_kickoff_team = kickoff_match.group(1)
                                    first_half_kickoff_detected = True

                            if "[SECOND HALF START]" in new_text and not second_half_kickoff_inserted and first_half_kickoff_team:
                                split_point = new_text.index("[SECOND HALF START]") + len("[SECOND HALF START]")
                                before_second_half = new_text[:split_point]
                                text_buffer.write(before_second_half)
                                
                                second_half_kickoff_team = away_team if first_half_kickoff_team in home_team else home_team
                                kickoff_event = f"\\n45:00 - {second_half_kickoff_team}  - pass by"
                                text_buffer.write(kickoff_event)
                                
                                injected_text = "[SECOND HALF START]\\n" + kickoff_event.strip()
                                injected_tokens = tokenizer.encode(injected_text, return_tensors="pt").to(device)
                                
                                generated_tokens = torch.cat((frozen_prefix, injected_tokens), dim=1)
                                second_half_kickoff_inserted = True
                                
                                num_generated += len(new_tokens[0])
                                continue
                            
                            text_buffer.write(new_text)
                            num_generated += len(new_tokens[0])
                            
                            if "[MATCH END]" in new_text:
                                break

                final_text = text_buffer.getvalue()
                if "[MATCH END]" not in final_text and num_generated >= num_tokens_to_generate:
                    final_text += '\\n[MATCH END]'
                text_buffer.close()

                generation_time = time.perf_counter() - generation_start
                tokens_per_second = num_generated / generation_time if generation_time > 0 else 0

                self.generation_times.append(generation_time)
                self.avg_generation_time = sum(self.generation_times) / len(self.generation_times)
                self.throughput_history.append(tokens_per_second)
                
                self.performance_stats['total_tokens_generated'] += num_generated
                self.performance_stats['total_time_spent'] += generation_time

                logger.info(f"Ultra-optimized generation completed: {generation_time:.2f}s, "
                            f"{len(final_text)} chars, {tokens_per_second:.1f} tokens/s")

                return final_text

        except Exception as e:
            error_msg = f"Error in ultra-optimized text generation: {str(e)}"
            logger.error(error_msg)
            import traceback
            logger.error(f"Traceback: {traceback.format_exc()}")
            raise HTTPException(status_code=500, detail=error_msg)

    def _calculate_optimal_batch_size(self, base_batch_size: int) -> int:
        """Calculate optimal batch size based on system resources and performance"""
        try:
            # Get current memory stats
            stats = self.model_resources.memory_monitor.get_memory_stats()

            # Adjust based on GPU memory usage
            gpu_memory_percent = stats.get('gpu_memory_percent', 0.5)
            if gpu_memory_percent > 0.8:
                return max(100, base_batch_size // 2)
            elif gpu_memory_percent < 0.4:
                return min(500, base_batch_size * 2)

            # Adjust based on recent performance
            if len(self.throughput_history) > 5:
                recent_throughput = sum(list(self.throughput_history)[-5:]) / 5
                if recent_throughput < 10:  # tokens/second
                    return max(50, base_batch_size // 2)
                elif recent_throughput > 25:
                    return min(400, int(base_batch_size * 1.5))

            return base_batch_size

        except Exception as e:
            logger.warning(f"Error calculating optimal batch size: {str(e)}")
            return base_batch_size

    def _inject_second_half_kickoff_optimized(self, new_text: str, home_team: str,
                                              away_team: str, first_half_kickoff_team: str) -> str:
        """Optimized second half kickoff injection"""
        try:
            split_index = new_text.find("[SECOND HALF START]")
            if split_index == -1:
                return new_text

            split_point = split_index + len("[SECOND HALF START]")
            before_second_half = new_text[:split_point]

            # Determine kickoff team efficiently
            second_half_kickoff_team = away_team if first_half_kickoff_team in home_team else home_team
            kickoff_event = f"45:00 - {second_half_kickoff_team} - pass by"
            return f"{before_second_half}\n{kickoff_event}"

        except Exception as e:
            logger.error(f"Error in optimized kickoff injection: {str(e)}")
            return new_text

    async def update_simulation_status_async(self, sim_id: str, status_update: dict):
        """Ultra-optimized asynchronous status updates"""
        try:
            # Update in-memory status with minimal overhead
            if sim_id in self.simulation_status:
                current_status = self.simulation_status[sim_id].dict()
                current_status.update(status_update)
                self.simulation_status[sim_id] = SimulationStatus(**current_status)
            else:
                # Create new status entry with optimized defaults
                default_status = {
                    "simulation_id": sim_id,
                    "match_id": status_update.get("match_id", 0),
                    "status": status_update.get("status", "unknown"),
                    "start_time": status_update.get("start_time", time.time()),
                    "optimization_level": "ultra"
                }
                default_status.update(status_update)
                self.simulation_status[sim_id] = SimulationStatus(**default_status)

            # Queue for ultra-fast async disk write
            try:
                self.status_update_queue.put_nowait((sim_id, self.simulation_status[sim_id].dict()))
            except asyncio.QueueFull:
                # If queue is full, process immediately to avoid blocking
                await self._write_status_batch_optimized([(sim_id, self.simulation_status[sim_id].dict())])

        except Exception as e:
            logger.error(f"Error in ultra-optimized status update: {str(e)}")

    def get_simulation_status(self, sim_id: str) -> Optional[SimulationStatus]:
        """Get simulation status by ID with caching"""
        return self.simulation_status.get(sim_id)

    def get_all_simulation_statuses(self) -> dict:
        """Get all simulation statuses with optimized serialization"""
        return {sim_id: status.dict() for sim_id, status in self.simulation_status.items()}

    def get_performance_metrics(self) -> dict:
        """Get comprehensive performance metrics"""
        avg_throughput = 0
        if self.throughput_history:
            avg_throughput = sum(self.throughput_history) / len(self.throughput_history)

        memory_stats = {}
        if self.model_resources and hasattr(self.model_resources, 'memory_monitor'):
            memory_stats = self.model_resources.memory_monitor.get_memory_stats()

        return {
            "average_generation_time": self.avg_generation_time,
            "average_throughput_tokens_per_second": avg_throughput,
            "total_simulations": len(self.simulation_status),
            "performance_stats": self.performance_stats,
            "memory_stats": memory_stats,
            "optimization_level": "ultra-maximum"
        }

    async def process_match_simulation_ultra_optimized(self, simulation_id: str, request: MatchRequest):
        """Ultra-optimized match simulation with maximum performance"""
        start_time = time.perf_counter()
        total_start_time = time.time()

        try:
            await self.update_simulation_status_async(simulation_id, {
                "status": "processing",
                "progress": 5.0,
                "optimization_level": "ultra"
            })

            # Prepare team names with optimized string operations
            home_team_season = request.home_team_season.split("/")[-1]
            away_team_season = request.away_team_season.split("/")[-1]
            home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
            away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"
            bad_home_team_name = f"{request.home_team_name.replace(' ', '_')}_{away_team_season}"
            bad_away_team_name = f"{request.away_team_name.replace(' ', '_')}_{home_team_season}"

            # Generate features inline using the already loaded model (NO ProcessPool!)
            loop = asyncio.get_event_loop()
            
            def generate_features_sync():
                match_stat = self.model_resources.match_stat
                features = match_stat.generate_features(
                    request.home_team_id, request.away_team_id, 
                    int(home_team_season), int(away_team_season)
                )
                header_lines = match_stat.convert_to_text(home_team_name, away_team_name, features)
                
                os.makedirs(HEADERLINES_DIR, exist_ok=True)
                os.makedirs(INPUTTOKENS_DIR, exist_ok=True)
                
                header_path = os.path.join(HEADERLINES_DIR, f"{home_team_name}_vs_{away_team_name}_header_lines.txt")
                match_stat.save_text_file(header_lines, header_path)
                
                input_tokens_path = os.path.join(INPUTTOKENS_DIR, f"{home_team_name}_vs_{away_team_name}_input_tokens.pt")
                match_stat.tokenize_and_save(header_path, input_tokens_path)
                return input_tokens_path

            input_tokens_path = await loop.run_in_executor(None, generate_features_sync)
            os.makedirs(SIMMATCHES_DIR, exist_ok=True)

            await self.update_simulation_status_async(simulation_id, {"progress": 20.0})
            logger.info(f"Ultra-fast input tokens generated: {input_tokens_path}")

            # Process through ultra-optimized generation pipeline without DynamicBatcher overhead
            generated_text = await loop.run_in_executor(
                self.generation_pool,
                self.generate_text_ultra_optimized,
                home_team_name, away_team_name, input_tokens_path,
                request.num_tokens_to_generate, request.max_length,
                request.temperature, request.top_p, request.top_k
            )

            await self.update_simulation_status_async(simulation_id, {"progress": 80.0})

            # Optimized file I/O with async operations
            simulated_match_path = os.path.join(SIMMATCHES_DIR, f"match_{request.match_id}_{simulation_id}.txt")

            await loop.run_in_executor(
                None,
                lambda: self._write_file_atomic(simulated_match_path, generated_text)
            )

            logger.info(f"Ultra-optimized match saved to {simulated_match_path}")

            # Parse events inline (NO ProcessPool!)
            events = await loop.run_in_executor(
                None,
                parse_and_publish,
                simulated_match_path
            )

            # Update performance metrics while parsing
            generation_time = time.perf_counter() - start_time
            total_time = time.time() - total_start_time

            logger.info(f"Ultra-fast parsing completed: {len(events)} events")

            # Final status update with comprehensive metrics
            await self.update_simulation_status_async(simulation_id, {
                "status": "completed",
                "progress": 100.0,
                "end_time": time.time(),
                "events_count": len(events),
                "result_path": simulated_match_path,
                "generation_time": generation_time,
                "total_time": total_time,
                "performance_metrics": {
                    "tokens_generated": request.num_tokens_to_generate,
                    "avg_tokens_per_second": request.num_tokens_to_generate / generation_time if generation_time > 0 else 0,
                    "memory_efficiency": "ultra-optimized",
                    "optimization_level": "maximum"
                }
            })

            # Immediate cleanup for next request
            self.model_resources.cleanup_gpu_memory()

            logger.info(f"Ultra-optimized simulation {simulation_id} completed in {total_time:.2f}s "
                        f"(generation: {generation_time:.2f}s)")

        except Exception as e:
            error_msg = f"Error in ultra-optimized simulation: {str(e)}"
            logger.error(error_msg)
            logger.error(f"Exception type: {type(e).__name__}")
            import traceback
            logger.error(f"Traceback: {traceback.format_exc()}")

            await self.update_simulation_status_async(simulation_id, {
                "status": "failed",
                "end_time": time.time(),
                "error_message": error_msg,
                "optimization_level": "ultra"
            })
            raise

    async def cleanup(self):
        """Ultra-comprehensive cleanup with advanced resource management"""
        try:
            logger.info("Starting ultra-comprehensive cleanup")

            # Stop all background tasks
            if self.status_update_task:
                self.status_update_task.cancel()
                try:
                    await self.status_update_task
                except asyncio.CancelledError:
                    pass

            # Process remaining status updates efficiently
            remaining_updates = []
            while not self.status_update_queue.empty():
                try:
                    update = self.status_update_queue.get_nowait()
                    remaining_updates.append(update)
                except asyncio.QueueEmpty:
                    break

            if remaining_updates:
                await self._write_status_batch_optimized(remaining_updates)

            # Shutdown thread pools gracefully
            if self.generation_pool:
                self.generation_pool.shutdown(wait=True)

            if self.text_processor_pool:
                self.text_processor_pool.shutdown(wait=True)

            # Advanced GPU memory cleanup
            if self.model_resources:
                self.model_resources.cleanup_gpu_memory()

                # Stop background threads
                if hasattr(self.model_resources, 'cleanup_thread'):
                    # Background threads are daemon threads, they'll stop automatically
                    pass

            # Final performance report
            if self.performance_stats['total_tokens_generated'] > 0:
                avg_throughput = (self.performance_stats['total_tokens_generated'] /
                                  self.performance_stats['total_time_spent'])
                logger.info(f"Final performance metrics: "
                            f"{self.performance_stats['total_tokens_generated']} tokens generated, "
                            f"avg throughput: {avg_throughput:.1f} tokens/s")

            logger.info("Ultra-comprehensive cleanup completed successfully")

        except Exception as e:
            logger.error(f"Error during ultra-comprehensive cleanup: {str(e)}")

    def create_simulation_status(self, simulation_id, match_id, start_time, metadata):
        """Create a new simulation status with ultra-optimized defaults"""
        """Create a new simulation status with ultra-optimized defaults"""
        status = SimulationStatus(
            simulation_id=simulation_id,
            match_id=match_id,
            start_time=start_time,
            status="initialized",
            progress=0.0,
            metadata=metadata
        )
        self.simulation_status[simulation_id] = status
        return status


# Global ultra-optimized service instance
ultra_optimized_simulation_service = UltraOptimizedSimulationService()


def get_optimized_simulation_service() -> UltraOptimizedSimulationService:
    """Get ultra-optimized simulation service instance"""
    return ultra_optimized_simulation_service

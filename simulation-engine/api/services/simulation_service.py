"""
Core Services Module
Handles model loading, simulation processing, and business logic
"""

import asyncio
import concurrent.futures
import functools
import json
import os
import sys
import time
import torch
import xgboost as xgb
from fastapi import HTTPException
from transformers import GPT2LMHeadModel, GPT2Tokenizer
from typing import Optional
from xgboost import XGBRegressor

from ..config.settings import (
    MODEL_PATH, XGB_MODEL_PATH, SPECIAL_TOKENS, SPECIAL_LIST,
    HEADERLINES_DIR, INPUTTOKENS_DIR, SIMMATCHES_DIR, STATUS_DIR
)
from ..models.schemas import MatchRequest, SimulationStatus
from ..utils.logging import get_logger

# Initialize logger
logger = get_logger(__name__)

from ..core.parser import parse_and_publish
from ..core.xgboost_class import MatchStatProcessor


class ModelResources:
    """Centralized model resource management"""

    def __init__(self):
        """Initialize all model resources"""
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        logger.info(f"Using device: {self.device}")
        self.bad_words = []
        self.bad_words_ids = None
        self.xgboost_model = XGBRegressor()
        # Load GPT-2 model and tokenizer
        self._load_gpt2_model()

        # Load XGBoost model
        self._load_xgboost_model()

        logger.info("All models loaded successfully")

    def _load_gpt2_model(self):
        """Load GPT-2 model and tokenizer"""
        try:
            self.tokenizer = GPT2Tokenizer.from_pretrained(MODEL_PATH)
            self.tokenizer.add_special_tokens(SPECIAL_TOKENS)
            self.tokenizer.pad_token = self.tokenizer.eos_token

            self.model = GPT2LMHeadModel.from_pretrained(MODEL_PATH)

            self.model.eval()
            self.model.to(self.device)

            logger.info(f"GPT-2 model loaded from {MODEL_PATH}")
        except Exception as e:
            logger.error(f"Failed to load GPT-2 model: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Failed to load GPT-2 model: {str(e)}")

    def _load_xgboost_model(self):
        """Load XGBoost model"""
        try:
            booster = xgb.Booster()
            booster.load_model(XGB_MODEL_PATH)
            self.xgboost_model._Booster = booster
            self.match_stat = MatchStatProcessor(self.xgboost_model, tokenizer_path=MODEL_PATH, special_tokens=SPECIAL_LIST)

            logger.info(f"XGBoost model loaded from {XGB_MODEL_PATH}")
        except Exception as e:
            logger.error(f"Failed to load XGBoost model: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Failed to load XGBoost model: {str(e)}")

    def is_healthy(self) -> tuple[bool, bool]:
        """Check if models are loaded and healthy"""
        model_loaded = self.model is not None and self.tokenizer is not None
        xgboost_loaded = self.xgboost_model is not None
        return model_loaded, xgboost_loaded


# Create a process pool executor for CPU-intensive tasks
_process_pool = concurrent.futures.ProcessPoolExecutor()
# Create a thread pool executor for I/O tasks
_thread_pool = concurrent.futures.ThreadPoolExecutor()

# Global model resources instance
resources = ModelResources()


def get_resources() -> ModelResources:
    """Get model resources instance"""
    return resources


def run_in_process(func):
    """
    Decorator to run a function in a separate process
    """

    @functools.wraps(func)
    async def wrapper(*args, **kwargs):
        loop = asyncio.get_running_loop()
        return await loop.run_in_executor(_process_pool,
                                          functools.partial(func, *args, **kwargs))

    return wrapper


def run_in_thread(func):
    """
    Decorator to run a function in a separate thread
    """

    @functools.wraps(func)
    async def wrapper(*args, **kwargs):
        loop = asyncio.get_running_loop()
        return await loop.run_in_executor(_thread_pool, functools.partial(func, *args, **kwargs))

    return wrapper


class SimulationService:
    """Handles simulation processing and status management"""

    def __init__(self):
        self.simulation_status = {}
        self.resources = None

    async def initialize(self):
        """Initialize the simulation service"""
        try:
            logger.info("Initializing simulation service")
            # Get the model resources instance
            self.resources = get_resources()

            # Ensure required directories exist
            os.makedirs(HEADERLINES_DIR, exist_ok=True)
            os.makedirs(INPUTTOKENS_DIR, exist_ok=True)
            os.makedirs(SIMMATCHES_DIR, exist_ok=True)
            os.makedirs(STATUS_DIR, exist_ok=True)

            # Load any existing simulation statuses
            if os.path.exists(STATUS_DIR):
                for filename in os.listdir(STATUS_DIR):
                    if filename.endswith('.json'):
                        sim_id = filename.split('.')[0]
                        status_path = os.path.join(STATUS_DIR, filename)
                        try:
                            with open(status_path, 'r') as f:
                                loaded_status = json.load(f)
                                self.simulation_status[sim_id] = SimulationStatus(**loaded_status)
                        except Exception as e:
                            logger.warning(f"Could not load status file {filename}: {str(e)}")

            logger.info("Simulation service initialized successfully")
        except Exception as e:
            logger.error(f"Error initializing simulation service: {str(e)}")
            raise

    async def cleanup(self):
        """Clean up resources before shutdown"""
        try:
            logger.info("Cleaning up simulation service resources")
            # Save any in-memory simulation statuses to disk
            for sim_id, status in self.simulation_status.items():
                try:
                    status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")
                    with open(status_path, 'w') as f:
                        json.dump(status.dict(), f, indent=2)
                except Exception as e:
                    logger.warning(f"Could not save status for {sim_id}: {str(e)}")

            # Free GPU memory if using CUDA
            if torch.cuda.is_available():
                torch.cuda.empty_cache()

            logger.info("Simulation service cleanup completed")
        except Exception as e:
            logger.error(f"Error during simulation service cleanup: {str(e)}")

    def create_simulation_status(self, simulation_id: str, match_id: int, start_time: float, metadata: dict):
        """Create and store the initial simulation status

        Args:
            simulation_id: The unique ID for this simulation
            match_id: The match identifier
            start_time: Unix timestamp when the simulation started
            metadata: Additional information about the simulation
        """
        try:
            # Create a new simulation status object
            status = SimulationStatus(
                simulation_id=simulation_id,
                match_id=match_id,
                status="pending",
                start_time=start_time,
                end_time=None,
                progress=0.0,
                metadata=metadata
            )

            # Save in memory
            self.simulation_status[simulation_id] = status

            # Save to disk
            status_path = os.path.join(STATUS_DIR, f"{simulation_id}.json")
            with open(status_path, 'w') as f:
                json.dump(status.model_dump(), f, indent=2)

            logger.info(f"Created simulation status for ID: {simulation_id}, Match ID: {match_id}")

        except Exception as e:
            logger.error(f"Error creating simulation status: {str(e)}")
            raise Exception(f"Failed to create simulation status: {str(e)}")

    def generate_features(self, home_team_id: int, away_team_id: int, home_team_season: str,
                          away_team_season: str, home_team_name: str, away_team_name: str) -> str:
        """Generate features for match simulation using XGBoost model"""
        try:
            match_stat = resources.match_stat
            print(f"{home_team_id},{away_team_id},{home_team_season},{away_team_season}")
            features = match_stat.generate_features(home_team_id, away_team_id, int(home_team_season),
                                                    int(away_team_season))
            header_lines = match_stat.convert_to_text(home_team_name, away_team_name, features)
            logger.info(f"Generated features for {home_team_name} vs {away_team_name}: {features}")

            os.makedirs(HEADERLINES_DIR, exist_ok=True)
            os.makedirs(INPUTTOKENS_DIR, exist_ok=True)

            header_path = os.path.join(HEADERLINES_DIR, f"{home_team_name}_vs_{away_team_name}_header_lines.txt")
            match_stat.save_text_file(header_lines, header_path)

            input_tokens_path = os.path.join(INPUTTOKENS_DIR, f"{home_team_name}_vs_{away_team_name}_input_tokens.pt")
            match_stat.tokenize_and_save(header_path, input_tokens_path)

            return input_tokens_path
        except Exception as e:
            logger.error(f"Error generating features: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Failed to generate features: {str(e)}")

    def load_input_tokens(self, input_tokens_path: str) -> torch.Tensor:
        """Load input tokens from file"""
        try:
            return torch.load(input_tokens_path).to(resources.device)
        except Exception as e:
            logger.error(f"Error loading input tokens: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Failed to load input tokens: {str(e)}")

    def generate_text(self, home_team: str, away_team: str, input_tokens_file: str,
                      num_tokens_to_generate: int = 300000,
                      max_length: int = 1024, temperature: float = 0.7,
                      top_p: float = 0.9, top_k: int = 50) -> str:
        """Generate text using the fine-tuned GPT-2 model"""
        try:
            print()
            tokenizer = resources.tokenizer
            model = resources.model
            device = resources.device

            input_tokens = self.load_input_tokens(input_tokens_file)
            if input_tokens is None:
                raise HTTPException(status_code=400, detail="No input tokens provided")

            initial_text = tokenizer.decode(input_tokens[0])
            generated_tokens = input_tokens.clone()
            attention_mask = (input_tokens != tokenizer.pad_token_id).long()
            frozen_prefix = input_tokens.clone()
            num = 0
            num_to_generate = 300
            first_half_kickoff_detected = False
            second_half_kickoff_inserted = False
            first_half_kickoff_team = None

            bad_words = [t for t in SPECIAL_LIST if t not in {home_team, away_team}]
            bad_words = bad_words[:-7]
            bad_words_ids = tokenizer(bad_words, add_special_tokens=False).input_ids

            logger.info(f"Starting text generation with {num_tokens_to_generate} tokens")

            for _ in range(num_tokens_to_generate):
                if generated_tokens.shape[1] + num_to_generate >= max_length:
                    keep_last_n = 400
                    context_tail = generated_tokens[:, -keep_last_n:]
                    # Truncate to maintain max_length
                    generated_tokens = torch.cat((frozen_prefix, context_tail), dim=1)
                    attention_mask = (generated_tokens != tokenizer.pad_token_id).long()

                current_input = generated_tokens[:, -max_length:]
                current_attention_mask = attention_mask[:, -max_length:]
                logger.debug(
                    f"Current input shape: {current_input.shape}, attention mask shape: {current_attention_mask.shape}")

                output = model.generate(
                    input_ids=current_input,
                    attention_mask=current_attention_mask,
                    max_new_tokens=num_to_generate,
                    temperature=temperature,
                    top_p=top_p,
                    top_k=top_k,
                    do_sample=True,
                    pad_token_id=tokenizer.eos_token_id,
                    bad_words_ids=bad_words_ids,
                )

                num += num_to_generate
                new_token = output[:, -num_to_generate:]
                generated_tokens = torch.cat((generated_tokens, new_token), dim=1)
                new_attention_mask = torch.ones_like(new_token).to(device)
                attention_mask = torch.cat((attention_mask, new_attention_mask), dim=1)

                new_text = tokenizer.decode(new_token[0])

                ### ---- Detect kickoff team ---- ###
                if not first_half_kickoff_detected:
                    import re
                    kickoff_match = re.search(r"\d{2}:\d{2}\s*-\s*([^\s]+_\d{4})\s*-\s*pass by .*?Kick Off", new_text)
                    if kickoff_match:
                        first_half_kickoff_team = kickoff_match.group(1)
                        print(f"Detected first half kickoff team: {first_half_kickoff_team}")
                        first_half_kickoff_detected = True

                    ### ---- Inject second half kickoff event ---- ###
                if "[SECOND HALF START]" in new_text and not second_half_kickoff_inserted and first_half_kickoff_team:
                    # Cut off the text at [SECOND HALF] so we don't keep unwanted auto-generation
                    split_point = new_text.index("[SECOND HALF START]") + len("[SECOND HALF START]")
                    before_second_half = new_text[:split_point]

                    # Append only the part before/including [SECOND HALF]
                    initial_text += before_second_half + "\n"

                    # Manually inject second half kickoff event
                    second_half_kickoff_team = away_team if first_half_kickoff_team in home_team else home_team
                    kickoff_event = f"45:00 - {second_half_kickoff_team}  - pass by"
                    print(f"\n--- Injecting Second Half Kickoff: {kickoff_event.strip()} ---\n")

                    initial_text += kickoff_event

                    # Add [SECOND HALF] + kickoff event tokens
                    injected_text = "[SECOND HALF START]\n" + kickoff_event
                    injected_tokens = tokenizer.encode(injected_text, return_tensors="pt").to(device)

                    # Update generation state
                    generated_tokens = torch.cat((frozen_prefix, injected_tokens), dim=1)
                    attention_mask = torch.ones_like(generated_tokens).to(device)

                    second_half_kickoff_inserted = True

                    # Skip printing the rest of auto-generated content in this step
                    continue

                initial_text += new_text

                if num >= num_tokens_to_generate:
                    initial_text += '\n[MATCH END]'
                    break

                # Stop if [MATCH END] is found
                if "[MATCH END]" in new_text:
                    print("Special token '[MATCH END]' found. Stopping generation.")
                    break

                if num >= num_tokens_to_generate:
                    break

            logger.info(f"Text generation completed. Generated {len(initial_text)} characters")
            return initial_text

        except Exception as e:
            logger.error(f"Error in text generation: {str(e)}")
            raise HTTPException(status_code=500, detail=f"Text generation failed: {str(e)}")

    def update_simulation_status(self, sim_id: str, status_update: dict) -> Optional[SimulationStatus]:
        """Update the status of a simulation in memory and on disk"""
        try:
            if sim_id in self.simulation_status:
                current_status = self.simulation_status[sim_id].dict()
                current_status.update(status_update)
                self.simulation_status[sim_id] = SimulationStatus(**current_status)
            else:
                logger.warning(f"Simulation {sim_id} not found in memory, creating new entry")
                # Create minimal required fields if not provided
                default_status = {
                    "simulation_id": sim_id,
                    "match_id": status_update.get("match_id", 0),
                    "status": status_update.get("status", "unknown"),
                    "start_time": status_update.get("start_time", time.time())
                }
                default_status.update(status_update)
                self.simulation_status[sim_id] = SimulationStatus(**default_status)

            os.makedirs(STATUS_DIR, exist_ok=True)
            status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")
            with open(status_path, 'w') as f:
                json.dump(self.simulation_status[sim_id].dict(), f, indent=2)

            return self.simulation_status[sim_id]
        except Exception as e:
            logger.error(f"Error updating simulation status: {str(e)}")
            return None

    def get_simulation_status(self, sim_id: str) -> Optional[SimulationStatus]:
        """Get simulation status from memory or disk"""
        try:
            # Check memory first
            if sim_id in self.simulation_status:
                return self.simulation_status[sim_id]

            # Try to load from disk
            status_path = os.path.join(STATUS_DIR, f"{sim_id}.json")
            if os.path.exists(status_path):
                with open(status_path, 'r') as f:
                    loaded_status = json.load(f)
                    self.simulation_status[sim_id] = SimulationStatus(**loaded_status)
                    return self.simulation_status[sim_id]

            return None
        except Exception as e:
            logger.error(f"Error getting simulation status: {str(e)}")
            return None

    def add_webhook(self, simulation_id: str, webhook_url: str, webhook_secret: Optional[str] = None):
        """Add a webhook to a simulation

        Args:
            simulation_id: Unique simulation identifier
            webhook_url: URL to call when simulation completes
            webhook_secret: Optional secret for signing the webhook payload
        """
        try:
            # Get current simulation status
            sim_status = self.get_simulation_status(simulation_id)
            if not sim_status:
                logger.error(f"Cannot add webhook: Simulation {simulation_id} not found")
                return

            # Get current webhooks or initialize empty list
            webhooks = sim_status.model_dump().get("webhooks", [])

            # Add new webhook
            webhooks.append({
                "url": webhook_url,
                "secret": webhook_secret
            })

            # Update simulation status with webhooks
            self.update_simulation_status(simulation_id, {"webhooks": webhooks})

            logger.info(f"Added webhook for simulation {simulation_id}: {webhook_url}")
        except Exception as e:
            logger.error(f"Error adding webhook: {str(e)}")

    def get_simulation_webhooks(self, simulation_id: str) -> list:
        """Get webhooks registered for a simulation

        Args:
            simulation_id: Unique simulation identifier

        Returns:
            list: List of webhook configurations
        """
        try:
            sim_status = self.get_simulation_status(simulation_id)
            if not sim_status:
                return []

            return sim_status.model_dump().get("webhooks", [])
        except Exception as e:
            logger.error(f"Error getting webhooks: {str(e)}")

    async def process_match_simulation(self, simulation_id: str, request: MatchRequest):
        """Process a match simulation asynchronously"""
        start_time = time.time()

        try:
            self.update_simulation_status(simulation_id, {"status": "processing", "progress": 5.0})

            home_team_season = request.home_team_season.split("/")[-1]
            away_team_season = request.away_team_season.split("/")[-1]
            home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
            away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"
            bad_home_team_name = f"{request.home_team_name.replace(' ', '_')}_{away_team_season}"
            bad_away_team_name = f"{request.away_team_name.replace(' ', '_')}_{home_team_season}"  # Run CPU-intensive feature generation in thread pool
            loop = asyncio.get_event_loop()
            input_tokens_path = await loop.run_in_executor(
                _thread_pool,
                self.generate_features,
                request.home_team_id, request.away_team_id, home_team_season, away_team_season,
                home_team_name, away_team_name
            )
            self.update_simulation_status(simulation_id, {"progress": 20.0})
            logger.info(f"Input tokens generated and saved to {input_tokens_path}")

            os.makedirs(SIMMATCHES_DIR, exist_ok=True)  # Bad words
            resources.bad_words = [bad_home_team_name, bad_away_team_name]
            resources.bad_words_ids = resources.tokenizer(resources.bad_words, add_special_tokens=False).input_ids

            # Run CPU-intensive text generation in thread pool
            generated_text = await loop.run_in_executor(
                _thread_pool,
                self.generate_text,
                home_team_name,
                away_team_name,
                input_tokens_path,
                request.num_tokens_to_generate,
                request.max_length,
                request.temperature,
                request.top_p,
                request.top_k
            )
            self.update_simulation_status(simulation_id, {"progress": 80.0})

            simulated_match_path = os.path.join(SIMMATCHES_DIR, f"match_{request.match_id}_{simulation_id}.txt")
            with open(simulated_match_path, "w", encoding="utf-8") as f:
                f.write(generated_text)

            logger.info(f"Generated match events saved to {simulated_match_path}")

            events = parse_and_publish(simulated_match_path)
            logger.info(f"Successfully parsed and published {len(events)} events")

            self.update_simulation_status(simulation_id, {
                "status": "completed",
                "progress": 100.0,
                "end_time": time.time(),
                "events_count": len(events),
                "result_path": simulated_match_path
            })

            # Import and trigger webhooks if available
            try:
                from ..services.webhook_service import get_webhook_service
                webhook_service = get_webhook_service()
                webhooks = self.get_simulation_webhooks(simulation_id)
                if webhooks:
                    await webhook_service.trigger_webhooks(
                        simulation_id=simulation_id,
                        status="completed",
                        webhooks=webhooks
                    )
            except ImportError:
                logger.warning("Webhook service not available")
            except Exception as e:
                logger.error(f"Error triggering webhooks: {str(e)}")

        except Exception as e:
            logger.error(f"Error in match simulation: {str(e)}")

            error_msg = str(e)
            self.update_simulation_status(simulation_id, {
                "status": "failed",
                "end_time": time.time(),
                "error_message": error_msg
            })

            # Trigger webhooks for failed simulations
            try:
                from ..services.webhook_service import get_webhook_service
                webhook_service = get_webhook_service()
                webhooks = self.get_simulation_webhooks(simulation_id)
                if webhooks:
                    await webhook_service.trigger_webhooks(
                        simulation_id=simulation_id,
                        status="failed",
                        webhooks=webhooks,
                        error_message=error_msg
                    )
            except ImportError:
                logger.warning("Webhook service not available")
            raise


# Global simulation service instance
simulation_service = SimulationService()


def get_simulation_service() -> SimulationService:
    """Get simulation service instance"""
    return simulation_service

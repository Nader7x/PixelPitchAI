"""Services package for Football Match Simulation API"""

from importlib import import_module

__all__ = ["ModelResources", "get_resources", "run_in_process", "run_in_thread",
           "SimulationService", "simulation_service", "get_simulation_service",
           "WebhookService", "webhook_service", "get_webhook_service"]


def __getattr__(name):
    """Keep package exports available without initializing unused model services."""
    if name not in __all__:
        raise AttributeError(name)
    module = ".webhook_service" if name in {"WebhookService", "webhook_service", "get_webhook_service"} else ".simulation_service"
    return getattr(import_module(module, __name__), name)

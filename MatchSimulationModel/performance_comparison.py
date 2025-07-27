#!/usr/bin/env python3
"""
Performance Comparison Script - Compare Original vs Optimized Simulation Service
"""

import asyncio
import json
import psutil
import time
import torch
from dataclasses import dataclass
from typing import List, Dict, Any

from api.models.schemas import MatchRequest
from api.services.optimized_simulation_service import ultra_optimized_simulation_service
# Import the simulation services
from api.services.simulation_service import simulation_service


@dataclass
class PerformanceMetrics:
    """Performance metrics for comparison"""
    total_time: float
    memory_usage_mb: float
    gpu_memory_usage_mb: float
    cpu_usage_percent: float
    tokens_generated: int
    tokens_per_second: float
    error_occurred: bool = False
    error_message: str = ""


class PerformanceComparator:
    """Compare performance between original and optimized services"""

    def __init__(self):
        self.results: Dict[str, List[PerformanceMetrics]] = {
            "original": [],
            "optimized": []
        }

    async def run_comparison(self, test_request: MatchRequest, num_runs: int = 3):
        """Run performance comparison"""
        print("🚀 Starting Performance Comparison")
        print(f"Test Configuration:")
        print(f"  - Number of runs: {num_runs}")
        print(f"  - Tokens to generate: {test_request.num_tokens_to_generate}")
        print(f"  - Teams: {test_request.home_team_name} vs {test_request.away_team_name}")
        print("-" * 60)

        # Test original service
        print("\n📊 Testing Original Service...")
        for i in range(num_runs):
            print(f"  Run {i + 1}/{num_runs}")
            metrics = await self._test_service(simulation_service, test_request, "original")
            self.results["original"].append(metrics)
            if metrics.error_occurred:
                print(f"    ❌ Error: {metrics.error_message}")
            else:
                print(f"    ✅ Completed in {metrics.total_time:.2f}s ({metrics.tokens_per_second:.0f} tokens/s)")

        # Test optimized service
        print("\n⚡ Testing Optimized Service...")
        for i in range(num_runs):
            print(f"  Run {i + 1}/{num_runs}")
            metrics = await self._test_service(ultra_optimized_simulation_service, test_request, "optimized")
            self.results["optimized"].append(metrics)
            if metrics.error_occurred:
                print(f"    ❌ Error: {metrics.error_message}")
            else:
                print(f"    ✅ Completed in {metrics.total_time:.2f}s ({metrics.tokens_per_second:.0f} tokens/s)")
        # Generate comparison report
        self._generate_report()

    async def _test_service(self, service, request: MatchRequest, service_type: str) -> PerformanceMetrics:
        """Test a specific service and measure performance"""
        start_time = time.time()
        initial_memory = psutil.Process().memory_info().rss / 1024 / 1024
        initial_gpu_memory = 0

        if torch.cuda.is_available():
            torch.cuda.empty_cache()
            initial_gpu_memory = torch.cuda.memory_allocated() / 1024 / 1024

        try:
            # Initialize service if needed
            if hasattr(service, 'initialize'):
                # Check if service is already initialized
                if service_type == "original" and not hasattr(service, 'resources'):
                    await service.initialize()
                elif service_type == "optimized" and (
                        not hasattr(service, 'model_resources') or service.model_resources is None):
                    await service.initialize()

            # Generate simulation ID
            sim_id = f"test_{service_type}_{int(time.time())}"

            # Run simulation
            if service_type == "optimized":
                await service.process_match_simulation_ultra_optimized(sim_id, request)
            else:
                await service.process_match_simulation(sim_id, request)

            # Calculate metrics
            end_time = time.time()
            total_time = end_time - start_time

            final_memory = psutil.Process().memory_info().rss / 1024 / 1024
            memory_usage = final_memory - initial_memory

            gpu_memory_usage = 0
            if torch.cuda.is_available():
                final_gpu_memory = torch.cuda.memory_allocated() / 1024 / 1024
                gpu_memory_usage = final_gpu_memory - initial_gpu_memory

            cpu_usage = psutil.cpu_percent()
            tokens_generated = request.num_tokens_to_generate
            tokens_per_second = tokens_generated / total_time if total_time > 0 else 0

            return PerformanceMetrics(
                total_time=total_time,
                memory_usage_mb=memory_usage,
                gpu_memory_usage_mb=gpu_memory_usage,
                cpu_usage_percent=cpu_usage,
                tokens_generated=tokens_generated,
                tokens_per_second=tokens_per_second
            )
        except Exception as e:
            end_time = time.time()
            total_time = end_time - start_time

            # Better error logging
            error_msg = str(e) if str(e) else f"Unknown error of type {type(e).__name__}"
            print(f"    Exception details: {error_msg}")
            import traceback
            print(f"    Traceback: {traceback.format_exc()}")

            return PerformanceMetrics(
                total_time=total_time,
                memory_usage_mb=0,
                gpu_memory_usage_mb=0,
                cpu_usage_percent=0,
                tokens_generated=0,
                tokens_per_second=0,
                error_occurred=True,
                error_message=error_msg
            )

    def _generate_report(self):
        """Generate performance comparison report"""
        print("\n" + "=" * 80)
        print("📈 PERFORMANCE COMPARISON REPORT")
        print("=" * 80)

        # Calculate averages for successful runs
        original_successful = [m for m in self.results["original"] if not m.error_occurred]
        optimized_successful = [m for m in self.results["optimized"] if not m.error_occurred]

        if not original_successful and not optimized_successful:
            print("❌ No successful runs for comparison")
            return

        def avg(metrics_list, attr):
            if not metrics_list:
                return 0
            return sum(getattr(m, attr) for m in metrics_list) / len(metrics_list)

        # Original service stats
        print("\n🔄 Original Service:")
        if original_successful:
            print(f"  Successful runs: {len(original_successful)}/{len(self.results['original'])}")
            print(f"  Average time: {avg(original_successful, 'total_time'):.2f}s")
            print(f"  Average tokens/sec: {avg(original_successful, 'tokens_per_second'):.0f}")
            print(f"  Average memory usage: {avg(original_successful, 'memory_usage_mb'):.1f} MB")
            if torch.cuda.is_available():
                print(f"  Average GPU memory: {avg(original_successful, 'gpu_memory_usage_mb'):.1f} MB")
        else:
            print("  ❌ No successful runs")
            for m in self.results["original"]:
                print(f"    Error: {m.error_message}")

        # Optimized service stats
        print("\n⚡ Optimized Service:")
        if optimized_successful:
            print(f"  Successful runs: {len(optimized_successful)}/{len(self.results['optimized'])}")
            print(f"  Average time: {avg(optimized_successful, 'total_time'):.2f}s")
            print(f"  Average tokens/sec: {avg(optimized_successful, 'tokens_per_second'):.0f}")
            print(f"  Average memory usage: {avg(optimized_successful, 'memory_usage_mb'):.1f} MB")
            if torch.cuda.is_available():
                print(f"  Average GPU memory: {avg(optimized_successful, 'gpu_memory_usage_mb'):.1f} MB")
        else:
            print("  ❌ No successful runs")
            for m in self.results["optimized"]:
                print(f"    Error: {m.error_message}")

        # Performance comparison
        if original_successful and optimized_successful:
            print("\n🏆 Performance Improvements:")

            original_time = avg(original_successful, 'total_time')
            optimized_time = avg(optimized_successful, 'total_time')
            time_improvement = (original_time - optimized_time) / original_time * 100 if original_time > 0 else 0

            original_throughput = avg(original_successful, 'tokens_per_second')
            optimized_throughput = avg(optimized_successful, 'tokens_per_second')
            throughput_improvement = (
                                                 optimized_throughput - original_throughput) / original_throughput * 100 if original_throughput > 0 else 0

            original_memory = avg(original_successful, 'memory_usage_mb')
            optimized_memory = avg(optimized_successful, 'memory_usage_mb')
            memory_improvement = (
                                             original_memory - optimized_memory) / original_memory * 100 if original_memory > 0 else 0

            print(f"  Time reduction: {time_improvement:+.1f}%")
            print(f"  Throughput improvement: {throughput_improvement:+.1f}%")
            print(f"  Memory reduction: {memory_improvement:+.1f}%")

            if torch.cuda.is_available():
                original_gpu = avg(original_successful, 'gpu_memory_usage_mb')
                optimized_gpu = avg(optimized_successful, 'gpu_memory_usage_mb')
                gpu_improvement = (original_gpu - optimized_gpu) / original_gpu * 100 if original_gpu > 0 else 0
                print(f"  GPU memory reduction: {gpu_improvement:+.1f}%")

        # Save detailed results
        self._save_results()
        print(f"\n💾 Detailed results saved to performance_comparison_results.json")
        print("=" * 80)

    def _save_results(self):
        """Save detailed results to JSON file"""
        results_data = {
            "timestamp": time.time(),
            "system_info": {
                "cpu_count": psutil.cpu_count(),
                "memory_gb": psutil.virtual_memory().total / 1024 ** 3,
                "gpu_available": torch.cuda.is_available(),
                "gpu_name": torch.cuda.get_device_name(0) if torch.cuda.is_available() else None,
                "pytorch_version": torch.__version__
            },
            "results": {}
        }

        for service_type, metrics_list in self.results.items():
            results_data["results"][service_type] = []
            for metrics in metrics_list:
                results_data["results"][service_type].append({
                    "total_time": metrics.total_time,
                    "memory_usage_mb": metrics.memory_usage_mb,
                    "gpu_memory_usage_mb": metrics.gpu_memory_usage_mb,
                    "cpu_usage_percent": metrics.cpu_usage_percent,
                    "tokens_generated": metrics.tokens_generated,
                    "tokens_per_second": metrics.tokens_per_second,
                    "error_occurred": metrics.error_occurred,
                    "error_message": metrics.error_message
                })

        with open("performance_comparison_results.json", "w") as f:
            json.dump(results_data, f, indent=2)


async def main():
    """Main function to run performance comparison"""  # Test configuration - realistic scale for comparison
    test_request = MatchRequest(
        match_id=999,
        home_team_id=2,
        away_team_id=7,
        home_team_name="Barcelona",
        away_team_name="Real Madrid",
        home_team_season="2020/2021",  # Fixed format
        away_team_season="2020/2021",  # Fixed format
        num_tokens_to_generate=1000,  # More realistic for performance testing
        max_length=1024,
        temperature=0.7,
        top_p=0.9,
        top_k=50
    )
    comparator = PerformanceComparator()
    await comparator.run_comparison(test_request, num_runs=1)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n⚠️  Comparison interrupted by user")
    except Exception as e:
        print(f"\n❌ Error running comparison: {str(e)}")

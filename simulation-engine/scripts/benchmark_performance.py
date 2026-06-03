"""
Performance Benchmarking and Monitoring Script for Match Simulation
"""

import asyncio
import json
import psutil
import statistics
import time
import torch
import sys
import os
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from datetime import datetime
from typing import Dict, List, Optional
from api.models.schemas import MatchRequest
from api.services.optimized_simulation_service import get_optimized_simulation_service


def _get_gpu_memory_usage() -> float:
    """Get current GPU memory usage in MB"""
    if torch.cuda.is_available():
        return torch.cuda.memory_allocated() / 1024 / 1024
    return 0.0


def _calculate_tokens_per_second(request: MatchRequest, total_time: float) -> float:
    """Calculate tokens generated per second"""
    if total_time > 0:
        return request.num_tokens_to_generate / total_time
    return 0.0


def _generate_benchmark_report(results: List[Dict], request: MatchRequest) -> Dict:
    """Generate comprehensive benchmark report"""
    successful_runs = [r for r in results if r.get('status') != 'failed']
    failed_runs = [r for r in results if r.get('status') == 'failed']

    report = {
        'timestamp': datetime.now().isoformat(),
        'test_parameters': {
            'num_tokens_to_generate': request.num_tokens_to_generate,
            'max_length': request.max_length,
            'temperature': request.temperature,
            'top_p': request.top_p,
            'top_k': request.top_k,
            'home_team': f"{request.home_team_name} ({request.home_team_season})",
            'away_team': f"{request.away_team_name} ({request.away_team_season})"
        },
        'system_info': {
            'gpu_available': torch.cuda.is_available(),
            'gpu_name': torch.cuda.get_device_name(0) if torch.cuda.is_available() else None,
            'cpu_count': psutil.cpu_count(),
            'total_memory_gb': psutil.virtual_memory().total / (1024 ** 3),
            'torch_version': torch.__version__
        },
        'summary': {
            'total_runs': len(results),
            'successful_runs': len(successful_runs),
            'failed_runs': len(failed_runs),
            'success_rate': len(successful_runs) / len(results) * 100 if results else 0
        },
        'performance_metrics': {},
        'detailed_results': results
    }

    if successful_runs:
        total_times = [r['total_time'] for r in successful_runs if 'total_time' in r]
        tokens_per_sec = [r['tokens_per_second'] for r in successful_runs if 'tokens_per_second' in r]
        gpu_memory = [r['gpu_memory_used'] for r in successful_runs if 'gpu_memory_used' in r]

        report['performance_metrics'] = {
            'average_total_time': statistics.mean(total_times) if total_times else 0,
            'median_total_time': statistics.median(total_times) if total_times else 0,
            'min_total_time': min(total_times) if total_times else 0,
            'max_total_time': max(total_times) if total_times else 0,
            'std_dev_total_time': statistics.stdev(total_times) if len(total_times) > 1 else 0,
            'average_tokens_per_second': statistics.mean(tokens_per_sec) if tokens_per_sec else 0,
            'average_gpu_memory_mb': statistics.mean(gpu_memory) if gpu_memory else 0,
            'peak_gpu_memory_mb': max(gpu_memory) if gpu_memory else 0
        }

    return report


def save_report(report: Dict, filename: str = None):
    """Save benchmark report to file"""
    if filename is None:
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        filename = f"benchmark_report_{timestamp}.json"

    with open(filename, 'w') as f:
        json.dump(report, f, indent=2)

    print(f"Benchmark report saved to {filename}")
    return filename


class PerformanceMonitor:
    """Monitor and benchmark simulation performance"""

    def __init__(self):
        self.metrics = {
            'generation_times': [],
            'feature_generation_times': [],
            'parsing_times': [],
            'total_simulation_times': [],
            'gpu_memory_usage': [],
            'cpu_usage': [],
            'memory_usage': []
        }

    async def benchmark_simulation(self, request: MatchRequest, num_runs: int = 3) -> Dict:
        """Benchmark simulation performance"""
        simulation_service = get_optimized_simulation_service()
        await simulation_service.initialize()

        results = []

        for run in range(num_runs):
            print(f"Running benchmark {run + 1}/{num_runs}")

            # Monitor system resources before
            initial_gpu_memory = _get_gpu_memory_usage()
            initial_cpu = psutil.cpu_percent()
            initial_memory = psutil.virtual_memory().percent

            start_time = time.time()

            try:
                simulation_id = f"benchmark_{run}_{int(time.time())}"

                # Create simulation status
                simulation_service.create_simulation_status(
                    simulation_id=simulation_id,
                    match_id=12345,
                    start_time=start_time,
                    metadata={"benchmark": True, "run": run}
                )

                # Run simulation
                await simulation_service.process_match_simulation_ultra_optimized(simulation_id, request)

                end_time = time.time()
                total_time = end_time - start_time

                # Monitor system resources after
                final_gpu_memory = _get_gpu_memory_usage()
                final_cpu = psutil.cpu_percent()
                final_memory = psutil.virtual_memory().percent

                # Get simulation status for detailed metrics
                status = simulation_service.get_simulation_status(simulation_id)

                run_results = {
                    'run': run + 1,
                    'total_time': total_time,
                    'generation_time': getattr(status, 'generation_time', None),
                    'events_count': getattr(status, 'events_count', 0),
                    'gpu_memory_used': final_gpu_memory - initial_gpu_memory,
                    'cpu_usage': final_cpu,
                    'memory_usage': final_memory,
                    'tokens_per_second': _calculate_tokens_per_second(request, total_time),
                    'status': status.status if status else 'unknown'
                }

                results.append(run_results)
                self._update_metrics(run_results)

                print(f"Run {run + 1} completed in {total_time:.2f}s")

                # Clean up GPU memory between runs
                if torch.cuda.is_available():
                    torch.cuda.empty_cache()

            except Exception as e:
                print(f"Run {run + 1} failed: {str(e)}")
                results.append({
                    'run': run + 1,
                    'error': str(e),
                    'status': 'failed'
                })

        return _generate_benchmark_report(results, request)

    def _update_metrics(self, run_results: Dict):
        """Update performance metrics"""
        if 'total_time' in run_results:
            self.metrics['total_simulation_times'].append(run_results['total_time'])
        if 'generation_time' in run_results and run_results['generation_time']:
            self.metrics['generation_times'].append(run_results['generation_time'])
        if 'gpu_memory_used' in run_results:
            self.metrics['gpu_memory_usage'].append(run_results['gpu_memory_used'])
        if 'cpu_usage' in run_results:
            self.metrics['cpu_usage'].append(run_results['cpu_usage'])
        if 'memory_usage' in run_results:
            self.metrics['memory_usage'].append(run_results['memory_usage'])


async def run_performance_benchmark():
    """Run a comprehensive performance benchmark"""  # Create test request
    test_request = MatchRequest(
        match_id=12345,
        home_team_id=2,
        away_team_id=7,
        home_team_name="Barcelona",
        away_team_name="Real Madrid",
        home_team_season="2020/2021",  # Using valid season from dataset
        away_team_season="2020/2021",  # Using valid season from dataset
        num_tokens_to_generate=10000,  # Smaller f or benchmarking
        max_length=1024,
        temperature=0.7,
        top_p=0.9,
        top_k=50
    )

    monitor = PerformanceMonitor()

    print("Starting performance benchmark...")
    print(f"Test parameters: {test_request.num_tokens_to_generate} tokens")

    # Run benchmark
    report = await monitor.benchmark_simulation(test_request, num_runs=3)

    # Save and print results
    filename = save_report(report)

    print("\n" + "=" * 50)
    print("BENCHMARK RESULTS SUMMARY")
    print("=" * 50)
    print(f"Success Rate: {report['summary']['success_rate']:.1f}%")

    if report['performance_metrics']:
        metrics = report['performance_metrics']
        print(f"Average Total Time: {metrics['average_total_time']:.2f}s")
        print(f"Average Tokens/Second: {metrics['average_tokens_per_second']:.0f}")
        print(f"Peak GPU Memory: {metrics['peak_gpu_memory_mb']:.1f} MB")
        print(f"Time Std Dev: {metrics['std_dev_total_time']:.2f}s")

    print(f"\nDetailed report saved to: {filename}")

    return report


if __name__ == "__main__":
    # Run benchmark
    import sys
    import os

    sys.path.append(os.path.dirname(os.path.dirname(__file__)))

    asyncio.run(run_performance_benchmark())

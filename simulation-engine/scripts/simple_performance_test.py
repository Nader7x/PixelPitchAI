"""
Simple Performance Test for Text Generation Optimization
Tests the core text generation improvements without full API setup
"""

import os
import sys
import time
import torch

# Add the project root to the path
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

try:
    from api.services.simulation_service import SimulationService, get_simulation_service
    from api.models.schemas import MatchRequest

    print("✓ Successfully imported simulation service")
except ImportError as e:
    print(f"✗ Import error: {e}")
    print("Please ensure you're running from the project root directory")
    sys.exit(1)


class SimplePerformanceTest:
    """Simple performance test focusing on core optimizations"""

    def __init__(self):
        self.results = []

    def test_text_generation_performance(self):
        """Test text generation performance specifically"""
        print("=" * 60)
        print("MATCH SIMULATION PERFORMANCE TEST")
        print("=" * 60)

        # Test parameters
        test_cases = [
            {"tokens": 10000, "description": "Small test (10K tokens)"},
            {"tokens": 25000, "description": "Medium test (25K tokens)"},
            {"tokens": 50000, "description": "Large test (50K tokens)"}
        ]

        for i, test_case in enumerate(test_cases, 1):
            print(f"\n[Test {i}/3] {test_case['description']}")
            print("-" * 40)
            try:
                # Create test request
                request = MatchRequest(
                    match_id=12345 + i,
                    home_team_id=7,
                    away_team_id=2,
                    home_team_name="Real Madrid",
                    away_team_name="Barcelona",
                    home_team_season="2020/2021",
                    away_team_season="2020/2021",
                    num_tokens_to_generate=test_case["tokens"],
                    max_length=1024,
                    temperature=0.7,
                    top_p=0.9,
                    top_k=50
                )

                # Run the test
                result = self.run_single_test(request, test_case["description"])
                self.results.append(result)

                # Print immediate results
                if result["success"]:
                    print(f"✓ Completed in {result['total_time']:.2f}s")
                    print(f"  Tokens/second: {result['tokens_per_second']:.1f}")
                    print(f"  GPU Memory Used: {result['gpu_memory_mb']:.1f} MB")
                else:
                    print(f"✗ Failed: {result['error']}")

            except Exception as e:
                print(f"✗ Test failed: {str(e)}")
                self.results.append({
                    "test": test_case["description"],
                    "success": False,
                    "error": str(e)
                })

        self.print_summary()

    def run_single_test(self, request: MatchRequest, description: str) -> dict:
        """Run a single performance test"""
        result = {
            "test": description,
            "success": False,
            "total_time": 0,
            "tokens_per_second": 0,
            "gpu_memory_mb": 0,
            "error": None
        }

        try:
            # Initialize simulation service
            service = SimulationService()

            # Monitor GPU memory before
            initial_gpu_memory = self.get_gpu_memory_mb()

            start_time = time.time()

            # Test feature generation
            print("  Generating features...")
            feature_start = time.time()

            home_team_season = request.home_team_season.split("/")[-1]
            away_team_season = request.away_team_season.split("/")[-1]
            home_team_name = f"{request.home_team_name.replace(' ', '_')}_{home_team_season}"
            away_team_name = f"{request.away_team_name.replace(' ', '_')}_{away_team_season}"

            input_tokens_path = service.generate_features(
                request.home_team_id, request.away_team_id,
                home_team_season, away_team_season,
                home_team_name, away_team_name
            )

            feature_time = time.time() - feature_start
            print(f"  Feature generation: {feature_time:.2f}s")

            # Test text generation (the main performance bottleneck)
            print("  Generating text...")
            gen_start = time.time()

            generated_text = service.generate_text(
                home_team_name, away_team_name, input_tokens_path,
                request.num_tokens_to_generate, request.max_length,
                request.temperature, request.top_p, request.top_k
            )

            gen_time = time.time() - gen_start
            print(f"  Text generation: {gen_time:.2f}s")

            total_time = time.time() - start_time
            final_gpu_memory = self.get_gpu_memory_mb()

            # Calculate performance metrics
            tokens_per_second = request.num_tokens_to_generate / gen_time if gen_time > 0 else 0
            gpu_memory_used = final_gpu_memory - initial_gpu_memory

            result.update({
                "success": True,
                "total_time": total_time,
                "generation_time": gen_time,
                "feature_time": feature_time,
                "tokens_per_second": tokens_per_second,
                "gpu_memory_mb": gpu_memory_used,
                "text_length": len(generated_text),
                "tokens_generated": request.num_tokens_to_generate
            })

            # Cleanup
            if torch.cuda.is_available():
                torch.cuda.empty_cache()

        except Exception as e:
            result["error"] = str(e)

        return result

    def get_gpu_memory_mb(self) -> float:
        """Get current GPU memory usage in MB"""
        if torch.cuda.is_available():
            return torch.cuda.memory_allocated() / (1024 * 1024)
        return 0.0

    def print_summary(self):
        """Print performance test summary"""
        print("\n" + "=" * 60)
        print("PERFORMANCE TEST SUMMARY")
        print("=" * 60)

        successful_tests = [r for r in self.results if r["success"]]
        failed_tests = [r for r in self.results if not r["success"]]

        print(f"Total Tests: {len(self.results)}")
        print(f"Successful: {len(successful_tests)}")
        print(f"Failed: {len(failed_tests)}")

        if successful_tests:
            print("\nPerformance Metrics:")
            print("-" * 30)

            total_times = [r["total_time"] for r in successful_tests]
            gen_times = [r["generation_time"] for r in successful_tests]
            tokens_per_sec = [r["tokens_per_second"] for r in successful_tests]
            gpu_memory = [r["gpu_memory_mb"] for r in successful_tests]

            print(f"Average Total Time: {sum(total_times) / len(total_times):.2f}s")
            print(f"Average Generation Time: {sum(gen_times) / len(gen_times):.2f}s")
            print(f"Average Tokens/Second: {sum(tokens_per_sec) / len(tokens_per_sec):.1f}")
            print(f"Average GPU Memory: {sum(gpu_memory) / len(gpu_memory):.1f} MB")
            print(f"Peak GPU Memory: {max(gpu_memory):.1f} MB")

            print("\nDetailed Results:")
            print("-" * 30)
            for result in successful_tests:
                print(f"• {result['test']}")
                print(f"  Time: {result['total_time']:.2f}s | "
                      f"Tokens/sec: {result['tokens_per_second']:.1f} | "
                      f"GPU: {result['gpu_memory_mb']:.1f}MB")

        if failed_tests:
            print("\nFailed Tests:")
            print("-" * 20)
            for result in failed_tests:
                print(f"• {result['test']}: {result['error']}")

        # Performance analysis
        if successful_tests:
            best_performance = max(successful_tests, key=lambda x: x["tokens_per_second"])
            print(f"\nBest Performance:")
            print(f"• {best_performance['test']}")
            print(f"• {best_performance['tokens_per_second']:.1f} tokens/second")

            # Performance rating
            avg_tokens_per_sec = sum(tokens_per_sec) / len(tokens_per_sec)
            if avg_tokens_per_sec > 2000:
                rating = "EXCELLENT"
            elif avg_tokens_per_sec > 1500:
                rating = "GOOD"
            elif avg_tokens_per_sec > 1000:
                rating = "AVERAGE"
            else:
                rating = "NEEDS IMPROVEMENT"

            print(f"\nOverall Performance Rating: {rating}")
            print(f"(Based on {avg_tokens_per_sec:.1f} tokens/second average)")


def check_system_setup():
    """Check if the system is properly set up for testing"""
    print("Checking system setup...")

    # Check CUDA
    if torch.cuda.is_available():
        gpu_name = torch.cuda.get_device_name(0)
        gpu_memory = torch.cuda.get_device_properties(0).total_memory / (1024 ** 3)
        print(f"✓ GPU: {gpu_name} ({gpu_memory:.1f} GB)")
    else:
        print("⚠ No CUDA GPU detected - will use CPU (slower)")

    # Check required directories
    required_dirs = [
        "api/services",
        "api/models",
        "api/config"
    ]

    for dir_path in required_dirs:
        if os.path.exists(dir_path):
            print(f"✓ Found directory: {dir_path}")
        else:
            print(f"✗ Missing directory: {dir_path}")
            return False

    return True


if __name__ == "__main__":
    print("MATCH SIMULATION PERFORMANCE TESTER")
    print("=" * 50)

    # Check system setup
    if not check_system_setup():
        print("\n✗ System setup check failed!")
        print("Please ensure you're running from the correct directory")
        sys.exit(1)

    print("\n✓ System setup looks good!")
    print("\nStarting performance tests...")
    print("This will test the optimized text generation performance.")

    # Run tests
    tester = SimplePerformanceTest()
    tester.test_text_generation_performance()

    print(f"\n✓ Performance testing completed!")
    print("Check the results above to see the optimization impact.")

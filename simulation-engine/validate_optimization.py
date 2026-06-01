#!/usr/bin/env python3
"""
Validation Script - Test the optimized simulation service
"""

import asyncio
import os
import sys
import time

from api.models.schemas import MatchRequest
from api.services.optimized_simulation_service import optimized_simulation_service


async def test_optimized_service():
    """Test the optimized simulation service"""
    print("🧪 Testing Optimized Simulation Service")
    print("=" * 50)

    try:
        # Initialize the service
        print("📋 Initializing optimized service...")
        await optimized_simulation_service.initialize()
        print("✅ Service initialized successfully")

        # Create a test request
        test_request = MatchRequest(
            match_id=12345,
            home_team_id=2,
            away_team_id=7,
            home_team_name="Barcelona",
            away_team_name="Real Madrid",
            home_team_season="2020/2021",
            away_team_season="2020/2021",
            num_tokens_to_generate=400,  # Small test
            max_length=512,
            temperature=0.7,
            top_p=0.9,
            top_k=50
        )

        print(f"\n🏈 Testing simulation:")
        print(f"  Teams: {test_request.home_team_name} vs {test_request.away_team_name}")
        print(f"  Tokens to generate: {test_request.num_tokens_to_generate}")

        # Run simulation
        sim_id = f"validation_test_{int(time.time())}"
        start_time = time.time()

        print(f"\n🚀 Starting simulation {sim_id}...")
        await optimized_simulation_service.process_match_simulation_optimized(sim_id, test_request)

        end_time = time.time()
        total_time = end_time - start_time

        print(f"✅ Simulation completed in {total_time:.2f} seconds")

        # Check status
        status = optimized_simulation_service.get_simulation_status(sim_id)
        if status:
            print(f"\n📊 Final Status:")
            print(f"  Status: {status.status}")
            print(f"  Progress: {status.progress}%")
            if hasattr(status, 'events_count'):
                print(f"  Events generated: {status.events_count}")
            if hasattr(status, 'result_path'):
                print(f"  Result saved to: {status.result_path}")

        # Test status methods
        all_statuses = optimized_simulation_service.get_all_simulation_statuses()
        print(f"\n📝 Total simulations tracked: {len(all_statuses)}")

        print("\n✅ All tests passed successfully!")
        return True

    except Exception as e:
        print(f"\n❌ Test failed with error: {str(e)}")
        import traceback
        traceback.print_exc()
        return False

    finally:
        # Cleanup
        try:
            await optimized_simulation_service.cleanup()
            print("\n🧹 Cleanup completed")
        except Exception as e:
            print(f"\n⚠️  Cleanup error: {str(e)}")


async def test_components():
    """Test individual components"""
    print("\n🔧 Testing Individual Components")
    print("-" * 30)

    try:
        # Test model resources initialization
        print("1️⃣ Testing OptimizedModelResources...")
        from api.services.optimized_simulation_service import OptimizedModelResources

        resources = OptimizedModelResources()
        print("   ✅ Model resources initialized")

        # Test bad words caching
        print("2️⃣ Testing bad words caching...")
        bad_words_ids = resources.get_cached_bad_words_ids("Barcelona_2021", "Real Madrid_2021")
        print(f"   ✅ Cached {len(bad_words_ids)} bad word patterns")

        # Test GPU memory cleanup
        print("3️⃣ Testing GPU memory cleanup...")
        resources.cleanup_gpu_memory()
        print("   ✅ GPU memory cleanup completed")

        print("\n✅ All component tests passed!")
        return True

    except Exception as e:
        print(f"\n❌ Component test failed: {str(e)}")
        import traceback
        traceback.print_exc()
        return False


async def main():
    """Main validation function"""
    print("🎯 OPTIMIZED SIMULATION SERVICE VALIDATION")
    print("=" * 60)

    # Test components first
    component_test_passed = await test_components()

    if component_test_passed:
        # Test full service
        service_test_passed = await test_optimized_service()

        if service_test_passed:
            print("\n🎉 ALL VALIDATION TESTS PASSED!")
            print("The optimized simulation service is ready for use.")
        else:
            print("\n❌ Service validation failed")
            sys.exit(1)
    else:
        print("\n❌ Component validation failed")
        sys.exit(1)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n⚠️  Validation interrupted by user")
    except Exception as e:
        print(f"\n💥 Unexpected error: {str(e)}")
        sys.exit(1)

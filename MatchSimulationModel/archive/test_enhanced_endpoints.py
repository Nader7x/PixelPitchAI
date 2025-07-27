#!/usr/bin/env python3
"""
Test script for the enhanced simulation result endpoints
Demonstrates webhooks, waiting, and streaming capabilities
"""

import aiohttp
import asyncio
import json
import time
from datetime import datetime

BASE_URL = "http://localhost:8000"
API_KEY = "REDACTED_TEST_KEY"

headers = {
    "X-API-Key": API_KEY,
    "Content-Type": "application/json"
}


async def test_webhook_endpoint():
    """Test the webhook registration functionality"""
    print("🔗 Testing Webhook Endpoint")
    print("=" * 50)

    # First, start a match simulation
    match_request = {
        "home_team_id": 1,
        "away_team_id": 2,
        "home_team_name": "Real Madrid",
        "away_team_name": "Barcelona",
        "home_team_season": "2020/2021",
        "away_team_season": "2020/2021",
        "match_id": 999001,
        "num_tokens_to_generate": 1000,  # Small for testing
        "temperature": 0.7
    }

    async with aiohttp.ClientSession() as session:
        # Start simulation
        async with session.post(f"{BASE_URL}/startMatch", json=match_request, headers=headers) as response:
            if response.status == 200:
                data = await response.json()
                simulation_id = data["simulation_id"]
                print(f"✅ Started simulation: {simulation_id}")
            else:
                print(f"❌ Failed to start simulation: {response.status}")
                return

        # Register a webhook (using a dummy URL for testing)
        webhook_request = {
            "webhook_url": "https://httpbin.org/post",
            "webhook_secret": "my_secret_key"
        }

        async with session.post(
                f"{BASE_URL}/simulations/{simulation_id}/webhook",
                json=webhook_request,
                headers=headers
        ) as response:
            if response.status == 200:
                data = await response.json()
                print(f"✅ Webhook registered: {data['message']}")
            else:
                print(f"❌ Failed to register webhook: {response.status}")
                text = await response.text()
                print(f"Error: {text}")

        print(f"📝 Simulation {simulation_id} will trigger webhook when complete")
        return simulation_id


async def test_wait_endpoint():
    """Test the waiting endpoint functionality"""
    print("\n⏳ Testing Wait Endpoint")
    print("=" * 50)

    # Start a match simulation
    match_request = {
        "home_team_id": 3,
        "away_team_id": 4,
        "home_team_name": "Atletico Madrid",
        "away_team_name": "Sevilla",
        "home_team_season": "2020/2021",
        "away_team_season": "2020/2021",
        "match_id": 999002,
        "num_tokens_to_generate": 2000,  # Small for testing
        "temperature": 0.7
    }

    async with aiohttp.ClientSession() as session:
        # Start simulation
        async with session.post(f"{BASE_URL}/startMatch", json=match_request, headers=headers) as response:
            if response.status == 200:
                data = await response.json()
                simulation_id = data["simulation_id"]
                print(f"✅ Started simulation: {simulation_id}")
            else:
                print(f"❌ Failed to start simulation: {response.status}")
                return

        # Wait for completion with timeout
        print("⏳ Waiting for simulation to complete (timeout: 60 seconds)...")
        start_time = time.time()

        async with session.get(
                f"{BASE_URL}/simulationResult/{simulation_id}/wait",
                params={"timeout_seconds": 60, "poll_interval": 2.0},
                headers=headers
        ) as response:
            if response.status == 200:
                data = await response.json()
                elapsed = time.time() - start_time

                if data.get("status") == "completed":
                    print(f"✅ Simulation completed in {elapsed:.1f} seconds!")
                    print(f"📄 Content length: {len(data.get('content', ''))}")
                    print(f"⏱️  API waited: {data.get('waited_seconds')} seconds")
                elif data.get("status") == "timeout":
                    print(f"⏰ Simulation timed out after {data.get('waited_seconds')} seconds")
                    print(f"📊 Current status: {data.get('current_simulation_status')}")
                    print(f"📈 Current progress: {data.get('current_progress')}%")
                elif data.get("status") == "failed":
                    print(f"❌ Simulation failed: {data.get('error_message')}")

            else:
                print(f"❌ Wait endpoint failed: {response.status}")
                text = await response.text()
                print(f"Error: {text}")


async def test_stream_endpoint():
    """Test the Server-Sent Events streaming endpoint"""
    print("\n📡 Testing Stream Endpoint (SSE)")
    print("=" * 50)

    # Start a match simulation
    match_request = {
        "home_team_id": 5,
        "away_team_id": 6,
        "home_team_name": "Valencia",
        "away_team_name": "Villarreal",
        "home_team_season": "2020/2021",
        "away_team_season": "2020/2021",
        "match_id": 999003,
        "num_tokens_to_generate": 1500,  # Small for testing
        "temperature": 0.7
    }

    async with aiohttp.ClientSession() as session:
        # Start simulation
        async with session.post(f"{BASE_URL}/startMatch", json=match_request, headers=headers) as response:
            if response.status == 200:
                data = await response.json()
                simulation_id = data["simulation_id"]
                print(f"✅ Started simulation: {simulation_id}")
            else:
                print(f"❌ Failed to start simulation: {response.status}")
                return

        # Stream the results
        print("📡 Opening SSE stream...")

        try:
            async with session.get(
                    f"{BASE_URL}/simulationResult/{simulation_id}/stream",
                    headers=headers
            ) as response:
                if response.status == 200:
                    print("✅ Connected to SSE stream")

                    async for line in response.content:
                        line = line.decode('utf-8').strip()
                        if line:
                            if line.startswith('event:'):
                                event_type = line.split(':', 1)[1].strip()
                                print(f"📨 Event: {event_type}")
                            elif line.startswith('data:'):
                                data_str = line.split(':', 1)[1].strip()
                                try:
                                    data = json.loads(data_str)
                                    if event_type == 'status':
                                        print(f"  📊 Status: {data.get('status')} ({data.get('progress')}%)")
                                    elif event_type == 'result':
                                        print(
                                            f"  📄 Result received! Content preview: {data.get('content', '')[:100]}...")
                                    elif event_type == 'error':
                                        print(
                                            f"  ❌ Error: {data.get('error', data.get('error_message', 'Unknown error'))}")
                                    elif event_type == 'complete':
                                        print(f"  ✅ Stream completed for {data.get('simulation_id')}")
                                        break
                                except json.JSONDecodeError:
                                    print(f"  📝 Raw data: {data_str}")
                else:
                    print(f"❌ Stream failed: {response.status}")
                    text = await response.text()
                    print(f"Error: {text}")

        except Exception as e:
            print(f"❌ Stream error: {str(e)}")


async def test_traditional_endpoint():
    """Test the traditional (immediate) endpoint for comparison"""
    print("\n🔄 Testing Traditional Endpoint (for comparison)")
    print("=" * 50)

    # Start a match simulation
    match_request = {
        "home_team_id": 7,
        "away_team_id": 8,
        "home_team_name": "Real Betis",
        "away_team_name": "Athletic Club",
        "home_team_season": "2020/2021",
        "away_team_season": "2020/2021",
        "match_id": 999004,
        "num_tokens_to_generate": 1000,  # Small for testing
        "temperature": 0.7
    }

    async with aiohttp.ClientSession() as session:
        # Start simulation
        async with session.post(f"{BASE_URL}/startMatch", json=match_request, headers=headers) as response:
            if response.status == 200:
                data = await response.json()
                simulation_id = data["simulation_id"]
                print(f"✅ Started simulation: {simulation_id}")
            else:
                print(f"❌ Failed to start simulation: {response.status}")
                return

        # Try traditional endpoint multiple times
        for attempt in range(10):
            print(f"🔄 Attempt {attempt + 1}: Checking traditional endpoint...")

            async with session.get(f"{BASE_URL}/simulationResult/{simulation_id}", headers=headers) as response:
                if response.status == 200:
                    data = await response.json()
                    print(f"✅ Traditional endpoint succeeded! Content length: {len(data.get('content', ''))}")
                    break
                elif response.status == 400:
                    data = await response.json()
                    print(f"⏳ Simulation not ready: {data.get('detail')}")
                    await asyncio.sleep(3)
                else:
                    print(f"❌ Unexpected error: {response.status}")
                    text = await response.text()
                    print(f"Error: {text}")
                    break
        else:
            print("⏰ Traditional endpoint timed out after 10 attempts")


async def main():
    """Run all tests"""
    print("🚀 Testing Enhanced Simulation Result Endpoints")
    print("=" * 60)
    print(f"🕒 Test started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print()

    try:
        # Test all endpoints
        await test_webhook_endpoint()
        await test_wait_endpoint()
        await test_stream_endpoint()
        await test_traditional_endpoint()

        print("\n" + "=" * 60)
        print("✅ All tests completed!")
        print(f"🕒 Test finished at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    except Exception as e:
        print(f"\n❌ Test suite failed: {str(e)}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    asyncio.run(main())

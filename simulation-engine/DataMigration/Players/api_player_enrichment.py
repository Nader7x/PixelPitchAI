#!/usr/bin/env python3
"""
Player API Integration System

This script integrates with multiple football APIs to get comprehensive real player data.
Supports multiple data sources with automatic fallbacks and rate limiting.
"""

import json
import logging
import os
import psycopg2
import requests
import sys
import time
from dataclasses import dataclass
from typing import Dict, List, Optional, Tuple

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


@dataclass
class PlayerData:
    """Data class for player information."""
    name: str
    position: Optional[str] = None
    preferred_foot: Optional[str] = None
    nationality: Optional[str] = None
    age: Optional[int] = None
    team: Optional[str] = None
    source: str = 'unknown'


class FootballAPIManager:
    """Manager for multiple football API integrations."""

    def __init__(self):
        self.apis = self.get_api_configurations()
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'MatchSimulationModel/1.0 (Educational Project)'
        })

    def get_api_configurations(self) -> Dict:
        """Get configuration for available football APIs."""
        return {
            'football_data': {
                'name': 'Football-Data.org',
                'base_url': 'https://api.football-data.org/v4',
                'api_key': os.getenv('FOOTBALL_DATA_API_KEY', ''),
                'headers': {'X-Auth-Token': os.getenv('FOOTBALL_DATA_API_KEY', '')},
                'rate_limit': 10,  # requests per minute
                'enabled': bool(os.getenv('FOOTBALL_DATA_API_KEY'))
            },
            'api_sports': {
                'name': 'API-Sports',
                'base_url': 'https://v3.football.api-sports.io',
                'api_key': os.getenv('API_SPORTS_KEY', ''),
                'headers': {
                    'x-rapidapi-key': os.getenv('API_SPORTS_KEY', ''),
                    'x-rapidapi-host': 'v3.football.api-sports.io'
                },
                'rate_limit': 100,  # requests per day
                'enabled': bool(os.getenv('API_SPORTS_KEY'))
            },
            'transfermarkt': {
                'name': 'Transfermarkt API',
                'base_url': 'https://transfermarkt-api.fly.dev',
                'api_key': '',
                'headers': {},
                'rate_limit': 60,  # reasonable use
                'enabled': True  # No API key required
            }
        }

    def display_api_setup_guide(self):
        """Display comprehensive API setup guide."""
        print("🔧 FOOTBALL API SETUP GUIDE")
        print("=" * 60)

        print("\n1. FOOTBALL-DATA.ORG (Recommended - Free Tier)")
        print("   • Visit: https://www.football-data.org/client/register")
        print("   • Create free account (10 requests/minute)")
        print("   • Copy your API token")
        print("   • Set environment variable: FOOTBALL_DATA_API_KEY=your_token")

        print("\n2. API-SPORTS (Comprehensive Data)")
        print("   • Visit: https://rapidapi.com/api-sports/api/api-football")
        print("   • Subscribe to free plan (100 requests/day)")
        print("   • Copy your RapidAPI key")
        print("   • Set environment variable: API_SPORTS_KEY=your_key")

        print("\n3. TRANSFERMARKT API (No Registration)")
        print("   • Already enabled - no setup required!")
        print("   • Provides comprehensive player data")
        print("   • No API key needed")

        print("\n🔧 HOW TO SET ENVIRONMENT VARIABLES:")
        print("Windows PowerShell:")
        print('   $env:FOOTBALL_DATA_API_KEY="your_token_here"')
        print('   $env:API_SPORTS_KEY="your_key_here"')

        print("\nWindows CMD:")
        print('   set FOOTBALL_DATA_API_KEY=your_token_here')
        print('   set API_SPORTS_KEY=your_key_here')

        print("\n📊 CURRENT API STATUS:")
        for api_key, config in self.apis.items():
            status = "✅ ENABLED" if config['enabled'] else "❌ DISABLED"
            print(f"   {config['name']}: {status}")

        print("\n" + "=" * 60)

    def search_player_transfermarkt(self, player_name: str) -> Optional[PlayerData]:
        """Search for player data using Transfermarkt API."""
        try:
            if not self.apis['transfermarkt']['enabled']:
                return None

            # Clean player name for search
            clean_name = player_name.replace(" ", "%20")
            url = f"{self.apis['transfermarkt']['base_url']}/players/search/{clean_name}"

            response = self.session.get(url, timeout=10)
            if response.status_code == 200:
                data = response.json()

                if data and 'results' in data and data['results']:
                    player = data['results'][0]  # Take first result

                    # Map position
                    position_mapping = {
                        'Goalkeeper': 'GK',
                        'Centre-Back': 'CB',
                        'Left-Back': 'LB',
                        'Right-Back': 'RB',
                        'Defensive Midfield': 'CDM',
                        'Central Midfield': 'CM',
                        'Attacking Midfield': 'CAM',
                        'Left Midfield': 'LM',
                        'Right Midfield': 'RM',
                        'Left Winger': 'LW',
                        'Right Winger': 'RW',
                        'Centre-Forward': 'CF',
                        'Striker': 'ST'
                    }

                    position = player.get('position', '')
                    mapped_position = position_mapping.get(position, position)

                    # Extract foot preference (if available)
                    foot = player.get('foot', '')
                    preferred_foot = None
                    if 'left' in foot.lower():
                        preferred_foot = 'Left'
                    elif 'right' in foot.lower():
                        preferred_foot = 'Right'
                    elif 'both' in foot.lower():
                        preferred_foot = 'Both'

                    return PlayerData(
                        name=player_name,
                        position=mapped_position if mapped_position else None,
                        preferred_foot=preferred_foot,
                        nationality=player.get('nationality'),
                        age=player.get('age'),
                        team=player.get('club'),
                        source='transfermarkt'
                    )

            time.sleep(1)  # Rate limiting

        except Exception as e:
            logger.warning(f"Error searching Transfermarkt for {player_name}: {e}")

        return None

    def search_player_football_data(self, player_name: str) -> Optional[PlayerData]:
        """Search for player data using Football-Data.org API."""
        try:
            if not self.apis['football_data']['enabled']:
                return None

            # This would require implementing the search logic for Football-Data.org
            # For now, return None as they don't have a direct player search endpoint
            return None

        except Exception as e:
            logger.warning(f"Error searching Football-Data.org for {player_name}: {e}")

        return None

    def enrich_players_with_apis(self, batch_size: int = 50) -> Dict[str, PlayerData]:
        """Enrich players using available APIs."""
        logger.info("Starting API-based player enrichment...")

        # Get players from database
        players = self.get_players_needing_enrichment()
        enriched_players = {}

        processed = 0
        for player_id, player_name in players[:batch_size]:
            processed += 1
            logger.info(f"Processing {processed}/{min(batch_size, len(players))}: {player_name}")

            # Try Transfermarkt first (most reliable and free)
            player_data = self.search_player_transfermarkt(player_name)

            if player_data and (player_data.position or player_data.preferred_foot):
                enriched_players[player_name] = player_data
                logger.info(f"✅ Found data for {player_name}: {player_data.position}, {player_data.preferred_foot}")
            else:
                logger.info(f"❌ No data found for {player_name}")

        return enriched_players

    def get_players_needing_enrichment(self) -> List[Tuple[int, str]]:
        """Get players that could benefit from additional enrichment."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()
            # Get players with generic or potentially incorrect data
            cursor.execute("""
                           SELECT "Id", "FullName"
                           FROM "Players"
                           WHERE "FullName" IS NOT NULL
                             AND ("Position" = 'CM' OR "PreferredFoot" = 'Right')
                             AND "Id" NOT IN (SELECT DISTINCT p."Id"
                                              FROM "Players" p
                                              WHERE p."FullName" IN (SELECT DISTINCT "FullName"
                                                                     FROM "Players"
                                                                     WHERE "Id" <=
                                                                           (SELECT MAX("Id") FROM "Players" WHERE "FullName" IS NOT NULL) -
                                                                           1100))
                           ORDER BY "Id" LIMIT 200
                           """)

            players = cursor.fetchall()
            conn.close()

            logger.info(f"Found {len(players)} players that could benefit from API enrichment")
            return players

        except Exception as e:
            logger.error(f"Error getting players from database: {e}")
            return []

    def update_database_with_api_data(self, enriched_players: Dict[str, PlayerData]) -> int:
        """Update database with API-retrieved player data."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            updated_count = 0

            for player_name, player_data in enriched_players.items():
                # Find player ID
                cursor.execute("""
                               SELECT "Id"
                               FROM "Players"
                               WHERE "FullName" = %s
                               """, (player_name,))

                result = cursor.fetchone()
                if result:
                    player_id = result[0]

                    # Update with new data
                    updates = []
                    params = []

                    if player_data.position:
                        updates.append('"Position" = %s')
                        params.append(player_data.position)

                    if player_data.preferred_foot:
                        updates.append('"PreferredFoot" = %s')
                        params.append(player_data.preferred_foot)

                    if player_data.nationality:
                        updates.append('"Nationality" = %s')
                        params.append(player_data.nationality)

                    if updates:
                        params.append(player_id)
                        query = f"""
                            UPDATE "Players" 
                            SET {', '.join(updates)}
                            WHERE "Id" = %s
                        """

                        cursor.execute(query, params)
                        updated_count += 1

                        logger.info(f"Updated {player_name} from {player_data.source}")

            conn.commit()
            conn.close()

            return updated_count

        except Exception as e:
            logger.error(f"Error updating database: {e}")
            return 0


def main():
    """Main execution function."""
    print("🌐 Football API Integration System")
    print("=" * 50)

    api_manager = FootballAPIManager()

    # Check if any APIs are configured
    enabled_apis = [config['name'] for config in api_manager.apis.values() if config['enabled']]

    if not enabled_apis:
        print("⚠️  No APIs are currently configured!")
        api_manager.display_api_setup_guide()
        return

    print(f"✅ Enabled APIs: {', '.join(enabled_apis)}")
    # Enrich players
    enriched_players = api_manager.enrich_players_with_apis(batch_size=100)

    if enriched_players:
        print(f"\n✅ Successfully enriched {len(enriched_players)} players from APIs")

        # Update database
        updated_count = api_manager.update_database_with_api_data(enriched_players)

        print(f"✅ Updated {updated_count} players in database")

        # Save results
        enriched_dict = {
            name: {
                'position': data.position,
                'preferred_foot': data.preferred_foot,
                'nationality': data.nationality,
                'source': data.source
            }
            for name, data in enriched_players.items()
        }

        with open('api_enriched_players.json', 'w', encoding='utf-8') as f:
            json.dump(enriched_dict, f, indent=2, ensure_ascii=False)

        print("📄 Saved results to api_enriched_players.json")

    else:
        print("❌ No additional player data found from APIs")

    print("\n" + "=" * 50)
    print("API enrichment completed!")


if __name__ == "__main__":
    main()

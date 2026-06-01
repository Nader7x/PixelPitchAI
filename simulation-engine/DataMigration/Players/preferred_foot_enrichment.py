#!/usr/bin/env python3
"""
Specialized Preferred Foot Data Enrichment

This script focuses specifically on finding preferred foot data for players
using multiple sources and heuristics.
"""

import json
import logging
import os
import psycopg2
import re
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
class FootPreferenceData:
    """Data class for foot preference information."""
    name: str
    preferred_foot: Optional[str] = None
    position: Optional[str] = None
    source: str = 'unknown'
    confidence: float = 0.0


class PreferredFootEnricher:
    """Specialized class for foot preference enrichment."""

    def __init__(self):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'MatchSimulationModel/1.0 (Educational Project)'
        })

        # Heuristics for foot preference based on position and name patterns
        self.position_foot_tendencies = {
            'LB': 'Left',  # Left-backs often left-footed
            'LWB': 'Left',  # Left wing-backs often left-footed
            'LW': 'Left',  # Left wingers often left-footed
            'LM': 'Left',  # Left midfielders often left-footed
            'LF': 'Left',  # Left forwards often left-footed
        }

        # Known left-footed player indicators (common in names/nationalities)
        self.left_foot_indicators = [
            'southpaw', 'lefty', 'left foot', 'left-foot'
        ]

    def get_players_needing_foot_data(self) -> List[Tuple[int, str, str]]:
        """Get players that need preferred foot enrichment."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            # Get players with generic foot preference or those in left-sided positions
            cursor.execute("""
                           SELECT "Id", "FullName", "Position"
                           FROM "Players"
                           WHERE "FullName" IS NOT NULL
                             AND ("PreferredFoot" = 'Right' AND "Position" IN ('LB', 'LWB', 'LW', 'LM', 'LF'))
                           ORDER BY "Id" LIMIT 50
                           """)

            players = cursor.fetchall()
            conn.close()

            logger.info(f"Found {len(players)} players in left-sided positions with 'Right' foot preference")
            return players

        except Exception as e:
            logger.error(f"Error getting players from database: {e}")
            return []

    def search_transfermarkt_detailed(self, player_name: str) -> Optional[FootPreferenceData]:
        """Search Transfermarkt with focus on foot preference."""
        try:
            clean_name = player_name.replace(" ", "%20")
            url = f"https://transfermarkt-api.fly.dev/players/search/{clean_name}"

            response = self.session.get(url, timeout=15)
            if response.status_code == 200:
                data = response.json()

                if data and 'results' in data and data['results']:
                    player = data['results'][0]

                    foot_data = FootPreferenceData(
                        name=player_name,
                        source='transfermarkt'
                    )

                    # Try to extract foot preference from various fields
                    foot_info = player.get('foot', '').lower()
                    if foot_info:
                        if 'left' in foot_info:
                            foot_data.preferred_foot = 'Left'
                            foot_data.confidence = 0.9
                        elif 'right' in foot_info:
                            foot_data.preferred_foot = 'Right'
                            foot_data.confidence = 0.9
                        elif 'both' in foot_info:
                            foot_data.preferred_foot = 'Both'
                            foot_data.confidence = 0.8

                    # Also get position for validation
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
                    foot_data.position = position_mapping.get(position, position)

                    return foot_data

            time.sleep(1)  # Rate limiting

        except Exception as e:
            logger.warning(f"Error searching Transfermarkt for {player_name}: {e}")

        return None

    def apply_position_heuristics(self, player_name: str, position: str) -> Optional[FootPreferenceData]:
        """Apply heuristics based on position to guess foot preference."""
        if position in self.position_foot_tendencies:
            return FootPreferenceData(
                name=player_name,
                preferred_foot=self.position_foot_tendencies[position],
                position=position,
                source='position_heuristic',
                confidence=0.6
            )
        return None

    def enrich_preferred_foot_data(self) -> Dict[str, FootPreferenceData]:
        """Enrich players with preferred foot data."""
        logger.info("Starting preferred foot enrichment...")

        players = self.get_players_needing_foot_data()
        enriched_players = {}

        for player_id, player_name, position in players:
            logger.info(f"Processing {player_name} ({position})")

            # Try API first
            foot_data = self.search_transfermarkt_detailed(player_name)

            if foot_data and foot_data.preferred_foot:
                enriched_players[player_name] = foot_data
                logger.info(f"✅ API: {player_name} -> {foot_data.preferred_foot} (confidence: {foot_data.confidence})")
            else:
                # Apply heuristics for left-sided positions
                heuristic_data = self.apply_position_heuristics(player_name, position)
                if heuristic_data:
                    enriched_players[player_name] = heuristic_data
                    logger.info(f"🤔 Heuristic: {player_name} -> {heuristic_data.preferred_foot} (position-based)")
                else:
                    logger.info(f"❌ No foot data found for {player_name}")

        return enriched_players

    def update_database_with_foot_data(self, enriched_players: Dict[str, FootPreferenceData]) -> int:
        """Update database with foot preference data."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            updated_count = 0

            for player_name, foot_data in enriched_players.items():
                if foot_data.preferred_foot:
                    # Find player ID
                    cursor.execute("""
                                   SELECT "Id"
                                   FROM "Players"
                                   WHERE "FullName" = %s
                                   """, (player_name,))

                    result = cursor.fetchone()
                    if result:
                        player_id = result[0]

                        # Update preferred foot
                        cursor.execute("""
                                       UPDATE "Players"
                                       SET "PreferredFoot" = %s
                                       WHERE "Id" = %s
                                       """, (foot_data.preferred_foot, player_id))

                        updated_count += 1
                        logger.info(f"Updated {player_name} foot preference: {foot_data.preferred_foot}")

            conn.commit()
            conn.close()

            return updated_count

        except Exception as e:
            logger.error(f"Error updating database: {e}")
            return 0


def main():
    """Main execution function."""
    print("👣 Preferred Foot Data Enrichment System")
    print("=" * 50)

    enricher = PreferredFootEnricher()

    # Enrich players
    enriched_players = enricher.enrich_preferred_foot_data()

    if enriched_players:
        print(f"\n✅ Found foot data for {len(enriched_players)} players")

        # Update database
        updated_count = enricher.update_database_with_foot_data(enriched_players)

        print(f"✅ Updated {updated_count} players in database")

        # Save results
        enriched_dict = {
            name: {
                'preferred_foot': data.preferred_foot,
                'position': data.position,
                'source': data.source,
                'confidence': data.confidence
            }
            for name, data in enriched_players.items()
        }

        with open('preferred_foot_enriched.json', 'w', encoding='utf-8') as f:
            json.dump(enriched_dict, f, indent=2, ensure_ascii=False)

        print("📄 Saved results to preferred_foot_enriched.json")

    else:
        print("❌ No additional foot preference data found")

    print("\n" + "=" * 50)
    print("Preferred foot enrichment completed!")


if __name__ == "__main__":
    main()

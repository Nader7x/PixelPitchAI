#!/usr/bin/env python3
"""
Enhanced Player Data Enrichment Script

This script finds and updates players with real La Liga data from known player databases.
"""

import json
import logging
import os
import psycopg2
import sys
from typing import Dict, List, Tuple

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)


class RealPlayerDataEnricher:
    """Enhanced player data enrichment using known La Liga players."""

    def __init__(self):
        self.known_players = self.get_known_la_liga_players()

    def get_known_la_liga_players(self) -> Dict:
        """Get comprehensive La Liga player database with real data."""
        logger.info("Loading known La Liga player database...")

        return {
            # Real Madrid
            'Karim Benzema': {'position': 'ST', 'preferred_foot': 'Right'},
            'Luka Modrić': {'position': 'CM', 'preferred_foot': 'Right'},
            'Luka Modric': {'position': 'CM', 'preferred_foot': 'Right'},  # Alternative spelling
            'Toni Kroos': {'position': 'CM', 'preferred_foot': 'Right'},
            'Sergio Ramos': {'position': 'CB', 'preferred_foot': 'Right'},
            'Marcelo': {'position': 'LB', 'preferred_foot': 'Left'},
            'Vinícius Júnior': {'position': 'LW', 'preferred_foot': 'Right'},
            'Vinicius Junior': {'position': 'LW', 'preferred_foot': 'Right'},
            'Eden Hazard': {'position': 'LW', 'preferred_foot': 'Right'},
            'Casemiro': {'position': 'CDM', 'preferred_foot': 'Right'},
            'Thibaut Courtois': {'position': 'GK', 'preferred_foot': 'Left'},
            'Federico Valverde': {'position': 'CM', 'preferred_foot': 'Right'},
            'Rodrygo': {'position': 'RW', 'preferred_foot': 'Right'},
            'Eduardo Camavinga': {'position': 'CM', 'preferred_foot': 'Left'},

            # Barcelona
            'Lionel Messi': {'position': 'RW', 'preferred_foot': 'Left'},
            'Antoine Griezmann': {'position': 'CF', 'preferred_foot': 'Left'},
            'Gerard Piqué': {'position': 'CB', 'preferred_foot': 'Right'},
            'Gerard Pique': {'position': 'CB', 'preferred_foot': 'Right'},
            'Jordi Alba': {'position': 'LB', 'preferred_foot': 'Left'},
            'Sergio Busquets': {'position': 'CDM', 'preferred_foot': 'Right'},
            'Philippe Coutinho': {'position': 'CAM', 'preferred_foot': 'Right'},
            'Ousmane Dembélé': {'position': 'RW', 'preferred_foot': 'Both'},
            'Ousmane Dembele': {'position': 'RW', 'preferred_foot': 'Both'},
            'Pedri': {'position': 'CM', 'preferred_foot': 'Right'},
            'Ansu Fati': {'position': 'LW', 'preferred_foot': 'Right'},
            'Marc-André ter Stegen': {'position': 'GK', 'preferred_foot': 'Right'},
            'Frenkie de Jong': {'position': 'CM', 'preferred_foot': 'Right'},
            'Memphis Depay': {'position': 'CF', 'preferred_foot': 'Right'},
            'Sergi Roberto': {'position': 'CM', 'preferred_foot': 'Right'},
            'Ronald Araújo': {'position': 'CB', 'preferred_foot': 'Right'},
            'Ronald Araujo': {'position': 'CB', 'preferred_foot': 'Right'},

            # Atlético Madrid
            'Luis Suárez': {'position': 'ST', 'preferred_foot': 'Right'},
            'Luis Suarez': {'position': 'ST', 'preferred_foot': 'Right'},
            'João Félix': {'position': 'CF', 'preferred_foot': 'Right'},
            'Joao Felix': {'position': 'CF', 'preferred_foot': 'Right'},
            'Koke': {'position': 'CM', 'preferred_foot': 'Right'},
            'Stefan Savić': {'position': 'CB', 'preferred_foot': 'Right'},
            'Stefan Savic': {'position': 'CB', 'preferred_foot': 'Right'},
            'Jan Oblak': {'position': 'GK', 'preferred_foot': 'Right'},
            'Marcos Llorente': {'position': 'CM', 'preferred_foot': 'Right'},
            'Ángel Correa': {'position': 'CF', 'preferred_foot': 'Right'},
            'Angel Correa': {'position': 'CF', 'preferred_foot': 'Right'},

            # Sevilla
            'Youssef En-Nesyri': {'position': 'ST', 'preferred_foot': 'Right'},
            'Jesús Navas': {'position': 'RB', 'preferred_foot': 'Right'},
            'Jesus Navas': {'position': 'RB', 'preferred_foot': 'Right'},
            'Ivan Rakitić': {'position': 'CM', 'preferred_foot': 'Right'},
            'Ivan Rakitic': {'position': 'CM', 'preferred_foot': 'Right'},
            'Yassine Bounou': {'position': 'GK', 'preferred_foot': 'Right'},

            # Real Sociedad
            'Mikel Oyarzabal': {'position': 'LW', 'preferred_foot': 'Left'},
            'Alexander Isak': {'position': 'ST', 'preferred_foot': 'Right'},
            'David Silva': {'position': 'CAM', 'preferred_foot': 'Left'},
            'Martin Ødegaard': {'position': 'CAM', 'preferred_foot': 'Left'},
            'Martin Odegaard': {'position': 'CAM', 'preferred_foot': 'Left'},

            # Villarreal
            'Gerard Moreno': {'position': 'ST', 'preferred_foot': 'Left'},
            'Pau Torres': {'position': 'CB', 'preferred_foot': 'Left'},
            'Dani Parejo': {'position': 'CM', 'preferred_foot': 'Right'},
            'Samuel Chukwueze': {'position': 'RW', 'preferred_foot': 'Left'},

            # Real Betis
            'Nabil Fekir': {'position': 'CAM', 'preferred_foot': 'Left'},
            'Sergio Canales': {'position': 'CM', 'preferred_foot': 'Right'},
            'Borja Iglesias': {'position': 'ST', 'preferred_foot': 'Right'},

            # Valencia
            'Carlos Soler': {'position': 'CM', 'preferred_foot': 'Right'},
            'José Gayà': {'position': 'LB', 'preferred_foot': 'Left'},
            'Jose Gaya': {'position': 'LB', 'preferred_foot': 'Left'},
            'Gonçalo Guedes': {'position': 'LW', 'preferred_foot': 'Right'},
            'Goncalo Guedes': {'position': 'LW', 'preferred_foot': 'Right'},

            # Athletic Bilbao
            'Iñaki Williams': {'position': 'ST', 'preferred_foot': 'Right'},
            'Inaki Williams': {'position': 'ST', 'preferred_foot': 'Right'},
            'Nico Williams': {'position': 'LW', 'preferred_foot': 'Right'},
            'Dani García': {'position': 'CDM', 'preferred_foot': 'Right'},
            'Dani Garcia': {'position': 'CDM', 'preferred_foot': 'Right'},

            # Celta Vigo
            'Iago Aspas': {'position': 'ST', 'preferred_foot': 'Left'},
            'Denis Suárez': {'position': 'CM', 'preferred_foot': 'Right'},
            'Denis Suarez': {'position': 'CM', 'preferred_foot': 'Right'},

            # Other notable players
            'Raúl de Tomás': {'position': 'ST', 'preferred_foot': 'Right'},
            'Raul de Tomas': {'position': 'ST', 'preferred_foot': 'Right'},
            'Mauro Arambarri': {'position': 'CM', 'preferred_foot': 'Right'},
            'Allan Nyom': {'position': 'RB', 'preferred_foot': 'Right'},
            'Lucas Vázquez': {'position': 'RW', 'preferred_foot': 'Right'},
            'Lucas Vazquez': {'position': 'RW', 'preferred_foot': 'Right'},
            'Marco Asensio': {'position': 'RW', 'preferred_foot': 'Left'},
            'Dani Carvajal': {'position': 'RB', 'preferred_foot': 'Right'},
            'Nacho': {'position': 'CB', 'preferred_foot': 'Right'},
            'Ferran Torres': {'position': 'RW', 'preferred_foot': 'Right'},
            'Gavi': {'position': 'CM', 'preferred_foot': 'Right'},
            'Nico González': {'position': 'CM', 'preferred_foot': 'Right'},
            'Nico Gonzalez': {'position': 'CM', 'preferred_foot': 'Right'},
            'Mateo Kovačić': {'position': 'CM', 'preferred_foot': 'Right'},
            'Mateo Kovacic': {'position': 'CM', 'preferred_foot': 'Right'}
        }

    def get_players_from_database(self) -> List[Tuple[int, str]]:
        """Get all players from database."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            cursor.execute("""
                           SELECT "Id", "FullName"
                           FROM "Players"
                           WHERE "FullName" IS NOT NULL
                           ORDER BY "Id"
                           """)

            players = cursor.fetchall()
            conn.close()

            logger.info(f"Found {len(players)} players in database")
            return players

        except Exception as e:
            logger.error(f"Error getting players from database: {e}")
            return []

    def find_matching_players(self) -> Dict:
        """Find players in database that match known players."""
        db_players = self.get_players_from_database()
        enriched_data = {}

        for player_id, full_name in db_players:
            if full_name in self.known_players:
                player_data = self.known_players[full_name]
                enriched_data[full_name] = {
                    'player_id': player_id,
                    'name': full_name,
                    'position': player_data['position'],
                    'preferred_foot': player_data['preferred_foot'],
                    'source': 'known_database'
                }
                logger.info(f"Found match: {full_name} -> {player_data}")

        return enriched_data

    def update_database(self, enriched_data: Dict) -> int:
        """Update database with real player data."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            updated_count = 0

            for player_name, data in enriched_data.items():
                player_id = data['player_id']
                position = data['position']
                preferred_foot = data['preferred_foot']

                cursor.execute("""
                               UPDATE "Players"
                               SET "Position"      = %s,
                                   "PreferredFoot" = %s
                               WHERE "Id" = %s
                               """, (position, preferred_foot, player_id))

                updated_count += 1
                logger.info(f"Updated {player_name} (ID: {player_id}): {position}, {preferred_foot}")

            conn.commit()
            conn.close()

            return updated_count

        except Exception as e:
            logger.error(f"Error updating database: {e}")
            return 0


def main():
    """Main execution function."""
    print("🚀 Enhanced Player Data Enrichment with Real La Liga Data")
    print("=" * 70)

    enricher = RealPlayerDataEnricher()

    # Find matching players
    enriched_data = enricher.find_matching_players()

    if enriched_data:
        print(f"\n✅ Found {len(enriched_data)} players with real data")

        # Show found players
        for player_name, data in enriched_data.items():
            print(f"  - {player_name}: {data['position']}, {data['preferred_foot']}")

        # Update database
        updated_count = enricher.update_database(enriched_data)

        print(f"\n✅ Successfully updated {updated_count} players with real data!")

        # Save to file
        with open('enhanced_player_data.json', 'w', encoding='utf-8') as f:
            json.dump(enriched_data, f, indent=2, ensure_ascii=False)

        print("📄 Saved results to enhanced_player_data.json")

    else:
        print("❌ No matching players found in database")

    print("\n" + "=" * 70)
    print("Player data enhancement completed!")


if __name__ == "__main__":
    main()

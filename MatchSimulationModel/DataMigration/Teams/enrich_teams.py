#!/usr/bin/env python3
"""
Team Data Enrichment Script

This script automatically gathers and updates team information including:
- City information
- Foundation dates
- Primary colors
- Secondary colors

Uses multiple data sources:
1. Predefined data for Spanish La Liga teams
2. Web scraping as backup
3. API calls where available
"""

import logging
import os
import psycopg2
import requests
import sys
import time
from datetime import datetime

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG

# Set up logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('team_enrichment.log'),
        logging.StreamHandler()
    ]
)

logger = logging.getLogger(__name__)

# Comprehensive Spanish La Liga team data with proper short names
SPANISH_TEAMS_DATA = {
    'Barcelona': {
        'city': 'Barcelona',
        'short_name': 'BAR',
        'foundation_date': '1899-11-29',
        'primary_color': '#004D98',  # Blaugrana Blue
        'secondary_color': '#A50044'  # Blaugrana Red
    },
    'Real Madrid': {
        'city': 'Madrid',
        'short_name': 'RMA',
        'foundation_date': '1902-03-06',
        'primary_color': '#FFFFFF',  # White
        'secondary_color': '#FFD700'  # Gold
    },
    'Atlético Madrid': {
        'city': 'Madrid',
        'short_name': 'ATM',
        'foundation_date': '1903-04-26',
        'primary_color': '#CE2029',  # Red
        'secondary_color': '#FFFFFF'  # White
    },
    'Valencia': {
        'city': 'Valencia',
        'short_name': 'VAL',
        'foundation_date': '1919-03-05',
        'primary_color': '#FF6600',  # Orange
        'secondary_color': '#000000'  # Black
    },
    'Sevilla': {
        'city': 'Sevilla',
        'short_name': 'SEV',
        'foundation_date': '1890-01-25',
        'primary_color': '#D82128',  # Red
        'secondary_color': '#FFFFFF'  # White
    },
    'Villarreal': {
        'city': 'Villarreal',
        'short_name': 'VIL',
        'foundation_date': '1923-03-10',
        'primary_color': '#FFE500',  # Yellow
        'secondary_color': '#005490'  # Blue
    },
    'Real Betis': {
        'city': 'Sevilla',
        'foundation_date': '1907-09-12',
        'primary_color': '#00B26D',  # Green
        'secondary_color': '#FFFFFF'  # White
    },
    'Real Sociedad': {
        'city': 'San Sebastián',
        'foundation_date': '1909-09-07',
        'primary_color': '#004D98',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Athletic Club': {
        'city': 'Bilbao',
        'foundation_date': '1898-01-01',
        'primary_color': '#EE2523',  # Red
        'secondary_color': '#FFFFFF'  # White
    },
    'Celta Vigo': {
        'city': 'Vigo',
        'foundation_date': '1923-08-23',
        'primary_color': '#69C5E6',  # Sky Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Getafe': {
        'city': 'Getafe',
        'foundation_date': '1946-02-24',
        'primary_color': '#005490',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Granada': {
        'city': 'Granada',
        'foundation_date': '1931-04-06',
        'primary_color': '#8B0000',  # Dark Red
        'secondary_color': '#FFFFFF'  # White
    },
    'Levante UD': {
        'city': 'Valencia',
        'foundation_date': '1909-09-09',
        'primary_color': '#003875',  # Blue
        'secondary_color': '#FF0000'  # Red
    },
    'Deportivo Alavés': {
        'city': 'Vitoria-Gasteiz',
        'foundation_date': '1921-01-23',
        'primary_color': '#1E3A8A',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Osasuna': {
        'city': 'Pamplona',
        'foundation_date': '1920-10-17',
        'primary_color': '#D1001F',  # Red
        'secondary_color': '#002D62'  # Navy Blue
    },
    'Eibar': {
        'city': 'Eibar',
        'foundation_date': '1940-11-30',
        'primary_color': '#8B0000',  # Dark Red
        'secondary_color': '#1E90FF'  # Blue
    },
    'Huesca': {
        'city': 'Huesca',
        'foundation_date': '1960-01-01',
        'primary_color': '#003875',  # Blue
        'secondary_color': '#FF0000'  # Red
    },
    'Real Valladolid': {
        'city': 'Valladolid',
        'foundation_date': '1928-06-20',
        'primary_color': '#663399',  # Purple
        'secondary_color': '#FFFFFF'  # White
    },
    'Leganés': {
        'city': 'Leganés',
        'foundation_date': '1928-06-26',
        'primary_color': '#FFFFFF',  # White
        'secondary_color': '#4169E1'  # Blue
    },
    'Espanyol': {
        'city': 'Barcelona',
        'foundation_date': '1900-10-28',
        'primary_color': '#004D98',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Mallorca': {
        'city': 'Palma',
        'foundation_date': '1916-06-05',
        'primary_color': '#FF0000',  # Red
        'secondary_color': '#000000'  # Black
    },
    'Cádiz': {
        'city': 'Cádiz',
        'foundation_date': '1910-09-10',
        'primary_color': '#FFD700',  # Yellow
        'secondary_color': '#1E3A8A'  # Blue
    },
    'Elche': {
        'city': 'Elche',
        'foundation_date': '1923-05-30',
        'primary_color': '#00B26D',  # Green
        'secondary_color': '#FFFFFF'  # White
    },
    'Girona': {
        'city': 'Girona',
        'foundation_date': '1930-07-25',
        'primary_color': '#FF0000',  # Red
        'secondary_color': '#FFFFFF'  # White
    },
    'Rayo Vallecano': {
        'city': 'Madrid',
        'foundation_date': '1924-05-29',
        'primary_color': '#FFFFFF',  # White
        'secondary_color': '#FF0000'  # Red
    },
    'RC Deportivo La Coruña': {
        'city': 'A Coruña',
        'foundation_date': '1906-03-02',
        'primary_color': '#1E3A8A',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Las Palmas': {
        'city': 'Las Palmas de Gran Canaria',
        'foundation_date': '1949-08-22',
        'primary_color': '#FFD700',  # Yellow
        'secondary_color': '#1E3A8A'  # Blue
    },
    'Málaga': {
        'city': 'Málaga',
        'foundation_date': '1904-04-03',
        'primary_color': '#1E3A8A',  # Blue
        'secondary_color': '#FFFFFF'  # White
    },
    'Sporting Gijón': {
        'city': 'Gijón',
        'foundation_date': '1905-06-01',
        'primary_color': '#FF0000',  # Red
        'secondary_color': '#FFFFFF'  # White
    }
}


def normalize_team_name(name):
    """Normalize team name for lookup."""
    # Handle common variations
    name_mappings = {
        'RC Deportivo La Coruña': 'RC Deportivo La Coruña',
        'Deportivo La Coruña': 'RC Deportivo La Coruña',
        'Deportivo': 'RC Deportivo La Coruña',
        'Athletic Club': 'Athletic Club',
        'Athletic Bilbao': 'Athletic Club',
        'Real Betis Balompié': 'Real Betis',
        'Betis': 'Real Betis',
        'RCD Espanyol': 'Espanyol',
        'Espanyol Barcelona': 'Espanyol'
    }

    return name_mappings.get(name, name)


def get_team_data_from_database():
    """Get all teams from database that need enrichment."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute("""
                       SELECT "Id", "Name", "City", "FoundationDate", "PrimaryColor", "SecondaryColor"
                       FROM "Teams"
                       ORDER BY "Id"
                       """)

        teams = cursor.fetchall()
        cursor.close()
        conn.close()

        return teams
    except Exception as e:
        logger.error(f"Error getting teams from database: {e}")
        return []


def update_team_in_database(team_id, city, foundation_date, primary_color, secondary_color, short_name=None):
    """Update team information in database."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Build update query based on whether short_name is provided
        if short_name:
            cursor.execute("""
                           UPDATE "Teams"
                           SET "City"           = %s,
                               "FoundationDate" = %s,
                               "PrimaryColor"   = %s,
                               "SecondaryColor" = %s,
                               "ShortName"      = %s
                           WHERE "Id" = %s
                           """, (city, foundation_date, primary_color, secondary_color, short_name, team_id))
        else:
            cursor.execute("""
                           UPDATE "Teams"
                           SET "City"           = %s,
                               "FoundationDate" = %s,
                               "PrimaryColor"   = %s,
                               "SecondaryColor" = %s
                           WHERE "Id" = %s
                           """, (city, foundation_date, primary_color, secondary_color, team_id))

        conn.commit()
        cursor.close()
        conn.close()

        logger.info(f"Successfully updated team ID {team_id}")
        return True

    except Exception as e:
        logger.error(f"Error updating team ID {team_id}: {e}")
        return False


def get_wikipedia_team_info(team_name):
    """Get team information from Wikipedia API as backup."""
    try:
        # Try to get team info from Wikipedia
        search_url = "https://en.wikipedia.org/api/rest_v1/page/summary/" + team_name.replace(" ", "_")

        headers = {
            'User-Agent': 'TeamEnrichmentScript/1.0 (https://github.com/yourusername/project)'
        }

        response = requests.get(search_url, headers=headers, timeout=10)

        if response.status_code == 200:
            data = response.json()
            extract = data.get('extract', '')

            # Basic extraction logic for foundation date
            # This is simplified - could be enhanced with more sophisticated parsing
            foundation_info = {}
            if 'founded' in extract.lower():
                # Simple regex could be added here to extract dates
                pass

            return foundation_info

    except Exception as e:
        logger.warning(f"Could not get Wikipedia info for {team_name}: {e}")

    return {}


def enrich_team_data():
    """Main function to enrich all team data."""
    logger.info("Starting team data enrichment process")

    teams = get_team_data_from_database()
    if not teams:
        logger.error("No teams found in database")
        return

    logger.info(f"Found {len(teams)} teams to process")

    success_count = 0
    failure_count = 0

    for team in teams:
        team_id, name, current_city, current_foundation, current_primary, current_secondary = team

        logger.info(f"Processing team: {name} (ID: {team_id})")

        # Normalize team name for lookup
        normalized_name = normalize_team_name(name)

        # Get team data from predefined data or API
        team_data = SPANISH_TEAMS_DATA.get(normalized_name)

        if not team_data:
            logger.warning(f"No predefined data found for {name} (normalized: {normalized_name})")
            # Try Wikipedia as backup
            team_data = get_wikipedia_team_info(name)

            if not team_data:
                # Use default values if no data found
                team_data = {
                    'city': 'Unknown',
                    'foundation_date': '1900-01-01',  # Default foundation date
                    'primary_color': '#000000',  # Black as default
                    'secondary_color': '#FFFFFF'  # White as default
                }
                logger.warning(f"Using default values for {name}")

        # Update team in database
        success = update_team_in_database(
            team_id,
            team_data['city'],
            team_data['foundation_date'],
            team_data['primary_color'],
            team_data['secondary_color']
        )

        if success:
            success_count += 1
            logger.info(f"✅ Successfully enriched {name}")
        else:
            failure_count += 1
            logger.error(f"❌ Failed to enrich {name}")

        # Small delay to be respectful to APIs
        time.sleep(0.5)

    logger.info(f"Team enrichment completed!")
    logger.info(f"✅ Successful updates: {success_count}")
    logger.info(f"❌ Failed updates: {failure_count}")
    logger.info(f"📊 Success rate: {(success_count / len(teams) * 100):.1f}%")


def validate_enrichment():
    """Validate that enrichment was successful."""
    logger.info("Validating team enrichment...")

    teams = get_team_data_from_database()
    enriched_count = 0

    for team in teams:
        team_id, name, city, foundation_date, primary_color, secondary_color = team

        is_enriched = (
                city and city != 'Unknown' and
                foundation_date and foundation_date != datetime(2000, 1, 1).date() and
                primary_color and primary_color != 'NULL' and
                secondary_color and secondary_color != 'NULL'
        )

        if is_enriched:
            enriched_count += 1
        else:
            logger.warning(f"Team {name} (ID: {team_id}) may need additional enrichment")

    logger.info(f"Validation complete: {enriched_count}/{len(teams)} teams fully enriched")
    return enriched_count, len(teams)


if __name__ == "__main__":
    try:
        # Run enrichment
        enrich_team_data()

        # Validate results
        print("\n" + "=" * 60)
        validate_enrichment()

        print("\n" + "=" * 60)
        print("Team enrichment process completed!")
        print("Check team_enrichment.log for detailed logs")

    except KeyboardInterrupt:
        logger.info("Process interrupted by user")
    except Exception as e:
        logger.error(f"Unexpected error: {e}")

#!/usr/bin/env python3
"""
Update teams with proper short names (abbreviations)
Spanish La Liga team short names based on official abbreviations
"""

import logging
import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG

# Set up logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)

# Official Spanish La Liga team short names
TEAM_SHORTNAMES = {
    'Barcelona': 'BAR',
    'Real Madrid': 'RMA',
    'Atlético Madrid': 'ATM',
    'Valencia': 'VAL',
    'Sevilla': 'SEV',
    'Villarreal': 'VIL',
    'Real Betis': 'BET',
    'Real Sociedad': 'RSO',
    'Athletic Club': 'ATH',
    'Celta Vigo': 'CEL',
    'Getafe': 'GET',
    'Granada': 'GRA',
    'Levante UD': 'LEV',
    'Deportivo Alavés': 'ALA',
    'Osasuna': 'OSA',
    'Eibar': 'EIB',
    'Huesca': 'HUE',
    'Real Valladolid': 'VLL',
    'Leganés': 'LEG',
    'Espanyol': 'ESP',
    'Mallorca': 'MLL',
    'Cádiz': 'CAD',
    'Elche': 'ELC',
    'Girona': 'GIR',
    'Rayo Vallecano': 'RAY',
    'RC Deportivo La Coruña': 'DEP',
    'Las Palmas': 'LPA',
    'Málaga': 'MAL',
    'Sporting Gijón': 'SPO'
}


def update_team_shortnames():
    """Update all teams with proper short names."""
    logger.info("Starting team short name updates")

    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Get all teams
        cursor.execute('SELECT "Id", "Name", "ShortName" FROM "Teams" ORDER BY "Id"')
        teams = cursor.fetchall()

        updated_count = 0

        for team_id, team_name, current_shortname in teams:
            # Get the proper short name
            new_shortname = TEAM_SHORTNAMES.get(team_name)

            if new_shortname:
                if current_shortname != new_shortname:
                    # Update the short name
                    cursor.execute(
                        'UPDATE "Teams" SET "ShortName" = %s WHERE "Id" = %s',
                        (new_shortname, team_id)
                    )
                    logger.info(f"Updated {team_name}: '{current_shortname}' → '{new_shortname}'")
                    updated_count += 1
                else:
                    logger.info(f"No change needed for {team_name}: '{current_shortname}'")
            else:
                logger.warning(f"No short name mapping found for: {team_name}")

        # Commit changes
        conn.commit()
        cursor.close()
        conn.close()

        logger.info(f"Short name update completed! Updated {updated_count} teams.")
        return updated_count

    except Exception as e:
        logger.error(f"Error updating short names: {e}")
        return 0


def validate_shortnames():
    """Validate that all teams have proper short names."""
    logger.info("Validating team short names...")

    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute('SELECT "Id", "Name", "ShortName" FROM "Teams" ORDER BY "Id"')
        teams = cursor.fetchall()

        print("\nValidation Results:")
        print("ID | Name                     | ShortName | Status")
        print("-" * 55)

        valid_count = 0

        for team_id, team_name, shortname in teams:
            expected_shortname = TEAM_SHORTNAMES.get(team_name)

            if shortname == expected_shortname:
                status = "✅"
                valid_count += 1
            elif shortname and expected_shortname:
                status = "❌ Wrong"
            elif not shortname:
                status = "❌ Missing"
            else:
                status = "⚠️  Unknown"

            print(f"{team_id:2} | {team_name:<24} | {shortname:<9} | {status}")

        cursor.close()
        conn.close()

        print(f"\nValidation Summary: {valid_count}/{len(teams)} teams have correct short names")
        return valid_count == len(teams)

    except Exception as e:
        logger.error(f"Error validating short names: {e}")
        return False


if __name__ == "__main__":
    # Update short names
    updated = update_team_shortnames()

    print("\n" + "=" * 60)

    # Validate results
    validation_success = validate_shortnames()

    print("\n" + "=" * 60)
    if validation_success:
        print("🎉 All teams now have proper short names!")
    else:
        print("⚠️  Some teams still need short name corrections")

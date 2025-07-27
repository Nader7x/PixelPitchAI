#!/usr/bin/env python3
"""
Check current teams in the target database to see what needs enrichment
"""
import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_teams():
    """Check current teams and their enrichment status."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()
        # Get all teams with current data
        cursor.execute("""
                       SELECT "Id", "Name", "ShortName", "City", "FoundationDate", "PrimaryColor", "SecondaryColor"
                       FROM "Teams"
                       ORDER BY "Id"
                       """)

        teams = cursor.fetchall()
        print(f'Current teams in database ({len(teams)} total):')
        print('-' * 120)
        print(f'{"ID":<4} {"Name":<25} {"Short":<6} {"City":<15} {"Founded":<12} {"Primary":<15} {"Secondary":<15}')
        print('-' * 120)

        teams_needing_enrichment = []

        for team in teams:
            team_id, name, short_name, city, foundation_date, primary_color, secondary_color = team
            # Check if enrichment is needed
            needs_enrichment = (
                    city is None or
                    foundation_date is None or
                    primary_color is None or
                    secondary_color is None or
                    short_name is None
            )

            if needs_enrichment:
                teams_needing_enrichment.append({
                    'id': team_id,
                    'name': name,
                    'short_name': short_name,
                    'city': city,
                    'foundation_date': foundation_date,
                    'primary_color': primary_color,
                    'secondary_color': secondary_color
                })

            # Display team info
            short_str = short_name or "NULL"
            city_str = city or "NULL"
            foundation_str = str(foundation_date) if foundation_date else "NULL"
            primary_str = primary_color or "NULL"
            secondary_str = secondary_color or "NULL"

            status = "❌" if needs_enrichment else "✅"
            print(
                f'{team_id:<4} {name:<25} {short_str:<6} {city_str:<15} {foundation_str:<12} {primary_str:<15} {secondary_str:<15} {status}')

        cursor.close()
        conn.close()

        print(f'\nSummary:')
        print(f'- Total teams: {len(teams)}')
        print(f'- Teams needing enrichment: {len(teams_needing_enrichment)}')
        print(f'- Teams fully enriched: {len(teams) - len(teams_needing_enrichment)}')

        return teams_needing_enrichment

    except Exception as e:
        print(f'Error checking teams: {e}')
        return []


if __name__ == "__main__":
    check_teams()

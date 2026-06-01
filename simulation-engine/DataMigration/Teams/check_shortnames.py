#!/usr/bin/env python3
"""
Check current ShortName status for teams
"""
import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_shortnames():
    """Check current ShortName values."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute("""
                       SELECT "Id", "Name", "ShortName"
                       FROM "Teams"
                       ORDER BY "Id"
                       """)

        teams = cursor.fetchall()
        print('Current ShortName status:')
        print('ID | Name                     | ShortName')
        print('-' * 45)

        missing_shortname_count = 0
        for team in teams:
            team_id, name, shortname = team
            shortname_display = shortname if shortname else "NULL"
            if not shortname:
                missing_shortname_count += 1
            print(f'{team_id:2} | {name:<24} | {shortname_display}')

        cursor.close()
        conn.close()

        print(f'\nSummary: {missing_shortname_count}/{len(teams)} teams missing ShortName')
        return missing_shortname_count

    except Exception as e:
        print(f'Error checking shortnames: {e}')
        return 0


if __name__ == "__main__":
    check_shortnames()

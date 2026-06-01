#!/usr/bin/env python3
"""
Update Database with Real Player Data
Generated from match file analysis
"""

import json
import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def update_database_with_real_data():
    """Update database with extracted real player data."""

    # Load real player data
    with open('real_player_data_extracted.json', 'r', encoding='utf-8') as f:
        real_data = json.load(f)

    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        updated_count = 0

        for player_name, data in real_data.items():
            position = data.get('position')
            preferred_foot = data.get('preferred_foot')

            if position or preferred_foot:
                # Try to find player by name
                cursor.execute("""
                               SELECT "Id", "FullName", "KnownName"
                               FROM "Players"
                               WHERE "FullName" ILIKE %s
                                  OR "KnownName" ILIKE %s
                                  OR "FullName" ILIKE %s
                                  OR "KnownName" ILIKE %s
                               """, (f'%{player_name}%', f'%{player_name}%',
                                     f'{player_name}%', f'{player_name}%'))

                matches = cursor.fetchall()

                if matches:
                    # Update the first match (you might want to be more specific)
                    player_id = matches[0][0]

                    cursor.execute("""
                                   UPDATE "Players"
                                   SET "Position"      = COALESCE(%s, "Position"),
                                       "PreferredFoot" = COALESCE(%s, "PreferredFoot"),
                                       "UpdatedAt"     = CURRENT_TIMESTAMP
                                   WHERE "Id" = %s
                                   """, (position, preferred_foot, player_id))

                    updated_count += 1
                    print(f"Updated {player_name}: {position}, {preferred_foot}")

        conn.commit()
        cursor.close()
        conn.close()

        print(f"\n✅ Updated {updated_count} players with REAL data!")

    except Exception as e:
        print(f"❌ Database update error: {e}")


if __name__ == "__main__":
    update_database_with_real_data()

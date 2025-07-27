#!/usr/bin/env python3
"""
Script to examine the Teams table structure
"""

import psycopg2

from db_config import TARGET_DB_CONFIG


def examine_teams_table():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print('=== TEAMS TABLE STRUCTURE ===')
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable
                       FROM information_schema.columns
                       WHERE table_name = 'Teams'
                       ORDER BY ordinal_position
                       """)

        columns = cursor.fetchall()
        for col in columns:
            nullable = 'NULL' if col[2] == 'YES' else 'NOT NULL'
            print(f'  {col[0]:<20} - {col[1]:<20} ({nullable})')

        print('\n=== SAMPLE TEAMS DATA ===')
        cursor.execute('SELECT "Id", "Name" FROM "Teams" LIMIT 10')
        teams = cursor.fetchall()
        for team in teams:
            print(f'  {team[0]:2d}. {team[1]}')

        print('\n=== TOTAL TEAMS ===')
        cursor.execute('SELECT COUNT(*) FROM "Teams"')
        total = cursor.fetchone()[0]
        print(f'Total teams: {total}')

        cursor.close()
        conn.close()

    except Exception as e:
        print(f'Error: {e}')


if __name__ == "__main__":
    examine_teams_table()

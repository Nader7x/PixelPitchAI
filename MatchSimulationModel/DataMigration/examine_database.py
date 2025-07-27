#!/usr/bin/env python3
"""
Script to examine all tables in the database to understand the current structure
"""

import psycopg2

from db_config import TARGET_DB_CONFIG


def examine_database():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== EXAMINING DATABASE STRUCTURE ===\n")

        # Get all tables
        cursor.execute("""
                       SELECT table_name
                       FROM information_schema.tables
                       WHERE table_schema = 'public'
                       ORDER BY table_name
                       """)

        tables = cursor.fetchall()
        print(f'Found {len(tables)} tables:')
        for table in tables:
            print(f'  - {table[0]}')

        # Check teams table if it exists
        if any('teams' in str(table) for table in tables):
            print('\n=== TEAMS TABLE STRUCTURE ===')
            cursor.execute("""
                           SELECT column_name, data_type, is_nullable, column_default
                           FROM information_schema.columns
                           WHERE table_name = 'teams'
                           ORDER BY ordinal_position
                           """)

            columns = cursor.fetchall()
            print('Teams table structure:')
            print('-' * 60)
            for col in columns:
                nullable = "NULL" if col[2] == "YES" else "NOT NULL"
                default = f" (Default: {col[3]})" if col[3] else ""
                print(f'  {col[0]:<20} - {col[1]:<15} ({nullable}){default}')

            # Get sample teams data
            cursor.execute('SELECT * FROM teams LIMIT 5')
            sample_data = cursor.fetchall()

            if sample_data:
                print('\nSample teams data:')
                print('-' * 80)
                for i, row in enumerate(sample_data, 1):
                    print(f'{i:2d}. {row}')

            # Count total teams
            cursor.execute('SELECT COUNT(*) FROM teams')
            total = cursor.fetchone()[0]
            print(f'\nTotal teams: {total}')

        # Check stadiums table
        if any('stadiums' in str(table) for table in tables):
            print('\n=== STADIUMS TABLE INFO ===')
            cursor.execute('SELECT COUNT(*) FROM stadiums')
            stadium_count = cursor.fetchone()[0]
            print(f'Total stadiums: {stadium_count}')

        cursor.close()
        conn.close()
        print('\nDatabase examination completed successfully!')

    except Exception as e:
        print(f'Error: {e}')


if __name__ == "__main__":
    examine_database()

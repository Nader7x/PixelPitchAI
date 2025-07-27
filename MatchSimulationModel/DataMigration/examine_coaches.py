#!/usr/bin/env python3
"""
Script to examine the current state of the coaches table in the database
"""

import psycopg2

from db_config import TARGET_DB_CONFIG


def examine_coaches_table():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== EXAMINING COACHES TABLE ===\n")

        # Check if coaches table exists
        cursor.execute("""
                       SELECT table_name
                       FROM information_schema.tables
                       WHERE table_schema = 'public'
                         AND table_name LIKE '%coach%'
                       """)

        coach_tables = cursor.fetchall()
        print('Coach-related tables found:', coach_tables)

        if not coach_tables:
            print("No coaches table found!")
            return

        # Get the structure of coaches table
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable, column_default
                       FROM information_schema.columns
                       WHERE table_name = 'coaches'
                       ORDER BY ordinal_position
                       """)

        columns = cursor.fetchall()
        print('\nCoaches table structure:')
        print('-' * 60)
        for col in columns:
            nullable = "NULL" if col[2] == "YES" else "NOT NULL"
            default = f" (Default: {col[3]})" if col[3] else ""
            print(f'  {col[0]:<20} - {col[1]:<15} ({nullable}){default}')

        # Get sample data from coaches table
        print('\n=== SAMPLE COACH DATA ===')
        cursor.execute('SELECT * FROM coaches LIMIT 10')
        sample_data = cursor.fetchall()

        if sample_data:
            print('\nFirst 10 coaches:')
            print('-' * 80)
            for i, row in enumerate(sample_data, 1):
                print(f'{i:2d}. {row}')
        else:
            print('No data found in coaches table')

        # Count total coaches
        cursor.execute('SELECT COUNT(*) FROM coaches')
        total = cursor.fetchone()[0]
        print(f'\nTotal coaches in table: {total}')

        # Check for missing data in key columns
        print('\n=== DATA COMPLETENESS CHECK ===')
        key_columns = ['nationality', 'dateofbirth', 'yearsofexperience', 'biography', 'coachingstyle',
                       'preferredformation']

        for column in key_columns:
            try:
                cursor.execute(f"""
                    SELECT 
                        COUNT(*) as total,
                        COUNT({column}) as filled,
                        COUNT(*) - COUNT({column}) as missing
                    FROM coaches
                """)
                total, filled, missing = cursor.fetchone()
                missing_pct = (missing / total * 100) if total > 0 else 0
                print(f'  {column:<20}: {filled:3d}/{total:3d} filled, {missing:3d} missing ({missing_pct:.1f}%)')
            except psycopg2.ProgrammingError as e:
                print(f'  {column:<20}: Column does not exist or error - {e}')

        cursor.close()
        conn.close()
        print('\nDatabase examination completed successfully!')

    except Exception as e:
        print(f'Error: {e}')


if __name__ == "__main__":
    examine_coaches_table()

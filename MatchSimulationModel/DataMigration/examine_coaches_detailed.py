#!/usr/bin/env python3
"""
Script to examine the Coaches table structure and data
"""

import psycopg2

from db_config import TARGET_DB_CONFIG


def examine_coaches_table():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== EXAMINING COACHES TABLE ===\n")

        # Get the structure of Coaches table (capital C)
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable, column_default
                       FROM information_schema.columns
                       WHERE table_name = 'Coaches'
                       ORDER BY ordinal_position
                       """)

        columns = cursor.fetchall()
        print('Coaches table structure:')
        print('-' * 70)
        for col in columns:
            nullable = "NULL" if col[2] == "YES" else "NOT NULL"
            default = f" (Default: {col[3]})" if col[3] else ""
            print(f'  {col[0]:<25} - {col[1]:<20} ({nullable}){default}')

        # Get sample data from Coaches table
        print('\n=== SAMPLE COACH DATA ===')
        cursor.execute('SELECT * FROM "Coaches" LIMIT 10')
        sample_data = cursor.fetchall()

        if sample_data:
            print('\nFirst 10 coaches:')
            print('-' * 120)
            for i, row in enumerate(sample_data, 1):
                print(f'{i:2d}. {row}')
        else:
            print('No data found in Coaches table')

        # Count total coaches
        cursor.execute('SELECT COUNT(*) FROM "Coaches"')
        total = cursor.fetchone()[0]
        print(f'\nTotal coaches in table: {total}')

        # Check for missing data in key columns
        print('\n=== DATA COMPLETENESS CHECK ===')

        # First, let's get column names properly
        column_names = [col[0] for col in columns]
        print(f'Available columns: {column_names}')

        # Look for columns that might need completion
        target_columns = []
        for col_name in column_names:
            lower_name = col_name.lower()
            if any(keyword in lower_name for keyword in
                   ['nationality', 'birth', 'experience', 'biography', 'style', 'formation']):
                target_columns.append(col_name)

        print(f'\nTarget columns for completion: {target_columns}')

        for column in target_columns:
            try:
                cursor.execute(f"""
                    SELECT 
                        COUNT(*) as total,
                        COUNT("{column}") as filled,
                        COUNT(*) - COUNT("{column}") as missing
                    FROM "Coaches"
                """)
                total_count, filled, missing = cursor.fetchone()
                missing_pct = (missing / total_count * 100) if total_count > 0 else 0
                print(f'  {column:<25}: {filled:3d}/{total_count:3d} filled, {missing:3d} missing ({missing_pct:.1f}%)')
            except psycopg2.ProgrammingError as e:
                print(f'  {column:<25}: Error - {e}')

        # Get teams with coaches
        print('\n=== TEAMS AND COACHES ===')
        cursor.execute("""
                       SELECT t."Name", c."FirstName", c."LastName"
                       FROM "Teams" t
                                LEFT JOIN "Coaches" c ON t."CoachId" = c."Id" LIMIT 10
                       """)

        team_coach_data = cursor.fetchall()
        if team_coach_data:
            print('Sample team-coach relationships:')
            print('-' * 60)
            for team, fname, lname in team_coach_data:
                coach_name = f"{fname} {lname}" if fname and lname else "No coach assigned"
                print(f'  {team:<25} - {coach_name}')

        cursor.close()
        conn.close()
        print('\nDatabase examination completed successfully!')

    except Exception as e:
        print(f'Error: {e}')


if __name__ == "__main__":
    examine_coaches_table()

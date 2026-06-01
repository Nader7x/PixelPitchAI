#!/usr/bin/env python3
"""
Inspect target database schema to understand table structures
"""
import psycopg2
from db_config import TARGET_DB_CONFIG


def inspect_table_schema(table_name):
    """Inspect schema of a specific table."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Get table structure
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable
                       FROM information_schema.columns
                       WHERE table_name = %s
                       ORDER BY ordinal_position
                       """, (table_name,))

        columns = cursor.fetchall()
        print(f'\n{table_name} table columns:')
        for col in columns:
            nullable = "NULL" if col[2] == "YES" else "NOT NULL"
            print(f'  - {col[0]} ({col[1]}) {nullable}')

        cursor.close()
        conn.close()

        return columns

    except Exception as e:
        print(f'Error inspecting {table_name}: {e}')
        return []


def main():
    """Main function to inspect all relevant tables."""
    print("Target Database Schema Inspection")
    print("=" * 50)

    tables_to_inspect = ['Competitions', 'Teams', 'Players', 'Coaches', 'Seasons']

    for table in tables_to_inspect:
        inspect_table_schema(table)

    print("\n" + "=" * 50)
    print("Schema inspection completed")


if __name__ == "__main__":
    main()

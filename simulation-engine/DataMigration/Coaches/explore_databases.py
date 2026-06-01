#!/usr/bin/env python3
"""
Script to explore database schema and find coach-related data
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def explore_database(db_config, db_name):
    """Explore database schema to find coach-related tables"""
    try:
        print(f"\n=== EXPLORING {db_name} DATABASE ===")
        conn = psycopg2.connect(**db_config)
        cursor = conn.cursor()

        # List all tables
        cursor.execute("""
                       SELECT table_name
                       FROM information_schema.tables
                       WHERE table_schema = 'public'
                       ORDER BY table_name
                       """)
        tables = cursor.fetchall()

        print(f"Found {len(tables)} tables:")
        coach_related_tables = []

        for table in tables:
            table_name = table[0]
            print(f"  - {table_name}")

            # Check if table name contains coach-related keywords
            if any(keyword in table_name.lower() for keyword in ['coach', 'manager', 'team']):
                coach_related_tables.append(table_name)

        # Examine coach-related tables in detail
        print(f"\n=== COACH-RELATED TABLES ===")
        for table_name in coach_related_tables:
            print(f"\nTable: {table_name}")

            # Get table structure
            cursor.execute(f"""
                SELECT column_name, data_type, is_nullable 
                FROM information_schema.columns 
                WHERE table_name = '{table_name}' 
                ORDER BY ordinal_position
            """)
            columns = cursor.fetchall()

            print("  Columns:")
            for col in columns:
                print(f"    - {col[0]} ({col[1]}) {'NULL' if col[2] == 'YES' else 'NOT NULL'}")

            # Get row count
            cursor.execute(f'SELECT COUNT(*) FROM "{table_name}"')
            count = cursor.fetchone()[0]
            print(f"  Row count: {count}")

            # Show sample data if there are rows
            if count > 0:
                cursor.execute(f'SELECT * FROM "{table_name}" LIMIT 3')
                sample_rows = cursor.fetchall()
                print("  Sample rows:")
                for i, row in enumerate(sample_rows):
                    print(f"    Row {i + 1}: {row}")

        conn.close()
        return coach_related_tables

    except Exception as e:
        print(f'Error exploring {db_name} database: {e}')
        return []


if __name__ == "__main__":
    # Explore both databases
    source_tables = explore_database(SOURCE_DB_CONFIG, "SOURCE")
    target_tables = explore_database(TARGET_DB_CONFIG, "TARGET")

    print(f"\n=== SUMMARY ===")
    print(f"Source coach-related tables: {source_tables}")
    print(f"Target coach-related tables: {target_tables}")

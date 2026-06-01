#!/usr/bin/env python3
"""
Check Teams Table Structure

This script checks the actual structure of the Teams table to understand
the column names and data available for stadium operations.
"""

import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_teams_table_structure():
    """Check the actual structure of the Teams table."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🏟️ TEAMS TABLE STRUCTURE ANALYSIS")
        print("=" * 40)

        # Get table structure
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable, column_default
                       FROM information_schema.columns
                       WHERE table_name = 'Teams'
                         AND table_schema = 'public'
                       ORDER BY ordinal_position;
                       """)

        columns = cursor.fetchall()

        if not columns:
            print("❌ Teams table not found!")
            return

        print("📋 TEAMS TABLE COLUMNS:")
        print("-" * 30)
        for col_name, data_type, nullable, default in columns:
            print(f"• {col_name} ({data_type}) - Nullable: {nullable}")

        # Get sample data to understand the structure
        cursor.execute('SELECT * FROM "Teams" LIMIT 5')
        sample_teams = cursor.fetchall()

        print("\n📊 SAMPLE TEAM DATA:")
        print("-" * 30)
        if sample_teams:
            # Print column headers
            col_names = [desc[0] for desc in cursor.description]
            print(" | ".join(col_names))
            print("-" * (len(" | ".join(col_names))))

            for team in sample_teams:
                print(" | ".join(str(val) if val is not None else "NULL" for val in team))

        # Get total count
        cursor.execute('SELECT COUNT(*) FROM "Teams"')
        total_teams = cursor.fetchone()[0]
        print(f"\n📈 Total Teams: {total_teams}")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"❌ Error checking table structure: {e}")


if __name__ == "__main__":
    check_teams_table_structure()

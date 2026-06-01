#!/usr/bin/env python3
"""
Check Stadium Table Structure

This script checks the current structure of the Stadiums table in the target database
and compares it with our requirements.
"""

import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_stadiums_table_structure():
    """Check the current structure of the Stadiums table."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🏟️ STADIUM TABLE STRUCTURE ANALYSIS")
        print("=" * 50)

        # Check if Stadiums table exists
        cursor.execute("""
                       SELECT EXISTS (SELECT
                                      FROM information_schema.tables
                                      WHERE table_schema = 'public'
                                        AND table_name = 'Stadiums');
                       """)

        table_exists = cursor.fetchone()[0]

        if not table_exists:
            print("❌ Stadiums table does not exist!")
            return

        print("✅ Stadiums table exists")

        # Get table structure
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable, column_default
                       FROM information_schema.columns
                       WHERE table_schema = 'public'
                         AND table_name = 'Stadiums'
                       ORDER BY ordinal_position;
                       """)

        columns = cursor.fetchall()

        print("\n📋 Current Table Structure:")
        print("-" * 30)
        for col_name, data_type, is_nullable, default_val in columns:
            nullable_str = "NULL" if is_nullable == "YES" else "NOT NULL"
            default_str = f" DEFAULT {default_val}" if default_val else ""
            print(f"   {col_name}: {data_type} {nullable_str}{default_str}")

        # Check for data
        cursor.execute('SELECT COUNT(*) FROM "Stadiums"')
        count = cursor.fetchone()[0]
        print(f"\n📊 Current records in table: {count}")

        if count > 0:
            print("\n🔍 Sample records:")
            cursor.execute('SELECT * FROM "Stadiums" LIMIT 3')
            records = cursor.fetchall()
            for i, record in enumerate(records, 1):
                print(f"   Record {i}: {record}")

        # Define required columns for our stadium system
        required_columns = {
            'Name': 'VARCHAR(255)',
            'City': 'VARCHAR(255)',
            'Capacity': 'INTEGER',
            'Surface': 'VARCHAR(100)',
            'Opened': 'INTEGER',
            'Latitude': 'DECIMAL(10, 6)',
            'Longitude': 'DECIMAL(10, 6)',
            'Architect': 'VARCHAR(255)',
            'CostMillionsEuros': 'DECIMAL(10, 2)',
            'Nickname': 'VARCHAR(255)',
            'Facilities': 'TEXT[]'
        }

        current_columns = {col[0]: col[1] for col in columns}

        print("\n🔍 COLUMN ANALYSIS:")
        print("-" * 30)

        missing_columns = []
        for req_col, req_type in required_columns.items():
            if req_col in current_columns:
                print(f"   ✅ {req_col}: Present ({current_columns[req_col]})")
            else:
                print(f"   ❌ {req_col}: Missing (need {req_type})")
                missing_columns.append((req_col, req_type))

        if missing_columns:
            print("\n⚠️ REQUIRED ACTIONS:")
            print("-" * 20)
            print("The following columns need to be added:")
            for col_name, col_type in missing_columns:
                print(f"   ADD COLUMN \"{col_name}\" {col_type}")

            print("\n💡 Recommendation: Use update_stadiums_table.py to add missing columns")
        else:
            print("\n✅ All required columns are present!")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"❌ Error checking table structure: {e}")


if __name__ == "__main__":
    check_stadiums_table_structure()

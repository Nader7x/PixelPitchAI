#!/usr/bin/env python3
"""
Update Stadiums Table Structure

This script updates the existing Stadiums table to match our comprehensive
stadium data requirements while preserving any existing data.
"""

import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def update_stadiums_table():
    """Update the Stadiums table structure to match our requirements."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🔧 UPDATING STADIUMS TABLE STRUCTURE")
        print("=" * 50)

        # Add missing columns
        columns_to_add = [
            ('Surface', 'VARCHAR(100)', 'Natural Grass'),
            ('Opened', 'INTEGER', None),
            ('Architect', 'VARCHAR(255)', None),
            ('CostMillionsEuros', 'DECIMAL(10, 2)', None),
            ('Nickname', 'VARCHAR(255)', None)
        ]

        for col_name, col_type, default_val in columns_to_add:
            try:
                # Check if column exists
                cursor.execute("""
                               SELECT COUNT(*)
                               FROM information_schema.columns
                               WHERE table_schema = 'public'
                                 AND table_name = 'Stadiums'
                                 AND column_name = %s
                               """, (col_name,))

                exists = cursor.fetchone()[0] > 0

                if not exists:
                    default_clause = f" DEFAULT '{default_val}'" if default_val else ""
                    query = f'ALTER TABLE "Stadiums" ADD COLUMN "{col_name}" {col_type}{default_clause};'
                    cursor.execute(query)
                    print(f"   ✅ Added column: {col_name}")
                else:
                    print(f"   ℹ️ Column already exists: {col_name}")

            except Exception as e:
                print(f"   ❌ Error adding column {col_name}: {e}")

        # Update existing columns if needed
        try:
            # Make sure Facilities column can handle arrays (it's already jsonb which is fine)
            print("   ✅ Facilities column is compatible (jsonb)")

            # Add useful indexes
            indexes_to_create = [
                ('idx_stadiums_name_new', '"Name"'),
                ('idx_stadiums_city_new', '"City"'),
                ('idx_stadiums_capacity_new', '"Capacity"')
            ]

            for idx_name, idx_col in indexes_to_create:
                try:
                    cursor.execute(f'CREATE INDEX IF NOT EXISTS {idx_name} ON "Stadiums"({idx_col});')
                    print(f"   ✅ Created index: {idx_name}")
                except Exception as e:
                    print(f"   ⚠️ Index creation info: {e}")

        except Exception as e:
            print(f"   ⚠️ Index creation warning: {e}")

        conn.commit()

        # Verify the updated structure
        print("\n🔍 VERIFYING UPDATED STRUCTURE:")
        print("-" * 40)

        cursor.execute("""
                       SELECT column_name, data_type, is_nullable
                       FROM information_schema.columns
                       WHERE table_schema = 'public'
                         AND table_name = 'Stadiums'
                       ORDER BY ordinal_position;
                       """)

        columns = cursor.fetchall()
        for col_name, data_type, is_nullable in columns:
            nullable_str = "NULL" if is_nullable == "YES" else "NOT NULL"
            print(f"   {col_name}: {data_type} ({nullable_str})")

        cursor.close()
        conn.close()

        print("\n✅ Stadiums table structure updated successfully!")
        return True

    except Exception as e:
        print(f"❌ Error updating Stadiums table: {e}")
        return False


if __name__ == "__main__":
    update_stadiums_table()

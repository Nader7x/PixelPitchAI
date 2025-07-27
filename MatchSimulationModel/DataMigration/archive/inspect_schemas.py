#!/usr/bin/env python3
"""
Script to inspect database schemas and table structures.
"""

import psycopg2
import psycopg2.extras
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def inspect_database_schema(config, db_name):
    """Inspect and display database schema."""
    print(f"\n{'=' * 50}")
    print(f"Database Schema: {db_name}")
    print(f"{'=' * 50}")

    try:
        conn = psycopg2.connect(**config)
        cursor = conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get all tables
        cursor.execute("""
                       SELECT table_name
                       FROM information_schema.tables
                       WHERE table_schema = 'public'
                       ORDER BY table_name
                       """)
        tables = cursor.fetchall()

        print(f"Tables found: {len(tables)}")
        for table in tables:
            print(f"  - {table['table_name']}")

            # Get columns for each table
            cursor.execute("""
                           SELECT column_name, data_type, is_nullable
                           FROM information_schema.columns
                           WHERE table_name = %s
                             AND table_schema = 'public'
                           ORDER BY ordinal_position
                           """, (table['table_name'],))
            columns = cursor.fetchall()

            for col in columns:
                nullable = "NULL" if col['is_nullable'] == 'YES' else "NOT NULL"
                print(f"    • {col['column_name']} ({col['data_type']}) {nullable}")
            print()

        # Get sequences
        cursor.execute("""
                       SELECT sequence_name
                       FROM information_schema.sequences
                       WHERE sequence_schema = 'public'
                       ORDER BY sequence_name
                       """)
        sequences = cursor.fetchall()

        print(f"Sequences found: {len(sequences)}")
        for seq in sequences:
            print(f"  - {seq['sequence_name']}")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error inspecting {db_name}: {e}")


def main():
    """Main function to inspect both databases."""
    print("Database Schema Inspector")
    print("========================")

    # Inspect source database
    inspect_database_schema(SOURCE_DB_CONFIG, "Source Database (Footex)")

    # Inspect target database  
    inspect_database_schema(TARGET_DB_CONFIG, "Target Database (Footex_Api)")


if __name__ == "__main__":
    main()

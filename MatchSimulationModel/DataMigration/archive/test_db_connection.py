"""
Test database connections script
Run this before the main migration to ensure connectivity
"""
import psycopg2
import sys


def test_connection(config, db_name):
    """Test connection to a database."""
    try:
        conn = psycopg2.connect(**config)
        cursor = conn.cursor()
        cursor.execute("SELECT version();")
        version = cursor.fetchone()[0]
        print(f"✓ Successfully connected to {db_name}")
        print(f"  PostgreSQL version: {version[:50]}...")

        cursor.close()
        conn.close()
        return True

    except Exception as e:
        print(f"✗ Failed to connect to {db_name}: {e}")
        return False


def main():
    """Test both database connections."""
    try:
        from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG

        print("Testing database connections...")
        print("=" * 50)

        source_ok = test_connection(SOURCE_DB_CONFIG, "Source DB (Footex)")
        target_ok = test_connection(TARGET_DB_CONFIG, "Target DB (Footex_Api)")

        print("=" * 50)

        if source_ok and target_ok:
            print("✓ All database connections successful!")
            print("You can now run the migration script.")
            return 0
        else:
            print("✗ Some database connections failed.")
            print("Please check your database configuration in db_config.py")
            return 1

    except ImportError:
        print("Error: Could not import database configuration.")
        print("Please update db_config.py with your database credentials.")
        return 1


if __name__ == "__main__":
    exit(main())

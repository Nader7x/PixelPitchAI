import psycopg2
from db_config import SOURCE_DB_CONFIG

conn = psycopg2.connect(**SOURCE_DB_CONFIG)
cursor = conn.cursor()

# Check structure of source tables
tables = ['competitions', 'teams', 'players', 'managers', 'seasons']

for table in tables:
    print(f"\n=== {table.upper()} TABLE ===")
    try:
        cursor.execute(
            f"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{table}' ORDER BY ordinal_position")
        columns = cursor.fetchall()

        print("Columns:")
        for col in columns:
            print(f"  - {col[0]} ({col[1]})")

        # Show sample data
        cursor.execute(f"SELECT * FROM {table} LIMIT 3")
        sample_data = cursor.fetchall()

        if sample_data:
            print("Sample data:")
            for i, row in enumerate(sample_data):
                print(f"  Row {i + 1}: {row}")
        else:
            print("  No data found")

    except Exception as e:
        print(f"  Error: {e}")

cursor.close()
conn.close()

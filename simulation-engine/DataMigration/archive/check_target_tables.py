import psycopg2
from db_config import TARGET_DB_CONFIG

conn = psycopg2.connect(**TARGET_DB_CONFIG)
cursor = conn.cursor()

# Check exact table names
cursor.execute("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
all_tables = cursor.fetchall()

print("All tables in target database:")
for table in all_tables:
    print(f"  - '{table[0]}'")

# Look specifically for competition-related tables
competition_tables = [t[0] for t in all_tables if 'competition' in t[0].lower()]
print(f"\nCompetition-related tables: {competition_tables}")

# Test if we can query the table
if competition_tables:
    table_name = competition_tables[0]
    print(f"\nTesting access to table '{table_name}':")
    try:
        cursor.execute(f'SELECT COUNT(*) FROM "{table_name}"')
        count = cursor.fetchone()[0]
        print(f"  - Table has {count} rows")

        # Get column info
        cursor.execute(
            f"SELECT column_name, data_type FROM information_schema.columns WHERE table_name = '{table_name}'")
        columns = cursor.fetchall()
        print(f"  - Columns: {columns}")

    except Exception as e:
        print(f"  - Error accessing table: {e}")

cursor.close()
conn.close()

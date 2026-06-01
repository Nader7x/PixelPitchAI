import psycopg2
from db_config import TARGET_DB_CONFIG, SOURCE_DB_CONFIG

print("=== TARGET DATABASE TABLES ===")
conn = psycopg2.connect(**TARGET_DB_CONFIG)
cursor = conn.cursor()
cursor.execute("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
tables = cursor.fetchall()
for table in tables:
    print(f"  - {table[0]}")
cursor.close()
conn.close()

print("\n=== SOURCE DATABASE TABLES ===")
conn = psycopg2.connect(**SOURCE_DB_CONFIG)
cursor = conn.cursor()
cursor.execute("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'")
tables = cursor.fetchall()
for table in tables:
    print(f"  - {table[0]}")
cursor.close()
conn.close()

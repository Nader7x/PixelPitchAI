import psycopg2
from db_config import TARGET_DB_CONFIG

conn = psycopg2.connect(**TARGET_DB_CONFIG)
cursor = conn.cursor()

cursor.execute(
    "SELECT column_name, data_type, is_nullable, column_default FROM information_schema.columns WHERE table_name = 'Competitions' ORDER BY ordinal_position")
columns = cursor.fetchall()

print('Competitions table structure:')
for col in columns:
    nullable = 'NULL' if col[2] == 'YES' else 'NOT NULL'
    default = f' DEFAULT {col[3]}' if col[3] else ''
    print(f'  {col[0]} ({col[1]}) {nullable}{default}')

cursor.close()
conn.close()

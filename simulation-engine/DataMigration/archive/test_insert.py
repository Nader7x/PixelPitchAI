import psycopg2
from db_config import TARGET_DB_CONFIG

conn = psycopg2.connect(**TARGET_DB_CONFIG)
cursor = conn.cursor()

print("Testing simple INSERT into Competitions table...")

try:
    # Test simple INSERT with Description
    cursor.execute('INSERT INTO "Competitions" ("Id", "Name", "Description") VALUES (%s, %s, %s)',
                   (1, 'Test Competition', 'Test Description'))
    conn.commit()
    print("✓ Simple INSERT with Description successful")

    # Test ON CONFLICT
    cursor.execute(
        'INSERT INTO "Competitions" ("Id", "Name", "Description") VALUES (%s, %s, %s) ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "Description" = EXCLUDED."Description"',
        (1, 'Updated Test Competition', 'Updated Description'))
    conn.commit()
    print("✓ ON CONFLICT INSERT successful")

    # Check the result
    cursor.execute('SELECT "Id", "Name", "Description" FROM "Competitions" WHERE "Id" = 1')
    result = cursor.fetchone()
    print(f"✓ Result: ID={result[0]}, Name={result[1]}, Description={result[2]}")

    # Clean up
    cursor.execute('DELETE FROM "Competitions" WHERE "Id" = 1')
    conn.commit()
    print("✓ Cleanup successful")

except Exception as e:
    print(f"❌ Error: {e}")
    conn.rollback()

cursor.close()
conn.close()

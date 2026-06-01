#!/usr/bin/env python3
"""
Update Existing Stadiums with Missing Fields

Add LastRenovation, Address, and Description to existing stadium records.
"""

import json
import os
import psycopg2
import sys
from datetime import datetime

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG
from stadium_data import get_all_stadiums


def update_stadiums_with_missing_fields():
    """Update existing stadiums with missing fields."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🏟️ UPDATING STADIUMS WITH MISSING FIELDS")
        print("=" * 50)

        # Get all stadium data
        stadium_data = get_all_stadiums()

        # Get existing stadiums from database
        cursor.execute('SELECT "Id", "Name" FROM "Stadiums" ORDER BY "Name"')
        existing_stadiums = cursor.fetchall()

        updated_count = 0

        for stadium_id, stadium_name in existing_stadiums:
            # Find matching stadium data
            team_name = None
            for team, data in stadium_data.items():
                if data['name'] == stadium_name:
                    team_name = team
                    break

            if not team_name:
                print(f"⚠️ No data found for stadium: {stadium_name}")
                continue

            data = stadium_data[team_name]

            # Prepare update fields
            last_renovation = None
            if data.get('last_renovation'):
                last_renovation = f"{data['last_renovation']}-01-01"

            address = data.get('address')
            description = data.get('description')

            # Update the stadium
            cursor.execute("""
                           UPDATE "Stadiums"
                           SET "LastRenovation" = %s,
                               "Address"        = %s,
                               "Description"    = %s
                           WHERE "Id" = %s
                           """, (last_renovation, address, description, stadium_id))

            print(f"✅ Updated {stadium_name} (ID: {stadium_id})")
            updated_count += 1

        conn.commit()
        print(f"\n🎉 Successfully updated {updated_count} stadiums!")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"❌ Error updating stadiums: {e}")
        if conn:
            conn.rollback()
            conn.close()


if __name__ == "__main__":
    update_stadiums_with_missing_fields()

#!/usr/bin/env python3
"""
Create Stadiums for La Liga Teams

This script creates stadium records for all La Liga teams using the existing
stadium table structure. Maps our comprehensive stadium data to the database schema.
"""

import json
import os
import psycopg2
import sys
from datetime import datetime

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG
from stadium_data import get_all_stadiums


def create_stadium_record(cursor, team_name, stadium_data):
    """Create a single stadium record using the existing database schema."""
    try:  # Convert opened year to date (January 1st of that year)
        built_date = f"{stadium_data['opened']}-01-01" if stadium_data['opened'] else None

        # Convert last_renovation year to date if available
        last_renovation = None
        if stadium_data.get('last_renovation'):
            last_renovation = f"{stadium_data['last_renovation']}-01-01"

        # Convert facilities list to JSON
        facilities_json = json.dumps(stadium_data['facilities']) if stadium_data['facilities'] else None

        # Insert stadium data using existing schema with all fields
        cursor.execute("""
                       INSERT INTO "Stadiums" ("Name", "City", "Country", "Capacity", "BuiltDate", "LastRenovation",
                                               "SurfaceType", "Address", "Latitude", "Longitude", "HasRoof",
                                               "Facilities", "Architect", "CostMillionsEuros", "Nickname",
                                               "Description")
                       VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING "Id";
                       """, (
                           stadium_data['name'],
                           stadium_data['city'],
                           'Spain',  # Country
                           stadium_data['capacity'],
                           built_date,
                           last_renovation,
                           stadium_data['surface'],
                           stadium_data.get('address'),
                           stadium_data['latitude'],
                           stadium_data['longitude'],
                           False,  # HasRoof - most Spanish stadiums don't have full roofs
                           facilities_json,
                           stadium_data.get('architect'),
                           stadium_data.get('cost_millions_euros'),
                           stadium_data.get('nicknamed'),
                           stadium_data.get('description')
                       ))

        stadium_id = cursor.fetchone()[0]
        return stadium_id

    except Exception as e:
        print(f"❌ Error creating stadium for {team_name}: {e}")
        return None


def update_team_stadium_links(cursor, team_stadium_mapping):
    """Update Teams table with StadiumId references."""
    try:
        print("\n🔗 Linking teams to stadiums...")

        for team_name, stadium_id in team_stadium_mapping.items():
            cursor.execute("""
                           UPDATE "Teams"
                           SET "StadiumId" = %s
                           WHERE "Name" = %s
                           """, (stadium_id, team_name))

            if cursor.rowcount > 0:
                print(f"   ✅ {team_name} → Stadium ID: {stadium_id}")
            else:
                print(f"   ⚠️ {team_name} → Team not found in database")

    except Exception as e:
        print(f"❌ Error updating team-stadium links: {e}")


def create_all_stadiums():
    """Create all stadiums for Spanish La Liga teams using existing table."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n🏟️ CREATING STADIUMS FOR LA LIGA TEAMS")
        print("=" * 60)

        # Check if Stadiums table exists
        cursor.execute("""
                       SELECT EXISTS (SELECT
                                      FROM information_schema.tables
                                      WHERE table_schema = 'public'
                                        AND table_name = 'Stadiums');
                       """)

        table_exists = cursor.fetchone()[0]
        if not table_exists:
            print("❌ Stadiums table does not exist! Please create it first.")
            return False

        print("✅ Using existing Stadiums table")

        stadium_data = get_all_stadiums()
        team_stadium_mapping = {}
        created_count = 0
        updated_count = 0

        for team_name, data in stadium_data.items():
            print(f"\n🏗️ Processing: {team_name}")
            print(f"   Stadium: {data['name']}")
            print(f"   City: {data['city']}")
            print(f"   Capacity: {data['capacity']:,}")

            # Check if stadium already exists
            cursor.execute("""
                           SELECT "Id"
                           FROM "Stadiums"
                           WHERE "Name" = %s
                             AND "City" = %s
                           """, (data['name'], data['city']))

            existing_stadium = cursor.fetchone()

            if existing_stadium:
                stadium_id = existing_stadium[0]
                print(f"   ℹ️ Stadium already exists (ID: {stadium_id})")
                updated_count += 1
            else:
                stadium_id = create_stadium_record(cursor, team_name, data)
                if stadium_id:
                    print(f"   ✅ Stadium created (ID: {stadium_id})")
                    created_count += 1
                else:
                    print(f"   ❌ Failed to create stadium")
                    continue

            team_stadium_mapping[team_name] = stadium_id

        # Update team-stadium links
        if team_stadium_mapping:
            update_team_stadium_links(cursor, team_stadium_mapping)

        conn.commit()

        print("\n" + "=" * 60)
        print("🎯 STADIUM CREATION SUMMARY")
        print("=" * 60)
        print(f"📊 Total teams processed: {len(stadium_data)}")
        print(f"🏗️ New stadiums created: {created_count}")
        print(f"ℹ️ Existing stadiums found: {updated_count}")
        print(f"🔗 Team-stadium links updated: {len(team_stadium_mapping)}")

        # Verify creation
        cursor.execute('SELECT COUNT(*) FROM "Stadiums"')
        total_stadiums = cursor.fetchone()[0]
        print(f"🏟️ Total stadiums in database: {total_stadiums}")

        cursor.close()
        conn.close()

        print("\n✅ Stadium creation process completed successfully!")
        return True

    except Exception as e:
        print(f"❌ Error in stadium creation process: {e}")
        return False


def main():
    """Main execution function."""
    print("🏟️ LA LIGA STADIUM CREATION SYSTEM")
    print("=" * 50)
    print(f"🕒 Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    success = create_all_stadiums()

    if success:
        print(f"\n🎉 Process completed successfully at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    else:
        print(f"\n💥 Process failed at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    return success


if __name__ == "__main__":
    main()

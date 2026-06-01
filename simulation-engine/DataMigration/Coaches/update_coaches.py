#!/usr/bin/env python3
"""
Script to update coaches table with real La Liga coach data
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG
from coach_data import get_all_coaches_data


def get_teams_from_database():
    """Get all teams from the database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute('SELECT "Id", "Name" FROM "Teams" ORDER BY "Name"')
        teams = {row[1]: row[0] for row in cursor.fetchall()}

        cursor.close()
        conn.close()

        return teams
    except Exception as e:
        print(f"Error getting teams: {e}")
        return {}


def clear_existing_coaches():
    """Clear all existing coaches from the database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute('DELETE FROM "Coaches"')
        deleted_count = cursor.rowcount

        conn.commit()
        cursor.close()
        conn.close()

        print(f"✅ Cleared {deleted_count} existing coaches from database")
        return True
    except Exception as e:
        print(f"❌ Error clearing coaches: {e}")
        return False


def insert_coaches_data():
    """Insert real coach data into the database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Get teams and coach data
        teams = get_teams_from_database()
        coaches_data = get_all_coaches_data()

        print(f"Database teams: {len(teams)}")
        print(f"Coach data available: {len(coaches_data)}")

        inserted_count = 0
        missing_teams = []

        for team_name, coach_data in coaches_data.items():
            if team_name in teams:
                team_id = teams[team_name]

                # Insert coach data
                cursor.execute("""
                               INSERT INTO "Coaches" ("FirstName", "LastName", "DateOfBirth", "Nationality",
                                                      "Role", "YearsOfExperience", "Biography", "TeamId",
                                                      "CoachingStyle", "PreferredFormation")
                               VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                               """, (
                                   coach_data['first_name'],
                                   coach_data['last_name'],
                                   coach_data['date_of_birth'],
                                   coach_data['nationality'],
                                   'Head Coach',
                                   coach_data['years_of_experience'],
                                   coach_data['biography'],
                                   team_id,
                                   coach_data['coaching_style'],
                                   coach_data['preferred_formation']
                               ))

                inserted_count += 1
                print(f"✅ Inserted: {coach_data['first_name']} {coach_data['last_name']} -> {team_name}")
            else:
                missing_teams.append(team_name)
                print(f"⚠️  Team not found in database: {team_name}")

        conn.commit()
        cursor.close()
        conn.close()

        print(f"\n=== INSERTION SUMMARY ===")
        print(f"Successfully inserted: {inserted_count} coaches")
        print(f"Missing teams: {len(missing_teams)}")

        if missing_teams:
            print("Teams not found in database:")
            for team in missing_teams:
                print(f"  - {team}")

        return True

    except Exception as e:
        print(f"❌ Error inserting coaches: {e}")
        return False


def verify_coaches_update():
    """Verify the coaches update was successful"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Count total coaches
        cursor.execute('SELECT COUNT(*) FROM "Coaches"')
        total_coaches = cursor.fetchone()[0]

        # Count coaches with real data (not placeholder)
        cursor.execute("""
                       SELECT COUNT(*)
                       FROM "Coaches"
                       WHERE "DateOfBirth" != '1970-01-01' 
            AND "Nationality" != 'Unknown'
            AND "YearsOfExperience" IS NOT NULL
                       """)
        real_data_coaches = cursor.fetchone()[0]

        # Get sample of updated coaches
        cursor.execute("""
                       SELECT c."FirstName",
                              c."LastName",
                              c."Nationality",
                              c."YearsOfExperience",
                              t."Name" as TeamName
                       FROM "Coaches" c
                                LEFT JOIN "Teams" t ON c."TeamId" = t."Id"
                       WHERE c."DateOfBirth" != '1970-01-01'
                       ORDER BY t."Name"
                           LIMIT 10
                       """)
        sample_coaches = cursor.fetchall()

        cursor.close()
        conn.close()

        print(f"\n=== VERIFICATION RESULTS ===")
        print(f"Total coaches in database: {total_coaches}")
        print(f"Coaches with real data: {real_data_coaches}")
        print(
            f"Success rate: {(real_data_coaches / total_coaches * 100):.1f}%" if total_coaches > 0 else "No coaches found")

        if sample_coaches:
            print(f"\nSample updated coaches:")
            for coach in sample_coaches:
                print(f"  {coach[0]} {coach[1]} ({coach[2]}) - {coach[3]} years exp -> {coach[4]}")

        return real_data_coaches > 0

    except Exception as e:
        print(f"❌ Error verifying update: {e}")
        return False


def main():
    print("=== UPDATING COACHES WITH REAL LA LIGA DATA ===\n")

    # Step 1: Clear existing coaches
    print("Step 1: Clearing existing coaches...")
    if not clear_existing_coaches():
        return

    # Step 2: Insert real coach data
    print("\nStep 2: Inserting real coach data...")
    if not insert_coaches_data():
        return

    # Step 3: Verify the update
    print("\nStep 3: Verifying update...")
    if verify_coaches_update():
        print("\n🎉 COACHES UPDATE COMPLETED SUCCESSFULLY!")
    else:
        print("\n❌ COACHES UPDATE FAILED!")


if __name__ == "__main__":
    main()

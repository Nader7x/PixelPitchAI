#!/usr/bin/env python3
"""
Script to check for original historical coaches data in the source database
"""
import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def check_source_database():
    """Check if the original 79 coaches still exist in the source database"""
    try:
        print("=== CHECKING SOURCE DATABASE (Footex - Training Database) ===")
        conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        cursor = conn.cursor()

        # Check coaches count
        cursor.execute('SELECT COUNT(*) FROM coaches')
        source_count = cursor.fetchone()[0]
        print(f'Source database coaches count: {source_count}')

        if source_count > 0:
            # Get sample coaches
            cursor.execute('SELECT "CoachID", "Name", "TeamID" FROM coaches ORDER BY "CoachID" LIMIT 10')
            sample_coaches = cursor.fetchall()
            print('\nSample coaches from source database:')
            for coach in sample_coaches:
                print(f'  ID: {coach[0]}, Name: {coach[1]}, TeamID: {coach[2]}')

            # Check distinct teams
            cursor.execute('SELECT COUNT(DISTINCT "TeamID") FROM coaches')
            distinct_teams = cursor.fetchone()[0]
            print(f'\nDistinct teams with coaches in source: {distinct_teams}')

            # Check all coaches
            cursor.execute('SELECT "CoachID", "Name", "TeamID" FROM coaches ORDER BY "TeamID", "CoachID"')
            all_coaches = cursor.fetchall()
            print(f'\nAll {len(all_coaches)} coaches in source database:')

            current_team = None
            for coach in all_coaches:
                if coach[2] != current_team:
                    current_team = coach[2]
                    print(f'\n--- Team ID: {current_team} ---')
                print(f'  Coach ID: {coach[0]} - {coach[1]}')
        else:
            print("No coaches found in source database!")

        conn.close()
        return source_count

    except Exception as e:
        print(f'Error connecting to source database: {e}')
        return 0


def check_target_database():
    """Check current coaches in target database"""
    try:
        print("\n=== CHECKING TARGET DATABASE (Footex_Api - API Database) ===")
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Check coaches count
        cursor.execute('SELECT COUNT(*) FROM coaches')
        target_count = cursor.fetchone()[0]
        print(f'Target database coaches count: {target_count}')

        if target_count > 0:
            # Check distinct teams
            cursor.execute('SELECT COUNT(DISTINCT "TeamID") FROM coaches')
            distinct_teams = cursor.fetchone()[0]
            print(f'Distinct teams with coaches in target: {distinct_teams}')

            # Get sample coaches
            cursor.execute('SELECT "CoachID", "Name", "TeamID" FROM coaches ORDER BY "CoachID" LIMIT 5')
            sample_coaches = cursor.fetchall()
            print('\nSample coaches from target database:')
            for coach in sample_coaches:
                print(f'  ID: {coach[0]}, Name: {coach[1]}, TeamID: {coach[2]}')

        conn.close()
        return target_count

    except Exception as e:
        print(f'Error connecting to target database: {e}')
        return 0


if __name__ == "__main__":
    source_count = check_source_database()
    target_count = check_target_database()

    print(f"\n=== SUMMARY ===")
    print(f"Source database (Footex): {source_count} coaches")
    print(f"Target database (Footex_Api): {target_count} coaches")

    if source_count > target_count:
        print(f"\n✅ GOOD NEWS! The source database has {source_count - target_count} more coaches than the target.")
        print("We can restore the missing historical coaches from the source database.")
    elif source_count == target_count:
        print("\n⚠️  Both databases have the same number of coaches.")
    else:
        print(f"\n❌ The target database has more coaches than the source.")

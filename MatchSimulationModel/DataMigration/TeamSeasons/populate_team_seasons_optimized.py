#!/usr/bin/env python3
"""
Optimized script to populate TeamSeasons table using batch processing
for better performance with large datasets.
"""

import os
import sys

# Add parent directory to path to access db_config
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def get_all_team_season_combinations():
    """Get all possible team-season combinations from target database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Get all team-season combinations using CROSS JOIN
        cursor.execute("""
                       SELECT t."Id"   as team_id,
                              t."Name" as team_name,
                              s."Id"   as season_id,
                              s."Name" as season_name
                       FROM "Teams" t
                                CROSS JOIN "Seasons" s
                       ORDER BY s."Id", t."Id"
                       """)

        combinations = cursor.fetchall()
        print(f"Generated {len(combinations)} team-season combinations to check")

        return combinations

    except Exception as e:
        print(f"❌ Error getting team-season combinations: {e}")
        return []

    finally:
        if conn:
            conn.close()


def get_teams_that_played_in_seasons_batch():
    """Get all teams that actually played in seasons using a single optimized query"""
    try:
        conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        cursor = conn.cursor()

        print("🔍 Analyzing matches to find team-season relationships...")

        # Single query to get all team-season combinations that exist in matches
        cursor.execute("""
                       SELECT DISTINCT m.season_id,
                                       team_id
                       FROM (SELECT season_id, home_team_id as team_id
                             FROM matches
                             UNION
                             SELECT season_id, away_team_id as team_id
                             FROM matches) m
                       ORDER BY m.season_id, team_id
                       """)

        valid_combinations = cursor.fetchall()
        print(f"Found {len(valid_combinations)} valid team-season combinations from matches")

        return set(valid_combinations)  # Convert to set for fast lookup

    except Exception as e:
        print(f"❌ Error getting teams from matches: {e}")
        return set()

    finally:
        if conn:
            conn.close()


def populate_team_seasons_optimized():
    """Optimized function to populate TeamSeasons table"""

    print("🚀 Starting Optimized TeamSeasons Population")
    print("=" * 60)

    # Step 1: Get all possible combinations
    print("\n=== STEP 1: GETTING TEAM-SEASON COMBINATIONS ===")
    all_combinations = get_all_team_season_combinations()

    if not all_combinations:
        print("❌ No team-season combinations found. Exiting.")
        return

    # Step 2: Get valid combinations from matches
    print("\n=== STEP 2: ANALYZING MATCHES DATA ===")
    valid_matches = get_teams_that_played_in_seasons_batch()

    if not valid_matches:
        print("❌ No valid matches found. Exiting.")
        return

    # Step 3: Filter combinations and prepare for insertion
    print("\n=== STEP 3: FILTERING AND PREPARING DATA ===")

    valid_team_seasons = []

    for team_id, team_name, season_id, season_name in all_combinations:
        if (season_id, team_id) in valid_matches:
            valid_team_seasons.append((team_id, season_id, team_name, season_name))

    print(f"✅ Found {len(valid_team_seasons)} valid team-season relationships")

    # Step 4: Batch insert into TeamSeasons table
    print("\n=== STEP 4: INSERTING INTO DATABASE ===")

    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Clear existing data (optional - remove if you want to preserve existing records)
        print("🧹 Clearing existing TeamSeasons data...")
        cursor.execute('DELETE FROM "TeamSeasons"')
        cleared_count = cursor.rowcount
        print(f"   Cleared {cleared_count} existing records")

        # Batch insert
        print("📥 Inserting new team-season relationships...")

        insert_data = [(team_id, season_id) for team_id, season_id, _, _ in valid_team_seasons]

        cursor.executemany("""
                           INSERT INTO "TeamSeasons" ("TeamId", "SeasonId")
                           VALUES (%s, %s)
                           """, insert_data)

        inserted_count = cursor.rowcount
        conn.commit()

        print(f"✅ Successfully inserted {inserted_count} team-season relationships")

        # Step 5: Verification and analysis
        print("\n=== STEP 5: VERIFICATION AND ANALYSIS ===")

        cursor.execute('SELECT COUNT(*) FROM "TeamSeasons"')
        total_records = cursor.fetchone()[0]

        cursor.execute("""
                       SELECT COUNT(DISTINCT "TeamId")   as unique_teams,
                              COUNT(DISTINCT "SeasonId") as unique_seasons
                       FROM "TeamSeasons"
                       """)
        unique_teams, unique_seasons = cursor.fetchone()

        print(f"📊 TeamSeasons Table Statistics:")
        print(f"   • Total records: {total_records}")
        print(f"   • Unique teams: {unique_teams}")
        print(f"   • Unique seasons: {unique_seasons}")
        print(f"   • Average teams per season: {total_records / unique_seasons if unique_seasons > 0 else 0:.1f}")

        # Teams per season breakdown
        cursor.execute("""
                       SELECT s."Name" as season_name, COUNT(*) as team_count
                       FROM "TeamSeasons" ts
                                JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                       GROUP BY s."Id", s."Name"
                       ORDER BY s."Name"
                       """)

        season_breakdown = cursor.fetchall()

        print(f"\n📊 Teams per Season Breakdown:")
        for season_name, team_count in season_breakdown:
            print(f"   • {season_name}: {team_count} teams")

        # Sample records
        cursor.execute("""
                       SELECT t."Name" as team_name, s."Name" as season_name
                       FROM "TeamSeasons" ts
                                JOIN "Teams" t ON ts."TeamId" = t."Id"
                                JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                       ORDER BY s."Name", t."Name" LIMIT 15
                       """)

        sample_records = cursor.fetchall()

        print(f"\n📋 Sample Records (showing first 15):")
        for team_name, season_name in sample_records:
            print(f"   • {team_name} → {season_name}")

        if len(sample_records) == 15:
            print("   ... and more")

    except Exception as e:
        print(f"❌ Error during database operations: {e}")
        if conn:
            conn.rollback()

    finally:
        if conn:
            conn.close()

    print("\n" + "=" * 60)
    print("✅ Optimized TeamSeasons Population Complete!")


def verify_team_seasons_integrity():
    """Verify the integrity of the populated TeamSeasons data"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== INTEGRITY VERIFICATION ===")

        # Check for duplicate records
        cursor.execute("""
                       SELECT "TeamId", "SeasonId", COUNT(*)
                       FROM "TeamSeasons"
                       GROUP BY "TeamId", "SeasonId"
                       HAVING COUNT(*) > 1
                       """)

        duplicates = cursor.fetchall()

        if duplicates:
            print(f"⚠️  Found {len(duplicates)} duplicate team-season combinations")
            for team_id, season_id, count in duplicates[:5]:
                print(f"   • Team {team_id}, Season {season_id}: {count} records")
        else:
            print("✅ No duplicate records found")

        # Check referential integrity
        cursor.execute("""
                       SELECT COUNT(*)
                       FROM "TeamSeasons" ts
                                LEFT JOIN "Teams" t ON ts."TeamId" = t."Id"
                       WHERE t."Id" IS NULL
                       """)

        orphaned_teams = cursor.fetchone()[0]

        cursor.execute("""
                       SELECT COUNT(*)
                       FROM "TeamSeasons" ts
                                LEFT JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                       WHERE s."Id" IS NULL
                       """)

        orphaned_seasons = cursor.fetchone()[0]

        if orphaned_teams > 0:
            print(f"⚠️  Found {orphaned_teams} records with invalid team references")
        else:
            print("✅ All team references are valid")

        if orphaned_seasons > 0:
            print(f"⚠️  Found {orphaned_seasons} records with invalid season references")
        else:
            print("✅ All season references are valid")

        print("✅ Integrity verification complete")

    except Exception as e:
        print(f"❌ Error during integrity verification: {e}")

    finally:
        if conn:
            conn.close()


def main():
    """Main function"""
    # Step 1: Populate TeamSeasons with optimized approach
    populate_team_seasons_optimized()

    # Step 2: Verify data integrity
    verify_team_seasons_integrity()


if __name__ == "__main__":
    main()

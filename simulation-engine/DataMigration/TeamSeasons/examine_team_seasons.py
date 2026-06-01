#!/usr/bin/env python3
"""
Script to examine and verify TeamSeasons table structure and data
before and after population.
"""

import os
import sys

# Add parent directory to path to access db_config
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def examine_target_database_structure():
    """Examine the target database structure for Teams, Seasons, and TeamSeasons tables"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== TARGET DATABASE STRUCTURE EXAMINATION ===\n")

        # Check Teams table
        cursor.execute('SELECT COUNT(*) FROM "Teams"')
        teams_count = cursor.fetchone()[0]

        cursor.execute('SELECT "Id", "Name" FROM "Teams" ORDER BY "Id" LIMIT 5')
        sample_teams = cursor.fetchall()

        print(f"📊 Teams Table:")
        print(f"   • Total teams: {teams_count}")
        print(f"   • Sample teams:")
        for team_id, team_name in sample_teams:
            print(f"     - ID {team_id}: {team_name}")
        print()

        # Check Seasons table
        cursor.execute('SELECT COUNT(*) FROM "Seasons"')
        seasons_count = cursor.fetchone()[0]

        cursor.execute('SELECT "Id", "Name" FROM "Seasons" ORDER BY "Id"')
        all_seasons = cursor.fetchall()

        print(f"📊 Seasons Table:")
        print(f"   • Total seasons: {seasons_count}")
        print(f"   • All seasons:")
        for season_id, season_name in all_seasons:
            print(f"     - ID {season_id}: {season_name}")
        print()

        # Check TeamSeasons table structure
        cursor.execute("""
                       SELECT column_name, data_type, is_nullable
                       FROM information_schema.columns
                       WHERE table_name = 'TeamSeasons'
                       ORDER BY ordinal_position
                       """)

        columns = cursor.fetchall()

        print(f"📊 TeamSeasons Table Structure:")
        for column_name, data_type, is_nullable in columns:
            nullable = "NULL" if is_nullable == "YES" else "NOT NULL"
            print(f"   • {column_name}: {data_type} ({nullable})")
        print()

        # Check current TeamSeasons data
        cursor.execute('SELECT COUNT(*) FROM "TeamSeasons"')
        team_seasons_count = cursor.fetchone()[0]

        print(f"📊 Current TeamSeasons Data:")
        print(f"   • Total records: {team_seasons_count}")

        if team_seasons_count > 0:
            cursor.execute("""
                           SELECT ts."Id", t."Name" as team_name, s."Name" as season_name
                           FROM "TeamSeasons" ts
                                    JOIN "Teams" t ON ts."TeamId" = t."Id"
                                    JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                           ORDER BY ts."Id" LIMIT 10
                           """)

            sample_data = cursor.fetchall()
            print(f"   • Sample records:")
            for ts_id, team_name, season_name in sample_data:
                print(f"     - ID {ts_id}: {team_name} → {season_name}")
        else:
            print(f"   • No records found (table is empty)")

        return teams_count, seasons_count, team_seasons_count

    except Exception as e:
        print(f"❌ Error examining target database: {e}")
        return 0, 0, 0

    finally:
        if conn:
            conn.close()


def examine_source_matches_data():
    """Examine the source database matches table to understand the data scope"""
    try:
        conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== SOURCE DATABASE MATCHES ANALYSIS ===\n")

        # Total matches
        cursor.execute('SELECT COUNT(*) FROM matches')
        total_matches = cursor.fetchone()[0]
        print(f"📊 Total matches in source database: {total_matches}")

        # Matches per season
        cursor.execute("""
                       SELECT season_id, COUNT(*) as match_count
                       FROM matches
                       GROUP BY season_id
                       ORDER BY season_id
                       """)

        season_matches = cursor.fetchall()
        print(f"\n📊 Matches per season:")
        for season_id, match_count in season_matches:
            print(f"   • Season {season_id}: {match_count} matches")

        # Unique teams in matches
        cursor.execute("""
                       SELECT COUNT(DISTINCT team_id) as unique_teams
                       FROM (SELECT home_team_id as team_id
                             FROM matches
                             UNION
                             SELECT away_team_id as team_id
                             FROM matches) teams
                       """)

        unique_teams = cursor.fetchone()[0]
        print(f"\n📊 Unique teams in matches: {unique_teams}")

        # Teams per season
        cursor.execute("""
                       SELECT season_id, COUNT(DISTINCT team_id) as unique_teams
                       FROM (SELECT season_id, home_team_id as team_id
                             FROM matches
                             UNION
                             SELECT season_id, away_team_id as team_id
                             FROM matches) teams
                       GROUP BY season_id
                       ORDER BY season_id
                       """)

        teams_per_season = cursor.fetchall()
        print(f"\n📊 Unique teams per season:")
        for season_id, team_count in teams_per_season:
            print(f"   • Season {season_id}: {team_count} teams")

        # Sample team-season combinations from matches
        cursor.execute("""
                       SELECT DISTINCT season_id, team_id
                       FROM (SELECT season_id, home_team_id as team_id
                             FROM matches
                             UNION
                             SELECT season_id, away_team_id as team_id
                             FROM matches) teams
                       ORDER BY season_id, team_id LIMIT 20
                       """)

        sample_combinations = cursor.fetchall()
        print(f"\n📊 Sample team-season combinations from matches:")
        for season_id, team_id in sample_combinations[:10]:
            print(f"   • Season {season_id}, Team {team_id}")
        if len(sample_combinations) > 10:
            print(f"   ... and {len(sample_combinations) - 10} more")

        return total_matches, unique_teams, len(teams_per_season)

    except Exception as e:
        print(f"❌ Error examining source matches: {e}")
        return 0, 0, 0

    finally:
        if conn:
            conn.close()


def calculate_expected_team_seasons():
    """Calculate expected number of TeamSeasons records based on matches data"""
    try:
        conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== EXPECTED TEAMSEASONS CALCULATION ===\n")

        # Count distinct team-season combinations from matches
        cursor.execute("""
                       SELECT COUNT(DISTINCT (season_id, team_id))
                       FROM (SELECT season_id, home_team_id as team_id
                             FROM matches
                             UNION
                             SELECT season_id, away_team_id as team_id
                             FROM matches) teams
                       """)

        expected_records = cursor.fetchone()[0]
        print(f"📊 Expected TeamSeasons records: {expected_records}")

        return expected_records

    except Exception as e:
        print(f"❌ Error calculating expected records: {e}")
        return 0

    finally:
        if conn:
            conn.close()


def main():
    """Main examination function"""
    print("🔍 TeamSeasons Table Examination and Verification")
    print("=" * 60)

    # Step 1: Examine target database structure
    teams_count, seasons_count, current_ts_count = examine_target_database_structure()

    # Step 2: Examine source matches data
    total_matches, unique_teams, season_count = examine_source_matches_data()

    # Step 3: Calculate expected records
    expected_records = calculate_expected_team_seasons()

    # Step 4: Summary and recommendations
    print("\n" + "=" * 60)
    print("📋 EXAMINATION SUMMARY")
    print("=" * 60)

    print(f"\n📊 Database Statistics:")
    print(f"   • Teams in target DB: {teams_count}")
    print(f"   • Seasons in target DB: {seasons_count}")
    print(f"   • Current TeamSeasons records: {current_ts_count}")
    print(f"   • Total matches in source DB: {total_matches}")
    print(f"   • Unique teams in matches: {unique_teams}")
    print(f"   • Expected TeamSeasons records: {expected_records}")

    print(f"\n📈 Analysis:")
    if current_ts_count == 0:
        print("   • ✅ TeamSeasons table is empty - ready for population")
    elif current_ts_count < expected_records:
        print(f"   • ⚠️  TeamSeasons table has {current_ts_count} records, expected {expected_records}")
        print("   • Consider running population script to complete data")
    elif current_ts_count == expected_records:
        print("   • ✅ TeamSeasons table appears to be fully populated")
    else:
        print(f"   • ⚠️  TeamSeasons table has more records ({current_ts_count}) than expected ({expected_records})")
        print("   • Check for duplicate records or data inconsistencies")

    print(f"\n🎯 Recommendations:")
    if current_ts_count == 0:
        print("   1. Run populate_team_seasons_optimized.py to populate the table")
        print("   2. Verify the results after population")
    else:
        print("   1. Review current data for accuracy")
        print("   2. Consider backup before any modifications")
        print("   3. Run integrity verification scripts")


if __name__ == "__main__":
    main()

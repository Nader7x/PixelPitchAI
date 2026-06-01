#!/usr/bin/env python3
"""
Script to populate TeamSeasons table by checking which teams played in which seasons
based on matches data from the source database.

Logic:
1. Get all teams from target database Teams table
2. Get all seasons from target database Seasons table  
3. For each team-season combination, check if team played in that season
4. Insert valid team-season combinations into TeamSeasons table
"""

import os
import sys

# Add parent directory to path to access db_config
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def get_teams_from_target():
    """Get all teams from target database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute('SELECT "Id", "Name" FROM "Teams" ORDER BY "Id"')
        teams = cursor.fetchall()

        print(f"Found {len(teams)} teams in target database")
        return teams

    except Exception as e:
        print(f"❌ Error getting teams from target database: {e}")
        return []

    finally:
        if conn:
            conn.close()


def get_seasons_from_target():
    """Get all seasons from target database"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute('SELECT "Id", "Name" FROM "Seasons" ORDER BY "Id"')
        seasons = cursor.fetchall()

        print(f"Found {len(seasons)} seasons in target database")
        return seasons

    except Exception as e:
        print(f"❌ Error getting seasons from target database: {e}")
        return []

    finally:
        if conn:
            conn.close()


def check_team_played_in_season(source_cursor, team_id, season_id):
    """Check if a team played in a specific season by looking at matches"""
    try:
        # Check if team appears as home_team_id or away_team_id in matches for this season
        source_cursor.execute("""
                              SELECT COUNT(*)
                              FROM matches
                              WHERE season_id = %s
                                AND (home_team_id = %s OR away_team_id = %s)
                              """, (season_id, team_id, team_id))

        count = source_cursor.fetchone()[0]
        return count > 0

    except Exception as e:
        print(f"❌ Error checking team {team_id} in season {season_id}: {e}")
        return False


def populate_team_seasons():
    """Main function to populate TeamSeasons table"""

    print("🚀 Starting TeamSeasons Population Process")
    print("=" * 60)

    # Step 1: Get teams and seasons from target database
    print("\n=== STEP 1: GETTING TEAMS AND SEASONS ===")
    teams = get_teams_from_target()
    seasons = get_seasons_from_target()

    if not teams or not seasons:
        print("❌ Failed to get teams or seasons. Exiting.")
        return

    print(f"📊 Processing {len(teams)} teams × {len(seasons)} seasons = {len(teams) * len(seasons)} combinations")

    # Step 2: Connect to both databases
    source_conn = None
    target_conn = None

    try:
        source_conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        target_conn = psycopg2.connect(**TARGET_DB_CONFIG)

        source_cursor = source_conn.cursor()
        target_cursor = target_conn.cursor()

        print("\n=== STEP 2: CHECKING TEAM-SEASON COMBINATIONS ===")

        valid_combinations = []
        checked_count = 0

        # Step 3: Check each team-season combination
        for team_id, team_name in teams:
            for season_id, season_name in seasons:
                checked_count += 1

                if check_team_played_in_season(source_cursor, team_id, season_id):
                    valid_combinations.append((team_id, season_id, team_name, season_name))
                    print(f"✓ {team_name} played in {season_name}")

                # Progress indicator
                if checked_count % 50 == 0:
                    print(f"   Checked {checked_count}/{len(teams) * len(seasons)} combinations...")

        print(f"\n📈 Found {len(valid_combinations)} valid team-season combinations")

        # Step 4: Insert valid combinations into TeamSeasons table
        print("\n=== STEP 3: INSERTING INTO TEAMSEASONS TABLE ===")

        inserted_count = 0

        for team_id, season_id, team_name, season_name in valid_combinations:
            try:
                # Insert with conflict handling (in case record already exists)
                target_cursor.execute("""
                                      INSERT INTO "TeamSeasons" ("TeamId", "SeasonId")
                                      VALUES (%s, %s) ON CONFLICT DO NOTHING
                                      """, (team_id, season_id))

                if target_cursor.rowcount > 0:
                    inserted_count += 1

            except Exception as e:
                print(f"❌ Error inserting {team_name} - {season_name}: {e}")

        # Commit the transaction
        target_conn.commit()

        print(f"\n✅ Successfully inserted {inserted_count} team-season relationships")

        # Step 5: Verification
        print("\n=== STEP 4: VERIFICATION ===")

        target_cursor.execute('SELECT COUNT(*) FROM "TeamSeasons"')
        total_records = target_cursor.fetchone()[0]

        target_cursor.execute("""
                              SELECT COUNT(DISTINCT "TeamId")   as unique_teams,
                                     COUNT(DISTINCT "SeasonId") as unique_seasons
                              FROM "TeamSeasons"
                              """)
        unique_teams, unique_seasons = target_cursor.fetchone()

        print(f"📊 Final TeamSeasons Statistics:")
        print(f"   • Total records: {total_records}")
        print(f"   • Unique teams: {unique_teams}")
        print(f"   • Unique seasons: {unique_seasons}")

        # Show sample data
        target_cursor.execute("""
                              SELECT ts."Id", t."Name" as team_name, s."Name" as season_name
                              FROM "TeamSeasons" ts
                                       JOIN "Teams" t ON ts."TeamId" = t."Id"
                                       JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                              ORDER BY s."Name", t."Name" LIMIT 10
                              """)

        sample_data = target_cursor.fetchall()

        print(f"\n📋 Sample TeamSeasons Records:")
        for ts_id, team_name, season_name in sample_data:
            print(f"   • {team_name} → {season_name}")

        if len(sample_data) == 10:
            print("   ... and more")

    except Exception as e:
        print(f"❌ Error during population process: {e}")
        if target_conn:
            target_conn.rollback()

    finally:
        if source_conn:
            source_conn.close()
        if target_conn:
            target_conn.close()

    print("\n" + "=" * 60)
    print("✅ TeamSeasons Population Complete!")


def analyze_team_seasons():
    """Analyze the populated TeamSeasons data"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== TEAMSEASONS ANALYSIS ===")

        # Teams per season analysis
        cursor.execute("""
                       SELECT s."Name" as season_name, COUNT(*) as team_count
                       FROM "TeamSeasons" ts
                                JOIN "Seasons" s ON ts."SeasonId" = s."Id"
                       GROUP BY s."Id", s."Name"
                       ORDER BY s."Name"
                       """)

        season_stats = cursor.fetchall()

        print("\n📊 Teams per Season:")
        for season_name, team_count in season_stats:
            print(f"   • {season_name}: {team_count} teams")

        # Seasons per team analysis
        cursor.execute("""
                       SELECT t."Name" as team_name, COUNT(*) as season_count
                       FROM "TeamSeasons" ts
                                JOIN "Teams" t ON ts."TeamId" = t."Id"
                       GROUP BY t."Id", t."Name"
                       ORDER BY COUNT(*) DESC, t."Name" LIMIT 15
                       """)

        team_stats = cursor.fetchall()

        print(f"\n📊 Top 15 Teams by Seasons Played:")
        for team_name, season_count in team_stats:
            print(f"   • {team_name}: {season_count} seasons")

    except Exception as e:
        print(f"❌ Error during analysis: {e}")

    finally:
        if conn:
            conn.close()


def main():
    """Main function"""
    # Step 1: Populate TeamSeasons
    populate_team_seasons()

    # Step 2: Analyze the data
    analyze_team_seasons()


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
Check Teams in Target Database

This script analyzes the teams currently in the target database to understand
which stadiums we need to create for the Stadium operations.
"""

import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_teams_for_stadiums():
    """Check teams in target database to plan stadium creation."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🏟️ STADIUM OPERATIONS - TEAM ANALYSIS")
        print("=" * 50)
        # Get all teams
        cursor.execute("""
                       SELECT "Id", "Name", "ShortName", "City", "StadiumId"
                       FROM "Teams"
                       ORDER BY "Name"
                       """)

        teams = cursor.fetchall()

        if not teams:
            print("❌ No teams found in target database!")
            return

        print(f"📊 Total Teams: {len(teams)}")
        print("-" * 30)
        # Group teams by base name (without season if any)
        team_groups = {}
        for team_id, name, short_name, city, stadium_id in teams:
            # For La Liga teams, use the name directly since they don't have season suffixes
            base_name = name

            if base_name not in team_groups:
                team_groups[base_name] = {
                    'id': team_id,
                    'full_name': name,
                    'short_name': short_name,
                    'city': city,
                    'stadium_id': stadium_id
                }
        print(f"🏟️ Unique Teams (stadiums needed): {len(team_groups)}")
        print("\nTEAM LIST:")
        print("-" * 40)

        teams_with_stadiums = 0
        teams_without_stadiums = 0

        for i, (base_name, team_data) in enumerate(sorted(team_groups.items()), 1):
            stadium_status = "✅ Has Stadium" if team_data['stadium_id'] else "❌ No Stadium"
            if team_data['stadium_id']:
                teams_with_stadiums += 1
            else:
                teams_without_stadiums += 1

            print(f"{i:2d}. {base_name}")
            print(f"    City: {team_data['city']}")
            print(f"    Short Name: {team_data['short_name']}")
            print(f"    Stadium: {stadium_status}")
            print()

        # Check if stadiums table exists
        cursor.execute("""
                       SELECT EXISTS (SELECT
                                      FROM information_schema.tables
                                      WHERE table_schema = 'public'
                                        AND table_name = 'Stadiums');
                       """)
        stadiums_table_exists = cursor.fetchone()[0]

        if stadiums_table_exists:
            cursor.execute('SELECT COUNT(*) FROM "Stadiums"')
            stadium_count = cursor.fetchone()[0]
            print(f"🏟️ Current Stadiums in Database: {stadium_count}")
        else:
            print("🏟️ Stadiums table: Not found (needs to be created)")

        print("\n" + "=" * 50)
        print("✅ Team analysis complete!")
        print(f"📋 Total teams: {len(team_groups)}")
        print(f"🏟️ Teams with stadiums: {teams_with_stadiums}")
        print(f"❌ Teams without stadiums: {teams_without_stadiums}")

        # Save team list for stadium creation
        with open('teams_for_stadiums.txt', 'w', encoding='utf-8') as f:
            f.write("Teams requiring stadium data:\n")
            f.write("=" * 40 + "\n\n")
            for base_name, team_data in sorted(team_groups.items()):
                status = "HAS_STADIUM" if team_data['stadium_id'] else "NEEDS_STADIUM"
                f.write(f"• {base_name} ({team_data['city']}) - {status}\n")

        print("📄 Team list saved to teams_for_stadiums.txt")

        cursor.close()
        conn.close()

        return list(team_groups.keys())

    except Exception as e:
        print(f"❌ Error checking teams: {e}")
        return None


if __name__ == "__main__":
    teams = check_teams_for_stadiums()
    if teams:
        print(f"\n🎯 Ready to create stadiums for {len(teams)} teams!")

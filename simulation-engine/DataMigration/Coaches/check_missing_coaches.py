#!/usr/bin/env python3
"""
Check which teams are missing coaches and what teams exist in database
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG


def check_missing_coaches():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Get all teams
        cursor.execute('SELECT "Id", "Name" FROM "Teams" ORDER BY "Name"')
        all_teams = cursor.fetchall()

        # Get teams with coaches
        cursor.execute("""
                       SELECT DISTINCT t."Name"
                       FROM "Teams" t
                                INNER JOIN "Coaches" c ON c."TeamId" = t."Id"
                       ORDER BY t."Name"
                       """)
        teams_with_coaches = [row[0] for row in cursor.fetchall()]

        # Get teams without coaches
        cursor.execute("""
                       SELECT t."Id", t."Name"
                       FROM "Teams" t
                                LEFT JOIN "Coaches" c ON c."TeamId" = t."Id"
                       WHERE c."Id" IS NULL
                       ORDER BY t."Name"
                       """)
        teams_without_coaches = cursor.fetchall()

        cursor.close()
        conn.close()

        print("=== TEAMS STATUS ===")
        print(f"Total teams in database: {len(all_teams)}")
        print(f"Teams with coaches: {len(teams_with_coaches)}")
        print(f"Teams without coaches: {len(teams_without_coaches)}")

        print("\n=== ALL TEAMS IN DATABASE ===")
        for team_id, team_name in all_teams:
            status = "✅ Has coach" if team_name in teams_with_coaches else "❌ No coach"
            print(f"{team_id:2d}. {team_name} - {status}")

        if teams_without_coaches:
            print("\n=== TEAMS MISSING COACHES ===")
            for team_id, team_name in teams_without_coaches:
                print(f"ID {team_id}: {team_name}")

    except Exception as e:
        print(f"Error: {e}")


if __name__ == "__main__":
    check_missing_coaches()

#!/usr/bin/env python3
"""
Check Teams Data

Quick check of teams in the database.
"""

import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def check_teams():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Check distinct league values
        cursor.execute('SELECT DISTINCT "League" FROM "Teams"')
        leagues = [row[0] for row in cursor.fetchall()]
        print(f"League values: {leagues}")

        # Check total teams
        cursor.execute('SELECT COUNT(*) FROM "Teams"')
        total_teams = cursor.fetchone()[0]
        print(f"Total teams: {total_teams}")

        # Check teams with StadiumId
        cursor.execute('SELECT COUNT(*) FROM "Teams" WHERE "StadiumId" IS NOT NULL')
        teams_with_stadiums = cursor.fetchone()[0]
        print(f"Teams with stadiums: {teams_with_stadiums}")

        # Show some team names and their stadium assignments
        cursor.execute('SELECT "Name", "League", "StadiumId" FROM "Teams" ORDER BY "Name" LIMIT 10')
        teams = cursor.fetchall()
        print("\nSample teams:")
        for name, league, stadium_id in teams:
            print(f"  {name} ({league}) -> Stadium ID: {stadium_id}")

        conn.close()

    except Exception as e:
        print(f"Error: {e}")


if __name__ == "__main__":
    check_teams()

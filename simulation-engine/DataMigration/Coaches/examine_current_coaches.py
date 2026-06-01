#!/usr/bin/env python3
"""
Script to examine current coaches and their team assignments
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG


def examine_current_coaches():
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== CURRENT COACHES WITH TEAMS ===\n")

        # Get all coaches with their teams
        cursor.execute("""
                       SELECT c."Id",
                              c."FirstName",
                              c."LastName",
                              c."DateOfBirth",
                              c."Nationality",
                              c."YearsOfExperience",
                              c."Biography",
                              c."CoachingStyle",
                              c."PreferredFormation",
                              t."Name" as TeamName,
                              c."TeamId"
                       FROM "Coaches" c
                                LEFT JOIN "Teams" t ON c."TeamId" = t."Id"
                       ORDER BY t."Name", c."LastName"
                       """)

        coaches = cursor.fetchall()

        print(f"Total coaches found: {len(coaches)}")
        print("=" * 120)

        for coach in coaches:
            coach_id, first_name, last_name, dob, nationality, experience, bio, style, formation, team_name, team_id = coach
            print(f"ID: {coach_id:2d} | {first_name} {last_name}")
            print(f"    Team: {team_name if team_name else 'No Team'} (ID: {team_id})")
            print(f"    Birth: {dob} | Nationality: {nationality}")
            print(f"    Experience: {experience} | Style: {style}")
            print(f"    Formation: {formation}")
            print(f"    Bio: {bio}")
            print("-" * 120)

        # Check for coaches without teams
        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "TeamId" IS NULL')
        no_team_count = cursor.fetchone()[0]
        print(f"\nCoaches without teams: {no_team_count}")

        # Check teams without coaches
        cursor.execute("""
                       SELECT t."Name"
                       FROM "Teams" t
                                LEFT JOIN "Coaches" c ON c."TeamId" = t."Id"
                       WHERE c."Id" IS NULL
                       ORDER BY t."Name"
                       """)
        teams_no_coach = cursor.fetchall()

        if teams_no_coach:
            print(f"\nTeams without coaches ({len(teams_no_coach)}):")
            for team in teams_no_coach:
                print(f"  - {team[0]}")
        else:
            print("\nAll teams have coaches assigned.")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error: {e}")


if __name__ == "__main__":
    examine_current_coaches()

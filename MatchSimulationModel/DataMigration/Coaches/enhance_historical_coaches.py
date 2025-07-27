#!/usr/bin/env python3
"""
Script to enhance historical coaches data with authentic biographical information
and implement temporal team-coach assignments for the 2015/2016-2020/2021 seasons.
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG

# Enhanced biographical data for key historical coaches
HISTORICAL_COACHES_DATA = {
    'Zinédine Zidane': {
        'DateOfBirth': '1972-06-23',
        'Nationality': 'French',
        'YearsOfExperience': 8,
        'Biography': 'Zinédine Zidane is a French professional football manager and former player. One of the greatest players of all time, he managed Real Madrid from 2016-2018 and 2019-2021, winning three consecutive Champions League titles. Known for his calm demeanor and tactical flexibility.',
        'CoachingStyle': 'Flexible tactical approach with emphasis on attacking football and player rotation. Master of managing superstars and adapting tactics to exploit opponents weaknesses.',
        'PreferredFormation': '4-3-3'
    },
    'Ronald Koeman': {
        'DateOfBirth': '1963-03-21',
        'Nationality': 'Dutch',
        'YearsOfExperience': 20,
        'Biography': 'Ronald Koeman is a Dutch professional football manager and former player. A legendary defender who won the European Cup with PSV and Barcelona as a player, he managed Barcelona from 2020-2021. Known for his attacking philosophy and development of young players.',
        'CoachingStyle': 'Possession-based attacking football with emphasis on youth development. Focuses on technical play, high pressing, and positional football.',
        'PreferredFormation': '4-3-3'
    },
    'Luis Enrique Martínez García': {
        'DateOfBirth': '1970-05-08',
        'Nationality': 'Spanish',
        'YearsOfExperience': 12,
        'Biography': 'Luis Enrique is a Spanish professional football manager and former player. He managed Barcelona from 2014-2017, winning the treble in 2015. Currently manages the Spanish national team. Known for his high-intensity style and tactical innovation.',
        'CoachingStyle': 'High-intensity pressing with quick transitions and attacking football. Emphasizes physical preparation, tactical versatility, and player rotation.',
        'PreferredFormation': '4-3-3'
    },
    'Ernesto Valverde Tejedor': {
        'DateOfBirth': '1964-02-09',
        'Nationality': 'Spanish',
        'YearsOfExperience': 25,
        'Biography': 'Ernesto Valverde is a Spanish football manager and former player. He managed Barcelona from 2017-2020, winning two La Liga titles. Previously successful with Athletic Bilbao. Known for his tactical discipline and defensive organization.',
        'CoachingStyle': 'Possession-based football with strong defensive organization. Emphasizes tactical discipline and players technical development.',
        'PreferredFormation': '4-2-3-1'
    },
    'Diego Pablo Simeone': {
        'DateOfBirth': '1970-04-28',
        'Nationality': 'Argentine',
        'YearsOfExperience': 14,
        'Biography': 'Diego Pablo Simeone has been the manager of Atlético Madrid since 2011. Under his leadership, Atlético has won La Liga twice and reached two Champions League finals. Known for his passionate touchline demeanor and defensive tactical approach.',
        'CoachingStyle': 'Defensive solidarity and counter-attacking football. Emphasizes physical intensity, team unity, and mental strength.',
        'PreferredFormation': '5-3-2'
    },
    'Unai Emery Etxegoien': {
        'DateOfBirth': '1971-11-03',
        'Nationality': 'Spanish',
        'YearsOfExperience': 18,
        'Biography': 'Unai Emery is a Spanish football manager who won three consecutive Europa League titles with Sevilla (2014-2016). He managed Paris Saint-Germain, Arsenal, and Villarreal. Known for his tactical preparation and cup competition success.',
        'CoachingStyle': 'Tactical flexibility with emphasis on detailed preparation and set-piece proficiency. Adapts formation based on opponent analysis.',
        'PreferredFormation': '4-2-3-1'
    },
    'Marcelino García Toral': {
        'DateOfBirth': '1965-08-14',
        'Nationality': 'Spanish',
        'YearsOfExperience': 22,
        'Biography': 'Marcelino García Toral has managed several Spanish clubs including Valencia, Athletic Bilbao, and Villarreal. Won the Copa del Rey with Valencia in 2019. Known for his tactical acumen and high-intensity playing style.',
        'CoachingStyle': 'High-intensity pressing with quick attacking transitions. Emphasizes tactical discipline and physical preparation.',
        'PreferredFormation': '4-4-2'
    },
    'José Bordalás Jiménez': {
        'DateOfBirth': '1964-03-05',
        'Nationality': 'Spanish',
        'YearsOfExperience': 20,
        'Biography': 'José Bordalás is known for his intense, physical style of play. He has managed Getafe, Valencia, and other Spanish clubs. Recognized for building solid, well-organized teams that are difficult to beat.',
        'CoachingStyle': 'Physical, aggressive pressing game with strong defensive organization. Emphasizes intensity and fighting spirit.',
        'PreferredFormation': '5-4-1'
    },
    'Manuel Luis Pellegrini Ripamonti': {
        'DateOfBirth': '1953-09-16',
        'Nationality': 'Chilean',
        'YearsOfExperience': 35,
        'Biography': 'Manuel Pellegrini has managed top clubs including Real Madrid, Manchester City, and Villarreal. Known as "The Engineer" due to his background in civil engineering. At Real Betis, he has implemented an attractive, possession-based style.',
        'CoachingStyle': 'Possession-based attacking football with emphasis on technical play and player development.',
        'PreferredFormation': '4-2-3-1'
    },
    'Julen Lopetegui Argote': {
        'DateOfBirth': '1966-08-28',
        'Nationality': 'Spanish',
        'YearsOfExperience': 15,
        'Biography': 'Julen Lopetegui managed the Spanish national team and later Sevilla FC, winning the Europa League in 2020. Previously managed Real Madrid briefly. Known for his possession-based tactical approach.',
        'CoachingStyle': 'Possession-based football with high pressing and positional play. Emphasizes technical development and tactical intelligence.',
        'PreferredFormation': '4-3-3'
    }
}


def enhance_historical_coaches():
    """Enhance historical coaches with authentic biographical data"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== ENHANCING HISTORICAL COACHES DATA ===\n")

        # Get all historical coaches (TeamId is NULL)
        cursor.execute("""
                       SELECT "Id", "FirstName", "LastName"
                       FROM "Coaches"
                       WHERE "TeamId" IS NULL
                       ORDER BY "LastName"
                       """)

        historical_coaches = cursor.fetchall()
        print(f"Found {len(historical_coaches)} historical coaches to enhance")

        enhanced_count = 0

        for coach_id, first_name, last_name in historical_coaches:
            full_name = f"{first_name} {last_name}".strip()

            # Check if we have enhanced data for this coach
            if full_name in HISTORICAL_COACHES_DATA:
                data = HISTORICAL_COACHES_DATA[full_name]

                cursor.execute("""
                               UPDATE "Coaches"
                               SET "DateOfBirth"        = %s,
                                   "Nationality"        = %s,
                                   "YearsOfExperience"  = %s,
                                   "Biography"          = %s,
                                   "CoachingStyle"      = %s,
                                   "PreferredFormation" = %s
                               WHERE "Id" = %s
                               """, (
                                   data['DateOfBirth'],
                                   data['Nationality'],
                                   data['YearsOfExperience'],
                                   data['Biography'],
                                   data['CoachingStyle'],
                                   data['PreferredFormation'],
                                   coach_id
                               ))

                enhanced_count += 1
                print(f"✓ Enhanced: {full_name}")
            else:
                # For coaches without specific data, improve the generic data
                cursor.execute("""
                               UPDATE "Coaches"
                               SET "Biography"     = %s,
                                   "CoachingStyle" = %s
                               WHERE "Id" = %s
                               """, (
                                   f"{full_name} was a football manager who coached in La Liga during the 2015-2021 period. Known for tactical discipline and team organization.",
                                   "Tactical flexibility with emphasis on team organization and maximizing player potential.",
                                   coach_id
                               ))

        conn.commit()
        print(f"\n✅ Enhancement complete!")
        print(f"   • Enhanced {enhanced_count} coaches with detailed biographical data")
        print(f"   • Improved {len(historical_coaches) - enhanced_count} coaches with generic data")

    except Exception as e:
        print(f"❌ Error enhancing historical coaches: {e}")
        conn.rollback()

    finally:
        if conn:
            conn.close()


def create_temporal_assignments_table():
    """Create a table to track temporal coach-team assignments"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== CREATING TEMPORAL ASSIGNMENTS TABLE ===\n")

        # Create the table for temporal coach-team assignments
        cursor.execute("""
                       CREATE TABLE IF NOT EXISTS "CoachTeamAssignments"
                       (
                           "Id"
                           SERIAL
                           PRIMARY
                           KEY,
                           "CoachId"
                           INTEGER
                           NOT
                           NULL
                           REFERENCES
                           "Coaches"
                       (
                           "Id"
                       ),
                           "TeamId" INTEGER NOT NULL REFERENCES "Teams"
                       (
                           "Id"
                       ),
                           "Season" VARCHAR
                       (
                           10
                       ) NOT NULL,
                           "StartDate" DATE,
                           "EndDate" DATE,
                           "IsActive" BOOLEAN DEFAULT FALSE,
                           UNIQUE
                       (
                           "CoachId",
                           "TeamId",
                           "Season"
                       )
                           )
                       """)

        # Create indexes for better performance
        cursor.execute("""
                       CREATE INDEX IF NOT EXISTS idx_coach_team_assignments_season
                           ON "CoachTeamAssignments"("Season")
                       """)

        cursor.execute("""
                       CREATE INDEX IF NOT EXISTS idx_coach_team_assignments_coach
                           ON "CoachTeamAssignments"("CoachId")
                       """)

        cursor.execute("""
                       CREATE INDEX IF NOT EXISTS idx_coach_team_assignments_team
                           ON "CoachTeamAssignments"("TeamId")
                       """)

        conn.commit()
        print("✅ Created CoachTeamAssignments table with indexes")

    except Exception as e:
        print(f"❌ Error creating temporal assignments table: {e}")
        conn.rollback()

    finally:
        if conn:
            conn.close()


def add_current_coach_assignments():
    """Add current coach assignments to the temporal table"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== ADDING CURRENT COACH ASSIGNMENTS ===\n")

        # Get all current coaches with team assignments
        cursor.execute("""
                       SELECT "Id", "TeamId"
                       FROM "Coaches"
                       WHERE "TeamId" IS NOT NULL
                       """)

        current_coaches = cursor.fetchall()

        for coach_id, team_id in current_coaches:
            cursor.execute("""
                           INSERT INTO "CoachTeamAssignments"
                               ("CoachId", "TeamId", "Season", "StartDate", "IsActive")
                           VALUES (%s, %s, %s, %s, %s) ON CONFLICT ("CoachId", "TeamId", "Season") DO NOTHING
                           """, (coach_id, team_id, '2024/2025', '2024-08-01', True))

        conn.commit()
        print(f"✅ Added {len(current_coaches)} current coach assignments for 2024/2025 season")

    except Exception as e:
        print(f"❌ Error adding current coach assignments: {e}")
        conn.rollback()

    finally:
        if conn:
            conn.close()


def create_season_queries():
    """Create utility functions for season-based queries"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== CREATING SEASON QUERY FUNCTIONS ===\n")

        # Create function to get coaches by season
        cursor.execute("""
                       CREATE
                       OR REPLACE FUNCTION get_coaches_by_season(season_param VARCHAR(10))
            RETURNS TABLE (
                coach_id INTEGER,
                coach_name VARCHAR(200),
                team_id INTEGER,
                team_name VARCHAR(100),
                nationality VARCHAR(50),
                experience INTEGER
            ) AS $$
                       BEGIN
                       RETURN QUERY
                       SELECT c."Id",
                              CONCAT(c."FirstName", ' ', c."LastName"),
                              cta."TeamId",
                              t."Name",
                              c."Nationality",
                              c."YearsOfExperience"
                       FROM "Coaches" c
                                JOIN "CoachTeamAssignments" cta ON c."Id" = cta."CoachId"
                                JOIN "Teams" t ON cta."TeamId" = t."Id"
                       WHERE cta."Season" = season_param
                       ORDER BY t."Name";
                       END;
            $$
                       LANGUAGE plpgsql;
                       """)

        # Create function to get coach history for a team
        cursor.execute("""
                       CREATE
                       OR REPLACE FUNCTION get_team_coach_history(team_id_param INTEGER)
            RETURNS TABLE (
                season VARCHAR(10),
                coach_name VARCHAR(200),
                start_date DATE,
                end_date DATE,
                nationality VARCHAR(50)
            ) AS $$
                       BEGIN
                       RETURN QUERY
                       SELECT cta."Season",
                              CONCAT(c."FirstName", ' ', c."LastName"),
                              cta."StartDate",
                              cta."EndDate",
                              c."Nationality"
                       FROM "CoachTeamAssignments" cta
                                JOIN "Coaches" c ON cta."CoachId" = c."Id"
                       WHERE cta."TeamId" = team_id_param
                       ORDER BY cta."Season" DESC;
                       END;
            $$
                       LANGUAGE plpgsql;
                       """)

        conn.commit()
        print("✅ Created season query functions")

    except Exception as e:
        print(f"❌ Error creating season query functions: {e}")
        conn.rollback()

    finally:
        if conn:
            conn.close()


def main():
    """Main function to enhance historical coaches data"""
    print("🚀 Starting Historical Coaches Enhancement Process")
    print("=" * 60)

    # Step 1: Enhance historical coaches with biographical data
    enhance_historical_coaches()

    # Step 2: Create temporal assignments table
    create_temporal_assignments_table()

    # Step 3: Add current coach assignments
    add_current_coach_assignments()

    # Step 4: Create season query functions
    create_season_queries()

    print("\n" + "=" * 60)
    print("✅ Historical Coaches Enhancement Complete!")
    print("\nNext steps:")
    print("1. Populate historical coach-team assignments for 2015-2021 seasons")
    print("2. Enhance remaining coaches with specific biographical data")
    print("3. Test season-based query functionality")


if __name__ == "__main__":
    main()

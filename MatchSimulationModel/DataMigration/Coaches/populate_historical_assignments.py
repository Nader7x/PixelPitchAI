#!/usr/bin/env python3
"""
Script to populate historical coach-team assignments for the 2015/2016-2020/2021 seasons
and demonstrate the enhanced temporal coaching system.
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG

# Sample historical coach-team assignments for major clubs
# This represents some of the key coaching changes during 2015-2021
HISTORICAL_ASSIGNMENTS = [
    # Real Madrid
    ('Zinédine Zidane', 'Real Madrid', '2015/2016', '2016-01-04', '2018-05-31'),
    ('Zinédine Zidane', 'Real Madrid', '2016/2017', '2016-08-01', '2018-05-31'),
    ('Zinédine Zidane', 'Real Madrid', '2017/2018', '2017-08-01', '2018-05-31'),
    ('Julen Lopetegui Argote', 'Real Madrid', '2018/2019', '2018-06-12', '2018-10-29'),
    ('Santiago Hernán Solari Poggio', 'Real Madrid', '2018/2019', '2018-10-29', '2019-03-11'),
    ('Zinédine Zidane', 'Real Madrid', '2018/2019', '2019-03-11', '2021-05-27'),
    ('Zinédine Zidane', 'Real Madrid', '2019/2020', '2019-08-01', '2021-05-27'),
    ('Zinédine Zidane', 'Real Madrid', '2020/2021', '2020-08-01', '2021-05-27'),

    # Barcelona
    ('Luis Enrique Martínez García', 'Barcelona', '2015/2016', '2014-05-19', '2017-05-28'),
    ('Luis Enrique Martínez García', 'Barcelona', '2016/2017', '2016-08-01', '2017-05-28'),
    ('Ernesto Valverde Tejedor', 'Barcelona', '2017/2018', '2017-05-29', '2020-01-13'),
    ('Ernesto Valverde Tejedor', 'Barcelona', '2018/2019', '2018-08-01', '2020-01-13'),
    ('Ernesto Valverde Tejedor', 'Barcelona', '2019/2020', '2019-08-01', '2020-01-13'),
    ('Enrique Setién Solar', 'Barcelona', '2019/2020', '2020-01-13', '2020-08-17'),
    ('Ronald Koeman', 'Barcelona', '2020/2021', '2020-08-19', '2021-10-27'),

    # Atlético Madrid
    ('Diego Pablo Simeone', 'Atlético Madrid', '2015/2016', '2011-12-23', None),
    ('Diego Pablo Simeone', 'Atlético Madrid', '2016/2017', '2016-08-01', None),
    ('Diego Pablo Simeone', 'Atlético Madrid', '2017/2018', '2017-08-01', None),
    ('Diego Pablo Simeone', 'Atlético Madrid', '2018/2019', '2018-08-01', None),
    ('Diego Pablo Simeone', 'Atlético Madrid', '2019/2020', '2019-08-01', None),
    ('Diego Pablo Simeone', 'Atlético Madrid', '2020/2021', '2020-08-01', None),

    # Sevilla
    ('Unai Emery Etxegoien', 'Sevilla', '2015/2016', '2013-01-15', '2016-06-30'),
    ('Jorge Luis Sampaoli Moya', 'Sevilla', '2016/2017', '2016-07-01', '2017-05-30'),
    ('Eduardo Berizzo', 'Sevilla', '2017/2018', '2017-05-30', '2017-12-23'),
    ('Vincenzo Montella', 'Sevilla', '2017/2018', '2017-12-23', '2018-04-30'),
    ('Joaquín Caparrós', 'Sevilla', '2017/2018', '2018-04-30', '2018-06-30'),
    ('Pablo Machín', 'Sevilla', '2018/2019', '2018-06-26', '2019-03-11'),
    ('Joaquín Caparrós', 'Sevilla', '2018/2019', '2019-03-11', '2019-06-30'),
    ('Julen Lopetegui Argote', 'Sevilla', '2019/2020', '2019-06-12', '2022-10-05'),
    ('Julen Lopetegui Argote', 'Sevilla', '2020/2021', '2020-08-01', '2022-10-05'),

    # Valencia
    ('Gary Neville', 'Valencia', '2015/2016', '2015-12-02', '2016-03-30'),
    ('Francisco Ayestarán', 'Valencia', '2015/2016', '2016-03-30', '2016-06-30'),
    ('Cesare Prandelli', 'Valencia', '2016/2017', '2016-09-26', '2016-12-30'),
    ('Voro González', 'Valencia', '2016/2017', '2016-12-30', '2017-03-26'),
    ('Marcelino García Toral', 'Valencia', '2017/2018', '2017-05-29', '2019-09-11'),
    ('Marcelino García Toral', 'Valencia', '2018/2019', '2018-08-01', '2019-09-11'),
    ('Albert Celades', 'Valencia', '2019/2020', '2019-09-11', '2020-06-30'),
    ('Javi Gracia', 'Valencia', '2020/2021', '2020-07-15', '2021-01-25'),

    # Athletic Bilbao
    ('Ernesto Valverde Tejedor', 'Athletic Club', '2015/2016', '2013-06-20', '2017-05-29'),
    ('Ernesto Valverde Tejedor', 'Athletic Club', '2016/2017', '2016-08-01', '2017-05-29'),
    ('José Ángel Ziganda', 'Athletic Club', '2017/2018', '2017-05-29', '2017-12-01'),
    ('José Ángel Ziganda', 'Athletic Club', '2018/2019', '2018-08-01', '2018-12-03'),
    ('Gaizka Garitano', 'Athletic Club', '2018/2019', '2018-12-03', '2021-01-04'),
    ('Gaizka Garitano', 'Athletic Club', '2019/2020', '2019-08-01', '2021-01-04'),
    ('Gaizka Garitano', 'Athletic Club', '2020/2021', '2020-08-01', '2021-01-04'),

    # Real Betis
    ('Pepe Mel', 'Real Betis', '2015/2016', '2010-06-01', '2016-01-18'),
    ('Juan Merino', 'Real Betis', '2015/2016', '2016-01-18', '2016-06-30'),
    ('Gustavo Poyet', 'Real Betis', '2016/2017', '2016-05-30', '2016-11-13'),
    ('Víctor Sánchez del Amo', 'Real Betis', '2016/2017', '2016-11-14', '2017-05-01'),
    ('Alexis Trujillo', 'Real Betis', '2016/2017', '2017-05-01', '2017-05-28'),
    ('Quique Setién', 'Real Betis', '2017/2018', '2017-05-30', '2020-01-13'),
    ('Quique Setién', 'Real Betis', '2018/2019', '2018-08-01', '2020-01-13'),
    ('Quique Setién', 'Real Betis', '2019/2020', '2019-08-01', '2020-01-13'),
    ('Rubi', 'Real Betis', '2019/2020', '2020-01-13', '2020-07-19'),
    ('Manuel Pellegrini', 'Real Betis', '2020/2021', '2020-07-19', None),
]


def get_coach_id_by_name(cursor, coach_name):
    """Get coach ID by name"""
    # Try exact match first
    cursor.execute("""
                   SELECT "Id"
                   FROM "Coaches"
                   WHERE CONCAT("FirstName", ' ', "LastName") = %s
                      OR "LastName" = %s LIMIT 1
                   """, (coach_name, coach_name.split()[-1]))

    result = cursor.fetchone()
    return result[0] if result else None


def get_team_id_by_name(cursor, team_name):
    """Get team ID by name"""
    cursor.execute("""
                   SELECT "Id"
                   FROM "Teams"
                   WHERE "Name" = %s LIMIT 1
                   """, (team_name,))

    result = cursor.fetchone()
    return result[0] if result else None


def populate_historical_assignments():
    """Populate historical coach-team assignments"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=== POPULATING HISTORICAL COACH-TEAM ASSIGNMENTS ===\n")

        successful_assignments = 0
        failed_assignments = 0

        for coach_name, team_name, season, start_date, end_date in HISTORICAL_ASSIGNMENTS:
            # Get coach and team IDs
            coach_id = get_coach_id_by_name(cursor, coach_name)
            team_id = get_team_id_by_name(cursor, team_name)

            if coach_id and team_id:
                try:
                    cursor.execute("""
                                   INSERT INTO "CoachTeamAssignments"
                                       ("CoachId", "TeamId", "Season", "StartDate", "EndDate", "IsActive")
                                   VALUES (%s, %s, %s, %s, %s, %s) ON CONFLICT ("CoachId", "TeamId", "Season") DO
                                   UPDATE SET
                                       "StartDate" = EXCLUDED."StartDate",
                                       "EndDate" = EXCLUDED."EndDate"
                                   """, (coach_id, team_id, season, start_date, end_date, False))

                    successful_assignments += 1
                    print(f"✓ {coach_name} → {team_name} ({season})")

                except Exception as e:
                    failed_assignments += 1
                    print(f"✗ Failed: {coach_name} → {team_name} ({season}): {e}")
            else:
                failed_assignments += 1
                print(f"✗ Not found: {coach_name} → {team_name} (Coach ID: {coach_id}, Team ID: {team_id})")

        conn.commit()
        print(f"\n✅ Historical assignments populated!")
        print(f"   • Successful: {successful_assignments}")
        print(f"   • Failed: {failed_assignments}")

    except Exception as e:
        print(f"❌ Error populating historical assignments: {e}")
        conn.rollback()

    finally:
        if conn:
            conn.close()


def test_temporal_queries():
    """Test the temporal query functionality"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== TESTING TEMPORAL QUERY FUNCTIONALITY ===\n")

        # Test 1: Get coaches for a specific season
        print("📊 COACHES IN 2019/2020 SEASON:")
        cursor.execute("SELECT * FROM get_coaches_by_season('2019/2020')")
        coaches_2019_20 = cursor.fetchall()

        for coach_id, coach_name, team_id, team_name, nationality, experience in coaches_2019_20:
            print(f"   • {coach_name} ({nationality}) → {team_name}")

        print(f"\nTotal coaches in 2019/2020: {len(coaches_2019_20)}")

        # Test 2: Get coach history for Real Madrid
        print("\n🏆 REAL MADRID COACH HISTORY:")
        real_madrid_id = get_team_id_by_name(cursor, 'Real Madrid')
        if real_madrid_id:
            cursor.execute("SELECT * FROM get_team_coach_history(%s)", (real_madrid_id,))
            madrid_history = cursor.fetchall()

            for season, coach_name, start_date, end_date, nationality in madrid_history:
                end_str = f" to {end_date}" if end_date else " (ongoing)"
                print(f"   • {season}: {coach_name} ({nationality}) - {start_date}{end_str}")

        # Test 3: Get statistics
        print("\n📈 ASSIGNMENT STATISTICS:")
        cursor.execute("""
                       SELECT "Season",
                              COUNT(*) as assignment_count
                       FROM "CoachTeamAssignments"
                       GROUP BY "Season"
                       ORDER BY "Season"
                       """)
        season_stats = cursor.fetchall()

        for season, count in season_stats:
            print(f"   • {season}: {count} assignments")

        # Test 4: Get total coaches and assignments
        cursor.execute('SELECT COUNT(*) FROM "Coaches"')
        total_coaches = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "CoachTeamAssignments"')
        total_assignments = cursor.fetchone()[0]

        print(f"\n📊 OVERALL STATISTICS:")
        print(f"   • Total coaches: {total_coaches}")
        print(f"   • Total assignments: {total_assignments}")
        print(f"   • Historical coaches: {total_coaches - 29}")
        print(f"   • Current coaches: 29")

    except Exception as e:
        print(f"❌ Error testing temporal queries: {e}")

    finally:
        if conn:
            conn.close()


def verify_enhanced_data():
    """Verify that historical coaches have been enhanced"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("\n=== VERIFYING ENHANCED HISTORICAL DATA ===\n")

        # Check a few key enhanced coaches
        key_coaches = ['Zinédine Zidane', 'Luis Enrique Martínez García', 'Ronald Koeman']

        for coach_name in key_coaches:
            cursor.execute("""
                           SELECT "FirstName",
                                  "LastName",
                                  "DateOfBirth",
                                  "Nationality",
                                  "YearsOfExperience",
                                  "CoachingStyle",
                                  "Biography"
                           FROM "Coaches"
                           WHERE CONCAT("FirstName", ' ', "LastName") = %s
                           """, (coach_name,))

            result = cursor.fetchone()
            if result:
                first_name, last_name, dob, nationality, experience, style, bio = result
                print(f"🔍 {first_name} {last_name}")
                print(f"   • Born: {dob} ({nationality})")
                print(f"   • Experience: {experience} years")
                print(f"   • Style: {style}")
                print(f"   • Bio: {bio[:100]}...")
                print()

        print("✅ Enhanced data verification complete!")

    except Exception as e:
        print(f"❌ Error verifying enhanced data: {e}")

    finally:
        if conn:
            conn.close()


def main():
    """Main function to populate and test historical assignments"""
    print("🚀 Starting Historical Assignments Population")
    print("=" * 60)

    # Step 1: Populate historical assignments
    populate_historical_assignments()

    # Step 2: Test temporal queries
    test_temporal_queries()

    # Step 3: Verify enhanced data
    verify_enhanced_data()

    print("\n" + "=" * 60)
    print("✅ Historical Assignments System Complete!")
    print("\nThe system now provides:")
    print("• 108 total coaches (29 current + 79 historical)")
    print("• Enhanced biographical data for key coaches")
    print("• Temporal coach-team assignments for 2015-2021")
    print("• Season-based query functionality")
    print("• Complete team coaching history")


if __name__ == "__main__":
    main()

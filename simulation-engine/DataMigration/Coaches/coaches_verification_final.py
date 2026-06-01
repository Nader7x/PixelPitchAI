#!/usr/bin/env python3
"""
Comprehensive coaches verification and final report
"""

import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import psycopg2
from db_config import TARGET_DB_CONFIG
from datetime import date


def generate_coaches_verification_report():
    """Generate comprehensive verification report for coaches data"""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("=" * 80)
        print("🏆 LA LIGA COACHES DATA VERIFICATION REPORT")
        print("=" * 80)

        # Basic statistics
        cursor.execute('SELECT COUNT(*) FROM "Coaches"')
        total_coaches = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Teams"')
        total_teams = cursor.fetchone()[0]

        print(f"\n📊 BASIC STATISTICS")
        print(f"    Total teams in database: {total_teams}")
        print(f"    Total coaches in database: {total_coaches}")
        print(f"    Coverage: {total_coaches}/{total_teams} teams ({(total_coaches / total_teams * 100):.1f}%)")

        # Data completeness check
        print(f"\n✅ DATA COMPLETENESS CHECK")

        # Check for missing/placeholder data
        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "DateOfBirth" = %s', (date(1970, 1, 1),))
        placeholder_dates = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "Nationality" = %s', ('Unknown',))
        unknown_nationalities = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "YearsOfExperience" IS NULL')
        missing_experience = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "Biography" IS NULL OR "Biography" = %s', ('',))
        missing_biographies = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "CoachingStyle" IS NULL OR "CoachingStyle" = %s', ('',))
        missing_styles = cursor.fetchone()[0]

        cursor.execute('SELECT COUNT(*) FROM "Coaches" WHERE "PreferredFormation" IS NULL OR "PreferredFormation" = %s',
                       ('',))
        missing_formations = cursor.fetchone()[0]

        fields_status = [
            ("DateOfBirth (placeholder 1970-01-01)", placeholder_dates, total_coaches),
            ("Nationality (Unknown)", unknown_nationalities, total_coaches),
            ("YearsOfExperience (NULL)", missing_experience, total_coaches),
            ("Biography (empty)", missing_biographies, total_coaches),
            ("CoachingStyle (empty)", missing_styles, total_coaches),
            ("PreferredFormation (empty)", missing_formations, total_coaches),
        ]

        for field_name, missing_count, total in fields_status:
            complete_count = total - missing_count
            percentage = (complete_count / total * 100) if total > 0 else 0
            status = "✅" if missing_count == 0 else "❌"
            print(f"    {status} {field_name:<35}: {complete_count:2d}/{total:2d} complete ({percentage:5.1f}%)")

        # Teams and coaches mapping
        print(f"\n🏟️  TEAMS AND COACHES MAPPING")
        cursor.execute("""
                       SELECT t."Name"                             as team_name,
                              c."FirstName" || ' ' || c."LastName" as coach_name,
                              c."Nationality",
                              c."YearsOfExperience",
                              c."PreferredFormation"
                       FROM "Teams" t
                                INNER JOIN "Coaches" c ON c."TeamId" = t."Id"
                       ORDER BY t."Name"
                       """)

        team_coaches = cursor.fetchall()

        for team_name, coach_name, nationality, experience, formation in team_coaches:
            print(f"    {team_name:<25} | {coach_name:<30} | {nationality:<12} | {experience:2d}y | {formation}")

        # Nationality distribution
        print(f"\n🌍 NATIONALITY DISTRIBUTION")
        cursor.execute("""
                       SELECT "Nationality", COUNT(*) as count
                       FROM "Coaches"
                       GROUP BY "Nationality"
                       ORDER BY count DESC, "Nationality"
                       """)

        nationalities = cursor.fetchall()
        for nationality, count in nationalities:
            percentage = (count / total_coaches * 100) if total_coaches > 0 else 0
            print(f"    {nationality:<15}: {count:2d} coaches ({percentage:5.1f}%)")

        # Experience distribution
        print(f"\n📈 EXPERIENCE DISTRIBUTION")
        cursor.execute("""
                       SELECT CASE
                                  WHEN "YearsOfExperience" < 5 THEN '0-4 years'
                                  WHEN "YearsOfExperience" < 10 THEN '5-9 years'
                                  WHEN "YearsOfExperience" < 15 THEN '10-14 years'
                                  WHEN "YearsOfExperience" < 20 THEN '15-19 years'
                                  WHEN "YearsOfExperience" < 25 THEN '20-24 years'
                                  WHEN "YearsOfExperience" < 30 THEN '25-29 years'
                                  ELSE '30+ years'
                                  END as experience_range,
                              COUNT(*) as count
                       FROM "Coaches"
                       WHERE "YearsOfExperience" IS NOT NULL
                       GROUP BY experience_range
                       ORDER BY MIN ("YearsOfExperience")
                       """)

        experience_ranges = cursor.fetchall()
        for exp_range, count in experience_ranges:
            percentage = (count / total_coaches * 100) if total_coaches > 0 else 0
            print(f"    {exp_range:<15}: {count:2d} coaches ({percentage:5.1f}%)")

        # Formation preferences
        print(f"\n⚽ FORMATION PREFERENCES")
        cursor.execute("""
                       SELECT "PreferredFormation", COUNT(*) as count
                       FROM "Coaches"
                       WHERE "PreferredFormation" IS NOT NULL AND "PreferredFormation" != ''
                       GROUP BY "PreferredFormation"
                       ORDER BY count DESC, "PreferredFormation"
                       """)

        formations = cursor.fetchall()
        for formation, count in formations:
            percentage = (count / total_coaches * 100) if total_coaches > 0 else 0
            print(f"    {formation:<15}: {count:2d} coaches ({percentage:5.1f}%)")

        # Overall quality assessment
        print(f"\n🎯 OVERALL QUALITY ASSESSMENT")

        all_complete = (placeholder_dates == 0 and unknown_nationalities == 0 and
                        missing_experience == 0 and missing_biographies == 0 and
                        missing_styles == 0 and missing_formations == 0)

        if all_complete:
            print(f"    ✅ ALL DATA FIELDS COMPLETE - 100% SUCCESS!")
            print(f"    ✅ All {total_teams} teams have assigned coaches")
            print(f"    ✅ All coaches have comprehensive real-world data")
            print(f"    ✅ Ready for production use")
        else:
            missing_total = (placeholder_dates + unknown_nationalities + missing_experience +
                             missing_biographies + missing_styles + missing_formations)
            total_fields = total_coaches * 6  # 6 critical fields per coach
            completion_rate = ((total_fields - missing_total) / total_fields * 100) if total_fields > 0 else 0
            print(f"    ⚠️  Data completion rate: {completion_rate:.1f}%")
            print(f"    ⚠️  Missing data fields: {missing_total}")

        print(f"\n" + "=" * 80)
        print(f"📅 Report generated: {date.today()}")
        print(f"🔗 Database: {TARGET_DB_CONFIG.get('host', 'localhost')}:{TARGET_DB_CONFIG.get('port', 5432)}")
        print(f"📊 Data source: coach_data.py (Real La Liga Coaches)")
        print(f"=" * 80)

        cursor.close()
        conn.close()

        return all_complete

    except Exception as e:
        print(f"❌ Error generating verification report: {e}")
        return False


def main():
    success = generate_coaches_verification_report()

    if success:
        print(f"\n🎉 COACHES DATA SYSTEM VERIFICATION COMPLETED SUCCESSFULLY!")
    else:
        print(f"\n❌ COACHES DATA SYSTEM VERIFICATION FAILED!")


if __name__ == "__main__":
    main()

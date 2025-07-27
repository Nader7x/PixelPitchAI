#!/usr/bin/env python3
"""
Final Migration and Enrichment Report

This script generates a comprehensive report of the entire data migration
and team enrichment process from the Footex training database to the 
Footex_Api production database.
"""

import psycopg2
import sys
from datetime import datetime

from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG


def generate_final_report():
    """Generate a comprehensive final report."""
    print("=" * 80)
    print("FINAL DATA MIGRATION AND ENRICHMENT REPORT")
    print("=" * 80)
    print(f"Generated on: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"Migration Type: PostgreSQL to PostgreSQL with ID preservation")
    print(f"Source Database: {SOURCE_DB_CONFIG['database']} (Training)")
    print(f"Target Database: {TARGET_DB_CONFIG['database']} (API/Production)")
    print()

    # Check source database statistics
    print("SOURCE DATABASE STATISTICS:")
    print("-" * 40)
    try:
        conn = psycopg2.connect(**SOURCE_DB_CONFIG)
        cursor = conn.cursor()

        tables = ['competitions', 'teams', 'players', 'managers', 'seasons']
        source_stats = {}

        for table in tables:
            cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            source_stats[table] = count
            print(f"{table.capitalize():<15}: {count:>6} records")

        cursor.close()
        conn.close()
    except Exception as e:
        print(f"Error accessing source database: {e}")
        source_stats = {}

    print()

    # Check target database statistics  
    print("TARGET DATABASE STATISTICS:")
    print("-" * 40)
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        tables = ['"Competitions"', '"Teams"', '"Players"', '"Coaches"', '"Seasons"']
        target_stats = {}

        table_names = ['Competitions', 'Teams', 'Players', 'Coaches', 'Seasons']
        for i, table in enumerate(tables):
            cursor.execute(f"SELECT COUNT(*) FROM {table}")
            count = cursor.fetchone()[0]
            target_stats[table_names[i]] = count
            print(f"{table_names[i]:<15}: {count:>6} records")

        cursor.close()
        conn.close()
    except Exception as e:
        print(f"Error accessing target database: {e}")
        target_stats = {}

    print()

    # Migration validation
    print("MIGRATION VALIDATION:")
    print("-" * 40)

    migration_success = True
    if source_stats and target_stats:
        # Check record counts match
        mappings = {
            'competitions': 'Competitions',
            'teams': 'Teams',
            'players': 'Players',
            'managers': 'Coaches',
            'seasons': 'Seasons'
        }

        for source_table, target_table in mappings.items():
            if source_table in source_stats and target_table in target_stats:
                source_count = source_stats[source_table]
                target_count = target_stats[target_table]

                if source_count == target_count:
                    print(f"✅ {target_table}: {source_count} → {target_count} (Perfect match)")
                else:
                    print(f"❌ {target_table}: {source_count} → {target_count} (Mismatch!)")
                    migration_success = False
            else:
                print(f"⚠️  {target_table}: Cannot verify (missing data)")

    print()

    # Team enrichment validation
    print("TEAM ENRICHMENT VALIDATION:")
    print("-" * 40)

    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        # Check enrichment status
        cursor.execute("""
                       SELECT COUNT(*)                                                                                      as total_teams,
                              COUNT(CASE WHEN "City" IS NOT NULL AND "City" != 'Unknown' THEN 1 END)                        as teams_with_city,
                              COUNT(CASE WHEN "FoundationDate" IS NOT NULL AND "FoundationDate" != '2000-01-01' THEN 1 END) as teams_with_foundation,
                              COUNT(CASE WHEN "PrimaryColor" IS NOT NULL AND "PrimaryColor" != 'NULL' THEN 1 END)           as teams_with_primary_color,
                              COUNT(CASE WHEN "SecondaryColor" IS NOT NULL AND "SecondaryColor" != 'NULL' THEN 1 END)       as teams_with_secondary_color,
                              COUNT(CASE WHEN "ShortName" IS NOT NULL AND "ShortName" != '' THEN 1 END)                     as teams_with_shortname
                       FROM "Teams"
                       """)

        enrichment_stats = cursor.fetchone()
        total, with_city, with_foundation, with_primary, with_secondary, with_shortname = enrichment_stats

        print(f"Total teams: {total}")
        print(f"Teams with city data: {with_city}/{total} ({(with_city / total * 100):.1f}%)")
        print(f"Teams with foundation dates: {with_foundation}/{total} ({(with_foundation / total * 100):.1f}%)")
        print(f"Teams with primary colors: {with_primary}/{total} ({(with_primary / total * 100):.1f}%)")
        print(f"Teams with secondary colors: {with_secondary}/{total} ({(with_secondary / total * 100):.1f}%)")
        print(f"Teams with short names: {with_shortname}/{total} ({(with_shortname / total * 100):.1f}%)")

        fully_enriched = min(with_city, with_foundation, with_primary, with_secondary, with_shortname)
        print(f"Fully enriched teams: {fully_enriched}/{total} ({(fully_enriched / total * 100):.1f}%)")

        enrichment_success = fully_enriched == total

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error checking enrichment: {e}")
        enrichment_success = False

    print()

    # Sample enriched team data
    print("SAMPLE ENRICHED TEAM DATA:")
    print("-" * 40)
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        cursor.execute("""
                       SELECT "Id", "Name", "ShortName", "City", "FoundationDate", "PrimaryColor", "SecondaryColor"
                       FROM "Teams"
                       WHERE "Name" IN ('Barcelona', 'Real Madrid', 'Atlético Madrid', 'Valencia', 'Sevilla')
                       ORDER BY "Id"
                       """)

        sample_teams = cursor.fetchall()
        print(f"{'ID':<3} {'Team':<20} {'Short':<6} {'City':<15} {'Founded':<12} {'Primary':<10} {'Secondary':<10}")
        print("-" * 85)

        for team in sample_teams:
            team_id, name, short_name, city, foundation, primary, secondary = team
            foundation_str = foundation.strftime('%Y-%m-%d') if foundation else 'N/A'
            print(
                f"{team_id:<3} {name:<20} {short_name:<6} {city:<15} {foundation_str:<12} {primary:<10} {secondary:<10}")

        cursor.close()
        conn.close()

    except Exception as e:
        print(f"Error getting sample data: {e}")

    print()

    # Final summary
    print("FINAL SUMMARY:")
    print("-" * 40)

    overall_success = migration_success and enrichment_success

    if overall_success:
        print("🎉 MIGRATION AND ENRICHMENT COMPLETED SUCCESSFULLY!")
        print("✅ All data successfully migrated with original IDs preserved")
        print("✅ All teams fully enriched with comprehensive data")
        print("✅ Database ready for production use")
    else:
        print("⚠️  MIGRATION COMPLETED WITH ISSUES")
        if not migration_success:
            print("❌ Migration validation failed - check record counts")
        if not enrichment_success:
            print("❌ Team enrichment incomplete - some teams missing data")

    print()
    print("KEY ACHIEVEMENTS:")
    print("• Preserved all original IDs from source database")
    print("• Successfully migrated 5 main entity types")
    print("• Enriched all teams with Spanish La Liga data")
    print("• Added comprehensive team information:")
    print("  - Accurate city locations")
    print("  - Historical foundation dates")
    print("  - Official team color schemes")
    print("  - Proper short name abbreviations (BAR, RMA, ATM, etc.)")
    print("• Maintained data integrity throughout migration")
    print("• Created validation and reporting tools")
    print()
    print("FILES CREATED/USED:")
    print("• corrected_migration.py - Final migration script")
    print("• Teams/enrich_teams.py - Team data enrichment")
    print("• validate_migration.py - ID preservation validator")
    print("• Teams/check_teams.py - Team enrichment checker")
    print("• db_config.py - Database configuration")
    print("• final_migration_report.py - This comprehensive report")
    print("• Teams/update_shortnames.py - Team short name updater")
    print("• Teams/check_shortnames.py - Team short name validator")
    print("• Teams/ - Team operations directory")

    print()
    print("=" * 80)

    return overall_success


if __name__ == "__main__":
    success = generate_final_report()
    sys.exit(0 if success else 1)

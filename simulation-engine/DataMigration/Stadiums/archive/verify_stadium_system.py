#!/usr/bin/env python3
"""
Stadium System Verification

Verify that all stadiums have been created correctly and teams are properly linked.
"""

import json
import os
import psycopg2
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


def verify_stadium_system():
    """Verify the complete stadium system."""
    try:
        conn = psycopg2.connect(**TARGET_DB_CONFIG)
        cursor = conn.cursor()

        print("🏟️ STADIUM SYSTEM VERIFICATION")
        print("=" * 50)
        # 1. Check total stadiums
        cursor.execute('SELECT COUNT(*) FROM "Stadiums"')
        stadium_count = cursor.fetchone()[0]
        print(f"📊 Total stadiums in database: {stadium_count}")

        # 2. Check stadium data completeness
        cursor.execute("""
                       SELECT "Id",
                              "Name",
                              "City",
                              "Capacity",
                              "Country",
                              "BuiltDate",
                              "LastRenovation",
                              "SurfaceType",
                              "Address",
                              "Latitude",
                              "Longitude",
                              "Architect",
                              "CostMillionsEuros",
                              "Nickname",
                              "Description",
                              "Facilities"
                       FROM "Stadiums"
                       ORDER BY "Name"
                       """)

        stadiums = cursor.fetchall()
        print(f"\n🏗️ STADIUM DETAILS")
        print("-" * 50)
        for stadium in stadiums[:5]:  # Show first 5 stadiums as sample
            id, name, city, capacity, country, built_date, last_renovation, \
                surface_type, address, latitude, longitude, architect, \
                cost_millions, nickname, description, facilities = stadium

            print(f"🏟️ {name}")
            print(f"   📍 {city}, {country}")
            print(f"   👥 Capacity: {capacity:,}")
            print(f"   📅 Built: {built_date} | Renovated: {last_renovation or 'N/A'}")
            print(f"   🏗️ Architect: {architect or 'N/A'}")
            print(f"   💰 Cost: €{cost_millions or 'N/A'}M")
            print(f"   🏷️ Nickname: {nickname or 'N/A'}")
            print(f"   📝 Description: {(description or 'N/A')[:100]}...")

            # Parse facilities JSON
            try:
                if facilities:
                    facilities_list = json.loads(facilities)
                    print(f"   🏢 Facilities: {', '.join(facilities_list)}")
                else:
                    print(f"   🏢 Facilities: N/A")
            except:
                print(f"   🏢 Facilities: {facilities or 'N/A'}")
            print()

    if len(stadiums) > 5:
        print(f"   ... and {len(stadiums) - 5} more stadiums")
    # 3. Check team-stadium links
    cursor.execute("""
                   SELECT t."Name" as TeamName, t."StadiumId", s."Name" as StadiumName, s."City"
                   FROM "Teams" t
                            LEFT JOIN "Stadiums" s ON t."StadiumId" = s."Id"
                   WHERE t."LeagueId" = 1 -- La Liga teams
                   ORDER BY t."Name"
                   """)

    team_links = cursor.fetchall()
    print(f"\n🔗 TEAM-STADIUM LINKS")
    print("-" * 50)

    linked_teams = 0
    unlinked_teams = 0

    for team_name, stadium_id, stadium_name, stadium_city in team_links:
        if stadium_id and stadium_name:
            linked_teams += 1
            print(f"✅ {team_name} → {stadium_name} ({stadium_city})")
        else:
            unlinked_teams += 1
            print(f"❌ {team_name} → No stadium assigned")

    print(f"\n📈 SUMMARY")
    print("-" * 30)
    print(f"✅ Teams with stadiums: {linked_teams}")
    print(f"❌ Teams without stadiums: {unlinked_teams}")
    print(f"🏟️ Total stadiums: {stadium_count}")
    # 4. Check for orphaned stadiums
    cursor.execute("""
                   SELECT s."Name", s."City"
                   FROM "Stadiums" s
                            LEFT JOIN "Teams" t ON s."Id" = t."StadiumId"
                   WHERE t."StadiumId" IS NULL
                   """)

    orphaned_stadiums = cursor.fetchall()
    if orphaned_stadiums:
        print(f"\n⚠️ ORPHANED STADIUMS (not linked to any team):")
        for stadium_name, city in orphaned_stadiums:
            print(f"   🏟️ {stadium_name} ({city})")
    else:
        print(f"\n✅ No orphaned stadiums found")
    # 5. Capacity statistics
    cursor.execute("""
                   SELECT MIN("Capacity") as MinCapacity,
                          MAX("Capacity") as MaxCapacity,
                          AVG("Capacity") as AvgCapacity,
                          COUNT(*)        as TotalStadiums
                   FROM "Stadiums"
                   """)

    capacity_stats = cursor.fetchone()
    min_cap, max_cap, avg_cap, total = capacity_stats

    print(f"\n📊 CAPACITY STATISTICS")
    print("-" * 30)
    print(f"🏟️ Smallest stadium: {min_cap:,} seats")
    print(f"🏟️ Largest stadium: {max_cap:,} seats")
    print(f"🏟️ Average capacity: {avg_cap:,.0f} seats")
    print(f"🏟️ Total stadiums: {total}")
    # 6. Check geographical distribution
    cursor.execute("""
                   SELECT "City", COUNT(*) as StadiumCount
                   FROM "Stadiums"
                   GROUP BY "City"
                   HAVING COUNT(*) > 1
                   ORDER BY StadiumCount DESC
                   """)

    cities_with_multiple = cursor.fetchall()
    if cities_with_multiple:
        print(f"\n🏙️ CITIES WITH MULTIPLE STADIUMS")
        print("-" * 40)
        for city, count in cities_with_multiple:
            print(f"   📍 {city}: {count} stadiums")

    conn.close()

    # Final verification
    success = (stadium_count == 29 and linked_teams == 29 and unlinked_teams == 0)

    print(f"\n{'🎉 VERIFICATION PASSED' if success else '❌ VERIFICATION FAILED'}")
    print("=" * 50)

    return success

except Exception as e:
print(f"❌ Error during verification: {e}")
return False

if __name__ == "__main__":
    verify_stadium_system()

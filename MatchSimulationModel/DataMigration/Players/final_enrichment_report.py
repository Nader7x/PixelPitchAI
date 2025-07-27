#!/usr/bin/env python3
"""
Final Comprehensive Player Data Quality Report

This script provides a complete overview of the player data enrichment results,
checking the database directly rather than relying on intermediate files.
"""

import json
import os
import psycopg2
import sys
from datetime import datetime
from typing import Dict, List, Tuple

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


class FinalDataQualityReporter:
    """Final comprehensive data quality analysis."""

    def __init__(self):
        self.start_time = datetime.now()

    def get_enrichment_summary(self) -> Dict:
        """Get summary of all enrichment efforts."""
        summary = {
            'real_data_files': [],
            'total_enriched': 0,
            'enrichment_sources': {}
        }

        # Check for enrichment files
        enrichment_files = [
            ('real_player_data_extracted.json', 'Match Files'),
            ('enhanced_player_data.json', 'Known Database'),
            ('api_enriched_players.json', 'API Sources'),
            ('preferred_foot_enriched.json', 'Foot Preference')
        ]

        for filename, source_name in enrichment_files:
            if os.path.exists(filename):
                try:
                    with open(filename, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        count = len(data)
                        summary['real_data_files'].append((filename, source_name, count))
                        summary['enrichment_sources'][source_name] = count
                        summary['total_enriched'] += count
                except Exception as e:
                    print(f"Warning: Could not read {filename}: {e}")

        return summary

    def analyze_database_comprehensively(self) -> Dict:
        """Perform comprehensive database analysis."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            # Overall statistics
            cursor.execute("""
                           SELECT COUNT(*)               as total_players,
                                  COUNT("Position")      as with_position,
                                  COUNT("PreferredFoot") as with_foot,
                                  COUNT("Nationality")   as with_nationality
                           FROM "Players"
                           WHERE "FullName" IS NOT NULL
                           """)

            overall = cursor.fetchone()

            # Position distribution
            cursor.execute("""
                           SELECT "Position", COUNT(*) as count
                           FROM "Players"
                           WHERE "Position" IS NOT NULL
                           GROUP BY "Position"
                           ORDER BY count DESC
                           """)
            position_dist = dict(cursor.fetchall())

            # Foot preference distribution
            cursor.execute("""
                           SELECT "PreferredFoot", COUNT(*) as count
                           FROM "Players"
                           WHERE "PreferredFoot" IS NOT NULL
                           GROUP BY "PreferredFoot"
                           ORDER BY count DESC
                           """)
            foot_dist = dict(cursor.fetchall())

            # Nationality distribution
            cursor.execute("""
                           SELECT "Nationality", COUNT(*) as count
                           FROM "Players"
                           WHERE "Nationality" IS NOT NULL
                           GROUP BY "Nationality"
                           ORDER BY count DESC
                               LIMIT 15
                           """)
            nationality_dist = dict(cursor.fetchall())

            # Detailed position analysis
            position_categories = {
                'Goalkeepers': ['GK'],
                'Defenders': ['CB', 'LB', 'RB', 'LWB', 'RWB'],
                'Midfielders': ['CDM', 'CM', 'CAM', 'LM', 'RM', 'DM', 'AM'],
                'Forwards': ['LW', 'RW', 'ST', 'CF', 'LF', 'RF']
            }

            category_counts = {}
            for category, positions in position_categories.items():
                count = sum(position_dist.get(pos, 0) for pos in positions)
                category_counts[category] = count

            conn.close()

            return {
                'total_players': overall[0],
                'with_position': overall[1],
                'with_foot': overall[2],
                'with_nationality': overall[3],
                'position_dist': position_dist,
                'foot_dist': foot_dist,
                'nationality_dist': nationality_dist,
                'category_counts': category_counts
            }

        except Exception as e:
            print(f"Error analyzing database: {e}")
            return {}

    def generate_comprehensive_report(self):
        """Generate the final comprehensive report."""
        print("🎯 FINAL PLAYER DATA ENRICHMENT REPORT")
        print("=" * 60)
        print(f"Generated: {self.start_time.strftime('%Y-%m-%d %H:%M:%S')}")

        # Get enrichment summary
        enrichment = self.get_enrichment_summary()

        print(f"\n📊 ENRICHMENT SUMMARY")
        print("-" * 30)
        print(f"Total Enrichment Files: {len(enrichment['real_data_files'])}")
        for filename, source, count in enrichment['real_data_files']:
            print(f"  • {source}: {count} players ({filename})")

        if enrichment['total_enriched'] > 0:
            print(f"\nTotal Players Enriched: {enrichment['total_enriched']}")

        # Get database analysis
        db_data = self.analyze_database_comprehensively()

        if db_data:
            print(f"\n📈 DATABASE ANALYSIS")
            print("-" * 30)
            total = db_data['total_players']
            print(f"Total Players: {total:,}")
            print(f"With Position: {db_data['with_position']:,} ({db_data['with_position'] / total * 100:.1f}%)")
            print(f"With Preferred Foot: {db_data['with_foot']:,} ({db_data['with_foot'] / total * 100:.1f}%)")
            print(
                f"With Nationality: {db_data['with_nationality']:,} ({db_data['with_nationality'] / total * 100:.1f}%)")

            print(f"\n⚽ POSITION CATEGORIES")
            print("-" * 30)
            for category, count in db_data['category_counts'].items():
                percentage = count / total * 100
                print(f"{category}: {count:,} ({percentage:.1f}%)")

            print(f"\n📍 DETAILED POSITIONS (Top 15)")
            print("-" * 30)
            sorted_positions = sorted(db_data['position_dist'].items(), key=lambda x: x[1], reverse=True)[:15]
            for position, count in sorted_positions:
                percentage = count / total * 100
                print(f"  {position}: {count:,} ({percentage:.1f}%)")

            print(f"\n🦶 FOOT PREFERENCE")
            print("-" * 30)
            for foot, count in db_data['foot_dist'].items():
                percentage = count / total * 100
                print(f"  {foot}: {count:,} ({percentage:.1f}%)")

            # Calculate left-footed improvement
            left_foot_count = db_data['foot_dist'].get('Left', 0)
            left_foot_percentage = left_foot_count / total * 100
            print(f"\n🎯 LEFT-FOOTED PLAYERS: {left_foot_count:,} ({left_foot_percentage:.1f}%)")

            print(f"\n🌍 TOP NATIONALITIES")
            print("-" * 30)
            for nationality, count in list(db_data['nationality_dist'].items())[:10]:
                percentage = count / total * 100
                print(f"  {nationality}: {count:,} ({percentage:.1f}%)")

        # Quality assessment
        print(f"\n✅ QUALITY ASSESSMENT")
        print("-" * 30)

        if db_data:
            completion_rate = (db_data['with_position'] + db_data['with_foot'] + db_data['with_nationality']) / (
                        total * 3) * 100
            print(f"Data Completion: {completion_rate:.1f}%")

            left_foot_rate = db_data['foot_dist'].get('Left', 0) / total * 100
            if left_foot_rate > 20:
                print("✅ EXCELLENT: Realistic left-foot distribution")
            elif left_foot_rate > 15:
                print("✅ GOOD: Improved left-foot distribution")
            else:
                print("⚠️  FAIR: Limited left-foot distribution")

        print(f"\n💡 NEXT STEPS")
        print("-" * 30)
        print("• Continue API enrichment for more players")
        print("• Set up additional API keys (Football-Data.org, API-Sports)")
        print("• Focus on preferred foot data where position suggests left-foot preference")
        print("• Validate data quality with expert football knowledge")
        print("• Consider web scraping for additional data sources")

        print(f"\n" + "=" * 60)
        print("Final enrichment report completed! ✅")

        # Save to file
        with open('final_enrichment_report.txt', 'w', encoding='utf-8') as f:
            # This is a simplified version for the file
            f.write(f"Final Player Data Enrichment Report\\n")
            f.write(f"Generated: {self.start_time.strftime('%Y-%m-%d %H:%M:%S')}\\n\\n")

            if db_data:
                f.write(f"Total Players: {db_data['total_players']:,}\\n")
                f.write(
                    f"Left-footed Players: {db_data['foot_dist'].get('Left', 0):,} ({db_data['foot_dist'].get('Left', 0) / db_data['total_players'] * 100:.1f}%)\\n")
                f.write(f"Enrichment Files Found: {len(enrichment['real_data_files'])}\\n")

            f.write("\\nEnrichment completed successfully!")

        print("📄 Report saved to final_enrichment_report.txt")


def main():
    """Main execution function."""
    reporter = FinalDataQualityReporter()
    reporter.generate_comprehensive_report()


if __name__ == "__main__":
    main()

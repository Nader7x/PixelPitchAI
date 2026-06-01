#!/usr/bin/env python3
"""
Comprehensive Player Data Quality Report

This script analyzes the current state of player data enrichment and provides
detailed statistics on real vs synthetic data usage.
"""

import json
import os
import psycopg2
import sys
from collections import defaultdict, Counter

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from db_config import TARGET_DB_CONFIG


class PlayerDataQualityAnalyzer:
    """Analyzer for player data quality and enrichment status."""

    def __init__(self):
        self.real_data_sources = self.load_real_data_sources()

    def load_real_data_sources(self) -> set:
        """Load names of players with real data from our enrichment files."""
        real_players = set()

        # Load from match file extraction
        try:
            with open('real_player_data_extracted.json', 'r', encoding='utf-8') as f:
                match_data = json.load(f)
                real_players.update(match_data.keys())
        except FileNotFoundError:
            pass

        # Load from enhanced enrichment
        try:
            with open('enhanced_player_data.json', 'r', encoding='utf-8') as f:
                enhanced_data = json.load(f)
                real_players.update(enhanced_data.keys())
        except FileNotFoundError:
            pass

        # Load from API enrichment
        try:
            with open('api_enriched_players.json', 'r', encoding='utf-8') as f:
                api_data = json.load(f)
                real_players.update(api_data.keys())
        except FileNotFoundError:
            pass

        return real_players

    def analyze_database(self) -> dict:
        """Perform comprehensive analysis of player data quality."""
        try:
            conn = psycopg2.connect(**TARGET_DB_CONFIG)
            cursor = conn.cursor()

            # Get overall statistics
            cursor.execute("""
                           SELECT COUNT(*)                                                as total_players,
                                  COUNT(CASE WHEN "Position" IS NOT NULL THEN 1 END)      as with_position,
                                  COUNT(CASE WHEN "PreferredFoot" IS NOT NULL THEN 1 END) as with_foot,
                                  COUNT(CASE WHEN "Nationality" IS NOT NULL THEN 1 END)   as with_nationality
                           FROM "Players"
                           """)

            stats = cursor.fetchone()

            # Get position distribution
            cursor.execute("""
                           SELECT "Position", COUNT(*) as count
                           FROM "Players"
                           WHERE "Position" IS NOT NULL
                           GROUP BY "Position"
                           ORDER BY count DESC
                           """)

            position_dist = dict(cursor.fetchall())

            # Get foot preference distribution
            cursor.execute("""
                           SELECT "PreferredFoot", COUNT(*) as count
                           FROM "Players"
                           WHERE "PreferredFoot" IS NOT NULL
                           GROUP BY "PreferredFoot"
                           ORDER BY count DESC
                           """)

            foot_dist = dict(cursor.fetchall())

            # Get nationality distribution (top 10)
            cursor.execute("""
                           SELECT "Nationality", COUNT(*) as count
                           FROM "Players"
                           WHERE "Nationality" IS NOT NULL
                           GROUP BY "Nationality"
                           ORDER BY count DESC
                               LIMIT 10
                           """)

            nationality_dist = dict(cursor.fetchall())

            # Get all players with their names
            cursor.execute("""
                           SELECT "Id", "FullName", "Position", "PreferredFoot", "Nationality"
                           FROM "Players"
                           ORDER BY "Id"
                           """)

            all_players = cursor.fetchall()
            conn.close()

            # Analyze real vs synthetic data
            real_data_count = 0
            real_players_found = []

            for player_id, full_name, position, foot, nationality in all_players:
                if full_name in self.real_data_sources:
                    real_data_count += 1
                    real_players_found.append(full_name)

            return {
                'total_stats': {
                    'total_players': stats[0],
                    'with_position': stats[1],
                    'with_foot': stats[2],
                    'with_nationality': stats[3]
                },
                'position_distribution': position_dist,
                'foot_distribution': foot_dist,
                'nationality_distribution': nationality_dist,
                'real_data_analysis': {
                    'real_data_count': real_data_count,
                    'synthetic_data_count': stats[0] - real_data_count,
                    'real_data_percentage': (real_data_count / stats[0]) * 100 if stats[0] > 0 else 0,
                    'real_players_found': real_players_found
                }
            }

        except Exception as e:
            print(f"Error analyzing database: {e}")
            return {}

    def generate_report(self) -> str:
        """Generate comprehensive data quality report."""
        analysis = self.analyze_database()

        if not analysis:
            return "❌ Could not generate report - database analysis failed"

        report = []
        report.append("📊 COMPREHENSIVE PLAYER DATA QUALITY REPORT")
        report.append("=" * 70)
        report.append(f"Generated: {self.get_current_timestamp()}")
        report.append("")

        # Overall Statistics
        stats = analysis['total_stats']
        report.append("📈 OVERALL STATISTICS")
        report.append("-" * 30)
        report.append(f"Total Players: {stats['total_players']:,}")
        report.append(
            f"With Position: {stats['with_position']:,} ({(stats['with_position'] / stats['total_players'] * 100):.1f}%)")
        report.append(
            f"With Preferred Foot: {stats['with_foot']:,} ({(stats['with_foot'] / stats['total_players'] * 100):.1f}%)")
        report.append(
            f"With Nationality: {stats['with_nationality']:,} ({(stats['with_nationality'] / stats['total_players'] * 100):.1f}%)")
        report.append("")

        # Real vs Synthetic Data
        real_data = analysis['real_data_analysis']
        report.append("🎯 DATA SOURCE ANALYSIS")
        report.append("-" * 30)
        report.append(f"Real Data Players: {real_data['real_data_count']:,} ({real_data['real_data_percentage']:.1f}%)")
        report.append(
            f"Synthetic Data Players: {real_data['synthetic_data_count']:,} ({100 - real_data['real_data_percentage']:.1f}%)")
        report.append("")
        report.append("🌟 REAL DATA SOURCES BREAKDOWN:")

        # Count by source type
        match_extracted = 0
        known_database = 0
        api_sourced = 0

        try:
            with open('real_player_data_extracted.json', 'r', encoding='utf-8') as f:
                match_data = json.load(f)
                match_extracted = len(match_data)
        except FileNotFoundError:
            pass

        try:
            with open('enhanced_player_data.json', 'r', encoding='utf-8') as f:
                enhanced_data = json.load(f)
                known_database = len(enhanced_data)
        except FileNotFoundError:
            pass

        try:
            with open('api_enriched_players.json', 'r', encoding='utf-8') as f:
                api_data = json.load(f)
                api_sourced = len(api_data)
        except FileNotFoundError:
            pass

        report.append(f"  • Match File Extraction: {match_extracted} players")
        report.append(f"  • Known Player Database: {known_database} players")
        report.append(f"  • API Sources: {api_sourced} players")
        report.append("")

        # Position Distribution
        report.append("⚽ POSITION DISTRIBUTION")
        report.append("-" * 30)
        pos_dist = analysis['position_distribution']
        total_pos = sum(pos_dist.values())

        # Group by position type
        goalkeepers = pos_dist.get('GK', 0)
        defenders = sum(pos_dist.get(pos, 0) for pos in ['CB', 'LB', 'RB', 'LWB', 'RWB'])
        midfielders = sum(pos_dist.get(pos, 0) for pos in ['CDM', 'CM', 'CAM', 'LM', 'RM', 'DM', 'AM'])
        forwards = sum(pos_dist.get(pos, 0) for pos in ['ST', 'CF', 'LW', 'RW', 'LF', 'RF'])

        report.append(f"Goalkeepers: {goalkeepers:,} ({(goalkeepers / total_pos * 100):.1f}%)")
        report.append(f"Defenders: {defenders:,} ({(defenders / total_pos * 100):.1f}%)")
        report.append(f"Midfielders: {midfielders:,} ({(midfielders / total_pos * 100):.1f}%)")
        report.append(f"Forwards: {forwards:,} ({(forwards / total_pos * 100):.1f}%)")
        report.append("")

        # Detailed position breakdown
        report.append("DETAILED POSITIONS:")
        for position, count in sorted(pos_dist.items(), key=lambda x: x[1], reverse=True):
            percentage = (count / total_pos) * 100
            report.append(f"  {position}: {count:,} ({percentage:.1f}%)")
        report.append("")

        # Foot Preference Distribution
        report.append("🦶 PREFERRED FOOT DISTRIBUTION")
        report.append("-" * 30)
        foot_dist = analysis['foot_distribution']
        total_foot = sum(foot_dist.values())

        for foot, count in sorted(foot_dist.items(), key=lambda x: x[1], reverse=True):
            percentage = (count / total_foot) * 100
            report.append(f"{foot}: {count:,} ({percentage:.1f}%)")
        report.append("")

        # Top Nationalities
        report.append("🌍 TOP 10 NATIONALITIES")
        report.append("-" * 30)
        nat_dist = analysis['nationality_distribution']
        total_nat = sum(nat_dist.values())

        for nationality, count in list(nat_dist.items())[:10]:
            percentage = (count / total_nat) * 100
            report.append(f"{nationality}: {count:,} ({percentage:.1f}%)")
        report.append("")

        # Data Quality Assessment
        report.append("✅ DATA QUALITY ASSESSMENT")
        report.append("-" * 30)

        completion_rate = (stats['with_position'] / stats['total_players']) * 100
        if completion_rate == 100:
            report.append("✅ EXCELLENT: 100% data completion")
        elif completion_rate >= 95:
            report.append("✅ VERY GOOD: >95% data completion")
        elif completion_rate >= 90:
            report.append("⚠️  GOOD: >90% data completion")
        else:
            report.append("❌ NEEDS IMPROVEMENT: <90% data completion")

        real_data_rate = real_data['real_data_percentage']
        if real_data_rate >= 10:
            report.append("✅ EXCELLENT: >10% real data usage")
        elif real_data_rate >= 5:
            report.append("✅ GOOD: >5% real data usage")
        elif real_data_rate >= 2:
            report.append("⚠️  FAIR: >2% real data usage")
        else:
            report.append("❌ LOW: <2% real data usage")

        report.append("")

        # Recommendations
        report.append("💡 RECOMMENDATIONS")
        report.append("-" * 30)

        if real_data_rate < 10:
            report.append("• Consider setting up additional API keys for more real data")
            report.append("• Run API enrichment for more player batches")

        if api_sourced == 0:
            report.append("• Set up Football-Data.org API for enhanced data")
            report.append("• Consider API-Sports for comprehensive player information")

        if match_extracted < 50:
            report.append("• Analyze more match files for real player behavior data")
            report.append("• Improve pattern matching for player action extraction")

        report.append("• Continue to prioritize real data over synthetic data")
        report.append("• Regularly validate and update player information")

        report.append("")
        report.append("=" * 70)
        report.append("Report Complete ✅")

        return "\\n".join(report)

    def get_current_timestamp(self) -> str:
        """Get current timestamp for report."""
        from datetime import datetime
        return datetime.now().strftime("%Y-%m-%d %H:%M:%S")

    def save_report(self, filename: str = 'player_data_quality_report.txt'):
        """Save the quality report to a file."""
        report_content = self.generate_report()

        try:
            with open(filename, 'w', encoding='utf-8') as f:
                f.write(report_content)
            print(f"📄 Report saved to {filename}")
            return True
        except Exception as e:
            print(f"❌ Error saving report: {e}")
            return False


def main():
    """Main execution function."""
    print("📊 Generating Player Data Quality Report...")
    print("=" * 50)

    analyzer = PlayerDataQualityAnalyzer()

    # Generate and display report
    report = analyzer.generate_report()
    print(report)

    # Save report to file
    analyzer.save_report()

    print("\\n" + "=" * 50)
    print("Quality analysis completed! ✅")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""
Cleanup and organize migration files
Keeps essential files and removes outdated/temporary files
"""

import os
import shutil
from pathlib import Path


def cleanup_migration_directory():
    """Clean up the DataMigration directory."""

    migration_dir = Path("d:/programming/vscodeProjects/MatchSimulationModel/DataMigration")

    # Essential files to keep
    essential_files = {
        'corrected_migration.py',  # Final working migration script
        'enrich_teams.py',  # Team enrichment script
        'validate_migration.py',  # ID preservation validator
        'check_teams.py',  # Team status checker
        'final_migration_report.py',  # Comprehensive report
        'db_config.py',  # Database configuration
        'MIGRATION_README.md',  # Documentation
        'team_enrichment.log'  # Enrichment log
    }

    # Files to remove (outdated/temporary)
    files_to_remove = {
        'working_migration.py',  # Outdated migration attempt
        'migration_report.py',  # Superseded by final_migration_report.py
        'test_insert.py',  # Temporary test file
        'check_columns.py',  # Development utility (no longer needed)
        'check_tables.py',  # Development utility (no longer needed)
        'check_target_tables.py',  # Development utility (no longer needed)
        'inspect_schemas.py',  # Development utility (no longer needed)
        'inspect_source_tables.py',  # Development utility (no longer needed)
        'inspect_target_schema.py',  # Development utility (no longer needed)
        'test_db_connection.py'  # Connection test (no longer needed)
    }

    # Create archive directory for removed files
    archive_dir = migration_dir / "archive"
    archive_dir.mkdir(exist_ok=True)

    print("🧹 CLEANING UP MIGRATION DIRECTORY")
    print("=" * 50)
    print()

    print("📁 Essential files (keeping):")
    for file in essential_files:
        file_path = migration_dir / file
        if file_path.exists():
            print(f"✅ {file}")
        else:
            print(f"⚠️  {file} (missing)")

    print()
    print("📦 Archiving outdated files:")
    archived_count = 0

    for file in files_to_remove:
        file_path = migration_dir / file
        if file_path.exists():
            # Move to archive
            archive_path = archive_dir / file
            shutil.move(str(file_path), str(archive_path))
            print(f"📦 {file} → archive/")
            archived_count += 1
        else:
            print(f"⚠️  {file} (already removed)")

    print()
    print("🗂️  FINAL DIRECTORY STRUCTURE:")
    print("-" * 30)

    # List current files
    remaining_files = [f for f in os.listdir(migration_dir) if os.path.isfile(migration_dir / f)]
    remaining_files.sort()

    for file in remaining_files:
        if file in essential_files:
            print(f"✅ {file}")
        else:
            print(f"❓ {file}")

    # Show archive contents
    if archive_dir.exists():
        archive_files = [f for f in os.listdir(archive_dir) if os.path.isfile(archive_dir / f)]
        if archive_files:
            print()
            print("📦 Archive directory:")
            for file in sorted(archive_files):
                print(f"   📄 {file}")

    print()
    print("✨ CLEANUP SUMMARY:")
    print(f"• Essential files maintained: {len([f for f in essential_files if (migration_dir / f).exists()])}")
    print(f"• Files archived: {archived_count}")
    print("• Directory structure optimized")
    print("• Ready for production use")

    return True


if __name__ == "__main__":
    cleanup_migration_directory()

#!/usr/bin/env python3
"""
Complete Migration Script - Updated for Actual Target Schema
Migrates data from Footex (training DB) to Footex_Api (target DB) while preserving original IDs
"""
import json
import logging
import psycopg2
import psycopg2.extras
from datetime import datetime, date
from typing import Dict, List, Optional, Any

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)


class DatabaseMigrator:
    def __init__(self, source_config: Dict[str, str], target_config: Dict[str, str]):
        """
        Initialize the database migrator with source and target database configurations.
        
        Args:
            source_config: Configuration for the source database (Footex)
            target_config: Configuration for the target database (Footex_Api)
        """
        self.source_config = source_config
        self.target_config = target_config
        self.source_conn = None
        self.target_conn = None

        # Mapping dictionaries to keep track of ID mappings between databases
        self.competition_id_mapping = {}
        self.team_id_mapping = {}
        self.player_id_mapping = {}
        self.manager_id_mapping = {}
        self.season_id_mapping = {}

    def connect_databases(self):
        """Establish connections to both source and target databases."""
        try:
            self.source_conn = psycopg2.connect(**self.source_config)
            self.target_conn = psycopg2.connect(**self.target_config)
            logger.info("Successfully connected to both databases")
        except Exception as e:
            logger.error(f"Failed to connect to databases: {e}")
            raise

    def disconnect_databases(self):
        """Close database connections."""
        if self.source_conn:
            self.source_conn.close()
        if self.target_conn:
            self.target_conn.close()
        logger.info("Database connections closed")

    def migrate_competitions(self):
        """Migrate competitions from source to target database with original IDs."""
        logger.info("Starting competitions migration...")

        try:
            source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
            target_cursor = self.target_conn.cursor()

            # Get competitions from source database
            source_cursor.execute("SELECT competition_id, competition_name FROM competitions")
            competitions = source_cursor.fetchall()

            for comp in competitions:
                # Insert into target database with original ID, providing a default description
                target_cursor.execute(
                    'INSERT INTO "Competitions" ("Id", "Name", "Description") VALUES (%s, %s, %s) ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "Description" = EXCLUDED."Description"',
                    (comp['competition_id'], comp['competition_name'],
                     f"Migrated from training database: {comp['competition_name']}")
                )

                # Store mapping (same ID in this case)
                self.competition_id_mapping[comp['competition_id']] = comp['competition_id']

            self.target_conn.commit()
            source_cursor.close()
            target_cursor.close()

            logger.info(f"Migrated {len(competitions)} competitions")

        except Exception as e:
            logger.error(f"Failed to migrate competitions: {e}")
            self.target_conn.rollback()
            raise

    def migrate_teams(self):
        """Migrate teams from source to target database with original IDs."""
        logger.info("Starting teams migration...")

        try:
            source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
            target_cursor = self.target_conn.cursor()

            # Get teams from source database (no competition_id in source)
            source_cursor.execute("SELECT team_id, team_name FROM teams")
            teams = source_cursor.fetchall()

            for team in teams:
                # Insert into target database with original ID
                # Using default values for required fields
                target_cursor.execute(
                    '''INSERT INTO "Teams" ("Id", "Name", "ShortName", "Country", "City", "League", "FoundationDate")
                       VALUES (%s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("Id") DO
                    UPDATE SET
                        "Name" = EXCLUDED."Name",
                        "ShortName" = EXCLUDED."ShortName"''',
                    (
                        team['team_id'],
                        team['team_name'],
                        team['team_name'][:10] if len(team['team_name']) > 10 else team['team_name'],  # ShortName
                        'Spain',  # Default country
                        'Unknown',  # Default city
                        'La Liga',  # Default league
                        date(2000, 1, 1)  # Default foundation date
                    )
                )

                # Store mapping (same ID in this case)
                self.team_id_mapping[team['team_id']] = team['team_id']

            self.target_conn.commit()
            source_cursor.close()
            target_cursor.close()

            logger.info(f"Migrated {len(teams)} teams")

        except Exception as e:
            logger.error(f"Failed to migrate teams: {e}")
            self.target_conn.rollback()
            raise

    def migrate_players(self):
        """Migrate players from source to target database with original IDs."""
        logger.info("Starting players migration...")

        try:
            source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
            target_cursor = self.target_conn.cursor()

            # Get players from source database (no team_id in source)
            source_cursor.execute("SELECT player_id, player_name FROM players")
            players = source_cursor.fetchall()

            # Get the first team ID from migrated teams to use as default
            default_team_id = list(self.team_id_mapping.values())[0] if self.team_id_mapping else 1
            current_time = datetime.now()

            for player in players:
                # Insert into target database with original ID
                target_cursor.execute(
                    '''INSERT INTO "Players" ("Id", "FullName", "KnownName", "TeamId", "CreatedAt", "UpdatedAt")
                       VALUES (%s, %s, %s, %s, %s, %s) ON CONFLICT ("Id") DO
                    UPDATE SET
                        "FullName" = EXCLUDED."FullName",
                        "KnownName" = EXCLUDED."KnownName",
                        "UpdatedAt" = EXCLUDED."UpdatedAt"''',
                    (
                        player['player_id'],
                        player['player_name'],  # FullName
                        player['player_name'],  # KnownName
                        default_team_id,  # TeamId
                        current_time,  # CreatedAt
                        current_time  # UpdatedAt
                    )
                )

                # Store mapping (same ID in this case)
                self.player_id_mapping[player['player_id']] = player['player_id']

            self.target_conn.commit()
            source_cursor.close()
            target_cursor.close()

            logger.info(f"Migrated {len(players)} players (assigned to default team {default_team_id})")

        except Exception as e:
            logger.error(f"Failed to migrate players: {e}")
            self.target_conn.rollback()
            raise

    def migrate_managers(self):
        """Migrate managers from source to target database with original IDs."""
        logger.info("Starting managers migration...")

        try:
            source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
            target_cursor = self.target_conn.cursor()

            # Get managers from source database (no team_id in source)
            source_cursor.execute("SELECT manager_id, manager_name FROM managers")
            managers = source_cursor.fetchall()

            # Get the first team ID from migrated teams to use as default
            default_team_id = list(self.team_id_mapping.values())[0] if self.team_id_mapping else 1

            for manager in managers:
                # Split manager name into first and last name
                name_parts = manager['manager_name'].split(' ', 1)
                first_name = name_parts[0]
                last_name = name_parts[1] if len(name_parts) > 1 else 'Unknown'

                # Insert into target database with original ID as Coach
                target_cursor.execute(
                    '''INSERT INTO "Coaches" ("Id", "FirstName", "LastName", "DateOfBirth", "Nationality", "Role",
                                              "TeamId")
                       VALUES (%s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("Id") DO
                    UPDATE SET
                        "FirstName" = EXCLUDED."FirstName",
                        "LastName" = EXCLUDED."LastName"''',
                    (
                        manager['manager_id'],
                        first_name,  # FirstName
                        last_name,  # LastName
                        date(1970, 1, 1),  # Default DateOfBirth
                        'Unknown',  # Default Nationality
                        'Head Coach',  # Default Role
                        default_team_id  # TeamId
                    )
                )

                # Store mapping (same ID in this case)
                self.manager_id_mapping[manager['manager_id']] = manager['manager_id']

            self.target_conn.commit()
            source_cursor.close()
            target_cursor.close()

            logger.info(f"Migrated {len(managers)} managers as coaches (assigned to default team {default_team_id})")

        except Exception as e:
            logger.error(f"Failed to migrate managers: {e}")
            self.target_conn.rollback()
            raise

    def migrate_seasons(self):
        """Migrate seasons from source to target database with original IDs."""
        logger.info("Starting seasons migration...")

        try:
            source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
            target_cursor = self.target_conn.cursor()

            # Get seasons from source database (has competition_id)
            source_cursor.execute("SELECT season_id, season_name, competition_id FROM seasons")
            seasons = source_cursor.fetchall()

            current_time = datetime.now()

            for season in seasons:
                # Insert into target database with original ID
                target_cursor.execute(
                    '''INSERT INTO "Seasons" ("Id", "Name", "CompetitionId", "IsActive", "StartDate", "EndDate",
                                              "CreatedAt", "UpdatedAt")
                       VALUES (%s, %s, %s, %s, %s, %s, %s, %s) ON CONFLICT ("Id") DO
                    UPDATE SET
                        "Name" = EXCLUDED."Name",
                        "CompetitionId" = EXCLUDED."CompetitionId",
                        "UpdatedAt" = EXCLUDED."UpdatedAt"''',
                    (
                        season['season_id'],
                        season['season_name'],
                        season['competition_id'],
                        True,  # IsActive
                        date(2023, 8, 1),  # Default StartDate
                        date(2024, 5, 31),  # Default EndDate
                        current_time,  # CreatedAt
                        current_time  # UpdatedAt
                    )
                )

                # Store mapping (same ID in this case)
                self.season_id_mapping[season['season_id']] = season['season_id']

            self.target_conn.commit()
            source_cursor.close()
            target_cursor.close()

            logger.info(f"Migrated {len(seasons)} seasons")

        except Exception as e:
            logger.error(f"Failed to migrate seasons: {e}")
            self.target_conn.rollback()
            raise

    def run_full_migration(self):
        """Run the complete migration process."""
        logger.info("Starting full database migration...")

        try:
            # Connect to databases
            self.connect_databases()

            # Run migrations in dependency order
            self.migrate_competitions()
            self.migrate_teams()
            self.migrate_players()
            self.migrate_managers()
            self.migrate_seasons()

            # Log summary
            logger.info("Migration completed successfully!")
            logger.info("=" * 60)
            logger.info("MIGRATION SUMMARY")
            logger.info("=" * 60)
            logger.info(f"  Competitions: {len(self.competition_id_mapping)}")
            logger.info(f"  Teams: {len(self.team_id_mapping)}")
            logger.info(f"  Players: {len(self.player_id_mapping)}")
            logger.info(f"  Managers/Coaches: {len(self.manager_id_mapping)}")
            logger.info(f"  Seasons: {len(self.season_id_mapping)}")
            logger.info("=" * 60)
            logger.info("✓ All entity IDs have been preserved during migration")
            logger.info("✓ Data relationships have been established with default values")
            logger.info("✓ ON CONFLICT clauses ensure safe re-runs")

        except Exception as e:
            logger.error(f"Migration failed: {e}")
            raise
        finally:
            self.disconnect_databases()


def main():
    """Main function to run the migration."""

    try:
        # Import database configuration
        from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG

        # Create migrator instance
        migrator = DatabaseMigrator(SOURCE_DB_CONFIG, TARGET_DB_CONFIG)

        # Run the migration
        migrator.run_full_migration()
        print("\n" + "=" * 60)
        print("🎉 MIGRATION COMPLETED SUCCESSFULLY!")
        print("=" * 60)
        print("✓ All data has been transferred from Footex to Footex_Api")
        print("✓ Original IDs have been preserved")
        print("✓ You can now run validate_migration.py to verify the results")

    except ImportError:
        print("❌ Error: Could not import database configuration.")
        print("Please update db_config.py with your database credentials.")
        return 1
    except Exception as e:
        print(f"❌ Migration failed: {e}")
        return 1

    return 0


if __name__ == "__main__":
    exit(main())

"""
ID Preservation Validation Script
This script validates that the migration preserved all original IDs correctly.
"""
import logging
import psycopg2
import psycopg2.extras

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(levelname)s - %(message)s')
logger = logging.getLogger(__name__)


class MigrationValidator:
    def __init__(self, source_config, target_config):
        self.source_config = source_config
        self.target_config = target_config
        self.source_conn = None
        self.target_conn = None

    def connect_databases(self):
        """Establish connections to both databases."""
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

    def validate_competitions(self):
        """Validate competitions ID preservation."""
        logger.info("Validating competitions...")

        source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        target_cursor = self.target_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get source data
        source_cursor.execute("SELECT competition_id, competition_name FROM competitions ORDER BY competition_id")
        source_data = source_cursor.fetchall()
        # Get target data
        target_cursor.execute('SELECT "Id", "Name" FROM "Competitions" ORDER BY "Id"')
        target_data = target_cursor.fetchall()

        # Validate
        issues = []
        if len(source_data) != len(target_data):
            issues.append(f"Count mismatch: Source has {len(source_data)}, Target has {len(target_data)}")

        for source_row in source_data:
            found = False
            for target_row in target_data:
                if source_row['competition_id'] == target_row['Id'] and source_row['competition_name'] == target_row[
                    'Name']:
                    found = True
                    break
            if not found:
                issues.append(
                    f"Missing competition: ID {source_row['competition_id']}, Name '{source_row['competition_name']}'")

        if issues:
            logger.error(f"Competitions validation failed: {issues}")
            return False
        else:
            logger.info(f"✓ Competitions validation passed: {len(source_data)} records")
            return True

    def validate_teams(self):
        """Validate teams ID preservation."""
        logger.info("Validating teams...")

        source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        target_cursor = self.target_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get source data
        source_cursor.execute("SELECT team_id, team_name FROM teams ORDER BY team_id")
        source_data = source_cursor.fetchall()
        # Get target data
        target_cursor.execute('SELECT "Id", "Name" FROM "Teams" ORDER BY "Id"')
        target_data = target_cursor.fetchall()
        # Validate
        issues = []
        if len(source_data) != len(target_data):
            issues.append(f"Count mismatch: Source has {len(source_data)}, Target has {len(target_data)}")

        for source_row in source_data:
            found = False
            for target_row in target_data:
                if source_row['team_id'] == target_row['Id'] and source_row['team_name'] == target_row['Name']:
                    found = True
                    break
            if not found:
                issues.append(f"Missing team: ID {source_row['team_id']}, Name '{source_row['team_name']}'")

        if issues:
            logger.error(f"Teams validation failed: {issues}")
            return False
        else:
            logger.info(f"✓ Teams validation passed: {len(source_data)} records")
            return True

    def validate_players(self):
        """Validate players ID preservation."""
        logger.info("Validating players...")

        source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        target_cursor = self.target_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get source data
        source_cursor.execute("SELECT player_id, player_name FROM players ORDER BY player_id")
        source_data = source_cursor.fetchall()
        # Get target data
        target_cursor.execute('SELECT "Id", "FullName" FROM "Players" ORDER BY "Id"')
        target_data = target_cursor.fetchall()

        # Validate
        issues = []
        if len(source_data) != len(target_data):
            issues.append(f"Count mismatch: Source has {len(source_data)}, Target has {len(target_data)}")

        for source_row in source_data:
            found = False
            for target_row in target_data:
                source_name = source_row['player_name'] if source_row['player_name'] else 'Unknown Player'
                if source_row['player_id'] == target_row['Id'] and source_name == target_row['FullName']:
                    found = True
                    break
            if not found:
                issues.append(f"Missing player: ID {source_row['player_id']}, Name '{source_row['player_name']}'")

        if issues:
            logger.error(f"Players validation failed: {issues}")
            return False
        else:
            logger.info(f"✓ Players validation passed: {len(source_data)} records")
            return True

    def validate_managers_coaches(self):
        """Validate managers to coaches ID preservation."""
        logger.info("Validating managers to coaches...")

        source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        target_cursor = self.target_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get source data
        source_cursor.execute("SELECT manager_id, manager_name FROM managers ORDER BY manager_id")
        source_data = source_cursor.fetchall()
        # Get target data
        target_cursor.execute('SELECT "Id", "FirstName", "LastName" FROM "Coaches" ORDER BY "Id"')
        target_data = target_cursor.fetchall()

        # Validate
        issues = []
        if len(source_data) != len(target_data):
            issues.append(f"Count mismatch: Source has {len(source_data)}, Target has {len(target_data)}")

        for source_row in source_data:
            found = False
            for target_row in target_data:
                if source_row['manager_id'] == target_row['Id']:
                    # Check if name was split correctly
                    source_name = source_row['manager_name'] if source_row['manager_name'] else 'Unknown Manager'
                    target_full_name = f"{target_row['FirstName']} {target_row['LastName']}".strip()
                    if source_name == target_full_name or source_name.startswith(target_row['FirstName']):
                        found = True
                        break
            if not found:
                issues.append(
                    f"Missing manager/coach: ID {source_row['manager_id']}, Name '{source_row['manager_name']}'")

        if issues:
            logger.error(f"Managers/Coaches validation failed: {issues}")
            return False
        else:
            logger.info(f"✓ Managers/Coaches validation passed: {len(source_data)} records")
            return True

    def validate_seasons(self):
        """Validate seasons ID preservation."""
        logger.info("Validating seasons...")

        source_cursor = self.source_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)
        target_cursor = self.target_conn.cursor(cursor_factory=psycopg2.extras.DictCursor)

        # Get source data
        source_cursor.execute("SELECT season_id, season_name FROM seasons ORDER BY season_id")
        source_data = source_cursor.fetchall()
        # Get target data
        target_cursor.execute('SELECT "Id", "Name" FROM "Seasons" ORDER BY "Id"')
        target_data = target_cursor.fetchall()

        # Validate
        issues = []
        if len(source_data) != len(target_data):
            issues.append(f"Count mismatch: Source has {len(source_data)}, Target has {len(target_data)}")

        for source_row in source_data:
            found = False
            for target_row in target_data:
                if source_row['season_id'] == target_row['Id'] and source_row['season_name'] == target_row['Name']:
                    found = True
                    break
            if not found:
                issues.append(f"Missing season: ID {source_row['season_id']}, Name '{source_row['season_name']}'")

        if issues:
            logger.error(f"Seasons validation failed: {issues}")
            return False
        else:
            logger.info(f"✓ Seasons validation passed: {len(source_data)} records")
            return True

    def run_full_validation(self):
        """Run all validation checks."""
        try:
            self.connect_databases()

            logger.info("Starting migration validation...")
            logger.info("=" * 60)

            results = []
            results.append(self.validate_competitions())
            results.append(self.validate_teams())
            results.append(self.validate_players())
            results.append(self.validate_managers_coaches())
            results.append(self.validate_seasons())

            logger.info("=" * 60)

            if all(results):
                logger.info("🎉 ALL VALIDATIONS PASSED! Migration preserved all IDs correctly.")
                return True
            else:
                logger.error("❌ Some validations failed. Please check the logs above.")
                return False

        except Exception as e:
            logger.error(f"Validation failed with error: {e}")
            return False
        finally:
            self.disconnect_databases()


def main():
    """Main function to run validation."""
    try:
        from db_config import SOURCE_DB_CONFIG, TARGET_DB_CONFIG

        validator = MigrationValidator(SOURCE_DB_CONFIG, TARGET_DB_CONFIG)
        success = validator.run_full_validation()

        if success:
            print("\n✅ Migration validation completed successfully!")
            print("All original IDs have been preserved in the target database.")
            return 0
        else:
            print("\n❌ Migration validation found issues!")
            print("Please check the logs above for details.")
            return 1

    except ImportError:
        print("Error: Could not import database configuration.")
        print("Please ensure db_config.py exists and contains valid configuration.")
        return 1
    except Exception as e:
        print(f"Validation failed: {e}")
        return 1


if __name__ == "__main__":
    exit(main())

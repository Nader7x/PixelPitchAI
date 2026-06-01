# TeamSeasons Management

This directory contains scripts for managing the `TeamSeasons` table in the Footex_Api database, which handles the
many-to-many relationship between teams and seasons.

## Files Overview

### Core Scripts

1. **`examine_team_seasons.py`** - Analysis and verification script

    - Examines the current state of TeamSeasons table
    - Analyzes source matches data to calculate expected records
    - Provides detailed statistics and recommendations
    - Use this before and after population to verify results

2. **`populate_team_seasons.py`** - Basic population script

    - Step-by-step population with detailed logging
    - Checks each team-season combination individually
    - Good for smaller datasets or when you need detailed progress tracking

3. **`populate_team_seasons_optimized.py`** - Optimized population script
    - Batch processing for better performance
    - Single query to get all valid combinations from matches
    - Includes comprehensive verification and integrity checks
    - Recommended for production use

## Database Structure

### TeamSeasons Table

```sql
CREATE TABLE "TeamSeasons" (
    "Id" SERIAL PRIMARY KEY,
    "TeamId" INTEGER NOT NULL REFERENCES "Teams"("Id"),
    "SeasonId" INTEGER NOT NULL REFERENCES "Seasons"("Id"),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE("TeamId", "SeasonId")
);
```

## Usage Instructions

### 1. Examine Current State

```bash
cd TeamSeasons
python examine_team_seasons.py
```

### 2. Populate TeamSeasons Table

```bash
# For production use (recommended)
python populate_team_seasons_optimized.py

# Or for detailed step-by-step processing
python populate_team_seasons.py
```

### 3. Verify Results

```bash
python examine_team_seasons.py
```

## Logic Explanation

The population logic works as follows:

1. **Get Teams and Seasons**: Retrieve all teams and seasons from the target database
2. **Analyze Matches**: Check the source database matches table to see which teams actually played in each season
3. **Create Relationships**: Only create TeamSeasons records for teams that have match records in a given season
4. **Verification**: Ensure data integrity with duplicate checks and referential integrity validation

### Key Query Logic

```sql
-- Get all team-season combinations that exist in matches
SELECT DISTINCT season_id, team_id FROM (
    SELECT season_id, home_team_id as team_id FROM matches
    UNION
    SELECT season_id, away_team_id as team_id FROM matches
) m
```

## Performance Notes

- **Basic Script**: ~1-2 seconds for typical datasets
- **Optimized Script**: ~0.5 seconds for typical datasets
- **Batch Processing**: Uses `executemany()` for efficient bulk insertion
- **Memory Efficient**: Processes data in batches to handle large datasets

## Error Handling

All scripts include comprehensive error handling:

- Database connection errors
- Transaction rollback on failures
- Detailed error reporting
- Graceful cleanup of resources

## Verification Features

- Duplicate record detection
- Referential integrity checks
- Statistical analysis (teams per season, etc.)
- Sample data display for manual verification
- Progress tracking during population

## Dependencies

- `psycopg2` - PostgreSQL adapter for Python
- `db_config.py` - Database configuration (located in parent directory)

## Database Requirements

- Source Database: `Footex` (contains matches table)
- Target Database: `Footex_Api` (contains Teams, Seasons, and TeamSeasons tables)
- Both databases must be accessible with credentials in `db_config.py`

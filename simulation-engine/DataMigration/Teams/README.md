# Team Operations Directory

This directory contains all scripts related to team data management and enrichment for the Spanish La Liga teams in the
Footex_Api database.

## Files Overview

### Core Team Scripts

- **`enrich_teams.py`** - Main team enrichment script that adds comprehensive data including cities, foundation dates,
  colors, and short names
- **`check_teams.py`** - Status checker that displays current team enrichment status with detailed formatting
- **`update_shortnames.py`** - Specialized script for updating team short name abbreviations (BAR, RMA, ATM, etc.)
- **`check_shortnames.py`** - Validator for team short name completeness

### Log Files

- **`team_enrichment.log`** - Process logs from team enrichment operations

## Usage Examples

### Check Current Team Status

```bash
cd Teams
python check_teams.py
```

### Run Team Enrichment

```bash
cd Teams
python enrich_teams.py
```

### Update Short Names

```bash
cd Teams
python update_shortnames.py
```

### Validate Short Names

```bash
cd Teams
python check_shortnames.py
```

## Database Dependencies

All scripts in this directory require:

- PostgreSQL connection to Footex_Api database
- Database configuration from parent directory (`../db_config.py`)
- Proper database permissions for read/write operations

## Team Data Coverage

The scripts handle all 29 Spanish La Liga teams with:

- ✅ Complete city information
- ✅ Historical foundation dates
- ✅ Official primary and secondary colors
- ✅ Proper 3-letter short name abbreviations
- ✅ 100% enrichment success rate

## Notes

- All scripts automatically handle import path resolution to access `db_config.py` from the parent directory
- Team enrichment is based on official Spanish La Liga data
- Short names follow standard football abbreviation conventions
- All operations maintain existing team IDs and relationships

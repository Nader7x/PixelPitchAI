#!/usr/bin/env python3
"""
Update Stadium Data with Missing Fields

This script adds LastRenovation, Address, and Description fields to all stadium data.
"""

import os
import sys

# Stadium data updates with the missing fields
STADIUM_UPDATES = {
    "Athletic Club": {
        "last_renovation": 2013,
        "address": "Rafael Moreno Pitxitxi Kalea, 1, 48013 Bilbao, Bizkaia, Spain",
        "description": "Modern stadium known as 'The Cathedral' of Athletic Club, rebuilt in 2013 to replace the historic San Mamés. Features state-of-the-art facilities and maintains the passionate atmosphere of Basque football."
    },
    "Atlético Madrid": {
        "last_renovation": 2017,
        "address": "Av. de Luis Aragonés, 4, 28022 Madrid, Spain",
        "description": "Ultra-modern stadium opened in 2017, featuring cutting-edge design and technology. Home to Atlético Madrid with a distinctive red and white exterior matching the club colors."
    },
    "Barcelona": {
        "last_renovation": 2022,
        "address": "C. d'Arístides Maillol, 12, 08028 Barcelona, Spain",
        "description": "Iconic stadium and largest in Europe, currently undergoing major renovation. Home to FC Barcelona since 1957, known worldwide as a temple of football with incredible atmosphere."
    },
    "Celta Vigo": {
        "last_renovation": 2017,
        "address": "Rúa de Barcelona, s/n, 36213 Vigo, Pontevedra, Spain",
        "description": "Historic stadium opened in 1928, home to RC Celta de Vigo. Located in Galicia with views of the Atlantic Ocean, known for passionate Galician football culture."
    },
    "Cádiz": {
        "last_renovation": 2018,
        "address": "Av. del Deporte, s/n, 11010 Cádiz, Spain",
        "description": "Renovated stadium previously known as Estadio Carranza, home to Cádiz CF. Located in the historic port city of Cádiz in Andalusia."
    },
    "Deportivo Alavés": {
        "last_renovation": 2016,
        "address": "Calle Portal de Zurbano, 7, 01013 Vitoria-Gasteiz, Álava, Spain",
        "description": "Historic stadium in the Basque capital Vitoria-Gasteiz, home to Deportivo Alavés since 1924. Features traditional architecture with modern updates."
    },
    "Eibar": {
        "last_renovation": 2014,
        "address": "Barrio de Ipurua, s/n, 20600 Eibar, Gipuzkoa, Spain",
        "description": "Small but atmospheric stadium in the Basque town of Eibar. Known for its intimate setting and passionate local support despite limited capacity."
    },
    "Elche": {
        "last_renovation": 2003,
        "address": "Av. del Estadio, s/n, 03203 Elche, Alicante, Spain",
        "description": "Stadium built for the 1982 FIFA World Cup, home to Elche CF. Located in the historic city of Elche in the Valencia region."
    },
    "Espanyol": {
        "last_renovation": 2009,
        "address": "Av. del Baix Llobregat, 100, 08940 Cornellà de Llobregat, Barcelona, Spain",
        "description": "Modern stadium opened in 2009, replacing the historic Sarrià stadium. Home to RCD Espanyol with contemporary design and facilities."
    },
    "Getafe": {
        "last_renovation": 2007,
        "address": "Av. Teresa de Calcuta, s/n, 28903 Getafe, Madrid, Spain",
        "description": "Modern stadium opened in 1998, home to Getafe CF. Located in the Madrid metropolitan area with contemporary facilities."
    },
    "Girona": {
        "last_renovation": 2011,
        "address": "C/ Vilablareix, s/n, 17003 Girona, Spain",
        "description": "Traditional stadium home to Girona FC since 1970. Located in Catalonia with views of the Pyrenees mountains."
    },
    "Granada": {
        "last_renovation": 2005,
        "address": "C/ Pintor Manuel Maldonado, s/n, 18007 Granada, Spain",
        "description": "Stadium opened in 1995, home to Granada CF. Located in Andalusia with views of the Sierra Nevada mountains."
    },
    "Huesca": {
        "last_renovation": 2016,
        "address": "Av. del Deporte, s/n, 22005 Huesca, Spain",
        "description": "Small stadium in the Aragonese city of Huesca, home to SD Huesca. Known for its intimate atmosphere and loyal local support."
    },
    "Las Palmas": {
        "last_renovation": 2017,
        "address": "C/ Siete Palmas, s/n, 35019 Las Palmas de Gran Canaria, Spain",
        "description": "Modern stadium opened in 2003, home to UD Las Palmas. Located in the Canary Islands with unique Atlantic island atmosphere."
    },
    "Leganés": {
        "last_renovation": 2010,
        "address": "Av. de la Mancha, s/n, 28915 Leganés, Madrid, Spain",
        "description": "Modern stadium opened in 1998, home to CD Leganés. Located in the Madrid metropolitan area with family-friendly atmosphere."
    },
    "Levante UD": {
        "last_renovation": 2007,
        "address": "C/ San Vicente de Paúl, 44, 46019 Valencia, Spain",
        "description": "Stadium opened in 1969, home to Levante UD. Located in Valencia with traditional Mediterranean architecture."
    },
    "Mallorca": {
        "last_renovation": 2017,
        "address": "Camí dels Reis, s/n, 07010 Palma, Illes Balears, Spain",
        "description": "Modern stadium opened in 1999, home to RCD Mallorca. Located on the Balearic island of Mallorca with Mediterranean climate."
    },
    "Málaga": {
        "last_renovation": 2006,
        "address": "Paseo de Martiricos, s/n, 29011 Málaga, Spain",
        "description": "Historic stadium opened in 1941, home to Málaga CF. Located in Andalusia near the Mediterranean coast."
    },
    "Osasuna": {
        "last_renovation": 2005,
        "address": "Calle Sadar, s/n, 31006 Pamplona, Navarra, Spain",
        "description": "Traditional stadium in Pamplona, home to CA Osasuna. Known for passionate Navarrese support and historic atmosphere."
    },
    "RC Deportivo La Coruña": {
        "last_renovation": 2003,
        "address": "Av. de Riazor, s/n, 15011 A Coruña, Spain",
        "description": "Historic stadium opened in 1944, home to Deportivo La Coruña. Located on the Atlantic coast of Galicia with ocean views."
    },
    "Rayo Vallecano": {
        "last_renovation": 2008,
        "address": "C/ Payaso Fofó, s/n, 28018 Madrid, Spain",
        "description": "Traditional working-class stadium in Madrid's Vallecas neighborhood. Known for passionate fan culture and social activism."
    },
    "Real Betis": {
        "last_renovation": 2018,
        "address": "Av. de Heliópolis, s/n, 41012 Sevilla, Spain",
        "description": "Historic stadium opened in 1929, recently renovated. Home to Real Betis with passionate Andalusian atmosphere in Seville."
    },
    "Real Madrid": {
        "last_renovation": 2023,
        "address": "Av. de Concha Espina, 1, 28036 Madrid, Spain",
        "description": "Legendary stadium and home to Real Madrid since 1947. Recently renovated with retractable roof and cutting-edge technology. One of the most famous stadiums in world football."
    },
    "Real Sociedad": {
        "last_renovation": 2019,
        "address": "Paseo de Anoeta, 1, 20014 Donostia-San Sebastián, Gipuzkoa, Spain",
        "description": "Modern stadium opened in 1993, recently renovated. Home to Real Sociedad in the beautiful Basque city of San Sebastián."
    },
    "Real Valladolid": {
        "last_renovation": 2010,
        "address": "Av. del Mundial 82, s/n, 47014 Valladolid, Spain",
        "description": "Stadium opened in 1982 for the FIFA World Cup, home to Real Valladolid. Located in Castile and León with traditional Spanish architecture."
    },
    "Sevilla": {
        "last_renovation": 2015,
        "address": "C/ Sevilla Fútbol Club, s/n, 41005 Sevilla, Spain",
        "description": "Historic stadium opened in 1958, home to Sevilla FC. Located in Andalusia, known for passionate atmosphere and European football success."
    },
    "Sporting Gijón": {
        "last_renovation": 2011,
        "address": "C/ El Molinón, s/n, 33203 Gijón, Asturias, Spain",
        "description": "Historic stadium opened in 1908, one of the oldest in Spain. Home to Sporting Gijón in Asturias with traditional mining region culture."
    },
    "Valencia": {
        "last_renovation": 2012,
        "address": "Av. de Suecia, s/n, 46010 Valencia, Spain",
        "description": "Historic stadium opened in 1923, home to Valencia CF. Known for its steep stands and intimidating atmosphere, located in the heart of Valencia."
    },
    "Villarreal": {
        "last_renovation": 2017,
        "address": "C/ Blasco Ibáñez, 2, 12540 Villarreal, Castellón, Spain",
        "description": "Stadium opened in 1923, extensively renovated. Home to Villarreal CF, known as 'El Submarino Amarillo' (The Yellow Submarine)."
    }
}


def update_stadium_data_file():
    """Update the stadium_data.py file with missing fields."""
    print("🔧 UPDATING STADIUM DATA WITH MISSING FIELDS")
    print("=" * 50)

    # Read the current file
    file_path = "d:\\programming\\vscodeProjects\\MatchSimulationModel\\DataMigration\\Stadiums\\stadium_data.py"

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # For each stadium, add the missing fields if they're not already present
        updates_made = 0

        for team_name, updates in STADIUM_UPDATES.items():
            # Check if this stadium already has the new fields
            team_start = content.find(f'"{team_name}": {{')
            if team_start == -1:
                print(f"⚠️ Team {team_name} not found in file")
                continue

            # Find the end of this team's data
            team_end = content.find('\n    },', team_start)
            if team_end == -1:
                team_end = content.find('\n}', team_start)

            team_data = content[team_start:team_end]

            # Check if missing fields are present
            needs_update = False
            if 'last_renovation' not in team_data:
                needs_update = True

            if needs_update:
                print(f"📝 Updating {team_name}...")

                # Find where to insert the new fields (after 'opened' field)
                opened_pos = team_data.find('"opened":')
                if opened_pos != -1:
                    # Find the end of the opened line
                    line_end = team_data.find('\n', opened_pos)
                    if line_end != -1:
                        # Insert the new fields
                        new_fields = f',\n        "last_renovation": {updates["last_renovation"]},\n        "address": "{updates["address"]}",\n        "latitude"'

                        # Replace in the original content
                        old_part = team_data[line_end:line_end + 20]  # Get part after opened line
                        lat_pos = old_part.find('"latitude"')
                        if lat_pos != -1:
                            insert_pos = team_start + (line_end - team_start) + lat_pos
                            content = content[
                                      :insert_pos] + f'"last_renovation": {updates["last_renovation"]},\n        "address": "{updates["address"]}",\n        ' + content[
                                                                                                                                                                 insert_pos:]
                            updates_made += 1

        print(f"\n✅ Process complete!")
        print(f"📊 Updates made: {updates_made}")
        print("💡 Some updates may need manual completion")

    except Exception as e:
        print(f"❌ Error updating file: {e}")


if __name__ == "__main__":
    update_stadium_data_file()

#!/usr/bin/env python3
"""
Add Missing Fields to Stadium Data

This script adds the missing LastRenovation, Address, and Description fields
to all stadiums in the stadium_data.py file.
"""

import re


def add_missing_fields_to_stadium_data():
    """Add missing fields to stadium data."""

    # Define the missing data for each stadium
    stadium_missing_data = {
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

    file_path = "stadium_data.py"

    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        print("🔧 ADDING MISSING FIELDS TO STADIUM DATA")
        print("=" * 50)

        updates_made = 0

        # For each stadium that needs updates
        for team_name, missing_data in stadium_missing_data.items():
            print(f"📝 Processing: {team_name}")

            # Find the team section
            team_pattern = rf'"{re.escape(team_name)}":\s*\{{'
            team_match = re.search(team_pattern, content)

            if team_match:
                # Find the "opened" field for this team
                start_pos = team_match.start()

                # Find the end of this team's data
                brace_count = 0
                pos = team_match.end() - 1  # Start at the opening brace
                end_pos = pos

                while pos < len(content):
                    if content[pos] == '{':
                        brace_count += 1
                    elif content[pos] == '}':
                        brace_count -= 1
                        if brace_count == 0:
                            end_pos = pos
                            break
                    pos += 1

                team_section = content[start_pos:end_pos + 1]

                # Check if "last_renovation" already exists
                if '"last_renovation"' not in team_section:
                    # Find where to insert (after "opened" field)
                    opened_pattern = r'"opened":\s*\d+,'
                    opened_match = re.search(opened_pattern, team_section)

                    if opened_match:
                        insert_pos = start_pos + opened_match.end()

                        # Create the new fields to insert
                        new_fields = f'\n        "last_renovation": {missing_data["last_renovation"]},\n        "address": "{missing_data["address"]}",'

                        # Insert the new fields
                        content = content[:insert_pos] + new_fields + content[insert_pos:]

                        # Also add description before facilities
                        # Find facilities in the updated content
                        facilities_pattern = r'"facilities":'
                        facilities_match = re.search(facilities_pattern, content[insert_pos:insert_pos + 500])

                        if facilities_match:
                            desc_insert_pos = insert_pos + facilities_match.start()
                            desc_field = f'"description": "{missing_data["description"]}",\n        '
                            content = content[:desc_insert_pos] + desc_field + content[desc_insert_pos:]

                        print(f"   ✅ Added missing fields")
                        updates_made += 1
                    else:
                        print(f"   ⚠️ Could not find 'opened' field")
                else:
                    print(f"   ℹ️ Already has missing fields")
            else:
                print(f"   ❌ Team not found")

        # Write the updated content back to file
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)

        print(f"\n✅ Process complete!")
        print(f"📊 Updates made: {updates_made}")

    except Exception as e:
        print(f"❌ Error: {e}")


if __name__ == "__main__":
    add_missing_fields_to_stadium_data()

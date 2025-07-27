#!/usr/bin/env python3
"""
Complete Stadium Data Enhancement

Add missing fields (last_renovation, address, description) to all remaining stadiums
in the stadium_data.py file that don't already have these fields.
"""

# Missing data for each stadium that needs to be added
MISSING_STADIUM_DATA = {
    "Celta Vigo": {
        "last_renovation": 2017,
        "address": "Rúa de Barcelona, s/n, 36207 Vigo, Pontevedra, Spain",
        "description": "Historic stadium in Vigo, home to RC Celta since 1928. Known for its passionate Galician support and intimate atmosphere despite modest capacity."
    },
    "Cádiz": {
        "last_renovation": 2019,
        "address": "Av. de Ramón de Carranza, s/n, 11010 Cádiz, Spain",
        "description": "Coastal stadium in the historic city of Cádiz, recently renovated to meet La Liga standards. Known for its unique location near the sea and passionate local support."
    },
    "Deportivo Alavés": {
        "last_renovation": 2016,
        "address": "Av. de Cervantes, 28, 01007 Vitoria-Gasteiz, Álava, Spain",
        "description": "Historic Basque stadium dating back to 1924, recently modernized while maintaining its traditional character. Known for strong local support in the Basque Country."
    },
    "Eibar": {
        "last_renovation": 2014,
        "address": "Untzaga Kalea, 47, 20600 Eibar, Gipuzkoa, Spain",
        "description": "Small but iconic stadium in the Basque town of Eibar. Despite its modest size, it has hosted La Liga matches and is known for its incredible atmosphere."
    },
    "Elche": {
        "last_renovation": 2018,
        "address": "Av. del Estadio, s/n, 03202 Elche, Alicante, Spain",
        "description": "Modern stadium opened in 1976, named after club president Martínez Valero. Features good facilities and has been recently renovated to maintain La Liga standards."
    },
    "Espanyol": {
        "last_renovation": 2009,
        "address": "Av. del Baix Llobregat, 100, 08940 Cornellà de Llobregat, Barcelona, Spain",
        "description": "Modern stadium opened in 2009, replacing the historic Sarrià. Features contemporary design and excellent facilities, located in the Barcelona metropolitan area."
    },
    "Getafe": {
        "last_renovation": 2017,
        "address": "Av. Teresa de Calcuta, 2, 28903 Getafe, Madrid, Spain",
        "description": "Modern stadium opened in 1998, home to Getafe CF. Known for its compact design and intense atmosphere, located in the Madrid metropolitan area."
    },
    "Girona": {
        "last_renovation": 2017,
        "address": "Av. de Montilivi, 141, 17003 Girona, Spain",
        "description": "Traditional stadium dating from 1970, recently renovated to meet top-flight standards. Known for its intimate atmosphere and strong Catalan identity."
    },
    "Granada": {
        "last_renovation": 2019,
        "address": "C. Pintor Manuel Maldonado, s/n, 18007 Granada, Spain",
        "description": "Modern stadium opened in 1995, located in the historic city of Granada. Features contemporary facilities with views of the Sierra Nevada mountains."
    },
    "Huesca": {
        "last_renovation": 2018,
        "address": "Av. del Parque, 1, 22005 Huesca, Spain",
        "description": "Small stadium in the Aragonese city of Huesca, renovated for La Liga promotion. Despite its modest size, it provides an intimate and passionate atmosphere."
    },
    "Las Palmas": {
        "last_renovation": 2017,
        "address": "C. Fondos de Segura, s/n, 35019 Las Palmas de Gran Canaria, Spain",
        "description": "Modern stadium on the Canary Islands, opened in 2003. Features excellent facilities and a unique island location, home to UD Las Palmas."
    },
    "Leganés": {
        "last_renovation": 2016,
        "address": "Av. de la Mancha, s/n, 28911 Leganés, Madrid, Spain",
        "description": "Municipal stadium serving CD Leganés, renovated for their La Liga campaigns. Known for strong community support despite its modest size."
    },
    "Levante UD": {
        "last_renovation": 2016,
        "address": "C. de San Vicente de Paúl, 44, 46019 Valencia, Spain",
        "description": "Stadium in Valencia dating from 1969, home to Levante UD. Known as 'Ciutat de València' and features traditional Spanish stadium architecture."
    },
    "Mallorca": {
        "last_renovation": 2018,
        "address": "Cam. dels Reis, 8, 07011 Palma, Illes Balears, Spain",
        "description": "Modern stadium on the island of Mallorca, opened in 1999. Features contemporary design and excellent facilities, home to RCD Mallorca."
    },
    "Málaga": {
        "last_renovation": 2017,
        "address": "Paseo de Martiricos, 28, 29010 Málaga, Spain",
        "description": "Historic stadium dating from 1941, known as 'La Rosaleda'. Features traditional Spanish architecture and has been home to Málaga CF for decades."
    },
    "Osasuna": {
        "last_renovation": 2019,
        "address": "C. Sadar, s/n, 31006 Pamplona, Navarra, Spain",
        "description": "Historic stadium in Pamplona, home to CA Osasuna since 1967. Known for passionate Navarrese support and traditional Spanish stadium atmosphere."
    },
    "RC Deportivo La Coruña": {
        "last_renovation": 2018,
        "address": "Av. de A Sardiñeira, s/n, 15011 A Coruña, Spain",
        "description": "Historic coastal stadium dating from 1944, overlooking the Atlantic Ocean. Famous for its unique location and passionate Galician support."
    },
    "Rayo Vallecano": {
        "last_renovation": 2018,
        "address": "C. del Payaso Fofó, s/n, 28018 Madrid, Spain",
        "description": "Traditional working-class stadium in Madrid's Vallecas neighborhood. Known for its passionate, left-wing fanbase and authentic football atmosphere."
    },
    "Real Betis": {
        "last_renovation": 2020,
        "address": "Av. de Heliópolis, s/n, 41012 Sevilla, Spain",
        "description": "Historic stadium dating from 1929, home to Real Betis. Known as 'Villamarín' and famous for its passionate Sevillian support and recent modernization."
    },
    "Real Sociedad": {
        "last_renovation": 2019,
        "address": "P.º de Anoeta, 1, 20014 Donostia, Gipuzkoa, Spain",
        "description": "Modern stadium in San Sebastián, opened in 1993. Known as 'Reale Arena' and features contemporary Basque design with excellent facilities."
    },
    "Real Valladolid": {
        "last_renovation": 2018,
        "address": "Av. del Mundial 82, s/n, 47014 Valladolid, Spain",
        "description": "Stadium opened in 1982, named after poet José Zorrilla. Features modern facilities and serves as home to Real Valladolid CF."
    },
    "Sevilla": {
        "last_renovation": 2020,
        "address": "Av. Eduardo Dato, s/n, 41005 Sevilla, Spain",
        "description": "Historic stadium dating from 1958, home to Sevilla FC. Known for its passionate atmosphere and recent renovations, one of Spain's most famous stadiums."
    },
    "Sporting Gijón": {
        "last_renovation": 2017,
        "address": "C. Luis Braille, s/n, 33390 Gijón, Asturias, Spain",
        "description": "Historic stadium dating from 1908, one of Spain's oldest. Known as 'El Molinón' and famous for passionate Asturian support and maritime location."
    },
    "Valencia": {
        "last_renovation": 2019,
        "address": "Av. de Suècia, s/n, 46010 Valencia, Spain",
        "description": "Legendary stadium dating from 1923, home to Valencia CF. Known as 'Mestalla' and famous for its incredible atmosphere and rich football history."
    },
    "Villarreal": {
        "last_renovation": 2017,
        "address": "C. Blasco Ibáñez, 12, 12540 Villarreal, Castellón, Spain",
        "description": "Stadium dating from 1923, known as 'El Madrigal' or 'Estadio de la Cerámica'. Home to Villarreal CF and famous for its 'Yellow Submarine' atmosphere."
    }
}


def add_missing_fields_to_stadium_data():
    """Add missing fields to stadium_data.py file."""
    # Read the current file
    file_path = "d:\\programming\\vscodeProjects\\MatchSimulationModel\\DataMigration\\Stadiums\\stadium_data.py"

    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # Process each stadium that needs missing fields
    for team_name, missing_data in MISSING_STADIUM_DATA.items():
        print(f"Processing {team_name}...")

        # Find the stadium entry in the file
        team_start = content.find(f'"{team_name}": {{')
        if team_start == -1:
            print(f"❌ Could not find {team_name} in file")
            continue

        # Find the end of this stadium's data
        brace_count = 0
        i = team_start
        while i < len(content):
            if content[i] == '{':
                brace_count += 1
            elif content[i] == '}':
                brace_count -= 1
                if brace_count == 0:
                    team_end = i
                    break
            i += 1

        # Extract the stadium data section
        stadium_section = content[team_start:team_end + 1]

        # Check if missing fields already exist
        has_last_renovation = 'last_renovation' in stadium_section
        has_address = 'address' in stadium_section
        has_description = 'description' in stadium_section

        if has_last_renovation and has_address and has_description:
            print(f"✅ {team_name} already has all fields")
            continue

        # Find where to insert the missing fields (after "opened" field)
        opened_match = stadium_section.find('"opened":')
        if opened_match == -1:
            print(f"❌ Could not find 'opened' field for {team_name}")
            continue

        # Find the end of the opened line
        opened_line_end = stadium_section.find('\n', opened_match)
        if opened_line_end == -1:
            print(f"❌ Could not find end of opened line for {team_name}")
            continue

        # Build the insertion text
        insertion_lines = []

        if not has_last_renovation:
            insertion_lines.append(f'        "last_renovation": {missing_data["last_renovation"]},')

        if not has_address:
            insertion_lines.append(f'        "address": "{missing_data["address"]}",')

        # For description, we need to place it before facilities
        description_insertion = ""
        if not has_description:
            description_insertion = f'        "description": "{missing_data["description"]}",\n'

        # Insert after opened line
        new_stadium_section = (
                stadium_section[:opened_line_end + 1] +
                '\n' + '\n'.join(insertion_lines) +
                stadium_section[opened_line_end + 1:]
        )

        # Now add description before facilities if needed
        if description_insertion:
            facilities_match = new_stadium_section.find('"facilities":')
            if facilities_match != -1:
                new_stadium_section = (
                        new_stadium_section[:facilities_match] +
                        description_insertion +
                        new_stadium_section[facilities_match:]
                )

        # Replace in the main content
        content = content[:team_start] + new_stadium_section + content[team_end + 1:]

        print(f"✅ Added missing fields to {team_name}")

    # Write the updated content back to the file
    with open(file_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print("\n🎉 Stadium data enhancement complete!")


if __name__ == "__main__":
    print("🏟️ COMPLETING STADIUM DATA")
    print("=" * 50)
    add_missing_fields_to_stadium_data()

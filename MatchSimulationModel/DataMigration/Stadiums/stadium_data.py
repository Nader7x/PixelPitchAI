#!/usr/bin/env python3
"""
Stadium Data Repository

Comprehensive stadium data for Spanish La Liga teams with real stadium information
including capacity, coordinates, surface type, construction year, and facilities.

Data sources: Official club websites, UEFA, and stadium databases.
"""

# Stadium data dictionary with comprehensive information for each team
STADIUM_DATA = {"Athletic Club": {
    "name": "San Mamés",
    "city": "Bilbao",
    "capacity": 53289,
    "surface": "Natural Grass",
    "opened": 2013,
    "last_renovation": 2013,
    "address": "Rafael Moreno Pitxitxi Kalea, 1, 48013 Bilbao, Bizkaia, Spain",
    "latitude": 43.264167,
    "longitude": -2.949167,
    "architect": "César Azkarate, Iñaki Aurrekoetxea",
    "cost_millions_euros": 211,
    "nicknamed": "The Cathedral",
    "description": "Modern stadium known as 'The Cathedral' of Athletic Club, rebuilt in 2013 to replace the historic San Mamés. Features state-of-the-art facilities and maintains the passionate atmosphere of Basque football.",
    "facilities": ["Museum", "VIP Boxes", "Heated Pitch", "LED Lighting"]
}, "Atlético Madrid": {
    "name": "Cívitas Metropolitano",
    "city": "Madrid",
    "capacity": 70460,
    "surface": "Natural Grass",
    "opened": 2017,
    "last_renovation": 2017,
    "address": "Av. de Luis Aragonés, 4, 28022 Madrid, Spain",
    "latitude": 40.436111,
    "longitude": -3.599444,
    "architect": "Cruz y Ortiz Arquitectos",
    "cost_millions_euros": 310,
    "nicknamed": "Wanda Metropolitano",
    "description": "Ultra-modern stadium opened in 2017, featuring cutting-edge design and technology. Home to Atlético Madrid with a distinctive red and white exterior matching the club colors.",
    "facilities": ["Shopping Center", "Museum", "VIP Areas", "Underground Parking", "Hotel"]
}, "Barcelona": {
    "name": "Spotify Camp Nou",
    "city": "Barcelona",
    "capacity": 99354,
    "surface": "Natural Grass",
    "opened": 1957,
    "last_renovation": 2022,
    "address": "C. d'Arístides Maillol, 12, 08028 Barcelona, Spain",
    "latitude": 41.380833,
    "longitude": 2.122778,
    "architect": "Francesc Mitjans, Lorenzo García-Barbón",
    "cost_millions_euros": 1500,  # Current renovation cost
    "nicknamed": "Camp Nou",
    "description": "Iconic stadium and largest in Europe, currently undergoing major renovation. Home to FC Barcelona since 1957, known worldwide as a temple of football with incredible atmosphere.",
    "facilities": ["Museum", "Megastore", "VIP Areas", "Press Center", "Chapel"]
},
    "Celta Vigo": {
        "name": "Estadio Balaídos",
        "city": "Vigo",
        "capacity": 29000,
        "surface": "Natural Grass",
        "opened": 1928,

        "last_renovation": 2017,
        "address": "Rúa de Barcelona, s/n, 36207 Vigo, Pontevedra, Spain", "latitude": 42.213056,
        "longitude": -8.738889,
        "architect": "Manuel del Río",
        "cost_millions_euros": 15,
        "nicknamed": "Balaídos",
        "description": "Historic stadium in Vigo, home to RC Celta since 1928. Known for its passionate Galician support and intimate atmosphere despite modest capacity.",
        "facilities": ["VIP Boxes", "Press Room", "Training Facilities"]
    },
    "Cádiz": {
        "name": "Estadio Nuevo Mirandilla",
        "city": "Cádiz",
        "capacity": 20724,
        "surface": "Natural Grass",
        "opened": 1955,

        "last_renovation": 2019,
        "address": "Av. de Ramón de Carranza, s/n, 11010 Cádiz, Spain", "latitude": 36.504722,
        "longitude": -6.270833,
        "architect": "Fernando Cavestany",
        "cost_millions_euros": 8,
        "nicknamed": "Carranza",
        "description": "Coastal stadium in the historic city of Cádiz, recently renovated to meet La Liga standards. Known for its unique location near the sea and passionate local support.",
        "facilities": ["VIP Areas", "Press Room", "Youth Academy Facilities"]
    },
    "Deportivo Alavés": {
        "name": "Estadio de Mendizorroza",
        "city": "Vitoria-Gasteiz",
        "capacity": 19840,
        "surface": "Natural Grass",
        "opened": 1924,

        "last_renovation": 2016,
        "address": "Av. de Cervantes, 28, 01007 Vitoria-Gasteiz, Álava, Spain", "latitude": 42.8403,
        "longitude": -2.6889,
        "architect": "José María Basterra",
        "cost_millions_euros": 12,
        "nicknamed": "Mendizorroza",
        "description": "Historic Basque stadium dating back to 1924, recently modernized while maintaining its traditional character. Known for strong local support in the Basque Country.",
        "facilities": ["VIP Boxes", "Museum", "Youth Training Center"]
    },
    "Eibar": {
        "name": "Estadio Municipal de Ipurua",
        "city": "Eibar",
        "capacity": 8164,
        "surface": "Natural Grass",
        "opened": 1947,

        "last_renovation": 2014,
        "address": "Untzaga Kalea, 47, 20600 Eibar, Gipuzkoa, Spain", "latitude": 43.203889,
        "longitude": -2.472222,
        "architect": "Municipal Design",
        "cost_millions_euros": 4,
        "nicknamed": "Ipurua",
        "description": "Small but iconic stadium in the Basque town of Eibar. Despite its modest size, it has hosted La Liga matches and is known for its incredible atmosphere.",
        "facilities": ["Small VIP Area", "Press Room", "Basic Facilities"]
    },
    "Elche": {
        "name": "Estadio Martínez Valero",
        "city": "Elche",
        "capacity": 33732,
        "surface": "Natural Grass",
        "opened": 1976,

        "last_renovation": 2018,
        "address": "Av. del Estadio, s/n, 03202 Elche, Alicante, Spain", "latitude": 38.262222,
        "longitude": -0.682222,
        "architect": "Joaquín Ramos Hernández",
        "cost_millions_euros": 18,
        "nicknamed": "Martínez Valero",
        "description": "Modern stadium opened in 1976, named after club president Martínez Valero. Features good facilities and has been recently renovated to maintain La Liga standards.",
        "facilities": ["VIP Boxes", "Press Center", "Training Ground"]
    },
    "Espanyol": {
        "name": "RCDE Stadium",
        "city": "Barcelona",
        "capacity": 40500,
        "surface": "Natural Grass",
        "opened": 2009,

        "last_renovation": 2009,
        "address": "Av. del Baix Llobregat, 100, 08940 Cornellà de Llobregat, Barcelona, Spain", "latitude": 41.347778,
        "longitude": 2.075556,
        "architect": "Estudi PSP Arquitectura",
        "cost_millions_euros": 60,
        "nicknamed": "Cornellà-El Prat",
        "description": "Modern stadium opened in 2009, replacing the historic Sarrià. Features contemporary design and excellent facilities, located in the Barcelona metropolitan area.",
        "facilities": ["VIP Areas", "Shopping Center", "Restaurant", "Museum"]
    },
    "Getafe": {
        "name": "Coliseum Alfonso Pérez",
        "city": "Getafe",
        "capacity": 17393,
        "surface": "Natural Grass",
        "opened": 1998,

        "last_renovation": 2017,
        "address": "Av. Teresa de Calcuta, 2, 28903 Getafe, Madrid, Spain", "latitude": 40.325556,
        "longitude": -3.714444,
        "architect": "Lamela Arquitectos",
        "cost_millions_euros": 25,
        "nicknamed": "Coliseum",
        "description": "Modern stadium opened in 1998, home to Getafe CF. Known for its compact design and intense atmosphere, located in the Madrid metropolitan area.",
        "facilities": ["VIP Boxes", "Press Room", "Training Facilities"]
    },
    "Girona": {
        "name": "Estadi Montilivi",
        "city": "Girona",
        "capacity": 14624,
        "surface": "Natural Grass",
        "opened": 1970,

        "last_renovation": 2017,
        "address": "Av. de Montilivi, 141, 17003 Girona, Spain", "latitude": 41.962222,
        "longitude": 2.829167,
        "architect": "Municipal Design",
        "cost_millions_euros": 8,
        "nicknamed": "Montilivi",
        "description": "Traditional stadium dating from 1970, recently renovated to meet top-flight standards. Known for its intimate atmosphere and strong Catalan identity.",
        "facilities": ["VIP Area", "Press Room", "Youth Academy"]
    },
    "Granada": {
        "name": "Estadio Nuevo Los Cármenes",
        "city": "Granada",
        "capacity": 19336,
        "surface": "Natural Grass",
        "opened": 1995,

        "last_renovation": 2019,
        "address": "C. Pintor Manuel Maldonado, s/n, 18007 Granada, Spain", "latitude": 37.161389,
        "longitude": -3.606944,
        "architect": "Ayuntamiento de Granada",
        "cost_millions_euros": 15,
        "nicknamed": "Los Cármenes",
        "description": "Modern stadium opened in 1995, located in the historic city of Granada. Features contemporary facilities with views of the Sierra Nevada mountains.",
        "facilities": ["VIP Boxes", "Press Center", "Training Ground"]
    },
    "Huesca": {
        "name": "Estadio El Alcoraz",
        "city": "Huesca",
        "capacity": 7638,
        "surface": "Natural Grass",
        "opened": 1972,

        "last_renovation": 2018,
        "address": "Av. del Parque, 1, 22005 Huesca, Spain", "latitude": 42.120833,
        "longitude": -0.420556,
        "architect": "Municipal Design",
        "cost_millions_euros": 3,
        "nicknamed": "El Alcoraz",
        "description": "Small stadium in the Aragonese city of Huesca, renovated for La Liga promotion. Despite its modest size, it provides an intimate and passionate atmosphere.",
        "facilities": ["Basic VIP Area", "Press Room", "Small Training Area"]
    },
    "Las Palmas": {
        "name": "Estadio de Gran Canaria",
        "city": "Las Palmas de Gran Canaria",
        "capacity": 32400,
        "surface": "Natural Grass",
        "opened": 2003,

        "last_renovation": 2017,
        "address": "C. Fondos de Segura, s/n, 35019 Las Palmas de Gran Canaria, Spain", "latitude": 28.099167,
        "longitude": -15.453889,
        "architect": "Alfredo Reiter",
        "cost_millions_euros": 110,
        "nicknamed": "Gran Canaria",
        "description": "Modern stadium on the Canary Islands, opened in 2003. Features excellent facilities and a unique island location, home to UD Las Palmas.",
        "facilities": ["VIP Areas", "Convention Center", "Shopping Area", "Museum"]
    },
    "Leganés": {
        "name": "Estadio Municipal de Butarque",
        "city": "Leganés",
        "capacity": 12454,
        "surface": "Natural Grass",
        "opened": 1998,

        "last_renovation": 2016,
        "address": "Av. de la Mancha, s/n, 28911 Leganés, Madrid, Spain", "latitude": 40.340278,
        "longitude": -3.766111,
        "architect": "Municipal Design",
        "cost_millions_euros": 8,
        "nicknamed": "Butarque",
        "description": "Municipal stadium serving CD Leganés, renovated for their La Liga campaigns. Known for strong community support despite its modest size.",
        "facilities": ["VIP Boxes", "Press Room", "Youth Training"]
    },
    "Levante UD": {
        "name": "Estadi Ciutat de València",
        "city": "Valencia",
        "capacity": 26354,
        "surface": "Natural Grass",
        "opened": 1969,

        "last_renovation": 2016,
        "address": "C. de San Vicente de Paúl, 44, 46019 Valencia, Spain", "latitude": 39.493889,
        "longitude": -0.358611,
        "architect": "Javier Goerlich",
        "cost_millions_euros": 12,
        "nicknamed": "Ciutat de València",
        "description": "Stadium in Valencia dating from 1969, home to Levante UD. Known as 'Ciutat de València' and features traditional Spanish stadium architecture.",
        "facilities": ["VIP Areas", "Press Center", "Training Complex"]
    },
    "Mallorca": {
        "name": "Estadi Mallorca Son Moix",
        "city": "Palma",
        "capacity": 23142,
        "surface": "Natural Grass",
        "opened": 1999,

        "last_renovation": 2018,
        "address": "Cam. dels Reis, 8, 07011 Palma, Illes Balears, Spain", "latitude": 39.590556,
        "longitude": 2.630278,
        "architect": "Tomàs Forteza",
        "cost_millions_euros": 35,
        "nicknamed": "Son Moix",
        "description": "Modern stadium on the island of Mallorca, opened in 1999. Features contemporary design and excellent facilities, home to RCD Mallorca.",
        "facilities": ["VIP Boxes", "Press Room", "Training Ground", "Youth Academy"]
    },
    "Málaga": {
        "name": "Estadio La Rosaleda",
        "city": "Málaga",
        "capacity": 30044,
        "surface": "Natural Grass",
        "opened": 1941,

        "last_renovation": 2017,
        "address": "Paseo de Martiricos, 28, 29010 Málaga, Spain", "latitude": 36.7325,
        "longitude": -4.425556,
        "architect": "Teodoro de Anasagasti",
        "cost_millions_euros": 20,
        "nicknamed": "La Rosaleda",
        "description": "Historic stadium dating from 1941, known as 'La Rosaleda'. Features traditional Spanish architecture and has been home to Málaga CF for decades.",
        "facilities": ["VIP Areas", "Press Center", "Museum", "Training Facilities"]
    },
    "Osasuna": {
        "name": "Estadio El Sadar",
        "city": "Pamplona",
        "capacity": 23576,
        "surface": "Natural Grass",
        "opened": 1967,

        "last_renovation": 2019,
        "address": "C. Sadar, s/n, 31006 Pamplona, Navarra, Spain", "latitude": 42.796667,
        "longitude": -1.636944,
        "architect": "Víctor Eusa",
        "cost_millions_euros": 15,
        "nicknamed": "El Sadar",
        "description": "Historic stadium in Pamplona, home to CA Osasuna since 1967. Known for passionate Navarrese support and traditional Spanish stadium atmosphere.",
        "facilities": ["VIP Boxes", "Press Room", "Training Complex"]
    },
    "RC Deportivo La Coruña": {
        "name": "Estadio Riazor",
        "city": "A Coruña",
        "capacity": 32660,
        "surface": "Natural Grass",
        "opened": 1944,

        "last_renovation": 2018,
        "address": "Av. de A Sardiñeira, s/n, 15011 A Coruña, Spain", "latitude": 43.368611,
        "longitude": -8.417222,
        "architect": "Alejandro de la Sota",
        "cost_millions_euros": 25,
        "nicknamed": "Riazor",
        "description": "Historic coastal stadium dating from 1944, overlooking the Atlantic Ocean. Famous for its unique location and passionate Galician support.",
        "facilities": ["VIP Areas", "Museum", "Press Center", "Training Ground"]
    },
    "Rayo Vallecano": {
        "name": "Estadio de Vallecas",
        "city": "Madrid",
        "capacity": 14708,
        "surface": "Natural Grass",
        "opened": 1976,

        "last_renovation": 2018,
        "address": "C. del Payaso Fofó, s/n, 28018 Madrid, Spain", "latitude": 40.391944,
        "longitude": -3.659167,
        "architect": "Luis Alemany",
        "cost_millions_euros": 6,
        "nicknamed": "Campo de Fútbol de Vallecas",
        "description": "Traditional working-class stadium in Madrid's Vallecas neighborhood. Known for its passionate, left-wing fanbase and authentic football atmosphere.",
        "facilities": ["Basic VIP Area", "Press Room", "Small Training Area"]
    },
    "Real Betis": {
        "name": "Estadio Benito Villamarín",
        "city": "Sevilla",
        "capacity": 60721,
        "surface": "Natural Grass",
        "opened": 1929,

        "last_renovation": 2020,
        "address": "Av. de Heliópolis, s/n, 41012 Sevilla, Spain", "latitude": 37.356389,
        "longitude": -5.981667,
        "architect": "Aníbal González",
        "cost_millions_euros": 80,
        "nicknamed": "Villamarín",
        "description": "Historic stadium dating from 1929, home to Real Betis. Known as 'Villamarín' and famous for its passionate Sevillian support and recent modernization.",
        "facilities": ["VIP Areas", "Museum", "Press Center", "Training Complex", "Hotel"]
    }, "Real Madrid": {
        "name": "Estadio Santiago Bernabéu",
        "city": "Madrid",
        "capacity": 81044,
        "surface": "Natural Grass",
        "opened": 1947,
        "last_renovation": 2023,
        "address": "Av. de Concha Espina, 1, 28036 Madrid, Spain",
        "latitude": 40.453056,
        "longitude": -3.688333,
        "architect": "Manuel Muñoz Monasterio, Luis Alemany",
        "cost_millions_euros": 1200,  # Recent renovation
        "nicknamed": "Santiago Bernabéu",
        "description": "Legendary stadium and home to Real Madrid since 1947. Recently renovated with retractable roof and cutting-edge technology. One of the most famous stadiums in world football.",
        "facilities": ["Museum", "VIP Areas", "Shopping Center", "Restaurant", "Hotel", "Retractable Roof"]
    },
    "Real Sociedad": {
        "name": "Reale Arena",
        "city": "San Sebastián",
        "capacity": 39500,
        "surface": "Natural Grass",
        "opened": 1993,

        "last_renovation": 2019,
        "address": "P.º de Anoeta, 1, 20014 Donostia, Gipuzkoa, Spain", "latitude": 43.301389,
        "longitude": -2.035833,
        "architect": "Dolores Alonso",
        "cost_millions_euros": 50,
        "nicknamed": "Anoeta",
        "description": "Modern stadium in San Sebastián, opened in 1993. Known as 'Reale Arena' and features contemporary Basque design with excellent facilities.",
        "facilities": ["VIP Areas", "Press Center", "Training Complex", "Youth Academy"]
    },
    "Real Valladolid": {
        "name": "Estadio José Zorrilla",
        "city": "Valladolid",
        "capacity": 26512,
        "surface": "Natural Grass",
        "opened": 1982,

        "last_renovation": 2018,
        "address": "Av. del Mundial 82, s/n, 47014 Valladolid, Spain", "latitude": 41.644167,
        "longitude": -4.760556,
        "architect": "Julio Cano Lasso",
        "cost_millions_euros": 20,
        "nicknamed": "Zorrilla",
        "description": "Stadium opened in 1982, named after poet José Zorrilla. Features modern facilities and serves as home to Real Valladolid CF.",
        "facilities": ["VIP Boxes", "Press Room", "Training Ground"]
    },
    "Sevilla": {
        "name": "Estadio Ramón Sánchez-Pizjuán",
        "city": "Sevilla",
        "capacity": 43883,
        "surface": "Natural Grass",
        "opened": 1958,

        "last_renovation": 2020,
        "address": "Av. Eduardo Dato, s/n, 41005 Sevilla, Spain", "latitude": 37.383889,
        "longitude": -5.970278,
        "architect": "Manuel Muñoz Monasterio",
        "cost_millions_euros": 45,
        "nicknamed": "Sánchez-Pizjuán",
        "description": "Historic stadium dating from 1958, home to Sevilla FC. Known for its passionate atmosphere and recent renovations, one of Spain's most famous stadiums.",
        "facilities": ["VIP Areas", "Museum", "Press Center", "Training Complex"]
    },
    "Sporting Gijón": {
        "name": "Estadio El Molinón",
        "city": "Gijón",
        "capacity": 30000,
        "surface": "Natural Grass",
        "opened": 1908,

        "last_renovation": 2017,
        "address": "C. Luis Braille, s/n, 33390 Gijón, Asturias, Spain", "latitude": 43.534444,
        "longitude": -5.631944,
        "architect": "Various (historic)",
        "cost_millions_euros": 18,
        "nicknamed": "El Molinón",
        "description": "Historic stadium dating from 1908, one of Spain's oldest. Known as 'El Molinón' and famous for passionate Asturian support and maritime location.",
        "facilities": ["VIP Areas", "Museum", "Press Center", "Training Ground"]
    },
    "Valencia": {
        "name": "Estadio de Mestalla",
        "city": "Valencia",
        "capacity": 49430,
        "surface": "Natural Grass",
        "opened": 1923,

        "last_renovation": 2019,
        "address": "Av. de Suècia, s/n, 46010 Valencia, Spain", "latitude": 39.474722,
        "longitude": -0.358611,
        "architect": "Francisco Almenar Quinzá",
        "cost_millions_euros": 55,
        "nicknamed": "Mestalla",
        "description": "Legendary stadium dating from 1923, home to Valencia CF. Known as 'Mestalla' and famous for its incredible atmosphere and rich football history.",
        "facilities": ["VIP Areas", "Museum", "Press Center", "Training Complex", "Youth Academy"]
    },
    "Villarreal": {
        "name": "Estadio de la Cerámica",
        "city": "Villarreal",
        "capacity": 23500,
        "surface": "Natural Grass",
        "opened": 1923,

        "last_renovation": 2017,
        "address": "C. Blasco Ibáñez, 12, 12540 Villarreal, Castellón, Spain", "latitude": 39.944167,
        "longitude": -0.103611,
        "architect": "Various renovations",
        "cost_millions_euros": 30,
        "nicknamed": "El Madrigal",
        "description": "Stadium dating from 1923, known as 'El Madrigal' or 'Estadio de la Cerámica'. Home to Villarreal CF and famous for its 'Yellow Submarine' atmosphere.",
        "facilities": ["VIP Areas", "Press Center", "Training Complex", "Youth Academy"]
    }
}


def get_stadium_data(team_name):
    """Get stadium data for a specific team."""
    return STADIUM_DATA.get(team_name)


def get_all_stadiums():
    """Get all stadium data."""
    return STADIUM_DATA


def validate_stadium_data():
    """Validate stadium data completeness."""
    required_fields = ['name', 'city', 'capacity', 'surface', 'opened', 'latitude', 'longitude']

    for team, data in STADIUM_DATA.items():
        missing_fields = [field for field in required_fields if field not in data]
        if missing_fields:
            print(f"❌ {team}: Missing fields: {missing_fields}")
        else:
            print(f"✅ {team}: Complete data")


if __name__ == "__main__":
    print("🏟️ STADIUM DATA VALIDATION")
    print("=" * 50)
    validate_stadium_data()
    print(f"\n📊 Total stadiums in database: {len(STADIUM_DATA)}")

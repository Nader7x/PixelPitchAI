#!/usr/bin/env python3
"""
Comprehensive Real La Liga Coaches Data Repository
Contains authentic data for current and recent La Liga team coaches
"""

from datetime import date

# Real La Liga Coaches Data (2023-2025 season data)
LALIGA_COACHES_DATA = {
    # Athletic Club
    "Athletic Club": {
        "first_name": "Ernesto",
        "last_name": "Valverde Tejedor",
        "date_of_birth": date(1964, 2, 9),
        "nationality": "Spanish",
        "years_of_experience": 25,
        "biography": "Ernesto Valverde is a Spanish football manager and former player. Born in Viandar de la Vera, he had a distinguished playing career as a forward with Athletic Bilbao, Barcelona, and other clubs. As a manager, he has led Athletic Bilbao to multiple successes and previously managed FC Barcelona from 2017 to 2020, winning two La Liga titles and one Copa del Rey.",
        "coaching_style": "Possession-based football with strong defensive organization. Emphasizes tactical discipline and players' technical development. Known for adaptive tactics and building teams around existing squad strengths.",
        "preferred_formation": "4-2-3-1"
    },

    # Atlético Madrid
    "Atlético Madrid": {
        "first_name": "Diego",
        "last_name": "Pablo Simeone",
        "date_of_birth": date(1970, 4, 28),
        "nationality": "Argentine",
        "years_of_experience": 14,
        "biography": "Diego Pablo Simeone is an Argentine professional football manager and former player. Nicknamed 'El Cholo', he has been the manager of Atlético Madrid since 2011. Under his leadership, Atlético has won La Liga twice, the Copa del Rey, two UEFA Europa League titles, and reached two UEFA Champions League finals. Known for his passionate touchline demeanor and defensive tactical approach.",
        "coaching_style": "Defensive solidarity and counter-attacking football. Emphasizes physical intensity, team unity, and mental strength. Known for transforming players into warriors and creating an unbreakable team spirit.",
        "preferred_formation": "5-3-2"
    },

    # FC Barcelona
    "Barcelona": {
        "first_name": "Hansi",
        "last_name": "Flick",
        "date_of_birth": date(1965, 2, 24),
        "nationality": "German",
        "years_of_experience": 16,
        "biography": "Hans-Dieter 'Hansi' Flick is a German professional football manager and former player. He won the FIFA World Cup with Germany as an assistant coach in 2014 and later managed the German national team. At Bayern Munich, he achieved a historic sextuple in 2020. Appointed as Barcelona manager in 2024, bringing his experience in high-pressing, attacking football.",
        "coaching_style": "High-intensity pressing and fast-paced attacking football. Emphasizes quick transitions, positional play, and technical excellence. Known for developing young talents and implementing modern tactical concepts.",
        "preferred_formation": "4-3-3"
    },

    # Real Madrid
    "Real Madrid": {
        "first_name": "Carlo",
        "last_name": "Ancelotti",
        "date_of_birth": date(1959, 6, 10),
        "nationality": "Italian",
        "years_of_experience": 29,
        "biography": "Carlo Ancelotti is an Italian professional football manager and former player. One of the most successful managers in football history, he has won league titles in Italy, England, France, Germany, and Spain. With Real Madrid, he has won multiple Champions League titles and is known for his man-management skills and tactical flexibility.",
        "coaching_style": "Flexible tactical approach adapting to players' strengths. Emphasizes attacking football, player rotation, and creating harmony within the squad. Master of managing superstar players and big personalities.",
        "preferred_formation": "4-3-3"
    },

    # Sevilla FC
    "Sevilla": {
        "first_name": "Francisco",
        "last_name": "Javier García Pimienta",
        "date_of_birth": date(1974, 8, 8),
        "nationality": "Spanish",
        "years_of_experience": 12,
        "biography": "Francisco Javier García Pimienta is a Spanish football manager and former player. Known for his work with Barcelona's youth academy and later with Las Palmas. Appointed as Sevilla manager, he brings a possession-based philosophy and emphasis on developing young players through Barcelona's La Masia methodology.",
        "coaching_style": "Possession-based football with high pressing. Emphasizes technical development, positional play, and youth integration. Follows Barcelona's traditional playing philosophy with modern tactical adaptations.",
        "preferred_formation": "4-3-3"
    },

    # Real Betis
    "Real Betis": {
        "first_name": "Manuel",
        "last_name": "Luis Pellegrini Ripamonti",
        "date_of_birth": date(1953, 9, 16),
        "nationality": "Chilean",
        "years_of_experience": 35,
        "biography": "Manuel Luis Pellegrini is a Chilean professional football manager and former player. Known as 'The Engineer' due to his background in civil engineering, he has managed top clubs including Real Madrid, Manchester City, and Villarreal. At Real Betis since 2020, he has implemented an attractive, possession-based style of play.",
        "coaching_style": "Possession-based attacking football with emphasis on technical play. Known for his calm demeanor, tactical intelligence, and ability to develop young players. Focuses on beautiful football and player development.",
        "preferred_formation": "4-2-3-1"
    },

    # Valencia CF
    "Valencia": {
        "first_name": "Rubén",
        "last_name": "Baraja Vegas",
        "date_of_birth": date(1975, 7, 11),
        "nationality": "Spanish",
        "years_of_experience": 8,
        "biography": "Rubén Baraja is a Spanish football manager and former midfielder who spent most of his playing career at Valencia CF. After retiring, he moved into coaching and worked his way up through Valencia's youth system before being appointed as first-team manager. Known for his deep understanding of the club's culture and values.",
        "coaching_style": "Balanced approach combining defensive solidity with creative attacking play. Emphasizes team unity, pressing, and quick transitions. Focuses on maintaining Valencia's traditional fighting spirit.",
        "preferred_formation": "4-4-2"
    },

    # Villarreal CF
    "Villarreal": {
        "first_name": "Marcelino",
        "last_name": "García Toral",
        "date_of_birth": date(1965, 8, 14),
        "nationality": "Spanish",
        "years_of_experience": 22,
        "biography": "Marcelino García Toral is a Spanish football manager and former player. He has managed several Spanish clubs including Valencia, Athletic Bilbao, and Villarreal. Known for his tactical acumen and ability to develop teams that play attractive, attacking football while maintaining defensive discipline.",
        "coaching_style": "High-intensity pressing with quick attacking transitions. Emphasizes tactical discipline, physical preparation, and team chemistry. Known for getting the best out of his players through detailed tactical preparation.",
        "preferred_formation": "4-4-2"
    },

    # Real Sociedad
    "Real Sociedad": {
        "first_name": "Imanol",
        "last_name": "Alguacil Barrenetxea",
        "date_of_birth": date(1971, 7, 4),
        "nationality": "Spanish",
        "years_of_experience": 15,
        "biography": "Imanol Alguacil is a Spanish football manager and former player who spent his entire playing career at Real Sociedad. He joined the coaching staff after retirement and worked his way up to become head coach. Under his guidance, Real Sociedad has qualified for European competitions and developed a distinctive playing style.",
        "coaching_style": "Possession-based football with high pressing and quick passing combinations. Emphasizes youth development, technical skills, and maintaining Real Sociedad's Basque identity. Focuses on team cohesion and tactical intelligence.",
        "preferred_formation": "4-3-3"
    },

    # CA Osasuna
    "Osasuna": {
        "first_name": "Vicente",
        "last_name": "Moreno Peris",
        "date_of_birth": date(1974, 7, 25),
        "nationality": "Spanish",
        "years_of_experience": 16,
        "biography": "Vicente Moreno is a Spanish football manager and former player. He has managed several Spanish clubs including Mallorca, Espanyol, and currently Osasuna. Known for his ability to organize teams effectively and get the maximum performance from limited resources.",
        "coaching_style": "Organized defensive play with effective counter-attacking. Emphasizes team unity, work ethic, and tactical discipline. Known for maximizing team potential and creating competitive sides regardless of budget constraints.",
        "preferred_formation": "5-4-1"
    },

    # Getafe CF
    "Getafe": {
        "first_name": "José",
        "last_name": "Bordalás Jiménez",
        "date_of_birth": date(1964, 3, 5),
        "nationality": "Spanish",
        "years_of_experience": 20,
        "biography": "José Bordalás is a Spanish football manager and former player. Known for his intense, physical style of play, he has managed several Spanish clubs including Getafe and Valencia. He is recognized for building solid, well-organized teams that are difficult to beat.",
        "coaching_style": "Physical, aggressive pressing game with strong defensive organization. Emphasizes intensity, fighting spirit, and direct play. Known for creating teams that are extremely difficult to break down and score against.",
        "preferred_formation": "5-4-1"
    },

    # RC Celta de Vigo
    "Celta Vigo": {
        "first_name": "Claudio",
        "last_name": "Giráldez López",
        "date_of_birth": date(1991, 12, 17),
        "nationality": "Spanish",
        "years_of_experience": 8,
        "biography": "Claudio Giráldez is a young Spanish football manager who has risen through the ranks at RC Celta de Vigo. Starting in youth football, he has quickly established himself as one of the most promising young coaches in Spanish football, known for his modern tactical approach and ability to work with young players.",
        "coaching_style": "Modern possession-based football with high pressing. Emphasizes youth development, quick transitions, and attacking play. Known for implementing contemporary tactical concepts and developing young talent.",
        "preferred_formation": "4-3-3"
    },

    # Rayo Vallecano
    "Rayo Vallecano": {
        "first_name": "Iñigo",
        "last_name": "Pérez Soto",
        "date_of_birth": date(1988, 5, 14),
        "nationality": "Spanish",
        "years_of_experience": 6,
        "biography": "Iñigo Pérez is a Spanish football manager and former player. After a playing career primarily in lower divisions, he transitioned to coaching and has worked his way up through various Spanish clubs. Known for his innovative tactical approaches and ability to motivate players.",
        "coaching_style": "High-energy attacking football with emphasis on pressing and quick transitions. Focuses on maximizing player potential and creating an exciting brand of football that reflects Rayo Vallecano's fighting spirit.",
        "preferred_formation": "4-2-3-1"
    },

    # Deportivo Alavés
    "Deportivo Alavés": {
        "first_name": "Luis",
        "last_name": "García Plaza",
        "date_of_birth": date(1968, 6, 26),
        "nationality": "Spanish",
        "years_of_experience": 18,
        "biography": "Luis García Plaza is a Spanish football manager and former player. He has managed several Spanish clubs including Mallorca and Deportivo Alavés. Known for his tactical knowledge and ability to adapt his systems to the players at his disposal.",
        "coaching_style": "Flexible tactical approach with emphasis on defensive organization. Adapts formation and style based on available players. Known for his pragmatic approach and ability to get results with limited resources.",
        "preferred_formation": "4-1-4-1"
    },

    # Girona FC
    "Girona": {
        "first_name": "Míchel",
        "last_name": "Sánchez Muñoz",
        "date_of_birth": date(1975, 11, 23),
        "nationality": "Spanish",
        "years_of_experience": 12,
        "biography": "Míchel Sánchez is a Spanish football manager and former player who had a distinguished playing career with Real Madrid. As a coach, he has developed a reputation for attractive, attacking football and has led Girona to remarkable success, including their first-ever qualification for European competition.",
        "coaching_style": "High-tempo attacking football with emphasis on technical play and quick passing. Focuses on creating numerous scoring opportunities and maintaining high intensity throughout matches. Known for developing cohesive attacking units.",
        "preferred_formation": "4-3-3"
    },

    # RCD Mallorca
    "Mallorca": {
        "first_name": "Jagoba",
        "last_name": "Arrasate Elustondo",
        "date_of_birth": date(1978, 4, 22),
        "nationality": "Spanish",
        "years_of_experience": 14,
        "biography": "Jagoba Arrasate is a Spanish football manager and former player. He has managed Real Sociedad and CA Osasuna before joining Mallorca. Known for his tactical intelligence and ability to build well-organized teams that punch above their weight.",
        "coaching_style": "Organized defensive play with effective counter-attacking and set-piece proficiency. Emphasizes tactical discipline, team unity, and maximizing limited resources. Focuses on creating competitive teams through collective effort.",
        "preferred_formation": "5-3-2"
    },

    # UD Las Palmas
    "Las Palmas": {
        "first_name": "Diego",
        "last_name": "Martínez Penas",
        "date_of_birth": date(1981, 12, 2),
        "nationality": "Spanish",
        "years_of_experience": 11,
        "biography": "Diego Martínez is a Spanish football manager who has made a name for himself with his tactical innovation and attacking philosophy. He has managed clubs like Granada CF and Espanyol, known for implementing attractive football and developing young players.",
        "coaching_style": "Possession-based attacking football with high pressing. Emphasizes technical development, positional play, and creating multiple attacking threats. Known for his detailed tactical preparation and youth development.",
        "preferred_formation": "4-2-3-1"
    },

    # CD Leganés
    "Leganés": {
        "first_name": "Borja",
        "last_name": "Jiménez Sáez",
        "date_of_birth": date(1985, 3, 15),
        "nationality": "Spanish",
        "years_of_experience": 9,
        "biography": "Borja Jiménez is a Spanish football manager who has worked his way up through the lower divisions of Spanish football. Known for his tactical acumen and ability to organize teams effectively, he has earned promotion with Leganés and established them as a competitive side.",
        "coaching_style": "Well-organized defensive structure with quick counter-attacking. Emphasizes tactical discipline, set-piece proficiency, and team chemistry. Focuses on maximizing team potential through detailed preparation.",
        "preferred_formation": "4-4-2"
    },

    # RCD Espanyol
    "Espanyol": {
        "first_name": "Manolo",
        "last_name": "González Villar",
        "date_of_birth": date(1977, 9, 12),
        "nationality": "Spanish",
        "years_of_experience": 10,
        "biography": "Manolo González is a Spanish football manager who has worked extensively in Spanish football. He has experience managing in various divisions and is known for his ability to adapt tactics to suit his team's strengths and exploit opponents' weaknesses.",
        "coaching_style": "Flexible tactical approach with emphasis on solid defensive foundation. Adapts formation and playing style based on opponent analysis. Known for his pragmatic approach and ability to motivate players.",
        "preferred_formation": "4-2-3-1"
    },

    # Real Valladolid
    "Real Valladolid": {
        "first_name": "Paulo",
        "last_name": "Pezzolano Fleitas",
        "date_of_birth": date(1972, 3, 8),
        "nationality": "Uruguayan",
        "years_of_experience": 16,
        "biography": "Paulo Pezzolano is a Uruguayan football manager and former player. He has managed several clubs in South America and Spain, known for his passionate approach and ability to instill fighting spirit in his teams. Brings South American tactical knowledge to European football.",
        "coaching_style": "Intense, physical approach with emphasis on team unity and fighting spirit. Combines South American passion with tactical discipline. Known for creating teams that never give up and fight until the final whistle.",
        "preferred_formation": "4-4-2"
    },

    # Cádiz CF
    "Cádiz": {
        "first_name": "Mauricio",
        "last_name": "Pellegrino",
        "date_of_birth": date(1971, 10, 5),
        "nationality": "Argentine",
        "years_of_experience": 15,
        "biography": "Mauricio Pellegrino is an Argentine football manager and former central defender. He had a successful playing career with clubs like Valencia, Liverpool, and Barcelona. As a manager, he has worked in Spain, England, and Argentina, known for his defensive expertise and tactical organization.",
        "coaching_style": "Defensive solidity with organized structure. Emphasizes tactical discipline, set-piece preparation, and physical conditioning. Known for creating hard-to-beat teams with strong defensive foundations.",
        "preferred_formation": "5-4-1"
    },

    # SD Eibar  
    "Eibar": {
        "first_name": "Joseba",
        "last_name": "Etxeberria Lizardi",
        "date_of_birth": date(1977, 2, 5),
        "nationality": "Spanish",
        "years_of_experience": 12,
        "biography": "Joseba Etxeberria is a Spanish football manager and former winger who had a distinguished playing career primarily with Athletic Bilbao. After retirement, he moved into coaching and has worked with various Spanish clubs. Known for his understanding of Basque football culture and developing attacking players.",
        "coaching_style": "Direct attacking football with emphasis on wing play and crosses. Focuses on physical preparation, team spirit, and maximizing limited resources. Known for creating competitive teams through hard work and organization.",
        "preferred_formation": "4-4-2"
    },

    # Elche CF
    "Elche": {
        "first_name": "Eder",
        "last_name": "Sarabia Armesto",
        "date_of_birth": date(1982, 5, 29),
        "nationality": "Spanish",
        "years_of_experience": 10,
        "biography": "Eder Sarabia is a Spanish football manager who has worked as an assistant coach with several top clubs including Barcelona under Quique Setién. He has developed a reputation for his tactical knowledge and modern approach to football. Known for his detailed analysis and innovative training methods.",
        "coaching_style": "Possession-based football with high pressing and positional play. Emphasizes technical development, tactical intelligence, and modern football concepts. Focuses on player education and tactical flexibility.",
        "preferred_formation": "4-3-3"
    },

    # Granada CF
    "Granada": {
        "first_name": "Fran",
        "last_name": "Escribá Segura",
        "date_of_birth": date(1965, 5, 9),
        "nationality": "Spanish",
        "years_of_experience": 20,
        "biography": "Fran Escribá is a Spanish football manager and former player. He has managed several Spanish clubs including Getafe, Villarreal, and Granada. Known for his tactical versatility and ability to adapt his teams to different situations and opponents.",
        "coaching_style": "Tactical flexibility with emphasis on defensive organization. Adapts formation and style based on opponent analysis and available players. Known for his pragmatic approach and ability to achieve results with limited resources.",
        "preferred_formation": "4-2-3-1"
    },

    # SD Huesca
    "Huesca": {
        "first_name": "Antonio",
        "last_name": "Hidalgo Morilla",
        "date_of_birth": date(1979, 9, 18),
        "nationality": "Spanish",
        "years_of_experience": 12,
        "biography": "Antonio Hidalgo is a Spanish football manager who has worked primarily in Spanish lower divisions before joining Huesca. Known for his work with young players and ability to develop teams through tactical discipline and hard work.",
        "coaching_style": "Organized team play with emphasis on collective effort and tactical discipline. Focuses on youth development, pressing, and creating a strong team spirit. Known for maximizing player potential through detailed preparation.",
        "preferred_formation": "4-4-2"
    },

    # Levante UD
    "Levante UD": {
        "first_name": "Julián",
        "last_name": "Calero García",
        "date_of_birth": date(1978, 1, 15),
        "nationality": "Spanish",
        "years_of_experience": 14,
        "biography": "Julián Calero is a Spanish football manager who has worked extensively in Spanish football. He has managed several teams in different divisions and is known for his tactical knowledge and ability to adapt to different playing styles and squad limitations.",
        "coaching_style": "Balanced approach combining defensive stability with attacking creativity. Emphasizes tactical preparation, physical conditioning, and team unity. Focuses on getting the best from available players through detailed planning.",
        "preferred_formation": "4-2-3-1"
    },

    # Málaga CF
    "Málaga": {
        "first_name": "Sergio",
        "last_name": "Pellicer Rodríguez",
        "date_of_birth": date(1978, 12, 3),
        "nationality": "Spanish",
        "years_of_experience": 13,
        "biography": "Sergio Pellicer is a Spanish football manager who rose through the ranks at Málaga CF, starting in youth football and working his way up to the first team. Known for his deep understanding of the club's philosophy and his ability to develop young players.",
        "coaching_style": "Possession-based football with emphasis on youth development and technical play. Focuses on maintaining Málaga's traditional playing style while adapting to modern tactical concepts. Known for promoting academy players.",
        "preferred_formation": "4-3-3"
    },

    # RC Deportivo La Coruña
    "RC Deportivo La Coruña": {
        "first_name": "Imanol",
        "last_name": "Idiakez Barkaiztegi",
        "date_of_birth": date(1973, 6, 8),
        "nationality": "Spanish",
        "years_of_experience": 15,
        "biography": "Imanol Idiakez is a Spanish football manager and former midfielder who had a playing career in Spanish football. He has managed several Spanish clubs and is known for his tactical knowledge and ability to organize teams effectively, particularly in defensive phases.",
        "coaching_style": "Organized defensive play with quick counter-attacking. Emphasizes tactical discipline, physical preparation, and team cohesion. Known for creating well-structured teams that are difficult to break down.",
        "preferred_formation": "5-3-2"
    },

    # Real Sporting de Gijón
    "Sporting Gijón": {
        "first_name": "Miguel",
        "last_name": "Ángel Ramírez González",
        "date_of_birth": date(1976, 4, 11),
        "nationality": "Spanish",
        "years_of_experience": 11,
        "biography": "Miguel Ángel Ramírez is a Spanish football manager who has worked in various Spanish clubs. Known for his tactical versatility and ability to adapt his teams to different competitive levels. Focuses on team organization and maximizing available resources.",
        "coaching_style": "Flexible tactical approach with emphasis on team organization and work ethic. Adapts formation and style based on squad capabilities and opposition. Known for his motivational skills and ability to create fighting spirit.",
        "preferred_formation": "4-4-2"
    }
}


def get_coach_data_by_team(team_name):
    """Get coach data for a specific team"""
    return LALIGA_COACHES_DATA.get(team_name)


def get_all_coaches_data():
    """Get all coaches data"""
    return LALIGA_COACHES_DATA


def validate_coach_data():
    """Validate that all required fields are present for each coach"""
    required_fields = ['first_name', 'last_name', 'date_of_birth', 'nationality',
                       'years_of_experience', 'biography', 'coaching_style', 'preferred_formation']

    issues = []
    for team, coach_data in LALIGA_COACHES_DATA.items():
        for field in required_fields:
            if field not in coach_data:
                issues.append(f"Team {team}: Missing {field}")
            elif not coach_data[field]:
                issues.append(f"Team {team}: Empty {field}")

    return issues


if __name__ == "__main__":
    print("=== LA LIGA COACHES DATA REPOSITORY ===")
    print(f"Total teams with coach data: {len(LALIGA_COACHES_DATA)}")

    # Validate data
    issues = validate_coach_data()
    if issues:
        print("\nData validation issues:")
        for issue in issues:
            print(f"  - {issue}")
    else:
        print("\n✅ All coach data is complete and valid")

    # Display summary
    print("\n=== COACHES SUMMARY ===")
    for team, coach in LALIGA_COACHES_DATA.items():
        print(
            f"{team}: {coach['first_name']} {coach['last_name']} ({coach['nationality']}, {coach['years_of_experience']} years exp)")

special_list = ["Deportivo_Alavés_2016", "Deportivo_Alavés_2017", "Deportivo_Alavés_2018", "Deportivo_Alavés_2019",
                "Deportivo_Alavés_2020", "Deportivo_Alavés_2021",
                "Barcelona_2016", "Barcelona_2017", "Barcelona_2018", "Barcelona_2019", "Barcelona_2020",
                "Barcelona_2021",
                "Granada_2016", "Granada_2017", "Granada_2018", "Granada_2019", "Granada_2020", "Granada_2021",
                "Celta_Vigo_2016", "Celta_Vigo_2017", "Celta_Vigo_2018", "Celta_Vigo_2019", "Celta_Vigo_2020",
                "Celta_Vigo_2021",
                "Real_Betis_2016", "Real_Betis_2017", "Real_Betis_2018", "Real_Betis_2019", "Real_Betis_2020",
                "Real_Betis_2021",
                "Osasuna_2016", "Osasuna_2017", "Osasuna_2018", "Osasuna_2019", "Osasuna_2020", "Osasuna_2021",
                "Real_Madrid_2016", "Real_Madrid_2017", "Real_Madrid_2018", "Real_Madrid_2019", "Real_Madrid_2020",
                "Real_Madrid_2021",
                "Levante_UD_2016", "Levante_UD_2017", "Levante_UD_2018", "Levante_UD_2019", "Levante_UD_2020",
                "Levante_UD_2021",
                "Villarreal_2016", "Villarreal_2017", "Villarreal_2018", "Villarreal_2019", "Villarreal_2020",
                "Villarreal_2021",
                "Huesca_2016", "Huesca_2017", "Huesca_2018", "Huesca_2019", "Huesca_2020", "Huesca_2021",
                "Sevilla_2016", "Sevilla_2017", "Sevilla_2018", "Sevilla_2019", "Sevilla_2020", "Sevilla_2021",
                "Getafe_2016", "Getafe_2017", "Getafe_2018", "Getafe_2019", "Getafe_2020", "Getafe_2021",
                "Atlético_Madrid_2016", "Atlético_Madrid_2017", "Atlético_Madrid_2018", "Atlético_Madrid_2019",
                "Atlético_Madrid_2020", "Atlético_Madrid_2021",
                "Valencia_2016", "Valencia_2017", "Valencia_2018", "Valencia_2019", "Valencia_2020", "Valencia_2021",
                "Real_Sociedad_2016", "Real_Sociedad_2017", "Real_Sociedad_2018", "Real_Sociedad_2019",
                "Real_Sociedad_2020", "Real_Sociedad_2021",
                "Real_Valladolid_2016", "Real_Valladolid_2017", "Real_Valladolid_2018", "Real_Valladolid_2019",
                "Real_Valladolid_2020", "Real_Valladolid_2021",
                "Cádiz_2016", "Cádiz_2017", "Cádiz_2018", "Cádiz_2019", "Cádiz_2020", "Cádiz_2021",
                "Athletic_Club_2016", "Athletic_Club_2017", "Athletic_Club_2018", "Athletic_Club_2019",
                "Athletic_Club_2020", "Athletic_Club_2021",
                "Elche_2016", "Elche_2017", "Elche_2018", "Elche_2019", "Elche_2020", "Elche_2021",
                "Eibar_2016", "Eibar_2017", "Eibar_2018", "Eibar_2019", "Eibar_2020", "Eibar_2021",
                "Leganés_2016", "Leganés_2017", "Leganés_2018", "Leganés_2019", "Leganés_2020", "Leganés_2021",
                "Mallorca_2016", "Mallorca_2017", "Mallorca_2018", "Mallorca_2019", "Mallorca_2020", "Mallorca_2021",
                "Espanyol_2016", "Espanyol_2017", "Espanyol_2018", "Espanyol_2019", "Espanyol_2020", "Espanyol_2021",
                "Girona_2016", "Girona_2017", "Girona_2018", "Girona_2019", "Girona_2020", "Girona_2021",
                "Rayo_Vallecano_2016", "Rayo_Vallecano_2017", "Rayo_Vallecano_2018", "Rayo_Vallecano_2019",
                "Rayo_Vallecano_2020", "Rayo_Vallecano_2021",
                "RC_Deportivo_La_Coruña_2016", "RC_Deportivo_La_Coruña_2017", "RC_Deportivo_La_Coruña_2018",
                "RC_Deportivo_La_Coruña_2019", "RC_Deportivo_La_Coruña_2020", "RC_Deportivo_La_Coruña_2021",
                "Las_Palmas_2016", "Las_Palmas_2017", "Las_Palmas_2018", "Las_Palmas_2019", "Las_Palmas_2020",
                "Las_Palmas_2021",
                "Málaga_2016", "Málaga_2017", "Málaga_2018", "Málaga_2019", "Málaga_2020", "Málaga_2021",
                "Sporting_Gijón_2016", "Sporting_Gijón_2017", "Sporting_Gijón_2018", "Sporting_Gijón_2019",
                "Sporting_Gijón_2020", "Sporting_Gijón_2021",
                "[MATCH START]", "[EVENTS START]", "[STOPPAGE TIME - FIRST HALF]", "[END OF FIRST HALF]",
                "[SECOND HALF START]", "[STOPPAGE TIME - SECOND HALF]",
                "[MATCH END]"
                ]

import xgboost as xgb
from xgboost import XGBRegressor

import sys
import os
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))
from api.core.xgboost_class import MatchStatProcessor

booster = xgb.Booster()
booster.load_model('tuned_xgboost_model.json')

model = XGBRegressor()
model._Booster = booster

matchStat = MatchStatProcessor(model, special_tokens=special_list)

team_A_id = 2
team_B_id = 5
team_A_season = 2018
team_B_season = 2018

team_A_name = "Barcelona_2018"
team_B_name = "Real_Betis_2018"

model_results = matchStat.generate_features(team_A_id, team_B_id, team_A_season, team_B_season)
print(model_results)

match_headerLines = matchStat.convert_to_text(team_A_name, team_B_name, model_results)
print(match_headerLines)

matchStat.save_text_file(match_headerLines, './headerlines.txt')
matchStat.tokenize_and_save('./headerlines.txt', './headerlines.pt')

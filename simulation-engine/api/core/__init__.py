"""Core package for Football Match Simulation API"""

from .parser import MatchParser, MatchEventProducer, analyze_match, parse_and_publish
from .xgboost_class import MatchStatProcessor

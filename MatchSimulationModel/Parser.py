import json
import math
import os
import pandas as pd
import pika
import re
import time
from typing import List, Optional


class MatchParser:
    def __init__(self):
        self.events = []
        self.match_id = None
        self.home_team = None
        self.away_team = None
        # Precompile regex patterns
        self.patterns = {
            'position': re.compile(r'\(([^)]+)\)'),
            'outcome': re.compile(r'outcome: ([^,]+)'),
            'height': re.compile(r'height: ([^,]+)'),
            'card': re.compile(r'card: ([^,]+)'),
            'pass_target': re.compile(r'to \(([^)]+)\)'),
            'shot_target': re.compile(r'aimed at \(([^)]+)\)'),
            'body_part': re.compile(r'using ([^,]+)'),
            'resulted_in': re.compile(r'resulted in ([^,]+)'),
            'type': re.compile(r'type: ([^,]+)')
        }

    def parse_match_file(self, file_path: str) -> List[dict]:
        with open(file_path, 'r', encoding='utf-8') as f:
            return self.parse_match_data(f.read(), file_path)

    def parse_match_data(self, match_text: str, file_path: Optional[str] = None) -> List[dict]:
        self.events = []
        lines = match_text.strip().split('\n')
        self._extract_match_info(lines, file_path)
        event_index = 0
        last_timestamp = "00:00"
        last_seconds = 0
        score = {'Home': 0, 'Away': 0}
        # Performance: cache teams, use set for marker checks
        home_team = self.home_team
        away_team = self.away_team
        marker_set = {"[MATCH START]", "[EVENTS START]", "[SECOND HALF]"}
        for line in lines:
            line = line.strip()  # Normalize whitespace
            if not line or any(marker in line for marker in marker_set):
                if line == "[MATCH START]":
                    self._add_system_event(event_index, "match_start", "00:00", 0)
                    event_index += 1
                continue
            if line == "[END OF FIRST HALF]":
                self._add_system_event(event_index, "first_half_end", last_timestamp, last_seconds)
                event_index += 1
                continue
            if line == "[SECOND HALF]" or line == "[SECOND HALF START]":
                self._add_system_event(event_index, "second_half_start", "45:00", 2700, 45, 0, score)
                event_index += 1
                continue
            if line == "[MATCH END]":
                self._add_system_event(event_index, "match_end", last_timestamp, last_seconds)
                event_index += 1
                continue
            if line == "[STOPPAGE TIME - SECOND HALF]":
                # Handle stoppage time at the end of the second half
                self._add_system_event(event_index, "stoppage_time_start", "90:00", last_seconds)
            event = self._parse_event_line(line)
            if event:
                last_timestamp = event['timestamp']
                last_seconds = event['time_seconds']
                event['event_index'] = event_index
                event['match_id'] = self.match_id               
                 # Update score if goal (use cached teams)
                if event.get('outcome') == 'Goal':
                    if event['team'] == home_team:
                        score['Home'] += 1
                    elif event['team'] == away_team:
                        score['Away'] += 1
                    # Add the updated score to the goal event
                    event['Score'] = {'Home': score['Home'], 'Away': score['Away']}

                self.events.append(event)
                event_index += 1

        self._infer_duel_outcomes()
        return self.events

    def _extract_match_info(self, lines: List[str], file_path: Optional[str]):
        for i in range(min(3, len(lines))):
            if "[MATCH START]" in lines[i]:
                if i + 1 < len(lines) and " faced " in lines[i + 1]:
                    # Handle cases like: Barcelona_2021  (from 2021 season) faced  Real_Madrid_2018  (from 2018 season)
                    teams_line = lines[i + 1]
                    teams = teams_line.split(" faced ")

                    # Remove any trailing season info in parentheses
                    def clean_team_name(team):
                        return team.split('(')[0].strip()

                    self.home_team = clean_team_name(teams[0])
                    self.away_team = clean_team_name(teams[1].rstrip('.'))
                    if file_path:
                        self.match_id = os.path.basename(file_path).split('_')[1]
                break

    def _parse_minute_second(self, timestamp: str):
        """Parse minute and second from a timestamp, handling stoppage time (e.g., '45+02:00')."""
        if '+' in timestamp:
            base_minute, rest = timestamp.split('+', 1)
            extra_minute, second = rest.split(':')
            minute = int(base_minute) + int(extra_minute)
            second = int(second)
        else:
            minute, second = map(int, timestamp.split(':'))
        return minute, second

    def _add_system_event(self, event_index, action, timestamp, time_seconds, minute=None, second=None, score=None):
        if minute is None or second is None:
            minute, second = self._parse_minute_second(timestamp)
        event = {
            'timestamp': timestamp,
            'time_seconds': time_seconds,
            'minute': minute,
            'second': second,
            'team': "SYSTEM",
            'player': "SYSTEM",
            'action': action,
            'event_type': action,
            'position': None,
            'outcome': None,
            'height': None,
            'card': None,
            'pass_target': None,
            'shot_target': None,
            'body_part': None,
            'event_index': event_index,
            'match_id': self.match_id,
            'home_team': self.home_team,
            'away_team': self.away_team,
            'long_pass': None  # Add long_pass field for system events
        }
        if score is not None:
            event['Score'] = score
        self.events.append(event)

    def _parse_event_line(self, line: str) -> Optional[dict]:
        parts = line.split(' - ', 2)
        if len(parts) < 3:
            return None
        timestamp_str, team, action_part = map(str.strip, parts)
        # Handle regular and stoppage time (e.g., 90+02:27)
        if '+' in timestamp_str:
            # Format: 90+MM:SS
            try:
                base_minute, rest = timestamp_str.split('+', 1)
                extra_minute, extra_second = map(int, rest.split(':'))
                minutes = int(base_minute) + extra_minute
                seconds = extra_second
                time_seconds = int(base_minute) * 60 + extra_minute * 60 + extra_second
            except Exception:
                return None
        else:
            try:
                minutes, seconds = map(int, timestamp_str.split(':'))
                time_seconds = minutes * 60 + seconds
            except ValueError:
                return None
        if ' by ' not in action_part:
            return None
        action_type, player_part = map(str.strip, action_part.split(' by ', 1))
        if ' at ' in player_part:
            player, position_part = map(str.strip, player_part.split(' at ', 1))
        else:
            player, position_part = player_part.strip(), ""

        # Fix for bad behaviour events with cards
        if action_type.lower() == "bad behaviour" and ", card:" in player:
            player_parts = player.split(", card:", 1)
            player = player_parts[0].strip()
            position_part = f"card: {player_parts[1].strip()}"

        attributes = self._extract_event_attributes(position_part)
        event = {
            'timestamp': timestamp_str,
            'time_seconds': time_seconds,
            'minute': minutes,
            'second': seconds,
            'team': team,
            'player': player,
            'action': action_type,
            'event_type': self._get_event_type(action_type, position_part),
            'position': attributes.get('position'),
            'outcome': attributes.get('outcome'),
            'height': attributes.get('height'),
            'card': attributes.get('card'),
            'pass_target': attributes.get('pass_target'),
            'shot_target': attributes.get('shot_target'),
            'body_part': attributes.get('body_part'),
            'long_pass': False  # Initialize long_pass field
        }

        # Determine if it's a long pass
        event_type = event['event_type']
        action = event['action']
        # Fixed condition to check if event_type is not None before 'in' operator
        if event_type and action and 'pass' in action or event_type and event['position'] and event['pass_target']:
            distance = self._calculate_distance(event['position'], event['pass_target'])
            event['pass_length'] = distance  # Store the calculated distance
            # Check if distance is greater than 30 yards (27.432 meters)
            if distance is not None and distance > 27.432:  # 30 yards in meters
                event['long_pass'] = True

        if 'type' in attributes:
            event['type'] = attributes['type']
        return event

    def _extract_event_attributes(self, position_part: str) -> dict:
        attributes = {}
        for key, pattern in self.patterns.items():
            match = pattern.search(position_part)
            if match:
                value = match.group(1).strip()
                if key in ['position', 'pass_target', 'shot_target']:
                    try:
                        coords = [float(x) for x in value.split(',')]
                        attributes[key] = tuple(coords)
                    except Exception:
                        attributes[key] = None
                else:
                    attributes[key] = value
        # 'resulted in' should override 'outcome' if both present
        if 'resulted_in' in attributes:
            attributes['outcome'] = attributes['resulted_in']
            del attributes['resulted_in']
        return attributes

    def _get_event_type(self, action_type: str, position_part: str) -> str:
        act = action_type.lower()
        if 'pass' in act:
            if 'corner' in position_part.lower():
                return 'corner_pass'
            if 'free kick' in position_part.lower():
                return 'free_kick_pass'
            if 'goal kick' in position_part.lower():
                return 'goal_kick_pass'
            if 'throw-in' in position_part.lower():
                return 'throw_in_pass'
            return 'pass'
        if 'shot' in act:
            return 'shot'
        if 'save' in act:
            return 'save'
        if 'block' in act:
            return 'block'
        if 'interception' in act:
            return 'interception'
        if 'clearance' in act:
            return 'clearance'
        if 'duel' in act:
            return 'duel'
        if 'dribble' in act and 'dribbled past' not in act:
            return 'dribble'
        if 'dribbled past' in act:
            return 'dribbled_past'
        if 'dispossessed' in act:
            return 'dispossessed'
        if 'carry' in act:
            return 'carry'
        if 'pressure' in act:
            return 'pressure'
        if 'foul committed' in act:
            return 'foul_committed'
        if 'foul won' in act:
            return 'foul_won'
        if 'ball recovery' in act:
            return 'ball_recovery'
        if 'ball receipt' in act:
            return 'ball_receipt'
        if 'miscontrol' in act:
            return 'miscontrol'
        return act.replace(' ', '_')

    def _infer_duel_outcomes(self):
        if not self.events or not self.home_team or not self.away_team:
            # Cannot infer without events or fully initialized team context
            return

        for i in range(len(self.events)):
            event = self.events[i]

            # Only process duels that don't have an explicit outcome
            if event.get('action', '').lower() == 'duel' and event.get('outcome') is None:
                # Default inferred outcome
                event['outcome'] = 'unknown'

                if i + 1 < len(self.events):
                    next_event = self.events[i + 1]

                    # Condition: next event within 5 seconds
                    if abs(next_event['time_seconds'] - event['time_seconds']) <= 5:
                        dueling_team = event['team']

                        opposing_team = None
                        if dueling_team == self.home_team:
                            opposing_team = self.away_team
                        elif dueling_team == self.away_team:
                            opposing_team = self.home_team

                        if not opposing_team:  # Should not happen if teams are correctly parsed and event team is valid
                            continue

                        # Case 1: Next event is by the Dueling Team
                        if next_event['team'] == dueling_team:
                            if next_event['player'] == event['player']:
                                if next_event.get('event_type') in ['pass', 'shot', 'carry', 'dribble', 'shield']:
                                    event['outcome'] = 'won'
                                elif next_event.get('event_type') == 'foul_committed':
                                    event['outcome'] = 'lost_by_foul'
                                elif next_event.get('event_type') in ['dispossessed', 'miscontrol', 'error', 'offside']:
                                    event['outcome'] = 'lost'
                            elif next_event.get('event_type') == 'ball_receipt':
                                event['outcome'] = 'won'
                            elif next_event.get('event_type') == 'offside':  # Team offside
                                event['outcome'] = 'lost'
                            # If outcome is still 'unknown', it means the dueling team's action wasn't decisive for duel outcome.

                        # Case 2: Next event is by the Opposing Team
                        elif next_event['team'] == opposing_team:
                            if next_event.get('event_type') == 'pressure':
                                # Tentatively, pressure means the duel might be lost. Look further.
                                event['outcome'] = 'lost'
                                if i + 2 < len(self.events):
                                    event_after_pressure = self.events[i + 2]
                                    # Check if event_after_pressure is close in time to the pressure event
                                    if abs(event_after_pressure['time_seconds'] - next_event['time_seconds']) <= 5:
                                        # Dueling team acts after being pressured
                                        if event_after_pressure['team'] == dueling_team:
                                            if event_after_pressure.get('event_type') in ['pass', 'shot', 'carry',
                                                                                          'dribble', 'shield',
                                                                                          'ball_receipt']:
                                                if event_after_pressure.get('event_type') == 'pass':
                                                    if event_after_pressure.get('outcome') == 'Complete':
                                                        event['outcome'] = 'won'
                                                    # else: pass not complete, duel remains 'lost' (tentatively set due to pressure)
                                                else:  # For shot, carry, dribble, shield, ball_receipt
                                                    event['outcome'] = 'won'
                                            elif event_after_pressure.get(
                                                    'event_type') == 'foul_committed':  # Dueling team committed foul
                                                event['outcome'] = 'lost_by_foul'
                                            elif event_after_pressure.get('event_type') in ['dispossessed',
                                                                                            'miscontrol', 'error',
                                                                                            'offside']:  # Dueling team error
                                                event['outcome'] = 'lost'
                                            # If dueling team's action is none of these, outcome remains 'lost' from pressure.

                                        # Opposing team acts after their pressure
                                        elif event_after_pressure['team'] == opposing_team:
                                            if event_after_pressure.get('event_type') in ['clearance', 'interception',
                                                                                          'ball_recovery', 'block']:
                                                event['outcome'] = 'lost'  # Confirmed lost
                                            elif event_after_pressure.get(
                                                    'event_type') == 'foul_committed':  # Opponent fouled after pressuring
                                                event['outcome'] = 'won_by_foul'
                                            # If opposing team's action is none of these, outcome remains 'lost' from pressure.
                                # If no relevant event_after_pressure in time, outcome remains 'lost' due to uncountered pressure.

                            elif next_event.get('event_type') in [
                                'clearance', 'interception', 'ball_recovery', 'block'  # Pressure handled above
                            ]:
                                event['outcome'] = 'lost'
                            elif next_event.get(
                                    'event_type') == 'foul_committed':  # Foul committed by opponent against dueler
                                event['outcome'] = 'won_by_foul'
                            # If opposing team's next action is not one of these (and not pressure),
                            # the duel outcome remains 'unknown' unless changed by other logic.

    def to_dataframe(self) -> pd.DataFrame:
        return pd.DataFrame(self.events)

    def to_json(self) -> str:
        return json.dumps(self.events, indent=2, ensure_ascii=False)

    def save_events_json(self, output_path: str) -> None:
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(self.events, f, indent=2, ensure_ascii=False)

    def _calculate_distance(self, pos1: Optional[list], pos2: Optional[list],
                            pitch_dimensions: tuple = (120.0, 80.0)) -> Optional[float]:
        """Calculates the Euclidean distance between two coordinates using StatsBomb's coordinate system."""
        if pos1 is None or pos2 is None:
            return None

        # Parse position strings, assuming format like "(x, y)"
        try:
            # Calculate Euclidean distance directly using StatsBomb coordinates
            distance = math.dist(pos1, pos2)

            return distance
        except (AttributeError, ValueError, TypeError):
            return None


class MatchEventProducer:
    def __init__(self, host='localhost', port=5672, username='guest', password='guest',
                 exchange='match_events', routing_key='match.events'):
        self.host = host
        self.port = port
        self.username = username
        self.password = password
        self.exchange = exchange
        self.routing_key = routing_key
        self.connection = None
        self.channel = None

    def connect(self):
        if self.connection is None or self.connection.is_closed:
            credentials = pika.PlainCredentials(self.username, self.password)
            parameters = pika.ConnectionParameters(
                host=self.host,
                port=self.port,
                credentials=credentials,
                heartbeat=600
            )
            self.connection = pika.BlockingConnection(parameters)
            self.channel = self.connection.channel()
            self.channel.exchange_declare(
                exchange=self.exchange,
                exchange_type='topic',
                durable=True
            )
        return self.channel

    def close(self):
        if self.connection and self.connection.is_open:
            self.connection.close()

    def publish_event(self, event):
        try:
            channel = self.connect()
            event_json = json.dumps(event, ensure_ascii=False)
            channel.basic_publish(
                exchange=self.exchange,
                routing_key=self.routing_key,
                body=event_json.encode('utf-8'),
                properties=pika.BasicProperties(
                    delivery_mode=2,
                    content_type='application/json'
                )
            )
            return True
        except Exception as e:
            print(f"Error publishing event: {e}")
            return False

    def publish_match_events(self, events, delay_ms=0):
        success_count = 0
        for event in events:
            if self.publish_event(event):
                success_count += 1
            if delay_ms > 0:
                time.sleep(delay_ms / 1000)
        print(f"Published {success_count} of {len(events)} events to RabbitMQ")
        return success_count


def analyze_match(file_path: str):
    parser = MatchParser()
    events = parser.parse_match_file(file_path)
    team_stats = {}
    for event in events:
        team = event['team']
        if team == 'SYSTEM':
            continue  # Exclude system events from stats
        if team not in team_stats:
            team_stats[team] = {'total_events': 0, 'passes': 0, 'shots': 0, 'goals': 0}
        team_stats[team]['total_events'] += 1
        # Use event_type for pass/shot detection
        if event.get('event_type', '') in ['pass', 'corner_pass', 'free_kick_pass', 'goal_kick_pass', 'throw_in_pass']:
            team_stats[team]['passes'] += 1
        if event.get('event_type', '') == 'shot':
            team_stats[team]['shots'] += 1
            if event.get('outcome') == 'Goal':
                team_stats[team]['goals'] += 1
    print(f"Match: {parser.home_team} vs {parser.away_team}")
    print(f"Total events: {len(events)}")
    for team, stats in team_stats.items():
        print(f"\n{team} statistics:")
        print(f"  Events: {stats['total_events']}")
        print(f"  Passes: {stats['passes']}")
        print(f"  Shots: {stats['shots']}")
        print(f"  Goals: {stats['goals']}")
    return parser


def parse_and_publish(file_path, rabbitmq_config=None):
    if rabbitmq_config is None:
        rabbitmq_config = {
            'host': 'localhost',
            'port': 5672,
            'username': 'guest',
            'password': 'guest',
            'exchange': 'match_events',
            'routing_key': 'match.events'
        }
    parser = MatchParser()
    events = parser.parse_match_file(file_path)
    print(f"Parsed {len(events)} events from {file_path}")
    producer = MatchEventProducer(**rabbitmq_config)
    print("Publishing events to RabbitMQ...")
    producer.connect()
    producer.publish_match_events(events, delay_ms=50)
    producer.close()
    return events


if __name__ == "__main__":
    matches_dir = "./Matches"
    matches_parsed_dir = "./Matches_Parsed"
    os.makedirs(matches_parsed_dir, exist_ok=True)
    match_files = [f for f in os.listdir(matches_dir) if f.endswith('.txt')]
    if not match_files:
        print("No match files found in the Matches directory")
    else:
        print(f"Found {len(match_files)} match files to process")
        for match_file in match_files:
            input_path = os.path.join(matches_dir, match_file)
            output_path = os.path.join(matches_parsed_dir, f"{os.path.splitext(match_file)[0]}_parsed.json")
            parser = analyze_match(input_path)
            parser.save_events_json(output_path)
            producer = MatchEventProducer()
            producer.publish_match_events(parser.events, delay_ms=50)
        print("\nAll match files have been processed")

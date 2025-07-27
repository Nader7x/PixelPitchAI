import json
import os
import pandas as pd
import pika  # For RabbitMQ
import re
import time
from typing import List, Optional, Dict, Any

from Tokenize_dataset_special_tokens import file_path


class MatchParser:
    def __init__(self):
        self.events = []
        self.match_id = None
        self.home_team = None
        self.away_team = None

        # Compile regex patterns once for better performance
        self.position_pattern = re.compile(r'\(([^)]+)\)')
        self.outcome_pattern = re.compile(r'outcome: ([^,]+)')
        self.height_pattern = re.compile(r'height: ([^,]+)')
        self.card_pattern = re.compile(r'card: ([^,]+)')
        self.pass_target_pattern = re.compile(r'to \(([^)]+)\)')
        self.shot_target_pattern = re.compile(r'aimed at \(([^)]+)\)')
        self.body_part_pattern = re.compile(r'using ([^,]+)')

    def _get_event_type(self, action_type: str, position_part: str) -> str:
        """Map action_type and context to a canonical event_type."""
        # Lowercase for normalization
        act = action_type.lower()
        # Map common action types to event types
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
        # Default fallback
        return act.replace(' ', '_')

    def parse_match_file(self, file_path: str) -> List[dict]:
        """Parse a match file and return structured events"""
        with open(file_path, 'r', encoding='utf-8') as f:
            match_text = f.read()
        return self.parse_match_data(match_text)

    def parse_match_data(self, match_text: str) -> List[dict]:
        """Parse raw match data into structured events."""
        self.events = []
        lines = match_text.strip().split('\n')

        # Extract match info from first line if available
        for i in range(min(3, len(lines))):
            if "[MATCH START]" in lines[i]:
                # Check next line for team info
                if i + 1 < len(lines) and " faced " in lines[i + 1]:
                    match_info = lines[i + 1]
                    teams = match_info.split(" faced ")
                    self.home_team = teams[0].strip()
                    # Remove any trailing period
                    if teams[1].endswith('.'):
                        self.away_team = teams[1][:-1].strip()
                    else:
                        self.away_team = teams[1].strip()
                    self.match_id = os.path.basename(file_path).split('_')[-1].split('.')[0]
                    break

        # Process event lines in bulk for better performance
        event_index = 0
        last_timestamp = "00:00"
        last_seconds = 0
        score = {
            'Home': 0,
            'Away': 0
        }

        for line in lines:
            # Skip empty lines and match markers
            if not line or "[MATCH START]" in line or "[EVENTS START]" in line or "[SECOND HALF]" in line:
                if line == "[MATCH START]":
                    # Create a match start event
                    match_start_event = {
                        'timestamp': "00:00",
                        'time_seconds': 0,
                        'minute': 0,
                        'second': 0,
                        'team': "SYSTEM",
                        'player': "SYSTEM",
                        'action': "match_start",
                        'event_type': "match_start",
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
                        'away_team': self.away_team
                    }
                    self.events.append(match_start_event)
                    event_index += 1
                continue
            if line == "[SECOND HALF]":
                # Create a second half event
                second_half_event = {
                    'timestamp': "45:00",
                    'time_seconds': 2700,
                    'minute': 45,
                    'second': 0,
                    'team': "SYSTEM",
                    'player': "SYSTEM",
                    'action': "second_half_start",
                    'event_type': "second_half_start",
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
                    'Score': score
                }

            if line == "[MATCH END]":
                # Create a match end event using the last timestamp
                match_end_event = {
                    'timestamp': last_timestamp,
                    'time_seconds': last_seconds,
                    'minute': int(last_timestamp.split(':')[0]),
                    'second': int(last_timestamp.split(':')[1]),
                    'team': "SYSTEM",
                    'player': "SYSTEM",
                    'action': "match_end",
                    'event_type': "match_end",
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
                    'away_team': self.away_team
                }
                self.events.append(match_end_event)
                event_index += 1
                continue

            event = self._parse_event_line(line)
            if event:
                # Update last timestamp
                last_timestamp = event['timestamp']
                last_seconds = event['time_seconds']

                # Add event index and match ID
                event['event_index'] = event_index
                event['match_id'] = self.match_id
                self.events.append(event)
                event_index += 1

        return self.events

    def _parse_event_line(self, line: str) -> Optional[dict]:
        """Parse a single line of match data into an event dictionary."""
        # Split timestamp, team, and the rest - use maxsplit for better performance
        parts = line.split(' - ', 2)
        if len(parts) < 3:
            return None

        timestamp_str = parts[0].strip()
        team = parts[1].strip()
        action_part = parts[2].strip()

        # Parse the timestamp
        try:
            minutes, seconds = map(int, timestamp_str.split(':'))
            # Store as seconds for easy calculations
            time_seconds = minutes * 60 + seconds
        except ValueError:
            return None

        # Extract the action type and player
        if ' by ' not in action_part:
            return None

        action_type, player_part = action_part.split(' by ', 1)
        action_type = action_type.strip()

        # Extract player name and position
        if ' at ' in player_part:
            player, position_part = player_part.split(' at ', 1)
            player = player.strip()
        else:
            player = player_part.strip()
            position_part = ""

        # Initialize the event with all common attributes
        event = {
            'timestamp': timestamp_str,
            'time_seconds': time_seconds,
            'minute': minutes,
            'second': seconds,
            'team': team,
            'player': player,
            'action': action_type,
            'position': None,
            'outcome': None,
            'height': None,
            'card': None,
            'pass_target': None,
            'shot_target': None,
            'body_part': None,
            'event_type': action_type
        }

        # Parse position - using precompiled regex
        pos_match = self.position_pattern.search(position_part)
        if pos_match:
            try:
                pos_coords = pos_match.group(1).split(', ')
                event['position'] = (float(pos_coords[0]), float(pos_coords[1]))
            except:
                pass

        # Extract additional attributes using precompiled regex
        outcome_match = self.outcome_pattern.search(position_part)
        if outcome_match:
            event['outcome'] = outcome_match.group(1).strip()

        height_match = self.height_pattern.search(position_part)
        if height_match:
            event['height'] = height_match.group(1).strip()

        card_match = self.card_pattern.search(position_part)
        if card_match:
            event['card'] = card_match.group(1).strip()

        # Pass target
        pass_target_match = self.pass_target_pattern.search(position_part)
        if pass_target_match:
            try:
                target_coords = pass_target_match.group(1).split(', ')
                event['pass_target'] = (float(target_coords[0]), float(target_coords[1]))
            except:
                pass

        # Shot target
        shot_target_match = self.shot_target_pattern.search(position_part)
        if shot_target_match:
            try:
                target_coords = shot_target_match.group(1).split(', ')
                event['shot_target'] = (float(target_coords[0]), float(target_coords[1]))
            except:
                pass

        # Body part for shots
        body_part_match = self.body_part_pattern.search(position_part)
        if body_part_match:
            event['body_part'] = body_part_match.group(1).strip()

        # Extract 'resulted in' (e.g., resulted in penalty) if present
        resulted_in_match = re.search(r'resulted in ([^,]+)', position_part)
        if resulted_in_match:
            event['outcome'] = resulted_in_match.group(1).strip()

        # Extract 'type' (e.g., Free Kick, Corner, etc.) if present
        type_match = re.search(r'type: ([^,]+)', position_part)
        if type_match:
            event['type'] = type_match.group(1).strip()

        return event

    def to_dataframe(self) -> pd.DataFrame:
        """Convert parsed events to a pandas DataFrame."""
        return pd.DataFrame(self.events)

    def to_json(self) -> str:
        """Convert parsed events to JSON string."""
        return json.dumps(self.events, indent=2, ensure_ascii=False)

    def save_events_json(self, output_path: str) -> None:
        """Save parsed events to a JSON file."""
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(self.events, f, indent=2, ensure_ascii=False)


# RabbitMQ Producer class - kept separate from the parser
class MatchEventProducer:
    def __init__(self, host='localhost', port=5672, username='guest', password='guest',
                 exchange='match_events', routing_key='match.events'):
        """Initialize the RabbitMQ producer for match events."""
        self.host = host
        self.port = port
        self.username = username
        self.password = password
        self.exchange = exchange
        self.routing_key = routing_key
        self.connection = None
        self.channel = None

    def connect(self):
        """Establish connection to RabbitMQ server."""
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

            # Declare exchange
            self.channel.exchange_declare(
                exchange=self.exchange,
                exchange_type='topic',
                durable=True
            )

            print(f"Connected to RabbitMQ at {self.host}:{self.port}")

        return self.channel

    def close(self):
        """Close the RabbitMQ connection."""
        if self.connection and self.connection.is_open:
            self.connection.close()
            print("RabbitMQ connection closed")

    def publish_event(self, event):
        """Publish a single match event to RabbitMQ."""
        try:
            channel = self.connect()
            # Convert event to JSON string
            event_json = json.dumps(event, ensure_ascii=False)
            print(event_json)

            # Publish to exchange
            channel.basic_publish(
                exchange=self.exchange,
                routing_key=self.routing_key,
                body=event_json.encode('utf-8'),
                properties=pika.BasicProperties(
                    delivery_mode=2,  # make message persistent
                    content_type='application/json'
                )
            )
            return True
        except Exception as e:
            print(f"Error publishing event: {e}")
            return False

    def publish_match_events(self, events, delay_ms=0):
        """Publish a list of match events with optional delay between messages."""
        success_count = 0

        for event in events:
            if self.publish_event(event):
                success_count += 1

            # Optional delay to simulate real-time event flow
            if delay_ms > 0:
                time.sleep(delay_ms / 1000)

        print(f"Published {success_count} of {len(events)} events to RabbitMQ")
        return success_count


# Example of how to use the parser
def analyze_match(file_path: str):
    parser = MatchParser()
    events = parser.parse_match_file(file_path)

    # Basic statistics
    team_stats = {}
    for event in events:
        team = event['team']
        if team not in team_stats:
            team_stats[team] = {
                'total_events': 0,
                'passes': 0,
                'shots': 0,
                'goals': 0
            }

        team_stats[team]['total_events'] += 1

        if event['action'] == 'pass':
            team_stats[team]['passes'] += 1

        if event['action'] == 'shot':
            team_stats[team]['shots'] += 1
            if event.get('outcome') == 'Goal':
                team_stats[team]['goals'] += 1

    # Print stats
    print(f"Match: {parser.home_team} vs {parser.away_team}")
    print(f"Total events: {len(events)}")

    for team, stats in team_stats.items():
        print(f"\n{team} statistics:")
        print(f"  Events: {stats['total_events']}")
        print(f"  Passes: {stats['passes']}")
        print(f"  Shots: {stats['shots']}")
        print(f"  Goals: {stats['goals']}")

    return parser


# Example of how to publish events to RabbitMQ
def parse_and_publish(file_path, rabbitmq_config=None):
    """Parse match file and publish events to RabbitMQ"""
    # Default RabbitMQ configuration
    if rabbitmq_config is None:
        rabbitmq_config = {
            'host': 'localhost',
            'port': 5672,
            'username': 'guest',
            'password': 'guest',
            'exchange': 'match_events',
            'routing_key': 'match.events'
        }

    # Parse the match file
    parser = MatchParser()
    events = parser.parse_match_file(file_path)
    print(f"Parsed {len(events)} events from {file_path}")

    # Create and connect producer
    producer = MatchEventProducer(**rabbitmq_config)

    # Publish events with a small delay to simulate real-time
    # event flow
    print("Publishing events to RabbitMQ...")
    producer.connect()
    print(producer.connection)
    producer.publish_match_events(events, delay_ms=50)

    # Close connection
    producer.close()

    return events


if __name__ == "__main__":
    # Get all match files in the Matches directory
    matches_dir = "../Matches"
    matches_parsed_dir = "./Matches_Parsed"

    # Create the output directory if it doesn't exist
    os.makedirs(matches_parsed_dir, exist_ok=True)

    # Get all text files in the matches directory
    match_files = [f for f in os.listdir(matches_dir) if f.endswith('.txt')]

    if not match_files:
        print("No match files found in the Matches directory")
    else:
        print(f"Found {len(match_files)} match files to process")
        #
        # for match_file in match_files:
        #     try:
        #         input_path = os.path.join(matches_dir, match_file)
        #         output_path = os.path.join(matches_parsed_dir, f"{os.path.splitext(match_file)[0]}_parsed.json")
        #
        #         print(f"Processing {match_file}...")
        #
        #         # Choose one:
        #         # 1. Just analyze and save to JSON
        #         parser = analyze_match(input_path)
        #         parser.save_events_json(output_path)
        #
        #         # 2. Publish to RabbitMQ (uncomment to use)
        #         parse_and_publish(input_path)
        #
        #         print(f"  → Saved to {output_path}")
        #     except Exception as e:
        #         print(f"Error processing {match_file}: {e}")

        # parse_and_publish('./Matches/match_266424.txt')
        parser = analyze_match("./Matches/generated_match_output.txt")
        parser.save_events_json("./Matches_Parsed/generated_match_output_parsed.json")

        print("\nAll match files have been processed")

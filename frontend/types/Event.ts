export type MatchMeta = {
  home_team: string;
  away_team: string;
};

export type EventType =
  | 'match_start'
  | 'pass'
  | 'ball_receipt'
  | 'carry'
  | 'pressure'
  | 'duel'
  | 'interception'
  | 'block'
  | 'throw_in_pass'
  | 'foul_committed'
  | 'foul_won'
  | 'free_kick_pass'
  | 'dribble'
  | 'ball_recovery'
  | 'shot'
  | 'save'
  | 'corner_pass'
  | 'goal_kick_pass'
  | 'match_end'
  | 'first_half_end'
  | 'second_half_start';

export type Position = [number, number]; // e.g., [x, y] in meters

export type Event = {
  timestamp: string; // e.g., "00:03"
  time_seconds: number; // seconds since match start
  minute: number;
  second: number;
  team: string; // team name like "Barcelona_2016"
  player: string;
  action: string; // general description like "pass", "carry"
  position: Position; // [x, y]
  outcome: string | null; // e.g., "Complete" or null
  height: string | null; // e.g., "Ground Pass", or null
  card: string | null; // e.g., "yellow", or null
  pass_target: Position | null; // destination [x, y] for passes
  shot_target: Position | null; // destination [x, y] for shots
  body_part: string | null; // e.g., "head", "left foot", or null
  event_type: EventType;
  event_index: number;
  match_id: string;
  // Optionally, you can add these as optional properties:
  home_team?: string;
  away_team?: string;
  Score?: {
    Home: number;
    Away: number;
  };
};

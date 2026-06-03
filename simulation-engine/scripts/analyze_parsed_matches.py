import json
import os
import collections

PARSED_DIR = './Matches_Parsed'


def collect_unique_fields(parsed_dir):
    unique_actions = set()
    unique_event_types = dict()  # event_type -> set of actions
    unique_types = dict()  # type -> set of actions
    unique_outcomes = dict()  # outcome -> set of actions
    file_count = 0
    event_count = 0
    # Add counters
    action_counter = collections.Counter()
    event_type_counter = collections.Counter()
    type_counter = collections.Counter()
    outcome_counter = collections.Counter()

    for fname in os.listdir(parsed_dir):
        if not fname.endswith('.json'):
            continue
        file_count += 1
        fpath = os.path.join(parsed_dir, fname)
        with open(fpath, 'r', encoding='utf-8') as f:
            try:
                events = json.load(f)
            except Exception as e:
                print(f"Error loading {fname}: {e}")
                continue
            for event in events:
                event_count += 1
                action = event.get('action')
                if action:
                    unique_actions.add(action)
                    action_counter[action] += 1
                # event_type
                event_type = event.get('event_type')
                if event_type:
                    if event_type not in unique_event_types:
                        unique_event_types[event_type] = set()
                    if action:
                        unique_event_types[event_type].add(action)
                    event_type_counter[event_type] += 1
                # type
                t = event.get('type')
                if t:
                    if t not in unique_types:
                        unique_types[t] = set()
                    if action:
                        unique_types[t].add(action)
                    type_counter[t] += 1
                # outcome
                outcome = event.get('outcome')
                if outcome:
                    if outcome not in unique_outcomes:
                        unique_outcomes[outcome] = set()
                    if action:
                        unique_outcomes[outcome].add(action)
                    outcome_counter[outcome] += 1
    return (unique_actions, unique_event_types, unique_types, unique_outcomes, 
            file_count, event_count, action_counter, event_type_counter, type_counter, outcome_counter)


def main():
    (actions, event_types, types, outcomes, file_count, event_count, 
     action_counter, event_type_counter, type_counter, outcome_counter) = collect_unique_fields(PARSED_DIR)
    print(f"Analyzed {file_count} files, {event_count} events.")
    print(f"\nUnique actions ({len(actions)}):\n{sorted(actions)}")
    print(f"\nAction counts:")
    for action, count in action_counter.most_common():
        print(f"  {action}: {count}")
    print(f"\nUnique event_types ({len(event_types)}):")
    for et, acts in sorted(event_types.items()):
        print(f"  {et}: {sorted(acts)}")
    print(f"\nEvent type counts:")
    for et, count in event_type_counter.most_common():
        print(f"  {et}: {count}")
    print(f"\nUnique types ({len(types)}):")
    for t, acts in sorted(types.items()):
        print(f"  {t}: {sorted(acts)}")
    print(f"\nType counts:")
    for t, count in type_counter.most_common():
        print(f"  {t}: {count}")
    print(f"\nUnique outcomes ({len(outcomes)}):")
    for o, acts in sorted(outcomes.items()):
        print(f"  {o}: {sorted(acts)}")
    print(f"\nOutcome counts:")
    for o, count in outcome_counter.most_common():
        print(f"  {o}: {count}")


if __name__ == "__main__":
    main()

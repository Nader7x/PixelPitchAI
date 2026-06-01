# Football Match Simulation API

This API provides access to a fine-tuned GPT-2 model that can generate football match simulations.

## Setup

1. Install the required dependencies:

```bash
pip install -r requirements.txt
```

2. Make sure you have the model files in the `gpt2-football-finetuned` directory.

3. Ensure the `input_tokens.pt` file is available in the project root directory if you plan to use stored tokens. See
   the [Creating Input Tokens](#creating-input-tokens) section for details on how to create this file.

4. For the `/startmatch` endpoint, you need a RabbitMQ server running. You can start one using Docker:

```bash
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

This will start a RabbitMQ server with the management plugin enabled, accessible at:

- AMQP: `localhost:5672` (for the API to connect)
- Management UI: `http://localhost:15672` (username: guest, password: guest)

## Running the API

Start the API server with:

```bash
python api.py
```

This will start the server on `http://0.0.0.0:8000`.

## Dashboard

The API includes a web-based dashboard for visualizing football match simulations in real-time. The dashboard provides
an intuitive interface for:

- Starting match simulations between any two teams
- Viewing match events as they happen
- Tracking the score and match progress
- Visualizing event statistics with charts
- Monitoring the full match log

### Accessing the Dashboard

Once the server is running, you can access the dashboard at:

- Dashboard: `http://localhost:8000/`

### Dashboard Features

1. **Match Configuration Panel**
    - Set home and away team names
    - Adjust the number of tokens to generate
    - Control the temperature parameter for generation

2. **Match Events Display**
    - Real-time score updates
    - Match progress bar
    - Color-coded event cards for different event types (goals, cards, substitutions)

3. **Match Statistics**
    - Event distribution chart showing goals, cards, and substitutions for each team
    - Possession chart (simulated)

4. **Match Log**
    - Complete text log of all match events

### Using the Dashboard

1. Start the API server:
   ```bash
   python api.py
   ```

2. Open your browser and navigate to `http://localhost:8000/`

3. Enter team names and adjust parameters as desired

4. Click "Start Match" to begin the simulation

5. Watch as events are generated and displayed in real-time

### Testing the Dashboard

For convenience, a test script is provided that starts the API server and opens the dashboard in your default web
browser:

```bash
python test_dashboard.py
```

This script will:

1. Start the API server
2. Open the dashboard in your web browser
3. Provide instructions for testing
4. Wait for you to press Enter to shut down the server

## Real-Time Event Generation

The API now supports real-time event generation, allowing you to receive match events as they're generated rather than
waiting for the entire match to be simulated first. This provides a more interactive and engaging experience for users.

### How It Works

1. **Incremental Text Generation**: The model generates text in small chunks (100 tokens at a time) rather than all at
   once.

2. **Real-Time Event Parsing**: As each chunk is generated, the system parses it to identify new events.

3. **Immediate Event Delivery**: New events are immediately delivered through either:
    - WebSocket for the dashboard
    - RabbitMQ for integration with other applications

4. **Continuous Context**: The model maintains the context of previously generated events, ensuring a coherent match
   narrative.

### Benefits

- **Faster Initial Response**: Users see the first events almost immediately instead of waiting for the entire match.
- **Progressive Updates**: Events appear gradually, simulating a real match experience.
- **Lower Memory Usage**: Processing events incrementally reduces peak memory consumption.
- **Better Integration**: Real-time events enable more responsive integrations with other systems.

### Using Real-Time Events with RabbitMQ

When you call the `/startmatch` endpoint, events are now published to RabbitMQ as they're generated:

1. Start a RabbitMQ consumer to receive events:
   ```bash
   python rabbitmq_consumer_example.py
   ```

2. In another terminal, make a request to the `/startmatch` endpoint:
   ```bash
   curl -X 'POST' \
     'http://localhost:8000/startmatch' \
     -H 'Content-Type: application/json' \
     -d '{
       "home_team": "Manchester United",
       "away_team": "Liverpool"
     }'
   ```

3. Watch as events are received and processed by the consumer in real-time.

## Creating Input Tokens

The `input_tokens.pt` file is used to provide the initial context for match simulations. It contains the tokenized
representation of the match setup (teams, playing styles, etc.) that the model uses as a starting point for generating
match events.

### What are Input Tokens?

Input tokens are the encoded representation of the initial match text that describes:

- The teams that are playing
- Their playing styles and characteristics
- Other contextual information about the match

This information is used by the model to generate appropriate match events between the specified teams.

### Creating New Input Tokens

To create a new `input_tokens.pt` file for a different match setup:

1. Use the `Tokenize_dataset_special_tokens.py` script:

```bash
python Tokenize_dataset_special_tokens.py
```

2. By default, this script uses the match file at `./Matches/match_266653.txt`. To use a different match file, modify
   the `file_path` variable in the script:

```python
# ✅ File path to your input text
file_path = "./Matches/your_match_file.txt"
```

3. The script extracts the initial part of the match text (up to the `[EVENTS START]` tag) and tokenizes it.

4. The tokenized representation is saved as `input_tokens.pt` in the project root directory.

### Match File Format

Match files should follow this format:

```
[MATCH START]
Team_A faced Team_B.
Team_A had dominant possession; Team_B had low possession.
Team_A had good passing accuracy; Team_B had poor passing accuracy.
... (other team characteristics)
[EVENTS START]
... (match events, not included in the tokenization)
```

Only the text between `[MATCH START]` and `[EVENTS START]` (inclusive) is tokenized and used as input for the model.

### Example

For example, to create input tokens for a match between Barcelona_2020 and Villarreal_2020:

1. Create a match file with the appropriate initial text
2. Update the `file_path` in the script to point to your new file
3. Run the script
4. The generated `input_tokens.pt` file can now be used to simulate a match between these teams

## API Documentation

You can still access the interactive API documentation at:

- Swagger UI: `http://localhost:8000/docs`
- ReDoc: `http://localhost:8000/redoc`

The API endpoints are also available at `/api` instead of the root path.

## API Endpoints

### Dashboard Endpoint

- **URL**: `/`
- **Method**: `GET`
- **Description**: Access the web-based dashboard for visualizing match simulations
- **Response**: HTML page with the dashboard interface

### API Root Endpoint

- **URL**: `/api`
- **Method**: `GET`
- **Description**: Check if the API is running
- **Response**: `{"message": "Football Match Simulation API is running"}`

### WebSocket Endpoint

- **URL**: `/ws`
- **Protocol**: `WebSocket`
- **Description**: Real-time communication for match events
- **Actions**:
    - `start_match`: Start a new match simulation
      ```json
      {
        "action": "start_match",
        "home_team": "Manchester United",
        "away_team": "Liverpool",
        "num_tokens_to_generate": 600,
        "temperature": 0.9
      }
      ```
- **Messages**:
    - `generation_started`: Sent when match generation begins
      ```json
      {
        "type": "generation_started",
        "message": "Generating match: Manchester United vs Liverpool",
        "timestamp": "2023-11-15T12:34:56.789Z"
      }
      ```
    - `match_started`: Sent when a match starts
      ```json
      {
        "type": "match_started",
        "match_id": "a1b2c3d4e5f6g7h8i9j0",
        "timestamp": "2023-11-15T12:34:56.789Z"
      }
      ```
    - `match_event`: Sent for each match event as it's generated
      ```json
      {
        "type": "match_event",
        "event": {
          "match_id": "a1b2c3d4e5f6g7h8i9j0",
          "type": "goal",
          "minute": "23",
          "text": "Salah scores with a powerful shot!",
          "player": "Salah",
          "team": "Liverpool",
          "home_team": "Manchester United",
          "away_team": "Liverpool",
          "event_index": 5,
          "timestamp": 1699967696
        },
        "timestamp": "2023-11-15T12:35:10.123Z"
      }
      ```
    - `match_completed`: Sent when a match completes
      ```json
      {
        "type": "match_completed",
        "match_id": "a1b2c3d4e5f6g7h8i9j0",
        "events_count": 25,
        "timestamp": "2023-11-15T12:38:45.678Z"
      }
      ```
    - `error`: Sent when an error occurs
      ```json
      {
        "type": "error",
        "message": "Error message",
        "timestamp": "2023-11-15T12:34:56.789Z"
      }
      ```
- **Real-Time Behavior**:
    - Events are now truly generated in real-time as the model produces text
    - Each chunk of generated text is parsed for new events immediately
    - Events are sent to the client as soon as they're detected
    - The dashboard visualizes events as they arrive, creating a more dynamic experience

### Model Information

- **URL**: `/model-info`
- **Method**: `GET`
- **Description**: Get information about the loaded model
- **Response**: JSON object with model details

### Generate Text

- **URL**: `/generate`
- **Method**: `POST`
- **Description**: Generate football match simulation text
- **Request Body**:
  ```json
  {
    "input_text": "Optional custom input text",
    "use_stored_tokens": true,
    "num_tokens_to_generate": 400,
    "temperature": 0.9,
    "top_p": 0.95,
    "top_k": 50,
    "max_length": 1024
  }
  ```
- **Response**:
  ```json
  {
    "generated_text": "The generated football match simulation text",
    "execution_time": 1.234
  }
  ```

### Start Match Simulation

- **URL**: `/startmatch`
- **Method**: `POST`
- **Description**: Start a match simulation between two teams, generate events in real-time, and send them to RabbitMQ
  as they're generated
- **Request Body**:
  ```json
  {
    "home_team": "Manchester United",
    "away_team": "Liverpool",
    "num_tokens_to_generate": 800,
    "temperature": 0.9,
    "top_p": 0.95,
    "top_k": 50,
    "max_length": 1024,
    "rabbitmq_host": "localhost",
    "rabbitmq_port": 5672,
    "rabbitmq_queue": "match_events"
  }
  ```
- **Response**:
  ```json
  {
    "match_id": "a1b2c3d4e5f6g7h8i9j0",
    "home_team": "Manchester United",
    "away_team": "Liverpool",
    "events_count": 25,
    "execution_time": 5.678,
    "preview": "The first 200 characters of the generated text..."
  }
  ```
- **Real-Time Behavior**:
    - Events are parsed and published to RabbitMQ as they're generated, not all at once at the end
    - The API response is returned only after all events have been generated and published
    - To receive events in real-time, connect a consumer to the specified RabbitMQ queue before making the request

## Parameters

### Common Parameters

- **num_tokens_to_generate**: Number of tokens to generate (default: 400 for `/generate`, 800 for `/startmatch`)
- **temperature**: Controls randomness in generation (default: 0.9)
- **top_p**: Nucleus sampling parameter (default: 0.95)
- **top_k**: Top-k sampling parameter (default: 50)
- **max_length**: Maximum length of context window (default: 1024)

### Generate Endpoint Parameters

- **input_text**: Custom input text to use instead of stored tokens (optional)
- **use_stored_tokens**: Whether to use the stored input tokens (default: true)

### Start Match Endpoint Parameters

- **home_team**: Name of the home team (required)
- **away_team**: Name of the away team (required)
- **rabbitmq_host**: RabbitMQ server hostname (default: "localhost")
- **rabbitmq_port**: RabbitMQ server port (default: 5672)
- **rabbitmq_queue**: RabbitMQ queue name to publish events to (default: "match_events")

## Example Usage

### Generate Endpoint

Using curl:

```bash
curl -X 'POST' \
  'http://localhost:8000/generate' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
  "use_stored_tokens": true,
  "num_tokens_to_generate": 400,
  "temperature": 0.9
}'
```

Using Python requests:

```python
import requests
import json

url = "http://localhost:8000/generate"
payload = {
    "use_stored_tokens": True,
    "num_tokens_to_generate": 400,
    "temperature": 0.9
}
headers = {
    "accept": "application/json",
    "Content-Type": "application/json"
}

response = requests.post(url, json=payload, headers=headers)
print(json.dumps(response.json(), indent=2))
```

### Start Match Endpoint

Using curl:

```bash
curl -X 'POST' \
  'http://localhost:8000/startmatch' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
  "home_team": "Manchester United",
  "away_team": "Liverpool",
  "num_tokens_to_generate": 800,
  "temperature": 0.9,
  "rabbitmq_host": "localhost",
  "rabbitmq_port": 5672,
  "rabbitmq_queue": "match_events"
}'
```

Using Python requests:

```python
import requests
import json

url = "http://localhost:8000/startmatch"
payload = {
    "home_team": "Manchester United",
    "away_team": "Liverpool",
    "num_tokens_to_generate": 800,
    "temperature": 0.9,
    "rabbitmq_host": "localhost",
    "rabbitmq_port": 5672,
    "rabbitmq_queue": "match_events"
}
headers = {
    "accept": "application/json",
    "Content-Type": "application/json"
}

response = requests.post(url, json=payload, headers=headers)
print(json.dumps(response.json(), indent=2))
```

## Event Format

Events published to RabbitMQ have the following structure:

```json
{
  "match_id": "a1b2c3d4e5f6g7h8i9j0",
  "type": "goal",
  "minute": "23",
  "text": "Smith scores with a powerful shot from outside the box!",
  "player": "Smith",
  "team": "Manchester United",
  "home_team": "Manchester United",
  "away_team": "Liverpool",
  "timestamp": 1634567890,
  "event_index": 5
}
```

Event types include:

- `goal`: A goal was scored
- `yellow_card`: A yellow card was shown
- `red_card`: A red card was shown
- `substitution`: A player substitution
- `unknown`: Other events that couldn't be classified

## Consuming Events from RabbitMQ

A simple example consumer is provided in `rabbitmq_consumer_example.py`. This consumer connects to RabbitMQ, listens for
events on the `match_events` queue, and processes them based on their type.

To run the consumer:

```bash
python rabbitmq_consumer_example.py
```

You can run this in one terminal while running the API in another terminal. Then, when you make a request to the
`/startmatch` endpoint, you'll see the events being processed by the consumer in real-time.

Example workflow:

1. Start RabbitMQ (if not already running):
   ```bash
   docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
   ```

2. Start the consumer in one terminal:
   ```bash
   python rabbitmq_consumer_example.py
   ```

3. Start the API in another terminal:
   ```bash
   python api.py
   ```

4. Make a request to the `/startmatch` endpoint:
   ```bash
   python test_startmatch.py
   ```

5. Watch the events being processed by the consumer in real-time.

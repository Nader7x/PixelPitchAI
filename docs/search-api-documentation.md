# Search API Documentation

The Search API provides comprehensive search functionality across all football entities in the Footex system. It supports both exact matching and fuzzy search capabilities with advanced ranking and relevance scoring.

## Base URL

```
/api/search
```

## Endpoints Overview

| Endpoint       | Method | Description                         |
| -------------- | ------ | ----------------------------------- |
| `/`            | GET    | Global search across all entities   |
| `/all`         | GET    | Advanced search with fuzzy matching |
| `/teams`       | GET    | Search specifically for teams       |
| `/players`     | GET    | Search specifically for players     |
| `/coaches`     | GET    | Search specifically for coaches     |
| `/stadiums`    | GET    | Search specifically for stadiums    |
| `/seasons`     | GET    | Search specifically for seasons     |
| `/suggestions` | GET    | Get search suggestions/autocomplete |

---

## 1. Global Search

**Endpoint:** `GET /api/search`

**Description:** Performs a global search across all entities (teams, players, coaches, matches, stadiums) with relevance ranking.

**Parameters:**

- `query` (string, required): Search term (minimum 2 characters)
- `page` (int, optional): Page number, 1-based (default: 1)
- `pageSize` (int, optional): Results per page, 1-50 (default: 10)

**Response:**

```json
{
  "totalResults": 42,
  "currentPage": 1,
  "totalPages": 5,
  "pageSize": 10,
  "items": [
    {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "type": "Team",
      "name": "Manchester United",
      "description": "Manchester - Founded in 1878",
      "thumbnailUrl": "/images/teams/manchester-united-logo.png",
      "url": "/teams/123e4567-e89b-12d3-a456-426614174000",
      "additionalData": {
        "location": "Manchester",
        "league": "Premier League"
      }
    }
  ]
}
```

**Example:**

```bash
GET /api/search?query=manchester&page=1&pageSize=10
```

---

## 2. Advanced Search with Fuzzy Matching

**Endpoint:** `GET /api/search/all`

**Description:** Advanced search with optional fuzzy matching capabilities across all entities.

**Parameters:**

- `query` (string, required): Search term (minimum 2 characters)
- `page` (int, optional): Page number, 1-based (default: 1)
- `pageSize` (int, optional): Results per page, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:** Same as Global Search

**Example:**

```bash
GET /api/search/all?query=manchster&enableFuzzySearch=true&page=1&pageSize=10
```

---

## 3. Team Search

**Endpoint:** `GET /api/search/teams`

**Description:** Search specifically for teams by name, city, country, or league.

**Parameters:**

- `query` (string, required): Search term
- `limit` (int, optional): Maximum results, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:**

```json
[
  {
    "id": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Manchester United",
    "city": "Manchester",
    "country": "England",
    "league": "Premier League",
    "foundationDate": "1878-03-01T00:00:00Z",
    "logo": "/images/teams/manchester-united-logo.png"
  }
]
```

**Example:**

```bash
GET /api/search/teams?query=united&limit=5&enableFuzzySearch=false
```

---

## 4. Player Search

**Endpoint:** `GET /api/search/players`

**Description:** Search for players by name, position, nationality, or team.

**Parameters:**

- `query` (string, required): Search term
- `limit` (int, optional): Maximum results, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:**

```json
[
  {
    "id": "456e7890-e89b-12d3-a456-426614174001",
    "fullName": "Cristiano Ronaldo",
    "knownName": "CR7",
    "nationality": "Portugal",
    "position": "Forward",
    "shirtNumber": 7,
    "photoUrl": "/images/players/ronaldo.jpg",
    "team": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Manchester United"
    }
  }
]
```

**Example:**

```bash
GET /api/search/players?query=ronaldo&limit=5
```

---

## 5. Coach Search

**Endpoint:** `GET /api/search/coaches`

**Description:** Search for coaches by name, role, nationality, or team.

**Parameters:**

- `query` (string, required): Search term
- `limit` (int, optional): Maximum results, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:**

```json
[
  {
    "id": "789e0123-e89b-12d3-a456-426614174002",
    "firstName": "Erik",
    "lastName": "ten Hag",
    "role": "Head Coach",
    "nationality": "Netherlands",
    "yearsOfExperience": 15,
    "photoUrl": "/images/coaches/ten-hag.jpg",
    "team": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Manchester United"
    }
  }
]
```

**Example:**

```bash
GET /api/search/coaches?query=ten hag&limit=5
```

---

## 6. Stadium Search

**Endpoint:** `GET /api/search/stadiums`

**Description:** Search for stadiums by name, city, or country.

**Parameters:**

- `query` (string, required): Search term
- `limit` (int, optional): Maximum results, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:**

```json
[
  {
    "id": "012e3456-e89b-12d3-a456-426614174003",
    "name": "Old Trafford",
    "city": "Manchester",
    "country": "England",
    "capacity": 74140,
    "surfaceType": "Grass"
  }
]
```

**Example:**

```bash
GET /api/search/stadiums?query=trafford&limit=5
```

---

## 7. Season Search

**Endpoint:** `GET /api/search/seasons`

**Description:** Search for seasons by name, league, or country.

**Parameters:**

- `query` (string, required): Search term
- `limit` (int, optional): Maximum results, 1-50 (default: 10)
- `enableFuzzySearch` (boolean, optional): Enable approximate matching (default: false)

**Response:**

```json
[
  {
    "id": "345e6789-e89b-12d3-a456-426614174004",
    "name": "Premier League 2023/24",
    "leagueName": "Premier League",
    "country": "England",
    "startDate": "2023-08-12T00:00:00Z",
    "endDate": "2024-05-19T00:00:00Z"
  }
]
```

**Example:**

```bash
GET /api/search/seasons?query=premier league&limit=5
```

---

## 8. Search Suggestions/Autocomplete

**Endpoint:** `GET /api/search/suggestions`

**Description:** Get search suggestions for autocomplete functionality.

**Parameters:**

- `query` (string, required): Partial search term (minimum 1 character)
- `limit` (int, optional): Maximum suggestions, 1-20 (default: 5)

**Response:**

```json
[
  {
    "text": "Manchester United",
    "type": "Team",
    "description": "Manchester - Premier League"
  },
  {
    "text": "Manchester City",
    "type": "Team",
    "description": "Manchester - Premier League"
  },
  {
    "text": "Marcus Rashford",
    "type": "Player",
    "description": "Forward - Manchester United"
  }
]
```

**Example:**

```bash
GET /api/search/suggestions?query=man&limit=5
```

---

## Error Responses

All endpoints return standard HTTP status codes:

- `200 OK`: Successful request
- `400 Bad Request`: Invalid parameters (e.g., empty query, invalid page/limit values)
- `500 Internal Server Error`: Server error

**Error Response Format:**

```json
{
  "error": "Search query must be at least 2 characters long"
}
```

---

## Search Features

### 1. Fuzzy Search

When `enableFuzzySearch=true`, the API performs approximate matching using Levenshtein distance calculation, allowing for:

- Typos and misspellings
- Partial word matches
- Similar sounding names

### 2. Multi-field Search

Each entity type searches across multiple relevant fields:

- **Teams**: Name, city, country, league
- **Players**: Full name, known name, position, nationality, team name
- **Coaches**: First name, last name, role, nationality, team name
- **Stadiums**: Name, city, country
- **Seasons**: Name, league name, country

### 3. Relevance Ranking

The global search endpoints use advanced ranking algorithms that consider:

- Exact matches vs. partial matches
- Field importance (name vs. description)
- Entity type priority (Teams > Players > Coaches > Matches > Stadiums)

### 4. Performance Optimization

- Results are limited to prevent performance issues
- Parallel processing for multi-entity searches
- Database query optimization with proper indexing
- No-tracking queries for read-only operations

---

## Usage Tips

1. **For autocomplete/typeahead**: Use `/suggestions` endpoint with short queries
2. **For comprehensive search**: Use `/all` endpoint with fuzzy search enabled
3. **For specific entity searches**: Use dedicated endpoints (`/teams`, `/players`, etc.)
4. **For best performance**: Use appropriate limits and avoid very generic queries
5. **For typo tolerance**: Enable fuzzy search for user-facing search features

---

## Rate Limiting

Consider implementing rate limiting for search endpoints to prevent abuse:

- Suggested limit: 100 requests per minute per IP
- Higher limits for authenticated users
- Lower limits for fuzzy search operations (more CPU intensive)

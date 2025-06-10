# SearchController API Documentation

This document describes the endpoints provided by the `SearchController` for implementing search features in the frontend UI.

---

## Base URL

`/api/search`

---

## 1. Global Search

**Endpoint:** `GET /api/search`

**Description:**
Performs a global search across all entities (teams, players, coaches, stadiums, matches) with ranking and relevance.

**Query Parameters:**
- `query` (string, required): The search term (minimum 2 characters).
- `page` (int, optional): Page number (default: 1).
- `pageSize` (int, optional): Number of results per page (default: 10, max: 50).

**Response:**
Returns a paginated list of search results.

**Sample Request:**
```
GET /api/search?query=ronaldo&page=1&pageSize=10
```

**Sample Response:**
```json
{
  "totalResults": 42,
  "currentPage": 1,
  "totalPages": 5,
  "pageSize": 10,
  "items": [
    {
      "id": "123",
      "type": "Player",
      "name": "Cristiano Ronaldo",
      "description": "Nationality: Portugal, Position: Forward, Team: Al Nassr",
      "thumbnailUrl": "https://...",
      "url": "/players/123",
      "additionalData": {
        "Nationality": "Portugal",
        "Position": "Forward",
        "TeamName": "Al Nassr"
      }
    }
    // ...more items
  ]
}
```

---

## 2. Advanced Search with Strategy

**Endpoint:** `GET /api/search/strategy`

**Description:**
Performs a search with a selectable strategy (full-text, fuzzy, hybrid, or auto).

**Query Parameters:**
- `query` (string, required): The search term (minimum 2 characters).
- `strategy` (enum, optional): Search strategy (`Auto`, `FullText`, `Fuzzy`, `Hybrid`). Default: `Auto`.
- `page` (int, optional): Page number (default: 1).
- `pageSize` (int, optional): Number of results per page (default: 10, max: 50).

**Sample Request:**
```
GET /api/search/strategy?query=ronaldo&strategy=Fuzzy
```

---

## 3. Advanced Search with Filters

**Endpoint:** `POST /api/search/filtered`

**Description:**
Performs a search with advanced filters and sorting.

**Request Body:**
Send a JSON object with the following fields:
- `query` (string, required): Search term.
- `entityTypes` (array of string, optional): Entity types to search (e.g., `["Team", "Player"]`).
- `country`, `league`, `position`, `role` (string, optional): Filter by these fields.
- `fromDate`, `toDate` (ISO date, optional): Filter matches by date.
- `minCapacity`, `maxCapacity` (int, optional): Filter stadiums by capacity.
- `page`, `pageSize` (int, optional): Pagination.
- `sortBy` (string, optional): Field to sort by.
- `sortDescending` (bool, optional): Sort order.
- `enableFuzzySearch` (bool, optional): Enable fuzzy search.
- `strategy` (enum, optional): Search strategy.

**Sample Request:**
```json
POST /api/search/filtered
{
  "query": "madrid",
  "entityTypes": ["Team", "Stadium"],
  "country": "Spain",
  "page": 1,
  "pageSize": 10,
  "sortBy": "Name",
  "sortDescending": false
}
```

---

## 4. Unified Search

**Endpoint:** `GET /api/search/unified`

**Description:**
Performs a unified search across selected entity types.

**Query Parameters:**
- `query` (string, required): Search term (minimum 2 characters).
- `entityTypes` (string, optional): Comma-separated list of entity types (e.g., `Team,Player`).
- `page` (int, optional): Page number (default: 1).
- `pageSize` (int, optional): Results per page (default: 10, max: 50).

**Sample Request:**
```
GET /api/search/unified?query=ronaldo&entityTypes=Player,Coach&page=1&pageSize=10
```

---

## 5. Search All Entities (with Fuzzy Option)

**Endpoint:** `GET /api/search/all`

**Description:**
Searches all entities with an option to enable fuzzy/approximate matching.

**Query Parameters:**
- `query` (string, required): Search term (minimum 2 characters).
- `page` (int, optional): Page number (default: 1).
- `pageSize` (int, optional): Results per page (default: 10, max: 50).
- `enableFuzzySearch` (bool, optional): Enable fuzzy search (default: false).

---

## 6. Search Teams

**Endpoint:** `GET /api/search/teams`

**Description:**
Searches for teams with advanced ranking and relevance.

**Query Parameters:**
- `query` (string, required): Search term.
- `limit` (int, optional): Max results (default: 10, max: 50).
- `enableFuzzySearch` (bool, optional): Enable fuzzy search.
- `advanced` (bool, optional): Use advanced ranking.

---

## 7. Search Players

**Endpoint:** `GET /api/search/players`

**Description:**
Searches for players with advanced ranking and filtering.

**Query Parameters:**
- `query` (string, required): Search term.
- `limit` (int, optional): Max results (default: 10, max: 50).
- `enableFuzzySearch` (bool, optional): Enable fuzzy search.
- `advanced` (bool, optional): Use advanced ranking.

---

## 8. Search Suggestions

**Endpoint:** `GET /api/search/suggestions`

**Description:**
Returns search suggestions for autocomplete.

**Query Parameters:**
- `query` (string, required): Partial search term.
- `limit` (int, optional): Max suggestions (default: 5).

**Sample Response:**
```json
[
  {
    "text": "Real Madrid",
    "type": "Team",
    "description": "Madrid - La Liga",
    "relevance": 0.95,
    "thumbnailUrl": "https://...",
    "additionalData": {}
  }
  // ...more suggestions
]
```

---

## 9. Search Analytics

**Endpoint:** `GET /api/search/analytics`

**Description:**
Returns analytics and statistics for a search query.

**Query Parameters:**
- `query` (string, required): Search term.

**Sample Response:**
```json
{
  "query": "ronaldo",
  "strategyUsed": "Hybrid",
  "searchDuration": "00:00:00.1234567",
  "totalResultsFound": 42,
  "resultsByEntityType": {
    "Team": 2,
    "Player": 30,
    "Coach": 5,
    "Stadium": 1,
    "Match": 4
  },
  "averageRelevanceScore": 0.87,
  "usedFallbackSearch": false,
  "searchSuggestions": ["Cristiano Ronaldo", "Ronaldo Nazario"]
}
```

---

## General Notes

- All endpoints return HTTP 400 for invalid queries (e.g., too short).
- Pagination is 1-based.
- All entity URLs are relative and should be prefixed with your frontend base path.
- The `type` field in results indicates the entity type (`Team`, `Player`, `Coach`, `Stadium`, `Match`).
- Use `/api/search/suggestions` for implementing autocomplete in the UI.

---

For more details on request/response payloads or UI flows, contact the backend team.


# API Documentation

This document describes all available API endpoints in the Footex project. Use this as a reference for integrating with the backend from the frontend application.

## Key Features

### Authentication

Most endpoints require JWT authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### Performance and Caching

- Many endpoints implement intelligent caching for improved performance
- Cache status is indicated via `X-Cache-Hit` header in responses
- Performance monitoring is active across match-related operations
- Real-time updates are provided via SignalR for live match events

### Match Simulation System

The API includes advanced match simulation capabilities:

- AI-powered match prediction and simulation
- Asynchronous processing with webhook callbacks
- Real-time notifications during simulation
- Health checks for external AI services
- Integration with live match statistics

### Live Match Statistics

- Real-time statistics tracking during matches
- Performance-optimized caching for live data
- SignalR integration for real-time updates
- Comprehensive match event monitoring

---

## AuthController (`/api/auth`)

### POST `/register`

- **Description:** Register a new user with optional profile image upload
- **Body:** `RegisterUserDto` (form-data)
  - `Email`: User email address
  - `Password`: User password
  - `FirstName`: User's first name
  - `LastName`: User's last name
  - `Image` (optional): Profile image file
- **Returns:** `RegisterUserCommandResponse`
- **Features:**
  - Image upload to Azure Blob Storage
  - Automatic image URL assignment
  - Email confirmation workflow

### POST `/login`

- **Description:** Authenticate user and establish session
- **Body:** `UserLoginDto` (JSON)
  - `Email`: User email
  - `Password`: User password
- **Returns:** `LoginUserCommandResponse`
- **Features:**
  - IP address tracking for security
  - Automatic refresh token cookie setting
  - HttpOnly, Secure cookies for enhanced security
  - 7-day refresh token expiration

### POST `/refresh-token`

- **Description:** Refresh JWT access token using stored refresh token
- **Auth:** Yes
- **Body:** None (uses refresh token from cookie)
- **Returns:** `RefreshTokenCommandResponse`
- **Features:**
  - Automatic cookie-based token retrieval
  - New refresh token generation and cookie update
  - IP address validation for security
  - Seamless token rotation

### POST `/forgot-password`

- **Description:** Initiate password reset process
- **Body:** `ForgotPasswordDto` (JSON)
  - `Email`: User email address
- **Returns:** `ForgotPasswordCommandResponse`
- **Features:**
  - Secure token generation
  - Email delivery with reset link

### POST `/reset-password`

- **Description:** Complete password reset with token validation
- **Body:** `ResetPasswordDto` (JSON)
  - `NewPassword`: New password
- **Query:** `email`, `token` (URL-decoded)
- **Returns:** `ResetPasswordCommandResponse`
- **Features:**
  - URL decoding for email and token parameters
  - Secure token validation
  - Password strength validation

### POST `/confirm-email`

- **Description:** Confirm user email address with validation token
- **Query:** `userId`, `token`
- **Returns:** `ConfirmEmailCommandResponse`
- **Features:**
  - Token-based email verification
  - Account activation upon confirmation

### POST `/resend-email-confirmation`

- **Description:** Resend email confirmation for unverified accounts
- **Body:** `ResendEmailConfirmationDto` (JSON)
  - `Email`: User email address
- **Returns:** `ResendEmailConfirmationCommandResponse`
- **Features:**
  - Duplicate confirmation handling
  - Rate limiting protection

### POST `/revoke-token`

- **Description:** Revoke refresh token for secure logout
- **Auth:** Yes
- **Body:** `RevokeTokenCommand` (JSON)
  - `RefreshToken` (optional): Token to revoke
- **Returns:** Success/Failure response
- **Features:**
  - Token from request body or cookie
  - IP address logging for security audit
  - Immediate token invalidation

### GET `/profile`

- **Description:** Get current authenticated user's profile
- **Auth:** Yes
- **Returns:** `GetUserProfileQueryResponse`
- **Features:**
  - JWT token claim extraction
  - Complete user profile data
  - Image URL inclusion

### GET `/profile/{id}`

- **Description:** Get specific user's profile by ID
- **Auth:** Yes
- **Path:** `id` - User ID to retrieve
- **Returns:** `GetUserProfileQueryResponse`
- **Features:**
  - Cross-user profile access
  - Complete profile information

### POST `/manual-refresh`

- **Description:** Manual token refresh for testing and Swagger UI
- **Body:** `string` (refresh token in request body)
- **Returns:** `RefreshTokenCommandResponse`
- **Features:**
  - Direct token input for testing
  - Swagger UI compatibility
  - Same security as automatic refresh
  - Cookie setting for subsequent requests

### PUT `/update`

- **Description:** Update user profile with optional image management
- **Auth:** Yes
- **Body:** `UpdateUserDto` (form-data)
  - `Id`: User ID
  - `FirstName`: Updated first name
  - `LastName`: Updated last name
  - `Email`: Updated email
  - `Image` (optional): New profile image
- **Returns:** `UpdateUserCommandResponse`
- **Features:**
  - Automatic old image deletion from storage
  - New image upload and URL assignment
  - Profile data validation
  - Atomic update operations

---

## CoachesController (`/api/coaches`)

### GET `/filter`

- **Description:** Get all coaches with intelligent caching and filtering capabilities
- **Query:**
  - `nationality` (optional): Filter by coach nationality
  - `teamId` (optional): Filter by associated team ID
- **Returns:** `GetAllCoachesQueryResponse`
- **Cache:** Yes (5-minute TTL with X-Cache-Hit header)
- **Features:**
  - Dynamic cache key generation based on query parameters
  - Performance-optimized database queries
  - Automatic cache invalidation on data changes

### GET `/{id}`

- **Description:** Get specific coach by ID with caching
- **Path:** `id` - Coach ID to retrieve
- **Returns:** `GetCoachByIdQueryResponse`
- **Cache:** Yes (5-minute TTL with X-Cache-Hit header)
- **Features:**
  - Individual coach caching
  - Automatic 404 handling for non-existent coaches
  - Cache invalidation on coach updates

### POST `/`

- **Description:** Create new coach with optional photo upload
- **Auth:** Admin
- **Body:** `CreateCoachDto` (form-data)
  - `Name`: Coach full name
  - `Nationality`: Coach nationality
  - `DateOfBirth`: Coach date of birth
  - `Biography`: Coach biography/description
  - `TeamId` (optional): Associated team ID
  - `Photo` (optional): Coach photo file
  - `PhotoUrl` (optional): Existing photo URL
- **Returns:** `CreateCoachCommandResponse`
- **Features:**
  - Azure Blob Storage integration for photo uploads
  - Automatic photo URL assignment
  - Cache invalidation for coach lists
  - Admin role validation

### PUT `/{id}`

- **Description:** Update existing coach with advanced photo management
- **Auth:** Admin
- **Path:** `id` - Coach ID to update
- **Body:** `UpdateCoachDto` (form-data)
  - `Name`: Updated coach name
  - `Nationality`: Updated nationality
  - `DateOfBirth`: Updated date of birth
  - `Biography`: Updated biography
  - `TeamId`: Updated team association
  - `Photo` (optional): New photo file
  - `PhotoUrl` (optional): New photo URL
- **Returns:** `UpdateCoachCommandResponse`
- **Features:**
  - Automatic old photo deletion from storage
  - New photo upload and URL assignment
  - Existence validation before update
  - Comprehensive cache invalidation (specific coach + lists)
  - Atomic update operations

### DELETE `/{id}`

- **Description:** Delete coach with complete cleanup
- **Auth:** Admin
- **Path:** `id` - Coach ID to delete
- **Returns:** `DeleteCoachCommandResponse`
- **Features:**
  - Automatic photo deletion from Azure Blob Storage
  - Existence validation before deletion
  - Complete cache invalidation (specific coach + lists)
  - Resource cleanup and validation
  - Admin role validation

---

## MatchesController (`/api/matches`)

### GET `/`

- **Description:** Get all matches (filterable with enhanced caching)
- **Query:** `seasonId`, `teamId`, `status`, `fromDate`, `toDate`, `matchWeek`
- **Returns:** `GetAllMatchesQueryResponse`
- **Cache:** Yes (X-Cache-Hit header included in response)
- **Performance:** Optimized with performance monitoring

### GET `/{id}`

- **Description:** Get match by ID
- **Returns:** `GetMatchByIdQueryResponse`

### GET `/Details/{MatchId}`

- **Description:** Get match by ID with details
- **Returns:** `GetMatchByIdWithDetailsQueryResponse`

### GET `/{userId}`

- **Description:** Get matches for a specific user
- **Auth:** Yes
- **Returns:** User-specific match data
- **Cache:** Yes (cached for performance)

### POST `/`

- **Description:** Create match
- **Auth:** Admin, Manager
- **Body:** `CreateMatchDto` (JSON)
- **Returns:** `CreateMatchCommandResponse`

### PUT `/{id}`

- **Description:** Update match
- **Auth:** Admin, Manager
- **Body:** `UpdateMatchDto` (JSON)
- **Returns:** `UpdateMatchCommandResponse`

### DELETE `/{id}`

- **Description:** Delete match
- **Auth:** Admin
- **Returns:** `DeleteMatchCommandResponse`

### POST `/simulateMatch/{userId}`

- **Description:** Simulate a match using AI service integration
- **Auth:** Yes
- **Body:** `SimulateMatchDto` (JSON)
  - `HomeTeamId`: ID of home team
  - `AwayTeamId`: ID of away team
  - `HomeTeamName`: Name of home team
  - `AwayTeamName`: Name of away team
  - `HomeTeamSeason`: Home team season data
  - `AwayTeamSeason`: Away team season data
- **Returns:** `CreateMatchCommandResponse` with `ApiResponse` containing simulation details
- **Features:**
  - Checks for existing live matches before simulation
  - Health check validation of AI service
  - Webhook registration for async result handling
  - Real-time notifications via SignalR
  - Performance monitoring integration

### POST `/webhookNotification/{simulationId}`

- **Description:** Webhook endpoint for receiving simulation results from AI service
- **Body:** `WebhookNotificationPayload` (JSON)
  - `SimulationId`: ID of the simulation
  - `Status`: Status of simulation (completed, failed, etc.)
  - `ResultUrl`: URL to fetch detailed results (optional)
  - Additional simulation data
- **Returns:** Success/failure response with simulation ID
- **Features:**
  - Validates simulation ID consistency
  - Handles different simulation statuses (completed, failed)
  - Updates local match status
  - Processes simulation results
  - Error handling and logging

---

## PlayersController (`/api/players`)

### GET `/`

- **Description:** Get all players with intelligent caching, filtering, and pagination
- **Query:**
  - `nationality` (optional): Filter by player nationality
  - `preferredFoot` (optional): Filter by preferred foot (Left/Right)
  - `teamId` (optional): Filter by associated team ID
  - `pageNumber` (optional): Page number for pagination
  - `pageSize` (optional): Number of items per page
- **Returns:** `GetAllPlayersQueryResponse`
- **Cache:** Yes (5-minute TTL with X-Cache-Hit header)
- **Features:**
  - Dynamic cache key generation based on all query parameters
  - Performance-optimized database queries with pagination
  - Automatic cache invalidation on data changes
  - Comprehensive filtering capabilities

### GET `/{id}`

- **Description:** Get specific player by ID with caching
- **Path:** `id` - Player ID to retrieve
- **Returns:** `GetPlayerByIdQueryResponse`
- **Cache:** Yes (5-minute TTL with X-Cache-Hit header)
- **Features:**
  - Individual player caching
  - Automatic 404 handling for non-existent players
  - Cache invalidation on player updates

### POST `/`

- **Description:** Create new player with optional photo upload
- **Auth:** Admin
- **Body:** `CreatePlayerDto` (form-data)
  - `Name`: Player full name
  - `Nationality`: Player nationality
  - `DateOfBirth`: Player date of birth
  - `Position`: Player position
  - `PreferredFoot`: Player preferred foot (Left/Right)
  - `TeamId` (optional): Associated team ID
  - `Photo` (optional): Player photo file
  - `PhotoUrl` (optional): Existing photo URL
- **Returns:** `CreatePlayerCommandResponse`
- **Features:**
  - Azure Blob Storage integration for photo uploads
  - Automatic photo URL assignment
  - Cache invalidation for player lists
  - Admin role validation
  - Returns 201 Created with location header

### PUT `/{id}`

- **Description:** Update existing player with advanced photo management
- **Auth:** Admin
- **Path:** `id` - Player ID to update
- **Body:** `UpdatePlayerDto` (form-data)
  - All fields from CreatePlayerDto can be updated
  - `Photo` (optional): New photo file
  - `PhotoUrl` (optional): New photo URL
- **Returns:** `UpdatePlayerCommandResponse`
- **Features:**
  - Automatic old photo deletion from storage
  - New photo upload and URL assignment
  - Existence validation before update
  - Comprehensive cache invalidation (specific player + lists)
  - Atomic update operations

### DELETE `/{id}`

- **Description:** Delete player with complete cleanup
- **Auth:** Admin
- **Path:** `id` - Player ID to delete
- **Returns:** `DeletePlayerCommandResponse`
- **Features:**
  - Automatic photo deletion from Azure Blob Storage
  - Existence validation before deletion
  - Complete cache invalidation (specific player + lists)
  - Resource cleanup and validation
  - Admin role validation

---

## SearchController (`/api/search`)

### GET `/`

- **Description:** Global search across all entities with ranking and relevance
- **Query:**
  - `query`: Search term (minimum 2 characters)
  - `page`: Page number (1-based, default: 1)
  - `pageSize`: Results per page (1-50, default: 10)
- **Returns:** `SearchResultDto`
- **Features:**
  - Cross-entity search with unified results
  - Relevance-based ranking
  - Pagination support
  - Comprehensive entity coverage

### GET `/strategy`

- **Description:** Advanced search with strategy selection and fuzzy matching
- **Query:**
  - `query`: Search term (minimum 2 characters)
  - `strategy`: Search strategy (Auto, Exact, Fuzzy, Wildcard)
  - `page`: Page number (default: 1)
  - `pageSize`: Results per page (default: 10)
- **Returns:** `SearchResultDto`
- **Features:**
  - Multiple search strategy options
  - Strategy-specific optimization
  - Advanced fuzzy matching capabilities

### POST `/filtered`

- **Description:** Advanced search with comprehensive filtering capabilities
- **Body:** `SearchFiltersDto`
  - `Query`: Search term
  - `EntityTypes`: Specific entity types to search
  - `DateRange`: Date filtering
  - `CountryFilter`: Geographic filtering
  - `Page`: Page number
  - `PageSize`: Results per page
- **Returns:** `SearchResultDto`
- **Features:**
  - Advanced filtering options
  - Multi-criteria search
  - Entity-type specific filters
  - Geographic and temporal filtering

### GET `/unified`

- **Description:** Unified search across multiple entity types with ranking
- **Query:**
  - `query`: Search term (minimum 2 characters)
  - `entityTypes`: Comma-separated entity types to search
  - `page`: Page number (default: 1)
  - `pageSize`: Results per page (default: 10)
- **Returns:** `SearchResultDto`
- **Features:**
  - Selective entity type searching
  - Unified result ranking
  - Cross-entity relevance scoring

### GET `/all`

- **Description:** Advanced search with fuzzy matching across all entities
- **Query:**
  - `query`: Search term (minimum 2 characters)
  - `page`: Page number (default: 1)
  - `pageSize`: Results per page (default: 10)
  - `enableFuzzySearch`: Enable fuzzy/approximate matching
- **Returns:** `SearchResultDto`
- **Features:**
  - Optional fuzzy search capabilities
  - Approximate string matching
  - Comprehensive entity coverage

### GET `/teams`

- **Description:** Search for teams with advanced ranking and relevance scoring
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
  - `advanced`: Use advanced ranking algorithm
- **Returns:** `List<Team>`
- **Features:**
  - Team-specific search optimization
  - Advanced ranking algorithms
  - Fuzzy matching for team names

### GET `/players`

- **Description:** Search for players with advanced ranking and detailed filtering
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
  - `advanced`: Use advanced ranking algorithm
- **Returns:** `List<Player>`
- **Features:**
  - Player-specific search optimization
  - Position and team-aware ranking
  - Advanced player attribute matching

### GET `/coaches`

- **Description:** Search for coaches with advanced ranking and relevance scoring
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
  - `advanced`: Use advanced ranking algorithm
- **Returns:** `List<Coach>`
- **Features:**
  - Coach-specific search optimization
  - Experience and achievement-based ranking
  - Nationality and team-aware search

### GET `/stadiums`

- **Description:** Search for stadiums with location-based ranking
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
  - `advanced`: Use advanced ranking algorithm
- **Returns:** `List<Stadium>`
- **Features:**
  - Stadium-specific search optimization
  - Location-based relevance scoring
  - Capacity and facility-aware ranking

### GET `/seasons`

- **Description:** Search for seasons with comprehensive filtering
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
- **Returns:** `List<Season>`
- **Features:**
  - Season-specific search capabilities
  - League and time-period aware search
  - Active season prioritization

### GET `/matches`

- **Description:** Search for matches with advanced filtering
- **Query:**
  - `query`: Search term
  - `limit`: Maximum results (1-50, default: 10)
  - `enableFuzzySearch`: Enable fuzzy matching
- **Returns:** `List<Match>`
- **Features:**
  - Match-specific search optimization
  - Team and date-aware search
  - Match status and result filtering

### GET `/suggestions`

- **Description:** Get search suggestions/autocomplete with enhanced relevance scoring
- **Query:**
  - `query`: Partial search term (minimum 1 character)
  - `limit`: Maximum suggestions (1-20, default: 5)
- **Returns:** `List<SearchSuggestionDto>`
- **Features:**
  - Real-time search suggestions
  - Relevance-based suggestion ranking
  - Cross-entity suggestion support
  - Error-tolerant response handling

### GET `/analytics`

- **Description:** Get search analytics and performance metrics
- **Query:**
  - `query`: Search term to analyze
- **Returns:** `SearchAnalyticsDto`
- **Features:**
  - Search performance analysis
  - Query effectiveness metrics
  - Result distribution analytics
  - Search pattern insights

### POST `/bulk`

- **Description:** Bulk search across multiple queries
- **Body:** `List<string>` - Array of search queries (max 10)
- **Query:**
  - `pageSize`: Results per query (1-50, default: 10)
- **Returns:** `Dictionary<string, SearchResultDto>`
- **Features:**
  - Batch search processing
  - Multiple query execution
  - Error-tolerant bulk processing
  - Individual query result mapping

---

## SeasonsController (`/api/seasons`)

### GET `/`

- **Description:** Get all seasons with intelligent caching and filtering
- **Query:**
  - `leagueName` (optional): Filter by league name
  - `country` (optional): Filter by country
  - `isActive` (optional): Filter by active status
- **Returns:** `GetAllSeasonsQueryResponse`
- **Cache:** Yes (15-minute TTL with X-Cache-Hit header)
- **Features:**
  - Dynamic cache key generation based on query parameters
  - League and country filtering capabilities
  - Active season filtering
  - Performance-optimized season retrieval

### GET `/{id}`

- **Description:** Get specific season by ID with caching
- **Path:** `id` - Season ID to retrieve
- **Returns:** `GetSeasonByIdQueryResponse`
- **Cache:** Yes (15-minute TTL with X-Cache-Hit header)
- **Features:**
  - Individual season caching
  - Automatic 404 handling for non-existent seasons
  - Cache invalidation on season updates

### GET `/SeasonTeams/{id}`

- **Description:** Get all teams participating in a season with caching
- **Path:** `id` - Season ID to get teams for
- **Returns:** `GetSeasonTeamsQueryResponse`
- **Cache:** Yes (15-minute TTL with X-Cache-Hit header)
- **Features:**
  - Season-specific team data caching
  - Performance-optimized team retrieval
  - Separate cache invalidation for team associations

### POST `/`

- **Description:** Create new season
- **Auth:** Admin
- **Body:** `CreateSeasonDto` (JSON)
  - `Name`: Season name
  - `LeagueName`: Associated league name
  - `Country`: Season country
  - `StartDate`: Season start date
  - `EndDate`: Season end date
  - `IsActive`: Season active status
- **Returns:** `CreateSeasonCommandResponse`
- **Features:**
  - Admin role validation
  - Cache invalidation for season lists
  - Returns 201 Created with location header

### PUT `/{id}`

- **Description:** Update existing season with comprehensive cache management
- **Auth:** Admin
- **Path:** `id` - Season ID to update
- **Body:** `UpdateSeasonDto` (JSON)
  - All fields from CreateSeasonDto can be updated
- **Returns:** `UpdateSeasonCommandResponse`
- **Features:**
  - Existence validation before update
  - Comprehensive cache invalidation (specific season + teams + lists)
  - Admin role validation
  - Atomic update operations

### DELETE `/{id}`

- **Description:** Delete season with complete cache cleanup
- **Auth:** Admin
- **Path:** `id` - Season ID to delete
- **Returns:** `DeleteSeasonCommandResponse`
- **Features:**
  - Complete season removal
  - Comprehensive cache invalidation (season + teams + lists)
  - Admin role validation
  - Automatic 404 handling for non-existent seasons

---

## StadiumsController (`/api/stadiums`)

### GET `/`

- **Description:** Get all stadiums with intelligent caching and filtering
- **Query:**
  - `country` (optional): Filter by stadium country
  - `city` (optional): Filter by stadium city
- **Returns:** `GetAllStadiumsQueryResponse`
- **Cache:** Yes (30-minute TTL with X-Cache-Hit header)
- **Features:**
  - Extended caching duration for stable data (30 minutes)
  - Dynamic cache key generation based on query parameters
  - Geographic filtering capabilities
  - Performance-optimized stadium retrieval

### GET `/{id}`

- **Description:** Get specific stadium by ID with caching
- **Path:** `id` - Stadium ID to retrieve
- **Returns:** `GetStadiumByIdQueryResponse`
- **Cache:** Yes (30-minute TTL with X-Cache-Hit header)
- **Features:**
  - Individual stadium caching
  - Automatic 404 handling for non-existent stadiums
  - Extended cache duration for stable venue data

### POST `/`

- **Description:** Create new stadium with optional image upload
- **Auth:** Admin
- **Body:** `CreateStadiumDto` (form-data)
  - `Name`: Stadium name
  - `City`: Stadium city
  - `Country`: Stadium country
  - `Capacity`: Stadium capacity
  - `SurfaceType`: Playing surface type
  - `Address`: Stadium address
  - `Latitude`: Geographic latitude
  - `Longitude`: Geographic longitude
  - `Description`: Stadium description
  - `Facilities`: Available facilities
  - `BuiltDate`: Stadium construction date
  - `Image` (optional): Stadium image file
  - `ImageUrl` (optional): Existing image URL
- **Returns:** `CreateStadiumCommandResponse`
- **Features:**
  - Azure Blob Storage integration for image uploads
  - Comprehensive stadium data management
  - Geographic coordinate support
  - Cache invalidation for stadium lists
  - Returns 201 Created with location header

### PUT `/{id}`

- **Description:** Update existing stadium with advanced image management
- **Auth:** Admin
- **Path:** `id` - Stadium ID to update
- **Body:** `UpdateStadiumDto` (form-data)
  - All fields from CreateStadiumDto can be updated
  - `Image` (optional): New stadium image file
  - `ImageUrl` (optional): New image URL
- **Returns:** `UpdateStadiumCommandResponse`
- **Features:**
  - Automatic old image deletion from storage
  - New image upload and URL assignment
  - Existence validation before update
  - Comprehensive cache invalidation (specific stadium + lists)
  - Complete venue data management

### DELETE `/{id}`

- **Description:** Delete stadium with complete cleanup
- **Auth:** Admin
- **Path:** `id` - Stadium ID to delete
- **Returns:** `DeleteStadiumCommandResponse`
- **Features:**
  - Automatic image deletion from Azure Blob Storage
  - Existence validation before deletion
  - Complete cache invalidation (specific stadium + lists)
  - Resource cleanup and validation
  - Admin role validation

---

## TeamsController (`/api/teams`)

### GET `/`

- **Description:** Get all teams with intelligent caching
- **Returns:** `GetAllTeamsQueryResponse`
- **Cache:** Yes (10-minute TTL with X-Cache-Hit header)
- **Features:**
  - Extended caching duration (10 minutes)
  - Performance-optimized team retrieval
  - Automatic cache invalidation on data changes

### GET `/{id}`

- **Description:** Get specific team by ID with caching
- **Path:** `id` - Team ID to retrieve
- **Returns:** `GetTeamByIdQueryResponse`
- **Cache:** Yes (10-minute TTL with X-Cache-Hit header)
- **Features:**
  - Individual team caching
  - Automatic 404 handling for non-existent teams
  - Cache invalidation on team updates

### POST `/`

- **Description:** Create new team with optional logo upload
- **Auth:** Admin
- **Body:** `CreateTeamDto` (form-data)
  - `Name`: Team name
  - `ShortName`: Team short name/abbreviation
  - `City`: Team's home city
  - `StadiumId`: Associated stadium ID
  - `Image` (optional): Team logo file
  - `Logo` (optional): Existing logo URL
- **Returns:** `CreateTeamCommandResponse`
- **Features:**
  - Azure Blob Storage integration for logo uploads
  - Automatic logo URL assignment
  - Cache invalidation for team lists
  - Admin role validation
  - Returns 201 Created with location header

### PUT `/{id}`

- **Description:** Update existing team with advanced logo management
- **Auth:** Admin
- **Path:** `id` - Team ID to update
- **Body:** `UpdateTeamDto` (form-data)
  - All fields from CreateTeamDto can be updated
  - `Image` (optional): New logo file
  - `Logo` (optional): New logo URL
- **Returns:** `UpdateTeamCommandResponse`
- **Features:**
  - Automatic old logo deletion from storage
  - New logo upload and URL assignment
  - Existence validation before update
  - Comprehensive cache invalidation (specific team + lists)
  - ID mismatch protection

### DELETE `/{id}`

- **Description:** Delete team with complete cleanup
- **Auth:** Admin
- **Path:** `id` - Team ID to delete
- **Returns:** `204 No Content`
- **Features:**
  - Complete team removal
  - Cache invalidation (specific team + lists)
  - Admin role validation
  - Automatic 404 handling for non-existent teams

### GET `/Seasons/{id}`

- **Description:** Get all seasons associated with a team
- **Path:** `id` - Team ID to get seasons for
- **Returns:** `GetTeamSeasonsQueryResponse`
- **Features:**
  - Team-specific season data retrieval
  - Historical season information
  - Automatic 404 handling for non-existent teams

---

## New DTOs for Match Simulation

### SimulateMatchDto

Used in the `POST /simulateMatch/{userId}` endpoint:

```json
{
  "HomeTeamId": "integer",
  "AwayTeamId": "integer",
  "HomeTeamName": "string",
  "AwayTeamName": "string",
  "HomeTeamSeason": "SeasonData object",
  "AwayTeamSeason": "SeasonData object"
}
```

## New DTOs for Advanced Search

### SearchResultDto

Used in all search endpoints for paginated results:

```json
{
  "TotalResults": "integer",
  "CurrentPage": "integer",
  "TotalPages": "integer",
  "PageSize": "integer",
  "Items": "Array of SearchItemDto",
  "Error": "string (optional error message)"
}
```

### SearchItemDto

Individual search result item:

```json
{
  "Id": "string",
  "Type": "string (Team, Player, Match, Coach, Stadium, Season)",
  "Name": "string",
  "Description": "string",
  "ThumbnailUrl": "string (optional)",
  "Url": "string (frontend navigation URL)",
  "AdditionalData": "object (type-specific additional fields)"
}
```

### SearchFiltersDto

Used in `POST /search/filtered` for advanced filtering:

```json
{
  "Query": "string",
  "EntityTypes": "Array of strings (entity types to search)",
  "DateRange": "object (start/end dates)",
  "CountryFilter": "string",
  "Page": "integer",
  "PageSize": "integer"
}
```

### SearchSuggestionDto

Used in `GET /search/suggestions` for autocomplete:

```json
{
  "Text": "string (suggestion text)",
  "Type": "string (entity type)",
  "RelevanceScore": "double (0.0-1.0)",
  "MatchedFields": "Array of strings (fields that matched)"
}
```

### SearchAnalyticsDto

Used in `GET /search/analytics` for search performance metrics:

```json
{
  "Query": "string",
  "ExecutionTime": "number (milliseconds)",
  "TotalResults": "integer",
  "EntityBreakdown": "object (results per entity type)",
  "PerformanceMetrics": "object (various performance indicators)"
}
```

### WebhookNotificationPayload

Used in the `POST /webhookNotification/{simulationId}` endpoint:

```json
{
  "SimulationId": "string",
  "Status": "string (completed, failed, etc.)",
  "ResultUrl": "string (optional)",
  "AdditionalData": "object (varies by simulation type)"
}
```

### Performance Headers

Enhanced endpoints may include these response headers:

- `X-Cache-Hit`: Indicates if response was served from cache
- `X-Performance-Metrics`: Performance timing information
- `X-Live-Updates`: Indicates real-time update availability

---

## NotificationsController (`/api/notifications`)

### GET `/user/{userId}`

- **Description:** Get all notifications for a specific user
- **Auth:** User, Admin
- **Path:** `userId` - User ID to get notifications for
- **Returns:** `GetUserNotificationsQueryResponse`
- **Features:**
  - User-specific notification retrieval
  - Role-based access control
  - Comprehensive notification data including read status

### GET `/user/{userId}/unread-count`

- **Description:** Get count of unread notifications for a user
- **Auth:** User, Admin
- **Path:** `userId` - User ID to count unread notifications for
- **Returns:** `int` - Count of unread notifications
- **Features:**
  - Quick unread count retrieval
  - Optimized for notification badges/indicators
  - Direct repository access for performance

### POST `/mark-as-read/{notificationId}`

- **Description:** Mark a specific notification as read
- **Auth:** User, Admin
- **Path:** `notificationId` - Notification ID to mark as read
- **Returns:** `204 No Content` on success, `404 Not Found` if notification doesn't exist
- **Features:**
  - Individual notification status management
  - Existence validation before update
  - Atomic update operation

### POST `/user/{userId}/mark-all-read`

- **Description:** Mark all notifications as read for a user
- **Auth:** User, Admin
- **Path:** `userId` - User ID to mark all notifications as read
- **Returns:** `204 No Content` on success, `404 Not Found` if no notifications found
- **Features:**
  - Bulk notification status update
  - Batch processing for efficiency
  - User-specific notification management

### DELETE `/{notificationId}`

- **Description:** Delete a specific notification
- **Auth:** User, Admin
- **Path:** `notificationId` - Notification ID to delete
- **Returns:** `204 No Content` on success, `404 Not Found` if notification doesn't exist
- **Features:**
  - Individual notification deletion
  - Existence validation before deletion
  - Complete removal from system

### DELETE `/user/{userId}/all`

- **Description:** Delete all notifications for a user
- **Auth:** User, Admin
- **Path:** `userId` - User ID to delete all notifications for
- **Returns:** `204 No Content` on success, `404 Not Found` if no notifications found
- **Features:**
  - Bulk notification deletion
  - User-specific cleanup operations
  - Batch processing for performance
  - Validation to ensure notifications exist before deletion

---

## Notes

- All endpoints return a standard response object with a `Succeeded` property and may include error details.
- Endpoints requiring authentication are marked with **Auth**.
- For file uploads, use `multipart/form-data`.
- For endpoints with `{id}` in the path, replace with the actual resource ID.
- Some endpoints require specific roles (e.g., Admin, Manager).

For detailed request/response DTOs, see the backend code or ask for specific DTO documentation.

# API Documentation

This document describes all available API endpoints in the Footex project. Use this as a reference for integrating with the backend from the frontend application.

---

## AuthController (`/api/auth`)

### POST `/register`
- **Description:** Register a new user (with optional image upload)
- **Body:** `RegisterUserDto` (form-data)
- **Returns:** `RegisterUserCommandResponse`

### POST `/login`
- **Description:** Login user
- **Body:** `UserLoginDto` (JSON)
- **Returns:** `LoginUserCommandResponse` (sets refresh token cookie)

### POST `/refresh-token`
- **Description:** Refresh JWT using refresh token (from cookie)
- **Auth:** Yes
- **Returns:** `RefreshTokenCommandResponse` (sets new refresh token cookie)

### POST `/forgot-password`
- **Description:** Request password reset
- **Body:** `ForgotPasswordDto` (JSON)
- **Returns:** `ForgotPasswordCommandResponse`

### POST `/reset-password`
- **Description:** Reset password
- **Body:** `ResetPasswordDto` (JSON)
- **Query:** `email`, `token`
- **Returns:** `ResetPasswordCommandResponse`

### POST `/confirm-email`
- **Description:** Confirm email
- **Query:** `userId`, `token`
- **Returns:** `ConfirmEmailCommandResponse`

### POST `/resend-email-confirmation`
- **Description:** Resend email confirmation
- **Body:** `ResendEmailConfirmationDto` (JSON)
- **Returns:** `ResendEmailConfirmationCommandResponse`

### POST `/revoke-token`
- **Description:** Revoke refresh token (from body or cookie)
- **Auth:** Yes
- **Body:** `RevokeTokenCommand` (JSON)
- **Returns:** Success/Failure

### GET `/profile`
- **Description:** Get current user's profile
- **Auth:** Yes
- **Returns:** `GetUserProfileQueryResponse`

### GET `/profile/{id}`
- **Description:** Get user profile by ID
- **Auth:** Yes
- **Returns:** `GetUserProfileQueryResponse`

### POST `/manual-refresh`
- **Description:** Manually refresh token (for Swagger/testing)
- **Body:** `string` (refresh token)
- **Returns:** `RefreshTokenCommandResponse`

### PUT `/update`
- **Description:** Update user profile (with optional image upload)
- **Auth:** Yes
- **Body:** `UpdateUserDto` (form-data)
- **Returns:** `UpdateUserCommandResponse`

---

## CoachesController (`/api/coaches`)

### GET `/filter`
- **Description:** Get all coaches (filter by nationality, teamId)
- **Query:** `nationality`, `teamId`
- **Returns:** `GetAllCoachesQueryResponse`

### GET `/{id}`
- **Description:** Get coach by ID
- **Returns:** `GetCoachByIdQueryResponse`

### POST `/`
- **Description:** Create coach (with optional photo upload)
- **Auth:** Admin
- **Body:** `CreateCoachDto` (form-data)
- **Returns:** `CreateCoachCommandResponse`

### PUT `/{id}`
- **Description:** Update coach (with optional photo upload)
- **Auth:** Admin
- **Body:** `UpdateCoachDto` (form-data)
- **Returns:** `UpdateCoachCommandResponse`

### DELETE `/{id}`
- **Description:** Delete coach
- **Auth:** Admin
- **Returns:** `DeleteCoachCommandResponse`

---

## MatchesController (`/api/matches`)

### GET `/`
- **Description:** Get all matches (filterable)
- **Query:** `seasonId`, `teamId`, `status`, `fromDate`, `toDate`, `matchWeek`
- **Returns:** `GetAllMatchesQueryResponse`

### GET `/{id}`
- **Description:** Get match by ID
- **Returns:** `GetMatchByIdQueryResponse`

### GET `/Details/{MatchId}`
- **Description:** Get match by ID with details
- **Returns:** `GetMatchByIdWithDetailsQueryResponse`

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

---

## PlayersController (`/api/players`)

### GET `/`
- **Description:** Get all players (filterable)
- **Query:** `nationality`, `preferredFoot`, `teamId`
- **Returns:** `GetAllPlayersQueryResponse`

### GET `/{id}`
- **Description:** Get player by ID
- **Returns:** `GetPlayerByIdQueryResponse`

### POST `/`
- **Description:** Create player (with optional photo upload)
- **Auth:** Admin
- **Body:** `CreatePlayerDto` (form-data)
- **Returns:** `CreatePlayerCommandResponse`

### PUT `/{id}`
- **Description:** Update player (with optional photo upload)
- **Auth:** Admin
- **Body:** `UpdatePlayerDto` (form-data)
- **Returns:** `UpdatePlayerCommandResponse`

### DELETE `/{id}`
- **Description:** Delete player
- **Auth:** Admin
- **Returns:** `DeletePlayerCommandResponse`

---

## SearchController (`/api/search`)

### GET `/`
- **Description:** Search across teams, matches, players, coaches
- **Query:** `query` (min 2 chars), `page`, `pageSize`
- **Returns:** `SearchResultDto`

---

## SeasonsController (`/api/seasons`)

### GET `/`
- **Description:** Get all seasons (filterable)
- **Query:** `leagueName`, `country`, `isActive`
- **Returns:** `GetAllSeasonsQueryResponse`

### GET `/{id}`
- **Description:** Get season by ID
- **Returns:** `GetSeasonByIdQueryResponse`

### POST `/`
- **Description:** Create season
- **Auth:** Admin
- **Body:** `CreateSeasonDto` (JSON)
- **Returns:** `CreateSeasonCommandResponse`

### PUT `/{id}`
- **Description:** Update season
- **Auth:** Admin
- **Body:** `UpdateSeasonDto` (JSON)
- **Returns:** `UpdateSeasonCommandResponse`

### DELETE `/{id}`
- **Description:** Delete season
- **Auth:** Admin
- **Returns:** `DeleteSeasonCommandResponse`

### GET `/SeasonTeams/{id}`
- **Description:** Get teams for a season
- **Returns:** `GetSeasonTeamsQueryResponse`

---

## StadiumsController (`/api/stadiums`)

### GET `/`
- **Description:** Get all stadiums (filterable)
- **Query:** `country`, `city`
- **Returns:** `GetAllStadiumsQueryResponse`

### GET `/{id}`
- **Description:** Get stadium by ID
- **Returns:** `GetStadiumByIdQueryResponse`

### POST `/`
- **Description:** Create stadium (with optional image upload)
- **Auth:** Admin
- **Body:** `CreateStadiumDto` (form-data)
- **Returns:** `CreateStadiumCommandResponse`

### PUT `/{id}`
- **Description:** Update stadium (with optional image upload)
- **Auth:** Admin
- **Body:** `UpdateStadiumDto` (form-data)
- **Returns:** `UpdateStadiumCommandResponse`

### DELETE `/{id}`
- **Description:** Delete stadium
- **Auth:** Admin
- **Returns:** `DeleteStadiumCommandResponse`

---

## TeamsController (`/api/teams`)

### GET `/`
- **Description:** Get all teams
- **Returns:** `GetAllTeamsQueryResponse`

### GET `/{id}`
- **Description:** Get team by ID
- **Returns:** `GetTeamByIdQueryResponse`

### POST `/`
- **Description:** Create team (with optional logo upload)
- **Auth:** Admin
- **Body:** `CreateTeamDto` (form-data)
- **Returns:** `CreateTeamCommandResponse`

### PUT `/{id}`
- **Description:** Update team (with optional logo upload)
- **Auth:** Admin
- **Body:** `UpdateTeamDto` (form-data)
- **Returns:** `UpdateTeamCommandResponse`

### DELETE `/{id}`
- **Description:** Delete team
- **Auth:** Admin
- **Returns:** No content

### GET `/Seasons/{id}`
- **Description:** Get seasons for a team
- **Returns:** `GetTeamSeasonsQueryResponse`

---

## Notes
- All endpoints return a standard response object with a `Succeeded` property and may include error details.
- Endpoints requiring authentication are marked with **Auth**.
- For file uploads, use `multipart/form-data`.
- For endpoints with `{id}` in the path, replace with the actual resource ID.
- Some endpoints require specific roles (e.g., Admin, Manager).

For detailed request/response DTOs, see the backend code or ask for specific DTO documentation.


# Architecture

## High-level system

NiceClean is a two-tier system:

1. **REST API (ASP.NET Core, .NET 10)**
   - Project: `backend/dotnet/NiceCleanREST`
   - References: `backend/dotnet/NiceCleanLib`
   - Responsibilities: data access, validation rules, REST endpoints, JSON serialization, Swagger/OpenAPI.

2. **Mobile client (MAUI, .NET 10)**
   - Project: `frontend/mobile/NiceCleanApp`
   - Responsibilities: UI, maps, user flows, calling the REST API, local notifications, and GPS-based proximity logic.

## Component breakdown

### Backend REST API

- **Web host**: `Program.cs`
  - Adds controllers, OpenAPI/Swagger, JSON enum serialization.
  - Configures CORS to allow any origin/header/method.
  - Registers repositories (in-memory by default).
  - Configures EF Core with MySQL connection string (ready for DB repositories).

- **Controllers**
  - `EventController`: event lifecycle, joining/leaving, status updates, rescheduling.
  - `PinController`: CRUD for pins, proximity checks, voting, location lookup.
  - `ReportController`: event report submission and duplicate prevention.
  - `UserController`: user CRUD and login.

- **Domain library** (`NiceCleanLib`)
  - **Models**: `User`, `Pin`, `Event`, `Participation`, `Report`, `PinVote`.
  - **Enums**: pollution severity/type, pin status, event status, vote type, bag volume.
  - **Repositories**:
    - In-memory: `*Repository.cs` (default in `Program.cs`).
    - EF Core: `*RepositoryDB.cs` (MySQL backing).
  - **DbContext**: `NiceCleanDbContext` defines tables, keys, indices, relationships.

### Mobile app

- **Composition root**: `MauiProgram.cs`
  - Registers `IClient` (NSwag-generated REST client) and base API URL.
  - Registers session, credential, proximity, and notification services.
  - Registers pages for DI.

- **Pages**
  - `AuthPage`: login and registration.
  - `MapPage`: map, pin placement, pin validation, drawer menu, sign-out.
  - `EventsPage`: list and filter events; create and detail flows.

- **Controls (popups)**
  - Pins: `PinInfoPopup`, `PinReportPopup`, `PinValidationPopup`.
  - Events: `CreateEventPopup`, `EventDetailPopup`, `EventReportPopup`.

- **Services**
  - `ApiClient.cs`: NSwag-generated API client (downloaded from Swagger).
  - `PinProximityService`: polls GPS and raises events for nearby pins.
  - `PinNotificationService`: local notification integration.
  - `CredentialService`: secure storage of login credentials.
  - `UserSession`: in-memory logged-in user state.

## Data flow (typical)

### Reporting a pollution pin

1. User taps “Place Pin” in `MapPage`.
2. Map click triggers local validation (distance check + nearby pin check).
3. `PinReportPopup` collects severity and type.
4. `IClient.PinPOSTAsync` sends `PinCreateDto` to the API.
5. API returns created pin → map layer updated.

### Validating a nearby pin

1. `PinProximityService` detects proximity and raises `PinEntered`.
2. `MapPage` shows `PinValidationPopup`.
3. User submits vote via `PinController` (`/api/Pin/{id}/vote`).
4. Backend updates pin status when vote thresholds are met.

### Event lifecycle

1. User selects a verified pin without an event.
2. `CreateEventPopup` posts `EventCreateDto` to `/api/Event`.
3. Participants can join/leave via `/api/Event/{id}/join` and `/api/Event/{eventId}/remove/{userId}`.
4. Host updates status via `/api/Event/{id}/status` and can end the event.

## Storage model

- **Default**: in-memory repositories with seed data; resets on restart.
- **Optional**: MySQL with EF Core repositories (available but not currently wired in `Program.cs`).

## External dependencies

- **Map tiles**: OpenStreetMap via Mapsui.
- **Device features**: GPS, geocoding, secure storage, local notifications.
- **API client**: NSwag-generated client from Swagger endpoint.


# Deployment

## Runtime dependencies

### Backend API

- .NET 10 runtime.
- MySQL (if using EF Core repositories).
- HTTP hosting for ASP.NET Core (Kestrel, IIS, or container).

### Mobile app

- Android/iOS/Windows/MacCatalyst targets as configured in `NiceCleanApp.csproj`.
- Location services permission.
- Local notifications permission.

## Configuration

### Backend

- `appsettings.json` contains `ConnectionStrings:NiceClean` for MySQL.
- `Program.cs` currently registers **in-memory** repositories (singletons).
- To use MySQL, replace the in-memory registrations with the DB repository registrations.

### Frontend

- Base API URL is set in `MauiProgram.cs`:
  - `https://nicecleanrest.azurewebsites.net`
- For local testing, the base URL must be changed to the local API host.

## Deployment notes (backend)

- Swagger/OpenAPI is enabled unconditionally in `Program.cs`.
- CORS policy is permissive (AllowAnyOrigin/Header/Method).
- With in-memory repositories, data is ephemeral and resets on restart.
- With DB repositories, ensure MySQL migrations/DDL match `NiceCleanDbContext`.

## Deployment notes (mobile)

- The app is configured as a single-project MAUI application.
- Local notification permissions must be declared per platform:
  - Android: POST_NOTIFICATIONS and RECEIVE_BOOT_COMPLETED.
  - iOS: UIBackgroundModes = fetch.


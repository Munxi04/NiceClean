# Setup

## Prerequisites

### Backend

- .NET 10 SDK.
- (Optional) MySQL if switching to EF Core repositories.

### Mobile app

- .NET 10 SDK with MAUI workload.
- Platform toolchains as required:
  - Android SDK/NDK for Android builds.
  - Xcode for iOS/MacCatalyst builds.
  - Windows 10 SDK for Windows builds.

## Backend configuration

- `backend/dotnet/NiceCleanREST/appsettings.json` contains `ConnectionStrings:NiceClean`.
- `backend/dotnet/NiceCleanREST/Program.cs` currently registers **in-memory** repositories.
  - To use MySQL, register the `*RepositoryDB` services instead.

## Mobile configuration

- `frontend/mobile/NiceCleanApp/MauiProgram.cs` sets the base API URL.
- For local testing, update the base URL to the local API host.
- The API client is generated from Swagger (`NiceCleanApp.csproj`).

## Running locally (high-level)

- Start the backend API first so Swagger is reachable.
- Build and run the MAUI app on your target platform.
- Ensure location and notification permissions are granted on device/simulator.


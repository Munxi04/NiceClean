# NiceClean Documentation

This documentation covers the current codebase as-is in the `NiceClean` workspace. It describes architecture, deployment, structure, code organization, functionality, and the deployment-ready prototype tracked on the `proto-deploy` branch.

## Documentation map

- `docs/architecture.md` — System architecture, data flow, and component responsibilities.
- `docs/structure.md` — Folder layout and key files.
- `docs/code.md` — Code organization, generated code, and key implementation notes.
- `docs/functionality.md` — End-to-end functionality and user flows.
- `docs/deployment.md` — Runtime dependencies, configuration, and deployment notes.
- `docs/prototype.md` — Prototype release notes for the `proto-deploy` branch.
- `docs/setup.md` — Local development setup and environment prerequisites.
- `docs/api.md` — REST API reference (endpoints and payloads).
- `docs/data-model.md` — Core entities, relationships, and constraints.
- `docs/security.md` — Security posture and known gaps.
- `docs/testing.md` — Test status and validation guidance.
- `docs/operations.md` — Operational considerations and runtime behavior.

## Scope and assumptions

- Backend API: `backend/dotnet/NiceCleanREST` (ASP.NET Core Web API, .NET 10).
- Shared domain library: `backend/dotnet/NiceCleanLib` (models, enums, repositories, EF Core DbContext).
- Mobile frontend: `frontend/mobile/NiceCleanApp` (.NET MAUI, Android/iOS/Windows/MacCatalyst).
- The mobile app currently targets an Azure-hosted API base URL: `https://nicecleanrest.azurewebsites.net`.

## Known constraints (current implementation)

- The backend registers **in-memory repositories** by default. Data resets on API restart unless switched to DB repositories.
- Authentication is **not token-based**. Login uses raw email/password and returns a user object.
- Passwords are stored and compared in **plain text**.
- CORS is configured to allow **any origin/headers/methods**.
- Event cleanup reports are collected in the UI but are **not persisted** to the API yet.

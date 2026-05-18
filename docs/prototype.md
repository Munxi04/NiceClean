# Prototype (proto-deploy branch)

## Purpose

The `proto-deploy` branch is the deployment-ready prototype of NiceClean. It is intended to be used for demo and release builds with stable feature scope and configuration.

## Expected characteristics

- Mobile app points to the production API base URL.
- Feature set matches current production demo:
  - Login and registration
  - Map with pin reporting and validation
  - Event creation, join/leave, and status updates
  - Local notifications for nearby pins
- Deployment artifacts are built from `proto-deploy` to avoid changes under active development on `main`.

## Verification checklist

When preparing a release from `proto-deploy`, verify:

- The mobile base URL is correct.
- Backend repository mode is correct for the environment:
  - In-memory repositories for pure demo sessions.
  - DB repositories for persistent environments.
- Swagger endpoint is reachable and returns the latest contract.
- Permissions (location + notifications) are configured for target platforms.

## Notes about the current codebase

- The `main` branch already points to `https://nicecleanrest.azurewebsites.net`.
- Event cleanup reports are not posted to the API yet; the UI shows a local summary only.
- If persistent data is required, update `Program.cs` to register DB repositories and ensure MySQL is configured.


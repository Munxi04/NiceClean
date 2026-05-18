# Testing

## Current status

- No automated tests are present in the repository.
- Validation is currently manual via Swagger UI and the MAUI client.

## Manual verification checklist

- API starts without errors and Swagger is reachable.
- Pin creation works and prevents duplicates within 100 m.
- Pin voting updates status after reaching thresholds.
- Event creation/join/leave/status update flows are functional.
- Login and registration flows work end-to-end in the mobile app.
- Location and notification permissions are granted and operate as expected.

## Recommended additions

- Unit tests for repositories and controllers.
- Integration tests for REST endpoints and database repositories.
- UI tests for MAUI pages and popups.


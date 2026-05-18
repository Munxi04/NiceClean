# Security

## Current posture (as implemented)

- **Authentication**: email/password login only; no session tokens or JWTs.
- **Password handling**: stored and compared in plain text.
- **Transport security**: relies on HTTPS where hosted; no enforcement in code.
- **CORS**: allow-any-origin/header/method in `Program.cs`.
- **Authorization**: no role-based or user-based authorization checks beyond simple ID matching in controllers.

## Data handling

- User credentials are stored in secure storage on the device (`SecureStorage`).
- Backend returns full user objects on login, including password fields.

## Known gaps

- Missing password hashing and salting.
- No server-side authentication tokens or refresh flow.
- No rate-limiting or abuse protection on login endpoints.
- No audit logging for user or admin actions.

## Recommended improvements (future)

- Hash passwords using a modern algorithm (e.g., BCrypt/Argon2).
- Add token-based authentication (JWT or OAuth2-based flow).
- Lock down CORS to known client origins.
- Add request validation and throttling on login and write endpoints.


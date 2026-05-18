# Functionality

## Core user flows

### Authentication

- **Register**: creates a user with nickname, email, password, and date of birth.
- **Login**: uses email/password, returns the full user record.
- **Session**: stored in-memory in `UserSession`, optionally persisted credentials in `SecureStorage`.

### Map and pins

- **Map view**: OpenStreetMap tiles centered on Nice, France.
- **Pin creation**:
  - User must be near the target location (checked client-side).
  - Backend rejects creation if a pin already exists within 100 meters.
  - Pin status starts as `Verified` if the user is trusted (>= 10 walks or verified), else `Unverified`.
- **Pin voting**:
  - Users can confirm or reject a pin.
  - Net vote count $\ge 3$ verifies; $\le -3$ deletes (in backend logic).
- **Pin validation popup**:
  - Triggered when user is within 10 meters of a pin (proximity service).

### Events (clean walks)

- **Create**: host selects a verified pin without an active event and schedules a start time.
- **Join/leave**: users can join or leave events, unless already ended.
- **Status update**: host can move event from Pending → Ongoing → Ended.
- **Event detail**: shows status, location, host, participants.

### Reporting cleanup

- **Event report UI**: host can submit bag count and bag volume.
- **Persistence**: currently **not posted** to the API; UI shows a local summary only.

### Notifications

- **Local notifications**: sent when a nearby pin is detected while the app is backgrounded.

## REST API endpoints (high-level)

### Users

- `GET /api/User` — list users
- `GET /api/User/{id}` — user by id
- `POST /api/User` — create user
- `POST /api/User/login` — login
- `PUT /api/User/{id}` — update user
- `DELETE /api/User/{id}` — delete user

### Pins

- `GET /api/Pin` — list pins
- `GET /api/Pin/{id}` — pin by id
- `GET /api/Pin/atLocation` — pin lookup within radius
- `GET /api/Pin/isUserNear` — proximity check
- `GET /api/Pin/{pinId}/hasVoted/{userId}` — vote check
- `POST /api/Pin` — create pin
- `POST /api/Pin/{id}/vote` — vote
- `PUT /api/Pin/{id}` — update pin
- `DELETE /api/Pin/{id}` — delete pin

### Events

- `GET /api/Event` — list events
- `GET /api/Event/{id}` — event by id
- `GET /api/Event/{eventId}/hasJoined/{userId}` — participation check
- `POST /api/Event` — create event
- `POST /api/Event/{id}/join` — join
- `PUT /api/Event/{id}/status` — update status
- `PUT /api/Event/{id}/reschedule` — reschedule
- `DELETE /api/Event/{eventId}/remove/{userId}` — leave

### Reports

- `GET /api/Report/event/{eventId}/hasReport` — report existence
- `POST /api/Report` — submit report


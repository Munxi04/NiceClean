# API Reference

Base path: `/api`

## Users

### Create user

- `POST /api/User`
- Body: `User`
  - `Email` (string)
  - `Password` (string)
  - `Age` (DateTime)
  - `Nickname` (string)

### Login

- `POST /api/User/login`
- Body: `LoginDto`
  - `Email` (string)
  - `Password` (string)

### Read / update / delete

- `GET /api/User`
- `GET /api/User/{id}`
- `PUT /api/User/{id}` (body: `User`)
- `DELETE /api/User/{id}`

## Pins

### Create pin

- `POST /api/Pin`
- Body: `PinCreateDto`
  - `Severity` (PollutionSeverity)
  - `PollutionType` (PollutionType)
  - `Latitude` (double)
  - `Longitude` (double)
  - `LocationName` (string)
  - `UserId` (int)

### Vote on pin

- `POST /api/Pin/{id}/vote`
- Body: `PinVoteDto`
  - `UserId` (int)
  - `VoteType` (VoteType: Confirmed/Rejected)

### Read / update / delete

- `GET /api/Pin`
- `GET /api/Pin/{id}`
- `PUT /api/Pin/{id}` (body: `PinUpdateDto`)
- `DELETE /api/Pin/{id}`

### Proximity helpers

- `GET /api/Pin/atLocation?latitude={lat}&longitude={lon}`
- `GET /api/Pin/isUserNear?userLat={lat}&userLon={lon}&targetLat={lat}&targetLon={lon}`
- `GET /api/Pin/{pinId}/hasVoted/{userId}`

## Events

### Create event

- `POST /api/Event`
- Body: `EventCreateDto`
  - `PinId` (int)
  - `HostUserId` (int)
  - `HostNickname` (string)
  - `StartTime` (DateTime)

### Join / leave

- `POST /api/Event/{id}/join` (body: `ParticipationDto`)
- `DELETE /api/Event/{eventId}/remove/{userId}`
- `GET /api/Event/{eventId}/hasJoined/{userId}`

### Status / reschedule

- `PUT /api/Event/{id}/status?status={EventStatus}&hostUserId={id}`
- `PUT /api/Event/{id}/reschedule?newDate={DateTime}&hostUserId={id}`

### Read

- `GET /api/Event`
- `GET /api/Event/{id}`

## Reports

### Submit report

- `POST /api/Report`
- Body: `ReportCreateDto`
  - `EventId` (int)
  - `NumberOfBags` (int)
  - `BagVolume` (BagVolume)

### Check report

- `GET /api/Report/event/{eventId}/hasReport`


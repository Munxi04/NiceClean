# Data Model

## Entities

### User

- `Id` (int, PK)
- `Email` (string, unique)
- `Password` (string)
- `Age` (DateTime)
- `Nickname` (string)
- `NumberOfWalks` (int, default 0)
- `IsVerified` (bool, default false)

### Pin

- `Id` (int, PK)
- `UserId` (int, FK → User.Id)
- `CreationDate` (DateTime, default current timestamp)
- `Severity` (enum, stored as string)
- `Radius` (double, default 100.0)
- `Status` (enum, stored as string, default Unverified)
- `PollutionType` (enum, stored as string)
- `Latitude` (double)
- `Longitude` (double)
- `LocationName` (string)
- `HasEvent` (bool)
- `EventId` (int)

### PinVote

- `Id` (int, PK)
- `PinId` (int, FK → Pin.Id)
- `UserId` (int, FK → User.Id)
- `VoteType` (enum, stored as string)
- `CreatedAt` (DateTime, default current timestamp)
- Unique index: `(PinId, UserId)`

### Event

- `EventId` (int, PK)
- `Date` (DateTime)
- `EventStatus` (enum, stored as string)
- `HostUserId` (int)
- `Nickname` (string)
- `PinId` (int)
- `ParticipationCount` (int)

### Participation

- `ParticipationId` (int, PK)
- `EventId` (int, FK → Event.EventId)
- `UserId` (int, FK → User.Id)
- `IsParticipating` (bool)
- `JoinDate` (DateTime, default current timestamp)
- Unique index: `(EventId, UserId)`

### Report

- `ReportId` (int, PK)
- `EventId` (int, FK → Event.EventId)
- `NumberOfBags` (int)
- `BagVolume` (enum, stored as string)
- `CreatedAt` (DateTime)
- Unique index: `EventId` (one report per event)

## Relationships

- **User → Pins**: one-to-many (user can create many pins).
- **User → PinVotes**: one-to-many (user can vote on many pins).
- **User → Participations**: one-to-many (user can join many events).
- **Pin → PinVotes**: one-to-many.
- **Event → Participations**: one-to-many.
- **Event → Report**: one-to-one (enforced by unique index on `EventId`).


# Operations

## Runtime behavior

- Backend runs as an ASP.NET Core API with Swagger enabled.
- Repositories are in-memory by default; data resets on restart.
- Optional MySQL persistence is available via EF Core repositories.

## Logging

- Default ASP.NET Core logging configured in `appsettings.json`.
- No structured logging or external telemetry configured.

## Monitoring

- No built-in health checks or metrics endpoints.
- Swagger can be used for basic availability testing.

## Scaling considerations

- In-memory repositories are single-instance only.
- Horizontal scaling requires persistent storage (MySQL) and stateless services.

## Backup and recovery

- With MySQL, standard database backups apply.
- With in-memory repositories, no persistence or recovery is possible.


# AGENTS.md

.NET 10 Clean Architecture auth API (register / login / JWT access + rotating refresh tokens / token blacklist). PostgreSQL via Npgsql. Team writes code identifiers in English but user-facing messages in Spanish.

## Layout

- `src/ApiPoo2.Domain` — entities, value objects, domain events/exceptions. No project refs.
- `src/ApiPoo2.Application` — custom CQRS handlers, validators, exceptions, `IRepositories`/`IServices`. Depends only on Domain.
- `src/ApiPoo2.Infrastructure` — EF Core/Npgsql, repositories, JWT/password services, `.env` loader, migrations. Depends on Domain + Application.
- `src/ApiPoo2.WebApi` — controllers + exception middleware, composition root, `public partial class Program`.
- `tests/` — UnitTests, IntegrationTests, ArchitectureTests. ArchitectureTests enforce layer refs and "domain entities expose no public setters" — preserve that invariant.

## Commands

- Build: `dotnet build ApiPoo2.sln`
- Run API: `dotnet run --project src/ApiPoo2.WebApi` (http `:5114`, https `:7062`)
- All tests: `dotnet test ApiPoo2.sln`
- Single test: `dotnet test tests/ApiPoo2.UnitTests --filter "FullyQualifiedName~UserTests"`
- New migration: `dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure` (design-time factory in Infrastructure reads root `.env`; no startup project needed)

## Config & secrets

- All config comes from the repo-root `.env`, loaded by `EnvFileLoader.LoadFromRepositoryRoot()` (walks up from `AppContext.BaseDirectory`). `appsettings.json` intentionally has no connection string or JWT config — don't add them there.
- Keys: `DATABASE_URL` (PostgreSQL; currently a live Neon cloud DB) and `Jwt__Secret` (min 32 chars, enforced at startup), `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenTtlMinutes`, `Jwt__RefreshTokenTtlDays`. `__` maps to config section separators.
- `.env` holds live credentials and is gitignored (along with `bin/`/`obj/`). Never commit it or echo its values.

## Conventions & gotchas

- Custom CQRS, **not MediatR**. Send `ICommand<T>`/`IQuery<T>` records through `IDispatcher.SendAsync`/`QueryAsync`; handlers implement `ICommandHandler<,>`/`IQueryHandler<,>`. `AddApplication()` reflection-scans the Application assembly for handlers and FluentValidation validators — pass the assembly to `AddApplication(assembly)` if handlers live elsewhere.
- Pipeline order: `ValidationBehavior` then `LoggingBehavior`.
- Errors: throw typed exceptions from Application/Domain (`BaseApplicationException` subclasses, `DomainValidationException`) carrying a stable machine-readable `code` and a Spanish message. `ExceptionHandlingMiddleware` maps them to `application/problem+json` `{status, code, message, errors, traceId}`. Integration tests assert these codes (`email.conflict`, `password.policy`, `refresh.reuse`, …) — keep messages Spanish and codes stable.
- Domain entities have no public setters; mutate only via domain methods/factories (e.g. `User.Register`). Password rules live in `Domain/Common/PasswordPolicy.EnsureValid`; `Email`/`PasswordHash` are value objects.
- Controllers receive only `IDispatcher` + request contracts; keep application/domain logic out of WebApi.
- EF configs (`Infrastructure/Persistencia/Configurations`) use snake_case tables/columns (`users`, `refresh_tokens`, `blacklisted_tokens`); `PasswordHash` is an owned type and `Email` uses a converter.

## Testing gotchas

- IntegrationTests use `WebApplicationFactory<Program>`; `ApiFactory.EnsureSchemaAsync()` runs `Database.MigrateAsync()` against the **real** `DATABASE_URL` in `.env`. They are not isolated — they hit the live cloud DB and need it reachable. Each test uses a unique email (keep that pattern).
- Unit + Architecture tests are fast and offline. Integration tests mutate the shared database, so prefer `--filter` when iterating.

# AGENTS.md

.NET 10 Clean Architecture auth API (register / login / JWT access + rotating refresh tokens / token blacklist). PostgreSQL via Npgsql. Team writes code identifiers in English but user-facing messages in Spanish.

## Layout

- `src/ApiPoo2.Domain` — entities, value objects, domain events/exceptions. No project refs.
- `src/ApiPoo2.Application` — use cases grouped by feature (`UseCases/<Feature>/<Caso>/`), validators, exceptions, DTOs (`DTOs/`), `IRepositories`/`IServices`. Depends only on Domain.
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

- Use cases per feature, **no CQRS, no MediatR, no Dispatcher**. Each use case is a concrete class in `UseCases/<Feature>/<Caso>/` that inherits `BaseUseCase<TRequest, TResult>` from `UseCases/BaseUseCase.cs` (abstract `ExecuteCoreAsync`). `BaseUseCase` applies FluentValidation validators plus logs "Iniciando/Finalizado" around execution. `AddApplication()` registers every use case and validator **explicitly** with `AddScoped` (no reflection) — add a line when you create a use case/validator.
- Use-case requests are records named `<Caso>Request` (e.g. `RegisterRequest`) living next to the use case; controllers bind them directly as `[FromBody]` and inject the concrete use cases by constructor (one per endpoint). Keep application/domain logic out of WebApi.
- Errors: throw typed exceptions from Application/Domain (`BaseApplicationException` subclasses, `DomainValidationException`) carrying a stable machine-readable `code` and a Spanish message. `ExceptionHandlingMiddleware` maps them to `application/problem+json` `{status, code, message, errors, traceId}`. Integration tests assert these codes (`email.conflict`, `password.policy`, `refresh.reuse`, …) — keep messages Spanish and codes stable.
- Domain entities have no public setters; mutate only via domain methods/factories (e.g. `User.Register`). Password rules live in `Domain/Common/PasswordPolicy.EnsureValid`; `Email`/`PasswordHash` are value objects.
- **DTO rules**: DTOs live only in `Application/DTOs/` and are created only by use cases (repos return domain entities, never DTOs; Domain/Infrastructure never reference DTOs). Name DTOs `<Accion><Entidad>Dto` — the producing action plus the domain concept, never the bare entity name (e.g. `RegisterUserDto`, `CurrentUserDto`, `LoginTokenPairDto`, `RefreshTokenPairDto`; avoid `UserDto`). `OperationResult` is an exception (generic result type, not entity-shaped).
- EF configs (`Infrastructure/Persistencia/Configurations`) use snake_case tables/columns (`users`, `refresh_tokens`, `blacklisted_tokens`); `PasswordHash` is an owned type and `Email` uses a converter.
- **Concurrency & invariants**: `refresh_tokens` carries octal concurrency via the Postgres `xmin` row version (`RefreshTokenConfiguration`). `UnitOfWork.SaveChangesAsync` maps `DbUpdateConcurrencyException` → `UnauthorizedException("refresh.reuse", …)` and Postgres unique violations on `users` → `ConflictException("email.conflict", …)` (`Infrastructure/Persistencia/Exceptions/PersistenceErrors.cs`). Don't rename the `users` email index (`IX_users_email` mapping depends on it). Guided by domain limits: `RefreshToken.MaxLifetime` is 7 days and `Jwt__RefreshTokenTtlDays` is capped at that at startup.
- Registration validates password policy **before** checking duplicate email. `ChangePassword` invalidates all sessions: it blacklists the current access token and revokes every refresh token (`RevocationReason.PasswordChanged`) — the input carries `AccessTokenJti`/`AccessTokenExpiresAtUtc` fabricated by the controller from claims. `RevokeRefreshToken` returns 404 (`user.not_found`) for unknown users.

## Testing gotchas

- IntegrationTests use `WebApplicationFactory<Program>`; `ApiFactory.EnsureSchemaAsync()` runs `Database.MigrateAsync()` against the **real** `DATABASE_URL` in `.env`. They are not isolated — they hit the live cloud DB and need it reachable. Each test uses a unique email (keep that pattern).
- Unit + Architecture tests are fast and offline. Integration tests mutate the shared database, so prefer `--filter` when iterating.

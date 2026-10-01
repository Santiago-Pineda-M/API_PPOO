# AGENTS.md

.NET 10 Clean Architecture auth API (register / login / JWT access + rotating refresh tokens / token blacklist). PostgreSQL via Npgsql. Team writes code identifiers in English but user-facing messages in Spanish.

> Manual completo de arquitectura, responsabilidades por capa, estructura de directorios y convenciones: **`ARCHITECTURE.md`**.

## Layout

- `src/ApiPoo2.Domain` — feature-sliced: `Users/`, `Personas/`, `Vehiculos/`, `TiposDocumento/`, `RefreshTokens/`, `BlacklistedTokens/`, `Common/`. Entities, value objects, domain exceptions. No project refs, no events (removed as dead code).
- `src/ApiPoo2.Application` — use cases grouped by feature (`UseCases/<Feature>/<Caso>/`) with DTOs **co-located** next to their use case, validators, `Exceptions/`, `IRepositories/`, `IServices/` (JWT + password contracts, `JwtOptions`), `Common/` (`IDateTimeProvider`, `PagedFilter`/`PagedResult`), `UseCases/IUseCase.cs` and `GlobalUsings.cs`. Depends only on Domain.
- `src/ApiPoo2.Infrastructure` — `Persistence/` (EF Core/Npgsql, repositories, migrations), `Security/` (JWT/password/token cleanup), `Common/` (`DateTimeProvider`), `.env` loader. Depends on Domain + Application.
- `src/ApiPoo2.WebApi` — controllers + exception middleware, composition root, `public partial class Program`.
- `tests/` — UnitTests, ApplicationTests, IntegrationTests, ArchitectureTests. ArchitectureTests enforce layer refs plus the rich-model invariants (entities `sealed`, no public constructors, no public setters, built only through validating factories) — preserve those invariants.

## Commands

- Build: `dotnet build ApiPoo2.sln`
- Run API: `dotnet run --project src/ApiPoo2.WebApi` (http `:5114`, https `:7062`)
- All tests: `dotnet test ApiPoo2.sln`
- Single test: `dotnet test tests/ApiPoo2.UnitTests --filter "FullyQualifiedName~UserTests"`
- Integration tests use local Docker PostgreSQL only: start `api-poo2-test-db` on `localhost:5433` before running them (for example `podman run ...` or `docker compose up -d postgres-test`). Never point integration tests at Neon.
- `dotnet ef` is a global tool; run it with `$HOME/.dotnet/tools` on `PATH` (add `--project src/ApiPoo2.Infrastructure`; a startup project is not needed)
- New migration: `dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure` (design-time factory in Infrastructure reads root `.env`; no startup project needed)

## Config & secrets

- All config comes from the repo-root `.env`, loaded by `EnvFileLoader.LoadFromRepositoryRoot()` (walks up from `AppContext.BaseDirectory`). Integration tests instead load repo-root `.env.test` through `EnvFileLoader.LoadTestEnvironment()` and force those process values, so tests always use the local Docker database. `appsettings.json` intentionally has no connection string or JWT config — don't add them there.
- Keys: `DATABASE_URL` (production PostgreSQL on Neon; integration `DATABASE_URL` points to local Docker `api_poo2_test` on `localhost:5433`) and `Jwt__Secret` (min 32 chars, enforced at startup), `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenTtlMinutes`, `Jwt__RefreshTokenTtlDays`. `__` maps to config section separators.
- `.env` and `.env.test` hold live/test credentials and are gitignored (along with `bin/`/`obj/`). Never commit them or echo their values.

## Conventions & gotchas

- Use cases per feature, **no CQRS, no MediatR, no Dispatcher**. Each use case is a concrete class in `UseCases/<Feature>/<Caso>/` that inherits `BaseUseCase<TRequest, TResult>` from `UseCases/BaseUseCase.cs` (abstract `ExecuteCoreAsync`). `BaseUseCase` applies FluentValidation validators plus logs "Iniciando/Finalizado" around execution. `AddApplication()` registers every use case and validator **explicitly** with `AddScoped` (no reflection) — add a line when you create a use case/validator.
- Use-case requests are records named `<Caso>Request` (e.g. `RegisterRequest`) living next to the use case; controllers bind them directly as `[FromBody]` and inject the concrete use cases by constructor (one per endpoint). Keep application/domain logic out of WebApi.
- Errors: throw typed exceptions from Application/Domain (`BaseApplicationException` subclasses, `DomainValidationException`) carrying a stable machine-readable `code` and a Spanish message. `ExceptionHandlingMiddleware` maps them to `application/problem+json` `{status, code, message, errors, traceId}`. Integration tests assert these codes (`persona.identificacion.conflict`, `password.policy`, `refresh.reuse`, …) — keep messages Spanish and codes stable.
- Domain entities have no public setters; mutate only via domain methods/factories (e.g. `Persona.CrearUsuario`). Password rules live in `Domain/Common/PasswordPolicy.EnsureValid`; `Login`/`PasswordHash` are value objects.
- **DTO rules**: a DTO lives in the folder of the use case that produces it (e.g. `UseCases/Personas/Create/CreatePersonaOutputDto.cs`); there is no shared `DTOs/` folder. DTOs are created only by use cases (repos return domain entities, never DTOs; Domain/Infrastructure never reference DTOs). Inputs are `<Caso>InputDto`; outputs are `<Accion><Entidad>OutputDto` — the producing action plus the domain concept, never the bare entity name (e.g. `CreatePersonaOutputDto`, `VehiculoConsultaOutputDto`, `LoginTokenPairOutputDto`; avoid `UserDto`). `OperationResult` is an exception (generic result type, not entity-shaped).
- **Entity construction**: entities are `sealed` with a private parameterless constructor (EF materialization only) plus a validating static factory (`Persona.Register`, `User.Crear`, `Vehiculo.Register`, `RefreshToken.Issue`, `BlacklistedToken.Create`, `Login.From`, `PasswordHash.Create`). Base identity (`Id`, `CreatedAtUtc`, `UpdatedAtUtc`) is assigned only through `BaseEntity.Initialize`, so there is no public constructor path. Documented composite-key exceptions are `User` (`id_persona + login`) and `DocumentoVehiculo` (`id_vehiculo + id_tipo_documento`). `RichDomainModelTests` enforces this.
- **Where rules live**: state, invariants and transitions belong to the entity (`User.EvaluateAuthentication`, `RefreshToken.WasRotated`/`RotateTo`/`Revoke`, `User.ChangePassword`). Lookups, decisions, orchestration and persistence belong to the use case — e.g. `RefreshTokenUseCase` detects reuse, calls the domain and persists. Never add a use case method that only wraps a getter.
- **Known gap (intentional)**: `PasswordPolicy.EnsureValid` is a static Domain service, not an entity invariant, because `User` receives the *hash* rather than the secret. Register/change-password use cases must call it before hashing. `PasswordPolicyTests` (ApplicationTests) pins the behavior; the invariant is not enforced by the entity.
- EF configs (`Infrastructure/Persistence/Configurations`) use snake_case tables/columns: `personas`, `usuarios`, `vehiculos`, `tipos_documento`, `documentos_vehiculo`, `conductores_vehiculos`, `refresh_tokens`, and `access_token_blacklist` (the entity is `BlacklistedToken` but the table is `access_token_blacklist`); enum codes and value objects use converters.
- **Concurrency & invariants**: `refresh_tokens` carries optimistic concurrency via the Postgres `xmin` row version (`RefreshTokenConfiguration`). `UnitOfWork.SaveChangesAsync` maps `DbUpdateConcurrencyException` → `UnauthorizedException("refresh.reuse", …)` and Postgres unique violations on `personas`/`vehiculos` → `ConflictException("persona.identificacion.conflict", …)` or `ConflictException("vehiculo.placa.conflict", …)` (`Infrastructure/Persistence/Exceptions/PersistenceErrors.cs`). Guided by domain limits: `RefreshToken.MaxLifetime` is 7 days and `Jwt__RefreshTokenTtlDays` is capped at that at startup.
- Creating an administrative `Persona` validates identification/email and then generates its one associated user: mnemonic `Login`, auto-generated password, and auto-generated `ApiKey`. `ChangePassword` invalidates all sessions: it blacklists the current access token and revokes every refresh token (`RevocationReason.PasswordChanged`) — the input carries `AccessTokenJti`/`AccessTokenExpiresAtUtc` fabricated by the controller from claims. `RevokeRefreshToken` returns 404 (`user.not_found`) for unknown users.

## Testing gotchas

- IntegrationTests use `WebApplicationFactory<Program>` and need the local Docker `DATABASE_URL` from `.env.test` to be reachable on `localhost:5433`; they never use Neon. `ApiFactory` **is** isolated: it appends a per-run `Search Path` schema to the connection string, creates it, replays every migration into it, seeds one administrative persona for bootstrapping protected endpoints, and drops it with `CASCADE` on dispose — so runs no longer mutate `public` tables or leave residue. The isolation depends on `NpgsqlConnectionStringBuilder.SearchPath`; keep it if you touch `ApiFactory`.
- ApplicationTests cover `PasswordPolicy` and pagination offline (no DB).
- Unit, Application and Architecture tests are fast and offline. Integration tests use the local Docker database and replay all migrations per run, so they remain the slower suite; prefer `--filter` when iterating.

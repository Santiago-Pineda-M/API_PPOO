# Manual de Arquitectura — ApiPoo2

API .NET 10 de autenticación (registro / login / JWT de acceso + refresh tokens rotativos / blacklist de tokens).
PostgreSQL vía Npgsql y EF Core. Arquitectura limpia en 4 capas con dependencias unidireccionales.

> **Idioma:** los identificadores de código se escriben en inglés; los mensajes hacia el usuario final siempre en español.
> Este manual es la fuente de verdad de la arquitectura. Las reglas estructurales se verifican en `tests/ApiPoo2.ArchitectureTests`.

---

## 1. Principios

1. **Dependencias unidireccionales hacia adentro**: `Domain ← Application ← Infrastructure ← WebApi`. Cada capa solo puede referenciar a las capas interiores.
2. **DI nativa**: solo `Microsoft.Extensions.DependencyInjection`. Prohibidos Autofac, Scrutor, MediatR, CQRS/dispatchers o contenedores de terceros.
3. **Sin reflection para registro**: cada tipo se registra explícitamente con `AddScoped`/`AddSingleton`.
4. **Setters privados**: las entidades de dominio no exponen setters públicos; mutan solo por métodos/factorías de dominio.
5. **Los DTOs no cruzan capas interiores**: repositorios devuelven entidades de dominio; los DTOs los crean únicamente los use cases.
6. **Invariantes en el dominio**: reglas de negocio (policy de password, límites de sesión, TTL) viven en Domain y son la única fuente de verdad.
7. **Errores tipados**: excepciones con `code` estable (machine-readable) y mensaje en español; el middleware las convierte a `application/problem+json`.

---

## 2. Reglas de dependencias (qué puede referenciar qué)

| Capa | Puede referenciar | NO puede referenciar | Refleja |
|---|---|---|---|
| **ApiPoo2.Domain** | Nada (solo BCL) | Application, Infrastructure, WebApi, EF, paquetes de terceros | `ApiPoo2.Domain.csproj` |
| **ApiPoo2.Application** | Domain | Infrastructure, WebApi, EF, ASP.NET | `ApiPoo2.Application.csproj` |
| **ApiPoo2.Infrastructure** | Domain, Application | WebApi, controladores, DTOs de salida | `ApiPoo2.Infrastructure.csproj` |
| **ApiPoo2.WebApi** | Application (y transitivamente Domain/Infrastructure vía el root) | — | `ApiPoo2.WebApi.csproj` |

Reglas adicionales y su enforcement (ArchitectureTests/LayerDependencyTests):

- **Domain no referencia ningún proyecto** y sus entidades **no exponen setters públicos** (invariante preservado en los tests).
- **Application no referencia EF/ASP.NET**: sin `Microsoft.EntityFrameworkCore`, sin `Microsoft.AspNetCore.*`. Solo usa interfaces de Application para persistencia (repositorios) y servicios.
- **Infrastructure nunca referencia DTOs** (`Application/DTOs`): solo entidades de dominio e interfaces.
- **Domain/Infrastructure no crean DTOs**; los DTOs los produce el use case en Application.
- Los tests (UnitTests/IntegrationTests/ArchitectureTests) pueden referenciar cualquier capa.

---

## 3. Responsabilidades, reglas y limitaciones por capa

### 3.1 ApiPoo2.Domain (sin dependencias)

**Responsabilidades**

- Entidades del agregado: `User`, `RefreshToken`, `BlacklistedToken`.
- Value Objects: `Email`, `PasswordHash`.
- Enums: `UserRole`, `RevocationReason`.
- Eventos de dominio: `UserRegisteredEvent`, `PasswordChangedEvent`, `UserLockedOutEvent`, `RefreshTokenRevokedEvent`.
- Reglas de negocio e invariantes (aquí vive la única fuente de verdad):
  - `PasswordPolicy.EnsureValid(password)` — política de contraseñas.
  - `User.MaxFailedAccessAttempts = 5`, `User.DefaultLockoutDuration = 15 min`.
  - `User.MaxActiveRefreshTokens = 10` (al emitir el número 11, se revoca el más antiguo con `SessionLimitReached`).
  - `RefreshToken.MaxLifetime = 7 días` (`RefreshToken.Issue` rechaza TTL mayores).
- Excepciones de dominio: `DomainException`, `DomainValidationException` (con `code` + errores).

**Reglas / limitaciones**

- **Cero dependencias** de proyecto y cero librerías externas.
- Entidades con setters privados; **mutación solo por métodos/factorías de dominio** (ej. `User.Register`, `User.ChangePassword`, `User.RevokeAllRefreshTokens`, `RefreshToken.Issue`, `RefreshToken.RotateTo`).
- Ids tipo `Guid`, timestamp UTC; `MarkUpdated(utcNow)` en toda mutación.
- No se registran eventos en constructores; se registran dentro de los métodos que mutan.

### 3.2 ApiPoo2.Application (depende solo de Domain)

**Responsabilidades**

- **Use cases** por feature en `UseCases/<Feature>/<Caso>/`, heredando `BaseUseCase<TRequest, TResult>` (valida con FluentValidation y loguea inicio/fin). **NO CQRS, NO MediatR, NO Dispatcher.**
- **Validators** FluentValidation (uno por use case que lo requiera, junto al use case).
- **DTOs** de entrada/salida en `DTOs/` — ver convenciones en §6.
- **Excepciones de aplicación**: `BaseApplicationException` y subclases (`UnauthorizedException`, `NotFoundException`, `ConflictException`, `ForbiddenException`, `RequestValidationException`) con `code` estable.
- **Puertos (interfaces)**: `IRepositories/` (`IRepository<T>`, `IUserRepository`, `IRefreshTokenRepository`, `IBlacklistedTokenRepository`, `IUnitOfWork`) e `IServices/` (`IPasswordHasher`, `IJwtTokenService`, `IJwtTokenBlacklistService`, `IDateTimeProvider`).
- `DependencyInjection.AddApplication()` registra **explícitamente** use cases y validators (`AddScoped`), sin reflection.

**Reglas / limitaciones**

- Prohibido: CQRS, MediatR, cualquier Dispatcher, carpetas `Pipeline`/`Behaviors`.
- **Los repositorios devuelven entidades de dominio, nunca DTOs.**
- La lógica de negocios vive en Domain; el use case **orquesta** (repositorios, servicios, validators) y produce DTOs.
- No puede referenciar EF ni ASP.NET.
- Cada use case inyecta los servicios que necesita por constructor (nada de service locator).
- Los `InputDto` que vienen del request **no deben contener claims derivables** (UserId, jti, expiración): el controller los fabrica desde el token.

### 3.3 ApiPoo2.Infrastructure (depende de Domain + Application)

**Responsabilidades**

- **Persistencia** EF Core/Npgsql: `AppDbContext`, `Configurations/` (mapeos snake_case), `Repositories/` (implementaciones), `Migrations/`.
- **Mapeo de errores de persistencia**: `Persistencia/Exceptions/PersistenceErrors.cs` traduce violaciones de esquema a excepciones de aplicación (ver §7).
- **Servicios**: `PasswordHasher` (BCrypt), `JwtTokenService` (emisión/validación de JWT + hash de refresh), `JwtTokenBlacklistService` (cache en memoria + tabla `access_token_blacklist`), `DateTimeProvider`.
- **Background service**: `TokenCleanupBackgroundService` (purga de blacklist expirada).
- **Carga de configuración**: `EnvFileLoader` (lee `.env` de la raíz del repo) y `Options/JwtOptions`.
- `DependencyInjection.AddInfrastructure()` arma `AddPersistence` + `AddServices` + `AddAuth`.

**Reglas / limitaciones**

- **No referencia DTOs de Application** (no crea DTOs; los repositorios devuelven entidades).
- La conversión de excepciones EF a excepciones de aplicación ocurre aquí, porque ES la única capa que conoce EF (§7).
- Guardas de configuración al arrancar: secret ≥ 32 chars, `AccessTokenTtlMinutes > 0`, `RefreshTokenTtlDays` entre 1 y `RefreshToken.MaxLifetime.Days` (7) — falla rápido con `InvalidOperationException` en español.
- Nunca commitear `.env`; no volcar valores de secretos.
- EF configs usan snake_case; `PasswordHash` owned type; `Email` con converter.

### 3.4 ApiPoo2.WebApi (composición root)

**Responsabilidades**

- **Composición root**: `Program.cs` (`public partial class Program`), registro de ambas capas inferiores y middleware.
- **Controllers**: endpoints REST. Inyectan use cases concretos por constructor (uno por endpoint) y bindean los `InputDto` de Application como `[FromBody]`.
- **Extensiones de claims**: `GetUserId()`, `GetTokenJti()`, `GetTokenExpiresAtUtc()` fabrican campos derivados del access token (nunca vienen del body del cliente).
- **Middleware de errores**: `ExceptionHandlingMiddleware` mapea excepciones tipadas a `application/problem+json` `{status, code, message, errors, traceId}`.
- Configuración Swagger.

**Reglas / limitaciones**

- **Sin lógica de aplicación/dominio en el controller**: solo orquestar HTTP → use case.
- Los controllers NO definen DTOs propios (se eliminó `Contracts/`); reutilizan `Application/DTOs`.
- Los campos derivados del token se construyen en el controller y se pasan en el `InputDto`.

---

## 4. Estructura de directorios

```
ApiPoo2.sln
├── AGENTS.md
├── .env                      (ignorado; credenciales live)
├── src/
│   ├── ApiPoo2.Domain/
│   │   ├── Common/           BaseEntity, IDomainEvent, IHasDomainEvents, IAggregateRoot, PasswordPolicy
│   │   ├── Entities/         User, RefreshToken, BlacklistedToken
│   │   ├── ValueObjects/     Email, PasswordHash
│   │   ├── Enums/            UserRole, RevocationReason
│   │   ├── Events/           DomainEvents…, PasswordChangedEvent, RefreshTokenRevokedEvent…
│   │   └── Exceptions/       DomainException, DomainValidationException
│   ├── ApiPoo2.Application/
│   │   ├── DTOs/             <Caso>InputDto, <Accion><Entidad>Dto, OperationResult
│   │   ├── Exceptions/       BaseApplicationException y subclases
│   │   ├── IRepositories/    IRepository<T>, I<Entidad>Repository, IUnitOfWork
│   │   ├── IServices/        IPasswordHasher, IJwtTokenService, IJwtTokenBlacklistService, IDateTimeProvider
│   │   ├── UseCases/
│   │   │   ├── BaseUseCase.cs
│   │   │   └── <Feature>/<Caso>/   <Caso>UseCase.cs, <Caso>Validator.cs
│   │   └── DependencyInjection.cs
│   ├── ApiPoo2.Infrastructure/
│   │   ├── Options/          JwtOptions
│   │   ├── Persistencia/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/      <Entidad>Configuration.cs, EmailConverter.cs
│   │   │   ├── Exceptions/          PersistenceErrors.cs
│   │   │   ├── Migrations/          <timestamp>_<Name>.cs
│   │   │   ├── Repositories/        GenericRepository.cs, <Entidad>Repository.cs, UnitOfWork.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   └── EnvFileLoader.cs
│   │   ├── Services/         PasswordHasher, JwtTokenService, JwtTokenBlacklistService, DateTimeProvider, TokenCleanupBackgroundService
│   │   └── DependencyInjection.cs
│   └── ApiPoo2.WebApi/
│       ├── Controllers/      AuthController, UsersController
│       ├── Extensions/       ClaimsPrincipalExtensions, SwaggerExtensions
│       ├── Middleware/       ExceptionHandlingMiddleware
│       └── Program.cs
└── tests/
    ├── ApiPoo2.UnitTests/          Domain/, Persistence/
    ├── ApiPoo2.ArchitectureTests/  LayerDependencyTests
    └── ApiPoo2.IntegrationTests/   ApiFactory, ApiCollection, AuthApiTests
```

---

## 5. Convenciones de forma

| Tema | Regla |
|---|---|
| Identificadores | English (clases, métodos, variables, DB) |
| Mensajes al usuario | Español (excepciones y validadores) |
| Clases/records/enums/interfaces | `PascalCase`; interfaces con prefijo `I` |
| Métodos/funciones | `PascalCase` |
| Variables/parámetros | `camelCase` |
| Constantes estáticas públicas | `PascalCase` (ej. `MaxActiveRefreshTokens`) |
| Constantes privadas | `camelCase` (ej. `cacheKeyPrefix`) |
| Namespaces | `ApiPoo2.<Capa>.<Subcarpeta>` (ej. `ApiPoo2.Application.UseCases.Auth.Login`) |
| Archivos | uno por tipo, nombrado igual que el tipo |
| Tipos sellados | use cases, validators, configs, repositorios concretos: `sealed` |
| Entidades de dominio | sin setters públicos; mutación por métodos de dominio (nombres en verbo, ej. `ChangePassword`, `Revoke`, `RotateTo`) |
| Sin comentarios | no agregar comentarios salvo que se pidan (solo los `///` xml esperados o avisos de migración) |

### Convenciones de nombres por artefacto

| Artefacto | Patrón | Ejemplo |
|---|---|---|
| Use case | `<Caso>UseCase` | `LoginUseCase`, `RefreshTokenUseCase` |
| Validator | `<Caso>Validator` (junto al use case) | `RegisterValidator`, `RefreshTokenValidator` |
| DTO de entrada | `<Caso>InputDto` | `LoginInputDto`, `ChangePasswordInputDto` |
| DTO de salida | `<Accion><Entidad>Dto` | `RegisterUserDto`, `CurrentUserDto`, `LoginTokenPairDto`, `RefreshTokenPairDto` |
| Resultado genérico | `OperationResult` (excepción, result type) | `OperationResult.Success()` |
| Repositorio (iface) | `I<Entidad>Repository` | `IUserRepository`, `IRefreshTokenRepository` |
| Repositorio (impl) | `<Entidad>Repository` en `Repositories/` | `UserRepository` |
| Servicio (iface) | `I<Cosa>Service` | `IJwtTokenService`, `IPasswordHasher` |
| Servicio (impl) | `<Cosa>Service` en `Services/` | `JwtTokenService`, `DateTimeProvider` |
| Config EF | `<Entidad>Configuration` en `Configurations/` | `UserConfiguration` |
| Migración | `<timestamp>_<Nombre>` generado por EF | `20260918041507_AddRefreshTokenConcurrency` |
| Exception de dominio | `<Algo>Exception` en `Exceptions/` | `DomainValidationException` |
| Excepción de app | `<Status>Exception` | `NotFoundException`, `ConflictException` |

---

## 6. Reglas de DTOs

- Los DTOs viven **solo** en `Application/DTOs/`. No hay `Contracts/` en WebApi, ni carpetas `Models/`.
- Los DTOs los crea **solo el use case**. Domain/Infrastructure nunca los referencian ni los crean.
- Los repositorios devuelven **entidades de dominio**, nunca DTOs.
- Nombre prohibido: `<Entidad>Dto` a secas (ej. `UserDto`) — siempre `<Accion><Entidad>Dto` o `<Caso>InputDto`.
- **Entrada (`[FromBody]`)**: `<Caso>InputDto` (records).
- **Salida**: `<Accion><Entidad>Dto` (ej. `Tag`… `CreateTagDto`).
- El request del cliente **no incluye** identidad derivada del token (`UserId`, `jti`, `exp`): el controller la inyecta desde claims (`ClaimsPrincipalExtensions`).

---

## 7. Errores y códigos estables

Formato de respuesta de error (middleware): `application/problem+json`

```json
{ "status": 400, "code": "password.policy", "message": "…", "errors": ["…"], "traceId": "…" }
```

| Código | HTTP | Origen |
|---|---|---|
| `email.conflict` | 409 | `RegisterUseCase` (chequeo previo) o mapeo de unique violation (`PersistenceErrors`, raza de registro) |
| `password.policy` | 400 | `PasswordPolicy.EnsureValid` |
| `password.incorrect` | 401 | `ChangePasswordUseCase` |
| `credentials.invalid` | 401 | `LoginUseCase` |
| `account.locked` | 401 | `LoginUseCase` (lockout) |
| `refresh.invalid` | 401 | token inexistente/expirado/revocado |
| `refresh.reuse` | 401 | reuso secuencial (`IsUsed`) **o** conflicto de concurrencia (`DbUpdateConcurrencyException` por `xmin`) |
| `refresh.already_used` | 400 | `RefreshToken.RotateTo` |
| `user.not_found` | 404 | `GetCurrentUser`/`ChangePassword`/`RevokeRefreshToken` |
| `internal.error` | 500 | error no controlado |

Reglas:

- Cada excepción lleva `code` **estable** (los tests de integración lo asertan) y mensaje en español.
- `email.conflict` y `refresh.reuse` tienen **doble origen** (chequeo del use case + mapeo de persistencia); no cambiar el código al agregar rutas nuevas.
- Extender `PersistenceErrors` al agregar índices únicos nuevos, mapeando `constraintName` → código.

---

## 8. Estructuras de datos (esquema)

### 8.1 Tablas (snake_case, EF Configurations)

**`users`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `email` | varchar(320) | **único** → índice `IX_users_email` (no renombrar; el mapper depende del nombre) |
| `password_hash_algorithm` | varchar(16) | owned type |
| `password_hash` | varchar(256) | owned type |
| `role` | varchar(20) | string del enum |
| `is_active` | bool | default true |
| `access_failed_count` | int | lockout tras 5 fallos |
| `lockout_end_utc` | timestamptz? | 15 min |
| `last_login_at_utc` | timestamptz? | |
| `created_at_utc` / `updated_at_utc` | timestamptz | |

**`refresh_tokens`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `user_id` | uuid (FK→users, cascade) | |
| `token_hash` | varchar(128) | **único** |
| `expires_at_utc` | timestamptz | ≤ 7 días desde emisión |
| `is_used` | bool | |
| `used_at_utc` | timestamptz? | |
| `is_revoked` | bool | |
| `revoked_at_utc` | timestamptz? | |
| `revoked_reason` | varchar(32) | string del enum `RevocationReason` |
| `replaced_by_token_id` | uuid? | rotación |
| `created_at_utc` / `updated_at_utc` | timestamptz | |
| **`xmin`** | xid (sistema) | **concurrency token** (`IsRowVersion()`); la migración es no-op porque la columna ya existe |

**`access_token_blacklist`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `jti` | uuid | **único** |
| `user_id` | uuid | |
| `expires_at_utc` | timestamptz | índice |
| `revoked_at_utc` / `created_at_utc` | timestamptz | |

### 8.2 Datos en memoria

- `IMemoryCache` (`jwt:blacklist:<jti>`) cachea el estado de blacklist con TTL hasta la expiración del token.

### 8.3 Migraciones

- Generar con: `dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure` (usa la design-time factory que lee `.env`; no necesita startup project).
- Antes de commitear, **revisar el SQL generado**: el concurrency por `xmin` genera `AddColumn xmin` que fallaría en Postgres → se deja la migración como no-op (la columna es del sistema, ya existe).
- `EnsureSchemaAsync()` (tests de integración) ejecuta `Database.MigrateAsync()` contra la **DB live**.
- Migraciones que tocan columnas/índices involucrados en mapeos (`IX_users_email`, `xmin`) deben conservar los nombres.

---

## 9. Configuración y secretos

- Toda la configuración viene del `.env` raíz (cargado por `EnvFileLoader`); `appsettings.json` **no** tiene connection string ni JWT.
- Claves: `DATABASE_URL`, `Jwt__Secret` (≥ 32 chars), `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenTtlMinutes`, `Jwt__RefreshTokenTtlDays` (`__` = separador de sección).
- Guardas de arranque (fail-fast) en `Infrastructure/DependencyInjection.AddAuth`.
- `.env` está gitignoreado: nunca commitearlo ni loguear/echo de sus valores.

---

## 10. Reglas de tests

- **UnitTests** (offline, rápidos): dominio (entidades, policy) y lógica pura (ej. `PersistenceErrors`). Puede referenciar Infrastructure.
- **ArchitectureTests** (offline): refs de capas + "sin setters públicos en entidades". Preservar ese invariante.
- **IntegrationTests** (mutan la **DB live** de Neon, requieren red): `WebApplicationFactory<Program>` + `ApiFactory.EnsureSchemaAsync()`. Cada test usa un email único; preferir `--filter` al iterar.
- Flujo a seguir al escribir un test
  1. definí la invariante/motivo (bug fix → test que reproduce),
  2. agregá el test, 
  3. `dotnet build ApiPoo2.sln` → units + architecture → integración filtrada.

---

## 11. Checklist de review

Al tocar código, verificá que:

- [ ] No hay referencias circulares ni saltos de capa.
- [ ] Ningún DTO cruza hacia Domain/Infrastructure; repos devuelven entidades.
- [ ] Registro DI explícito (sin reflection) en `DependencyInjection` de la capa.
- [ ] No hay MediatR/CQRS/Dispatcher.
- [ ] Entidades de dominio sin setters públicos; mutación por métodos.
- [ ] `code` estable y mensaje en español en toda excepción.
- [ ] Claims (UserId/jti/exp) fabricados en el controller, no en el body.
- [ ] Migración revisada (no-op de `xmin`, nombres de constraint intactos).
- [ ] Tests actualizados/agregados y suite verde.
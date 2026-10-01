# Manual de Arquitectura — ApiPoo2

API .NET 10 de personas, vehículos, documentos y autenticación (JWT de acceso + refresh tokens rotativos / blacklist de tokens, con APIKey como segunda autorización).
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
- **Infrastructure nunca referencia DTOs** (viven junto a los casos de uso): solo entidades de dominio e interfaces.
- **Domain/Infrastructure no crean DTOs**; los DTOs los produce el use case en Application.
- Los tests (UnitTests/IntegrationTests/ArchitectureTests) pueden referenciar cualquier capa.

---

## 3. Responsabilidades, reglas y limitaciones por capa

### 3.1 ApiPoo2.Domain (sin dependencias)

**Responsabilidades**

- Entidades por agregado: `Persona` + `ConductorVehiculo`, `Vehiculo`, `Documento` + `VehiculoDocumento`, `User`, `RefreshToken`, `BlacklistedToken`.
- Value Objects: `NumeroIdentificacion`, `Nombres`, `Apellidos`, `CorreoElectronico`, `Login`, `ApiKey`, `Placa`, `Color`, `Marca`, `Linea`, `DocumentoCodigo`, `DocumentoNombre`, `DocumentoDescripcion`, `NombreArchivo`, `ContenidoDocumento`, `PasswordHash`.
- Enums: `TipoIdentificacion`, `TipoPersona`, `EstadoConductor`, `TipoVehiculo`, `TipoServicio`, `TipoCombustible`, `TiposVehiculoAplicables`, `CodigoObligatoriedad`, `EstadoDocumento`, `UserRole`, `RevocationReason`.
- Sin eventos de dominio: se eliminaron por ser código muerto.
- Reglas de negocio e invariantes (aquí vive la única fuente de verdad):
  - `PasswordPolicy.EnsureValid(password)` — política de contraseñas.
  - `User.MaxFailedAccessAttempts = 5`, `User.DefaultLockoutDuration = 15 min`.
  - `User.MaxActiveRefreshTokens = 10` (al emitir el número 11, se revoca el más antiguo con `SessionLimitReached`).
  - `RefreshToken.MaxLifetime = 7 días` (`RefreshToken.Issue` rechaza TTL mayores).
- Excepciones de dominio: `DomainException`, `DomainValidationException` (con `code` + errores).

**Reglas / limitaciones**

- **Cero dependencias** de proyecto y cero librerías externas.
- Entidades con setters privados; **mutación solo por métodos/factorías de dominio** (ej. `Persona.CrearUsuario`, `User.Crear`, `Vehiculo.Register`, `Vehiculo.AdjuntarDocumento`, `Vehiculo.Actualizar`, `Documento.Register`, `User.ChangePassword`, `User.RegenerarApiKey`, `RefreshToken.Issue`, `RefreshToken.RotateTo`).
- Identidad normalmente `Guid` con `BaseEntity.Initialize` y `MarkUpdated(utcNow)` en toda mutación. Excepciones documentadas por PK compuesta: `User` (`idpersona + login`) y `VehiculoDocumento` (`idvehiculo + iddocumento`).
- No hay eventos de dominio; tampoco se registran estados en constructores.

### 3.2 ApiPoo2.Application (depende solo de Domain)

**Responsabilidades**

- **Use cases** por feature en `UseCases/<Feature>/<Caso>/`, heredando `BaseUseCase<TRequest, TResult>` (valida con FluentValidation y loguea inicio/fin). **NO CQRS, NO MediatR, NO Dispatcher.**
- **Validators** FluentValidation (uno por use case que lo requiera, junto al use case).
- **DTOs** de entrada/salida junto al caso de uso que los produce — ver convenciones en §6.
- **Excepciones de aplicación**: `BaseApplicationException` y subclases (`UnauthorizedException`, `NotFoundException`, `ConflictException`, `ForbiddenException`, `RequestValidationException`) con `code` estable.
- **Puertos (interfaces)**: `IRepositories/` (`IRepository<T>`, `IPersonaRepository`, `IUserRepository`, `IVehiculoRepository`, `IDocumentoRepository`, `IConductorVehiculoRepository`, `IRefreshTokenRepository`, `IBlacklistedTokenRepository`, `IUnitOfWork`) e `IServices/` (`IPasswordHasher`, `IJwtTokenService`, `IJwtTokenBlacklistService`, `IDateTimeProvider`, `JwtOptions`).
- `DependencyInjection.AddApplication()` registra **explícitamente** use cases y validators (`AddScoped`), sin reflection.

**Reglas / limitaciones**

- Prohibido: CQRS, MediatR, cualquier Dispatcher, carpetas `Pipeline`/`Behaviors`.
- **Los repositorios devuelven entidades de dominio, nunca DTOs.**
- La lógica de negocios vive en Domain; el use case **orquesta** (repositorios, servicios, validators) y produce DTOs.
- No puede referenciar EF ni ASP.NET.
- Cada use case inyecta los servicios que necesita por constructor (nada de service locator).
- Los `InputDto` que vienen del request **no deben contener claims derivables** (PersonaId, jti, expiración): el controller los fabrica desde el token.

### 3.3 ApiPoo2.Infrastructure (depende de Domain + Application)

**Responsabilidades**

- **Persistence** EF Core/Npgsql: `AppDbContext`, `Configurations/` (mapeos snake_case), `Repositories/` (implementaciones), `Migrations/`.
- **Mapeo de errores de persistencia**: `Persistence/Exceptions/PersistenceErrors.cs` traduce violaciones de esquema a excepciones de aplicación (ver §7).
- **Servicios**: `PasswordHasher` (BCrypt), `JwtTokenService` (emisión/validación de JWT + hash de refresh), `JwtTokenBlacklistService` (cache en memoria + tabla `access_token_blacklist`), `DateTimeProvider`.
- **Background service**: `TokenCleanupBackgroundService` (purga de blacklist expirada).
- **Carga de configuración**: `EnvFileLoader` lee `.env` en producción y `.env.test` en pruebas; `JwtOptions` vive en `Application/IServices`.
- `DependencyInjection.AddInfrastructure()` arma `AddPersistence` + `AddServices` + `AddAuth`.

**Reglas / limitaciones**

- **No referencia DTOs de Application** (no crea DTOs; los repositorios devuelven entidades).
- La conversión de excepciones EF a excepciones de aplicación ocurre aquí, porque ES la única capa que conoce EF (§7).
- Guardas de configuración al arrancar: secret ≥ 32 chars, `AccessTokenTtlMinutes > 0`, `RefreshTokenTtlDays` entre 1 y `RefreshToken.MaxLifetime.Days` (7) — falla rápido con `InvalidOperationException` en español.
- Nunca commitear `.env` ni `.env.test`; no volcar valores de secretos.
- EF configs usan snake_case; los enums se persisten con converters de códigos y los value objects con conversiones.

### 3.4 ApiPoo2.WebApi (composición root)

**Responsabilidades**

- **Composición root**: `Program.cs` (`public partial class Program`), registro de ambas capas inferiores y middleware.
- **Controllers**: endpoints REST. Inyectan use cases concretos por constructor (uno por endpoint) y bindean los `InputDto` de Application como `[FromBody]` o los arman con valores de ruta/claims.
- **Seguridad dual**: JWT autentica; la policy `ApiKey` exige además `X-Api-Key` y que pertenezca al usuario autenticado. Los endpoints de escritura de E1/E2 la requieren; las consultas públicas usan `[AllowAnonymous]`.
- **Extensiones de claims**: `GetUserId()`, `GetTokenJti()`, `GetTokenExpiresAtUtc()` fabrican campos derivados del access token (nunca vienen del body del cliente).
- **Middleware de errores**: `ExceptionHandlingMiddleware` mapea excepciones tipadas a `application/problem+json` `{status, code, message, errors, traceId}`.
- Configuración Swagger.

**Reglas / limitaciones**

- **Sin lógica de aplicación/dominio en el controller**: solo orquestar HTTP → use case.
- Los controllers NO definen DTOs propios (se eliminó `Contracts/`); reutilizan los DTOs de Application.
- Los campos derivados del token se construyen en el controller y se pasan en el `InputDto`.

---

## 4. Estructura de directorios

```
ApiPoo2.sln
├── AGENTS.md
├── .env                      (ignorado; credenciales live)
├── src/
│   ├── ApiPoo2.Domain/
│   │   ├── Users/            User, PasswordHash, UserRole, AuthenticationBlock
│   │   ├── Personas/         Persona, ConductorVehiculo, Login, ApiKey, identificaciones y tipos
│   │   ├── Vehiculos/        Vehiculo, Placa, Color, Marca, Linea y tipos
│   │   ├── Documentos/       Documento, VehiculoDocumento y contenido
│   │   ├── RefreshTokens/    RefreshToken, RevocationReason
│   │   ├── BlacklistedTokens/ BlacklistedToken
│   │   └── Common/           BaseEntity, PasswordPolicy, DomainException, DomainValidationException
│   ├── ApiPoo2.Application/
│   │   ├── Common/           IDateTimeProvider, PagedFilter, PagedResult
│   │   ├── Exceptions/       BaseApplicationException y subclases
│   │   ├── IRepositories/    IRepository<T>, I<Entidad>Repository, IUnitOfWork
│   │   ├── IServices/        IPasswordHasher, IJwtTokenService, IJwtTokenBlacklistService, IDateTimeProvider, JwtOptions
│   │   ├── UseCases/
│   │   │   ├── IUseCase.cs
│   │   │   ├── BaseUseCase.cs
│   │   │   └── <Feature>/<Caso>/   <Caso>UseCase.cs, <Caso>Validator.cs, <Caso>InputDto.cs, <Accion><Entidad>OutputDto.cs
│   │   ├── GlobalUsings.cs
│   │   └── DependencyInjection.cs
│   ├── ApiPoo2.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/      <Entidad>Configuration.cs y converters
│   │   │   ├── Exceptions/          PersistenceErrors.cs
│   │   │   ├── Migrations/          <timestamp>_<Name>.cs
│   │   │   ├── Repositories/        GenericRepository.cs, <Entidad>Repository.cs, UnitOfWork.cs
│   │   │   └── DesignTimeDbContextFactory.cs
│   │   ├── Security/         PasswordHasher, JwtTokenService, JwtTokenBlacklistService, TokenCleanupBackgroundService
│   │   ├── Common/           DateTimeProvider
│   │   ├── Persistence/EnvFileLoader.cs
│   │   └── DependencyInjection.cs
│   └── ApiPoo2.WebApi/
│       ├── Auth/             ApiKeyRequirement, ApiKeyHandler
│       ├── Controllers/      AuthController, PersonasController, UsuariosController, VehiculosController, DocumentosController, ConductoresController, UsersController
│       ├── Extensions/       ClaimsPrincipalExtensions, SwaggerExtensions
│       ├── Middleware/       ExceptionHandlingMiddleware
│       └── Program.cs
└── tests/
    ├── ApiPoo2.UnitTests/          Domain/, Persistence/
    ├── ApiPoo2.Application.Tests/  Common/ (Pagination, PasswordPolicy)
    ├── ApiPoo2.ArchitectureTests/  LayerDependencyTests, RichDomainModelTests
    └── ApiPoo2.IntegrationTests/   ApiFactory, ApiCollection, AuthApiTests, FuncionalesApiTests
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
| Namespaces | `ApiPoo2.<Capa>.<Subcarpeta>`, una capa por feature (ej. `ApiPoo2.Application.UseCases.Auth`) |
| Archivos | uno por tipo, nombrado igual que el tipo |
| Tipos sellados | use cases, validators, configs, repositorios concretos: `sealed` |
| Entidades de dominio | `sealed`, sin setters públicos, constructor privado + factoría estática validante; mutación por métodos de dominio (nombres en verbo, ej. `ChangePassword`, `Revoke`, `RotateTo`) |
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

- Los DTOs viven **junto al caso de uso que los produce** (`UseCases/<Feature>/<Caso>/`). No hay carpeta compartida `DTOs/`, ni `Contracts/` en WebApi, ni carpetas `Models/`.
- Los DTOs los crea **solo el use case**. Domain/Infrastructure nunca los referencian ni los crean.
- Los repositorios devuelven **entidades de dominio**, nunca DTOs.
- Nombre prohibido: `<Entidad>Dto` a secas (ej. `UserDto`) — siempre `<Accion><Entidad>OutputDto` o `<Caso>InputDto`.
- **Entrada (`[FromBody]`)**: `<Caso>InputDto` (records).
- **Salida**: `<Accion><Entidad>OutputDto`.
- El request del cliente **no incluye** identidad derivada del token (`PersonaId`, `jti`, `exp`): el controller la inyecta desde claims (`ClaimsPrincipalExtensions`).

---

## 7. Errores y códigos estables

Formato de respuesta de error (middleware): `application/problem+json`

```json
{ "status": 400, "code": "password.policy", "message": "…", "errors": ["…"], "traceId": "…" }
```

| Código | HTTP | Origen |
|---|---|---|
| `persona.identificacion.conflict` | 409 | `CreatePersonaUseCase` (chequeo previo) o mapeo de unique violation (`PersistenceErrors`, condición de carrera) |
| `persona.correo.conflict` | 409 | `UpdatePersonaUseCase` (correo ya usado por otra persona) |
| `vehiculo.placa.conflict` | 409 | `CreateVehiculoUseCase`/`UpdateVehiculoUseCase` (chequeo previo) o mapeo de unique violation |
| `user.login.conflict` | 409 | no se pudo generar un login mnemotécnico único |
| `password.policy` | 400 | `PasswordPolicy.EnsureValid` |
| `password.incorrect` | 401 | `ChangePasswordUseCase` |
| `credentials.invalid` | 401 | `LoginUseCase` |
| `account.locked` | 401 | `LoginUseCase` (lockout) |
| `refresh.invalid` | 401 | token inexistente/expirado/revocado |
| `refresh.reuse` | 401 | reuso secuencial (`IsUsed`) **o** conflicto de concurrencia (`DbUpdateConcurrencyException` por `xmin`) |
| `refresh.already_used` | 400 | `RefreshToken.RotateTo` |
| `user.not_found` | 404 | persona sin usuario asociado en `GetCurrentUser`/`ChangePassword`/`Logout`/`RefreshToken`/`RevokeRefreshToken` |
| `person.not_found` | 404 | persona inexistente |
| `vehicle.not_found` | 404 | vehículo inexistente |
| `document.not_found` | 404 | documento paramétrico inexistente |
| `internal.error` | 500 | error no controlado |

Reglas:

- Cada excepción lleva `code` **estable** (los tests de integración lo asertan) y mensaje en español.
- `persona.identificacion.conflict`, `vehiculo.placa.conflict` y `refresh.reuse` tienen **doble origen** (chequeo del use case + mapeo de persistencia); no cambiar el código al agregar rutas nuevas.
- Extender `PersistenceErrors` al agregar índices únicos nuevos, mapeando `constraintName` → código.

---

## 8. Estructuras de datos (esquema)

### 8.1 Tablas (snake_case, EF Configurations)

**`personas`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `tipo_identificacion` | varchar | códigos `CC/CE/TI/NIT` + CHECK |
| `numero_identificacion` | varchar(20) | **único**, solo dígitos + CHECK |
| `nombres` | varchar(100) | |
| `apellidos` | varchar(100) | |
| `correo_electronico` | varchar(320) | **único**, formato + CHECK |
| `tipo_persona` | varchar | `ADMINISTRATIVO/CONDUCTOR` + CHECK |
| `created_at_utc` / `updated_at_utc` | timestamptz | |

**`usuarios`**

| Columna | Tipo | Nota |
|---|---|---|
| `idpersona` | uuid (PK, FK→personas, cascade) | parte 1 de la PK compuesta |
| `login` | varchar(64) (PK) | mnemotécnico, parte 2 de la PK compuesta |
| `password_hash` / `password_hash_algorithm` | varchar | BCrypt; nunca en claro |
| `api_key` | varchar | **único**, autogenerada |
| `rol` | varchar(20) | string del enum |
| `is_active` | bool | default true |
| `access_failed_count` | int | lockout tras 5 fallos |
| `lockout_end_utc` | timestamptz? | 15 min |
| `last_login_at_utc` | timestamptz? | |
| `created_at_utc` / `updated_at_utc` | timestamptz | |

**`refresh_tokens`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `idpersona` | uuid (FK→personas, cascade) | |
| `login` | varchar(64) (FK→usuarios, cascade) | junto con `idpersona` referencia la PK compuesta |
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
| `idpersona` | uuid | persona titular del token revocado |
| `expires_at_utc` | timestamptz | índice |
| `revoked_at_utc` / `created_at_utc` | timestamptz | |

**`vehiculos`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `placa` | varchar(10) | **única**, formato según tipo + CHECK |
| `tipo_vehiculo` | varchar | `AUTOMOVIL/MOTOCICLETA` + CHECK |
| `tipo_servicio` | varchar | `PUBLICO/PRIVADO` + CHECK |
| `tipo_combustible` | varchar | `GASOLINA/GAS/DISEL` + CHECK |
| `capacidad_pasajeros` | int | `>= 0` + CHECK |
| `color` | varchar(7) | `#RRGGBB` + CHECK |
| `modelo` | int | `> 0` + CHECK |
| `marca` / `linea` | varchar(50) | |
| `created_at_utc` / `updated_at_utc` | timestamptz | |

**`documentos`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | catálogo paramétrico, sin binario |
| `codigo` | varchar(30) | **único** |
| `nombre` | varchar(100) | |
| `tipos_vehiculo_aplicables` | varchar(2) | `A/M/AM` + CHECK |
| `codigo_obligatoriedad` | varchar(2) | `RA/RM/RR` + CHECK |
| `descripcion` | varchar(500) | |

**`vehiculos_documentos`**

| Columna | Tipo | Nota |
|---|---|---|
| `idvehiculo` | uuid (PK, FK→vehiculos, cascade) | parte 1 de la PK compuesta |
| `iddocumento` | uuid (PK, FK→documentos, restrict) | parte 2 de la PK compuesta |
| `contenido` | `bytea` | PDF binario, no vacío + CHECK |
| `nombre_archivo` | varchar(255) | |
| `fecha_expedicion` | timestamptz | no futura |
| `fecha_vencimiento` | timestamptz | posterior a expedición |
| `estado` | varchar(20) | `HABILITADO/VENCIDO/EN_VERIFICACION` + CHECK |
| `created_at_utc` / `updated_at_utc` | timestamptz | |

**`conductores_vehiculos`**

| Columna | Tipo | Nota |
|---|---|---|
| `id` | uuid (PK) | |
| `idpersona` | uuid (FK→personas, cascade) | debe ser `CONDUCTOR` (dominio/caso de uso) |
| `idvehiculo` | uuid (FK→vehiculos, cascade) | |
| `fecha_asociacion` | timestamptz | |
| `estado` | varchar(2) | `PO/EA/RO` + CHECK |
| pareja `idpersona + idvehiculo` | única | evita asociaciones duplicadas |

### 8.2 Datos en memoria

- `IMemoryCache` (`jwt:blacklist:<jti>`) cachea el estado de blacklist con TTL hasta la expiración del token.

### 8.3 Migraciones

- Generar con: `dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure` (usa la design-time factory que lee `.env`; no necesita startup project).
- Antes de commitear, **revisar el SQL generado**: el concurrency por `xmin` genera `AddColumn xmin` que fallaría en Postgres → se deja la migración como no-op (la columna es del sistema, ya existe).
- IntegrationTests usan `.env.test` y Docker local; `ApiFactory` crea el schema aislado y aplica migraciones ahí. Nunca apuntan a Neon.
- Migraciones que tocan columnas/índices involucrados en mapeos (`personas_numero_identificacion`, `vehiculos_placa`, `xmin`) deben conservar los nombres.

---

## 9. Configuración y secretos

- Toda la configuración viene del `.env` raíz en producción (cargado por `EnvFileLoader`) y de `.env.test` en pruebas; `appsettings.json` **no** tiene connection string ni JWT.
- Claves: `DATABASE_URL` (Neon en producción; Docker local `api_poo2_test:5433` en pruebas), `Jwt__Secret` (≥ 32 chars), `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenTtlMinutes`, `Jwt__RefreshTokenTtlDays` (`__` = separador de sección).
- Guardas de arranque (fail-fast) en `Infrastructure/DependencyInjection.AddAuth`.
- `.env` y `.env.test` están gitignoreados: nunca commitearlos ni loguear/echo de sus valores.

---

## 10. Reglas de tests

- **UnitTests** (offline, rápidos): entidades, value objects y `PasswordPolicy`.
- **ApplicationTests** (offline): comportamiento de la capa Application sin DB — `PasswordPolicy` (la política que la entidad no puede imponer) y paginación.
- **ArchitectureTests** (offline): refs de capas + invariantes del modelo rico (`RichDomainModelTests`: entidades `sealed`, sin constructores públicos, sin setters públicos, construidas solo por factorías).
- **IntegrationTests** (requieren Docker local `api_poo2-test-db:5433` y red local): `WebApplicationFactory<Program>`. Usan identificaciones/placas/códigos únicos; preferir `--filter` al iterar.
  - **Aislamiento por ejecución**: `ApiFactory` crea un schema propio (`test_<guid>`) inyectándolo como `Search Path` en la cadena de conexión, reproduce todas las migraciones ahí, siembra un administrativo mínimo y lo borra con `CASCADE` al terminar. Las ejecuciones no tocan las tablas de `public` ni dejan residuos. Neon queda reservado para producción.
- Flujo a seguir al escribir un test
  1. definí la invariante/motivo (bug fix → test que reproduce),
  2. agregá el test,
  3. `dotnet build ApiPoo2.sln` → unit + application + architecture → integración filtrada.

---

## 11. Checklist de review

Al tocar código, verificá que:

- [ ] No hay referencias circulares ni saltos de capa.
- [ ] Ningún DTO cruza hacia Domain/Infrastructure; repos devuelven entidades.
- [ ] Registro DI explícito (sin reflection) en `DependencyInjection` de la capa.
- [ ] No hay MediatR/CQRS/Dispatcher.
- [ ] Entidades de dominio `sealed`, sin setters ni constructores públicos; mutación por métodos de dominio.
- [ ] DTOs nuevos junto a su caso de uso y nombrados `*InputDto` / `*OutputDto`.
- [ ] Si tocás `ApiFactory`, mantené el aislamiento por schema (`Search Path`).
- [ ] `code` estable y mensaje en español en toda excepción.
- [ ] Claims (PersonaId/jti/exp) fabricados en el controller, no en el body.
- [ ] Endpoints de escritura protegidos con JWT + `X-Api-Key`; consultas públicas con `[AllowAnonymous]` solo donde el enunciado lo permite.
- [ ] Migración revisada (no-op de `xmin`, nombres de constraint intactos).
- [ ] Tests actualizados/agregados y suite verde.

---

## 12. Deuda técnica conocida

| Deuda | Por qué | Mitigación actual |
|---|---|---|
| `PasswordPolicy` fuera de la entidad | `User` recibe el *hash*, no el secreto, así que la entidad no puede evaluar la política | Los casos de uso la invocan antes de hashear; `PasswordPolicyTests` fija el comportamiento |
| `Pagination` sin uso | `PagedFilter`/`PagedResult` se usarán con el primer endpoint paginado | Listo para usar; cubierto por `PaginationTests` |
| Login mnemotécnico y PK compuesta | `User` (`idpersona + login`) y `VehiculoDocumento` (`idvehiculo + iddocumento`) no heredan `BaseEntity` | Excepción documentada y testeada en `RichDomainModelTests` |

## 13. Divergencias respecto de Robot/Api (referencia estructural)

La referencia se usó como guía de **estructura de carpetas y contratos**, no de diseño de dominio. Divergencias deliberadas:

| Tema | Robot/Api | API_PPOO | Motivo |
|---|---|---|---|
| Setters de entidad | públicos (anémico) | privados + métodos de dominio | El usuario pidió dominio rico; evitar que se invente un patrón anémico |
| Eventos de dominio | presentes | eliminados | Ninguno se publicaba ni se suscribía: código muerto |
| Unidad de trabajo | no usada | `IUnitOfWork` | El mapeo de errores de concurrencia/unicidad vive ahí |
| Value objects | no usados | `Login`, `PasswordHash`, `Placa`, `CorreoElectronico`, etc. | Evitan que login, hash, placa y correo sean strings sueltos |
| Sello de entidades | `sealed` | `sealed` | Alineado: impide heredar y llamar `Initialize` para forzar identidad |

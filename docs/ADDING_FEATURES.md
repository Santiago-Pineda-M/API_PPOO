# Cómo añadir funcionalidades — API_PPOO

Guía operativa para extender la API sin romper la arquitectura. El detalle de
capas y responsabilidades está en [`ARCHITECTURE.md`](../ARCHITECTURE.md); las
reglas de pruebas y cobertura, en [`TESTING.md`](TESTING.md).

## Reglas no negociables

1. **Dirección de dependencias:** `Domain ← Application ← Infrastructure ← WebApi`.
   Domain no referencia nada; Application solo Domain; WebApi no contiene lógica
   de negocio. `LayerDependencyTests` lo verifica.
2. **Un caso de uso por acción**, clase concreta en
   `UseCases/<Feature>/<Caso>/`, heredando `BaseUseCase<TRequest, TResult>`.
   Sin CQRS, MediatR ni Dispatcher.
3. **DTOs co-locados** junto al caso de uso, nunca en una carpeta `DTOs/`:
   entrada `<Caso>InputDto`, salida `<Accion><Entidad>OutputDto`.
4. **Registro DI explícito** (sin reflection) en
   `Application/DependencyInjection.cs` (`AddScoped` para casos de uso y
   validadores) y, si aplica, en `Infrastructure/DependencyInjection.cs`.
5. **Errores tipados** (`BaseApplicationException`, `DomainValidationException`)
   con `code` estable y mensaje en español. El middleware los traduce a
   `application/problem+json`.
6. **Entidades ricas:** `sealed`, sin setters ni constructores públicos;
   factoría estática validante (`Register`, `Crear`, `Issue`). La mutación va por
   métodos de dominio.
7. **Migraciones** para todo cambio de esquema, con nombres snake_case.
8. **Tests obligatorios** (ver checklist de `TESTING.md`) y suite verde.

## Checklist rápido

```
[ ] Contrato del endpoint definido (método, ruta, body, códigos)
[ ] Regla de negocio en Domain (entidad/VO) si aplica
[ ] InputDto + OutputDto junto al caso de uso
[ ] Validator FluentValidation
[ ] UseCase sobre BaseUseCase
[ ] Puerto de repositorio en Application/IRepositories (si hay datos nuevos)
[ ] Implementación EF + configuración + migración (si aplica)
[ ] Registro DI en Application (caso de uso + validator)
[ ] Endpoint en el controller con auth correcta
[ ] Test unitario (Domain/Application) + test de integración
[ ] Swagger/README actualizados
[ ] dotnet build + suites offline + integración
```

## Receta A — endpoint nuevo en un recurso existente

Ejemplo real: `POST /api/usuarios` (`CrearUsuarioUseCase`).

1. **Contrato.** Define método, ruta, body y respuestas. Escrituras: JWT +
   `X-Api-Key`; consultas públicas: `[AllowAnonymous]` solo si el enunciado lo
   permite.
2. **Input/Output DTO** en `src/ApiPoo2.Application/UseCases/<Feature>/<Caso>/`:

   ```csharp
   public sealed record CrearUsuarioInputDto(Guid PersonaId, string Password);
   ```

   Salidas reutilizables van junto al feature (`Usuarios/UsuarioOutputDto.cs`).
3. **Validator** (`<Caso>Validator : AbstractValidator<TInput>`) con mensajes en
   español y límites tomados de Domain (`PasswordPolicy.MinimumLength`).
4. **Caso de uso**:

   ```csharp
   public sealed class CrearUsuarioUseCase
       : BaseUseCase<CrearUsuarioInputDto, UsuarioOutputDto>
   {
       // inyecta puertos: repositorios, hasher, reloj, unit of work
       protected override async Task<UsuarioOutputDto> ExecuteCoreAsync(
           CrearUsuarioInputDto request, CancellationToken cancellationToken)
       {
           var persona = await _personaRepository.GetByIdAsync(request.PersonaId, cancellationToken)
               ?? throw new NotFoundException("persona.not_found", "La persona no existe.");
           // reglas de dominio, persistencia y SaveChanges
       }
   }
   ```

   El flujo es: validar → cargar agregado → aplicar regla de dominio →
   persistir → devolver DTO. No pongas lógica de negocio en el controller.
5. **Repositorio (si hace falta).** Añade el método al puerto
   `Application/IRepositories/I<Xxx>Repository.cs` y su implementación EF en
   `Infrastructure/Persistence/Repositories/`. Los repos devuelven **entidades**,
   nunca DTOs.
6. **Registro DI** en `Application/DependencyInjection.cs`:

   ```csharp
   services.AddScoped<CrearUsuarioUseCase>();
   services.AddScoped<IValidator<CrearUsuarioInputDto>, CrearUsuarioValidator>();
   ```
7. **Controller.** Inyecta el caso de uso concreto y expón el endpoint:

   ```csharp
   [Authorize(Roles = "Administrator")]
   [HttpPost]
   public async Task<ActionResult<UsuarioOutputDto>> Crear(
       [FromBody] CrearUsuarioInputDto request, CancellationToken cancellationToken)
       => StatusCode(StatusCodes.Status201Created,
           await _crearUsuario.ExecuteAsync(request, cancellationToken));
   ```

   Los claims (`PersonaId`, `jti`, `exp`) se fabrican en el controller, no en el
   body.
8. **Tests.** Unitario del caso de uso con fakes + integración del endpoint
   (camino feliz, 400 de validación, 401/403, 404/409 si aplica). Ver
   [`TESTING.md`](TESTING.md).

## Receta B — feature/agregado nuevo

Orden de creación (cada paso depende del anterior):

| # | Capa | Archivo |
|---|---|---|
| 1 | Domain | `src/ApiPoo2.Domain/<Feature>/<Entidad>.cs` + value objects + enums |
| 2 | Domain | Invariantes y métodos de mutación en la entidad |
| 3 | Application | `IRepositories/I<Feature>Repository.cs` |
| 4 | Application | `UseCases/<Feature>/<Caso>/<Caso>InputDto.cs`, `...Validator.cs`, `...UseCase.cs`, `...OutputDto.cs` |
| 5 | Application | Registro en `DependencyInjection.cs` |
| 6 | Infrastructure | `Persistence/Configurations/<Entidad>Configuration.cs` (snake_case) |
| 7 | Infrastructure | `Persistence/Repositories/<Feature>Repository.cs` + `DbSet` en el contexto |
| 8 | Infrastructure | Registro del repositorio en `Infrastructure/DependencyInjection.cs` |
| 9 | Infrastructure | Migración: `dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure` |
| 10 | WebApi | `Controllers/<Feature>Controller.cs` |
| 11 | Tests | `DomainTests`, `ApplicationTests`, `IntegrationTests` |
| 12 | Docs | `README.md` (endpoints), Swagger queda automático |

## Receta C — cambio en una regla existente

1. Localiza la invariante: si es de negocio, vive en Domain, no en el caso de uso.
2. Escribe primero el test que falla (Domain o Application).
3. Cambia la entidad/método de dominio.
4. Ajusta el caso de uso solo si cambia la orquestación.
5. Añade/actualiza el test de integración si cambia el contrato HTTP o un código
   de error.
6. Si cambia el esquema, añade migración.

## Dónde NO tocar

- Domain no referencia EF, ASP.NET, FluentValidation ni otros proyectos.
- Application no referencia Infrastructure ni WebApi.
- WebApi no accede al `DbContext` ni a repositorios: solo casos de uso.
- No añadas MediatR/CQRS/Dispatcher, `DTOs/` compartido ni `throw` genéricos sin
  `code`.
- No cambies el aislamiento por schema de `ApiFactory` sin entender
  `NpgsqlConnectionStringBuilder.SearchPath`.

## Migraciones

```bash
dotnet ef migrations add <Name> --project src/ApiPoo2.Infrastructure
dotnet ef database update --project src/ApiPoo2.Infrastructure
```

El design-time factory lee el `.env` de la raíz. Revisa el `Up`/`Down` y que el
`xmin` de `refresh_tokens` siga como no-op.

## Documentación que debes actualizar

- `README.md` §6 (endpoints principales) y §10 (índice de documentos).
- `ARCHITECTURE.md` solo si cambia una regla estructural.
- `docs/TESTING.md` si cambia la estrategia o los umbrales de cobertura.
- Códigos de error nuevos: `ARCHITECTURE.md` §7.

## Commits

Conventional commits en español:

```
feat(usuarios): añade listado paginado de administradores
fix(auth): evita reutilización de refresh token
test(usuarios): cubre duplicado de usuario en integración
docs: documenta política de cobertura
```

Un commit = un cambio coherente; no mezcles migraciones con refactors.

## Trabajo pendiente conocido

- `POST /api/usuarios` solo es útil para personas administrativas heredadas sin
  usuario: `CreatePersonaUseCase` genera el usuario automáticamente para toda
  persona administrativa y las conductoras no pueden tenerlo. Revisar si el
  endpoint debe mantenerse, eliminarse o cambiar la regla de auto-creación.
- El cliente web (`../CLIENTE_PPOO`) aún no consume el CRUD de usuarios.

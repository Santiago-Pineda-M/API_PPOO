# Pruebas y cobertura — API_PPOO

Reglas operativas para añadir tests y para interpretar la cobertura. El marco
arquitectónico está en [`ARCHITECTURE.md`](../ARCHITECTURE.md) §10; este documento
es la guía práctica.

## Mapa de suites

| Proyecto | Qué cubre | Base de datos |
|---|---|---|
| `ApiPoo2.DomainTests` | Entidades, value objects, invariantes, `PasswordPolicy` | ninguna |
| `ApiPoo2.ApplicationTests` | Orquestación de casos de uso con fakes en memoria (`Common/TestDoubles/`) | ninguna |
| `ApiPoo2.InfrastructureTests` | Lógica determinista: hashing, JWT, converters, `DateTimeProvider` | ninguna |
| `ApiPoo2.WebApiTests` | Piezas aisladas: `ApiKeyHandler`, middleware de errores, claims | ninguna |
| `ApiPoo2.ArchitectureTests` | Referencias entre capas, modelo rico y naming de tests | ninguna |
| `ApiPoo2.IntegrationTests` | API completa vía `WebApplicationFactory<Program>` | Docker `:5433`, schema aislado por corrida |

Solo `IntegrationTests` toca PostgreSQL. Los repositorios y las migraciones se
prueban **únicamente** ahí.

## Reglas obligatorias

1. **Un proyecto por capa.** No mezcles pruebas de Domain en `WebApiTests` ni
   referencies capas que el proyecto no debería conocer (`LayerDependencyTests`
   lo verifica).
2. **Nombres:** `Sujeto_Comportamiento[_Detalle]`, en español, sin segmento
   `Should`. `TestNamingTests` falla el build si no se cumple.
   - ✅ `From_RechazaVacio`, `CrearUsuario_PersonaConUsuarioYa_DominioRechaza`
   - ❌ `ShouldRejectEmpty`, `Test1`, `crear_usuario`
3. **Un test por invariante o bug.** Si corriges un fallo, añade el test que lo
   reproduce antes del arreglo.
4. **Determinismo:** nada de `Thread.Sleep`, `DateTime.Now` ni red. Usa
   `IDateTimeProvider`/fakes y datos únicos.
5. **Integración aislada:** `ApiFactory` crea un schema `test_<guid>`, replica
   migraciones y lo borra con `CASCADE`. Nunca apuntes a Neon.
6. **Datos únicos:** identificaciones, placas y códigos nuevos por test
   (`NumeroAleatorio()` en `UsuariosApiTests` es el patrón).
7. **Paquetes de test centralizados** en `tests/Directory.Packages.props`.
   Nunca escribas `Version=` en un `PackageReference` de test.
8. **Suite verde antes de commitear**: `dotnet build` + todas las suites
   offline + integración (si tocaste persistencia o endpoints).

## Cómo añadir un test

### Domain (offline)

```csharp
[Fact]
public void CrearUsuario_SinPermiso_LanzaDominio()
{
    var persona = Persona.Register(/* ... TipoPersona.Conductor ... */);

    var act = () => persona.CrearUsuario(hash, Ahora);

    act.Should().Throw<DomainValidationException>()
        .Which.Code.Should().Be("persona.usuario.no_admitido");
}
```

### Application con fakes (offline)

Vive en `tests/ApiPoo2.ApplicationTests/<Feature>/`. Reutiliza
`Common/TestDoubles/` (`FakePersonaRepository`, `FakeUnitOfWork`,
`FakePasswordHasher`, `FakeDateTimeProvider`) y comprueba el efecto sobre el
repositorio y el código de excepción:

```csharp
var useCase = new CrearUsuarioUseCase(
    [], TestData.LoggerFactory, personasRepo, userRepo,
    new FakePasswordHasher(), new FakeDateTimeProvider(), new FakeUnitOfWork());

var resultado = await useCase.ExecuteAsync(new CrearUsuarioInputDto(persona.Id, "Pass123!"));

resultado.Login.Value.Should().StartWith("CP");
userRepo.Items.Should().ContainSingle();
```

### Infrastructure / WebApi (offline)

Solo lógica sin I/O: hashing, JWT, converters, middleware, handlers. Si necesitas
base de datos, va a integración.

### Integración (Docker)

Vive en `tests/ApiPoo2.IntegrationTests/<Recurso>ApiTests.cs`, con
`[Collection("API")]` y `ApiFactory`. Autentica con la semilla
(`ApiFactory.SeedLogin/SeedPassword`) y añade JWT + `X-Api-Key`:

```csharp
[Fact]
public async Task ListarUsuarios_ConCredencialesAdmin_DevuelveLista()
{
    await AutenticarComoSemilla();

    var response = await _client.GetAsync("/api/usuarios");

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    (await ReadAs<PagedUsuarioResponse>(response)).Items.Should().NotBeEmpty();
}
```

**Regla funcional:** todo endpoint nuevo o cambiado debe tener al menos un test
de integración que cubra el camino feliz y los códigos de error relevantes
(401/403/400/404/409).

## Comandos

```bash
# Suite completa (incluye integración; requiere Docker en :5433)
dotnet test ApiPoo2.sln

# Solo suites offline (sin Docker)
for p in DomainTests ApplicationTests InfrastructureTests WebApiTests ArchitectureTests; do
  dotnet test "tests/ApiPoo2.$p"
done

# Un test puntual
dotnet test tests/ApiPoo2.DomainTests --filter "FullyQualifiedName~UserTests"

# Integración filtrada (iterando)
dotnet test tests/ApiPoo2.IntegrationTests --filter "FullyQualifiedName~UsuariosApiTests"
```

Base de pruebas:

```bash
docker compose up -d          # api-poo2-test-db en localhost:5433
# o: podman run -d --name api-poo2-test-db ... postgres:16-alpine
```

Los tests de integración leen `.env.test` (nunca `.env`) y fuerzan sus valores
contra el Docker local.

## Cobertura

Herramienta: **coverlet.msbuild 6.0.4**, referenciado una sola vez en
`tests/Directory.Build.props` y versionado en `tests/Directory.Packages.props`.
`coverlet.collector` sigue disponible para `--collect:"XPlat Code Coverage"`.

Comando base (ejemplo Domain):

```bash
dotnet test tests/ApiPoo2.DomainTests \
  /p:CollectCoverage=true \
  "/p:Include=[ApiPoo2.Domain]*" \
  /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=total
```

### Umbrales (gate de cobertura)

| Capa | Umbral mínimo | Cobertura actual |
|---|---|---|
| Domain | **90 %** líneas | 95.9 % |
| Application | **50 %** líneas | 54.3 % |
| Infrastructure | **5 %** líneas | 5.0 % |
| WebApi | **15 %** líneas | 15.4 % |

`IntegrationTests` **no** entra en el gate: mide la app completa con base de
datos y sus cifras no son comparables con las suites unitarias.

**Por qué los umbrales de Infrastructure/WebApi son bajos:** ahí solo se prueba
la lógica determinista; el resto (repositorios EF, migraciones, controllers) se
cubre en integración por diseño. El umbral existe para **evitar regresiones**,
no para premiar cobertura.

### Reglas de cobertura

1. **Ratchet:** un PR nunca puede bajar el umbral de una capa. Si tu cambio
   reduce la cobertura, añade tests o justifica la excepción en el PR.
2. **Al mejorar, sube el umbral** en el mismo PR (así se movió Application de 30 → 50).
3. **Código nuevo con lógica** (entidades, casos de uso, servicios) debe venir
   con tests unitarios; los endpoints, con integración.
4. **No se persigue un global único.** La métrica que importa es la funcional:
   cada endpoint tiene su test de integración.
5. **Reporte:** los `.cobertura.xml` quedan en `TestResults/coverage/` al correr
   los comandos de arriba; inspeccionalos para ver las líneas sin cubrir.

## Verificación local

El repositorio no tiene pipeline de CI configurado, así que la verificación se
corre a mano antes de commitear:

1. `dotnet restore ApiPoo2.sln` + `dotnet build ApiPoo2.sln -c Release`.
2. Suites offline + gate de cobertura por capa (comandos de arriba).
3. Integración con PostgreSQL 16 local en `:5433` y `.env.test` (nunca secretos
   reales).

Con eso queda cubierto en local lo que antes hacía el pipeline.

# API_PPOO

API REST en **.NET 10** para gestión de **personas, vehículos, documentos paramétricos, rutas con sus paradas, cargue masivo de trayectos por Excel y autenticación** (JWT de acceso + refresh tokens rotativos con blacklist, y `X-Api-Key` como segunda autorización).

- **Base de datos:** PostgreSQL (Neon en producción, Docker local para pruebas) vía Npgsql + EF Core.
- **Arquitectura:** Clean Architecture en 4 capas (`Domain ← Application ← Infrastructure ← WebApi`).
- **Documentación interactiva:** Swagger UI con esquemas `Bearer` (JWT) y `ApiKey` (`X-Api-Key`).

> Documento complementario de arquitectura: [`ARCHITECTURE.md`](ARCHITECTURE.md).
> Requerimientos funcionales: [`REQUERIMIENTOS.md`](REQUERIMIENTOS.md).

---

## 1. Requisitos previos

| Herramienta | Versión mínima | Para qué |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** | compilar y correr la API |
| PostgreSQL | 14+ | base de datos (producción) |
| Docker / Podman | cualquiera | solo para las **pruebas de integración** |
| `dotnet-ef` (herramienta global) | 10.x | solo si vas a crear migraciones |

```bash
dotnet --version        # debe mostrar 10.x
```

Instalar la herramienta de Entity Framework (opcional, solo para migraciones):

```bash
dotnet tool install --global dotnet-ef --version 10.*
```

---

## 2. Descargar el proyecto

```bash
git clone https://github.com/Santiago-Pineda-M/API_PPOO.git
cd API_PPOO
```

---

## 3. Instalación y configuración

### 3.1 Restaurar paquetes

```bash
dotnet restore ApiPoo2.sln
```

### 3.2 Configurar las variables de entorno

Toda la configuración se lee del archivo **`.env` en la raíz del repositorio** (lo carga `EnvFileLoader`). No está en el repo porque contiene credenciales.

```bash
cp .env.example .env
```

Edita `.env` con tus valores:

| Clave | Obligatoria | Descripción |
|---|---|---|
| `DATABASE_URL` | sí | Connection string de PostgreSQL, p. ej. `postgresql://user:password@host:5432/db?sslmode=require` |
| `Jwt__Secret` | sí | Secreto de firma, **mínimo 32 caracteres** (la app falla al arrancar si no) |
| `Jwt__Issuer` | sí | Emisor del token (por defecto `ApiPoo2`) |
| `Jwt__Audience` | sí | Audiencia del token (por defecto `ApiPoo2`) |
| `Jwt__AccessTokenTtlMinutes` | sí | Vigencia del access token en minutos |
| `Jwt__RefreshTokenTtlDays` | sí | Vigencia del refresh token en días (máx. 7) |
| `Cors__AllowedOrigins` | no | Orígenes permitidos separados por coma; vacío bloquea el cross-origin desde navegador (Postman/curl no se ven afectados) |
| `Bootstrap__*` | no | Datos del primer administrador (`POST /api/bootstrap/admin`) |
| `Geocoding__*` | no | Geocodificación con Nominatim: `BaseUrl`, `UserAgent` (con contacto), `PaisCodigo`, `TimeoutSegundos`, `PausaEntreLlamadasMs`, `MaxPorCiclo`, `CacheTtlHoras` |
| `Trayectos__Cargue__*` | no | Límites del Excel de cargue: `TamanioMaximoMb`, `MaxFilas` |

> `.env` y `.env.test` están en `.gitignore`. **Nunca los commitees.**

### 3.3 Crear la base de datos y aplicar migraciones

Con la base creada (Neon o un PostgreSQL local), aplica las migraciones:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet ef database update --project src/ApiPoo2.Infrastructure
```

Esto crea las tablas: `personas`, `usuarios`, `refresh_tokens`, `vehiculos`, `tipos_documento`, `documentos_vehiculo`, `conductores_vehiculos`, `rutas`, `paradas`, `trayectos_tmp` y `access_token_blacklist`, además de las funciones PL/pgSQL del cargue masivo (`sp_trayecto_cargue`, `sp_trayecto_validar`, `sp_trayecto_procesar`).

### 3.4 Compilar

```bash
dotnet build ApiPoo2.sln
```

---

## 4. Correr la API

```bash
dotnet run --project src/ApiPoo2.WebApi
```

| URL | Descripción |
|---|---|
| `http://localhost:5114` | HTTP (perfil por defecto) |
| `https://localhost:7062` | HTTPS (perfil `https`) |
| **`http://localhost:5114/swagger`** | **Swagger UI** |
| `http://localhost:5114/health` | Health check (verifica conexión real a PostgreSQL) |

Verifica que todo esté arriba:

```bash
curl http://localhost:5114/health
```

---

## 5. Swagger

La documentación interactiva está en **<http://localhost:5114/swagger>** (o `/swagger` en el perfil HTTPS).

Cada endpoint muestra su seguridad real: los públicos sin candado, los de solo autenticación con `Bearer`, y los de escritura con **`Bearer` + `ApiKey`**.

### 5.1 Crear el primer administrador

Sólo funciona con la **base de datos vacía** (si ya hay personas responde `403 bootstrap.disabled`):

```bash
curl -X POST http://localhost:5114/api/bootstrap/admin
```

La respuesta trae `login`, `passwordGenerada` y `apiKey` — **guárdalos**, la contraseña sólo se devuelve una vez.

### 5.2 Autenticarse

```bash
curl -X POST http://localhost:5114/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"login":"AD1000000000","password":"Admin123!"}'
```

Devuelve el access token (JWT), el refresh token y el APIKey.

### 5.3 Usarlo en Swagger

1. **Authorize** (botón arriba a la derecha).
2. Esquema **Bearer**: pega el `accessToken`.
3. Esquema **ApiKey**: pega el `apiKey` (se envía como header `X-Api-Key`).
4. Ejecuta cualquier endpoint.

### 5.4 Ejemplo con `curl`

```bash
TOKEN="<accessToken>"
APIKEY="<apiKey>"

curl http://localhost:5114/api/vehiculos/placa/ABC123 \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Api-Key: $APIKEY"
```

---

## 6. Endpoints principales

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| `GET` | `/health` | público | Estado de la API y la base de datos |
| `POST` | `/api/bootstrap/admin` | público | Crea el primer administrador (base vacía) |
| `POST` | `/api/auth/login` | público | Inicia sesión (JWT + refresh + APIKey) |
| `POST` | `/api/auth/refresh` | público | Rota el refresh token |
| `POST` | `/api/auth/logout` | JWT | Cierra sesión |
| `POST` | `/api/auth/change-password` | JWT + APIKey | Cambia la contraseña e invalida las sesiones |
| `GET` | `/api/users/me` | JWT | Usuario autenticado |
| `GET`/`POST`/`PUT` | `/api/personas`, `/api/personas/{id}` | JWT + APIKey | Personas (el administrativo genera su usuario) |
| `GET`/`POST`/`PUT`/`DELETE` | `/api/vehiculos`, `/api/vehiculos/{id}` | JWT + APIKey | Vehículos y sus documentos |
| `GET` | `/api/vehiculos/placa/{placa}` | JWT + APIKey | Vehículo por placa con conductores y documentos |
| `GET`/`POST`/`PUT`/`DELETE` | `/api/tipos-documento`, `/api/tipos-documento/{id}` | JWT + APIKey | Catálogo de documentos paramétricos |
| `POST`/`PUT` | `/api/conductores/vehiculos`, `/api/conductores/estado` | JWT + APIKey | Asociación conductor–vehículo |
| `GET` | `/api/rutas`, `/api/rutas/{codigoRuta}` | JWT | Consulta de rutas con filtros y de una ruta con sus paradas ordenadas |
| `GET` | `/api/rutas/conductor/{numero}`, `/api/rutas/placa/{placa}` | JWT | Solo códigos del conductor, y código + conductor del vehículo |
| `POST`/`PUT`/`DELETE` | `/api/rutas`, `/api/rutas/{codigoRuta}` | JWT + APIKey | Rutas: alta con parada inicial y final, reasignación y baja en cascada |
| `GET` | `/api/rutas/{codigoRuta}/paradas` | JWT | Paradas ordenadas de una ruta (orden 0–6, máximo 7) |
| `POST` | `/api/rutas/{codigoRuta}/paradas` | JWT + APIKey | Agrega una parada intermedia (orden 2–6) |
| `GET` | `/api/paradas/{id}` | JWT | Detalle de una parada |
| `PUT`/`DELETE` | `/api/paradas/{id}` | JWT + APIKey | Edición y baja de una parada |
| `GET`/`PUT`/`DELETE` | `/api/personas/{id}/licencia` | JWT + APIKey | Licencia de conducción (metadatos, UPSERT en Base64, baja) |
| `POST` | `/api/cargues` | JWT + APIKey | Cargue masivo de trayectos desde Excel `.xlsx` |
| `GET`/`DELETE` | `/api/cargues`, `/api/cargues/{idCargue}` | JWT + APIKey | Resúmenes, filas con observaciones y purga de la staging |
| `GET` | `/api/consultas/documentos-vencidos` | público | Vehículos con documentos vencidos |
| `GET` | `/api/consultas/documentos-por-vencer?dias={n}` | público | Documentos que vencen pronto |
| `GET` | `/api/consultas/conductores-operables?cantidad={n}` | público | Conductores en estado `PO` |
| `GET` | `/api/consultas/personas-por-tipo` | público | Total de personas por tipo |
| `POST` | `/api/mapas/direcciones` | JWT + APIKey | Busca direcciones y devuelve coordenadas (Geoapify) |
| `POST` | `/api/mapas/rutas` | JWT + APIKey | Calcula ruta, distancia, duración y geometría (Geoapify) |

Las credenciales de Geoapify se configuran localmente con `Geoapify__ApiKey` en `.env`; no se deben subir al repositorio. La API devuelve la geometría de la ruta para que un frontend pueda dibujarla. La visualización del mapa base requiere una interfaz frontend, que no forma parte de este repositorio.

Los errores responden `application/problem+json` con `{status, code, message, errors, traceId}`; el `code` es estable (ej. `password.policy`, `refresh.reuse`, `persona.identificacion.conflict`).

---

### Ejemplos de altas en Swagger

En Swagger pulsa **Try it out** y envía JSON válido. Los enums se reciben como
números: `tipoPersona` es `1` para `Administrativo` y `2` para `Conductor`;
`tipoIdentificacion` es `1` para `CedulaCiudadania`. No escribas etiquetas como
`conductor` sin comillas ni texto explicativo dentro del JSON.

Para registrar una persona, autoriza primero con JWT y `X-Api-Key` y usa
`POST /api/personas`:

```json
{
  "tipoIdentificacion": 1,
  "numeroIdentificacion": "9000001234",
  "nombres": "Ana",
  "apellidos": "Conductora Demo",
  "correoElectronico": "ana.conductora.demo@example.com",
  "tipoPersona": 2
}
```

La identificación debe contener solo dígitos, el correo debe ser válido y el
`tipoPersona` no lleva texto adicional. Para crear una persona administrativa,
cambia `tipoPersona` a `1`; la respuesta incluye las credenciales generadas
para su usuario, que se deben guardar de forma segura.

Para crear un vehículo primero registra un tipo de documento con
`POST /api/tipos-documento`, por ejemplo:

```json
{
  "codigo": "DEMO-SOAT",
  "nombre": "SOAT de demostración",
  "tiposVehiculoAplicables": "A",
  "codigoObligatoriedad": "RA",
  "descripcion": "Documento de prueba para automóvil"
}
```

Copia el `id` devuelto y úsalo en `POST /api/vehiculos`. El alta requiere los
datos del vehículo y un documento en Base64:

```json
{
  "placa": "DEM123",
  "tipoVehiculo": 1,
  "tipoServicio": 2,
  "tipoCombustible": 1,
  "capacidadPasajeros": 5,
  "color": "#336699",
  "modelo": 2024,
  "marca": "Demo",
  "linea": "Presentacion",
  "tipoDocumentoId": "PEGA-AQUI-EL-ID-DEL-TIPO-DE-DOCUMENTO",
  "documentoBase64": "JVBERi0xLjQKJUVPRg==",
  "nombreArchivo": "soat.pdf",
  "fechaExpedicion": "2026-01-01T00:00:00Z",
  "fechaVencimiento": "2027-01-01T00:00:00Z"
}
```

Los valores de enums usados arriba son `tipoVehiculo: 1` (`Automovil`),
`tipoServicio: 2` (`Privado`) y `tipoCombustible: 1` (`Gasolina`). La placa de
automóvil debe tener tres letras y tres números; la de motocicleta, tres letras,
dos números y una letra. El color debe usar `#RRGGBB`, las fechas deben ser
válidas y el vencimiento debe ser posterior a la expedición. Cambia la placa,
el código de documento, la identificación y el correo si ya existen en tu base.
Más ejemplos listos para el cliente REST de VS Code están en
[`src/ApiPoo2.WebApi/Demo.http`](src/ApiPoo2.WebApi/Demo.http).

#### Cargar registros locales para una presentación

Con PostgreSQL local y la API ejecutándose en `http://localhost:5114`, ejecuta
desde PowerShell:

```powershell
.\scripts\Seed-DemoData.ps1
```

El script crea un tipo de documento, una persona conductora, un vehículo y su
asociación, usando únicamente endpoints de la API. Si la base está vacía,
inicializa el administrador con los valores `Bootstrap__*` de `.env`; si ya
está inicializada, solicita el login y la contraseña de un administrador local.
No escribe las credenciales en archivos ni permite apuntar a un host remoto.
Cada ejecución agrega datos de demostración nuevos.

## 7. Pruebas

Un proyecto por capa más suites transversales. Solo `IntegrationTests` toca PostgreSQL; el resto es offline (sin Docker):

| Proyecto | Capa | Base de datos |
|---|---|---|
| `ApiPoo2.DomainTests` | Domain | ninguna |
| `ApiPoo2.ApplicationTests` | Application | ninguna (fakes en memoria) |
| `ApiPoo2.InfrastructureTests` | Infrastructure | ninguna |
| `ApiPoo2.WebApiTests` | WebApi | ninguna |
| `ApiPoo2.ArchitectureTests` | grafo + nombres | ninguna |
| `ApiPoo2.IntegrationTests` | app completa | Docker `:5433`, schema aislado por corrida |

Los nombres de test siguen el estándar `Sujeto_Comportamiento[_Detalle]` en español
(ej. `From_RechazaVacio`); `ArchitectureTests` lo verifica automáticamente.

```bash
# 1. Levantar la base de pruebas (con Docker)
docker compose up -d
# o con Podman:
# podman run -d --name api-poo2-test-db -e POSTGRES_DB=api_poo2_test \
#   -e POSTGRES_USER=test_user -e POSTGRES_PASSWORD=test_password \
#   -p 5433:5432 docker.io/library/postgres:16-alpine

# 2. Correr toda la suite
dotnet test ApiPoo2.sln
```

Correr sólo un proyecto o un test puntual:

```bash
dotnet test tests/ApiPoo2.DomainTests
dotnet test tests/ApiPoo2.DomainTests --filter "FullyQualifiedName~UserTests"
```

Las pruebas de integración leen `.env.test` (no `.env`) y **nunca** apuntan a la base de producción.

Las reglas para añadir tests (naming, fakes, aislamiento) y la política de
cobertura están en [`docs/TESTING.md`](docs/TESTING.md).

### Cobertura

`coverlet.msbuild` está disponible en todos los proyectos de test. Ejemplo:

```bash
dotnet test tests/ApiPoo2.DomainTests \
  /p:CollectCoverage=true "/p:Include=[ApiPoo2.Domain]*" \
  /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=total
```

| Capa | Umbral mínimo |
|---|---|
| Domain | 90 % líneas |
| Application | 50 % líneas |
| Infrastructure | 5 % líneas |
| WebApi | 15 % líneas |

`IntegrationTests` queda fuera del gate: cubre la app completa con PostgreSQL y
se exige un test de integración por endpoint, no un porcentaje. El gate con
estos umbrales se corre en local (ver [`docs/TESTING.md`](docs/TESTING.md)); el
repositorio no tiene pipeline de CI configurado.

---

## 8. Estructura del proyecto

```
ApiPoo2.sln
├── .env / .env.example          Configuración (credenciales reales / plantilla)
├── docker-compose.yml           PostgreSQL local para pruebas
├── src/
│   ├── ApiPoo2.Domain/          Entidades, value objects, invariantes (sin dependencias)
│   ├── ApiPoo2.Application/     Casos de uso, DTOs, validadores, puertos (interfaces)
│   ├── ApiPoo2.Infrastructure/  EF Core/Npgsql, repositorios, JWT, hashing, migraciones
│   └── ApiPoo2.WebApi/          Controllers, middleware de errores, Swagger, composition root
└── tests/
    ├── ApiPoo2.DomainTests/
    ├── ApiPoo2.ApplicationTests/
    ├── ApiPoo2.InfrastructureTests/
    ├── ApiPoo2.WebApiTests/
    ├── ApiPoo2.ArchitectureTests/
    └── ApiPoo2.IntegrationTests/
```

---

## 9. Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---|---|---|
| `Jwt__Secret` debe tener al menos 32 caracteres | Secreto corto o ausente en `.env` | Genera uno: `openssl rand -base64 48` |
| Falla al conectar con PostgreSQL | `DATABASE_URL` incorrecta o base inexistente | Verifica la cadena y ejecuta `dotnet ef database update` |
| `403 bootstrap.disabled` | Ya existe al menos una persona | El bootstrap sólo corre una vez, con la base vacía |
| `401 credentials.invalid` / `account.locked` | Login o contraseña mal, o 5 intentos fallidos | El bloqueo dura 15 minutos |
| `403` en endpoints protegidos | Falta el header `X-Api-Key` o no corresponde al usuario del JWT | Autoriza ambos esquemas en Swagger |
| El navegador bloquea las llamadas | `Cors__AllowedOrigins` vacío o sin el origen del front | Agrega el origen separado por coma |
| La UI de Swagger no carga en HTTPS | Certificado de desarrollo no confiable | `dotnet dev-certs https --trust` |

---

## 10. Documentación del proyecto

| Documento | Contenido |
|---|---|
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Manual completo: capas, responsabilidades, esquema de base de datos, códigos de error, reglas de tests |
| [`docs/ADDING_FEATURES.md`](docs/ADDING_FEATURES.md) | Recetas paso a paso para añadir endpoints y features |
| [`docs/TESTING.md`](docs/TESTING.md) | Reglas de tests, naming, integración y política de cobertura |
| [`docs/plantilla-trayectos.xlsx`](docs/plantilla-trayectos.xlsx) | Plantilla del Excel de cargue E4: columnas obligatorias y reglas de validación |
| [`REQUERIMIENTOS.md`](REQUERIMIENTOS.md) | Requerimientos funcionales E1–E4 y criterios de entrega |
| [`AGENTS.md`](AGENTS.md) | Convenciones de trabajo para el equipo |

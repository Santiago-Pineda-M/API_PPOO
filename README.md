# API_PPOO

API REST en **.NET 10** para gestión de **personas, vehículos, documentos paramétricos y autenticación** (JWT de acceso + refresh tokens rotativos con blacklist, y `X-Api-Key` como segunda autorización).

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

> `.env` y `.env.test` están en `.gitignore`. **Nunca los commitees.**

### 3.3 Crear la base de datos y aplicar migraciones

Con la base creada (Neon o un PostgreSQL local), aplica las migraciones:

```bash
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet ef database update --project src/ApiPoo2.Infrastructure
```

Esto crea las tablas: `personas`, `usuarios`, `refresh_tokens`, `vehiculos`, `tipos_documento`, `documentos_vehiculo`, `conductores_vehiculos` y `access_token_blacklist`.

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
| `GET` | `/api/consultas/documentos-vencidos` | público | Vehículos con documentos vencidos |
| `GET` | `/api/consultas/documentos-por-vencer?dias={n}` | público | Documentos que vencen pronto |
| `GET` | `/api/consultas/conductores-operables?cantidad={n}` | público | Conductores en estado `PO` |
| `GET` | `/api/consultas/personas-por-tipo` | público | Total de personas por tipo |

Los errores responden `application/problem+json` con `{status, code, message, errors, traceId}`; el `code` es estable (ej. `password.policy`, `refresh.reuse`, `persona.identificacion.conflict`).

---

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
| Application | 30 % líneas |
| Infrastructure | 5 % líneas |
| WebApi | 15 % líneas |

`IntegrationTests` queda fuera del gate: cubre la app completa con PostgreSQL y
se exige un test de integración por endpoint, no un porcentaje. El CI aplica
estos umbrales y publica los reportes como artefacto.

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
| [`REQUERIMIENTOS.md`](REQUERIMIENTOS.md) | Requerimientos funcionales E1/E2 y criterios de entrega |
| [`AGENTS.md`](AGENTS.md) | Convenciones de trabajo para el equipo |

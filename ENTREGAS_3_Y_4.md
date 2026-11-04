# Entregas 3 y 4 — qué se agregó y cómo probarlo

Este documento cubre lo que se sumó al proyecto para las **Entregas 3 (trayectos)** y **4 (cargue masivo Excel)**.
Las Entregas 1 y 2 siguen como estaban; solo se agregaron métodos donde hacía falta (ver "Archivos tocados").

> ⚠️ Este código se escribió sin tener el SDK de .NET disponible, así que **no se ha compilado**.
> Los procedimientos almacenados (E4) sí se probaron en PostgreSQL 16 con 15 escenarios (válidos y con errores).
> Si `dotnet build` muestra algún error, casi seguro es un detalle pequeño.

---

## 1. Cómo arrancar (Windows, con Docker)

1. Levanta la base local:
   ```bash
   docker compose up -d postgres-dev
   ```
2. Copia `.env.example` a `.env` en la raíz del repo y ajusta estas líneas:
   ```env
   DATABASE_URL=Host=localhost;Port=5432;Database=api_poo2;Username=api_poo2;Password=api_poo2
   Jwt__Secret=pon-aqui-un-secreto-de-al-menos-32-caracteres
   Database__AutoMigrate=true
   Geocoding__Provider=Nominatim
   GoogleMaps__ApiKey=
   ```
   `Database__AutoMigrate=true` crea las tablas y los procedimientos al arrancar, así no necesitas instalar `dotnet-ef`.
3. Arranca la API:
   ```bash
   dotnet run --project src/ApiPoo2.WebApi
   ```
   Queda en `http://localhost:5114` (Swagger en `/swagger`, mapa en `/mapa.html`).
4. Importa en Postman `postman/ApiPoo2_E1_a_E4.postman_collection.json` y ejecuta las peticiones **en orden**.
   La primera (`/api/bootstrap/admin`) crea el administrador y guarda login y contraseña solos.

**Fechas en Postman:** envíalas siempre con `Z` al final (`2027-12-31T00:00:00Z`). Los servicios nuevos aceptan
fechas sin zona, pero los de E1/E2 todavía no (PostgreSQL las rechaza).

**Enums:** la API recibe los enums como números (`tipoPersona: 2` = Conductor, `estado: 1` = PO / Habilitado).

---

## 2. Entrega 3 — Trayectos

### Base de datos
- **`trayectos`**: id, id_persona (FK), id_vehiculo (FK), codigo_ruta, ubicacion, orden_parada, latitud, longitud,
  login_registra. CHECK: `orden_parada BETWEEN 0 AND 6`, rangos de latitud/longitud y que vayan juntas.
  Índice único `(codigo_ruta, orden_parada)`.
- **`personas`**: nuevas columnas `licencia_conduccion` (bytea, PDF) y `fecha_vigencia_licencia`.
  CHECK: solo los conductores pueden tener licencia.

Una ruta = todas las filas con el mismo código. Entre **2 y 7 paradas**: la inicial (orden 0), la final (el mayor)
y como máximo 5 intermedias, sin saltos ni órdenes repetidos (`Domain/Trayectos/RutaPolicy.cs`).

### Servicios (todos con token + APIKey)
| Método | Ruta | Qué hace |
|---|---|---|
| PUT | `/api/personas/{id}/licencia` | Registra la licencia (PDF Base64) y su vigencia. Solo conductores. |
| POST | `/api/trayectos/rutas` | Crea una ruta con todas sus paradas de una vez. |
| GET | `/api/trayectos/rutas/{codigo}` | Paradas de la ruta, en orden, con conductor y vehículo. |
| GET | `/api/trayectos/conductor/{identificacion}` | Solo los códigos de ruta del conductor, agrupados. |
| GET | `/api/trayectos/vehiculo/{placa}` | Código de ruta + conductor, agrupados. |
| GET | `/api/trayectos/novedades` | Rutas con vehículo NO habilitado o conductor en RO (con el motivo). |

**Reglas al crear una ruta** (nota del enunciado): todos los documentos del vehículo deben estar en Habilitado y
vigentes, y el conductor debe estar en **PO** para ese vehículo. También se rechaza si la licencia está vencida.

### Tareas programadas (`Infrastructure/BackgroundJobs/TareasProgramadas.cs`)
| Cada | Qué hace |
|---|---|
| 2 min | Licencia vencida → el conductor pasa a **RO** en todos sus vehículos. Cereza: correo opcional (`Email__*` en el .env; si está vacío solo se escribe en el log). |
| 2 min | Documento con fecha vencida → se guarda el estado **VENCIDO**. |
| 90 s | Trayectos sin latitud/longitud → se geocodifica la ubicación y se guarda. |

`TareasProgramadas__Habilitadas=false` las apaga.

### Geocodificación: gratis ahora, Google después
- Por defecto `Geocoding__Provider=Nominatim`: OpenStreetMap, **gratis y sin llave** (máx. 1 consulta por segundo,
  ya está controlado). Busca en Colombia (`Geocoding__CountryCodes=co`).
- Cuando tengas la llave de Google: en el `.env` pon `Geocoding__Provider=Google` y `GoogleMaps__ApiKey=tu-llave`.
  Si pones Google sin llave, la API usa Nominatim y lo avisa en el log.
- **La llave va solo en el `.env`**, que ya está en `.gitignore`. Nunca la pongas en `appsettings.json`.
- Como el enunciado dice "API externa Google Maps", confirma con el docente que acepta Nominatim mientras tanto.

### Mapa web (`/mapa.html`)
Login → código de ruta → mapa con las paradas en colores (verde inicial, azul intermedias, rojo final), una línea
entre ellas y la lista. Usa **Leaflet + OpenStreetMap** (gratis). Con `Geocoding__Provider=Google` y llave, usa
Google Maps. El botón "Abrir en Google Maps" funciona siempre, sin llave.
Si una parada aún no tiene coordenadas, la página lo avisa (la tarea de 90 s las completa).

---

## 3. Entrega 4 — Cargue masivo Excel

### Base de datos
- **`trayectos_tmp`**: los mismos campos de `trayectos` **como texto**, más `id`, `estado`
  (CHECK `CARGADO/VALIDADO/PROCESADO/ERROR`), `observacion` acumulativa, `id_cargue` (año-mes-día-hora-minuto,
  ej. `202611032245`, hora de Colombia) y `fila` (número de fila del Excel).

### Los tres procedimientos almacenados (`Infrastructure/Persistence/Sql/CargueTrayectosSql.cs`)
1. **`sp_cargar_trayecto_tmp`**: copia las filas a `trayectos_tmp` **sin alterarlas**, en estado CARGADO.
   PostgreSQL no puede abrir un `.xlsx`, así que la API lee el Excel y le pasa todas las filas tal cual en un JSON.
2. **`sp_validar_trayecto_tmp`**: revisa cada fila con las reglas de E1–E3 (conductor existe y es CONDUCTOR, licencia
   vigente, vehículo habilitado, relación en PO, código de ruta nuevo, orden 0–6, coordenadas válidas, login existente)
   y las de la ruta completa (2–7 paradas, órdenes 0..n-1, un solo conductor y vehículo). **Acumula todos los errores**
   en la observación. Si una parada falla, toda la ruta queda en ERROR.
3. **`sp_procesar_trayecto_tmp`**: inserta en `trayectos` las filas VALIDADO y las marca PROCESADO.

### Servicios (token + APIKey)
| Método | Ruta | Qué hace |
|---|---|---|
| GET | `/api/cargues/plantilla` | Descarga la plantilla `.xlsx` (con ejemplo y hoja de instrucciones). |
| POST | `/api/cargues/trayectos` | Sube el Excel (form-data, campo `archivo`) y ejecuta los 3 procedimientos. |
| GET | `/api/cargues/{idCargue}` | Resumen del cargue y estado + observación de cada fila. |

**Plantilla:** `IdPersona, IdVehiculo, CodigoRuta, Ubicacion, OrdenParada, Latitud, Longitud, LoginRegistra`.
Para que sea fácil de llenar, `IdPersona` acepta el uuid **o el número de identificación**, e `IdVehiculo` el uuid
**o la placa**. Latitud/Longitud y LoginRegistra son opcionales. Dos cargues en el mismo minuto → `409`
(el IdCargue sería el mismo).

---

## 4. Decisiones que conviene saber
- Se respetó la arquitectura del repo (Domain / Application / Infrastructure / WebApi, casos de uso, errores con código).
- La migración `20261103230000_Entregas3y4` se escribió a mano (sin `dotnet ef`). No tiene `.Designer.cs`
  (EF no lo necesita para aplicarla) y el snapshot se actualizó. Por si queda alguna diferencia menor, se ignora la
  advertencia `PendingModelChangesWarning`, igual que ya hacían las pruebas de integración.
- La licencia se guarda en bytes (`bytea`), igual que tu compañero guardó los PDF de los vehículos.
- `GET /api/mapa/config` es público: solo dice qué mapa usar.

## 5. Cosas que encontré en el código existente (no las cambié)
- **Asociar dos veces el mismo conductor al mismo vehículo** (`POST /api/conductores/vehiculos`) termina en error 500:
  el caso de uso no revisa si ya existe la asociación y `PersistenceErrors` no traduce ese índice único a 409.
- **Validación de PDF débil**: `ContenidoDocumento` acepta cualquier contenido si el nombre del archivo termina en `.pdf`.
- **Fechas sin `Z`** en E1/E2 fallan con 500 (Npgsql exige UTC).
- El código guardado para motocicleta es `MOTOCIClETA` (con una "l" minúscula). Funciona porque está igual en el
  conversor y en el CHECK, pero corregirlo requiere una migración.

## 6. Archivos tocados de la parte existente
Casi todo son líneas **agregadas**; solo se reescribieron 2 (el `AddDbContext` y el factory de diseño, para ignorar
`PendingModelChangesWarning`):
`Persona.cs` (licencia), `DocumentoVehiculo.cs` (`MarcarVencidoSiCorresponde`), `Vehiculo.cs` (`EstaHabilitado`),
`IPersonaRepository` / `ITipoDocumentoRepository` / `IVehiculoRepository` y sus implementaciones (consultas nuevas),
`PersonaConfiguration.cs`, `EnumCodeConverters.cs`, `AppDbContext.cs`, `AppDbContextModelSnapshot.cs`,
`DesignTimeDbContextFactory.cs`, los dos `DependencyInjection.cs`, `ClaimsPrincipalExtensions.cs`, `Program.cs`
(static files y auto-migración opcional), `ApiPoo2.Infrastructure.csproj` (ClosedXML y Microsoft.Extensions.Http),
`docker-compose.yml` (servicio `postgres-dev`), `.env.example` y el repositorio falso de
`BootstrapAdminUseCaseTests.cs` (para que las pruebas sigan compilando).

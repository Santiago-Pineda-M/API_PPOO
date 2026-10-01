# Requerimientos funcionales — Proyecto PPOO

Fuentes normativas:

- `documentos requerimientos/Proyecto E1 V2.pdf`
- `documentos requerimientos/Proyecto E2 V2.pdf`

Adaptación tecnológica: los PDF mencionan Spring Boot, Java, JPA, Maven y MySQL. Este repositorio usa **.NET 10, EF Core y PostgreSQL**. Los requerimientos funcionales se aplican igual; la tecnología se traduce así:

- JPA/entidades → entidades de dominio + configuraciones EF Core.
- Restricciones/checks → validación en dominio + `CHECK` en PostgreSQL.
- `BLOB` → `bytea`.
- PDF en Base64 → la API recibe/devuelve Base64, pero la base guarda bytes binarios.
- Token + APIKey → JWT para autenticación y `X-Api-Key` como segunda autorización.

## Convenciones

- Cada feature tiene un ID estable, por ejemplo `F-E1-VEH-01`.
- “Criterios de entrega” significa qué debe demostrarse para considerar completa la feature.
- Los endpoints indicados son el contrato objetivo del proyecto.
- `ADMINISTRATIVO` es el único tipo de persona con usuario; `ADMINISTRADOR` se interpreta como autorización/rol, no como otro tipo de persona.

## Mapa incremental

1. Base de seguridad: JWT + APIKey.
2. Personas y usuarios.
3. Vehículos y documentos paramétricos.
4. Relaciones vehículo-documento y conductor-vehículo.
5. Consultas públicas.
6. Evidencia de entrega: migraciones, pruebas, Postman y datos limpios.

---

# E1 — Vehículos

## F-E1-VEH-01 — Crear vehículo con documento asociado

Alcance:

- Crear un vehículo y, en la misma operación, asociar al menos un documento.
- El documento asociado nace en `EN_VERIFICACION`.

Entradas:

- `placa`
- `tipoVehiculo`: `Automovil` o `Motocicleta`
- `tipoServicio`: `Publico` o `Privado`
- `tipoCombustible`: `Gasolina`, `Gas` o `Disel`
- `capacidadPasajeros`: entero, mayor o igual que cero
- `color`: hexadecimal `#RRGGBB`
- `modelo`: entero positivo
- `marca`
- `linea`
- Documento inicial:
  - `tipoDocumentoId`
  - PDF en Base64
  - `nombreArchivo`
  - `fechaExpedicion`
  - `fechaVencimiento`

Reglas:

- La placa debe tener exactamente seis caracteres.
- Automóvil: tres letras + tres números, por ejemplo `ABC123`.
- Motocicleta: tres letras + dos números + una letra, por ejemplo `ABC12D`.
- La placa es única.
- `fechaVencimiento` debe ser posterior a `fechaExpedicion`.
- El documento debe aplicar al tipo del vehículo.
- El contenido debe ser un PDF válido y no vacío.

Criterios de entrega:

- `POST /api/vehiculos` responde `201`.
- Vehículo y documento se guardan en una sola transacción.
- Sin documento asociado, la creación falla.
- El documento queda en `EN_VERIFICACION`.

## F-E1-VEH-02 — Consultar vehículo por placa

Contrato:

- `GET /api/vehiculos/placa/{placa}`

Criterios de entrega:

- Devuelve el vehículo, sus conductores asociados y sus documentos.
- No incluye el binario del PDF; solo metadatos y tamaño.
- Si no existe, responde `404`.

## F-E1-VEH-03 — Buscar vehículos por tipo

Contrato:

- `GET /api/vehiculos/tipo/{tipoVehiculo}` (`Automovil` o `Motocicleta`)

Criterios de entrega:

- Solo devuelve vehículos del tipo solicitado.
- Endpoint protegido con token + APIKey.

## F-E1-VEH-04 — Buscar vehículos por tipo de documento común

Contrato:

- `GET /api/vehiculos/tipo-documento/{codigo}`

Criterios de entrega:

- Devuelve vehículos que tengan asociado el mismo documento paramétrico.
- Si el código no existe, responde lista vacía.
- Endpoint protegido con token + APIKey.

## F-E1-VEH-05 — Buscar vehículos por estado de documento

Contrato:

- `GET /api/vehiculos/estado-documento/{estado}` (`Habilitado`, `Vencido` o `EnVerificacion`)

Criterios de entrega:

- `Vencido` se deriva por fecha, aunque el estado almacenado sea otro.
- Endpoint protegido con token + APIKey.

## F-E1-VEH-05b — Consultar vehículo por id

Contrato:

- `GET /api/vehiculos/{id}`

Criterios de entrega:

- Devuelve el vehículo con sus conductores asociados y sus documentos (metadatos, no el binario).
- Si no existe, responde `404`.
- Endpoint protegido con token + APIKey (la versión pública por placa es `F-E2-PUB-03`).

## F-E1-VEH-06 — Actualizar vehículo

Contrato:

- `PUT /api/vehiculos/{id}`

Criterios de entrega:

- Permite actualizar los campos del vehículo.
- Valida formato de placa según el tipo.
- No permite cambiar el tipo si ya tiene documentos asociados.
- Si no existe, responde `404`.
- Protegido con token + APIKey.

## F-E1-VEH-07 — Eliminar vehículo

Contrato:

- `DELETE /api/vehiculos/{id}`

Criterios de entrega:

- Elimina el vehículo y sus relaciones dependientes.
- No elimina el catálogo paramétrico de documentos.
- Si no existe, responde `404`.
- Protegido con token + APIKey.

## F-E1-VEH-08 — Agregar documentos a un vehículo

Contrato:

- `POST /api/vehiculos/{vehiculoId}/documentos`

Criterios de entrega:

- Acepta uno o varios documentos en la misma llamada.
- Si el documento ya estaba asociado, actualiza su archivo y vuelve a `EN_VERIFICACION`.
- Protegido con token + APIKey.

## F-E1-VEH-09 — Cambiar estado de un documento asociado

Contrato:

- `PUT /api/vehiculos/{vehiculoId}/documentos/{tipoDocumentoId}/estado` con `{ estado }`

Criterios de entrega:

- Permite pasar a `Habilitado` o volver a `EnVerificacion`.
- `Vencido` no se asigna manualmente: siempre se deriva de la fecha de vencimiento.
- Si la asociación no existe, responde `404`.
- Protegido con token + APIKey.

---

# E1 — Documentos paramétricos

## F-E1-DOC-01 — Crear documento paramétrico

Contrato:

- `POST /api/tipos-documento`

Entradas:

- `codigo`
- `nombre`
- `tiposVehiculoAplicables`: `A`, `M` o `AM`
- `codigoObligatoriedad`: `RA`, `RM` o `RR`
- `descripcion`

Criterios de entrega:

- El código es único.
- Valores fuera de `A/M/AM` o `RA/RM/RR` son rechazados.
- Protegido con token + APIKey.

## F-E1-DOC-02 — Consultar documentos paramétricos

Contrato:

- `GET /api/tipos-documento`
- `GET /api/tipos-documento/{id}`

Criterios de entrega:

- Devuelve catálogo sin contenido binario.
- Protegido con token + APIKey.

## F-E1-DOC-03 — Actualizar documento paramétrico

Contrato:

- `PUT /api/tipos-documento/{id}`

Criterios de entrega:

- No cambia el código una vez creado.
- Valida `A/M/AM` y `RA/RM/RR`.
- Protegido con token + APIKey.

## F-E1-DOC-04 — Eliminar documento paramétrico

Contrato:

- `DELETE /api/tipos-documento/{id}`

Criterios de entrega:

- No permite eliminar un tipo con archivos asociados.
- Protegido con token + APIKey.

---

# E2 — Personas

## F-E2-PER-01 — Crear persona

Contrato:

- `POST /api/personas`

Entradas:

- `tipoIdentificacion`
- `numeroIdentificacion`
- `nombres`
- `apellidos`
- `correoElectronico`
- `tipoPersona`: `Administrativo` o `Conductor`

Reglas:

- El número de identificación debe ser único y numérico.
- El correo debe tener formato válido y ser único.
- Si la persona es `Administrativo`, el sistema crea su usuario automáticamente.
- Si es `Conductor`, no crea usuario.

Criterios de entrega:

- Responde `201`.
- Para administrativo, devuelve `login`, `apiKey` y contraseña temporal generada.
- Protegido con token + APIKey.

## F-E2-PER-02 — Consultar persona

Contrato:

- `GET /api/personas/{id}`

Criterios de entrega:

- Devuelve los datos de la persona y su login si tiene usuario.
- No expone contraseña ni hash.
- Protegido con token + APIKey.

## F-E2-PER-03 — Actualizar persona

Contrato:

- `PUT /api/personas/{id}`

Criterios de entrega:

- Permite actualizar nombres, apellidos y correo.
- El número de identificación no cambia porque alimenta el login mnemotécnico.
- No permite quitar `Administrativo` si ya tiene usuario.
- Protegido con token + APIKey.

---

# E2 — Usuarios

## F-E2-USR-01 — Generación automática del usuario

Reglas:

- Solo personas `Administrativo`.
- Relación uno a uno entre persona y usuario.
- Primary key compuesta: `id_persona` + `login`.
- Login mnemotécnico: primera letra del nombre + primera letra del apellido + número de identificación.
- Colisiones entre homónimos: sufijo incremental documentado.
- Contraseña temporal y APIKey generadas automáticamente.
- Solo se almacena el hash de la contraseña.

Criterios de entrega:

- Crear persona administrativa siempre produce exactamente un usuario.
- Crear persona conductora nunca produce usuario.
- La APIKey generada no está vacía y es única.

## F-E2-USR-02 — Cambiar contraseña

Contrato:

- `PUT /api/usuarios/{login}/password`
- Nueva contraseña en el body.
- Login en la URL.

Criterios de entrega:

- Cambia la contraseña del usuario indicado.
- Invalida sesiones activas según la política de seguridad.
- Protegido con token + APIKey.

## F-E2-USR-03 — Regenerar APIKey

Contrato:

- `GET /api/usuarios/{login}/apikey`

Criterios de entrega:

- Genera una APIKey nueva y la devuelve.
- La anterior deja de ser válida.
- Protegido con token + APIKey.

## F-E2-USR-04 — Autenticación con login mnemotécnico

Contrato:

- `POST /api/auth/login`

Criterios de entrega:

- Acepta el login mnemotécnico y la contraseña.
- Devuelve JWT, refresh token y APIKey.
- Mantiene bloqueo por intentos fallidos y revocación por reuso de refresh.

---

## F-E2-USR-05 — Arranque del primer administrador (bootstrap)

Contrato:

- `POST /api/bootstrap/admin` (público, sin token)

Reglas:

- Lee los datos desde la configuración (`Bootstrap__TipoIdentificacion`, `Bootstrap__NumeroIdentificacion`, `Bootstrap__Nombres`, `Bootstrap__Apellidos`, `Bootstrap__CorreoElectronico`).
- Crea siempre una persona `Administrativo` con su usuario (login mnemotécnico, contraseña y APIKey autogeneradas).
- **Solo funciona con la base vacía**: si ya existe una persona, responde `403 bootstrap.disabled`.

Criterios de entrega:

- Con base vacía responde `201` con `login`, `passwordGenerada` y `apiKey` para iniciar sesión.
- Con sistema inicializado responde `403` sin crear nada.
- La contraseña temporal solo se devuelve en esa respuesta; nunca se almacena en claro.

---

# E2 — Relaciones

## F-E2-REL-01 — Asociar conductor a vehículos

Contrato:

- `POST /api/conductores/vehiculos`

Reglas:

- Solo personas tipo `Conductor`.
- Registra `fechaAsociacion`.
- Estado inicial: `EA`.
- No permite duplicar la misma pareja persona-vehículo.

Criterios de entrega:

- Acepta uno o varios vehículos.
- Protegido con token + APIKey.

## F-E2-REL-02 — Cambiar estado del conductor

Contrato:

- `PUT /api/conductores/estado`

Valores:

- `PO`: Puede Operar
- `EA`: Espera de Aprobación
- `RO`: Restringido para Operar

Criterios de entrega:

- Solo cambia el estado de una asociación existente.
- Protegido con token + APIKey.

## F-E2-REL-03 — Contenido PDF en la relación vehículo-documento

Reglas:

- La API recibe PDF en Base64.
- La base guarda bytes binarios en `bytea`.
- Base64 inválido, vacío o no PDF es rechazado.
- Al reemplazar un documento, vuelve a `EN_VERIFICACION`.

Criterios de entrega:

- El binario almacenado corresponde exactamente al PDF enviado.
- La consulta pública no expone el binario completo.

---

# Consultas públicas

Estas consultas no requieren token ni APIKey.

## F-E2-PUB-01 — Vehículos con documentos vencidos

Contrato:

- `GET /api/consultas/documentos-vencidos`

## F-E2-PUB-02 — Conductores que pueden operar

Contrato:

- `GET /api/consultas/conductores-operables?cantidad={n}`

Criterio:

- Solo incluye asociaciones en estado `PO`.

## F-E2-PUB-03 — Vehículo por placa con conductores y documentos

Contrato:

- `GET /api/vehiculos/placa/{placa}`

## F-E2-PUB-04 — Documentos por vencer

Contrato:

- `GET /api/consultas/documentos-por-vencer?dias={n}`

Criterios:

- `dias` debe ser mayor que cero.
- Solo incluye documentos con vencimiento futuro dentro del rango.
- Ordenados por fecha de vencimiento.

## F-E2-PUB-05 — Total de personas por tipo

Contrato:

- `GET /api/consultas/personas-por-tipo`

Criterio:

- Incluye todos los tipos, incluso con cero personas.

---

# Seguridad transversal

## F-SEC-01 — JWT a la par con APIKey

Modelo:

- JWT autentica la identidad y expira.
- `X-Api-Key` autoriza la operación sensible.
- La APIKey debe pertenecer al mismo usuario autenticado por el JWT.

Criterios de entrega:

- Servicio protegido sin JWT válido: `401`.
- Servicio protegido con JWT válido pero sin APIKey válida: `403`.
- Endpoints públicos responden sin token ni APIKey.
- Todos los servicios de escritura de E1/E2 exigen ambas credenciales.

## F-SEC-02 — Respuestas y secretos

Criterios:

- Nunca devolver hashes, secretos ni binarios innecesarios.
- La contraseña temporal y la APIKey solo se devuelven en creación, login o regeneración.
- Errores con código estable y mensaje en español.

---

# Base de datos transversal

Tablas:

- `personas`
- `usuarios`
- `refresh_tokens`
- `vehiculos`
- `tipos_documento`
- `documentos_vehiculo`
- `conductores_vehiculos`
- `access_token_blacklist`

Restricciones mínimas:

- `personas`: tipo de identificación, tipo de persona, número numérico, correo con formato y único.
- `usuarios`: PK compuesta `id_persona + login`, APIKey única.
- `vehiculos`: placa única y con formato según tipo; tipo, servicio, combustible, capacidad, modelo y color validados.
- `tipos_documento`: tipos aplicables `A/M/AM`; obligatoriedad `RA/RM/RR`.
- `documentos_vehiculo`: estado válido y contenido no vacío.
- `conductores_vehiculos`: estado `PO/EA/RO` y pareja persona-vehículo única.

Invariantes que la base no puede imponer sola:

- Solo `CONDUCTOR` puede asociarse a vehículos.
- Un vehículo no se crea sin documento asociado.
- Una persona `Administrativo` siempre termina con usuario.

Estas tres se garantizan en dominio, casos de uso y pruebas.

---

# Operación y documentación

## F-OPS-01 — CORS configurable

- Política `Frontend` con orígenes por `Cors__AllowedOrigins` (separados por coma).
- Sin configurar, el navegador bloquea el cross-origin; Postman/curl no se ven afectados.

## F-OPS-02 — Health check

- `GET /health` (público): responde `200` si PostgreSQL responde.
- Sin paquetes extra: usa el propio `DbContext` con `SELECT 1`.

## F-OPS-03 — Swagger completo

- Esquemas `Bearer` (JWT) y `ApiKey` (`X-Api-Key`) registrados.
- Cada endpoint muestra su seguridad real: públicos sin candado, auth solo Bearer, escritura Bearer + APIKey.

---

# Criterios globales de entrega

- Migraciones generadas por EF Core, sin SQL manual divergente (una sola `InitialSchema` aplanada).
- `dotnet build` limpio.
- Pruebas en verde: 117 unitarias + 29 aplicación + 13 arquitectura + 18 integración = **177/177**.
- Pruebas de integración contra PostgreSQL local en Docker (`api-poo2-test-db:5433`, `.env.test`), con schema aislado por ejecución; Neon reservado para producción.
- Base de producción sin datos de prueba antes de la entrega.
- Colección Postman con los flujos E1/E2 y los casos públicos/protegidos.

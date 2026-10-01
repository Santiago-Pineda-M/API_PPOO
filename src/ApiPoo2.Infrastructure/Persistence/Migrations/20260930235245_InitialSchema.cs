using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "access_token_blacklist",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    jti = table.Column<Guid>(type: "uuid", nullable: false),
                    idpersona = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_token_blacklist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documentos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipos_vehiculo_aplicables = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    codigo_obligatoriedad = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos", x => x.id);
                    table.CheckConstraint("ck_documentos_codigo_obligatoriedad", "codigo_obligatoriedad IN ('RA','RM','RR')");
                    table.CheckConstraint("ck_documentos_tipos_vehiculo_aplicables", "tipos_vehiculo_aplicables IN ('A','M','AM')");
                });

            migrationBuilder.CreateTable(
                name: "personas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_identificacion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    numero_identificacion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_persona = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personas", x => x.id);
                    table.CheckConstraint("ck_personas_num_identificacion", "numero_identificacion ~ '^[0-9]+$'");
                    table.CheckConstraint("ck_personas_tipo_identificacion", "tipo_identificacion IN ('CC','CE','TI','NIT')");
                    table.CheckConstraint("ck_personas_tipo_persona", "tipo_persona IN ('ADMINISTRATIVO','CONDUCTOR')");
                });

            migrationBuilder.CreateTable(
                name: "vehiculos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo_vehiculo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo_servicio = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    tipo_combustible = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    capacidad_pasajeros = table.Column<int>(type: "integer", nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    modelo = table.Column<int>(type: "integer", nullable: false),
                    marca = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    linea = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculos", x => x.id);
                    table.CheckConstraint("ck_vehiculos_capacidad_pasajeros", "capacidad_pasajeros >= 0");
                    table.CheckConstraint("ck_vehiculos_color", "color ~ '^#[0-9A-Fa-f]{6}$'");
                    table.CheckConstraint("ck_vehiculos_modelo", "modelo > 0");
                    table.CheckConstraint("ck_vehiculos_placa", "(tipo_vehiculo = 'AUTOMOVIL' AND placa ~ '^[A-Z]{3}[0-9]{3}$') OR (tipo_vehiculo = 'MOTOCIClETA' AND placa ~ '^[A-Z]{3}[0-9]{2}[A-Z]$')");
                    table.CheckConstraint("ck_vehiculos_tipo_combustible", "tipo_combustible IN ('GASOLINA','GAS','DISEL')");
                    table.CheckConstraint("ck_vehiculos_tipo_servicio", "tipo_servicio IN ('PUBLICO','PRIVADO')");
                    table.CheckConstraint("ck_vehiculos_tipo_vehiculo", "tipo_vehiculo IN ('AUTOMOVIL','MOTOCIClETA')");
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    idpersona = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash_algorithm = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    api_key = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false),
                    lockout_end_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => new { x.idpersona, x.login });
                    table.ForeignKey(
                        name: "FK_usuarios_personas_idpersona",
                        column: x => x.idpersona,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conductores_vehiculos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idpersona = table.Column<Guid>(type: "uuid", nullable: false),
                    idvehiculo = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha_asociacion = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PersonaId1 = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conductores_vehiculos", x => x.id);
                    table.CheckConstraint("ck_conductores_vehiculos_estado", "estado IN ('PO','EA','RO')");
                    table.ForeignKey(
                        name: "FK_conductores_vehiculos_personas_PersonaId1",
                        column: x => x.PersonaId1,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conductores_vehiculos_personas_idpersona",
                        column: x => x.idpersona,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conductores_vehiculos_vehiculos_VehiculoId1",
                        column: x => x.VehiculoId1,
                        principalTable: "vehiculos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_conductores_vehiculos_vehiculos_idvehiculo",
                        column: x => x.idvehiculo,
                        principalTable: "vehiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehiculos_documentos",
                columns: table => new
                {
                    idvehiculo = table.Column<Guid>(type: "uuid", nullable: false),
                    iddocumento = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_archivo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    fecha_expedicion = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    fecha_vencimiento = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    VehiculoId1 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehiculos_documentos", x => new { x.idvehiculo, x.iddocumento });
                    table.CheckConstraint("ck_vehiculos_documentos_contenido", "octet_length(contenido) > 0");
                    table.CheckConstraint("ck_vehiculos_documentos_estado", "estado IN ('HABILITADO','VENCIDO','EN_VERIFICACION')");
                    table.ForeignKey(
                        name: "FK_vehiculos_documentos_documentos_iddocumento",
                        column: x => x.iddocumento,
                        principalTable: "documentos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_vehiculos_documentos_vehiculos_VehiculoId1",
                        column: x => x.VehiculoId1,
                        principalTable: "vehiculos",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_vehiculos_documentos_vehiculos_idvehiculo",
                        column: x => x.idvehiculo,
                        principalTable: "vehiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idpersona = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    used_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    revoked_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                    UserIdPersona = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_usuarios_UserIdPersona_login",
                        columns: x => new { x.UserIdPersona, x.login },
                        principalTable: "usuarios",
                        principalColumns: new[] { "idpersona", "login" });
                    table.ForeignKey(
                        name: "FK_refresh_tokens_usuarios_idpersona_login",
                        columns: x => new { x.idpersona, x.login },
                        principalTable: "usuarios",
                        principalColumns: new[] { "idpersona", "login" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_token_blacklist_expires_at_utc",
                table: "access_token_blacklist",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_access_token_blacklist_jti",
                table: "access_token_blacklist",
                column: "jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_idpersona_idvehiculo",
                table: "conductores_vehiculos",
                columns: new[] { "idpersona", "idvehiculo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_idvehiculo",
                table: "conductores_vehiculos",
                column: "idvehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_PersonaId1",
                table: "conductores_vehiculos",
                column: "PersonaId1");

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_VehiculoId1",
                table: "conductores_vehiculos",
                column: "VehiculoId1");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_codigo",
                table: "documentos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personas_numero_identificacion",
                table: "personas",
                column: "numero_identificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personas_tipo_persona",
                table: "personas",
                column: "tipo_persona");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_expires_at_utc",
                table: "refresh_tokens",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_idpersona_login",
                table: "refresh_tokens",
                columns: new[] { "idpersona", "login" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserIdPersona_login",
                table: "refresh_tokens",
                columns: new[] { "UserIdPersona", "login" });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_api_key",
                table: "usuarios",
                column: "api_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_idpersona",
                table: "usuarios",
                column: "idpersona",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_idpersona_api_key",
                table: "usuarios",
                columns: new[] { "idpersona", "api_key" });

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_placa",
                table: "vehiculos",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_tipo_vehiculo",
                table: "vehiculos",
                column: "tipo_vehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_documentos_iddocumento",
                table: "vehiculos_documentos",
                column: "iddocumento");

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_documentos_VehiculoId1",
                table: "vehiculos_documentos",
                column: "VehiculoId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_token_blacklist");

            migrationBuilder.DropTable(
                name: "conductores_vehiculos");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "vehiculos_documentos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "documentos");

            migrationBuilder.DropTable(
                name: "vehiculos");

            migrationBuilder.DropTable(
                name: "personas");
        }
    }
}

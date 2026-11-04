using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRutasParadasYLicencias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuarios_login",
                table: "usuarios");

            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_vigencia_licencia",
                table: "personas",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "licencia_conduccion",
                table: "personas",
                type: "bytea",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "rutas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_ruta = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    id_persona = table.Column<Guid>(type: "uuid", nullable: false),
                    id_vehiculo = table.Column<Guid>(type: "uuid", nullable: false),
                    login_usuario = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rutas", x => x.id);
                    table.ForeignKey(
                        name: "FK_rutas_personas_id_persona",
                        column: x => x.id_persona,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rutas_vehiculos_id_vehiculo",
                        column: x => x.id_vehiculo,
                        principalTable: "vehiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "trayectos_tmp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_cargue = table.Column<long>(type: "bigint", nullable: false),
                    codigo_ruta = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    identificacion_conductor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    placa_vehiculo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    orden_parada = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    latitud = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    longitud = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    login_usuario = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    id_persona = table.Column<Guid>(type: "uuid", nullable: true),
                    id_vehiculo = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    observacion = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trayectos_tmp", x => x.id);
                    table.CheckConstraint("ck_trayectos_tmp_estado", "estado IN ('CARGADO','VALIDADO','PROCESADO','ERROR')");
                    table.ForeignKey(
                        name: "FK_trayectos_tmp_personas_id_persona",
                        column: x => x.id_persona,
                        principalTable: "personas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_trayectos_tmp_vehiculos_id_vehiculo",
                        column: x => x.id_vehiculo,
                        principalTable: "vehiculos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "paradas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    id_ruta = table.Column<Guid>(type: "uuid", nullable: false),
                    ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    orden_parada = table.Column<int>(type: "integer", nullable: false),
                    latitud = table.Column<double>(type: "double precision", nullable: true),
                    longitud = table.Column<double>(type: "double precision", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paradas", x => x.id);
                    table.CheckConstraint("ck_paradas_coordenadas", "(latitud IS NULL AND longitud IS NULL) OR (latitud IS NOT NULL AND longitud IS NOT NULL)");
                    table.CheckConstraint("ck_paradas_latitud", "latitud IS NULL OR (latitud BETWEEN -90 AND 90)");
                    table.CheckConstraint("ck_paradas_longitud", "longitud IS NULL OR (longitud BETWEEN -180 AND 180)");
                    table.CheckConstraint("ck_paradas_orden", "orden_parada BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_paradas_rutas_id_ruta",
                        column: x => x.id_ruta,
                        principalTable: "rutas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_login",
                table: "usuarios",
                column: "login",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_personas_licencia_contenido",
                table: "personas",
                sql: "licencia_conduccion IS NULL OR octet_length(licencia_conduccion) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_personas_licencia_par",
                table: "personas",
                sql: "(licencia_conduccion IS NULL) = (fecha_vigencia_licencia IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_personas_licencia_solo_conductor",
                table: "personas",
                sql: "licencia_conduccion IS NULL OR tipo_persona = 'CONDUCTOR'");

            migrationBuilder.CreateIndex(
                name: "IX_paradas_id_ruta",
                table: "paradas",
                column: "id_ruta");

            migrationBuilder.CreateIndex(
                name: "IX_paradas_id_ruta_orden_parada",
                table: "paradas",
                columns: new[] { "id_ruta", "orden_parada" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rutas_codigo_ruta",
                table: "rutas",
                column: "codigo_ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rutas_id_persona",
                table: "rutas",
                column: "id_persona");

            migrationBuilder.CreateIndex(
                name: "IX_rutas_id_vehiculo",
                table: "rutas",
                column: "id_vehiculo");

            migrationBuilder.CreateIndex(
                name: "IX_rutas_login_usuario",
                table: "rutas",
                column: "login_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_trayectos_tmp_id_cargue_estado",
                table: "trayectos_tmp",
                columns: new[] { "id_cargue", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_trayectos_tmp_id_persona",
                table: "trayectos_tmp",
                column: "id_persona");

            migrationBuilder.CreateIndex(
                name: "IX_trayectos_tmp_id_vehiculo",
                table: "trayectos_tmp",
                column: "id_vehiculo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "paradas");

            migrationBuilder.DropTable(
                name: "trayectos_tmp");

            migrationBuilder.DropTable(
                name: "rutas");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_login",
                table: "usuarios");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personas_licencia_contenido",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personas_licencia_par",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personas_licencia_solo_conductor",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "fecha_vigencia_licencia",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "licencia_conduccion",
                table: "personas");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_login",
                table: "usuarios",
                column: "login");
        }
    }
}

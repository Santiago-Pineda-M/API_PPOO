using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizaNombresTablasYColumnas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Soltar FKs reales (sus nombres llevan los nombres viejos).
            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_documentos_documentos_iddocumento",
                table: "vehiculos_documentos");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_documentos_vehiculos_idvehiculo",
                table: "vehiculos_documentos");

            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_personas_idpersona",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_idvehiculo",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_usuarios_idpersona_login",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_personas_idpersona",
                table: "usuarios");

            // 2. Renombrar tablas (preserva los datos).
            migrationBuilder.RenameTable(
                name: "documentos",
                newName: "tipos_documento");

            migrationBuilder.RenameTable(
                name: "vehiculos_documentos",
                newName: "documentos_vehiculo");

            // 3. Renombrar PKs para que acompañen a sus tablas.
            migrationBuilder.DropPrimaryKey(
                name: "PK_documentos",
                table: "tipos_documento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tipos_documento",
                table: "tipos_documento",
                column: "id");

            migrationBuilder.DropPrimaryKey(
                name: "PK_vehiculos_documentos",
                table: "documentos_vehiculo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_documentos_vehiculo",
                table: "documentos_vehiculo",
                columns: new[] { "idvehiculo", "iddocumento" });

            // 4. Renombrar checks explícitos (los cuerpos no cambian: ninguna columna chequeada se renombra).
            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_codigo_obligatoriedad",
                table: "tipos_documento");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_tipos_vehiculo_aplicables",
                table: "tipos_documento");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tipos_documento_codigo_obligatoriedad",
                table: "tipos_documento",
                sql: "codigo_obligatoriedad IN ('RA','RM','RR')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_tipos_documento_tipos_vehiculo_aplicables",
                table: "tipos_documento",
                sql: "tipos_vehiculo_aplicables IN ('A','M','AM')");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vehiculos_documentos_contenido",
                table: "documentos_vehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "ck_vehiculos_documentos_estado",
                table: "documentos_vehiculo");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_vehiculo_contenido",
                table: "documentos_vehiculo",
                sql: "octet_length(contenido) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_vehiculo_estado",
                table: "documentos_vehiculo",
                sql: "estado IN ('HABILITADO','VENCIDO','EN_VERIFICACION')");

            // 5. Renombrar columnas.
            migrationBuilder.RenameColumn(
                name: "rol",
                table: "usuarios",
                newName: "role");

            migrationBuilder.RenameColumn(
                name: "idpersona",
                table: "usuarios",
                newName: "id_persona");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_idpersona_api_key",
                table: "usuarios",
                newName: "IX_usuarios_id_persona_api_key");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_idpersona",
                table: "usuarios",
                newName: "IX_usuarios_id_persona");

            migrationBuilder.RenameColumn(
                name: "idpersona",
                table: "refresh_tokens",
                newName: "id_persona");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_idpersona_login",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_id_persona_login");

            migrationBuilder.RenameColumn(
                name: "idvehiculo",
                table: "conductores_vehiculos",
                newName: "id_vehiculo");

            migrationBuilder.RenameColumn(
                name: "idpersona",
                table: "conductores_vehiculos",
                newName: "id_persona");

            migrationBuilder.RenameIndex(
                name: "IX_conductores_vehiculos_idvehiculo",
                table: "conductores_vehiculos",
                newName: "IX_conductores_vehiculos_id_vehiculo");

            migrationBuilder.RenameIndex(
                name: "IX_conductores_vehiculos_idpersona_idvehiculo",
                table: "conductores_vehiculos",
                newName: "IX_conductores_vehiculos_id_persona_id_vehiculo");

            migrationBuilder.RenameColumn(
                name: "idvehiculo",
                table: "documentos_vehiculo",
                newName: "id_vehiculo");

            migrationBuilder.RenameColumn(
                name: "iddocumento",
                table: "documentos_vehiculo",
                newName: "id_tipo_documento");

            migrationBuilder.RenameIndex(
                name: "IX_vehiculos_documentos_iddocumento",
                table: "documentos_vehiculo",
                newName: "IX_documentos_vehiculo_id_tipo_documento");

            migrationBuilder.RenameColumn(
                name: "jti",
                table: "access_token_blacklist",
                newName: "jwt_id");

            migrationBuilder.RenameColumn(
                name: "idpersona",
                table: "access_token_blacklist",
                newName: "id_persona");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_blacklist_jti",
                table: "access_token_blacklist",
                newName: "IX_access_token_blacklist_jwt_id");

            migrationBuilder.RenameIndex(
                name: "IX_documentos_codigo",
                table: "tipos_documento",
                newName: "IX_tipos_documento_codigo");

            // 6. Re-agregar FKs con nombres normalizados.
            migrationBuilder.AddForeignKey(
                name: "FK_documentos_vehiculo_tipos_documento_id_tipo_documento",
                table: "documentos_vehiculo",
                column: "id_tipo_documento",
                principalTable: "tipos_documento",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_documentos_vehiculo_vehiculos_id_vehiculo",
                table: "documentos_vehiculo",
                column: "id_vehiculo",
                principalTable: "vehiculos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_personas_id_persona",
                table: "conductores_vehiculos",
                column: "id_persona",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_id_vehiculo",
                table: "conductores_vehiculos",
                column: "id_vehiculo",
                principalTable: "vehiculos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_usuarios_id_persona_login",
                table: "refresh_tokens",
                columns: new[] { "id_persona", "login" },
                principalTable: "usuarios",
                principalColumns: new[] { "id_persona", "login" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_personas_id_persona",
                table: "usuarios",
                column: "id_persona",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documentos_vehiculo_tipos_documento_id_tipo_documento",
                table: "documentos_vehiculo");

            migrationBuilder.DropForeignKey(
                name: "FK_documentos_vehiculo_vehiculos_id_vehiculo",
                table: "documentos_vehiculo");

            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_personas_id_persona",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_id_vehiculo",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_usuarios_id_persona_login",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_personas_id_persona",
                table: "usuarios");

            migrationBuilder.RenameIndex(
                name: "IX_tipos_documento_codigo",
                table: "tipos_documento",
                newName: "IX_documentos_codigo");

            migrationBuilder.RenameIndex(
                name: "IX_access_token_blacklist_jwt_id",
                table: "access_token_blacklist",
                newName: "IX_access_token_blacklist_jti");

            migrationBuilder.RenameColumn(
                name: "id_persona",
                table: "access_token_blacklist",
                newName: "idpersona");

            migrationBuilder.RenameColumn(
                name: "jwt_id",
                table: "access_token_blacklist",
                newName: "jti");

            migrationBuilder.RenameIndex(
                name: "IX_documentos_vehiculo_id_tipo_documento",
                table: "documentos_vehiculo",
                newName: "IX_vehiculos_documentos_iddocumento");

            migrationBuilder.RenameColumn(
                name: "id_tipo_documento",
                table: "documentos_vehiculo",
                newName: "iddocumento");

            migrationBuilder.RenameColumn(
                name: "id_vehiculo",
                table: "documentos_vehiculo",
                newName: "idvehiculo");

            migrationBuilder.RenameIndex(
                name: "IX_conductores_vehiculos_id_persona_id_vehiculo",
                table: "conductores_vehiculos",
                newName: "IX_conductores_vehiculos_idpersona_idvehiculo");

            migrationBuilder.RenameIndex(
                name: "IX_conductores_vehiculos_id_vehiculo",
                table: "conductores_vehiculos",
                newName: "IX_conductores_vehiculos_idvehiculo");

            migrationBuilder.RenameColumn(
                name: "id_persona",
                table: "conductores_vehiculos",
                newName: "idpersona");

            migrationBuilder.RenameColumn(
                name: "id_vehiculo",
                table: "conductores_vehiculos",
                newName: "idvehiculo");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_id_persona_login",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_idpersona_login");

            migrationBuilder.RenameColumn(
                name: "id_persona",
                table: "refresh_tokens",
                newName: "idpersona");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_id_persona",
                table: "usuarios",
                newName: "IX_usuarios_idpersona");

            migrationBuilder.RenameIndex(
                name: "IX_usuarios_id_persona_api_key",
                table: "usuarios",
                newName: "IX_usuarios_idpersona_api_key");

            migrationBuilder.RenameColumn(
                name: "id_persona",
                table: "usuarios",
                newName: "idpersona");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "usuarios",
                newName: "rol");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_vehiculo_contenido",
                table: "documentos_vehiculo");

            migrationBuilder.DropCheckConstraint(
                name: "ck_documentos_vehiculo_estado",
                table: "documentos_vehiculo");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehiculos_documentos_contenido",
                table: "documentos_vehiculo",
                sql: "octet_length(contenido) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_vehiculos_documentos_estado",
                table: "documentos_vehiculo",
                sql: "estado IN ('HABILITADO','VENCIDO','EN_VERIFICACION')");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tipos_documento_codigo_obligatoriedad",
                table: "tipos_documento");

            migrationBuilder.DropCheckConstraint(
                name: "ck_tipos_documento_tipos_vehiculo_aplicables",
                table: "tipos_documento");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_codigo_obligatoriedad",
                table: "tipos_documento",
                sql: "codigo_obligatoriedad IN ('RA','RM','RR')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_documentos_tipos_vehiculo_aplicables",
                table: "tipos_documento",
                sql: "tipos_vehiculo_aplicables IN ('A','M','AM')");

            migrationBuilder.DropPrimaryKey(
                name: "PK_documentos_vehiculo",
                table: "documentos_vehiculo");

            migrationBuilder.AddPrimaryKey(
                name: "PK_vehiculos_documentos",
                table: "documentos_vehiculo",
                columns: new[] { "idvehiculo", "iddocumento" });

            migrationBuilder.DropPrimaryKey(
                name: "PK_tipos_documento",
                table: "tipos_documento");

            migrationBuilder.AddPrimaryKey(
                name: "PK_documentos",
                table: "tipos_documento",
                column: "id");

            migrationBuilder.RenameTable(
                name: "documentos_vehiculo",
                newName: "vehiculos_documentos");

            migrationBuilder.RenameTable(
                name: "tipos_documento",
                newName: "documentos");

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_documentos_documentos_iddocumento",
                table: "vehiculos_documentos",
                column: "iddocumento",
                principalTable: "documentos",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_documentos_vehiculos_idvehiculo",
                table: "vehiculos_documentos",
                column: "idvehiculo",
                principalTable: "vehiculos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_personas_idpersona",
                table: "conductores_vehiculos",
                column: "idpersona",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_idvehiculo",
                table: "conductores_vehiculos",
                column: "idvehiculo",
                principalTable: "vehiculos",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_usuarios_idpersona_login",
                table: "refresh_tokens",
                columns: new[] { "idpersona", "login" },
                principalTable: "usuarios",
                principalColumns: new[] { "idpersona", "login" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_personas_idpersona",
                table: "usuarios",
                column: "idpersona",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

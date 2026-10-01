using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_personas_PersonaId1",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_VehiculoId1",
                table: "conductores_vehiculos");

            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_usuarios_UserIdPersona_login",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_vehiculos_documentos_vehiculos_VehiculoId1",
                table: "vehiculos_documentos");

            migrationBuilder.DropIndex(
                name: "IX_vehiculos_documentos_VehiculoId1",
                table: "vehiculos_documentos");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserIdPersona_login",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_conductores_vehiculos_PersonaId1",
                table: "conductores_vehiculos");

            migrationBuilder.DropIndex(
                name: "IX_conductores_vehiculos_VehiculoId1",
                table: "conductores_vehiculos");

            migrationBuilder.DropColumn(
                name: "VehiculoId1",
                table: "vehiculos_documentos");

            migrationBuilder.DropColumn(
                name: "UserIdPersona",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "PersonaId1",
                table: "conductores_vehiculos");

            migrationBuilder.DropColumn(
                name: "VehiculoId1",
                table: "conductores_vehiculos");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_login",
                table: "usuarios",
                column: "login");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_usuarios_login",
                table: "usuarios");

            migrationBuilder.AddColumn<Guid>(
                name: "VehiculoId1",
                table: "vehiculos_documentos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserIdPersona",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PersonaId1",
                table: "conductores_vehiculos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VehiculoId1",
                table: "conductores_vehiculos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehiculos_documentos_VehiculoId1",
                table: "vehiculos_documentos",
                column: "VehiculoId1");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserIdPersona_login",
                table: "refresh_tokens",
                columns: new[] { "UserIdPersona", "login" });

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_PersonaId1",
                table: "conductores_vehiculos",
                column: "PersonaId1");

            migrationBuilder.CreateIndex(
                name: "IX_conductores_vehiculos_VehiculoId1",
                table: "conductores_vehiculos",
                column: "VehiculoId1");

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_personas_PersonaId1",
                table: "conductores_vehiculos",
                column: "PersonaId1",
                principalTable: "personas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conductores_vehiculos_vehiculos_VehiculoId1",
                table: "conductores_vehiculos",
                column: "VehiculoId1",
                principalTable: "vehiculos",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_usuarios_UserIdPersona_login",
                table: "refresh_tokens",
                columns: new[] { "UserIdPersona", "login" },
                principalTable: "usuarios",
                principalColumns: new[] { "idpersona", "login" });

            migrationBuilder.AddForeignKey(
                name: "FK_vehiculos_documentos_vehiculos_VehiculoId1",
                table: "vehiculos_documentos",
                column: "VehiculoId1",
                principalTable: "vehiculos",
                principalColumn: "id");
        }
    }
}

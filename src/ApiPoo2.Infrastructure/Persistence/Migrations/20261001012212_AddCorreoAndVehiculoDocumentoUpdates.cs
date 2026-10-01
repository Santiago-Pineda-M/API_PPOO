using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorreoAndVehiculoDocumentoUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "correo_electronico",
                table: "personas",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_personas_correo_electronico",
                table: "personas",
                column: "correo_electronico",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_personas_correo",
                table: "personas",
                sql: "correo_electronico ~ '^[^@]+@[^@]+\\.[^@]+$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_personas_correo_electronico",
                table: "personas");

            migrationBuilder.DropCheckConstraint(
                name: "ck_personas_correo",
                table: "personas");

            migrationBuilder.DropColumn(
                name: "correo_electronico",
                table: "personas");
        }
    }
}

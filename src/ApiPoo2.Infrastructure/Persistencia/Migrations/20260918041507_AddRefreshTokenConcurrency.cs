using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPoo2.Infrastructure.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin es una columna de sistema que ya existe en toda tabla de PostgreSQL.
            // El token de concurrencia es solo metadata del lado cliente (residente en el snapshot del modelo).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}

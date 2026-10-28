using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaCuscatlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCreditoImagen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagenCredito",
                table: "Destinos",
                type: "TEXT",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagenFuente",
                table: "Destinos",
                type: "TEXT",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagenCredito",
                table: "Destinos");

            migrationBuilder.DropColumn(
                name: "ImagenFuente",
                table: "Destinos");
        }
    }
}

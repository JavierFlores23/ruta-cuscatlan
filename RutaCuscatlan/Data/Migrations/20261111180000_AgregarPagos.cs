using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RutaCuscatlan.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReservaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Proveedor = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Marca = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Ultimos4 = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "Reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_ReservaId",
                table: "Pagos",
                column: "ReservaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");
        }
    }
}

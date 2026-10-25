using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DatosPortadaHorariosCuenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "tb_medio_pago",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Schedules",
                table: "tb_iglesia",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "tb_medio_pago");

            migrationBuilder.DropColumn(
                name: "Schedules",
                table: "tb_iglesia");
        }
    }
}

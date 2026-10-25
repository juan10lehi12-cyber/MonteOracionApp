using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DonacionEventoDestino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "tb_donacion",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_EventId",
                table: "tb_donacion",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_tb_publicacion_EventId",
                table: "tb_donacion",
                column: "EventId",
                principalTable: "tb_publicacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_tb_publicacion_EventId",
                table: "tb_donacion");

            migrationBuilder.DropIndex(
                name: "IX_tb_donacion_EventId",
                table: "tb_donacion");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "tb_donacion");
        }
    }
}

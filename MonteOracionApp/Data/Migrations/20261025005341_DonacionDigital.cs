using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class DonacionDigital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Digital",
                table: "tb_donacion",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Las donaciones registradas desde la web no tienen usuario que las registre.
            migrationBuilder.Sql("UPDATE [tb_donacion] SET [Digital] = 1 WHERE [RecordedByUserId] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Digital",
                table: "tb_donacion");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplificarPanelAlmacenCongregantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dbo.tb_congregante SET Phone = NULL WHERE Phone IS NOT NULL AND Phone NOT LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConfirmedAt",
                table: "tb_lote",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedBy",
                table: "tb_lote",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingActorId",
                table: "tb_lote",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingAt",
                table: "tb_lote",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PendingReview",
                table: "tb_lote",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "tb_congregante",
                type: "nvarchar(9)",
                maxLength: 9,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "tb_congregante",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "tb_congregante",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "tb_lote");

            migrationBuilder.DropColumn(
                name: "ConfirmedBy",
                table: "tb_lote");

            migrationBuilder.DropColumn(
                name: "PendingActorId",
                table: "tb_lote");

            migrationBuilder.DropColumn(
                name: "PendingAt",
                table: "tb_lote");

            migrationBuilder.DropColumn(
                name: "PendingReview",
                table: "tb_lote");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "tb_congregante");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "tb_congregante");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "tb_congregante",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(9)",
                oldMaxLength: 9,
                oldNullable: true);
        }
    }
}

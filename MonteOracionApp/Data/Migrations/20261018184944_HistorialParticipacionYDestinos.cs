using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class HistorialParticipacionYDestinos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryAddress",
                table: "tb_solicitud_bienes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryContact",
                table: "tb_solicitud_bienes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryName",
                table: "tb_solicitud_bienes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "tb_participante_evento",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovalReason",
                table: "tb_participante_evento",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "tb_participante_evento",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
            migrationBuilder.Sql(@"UPDATE r SET BeneficiaryName=b.Name, BeneficiaryAddress=b.Address, BeneficiaryContact=b.Contact FROM tb_solicitud_bienes r INNER JOIN tb_beneficiario b ON b.Id=r.BeneficiaryId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeneficiaryAddress",
                table: "tb_solicitud_bienes");

            migrationBuilder.DropColumn(
                name: "BeneficiaryContact",
                table: "tb_solicitud_bienes");

            migrationBuilder.DropColumn(
                name: "BeneficiaryName",
                table: "tb_solicitud_bienes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "tb_participante_evento");

            migrationBuilder.DropColumn(
                name: "RemovalReason",
                table: "tb_participante_evento");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "tb_participante_evento");
        }
    }
}

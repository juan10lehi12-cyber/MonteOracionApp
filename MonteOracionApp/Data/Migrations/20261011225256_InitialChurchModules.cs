using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialChurchModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChurchSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    About = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChurchSettings", x => x.Id);
                    table.CheckConstraint("CK_ChurchSettings_Singleton", "[Id] = 1");
                });

            migrationBuilder.CreateTable(
                name: "Congregants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Congregants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Congregants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QrStorageKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QrContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Publications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    EventAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackingCode = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DonorKind = table.Column<int>(type: "int", nullable: false),
                    CongregantId = table.Column<int>(type: "int", nullable: true),
                    ExternalName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    ExternalContact = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    PaymentMethodId = table.Column<int>(type: "int", nullable: true),
                    PaymentMethodName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceivedOrPaidOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReceiptStorageKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiptContentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.Id);
                    table.CheckConstraint("CK_Donation_Amount", "([Kind] = 0 AND [Amount] > 0 AND [Amount] IS NOT NULL) OR ([Kind] = 1 AND [Amount] IS NULL)");
                    table.CheckConstraint("CK_Donation_Donor", "([DonorKind] = 0 AND [CongregantId] IS NOT NULL) OR ([DonorKind] = 1 AND [CongregantId] IS NULL AND [ExternalName] IS NOT NULL) OR ([DonorKind] = 2 AND [CongregantId] IS NULL AND [ExternalName] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Donations_Congregants_CongregantId",
                        column: x => x.CongregantId,
                        principalTable: "Congregants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Donations_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonationDecision",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AdministratorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationDecision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonationDecision_AspNetUsers_AdministratorId",
                        column: x => x.AdministratorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonationDecision_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DonationItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationItem", x => x.Id);
                    table.CheckConstraint("CK_DonationItem_Quantity", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_DonationItem_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Congregants_UserId",
                table: "Congregants",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DonationDecision_AdministratorId",
                table: "DonationDecision",
                column: "AdministratorId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationDecision_DonationId",
                table: "DonationDecision",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_DonationItem_DonationId",
                table: "DonationItem",
                column: "DonationId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_CongregantId",
                table: "Donations",
                column: "CongregantId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_PaymentMethodId",
                table: "Donations",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Donations_Status_CreatedAt",
                table: "Donations",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Donations_TrackingCode",
                table: "Donations",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChurchSettings");

            migrationBuilder.DropTable(
                name: "DonationDecision");

            migrationBuilder.DropTable(
                name: "DonationItem");

            migrationBuilder.DropTable(
                name: "Publications");

            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DropTable(
                name: "Congregants");

            migrationBuilder.DropTable(
                name: "PaymentMethods");
        }
    }
}

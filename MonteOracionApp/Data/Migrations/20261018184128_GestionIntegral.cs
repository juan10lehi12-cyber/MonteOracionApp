using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class GestionIntegral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tb_donacion_PaymentMethodId",
                table: "tb_donacion");

            migrationBuilder.AddColumn<DateTime>(
                name: "EventEndsAt",
                table: "tb_publicacion",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageKey",
                table: "tb_publicacion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageType",
                table: "tb_publicacion",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresOn",
                table: "tb_donacion_bien",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "tb_donacion_bien",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageLocation",
                table: "tb_donacion_bien",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundId",
                table: "tb_donacion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OperationNumber",
                table: "tb_donacion",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dni",
                table: "tb_congregante",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tb_auditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_beneficiario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Contact = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_beneficiario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_cierre_caja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    EvidenceKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Day = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Opening = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Income = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Outgoing = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Expected = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Counted = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PreparedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_cierre_caja", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_fondo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_fondo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_participante_evento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicationId = table.Column<int>(type: "int", nullable: false),
                    CongregantId = table.Column<int>(type: "int", nullable: false),
                    Attendance = table.Column<int>(type: "int", nullable: false),
                    InternalNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RecordedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_participante_evento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_participante_evento_tb_congregante_CongregantId",
                        column: x => x.CongregantId,
                        principalTable: "tb_congregante",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_participante_evento_tb_publicacion_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "tb_publicacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_personal",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_personal", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_tb_personal_tb_usuario_UserId",
                        column: x => x.UserId,
                        principalTable: "tb_usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_producto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequiresExpiry = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_producto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tb_solicitud_bienes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BeneficiaryId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NeededOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDueOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_solicitud_bienes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_solicitud_bienes_tb_beneficiario_BeneficiaryId",
                        column: x => x.BeneficiaryId,
                        principalTable: "tb_beneficiario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_solicitud_dinero",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Payee = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PaidBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecisionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SettledOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualSpent = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    EvidenceKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettlementKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettlementType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_solicitud_dinero", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_solicitud_dinero_tb_fondo_FundId",
                        column: x => x.FundId,
                        principalTable: "tb_fondo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_lote",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    DonationItemId = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExpiresOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OnHand = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Reserved = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_lote", x => x.Id);
                    table.CheckConstraint("CK_Lote_Saldos", "[OnHand] >= 0 AND [Reserved] >= 0 AND [Reserved] <= [OnHand]");
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_donacion_bien_DonationItemId",
                        column: x => x.DonationItemId,
                        principalTable: "tb_donacion_bien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_producto_ProductId",
                        column: x => x.ProductId,
                        principalTable: "tb_producto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_movimiento_dinero",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    DonationId = table.Column<int>(type: "int", nullable: true),
                    FinanceRequestId = table.Column<int>(type: "int", nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BusinessDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_movimiento_dinero", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_dinero_tb_donacion_DonationId",
                        column: x => x.DonationId,
                        principalTable: "tb_donacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_dinero_tb_fondo_FundId",
                        column: x => x.FundId,
                        principalTable: "tb_fondo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_dinero_tb_solicitud_dinero_FinanceRequestId",
                        column: x => x.FinanceRequestId,
                        principalTable: "tb_solicitud_dinero",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_solicitud_bien",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockRequestId = table.Column<int>(type: "int", nullable: false),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Delivered = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    Returned = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    WrittenOff = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_solicitud_bien", x => x.Id);
                    table.CheckConstraint("CK_SolicitudBien_Saldos", "[Quantity] > 0 AND [Delivered] >= 0 AND [Delivered] <= [Quantity] AND [Returned] >= 0 AND [WrittenOff] >= 0 AND [Returned] + [WrittenOff] <= [Delivered]");
                    table.ForeignKey(
                        name: "FK_tb_solicitud_bien_tb_lote_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "tb_lote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_solicitud_bien_tb_solicitud_bienes_StockRequestId",
                        column: x => x.StockRequestId,
                        principalTable: "tb_solicitud_bienes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_movimiento_almacen",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StockLotId = table.Column<int>(type: "int", nullable: false),
                    StockRequestLineId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ReservedChange = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Receiver = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    EvidenceKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvidenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BusinessDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_movimiento_almacen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_almacen_tb_lote_StockLotId",
                        column: x => x.StockLotId,
                        principalTable: "tb_lote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_almacen_tb_solicitud_bien_StockRequestLineId",
                        column: x => x.StockRequestLineId,
                        principalTable: "tb_solicitud_bien",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "tb_fondo",
                columns: new[] { "Id", "IsActive", "Name", "Purpose" },
                values: new object[] { 1, true, "General", "Obra y necesidades de la iglesia" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_bien_ProductId",
                table: "tb_donacion_bien",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_FundId",
                table: "tb_donacion",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_PaymentMethodId_ReceivedOrPaidOn_OperationNumber",
                table: "tb_donacion",
                columns: new[] { "PaymentMethodId", "ReceivedOrPaidOn", "OperationNumber" },
                unique: true,
                filter: "[OperationNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_congregante_Dni",
                table: "tb_congregante",
                column: "Dni",
                unique: true,
                filter: "[Dni] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_auditoria_At",
                table: "tb_auditoria",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_tb_cierre_caja_Day_Channel",
                table: "tb_cierre_caja",
                columns: new[] { "Day", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_fondo_Name",
                table: "tb_fondo",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_DonationItemId",
                table: "tb_lote",
                column: "DonationItemId",
                unique: true,
                filter: "[DonationItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_ProductId",
                table: "tb_lote",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_BusinessDate",
                table: "tb_movimiento_almacen",
                column: "BusinessDate");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_CommandId",
                table: "tb_movimiento_almacen",
                column: "CommandId",
                unique: true,
                filter: "[CommandId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_StockLotId",
                table: "tb_movimiento_almacen",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_StockRequestLineId",
                table: "tb_movimiento_almacen",
                column: "StockRequestLineId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_dinero_DonationId_Kind",
                table: "tb_movimiento_dinero",
                columns: new[] { "DonationId", "Kind" },
                unique: true,
                filter: "[DonationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_dinero_FinanceRequestId",
                table: "tb_movimiento_dinero",
                column: "FinanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_dinero_FundId_Channel_BusinessDate",
                table: "tb_movimiento_dinero",
                columns: new[] { "FundId", "Channel", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_participante_evento_CongregantId",
                table: "tb_participante_evento",
                column: "CongregantId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_participante_evento_PublicationId_CongregantId",
                table: "tb_participante_evento",
                columns: new[] { "PublicationId", "CongregantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_producto_Name_Unit",
                table: "tb_producto",
                columns: new[] { "Name", "Unit" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_solicitud_bien_StockLotId",
                table: "tb_solicitud_bien",
                column: "StockLotId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_solicitud_bien_StockRequestId_StockLotId",
                table: "tb_solicitud_bien",
                columns: new[] { "StockRequestId", "StockLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_solicitud_bienes_BeneficiaryId",
                table: "tb_solicitud_bienes",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_tb_solicitud_dinero_FundId_Channel_Status",
                table: "tb_solicitud_dinero",
                columns: new[] { "FundId", "Channel", "Status" });

            // Preserve existing donations; reconcile their confirmed balance into the new ledger.
            // Historical goods require a physical check through the warehouse import screen.
            migrationBuilder.Sql(@"
UPDATE tb_donacion SET FundId = 1;
INSERT INTO tb_personal (UserId, FullName, Position, IsActive)
SELECT DISTINCT u.Id, LEFT(COALESCE(u.Email, u.UserName, u.Id),160), NULL, 1
FROM tb_usuario u INNER JOIN tb_usuario_rol ur ON ur.UserId=u.Id
INNER JOIN tb_rol r ON r.Id=ur.RoleId
WHERE r.Name IN (N'Administrador',N'Tesorero',N'Almacenero');
INSERT INTO tb_movimiento_dinero (FundId,Channel,Kind,Amount,DonationId,FinanceRequestId,ActorId,Detail,BusinessDate,At)
SELECT 1, CASE WHEN PaymentMethodId IS NULL THEN 0 ELSE 1 END, 0, Amount, Id, NULL,
COALESCE(RecordedByUserId,N'migracion'), N'Saldo de donación aprobada anterior a Gestión Integral',
CAST(ReceivedOrPaidOn AS date), CreatedAt FROM tb_donacion WHERE Kind=0 AND Status=1;
INSERT INTO tb_auditoria (ActorId,Action,Reference,Detail,At)
VALUES(N'migracion',N'Actualización de módulos',N'GestionIntegral',N'Se conservaron donaciones, usuarios y congregantes. Completar DNI e incorporar bienes tras conteo físico.',SYSUTCDATETIME());");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_tb_fondo_FundId",
                table: "tb_donacion",
                column: "FundId",
                principalTable: "tb_fondo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_bien_tb_producto_ProductId",
                table: "tb_donacion_bien",
                column: "ProductId",
                principalTable: "tb_producto",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_tb_fondo_FundId",
                table: "tb_donacion");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_bien_tb_producto_ProductId",
                table: "tb_donacion_bien");

            migrationBuilder.DropTable(
                name: "tb_auditoria");

            migrationBuilder.DropTable(
                name: "tb_cierre_caja");

            migrationBuilder.DropTable(
                name: "tb_movimiento_almacen");

            migrationBuilder.DropTable(
                name: "tb_movimiento_dinero");

            migrationBuilder.DropTable(
                name: "tb_participante_evento");

            migrationBuilder.DropTable(
                name: "tb_personal");

            migrationBuilder.DropTable(
                name: "tb_solicitud_bien");

            migrationBuilder.DropTable(
                name: "tb_solicitud_dinero");

            migrationBuilder.DropTable(
                name: "tb_lote");

            migrationBuilder.DropTable(
                name: "tb_solicitud_bienes");

            migrationBuilder.DropTable(
                name: "tb_fondo");

            migrationBuilder.DropTable(
                name: "tb_producto");

            migrationBuilder.DropTable(
                name: "tb_beneficiario");

            migrationBuilder.DropIndex(
                name: "IX_tb_donacion_bien_ProductId",
                table: "tb_donacion_bien");

            migrationBuilder.DropIndex(
                name: "IX_tb_donacion_FundId",
                table: "tb_donacion");

            migrationBuilder.DropIndex(
                name: "IX_tb_donacion_PaymentMethodId_ReceivedOrPaidOn_OperationNumber",
                table: "tb_donacion");

            migrationBuilder.DropIndex(
                name: "IX_tb_congregante_Dni",
                table: "tb_congregante");

            migrationBuilder.DropColumn(
                name: "EventEndsAt",
                table: "tb_publicacion");

            migrationBuilder.DropColumn(
                name: "ImageKey",
                table: "tb_publicacion");

            migrationBuilder.DropColumn(
                name: "ImageType",
                table: "tb_publicacion");

            migrationBuilder.DropColumn(
                name: "ExpiresOn",
                table: "tb_donacion_bien");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "tb_donacion_bien");

            migrationBuilder.DropColumn(
                name: "StorageLocation",
                table: "tb_donacion_bien");

            migrationBuilder.DropColumn(
                name: "FundId",
                table: "tb_donacion");

            migrationBuilder.DropColumn(
                name: "OperationNumber",
                table: "tb_donacion");

            migrationBuilder.DropColumn(
                name: "Dni",
                table: "tb_congregante");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_PaymentMethodId",
                table: "tb_donacion",
                column: "PaymentMethodId");
        }
    }
}

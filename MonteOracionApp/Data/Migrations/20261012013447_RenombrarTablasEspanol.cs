using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarTablasEspanol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_Congregants_AspNetUsers_UserId",
                table: "Congregants");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationDecision_AspNetUsers_AdministratorId",
                table: "DonationDecision");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationDecision_Donations_DonationId",
                table: "DonationDecision");

            migrationBuilder.DropForeignKey(
                name: "FK_DonationItem_Donations_DonationId",
                table: "DonationItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_Congregants_CongregantId",
                table: "Donations");

            migrationBuilder.DropForeignKey(
                name: "FK_Donations_PaymentMethods_PaymentMethodId",
                table: "Donations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Publications",
                table: "Publications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Donations",
                table: "Donations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DonationItem",
                table: "DonationItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DonationDecision",
                table: "DonationDecision");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Congregants",
                table: "Congregants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChurchSettings",
                table: "ChurchSettings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "Publications",
                newName: "tb_publicacion");

            migrationBuilder.RenameTable(
                name: "PaymentMethods",
                newName: "tb_medio_pago");

            migrationBuilder.RenameTable(
                name: "Donations",
                newName: "tb_donacion");

            migrationBuilder.RenameTable(
                name: "DonationItem",
                newName: "tb_donacion_bien");

            migrationBuilder.RenameTable(
                name: "DonationDecision",
                newName: "tb_donacion_historial");

            migrationBuilder.RenameTable(
                name: "Congregants",
                newName: "tb_congregante");

            migrationBuilder.RenameTable(
                name: "ChurchSettings",
                newName: "tb_iglesia");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "tb_usuario_token");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "tb_usuario");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "tb_usuario_rol");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "tb_usuario_acceso_externo");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "tb_usuario_atributo");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "tb_rol");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "tb_rol_atributo");

            migrationBuilder.RenameIndex(
                name: "IX_Donations_TrackingCode",
                table: "tb_donacion",
                newName: "IX_tb_donacion_TrackingCode");

            migrationBuilder.RenameIndex(
                name: "IX_Donations_Status_CreatedAt",
                table: "tb_donacion",
                newName: "IX_tb_donacion_Status_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Donations_PaymentMethodId",
                table: "tb_donacion",
                newName: "IX_tb_donacion_PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_Donations_CongregantId",
                table: "tb_donacion",
                newName: "IX_tb_donacion_CongregantId");

            migrationBuilder.RenameIndex(
                name: "IX_DonationItem_DonationId",
                table: "tb_donacion_bien",
                newName: "IX_tb_donacion_bien_DonationId");

            migrationBuilder.RenameIndex(
                name: "IX_DonationDecision_DonationId",
                table: "tb_donacion_historial",
                newName: "IX_tb_donacion_historial_DonationId");

            migrationBuilder.RenameIndex(
                name: "IX_DonationDecision_AdministratorId",
                table: "tb_donacion_historial",
                newName: "IX_tb_donacion_historial_AdministratorId");

            migrationBuilder.RenameIndex(
                name: "IX_Congregants_UserId",
                table: "tb_congregante",
                newName: "IX_tb_congregante_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "tb_usuario_rol",
                newName: "IX_tb_usuario_rol_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "tb_usuario_acceso_externo",
                newName: "IX_tb_usuario_acceso_externo_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "tb_usuario_atributo",
                newName: "IX_tb_usuario_atributo_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "tb_rol_atributo",
                newName: "IX_tb_rol_atributo_RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_publicacion",
                table: "tb_publicacion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_medio_pago",
                table: "tb_medio_pago",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_donacion",
                table: "tb_donacion",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_donacion_bien",
                table: "tb_donacion_bien",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_donacion_historial",
                table: "tb_donacion_historial",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_congregante",
                table: "tb_congregante",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_iglesia",
                table: "tb_iglesia",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_usuario_token",
                table: "tb_usuario_token",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_usuario",
                table: "tb_usuario",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_usuario_rol",
                table: "tb_usuario_rol",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_usuario_acceso_externo",
                table: "tb_usuario_acceso_externo",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_usuario_atributo",
                table: "tb_usuario_atributo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_rol",
                table: "tb_rol",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tb_rol_atributo",
                table: "tb_rol_atributo",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tb_congregante_tb_usuario_UserId",
                table: "tb_congregante",
                column: "UserId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_tb_congregante_CongregantId",
                table: "tb_donacion",
                column: "CongregantId",
                principalTable: "tb_congregante",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_tb_medio_pago_PaymentMethodId",
                table: "tb_donacion",
                column: "PaymentMethodId",
                principalTable: "tb_medio_pago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_bien_tb_donacion_DonationId",
                table: "tb_donacion_bien",
                column: "DonationId",
                principalTable: "tb_donacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_historial_tb_donacion_DonationId",
                table: "tb_donacion_historial",
                column: "DonationId",
                principalTable: "tb_donacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_donacion_historial_tb_usuario_AdministratorId",
                table: "tb_donacion_historial",
                column: "AdministratorId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_rol_atributo_tb_rol_RoleId",
                table: "tb_rol_atributo",
                column: "RoleId",
                principalTable: "tb_rol",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_usuario_acceso_externo_tb_usuario_UserId",
                table: "tb_usuario_acceso_externo",
                column: "UserId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_usuario_atributo_tb_usuario_UserId",
                table: "tb_usuario_atributo",
                column: "UserId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_usuario_rol_tb_rol_RoleId",
                table: "tb_usuario_rol",
                column: "RoleId",
                principalTable: "tb_rol",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_usuario_rol_tb_usuario_UserId",
                table: "tb_usuario_rol",
                column: "UserId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tb_usuario_token_tb_usuario_UserId",
                table: "tb_usuario_token",
                column: "UserId",
                principalTable: "tb_usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tb_congregante_tb_usuario_UserId",
                table: "tb_congregante");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_tb_congregante_CongregantId",
                table: "tb_donacion");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_tb_medio_pago_PaymentMethodId",
                table: "tb_donacion");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_bien_tb_donacion_DonationId",
                table: "tb_donacion_bien");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_historial_tb_donacion_DonationId",
                table: "tb_donacion_historial");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_donacion_historial_tb_usuario_AdministratorId",
                table: "tb_donacion_historial");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_rol_atributo_tb_rol_RoleId",
                table: "tb_rol_atributo");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_usuario_acceso_externo_tb_usuario_UserId",
                table: "tb_usuario_acceso_externo");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_usuario_atributo_tb_usuario_UserId",
                table: "tb_usuario_atributo");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_usuario_rol_tb_rol_RoleId",
                table: "tb_usuario_rol");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_usuario_rol_tb_usuario_UserId",
                table: "tb_usuario_rol");

            migrationBuilder.DropForeignKey(
                name: "FK_tb_usuario_token_tb_usuario_UserId",
                table: "tb_usuario_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_usuario_token",
                table: "tb_usuario_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_usuario_rol",
                table: "tb_usuario_rol");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_usuario_atributo",
                table: "tb_usuario_atributo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_usuario_acceso_externo",
                table: "tb_usuario_acceso_externo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_usuario",
                table: "tb_usuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_rol_atributo",
                table: "tb_rol_atributo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_rol",
                table: "tb_rol");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_publicacion",
                table: "tb_publicacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_medio_pago",
                table: "tb_medio_pago");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_iglesia",
                table: "tb_iglesia");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_donacion_historial",
                table: "tb_donacion_historial");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_donacion_bien",
                table: "tb_donacion_bien");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_donacion",
                table: "tb_donacion");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tb_congregante",
                table: "tb_congregante");

            migrationBuilder.RenameTable(
                name: "tb_usuario_token",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "tb_usuario_rol",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "tb_usuario_atributo",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "tb_usuario_acceso_externo",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "tb_usuario",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "tb_rol_atributo",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "tb_rol",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "tb_publicacion",
                newName: "Publications");

            migrationBuilder.RenameTable(
                name: "tb_medio_pago",
                newName: "PaymentMethods");

            migrationBuilder.RenameTable(
                name: "tb_iglesia",
                newName: "ChurchSettings");

            migrationBuilder.RenameTable(
                name: "tb_donacion_historial",
                newName: "DonationDecision");

            migrationBuilder.RenameTable(
                name: "tb_donacion_bien",
                newName: "DonationItem");

            migrationBuilder.RenameTable(
                name: "tb_donacion",
                newName: "Donations");

            migrationBuilder.RenameTable(
                name: "tb_congregante",
                newName: "Congregants");

            migrationBuilder.RenameIndex(
                name: "IX_tb_usuario_rol_RoleId",
                table: "AspNetUserRoles",
                newName: "IX_AspNetUserRoles_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_usuario_atributo_UserId",
                table: "AspNetUserClaims",
                newName: "IX_AspNetUserClaims_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_usuario_acceso_externo_UserId",
                table: "AspNetUserLogins",
                newName: "IX_AspNetUserLogins_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_rol_atributo_RoleId",
                table: "AspNetRoleClaims",
                newName: "IX_AspNetRoleClaims_RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_historial_DonationId",
                table: "DonationDecision",
                newName: "IX_DonationDecision_DonationId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_historial_AdministratorId",
                table: "DonationDecision",
                newName: "IX_DonationDecision_AdministratorId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_bien_DonationId",
                table: "DonationItem",
                newName: "IX_DonationItem_DonationId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_TrackingCode",
                table: "Donations",
                newName: "IX_Donations_TrackingCode");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_Status_CreatedAt",
                table: "Donations",
                newName: "IX_Donations_Status_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_PaymentMethodId",
                table: "Donations",
                newName: "IX_Donations_PaymentMethodId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_donacion_CongregantId",
                table: "Donations",
                newName: "IX_Donations_CongregantId");

            migrationBuilder.RenameIndex(
                name: "IX_tb_congregante_UserId",
                table: "Congregants",
                newName: "IX_Congregants_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserTokens",
                table: "AspNetUserTokens",
                columns: new[] { "UserId", "LoginProvider", "Name" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserRoles",
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserClaims",
                table: "AspNetUserClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUserLogins",
                table: "AspNetUserLogins",
                columns: new[] { "LoginProvider", "ProviderKey" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetUsers",
                table: "AspNetUsers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoleClaims",
                table: "AspNetRoleClaims",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AspNetRoles",
                table: "AspNetRoles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Publications",
                table: "Publications",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PaymentMethods",
                table: "PaymentMethods",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChurchSettings",
                table: "ChurchSettings",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DonationDecision",
                table: "DonationDecision",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DonationItem",
                table: "DonationItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Donations",
                table: "Donations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Congregants",
                table: "Congregants",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Congregants_AspNetUsers_UserId",
                table: "Congregants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationDecision_AspNetUsers_AdministratorId",
                table: "DonationDecision",
                column: "AdministratorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationDecision_Donations_DonationId",
                table: "DonationDecision",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DonationItem_Donations_DonationId",
                table: "DonationItem",
                column: "DonationId",
                principalTable: "Donations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_Congregants_CongregantId",
                table: "Donations",
                column: "CongregantId",
                principalTable: "Congregants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Donations_PaymentMethods_PaymentMethodId",
                table: "Donations",
                column: "PaymentMethodId",
                principalTable: "PaymentMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

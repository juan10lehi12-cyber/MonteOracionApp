using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_producto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    categoria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    unidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_producto", x => x.id);
                    table.CheckConstraint("CK_tb_producto_nombre", "LEN(LTRIM(RTRIM([nombre]))) >= 2");
                });

            migrationBuilder.CreateTable(
                name: "tb_rol",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    nombre_normalizado = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    sello_concurrencia = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_usuario",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nombre_usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    nombre_usuario_normalizado = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    correo_normalizado = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    correo_confirmado = table.Column<bool>(type: "bit", nullable: false),
                    clave_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sello_seguridad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sello_concurrencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    telefono_confirmado = table.Column<bool>(type: "bit", nullable: false),
                    doble_factor_activo = table.Column<bool>(type: "bit", nullable: false),
                    fin_bloqueo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    bloqueo_activo = table.Column<bool>(type: "bit", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_congregante",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    dni = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    fecha_nacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_congregante", x => x.id);
                    table.CheckConstraint("CK_tb_congregante_dni", "[dni] IS NULL OR [dni] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_tb_congregante_telefono", "[telefono] IS NULL OR [telefono] LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.ForeignKey(
                        name: "FK_tb_congregante_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_iglesia",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    informacion = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    contacto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    horarios = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_iglesia", x => x.id);
                    table.CheckConstraint("CK_ChurchSettings_Singleton", "[id] = 1");
                    table.ForeignKey(
                        name: "FK_tb_iglesia_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_medio_pago",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    instrucciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    numero_cuenta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    clave_qr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tipo_contenido_qr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_medio_pago", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_medio_pago_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_personal",
                columns: table => new
                {
                    id_usuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    nombre_completo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_personal", x => x.id_usuario);
                    table.CheckConstraint("CK_tb_personal_nombre_no_vacio", "LEN(LTRIM(RTRIM([nombre_completo]))) >= 3");
                    table.ForeignKey(
                        name: "FK_tb_personal_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_publicacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    titulo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    contenido = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    fecha_inicio_evento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fecha_fin_evento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    clave_imagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tipo_imagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    lugar = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    meta_recaudacion = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    publicado = table.Column<bool>(type: "bit", nullable: false),
                    cancelado = table.Column<bool>(type: "bit", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_publicacion", x => x.id);
                    table.CheckConstraint("CK_tb_publicacion_fechas", "[fecha_fin_evento] IS NULL OR [fecha_inicio_evento] IS NULL OR [fecha_fin_evento] >= [fecha_inicio_evento]");
                    table.CheckConstraint("CK_tb_publicacion_meta", "[meta_recaudacion] IS NULL OR [meta_recaudacion] > 0");
                    table.CheckConstraint("CK_tb_publicacion_tipo", "[tipo] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_tb_publicacion_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_usuario_rol",
                columns: table => new
                {
                    id_usuario = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    id_rol = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_usuario_rol", x => new { x.id_usuario, x.id_rol });
                    table.ForeignKey(
                        name: "FK_tb_usuario_rol_tb_rol_id_rol",
                        column: x => x.id_rol,
                        principalTable: "tb_rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_usuario_rol_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_mensaje_contacto",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    id_congregante = table.Column<int>(type: "int", nullable: true),
                    nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    motivo = table.Column<int>(type: "int", nullable: false),
                    preferencia = table.Column<int>(type: "int", nullable: false),
                    mensaje = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<int>(type: "int", nullable: false),
                    fecha_atencion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    notas_atencion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_mensaje_contacto", x => x.id);
                    table.CheckConstraint("CK_tb_mensaje_contacto_estado", "[estado] IN (0, 1, 2)");
                    table.CheckConstraint("CK_tb_mensaje_contacto_longitud", "LEN(LTRIM(RTRIM([mensaje]))) >= 10");
                    table.CheckConstraint("CK_tb_mensaje_contacto_motivo", "[motivo] IN (0, 1, 2, 3, 4)");
                    table.CheckConstraint("CK_tb_mensaje_contacto_preferencia", "[preferencia] IN (0, 1, 2)");
                    table.ForeignKey(
                        name: "FK_tb_mensaje_contacto_tb_congregante_id_congregante",
                        column: x => x.id_congregante,
                        principalTable: "tb_congregante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_mensaje_contacto_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_donacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    codigo_seguimiento = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<int>(type: "int", nullable: false),
                    tipo_donante = table.Column<int>(type: "int", nullable: false),
                    id_congregante = table.Column<int>(type: "int", nullable: true),
                    nombre_externo = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    contacto_externo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    monto = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    numero_operacion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    id_evento = table.Column<int>(type: "int", nullable: true),
                    id_medio_pago = table.Column<int>(type: "int", nullable: true),
                    nombre_medio_pago = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    fecha_recepcion_pago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fecha_creacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    clave_comprobante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    tipo_contenido_comprobante = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    digital = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_donacion", x => x.id);
                    table.CheckConstraint("CK_tb_donacion_estado", "[estado] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_tb_donacion_monto", "[monto] IS NULL OR [monto] > 0");
                    table.ForeignKey(
                        name: "FK_tb_donacion_tb_congregante_id_congregante",
                        column: x => x.id_congregante,
                        principalTable: "tb_congregante",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_donacion_tb_medio_pago_id_medio_pago",
                        column: x => x.id_medio_pago,
                        principalTable: "tb_medio_pago",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_donacion_tb_publicacion_id_evento",
                        column: x => x.id_evento,
                        principalTable: "tb_publicacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_donacion_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_donacion_bien",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_donacion = table.Column<int>(type: "int", nullable: false),
                    id_producto = table.Column<int>(type: "int", nullable: true),
                    ubicacion_almacen = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    descripcion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    cantidad = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    unidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_donacion_bien", x => x.id);
                    table.CheckConstraint("CK_DonationItem_Quantity", "[cantidad] > 0");
                    table.ForeignKey(
                        name: "FK_tb_donacion_bien_tb_donacion_id_donacion",
                        column: x => x.id_donacion,
                        principalTable: "tb_donacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_donacion_bien_tb_producto_id_producto",
                        column: x => x.id_producto,
                        principalTable: "tb_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_donacion_historial",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_donacion = table.Column<int>(type: "int", nullable: false),
                    estado = table.Column<int>(type: "int", nullable: false),
                    id_administrador = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_donacion_historial", x => x.id);
                    table.CheckConstraint("CK_tb_donacion_historial_estado", "[estado] IN (0, 1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_tb_donacion_historial_tb_donacion_id_donacion",
                        column: x => x.id_donacion,
                        principalTable: "tb_donacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_donacion_historial_tb_usuario_id_administrador",
                        column: x => x.id_administrador,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_lote",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_producto = table.Column<int>(type: "int", nullable: false),
                    id_donacion_bien = table.Column<int>(type: "int", nullable: true),
                    ubicacion = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    origen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    cantidad_actual = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    revision_pendiente = table.Column<bool>(type: "bit", nullable: false),
                    id_usuario_pendiente = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    fecha_pendiente = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    confirmado_por = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    fecha_confirmacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_lote", x => x.id);
                    table.CheckConstraint("CK_Lote_Saldos", "[cantidad_actual] >= 0");
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_donacion_bien_id_donacion_bien",
                        column: x => x.id_donacion_bien,
                        principalTable: "tb_donacion_bien",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_producto_id_producto",
                        column: x => x.id_producto,
                        principalTable: "tb_producto",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_usuario_confirmado_por",
                        column: x => x.confirmado_por,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_lote_tb_usuario_id_usuario_pendiente",
                        column: x => x.id_usuario_pendiente,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_movimiento_almacen",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_lote = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    id_usuario = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    destino = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    fecha_hora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    fecha_operacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_movimiento_almacen", x => x.id);
                    table.CheckConstraint("CK_tb_movimiento_almacen_cantidad", "[cantidad] <> 0");
                    table.CheckConstraint("CK_tb_movimiento_almacen_tipo", "[tipo] IN (0, 3, 9)");
                    table.ForeignKey(
                        name: "FK_tb_movimiento_almacen_tb_lote_id_lote",
                        column: x => x.id_lote,
                        principalTable: "tb_lote",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_movimiento_almacen_tb_usuario_id_usuario",
                        column: x => x.id_usuario,
                        principalTable: "tb_usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_congregante_dni",
                table: "tb_congregante",
                column: "dni",
                unique: true,
                filter: "[dni] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_congregante_id_usuario",
                table: "tb_congregante",
                column: "id_usuario",
                unique: true,
                filter: "[id_usuario] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_codigo_seguimiento",
                table: "tb_donacion",
                column: "codigo_seguimiento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_estado_fecha",
                table: "tb_donacion",
                columns: new[] { "estado", "fecha_creacion" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_id_congregante",
                table: "tb_donacion",
                column: "id_congregante");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_id_evento",
                table: "tb_donacion",
                column: "id_evento");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_id_usuario",
                table: "tb_donacion",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_operacion_unica",
                table: "tb_donacion",
                columns: new[] { "id_medio_pago", "numero_operacion" },
                unique: true,
                filter: "[numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_bien_id_donacion",
                table: "tb_donacion_bien",
                column: "id_donacion");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_bien_id_producto",
                table: "tb_donacion_bien",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_historial_id_administrador",
                table: "tb_donacion_historial",
                column: "id_administrador");

            migrationBuilder.CreateIndex(
                name: "IX_tb_donacion_historial_id_donacion",
                table: "tb_donacion_historial",
                column: "id_donacion");

            migrationBuilder.CreateIndex(
                name: "IX_tb_iglesia_id_usuario",
                table: "tb_iglesia",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_confirmado_por",
                table: "tb_lote",
                column: "confirmado_por");

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_id_donacion_bien",
                table: "tb_lote",
                column: "id_donacion_bien",
                unique: true,
                filter: "[id_donacion_bien] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_id_producto",
                table: "tb_lote",
                column: "id_producto");

            migrationBuilder.CreateIndex(
                name: "IX_tb_lote_id_usuario_pendiente",
                table: "tb_lote",
                column: "id_usuario_pendiente");

            migrationBuilder.CreateIndex(
                name: "IX_tb_medio_pago_id_usuario",
                table: "tb_medio_pago",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_mensaje_contacto_estado_fecha",
                table: "tb_mensaje_contacto",
                columns: new[] { "estado", "fecha_creacion" });

            migrationBuilder.CreateIndex(
                name: "IX_tb_mensaje_contacto_id_congregante",
                table: "tb_mensaje_contacto",
                column: "id_congregante");

            migrationBuilder.CreateIndex(
                name: "IX_tb_mensaje_contacto_id_usuario",
                table: "tb_mensaje_contacto",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_fecha_operacion",
                table: "tb_movimiento_almacen",
                column: "fecha_operacion");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_id_lote",
                table: "tb_movimiento_almacen",
                column: "id_lote");

            migrationBuilder.CreateIndex(
                name: "IX_tb_movimiento_almacen_id_usuario",
                table: "tb_movimiento_almacen",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_tb_producto_nombre_unidad",
                table: "tb_producto",
                columns: new[] { "nombre", "unidad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_publicacion_id_usuario",
                table: "tb_publicacion",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "tb_rol",
                column: "nombre_normalizado",
                unique: true,
                filter: "[nombre_normalizado] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "tb_usuario",
                column: "correo_normalizado");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "tb_usuario",
                column: "nombre_usuario_normalizado",
                unique: true,
                filter: "[nombre_usuario_normalizado] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tb_usuario_rol_id_rol",
                table: "tb_usuario_rol",
                column: "id_rol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_donacion_historial");

            migrationBuilder.DropTable(
                name: "tb_iglesia");

            migrationBuilder.DropTable(
                name: "tb_mensaje_contacto");

            migrationBuilder.DropTable(
                name: "tb_movimiento_almacen");

            migrationBuilder.DropTable(
                name: "tb_personal");

            migrationBuilder.DropTable(
                name: "tb_usuario_rol");

            migrationBuilder.DropTable(
                name: "tb_lote");

            migrationBuilder.DropTable(
                name: "tb_rol");

            migrationBuilder.DropTable(
                name: "tb_donacion_bien");

            migrationBuilder.DropTable(
                name: "tb_donacion");

            migrationBuilder.DropTable(
                name: "tb_producto");

            migrationBuilder.DropTable(
                name: "tb_congregante");

            migrationBuilder.DropTable(
                name: "tb_medio_pago");

            migrationBuilder.DropTable(
                name: "tb_publicacion");

            migrationBuilder.DropTable(
                name: "tb_usuario");
        }
    }
}

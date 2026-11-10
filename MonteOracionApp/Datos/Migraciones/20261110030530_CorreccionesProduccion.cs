using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonteOracionApp.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class CorreccionesProduccion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La base existente se creó con EnsureCreated y puede tener el índice
            // anterior (medio + fecha + operación). Se reemplaza por una regla
            // más estricta: una operación no puede repetirse para el mismo medio.
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[tb_donacion]
                    WHERE [numero_operacion] IS NOT NULL
                      AND [id_medio_pago] IS NOT NULL
                    GROUP BY [id_medio_pago], [numero_operacion]
                    HAVING COUNT(*) > 1
                )
                    THROW 50050, N'No se puede crear el índice único de operaciones: existen números de operación repetidos para el mismo medio de pago. Corrige esos registros antes de continuar.', 1;

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [object_id] = OBJECT_ID(N'dbo.tb_donacion')
                      AND [name] = N'IX_tb_donacion_operacion_unica'
                )
                    DROP INDEX [IX_tb_donacion_operacion_unica] ON [dbo].[tb_donacion];

                CREATE UNIQUE INDEX [IX_tb_donacion_operacion_unica]
                    ON [dbo].[tb_donacion] ([id_medio_pago], [numero_operacion])
                    WHERE [numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL;
                """);

            // Restricciones que faltaban en bases creadas con versiones anteriores.
            // Se agregan con NOCHECK para no bloquear el arranque por datos históricos;
            // todos los registros nuevos sí quedan validados.
            AgregarCheck(migrationBuilder, "tb_usuario", "CK_tb_usuario_intentos", "[intentos_fallidos] >= 0");
            AgregarCheck(migrationBuilder, "tb_personal", "CK_tb_personal_nombre_no_vacio", "LEN(LTRIM(RTRIM([nombre_completo]))) >= 3");
            AgregarCheck(migrationBuilder, "tb_congregante", "CK_tb_congregante_dni", "[dni] IS NULL OR [dni] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
            AgregarCheck(migrationBuilder, "tb_congregante", "CK_tb_congregante_telefono", "[telefono] IS NULL OR [telefono] LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
            AgregarCheck(migrationBuilder, "tb_mensaje_contacto", "CK_tb_mensaje_contacto_motivo", "[motivo] IN (0, 1, 2, 3, 4)");
            AgregarCheck(migrationBuilder, "tb_mensaje_contacto", "CK_tb_mensaje_contacto_preferencia", "[preferencia] IN (0, 1, 2)");
            AgregarCheck(migrationBuilder, "tb_mensaje_contacto", "CK_tb_mensaje_contacto_estado", "[estado] IN (0, 1, 2)");
            AgregarCheck(migrationBuilder, "tb_mensaje_contacto", "CK_tb_mensaje_contacto_longitud", "LEN(LTRIM(RTRIM([mensaje]))) >= 10");
            AgregarCheck(migrationBuilder, "tb_publicacion", "CK_tb_publicacion_tipo", "[tipo] IN (0, 1)");
            AgregarCheck(migrationBuilder, "tb_publicacion", "CK_tb_publicacion_meta", "[meta_recaudacion] IS NULL OR [meta_recaudacion] > 0");
            AgregarCheck(migrationBuilder, "tb_publicacion", "CK_tb_publicacion_fechas", "[fecha_fin_evento] IS NULL OR [fecha_inicio_evento] IS NULL OR [fecha_fin_evento] >= [fecha_inicio_evento]");
            AgregarCheck(migrationBuilder, "tb_producto", "CK_tb_producto_nombre", "LEN(LTRIM(RTRIM([nombre]))) >= 2");
            AgregarCheck(migrationBuilder, "tb_donacion", "CK_tb_donacion_estado", "[estado] IN (0, 1, 2, 3)");
            AgregarCheck(migrationBuilder, "tb_donacion_historial", "CK_tb_donacion_historial_estado", "[estado] IN (0, 1, 2, 3)");
            AgregarCheck(migrationBuilder, "tb_movimiento_almacen", "CK_tb_movimiento_almacen_cantidad", "[cantidad] <> 0");
            AgregarCheck(migrationBuilder, "tb_movimiento_almacen", "CK_tb_movimiento_almacen_tipo", "[tipo] IN (0, 3, 9)");
        }

        private static void AgregarCheck(MigrationBuilder migrationBuilder, string tabla, string nombre, string expresion)
        {
            migrationBuilder.Sql($"""
                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.check_constraints
                    WHERE [parent_object_id] = OBJECT_ID(N'dbo.{tabla}')
                      AND [name] = N'{nombre}'
                )
                    ALTER TABLE [dbo].[{tabla}] WITH NOCHECK
                        ADD CONSTRAINT [{nombre}] CHECK ({expresion});
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La corrección es de integridad y no se revierte automáticamente para
            // no volver a permitir operaciones duplicadas en producción.
        }
    }
}

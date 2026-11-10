using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Modelos;
using MonteOracionApp.Servicios;
namespace MonteOracionApp.Datos;

public static class ComandosBaseDatos
{
    private const string MigracionInicial = "20261110030506_InitialCreate";

    /// <summary>
    /// Prepara el esquema con migraciones controladas. En bases ya existentes
    /// registra la migración inicial como línea base y aplica solo los cambios
    /// posteriores, sin borrar tablas ni columnas.
    /// </summary>
    public static async Task PrepararBaseDatosAsync(ContextoBaseDatos db, bool permitirCrear)
    {
        if (!db.Database.IsSqlServer())
            throw new InvalidOperationException("El proyecto requiere SQL Server.");

        if (!await db.Database.CanConnectAsync())
        {
            if (!permitirCrear)
                throw new InvalidOperationException("No se pudo conectar a la base de datos. Revisa DefaultConnection o ejecuta 'dotnet run -- --initialize'.");

            await db.Database.MigrateAsync();
            return;
        }

        var tablas = await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS [Value]
            FROM sys.tables
            WHERE [name] IN (
                N'tb_usuario', N'tb_rol', N'tb_usuario_rol', N'tb_personal',
                N'tb_congregante', N'tb_mensaje_contacto', N'tb_iglesia',
                N'tb_medio_pago', N'tb_publicacion', N'tb_producto',
                N'tb_donacion', N'tb_donacion_bien', N'tb_donacion_historial',
                N'tb_lote', N'tb_movimiento_almacen')
            """).SingleAsync();

        if (tablas == 0)
        {
            if (!permitirCrear)
                throw new InvalidOperationException("La base existe pero no tiene el esquema. Ejecuta 'dotnet run -- --initialize'.");

            await db.Database.MigrateAsync();
            return;
        }

        if (tablas < 15)
            throw new InvalidOperationException($"El esquema está incompleto: se encontraron {tablas} de 15 tablas. No se aplicaron cambios automáticos para evitar pérdida de datos.");

        var historial = await db.Database.SqlQueryRaw<int>("""
            SELECT CASE WHEN OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL THEN 0 ELSE 1 END AS [Value]
            """).SingleAsync();

        if (historial == 0)
        {
            await db.Database.ExecuteSqlRawAsync($"""
                IF OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL
                BEGIN
                    CREATE TABLE [__EFMigrationsHistory] (
                        [MigrationId] nvarchar(150) NOT NULL,
                        [ProductVersion] nvarchar(32) NOT NULL,
                        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'{MigracionInicial}')
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                    VALUES (N'{MigracionInicial}', N'10.0.13');
                """);
        }

        var pendientes = await db.Database.GetPendingMigrationsAsync();
        if (pendientes.Any())
            await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Crea o actualiza las vistas y procedimientos académicos. Solo se ejecuta
    /// con --initialize, nunca en el arranque normal de la aplicación.
    /// </summary>
    public static async Task AsegurarObjetosSqlAsync(ContextoBaseDatos db)
    {
        if (!db.Database.IsSqlServer()) return;
        // 2. Asegurar las 5 Vistas en SQL Server
        string[] vistas =
        [
            """
            CREATE OR ALTER VIEW [dbo].[vw_donaciones_detalladas] AS
            SELECT
                d.[id] AS [id_donacion],
                d.[codigo_seguimiento],
                CASE d.[tipo] WHEN 0 THEN N'Dinero' ELSE N'Bienes Físicos' END AS [tipo_donacion],
                CASE d.[estado]
                    WHEN 0 THEN N'Pendiente'
                    WHEN 1 THEN N'Aprobada'
                    WHEN 2 THEN N'Rechazada'
                    ELSE N'Anulada'
                END AS [estado_donacion],
                CASE d.[tipo_donante]
                    WHEN 0 THEN CONCAT(c.[nombres], N' ', c.[apellidos])
                    WHEN 1 THEN ISNULL(d.[nombre_externo], N'Donante Externo')
                    ELSE N'Anónimo'
                END AS [nombre_donante],
                c.[dni] AS [dni_congregante],
                d.[monto],
                ISNULL(mp.[nombre], d.[nombre_medio_pago]) AS [medio_pago],
                d.[numero_operacion],
                ISNULL(p.[titulo], N'Ofrenda / Obra Social General') AS [destino_donacion],
                CASE WHEN d.[digital] = 1 THEN N'Web / Digital' ELSE N'Presencial en Iglesia' END AS [canal_origen],
                d.[fecha_recepcion_pago],
                d.[fecha_creacion],
                per.[nombre_completo] AS [registrado_por]
            FROM [dbo].[tb_donacion] d
            LEFT JOIN [dbo].[tb_congregante] c ON c.[id] = d.[id_congregante]
            LEFT JOIN [dbo].[tb_medio_pago] mp ON mp.[id] = d.[id_medio_pago]
            LEFT JOIN [dbo].[tb_publicacion] p ON p.[id] = d.[id_evento]
            LEFT JOIN [dbo].[tb_personal] per ON per.[id_usuario] = d.[id_usuario];
            """,
            """
            CREATE OR ALTER VIEW [dbo].[vw_stock_general_almacen] AS
            SELECT
                pr.[id] AS [id_producto],
                pr.[categoria],
                pr.[nombre] AS [producto],
                pr.[unidad],
                pr.[activo],
                COUNT(l.[id]) AS [total_lotes_activos],
                ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) AS [stock_disponible],
                ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 1 THEN l.[cantidad_actual] ELSE 0 END), 0) AS [stock_pendiente_revision],
                CASE
                    WHEN ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) = 0 THEN N'SIN STOCK'
                    WHEN ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) <= 5 THEN N'STOCK CRÍTICO'
                    ELSE N'STOCK ÓPTIMO'
                END AS [estado_inventario]
            FROM [dbo].[tb_producto] pr
            LEFT JOIN [dbo].[tb_lote] l ON l.[id_producto] = pr.[id] AND l.[cantidad_actual] > 0
            GROUP BY pr.[id], pr.[categoria], pr.[nombre], pr.[unidad], pr.[activo];
            """,
            """
            CREATE OR ALTER VIEW [dbo].[vw_avance_eventos] AS
            SELECT
                p.[id] AS [id_evento],
                p.[titulo],
                p.[lugar],
                p.[fecha_inicio_evento],
                p.[fecha_fin_evento],
                ISNULL(p.[meta_recaudacion], 0) AS [meta_recaudacion],
                COUNT(d.[id]) AS [cantidad_aportes_aprobados],
                ISNULL(SUM(d.[monto]), 0) AS [monto_recaudado],
                CASE
                    WHEN ISNULL(p.[meta_recaudacion], 0) > 0
                    THEN CAST(ROUND((ISNULL(SUM(d.[monto]), 0) * 100.0) / p.[meta_recaudacion], 2) AS DECIMAL(10,2))
                    ELSE 0
                END AS [porcentaje_avance],
                CASE
                    WHEN p.[cancelado] = 1 THEN N'Cancelado'
                    WHEN ISNULL(p.[fecha_fin_evento], p.[fecha_inicio_evento]) < DATEADD(HOUR, -5, GETUTCDATE()) THEN N'Finalizado'
                    ELSE N'Activo / Próximo'
                END AS [estado_evento]
            FROM [dbo].[tb_publicacion] p
            LEFT JOIN [dbo].[tb_donacion] d
                ON d.[id_evento] = p.[id] AND d.[estado] = 1 AND d.[tipo] = 0
            WHERE p.[tipo] = 0
            GROUP BY p.[id], p.[titulo], p.[lugar], p.[fecha_inicio_evento], p.[fecha_fin_evento], p.[meta_recaudacion], p.[cancelado];
            """,
            """
            CREATE OR ALTER VIEW [dbo].[vw_kardex_movimientos] AS
            SELECT
                m.[id] AS [id_movimiento],
                m.[fecha_operacion],
                m.[fecha_hora],
                CASE m.[tipo]
                    WHEN 0 THEN N'Ingreso por Donación'
                    WHEN 3 THEN N'Salida a Obra Social'
                    WHEN 9 THEN N'Visto Bueno de Almacén'
                    ELSE N'Otro Movimiento'
                END AS [tipo_movimiento],
                pr.[categoria],
                pr.[nombre] AS [producto],
                ABS(m.[cantidad]) AS [cantidad],
                pr.[unidad],
                l.[id] AS [id_lote],
                l.[ubicacion] AS [ubicacion_almacen],
                l.[origen],
                m.[motivo],
                ISNULL(m.[destino], N'Almacén Central') AS [destino],
                ISNULL(per.[nombre_completo], u.[nombre_usuario]) AS [responsable]
            FROM [dbo].[tb_movimiento_almacen] m
            INNER JOIN [dbo].[tb_lote] l ON l.[id] = m.[id_lote]
            INNER JOIN [dbo].[tb_producto] pr ON pr.[id] = l.[id_producto]
            INNER JOIN [dbo].[tb_usuario] u ON u.[id] = m.[id_usuario]
            LEFT JOIN [dbo].[tb_personal] per ON per.[id_usuario] = m.[id_usuario];
            """,
            """
            CREATE OR ALTER VIEW [dbo].[vw_mensajes_atencion] AS
            SELECT
                mc.[id] AS [id_mensaje],
                mc.[fecha_creacion],
                mc.[nombre] AS [remitente],
                CASE WHEN mc.[id_congregante] IS NOT NULL THEN N'Miembro Congregante' ELSE N'Visitante Web' END AS [tipo_remitente],
                c.[dni] AS [dni_miembro],
                mc.[telefono],
                mc.[correo],
                CASE mc.[motivo]
                    WHEN 0 THEN N'Petición de oración'
                    WHEN 1 THEN N'Consejería pastoral'
                    WHEN 2 THEN N'Información de cultos'
                    WHEN 3 THEN N'Servir en ministerio'
                    ELSE N'Otro motivo'
                END AS [motivo_contacto],
                CASE mc.[preferencia]
                    WHEN 0 THEN N'Llamada telefónica'
                    WHEN 1 THEN N'WhatsApp'
                    ELSE N'Indiferente'
                END AS [preferencia_contacto],
                mc.[mensaje],
                CASE mc.[estado]
                    WHEN 0 THEN N'Pendiente'
                    WHEN 1 THEN N'Atendido'
                    ELSE N'Archivado'
                END AS [estado_atencion],
                mc.[fecha_atencion],
                ISNULL(per.[nombre_completo], u.[nombre_usuario]) AS [atendido_por],
                mc.[notas_atencion]
            FROM [dbo].[tb_mensaje_contacto] mc
            LEFT JOIN [dbo].[tb_congregante] c ON c.[id] = mc.[id_congregante]
            LEFT JOIN [dbo].[tb_usuario] u ON u.[id] = mc.[id_usuario]
            LEFT JOIN [dbo].[tb_personal] per ON per.[id_usuario] = mc.[id_usuario];
            """
        ];

        foreach (var v in vistas)
            await db.Database.ExecuteSqlRawAsync(v);

        // 3. Asegurar los 5 Procedimientos Almacenados en SQL Server
        string[] procedimientos =
        [
            """
            CREATE OR ALTER PROCEDURE [dbo].[sp_registrar_donacion_monetaria]
                @tipo_donante       INT,
                @id_congregante     INT = NULL,
                @nombre_externo     NVARCHAR(180) = NULL,
                @contacto_externo   NVARCHAR(150) = NULL,
                @monto              DECIMAL(12,2),
                @numero_operacion   NVARCHAR(80) = NULL,
                @id_evento          INT = NULL,
                @id_medio_pago      INT,
                @id_usuario         NVARCHAR(450) = NULL,
                @digital            BIT = 1,
                @id_donacion_creada INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    IF @monto IS NULL OR @monto <= 0
                        THROW 50001, 'El monto de la donación monetaria debe ser mayor a cero.', 1;

                    IF @numero_operacion IS NOT NULL AND EXISTS (
                        SELECT 1 FROM [dbo].[tb_donacion]
                        WHERE [id_medio_pago] = @id_medio_pago
                          AND [numero_operacion] = @numero_operacion
                    )
                        THROW 50002, 'El número de operación ya fue registrado para ese medio de pago.', 1;

                    DECLARE @nombreMedio NVARCHAR(100) = (SELECT [nombre] FROM [dbo].[tb_medio_pago] WHERE [id] = @id_medio_pago);
                    IF @nombreMedio IS NULL
                        THROW 50003, 'El medio de pago indicado no existe.', 1;

                    BEGIN TRANSACTION;

                    DECLARE @estadoInicial INT = CASE WHEN @digital = 1 THEN 0 ELSE 1 END;

                    INSERT INTO [dbo].[tb_donacion] (
                        [codigo_seguimiento], [tipo], [estado], [tipo_donante],
                        [id_congregante], [nombre_externo], [contacto_externo],
                        [monto], [numero_operacion], [id_evento], [id_medio_pago],
                        [nombre_medio_pago], [fecha_recepcion_pago], [id_usuario], [digital]
                    )
                    VALUES (
                        NEWID(), 0, @estadoInicial, @tipo_donante,
                        @id_congregante, @nombre_externo, @contacto_externo,
                        @monto, @numero_operacion, @id_evento, @id_medio_pago,
                        @nombreMedio, CAST(GETDATE() AS DATE), @id_usuario, @digital
                    );

                    SET @id_donacion_creada = SCOPE_IDENTITY();

                    IF @estadoInicial = 1 AND @id_usuario IS NOT NULL
                    BEGIN
                        INSERT INTO [dbo].[tb_donacion_historial] ([id_donacion], [estado], [id_administrador], [motivo])
                        VALUES (@id_donacion_creada, 1, @id_usuario, N'Aprobación directa por recepción presencial.');
                    END

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[sp_evaluar_donacion]
                @id_donacion      INT,
                @nuevo_estado     INT,
                @id_administrador NVARCHAR(450),
                @motivo           NVARCHAR(1000) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    IF @nuevo_estado NOT IN (1, 2)
                        THROW 50010, 'El estado de evaluación debe ser 1 (Aprobada) o 2 (Rechazada).', 1;

                    DECLARE @estadoActual INT, @tipoDonacion INT;
                    SELECT @estadoActual = [estado], @tipoDonacion = [tipo]
                    FROM [dbo].[tb_donacion]
                    WHERE [id] = @id_donacion;

                    IF @estadoActual IS NULL
                        THROW 50011, 'La donación indicada no existe.', 1;
                    IF @estadoActual <> 0
                        THROW 50012, 'Solo se pueden evaluar donaciones que estén en estado Pendiente.', 1;

                    BEGIN TRANSACTION;

                    UPDATE [dbo].[tb_donacion]
                    SET [estado] = @nuevo_estado
                    WHERE [id] = @id_donacion;

                    INSERT INTO [dbo].[tb_donacion_historial] ([id_donacion], [estado], [id_administrador], [motivo])
                    VALUES (@id_donacion, @nuevo_estado, @id_administrador, ISNULL(@motivo, N'Evaluación registrada por dirección.'));

                    IF @nuevo_estado = 1 AND @tipoDonacion = 1
                    BEGIN
                        INSERT INTO [dbo].[tb_lote] (
                            [id_producto], [id_donacion_bien], [ubicacion], [origen],
                            [cantidad_actual], [revision_pendiente], [id_usuario_pendiente], [fecha_pendiente]
                        )
                        SELECT
                            db.[id_producto],
                            db.[id],
                            ISNULL(db.[ubicacion_almacen], N'Recepción Almacén'),
                            CONCAT(N'Donación #', @id_donacion),
                            db.[cantidad],
                            1,
                            @id_administrador,
                            SYSDATETIMEOFFSET()
                        FROM [dbo].[tb_donacion_bien] db
                        WHERE db.[id_donacion] = @id_donacion
                          AND db.[id_producto] IS NOT NULL
                          AND NOT EXISTS (SELECT 1 FROM [dbo].[tb_lote] l WHERE l.[id_donacion_bien] = db.[id]);
                    END

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[sp_confirmar_ingreso_lote]
                @id_lote              INT,
                @id_usuario_almacen   NVARCHAR(450),
                @ubicacion_definitiva NVARCHAR(160)
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    DECLARE @pendiente BIT, @cantidad DECIMAL(12,2);
                    SELECT @pendiente = [revision_pendiente], @cantidad = [cantidad_actual]
                    FROM [dbo].[tb_lote]
                    WHERE [id] = @id_lote;

                    IF @pendiente IS NULL
                        THROW 50020, 'El lote indicado no existe.', 1;
                    IF @pendiente = 0
                        THROW 50021, 'Este lote ya fue confirmado anteriormente.', 1;

                    BEGIN TRANSACTION;

                    UPDATE [dbo].[tb_lote]
                    SET [revision_pendiente] = 0,
                        [ubicacion]          = LTRIM(RTRIM(@ubicacion_definitiva)),
                        [confirmado_por]     = @id_usuario_almacen,
                        [fecha_confirmacion] = SYSDATETIMEOFFSET()
                    WHERE [id] = @id_lote;

                    INSERT INTO [dbo].[tb_movimiento_almacen] (
                        [id_lote], [tipo], [cantidad],
                        [id_usuario], [motivo], [destino], [fecha_operacion]
                    )
                    VALUES (
                        @id_lote, 9, @cantidad,
                        @id_usuario_almacen, N'Visto bueno e ingreso definitivo a estantería de almacén.',
                        LTRIM(RTRIM(@ubicacion_definitiva)), CAST(GETDATE() AS DATE)
                    );

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[sp_registrar_salida_almacen]
                @id_lote            INT,
                @cantidad_salida    DECIMAL(12,2),
                @id_usuario_almacen NVARCHAR(450),
                @destino            NVARCHAR(160),
                @motivo             NVARCHAR(1000)
            AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRY
                    IF @cantidad_salida IS NULL OR @cantidad_salida <= 0
                        THROW 50030, 'La cantidad de salida debe ser mayor a cero.', 1;

                    DECLARE @stockActual DECIMAL(12,2), @pendiente BIT;
                    SELECT @stockActual = [cantidad_actual], @pendiente = [revision_pendiente]
                    FROM [dbo].[tb_lote] WITH (UPDLOCK)
                    WHERE [id] = @id_lote;

                    IF @stockActual IS NULL
                        THROW 50031, 'El lote seleccionado no existe.', 1;
                    IF @pendiente = 1
                        THROW 50032, 'No se puede retirar mercadería de un lote pendiente de visto bueno.', 1;
                    IF @stockActual < @cantidad_salida
                        THROW 50033, 'Stock insuficiente en el lote para cubrir la cantidad solicitada.', 1;

                    BEGIN TRANSACTION;

                    UPDATE [dbo].[tb_lote]
                    SET [cantidad_actual] = [cantidad_actual] - @cantidad_salida
                    WHERE [id] = @id_lote;

                    INSERT INTO [dbo].[tb_movimiento_almacen] (
                        [id_lote], [tipo], [cantidad],
                        [id_usuario], [motivo], [destino], [fecha_operacion]
                    )
                    VALUES (
                        @id_lote, 3, -@cantidad_salida,
                        @id_usuario_almacen, LTRIM(RTRIM(@motivo)), LTRIM(RTRIM(@destino)), CAST(GETDATE() AS DATE)
                    );

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH
            END
            """,
            """
            CREATE OR ALTER PROCEDURE [dbo].[sp_reporte_general_iglesia]
                @fecha_inicio DATE,
                @fecha_fin    DATE
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT
                    @fecha_inicio AS [desde],
                    @fecha_fin AS [hasta],
                    (SELECT ISNULL(SUM([monto]), 0) FROM [dbo].[tb_donacion]
                     WHERE [tipo] = 0 AND [estado] = 1 AND [fecha_recepcion_pago] BETWEEN @fecha_inicio AND @fecha_fin) AS [total_soles_aprobados],
                    (SELECT COUNT(*) FROM [dbo].[tb_donacion]
                     WHERE [estado] = 0 AND [fecha_recepcion_pago] BETWEEN @fecha_inicio AND @fecha_fin) AS [donaciones_pendientes],
                    (SELECT ISNULL(SUM([cantidad]), 0) FROM [dbo].[tb_movimiento_almacen]
                     WHERE [tipo] = 0 AND [fecha_operacion] BETWEEN @fecha_inicio AND @fecha_fin) AS [unidades_bienes_recibidos],
                    (SELECT ABS(ISNULL(SUM([cantidad]), 0)) FROM [dbo].[tb_movimiento_almacen]
                     WHERE [tipo] = 3 AND [fecha_operacion] BETWEEN @fecha_inicio AND @fecha_fin) AS [unidades_entregadas_obra_social],
                    (SELECT COUNT(*) FROM [dbo].[tb_mensaje_contacto]
                     WHERE CAST([fecha_creacion] AS DATE) BETWEEN @fecha_inicio AND @fecha_fin) AS [peticiones_oracion_recibidas],
                    (SELECT COUNT(*) FROM [dbo].[tb_mensaje_contacto]
                     WHERE [estado] = 1 AND CAST([fecha_creacion] AS DATE) BETWEEN @fecha_inicio AND @fecha_fin) AS [peticiones_oracion_atendidas];
            END
            """
        ];

        foreach (var sp in procedimientos)
            await db.Database.ExecuteSqlRawAsync(sp);
    }

    public static async Task<bool> EjecutarAsync(WebApplication app, string[] args)
    {
        var inicializar = args.Contains("--initialize");
        var crearAdmin = args.Contains("--create-admin");
        var limpiarDemo = args.Contains("--security-cleanup");
        if (!inicializar && !crearAdmin && !limpiarDemo) return false;

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ContextoBaseDatos>();
        await PrepararBaseDatosAsync(db, permitirCrear: true);

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var nombre in Roles.Todos)
            if (!await roles.RoleExistsAsync(nombre)) Asegurar(await roles.CreateAsync(new IdentityRole(nombre)));

        if (!await db.ConfiguracionesIglesia.AnyAsync(x => x.id == 1))
        {
            db.ConfiguracionesIglesia.Add(new ConfiguracionIglesia
            {
                id = 1,
                nombre = "Iglesia Monte de Oración",
                informacion = "Bienvenidos a nuestra comunidad de fe en Comas, Lima."
            });
            await db.SaveChangesAsync();
        }

        if (inicializar)
        {
            await AsegurarObjetosSqlAsync(db);
            Console.WriteLine("Esquema, migraciones, roles, vistas y procedimientos listos.");
        }

        if (crearAdmin)
        {
            Console.Write("Correo del administrador: ");
            var correo = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(correo) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(correo))
                throw new InvalidOperationException("Correo inválido.");

            Console.Write("Nombre completo (opcional): ");
            var nombre = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(nombre)) nombre = correo;

            Console.Write("Contraseña (mínimo 10 caracteres con mayúscula, minúscula, número y símbolo): ");
            var clave = LeerClave();
            Console.WriteLine();

            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            if (await usuarios.FindByEmailAsync(correo) != null)
                throw new InvalidOperationException("Ese correo ya tiene una cuenta. No se modificó.");

            await using var transaccion = await db.Database.BeginTransactionAsync();
            var usuario = new IdentityUser { UserName = correo, Email = correo, EmailConfirmed = true };
            Asegurar(await usuarios.CreateAsync(usuario, clave));
            Asegurar(await usuarios.AddToRoleAsync(usuario, Roles.Administrador));
            db.PerfilesPersonal.Add(new PerfilPersonal
            {
                id_usuario = usuario.Id,
                nombre_completo = nombre[..Math.Min(160, nombre.Length)]
            });
            await db.SaveChangesAsync();
            await transaccion.CommitAsync();
            Console.WriteLine("Administrador creado.");
        }

        if (limpiarDemo)
        {
            string[] idsReferencia = ["USR-DIR-001", "USR-REC-002", "USR-ALM-003"];
            var perfiles = await db.PerfilesPersonal.Where(x => idsReferencia.Contains(x.id_usuario)).ToListAsync();
            var rolesDemo = await db.UserRoles.Where(x => idsReferencia.Contains(x.UserId)).ToListAsync();

            var rolAdmin = await db.Roles.Where(x => x.NormalizedName == Roles.Administrador.ToUpperInvariant()).Select(x => x.Id).SingleOrDefaultAsync();
            var quedaAdmin = rolAdmin != null && await db.UserRoles.AnyAsync(x =>
                x.RoleId == rolAdmin && !idsReferencia.Contains(x.UserId) &&
                db.PerfilesPersonal.Any(p => p.id_usuario == x.UserId && p.activo));
            if (!quedaAdmin)
                throw new InvalidOperationException("No se desactivaron las cuentas de referencia porque no quedaría ningún administrador activo. Crea o activa otro administrador primero.");

            foreach (var perfil in perfiles) perfil.activo = false;
            if (rolesDemo.Count > 0) db.UserRoles.RemoveRange(rolesDemo);
            await db.SaveChangesAsync();
            Console.WriteLine($"Cuentas de referencia desactivadas: {perfiles.Count}.");
        }

        Console.WriteLine("Operación completada.");
        return true;
    }
    private static string LeerClave()
    {
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var texto = new System.Text.StringBuilder();
        while (true) { var tecla = Console.ReadKey(true); if (tecla.Key == ConsoleKey.Enter) return texto.ToString(); if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; } else if (!char.IsControl(tecla.KeyChar)) texto.Append(tecla.KeyChar); }
    }

    private static void Asegurar(IdentityResult resultado) { if (!resultado.Succeeded) throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(x => x.Description))); }
}

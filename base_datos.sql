-- =============================================================================
-- SCRIPT INTEGRAL SQL SERVER - SISTEMA DE GESTIÓN "IGLESIA MONTE DE ORACIÓN"
-- =============================================================================
-- Cumple con:
--   1. Normalización en 1FN, 2FN y 3FN (100% de tablas conectadas con FOREIGN KEY)
--   2. Restricciones de Campo (PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK, DEFAULT, NOT NULL)
--   3. Proceso ETL (Extract, Transform, Load) e Inserción de Datos
--   4. Consultas Avanzadas de Negocio (JOIN, GROUP BY, HAVING, CASE, Subconsultas)
--   5. Vistas (CREATE OR ALTER VIEW)
--   6. Procedimientos Almacenados Transaccionales (CREATE OR ALTER PROCEDURE)
-- =============================================================================

IF DB_ID(N'MonteOracionApp') IS NULL
BEGIN
    CREATE DATABASE [MonteOracionApp];
END
GO

USE [MonteOracionApp];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- SECCIÓN 1: ESTRUCTURA DE TABLAS (1FN, 2FN, 3FN) Y RESTRICCIONES DE CAMPO
-- =============================================================================

-- -----------------------------------------------------------------------------
-- MÓDULO 1: SEGURIDAD, ROLES Y PERSONAL INTERNO
-- -----------------------------------------------------------------------------

-- 1. TABLA DE USUARIOS DEL SISTEMA
IF OBJECT_ID(N'dbo.tb_usuario', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_usuario] (
        [id]                         NVARCHAR(450)  NOT NULL,
        [nombre_usuario]             NVARCHAR(256)  NULL,
        [nombre_usuario_normalizado] NVARCHAR(256)  NULL,
        [correo]                     NVARCHAR(256)  NULL,
        [correo_normalizado]         NVARCHAR(256)  NULL,
        [correo_confirmado]          BIT            NOT NULL CONSTRAINT [DF_tb_usuario_correo_confirmado] DEFAULT (0),
        [clave_hash]                 NVARCHAR(MAX)  NULL,
        [sello_seguridad]            NVARCHAR(MAX)  NULL,
        [sello_concurrencia]         NVARCHAR(MAX)  NULL,
        [telefono]                   NVARCHAR(MAX)  NULL,
        [telefono_confirmado]        BIT            NOT NULL CONSTRAINT [DF_tb_usuario_telefono_confirmado] DEFAULT (0),
        [doble_factor_activo]        BIT            NOT NULL CONSTRAINT [DF_tb_usuario_doble_factor] DEFAULT (0),
        [fin_bloqueo]                DATETIMEOFFSET NULL,
        [bloqueo_activo]             BIT            NOT NULL CONSTRAINT [DF_tb_usuario_bloqueo] DEFAULT (1),
        [intentos_fallidos]          INT            NOT NULL CONSTRAINT [DF_tb_usuario_intentos] DEFAULT (0),
        CONSTRAINT [PK_tb_usuario] PRIMARY KEY ([id]),
        CONSTRAINT [CK_tb_usuario_intentos] CHECK ([intentos_fallidos] >= 0)
    );
    CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[tb_usuario] ([nombre_usuario_normalizado]) WHERE [nombre_usuario_normalizado] IS NOT NULL;
    CREATE INDEX [EmailIndex] ON [dbo].[tb_usuario] ([correo_normalizado]);
END
GO

-- 2. TABLA DE ROLES DEL SISTEMA (Administrador, Recepcion, Almacen)
IF OBJECT_ID(N'dbo.tb_rol', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_rol] (
        [id]                 NVARCHAR(450) NOT NULL,
        [nombre]             NVARCHAR(256) NULL,
        [nombre_normalizado] NVARCHAR(256) NULL,
        [sello_concurrencia] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_tb_rol] PRIMARY KEY ([id])
    );
    CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[tb_rol] ([nombre_normalizado]) WHERE [nombre_normalizado] IS NOT NULL;
END
GO

-- 3. TABLA PUENTE DE ROLES POR USUARIO (Cumple 1FN y 2FN para relación N:M)
IF OBJECT_ID(N'dbo.tb_usuario_rol', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_usuario_rol] (
        [id_usuario] NVARCHAR(450) NOT NULL,
        [id_rol]     NVARCHAR(450) NOT NULL,
        CONSTRAINT [PK_tb_usuario_rol] PRIMARY KEY ([id_usuario], [id_rol]),
        CONSTRAINT [FK_tb_usuario_rol_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_usuario_rol_tb_rol]     FOREIGN KEY ([id_rol])     REFERENCES [dbo].[tb_rol] ([id])     ON DELETE CASCADE
    );
    CREATE INDEX [IX_tb_usuario_rol_id_rol] ON [dbo].[tb_usuario_rol] ([id_rol]);
END
GO

-- 4. TABLA DE PERFIL DE PERSONAL INTERNO (Conectada a tb_usuario)
IF OBJECT_ID(N'dbo.tb_personal', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_personal] (
        [id_usuario]      NVARCHAR(450) NOT NULL,
        [nombre_completo] NVARCHAR(160) NOT NULL,
        [cargo]           NVARCHAR(100) NULL,
        [activo]          BIT           NOT NULL CONSTRAINT [DF_tb_personal_activo] DEFAULT (1),
        CONSTRAINT [PK_tb_personal] PRIMARY KEY ([id_usuario]),
        CONSTRAINT [FK_tb_personal_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_personal_nombre_no_vacio] CHECK (LEN(LTRIM(RTRIM([nombre_completo]))) >= 3)
    );
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 2: COMUNIDAD Y ATENCIÓN PASTORAL
-- -----------------------------------------------------------------------------

-- 5. TABLA DE MIEMBROS / CONGREGANTES (Conectada a tb_usuario)
IF OBJECT_ID(N'dbo.tb_congregante', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_congregante] (
        [id]               INT           IDENTITY(1,1) NOT NULL,
        [dni]              NVARCHAR(8)   NULL,
        [nombres]          NVARCHAR(100) NOT NULL,
        [apellidos]        NVARCHAR(100) NOT NULL,
        [telefono]         NVARCHAR(9)   NULL,
        [fecha_nacimiento] DATETIME2     NULL,
        [direccion]        NVARCHAR(250) NULL,
        [activo]           BIT           NOT NULL CONSTRAINT [DF_tb_congregante_activo] DEFAULT (1),
        [id_usuario]       NVARCHAR(450) NULL,
        CONSTRAINT [PK_tb_congregante] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_congregante_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_congregante_dni]      CHECK ([dni] IS NULL OR [dni] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
        CONSTRAINT [CK_tb_congregante_telefono] CHECK ([telefono] IS NULL OR [telefono] LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]')
    );
    CREATE UNIQUE INDEX [IX_tb_congregante_dni] ON [dbo].[tb_congregante] ([dni]) WHERE [dni] IS NOT NULL;
    CREATE UNIQUE INDEX [IX_tb_congregante_id_usuario] ON [dbo].[tb_congregante] ([id_usuario]) WHERE [id_usuario] IS NOT NULL;
END
GO

-- 6. TABLA DE MENSAJES Y PETICIONES DE ORACIÓN (Conectada a tb_congregante y tb_usuario)
IF OBJECT_ID(N'dbo.tb_mensaje_contacto', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_mensaje_contacto] (
        [id]                 INT            IDENTITY(1,1) NOT NULL,
        [fecha_creacion]     DATETIMEOFFSET NOT NULL CONSTRAINT [DF_tb_mensaje_fecha] DEFAULT (SYSDATETIMEOFFSET()),
        [id_congregante]     INT            NULL,
        [nombre]             NVARCHAR(150)  NOT NULL,
        [telefono]           NVARCHAR(30)   NOT NULL,
        [correo]             NVARCHAR(256)  NULL,
        [motivo]             INT            NOT NULL CONSTRAINT [DF_tb_mensaje_motivo] DEFAULT (0),
        [preferencia]        INT            NOT NULL CONSTRAINT [DF_tb_mensaje_preferencia] DEFAULT (1),
        [mensaje]            NVARCHAR(2000) NOT NULL,
        [estado]             INT            NOT NULL CONSTRAINT [DF_tb_mensaje_estado] DEFAULT (0),
        [fecha_atencion]     DATETIMEOFFSET NULL,
        [id_usuario]         NVARCHAR(450)  NULL,
        [notas_atencion]     NVARCHAR(1000) NULL,
        CONSTRAINT [PK_tb_mensaje_contacto] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_mensaje_contacto_tb_congregante] FOREIGN KEY ([id_congregante]) REFERENCES [dbo].[tb_congregante] ([id]),
        CONSTRAINT [FK_tb_mensaje_contacto_tb_usuario]     FOREIGN KEY ([id_usuario])     REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_mensaje_contacto_motivo]         CHECK ([motivo] IN (0, 1, 2, 3, 4)),
        CONSTRAINT [CK_tb_mensaje_contacto_preferencia]    CHECK ([preferencia] IN (0, 1, 2)),
        CONSTRAINT [CK_tb_mensaje_contacto_estado]         CHECK ([estado] IN (0, 1, 2)),
        CONSTRAINT [CK_tb_mensaje_contacto_longitud]       CHECK (LEN(LTRIM(RTRIM([mensaje]))) >= 10)
    );
    CREATE INDEX [IX_tb_mensaje_contacto_estado_fecha] ON [dbo].[tb_mensaje_contacto] ([estado], [fecha_creacion]);
    CREATE INDEX [IX_tb_mensaje_contacto_id_congregante] ON [dbo].[tb_mensaje_contacto] ([id_congregante]);
    CREATE INDEX [IX_tb_mensaje_contacto_id_usuario] ON [dbo].[tb_mensaje_contacto] ([id_usuario]);
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 3: CONFIGURACIÓN, EVENTOS Y DONACIONES
-- -----------------------------------------------------------------------------

-- 7. TABLA DE CONFIGURACIÓN INSTITUCIONAL DE LA IGLESIA (Conectada a tb_usuario)
IF OBJECT_ID(N'dbo.tb_iglesia', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_iglesia] (
        [id]                      INT            NOT NULL CONSTRAINT [DF_tb_iglesia_id] DEFAULT (1),
        [nombre]                  NVARCHAR(150)  NOT NULL,
        [informacion]             NVARCHAR(3000) NOT NULL,
        [direccion]               NVARCHAR(300)  NULL,
        [contacto]                NVARCHAR(100)  NULL,
        [horarios]                NVARCHAR(600)  NULL,
        [id_usuario]              NVARCHAR(450)  NULL,
        CONSTRAINT [PK_tb_iglesia] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_iglesia_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_ChurchSettings_Singleton] CHECK ([id] = 1)
    );
END
GO

-- 8. TABLA DE MEDIOS DE PAGO (Conectada a tb_usuario)
IF OBJECT_ID(N'dbo.tb_medio_pago', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_medio_pago] (
        [id]                INT           IDENTITY(1,1) NOT NULL,
        [nombre]            NVARCHAR(100) NOT NULL,
        [instrucciones]     NVARCHAR(500) NOT NULL,
        [numero_cuenta]     NVARCHAR(50)  NULL,
        [clave_qr]          NVARCHAR(MAX) NULL,
        [tipo_contenido_qr] NVARCHAR(MAX) NULL,
        [activo]            BIT           NOT NULL CONSTRAINT [DF_tb_medio_pago_activo] DEFAULT (1),
        [id_usuario]        NVARCHAR(450) NULL,
        CONSTRAINT [PK_tb_medio_pago] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_medio_pago_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id])
    );
END
GO

-- 9. TABLA DE EVENTOS Y PUBLICACIONES (Conectada a tb_usuario)
IF OBJECT_ID(N'dbo.tb_publicacion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_publicacion] (
        [id]                  INT            IDENTITY(1,1) NOT NULL,
        [tipo]                INT            NOT NULL CONSTRAINT [DF_tb_publicacion_tipo] DEFAULT (0),
        [titulo]              NVARCHAR(160)  NOT NULL,
        [contenido]           NVARCHAR(MAX)  NOT NULL,
        [fecha_inicio_evento] DATETIME2      NULL,
        [fecha_fin_evento]    DATETIME2      NULL,
        [clave_imagen]        NVARCHAR(MAX)  NULL,
        [tipo_imagen]         NVARCHAR(MAX)  NULL,
        [lugar]               NVARCHAR(250)  NULL,
        [meta_recaudacion]    DECIMAL(12,2)  NULL,
        [publicado]           BIT            NOT NULL CONSTRAINT [DF_tb_publicacion_publicado] DEFAULT (1),
        [cancelado]           BIT            NOT NULL CONSTRAINT [DF_tb_publicacion_cancelado] DEFAULT (0),
        [fecha_creacion]      DATETIMEOFFSET NOT NULL CONSTRAINT [DF_tb_publicacion_fecha] DEFAULT (SYSDATETIMEOFFSET()),
        [id_usuario]          NVARCHAR(450)  NULL,
        CONSTRAINT [PK_tb_publicacion] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_publicacion_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_publicacion_tipo]   CHECK ([tipo] IN (0, 1)),
        CONSTRAINT [CK_tb_publicacion_meta]   CHECK ([meta_recaudacion] IS NULL OR [meta_recaudacion] > 0),
        CONSTRAINT [CK_tb_publicacion_fechas] CHECK ([fecha_fin_evento] IS NULL OR [fecha_inicio_evento] IS NULL OR [fecha_fin_evento] >= [fecha_inicio_evento])
    );
END
GO

-- 10. TABLA DE CATÁLOGO DE PRODUCTOS DE ALMACÉN (Cumple 2FN y 3FN)
IF OBJECT_ID(N'dbo.tb_producto', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_producto] (
        [id]        INT           IDENTITY(1,1) NOT NULL,
        [nombre]    NVARCHAR(160) NOT NULL,
        [categoria] NVARCHAR(80)  NOT NULL CONSTRAINT [DF_tb_producto_categoria] DEFAULT (N'Alimentos y Víveres'),
        [unidad]    NVARCHAR(40)  NOT NULL CONSTRAINT [DF_tb_producto_unidad] DEFAULT (N'unidad'),
        [activo]    BIT           NOT NULL CONSTRAINT [DF_tb_producto_activo] DEFAULT (1),
        CONSTRAINT [PK_tb_producto] PRIMARY KEY ([id]),
        CONSTRAINT [CK_tb_producto_nombre] CHECK (LEN(LTRIM(RTRIM([nombre]))) >= 2)
    );
    CREATE UNIQUE INDEX [IX_tb_producto_nombre_unidad] ON [dbo].[tb_producto] ([nombre], [unidad]);
END
GO

-- 11. TABLA DE CABECERA DE DONACIONES (Conectada a tb_congregante, tb_publicacion, tb_medio_pago y tb_usuario)
IF OBJECT_ID(N'dbo.tb_donacion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion] (
        [id]                         INT              IDENTITY(1,1) NOT NULL,
        [codigo_seguimiento]         UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_tb_donacion_codigo] DEFAULT (NEWID()),
        [tipo]                       INT              NOT NULL,
        [estado]                     INT              NOT NULL CONSTRAINT [DF_tb_donacion_estado] DEFAULT (0),
        [tipo_donante]               INT              NOT NULL,
        [id_congregante]             INT              NULL,
        [nombre_externo]             NVARCHAR(180)    NULL,
        [contacto_externo]           NVARCHAR(150)    NULL,
        [monto]                      DECIMAL(12,2)    NULL,
        [numero_operacion]           NVARCHAR(80)     NULL,
        [id_evento]                  INT              NULL,
        [id_medio_pago]              INT              NULL,
        [nombre_medio_pago]          NVARCHAR(100)    NULL,
        [fecha_recepcion_pago]       DATETIME2        NOT NULL,
        [fecha_creacion]             DATETIMEOFFSET   NOT NULL CONSTRAINT [DF_tb_donacion_fecha] DEFAULT (SYSDATETIMEOFFSET()),
        [clave_comprobante]          NVARCHAR(MAX)    NULL,
        [tipo_contenido_comprobante] NVARCHAR(MAX)    NULL,
        [id_usuario]                 NVARCHAR(450)    NULL,
        [digital]                    BIT              NOT NULL CONSTRAINT [DF_tb_donacion_digital] DEFAULT (0),
        CONSTRAINT [PK_tb_donacion] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_donacion_tb_congregante] FOREIGN KEY ([id_congregante]) REFERENCES [dbo].[tb_congregante] ([id]),
        CONSTRAINT [FK_tb_donacion_tb_publicacion] FOREIGN KEY ([id_evento])      REFERENCES [dbo].[tb_publicacion] ([id]),
        CONSTRAINT [FK_tb_donacion_tb_medio_pago]  FOREIGN KEY ([id_medio_pago])  REFERENCES [dbo].[tb_medio_pago] ([id]),
        CONSTRAINT [FK_tb_donacion_tb_usuario]     FOREIGN KEY ([id_usuario])     REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_donacion_monto]  CHECK ([monto] IS NULL OR [monto] > 0),
        CONSTRAINT [CK_tb_donacion_estado] CHECK ([estado] IN (0, 1, 2, 3))
    );
    CREATE UNIQUE INDEX [IX_tb_donacion_codigo_seguimiento] ON [dbo].[tb_donacion] ([codigo_seguimiento]);
    CREATE UNIQUE INDEX [IX_tb_donacion_operacion_unica] ON [dbo].[tb_donacion] ([id_medio_pago], [numero_operacion]) WHERE [numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL;
    CREATE INDEX [IX_tb_donacion_id_evento] ON [dbo].[tb_donacion] ([id_evento]);
    CREATE INDEX [IX_tb_donacion_estado_fecha] ON [dbo].[tb_donacion] ([estado], [fecha_creacion]);
    CREATE INDEX [IX_tb_donacion_id_congregante] ON [dbo].[tb_donacion] ([id_congregante]);
END
GO

-- 12. TABLA DE DETALLE DE BIENES DONADOS (Cumple 1FN: separa ítems repetitivos, conectada a tb_donacion y tb_producto)
IF OBJECT_ID(N'dbo.tb_donacion_bien', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion_bien] (
        [id]                INT           IDENTITY(1,1) NOT NULL,
        [id_donacion]       INT           NOT NULL,
        [id_producto]       INT           NULL,
        [ubicacion_almacen] NVARCHAR(160) NULL,
        [descripcion]       NVARCHAR(250) NOT NULL,
        [cantidad]          DECIMAL(12,2) NOT NULL,
        [unidad]            NVARCHAR(40)  NOT NULL,
        CONSTRAINT [PK_tb_donacion_bien] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_donacion_bien_tb_donacion] FOREIGN KEY ([id_donacion]) REFERENCES [dbo].[tb_donacion] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_donacion_bien_tb_producto] FOREIGN KEY ([id_producto]) REFERENCES [dbo].[tb_producto] ([id]),
        CONSTRAINT [CK_DonationItem_Quantity] CHECK ([cantidad] > 0)
    );
    CREATE INDEX [IX_tb_donacion_bien_id_donacion] ON [dbo].[tb_donacion_bien] ([id_donacion]);
    CREATE INDEX [IX_tb_donacion_bien_id_producto] ON [dbo].[tb_donacion_bien] ([id_producto]);
END
GO

-- 13. TABLA DE AUDITORÍA / HISTORIAL DE EVALUACIÓN DE DONACIONES (Conectada a tb_donacion y tb_usuario)
IF OBJECT_ID(N'dbo.tb_donacion_historial', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion_historial] (
        [id]               INT            IDENTITY(1,1) NOT NULL,
        [id_donacion]      INT            NOT NULL,
        [estado]           INT            NOT NULL,
        [id_administrador] NVARCHAR(450)  NOT NULL,
        [fecha]            DATETIMEOFFSET NOT NULL CONSTRAINT [DF_tb_donacion_historial_fecha] DEFAULT (SYSDATETIMEOFFSET()),
        [motivo]           NVARCHAR(1000) NULL,
        CONSTRAINT [PK_tb_donacion_historial] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_donacion_historial_tb_donacion] FOREIGN KEY ([id_donacion])      REFERENCES [dbo].[tb_donacion] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_donacion_historial_tb_usuario]  FOREIGN KEY ([id_administrador]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_donacion_historial_estado]      CHECK ([estado] IN (0, 1, 2, 3))
    );
    CREATE INDEX [IX_tb_donacion_historial_id_donacion] ON [dbo].[tb_donacion_historial] ([id_donacion]);
    CREATE INDEX [IX_tb_donacion_historial_id_administrador] ON [dbo].[tb_donacion_historial] ([id_administrador]);
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 4: ALMACÉN, LOTES Y KARDEX DE OBRA SOCIAL
-- -----------------------------------------------------------------------------

-- 14. TABLA DE LOTES / EXISTENCIAS EN ALMACÉN (Conectada a tb_producto, tb_donacion_bien y tb_usuario)
IF OBJECT_ID(N'dbo.tb_lote', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_lote] (
        [id]                   INT            IDENTITY(1,1) NOT NULL,
        [id_producto]          INT            NOT NULL,
        [id_donacion_bien]     INT            NULL,
        [ubicacion]            NVARCHAR(160)  NOT NULL,
        [origen]               NVARCHAR(200)  NOT NULL,
        [cantidad_actual]      DECIMAL(12,2)  NOT NULL,
        [revision_pendiente]   BIT            NOT NULL CONSTRAINT [DF_tb_lote_revision] DEFAULT (1),
        [id_usuario_pendiente] NVARCHAR(450)  NULL,
        [fecha_pendiente]      DATETIMEOFFSET NULL,
        [confirmado_por]       NVARCHAR(450)  NULL,
        [fecha_confirmacion]   DATETIMEOFFSET NULL,
        CONSTRAINT [PK_tb_lote] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_lote_tb_producto]             FOREIGN KEY ([id_producto])          REFERENCES [dbo].[tb_producto] ([id]),
        CONSTRAINT [FK_tb_lote_tb_donacion_bien]        FOREIGN KEY ([id_donacion_bien])     REFERENCES [dbo].[tb_donacion_bien] ([id]),
        CONSTRAINT [FK_tb_lote_tb_usuario_pendiente]    FOREIGN KEY ([id_usuario_pendiente]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [FK_tb_lote_tb_usuario_confirmacion] FOREIGN KEY ([confirmado_por])       REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_Lote_Saldos] CHECK ([cantidad_actual] >= 0)
    );
    CREATE UNIQUE INDEX [IX_tb_lote_id_donacion_bien] ON [dbo].[tb_lote] ([id_donacion_bien]) WHERE [id_donacion_bien] IS NOT NULL;
    CREATE INDEX [IX_tb_lote_id_producto] ON [dbo].[tb_lote] ([id_producto]);
END
GO

-- 15. TABLA DE MOVIMIENTOS DE ALMACÉN / KARDEX (Conectada a tb_lote y tb_usuario)
IF OBJECT_ID(N'dbo.tb_movimiento_almacen', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_movimiento_almacen] (
        [id]              BIGINT         IDENTITY(1,1) NOT NULL,
        [id_lote]         INT            NOT NULL,
        [tipo]            INT            NOT NULL,
        [cantidad]        DECIMAL(12,2)  NOT NULL,
        [id_usuario]      NVARCHAR(450)  NOT NULL,
        [motivo]          NVARCHAR(1000) NOT NULL,
        [destino]         NVARCHAR(160)  NULL,
        [fecha_hora]      DATETIMEOFFSET NOT NULL CONSTRAINT [DF_tb_movimiento_fecha_hora] DEFAULT (SYSDATETIMEOFFSET()),
        [fecha_operacion] DATETIME2      NOT NULL,
        CONSTRAINT [PK_tb_movimiento_almacen] PRIMARY KEY ([id]),
        CONSTRAINT [FK_tb_movimiento_almacen_tb_lote]    FOREIGN KEY ([id_lote])    REFERENCES [dbo].[tb_lote] ([id]),
        CONSTRAINT [FK_tb_movimiento_almacen_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]),
        CONSTRAINT [CK_tb_movimiento_almacen_cantidad]   CHECK ([cantidad] <> 0),
        CONSTRAINT [CK_tb_movimiento_almacen_tipo]       CHECK ([tipo] IN (0, 3, 9))
    );
    CREATE INDEX [IX_tb_movimiento_almacen_id_lote] ON [dbo].[tb_movimiento_almacen] ([id_lote]);
    CREATE INDEX [IX_tb_movimiento_almacen_fecha_operacion] ON [dbo].[tb_movimiento_almacen] ([fecha_operacion]);
END
GO


-- =============================================================================
-- CORRECCIONES IDEMPOTENTES PARA BASES CREADAS POR VERSIONES ANTERIORES
-- =============================================================================
-- Reemplaza el índice antiguo (medio + fecha + operación) por la regla
-- definitiva: una operación no se repite para el mismo medio de pago.
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id]
    INNER JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
    WHERE i.[object_id] = OBJECT_ID(N'dbo.tb_donacion')
      AND i.[name] = N'IX_tb_donacion_operacion_unica'
      AND c.[name] = N'fecha_recepcion_pago'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM [dbo].[tb_donacion]
        WHERE [numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL
        GROUP BY [id_medio_pago], [numero_operacion]
        HAVING COUNT(*) > 1
    )
        THROW 50050, N'Existen números de operación repetidos para el mismo medio de pago. Corrige esos registros antes de crear el índice único.', 1;

    DROP INDEX [IX_tb_donacion_operacion_unica] ON [dbo].[tb_donacion];
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE [object_id] = OBJECT_ID(N'dbo.tb_donacion')
      AND [name] = N'IX_tb_donacion_operacion_unica'
)
BEGIN
    IF EXISTS (
        SELECT 1
        FROM [dbo].[tb_donacion]
        WHERE [numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL
        GROUP BY [id_medio_pago], [numero_operacion]
        HAVING COUNT(*) > 1
    )
        THROW 50050, N'Existen números de operación repetidos para el mismo medio de pago. Corrige esos registros antes de crear el índice único.', 1;

    CREATE UNIQUE INDEX [IX_tb_donacion_operacion_unica]
        ON [dbo].[tb_donacion] ([id_medio_pago], [numero_operacion])
        WHERE [numero_operacion] IS NOT NULL AND [id_medio_pago] IS NOT NULL;
END

-- Restricciones que pudieron faltar en bases creadas con versiones anteriores.
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_usuario') AND [name] = N'CK_tb_usuario_intentos')
    ALTER TABLE [dbo].[tb_usuario] WITH NOCHECK ADD CONSTRAINT [CK_tb_usuario_intentos] CHECK ([intentos_fallidos] >= 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_personal') AND [name] = N'CK_tb_personal_nombre_no_vacio')
    ALTER TABLE [dbo].[tb_personal] WITH NOCHECK ADD CONSTRAINT [CK_tb_personal_nombre_no_vacio] CHECK (LEN(LTRIM(RTRIM([nombre_completo]))) >= 3);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_congregante') AND [name] = N'CK_tb_congregante_dni')
    ALTER TABLE [dbo].[tb_congregante] WITH NOCHECK ADD CONSTRAINT [CK_tb_congregante_dni] CHECK ([dni] IS NULL OR [dni] LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_congregante') AND [name] = N'CK_tb_congregante_telefono')
    ALTER TABLE [dbo].[tb_congregante] WITH NOCHECK ADD CONSTRAINT [CK_tb_congregante_telefono] CHECK ([telefono] IS NULL OR [telefono] LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_mensaje_contacto') AND [name] = N'CK_tb_mensaje_contacto_motivo')
    ALTER TABLE [dbo].[tb_mensaje_contacto] WITH NOCHECK ADD CONSTRAINT [CK_tb_mensaje_contacto_motivo] CHECK ([motivo] IN (0, 1, 2, 3, 4));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_mensaje_contacto') AND [name] = N'CK_tb_mensaje_contacto_preferencia')
    ALTER TABLE [dbo].[tb_mensaje_contacto] WITH NOCHECK ADD CONSTRAINT [CK_tb_mensaje_contacto_preferencia] CHECK ([preferencia] IN (0, 1, 2));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_mensaje_contacto') AND [name] = N'CK_tb_mensaje_contacto_estado')
    ALTER TABLE [dbo].[tb_mensaje_contacto] WITH NOCHECK ADD CONSTRAINT [CK_tb_mensaje_contacto_estado] CHECK ([estado] IN (0, 1, 2));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_mensaje_contacto') AND [name] = N'CK_tb_mensaje_contacto_longitud')
    ALTER TABLE [dbo].[tb_mensaje_contacto] WITH NOCHECK ADD CONSTRAINT [CK_tb_mensaje_contacto_longitud] CHECK (LEN(LTRIM(RTRIM([mensaje]))) >= 10);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_publicacion') AND [name] = N'CK_tb_publicacion_tipo')
    ALTER TABLE [dbo].[tb_publicacion] WITH NOCHECK ADD CONSTRAINT [CK_tb_publicacion_tipo] CHECK ([tipo] IN (0, 1));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_publicacion') AND [name] = N'CK_tb_publicacion_meta')
    ALTER TABLE [dbo].[tb_publicacion] WITH NOCHECK ADD CONSTRAINT [CK_tb_publicacion_meta] CHECK ([meta_recaudacion] IS NULL OR [meta_recaudacion] > 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_publicacion') AND [name] = N'CK_tb_publicacion_fechas')
    ALTER TABLE [dbo].[tb_publicacion] WITH NOCHECK ADD CONSTRAINT [CK_tb_publicacion_fechas] CHECK ([fecha_fin_evento] IS NULL OR [fecha_inicio_evento] IS NULL OR [fecha_fin_evento] >= [fecha_inicio_evento]);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_producto') AND [name] = N'CK_tb_producto_nombre')
    ALTER TABLE [dbo].[tb_producto] WITH NOCHECK ADD CONSTRAINT [CK_tb_producto_nombre] CHECK (LEN(LTRIM(RTRIM([nombre]))) >= 2);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_donacion') AND [name] = N'CK_tb_donacion_estado')
    ALTER TABLE [dbo].[tb_donacion] WITH NOCHECK ADD CONSTRAINT [CK_tb_donacion_estado] CHECK ([estado] IN (0, 1, 2, 3));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_donacion_historial') AND [name] = N'CK_tb_donacion_historial_estado')
    ALTER TABLE [dbo].[tb_donacion_historial] WITH NOCHECK ADD CONSTRAINT [CK_tb_donacion_historial_estado] CHECK ([estado] IN (0, 1, 2, 3));
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_movimiento_almacen') AND [name] = N'CK_tb_movimiento_almacen_cantidad')
    ALTER TABLE [dbo].[tb_movimiento_almacen] WITH NOCHECK ADD CONSTRAINT [CK_tb_movimiento_almacen_cantidad] CHECK ([cantidad] <> 0);
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE [parent_object_id] = OBJECT_ID(N'dbo.tb_movimiento_almacen') AND [name] = N'CK_tb_movimiento_almacen_tipo')
    ALTER TABLE [dbo].[tb_movimiento_almacen] WITH NOCHECK ADD CONSTRAINT [CK_tb_movimiento_almacen_tipo] CHECK ([tipo] IN (0, 3, 9));
GO


-- =============================================================================
-- SECCIÓN 2: PROCESO ETL (EXTRACCIÓN, TRANSFORMACIÓN Y CARGA) E INSERCIÓN DE DATOS
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 2.1 INSERCIÓN DE CATÁLOGOS BASE (ROLES, PERSONAL, IGLESIA, MEDIOS DE PAGO, PRODUCTOS, EVENTOS)
-- -----------------------------------------------------------------------------

-- A) Roles del sistema
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'ADMINISTRADOR')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (N'ROL-ADMIN-001', N'Administrador', N'ADMINISTRADOR', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'RECEPCION')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (N'ROL-RECEP-002', N'Recepcion', N'RECEPCION', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'ALMACEN')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (N'ROL-ALMAC-003', N'Almacen', N'ALMACEN', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'PERSONAL')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (N'ROL-PERSO-004', N'Personal', N'PERSONAL', NEWID());
GO

-- B) Cuentas de personal
-- No se crean usuarios, contraseñas ni cuentas de demostración desde el script.
-- Después de ejecutar este archivo, crea el administrador con un comando interactivo:
--     dotnet run -- --create-admin
GO

-- C) Configuración de la Iglesia
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_iglesia] WHERE [id] = 1)
    INSERT INTO [dbo].[tb_iglesia] ([id], [nombre], [informacion], [direccion], [contacto], [horarios], [id_usuario])
    VALUES (1, N'Iglesia Monte de Oración', N'Bienvenidos a nuestra comunidad de fe y obra social en Comas, Lima.', N'Av. Túpac Amaru Km 11, Comas - Lima', N'987654321', N'Miércoles 7:00 p. m. | Domingos 9:00 a. m. y 6:00 p. m.', NULL);
GO

-- D) Medios de Pago
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_medio_pago])
BEGIN
    INSERT INTO [dbo].[tb_medio_pago] ([nombre], [instrucciones], [numero_cuenta], [activo], [id_usuario])
    VALUES
        (N'Yape Oficial Iglesia', N'Yapear a nombre de Iglesia Monte de Oración y adjuntar captura.', N'987654321', 1, NULL),
        (N'Plin Obra Social', N'Transferencia directa por Plin al número oficial de tesorería.', N'987654321', 1, NULL),
        (N'BCP Cuenta Soles', N'Cuenta Corriente Soles BCP - CCI: 00219100234567890123.', N'191-2345678-0-99', 1, NULL);
END
GO

-- E) Catálogo Maestro de Productos de Almacén
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_producto])
BEGIN
    INSERT INTO [dbo].[tb_producto] ([nombre], [categoria], [unidad], [activo])
    VALUES
        (N'Arroz Extra Costeño', N'Alimentos y Víveres', N'kg', 1),
        (N'Azúcar Rubia Doméstica', N'Alimentos y Víveres', N'kg', 1),
        (N'Aceite Vegetal 1L', N'Alimentos y Víveres', N'botella', 1),
        (N'Leche Evaporada Tarro', N'Alimentos y Víveres', N'lata', 1),
        (N'Fideos Spaghetti 500g', N'Alimentos y Víveres', N'paquete', 1),
        (N'Frazada Polar 2 Plazas', N'Ropa y Abrigo', N'unidad', 1),
        (N'Kit de Higiene Familiar', N'Aseo y Salud', N'kit', 1);
END
GO

-- F) Eventos y Publicaciones
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_publicacion])
BEGIN
    INSERT INTO [dbo].[tb_publicacion] ([tipo], [titulo], [contenido], [fecha_inicio_evento], [fecha_fin_evento], [lugar], [meta_recaudacion], [publicado], [cancelado], [id_usuario])
    VALUES
        (0, N'Campaña de Abrigo y Víveres para Comas Alto', N'Recolección solidaria de alimentos no perecibles y frazadas para 80 familias vulnerables de las zonas altas de Comas.', DATEADD(DAY, 10, GETDATE()), DATEADD(DAY, 11, GETDATE()), N'Templo Central - Av. Túpac Amaru, Comas', 2500.00, 1, 0, NULL),
        (0, N'Desayuno Navideño Infantil Monte de Oración', N'Jornada de amor, chocolatada y entrega de canastas para los niños de nuestra comunidad.', DATEADD(DAY, 30, GETDATE()), DATEADD(DAY, 30, GETDATE()), N'Patio Principal de la Iglesia', 1800.00, 1, 0, NULL),
        (1, N'Vigilia General de Oración e Intercesión', N'Invitamos a toda la congregación este viernes desde las 8:00 p. m. a nuestro tiempo especial de clamor familiar.', NULL, NULL, N'Templo Central', NULL, 1, 0, NULL);
END
GO

-- -----------------------------------------------------------------------------
-- 2.2 PROCESO ETL COMPLETO (EXTRACT -> TRANSFORM -> LOAD)
-- Simulación de migración de una planilla Excel antigua con datos sucios
-- -----------------------------------------------------------------------------

-- FASE 1 DEL ETL (EXTRACT): Carga en bruto hacia tabla temporal Staging (#stg_padron_excel)
IF OBJECT_ID('tempdb..#stg_padron_excel') IS NOT NULL DROP TABLE #stg_padron_excel;
CREATE TABLE #stg_padron_excel (
    fila_origen       INT IDENTITY(1,1),
    dni_bruto         NVARCHAR(50),
    nombres_bruto     NVARCHAR(150),
    apellidos_bruto   NVARCHAR(150),
    celular_bruto     NVARCHAR(50),
    fecha_nac_bruto   NVARCHAR(50),
    direccion_bruto   NVARCHAR(300)
);

INSERT INTO #stg_padron_excel (dni_bruto, nombres_bruto, apellidos_bruto, celular_bruto, fecha_nac_bruto, direccion_bruto)
VALUES
    -- Datos con espacios extra, guiones en DNI, prefijo +51 en celular y minúsculas
    (N' 45891230 ', N'  maría elena ', N' quispe mamani ', N'+51 987-112-233', N'1985-05-14', N' Jr. Los Pinos 234, Comas '),
    (N'71234568',   N'CARLOS ALBERTO', N'ROJAS HUAMÁN',   N'991 223 344',     N'1992-11-20', N'Av. Universitaria 4510, Comas'),
    (N'09876543-A', N'  lucía fernanda', N' flores pérez', N'+51945667788',   N'1978-03-08', N'Urb. Santa Luzmila Mz C Lt 12'),
    (N'41223344',   N'jorge luis',     N'cárdenas soto',  N'955-889-900',     N'1990-08-25', N'Jr. La Unión 108, Carabayllo'),
    -- Registro duplicado a propósito para demostrar limpieza en la fase Transform
    (N'45891230',   N'Maria Elena',    N'Quispe Mamani',  N'987112233',       N'1985-05-14', N'Jr. Los Pinos 234, Comas'),
    -- Registro inválido (DNI incompleto de 5 dígitos) que el ETL descartará
    (N'12345',      N'Usuario Prueba', N'Dato Incompleto', N'12345',          N'fecha-mala', N'Sin dirección');

-- FASE 2 DEL ETL (TRANSFORM): Limpieza, normalización de cadenas, validación de reglas CHECK y deduplicación
IF OBJECT_ID('tempdb..#etl_congregante_limpio') IS NOT NULL DROP TABLE #etl_congregante_limpio;

WITH cte_limpieza AS (
    SELECT
        fila_origen,
        -- Limpiar DNI dejando solo caracteres numéricos
        REPLACE(REPLACE(LTRIM(RTRIM(dni_bruto)), N'-', N''), N'A', N'') AS dni_limpio,
        UPPER(LEFT(LTRIM(RTRIM(nombres_bruto)), 1)) + LOWER(SUBSTRING(LTRIM(RTRIM(nombres_bruto)), 2, 100)) AS nombres_limpios,
        UPPER(LEFT(LTRIM(RTRIM(apellidos_bruto)), 1)) + LOWER(SUBSTRING(LTRIM(RTRIM(apellidos_bruto)), 2, 100)) AS apellidos_limpios,
        -- Limpiar celular quitando '+51', espacios y guiones
        RIGHT(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(celular_bruto)), N'+51', N''), N'-', N''), N' ', N''), 9) AS telefono_limpio,
        TRY_CONVERT(DATETIME2, LTRIM(RTRIM(fecha_nac_bruto))) AS fecha_nacimiento_limpia,
        LTRIM(RTRIM(direccion_bruto)) AS direccion_limpia
    FROM #stg_padron_excel
),
cte_deduplicado AS (
    SELECT *,
        ROW_NUMBER() OVER (PARTITION BY dni_limpio ORDER BY fila_origen ASC) AS num_aparicion
    FROM cte_limpieza
    WHERE dni_limpio LIKE '[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
      AND telefono_limpio LIKE '9[0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'
)
SELECT
    dni_limpio AS dni,
    nombres_limpios AS nombres,
    apellidos_limpios AS apellidos,
    telefono_limpio AS telefono,
    fecha_nacimiento_limpia AS fecha_nacimiento,
    direccion_limpia AS direccion
INTO #etl_congregante_limpio
FROM cte_deduplicado
WHERE num_aparicion = 1;

-- FASE 3 DEL ETL (LOAD): Carga incremental (MERGE) hacia la tabla maestra tb_congregante
MERGE INTO [dbo].[tb_congregante] AS destino
USING #etl_congregante_limpio AS origen
ON destino.[dni] = origen.[dni]
WHEN MATCHED THEN
    UPDATE SET
        destino.[telefono]  = origen.[telefono],
        destino.[direccion] = origen.[direccion]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([dni], [nombres], [apellidos], [telefono], [fecha_nacimiento], [direccion], [activo])
    VALUES (origen.[dni], origen.[nombres], origen.[apellidos], origen.[telefono], origen.[fecha_nacimiento], origen.[direccion], 1);

DROP TABLE #stg_padron_excel;
DROP TABLE #etl_congregante_limpio;
GO

-- No se insertan donaciones, bienes, lotes, movimientos ni mensajes de demostración.
-- Los datos reales se cargan desde la aplicación una vez creado el administrador.
GO


-- =============================================================================
-- SECCIÓN 3: VISTAS DEL SISTEMA (CREATE OR ALTER VIEW)
-- =============================================================================

-- VISTA 1: Consolidado detallado de donaciones (Finanzas, Donantes y Eventos)
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
GO

-- VISTA 2: Stock consolidado y semáforo de abastecimiento de almacén
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
GO

-- VISTA 3: Avance de recaudación por Evento Solidario frente a su Meta
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
GO

-- VISTA 4: Kardex detallado de entradas, vistos buenos y salidas de almacén
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
GO

-- VISTA 5: Bandeja pastoral de peticiones de oración y contacto
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
GO


-- =============================================================================
-- SECCIÓN 4: PROCEDIMIENTOS ALMACENADOS (CREATE OR ALTER PROCEDURE)
-- =============================================================================

-- PROCEDIMIENTO 1: Registrar una donación monetaria validando donante y operación única
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
GO

-- PROCEDIMIENTO 2: Evaluar (Aprobar / Rechazar) una donación y generar lotes si es de bienes
CREATE OR ALTER PROCEDURE [dbo].[sp_evaluar_donacion]
    @id_donacion      INT,
    @nuevo_estado     INT, -- 1 = Aprobada, 2 = Rechazada
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

        -- Si es donación de bienes físicos y se aprueba, generar lotes pendientes de revisión para almacén
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
GO

-- PROCEDIMIENTO 3: Confirmar el ingreso físico de un lote en Almacén (Visto Bueno)
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
GO

-- PROCEDIMIENTO 4: Registrar salida de almacén para Obra Social validando saldo disponible
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
GO

-- PROCEDIMIENTO 5: Reporte gerencial de la iglesia por rango de fechas
CREATE OR ALTER PROCEDURE [dbo].[sp_reporte_general_iglesia]
    @fecha_inicio DATE,
    @fecha_fin    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        @fecha_inicio AS [desde],
        @fecha_fin AS [hasta],
        -- Indicadores financieros
        (SELECT ISNULL(SUM([monto]), 0) FROM [dbo].[tb_donacion]
         WHERE [tipo] = 0 AND [estado] = 1 AND [fecha_recepcion_pago] BETWEEN @fecha_inicio AND @fecha_fin) AS [total_soles_aprobados],
        (SELECT COUNT(*) FROM [dbo].[tb_donacion]
         WHERE [estado] = 0 AND [fecha_recepcion_pago] BETWEEN @fecha_inicio AND @fecha_fin) AS [donaciones_pendientes],
        -- Indicadores de almacén y obra social
        (SELECT ISNULL(SUM([cantidad]), 0) FROM [dbo].[tb_movimiento_almacen]
         WHERE [tipo] = 0 AND [fecha_operacion] BETWEEN @fecha_inicio AND @fecha_fin) AS [unidades_bienes_recibidos],
        (SELECT ABS(ISNULL(SUM([cantidad]), 0)) FROM [dbo].[tb_movimiento_almacen]
         WHERE [tipo] = 3 AND [fecha_operacion] BETWEEN @fecha_inicio AND @fecha_fin) AS [unidades_entregadas_obra_social],
        -- Indicadores pastorales
        (SELECT COUNT(*) FROM [dbo].[tb_mensaje_contacto]
         WHERE CAST([fecha_creacion] AS DATE) BETWEEN @fecha_inicio AND @fecha_fin) AS [peticiones_oracion_recibidas],
        (SELECT COUNT(*) FROM [dbo].[tb_mensaje_contacto]
         WHERE [estado] = 1 AND CAST([fecha_creacion] AS DATE) BETWEEN @fecha_inicio AND @fecha_fin) AS [peticiones_oracion_atendidas];
END
GO


-- =============================================================================
-- SECCIÓN 5: CONSULTAS DE ANÁLISIS Y DEMOSTRACIÓN ACADÉMICA (SELECT)
-- =============================================================================

-- CONSULTA 1: Avance de recaudación financiera por Evento frente a su Meta (Uso de Vista)
SELECT
    [id_evento],
    [titulo] AS [evento_solidario],
    [meta_recaudacion],
    [monto_recaudado],
    [porcentaje_avance],
    [estado_evento]
FROM [dbo].[vw_avance_eventos]
ORDER BY [fecha_inicio_evento] DESC;

-- CONSULTA 2: Ranking de miembros congregantes con mayor participación en donaciones
SELECT
    c.[dni],
    CONCAT(c.[nombres], N' ', c.[apellidos]) AS [miembro_congregante],
    c.[telefono],
    COUNT(d.[id]) AS [total_operaciones],
    ISNULL(SUM(CASE WHEN d.[tipo] = 0 AND d.[estado] = 1 THEN d.[monto] ELSE 0 END), 0) AS [total_donado_soles],
    SUM(CASE WHEN d.[tipo] = 1 AND d.[estado] = 1 THEN 1 ELSE 0 END) AS [entregas_viveres]
FROM [dbo].[tb_congregante] c
INNER JOIN [dbo].[tb_donacion] d ON d.[id_congregante] = c.[id]
GROUP BY c.[id], c.[dni], c.[nombres], c.[apellidos], c.[telefono]
HAVING COUNT(d.[id]) >= 1
ORDER BY [total_donado_soles] DESC, [total_operaciones] DESC;

-- CONSULTA 3: Trazabilidad completa de Obra Social (6 tablas conectadas: Miembro -> Donación -> Bien -> Producto -> Lote -> Salida Kardex)
SELECT
    CONCAT(c.[nombres], N' ', c.[apellidos]) AS [donante_origen],
    d.[id] AS [nro_donacion],
    pr.[categoria],
    pr.[nombre] AS [producto],
    db.[cantidad] AS [cantidad_donada],
    l.[cantidad_actual] AS [saldo_en_estante],
    m.[cantidad] AS [cantidad_entregada],
    m.[destino] AS [beneficiario_obra_social],
    per.[nombre_completo] AS [almacenero_responsable],
    m.[fecha_operacion]
FROM [dbo].[tb_movimiento_almacen] m
INNER JOIN [dbo].[tb_lote] l           ON l.[id] = m.[id_lote]
INNER JOIN [dbo].[tb_producto] pr      ON pr.[id] = l.[id_producto]
INNER JOIN [dbo].[tb_donacion_bien] db ON db.[id] = l.[id_donacion_bien]
INNER JOIN [dbo].[tb_donacion] d       ON d.[id] = db.[id_donacion]
LEFT JOIN  [dbo].[tb_congregante] c    ON c.[id] = d.[id_congregante]
LEFT JOIN  [dbo].[tb_personal] per     ON per.[id_usuario] = m.[id_usuario]
WHERE m.[tipo] = 3;

-- CONSULTA 4: Semáforo de Inventario por Categoría y Producto
SELECT
    [categoria],
    [producto],
    [unidad],
    [stock_disponible],
    [stock_pendiente_revision],
    [estado_inventario]
FROM [dbo].[vw_stock_general_almacen]
ORDER BY [categoria], [producto];

-- CONSULTA 5: Tiempos de respuesta pastoral en la Bandeja de Peticiones de Oración
SELECT
    [id_mensaje],
    [remitente],
    [tipo_remitente],
    [motivo_contacto],
    [estado_atencion],
    [atendido_por],
    DATEDIFF(MINUTE, [fecha_creacion], ISNULL([fecha_atencion], SYSDATETIMEOFFSET())) AS [minutos_transcurridos]
FROM [dbo].[vw_mensajes_atencion]
ORDER BY [fecha_creacion] DESC;
GO

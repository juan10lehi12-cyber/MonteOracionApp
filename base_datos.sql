-- =============================================================================
-- SCRIPT SQL SERVER - SISTEMA MONTE DE ORACIÓN
-- Todas las tablas y atributos están definidos en español (snake_case)
-- =============================================================================

IF DB_ID(N'MonteOracionApp') IS NULL
BEGIN
    CREATE DATABASE [MonteOracionApp];
END
GO

USE [MonteOracionApp];
GO

-- 1. TABLA DE USUARIOS
CREATE TABLE [dbo].[tb_usuario] (
    [id]                         NVARCHAR(450)  NOT NULL,
    [nombre_usuario]             NVARCHAR(256)  NULL,
    [nombre_usuario_normalizado] NVARCHAR(256)  NULL,
    [correo]                     NVARCHAR(256)  NULL,
    [correo_normalizado]         NVARCHAR(256)  NULL,
    [correo_confirmado]          BIT            NOT NULL,
    [clave_hash]                 NVARCHAR(MAX)  NULL,
    [sello_seguridad]            NVARCHAR(MAX)  NULL,
    [sello_concurrencia]         NVARCHAR(MAX)  NULL,
    [telefono]                   NVARCHAR(MAX)  NULL,
    [telefono_confirmado]        BIT            NOT NULL,
    [doble_factor_activo]        BIT            NOT NULL,
    [fin_bloqueo]                DATETIMEOFFSET NULL,
    [bloqueo_activo]             BIT            NOT NULL,
    [intentos_fallidos]          INT            NOT NULL,
    CONSTRAINT [PK_tb_usuario] PRIMARY KEY ([id])
);
CREATE UNIQUE INDEX [UserNameIndex] ON [dbo].[tb_usuario] ([nombre_usuario_normalizado]) WHERE [nombre_usuario_normalizado] IS NOT NULL;
CREATE INDEX [EmailIndex] ON [dbo].[tb_usuario] ([correo_normalizado]);
GO

-- 2. TABLA DE ROLES
CREATE TABLE [dbo].[tb_rol] (
    [id]                 NVARCHAR(450) NOT NULL,
    [nombre]             NVARCHAR(256) NULL,
    [nombre_normalizado] NVARCHAR(256) NULL,
    [sello_concurrencia] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_tb_rol] PRIMARY KEY ([id])
);
CREATE UNIQUE INDEX [RoleNameIndex] ON [dbo].[tb_rol] ([nombre_normalizado]) WHERE [nombre_normalizado] IS NOT NULL;
GO

-- 3. TABLA DE ROLES POR USUARIO
CREATE TABLE [dbo].[tb_usuario_rol] (
    [id_usuario] NVARCHAR(450) NOT NULL,
    [id_rol]     NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_tb_usuario_rol] PRIMARY KEY ([id_usuario], [id_rol]),
    CONSTRAINT [FK_tb_usuario_rol_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_tb_usuario_rol_tb_rol]     FOREIGN KEY ([id_rol])     REFERENCES [dbo].[tb_rol] ([id])     ON DELETE CASCADE
);
CREATE INDEX [IX_tb_usuario_rol_id_rol] ON [dbo].[tb_usuario_rol] ([id_rol]);
GO

-- 4. TABLA DE ATRIBUTOS (CLAIMS) DE USUARIO
CREATE TABLE [dbo].[tb_usuario_atributo] (
    [id]             INT           IDENTITY(1,1) NOT NULL,
    [id_usuario]     NVARCHAR(450) NOT NULL,
    [tipo_atributo]  NVARCHAR(MAX) NULL,
    [valor_atributo] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_tb_usuario_atributo] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_usuario_atributo_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]) ON DELETE CASCADE
);
CREATE INDEX [IX_tb_usuario_atributo_id_usuario] ON [dbo].[tb_usuario_atributo] ([id_usuario]);
GO

-- 5. TABLA DE ATRIBUTOS (CLAIMS) DE ROL
CREATE TABLE [dbo].[tb_rol_atributo] (
    [id]             INT           IDENTITY(1,1) NOT NULL,
    [id_rol]         NVARCHAR(450) NOT NULL,
    [tipo_atributo]  NVARCHAR(MAX) NULL,
    [valor_atributo] NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_tb_rol_atributo] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_rol_atributo_tb_rol] FOREIGN KEY ([id_rol]) REFERENCES [dbo].[tb_rol] ([id]) ON DELETE CASCADE
);
CREATE INDEX [IX_tb_rol_atributo_id_rol] ON [dbo].[tb_rol_atributo] ([id_rol]);
GO

-- 6. TABLA DE ACCESOS EXTERNOS DE USUARIO
CREATE TABLE [dbo].[tb_usuario_acceso_externo] (
    [proveedor_acceso]         NVARCHAR(128) NOT NULL,
    [clave_proveedor]          NVARCHAR(128) NOT NULL,
    [nombre_visible_proveedor] NVARCHAR(MAX) NULL,
    [id_usuario]               NVARCHAR(450) NOT NULL,
    CONSTRAINT [PK_tb_usuario_acceso_externo] PRIMARY KEY ([proveedor_acceso], [clave_proveedor]),
    CONSTRAINT [FK_tb_usuario_acceso_externo_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]) ON DELETE CASCADE
);
CREATE INDEX [IX_tb_usuario_acceso_externo_id_usuario] ON [dbo].[tb_usuario_acceso_externo] ([id_usuario]);
GO

-- 7. TABLA DE TOKENS DE USUARIO
CREATE TABLE [dbo].[tb_usuario_token] (
    [id_usuario]       NVARCHAR(450) NOT NULL,
    [proveedor_acceso] NVARCHAR(128) NOT NULL,
    [nombre]           NVARCHAR(128) NOT NULL,
    [valor]            NVARCHAR(MAX) NULL,
    CONSTRAINT [PK_tb_usuario_token] PRIMARY KEY ([id_usuario], [proveedor_acceso], [nombre]),
    CONSTRAINT [FK_tb_usuario_token_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id]) ON DELETE CASCADE
);
GO

-- 8. TABLA DE PERSONAL INTERNO
CREATE TABLE [dbo].[tb_personal] (
    [id_usuario]      NVARCHAR(450) NOT NULL,
    [nombre_completo] NVARCHAR(160) NOT NULL,
    [cargo]           NVARCHAR(100) NULL,
    [activo]          BIT           NOT NULL,
    CONSTRAINT [PK_tb_personal] PRIMARY KEY ([id_usuario]),
    CONSTRAINT [FK_tb_personal_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id])
);
GO

-- 9. TABLA DE MIEMBROS / CONGREGANTES
CREATE TABLE [dbo].[tb_congregante] (
    [id]               INT           IDENTITY(1,1) NOT NULL,
    [dni]              NVARCHAR(8)   NULL,
    [nombres]          NVARCHAR(100) NOT NULL,
    [apellidos]        NVARCHAR(100) NOT NULL,
    [telefono]         NVARCHAR(9)   NULL,
    [fecha_nacimiento] DATETIME2     NULL,
    [direccion]        NVARCHAR(250) NULL,
    [activo]           BIT           NOT NULL,
    [id_usuario]       NVARCHAR(450) NULL,
    CONSTRAINT [PK_tb_congregante] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_congregante_tb_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id])
);
CREATE UNIQUE INDEX [IX_tb_congregante_dni] ON [dbo].[tb_congregante] ([dni]) WHERE [dni] IS NOT NULL;
CREATE UNIQUE INDEX [IX_tb_congregante_id_usuario] ON [dbo].[tb_congregante] ([id_usuario]) WHERE [id_usuario] IS NOT NULL;
GO

-- 10. TABLA DE CONFIGURACIÓN DE LA IGLESIA
CREATE TABLE [dbo].[tb_iglesia] (
    [id]          INT            NOT NULL,
    [nombre]      NVARCHAR(150)  NOT NULL,
    [informacion] NVARCHAR(3000) NOT NULL,
    [direccion]   NVARCHAR(300)  NULL,
    [contacto]    NVARCHAR(100)  NULL,
    [horarios]    NVARCHAR(600)  NULL,
    CONSTRAINT [PK_tb_iglesia] PRIMARY KEY ([id]),
    CONSTRAINT [CK_ChurchSettings_Singleton] CHECK ([id] = 1)
);
GO

-- 11. TABLA DE MEDIOS DE PAGO
CREATE TABLE [dbo].[tb_medio_pago] (
    [id]                INT           IDENTITY(1,1) NOT NULL,
    [nombre]            NVARCHAR(100) NOT NULL,
    [instrucciones]     NVARCHAR(500) NOT NULL,
    [numero_cuenta]     NVARCHAR(50)  NULL,
    [clave_qr]          NVARCHAR(MAX) NULL,
    [tipo_contenido_qr] NVARCHAR(MAX) NULL,
    [activo]            BIT           NOT NULL,
    CONSTRAINT [PK_tb_medio_pago] PRIMARY KEY ([id])
);
GO

-- 12. TABLA DE EVENTOS Y PUBLICACIONES
CREATE TABLE [dbo].[tb_publicacion] (
    [id]                  INT            IDENTITY(1,1) NOT NULL,
    [tipo]                INT            NOT NULL,
    [titulo]              NVARCHAR(160)  NOT NULL,
    [contenido]           NVARCHAR(MAX)  NOT NULL,
    [fecha_inicio_evento] DATETIME2      NULL,
    [fecha_fin_evento]    DATETIME2      NULL,
    [clave_imagen]        NVARCHAR(MAX)  NULL,
    [tipo_imagen]         NVARCHAR(MAX)  NULL,
    [lugar]               NVARCHAR(250)  NULL,
    [meta_recaudacion]    DECIMAL(12,2)  NULL,
    [publicado]           BIT            NOT NULL,
    [cancelado]           BIT            NOT NULL,
    [fecha_creacion]      DATETIMEOFFSET NOT NULL,
    CONSTRAINT [PK_tb_publicacion] PRIMARY KEY ([id])
);
GO

-- 13. TABLA DE PRODUCTOS DE ALMACÉN
CREATE TABLE [dbo].[tb_producto] (
    [id]     INT           IDENTITY(1,1) NOT NULL,
    [nombre] NVARCHAR(160) NOT NULL,
    [unidad] NVARCHAR(40)  NOT NULL,
    [activo] BIT           NOT NULL,
    CONSTRAINT [PK_tb_producto] PRIMARY KEY ([id])
);
CREATE UNIQUE INDEX [IX_tb_producto_nombre_unidad] ON [dbo].[tb_producto] ([nombre], [unidad]);
GO

-- 14. TABLA DE DONACIONES
CREATE TABLE [dbo].[tb_donacion] (
    [id]                         INT              IDENTITY(1,1) NOT NULL,
    [codigo_seguimiento]         UNIQUEIDENTIFIER NOT NULL,
    [tipo]                       INT              NOT NULL,
    [estado]                     INT              NOT NULL,
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
    [fecha_creacion]             DATETIMEOFFSET   NOT NULL,
    [clave_comprobante]          NVARCHAR(MAX)    NULL,
    [tipo_contenido_comprobante] NVARCHAR(MAX)    NULL,
    [id_usuario_registro]        NVARCHAR(MAX)    NULL,
    [digital]                    BIT              NOT NULL DEFAULT(0),
    [version]                    ROWVERSION       NOT NULL,
    CONSTRAINT [PK_tb_donacion] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_donacion_tb_congregante] FOREIGN KEY ([id_congregante]) REFERENCES [dbo].[tb_congregante] ([id]),
    CONSTRAINT [FK_tb_donacion_tb_publicacion] FOREIGN KEY ([id_evento])      REFERENCES [dbo].[tb_publicacion] ([id]),
    CONSTRAINT [FK_tb_donacion_tb_medio_pago]  FOREIGN KEY ([id_medio_pago])  REFERENCES [dbo].[tb_medio_pago] ([id]),
    CONSTRAINT [CK_Donation_Amount] CHECK (([tipo] = 0 AND [monto] > 0 AND [monto] IS NOT NULL) OR ([tipo] = 1 AND [monto] IS NULL)),
    CONSTRAINT [CK_Donation_Donor]  CHECK (([tipo_donante] = 0 AND [id_congregante] IS NOT NULL) OR ([tipo_donante] = 1 AND [id_congregante] IS NULL AND [nombre_externo] IS NOT NULL) OR ([tipo_donante] = 2 AND [id_congregante] IS NULL AND [nombre_externo] IS NULL))
);
CREATE UNIQUE INDEX [IX_tb_donacion_codigo_seguimiento] ON [dbo].[tb_donacion] ([codigo_seguimiento]);
CREATE UNIQUE INDEX [IX_tb_donacion_operacion_unica] ON [dbo].[tb_donacion] ([id_medio_pago], [fecha_recepcion_pago], [numero_operacion]) WHERE [numero_operacion] IS NOT NULL;
CREATE INDEX [IX_tb_donacion_id_evento] ON [dbo].[tb_donacion] ([id_evento]);
CREATE INDEX [IX_tb_donacion_estado_fecha] ON [dbo].[tb_donacion] ([estado], [fecha_creacion]);
CREATE INDEX [IX_tb_donacion_id_congregante] ON [dbo].[tb_donacion] ([id_congregante]);
GO

-- 15. TABLA DE BIENES DONADOS (DETALLE DE DONACIÓN)
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
GO

-- 16. TABLA DE HISTORIAL DE DECISIONES DE DONACIÓN
CREATE TABLE [dbo].[tb_donacion_historial] (
    [id]               INT            IDENTITY(1,1) NOT NULL,
    [id_donacion]      INT            NOT NULL,
    [estado]           INT            NOT NULL,
    [id_administrador] NVARCHAR(450)  NOT NULL,
    [fecha]            DATETIMEOFFSET NOT NULL,
    [motivo]           NVARCHAR(1000) NULL,
    CONSTRAINT [PK_tb_donacion_historial] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_donacion_historial_tb_donacion] FOREIGN KEY ([id_donacion])      REFERENCES [dbo].[tb_donacion] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_tb_donacion_historial_tb_usuario]  FOREIGN KEY ([id_administrador]) REFERENCES [dbo].[tb_usuario] ([id])
);
CREATE INDEX [IX_tb_donacion_historial_id_donacion] ON [dbo].[tb_donacion_historial] ([id_donacion]);
CREATE INDEX [IX_tb_donacion_historial_id_administrador] ON [dbo].[tb_donacion_historial] ([id_administrador]);
GO

-- 17. TABLA DE EXISTENCIAS / LOTES EN ALMACÉN
CREATE TABLE [dbo].[tb_lote] (
    [id]                   INT            IDENTITY(1,1) NOT NULL,
    [id_producto]          INT            NOT NULL,
    [id_donacion_bien]     INT            NULL,
    [ubicacion]            NVARCHAR(160)  NOT NULL,
    [origen]               NVARCHAR(200)  NOT NULL,
    [cantidad_actual]      DECIMAL(12,2)  NOT NULL,
    [revision_pendiente]   BIT            NOT NULL,
    [id_usuario_pendiente] NVARCHAR(450)  NULL,
    [fecha_pendiente]      DATETIMEOFFSET NULL,
    [confirmado_por]       NVARCHAR(450)  NULL,
    [fecha_confirmacion]   DATETIMEOFFSET NULL,
    [version]              ROWVERSION     NOT NULL,
    CONSTRAINT [PK_tb_lote] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_lote_tb_producto]      FOREIGN KEY ([id_producto])      REFERENCES [dbo].[tb_producto] ([id]),
    CONSTRAINT [FK_tb_lote_tb_donacion_bien] FOREIGN KEY ([id_donacion_bien]) REFERENCES [dbo].[tb_donacion_bien] ([id]),
    CONSTRAINT [CK_Lote_Saldos] CHECK ([cantidad_actual] >= 0)
);
CREATE UNIQUE INDEX [IX_tb_lote_id_donacion_bien] ON [dbo].[tb_lote] ([id_donacion_bien]) WHERE [id_donacion_bien] IS NOT NULL;
CREATE INDEX [IX_tb_lote_id_producto] ON [dbo].[tb_lote] ([id_producto]);
GO

-- 18. TABLA DE MOVIMIENTOS DE ALMACÉN (ENTRADAS Y SALIDAS)
CREATE TABLE [dbo].[tb_movimiento_almacen] (
    [id]              BIGINT           IDENTITY(1,1) NOT NULL,
    [id_comando]      UNIQUEIDENTIFIER NULL,
    [id_lote]         INT              NOT NULL,
    [tipo]            INT              NOT NULL,
    [cantidad]        DECIMAL(12,2)    NOT NULL,
    [id_usuario]      NVARCHAR(450)    NOT NULL,
    [motivo]          NVARCHAR(1000)   NOT NULL,
    [destino]         NVARCHAR(160)    NULL,
    [fecha_hora]      DATETIMEOFFSET   NOT NULL,
    [fecha_operacion] DATETIME2        NOT NULL,
    CONSTRAINT [PK_tb_movimiento_almacen] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tb_movimiento_almacen_tb_lote] FOREIGN KEY ([id_lote]) REFERENCES [dbo].[tb_lote] ([id])
);
CREATE INDEX [IX_tb_movimiento_almacen_id_lote] ON [dbo].[tb_movimiento_almacen] ([id_lote]);
CREATE INDEX [IX_tb_movimiento_almacen_fecha_operacion] ON [dbo].[tb_movimiento_almacen] ([fecha_operacion]);
CREATE UNIQUE INDEX [IX_tb_movimiento_almacen_id_comando] ON [dbo].[tb_movimiento_almacen] ([id_comando]) WHERE [id_comando] IS NOT NULL;
GO

-- DATOS INICIALES BASE (ROLES E IGLESIA)
IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'ADMINISTRADOR')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (NEWID(), N'Administrador', N'ADMINISTRADOR', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_rol] WHERE [nombre_normalizado] = N'PERSONAL')
    INSERT INTO [dbo].[tb_rol] ([id], [nombre], [nombre_normalizado], [sello_concurrencia])
    VALUES (NEWID(), N'Personal', N'PERSONAL', NEWID());

IF NOT EXISTS (SELECT 1 FROM [dbo].[tb_iglesia] WHERE [id] = 1)
    INSERT INTO [dbo].[tb_iglesia] ([id], [nombre], [informacion])
    VALUES (1, N'Iglesia Monte de Oración', N'Bienvenidos a nuestra comunidad de fe en Comas, Lima.');
GO

-- =============================================================================
-- ADVERTENCIA DE SEGURIDAD
-- Este archivo es material académico y NO forma parte de la aplicación ASP.NET Core.
-- Usa un esquema distinto (MonteOracionAcademico) y conserva credenciales de
-- laboratorio en texto plano. No ejecutar en producción ni reutilizar sus claves.
-- La base real de la aplicación se crea con base_datos.sql y sus migraciones.
-- =============================================================================
-- SCRIPT 100% ACADÉMICO PARA LA UNIVERSIDAD: "IGLESIA MONTE DE ORACIÓN"
-- =============================================================================
-- BASE DE DATOS: MonteOracionAcademico
-- ENFOQUE: Database-First Puro (Sin artefactos de framework C# / Identity / ORM)
-- NORMALIZACIÓN: 1FN, 2FN y 3FN en el 100% de las tablas
-- CONVENCIÓN: Clave Primaria [id_tabla] <---> Clave Foránea [id_tabla]
-- =============================================================================

IF DB_ID(N'MonteOracionAcademico') IS NULL
BEGIN
    CREATE DATABASE [MonteOracionAcademico];
END
GO

USE [MonteOracionAcademico];
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================================================
-- SECCIÓN 1: CREACIÓN DE LAS 15 TABLAS NORMALIZADAS (3FN)
-- =============================================================================

-- -----------------------------------------------------------------------------
-- MÓDULO 1: SEGURIDAD, ROLES Y PERSONAL
-- -----------------------------------------------------------------------------

-- 1. TABLA: tb_usuario (Cuentas de usuario limpias)
IF OBJECT_ID(N'dbo.tb_usuario', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_usuario] (
        [id_usuario]        INT IDENTITY(1,1) NOT NULL,
        [nombre_usuario]    VARCHAR(50)       NOT NULL,
        [clave]             VARCHAR(255)      NOT NULL,
        [correo]            VARCHAR(100)      NOT NULL,
        [telefono]          VARCHAR(20)       NULL,
        [activo]            BIT               NOT NULL CONSTRAINT [DF_usuario_activo] DEFAULT (1),
        [fecha_registro]    DATETIME          NOT NULL CONSTRAINT [DF_usuario_fecha] DEFAULT (GETDATE()),
        CONSTRAINT [PK_tb_usuario] PRIMARY KEY ([id_usuario]),
        CONSTRAINT [UQ_tb_usuario_nombre] UNIQUE ([nombre_usuario]),
        CONSTRAINT [UQ_tb_usuario_correo] UNIQUE ([correo]),
        CONSTRAINT [CK_tb_usuario_nombre_min] CHECK (LEN(LTRIM(RTRIM([nombre_usuario]))) >= 3)
    );
END
GO

-- 2. TABLA: tb_rol (Catálogo clásico de roles)
IF OBJECT_ID(N'dbo.tb_rol', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_rol] (
        [id_rol]            INT IDENTITY(1,1) NOT NULL,
        [nombre_rol]        VARCHAR(50)       NOT NULL,
        [descripcion]       VARCHAR(200)      NULL,
        CONSTRAINT [PK_tb_rol] PRIMARY KEY ([id_rol]),
        CONSTRAINT [UQ_tb_rol_nombre] UNIQUE ([nombre_rol])
    );
END
GO

-- 3. TABLA: tb_usuario_rol (Tabla puente N:M)
IF OBJECT_ID(N'dbo.tb_usuario_rol', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_usuario_rol] (
        [id_usuario]        INT NOT NULL,
        [id_rol]            INT NOT NULL,
        CONSTRAINT [PK_tb_usuario_rol] PRIMARY KEY ([id_usuario], [id_rol]),
        CONSTRAINT [FK_tb_usuario_rol_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_usuario_rol_rol]     FOREIGN KEY ([id_rol])     REFERENCES [dbo].[tb_rol]     ([id_rol])     ON DELETE CASCADE
    );
END
GO

-- 4. TABLA: tb_personal (Datos del trabajador / staff de la iglesia)
IF OBJECT_ID(N'dbo.tb_personal', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_personal] (
        [id_personal]       INT IDENTITY(1,1) NOT NULL,
        [id_usuario]        INT               NOT NULL,
        [nombre_completo]   VARCHAR(150)      NOT NULL,
        [cargo]             VARCHAR(100)      NOT NULL,
        [activo]            BIT               NOT NULL CONSTRAINT [DF_personal_activo] DEFAULT (1),
        CONSTRAINT [PK_tb_personal] PRIMARY KEY ([id_personal]),
        CONSTRAINT [UQ_tb_personal_usuario] UNIQUE ([id_usuario]),
        CONSTRAINT [FK_tb_personal_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]),
        CONSTRAINT [CK_tb_personal_nombre]  CHECK (LEN(LTRIM(RTRIM([nombre_completo]))) >= 3)
    );
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 2: GESTIÓN INSTITUCIONAL, MEDIOS DE PAGO Y COMUNIDAD
-- -----------------------------------------------------------------------------

-- 5. TABLA: tb_iglesia (Configuración de la sede central)
IF OBJECT_ID(N'dbo.tb_iglesia', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_iglesia] (
        [id_iglesia]        INT IDENTITY(1,1) NOT NULL,
        [nombre_iglesia]    VARCHAR(200)      NOT NULL,
        [ruc]               VARCHAR(20)       NULL,
        [direccion]         VARCHAR(300)      NOT NULL,
        [telefono]          VARCHAR(50)       NULL,
        [correo]            VARCHAR(150)      NULL,
        [id_usuario]        INT               NULL, -- Usuario administrador que configuró
        [fecha_actualizo]   DATETIME          NULL,
        CONSTRAINT [PK_tb_iglesia] PRIMARY KEY ([id_iglesia]),
        CONSTRAINT [UQ_tb_iglesia_ruc] UNIQUE ([ruc]),
        CONSTRAINT [FK_tb_iglesia_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario])
    );
END
GO

-- 6. TABLA: tb_medio_pago (Cuentas bancarias y billeteras: Yape, Plin, BCP)
IF OBJECT_ID(N'dbo.tb_medio_pago', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_medio_pago] (
        [id_medio_pago]     INT IDENTITY(1,1) NOT NULL,
        [nombre_medio]      VARCHAR(100)      NOT NULL,
        [tipo_medio]        VARCHAR(50)       NOT NULL, -- 'Billetera Digital', 'Cuenta Bancaria', 'Efectivo'
        [numero_cuenta]     VARCHAR(80)       NULL,
        [titular]           VARCHAR(150)      NULL,
        [activo]            BIT               NOT NULL CONSTRAINT [DF_medio_pago_activo] DEFAULT (1),
        [id_usuario]        INT               NULL, -- Usuario que registró la cuenta
        CONSTRAINT [PK_tb_medio_pago] PRIMARY KEY ([id_medio_pago]),
        CONSTRAINT [FK_tb_medio_pago_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]),
        CONSTRAINT [CK_tb_medio_pago_tipo] CHECK ([tipo_medio] IN ('Billetera Digital', 'Cuenta Bancaria', 'Efectivo'))
    );
END
GO

-- 7. TABLA: tb_publicacion (Campañas de recaudación y eventos)
IF OBJECT_ID(N'dbo.tb_publicacion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_publicacion] (
        [id_publicacion]    INT IDENTITY(1,1) NOT NULL,
        [titulo]            VARCHAR(200)      NOT NULL,
        [tipo]              VARCHAR(50)       NOT NULL, -- 'Evento', 'Noticia'
        [descripcion]       VARCHAR(MAX)      NOT NULL,
        [lugar]             VARCHAR(200)      NULL,
        [fecha_inicio]      DATETIME          NULL,
        [fecha_fin]         DATETIME          NULL,
        [meta_recaudacion]  DECIMAL(12,2)     NULL,
        [cancelado]         BIT               NOT NULL CONSTRAINT [DF_publicacion_cancelado] DEFAULT (0),
        [id_usuario]        INT               NOT NULL, -- Usuario creador
        [fecha_creacion]    DATETIME          NOT NULL CONSTRAINT [DF_publicacion_fecha] DEFAULT (GETDATE()),
        CONSTRAINT [PK_tb_publicacion] PRIMARY KEY ([id_publicacion]),
        CONSTRAINT [FK_tb_publicacion_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]),
        CONSTRAINT [CK_tb_publicacion_tipo]    CHECK ([tipo] IN ('Evento', 'Noticia')),
        CONSTRAINT [CK_tb_publicacion_meta]    CHECK ([meta_recaudacion] IS NULL OR [meta_recaudacion] > 0),
        CONSTRAINT [CK_tb_publicacion_fechas]  CHECK ([fecha_fin] IS NULL OR [fecha_inicio] IS NULL OR [fecha_fin] >= [fecha_inicio])
    );
END
GO

-- 8. TABLA: tb_congregante (Directorio de miembros y familias)
IF OBJECT_ID(N'dbo.tb_congregante', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_congregante] (
        [id_congregante]    INT IDENTITY(1,1) NOT NULL,
        [dni]               VARCHAR(16)       NULL,
        [nombres]           VARCHAR(100)      NOT NULL,
        [apellidos]         VARCHAR(100)      NOT NULL,
        [telefono]          VARCHAR(20)       NULL,
        [direccion]         VARCHAR(250)      NULL,
        [fecha_nacimiento]  DATE              NULL,
        [id_usuario]        INT               NULL, -- Cuenta de usuario opcional
        [activo]            BIT               NOT NULL CONSTRAINT [DF_congregante_activo] DEFAULT (1),
        CONSTRAINT [PK_tb_congregante] PRIMARY KEY ([id_congregante]),
        CONSTRAINT [UQ_tb_congregante_dni] UNIQUE ([dni]),
        CONSTRAINT [FK_tb_congregante_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]),
        CONSTRAINT [CK_tb_congregante_nombres] CHECK (LEN([nombres]) >= 2 AND LEN([apellidos]) >= 2)
    );
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 3: DONACIONES Y COMPROBANTES
-- -----------------------------------------------------------------------------

-- 9. TABLA: tb_producto (Catálogo de bienes físicos para ayuda social)
IF OBJECT_ID(N'dbo.tb_producto', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_producto] (
        [id_producto]       INT IDENTITY(1,1) NOT NULL,
        [nombre_producto]   VARCHAR(150)      NOT NULL,
        [categoria]         VARCHAR(80)       NOT NULL,
        [unidad_medida]     VARCHAR(30)       NOT NULL,
        [activo]            BIT               NOT NULL CONSTRAINT [DF_producto_activo] DEFAULT (1),
        CONSTRAINT [PK_tb_producto] PRIMARY KEY ([id_producto]),
        CONSTRAINT [UQ_tb_producto_nombre_unidad] UNIQUE ([nombre_producto], [unidad_medida])
    );
END
GO

-- 10. TABLA: tb_donacion (Registro maestro de aportes)
IF OBJECT_ID(N'dbo.tb_donacion', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion] (
        [id_donacion]          INT IDENTITY(1,1) NOT NULL,
        [codigo_seguimiento]   VARCHAR(50)       NOT NULL,
        [tipo_donacion]        VARCHAR(50)       NOT NULL, -- 'Dinero', 'Bienes'
        [estado]               VARCHAR(50)       NOT NULL CONSTRAINT [DF_donacion_estado] DEFAULT ('Pendiente'),
        [tipo_donante]         VARCHAR(50)       NOT NULL, -- 'Congregante', 'Externo', 'Anonimo'
        [id_congregante]       INT               NULL,
        [nombre_externo]       VARCHAR(150)      NULL,
        [contacto_externo]     VARCHAR(150)      NULL,
        [monto]                DECIMAL(12,2)     NULL,
        [id_medio_pago]        INT               NULL,
        [numero_operacion]     VARCHAR(80)       NULL,
        [id_publicacion]       INT               NULL, -- Campaña o evento destino
        [id_usuario]           INT               NULL, -- Usuario que registró
        [fecha_pago]           DATE              NOT NULL,
        [fecha_registro]       DATETIME          NOT NULL CONSTRAINT [DF_donacion_fecha] DEFAULT (GETDATE()),
        [digital]              BIT               NOT NULL CONSTRAINT [DF_donacion_digital] DEFAULT (0),
        CONSTRAINT [PK_tb_donacion] PRIMARY KEY ([id_donacion]),
        CONSTRAINT [UQ_tb_donacion_codigo] UNIQUE ([codigo_seguimiento]),
        CONSTRAINT [FK_tb_donacion_congregante] FOREIGN KEY ([id_congregante]) REFERENCES [dbo].[tb_congregante] ([id_congregante]),
        CONSTRAINT [FK_tb_donacion_medio_pago]  FOREIGN KEY ([id_medio_pago])  REFERENCES [dbo].[tb_medio_pago]  ([id_medio_pago]),
        CONSTRAINT [FK_tb_donacion_publicacion] FOREIGN KEY ([id_publicacion]) REFERENCES [dbo].[tb_publicacion] ([id_publicacion]),
        CONSTRAINT [FK_tb_donacion_usuario]     FOREIGN KEY ([id_usuario])     REFERENCES [dbo].[tb_usuario]     ([id_usuario]),
        CONSTRAINT [CK_tb_donacion_tipo]   CHECK ([tipo_donacion] IN ('Dinero', 'Bienes')),
        CONSTRAINT [CK_tb_donacion_estado] CHECK ([estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Anulada')),
        CONSTRAINT [CK_tb_donacion_monto]  CHECK ([monto] IS NULL OR [monto] > 0)
    );
END
GO

-- 11. TABLA: tb_donacion_bien (Detalle de bienes físicos recibidos)
IF OBJECT_ID(N'dbo.tb_donacion_bien', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion_bien] (
        [id_donacion_bien]  INT IDENTITY(1,1) NOT NULL,
        [id_donacion]       INT               NOT NULL,
        [id_producto]       INT               NOT NULL,
        [descripcion]       VARCHAR(200)      NOT NULL,
        [cantidad]          DECIMAL(12,2)     NOT NULL,
        [unidad_medida]     VARCHAR(30)       NOT NULL,
        [ubicacion_almacen] VARCHAR(150)      NULL,
        CONSTRAINT [PK_tb_donacion_bien] PRIMARY KEY ([id_donacion_bien]),
        CONSTRAINT [FK_tb_donacion_bien_donacion] FOREIGN KEY ([id_donacion]) REFERENCES [dbo].[tb_donacion] ([id_donacion]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_donacion_bien_producto] FOREIGN KEY ([id_producto]) REFERENCES [dbo].[tb_producto] ([id_producto]),
        CONSTRAINT [CK_tb_donacion_bien_cantidad] CHECK ([cantidad] > 0)
    );
END
GO

-- 12. TABLA: tb_donacion_historial (Pistas de auditoría de aprobaciones)
IF OBJECT_ID(N'dbo.tb_donacion_historial', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_donacion_historial] (
        [id_historial]      INT IDENTITY(1,1) NOT NULL,
        [id_donacion]       INT               NOT NULL,
        [id_usuario]        INT               NOT NULL, -- Administrador que tomó la decisión
        [estado_resultado]  VARCHAR(50)       NOT NULL,
        [motivo]            VARCHAR(500)      NULL,
        [fecha_decision]    DATETIME          NOT NULL CONSTRAINT [DF_historial_fecha] DEFAULT (GETDATE()),
        CONSTRAINT [PK_tb_donacion_historial] PRIMARY KEY ([id_historial]),
        CONSTRAINT [FK_tb_donacion_historial_donacion] FOREIGN KEY ([id_donacion]) REFERENCES [dbo].[tb_donacion] ([id_donacion]) ON DELETE CASCADE,
        CONSTRAINT [FK_tb_donacion_historial_usuario]  FOREIGN KEY ([id_usuario])  REFERENCES [dbo].[tb_usuario]  ([id_usuario])
    );
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 4: ALMACÉN, INVENTARIOS Y KARDEX DE OBRA SOCIAL
-- -----------------------------------------------------------------------------

-- 13. TABLA: tb_lote (Existencias físicas por estantería o almacén)
IF OBJECT_ID(N'dbo.tb_lote', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_lote] (
        [id_lote]              INT IDENTITY(1,1) NOT NULL,
        [id_producto]          INT               NOT NULL,
        [id_donacion_bien]     INT               NULL,
        [ubicacion]            VARCHAR(150)      NOT NULL,
        [origen]               VARCHAR(200)      NOT NULL,
        [cantidad_actual]      DECIMAL(12,2)     NOT NULL,
        [revision_pendiente]   BIT               NOT NULL CONSTRAINT [DF_lote_revision] DEFAULT (1),
        [id_usuario]           INT               NULL, -- Quien registró
        [confirmado_por]       INT               NULL, -- Almacenero que dio Visto Bueno
        [fecha_registro]       DATETIME          NOT NULL CONSTRAINT [DF_lote_fecha] DEFAULT (GETDATE()),
        [fecha_confirmacion]   DATETIME          NULL,
        CONSTRAINT [PK_tb_lote] PRIMARY KEY ([id_lote]),
        CONSTRAINT [FK_tb_lote_producto]      FOREIGN KEY ([id_producto])      REFERENCES [dbo].[tb_producto]      ([id_producto]),
        CONSTRAINT [FK_tb_lote_donacion_bien] FOREIGN KEY ([id_donacion_bien]) REFERENCES [dbo].[tb_donacion_bien] ([id_donacion_bien]),
        CONSTRAINT [FK_tb_lote_usuario_reg]   FOREIGN KEY ([id_usuario])       REFERENCES [dbo].[tb_usuario]       ([id_usuario]),
        CONSTRAINT [FK_tb_lote_usuario_conf]  FOREIGN KEY ([confirmado_por])   REFERENCES [dbo].[tb_usuario]       ([id_usuario]),
        CONSTRAINT [CK_tb_lote_saldo]         CHECK ([cantidad_actual] >= 0)
    );
END
GO

-- 14. TABLA: tb_movimiento_almacen (Kardex: Entradas, Visto Bueno, Salidas a Comedores)
IF OBJECT_ID(N'dbo.tb_movimiento_almacen', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_movimiento_almacen] (
        [id_movimiento]     BIGINT IDENTITY(1,1) NOT NULL,
        [id_lote]           INT                  NOT NULL,
        [tipo_movimiento]   VARCHAR(50)          NOT NULL, -- 'Entrada Donacion', 'Visto Bueno', 'Salida Obra Social'
        [cantidad]          DECIMAL(12,2)        NOT NULL,
        [id_usuario]        INT                  NOT NULL, -- Responsable
        [motivo]            VARCHAR(500)         NOT NULL,
        [destino]           VARCHAR(150)         NULL,
        [fecha_operacion]   DATE                 NOT NULL,
        [fecha_registro]    DATETIME             NOT NULL CONSTRAINT [DF_movimiento_fecha] DEFAULT (GETDATE()),
        CONSTRAINT [PK_tb_movimiento_almacen] PRIMARY KEY ([id_movimiento]),
        CONSTRAINT [FK_tb_movimiento_lote]    FOREIGN KEY ([id_lote])    REFERENCES [dbo].[tb_lote]    ([id_lote]),
        CONSTRAINT [FK_tb_movimiento_usuario] FOREIGN KEY ([id_usuario]) REFERENCES [dbo].[tb_usuario] ([id_usuario]),
        CONSTRAINT [CK_tb_movimiento_cantidad] CHECK ([cantidad] <> 0)
    );
END
GO

-- -----------------------------------------------------------------------------
-- MÓDULO 5: CONSEJERÍA PASTORAL Y MENSAJES
-- -----------------------------------------------------------------------------

-- 15. TABLA: tb_mensaje_contacto (Peticiones de oración y consejería pastoral)
IF OBJECT_ID(N'dbo.tb_mensaje_contacto', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[tb_mensaje_contacto] (
        [id_mensaje]        INT IDENTITY(1,1) NOT NULL,
        [id_congregante]    INT               NULL,
        [nombre_remitente]  VARCHAR(150)      NOT NULL,
        [telefono]          VARCHAR(40)       NULL,
        [correo]            VARCHAR(150)      NULL,
        [motivo]            VARCHAR(50)       NOT NULL, -- 'Oracion', 'Consejeria', 'Horarios', 'Servicio'
        [preferencia]       VARCHAR(50)       NOT NULL, -- 'Llamada', 'WhatsApp', 'Indiferente'
        [mensaje]           VARCHAR(MAX)      NOT NULL,
        [estado]            VARCHAR(50)       NOT NULL CONSTRAINT [DF_mensaje_estado] DEFAULT ('Pendiente'),
        [id_usuario]        INT               NULL, -- Pastor o secretaria que atendió
        [fecha_registro]    DATETIME          NOT NULL CONSTRAINT [DF_mensaje_fecha] DEFAULT (GETDATE()),
        [fecha_atencion]    DATETIME          NULL,
        [notas_atencion]    VARCHAR(MAX)      NULL,
        CONSTRAINT [PK_tb_mensaje_contacto] PRIMARY KEY ([id_mensaje]),
        CONSTRAINT [FK_tb_mensaje_congregante] FOREIGN KEY ([id_congregante]) REFERENCES [dbo].[tb_congregante] ([id_congregante]),
        CONSTRAINT [FK_tb_mensaje_usuario]     FOREIGN KEY ([id_usuario])     REFERENCES [dbo].[tb_usuario]     ([id_usuario]),
        CONSTRAINT [CK_tb_mensaje_motivo]      CHECK ([motivo] IN ('Oracion', 'Consejeria', 'Horarios', 'Servicio', 'Otro')),
        CONSTRAINT [CK_tb_mensaje_preferencia] CHECK ([preferencia] IN ('Llamada', 'WhatsApp', 'Indiferente')),
        CONSTRAINT [CK_tb_mensaje_estado]      CHECK ([estado] IN ('Pendiente', 'Atendido', 'Archivado'))
    );
END
GO


-- =============================================================================
-- SECCIÓN 2: INSERCIÓN DE DATOS DE PRUEBA (PROCESO ETL)
-- =============================================================================

-- A) Roles del Sistema
INSERT INTO [dbo].[tb_rol] ([nombre_rol], [descripcion])
VALUES
    ('Administrador', 'Control total de finanzas, directiva y aprobaciones.'),
    ('Recepcion',     'Registro de congregantes, ofrendas presenciales y mensajes.'),
    ('Almacen',       'Control de inventario de víveres, lotes y entregas a comedores.');

-- B) Usuarios del Sistema (Contraseñas de ejemplo)
INSERT INTO [dbo].[tb_usuario] ([nombre_usuario], [clave], [correo], [telefono])
VALUES
    ('director.pastor',      'Admin123*#', 'director@monteoracion.pe', '987654321'),
    ('recepcion.secretaria', 'Recep123*#', 'recepcion@monteoracion.pe', '987654322'),
    ('encargado.almacen',    'Almac123*#', 'almacen@monteoracion.pe',   '987654323');

-- C) Asignación de Roles a Usuarios
INSERT INTO [dbo].[tb_usuario_rol] ([id_usuario], [id_rol])
VALUES
    (1, 1), -- director -> Administrador
    (2, 2), -- recepcion -> Recepcion
    (3, 3); -- almacen -> Almacen

-- D) Personal Interno
INSERT INTO [dbo].[tb_personal] ([id_usuario], [nombre_completo], [cargo])
VALUES
    (1, 'Pr. Samuel Huamán Rojas',   'Pastor Principal / Director'),
    (2, 'Hna. Ruth Mendoza Vargas',  'Secretaria General y Recepción'),
    (3, 'Hno. David Quispe Paredes', 'Encargado de Logística y Obra Social');

-- E) Configuración Institucional
INSERT INTO [dbo].[tb_iglesia] ([nombre_iglesia], [ruc], [direccion], [telefono], [correo], [id_usuario])
VALUES
    ('Iglesia Evangélica Monte de Oración', '20601234567', 'Av. Túpac Amaru Km 11, Comas - Lima', '01-5432100', 'contacto@monteoracion.pe', 1);

-- F) Medios de Pago
INSERT INTO [dbo].[tb_medio_pago] ([nombre_medio], [tipo_medio], [numero_cuenta], [titular], [id_usuario])
VALUES
    ('Yape Oficial', 'Billetera Digital', '987654321',        'Iglesia Monte de Oración', 1),
    ('Plin Parroquia', 'Billetera Digital', '987654322',      'Iglesia Monte de Oración', 1),
    ('BCP Cuenta Soles', 'Cuenta Bancaria', '191-98765432-0-12', 'Iglesia Monte de Oración', 1),
    ('Caja Presencial', 'Efectivo', NULL, 'Recepción Sede Central', 1);

-- G) Publicaciones y Campañas
INSERT INTO [dbo].[tb_publicacion] ([titulo], [tipo], [descripcion], [lugar], [fecha_inicio], [fecha_fin], [meta_recaudacion], [id_usuario])
VALUES
    ('Chocolatada Navideña en Comas', 'Evento', 'Recaudación de fondos y juguetes para 300 niños de escasos recursos en la zona alta.', 'Comedor El Mirador - Comas', '2026-12-20', '2026-12-21', 1500.00, 1),
    ('Vigilia de Oración y Avivamiento', 'Evento', 'Noche de intercesión y clamor por las familias de la comunidad.', 'Templo Central', '2026-11-28', '2026-11-29', NULL, 1);

-- H) Congregantes Registrados
INSERT INTO [dbo].[tb_congregante] ([dni], [nombres], [apellidos], [telefono], [direccion], [fecha_nacimiento])
VALUES
    ('76199170', 'Naasón', 'Santana Gómez', '919182734', 'Jr. Las Palmeras 123, Comas', '1998-05-14'),
    ('45678912', 'María Elena', 'Quispe Rojas', '987654321', 'Av. Belaúnde Este 456, Comas', '1985-09-22'),
    ('42345678', 'Pedro Pablo', 'Sánchez Ríos', '933221100', 'Calle Los Jazmines 789, Carabayllo', '1980-02-10');

-- I) Catálogo de Bienes Físicos
INSERT INTO [dbo].[tb_producto] ([nombre_producto], [categoria], [unidad_medida])
VALUES
    ('Arroz Superior', 'Alimentos y Víveres', 'kg'),
    ('Aceite Vegetal', 'Alimentos y Víveres', 'litro'),
    ('Leche Evaporada', 'Alimentos y Víveres', 'tarro'),
    ('Fideos Cortos', 'Alimentos y Víveres', 'paquete');

-- J) Donaciones de Prueba (Monetarias y Físicas)
INSERT INTO [dbo].[tb_donacion] ([codigo_seguimiento], [tipo_donacion], [estado], [tipo_donante], [id_congregante], [monto], [id_medio_pago], [numero_operacion], [id_publicacion], [id_usuario], [fecha_pago], [digital])
VALUES
    ('DON-2026-001', 'Dinero', 'Aprobada', 'Congregante', 1, 200.00, 1, 'OP-782190', 1, 2, '2026-10-14', 1),
    ('DON-2026-002', 'Dinero', 'Aprobada', 'Congregante', 2, 150.00, 3, 'BCP-44512', 1, 2, '2026-10-15', 1),
    ('DON-2026-003', 'Bienes', 'Aprobada', 'Congregante', 3, NULL, NULL, NULL, NULL, 2, '2026-10-16', 0);

-- K) Detalle de Bienes Donados
INSERT INTO [dbo].[tb_donacion_bien] ([id_donacion], [id_producto], [descripcion], [cantidad], [unidad_medida], [ubicacion_almacen])
VALUES
    (3, 1, 'Donación de 50 kg de arroz en sacos limpios.', 50.00, 'kg', 'Estante A - Nivel 1'),
    (3, 2, 'Donación de 20 botellas de aceite vegetal.', 20.00, 'litro', 'Estante B - Nivel 2');

-- L) Historial de Decisiones
INSERT INTO [dbo].[tb_donacion_historial] ([id_donacion], [id_usuario], [estado_resultado], [motivo])
VALUES
    (1, 1, 'Aprobada', 'Comprobante verificado en cuenta Yape.'),
    (2, 1, 'Aprobada', 'Abono confirmado en extracto BCP.'),
    (3, 1, 'Aprobada', 'Mercadería física recibida en secretaría.');

-- M) Lotes en Almacén
INSERT INTO [dbo].[tb_lote] ([id_producto], [id_donacion_bien], [ubicacion], [origen], [cantidad_actual], [revision_pendiente], [id_usuario], [confirmado_por], [fecha_confirmacion])
VALUES
    (1, 1, 'Estante A - Nivel 1', 'Donación #3 (Pedro Sánchez)', 35.00, 0, 2, 3, GETDATE()),
    (2, 2, 'Estante B - Nivel 2', 'Donación #3 (Pedro Sánchez)', 20.00, 1, 2, NULL, NULL);

-- N) Movimientos de Almacén (Kardex)
INSERT INTO [dbo].[tb_movimiento_almacen] ([id_lote], [tipo_movimiento], [cantidad], [id_usuario], [motivo], [destino], [fecha_operacion])
VALUES
    (1, 'Entrada Donacion', 50.00, 2, 'Ingreso inicial por donación #3', 'Almacén Central', '2026-10-16'),
    (1, 'Visto Bueno',      50.00, 3, 'Conformidad de peso y empaque.', 'Estante A - Nivel 1', '2026-10-16'),
    (1, 'Salida Obra Social', -15.00, 3, 'Entrega al Comedor Popular Collique.', 'Comedor Collique', '2026-10-18');

-- O) Mensajes de Contacto
INSERT INTO [dbo].[tb_mensaje_contacto] ([id_congregante], [nombre_remitente], [telefono], [correo], [motivo], [preferencia], [mensaje], [estado], [id_usuario], [fecha_atencion], [notas_atencion])
VALUES
    (2, 'María Elena Quispe Rojas', '987654321', 'maria.quispe@correo.pe', 'Oracion', 'WhatsApp', 'Hermanos, ruego oración por la salud de mi madre en el hospital de Collique.', 'Atendido', 2, GETDATE(), 'Se coordinó visita pastoral y llamada telefónica.'),
    (NULL, 'Juan Carlos Mendoza', '966554433', 'juan.mendoza@gmail.com', 'Horarios', 'Llamada', 'Buenas tardes, quisiera información sobre los cultos de jóvenes.', 'Pendiente', NULL, NULL, NULL);
GO


-- =============================================================================
-- SECCIÓN 3: VISTAS DEL SISTEMA (CREATE VIEW)
-- =============================================================================

-- VISTA 1: Reporte consolidado de donaciones
CREATE OR ALTER VIEW [dbo].[vw_donaciones_detalladas] AS
SELECT
    d.[id_donacion],
    d.[codigo_seguimiento],
    d.[tipo_donacion],
    d.[estado] AS [estado_donacion],
    CASE d.[tipo_donante]
        WHEN 'Congregante' THEN CONCAT(c.[nombres], ' ', c.[apellidos])
        WHEN 'Externo'     THEN ISNULL(d.[nombre_externo], 'Donante Externo')
        ELSE 'Anónimo'
    END AS [nombre_donante],
    c.[dni] AS [dni_donante],
    d.[monto],
    ISNULL(mp.[nombre_medio], 'Efectivo / Presencial') AS [medio_pago],
    d.[numero_operacion],
    ISNULL(p.[titulo], 'Obra Social General') AS [campana_evento],
    d.[fecha_pago],
    d.[fecha_registro],
    per.[nombre_completo] AS [registrado_por]
FROM [dbo].[tb_donacion] d
LEFT JOIN [dbo].[tb_congregante] c ON c.[id_congregante] = d.[id_congregante]
LEFT JOIN [dbo].[tb_medio_pago] mp ON mp.[id_medio_pago] = d.[id_medio_pago]
LEFT JOIN [dbo].[tb_publicacion] p ON p.[id_publicacion] = d.[id_publicacion]
LEFT JOIN [dbo].[tb_personal] per  ON per.[id_usuario]   = d.[id_usuario];
GO

-- VISTA 2: Kardex completo de movimientos de almacén
CREATE OR ALTER VIEW [dbo].[vw_kardex_movimientos] AS
SELECT
    m.[id_movimiento],
    m.[fecha_operacion],
    m.[fecha_registro],
    m.[tipo_movimiento],
    pr.[categoria],
    pr.[nombre_producto],
    ABS(m.[cantidad]) AS [cantidad],
    pr.[unidad_medida],
    l.[id_lote],
    l.[ubicacion] AS [ubicacion_estante],
    l.[origen] AS [origen_mercaderia],
    m.[motivo],
    ISNULL(m.[destino], 'Almacén Central') AS [destino],
    per.[nombre_completo] AS [responsable]
FROM [dbo].[tb_movimiento_almacen] m
INNER JOIN [dbo].[tb_lote] l        ON l.[id_lote]        = m.[id_lote]
INNER JOIN [dbo].[tb_producto] pr   ON pr.[id_producto]   = l.[id_producto]
INNER JOIN [dbo].[tb_personal] per  ON per.[id_usuario]   = m.[id_usuario];
GO

-- VISTA 3: Control de stock por producto
CREATE OR ALTER VIEW [dbo].[vw_stock_general_almacen] AS
SELECT
    pr.[id_producto],
    pr.[categoria],
    pr.[nombre_producto],
    pr.[unidad_medida],
    COUNT(l.[id_lote]) AS [total_lotes],
    ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) AS [stock_disponible],
    ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 1 THEN l.[cantidad_actual] ELSE 0 END), 0) AS [stock_por_revisar],
    CASE
        WHEN ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) = 0 THEN 'SIN STOCK'
        WHEN ISNULL(SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END), 0) <= 10 THEN 'STOCK CRÍTICO'
        ELSE 'STOCK ÓPTIMO'
    END AS [estado_inventario]
FROM [dbo].[tb_producto] pr
LEFT JOIN [dbo].[tb_lote] l ON l.[id_producto] = pr.[id_producto] AND l.[cantidad_actual] > 0
GROUP BY pr.[id_producto], pr.[categoria], pr.[nombre_producto], pr.[unidad_medida];
GO

-- VISTA 4: Recaudación y avance de metas por evento
CREATE OR ALTER VIEW [dbo].[vw_avance_eventos] AS
SELECT
    p.[id_publicacion],
    p.[titulo] AS [evento],
    p.[lugar],
    p.[fecha_inicio],
    p.[fecha_fin],
    ISNULL(p.[meta_recaudacion], 0) AS [meta_soles],
    COUNT(d.[id_donacion]) AS [cantidad_aportes],
    ISNULL(SUM(d.[monto]), 0) AS [monto_recaudado],
    CASE
        WHEN ISNULL(p.[meta_recaudacion], 0) > 0
        THEN CAST(ROUND((ISNULL(SUM(d.[monto]), 0) * 100.0) / p.[meta_recaudacion], 2) AS DECIMAL(10,2))
        ELSE 0
    END AS [porcentaje_avance]
FROM [dbo].[tb_publicacion] p
LEFT JOIN [dbo].[tb_donacion] d ON d.[id_publicacion] = p.[id_publicacion] AND d.[estado] = 'Aprobada'
WHERE p.[tipo] = 'Evento'
GROUP BY p.[id_publicacion], p.[titulo], p.[lugar], p.[fecha_inicio], p.[fecha_fin], p.[meta_recaudacion];
GO

-- VISTA 5: Peticiones de oración y atención pastoral
CREATE OR ALTER VIEW [dbo].[vw_mensajes_atencion] AS
SELECT
    mc.[id_mensaje],
    mc.[fecha_registro],
    mc.[nombre_remitente],
    CASE WHEN mc.[id_congregante] IS NOT NULL THEN 'Miembro de la Iglesia' ELSE 'Visitante Externo' END AS [tipo_remitente],
    c.[dni] AS [dni_miembro],
    mc.[telefono],
    mc.[correo],
    mc.[motivo],
    mc.[preferencia],
    mc.[mensaje],
    mc.[estado],
    mc.[fecha_atencion],
    per.[nombre_completo] AS [atendido_por],
    mc.[notas_atencion]
FROM [dbo].[tb_mensaje_contacto] mc
LEFT JOIN [dbo].[tb_congregante] c ON c.[id_congregante] = mc.[id_congregante]
LEFT JOIN [dbo].[tb_personal] per  ON per.[id_usuario]     = mc.[id_usuario];
GO


-- =============================================================================
-- SECCIÓN 4: PROCEDIMIENTOS ALMACENADOS (CREATE PROCEDURE)
-- =============================================================================

-- PROCEDIMIENTO 1: Registrar donación monetaria con validación de duplicidad
CREATE OR ALTER PROCEDURE [dbo].[sp_registrar_donacion_monetaria]
    @id_congregante     INT = NULL,
    @nombre_externo     VARCHAR(150) = NULL,
    @monto              DECIMAL(12,2),
    @id_medio_pago      INT,
    @numero_operacion   VARCHAR(80) = NULL,
    @id_publicacion     INT = NULL,
    @id_usuario         INT = NULL,
    @id_donacion_out    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @monto IS NULL OR @monto <= 0
            THROW 50001, 'El monto donado debe ser mayor a cero.', 1;

        IF @numero_operacion IS NOT NULL AND EXISTS (
            SELECT 1 FROM [dbo].[tb_donacion]
            WHERE [id_medio_pago] = @id_medio_pago
              AND [fecha_pago] = CAST(GETDATE() AS DATE)
              AND [numero_operacion] = @numero_operacion
        )
            THROW 50002, 'Ese número de operación ya fue registrado el día de hoy.', 1;

        BEGIN TRANSACTION;

        INSERT INTO [dbo].[tb_donacion] (
            [codigo_seguimiento], [tipo_donacion], [estado], [tipo_donante],
            [id_congregante], [nombre_externo], [monto], [id_medio_pago],
            [numero_operacion], [id_publicacion], [id_usuario], [fecha_pago], [digital]
        )
        VALUES (
            CONCAT('DON-', FORMAT(GETDATE(), 'yyyyMMdd'), '-', NEWID()),
            'Dinero', 'Aprobada',
            CASE WHEN @id_congregante IS NOT NULL THEN 'Congregante' WHEN @nombre_externo IS NOT NULL THEN 'Externo' ELSE 'Anonimo' END,
            @id_congregante, @nombre_externo, @monto, @id_medio_pago,
            @numero_operacion, @id_publicacion, @id_usuario, CAST(GETDATE() AS DATE), 1
        );

        SET @id_donacion_out = SCOPE_IDENTITY();

        INSERT INTO [dbo].[tb_donacion_historial] ([id_donacion], [id_usuario], [estado_resultado], [motivo])
        VALUES (@id_donacion_out, @id_usuario, 'Aprobada', 'Registro de ingreso confirmado.');

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- PROCEDIMIENTO 2: Registrar salida de almacén para obra social
CREATE OR ALTER PROCEDURE [dbo].[sp_registrar_salida_almacen]
    @id_lote            INT,
    @cantidad_salida    DECIMAL(12,2),
    @id_usuario         INT,
    @destino            VARCHAR(150),
    @motivo             VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF @cantidad_salida IS NULL OR @cantidad_salida <= 0
            THROW 50010, 'La cantidad retirada debe ser mayor a cero.', 1;

        DECLARE @saldoActual DECIMAL(12,2), @pendiente BIT;
        SELECT @saldoActual = [cantidad_actual], @pendiente = [revision_pendiente]
        FROM [dbo].[tb_lote] WITH (UPDLOCK)
        WHERE [id_lote] = @id_lote;

        IF @saldoActual IS NULL
            THROW 50011, 'El lote seleccionado no existe.', 1;
        IF @pendiente = 1
            THROW 50012, 'No se puede retirar mercadería de un lote pendiente de revisión.', 1;
        IF @saldoActual < @cantidad_salida
            THROW 50013, 'Stock insuficiente en el lote.', 1;

        BEGIN TRANSACTION;

        UPDATE [dbo].[tb_lote]
        SET [cantidad_actual] = [cantidad_actual] - @cantidad_salida
        WHERE [id_lote] = @id_lote;

        INSERT INTO [dbo].[tb_movimiento_almacen] (
            [id_lote], [tipo_movimiento], [cantidad], [id_usuario],
            [motivo], [destino], [fecha_operacion]
        )
        VALUES (
            @id_lote, 'Salida Obra Social', -@cantidad_salida, @id_usuario,
            @motivo, @destino, CAST(GETDATE() AS DATE)
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- PROCEDIMIENTO 3: Reporte General Ejecutivo de la Iglesia por Rango de Fechas
CREATE OR ALTER PROCEDURE [dbo].[sp_reporte_general_iglesia]
    @fecha_inicio DATE,
    @fecha_fin    DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        @fecha_inicio AS [desde],
        @fecha_fin    AS [hasta],
        ISNULL((
            SELECT SUM([monto])
            FROM [dbo].[tb_donacion]
            WHERE [estado] = 'Aprobada'
              AND [fecha_pago] BETWEEN @fecha_inicio AND @fecha_fin
        ), 0) AS [total_soles_aprobados],
        (
            SELECT COUNT(*)
            FROM [dbo].[tb_donacion]
            WHERE [estado] = 'Pendiente'
              AND [fecha_pago] BETWEEN @fecha_inicio AND @fecha_fin
        ) AS [donaciones_pendientes],
        ISNULL((
            SELECT SUM(db.[cantidad])
            FROM [dbo].[tb_donacion_bien] db
            INNER JOIN [dbo].[tb_donacion] d ON d.[id_donacion] = db.[id_donacion]
            WHERE d.[estado] = 'Aprobada'
              AND d.[fecha_pago] BETWEEN @fecha_inicio AND @fecha_fin
        ), 0) AS [unidades_viveres_recibidos],
        ISNULL((
            SELECT SUM(ABS(m.[cantidad]))
            FROM [dbo].[tb_movimiento_almacen] m
            WHERE m.[tipo_movimiento] = 'Salida Obra Social'
              AND m.[fecha_operacion] BETWEEN @fecha_inicio AND @fecha_fin
        ), 0) AS [unidades_entregadas_comedores],
        (
            SELECT COUNT(*)
            FROM [dbo].[tb_mensaje_contacto]
            WHERE [motivo] = 'Oracion'
              AND CAST([fecha_registro] AS DATE) BETWEEN @fecha_inicio AND @fecha_fin
        ) AS [peticiones_oracion_recibidas];
END;
GO


-- =============================================================================
-- SECCIÓN 5: CONSULTAS ANALÍTICAS AVANZADAS (JOIN, GROUP BY, HAVING, SUBQUERIES)
-- =============================================================================

-- CONSULTA 1: Ranking de congregantes con mayor apoyo solidario (JOIN + GROUP BY + HAVING)
SELECT
    c.[dni],
    CONCAT(c.[nombres], ' ', c.[apellidos]) AS [congregante],
    c.[telefono],
    COUNT(d.[id_donacion]) AS [total_aportes],
    SUM(d.[monto]) AS [monto_total_aportado]
FROM [dbo].[tb_congregante] c
INNER JOIN [dbo].[tb_donacion] d ON d.[id_congregante] = c.[id_congregante]
WHERE d.[estado] = 'Aprobada' AND d.[tipo_donacion] = 'Dinero'
GROUP BY c.[dni], c.[nombres], c.[apellidos], c.[telefono]
HAVING SUM(d.[monto]) >= 100.00
ORDER BY [monto_total_aportado] DESC;

-- CONSULTA 2: Estado de existencias por categoría de víveres (Subconsulta y CASE)
SELECT
    pr.[categoria],
    COUNT(DISTINCT pr.[id_producto]) AS [variedad_productos],
    SUM(CASE WHEN l.[revision_pendiente] = 0 THEN l.[cantidad_actual] ELSE 0 END) AS [saldo_disponible_total]
FROM [dbo].[tb_producto] pr
LEFT JOIN [dbo].[tb_lote] l ON l.[id_producto] = pr.[id_producto]
GROUP BY pr.[categoria];
GO

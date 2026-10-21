-- Importar desde phpMyAdmin > Importar. MariaDB 10.4+ / MySQL 8.0.16+.
-- Base nueva independiente: no convierte ni modifica SQL Server.
CREATE DATABASE IF NOT EXISTS monte_oracion_java CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE monte_oracion_java;
CREATE TABLE IF NOT EXISTS personal (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, nombre VARCHAR(160) NOT NULL,
 email VARCHAR(254) NOT NULL UNIQUE, cargo VARCHAR(100), password_hash VARCHAR(300) NOT NULL,
 activo BOOLEAN NOT NULL DEFAULT TRUE, fallos INT NOT NULL DEFAULT 0, bloqueado_hasta DATETIME NULL,
 creado TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS personal_rol (
 personal_id BIGINT NOT NULL, rol ENUM('ADMINISTRADOR','TESORERO','ALMACENERO') NOT NULL,
 PRIMARY KEY(personal_id,rol), FOREIGN KEY(personal_id) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS congregante (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, dni CHAR(8) NULL UNIQUE,
 nombres VARCHAR(100) NOT NULL, apellidos VARCHAR(100) NOT NULL, telefono VARCHAR(30), consulta_codigo CHAR(36) NOT NULL UNIQUE,
 activo BOOLEAN NOT NULL DEFAULT TRUE, CHECK(dni IS NULL OR dni REGEXP '^[0-9]{8}$')
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS iglesia (
 id INT PRIMARY KEY, nombre VARCHAR(150) NOT NULL, informacion VARCHAR(3000) NOT NULL,
 direccion VARCHAR(300), contacto VARCHAR(100), CHECK(id=1)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS medio_pago (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, nombre VARCHAR(100) NOT NULL,
 instrucciones VARCHAR(500) NOT NULL, qr_clave VARCHAR(80), qr_tipo VARCHAR(80), activo BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS fondo (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, nombre VARCHAR(120) NOT NULL UNIQUE,
 finalidad VARCHAR(500) NOT NULL, activo BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS producto (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, nombre VARCHAR(160) NOT NULL, unidad VARCHAR(40) NOT NULL,
 categoria VARCHAR(100) NOT NULL, requiere_vencimiento BOOLEAN NOT NULL DEFAULT FALSE,
 activo BOOLEAN NOT NULL DEFAULT TRUE, UNIQUE(nombre,unidad)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS beneficiario (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, nombre VARCHAR(160) NOT NULL, direccion VARCHAR(250) NOT NULL,
 contacto VARCHAR(160) NOT NULL, telefono VARCHAR(30), activo BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS donacion (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, codigo CHAR(36) NOT NULL UNIQUE,
 tipo ENUM('DINERO','BIENES') NOT NULL, estado ENUM('PENDIENTE','APROBADA','RECHAZADA','ANULADA') NOT NULL,
 donante_tipo ENUM('CONGREGANTE','EXTERNO','ANONIMO') NOT NULL, congregante_id BIGINT,
 externo_nombre VARCHAR(180), externo_contacto VARCHAR(150), importe DECIMAL(12,2),
 fondo_id BIGINT NOT NULL, medio_pago_id BIGINT NULL, medio_nombre VARCHAR(100), operacion VARCHAR(80),
 fecha DATE NOT NULL, creado TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
 archivo_clave VARCHAR(80), archivo_tipo VARCHAR(80), registrado_por BIGINT NULL, version BIGINT NOT NULL DEFAULT 0,
 FOREIGN KEY(congregante_id) REFERENCES congregante(id), FOREIGN KEY(fondo_id) REFERENCES fondo(id),
 FOREIGN KEY(medio_pago_id) REFERENCES medio_pago(id), FOREIGN KEY(registrado_por) REFERENCES personal(id),
 UNIQUE(medio_pago_id,fecha,operacion), INDEX(estado,fecha),
 CHECK((tipo='DINERO' AND importe IS NOT NULL AND importe>0) OR (tipo='BIENES' AND importe IS NULL)),
 CHECK((donante_tipo='CONGREGANTE' AND congregante_id IS NOT NULL) OR
 (donante_tipo='EXTERNO' AND congregante_id IS NULL AND externo_nombre IS NOT NULL) OR
 (donante_tipo='ANONIMO' AND congregante_id IS NULL AND externo_nombre IS NULL))
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS donacion_bien (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, donacion_id BIGINT NOT NULL, producto_id BIGINT NOT NULL,
 descripcion VARCHAR(250) NOT NULL, cantidad DECIMAL(12,2) NOT NULL, unidad VARCHAR(40) NOT NULL,
 vencimiento DATE, ubicacion VARCHAR(160) NOT NULL, CHECK(cantidad>0),
 FOREIGN KEY(donacion_id) REFERENCES donacion(id), FOREIGN KEY(producto_id) REFERENCES producto(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS donacion_decision (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, donacion_id BIGINT NOT NULL, estado VARCHAR(20) NOT NULL,
 actor_id BIGINT NOT NULL, motivo VARCHAR(1000), creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
 FOREIGN KEY(donacion_id) REFERENCES donacion(id), FOREIGN KEY(actor_id) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS lote (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, producto_id BIGINT NOT NULL, donacion_bien_id BIGINT NULL UNIQUE,
 ubicacion VARCHAR(160) NOT NULL, origen VARCHAR(200) NOT NULL, vencimiento DATE NULL,
 existencia DECIMAL(12,2) NOT NULL DEFAULT 0, reservado DECIMAL(12,2) NOT NULL DEFAULT 0,
 version BIGINT NOT NULL DEFAULT 0, CHECK(existencia>=0 AND reservado>=0 AND reservado<=existencia),
 FOREIGN KEY(producto_id) REFERENCES producto(id), FOREIGN KEY(donacion_bien_id) REFERENCES donacion_bien(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS solicitud_bienes (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, beneficiario_id BIGINT NOT NULL,
 beneficiario_nombre VARCHAR(160) NOT NULL, beneficiario_direccion VARCHAR(250) NOT NULL, beneficiario_contacto VARCHAR(160) NOT NULL,
 motivo VARCHAR(1000) NOT NULL, prioridad ENUM('NORMAL','ALTA','URGENTE') NOT NULL,
 tipo ENUM('ENTREGA','USO_INTERNO','PRESTAMO') NOT NULL,
 estado ENUM('PENDIENTE','APROBADA','PARCIAL','ENTREGADA','RECHAZADA','CANCELADA') NOT NULL DEFAULT 'PENDIENTE',
 necesaria DATE NOT NULL, devolucion DATE, solicitante_id BIGINT NOT NULL, aprobador_id BIGINT NULL,
 decision_motivo VARCHAR(1000), creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, version BIGINT NOT NULL DEFAULT 0,
 FOREIGN KEY(beneficiario_id) REFERENCES beneficiario(id), FOREIGN KEY(solicitante_id) REFERENCES personal(id),
 FOREIGN KEY(aprobador_id) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS solicitud_bien (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, solicitud_id BIGINT NOT NULL, lote_id BIGINT NOT NULL,
 cantidad DECIMAL(12,2) NOT NULL, entregado DECIMAL(12,2) NOT NULL DEFAULT 0,
 devuelto DECIMAL(12,2) NOT NULL DEFAULT 0, condonado DECIMAL(12,2) NOT NULL DEFAULT 0,
 UNIQUE(solicitud_id,lote_id), CHECK(cantidad>0 AND entregado>=0 AND entregado<=cantidad AND devuelto>=0 AND condonado>=0 AND devuelto+condonado<=entregado),
 FOREIGN KEY(solicitud_id) REFERENCES solicitud_bienes(id), FOREIGN KEY(lote_id) REFERENCES lote(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS movimiento_almacen (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, comando CHAR(36) NULL UNIQUE, lote_id BIGINT NOT NULL, linea_id BIGINT NULL,
 tipo ENUM('DONACION','RESERVA','LIBERACION','ENTREGA','REPOSICION','MERMA','AJUSTE_POSITIVO','AJUSTE_NEGATIVO','CONDONACION') NOT NULL,
 cantidad DECIMAL(12,2) NOT NULL, reserva_cambio DECIMAL(12,2) NOT NULL DEFAULT 0,
 actor_id BIGINT NOT NULL, motivo VARCHAR(1000) NOT NULL, receptor VARCHAR(160),
 archivo_clave VARCHAR(80), archivo_tipo VARCHAR(80), fecha DATE NOT NULL,
 creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, INDEX(fecha),
 FOREIGN KEY(lote_id) REFERENCES lote(id), FOREIGN KEY(linea_id) REFERENCES solicitud_bien(id), FOREIGN KEY(actor_id) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS solicitud_dinero (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, fondo_id BIGINT NOT NULL,
 canal ENUM('EFECTIVO','DIGITAL') NOT NULL, tipo ENUM('GASTO','ANTICIPO','SALDO_INICIAL','AJUSTE_INGRESO','AJUSTE_SALIDA') NOT NULL,
 estado ENUM('PENDIENTE','APROBADA','PAGADA','RECHAZADA','CANCELADA','RENDIDA') NOT NULL DEFAULT 'PENDIENTE',
 importe DECIMAL(12,2) NOT NULL, motivo VARCHAR(1000) NOT NULL, destinatario VARCHAR(160) NOT NULL,
 solicitante_id BIGINT NOT NULL, aprobador_id BIGINT, pagado_por BIGINT, decision_motivo VARCHAR(1000),
 limite DATE, pagado_fecha DATE, rendido_fecha DATE, gasto_real DECIMAL(12,2),
 archivo_clave VARCHAR(80), archivo_tipo VARCHAR(80), rendicion_clave VARCHAR(80), rendicion_tipo VARCHAR(80),
 creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, version BIGINT NOT NULL DEFAULT 0,
 CHECK(importe>0 AND (gasto_real IS NULL OR (gasto_real>=0 AND gasto_real<=importe))), INDEX(fondo_id,canal,estado),
 FOREIGN KEY(fondo_id) REFERENCES fondo(id), FOREIGN KEY(solicitante_id) REFERENCES personal(id),
 FOREIGN KEY(aprobador_id) REFERENCES personal(id), FOREIGN KEY(pagado_por) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS movimiento_dinero (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, fondo_id BIGINT NOT NULL, canal ENUM('EFECTIVO','DIGITAL') NOT NULL,
 donacion_id BIGINT NULL, solicitud_id BIGINT NULL,
 tipo ENUM('DONACION','ANULACION_DONACION','GASTO','ANTICIPO','DEVOLUCION_ANTICIPO','SALDO_INICIAL','AJUSTE') NOT NULL,
 importe DECIMAL(12,2) NOT NULL, actor_id BIGINT NOT NULL, detalle VARCHAR(1000) NOT NULL,
 fecha DATE NOT NULL, creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, UNIQUE(donacion_id,tipo), INDEX(fondo_id,canal,fecha),
 FOREIGN KEY(fondo_id) REFERENCES fondo(id), FOREIGN KEY(donacion_id) REFERENCES donacion(id),
 FOREIGN KEY(solicitud_id) REFERENCES solicitud_dinero(id), FOREIGN KEY(actor_id) REFERENCES personal(id)
) ENGINE=InnoDB;
-- Mutex por canal: serializa cierres y movimientos, incluso con el libro vacío.
CREATE TABLE IF NOT EXISTS canal_control (canal VARCHAR(10) PRIMARY KEY) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS cierre_caja (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, canal ENUM('EFECTIVO','DIGITAL') NOT NULL, fecha DATE NOT NULL,
 apertura DECIMAL(12,2) NOT NULL, ingresos DECIMAL(12,2) NOT NULL, salidas DECIMAL(12,2) NOT NULL,
 esperado DECIMAL(12,2) NOT NULL, contado DECIMAL(12,2) NOT NULL, notas VARCHAR(1000) NOT NULL,
 preparado_por BIGINT NOT NULL, revisado_por BIGINT NULL, archivo_clave VARCHAR(80), archivo_tipo VARCHAR(80),
 creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, UNIQUE(fecha,canal), CHECK(contado>=0),
 FOREIGN KEY(preparado_por) REFERENCES personal(id), FOREIGN KEY(revisado_por) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS publicacion (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, tipo ENUM('EVENTO','AVISO') NOT NULL,
 titulo VARCHAR(160) NOT NULL, contenido VARCHAR(6000) NOT NULL, inicio DATETIME, fin DATETIME,
 lugar VARCHAR(250), publicado BOOLEAN NOT NULL DEFAULT FALSE, cancelado BOOLEAN NOT NULL DEFAULT FALSE,
 imagen_clave VARCHAR(80), imagen_tipo VARCHAR(80), version BIGINT NOT NULL DEFAULT 0,
 creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP, CHECK(fin IS NULL OR (inicio IS NOT NULL AND fin>=inicio))
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS participante_evento (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, publicacion_id BIGINT NOT NULL, congregante_id BIGINT NOT NULL,
 asistencia ENUM('PENDIENTE','ASISTIO','NO_ASISTIO','JUSTIFICADA') NOT NULL DEFAULT 'PENDIENTE',
 activo BOOLEAN NOT NULL DEFAULT TRUE, retiro_motivo VARCHAR(1000), nota_interna VARCHAR(1000),
 registrado_por BIGINT NOT NULL, version BIGINT NOT NULL DEFAULT 0, UNIQUE(publicacion_id,congregante_id),
 FOREIGN KEY(publicacion_id) REFERENCES publicacion(id), FOREIGN KEY(congregante_id) REFERENCES congregante(id),
 FOREIGN KEY(registrado_por) REFERENCES personal(id)
) ENGINE=InnoDB;
CREATE TABLE IF NOT EXISTS auditoria (
 id BIGINT PRIMARY KEY AUTO_INCREMENT, actor_id BIGINT NOT NULL, accion VARCHAR(160) NOT NULL,
 referencia VARCHAR(160) NOT NULL, detalle VARCHAR(1000) NOT NULL, creado TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
 INDEX(creado), FOREIGN KEY(actor_id) REFERENCES personal(id)
) ENGINE=InnoDB;
INSERT INTO iglesia(id,nombre,informacion,direccion,contacto)
 SELECT 1,'Iglesia Monte de Oración','Bienvenidos a nuestra comunidad de fe en Comas, Lima.','Comas, Lima','' WHERE NOT EXISTS(SELECT 1 FROM iglesia WHERE id=1);
INSERT INTO fondo(id,nombre,finalidad) SELECT 1,'General','Obra y necesidades de la iglesia' WHERE NOT EXISTS(SELECT 1 FROM fondo WHERE id=1);
INSERT IGNORE INTO canal_control(canal) VALUES ('EFECTIVO'),('DIGITAL');
-- Sin cuentas ni contraseñas predeterminadas. Crear administrador con scripts/crear-admin.ps1.

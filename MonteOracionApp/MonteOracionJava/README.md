# MonteOracionJava — Java + Maven + XAMPP

Aplicación web Java basada en MonteOracionApp. Proyecto Maven con empaquetado **WAR**, Servlets, JSP, JDBC y MariaDB. No es una aplicación de escritorio y no utiliza PHP. El proyecto C# se conserva.

## Qué elegir en NetBeans

En la ventana de tu captura:

| Campo | Valor |
|---|---|
| Categoría | Java with Maven |
| Proyecto | Web Application |
| Project Name / Artifact Id | MonteOracionJava |
| Group Id | pe.monteoracion |
| Version | 1.0.0 |
| Package | pe.monteoracion |
| Java | JDK 17 o 21 (se encontró JDK 21 instalado) |
| Servidor | Apache Tomcat 10.1, Jakarta EE 10, API `jakarta.servlet` 6.0 |
| Context Path | /MonteOracionJava |

**Como el código ya está creado, no necesitas crear otro proyecto vacío:** Archivo → Abrir proyecto → selecciona esta carpeta, que contiene `pom.xml`.

Usa **Tomcat 10.1**, como el 10.1.55 de tu instalación. El proyecto utiliza Jakarta Servlet 6.0. Tomcat 8.5/9 utiliza la API anterior `javax.servlet` y no ejecuta esta versión del WAR.

XAMPP muestra “MySQL”, aunque distribuye MariaDB. En este proyecto el controlador MariaDB utiliza JDBC y funciona con MariaDB y MySQL compatibles. La aplicación Java corre en Tomcat; Apache HTTP de XAMPP se necesita para phpMyAdmin. No copies fuentes Java a `htdocs`.

## 1. Crear la base en XAMPP

1. Abre XAMPP Control Panel y pulsa **Start en MySQL**. Para phpMyAdmin pulsa también Start en Apache.
2. Abre `http://localhost/phpmyadmin`.
3. Importar → selecciona [`sql/01_esquema.sql`](sql/01_esquema.sql) → Continuar.
4. Se crea **monte_oracion_java**, con tablas, claves foráneas, restricciones, fondo General y datos iniciales de la iglesia. No se insertan usuarios ni datos ficticios.
5. Abre [`sql/02_usuario.sql.example`](sql/02_usuario.sql.example), sustituye `CAMBIAR_CONTRASENA` y ejecútalo en la pestaña SQL como administrador. Otorga acceso a esta base y permite cambiar roles del personal.

Requisitos de base: MariaDB 10.4+ (XAMPP) o MySQL 8.0.16+, motor InnoDB y `utf8mb4`. El script no usa `DROP` ni modifica SQL Server. `IF NOT EXISTS` facilita repetir la importación inicial; no sustituye un sistema de migraciones para futuras versiones.

## 2. Configurar la conexión

Copia `config/database.properties.example` a `config/database.properties` y ajusta:

```properties
db.url=jdbc:mariadb://127.0.0.1:3306/monte_oracion_java
db.user=monte_app
db.password=TU_CONTRASENA
storage.path=C:/MonteOracionJava/adjuntos
```

El puerto debe coincidir con XAMPP. La carpeta de adjuntos debe ser privada, escribible para Java y estar fuera de `htdocs` y `webapps`. Se admiten JPG/PNG/PDF de hasta 5 MB. El código valida imágenes, pero no realiza escaneo antivirus.

La configuración se carga desde la ruta absoluta indicada por `MONTE_CONFIG` o `-Dmonte.config=...`. También puedes usar `MONTE_DB_URL`, `MONTE_DB_USER`, `MONTE_DB_PASSWORD`, `MONTE_STORAGE`. Los scripts ya pasan la ruta de configuración.

Si no se especifica `MONTE_CONFIG` ni `-Dmonte.config`, la aplicación también busca `conf/MonteOracionJava/database.properties` dentro de la instancia Tomcat activa. Esta opción funciona desde NetBeans sin modificar sus opciones Java y mantiene las contraseñas fuera del WAR.

Para XAMPP local con `root` sin contraseña, si `monte_app` todavía no existe, puedes configurar la conexión automáticamente después de importar el SQL:

```powershell
.\scripts\configurar-xampp.ps1 -TomcatHome 'C:\apache-tomcat-10.1.55-windows-x64\apache-tomcat-10.1.55'
```

El script genera una contraseña aleatoria, crea el usuario con permisos para esta base y guarda la configuración tanto en la carpeta privada del proyecto como en la de Tomcat. No reemplaza usuarios ni archivos de conexión existentes.

## 3. Compilar y crear el administrador

Desde PowerShell, dentro de esta carpeta:

```powershell
.\scripts\compilar.ps1
.\scripts\crear-admin.ps1
```

El primer script usa Maven en PATH o el incluido en NetBeans. La primera compilación necesita Internet para descargar dependencias. El segundo solicita nombre, correo y contraseña oculta de 10 a 128 caracteres; no guarda la contraseña en texto plano ni reemplaza cuentas existentes.

El resultado es `target/MonteOracionJava.war`. En NetBeans también puedes usar **Clean and Build**.

## 4. Ejecutar

Opción directa con tu Tomcat 10.1, solo para desarrollo local:

```powershell
.\scripts\ejecutar-local.ps1 -TomcatHome 'C:\apache-tomcat-10.1.55-windows-x64\apache-tomcat-10.1.55'
```

Abre **http://localhost:8080/MonteOracionJava/**. Si 8080 está ocupado, usa `-Puerto 8081`. Ctrl+C detiene el proceso. MySQL debe seguir iniciado. El script crea una instancia independiente dentro de `data/tomcat10` y no cambia la configuración de XAMPP.

Si Tomcat está en otra carpeta, indica su ubicación:

```powershell
.\scripts\ejecutar-local.ps1 -TomcatHome 'C:\servidores\apache-tomcat-10.1'
```

Opción NetBeans: Servicios → Servidores → Agregar servidor → Apache Tomcat → seleccionar su carpeta. En Propiedades del proyecto → Run, elige Tomcat y `/MonteOracionJava`. En el Tomcat de NetBeans agrega `-Dmonte.config=C:/ruta/completa/MonteOracionJava/config/database.properties` a las opciones de Java antes de iniciarlo. Ejecuta el proyecto.

### Si aparece HTTP 404 con Tomcat 10.1

La versión inicial del proyecto utilizaba `javax.servlet`; Tomcat 10.1 requiere `jakarta.servlet`. Esta versión ya está adaptada: API Servlet 6.0 en Maven, imports Jakarta y `web.xml` 6.0. Después de actualizar el código:

1. Detén la aplicación en NetBeans.
2. Clic derecho en **MonteOracionJava → Clean and Build**.
3. En **Properties → Run**, comprueba Tomcat 10.1 y **Context Path `/MonteOracionJava`**.
4. Ejecuta con **Run Project (F6)**. Espera a que finalice el despliegue antes de abrir `http://localhost:8080/MonteOracionJava/`.
5. Si todavía aparece el 404, reinicia Tomcat desde **Services → Servers** y vuelve a ejecutar el proyecto para que descarte las clases anteriores.

Si aparece HTTP 500 después de desplegar, revisa la conexión: importa el SQL, configura el usuario y establece `MONTE_CONFIG` o `-Dmonte.config` como se indica arriba. Un 404 también puede indicar un despliegue fallido o un contexto distinto; revisa **Output → Apache Tomcat Log**.

## Primer uso y módulos

1. Personal → entra con el administrador creado. Configura iglesia, fondos y medios de pago, incluido QR opcional.
2. Personal y permisos → crea al menos otra cuenta. Las aprobaciones requieren una persona diferente de quien solicita. El administrador aprueba; el tesorero paga y rinde; el almacenero entrega y recibe reposiciones. Una cuenta puede tener varios roles.
3. Congregantes → registra DNI y datos. Al editar la ficha se muestra su **código privado de consulta**: entrégalo a esa persona. La consulta pública exige DNI más ese código; no muestra archivos, contactos ni asistencia privada.
4. Productos y beneficiarios → completa el catálogo antes de recibir bienes. Las unidades no se convierten automáticamente.
5. Donaciones → el público envía aportes digitales con comprobante para revisión. El personal registra dinero efectivo o bienes ya recibidos. Bienes crea lotes y movimientos; cada recepción puede tener hasta tres líneas desde la pantalla.
6. Almacén → solicita, aprueba, reserva, entrega parcial o total, cancela pendientes, recibe reposiciones de préstamos y registra condonaciones o ajustes con motivo. Las pantallas admiten hasta tres lotes por solicitud.
7. Dinero → solicitudes de gasto, anticipo, saldo inicial y ajustes; aprobación reserva, ejecución cambia el saldo; rendición recibe el sobrante. Cada fondo conserva su destino.
8. Cierres → efectivo y digital. La conciliación digital adjunta constancia; no se conecta al banco. Otra persona revisa el cierre. Las fechas cerradas bloquean movimientos nuevos en ese canal.
9. Eventos → publica horarios e imágenes, asigna congregantes y guarda asistencia/notas privadas.
10. Reportes → PDF por rango de fechas de donaciones, movimientos de dinero, movimientos de almacén y cierres, según rol. Auditoría registra las acciones del personal. Listados paginados en bloques de 30.

## Estructura

```text
pom.xml                          Dependencias y compilación Maven WAR
sql/01_esquema.sql               Base y tablas para phpMyAdmin
config/                         Ejemplo de conexión, sin credenciales reales
scripts/                        Compilar, crear administrador y ejecutar
src/main/java/pe/monteoracion/
  Web.java                      Controlador HTTP y rutas
  Security.java                 Sesión, roles, CSRF y límites por IP
  Db.java                       JDBC parametrizado y transacciones
  Catalog.java                  Congregantes, personal, catálogos y eventos
  Donations.java                Recepción y revisión de donaciones
  Inventory.java                Reservas, entregas, préstamos y ajustes
  Treasury.java                 Fondos, pagos, rendiciones y cierres
  Pages.java / OperationPages.java / Html.java  Presentación y formularios
  FilesStore.java / Reports.java Adjuntos privados y reportes PDF
src/main/webapp/WEB-INF/         Plantilla JSP
src/main/webapp/assets/          Estilos y comportamiento del navegador
src/test/                       Comprobaciones de reglas e integración aislada
```

## Alcance respecto a C#

Es una implementación Java independiente de los flujos principales; no una conversión automática ni una migración de los datos de SQL Server. Las contraseñas de ASP.NET Identity no se copian. Crea cuentas propias en Java.

Se conservan separaciones de roles, reservas, doble aprobación, movimientos auditados, archivos privados, decimales exactos y detección de doble entrega. Las operaciones usan transacciones InnoDB y bloqueos de filas; cierres y dinero se serializan por canal. Un conflicto de concurrencia se informa para actualizar el formulario.

La presentación replica el estilo verde y los módulos, con vistas más sencillas. No incluye todas las funciones accesorias de la versión C#: importación especial de bienes históricos, reemplazo posterior de fotos de recepción, historial detallado de cambios de horario, filtros completos por cada listado ni formato tabular avanzado de sus PDFs. Para bienes existentes, registra una recepción real y verificada con su procedencia; no vuelvas a contabilizar dinero migrado como donación nueva.

No hay integración bancaria, correo, mensajería ni copias automáticas. Antes de operar con información real, concilia saldos e inventario, configura HTTPS en un Tomcat mantenido y programa respaldos de **la base y los adjuntos juntos**. El script de ejecución incluido escucha solo en `127.0.0.1` para desarrollo.

## Pruebas

`mvn test` ejecuta comprobaciones de importes, contraseñas, escape HTML y paginación PDF sin tocar la base. La integración se habilita explícitamente con `MONTE_TEST_DB_URL` apuntando a una base cuyo nombre empiece por `monte_oracion_java_test_`; crea y elimina únicamente esa base de prueba. `HttpSmokeTest` se habilita por separado con `MONTE_HTTP_TEST=1` y requiere el WAR compilado. Consulta [`docs/VERIFICACION.md`](docs/VERIFICACION.md) para los resultados y comandos de esta entrega.

Referencias: [XAMPP y MariaDB](https://www.apachefriends.org/faq_windows), [versiones de Tomcat y APIs](https://tomcat.apache.org/whichversion.html), [MariaDB Connector/J](https://mariadb.com/docs/connectors/mariadb-connector-j/installing-mariadb-connectorj).

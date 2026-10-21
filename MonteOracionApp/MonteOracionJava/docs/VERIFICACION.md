# Verificación — 20 de octubre de 2026

## Conexión local de XAMPP

Se diagnosticó `Access denied for user 'monte_app'@'localhost' (using password: NO)`: la base ya existía, pero no había cuenta de aplicación ni archivo de conexión. Se creó `monte_app` con contraseña aleatoria y permisos limitados a esta base, y se guardó la configuración privada del proyecto y de la instancia Tomcat. Las credenciales no se incluyen en el WAR ni en el ZIP de fuentes.

Se añadió carga de configuración desde `catalina.base/conf/MonteOracionJava/database.properties`, manteniendo la prioridad de las opciones explícitas. JDBC confirmó la conexión a la base local. Tras recargar únicamente esta aplicación, `http://localhost:8080/MonteOracionJava/` respondió **HTTP 200**, mostró el inicio y dejó de mostrar el error. La base no tenía cuentas activas del personal; el administrador debe crearse mediante el script interactivo, sin contraseña predeterminada.

## Actualización para Tomcat 10.1

Se adaptó el proyecto a Jakarta Servlet 6.0 para resolver la incompatibilidad de la versión inicial con el Tomcat 10.1.55 utilizado en NetBeans. Se actualizaron la dependencia Maven, los imports de producción/pruebas y el descriptor `web.xml` 6.0. También se actualizaron la guía y el script de ejecución.

Resultado de la actualización: WAR compilado correctamente, ocho pruebas unitarias aprobadas y la suite HTTP completa aprobada con **Apache Tomcat 10.1.55 y Java 21**, usando una base desechable de MariaDB. La ejecución HTTP utiliza una carpeta nueva para evitar JSP compiladas por Tomcat 8.5. No se modificó la configuración del servidor de NetBeans ni la base real. Reinicia Tomcat y vuelve a desplegar desde NetBeans para cargar las clases Jakarta.

## Resultado

- Maven WAR compilado con Java 21, destino Java 17: **BUILD SUCCESS**.
- Ocho pruebas unitarias: aprobadas, sin errores.
- Una prueba de integración con un flujo extenso en MariaDB 10.4.32: aprobada, sin errores.
- Una prueba HTTP con Tomcat 8.5.96 de XAMPP y Java 21: aprobada, sin errores.
- Se usó una instancia MariaDB independiente en `127.0.0.1:13306`, con archivos dentro de `target/test-mariadb`. No se inició ni modificó el MySQL habitual de XAMPP ni SQL Server.
- Las bases `monte_oracion_java_test_integracion` y `monte_oracion_java_test_http` se crearon y eliminaron por las pruebas. No quedaron cuentas de prueba en una base operativa.

## Cobertura

Las pruebas unitarias comprueban precisión decimal, cantidades inválidas, conteos en cero, prohibición de autoaprobación, contraseñas PBKDF2 con sal aleatoria, escape HTML, comandos y opciones inválidas, y PDF con acentos y varias páginas.

La prueba SQL importa el esquema completo y registra un congregante, producto y beneficiario. Luego recibe efectivo y bienes; rechaza autoaprobaciones; reserva dinero; rechaza sobreasignación; ejecuta un anticipo una sola vez; registra rendición y devolución; reserva bienes; entrega parcialmente; rechaza entrega repetida; completa la entrega; recibe reposición de préstamo; condona el saldo; rechaza exceso de devolución y ajustes inválidos. Dos aprobaciones paralelas compiten por un saldo insuficiente: solo una se acepta. También verifica cierres, revisión por otra persona, bloqueo de fechas cerradas, rechazo de anulación de bienes ya movidos, auditoría y consulta privada.

La prueba HTTP arranca una instancia temporal de Tomcat y recorre los módulos y formularios. Verifica sesión, CSRF, límites de acceso por rol, recepción de una donación digital multipart, aprobación, comprobante restringido, PDF, QR público, escape de contenido, publicación de evento con imagen, asignación de participante, consulta por DNI y código, desactivación de cuenta y cierre de sesión. Las JSP se compilaron y ejecutaron en Tomcat.

## Repetir

Pruebas sin base de datos:

```powershell
mvn test
```

Para integración, prepara un MariaDB de prueba en localhost que permita crear bases. Usa una contraseña de prueba mediante una variable de entorno local; no la guardes en el repositorio. La base indicada debe **no existir** y su nombre debe comenzar por `monte_oracion_java_test_`.

```powershell
$env:MONTE_TEST_DB_URL = 'jdbc:mariadb://127.0.0.1:13306/monte_oracion_java_test_integracion'
$env:MONTE_DB_URL = $env:MONTE_TEST_DB_URL
$env:MONTE_DB_USER = 'USUARIO_DE_PRUEBA'
$env:MONTE_DB_PASSWORD = 'CONTRASENA_DE_PRUEBA'
$env:MONTE_STORAGE = "$PWD/target/test-adjuntos"
mvn '-Dtest=DatabaseTest' test
```

Para HTTP, primero compila el WAR con `mvn package`. Luego configura la misma base aislada y ejecuta:

```powershell
$env:MONTE_TEST_DB_URL = 'jdbc:mariadb://127.0.0.1:13306/monte_oracion_java_test_http'
$env:MONTE_DB_URL = $env:MONTE_TEST_DB_URL
$env:MONTE_HTTP_TEST = '1'
$env:MONTE_TOMCAT_HOME = 'C:/apache-tomcat-10.1.55-windows-x64/apache-tomcat-10.1.55'
mvn '-Dtest=HttpSmokeTest' test
```

Elimina esas variables de tu sesión al terminar para no usarlas al ejecutar la aplicación normalmente. El caso HTTP se ejecuta por separado del caso SQL y utiliza el WAR ya compilado, crea su base, inicia y detiene Tomcat, y elimina su base al finalizar.

Los reportes automáticos se encuentran en `target/surefire-reports/`. La salida de cada instancia de Tomcat queda en `target/http-tomcat/<identificador>/console.log`. Cada ejecución usa una carpeta nueva para evitar reutilizar JSP compiladas por otra versión de Tomcat.

## Límites

No se ha probado carga de muchos usuarios, despliegue público, integración bancaria, restauración de respaldos ni migración desde SQL Server. Tampoco se ha realizado una comparación visual píxel a píxel de las pantallas originales. El resultado de Tomcat 8.5.96 corresponde a la versión inicial `javax.servlet`. El código actual utiliza `jakarta.servlet` 6.0 y requiere Tomcat 10.1.

# Monte de Oración

Aplicación web **ASP.NET Core MVC (.NET 10)** con **Identity**, **Entity Framework Core** y **SQL Server** para administrar donaciones monetarias y de bienes, almacén, congregantes, publicaciones, mensajes de contacto y personal de la iglesia **Monte de Oración**.

---

## Arquitectura

El proyecto conserva el patrón MVC. Los controladores y las rutas están en inglés para mantener compatibilidad con las URLs existentes; las clases, propiedades, vistas y archivos del dominio están en español. `ExpansorVistasEspanol` resuelve automáticamente las vistas en español.

```text
MonteOracionApp/
├── base_datos.sql                       Script SQL canónico de la base real
└── MonteOracionApp/
    ├── Program.cs                       Arranque, seguridad y preparación de la base
    ├── Controladores/                   Home, Dashboard, Account, Donations, Inventory, ...
    ├── Modelos/                         Entidades y formularios del dominio
    ├── Servicios/                       Donaciones, almacén, archivos y utilidades
    ├── Datos/
    │   ├── ContextoBaseDatos.cs         Mapeo EF Core, índices y restricciones
    │   ├── ContextoBaseDatosFactory.cs  Fábrica para `dotnet ef`
    │   ├── ComandosBaseDatos.cs         Comandos CLI y objetos SQL
    │   └── Migraciones/                 Migraciones versionadas
    ├── Vistas/                          Vistas Razor en español
    ├── wwwroot/                         Estilos, scripts e imágenes propias
    ├── appsettings.json                 Configuración base sin secretos
    └── appsettings.Development.json     Configuración local de desarrollo
```

---

## Base de datos

La base conserva exactamente **15 tablas de negocio**:

| # | Tabla | Propósito |
| :--- | :--- | :--- |
| 1 | `tb_usuario` | Cuentas de Identity. |
| 2 | `tb_rol` | Roles de Identity: Administrador, Recepción y Almacén. |
| 3 | `tb_usuario_rol` | Asignación de roles a usuarios. |
| 4 | `tb_personal` | Perfil y estado activo del personal. |
| 5 | `tb_congregante` | Congregantes y datos de contacto. |
| 6 | `tb_iglesia` | Configuración general de la iglesia. |
| 7 | `tb_medio_pago` | Medios de pago y datos para transferencias. |
| 8 | `tb_publicacion` | Avisos y eventos. |
| 9 | `tb_producto` | Catálogo de bienes del almacén. |
| 10 | `tb_donacion` | Cabecera de donaciones monetarias y de bienes. |
| 11 | `tb_donacion_bien` | Detalle de los bienes donados. |
| 12 | `tb_donacion_historial` | Auditoría de estados de las donaciones. |
| 13 | `tb_lote` | Lotes de inventario y su ubicación. |
| 14 | `tb_movimiento_almacen` | Kardex de entradas, salidas y ajustes. |
| 15 | `tb_mensaje_contacto` | Peticiones y mensajes recibidos. |

`__EFMigrationsHistory` es una tabla técnica de Entity Framework, no una tabla de negocio. No se crean tablas adicionales para Identity.

### Migraciones

- `20261110030506_InitialCreate`: esquema completo para una instalación nueva.
- `20261110030530_CorreccionesProduccion`: índice único de operaciones, restricciones e integridad adicional.

En una base existente, el arranque detecta las 15 tablas, registra la migración inicial como **línea base** y aplica únicamente las migraciones pendientes. No se usa `EnsureCreated` ni se eliminan tablas o datos en el arranque.

Las restricciones agregadas a bases existentes se crean con `WITH NOCHECK` para no rechazar datos históricos; las operaciones nuevas sí se validan. El índice definitivo es `IX_tb_donacion_operacion_unica` sobre `(id_medio_pago, numero_operacion)`.

---

## Requisitos

- .NET SDK 10.
- SQL Server con una base `MonteOracionApp` y permisos de creación de esquema.
- `dotnet-ef` solo si se van a crear o aplicar migraciones manualmente.

---

## Configuración

La conexión local vive únicamente en `MonteOracionApp/appsettings.Development.json`. No se versiona ninguna contraseña de producción.

Para producción configure la cadena por variable de entorno o secreto:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=MonteOracionApp;User Id=...;Password=...;TrustServerCertificate=True"
```

La carpeta de archivos se puede cambiar con `Storage__Path`. Si no se configura, se usa `Imagenes` dentro de la raíz del contenido.

---

## Ejecutar

Desde la carpeta `MonteOracionApp`:

```powershell
dotnet restore
dotnet build

# 1. Preparar esquema, roles, configuración y objetos SQL.
#    En una base existente crea la línea base sin borrar datos.
dotnet run -- --initialize

# 2. Crear el primer administrador de forma interactiva.
dotnet run -- --create-admin

# 3. Ejecutar la aplicación.
dotnet run
```

La aplicación usa `http://localhost:5035` con el perfil de desarrollo incluido.

### Comandos de mantenimiento

| Comando | Efecto |
| :--- | :--- |
| `--initialize` | Verifica las 15 tablas, registra la línea base, aplica migraciones pendientes, asegura roles, configuración, vistas y procedimientos. |
| `--create-admin` | Pide correo, nombre y contraseña; crea una cuenta con rol Administrador. No modifica cuentas existentes. |
| `--security-cleanup` | Desactiva las cuentas de referencia antiguas `USR-DIR-001`, `USR-REC-002` y `USR-ALM-003`, y retira sus roles. Conserva sus registros históricos y exige que quede otro administrador activo. No borra usuarios. |

`--security-cleanup` es una acción explícita. No se ejecuta automáticamente en el arranque.

---

## Seguridad implementada

- Identity con correo único y roles Administrador, Recepción y Almacén.
- Contraseña mínima de 10 caracteres, con mayúscula, minúscula, número, símbolo y 4 caracteres únicos.
- Bloqueo de cuenta tras 5 intentos fallidos durante 15 minutos.
- Cookie de autenticación `HttpOnly`, `SameSite=Lax`, expiración deslizante de 30 minutos y `Secure` en producción.
- Validación del sello de seguridad cada 5 minutos.
- Revalidación en cada solicitud autenticada: el usuario debe seguir activo en `tb_personal` y conservar un rol del equipo.
- Antiforgery global en formularios y limitación de solicitudes en formularios sensibles.
- Encabezados de seguridad: CSP, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` y `frame-ancestors`.
- HTTPS, HSTS y redirección HTTPS solo en producción.
- Página 404 personalizada, manejo de errores y endpoint de salud.
- No se crean usuarios ni contraseñas por defecto desde el código ni desde `base_datos.sql`.

---

## Rutas principales

Públicas:

- `/`, `/eventos`, `/avisos`, `/contacto`, `/quienes-somos`, `/ministerios`, `/horarios-y-mapa`
- `/health` devuelve `200 {"estado":"ok"}` cuando la base responde.

Administración (requiere sesión):

- `/Dashboard`, `/Donations`, `/Inventory`, `/Congregants`, `/Messages`, `/Publications`, `/Staff`, `/Settings`
- `/Account/Login`, `/Account/Password`

---

## Notas

- `base_datos.sql` es el script canónico de la base real. Ya no crea usuarios, contraseñas ni donaciones de demostración.
- `base_datos_academico.sql` es material académico con otro esquema; no se usa para ejecutar esta aplicación y no debe usarse en producción.
- Los cambios de esquema se realizan mediante migraciones versionadas. No edite tablas manualmente sin agregar la migración correspondiente.

# Monte de Oración — primera versión

Aplicación web ASP.NET Core MVC (.NET 10), ASP.NET Identity, Entity Framework Core y SQL Server. Interfaz adaptable en español.

## Arranque

1. Abrir `MonteOracionApp.slnx` en Visual Studio compatible con .NET 10.
2. Verificar SQL Server. `appsettings.json` conserva la conexión LocalDB original. Si usas la instancia local normal, configurar desde la carpeta del proyecto:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=MonteOracionApp;Trusted_Connection=True;TrustServerCertificate=True"
dotnet restore
dotnet run -- --initialize
dotnet run -- --create-admin
dotnet run
```

Los comandos de inicialización terminan después de preparar la base. `--create-admin` solicita correo y contraseña por consola (la contraseña no se muestra). Requiere 10 caracteres. No hay una contraseña predeterminada ni registro público de cuentas. No ejecutar mientras otra instancia de la aplicación esté bloqueando la compilación. Para usar secretos locales, ejecutar en entorno Development; Visual Studio y los perfiles de lanzamiento existentes lo establecen.

La conexión del ejemplo es para desarrollo en el mismo equipo. En producción usar un servidor y certificado válidos, configuración externa de secretos y un usuario SQL con los permisos necesarios. La cuenta que aplica migraciones necesita permisos de esquema; no exponer el comando de inicialización por HTTP.

Después de iniciar sesión como administrador:

1. Configuración → completar información de la iglesia.
2. Medios de pago → agregar datos reales y QR de Yape o transferencia.
3. Congregantes → registrar personas y crear sus cuentas cuando corresponda.
4. Publicaciones → publicar eventos y avisos.
5. Donaciones → revisar pagos digitales o registrar entregas presenciales.

## Árbol y responsabilidades

```text
MonteOracionApp/
├── Program.cs                  Registro de servicios y seguridad HTTP
├── Controllers/HomeController.cs  Portada pública
├── Data/
│   ├── ApplicationDbContext.cs Modelo SQL, relaciones y restricciones
│   ├── DatabaseCommands.cs     Migraciones, roles y alta inicial del administrador
│   ├── Migrations/             Historial versionado del esquema
│   └── Scripts/                SQL generado para revisar/aplicar las migraciones
├── Modules/
│   ├── Accounts/               Inicio de sesión y contraseñas
│   ├── Congregants/            Personas, cuentas, activación y restablecimiento
│   ├── Donations/              Entidades, formularios, reglas, servicio y controlador
│   ├── Publications/           Eventos y avisos
│   ├── Settings/               Información institucional y medios de pago
│   ├── Portal/                 Datos e historial del congregante
│   └── Dashboard/              Resumen administrativo
├── Shared/                     Roles y almacenamiento privado de archivos
├── Views/                      Pantallas agrupadas por controlador
├── wwwroot/                    CSS, JavaScript y librerías públicas
├── App_Data/attachments/       Comprobantes y QR (se crea al usar archivos)
└── docs/                       Arquitectura, reglas, pruebas y operación
```

## Dónde corregir o ampliar

- Texto, tabla o formulario: `Views/<módulo>/`.
- Estilo o interacción del formulario: `wwwroot/css/site.css` o `wwwroot/js/site.js`.
- Datos que acepta una pantalla: clases `*Form` del módulo. No enlazar directamente todos los campos sensibles de una entidad.
- Aprobación, rechazo, anulación: `Modules/Donations/DonationRules.cs` y `DonationService.cs`.
- Permisos y rutas: controlador del módulo. Mantener los filtros `Authorize` y el filtro por propietario del comprobante.
- Archivos: `Shared/FileStorage.cs` y la autorización en `DonationsController.Receipt`.
- Nuevos campos SQL: entidad + `ApplicationDbContext` + nueva migración. No editar migraciones ya aplicadas.

Para una migración nueva, usar `Add-Migration NombreDescriptivo` en la consola del Administrador de paquetes de Visual Studio, con MonteOracionApp como proyecto predeterminado. También se puede usar `dotnet ef` si instalas la herramienta correspondiente a EF Core 10.0.12. Luego revisar el cambio y ejecutar `dotnet run -- --initialize`.

## Alcance

Incluye acceso por roles, gestión de congregantes y cuentas, donaciones digitales pendientes, recepción presencial de bienes y efectivo, decisiones administrativas con historial y control de concurrencia, comprobantes privados, consulta del congregante, publicaciones y configuración de medios de pago. Los listados principales tienen paginación.

No incluye integración bancaria automática, donaciones de servicios, asistencia, reportes PDF ni notificaciones externas. La verificación del ingreso bancario es manual. El portal informa al congregante mediante su historial; no envía correos ni mensajes.

Ver `docs/ARQUITECTURA.md`, `docs/OPERACION.md` y `docs/VERIFICACION.md`.


## Renombrado de tablas y contraseñas

La migración RenombrarTablasEspanol cambia las 14 tablas funcionales a nombres en español con prefijo tb_. El mapeo está centralizado en Data/TableNames.cs. __EFMigrationsHistory conserva su nombre. Las columnas y clases C# conservan sus nombres actuales.

Antes de ejecutar la versión nueva, detener la aplicación en Visual Studio y ejecutar Update-Database en la Consola del Administrador de paquetes, con MonteOracionApp como proyecto predeterminado. Luego iniciar de nuevo. No hace falta Add-Migration ni volver a crear el administrador. La migración renombra tablas e índices y restablece claves; no borra tablas ni registros. El script específico está en Data/Scripts/RenombrarTablasEspanol.sql.

Las contraseñas nuevas requieren al menos 10 caracteres, sin categorías obligatorias de mayúsculas, minúsculas, números o símbolos. Las contraseñas existentes siguen funcionando.

Verificación: compilación en carpeta separada con cero errores y advertencias; modelo y migración coinciden; revisión de las operaciones de renombrado. Aplicación a SQL Server pendiente: la sesión de trabajo no logró autenticarse con Windows.

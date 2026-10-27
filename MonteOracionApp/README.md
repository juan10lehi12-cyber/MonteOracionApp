# Monte de Oración

Aplicación web en **ASP.NET Core MVC (.NET 10)**, **Identity**, **Entity Framework Core** y **SQL Server** para la gestión ágil de donaciones (monetarias y de bienes), almacén de existencias, miembros y eventos de la iglesia **Monte de Oración**.

---

## Arquitectura del proyecto (Patrón MVC en Español)

Tanto las carpetas y archivos como las clases, métodos y atributos del dominio en C# están en **español** (conservando únicamente las palabras reservadas de C#/.NET y los nombres de rutas MVC):

```text
MonteOracionApp/
├── base_datos.sql                    (Script SQL Server completo en la raíz de la solución)
└── MonteOracionApp/
    ├── Controladores/
    │   ├── InicioControlador.cs          (HomeController)
    │   ├── PanelControlador.cs           (DashboardController)
    │   ├── CuentaControlador.cs          (AccountController)
    │   ├── DonacionesControlador.cs      (DonationsController)
    │   ├── AlmacenControlador.cs         (InventoryController)
    │   ├── PublicacionesControlador.cs   (PublicationsController)
    │   ├── MiembrosControlador.cs        (CongregantsController)
    │   ├── PersonalControlador.cs        (StaffController)
    │   └── ConfiguracionControlador.cs   (SettingsController)
    ├── Modelos/
    │   ├── Donacion.cs                   (Donacion, DonacionBien, DonacionHistorial)
    │   ├── FormulariosDonacion.cs        (FormularioDonacionWeb, FormularioRecepcionDonacion, ReglasDonacion)
    │   ├── Almacen.cs                    (Producto, Lote, MovimientoAlmacen, FormularioSalidaAlmacen)
    │   ├── Publicacion.cs                (Publicacion, TipoPublicacion)
    │   ├── Miembro.cs                    (Congregante)
    │   ├── Personal.cs                   (PerfilPersonal, FormularioPersonal)
    │   ├── ConfiguracionIglesia.cs       (ConfiguracionIglesia, MedioPago)
    │   ├── FormulariosCuenta.cs          (FormularioAcceso, FormularioClave)
    │   └── ErrorVista.cs                 (ModeloErrorVista)
    ├── Servicios/
    │   ├── ServicioDonaciones.cs         (IServicioDonaciones, ServicioDonaciones)
    │   ├── ServicioAlmacen.cs            (ServicioAlmacen)
    │   ├── AlmacenamientoArchivos.cs     (IAlmacenamientoArchivos, AlmacenamientoArchivos)
    │   └── Utilidades.cs                 (Roles, HoraIglesia, Operaciones, ExpansorVistasEspanol)
    ├── Datos/
    │   ├── ContextoBaseDatos.cs          (ContextoBaseDatos — mapeo directo sin traducción de columnas)
    │   └── ComandosBaseDatos.cs          (ComandosBaseDatos)
    └── Vistas/
        ├── Compartido/                   (_Diseno.cshtml, _AccesoParcial.cshtml, _Paginacion.cshtml, etc.)
        ├── Inicio/                       (Indice.cshtml, Privacidad.cshtml)
        ├── Panel/                        (Indice.cshtml)
        ├── Cuenta/                       (Acceso.cshtml, Clave.cshtml, Denegado.cshtml)
        ├── Donaciones/                   (Indice.cshtml, Crear.cshtml, Enviada.cshtml, Recibir.cshtml, Detalle.cshtml)
        ├── Almacen/                      (Indice.cshtml, Pendientes.cshtml, Salida.cshtml, Movimientos.cshtml)
        ├── Publicaciones/                (Indice.cshtml, Editar.cshtml, Calendario.cshtml, Recaudacion.cshtml)
        ├── Miembros/                     (Indice.cshtml, Editar.cshtml)
        ├── Personal/                     (Indice.cshtml, Editar.cshtml)
        └── Configuracion/                (Indice.cshtml, MediosPago.cshtml, MedioPago.cshtml)
```

---

## Base de datos SQL Server (`../base_datos.sql`)

La base de datos utiliza nombres de tablas (`tb_*`) y columnas íntegramente en **español con minúsculas y guion bajo (`snake_case`)**, idénticos a las propiedades de las clases en `Modelos/`, por lo que `Datos/ContextoBaseDatos.cs` no requiere traducir nombres de columnas para nuestras tablas.

| Tabla | Entidad C# | Columnas principales en SQL Server / Propiedades C# |
| :--- | :--- | :--- |
| `tb_usuario` | `IdentityUser` | `id`, `nombre_usuario`, `correo`, `clave_hash`, `bloqueo_activo`, `intentos_fallidos` |
| `tb_rol` | `IdentityRole` | `id`, `nombre`, `nombre_normalizado` |
| `tb_usuario_rol` | `IdentityUserRole` | `id_usuario`, `id_rol` |
| `tb_personal` | `PerfilPersonal` | `id_usuario`, `nombre_completo`, `cargo`, `activo` |
| `tb_congregante` | `Congregante` | `id`, `dni`, `nombres`, `apellidos`, `telefono`, `fecha_nacimiento`, `direccion`, `activo` |
| `tb_iglesia` | `ConfiguracionIglesia` | `id`, `nombre`, `informacion`, `direccion`, `contacto`, `horarios` |
| `tb_medio_pago` | `MedioPago` | `id`, `nombre`, `instrucciones`, `numero_cuenta`, `clave_qr`, `activo` |
| `tb_publicacion` | `Publicacion` | `id`, `tipo`, `titulo`, `contenido`, `fecha_inicio_evento`, `fecha_fin_evento`, `lugar`, `meta_recaudacion`, `publicado`, `cancelado` |
| `tb_producto` | `Producto` | `id`, `nombre`, `unidad`, `activo` |
| `tb_donacion` | `Donacion` | `id`, `codigo_seguimiento`, `tipo`, `estado`, `tipo_donante`, `id_congregante`, `monto`, `numero_operacion`, `id_evento`, `id_medio_pago`, `fecha_recepcion_pago`, `digital`, `version` |
| `tb_donacion_bien` | `DonacionBien` | `id`, `id_donacion`, `id_producto`, `descripcion`, `cantidad`, `unidad` |
| `tb_donacion_historial` | `DonacionHistorial` | `id`, `id_donacion`, `estado`, `id_administrador`, `fecha`, `motivo` |
| `tb_lote` | `Lote` | `id`, `id_producto`, `id_donacion_bien`, `ubicacion`, `origen`, `cantidad_actual`, `revision_pendiente`, `confirmado_por`, `version` |
| `tb_movimiento_almacen` | `MovimientoAlmacen` | `id`, `id_comando`, `id_lote`, `tipo`, `cantidad`, `id_usuario`, `motivo`, `destino`, `fecha_hora`, `fecha_operacion` |

---

## Ejecutar el proyecto

Desde la carpeta `MonteOracionApp`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run -- --initialize
dotnet run -- --create-admin
dotnet run
```

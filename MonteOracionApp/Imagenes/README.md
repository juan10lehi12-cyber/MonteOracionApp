# Estructura de Carpetas de Imágenes (`MonteOracionApp/Imagenes/`)

Todas las imágenes y comprobantes del proyecto están organizados por categoría dentro de `C:\Users\Usuario\source\repos\MonteOracionApp\MonteOracionApp\Imagenes\` para que al subir el proyecto a **Git** sea fácil encontrar cada archivo según su función:

```text
MonteOracionApp/Imagenes/
├── MediosPago/       --> Códigos QR de medios de pago (Yape, Plin, Cuentas)
├── Eventos/          --> Fotografías y portadas de eventos y campañas
├── Donaciones/       --> Capturas de pago (vouchers) y fotos de bienes donados
└── Portada/          --> Fotografías principales del diseño web (Hero, Comunidad, Obra Social)
```

---

## 1. `Imagenes/MediosPago/` (Códigos QR en `tb_medio_pago.clave_qr`)
Aquí se guardan automáticamente todos los códigos QR que el administrador sube en **Configuración → Medios de pago**:
| ID | Medio de Pago | Ruta del archivo |
| :- | :------------ | :--------------- |
| 1  | Yape          | `Imagenes/MediosPago/231853f6a7d1416999c9c7fa26e84719.jpg` |
| 3  | Plin          | `Imagenes/MediosPago/0977325b0c6c48c9be76e00fd99938fd.png` |

---

## 2. `Imagenes/Eventos/` (Fotos de eventos en `tb_publicacion.clave_imagen`)
Aquí se guardan automáticamente los afiches y fotos de eventos subidos en **Administración → Eventos y publicaciones**:
| ID | Evento / Publicación          | Ruta del archivo |
| :- | :---------------------------- | :--------------- |
| 2  | Gran Evento de Fe y Esperanza | `Imagenes/Eventos/593d9ee8979d4dfdbc4fb18695055283.png` |
| 6  | Vigilia de Adoración          | `Imagenes/Eventos/24b7268c6a534684a3b53c39e48f6972.png` |
| 7  | Concierto de Navidad          | `Imagenes/Eventos/19b73f70da5b4180bb7d5ceb0dfe5e2d.png` |
| 8  | Aniversario 2026              | `Imagenes/Eventos/f6a668cafdc84044891e2afc19b553bd.png` |
| 9  | Evento de prueba (cancelar)   | `Imagenes/Eventos/29568c930e3b4f91aa295ac352827a6b.png` |

---

## 3. `Imagenes/Donaciones/` (Capturas de pagos y bienes en `tb_donacion.clave_comprobante`)
Aquí se guardan automáticamente todas las capturas de Yape/Plin/transferencias que suben los donantes y las fotos de bienes físicos en almacén:
| ID Donación | Ruta del archivo |
| :---------- | :--------------- |
| 1           | `Imagenes/Donaciones/717ff3dbf898473aa2914c6a997ed3eb.png` |
| 3           | `Imagenes/Donaciones/29a9240e73a14327b5074ea71857c5e7.jpg` |
| 26          | `Imagenes/Donaciones/6e230fdbf0dd460bb1c0f8a960144da5.png` |
| 27          | `Imagenes/Donaciones/d5ce8909408245fba41ec1b1e1cb8df0.png` |
| 28          | `Imagenes/Donaciones/175f13ef6f014c7c8d3b0a0034b0f776.png` |
| 29          | `Imagenes/Donaciones/311cc50f656e4d20b7fcc5071f76b201.png` |
| 30          | `Imagenes/Donaciones/3d5bd85ee9ed4e03a861231d5cc36e9a.png` |
| 31          | `Imagenes/Donaciones/89fb36ee3b7741ca99c2decf0066af98.png` |

*(Además incluye las demás capturas de prueba registradas en el historial de donaciones).*

---

## 4. `Imagenes/Portada/` (También en `wwwroot/imagenes/`)
Fotografías del diseño de la interfaz pública:
- `Imagenes/Portada/hero-comunidad.png` — Fotografía principal de bienvenida y comunidad.
- `Imagenes/Portada/vigilia-adoracion.png` — Fotografía para vigilias y alabanza.
- `Imagenes/Portada/concierto-navidad.png` — Fotografía para el Concierto de Navidad.
- `Imagenes/Portada/accion-social.png` — Fotografía para campañas de víveres y obra social.

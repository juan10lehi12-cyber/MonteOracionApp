using System.Data;
using Microsoft.EntityFrameworkCore;
using MonteOracionApp.Datos;
using MonteOracionApp.Modelos;
namespace MonteOracionApp.Servicios;

public interface IServicioDonaciones
{
    Task<Donacion> RegistrarWebAsync(FormularioDonacionWeb formulario, string? idUsuario);
    Task<Donacion> RegistrarRecepcionAsync(FormularioRecepcionDonacion formulario, string idAdministrador);
    Task ReemplazarFotoBienAsync(int id, IFormFile foto, byte[] version, string idAdministrador);
    Task DecidirAsync(int id, EstadoDonacion estado, string? motivo, string idAdministrador, byte[] version, int? idCongregante = null);
}

public class ServicioDonaciones(ContextoBaseDatos db, IAlmacenamientoArchivos almacenamiento, ServicioAlmacen almacen) : IServicioDonaciones
{
    public async Task<Donacion> RegistrarWebAsync(FormularioDonacionWeb formulario, string? idUsuario)
    {
        ReglasDonacion.ValidarFecha(formulario.fecha_pago, DateTime.UtcNow.AddHours(-5));
        ReglasDonacion.ValidarMonto(formulario.monto);
        if (formulario.monto > 99999.99m) throw new InvalidOperationException("Ingresa un monto positivo hasta S/ 99 999.99 con dos decimales.");
        var medioPago = await db.MediosPago.SingleOrDefaultAsync(x => x.id == formulario.id_medio_pago && x.activo)
            ?? throw new InvalidOperationException("Selecciona un medio de pago disponible.");
        Congregante? congregante = null;
        if (!formulario.anonimo)
        {
            if (string.IsNullOrWhiteSpace(formulario.dni)) throw new InvalidOperationException("Ingresa tu DNI o marca la casilla de donación anónima.");
            congregante = await db.Congregantes.SingleOrDefaultAsync(x => x.dni == formulario.dni && x.activo)
                ?? throw new InvalidOperationException("No encontramos ese DNI. Acércate a la administración para registrar tu ficha o dona de forma anónima.");
        }
        Publicacion? modeloEvento = null;
        var operacion = formulario.numero_operacion.Trim().ToUpperInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(operacion, "^[A-Z0-9-]{4,20}$")) throw new InvalidOperationException("El número de operación admite de 4 a 20 caracteres (letras, números o guiones).");
        if (await db.Donaciones.AnyAsync(x => x.id_medio_pago == medioPago.id && x.fecha_recepcion_pago == formulario.fecha_pago.Date && x.numero_operacion == operacion)) throw new InvalidOperationException("Esta operación ya fue registrada para ese medio y fecha. Consulta con la administración.");
        if (formulario.id_evento.HasValue)
        {
            modeloEvento = await db.Publicaciones.SingleOrDefaultAsync(x => x.id == formulario.id_evento)
                ?? throw new InvalidOperationException("El evento indicado no está disponible.");
            if (modeloEvento.tipo != TipoPublicacion.Evento || !modeloEvento.publicado || modeloEvento.cancelado
                || modeloEvento.fecha_inicio_evento is null || modeloEvento.fecha_inicio_evento.Value.Date < formulario.fecha_pago.Date)
                throw new InvalidOperationException("El evento indicado no está aceptando donaciones.");
        }
        if (formulario.comprobante == null) throw new InvalidOperationException("El comprobante es obligatorio.");
        var archivo = await almacenamiento.GuardarAsync(formulario.comprobante, soloImagen: true);
        var donacion = new Donacion
        {
            tipo = TipoDonacion.Dinero, estado = EstadoDonacion.Pendiente,
            tipo_donante = congregante != null ? TipoDonante.Congregante : TipoDonante.Anonimo,
            id_congregante = congregante?.id, nombre_externo = null, contacto_externo = null,
            monto = formulario.monto, id_medio_pago = medioPago.id, nombre_medio_pago = medioPago.nombre, numero_operacion = operacion,
            id_evento = modeloEvento?.id, fecha_recepcion_pago = formulario.fecha_pago.Date, clave_comprobante = archivo.clave, tipo_contenido_comprobante = archivo.tipo_contenido, id_usuario_registro = null, digital = true
        };
        db.Donaciones.Add(donacion);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) { almacenamiento.Eliminar(archivo.clave); throw new InvalidOperationException("La operación ya fue registrada. Consulta con la administración."); }
        catch { almacenamiento.Eliminar(archivo.clave); throw; }
        return donacion;
    }

    public async Task<Donacion> RegistrarRecepcionAsync(FormularioRecepcionDonacion formulario, string idAdministrador)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        ReglasDonacion.ValidarFecha(formulario.ReceivedOn, DateTime.UtcNow.AddHours(-5));
        if (!Enum.IsDefined(formulario.Kind) || !Enum.IsDefined(formulario.DonorKind)) throw new InvalidOperationException("Tipo de donación o donante inválido.");
        int? idCongregante = null;
        if (formulario.DonorKind == TipoDonante.Congregante)
        {
            idCongregante = formulario.CongregantId;
            if (idCongregante == null && !string.IsNullOrWhiteSpace(formulario.NewMemberName))
            {
                Congregante? creado = formulario.NewMemberDni is { Length: 8 } dni
                    ? await db.Congregantes.SingleOrDefaultAsync(x => x.dni == dni) : null;
                if (creado != null && !creado.activo) throw new InvalidOperationException("El miembro con ese DNI está desactivado; reactiva su ficha en Miembros antes de registrar la donación.");
                if (creado == null)
                {
                    var completo = formulario.NewMemberName.Trim();
                    var corte = completo.IndexOf(' ');
                    var nombres = corte > 0 ? completo[..corte] : completo;
                    var apellidos = corte > 0 ? completo[(corte + 1)..] : "";
                    creado = new Congregante { nombres = nombres, apellidos = apellidos, dni = formulario.NewMemberDni, telefono = formulario.NewMemberPhone };
                    db.Congregantes.Add(creado);
                }
                await db.SaveChangesAsync();
                idCongregante = creado.id;
            }
            if (idCongregante == null || !await db.Congregantes.AnyAsync(x => x.id == idCongregante && x.activo))
                throw new InvalidOperationException("Busca y selecciona al miembro que dona, o registra sus datos básicos.");
        }
        if (formulario.DonorKind == TipoDonante.Externo && string.IsNullOrWhiteSpace(formulario.ExternalName)) throw new InvalidOperationException("Indica el nombre del donante externo.");
        var d = new Donacion
        {
            tipo = formulario.Kind, estado = EstadoDonacion.Aprobada, tipo_donante = formulario.DonorKind,
            id_congregante = formulario.DonorKind == TipoDonante.Congregante ? idCongregante : null,
            nombre_externo = formulario.DonorKind == TipoDonante.Externo ? formulario.ExternalName?.Trim() : null,
            contacto_externo = formulario.DonorKind == TipoDonante.Externo ? formulario.ExternalContact?.Trim() : null,
            fecha_recepcion_pago = formulario.ReceivedOn.Date, id_usuario_registro = idAdministrador
        };
        if (formulario.Kind == TipoDonacion.Dinero)
        {
            ReglasDonacion.ValidarMonto(formulario.Amount);
            d.monto = formulario.Amount; d.nombre_medio_pago = "Efectivo";
        }
        else
        {
            var items = formulario.Items.Where(x => x.ProductId.HasValue || x.Quantity.HasValue || !string.IsNullOrWhiteSpace(x.NewProductName)).ToList();
            if (items.Count is 0 or > 50 || items.Any(x => (x.ProductId == null && string.IsNullOrWhiteSpace(x.NewProductName)) || x.Quantity is null or <= 0 or > 9999999999.99m || decimal.Round(x.Quantity.Value, 2) != x.Quantity)) throw new InvalidOperationException("Registra de 1 a 50 bienes: elige del catálogo o escribe un producto nuevo, con cantidad positiva y máximo dos decimales.");
            foreach (var it in items)
            {
                if (it.ProductId != null) continue;
                var nombre = it.NewProductName!.Trim();
                var unidad = string.IsNullOrWhiteSpace(it.NewProductUnit) ? "unidad" : it.NewProductUnit.Trim();
                var producto = await db.Productos.SingleOrDefaultAsync(p => p.nombre == nombre && p.unidad == unidad);
                if (producto == null)
                {
                    producto = new Producto { nombre = nombre, unidad = unidad };
                    db.Productos.Add(producto);
                }
                await db.SaveChangesAsync();
                it.ProductId = producto.id;
            }
            d.bienes = items.GroupBy(x => x.ProductId)
                .Select(g => new DonacionBien { id_producto = g.Key, cantidad = g.Sum(x => x.Quantity!.Value) })
                .ToList();
            await almacen.RecibirDonacionAsync(d, idAdministrador);
        }
        d.decisiones.Add(new() { id_administrador = idAdministrador, estado = EstadoDonacion.Aprobada, motivo = "Recepción presencial confirmada." });
        db.Donaciones.Add(d);
        await db.GuardarOperacionAsync(); await transaccion.CommitAsync();
        return d;
    }

    public async Task DecidirAsync(int id, EstadoDonacion estado, string? motivo, string idAdministrador, byte[] version, int? idCongregante = null)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var d = await db.Donaciones.SingleOrDefaultAsync(x => x.id == id) ?? throw new InvalidOperationException("Donación no encontrada.");
        ReglasDonacion.ValidarDecision(d.estado, estado, motivo);
        if (d.tipo == TipoDonacion.Bienes && estado == EstadoDonacion.Anulada)
        {
            var lotes = await db.Lotes.Where(x => x.donacion_bien != null && x.donacion_bien.id_donacion == id).ToListAsync();
            if (lotes.Count == 0 || lotes.Any(x => !x.revision_pendiente))
                throw new InvalidOperationException("El bien ya tiene visto bueno en el almacén. Registra su baja desde Registrar salida.");
            foreach (var lote in lotes)
            {
                db.MovimientosAlmacen.Add(new MovimientoAlmacen { id_lote = lote.id, cantidad = -lote.cantidad_actual, tipo = TipoMovimientoAlmacen.Salida, destino = "Anulación de donación", id_usuario = idAdministrador, motivo = "Anulación de recepción: " + motivo });
                lote.cantidad_actual = 0;
                lote.revision_pendiente = false; lote.id_usuario_pendiente = null;
            }
        }
        if (idCongregante.HasValue)
        {
            if (d.estado != EstadoDonacion.Pendiente || estado is not (EstadoDonacion.Aprobada or EstadoDonacion.Rechazada) || d.id_congregante != null)
                throw new InvalidOperationException("Solo puedes vincular una donación pendiente sin congregante al aprobarla o rechazarla.");
            var congregante = await db.Congregantes.SingleOrDefaultAsync(x => x.id == idCongregante && x.activo)
                ?? throw new InvalidOperationException("El congregante seleccionado no existe o está inactivo.");
            d.id_congregante = congregante.id;
            d.tipo_donante = TipoDonante.Congregante;
            d.decisiones.Add(new()
            {
                estado = estado, id_administrador = idAdministrador,
                motivo = $"Vinculada a la ficha del congregante #{congregante.id} durante la revisión."
            });
        }
        db.Entry(d).Property(x => x.version).OriginalValue = version;
        d.estado = estado;
        d.decisiones.Add(new() { estado = estado, motivo = motivo?.Trim(), id_administrador = idAdministrador });
        try { await db.SaveChangesAsync(); await transaccion.CommitAsync(); }
        catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("Otro administrador ya modificó esta donación. Actualiza la página."); }
    }

    public async Task ReemplazarFotoBienAsync(int id, IFormFile foto, byte[] version, string idAdministrador)
    {
        var d = await db.Donaciones.SingleOrDefaultAsync(x => x.id == id)
            ?? throw new InvalidOperationException("Donación no encontrada.");
        if (d.tipo != TipoDonacion.Bienes || d.estado != EstadoDonacion.Aprobada)
            throw new InvalidOperationException("Solo puedes cambiar la foto de bienes con recepción aprobada.");
        var subido = await almacenamiento.GuardarAsync(foto, soloImagen: true);
        var claveAnterior = d.clave_comprobante;
        db.Entry(d).Property(x => x.version).OriginalValue = version;
        d.clave_comprobante = subido.clave;
        d.tipo_contenido_comprobante = subido.tipo_contenido;
        d.decisiones.Add(new() { id_administrador = idAdministrador, estado = d.estado, motivo = "Foto del bien recibido actualizada." });
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateConcurrencyException)
        {
            almacenamiento.Eliminar(subido.clave);
            throw new InvalidOperationException("Otro administrador ya modificó esta donación. Actualiza la página.");
        }
        catch { almacenamiento.Eliminar(subido.clave); throw; }
        if (claveAnterior != null) almacenamiento.Eliminar(claveAnterior);
    }
}

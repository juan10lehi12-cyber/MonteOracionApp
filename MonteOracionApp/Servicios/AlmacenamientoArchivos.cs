namespace MonteOracionApp.Servicios;

public record ArchivoGuardado(string clave, string tipo_contenido);

public interface IAlmacenamientoArchivos
{
    Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, bool soloImagen = false);
    Stream Abrir(string clave);
    void Eliminar(string clave);
}

public sealed class AlmacenamientoArchivos : IAlmacenamientoArchivos
{
    private readonly string raiz;
    public AlmacenamientoArchivos(IWebHostEnvironment env, IConfiguration config)
    {
        raiz = Path.GetFullPath(config["Storage:Path"] ?? Path.Combine(env.ContentRootPath, "App_Data", "attachments"));
        Directory.CreateDirectory(raiz);
    }

    public async Task<ArchivoGuardado> GuardarAsync(IFormFile archivo, bool soloImagen = false)
    {
        if (archivo.Length is <= 0 or > 5 * 1024 * 1024) throw new InvalidOperationException("El archivo debe tener entre 1 byte y 5 MB.");
        await using var entrada = archivo.OpenReadStream();
        using var buffer = new MemoryStream();
        await entrada.CopyToAsync(buffer);
        var datos = buffer.ToArray();
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var png = datos.Length >= 57 && datos.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && datos.AsSpan(12, 4).SequenceEqual("IHDR"u8) && datos.AsSpan(datos.Length - 12, 12).SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 });
        var jpg = datos.Length >= 4 && datos[0] == 255 && datos[1] == 216 && datos[2] == 255 && datos[^2] == 255 && datos[^1] == 217;
        var pdf = datos.Length >= 10 && datos.AsSpan(0, 5).SequenceEqual("%PDF-"u8) && System.Text.Encoding.Latin1.GetString(datos.AsSpan(Math.Max(0, datos.Length - 1024))).Contains("%%EOF");
        string tipo;
        if (png && extension == ".png") tipo = "image/png";
        else if (jpg && extension is ".jpg" or ".jpeg") tipo = "image/jpeg";
        else if (pdf && extension == ".pdf" && !soloImagen) tipo = "application/pdf";
        else throw new InvalidOperationException(soloImagen ? "Adjunta una imagen JPG o PNG válida." : "Adjunta un archivo JPG, PNG o PDF válido.");
        var clave = Guid.NewGuid().ToString("N") + (tipo == "image/png" ? ".png" : tipo == "image/jpeg" ? ".jpg" : ".pdf");
        await File.WriteAllBytesAsync(Path.Combine(raiz, clave), datos);
        return new(clave, tipo);
    }

    private string Resolver(string clave)
    {
        if (string.IsNullOrWhiteSpace(clave) || clave != Path.GetFileName(clave) || clave.Contains('\\') || clave.Contains('/')) throw new InvalidOperationException("Archivo inválido.");
        return Path.Combine(raiz, clave);
    }

    public Stream Abrir(string clave) => File.OpenRead(Resolver(clave));
    public void Eliminar(string clave) => File.Delete(Resolver(clave));
}

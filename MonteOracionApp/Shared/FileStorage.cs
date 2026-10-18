namespace MonteOracionApp.Shared;
public record StoredFile(string Key, string ContentType);
public interface IFileStorage
{
    Task<StoredFile> SaveAsync(IFormFile file, bool imageOnly = false);
    Stream Open(string key);
    void Delete(string key);
}
// Files are private. Authorization belongs in the controller before Open().
public sealed class FileStorage : IFileStorage
{
    private readonly string root;
    public FileStorage(IWebHostEnvironment env, IConfiguration config)
    {
        root = Path.GetFullPath(config["Storage:Path"] ?? Path.Combine(env.ContentRootPath, "App_Data", "attachments"));
        Directory.CreateDirectory(root);
    }
    public async Task<StoredFile> SaveAsync(IFormFile file, bool imageOnly = false)
    {
        if (file.Length is <= 0 or > 5 * 1024 * 1024) throw new InvalidOperationException("El archivo debe tener entre 1 byte y 5 MB.");
        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer);
        var data = buffer.ToArray();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var png = data.Length >= 57 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) && data.AsSpan(12, 4).SequenceEqual("IHDR"u8) && data.AsSpan(data.Length - 12, 12).SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 });
        var jpg = data.Length >= 4 && data[0] == 255 && data[1] == 216 && data[2] == 255 && data[^2] == 255 && data[^1] == 217;
        var pdf = data.Length >= 10 && data.AsSpan(0, 5).SequenceEqual("%PDF-"u8) && System.Text.Encoding.Latin1.GetString(data.AsSpan(Math.Max(0, data.Length - 1024))).Contains("%%EOF");
        string type;
        if (png && extension == ".png") type = "image/png";
        else if (jpg && extension is ".jpg" or ".jpeg") type = "image/jpeg";
        else if (pdf && extension == ".pdf" && !imageOnly) type = "application/pdf";
        else throw new InvalidOperationException(imageOnly ? "Adjunta una imagen JPG o PNG válida." : "Adjunta un archivo JPG, PNG o PDF válido.");
        var key = Guid.NewGuid().ToString("N") + (type == "image/png" ? ".png" : type == "image/jpeg" ? ".jpg" : ".pdf");
        await File.WriteAllBytesAsync(Path.Combine(root, key), data);
        return new(key, type);
    }
    private string Resolve(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key != Path.GetFileName(key) || key.Contains('\\') || key.Contains('/')) throw new InvalidOperationException("Archivo inválido.");
        return Path.Combine(root, key);
    }
    public Stream Open(string key) => File.OpenRead(Resolve(key));
    public void Delete(string key) => File.Delete(Resolve(key));
}

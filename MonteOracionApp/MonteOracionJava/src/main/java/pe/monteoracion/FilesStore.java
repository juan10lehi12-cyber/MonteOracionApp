package pe.monteoracion;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import javax.imageio.ImageIO;
import jakarta.servlet.http.Part;

public final class FilesStore {
    public record Stored(String key, String type) { }
    private static Path root() throws IOException {
        Path path = Path.of(Db.config("MONTE_STORAGE","storage.path",System.getProperty("user.home")+"/MonteOracionJava/adjuntos")).toAbsolutePath().normalize();
        Files.createDirectories(path); return path;
    }
    public static Stored save(Part part, boolean imageOnly) throws IOException {
        Rules.require(part != null && part.getSize() > 0 && part.getSize() <= 5*1024*1024, "Adjunta un archivo de hasta 5 MB.");
        byte[] data;
        try (var input = part.getInputStream()) { data = input.readNBytes(5*1024*1024+1); }
        Rules.require(data.length <= 5*1024*1024, "Archivo demasiado grande.");
        String name = Objects.toString(part.getSubmittedFileName(),"").toLowerCase(Locale.ROOT);
        String type; String extension;
        if ((name.endsWith(".jpg") || name.endsWith(".jpeg") || name.endsWith(".png"))) {
            try (var imageInput = ImageIO.createImageInputStream(new ByteArrayInputStream(data))) {
                var readers = ImageIO.getImageReaders(imageInput);
                Rules.require(readers.hasNext(),"La imagen no es válida.");
                var reader = readers.next();
                try {
                    reader.setInput(imageInput);
                    String format = reader.getFormatName().toLowerCase(Locale.ROOT);
                    Rules.require(reader.getWidth(0)>0 && reader.getHeight(0)>0 && (long)reader.getWidth(0)*reader.getHeight(0)<=25000000,"Imagen demasiado grande.");
                    Rules.require(format.equals("png") && name.endsWith(".png") || format.equals("jpeg") && (name.endsWith(".jpg")||name.endsWith(".jpeg")),"Extensión de imagen inválida.");
                    Rules.require(reader.read(0)!=null,"Imagen dañada.");
                    extension = format.equals("png") ? ".png" : ".jpg"; type = format.equals("png") ? "image/png" : "image/jpeg";
                } finally { reader.dispose(); }
            }
        } else {
            Rules.require(!imageOnly && name.endsWith(".pdf") && data.length >= 10 && new String(data,0,5,java.nio.charset.StandardCharsets.US_ASCII).equals("%PDF-") && new String(data,Math.max(0,data.length-1024),Math.min(data.length,1024),java.nio.charset.StandardCharsets.ISO_8859_1).contains("%%EOF"),"Usa JPG, PNG o PDF válido.");
            type="application/pdf"; extension=".pdf";
        }
        String key=UUID.randomUUID()+extension;
        Files.write(root().resolve(key),data,StandardOpenOption.CREATE_NEW);
        return new Stored(key,type);
    }
    public static Path path(String key) throws IOException {
        Rules.require(key != null && key.matches("[a-f0-9-]{36}\\.(jpg|png|pdf)"),"Archivo inválido.");
        return root().resolve(key);
    }
    public static void discard(Stored file) {
        if(file!=null) try { Files.deleteIfExists(path(file.key())); } catch(IOException e) { java.util.logging.Logger.getLogger(FilesStore.class.getName()).warning("No se pudo retirar adjunto huérfano: "+file.key()); }
    }
}

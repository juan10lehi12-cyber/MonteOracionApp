using System.Globalization;
using System.Text;
namespace MonteOracionApp.Modules.Reports;

// Portable PDF 1.4 using the standard Courier font. Every text row is wrapped and pages have fixed margins.
public static class PdfReport
{
    public static byte[] Create(string title, string subtitle, IEnumerable<string> rows)
    {
        var lines = new List<string>();
        foreach (var row in rows) foreach(var paragraph in row.Replace("\r","").Split('\n')) lines.AddRange(Wrap(paragraph,90));
        if(lines.Count==0)lines.Add("Sin registros para este filtro.");
        var pages=lines.Chunk(49).ToArray();
        var objects=new List<byte[]> { Bytes("<< /Type /Catalog /Pages 2 0 R >>"), Array.Empty<byte>(), Bytes("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>") };
        var kids=new List<string>();
        for(var i=0;i<pages.Length;i++)
        {
            var pageId=objects.Count+1;var contentId=pageId+1;kids.Add(pageId+" 0 R");
            objects.Add(Bytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>"));
            var stream=new StringBuilder("BT /F1 14 Tf 42 795 Td ");
            foreach(var part in Wrap(title,60)){stream.Append('(').Append(Escape(part)).Append(") Tj 0 -18 Td ");}
            stream.Append("/F1 8 Tf (").Append(Escape(subtitle[..Math.Min(subtitle.Length,100)])).Append(") Tj ET\nBT /F1 9 Tf 42 730 Td 13 TL\n");
            foreach(var line in pages[i])stream.Append('(').Append(Escape(line)).Append(") Tj T*\n");
            stream.Append("ET\nBT /F1 8 Tf 42 35 Td (").Append(Escape($"Monte de Oración | {i+1}/{pages.Length} | Documento generado {Shared.ChurchTime.Now:dd/MM/yyyy HH:mm}")).Append(") Tj ET");
            var data=Bytes(stream.ToString());objects.Add(Bytes($"<< /Length {data.Length} >>\nstream\n").Concat(data).Concat(Bytes("\nendstream")).ToArray());
        }
        objects[1]=Bytes($"<< /Type /Pages /Count {pages.Length} /Kids [ {string.Join(" ",kids)} ] >>");
        using var output=new MemoryStream();
        void Write(string value)=>output.Write(Bytes(value));
        Write("%PDF-1.4\n%âãÏÓ\n");var offsets=new List<long>{0};
        for(var i=0;i<objects.Count;i++){offsets.Add(output.Position);Write($"{i+1} 0 obj\n");output.Write(objects[i]);Write("\nendobj\n");}
        var xref=output.Position;Write($"xref\n0 {objects.Count+1}\n0000000000 65535 f \n");
        foreach(var offset in offsets.Skip(1))Write(offset.ToString("0000000000",CultureInfo.InvariantCulture)+" 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }
    private static byte[] Bytes(string s)=>Encoding.Latin1.GetBytes(s);
    private static string Escape(string s)=>new(s.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)").Replace('—','-').Replace('–','-').Select(c=>c<32?' ':c>255?'?':c).ToArray());
    private static IEnumerable<string> Wrap(string text,int width)
    {
        if(text.Length==0){yield return "";yield break;}
        while(text.Length>width)
        {
            var split=text.LastIndexOf(' ',width);if(split<width/2)split=width;
            yield return text[..split];text=text[split..].TrimStart();
        }
        yield return text;
    }
}

package pe.monteoracion;

import java.io.*;
import java.sql.Connection;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import org.apache.pdfbox.pdmodel.*;
import org.apache.pdfbox.pdmodel.common.PDRectangle;
import org.apache.pdfbox.pdmodel.font.PDType1Font;
import static pe.monteoracion.Rules.*;

public final class Reports {
    public static byte[] create(HttpServletRequest r) throws Exception {
        String type=choice(p(r,"tipo"),"donaciones","dinero","almacen","cierres");
        if(type.equals("dinero")||type.equals("cierres")) Security.role(r,"ADMINISTRADOR","TESORERO");
        if(type.equals("almacen")) Security.role(r,"ADMINISTRADOR","ALMACENERO");
        var from=date(p(r,"desde")); var to=date(p(r,"hasta")); require(!from.isAfter(to),"Rango de fechas inválido.");
        var rows=Db.read(c->rows(c,r,type,from,to)); require(rows.size()<=10000,"El reporte supera 10 000 movimientos. Reduce el rango de fechas.");
        var lines=new ArrayList<String>();
        lines.add("Monte de Oración - "+type.toUpperCase(Locale.ROOT)); lines.add("Desde "+from+" hasta "+to+" | Emitido: "+now()); lines.add("Registros: "+rows.size()); lines.add("");
        if(!rows.isEmpty()) lines.add(String.join(" | ",rows.get(0).keySet()));
        for(var row:rows) lines.add(String.join(" | ",row.values().stream().map(v->Objects.toString(v,"")).toList()));
        return pdf(lines);
    }
    private static List<Map<String,Object>> rows(Connection c,HttpServletRequest r,String type,java.time.LocalDate from,java.time.LocalDate to) throws Exception {
        return switch(type) {
            case "donaciones" -> Db.rows(c,"SELECT d.id,d.fecha,d.tipo,d.estado,COALESCE(CONCAT(c.nombres,' ',c.apellidos),d.externo_nombre,'Anónimo') donante,d.importe FROM donacion d LEFT JOIN congregante c ON c.id=d.congregante_id WHERE d.fecha BETWEEN ? AND ? AND "+OperationPages.donationScope(Security.user(r),"d")+" ORDER BY d.fecha,d.id LIMIT 10001",from,to);
            case "dinero" -> Db.rows(c,"SELECT m.id,m.fecha,f.nombre fondo,m.canal,m.tipo,m.importe,m.detalle FROM movimiento_dinero m JOIN fondo f ON f.id=m.fondo_id WHERE m.fecha BETWEEN ? AND ? ORDER BY m.fecha,m.id LIMIT 10001",from,to);
            case "almacen" -> Db.rows(c,"SELECT m.id,m.fecha,p.nombre producto,m.lote_id,m.tipo,m.cantidad,m.reserva_cambio,m.motivo FROM movimiento_almacen m JOIN lote l ON l.id=m.lote_id JOIN producto p ON p.id=l.producto_id WHERE m.fecha BETWEEN ? AND ? ORDER BY m.fecha,m.id LIMIT 10001",from,to);
            case "cierres" -> Db.rows(c,"SELECT fecha,canal,apertura,ingresos,salidas,esperado,contado,notas FROM cierre_caja WHERE fecha BETWEEN ? AND ? ORDER BY fecha,id LIMIT 10001",from,to);
            default -> throw new IllegalArgumentException("Reporte inválido");
        };
    }
    static byte[] pdf(List<String> lines) throws IOException {
        try(var document=new PDDocument(); var bytes=new ByteArrayOutputStream()) {
            PDPageContentStream stream=null; float y=0; int pageNo=0;
            try {
                for(String original:lines) {
                    var safe=new StringBuilder();
                    for(int cp:original.codePoints().toArray()) {
                        String ch=new String(Character.toChars(cp));
                        try { PDType1Font.HELVETICA.encode(ch); safe.append(ch); } catch(Exception e) { safe.append(' '); }
                    }
                    String text=safe.toString();
                    do {
                        int end=Math.min(text.length(),100);
                        while(end>1&&PDType1Font.HELVETICA.getStringWidth(text.substring(0,end))*9/1000>PDRectangle.A4.getWidth()-80) end--;
                        String line=text.substring(0,end); text=text.substring(end);
                        if(stream==null||y<55) {
                            if(stream!=null) stream.close();
                            var page=new PDPage(PDRectangle.A4); document.addPage(page); pageNo++;
                            stream=new PDPageContentStream(document,page); stream.beginText(); stream.setFont(PDType1Font.HELVETICA,9); stream.newLineAtOffset(40,28); stream.showText("Monte de Oración | Página "+pageNo); stream.endText(); y=790;
                        }
                        stream.beginText(); stream.setFont(PDType1Font.HELVETICA,9); stream.newLineAtOffset(40,y); stream.showText(line); stream.endText(); y-=14;
                    } while(!text.isEmpty());
                }
            } finally { if(stream!=null) stream.close(); }
            if(document.getNumberOfPages()==0) document.addPage(new PDPage());
            document.save(bytes); return bytes.toByteArray();
        }
    }
}

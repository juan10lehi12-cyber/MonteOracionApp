package pe.monteoracion;

import java.sql.Connection;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;
import static pe.monteoracion.Html.columns;

public final class OperationPages {
    private final HttpServletRequest r;
    private final Pages page;
    private final Html h;
    public OperationPages(HttpServletRequest request,Pages pages) { r=request; page=pages; h=pages.h; }
    public static String donationScope(Security.User user,String alias) {
        if(user.has("ADMINISTRADOR")||user.has("TESORERO")&&user.has("ALMACENERO")) return "1=1";
        return alias+".tipo='"+(user.has("TESORERO")?"DINERO":"BIENES")+"'";
    }
    public void donations(Connection c) throws Exception {
        h.heading("Donaciones").link("/recibir","Registrar recepción");
        String term=text(p(r,"buscar"),180,false), status=p(r,"estado");
        h.raw("<form method=get action=\"").text(h.url("/donaciones")).raw("\">").input("buscar","Buscar por código, nombre o número","text",term,180,false);
        var options=new LinkedHashMap<String,String>(); for(String s:List.of("PENDIENTE","APROBADA","RECHAZADA","ANULADA")) options.put(s,s);
        h.select("estado","Estado",options,status,false).submit("Buscar");
        String where=" WHERE "+donationScope(Security.user(r),"d")+" AND (?='' OR d.estado=?) AND (?='' OR d.codigo=? OR CAST(d.id AS CHAR)=? OR d.externo_nombre LIKE ? OR CONCAT(c.nombres,' ',c.apellidos) LIKE ?)";
        var rows=Db.rows(c,"SELECT d.id,d.fecha,d.tipo,d.estado,d.importe,COALESCE(CONCAT(c.nombres,' ',c.apellidos),d.externo_nombre,'Anónimo') donante FROM donacion d LEFT JOIN congregante c ON c.id=d.congregante_id"+where+" ORDER BY d.id DESC LIMIT 30 OFFSET ?",status,status,term,term,term,"%"+term+"%","%"+term+"%",page.offset());
        h.table(rows,columns("id","Nº","fecha","Fecha","donante","Donante","tipo","Tipo","importe","S/","estado","Estado"),"/donacion?id=");
        page.pager("/donaciones?buscar="+java.net.URLEncoder.encode(term,java.nio.charset.StandardCharsets.UTF_8)+"&estado="+java.net.URLEncoder.encode(status,java.nio.charset.StandardCharsets.UTF_8),rows.size());
    }
    public static Map<String,Object> visibleDonation(Connection c,HttpServletRequest r,long id) throws Exception {
        return Db.one(c,"SELECT d.*,COALESCE(CONCAT(c.nombres,' ',c.apellidos),d.externo_nombre,'Anónimo') donante FROM donacion d LEFT JOIN congregante c ON c.id=d.congregante_id WHERE d.id=? AND "+donationScope(Security.user(r),"d"),id);
    }
    public void donation(Connection c) throws Exception {
        long id=id(r); var row=visibleDonation(c,r,id);
        h.heading("Donación #"+id).details(row,"codigo","Código de seguimiento","fecha","Fecha declarada","donante","Donante","tipo","Tipo","estado","Estado","importe","Importe S/","medio_nombre","Medio","operacion","Operación");
        if(row.get("archivo_clave")!=null) h.link("/archivo?tipo=donacion&id="+id,"Ver comprobante / foto");
        h.table(Db.rows(c,"SELECT descripcion,cantidad,unidad,vencimiento,ubicacion FROM donacion_bien WHERE donacion_id=?",id),columns("descripcion","Bien","cantidad","Cantidad","unidad","Unidad","vencimiento","Vencimiento","ubicacion","Ubicación"),null);
        h.sub("Historial de decisiones").table(Db.rows(c,"SELECT d.estado,d.motivo,d.creado,p.nombre FROM donacion_decision d JOIN personal p ON p.id=d.actor_id WHERE donacion_id=? ORDER BY d.id",id),columns("creado","Fecha","estado","Estado","nombre","Personal","motivo","Motivo"),null);
        String status=Db.str(row,"estado");
        if(Security.user(r).has("ADMINISTRADOR","TESORERO")&&(Security.user(r).has("ADMINISTRADOR")||Db.str(row,"tipo").equals("DINERO"))&&Set.of("PENDIENTE","APROBADA").contains(status)) {
            h.sub("Revisar donación").startForm("/donacion",false).hidden("id",id).hidden("version",row.get("version"));
            if(status.equals("PENDIENTE")) h.choices("estado","Decisión","APROBADA","APROBADA","RECHAZADA"); else h.hidden("estado","ANULADA").note("La anulación agrega un movimiento inverso, sin borrar el historial.");
            if(status.equals("PENDIENTE")&&row.get("congregante_id")==null) h.options("congregante_id","Vincular a congregante (opcional)",Db.rows(c,"SELECT id,CONCAT(nombres,' ',apellidos) nombre FROM congregante WHERE activo=1 ORDER BY apellidos"),"id","nombre",null,false);
            h.area("motivo","Motivo (obligatorio para rechazar o anular)","",1000,false).check("confirmado","Confirmo que se recibió el dinero",false).submit("Registrar decisión");
        }
    }
    public void stock(Connection c) throws Exception {
        h.heading("Existencias de almacén");
        var rows=Db.rows(c,"SELECT l.id,p.nombre,p.unidad,l.ubicacion,l.vencimiento,l.existencia,l.reservado,l.existencia-l.reservado disponible FROM lote l JOIN producto p ON p.id=l.producto_id ORDER BY l.id DESC LIMIT 30 OFFSET ?",page.offset());
        h.table(rows,columns("id","Lote","nombre","Producto","unidad","Unidad","ubicacion","Ubicación","vencimiento","Vencimiento","existencia","Existencia","reservado","Reservado","disponible","Disponible"),"/lote?id="); page.pager("/almacen",rows.size());
    }
    public void lot(Connection c) throws Exception {
        long id=id(r); var row=Db.one(c,"SELECT l.*,p.nombre,p.unidad FROM lote l JOIN producto p ON p.id=l.producto_id WHERE l.id=?",id);
        h.heading("Lote #"+id).details(row,"nombre","Producto","unidad","Unidad","ubicacion","Ubicación","origen","Origen","vencimiento","Vencimiento","existencia","Existencia","reservado","Reservado");
        h.sub("Movimientos").table(Db.rows(c,"SELECT m.id,m.tipo,m.cantidad,m.reserva_cambio,m.motivo,m.fecha,p.nombre FROM movimiento_almacen m JOIN personal p ON p.id=m.actor_id WHERE m.lote_id=? ORDER BY m.id DESC LIMIT 30 OFFSET ?",id,page.offset()),columns("id","Nº","fecha","Fecha","tipo","Tipo","cantidad","Cambio","reserva_cambio","Reserva","nombre","Personal","motivo","Motivo"),null);
        page.pager("/lote?id="+id,Db.rows(c,"SELECT id FROM movimiento_almacen WHERE lote_id=? LIMIT 30 OFFSET ?",id,page.offset()).size());
        if(Security.user(r).has("ADMINISTRADOR")) h.sub("Ajuste auditado").startForm("/ajuste-almacen",false).hidden("id",id).hidden("comando",UUID.randomUUID()).choices("tipo","Tipo","MERMA","AJUSTE_POSITIVO","AJUSTE_NEGATIVO","MERMA").input("cantidad","Cantidad positiva","number","",0,true).area("motivo","Motivo","",1000,true).submit("Registrar ajuste");
    }
    public void stockRequests(Connection c) throws Exception {
        h.heading("Solicitudes de bienes").link("/bienes-nueva","Nueva solicitud");
        var rows=Db.rows(c,"SELECT id,beneficiario_nombre,tipo,prioridad,necesaria,estado FROM solicitud_bienes ORDER BY id DESC LIMIT 30 OFFSET ?",page.offset());
        h.table(rows,columns("id","Nº","beneficiario_nombre","Beneficiario","tipo","Tipo","prioridad","Prioridad","necesaria","Necesaria","estado","Estado"),"/bienes-detalle?id="); page.pager("/bienes",rows.size());
    }
    public void stockNew(Connection c) throws Exception {
        h.heading("Solicitar bienes").note("La aprobación reservará la cantidad. La entrega confirmada descontará la existencia.").startForm("/bienes",false);
        h.options("beneficiario_id","Beneficiario",Db.rows(c,"SELECT id,nombre FROM beneficiario WHERE activo=1 ORDER BY nombre"),"id","nombre",null,true).choices("tipo","Tipo","ENTREGA","ENTREGA","USO_INTERNO","PRESTAMO").choices("prioridad","Prioridad","NORMAL","NORMAL","ALTA","URGENTE").input("necesaria","Fecha necesaria","date",today(),0,true).input("devolucion","Límite de devolución (préstamo)","date","",0,false).area("motivo","Motivo y justificación","",1000,true);
        var lots=Db.rows(c,"SELECT l.id,CONCAT('#',l.id,' · ',p.nombre,' · ',l.existencia-l.reservado,' ',p.unidad,' · ',l.ubicacion) nombre FROM lote l JOIN producto p ON p.id=l.producto_id WHERE p.activo=1 AND l.existencia>l.reservado AND (l.vencimiento IS NULL OR l.vencimiento>=?) ORDER BY p.nombre,l.id",today());
        for(int i=1;i<=3;i++) h.raw("<fieldset><legend>Línea "+i+"</legend>").options("lote_id","Lote",lots,"id","nombre",null,false).input("cantidad","Cantidad","number","",0,false).raw("</fieldset>");
        h.submit("Enviar solicitud");
    }
    public void stockDetail(Connection c) throws Exception {
        long id=id(r); var row=Db.one(c,"SELECT * FROM solicitud_bienes WHERE id=?",id); var user=Security.user(r); String status=Db.str(row,"estado");
        h.heading("Solicitud de bienes #"+id).details(row,"beneficiario_nombre","Beneficiario","beneficiario_direccion","Dirección al solicitar","beneficiario_contacto","Contacto al solicitar","tipo","Tipo","estado","Estado","prioridad","Prioridad","motivo","Motivo","necesaria","Fecha necesaria","devolucion","Límite devolución","decision_motivo","Decisión");
        var lines=Db.rows(c,"SELECT b.*,p.nombre,p.unidad FROM solicitud_bien b JOIN lote l ON l.id=b.lote_id JOIN producto p ON p.id=l.producto_id WHERE solicitud_id=? ORDER BY b.id",id);
        h.table(lines,columns("id","Línea","lote_id","Lote","nombre","Producto","unidad","Unidad","cantidad","Solicitado","entregado","Entregado","devuelto","Devuelto","condonado","Condonado"),null);
        if(status.equals("PENDIENTE")&&user.has("ADMINISTRADOR")) decisionForm("/bienes-decision",row,"aprobar","rechazar");
        if(Set.of("PENDIENTE","APROBADA","PARCIAL").contains(status)&&(user.has("ADMINISTRADOR")||Db.id(row,"solicitante_id")==user.id()&&status.equals("PENDIENTE"))) decisionForm("/bienes-decision",row,"cancelar");
        for(var line:lines) {
            if(Set.of("APROBADA","PARCIAL").contains(status)&&user.has("ADMINISTRADOR","ALMACENERO")) {
                h.raw("<details><summary>Entregar línea #").text(line.get("id")).raw("</summary>").startForm("/bienes-linea",true).hidden("linea_id",line.get("id")).hidden("id",id).hidden("accion","entregar").hidden("comando",UUID.randomUUID()).input("cantidad","Cantidad a entregar","number","",0,true).input("receptor","Nombre del receptor","text","",160,true).file("Constancia de entrega",true,false).submit("Confirmar entrega").raw("</details>");
            }
            if(Db.str(row,"tipo").equals("PRESTAMO")&&decimal(line,"entregado").subtract(decimal(line,"devuelto")).subtract(decimal(line,"condonado")).signum()>0&&user.has("ADMINISTRADOR","ALMACENERO")) {
                h.raw("<details><summary>Reposición de línea #").text(line.get("id")).raw("</summary>").startForm("/bienes-linea",true).hidden("linea_id",line.get("id")).hidden("id",id).hidden("comando",UUID.randomUUID());
                if(user.has("ADMINISTRADOR")) h.choices("accion","Operación","devolver","devolver","condonar"); else h.hidden("accion","devolver");
                h.input("cantidad","Cantidad","number","",0,true).input("ubicacion","Ubicación para la reposición","text","",160,false).input("vencimiento","Vencimiento para la reposición","date","",0,false).area("motivo","Motivo","",1000,true).submit("Registrar reposición / condonación").raw("</details>");
            }
        }
        var movements=Db.rows(c,"SELECT m.id,m.tipo,m.cantidad,m.receptor,m.motivo,m.fecha,m.archivo_clave FROM movimiento_almacen m JOIN solicitud_bien b ON b.id=m.linea_id WHERE b.solicitud_id=? ORDER BY m.id DESC LIMIT 100",id);
        h.sub("Movimientos de la solicitud").table(movements,columns("id","Nº","tipo","Tipo","cantidad","Cantidad","receptor","Receptor","motivo","Motivo","fecha","Fecha"),null);
        for(var movement:movements) if(movement.get("archivo_clave")!=null) h.link("/archivo?tipo=almacen&id="+movement.get("id"),"Constancia #"+movement.get("id"));
    }
    private void decisionForm(String route,Map<String,Object> row,String... actions) {
        h.startForm(route,false).hidden("id",row.get("id")).hidden("version",row.get("version"));
        h.choices("accion","Acción",actions[0],actions).area("motivo","Motivo (obligatorio para rechazar o cancelar)","",1000,false).submit("Registrar decisión");
    }
    public void moneyRequests(Connection c) throws Exception {
        h.heading("Solicitudes de dinero").link("/dinero-nueva","Nueva solicitud");
        var rows=Db.rows(c,"SELECT s.id,f.nombre fondo,s.canal,s.tipo,s.importe,s.destinatario,s.estado FROM solicitud_dinero s JOIN fondo f ON f.id=s.fondo_id ORDER BY s.id DESC LIMIT 30 OFFSET ?",page.offset());
        h.table(rows,columns("id","Nº","fondo","Fondo","canal","Canal","tipo","Tipo","importe","S/","destinatario","Destinatario","estado","Estado"),"/dinero-detalle?id="); page.pager("/dinero",rows.size());
    }
    public void moneyNew(Connection c) throws Exception {
        h.heading("Solicitar dinero").startForm("/dinero",false).options("fondo_id","Fondo",Db.rows(c,"SELECT id,nombre FROM fondo WHERE activo=1 ORDER BY nombre"),"id","nombre",null,true).choices("canal","Canal","EFECTIVO","EFECTIVO","DIGITAL");
        if(Security.user(r).has("ADMINISTRADOR")) h.choices("tipo","Tipo","GASTO","GASTO","ANTICIPO","SALDO_INICIAL","AJUSTE_INGRESO","AJUSTE_SALIDA"); else h.choices("tipo","Tipo","GASTO","GASTO","ANTICIPO");
        h.input("importe","Importe S/","number","",0,true).input("destinatario","Proveedor o responsable","text","",160,true).input("limite","Límite de rendición para anticipo","date","",0,false).area("motivo","Propósito y justificación","",1000,true).submit("Enviar solicitud");
    }
    public void moneyDetail(Connection c) throws Exception {
        long id=id(r); var row=Db.one(c,"SELECT s.*,f.nombre fondo FROM solicitud_dinero s JOIN fondo f ON f.id=s.fondo_id WHERE s.id=?",id); var user=Security.user(r); String status=Db.str(row,"estado");
        h.heading("Solicitud de dinero #"+id).details(row,"fondo","Fondo","canal","Canal","tipo","Tipo","importe","Importe S/","destinatario","Destinatario","motivo","Motivo","estado","Estado","limite","Límite de rendición","pagado_fecha","Fecha de ejecución","gasto_real","Gasto real","decision_motivo","Decisión");
        if(row.get("archivo_clave")!=null) h.link("/archivo?tipo=dinero&id="+id,"Comprobante de ejecución"); if(row.get("rendicion_clave")!=null) h.link("/archivo?tipo=rendicion&id="+id,"Rendición");
        if(status.equals("PENDIENTE")&&user.has("ADMINISTRADOR")) decisionForm("/dinero-accion",row,"aprobar","rechazar");
        if(Set.of("PENDIENTE","APROBADA").contains(status)&&(user.has("ADMINISTRADOR")||Db.id(row,"solicitante_id")==user.id()&&status.equals("PENDIENTE"))) decisionForm("/dinero-accion",row,"cancelar");
        if(status.equals("APROBADA")&&user.has("ADMINISTRADOR","TESORERO")) h.sub("Ejecutar movimiento").startForm("/dinero-accion",true).hidden("id",id).hidden("version",row.get("version")).hidden("accion","pagar").file("Comprobante o constancia",true,false).submit("Confirmar pago / ingreso efectivo");
        if(status.equals("PAGADA")&&Db.str(row,"tipo").equals("ANTICIPO")&&user.has("ADMINISTRADOR","TESORERO")) h.sub("Rendir anticipo").note("Confirma el gasto real y la recepción del sobrante, cuando corresponda.").startForm("/dinero-accion",true).hidden("id",id).hidden("version",row.get("version")).hidden("accion","rendir").input("gasto_real","Gasto real S/","number","",0,true).file("Documento de rendición",true,false).submit("Confirmar rendición y devolución");
    }
    public void balances(Connection c) throws Exception {
        h.heading("Fondos y saldos").note("Disponible = saldo real menos solicitudes aprobadas pendientes de ejecución.");
        var rows=Db.rows(c,"SELECT f.id,f.nombre fondo,k.canal,COALESCE((SELECT SUM(m.importe) FROM movimiento_dinero m WHERE m.fondo_id=f.id AND m.canal=k.canal),0) saldo,COALESCE((SELECT SUM(s.importe) FROM solicitud_dinero s WHERE s.fondo_id=f.id AND s.canal=k.canal AND s.estado='APROBADA' AND s.tipo NOT IN ('SALDO_INICIAL','AJUSTE_INGRESO')),0) reservado FROM fondo f CROSS JOIN canal_control k ORDER BY f.nombre,k.canal");
        for(var row:rows) row.put("disponible",decimal(row,"saldo").subtract(decimal(row,"reservado")));
        h.table(rows,columns("fondo","Fondo","canal","Canal","saldo","Saldo real","reservado","Reservado","disponible","Disponible"),null);
    }
    public void closings(Connection c) throws Exception {
        h.heading("Cierres y conciliación").note("Cada cierre bloquea las fechas anteriores de ese canal. Las diferencias se corrigen mediante un ajuste autorizado en fecha abierta.");
        h.startForm("/cierres",true).hidden("accion","crear").choices("canal","Canal","EFECTIVO","EFECTIVO","DIGITAL").input("fecha","Fecha de cierre","date",today(),0,true).input("contado","Saldo contado / conciliado S/","number","",0,true).area("notas","Explicación de diferencias","",1000,false).file("Estado de cuenta (obligatorio para digital)",false,false).submit("Registrar cierre");
        var rows=Db.rows(c,"SELECT * FROM cierre_caja ORDER BY fecha DESC,id DESC LIMIT 30 OFFSET ?",page.offset());
        h.table(rows,columns("id","Nº","fecha","Fecha","canal","Canal","apertura","Apertura","ingresos","Ingresos","salidas","Salidas","esperado","Esperado","contado","Contado","notas","Notas","revisado_por","Revisado por"),null);
        for(var row:rows) {
            if(row.get("archivo_clave")!=null) h.link("/archivo?tipo=cierre&id="+row.get("id"),"Constancia de cierre #"+row.get("id"));
            if(row.get("revisado_por")==null&&Security.user(r).has("ADMINISTRADOR")) h.startForm("/cierres",false).hidden("id",row.get("id")).hidden("accion","revisar").submit("Revisar cierre #"+row.get("id"));
        } page.pager("/cierres",rows.size());
    }
    public void audit(Connection c) throws Exception {
        h.heading("Auditoría del personal"); var rows=Db.rows(c,"SELECT a.id,a.creado,p.nombre,a.accion,a.referencia,a.detalle FROM auditoria a JOIN personal p ON p.id=a.actor_id ORDER BY a.id DESC LIMIT 30 OFFSET ?",page.offset());
        h.table(rows,columns("id","Nº","creado","Fecha","nombre","Personal","accion","Acción","referencia","Referencia","detalle","Detalle"),null); page.pager("/auditoria",rows.size());
    }
    public void reports() {
        h.heading("Reportes PDF").note("Selecciona un rango de fechas. Los reportes conservan el alcance permitido por tu rol.");
        h.raw("<form method=get action=\"").text(h.url("/reporte.pdf")).raw("\">");
        var user=Security.user(r); var options=new LinkedHashMap<String,String>(); options.put("donaciones","Donaciones");
        if(user.has("ADMINISTRADOR","TESORERO")) { options.put("dinero","Movimientos de dinero"); options.put("cierres","Cierres de caja"); }
        if(user.has("ADMINISTRADOR","ALMACENERO")) options.put("almacen","Movimientos de almacén");
        h.select("tipo","Reporte",options,"donaciones",true).input("desde","Desde","date",today().withDayOfMonth(1),0,true).input("hasta","Hasta","date",today(),0,true).submit("Descargar PDF");
    }
}

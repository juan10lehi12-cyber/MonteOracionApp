package pe.monteoracion;

import java.sql.Connection;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;
import static pe.monteoracion.Html.columns;

public final class Pages {
    private final HttpServletRequest r;
    public final Html h;
    public Pages(HttpServletRequest request) { r=request; h=new Html(r); }
    public int page() { try { return Math.max(1,Math.min(100000,Integer.parseInt(p(r,"pagina")))); } catch(Exception e) { return 1; } }
    public int offset() { return (page()-1)*30; }
    public void pager(String base,int count) {
        h.raw("<nav class=pager aria-label=Paginación>");
        String separator=base.contains("?")?"&":"?";
        if(page()>1) h.link(base+separator+"pagina="+(page()-1),"Anterior");
        h.text("Página "+page()); if(count==30) h.link(base+separator+"pagina="+(page()+1),"Siguiente"); h.raw("</nav>");
    }
    public void home(Connection c) throws Exception {
        var church=Db.one(c,"SELECT * FROM iglesia WHERE id=1");
        h.raw("<section class=hero><small>GENEROSIDAD Y COMUNIDAD</small>").heading(Db.str(church,"nombre"));
        h.link("/donar","Registrar donación").link("/consulta","Mis donaciones").raw("</section>");
        h.sub("Medios para donar").note("Realiza el pago y registra el comprobante para su verificación. También recibimos bienes de forma presencial.");
        h.raw("<div class=grid>");
        for(var method:Db.rows(c,"SELECT * FROM medio_pago WHERE activo=1 ORDER BY nombre")) {
            h.raw("<article class=card><h3>").text(method.get("nombre")).raw("</h3><p class=preserve>").text(method.get("instrucciones")).raw("</p>");
            if(method.get("qr_clave")!=null) h.raw("<img alt=\"Código QR para donar\" src=\"").text(h.url("/archivo?tipo=qr&id="+method.get("id"))).raw("\">"); h.raw("</article>");
        } h.raw("</div>");
        h.sub("Eventos e información");
        var events=Db.rows(c,"SELECT * FROM publicacion WHERE publicado=1 ORDER BY COALESCE(inicio,creado) DESC LIMIT 30 OFFSET ?",offset());
        for(var event:events) event(event);
        pager("/",events.size()); h.sub("Conoce nuestra iglesia").raw("<p class=preserve>").text(church.get("informacion")).raw("</p><p>").text(church.get("direccion")).raw(" · ").text(church.get("contacto")).raw("</p>");
    }
    private void event(Map<String,Object> event) {
        h.raw("<article class=card><h3>").text(event.get("titulo")).raw("</h3>");
        if(Db.yes(event,"cancelado")) h.note("Evento cancelado");
        h.raw("<p>").text(event.get("inicio")).raw(" · ").text(event.get("lugar")).raw("</p><p class=preserve>").text(event.get("contenido")).raw("</p>");
        if(event.get("imagen_clave")!=null) h.raw("<img loading=lazy alt=\"Imagen de la publicación\" src=\"").text(h.url("/archivo?tipo=evento&id="+event.get("id"))).raw("\">"); h.raw("</article>");
    }
    public void login() {
        h.heading("Acceso del personal").startForm("/login",false).input("email","Correo","email","",254,true).input("password","Contraseña","password","",128,true).submit("Ingresar");
    }
    public void password() {
        h.heading("Cambiar contraseña").startForm("/password",false).input("actual","Contraseña actual","password","",128,true).input("nueva","Nueva contraseña (10-128 caracteres)","password","",128,true).input("repetir","Repetir nueva contraseña","password","",128,true).submit("Actualizar contraseña");
    }
    public void donate(Connection c,boolean received) throws Exception {
        h.heading(received?"Registrar recepción presencial":"Registrar donación digital");
        h.note(received?"Registra solo lo que ya recibiste físicamente.":"La iglesia revisará el comprobante antes de aprobar. Elige el fondo al que deseas destinar el aporte.");
        h.startForm(received?"/recibir":"/donar",true);
        if(received) {
            var user=Security.user(r);
            var options=new LinkedHashMap<String,String>(); if(user.has("ADMINISTRADOR","TESORERO")) options.put("DINERO","Dinero en efectivo"); if(user.has("ADMINISTRADOR","ALMACENERO")) options.put("BIENES","Bienes");
            h.select("tipo","Tipo de recepción",options,"DINERO",true);
        }
        h.choices("donante_tipo","Donante","ANONIMO","CONGREGANTE","EXTERNO","ANONIMO").input("dni","DNI (solo si es congregante)","text","",8,false).input("externo_nombre","Nombre del donante externo","text","",180,false).input("externo_contacto","Contacto externo (opcional)","text","",150,false);
        h.options("fondo_id","Fondo",Db.rows(c,"SELECT id,nombre FROM fondo WHERE activo=1 ORDER BY nombre"),"id","nombre","1",true);
        h.input("importe","Importe S/ (solo dinero)","number","",0,!received).input("fecha","Fecha de recepción o pago","date",today(),0,true);
        if(!received) h.options("medio_pago_id","Medio de pago",Db.rows(c,"SELECT id,nombre FROM medio_pago WHERE activo=1"),"id","nombre",null,true).input("operacion","Número de operación","text","",80,true);
        if(received) {
            h.raw("<details><summary>Bienes recibidos (hasta 3 líneas por recepción)</summary>");
            var products=Db.rows(c,"SELECT id,CONCAT(nombre,' / ',unidad) nombre FROM producto WHERE activo=1 ORDER BY nombre");
            for(int i=1;i<=3;i++) h.raw("<fieldset><legend>Bien "+i+"</legend>").options("producto_id","Producto",products,"id","nombre",null,false).input("cantidad","Cantidad","number","",0,false).input("ubicacion","Ubicación","text","",160,false).input("vencimiento","Vencimiento","date","",0,false).raw("</fieldset>");
            h.raw("</details>");
        }
        h.file(received?"Foto obligatoria para bienes; comprobante opcional para efectivo":"Comprobante JPG o PNG",!received,!received);
        if(received) h.check("confirmado","Confirmo la recepción efectiva del dinero o bienes",false);
        h.submit(received?"Registrar recepción":"Enviar para revisión");
    }
    public void consultation(Connection c,boolean submitted) throws Exception {
        h.heading("Mis donaciones").note("Ingresa tu DNI y el código privado de consulta que te entrega la iglesia.").startForm("/consulta",false).input("dni","DNI","text","",8,true).input("codigo","Código privado de consulta","text","",36,true).submit("Consultar");
        if(!submitted) return;
        String dni=p(r,"dni"), code=p(r,"codigo"); require(dni.matches("[0-9]{8}")&&code.length()==36,"Revisa el DNI y el código.");
        var members=Db.rows(c,"SELECT id FROM congregante WHERE dni=? AND consulta_codigo=? AND activo=1",dni,code);
        if(members.isEmpty()) { h.note("No se encontraron resultados con esos datos."); return; }
        long member=Db.id(members.get(0),"id");
        int page=page(); var donations=Db.rows(c,"SELECT fecha,tipo,importe,estado FROM donacion WHERE congregante_id=? ORDER BY id DESC LIMIT 30 OFFSET ?",member,(page-1)*30);
        h.sub("Historial de donaciones").table(donations,columns("fecha","Fecha","tipo","Tipo","importe","S/","estado","Estado"),null);
        if(page>1||donations.size()==30) {
            h.startForm("/consulta",false).hidden("dni",dni).hidden("codigo",code);
            h.input("pagina","Página","number",page+(donations.size()==30?1:-1),0,true).submit("Cambiar página");
        }
        h.sub("Próximos eventos asignados");
        for(var event:Db.rows(c,"SELECT p.* FROM publicacion p JOIN participante_evento e ON e.publicacion_id=p.id WHERE e.congregante_id=? AND e.activo=1 AND p.publicado=1 AND p.cancelado=0 AND p.inicio>=? ORDER BY p.inicio LIMIT 100",member,now())) event(event);
    }
    public void panel(Connection c) throws Exception {
        h.heading("Mi panel").note("Bienvenido, "+Security.user(r).name()+". Cada operación queda asociada a tu cuenta.").raw("<div class=grid>");
        h.raw("<article class=card><h2>Donaciones</h2>").link("/donaciones","Revisar registros").link("/recibir","Registrar recepción").raw("</article>");
        h.raw("<article class=card><h2>Distribución</h2>").link("/bienes","Solicitudes de bienes").link("/dinero","Solicitudes de dinero").raw("</article>");
        h.raw("<article class=card><h2>Seguimiento</h2>").link("/reportes","Reportes PDF").raw("</article></div>");
        if(Security.user(r).has("ADMINISTRADOR","ALMACENERO")) h.sub("Lotes próximos a vencer").table(Db.rows(c,"SELECT l.id,p.nombre,l.existencia,l.reservado,l.vencimiento FROM lote l JOIN producto p ON p.id=l.producto_id WHERE l.existencia>0 AND l.vencimiento BETWEEN ? AND ? ORDER BY l.vencimiento LIMIT 20",today(),today().plusDays(30)),columns("id","Lote","nombre","Producto","existencia","Existencia","reservado","Reservado","vencimiento","Vencimiento"),"/lote?id=");
        if(Security.user(r).has("ADMINISTRADOR","TESORERO")) h.sub("Anticipos pendientes de rendición").table(Db.rows(c,"SELECT id,destinatario,importe,limite FROM solicitud_dinero WHERE tipo='ANTICIPO' AND estado='PAGADA' ORDER BY limite LIMIT 20"),columns("id","Nº","destinatario","Responsable","importe","Importe","limite","Límite"),"/dinero-detalle?id=");
    }
    public void catalog(Connection c) throws Exception {
        String module=p(r,"modulo"); var def=Catalog.ALL.get(module); require(def!=null,"Módulo inválido.");
        Security.role(r,def.roles().toArray(String[]::new));
        h.heading(def.title()); String id=p(r,"id");
        if(!id.isBlank()) {
            Map<String,Object> row=id.equals("nuevo")?Map.of("activo",true):Db.one(c,"SELECT * FROM "+def.table()+" WHERE id=?",number(id));
            h.startForm("/catalogo",true).hidden("modulo",module);
            if(!id.equals("nuevo")) h.hidden("id",id);
            for(var field:def.fields()) {
                Object value=row.get(field.name());
                switch(field.type()) {
                    case "checkbox" -> h.check(field.name(),field.label(),Db.yes(row,field.name()));
                    case "textarea" -> h.area(field.name(),field.label(),value,field.max(),field.required());
                    default -> h.input(field.name(),field.label(),field.type(),value,field.max(),field.required());
                }
            }
            if(module.equals("medios")) h.file("QR (opcional)",false,true);
            h.submit("Guardar");
            if(module.equals("congregantes")&&row.containsKey("consulta_codigo")) h.note("Código privado de consulta para entregar a esta persona: "+row.get("consulta_codigo"));
            return;
        }
        h.link("/catalogo?modulo="+module+"&id=nuevo","Nuevo registro");
        var rows=Db.rows(c,"SELECT * FROM "+def.table()+" ORDER BY id DESC LIMIT 30 OFFSET ?",offset());
        var columns=columns("id","Nº"); for(var field:def.fields()) columns.put(field.name(),field.label());
        h.table(rows,columns,"/catalogo?modulo="+module+"&id="); pager("/catalogo?modulo="+module,rows.size());
    }
    public void staff(Connection c) throws Exception {
        h.heading("Personal y permisos"); String id=p(r,"id");
        if(!id.isBlank()) {
            var row=id.equals("nuevo")?Map.<String,Object>of("activo",true):Db.one(c,"SELECT * FROM personal WHERE id=?",number(id));
            var roles=id.equals("nuevo")?List.<Map<String,Object>>of():Db.rows(c,"SELECT rol FROM personal_rol WHERE personal_id=?",number(id));
            h.startForm("/personal",false); if(!id.equals("nuevo")) h.hidden("id",id);
            h.input("nombre","Nombre completo","text",row.get("nombre"),160,true).input("email","Correo","email",row.get("email"),254,true).input("cargo","Cargo","text",row.get("cargo"),100,false).input("password","Contraseña (al editar, vacía conserva la actual)","password","",128,id.equals("nuevo")).check("activo","Activo",Db.yes(row,"activo"));
            for(String role:List.of("ADMINISTRADOR","TESORERO","ALMACENERO")) h.check(role,role,roles.stream().anyMatch(x->role.equals(x.get("rol"))));
            h.submit("Guardar personal"); return;
        }
        h.link("/personal?id=nuevo","Nueva cuenta");
        var rows=Db.rows(c,"SELECT p.id,p.nombre,p.email,p.cargo,p.activo,GROUP_CONCAT(r.rol ORDER BY r.rol SEPARATOR ', ') roles FROM personal p LEFT JOIN personal_rol r ON r.personal_id=p.id GROUP BY p.id ORDER BY p.id DESC LIMIT 30 OFFSET ?",offset());
        h.table(rows,columns("id","Nº","nombre","Nombre","email","Correo","cargo","Cargo","activo","Activo","roles","Roles"),"/personal?id="); pager("/personal",rows.size());
    }
    public void publications(Connection c) throws Exception {
        h.heading("Eventos y publicaciones"); String id=p(r,"id");
        if(!id.isBlank()) {
            var row=id.equals("nuevo")?Map.<String,Object>of("tipo","EVENTO"):Db.one(c,"SELECT * FROM publicacion WHERE id=?",number(id));
            h.startForm("/publicaciones",true); if(!id.equals("nuevo")) h.hidden("id",id).hidden("version",row.get("version"));
            h.choices("tipo","Tipo",row.get("tipo"),"EVENTO","AVISO").input("titulo","Título","text",row.get("titulo"),160,true).area("contenido","Contenido",row.get("contenido"),6000,true);
            for(String name:List.of("inicio","fin")) h.input(name,name.equals("inicio")?"Inicio (Lima)":"Fin (Lima)","datetime-local",Db.str(row,name).replace(' ','T'),0,false);
            h.input("lugar","Lugar","text",row.get("lugar"),250,false).check("publicado","Publicado",Db.yes(row,"publicado")).check("cancelado","Cancelado",Db.yes(row,"cancelado")).file("Imagen (opcional)",false,true).submit("Guardar publicación");
            if(!id.equals("nuevo")&&Db.str(row,"tipo").equals("EVENTO")) h.link("/participantes?evento="+id,"Participantes y asistencia"); return;
        }
        h.link("/publicaciones?id=nuevo","Nueva publicación");
        var rows=Db.rows(c,"SELECT id,titulo,tipo,inicio,lugar,publicado,cancelado FROM publicacion ORDER BY id DESC LIMIT 30 OFFSET ?",offset());
        h.table(rows,columns("id","Nº","titulo","Título","tipo","Tipo","inicio","Inicio","lugar","Lugar","publicado","Publicado","cancelado","Cancelado"),"/publicaciones?id="); pager("/publicaciones",rows.size());
    }
    public void participants(Connection c) throws Exception {
        long event=number(p(r,"evento")); var publication=Db.one(c,"SELECT * FROM publicacion WHERE id=?",event);
        h.heading("Participantes: "+Db.str(publication,"titulo")).note("La asistencia y las notas internas son privadas.");
        h.startForm("/participantes",false).hidden("accion","agregar").hidden("publicacion_id",event).options("congregante_id","Congregante",Db.rows(c,"SELECT id,CONCAT(nombres,' ',apellidos,' · ',COALESCE(dni,'')) nombre FROM congregante WHERE activo=1 ORDER BY apellidos"),"id","nombre",null,true).submit("Asignar participante");
        var rows=Db.rows(c,"SELECT e.*,CONCAT(c.nombres,' ',c.apellidos) nombre FROM participante_evento e JOIN congregante c ON c.id=e.congregante_id WHERE publicacion_id=? ORDER BY e.id LIMIT 30 OFFSET ?",event,offset());
        for(var row:rows) {
            h.raw("<details><summary>").text(row.get("nombre")).raw(" · ").text(row.get("asistencia")).raw("</summary>");
            h.startForm("/participantes",false).hidden("accion","actualizar").hidden("id",row.get("id")).hidden("version",row.get("version")).hidden("publicacion_id",event).choices("asistencia","Asistencia",row.get("asistencia"),"PENDIENTE","ASISTIO","NO_ASISTIO","JUSTIFICADA").check("activo","Participación activa",Db.yes(row,"activo")).area("retiro_motivo","Motivo de retiro (obligatorio al desactivar)",row.get("retiro_motivo"),1000,false).area("nota_interna","Nota interna",row.get("nota_interna"),1000,false).submit("Guardar asistencia").raw("</details>");
        } pager("/participantes?evento="+event,rows.size());
    }
}

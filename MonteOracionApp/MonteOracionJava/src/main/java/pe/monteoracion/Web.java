package pe.monteoracion;

import java.io.*;
import java.nio.file.Files;
import java.sql.SQLException;
import java.sql.Timestamp;
import java.util.*;
import jakarta.servlet.*;
import jakarta.servlet.annotation.*;
import jakarta.servlet.http.*;
import static pe.monteoracion.Rules.*;

@WebServlet(urlPatterns="/")
@MultipartConfig(maxFileSize=5*1024*1024,maxRequestSize=6*1024*1024,fileSizeThreshold=1024*1024)
public final class Web extends HttpServlet {
    private static final String DUMMY=Passwords.hash("NoEsUnaCuentaReal-2026");
    private static final Set<String> PUBLIC=Set.of("/","/login","/donar","/consulta","/privacidad","/archivo");
    private static String route(HttpServletRequest r) { return r.getServletPath().isEmpty()?"/":r.getServletPath(); }
    @Override protected void doGet(HttpServletRequest r,HttpServletResponse s) throws IOException,ServletException {
        String path=route(r);
        if(Set.of("/assets/app.css","/assets/app.js").contains(path)) {
            s.setContentType(path.endsWith(".css")?"text/css;charset=UTF-8":"text/javascript;charset=UTF-8");
            try(var input=getServletContext().getResourceAsStream(path)) { if(input==null) s.sendError(404); else input.transferTo(s.getOutputStream()); } return;
        }
        if(!PUBLIC.contains(path)&&Security.user(r)==null) { s.sendRedirect(r.getContextPath()+"/login"); return; }
        try {
            if(path.equals("/archivo")) { file(r,s); return; }
            if(path.equals("/reporte.pdf")) {
                byte[] pdf=Reports.create(r); s.setContentType("application/pdf"); s.setHeader("Content-Disposition","attachment; filename=monte-oracion.pdf"); s.getOutputStream().write(pdf); return;
            }
            var pages=new Pages(r); var operations=new OperationPages(r,pages);
            Db.read(c->{
                switch(path) {
                    case "/" -> pages.home(c);
                    case "/login" -> pages.login();
                    case "/password" -> pages.password();
                    case "/donar" -> pages.donate(c,false);
                    case "/consulta" -> pages.consultation(c,false);
                    case "/panel" -> pages.panel(c);
                    case "/catalogo" -> pages.catalog(c);
                    case "/personal" -> { Security.role(r,"ADMINISTRADOR"); pages.staff(c); }
                    case "/publicaciones" -> { Security.role(r,"ADMINISTRADOR"); pages.publications(c); }
                    case "/participantes" -> { Security.role(r,"ADMINISTRADOR"); pages.participants(c); }
                    case "/donaciones" -> operations.donations(c);
                    case "/donacion" -> operations.donation(c);
                    case "/recibir" -> pages.donate(c,true);
                    case "/almacen" -> { Security.role(r,"ADMINISTRADOR","ALMACENERO"); operations.stock(c); }
                    case "/lote" -> { Security.role(r,"ADMINISTRADOR","ALMACENERO"); operations.lot(c); }
                    case "/bienes" -> operations.stockRequests(c);
                    case "/bienes-nueva" -> operations.stockNew(c);
                    case "/bienes-detalle" -> operations.stockDetail(c);
                    case "/dinero" -> operations.moneyRequests(c);
                    case "/dinero-nueva" -> operations.moneyNew(c);
                    case "/dinero-detalle" -> operations.moneyDetail(c);
                    case "/saldos" -> { Security.role(r,"ADMINISTRADOR","TESORERO"); operations.balances(c); }
                    case "/cierres" -> { Security.role(r,"ADMINISTRADOR","TESORERO"); operations.closings(c); }
                    case "/auditoria" -> { Security.role(r,"ADMINISTRADOR"); operations.audit(c); }
                    case "/reportes" -> operations.reports();
                    case "/privacidad" -> pages.h.heading("Privacidad").note("La iglesia utiliza los datos para registrar aportes, distribuir bienes y gestionar actividades. La consulta personal requiere DNI y código privado. Los comprobantes, contactos y notas internas están restringidos al personal autorizado. Solicita a la administración la actualización de tus datos y las condiciones de conservación aplicables.");
                    default -> { s.setStatus(404); pages.h.heading("Página no encontrada"); }
                } return null;
            });
            render(r,s,pages);
        } catch(Exception e) { error(r,s,e); }
    }
    @Override protected void doPost(HttpServletRequest r,HttpServletResponse s) throws IOException,ServletException {
        String path=route(r);
        if(!Set.of("/login","/donar","/consulta").contains(path)&&Security.user(r)==null) { s.sendError(401); return; }
        try {
            var user=Security.user(r); long actor=user==null?0:user.id(); boolean admin=user!=null&&user.has("ADMINISTRADOR");
            String redirect="/panel";
            switch(path) {
                case "/login" -> { login(r); redirect="/panel"; }
                case "/logout" -> { r.getSession().invalidate(); redirect="/"; }
                case "/password" -> { password(r,actor); redirect="/panel"; }
                case "/donar" -> {
                    long id=Donations.register(r,null); var row=Db.read(c->Db.one(c,"SELECT codigo FROM donacion WHERE id=?",id));
                    var pages=new Pages(r); pages.h.heading("Donación enviada para revisión").note("Código de seguimiento: "+row.get("codigo")).note("Guarda este código para consultar al personal sobre el estado de tu aporte. El código privado para consultar tu historial lo entrega la iglesia.").link("/","Volver al inicio"); render(r,s,pages); return;
                }
                case "/consulta" -> { var pages=new Pages(r); Db.read(c->{pages.consultation(c,true); return null;}); render(r,s,pages); return; }
                case "/recibir" -> {
                    Security.role(r,p(r,"tipo").equals("DINERO")?new String[]{"ADMINISTRADOR","TESORERO"}:new String[]{"ADMINISTRADOR","ALMACENERO"});
                    redirect="/donacion?id="+Donations.register(r,actor);
                }
                case "/donacion" -> {
                    Security.role(r,"ADMINISTRADOR","TESORERO");
                    Db.read(c->{var d=OperationPages.visibleDonation(c,r,id(r)); require(admin||Db.str(d,"tipo").equals("DINERO"),"No tienes permiso para esta operación."); return null;});
                    Donations.decide(r,actor); redirect="/donacion?id="+id(r);
                }
                case "/catalogo" -> {
                    var def=Catalog.ALL.get(p(r,"modulo")); require(def!=null,"Módulo inválido."); Security.role(r,def.roles().toArray(String[]::new));
                    Catalog.save(r,actor); redirect="/catalogo?modulo="+p(r,"modulo")+(p(r,"modulo").equals("iglesia")?"&id=1":"");
                }
                case "/personal" -> { Security.role(r,"ADMINISTRADOR"); Catalog.staff(r,actor); redirect="/personal"; }
                case "/publicaciones" -> { Security.role(r,"ADMINISTRADOR"); Catalog.publication(r,actor); redirect="/publicaciones"; }
                case "/participantes" -> { Security.role(r,"ADMINISTRADOR"); Catalog.participant(r,actor); redirect="/participantes?evento="+number(p(r,"publicacion_id")); }
                case "/bienes" -> redirect="/bienes-detalle?id="+Inventory.request(r,actor);
                case "/bienes-decision" -> {
                    if(!p(r,"accion").equals("cancelar")) Security.role(r,"ADMINISTRADOR"); Inventory.decision(r,actor,admin); redirect="/bienes-detalle?id="+id(r);
                }
                case "/bienes-linea" -> {
                    if(p(r,"accion").equals("condonar")) Security.role(r,"ADMINISTRADOR"); else Security.role(r,"ADMINISTRADOR","ALMACENERO");
                    Inventory.line(r,actor); redirect="/bienes-detalle?id="+id(r);
                }
                case "/ajuste-almacen" -> { Security.role(r,"ADMINISTRADOR"); Inventory.adjust(r,actor); redirect="/lote?id="+id(r); }
                case "/dinero" -> redirect="/dinero-detalle?id="+Treasury.request(r,actor,admin);
                case "/dinero-accion" -> {
                    if(Set.of("aprobar","rechazar").contains(p(r,"accion"))) Security.role(r,"ADMINISTRADOR");
                    else if(!p(r,"accion").equals("cancelar")) Security.role(r,"ADMINISTRADOR","TESORERO");
                    Treasury.action(r,actor,admin); redirect="/dinero-detalle?id="+id(r);
                }
                case "/cierres" -> {
                    if(p(r,"accion").equals("revisar")) Security.role(r,"ADMINISTRADOR"); else Security.role(r,"ADMINISTRADOR","TESORERO");
                    Treasury.closing(r,actor); redirect="/cierres";
                }
                default -> { s.sendError(404); return; }
            }
            r.getSession().setAttribute("flash","Operación completada."); s.setStatus(303); s.setHeader("Location",r.getContextPath()+redirect);
        } catch(Exception e) { error(r,s,e); }
    }
    private void login(HttpServletRequest r) throws Exception {
        String email=Catalog.email(p(r,"email")), password=Objects.toString(r.getParameter("password"),"");
        var result=Db.tx(c->{
            var rows=Db.rows(c,"SELECT * FROM personal WHERE email=? FOR UPDATE",email);
            if(rows.isEmpty()) { Passwords.verify(password,DUMMY); return null; }
            var row=rows.get(0); boolean valid=Passwords.verify(password,Db.str(row,"password_hash"));
            boolean locked=row.get("bloqueado_hasta") instanceof Timestamp time&&time.toLocalDateTime().isAfter(now());
            if(locked||!Db.yes(row,"activo")) return null;
            if(!valid) {
                int failures=((Number)row.get("fallos")).intValue()+1;
                Db.exec(c,"UPDATE personal SET fallos=?,bloqueado_hasta=? WHERE id=?",failures>=5?0:failures,failures>=5?now().plusMinutes(15):null,row.get("id")); return null;
            }
            if(Db.rows(c,"SELECT rol FROM personal_rol WHERE personal_id=?",row.get("id")).isEmpty()) return null;
            Db.exec(c,"UPDATE personal SET fallos=0,bloqueado_hasta=NULL WHERE id=?",row.get("id"));
            Db.audit(c,Db.id(row,"id"),"Inicio de sesión","Personal #"+row.get("id"),""); return row;
        });
        require(result!=null,"No se pudo iniciar sesión. Revisa tus datos o consulta al administrador.");
        r.getSession().invalidate(); var session=r.getSession(true); session.setAttribute("uid",Db.id(result,"id")); session.setAttribute("stamp",result.get("password_hash"));
    }
    private void password(HttpServletRequest r,long actor) throws Exception {
        String current=Objects.toString(r.getParameter("actual"),""), next=Objects.toString(r.getParameter("nueva"),"");
        require(next.equals(r.getParameter("repetir")),"Las contraseñas no coinciden."); String hash=Passwords.hash(next);
        Db.tx(c->{var row=Db.one(c,"SELECT password_hash FROM personal WHERE id=? FOR UPDATE",actor); require(Passwords.verify(current,Db.str(row,"password_hash")),"La contraseña actual no coincide.");
            Db.exec(c,"UPDATE personal SET password_hash=? WHERE id=?",hash,actor); Db.audit(c,actor,"Cambio de contraseña","Personal #"+actor,""); return null;});
        r.getSession().setAttribute("stamp",hash);
    }
    private void file(HttpServletRequest r,HttpServletResponse s) throws Exception {
        String type=p(r,"tipo"); long id=id(r);
        Map<String,Object> row=Db.read(c->{
            return switch(type) {
                case "qr" -> Db.one(c,"SELECT qr_clave clave,qr_tipo tipo FROM medio_pago WHERE id=? AND activo=1",id);
                case "evento" -> {
                    String condition=Security.user(r)!=null&&Security.user(r).has("ADMINISTRADOR")?"1=1":"publicado=1";
                    yield Db.one(c,"SELECT imagen_clave clave,imagen_tipo tipo FROM publicacion WHERE id=? AND "+condition,id);
                }
                case "donacion" -> {
                    Security.role(r,"ADMINISTRADOR","TESORERO","ALMACENERO"); var d=OperationPages.visibleDonation(c,r,id); yield Map.of("clave",Db.str(d,"archivo_clave"),"tipo",Db.str(d,"archivo_tipo"));
                }
                case "dinero", "rendicion" -> {
                    Security.role(r,"ADMINISTRADOR","TESORERO"); yield Db.one(c,type.equals("dinero")?"SELECT archivo_clave clave,archivo_tipo tipo FROM solicitud_dinero WHERE id=?":"SELECT rendicion_clave clave,rendicion_tipo tipo FROM solicitud_dinero WHERE id=?",id);
                }
                case "cierre" -> { Security.role(r,"ADMINISTRADOR","TESORERO"); yield Db.one(c,"SELECT archivo_clave clave,archivo_tipo tipo FROM cierre_caja WHERE id=?",id); }
                case "almacen" -> { Security.role(r,"ADMINISTRADOR","ALMACENERO"); yield Db.one(c,"SELECT archivo_clave clave,archivo_tipo tipo FROM movimiento_almacen WHERE id=?",id); }
                default -> throw new IllegalArgumentException("Archivo no encontrado.");
            };
        });
        String key=Db.str(row,"clave"); require(!key.isBlank(),"Archivo no encontrado.");
        s.setContentType(Db.str(row,"tipo")); s.setHeader("Content-Disposition","inline; filename=\""+key+"\"");
        try(var input=Files.newInputStream(FilesStore.path(key))) { input.transferTo(s.getOutputStream()); }
    }
    private void render(HttpServletRequest r,HttpServletResponse s,Pages pages) throws IOException,ServletException {
        s.setContentType("text/html;charset=UTF-8"); r.setAttribute("title","Monte de Oración"); r.setAttribute("content",pages.h.toString());
        r.getRequestDispatcher("/WEB-INF/layout.jsp").forward(r,s);
    }
    private void error(HttpServletRequest r,HttpServletResponse s,Exception e) throws IOException,ServletException {
        if(s.isCommitted()) { getServletContext().log("Respuesta interrumpida",e); return; }
        int status=500; String message="No se pudo completar la operación. Revisa la conexión con la base de datos o consulta al administrador.";
        if(e instanceof Security.Forbidden) { status=403; message="Tu cuenta no tiene permiso para esta operación."; }
        else if(e instanceof IllegalArgumentException) { status=400; message=e instanceof NumberFormatException?"Actualiza el formulario y revisa los valores.":e.getMessage(); }
        else if(e instanceof IllegalStateException) { status=400; message="El archivo o formulario supera el tamaño permitido."; }
        else if(e instanceof SQLException sql && (sql.getErrorCode()==1062||sql.getSQLState()!=null&&sql.getSQLState().startsWith("23"))) { status=409; message="El registro está duplicado o la operación entra en conflicto con los datos existentes. Actualiza la página."; }
        else if(e instanceof SQLException sql && (sql.getErrorCode()==1213||sql.getErrorCode()==1205)) { status=409; message="Otra persona está operando sobre estos registros. Actualiza y vuelve a intentar."; }
        if(status==500) getServletContext().log("Error de aplicación",e);
        s.setStatus(status); var pages=new Pages(r); pages.h.heading("No se pudo completar la operación").raw("<p class=message role=alert>").text(message).raw("</p>").note("Puedes volver a la página anterior para corregir el formulario.").link("/","Ir al inicio"); render(r,s,pages);
    }
}

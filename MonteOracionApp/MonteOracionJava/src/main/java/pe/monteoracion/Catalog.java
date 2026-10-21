package pe.monteoracion;

import java.sql.*;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;

/** Los nombres de tablas/columnas solo proceden de este catálogo, nunca del formulario. */
public final class Catalog {
    public record Field(String name,String label,String type,int max,boolean required) { }
    public record Definition(String table,String title,List<Field> fields,Set<String> roles) { }
    private static Field f(String n,String l,String t,int max,boolean req) { return new Field(n,l,t,max,req); }
    public static final Map<String,Definition> ALL = Map.of(
        "congregantes",new Definition("congregante","Congregantes",List.of(f("dni","DNI","text",8,false),f("nombres","Nombres","text",100,true),f("apellidos","Apellidos","text",100,true),f("telefono","Teléfono","text",30,false),f("activo","Activo","checkbox",0,false)),Set.of("ADMINISTRADOR")),
        "productos",new Definition("producto","Productos",List.of(f("nombre","Nombre","text",160,true),f("unidad","Unidad base","text",40,true),f("categoria","Categoría","text",100,true),f("requiere_vencimiento","Requiere vencimiento","checkbox",0,false),f("activo","Activo","checkbox",0,false)),Set.of("ADMINISTRADOR","ALMACENERO")),
        "beneficiarios",new Definition("beneficiario","Beneficiarios",List.of(f("nombre","Nombre","text",160,true),f("direccion","Dirección","text",250,true),f("contacto","Responsable / contacto","text",160,true),f("telefono","Teléfono","text",30,false),f("activo","Activo","checkbox",0,false)),Set.of("ADMINISTRADOR","ALMACENERO")),
        "fondos",new Definition("fondo","Fondos",List.of(f("nombre","Nombre","text",120,true),f("finalidad","Finalidad","textarea",500,true),f("activo","Activo","checkbox",0,false)),Set.of("ADMINISTRADOR")),
        "medios",new Definition("medio_pago","Medios de pago",List.of(f("nombre","Nombre","text",100,true),f("instrucciones","Cuenta, titular e instrucciones","textarea",500,true),f("activo","Activo","checkbox",0,false)),Set.of("ADMINISTRADOR")),
        "iglesia",new Definition("iglesia","Datos de la iglesia",List.of(f("nombre","Nombre","text",150,true),f("informacion","Información","textarea",3000,true),f("direccion","Dirección","text",300,false),f("contacto","Contacto","text",100,false)),Set.of("ADMINISTRADOR"))
    );
    public static String email(String value) {
        String v=text(value,254,true).toLowerCase(Locale.ROOT);
        require(v.matches("[^\\s@]+@[^\\s@]+\\.[^\\s@]+"),"Correo inválido."); return v;
    }
    public static void save(HttpServletRequest r, long actor) throws Exception {
        Definition def=ALL.get(p(r,"modulo")); require(def!=null,"Módulo inválido.");
        var names=new ArrayList<String>(); var values=new ArrayList<Object>();
        for (var f:def.fields()) {
            names.add(f.name());
            if(f.type().equals("checkbox")) values.add("on".equals(p(r,f.name())));
            else {
                String value=text(p(r,f.name()),f.max(),f.required());
                if(f.name().equals("dni")) { require(value.isBlank()||value.matches("[0-9]{8}"),"El DNI debe tener ocho dígitos."); values.add(value.isBlank()?null:value); }
                else values.add(value);
            }
        }
        long id=p(r,"id").isBlank()?0:id(r);
        if(def.table().equals("congregante")&&id==0) { names.add("consulta_codigo"); values.add(UUID.randomUUID().toString()); }
        if(def.table().equals("iglesia")) require(id==1,"Configuración inválida.");
        FilesStore.Stored qr=null;
        if(def.table().equals("medio_pago") && r.getPart("archivo")!=null && r.getPart("archivo").getSize()>0) {
            qr=FilesStore.save(r.getPart("archivo"),true);
            names.add("qr_clave"); values.add(qr.key()); names.add("qr_tipo"); values.add(qr.type());
        }
        try {
            Db.tx(c -> {
                if(id==0) Db.insert(c,"INSERT INTO "+def.table()+"("+String.join(",",names)+") VALUES("+String.join(",",Collections.nCopies(values.size(),"?"))+")",values.toArray());
                else {
                    Db.one(c,"SELECT id FROM "+def.table()+" WHERE id=? FOR UPDATE",id);
                    var args=new ArrayList<>(values); args.add(id);
                    Db.exec(c,"UPDATE "+def.table()+" SET "+String.join(",",names.stream().map(n->n+"=?").toList())+" WHERE id=?",args.toArray());
                }
                Db.audit(c,actor,"Guardar "+def.title(),"#"+id,""); return null;
            });
        } catch(Exception e) { FilesStore.discard(qr); throw e; }
    }
    public static void staff(HttpServletRequest r,long actor) throws Exception {
        long id=p(r,"id").isBlank()?0:id(r);
        String name=text(p(r,"nombre"),160,true), email=email(p(r,"email")), position=text(p(r,"cargo"),100,false);
        boolean active="on".equals(p(r,"activo"));
        var roles=new TreeSet<String>();
        for(String role:List.of("ADMINISTRADOR","TESORERO","ALMACENERO")) if("on".equals(p(r,role))) roles.add(role);
        require(!roles.isEmpty(),"Asigna al menos un rol.");
        String password=Objects.toString(r.getParameter("password"),"");
        String hash=password.isEmpty()?null:Passwords.hash(password);
        require(id!=0||hash!=null,"Indica contraseña inicial.");
        Db.tx(c -> {
            Db.one(c,"SELECT id FROM iglesia WHERE id=1 FOR UPDATE");
            if(id==0) {
                long created=Db.insert(c,"INSERT INTO personal(nombre,email,cargo,password_hash,activo) VALUES(?,?,?,?,?)",name,email,position,hash,active);
                for(String role:roles) Db.exec(c,"INSERT INTO personal_rol(personal_id,rol) VALUES(?,?)",created,role);
            } else {
                Db.one(c,"SELECT id FROM personal WHERE id=? FOR UPDATE",id);
                var admins=Db.rows(c,"SELECT p.id FROM personal p JOIN personal_rol r ON r.personal_id=p.id WHERE p.activo=1 AND r.rol='ADMINISTRADOR'");
                if(admins.size()==1 && Db.id(admins.get(0),"id")==id) require(active&&roles.contains("ADMINISTRADOR"),"Debe quedar un administrador activo.");
                Db.exec(c,"UPDATE personal SET nombre=?,email=?,cargo=?,activo=? WHERE id=?",name,email,position,active,id);
                if(hash!=null) Db.exec(c,"UPDATE personal SET password_hash=?,fallos=0,bloqueado_hasta=NULL WHERE id=?",hash,id);
                Db.exec(c,"DELETE FROM personal_rol WHERE personal_id=?",id);
                for(String role:roles) Db.exec(c,"INSERT INTO personal_rol(personal_id,rol) VALUES(?,?)",id,role);
            }
            Db.audit(c,actor,"Personal y permisos","#"+id,"Roles: "+String.join(", ",roles)); return null;
        });
    }
    public static void publication(HttpServletRequest r,long actor) throws Exception {
        long id=p(r,"id").isBlank()?0:id(r);
        String type=choice(p(r,"tipo"),"EVENTO","AVISO"), title=text(p(r,"titulo"),160,true), body=text(p(r,"contenido"),6000,true), place=text(p(r,"lugar"),250,false);
        var start=optionalTime(p(r,"inicio")); var end=optionalTime(p(r,"fin"));
        require(!type.equals("EVENTO")||start!=null,"Indica el horario del evento.");
        require(end==null||start!=null&&!end.isBefore(start),"Revisa el horario final.");
        boolean published="on".equals(p(r,"publicado")), cancelled="on".equals(p(r,"cancelado"));
        FilesStore.Stored image=r.getPart("archivo")==null||r.getPart("archivo").getSize()==0?null:FilesStore.save(r.getPart("archivo"),true);
        try {
            Db.tx(c->{
                long key=id;
                if(id==0) key=Db.insert(c,"INSERT INTO publicacion(tipo,titulo,contenido,lugar,inicio,fin,publicado,cancelado) VALUES(?,?,?,?,?,?,?,?)",type,title,body,place,start,end,published,cancelled);
                else {
                    var row=Db.one(c,"SELECT * FROM publicacion WHERE id=? FOR UPDATE",id);
                    require(Db.id(row,"version")==Long.parseLong(p(r,"version")),"El evento cambió. Actualiza la página.");
                    Db.exec(c,"UPDATE publicacion SET tipo=?,titulo=?,contenido=?,lugar=?,inicio=?,fin=?,publicado=?,cancelado=?,version=version+1 WHERE id=?",type,title,body,place,start,end,published,cancelled,id);
                }
                if(image!=null) Db.exec(c,"UPDATE publicacion SET imagen_clave=?,imagen_tipo=? WHERE id=?",image.key(),image.type(),key);
                Db.audit(c,actor,"Publicación guardada","#"+key,title); return null;
            });
        } catch(Exception e) { FilesStore.discard(image); throw e; }
    }
    public static void participant(HttpServletRequest r,long actor) throws Exception {
        Db.tx(c->{
            if(p(r,"accion").equals("agregar")) {
                long event=number(p(r,"publicacion_id")), member=number(p(r,"congregante_id"));
                Db.one(c,"SELECT id FROM publicacion WHERE id=? AND tipo='EVENTO'",event);
                Db.one(c,"SELECT id FROM congregante WHERE id=? AND activo=1",member);
                Db.exec(c,"INSERT INTO participante_evento(publicacion_id,congregante_id,registrado_por) VALUES(?,?,?)",event,member,actor);
            } else {
                var row=Db.one(c,"SELECT * FROM participante_evento WHERE id=? FOR UPDATE",id(r));
                require(Db.id(row,"version")==Long.parseLong(p(r,"version")),"La participación cambió. Actualiza.");
                String attendance=choice(p(r,"asistencia"),"PENDIENTE","ASISTIO","NO_ASISTIO","JUSTIFICADA");
                boolean active="on".equals(p(r,"activo")); String reason=text(p(r,"retiro_motivo"),1000,!active);
                Db.exec(c,"UPDATE participante_evento SET asistencia=?,activo=?,retiro_motivo=?,nota_interna=?,registrado_por=?,version=version+1 WHERE id=?",attendance,active,reason,text(p(r,"nota_interna"),1000,false),actor,id(r));
            }
            Db.audit(c,actor,"Participación / asistencia",p(r,"publicacion_id"),""); return null;
        });
    }
}

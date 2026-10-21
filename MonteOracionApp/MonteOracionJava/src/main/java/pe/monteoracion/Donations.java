package pe.monteoracion;

import java.math.BigDecimal;
import java.sql.*;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;

public final class Donations {
    public static long register(HttpServletRequest r,Long actor) throws Exception {
        boolean online=actor==null;
        String kind=online?"DINERO":choice(p(r,"tipo"),"DINERO","BIENES");
        if(!online) require("on".equals(p(r,"confirmado")),"Confirma la recepción física.");
        String donor=choice(p(r,"donante_tipo"),"CONGREGANTE","EXTERNO","ANONIMO");
        var date=date(p(r,"fecha")); require(!date.isAfter(today())&&date.getYear()>=2000,"Fecha inválida.");
        long fund=number(p(r,"fondo_id")); BigDecimal value=kind.equals("DINERO")?amount(p(r,"importe")):null;
        String external=donor.equals("EXTERNO")?text(p(r,"externo_nombre"),180,true):null;
        String contact=donor.equals("EXTERNO")?text(p(r,"externo_contacto"),150,false):null;
        String operation=online?text(p(r,"operacion"),80,true).toUpperCase(Locale.ROOT):null;
        var part=r.getPart("archivo");
        FilesStore.Stored file=online||kind.equals("BIENES")?FilesStore.save(part,true):part!=null&&part.getSize()>0?FilesStore.save(part,false):null;
        try {
            return Db.tx(c->{
                Long member=null, payment=null; String method="Efectivo";
                Db.one(c,"SELECT id FROM fondo WHERE id=? AND activo=1",fund);
                if(donor.equals("CONGREGANTE")) {
                    String dni=p(r,"dni"); require(dni.matches("[0-9]{8}"),"Indica ocho dígitos de DNI.");
                    member=Db.id(Db.one(c,"SELECT id FROM congregante WHERE dni=? AND activo=1",dni),"id");
                }
                if(online) { payment=number(p(r,"medio_pago_id")); method=Db.str(Db.one(c,"SELECT nombre FROM medio_pago WHERE id=? AND activo=1",payment),"nombre"); }
                if(!online&&kind.equals("DINERO")) { Treasury.lock(c,"EFECTIVO"); Treasury.openDay(c,"EFECTIVO",today()); }
                long id=Db.insert(c,"INSERT INTO donacion(codigo,tipo,estado,donante_tipo,congregante_id,externo_nombre,externo_contacto,importe,fondo_id,medio_pago_id,medio_nombre,operacion,fecha,archivo_clave,archivo_tipo,registrado_por) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",UUID.randomUUID().toString(),kind,online?"PENDIENTE":"APROBADA",donor,member,external,contact,value,fund,payment,method,operation,date,file==null?null:file.key(),file==null?null:file.type(),actor);
                if(!online) {
                    if(kind.equals("DINERO")) Treasury.movement(c,fund,"EFECTIVO",id,null,"DONACION",value,actor,"Donación presencial");
                    else {
                        String[] products=r.getParameterValues("producto_id"), quantities=r.getParameterValues("cantidad"), locations=r.getParameterValues("ubicacion"), expiries=r.getParameterValues("vencimiento");
                        require(products!=null&&products.length>0&&products.length<=50&&quantities!=null&&locations!=null&&expiries!=null&&products.length==quantities.length&&products.length==locations.length&&products.length==expiries.length,"Registra entre 1 y 50 bienes.");
                        int count=0;
                        for(int i=0;i<products.length;i++) {
                            if(products[i].isBlank()&&quantities[i].isBlank()) continue;
                            var product=Db.one(c,"SELECT * FROM producto WHERE id=? AND activo=1",number(products[i]));
                            var quantity=amount(quantities[i]); var expiry=optionalDate(expiries[i]); Inventory.expiry(product,expiry);
                            String location=text(locations[i],160,true);
                            long item=Db.insert(c,"INSERT INTO donacion_bien(donacion_id,producto_id,descripcion,cantidad,unidad,vencimiento,ubicacion) VALUES(?,?,?,?,?,?,?)",id,product.get("id"),product.get("nombre"),quantity,product.get("unidad"),expiry,location);
                            long lot=Db.insert(c,"INSERT INTO lote(producto_id,donacion_bien_id,ubicacion,origen,vencimiento,existencia) VALUES(?,?,?,?,?,?)",product.get("id"),item,location,"Donación #"+id,expiry,quantity);
                            Inventory.movement(c,lot,null,"DONACION",quantity,BigDecimal.ZERO,actor,"Recepción de donación",null,null,null); count++;
                        }
                        require(count>0,"Registra al menos un bien.");
                    }
                    Db.exec(c,"INSERT INTO donacion_decision(donacion_id,estado,actor_id,motivo) VALUES(?,'APROBADA',?,'Recepción presencial confirmada')",id,actor);
                    Db.audit(c,actor,"Donación recibida","#"+id,kind);
                }
                return id;
            });
        } catch(Exception e) { FilesStore.discard(file); throw e; }
    }
    public static void decide(HttpServletRequest r,long actor) throws Exception {
        long id=id(r); String target=choice(p(r,"estado"),"APROBADA","RECHAZADA","ANULADA");
        String reason=text(p(r,"motivo"),1000,!target.equals("APROBADA"));
        if(target.equals("APROBADA")) require("on".equals(p(r,"confirmado")),"Confirma que el dinero fue recibido.");
        Db.tx(c->{
            var d=Db.one(c,"SELECT * FROM donacion WHERE id=? FOR UPDATE",id);
            require(Db.id(d,"version")==Long.parseLong(p(r,"version")),"La donación cambió. Actualiza.");
            String old=Db.str(d,"estado"); boolean cancel=target.equals("ANULADA");
            require(cancel?old.equals("APROBADA"):old.equals("PENDIENTE"),"Transición de estado inválida.");
            if(Db.str(d,"tipo").equals("DINERO")&&(target.equals("APROBADA")||cancel)) {
                String channel=d.get("medio_pago_id")==null?"EFECTIVO":"DIGITAL";
                Treasury.lock(c,channel); BigDecimal value=decimal(d,"importe");
                if(cancel) require(Treasury.available(c,Db.id(d,"fondo_id"),channel).compareTo(value)>=0,"La anulación afectaría recursos comprometidos.");
                Treasury.movement(c,Db.id(d,"fondo_id"),channel,id,null,cancel?"ANULACION_DONACION":"DONACION",cancel?value.negate():value,actor,reason.isBlank()?"Donación verificada":reason);
            } else if(cancel) {
                var lots=Db.rows(c,"SELECT l.* FROM lote l JOIN donacion_bien b ON b.id=l.donacion_bien_id WHERE b.donacion_id=? ORDER BY l.id FOR UPDATE",id);
                for(var lot:lots) {
                    long key=Db.id(lot,"id");
                    require(decimal(lot,"reservado").signum()==0&&Db.rows(c,"SELECT id FROM movimiento_almacen WHERE lote_id=? AND tipo<>'DONACION' LIMIT 1",key).isEmpty(),"El bien tiene movimientos. Registra una corrección de inventario.");
                    Inventory.movement(c,key,null,"AJUSTE_NEGATIVO",decimal(lot,"existencia").negate(),BigDecimal.ZERO,actor,reason,null,null,null);
                    Db.exec(c,"UPDATE lote SET existencia=0,version=version+1 WHERE id=?",key);
                }
            }
            if(!p(r,"congregante_id").isBlank()) {
                require(old.equals("PENDIENTE")&&d.get("congregante_id")==null,"No se puede vincular esta donación.");
                long member=number(p(r,"congregante_id")); Db.one(c,"SELECT id FROM congregante WHERE id=? AND activo=1",member);
                Db.exec(c,"UPDATE donacion SET donante_tipo='CONGREGANTE',congregante_id=?,externo_nombre=NULL,externo_contacto=NULL WHERE id=?",member,id);
            }
            Db.exec(c,"UPDATE donacion SET estado=?,version=version+1 WHERE id=?",target,id);
            Db.exec(c,"INSERT INTO donacion_decision(donacion_id,estado,actor_id,motivo) VALUES(?,?,?,?)",id,target,actor,reason);
            Db.audit(c,actor,"Decisión de donación","#"+id,target+": "+reason); return null;
        });
    }
}

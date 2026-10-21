package pe.monteoracion;

import java.math.BigDecimal;
import java.sql.*;
import java.time.LocalDate;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;

public final class Inventory {
    public static void expiry(Map<String,Object> product,LocalDate date) {
        require(!Db.yes(product,"requiere_vencimiento")||date!=null,"El producto requiere vencimiento.");
        require(date==null||!date.isBefore(today()),"El vencimiento no puede estar en el pasado.");
    }
    private static void usable(Map<String,Object> lot) {
        require(Db.yes(lot,"activo"),"El producto está inactivo.");
        require(lot.get("vencimiento")==null||!date(lot.get("vencimiento").toString()).isBefore(today()),"El lote está vencido.");
    }
    public static void movement(Connection c,long lot,Long line,String type,BigDecimal quantity,BigDecimal reserve,long actor,String reason,String command,String receiver,FilesStore.Stored file) throws SQLException {
        Db.exec(c,"INSERT INTO movimiento_almacen(lote_id,linea_id,tipo,cantidad,reserva_cambio,actor_id,motivo,comando,receptor,archivo_clave,archivo_tipo,fecha) VALUES(?,?,?,?,?,?,?,?,?,?,?,?)",lot,line,type,quantity,reserve,actor,reason,command,receiver,file==null?null:file.key(),file==null?null:file.type(),today());
    }
    private static Map<String,Object> lot(Connection c,long id) throws SQLException {
        return Db.one(c,"SELECT l.*,p.activo,p.requiere_vencimiento FROM lote l JOIN producto p ON p.id=l.producto_id WHERE l.id=? FOR UPDATE",id);
    }
    public static long request(HttpServletRequest r,long actor) throws Exception {
        String kind=choice(p(r,"tipo"),"ENTREGA","USO_INTERNO","PRESTAMO"), priority=choice(p(r,"prioridad"),"NORMAL","ALTA","URGENTE"), reason=text(p(r,"motivo"),1000,true);
        var needed=date(p(r,"necesaria")); var due=optionalDate(p(r,"devolucion"));
        require(!needed.isBefore(today())&&(!kind.equals("PRESTAMO")||due!=null&&!due.isBefore(needed)),"Revisa la fecha necesaria y el límite de devolución.");
        long beneficiary=number(p(r,"beneficiario_id"));
        String[] lots=r.getParameterValues("lote_id"), quantities=r.getParameterValues("cantidad");
        require(lots!=null&&quantities!=null&&lots.length==quantities.length&&lots.length>0&&lots.length<=50,"Registra entre 1 y 50 lotes.");
        var lines=new TreeMap<Long,BigDecimal>();
        for(int i=0;i<lots.length;i++) { if(lots[i].isBlank()&&quantities[i].isBlank()) continue; long key=number(lots[i]); require(!lines.containsKey(key),"No repitas el lote."); lines.put(key,amount(quantities[i])); }
        require(!lines.isEmpty(),"Selecciona al menos un lote.");
        return Db.tx(c->{
            var b=Db.one(c,"SELECT * FROM beneficiario WHERE id=? AND activo=1",beneficiary);
            long id=Db.insert(c,"INSERT INTO solicitud_bienes(beneficiario_id,beneficiario_nombre,beneficiario_direccion,beneficiario_contacto,motivo,prioridad,tipo,necesaria,devolucion,solicitante_id) VALUES(?,?,?,?,?,?,?,?,?,?)",beneficiary,b.get("nombre"),b.get("direccion"),b.get("contacto"),reason,priority,kind,needed,kind.equals("PRESTAMO")?due:null,actor);
            for(var line:lines.entrySet()) {
                var l=lot(c,line.getKey()); usable(l);
                require(decimal(l,"existencia").subtract(decimal(l,"reservado")).compareTo(line.getValue())>=0,"No hay existencia disponible para la cantidad solicitada.");
                Db.exec(c,"INSERT INTO solicitud_bien(solicitud_id,lote_id,cantidad) VALUES(?,?,?)",id,line.getKey(),line.getValue());
            }
            Db.audit(c,actor,"Solicitud de bienes","#"+id,reason); return id;
        });
    }
    public static void decision(HttpServletRequest r,long actor,boolean admin) throws Exception {
        long id=id(r); String action=choice(p(r,"accion"),"aprobar","rechazar","cancelar");
        String reason=text(p(r,"motivo"),1000,!action.equals("aprobar"));
        Db.tx(c->{
            var request=Db.one(c,"SELECT * FROM solicitud_bienes WHERE id=? FOR UPDATE",id);
            require(Db.id(request,"version")==Long.parseLong(p(r,"version")),"La solicitud cambió. Actualiza.");
            String status=Db.str(request,"estado");
            if(action.equals("cancelar")) require(Set.of("PENDIENTE","APROBADA","PARCIAL").contains(status)&&(admin||status.equals("PENDIENTE")&&Db.id(request,"solicitante_id")==actor),"No puedes cancelar esta solicitud.");
            else { require(status.equals("PENDIENTE"),"La solicitud ya fue atendida."); other(Db.id(request,"solicitante_id"),actor); }
            if(action.equals("aprobar")) Db.one(c,"SELECT id FROM beneficiario WHERE id=? AND activo=1",request.get("beneficiario_id"));
            var lines=Db.rows(c,"SELECT * FROM solicitud_bien WHERE solicitud_id=? ORDER BY lote_id FOR UPDATE",id);
            for(var line:lines) {
                long lotId=Db.id(line,"lote_id");
                var lot=lot(c,lotId);
                if(action.equals("aprobar")) {
                    usable(lot); var quantity=decimal(line,"cantidad");
                    require(decimal(lot,"existencia").subtract(decimal(lot,"reservado")).compareTo(quantity)>=0,"Existencia insuficiente. Hay otras reservas.");
                    Db.exec(c,"UPDATE lote SET reservado=reservado+?,version=version+1 WHERE id=?",quantity,lotId);
                    movement(c,lotId,Db.id(line,"id"),"RESERVA",BigDecimal.ZERO,quantity,actor,"Reserva #"+id,null,null,null);
                } else if(action.equals("cancelar")&&!status.equals("PENDIENTE")) {
                    var remaining=decimal(line,"cantidad").subtract(decimal(line,"entregado"));
                    Db.exec(c,"UPDATE lote SET reservado=reservado-?,version=version+1 WHERE id=?",remaining,lotId);
                    movement(c,lotId,Db.id(line,"id"),"LIBERACION",BigDecimal.ZERO,remaining.negate(),actor,reason,null,null,null);
                }
            }
            String target=action.equals("aprobar")?"APROBADA":action.equals("rechazar")?"RECHAZADA":"CANCELADA";
            Db.exec(c,"UPDATE solicitud_bienes SET estado=?,aprobador_id=COALESCE(?,aprobador_id),decision_motivo=?,version=version+1 WHERE id=?",target,action.equals("cancelar")?null:actor,reason,id);
            Db.audit(c,actor,"Bienes: "+action,"#"+id,reason); return null;
        });
    }
    public static void line(HttpServletRequest r,long actor) throws Exception {
        long lineId=number(p(r,"linea_id")); String action=choice(p(r,"accion"),"entregar","devolver","condonar");
        BigDecimal quantity=amount(p(r,"cantidad")); String command=command(p(r,"comando"));
        String reason=text(p(r,"motivo"),1000,!action.equals("entregar")), receiver=text(p(r,"receptor"),160,action.equals("entregar"));
        FilesStore.Stored file=action.equals("entregar")?FilesStore.save(r.getPart("archivo"),false):null;
        try {
            Db.tx(c->{
                long requestId=Db.id(Db.one(c,"SELECT solicitud_id FROM solicitud_bien WHERE id=?",lineId),"solicitud_id");
                var request=Db.one(c,"SELECT * FROM solicitud_bienes WHERE id=? FOR UPDATE",requestId);
                var line=Db.one(c,"SELECT * FROM solicitud_bien WHERE id=? FOR UPDATE",lineId); long lotId=Db.id(line,"lote_id"); var lot=lot(c,lotId);
                require(Db.rows(c,"SELECT id FROM movimiento_almacen WHERE comando=?",command).isEmpty(),"Esta operación ya fue registrada.");
                switch(action) {
                    case "entregar" -> {
                        require(Set.of("APROBADA","PARCIAL").contains(Db.str(request,"estado")),"La solicitud no está autorizada para entregar."); usable(lot);
                        require(decimal(line,"cantidad").subtract(decimal(line,"entregado")).compareTo(quantity)>=0&&decimal(lot,"reservado").compareTo(quantity)>=0&&decimal(lot,"existencia").compareTo(quantity)>=0,"La cantidad supera lo pendiente o la reserva.");
                        Db.exec(c,"UPDATE lote SET existencia=existencia-?,reservado=reservado-?,version=version+1 WHERE id=?",quantity,quantity,lotId);
                        Db.exec(c,"UPDATE solicitud_bien SET entregado=entregado+? WHERE id=?",quantity,lineId);
                        boolean complete=Db.rows(c,"SELECT id FROM solicitud_bien WHERE solicitud_id=? AND entregado<cantidad",requestId).isEmpty();
                        Db.exec(c,"UPDATE solicitud_bienes SET estado=?,version=version+1 WHERE id=?",complete?"ENTREGADA":"PARCIAL",requestId);
                        movement(c,lotId,lineId,"ENTREGA",quantity.negate(),quantity.negate(),actor,"Entrega #"+requestId,command,receiver,file);
                    }
                    case "devolver", "condonar" -> {
                        require(Db.str(request,"tipo").equals("PRESTAMO"),"No es un préstamo.");
                        require(decimal(line,"entregado").subtract(decimal(line,"devuelto")).subtract(decimal(line,"condonado")).compareTo(quantity)>=0,"La cantidad supera lo pendiente de reposición.");
                        if(action.equals("condonar")) {
                            other(Db.id(request,"solicitante_id"),actor);
                            Db.exec(c,"UPDATE solicitud_bien SET condonado=condonado+? WHERE id=?",quantity,lineId);
                            movement(c,lotId,lineId,"CONDONACION",BigDecimal.ZERO,BigDecimal.ZERO,actor,"Condonadas "+quantity+": "+reason,command,null,null);
                        } else {
                            var expiry=optionalDate(p(r,"vencimiento")); expiry(lot,expiry); String location=text(p(r,"ubicacion"),160,true);
                            long newLot=Db.insert(c,"INSERT INTO lote(producto_id,ubicacion,origen,vencimiento,existencia) VALUES(?,?,?,?,?)",lot.get("producto_id"),location,"Reposición préstamo #"+requestId,expiry,quantity);
                            Db.exec(c,"UPDATE solicitud_bien SET devuelto=devuelto+? WHERE id=?",quantity,lineId);
                            movement(c,newLot,lineId,"REPOSICION",quantity,BigDecimal.ZERO,actor,reason,command,null,null);
                        }
                        Db.exec(c,"UPDATE solicitud_bienes SET version=version+1 WHERE id=?",requestId);
                    }
                    default -> throw new IllegalArgumentException("Acción inválida");
                }
                Db.audit(c,actor,"Bienes: "+action,"#"+requestId,quantity+"; "+reason); return null;
            });
        } catch(Exception e) { FilesStore.discard(file); throw e; }
    }
    public static void adjust(HttpServletRequest r,long actor) throws Exception {
        long id=id(r); String type=choice(p(r,"tipo"),"AJUSTE_POSITIVO","AJUSTE_NEGATIVO","MERMA");
        var quantity=amount(p(r,"cantidad")); var change=type.equals("AJUSTE_POSITIVO")?quantity:quantity.negate();
        String reason=text(p(r,"motivo"),1000,true), command=command(p(r,"comando"));
        Db.tx(c->{
            var lot=lot(c,id); BigDecimal next=decimal(lot,"existencia").add(change);
            require(next.compareTo(decimal(lot,"reservado"))>=0&&next.compareTo(new BigDecimal("9999999999.99"))<=0,"El ajuste afectaría reservas o supera la capacidad del lote.");
            Db.exec(c,"UPDATE lote SET existencia=?,version=version+1 WHERE id=?",next,id);
            movement(c,id,null,type,change,BigDecimal.ZERO,actor,reason,command,null,null);
            Db.audit(c,actor,"Ajuste de inventario","Lote #"+id,reason); return null;
        });
    }
}

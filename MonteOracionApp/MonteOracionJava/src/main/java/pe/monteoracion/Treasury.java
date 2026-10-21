package pe.monteoracion;

import java.math.BigDecimal;
import java.sql.*;
import java.time.LocalDate;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;
import static pe.monteoracion.Rules.*;

public final class Treasury {
    public static void lock(Connection c,String channel) throws SQLException { Db.one(c,"SELECT canal FROM canal_control WHERE canal=? FOR UPDATE",channel); }
    public static void openDay(Connection c,String channel,LocalDate day) throws SQLException {
        require(Db.rows(c,"SELECT id FROM cierre_caja WHERE canal=? AND fecha>=?",channel,day).isEmpty(),"La fecha está cerrada. Registra una corrección en fecha abierta.");
    }
    public static BigDecimal available(Connection c,long fund,String channel) throws SQLException {
        var balance=Db.one(c,"SELECT COALESCE(SUM(importe),0) saldo FROM movimiento_dinero WHERE fondo_id=? AND canal=?",fund,channel);
        var reserve=Db.one(c,"SELECT COALESCE(SUM(importe),0) saldo FROM solicitud_dinero WHERE fondo_id=? AND canal=? AND estado='APROBADA' AND tipo NOT IN ('SALDO_INICIAL','AJUSTE_INGRESO')",fund,channel);
        return decimal(balance,"saldo").subtract(decimal(reserve,"saldo"));
    }
    public static void movement(Connection c,long fund,String channel,Long donation,Long request,String type,BigDecimal value,long actor,String detail) throws SQLException {
        openDay(c,channel,today());
        Db.exec(c,"INSERT INTO movimiento_dinero(fondo_id,canal,donacion_id,solicitud_id,tipo,importe,actor_id,detalle,fecha) VALUES(?,?,?,?,?,?,?,?,?)",fund,channel,donation,request,type,value,actor,detail,today());
    }
    public static long request(HttpServletRequest r,long actor,boolean admin) throws Exception {
        String channel=choice(p(r,"canal"),"EFECTIVO","DIGITAL"), type=choice(p(r,"tipo"),"GASTO","ANTICIPO","SALDO_INICIAL","AJUSTE_INGRESO","AJUSTE_SALIDA");
        require(admin||Set.of("GASTO","ANTICIPO").contains(type),"Solo administración solicita ajustes o saldo inicial.");
        var amount=amount(p(r,"importe")); var due=optionalDate(p(r,"limite"));
        require(!type.equals("ANTICIPO")||due!=null&&!due.isBefore(today()),"Indica fecha límite de rendición.");
        String reason=text(p(r,"motivo"),1000,true), payee=text(p(r,"destinatario"),160,true); long fund=number(p(r,"fondo_id"));
        return Db.tx(c->{
            Db.one(c,"SELECT id FROM fondo WHERE id=? AND activo=1",fund);
            long id=Db.insert(c,"INSERT INTO solicitud_dinero(fondo_id,canal,tipo,importe,motivo,destinatario,solicitante_id,limite) VALUES(?,?,?,?,?,?,?,?)",fund,channel,type,amount,reason,payee,actor,type.equals("ANTICIPO")?due:null);
            Db.audit(c,actor,"Solicitud de dinero","#"+id,reason); return id;
        });
    }
    public static void action(HttpServletRequest r,long actor,boolean admin) throws Exception {
        long id=id(r); String action=choice(p(r,"accion"),"aprobar","rechazar","cancelar","pagar","rendir");
        FilesStore.Stored file=Set.of("pagar","rendir").contains(action)?FilesStore.save(r.getPart("archivo"),false):null;
        try {
            Db.tx(c->{
                String channel=Db.str(Db.one(c,"SELECT canal FROM solicitud_dinero WHERE id=?",id),"canal"); lock(c,channel);
                var row=Db.one(c,"SELECT * FROM solicitud_dinero WHERE id=? FOR UPDATE",id);
                String status=Db.str(row,"estado"), type=Db.str(row,"tipo"); long fund=Db.id(row,"fondo_id"); BigDecimal value=decimal(row,"importe");
                require(Db.id(row,"version")==Long.parseLong(p(r,"version")),"La solicitud cambió. Actualiza.");
                String reason=text(p(r,"motivo"),1000,Set.of("rechazar","cancelar").contains(action));
                switch(action) {
                    case "aprobar", "rechazar" -> {
                        require(status.equals("PENDIENTE"),"La solicitud ya fue atendida."); other(Db.id(row,"solicitante_id"),actor);
                        if(action.equals("aprobar")) {
                            Db.one(c,"SELECT id FROM fondo WHERE id=? AND activo=1",fund);
                            if(type.equals("SALDO_INICIAL")) require(Db.rows(c,"SELECT id FROM movimiento_dinero WHERE fondo_id=? AND canal=? LIMIT 1",fund,channel).isEmpty()&&Db.rows(c,"SELECT id FROM solicitud_dinero WHERE fondo_id=? AND canal=? AND tipo='SALDO_INICIAL' AND estado='APROBADA'",fund,channel).isEmpty(),"Ya existe un saldo inicial o movimientos. Solicita un ajuste.");
                            if(!incoming(type)) require(available(c,fund,channel).compareTo(value)>=0,"Saldo disponible insuficiente.");
                        }
                        Db.exec(c,"UPDATE solicitud_dinero SET estado=?,aprobador_id=?,decision_motivo=?,version=version+1 WHERE id=?",action.equals("aprobar")?"APROBADA":"RECHAZADA",actor,reason,id);
                    }
                    case "cancelar" -> {
                        require(Set.of("PENDIENTE","APROBADA").contains(status)&&(admin||Db.id(row,"solicitante_id")==actor&&status.equals("PENDIENTE")),"No puedes cancelar esta solicitud.");
                        Db.exec(c,"UPDATE solicitud_dinero SET estado='CANCELADA',decision_motivo=?,version=version+1 WHERE id=?",reason,id);
                    }
                    case "pagar" -> {
                        require(status.equals("APROBADA"),"La solicitud no está aprobada o ya se ejecutó.");
                        if(type.equals("SALDO_INICIAL")) require(Db.rows(c,"SELECT id FROM movimiento_dinero WHERE fondo_id=? AND canal=? LIMIT 1",fund,channel).isEmpty(),"Ya hay movimientos. Solicita un ajuste.");
                        if(!incoming(type)) require(available(c,fund,channel).signum()>=0,"El saldo no cubre las reservas.");
                        String movementType=Set.of("GASTO","ANTICIPO","SALDO_INICIAL").contains(type)?type:"AJUSTE";
                        movement(c,fund,channel,null,id,movementType,incoming(type)?value:value.negate(),actor,Db.str(row,"motivo"));
                        Db.exec(c,"UPDATE solicitud_dinero SET estado='PAGADA',pagado_por=?,pagado_fecha=?,archivo_clave=?,archivo_tipo=?,version=version+1 WHERE id=?",actor,today(),file.key(),file.type(),id);
                    }
                    case "rendir" -> {
                        require(type.equals("ANTICIPO")&&status.equals("PAGADA"),"Anticipo no disponible para rendir.");
                        BigDecimal spent=zeroAmount(p(r,"gasto_real")); require(spent.compareTo(value)<=0,"El gasto no debe superar el anticipo."); openDay(c,channel,today());
                        if(spent.compareTo(value)<0) movement(c,fund,channel,null,id,"DEVOLUCION_ANTICIPO",value.subtract(spent),actor,"Sobrante recibido de anticipo");
                        Db.exec(c,"UPDATE solicitud_dinero SET estado='RENDIDA',rendido_fecha=?,gasto_real=?,rendicion_clave=?,rendicion_tipo=?,version=version+1 WHERE id=?",today(),spent,file.key(),file.type(),id);
                    }
                    default -> throw new IllegalArgumentException("Acción inválida");
                }
                Db.audit(c,actor,"Dinero: "+action,"#"+id,reason); return null;
            });
        } catch(Exception e) { FilesStore.discard(file); throw e; }
    }
    private static boolean incoming(String type) { return Set.of("SALDO_INICIAL","AJUSTE_INGRESO").contains(type); }
    public static void closing(HttpServletRequest r,long actor) throws Exception {
        if(p(r,"accion").equals("revisar")) {
            Db.tx(c->{
                var row=Db.one(c,"SELECT * FROM cierre_caja WHERE id=? FOR UPDATE",id(r));
                other(Db.id(row,"preparado_por"),actor); require(row.get("revisado_por")==null,"Cierre ya revisado.");
                Db.exec(c,"UPDATE cierre_caja SET revisado_por=? WHERE id=?",actor,id(r)); Db.audit(c,actor,"Cierre revisado","#"+id(r),""); return null;
            }); return;
        }
        String channel=choice(p(r,"canal"),"EFECTIVO","DIGITAL"); var day=date(p(r,"fecha")); require(!day.isAfter(today()),"Fecha futura inválida.");
        var counted=zeroAmount(p(r,"contado")); String notes=text(p(r,"notas"),1000,false);
        var part=r.getPart("archivo"); FilesStore.Stored file=part==null||part.getSize()==0?null:FilesStore.save(part,false);
        try {
            require(!channel.equals("DIGITAL")||file!=null,"Adjunta constancia de conciliación digital.");
            Db.tx(c->{
                lock(c,channel); openDay(c,channel,day);
                var totals=Db.one(c,"SELECT COALESCE(SUM(CASE WHEN fecha<? THEN importe ELSE 0 END),0) apertura, COALESCE(SUM(CASE WHEN fecha=? AND importe>0 THEN importe ELSE 0 END),0) ingresos, COALESCE(SUM(CASE WHEN fecha=? AND importe<0 THEN -importe ELSE 0 END),0) salidas FROM movimiento_dinero WHERE canal=?",day,day,day,channel);
                BigDecimal expected=decimal(totals,"apertura").add(decimal(totals,"ingresos")).subtract(decimal(totals,"salidas"));
                require(counted.compareTo(expected)==0||!notes.isBlank(),"Explica la diferencia del cierre.");
                long id=Db.insert(c,"INSERT INTO cierre_caja(canal,fecha,apertura,ingresos,salidas,esperado,contado,notas,preparado_por,archivo_clave,archivo_tipo) VALUES(?,?,?,?,?,?,?,?,?,?,?)",channel,day,totals.get("apertura"),totals.get("ingresos"),totals.get("salidas"),expected,counted,notes,actor,file==null?null:file.key(),file==null?null:file.type());
                Db.audit(c,actor,"Cierre / conciliación","#"+id,notes); return null;
            });
        } catch(Exception e) { FilesStore.discard(file); throw e; }
    }
}

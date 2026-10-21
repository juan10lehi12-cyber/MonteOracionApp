package pe.monteoracion;

import java.math.BigDecimal;
import java.nio.file.*;
import java.sql.*;
import java.util.*;
import java.util.concurrent.*;
import org.junit.*;
import static org.junit.Assert.*;
import static pe.monteoracion.Requests.form;

/** Solo crea/elimina una base explícita de prueba en localhost. */
public class DatabaseTest {
    private static String url,name,server,user,password;
    private static boolean created;
    @BeforeClass public static void setup() throws Exception {
        url=System.getenv("MONTE_TEST_DB_URL"); Assume.assumeTrue("Integración SQL no habilitada",url!=null);
        assertTrue("Usar una base de prueba local",url.matches("jdbc:mariadb://127\\.0\\.0\\.1:[0-9]+/monte_oracion_java_test_[a-z0-9_]+"));
        assertEquals("Db debe apuntar a la misma base de prueba",url,System.getenv("MONTE_DB_URL"));
        assertNotNull("Configura adjuntos de prueba",System.getenv("MONTE_STORAGE"));
        name=url.substring(url.lastIndexOf('/')+1); server=url.substring(0,url.lastIndexOf('/')+1);
        user=System.getenv("MONTE_DB_USER"); password=Objects.toString(System.getenv("MONTE_DB_PASSWORD"),"");
        Class.forName("org.mariadb.jdbc.Driver");
        try(var c=DriverManager.getConnection(server,user,password); var s=c.createStatement()) {
            s.execute("CREATE DATABASE "+name+" CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci"); created=true;
            String sql=Files.readString(Path.of("sql/01_esquema.sql")).replaceAll("(?m)^--.*$","").replace("monte_oracion_java",name);
            for(String statement:sql.split(";")) if(!statement.isBlank()) s.execute(statement);
        }
    }
    @AfterClass public static void cleanup() throws Exception {
        if(created) try(var c=DriverManager.getConnection(server,user,password);var s=c.createStatement()) { s.execute("DROP DATABASE "+name); }
    }
    private static Map<String,Object> row(String sql,Object... args) throws Exception { return Db.read(c->Db.one(c,sql,args)); }
    private static long actor(String email) throws Exception { return Db.tx(c->Db.insert(c,"INSERT INTO personal(nombre,email,password_hash) VALUES(?,?,?)",email,email,Passwords.hash("TestPrivado2026"))); }
    private static void balance(String expected) throws Exception { assertEquals(0,new BigDecimal(expected).compareTo(Db.read(c->Treasury.available(c,1,"EFECTIVO")))); }
    private static void moneyAction(long id,String action,long actor,Object... extra) throws Exception {
        var values=new ArrayList<Object>(List.of("id",id,"accion",action,"version",row("SELECT version FROM solicitud_dinero WHERE id=?",id).get("version"),"motivo","Prueba documentada"));
        values.addAll(Arrays.asList(extra)); Treasury.action(form(values.toArray()),actor,true);
    }
    private static void stockAction(long id,String action,long actor) throws Exception { Inventory.decision(form("id",id,"accion",action,"version",row("SELECT version FROM solicitud_bienes WHERE id=?",id).get("version"),"motivo","Prueba documentada"),actor,true); }
    @Test public void operationalLifecycleAndConcurrency() throws Exception {
        long a=actor("admin@test.local"),b=actor("otro@test.local");
        Catalog.save(form("modulo","congregantes","dni","12345678","nombres","María","apellidos","Pérez","activo","on"),a);
        Catalog.save(form("modulo","productos","nombre","Arroz","unidad","kg","categoria","Alimentos","activo","on"),a);
        Catalog.save(form("modulo","beneficiarios","nombre","Comedor","direccion","Comas","contacto","Responsable","activo","on"),a);
        assertEquals(36,Db.str(row("SELECT consulta_codigo FROM congregante WHERE dni='12345678'"),"consulta_codigo").length());
        long cash=Donations.register(form("tipo","DINERO","confirmado","on","donante_tipo","CONGREGANTE","dni","12345678","fondo_id","1","importe","100.00","fecha",Rules.today()),a);
        balance("100");
        long advance=Treasury.request(form("canal","EFECTIVO","tipo","ANTICIPO","importe","80.00","fondo_id","1","limite",Rules.today().plusDays(1),"destinatario","Proveedor","motivo","Alimentos"),a,true);
        assertThrows(IllegalArgumentException.class,()->moneyAction(advance,"aprobar",a));
        moneyAction(advance,"aprobar",b); balance("20");
        long tooMuch=Treasury.request(form("canal","EFECTIVO","tipo","GASTO","importe","30.00","fondo_id","1","destinatario","Proveedor","motivo","Ayuda"),a,true);
        assertThrows(IllegalArgumentException.class,()->moneyAction(tooMuch,"aprobar",b));
        moneyAction(advance,"pagar",b,"archivo",Requests.image()); balance("20");
        assertThrows(IllegalArgumentException.class,()->moneyAction(advance,"pagar",b,"archivo",Requests.image()));
        moneyAction(advance,"rendir",b,"gasto_real","60.00","archivo",Requests.image()); balance("40");
        long goods=Donations.register(form("tipo","BIENES","confirmado","on","donante_tipo","ANONIMO","fondo_id","1","fecha",Rules.today(),"archivo",Requests.image(),"producto_id",new String[]{"1"},"cantidad",new String[]{"10"},"ubicacion",new String[]{"A1"},"vencimiento",new String[]{""}),a);
        long stock=Inventory.request(form("tipo","PRESTAMO","prioridad","ALTA","motivo","Evento","necesaria",Rules.today(),"devolucion",Rules.today().plusDays(3),"beneficiario_id","1","lote_id",new String[]{"1"},"cantidad",new String[]{"8"}),a);
        assertThrows(IllegalArgumentException.class,()->stockAction(stock,"aprobar",a)); stockAction(stock,"aprobar",b);
        long line=Db.id(row("SELECT id FROM solicitud_bien WHERE solicitud_id=?",stock),"id"); String command=UUID.randomUUID().toString();
        var delivery=form("linea_id",line,"accion","entregar","cantidad","3","comando",command,"receptor","Responsable","archivo",Requests.image());
        Inventory.line(delivery,b); assertThrows(IllegalArgumentException.class,()->Inventory.line(delivery,b));
        assertEquals("PARCIAL",row("SELECT estado FROM solicitud_bienes WHERE id=?",stock).get("estado"));
        Inventory.line(form("linea_id",line,"accion","entregar","cantidad","5","comando",UUID.randomUUID(),"receptor","Responsable","archivo",Requests.image()),b);
        assertEquals("ENTREGADA",row("SELECT estado FROM solicitud_bienes WHERE id=?",stock).get("estado"));
        Inventory.line(form("linea_id",line,"accion","devolver","cantidad","5","comando",UUID.randomUUID(),"motivo","Retorno","ubicacion","A2"),b);
        Inventory.line(form("linea_id",line,"accion","condonar","cantidad","3","comando",UUID.randomUUID(),"motivo","Pérdida autorizada"),b);
        assertThrows(IllegalArgumentException.class,()->Inventory.line(form("linea_id",line,"accion","devolver","cantidad","1","comando",UUID.randomUUID(),"motivo","Exceso","ubicacion","A2"),b));
        assertThrows(IllegalArgumentException.class,()->Inventory.adjust(form("id","1","tipo","MERMA","cantidad","3","comando",UUID.randomUUID(),"motivo","Exceso"),b));
        // Dos aprobaciones concurrentes no pueden reservar más que el disponible.
        long x=Treasury.request(form("canal","EFECTIVO","tipo","GASTO","importe","30","fondo_id","1","destinatario","Uno","motivo","Concurrente"),a,true);
        long y=Treasury.request(form("canal","EFECTIVO","tipo","GASTO","importe","30","fondo_id","1","destinatario","Dos","motivo","Concurrente"),a,true);
        var pool=Executors.newFixedThreadPool(2); var gate=new CountDownLatch(1);
        try {
            List<Future<Boolean>> futures=new ArrayList<>();
            for(long id:List.of(x,y)) futures.add(pool.submit(()->{gate.await(); try {moneyAction(id,"aprobar",b); return true;} catch(IllegalArgumentException e) {return false;} }));
            gate.countDown(); int accepted=0; for(var f:futures) if(f.get(15,TimeUnit.SECONDS)) accepted++; assertEquals(1,accepted); balance("10");
        } finally { pool.shutdownNow(); }
        Treasury.closing(form("accion","crear","canal","EFECTIVO","fecha",Rules.today(),"contado","40","notas","Conciliado"),a);
        assertThrows(IllegalArgumentException.class,()->Treasury.closing(form("accion","revisar","id","1"),a));
        Treasury.closing(form("accion","revisar","id","1"),b);
        assertThrows(IllegalArgumentException.class,()->Donations.register(form("tipo","DINERO","confirmado","on","donante_tipo","ANONIMO","fondo_id","1","importe","10","fecha",Rules.today()),a));
        assertThrows(IllegalArgumentException.class,()->Donations.decide(form("id",goods,"estado","ANULADA","motivo","No permitido","version","0"),b));
        assertEquals("APROBADA",row("SELECT estado FROM donacion WHERE id=?",cash).get("estado"));
        assertTrue(((Number)row("SELECT COUNT(*) n FROM auditoria").get("n")).intValue()>15);
        // Consulta pública: DNI solo no revela datos; DNI+código sí, sin notas ni archivos.
        var member=row("SELECT consulta_codigo FROM congregante WHERE dni='12345678'");
        var request=form("dni","12345678","codigo",member.get("consulta_codigo")); var pages=new Pages(request);
        Db.read(c->{pages.consultation(c,true);return null;}); assertTrue(pages.h.toString().contains("100.00")); assertFalse(pages.h.toString().contains("archivo_clave"));
        var bad=form("dni","12345678","codigo",UUID.randomUUID()); var badPages=new Pages(bad); Db.read(c->{badPages.consultation(c,true);return null;}); assertFalse(badPages.h.toString().contains("100.00"));
    }
}

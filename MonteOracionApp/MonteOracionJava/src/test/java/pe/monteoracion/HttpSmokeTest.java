package pe.monteoracion;

import java.io.*;
import java.net.*;
import java.net.http.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.time.Duration;
import java.util.*;
import java.util.regex.Pattern;
import org.junit.*;
import static org.junit.Assert.*;

/** Activación: MONTE_HTTP_TEST=1 más las variables de base de prueba; requiere WAR ya compilado. */
public class HttpSmokeTest {
    private static Process tomcat;
    private static String base,secret;
    private static Path runtime;
    private static HttpClient anonymous,admin,warehouse;
    private static HttpClient client() { return HttpClient.newBuilder().cookieHandler(new CookieManager(null,CookiePolicy.ACCEPT_ALL)).connectTimeout(Duration.ofSeconds(4)).followRedirects(HttpClient.Redirect.NEVER).build(); }
    private static HttpResponse<byte[]> get(HttpClient client,String path) throws Exception { return client.send(HttpRequest.newBuilder(URI.create(base+path)).timeout(Duration.ofSeconds(15)).GET().build(),HttpResponse.BodyHandlers.ofByteArray()); }
    private static String body(HttpResponse<byte[]> response) { return new String(response.body(),StandardCharsets.UTF_8); }
    private static String csrf(HttpClient client,String path) throws Exception {
        var response=get(client,path); assertEquals(path,200,response.statusCode());
        var matcher=Pattern.compile("name=[\"']?csrf[\"']? value=[\"']([^\"']+)").matcher(body(response)); assertTrue("Token CSRF en "+path,matcher.find()); return matcher.group(1);
    }
    private static HttpResponse<byte[]> post(HttpClient client,String path,Map<String,String> values) throws Exception {
        String encoded=String.join("&",values.entrySet().stream().map(e->URLEncoder.encode(e.getKey(),StandardCharsets.UTF_8)+"="+URLEncoder.encode(e.getValue(),StandardCharsets.UTF_8)).toList());
        return client.send(HttpRequest.newBuilder(URI.create(base+path)).timeout(Duration.ofSeconds(15)).header("Content-Type","application/x-www-form-urlencoded").POST(HttpRequest.BodyPublishers.ofString(encoded)).build(),HttpResponse.BodyHandlers.ofByteArray());
    }
    private static HttpResponse<byte[]> multipart(HttpClient client,String path,Map<String,String> values) throws Exception {
        String boundary="MonteBoundary"+UUID.randomUUID().toString().replace("-",""); var output=new ByteArrayOutputStream();
        for(var item:values.entrySet()) output.write(("--"+boundary+"\r\nContent-Disposition: form-data; name=\""+item.getKey()+"\"\r\n\r\n"+item.getValue()+"\r\n").getBytes(StandardCharsets.UTF_8));
        output.write(("--"+boundary+"\r\nContent-Disposition: form-data; name=\"archivo\"; filename=\"foto.png\"\r\nContent-Type: image/png\r\n\r\n").getBytes(StandardCharsets.UTF_8));
        output.write(Requests.image().getInputStream().readAllBytes()); output.write(("\r\n--"+boundary+"--\r\n").getBytes(StandardCharsets.UTF_8));
        return client.send(HttpRequest.newBuilder(URI.create(base+path)).timeout(Duration.ofSeconds(15)).header("Content-Type","multipart/form-data; boundary="+boundary).POST(HttpRequest.BodyPublishers.ofByteArray(output.toByteArray())).build(),HttpResponse.BodyHandlers.ofByteArray());
    }
    @BeforeClass public static void setup() throws Exception {
        Assume.assumeTrue("HTTP no habilitado", "1".equals(System.getenv("MONTE_HTTP_TEST")));
        DatabaseTest.setup(); secret="PruebaPrivada-"+UUID.randomUUID();
        Db.tx(c->{
            long a=Db.insert(c,"INSERT INTO personal(nombre,email,password_hash) VALUES('Admin','admin@http.local',?)",Passwords.hash(secret));
            long w=Db.insert(c,"INSERT INTO personal(nombre,email,password_hash) VALUES('Almacén','almacen@http.local',?)",Passwords.hash(secret));
            Db.exec(c,"INSERT INTO personal_rol(personal_id,rol) VALUES(?,'ADMINISTRADOR'),(?,'ALMACENERO')",a,w);
            Db.exec(c,"INSERT INTO medio_pago(nombre,instrucciones) VALUES('Yape','Coordinar con tesorería')");
            Db.exec(c,"INSERT INTO congregante(dni,nombres,apellidos,consulta_codigo) VALUES('87654321','José','Prueba',?)",UUID.randomUUID().toString());
            Db.exec(c,"INSERT INTO producto(nombre,unidad,categoria) VALUES('Arroz','kg','Alimentos')");
            Db.exec(c,"INSERT INTO beneficiario(nombre,direccion,contacto) VALUES('Comedor','Comas','Responsable')");
            return null;
        });
        String tomcatHome=System.getenv("MONTE_TOMCAT_HOME");
        assertNotNull("Indica la carpeta de Tomcat 10.1 en MONTE_TOMCAT_HOME",tomcatHome);
        Path home=Path.of(tomcatHome); runtime=Path.of("target/http-tomcat",UUID.randomUUID().toString()).toAbsolutePath();
        for(String folder:List.of("conf","logs","temp","work","webapps")) Files.createDirectories(runtime.resolve(folder));
        Files.copy(home.resolve("conf/web.xml"),runtime.resolve("conf/web.xml"),StandardCopyOption.REPLACE_EXISTING);
        int port; try(var socket=new java.net.ServerSocket(0,0,InetAddress.getByName("127.0.0.1"))) { port=socket.getLocalPort(); }
        Files.writeString(runtime.resolve("conf/server.xml"),"<Server port=\"-1\"><Service name=\"Catalina\"><Connector address=\"127.0.0.1\" port=\""+port+"\" protocol=\"HTTP/1.1\"/><Engine name=\"Catalina\" defaultHost=\"localhost\"><Host name=\"localhost\" appBase=\"webapps\" autoDeploy=\"false\"/></Engine></Service></Server>");
        Files.copy(Path.of("target/MonteOracionJava.war"),runtime.resolve("webapps/MonteOracionJava.war"),StandardCopyOption.REPLACE_EXISTING);
        var builder=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin/java.exe").toString(),"-Dcatalina.home="+home,"-Dcatalina.base="+runtime,"-Djava.io.tmpdir="+runtime.resolve("temp"),"-cp",home.resolve("bin/bootstrap.jar")+";"+home.resolve("bin/tomcat-juli.jar"),"org.apache.catalina.startup.Bootstrap","start");
        builder.redirectErrorStream(true).redirectOutput(runtime.resolve("console.log").toFile()); tomcat=builder.start();
        base="http://127.0.0.1:"+port+"/MonteOracionJava"; anonymous=client(); admin=client(); warehouse=client();
        boolean ready=false;
        for(int i=0;i<100&&tomcat.isAlive();i++) { try { if(get(anonymous,"/assets/app.css").statusCode()==200) {ready=true;break;} } catch(Exception ignored) { } Thread.sleep(200); }
        assertTrue("Tomcat no inició; revisar target/http-tomcat/console.log",ready);
    }
    @AfterClass public static void cleanup() throws Exception {
        if(tomcat!=null) {tomcat.destroy(); if(!tomcat.waitFor(10,java.util.concurrent.TimeUnit.SECONDS)) tomcat.destroyForcibly();}
        if("1".equals(System.getenv("MONTE_HTTP_TEST"))) DatabaseTest.cleanup();
    }
    private static void login(HttpClient client,String email) throws Exception {
        assertEquals(303,post(client,"/login",Map.of("csrf",csrf(client,"/login"),"email",email,"password",secret)).statusCode());
    }
    @Test public void pagesPermissionsFormsAndFiles() throws Exception {
        assertEquals(200,get(anonymous,"/").statusCode()); assertTrue(body(get(anonymous,"/")).contains("Monte de Oración"));
        assertEquals(302,get(anonymous,"/panel").statusCode()); assertEquals(403,post(anonymous,"/login",Map.of("email","admin@http.local","password",secret)).statusCode());
        login(admin,"admin@http.local"); login(warehouse,"almacen@http.local");
        for(String path:List.of("/panel","/donaciones","/recibir","/almacen","/bienes","/bienes-nueva","/dinero","/dinero-nueva","/saldos","/cierres","/personal","/personal?id=nuevo","/publicaciones","/publicaciones?id=nuevo","/auditoria","/reportes","/password","/catalogo?modulo=congregantes","/catalogo?modulo=congregantes&id=1","/catalogo?modulo=productos&id=nuevo","/catalogo?modulo=beneficiarios","/catalogo?modulo=fondos","/catalogo?modulo=medios","/catalogo?modulo=iglesia&id=1")) {
            var response=get(admin,path); assertEquals(path+": "+body(response),200,response.statusCode());
        }
        for(String path:List.of("/personal","/publicaciones","/saldos","/cierres","/auditoria","/catalogo?modulo=congregantes")) assertEquals(path,403,get(warehouse,path).statusCode());
        var response=multipart(anonymous,"/donar",Map.of("csrf",csrf(anonymous,"/donar"),"donante_tipo","ANONIMO","fondo_id","1","importe","15.50","fecha",Rules.today().toString(),"medio_pago_id","1","operacion","HTTP-001"));
        assertEquals(body(response),200,response.statusCode()); assertTrue(body(response).contains("Donación enviada para revisión"));
        var donation=Db.read(c->Db.one(c,"SELECT id,version FROM donacion WHERE operacion='HTTP-001'")); String id=donation.get("id").toString();
        assertEquals(303,post(admin,"/donacion",Map.of("csrf",csrf(admin,"/donacion?id="+id),"id",id,"version","0","estado","APROBADA","confirmado","on")).statusCode());
        assertEquals(200,get(admin,"/archivo?tipo=donacion&id="+id).statusCode());
        assertEquals(403,get(anonymous,"/archivo?tipo=donacion&id="+id).statusCode()); assertEquals(400,get(warehouse,"/donacion?id="+id).statusCode());
        var pdf=get(admin,"/reporte.pdf?tipo=donaciones&desde="+Rules.today()+"&hasta="+Rules.today()); assertEquals(200,pdf.statusCode()); assertTrue(new String(pdf.body(),0,5,StandardCharsets.US_ASCII).equals("%PDF-"));
        assertEquals(403,get(warehouse,"/reporte.pdf?tipo=dinero&desde="+Rules.today()+"&hasta="+Rules.today()).statusCode());
        // Formulario de configuración multipart, QR público e imagen protegida por publicación.
        assertEquals(303,multipart(admin,"/catalogo",Map.of("csrf",csrf(admin,"/catalogo?modulo=medios&id=1"),"modulo","medios","id","1","nombre","Yape","instrucciones","Prueba <script>alert(1)</script>","activo","on")).statusCode());
        assertEquals(200,get(anonymous,"/archivo?tipo=qr&id=1").statusCode()); assertFalse(body(get(anonymous,"/")).contains("<script>alert(1)</script>"));
        assertEquals(303,multipart(admin,"/publicaciones",Map.of("csrf",csrf(admin,"/publicaciones?id=nuevo"),"tipo","EVENTO","titulo","Reunión de prueba","contenido","Bienvenidos","inicio",Rules.today().plusDays(1)+"T18:00","publicado","on","lugar","Comas")).statusCode());
        assertEquals(200,get(admin,"/publicaciones?id=1").statusCode()); assertEquals(200,get(anonymous,"/archivo?tipo=evento&id=1").statusCode());
        assertEquals(303,post(admin,"/participantes",Map.of("csrf",csrf(admin,"/participantes?evento=1"),"accion","agregar","publicacion_id","1","congregante_id","1")).statusCode());
        assertEquals(200,get(admin,"/participantes?evento=1").statusCode());
        String code=Db.read(c->Db.str(Db.one(c,"SELECT consulta_codigo FROM congregante WHERE id=1"),"consulta_codigo"));
        var query=post(anonymous,"/consulta",Map.of("csrf",csrf(anonymous,"/consulta"),"dni","87654321","codigo",code)); assertEquals(200,query.statusCode()); assertTrue(body(query).contains("Reunión de prueba")); assertFalse(body(query).contains("nota_interna"));
        // Baja de cuenta invalida una sesión existente en su siguiente petición.
        Db.tx(c->{Db.exec(c,"UPDATE personal SET activo=0 WHERE email='almacen@http.local'");return null;}); assertEquals(302,get(warehouse,"/panel").statusCode());
        assertEquals(303,post(admin,"/logout",Map.of("csrf",csrf(admin,"/panel"))).statusCode()); assertEquals(302,get(admin,"/panel").statusCode());
    }
}

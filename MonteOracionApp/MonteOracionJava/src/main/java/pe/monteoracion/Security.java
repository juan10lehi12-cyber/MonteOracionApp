package pe.monteoracion;

import java.io.IOException;
import java.util.*;
import java.util.concurrent.*;
import jakarta.servlet.*;
import jakarta.servlet.annotation.WebFilter;
import jakarta.servlet.http.*;

@WebFilter("/*")
public final class Security implements Filter {
    @Override public void init(FilterConfig config) { }
    @Override public void destroy() { }
    public record User(long id,String name,Set<String> roles) {
        public boolean has(String... allowed) { return Arrays.stream(allowed).anyMatch(roles::contains); }
    }
    private record Window(long start,int count) { }
    private static final ConcurrentHashMap<String,Window> LIMITS=new ConcurrentHashMap<>();
    static boolean permit(String key,int max) {
        long now=System.currentTimeMillis();
        if(LIMITS.size()>20000) { LIMITS.entrySet().removeIf(e->now-e.getValue().start()>300000); if(LIMITS.size()>20000&&!LIMITS.containsKey(key)) return false; }
        return LIMITS.compute(key,(k,w)->w==null||now-w.start()>300000?new Window(now,1):new Window(w.start(),w.count()+1)).count()<=max;
    }
    public static User user(HttpServletRequest r) { return (User)r.getAttribute("user"); }
    public static void role(HttpServletRequest r,String... roles) { if(user(r)==null||!user(r).has(roles)) throw new Forbidden(); }
    public static final class Forbidden extends RuntimeException { }
    public static String csrf(HttpServletRequest r) {
        var session=r.getSession(); String token=(String)session.getAttribute("csrf");
        if(token==null) { token=UUID.randomUUID().toString(); session.setAttribute("csrf",token); } return token;
    }
    @Override public void doFilter(ServletRequest request,ServletResponse response,FilterChain chain) throws IOException,ServletException {
        var r=(HttpServletRequest)request; var s=(HttpServletResponse)response;
        r.setCharacterEncoding("UTF-8"); s.setCharacterEncoding("UTF-8");
        s.setHeader("X-Content-Type-Options","nosniff"); s.setHeader("X-Frame-Options","DENY"); s.setHeader("Referrer-Policy","same-origin");
        s.setHeader("Content-Security-Policy","default-src 'self'; img-src 'self'; style-src 'self'; script-src 'self'; base-uri 'self'; frame-ancestors 'none'; form-action 'self'");
        if(r.isSecure()) s.setHeader("Strict-Transport-Security","max-age=31536000");
        String path=Objects.toString(r.getServletPath(),"");
        if(path.startsWith("/assets/")) { chain.doFilter(r,s); return; }
        s.setHeader("Cache-Control","no-store"); s.setHeader("X-Robots-Tag","noindex, nofollow");
        try {
            var session=r.getSession(false);
            if(session!=null&&session.getAttribute("uid")!=null) {
                long uid=(Long)session.getAttribute("uid");
                var rows=Db.read(c->Db.rows(c,"SELECT * FROM personal WHERE id=? AND activo=1",uid));
                if(rows.isEmpty()||!Db.str(rows.get(0),"password_hash").equals(session.getAttribute("stamp"))) { session.invalidate(); s.sendRedirect(r.getContextPath()+"/login"); return; }
                var roles=Db.read(c->Db.rows(c,"SELECT rol FROM personal_rol WHERE personal_id=?",uid));
                var names=new HashSet<String>(); for(var role:roles) names.add(Db.str(role,"rol"));
                if(names.isEmpty()) { session.invalidate(); s.sendRedirect(r.getContextPath()+"/login"); return; }
                r.setAttribute("user",new User(uid,Db.str(rows.get(0),"nombre"),Set.copyOf(names)));
            }
            if(r.getMethod().equals("POST")) {
                if(Set.of("/login","/donar","/consulta").contains(path) && !permit(path+":"+r.getRemoteAddr(),path.equals("/consulta")?10:15)) { s.sendError(429,"Demasiados intentos. Espera cinco minutos."); return; }
                String expected=csrf(r), actual=r.getParameter("csrf");
                if(!expected.equals(actual)) { s.sendError(403,"Formulario vencido. Actualiza la página."); return; }
            }
            chain.doFilter(r,s);
        } catch(Exception e) {
            if(e instanceof IOException io) throw io;
            if(e instanceof ServletException se) throw se;
            r.getServletContext().log("Error de acceso o conexión",e); s.sendError(500);
        }
    }
}

package pe.monteoracion;

import java.io.*;
import java.lang.reflect.Proxy;
import java.util.*;
import javax.imageio.ImageIO;
import jakarta.servlet.http.*;

final class Requests {
    static HttpServletRequest form(Object... pairs) {
        var params=new HashMap<String,String[]>(); var attrs=new HashMap<String,Object>(); var sessionAttrs=new HashMap<String,Object>(); var parts=new HashMap<String,Part>();
        for(int i=0;i<pairs.length;i+=2) {
            String name=pairs[i].toString(); Object value=pairs[i+1];
            if(value instanceof Part part) parts.put(name,part);
            else if(name.equals("user")) attrs.put("user",value);
            else params.put(name,value instanceof String[] array?array:new String[]{Objects.toString(value,"")});
        }
        var session=(HttpSession)Proxy.newProxyInstance(Requests.class.getClassLoader(),new Class[]{HttpSession.class},(p,m,a)->switch(m.getName()) {
            case "getAttribute" -> sessionAttrs.get(a[0]); case "setAttribute" -> {sessionAttrs.put((String)a[0],a[1]);yield null;} case "removeAttribute" -> sessionAttrs.remove(a[0]); default -> null;
        });
        return (HttpServletRequest)Proxy.newProxyInstance(Requests.class.getClassLoader(),new Class[]{HttpServletRequest.class},(p,m,a)->switch(m.getName()) {
            case "getParameter" -> params.containsKey(a[0])?params.get(a[0])[0]:null;
            case "getParameterValues" -> params.get(a[0]); case "getParameterMap" -> params;
            case "getPart" -> parts.get(a[0]); case "getAttribute" -> attrs.get(a[0]);
            case "setAttribute" -> { attrs.put((String)a[0],a[1]); yield null; }
            case "getSession" -> session; case "getContextPath" -> "/MonteOracionJava";
            default -> null;
        });
    }
    static Part image() throws IOException {
        var bytes=new ByteArrayOutputStream(); ImageIO.write(new java.awt.image.BufferedImage(2,2,java.awt.image.BufferedImage.TYPE_INT_RGB),"png",bytes); return file(bytes.toByteArray(),"foto.png");
    }
    static Part file(byte[] bytes,String name) {
        return new Part() {
            public InputStream getInputStream() { return new ByteArrayInputStream(bytes); }
            public String getContentType() { return "application/octet-stream"; }
            public String getName() { return "archivo"; }
            public String getSubmittedFileName() { return name; }
            public long getSize() { return bytes.length; }
            public void write(String fileName) { throw new UnsupportedOperationException(); }
            public void delete() { }
            public String getHeader(String header) { return null; }
            public Collection<String> getHeaders(String header) { return List.of(); }
            public Collection<String> getHeaderNames() { return List.of(); }
        };
    }
}

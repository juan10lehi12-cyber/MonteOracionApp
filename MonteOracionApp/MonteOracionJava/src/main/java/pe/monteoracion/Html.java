package pe.monteoracion;

import java.util.*;
import jakarta.servlet.http.HttpServletRequest;

/** Único punto de construcción HTML; los datos se escapan antes de insertarse. */
public final class Html {
    private final StringBuilder out=new StringBuilder();
    private final HttpServletRequest request;
    public Html(HttpServletRequest r) { request=r; }
    public static String e(Object value) {
        return Objects.toString(value,"").replace("&","&amp;").replace("<","&lt;").replace(">","&gt;").replace("\"","&quot;").replace("'","&#39;");
    }
    public Html raw(String constant) { out.append(constant); return this; }
    public Html text(Object value) { return raw(e(value)); }
    public String url(String path) { return request.getContextPath()+path; }
    public Html heading(String title) { return raw("<h1>").text(title).raw("</h1>"); }
    public Html sub(String title) { return raw("<h2>").text(title).raw("</h2>"); }
    public Html note(String message) { return raw("<p class=muted>").text(message).raw("</p>"); }
    public Html link(String path,String label) { return raw("<a class=button href=\"").text(url(path)).raw("\">").text(label).raw("</a> "); }
    public Html startForm(String path,boolean multipart) {
        raw("<form method=post action=\"").text(url(path)).raw("\""+(multipart?" enctype=\"multipart/form-data\"":"")+">");
        return hidden("csrf",Security.csrf(request));
    }
    public Html hidden(String name,Object value) { return raw("<input type=hidden name=\"").text(name).raw("\" value=\"").text(value).raw("\">"); }
    public Html input(String name,String label,String type,Object value,int max,boolean required) {
        raw("<label>").text(label).raw("<input name=\"").text(name).raw("\" type=\"").text(type).raw("\" value=\"").text(value).raw("\"");
        if(max>0) raw(" maxlength=\""+max+"\"");
        if(type.equals("number")) raw(" step=\"0.01\" min=\"0\" max=\"9999999999.99\"");
        if(type.equals("password")) raw(" autocomplete=\"new-password\"");
        if(required) raw(" required"); return raw("></label>");
    }
    public Html area(String name,String label,Object value,int max,boolean required) {
        return raw("<label>").text(label).raw("<textarea name=\"").text(name).raw("\" maxlength=\""+max+"\""+(required?" required":"")+">").text(value).raw("</textarea></label>");
    }
    public Html check(String name,String label,boolean checked) {
        return raw("<label class=check><input type=checkbox name=\"").text(name).raw("\""+(checked?" checked":"")+">").text(label).raw("</label>");
    }
    public Html select(String name,String label,Map<String,String> options,Object selected,boolean required) {
        raw("<label>").text(label).raw("<select name=\"").text(name).raw("\""+(required?" required":"")+">");
        raw("<option value=\"\">Selecciona…</option>");
        for(var option:options.entrySet()) raw("<option value=\"").text(option.getKey()).raw("\""+(option.getKey().equals(Objects.toString(selected,""))?" selected":"")+">").text(option.getValue()).raw("</option>");
        return raw("</select></label>");
    }
    public Html choices(String name,String label,Object selected,String... values) {
        var options=new LinkedHashMap<String,String>(); for(String v:values) options.put(v,v.replace('_',' ')); return select(name,label,options,selected,true);
    }
    public Html options(String name,String label,List<Map<String,Object>> rows,String value,String display,Object selected,boolean required) {
        var options=new LinkedHashMap<String,String>(); for(var row:rows) options.put(Objects.toString(row.get(value)),Objects.toString(row.get(display),"")); return select(name,label,options,selected,required);
    }
    public Html file(String label,boolean required,boolean images) {
        return raw("<label>").text(label).raw("<input type=file name=archivo accept=\""+(images?".jpg,.jpeg,.png":".jpg,.jpeg,.png,.pdf")+"\""+(required?" required":"")+"><small>Hasta 5 MB</small></label>");
    }
    public Html submit(String label) { return raw("<button type=submit>").text(label).raw("</button></form>"); }
    public Html table(List<Map<String,Object>> rows,LinkedHashMap<String,String> columns,String details) {
        raw("<div class=table-wrap><table><thead><tr>");
        for(String name:columns.values()) raw("<th>").text(name).raw("</th>"); if(details!=null) raw("<th>Acciones</th>"); raw("</tr></thead><tbody>");
        for(var row:rows) {
            raw("<tr>"); for(String key:columns.keySet()) raw("<td>").text(row.get(key)).raw("</td>");
            if(details!=null) { raw("<td>"); link(details+row.get("id"),"Abrir"); raw("</td>"); } raw("</tr>");
        }
        raw("</tbody></table></div>"); if(rows.isEmpty()) note("No hay registros para mostrar."); return this;
    }
    public static LinkedHashMap<String,String> columns(String... pairs) {
        var result=new LinkedHashMap<String,String>(); for(int i=0;i<pairs.length;i+=2) result.put(pairs[i],pairs[i+1]); return result;
    }
    public Html details(Map<String,Object> row,String... fields) {
        raw("<dl>"); for(int i=0;i<fields.length;i+=2) raw("<dt>").text(fields[i+1]).raw("</dt><dd>").text(row.get(fields[i])).raw("</dd>"); return raw("</dl>");
    }
    @Override public String toString() { return out.toString(); }
}

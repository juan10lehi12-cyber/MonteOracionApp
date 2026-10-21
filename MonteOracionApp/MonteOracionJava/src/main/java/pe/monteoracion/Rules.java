package pe.monteoracion;

import java.math.BigDecimal;
import java.time.*;
import java.util.*;
import jakarta.servlet.http.HttpServletRequest;

public final class Rules {
    private Rules() { }
    public static LocalDate today() { return LocalDate.now(ZoneId.of("America/Lima")); }
    public static LocalDateTime now() { return LocalDateTime.now(ZoneId.of("America/Lima")); }
    public static void require(boolean condition, String message) { if (!condition) throw new IllegalArgumentException(message); }
    public static String text(String value, int max, boolean required) {
        String v = value == null ? "" : value.trim();
        require(v.length() <= max && (!required || !v.isBlank()), "Completa los campos obligatorios y respeta la longitud máxima (" + max + ").");
        return v;
    }
    public static String p(HttpServletRequest r, String name) { return Objects.toString(r.getParameter(name), "").trim(); }
    public static long number(String v) { try { long n = Long.parseLong(v); require(n > 0, "Identificador inválido."); return n; } catch (NumberFormatException e) { throw new IllegalArgumentException("Identificador inválido."); } }
    public static long id(HttpServletRequest r) { return number(p(r,"id")); }
    public static BigDecimal amount(String v) {
        try {
            var n = new BigDecimal(v);
            require(n.scale() <= 2 && n.signum() > 0 && n.compareTo(new BigDecimal("9999999999.99")) <= 0, "Cantidad positiva con máximo dos decimales.");
            return n;
        } catch (NumberFormatException e) { throw new IllegalArgumentException("Importe inválido."); }
    }
    public static BigDecimal zeroAmount(String v) { return "0".equals(v) || "0.00".equals(v) || "0.0".equals(v) ? BigDecimal.ZERO : amount(v); }
    public static BigDecimal decimal(Map<String,Object> row, String key) { return row.get(key) == null ? BigDecimal.ZERO : new BigDecimal(row.get(key).toString()); }
    public static String choice(String v, String... allowed) { require(Arrays.asList(allowed).contains(v), "Opción inválida."); return v; }
    public static LocalDate date(String v) { try { return LocalDate.parse(v); } catch (Exception e) { throw new IllegalArgumentException("Fecha inválida."); } }
    public static LocalDate optionalDate(String v) { return v == null || v.isBlank() ? null : date(v); }
    public static LocalDateTime optionalTime(String v) {
        if (v == null || v.isBlank()) return null;
        try { return LocalDateTime.parse(v); } catch (Exception e) { throw new IllegalArgumentException("Horario inválido."); }
    }
    public static void other(long requester, long approver) { require(requester != approver, "Otra persona debe aprobar tu solicitud."); }
    public static String command(String value) { try { return UUID.fromString(value).toString(); } catch (Exception e) { throw new IllegalArgumentException("Actualiza el formulario antes de enviar."); } }
}

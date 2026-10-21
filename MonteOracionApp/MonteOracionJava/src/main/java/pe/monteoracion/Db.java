package pe.monteoracion;

import java.io.*;
import java.nio.file.*;
import java.sql.*;
import java.util.*;

/** JDBC con parámetros. Cada transacción utiliza una conexión independiente. */
public final class Db {
    private static final Properties CONFIG = load();
    private Db() { }
    private static Properties load() {
        var p = new Properties();
        String file = System.getProperty("monte.config", System.getenv("MONTE_CONFIG"));
        // Configuración por aplicación: funciona en NetBeans sin empaquetar credenciales en el WAR.
        if ((file == null || file.isBlank()) && System.getProperty("catalina.base") != null) {
            Path serverConfig = Path.of(System.getProperty("catalina.base"), "conf", "MonteOracionJava", "database.properties");
            if (Files.isRegularFile(serverConfig)) file = serverConfig.toString();
        }
        if (file != null && !file.isBlank()) {
            try (var in = Files.newInputStream(Path.of(file))) { p.load(new InputStreamReader(in, java.nio.charset.StandardCharsets.UTF_8)); }
            catch (IOException e) { throw new IllegalStateException("No se puede leer MONTE_CONFIG", e); }
        }
        return p;
    }
    public static String config(String env, String property, String fallback) {
        String value = System.getenv(env);
        return value == null ? CONFIG.getProperty(property, fallback) : value;
    }
    public static Connection open() throws SQLException {
        try { Class.forName("org.mariadb.jdbc.Driver"); }
        catch (ClassNotFoundException e) { throw new SQLException("Falta el controlador MariaDB", e); }
        var c = DriverManager.getConnection(config("MONTE_DB_URL", "db.url", "jdbc:mariadb://127.0.0.1:3306/monte_oracion_java"),
                config("MONTE_DB_USER", "db.user", "monte_app"), config("MONTE_DB_PASSWORD", "db.password", ""));
        try (var s = c.createStatement()) { s.execute("SET time_zone = '-05:00'"); }
        return c;
    }
    @FunctionalInterface public interface Work<T> { T run(Connection c) throws Exception; }
    public static <T> T read(Work<T> work) throws Exception { try (var c = open()) { return work.run(c); } }
    public static <T> T tx(Work<T> work) throws Exception {
        try (var c = open()) {
            c.setTransactionIsolation(Connection.TRANSACTION_READ_COMMITTED);
            c.setAutoCommit(false);
            try { T result = work.run(c); c.commit(); return result; }
            catch (Exception e) { try { c.rollback(); } catch (SQLException rollback) { e.addSuppressed(rollback); } throw e; }
        }
    }
    private static PreparedStatement prepare(Connection c, String sql, Object... args) throws SQLException {
        var p = c.prepareStatement(sql, Statement.RETURN_GENERATED_KEYS);
        for (int i = 0; i < args.length; i++) p.setObject(i + 1, args[i]);
        return p;
    }
    public static List<Map<String,Object>> rows(Connection c, String sql, Object... args) throws SQLException {
        try (var p = prepare(c, sql, args); var r = p.executeQuery()) {
            var list = new ArrayList<Map<String,Object>>();
            var meta = r.getMetaData();
            while (r.next()) {
                var row = new LinkedHashMap<String,Object>();
                for (int i = 1; i <= meta.getColumnCount(); i++) row.put(meta.getColumnLabel(i).toLowerCase(Locale.ROOT), r.getObject(i));
                list.add(row);
            }
            return list;
        }
    }
    public static Map<String,Object> one(Connection c, String sql, Object... args) throws SQLException {
        var list = rows(c, sql, args);
        if (list.isEmpty()) throw new IllegalArgumentException("Registro no encontrado.");
        return list.get(0);
    }
    public static int exec(Connection c, String sql, Object... args) throws SQLException {
        try (var p = prepare(c, sql, args)) { return p.executeUpdate(); }
    }
    public static long insert(Connection c, String sql, Object... args) throws SQLException {
        try (var p = prepare(c, sql, args)) {
            p.executeUpdate();
            try (var keys = p.getGeneratedKeys()) { if (keys.next()) return keys.getLong(1); }
            throw new SQLException("No se generó identificador");
        }
    }
    public static long id(Map<String,Object> row, String key) { return ((Number)row.get(key)).longValue(); }
    public static String str(Map<String,Object> row, String key) { return Objects.toString(row.get(key), ""); }
    public static boolean yes(Map<String,Object> row, String key) {
        Object v = row.get(key); return Boolean.TRUE.equals(v) || v instanceof Number n && n.intValue() != 0;
    }
    public static void audit(Connection c, long actor, String action, String ref, String detail) throws SQLException {
        exec(c, "INSERT INTO auditoria(actor_id,accion,referencia,detalle) VALUES(?,?,?,?)", actor, action, ref, detail);
    }
}

package pe.monteoracion;

/** Diagnóstico por terminal sin imprimir contraseñas ni datos personales. */
public final class ConnectionCheck {
    public static void main(String[] args) throws Exception {
        Db.read(c -> {
            var row=Db.one(c,"SELECT DATABASE() base, (SELECT COUNT(*) FROM personal WHERE activo=1) personal_activo");
            Db.one(c,"SELECT id FROM iglesia WHERE id=1");
            System.out.println("Conexión correcta: "+Db.str(row,"base"));
            System.out.println("Cuentas activas: "+row.get("personal_activo"));
            return null;
        });
    }
}

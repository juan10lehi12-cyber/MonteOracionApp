package pe.monteoracion;

import java.util.*;

/** Sin contraseña en argumentos, historial o SQL. */
public final class AdminCli {
    public static void main(String[] args) throws Exception {
        var console = System.console();
        if (console == null) throw new IllegalStateException("Ejecuta scripts/crear-admin.ps1 en una terminal interactiva.");
        String name = Rules.text(console.readLine("Nombre completo: "),160,true);
        String email = Catalog.email(console.readLine("Correo: "));
        char[] password = console.readPassword("Contraseña (10-128 caracteres): ");
        char[] confirmation = console.readPassword("Repetir contraseña: ");
        try {
            Rules.require(Arrays.equals(password,confirmation), "Las contraseñas no coinciden.");
            String hash = Passwords.hash(new String(password));
            Db.tx(c -> {
                long id = Db.insert(c,"INSERT INTO personal(nombre,email,password_hash) VALUES(?,?,?)",name,email,hash);
                Db.exec(c,"INSERT INTO personal_rol(personal_id,rol) VALUES(?,'ADMINISTRADOR')",id);
                Db.audit(c,id,"Administrador creado","Personal #"+id,"Alta desde terminal"); return null;
            });
            console.printf("Administrador creado. No se reemplazó ninguna cuenta existente.%n");
        } finally { Arrays.fill(password,'\0'); Arrays.fill(confirmation,'\0'); }
    }
}

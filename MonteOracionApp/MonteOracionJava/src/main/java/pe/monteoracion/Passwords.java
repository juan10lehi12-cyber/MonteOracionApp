package pe.monteoracion;

import java.security.*;
import java.util.*;
import javax.crypto.SecretKeyFactory;
import javax.crypto.spec.PBEKeySpec;

public final class Passwords {
    private static final int ITERATIONS = 600000;
    private Passwords() { }
    public static String hash(String password) {
        Rules.require(password != null && password.length() >= 10 && password.length() <= 128, "La contraseña debe tener entre 10 y 128 caracteres.");
        var salt = new byte[16]; new SecureRandom().nextBytes(salt);
        return "pbkdf2-sha256$" + ITERATIONS + "$" + Base64.getEncoder().encodeToString(salt) + "$" + Base64.getEncoder().encodeToString(derive(password,salt,ITERATIONS));
    }
    public static boolean verify(String password, String hash) {
        try {
            if (password == null || password.length() > 128) return false;
            var parts = hash.split("\\$");
            if (parts.length != 4 || !parts[0].equals("pbkdf2-sha256")) return false;
            int n = Integer.parseInt(parts[1]);
            if (n < 10000 || n > 2000000) return false;
            return MessageDigest.isEqual(Base64.getDecoder().decode(parts[3]), derive(password,Base64.getDecoder().decode(parts[2]),n));
        } catch (Exception e) { return false; }
    }
    private static byte[] derive(String password, byte[] salt, int iterations) {
        var spec = new PBEKeySpec(password.toCharArray(),salt,iterations,256);
        try { return SecretKeyFactory.getInstance("PBKDF2WithHmacSHA256").generateSecret(spec).getEncoded(); }
        catch (GeneralSecurityException e) { throw new IllegalStateException(e); }
        finally { spec.clearPassword(); }
    }
}

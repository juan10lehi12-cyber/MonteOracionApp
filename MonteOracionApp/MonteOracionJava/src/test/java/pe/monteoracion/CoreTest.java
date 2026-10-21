package pe.monteoracion;

import java.math.BigDecimal;
import java.util.*;
import org.junit.Test;
import static org.junit.Assert.*;
import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.pdfbox.text.PDFTextStripper;

public class CoreTest {
    @Test public void moneyUsesExactDecimals() { assertEquals(new BigDecimal("0.30"),Rules.amount("0.10").add(Rules.amount("0.20"))); }
    @Test public void rejectsInvalidAmounts() {
        for(String value:List.of("-1","0","0.001","10000000000","NaN","abc")) assertThrows(IllegalArgumentException.class,()->Rules.amount(value));
    }
    @Test public void zeroAllowedOnlyForCountingAndSettlement() { assertEquals(BigDecimal.ZERO,Rules.zeroAmount("0.00")); }
    @Test public void approvalNeedsAnotherPerson() { assertThrows(IllegalArgumentException.class,()->Rules.other(1,1)); Rules.other(1,2); }
    @Test public void passwordsAreSaltedAndVerified() {
        String one=Passwords.hash("UnaClavePrivada2026"),two=Passwords.hash("UnaClavePrivada2026");
        assertNotEquals(one,two); assertTrue(Passwords.verify("UnaClavePrivada2026",one)); assertFalse(Passwords.verify("incorrecta",one)); assertFalse(Passwords.verify("x","malformado"));
    }
    @Test public void escapesUntrustedHtml() { assertEquals("&lt;script&gt;&amp;&quot;&#39;",Html.e("<script>&\"'")); }
    @Test public void rejectsCommandsAndEnumsOutsideAllowlist() {
        assertThrows(IllegalArgumentException.class,()->Rules.command("otro")); assertThrows(IllegalArgumentException.class,()->Rules.choice("ADMIN","TESORERO"));
    }
    @Test public void pdfKeepsSpanishAccentsAndPaginates() throws Exception {
        var lines=new ArrayList<String>(); for(int i=0;i<200;i++) lines.add("Donación y oración - página - ñ - registro "+i);
        try(var pdf=PDDocument.load(Reports.pdf(lines))) { assertTrue(pdf.getNumberOfPages()>1); String text=new PDFTextStripper().getText(pdf); assertTrue(text.contains("Donación y oración")); assertTrue(text.contains("registro 199")); }
    }
}

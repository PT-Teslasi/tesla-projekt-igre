/* ============================================================
   TESLA BET - zajednicki pomocnici za forme (prijava i registracija)
   Ucitava se PRE loginStrana.js / registracija.js
   ============================================================ */

/* Pravi funkcije za poruke. `umetni(box)` odlucuje gde se poruka ubacuje u stranicu,
   `dodatniStil` se dodaje na pocetak CSS-a poruke (npr. margin). */
function napraviPoruke(umetni, dodatniStil) {
    function prikaziPoruku(tekst, tip = "error") {
        let box = document.querySelector(".js-poruka");
        if (!box) {
            box = document.createElement("p");
            box.className = "js-poruka";
            box.style.cssText =
                (dodatniStil || "") +
                "padding:12px 14px;border-radius:8px;" +
                "font-size:12px;font-weight:600;line-height:1.5;";
            umetni(box);
        }
        const greska = tip === "error";
        box.textContent = tekst;
        box.style.background = greska ? "rgba(255, 60, 60, 0.12)" : "rgba(60, 220, 120, 0.12)";
        box.style.color      = greska ? "#ff6b6b" : "#4ade80";
        box.style.border     = greska ? "1px solid rgba(255,60,60,0.35)" : "1px solid rgba(60,220,120,0.35)";
    }

    function ocistiPoruku() {
        const box = document.querySelector(".js-poruka");
        if (box) box.remove();
    }

    return { prikaziPoruku, ocistiPoruku };
}

function oznacPolje(el, greska) {
    if (el) el.style.borderColor = greska ? "#ff6b6b" : "";
}

/* Zakljuca dugme dok traje slanje; vraca funkciju koja ga vraca u prvobitno stanje. */
function zakljucajDugme(dugme, html) {
    dugme.disabled = true;
    const original = dugme.innerHTML;
    dugme.innerHTML = html;
    return function () {
        dugme.disabled = false;
        dugme.innerHTML = original;
    };
}

/* ===== KVOTE (login i registracija) =====
   Klik na kvotu je bira (klasa "active"). Unutar jedne grupe (.odds-grid = "Kvota dana",
   .match-odds = jedan mec) moze biti izabrana samo jedna kvota; ponovni klik ponistava izbor. */
document.addEventListener("click", function (e) {
    const kvota = e.target.closest(".odd-card, .match-odd");
    if (!kvota) return;

    const vec = kvota.classList.contains("active");
    const grupa = kvota.closest(".odds-grid, .match-odds");
    grupa.querySelectorAll(".odd-card, .match-odd").forEach(k => k.classList.remove("active"));
    if (!vec) kvota.classList.add("active");
});


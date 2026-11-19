/* ============================================================
   TESLA BET - Prijava (frontend)
   Backend: POST http://localhost:5000/api/prijava
   Telo zahteva (JSON): { unos, sifra }
     unos = korisnicko ime ILI email
   Uspesan odgovor: { poruka, korisnik: { username, ime } }
   ============================================================ */

const API_URL = "http://localhost:5000/api/prijava";
const POCETNA = "/pocetnaStrana/pocetnaStrana.html";

// ---- DOM reference ----
const unosEl     = document.querySelector("#unos");
const sifraEl    = document.querySelector("#sifra");
const zapamtiEl  = document.querySelector("#zapamti");
const submitBtn  = document.querySelector("#btnPrijava");
const toggleBtn  = document.querySelector("#togglePass");
const panel      = document.querySelector(".login-panel");

// ---- Prikazi / sakrij sifru ----
toggleBtn.addEventListener("click", () => {
    const skrivena = sifraEl.type === "password";
    sifraEl.type = skrivena ? "text" : "password";
    toggleBtn.textContent = skrivena ? "Sakrij" : "Prikaži";
});

// ---- Poruke (zajednicki pomocnici iz forme-zajednicko.js) ----
const { prikaziPoruku, ocistiPoruku } = napraviPoruke(
    box => document.querySelector(".login-options").insertAdjacentElement("beforebegin", box)
);

// ---- Frontend validacija ----
function validiraj() {
    oznacPolje(unosEl, false);
    oznacPolje(sifraEl, false);

    if (!unosEl.value.trim()) {
        oznacPolje(unosEl, true);
        return "Unesite korisničko ime ili email.";
    }
    if (!sifraEl.value) {
        oznacPolje(sifraEl, true);
        return "Unesite šifru.";
    }
    return null;
}

// ---- Slanje ----
async function prijavi() {
    ocistiPoruku();

    const greska = validiraj();
    if (greska) { prikaziPoruku(greska, "error"); return; }

    const otkljucaj = zakljucajDugme(
        submitBtn, 'Prijavljujem... <i class="fa-solid fa-spinner fa-spin"></i>');

    try {
        const odgovor = await fetch(API_URL, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                unos:  unosEl.value.trim(),
                sifra: sifraEl.value
            })
        });
        const rezultat = await odgovor.json().catch(() => ({}));

        if (odgovor.ok) {
            // "Zapamti me" -> localStorage (ostaje), inace sessionStorage (do zatvaranja taba)
            const skladiste = zapamtiEl.checked ? localStorage : sessionStorage;
            skladiste.setItem("korisnik", JSON.stringify(rezultat.korisnik || {}));

            prikaziPoruku(rezultat.poruka || "Uspešna prijava!", "success");
            setTimeout(() => { window.location.href = POCETNA; }, 800);
        } else {
            prikaziPoruku(rezultat.greska || "Došlo je do greške.", "error");
            oznacPolje(sifraEl, true);
        }
    } catch (e) {
        prikaziPoruku("Server nije dostupan. Proveri da li API radi.", "error");
        console.error(e);
    } finally {
        otkljucaj();
    }
}

submitBtn.addEventListener("click", (e) => { e.preventDefault(); prijavi(); });

panel.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
        e.preventDefault();
        prijavi();
    }
});
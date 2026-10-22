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

// ---- Prikaz poruke ----
function prikaziPoruku(tekst, tip = "error") {
    let box = document.querySelector(".js-poruka");
    if (!box) {
        box = document.createElement("p");
        box.className = "js-poruka";
        box.style.cssText =
            "padding:12px 14px;border-radius:8px;" +
            "font-size:12px;font-weight:600;line-height:1.5;";
        document.querySelector(".login-options").insertAdjacentElement("beforebegin", box);
    }
    box.textContent = tekst;
    box.style.background = tip === "error"
        ? "rgba(255, 60, 60, 0.12)" : "rgba(60, 220, 120, 0.12)";
    box.style.color = tip === "error" ? "#ff6b6b" : "#4ade80";
    box.style.border = tip === "error"
        ? "1px solid rgba(255,60,60,0.35)" : "1px solid rgba(60,220,120,0.35)";
}

function ocistiPoruku() {
    const box = document.querySelector(".js-poruka");
    if (box) box.remove();
}

function oznacPolje(el, greska) {
    if (el) el.style.borderColor = greska ? "#ff6b6b" : "";
}

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

    submitBtn.disabled = true;
    const original = submitBtn.innerHTML;
    submitBtn.innerHTML = 'Prijavljujem... <i class="fa-solid fa-spinner fa-spin"></i>';

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
        submitBtn.disabled = false;
        submitBtn.innerHTML = original;
    }
}

submitBtn.addEventListener("click", (e) => { e.preventDefault(); prijavi(); });

panel.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
        e.preventDefault();
        prijavi();
    }
});
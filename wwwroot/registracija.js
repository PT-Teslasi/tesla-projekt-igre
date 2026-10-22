/* ============================================================
   TESLA BET - Registracija (frontend)
   Backend: POST http://localhost:5000/api/registracija
   Polja koja backend ocekuje (mala slova):
     ime, prezime, username, email, zemlja, sifra, potvrda,
     licnaPrednja, licnaZadnja, saglasnost
   ============================================================ */

const API_URL = "http://localhost:5000/api/registracija";

// ---- DOM reference (HTML nema id/name, trazimo po placeholder-u) ----
const form = document.querySelector(".form-panel");
const inputs = {
    ime:        document.querySelector('.form-group input[placeholder="Nikola"]'),
    prezime:    document.querySelector('.form-group input[placeholder="Tesla"]'),
    username:   document.querySelector('.form-group input[placeholder="tesla_fan_99"]'),
    email:      document.querySelector('.form-group input[placeholder="ime@primer.com"]'),
    zemlja:     document.querySelector(".form-group select"),
    sifra:      document.querySelector('.form-group input[placeholder="Min. 8 znakova"]'),
    potvrda:    document.querySelector('.form-group input[placeholder="Ponovi šifru"]'),
    saglasnost: document.querySelector("#terms")
};
const fileUpload = document.querySelector(".file-upload");
const submitBtn  = document.querySelector(".btn-submit");

// Skriveni <input type="file"> (u HTML-u ne postoji, pravimo ga ovde)
const fileInput = document.createElement("input");
fileInput.type = "file";
fileInput.accept = ".jpg,.jpeg,.png,.webp,.pdf";
fileInput.multiple = true;
fileInput.style.display = "none";
document.body.appendChild(fileInput);

fileUpload.addEventListener("click", () => fileInput.click());

fileInput.addEventListener("change", () => {
    const fajlovi = Array.from(fileInput.files);
    if (fajlovi.length === 0) return;

    const imena = fajlovi.map(f => f.name).join(", ");
    fileUpload.querySelector(".upload-title").textContent = imena;
    fileUpload.querySelector(".upload-hint").textContent =
        `${fajlovi.length} fajl(a) izabrano`;
    fileUpload.style.borderColor = "#FFD700";
});

// ---- Prikaz poruke ----
function prikaziPoruku(tekst, tip = "error") {
    let box = document.querySelector(".js-poruka");
    if (!box) {
        box = document.createElement("p");
        box.className = "js-poruka";
        box.style.cssText =
            "margin-top:14px;padding:12px 14px;border-radius:8px;" +
            "font-size:12px;font-weight:600;line-height:1.5;";
        submitBtn.insertAdjacentElement("afterend", box);
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
    const v = {
        ime:      inputs.ime.value.trim(),
        prezime:  inputs.prezime.value.trim(),
        username: inputs.username.value.trim(),
        email:    inputs.email.value.trim(),
        sifra:    inputs.sifra.value,
        potvrda:  inputs.potvrda.value
    };

    Object.values(inputs).forEach(el => oznacPolje(el, false));

    if (!v.ime)      { oznacPolje(inputs.ime, true);      return "Ime je obavezno."; }
    if (!v.prezime)  { oznacPolje(inputs.prezime, true);  return "Prezime je obavezno."; }
    if (!v.username) { oznacPolje(inputs.username, true); return "Korisnicko ime je obavezno."; }
    if (v.username.length < 3) {
        oznacPolje(inputs.username, true);
        return "Korisnicko ime mora imati bar 3 znaka.";
    }
    if (!v.email) { oznacPolje(inputs.email, true); return "Email je obavezan."; }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v.email)) {
        oznacPolje(inputs.email, true);
        return "Email nije ispravan.";
    }
    if (v.sifra.length < 8) {
        oznacPolje(inputs.sifra, true);
        return "Sifra mora imati bar 8 znakova.";
    }
    if (v.sifra !== v.potvrda) {
        oznacPolje(inputs.potvrda, true);
        return "Sifre se ne poklapaju.";
    }
    if (fileInput.files.length !== 2) {
        return "Potrebno je priloziti dve slike licne karte (prednja i zadnja).";
    }
    for (const f of fileInput.files) {
        if (!/\.(jpe?g|png|webp|pdf)$/i.test(f.name))
            return `Nedozvoljen format: ${f.name}`;
        if (f.size > 10 * 1024 * 1024)
            return `Fajl je veci od 10 MB: ${f.name}`;
    }
    if (!inputs.saglasnost.checked) {
        return "Morate prihvatiti uslove i potvrditi da imate 18+ godina.";
    }
    return null;
}

// ---- Slanje ----
async function posalji() {
    ocistiPoruku();

    const greska = validiraj();
    if (greska) { prikaziPoruku(greska, "error"); return; }

    // Imena polja MORAJU odgovarati onome sto Program.cs cita iz forme
    const podaci = new FormData();
    podaci.append("ime",          inputs.ime.value.trim());
    podaci.append("prezime",      inputs.prezime.value.trim());
    podaci.append("username",     inputs.username.value.trim());
    podaci.append("email",        inputs.email.value.trim());
    podaci.append("zemlja",       inputs.zemlja.value || "");
    podaci.append("sifra",        inputs.sifra.value);
    podaci.append("potvrda",      inputs.potvrda.value);
    podaci.append("saglasnost",   inputs.saglasnost.checked ? "true" : "false");

    const fajlovi = Array.from(fileInput.files);
    podaci.append("licnaPrednja", fajlovi[0]);
    podaci.append("licnaZadnja",  fajlovi[1]);

    submitBtn.disabled = true;
    const original = submitBtn.innerHTML;
    submitBtn.innerHTML = 'Saljem... <i class="fa-solid fa-spinner fa-spin"></i>';

    try {
        const odgovor = await fetch(API_URL, { method: "POST", body: podaci });
        const rezultat = await odgovor.json().catch(() => ({}));

        if (odgovor.ok) {
            prikaziPoruku(rezultat.poruka || "Nalog je uspesno napravljen!", "success");
            // reset
            inputs.ime.value = "";
            inputs.prezime.value = "";
            inputs.username.value = "";
            inputs.email.value = "";
            inputs.zemlja.selectedIndex = 0;
            inputs.sifra.value = "";
            inputs.potvrda.value = "";
            inputs.saglasnost.checked = false;
            fileInput.value = "";
            fileUpload.querySelector(".upload-title").textContent = "Licna karta ili pasos";
            fileUpload.querySelector(".upload-hint").textContent = "JPG, PNG, WEBP ili PDF - do 10 MB";
            fileUpload.style.borderColor = "";
        } else {
            prikaziPoruku(rezultat.greska || "Doslo je do greske.", "error");
        }
    } catch (e) {
        prikaziPoruku("Server nije dostupan. Proveri da li API radi.", "error");
        console.error(e);
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = original;
    }
}

submitBtn.addEventListener("click", (e) => { e.preventDefault(); posalji(); });

form.addEventListener("keydown", (e) => {
    if (e.key === "Enter" && e.target.tagName !== "TEXTAREA") {
        e.preventDefault();
        posalji();
    }
});
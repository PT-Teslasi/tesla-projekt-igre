/* ===== KVOTE (index.html) =====
   Klik na kvotu je bira (klasa "active"). U jednom meču može biti izabrana samo jedna kvota;
   ponovni klik na izabranu kvotu poništava izbor. */
document.addEventListener("click", function (e) {
    var dugme = e.target.closest(".odds-btn");
    if (!dugme) return;

    var vec = dugme.classList.contains("active");
    var grid = dugme.closest(".odds-grid");
    grid.querySelectorAll(".odds-btn").forEach(function (b) {
        b.classList.remove("active");
        b.setAttribute("aria-pressed", "false");
    });
    if (!vec) {
        dugme.classList.add("active");
        dugme.setAttribute("aria-pressed", "true");
    }
});

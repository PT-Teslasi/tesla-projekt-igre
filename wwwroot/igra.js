/* ===== STRANICA JEDNE IGRE (igra.html?igra=...) ===== */
(function () {
    var igra = nadjiIgru(new URLSearchParams(window.location.search).get('igra'));

    var naslov = document.getElementById('gameTitle');
    var opis = document.getElementById('gameDesc');
    var dugme = document.getElementById('btnPlay');

    if (!igra) {
        naslov.textContent = 'Igra nije pronađena';
        opis.textContent = 'Vrati se na listu i izaberi neku od igara.';
        dugme.style.display = 'none';
        return;
    }

    document.title = 'Tesla Bet - ' + igra.naziv;
    naslov.textContent = igra.naziv;
    document.getElementById('gameGenre').textContent = igra.zanr;
    document.getElementById('gamePlays').textContent = igra.igracaTekst + ' igrača';
    opis.textContent = igra.opis;

    /* slika igre kao pozadina banera; ako je nema u slike/, ostaje boja + oblik */
    postaviSliku(document.getElementById('gameBg'), igra, false);

    /* "Igraj sada" otvara prostor u kome će se učitati igra */
    var prostor = document.getElementById('igraProstor');
    dugme.addEventListener('click', function () {
        prostor.hidden = false;
        prostor.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
})();

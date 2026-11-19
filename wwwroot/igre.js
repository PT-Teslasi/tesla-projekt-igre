/* ===== LISTA IGARA (igre.html) ===== */
(function () {
    var grid = document.getElementById('gamesGrid');
    if (!grid) { return; }
    var sortSelect = document.getElementById('sortIgre');

    function prikazi() {
        var lista = IGRE.slice();
        var nacin = sortSelect ? sortSelect.value : 'popularno';

        if (nacin === 'najmanje') {
            lista.sort(function (a, b) { return a.igraca - b.igraca; });
        } else if (nacin === 'az') {
            lista.sort(function (a, b) { return a.naziv.localeCompare(b.naziv, 'sr'); });
        } else {
            lista.sort(function (a, b) { return b.igraca - a.igraca; });
        }

        var frag = document.createDocumentFragment();
        lista.forEach(function (igra) {
            var a = document.createElement('a');
            a.className = 'game-card';
            a.href = 'igra.html?igra=' + encodeURIComponent(igra.id);

            var slika = document.createElement('div');
            slika.className = 'game-image';
            postaviSliku(slika, igra, true);
            var badge = document.createElement('span');
            badge.className = 'rank-badge';
            badge.textContent = '#' + igra.rang;
            slika.appendChild(badge);

            var info = document.createElement('div');
            info.className = 'game-info';

            var naziv = document.createElement('h3');
            naziv.className = 'game-name';
            naziv.textContent = igra.naziv;

            var meta = document.createElement('div');
            meta.className = 'game-meta';
            var zanr = document.createElement('span');
            zanr.textContent = igra.zanr;
            var igraca = document.createElement('span');
            igraca.className = 'game-plays';
            igraca.textContent = igra.igracaTekst + ' igrača';
            meta.appendChild(zanr);
            meta.appendChild(igraca);

            info.appendChild(naziv);
            info.appendChild(meta);
            a.appendChild(slika);
            a.appendChild(info);
            frag.appendChild(a);
        });

        grid.innerHTML = '';
        grid.appendChild(frag);
    }

    if (sortSelect) { sortSelect.addEventListener('change', prikazi); }
    prikazi();
})();

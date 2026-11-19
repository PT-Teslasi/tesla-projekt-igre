/* ===== PODACI O IGRAMA =====
   id     = ime slike u folderu slike/ (slike/zmija.jpg) i parametar u linku (igra.html?igra=zmija)
   rang   = mesto po popularnosti
   igraca = broj igranja (za sortiranje), igracaTekst = kako se prikazuje
   opis   = tekst u kutiji "O igri" na stranici igre
*/
var IGRE = [
    { id: 'zmija', naziv: 'Zmija', zanr: 'Arkada', rang: 1, igraca: 48200, igracaTekst: '48,2K',
      opis: 'Vodi zmiju kroz polje, skupljaj poene i izbegavaj sudar. Svaki zalogaj je brži od prethodnog. Pokušaj da obaraš rekord.' },
    { id: 'sah-klasik', naziv: 'Šah klasik', zanr: 'Strategija', rang: 2, igraca: 41700, igracaTekst: '41,7K',
      opis: 'Klasičan šah protiv računara ili prijatelja. Planiraj poteze unapred i matiraj protivnika pre nego što on matira tebe.' },
    { id: 'blok-slagalica', naziv: 'Blok slagalica', zanr: 'Puzzle', rang: 3, igraca: 37500, igracaTekst: '37,5K',
      opis: 'Slaži blokove koji padaju i popuni cele redove da bi ih uklonio. Što duže izdržiš, brže padaju.' },
    { id: 'piksel-trka', naziv: 'Piksel trka', zanr: 'Trke', rang: 4, igraca: 33100, igracaTekst: '33,1K',
      opis: 'Prestigni protivnike na pikselizovanoj stazi. Ubiraj bonuse, izbegavaj prepreke i stigni prvi na cilj.' },
    { id: 'svemirski-lov', naziv: 'Svemirski lov', zanr: 'Pucačina', rang: 5, igraca: 29800, igracaTekst: '29,8K',
      opis: 'Brani svemir od talasa vanzemaljskih brodova. Skupljaj poboljšanja za oružje i preživi što duže.' },
    { id: 'memorija', naziv: 'Memorija', zanr: 'Puzzle', rang: 6, igraca: 26400, igracaTekst: '26,4K',
      opis: 'Otkrivaj kartice i pronađi sve parove. Što manje pokušaja, to više poena.' },
    { id: 'kviz-znanja', naziv: 'Kviz znanja', zanr: 'Kviz', rang: 7, igraca: 23900, igracaTekst: '23,9K',
      opis: 'Odgovaraj na pitanja iz različitih oblasti i skupljaj poene. Svaki tačan odgovor u nizu donosi bonus.' },
    { id: 'labirint', naziv: 'Labirint', zanr: 'Avantura', rang: 8, igraca: 21300, igracaTekst: '21,3K',
      opis: 'Pronađi izlaz iz lavirinta pre nego što istekne vreme. Svaki nivo je veći i zamršeniji od prethodnog.' },
    { id: 'karte-21', naziv: 'Karte 21', zanr: 'Karte', rang: 9, igraca: 18700, igracaTekst: '18,7K',
      opis: 'Skupi karte čija je vrednost što bliža broju 21, ali ga nemoj preći. Nadmudri delioca i pobedi.' },
    { id: 'kule', naziv: 'Kule', zanr: 'Strategija', rang: 10, igraca: 16200, igracaTekst: '16,2K',
      opis: 'Gradi odbrambene kule i zaustavi neprijatelje pre nego što stignu do tvoje baze. Planiraj raspored i nadograđuj odbranu.' },
    { id: 'domine', naziv: 'Domine', zanr: 'Table', rang: 11, igraca: 14600, igracaTekst: '14,6K',
      opis: 'Spajaj domine sa istim brojem tačaka i prvi ostani bez kockica. Pametan raspored odlučuje pobednika.' },
    { id: 'slova', naziv: 'Slova', zanr: 'Reči', rang: 12, igraca: 12900, igracaTekst: '12,9K',
      opis: 'Od ponuđenih slova složi što više reči. Duže reči donose više poena.' },
    { id: 'fudbal-2d', naziv: 'Fudbal 2D', zanr: 'Sport', rang: 13, igraca: 11300, igracaTekst: '11,3K',
      opis: 'Brza fudbalska utakmica iz ptičje perspektive. Dodaj, driblaj i postigni više golova od protivnika.' },
    { id: 'farma', naziv: 'Farma', zanr: 'Simulacija', rang: 14, igraca: 9800, igracaTekst: '9,8K',
      opis: 'Zasadi useve, čuvaj životinje i razvij svoju farmu. Prodaj proizvode i proširi imanje.' },
    { id: 'pecaj', naziv: 'Pecaj', zanr: 'Arkada', rang: 15, igraca: 8400, igracaTekst: '8,4K',
      opis: 'Zabaci mamac i uhvati što više riba pre nego što istekne vreme. Ređe ribe donose više poena.' }
];

/* Zajednicki pomocnik za igre.js i igra.js:
   postavlja boju + oblik i ucitava slike/<id>.jpg u dati element.
   Ako slike nema, element dobija klasu "no-img" (ostaje boja + oblik). */
var BOJE = ['#2a2070', '#3a2a8f', '#4a2d7a', '#1e3a6e', '#33257a'];
var OBLICI = ['krug', 'kvadrat', 'trougao'];

function postaviSliku(box, igra, lazy) {
    box.style.setProperty('--c', BOJE[(igra.rang - 1) % BOJE.length]);
    box.setAttribute('data-oblik', OBLICI[(igra.rang - 1) % OBLICI.length]);

    var img = document.createElement('img');
    img.src = 'slike/' + igra.id + '.jpg';
    img.alt = igra.naziv;
    if (lazy) { img.loading = 'lazy'; }
    img.onerror = function () {
        box.classList.add('no-img');
        if (img.parentNode) { img.parentNode.removeChild(img); }
    };
    box.appendChild(img);
}

function nadjiIgru(id) {
    for (var i = 0; i < IGRE.length; i++) {
        if (IGRE[i].id === id) { return IGRE[i]; }
    }
    return null;
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TeslaBet
{
    // Telo JSON zahteva koje salje login.js: { "unos": "...", "sifra": "..." }
    public class PrijavaZahtev
    {
        public string Unos { get; set; } = "";
        public string Sifra { get; set; } = "";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS - dozvoli frontendu (bilo koji port) da zove API
            builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
                p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

            // Kompresija odgovora (HTML/CSS/JS/JSON) - manji prenos, brze ucitavanje
            builder.Services.AddResponseCompression();

            var app = builder.Build();

            app.UseResponseCompression();
            app.UseCors();
            app.UseDefaultFiles();   // / -> wwwroot/index.html
            app.UseStaticFiles();    // servira wwwroot

            // Pripremi bazu i folder za slike pri startu
            Baza.KreirajBazu();
            Directory.CreateDirectory(Baza.FolderKarte);

            // ---- API endpoint: REGISTRACIJA (nepromenjen) ----
            app.MapPost("/api/registracija", async (HttpRequest request) =>
            {
                try
                {
                    var form = await request.ReadFormAsync();

                    string ime      = form["ime"].ToString().Trim();
                    string prezime  = form["prezime"].ToString().Trim();
                    string username = form["username"].ToString().Trim();
                    string email    = form["email"].ToString().Trim();
                    string zemlja   = form["zemlja"].ToString().Trim();
                    string sifra    = form["sifra"].ToString();
                    string potvrda  = form["potvrda"].ToString();
                    bool saglasnost = form["saglasnost"].ToString() == "true";

                    var fPrednja = form.Files.GetFile("licnaPrednja");
                    var fZadnja  = form.Files.GetFile("licnaZadnja");

                    // --- validacija ---
                    if (string.IsNullOrWhiteSpace(ime) || string.IsNullOrWhiteSpace(prezime))
                        return Results.BadRequest(new { greska = "Ime i prezime su obavezni." });

                    if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                        return Results.BadRequest(new { greska = "Korisnicko ime mora imati bar 3 znaka." });

                    if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                        return Results.BadRequest(new { greska = "Email nije ispravan." });

                    if (sifra.Length < 8)
                        return Results.BadRequest(new { greska = "Sifra mora imati bar 8 znakova." });

                    if (sifra != potvrda)
                        return Results.BadRequest(new { greska = "Sifre se ne poklapaju." });

                    if (fPrednja == null || fZadnja == null)
                        return Results.BadRequest(new { greska = "Prilozite obe strane licne karte." });

                    if (!saglasnost)
                        return Results.BadRequest(new { greska = "Morate prihvatiti uslove." });

                    // --- provera zauzetosti (jedan upit u bazu) ---
                    int zauzeto = await Baza.ProveriZauzetostAsync(username, email);

                    if (zauzeto == Baza.UsernameZauzet)
                        return Results.Conflict(new { greska = "Korisnicko ime je zauzeto." });

                    if (zauzeto == Baza.EmailZauzet)
                        return Results.Conflict(new { greska = "Email je vec registrovan." });

                    // --- hash sifre ---
                    Baza.NapraviHash(sifra, out string hash, out string salt);

                    // --- sacuvaj fajlove ---
                    string putanjaPrednja = await Baza.SacuvajFajlAsync(fPrednja, username, "prednja");
                    string putanjaZadnja  = await Baza.SacuvajFajlAsync(fZadnja,  username, "zadnja");

                    // --- upis u bazu ---
                    await Baza.UbaciKorisnikaAsync(ime, prezime, username, email, zemlja,
                                              hash, salt, putanjaPrednja, putanjaZadnja);

                    return Results.Ok(new
                    {
                        poruka = "Nalog je uspesno napravljen! Licna karta ceka verifikaciju."
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GRESKA: " + ex.Message);
                    return Results.Json(new { greska = "Greska na serveru: " + ex.Message },
                                        statusCode: 500);
                }
            });

            // ---- API endpoint: PRIJAVA (NOVO) ----
            app.MapPost("/api/prijava", async (PrijavaZahtev zahtev) =>
            {
                try
                {
                    string unos  = (zahtev.Unos ?? "").Trim();
                    string sifra = zahtev.Sifra ?? "";

                    if (unos == "" || sifra == "")
                        return Results.BadRequest(new { greska = "Unesite korisnicko ime ili email i sifru." });

                    // Trazimo korisnika po username-u ILI email-u.
                    // Ista poruka za "nema korisnika" i "pogresna sifra" (ne odajemo sta postoji)
                    var korisnik = await Baza.NadjiZaPrijavuAsync(unos);

                    if (korisnik == null || !Baza.ProveriSifru(sifra, korisnik.Hash, korisnik.Salt))
                        return Results.Json(new { greska = "Pogresno korisnicko ime/email ili sifra." },
                                            statusCode: 401);

                    return Results.Ok(new
                    {
                        poruka = "Uspesna prijava!",
                        korisnik = new { username = korisnik.Username, ime = korisnik.Ime }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("GRESKA: " + ex.Message);
                    return Results.Json(new { greska = "Greska na serveru: " + ex.Message },
                                        statusCode: 500);
                }
            });

            Console.WriteLine("API radi na http://localhost:5000");
            app.Run("http://localhost:5000");
        }
    }
}
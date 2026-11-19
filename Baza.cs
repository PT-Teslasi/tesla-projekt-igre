using System;
using System.IO;
using System.Data;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace TeslaBet
{
    // Podaci potrebni za proveru prijave
    public record KorisnikPrijava(string Username, string Ime, string Hash, string Salt);

    //NAPRAVITI DA IDE U SQLite bazu!!!!
    public static class Baza
    {
        const string KonekcijaMaster = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
        const string Konekcija       = @"Server=(localdb)\MSSQLLocalDB;Database=TeslaBet;Integrated Security=true;";

        public const string FolderKarte = "LicneKarte";

        // MORA ostati isto (algoritam SHA1 + 100000 iteracija) da bi vec sacuvane sifre i dalje radile
        const int Iteracije = 100000;

        // Rezultat provere zauzetosti
        public const int SlobodnoJe = 0;
        public const int UsernameZauzet = 1;
        public const int EmailZauzet = 2;

        public static void KreirajBazu()
        {
            using (var master = new SqlConnection(KonekcijaMaster))
            {
                master.Open();
                using var cmdBaza = new SqlCommand(
                    "IF DB_ID('TeslaBet') IS NULL CREATE DATABASE TeslaBet", master);
                cmdBaza.ExecuteNonQuery();
            }

            using (var veza = new SqlConnection(Konekcija))
            {
                veza.Open();
                const string sql =
                    "IF OBJECT_ID('dbo.Korisnici', 'U') IS NULL " +
                    "CREATE TABLE dbo.Korisnici (" +
                    "  Id INT IDENTITY(1,1) PRIMARY KEY," +
                    "  Ime NVARCHAR(50) NOT NULL," +
                    "  Prezime NVARCHAR(50) NOT NULL," +
                    "  Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Korisnici_Username UNIQUE," +
                    "  Email NVARCHAR(100) NOT NULL CONSTRAINT UQ_Korisnici_Email UNIQUE," +
                    "  Zemlja NVARCHAR(60) NULL," +
                    "  HashSifre NVARCHAR(100) NOT NULL," +
                    "  SaltSifre NVARCHAR(50) NOT NULL," +
                    "  LicnaPrednja NVARCHAR(260) NOT NULL," +
                    "  LicnaZadnja NVARCHAR(260) NOT NULL," +
                    "  LicnaVerifikovana BIT NOT NULL DEFAULT 0," +
                    "  Saglasnost BIT NOT NULL," +
                    "  DatumRegistracije DATETIME NOT NULL DEFAULT GETDATE()" +
                    ")";
                using var cmd = new SqlCommand(sql, veza);
                cmd.ExecuteNonQuery();
            }
        }

        // Jedan upit umesto dva: vraca SlobodnoJe, UsernameZauzet ili EmailZauzet
        // (username se proverava prvi, kao i ranije)
        public static async Task<int> ProveriZauzetostAsync(string username, string email)
        {
            using var veza = new SqlConnection(Konekcija);
            await veza.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT CASE " +
                "  WHEN EXISTS (SELECT 1 FROM Korisnici WHERE Username = @u) THEN 1 " +
                "  WHEN EXISTS (SELECT 1 FROM Korisnici WHERE Email = @e) THEN 2 " +
                "  ELSE 0 END", veza);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);
            return (int)await cmd.ExecuteScalarAsync();
        }

        static byte[] IzracunajHash(string sifra, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(sifra, salt, Iteracije, HashAlgorithmName.SHA1, 32);

        public static void NapraviHash(string sifra, out string hash, out string salt)
        {
            byte[] saltBajtovi = RandomNumberGenerator.GetBytes(16);
            hash = Convert.ToBase64String(IzracunajHash(sifra, saltBajtovi));
            salt = Convert.ToBase64String(saltBajtovi);
        }

        public static async Task<string> SacuvajFajlAsync(IFormFile fajl, string username, string strana)
        {
            Directory.CreateDirectory(FolderKarte);
            string ekstenzija = Path.GetExtension(fajl.FileName).ToLower();

            // username ulazi u ime fajla, pa izbacujemo znakove koji nisu dozvoljeni u imenu
            // (spreca ../ u korisnickom imenu da upise fajl van foldera LicneKarte)
            string bezbedno = username;
            foreach (char c in Path.GetInvalidFileNameChars())
                bezbedno = bezbedno.Replace(c, '_');

            string novaPutanja = Path.Combine(FolderKarte, bezbedno + "_" + strana + ekstenzija);

            using (var stream = new FileStream(novaPutanja, FileMode.Create))
            {
                await fajl.CopyToAsync(stream);
            }
            return novaPutanja;
        }

        // Trazi korisnika po username-u ILI email-u (oba su UNIQUE, pa ima najvise jedan red).
        // Vraca null ako korisnik ne postoji.
        public static async Task<KorisnikPrijava> NadjiZaPrijavuAsync(string unos)
        {
            using var veza = new SqlConnection(Konekcija);
            await veza.OpenAsync();
            using var cmd = new SqlCommand(
                "SELECT Username, Ime, HashSifre, SaltSifre FROM Korisnici " +
                "WHERE Username = @u OR Email = @u", veza);
            cmd.Parameters.AddWithValue("@u", unos);

            using var citac = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (!await citac.ReadAsync()) return null;

            return new KorisnikPrijava(
                citac.GetString(0), citac.GetString(1), citac.GetString(2), citac.GetString(3));
        }

        // Racuna hash od unete sifre sa istim saltom i poredi ga sa sacuvanim
        public static bool ProveriSifru(string sifra, string hash, string salt)
        {
            byte[] izracunato = IzracunajHash(sifra, Convert.FromBase64String(salt));
            return CryptographicOperations.FixedTimeEquals(izracunato, Convert.FromBase64String(hash));
        }

        public static async Task UbaciKorisnikaAsync(
            string ime, string prezime, string username, string email, string zemlja,
            string hash, string salt, string putanjaPrednja, string putanjaZadnja)
        {
            using var veza = new SqlConnection(Konekcija);
            await veza.OpenAsync();
            using var cmd = new SqlCommand(
                "INSERT INTO Korisnici " +
                "(Ime, Prezime, Username, Email, Zemlja, HashSifre, SaltSifre, " +
                " LicnaPrednja, LicnaZadnja, LicnaVerifikovana, Saglasnost) " +
                "VALUES (@ime, @prezime, @username, @email, @zemlja, @hash, @salt, " +
                " @prednja, @zadnja, 0, 1)", veza);

            cmd.Parameters.AddWithValue("@ime", ime);
            cmd.Parameters.AddWithValue("@prezime", prezime);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@zemlja", string.IsNullOrEmpty(zemlja) ? DBNull.Value : (object)zemlja);
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@salt", salt);
            cmd.Parameters.AddWithValue("@prednja", putanjaPrednja);
            cmd.Parameters.AddWithValue("@zadnja", putanjaZadnja);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}

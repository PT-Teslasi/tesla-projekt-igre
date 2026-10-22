using System;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TeslaBet
{
    //NAPRAVITI DA IDE U SQLite bazu!!!!
    public static class Baza
    {
        const string KonekcijaMaster = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
        const string Konekcija       = @"Server=(localdb)\MSSQLLocalDB;Database=TeslaBet;Integrated Security=true;";

        public const string FolderKarte = "LicneKarte";

        public static void KreirajBazu()
        {
            var master = new SqlConnection(KonekcijaMaster);
            master.Open();
            var cmdBaza = new SqlCommand(
                "IF DB_ID('TeslaBet') IS NULL CREATE DATABASE TeslaBet", master);
            cmdBaza.ExecuteNonQuery();
            master.Close();

            var veza = new SqlConnection(Konekcija);
            veza.Open();
            string sql =
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
            new SqlCommand(sql, veza).ExecuteNonQuery();
            veza.Close();
        }

        public static bool Postoji(string kolona, string vrednost)
        {
            var veza = new SqlConnection(Konekcija);
            veza.Open();
            var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Korisnici WHERE " + kolona + " = @v", veza);
            cmd.Parameters.AddWithValue("@v", vrednost);
            int broj = (int)cmd.ExecuteScalar();
            veza.Close();
            return broj > 0;
        }

        public static void NapraviHash(string sifra, out string hash, out string salt)
        {
            byte[] saltBajtovi = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(saltBajtovi);

            using (var pbkdf2 = new Rfc2898DeriveBytes(sifra, saltBajtovi, 100000))
            {
                hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                salt = Convert.ToBase64String(saltBajtovi);
            }
        }

        public static async Task<string> SacuvajFajlAsync(IFormFile fajl, string username, string strana)
        {
            Directory.CreateDirectory(FolderKarte);
            string ekstenzija = Path.GetExtension(fajl.FileName).ToLower();
            string novaPutanja = Path.Combine(FolderKarte, username + "_" + strana + ekstenzija);

            using (var stream = new FileStream(novaPutanja, FileMode.Create))
            {
                await fajl.CopyToAsync(stream);
            }
            return novaPutanja;
        }

// Trazi korisnika po username-u ILI email-u (oba su UNIQUE, pa ima najvise jedan red)
public static bool NadjiZaPrijavu(string unos, out string username, out string ime,
                                  out string hash, out string salt)
{
    username = ""; ime = ""; hash = ""; salt = "";

    var veza = new SqlConnection(Konekcija);
    veza.Open();
    var cmd = new SqlCommand(
        "SELECT Username, Ime, HashSifre, SaltSifre FROM Korisnici " +
        "WHERE Username = @u OR Email = @u", veza);
    cmd.Parameters.AddWithValue("@u", unos);

    var citac = cmd.ExecuteReader();
    bool nadjen = citac.Read();          // true ako je red pronadjen
    if (nadjen)
    {
        username = citac.GetString(0);
        ime      = citac.GetString(1);
        hash     = citac.GetString(2);
        salt     = citac.GetString(3);
    }
    citac.Close();
    veza.Close();
    return nadjen;
}

// Racuna hash od unete sifre sa istim saltom i poredi ga sa sacuvanim
public static bool ProveriSifru(string sifra, string hash, string salt)
{
    byte[] saltBajtovi = Convert.FromBase64String(salt);
    byte[] sacuvano    = Convert.FromBase64String(hash);

    // MORA isti poziv kao u NapraviHash (isti algoritam i 100000 iteracija)
    using (var pbkdf2 = new Rfc2898DeriveBytes(sifra, saltBajtovi, 100000))
    {
        byte[] izracunato = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(izracunato, sacuvano);
    }
}


        public static void UbaciKorisnika(
            string ime, string prezime, string username, string email, string zemlja,
            string hash, string salt, string putanjaPrednja, string putanjaZadnja)
        {
            var veza = new SqlConnection(Konekcija);
            veza.Open();
            var cmd = new SqlCommand(
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

            cmd.ExecuteNonQuery();
            veza.Close();
        }
    }
}
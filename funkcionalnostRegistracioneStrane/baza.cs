using System;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TeslaBetRegistracija
{
    class Program
    {
        // Konekcija ka SQL Server LocalDB (dolazi uz Visual Studio)
        const string KonekcijaMaster = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
        const string Konekcija = @"Server=(localdb)\MSSQLLocalDB;Database=TeslaBet;Integrated Security=true;";

        // Folder u koji se kopiraju slike licne karte
        const string FolderKarte = "LicneKarte";

        // ------------------------------------------------------------
        // Pravi bazu i tabelu ako ne postoje
        // ------------------------------------------------------------
        static void KreirajBazu()
        {
            // 1. korak: baza TeslaBet
            SqlConnection master = new SqlConnection(KonekcijaMaster);
            master.Open();
            SqlCommand cmdBaza = new SqlCommand(
                "IF DB_ID('TeslaBet') IS NULL CREATE DATABASE TeslaBet", master);
            cmdBaza.ExecuteNonQuery();
            master.Close();

            // 2. korak: tabela Korisnici
            SqlConnection veza = new SqlConnection(Konekcija);
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
            SqlCommand cmdTabela = new SqlCommand(sql, veza);
            cmdTabela.ExecuteNonQuery();
            veza.Close();
        }

        // ------------------------------------------------------------
        // Unos obaveznog polja (ponavlja dok se ne unese nesto)
        // ------------------------------------------------------------
        static string UnesiObavezno(string naziv)
        {
            string vrednost;
            do
            {
                Console.Write(naziv + ": ");               // printf
                vrednost = Console.ReadLine().Trim();      // scanf
                if (vrednost == "")
                    Console.WriteLine("Polje je obavezno!");
            } while (vrednost == "");
            return vrednost;
        }

        // ------------------------------------------------------------
        // Unos sifre sa zvezdicama (ReadKey(true) je kao getch u C-u)
        // ------------------------------------------------------------
        static string UnesiSifru(string naziv)
        {
            string sifra = "";
            ConsoleKeyInfo taster;
            Console.Write(naziv + ": ");
            do
            {
                taster = Console.ReadKey(true);            // getch
                if (taster.Key == ConsoleKey.Backspace && sifra.Length > 0)
                {
                    sifra = sifra.Substring(0, sifra.Length - 1);
                    Console.Write("\b \b");
                }
                else if (taster.Key != ConsoleKey.Enter && taster.Key != ConsoleKey.Backspace)
                {
                    sifra += taster.KeyChar;
                    Console.Write("*");
                }
            } while (taster.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return sifra;
        }

        // ------------------------------------------------------------
        // Provera da li vrednost vec postoji u koloni (Username / Email)
        // ------------------------------------------------------------
        static bool Postoji(string kolona, string vrednost)
        {
            SqlConnection veza = new SqlConnection(Konekcija);
            veza.Open();
            SqlCommand cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Korisnici WHERE " + kolona + " = @v", veza);
            cmd.Parameters.AddWithValue("@v", vrednost);
            int broj = (int)cmd.ExecuteScalar();
            veza.Close();
            return broj > 0;
        }

        // ------------------------------------------------------------
        // Hash sifre: PBKDF2 + nasumicni salt (sifra se NIKAD ne cuva kao tekst)
        // ------------------------------------------------------------
        static void NapraviHash(string sifra, out string hash, out string salt)
        {
            byte[] saltBajtovi = new byte[16];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(saltBajtovi);

            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(sifra, saltBajtovi, 100000);
            hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            salt = Convert.ToBase64String(saltBajtovi);
        }

        // ------------------------------------------------------------
        // Unos putanje do slike: .jpg/.jpeg/.png/.pdf, najvise 10 MB
        // ------------------------------------------------------------
        static string UnesiFajl(string naziv)
        {
            string putanja;
            bool ispravno;
            do
            {
                Console.Write(naziv + " (putanja do fajla): ");
                putanja = Console.ReadLine().Trim().Trim('"');
                ispravno = false;

                if (!File.Exists(putanja))
                {
                    Console.WriteLine("Fajl ne postoji!");
                }
                else
                {
                    string ekstenzija = Path.GetExtension(putanja).ToLower();
                    long velicina = new FileInfo(putanja).Length;

                    if (ekstenzija != ".jpg" && ekstenzija != ".jpeg" &&
                        ekstenzija != ".png" && ekstenzija != ".pdf")
                        Console.WriteLine("Dozvoljeno: JPG, JPEG, PNG ili PDF!");
                    else if (velicina > 10 * 1024 * 1024)
                        Console.WriteLine("Fajl je veci od 10 MB!");
                    else
                        ispravno = true;
                }
            } while (!ispravno);
            return putanja;
        }

        // Kopira fajl u folder LicneKarte i vraca novu putanju (ona ide u bazu)
        static string SacuvajFajl(string izvor, string username, string strana)
        {
            Directory.CreateDirectory(FolderKarte);
            string ekstenzija = Path.GetExtension(izvor).ToLower();
            string novaPutanja = Path.Combine(FolderKarte, username + "_" + strana + ekstenzija);
            File.Copy(izvor, novaPutanja, true);
            return novaPutanja;
        }

        // ------------------------------------------------------------
        // Registracija jednog korisnika
        // ------------------------------------------------------------
        static void Registracija()
        {
            Console.WriteLine();
            Console.WriteLine("=== NAPRAVI NALOG ===");

            // 01 - Podaci o nalogu
            string ime = UnesiObavezno("Ime");
            string prezime = UnesiObavezno("Prezime");

            string username;
            do
            {
                username = UnesiObavezno("Korisnicko ime");
                if (Postoji("Username", username))
                    Console.WriteLine("Korisnicko ime je zauzeto!");
            } while (Postoji("Username", username));

            string email;
            do
            {
                email = UnesiObavezno("Email adresa");
                if (!email.Contains("@") || !email.Contains("."))
                {
                    Console.WriteLine("Email nije ispravan!");
                    email = "";
                }
                else if (Postoji("Email", email))
                {
                    Console.WriteLine("Email je vec registrovan!");
                }
            } while (email == "" || Postoji("Email", email));

            Console.Write("Zemlja (neobavezno, Enter za preskakanje): ");
            string zemlja = Console.ReadLine().Trim();

            // 02 - Sifra pristupa
            string sifra, potvrda;
            do
            {
                sifra = UnesiSifru("Sifra (min. 8 znakova)");
                potvrda = UnesiSifru("Potvrdi sifru");
                if (sifra.Length < 8)
                    Console.WriteLine("Sifra mora imati bar 8 znakova!");
                else if (sifra != potvrda)
                    Console.WriteLine("Sifre se ne poklapaju!");
            } while (sifra.Length < 8 || sifra != potvrda);

            // 03 - Verifikacija identiteta
            string prednja = UnesiFajl("Licna karta - PREDNJA strana");
            string zadnja = UnesiFajl("Licna karta - ZADNJA strana");

            // 04 - Saglasnost
            Console.Write("Saglasan/na sam sa pravilima koriscenja i imam najmanje 18 godina (da/ne): ");
            string saglasnost = Console.ReadLine().Trim().ToLower();
            if (saglasnost != "da")
            {
                Console.WriteLine("Registracija nije moguca bez saglasnosti!");
                return;
            }

            // Hash sifre i cuvanje slika
            string hash, salt;
            NapraviHash(sifra, out hash, out salt);
            string putanjaPrednja = SacuvajFajl(prednja, username, "prednja");
            string putanjaZadnja = SacuvajFajl(zadnja, username, "zadnja");

            // Upis u bazu
            try
            {
                SqlConnection veza = new SqlConnection(Konekcija);
                veza.Open();
                SqlCommand cmd = new SqlCommand(
                    "INSERT INTO Korisnici " +
                    "(Ime, Prezime, Username, Email, Zemlja, HashSifre, SaltSifre, " +
                    " LicnaPrednja, LicnaZadnja, LicnaVerifikovana, Saglasnost) " +
                    "VALUES (@ime, @prezime, @username, @email, @zemlja, @hash, @salt, " +
                    " @prednja, @zadnja, 0, 1)", veza);

                cmd.Parameters.AddWithValue("@ime", ime);
                cmd.Parameters.AddWithValue("@prezime", prezime);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@email", email);
                if (zemlja == "")
                    cmd.Parameters.AddWithValue("@zemlja", DBNull.Value);   // neobavezno polje = NULL
                else
                    cmd.Parameters.AddWithValue("@zemlja", zemlja);
                cmd.Parameters.AddWithValue("@hash", hash);
                cmd.Parameters.AddWithValue("@salt", salt);
                cmd.Parameters.AddWithValue("@prednja", putanjaPrednja);
                cmd.Parameters.AddWithValue("@zadnja", putanjaZadnja);

                cmd.ExecuteNonQuery();
                veza.Close();

                Console.WriteLine();
                Console.WriteLine("Nalog je uspesno napravljen! Licna karta ceka verifikaciju.");
            }
            catch (SqlException greska)
            {
                // 2627 / 2601 = prekrsen UNIQUE uslov
                if (greska.Number == 2627 || greska.Number == 2601)
                    Console.WriteLine("Korisnicko ime ili email vec postoji!");
                else
                    Console.WriteLine("Greska u bazi: " + greska.Message);
            }
        }

        // ------------------------------------------------------------
        // Ispis svih korisnika (za proveru da li radi)
        // ------------------------------------------------------------
        static void IspisiKorisnike()
        {
            SqlConnection veza = new SqlConnection(Konekcija);
            veza.Open();
            SqlCommand cmd = new SqlCommand(
                "SELECT Id, Ime, Prezime, Username, Email, Zemlja, LicnaVerifikovana " +
                "FROM Korisnici", veza);
            SqlDataReader citac = cmd.ExecuteReader();

            Console.WriteLine();
            Console.WriteLine("=== KORISNICI ===");
            while (citac.Read())
            {
                string zemlja = citac.IsDBNull(5) ? "-" : citac.GetString(5);
                string verifikovan = citac.GetBoolean(6) ? "DA" : "NE";
                Console.WriteLine(citac.GetInt32(0) + " | " + citac.GetString(1) + " " +
                    citac.GetString(2) + " | " + citac.GetString(3) + " | " +
                    citac.GetString(4) + " | " + zemlja + " | verifikovan: " + verifikovan);
            }
            citac.Close();
            veza.Close();
        }

        // ------------------------------------------------------------
        // Glavni meni
        // ------------------------------------------------------------
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            try
            {
                KreirajBazu();
            }
            catch (Exception greska)
            {
                Console.WriteLine("Ne mogu da napravim bazu: " + greska.Message);
                Console.ReadKey();                         // getch
                return;
            }

            string izbor;
            do
            {
                Console.WriteLine();
                Console.WriteLine("=== TESLA BET ===");
                Console.WriteLine("1 - Registracija");
                Console.WriteLine("2 - Ispis korisnika");
                Console.WriteLine("0 - Izlaz");
                Console.Write("Izbor: ");
                izbor = Console.ReadLine().Trim();

                if (izbor == "1")
                    Registracija();
                else if (izbor == "2")
                    IspisiKorisnike();
            } while (izbor != "0");

            Console.ReadKey();                             // getch
        }
    }
}
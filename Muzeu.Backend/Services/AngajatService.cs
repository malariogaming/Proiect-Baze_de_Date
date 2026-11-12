using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{
    public class AngajatService
    {
        private const int LungimeMinimaParola = 6;

        private static readonly Regex RegexNumeUtilizator = new Regex(@"^[a-z0-9._]{3,50}$");
        private static readonly Regex RegexEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex RegexTelefon = new Regex(@"^[0-9+ ]{6,20}$");

        private const string MesajLoginGresit = "Nume de utilizator sau parolă greșită.";

        // =====================================================================
        // HASH PAROLA
        // =====================================================================
        public static string CalculeazaHash(string parola)
        {
            using (var sha = SHA256.Create())
            {
                byte[] octeti = sha.ComputeHash(Encoding.UTF8.GetBytes(parola));

                var sb = new StringBuilder(64);
                foreach (byte b in octeti)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        // =====================================================================
        // ANGAJATI - CITIRE
        // =====================================================================

        public List<AngajatDto> GetAngajati(string functie = null, bool includeInactivi = false)
        {
            using (var db = DbFactory.Create())
            {
                IQueryable<Angajati> q = db.Angajati;

                if (!includeInactivi)
                    q = q.Where(a => a.Activ);

                if (!string.IsNullOrWhiteSpace(functie))
                {
                    string f = functie;
                    q = q.Where(a => a.Functie == f);
                }

                return q
                    .OrderBy(a => a.Nume).ThenBy(a => a.Prenume)
                    .Select(a => new AngajatDto
                    {
                        IdAngajat = a.IdAngajat,
                        Nume = a.Nume,
                        Prenume = a.Prenume,
                        Functie = a.Functie,
                        Email = a.Email,
                        Telefon = a.Telefon,
                        DataAngajarii = a.DataAngajarii,
                        Activ = a.Activ,
                        NumeUtilizator = db.Utilizatori.Where(u => u.IdAngajat == a.IdAngajat)
                                                       .Select(u => u.NumeUtilizator).FirstOrDefault()
                    })
                    .ToList();
            }
        }

        public Angajati GetAngajatById(int idAngajat)
        {
            using (var db = DbFactory.Create())
            {
                var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                if (angajat == null)
                    throw new MuzeuException("Angajatul căutat nu există.");

                return angajat;
            }
        }

        public List<ItemLista> GetAngajatiFaraCont()
        {
            using (var db = DbFactory.Create())
            {
                return db.Angajati
                    .Where(a => a.Activ && !db.Utilizatori.Any(u => u.IdAngajat == a.IdAngajat))
                    .OrderBy(a => a.Nume)
                    .Select(a => new ItemLista { Id = a.IdAngajat, Denumire = a.Nume + " " + a.Prenume + " (" + a.Functie + ")" })
                    .ToList();
            }
        }

        // =====================================================================
        // ANGAJATI - SCRIERE
        // =====================================================================

        public int AdaugaAngajat(Angajati angajat)
        {
            if (angajat == null) throw new ArgumentNullException(nameof(angajat));

            if (angajat.DataAngajarii == default(DateTime))
                angajat.DataAngajarii = DateTime.Today;

            angajat.Activ = true;

            using (var db = DbFactory.Create())
            {
                NormalizeazaAngajat(angajat);
                ValideazaAngajat(db, angajat);

                db.Angajati.InsertOnSubmit(angajat);
                db.SubmitChanges();

                return angajat.IdAngajat;
            }
        }
        public void ModificaAngajat(Angajati angajat)
        {
            if (angajat == null) throw new ArgumentNullException(nameof(angajat));

            using (var db = DbFactory.Create())
            {
                var existent = db.Angajati.SingleOrDefault(a => a.IdAngajat == angajat.IdAngajat);
                if (existent == null)
                    throw new MuzeuException("Angajatul nu mai există în baza de date.");

                NormalizeazaAngajat(angajat);
                ValideazaAngajat(db, angajat);

                if (existent.Functie != angajat.Functie)
                {
                    var erori = VerificaResponsabilitati(db, existent.IdAngajat,
                        curator: existent.Functie == FunctiiAngajat.Curator,
                        ghid: existent.Functie == FunctiiAngajat.Ghid,
                        restaurator: existent.Functie == FunctiiAngajat.Restaurator);

                    if (erori.Count > 0)
                    {
                        erori.Insert(0, $"Funcția nu poate fi schimbată din {existent.Functie}:");
                        throw new MuzeuException(erori);
                    }
                }

                existent.Nume = angajat.Nume;
                existent.Prenume = angajat.Prenume;
                existent.Functie = angajat.Functie;
                existent.Email = angajat.Email;
                existent.Telefon = angajat.Telefon;
                existent.DataAngajarii = angajat.DataAngajarii;

                db.SubmitChanges();
            }
        }

        public void DezactiveazaAngajat(int idAngajat)
        {
            using (var db = DbFactory.Create())
            {
                var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                if (angajat == null)
                    throw new MuzeuException("Angajatul nu există.");
                if (!angajat.Activ)
                    throw new MuzeuException("Angajatul este deja inactiv.");

                var erori = VerificaResponsabilitati(db, idAngajat, curator: true, ghid: true, restaurator: true);

                var cont = db.Utilizatori.SingleOrDefault(u => u.IdAngajat == idAngajat);
                if (cont != null && cont.Activ && cont.Rol == RoluriUtilizator.Admin &&
                    EsteUltimulAdminActiv(db, cont.IdUtilizator))
                {
                    erori.Add("Contul lui este singurul cont de Admin activ. Creați sau activați alt Admin întâi.");
                }

                if (erori.Count > 0)
                {
                    erori.Insert(0, $"{angajat.Nume} {angajat.Prenume} nu poate fi dezactivat(ă):");
                    throw new MuzeuException(erori);
                }

                angajat.Activ = false;
                if (cont != null)
                    cont.Activ = false;

                db.SubmitChanges();
            }
        }

        public void ReactiveazaAngajat(int idAngajat)
        {
            using (var db = DbFactory.Create())
            {
                var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                if (angajat == null)
                    throw new MuzeuException("Angajatul nu există.");
                if (angajat.Activ)
                    throw new MuzeuException("Angajatul este deja activ.");

                angajat.Activ = true;
                db.SubmitChanges();
            }
        }

        public void StergeAngajat(int idAngajat)
        {
            using (var db = DbFactory.Create())
            {
                var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                if (angajat == null)
                    throw new MuzeuException("Angajatul nu există.");

                if (db.Utilizatori.Any(u => u.IdAngajat == idAngajat))
                    throw new MuzeuException("Angajatul are un cont de utilizator. Folosiți dezactivarea.");

                bool areIstoric =
                    db.Expozitii.Any(e => e.IdCurator == idAngajat) ||
                    db.TururiGhidate.Any(t => t.IdGhid == idAngajat) ||
                    db.Restaurari.Any(r => r.IdRestaurator == idAngajat) ||
                    db.Vizite.Any(v => v.IdCasier == idAngajat);

                if (areIstoric)
                    throw new MuzeuException(
                        "Angajatul apare în expoziții, tururi, restaurări sau vânzări și nu poate fi șters. Folosiți dezactivarea.");

                db.Angajati.DeleteOnSubmit(angajat);
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // CONTURI - CITIRE
        // =====================================================================

        public List<UtilizatorDto> GetUtilizatori()
        {
            using (var db = DbFactory.Create())
            {
                return db.Utilizatori
                    .OrderBy(u => u.NumeUtilizator)
                    .Select(u => new UtilizatorDto
                    {
                        IdUtilizator = u.IdUtilizator,
                        NumeUtilizator = u.NumeUtilizator,
                        Rol = u.Rol,
                        Activ = u.Activ,
                        UltimaAutentificare = u.UltimaAutentificare,
                        IdAngajat = u.IdAngajat,
                        NumeAngajat = db.Angajati.Where(a => a.IdAngajat == u.IdAngajat)
                                                 .Select(a => a.Nume + " " + a.Prenume).FirstOrDefault()
                    })
                    .ToList();
            }
        }

        public bool ExistaUtilizatori()
        {
            using (var db = DbFactory.Create())
            {
                return db.Utilizatori.Any();
            }
        }

        // =====================================================================
        // CONTURI - SCRIERE
        // =====================================================================

        public int CreeazaPrimulAdmin(string numeUtilizator, string parola)
        {
            if (ExistaUtilizatori())
                throw new MuzeuException("Există deja conturi în aplicație. Un cont nou poate fi creat doar de un Admin.");

            return CreeazaCont(numeUtilizator, parola, RoluriUtilizator.Admin, null);
        }
        public int CreeazaCont(string numeUtilizator, string parola, string rol, int? idAngajat)
        {
            string nume = NormalizeazaNumeUtilizator(numeUtilizator);

            using (var db = DbFactory.Create())
            {
                var erori = new List<string>();

                ValideazaNumeUtilizator(db, nume, erori);
                ValideazaParola(parola, erori);

                if (!RoluriUtilizator.Toate.Contains(rol))
                    erori.Add("Rolul ales nu este valid.");

                if (idAngajat.HasValue)
                {
                    int id = idAngajat.Value;
                    var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == id);

                    if (angajat == null)
                        erori.Add("Angajatul selectat nu există.");
                    else if (!angajat.Activ)
                        erori.Add("Angajatul selectat este inactiv.");
                    else if (db.Utilizatori.Any(u => u.IdAngajat == id))
                        erori.Add($"{angajat.Nume} {angajat.Prenume} are deja un cont.");
                }

                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                var cont = new Utilizatori
                {
                    NumeUtilizator = nume,
                    ParolaHash = CalculeazaHash(parola),
                    Rol = rol,
                    IdAngajat = idAngajat,
                    Activ = true,
                    UltimaAutentificare = null
                };

                db.Utilizatori.InsertOnSubmit(cont);
                db.SubmitChanges();

                return cont.IdUtilizator;
            }
        }
        public UtilizatorAutentificatDto Login(string numeUtilizator, string parola)
        {
            string nume = NormalizeazaNumeUtilizator(numeUtilizator);

            if (string.IsNullOrEmpty(nume) || string.IsNullOrEmpty(parola))
                throw new MuzeuException("Introduceți numele de utilizator și parola.");

            using (var db = DbFactory.Create())
            {
                var cont = db.Utilizatori.SingleOrDefault(u => u.NumeUtilizator == nume);

                if (cont == null || cont.ParolaHash != CalculeazaHash(parola))
                    throw new MuzeuException(MesajLoginGresit);

                Angajati angajat = null;
                if (cont.IdAngajat.HasValue)
                {
                    int idAngajat = cont.IdAngajat.Value;
                    angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                }

                if (!cont.Activ || (angajat != null && !angajat.Activ))
                    throw new MuzeuException("Contul este dezactivat. Contactați un administrator.");

                cont.UltimaAutentificare = DateTime.Now;
                db.SubmitChanges();

                var utilizator = new UtilizatorAutentificatDto
                {
                    IdUtilizator = cont.IdUtilizator,
                    NumeUtilizator = cont.NumeUtilizator,
                    Rol = cont.Rol,
                    IdAngajat = cont.IdAngajat,
                    NumeAngajat = angajat == null ? null : $"{angajat.Nume} {angajat.Prenume}"
                };

                Sesiune.UtilizatorCurent = utilizator;
                return utilizator;
            }
        }

        public void Delogare()
        {
            Sesiune.UtilizatorCurent = null;
        }
        public void SchimbaParola(int idUtilizator, string parolaVeche, string parolaNoua)
        {
            using (var db = DbFactory.Create())
            {
                var cont = GasesteCont(db, idUtilizator);

                if (string.IsNullOrEmpty(parolaVeche) || cont.ParolaHash != CalculeazaHash(parolaVeche))
                    throw new MuzeuException("Parola actuală nu este corectă.");

                var erori = new List<string>();
                ValideazaParola(parolaNoua, erori);
                if (parolaNoua == parolaVeche)
                    erori.Add("Parola nouă trebuie să fie diferită de cea actuală.");

                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                cont.ParolaHash = CalculeazaHash(parolaNoua);
                db.SubmitChanges();
            }
        }
        public void ReseteazaParola(int idUtilizator, string parolaNoua)
        {
            using (var db = DbFactory.Create())
            {
                var cont = GasesteCont(db, idUtilizator);

                var erori = new List<string>();
                ValideazaParola(parolaNoua, erori);
                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                cont.ParolaHash = CalculeazaHash(parolaNoua);
                db.SubmitChanges();
            }
        }

        public void SchimbaRol(int idUtilizator, string rolNou)
        {
            if (!RoluriUtilizator.Toate.Contains(rolNou))
                throw new MuzeuException("Rolul ales nu este valid.");

            using (var db = DbFactory.Create())
            {
                var cont = GasesteCont(db, idUtilizator);

                if (cont.Rol == RoluriUtilizator.Admin && rolNou != RoluriUtilizator.Admin &&
                    cont.Activ && EsteUltimulAdminActiv(db, idUtilizator))
                {
                    throw new MuzeuException("Acesta este singurul cont de Admin activ, deci nu îi puteți schimba rolul.");
                }

                cont.Rol = rolNou;
                db.SubmitChanges();
            }
        }

        public void DezactiveazaCont(int idUtilizator)
        {
            using (var db = DbFactory.Create())
            {
                var cont = GasesteCont(db, idUtilizator);
                if (!cont.Activ)
                    throw new MuzeuException("Contul este deja dezactivat.");

                if (cont.Rol == RoluriUtilizator.Admin && EsteUltimulAdminActiv(db, idUtilizator))
                    throw new MuzeuException("Acesta este singurul cont de Admin activ și nu poate fi dezactivat.");

                cont.Activ = false;
                db.SubmitChanges();
            }
        }

        public void ReactiveazaCont(int idUtilizator)
        {
            using (var db = DbFactory.Create())
            {
                var cont = GasesteCont(db, idUtilizator);
                if (cont.Activ)
                    throw new MuzeuException("Contul este deja activ.");

                if (cont.IdAngajat.HasValue)
                {
                    int idAngajat = cont.IdAngajat.Value;
                    if (!db.Angajati.Any(a => a.IdAngajat == idAngajat && a.Activ))
                        throw new MuzeuException("Angajatul acestui cont este inactiv. Reactivați întâi angajatul.");
                }

                cont.Activ = true;
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // METODE AJUTATOARE (private)
        // =====================================================================

        private static Utilizatori GasesteCont(MuzeuDBDataContext db, int idUtilizator)
        {
            var cont = db.Utilizatori.SingleOrDefault(u => u.IdUtilizator == idUtilizator);
            if (cont == null)
                throw new MuzeuException("Contul nu există.");
            return cont;
        }

        private static bool EsteUltimulAdminActiv(MuzeuDBDataContext db, int idUtilizator)
        {
            return !db.Utilizatori.Any(u =>
                u.IdUtilizator != idUtilizator && u.Rol == RoluriUtilizator.Admin && u.Activ);
        }

        private static List<string> VerificaResponsabilitati(MuzeuDBDataContext db, int idAngajat,
                                                             bool curator, bool ghid, bool restaurator)
        {
            var erori = new List<string>();
            DateTime azi = DateTime.Today;
            DateTime acum = DateTime.Now;

            if (curator)
            {
                var expozitii = db.Expozitii
                    .Where(e => e.IdCurator == idAngajat && (e.DataSfarsit == null || e.DataSfarsit >= azi))
                    .Select(e => e.Titlu)
                    .ToList();

                erori.AddRange(expozitii.Select(t => $"Este curator al expoziției „{t}”. Alegeți alt curator."));
            }

            if (ghid)
            {
                var tururi = db.TururiGhidate
                    .Where(t => t.IdGhid == idAngajat && t.DataOraInceput >= acum)
                    .OrderBy(t => t.DataOraInceput)
                    .Select(t => t.DataOraInceput)
                    .ToList();

                erori.AddRange(tururi.Select(d => $"Are un tur ghidat programat pe {d:dd.MM.yyyy HH:mm}. Alegeți alt ghid."));
            }

            if (restaurator)
            {
                int restaurari = db.Restaurari.Count(r => r.IdRestaurator == idAngajat && r.DataSfarsit == null);
                if (restaurari > 0)
                    erori.Add($"Are {restaurari} restaurare(ări) în curs. Finalizați-le sau alegeți alt restaurator.");
            }

            return erori;
        }

        private static void NormalizeazaAngajat(Angajati a)
        {
            a.Nume = a.Nume?.Trim();
            a.Prenume = a.Prenume?.Trim();
            a.Email = string.IsNullOrWhiteSpace(a.Email) ? null : a.Email.Trim().ToLowerInvariant();
            a.Telefon = string.IsNullOrWhiteSpace(a.Telefon) ? null : a.Telefon.Trim();
            a.DataAngajarii = a.DataAngajarii.Date;
        }

        private static void ValideazaAngajat(MuzeuDBDataContext db, Angajati a)
        {
            var erori = new List<string>();

            if (string.IsNullOrEmpty(a.Nume))
                erori.Add("Numele este obligatoriu.");
            else if (a.Nume.Length > 50)
                erori.Add("Numele poate avea maximum 50 de caractere.");

            if (string.IsNullOrEmpty(a.Prenume))
                erori.Add("Prenumele este obligatoriu.");
            else if (a.Prenume.Length > 50)
                erori.Add("Prenumele poate avea maximum 50 de caractere.");

            if (!FunctiiAngajat.Toate.Contains(a.Functie))
                erori.Add("Funcția nu este validă.");

            if (a.Email != null)
            {
                if (a.Email.Length > 100 || !RegexEmail.IsMatch(a.Email))
                    erori.Add("Adresa de email nu este validă.");
                else
                {
                    string email = a.Email;
                    int idAngajat = a.IdAngajat;   // 0 la adăugare
                    if (db.Angajati.Any(x => x.Email == email && x.IdAngajat != idAngajat))
                        erori.Add($"Adresa de email {email} este folosită deja de alt angajat.");
                }
            }

            if (a.Telefon != null && !RegexTelefon.IsMatch(a.Telefon))
                erori.Add("Telefonul poate conține doar cifre, spații și +, între 6 și 20 de caractere.");

            if (a.DataAngajarii > DateTime.Today)
                erori.Add("Data angajării nu poate fi în viitor.");

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }

        private static string NormalizeazaNumeUtilizator(string nume)
        {
            return nume?.Trim().ToLowerInvariant();
        }

        private static void ValideazaNumeUtilizator(MuzeuDBDataContext db, string nume, List<string> erori)
        {
            if (string.IsNullOrEmpty(nume))
            {
                erori.Add("Numele de utilizator este obligatoriu.");
                return;
            }

            if (!RegexNumeUtilizator.IsMatch(nume))
            {
                erori.Add("Numele de utilizator trebuie să aibă 3-50 de caractere: litere, cifre, punct sau _.");
                return;
            }

            if (db.Utilizatori.Any(u => u.NumeUtilizator == nume))
                erori.Add($"Numele de utilizator „{nume}” este deja folosit.");
        }

        private static void ValideazaParola(string parola, List<string> erori)
        {
            if (string.IsNullOrEmpty(parola))
            {
                erori.Add("Parola este obligatorie.");
                return;
            }

            if (parola.Length < LungimeMinimaParola)
                erori.Add($"Parola trebuie să aibă cel puțin {LungimeMinimaParola} caractere.");
            if (!parola.Any(char.IsLetter) || !parola.Any(char.IsDigit))
                erori.Add("Parola trebuie să conțină cel puțin o literă și o cifră.");
        }
    }
}
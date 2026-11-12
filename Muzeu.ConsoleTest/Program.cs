using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Muzeu.Backend;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;
using Muzeu.Backend.Services;

namespace Muzeu.ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;   // ca să apară corect ă, ș, ț

            // Comentați un apel dacă vreți să rulați doar testele pentru un singur serviciu.
            //TesteazaExpozitii();
            //TesteazaArtefacte();
            //TesteazaProgram();
            //TesteazaAngajati();
            //TesteazaTururi();
            TesteazaVanzari();

            Console.WriteLine("\nGata. Apasă o tastă...");
            Console.ReadKey();
        }

        // =====================================================================
        // TESTE ExpozitieService
        // =====================================================================
        static void TesteazaExpozitii()
        {
            var service = new ExpozitieService();

            Titlu("1. Toate expozițiile");
            Afiseaza(service.GetAll());

            Titlu("2. Active azi");
            Afiseaza(service.GetActive(DateTime.Today));

            Titlu("3. Căutare după 'pictura'");
            Afiseaza(service.Cauta("pictura"));

            Titlu("4. Liste pentru ComboBox");
            Console.WriteLine("  Săli:     " + string.Join(", ", service.GetSali()));
            Console.WriteLine("  Curatori: " + string.Join(", ", service.GetCuratori()));

            Titlu("5. Adăugare validă");
            var noua = new Expozitii
            {
                Titlu = "Icoane pe sticlă din Transilvania",
                Descriere = "Expoziție temporară de icoane pe sticlă din sec. XVIII-XIX.",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2027, 2, 10),
                DataSfarsit = new DateTime(2027, 4, 30),
                IdSala = 3,
                IdCurator = 2,
                PretSuplimentar = 10
            };
            int idNou = service.Adauga(noua);
            Console.WriteLine($"  OK: adăugată cu Id = {idNou}");

            Titlu("6. Adăugare cu date greșite (trebuie respinsă)");
            AsteaptaEroare(() => service.Adauga(new Expozitii
            {
                Titlu = "   ",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2026, 12, 1),
                DataSfarsit = new DateTime(2026, 11, 1),
                IdSala = 3
            }));

            Titlu("7. Adăugare într-o sală ocupată (trebuie respinsă)");
            AsteaptaEroare(() => service.Adauga(new Expozitii
            {
                Titlu = "Expoziție suprapusă",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2026, 12, 1),
                DataSfarsit = new DateTime(2026, 12, 20),
                IdSala = 3              // ocupată de "Ceramica de Cucuteni" până la 31.01.2027
            }));

            Titlu("8. Modificare");
            var deModificat = service.GetById(idNou);
            deModificat.PretSuplimentar = 12;
            service.Modifica(deModificat);
            Console.WriteLine($"  OK: preț nou = {service.GetById(idNou).PretSuplimentar} lei");

            Titlu("9. Ștergerea unei expoziții cu artefacte (trebuie respinsă)");
            AsteaptaEroare(() => service.Sterge(1));

            Titlu("10. Ștergerea expoziției adăugate la pasul 5");
            service.Sterge(idNou);
            Console.WriteLine("  OK: ștearsă");
        }

        // =====================================================================
        // TESTE ArtefactService
        // =====================================================================
        static void TesteazaArtefacte()
        {
            var service = new ArtefactService();

            Titlu("A1. Toate artefactele");
            Afiseaza(service.GetAll());

            Titlu("A2. Căutare text 'aur'");
            Afiseaza(service.Cauta(new FiltruArtefacte { Text = "aur" }));

            Titlu("A3. Filtru: categoria Pictura (Id 2) și starea Buna");
            Afiseaza(service.Cauta(new FiltruArtefacte { IdCategorie = 2, StareConservare = StariConservare.Buna }));

            Titlu("A4. Din depozit");
            Afiseaza(service.GetDinDepozit());

            Titlu("A5. Restaurări în curs");
            Afiseaza(service.GetRestaurariInCurs());

            Titlu("A6. Liste pentru ComboBox");
            Console.WriteLine("  Categorii:    " + string.Join(", ", service.GetCategorii()));
            Console.WriteLine("  Expoziții:    " + string.Join(", ", service.GetExpozitiiPentruMutare()));
            Console.WriteLine("  Restauratori: " + string.Join(", ", service.GetRestauratori()));

            Titlu("A7. Adăugare validă (în depozit)");
            string cod = service.GenereazaCodInventar();
            int idNou = service.Adauga(new Artefacte
            {
                CodInventar = cod,
                Denumire = "Opaiț roman de test",
                Material = "Lut ars",
                Perioada = "sec. II d.Hr.",
                AnAproximativ = 150,
                IdCategorie = 1,                        // Arheologie
                StareConservare = StariConservare.Satisfacatoare,
                IdExpozitie = null
            });
            Console.WriteLine($"  OK: adăugat cu Id = {idNou}, cod = {cod}");

            // Tot ce urmează e în try/finally, ca artefactul de test să fie șters
            // chiar dacă un pas dă eroare (așa testul poate fi rulat de oricâte ori).
            try
            {
                Titlu("A8. Adăugare cu date greșite (trebuie respinsă)");
                AsteaptaEroare(() => service.Adauga(new Artefacte
                {
                    CodInventar = "inv-0001",           // există deja (e transformat în INV-0001)
                    Denumire = "",
                    IdCategorie = 99,                   // nu există
                    StareConservare = "Excelenta",      // nu e o valoare permisă
                    ValoareEstimata = -100
                }));

                Titlu("A9. Modificare");
                var deModificat = service.GetById(idNou);
                deModificat.Dimensiuni = "8 x 6 cm";
                deModificat.ValoareEstimata = 1500;
                service.Modifica(deModificat);
                Afiseaza(service.GetDetalii(idNou));

                Titlu("A10. Mutare în expoziția 1");
                service.MutaInExpozitie(idNou, 1);
                Afiseaza(service.GetDetalii(idNou));

                Titlu("A11. Mutare din nou în aceeași expoziție (trebuie respinsă)");
                AsteaptaEroare(() => service.MutaInExpozitie(idNou, 1));

                Titlu("A12. Începere restaurare (trebuie mutat automat în depozit)");
                int idRestaurare = service.IncepeRestaurare(idNou, 5, DateTime.Today, "Consolidare și curățare.");
                Console.WriteLine($"  OK: restaurare Id = {idRestaurare}");
                Afiseaza(service.GetDetalii(idNou));

                Titlu("A13. Mutare în expoziție în timpul restaurării (trebuie respinsă)");
                AsteaptaEroare(() => service.MutaInExpozitie(idNou, 1));

                Titlu("A14. Finalizare restaurare");
                service.FinalizeazaRestaurare(idRestaurare, DateTime.Today, 750, StariConservare.Buna);
                Afiseaza(service.GetDetalii(idNou));
                Afiseaza(service.GetRestaurari(idNou));

                Titlu("A15. Istoricul locațiilor");
                foreach (var m in service.GetIstoricLocatii(idNou))
                    Console.WriteLine($"  {m.DataMutare:dd.MM.yyyy HH:mm} | {m.De} -> {m.La}");

                Titlu("A16. Ștergere artefact cu istoric (trebuie respinsă)");
                AsteaptaEroare(() => service.Sterge(idNou));
            }
            finally
            {
                StergeArtefactDeTest(idNou);
                Console.WriteLine("\n  (artefactul de test a fost șters din bază)");
            }
        }

        // =====================================================================
        // TESTE ProgramService
        // =====================================================================
        static void TesteazaProgram()
        {
            var service = new ProgramService();

            // Păstrăm programul inițial ca să-l putem reface la final, orice s-ar întâmpla.
            var programInitial = service.GetProgramSaptamanal();
            var exceptiiDeSters = new List<int>();

            try
            {
                Titlu("P1. Programul săptămânal");
                Afiseaza(programInitial);

                Titlu("P2. Programul între 28.11.2026 și 03.12.2026 (1 decembrie e excepție)");
                Afiseaza(service.GetProgramPerioada(new DateTime(2026, 11, 28), new DateTime(2026, 12, 3)));

                Titlu("P3. EsteDeschis (funcția SQL) comparat cu programul calculat în C#");
                var momente = new[]
                {
                    new DateTime(2026, 11, 17, 12, 0, 0),   // marți la prânz           -> deschis
                    new DateTime(2026, 11, 17, 18, 0, 0),   // marți fix la 18:00        -> închis (se închide la 18)
                    new DateTime(2026, 11, 19, 19, 30, 0),  // joi seara (program la 20) -> deschis
                    new DateTime(2026, 11, 16, 12, 0, 0),   // luni                      -> închis
                    new DateTime(2026, 12, 1, 12, 0, 0),    // 1 Decembrie (excepție)    -> închis
                    new DateTime(2027, 5, 15, 12, 0, 0),    // Noaptea Muzeelor, prânz   -> închis (excepția e 18:00-23:59)
                    new DateTime(2027, 5, 15, 22, 0, 0)     // Noaptea Muzeelor, seara   -> deschis
                };
                foreach (var m in momente)
                {
                    bool dinSql = service.EsteDeschis(m);
                    var p = service.GetProgramPentruData(m);
                    bool dinCSharp = !p.Inchis && m.TimeOfDay >= p.OraDeschidere && m.TimeOfDay < p.OraInchidere;

                    Console.WriteLine(
                        $"  {p.NumeZi,-9} {m:dd.MM.yyyy HH:mm} | SQL: {(dinSql ? "deschis" : "închis"),-7} | " +
                        $"C#: {(dinCSharp ? "deschis" : "închis"),-7} | {(dinSql == dinCSharp ? "OK" : "DIFERIT!")}");
                }

                Titlu("P4. EsteDeschisIntre (pentru tururi)");
                Console.WriteLine("  Duminică 15.11 14:00-14:45: " +
                    service.EsteDeschisIntre(new DateTime(2026, 11, 15, 14, 0, 0), new DateTime(2026, 11, 15, 14, 45, 0)) +
                    "  (așteptat True)");
                Console.WriteLine("  Duminică 15.11 16:30-17:15: " +
                    service.EsteDeschisIntre(new DateTime(2026, 11, 15, 16, 30, 0), new DateTime(2026, 11, 15, 17, 15, 0)) +
                    "  (așteptat False: se închide la 17:00)");

                Titlu("P5. Următoarea zi deschisă după luni 16.11.2026");
                Console.WriteLine($"  {service.UrmatoareaZiDeschisa(new DateTime(2026, 11, 16)):dd.MM.yyyy}  (așteptat 17.11.2026)");

                Titlu("P6. ParseazaOra");
                Console.WriteLine($"  \"9:30\" -> {ProgramService.ParseazaOra("9:30")}");
                AsteaptaEroare(() => ProgramService.ParseazaOra("25:00"));
                AsteaptaEroare(() => ProgramService.ParseazaOra("abc"));

                Titlu("P7. Deschidem lunea 10:00-16:00");
                service.ModificaZi(1, false, new TimeSpan(10, 0, 0), new TimeSpan(16, 0, 0));
                Afiseaza(new List<ProgramZiDto> { service.GetProgramSaptamanal()[0] });

                Titlu("P8. Ore inversate (trebuie respinsă)");
                AsteaptaEroare(() => service.ModificaZi(1, false, new TimeSpan(16, 0, 0), new TimeSpan(10, 0, 0)));

                // Pentru P9 și P13 avem nevoie de un tur ghidat viitor (citit direct din bază, doar în test).
                DateTime? urmatorulTur;
                using (var db = DbFactory.Create())
                {
                    urmatorulTur = db.TururiGhidate
                        .Where(t => t.DataOraInceput >= DateTime.Now)
                        .OrderBy(t => t.DataOraInceput)
                        .Select(t => (DateTime?)t.DataOraInceput)
                        .FirstOrDefault();
                }

                Titlu("P9. Închidem ziua în care există un tur ghidat (trebuie respinsă)");
                if (urmatorulTur.HasValue)
                {
                    byte ziTur = ProgramService.ZiSaptamanaDin(urmatorulTur.Value);
                    Console.WriteLine($"  (încercăm să închidem {ProgramService.NumeZi(ziTur)}; tur pe {urmatorulTur:dd.MM.yyyy HH:mm})");
                    AsteaptaEroare(() => service.ModificaZi(ziTur, true, null, null));
                }
                else
                {
                    Console.WriteLine("  (sărit: nu există tururi viitoare în bază)");
                }

                Titlu("P10. Adăugare excepție: 01.01.2027 închis");
                int idExceptie = service.AdaugaExceptie(new ExceptiiProgram
                {
                    Data = new DateTime(2027, 1, 1),
                    Inchis = true,
                    Motiv = "Anul Nou"
                });
                exceptiiDeSters.Add(idExceptie);
                Afiseaza(new List<ProgramZiDto> { service.GetProgramPentruData(new DateTime(2027, 1, 1)) });

                Titlu("P11. Excepție duplicat + date greșite (trebuie respinse)");
                AsteaptaEroare(() => service.AdaugaExceptie(new ExceptiiProgram
                {
                    Data = new DateTime(2027, 1, 1),
                    Inchis = true,
                    Motiv = "Duplicat"
                }));
                AsteaptaEroare(() => service.AdaugaExceptie(new ExceptiiProgram
                {
                    Data = new DateTime(2025, 1, 1),
                    Inchis = false,
                    OraDeschidere = new TimeSpan(15, 0, 0),
                    OraInchidere = new TimeSpan(9, 0, 0),
                    Motiv = "  "
                }));

                Titlu("P12. Modificare excepție: 01.01.2027 program redus 12:00-16:00");
                service.ModificaExceptie(new ExceptiiProgram
                {
                    IdExceptie = idExceptie,
                    Data = new DateTime(2027, 1, 1),
                    Inchis = false,
                    OraDeschidere = new TimeSpan(12, 0, 0),
                    OraInchidere = new TimeSpan(16, 0, 0),
                    Motiv = "Anul Nou - program redus"
                });
                Afiseaza(service.GetExceptii());

                Titlu("P13. Excepție de închidere în ziua unui tur ghidat (trebuie respinsă)");
                if (urmatorulTur.HasValue)
                {
                    AsteaptaEroare(() => exceptiiDeSters.Add(service.AdaugaExceptie(new ExceptiiProgram
                    {
                        Data = urmatorulTur.Value.Date,
                        Inchis = true,
                        Motiv = "Test închidere"
                    })));
                }
                else
                {
                    Console.WriteLine("  (sărit: nu există tururi viitoare în bază)");
                }
            }
            finally
            {
                // Curățenie: ștergem excepțiile de test și refacem programul inițial.
                foreach (int id in exceptiiDeSters)
                    service.StergeExceptie(id);

                foreach (var zi in programInitial)
                    service.ModificaZi(zi.ZiSaptamana, zi.Inchis, zi.OraDeschidere, zi.OraInchidere);

                Console.WriteLine("\n  (programul inițial a fost refăcut, excepțiile de test au fost șterse)");
            }
        }

        // =====================================================================
        // TESTE AngajatService
        // =====================================================================
        static void TesteazaAngajati()
        {
            var service = new AngajatService();
            int idAngajatTest = 0;

            try
            {
                // ---------------- ANGAJAȚI ----------------
                Titlu("G1. Angajații activi");
                Afiseaza(service.GetAngajati());

                Titlu("G2. Doar ghizii");
                Afiseaza(service.GetAngajati(FunctiiAngajat.Ghid));

                Titlu("G3. Adăugare validă (email cu spații și majuscule, fără dată de angajare)");
                idAngajatTest = service.AdaugaAngajat(new Angajati
                {
                    Nume = "Test",
                    Prenume = "Ghid",
                    Functie = FunctiiAngajat.Ghid,
                    Email = "  Test.Ghid@Muzeu.ro ",
                    Telefon = "0722 000 999"
                });
                Afiseaza(service.GetAngajati().Where(a => a.IdAngajat == idAngajatTest).ToList());

                Titlu("G4. Adăugare cu date greșite (trebuie respinsă)");
                AsteaptaEroare(() => service.AdaugaAngajat(new Angajati
                {
                    Nume = "",
                    Prenume = "X",
                    Functie = "Director",                   // nu e o funcție permisă
                    Email = "maria.ionescu@muzeu.ro",       // folosit deja de Ionescu Maria
                    Telefon = "abc",
                    DataAngajarii = DateTime.Today.AddDays(10)
                }));

                Titlu("G5. Modificare telefon");
                var deModificat = service.GetAngajatById(idAngajatTest);
                deModificat.Telefon = "0733 111 222";
                service.ModificaAngajat(deModificat);
                Afiseaza(service.GetAngajati().Where(a => a.IdAngajat == idAngajatTest).ToList());

                Titlu("G6. Schimbarea funcției pentru curatorul Ionescu Maria (trebuie respinsă)");
                var curator = service.GetAngajatById(2);
                curator.Functie = FunctiiAngajat.Ghid;
                AsteaptaEroare(() => service.ModificaAngajat(curator));

                Titlu("G7. Dezactivarea lui Ionescu Maria (trebuie respinsă)");
                AsteaptaEroare(() => service.DezactiveazaAngajat(2));

                Titlu("G8. Ștergerea lui Ionescu Maria (trebuie respinsă: are istoric)");
                AsteaptaEroare(() => service.StergeAngajat(2));

                // ---------------- CONTURI ----------------
                Titlu("U1. Primul cont de Admin");
                bool existau = service.ExistaUtilizatori();
                Console.WriteLine($"  Existau conturi înainte de test: {existau}");
                int idAdmin = existau
                    ? service.CreeazaCont("test_admin", "Admin123", RoluriUtilizator.Admin, null)
                    : service.CreeazaPrimulAdmin("test_admin", "Admin123");
                Console.WriteLine($"  OK: test_admin creat cu Id = {idAdmin}");
                Console.WriteLine("  Încă un 'prim admin' (trebuie respins):");
                AsteaptaEroare(() => service.CreeazaPrimulAdmin("test_altul", "Admin123"));

                Titlu("U2. Cont pentru angajatul de test");
                int idContGhid = service.CreeazaCont("Test_Ghid", "Ghid2026", RoluriUtilizator.Ghid, idAngajatTest);
                Console.WriteLine($"  OK: cont creat cu Id = {idContGhid}");

                Titlu("U3. Conturi cu date greșite (trebuie respinse)");
                AsteaptaEroare(() => service.CreeazaCont("a b", "123", "Sef", 999));
                AsteaptaEroare(() => service.CreeazaCont("TEST_ADMIN", "Parola1", RoluriUtilizator.Casier, idAngajatTest));

                Titlu("U4. Lista conturilor");
                Afiseaza(service.GetUtilizatori());

                Titlu("U5. Login corect (nume scris cu majuscule și spațiu)");
                service.Login(" Test_Ghid", "Ghid2026");
                AfiseazaSesiune();

                Titlu("U6. Login greșit (trebuie respins, cu același mesaj în ambele cazuri)");
                AsteaptaEroare(() => service.Login("test_ghid", "parolagresita1"));
                AsteaptaEroare(() => service.Login("nu_exista", "Ghid2026"));

                Titlu("U7. Schimbare parolă");
                AsteaptaEroare(() => service.SchimbaParola(idContGhid, "gresita1", "GhidNou2026"));
                service.SchimbaParola(idContGhid, "Ghid2026", "GhidNou2026");
                service.Login("test_ghid", "GhidNou2026");
                Console.WriteLine("  OK: login cu parola nouă");

                Titlu("U8. Resetare parolă de către Admin");
                service.ReseteazaParola(idContGhid, "Resetata99");
                service.Login("test_ghid", "Resetata99");
                Console.WriteLine("  OK: login cu parola resetată");

                Titlu("U9. Hash-ul salvat în baza de date pentru parola 'admin123'");
                Console.WriteLine("  " + AngajatService.CalculeazaHash("admin123"));

                Titlu("U10. Dezactivarea angajatului de test (îi dezactivează și contul)");
                service.DezactiveazaAngajat(idAngajatTest);
                Afiseaza(service.GetAngajati(includeInactivi: true).Where(a => a.IdAngajat == idAngajatTest).ToList());
                AsteaptaEroare(() => service.Login("test_ghid", "Resetata99"));
                Console.WriteLine("  Reactivare cont cât timp angajatul e inactiv (trebuie respinsă):");
                AsteaptaEroare(() => service.ReactiveazaCont(idContGhid));
                service.ReactiveazaAngajat(idAngajatTest);
                service.ReactiveazaCont(idContGhid);
                service.Login("test_ghid", "Resetata99");
                Console.WriteLine("  OK: după reactivare, login-ul merge din nou");

                Titlu("U11. Ultimul Admin activ");
                int adminiActivi = service.GetUtilizatori().Count(u => u.Rol == RoluriUtilizator.Admin && u.Activ);
                if (adminiActivi == 1)
                {
                    AsteaptaEroare(() => service.DezactiveazaCont(idAdmin));
                    AsteaptaEroare(() => service.SchimbaRol(idAdmin, RoluriUtilizator.Casier));
                }
                else
                {
                    Console.WriteLine($"  (sărit: există {adminiActivi} conturi de Admin active)");
                }

                Titlu("U12. Delogare");
                service.Delogare();
                AfiseazaSesiune();
            }
            finally
            {
                StergeDateTestAngajati(idAngajatTest);
                Console.WriteLine("\n  (conturile test_* și angajatul de test au fost șterse din bază)");
            }
        }

        /// <summary>Curățenie după test: conturile test_* și angajatul de test, direct din bază.</summary>
        static void StergeDateTestAngajati(int idAngajat)
        {
            new AngajatService().Delogare();

            using (var db = DbFactory.Create())
            {
                db.Utilizatori.DeleteAllOnSubmit(db.Utilizatori.Where(u => u.NumeUtilizator.StartsWith("test_")));
                db.SubmitChanges();

                var angajat = db.Angajati.SingleOrDefault(a => a.IdAngajat == idAngajat);
                if (angajat != null)
                {
                    db.Angajati.DeleteOnSubmit(angajat);
                    db.SubmitChanges();
                }
            }
        }

        static void Afiseaza(List<AngajatDto> angajati)
        {
            foreach (var a in angajati)
                Console.WriteLine(
                    $"  [{a.IdAngajat}] {a.NumeComplet} | {a.Functie} | {a.Email ?? "-"} | {a.Telefon ?? "-"} | " +
                    $"din {a.DataAngajarii:dd.MM.yyyy} | {(a.Activ ? "activ" : "INACTIV")} | cont: {a.NumeUtilizator ?? "-"}");

            if (angajati.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(List<UtilizatorDto> utilizatori)
        {
            foreach (var u in utilizatori)
                Console.WriteLine(
                    $"  [{u.IdUtilizator}] {u.NumeUtilizator} | {u.Rol} | {(u.Activ ? "activ" : "INACTIV")} | " +
                    $"angajat: {u.NumeAngajat ?? "-"} | ultima autentificare: " +
                    (u.UltimaAutentificare.HasValue ? u.UltimaAutentificare.Value.ToString("dd.MM.yyyy HH:mm") : "niciodată"));

            if (utilizatori.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void AfiseazaSesiune()
        {
            if (!Sesiune.EsteAutentificat)
            {
                Console.WriteLine("  Nimeni nu e logat.");
                return;
            }

            var u = Sesiune.UtilizatorCurent;
            Console.WriteLine($"  Logat: {u.NumeAfisat} ({u.NumeUtilizator}), rol {u.Rol}");
            Console.WriteLine($"  AreRol(Ghid) = {Sesiune.AreRol(RoluriUtilizator.Ghid)}, " +
                              $"AreRol(Admin) = {Sesiune.AreRol(RoluriUtilizator.Admin)}");
        }

        // =====================================================================
        // TESTE TurService
        // =====================================================================
        static void TesteazaTururi()
        {
            var service = new TurService();
            var programService = new ProgramService();
            var tururiTest = new List<int>();
            var viziteTest = new List<int>();

            // O zi de marți (program 10:00-18:00) peste cel puțin o săptămână, care nu e excepție.
            // Calculată față de azi, ca testul să meargă oricând e rulat.
            DateTime zi = DateTime.Today.AddDays(7);
            while (zi.DayOfWeek != DayOfWeek.Tuesday || programService.GetProgramPentruData(zi).EsteExceptie)
                zi = zi.AddDays(1);

            const int idGhid = 3;   // Dumitru Elena

            try
            {
                Titlu("T1. Tururi viitoare");
                var viitoare = service.GetTururiViitoare();
                Afiseaza(viitoare);

                Titlu("T2. Participanții la primul tur viitor");
                if (viitoare.Count > 0)
                    Afiseaza(service.GetParticipanti(viitoare[0].IdTur));
                else
                    Console.WriteLine("  (nu există tururi viitoare)");

                Titlu($"T3. Liste pentru formular (ziua de test: marți {zi:dd.MM.yyyy})");
                Console.WriteLine("  Limbi:              " + string.Join(", ", TurService.LimbiUzuale));
                Console.WriteLine("  Expoziții deschise: " + string.Join(", ", service.GetExpozitiiDeschise(zi)));
                Console.WriteLine("  Ghizi liberi 11:00: " + string.Join(", ", service.GetGhiziDisponibili(zi.AddHours(11), 60)));

                Titlu("T4. Adăugare validă: marți 11:00, 60 min, maximum 10 persoane");
                int idTur = service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 1,
                    IdGhid = idGhid,
                    DataOraInceput = zi.AddHours(11),
                    DurataMinute = 60,
                    Limba = "Romana",
                    NrMaximParticipanti = 10,
                    PretSuplimentar = 15
                });
                tururiTest.Add(idTur);
                Afiseaza(service.GetDetalii(idTur));

                Titlu("T5. Adăugare cu date greșite (trebuie respinsă)");
                AsteaptaEroare(() => service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 99,                       // nu există
                    IdGhid = 2,                             // Ionescu Maria e curator, nu ghid
                    DataOraInceput = DateTime.Today.AddDays(-1).AddHours(11),
                    DurataMinute = 5,
                    Limba = " ",
                    NrMaximParticipanti = 0,
                    PretSuplimentar = -5
                }));

                Titlu("T6. Mai mulți participanți decât încap în sală (trebuie respinsă)");
                AsteaptaEroare(() => service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 2,                        // Sala de Arta Veche: 60 de locuri
                    IdGhid = idGhid,
                    DataOraInceput = zi.AddHours(15),
                    DurataMinute = 60,
                    Limba = "Engleza",
                    NrMaximParticipanti = 70
                }));

                Titlu("T7. În afara programului (trebuie respinse)");
                AsteaptaEroare(() => service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 1,
                    IdGhid = idGhid,
                    DataOraInceput = zi.AddHours(17.5),
                    DurataMinute = 60,
                    Limba = "Romana",
                    NrMaximParticipanti = 10      // 17:30-18:30, se închide la 18:00
                }));
                AsteaptaEroare(() => service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 1,
                    IdGhid = idGhid,
                    DataOraInceput = zi.AddDays(-1).AddHours(11),
                    DurataMinute = 60,
                    Limba = "Romana",
                    NrMaximParticipanti = 10      // luni: închis
                }));

                Titlu("T8. Ghid ocupat: alt tur la 11:30 cu același ghid (trebuie respinsă)");
                AsteaptaEroare(() => service.Adauga(new TururiGhidate
                {
                    IdExpozitie = 2,
                    IdGhid = idGhid,
                    DataOraInceput = zi.AddHours(11.5),
                    DurataMinute = 45,
                    Limba = "Engleza",
                    NrMaximParticipanti = 10
                }));
                Console.WriteLine("  Ghizi liberi la 11:30: " + string.Join(", ", service.GetGhiziDisponibili(zi.AddHours(11.5), 45)) +
                                  "  (așteptat: niciunul)");
                Console.WriteLine("  Ghizi liberi la 14:00: " + string.Join(", ", service.GetGhiziDisponibili(zi.AddHours(14), 45)));

                Titlu("T9. O rezervare de test de 8 persoane (adăugată direct în bază)");
                viziteTest.Add(AdaugaRezervareTest(idTur, zi.AddHours(11), 8));
                Afiseaza(service.GetDetalii(idTur));

                Titlu("T10. Modificare: maximum 5 persoane + altă expoziție (trebuie respinsă)");
                var deModificat = service.GetById(idTur);
                deModificat.NrMaximParticipanti = 5;
                deModificat.IdExpozitie = 2;
                AsteaptaEroare(() => service.Modifica(deModificat));

                Titlu("T11. Modificare: ora 14:00, maximum 12 persoane (rezervarea se mută și ea)");
                deModificat = service.GetById(idTur);
                deModificat.DataOraInceput = zi.AddHours(14);
                deModificat.NrMaximParticipanti = 12;
                service.Modifica(deModificat);
                Afiseaza(service.GetDetalii(idTur));
                Afiseaza(service.GetParticipanti(idTur));

                Titlu("T12. Anularea turului");
                int anulate = service.AnuleazaTur(idTur, "ghid indisponibil");
                Console.WriteLine($"  OK: {anulate} rezervare(ări) anulată(e)");
                using (var db = DbFactory.Create())
                {
                    var v = db.Vizite.Single(x => x.IdVizita == viziteTest[0]);
                    Console.WriteLine($"  Rezervarea {v.IdVizita}: status = {v.Status}, IdTur = {(v.IdTur?.ToString() ?? "null")}");
                    Console.WriteLine($"  Observații: {v.Observatii}");
                }

                Titlu("T13. Turul anulat nu mai există (trebuie respinsă)");
                AsteaptaEroare(() => service.GetDetalii(idTur));
            }
            finally
            {
                StergeDateTestTururi(tururiTest, viziteTest);
                Console.WriteLine("\n  (tururile și rezervările de test au fost șterse din bază)");
            }
        }

        /// <summary>Adaugă direct în bază o rezervare la un tur (până avem VanzareService). Doar pentru teste!</summary>
        static int AdaugaRezervareTest(int idTur, DateTime dataTur, int nrPersoane)
        {
            using (var db = DbFactory.Create())
            {
                var vizita = new Vizite
                {
                    IdVizitator = 1,
                    DataVizita = dataTur,
                    IdTur = idTur,
                    IdCasier = null,
                    Status = StatusuriVizita.Rezervata,
                    DataCreare = DateTime.Now,
                    Observatii = "rezervare de test"
                };
                db.Vizite.InsertOnSubmit(vizita);
                db.SubmitChanges();

                for (int i = 0; i < nrPersoane; i++)
                    db.Bilete.InsertOnSubmit(new Bilete { IdVizita = vizita.IdVizita, IdTipBilet = 1, PretAchitat = 45 });
                db.SubmitChanges();

                return vizita.IdVizita;
            }
        }

        static void StergeDateTestTururi(List<int> tururi, List<int> vizite)
        {
            using (var db = DbFactory.Create())
            {
                db.Bilete.DeleteAllOnSubmit(db.Bilete.Where(b => vizite.Contains(b.IdVizita)));
                db.SubmitChanges();

                db.Vizite.DeleteAllOnSubmit(db.Vizite.Where(v => vizite.Contains(v.IdVizita)));
                db.SubmitChanges();

                db.TururiGhidate.DeleteAllOnSubmit(db.TururiGhidate.Where(t => tururi.Contains(t.IdTur)));
                db.SubmitChanges();
            }
        }

        static void Afiseaza(List<TurDto> tururi)
        {
            foreach (var t in tururi)
                Afiseaza(t);

            if (tururi.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(TurDto t)
        {
            Console.WriteLine(
                $"  [{t.IdTur}] {t.Interval} | {t.Expozitie} | ghid: {t.Ghid} | {t.Limba} | " +
                $"{t.NrParticipanti}/{t.NrMaximParticipanti} ocupate, {t.LocuriLibere} libere | +{t.PretSuplimentar} lei");
        }

        static void Afiseaza(List<ParticipantTurDto> participanti)
        {
            foreach (var p in participanti)
                Console.WriteLine(
                    $"  vizita {p.IdVizita} | {p.Vizitator} | {p.Telefon ?? "-"} | {p.NrPersoane} pers. | " +
                    $"{p.Status} | data vizitei: {p.DataVizita:dd.MM.yyyy HH:mm}");

            if (participanti.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        // =====================================================================
        // TESTE VanzareService
        // =====================================================================
        static void TesteazaVanzari()
        {
            var service = new VanzareService();
            var programService = new ProgramService();

            var viziteTest = new List<int>();
            var feedbackTest = new List<int>();
            int idVizitatorTest = 0, idTurTest = 0, idTipTest = 0;

            // O zi de marți (10:00-18:00) peste cel puțin o săptămână, care nu e excepție.
            DateTime zi = DateTime.Today.AddDays(7);
            while (zi.DayOfWeek != DayOfWeek.Tuesday || programService.GetProgramPentruData(zi).EsteExceptie)
                zi = zi.AddDays(1);

            // O expoziție deschisă în ziua de test, cu supliment de preț (dacă există).
            var expozitieCuSupliment = new ExpozitieService().GetActive(zi).FirstOrDefault(e => e.PretSuplimentar > 0);

            try
            {
                // ---------------- VIZITATORI ----------------
                Titlu("V1. Căutare vizitatori după 'marin'");
                Afiseaza(service.CautaVizitatori("marin"));

                Titlu("V2. Adăugare vizitator");
                idVizitatorTest = service.AdaugaVizitator(new Vizitatori
                {
                    Nume = "Test",
                    Prenume = "Vizitator",
                    Email = " Test.Vizitator@Email.ro ",
                    Telefon = "0744 123 456"
                });
                Afiseaza(service.CautaVizitatori("test vizitator"));

                Titlu("V3. Vizitatori cu date greșite (trebuie respinși)");
                AsteaptaEroare(() => service.AdaugaVizitator(new Vizitatori
                {
                    Nume = "",
                    Prenume = "X",
                    Email = "ana.marin@email.ro",
                    Telefon = "12"
                }));
                AsteaptaEroare(() => service.AdaugaVizitator(new Vizitatori { Nume = "Fara", Prenume = "Contact" }));

                Titlu("V4. Modificare telefon");
                var vizitator = service.GetVizitatorById(idVizitatorTest);
                vizitator.Telefon = "0755 999 888";
                service.ModificaVizitator(vizitator);
                Afiseaza(service.CautaVizitatori("test vizitator"));

                // ---------------- TIPURI DE BILET ----------------
                Titlu("B1. Tipuri de bilet active");
                foreach (var t in service.GetTipuriBilet())
                    Console.WriteLine($"  [{t.IdTipBilet}] {t.Afisare}");

                Titlu("B2. Tip nou + tipuri greșite (duplicat, preț negativ)");
                idTipTest = service.AdaugaTipBilet("Test Grup", 20);
                Console.WriteLine($"  OK: tip nou cu Id = {idTipTest}");
                AsteaptaEroare(() => service.AdaugaTipBilet("Adult", 25));
                AsteaptaEroare(() => service.AdaugaTipBilet("Gratuit special", -1));

                Titlu("B3. Tip dezactivat nu mai poate fi vândut (trebuie respinsă)");
                service.SeteazaTipBiletActiv(idTipTest, false);
                AsteaptaEroare(() => service.CalculeazaPret(new CerereVizita().AdaugaBilete(idTipTest, 1)));

                // ---------------- PREȚ ----------------
                Titlu($"P1. Calcul preț pentru marți {zi:dd.MM.yyyy}");
                var cererePret = new CerereVizita { DataVizita = zi.AddHours(12) }
                    .AdaugaBilete(1, 2)          // 2 x Adult, acces general
                    .AdaugaBilete(2, 1);         // 1 x Elev/Student
                if (expozitieCuSupliment != null)
                    cererePret.AdaugaBilete(1, 1, expozitieCuSupliment.IdExpozitie);   // 1 x Adult cu expoziția temporară
                Afiseaza(service.CalculeazaPret(cererePret));

                // ---------------- VÂNZARE LA CASĂ ----------------
                Titlu("C1. Vânzare la casă acum (vizitator anonim)");
                var cerereCasa = new CerereVizita().AdaugaBilete(1, 1).AdaugaBilete(4, 1);   // Adult + Copil
                if (programService.EsteDeschis(DateTime.Now))
                {
                    int idCasa = service.VindeLaCasa(cerereCasa);
                    viziteTest.Add(idCasa);
                    Afiseaza(service.GetDetalii(idCasa));
                }
                else
                {
                    Console.WriteLine("  Muzeul e închis acum, deci vânzarea trebuie respinsă:");
                    AsteaptaEroare(() => viziteTest.Add(service.VindeLaCasa(cerereCasa)));
                }

                // ---------------- REZERVĂRI ----------------
                Titlu("R1. Rezervare fără tur: marți 12:00, 2 x Adult");
                int idRezervare = service.Rezerva(new CerereVizita
                {
                    IdVizitator = idVizitatorTest,
                    DataVizita = zi.AddHours(12),
                    Observatii = "rezervare de test"
                }.AdaugaBilete(1, 2));
                viziteTest.Add(idRezervare);
                Afiseaza(service.GetDetalii(idRezervare));

                Titlu("R2. Rezervări greșite (trebuie respinse)");
                AsteaptaEroare(() => service.Rezerva(new CerereVizita
                {
                    IdVizitator = null,
                    DataVizita = DateTime.Today.AddDays(-1).AddHours(12)
                }.AdaugaBilete(99, 1).AdaugaBilete(1, 0).AdaugaBilete(1, 1, idExpozitie: 99)));
                AsteaptaEroare(() => service.Rezerva(new CerereVizita
                {
                    IdVizitator = idVizitatorTest,
                    DataVizita = zi.AddDays(-1).AddHours(12)   // luni: închis
                }.AdaugaBilete(1, 1)));

                Titlu("R3. Tur de test (5 locuri, supliment 20 lei) + rezervare 3 x Adult și 1 x Student");
                idTurTest = new TurService().Adauga(new TururiGhidate
                {
                    IdExpozitie = 1,
                    IdGhid = 3,
                    DataOraInceput = zi.AddHours(11),
                    DurataMinute = 60,
                    Limba = "Romana",
                    NrMaximParticipanti = 5,
                    PretSuplimentar = 20
                });
                int idRezervareTur = service.Rezerva(new CerereVizita { IdVizitator = idVizitatorTest, IdTur = idTurTest }
                    .AdaugaBilete(1, 3)
                    .AdaugaBilete(2, 1));
                viziteTest.Add(idRezervareTur);
                Afiseaza(service.GetDetalii(idRezervareTur));
                Console.WriteLine($"  Locuri libere la tur: {new TurService().LocuriLibere(idTurTest)}  (așteptat 1)");

                Titlu("R4. Încă 2 locuri la tur (trebuie respinsă: a rămas doar 1)");
                AsteaptaEroare(() => viziteTest.Add(service.Rezerva(
                    new CerereVizita { IdVizitator = idVizitatorTest, IdTur = idTurTest }.AdaugaBilete(1, 2))));

                // ---------------- STĂRI ----------------
                Titlu("S1. Confirmare rezervare + a doua confirmare (trebuie respinsă)");
                service.Confirma(idRezervare);
                Console.WriteLine($"  Status: {service.GetDetalii(idRezervare).Status}");
                AsteaptaEroare(() => service.Confirma(idRezervare));

                Titlu("S2. Marcare ca efectuată înainte de ziua vizitei (trebuie respinsă)");
                AsteaptaEroare(() => service.MarcheazaEfectuata(idRezervare));

                Titlu("S3. Anularea rezervării la tur (eliberează locurile)");
                service.Anuleaza(idRezervareTur, "vizitatorul nu mai poate veni");
                var anulata = service.GetDetalii(idRezervareTur);
                Console.WriteLine($"  Status: {anulata.Status} | Observații: {anulata.Observatii}");
                Console.WriteLine($"  Locuri libere la tur: {new TurService().LocuriLibere(idTurTest)}  (așteptat 5)");
                AsteaptaEroare(() => service.Anuleaza(idRezervareTur, null));

                Titlu($"S4. Rezervările pentru marți {zi:dd.MM.yyyy}");
                Afiseaza(service.GetRezervariPentruZi(zi));

                Titlu("S5. Istoricul vizitatorului de test");
                Afiseaza(service.GetVizite(idVizitator: idVizitatorTest));

                // ---------------- FEEDBACK ----------------
                Titlu("F1. Feedback pentru vizita 2 (efectuată), expoziția 2");
                feedbackTest.Add(service.AdaugaFeedback(2, 2, 4, "Picturi frumoase, iluminat bun."));
                Console.WriteLine("  OK");

                Titlu("F2. Feedback greșit (trebuie respins)");
                AsteaptaEroare(() => feedbackTest.Add(service.AdaugaFeedback(2, 2, 5, "Din nou")));    // duplicat
                AsteaptaEroare(() => feedbackTest.Add(service.AdaugaFeedback(3, null, 7, null)));      // vizită rezervată + notă 7

                Titlu("F3. Tot feedback-ul");
                foreach (var f in service.GetFeedback())
                    Console.WriteLine($"  {f.DataFeedback:dd.MM.yyyy} | {f.Stele} | {f.Expozitie} | {f.Vizitator} | {f.Comentariu ?? "-"}");
            }
            finally
            {
                StergeDateTestVanzari(viziteTest, feedbackTest, idVizitatorTest, idTurTest, idTipTest);
                Console.WriteLine("\n  (vizitele, feedback-ul, vizitatorul, turul și tipul de bilet de test au fost șterse)");
            }
        }

        static void StergeDateTestVanzari(List<int> vizite, List<int> feedback, int idVizitator, int idTur, int idTipBilet)
        {
            using (var db = DbFactory.Create())
            {
                db.Feedback.DeleteAllOnSubmit(db.Feedback.Where(f => feedback.Contains(f.IdFeedback) || vizite.Contains(f.IdVizita)));
                db.SubmitChanges();

                db.Bilete.DeleteAllOnSubmit(db.Bilete.Where(b => vizite.Contains(b.IdVizita)));
                db.SubmitChanges();

                db.Vizite.DeleteAllOnSubmit(db.Vizite.Where(v => vizite.Contains(v.IdVizita)));
                db.SubmitChanges();

                db.TururiGhidate.DeleteAllOnSubmit(db.TururiGhidate.Where(t => t.IdTur == idTur));
                db.Vizitatori.DeleteAllOnSubmit(db.Vizitatori.Where(z => z.IdVizitator == idVizitator));
                db.TipuriBilet.DeleteAllOnSubmit(db.TipuriBilet.Where(t => t.IdTipBilet == idTipBilet));
                db.SubmitChanges();
            }
        }

        static void Afiseaza(List<VizitatorDto> vizitatori)
        {
            foreach (var z in vizitatori)
                Console.WriteLine(
                    $"  [{z.IdVizitator}] {z.NumeComplet} | {z.Email ?? "-"} | {z.Telefon ?? "-"} | " +
                    $"înregistrat {z.DataInregistrare:dd.MM.yyyy} | {z.NrVizite} vizite");

            if (vizitatori.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(CalculPretDto calcul)
        {
            foreach (var l in calcul.Linii)
                Console.WriteLine(
                    $"  {l.Cantitate} x {l.TipBilet} | {l.Expozitie} | {l.PretBaza} + {l.SuplimentExpozitie} + {l.SuplimentTur} " +
                    $"= {l.PretUnitar} lei/buc | {l.Total} lei");
            Console.WriteLine($"  TOTAL: {calcul.NrBilete} bilete, {calcul.Total} lei");
        }

        static void Afiseaza(VizitaDto v)
        {
            Console.WriteLine(
                $"  vizita {v.IdVizita} | {v.DataVizita:dd.MM.yyyy HH:mm} | {v.Vizitator} | {v.Status} | " +
                $"{(v.Tur != null ? "tur: " + v.Tur + " | " : "")}{v.NrBilete} bilete, {v.Total} lei" +
                $"{(v.Casier != null ? " | casier: " + v.Casier : "")}");

            if (v.Bilete != null)
                foreach (var b in v.Bilete)
                    Console.WriteLine($"      bilet {b.IdBilet}: {b.TipBilet} | {b.Expozitie} | {b.PretAchitat} lei");
        }

        static void Afiseaza(List<VizitaDto> vizite)
        {
            foreach (var v in vizite)
                Afiseaza(v);

            if (vizite.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(List<ProgramZiDto> zile)
        {
            foreach (var z in zile)
                Console.WriteLine(
                    $"  {z.NumeZi,-9} {(z.Data.HasValue ? z.Data.Value.ToString("dd.MM.yyyy") : ""),-10} {z.Ore,-13}" +
                    (z.EsteExceptie ? $" (excepție: {z.Motiv})" : ""));

            if (zile.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        /// <summary>
        /// Curățenie după test: șterge direct din bază artefactul de test cu tot istoricul lui.
        /// Doar pentru teste! În aplicație ștergerea trece prin ArtefactService.Sterge.
        /// </summary>
        static void StergeArtefactDeTest(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                db.IstoricLocatii.DeleteAllOnSubmit(db.IstoricLocatii.Where(i => i.IdArtefact == idArtefact));
                db.Restaurari.DeleteAllOnSubmit(db.Restaurari.Where(r => r.IdArtefact == idArtefact));
                db.SubmitChanges();

                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact != null)
                {
                    db.Artefacte.DeleteOnSubmit(artefact);
                    db.SubmitChanges();
                }
            }
        }

        // =====================================================================
        // AFIȘARE
        // =====================================================================
        static void Titlu(string text)
        {
            Console.WriteLine();
            Console.WriteLine("=== " + text + " ===");
        }

        static void Afiseaza(List<ExpozitieDto> expozitii)
        {
            foreach (var e in expozitii)
                Console.WriteLine(
                    $"  [{e.IdExpozitie}] {e.Titlu} | {e.Tip} | {e.Status} | {e.Perioada} | " +
                    $"{e.Sala} | curator: {e.Curator ?? "-"} | {e.NrArtefacte} artefacte");

            if (expozitii.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(List<ArtefactDto> artefacte)
        {
            foreach (var a in artefacte)
                Afiseaza(a);

            if (artefacte.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        static void Afiseaza(ArtefactDto a)
        {
            Console.WriteLine(
                $"  {a.CodInventar} | {a.Denumire} | {a.Categorie} | {a.StareConservare} | {a.Locatie}" +
                (a.InRestaurare ? " | ÎN RESTAURARE" : ""));
        }

        static void Afiseaza(List<RestaurareDto> restaurari)
        {
            foreach (var r in restaurari)
                Console.WriteLine(
                    $"  [{r.IdRestaurare}] {r.CodInventar} {r.Artefact} | {r.Perioada} | " +
                    $"{r.Restaurator ?? "-"} | cost: {(r.Cost.HasValue ? r.Cost.Value + " lei" : "-")} | {r.Descriere}");

            if (restaurari.Count == 0)
                Console.WriteLine("  (niciun rezultat)");
        }

        /// <summary>Rulează o acțiune care TREBUIE să arunce MuzeuException și afișează mesajul.</summary>
        static void AsteaptaEroare(Action actiune)
        {
            try
            {
                actiune();
                Console.WriteLine("  PROBLEMĂ: operația trebuia respinsă, dar a reușit!");
            }
            catch (MuzeuException ex)
            {
                Console.WriteLine("  OK, respinsă cu mesajul:");
                foreach (var eroare in ex.Erori)
                    Console.WriteLine("    - " + eroare);
            }
        }
    }
}
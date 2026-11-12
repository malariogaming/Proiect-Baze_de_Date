using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Console.OutputEncoding = Encoding.UTF8;   // pentru diacritice

            TesteazaExpozitii();
            TesteazaArtefacte();

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

        /// <summary>
        /// Curatenie dupa test
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
        // AFISARE
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

        /// <summary>Ruleaza o actiune care TREBUIE sa arunce MuzeuException și afiseaza mesajul.</summary>
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
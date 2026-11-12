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
            Console.OutputEncoding = Encoding.UTF8; // pentru diacritice

            TesteazaExpozitii();

            Console.WriteLine("\nGata. Apasa o tasta...");
            Console.ReadKey();
        }

        // =====================================================================
        // TESTE ExpozitieService
        // =====================================================================
        static void TesteazaExpozitii()
        {
            var service = new ExpozitieService();

            Titlu("1. Toate expozitiile");
            Afiseaza(service.GetAll());

            Titlu("2. Active azi");
            Afiseaza(service.GetActive(DateTime.Today));

            Titlu("3. Cautare dupa 'pictura'");
            Afiseaza(service.Cauta("pictura"));

            Titlu("4. Liste pentru ComboBox");
            Console.WriteLine("  Sali:     " + string.Join(", ", service.GetSali()));
            Console.WriteLine("  Curatori: " + string.Join(", ", service.GetCuratori()));

            Titlu("5. Adaugare valida");
            var noua = new Expozitii
            {
                Titlu = "Icoane pe sticla din Transilvania",
                Descriere = "Expozitie temporara de icoane pe sticla din sec. XVIII-XIX.",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2027, 2, 10),
                DataSfarsit = new DateTime(2027, 4, 30),
                IdSala = 3,
                IdCurator = 2,
                PretSuplimentar = 10
            };
            int idNou = service.Adauga(noua);
            Console.WriteLine($"  OK: adaugata cu Id = {idNou}");

            Titlu("6. Adaugare cu date gresite (trebuie respinsa)");
            AsteaptaEroare(() => service.Adauga(new Expozitii
            {
                Titlu = "   ",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2026, 12, 1),
                DataSfarsit = new DateTime(2026, 11, 1),
                IdSala = 3
            }));

            Titlu("7. Adaugare într-o sala ocupata (trebuie respinsa)");
            AsteaptaEroare(() => service.Adauga(new Expozitii
            {
                Titlu = "Expozitie suprapusa",
                Tip = TipuriExpozitie.Temporara,
                DataInceput = new DateTime(2026, 12, 1),
                DataSfarsit = new DateTime(2026, 12, 20),
                IdSala = 3              // ocupata de "Ceramica de Cucuteni" până la 31.01.2027
            }));

            Titlu("8. Modificare");
            var deModificat = service.GetById(idNou);
            deModificat.PretSuplimentar = 12;
            service.Modifica(deModificat);
            Console.WriteLine($"  OK: preț nou = {service.GetById(idNou).PretSuplimentar} lei");

            Titlu("9. Stergerea unei expoziții cu artefacte (trebuie respinsa)");
            AsteaptaEroare(() => service.Sterge(1));

            Titlu("10. Stergerea expoziției adaugate la pasul 5");
            service.Sterge(idNou);
            Console.WriteLine("  OK: ștearsa");
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

        /// <summary>Ruleaza o actiune care TREBUIE sa arunce MuzeuException și afiseaza mesajul.</summary>
        static void AsteaptaEroare(Action actiune)
        {
            try
            {
                actiune();
                Console.WriteLine("  PROBLEMA: operatia trebuia respinsa, dar a reusit!");
            }
            catch (MuzeuException ex)
            {
                Console.WriteLine("  OK, respinsa cu mesajul:");
                foreach (var eroare in ex.Erori)
                    Console.WriteLine("    - " + eroare);
            }
        }
    }
}
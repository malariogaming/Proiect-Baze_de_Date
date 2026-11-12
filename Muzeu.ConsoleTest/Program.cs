using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend;

namespace Muzeu.ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (var db = DbFactory.Create())
            {
                Console.WriteLine("Expozitii:");
                foreach (var e in db.Expozitii)
                    Console.WriteLine($"  - {e.Titlu} ({e.Tip})");

                Console.WriteLine($"\nNumar artefacte: {db.Artefacte.Count()}");

                Console.WriteLine("\nVenituri oct-nov 2026:");
                foreach (var r in db.sp_VenituriPerioada(new DateTime(2026, 10, 1), new DateTime(2026, 11, 30)))
                    Console.WriteLine($"  {r.Zi:dd.MM.yyyy}: {r.NrBilete} bilete, {r.Venit} lei");

                Console.WriteLine($"\nDeschis marti 17.11.2026 la 12:00? " +
                                  $"{db.fn_EsteDeschis(new DateTime(2026, 11, 17, 12, 0, 0))}");
            }

            Console.ReadKey();
        }
    }
}
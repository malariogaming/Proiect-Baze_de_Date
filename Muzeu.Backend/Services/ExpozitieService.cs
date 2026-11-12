using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{
    /// <summary>
    /// Toate operațiile pe expoziții. Interfața (WPF) apelează doar metodele de aici,
    /// nu lucrează direct cu DataContext-ul.
    ///
    /// Reguli respectate în fiecare metodă:
    ///  - DataContext-ul se deschide cu DbFactory.Create() și se închide imediat (using);
    ///  - la citire se întorc DTO-uri sau entități fără proprietăți de navigare folosite;
    ///  - orice încălcare a unei reguli => MuzeuException cu mesaj pentru utilizator.
    /// </summary>
    public class ExpozitieService
    {
        // =====================================================================
        // CITIRE
        // =====================================================================

        public List<ExpozitieDto> GetAll()
        {
            using (var db = DbFactory.Create())
            {
                return ProiecteazaDto(db, db.Expozitii)
                    .OrderBy(e => e.DataInceput)
                    .ToList();
            }
        }

        public List<ExpozitieDto> GetActive(DateTime data)
        {
            DateTime zi = data.Date;

            using (var db = DbFactory.Create())
            {
                var active = db.Expozitii.Where(e =>
                    e.DataInceput <= zi &&
                    (e.DataSfarsit == null || e.DataSfarsit >= zi));

                return ProiecteazaDto(db, active)
                    .OrderBy(e => e.Titlu)
                    .ToList();
            }
        }

        public List<ExpozitieDto> Cauta(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return GetAll();

            string t = text.Trim();

            using (var db = DbFactory.Create())
            {
                var gasite = db.Expozitii.Where(e =>
                    e.Titlu.Contains(t) ||
                    (e.Descriere != null && e.Descriere.Contains(t)));

                return ProiecteazaDto(db, gasite)
                    .OrderBy(e => e.Titlu)
                    .ToList();
            }
        }

        public Expozitii GetById(int idExpozitie)
        {
            using (var db = DbFactory.Create())
            {
                var expozitie = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == idExpozitie);
                if (expozitie == null)
                    throw new MuzeuException("Expoziția căutată nu există.");

                return expozitie;
            }
        }

        public List<ItemLista> GetSali()
        {
            using (var db = DbFactory.Create())
            {
                return db.Sali
                    .OrderBy(s => s.Denumire)
                    .Select(s => new ItemLista
                    {
                        Id = s.IdSala,
                        Denumire = s.Denumire + " (etaj " + s.Etaj + ")"
                    })
                    .ToList();
            }
        }

        public List<ItemLista> GetCuratori()
        {
            using (var db = DbFactory.Create())
            {
                return db.Angajati
                    .Where(a => a.Functie == FunctiiAngajat.Curator && a.Activ)
                    .OrderBy(a => a.Nume)
                    .Select(a => new ItemLista
                    {
                        Id = a.IdAngajat,
                        Denumire = a.Nume + " " + a.Prenume
                    })
                    .ToList();
            }
        }

        // =====================================================================
        // SCRIERE
        // =====================================================================

        public int Adauga(Expozitii expozitie)
        {
            if (expozitie == null) throw new ArgumentNullException(nameof(expozitie));

            using (var db = DbFactory.Create())
            {
                Normalizeaza(expozitie);
                Valideaza(db, expozitie);

                db.Expozitii.InsertOnSubmit(expozitie);
                db.SubmitChanges();

                return expozitie.IdExpozitie;
            }
        }

        public void Modifica(Expozitii expozitie)
        {
            if (expozitie == null) throw new ArgumentNullException(nameof(expozitie));

            using (var db = DbFactory.Create())
            {

                var existenta = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == expozitie.IdExpozitie);
                if (existenta == null)
                    throw new MuzeuException("Expoziția nu mai există în baza de date.");

                Normalizeaza(expozitie);
                Valideaza(db, expozitie);

                existenta.Titlu = expozitie.Titlu;
                existenta.Descriere = expozitie.Descriere;
                existenta.Tip = expozitie.Tip;
                existenta.DataInceput = expozitie.DataInceput;
                existenta.DataSfarsit = expozitie.DataSfarsit;
                existenta.IdSala = expozitie.IdSala;
                existenta.IdCurator = expozitie.IdCurator;
                existenta.PretSuplimentar = expozitie.PretSuplimentar;

                db.SubmitChanges();
            }
        }

        public int Incheie(int idExpozitie, DateTime dataSfarsit)
        {
            DateTime zi = dataSfarsit.Date;
            DateTime ziuaUrmatoare = zi.AddDays(1);

            using (var db = DbFactory.Create())
            {
                var expozitie = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == idExpozitie);
                if (expozitie == null)
                    throw new MuzeuException("Expoziția nu există.");

                if (zi < expozitie.DataInceput)
                    throw new MuzeuException("Data de încheiere nu poate fi înaintea datei de început a expoziției.");

                if (db.TururiGhidate.Any(t => t.IdExpozitie == idExpozitie && t.DataOraInceput >= ziuaUrmatoare))
                    throw new MuzeuException("Există tururi ghidate programate după această dată. Anulați-le sau mutați-le mai întâi.");

                expozitie.DataSfarsit = zi;

                var artefacte = db.Artefacte.Where(a => a.IdExpozitie == idExpozitie).ToList();
                foreach (var artefact in artefacte)
                    artefact.IdExpozitie = null;   // null = depozit

                db.SubmitChanges();
                return artefacte.Count;
            }
        }

        public void Sterge(int idExpozitie)
        {
            using (var db = DbFactory.Create())
            {
                var expozitie = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == idExpozitie);
                if (expozitie == null)
                    throw new MuzeuException("Expoziția nu există.");

                if (db.Artefacte.Any(a => a.IdExpozitie == idExpozitie))
                    throw new MuzeuException(
                        "Expoziția are artefacte. Mutați-le în altă expoziție sau în depozit înainte de ștergere.");

                bool areIstoric =
                    db.Bilete.Any(b => b.IdExpozitie == idExpozitie) ||
                    db.TururiGhidate.Any(t => t.IdExpozitie == idExpozitie) ||
                    db.Feedback.Any(f => f.IdExpozitie == idExpozitie) ||
                    db.IstoricLocatii.Any(i => i.IdExpozitieVeche == idExpozitie || i.IdExpozitieNoua == idExpozitie);

                if (areIstoric)
                    throw new MuzeuException(
                        "Expoziția are deja bilete, tururi, feedback sau istoric de artefacte și nu poate fi ștearsă. " +
                        "Folosiți opțiunea de încheiere a expoziției.");

                db.Expozitii.DeleteOnSubmit(expozitie);
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // METODE AJUTATOARE (private)
        // =====================================================================

        private static IQueryable<ExpozitieDto> ProiecteazaDto(MuzeuDBDataContext db, IQueryable<Expozitii> sursa)
        {
            return from e in sursa
                   join s in db.Sali on e.IdSala equals s.IdSala
                   select new ExpozitieDto
                   {
                       IdExpozitie = e.IdExpozitie,
                       Titlu = e.Titlu,
                       Tip = e.Tip,
                       DataInceput = e.DataInceput,
                       DataSfarsit = e.DataSfarsit,
                       Sala = s.Denumire,
                       Curator = db.Angajati
                                   .Where(a => a.IdAngajat == e.IdCurator)
                                   .Select(a => a.Nume + " " + a.Prenume)
                                   .FirstOrDefault(),
                       PretSuplimentar = e.PretSuplimentar,
                       NrArtefacte = db.Artefacte.Count(a => a.IdExpozitie == e.IdExpozitie)
                   };
        }

        private static void Normalizeaza(Expozitii e)
        {
            e.Titlu = e.Titlu?.Trim();
            e.Descriere = string.IsNullOrWhiteSpace(e.Descriere) ? null : e.Descriere.Trim();
            e.DataInceput = e.DataInceput.Date;
            if (e.DataSfarsit.HasValue)
                e.DataSfarsit = e.DataSfarsit.Value.Date;
        }

        private static void Valideaza(MuzeuDBDataContext db, Expozitii e)
        {
            var erori = new List<string>();

            // --- campuri simple ---
            if (string.IsNullOrWhiteSpace(e.Titlu))
                erori.Add("Titlul este obligatoriu.");
            else if (e.Titlu.Length > 100)
                erori.Add("Titlul poate avea maximum 100 de caractere.");

            if (!TipuriExpozitie.Toate.Contains(e.Tip))
                erori.Add("Tipul expoziției nu este valid.");
            else if (e.Tip != TipuriExpozitie.Permanenta && !e.DataSfarsit.HasValue)
                erori.Add("Expozițiile temporare și itinerante trebuie să aibă o dată de sfârșit.");

            if (e.DataSfarsit.HasValue && e.DataSfarsit.Value < e.DataInceput)
                erori.Add("Data de sfârșit nu poate fi înaintea datei de început.");

            if (e.PretSuplimentar < 0)
                erori.Add("Prețul suplimentar nu poate fi negativ.");

            // --- legaturi cu alte tabele ---
            int idSala = e.IdSala;
            if (!db.Sali.Any(s => s.IdSala == idSala))
                erori.Add("Sala selectată nu există.");

            if (e.IdCurator.HasValue)
            {
                int idCurator = e.IdCurator.Value;
                bool curatorValid = db.Angajati.Any(a =>
                    a.IdAngajat == idCurator &&
                    a.Functie == FunctiiAngajat.Curator &&
                    a.Activ);

                if (!curatorValid)
                    erori.Add("Persoana selectată nu este un curator activ.");
            }

            if (erori.Count == 0)
            {
                int idExpozitie = e.IdExpozitie;
                DateTime inceput = e.DataInceput;

                var suprapuse = db.Expozitii.Where(x =>
                    x.IdSala == idSala &&
                    x.IdExpozitie != idExpozitie &&
                    (x.DataSfarsit == null || x.DataSfarsit >= inceput));

                if (e.DataSfarsit.HasValue)
                {
                    DateTime sfarsit = e.DataSfarsit.Value;
                    suprapuse = suprapuse.Where(x => x.DataInceput <= sfarsit);
                }

                string conflict = suprapuse.Select(x => x.Titlu).FirstOrDefault();
                if (conflict != null)
                    erori.Add($"Sala este deja ocupată în perioada aleasă de expoziția „{conflict}”.");
            }

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }
    }
}
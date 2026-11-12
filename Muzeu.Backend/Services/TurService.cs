using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{
    public class TurService
    {
        public const int DurataMinima = 15;
        public const int DurataMaxima = 240;

        public static readonly string[] LimbiUzuale = { "Romana", "Engleza", "Franceza", "Germana", "Italiana", "Spaniola" };

        // =====================================================================
        // CITIRE
        // =====================================================================

        public List<TurDto> GetTururi(DateTime de, DateTime pana, int? idExpozitie = null, int? idGhid = null,
                                      bool doarCuLocuriLibere = false)
        {
            DateTime start = de.Date;
            DateTime sfarsitExclusiv = pana.Date.AddDays(1);

            using (var db = DbFactory.Create())
            {
                IQueryable<TururiGhidate> q = db.TururiGhidate
                    .Where(t => t.DataOraInceput >= start && t.DataOraInceput < sfarsitExclusiv);

                if (idExpozitie.HasValue)
                {
                    int idE = idExpozitie.Value;
                    q = q.Where(t => t.IdExpozitie == idE);
                }

                if (idGhid.HasValue)
                {
                    int idG = idGhid.Value;
                    q = q.Where(t => t.IdGhid == idG);
                }

                var tururi = ProiecteazaDto(db, q).OrderBy(t => t.DataOraInceput).ToList();

                return doarCuLocuriLibere ? tururi.Where(t => t.LocuriLibere > 0).ToList() : tururi;
            }
        }
        public List<TurDto> GetTururiViitoare()
        {
            DateTime acum = DateTime.Now;

            using (var db = DbFactory.Create())
            {
                return ProiecteazaDto(db, db.TururiGhidate.Where(t => t.DataOraInceput > acum))
                    .OrderBy(t => t.DataOraInceput)
                    .ToList();
            }
        }
        public List<TurDto> GetTururiDisponibile(DateTime zi)
        {
            DateTime acum = DateTime.Now;
            return GetTururi(zi, zi, doarCuLocuriLibere: true)
                .Where(t => t.DataOraInceput > acum)
                .ToList();
        }

        public TurDto GetDetalii(int idTur)
        {
            using (var db = DbFactory.Create())
            {
                var tur = ProiecteazaDto(db, db.TururiGhidate.Where(t => t.IdTur == idTur)).SingleOrDefault();
                if (tur == null)
                    throw new MuzeuException("Turul ghidat nu există.");
                return tur;
            }
        }
        public TururiGhidate GetById(int idTur)
        {
            using (var db = DbFactory.Create())
            {
                var tur = db.TururiGhidate.SingleOrDefault(t => t.IdTur == idTur);
                if (tur == null)
                    throw new MuzeuException("Turul ghidat nu există.");
                return tur;
            }
        }
        public List<ParticipantTurDto> GetParticipanti(int idTur)
        {
            using (var db = DbFactory.Create())
            {
                return (from v in db.Vizite
                        where v.IdTur == idTur
                        join z in db.Vizitatori on v.IdVizitator equals (int?)z.IdVizitator into vizitatori
                        from z in vizitatori.DefaultIfEmpty()
                        orderby v.DataCreare
                        select new ParticipantTurDto
                        {
                            IdVizita = v.IdVizita,
                            Vizitator = z == null ? "Vizitator anonim" : z.Nume + " " + z.Prenume,
                            Telefon = z == null ? null : z.Telefon,
                            Email = z == null ? null : z.Email,
                            NrPersoane = db.Bilete.Count(b => b.IdVizita == v.IdVizita),
                            Status = v.Status,
                            DataVizita = v.DataVizita,
                            DataCreare = v.DataCreare
                        })
                       .ToList();
            }
        }

        public int LocuriLibere(int idTur)
        {
            return GetDetalii(idTur).LocuriLibere;
        }
        public List<ItemLista> GetGhiziDisponibili(DateTime inceput, int durataMinute, int idTurExclus = 0)
        {
            DateTime sfarsit = inceput.AddMinutes(durataMinute);

            using (var db = DbFactory.Create())
            {
                var ghizi = db.Angajati
                    .Where(a => a.Functie == FunctiiAngajat.Ghid && a.Activ)
                    .OrderBy(a => a.Nume)
                    .Select(a => new ItemLista { Id = a.IdAngajat, Denumire = a.Nume + " " + a.Prenume })
                    .ToList();

                return ghizi
                    .Where(g => GasesteSuprapunereGhid(db, g.Id, inceput, sfarsit, idTurExclus) == null)
                    .ToList();
            }
        }
        public List<ItemLista> GetExpozitiiDeschise(DateTime zi)
        {
            DateTime data = zi.Date;

            using (var db = DbFactory.Create())
            {
                return db.Expozitii
                    .Where(e => e.DataInceput <= data && (e.DataSfarsit == null || e.DataSfarsit >= data))
                    .OrderBy(e => e.Titlu)
                    .Select(e => new ItemLista { Id = e.IdExpozitie, Denumire = e.Titlu })
                    .ToList();
            }
        }

        // =====================================================================
        // SCRIERE
        // =====================================================================

        public int Adauga(TururiGhidate tur)
        {
            if (tur == null) throw new ArgumentNullException(nameof(tur));

            Normalizeaza(tur);

            using (var db = DbFactory.Create())
            {
                Valideaza(db, tur);

                db.TururiGhidate.InsertOnSubmit(tur);
                db.SubmitChanges();

                return tur.IdTur;
            }
        }

        public void Modifica(TururiGhidate tur)
        {
            if (tur == null) throw new ArgumentNullException(nameof(tur));

            Normalizeaza(tur);

            using (var db = DbFactory.Create())
            {
                var existent = db.TururiGhidate.SingleOrDefault(t => t.IdTur == tur.IdTur);
                if (existent == null)
                    throw new MuzeuException("Turul ghidat nu mai există în baza de date.");

                if (existent.DataOraInceput <= DateTime.Now)
                    throw new MuzeuException("Turul a avut deja loc și nu mai poate fi modificat.");

                Valideaza(db, tur);

                int participanti = NumaraParticipanti(db, existent.IdTur);
                var erori = new List<string>();

                if (participanti > 0 && tur.IdExpozitie != existent.IdExpozitie)
                    erori.Add("Turul are deja participanți, deci nu i se poate schimba expoziția. Anulați turul și creați altul.");

                if (tur.NrMaximParticipanti < participanti)
                    erori.Add($"Turul are deja {participanti} participanți, deci numărul maxim nu poate fi mai mic de atât.");

                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                bool oraSchimbata = existent.DataOraInceput != tur.DataOraInceput;

                existent.IdExpozitie = tur.IdExpozitie;
                existent.IdGhid = tur.IdGhid;
                existent.DataOraInceput = tur.DataOraInceput;
                existent.DurataMinute = tur.DurataMinute;
                existent.Limba = tur.Limba;
                existent.NrMaximParticipanti = tur.NrMaximParticipanti;
                existent.PretSuplimentar = tur.PretSuplimentar;

                if (oraSchimbata)
                {
                    int idTur = existent.IdTur;
                    foreach (var vizita in db.Vizite.Where(v => v.IdTur == idTur).ToList())
                        vizita.DataVizita = tur.DataOraInceput;
                }

                db.SubmitChanges();   // turul și rezervările, împreună
            }
        }

        public int AnuleazaTur(int idTur, string motiv)
        {
            using (var db = DbFactory.Create())
            {
                var tur = db.TururiGhidate.SingleOrDefault(t => t.IdTur == idTur);
                if (tur == null)
                    throw new MuzeuException("Turul ghidat nu există.");

                if (tur.DataOraInceput <= DateTime.Now)
                    throw new MuzeuException("Turul a avut deja loc și nu mai poate fi anulat.");

                string nota = $"Tur ghidat din {tur.DataOraInceput:dd.MM.yyyy HH:mm} anulat" +
                              (string.IsNullOrWhiteSpace(motiv) ? "." : $": {motiv.Trim()}");

                db.Connection.Open();
                using (var tranzactie = db.Connection.BeginTransaction())
                {
                    db.Transaction = tranzactie;

                    var vizite = db.Vizite.Where(v => v.IdTur == idTur).ToList();
                    int anulate = 0;

                    foreach (var vizita in vizite)
                    {
                        if (vizita.Status != StatusuriVizita.Anulata)
                        {
                            vizita.Status = StatusuriVizita.Anulata;
                            anulate++;
                        }

                        vizita.IdTur = null;
                        vizita.Observatii = Trunchiaza(
                            string.IsNullOrEmpty(vizita.Observatii) ? nota : vizita.Observatii + " | " + nota, 200);
                    }
                    db.SubmitChanges();

                    db.TururiGhidate.DeleteOnSubmit(tur);
                    db.SubmitChanges();

                    tranzactie.Commit();
                    return anulate;
                }
            }
        }

        // =====================================================================
        // METODE AJUTATOARE
        // =====================================================================

        internal static int NumaraParticipanti(MuzeuDBDataContext db, int idTur)
        {
            return db.Bilete.Count(b => db.Vizite.Any(v =>
                v.IdVizita == b.IdVizita && v.IdTur == idTur && v.Status != StatusuriVizita.Anulata));
        }

        private static IQueryable<TurDto> ProiecteazaDto(MuzeuDBDataContext db, IQueryable<TururiGhidate> sursa)
        {
            return from t in sursa
                   join e in db.Expozitii on t.IdExpozitie equals e.IdExpozitie
                   join g in db.Angajati on t.IdGhid equals g.IdAngajat
                   select new TurDto
                   {
                       IdTur = t.IdTur,
                       IdExpozitie = t.IdExpozitie,
                       Expozitie = e.Titlu,
                       IdGhid = t.IdGhid,
                       Ghid = g.Nume + " " + g.Prenume,
                       DataOraInceput = t.DataOraInceput,
                       DurataMinute = t.DurataMinute,
                       Limba = t.Limba,
                       NrMaximParticipanti = t.NrMaximParticipanti,
                       PretSuplimentar = t.PretSuplimentar,
                       NrParticipanti = db.Bilete.Count(b => db.Vizite.Any(v =>
                           v.IdVizita == b.IdVizita && v.IdTur == t.IdTur && v.Status != StatusuriVizita.Anulata))
                   };
        }
        private static string GasesteSuprapunereGhid(MuzeuDBDataContext db, int idGhid, DateTime inceput,
                                                     DateTime sfarsit, int idTurExclus)
        {
            // tururile nu trec de miezul nopții (programul muzeului), deci ajung cele din aceeași zi
            DateTime zi = inceput.Date;
            DateTime ziuaUrmatoare = zi.AddDays(1);

            var tururi = db.TururiGhidate
                .Where(t => t.IdGhid == idGhid && t.IdTur != idTurExclus &&
                            t.DataOraInceput >= zi && t.DataOraInceput < ziuaUrmatoare)
                .Select(t => new { t.DataOraInceput, t.DurataMinute })
                .ToList();

            // doua intervale se suprapun daca fiecare incepe inainte ca celalalt sa se termine
            var conflict = tururi.FirstOrDefault(t =>
                t.DataOraInceput < sfarsit && t.DataOraInceput.AddMinutes(t.DurataMinute) > inceput);

            if (conflict == null)
                return null;

            return $"Ghidul are deja un tur între {conflict.DataOraInceput:HH:mm} și " +
                   $"{conflict.DataOraInceput.AddMinutes(conflict.DurataMinute):HH:mm}.";
        }

        private static void Normalizeaza(TururiGhidate t)
        {
            t.Limba = t.Limba?.Trim();
            DateTime d = t.DataOraInceput;
            t.DataOraInceput = new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);   // fara secunde
        }

        private static void Valideaza(MuzeuDBDataContext db, TururiGhidate t)
        {
            var erori = new List<string>();

            // --- campuri simple ---
            if (t.DataOraInceput <= DateTime.Now)
                erori.Add("Turul trebuie programat în viitor.");

            if (t.DurataMinute < DurataMinima || t.DurataMinute > DurataMaxima)
                erori.Add($"Durata trebuie să fie între {DurataMinima} și {DurataMaxima} de minute.");

            if (string.IsNullOrEmpty(t.Limba))
                erori.Add("Limba turului este obligatorie.");
            else if (t.Limba.Length > 30)
                erori.Add("Limba poate avea maximum 30 de caractere.");

            if (t.NrMaximParticipanti < 1)
                erori.Add("Numărul maxim de participanți trebuie să fie cel puțin 1.");

            if (t.PretSuplimentar < 0)
                erori.Add("Prețul suplimentar nu poate fi negativ.");

            // --- expozitia: exista, e deschisa în ziua turului, sala are loc ---
            int idExpozitie = t.IdExpozitie;
            var expozitie = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == idExpozitie);

            if (expozitie == null)
            {
                erori.Add("Expoziția selectată nu există.");
            }
            else
            {
                DateTime zi = t.DataOraInceput.Date;
                if (expozitie.DataInceput > zi || (expozitie.DataSfarsit.HasValue && expozitie.DataSfarsit.Value < zi))
                    erori.Add($"Expoziția „{expozitie.Titlu}” nu este deschisă pe {zi:dd.MM.yyyy}.");

                int idSala = expozitie.IdSala;
                int capacitate = db.Sali.Where(s => s.IdSala == idSala).Select(s => s.CapacitateMaxima).Single();
                if (t.NrMaximParticipanti > capacitate)
                    erori.Add($"Numărul maxim de participanți depășește capacitatea sălii ({capacitate} persoane).");
            }

            // --- ghidul ---
            int idGhid = t.IdGhid;
            if (!db.Angajati.Any(a => a.IdAngajat == idGhid && a.Functie == FunctiiAngajat.Ghid && a.Activ))
                erori.Add("Persoana selectată nu este un ghid activ.");

            if (erori.Count > 0)
                throw new MuzeuException(erori);

            // --- verificarile care au sens doar daca restul datelor sunt corecte ---
            DateTime sfarsit = t.DataOraInceput.AddMinutes(t.DurataMinute);

            var programService = new ProgramService();
            if (!programService.EsteDeschisIntre(t.DataOraInceput, sfarsit))
            {
                var program = programService.GetProgramPentruData(t.DataOraInceput);
                erori.Add($"Turul ({t.DataOraInceput:HH:mm}-{sfarsit:HH:mm}) nu se încadrează în programul muzeului " +
                          $"din {program.NumeZi}, {t.DataOraInceput:dd.MM.yyyy}: {program.Ore}.");
            }

            string suprapunere = GasesteSuprapunereGhid(db, idGhid, t.DataOraInceput, sfarsit, t.IdTur);
            if (suprapunere != null)
                erori.Add(suprapunere);

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }

        private static string Trunchiaza(string text, int lungimeMaxima)
        {
            return text.Length <= lungimeMaxima ? text : text.Substring(0, lungimeMaxima);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{

    public class VanzareService
    {
        public const int MaxBileteVizita = 50;

        private static readonly Regex RegexEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex RegexTelefon = new Regex(@"^[0-9+ ]{6,20}$");

        private enum ModVanzare { LaCasa, Rezervare }

        // =====================================================================
        // VIZITATORI
        // =====================================================================
        public List<VizitatorDto> CautaVizitatori(string text)
        {
            using (var db = DbFactory.Create())
            {
                IQueryable<Vizitatori> q = db.Vizitatori;

                if (!string.IsNullOrWhiteSpace(text))
                {
                    string t = text.Trim();
                    q = q.Where(z =>
                        (z.Nume + " " + z.Prenume).Contains(t) ||
                        (z.Prenume + " " + z.Nume).Contains(t) ||
                        (z.Email != null && z.Email.Contains(t)) ||
                        (z.Telefon != null && z.Telefon.Contains(t)));
                }

                return q
                    .OrderBy(z => z.Nume).ThenBy(z => z.Prenume)
                    .Select(z => new VizitatorDto
                    {
                        IdVizitator = z.IdVizitator,
                        Nume = z.Nume,
                        Prenume = z.Prenume,
                        Email = z.Email,
                        Telefon = z.Telefon,
                        DataInregistrare = z.DataInregistrare,
                        NrVizite = db.Vizite.Count(v => v.IdVizitator == z.IdVizitator && v.Status != StatusuriVizita.Anulata)
                    })
                    .ToList();
            }
        }

        public Vizitatori GetVizitatorById(int idVizitator)
        {
            using (var db = DbFactory.Create())
            {
                var vizitator = db.Vizitatori.SingleOrDefault(z => z.IdVizitator == idVizitator);
                if (vizitator == null)
                    throw new MuzeuException("Vizitatorul căutat nu există.");
                return vizitator;
            }
        }

        public int AdaugaVizitator(Vizitatori vizitator)
        {
            if (vizitator == null) throw new ArgumentNullException(nameof(vizitator));

            NormalizeazaVizitator(vizitator);
            vizitator.DataInregistrare = DateTime.Now;   // DEFAULT-ul din SQL nu e folosit de LINQ to SQL

            using (var db = DbFactory.Create())
            {
                ValideazaVizitator(db, vizitator);

                db.Vizitatori.InsertOnSubmit(vizitator);
                db.SubmitChanges();
                return vizitator.IdVizitator;
            }
        }

        public void ModificaVizitator(Vizitatori vizitator)
        {
            if (vizitator == null) throw new ArgumentNullException(nameof(vizitator));

            NormalizeazaVizitator(vizitator);

            using (var db = DbFactory.Create())
            {
                var existent = db.Vizitatori.SingleOrDefault(z => z.IdVizitator == vizitator.IdVizitator);
                if (existent == null)
                    throw new MuzeuException("Vizitatorul nu mai există în baza de date.");

                ValideazaVizitator(db, vizitator);

                existent.Nume = vizitator.Nume;
                existent.Prenume = vizitator.Prenume;
                existent.Email = vizitator.Email;
                existent.Telefon = vizitator.Telefon;

                db.SubmitChanges();
            }
        }
        public void StergeVizitator(int idVizitator)
        {
            using (var db = DbFactory.Create())
            {
                var vizitator = db.Vizitatori.SingleOrDefault(z => z.IdVizitator == idVizitator);
                if (vizitator == null)
                    throw new MuzeuException("Vizitatorul nu există.");

                if (db.Vizite.Any(v => v.IdVizitator == idVizitator))
                    throw new MuzeuException("Vizitatorul are vizite înregistrate și nu poate fi șters.");

                db.Vizitatori.DeleteOnSubmit(vizitator);
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // TIPURI DE BILET
        // =====================================================================

        public List<TipBiletDto> GetTipuriBilet(bool includeInactive = false)
        {
            using (var db = DbFactory.Create())
            {
                IQueryable<TipuriBilet> q = db.TipuriBilet;
                if (!includeInactive)
                    q = q.Where(t => t.Activ);

                return q
                    .OrderByDescending(t => t.Pret).ThenBy(t => t.Denumire)
                    .Select(t => new TipBiletDto { IdTipBilet = t.IdTipBilet, Denumire = t.Denumire, Pret = t.Pret, Activ = t.Activ })
                    .ToList();
            }
        }

        public int AdaugaTipBilet(string denumire, decimal pret)
        {
            string d = denumire?.Trim();

            using (var db = DbFactory.Create())
            {
                ValideazaTipBilet(db, 0, d, pret);

                var tip = new TipuriBilet { Denumire = d, Pret = pret, Activ = true };
                db.TipuriBilet.InsertOnSubmit(tip);
                db.SubmitChanges();
                return tip.IdTipBilet;
            }
        }
        public void ModificaTipBilet(int idTipBilet, string denumire, decimal pret)
        {
            string d = denumire?.Trim();

            using (var db = DbFactory.Create())
            {
                var tip = db.TipuriBilet.SingleOrDefault(t => t.IdTipBilet == idTipBilet);
                if (tip == null)
                    throw new MuzeuException("Tipul de bilet nu există.");

                ValideazaTipBilet(db, idTipBilet, d, pret);

                tip.Denumire = d;
                tip.Pret = pret;
                db.SubmitChanges();
            }
        }
        public void SeteazaTipBiletActiv(int idTipBilet, bool activ)
        {
            using (var db = DbFactory.Create())
            {
                var tip = db.TipuriBilet.SingleOrDefault(t => t.IdTipBilet == idTipBilet);
                if (tip == null)
                    throw new MuzeuException("Tipul de bilet nu există.");

                if (!activ && tip.Activ && !db.TipuriBilet.Any(t => t.IdTipBilet != idTipBilet && t.Activ))
                    throw new MuzeuException("Trebuie să rămână cel puțin un tip de bilet activ.");

                tip.Activ = activ;
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // PREȚ, VANZARE, REZERVARE
        // =====================================================================

        public CalculPretDto CalculeazaPret(CerereVizita cerere)
        {
            if (cerere == null) throw new ArgumentNullException(nameof(cerere));

            using (var db = DbFactory.Create())
            {
                var erori = new List<string>();
                TururiGhidate tur = GasesteTur(db, cerere.IdTur);
                DateTime dataVizita = tur?.DataOraInceput ??
                                      (cerere.DataVizita == default(DateTime) ? DateTime.Now : cerere.DataVizita);

                var calcul = CalculeazaLinii(db, cerere.Bilete, tur, dataVizita, erori);

                if (erori.Count > 0)
                    throw new MuzeuException(erori);
                return calcul;
            }
        }

        public int VindeLaCasa(CerereVizita cerere)
        {
            return SalveazaVizita(cerere, ModVanzare.LaCasa);
        }
        public int Rezerva(CerereVizita cerere)
        {
            return SalveazaVizita(cerere, ModVanzare.Rezervare);
        }
        public void Confirma(int idVizita)
        {
            using (var db = DbFactory.Create())
            {
                var vizita = GasesteVizita(db, idVizita);

                if (vizita.Status != StatusuriVizita.Rezervata)
                    throw new MuzeuException($"Doar o rezervare poate fi confirmată (statusul actual: {vizita.Status}).");
                if (vizita.DataVizita <= DateTime.Now)
                    throw new MuzeuException("Momentul vizitei a trecut; rezervarea nu mai poate fi confirmată.");

                vizita.Status = StatusuriVizita.Confirmata;
                vizita.IdCasier = Sesiune.UtilizatorCurent?.IdAngajat;
                db.SubmitChanges();
            }
        }
        public void MarcheazaEfectuata(int idVizita)
        {
            using (var db = DbFactory.Create())
            {
                var vizita = GasesteVizita(db, idVizita);

                if (vizita.Status != StatusuriVizita.Rezervata && vizita.Status != StatusuriVizita.Confirmata)
                    throw new MuzeuException($"Vizita nu poate fi marcată ca efectuată (statusul actual: {vizita.Status}).");
                if (vizita.DataVizita.Date > DateTime.Today)
                    throw new MuzeuException($"Vizita este programată pe {vizita.DataVizita:dd.MM.yyyy}; nu poate fi marcată ca efectuată înainte de acea zi.");

                vizita.Status = StatusuriVizita.Efectuata;
                if (vizita.IdCasier == null)
                    vizita.IdCasier = Sesiune.UtilizatorCurent?.IdAngajat;
                db.SubmitChanges();
            }
        }
        public void Anuleaza(int idVizita, string motiv)
        {
            using (var db = DbFactory.Create())
            {
                var vizita = GasesteVizita(db, idVizita);

                if (vizita.Status != StatusuriVizita.Rezervata && vizita.Status != StatusuriVizita.Confirmata)
                    throw new MuzeuException($"Vizita nu poate fi anulată (statusul actual: {vizita.Status}).");
                if (vizita.DataVizita <= DateTime.Now)
                    throw new MuzeuException("Momentul vizitei a trecut; vizita nu mai poate fi anulată.");

                string nota = "Anulata" + (string.IsNullOrWhiteSpace(motiv) ? "" : $": {motiv.Trim()}");
                vizita.Status = StatusuriVizita.Anulata;
                vizita.Observatii = Trunchiaza(
                    string.IsNullOrEmpty(vizita.Observatii) ? nota : vizita.Observatii + " | " + nota, 200);

                db.SubmitChanges();
            }
        }

        // =====================================================================
        // CITIRE VIZITE
        // =====================================================================
        public List<VizitaDto> GetVizite(DateTime? de = null, DateTime? pana = null, string status = null, int? idVizitator = null)
        {
            using (var db = DbFactory.Create())
            {
                IQueryable<Vizite> q = db.Vizite;

                if (de.HasValue)
                {
                    DateTime start = de.Value.Date;
                    q = q.Where(v => v.DataVizita >= start);
                }
                if (pana.HasValue)
                {
                    DateTime sfarsit = pana.Value.Date.AddDays(1);
                    q = q.Where(v => v.DataVizita < sfarsit);
                }
                if (!string.IsNullOrWhiteSpace(status))
                {
                    string s = status;
                    q = q.Where(v => v.Status == s);
                }
                if (idVizitator.HasValue)
                {
                    int idZ = idVizitator.Value;
                    q = q.Where(v => v.IdVizitator == idZ);
                }

                return ProiecteazaVizite(db, q).OrderBy(v => v.DataVizita).ToList();
            }
        }
        public List<VizitaDto> GetRezervariPentruZi(DateTime zi)
        {
            return GetVizite(zi, zi)
                .Where(v => v.Status == StatusuriVizita.Rezervata || v.Status == StatusuriVizita.Confirmata)
                .ToList();
        }
        public VizitaDto GetDetalii(int idVizita)
        {
            using (var db = DbFactory.Create())
            {
                var vizita = ProiecteazaVizite(db, db.Vizite.Where(v => v.IdVizita == idVizita)).SingleOrDefault();
                if (vizita == null)
                    throw new MuzeuException("Vizita nu există.");

                vizita.Bilete = (from b in db.Bilete
                                 where b.IdVizita == idVizita
                                 join t in db.TipuriBilet on b.IdTipBilet equals t.IdTipBilet
                                 join e in db.Expozitii on b.IdExpozitie equals (int?)e.IdExpozitie into expozitii
                                 from e in expozitii.DefaultIfEmpty()
                                 orderby b.IdBilet
                                 select new BiletDto
                                 {
                                     IdBilet = b.IdBilet,
                                     TipBilet = t.Denumire,
                                     Expozitie = e == null ? "Acces general" : e.Titlu,
                                     PretAchitat = b.PretAchitat
                                 })
                                .ToList();

                return vizita;
            }
        }

        // =====================================================================
        // FEEDBACK
        // =====================================================================
        public int AdaugaFeedback(int idVizita, int? idExpozitie, int nota, string comentariu)
        {
            string text = string.IsNullOrWhiteSpace(comentariu) ? null : comentariu.Trim();

            using (var db = DbFactory.Create())
            {
                var vizita = GasesteVizita(db, idVizita);
                var erori = new List<string>();

                if (vizita.Status != StatusuriVizita.Efectuata)
                    erori.Add("Feedback-ul se poate da doar pentru o vizită efectuată.");

                if (nota < 1 || nota > 5)
                    erori.Add("Nota trebuie să fie între 1 și 5.");

                if (text != null && text.Length > 500)
                    erori.Add("Comentariul poate avea maximum 500 de caractere.");

                bool existaDeja;
                if (idExpozitie.HasValue)
                {
                    int idE = idExpozitie.Value;
                    if (!db.Expozitii.Any(e => e.IdExpozitie == idE))
                        erori.Add("Expoziția aleasă nu există.");

                    existaDeja = db.Feedback.Any(f => f.IdVizita == idVizita && f.IdExpozitie == idE);
                }
                else
                {
                    existaDeja = db.Feedback.Any(f => f.IdVizita == idVizita && f.IdExpozitie == null);
                }

                if (existaDeja)
                    erori.Add("Există deja un feedback pentru această vizită și această expoziție.");

                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                var feedback = new Feedback
                {
                    IdVizita = idVizita,
                    IdExpozitie = idExpozitie,
                    Nota = (byte)nota,
                    Comentariu = text,
                    DataFeedback = DateTime.Now
                };
                db.Feedback.InsertOnSubmit(feedback);
                db.SubmitChanges();
                return feedback.IdFeedback;
            }
        }
        public List<FeedbackDto> GetFeedback(int? idExpozitie = null)
        {
            using (var db = DbFactory.Create())
            {
                IQueryable<Feedback> q = db.Feedback;
                if (idExpozitie.HasValue)
                {
                    int idE = idExpozitie.Value;
                    q = q.Where(f => f.IdExpozitie == idE);
                }

                return (from f in q
                        join v in db.Vizite on f.IdVizita equals v.IdVizita
                        join z in db.Vizitatori on v.IdVizitator equals (int?)z.IdVizitator into vizitatori
                        from z in vizitatori.DefaultIfEmpty()
                        join e in db.Expozitii on f.IdExpozitie equals (int?)e.IdExpozitie into expozitii
                        from e in expozitii.DefaultIfEmpty()
                        orderby f.DataFeedback descending
                        select new FeedbackDto
                        {
                            IdFeedback = f.IdFeedback,
                            IdVizita = f.IdVizita,
                            DataFeedback = f.DataFeedback,
                            Vizitator = z == null ? "Vizitator anonim" : z.Nume + " " + z.Prenume,
                            Expozitie = e == null ? "Muzeul în general" : e.Titlu,
                            Nota = f.Nota,
                            Comentariu = f.Comentariu
                        })
                       .ToList();
            }
        }

        // =====================================================================
        // METODE AJUTATOARE (private)
        // =====================================================================
        private int SalveazaVizita(CerereVizita cerere, ModVanzare mod)
        {
            if (cerere == null) throw new ArgumentNullException(nameof(cerere));

            DateTime acum = DateTime.Now;
            var programService = new ProgramService();

            using (var db = DbFactory.Create())
            {
                var erori = new List<string>();
                TururiGhidate tur = GasesteTur(db, cerere.IdTur);

                // --- momentul vizitei ---
                DateTime dataVizita;
                if (tur != null)
                    dataVizita = tur.DataOraInceput;
                else if (mod == ModVanzare.LaCasa)
                    dataVizita = acum;
                else
                    dataVizita = FaraSecunde(cerere.DataVizita);

                // --- reguli diferite pentru casa / rezervare ---
                if (mod == ModVanzare.LaCasa)
                {
                    if (tur != null)
                    {
                        if (tur.DataOraInceput.Date != acum.Date)
                            erori.Add("La casă se vând bilete doar pentru tururile de azi. Pentru altă zi folosiți rezervarea.");
                        else if (tur.DataOraInceput <= acum)
                            erori.Add("Turul a început deja.");
                    }
                    else if (!programService.EsteDeschis(acum))
                    {
                        erori.Add("Muzeul este închis acum. Puteți face o rezervare pentru altă zi.");
                    }
                }
                else
                {
                    if (!cerere.IdVizitator.HasValue)
                        erori.Add("Pentru o rezervare trebuie ales un vizitator (pentru datele de contact).");

                    if (dataVizita <= acum)
                        erori.Add("Rezervarea trebuie făcută pentru un moment din viitor.");
                    else if (dataVizita > acum.AddYears(1))
                        erori.Add("Rezervările se pot face cu cel mult un an înainte.");
                    else if (tur == null && !programService.EsteDeschis(dataVizita))
                        erori.Add($"Muzeul este închis pe {dataVizita:dd.MM.yyyy} la ora {dataVizita:HH:mm}.");
                }

                // --- vizitator, observatii, bilete ---
                if (cerere.IdVizitator.HasValue)
                {
                    int idVizitator = cerere.IdVizitator.Value;
                    if (!db.Vizitatori.Any(z => z.IdVizitator == idVizitator))
                        erori.Add("Vizitatorul ales nu există.");
                }

                string observatii = string.IsNullOrWhiteSpace(cerere.Observatii) ? null : cerere.Observatii.Trim();
                if (observatii != null && observatii.Length > 200)
                    erori.Add("Observațiile pot avea maximum 200 de caractere.");

                var calcul = CalculeazaLinii(db, cerere.Bilete, tur, dataVizita, erori);

                if (erori.Count > 0)
                    throw new MuzeuException(erori);

                db.Connection.Open();
                using (var tranzactie = db.Connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    db.Transaction = tranzactie;

                    if (tur != null)
                    {
                        int libere = tur.NrMaximParticipanti - TurService.NumaraParticipanti(db, tur.IdTur);
                        if (calcul.NrBilete > libere)
                            throw new MuzeuException(libere <= 0 ? "Turul nu mai are locuri libere."
                                                    : libere == 1 ? "Turul mai are doar un loc liber."
                                                    : $"Turul mai are doar {libere} locuri libere.");
                    }

                    string status;
                    if (mod == ModVanzare.Rezervare)
                        status = StatusuriVizita.Rezervata;
                    else
                        status = tur != null ? StatusuriVizita.Confirmata : StatusuriVizita.Efectuata;

                    var vizita = new Vizite
                    {
                        IdVizitator = cerere.IdVizitator,
                        DataVizita = dataVizita,
                        IdTur = tur?.IdTur,
                        IdCasier = mod == ModVanzare.LaCasa ? Sesiune.UtilizatorCurent?.IdAngajat : null,
                        Status = status,
                        DataCreare = acum,
                        Observatii = observatii
                    };
                    db.Vizite.InsertOnSubmit(vizita);
                    db.SubmitChanges();   // acum vizita are Id

                    foreach (var linie in calcul.Linii)
                    {
                        for (int i = 0; i < linie.Cantitate; i++)
                        {
                            db.Bilete.InsertOnSubmit(new Bilete
                            {
                                IdVizita = vizita.IdVizita,
                                IdTipBilet = linie.IdTipBilet,
                                IdExpozitie = linie.IdExpozitie,
                                PretAchitat = linie.PretUnitar   // se salveaza pretul de acum
                            });
                        }
                    }
                    db.SubmitChanges();

                    tranzactie.Commit();
                    return vizita.IdVizita;
                }
            }
        }
        private static CalculPretDto CalculeazaLinii(MuzeuDBDataContext db, List<CerereBilet> bilete,
                                                     TururiGhidate tur, DateTime dataVizita, List<string> erori)
        {
            var calcul = new CalculPretDto();

            if (bilete == null || bilete.Count == 0)
            {
                erori.Add("Adăugați cel puțin un bilet.");
                return calcul;
            }
            var tipuri = db.TipuriBilet.ToDictionary(t => t.IdTipBilet);
            var expozitii = db.Expozitii.ToDictionary(e => e.IdExpozitie);
            DateTime zi = dataVizita.Date;

            foreach (var b in bilete)
            {
                if (b.Cantitate < 1)
                {
                    erori.Add("Cantitatea fiecărui tip de bilet trebuie să fie cel puțin 1.");
                    continue;
                }

                if (!tipuri.TryGetValue(b.IdTipBilet, out TipuriBilet tip))
                {
                    erori.Add($"Tipul de bilet cu Id {b.IdTipBilet} nu există.");
                    continue;
                }
                if (!tip.Activ)
                {
                    erori.Add($"Tipul de bilet „{tip.Denumire}” nu mai este disponibil.");
                    continue;
                }

                // la tururi, expozitia e cea a turului
                int? idExpozitie = tur != null ? tur.IdExpozitie : b.IdExpozitie;
                Expozitii expozitie = null;

                if (idExpozitie.HasValue)
                {
                    if (!expozitii.TryGetValue(idExpozitie.Value, out expozitie))
                    {
                        erori.Add($"Expoziția cu Id {idExpozitie.Value} nu există.");
                        continue;
                    }
                    if (expozitie.DataInceput > zi || (expozitie.DataSfarsit.HasValue && expozitie.DataSfarsit.Value < zi))
                    {
                        erori.Add($"Expoziția „{expozitie.Titlu}” nu este deschisă pe {zi:dd.MM.yyyy}.");
                        continue;
                    }
                }

                calcul.Linii.Add(new LiniePretDto
                {
                    IdTipBilet = tip.IdTipBilet,
                    TipBilet = tip.Denumire,
                    IdExpozitie = idExpozitie,
                    Expozitie = expozitie?.Titlu ?? "Acces general",
                    Cantitate = b.Cantitate,
                    PretBaza = tip.Pret,
                    SuplimentExpozitie = expozitie?.PretSuplimentar ?? 0,
                    SuplimentTur = tur?.PretSuplimentar ?? 0
                });
            }

            if (calcul.NrBilete > MaxBileteVizita)
                erori.Add($"O vizită poate avea maximum {MaxBileteVizita} de bilete.");

            return calcul;
        }

        private static IQueryable<VizitaDto> ProiecteazaVizite(MuzeuDBDataContext db, IQueryable<Vizite> sursa)
        {
            return from v in sursa
                   join z in db.Vizitatori on v.IdVizitator equals (int?)z.IdVizitator into vizitatori
                   from z in vizitatori.DefaultIfEmpty()
                   select new VizitaDto
                   {
                       IdVizita = v.IdVizita,
                       DataVizita = v.DataVizita,
                       Status = v.Status,
                       DataCreare = v.DataCreare,
                       Observatii = v.Observatii,
                       IdVizitator = v.IdVizitator,
                       Vizitator = z == null ? "Vizitator anonim" : z.Nume + " " + z.Prenume,
                       Telefon = z == null ? null : z.Telefon,
                       IdTur = v.IdTur,
                       Tur = (from t in db.TururiGhidate
                              join e in db.Expozitii on t.IdExpozitie equals e.IdExpozitie
                              where t.IdTur == v.IdTur
                              select e.Titlu + " (" + t.Limba + ")").FirstOrDefault(),
                       Casier = db.Angajati.Where(a => a.IdAngajat == v.IdCasier)
                                           .Select(a => a.Nume + " " + a.Prenume).FirstOrDefault(),
                       NrBilete = db.Bilete.Count(b => b.IdVizita == v.IdVizita),
                       Total = db.Bilete.Where(b => b.IdVizita == v.IdVizita).Sum(b => (decimal?)b.PretAchitat) ?? 0
                   };
        }

        private static TururiGhidate GasesteTur(MuzeuDBDataContext db, int? idTur)
        {
            if (!idTur.HasValue)
                return null;

            int id = idTur.Value;
            var tur = db.TururiGhidate.SingleOrDefault(t => t.IdTur == id);
            if (tur == null)
                throw new MuzeuException("Turul ghidat ales nu există.");
            return tur;
        }

        private static Vizite GasesteVizita(MuzeuDBDataContext db, int idVizita)
        {
            var vizita = db.Vizite.SingleOrDefault(v => v.IdVizita == idVizita);
            if (vizita == null)
                throw new MuzeuException("Vizita nu există.");
            return vizita;
        }

        private static void NormalizeazaVizitator(Vizitatori z)
        {
            z.Nume = z.Nume?.Trim();
            z.Prenume = z.Prenume?.Trim();
            z.Email = string.IsNullOrWhiteSpace(z.Email) ? null : z.Email.Trim().ToLowerInvariant();
            z.Telefon = string.IsNullOrWhiteSpace(z.Telefon) ? null : z.Telefon.Trim();
        }

        private static void ValideazaVizitator(MuzeuDBDataContext db, Vizitatori z)
        {
            var erori = new List<string>();

            if (string.IsNullOrEmpty(z.Nume))
                erori.Add("Numele este obligatoriu.");
            else if (z.Nume.Length > 50)
                erori.Add("Numele poate avea maximum 50 de caractere.");

            if (string.IsNullOrEmpty(z.Prenume))
                erori.Add("Prenumele este obligatoriu.");
            else if (z.Prenume.Length > 50)
                erori.Add("Prenumele poate avea maximum 50 de caractere.");

            if (z.Email == null && z.Telefon == null)
                erori.Add("Completați cel puțin emailul sau telefonul (pentru a putea contacta vizitatorul).");

            if (z.Email != null)
            {
                if (z.Email.Length > 100 || !RegexEmail.IsMatch(z.Email))
                    erori.Add("Adresa de email nu este validă.");
                else
                {
                    string email = z.Email;
                    int idVizitator = z.IdVizitator;   // 0 la adăugare
                    if (db.Vizitatori.Any(x => x.Email == email && x.IdVizitator != idVizitator))
                        erori.Add($"Există deja un vizitator cu adresa {email}.");
                }
            }

            if (z.Telefon != null && !RegexTelefon.IsMatch(z.Telefon))
                erori.Add("Telefonul poate conține doar cifre, spații și +, între 6 și 20 de caractere.");

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }

        private static void ValideazaTipBilet(MuzeuDBDataContext db, int idTipBilet, string denumire, decimal pret)
        {
            var erori = new List<string>();

            if (string.IsNullOrEmpty(denumire))
                erori.Add("Denumirea tipului de bilet este obligatorie.");
            else if (denumire.Length > 50)
                erori.Add("Denumirea poate avea maximum 50 de caractere.");
            else if (db.TipuriBilet.Any(t => t.Denumire == denumire && t.IdTipBilet != idTipBilet))
                erori.Add($"Există deja un tip de bilet „{denumire}”.");

            if (pret < 0)
                erori.Add("Prețul nu poate fi negativ.");
            else if (pret > 10000)
                erori.Add("Prețul pare prea mare (maximum 10.000 lei).");

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }

        private static DateTime FaraSecunde(DateTime d)
        {
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        }

        private static string Trunchiaza(string text, int lungimeMaxima)
        {
            return text.Length <= lungimeMaxima ? text : text.Substring(0, lungimeMaxima);
        }
    }
}
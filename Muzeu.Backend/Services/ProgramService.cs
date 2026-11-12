using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{
    public class ProgramService
    {
        private static readonly string[] NumeZile =
            { "", "Luni", "Marți", "Miercuri", "Joi", "Vineri", "Sâmbătă", "Duminică" };

        private const int MaxZilePerioada = 366;

        // =====================================================================
        // METODE STATICE UTILE SI PENTRU INTERFATA
        // =====================================================================

        /// <summary>
        /// Ziua saptamanii in conventia din baza de date: 1 = Luni ... 7 = Duminica.
        /// (În C#, DayOfWeek.Sunday = 0, de aceea e nevoie de conversie.)
        /// </summary>
        public static byte ZiSaptamanaDin(DateTime data)
        {
            return (byte)(((int)data.DayOfWeek + 6) % 7 + 1);
        }

        public static string NumeZi(byte ziSaptamana)
        {
            return ziSaptamana >= 1 && ziSaptamana <= 7 ? NumeZile[ziSaptamana] : "?";
        }

        public static TimeSpan ParseazaOra(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) &&
                TimeSpan.TryParseExact(text.Trim(), new[] { @"h\:mm", @"hh\:mm" },
                                       CultureInfo.InvariantCulture, out TimeSpan ora) &&
                ora < TimeSpan.FromDays(1))
            {
                return ora;
            }

            throw new MuzeuException($"„{text}” nu este o oră validă. Folosiți formatul HH:mm (ex. 09:30).");
        }

        // =====================================================================
        // CITIRE
        // =====================================================================

        public List<ProgramZiDto> GetProgramSaptamanal()
        {
            using (var db = DbFactory.Create())
            {
                var zile = db.ProgramSaptamanal.ToDictionary(p => p.ZiSaptamana);
                var rezultat = new List<ProgramZiDto>();

                for (byte zi = 1; zi <= 7; zi++)
                {
                    zile.TryGetValue(zi, out ProgramSaptamanal p);
                    bool inchis = p == null || p.Inchis;   // o zi lipsa din tabel = inchis

                    rezultat.Add(new ProgramZiDto
                    {
                        ZiSaptamana = zi,
                        NumeZi = NumeZi(zi),
                        Inchis = inchis,
                        OraDeschidere = inchis ? null : p.OraDeschidere,
                        OraInchidere = inchis ? null : p.OraInchidere
                    });
                }

                return rezultat;
            }
        }
        public ProgramZiDto GetProgramPentruData(DateTime data)
        {
            return GetProgramPerioada(data, data)[0];
        }
        public List<ProgramZiDto> GetProgramPerioada(DateTime de, DateTime pana)
        {
            DateTime start = de.Date;
            DateTime sfarsit = pana.Date;

            if (sfarsit < start)
                throw new MuzeuException("Data de sfârșit a perioadei nu poate fi înaintea datei de început.");
            if ((sfarsit - start).TotalDays >= MaxZilePerioada)
                throw new MuzeuException($"Perioada poate avea maximum {MaxZilePerioada} de zile.");

            using (var db = DbFactory.Create())
            {
                var calculator = IncarcaCalculator(db, start, sfarsit);

                var rezultat = new List<ProgramZiDto>();
                for (DateTime zi = start; zi <= sfarsit; zi = zi.AddDays(1))
                    rezultat.Add(calculator.Calculeaza(zi));

                return rezultat;
            }
        }

        public List<ProgramZiDto> GetExceptii(bool doarViitoare = true)
        {
            DateTime azi = DateTime.Today;

            using (var db = DbFactory.Create())
            {
                IQueryable<ExceptiiProgram> q = db.ExceptiiProgram;
                if (doarViitoare)
                    q = q.Where(e => e.Data >= azi);

                var exceptii = q.OrderBy(e => e.Data).ToList();
                var calculator = new CalculatorProgram(new List<ProgramSaptamanal>(), exceptii);

                return exceptii.Select(e => calculator.Calculeaza(e.Data)).ToList();
            }
        }

        public bool EsteDeschis(DateTime moment)
        {
            using (var db = DbFactory.Create())
            {
                return db.fn_EsteDeschis(moment) ?? false;
            }
        }

        public bool EsteDeschisIntre(DateTime inceput, DateTime sfarsit)
        {
            using (var db = DbFactory.Create())
            {
                var calculator = IncarcaCalculator(db, inceput.Date, inceput.Date);
                return calculator.IntervalPermis(inceput, sfarsit);
            }
        }

        public DateTime? UrmatoareaZiDeschisa(DateTime deLa)
        {
            return GetProgramPerioada(deLa, deLa.AddDays(60))
                .Where(p => !p.Inchis)
                .Select(p => p.Data)
                .FirstOrDefault();
        }

        // =====================================================================
        // MODIFICARE PROGRAM SAPTAMANAL
        // =====================================================================

        public void ModificaZi(byte ziSaptamana, bool inchis, TimeSpan? oraDeschidere, TimeSpan? oraInchidere)
        {
            if (ziSaptamana < 1 || ziSaptamana > 7)
                throw new MuzeuException("Ziua săptămânii trebuie să fie între 1 (Luni) și 7 (Duminică).");

            TimeSpan? deschidere = inchis ? null : FaraSecunde(oraDeschidere);
            TimeSpan? inchidere = inchis ? null : FaraSecunde(oraInchidere);

            var erori = new List<string>();
            ValideazaOre(inchis, deschidere, inchidere, erori);
            if (erori.Count > 0)
                throw new MuzeuException(erori);

            DateTime azi = DateTime.Today;

            using (var db = DbFactory.Create())
            {
                var saptamanal = db.ProgramSaptamanal.ToList();
                var zi = saptamanal.SingleOrDefault(p => p.ZiSaptamana == ziSaptamana);

                if (zi == null)   // ziua lipseste din tabel => o adaugam
                {
                    zi = new ProgramSaptamanal { ZiSaptamana = ziSaptamana };
                    db.ProgramSaptamanal.InsertOnSubmit(zi);
                    saptamanal.Add(zi);
                }

                zi.Inchis = inchis;
                zi.OraDeschidere = deschidere;
                zi.OraInchidere = inchidere;

                var calculator = new CalculatorProgram(saptamanal, db.ExceptiiProgram.Where(e => e.Data >= azi).ToList());
                VerificaActivitatiViitoare(db, calculator);

                db.SubmitChanges();
            }
        }

        // =====================================================================
        // EXCEPTII
        // =====================================================================

        /// <summary>Adauga o exceptie si intoarce Id-ul ei.</summary>
        public int AdaugaExceptie(ExceptiiProgram exceptie)
        {
            if (exceptie == null) throw new ArgumentNullException(nameof(exceptie));

            NormalizeazaExceptie(exceptie);

            using (var db = DbFactory.Create())
            {
                ValideazaExceptie(db, exceptie);

                var calculator = IncarcaCalculator(db, DateTime.Today, null);
                calculator.SeteazaExceptie(exceptie);
                VerificaActivitatiViitoare(db, calculator);

                db.ExceptiiProgram.InsertOnSubmit(exceptie);
                db.SubmitChanges();

                return exceptie.IdExceptie;
            }
        }

        public void ModificaExceptie(ExceptiiProgram exceptie)
        {
            if (exceptie == null) throw new ArgumentNullException(nameof(exceptie));

            NormalizeazaExceptie(exceptie);

            using (var db = DbFactory.Create())
            {
                var existenta = db.ExceptiiProgram.SingleOrDefault(e => e.IdExceptie == exceptie.IdExceptie);
                if (existenta == null)
                    throw new MuzeuException("Excepția nu mai există în baza de date.");

                ValideazaExceptie(db, exceptie);

                var calculator = IncarcaCalculator(db, DateTime.Today, null);
                calculator.EliminaExceptie(existenta.Data);   // scoatem varianta veche (data se poate schimba)

                existenta.Data = exceptie.Data;
                existenta.Inchis = exceptie.Inchis;
                existenta.OraDeschidere = exceptie.OraDeschidere;
                existenta.OraInchidere = exceptie.OraInchidere;
                existenta.Motiv = exceptie.Motiv;

                calculator.SeteazaExceptie(existenta);         // punem varianta noua
                VerificaActivitatiViitoare(db, calculator);

                db.SubmitChanges();
            }
        }

        public void StergeExceptie(int idExceptie)
        {
            using (var db = DbFactory.Create())
            {
                var exceptie = db.ExceptiiProgram.SingleOrDefault(e => e.IdExceptie == idExceptie);
                if (exceptie == null)
                    throw new MuzeuException("Excepția nu există.");

                var calculator = IncarcaCalculator(db, DateTime.Today, null);
                calculator.EliminaExceptie(exceptie.Data);
                VerificaActivitatiViitoare(db, calculator);

                db.ExceptiiProgram.DeleteOnSubmit(exceptie);
                db.SubmitChanges();
            }
        }

        // =====================================================================
        // METODE AJUTATOARE (private)
        // =====================================================================

        private static CalculatorProgram IncarcaCalculator(MuzeuDBDataContext db, DateTime de, DateTime? pana)
        {
            DateTime start = de.Date;
            IQueryable<ExceptiiProgram> exceptii = db.ExceptiiProgram.Where(e => e.Data >= start);

            if (pana.HasValue)
            {
                DateTime sfarsit = pana.Value.Date;
                exceptii = exceptii.Where(e => e.Data <= sfarsit);
            }

            return new CalculatorProgram(db.ProgramSaptamanal.ToList(), exceptii.ToList());
        }

        private static void VerificaActivitatiViitoare(MuzeuDBDataContext db, CalculatorProgram calculator)
        {
            DateTime acum = DateTime.Now;

            var tururi = db.TururiGhidate
                .Where(t => t.DataOraInceput >= acum)
                .Select(t => new { t.DataOraInceput, t.DurataMinute })
                .ToList();

            var rezervari = db.Vizite
                .Where(v => v.DataVizita >= acum && v.IdTur == null &&
                            (v.Status == StatusuriVizita.Rezervata || v.Status == StatusuriVizita.Confirmata))
                .Select(v => v.DataVizita)
                .ToList();

            var tururiAfectate = tururi
                .Where(t => !calculator.IntervalPermis(t.DataOraInceput, t.DataOraInceput.AddMinutes(t.DurataMinute)))
                .Select(t => t.DataOraInceput)
                .OrderBy(d => d)
                .ToList();

            var rezervariAfectate = rezervari
                .Where(d => !calculator.MomentPermis(d))
                .OrderBy(d => d)
                .ToList();

            if (tururiAfectate.Count == 0 && rezervariAfectate.Count == 0)
                return;

            var erori = new List<string>
            {
                $"Modificarea nu poate fi salvată: {tururiAfectate.Count} tur(uri) ghidat(e) și " +
                $"{rezervariAfectate.Count} rezervare(ări) ar rămâne în afara programului."
            };
            erori.AddRange(tururiAfectate.Take(5).Select(d => $"Tur ghidat: {d:dd.MM.yyyy HH:mm}"));
            erori.AddRange(rezervariAfectate.Take(5).Select(d => $"Rezervare: {d:dd.MM.yyyy HH:mm}"));
            erori.Add("Mutați sau anulați-le întâi.");

            throw new MuzeuException(erori);
        }

        private static void NormalizeazaExceptie(ExceptiiProgram e)
        {
            e.Data = e.Data.Date;
            e.Motiv = e.Motiv?.Trim();

            if (e.Inchis)
            {
                e.OraDeschidere = null;
                e.OraInchidere = null;
            }
            else
            {
                e.OraDeschidere = FaraSecunde(e.OraDeschidere);
                e.OraInchidere = FaraSecunde(e.OraInchidere);
            }
        }

        private static void ValideazaExceptie(MuzeuDBDataContext db, ExceptiiProgram e)
        {
            var erori = new List<string>();

            if (e.Data < DateTime.Today)
                erori.Add("Nu se pot adăuga excepții pentru zile din trecut.");

            if (string.IsNullOrEmpty(e.Motiv))
                erori.Add("Motivul este obligatoriu (ex. „Crăciun”, „Noaptea Muzeelor”).");
            else if (e.Motiv.Length > 100)
                erori.Add("Motivul poate avea maximum 100 de caractere.");

            ValideazaOre(e.Inchis, e.OraDeschidere, e.OraInchidere, erori);

            DateTime data = e.Data;
            int idExceptie = e.IdExceptie;   // 0 la adaugare
            if (db.ExceptiiProgram.Any(x => x.Data == data && x.IdExceptie != idExceptie))
                erori.Add($"Există deja o excepție pentru {data:dd.MM.yyyy}.");

            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }

        private static void ValideazaOre(bool inchis, TimeSpan? deschidere, TimeSpan? inchidere, List<string> erori)
        {
            if (inchis) return;

            if (!deschidere.HasValue || !inchidere.HasValue)
            {
                erori.Add("Pentru o zi deschisă trebuie completate ambele ore.");
                return;
            }

            if (!OraValida(deschidere.Value) || !OraValida(inchidere.Value))
                erori.Add("Orele trebuie să fie între 00:00 și 23:59.");
            else if (inchidere.Value <= deschidere.Value)
                erori.Add("Ora de închidere trebuie să fie după ora de deschidere.");
        }

        private static bool OraValida(TimeSpan ora)
        {
            return ora >= TimeSpan.Zero && ora < TimeSpan.FromDays(1);
        }

        private static TimeSpan? FaraSecunde(TimeSpan? ora)
        {
            if (!ora.HasValue) return null;
            return TimeSpan.FromMinutes(Math.Floor(ora.Value.TotalMinutes));
        }

        // =====================================================================
        // CALCULATORUL DE PROGRAM
        // =====================================================================
        private class CalculatorProgram
        {
            private readonly Dictionary<byte, ProgramSaptamanal> _saptamanal;
            private readonly Dictionary<DateTime, ExceptiiProgram> _exceptii;

            public CalculatorProgram(IEnumerable<ProgramSaptamanal> saptamanal, IEnumerable<ExceptiiProgram> exceptii)
            {
                _saptamanal = saptamanal.ToDictionary(p => p.ZiSaptamana);
                _exceptii = exceptii.ToDictionary(e => e.Data.Date);
            }

            public void SeteazaExceptie(ExceptiiProgram exceptie)
            {
                _exceptii[exceptie.Data.Date] = exceptie;
            }

            public void EliminaExceptie(DateTime data)
            {
                _exceptii.Remove(data.Date);
            }

            public ProgramZiDto Calculeaza(DateTime data)
            {
                DateTime zi = data.Date;
                byte ziSaptamana = ZiSaptamanaDin(zi);

                if (_exceptii.TryGetValue(zi, out ExceptiiProgram ex))
                {
                    return new ProgramZiDto
                    {
                        IdExceptie = ex.IdExceptie,
                        Data = zi,
                        ZiSaptamana = ziSaptamana,
                        NumeZi = NumeZi(ziSaptamana),
                        Inchis = ex.Inchis,
                        OraDeschidere = ex.Inchis ? null : ex.OraDeschidere,
                        OraInchidere = ex.Inchis ? null : ex.OraInchidere,
                        EsteExceptie = true,
                        Motiv = ex.Motiv
                    };
                }

                _saptamanal.TryGetValue(ziSaptamana, out ProgramSaptamanal p);
                bool inchis = p == null || p.Inchis;

                return new ProgramZiDto
                {
                    Data = zi,
                    ZiSaptamana = ziSaptamana,
                    NumeZi = NumeZi(ziSaptamana),
                    Inchis = inchis,
                    OraDeschidere = inchis ? null : p.OraDeschidere,
                    OraInchidere = inchis ? null : p.OraInchidere
                };
            }

            public bool MomentPermis(DateTime moment)
            {
                var p = Calculeaza(moment);
                return !p.Inchis &&
                       moment.TimeOfDay >= p.OraDeschidere.Value &&
                       moment.TimeOfDay < p.OraInchidere.Value;
            }

            public bool IntervalPermis(DateTime inceput, DateTime sfarsit)
            {
                if (sfarsit < inceput || sfarsit.Date != inceput.Date)
                    return false;

                var p = Calculeaza(inceput);
                return !p.Inchis &&
                       inceput.TimeOfDay >= p.OraDeschidere.Value &&
                       sfarsit.TimeOfDay <= p.OraInchidere.Value;
            }
        }
    }
}

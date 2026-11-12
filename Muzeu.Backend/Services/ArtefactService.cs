using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend.DTOs;
using Muzeu.Backend.Exceptions;

namespace Muzeu.Backend.Services
{
    public class ArtefactService
    {
        private const string Depozit = "Depozit";
 
        // =====================================================================
        // CITIRE
        // =====================================================================
 
        public List<ArtefactDto> GetAll()
        {
            return Cauta(new FiltruArtefacte());
        }
 
        /// <summary>Cautare cu filtre combinate. Filtru null sau gol => toate artefactele.</summary>
        public List<ArtefactDto> Cauta(FiltruArtefacte filtru)
        {
            if (filtru == null) filtru = new FiltruArtefacte();
 
            using (var db = DbFactory.Create())
            {
                IQueryable<Artefacte> q = db.Artefacte;
 
                if (!string.IsNullOrWhiteSpace(filtru.Text))
                {
                    string t = filtru.Text.Trim();
                    q = q.Where(a =>
                        a.Denumire.Contains(t) ||
                        a.CodInventar.Contains(t) ||
                        (a.Autor != null && a.Autor.Contains(t)) ||
                        (a.Material != null && a.Material.Contains(t)));
                }
 
                if (filtru.IdCategorie.HasValue)
                {
                    int idCategorie = filtru.IdCategorie.Value;
                    q = q.Where(a => a.IdCategorie == idCategorie);
                }
 
                if (!string.IsNullOrWhiteSpace(filtru.StareConservare))
                {
                    string stare = filtru.StareConservare;
                    q = q.Where(a => a.StareConservare == stare);
                }
 
                if (filtru.DoarDepozit)
                {
                    q = q.Where(a => a.IdExpozitie == null);
                }
                else if (filtru.IdExpozitie.HasValue)
                {
                    int idExpozitie = filtru.IdExpozitie.Value;
                    q = q.Where(a => a.IdExpozitie == idExpozitie);
                }
 
                if (filtru.DoarInRestaurare)
                {
                    q = q.Where(a => db.Restaurari.Any(r => r.IdArtefact == a.IdArtefact && r.DataSfarsit == null));
                }
 
                return ProiecteazaDto(db, q)
                    .OrderBy(a => a.CodInventar)
                    .ToList();
            }
        }
 
        public List<ArtefactDto> GetByExpozitie(int idExpozitie)
        {
            return Cauta(new FiltruArtefacte { IdExpozitie = idExpozitie });
        }
 
        public List<ArtefactDto> GetDinDepozit()
        {
            return Cauta(new FiltruArtefacte { DoarDepozit = true });
        }
 
        /// <summary>Datele de afisare pentru un singur artefact (locatie, categorie, stare).</summary>
        public ArtefactDto GetDetalii(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                var dto = ProiecteazaDto(db, db.Artefacte.Where(a => a.IdArtefact == idArtefact))
                    .SingleOrDefault();
 
                if (dto == null)
                    throw new MuzeuException("Artefactul căutat nu există.");
 
                return dto;
            }
        }
 
        public Artefacte GetById(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact == null)
                    throw new MuzeuException("Artefactul căutat nu există.");
 
                return artefact;
            }
        }
 
        /// <summary>Urmatorul cod liber de forma INV-0009 (pentru a-l propune in formular).</summary>
        public string GenereazaCodInventar()
        {
            using (var db = DbFactory.Create())
            {
                var coduri = db.Artefacte
                    .Where(a => a.CodInventar.StartsWith("INV-"))
                    .Select(a => a.CodInventar)
                    .ToList();
 
                int maxim = 0;
                foreach (var cod in coduri)
                {
                    if (int.TryParse(cod.Substring(4), out int numar) && numar > maxim)
                        maxim = numar;
                }
 
                return "INV-" + (maxim + 1).ToString("D4");
            }
        }
 
        /// <summary>Istoricul mutarilor unui artefact, cele mai recente primele.</summary>
        public List<MutareDto> GetIstoricLocatii(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                return db.IstoricLocatii
                    .Where(i => i.IdArtefact == idArtefact)
                    .OrderByDescending(i => i.DataMutare)
                    .ThenByDescending(i => i.IdMutare)
                    .Select(i => new MutareDto
                    {
                        IdMutare = i.IdMutare,
                        DataMutare = i.DataMutare,
                        De = db.Expozitii.Where(e => e.IdExpozitie == i.IdExpozitieVeche)
                                         .Select(e => e.Titlu).FirstOrDefault() ?? Depozit,
                        La = db.Expozitii.Where(e => e.IdExpozitie == i.IdExpozitieNoua)
                                         .Select(e => e.Titlu).FirstOrDefault() ?? Depozit
                    })
                    .ToList();
            }
        }
 
        /// <summary>Toate restaurarile unui artefact, cele mai recente primele.</summary>
        public List<RestaurareDto> GetRestaurari(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                return ProiecteazaRestaurari(db, db.Restaurari.Where(r => r.IdArtefact == idArtefact))
                    .OrderByDescending(r => r.DataInceput)
                    .ToList();
            }
        }
 
        /// <summary>Restaurarile nefinalizate, din tot muzeul.</summary>
        public List<RestaurareDto> GetRestaurariInCurs()
        {
            using (var db = DbFactory.Create())
            {
                return ProiecteazaRestaurari(db, db.Restaurari.Where(r => r.DataSfarsit == null))
                    .OrderBy(r => r.DataInceput)
                    .ToList();
            }
        }
 
        public List<ItemLista> GetCategorii()
        {
            using (var db = DbFactory.Create())
            {
                return db.Categorii
                    .OrderBy(c => c.Denumire)
                    .Select(c => new ItemLista { Id = c.IdCategorie, Denumire = c.Denumire })
                    .ToList();
            }
        }
 
        /// <summary>Expozitiile in care se pot muta artefacte (cele neincheiate).</summary>
        public List<ItemLista> GetExpozitiiPentruMutare()
        {
            DateTime azi = DateTime.Today;
 
            using (var db = DbFactory.Create())
            {
                return db.Expozitii
                    .Where(e => e.DataSfarsit == null || e.DataSfarsit >= azi)
                    .OrderBy(e => e.Titlu)
                    .Select(e => new ItemLista { Id = e.IdExpozitie, Denumire = e.Titlu })
                    .ToList();
            }
        }
 
        public List<ItemLista> GetRestauratori()
        {
            using (var db = DbFactory.Create())
            {
                return db.Angajati
                    .Where(a => a.Functie == FunctiiAngajat.Restaurator && a.Activ)
                    .OrderBy(a => a.Nume)
                    .Select(a => new ItemLista { Id = a.IdAngajat, Denumire = a.Nume + " " + a.Prenume })
                    .ToList();
            }
        }
 
        // =====================================================================
        // ADĂUGARE / MODIFICARE / STERGERE
        // =====================================================================
 
        public int Adauga(Artefacte artefact)
        {
            if (artefact == null) throw new ArgumentNullException(nameof(artefact));
 
            using (var db = DbFactory.Create())
            {
                Normalizeaza(artefact);
                Valideaza(db, artefact, verificaLocatia: true);
 
                db.Artefacte.InsertOnSubmit(artefact);
                db.SubmitChanges();
 
                return artefact.IdArtefact;
            }
        }
 
        public void Modifica(Artefacte artefact)
        {
            if (artefact == null) throw new ArgumentNullException(nameof(artefact));
 
            using (var db = DbFactory.Create())
            {
                var existent = db.Artefacte.SingleOrDefault(a => a.IdArtefact == artefact.IdArtefact);
                if (existent == null)
                    throw new MuzeuException("Artefactul nu mai există în baza de date.");
 
                Normalizeaza(artefact);
                Valideaza(db, artefact, verificaLocatia: false);
 
                existent.CodInventar = artefact.CodInventar;
                existent.Denumire = artefact.Denumire;
                existent.Descriere = artefact.Descriere;
                existent.Autor = artefact.Autor;
                existent.Perioada = artefact.Perioada;
                existent.AnAproximativ = artefact.AnAproximativ;
                existent.Material = artefact.Material;
                existent.Dimensiuni = artefact.Dimensiuni;
                existent.Provenienta = artefact.Provenienta;
                existent.DataAchizitie = artefact.DataAchizitie;
                existent.ValoareEstimata = artefact.ValoareEstimata;
                existent.StareConservare = artefact.StareConservare;
                existent.IdCategorie = artefact.IdCategorie;
                // IdExpozitie ramane neschimbat
 
                db.SubmitChanges();
            }
        }
 
        public void Sterge(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact == null)
                    throw new MuzeuException("Artefactul nu există.");
 
                bool areIstoric =
                    db.Restaurari.Any(r => r.IdArtefact == idArtefact) ||
                    db.IstoricLocatii.Any(i => i.IdArtefact == idArtefact);
 
                if (areIstoric)
                    throw new MuzeuException(
                        "Artefactul are istoric de mutări sau restaurări și nu poate fi șters din evidență.");
 
                db.Artefacte.DeleteOnSubmit(artefact);
                db.SubmitChanges();
            }
        }
 
        // =====================================================================
        // MUTARI (fiecare mutare ajunge automat in IstoricLocatii, prin trigger)
        // =====================================================================
 
        public void MutaInExpozitie(int idArtefact, int idExpozitie)
        {
            using (var db = DbFactory.Create())
            {
                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact == null)
                    throw new MuzeuException("Artefactul nu există.");
 
                if (artefact.IdExpozitie == idExpozitie)
                    throw new MuzeuException("Artefactul se află deja în această expoziție.");
 
                if (EsteInRestaurare(db, idArtefact))
                    throw new MuzeuException(
                        "Artefactul este în restaurare și nu poate fi expus până la finalizarea restaurării.");
 
                string eroareExpozitie = VerificaExpozitieDisponibila(db, idExpozitie);
                if (eroareExpozitie != null)
                    throw new MuzeuException(eroareExpozitie);
 
                artefact.IdExpozitie = idExpozitie;
                db.SubmitChanges();
            }
        }
 
        public void MutaInDepozit(int idArtefact)
        {
            using (var db = DbFactory.Create())
            {
                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact == null)
                    throw new MuzeuException("Artefactul nu există.");
 
                if (artefact.IdExpozitie == null)
                    throw new MuzeuException("Artefactul se află deja în depozit.");
 
                artefact.IdExpozitie = null;
                db.SubmitChanges();
            }
        }
 
        // =====================================================================
        // RESTAURARI
        // =====================================================================
 
        public int IncepeRestaurare(int idArtefact, int? idRestaurator, DateTime dataInceput, string descriere)
        {
            using (var db = DbFactory.Create())
            {
                var artefact = db.Artefacte.SingleOrDefault(a => a.IdArtefact == idArtefact);
                if (artefact == null)
                    throw new MuzeuException("Artefactul nu există.");
 
                var erori = new List<string>();
                string descriereCurata = descriere?.Trim();
 
                if (EsteInRestaurare(db, idArtefact))
                    erori.Add("Artefactul este deja în restaurare.");
 
                if (string.IsNullOrEmpty(descriereCurata))
                    erori.Add("Descrierea lucrărilor de restaurare este obligatorie.");
                else if (descriereCurata.Length > 500)
                    erori.Add("Descrierea poate avea maximum 500 de caractere.");
 
                if (dataInceput.Date > DateTime.Today)
                    erori.Add("Data de început nu poate fi în viitor.");
 
                if (idRestaurator.HasValue)
                {
                    int id = idRestaurator.Value;
                    bool restauratorValid = db.Angajati.Any(a =>
                        a.IdAngajat == id && a.Functie == FunctiiAngajat.Restaurator && a.Activ);
 
                    if (!restauratorValid)
                        erori.Add("Persoana selectată nu este un restaurator activ.");
                }
 
                if (erori.Count > 0)
                    throw new MuzeuException(erori);
 
                var restaurare = new Restaurari
                {
                    IdArtefact = idArtefact,
                    IdRestaurator = idRestaurator,
                    DataInceput = dataInceput.Date,
                    DataSfarsit = null,
                    Descriere = descriereCurata,
                    Cost = null
                };
                db.Restaurari.InsertOnSubmit(restaurare);
 
                artefact.IdExpozitie = null;
 
                db.SubmitChanges();
                return restaurare.IdRestaurare;
            }
        }
 
        public void FinalizeazaRestaurare(int idRestaurare, DateTime dataSfarsit, decimal? cost, string stareNoua)
        {
            using (var db = DbFactory.Create())
            {
                var restaurare = db.Restaurari.SingleOrDefault(r => r.IdRestaurare == idRestaurare);
                if (restaurare == null)
                    throw new MuzeuException("Restaurarea nu există.");
 
                if (restaurare.DataSfarsit.HasValue)
                    throw new MuzeuException("Restaurarea a fost deja finalizată.");
 
                var erori = new List<string>();
                DateTime zi = dataSfarsit.Date;
 
                if (zi < restaurare.DataInceput)
                    erori.Add("Data de finalizare nu poate fi înaintea datei de început.");
                if (zi > DateTime.Today)
                    erori.Add("Data de finalizare nu poate fi în viitor.");
                if (cost.HasValue && cost.Value < 0)
                    erori.Add("Costul nu poate fi negativ.");
                if (!StariConservare.Toate.Contains(stareNoua))
                    erori.Add("Starea de conservare aleasă nu este validă.");
 
                if (erori.Count > 0)
                    throw new MuzeuException(erori);
 
                restaurare.DataSfarsit = zi;
                restaurare.Cost = cost;
 
                int idArtefact = restaurare.IdArtefact;
                var artefact = db.Artefacte.Single(a => a.IdArtefact == idArtefact);
                artefact.StareConservare = stareNoua;
 
                db.SubmitChanges();
            }
        }
 
        // =====================================================================
        // METODE AJUTATOARE (private)
        // =====================================================================
 
        private static IQueryable<ArtefactDto> ProiecteazaDto(MuzeuDBDataContext db, IQueryable<Artefacte> sursa)
        {
            return from a in sursa
                   join c in db.Categorii on a.IdCategorie equals c.IdCategorie
                   select new ArtefactDto
                   {
                       IdArtefact = a.IdArtefact,
                       CodInventar = a.CodInventar,
                       Denumire = a.Denumire,
                       Autor = a.Autor,
                       Perioada = a.Perioada,
                       AnAproximativ = a.AnAproximativ,
                       Material = a.Material,
                       StareConservare = a.StareConservare,
                       ValoareEstimata = a.ValoareEstimata,
                       Categorie = c.Denumire,
                       IdExpozitie = a.IdExpozitie,
                       Locatie = db.Expozitii.Where(e => e.IdExpozitie == a.IdExpozitie)
                                             .Select(e => e.Titlu).FirstOrDefault() ?? Depozit,
                       InRestaurare = db.Restaurari.Any(r => r.IdArtefact == a.IdArtefact && r.DataSfarsit == null)
                   };
        }
 
        private static IQueryable<RestaurareDto> ProiecteazaRestaurari(MuzeuDBDataContext db, IQueryable<Restaurari> sursa)
        {
            return from r in sursa
                   join a in db.Artefacte on r.IdArtefact equals a.IdArtefact
                   select new RestaurareDto
                   {
                       IdRestaurare = r.IdRestaurare,
                       IdArtefact = r.IdArtefact,
                       CodInventar = a.CodInventar,
                       Artefact = a.Denumire,
                       Restaurator = db.Angajati.Where(x => x.IdAngajat == r.IdRestaurator)
                                                .Select(x => x.Nume + " " + x.Prenume).FirstOrDefault(),
                       DataInceput = r.DataInceput,
                       DataSfarsit = r.DataSfarsit,
                       Descriere = r.Descriere,
                       Cost = r.Cost
                   };
        }
 
        private static bool EsteInRestaurare(MuzeuDBDataContext db, int idArtefact)
        {
            return db.Restaurari.Any(r => r.IdArtefact == idArtefact && r.DataSfarsit == null);
        }

        private static string VerificaExpozitieDisponibila(MuzeuDBDataContext db, int idExpozitie)
        {
            var expozitie = db.Expozitii.SingleOrDefault(e => e.IdExpozitie == idExpozitie);
            if (expozitie == null)
                return "Expoziția aleasă nu există.";
 
            if (expozitie.DataSfarsit.HasValue && expozitie.DataSfarsit.Value < DateTime.Today)
                return $"Expoziția „{expozitie.Titlu}” s-a încheiat și nu mai poate primi artefacte.";
 
            return null;
        }

        private static void Normalizeaza(Artefacte a)
        {
            a.CodInventar = a.CodInventar?.Trim().ToUpperInvariant();
            a.Denumire = a.Denumire?.Trim();
            a.Descriere = TextSauNull(a.Descriere);
            a.Autor = TextSauNull(a.Autor);
            a.Perioada = TextSauNull(a.Perioada);
            a.Material = TextSauNull(a.Material);
            a.Dimensiuni = TextSauNull(a.Dimensiuni);
            a.Provenienta = TextSauNull(a.Provenienta);
            if (a.DataAchizitie.HasValue)
                a.DataAchizitie = a.DataAchizitie.Value.Date;
        }
 
        private static string TextSauNull(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
 
        private static void VerificaLungime(List<string> erori, string valoare, int maxim, string numeCamp)
        {
            if (valoare != null && valoare.Length > maxim)
                erori.Add($"{numeCamp} poate avea maximum {maxim} de caractere.");
        }
 
        private static void Valideaza(MuzeuDBDataContext db, Artefacte a, bool verificaLocatia)
        {
            var erori = new List<string>();
 
            // --- campuri obligatorii și lungimi (conform coloanelor din SQL) ---
            if (string.IsNullOrEmpty(a.CodInventar))
                erori.Add("Codul de inventar este obligatoriu.");
            else
                VerificaLungime(erori, a.CodInventar, 20, "Codul de inventar");
 
            if (string.IsNullOrEmpty(a.Denumire))
                erori.Add("Denumirea este obligatorie.");
            else
                VerificaLungime(erori, a.Denumire, 100, "Denumirea");
 
            VerificaLungime(erori, a.Autor, 100, "Autorul");
            VerificaLungime(erori, a.Perioada, 50, "Perioada");
            VerificaLungime(erori, a.Material, 100, "Materialul");
            VerificaLungime(erori, a.Dimensiuni, 50, "Dimensiunile");
            VerificaLungime(erori, a.Provenienta, 150, "Proveniența");
 
            // --- valori ---
            if (!StariConservare.Toate.Contains(a.StareConservare))
                erori.Add("Starea de conservare nu este validă.");
 
            if (a.ValoareEstimata.HasValue && a.ValoareEstimata.Value < 0)
                erori.Add("Valoarea estimată nu poate fi negativă.");
 
            if (a.DataAchizitie.HasValue && a.DataAchizitie.Value > DateTime.Today)
                erori.Add("Data achiziției nu poate fi în viitor.");
 
            if (a.AnAproximativ.HasValue && a.AnAproximativ.Value > DateTime.Today.Year)
                erori.Add("Anul aproximativ nu poate fi în viitor.");
 
            // --- legaturi cu alte tabele ---
            int idCategorie = a.IdCategorie;
            if (!db.Categorii.Any(c => c.IdCategorie == idCategorie))
                erori.Add("Categoria selectată nu există.");
 
            if (!string.IsNullOrEmpty(a.CodInventar))
            {
                string cod = a.CodInventar;
                int idArtefact = a.IdArtefact;
                if (db.Artefacte.Any(x => x.CodInventar == cod && x.IdArtefact != idArtefact))
                    erori.Add($"Codul de inventar „{cod}” este deja folosit de alt artefact.");
            }
 
            if (verificaLocatia && a.IdExpozitie.HasValue)
            {
                string eroareExpozitie = VerificaExpozitieDisponibila(db, a.IdExpozitie.Value);
                if (eroareExpozitie != null)
                    erori.Add(eroareExpozitie);
            }
 
            if (erori.Count > 0)
                throw new MuzeuException(erori);
        }
    }
}
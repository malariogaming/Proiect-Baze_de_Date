using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class ArtefactDto
    {
        public int IdArtefact { get; set; }
        public string CodInventar { get; set; }
        public string Denumire { get; set; }
        public string Autor { get; set; }
        public string Perioada { get; set; }
        public int? AnAproximativ { get; set; }
        public string Material { get; set; }
        public string StareConservare { get; set; }
        public decimal? ValoareEstimata { get; set; }
        public string Categorie { get; set; }

        /// <summary>null = artefactul e in depozit.</summary>
        public int? IdExpozitie { get; set; }

        /// <summary>Titlul expozitiei sau "Depozit".</summary>
        public string Locatie { get; set; }

        /// <summary>true daca are o restaurare inceputa si nefinalizata.</summary>
        public bool InRestaurare { get; set; }
    }
}
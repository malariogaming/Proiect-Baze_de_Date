using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class VizitaDto
    {
        public int IdVizita { get; set; }
        public DateTime DataVizita { get; set; }
        public string Status { get; set; }
        public DateTime DataCreare { get; set; }
        public string Observatii { get; set; }

        public int? IdVizitator { get; set; }

        public string Vizitator { get; set; }
        public string Telefon { get; set; }

        public int? IdTur { get; set; }

        public string Tur { get; set; }

        public string Casier { get; set; }

        public int NrBilete { get; set; }
        public decimal Total { get; set; }

        public List<BiletDto> Bilete { get; set; }
    }

    public class BiletDto
    {
        public int IdBilet { get; set; }
        public string TipBilet { get; set; }

        public string Expozitie { get; set; }

        public decimal PretAchitat { get; set; }
    }
}
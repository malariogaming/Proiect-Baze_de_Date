using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class CerereVizita
    {
        public int? IdVizitator { get; set; }

        public DateTime DataVizita { get; set; }

        public int? IdTur { get; set; }

        public string Observatii { get; set; }

        public List<CerereBilet> Bilete { get; set; } = new List<CerereBilet>();
        public CerereVizita AdaugaBilete(int idTipBilet, int cantitate, int? idExpozitie = null)
        {
            Bilete.Add(new CerereBilet { IdTipBilet = idTipBilet, Cantitate = cantitate, IdExpozitie = idExpozitie });
            return this;
        }
    }
    public class CerereBilet
    {
        public int IdTipBilet { get; set; }
        public int Cantitate { get; set; }
        public int? IdExpozitie { get; set; }
    }
}
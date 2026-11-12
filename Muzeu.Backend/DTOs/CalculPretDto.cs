using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class CalculPretDto
    {
        public List<LiniePretDto> Linii { get; set; } = new List<LiniePretDto>();

        public int NrBilete => Linii.Sum(l => l.Cantitate);
        public decimal Total => Linii.Sum(l => l.Total);
    }
    public class LiniePretDto
    {
        public int IdTipBilet { get; set; }
        public string TipBilet { get; set; }
        public int? IdExpozitie { get; set; }

        public string Expozitie { get; set; }

        public int Cantitate { get; set; }
        public decimal PretBaza { get; set; }
        public decimal SuplimentExpozitie { get; set; }
        public decimal SuplimentTur { get; set; }

        public decimal PretUnitar => PretBaza + SuplimentExpozitie + SuplimentTur;
        public decimal Total => PretUnitar * Cantitate;
    }
}

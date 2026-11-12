using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class RestaurareDto
    {
        public int IdRestaurare { get; set; }
        public int IdArtefact { get; set; }
        public string CodInventar { get; set; }
        public string Artefact { get; set; }
        public string Restaurator { get; set; }
        public DateTime DataInceput { get; set; }
        public DateTime? DataSfarsit { get; set; }
        public string Descriere { get; set; }
        public decimal? Cost { get; set; }
        public bool EsteInCurs => !DataSfarsit.HasValue;
        public string Perioada =>
            DataSfarsit.HasValue
                ? $"{DataInceput:dd.MM.yyyy} - {DataSfarsit.Value:dd.MM.yyyy}"
                : $"din {DataInceput:dd.MM.yyyy} (în curs)";
    }
}
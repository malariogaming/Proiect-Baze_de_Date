using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Muzeu.Backend.DTOs
{
    public class ExpozitieDto
    {
        public int IdExpozitie { get; set; }
        public string Titlu { get; set; }
        public string Tip { get; set; }
        public DateTime DataInceput { get; set; }
        public DateTime? DataSfarsit { get; set; }
        public string Sala { get; set; }
        public string Curator { get; set; }
        public decimal PretSuplimentar { get; set; }
        public int NrArtefacte { get; set; }

        public string Status
        {
            get
            {
                DateTime azi = DateTime.Today;
                if (DataInceput > azi) return "Viitoare";
                if (DataSfarsit.HasValue && DataSfarsit.Value < azi) return "Incheiata";
                return "Activa";
            }
        }

        public string Perioada =>
            DataSfarsit.HasValue
                ? $"{DataInceput:dd.MM.yyyy} - {DataSfarsit.Value:dd.MM.yyyy}"
                : $"din {DataInceput:dd.MM.yyyy}";
    }
}
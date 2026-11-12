using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class ProgramZiDto
    {
        public int? IdExceptie { get; set; }

        /// <summary>null pentru programul saptamâaal.</summary>
        public DateTime? Data { get; set; }

        /// <summary>1 = Luni ... 7 = Duminica.</summary>
        public byte ZiSaptamana { get; set; }

        public string NumeZi { get; set; }
        public bool Inchis { get; set; }
        public TimeSpan? OraDeschidere { get; set; }
        public TimeSpan? OraInchidere { get; set; }
        public bool EsteExceptie { get; set; }
        public string Motiv { get; set; }

        /// <summary>Text pentru afisare: "10:00 - 18:00" sau "inchis".</summary>
        public string Ore => Inchis ? "Închis" : $"{Formateaza(OraDeschidere)} - {Formateaza(OraInchidere)}";

        private static string Formateaza(TimeSpan? ora) =>
            ora.HasValue ? ora.Value.ToString(@"hh\:mm") : "?";
    }
}
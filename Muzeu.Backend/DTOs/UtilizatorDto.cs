using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class UtilizatorDto
    {
        public int IdUtilizator { get; set; }
        public string NumeUtilizator { get; set; }
        public string Rol { get; set; }
        public bool Activ { get; set; }
        public DateTime? UltimaAutentificare { get; set; }
        public int? IdAngajat { get; set; }
        public string NumeAngajat { get; set; }
    }
}
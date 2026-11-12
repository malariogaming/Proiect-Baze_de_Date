using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class UtilizatorAutentificatDto
    {
        public int IdUtilizator { get; set; }
        public string NumeUtilizator { get; set; }
        public string Rol { get; set; }
        public int? IdAngajat { get; set; }
        public string NumeAngajat { get; set; }
        public string NumeAfisat => NumeAngajat ?? NumeUtilizator;
    }
}
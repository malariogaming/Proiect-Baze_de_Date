using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class AngajatDto
    {
        public int IdAngajat { get; set; }
        public string Nume { get; set; }
        public string Prenume { get; set; }
        public string Functie { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public DateTime DataAngajarii { get; set; }
        public bool Activ { get; set; }

        public string NumeUtilizator { get; set; }

        public string NumeComplet => $"{Nume} {Prenume}";
        public bool AreCont => NumeUtilizator != null;
    }
}
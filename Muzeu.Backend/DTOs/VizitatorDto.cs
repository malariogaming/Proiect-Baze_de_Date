using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class VizitatorDto
    {
        public int IdVizitator { get; set; }
        public string Nume { get; set; }
        public string Prenume { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public DateTime DataInregistrare { get; set; }
        public int NrVizite { get; set; }
        public string NumeComplet => $"{Nume} {Prenume}";
        public override string ToString() => NumeComplet;
    }
}
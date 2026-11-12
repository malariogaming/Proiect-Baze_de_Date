using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class TipBiletDto
    {
        public int IdTipBilet { get; set; }
        public string Denumire { get; set; }
        public decimal Pret { get; set; }
        public bool Activ { get; set; }
        public string Afisare => $"{Denumire} - {Pret:0.00} lei";
        public override string ToString() => Afisare;
    }
}

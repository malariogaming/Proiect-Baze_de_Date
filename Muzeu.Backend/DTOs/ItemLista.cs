using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class ItemLista
    {
        public int Id { get; set; }
        public string Denumire { get; set; }

        public override string ToString() => Denumire;
    }
}

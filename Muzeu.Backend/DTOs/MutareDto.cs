using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class MutareDto
    {
        public int IdMutare { get; set; }
        public DateTime DataMutare { get; set; }

        /// <summary>Titlul expozitiei de unde a plecat sau "Depozit".</summary>
        public string De { get; set; }

        /// <summary>Titlul expoziției unde a ajuns sau "Depozit".</summary>
        public string La { get; set; }
    }
}
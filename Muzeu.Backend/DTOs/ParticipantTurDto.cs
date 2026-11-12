using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class ParticipantTurDto
    {
        public int IdVizita { get; set; }

        public string Vizitator { get; set; }

        public string Telefon { get; set; }
        public string Email { get; set; }

        public int NrPersoane { get; set; }

        public string Status { get; set; }
        public DateTime DataVizita { get; set; }
        public DateTime DataCreare { get; set; }
    }
}
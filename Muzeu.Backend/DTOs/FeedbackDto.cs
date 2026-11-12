using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace Muzeu.Backend.DTOs
{
    public class FeedbackDto
    {
        public int IdFeedback { get; set; }
        public int IdVizita { get; set; }
        public DateTime DataFeedback { get; set; }
        public string Vizitator { get; set; }
        public string Expozitie { get; set; }

        public byte Nota { get; set; }
        public string Comentariu { get; set; }
        public string Stele => new string('★', Nota) + new string('☆', 5 - Nota);
    }
}
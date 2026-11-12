using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class FiltruArtefacte
    {
        /// <summary>Cauta în denumire, cod de inventar, autor si material.</summary>
        public string Text { get; set; }

        public int? IdCategorie { get; set; }

        /// <summary>Una dintre valorile din StariConservare.</summary>
        public string StareConservare { get; set; }

        /// <summary>Doar artefactele dintr-o anumita expozitie.</summary>
        public int? IdExpozitie { get; set; }

        /// <summary>Doar artefactele din depozit (are prioritate fata de IdExpozitie).</summary>
        public bool DoarDepozit { get; set; }

        /// <summary>Doar artefactele aflate acum in restaurare.</summary>
        public bool DoarInRestaurare { get; set; }
    }
}
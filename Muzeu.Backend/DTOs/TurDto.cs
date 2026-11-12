using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.DTOs
{
    public class TurDto
    {
        public int IdTur { get; set; }
        public int IdExpozitie { get; set; }
        public string Expozitie { get; set; }
        public int IdGhid { get; set; }
        public string Ghid { get; set; }
        public DateTime DataOraInceput { get; set; }
        public int DurataMinute { get; set; }
        public string Limba { get; set; }
        public int NrMaximParticipanti { get; set; }

        public int NrParticipanti { get; set; }

        public decimal PretSuplimentar { get; set; }

        public DateTime DataOraSfarsit => DataOraInceput.AddMinutes(DurataMinute);
        public int LocuriLibere => Math.Max(0, NrMaximParticipanti - NrParticipanti);
        public bool EsteComplet => LocuriLibere == 0;
        public bool AAvutLoc => DataOraInceput <= DateTime.Now;

        public string Interval => $"{DataOraInceput:dd.MM.yyyy HH:mm}-{DataOraSfarsit:HH:mm}";
    }
}
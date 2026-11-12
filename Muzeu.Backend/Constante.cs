using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend
{
  
    public static class TipuriExpozitie
    {
        public const string Permanenta = "Permanenta";
        public const string Temporara = "Temporara";
        public const string Itineranta = "Itineranta";

        public static readonly string[] Toate = { Permanenta, Temporara, Itineranta };
    }

    public static class FunctiiAngajat
    {
        public const string Administrator = "Administrator";
        public const string Curator = "Curator";
        public const string Ghid = "Ghid";
        public const string Casier = "Casier";
        public const string Restaurator = "Restaurator";

        public static readonly string[] Toate = { Administrator, Curator, Ghid, Casier, Restaurator };
    }

    public static class RoluriUtilizator
    {
        public const string Admin = "Admin";
        public const string Curator = "Curator";
        public const string Casier = "Casier";
        public const string Ghid = "Ghid";

        public static readonly string[] Toate = { Admin, Curator, Casier, Ghid };
    }

    public static class StariConservare
    {
        public const string FoarteBuna = "Foarte buna";
        public const string Buna = "Buna";
        public const string Satisfacatoare = "Satisfacatoare";
        public const string Deteriorata = "Deteriorata";

        public static readonly string[] Toate = { FoarteBuna, Buna, Satisfacatoare, Deteriorata };
    }

    public static class StatusuriVizita
    {
        public const string Rezervata = "Rezervata";
        public const string Confirmata = "Confirmata";
        public const string Efectuata = "Efectuata";
        public const string Anulata = "Anulata";

        public static readonly string[] Toate = { Rezervata, Confirmata, Efectuata, Anulata };
    }
}
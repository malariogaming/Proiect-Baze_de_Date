using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Muzeu.Backend.DTOs;

namespace Muzeu.Backend
{
    public static class Sesiune
    {
        public static UtilizatorAutentificatDto UtilizatorCurent { get; internal set; }

        public static bool EsteAutentificat => UtilizatorCurent != null;

        public static bool AreRol(params string[] roluri)
        {
            return UtilizatorCurent != null && roluri.Contains(UtilizatorCurent.Rol);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Muzeu.Backend
{
    public static class DbFactory
    {
        public static MuzeuDBDataContext Create()
        {
            var cs = ConfigurationManager.ConnectionStrings["MuzeuDB"];
            if (cs == null)
                throw new ConfigurationErrorsException(
                    "Lipseste connection string-ul 'MuzeuDB' din App.config.");

            return new MuzeuDBDataContext(cs.ConnectionString);
        }
    }
}
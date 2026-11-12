using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Muzeu.Backend.Exceptions
{

    public class MuzeuException : Exception
    {
        public IReadOnlyList<string> Erori { get; }

        public MuzeuException(string mesaj) : base(mesaj)
        {
            Erori = new[] { mesaj };
        }

        public MuzeuException(IEnumerable<string> erori)
            : base(string.Join(Environment.NewLine, erori))
        {
            Erori = erori.ToList();
        }
    }
}
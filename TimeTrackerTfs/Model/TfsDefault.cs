using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTrackerTfs.Model
{
    public class TfsDefault<T>
    {
        public int count { get; set; }
        public List<T> value { get; set; }
    }
}

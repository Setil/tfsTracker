using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTrackerTfs.Model
{
    public class ConfigDTO
    {
        public string UserName { get; set; }
        public string TfsUrl { get; set; }
        public string Language { get; set; }
        public string VersionPath { get; set; }
        public int UpdateCicle { get; set; }
        public int IdleCicle
        {
            get
            {
                return UpdateCicle * 6;
            }
        }
        public int SyncCicle
        {
            get
            {
                return UpdateCicle * 5;
            }
        }
    }
}

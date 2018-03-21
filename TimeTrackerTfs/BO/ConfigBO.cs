using arq.Common.VO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTrackerTfs.Model;
using arq.Common.Utilities;

namespace TimeTrackerTfs.BO
{
    public class ConfigBO
    {
        private static string configJsonPath = AppDomain.CurrentDomain.BaseDirectory +"\\config.json";
        public static BusinessObject<ConfigDTO> Load()
        {
            try
            {
                if (!File.Exists(configJsonPath))
                    return null;
                var lst = LoadList();
                if (lst == null || lst.Count() == 0)
                    return null;
                var dto = lst.Where(t => t.UserName == Environment.UserName).FirstOrDefault();
                if (dto == null)
                    dto = lst.FirstOrDefault();
                return dto.EntityToBusinessObject();
            }
            catch(Exception ex)
            {
                return ConstructError<ConfigDTO>.Set(ex);
            } 
        }

        private static List<ConfigDTO> LoadList()
        {

            if (!File.Exists(configJsonPath))
                return null;
            using (var sr = File.OpenText(configJsonPath))
            {
                string file = sr.ReadToEnd();
                var lst = JsonConvert.DeserializeObject<List<ConfigDTO>>(file);
                return lst;
            }
        }

        public static BusinessObject<ConfigDTO> Save(ConfigDTO dto)
        {
            try
            {
                var lst = LoadList();
                if (lst == null)
                    lst = new List<ConfigDTO>();
                var dtoAux = lst.Where(t => t.UserName == Environment.UserName).FirstOrDefault();
                if (dtoAux == null)
                    lst.Add(dto);
                else
                {
                    dtoAux.TfsUrl = dto.TfsUrl;
                    dtoAux.VersionPath = dto.VersionPath;
                    dtoAux.UpdateCicle = dto.UpdateCicle;
                    dtoAux.Language = dto.Language;
                }
                using (var sw = File.CreateText(configJsonPath))
                {
                    sw.Write(JsonConvert.SerializeObject(lst));
                    sw.Close();
                }
                return dto.EntityToBusinessObject();
            }
            catch(Exception ex)
            {
                return ConstructError<ConfigDTO>.Set(ex);
            }
            
        }
    }
}

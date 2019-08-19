using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.Helper
{
    public static class ConfigurationHelper
    {
        public static dynamic getConfiguration()
        {
            string arquivo = "./appsettings.json";
            using (StreamReader sr = new StreamReader(arquivo))
            {
                var sJson = sr.ReadToEnd();
                return Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
            }
        }
    }
}

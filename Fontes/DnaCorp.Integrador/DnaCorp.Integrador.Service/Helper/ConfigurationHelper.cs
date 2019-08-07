using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DnaCorp.Integrador.Service.Helper
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

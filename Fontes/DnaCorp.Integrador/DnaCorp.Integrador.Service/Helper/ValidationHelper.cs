using System;
using System.Collections.Generic;
using System.Text;

namespace DnaCorp.Integrador.Service.Helper
{
    public static class ValidationHelper
    {
        public static bool IsValid()
        {
            dynamic config = ConfigurationHelper.getConfiguration();
            string textCifrado = Convert.ToString(Convert.ToString(config.ValidationKey));
            var textoClaro = CryptoHelper.ConvertBase64ToString(textCifrado);
            var json = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(textoClaro);
            DateTime validoAte = Convert.ToDateTime(json.data);
            return DateTime.Now < validoAte;
        }
    }
}

using Newtonsoft.Json;
using System;

namespace DnaCorp.Robo.Integrador.Service.Helper
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
        public static string CreateKey(DateTime data)
        {
            var obj = new
            {
                data = data
            };

            var textoClaro = JsonConvert.SerializeObject(obj);
            var textoCifra = CryptoHelper.ConvertStringToBase64(textoClaro);

            return textoCifra;
        }
    }
}

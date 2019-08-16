using System;
using System.Collections.Generic;
using System.Text;

namespace DnaCorp.Integrador.Service.Helper
{
    public static class CryptoHelper
    {
        public static string ConvertBase64ToString(string textoCifrado)
        {
            byte[] bytes = Convert.FromBase64String(textoCifrado);
            return  ASCIIEncoding.ASCII.GetString(bytes);            
        }
        public static string ConvertStringToBase64(string textoClaro)
        {
            var bytes = ASCIIEncoding.ASCII.GetBytes(textoClaro);
            return Convert.ToBase64String(bytes);
        }
    }
}

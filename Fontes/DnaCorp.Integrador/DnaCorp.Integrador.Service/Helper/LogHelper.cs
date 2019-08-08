using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DnaCorp.Integrador.Service.Helper
{
    public static class LogHelper
    {
        public static void CriarLog(string mensagem, bool sucesso)
        {
            var pasta = $@"c:\temp\dnacorp\integrador\log\{(sucesso ? "sucesso":"erros")}\";
            if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);

            var arquivo = $"{pasta}{DateTime.Now.ToString("yyMMddHHmmss")}.txt";

            using(StreamWriter sw = new StreamWriter(arquivo))
            {
                sw.WriteLine(mensagem);
                sw.Flush();
            }
        }
    }
}

using DnaCorp.Integrador.Domain.Contratos.Job;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DnaCorp.Integrador.Service.JOB
{
    public class TesteJobService : ITesteJobService
    {
        public void Executa()
        {
            //throw new NotImplementedException();
            CriarArquivo();
        }

        private void CriarArquivo()
        {
            var pasta = "c:/TesteJob/";

            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            var nomeArquivo = $"{pasta}teste_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";

            var sorteador = new Random();
            var numerosSorteados = new List<int>();

            for (int i = 0; i < 6; i++)
            {
                numerosSorteados.Add(sorteador.Next(1, 60));
            }

            numerosSorteados.Sort();

            using (var arquivo = new StreamWriter(nomeArquivo))
            {
                arquivo.WriteLine("Numeros sorteados:");
                foreach (var item in numerosSorteados)
                {
                    arquivo.Write(item.ToString("00 "));
                }

                arquivo.Flush();
                arquivo.Close();
            }
        }
    }
}

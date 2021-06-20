using DnaCorp.Integrador.Domain.Contratos.Job;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hangfire.Console;
using Hangfire.Server;

namespace DnaCorp.Integrador.Service.JOB
{
    public class TesteJobService : ITesteJobService
    {
        PerformContext context;

        public void Executa(PerformContext context)
        {
            this.context = context;
            //throw new NotImplementedException();
            CriarArquivo();
        }

        private void Pausa(int segundos)
        {
            System.Threading.Thread.Sleep(segundos * 1000);
        }
        private void Mensagem(string texto)
        {
            if (context != null)
            {
                context.WriteLine(texto);
                Pausa(4);
            }
                
        }
        private void CriarArquivo()
        {
            Mensagem("Iniciando Sorteio...");

            var pasta = "c:/TesteJob/";

            Mensagem($"Verificando existencia da pasta {pasta}");

            if (Directory.Exists(pasta))
                Mensagem("A pasta já existe");            
            else
            {
                Mensagem("A pasta ainda não foi criada. Criando a pasta");
                Directory.CreateDirectory(pasta);
            }
                

            var nomeArquivo = $"{pasta}teste_{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt";

            var sorteador = new Random();
            var numerosSorteados = new List<int>();
            Mensagem("Iniciando sorteio de 6 números de 1 a 60");
            for (int i = 0; i < 6; i++)
            {
                var numero = sorteador.Next(1, 60);
                Mensagem($"{(i + 1)}ª número sorteado: {numero.ToString("00")}");
                
                numerosSorteados.Add(numero);
            }

            numerosSorteados.Sort();

            var resultado = "";

            using (var arquivo = new StreamWriter(nomeArquivo))
            {
                arquivo.WriteLine("Numeros sorteados:");
                foreach (var item in numerosSorteados)
                {
                    resultado += item.ToString("00 ");
                }

                Mensagem($"Resultado do sorteio: {resultado}");

                arquivo.Write(resultado);

                arquivo.Flush();
                arquivo.Close();
            }

            Mensagem($"Gravando arquivo em {nomeArquivo}");

            Mensagem("Encerrando Job");
        }
    }
}

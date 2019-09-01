using DnaCorp.Robo.Integrador.Service.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DnaCorp.Robo.Integrador.Service.JOB
{
    public class ObterPosicoesOmnilinkJobService : IObterDados
    {
        public ObterDadosResponse Executa()
        {
            Teste();
            throw new NotImplementedException();
        }

        private void Teste()
        {
            var tcpClient = new TcpClient();

            try
            {
                dynamic config = ConfigurationHelper.getConfiguration();
                
                var endereco = Convert.ToString(config.Rastreadores.Omnilink.Endereco);
                var porta = Convert.ToString(config.Rastreadores.Omnilink.Porta);
               
                var enderecoIP = IPAddress.Parse(endereco);

                tcpClient.Connect(enderecoIP, Convert.ToInt32(porta));

                var srReceptor = new StreamReader(tcpClient.GetStream());

                var strPacote = srReceptor.ReadToEnd();
                
            }
            catch (Exception erro)
            {
                throw erro; 
            }
            finally
            {
                if (tcpClient.Connected) tcpClient.Close();
            }
        }
    }
}

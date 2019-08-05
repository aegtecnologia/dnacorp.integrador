using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterEspelhamentoJaburJobService : IObterEspelhamentoJaburJobService
    {
        private IConexao _conexao;

        public ObterEspelhamentoJaburJobService(IConexao conexao)
        {
            _conexao = conexao ?? throw new ArgumentNullException(nameof(conexao));
            _conexao.Configura("");
        }
        public void Executa()
        {
            var veiculos = ObterEspelhamento();

            PersistirDados(veiculos);
        }

        private void PersistirDados(List<EspelhamentoJabur> veiculos)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var veiculo in veiculos)
            {
                sb.AppendLine($@"insert into espelhamento_jabur values ({veiculo.VeiculoId.ToString()},'{veiculo.Placa}','{FormataData(veiculo.EspelhadoAte).ToString("yyyy/MM/dd")}');");
            }

            _conexao.Executa(sb.ToString());
        }
        private string ReceberVeiculosMock()
        {
            var pasta = @"C:\Anderson\documents\Interage\";
            var arquivo = $"{pasta}jabur-lista-veiculos-20190708170801.xml";

            using (StreamReader sr = new StreamReader(arquivo))
            {
                return sr.ReadToEnd();
            }
        }

        private DateTime FormataData(string data)
        {
            var dataPartes = data.Split("/");
            return new DateTime(Convert.ToInt32(dataPartes[2]), Convert.ToInt32(dataPartes[1]), Convert.ToInt32(dataPartes[0]));
        }

        private string ReceberMensagensMock()
        {
            //ToDo
            var pasta = @"C:\AEG\Desenvolvimento\Projetos\Interage\Arquivos\";
            var arquivo = $"{pasta}jabur-mensagens-20190708171146.xml";

            using (StreamReader sr = new StreamReader(arquivo))
            {
                return sr.ReadToEnd();
            }
        }

        public List<EspelhamentoJabur> ObterEspelhamento()
        {
            var espelhamentos = new List<EspelhamentoJabur>();
            var xmlResponse = ReceberVeiculosMock();

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("Veiculo");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    espelhamentos.Add(new EspelhamentoJabur()
                    {
                        VeiculoId = (int)msg.veiID,
                        Placa = msg.placa,
                        EspelhadoAte = msg.valEspelhamento
                    });
                }
            }

            return espelhamentos;
        }

    }
}

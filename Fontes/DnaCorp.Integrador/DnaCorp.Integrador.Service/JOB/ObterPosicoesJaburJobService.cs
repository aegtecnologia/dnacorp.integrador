using DnaCorp.Integrador.Domain.Contratos.Job;
using DnaCorp.Integrador.Domain.Dominios;
using DnaCorp.Integrador.Infra.MSSql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DnaCorp.Integrador.Service.JOB
{
    public class ObterPosicoesJaburJobService : IObterPosicoesJaburJobService
    {
        private IConexao _conexao;

        public ObterPosicoesJaburJobService(IConexao conexao)
        {
            _conexao = conexao ?? throw new ArgumentNullException(nameof(conexao));
            _conexao.Configura("");
        }
        public void Executa()
        {
            var posicoes = ObterPosicoes();

            PersistirDados(posicoes);
        }

        private void PersistirDados(List<PosicaoJabur> posicoes)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var p in posicoes)
            {
                sb.AppendLine($@"insert into posicoes_jabur values (
{p.VeiculoId.ToString()},
{p.VeiculoId},
'{p.Codigo}',
'{p.DataCadastro.ToString("yyyy/MM/dd")}',
'{p.Data.ToString("yyyy/MM/dd")}',
'{p.Latitude}',
'{p.Longitude}',
'{p.UF}',
'{p.Cidade}',
'{p.Endereco}');");
            }

            _conexao.Executa(sb.ToString());
        }
      

        private DateTime FormataData(string data)
        {
            var dataPartes = data.Split("/");
            return new DateTime(Convert.ToInt32(dataPartes[2]), Convert.ToInt32(dataPartes[1]), Convert.ToInt32(dataPartes[0]));
        }

        private string ReceberPosicoesMock()
        {
            //ToDo
            var pasta = @"C:\Anderson\dnacorp.integrador\Recursos\documentos\jabur\modelos\";
            var arquivo = $"{pasta}jabur-mensagens-20190708171146.xml";

            using (StreamReader sr = new StreamReader(arquivo))
            {
                return sr.ReadToEnd();
            }
        }

        public List<PosicaoJabur> ObterPosicoes()
        {
            var posicoes = new List<PosicaoJabur>();
            var xmlResponse = ReceberPosicoesMock();

            using (MemoryStream ms = new MemoryStream())
            {
                var xml = new XmlDocument();
                xml.LoadXml(xmlResponse);
                var mensagens = xml.GetElementsByTagName("Veiculo");
                foreach (XmlNode no in mensagens)
                {
                    string sJson = Newtonsoft.Json.JsonConvert.SerializeXmlNode(no, Newtonsoft.Json.Formatting.None, true);
                    dynamic msg = Newtonsoft.Json.JsonConvert.DeserializeObject(sJson);
                    posicoes.Add(new PosicaoJabur()
                    {
                        Codigo = msg.mId,
                        VeiculoId = (int)msg.veiID,
                        Data = msg.dt,
                        DataCadastro = DateTime.Now,
                        Latitude = msg.lat,
                        Longitude = msg.lon,
                        Cidade = msg.mun,
                        UF = msg.uf,
                        Endereco = msg.rua,
                    }) ;
                }
            }

            return posicoes;
        }
    }
}
